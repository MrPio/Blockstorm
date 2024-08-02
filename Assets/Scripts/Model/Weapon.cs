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
        Tertiary,
        Grenade
    }

    public class Weapon
    {
        private const float PlaceDelay = 0.375f;

        public static readonly Dictionary<string, float> BodyPartMultipliers = new()
        {
            ["Head"] = 1.5f,
            ["Body"] = 0.85f,
            ["Chest"] = 1f,

            ["Arm:Left:Upper"] = 0.85f,
            ["Arm:Left:Lower"] = 0.75f,
            ["Arm:Right:Upper"] = 0.85f,
            ["Arm:Right:Lower"] = 0.75f,

            ["Leg:Left:Upper"] = 0.65f,
            ["Leg:Left:Lower"] = 0.5f,
            ["Leg:Right:Upper"] = 0.65f,
            ["Leg:Right:Lower"] = 0.5f,
        };

        public static readonly List<Weapon> Blocks = new()
        {
            new Weapon(name: "block", damage: 0, rof: (uint)(10f / PlaceDelay), distance: 5, type: WeaponType.Block,
                fireAnimation: "shovel", magazine: 100),
        };

        public static readonly List<Weapon> Melees = new()
        {
            new Weapon(name: "shovel", damage: 35, rof: 50, distance: 4, type: WeaponType.Melee,
                fireAnimation: "shovel")
        };

        public static readonly List<Weapon> Primaries = new()
        {
            new Weapon(name: "ak47", damage: 24, rof: 100, distance: 72, type: WeaponType.Primary,
                fireAnimation: "gun", zoom: 1.75f, ammo: 120, magazine: 30, reloadTime: 250)
        };

        public static readonly List<Weapon> Secondaries = new()
        {
        };

        public static readonly List<Weapon> Tertiaries = new()
        {
            new Weapon(name: "shmel", damage: 175, zoom: 1.5f, explosionRange: 3f, type: WeaponType.Tertiary, ammo: 5,
                reloadTime: 200, magazine: 1, scope:"shmel"),
        };

        public static readonly List<Weapon> Grenades = new()
        {
            new Weapon(name: "M61", damage: 100, explosionRange: 2.5f, explosionTime: 3.25f, type: WeaponType.Grenade),
            new Weapon(name: "M61_NY", damage: 150, explosionRange: 2f, explosionTime: 2.25f, type: WeaponType.Grenade),
        };

        public string Name, Audio, FireAnimation;
        public uint Damage, Rof, Distance;
        public ushort? Magazine, Ammo, ReloadTime;
        public float? ExplosionRange, ExplosionTime;
        public float Zoom; // ex: 1.5x
        public WeaponType Type;
        [CanBeNull] public string Scope;

        public Weapon(string name, uint damage, uint rof = 0, uint distance = 0, [CanBeNull] string audio = null,
            [CanBeNull] string fireAnimation = null,
            ushort? magazine = null, ushort? ammo = null, ushort? reloadTime = null,
            WeaponType type = WeaponType.Primary,
            float zoom = 1.0f, float explosionRange = 0, float explosionTime = 0, string scope=null)
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
            ExplosionRange = explosionRange;
            ExplosionTime = explosionTime;
            Scope = scope;
        }

        public float Delay => 1f / (Rof / 10f); // rof = 10 => Delay = 1 sec
        public bool IsGun => Type is WeaponType.Primary or WeaponType.Secondary or WeaponType.Tertiary;
    }
}