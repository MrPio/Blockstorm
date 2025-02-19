using System.Collections.Generic;
using ExtensionFunctions;
using Managers;
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
        private SceneManager _sm;
        private Player _player;

        private readonly Vector2 _moveRange = new(25, 100);
        private const float LogicStep = 1f / 15; // 15 FPS
        private const bool DebugMode = false;

        private AIState _state = AIState.Patrolling;
        private Vector3 _lastKnownEnemyPosition;
        private Transform _target = null;
        private List<Vector3Int> _currentPath;
        private int _currentPathIndex;
        private float _acc;

        private void Awake()
        {
            _sm = FindFirstObjectByType<SceneManager>();
            _player = GetComponent<Player>();
        }

        /// <summary>
        /// Choose a random path to follow
        /// </summary>
        private List<Vector3Int> ChoosePath()
        {
            // Select a random point in the map, in the circle around the player
            Vector3Int dest;
            do
            {
                dest = Vector3Int.RoundToInt(transform.position +
                                             VectorExtensions.RandomVector3(-1, 1) *
                                             Random.Range(_moveRange.x, _moveRange.y));
                dest.y = 0;
            } while (!_sm.worldManager.IsVoxelInWorld(dest));

            // Find a valid y
            for (dest.y = 0; dest.y < _sm.worldManager.Map.size.y - 1; dest.y++)
                if (!VoxelData.BlockTypes[_sm.worldManager.Map.Blocks[dest.y, dest.x, dest.z]].isSolid &&
                    !VoxelData.BlockTypes[_sm.worldManager.Map.Blocks[dest.y + 1, dest.x, dest.z]].isSolid)
                    break;

            // Valid y not found
            if (dest.y >= _sm.worldManager.Map.size.y - 2) return null;
            return _sm.worldManager.Map.Pathfinder.FindPath(
                Vector3Int.RoundToInt(transform.position + Vector3.down * 0.5f), dest);
        }

        private void FixedUpdate()
        {
            if (Time.time < 3)
                return;
            // Ensure the algorithm is run every _logicStep
            _acc += Time.deltaTime;
            if (_acc < LogicStep)
                return;
            _acc = 0;

            // AI Finite State Machine
            if (_state is AIState.Patrolling)
            {
                // I've got no path to follow
                if (_currentPath == null)
                {
                    _currentPath = ChoosePath();
                    if (_currentPath != null)
                    {
                        _currentPathIndex = 1;

                        // Debug -- spawn path
                        if (DebugMode)
                            foreach (var point in _currentPath)
                                Instantiate(_sm.pathPointPrefab, point + Vector3.one * 0.5f,
                                    Quaternion.identity);
                    }
                }
                // I'm currently following a path
                else
                {
                    var from = _currentPath[_currentPathIndex - 1] + Vector3.one * 0.5f;
                    var to = _currentPath[_currentPathIndex] + Vector3.one * 0.5f;
                    var dir = (Vector2)new Vector2XZ(transform.position - to);
                    var dist = dir.magnitude;
                    print($"dir = {dir}, dist = {dist}");
                    
                    // Set rotation along the direction
                    _player.transform.rotation =
                        Quaternion.AngleAxis(Mathf.Atan2(-dir.x, -dir.y) * Mathf.Rad2Deg, Vector3.up);

                    // End of the current dir
                    if (dist < 0.65)
                    {
                        _currentPathIndex++;

                        // End of the current path
                        if (_currentPathIndex >= _currentPath.Count)
                            _currentPath = null;
                        // Jump if there's a stair
                        else if (_currentPath[_currentPathIndex].y > _currentPath[_currentPathIndex - 1].y)
                        {
                            _player.InputInterface.IsJumpDown = true;
                            _currentPathIndex++;
                        }
                    }
                    // Still pursuing the current dir
                    else
                    {
                        // Move if not falling
                        if (Mathf.Abs(from.y - to.y) < 0.2f)
                        {
                            // Go forward
                            _player.InputInterface.Axis = new Vector2(0, 1);
                            print($"InputInterface.Axis= {_player.InputInterface.Axis}");
                        }
                        else
                            _player.InputInterface.Axis = Vector2.zero;
                    }
                }
            }
        }
    }
}