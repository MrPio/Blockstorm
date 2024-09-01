using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Model;
using Network;
using Partials;
using UI;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Utils.Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Random = UnityEngine.Random;


namespace Prefabs.Player
{
    public class Player : NetworkBehaviour
    {
        private SceneManager _sm;

        #region Serializable

        [Header("Params")] [SerializeField] public float speed = 8f;
        [SerializeField] public float stamina = 5f;
        [SerializeField] public float staminaRecoverSpeed = 1f;
        [SerializeField] public float runMultiplier = 1.5f;
        [SerializeField] public float crouchMultiplier = 0.65f;
        [SerializeField] public float fallSpeed = 2f;
        [SerializeField] public float gravity = 9.18f;
        [SerializeField] public float jumpHeight = 1.25f;
        [SerializeField] public float maxVelocityY = 20f;
        [SerializeField] private float cameraBounceDuration;
        [SerializeField] private AnimationCurve cameraBounceCurve;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private float spawnInvincibilityDuration = 5f;

        [Header("Components")] [SerializeField]
        private CharacterController characterController;

        [SerializeField] private Transform groundCheck;
        [SerializeField] private Animator bodyAnimator;
        [SerializeField] private Transform enemyWeaponContainer;
        [SerializeField] public Weapon weapon;
        [SerializeField] public AudioSource walkAudioSource;
        [SerializeField] public AudioSource audioSource;
        [SerializeField] public Transform cameraTransform;
        [SerializeField] private Transform head;
        [SerializeField] private Transform belly;
        [SerializeField] private Ragdoll ragdoll;
        [SerializeField] private SkinnedMeshRenderer[] bodyMeshes;
        [SerializeField] private GameObject helmet;

        [Header("Prefabs")] [SerializeField] public List<GameObject> muzzles;
        [SerializeField] public List<GameObject> smokes;
        [SerializeField] public GameObject circleDamage;
        [SerializeField] private GameObject helmetPrefab;
        [SerializeField] private GameObject playerBodyPrefab;

        [Header("AudioClips")] [SerializeField]
        public AudioClip walkGeneric;

        [SerializeField] private AudioClip hit;
        [SerializeField] private AudioClip deadHit;
        [SerializeField] private AudioClip helmetHit;

        [SerializeField] public AudioClip walkMetal;
        [SerializeField] public AudioClip walkWater;

        #endregion

        #region Private

        private Transform _transform;
        private bool _isGrounded;
        private Vector3 _velocity;
        private float _cameraBounceStart, _cameraBounceIntensity;
        private Vector3 _cameraInitialLocalPosition;
        private float _lastWalkCheck;
        [NonSerialized] public GameObject WeaponPrefab;
        private bool isDying;
        private float _usedStamina;
        private NetworkDestroyable _networkDestroyable;

        private bool CanUseInventory => Team is not Team.None && active.Value &&
                                        _sm.worldManager.Map.spawns.First(it => it.team == Team)
                                            .IsInside(transform.position);

        // Owner-only
        private void UpdateChunks()
        {
            if (!IsOwner) return;
            _sm.ammoHUD.SetInventoryIcon(CanUseInventory);
            if (active.Value)
                _sm.worldManager.UpdatePlayerPos(_transform.position);
        }

