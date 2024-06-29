using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelEngine;

public class Player : MonoBehaviour
{
    [SerializeField] public float speed = 8f, fallSpeed = 2, gravity = 9.18f, jumpHeight = 1.25f, maxVelocityY=20f;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayerMask;
    private Transform _transform;
    private bool _isGrounded;
    private Vector3 _velocity;

    private void Start()
    {
        _transform = transform;
        WorldManager.instance.UpdatePlayerPos(_transform.position);
    }

    private void Update()
    {
        _isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayerMask);
        if (_isGrounded && _velocity.y < 0)
            _velocity.y = -fallSpeed;

        var x = Input.GetAxis("Horizontal");
        var z = Input.GetAxis("Vertical");
        var move = _transform.right * x + _transform.forward * z;
        if (Input.GetButtonDown("Jump") && _isGrounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
        _velocity.y -= gravity * Time.deltaTime;
        _velocity.y = Mathf.Clamp(_velocity.y, -maxVelocityY, 100);
        characterController.Move(speed * Time.deltaTime * move + _velocity * Time.deltaTime);
        WorldManager.instance.UpdatePlayerPos(_transform.position);
    }

}