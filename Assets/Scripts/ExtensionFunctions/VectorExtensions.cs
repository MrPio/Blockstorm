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

        public static Vector3 RandomVector3(float min, float max) =>
            new(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
        
        public static Vector2 RotateByAngle(this Vector2 v, float angleInDegrees)
        {
            var angleInRadians = angleInDegrees * Mathf.Deg2Rad;
            var cosTheta = Mathf.Cos(angleInRadians);
            var sinTheta = Mathf.Sin(angleInRadians);
            var newX = cosTheta * v.x - sinTheta * v.y;
            var newY = sinTheta * v.x + cosTheta * v.y;
            return new Vector2(newX, newY);
        }
    }
}