using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ExtensionFunctions;
using JetBrains.Annotations;
using Managers;
using Model;
using Network;
using Partials;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Utils;
using VoxelEngine;
using Random = UnityEngine.Random;

namespace Prefabs.Player.AI
{
    public enum AIState
    {
        Dead,
        Patrolling,
        Searching,
        Attacking
    }

    public class BotAI : MonoBehaviour
    {
        #region constants
        private const float LogicStep = 1f / 6; // 6 FPS

        // Patrolling
        private readonly Dictionary<AIState, Vector2> _movingRange = new()
        {
            { AIState.Patrolling, new Vector2(15, 45) },
            { AIState.Attacking, new Vector2(5, 30) },
            { AIState.Searching, new Vector2(1, 10) },
        };

        private readonly Dictionary<AIState, Vector2> _speedFactorRange = new()
        {
            { AIState.Patrolling, new Vector2(0.8f, 1.25f) },
            { AIState.Attacking, new Vector2(0.9f, 1.35f) },
            { AIState.Searching, new Vector2(1.15f, 1.45f) },
        };

        private readonly Dictionary<AIState, float> _speedChangeStep = new()
        {
            { AIState.Patrolling, 5 },
            { AIState.Attacking, 4 },
            { AIState.Searching, 4 },
        };

        private readonly Dictionary<AIState, float> _propCheckStep = new()
        {
            { AIState.Patrolling, 15 },
            { AIState.Attacking, 7 },
            { AIState.Searching, 8 },
        };

        private readonly Dictionary<AIState, float> _playerBlockCheckStep = new()
        {
            { AIState.Patrolling, 1 },
            { AIState.Attacking, 0.75f },
            { AIState.Searching, 0.8f },
        };

        private readonly Dictionary<AIState, float> _jumpProbability = new()
        {
            { AIState.Patrolling, 0.05f },
            { AIState.Attacking, 0.25f },
            { AIState.Searching, 0.1f },
        };

        private readonly Dictionary<AIState, float> _grenadeProbability = new()
        {
            { AIState.Patrolling, 0.001f },
            { AIState.Attacking, 0.0075f },
            { AIState.Searching, 0.0025f },
        };

        private readonly Dictionary<AIState, float> _tertiaryProbability = new()
        {
            { AIState.Patrolling, 0f },
            { AIState.Attacking, 0.015f },
            { AIState.Searching, 0.0025f },
        };

        #endregion

        #region serializable

        [SerializeField] private AnimationCurve weaponDistance2MaxAngleImprecision;

        [SerializeField] private float rotationYSmoothness,
            sphereRange = 5f,
            coneRange = 70f,
            coneAngle = 50f,
            searchStateTimeout = 15f,
            attackStateTimeout = 10f,
            searchForPlayersStep = 1f,
            secondaryMaxDistance = 15f,
            meleeMaxDistance = 5f,
            pathPointReachThreshold = 1f;

        [SerializeField] private GameObject missile;

        #endregion

        #region non serializable

        [NonSerialized] public AIState State = AIState.Dead;
        private SceneManager _sm;
        private Player _player;
        [NonSerialized] public Transform Target;
        private Vector3Int? _lastKnownEnemyPosition;
        private List<Vector3Int> _currentPath;
        private bool _isSearchingPath;
        private Thread _pathThread;
        private float _lastPathThreadDuration, _lastTargetHit;
        private int _currentPathIndex, _lostPathPointsCount, _fireCount;
        private Coroutine _switchStateCoroutine;
        private List<Vector3> _blocksTargets = new();

        private float _acc,
            _speedChangeAcc,
            _propCheckAcc,
            _playerBlockCheckAcc,
            _indexAcc,
            _fireAcc,
            _searchForPlayersAcc,
            _searchingStart;

        private float _baseSpeed, _magazine;
        private Vector2 _lookDir;
        private Model.Weapon _weaponModel;

        #endregion

        #region events

        private void Awake()
        {
            _sm = FindFirstObjectByType<SceneManager>();
            _player = GetComponent<Player>();
        }

