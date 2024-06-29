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
        public readonly byte[,,] blocks; // y,x,z

        public Map(string name, byte[,,] blocks)
        {
            this.name = name;
            this.blocks = blocks;
        }

        public static Map GetMap(string mapName) => Array.Find(Maps,
            map => string.Equals(map.name, mapName, StringComparison.CurrentCultureIgnoreCase));

        public Vector2Int Size => new(blocks.GetLength(1), blocks.GetLength(2));
    }
}