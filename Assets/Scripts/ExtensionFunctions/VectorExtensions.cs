using UnityEngine;

namespace ExtensionFunctions
{
    public static class VectorExtensions
    {
        public static readonly System.Random Random = new();

        public static Quaternion ToQuaternion(this Vector2 vector) =>
            Quaternion.Euler(0f, 0f, Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg);

        public static Quaternion ToQuaternion(this Vector3 vector) =>
            ((Vector2)vector).ToQuaternion();

        public static Vector2 GetRandomPointInBounds(this Bounds bounds) =>
            new(
                (float)Random.NextDouble() * (bounds.max.x - bounds.min.x) + bounds.min.x,
                (float)Random.NextDouble() * (bounds.max.y - bounds.min.y) + bounds.min.y
            );

        public static Vector3 RandomVector3(float min, float max)
        {
            return new Vector3((float)Random.NextDouble() * (max - min) + min,
                (float)Random.NextDouble() * (max - min) + min,
                (float)Random.NextDouble() * (max - min) + min);
        }

        public static Vector2 RotateByAngle(this Vector2 v, float angleInDegrees)
        {
            var angleInRadians = angleInDegrees * Mathf.Deg2Rad;
            var cosTheta = Mathf.Cos(angleInRadians);
            var sinTheta = Mathf.Sin(angleInRadians);
            var newX = cosTheta * v.x - sinTheta * v.y;
            var newY = sinTheta * v.x + cosTheta * v.y;
            return new Vector2(newX, newY);
        }

        public static float RandomRange(this Vector2 v) => (float)Random.NextDouble() * (v.y - v.x) + v.x;
        public static float RandomRange(float a, float b) => (float)Random.NextDouble() * (b - a) + a;
    }
}