        private void Start()
        {
            _baseSpeed = _player.speed;
        }

        private void FixedUpdate()
        {
            if (_player.Status.Value.IsDead) return;
            // Shoot ========================================================
            if (State is AIState.Attacking || _blocksTargets.Count > 0)
            {
                _fireAcc += Time.deltaTime;
                if (_fireAcc > _weaponModel.Delay)
                {
                    Fire(_blocksTargets.Count > 0 ? _blocksTargets[0] : Target.position);
                    if (_weaponModel.Type is WeaponType.Tertiary)
                        AutoChooseWeapon();
                    _fireAcc = 0;
                }

                // Reload
                if (_fireCount > math.max(1f, _magazine))
                {
                    if (_blocksTargets.Count > 0)
                        _blocksTargets.RemoveAt(0);
                    _fireCount = 0;
                    if (_weaponModel.IsGun)
                    {
                        _fireAcc = -_weaponModel.ReloadTime!.Value / 100f;
                        _magazine = _weaponModel.Magazine!.Value / Random.Range(1f, 2f);
                    }
                    else if (_weaponModel.Type is WeaponType.Melee)
                    {
                        _fireAcc = -Random.Range(0.25f, 1f);
                        _magazine = Random.Range(1, 10);
                    }

                    if (_blocksTargets.Count > 0)
                        _fireAcc = -Random.Range(0f, 0.15f);
                }
            }

            // Walk =========================================================
            // Ensure the walking algorithm is run every _logicStep
            _acc += Time.deltaTime;
            if (_acc > LogicStep)
            {
                _acc = 0;
                // Walk logic
                if (State is AIState.Patrolling or AIState.Attacking or AIState.Searching)
                {
                    // I've got no path to follow
                    if (_currentPath == null || _currentPath.Count < 3)
                    {
                        if (_pathThread is not { IsAlive: true })
                        {
                            var currentPos = transform.position;
                            var targetPos = Target?.position ?? _lastKnownEnemyPosition ?? Vector3.zero;
                            _pathThread = new Thread(() => ChoosePath(currentPos, targetPos));
                            // await Task.Run(() => ChoosePath(currentPos, targetPos));
                            _pathThread.Start();
                            Debug.LogWarning("===============");
                        }
                    }
                    // I'm currently following a path
                    else
                    {
                        if (_pathThread != null)
                        {
                            _pathThread = null;
                            _sm.logger.Log($"Find Path took {_lastPathThreadDuration}s");
                            // Debug -- spawn path
                            if (_sm.debugManager.botDrawPath)
                                foreach (var point in _currentPath)
                                    Instantiate(_sm.pathPointPrefab, point + Vector3.one * 0.5f,
                                        Quaternion.identity);
                        }

                        if (_currentPathIndex > _currentPath.Count - 1)
                        {
                            _currentPath = null;
                            return;
                        }

                        var from = _currentPath[_currentPathIndex - 1] + Vector3.one * 0.5f;
                        var to = _currentPath[_currentPathIndex] + Vector3.one * 0.5f;
                        var dir3 = transform.position - to;
                        var dir2 = new Vector2(dir3.x, dir3.z);
                        _lookDir = State switch
                        {
                            AIState.Attacking => (Vector2)new Vector2XZ(transform.position - Target.position),
                            AIState.Searching => (Vector2)new Vector2XZ(transform.position -
                                                                        _lastKnownEnemyPosition!.Value),
                            _ => dir2
                        };
                        var dist = dir2.magnitude;

                        // Set x head rotation
                        if (State is AIState.Attacking)
                        {
                            var fullLookDir = Target!.position - transform.position;
                            var deltaAngleX = Quaternion.LookRotation(fullLookDir).eulerAngles.x;
                            deltaAngleX = deltaAngleX > 180f ? deltaAngleX - 360f : deltaAngleX;
                            _player.CameraRotationX.Value = (byte)((int)deltaAngleX + 128);
                        }

                        // End of the current dir
                        if (dist < pathPointReachThreshold && Mathf.Abs(_player.groundCheck.position.y - to.y) < 0.75f)
                        {
                            _currentPathIndex++;
                            _indexAcc = 0;

                            // End of the current path
                            if (_currentPathIndex >= _currentPath.Count - 2)
                                _currentPath = null;
                        }
                        // Still pursuing the current dir
                        else
                        {
                            // Jump if there's a stair
                            if (to.y - from.y > 0.5f && _blocksTargets.Count<=0)
                                _player.InputInterface.IsJumpDown = true;

                            // Move if not falling
                            if (from.y - to.y < 0.2f)
                            {
                                // Go along the path
                                var deltaAngle = (Mathf.Atan2(dir2.y, dir2.x) - Mathf.Atan2(_lookDir.y, _lookDir.x)) *
                                                 Mathf.Rad2Deg;
                                _player.InputInterface.Axis = new Vector2(0, 1).RotateByAngle(deltaAngle);

                                // Too much time on current point, retry...
                                if (_blocksTargets.Count <= 0)
                                    _indexAcc += LogicStep;
                                if (_indexAcc > 2 && _currentPathIndex > 1)
                                {
                                    _currentPathIndex--;
                                    _lostPathPointsCount++;
                                    _indexAcc = 0;

                                    // If map has changed I may be stuck
                                    if (_lostPathPointsCount >= 3)
                                        _currentPath = null;
                                }
                            }
                            else
                            {
                                _player.InputInterface.Axis = Vector2.zero;
                                _currentPathIndex++;
                                _indexAcc = 0;
                            }

                            // Change walking speed
                            _speedChangeAcc += LogicStep;
                            if (_speedChangeAcc > _speedChangeStep[State])
                            {
                                _speedChangeAcc = 0;
                                _player.speed = _baseSpeed * _speedFactorRange[State].RandomRange();
                            }

                            // Random jump
                            if (Random.value < _jumpProbability[State] && _blocksTargets.Count<=0)
                                _player.InputInterface.IsJumpDown = true;

                            // Random grenade
                            if (Random.value < _grenadeProbability[State])
                                ThrowGrenade(Random.Range(0.25f, 0.5f), Random.value < 0.4f);

                            // Random bazooka
                            if (Random.value < _tertiaryProbability[State] && (Target is null ||
                                                                               Vector3.Distance(transform.position,
                                                                                   Target.position) > 8f))
                                SwitchEquipped(WeaponType.Tertiary);

                            // Check prop to destroy
                            _propCheckAcc += LogicStep;
                            if (_propCheckAcc > _propCheckStep[State])
                            {
                                _propCheckAcc = 0;
                                foreach (var prop in _sm.worldManager.SpawnedProps)
                                {
                                    if (prop.IsDestroyed()) continue;

                                    if (Vector3.Distance(prop.transform.position, transform.position) <= 1)
                                    {
                                        // Broadcast the damage action
                                        _sm.ClientManager.DamagePropRpc(prop.ID, 9999, false, _player.NetworkObjectId);
                                        break;
                                    }
                                }
                            }


                            // Check placed blocks to destroy
                            _playerBlockCheckAcc += LogicStep;
                            if (_playerBlockCheckAcc > _playerBlockCheckStep[State] && _blocksTargets.Count <= 0)
                            {
                                _playerBlockCheckAcc = 0;
                                CheckForDig(from, to, dir2);
                            }
                        }
                    }
                }

                // Search state timeout
                if (State is AIState.Searching && Time.time - _searchingStart > searchStateTimeout)
                    SwitchState(AIState.Patrolling);

                // Attack state timeout
                if (State is AIState.Attacking && Time.time - _lastTargetHit > attackStateTimeout)
                {
                    _lastTargetHit = Time.time;
                    _lastKnownEnemyPosition = Vector3Int.FloorToInt(Target.position + Vector3.down * 0.5f);
                    SwitchState(AIState.Searching);
                }

                // Search for players
                if (State is AIState.Patrolling or AIState.Searching)
                {
                    _searchForPlayersAcc += LogicStep;

                    if (_searchForPlayersAcc > searchForPlayersStep)
                    {
                        _searchForPlayersAcc = 0;
                        var enemy = CheckForVisiblePlayers();
                        if (enemy is not null && enemy.Team != _player.Team)
                        {
                            Alert(enemy.transform, hasSeenIt: true);
                            Debug.Log($"{gameObject.name} has seen {enemy.gameObject.name}!");
                        }
                    }
                }
            }

            // Set y rotation along the direction with smoothness
            _player.transform.rotation = Quaternion.Slerp(
                _player.transform.rotation,
                Quaternion.LookRotation(new Vector3(-_lookDir.x + 0.001f, 0, -_lookDir.y)),
                1f / (1f + rotationYSmoothness)
            );

            // Change weapon
            if ((State is AIState.Attacking || _blocksTargets.Count > 0) &&
                _weaponModel.Type is not WeaponType.Tertiary &&
                Time.frameCount % (_weaponModel.Type is WeaponType.Melee ? 25 : 100) == 0)
                AutoChooseWeapon();
        }

