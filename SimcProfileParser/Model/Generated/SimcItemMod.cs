using SimcProfileParser.Model.RawData;

namespace SimcProfileParser.Model.Generated
{
    public class SimcItemMod
    {
        public ItemModType Type { get; internal set; }
        public int RawStatAllocation { get; internal set; }
        /// <summary>
        /// Calculated actual value of the stat on this item
        /// </summary>
        public int StatRating { get; internal set; }

        public SimcItemMod()
        {
            Type = ItemModType.ITEM_MOD_NONE;
        }

        public override string ToString()
        {
            return $@"{Type} ({RawStatAllocation}) Rating: {StatRating}";
        }
    }
}