        // Owner-only
        private void LoadStatus()
        {
            if (!IsOwner)
                _sm.logger.Log($"Receiving new status from {OwnerClientId}: team={Team}", Color.yellow);

            // Load HUD values
            if (IsOwner)
            {
                _sm.hpHUD.SetHp(Status.Value.Hp, Status.Value.Armor, Status.Value.HasHelmet);
                _sm.ammoHUD.SetGrenades(Status.Value.LeftGrenades, Status.Value.LeftSecondaryGrenades);
            }

            // Load the enemy helmet, if any
            if (!IsOwner)
            {
                helmet.SetActive(Status.Value.HasHelmet);
                if (Status.Value.HasHelmet)
                    helmet.GetComponent<MeshRenderer>().material =
                        Resources.Load<Material>(
                            $"Textures/helmet/Materials/helmet_{Team.ToString().ToLower()}");
            }

            // Add the player to the mipmap
            var player = FindObjectsOfType<Player>().First(it => it.IsOwner);
            if (Team is not Team.None && (IsOwner || player.Team is Team.None || Team == player.Team))
                _sm.mipmap.AddPlayerMarker(Team, transform);

            // Load the body skin
            foreach (var bodyMesh in bodyMeshes)
            {
                if (bodyMesh.IsDestroyed()) continue;
                var skinName = Status.Value.Skin.GetSkinForTeam(Team, invincible: invincible.Value);
                bodyMesh.material = Resources.Load<Material>($"Materials/skin/{skinName}");
            }

            // Load collectables weapon prefabs
            if (IsOwner)
                foreach (var colletable in _sm.worldManager.SpawnedCollectables)
                    colletable.TryUpdateWeaponPrefab(Status.Value);
        }

        #endregion

        #region NetworkVariables

