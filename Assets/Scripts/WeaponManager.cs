using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Managers;
using Model;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [CanBeNull] private Model.Weapon _weaponModel;

    private AudioClip _fireClip;
    [SerializeField] private AudioClip switchEquippedClip;
    public AudioSource audioSource;
    public Animator animator;
    public Transform bulletSpawnPoint;
    public GameObject bullet;
    public float fireRate = 4;
    private float _lastFire;
    private float _fireCooldown;
    [SerializeField] private CameraMovement cameraMovement;

    [CanBeNull]
    public Model.Weapon WeaponModel
    {
        get => _weaponModel;
        set
        {
            _weaponModel = value;
            if (value != null)
            {
                _fireClip = Resources.Load<AudioClip>(value.audio);
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


    private void Start()
    {
        _fireCooldown = 1 / fireRate;
        WeaponModel = InventoryManager.Instance.block;
    }

    private void Update()
    {
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
        }
    }

    public void Fire()
    {
        if (_weaponModel != null)
        {
            _lastFire = Time.time;
            audioSource.PlayOneShot(_fireClip);
            animator.SetTrigger(Animator.StringToHash($"fire_{_weaponModel.fireAnimation}"));
            // var bulletGO=Instantiate(bullet);
            // bulletGO.transform.position = bulletSpawnPoint.transform.position;
            // bulletGO.GetComponent<Rigidbody>().AddForce(transform.parent.forward*0.4f,ForceMode.Impulse);
        }
    }

    public void SwitchEquipped(WeaponType weaponType)
    {
        audioSource.PlayOneShot(switchEquippedClip);
        WeaponModel = weaponType switch
        {
            WeaponType.Block => InventoryManager.Instance.block,
            WeaponType.Melee => InventoryManager.Instance.melee,
            WeaponType.Primary => InventoryManager.Instance.primary,
            WeaponType.Secondary => InventoryManager.Instance.secondary,
            WeaponType.Tertiary => InventoryManager.Instance.tertiary,
            _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
        };
    }   
}