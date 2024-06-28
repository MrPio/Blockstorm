using UnityEngine;

namespace VoxelEngine
{
    public class WorldManager : MonoBehaviour
    {
        public Material material;
        public BlockType[] blockTypes;
        public int atlasCount = 16;
        public float AtlasBlockSize => 1f / atlasCount;
        
        private static WorldManager _instance;

        public static WorldManager Instance => _instance ??= FindObjectOfType<WorldManager>();
    }

    [System.Serializable]
    public class BlockType
    {
        public string name;
        public bool isSolid;
    }
}