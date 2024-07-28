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
        public static WorldManager instance;

        [NonSerialized] public readonly BlockType[] blockTypes =
        {
            new("air", topID: (15, 15), isSolid: false, isTransparent: true),
            new("iron", topID: (1, 0), blockHealth: BlockHealth.Indestructible), // Bedrock must be in index 1

            new("dirt", topID: (0, 0), blockHealth: BlockHealth.Medium),
            new("grass", topID: (0, 1), bottomID: (0, 0), sideID: (0, 2), blockHealth: BlockHealth.Medium),
            new("grass_dry", topID: (0, 3), bottomID: (0, 0), sideID: (0, 4), blockHealth: BlockHealth.Medium),
            new("snow", topID: (0, 5), bottomID: (0, 0), sideID: (0, 6), blockHealth: BlockHealth.Medium),

            new("iron_dark", topID: (1, 1), blockHealth: BlockHealth.Indestructible),
            new("iron_white", topID: (1, 2), blockHealth: BlockHealth.Medium),
            new("iron_red", topID: (1, 3), blockHealth: BlockHealth.Medium),

            new("bush", topID: (4, 8), blockHealth: BlockHealth.Low),
            new("foliage", topID: (1, 9), blockHealth: BlockHealth.Low),
            new("foliage_orange", topID: (1, 10), blockHealth: BlockHealth.Low),
            new("foliage_white", topID: (1, 11), blockHealth: BlockHealth.Low),
            new("foliage_green", topID: (1, 12), blockHealth: BlockHealth.Low),

            new("log", topID: (2, 1), sideID: (2, 0), blockHealth: BlockHealth.Low),
            new("log_dark", topID: (2, 3), sideID: (2, 2), blockHealth: BlockHealth.Low),

            new("plank", topID: (2, 4), blockHealth: BlockHealth.Medium),
            new("plank_dark", topID: (2, 5), blockHealth: BlockHealth.Medium),
            new("plank_smooth", topID: (2, 6), blockHealth: BlockHealth.Medium),
            new("plank_smooth_dark", topID: (2, 7), blockHealth: BlockHealth.Medium),
            new("plank_alt", topID: (2, 8), blockHealth: BlockHealth.Medium),
            new("plank_alt_dark", topID: (2, 9), blockHealth: BlockHealth.Medium),
            new("plank_log", topID: (2, 11), sideID: (2, 10), blockHealth: BlockHealth.Medium),

            new("cobblestone", topID: (0, 10), blockHealth: BlockHealth.High),
            new("stone", topID: (1, 8), blockHealth: BlockHealth.High),
            new("sand", topID: (0, 7), blockHealth: BlockHealth.Low),
            new("sand_stone", topID: (0, 8), blockHealth: BlockHealth.Medium),
            new("ardesia", topID: (4, 7), blockHealth: BlockHealth.Medium),
            new("clay", topID: (0, 9), blockHealth: BlockHealth.Medium),
            new("red_stone", topID: (0, 11), blockHealth: BlockHealth.Medium),
            new("red_stone_dark", topID: (0, 12), blockHealth: BlockHealth.Medium),
            new("grey_stone", topID: (0, 13), blockHealth: BlockHealth.Medium),
            new("slab", topID: (4, 4), blockHealth: BlockHealth.High),
            new("stone_refined  ", topID: (4, 5), sideID: (4, 6), blockHealth: BlockHealth.High),
            new("plates", topID: (1, 4), blockHealth: BlockHealth.High),
            new("cusp", topID: (1, 5), blockHealth: BlockHealth.High),
            new("bunker", topID: (1, 6), blockHealth: BlockHealth.High),
            new("warning", topID: (1, 7), blockHealth: BlockHealth.Medium),

            new("marble_white", topID: (4, 1), sideID: (4, 0), blockHealth: BlockHealth.Medium),
            new("marble_yellow", topID: (4, 3), sideID: (4, 2), blockHealth: BlockHealth.Medium),

            new("brick_grey", topID: (5, 1), sideID: (5, 0), blockHealth: BlockHealth.Medium),
            new("brick_white", topID: (5, 3), sideID: (5, 2), blockHealth: BlockHealth.Medium),
            new("brick_red", topID: (5, 5), sideID: (5, 4), blockHealth: BlockHealth.Medium),
            new("brick_green", topID: (5, 7), sideID: (5, 6), blockHealth: BlockHealth.Medium),
            new("brick_blue", topID: (5, 9), sideID: (5, 8), blockHealth: BlockHealth.Medium),
            new("brick_yellow", topID: (5, 11), sideID: (5, 10), blockHealth: BlockHealth.Medium),
            new("brick_orange", topID: (5, 13), sideID: (5, 12), blockHealth: BlockHealth.Medium),
            new("brick_black", topID: (5, 15), sideID: (5, 14), blockHealth: BlockHealth.Medium),

            new("brick_alt_grey", topID: (4, 9), blockHealth: BlockHealth.Medium),
            new("brick_alt_red", topID: (4, 10), blockHealth: BlockHealth.Medium),
            new("brick_alt_green", topID: (4, 11), blockHealth: BlockHealth.Medium),
            new("brick_alt_blue", topID: (4, 12), blockHealth: BlockHealth.Medium),
            new("brick_alt_yellow", topID: (4, 13), blockHealth: BlockHealth.Medium),
            new("brick_alt_white", topID: (4, 14), blockHealth: BlockHealth.Medium),
            new("brick_alt_black", topID: (4, 15), blockHealth: BlockHealth.Medium),

            new("red", topID: (6, 0), blockHealth: BlockHealth.Low),
            new("magenta", topID: (6, 1), blockHealth: BlockHealth.Low),
            new("purple", topID: (6, 2), blockHealth: BlockHealth.Low),
            new("blue", topID: (6, 3), blockHealth: BlockHealth.Low),
            new("cyan", topID: (6, 4), blockHealth: BlockHealth.Low),
            new("green", topID: (6, 5), blockHealth: BlockHealth.Low),
            new("lime", topID: (6, 6), blockHealth: BlockHealth.Low),
            new("yellow", topID: (6, 7), blockHealth: BlockHealth.Low),
            new("ochre", topID: (6, 8), blockHealth: BlockHealth.Low),
            new("orange", topID: (6, 9), blockHealth: BlockHealth.Low),
            new("brown", topID: (6, 10), blockHealth: BlockHealth.Low),
            new("white", topID: (6, 11), blockHealth: BlockHealth.Low),
            new("light_grey", topID: (6, 12), blockHealth: BlockHealth.Low),
            new("grey", topID: (6, 13), blockHealth: BlockHealth.Low),
            new("dark_grey", topID: (6, 14), blockHealth: BlockHealth.Low),
            new("black", topID: (6, 15), blockHealth: BlockHealth.Low),

            new("red_alt", topID: (7, 5), blockHealth: BlockHealth.Low),
            new("green_alt", topID: (7, 6), blockHealth: BlockHealth.Low),
            new("blue_alt", topID: (7, 7), blockHealth: BlockHealth.Low),
            new("yellow_alt", topID: (7, 8), blockHealth: BlockHealth.Low),
            new("orange_alt", topID: (7, 9), blockHealth: BlockHealth.Low),
            new("white_alt", topID: (7, 10), blockHealth: BlockHealth.Low),
            new("grey_alt", topID: (7, 11), blockHealth: BlockHealth.Low),

            new("steel_red", topID: (3, 10), blockHealth: BlockHealth.Medium),
            new("steel_green", topID: (3, 11), blockHealth: BlockHealth.Medium),
            new("steel_blue", topID: (3, 12), blockHealth: BlockHealth.Medium),
            new("steel_yellow", topID: (3, 13), blockHealth: BlockHealth.Medium),
            new("steel_white", topID: (3, 14), blockHealth: BlockHealth.Medium),
            new("steel_black", topID: (3, 15), blockHealth: BlockHealth.Medium),

            new("barrel", topID: (3, 1), sideID: (3, 0), blockHealth: BlockHealth.Medium),
            new("barrel_blue", topID: (3, 3), sideID: (3, 2), blockHealth: BlockHealth.Medium),
            new("barrel_green", topID: (3, 5), sideID: (3, 4), blockHealth: BlockHealth.Medium),
            new("barrel_yellow", topID: (3, 7), sideID: (3, 6), blockHealth: BlockHealth.Medium),
            new("barrel_white", topID: (3, 9), sideID: (3, 8), blockHealth: BlockHealth.Medium),

            new("water", topID: (0, 15), isSolid: false, isTransparent: true, blockHealth: BlockHealth.NonDiggable),
            new("water_shallow", topID: (0, 14), isSolid: false, isTransparent: true,
                blockHealth: BlockHealth.NonDiggable),

            new("player_block_red", topID: (12, 1), sideID: (12, 0), blockHealth: BlockHealth.Medium),
            new("player_block_blue", topID: (12, 3), sideID: (12, 2), blockHealth: BlockHealth.Medium),
            new("player_block_green", topID: (12, 5), sideID: (12, 4), blockHealth: BlockHealth.Medium),
            new("player_block_yellow", topID: (12, 7), sideID: (12, 6), blockHealth: BlockHealth.Medium),
            new("player_block_armored_red", topID: (12, 9), sideID: (12, 8), blockHealth: BlockHealth.High),
            new("player_block_armored_blue", topID: (12, 11), sideID: (12, 10), blockHealth: BlockHealth.High),
            new("player_block_armored_green", topID: (12, 13), sideID: (12, 12), blockHealth: BlockHealth.High),
            new("player_block_armored_yellow", topID: (12, 15), sideID: (12, 14), blockHealth: BlockHealth.High),

            new("hay", topID: (9, 11), sideID: (9, 10), blockHealth: BlockHealth.Medium),
            new("cactus", topID: (10, 1), sideID: (10, 0), blockHealth: BlockHealth.Medium),
            new("tnt", topID: (14, 15), sideID: (14, 14), bottomID: (10, 2), blockHealth: BlockHealth.OneHit),
            new("window", topID: (1, 13), blockHealth: BlockHealth.OneHit),
            new("crate", topID: (2, 13), blockHealth: BlockHealth.Low),
            new("crate_alt", topID: (2, 15), sideID: (2, 14), blockHealth: BlockHealth.Low),
            new("flame_box", topID: (2, 12), blockHealth: BlockHealth.OneHit),
        };

        public byte BlockTypeIndex(string blockName) =>
            (byte)blockTypes.ToList().FindIndex(it => it.name == blockName.ToLower());

        public Material material, transparentMaterial;
        [Range(1, 128)] public int chunkSize = 2;
        [Range(1, 512)] public int viewDistance = 2;
        public int atlasCount = 16;
        public float AtlasBlockSize => 1f / atlasCount;
        private Chunk[,] _chunks;
        [ItemCanBeNull] private Chunk[,] _nonSolidChunks;
        [CanBeNull] private List<Chunk> _brokenChunks = new();
        [NonSerialized] public Map map;
        private Vector3 _playerLastPos;
        [SerializeField] private string mapName = "Harbor";

        private void Start()
        {
            instance = this;
            chunkSize = math.max(1, chunkSize);
            viewDistance = math.max(1, viewDistance);
            LoadMap(Map.GetMap(mapName));
        }

        private void LoadMap(Map newMap)
        {
            map = newMap;
            var mapSize = map.size;
            var chunksX = Mathf.CeilToInt((float)mapSize.x / chunkSize);
            var chunksZ = Mathf.CeilToInt((float)mapSize.z / chunkSize);
            _chunks = new Chunk[chunksX, chunksZ];
            _nonSolidChunks = new Chunk[chunksX, chunksZ];
            for (var x = 0; x < chunksX; x++)
            for (var z = 0; z < chunksZ; z++)
            {
                _chunks[x, z] = new Chunk(new ChunkCoord(x, z), isSolid: true);

                _nonSolidChunks[x, z] = new Chunk(new ChunkCoord(x, z), isSolid: false);
                if (_nonSolidChunks[x, z].IsEmpty)
                {
                    Destroy(_nonSolidChunks[x, z].chunkGo);
                    _nonSolidChunks[x, z] = null;
                }
            }
        }

        public bool IsVoxelInWorld(Vector3Int pos) =>
            pos.x >= 0 && pos.x < map.size.x && pos.y >= 0 && pos.y < map.size.y && pos.z >= 0 && pos.z < map.size.z;

        [CanBeNull]
        public BlockType GetVoxel(Vector3Int pos) =>
            IsVoxelInWorld(pos) ? blockTypes[map.blocks[pos.y, pos.x, pos.z]] : null;

        // This is used to update the rendered chunks
        public void UpdatePlayerPos(Vector3 playerPos)
        {
            if (Vector3.Distance(_playerLastPos, playerPos) < chunkSize * .9)
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

        public void EditVoxel(Vector3 pos, byte newID)
        {
            var posNorm = Vector3Int.FloorToInt(pos);
            map.blocks[posNorm.y, posNorm.x, posNorm.z] = newID;
            if (newID == 0)
                CheckForFlyingMesh(posNorm);
            GetChunk(posNorm)?.Apply(e =>
            {
                e.UpdateMesh();
                e.UpdateAdjacentChunks(posNorm);
            });
        }

        public bool DamageVoxel(Vector3 pos, uint damage)
        {
            var posNorm = Vector3Int.FloorToInt(pos);
            if (map.DamageBlock(Vector3Int.FloorToInt(pos), damage) <= 0)
            {
                EditVoxel(posNorm, 0);
                return true;
            }

            return false;
        }

        [CanBeNull]
        private List<Vector3Int> GetAdjacentSolids(Vector3Int posNorm, List<Vector3Int> visited)
        {
            var totalAdjacentSolids = new List<Vector3Int> { posNorm };
            visited.Add(posNorm);
            foreach (var adjacent in VoxelData.AdjacentVoxelsToCheck)
            {
                var newPos = posNorm + adjacent;
                if (visited.Contains(newPos))
                    continue;
                if (newPos.y < 1)
                    return null;
                var adjacentVoxel = GetVoxel(newPos);
                if (adjacentVoxel is { isSolid: true })
                {
                    var adjacentSolids = GetAdjacentSolids(newPos, visited);
                    if (adjacentSolids == null)
                        return null;
                    totalAdjacentSolids.AddRange(adjacentSolids);
                    if (totalAdjacentSolids.Count > 500)
                        return null;
                }
            }

            return totalAdjacentSolids;
        }

        private bool CheckForFlyingMesh(Vector3Int posNorm)
        {
            var chunksToUpdate = new List<Chunk>();
            foreach (var adjacent in VoxelData.AdjacentVoxelsToCheck)
            {
                var flyingBlocks = GetAdjacentSolids(posNorm + adjacent, new List<Vector3Int>());
                if (flyingBlocks == null)
                    continue;
                var removedBlocks = new Dictionary<Vector3Int, byte>();
                foreach (var block in flyingBlocks)
                {
                    removedBlocks[new Vector3Int(block.x, block.y, block.z)] = map.blocks[block.y, block.x, block.z];
                    map.blocks[block.y, block.x, block.z] = 0;
                    var chunk = GetChunk(block);
                    if (!chunksToUpdate.Contains(chunk))
                        chunksToUpdate.Add(chunk);
                }

                _brokenChunks.Add(new Chunk(removedBlocks));
            }

            foreach (var chunk in chunksToUpdate)
                chunk.UpdateMesh();
            return true;
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
            textureIDs = new Dictionary<int, ushort>
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
        public Dictionary<int, ushort> textureIDs;
    }
}