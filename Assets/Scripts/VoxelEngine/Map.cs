using System;
using System.Collections.Generic;
using ExtensionFunctions;
using JetBrains.Annotations;
using Managers.Serializer;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using Random = UnityEngine.Random;

namespace VoxelEngine
{
    public enum Team
    {
        Red,
        Blue,
        Green,
        Yellow
    }

    [Serializable]
    public class Map
    {
        public static ISerializer Serializer => JsonSerializer.Instance;
        public const short MaxHeight = 128;

        private static readonly string MapsDir = "maps/";

        public string name;

        // We assume to have no more than 256 types of blocks. Otherwise an int would be required (4x size)
        [SerializeField] private List<BlockEncoding> blocksList;
        [NonSerialized] public byte[,,] blocks; // y,x,z
        [NonSerialized] public Dictionary<Vector3Int, uint> blocksHealth;
        [SerializeField] public SerializableVector3Int size;
        [SerializeField] public List<Spawn> spawns;
        [SerializeField] public List<CameraSpawn> cameraSpawns;

        public Map(string name, List<BlockEncoding> blocksList, Vector3Int size)
        {
            this.name = name;
            this.blocksList = blocksList;
            this.size = size;
            blocksHealth = new Dictionary<Vector3Int, uint>();
        }

        private Map DeserializeMap()
        {
            // From blocksList list to blocks array
            blocks = new byte[size.y, size.x, size.z];
            foreach (var block in blocksList)
                blocks[block.y, block.x, block.z] = block.type;
            blocksHealth = new Dictionary<Vector3Int, uint>();
            return this;
        }

        public static Map GetMap(string mapName) => Serializer.Deserialize<Map>(MapsDir + mapName).DeserializeMap();

        public BlockType GetBlock(Vector3Int pos) => WorldManager.instance.blockTypes[blocks[pos.y, pos.x, pos.z]];

        public uint DamageBlock(Vector3Int pos, uint damage)
        {
            var blockHealth = GetBlock(pos).blockHealth;
            if (blockHealth is BlockHealth.Indestructible or BlockHealth.NonDiggable)
                return uint.MaxValue;
            if (blockHealth is BlockHealth.OneHit)
                return 0;
            blocksHealth[pos] = (uint)math.max(0,
                (blocksHealth.ContainsKey(pos) ? blocksHealth[pos] : (int)blockHealth) - damage);
            return blocksHealth[pos];
        }

        public Vector3 GetRandomSpawnPoint(Team team) => spawns.Find(it => it.team == team).GetRandomSpawnPoint;

        public void Save([CanBeNull] ISerializer serializer = null) =>
            (serializer ?? Serializer).Serialize(this, "maps", name);
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
        [SerializeField] public Vector3 position;
        [SerializeField] public Vector3 rotation;

        public CameraSpawn(Vector3 position, Vector3 rotation, Utils.Nullable<Team> team)
        {
            this.position = position;
            this.rotation = rotation;
            this.team = team;
        }
    }
}