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
        [CanBeNull] public Weapon block, // Block
            melee, // Melee
            primary, // Rifles
            secondary, // Pistols
            tertiary; // Misc

        public byte blockType;

        private static InventoryManager _instance;
        public static InventoryManager Instance => _instance ??= new InventoryManager();

        private InventoryManager()
        {
            health = 100;
            armor = 0;
            hasHelmet = false;
            melee = Weapon.Melees[0];
            block = Weapon.Blocks[0];
            blocks = 100;
            blockType = (byte)WorldManager.instance.blockTypes.ToList().FindIndex(e => e.name == "player_block_yellow");
        }
    }
}