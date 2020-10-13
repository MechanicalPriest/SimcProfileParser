using SimcProfileParser.Model.RawData;

namespace SimcProfileParser.Model.Generated
{
    public class SimcSpellOptions
    {
        public uint SpellId { get; set; }
        public int ItemLevel { get; set; }
        public uint? PlayerLevel { get; set; }
        public ItemQuality? ItemQuality { get; internal set; }
        public InventoryType? ItemInventoryType { get; internal set; }

        public SimcSpellOptions()
        {
            ItemQuality = null;
            ItemInventoryType = null;
        }
    }
}
