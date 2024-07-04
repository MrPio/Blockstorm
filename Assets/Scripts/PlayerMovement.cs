using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelEngine;

public class Player : MonoBehaviour
{
    [SerializeField] public float speed = 8f, fallSpeed = 2, gravity = 9.18f, jumpHeight = 1.25f, maxVelocityY = 20f;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayerMask;
    private Transform _transform;
    private bool _isGrounded;
    private Vector3 _velocity;
    [SerializeField] private AnimationCurve cameraBounceCurve;
    [SerializeField] private float cameraBounceDuration;
    [SerializeField] private Transform camera;
    private float _cameraBounceStart, _cameraBounceIntensity;
    private Vector3 _cameraInitialLocalPosition;

    private void Start()
    {
        _transform = transform;
        WorldManager.instance.UpdatePlayerPos(_transform.position);
        _cameraInitialLocalPosition = camera.localPosition;
    }

    private void Update()
    {
        _isGrounded = Physics.CheckBox(groundCheck.position, new Vector3(0.45f,0.2f,0.45f),Quaternion.identity, groundLayerMask);
        if (_isGrounded && _velocity.y < 0)
        {
            // The player has touched the ground
            if (_velocity.y < -fallSpeed * 2)
            {
                // The player has hit the ground
                _cameraBounceStart = Time.time;
                _cameraBounceIntensity = 1f * Mathf.Pow(-_velocity.y / maxVelocityY, 3f);
            }

            _velocity.y = -fallSpeed;
        }

        var dampFactor = (Time.time - _cameraBounceStart) / cameraBounceDuration;
        camera.transform.localPosition = _cameraInitialLocalPosition + Vector3.down *
            ((dampFactor > 1 ? 0 : cameraBounceCurve.Evaluate(dampFactor)) * _cameraBounceIntensity);

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