        #endregion

        # region private methods

        /// <summary>
        /// Choose a random path to follow
        /// </summary>
        private void ChoosePath(Vector3 currentPos, Vector3 targetPos)
        {
            var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var aStar = new AStarPathfinder(_sm.worldManager.Map);
            // Select a random point in the map, in the circle around the player
            var validDestFound = false;
            var dest = Vector3Int.zero;
            var iterations = 0;
            while (!validDestFound)
            {
                iterations++;
                if (iterations > 20)
                {
                    Thread.Sleep(10000);
                    return;
                }

                do
                {
                    var isUsingMelee = State is AIState.Attacking && _weaponModel.Type is WeaponType.Melee;
                    if (State is AIState.Patrolling)
                        dest = Vector3Int.FloorToInt(currentPos +
                                                     VectorExtensions.RandomVector3(-1, 1) *
                                                     _movingRange[State].RandomRange());
                    else if (State is AIState.Attacking or AIState.Searching)
                        dest = Vector3Int.FloorToInt(targetPos +
                                                     VectorExtensions.RandomVector3(-1, 1) *
                                                     (isUsingMelee ? 3f : _movingRange[State].RandomRange()));

                    dest.y = State is AIState.Patrolling ? 0 : math.max(0, (int)(targetPos.y - 4));
                } while (!_sm.worldManager.IsVoxelInWorld(dest));

                // Goto center
                if (State is AIState.Patrolling && VectorExtensions.Random.NextDouble() < 0.15f)
                    dest = Vector3Int.FloorToInt(_sm.worldManager.Map.scoreCubePosition +
                                                 Vector3Int.back * (VectorExtensions.Random.Next(1, 5) *
                                                                    (VectorExtensions.Random.Next() < 0.5 ? -1 : 1)) +
                                                 Vector3Int.left * (VectorExtensions.Random.Next(1, 5) *
                                                                    (VectorExtensions.Random.Next() < 0.5 ? -1 : 1)) +
                                                 Vector3Int.down * 4);
                // Find a valid y
                var startY = dest.y;
                for (dest.y = dest.y; dest.y < _sm.worldManager.Map.size.y - 1; dest.y++)
                    if (aStar.IsValidPos(new(dest.x, dest.y, dest.z)))
                    {
                        validDestFound = true;
                        break;
                    }

                if (!validDestFound)
                {
                    for (dest.y = 0; dest.y < startY + 1; dest.y++)
                        if (aStar.IsValidPos(new(dest.x, dest.y, dest.z)))
                        {
                            validDestFound = true;
                            break;
                        }
                }
            }

            List<Vector3Int> path = null;
            path = aStar.FindPath(
                Vector3Int.FloorToInt(currentPos + Vector3.down * 0.75f), dest);
            if (path != null)
            {
                _lostPathPointsCount = 0;
                _currentPathIndex = 1;
            }

            _currentPath = path;
            _lastPathThreadDuration = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - start) / 1000f;
        }

