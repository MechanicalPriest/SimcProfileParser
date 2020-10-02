using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.RawData
{
    class SimcRawItemEffect
    {
        public int SpellTriggerType { get; set; }
        public int SpellId { get; set; }
        public int CooldownDuration { get; set; }
        public int CooldownGroup { get; set; }
        public int CooldownGroupDuration { get; set; }

        public override string ToString()
        {
            return $@"{SpellTriggerType}, {SpellId}, {CooldownDuration}, {CooldownGroup}, {CooldownGroupDuration}";
        }
    }
}
