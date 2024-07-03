﻿using System;
using System.Collections.Generic;
using Managers;
using Unity.Mathematics;
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
        [NonSerialized] public Dictionary<Vector3Int, uint> blocksHealth;
        public Vector3Int size;

        public Map(string name, List<BlockEncoding> blocksList, Vector3Int size)
        {
            this.name = name;
            this.blocksList = blocksList;
            this.size = size;
            blocksHealth = new Dictionary<Vector3Int, uint>();
        }

        private Map DeserializeMap()
        {
            // From blocksList list to blocks array
            blocks = new byte[size.y, size.x, size.y];
            foreach (var block in blocksList)
                blocks[block.y, block.x, block.z] = block.type;
            blocksHealth = new Dictionary<Vector3Int, uint>();
            return this;
        }

        public static Map GetMap(string mapName) => IOManager.Deserialize<Map>(MapsDir + mapName).DeserializeMap();

        public BlockType GetBlock(Vector3Int pos) => WorldManager.instance.blockTypes[blocks[pos.y, pos.x, pos.z]];

        public uint DamageBlock(Vector3Int pos, uint damage)
        {
            var blockHealth = GetBlock(pos).blockHealth;
            if (blockHealth is BlockHealth.Indestructible or BlockHealth.NonDiggable)
                return uint.MaxValue;
            if (blockHealth is BlockHealth.OneHit)
                return 0;
            blocksHealth[pos] = (uint)math.max(0,
                (blocksHealth.ContainsKey(pos) ? blocksHealth[pos] : (int) blockHealth ) - damage);
            return blocksHealth[pos];
        }
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