        private void Fire(Vector3 targetPos)
        {
            _fireCount++;

            // Propagate the sound across the net
            _player.LastShotWeapon.Value = "";
            _player.LastShotWeapon.Value = _weaponModel.GetNetName;


            // Cast a ray to check for collisions
            var maxAngle = weaponDistance2MaxAngleImprecision.Evaluate(_weaponModel.Distance / 100f);
            var randomImprecision =
                Quaternion.Euler(Random.Range(-maxAngle, maxAngle), Random.Range(-maxAngle, maxAngle), 0);
            var shootDir = (targetPos + Vector3.up * 0.2f - transform.position).normalized;
            var bulletDir = randomImprecision * shootDir;

            // Spawn the weapon effect
            if (_weaponModel.IsGun && _weaponModel.Type is not WeaponType.Tertiary)
                _player.SpawnWeaponEffectRpc(bulletDir, _weaponModel.BulletSpeed);

            if (_weaponModel.Type is WeaponType.Tertiary)
            {
                if (_weaponModel.Name.ToUpper() == "TACT")
                {
                    StartCoroutine(SpawnTACTMissiles());

                    IEnumerator SpawnTACTMissiles()
                    {
                        var centre = targetPos;
                        var model = _weaponModel!;
                        for (var i = 0; i < 8f / model.Delay; i++)
                        {
                            var range = _sm.highlightArea.Range / 2;
                            _sm.ServerManager.SpawnExplosiveServerRpc(
                                missile.name,
                                centre + new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range)) +
                                Vector3.up * 60,
                                new NetVector3(90, 0, 0),
                                Vector3.down,
                                model.Damage,
                                model.ExplosionTime!.Value,
                                model.ExplosionRange!.Value,
                                model.GroundDamageFactor!.Value,
                                _player.NetworkObjectId
                            );
                            yield return new WaitForSeconds(model.Delay);
                        }
                    }
                }
                else
                {
                    _sm.ServerManager.SpawnExplosiveServerRpc(
                        missile.name,
                        transform.position + Vector3.up * 0.3f + transform.forward * 0.5f,
                        transform.rotation.eulerAngles,
                        (transform.forward + Vector3.up * 0.8f).normalized,
                        _weaponModel.Damage,
                        _weaponModel.ExplosionTime!.Value,
                        _weaponModel.ExplosionRange!.Value,
                        _weaponModel!.GroundDamageFactor!.Value,
                        _player.NetworkObjectId
                    );
                }

