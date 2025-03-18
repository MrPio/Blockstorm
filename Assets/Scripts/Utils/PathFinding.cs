using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using NUnit.Framework;
using UnityEngine;
using VoxelEngine;

namespace Utils
{
    /// <summary>
    /// Implementation of A* algorithm using a PriorityQueue,
    /// where the priority of each node is given by the Euclidean distance heuristics.
    /// </summary>
    public class AStarPathfinder
    {
        private readonly struct Point3D : IEquatable<Point3D>
        {
            public readonly short X, Y, Z;

            public Point3D(short x, short y, short z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public bool Equals(Point3D other) => X == other.X && Y == other.Y && Z == other.Z;
            public override bool Equals(object obj) => obj is Point3D other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(X, Y, Z);
            public static bool operator ==(Point3D a, Point3D b) => a.Equals(b);
            public static bool operator !=(Point3D a, Point3D b) => !a.Equals(b);
            public static implicit operator Vector3Int(Point3D rValue) => new(rValue.X, rValue.Y, rValue.Z);

            public static implicit operator Point3D(Vector3Int rValue) =>
                new((short)rValue.x, (short)rValue.y, (short)rValue.z);
        }

        // 4 cardinal and 4 diagonal XZ moves.
        private static readonly Point3D[] BaseDirections =
            {
                new(1, 0, 0),
                new(-1, 0, 0),
                new(0, 0, 1),
                new(0, 0, -1)
            },
            BaseDiagonalDirections =
            {
                new(1, 0, 1),
                new(1, 0, -1),
                new(-1, 0, 1),
                new(-1, 0, -1)
            };

        private static Point3D[] _directions, _diagonalDirections;

        private byte[,,] _blocks;


        public AStarPathfinder(Map map)
        {
            _blocks = (byte[,,])map.Blocks.Clone();
        }

        /// <summary>
        /// Finds a path from start to goal in the given voxel map.
        /// The map is a 3D int array where 0 = air, 1 = block.
        /// The player occupies two vertical cells.
        /// </summary>
        public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, bool canDig)
        {
            // TODO if canDig, dig NESW blocks as last resource
            // Randomize the direction choice
            _directions = BaseDirections.ToList().Shuffle().ToArray();
            _diagonalDirections = BaseDiagonalDirections.ToList().Shuffle().ToArray();

            Point3D startP = start;
            Point3D goalP = goal;
            var openSet = new PriorityQueue<Point3D, int>();
            openSet.Enqueue(startP, Heuristic(startP, goalP));
            var cameFrom = new Dictionary<Point3D, Point3D>();
            var gScore = new Dictionary<Point3D, int> { [startP] = 0 };

            var steps = 0;
            while (openSet.Count > 0)
            {
                ++steps;
                var current = openSet.Dequeue();
                if (current == goalP)
                {
                    var path = ReconstructPath(cameFrom, current);
                    List<Vector3Int> pathBuffer = new();
                    foreach (var point in path)
                        pathBuffer.Add(point);
                    return new List<Vector3Int>(pathBuffer); // Allocate only once
                    // ERA QUESTO IL FOTTUTO COLPEVOLE DEL LAG SUL MAIN THREAD!! LINQ CAUSAVA PESANTE LAVORO DI GARBAGE COLLECTOR!
                    // return ReconstructPath(cameFrom, current).Select(point3D => (Vector3Int)point3D).ToList();
                }

                foreach (var neighbor in GetMoves(current))
                {
                    var tentativeG = gScore[current] + 1;
                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        openSet.Enqueue(neighbor, tentativeG + Heuristic(neighbor, goalP));
                    }
                }
            }

            return null; // No path found.
        }

        private static List<Point3D> ReconstructPath(Dictionary<Point3D, Point3D> cameFrom, Point3D current)
        {
            var path = new List<Point3D> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        // Distance heuristic.
        private static int Heuristic(Point3D a, Point3D b) =>
            // Manhattan distance
            // Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
            // Euclidean distance
            (int)Math.Pow(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2), 0.5);

        private IEnumerable<Point3D> GetMoves(Point3D p)
        {
            var ground = new Point3D(p.X, (short)(p.Y - 1), p.Z);

            // Check if I'm grounded, otherwise keep falling
            if (InBounds(ground) && !IsSolidAndNotPlayerBlock(ground))
                yield return ground;
            else
            {
                // XZ cardinal moves
                foreach (var d in _directions)
                {
                    var np = new Point3D((short)(p.X + d.X), (short)(p.Y + d.Y), (short)(p.Z + d.Z));
                    if (IsValidPos(np))
                        yield return np;

                    // Jump up by 1 block, but only if there's a step to jump on.
                    var jump = new Point3D(np.X, (short)(np.Y + 1), np.Z);
                    if (IsValidPos(jump) && IsSolidAndNotPlayerBlock(new Point3D(jump.X, (short)(jump.Y - 1), jump.Z)))
                        yield return jump;
                }

                // XZ diagonal moves
                foreach (var d in _diagonalDirections)
                {
                    // Is not valid:
                    //  █ X     - X     █ X 
                    //  • █     • █     • - 
                    // Is valid:
                    //  - X
                    //  • -
                    var np = new Point3D((short)(p.X + d.X), (short)(p.Y + d.Y), (short)(p.Z + d.Z));
                    var np1 = new Point3D((short)(p.X + d.X), (short)(p.Y + d.Y), p.Z);
                    var np2 = new Point3D(p.X, (short)(p.Y + d.Y), (short)(p.Z + d.Z));
                    if (IsValidPos(np) && IsValidPos(np1) && IsValidPos(np2))
                        yield return np;    
                }

                // Fall down by 1 block.
                var fall = new Point3D(p.X, (short)(p.Y + 1), p.Z);
                if (IsValidPos(fall)) yield return fall;
            }
        }

        // Checks if the player can occupy the given position.
        public bool IsValidPos(Vector3Int pos)
        {
            var above = pos + Vector3Int.up;
            return InBounds(pos) && InBounds(above) &&
                   !IsSolidAndNotPlayerBlock(pos) && !IsSolidAndNotPlayerBlock(above);
        }

        private bool InBounds(Point3D pos) =>
            pos.Y >= 0 && pos.Y < _blocks.GetLength(0) &&
            pos.X >= 0 && pos.X < _blocks.GetLength(1) &&
            pos.Z >= 0 && pos.Z < _blocks.GetLength(2);

        private bool IsSolidAndNotPlayerBlock(Point3D pos)
        {
            var block = VoxelData.BlockTypes[_blocks[pos.Y, pos.X, pos.Z]];
            return block.isSolid && !block.name.Contains("player_block");
        }
    }
}