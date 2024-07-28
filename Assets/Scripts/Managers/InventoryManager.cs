using System.Linq;
using JetBrains.Annotations;
using Model;
using VoxelEngine;

namespace Managers
{
    public class InventoryManager
    {
        public Team team = Team.Yellow;
        public uint hp, armor, blocks;
        public bool hasHelmet;

        [CanBeNull] public Weapon block, // Block
            melee, // Melee
            primary, // Rifles
            secondary, // Pistols
            tertiary; // Misc

        public byte blockId;
        public BlockType BlockType => WorldManager.instance.blockTypes[blockId];

        private static InventoryManager _instance;
        public static InventoryManager Instance => _instance ??= new InventoryManager();

        private InventoryManager()
        {
            hp = 100;
            armor = 0;
            hasHelmet = false;
            melee = Weapon.Melees[0];
            block = Weapon.Blocks[0];
            primary = Weapon.Primaries[0];
            blocks = 100;
            blockId = (byte)WorldManager.instance.blockTypes.ToList().FindIndex(e => e.name == "player_block_yellow");
        }
    }
}