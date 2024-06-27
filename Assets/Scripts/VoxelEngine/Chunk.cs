using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))] // Vertices
[RequireComponent(typeof(MeshRenderer))] // Light + Material
[RequireComponent(typeof(MeshCollider))] // Collisions
public class Chunk : MonoBehaviour
{
    public Vector3 chunkPosition;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    private void Start()
    {
        var vertexIndex = 0;
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uvs = new List<Vector2>();

        // The following can be (a little) more efficiently rewritten using arrays instead of lists
        // NOTE: Vertices cannot be shared across different faces, otherwise Unity will attempt to
        // smoothen them and we won't obtain the desired cube crisp edges 
        for (var p = 0; p < 6; p++)
        {
            for (var i = 0; i < 6; i++)
            {
                var triangleIndex = VoxelData.voxelTris[p, i];
                vertices.Add(VoxelData.voxelVerts[triangleIndex]);
                triangles.Add(vertexIndex);
                uvs.Add(VoxelData.voxelUvs[i]);
                vertexIndex++;
            }
        }

        var mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }
}