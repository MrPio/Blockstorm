using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Model;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;
using Random = UnityEngine.Random;


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
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private Transform enemyWeaponContainer;
    [SerializeField] private WeaponManager weaponManager;

    private readonly NetworkVariable<bool> _isPlayerWalking = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<Message> _equipped = new(new Message { message = "" },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private struct Message : INetworkSerializable
    {
        public FixedString32Bytes message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref message);
        }
    }

    public override void OnNetworkSpawn()
    {
        Spawn();

        if (!IsOwner)
        {
            _isPlayerWalking.OnValueChanged += (_, newValue) =>
            {
                bodyAnimator.SetTrigger(newValue
                    ? Animator.StringToHash("walk")
                    : Animator.StringToHash("idle"));
            };

            _equipped.OnValueChanged += (_, newValue) =>
            {
                print($"Player {OwnerClientId} has equipped {newValue.message}");
                foreach (Transform child in enemyWeaponContainer)
                    Destroy(child.gameObject);
                var go = Resources.Load<GameObject>($"Prefabs/weapons/enemy/{newValue.message.Value.ToUpper()}");
                Instantiate(go, enemyWeaponContainer).Apply(o =>
                {
                    o.AddComponent<WeaponSway>();
                    if (newValue.message.Value.ToUpper() == "BLOCK")
                        o.GetComponent<MeshRenderer>().material = Resources.Load<Material>(
                            $"Textures/texturepacks/blockade/Materials/blockade_{(InventoryManager.Instance.BlockType.sideID + 1):D1}");
                });
            };
        }
    }

    private void Spawn()
    {
        _velocity = new();
        var spawnPoint = WorldManager.instance.map.GetRandomSpawnPoint(InventoryManager.Instance.team) +
                         Vector3.up * 1.25f;
        var rotation = Quaternion.Euler(0, Random.Range(-180f, 180f), 0);
        transform.SetPositionAndRotation(spawnPoint, rotation);
    }

    private void Start()
    {
        if (!IsOwner) return;
        _transform = transform;
        _cameraInitialLocalPosition = cameraTransform.localPosition;
        WorldManager.instance.UpdatePlayerPos(_transform.position);
        weaponManager.SwitchEquipped(WeaponType.Block);
        _equipped.Value = new Message { message = weaponManager.WeaponModel!.name };
    }

    private void Update()
    {
        if (!IsOwner) return;

        // When the player has touched the ground, activate his jump.
        _isGrounded = Physics.CheckBox(groundCheck.position, new Vector3(0.4f, 0.25f, 0.4f), Quaternion.identity,
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

        // Broadcast the walking state
        var isWalking = math.abs(x) > 0.1f || math.abs(z) > 0.1f;
        if (_isPlayerWalking.Value != isWalking)
            _isPlayerWalking.Value = isWalking;

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

        if (pos.y < -2)
            Spawn();

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

        // Handle inventory weapon switch
        if (weaponManager.WeaponModel != null)
        {
            WeaponType? weapon = null;
            if (Input.GetKeyDown(KeyCode.Alpha1) && weaponManager.WeaponModel!.type != WeaponType.Block)
                weapon = WeaponType.Block;
            else if (Input.GetKeyDown(KeyCode.Alpha2) && weaponManager.WeaponModel!.type != WeaponType.Melee)
                weapon = WeaponType.Melee;
            else if (Input.GetKeyDown(KeyCode.Alpha3) && weaponManager.WeaponModel!.type != WeaponType.Primary)
                weapon = WeaponType.Primary;
            else if (Input.GetKeyDown(KeyCode.Alpha4) && weaponManager.WeaponModel!.type != WeaponType.Secondary)
                weapon = WeaponType.Secondary;
            else if (Input.GetKeyDown(KeyCode.Alpha5) && weaponManager.WeaponModel!.type != WeaponType.Tertiary)
                weapon = WeaponType.Tertiary;
            if (weapon != null)
            {
                weaponManager.SwitchEquipped(weapon.Value);
                _equipped.Value = new Message { message = weaponManager.WeaponModel.name };
            }

            if (Input.GetMouseButtonDown(1) && weaponManager.WeaponModel!.type != WeaponType.Block &&
                weaponManager.WeaponModel!.type != WeaponType.Melee)
                weaponManager.ToggleAim();
        }
    }
}