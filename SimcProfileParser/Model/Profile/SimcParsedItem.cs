using System.Collections.Generic;

namespace SimcProfileParser.Model.Profile
{
    public class SimcParsedItem
    {
        public string Slot { get; internal set; }
        public uint ItemId { get; internal set; }
        public int ItemLevel { get; internal set; }
        public int EnchantId { get; internal set; }
        public IReadOnlyCollection<int> GemIds { get; internal set; }
        public IReadOnlyCollection<int> BonusIds { get; internal set; }
        public IReadOnlyCollection<int> CraftedStatIds { get; internal set; }
        public int Context { get; internal set; }
        public int DropLevel { get; internal set; }
        /// <summary>
        /// The content_tuning= item option from a simc profile. New in Midnight and used to cap the level some items scale to.
        /// </summary>
        public int ContentTuningId { get; internal set; }
        /// <summary>
        /// The redirected_base_stats= item option. Items converted through the catalyst keep the base stats of the
        /// item they were converted from, which is the item ID given here.
        /// </summary>
        public uint RedirectedBaseStatsItemId { get; internal set; }
        public bool Equipped { get; internal set; }

        public SimcParsedItem()
        {
            GemIds = new List<int>();
            BonusIds = new List<int>();
            CraftedStatIds = new List<int>();
        }
    }
}
