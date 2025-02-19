using System;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Useful when thinking the map as a list of grids, where each grid represents a XZ plane at a certain height Y.
    /// </summary>
    [Serializable]
    public class Vector2XZ
    {
        [SerializeField] public float x;
        [SerializeField] public float z;

        public Vector2XZ(float x, float z)
        {
            this.x = x;
            this.z = z;
        }

        public Vector2XZ(Vector3 v)
        {
            x = v.x;
            z = v.z;
        }

        public Vector3 ToVector3(float y = 0) => new(x, y, z);

        public static implicit operator Vector2(Vector2XZ rValue) => new(rValue.x, rValue.z);

        public static implicit operator Vector2XZ(Vector2 rValue) => new(rValue.x, rValue.y);
    }
}