using System.Collections.Generic;

namespace SimcProfileParser.Model.Profile
{
    public class SimcParsedSoulbind
    {
        public string Name { get; internal set; }
        public IReadOnlyList<int> SoulbindSpells { get; internal set; }
        public IReadOnlyList<SimcParsedConduit> SocketedConduits { get; internal set; }
        public bool IsActive { get; internal set; }

        public SimcParsedSoulbind()
        {

            SoulbindSpells = new List<int>();
            SocketedConduits = new List<SimcParsedConduit>();
        }
    }
}
