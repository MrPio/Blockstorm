using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Utils;
using Random = UnityEngine.Random;

namespace VoxelEngine
{
    public class ChunkCoord
    {
        public readonly int x, z;
        public readonly Vector3Int worldPos;

        public ChunkCoord(int x, int z)
        {
            this.x = x;
            this.z = z;
            worldPos = new Vector3Int(x * WorldManager.instance.chunkSize, 0, z * WorldManager.instance.chunkSize);
        }
    }

    public class Chunk
    {
        public readonly GameObject chunkGo;
        private MeshFilter _meshFilter; // Vertices
        private MeshRenderer _meshRenderer; // Light + Material
        private MeshCollider _meshCollider; // Collisions

        private int _vertexIndex;
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new(), _transparentTriangles = new();
        private readonly List<Vector2> _uvs = new();
        private readonly Vector3Int _size;
        private readonly WorldManager _wm;
        public readonly ChunkCoord coord;
        public readonly bool isSolid;
        private Dictionary<Vector3Int, byte> _removedBlocks = null;

        public bool IsEmpty => _vertices.Count == 0;

        public Chunk(ChunkCoord coord, bool isSolid)
        {
            this.coord = coord;
            this.isSolid = isSolid;
            _wm = WorldManager.instance;
            _size = new Vector3Int(
                math.min(_wm.chunkSize, _wm.map.size.x - coord.worldPos.x),
                Map.MaxHeight,
                math.min(_wm.chunkSize, _wm.map.size.z - coord.worldPos.z)
            );
            chunkGo = new GameObject($"Chunk ({coord.x},{coord.z}) " + (isSolid ? "Solid" : "NonSolid"))
                { layer = LayerMask.NameToLayer("Ground") };
            chunkGo.transform.SetParent(_wm.transform);
            chunkGo.transform.position = coord.worldPos;

            _meshRenderer = chunkGo.AddComponent<MeshRenderer>();
            _meshFilter = chunkGo.AddComponent<MeshFilter>();
            _meshCollider = chunkGo.AddComponent<MeshCollider>();
            _meshRenderer.materials = new[] { _wm.material, _wm.transparentMaterial };

            UpdateMesh();
            chunkGo.SetActive(false);
        }

        public Chunk(Dictionary<Vector3Int, byte> removedBlocks)
        {
            this._removedBlocks = removedBlocks;
            this.isSolid = true;
            _wm = WorldManager.instance;
            chunkGo = new GameObject($"Debris Chunk");
            chunkGo.transform.SetParent(_wm.transform);

            // coord = new ChunkCoord(removedBlocks.Keys.Min(it => it.x), removedBlocks.Keys.Min(it => it.z));
            chunkGo.transform.position = removedBlocks.Keys.Aggregate(Vector3.zero, (acc, v) => acc + v) /
                                         removedBlocks.Keys.Count;

            _meshRenderer = chunkGo.AddComponent<MeshRenderer>();
            _meshFilter = chunkGo.AddComponent<MeshFilter>();
            _meshCollider = chunkGo.AddComponent<MeshCollider>();
            _meshRenderer.materials = new[] { _wm.material };

            UpdateMesh();
            chunkGo.SetActive(true);
        }

        public bool IsActive
        {
            get => chunkGo.activeSelf;
            set => chunkGo.SetActive(value);
        }

        // Check if there is a solid voxel (in the world) in the given position
        // NOTE: this is used to apply face pruning
        private bool CheckVoxel(Vector3 pos, byte currentBlockType)
        {
            var posNorm = Vector3Int.FloorToInt(pos);
            if (_removedBlocks == null)
            {
                var worldPos = Vector3Int.FloorToInt(pos + coord.worldPos);
                return _wm.IsVoxelInWorld(worldPos) &&
                       (_wm.blockTypes[_wm.map.blocks[worldPos.y, worldPos.x, worldPos.z]].isSolid ||
                        _wm.map.blocks[worldPos.y, worldPos.x, worldPos.z] == currentBlockType);
            }

            return _removedBlocks.ContainsKey(posNorm);
        }

