using System;
using System.Collections.Generic;
using System.Linq;
using Partials;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace VoxelEngine
{
    public class ChunkCoord
    {
        public readonly int X, Z;
        public readonly Vector3Int WorldPos;

        public ChunkCoord(int x, int z, int chunkSize)
        {
            X = x;
            Z = z;
            WorldPos = new Vector3Int(x * chunkSize, 0, z * chunkSize);
        }
    }

    public class Chunk
    {
        public static Dictionary<ushort, List<Vector2>> cachedUVs;
        public readonly GameObject ChunkGo;
        private MeshFilter _meshFilter; // Vertices
        private readonly MeshRenderer _meshRenderer; // Light + Material
        private MeshCollider _meshCollider; // Collisions

        private int _vertexIndex;
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new(), _transparentTriangles = new();
        private readonly List<Vector2> _uvs = new();
        private readonly Vector3Int _size;
        private readonly WorldManager _wm;
        public readonly ChunkCoord Coord;
        public readonly bool IsSolid;
        private Dictionary<Vector3Int, byte> _removedBlocks = null;

        public bool IsEmpty => _vertices.Count == 0;

        public Chunk(ChunkCoord coord, bool isSolid, WorldManager wm)
        {
            Coord = coord;
            IsSolid = isSolid;
            _wm = wm;
            _size = new Vector3Int(
                math.min(_wm.chunkSize, _wm.Map.size.x - coord.WorldPos.x),
                Map.MaxHeight,
                math.min(_wm.chunkSize, _wm.Map.size.z - coord.WorldPos.z)
            );

            if (cachedUVs == null)
            {
                cachedUVs = new Dictionary<ushort, List<Vector2>>();
                for (ushort id = 0; id < _wm.atlasCount * _wm.atlasCount; id++)
                {
                    var y = 1 - (Mathf.FloorToInt((float)id / _wm.atlasCount) + 1) * _wm.AtlasBlockSize;
                    var x = id % _wm.atlasCount * _wm.AtlasBlockSize;
                    cachedUVs[id] = VoxelData.VoxelUvs.Select(it => new Vector2(x, y) + it * _wm.AtlasBlockSize)
                        .ToList();
                }
            }

            ChunkGo = new GameObject($"Chunk ({coord.X},{coord.Z}) " + (isSolid ? "Solid" : "NonSolid"))
                { layer = LayerMask.NameToLayer("Ground") };
            ChunkGo.transform.SetParent(_wm.transform);
            ChunkGo.transform.position = coord.WorldPos;

            _meshRenderer = ChunkGo.AddComponent<MeshRenderer>();
            _meshFilter = ChunkGo.AddComponent<MeshFilter>();
            _meshCollider = ChunkGo.AddComponent<MeshCollider>();
            _meshRenderer.materials = new[] { _wm.material, _wm.transparentMaterial };

            UpdateMesh();
            ChunkGo.SetActive(false);
        }

        public Chunk(Dictionary<Vector3Int, byte> removedBlocks, WorldManager wm)
        {
            _removedBlocks = removedBlocks;
            _wm = wm;
            IsSolid = true;
            ChunkGo = new GameObject($"Debris Chunk");
            ChunkGo.transform.SetParent(_wm.transform);

            // coord = new ChunkCoord(removedBlocks.Keys.Min(it => it.x), removedBlocks.Keys.Min(it => it.z));
            ChunkGo.transform.position = removedBlocks.Keys.Aggregate(Vector3.zero, (acc, v) => acc + v) /
                                         removedBlocks.Keys.Count;

            _meshRenderer = ChunkGo.AddComponent<MeshRenderer>();
            _meshFilter = ChunkGo.AddComponent<MeshFilter>();
            _meshCollider = ChunkGo.AddComponent<MeshCollider>();
            _meshRenderer.materials = new[] { _wm.material };

            UpdateMesh();
            ChunkGo.SetActive(true);
        }

        public bool IsActive
        {
            get => ChunkGo.activeSelf;
            set => ChunkGo.SetActive(value);
        }

        // Check if there is a solid voxel (in the world) in the given position
        // NOTE: this is used to apply face pruning
        private bool CheckVoxel(Vector3Int posNorm, byte currentBlockType)
        {
            if (_removedBlocks != null) return _removedBlocks.ContainsKey(posNorm);
            var worldPos = posNorm + Coord.WorldPos;
            if (!_wm.IsVoxelInWorld(worldPos)) return false;
            var blockId = _wm.Map.Blocks[worldPos.y, worldPos.x, worldPos.z];
            return (VoxelData.BlockTypes[blockId].isSolid && !VoxelData.BlockTypes[blockId].isTransparent) || blockId == currentBlockType;
        }

        // The following can be (a little) more efficiently rewritten using arrays instead of lists
        // NOTE: Vertices cannot be shared across different faces, otherwise Unity will attempt to
        // smoothen them, and we won't get the desired cube crisp edges.
        // So each extern vertex is repeated 3 times, 1 for each of the three adjacent faces.
        // The used optimizations are:
        // - Faces are drawn only if visible thanks to _voxelMap[,,] (faces pruning)
        // - Each face has 4 vertices instead of 6 (vertices pruning)
        private void AddVoxel(Vector3 pos)
        {
            var blockId = _removedBlocks == null
                ? _wm.Map.Blocks[(int)pos.y, (int)pos.x + Coord.WorldPos.x, (int)pos.z + Coord.WorldPos.z]
                : _removedBlocks[Vector3Int.FloorToInt(pos + ChunkGo.transform.position)];
            var blockType = VoxelData.BlockTypes[blockId];
            // Skip if air
            if (blockType.name == "air") return;
            if ((IsSolid && !blockType.isSolid) || (!IsSolid && blockType.isSolid))
                return;
            for (var p = 0; p < 6; p++)
            {
                // Skip if the current face is hidden
                if (CheckVoxel(Vector3Int.FloorToInt(pos) + VoxelData.FaceChecks[p], blockId)) continue;
                for (var i = 0; i < 4; i++)
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, i]]);
                _uvs.AddRange(cachedUVs[blockType.TextureIDs[p]]);
                for (var i = 0; i < VoxelData.Triangles.Length; i++)
                    if (blockType.isTransparent)
                        _transparentTriangles.Add(_vertexIndex + VoxelData.Triangles[i]);
                    else
                        _triangles.Add(_vertexIndex + VoxelData.Triangles[i]);
                _vertexIndex += 4;
            }
        }

        public void UpdateMesh()
        {
            // var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            ClearMesh();
            // Debug.Log(DateTimeOffset.Now.ToUnixTimeMilliseconds() - start);

            if (_removedBlocks == null)
                for (var y = 0; y < _size.y; y++)
                for (var x = 0; x < _size.x; x++)
                for (var z = 0; z < _size.z; z++)
                    AddVoxel(new Vector3(x, y, z));
            else
                foreach (var block in _removedBlocks.Keys)
                    AddVoxel(block - ChunkGo.transform.position);

            var mesh = new Mesh
            {
                indexFormat = IndexFormat.UInt32,
                vertices = _vertices.ToArray(),
                subMeshCount = _removedBlocks == null ? 2 : 1,
                uv = _uvs.ToArray()
            };

            mesh.SetTriangles(_triangles.ToArray(), 0);
            if (_removedBlocks == null)
                mesh.SetTriangles(_transparentTriangles.ToArray(), 1);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            if (IsSolid && _removedBlocks == null)
                _meshCollider.sharedMesh = mesh;
            _meshFilter.mesh = mesh;

            // If this chunk is a group of flying voxels, let it fall and disappear from the map.
            if (_removedBlocks != null)
            {
                _meshCollider.convex = true;
                var rb = ChunkGo.AddComponent<Rigidbody>();
                var dir = Vector3.up *
                          Random.Range(1f,
                              2f); //+ Vector3.left * Random.Range(-0.5f, 0.5f) +Vector3.forward * Random.Range(-0.5f, 0.5f);
                rb.AddRelativeForce(dir, ForceMode.Impulse);
                // rb.AddForceAtPosition(dir, Vector3.zero, ForceMode.Impulse);
                rb.angularVelocity = Vector3.up * 0.25f + Vector3.left * Random.Range(-0.35f, 0.35f) +
                                     Vector3.forward * Random.Range(-0.35f, 0.35f);
                ChunkGo.AddComponent<Destroyable>().lifespan = 10f;
            }
        }

        private void ClearMesh()
        {
            _vertexIndex = 0;
            _vertices.Clear();
            _triangles.Clear();
            _transparentTriangles.Clear();
            _uvs.Clear();
        }

        public List<Chunk> UpdateAdjacentChunks(Vector3Int[] posNorms)
        {
            var updatedChunks = new List<Chunk>();
            foreach (var posNorm in posNorms)
                for (var p = 0; p < 6; p++)
                {
                    var currentVoxel = posNorm + VoxelData.FaceChecks[p];
                    if (!IsVoxelInChunk(currentVoxel))
                    {
                        var chunk = _wm.GetChunk(currentVoxel);
                        if (chunk is not null && !updatedChunks.Contains(chunk))
                        {
                            Debug.Log($"Update {currentVoxel}, p={p}, chunk={chunk.Coord.WorldPos.ToString()}");
                            chunk.UpdateMesh();
                            updatedChunks.Add(chunk);
                        }
                    }
                }

            return updatedChunks;
        }

        private bool IsVoxelInChunk(Vector3Int pos) =>
            pos.x >= Coord.X * _wm.chunkSize && pos.x < (Coord.X + 1) * _wm.chunkSize &&
            pos.z >= Coord.Z * _wm.chunkSize && pos.z < (Coord.Z + 1) * _wm.chunkSize;
    }
}