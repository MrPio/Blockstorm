using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Model;
using Network;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Weapon = Model.Weapon;

namespace Prefabs
{
    /// <summary>
    /// This is not a network object since weapons prefab need to be loaded inside
    /// </summary>
    public class Collectable : MonoBehaviour
    {
        private static SceneManager _sm;
        [SerializeField] private GameObject ammoLight;
        [SerializeField] private GameObject hpLight;
        [SerializeField] private GameObject weaponLight;
        [SerializeField] private GameObject collectableContainer;
        [SerializeField] private GameObject ammoPrefab;
        [SerializeField] private GameObject[] medkitPrefabs;
        [SerializeField] private AudioClip lootAudioClip;

        public Model.Collectable Model;

        public void Initialize(Model.Collectable collectable)
        {
            _sm = FindObjectOfType<SceneManager>();
            Model = collectable;
            ammoLight.SetActive(collectable.Type is CollectableType.Ammo);
            hpLight.SetActive(collectable.Type is CollectableType.Hp);
            weaponLight.SetActive(collectable.Type is CollectableType.Weapon);
            var item = collectable.Type switch
            {
                CollectableType.Ammo => ammoPrefab,
                CollectableType.Hp => medkitPrefabs[(int)collectable.MedkitType!.Value],
                CollectableType.Weapon => null,
                _ => throw new ArgumentOutOfRangeException()
            };
            if (item is not null)
                Instantiate(item, collectableContainer.transform);
            collectableContainer.transform.rotation = Quaternion.Euler(0f, Random.Range(-180, 180f), 90f);
        }

        /// <summary>
        /// If the collectable is a weapon, load the next upgrade for the current player.
        /// If the player's current weapon has no more upgrades, change the collectable type to ammo.
        /// </summary>
        /// <param name="playerStatus">The current player arsenal</param>
        public void TryUpdateWeaponPrefab(PlayerStatus playerStatus)
        {
            if (Model.Type is not CollectableType.Weapon || Model.WeaponType is null)
                return;
            var currentWeapon = playerStatus.WeaponType2Weapon(Model.WeaponType.Value);

            var upgradedWeapon = currentWeapon.GetUpgraded ?? currentWeapon;

            // Skip if the weapon is already loaded
            if (Model.WeaponItem is not null && Model.WeaponItem.GetNetName == upgradedWeapon.GetNetName)
                return;
            Model.WeaponItem = upgradedWeapon;

            // If the player has no more upgrades, change the collectable type to ammo
            // if (upgradedWeapon is null)
            // {
            //     Model.Type = CollectableType.Ammo;
            //     Initialize(Model);
            //     return;
            // }

            var upgradedWeaponPrefab = Resources.Load<GameObject>(Model.WeaponItem.GetPrefab(collectable: true));
            foreach (Transform child in collectableContainer.transform)
                if (!child.IsDestroyed())
                    Destroy(child.gameObject);
            var go = Instantiate(upgradedWeaponPrefab, collectableContainer.transform);
            // Load the weapon material and the scope
            if (Model.WeaponItem.Variant is not null)
                foreach (var mesh in go.GetComponentsInChildren<MeshRenderer>(true))
                    if (!mesh.gameObject.name.Contains("scope"))
                        mesh.material = Resources.Load<Material>(Model.WeaponItem.GetMaterial);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.gameObject.GetComponentInParent<Player.Player>();
            if (player is not null && player.IsOwner)
            {
                player.audioSource.PlayOneShot(lootAudioClip);
                LootCollectable(player, Model);

                _sm.ServerManager.LootCollectableServerRpc(Model.ID);
                Destroy(gameObject);
            }
        }

        public static void LootCollectable(Player.Player player, Model.Collectable model)
        {
            var newStatus = player.Status.Value;

            if (model.Type is CollectableType.Weapon)
            {
                // Equip the weapon
                if (model.WeaponItem!.Type is WeaponType.Primary)
                    newStatus.Primary = model.WeaponItem;
                if (model.WeaponItem!.Type is WeaponType.Secondary)
                    newStatus.Secondary = model.WeaponItem;
                if (model.WeaponItem!.Type is WeaponType.Tertiary)
                    newStatus.Tertiary = model.WeaponItem;
                if (model.WeaponItem!.Type is WeaponType.Grenade)
                    newStatus.Grenade = model.WeaponItem;
                if (model.WeaponItem!.Type is WeaponType.GrenadeSecondary)
                    newStatus.GrenadeSecondary = model.WeaponItem;


                // Add ammo
                if (model.WeaponItem!.Type is WeaponType.Grenade)
                    newStatus.LeftGrenades += (byte)Random.Range(1, 4);
                else if (model.WeaponItem!.Type is WeaponType.GrenadeSecondary)
                    newStatus.LeftSecondaryGrenades += (byte)Random.Range(1, 3);
                else if (player.weapon.LeftAmmo.ContainsKey(model.WeaponItem!.GetNetName))
                    player.weapon.LeftAmmo[model.WeaponItem!.GetNetName] +=
                        model.WeaponItem!.Type is WeaponType.Tertiary
                            ? Random.Range(1, model.WeaponItem.Ammo!.Value + 1)
                            : (int)(model.WeaponItem.Ammo!.Value / Random.Range(2f, 4f));
                else
                {
                    player.weapon.LeftAmmo[model.WeaponItem!.GetNetName] = model.WeaponItem!.Ammo!.Value;
                    player.weapon.Magazine[model.WeaponItem!.GetNetName] = model.WeaponItem!.Magazine!.Value;
                }

                var lootedModel = model.WeaponItem!;
                player.Status.Value = newStatus;

                // Refresh the equipped weapon and Ammo HUD
                if (lootedModel.Type is not WeaponType.Grenade and not WeaponType.GrenadeSecondary)
                {
                    player.weapon.SwitchEquipped(lootedModel.Type, force: true);
                    _sm.ammoHUD.SetAmmo(player.weapon.Magazine[lootedModel.GetNetName],
                        player.weapon.LeftAmmo[lootedModel.GetNetName],
                        isTertiary: lootedModel.Type is WeaponType.Tertiary);
                }
            }
            else if (model.Type is CollectableType.Ammo)
            {
                var refillFactor = Random.Range(2f, 4f);
                foreach (var leftAmmoKey in new List<string>(player.weapon.LeftAmmo.Keys))
                    player.weapon.LeftAmmo[leftAmmoKey] +=
                        (int)(Weapon.Name2Weapon(leftAmmoKey)!.Ammo!.Value / refillFactor);
                if (player.weapon.WeaponModel!.IsGun)
                    _sm.ammoHUD.SetAmmo(player.weapon.Magazine[player.weapon.WeaponModel!.GetNetName],
                        player.weapon.LeftAmmo[player.weapon.WeaponModel!.GetNetName],
                        isTertiary: player.weapon.WeaponModel.Type is WeaponType.Tertiary);
                player.Status.Value = newStatus;
            }
            else if (model.Type is CollectableType.Hp)
            {
                newStatus.Hp = math.min(100,
                    newStatus.Hp + global::Model.Collectable.MedkitHps[model.MedkitType!.Value]);
                player.Status.Value = newStatus;
            }
        }
    }
}