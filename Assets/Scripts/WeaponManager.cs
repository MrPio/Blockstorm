using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using JetBrains.Annotations;
using Managers;
using Model;
using Network;
using UnityEngine;
using VoxelEngine;
using Random = UnityEngine.Random;

// who: Owner
public class WeaponManager : MonoBehaviour
{
    [CanBeNull] private Weapon _weaponModel;
    private AudioClip _fireClip;
    [SerializeField] private AudioClip switchEquippedClip;
    public AudioSource audioSource;
    public Animator animator;
    private float _lastSwitch = -99;
    [SerializeField] private CameraMovement cameraMovement;
    private ParticleSystem _blockDigEffect;
    private WorldManager _wm;
    [SerializeField] private AudioClip blockDamageLightClip, blockDamageMediumClip, noBlockDamageClip;
    [NonSerialized] public static bool isAiming;
    [SerializeField] private Camera mainCamera, weaponCamera;
    private Transform _crosshair;
    [SerializeField] private GameObject bodyBlood, headBlood;
    [SerializeField] private Player player;

    [CanBeNull]
    public Weapon WeaponModel
    {
        get => _weaponModel;
        set
        {
            _weaponModel = value;
            if (value != null)
            {
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
    }

    private void Awake()
    {
        _blockDigEffect = GameObject.FindWithTag("BlockDigEffect").GetComponent<ParticleSystem>();
        _crosshair = GameObject.FindWithTag("Crosshair").transform;
    }

    private void Start()
    {
        isAiming = false;
        _wm = WorldManager.instance;
        SwitchEquipped(WeaponType.Block);
        player.equipped.Value = new Player.Message { message = WeaponModel!.Name };
    }

    public void Fire()
    {
        if (_weaponModel != null)
        {
            player.lastShot.Value = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            audioSource.PlayOneShot(_fireClip, 0.5f);
            animator.SetTrigger(Animator.StringToHash($"fire_{_weaponModel.FireAnimation}"));

            if (_weaponModel.Type != WeaponType.Block && _weaponModel.Type != WeaponType.Melee)
            {
                // Weapon Effect
                player.SpawnWeaponEffect(_weaponModel!.Type);

                var cameraTransform = cameraMovement.transform;
                Ray ray = new Ray(cameraTransform.position + cameraTransform.forward * 0.45f, cameraTransform.forward);
                RaycastHit hit;

                // Enemy hit
                if (Physics.Raycast(ray, out hit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Enemy")))
                    if (hit.collider != null)
                    {
                        Instantiate(hit.transform.gameObject.name == "HEAD" ? headBlood : bodyBlood,
                            hit.point + VectorExtensions.RandomVector3(-0.15f, 0.15f),
                            Quaternion.FromToRotation(Vector3.up, -cameraTransform.forward) *
                            Quaternion.Euler(0, Random.Range(-180, 180), 0));
                        var attackedPlayer = hit.transform.GetComponentInParent<Player>();
                        attackedPlayer.DamageClientRpc(_weaponModel.Damage, player.OwnerClientId);
                        return; // Prevents damaging the ground
                    }

                // Ground hit
                if (Physics.Raycast(ray, out hit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Ground")))
                    if (hit.collider != null)
                    {
                        var pos = Vector3Int.FloorToInt(hit.point + cameraTransform.forward * 0.05f);
                        var blockType = _wm.GetVoxel(pos);
                        if (blockType is { isSolid: true })
                        {
                            _blockDigEffect.transform.position = pos + Vector3.one * 0.5f;
                            _blockDigEffect.GetComponent<Renderer>().material =
                                Resources.Load<Material>(
                                    $"Textures/texturepacks/blockade/Materials/blockade_{(blockType.topID + 1):D1}");
                            _blockDigEffect.Play();
                            if (new List<string>() { "crate", "crate", "window", "hay", "barrel", "log" }.Any(it =>
                                    blockType.name.Contains(it)))
                                audioSource.PlayOneShot(blockDamageLightClip, 1);
                            else if (blockType.blockHealth == BlockHealth.Indestructible)
                                audioSource.PlayOneShot(noBlockDamageClip, 1);
                            else
                                audioSource.PlayOneShot(blockDamageMediumClip, 1);
                            ServerManager.instance.DamageVoxelServerRpc(pos, _weaponModel.Damage);
                        }
                    }
            }
        }
    }

    public void SwitchEquipped(WeaponType weaponType)
    {
        if (Time.time - _lastSwitch < 0.25f)
            return;
        _lastSwitch = Time.time;
        if (isAiming)
            ToggleAim();
        WeaponModel = weaponType switch
        {
            WeaponType.Block => InventoryManager.Instance.Block,
            WeaponType.Melee => InventoryManager.Instance.Melee,
            WeaponType.Primary => InventoryManager.Instance.Primary,
            WeaponType.Secondary => InventoryManager.Instance.Secondary,
            WeaponType.Tertiary => InventoryManager.Instance.Tertiary,
            _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
        };
        audioSource.PlayOneShot(switchEquippedClip);
        animator.SetTrigger(Animator.StringToHash("inventory_switch"));
    }

    // Called by inventory_switch animation
    public void ChangeWeaponPrefab()
    {
        foreach (var child in transform.GetComponentsInChildren<Transform>().Where(it => it != transform))
            Destroy(child.gameObject);
        var go = Resources.Load<GameObject>($"Prefabs/weapons/{WeaponModel!.Name.ToUpper()}");
        player.weaponPrefab = Instantiate(go, transform).Apply(o =>
        {
            o.layer = LayerMask.NameToLayer("WeaponCamera");
            o.AddComponent<WeaponSway>();
            if (WeaponModel.Type == WeaponType.Block)
                o.GetComponent<MeshRenderer>().material = Resources.Load<Material>(
                    $"Textures/texturepacks/blockade/Materials/blockade_{(InventoryManager.Instance.BlockType.sideID + 1):D1}");
        });
    }

    public void ToggleAim()
    {
        isAiming = !isAiming;
        _crosshair.gameObject.SetActive(!isAiming);
        foreach (var child in transform.parent.GetComponentsInChildren<Transform>()
                     .Where(it => it != transform && it != transform.parent))
            Destroy(child.gameObject);
        var go = Resources.Load<GameObject>(
            $"Prefabs/weapons/{WeaponModel!.Name.ToUpper()}" + (isAiming ? "_aim" : ""));
        player.weaponPrefab = Instantiate(go, isAiming ? transform.parent : transform).Apply(o =>
        {
            o.layer = LayerMask.NameToLayer("WeaponCamera");
            o.AddComponent<WeaponSway>();
            mainCamera.fieldOfView = CameraMovement.FOVMain / (isAiming ? _weaponModel!.Zoom : 1);
            weaponCamera.fieldOfView = CameraMovement.FOVWeapon / (isAiming ? _weaponModel!.Zoom : 1);
        });
    }
}