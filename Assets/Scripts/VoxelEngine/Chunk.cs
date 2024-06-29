using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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
        private GameObject chunkGO;
        private MeshFilter _meshFilter; // Vertices
        private MeshRenderer _meshRenderer; // Light + Material
        private MeshCollider _meshCollider; // Collisions

        private int _vertexIndex;
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new();
        private readonly List<Vector2> _uvs = new();
        private readonly Vector3Int _size;
        private readonly WorldManager _wm;
        public ChunkCoord coord;

        public Chunk(ChunkCoord coord)
        {
            this.coord = coord;
            _wm = WorldManager.instance;
            _size = new Vector3Int(
                math.min(_wm.chunkSize, _wm.map.size.x - coord.worldPos.x),
                Map.MaxHeight,
                math.min(_wm.chunkSize, _wm.map.size.z - coord.worldPos.z)
            );
            chunkGO = new GameObject($"Chunk ({coord.x},{coord.z})");
            chunkGO.transform.SetParent(_wm.transform);
            chunkGO.transform.position = coord.worldPos;

            _meshRenderer = chunkGO.AddComponent<MeshRenderer>();
            _meshFilter = chunkGO.AddComponent<MeshFilter>();
            _meshCollider = chunkGO.AddComponent<MeshCollider>();
            _meshRenderer.material = _wm.material;

            for (var y = 0; y < _size.y; y++)
            for (var x = 0; x < _size.x; x++)
            for (var z = 0; z < _size.z; z++)
                AddVoxel(new Vector3(x, y, z));

            CreateMesh();
        }

        public bool IsActive
        {
            get => chunkGO.activeSelf;
            set => chunkGO.SetActive(value);
        }
        
        // Check if there is a solid voxel (in the world) in the given position
        // NOTE: this is used to apply face pruning
        private bool CheckVoxel(Vector3 pos)
        {
            var worldPos = Vector3Int.FloorToInt(pos + coord.worldPos);
            return _wm.IsVoxelInWorld(worldPos) &&
                   _wm.blockTypes[_wm.map.blocks[worldPos.y, worldPos.x, worldPos.z]].isSolid;
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
            var blockType =
                _wm.blockTypes[
                    _wm.map.blocks[(int)pos.y, (int)pos.x + coord.worldPos.x, (int)pos.z + coord.worldPos.z]];
            // Skip if air
            if (blockType.name == "air") return; // var faces = 0;
            for (var p = 0; p < 6; p++)
            {
                // Skip if current face is hidden
                if (CheckVoxel(pos + VoxelData.FaceChecks[p])) continue;
                for (var i = 0; i < 4; i++)
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, i]]);
                AddTexture(blockType.GetTextureID(p));
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

        private void AddTexture(int textureID)
        {
            var y = 1 - (Mathf.FloorToInt((float)textureID / _wm.atlasCount) + 1) * _wm.AtlasBlockSize;
            var x = textureID % _wm.atlasCount * _wm.AtlasBlockSize;
            foreach (var uv in VoxelData.VoxelUvs)
                _uvs.Add(new Vector2(x, y) + uv * _wm.AtlasBlockSize);
        }
    }
}