                return;
            }

            var ray = new Ray(transform.position + Vector3.up * 0.2f + shootDir * 0.5f, bulletDir);

            // Checks if there was a hit on a prop
            var hasHitHostPlayer =
                Physics.Raycast(ray, out var hostPlayerHit, _weaponModel.Distance,
                    1 << LayerMask.NameToLayer("Self")) &&
                hostPlayerHit.collider is not null;
            var hasHitEnemy =
                Physics.Raycast(ray, out var enemyHit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Enemy")) &&
                enemyHit.collider is not null;
            var hasHitGround =
                Physics.Raycast(ray, out var groundHit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Ground")) &&
                groundHit.collider is not null;
            var hasHitProp =
                Physics.Raycast(ray, out var propHit, _weaponModel.Distance, 1 << LayerMask.NameToLayer("Prop")) &&
                propHit.collider is not null;

            // Checks if there was a hit on the host player
            if (hasHitHostPlayer && hostPlayerHit.distance < (hasHitGround ? groundHit.distance : 9999f) &&
                hostPlayerHit.distance < (hasHitProp ? propHit.distance : 9999f))
            {
                var attackedPlayer = hostPlayerHit.transform.GetComponentInParent<Player>();
                var multiplier = Model.Weapon.BodyPartMultipliers[hostPlayerHit.transform.gameObject.name];
                var distance = Vector3.Distance(transform.position, hostPlayerHit.collider.transform.position);
                var distanceFactor =
                    math.clamp((1f - distance / _weaponModel.Distance) * 2, 0.25f, 1f); // 1f ---> 0.25f
                var helmetHit = hostPlayerHit.transform.gameObject.name == "Head" &&
                                attackedPlayer.Status.Value.HasHelmet;

                var damage = (uint)(_weaponModel.Damage * multiplier *
                                    (_weaponModel.Distance < 100 ? distanceFactor : 1f) * (helmetHit ? 0.6f : 1f));

                if (!attackedPlayer.Status.Value.IsDead)
                {
                    // Check if the enemy is not allied nor invincible
                    if (((attackedPlayer.IsOwner && attackedPlayer.IsBot.Value) ||
                         attackedPlayer.Team != _player.Team) && !attackedPlayer.invincible.Value)
                    {
                        // Send the damage to the enemy
                        attackedPlayer.DamageClientRpc(
                            damage: damage,
                            bodyPart: hostPlayerHit.transform.gameObject.name,
                            direction: new NetVector3(bulletDir),
                            attackerID: _player.NetworkObjectId
                        );
                        _lastTargetHit = Time.time;
                    }
                }
            }

            // Checks if there was a hit on an enemy
            if (hasHitEnemy && enemyHit.distance < (hasHitGround ? groundHit.distance : 9999f) &&
                enemyHit.distance < (hasHitProp ? propHit.distance : 9999f))
            {
                var attackedPlayer = enemyHit.transform.GetComponentInParent<Player>();
                if (attackedPlayer is not null)
                {
                    var multiplier = Model.Weapon.BodyPartMultipliers[enemyHit.transform.gameObject.name];
                    var distance = Vector3.Distance(transform.position, enemyHit.collider.transform.position);
                    var distanceFactor =
                        math.clamp((1f - distance / _weaponModel.Distance) * 2, 0.25f, 1f); // 1f ---> 0.25f
                    var helmetHit = enemyHit.transform.gameObject.name == "Head" &&
                                    attackedPlayer.Status.Value.HasHelmet;

                    var damage = (uint)(_weaponModel.Damage * multiplier *
                                        (_weaponModel.Distance < 100 ? distanceFactor : 1f) * (helmetHit ? 0.6f : 1f));

                    if (!attackedPlayer.Status.Value.IsDead)
                    {
                        // Check if the enemy is not allied nor invincible
                        if (((attackedPlayer.IsOwner && attackedPlayer.IsBot.Value) ||
                             attackedPlayer.Team != _player.Team) && !attackedPlayer.invincible.Value)
                        {
                            // Send the damage to the enemy
                            attackedPlayer.DamageClientRpc(
                                damage: damage,
                                bodyPart: enemyHit.transform.gameObject.name,
                                direction: new NetVector3(bulletDir),
                                attackerID: _player.NetworkObjectId
                            );
                            _lastTargetHit = Time.time;
                        }
                    }
                }
            }

            // Checks if there was a hit on the ground
            if (hasHitGround && groundHit.distance < (hasHitEnemy ? enemyHit.distance : 9999f) &&
                groundHit.distance < (hasHitProp ? propHit.distance : 9999f))
            {
                // Check if the hit block is solid
                var pos = Vector3Int.FloorToInt(groundHit.point + transform.forward * 0.05f);
                var block = _sm.worldManager.GetVoxel(pos);
                if (block is not { isSolid: true }) return;

                // Broadcast the damage action
                _sm.ClientManager.DamageVoxelRpc(pos, _weaponModel.Damage);
            }

            // Checks if there was a hit on a prop
            if (hasHitProp && propHit.distance < (hasHitGround ? groundHit.distance : 9999f) &&
                propHit.distance < (hasHitEnemy ? enemyHit.distance : 9999f))
            {
                // Broadcast the damage action
                if (propHit.transform.TryGetComponent<Prop>(out var prop))
                    _sm.ClientManager.DamagePropRpc(prop.ID, _weaponModel.Damage, false, _player.NetworkObjectId);
            }
        }

        private void ThrowGrenade(float force, bool isSecondary = false)
        {
            var status = _player.Status.Value;
            var grenadeModel = isSecondary ? status.GrenadeSecondary : status.Grenade;
            var throwDir = State switch
            {
                AIState.Attacking => (Target.position - transform.position).normalized,
                AIState.Searching => (_lastKnownEnemyPosition!.Value - transform.position).normalized,
                _ => transform.forward
            };
            _sm.ServerManager.SpawnExplosiveServerRpc(
                grenadeModel!.Name.ToUpper(),
                transform.position + transform.forward * 1f + Vector3.down * 0.2f,
                VectorExtensions.RandomVector3(-180, 180f),
                throwDir + Vector3.up * Random.Range(-0.2f, 0.65f),
                grenadeModel!.Damage,
                grenadeModel!.ExplosionTime!.Value,
                grenadeModel!.ExplosionRange!.Value,
                grenadeModel!.GroundDamageFactor!.Value,
                _player.NetworkObjectId,
                force
            );
        }

        private void AutoChooseWeapon()
        {
            if (State is not AIState.Attacking && _blocksTargets.Count <= 0) return;
            var distanceToTarget = Vector3.Distance(transform.position,
                _blocksTargets.Count > 0 ? _blocksTargets[0] : Target.transform.position);
            if (distanceToTarget < meleeMaxDistance)
            {
                if (_weaponModel.Type is not WeaponType.Melee)
                    SwitchEquipped(WeaponType.Melee);
            }
            else if (distanceToTarget < secondaryMaxDistance)
            {
                if (_weaponModel.Type is not WeaponType.Secondary)
                    SwitchEquipped(WeaponType.Secondary);
            }
            else
            {
                if (_weaponModel.Type is not WeaponType.Primary)
                    SwitchEquipped(WeaponType.Primary);
            }
        }


        /// <summary>
        /// Check if any player falls within the visual cone, Regardless of any condition like Team.
        /// </summary>
        /// <returns>One visible player, if any</returns>
        [CanBeNull]
        private Player CheckForVisiblePlayers()
        {
            foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None)
                         .Where(player => player != _player && !player.Status.Value.IsDead).ToList().Shuffle())
            {
                var directionToPlayer = player.transform.position - transform.position;
                var distance = directionToPlayer.magnitude;
                var angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);
                // Check sphere + cone
                if (distance < sphereRange || (distance < coneRange && angle <= coneAngle))
                {
                    var ray = new Ray(transform.position + directionToPlayer * 0.5f, directionToPlayer);
                    var hasHitHostPlayer =
                        Physics.Raycast(ray, out var hostPlayerHit, distance + 1,
                            1 << LayerMask.NameToLayer("Self")) &&
                        hostPlayerHit.collider is not null;
                    var hasHitEnemy =
                        Physics.Raycast(ray, out var enemyHit, distance + 1, 1 << LayerMask.NameToLayer("Enemy")) &&
                        enemyHit.collider is not null;
                    if (hasHitHostPlayer || hasHitEnemy)
                        return player;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if I need to dig some player blocks to proceed
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="dir2"></param>
        private void CheckForDig(Vector3 from, Vector3 to, Vector2 dir2)
        {
            _blocksTargets.Clear();
            var top = Vector3Int.FloorToInt(transform.position) + Vector3Int.up;
            var bottom = Vector3Int.FloorToInt(transform.position) + Vector3Int.down;
            var middleForward =
                Vector3Int.FloorToInt(transform.position + transform.forward * 0.75f);
            var bottomForward = middleForward + Vector3Int.down;
            var topForward = middleForward + Vector3Int.up;
            if (_sm.worldManager.Map.GetBlock(middleForward).name.Contains("player_block"))
                _blocksTargets.Add(middleForward + Vector3.one * 0.5f);
            if (_sm.worldManager.Map.GetBlock(top).name.Contains("player_block"))
                _blocksTargets.Add(top + Vector3.one * 0.5f);
            if (dir2.sqrMagnitude < 0.1 && to.y - from.y < -0.5f)
            {
                if (_sm.worldManager.Map.GetBlock(bottom).name.Contains("player_block"))
                    _blocksTargets.Add(bottom + Vector3.one * 0.5f);
            }
            else if (to.y - from.y > 0.5f)
            {
                if (_sm.worldManager.Map.GetBlock(topForward).name.Contains("player_block"))
                    _blocksTargets.Add(topForward + Vector3.one * 0.5f);
            }
            else if (to.y - from.y < -0.5f)
            {
                if (_sm.worldManager.Map.GetBlock(bottomForward).name.Contains("player_block"))
                    _blocksTargets.Add(bottomForward + Vector3.one * 0.5f);
            }
        }

        #endregion

        #region public methods

        public void SwitchEquipped(WeaponType weaponType, bool silent = false)
        {
            _fireCount = 0;
            var weapon = _player.Status.Value.WeaponType2Weapon(weaponType);
            _player.EquippedWeapon.Value = $"{weapon!.Name}:{weapon!.Variant}";
            _weaponModel = weapon;
            if (weapon.IsGun)
                _magazine = _weaponModel.Magazine!.Value / Random.Range(1f, 3f);
            else if (weaponType is WeaponType.Melee)
                _magazine = 7;

            // Wait for switch Equip animation to finish
            _fireAcc = -0.8f;

            // Play switch sound
            if (!silent)
            {
                _player.MiscSound.Value = 0;
                _player.MiscSound.Value = (byte)_player.MiscClips.IndexOf(_player.switchEquippedClip);
            }
        }

        public void SwitchState(AIState newState)
        {
            _sm.logger.Log($"{gameObject.name} - SwitchState() to {newState}....");
            if (State == newState)
                return;
            if (_switchStateCoroutine != null)
            {
                StopCoroutine(_switchStateCoroutine);
                _switchStateCoroutine = null;
                Debug.LogWarning(
                    $"Requested SwitchState({newState}), but a coroutine is already pending, aborting the previous one");
            }

            var delay = newState switch
            {
                AIState.Attacking => Random.Range(0.025f, 0.2f),
                AIState.Searching => Random.Range(0.025f, 0.1f),
                _ => 0
            };

            _switchStateCoroutine = StartCoroutine(SwitchStateCoroutine());
            return;

            IEnumerator SwitchStateCoroutine()
            {
                yield return new WaitForSeconds(delay);
                // Reinitialize AI state
                _currentPath = null;

                if (newState is AIState.Searching)
                    _searchingStart = Time.time;

                // Reset any target
                if (newState is not AIState.Searching)
                    _lastKnownEnemyPosition = null;
                if (newState is not AIState.Attacking)
                    Target = null;

                // Equip primary
                if (newState is AIState.Patrolling && _weaponModel is not null &&
                    _weaponModel.Type is not WeaponType.Primary)
                    SwitchEquipped(WeaponType.Primary);

                // Reset Attack timeout
                if (newState is AIState.Attacking)
                {
                    _lastTargetHit = Time.time;
                    _fireAcc = _weaponModel.Delay * 0.5f;
                }

                State = newState;
                _switchStateCoroutine = null;
            }
        }

        public void Alert(Transform playerToAttack, bool hasSeenIt)
        {
            if (State is AIState.Attacking)
            {
                if (playerToAttack == Target)
                    return;
                if (Random.Range(0f, 1f) < 0.5f)
                    return;
            }

            Debug.LogWarning(
                $"{gameObject.name} - has been alerted for {playerToAttack.gameObject.name}, hasSeenIt:{hasSeenIt}");

            if (hasSeenIt)
            {
                Target = playerToAttack;
                SwitchState(AIState.Attacking);
            }
            else
            {
                _lastKnownEnemyPosition = Vector3Int.FloorToInt(playerToAttack.position + Vector3.down * 0.5f);
                SwitchState(AIState.Searching);
            }
        }

        #endregion
    }
}