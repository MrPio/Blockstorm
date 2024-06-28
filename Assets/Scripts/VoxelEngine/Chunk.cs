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


        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        private void Start()
        {
            for (var y = -10; y < 0; y++)
            for (var x = -50; x < 50; x++)
            for (var z = -50; z < 50; z++)
                AddVoxelDataToChunk(new Vector3(x, y, z));
            CreateMesh();
        }

        
        private void AddVoxelDataToChunk(Vector3 pos)
        {
            // The following can be (a little) more efficiently rewritten using arrays instead of lists
            // NOTE: Vertices cannot be shared across different faces, otherwise Unity will attempt to
            // smoothen them and we won't obtain the desired cube crisp edges 
            for (var p = 0; p < 6; p++)
            {
                for (var i = 0; i < 6; i++)
                {
                    var triangleIndex = VoxelData.VoxelTris[p, i];
                    _vertices.Add(VoxelData.VoxelVerts[triangleIndex] + pos);
                    _triangles.Add(_vertexIndex);
                    _uvs.Add(VoxelData.VoxelUvs[i]);
                    _vertexIndex++;
                }
            }
        }

        private void CreateMesh()
        {
            var mesh = new Mesh
            {
                // indexFormat = IndexFormat.UInt32,
                vertices = _vertices.ToArray(),
                triangles = _triangles.ToArray(),
                uv = _uvs.ToArray()
            };
            mesh.RecalculateNormals();
            _meshFilter.mesh = mesh;
        }
    }
}