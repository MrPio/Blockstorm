using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using JetBrains.Annotations;
using Managers;
using Model;
using Network;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;
using Random = UnityEngine.Random;

namespace Prefabs.Player
{
    /// <summary>
    /// Handle the player's weapon.
    /// </summary>
    /// <remarks> This script is attached only to the owned player and removed from the enemy players. </remarks>
    public class Weapon : MonoBehaviour
    {
        [Header("Components")] [SerializeField]
        private AudioClip switchEquippedClip;

        [SerializeField] public AudioSource audioSource;
        [SerializeField] public Animator animator;
        [SerializeField] private CameraMovement cameraMovement;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera weaponCamera;
        [SerializeField] public GameObject RightArm;

        [Header("AudioClips")] [SerializeField]
        private AudioClip blockDamageLightClip;

        [SerializeField] private AudioClip blockDamageMediumClip;
        [SerializeField] private AudioClip noBlockDamageClip;

        [Header("Prefabs")] [SerializeField] private GameObject bodyBlood;
        [SerializeField] private GameObject headBlood;
        [SerializeField] private Player player;
        [SerializeField] private GameObject damageText;

        [CanBeNull] private Model.Weapon _weaponModel;
        private AudioClip _fireClip;
        private float _lastSwitch = -99;
        private ParticleSystem _blockDigEffect;
        private WorldManager _wm;
        [NonSerialized] public static bool isAiming;
        private Transform _crosshair;
        private Animator _crosshairAnimator;
        private ClientManager _clientManager;
        private Canvas worldCanvas;

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
                    cameraMovement.CanPlace = true;
                else if (value.Type == WeaponType.Melee)
                    cameraMovement.CanDig = true;
                else
                {
                    cameraMovement.CanPlace = false;
                    cameraMovement.CanDig = false;
                }
            }
        }

        private void Awake()
        {
            _blockDigEffect = GameObject.FindWithTag("BlockDigEffect").GetComponent<ParticleSystem>();
            _crosshair = GameObject.FindWithTag("Crosshair").transform;
            _crosshairAnimator = _crosshair.Find("CrosshairLines").GetComponent<Animator>();
            _wm = GameObject.FindWithTag("WorldManager").GetComponent<WorldManager>();
            _clientManager = GameObject.FindWithTag("ClientServerManagers").GetComponentInChildren<ClientManager>();
            worldCanvas = GameObject.FindWithTag("WorldCanvas").GetComponent<Canvas>();
        }

        private void Start()
        {
            isAiming = false;
            SwitchEquipped(WeaponType.Block);
        }

        /// <summary>
        /// Fire the current weapon and check if any enemy or ground block has been hit.
        /// </summary>
        public void Fire()
        {
            if (_weaponModel == null) return;

            // Play audio effect and animation.
            player.LastShot.Value = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            audioSource.PlayOneShot(_fireClip, 0.5f);
            animator.SetTrigger(Animator.StringToHash($"fire_{_weaponModel.FireAnimation}"));
            _crosshairAnimator.SetTrigger(Animator.StringToHash("fire"));

            // Check that the current weapon deals damage
            // TODO: Melee weapons also deals damages! Move here the block digging logic and add enemy damage.
            if (_weaponModel.Type is WeaponType.Block or WeaponType.Melee) return;

            // Spawn the weapon effect
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

                    // Spawn the damage text
                    // GameObject.FindGameObjectsWithTag() TODO: after moving HP to Player script, use it to check if the player is already dead
                    {
                        var damageTextGo = Instantiate(damageText, worldCanvas.transform);
                        damageTextGo.transform.position = hit.point + VectorExtensions.RandomVector3(-0.15f, 0.15f) -
                                                          cameraTransform.forward * 0.35f;
                        damageTextGo.transform.rotation = player.transform.rotation;
                        damageTextGo.GetComponent<FollowRotation>().follow = player.transform;
                        damageTextGo.GetComponentInChildren<TextMeshProUGUI>().Apply(text =>
                        {
                            text.text = damage.ToString();
                            text.color = Color.Lerp(Color.white, Color.red, multiplier - 0.5f);
                        });
                        damageTextGo.transform.localScale = Vector3.one * math.sqrt(multiplier);
                    }

                    // Send the damage to the enemy
                    var attackedPlayer = hit.transform.GetComponentInParent<Player>();
                    attackedPlayer.DamageClientRpc(damage, hit.transform.gameObject.name,
                        new NetVector3(cameraTransform.forward),
                        player.OwnerClientId);

                    // Prevents damaging the ground
                    return;
                }

            // Checks if there was a hit on the ground
            if (Physics.Raycast(ray, out hit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Ground")))
                if (hit.collider is not null)
                {
                    // Check if the hit block is solid
                    var pos = Vector3Int.FloorToInt(hit.point + cameraTransform.forward * 0.05f);
                    var blockType = _wm.GetVoxel(pos);
                    if (blockType is not { isSolid: true }) return;

                    // Spawn damage effect on the block
                    _blockDigEffect.transform.position = pos + Vector3.one * 0.5f;
                    _blockDigEffect.GetComponent<Renderer>().material =
                        Resources.Load<Material>(
                            $"Textures/texturepacks/blockade/Materials/blockade_{blockType.topID + 1:D1}");
                    _blockDigEffect.Play();

                    // Play the audio effect
                    if (new List<string> { "crate", "crate", "window", "hay", "barrel", "log" }.Any(it =>
                            blockType.name.Contains(it)))
                        audioSource.PlayOneShot(blockDamageLightClip, 1);
                    else if (blockType.blockHealth == BlockHealth.Indestructible)
                        audioSource.PlayOneShot(noBlockDamageClip, 1);
                    else
                        audioSource.PlayOneShot(blockDamageMediumClip, 1);

                    // Broadcast the damage action
                    _clientManager.DamageVoxelRpc(pos, _weaponModel.Damage);
                }
        }

        /// <summary>
        /// Throw a grenade with a certain strength amount
        /// </summary>
        /// <param name="force"> A normalised value of throwing strength</param>
        public void ThrowGrenade(float force)
        {
            InventoryManager.Instance.LeftGrenades--;
            StartCoroutine(Throw());
            return;

            IEnumerator Throw()
            {
                animator.SetTrigger(Animator.StringToHash("inventory_switch"));
                RightArm.SetActive(true);
                yield return new WaitForSeconds(0.35f);
                var grenade =
                    Resources.Load<GameObject>(
                        $"Prefabs/weapons/grenade/{InventoryManager.Instance.Grenade!.Name.ToUpper()}");
                var go = Instantiate(grenade,
                    mainCamera.transform.position + mainCamera.transform.forward * 0.5f + Vector3.down * 0.2f,
                    Quaternion.Euler(VectorExtensions.RandomVector3(-180, 180f)));
                go.GetComponent<NetworkObject>().Spawn();
                var rb = go.GetComponent<Rigidbody>();
                rb.AddForce(mainCamera.transform.forward * math.clamp(5f * force, 1.5f, 5f),
                    ForceMode.Impulse);
                rb.angularVelocity = VectorExtensions.RandomVector3(-60f, 60f);
                go.GetComponent<Grenade>().Delay = force;
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

            // Disable any aiming
            if (isAiming)
                ToggleAim();

            // Find the new weapon in the player's inventory
            WeaponModel = weaponType switch
            {
                WeaponType.Block => InventoryManager.Instance.Block,
                WeaponType.Melee => InventoryManager.Instance.Melee,
                WeaponType.Primary => InventoryManager.Instance.Primary,
                WeaponType.Secondary => InventoryManager.Instance.Secondary,
                WeaponType.Tertiary => InventoryManager.Instance.Tertiary,
                _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
            };

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
            foreach (var child in transform.GetComponentsInChildren<Transform>().Where(it => it != transform))
                Destroy(child.gameObject);
            var go = Resources.Load<GameObject>($"Prefabs/weapons/{WeaponModel!.Name.ToUpper()}");
            player.WeaponPrefab = Instantiate(go, transform).Apply(o =>
            {
                o.layer = LayerMask.NameToLayer("WeaponCamera");
                o.AddComponent<WeaponSway>();
                if (WeaponModel.Type == WeaponType.Block)
                    o.GetComponent<MeshRenderer>().material = Resources.Load<Material>(
                        $"Textures/texturepacks/blockade/Materials/blockade_{(InventoryManager.Instance.BlockType.sideID + 1):D1}");
            });
        }

        /// <summary>
        /// Switch between aim and non-aim mode.
        /// </summary>
        public void ToggleAim()
        {
            isAiming = !isAiming;

            // Disable the crosshair
            _crosshair.gameObject.SetActive(!isAiming);

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
    }
}