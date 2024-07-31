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
        public byte LeftGrenades = 2*20;

        [CanBeNull] public Model.Weapon Block, // Block
            Melee, // Melee
            Primary, // Rifles
            Secondary, // Pistols
            Tertiary, // Misc
            Grenade;

        public byte BlockId;
        public BlockType BlockType => VoxelData.BlockTypes[BlockId];

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
            Grenade = Model.Weapon.Grenades[1];
            Blocks = 100;
            BlockId = (byte)VoxelData.BlockTypes.ToList().FindIndex(e => e.name == "player_block_yellow");
        }
    }
}