using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using JetBrains.Annotations;
using Managers.Encoder;
using Managers.Serializer;
using Model;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using Random = UnityEngine.Random;

namespace VoxelEngine
{
    [Serializable]
    public class Map
    {
        // The list of game maps. It is assumed that each map is stored, 2 times gzip compressed, inside the Firebase storage
        public static readonly string[] AvailableMaps = { "Harbor","City" };
        public static ISerializer Serializer => JsonSerializer.Instance;
        public const short MaxHeight = 128;

        public string name;

        // We assume that we have no more than 256 types of blocks.
        [SerializeField] private List<BlockEncoding> blocksList;
        [NonSerialized] public byte[,,] Blocks; // y,x,z
        [NonSerialized] public Dictionary<Vector3Int, uint> BlocksHealth;
        [NonSerialized] public Dictionary<Vector3Int, byte> BlocksEdits;
        [SerializeField] public SerializableVector3Int size;
        [SerializeField] public List<Spawn> spawns;
        [SerializeField] public List<Prop> props;
        [SerializeField] public List<CameraSpawn> cameraSpawns;
        [SerializeField] public SerializableVector3Int scoreCubePosition;

        public Map(string name, List<BlockEncoding> blocksList, Vector3Int size)
        {
            this.name = name;
            this.blocksList = blocksList;
            this.size = size;
            DeserializeMap();
        }

        public Map DeserializeMap()
        {
            // From blocksList list to blocks array
            Blocks = new byte[size.y, size.x, size.z];
            foreach (var block in blocksList)
                Blocks[block.y, block.x, block.z] = block.type;
            BlocksHealth = new Dictionary<Vector3Int, uint>();
            BlocksEdits = new Dictionary<Vector3Int, byte>();
            return this;
        }

        public BlockType GetBlock(Vector3Int pos) => VoxelData.BlockTypes[Blocks[pos.y, pos.x, pos.z]];

        public uint DamageBlock(Vector3Int pos, uint damage)
        {
            var blockHealth = GetBlock(pos).blockHealth;
            if (blockHealth is BlockHealth.Indestructible or BlockHealth.NonDiggable)
                return uint.MaxValue;
            if (blockHealth is BlockHealth.OneHit)
                return 0;
            BlocksHealth[pos] = (uint)math.max(0,
                (BlocksHealth.ContainsKey(pos) ? BlocksHealth[pos] : (int)blockHealth) - damage);
            return BlocksHealth[pos];
        }

        public Vector3 GetRandomSpawnPoint(Team team) => spawns.Find(it => it.team == team).GetRandomSpawnPoint;

        public void Save([CanBeNull] ISerializer serializer = null)
        {
            // Encode the map
            blocksList.Clear();
            for (short x = 0; x < size.x; x++)
            for (short y = 0; y < size.y; y++)
            for (short z = 0; z < size.z; z++)
                if (Blocks[y, x, z] != 0)
                    blocksList.Add(new BlockEncoding(x, y, z, Blocks[y, x, z]));
            (serializer ?? Serializer).Serialize(this, "maps", name);
        }
    }

    [Serializable]
    public class BlockEncoding
    {
        public short x, y, z;
        public byte type;

        public BlockEncoding(short x, short y, short z, byte type)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.type = type;
        }
    }

    [Serializable]
    public class Spawn
    {
        [SerializeField] public Team team;
        [SerializeField] public List<SpawnArea> spawnLayers;

        public Spawn(Team team, List<SpawnArea> spawnLayers)
        {
            this.team = team;
            this.spawnLayers = spawnLayers;
        }

        public Vector3 GetRandomSpawnPoint => spawnLayers.RandomItem().GetRandomSpawnPoint;

        public bool IsInside(Vector3 point) => spawnLayers.Any(it =>
            point.x > it.bottomLeft.x && point.x < it.topRight.x &&
            point.z > it.bottomLeft.z && point.z < it.topRight.z &&
            point.y > spawnLayers.Select(sp => sp.y).Min() - 2f && point.y < spawnLayers.Select(sp => sp.y).Max() + 2f);
    }

    [Serializable]
    public class Prop
    {
        [SerializeField] public SerializableVector3 position;
        [SerializeField] public SerializableVector3 rotation;
        [SerializeField] public string prefabName;

        public Prop(SerializableVector3 position, SerializableVector3 rotation, string prefabName)
        {
            this.position = position;
            this.rotation = rotation;
            this.prefabName = prefabName;
        }

        public string GetPrefab => $"Prefabs/props/{prefabName}";
    }
    /**
     * A spawn area. Each spawn area consists of a rectangle on the XZ plane,
     * drawn at the given Y position.
     */
    [Serializable]
    public class SpawnArea
    {
        [SerializeField] public Vector2XZ bottomLeft, topRight;
        [SerializeField] public short y;

        public SpawnArea(Vector2XZ bottomLeft, Vector2XZ topRight, short y)
        {
            this.bottomLeft = bottomLeft;
            this.topRight = topRight;
            this.y = y;
        }

        public Vector3 GetRandomSpawnPoint =>
            new(Random.Range(bottomLeft.x, topRight.x), y, Random.Range(bottomLeft.z, topRight.z));

        public Vector3 Center => (bottomLeft.ToVector3(y) + topRight.ToVector3(y)) / 2;
    }

    /**
     * A camera spawn position.
     * This is loaded when the player has chosen the map, but is still choosing the team.
     * A nullish value for team indicates the global camera that record the map from an high position.
     */
    [Serializable]
    public class CameraSpawn
    {
        [SerializeField] public Utils.Nullable<Team> team;
        [SerializeField] public SerializableVector3 position;
        [SerializeField] public SerializableVector3 rotation;

        public CameraSpawn(SerializableVector3 position, SerializableVector3 rotation, Utils.Nullable<Team> team)
        {
            this.position = position;
            this.rotation = rotation;
            this.team = team;
        }
    }
}