        // Used to set the enemy walking animation
        private readonly NetworkVariable<bool> _isWalking = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Used to set the enemy walking speed animation
        private readonly NetworkVariable<bool> _isRunning = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Used to set the enemy crouching animation
        private readonly NetworkVariable<bool> _isCrouching = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Used to disable the enemy walking animation
        private readonly NetworkVariable<long> lastShot = new(0,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Used to play the weapon sound
        public readonly NetworkVariable<NetString> LastShotWeapon = new(new(),
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Used to animate the enemy's body tilt
        public readonly NetworkVariable<byte> CameraRotationX = new(0,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Used to update the enemy's weapon prefab
        public readonly NetworkVariable<NetString> EquippedWeapon = new(new NetString { Message = "" },
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // The player status contains all the player info that needs to be shared
        public readonly NetworkVariable<PlayerStatus> Status = new(new PlayerStatus(null),
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // The player stat stores all the info that needs to be showed in the dashboard
        public readonly NetworkVariable<PlayerStats> Stats = new(new PlayerStats(null),
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // The player team
        private readonly NetworkVariable<Team> team = new(Team.None,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public Team Team
        {
            get => team.Value;
            set => team.Value = value;
        }

        // If the player is spawned
        public NetworkVariable<bool> active = new(false, NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        // If the player is invincible
        public NetworkVariable<bool> invincible = new(false, NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        #endregion

        #region Events

        public override void OnNetworkSpawn()
        {
            // Listen to network variables based on ownership
            Status.OnValueChanged += (_, _) => LoadStatus();
            if (!IsOwner)
            {
                _isWalking.OnValueChanged += (_, newValue) =>
                {
                    var isShooting = DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastShot.Value < 400;
                    bodyAnimator.SetTrigger(newValue && !isShooting
                        ? Animator.StringToHash("walk")
                        : Animator.StringToHash("idle"));
                };
                _isRunning.OnValueChanged += (_, newValue) =>
                {
                    var isShooting = DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastShot.Value < 400;
                    bodyAnimator.speed = (newValue && _isWalking.Value && !isShooting
                        ? runMultiplier
                        : 1f) * (_isCrouching.Value ? crouchMultiplier : 1f);
                };
                _isCrouching.OnValueChanged += (_, newValue) =>
                {
                    var isShooting = DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastShot.Value < 400;
                    bodyAnimator.speed = (newValue && _isWalking.Value && !isShooting
                        ? crouchMultiplier
                        : 1f) * (_isRunning.Value ? runMultiplier : 1f);
                    bodyAnimator.SetTrigger(Animator.StringToHash(newValue ? "crouch" : "no_crouch"));
                };
                lastShot.OnValueChanged += (_, _) =>
                {
                    bodyAnimator.SetTrigger(Animator.StringToHash("idle"));
                    // _isPlayerWalking.Value = false;
                };
                LastShotWeapon.OnValueChanged += (_, newValue) =>
                {
                    if (newValue.Message.Value.Length > 0)
                        audioSource.PlayOneShot(
                            Resources.Load<AudioClip>(Model.Weapon.Name2Weapon(newValue)!.GetAudioClip),
                            0.8f);
                };
                EquippedWeapon.OnValueChanged += (_, newValue) =>
                {
                    print($"[EquippedWeapon.OnValueChanged] Player {OwnerClientId} has equipped {newValue.Message}");
                    var weaponModel = Model.Weapon.Name2Weapon(newValue.Message.Value);
                    if (weaponModel is null)
                        return;
                    foreach (Transform child in enemyWeaponContainer)
                        Destroy(child.gameObject);
                    var go = Resources.Load<GameObject>(weaponModel.GetPrefab(enemy: true));
                    WeaponPrefab = Instantiate(go, enemyWeaponContainer).Apply(o =>
                    {
                        o.AddComponent<WeaponSway>();
                        if (weaponModel.Type is WeaponType.Block)
                            o.GetComponent<MeshRenderer>().material = Resources.Load<Material>(
                                $"Textures/texturepacks/blockade/Materials/blockade_{(Status.Value.BlockType(Team).sideID + 1):D1}");
                    });

                    // Load materials
                    foreach (var mesh in WeaponPrefab.GetComponentsInChildren<MeshRenderer>(true))
                        if (!mesh.gameObject.name.Contains("scope") && weaponModel.Variant is not null)
                            mesh.material = Resources.Load<Material>(weaponModel.GetMaterial);
                };
                CameraRotationX.OnValueChanged += (_, newValue) =>
                {
                    var rotation = (float)(newValue - 128);
                    var headRotation = Mathf.Clamp(rotation + 20f, -38, 20f) - 20f;
                    var bellyRotation = Mathf.Clamp(rotation - 15f, -30f, 30f);
                    head.localRotation = Quaternion.Euler(headRotation, 0f, 0f);
                    belly.localRotation = Quaternion.Euler(bellyRotation, 0f, 0f);
                };
                team.OnValueChanged += (_, _) => LoadStatus();
                if (team.Value is not Team.None) LoadStatus();
            }
            else
                invincible.Value = false;

            active.OnValueChanged += (_, newValue) => _networkDestroyable.SetEnabled(newValue);
            _networkDestroyable.SetEnabled(active.Value);

            invincible.OnValueChanged += (_, newValue) =>
            {
                if (IsOwner)
                    _sm.invincibilityHUD.SetActive(newValue);
                else
                {
                    foreach (var bodyMesh in bodyMeshes)
                    {
                        if (bodyMesh.IsDestroyed()) continue;
                        var skinName = Status.Value.Skin.GetSkinForTeam(Team, invincible: newValue);
                        bodyMesh.material = Resources.Load<Material>($"Materials/skin/{skinName}");
                    }
                }
            };

            _sm.logger.Log($"[OnNetworkSpawn] Player {OwnerClientId} joined the session!");
        }

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
            _networkDestroyable = GetComponent<NetworkDestroyable>();
        }

        private void Start()
        {
            if (!IsOwner) return;
            _transform = transform;
            _cameraInitialLocalPosition = cameraTransform.localPosition;

            // Update the view distance. Render new chunks if needed.
            InvokeRepeating(nameof(UpdateChunks), 0, 1);
        }

        private void Update()
        {
            if (!IsOwner || isDying || !active.Value) return;

            // Show the pause menu
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (!_sm.pauseMenu.activeSelf)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                _sm.pauseMenu.SetActive(!_sm.pauseMenu.activeSelf);
            }

            if (_sm.pauseMenu.activeSelf)
                return;

            // Debug: Kill everyone except myself when pressing the L key
            // if (Input.GetKeyDown(KeyCode.L))
            //     foreach (var enemy in FindObjectsOfType<Player>().Where(it => !it.IsOwner))
            //         enemy.DamageClientRpc(999, "chest",
            //             new NetVector3(Vector3.up), OwnerClientId);

            // When the player has touched the ground, activate his jump.
            // _isGrounded = Physics.CheckBox(groundCheck.position, new Vector3(0.4f, 0.25f, 0.4f), Quaternion.identity,
            //     groundLayerMask);
            _isGrounded = characterController.isGrounded;
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

            // Move the camera down when hitting the ground after a high fall.
            var bounceTime = (Time.time - _cameraBounceStart) / cameraBounceDuration;
            cameraTransform.transform.localPosition = _cameraInitialLocalPosition + Vector3.down *
                ((bounceTime > 1 ? 0 : cameraBounceCurve.Evaluate(bounceTime)) * _cameraBounceIntensity +
                 (_isCrouching.Value ? 0.4f : 0f));

            // Handle XZ movement
            var x = Input.GetAxis("Horizontal");
            var z = Input.GetAxis("Vertical");
            var move = _transform.right * x + _transform.forward * z;
            _velocity.y -= gravity * Time.deltaTime;
            _velocity.y = Mathf.Clamp(_velocity.y, -maxVelocityY, 100);

            // If crunching prevent from falling
            var isAboutToFall =
                !Physics.CheckSphere(groundCheck.position + move.normalized * 0.05f, 0.15f, groundLayerMask);
            characterController.Move(move * (speed * Time.deltaTime * (Weapon.isAiming ? 0.66f : 1f) *
                                             (_isRunning.Value ? runMultiplier : 1f) *
                                             (_isCrouching.Value ? crouchMultiplier : 1f) *
                                             (_isCrouching.Value && isAboutToFall ? 0f : 1f))
                                     + _velocity * Time.deltaTime);

            // Broadcast the walking state
            var isWalking = math.abs(x) > 0.1f || math.abs(z) > 0.1f;
            if (_isWalking.Value != isWalking)
                _isWalking.Value = isWalking;

            // Handle jump
            if (Input.GetButtonDown("Jump") && _isGrounded /*&& !Weapon.isAiming*/)
                _velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);

            // Invisible walls on map edges
            var mapSize = _sm.worldManager.Map.size;
            var pos = _transform.position;
            if (pos.x > mapSize.x || pos.y > mapSize.y ||
                pos.z > mapSize.z || pos.x > 0 || pos.y > 0 ||
                pos.z > 0)
                transform.position = new Vector3(MathF.Max(0.5f, Mathf.Min(pos.x, mapSize.x - 0.5f)),
                    MathF.Max(0.5f, Mathf.Min(pos.y, mapSize.y - 0.5f)),
                    MathF.Max(0.5f, Mathf.Min(pos.z, mapSize.z - 0.5f)));

            if (pos.y < 0.85)
                Spawn(onlyPosition: true);

            // Play walk sound
            if (Time.time - _lastWalkCheck > 0.1f)
            {
                walkAudioSource.pitch = _isRunning.Value ? runMultiplier : 1f;
                _lastWalkCheck = Time.time;
                if (_isGrounded && move.magnitude > 0.1f)
                {
                    var terrainType =
                        _sm.worldManager.GetVoxel(Vector3Int.FloorToInt(cameraTransform.position + Vector3.down * 2));
                    var hasWater =
                        _sm.worldManager.GetVoxel(Vector3Int.FloorToInt(cameraTransform.position + Vector3.down * 1))!
                            .name.Contains("water");

                    if (terrainType == null)
                        return;
                    var clip = walkGeneric;
                    if (new List<string> { "iron", "steel" }.Any(it => terrainType.name.Contains(it)))
                        clip = walkMetal;
                    if (hasWater)
                        clip = walkWater;
                    if (walkAudioSource.clip != clip)
                        walkAudioSource.clip = clip;
                    if (!walkAudioSource.isPlaying)
                        walkAudioSource.Play();
                }
                else if (walkAudioSource.isPlaying)
                    walkAudioSource.Pause();
            }

            // Handle inventory weapon switch
            if (weapon.WeaponModel != null)
            {
                WeaponType? weapon = null;
                if (Input.GetKeyDown(KeyCode.Alpha1) && this.weapon.WeaponModel!.Type != WeaponType.Block)
                    weapon = WeaponType.Block;
                else if (Input.GetKeyDown(KeyCode.Alpha2) && this.weapon.WeaponModel!.Type != WeaponType.Melee)
                    weapon = WeaponType.Melee;
                else if (Input.GetKeyDown(KeyCode.Alpha3) && this.weapon.WeaponModel!.Type != WeaponType.Primary)
                    weapon = WeaponType.Primary;
                else if (Input.GetKeyDown(KeyCode.Alpha4) && this.weapon.WeaponModel!.Type != WeaponType.Secondary)
                    weapon = WeaponType.Secondary;
                else if (Input.GetKeyDown(KeyCode.Q) && this.weapon.WeaponModel!.Type != WeaponType.Tertiary)
                    weapon = WeaponType.Tertiary;
                else if (Input.GetAxis("Mouse ScrollWheel") > 0.05f)
                    weapon = this.weapon.WeaponModel.Type switch
                    {
                        WeaponType.Block => WeaponType.Melee,
                        WeaponType.Melee => WeaponType.Primary,
                        WeaponType.Primary => WeaponType.Secondary,
                        WeaponType.Secondary => WeaponType.Tertiary,
                        WeaponType.Tertiary => WeaponType.Block,
                        _ => null
                    };
                else if (Input.GetAxis("Mouse ScrollWheel") < -0.05f)
                    weapon = this.weapon.WeaponModel.Type switch
                    {
                        WeaponType.Block => WeaponType.Tertiary,
                        WeaponType.Melee => WeaponType.Block,
                        WeaponType.Primary => WeaponType.Melee,
                        WeaponType.Secondary => WeaponType.Primary,
                        WeaponType.Tertiary => WeaponType.Secondary,
                        _ => null
                    };
                if (weapon is not null)
                    this.weapon.SwitchEquipped(weapon.Value);

                if (Input.GetMouseButtonDown(1) && this.weapon.WeaponModel!.Type != WeaponType.Block &&
                    this.weapon.WeaponModel!.Type != WeaponType.Melee && this.weapon.WeaponModel!.HasAim)
                    this.weapon.ToggleAim();
            }

            // Handle weapon reloading
            if (Input.GetKeyDown(KeyCode.R) && (weapon.WeaponModel?.IsGun ?? false))
                if (weapon.Magazine[weapon.WeaponModel!.GetNetName] < weapon.WeaponModel.Magazine)
                    weapon.Reload();
                else audioSource.PlayOneShot(weapon.noAmmoClip);

            // Handle sprint
            if (Input.GetKey(KeyCode.LeftShift) && isWalking && _usedStamina < stamina)
            {
                _usedStamina += Time.deltaTime;
                _sm.staminaBar.SetValue(1 - _usedStamina / stamina);
                _isRunning.Value = true;
                _isCrouching.Value = false;
            }
            else
            {
                if (_isRunning.Value)
                    _isRunning.Value = false;
                if (!Input.GetKey(KeyCode.LeftShift) && _usedStamina > 0)
                {
                    _usedStamina -= Time.deltaTime * staminaRecoverSpeed;
                    _sm.staminaBar.SetValue(1 - _usedStamina / stamina);
                }
            }

            // Handle crouch
            if (Input.GetKeyDown(KeyCode.LeftControl) && !_isCrouching.Value && !_isRunning.Value)
            {
                _isRunning.Value = false;
                _isCrouching.Value = true;
            }

            else if (Input.GetKeyUp(KeyCode.LeftControl) && _isCrouching.Value)
                _isCrouching.Value = false;

            // Handle Inventory
            if (Input.GetKeyDown(KeyCode.I) && CanUseInventory)
            {
                active.Value = false;
                _sm.InitializeInventory();
            }
        }

        # endregion

        #region RPCs

        // Spawn the muzzle billboard texture on the weapon mouth
        [Rpc(SendTo.Everyone)]
        public void SpawnWeaponEffectRpc()
        {
            var mouth = WeaponPrefab.transform.Find("mouth");
            if (mouth)
                Instantiate(muzzles.RandomItem(), mouth.position, mouth.rotation)
                    .Apply(o => o.layer = LayerMask.NameToLayer(IsOwner ? "WeaponCamera" : "Default"));
        }

        [Rpc(SendTo.Everyone)]
        public void DamageClientRpc(uint damage, string bodyPart, NetVector3 direction, ulong attackerID,
            float ragdollScale = 1)
        {
            var newStatus = Status.Value;
            var absorbedByArmor = math.min((int)damage, newStatus.Armor);
            newStatus.Armor -= absorbedByArmor;
            newStatus.Hp -= ((int)damage - absorbedByArmor);

            // Both owner and non-owner hear the hit sound effect
            if (bodyPart == "Head" && Status.Value.HasHelmet)
            {
                audioSource.PlayOneShot(helmetHit);

                // Handle helmet removal
                var rb = Instantiate(helmetPrefab,
                        helmet.IsDestroyed() ? cameraTransform.position + Vector3.up * 0.5f : helmet.transform.position,
                        helmet.IsDestroyed() ? cameraTransform.rotation : helmet.transform.rotation)
                    .GetComponent<Rigidbody>();
                rb.AddExplosionForce(helmet.IsDestroyed() ? 450f : 750f,
                    helmet.IsDestroyed()
                        ? cameraTransform.position
                        : helmet.transform.position + VectorExtensions.RandomVector3(-0.8f, 0.8f), 2f);
                rb.angularVelocity = VectorExtensions.RandomVector3(-70f, 70f);
                rb.GetComponent<MeshRenderer>().material =
                    Resources.Load<Material>(
                        $"Textures/helmet/Materials/helmet_{Team.ToString().ToLower()}");


                if (!IsOwner)
                    helmet.SetActive(false);
                // damage /= 2; Already halved by Fire()
            }
            else
                audioSource.PlayOneShot(newStatus.IsDead ? deadHit : hit);

            // Stop adding points
            if (IsHost && newStatus.IsDead)
                FindObjectOfType<ScoreCube>().insidePlayers.Remove(this);

            print($"{OwnerClientId} - {attackerID} has attacked {OwnerClientId} dealing {damage} damage!");
            var attacker = FindObjectsOfType<Player>().First(it => it.OwnerClientId == attackerID);

            // Show kill HUD
            if (_sm.networkManager.LocalClientId == attackerID &&
                !((attackerID != OwnerClientId && attacker.Team == Team) || invincible.Value))
                _sm.killPlusOne.Activate(Team, OwnerClientId == attackerID, isKill: newStatus.IsDead);


            if (!IsOwner) return;
            // Owner only ========================================================================================

            // Check if the enemy is allied or invincible
            if ((attackerID != OwnerClientId && attacker.Team == Team) || invincible.Value)
                return;

            if (bodyPart == "Head" && Status.Value.HasHelmet)
                newStatus.HasHelmet = false;

            // Update player's HP
            Status.Value = newStatus;

            // Ragdoll
            if (newStatus.IsDead)
            {
                // Disable any aiming
                if (Weapon.isAiming)
                    weapon.ToggleAim();

                // If it's not a suicide, add the kill
                if (attackerID != OwnerClientId /*AKA: attackedID*/)
                {
                    var newAttackerStats = attacker.Stats.Value;
                    newAttackerStats.Kills += 1;
                    attacker.UpdateStatServerRpc(newAttackerStats);
                }

                var newAttackedStats = Stats.Value;
                newAttackedStats.Deaths += 1;
                UpdateStatServerRpc(newAttackedStats);
                RagdollRpc((uint)(damage * ragdollScale), bodyPart, direction);

                StartCoroutine(Respawn());

                IEnumerator Respawn()
                {
                    yield return new WaitForSeconds(2f);
                    active.Value = false;
                    _sm.InitializeTeamSelection(isFirstSpawn: false);
                }
            }

            // Spawn damage circle
            var directionToEnemy = attacker.transform.position - cameraTransform.position;
            var projectedDirection = Vector3.ProjectOnPlane(directionToEnemy, cameraTransform.up);
            var angle = Vector3.SignedAngle(cameraTransform.forward, projectedDirection, Vector3.up);
            var circleDamageGo = Instantiate(circleDamage, _sm.circleDamageContainer.transform);
            circleDamageGo.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, -angle);
        }

        [Rpc(SendTo.Everyone)]
        private void RagdollRpc(uint damage, string bodyPart, NetVector3 direction, bool reverse = false)
        {
            isDying = !reverse;
            if (isDying)
                _sm.logger.Log($"[RagdollRpc] {OwnerClientId} is dead!", IsOwner ? Color.cyan : Color.yellow);
            if (IsOwner)
            {
                walkAudioSource.Pause();
                if (isDying)
                    GetComponent<CharacterController>().enabled = !isDying;
                GetComponent<CapsuleCollider>().enabled = isDying;
                GetComponentInChildren<CameraMovement>().enabled = !isDying;
                GetComponentInChildren<Weapon>().enabled = !isDying;
                GetComponentInChildren<WeaponSway>().enabled = !isDying;
                transform.Find("WeaponCamera").gameObject.SetActive(!isDying);
                if (isDying)
                    gameObject.AddComponent<Rigidbody>().Apply(rb =>
                        rb.AddForceAtPosition(direction.ToVector3.normalized * (damage * 5f),
                            _transform.position + Vector3.up * 0.5f));
                else if (gameObject.TryGetComponent<Rigidbody>(out var rb))
                    Destroy(rb);
            }
            else
            {
                if (isDying)
                    ragdoll.ApplyForce(bodyPart, direction.ToVector3.normalized * math.clamp(damage * 5, 50f, 500f));
                else
                    ragdoll.SetRagdollState(false);

                gameObject.GetComponent<ClientNetworkTransform>().enabled = !isDying;
            }
        }

        [Rpc(SendTo.Owner)]
        private void UpdateStatServerRpc(PlayerStats playerStats) => Stats.Value = playerStats;

        #endregion

        /// <summary>
        /// The owner spawns the player, adds it to the mipmap and loads the right arm skin texture.
        /// The other clients add the player to the mipmap and load the helmet and the body skin texture.
        /// </summary>
        public void Spawn(Team? newTeam = null, PlayerStats? playerStats = null, bool onlyPosition = false)
        {
            characterController.enabled = false;

            // Spawn the player location
            transform.SetPositionAndRotation(
                position: _sm.worldManager.Map.GetRandomSpawnPoint(newTeam ?? Team) + Vector3.up * 1.5f,
                // position: (Vector3Int)_sm.worldManager.Map.scoreCubePosition + Vector3.up * 2.1f +
                // Vector3.forward * 4.5f,
                rotation: Quaternion.Euler(0, Random.Range(-180f, 180f), 0));
            GetComponent<ClientNetworkTransform>().Interpolate = true;

            if (onlyPosition) return;

            if (newTeam is not null)
                team.Value = newTeam.Value;
            if (playerStats is not null)
                Stats.Value = playerStats.Value;

            _sm.logger.Log($"[Spawn] Spawning {OwnerClientId}, team = {Team.ToString()}", Color.cyan);
            active.Value = true;
            invincible.Value = true;
            Status.Value = new PlayerStatus(null);
            RagdollRpc(0, "", new NetVector3(), true);
            weapon.Magazine.Clear();
            weapon.LeftAmmo.Clear();
            weapon.WeaponModel = null;

            if (newTeam is not null || playerStats is not null)
                LoadStatus();
            StartCoroutine(EquipBlock());
            StartCoroutine(EndInvincibility());
            StartCoroutine(EnableCc());
            return;

            IEnumerator EnableCc()
            {
                yield return new WaitForSeconds(1f);
                characterController.enabled = true;
            }

            IEnumerator EquipBlock()
            {
                yield return new WaitForSeconds(0.15f);
                weapon.SwitchEquipped(WeaponType.Block);
            }

            IEnumerator EndInvincibility()
            {
                yield return new WaitForSeconds(spawnInvincibilityDuration);
                invincible.Value = false;
            }
        }
    }
}