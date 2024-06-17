using UnityEngine;

public struct Voxel
{
    public byte id;
    
    public bool IsSolid => id != 0;
}
