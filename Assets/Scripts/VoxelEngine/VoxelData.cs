using System;
using System.Linq;
using Model;
using UnityEngine;

namespace VoxelEngine
{
    public static class VoxelData
    {
        public static readonly BlockType[] BlockTypes =
        {
            new("air", topID: (15, 15), isSolid: false, isTransparent: true),
            new("iron", topID: (1, 0), blockHealth: BlockHealth.Indestructible), // Bedrock must be in index 1

            new("dirt", topID: (0, 0), blockHealth: BlockHealth.Medium),
            new("grass", topID: (0, 1), bottomID: (0, 0), sideID: (0, 2), blockHealth: BlockHealth.Medium),
            new("grass_dry", topID: (0, 3), bottomID: (0, 0), sideID: (0, 4), blockHealth: BlockHealth.Medium),
            new("snow", topID: (0, 5), bottomID: (0, 0), sideID: (0, 6), blockHealth: BlockHealth.Medium),

            new("iron_dark", topID: (1, 1), blockHealth: BlockHealth.Indestructible),
            new("iron_white", topID: (1, 2), blockHealth: BlockHealth.Medium),
            new("iron_red", topID: (1, 3), blockHealth: BlockHealth.Medium),

            new("bush", topID: (4, 8), blockHealth: BlockHealth.Low),
            new("foliage", topID: (1, 9), blockHealth: BlockHealth.Low),
            new("foliage_orange", topID: (1, 10), blockHealth: BlockHealth.Low),
            new("foliage_white", topID: (1, 11), blockHealth: BlockHealth.Low),
            new("foliage_green", topID: (1, 12), blockHealth: BlockHealth.Low),

            new("log", topID: (2, 1), sideID: (2, 0), blockHealth: BlockHealth.Low),
            new("log_dark", topID: (2, 3), sideID: (2, 2), blockHealth: BlockHealth.Low),

            new("plank", topID: (2, 4), blockHealth: BlockHealth.Medium),
            new("plank_dark", topID: (2, 5), blockHealth: BlockHealth.Medium),
            new("plank_smooth", topID: (2, 6), blockHealth: BlockHealth.Medium),
            new("plank_smooth_dark", topID: (2, 7), blockHealth: BlockHealth.Medium),
            new("plank_alt", topID: (2, 8), blockHealth: BlockHealth.Medium),
            new("plank_alt_dark", topID: (2, 9), blockHealth: BlockHealth.Medium),
            new("plank_log", topID: (2, 11), sideID: (2, 10), blockHealth: BlockHealth.Medium),

            new("cobblestone", topID: (0, 10), blockHealth: BlockHealth.High),
            new("stone", topID: (1, 8), blockHealth: BlockHealth.High),
            new("sand", topID: (0, 7), blockHealth: BlockHealth.Low),
            new("sand_stone", topID: (0, 8), blockHealth: BlockHealth.Medium),
            new("ardesia", topID: (4, 7), blockHealth: BlockHealth.Medium),
            new("clay", topID: (0, 9), blockHealth: BlockHealth.Medium),
            new("red_stone", topID: (0, 11), blockHealth: BlockHealth.Medium),
            new("red_stone_dark", topID: (0, 12), blockHealth: BlockHealth.Medium),
            new("grey_stone", topID: (0, 13), blockHealth: BlockHealth.Medium),
            new("slab", topID: (4, 4), blockHealth: BlockHealth.High),
            new("stone_refined  ", topID: (4, 5), sideID: (4, 6), blockHealth: BlockHealth.High),
            new("plates", topID: (1, 4), blockHealth: BlockHealth.High),
            new("cusp", topID: (1, 5), blockHealth: BlockHealth.High),
            new("bunker", topID: (1, 6), blockHealth: BlockHealth.High),
            new("warning", topID: (1, 7), blockHealth: BlockHealth.Medium),

            new("marble_white", topID: (4, 1), sideID: (4, 0), blockHealth: BlockHealth.Medium),
            new("marble_yellow", topID: (4, 3), sideID: (4, 2), blockHealth: BlockHealth.Medium),

            new("brick_grey", topID: (5, 1), sideID: (5, 0), blockHealth: BlockHealth.Medium),
            new("brick_white", topID: (5, 3), sideID: (5, 2), blockHealth: BlockHealth.Medium),
            new("brick_red", topID: (5, 5), sideID: (5, 4), blockHealth: BlockHealth.Medium),
            new("brick_green", topID: (5, 7), sideID: (5, 6), blockHealth: BlockHealth.Medium),
            new("brick_blue", topID: (5, 9), sideID: (5, 8), blockHealth: BlockHealth.Medium),
            new("brick_yellow", topID: (5, 11), sideID: (5, 10), blockHealth: BlockHealth.Medium),
            new("brick_orange", topID: (5, 13), sideID: (5, 12), blockHealth: BlockHealth.Medium),
            new("brick_black", topID: (5, 15), sideID: (5, 14), blockHealth: BlockHealth.Medium),

            new("brick_alt_grey", topID: (4, 9), blockHealth: BlockHealth.Medium),
            new("brick_alt_red", topID: (4, 10), blockHealth: BlockHealth.Medium),
            new("brick_alt_green", topID: (4, 11), blockHealth: BlockHealth.Medium),
            new("brick_alt_blue", topID: (4, 12), blockHealth: BlockHealth.Medium),
            new("brick_alt_yellow", topID: (4, 13), blockHealth: BlockHealth.Medium),
            new("brick_alt_white", topID: (4, 14), blockHealth: BlockHealth.Medium),
            new("brick_alt_black", topID: (4, 15), blockHealth: BlockHealth.Medium),

            new("red", topID: (6, 0), blockHealth: BlockHealth.Low),
            new("magenta", topID: (6, 1), blockHealth: BlockHealth.Low),
            new("purple", topID: (6, 2), blockHealth: BlockHealth.Low),
            new("blue", topID: (6, 3), blockHealth: BlockHealth.Low),
            new("cyan", topID: (6, 4), blockHealth: BlockHealth.Low),
            new("green", topID: (6, 5), blockHealth: BlockHealth.Low),
            new("lime", topID: (6, 6), blockHealth: BlockHealth.Low),
            new("yellow", topID: (6, 7), blockHealth: BlockHealth.Low),
            new("ochre", topID: (6, 8), blockHealth: BlockHealth.Low),
            new("orange", topID: (6, 9), blockHealth: BlockHealth.Low),
            new("brown", topID: (6, 10), blockHealth: BlockHealth.Low),
            new("white", topID: (6, 11), blockHealth: BlockHealth.Low),
            new("light_grey", topID: (6, 12), blockHealth: BlockHealth.Low),
            new("grey", topID: (6, 13), blockHealth: BlockHealth.Low),
            new("dark_grey", topID: (6, 14), blockHealth: BlockHealth.Low),
            new("black", topID: (6, 15), blockHealth: BlockHealth.Low),

            new("red_alt", topID: (7, 5), blockHealth: BlockHealth.Low),
            new("green_alt", topID: (7, 6), blockHealth: BlockHealth.Low),
            new("blue_alt", topID: (7, 7), blockHealth: BlockHealth.Low),
            new("yellow_alt", topID: (7, 8), blockHealth: BlockHealth.Low),
            new("orange_alt", topID: (7, 9), blockHealth: BlockHealth.Low),
            new("white_alt", topID: (7, 10), blockHealth: BlockHealth.Low),
            new("grey_alt", topID: (7, 11), blockHealth: BlockHealth.Low),

            new("steel_red", topID: (3, 10), blockHealth: BlockHealth.Medium),
            new("steel_green", topID: (3, 11), blockHealth: BlockHealth.Medium),
            new("steel_blue", topID: (3, 12), blockHealth: BlockHealth.Medium),
            new("steel_yellow", topID: (3, 13), blockHealth: BlockHealth.Medium),
            new("steel_white", topID: (3, 14), blockHealth: BlockHealth.Medium),
            new("steel_black", topID: (3, 15), blockHealth: BlockHealth.Medium),

            new("barrel", topID: (3, 1), sideID: (3, 0), blockHealth: BlockHealth.Medium),
            new("barrel_blue", topID: (3, 3), sideID: (3, 2), blockHealth: BlockHealth.Medium),
            new("barrel_green", topID: (3, 5), sideID: (3, 4), blockHealth: BlockHealth.Medium),
            new("barrel_yellow", topID: (3, 7), sideID: (3, 6), blockHealth: BlockHealth.Medium),
            new("barrel_white", topID: (3, 9), sideID: (3, 8), blockHealth: BlockHealth.Medium),

            new("water", topID: (0, 15), isSolid: false, isTransparent: true, blockHealth: BlockHealth.NonDiggable),
            new("water_shallow", topID: (0, 14), isSolid: false, isTransparent: true,
                blockHealth: BlockHealth.NonDiggable),

            new("player_block_red", topID: (12, 1), sideID: (12, 0), blockHealth: BlockHealth.Medium),
            new("player_block_blue", topID: (12, 3), sideID: (12, 2), blockHealth: BlockHealth.Medium),
            new("player_block_green", topID: (12, 5), sideID: (12, 4), blockHealth: BlockHealth.Medium),
            new("player_block_yellow", topID: (12, 7), sideID: (12, 6), blockHealth: BlockHealth.Medium),
            new("player_block_armored_red", topID: (12, 9), sideID: (12, 8), blockHealth: BlockHealth.High),
            new("player_block_armored_blue", topID: (12, 11), sideID: (12, 10), blockHealth: BlockHealth.High),
            new("player_block_armored_green", topID: (12, 13), sideID: (12, 12), blockHealth: BlockHealth.High),
            new("player_block_armored_yellow", topID: (12, 15), sideID: (12, 14), blockHealth: BlockHealth.High),

            new("hay", topID: (9, 11), sideID: (9, 10), blockHealth: BlockHealth.Medium),
            new("cactus", topID: (10, 1), sideID: (10, 0), blockHealth: BlockHealth.Medium),
            new("tnt", topID: (14, 15), sideID: (14, 14), bottomID: (10, 2), blockHealth: BlockHealth.OneHit),
            new("window", topID: (1, 13), isTransparent: true, blockHealth: BlockHealth.OneHit),
            new("crate", topID: (2, 13), blockHealth: BlockHealth.Low),
            new("crate_alt", topID: (2, 15), sideID: (2, 14), blockHealth: BlockHealth.Low),
            new("flame_box", topID: (2, 12), blockHealth: BlockHealth.OneHit),
            new("iron_bars", topID: (10, 3), isTransparent: true, blockHealth: BlockHealth.Indestructible),

            new("clay_red", topID: (8, 13), blockHealth: BlockHealth.Medium),
            new("clay_brown", topID: (8, 14), blockHealth: BlockHealth.Medium),
            new("clay_black", topID: (8, 15), blockHealth: BlockHealth.Medium),

            new("tile_orange", topID: (8, 6), blockHealth: BlockHealth.Medium),
            new("tile_green", topID: (8, 7), blockHealth: BlockHealth.Medium),
            new("tile_blue", topID: (8, 8), blockHealth: BlockHealth.Medium),
            new("tile_red", topID: (8, 9), blockHealth: BlockHealth.Medium),
            new("tile_white", topID: (8, 10), blockHealth: BlockHealth.Medium),
            new("tile_black", topID: (8, 11), blockHealth: BlockHealth.Medium),
            new("tile_alt_red", topID: (9, 6), blockHealth: BlockHealth.Low),
            new("tile_alt_green", topID: (9, 7), blockHealth: BlockHealth.Low),
            new("tile_alt_blue", topID: (9, 8), blockHealth: BlockHealth.Low),
            new("tile_alt_yellow", topID: (9, 9), blockHealth: BlockHealth.Low),
            new("sand_alt", topID: (9, 12), blockHealth: BlockHealth.Low),
            new("window_alt", topID: (1, 14), isTransparent: true, blockHealth: BlockHealth.OneHit),
            new("glass", topID: (1, 15), isTransparent: true, blockHealth: BlockHealth.OneHit),

            new("plank_brick_red", topID: (7, 12), blockHealth: BlockHealth.Medium),
            new("plank_brick", topID: (7, 13), blockHealth: BlockHealth.Medium),
            new("plank_brick_gray", topID: (7, 14), blockHealth: BlockHealth.Medium),

            // With damage
            new("lava", topID: (15, 11), isSolid: false, isTransparent: false),
        };

