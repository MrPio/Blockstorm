using System.Linq;
using JetBrains.Annotations;
using VoxelEngine;

namespace Managers
{
    public class InventoryManager
    {
        public Team Team = Team.Yellow;
        public int Hp, Armor, Blocks;
        public bool HasHelmet; // TODO: handle halmet removal

        [CanBeNull] public Model.Weapon Block, // Block
            Melee, // Melee
            Primary, // Rifles
            Secondary, // Pistols
            Tertiary; // Misc

        public byte BlockId;
        public BlockType BlockType => WorldManager.instance.blockTypes[BlockId];

        private static InventoryManager _instance;
        public static InventoryManager Instance => _instance ??= new InventoryManager();

        private InventoryManager()
        {
            Hp = 100;
            Armor = 0;
            HasHelmet = false;
            Melee = Model.Weapon.Melees[0];
            Block = Model.Weapon.Blocks[0];
            Primary = Model.Weapon.Primaries[0];
            Blocks = 100;
            BlockId = (byte)WorldManager.instance.blockTypes.ToList().FindIndex(e => e.name == "player_block_yellow");
        }
    }
}