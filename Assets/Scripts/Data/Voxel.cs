using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct Voxel
{
    public int ID;

    public bool isSolid => ID != 0;
}