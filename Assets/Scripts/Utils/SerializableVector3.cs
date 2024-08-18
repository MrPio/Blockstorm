using System;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Since unity doesn't flag the Vector3 as serializable, we
    /// need to create our own version. This one will automatically convert
    /// between Vector3 and NetVector3
    /// </summary>
    [Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3(float rX, float rY, float rZ)
        {
            x = rX;
            y = rY;
            z = rZ;
        }

        public override string ToString() => $"[{x}, {y}, {z}]";

        public static implicit operator Vector3(SerializableVector3 rValue) => new(rValue.x, rValue.y, rValue.z);

        public static implicit operator SerializableVector3(Vector3 rValue) => new(rValue.x, rValue.y, rValue.z);
    }
}