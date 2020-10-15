using SimcProfileParser.Model.RawData;
using System.Collections.Generic;

namespace SimcProfileParser.Model.Generated
{
    public class SimcItemOptions
    {
        public uint ItemId { get; set; }
        public int ItemLevel { get; set; }
        public IList<int> BonusIds { get; internal set; }
        public IList<int> GemIds { get; internal set; }
        public ItemQuality Quality { get; internal set; }
        public int DropLevel { get; internal set; }

        public SimcItemOptions()
        {
            BonusIds = new List<int>();
            GemIds = new List<int>();
            Quality = ItemQuality.ITEM_QUALITY_NONE;
        }
    }
}
