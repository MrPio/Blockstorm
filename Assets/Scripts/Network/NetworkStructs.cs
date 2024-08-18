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
using Random = System.Random;

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
        public int Hp, Armor;
        public bool HasHelmet;
        public byte LeftGrenades, LeftSecondaryGrenades;
        public bool HasArmoredBlock;

        private FixedString32Bytes
            skinName, blockName, meleeName, primaryName, secondaryName, tertiaryName, grenadeName, grenadeSecondaryName;


        public PlayerStatus(int? hp = null, int? armor = null,
            bool? hasHelmet = null,
            byte? leftGrenades = null, byte? leftSecondaryGrenades = null, bool? hasArmoredBlock = null,
            FixedString32Bytes? skinName = null,
            FixedString32Bytes? blockName = null,
            FixedString32Bytes? meleeName = null,
            FixedString32Bytes? primaryName = null, FixedString32Bytes? secondaryName = null,
            FixedString32Bytes? tertiaryName = null,
            FixedString32Bytes? grenadeName = null,
            FixedString32Bytes? grenadeSecondaryName = null)
        {
            this.skinName = skinName ?? "soldier";
            this.blockName = blockName ?? "block";
            this.meleeName =
                meleeName ?? Weapon.Melees.Where(it => it.Variant is null).ToList().RandomItem().GetNetName;
            this.primaryName = primaryName ??
                               Weapon.Primaries.Where(it => it.Variant is null).ToList().RandomItem().GetNetName;
            this.secondaryName = secondaryName ??
                                 Weapon.Secondaries.Where(it => it.Variant is null).ToList().RandomItem().GetNetName;
            this.tertiaryName = tertiaryName ?? "shmel";
            this.grenadeName = grenadeName ??
                               Weapon.Grenades.Where(it => it.Variant is null).ToList().RandomItem().GetNetName;
            this.grenadeSecondaryName = grenadeSecondaryName ??
                                        Weapon.GrenadesSecondary.Where(it => it.Variant is null).ToList().RandomItem()
                                            .GetNetName;
            Hp = hp ?? 100;
            Armor = armor ?? 0;
            HasHelmet = hasHelmet ?? true;
            LeftGrenades = leftGrenades ?? 2;
            LeftSecondaryGrenades = leftSecondaryGrenades ?? (this.grenadeSecondaryName.Value.ToLower() == "smoke"
                ? (byte)2
                : (byte)1);
            HasArmoredBlock = hasArmoredBlock ?? new List<bool> { true, false }.RandomItem();
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
            get => Weapon.Name2Weapon(blockName.Value);
            set => blockName = value?.GetNetName;
        }

        [CanBeNull]
        public Weapon Melee
        {
            get => Weapon.Name2Weapon(meleeName.Value);
            set => meleeName = value?.GetNetName;
        }

        [CanBeNull]
        public Weapon Primary
        {
            get => Weapon.Name2Weapon(primaryName.Value);
            set => primaryName = value?.GetNetName;
        }

        [CanBeNull]
        public Weapon Secondary
        {
            get => Weapon.Name2Weapon(secondaryName.Value);
            set => secondaryName = value?.GetNetName;
        }

        [CanBeNull]
        public Weapon Tertiary
        {
            get => Weapon.Name2Weapon(tertiaryName.Value);
            set => tertiaryName = value?.GetNetName;
        }

        [CanBeNull]
        public Weapon Grenade
        {
            get => Weapon.Name2Weapon(grenadeName.Value);
            set => grenadeName = value?.GetNetName;
        }

        [CanBeNull]
        public Weapon GrenadeSecondary
        {
            get => Weapon.Name2Weapon(grenadeSecondaryName.Value);
            set => grenadeSecondaryName = value?.GetNetName;
        }

        public byte BlockId(Team team) =>
            VoxelData.Name2Id($"player_block_{(HasArmoredBlock ? "armored_" : "")}{team.ToString().ToLower()}");

        public BlockType BlockType(Team team) => VoxelData.BlockTypes[BlockId(team)];

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref Armor);
            serializer.SerializeValue(ref HasHelmet);
            serializer.SerializeValue(ref LeftGrenades);
            serializer.SerializeValue(ref LeftSecondaryGrenades);
            serializer.SerializeValue(ref HasArmoredBlock);
            serializer.SerializeValue(ref skinName);
            serializer.SerializeValue(ref blockName);
            serializer.SerializeValue(ref meleeName);
            serializer.SerializeValue(ref primaryName);
            serializer.SerializeValue(ref secondaryName);
            serializer.SerializeValue(ref tertiaryName);
            serializer.SerializeValue(ref grenadeName);
            serializer.SerializeValue(ref grenadeSecondaryName);
        }
    }

    /// <summary>
    /// The player info that needs to be showed in the dashboard.
    /// </summary>
    public struct PlayerStats : INetworkSerializable
    {
        public ushort Kills, Deaths;
        public FixedString32Bytes Username;


        public PlayerStats(ushort? kills = null, ushort? deaths = null,
            FixedString32Bytes? username = null)
        {
            Kills = kills ?? 0;
            Deaths = deaths ?? 0;
            Username = username ?? "Guest";
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Kills);
            serializer.SerializeValue(ref Deaths);
            serializer.SerializeValue(ref Username);
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

    public struct Scores : INetworkSerializeByMemcpy
    {
        public const ushort WinScore = 500;
        public ushort Red, Blue, Green, Yellow;

        public Team? Winner =>
            Red >= WinScore ? Team.Red :
            Blue >= WinScore ? Team.Blue :
            Green >= WinScore ? Team.Green :
            Yellow >= WinScore ? Team.Yellow : null;

        public override string ToString()
        {
            return
                $"{nameof(Red)}: {Red}, {nameof(Blue)}: {Blue}, {nameof(Green)}: {Green}, {nameof(Yellow)}: {Yellow}";
        }
    }
}