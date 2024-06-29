using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine
{
    public class ChunkCoord
    {
        public readonly int x, z;

        public ChunkCoord(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public Vector3 ToWorldPoint() =>
            new(x * WorldManager.instance.chunkSize, 0f, z * WorldManager.instance.chunkSize);
    }

    public class Chunk
    {
        private GameObject chunkGO;
        private MeshFilter _meshFilter; // Vertices
        private MeshRenderer _meshRenderer; // Light + Material
        private MeshCollider _meshCollider; // Collisions

        private int _vertexIndex = 0;
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new();

        private readonly List<Vector2> _uvs = new();

        // We assume to have no more than 256 types of blocks. Otherwise an int would be required (4x size)
        private readonly byte[,,] _blocks; // y,x,z
        private readonly Vector3Int _mapSize;
        private readonly WorldManager _wm;

        public ChunkCoord coord;


        public Chunk(ChunkCoord coord, byte[,,] blocks, WorldManager wm)
        {
            this.coord = coord;
            this._blocks = blocks;
            _mapSize = new Vector3Int(blocks.GetLength(1), blocks.GetLength(0), blocks.GetLength(2));
            _wm = wm;
            chunkGO = new GameObject($"Chunk ({coord.x},{coord.z})");
            chunkGO.transform.SetParent(wm.transform);
            chunkGO.transform.position = coord.ToWorldPoint();

            _meshRenderer = chunkGO.AddComponent<MeshRenderer>();
            _meshFilter = chunkGO.AddComponent<MeshFilter>();
            _meshCollider = chunkGO.AddComponent<MeshCollider>();
            _meshRenderer.material = wm.material;

            for (var y = 0; y < _mapSize.y; y++)
            for (var x = 0; x < _mapSize.x; x++)
            for (var z = 0; z < _mapSize.z; z++)
                AddVoxel(new Vector3(x, y, z));

            CreateMesh();
        }

        private bool CheckVoxel(Vector3 pos)
        {
            var x = Mathf.FloorToInt(pos.x);
            var y = Mathf.FloorToInt(pos.y);
            var z = Mathf.FloorToInt(pos.z);

            if (x < 0 || x > _mapSize.x - 1 || y < 0 || y > _mapSize.y - 1 || z < 0 ||
                z > _mapSize.z - 1)
                return false;

            return _wm.blockTypes[_blocks[y, x, z]].isSolid;
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
            var blockID = _blocks[(int)pos.y, (int)pos.x, (int)pos.z];
            // Skip if air
            if (blockID == 0) return;
            var blockType = _wm.blockTypes[blockID];
            // var faces = 0;
            for (var p = 0; p < 6; p++)
            {
                // Skip if current face is hidden
                if (CheckVoxel(pos + VoxelData.FaceChecks[p])) continue;
                // faces++;
                for (var i = 0; i < 4; i++)
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, i]]);
                AddTexture(blockType.GetTextureID(p));
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
            var y = 1 - (Mathf.FloorToInt((float)textureID / _wm.atlasCount) + 1) * _wm.AtlasBlockSize;
            var x = textureID % _wm.atlasCount * _wm.AtlasBlockSize;
            foreach (var uv in VoxelData.VoxelUvs)
                _uvs.Add(new Vector2(x, y) + uv * _wm.AtlasBlockSize);
        }
    }
}