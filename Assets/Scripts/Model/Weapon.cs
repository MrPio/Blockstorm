using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Model
{
    [Serializable]
    public enum WeaponType
    {
        Block,
        Melee,
        Primary,
        Secondary,
        Tertiary
    }

    public class Weapon
    {
        private const float PlaceDelay = 0.375f;

        public static readonly List<Weapon> Blocks = new()
        {
            new Weapon(name: "block", damage: 0, rof: (uint)(10f / PlaceDelay), distance: 5, type: WeaponType.Block,
                fireAnimation: "shovel"),
        };

        public static readonly List<Weapon> Melees = new()
        {
            new Weapon(name: "shovel", damage: 35, rof: 42, distance: 4, type: WeaponType.Melee,
                fireAnimation: "shovel")
        };

        public static readonly List<Weapon> Primaries = new()
        {
            new Weapon(name: "ak47", damage: 24*20, rof: 100, distance: 72, type: WeaponType.Primary, fireAnimation: "gun",
                zoom: 1.75f)
        };

        public string Name, Audio, FireAnimation;
        public uint Damage, Rof, Distance;
        public uint? Magazine, Ammo, ReloadTime;
        public float Zoom; // ex: 1.5x
        public WeaponType Type;

        public Weapon(string name, uint damage, uint rof, uint distance, [CanBeNull] string audio = null,
            [CanBeNull] string fireAnimation = null,
            uint? magazine = null, uint? ammo = null, uint? reloadTime = null, WeaponType type = WeaponType.Primary,
            float zoom = 1.0f)
        {
            Name = name;
            Audio = audio ?? name;
            FireAnimation = fireAnimation ?? name;
            Damage = damage;
            Rof = rof;
            Distance = distance;
            Magazine = magazine;
            Ammo = ammo;
            ReloadTime = reloadTime;
            Type = type;
            Zoom = zoom;
        }

        public float Delay => 1f / (Rof / 10f); // rof = 10 => Delay = 1 sec
    }
}