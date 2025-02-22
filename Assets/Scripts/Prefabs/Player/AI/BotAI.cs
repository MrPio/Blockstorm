using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExtensionFunctions;
using Managers;
using Model;
using Network;
using Partials;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Utils;
using VoxelEngine;
using Random = UnityEngine.Random;

namespace Prefabs.Player.AI
{
    public enum AIState
    {
        Dead,
        Patrolling,
        Attacking
    }

    public class BotAI : MonoBehaviour
    {
        #region constants

        private const bool DebugMode = false;
        private const float LogicStep = 1f / 15; // 15 FPS

        // Patrolling
        private readonly Dictionary<AIState, Vector2> _movingRange = new()
        {
            { AIState.Patrolling, new Vector2(15, 65) },
            { AIState.Attacking, new Vector2(5, 40) },
        };

        private readonly Dictionary<AIState, Vector2> _speedFactorRange = new()
        {
            { AIState.Patrolling, new Vector2(0.5f, 1f) },
            { AIState.Attacking, new Vector2(0.9f, 1.3f) },
        };

        private readonly Dictionary<AIState, float> _speedChangeStep = new()
        {
            { AIState.Patrolling, 5 },
            { AIState.Attacking, 4 },
        };

        private readonly Dictionary<AIState, float> _propCheckStep = new()
        {
            { AIState.Patrolling, 15 },
            { AIState.Attacking, 7 },
        };

        private readonly Dictionary<AIState, float> _jumpProbability = new()
        {
            { AIState.Patrolling, 0.05f },
            { AIState.Attacking, 0.25f },
        };

        #endregion

        #region private

        [NonSerialized] public Transform Target = null;
        private SceneManager _sm;
        private Player _player;
        private AIState _state = AIState.Dead;
        private Vector3 _lastKnownEnemyPosition;
        private List<Vector3Int> _currentPath;
        private int _currentPathIndex, _fireCount;
        private float _acc, _speedChangeAcc, _patrollingPropCheckAcc, _indexAcc, _fireAcc;
        private float _baseSpeed,_magazine;
        private Model.Weapon _weaponModel;

        #endregion

        private void Awake()
        {
            _sm = FindFirstObjectByType<SceneManager>();
            _player = GetComponent<Player>();
        }

        private void Start()
        {
            _baseSpeed = _player.speed;
        }

        /// <summary>
        /// Choose a random path to follow
        /// </summary>
        private void ChoosePath(Vector3 currentPos, Vector3 targetPos)
        {
            // Select a random point in the map, in the circle around the player
            var validDestFound = false;
            var dest = Vector3Int.zero;
            var iterations = 0;
            while (!validDestFound)
            {
                iterations++;
                if (iterations > 20)
                {
                    Debug.LogError("Bot ChoosePath() disabled due to more than 20 iterations.");
                    // I block the moving avoid setting _choosePathCoroutine = null;
                    return;
                }

                do
                {
                    if (_state is AIState.Patrolling)
                        dest = Vector3Int.FloorToInt(currentPos +
                                                     VectorExtensions.RandomVector3(-1, 1) *
                                                     _movingRange[AIState.Patrolling].RandomRange());
                    else if (_state is AIState.Attacking)
                        dest = Vector3Int.FloorToInt(targetPos +
                                                     VectorExtensions.RandomVector3(-1, 1) *
                                                     _movingRange[AIState.Patrolling].RandomRange());

                    dest.y = 0;
                } while (!_sm.worldManager.IsVoxelInWorld(dest));

                // Find a valid y
                for (dest.y = 0; dest.y < _sm.worldManager.Map.size.y - 1; dest.y++)
                    if (!VoxelData.BlockTypes[_sm.worldManager.Map.Blocks[dest.y, dest.x, dest.z]].isSolid &&
                        !VoxelData.BlockTypes[_sm.worldManager.Map.Blocks[dest.y + 1, dest.x, dest.z]].isSolid)
                        break;

                // Valid y condition
                validDestFound = dest.y < _sm.worldManager.Map.size.y - 2;
            }

            var path = _sm.worldManager.Map.Pathfinder.FindPath(
                Vector3Int.FloorToInt(currentPos + Vector3.down * 0.75f), dest);
            if (path != null)
            {
                _currentPathIndex = 1;

                // Debug -- spawn path
                if (DebugMode)
                    foreach (var point in path)
                        Instantiate(_sm.pathPointPrefab, point + Vector3.one * 0.5f,
                            Quaternion.identity);
            }

            _currentPath = path;
        }

