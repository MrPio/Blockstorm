using System;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using Managers.Serializer;
using Model;
using UnityEngine;
using VoxelEngine;

namespace Utils
{
    public enum CopyType
    {
        Rotate180,
        MirrorX,
        MirrorZ
    }

    public enum CopyDirection
    {
        X,
        Z
    }

    /**
 * A rule used to automate the duplication of parts of the map.
 * This is useful for symmetric maps. Instead of building it in its entirety,
 * you could build only a quarter of it and then duplicate and rotate it 3 times.
 */
    [Serializable]
    public class CopyRule
    {
        [Serializable]
        public struct RemappingRule
        {
            public string oldName, newName;
        }

        public Vector3Int from, to;
        public CopyType copyType;
        public RemappingRule[] remappingRules;
        public CopyDirection direction;

        public CopyRule(Vector3Int from, Vector3Int to, CopyType copyType, RemappingRule[] remappingRules,
            CopyDirection direction)
        {
            this.from = from;
            this.to = to;
            this.copyType = copyType;
            this.remappingRules = remappingRules;
            this.direction = direction;
        }
    }

    public class MapConverter : MonoBehaviour
    {
        public string mapName = "";
        [Header("Map2Voxel")] public CopyRule[] copyRules;
        public CopyRule.RemappingRule[] remappingRules;
        [Header("Voxel2Map")] public GameObject cube;
        private WorldManager _wm;

        private void Awake()
        {
            _wm = GameObject.FindWithTag("WorldManager").GetComponent<WorldManager>();
        }

