using System;
using System.Collections.Generic;
using Managers;
using Model;
using Unity.Mathematics;
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
        private SceneManager _sm;
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
                CollectableType.Weapon =>
                    Resources.Load<GameObject>(collectable.WeaponItem!.GetPrefab(collectable: true)),
                _ => throw new ArgumentOutOfRangeException()
            };
            var go = Instantiate(item, collectableContainer.transform);
            collectableContainer.transform.rotation = Quaternion.Euler(0f, Random.Range(-180, 180f), 90f);
            if (collectable.Type is CollectableType.Weapon && collectable.WeaponItem!.Variant is not null)

                // Load the weapon material and the scope
                foreach (var mesh in go.GetComponentsInChildren<MeshRenderer>(true))
                    if (!mesh.gameObject.name.Contains("scope"))
                        mesh.material = Resources.Load<Material>(collectable.WeaponItem!.GetMaterial);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.gameObject.GetComponentInParent<Player.Player>();
            if (player is not null && player.IsOwner)
            {
                var newStatus = player.Status.Value;
                player.audioSource.PlayOneShot(lootAudioClip);
                if (Model.Type is CollectableType.Weapon)
                {
                    // Equip the weapon
                    if (Model.WeaponItem!.Type is WeaponType.Primary)
                        newStatus.Primary = Model.WeaponItem;
                    if (Model.WeaponItem!.Type is WeaponType.Secondary)
                        newStatus.Secondary = Model.WeaponItem;
                    if (Model.WeaponItem!.Type is WeaponType.Tertiary)
                        newStatus.Tertiary = Model.WeaponItem;
                    if (Model.WeaponItem!.Type is WeaponType.Grenade)
                        newStatus.Grenade = Model.WeaponItem;

                    // Add ammo
                    if (Model.WeaponItem!.Type is WeaponType.Grenade)
                        newStatus.LeftGrenades += (byte)Random.Range(1, 4);
                    else if (player.weapon.LeftAmmo.ContainsKey(Model.WeaponItem!.GetNetName))
                        player.weapon.LeftAmmo[Model.WeaponItem!.GetNetName] +=
                            (int)(Model.WeaponItem.Ammo!.Value / Random.Range(2f, 4f));
                    else
                    {
                        player.weapon.LeftAmmo[Model.WeaponItem!.GetNetName] = Model.WeaponItem!.Ammo!.Value;
                        player.weapon.Magazine[Model.WeaponItem!.GetNetName] = Model.WeaponItem!.Magazine!.Value;
                    }

                    player.Status.Value = newStatus;

                    // Refresh the equipped weapon and Ammo HUD
                    if (player.weapon.WeaponModel!.Type is not WeaponType.Grenade)
                    {
                        player.weapon.SwitchEquipped(Model.WeaponItem!.Type);
                        _sm.ammoHUD.SetAmmo(player.weapon.Magazine[Model.WeaponItem!.GetNetName],
                            player.weapon.LeftAmmo[Model.WeaponItem!.GetNetName],
                            isTertiary: Model.WeaponItem!.Type is WeaponType.Tertiary);
                    }
                }
                else if (Model.Type is CollectableType.Ammo)
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
                else if (Model.Type is CollectableType.Hp)
                {
                    newStatus.Hp = math.min(100, newStatus.Hp + global::Model.Collectable.MedkitHps[Model.MedkitType!.Value]);
                    player.Status.Value = newStatus;
                }

                _sm.serverManager.LootCollectableServerRpc(Model.ID);
                Destroy(gameObject);
            }
        }
    }
}