        // The following can be (a little) more efficiently rewritten using arrays instead of lists
        // NOTE: Vertices cannot be shared across different faces, otherwise Unity will attempt to
        // smoothen them and we won't obtain the desired cube crisp edges.
        // So each extern vertex is repeated 3 times, 1 for each of the three adjacent faces.
        // The used optimizations are:
        // - Faces are drawn only if visible thanks to _voxelMap[,,] (faces pruning)
        // - Each face has 4 vertices instead of 6 (vertices pruning)
        private void AddVoxel(Vector3 pos)
        {
            var blockId = _removedBlocks == null
                ? _wm.map.blocks[(int)pos.y, (int)pos.x + coord.worldPos.x, (int)pos.z + coord.worldPos.z]
                : _removedBlocks[Vector3Int.FloorToInt(pos + chunkGo.transform.position)];
            var blockType = _wm.blockTypes[blockId];
            // Skip if air
            if (blockType.name == "air") return;
            if ((isSolid && !blockType.isSolid) || (!isSolid && blockType.isSolid))
                return;
            for (var p = 0; p < 6; p++)
            {
                // Skip if current face is hidden
                if (CheckVoxel(pos + VoxelData.FaceChecks[p], blockId)) continue;
                for (var i = 0; i < 4; i++)
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, i]]);
                AddTexture(blockType.GetTextureID(p));
                foreach (var i in new[] { 0, 1, 2, 2, 1, 3 })
                    if (blockType.isTransparent)
                        _transparentTriangles.Add(_vertexIndex + i);
                    else
                        _triangles.Add(_vertexIndex + i);
                _vertexIndex += 4;
            }
        }

        public void UpdateMesh()
        {
            ClearMesh();

            if (_removedBlocks == null)
                for (var y = 0; y < _size.y; y++)
                for (var x = 0; x < _size.x; x++)
                for (var z = 0; z < _size.z; z++)
                    AddVoxel(new Vector3(x, y, z));
            else
                foreach (var block in _removedBlocks.Keys)
                    AddVoxel(block - chunkGo.transform.position);

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

            if (isSolid && _removedBlocks == null)
                _meshCollider.sharedMesh = mesh;
            _meshFilter.mesh = mesh;

            if (_removedBlocks != null)
            {
                _meshCollider.convex = true;
                var rb = chunkGo.AddComponent<Rigidbody>();
                var dir = Vector3.up * Random.Range(1f, 2f); //+ Vector3.left * Random.Range(-0.5f, 0.5f) +Vector3.forward * Random.Range(-0.5f, 0.5f);
                rb.AddRelativeForce(dir, ForceMode.Impulse);
                // rb.AddForceAtPosition(dir, Vector3.zero, ForceMode.Impulse);
                rb.angularVelocity = Vector3.up * 0.25f + Vector3.left * Random.Range(-0.35f, 0.35f) +Vector3.forward * Random.Range(-0.35f, 0.35f);
                chunkGo.AddComponent<Destroyable>().lifespan = 10f;
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

        public void UpdateAdjacentChunks(Vector3Int posNorm)
        {
            for (var p = 0; p < 6; p++)
            {
                var currentVoxel = posNorm + VoxelData.FaceChecks[p];
                if (!IsVoxelInChunk(currentVoxel))
                    _wm.GetChunk(currentVoxel)?.UpdateMesh();
            }
        }

        public bool IsVoxelInChunk(Vector3Int pos) =>
            pos.x >= 0 && pos.x < _wm.chunkSize && pos.z >= 0 && pos.z < _wm.chunkSize;


        private void AddTexture(int textureID)
        {
            var y = 1 - (Mathf.FloorToInt((float)textureID / _wm.atlasCount) + 1) * _wm.AtlasBlockSize;
            var x = textureID % _wm.atlasCount * _wm.AtlasBlockSize;
            foreach (var uv in VoxelData.VoxelUvs)
                _uvs.Add(new Vector2(x, y) + uv * _wm.AtlasBlockSize);
        }
    }
}