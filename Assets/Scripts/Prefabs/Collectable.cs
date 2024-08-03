using System;
using System.Collections.Generic;
using ExtensionFunctions;
using Managers;
using Model;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using Weapon = Model.Weapon;

namespace Prefabs
{
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

        private Model.Collectable model;

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            model = global::Model.Collectable.GetRandomCollectable;
            ammoLight.SetActive(model.Type is CollectableType.Ammo);
            hpLight.SetActive(model.Type is CollectableType.Hp);
            weaponLight.SetActive(model.Type is CollectableType.Weapon);
            var item = model.Type switch
            {
                CollectableType.Ammo => ammoPrefab,
                CollectableType.Hp => medkitPrefabs[(int)model.MedkitType!.Value],
                CollectableType.Weapon =>
                    Resources.Load<GameObject>(model.WeaponItem!.GetPrefab(collectable: true)),
                _ => throw new ArgumentOutOfRangeException()
            };
            var go = Instantiate(item, collectableContainer.transform);
            collectableContainer.transform.rotation = Quaternion.Euler(0f, Random.Range(-180, 180f), 90f);
            if (model.Type is CollectableType.Weapon && model.WeaponItem!.Variant is not null)

                // Load the weapon material and the scope
                foreach (var mesh in go.GetComponentsInChildren<MeshRenderer>(true))
                    if (!mesh.gameObject.name.Contains("scope"))
                        mesh.material = Resources.Load<Material>(model.WeaponItem!.GetMaterial);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.gameObject.GetComponentInParent<Player.Player>();
            if (player.IsOwner)
            {
                var newStatus = player.Status.Value;
                player.audioSource.PlayOneShot(lootAudioClip);
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

                    // Add ammo
                    if (model.WeaponItem!.Type is WeaponType.Grenade)
                        newStatus.LeftGrenades += (byte)Random.Range(1, 4);
                    else if (player.weapon.LeftAmmo.ContainsKey(model.WeaponItem!.GetNetName))
                        player.weapon.LeftAmmo[model.WeaponItem!.GetNetName] +=
                            (int)(model.WeaponItem.Ammo!.Value / Random.Range(2f, 4f));
                    else
                    {
                        player.weapon.LeftAmmo[model.WeaponItem!.GetNetName] = model.WeaponItem!.Ammo!.Value;
                        player.weapon.Magazine[model.WeaponItem!.GetNetName] = model.WeaponItem!.Magazine!.Value;
                    }

                    player.Status.Value = newStatus;

                    // Refresh the equipped weapon and Ammo HUD
                    if (player.weapon.WeaponModel!.Type is not WeaponType.Grenade)
                    {
                        player.weapon.SwitchEquipped(model.WeaponItem!.Type);
                        _sm.ammoHUD.SetAmmo(player.weapon.Magazine[model.WeaponItem!.GetNetName],
                            player.weapon.LeftAmmo[model.WeaponItem!.GetNetName],
                            isTertiary: model.WeaponItem!.Type is WeaponType.Tertiary);
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
                    newStatus.Hp = math.min(100, newStatus.Hp + Model.Collectable.MedkitHps[model.MedkitType!.Value]);
                    player.Status.Value = newStatus;
                }

                Destroy(gameObject);
            }
        }
    }
}