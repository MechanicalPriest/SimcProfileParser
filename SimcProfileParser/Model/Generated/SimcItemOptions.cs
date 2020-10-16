using SimcProfileParser.Model.RawData;
using System.Collections.Generic;

namespace SimcProfileParser.Model.Generated
{
    public class SimcItemOptions
    {
        public uint ItemId { get; set; }
        public int ItemLevel { get; set; }
        public IList<int> BonusIds { get; set; }
        public IList<int> GemIds { get; set; }
        public ItemQuality Quality { get; set; }
        public int DropLevel { get; set; }

        public SimcItemOptions()
        {
            BonusIds = new List<int>();
            GemIds = new List<int>();
            Quality = ItemQuality.ITEM_QUALITY_NONE;
        }
    }
}
