using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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
using UnityEngine.Profiling;
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

        private readonly Dictionary<AIState, float> _grenadeProbability = new()
        {
            { AIState.Patrolling, 0.001f },
            { AIState.Attacking, 0.0075f },
        };

        #endregion

        #region serializable

        [SerializeField] private AnimationCurve weaponDistance2MaxAngleImprecision;
        [SerializeField] private float rotationYSmoothness;

        #endregion

        #region private

        [NonSerialized] public Transform Target = null;
        private SceneManager _sm;
        private Player _player;
        private AIState _state = AIState.Dead;
        private Vector3 _lastKnownEnemyPosition;
        private List<Vector3Int> _currentPath;
        private bool _isSearchingPath;
        private Thread _pathThread;
        private float _lastPathThreadDuration;
        private int _currentPathIndex, _fireCount;
        private float _acc, _speedChangeAcc, _patrollingPropCheckAcc, _indexAcc, _fireAcc;
        private float _baseSpeed, _magazine;
        private Vector2 _lookDir;
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

        private void FixedUpdate()
        {
            // Shoot ========================================================
            if (_state is AIState.Attacking)
            {
                _fireAcc += Time.deltaTime;
                if (_fireAcc > _weaponModel.Delay)
                {
                    StartCoroutine(Fire());
                    _fireAcc = 0;
                }

                // Reload
                if (_fireCount > math.max(1f, _magazine))
                {
                    _fireCount = 0;
                    _fireAcc = -_weaponModel.ReloadTime!.Value / 100f;
                    _magazine = _weaponModel.Magazine!.Value / Random.Range(1f, 2f);
                }
            }

            // Walk =========================================================
            // Ensure the walking algorithm is run every _logicStep
            _acc += Time.deltaTime;
            if (_acc > LogicStep)
            {
                _acc = 0;
                if (_state is AIState.Patrolling or AIState.Attacking)
                {
                    // I've got no path to follow
                    if (_currentPath == null || _currentPath.Count < 3)
                    {
                        if (_pathThread is not { IsAlive: true })
                        {
                            var currentPos = transform.position;
                            var targetPos = Target?.position ?? Vector3.zero;
                            _pathThread = new Thread(() => ChoosePath(currentPos, targetPos));
                            // await Task.Run(() => ChoosePath(currentPos, targetPos));
                            _pathThread.Start();
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
                            if (DebugMode)
                                foreach (var point in _currentPath)
                                    Instantiate(_sm.pathPointPrefab, point + Vector3.one * 0.5f,
                                        Quaternion.identity);
                        }

                        var from = _currentPath[_currentPathIndex - 1] + Vector3.one * 0.5f;
                        var to = _currentPath[_currentPathIndex] + Vector3.one * 0.5f;
                        var dir = (Vector2)new Vector2XZ(transform.position - to);
                        _lookDir = _state is AIState.Patrolling
                            ? dir // look forward
                            : (Vector2)new Vector2XZ(transform.position - Target.position); // look at the target
                        var dist = dir.magnitude;

                        // Set x head rotation
                        if (_state is AIState.Attacking)
                        {
                            var fullLookDir = Target.position - transform.position;
                            var deltaAngleX = Quaternion.LookRotation(fullLookDir).eulerAngles.x;
                            deltaAngleX = deltaAngleX > 180f ? deltaAngleX - 360f : deltaAngleX;
                            _player.CameraRotationX.Value = (byte)((int)deltaAngleX + 128);
                        }

                        // End of the current dir
                        if (dist < 0.75)
                        {
                            _currentPathIndex++;
                            _indexAcc = 0;

                            // End of the current path
                            if (_currentPathIndex >= _currentPath.Count - 1)
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
                                var deltaAngle = (Mathf.Atan2(dir.y, dir.x) - Mathf.Atan2(_lookDir.y, _lookDir.x)) *
                                                 Mathf.Rad2Deg;
                                _player.InputInterface.Axis = new Vector2(0, 1).RotateByAngle(deltaAngle);

                                // Too much time on current point, retry...
                                _indexAcc += LogicStep;
                                if (_indexAcc > 2 && _currentPathIndex > 1)
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

                            // Random grenade
                            if (Random.value < _grenadeProbability[_state])
                                ThrowGrenade(Random.Range(0.25f, 0.5f), Random.value < 0.4f);

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

                            // Check placed blocks to destroy
                            // TODO
                        }
                    }
                }
            }

            // Set y rotation along the direction with smoothness
            _player.transform.rotation = Quaternion.Slerp(
                _player.transform.rotation,
                Quaternion.LookRotation(new Vector3(-_lookDir.x, 0, -_lookDir.y)),
                1f / (1f + rotationYSmoothness)
            );
        }

        
        /// <summary>
        /// Choose a random path to follow
        /// </summary>
        private void ChoosePath(Vector3 currentPos, Vector3 targetPos)
        {
            var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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

                // TODO this
                // dest = new(_sm.worldManager.Map.size.x / 2, 0, _sm.worldManager.Map.size.x / 2);

                // Find a valid y
                for (dest.y = 0; dest.y < _sm.worldManager.Map.size.y - 1; dest.y++)
                    if (!VoxelData.BlockTypes[_sm.worldManager.Map.Blocks[dest.y, dest.x, dest.z]].isSolid &&
                        !VoxelData.BlockTypes[_sm.worldManager.Map.Blocks[dest.y + 1, dest.x, dest.z]].isSolid)
                        break;

                // Valid y condition
                validDestFound = dest.y < _sm.worldManager.Map.size.y - 2;
            }

            List<Vector3Int> path = null;
            path = new AStarPathfinder(_sm.worldManager.Map).FindPath(
                Vector3Int.FloorToInt(currentPos + Vector3.down * 0.75f), dest);
            if (path != null)
                _currentPathIndex = 1;

            _currentPath = path;
            _lastPathThreadDuration = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - start) / 1000f;
        }

        private IEnumerator Fire()
        {
            _fireCount++;

            // Propagate the sound across the net
            _player.LastShotWeapon.Value = "";
            _player.LastShotWeapon.Value = _weaponModel.GetNetName;


            // Cast a ray to check for collisions
            var maxAngle = weaponDistance2MaxAngleImprecision.Evaluate(_weaponModel.Distance / 100f);
            var randomImprecision =
                Quaternion.Euler(Random.Range(-maxAngle, maxAngle), Random.Range(-maxAngle, maxAngle), 0);
            var shootDir = (Target.position + Vector3.up * 0.2f - transform.position).normalized;
            var bulletDir = randomImprecision * shootDir;

            // Spawn the weapon effect
            if (_weaponModel.IsGun)
            {
                _player.SpawnWeaponEffectRpc(bulletDir, _weaponModel.BulletSpeed);
                yield return new WaitForSeconds(0.05f);
            }

            var ray = new Ray(transform.position + shootDir * 0.5f, bulletDir);

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
                                bodyPart: hostPlayerHit.transform.gameObject.name,
                                direction: new NetVector3(bulletDir),
                                attackerID: _player.NetworkObjectId
                            );
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
                if (block is not { isSolid: true }) yield break;

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

        public void ThrowGrenade(float force, bool isSecondary = false)
        {
            var status = _player.Status.Value;
            var grenadeModel = isSecondary ? status.GrenadeSecondary : status.Grenade;
            var throwDir = _state is AIState.Attacking
                ? (Target.position - transform.position).normalized
                : transform.forward;
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

        #region public methods

        public void SwitchEquipped(WeaponType weaponType)
        {
            _fireCount = 0;
            var weapon = _player.Status.Value.WeaponType2Weapon(weaponType);
            print($"------{weapon.GetNetName}");
            _player.EquippedWeapon.Value = $"{weapon!.Name}:{weapon!.Variant}";
            _weaponModel = weapon;
            _magazine = _weaponModel.Magazine!.Value / Random.Range(1f, 3f);
        }

        public void SwitchState(AIState newState)
        {
            if (_state == newState) return;
            var delay = 0f;
            if (newState is AIState.Attacking)
                delay = Random.Range(0.05f, 0.275f);
            StartCoroutine(SwitchStateCoroutine());
            return;

            IEnumerator SwitchStateCoroutine()
            {
                yield return new WaitForSeconds(delay);
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

        #endregion
    }
}