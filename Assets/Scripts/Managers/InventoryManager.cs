using System.Linq;
using JetBrains.Annotations;
using Model;
using VoxelEngine;

namespace Managers
{
    public class InventoryManager
    {
        public uint health,armor,blocks;
        public bool hasHelmet;
        [CanBeNull] public Weapon shovel, // Melee
            primary, // Rifles
            secondary, // Pistols
            tertiary; // Misc

        public byte block;

        private static InventoryManager _instance;
        public static InventoryManager Instance => _instance ??= new InventoryManager();
        public readonly float placeDelay = 0.375f;

        private InventoryManager()
        {
            health = 100;
            armor = 0;
            hasHelmet = false;
            shovel = Weapon.shovels[0];
            blocks = 100;
            block = (byte)WorldManager.instance.blockTypes.ToList().FindIndex(e => e.name == "player_block_yellow");
        }
    }
}