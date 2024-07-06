using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExtensionFunctions;
using JetBrains.Annotations;
using Managers;
using Model;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class WeaponManager : MonoBehaviour
{
    [CanBeNull] private Model.Weapon _weaponModel;

    private AudioClip _fireClip;
    [SerializeField] private AudioClip switchEquippedClip;
    public AudioSource audioSource;
    public Animator animator;
    private float _lastSwitch = -99;
    [SerializeField] private CameraMovement cameraMovement;
    private ParticleSystem blockDigEffect;
    private WorldManager _wm;
    [SerializeField] private AudioClip blockDamageLightClip, blockDamageMediumClip, noBlockDamageClip;
    [NonSerialized] public static bool isAiming;
    [SerializeField] private Camera mainCamera, weaponCamera;
    private Transform crosshair;
    [SerializeField] private List<GameObject> muzzleEffects;
    [SerializeField] private GameObject bodyBlood, headBlood;
    [SerializeField]private Alteruna.Avatar avatar;

    [CanBeNull]
    public Model.Weapon WeaponModel
    {
        get => _weaponModel;
        set
        {
            _weaponModel = value;
            if (value != null)
            {
                _fireClip = Resources.Load<AudioClip>($"Audio/weapons/{value.audio.ToUpper()}");
                if (value.type == WeaponType.Block)
                    cameraMovement.CanPlace = true;
                else if (value.type == WeaponType.Melee)
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
        blockDigEffect = GameObject.FindWithTag("BlockDigEffect").GetComponent<ParticleSystem>();
        crosshair = GameObject.FindWithTag("Crosshair").transform;
    }

    private void Start()
    {
        if(!avatar.IsMe)
            return;
        isAiming = false;
        _wm = WorldManager.instance;
        SwitchEquipped(WeaponType.Block);
    }

    private void Update()
    {
        if(!avatar.IsMe)
            return;
        if (_weaponModel != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && _weaponModel.type != WeaponType.Block)
                SwitchEquipped(WeaponType.Block);
            else if (Input.GetKeyDown(KeyCode.Alpha2) && _weaponModel.type != WeaponType.Melee)
                SwitchEquipped(WeaponType.Melee);
            else if (Input.GetKeyDown(KeyCode.Alpha3) && _weaponModel.type != WeaponType.Primary)
                SwitchEquipped(WeaponType.Primary);
            else if (Input.GetKeyDown(KeyCode.Alpha4) && _weaponModel.type != WeaponType.Secondary)
                SwitchEquipped(WeaponType.Secondary);
            else if (Input.GetKeyDown(KeyCode.Alpha5) && _weaponModel.type != WeaponType.Tertiary)
                SwitchEquipped(WeaponType.Tertiary);

            if (Input.GetMouseButtonDown(1) && _weaponModel.type != WeaponType.Block &&
                _weaponModel.type != WeaponType.Melee)
                ToggleAim();
        }
    }

    public void Fire()
    {
        if (_weaponModel != null)
        {
            audioSource.PlayOneShot(_fireClip, 0.5f);
            animator.SetTrigger(Animator.StringToHash($"fire_{_weaponModel.fireAnimation}"));

            if (_weaponModel.type != WeaponType.Block && _weaponModel.type != WeaponType.Melee)
            {
                if (isAiming)
                    muzzleEffects.RandomItem().Apply(muzzle =>
                    {
                        muzzle.SetActive(true);
                        muzzle.GetComponent<RectTransform>().sizeDelta = Vector2.one * Random.Range(100f, 280f);
                    });

                var cameraTransform = cameraMovement.transform;
                Ray ray = new Ray(cameraTransform.position + cameraTransform.forward * 0.45f, cameraTransform.forward);
                RaycastHit hit;

                // Ground hit
                if (Physics.Raycast(ray, out hit, _weaponModel.distance, ~LayerMask.NameToLayer("Ground")))
                    if (hit.collider != null)
                    {
                        var pos = Vector3Int.FloorToInt(hit.point + cameraTransform.forward * 0.05f);
                        var blockType = _wm.GetVoxel(Vector3Int.FloorToInt(pos));
                        if (blockType is { isSolid: true })
                        {
                            blockDigEffect.transform.position = pos + Vector3.one * 0.5f;
                            blockDigEffect.GetComponent<Renderer>().material =
                                Resources.Load<Material>(
                                    $"Textures/texturepacks/blockade/Materials/blockade_{(blockType.topID + 1):D1}");
                            blockDigEffect.Play();
                            if (new List<string>() { "crate", "crate", "window", "hay", "barrel", "log" }.Any(it =>
                                    blockType.name.Contains(it)))
                                audioSource.PlayOneShot(blockDamageLightClip, 1);
                            else if (blockType.blockHealth == BlockHealth.Indestructible)
                                audioSource.PlayOneShot(noBlockDamageClip, 1);
                            else
                                audioSource.PlayOneShot(blockDamageMediumClip, 1);
                            _wm.DamageBlock(pos, _weaponModel.damage);
                        }
                    }
                // Enemy hit
                if (Physics.Raycast(ray, out hit, _weaponModel.distance, ~LayerMask.NameToLayer("Enemy")))
                    if (hit.collider != null)
                    {
                        
                        print(hit.transform.position.y-hit.point.y);
                        Instantiate(hit.transform.gameObject.name=="HEAD"?headBlood:bodyBlood, hit.point,
                            Quaternion.FromToRotation(Vector3.up,-cameraTransform.forward));
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
            WeaponType.Block => InventoryManager.Instance.block,
            WeaponType.Melee => InventoryManager.Instance.melee,
            WeaponType.Primary => InventoryManager.Instance.primary,
            WeaponType.Secondary => InventoryManager.Instance.secondary,
            WeaponType.Tertiary => InventoryManager.Instance.tertiary,
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
        var go = Resources.Load<GameObject>($"Prefabs/weapons/{WeaponModel!.name.ToUpper()}");
        Instantiate(go, transform).Apply(go =>
        {
            go.layer = LayerMask.NameToLayer("WeaponCamera");
            go.AddComponent<WeaponSway>();
            if (WeaponModel.type == WeaponType.Block)
                go.GetComponent<MeshRenderer>().material = Resources.Load<Material>(
                    $"Textures/texturepacks/blockade/Materials/blockade_{(InventoryManager.Instance.BlockType.sideID + 1):D1}");
        });
    }

    private void ToggleAim()
    {
        isAiming = !isAiming;
        crosshair.gameObject.SetActive(!isAiming);
        foreach (var child in transform.parent.GetComponentsInChildren<Transform>()
                     .Where(it => it != transform && it != transform.parent))
            Destroy(child.gameObject);
        var go = Resources.Load<GameObject>(
            $"Prefabs/weapons/{WeaponModel!.name.ToUpper()}" + (isAiming ? "_aim" : ""));
        Instantiate(go, isAiming ? transform.parent : transform).Apply(it =>
        {
            it.layer = LayerMask.NameToLayer("WeaponCamera");
            it.AddComponent<WeaponSway>();
            mainCamera.fieldOfView = CameraMovement.FOV / (isAiming ? _weaponModel!.zoom : 1);
            weaponCamera.fieldOfView = CameraMovement.FOV / (isAiming ? _weaponModel!.zoom : 1);
        });
    }
}