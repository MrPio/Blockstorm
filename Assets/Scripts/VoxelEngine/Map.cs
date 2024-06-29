using System;
using UnityEngine;
using static System.String;

namespace VoxelEngine
{
    public class Map
    {
        public const int MaxHeight = 4;

        private static readonly Map[] Maps =
        {
            new("Debug", new byte[MaxHeight, 4, 4]
            {
                {
                    { 2, 2, 2, 2, },
                    { 2, 2, 2, 2, },
                    { 2, 2, 2, 2, },
                    { 2, 2, 2, 2, },
                },
                {
                    { 2, 2, 2, 2, },
                    { 2, 2, 2, 2, },
                    { 2, 2, 2, 2, },
                    { 2, 2, 2, 2, },
                },
                {
                    { 1, 1, 1, 1, },
                    { 1, 1, 1, 1, },
                    { 1, 1, 1, 1, },
                    { 1, 1, 1, 1, },
                },
                {
                    { 0, 0, 0, 0, },
                    { 0, 0, 0, 0, },
                    { 0, 0, 0, 0, },
                    { 0, 0, 0, 0, },
                },
            })
        };

        public readonly string name;

        // We assume to have no more than 256 types of blocks. Otherwise an int would be required (4x size)
        public readonly byte[,,] blocks; // y,x,z
        public Vector3Int size;

        public Map(string name, byte[,,] blocks)
        {
            this.name = name;
            this.blocks = blocks;
            this.size = new Vector3Int(blocks.GetLength(1),MaxHeight, blocks.GetLength(2));
        }

        public static Map GetMap(string mapName) => Array.Find(Maps,
            map => string.Equals(map.name, mapName, StringComparison.CurrentCultureIgnoreCase));
    }
}