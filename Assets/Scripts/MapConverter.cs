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
        var cubes = GameObject.Find("Map").GetComponentsInChildren<MeshRenderer>();
        var blockTypes = new List<byte>();
        var positionsX = new List<int>();
        var positionsY = new List<int>();
        var positionsZ = new List<int>();
        foreach (var cube in cubes)
        {
            var id = int.Parse(cube.material.mainTexture.name.Split('_')[1]) - 1;
            var blockType = blockTypesList.FindIndex(e => e.topID == id || e.bottomID == id || e.sideID == id);
            var pos = Vector3Int.FloorToInt(cube.transform.position);
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
                (short)(positionsY[i] - minY),
                (short)(positionsZ[i] - minZ),
                blockType)).ToList();
        var map = new Map(mapName, blocksList, new Vector3Int(maxX - minX + 1, Map.MaxHeight, maxZ - minZ + 1));
        IOManager.Serialize(map, "maps", map.name);
        print($"Map [{map.name}] saved successfully!");
    }
}