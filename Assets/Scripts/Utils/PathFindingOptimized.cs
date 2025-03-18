using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Model;
using NUnit.Framework;
using UnityEngine;
using VoxelEngine;
using Random = System.Random;

namespace Utils
{
    /// <summary>
    /// Implementation of A* algorithm using a PriorityQueue,
    /// optimized to reduce allocations and CPU overhead.
    /// </summary>
    public class AStarPathfinderOptimized
    {
        public enum Heuristics
        {
            Euclidean,
            Manhattan
        }

        private struct DiggablePos
        {
            public Vector3Int Pos;
            public bool RequiresDig;

            public DiggablePos(Vector3Int pos, bool requiresDig = false)
            {
                Pos = pos;
                RequiresDig = requiresDig;
            }
        }

        // Base directions (cardinal and diagonal).
        private static readonly Vector3Int[] BaseDirections =
        {
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 0, 1),
            new(0, 0, -1)
        };

        private static readonly Vector3Int[] BaseDiagonalDirections =
        {
            new(1, 0, 1),
            new(1, 0, -1),
            new(-1, 0, 1),
            new(-1, 0, -1)
        };

        // Direction arrays that get randomized per call.
        private Vector3Int[] _directions, _diagonalDirections;
        private byte[,,] Blocks => _map.Blocks;
        private Map _map;
        private Heuristics _heuristics;

        public AStarPathfinderOptimized(Map map, Heuristics heuristics = Heuristics.Euclidean)
        {
            _map = map;
            this._heuristics = heuristics;
        }

