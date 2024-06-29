using System;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using static System.String;

namespace VoxelEngine
{
    [Serializable]
    public class Map
    {
        public const short MaxHeight = 128;

        private static readonly string MapsDir = "maps/";

        public string name;

        // We assume to have no more than 256 types of blocks. Otherwise an int would be required (4x size)
        [SerializeField] private List<BlockEncoding> blocksList;
        public byte[,,] blocks; // y,x,z
        public Vector3Int size;

        public Map(string name, List<BlockEncoding> blocksList, Vector3Int size)
        {
            this.name = name;
            this.blocksList = blocksList;
            this.size = size;
        }

        private Map DeserializeMap()
        {
            // From blocksList list to blocks array
            blocks = new byte[size.y, size.x, size.y];
            foreach (var block in blocksList)
                blocks[block.y, block.x, block.z] = block.type;
            return this;
        }

        public static Map GetMap(string mapName) => IOManager.Deserialize<Map>(MapsDir + mapName).DeserializeMap();
    }

    [Serializable]
    public class BlockEncoding
    {
        public short x, y, z;
        public byte type;

        public BlockEncoding(short x, short y, short z, byte type)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.type = type;
        }
    }
}