        /*
         * Convert a map of cubes into a Map class instance and serialise it into a compact JSON file.
         * Instead of dumping the whole 3-dimensional array, which is not even possible, the map is converted
         * to a smaller list representation that only stores non-air blocks.
         */
        [Button]
        private void Map2Voxel()
        {
            var blockTypesList = VoxelData.BlockTypes.ToList();
            var cubes = GameObject.FindWithTag("MapGenerator").GetComponentsInChildren<MeshRenderer>();
            var blockTypes = new List<byte>();
            var positionsX = new List<int>();
            var positionsY = new List<int>();
            var positionsZ = new List<int>();
            foreach (var cube in cubes)
            {
                var id = int.Parse(cube.material.mainTexture.name.Split('_')[1]) - 1;
                var blockType = blockTypesList.FindIndex(e => e.topID == id || e.bottomID == id || e.sideID == id);
                var posNorm = Vector3Int.FloorToInt(cube.transform.position + Vector3.one * 0.25f);
                blockTypes.Add((byte)blockType);
                positionsX.Add(posNorm.x);
                positionsY.Add(posNorm.y);
                positionsZ.Add(posNorm.z);
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
            var mapSize = new Vector3Int(maxX - minX + 1, Map.MaxHeight, maxZ - minZ + 1);
            // Apply remapping rules
            foreach (var block in blocksList)
            foreach (var remap in remappingRules)
                if (block.type == _wm.BlockTypeIndex(remap.oldName))
                    block.type = _wm.BlockTypeIndex(remap.newName);
            // Add bottom bedrock layer
            for (var x = 0; x < mapSize.x; x++)
            for (var z = 0; z < mapSize.z; z++)
                blocksList.Add(new BlockEncoding((short)x, 0, (short)z, 1));
            // Apply copy rules
            foreach (var rule in copyRules)
            {
                if (rule.direction == CopyDirection.X)
                    throw new ArgumentOutOfRangeException();
                mapSize.z += rule.to.z - rule.from.z - 1;
                var blocksToCopy = blocksList.Where(it =>
                    it.x >= rule.from.x && it.x < rule.to.x && it.y >= rule.from.y && it.y < rule.to.y &&
                    it.z >= rule.from.z && it.z < rule.to.z);
                var newBlocks = rule.copyType switch
                {
                    CopyType.Rotate180 =>
                        blocksToCopy.Select(it =>
                            new BlockEncoding((short)(maxX - minX - it.x), it.y, (short)
                                (maxZ - minZ - 1 + rule.to.z - rule.from.z - it.z), it.type)).ToList(),
                    CopyType.MirrorX =>
                        blocksToCopy.Select(it =>
                            new BlockEncoding((short)(maxX - minX - it.x), it.y, (short)
                                (it.z + maxZ - minZ - 1), it.type)).ToList(),
                    CopyType.MirrorZ =>
                        blocksToCopy.Select(it =>
                            new BlockEncoding((short)(it.x), it.y, (short)
                                (it.z + maxZ - minZ - 1), it.type)).ToList(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                foreach (var block in newBlocks)
                foreach (var remap in rule.remappingRules)
                    if (block.type == _wm.BlockTypeIndex(remap.oldName))
                        block.type = _wm.BlockTypeIndex(remap.newName);
                blocksList.AddRange(newBlocks);
            }

            var map = new Map(mapName, blocksList, mapSize);
            map.Save();
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
            var blocks = _wm.Map.Blocks;
            var size = _wm.Map.size;
            for (var y = 1; y < size.y; y++) // Ignoring the indestructible base
            for (var x = 0; x < size.x; x++)
            for (var z = 0; z < size.z; z++)
            {
                if (blocks[y, x, z] == 0)
                    continue;
                var cubeGo = Instantiate(cube, map);
                cubeGo.transform.position = new Vector3(x, y, z) + Vector3.one * 0.5f;
                var textureId = VoxelData.BlockTypes[blocks[y, x, z]].topID;
                cubeGo.GetComponent<MeshRenderer>().material =
                    Resources.Load($"Textures/texturepacks/blockade/Materials/blockade_{(textureId + 1):D1}") as
                        Material;
            }
        }

        // This is used to select an ISerializer concrete in the editor.
        private enum Serializers
        {
            JsonSerializer,
            BinarySerializer
        }

        [Header("SerializeMap")] [SerializeField]
        private Spawn redSpawn;

        /*
         * Serialize the map currently loaded in the world manager using the provided serialization method.
         * This is used to convert JSON encoded maps to binary encoding, which uses much less disk space.
         * This is also used to add spawn points to a map.
         */
        [Button]
        private void SerializeMap(Serializers serializer)
        {
            var map = _wm.Map;
            map.spawns = new List<Spawn>
            {
                redSpawn,
                new(Team.Blue,
                    redSpawn.spawnLayers.Select(it =>
                            new SpawnArea(new Vector2XZ(map.size.x - it.topRight.x, it.bottomLeft.z),
                                new Vector2XZ(map.size.x - it.bottomLeft.x, it.topRight.z), it.y))
                        .ToList()),
                new(Team.Green,
                    redSpawn.spawnLayers.Select(it =>
                            new SpawnArea(new Vector2XZ(it.bottomLeft.x, map.size.z - it.topRight.z),
                                new Vector2XZ(it.topRight.x, map.size.z - it.bottomLeft.z), it.y))
                        .ToList()),
                new(Team.Yellow,
                    redSpawn.spawnLayers.Select(it =>
                            new SpawnArea(new Vector2XZ(map.size.x - it.topRight.x, map.size.z - it.topRight.z),
                                new Vector2XZ(map.size.x - it.bottomLeft.x, map.size.z - it.bottomLeft.z), it.y))
                        .ToList())
            };

            if (serializer is Serializers.BinarySerializer)
                map.Save(BinarySerializer.Instance);
            else if (serializer is Serializers.JsonSerializer)
                map.Save(JsonSerializer.Instance);
        }

        [Serializable]
        private class SpawnCamera
        {
            public Camera camera;
            public Nullable<Team> team;
        }

        [Header("AddSpawnCameras")] [SerializeField]
        private List<SpawnCamera> spawnCameras;

        /*
         * Set a list of spawn camera for the currently loaded map.
         * Spawn camera is used to show a part of the map in the connection menu instead of a black screen.
         */
        [Button]
        private void AddSpawnCameras()
        {
            var map = _wm.Map;
            map.cameraSpawns ??= new List<CameraSpawn>();
            map.cameraSpawns.AddRange(spawnCameras.Select(it =>
                    new CameraSpawn(it.camera.transform.position, it.camera.transform.rotation.eulerAngles, it.team))
                .ToList());
            map.Save();
        }

        // Change the type of some map blocks
        [Header("ChangeBlocks")] [SerializeField]
        private string[] oldIds;

        [SerializeField] private string newId;
        [SerializeField] private Vector3Int[] mins;
        [SerializeField] private Vector3Int[] maxs;

        [Button]
        private void ChangeBlocks()
        {
            var map = _wm.Map;
            var count = 0;
            for (var i = 0; i < mins.Length; i++)
            {
                var min = mins[i];
                var max = maxs[i];
                for (var x = min.x; x < max.x; x++)
                for (var y = min.y; y < max.y; y++)
                for (var z = min.z; z < max.z; z++)
                    if (oldIds.Any(it => it == map.GetBlock(new Vector3Int(x, y, z)).name))
                    {
                        count++;
                        map.Blocks[y, x, z] = VoxelData.Name2Id(newId);
                    }
            }

            Debug.Log($"Updated {count} blocks!");
            map.Save();
        }
    }
}