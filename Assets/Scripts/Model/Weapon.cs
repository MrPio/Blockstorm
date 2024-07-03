using System.Collections.Generic;

namespace Model
{
    public class Weapon
    {
        public static List<Weapon> shovels = new()
        {
            new Weapon(name: "shovel", audio: "shovel", damage: 30, rof: 50, distance: 4)
        };
        
        public string name, audio;
        public uint damage, rof, distance;
        public uint? magazine, ammo, reloadTime;

        public Weapon(string name, string audio, uint damage, uint rof, uint distance, uint? magazine = null,
            uint? ammo = null, uint? reloadTime = null)
        {
            this.name = name;
            this.audio = audio;
            this.damage = damage;
            this.rof = rof;
            this.distance = distance;
            this.magazine = magazine;
            this.ammo = ammo;
            this.reloadTime = reloadTime;
        }

        public float Delay => 1f / (rof / 10f); // rof = 10 => Delay = 1 sec
    }
}