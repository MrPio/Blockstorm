using System;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Since unity doesn't flag the Vector3 as serializable, we
    /// need to create our own version. This one will automatically convert
    /// between Vector3 and SerializableVector3
    /// </summary>
    [Serializable]
    public struct SerializableVector3Int
    {
        public int x, y, z;

        public SerializableVector3Int(int rX, int rY, int rZ)
        {
            x = rX;
            y = rY;
            z = rZ;
        }

        public override string ToString() => $"[{x}, {y}, {z}]";

        public static implicit operator Vector3Int(SerializableVector3Int rValue) => new(rValue.x, rValue.y, rValue.z);

        public static implicit operator SerializableVector3Int(Vector3Int rValue) => new(rValue.x, rValue.y, rValue.z);
    }
}