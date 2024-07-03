using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponMovement : MonoBehaviour
{
    public AudioClip fireClip;
    public AudioSource audioSource;
    public Animator animator;
    public Transform bulletSpawnPoint;
    public GameObject bullet;
    public float fireRate = 4;
    private float _lastFire;
    private float _fireCooldown;
    

    private void Start()
    {
        _fireCooldown = 1 / fireRate;
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time - _lastFire > _fireCooldown)
        {
            _lastFire = Time.time;
            audioSource.PlayOneShot(fireClip);
            animator.SetTrigger(Animator.StringToHash("Fire"));
            var bulletGO=Instantiate(bullet);
            bulletGO.transform.position = bulletSpawnPoint.transform.position;
            bulletGO.GetComponent<Rigidbody>().AddForce(transform.parent.forward*0.4f,ForceMode.Impulse);
        }
    }
}