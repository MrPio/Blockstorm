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
using UnityEngine.UI;
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
        [SerializeField] private GameObject missile;

        [CanBeNull] private Model.Weapon _weaponModel;
        private AudioClip _fireClip;
        private float _lastSwitch = -99;
        [NonSerialized] public static bool isAiming;
        public readonly Dictionary<string, int> LeftAmmo = new();
        public readonly Dictionary<string, int> Magazine = new();
        [CanBeNull] private Coroutine _reloadingCoroutine;
        private WeaponType _lastWeapon = WeaponType.Primary;
        [CanBeNull] private Animator weaponAnimator;

        [CanBeNull]
        public Model.Weapon WeaponModel
        {
            get => _weaponModel;
            private set
            {
                _weaponModel = value;
                if (value == null) return;
                _fireClip = Resources.Load<AudioClip>(value.GetAudioClip);
                if (value.Type == WeaponType.Block)
                {
                    cameraMovement.CanPlace = true;
                    if (!Magazine.ContainsKey(_weaponModel.GetNetName))
                        Magazine[_weaponModel.GetNetName] = _weaponModel.Magazine!.Value;
                }
                else if (value.Type == WeaponType.Melee)
                    cameraMovement.CanDig = true;
                else
                {
                    cameraMovement.CanPlace = false;
                    cameraMovement.CanDig = false;
                    if (!LeftAmmo.ContainsKey(_weaponModel.GetNetName))
                        LeftAmmo[_weaponModel.GetNetName] = _weaponModel.Ammo!.Value;
                    if (!Magazine.ContainsKey(_weaponModel.GetNetName))
                        Magazine[_weaponModel.GetNetName] = _weaponModel.Magazine!.Value;
                }
            }
        }

        public void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            isAiming = false;
            Debug.Log($"[Player.Weapon::Start()] Equipping block");
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

            // Play audio effect and crosshair/scope animation
            // player.LastShot.Value = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (_weaponModel.Type is WeaponType.Block or WeaponType.Melee || Magazine[_weaponModel.GetNetName] > 0)
            {
                // Propagate the sound across the net
                player.LastShotWeapon.Value = "";
                player.LastShotWeapon.Value = _weaponModel.Name.ToUpper();
                audioSource.PlayOneShot(_fireClip, 0.65f);
                animator.SetTrigger(Animator.StringToHash($"fire_{_weaponModel.FireAnimation}"));
                weaponAnimator?.SetTrigger(Animator.StringToHash("fire"));

                if (_weaponModel.HasScope && isAiming)
                    _sm.scopeFire.Animate(_weaponModel.Delay, math.clamp(_weaponModel.Damage / 30f, 0.75f, 2.75f));
                else
                    _sm.crosshairFire.Animate(_weaponModel.Delay * 0.925f,
                        math.clamp(_weaponModel.Damage / 30f, 0.75f, 2.5f));
            }

            // Handle ammo
            if (_weaponModel.Type is WeaponType.Block || _weaponModel.IsGun)
            {
                // Check if the player has enough ammo
                if (Magazine[_weaponModel.GetNetName] <= 0)
                {
                    audioSource.PlayOneShot(noAmmoClip);
                    return;
                }

                // Subtract ammo
                Magazine[_weaponModel.GetNetName]--;
                if (_weaponModel.Type is WeaponType.Block)
                    _sm.ammoHUD.SetBlocks(Magazine[_weaponModel.GetNetName]);
                else
                    _sm.ammoHUD.SetAmmo(Magazine[_weaponModel.GetNetName],
                        isTertiary: _weaponModel.Type is WeaponType.Tertiary);
            }

            // Add block to map
            if (_weaponModel.Type is WeaponType.Block)
            {
                _sm.clientManager.EditVoxelClientRpc(new[] { _sm.placeBlock.transform.position },
                    player.Status.Value.BlockId(player.Team));
                return;
            }

            // Spawn the weapon effect
            if (_weaponModel.IsGun && !_weaponModel.HasScope)
                player.SpawnWeaponEffect(_weaponModel!.Type);

            if (_weaponModel.Type is WeaponType.Tertiary)
            {
                _sm.serverManager.SpawnExplosiveServerRpc(
                    missile.name,
                    mainCamera.transform.position + mainCamera.transform.forward * 0.5f,
                    mainCamera.transform.rotation.eulerAngles,
                    mainCamera.transform.forward,
                    _weaponModel.Damage,
                    _weaponModel.ExplosionTime!.Value,
                    _weaponModel.ExplosionRange!.Value
                );

                // Switch to the previous weapon if ran out of ammo
                if (LeftAmmo[_weaponModel.GetNetName] <= 0)
                    SwitchEquipped(_lastWeapon);
                else
                    SwitchEquipped(WeaponType.Tertiary, silent: true);
                return;
            }

            // Cast a ray to check for collisions
            var cameraTransform = cameraMovement.transform;
            var ray = new Ray(cameraTransform.position + cameraTransform.forward * 0.45f, cameraTransform.forward);

            // Checks if there was a hit on an enemy
            var hasHitEnemy = false;
            if (Physics.Raycast(ray, out var hit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Enemy")))
                if (hit.collider is not null)
                {
                    var attackedPlayer = hit.transform.GetComponentInParent<Player>();
                    var multiplier = Model.Weapon.BodyPartMultipliers[hit.transform.gameObject.name];
                    var distance = Vector3.Distance(player.transform.position, hit.collider.transform.position);
                    var distanceFactor =
                        math.clamp((1f - distance / _weaponModel.Distance) * 2, 0.25f, 1f); // 1f ---> 0.25f
                    var helmetHit = hit.transform.gameObject.name == "Head" && attackedPlayer.Status.Value.HasHelmet;

                    var damage = (uint)(_weaponModel.Damage * multiplier *
                                        (_weaponModel.Distance < 100 ? distanceFactor : 1f) * (helmetHit ? 0.5f : 1f));

                    // Spawn blood effect on the enemy
                    Instantiate(hit.transform.gameObject.name.ToLower() == "head" ? headBlood : bodyBlood,
                        hit.point + VectorExtensions.RandomVector3(-0.15f, 0.15f) - cameraTransform.forward * 0.1f,
                        Quaternion.FromToRotation(Vector3.up, -cameraTransform.forward) *
                        Quaternion.Euler(0, Random.Range(-180, 180), 0));

                    if (!attackedPlayer.Status.Value.IsDead)
                    {
                        // Check if the enemy is not allied
                        if (attackedPlayer.IsOwner ||
                            attackedPlayer.Team != player.Team)
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
                                text.fontSize += distance > 10f ? distance / 20f + 0.5f : 1f;
                            });
                            damageTextGo.transform.localScale = Vector3.one * math.sqrt(multiplier);

                            // Send the damage to the enemy
                            attackedPlayer.DamageClientRpc(damage, hit.transform.gameObject.name,
                                new NetVector3(cameraTransform.forward),
                                player.OwnerClientId);
                        }
                    }

                    // Prevents damaging the ground
                    hasHitEnemy = true;
                }

            // Checks if there was a hit on the ground
            if (!hasHitEnemy &&
                Physics.Raycast(ray, out hit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Ground")))
                if (hit.collider is not null)
                {
                    // Check if the hit block is solid
                    var pos = Vector3Int.FloorToInt(hit.point + cameraTransform.forward * 0.05f);
                    var block = _sm.worldManager.GetVoxel(pos);
                    if (block is not { isSolid: true }) return;

                    // Spawn damage effect on the block
                    _sm.blockDigEffect.transform.position = pos + Vector3.one * 0.5f;
                    _sm.blockDigEffect.GetComponent<Renderer>().material = Resources.Load<Material>(block.GetMaterial);
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
                _sm.serverManager.SpawnExplosiveServerRpc(
                    newStatus.Grenade!.Name.ToUpper(),
                    mainCamera.transform.position + mainCamera.transform.forward * 0.75f + Vector3.down * 0.2f,
                    VectorExtensions.RandomVector3(-180, 180f),
                    mainCamera.transform.forward,
                    newStatus.Grenade!.Damage,
                    newStatus.Grenade!.ExplosionTime!.Value,
                    newStatus.Grenade!.ExplosionRange!.Value,
                    force
                );
            }
        }

        /// <summary>
        /// Switch the currently selected weapon.
        /// </summary>
        /// <param name="weaponType"> The weapon to equip. </param>
        public void SwitchEquipped(WeaponType weaponType, bool silent = false)
        {
            // Make sure the weapon is not switching too fast.
            if (Time.time - _lastSwitch < 0.25f)
                return;
            _lastSwitch = Time.time;
            if (_reloadingCoroutine is not null)
            {
                StopCoroutine(_reloadingCoroutine);
                _sm.reloadBar.Stop();
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
            if (newWeapon.Type is not WeaponType.Grenade and not WeaponType.Tertiary)
                _lastWeapon = newWeapon.Type;

            // Update Ammo HUD
            if (weaponType is WeaponType.Block)
                _sm.ammoHUD
                    .SetBlocks(Magazine[_weaponModel!.GetNetName]);
            else if (weaponType is WeaponType.Melee)
                _sm.ammoHUD.SetMelee();
            else
                _sm.ammoHUD
                    .SetAmmo(Magazine[_weaponModel!.GetNetName], LeftAmmo[_weaponModel!.GetNetName],
                        weaponType is WeaponType.Tertiary);


            // Play sound and animation
            if (!silent)
                audioSource.PlayOneShot(switchEquippedClip);
            animator.SetTrigger(Animator.StringToHash("inventory_switch"));

            // Broadcast the new equipment
            player.EquippedWeapon.Value = $"{WeaponModel!.Name}:{WeaponModel!.Variant}";
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
                var go = Resources.Load<GameObject>(WeaponModel!.GetPrefab());
                player.WeaponPrefab = Instantiate(go, transform).Apply(o =>
                {
                    // Load the weapon material and the scope
                    foreach (var mesh in o.GetComponentsInChildren<MeshRenderer>(true))
                        if (mesh.gameObject.name.Contains("scope"))
                            mesh.gameObject.SetActive(_weaponModel!.Scope == mesh.gameObject.name);
                        else if (WeaponModel!.Variant is not null)
                            mesh.material = Resources.Load<Material>(WeaponModel.GetMaterial);
                    o.layer = LayerMask.NameToLayer("WeaponCamera");
                    o.AddComponent<WeaponSway>();
                    if (WeaponModel.Type == WeaponType.Block)
                        o.GetComponent<MeshRenderer>().material =
                            Resources.Load<Material>(player.Status.Value.BlockType(player.Team).GetMaterial);
                });
                weaponAnimator = player.WeaponPrefab.GetComponentInChildren<Animator>();
            }
        }

        /// <summary>
        /// Switch between aim and non-aim mode.
        /// </summary>
        public void ToggleAim()
        {
            if (_reloadingCoroutine is not null)
                return;

            isAiming = !isAiming;

            // Disable the crosshair
            _sm.crosshair.gameObject.SetActive(!isAiming);

            if (_weaponModel!.HasScope)
            {
                _sm.scopeContainer.SetActive(isAiming);
                if (isAiming)
                    _sm.scopeContainer.transform.Find("Scope").GetComponent<Image>().sprite =
                        Resources.Load<Sprite>(_weaponModel!.GetScope);
                foreach (var child in transform.parent.GetComponentsInChildren<Transform>(true)
                             .Where(it => it != transform && it != transform.parent))
                    child.gameObject.SetActive(!isAiming);
            }
            else
            {
                // Destroy the current weapon prefab
                foreach (var child in transform.parent.GetComponentsInChildren<Transform>()
                             .Where(it => it != transform && it != transform.parent))
                    Destroy(child.gameObject);

                var prefab = Resources.Load<GameObject>(WeaponModel!.GetPrefab(aiming: isAiming));
                player.WeaponPrefab = Instantiate(prefab, isAiming ? transform.parent : transform).Apply(go =>
                {
                    // Load the weapon material and the scope
                    var hasDot = false;
                    foreach (var mesh in go.GetComponentsInChildren<MeshRenderer>(true))
                        if (mesh.gameObject.name.Contains("scope"))
                        {
                            var isRightScope = _weaponModel.Scope == mesh.gameObject.name;
                            mesh.gameObject.SetActive(isRightScope);
                            if (isRightScope)
                                hasDot = mesh.transform.childCount > 0;
                        }
                        else if (WeaponModel!.Variant is not null)
                            mesh.material = Resources.Load<Material>(WeaponModel.GetMaterial);

                    // Hide weapon mesh when aiming with a red dot weapon
                    if (isAiming && hasDot)
                        go.GetComponent<MeshRenderer>().enabled = false;

                    go.layer = LayerMask.NameToLayer("WeaponCamera");
                    go.AddComponent<WeaponSway>();
                });
                weaponAnimator = player.WeaponPrefab.GetComponentInChildren<Animator>();
            }

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

            // Disable any aiming
            if (isAiming)
                ToggleAim();

            var leftAmmo = LeftAmmo[_weaponModel!.GetNetName];
            if (leftAmmo <= 0) return;

            // Play audio clip and animation
            audioSource.PlayOneShot(reloadingClip);
            animator.SetTrigger(Animator.StringToHash("inventory_switch"));
            _sm.reloadBar.Reload(_weaponModel!.ReloadTime!.Value / 100f);

            // Make the animation last for the entire reloading time.
            _reloadingCoroutine = StartCoroutine(BringWeaponUp());
            return;

            IEnumerator BringWeaponUp()
            {
                yield return new WaitForSeconds(_weaponModel!.ReloadTime!.Value / 100f);

                // Update the magazine and notify the HUD
                var takenAmmo = math.min(leftAmmo, _weaponModel.Magazine!.Value - Magazine[_weaponModel!.GetNetName]);
                LeftAmmo[_weaponModel!.GetNetName] -= takenAmmo;
                Magazine[_weaponModel!.GetNetName] += takenAmmo;
                _sm.ammoHUD.SetAmmo(Magazine[_weaponModel.GetNetName], LeftAmmo[_weaponModel!.GetNetName],
                    isTertiary: _weaponModel.Type is WeaponType.Tertiary);

                // Unpause the animator
                animator.speed = 1;
                _reloadingCoroutine = null;
            }
        }
    }
}