        public static byte Name2Id(string name) => (byte)BlockTypes.ToList()
            .FindIndex(it => string.Equals(it.name, name, StringComparison.CurrentCultureIgnoreCase));

        // We assume all the blocks to have 8 vertices. Stairs, for example make an exception.
        public static readonly Vector3[] VoxelVerts = new Vector3[8]
        {
            new(0.0f, 0.0f, 0.0f),
            new(1.0f, 0.0f, 0.0f),
            new(1.0f, 1.0f, 0.0f),
            new(0.0f, 1.0f, 0.0f),
            new(0.0f, 0.0f, 1.0f),
            new(1.0f, 0.0f, 1.0f),
            new(1.0f, 1.0f, 1.0f),
            new(0.0f, 1.0f, 1.0f),
        };

        // Each int point to one Vector3 of the voxelVerts list
        // The order of each triangle's vertices is constrained by:
        // - Clockwise order to have an outgoing normal vector
        // - The permutation must be the same for all 12 triangles to get a correct UV mapping
        // NOTE: The number of vertices in each is reduced from 6 (3 for each of the 2 triangles) to 4 by sharing the 2 common vertices.
        public static readonly int[,] VoxelTris = new int[6, 4]
        {
            { 0, 3, 1, 2 }, // Back Face
            { 5, 6, 4, 7 }, // Front Face
            { 3, 7, 2, 6 }, // Top Face
            { 1, 5, 0, 4 }, // Bottom Face
            { 4, 7, 0, 3 }, // Left Face
            { 1, 2, 5, 6 } // Right Face
        };


