using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using Managers;
using UnityEngine;
using VoxelEngine;

public class MapConverter : MonoBehaviour
{
    public string mapName = "";
    public GameObject cube;

    /*
     * Convert a map of cubes into a Map class instance and serialise it into a compact JSON file.
     * Instead of dumping the whole 3-dimensional array, which is not even possible, the map is converted
     * to a smaller list representation that only stores non-air blocks. 
     */
    [Button]
    private void Map2Voxel()
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

    /*
     * Convert a voxel into a cube map. This became necessary when I lost a good prefab of a map in
     * progress due to a mistake of mine. So I wrote the following code to retrieve it from the JSON
     * voxel serialised file that I had luckily saved.
     * This can be used to avoid storing large prefabs (~10x memory usage compared to the JSON serialised voxel version).
     */
    [Button]
    private void Voxel2Map()
    {
        var map = GameObject.FindWithTag("MapGenerator").transform;
        var blocks = WorldManager.instance.map.blocks;
        var size = WorldManager.instance.map.size;
        for (var y = 1; y < size.y; y++) // Ignoring the indestructible base
        for (var x = 0; x < size.x; x++)
        for (var z = 0; z < size.z; z++)
        {
            if (blocks[y, x, z] == 0)
                continue;
            var cubeGo = Instantiate(cube, map);
            cubeGo.transform.position = new Vector3(x, y, z) + Vector3.one * 0.5f;
            var textureId = WorldManager.instance.blockTypes[blocks[y, x, z]].topID;
            cubeGo.GetComponent<MeshRenderer>().material =
                Resources.Load($"Textures/texturepacks/blockade/Materials/blockade_{(textureId + 1):D1}") as Material;
        }
    }
}