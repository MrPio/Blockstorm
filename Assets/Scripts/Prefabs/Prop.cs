using System;
using System.Collections;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Model;
using Partials;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Weapon = Model.Weapon;

namespace Prefabs
{
    public class Prop : MonoBehaviour
    {
        private SceneManager _sm;
        private Rigidbody _rb;
        [NonSerialized] public ushort ID;
        [SerializeField] public float Hp = 200;
        [SerializeField] private string lootWeapon;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
            _rb = FindObjectOfType<Rigidbody>();
        }

        private void DestroyProp(bool explode = false)
        {
            Destroy(_rb);
            foreach (var mesh in transform.GetComponentsInChildren<MeshRenderer>())
            {
                var rb = mesh.AddComponent<Rigidbody>();
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.mass = 100;
                rb.AddForce(VectorExtensions.RandomVector3(-1f, 1f).normalized * (explode ? 1750f : 750f),
                    ForceMode.Impulse);
                rb.angularVelocity = VectorExtensions.RandomVector3(-1f, 1f).normalized *
                                     Random.Range(0, explode ? 100f : 30f);
            }

            gameObject.AddComponent<Destroyable>();
            if (lootWeapon is not null)
                StartCoroutine(Collect());
            return;

            IEnumerator Collect()
            {
                yield return new WaitForSeconds(0.3f);
                GetComponent<AudioSource>().Play();
                var weapon = Weapon.Name2Weapon(lootWeapon);
                var player = FindObjectsOfType<Player.Player>().First(it => it.IsOwner);
                var newStatus = player.Status.Value;

                // Equip the weapon
                if (weapon.Type is WeaponType.Primary)
                    newStatus.Primary = weapon;
                if (weapon.Type is WeaponType.Secondary)
                    newStatus.Secondary = weapon;
                if (weapon.Type is WeaponType.Tertiary)
                    newStatus.Tertiary = weapon;
                if (weapon.Type is WeaponType.Grenade)
                    newStatus.Grenade = weapon;
                if (weapon.Type is WeaponType.GrenadeSecondary)
                    newStatus.GrenadeSecondary = weapon;

                // Add ammo
                if (weapon.Type is WeaponType.Grenade)
                    newStatus.LeftGrenades += (byte)Random.Range(1, 4);
                else if (weapon.Type is WeaponType.GrenadeSecondary)
                    newStatus.LeftSecondaryGrenades += (byte)Random.Range(1, 3);
                else if (player.weapon.LeftAmmo.ContainsKey(weapon.GetNetName))
                    player.weapon.LeftAmmo[weapon.GetNetName] +=
                        weapon.Type is WeaponType.Tertiary
                            ? Random.Range(1, weapon.Ammo!.Value + 1)
                            : (int)(weapon.Ammo!.Value / Random.Range(2f, 4f));
                else
                {
                    player.weapon.LeftAmmo[weapon.GetNetName] = weapon.Ammo!.Value;
                    player.weapon.Magazine[weapon.GetNetName] = weapon.Magazine!.Value;
                }

                player.Status.Value = newStatus;

                // Refresh the equipped weapon and Ammo HUD
                if (weapon.Type is not WeaponType.Grenade and not WeaponType.GrenadeSecondary)
                {
                    player.weapon.SwitchEquipped(weapon.Type, force: true);
                    _sm.ammoHUD.SetAmmo(player.weapon.Magazine[weapon.GetNetName],
                        player.weapon.LeftAmmo[weapon.GetNetName],
                        isTertiary: weapon.Type is WeaponType.Tertiary);
                }
            }
        }

        /// <summary>
        /// Take damage
        /// </summary>
        /// <param name="damage"> The damage amount</param>
        /// <returns> Whenever the prop was destroyed.</returns>
        public bool Damage(uint damage, bool explode)
        {
            Hp -= damage;
            if (Hp <= 0)
                DestroyProp(explode);
            return Hp <= 0;
        }
    }
}