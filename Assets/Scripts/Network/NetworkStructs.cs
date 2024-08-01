using System.Linq;
using JetBrains.Annotations;
using Model;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;

namespace Network
{
    /// <summary>
    /// A string message of 512 bytes in size.
    /// </summary>
    public struct NetString : INetworkSerializable
    {
        public FixedString512Bytes Message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Message);
        }
    }

    /// <summary>
    /// A list of changes to the original map.
    /// Each voxel edit has three coordinates and the id of the new block type.
    /// </summary>
    /// <remarks> This is used to synchronize the map status when a new player connects. </remarks>
    public struct MapStatus : INetworkSerializable
    {
        public short[] Xs, Ys, Zs;
        public byte[] Ids;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Xs);
            serializer.SerializeValue(ref Ys);
            serializer.SerializeValue(ref Zs);
            serializer.SerializeValue(ref Ids);
        }

        public MapStatus(Map map)
        {
            // Debug.Log($"Host is updating the MapStatus...");
            var positions = map.BlocksEdits.Keys;
            var ids = map.BlocksEdits.Values;
            Xs = positions.Select(it => (short)it.x).ToArray();
            Ys = positions.Select(it => (short)it.y).ToArray();
            Zs = positions.Select(it => (short)it.z).ToArray();
            Ids = ids.ToArray();
        }
    }

    /// <summary>
    /// A 12 byte network serializable Vector3.
    /// </summary>
    public struct NetVector3 : INetworkSerializable
    {
        private float x, y, z;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref y);
            serializer.SerializeValue(ref z);
        }

        public NetVector3(Vector3 vector3)
        {
            x = vector3.x;
            y = vector3.y;
            z = vector3.z;
        }

        public Vector3 ToVector3 => new(x, y, z);
    }

    /// <summary>
    /// The player info that must be shared with all the players.
    /// </summary>
    public struct PlayerStatus : INetworkSerializable
    {
        public Team Team;
        public int Hp, Armor;
        public bool HasHelmet; // TODO: handle helmet removal
        public byte LeftGrenades;
        public bool HasArmoredBlock;

        private FixedString32Bytes
            skinName, blockName, meleeName, primaryName, secondaryName, tertiaryName, grenadeName;


        public PlayerStatus(Team? team = null, int? hp = null, int? armor = null,
            bool? hasHelmet = null,
            byte? leftGrenades = null, bool? hasArmoredBlock = null, FixedString32Bytes? skinName = null,
            FixedString32Bytes? blockName = null,
            FixedString32Bytes? meleeName = null,
            FixedString32Bytes? primaryName = null, FixedString32Bytes? secondaryName = null,
            FixedString32Bytes? tertiaryName = null,
            FixedString32Bytes? grenadeName = null)
        {
            Team = team ?? Team.Yellow;
            Hp = hp ?? 100;
            Armor = armor ?? 0;
            HasHelmet = hasHelmet ?? false;
            LeftGrenades = leftGrenades ?? 5;
            HasArmoredBlock = hasArmoredBlock ?? false;
            this.skinName = skinName ?? "soldier";
            this.blockName = blockName ?? "block";
            this.meleeName = meleeName ?? "shovel";
            this.primaryName = primaryName ?? "ak47";
            this.secondaryName = secondaryName ?? "";
            this.tertiaryName = tertiaryName ?? "";
            this.grenadeName = grenadeName ?? "M61_NY";
        }

        public bool IsDead => Hp <= 0;
        [CanBeNull]
        public Weapon Name2Weapon(string name) => Weapon.Blocks.FirstOrDefault(it => it.Name == name) ??
                                                  Weapon.Melees.FirstOrDefault(it => it.Name == name) ??
                                                  Weapon.Primaries.FirstOrDefault(it => it.Name == name) ??
                                                  Weapon.Secondaries.FirstOrDefault(it => it.Name == name) ??
                                                  Weapon.Tertiaries.FirstOrDefault(it => it.Name == name) ??
                                                  Weapon.Grenades.FirstOrDefault(it => it.Name == name);

        public Skin Skin
        {
            get
            {
                var name = skinName.Value;
                return Skin.Skins.First(it => it.Name == name);
            }
            set => skinName = value?.Name ?? "";
        }

        [CanBeNull]
        public Weapon Block
        {
            get
            {
                var name = blockName.Value;
                return Weapon.Blocks.FirstOrDefault(it => it.Name == name);
            }
            set => blockName = value?.Name ?? "";
        }

        [CanBeNull]
        public Weapon Melee
        {
            get
            {
                var name = meleeName.Value;
                return Weapon.Melees.FirstOrDefault(it => it.Name == name);
            }
            set => meleeName = value?.Name ?? "";
        }

        [CanBeNull]
        public Weapon Primary
        {
            get
            {
                var name = primaryName.Value;
                return Weapon.Primaries.FirstOrDefault(it => it.Name == name);
            }
            set => primaryName = value?.Name ?? "";
        }

        [CanBeNull]
        public Weapon Secondary
        {
            get
            {
                var name = secondaryName.Value;
                return Weapon.Secondaries.FirstOrDefault(it => it.Name == name);
            }
            set => secondaryName = value?.Name ?? "";
        }

        [CanBeNull]
        public Weapon Tertiary
        {
            get
            {
                var name = tertiaryName.Value;
                return Weapon.Tertiaries.FirstOrDefault(it => it.Name == name);
            }
            set => tertiaryName = value?.Name ?? "";
        }

        [CanBeNull]
        public Weapon Grenade
        {
            get
            {
                var name = grenadeName.Value;
                return Weapon.Grenades.FirstOrDefault(it => it.Name == name);
            }
            set => grenadeName = value?.Name ?? "";
        }

        public byte BlockId =>
            VoxelData.Name2Id($"player_block_{(HasArmoredBlock ? "armored_" : "")}{Team.ToString().ToLower()}");

        public BlockType BlockType => VoxelData.BlockTypes[BlockId];

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Team);
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref Armor);
            serializer.SerializeValue(ref HasHelmet);
            serializer.SerializeValue(ref LeftGrenades);
            serializer.SerializeValue(ref HasArmoredBlock);
            serializer.SerializeValue(ref skinName);
            serializer.SerializeValue(ref blockName);
            serializer.SerializeValue(ref meleeName);
            serializer.SerializeValue(ref primaryName);
            serializer.SerializeValue(ref secondaryName);
            serializer.SerializeValue(ref tertiaryName);
            serializer.SerializeValue(ref grenadeName);
        }
    }
}