        private async void FixedUpdate()
        {
            // Shoot
            if (_state is AIState.Attacking)
            {
                _fireAcc += Time.deltaTime;
                if (_fireAcc > _weaponModel.Delay)
                {
                    Fire();
                    _fireAcc = 0;
                }

                // Reload
                if (_fireCount > math.max(1f, _magazine))
                {
                    _fireCount = 0;
                    _fireAcc = -_weaponModel.ReloadTime!.Value / 100f;
                    _magazine = _weaponModel.Magazine!.Value / Random.Range(1f, 3f);
                }
            }
            
            // Ensure the walking algorithm is run every _logicStep
            _acc += Time.deltaTime;
            if (_acc < LogicStep)
                return;
            _acc = 0;

            // AI Finite State Machine
            if (_state is AIState.Patrolling or AIState.Attacking)
            {
                // I've got no path to follow
                if (_currentPath == null || _currentPath.Count < 3)
                {
                    print("===================");
                    var currentPos = transform.position;
                    var targetPos = Target?.position ?? Vector3.zero;
                    await Task.Run(() => ChoosePath(currentPos, targetPos));
                }
                // I'm currently following a path
                else
                {
                    var from = _currentPath[_currentPathIndex - 1] + Vector3.one * 0.5f;
                    var to = _currentPath[_currentPathIndex] + Vector3.one * 0.5f;
                    var dir = (Vector2)new Vector2XZ(transform.position - to);
                    var lookDir = _state is AIState.Patrolling
                        ? dir // look forward
                        : (Vector2)new Vector2XZ(transform.position - Target.position); // look at the target
                    var dist = dir.magnitude;

                    // Set y rotation along the direction
                    _player.transform.rotation =
                        Quaternion.AngleAxis(Mathf.Atan2(-lookDir.x, -lookDir.y) * Mathf.Rad2Deg, Vector3.up);

                    // Set x head rotation
                    if (_state is AIState.Attacking)
                    {
                        var fullLookDir = Target.position - transform.position;
                        var deltaAngleX = Quaternion.LookRotation(fullLookDir).eulerAngles.x;
                        deltaAngleX = deltaAngleX > 180f ? deltaAngleX - 360f : deltaAngleX;
                        _player.CameraRotationX.Value = (byte)((int)deltaAngleX + 128);
                    }

                    // End of the current dir
                    if (dist < 0.55)
                    {
                        _currentPathIndex++;
                        _indexAcc = 0;

                        // End of the current path
                        if (_currentPathIndex >= _currentPath.Count)
                            _currentPath = null;
                        // Jump if there's a stair
                        else if (_currentPath[_currentPathIndex].y > _currentPath[_currentPathIndex - 1].y)
                        {
                            _player.InputInterface.IsJumpDown = true;
                            _currentPathIndex++;
                            _indexAcc = 0;
                        }
                    }
                    // Still pursuing the current dir
                    else
                    {
                        // Move if not falling
                        if (from.y - to.y < 0.2f)
                        {
                            // Go along the path
                            var deltaAngle = (Mathf.Atan2(dir.y, dir.x) - Mathf.Atan2(lookDir.y, lookDir.x)) *
                                             Mathf.Rad2Deg;
                            _player.InputInterface.Axis = new Vector2(0, 1).RotateByAngle(deltaAngle);

                            // Too much time on current point, retry...
                            _indexAcc += LogicStep;
                            if (_indexAcc > 3 && _currentPathIndex > 1)
                            {
                                _currentPathIndex--;
                                _indexAcc = 0;
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
                        if (_speedChangeAcc > _speedChangeStep[_state])
                        {
                            _speedChangeAcc = 0;
                            _player.speed = _baseSpeed * _speedFactorRange[_state].RandomRange();
                        }

                        // Random jump
                        if (Random.value < _jumpProbability[_state])
                            _player.InputInterface.IsJumpDown = true;

                        // Check prop to destroy
                        _patrollingPropCheckAcc += LogicStep;
                        if (_patrollingPropCheckAcc > _propCheckStep[_state])
                        {
                            _patrollingPropCheckAcc = 0;
                            foreach (var prop in _sm.worldManager.SpawnedProps)
                            {
                                if (prop.IsDestroyed()) continue;

                                if (Vector3.Distance(prop.transform.position, transform.position) <= 2.5)
                                {
                                    // Broadcast the damage action
                                    _sm.ClientManager.DamagePropRpc(prop.ID, 9999, true, _player.NetworkObjectId);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Fire()
        {
            _fireCount++;

            // Propagate the sound across the net
            _player.LastShotWeapon.Value = "";
            _player.LastShotWeapon.Value = _weaponModel.GetNetName;

            // Spawn the weapon effect
            if (_weaponModel.IsGun && !_weaponModel.HasScope)
                _player.SpawnWeaponEffectRpc();

            // Cast a ray to check for collisions
            var cameraTransform = transform;
            var ray = new Ray(cameraTransform.position + cameraTransform.forward * 0.5f, cameraTransform.forward);

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
                        attackedPlayer.DamageClientRpc(damage, hostPlayerHit.transform.gameObject.name,
                            new NetVector3(cameraTransform.forward),
                            _player.NetworkObjectId);
                    }
                }
            }

            // Checks if there was a hit on an enemy
            if (hasHitEnemy && enemyHit.distance < (hasHitGround ? groundHit.distance : 9999f) &&
                enemyHit.distance < (hasHitProp ? propHit.distance : 9999f))
            {
                var attackedPlayer = enemyHit.transform.GetComponentInParent<Player>();
                var multiplier = Model.Weapon.BodyPartMultipliers[enemyHit.transform.gameObject.name];
                var distance = Vector3.Distance(transform.position, enemyHit.collider.transform.position);
                var distanceFactor =
                    math.clamp((1f - distance / _weaponModel.Distance) * 2, 0.25f, 1f); // 1f ---> 0.25f
                var helmetHit = enemyHit.transform.gameObject.name == "Head" && attackedPlayer.Status.Value.HasHelmet;

                var damage = (uint)(_weaponModel.Damage * multiplier *
                                    (_weaponModel.Distance < 100 ? distanceFactor : 1f) * (helmetHit ? 0.6f : 1f));

                if (!attackedPlayer.Status.Value.IsDead)
                {
                    // Check if the enemy is not allied nor invincible
                    if (((attackedPlayer.IsOwner && attackedPlayer.IsBot.Value) ||
                         attackedPlayer.Team != _player.Team) && !attackedPlayer.invincible.Value)
                    {
                        // Send the damage to the enemy
                        attackedPlayer.DamageClientRpc(damage, enemyHit.transform.gameObject.name,
                            new NetVector3(cameraTransform.forward),
                            _player.NetworkObjectId);
                    }
                }
            }

            // Checks if there was a hit on the ground
            if (hasHitGround && groundHit.distance < (hasHitEnemy ? enemyHit.distance : 9999f) &&
                groundHit.distance < (hasHitProp ? propHit.distance : 9999f))
            {
                // Check if the hit block is solid
                var pos = Vector3Int.FloorToInt(groundHit.point + cameraTransform.forward * 0.05f);
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

        public void SwitchEquipped(WeaponType weaponType)
        {
            _fireCount = 0;
            var weapon = _player.Status.Value.WeaponType2Weapon(weaponType);
            _player.EquippedWeapon.Value = $"{weapon!.Name}:{weapon!.Variant}";
            _weaponModel = weapon;
            _magazine = _weaponModel.Magazine!.Value / Random.Range(1f, 3f);
        }

        public void SwitchState(AIState newState)
        {
            if (_state == newState) return;
            if (newState is AIState.Dead)
            {
                // Reinitialize AI state
                _currentPath = null;
            }
            else if (_state is AIState.Patrolling && newState is AIState.Attacking)
            {
                _currentPath = null;
            }
            else if (_state is AIState.Attacking && newState is AIState.Patrolling)
            {
                _currentPath = null;
            }

            _state = newState;
        }
    }
}