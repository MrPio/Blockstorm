using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

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

        // We assume to have no more than 256 types of blocks. Otherwise an int would be required (4x size)
        private byte[,,] _voxelMap;
        private WorldManager _worldManager;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _worldManager = WorldManager.Instance;
        }


        private void Start()
        {
            _voxelMap = new byte[chunkSize, chunkHeight, chunkSize];
            // We initialize to true all the cubes positions. This enables faces pruning.
            for (var y = 0; y < chunkHeight; y++)
            for (var x = 0; x < chunkSize; x++)
            for (var z = 0; z < chunkSize; z++)
                _voxelMap[x, y, z] = 0;

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

            return _worldManager.blockTypes[_voxelMap[x, y, z]].isSolid;
        }

        // The following can be (a little) more efficiently rewritten using arrays instead of lists
        // NOTE: Vertices cannot be shared across different faces, otherwise Unity will attempt to
        // smoothen them and we won't obtain the desired cube crisp edges.
        // So each extern vertex is repeated 3 times, 1 for each of the three adjacent faces.
        // The used optimizations are:
        // - Faces are drawn only if visible thanks to _voxelMap[,,] (faces pruning)
        // - Each face has 4 vertices instead of 6 (vertices pruning)
        private void AddVoxelDataToChunk(Vector3 pos)
        {
            var faces = 0;
            for (var p = 0; p < 6; p++)
            {
                // Skip if current face is hidden
                if (CheckVoxel(pos + VoxelData.FaceChecks[p])) continue;
                faces++;
                for (var i = 0; i < 4; i++)
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, i]]);
                AddTexture((faces*11)%256);
                foreach (var i in new[] { 0, 1, 2, 2, 1, 3 })
                    _triangles.Add(_vertexIndex + i);
                _vertexIndex += 4;
            }
            // print($"Drawn {faces} faces");
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

        private void AddTexture(int textureID)
        {
            var y = 1 - Mathf.CeilToInt((float)textureID / _worldManager.atlasCount) * _worldManager.AtlasBlockSize;
            var x = textureID % _worldManager.atlasCount * _worldManager.AtlasBlockSize;
            foreach (var uv in VoxelData.VoxelUvs)
                _uvs.Add(new Vector2(x, y) + uv * _worldManager.AtlasBlockSize);
        }
    }
}