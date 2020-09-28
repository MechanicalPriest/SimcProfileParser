using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SimcProfileParser.Model.Profile
{
    /// <summary>
    /// Represents a profile export file from the Simc Addon
    /// </summary>
    public class SimcParsedProfile
    {
        /// <summary>
        /// Comment present showing the simc addon version. e.g. SimC Addon 1.XX.Y
        /// </summary>
        public string SimcAddonVersion { get; internal set; }
        
        /// <summary>
        /// Comment present showing the date and time the profile was collected.
        /// </summary>
        public DateTime CollectionDate { get; internal set; }
        public string Class { get; internal set; }
        public string Spec { get; internal set; }
        public string Name { get; internal set; }
        public int Level { get; internal set; }
        public string Race { get; internal set; }
        public string Role { get; internal set; }

        public string Covenant { get; internal set; }
        public IReadOnlyList<SimcParsedSoulbind> Soulbinds { get; internal set; }
        /// <summary>
        /// Available conduits
        /// </summary>
        public IReadOnlyList<SimcParsedConduit> Conduits { get; internal set; }
        /// <summary>
        /// Covenant Renown level
        /// </summary>
        public int Renown { get; internal set; }

        /// <summary>
        /// Not yet implemented 
        /// </summary>
        public IReadOnlyList<SimcParsedProfession> Professions { get; internal set; }
        public IReadOnlyList<int> Talents { get; internal set; }

        public string Region { get; internal set; }
        public string Server { get; internal set; }

        public IReadOnlyList<SimcParsedItem> Items { get; internal set; }

        public IReadOnlyList<SimcParsedLine> ProfileLines { get; internal set; }

        public SimcParsedProfile()
        {
            Items = new List<SimcParsedItem>();
            Professions = new List<SimcParsedProfession>();
            Talents = new List<int>();

            Soulbinds = new List<SimcParsedSoulbind>();
            Conduits = new List<SimcParsedConduit>();

            ProfileLines = new List<SimcParsedLine>();
        }
    }
}
