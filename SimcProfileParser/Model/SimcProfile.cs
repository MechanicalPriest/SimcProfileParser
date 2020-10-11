using SimcProfileParser.Model.Profile;
using System.Collections;
using System.Collections.Generic;

namespace SimcProfileParser.Model
{
    public class SimcProfile
    {
        public SimcParsedProfile ParsedProfile { get; internal set; }
        public IList<SimcItem> GeneratedItems { get; internal set; }

        public SimcProfile()
        {
            GeneratedItems = new List<SimcItem>();
        }
    }
}
