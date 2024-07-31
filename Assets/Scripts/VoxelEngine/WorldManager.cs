using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public class WorldManager : MonoBehaviour
    {
        public static ushort maxPlayers = 32;

        public byte BlockTypeIndex(string blockName) =>
            (byte)VoxelData.BlockTypes.ToList().FindIndex(it => it.name == blockName.ToLower());

        public Material material, transparentMaterial;
        [Range(1, 128)] public int chunkSize = 2;
        [Range(1, 512)] public int viewDistance = 2;
        public int atlasCount = 16;
        public float AtlasBlockSize => 1f / atlasCount;
        private Chunk[,] _chunks;
        [ItemCanBeNull] private Chunk[,] _nonSolidChunks;
        [CanBeNull] private List<Chunk> _brokenChunks = new();
        [NonSerialized] public Map Map;
        private Vector3 _playerLastPos;
        [SerializeField] private string mapName = "Harbor";
        private bool _hasRendered;

        private void Start()
        {
            chunkSize = math.max(1, chunkSize);
            viewDistance = math.max(1, viewDistance);
            Map = Map.GetMap(mapName);
        }

        public void RenderMap()
        {
            var mapSize = Map.size;
            var chunksX = Mathf.CeilToInt((float)mapSize.x / chunkSize);
            var chunksZ = Mathf.CeilToInt((float)mapSize.z / chunkSize);
            _chunks = new Chunk[chunksX, chunksZ];
            _nonSolidChunks = new Chunk[chunksX, chunksZ];
            for (var x = 0; x < chunksX; x++)
            for (var z = 0; z < chunksZ; z++)
            {
                _chunks[x, z] = new Chunk(new ChunkCoord(x, z, chunkSize), isSolid: true, this);

                _nonSolidChunks[x, z] = new Chunk(new ChunkCoord(x, z, chunkSize), isSolid: false, this);
                if (_nonSolidChunks[x, z].IsEmpty)
                {
                    Destroy(_nonSolidChunks[x, z].ChunkGo);
                    _nonSolidChunks[x, z] = null;
                }
            }

            _hasRendered = true;
        }

        public bool IsVoxelInWorld(Vector3Int pos) =>
            pos.x >= 0 && pos.x < Map.size.x && pos.y >= 0 && pos.y < Map.size.y && pos.z >= 0 && pos.z < Map.size.z;

        [CanBeNull]
        public BlockType GetVoxel(Vector3Int pos) =>
            IsVoxelInWorld(pos) ? VoxelData.BlockTypes[Map.Blocks[pos.y, pos.x, pos.z]] : null;

        // This is used to update the rendered chunks
        public void UpdatePlayerPos(Vector3 playerPos)
        {
            if (!_hasRendered || Vector3.Distance(_playerLastPos, playerPos) < chunkSize * .9)
                return;
            _playerLastPos = playerPos;
            for (var x = 0; x < _chunks.GetLength(0); x++)
            for (var z = 0; z < _chunks.GetLength(1); z++)
            {
                _chunks[x, z].IsActive = math.abs(x * chunkSize - playerPos.x) < viewDistance &&
                                         math.abs(z * chunkSize - playerPos.z) < viewDistance;
                if (_nonSolidChunks[x, z] != null)
                    _nonSolidChunks[x, z].IsActive = _chunks[x, z].IsActive;
            }
        }

        [CanBeNull]
        public Chunk GetChunk(Vector3Int posNorm)
        {
            posNorm /= chunkSize;
            if (posNorm.x < _chunks.GetLength(0) && posNorm.z < _chunks.GetLength(1))
                return _chunks[posNorm.x, posNorm.z];
            return null;
        }

        public void EditVoxels(List<Vector3> positions, byte newID)
        {
            var posNorms = positions.Select(Vector3Int.FloorToInt).ToList();
            foreach (var posNorm in posNorms)
            {
                if (VoxelData.BlockTypes[Map.Blocks[posNorm.y, posNorm.x, posNorm.z]].blockHealth !=
                    BlockHealth.Indestructible)
                    Map.Blocks[posNorm.y, posNorm.x, posNorm.z] = newID;
                Map.BlocksEdits[posNorm] = newID;
            }

            var chunks = new List<Chunk>();
            foreach (var posNorm in posNorms)
            {
                if (newID == 0)
                    CheckForFlyingMesh(posNorm);
                var chunk = GetChunk(posNorm);
                if (!chunks.Contains(chunk))
                {
                    chunks.Add(chunk);
                    chunk!.UpdateMesh();
                    chunks.AddRange(chunk!.UpdateAdjacentChunks(posNorms.ToArray()));
                }
            }
        }

        public bool DamageVoxel(Vector3 pos, uint damage)
        {
            var posNorm = Vector3Int.FloorToInt(pos);
            if (Map.DamageBlock(Vector3Int.FloorToInt(pos), damage) <= 0)
            {
                EditVoxels(new() { posNorm }, 0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Starting from a given voxel, the neighboring voxels are visited recursively until
        /// a detached structure is found or until the bottom of the map is reached.
        /// </summary>
        /// <param name="posNorm">The position of the starting voxel.</param>
        /// <param name="visited">The list of already visited blocks.
        /// This is used to implement an acyclic <b>Depth-first</b> search.</param>
        /// <param name="stopVoxels">A list of voxels that, if found, invalidate the recursion.
        /// This is used to reduce computation when checking for flying structures around a destroyed voxel</param>
        /// <returns>
        /// A list of block positions if a flying structure was found, or null otherwise.
        /// </returns>
        private List<Vector3Int> GetAdjacentSolids(Vector3Int posNorm, List<Vector3Int> visited = null,
            List<Vector3Int> stopVoxels = null)
        {
            visited ??= new List<Vector3Int>();
            stopVoxels ??= new List<Vector3Int>();
            visited.Add(posNorm);
            var totalAdjacentSolids = new List<Vector3Int> { posNorm };

            // Explore all the neighbouring voxels.
            foreach (var adjacent in VoxelData.AdjacentVoxels)
            {
                var newPos = posNorm + adjacent;

                // Skip this voxel if already visited or terminate if is a stop voxel.
                if (visited.Contains(newPos))
                    continue;
                if (stopVoxels.Contains(newPos))
                    return null;

                // Get the voxel at the current position
                var adjacentVoxel = GetVoxel(newPos);

                // Terminate the recursion if an indestructible voxel is reached.
                if (newPos.y < 1 || adjacentVoxel is null ||
                    adjacentVoxel is { blockHealth: BlockHealth.Indestructible })
                    return null;

                // Skip the block if non-solid
                if (adjacentVoxel is not { isSolid: true })
                    continue;

                // Continue the depth-first search
                var adjacentSolids = GetAdjacentSolids(newPos, visited);

                // No flying structure can be found if any recursion cap condition is met.
                if (adjacentSolids == null)
                    return null;

                totalAdjacentSolids.AddRange(adjacentSolids);

                // Stop the recursion if when too many voxels have been visited.
                // This reduces the cost of recursion.
                if (totalAdjacentSolids.Count > 4000)
                    return null;
            }

            return totalAdjacentSolids;
        }

        /// <summary>
        /// Destroying a voxel can create a flying group of voxels.
        /// Therefore, for each neighbouring voxel we check if a flying structure has been created.
        /// A fall animation is created for each flying structure detected and those voxels are removed from their chunks.
        /// </summary>
        /// <param name="posNorm">The position of the destroyed block.</param>
        private void CheckForFlyingMesh(Vector3Int posNorm)
        {
            var chunksToUpdate = new List<Chunk>();
            var stopVoxels = new List<Vector3Int>();
            foreach (var adjacent in VoxelData.AdjacentVoxelsToCheck)
            {
                // Skip if the block is an air block or an indestructible block.
                var newPos = posNorm + adjacent;
                var adjacentVoxel = GetVoxel(newPos);
                if (adjacentVoxel is { isSolid: false } or { blockHealth: BlockHealth.Indestructible })
                    continue;

                // Check if there is a flying structure that branches off from this neighboring voxel.
                var flyingBlocks = GetAdjacentSolids(posNorm + adjacent, stopVoxels: stopVoxels);

                // If no flying structure has been found, try the next neighboring voxel.
                if (flyingBlocks == null)
                    continue;

                // Remove the flying blocks from their chunks and update their meshes.
                stopVoxels.AddRange(flyingBlocks);
                var removedBlocks = new Dictionary<Vector3Int, byte>();
                foreach (var block in flyingBlocks)
                {
                    removedBlocks[new Vector3Int(block.x, block.y, block.z)] = Map.Blocks[block.y, block.x, block.z];
                    Map.Blocks[block.y, block.x, block.z] = 0;
                    Map.BlocksEdits[block] = 0;
                    var chunk = GetChunk(block);
                    if (!chunksToUpdate.Contains(chunk))
                        chunksToUpdate.Add(chunk);
                }

                // Spawn a falling group of voxels.
                _brokenChunks!.Add(new Chunk(removedBlocks, this));
            }

            foreach (var chunk in chunksToUpdate)
                chunk.UpdateMesh();
        }
    }

    public enum BlockHealth
    {
        NonDiggable = 0,
        OneHit = 1,
        Low = 50,
        Medium = 85,
        High = 140,
        Indestructible = int.MaxValue,
    }

    [Serializable]
    public class BlockType
    {
        public string name;
        public BlockHealth blockHealth;

        public bool isSolid, // Do the block generate collision?
            isTransparent; // Can you see through the block?

        public ushort topID, sideID, bottomID;

        public BlockType(string name, (ushort, ushort) topID, (ushort, ushort)? sideID = null,
            (ushort, ushort)? bottomID = null, bool isSolid = true, bool isTransparent = false,
            BlockHealth blockHealth = BlockHealth.NonDiggable)
        {
            this.name = name;
            this.isSolid = isSolid;
            this.isTransparent = isTransparent;
            this.topID = (ushort)(topID.Item1 * 16 + topID.Item2);
            this.sideID = sideID == null ? this.topID : (ushort)(sideID.Value.Item1 * 16 + sideID.Value.Item2);
            this.bottomID = bottomID == null
                ? this.topID
                : (ushort)(bottomID.Value.Item1 * 16 + bottomID.Value.Item2);
            this.blockHealth = blockHealth;
            TextureIDs = new Dictionary<int, ushort>
            {
                { 0, this.sideID },
                { 1, this.sideID },
                { 2, this.topID },
                { 3, this.bottomID },
                { 4, this.sideID },
                { 5, this.sideID },
            };
        }

        // Convert the face index to the corresponding texture ID
        // The face index order is given by VoxelData.FaceChecks
        public Dictionary<int, ushort> TextureIDs;
    }
}