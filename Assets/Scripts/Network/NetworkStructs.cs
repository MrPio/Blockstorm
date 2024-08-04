using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using JetBrains.Annotations;
using Model;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VoxelEngine;

namespace Network
{
    /// <summary>
    /// A string message of 64 bytes in size.
    /// </summary>
    public struct NetString : INetworkSerializable
    {
        public FixedString64Bytes Message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Message);
        }

        public static implicit operator string(NetString rValue) => rValue.Message.Value;

        public static implicit operator NetString(string rValue) => new() { Message = rValue };
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

        public NetVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector3 => new(x, y, z);

        public static implicit operator Vector3(NetVector3 rValue) => new(rValue.x, rValue.y, rValue.z);

        public static implicit operator NetVector3(Vector3 rValue) => new(rValue);

        public static bool operator ==(NetVector3 lhs, NetVector3 rhs)
        {
            return Mathf.Approximately(lhs.x, rhs.x) && Mathf.Approximately(lhs.y, rhs.y) &&
                   Mathf.Approximately(lhs.z, rhs.z);
        }

        public static bool operator !=(NetVector3 lhs, NetVector3 rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString() => $"{nameof(x)}: {x}, {nameof(y)}: {y}, {nameof(z)}: {z}";

        public override bool Equals(object obj) => base.Equals(obj);

        public override int GetHashCode() => base.GetHashCode();
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
            Team = team ?? EnumExtensions.RandomItem<Team>();
            Hp = hp ?? 100;
            Armor = armor ?? 0;
            HasHelmet = hasHelmet ?? true;
            LeftGrenades = leftGrenades ?? 5;
            HasArmoredBlock = hasArmoredBlock ?? false;
            this.skinName = skinName ?? "soldier";
            this.blockName = blockName ?? "block";
            this.meleeName = meleeName ?? "shovel";
            this.primaryName = primaryName ?? "ak47:NY22";
            this.secondaryName = secondaryName ?? "m1911:ICE";
            this.tertiaryName = tertiaryName ?? "shmel";
            this.grenadeName = grenadeName ?? "M61_NY";
        }

        public bool IsDead => Hp <= 0;

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
            set => blockName = value is null ? null : $"{value.Name}:{value.Variant}";
        }

        [CanBeNull]
        public Weapon Melee
        {
            get
            {
                var name = meleeName.Value;
                return Weapon.Melees.FirstOrDefault(it => it.Name == name);
            }
            set => meleeName = value is null ? null : $"{value.Name}:{value.Variant}";
        }

        [CanBeNull]
        public Weapon Primary
        {
            get
            {
                var name = primaryName.Value;
                return Weapon.Primaries.FirstOrDefault(it =>
                    it.Name == name.Split(':')[0] && (it.Variant ?? "") == name.Split(':')[1]);
            }
            set => primaryName = value is null ? null : $"{value.Name}:{value.Variant}";
        }

        [CanBeNull]
        public Weapon Secondary
        {
            get
            {
                var name = secondaryName.Value;
                return Weapon.Secondaries.FirstOrDefault(it =>
                    it.Name == name.Split(':')[0] && (it.Variant ?? "") == name.Split(':')[1]);
            }
            set => secondaryName = value is null ? null : $"{value.Name}:{value.Variant}";
        }

        [CanBeNull]
        public Weapon Tertiary
        {
            get
            {
                var name = tertiaryName.Value;
                return Weapon.Tertiaries.FirstOrDefault(it => it.Name == name);
            }
            set => tertiaryName = value is null ? null : $"{value.Name}:{value.Variant}";
        }

        [CanBeNull]
        public Weapon Grenade
        {
            get
            {
                var name = grenadeName.Value;
                return Weapon.Grenades.FirstOrDefault(it => it.Name == name);
            }
            set => grenadeName = value is null ? null : $"{value.Name}:{value.Variant}";
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

    public struct CollectablesStatus : INetworkSerializable
    {
        public float[] Xs, Ys, Zs;
        public byte[] MedkitTypes, CollectableTypes;
        public NetString[] WeaponNames;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Xs);
            serializer.SerializeValue(ref Ys);
            serializer.SerializeValue(ref Zs);
            serializer.SerializeValue(ref WeaponNames);
            serializer.SerializeValue(ref MedkitTypes);
            serializer.SerializeValue(ref CollectableTypes);
        }

        public CollectablesStatus(List<Vector3> positions, List<Collectable> collectables)
        {
            Xs = positions.Select(it => it.x).ToArray();
            Ys = positions.Select(it => it.y).ToArray();
            Zs = positions.Select(it => it.z).ToArray();
            CollectableTypes = collectables.Select(it => (byte)it.Type).ToArray();
            MedkitTypes = collectables.Select(it => it.MedkitType is null ? (byte)0 : (byte)it.MedkitType).ToArray();
            WeaponNames = collectables.Select(it => (NetString)(it.WeaponItem?.GetNetName ?? "")).ToArray();
        }

        public List<Collectable> ToCollectables
        {
            get
            {
                List<Collectable> collectables = new();
                for (var i = 0; i < Xs.Length; i++)
                    collectables.Add(new Collectable(
                        (CollectableType)Enum.GetValues(typeof(CollectableType)).GetValue(CollectableTypes[i]),
                        new NetVector3(Xs[i], Ys[i], Zs[i]),
                        Weapon.Name2Weapon(WeaponNames[i]),
                        (Medkit)Enum.GetValues(typeof(Medkit)).GetValue(MedkitTypes[i])
                    ));
                return collectables;
            }
        }
    }
}