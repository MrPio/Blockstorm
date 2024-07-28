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
            new Weapon(name: "block", damage: 0, rof: (uint)(10f / PlaceDelay), distance: 5, type: WeaponType.Block, fireAnimation:"shovel"),
        };
        public static readonly List<Weapon> Melees = new()
        {
            new Weapon(name: "shovel", damage: 35, rof: 42, distance: 4, type: WeaponType.Melee, fireAnimation:"shovel")
        };
        public static readonly List<Weapon> Primaries = new()
        {
            new Weapon(name: "ak47", damage: 24*10, rof: 100, distance: 72, type: WeaponType.Primary, fireAnimation:"gun",zoom:1.75f)
        };

        public string name, audio, fireAnimation;
        public uint damage, rof, distance;
        public uint? magazine, ammo, reloadTime;
        public float zoom; //ex: 1.5x
        public WeaponType type;

        public Weapon(string name, uint damage, uint rof, uint distance, [CanBeNull] string audio = null,
            [CanBeNull] string fireAnimation = null,
            uint? magazine = null, uint? ammo = null, uint? reloadTime = null, WeaponType type = WeaponType.Primary, float zoom=1.0f)
        {
            this.name = name;
            this.audio = audio ?? name;
            this.fireAnimation = fireAnimation ?? name;
            this.damage = damage;
            this.rof = rof;
            this.distance = distance;
            this.magazine = magazine;
            this.ammo = ammo;
            this.reloadTime = reloadTime;
            this.type = type;
            this.zoom = zoom;
        }

        public float Delay => 1f / (rof / 10f); // rof = 10 => Delay = 1 sec
    }
}