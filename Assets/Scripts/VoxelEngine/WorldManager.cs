using System;
using ExtensionFunctions;
using UnityEngine;

namespace VoxelEngine
{
    // To add a new block type:
    // - Add one BlockType instance in the blockTypes[] array.
    // - Add one BlockTypes entry at the same index of the element in the blockTypes[] array
    // This enum is redundant. It allows you to access the blockTypes array with a name instead of an index.
    public enum BlockTypes
    {
        Air,
        Grass,
        Dirt,
        Steel
    }

    public class WorldManager : MonoBehaviour
    {
        public static WorldManager instance;

        [NonSerialized] public readonly BlockType[] blockTypes =
        {
            new("air", topID: 255, isSolid: false),
            new("grass", topID: 1, bottomID: 0, sideID: 2),
            new("dirt", topID: 0),
            new("steel", topID: 16),
        };

        public Material material;

        public int chunkSize = 2;

        // public int chunkHeight = 15; // Replaced by Map.MaxHeight
        public int worldChunks = 50;
        public int viewChunksDistance = 8;
        public int atlasCount = 16;
        public int textureAtlasSizeInBlocks = 4;
        public int WorldBlocks => worldChunks * chunkSize;
        public float NormalizedBlockTextureSize => 1f / textureAtlasSizeInBlocks;
        public float AtlasBlockSize => 1f / atlasCount;
        private Chunk[,] _chunks;

        private void Start()
        {
            instance = this;
            LoadMap(Map.GetMap("debug"));
        }

        public void LoadMap(Map map)
        {
            var mapSize = map.Size;
            var chunksX = Mathf.CeilToInt((float)mapSize.x / chunkSize);
            var chunksZ = Mathf.CeilToInt((float)mapSize.y / chunkSize);
            _chunks = new Chunk[chunksX, chunksZ];
            for (var x = 0; x < chunksX; x++)
            for (var z = 0; z < chunksZ; z++)
                _chunks[x, z] = new Chunk(new ChunkCoord(x, z),
                    map.blocks.GetSubArray(
                        from2: x * chunkSize, to2: (x + 1) * chunkSize,
                        from3: z * chunkSize, to3: (z + 1) * chunkSize), this);
        }
    }

    [Serializable]
    public class BlockType
    {
        public string name;
        public bool isSolid;
        public ushort topID, sideID, bottomID;

        public BlockType(string name, ushort topID, ushort? sideID = null, ushort? bottomID = null, bool isSolid = true)
        {
            this.name = name;
            this.isSolid = isSolid;
            this.topID = topID;
            this.sideID = sideID ?? topID;
            this.bottomID = bottomID ?? topID;
        }

        // Convert the face index to the corresponding texture ID
        // The face index order is given by VoxelData.FaceChecks
        public int GetTextureID(int i) =>
            i switch
            {
                2 => topID,
                3 => bottomID,
                _ => sideID
            };
    }
}