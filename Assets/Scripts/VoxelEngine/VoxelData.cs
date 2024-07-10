using UnityEngine;

namespace VoxelEngine
{
    public static class VoxelData
    {
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
    }
}