using System.Collections.Generic;
using Unity.VisualScripting;
using VoxelEngine;

namespace Model
{
    public class Skin
    {
        public static readonly List<Skin> Skins = new()
        {
            new Skin(name: "Soldier")
        };

        public string Name;

        public Skin(string name)
        {
            Name = name;
        }

        public string GetSkinForTeam(Team team) => $"{Name.ToUpper()}_{team.ToString().ToUpper()}";
    }
}