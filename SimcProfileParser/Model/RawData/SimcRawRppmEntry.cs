using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.RawData
{
    class SimcRawRppmEntry
    {
        public uint SpellId { get; set; }
        /// <summary>
        /// Value for type, ex if it's player scaling it's the specid
        /// </summary>
        public uint Type { get; set; }
        /// <summary>
        /// 1 for haste scaling, 4 for spec scaling
        /// </summary>
        public RppmModifierType ModifierType { get; set; }
        /// <summary>
        /// The rppm modifier for these conditions
        /// </summary>
        public double Coefficient { get; set; }
    }
}
