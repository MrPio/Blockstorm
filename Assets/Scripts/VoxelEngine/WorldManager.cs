using System;
using ExtensionFunctions;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace VoxelEngine
{
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager instance;

        [NonSerialized] public readonly BlockType[] blockTypes =
        {
            new("air", topID: (15, 15), isSolid: false),
            new("iron", topID: (1, 0)), // Bedrock must be in index 1

            new("dirt", topID: (0, 0)),
            new("grass", topID: (0, 1), bottomID: (0, 0), sideID: (0, 2)),
            new("grass_dry", topID: (0, 3), bottomID: (0, 0), sideID: (0, 4)),
            new("snow", topID: (0, 5), bottomID: (0, 0), sideID: (0, 6)),

            new("iron_dark", topID: (1, 1)),
            new("iron_white", topID: (1, 2)),
            new("iron_red", topID: (1, 3)),

            new("bush", topID: (4, 8)),
            new("foliage", topID: (1, 9)),
            new("foliage_orange", topID: (1, 10)),
            new("foliage_white", topID: (1, 11)),
            new("foliage_green", topID: (1, 12)),

            new("log", topID: (2, 1), sideID: (2, 0)),
            new("log_dark", topID: (2, 3), sideID: (2, 2)),

            new("plank", topID: (2, 4)),
            new("plank_dark", topID: (2, 5)),
            new("plank_smooth", topID: (2, 6)),
            new("plank_smooth_dark", topID: (2, 7)),
            new("plank_alt", topID: (2, 8)),
            new("plank_alt_dark", topID: (2, 9)),
            new("plank_log", topID: (2, 11), sideID: (2, 10)),

            new("cobblestone", topID: (0, 10)),
            new("stone", topID: (1, 8)),
            new("sand", topID: (0, 7)),
            new("sand_stone", topID: (0, 8)),
            new("ardesia", topID: (4, 7)),
            new("clay", topID: (0, 9)),
            new("red_stone", topID: (0, 11)),
            new("red_stone_dark", topID: (0, 12)),
            new("grey_stone", topID: (0, 13)),
            new("slab", topID: (4, 4)),
            new("stone_refined  ", topID: (4, 5), sideID: (4, 6)),
            new("plates", topID: (1, 4)),
            new("cusp", topID: (1, 5)),
            new("bunker", topID: (1, 6)),
            new("warning", topID: (1, 7)),

            new("marble_white", topID: (4, 1), sideID: (4, 0)),
            new("marble_yellow", topID: (4, 3), sideID: (4, 2)),

            new("brick_grey", topID: (5, 1), sideID: (5, 0)),
            new("brick_white", topID: (5, 3), sideID: (5, 2)),
            new("brick_red", topID: (5, 5), sideID: (5, 4)),
            new("brick_green", topID: (5, 7), sideID: (5, 6)),
            new("brick_blue", topID: (5, 9), sideID: (5, 8)),
            new("brick_yellow", topID: (5, 11), sideID: (5, 10)),
            new("brick_orange", topID: (5, 13), sideID: (5, 12)),
            new("brick_black", topID: (5, 15), sideID: (5, 14)),

            new("brick_alt_grey", topID: (4, 9)),
            new("brick_alt_red", topID: (4, 10)),
            new("brick_alt_green", topID: (4, 11)),
            new("brick_alt_blue", topID: (4, 12)),
            new("brick_alt_yellow", topID: (4, 13)),
            new("brick_alt_white", topID: (4, 14)),
            new("brick_alt_black", topID: (4, 15)),

            new("red", topID: (6, 0)),
            new("magenta", topID: (6, 1)),
            new("purple", topID: (6, 2)),
            new("blue", topID: (6, 3)),
            new("cyan", topID: (6, 4)),
            new("green", topID: (6, 5)),
            new("lime", topID: (6, 6)),
            new("yellow", topID: (6, 7)),
            new("ochre", topID: (6, 8)),
            new("orange", topID: (6, 9)),
            new("brown", topID: (6, 10)),
            new("white", topID: (6, 11)),
            new("light_grey", topID: (6, 12)),
            new("grey", topID: (6, 13)),
            new("dark_grey", topID: (6, 14)),
            new("black", topID: (6, 15)),

            new("steel_red", topID: (3, 10)),
            new("steel_green", topID: (3, 11)),
            new("steel_blue", topID: (3, 12)),
            new("steel_yellow", topID: (3, 13)),
            new("steel_white", topID: (3, 14)),
            new("steel_black", topID: (3, 15)),


            new("barrel", topID: (3, 1), sideID: (3, 0)),
            new("barrel_blue", topID: (3, 3), sideID: (3, 2)),
            new("barrel_green", topID: (3, 5), sideID: (3, 4)),
            new("barrel_yellow", topID: (3, 7), sideID: (3, 6)),
            new("barrel_white", topID: (3, 9), sideID: (3, 8)),

            new("water", topID: (0, 15), isSolid: false), // TODO: use specular material + remove collider
            new("water_shallow", topID: (0, 14), isSolid: false),

            new("player_block_red", topID: (12, 1), sideID: (12, 0)),
            new("player_block_blue", topID: (12, 3), sideID: (12, 2)),
            new("player_block_green", topID: (12, 5), sideID: (12, 4)),
            new("player_block_yellow", topID: (12, 7), sideID: (12, 6)),
            new("player_block_armored_red", topID: (12, 9), sideID: (12, 8)),
            new("player_block_armored_blue", topID: (12, 11), sideID: (12, 10)),
            new("player_block_armored_green", topID: (12, 13), sideID: (12, 12)),
            new("player_block_armored_yellow", topID: (12, 15), sideID: (12, 14)),

            new("hay", topID: (9, 11), sideID: (9, 10)),
            new("cactus", topID: (10, 1), sideID: (10, 0)),
            new("tnt", topID: (14, 15), sideID: (14, 14), bottomID: (10, 2)),
            new("window", topID: (1, 13)),
            new("crate", topID: (2, 13)),
            new("crate_alt", topID: (2, 15), sideID: (2, 14)),
            new("flame_box", topID: (2, 12)),
        };

        public Material material;
        [Range(1, 128)] public int chunkSize = 2;
        [Range(1, 512)] public int viewDistance = 2;
        public int atlasCount = 16;
        public float AtlasBlockSize => 1f / atlasCount;
        private Chunk[,] _chunks;
        [NonSerialized] public Map map;
        private Vector3 _playerLastPos;

        private void Start()
        {
            instance = this;
            chunkSize = math.max(1, chunkSize);
            viewDistance = math.max(1, viewDistance);
            LoadMap(Map.GetMap("Harbor"));
        }

        private void LoadMap(Map newMap)
        {
            map = newMap;
            var mapSize = map.size;
            var chunksX = Mathf.CeilToInt((float)mapSize.x / chunkSize);
            var chunksZ = Mathf.CeilToInt((float)mapSize.y / chunkSize);
            _chunks = new Chunk[chunksX, chunksZ];
            for (var x = 0; x < chunksX; x++)
            for (var z = 0; z < chunksZ; z++)
                _chunks[x, z] = new Chunk(new ChunkCoord(x, z));
        }

        public bool IsVoxelInWorld(Vector3Int pos) =>
            pos.x >= 0 && pos.x < map.size.x && pos.y >= 0 && pos.y < map.size.y && pos.z >= 0 && pos.z < map.size.z;

        [CanBeNull]
        public BlockType GetVoxel(Vector3Int pos) =>
            IsVoxelInWorld(pos) ? blockTypes[map.blocks[pos.y, pos.x, pos.z]] : null;

        public void UpdatePlayerPos(Vector3 playerPos)
        {
            if (Vector3.Distance(_playerLastPos, playerPos) < chunkSize * .5)
                return;
            _playerLastPos = playerPos;
            for (var x = 0; x < _chunks.GetLength(0); x++)
            for (var z = 0; z < _chunks.GetLength(1); z++)
                _chunks[x, z].IsActive = math.abs(x*chunkSize - playerPos.x) < viewDistance &&
                                         math.abs(z*chunkSize - playerPos.z) < viewDistance;
        }

        [CanBeNull]
        public Chunk GetChunk(Vector3Int posNorm)
        {
            posNorm /= chunkSize;
            if (posNorm.x < _chunks.GetLength(0) && posNorm.z<_chunks.GetLength(1))
                return _chunks[posNorm.x, posNorm.z];
            return null;
        }
        
        public void EditVoxel(Vector3 pos, byte newID)
        { 
            var posNorm = Vector3Int.FloorToInt(pos);
            print(posNorm);
            print(map.blocks[posNorm.x, posNorm.y, posNorm.z]);
            map.blocks[posNorm.y, posNorm.x, posNorm.z] = newID;
            GetChunk(posNorm)?.Apply(e=> {
                e.UpdateMesh();
                e.UpdateAdjacentChunks(posNorm);
            });
        }
    }

    [Serializable]
    public class BlockType
    {
        public string name;
        public bool isSolid;
        public ushort topID, sideID, bottomID;

        public BlockType(string name, (ushort, ushort) topID, (ushort, ushort)? sideID = null,
            (ushort, ushort)? bottomID = null, bool isSolid = true)
        {
            this.name = name;
            this.isSolid = isSolid;
            this.topID = (ushort)(topID.Item1 * 16 + topID.Item2);
            this.sideID = sideID == null ? this.topID : (ushort)(sideID.Value.Item1 * 16 + sideID.Value.Item2);
            this.bottomID = bottomID == null ? this.topID : (ushort)(bottomID.Value.Item1 * 16 + bottomID.Value.Item2);
        }

        // Convert the face index to the corresponding texture ID
        // The face index order is given by VoxelData.FaceChecks
        public int GetTextureID(int i) =>
            i switch
            {
                2 => topID,
                3 => bottomID,
                _ => sideID
            };
    }
}