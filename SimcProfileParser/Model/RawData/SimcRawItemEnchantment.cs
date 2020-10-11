using System.Collections.Generic;

namespace SimcProfileParser.Model.RawData
{
    class SimcRawItemEnchantment
    {
        public uint Id { get; set; }
        public int Slot { get; set; }
        public uint GemId { get; set; }
        public int ScalingId { get; set; }
        public uint MinScalingLevel { get; set; }
        public uint MaxScalingLevel { get; set; }
        public uint RequiredSkill { get; set; }
        public uint RequiredSkillLevel { get; set; }
        public List<SimcRawItemSubEnchantment> SubEnchantments { get; set; }
        public uint SpellId { get; set; }
        public string Name { get; set; }

        public SimcRawItemEnchantment()
        {
            SubEnchantments = new List<SimcRawItemSubEnchantment>();
        }
    }
}
