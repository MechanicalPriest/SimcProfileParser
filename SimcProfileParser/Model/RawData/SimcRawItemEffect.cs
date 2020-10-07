using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.RawData
{
    class SimcRawItemEffect
    {
        public uint Id { get; set; }
        public uint SpellId { get; set; }
        public uint ItemId { get; set; }
        public int Index { get; set; }
        /// <summary>
        /// This could be spell trigger type?
        /// </summary>
        public int Type { get; set; }
        public int CooldownGroup { get; set; }
        public int CooldownDuration { get; set; }
        public int CooldownGroupDuration { get; set; }

        public override string ToString()
        {
            return $@"{Id}, {SpellId}, {ItemId}, {Index}, {Type}, {CooldownGroup}, {CooldownDuration}, {CooldownGroupDuration}";
        }
    }
}
