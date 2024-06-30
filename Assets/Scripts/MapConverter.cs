using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using Managers;
using UnityEngine;
using VoxelEngine;

public class MapConverter : MonoBehaviour
{
    public string mapName = "";

    [Button]
    private void SaveMap()
    {
        var blockTypesList = WorldManager.instance.blockTypes.ToList();
        var cubes = GameObject.FindWithTag("MapGenerator").GetComponentsInChildren<MeshRenderer>();
        var blockTypes = new List<byte>();
        var positionsX = new List<int>();
        var positionsY = new List<int>();
        var positionsZ = new List<int>();
        foreach (var cube in cubes)
        {
            var id = int.Parse(cube.material.mainTexture.name.Split('_')[1]) - 1;
            var blockType = blockTypesList.FindIndex(e => e.topID == id || e.bottomID == id || e.sideID == id);
            var pos = Vector3Int.FloorToInt(cube.transform.position + Vector3.one * 0.05f);
            blockTypes.Add((byte)blockType);
            positionsX.Add(pos.x);
            positionsY.Add(pos.y);
            positionsZ.Add(pos.z);
        }

        var minX = positionsX.Min();
        var minY = positionsY.Min();
        var minZ = positionsZ.Min();
        var maxX = positionsX.Max();
        var maxZ = positionsZ.Max();
        var blocksList = blockTypes.Select((blockType, i) =>
            new BlockEncoding(
                (short)(positionsX[i] - minX),
                (short)(positionsY[i] - minY + 1),
                (short)(positionsZ[i] - minZ),
                blockType)).ToList();
        for (var x = 0; x < maxX - minX; x++)
        for (var z = 0; z < maxZ - minZ; z++)
            blocksList.Add(new BlockEncoding((short)x, 0, (short)z, 1));
        var map = new Map(mapName, blocksList, new Vector3Int(maxX - minX + 1, Map.MaxHeight, maxZ - minZ + 1));
        IOManager.Serialize(map, "maps", map.name);
        print($"Map [{map.name}] saved successfully!");
    }
}