using UnityEngine;

public static class VoxelData
{
    public static readonly int ChunkWidth = 5;
    public static readonly int ChunkHeight = 15;
    public static readonly int WorldSizeInChunks = 50;
    public static readonly int ViewDistanceInChunks = 8;

    public static int WorldSizeInBlocks
    {
        get { return WorldSizeInChunks * ChunkWidth; }
    }

    public static readonly int TextureAtlasSizeInBlocks = 4;

    public static float NormalizedBlockTextureSize
    {
        get { return 1f / (float)TextureAtlasSizeInBlocks; }
    }

    // We assume all the blocks to have 8 vertices. Stairs, for example make an exception.
    public static readonly Vector3[] voxelVerts = new Vector3[8]
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
    public static readonly int[,] voxelTris = new int[6,6] {

        {0, 3, 1, 1, 3, 2}, // Back Face
        {5, 6, 4, 4, 6, 7}, // Front Face
        {3, 7, 2, 2, 7, 6}, // Top Face
        {1, 5, 0, 0, 5, 4}, // Bottom Face
        {4, 7, 0, 0, 7, 3}, // Left Face
        {1, 2, 5, 5, 2, 6} // Right Face

    };
    
    
    // UVs are applied by assigning a normalized texture pixel value to the voxel face.
    // The order of the UV Vector2 list matches the order of the Vector3 vertices list.
    // Now that the order is the same for each face, we can write the lookup table for just one face.
    public static readonly Vector2[] voxelUvs = new Vector2[6]
    {
        new(0.0f, 0.0f),
        new(0.0f, 1.0f),
        new(1.0f, 0.0f),
        new(1.0f, 0.0f),
        new(0.0f, 1.0f),
        new(1.0f, 1.0f)
        
    };

    public static readonly Vector3[] faceChecks = new Vector3[6]
    {
        new(0.0f, 0.0f, -1.0f),
        new(0.0f, 0.0f, 1.0f),
        new(0.0f, 1.0f, 0.0f),
        new(0.0f, -1.0f, 0.0f),
        new(-1.0f, 0.0f, 0.0f),
        new(1.0f, 0.0f, 0.0f)
    };


}