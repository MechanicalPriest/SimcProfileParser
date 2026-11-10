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
        public IList<int> CraftedStatIds { get; set; }
        public ItemQuality Quality { get; set; }
        public int DropLevel { get; set; }
        /// <summary>
        /// The content_tuning= item option from a simc profile. New in Midnight and used to cap the level some items scale to.
        /// </summary>
        public int ContentTuningId { get; set; }
        /// <summary>
        /// The redirected_base_stats= item option from a simc profile. Items converted through the catalyst keep the
        /// base stats of the item they were converted from, which is the item ID given here.
        /// </summary>
        public uint RedirectedBaseStatsItemId { get; set; }
        /// <summary>
        /// The enchant_id= item option from a simc profile, the item's permanent enchant
        /// </summary>
        public int EnchantId { get; set; }
        /// <summary>
        /// Player level to scale gems and enchants to. Defaults to the max player level if not set
        /// </summary>
        public int PlayerLevel { get; set; }

        public SimcItemOptions()
        {
            BonusIds = new List<int>();
            GemIds = new List<int>();
            CraftedStatIds = new List<int>();
            Quality = ItemQuality.ITEM_QUALITY_NONE;
        }
    }
}
