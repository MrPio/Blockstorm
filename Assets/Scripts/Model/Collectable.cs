using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using JetBrains.Annotations;
using Network;
using Utils;

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
        private static readonly Dictionary<CollectableType, float> Probabilities = new()
        {
            { CollectableType.Hp, 0.35f },
            { CollectableType.Ammo, 0.35f },
            { CollectableType.Weapon, 0.35f },
        };

        public static readonly Dictionary<Medkit, ushort> MedkitHps = new()
        {
            { Medkit.Small, 30 },
            { Medkit.Medium, 60 },
            { Medkit.Large, 100 }
        };

        public CollectableType Type;
        [CanBeNull] public Weapon WeaponItem;
        public Medkit? MedkitType;
        public NetVector3 ID;

        public Collectable(CollectableType type, NetVector3 id, Weapon weaponItem = null,
            Medkit? medkitType = null)
        {
            Type = type;
            WeaponItem = weaponItem;
            MedkitType = medkitType;
            ID = id;
        }

        public static Collectable GetRandomCollectable(NetVector3 id)
        {
            var p = new Random().NextDouble();
            var acc = 0f;
            foreach (var (key, value) in Probabilities)
            {
                acc += value;
                if (acc >= p)
                    return new Collectable(key, id,
                        key is CollectableType.Weapon
                            ? Weapon.Weapons.Where(it => it.Type is not WeaponType.Block and not WeaponType.Melee)
                                .ToList().RandomItem()
                            : null,
                        key is CollectableType.Hp
                            ? (Medkit)Enum.GetValues(typeof(Medkit))
                                .GetValue(new Random().Next(Enum.GetNames(typeof(Medkit)).Length))
                            : null);
            }

            return null;
        }
    }
}