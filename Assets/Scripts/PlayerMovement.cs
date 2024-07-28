using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;

public class Player : NetworkBehaviour
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
    [SerializeField] private Transform cameraTransform;
    private float _cameraBounceStart, _cameraBounceIntensity;
    private Vector3 _cameraInitialLocalPosition;
    public AudioSource audioSource;
    public AudioClip walkGeneric, walkMetal, walkWater;
    private float _lastWalkCheck;

    private void Start()
    {
        _transform = transform;
        WorldManager.instance.UpdatePlayerPos(_transform.position);
        _cameraInitialLocalPosition = cameraTransform.localPosition;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        // When the player has touched the ground, activate his jump.
        _isGrounded = Physics.CheckBox(groundCheck.position, new Vector3(0.4f, 0.3f, 0.4f), Quaternion.identity,
            groundLayerMask);
        if (_isGrounded && _velocity.y < 0)
        {
            // When the player hits the ground after a hard enough fall, start the camera bounce animation.
            if (_velocity.y < -fallSpeed * 2)
            {
                _cameraBounceStart = Time.time;
                _cameraBounceIntensity = 1f * Mathf.Pow(-_velocity.y / maxVelocityY, 3f);
            }
            // The vertical speed is set to a value less than 0 to get a faster fall on the next fall. 
            _velocity.y = -fallSpeed;
        }

        // Move the camera down when hitting the ground after an high fall.
        var bounceTime = (Time.time - _cameraBounceStart) / cameraBounceDuration;
        cameraTransform.transform.localPosition = _cameraInitialLocalPosition + Vector3.down *
            ((bounceTime > 1 ? 0 : cameraBounceCurve.Evaluate(bounceTime)) * _cameraBounceIntensity);

        // Handle XZ movement
        var x = Input.GetAxis("Horizontal");
        var z = Input.GetAxis("Vertical");
        var move = _transform.right * x + _transform.forward * z;
        _velocity.y -= gravity * Time.deltaTime;
        _velocity.y = Mathf.Clamp(_velocity.y, -maxVelocityY, 100);
        characterController.Move(move * (speed * Time.deltaTime * (WeaponManager.isAiming ? 0.66f : 1f)) +
                                 _velocity * Time.deltaTime);

        // Handle jump
        if (Input.GetButtonDown("Jump") && _isGrounded && !WeaponManager.isAiming)
            _velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);

        // Invisible walls on map edges
        var mapSize = WorldManager.instance.map.size;
        var pos = _transform.position;
        if (pos.x > mapSize.x || pos.y > mapSize.y ||
            pos.z > mapSize.z || pos.x > 0 || pos.y > 0 ||
            pos.z > 0)
            transform.position = new Vector3(MathF.Max(0.5f, Mathf.Min(pos.x, mapSize.x - 0.5f)),
                MathF.Max(0.5f, Mathf.Min(pos.y, mapSize.y - 0.5f)),
                MathF.Max(0.5f, Mathf.Min(pos.z, mapSize.z - 0.5f)));

        // Update the view distance. Render new chunks if needed.
        WorldManager.instance.UpdatePlayerPos(pos);

        // Play walk sound
        if (Time.time - _lastWalkCheck > 0.075f)
        {
            _lastWalkCheck = Time.time;
            if (_isGrounded && move.magnitude > 0.1f)
            {
                var terrainType =
                    WorldManager.instance.GetVoxel(Vector3Int.FloorToInt(cameraTransform.position - Vector3.up * 2));
                var hasWater =
                    WorldManager.instance.GetVoxel(Vector3Int.FloorToInt(cameraTransform.position - Vector3.up * 1))!
                        .name.Contains("water");

                if (terrainType == null)
                    return;
                var clip = walkGeneric;
                if (new List<string> { "iron", "steel" }.Any(it =>
                        terrainType.name.Contains(it)))
                    clip = walkMetal;
                if (hasWater)
                    clip = walkWater;
                if (audioSource.clip != clip)
                    audioSource.clip = clip;
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }
    }
}