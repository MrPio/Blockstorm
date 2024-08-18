using System;
using System.Collections.Generic;
using System.Linq;
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
        Grenade,
        GrenadeSecondary
    }

    public class Weapon
    {
        private const float PlaceDelay = 0.375f;

        public static readonly Dictionary<string, float> BodyPartMultipliers = new()
        {
            ["Head"] = 2f,
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
            // AK47 =========================================================================================
            // 2_000
            new Weapon(name: "ak47", damage: 20, rof: 100, distance: 80, type: WeaponType.Primary,
                fireAnimation: "gun", zoom: 1.65f, ammo: 120, magazine: 20, reloadTime: 250),

            // 2_400
            new Weapon(name: "ak47", damage: 24, rof: 100, distance: 80, type: WeaponType.Primary,
                fireAnimation: "gun", zoom: 1.75f, ammo: 120, magazine: 24, reloadTime: 300,
                variant: "DESERT_STORM"),

            // 3_000
            new Weapon(name: "ak47", damage: 20, rof: 150, distance: 80, type: WeaponType.Primary,
                fireAnimation: "gun", zoom: 2f, ammo: 120, magazine: 24, reloadTime: 250, variant: "NY22",
                scope: "scope0"),

            // 3_360
            new Weapon(name: "ak47", damage: 28, rof: 120, distance: 90, type: WeaponType.Primary,
                fireAnimation: "gun", zoom: 3.35f, ammo: 160, magazine: 32, reloadTime: 300, variant: "SNOW",
                scope: "scope3"),

            // 3_000
            new Weapon(name: "ak47", damage: 50, rof: 60, distance: 100, type: WeaponType.Primary,
                fireAnimation: "gun", zoom: 4f, ammo: 120, magazine: 24, reloadTime: 350, variant: "SURVIVAL",
                scope: "scope4"),

            // BARRETT =====================================================================================
            // 300
            new Weapon(name: "barrett", damage: 140, rof: 3, distance: 200, type: WeaponType.Primary,
                fireAnimation: "gun", zoom: 5f, ammo: 15, magazine: 5, reloadTime: 600,
                scope: "scope5"),

            // 375
            new Weapon(name: "barrett", damage: 200, rof: 3, distance: 150, type: WeaponType.Primary,
                fireAnimation: "gun", zoom: 5f, ammo: 15, magazine: 5, reloadTime: 600, variant: "DESERT_STORM",
                scope: "scope5", audio: "BARRETT"),
        };

        public static readonly List<Weapon> Secondaries = new()
        {
            // 1_104
            new Weapon(name: "m1911", damage: 24, rof: 46, distance: 50, type: WeaponType.Secondary,
                fireAnimation: "gun", zoom: 1.4f, ammo: 56, magazine: 9, reloadTime: 140),

            // 1_472
            new Weapon(name: "m1911", damage: 32, rof: 46, distance: 50, type: WeaponType.Secondary,
                fireAnimation: "gun", zoom: 1.55f, ammo: 56, magazine: 11, reloadTime: 160, variant: "MILITARY"),

            // 1_600
            new Weapon(name: "m1911", damage: 40, rof: 40, distance: 50, type: WeaponType.Secondary,
                fireAnimation: "gun", zoom: 1.55f, ammo: 77, magazine: 11, reloadTime: 200, variant: "ICE"),

            // 2_250
            new Weapon(name: "m1911", damage: 75, rof: 30, distance: 50, type: WeaponType.Secondary,
                fireAnimation: "gun", zoom: 1.55f, ammo: 60, magazine: 5, reloadTime: 160, variant: "GOLD"),
        };

        public static readonly List<Weapon> Tertiaries = new()
        {
            new Weapon(name: "shmel", damage: 240, rof: 10 * 2, zoom: 3f, explosionRange: 1.85f,
                type: WeaponType.Tertiary, ammo: 0,
                reloadTime: 200, magazine: 1, scope: "scope7", fireAnimation: "gun"),
        };

        public static readonly List<Weapon> Grenades = new()
        {
            new Weapon(name: "M61", damage: 100, explosionRange: 1.6f, explosionTime: 3.25f, type: WeaponType.Grenade),
            new Weapon(name: "M61_NY", damage: 150, explosionRange: 1.4f, explosionTime: 2.25f,
                type: WeaponType.Grenade),
        };

        public static readonly List<Weapon> GrenadesSecondary = new()
        {
            new Weapon(name: "smoke", damage: 0, explosionTime: 3f, fogDuration: 22f,
                type: WeaponType.GrenadeSecondary),
            new Weapon(name: "gas", damage: 25, explosionRange: 1.4f, explosionTime: 2.75f, fogDuration: 20f, rof: 10,
                type: WeaponType.GrenadeSecondary),
        };


        public string Name, Audio, FireAnimation;
        public uint Damage, Rof, Distance;
        public ushort? Magazine, Ammo, ReloadTime;
        public float? ExplosionRange, ExplosionTime, FogDuration;
        public float Zoom; // ex: 1.5x
        public WeaponType Type;
        [CanBeNull] public string Scope;
        [CanBeNull] public string Variant;

        public Weapon(string name, uint damage, uint rof = 0, uint distance = 0, [CanBeNull] string audio = null,
            [CanBeNull] string fireAnimation = null,
            ushort? magazine = null, ushort? ammo = null, ushort? reloadTime = null,
            WeaponType type = WeaponType.Primary,
            float zoom = 1.0f, float explosionRange = 0, float explosionTime = 0, float fogDuration = 0,
            string scope = null,
            string variant = null)
        {
            Name = name;
            Audio = audio ?? (variant is null ? name : $"{name}_{variant}");
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
            FogDuration = fogDuration;
            Scope = scope;
            Variant = variant;
        }

        public float Delay => 1f / (Rof / 10f); // rof = 10 => Delay = 1 sec
        public bool IsGun => Type is WeaponType.Primary or WeaponType.Secondary or WeaponType.Tertiary;

        public string GetPrefab(bool aiming = false, bool enemy = false, bool collectable = false) =>
            $"Prefabs/weapons/{(aiming ? "aim/" : "")}{(enemy ? "enemy/" : "")}{(collectable ? "collectable/" : "")}{Name.ToUpper()}";

        public string GetAudioClip => $"Audio/weapons/{Audio.ToUpper()}";
        public string GetScope => $"Textures/scope/{Scope}";

        public bool HasScope => Scope is not null && int.Parse(Scope.Split("scope")[1]) > 0;

        [CanBeNull]
        public string GetMaterial =>
            Variant is null ? null : $"Textures/weapons/Materials/{Name}_{Variant.ToUpper()}";

        [CanBeNull]
        public static Weapon Name2Weapon(string netName) =>
            Blocks.FirstOrDefault(it =>
                string.Equals(it.Name, netName.Split(":")[0], StringComparison.CurrentCultureIgnoreCase) &&
                (!netName.Contains(":") || (it.Variant ?? "") == netName.Split(":")[1])) ??
            Melees.FirstOrDefault(it =>
                string.Equals(it.Name, netName.Split(":")[0], StringComparison.CurrentCultureIgnoreCase) &&
                (!netName.Contains(":") || (it.Variant ?? "") == netName.Split(":")[1])) ??
            Primaries.FirstOrDefault(it =>
                string.Equals(it.Name, netName.Split(":")[0], StringComparison.CurrentCultureIgnoreCase) &&
                (!netName.Contains(":") || (it.Variant ?? "") == netName.Split(":")[1])) ??
            Secondaries.FirstOrDefault(it =>
                string.Equals(it.Name, netName.Split(":")[0], StringComparison.CurrentCultureIgnoreCase) &&
                (!netName.Contains(":") || (it.Variant ?? "") == netName.Split(":")[1])) ??
            Tertiaries.FirstOrDefault(it =>
                string.Equals(it.Name, netName.Split(":")[0], StringComparison.CurrentCultureIgnoreCase) &&
                (!netName.Contains(":") || (it.Variant ?? "") == netName.Split(":")[1])) ??
            Grenades.FirstOrDefault(it =>
                string.Equals(it.Name, netName.Split(":")[0], StringComparison.CurrentCultureIgnoreCase) &&
                (!netName.Contains(":") || (it.Variant ?? "") == netName.Split(":")[1])) ??
            GrenadesSecondary.FirstOrDefault(it =>
                string.Equals(it.Name, netName.Split(":")[0], StringComparison.CurrentCultureIgnoreCase) &&
                (!netName.Contains(":") || (it.Variant ?? "") == netName.Split(":")[1])) ;

        public static readonly List<Weapon> Weapons = Blocks.Concat(Melees).Concat(Primaries).Concat(Secondaries)
            .Concat(Tertiaries).Concat(Grenades).Concat(GrenadesSecondary).ToList();

        public string GetNetName => $"{Name}:{Variant ?? ""}";
    }
}