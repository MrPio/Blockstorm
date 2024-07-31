using System.Collections.Generic;
using UnityEngine;

namespace ExtensionFunctions
{
    public static class VectorExtensions
    {
        public static Quaternion ToQuaternion(this Vector2 vector) =>
            Quaternion.Euler(0f, 0f, Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg);

        public static Quaternion ToQuaternion(this Vector3 vector) =>
            ((Vector2)vector).ToQuaternion();

        public static Vector2 GetRandomPointInBounds(this Bounds bounds) =>
            new(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );

        public static List<Vector3Int> GetNeighborVoxels(this Vector3 position, float range)
        {
            var voxels = new List<Vector3Int>();
            for (var x = position.x - range; x < position.x + range; x++)
            for (var y = position.y - range; y < position.x + range; y++)
            for (var z = position.z - range; z < position.x + range; z++)
            {
                var pos = new Vector3(x, y, z);
                if (Vector3.Distance(position + Vector3.one * 0.5f, pos) <= range)
                {
                    var posNorm = Vector3Int.FloorToInt(pos);
                    voxels.Add(posNorm);
                }
            }

            return voxels;
        }

        public static Vector3 RandomVector3(float min, float max) =>
            new(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
    }
}