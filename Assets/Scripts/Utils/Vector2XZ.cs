﻿using System;
using UnityEngine;

namespace Utils
{
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

        public Vector3 ToVector3(float y = 0) => new(x, y, z);
    }
}