        /// <summary>
        /// Finds a path from start to goal in the voxel map.
        /// </summary>
        public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, bool canDig)
        {
            // Randomize the directions in place.
            _directions = (Vector3Int[])BaseDirections.Clone();
            _diagonalDirections = (Vector3Int[])BaseDiagonalDirections.Clone();
            _directions.Shuffle();
            _diagonalDirections.Shuffle();

            // Priority queue for the open set.
            var openSet = new PriorityQueue<Vector3Int, int>();
            openSet.Enqueue(start, Heuristic(start, goal, false));

            // Use dictionaries for tracking the best path.
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var gScore = new Dictionary<Vector3Int, int> { [start] = 0 };

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();
                if (current == goal)
                {
                    // Reconstruct path without extra LINQ allocations.
                    var path = ReconstructPath(cameFrom, current);
                    var pathBuffer = new List<Vector3Int>(path.Count);
                    for (var i = 0; i < path.Count; i++)
                        pathBuffer.Add(path[i]);
                    // Debug.LogWarning($"Found Path of {pathBuffer.Count} blocks!");
                    return pathBuffer;
                }

                // Use a fixed-size array to hold neighbors (maximum of 10 neighbors expected).
                var neighbors = new DiggablePos[16];
                var neighborCount = GetNeighbors(current, neighbors, canDig);
                for (var i = 0; i < neighborCount; i++)
                {
                    var neighbor = neighbors[i];
                    var tentativeG = gScore[current] + 1;
                    if (!gScore.TryGetValue(neighbor.Pos, out var neighborG) || tentativeG < neighborG)
                    {
                        cameFrom[neighbor.Pos] = current;
                        gScore[neighbor.Pos] = tentativeG;
                        var priority = tentativeG + Heuristic(neighbor.Pos, goal, neighbor.RequiresDig);
                        openSet.Enqueue(neighbor.Pos, priority);
                    }
                }
            }
            // Debug.LogWarning("No Path Found!");

            return null; // No path found.
        }

        // Reconstructs the path from the cameFrom dictionary.
        private static List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
        {
            var path = new List<Vector3Int>();
            while (cameFrom.TryGetValue(current, out var previous))
            {
                path.Add(current);
                current = previous;
            }

            path.Add(current);
            path.Reverse();
            return path;
        }

        // Optimized heuristic using Math.Sqrt.
        private int Heuristic(Vector3Int a, Vector3Int b, bool requiresDig)
        {
            var digPenalty = requiresDig ? 10 : 0;
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var dz = a.z - b.z;
            return digPenalty + _heuristics switch
            {
                Heuristics.Manhattan => Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz),
                _ => (int)Math.Sqrt(dx * dx + dy * dy + dz * dz),
            };
        }

        /// <summary>
        /// Fills the provided array with valid neighbors of point p.
        /// Returns the number of neighbors found.
        /// </summary>
        private int GetNeighbors(Vector3Int p, DiggablePos[] neighbors, bool canDig)
        {
            var count = 0;
            var ground = new Vector3Int(p.x, p.y - 1, p.z);

            // Check if I'm grounded, otherwise keep falling
            if (InBounds(ground) && IsSolid(ground) == 0)
                neighbors[count++] = new DiggablePos(ground);
            else
            {
                // XZ cardinal moves and jump.
                for (var i = 0; i < _directions.Length; i++)
                {
                    var newP = p + _directions[i];

                    // XZ cardinal moves
                    var isValidNewPos = IsValidPos(newP);
                    var requiresDig = isValidNewPos.Bool(1) || isValidNewPos.Bool(2);
                    if (isValidNewPos.Bool(0))
                        if (canDig || !requiresDig)
                            neighbors[count++] = new DiggablePos(newP, requiresDig);

                    // Allow a jump move if there's a step to jump on.
                    var jump = newP + Vector3Int.up;
                    var isValidJump = IsValidPos(jump);
                    requiresDig = isValidJump.Bool(1) || isValidJump.Bool(2);
                    if (isValidJump.Bool(0) && IsSolid(ground) != 0)
                        if (canDig || !requiresDig)
                            neighbors[count++] = new DiggablePos(jump, requiresDig);
                }

                // XZ diagonal moves.
                for (var i = 0; i < _diagonalDirections.Length; i++)
                {
                    var d = _diagonalDirections[i];
                    var newP = p + d;
                    var newP2 = p + d * Vector3Int.right;
                    byte isValidNewP = IsValidPos(newP), isValidNewP2 = IsValidPos(newP2);
                    var requiresDig = isValidNewP.Bool(1) || isValidNewP.Bool(2) ||
                                      isValidNewP2.Bool(1) || isValidNewP2.Bool(2);
                    if (isValidNewP.Bool(0) && isValidNewP2.Bool(0))
                        if (canDig || !requiresDig)
                            neighbors[count++] = new DiggablePos(newP, requiresDig);
                }

                // Fall down by 1 block.
                var fall = new Vector3Int(p.x, p.y + 1, p.z);
                var isValidFall = IsValidPos(fall);
                if (isValidFall.Bool(0) && !isValidFall.Bool(1) && !isValidFall.Bool(2))
                    neighbors[count++] = new DiggablePos(fall);
            }

            return count;
        }

        /// <summary>
        /// Checks if the player can occupy the given position.
        /// </summary>
        /// <returns>A mask of bits such that,
        /// 1st - is valid pos,
        /// 2nd - need to dig pos,
        /// 3rd - need to dig above</returns>
        public byte IsValidPos(Vector3Int pos)
        {
            var above = pos + Vector3Int.up;
            
            // If outside the map
            if (!InBounds(pos) || !InBounds(above)) return 0;
            byte isSolidPos = IsSolid(pos), isSolidAbove = IsSolid(above);
            
            // If solid not diggable
            if (isSolidPos == 1 || isSolidAbove == 1) return 0;
            return (byte)(1 | ((isSolidPos == 2 ? 1 : 0) << 1) | ((isSolidAbove == 2 ? 1 : 0) << 2));
        }

        private bool InBounds(Vector3Int pos) =>
            pos.y >= 0 && pos.y < Blocks.GetLength(0) &&
            pos.x >= 0 && pos.x < Blocks.GetLength(1) &&
            pos.z >= 0 && pos.z < Blocks.GetLength(2);

        /// <summary>
        /// Check if a block is solid
        /// </summary>
        /// <returns>0 if block is not solid, 1 if it is solid and not diggable and 2 if it is solid but diggable</returns>
        private byte IsSolid(Vector3Int pos)
        {
            var block = VoxelData.BlockTypes[Blocks[pos.y, pos.x, pos.z]];
            if (!block.isSolid)
                return 0;
            if (block.blockHealth != BlockHealth.Indestructible && block.blockHealth != BlockHealth.NonDiggable)
                return 2;
            return 1;
        }
    }
}