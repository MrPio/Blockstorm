using System;
using System.Collections.Generic;
using System.Linq;

namespace Model
{
    public enum BlockHealth
    {
        NonDiggable = 0,
        OneHit = 1,
        Low = 40,
        Medium = 75,
        High = 120,
        Indestructible = int.MaxValue,
    }

    [Serializable]
    public class BlockType
    {
        public string name;
        public BlockHealth blockHealth;

        public bool isSolid, // Does the block generate collision?
            isTransparent; // Can you see through the block?

        public ushort topID, sideID, bottomID;

        public BlockType(string name, (ushort, ushort) topID, (ushort, ushort)? sideID = null,
            (ushort, ushort)? bottomID = null, bool isSolid = true, bool isTransparent = false,
            BlockHealth blockHealth = BlockHealth.NonDiggable)
        {
            this.name = name;
            this.isSolid = isSolid;
            this.isTransparent = isTransparent;
            this.topID = (ushort)(topID.Item1 * 16 + topID.Item2);
            this.sideID = sideID == null ? this.topID : (ushort)(sideID.Value.Item1 * 16 + sideID.Value.Item2);
            this.bottomID = bottomID == null
                ? this.topID
                : (ushort)(bottomID.Value.Item1 * 16 + bottomID.Value.Item2);
            this.blockHealth = blockHealth;
            TextureIDs = new Dictionary<int, ushort>
            {
                { 0, this.sideID },
                { 1, this.sideID },
                { 2, this.topID },
                { 3, this.bottomID },
                { 4, this.sideID },
                { 5, this.sideID },
            };
        }

        // Convert the face index to the corresponding texture ID
        // The face index order is given by VoxelData.FaceChecks
        public Dictionary<int, ushort> TextureIDs;

        public string AudioClip
        {
            get
            {
                var audioMap = new Dictionary<List<string>, string>()
                {
                    { new() { "plank", "log" }, "hit_wood" },
                    { new() { "dirt", "grass", "snow" }, "wet_grass1" },
                    { new() { "window", "glass" }, "destroy_glass" },
                    { new() { "crate", "barrel", "hay" }, "block_damage_light" },
                    { new() { "sand" }, "sand1" },
                    { new() { "clay" }, "gravel1" },
                    { new() { "bush", "foliage" }, "grass1" },
                    { new() { "bars" }, "prop_hit_0" },
                };
                foreach (var key in audioMap.Keys.Where(key => key.Any(it => name.Contains(it))))
                    return audioMap[key];
                return blockHealth is BlockHealth.Indestructible
                    ? "block_damage_indestructible"
                    : "block_damage_medium";
            }
        }

        public string GetMaterial => $"Textures/texturepacks/blockade/Materials/blockade_{topID + 1:D2}";
    }
}