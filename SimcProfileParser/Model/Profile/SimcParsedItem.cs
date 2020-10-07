using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SimcProfileParser.Model.Profile
{
    public class SimcParsedItem
    {
        public string Slot { get; internal set; }
        public uint ItemId { get; internal set; }
        public int EnchantId { get; internal set; }
        public IReadOnlyCollection<int> GemIds { get; internal set; }
        public IReadOnlyCollection<int> BonusIds { get; internal set; }
        public int Context { get; internal set; }
        public int DropLevel { get; internal set; }
        public bool Equipped { get; internal set; }
        public SimcParsedItem()
        {
            GemIds = new List<int>();
            BonusIds = new List<int>();
        }
    }
}
