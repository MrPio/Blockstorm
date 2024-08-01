using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using JetBrains.Annotations;
using Managers;
using Model;
using Network;
using Partials;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Prefabs.Player
{
    /// <summary>
    /// Handle the player's weapon.
    /// </summary>
    /// <remarks> This script is attached only to the owned player and removed from the enemy players. </remarks>
    public class Weapon : MonoBehaviour
    {
        private SceneManager _sm;

        [Header("Components")] [SerializeField]
        private AudioClip switchEquippedClip;

        [SerializeField] public AudioSource audioSource;
        [SerializeField] public Animator animator;
        [SerializeField] private CameraMovement cameraMovement;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera weaponCamera;
        [SerializeField] public GameObject rightArm;

        [Header("AudioClips")] [SerializeField]
        private AudioClip blockDamageLightClip;

        [SerializeField] private AudioClip blockDamageMediumClip;
        [SerializeField] private AudioClip noBlockDamageClip;
        [SerializeField] public AudioClip noAmmoClip;
        [SerializeField] private AudioClip reloadingClip;

        [Header("Prefabs")] [SerializeField] private GameObject bodyBlood;
        [SerializeField] private GameObject headBlood;
        [SerializeField] private Player player;
        [SerializeField] private GameObject damageText;

        [CanBeNull] private Model.Weapon _weaponModel;
        private AudioClip _fireClip;
        private float _lastSwitch = -99;
        [NonSerialized] public static bool isAiming;
        public readonly Dictionary<string, int> LeftAmmo = new();
        public readonly Dictionary<string, int> Magazine = new();
        [CanBeNull] private Coroutine _reloadingCoroutine;

        [CanBeNull]
        public Model.Weapon WeaponModel
        {
            get => _weaponModel;
            private set
            {
                _weaponModel = value;
                if (value == null) return;
                _fireClip = Resources.Load<AudioClip>($"Audio/weapons/{value.Audio.ToUpper()}");
                if (value.Type == WeaponType.Block)
                {
                    cameraMovement.CanPlace = true;
                    if (!Magazine.ContainsKey(_weaponModel.Name))
                        Magazine[_weaponModel.Name] = _weaponModel.Magazine!.Value;
                }
                else if (value.Type == WeaponType.Melee)
                    cameraMovement.CanDig = true;
                else
                {
                    cameraMovement.CanPlace = false;
                    cameraMovement.CanDig = false;
                    if (!LeftAmmo.ContainsKey(_weaponModel.Name))
                        LeftAmmo[_weaponModel.Name] = _weaponModel.Ammo!.Value;
                    if (!Magazine.ContainsKey(_weaponModel.Name))
                        Magazine[_weaponModel.Name] = _weaponModel.Magazine!.Value;
                }
            }
        }

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            isAiming = false;
            SwitchEquipped(WeaponType.Block);
        }

        /// <summary>
        /// Fire the current weapon and check if any enemy or ground block has been hit.
        /// This is called from CameraMovement for Block, Melee and Guns
        /// </summary>
        /// <seealso cref="CameraMovement"/>
        public void Fire()
        {
            if (_weaponModel == null || Time.time - _lastSwitch < 0.25f || _reloadingCoroutine is not null)
                return;

            // Play audio effect and animation.
            player.LastShot.Value = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (_weaponModel.Type is WeaponType.Block or WeaponType.Melee || Magazine[_weaponModel.Name] > 0)
            {
                audioSource.PlayOneShot(_fireClip, 0.5f);
                animator.SetTrigger(Animator.StringToHash($"fire_{_weaponModel.FireAnimation}"));
                _sm.crosshairAnimator.SetTrigger(Animator.StringToHash("fire"));
            }

            // Handle ammo
            if (_weaponModel.Type is WeaponType.Block || _weaponModel.IsGun)
            {
                // Check if the player has enough ammo
                if (Magazine[_weaponModel.Name] <= 0)
                {
                    audioSource.PlayOneShot(noAmmoClip);
                    return;
                }

                // Subtract ammo
                Magazine[_weaponModel.Name]--;
                _sm.ammoHUD.SetAmmo(Magazine[_weaponModel.Name]);
            }

            // Add block to map
            if (_weaponModel.Type is WeaponType.Block)
            {
                _sm.clientManager.EditVoxelClientRpc(new[] { _sm.placeBlock.transform.position },
                    player.Status.Value.BlockId);
                return;
            }
            
            // Spawn the weapon effect
            if (_weaponModel.IsGun)
                player.SpawnWeaponEffect(_weaponModel!.Type);

            // Cast a ray to check for collisions
            var cameraTransform = cameraMovement.transform;
            var ray = new Ray(cameraTransform.position + cameraTransform.forward * 0.45f, cameraTransform.forward);

            // Checks if there was a hit on an enemy
            if (Physics.Raycast(ray, out var hit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Enemy")))
                if (hit.collider is not null)
                {
                    var multiplier = Model.Weapon.BodyPartMultipliers[hit.transform.gameObject.name];
                    var damage = (uint)(_weaponModel.Damage * multiplier);

                    // Spawn blood effect on the enemy
                    Instantiate(hit.transform.gameObject.name.ToLower() == "head" ? headBlood : bodyBlood,
                        hit.point + VectorExtensions.RandomVector3(-0.15f, 0.15f),
                        Quaternion.FromToRotation(Vector3.up, -cameraTransform.forward) *
                        Quaternion.Euler(0, Random.Range(-180, 180), 0));

                    var attackedPlayer = hit.transform.GetComponentInParent<Player>();
                    if (!attackedPlayer.Status.Value.IsDead)
                    {
                        // Spawn the damage text
                        var damageTextGo = Instantiate(damageText, _sm.worldCanvas.transform);
                        damageTextGo.transform.position =
                            hit.point + VectorExtensions.RandomVector3(-0.15f, 0.15f) -
                            cameraTransform.forward * 0.35f;
                        damageTextGo.transform.rotation = player.transform.rotation;
                        damageTextGo.GetComponent<FollowRotation>().follow = player.transform;
                        damageTextGo.GetComponentInChildren<TextMeshProUGUI>().Apply(text =>
                        {
                            text.text = damage.ToString();
                            text.color = Color.Lerp(Color.white, Color.red, multiplier - 0.5f);
                        });
                        damageTextGo.transform.localScale = Vector3.one * math.sqrt(multiplier);

                        // Send the damage to the enemy
                        attackedPlayer.DamageClientRpc(damage, hit.transform.gameObject.name,
                            new NetVector3(cameraTransform.forward),
                            player.OwnerClientId);
                    }

                    // Prevents damaging the ground
                    return;
                }

            // Checks if there was a hit on the ground
            if (Physics.Raycast(ray, out hit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Ground")))
                if (hit.collider is not null)
                {
                    // Check if the hit block is solid
                    var pos = Vector3Int.FloorToInt(hit.point + cameraTransform.forward * 0.05f);
                    var block = _sm.worldManager.GetVoxel(pos);
                    if (block is not { isSolid: true }) return;

                    // Spawn damage effect on the block
                    _sm.blockDigEffect.transform.position = pos + Vector3.one * 0.5f;
                    _sm.blockDigEffect.GetComponent<Renderer>().material =
                        Resources.Load<Material>(
                            $"Textures/texturepacks/blockade/Materials/blockade_{block.topID + 1:D1}");
                    _sm.blockDigEffect.Play();
                    audioSource.PlayOneShot(Resources.Load<AudioClip>($"Audio/blocks/{block.AudioClip}"));

                    // Broadcast the damage action
                    _sm.clientManager.DamageVoxelRpc(pos, _weaponModel.Damage);
                }
        }

        /// <summary>
        /// Throw a grenade with a certain strength amount
        /// </summary>
        /// <param name="force"> A normalised value of throwing strength</param>
        public void ThrowGrenade(float force)
        {
            var newStatus = player.Status.Value;
            newStatus.LeftGrenades--;
            player.Status.Value = newStatus;

            StartCoroutine(Throw());
            return;

            IEnumerator Throw()
            {
                animator.SetTrigger(Animator.StringToHash("inventory_switch"));
                rightArm.SetActive(true);
                yield return new WaitForSeconds(0.35f);
                var grenade =
                    Resources.Load<GameObject>(
                        $"Prefabs/weapons/grenade/{newStatus.Grenade!.Name.ToUpper()}");
                var go = Instantiate(grenade,
                    mainCamera.transform.position + mainCamera.transform.forward * 0.5f + Vector3.down * 0.2f,
                    Quaternion.Euler(VectorExtensions.RandomVector3(-180, 180f)));
                go.GetComponent<NetworkObject>().Spawn();
                var rb = go.GetComponent<Rigidbody>();
                rb.AddForce(mainCamera.transform.forward * math.clamp(6.5f * force, 2.25f, 6.5f),
                    ForceMode.Impulse);
                rb.angularVelocity = VectorExtensions.RandomVector3(-60f, 60f);
                go.GetComponent<Grenade>().Apply(it =>
                {
                    it.Delay = force;
                    it.ExplosionTime = newStatus.Grenade!.ExplosionTime!.Value;
                    it.ExplosionRange = newStatus.Grenade!.ExplosionRange!.Value;
                });
            }
        }

        /// <summary>
        /// Switch the currently selected weapon.
        /// </summary>
        /// <param name="weaponType"> The weapon to equip. </param>
        public void SwitchEquipped(WeaponType weaponType)
        {
            // Make sure the weapon is not switching too fast.
            if (Time.time - _lastSwitch < 0.25f)
                return;
            _lastSwitch = Time.time;
            if (_reloadingCoroutine is not null)
            {
                StopCoroutine(_reloadingCoroutine);
                _sm.ReloadBar.Stop();
                _reloadingCoroutine = null;
                animator.speed = 1;
            }

            // Disable any aiming
            if (isAiming)
                ToggleAim();

            // Find the new weapon in the player's inventory
            var status = player.Status.Value;
            var newWeapon = weaponType switch
            {
                WeaponType.Block => status.Block,
                WeaponType.Melee => status.Melee,
                WeaponType.Primary => status.Primary,
                WeaponType.Secondary => status.Secondary,
                WeaponType.Tertiary => status.Tertiary,
                _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
            };
            if (newWeapon is null)
                return;
            WeaponModel = newWeapon;

            // Update Ammo HUD
            if (weaponType is WeaponType.Block)
                _sm.ammoHUD
                    .SetBlocks(Magazine[_weaponModel!.Name]);
            else if (weaponType is WeaponType.Melee)
                _sm.ammoHUD.SetMelee();
            else
                _sm.ammoHUD
                    .SetAmmo(Magazine[_weaponModel!.Name], LeftAmmo[_weaponModel!.Name]);


            // Play sound and animation
            audioSource.PlayOneShot(switchEquippedClip);
            animator.SetTrigger(Animator.StringToHash("inventory_switch"));

            // Broadcast the new equipment
            player.EquippedWeapon.Value = new NetString { Message = WeaponModel!.Name };
        }

        /// <summary>
        /// Load the new equipped weapon prefab.
        /// </summary>
        /// <remarks> This is called in the middle of the <b>inventory_switch</b> animation </remarks>
        public void ChangeWeaponPrefab()
        {
            if (_reloadingCoroutine is not null)
                animator.speed = 0;
            else
            {
                // Load the new weapon prefab
                foreach (var child in transform.GetComponentsInChildren<Transform>().Where(it => it != transform))
                    Destroy(child.gameObject);
                var go = Resources.Load<GameObject>($"Prefabs/weapons/{WeaponModel!.Name.ToUpper()}");
                player.WeaponPrefab = Instantiate(go, transform).Apply(o =>
                {
                    o.layer = LayerMask.NameToLayer("WeaponCamera");
                    o.AddComponent<WeaponSway>();
                    if (WeaponModel.Type == WeaponType.Block)
                        o.GetComponent<MeshRenderer>().material = Resources.Load<Material>(
                            $"Textures/texturepacks/blockade/Materials/blockade_{(player.Status.Value.BlockType.sideID + 1):D1}");
                });
            }
        }

        /// <summary>
        /// Switch between aim and non-aim mode.
        /// </summary>
        public void ToggleAim()
        {
            isAiming = !isAiming;

            // Disable the crosshair
            _sm.crosshair.gameObject.SetActive(!isAiming);

            // Destroy the current weapon prefab
            foreach (var child in transform.parent.GetComponentsInChildren<Transform>()
                         .Where(it => it != transform && it != transform.parent))
                Destroy(child.gameObject);

            var go = Resources.Load<GameObject>(
                $"Prefabs/weapons/{WeaponModel!.Name.ToUpper()}" + (isAiming ? "_aim" : ""));
            player.WeaponPrefab = Instantiate(go, isAiming ? transform.parent : transform).Apply(o =>
            {
                o.layer = LayerMask.NameToLayer("WeaponCamera");
                o.AddComponent<WeaponSway>();
            });

            // Set the camera zoom according to the weapon scope
            mainCamera.fieldOfView = CameraMovement.FOVMain / (isAiming ? _weaponModel!.Zoom : 1);
            weaponCamera.fieldOfView = CameraMovement.FOVWeapon / (isAiming ? _weaponModel!.Zoom : 1);
        }

        /// <summary>
        /// Reload the current weapon if it is a gun.
        /// The weapon's magazine will be restored by subtracting the left ammo and the HUD will be updated.
        /// </summary>
        /// <remarks> While reloading, if the player switches weapons, the reload will be canceled. </remarks>
        public void Reload()
        {
            if (_reloadingCoroutine is not null || _weaponModel is null ||
                _weaponModel.Type is WeaponType.Block or WeaponType.Melee)
                return;
            var leftAmmo = LeftAmmo[_weaponModel!.Name];
            if (leftAmmo <= 0) return;

            // Play audio clip and animation
            audioSource.PlayOneShot(reloadingClip);
            animator.SetTrigger(Animator.StringToHash("inventory_switch"));
            _sm.ReloadBar.Reload(_weaponModel!.ReloadTime!.Value / 100f);

            // Make the animation last for the entire reloading time.
            _reloadingCoroutine = StartCoroutine(BringWeaponUp());
            return;

            IEnumerator BringWeaponUp()
            {
                yield return new WaitForSeconds(_weaponModel!.ReloadTime!.Value / 100f);

                // Update the magazine and notify the HUD
                var takenAmmo = math.min(leftAmmo, _weaponModel.Magazine!.Value - Magazine[_weaponModel!.Name]);
                LeftAmmo[_weaponModel!.Name] -= takenAmmo;
                Magazine[_weaponModel!.Name] += takenAmmo;
                _sm.ammoHUD.SetAmmo(Magazine[_weaponModel.Name], LeftAmmo[_weaponModel!.Name]);

                // Unpause the animator
                animator.speed = 1;
                _reloadingCoroutine = null;
            }
        }
    }
}