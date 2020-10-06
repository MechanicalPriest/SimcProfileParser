using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.RawData
{
    class SimcRawItem
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public uint Flags1 { get; set; }
        public uint Flags2 { get; set; }
        public uint TypeFlags { get; set; }
        public int ItemLevel { get; set; }
        public int RequiredLevel { get; set; }
        public int RequiredSkill { get; set; }
        public int RequiredSkillLevel { get; set; }
        /// <summary>
        /// This links to the Quality enum
        /// </summary>
        public ItemQuality Quality { get; set; }
        /// <summary>
        /// This links to the InventoryType enum
        /// </summary>
        public InventoryType InventoryType { get; set; }
        public ItemClass ItemClass { get; set; }
        public int ItemSubClass { get; set; }
        public int BindType { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public double Delay { get; set; }
        public double DamageRange { get; set; }
        public double ItemModifier { get; set; }
        /// <summary>
        /// Index of the ItemMods array the items mods start at
        /// </summary>
        public int DbcStats { get; set; }
        public uint DbcStatsCount { get; set; }
        public ulong RaceMask { get; set; }
        public uint ClassMask { get; set; }
        public List<SimcRawItemMod> ItemMods { get; set; }
        public List<SimcRawItemEffect> ItemEffects { get; set; }
        public int[] SocketColour { get; set; }
        public int GemProperties { get; set; }
        public int SocketBonusId { get; set; }
        public int SetId { get; set; }
        /// <summary>
        /// Was ScalingDistributionId
        /// </summary>
        public int CurveId { get; set; }
        public uint ArtifactId { get; set; }

        public SimcRawItem()
        {
            ItemMods = new List<SimcRawItemMod>();
            ItemEffects = new List<SimcRawItemEffect>();
        }
    }
}
