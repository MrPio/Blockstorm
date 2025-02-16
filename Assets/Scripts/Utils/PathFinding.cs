using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
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

        // 4 cardinal directions for horizontal moves.
        private static readonly Point3D[] Directions =
        {
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 0, 1),
            new(0, 0, -1)
        };

        private readonly bool[,,] _isBlock;

        public AStarPathfinder(bool[,,] isBlock)
        {
            this._isBlock = isBlock;
        }

        /// <summary>
        /// Finds a path from start to goal in the given voxel map.
        /// The map is a 3D int array where 0 = air, 1 = block.
        /// The player occupies two vertical cells.
        /// </summary>
        public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
        {
            Point3D startP = start;
            Point3D goalP = goal;
            var openSet = new PriorityQueue<Point3D, int>();
            openSet.Enqueue(startP, Heuristic(startP, goalP));
            var cameFrom = new Dictionary<Point3D, Point3D>();
            var gScore = new Dictionary<Point3D, int> { [startP] = 0 };

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();
                if (current == goalP)
                    return ReconstructPath(cameFrom, current).Select(point3D => (Vector3Int)point3D).ToList();

                foreach (var neighbor in GetNeighbors(current))
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

        // Manhattan distance heuristic.
        private static int Heuristic(Point3D a, Point3D b) =>
            Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);

        private IEnumerable<Point3D> GetNeighbors(Point3D p)
        {
            // Horizontal moves (same level).
            foreach (var d in Directions)
            {
                var np = new Point3D((short)(p.X + d.X), (short)(p.Y + d.Y), (short)(p.Z + d.Z));
                if (IsValid(np))
                    yield return np;
            }

            // Jump up by 1 block.
            var jump = new Point3D(p.X, (short)(p.Y + 1), p.Z);
            if (IsValid(jump))
                yield return jump;
            // Fall down by 1 block.
            var fall = new Point3D(p.X, (short)(p.Y + 1), p.Z);
            if (IsValid(fall)) yield return fall;
        }

        // Checks if the player can occupy the given position.
        // The player occupies cell 'pos' and the one above it.
        private bool IsValid(Point3D pos)
        {
            var above = new Point3D(pos.X, (short)(pos.Y + 1), pos.Z);
            return InBounds(pos) && InBounds(above) &&
                   !_isBlock[pos.Y, pos.X, pos.Z] && !_isBlock[above.Y, above.X, above.Z];
        }

        private bool InBounds(Point3D pos) =>
            pos.Y >= 0 && pos.Y < _isBlock.GetLength(0) &&
            pos.X >= 0 && pos.X < _isBlock.GetLength(1) &&
            pos.Z >= 0 && pos.Z < _isBlock.GetLength(2);
    }
}