using System;
using UnityEngine;

namespace VoxelEngine
{
    // This enum is redundant. It enable accessing the blockTypes array with a name instead of an index
    public enum BlockTypes
    {
        Grass,
        Dirt,
        Steel
    }

    public class WorldManager : MonoBehaviour
    {
        public int chunkSize = 5;
        public int chunkHeight = 15;
        public int worldChunks = 50;
        public int viewChunksDistance = 8;
        public int textureAtlasSizeInBlocks = 4;
        public int WorldBlocks => worldChunks * chunkSize;
        public float NormalizedBlockTextureSize => 1f / textureAtlasSizeInBlocks;

        public Material material;

        [NonSerialized] public readonly BlockType[] blockTypes =
        {
            new("grass", topID: 1, bottomID: 0, sideID: 2),
            new("dirt", topID: 0),
            new("steel", topID: 16),
        };

        public int atlasCount = 16;
        public float AtlasBlockSize => 1f / atlasCount;
        private static WorldManager _instance;
        public static WorldManager Instance => _instance ??= FindObjectOfType<WorldManager>();

        private void Start()
        {
            var chunk = new Chunk(new ChunkCoord(0, 0), this);
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