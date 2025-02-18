using System;
using System.Collections.Generic;
using ExtensionFunctions;
using Managers;
using UnityEngine;
using VoxelEngine;
using Random = UnityEngine.Random;

namespace Prefabs.Player
{
    public enum AIState
    {
        Roaming,
        Attacking
    }

    public class BotAI : MonoBehaviour
    {
        private SceneManager _sm;
        private Player _player;

        private readonly Vector2 _moveRange = new(25, 100);

        private AIState _state = AIState.Roaming;
        private Vector3 _lastKnownEnemyPosition;
        private Transform _target = null;
        private List<Vector3Int> _currentPath;
        private List<Vector3> _currentPathDirs;
        private int _currentPathDirIndex;

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

        private void Update()
        {
            if (_state is AIState.Roaming)
            {
                // I've got no path to follow
                if (_currentPath == null)
                {
                    _currentPath = ChoosePath();
                    if (_currentPath != null)
                    {
                        _currentPathDirs = new List<Vector3>();
                        for (var i = 1; i < _currentPath.Count; i++)
                            _currentPathDirs.Add((Vector3)_currentPath[i] - _currentPath[i - 1]);
                        _currentPathDirIndex = 1;

                        // Debug
                        foreach (var point in _currentPath)
                            Instantiate(_sm.pathPointPrefab, point + Vector3.one * 0.5f,
                                Quaternion.identity);
                    }
                }
                // I'm currently following a path
                else
                {
                    var currentDir = new Vector2(_currentPathDirs[_currentPathDirIndex].x,
                        _currentPathDirs[_currentPathDirIndex].z).normalized;
                    var currentDistVector3 = _currentPath[_currentPathDirIndex + 1] - transform.position;
                    var currentDist = new Vector2(currentDistVector3.x, currentDistVector3.z).normalized;
                    var currentAlignment = Vector2.Dot(currentDist, currentDir);
                    print($"previousPoint= {_currentPath[_currentPathDirIndex]}");
                    print($"currentPoint= {_currentPath[_currentPathDirIndex + 1]}");
                    print($"currentDir= {currentDir}");
                    print($"currentDist= {currentDist}");
                    print($"currentAlignment= {currentAlignment}");
                    print($"currentPathDirIndex= {_currentPathDirIndex}");

                    // Set rotation along the direction
                    Debug.LogWarning(Mathf.Atan2(currentDir.x, currentDir.y) * Mathf.Rad2Deg);
                    if (currentDir != Vector2.zero)
                        _player.transform.rotation =
                            Quaternion.AngleAxis(Mathf.Atan2(currentDir.x, currentDir.y) * Mathf.Rad2Deg, Vector3.up);

                    // End of the current dir
                    if (currentAlignment < 0)
                    {
                        _currentPathDirIndex++;
                        print($"Next point = {_currentPath[_currentPathDirIndex + 1]}");

                        // End of the current path
                        if (_currentPathDirIndex >= _currentPathDirs.Count)
                            _currentPath = null;
                        // Jump if there's a stair
                        else if (_currentPathDirs[_currentPathDirIndex].y >
                                 _currentPathDirs[_currentPathDirIndex - 1].y)
                        {
                            _player.InputInterface.IsJumpDown = true;
                            _currentPathDirIndex++;
                        }
                    }
                    // Still pursuing the current dir
                    else
                    {
                        // Move if not falling
                        if (Mathf.Abs(_currentPathDirs[_currentPathDirIndex].y) < 0.2f)
                        {
                            _player.InputInterface.Axis = currentDist;
                            print($"InputInterface.Axis= {_player.InputInterface.Axis}");
                        }
                    }
                }
            }
        }
    }
}