        // UVs are applied by assigning a normalized texture pixel value to the voxel face.
        // The order of the UV Vector2 list matches the order of the Vector3 vertices list.
        // Now that the order is the same for each face, we can write the lookup table for just one face.
        public static readonly Vector2[] VoxelUvs = new Vector2[4]
        {
            new(0.0f, 0.0f),
            new(0.0f, 1.0f),
            new(1.0f, 0.0f),
            new(1.0f, 1.0f)
        };

        // The order matches the faces of the VoxelTris faces.
        public static readonly Vector3Int[] FaceChecks = new Vector3Int[6]
        {
            new(0, 0, -1), // Back Face
            new(0, 0, 1), // Front Face
            new(0, 1, 0), // Top Face
            new(0, -1, 0), // Bottom Face
            new(-1, 0, 0), // Left Face
            new(1, 0, 0) // Right Face
        };

        public static readonly int[] Triangles = { 0, 1, 2, 2, 1, 3 };

        public static readonly Vector3Int[] AdjacentVoxelsToCheck =
        {
            new(0, -1, 0),
            new(0, 0, 1),
            new(0, 0, -1),
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 1, 0),
        };

        public static readonly Vector3Int[] AdjacentVoxels =
        {
            new(0, -1, 0),
            new(-1, -1, 0),
            new(0, -1, 1),
            new(1, -1, 0),
            new(0, -1, -1),
            new(1, -1, -1),
            new(1, -1, 1),
            new(-1, -1, 1),
            new(-1, -1, -1),

            new(-1, 0, -1),
            new(-1, 0, 0),
            new(0, 0, -1),
            new(0, 0, 1),
            new(1, 0, 0),
            new(1, 0, 1),
            new(-1, 0, 1),
            new(1, 0, -1),

            new(0, 1, 0),
            new(1, 1, 0),
            new(-1, 1, 0),
            new(0, 1, 1),
            new(0, 1, -1),
            new(1, 1, 1),
            new(-1, 1, 1),
            new(1, 1, -1),
            new(-1, 1, -1),
        };
    }
}