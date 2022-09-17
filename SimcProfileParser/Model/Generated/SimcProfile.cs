using SimcProfileParser.Model.Profile;
using System.Collections.Generic;

namespace SimcProfileParser.Model.Generated
{
    public class SimcProfile
    {
        public SimcParsedProfile ParsedProfile { get; internal set; }
        public IList<SimcItem> GeneratedItems { get; internal set; }
        /// <summary>
        /// Parsed talent information
        /// </summary>
        public IList<SimcTalent> Talents { get; internal set; }

        public SimcProfile()
        {
            GeneratedItems = new List<SimcItem>();
            Talents = new List<SimcTalent>();
        }
    }
}
