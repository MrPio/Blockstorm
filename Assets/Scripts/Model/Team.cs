using System.Collections.Generic;
using UnityEngine;

namespace Model
{
    public enum Team
    {
        Red,
        Blue,
        Green,
        Yellow,
        None,
    }

    public static class TeamData
    {
        public static readonly Dictionary<Team, Color> Colors = new()
        {
            { Team.Red, new Color(1, 0.02f, 0) },
            { Team.Blue, new Color(0f, 0.32f, 1) },
            { Team.Green, new Color(0.01f, 0.84f, 0) },
            { Team.Yellow, new Color(1, 0.91f, 0) },
        };
    }
}