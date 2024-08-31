using System;
using System.Collections.Generic;
using ExtensionFunctions;
using Network;

namespace Model
{
    public enum CollectableType
    {
        Weapon,
        Ammo,
        Hp
    }

    public enum Medkit
    {
        Small,
        Medium,
        Large
    }


    public class Collectable
    {
        private static readonly Dictionary<CollectableType, float> TypeProbabilities = new()
        {
            { CollectableType.Hp, 2 },
            { CollectableType.Ammo, 2 },
            { CollectableType.Weapon, 4 },
        };
        private static readonly Dictionary<WeaponType, float> WeaponProbabilities = new()
        {
            { Model.WeaponType.Primary, 2 },
            { Model.WeaponType.Secondary, 2 },
            { Model.WeaponType.Tertiary, 1 },
        };

        public static readonly Dictionary<Medkit, ushort> MedkitHps = new()
        {
            { Medkit.Small, 30 },
            { Medkit.Medium, 60 },
            { Medkit.Large, 100 }
        };

        public CollectableType Type;
        public WeaponType? WeaponType;
        public Medkit? MedkitType;
        public NetVector3 ID;
        [NonSerialized] public Weapon WeaponItem;

        public Collectable(CollectableType type, NetVector3 id, Medkit? medkitType = null,
            WeaponType? weaponType = null)
        {
            Type = type;
            ID = id;
            MedkitType = medkitType;
            WeaponType = weaponType;
        }

        public static Collectable GetRandomCollectable(NetVector3 id)
        {
            var type = TypeProbabilities.DrawRandom();
            return new Collectable(
                type: type,
                id: id,
                medkitType: type is CollectableType.Hp ? EnumExtensions.RandomItem<Medkit>() : null,
                weaponType: WeaponProbabilities.DrawRandom()
            );
        }
    }
}