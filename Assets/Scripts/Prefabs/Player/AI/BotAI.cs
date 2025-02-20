using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Unity.VisualScripting;
using UnityEngine;
using Utils;
using VoxelEngine;
using Random = UnityEngine.Random;

namespace Prefabs.Player.AI
{
    public enum AIState
    {
        Patrolling,
        Attacking
    }

    public class BotAI : MonoBehaviour
    {
        #region constants

        private const bool DebugMode = true;
        private const float LogicStep = 1f / 15; // 15 FPS

        // Patrolling
        private readonly Dictionary<AIState, Vector2> _movingRange = new()
            {
                { AIState.Patrolling, new Vector2(15, 65) },
                { AIState.Attacking, new Vector2(5, 40) },
            },
            _speedFactorRange = new()
            {
                { AIState.Patrolling, new Vector2(0.5f, 1f) },
                { AIState.Attacking, new Vector2(0.9f, 1.3f) },
            };

        private readonly Dictionary<AIState, float> _speedChangeStep = new()
            {
                { AIState.Patrolling, 5 },
                { AIState.Attacking, 4 },
            },
            _propCheckStep = new()
            {
                { AIState.Patrolling, 15 },
                { AIState.Attacking, 7 },
            },
            _jumpProbability = new()
            {
                { AIState.Patrolling, 0.05f },
                { AIState.Attacking, 0.085f },
            };

        #endregion

        #region private

        private SceneManager _sm;
        private Player _player;
        private AIState _state = AIState.Attacking;
        private Vector3 _lastKnownEnemyPosition;
        private Transform _target = null;
        private List<Vector3Int> _currentPath;
        private int _currentPathIndex;
        private float _acc, _speedChangeAcc, _patrollingPropCheckAcc, _indexAcc;
        private float _baseSpeed;
        private Coroutine _choosePathCoroutine;

        #endregion

        private void Awake()
        {
            _sm = FindFirstObjectByType<SceneManager>();
            _player = GetComponent<Player>();
        }

        private void Start()
        {
            _baseSpeed = _player.speed;
            _target = FindObjectsOfType<Player>().First(it => it.IsOwner && !it.IsBot.Value).transform;
        }

        /// <summary>
        /// Choose a random path to follow
        /// </summary>
        private IEnumerator ChoosePath()
        {
            // Select a random point in the map, in the circle around the player
            var validDestFound = false;
            var dest = Vector3Int.zero;
            var iterations = 0;
            while (!validDestFound)
            {
                iterations++;
                if (iterations > 25)
                {
                    Debug.LogError("Bot ChoosePath() disabled due to more than 25 iterations.");
                    // I block the moving avoid setting _choosePathCoroutine = null;
                    yield break;
                }

                do
                {
                    if (_state is AIState.Patrolling)
                        dest = Vector3Int.FloorToInt(transform.position + Vector3.down * 0.75f +
                                                     VectorExtensions.RandomVector3(-1, 1) *
                                                     _movingRange[AIState.Patrolling].RandomRange());
                    else if (_state is AIState.Attacking)
                        dest = Vector3Int.FloorToInt(_target.position + Vector3.down * 0.75f +
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
                Vector3Int.FloorToInt(transform.position + Vector3.down * 0.75f), dest);
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
            _choosePathCoroutine = null;
        }

        private void FixedUpdate()
        {
            // Ensure the algorithm is run every _logicStep
            _acc += Time.deltaTime;
            if (_acc < LogicStep)
                return;
            _acc = 0;

            // AI Finite State Machine
            if (_state is AIState.Patrolling or AIState.Attacking)
            {
                // I've got no path to follow
                if (_currentPath == null)
                {
                    if (_choosePathCoroutine == null)
                        _choosePathCoroutine = StartCoroutine(ChoosePath());
                }
                // I'm currently following a path
                else
                {
                    var from = _currentPath[_currentPathIndex - 1] + Vector3.one * 0.5f;
                    var to = _currentPath[_currentPathIndex] + Vector3.one * 0.5f;
                    var dir = (Vector2)new Vector2XZ(transform.position - to);
                    var lookDir = _state is AIState.Patrolling
                        ? dir // look forward
                        : (Vector2)new Vector2XZ(transform.position - _target.position); // look at the target
                    var dist = dir.magnitude;

                    // Set y rotation along the direction
                    _player.transform.rotation =
                        Quaternion.AngleAxis(Mathf.Atan2(-lookDir.x, -lookDir.y) * Mathf.Rad2Deg, Vector3.up);

                    // Set x head rotation
                    if (_state is AIState.Attacking)
                    {
                        var fullLookDir = _target.position - transform.position;
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
                                    _sm.ClientManager.DamagePropRpc(prop.ID, 9999, true, _player.OwnerClientId);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ChangeState(AIState newState)
        {
            if (_state is AIState.Patrolling && newState is AIState.Attacking)
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