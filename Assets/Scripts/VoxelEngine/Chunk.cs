using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine
{
    [RequireComponent(typeof(MeshFilter))] // Vertices
    [RequireComponent(typeof(MeshRenderer))] // Light + Material
    [RequireComponent(typeof(MeshCollider))] // Collisions
    public class Chunk : MonoBehaviour
    {
        [SerializeField] private int chunkSize = 5;
        [SerializeField] private int chunkHeight = 15;
        [SerializeField] private int worldChunks = 50;
        [SerializeField] private int viewChunksDistance = 8;
        [SerializeField] private int textureAtlasSizeInBlocks = 4;
        public int WorldBlocks => worldChunks * chunkSize;
        public float NormalizedBlockTextureSize => 1f / (float)textureAtlasSizeInBlocks;

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;

        private int _vertexIndex = 0;
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new();
        private readonly List<Vector2> _uvs = new();
        private bool[,,] _voxelMap;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        private void Start()
        {
            _voxelMap = new bool[chunkSize, chunkHeight, chunkSize];
            for (var y = 0; y < chunkHeight; y++)
            for (var x = 0; x < chunkSize; x++)
            for (var z = 0; z < chunkSize; z++)
                _voxelMap[x, y, z] = true;

            for (var y = 0; y < chunkHeight; y++)
            for (var x = 0; x < chunkSize; x++)
            for (var z = 0; z < chunkSize; z++)
                AddVoxelDataToChunk(new Vector3(x, y, z));

            CreateMesh();
        }

        private bool CheckVoxel(Vector3 pos)    
        {
            var x = Mathf.FloorToInt(pos.x);
            var y = Mathf.FloorToInt(pos.y);
            var z = Mathf.FloorToInt(pos.z);

            if (x < 0 || x > chunkSize - 1 || y < 0 || y > chunkHeight - 1 || z < 0 ||
                z > chunkSize - 1)
                return false;

            return _voxelMap[x, y, z];
        }

        // The following can be (a little) more efficiently rewritten using arrays instead of lists
        // NOTE: Vertices cannot be shared across different faces, otherwise Unity will attempt to
        // smoothen them and we won't obtain the desired cube crisp edges 
        private void AddVoxelDataToChunk(Vector3 pos)
        {
            for (var p = 0; p < 6; p++)
            {
                // Skip if current face is hidden
                if (CheckVoxel(pos + VoxelData.FaceChecks[p])) continue;
                for (var i = 0; i < 4; i++)
                {
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, i]]);
                    _uvs.Add(VoxelData.VoxelUvs[i]);
                }

                foreach (var i in new[] { 0, 1, 2, 2, 1, 3 })
                    _triangles.Add(_vertexIndex + i);
                _vertexIndex += 4;
            }
        }

        private void CreateMesh()
        {
            var mesh = new Mesh
            {
                indexFormat = IndexFormat.UInt32,
                vertices = _vertices.ToArray(),
                triangles = _triangles.ToArray(),
                uv = _uvs.ToArray()
            };
            mesh.RecalculateNormals();
            _meshFilter.mesh = mesh;
        }
    }
}