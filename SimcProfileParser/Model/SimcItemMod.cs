using SimcProfileParser.Model.RawData;

namespace SimcProfileParser.Model
{
    public class SimcItemMod
    {
        public ItemModType Type { get; set; }
        public int RawStatAllocation { get; set; }
        /// <summary>
        /// Calculated actual value of the stat on this item
        /// </summary>
        public int StatRating { get; set; }

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