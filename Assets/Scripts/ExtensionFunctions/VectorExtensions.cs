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
    }
}