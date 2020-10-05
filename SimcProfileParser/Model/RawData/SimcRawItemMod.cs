using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.RawData
{
    class SimcRawItemMod
    {
        /// <summary>
        /// Item Mod Types from item_mod_type in data_enums.hh - Common ones:
        /// 5	ITEM_MOD_INTELLECT
        /// 7	ITEM_MOD_STAMINA
        /// 32	ITEM_MOD_CRIT_RATING
        /// 36	ITEM_MOD_HASTE_RATING
        /// 49	ITEM_MOD_MASTERY_RATING
        /// 62	ITEM_MOD_LEECH_RATING
        /// 
        /// </summary>
        public ItemModType ModType { get; set; }
        public int StatAllocation { get; set; }
        public double SocketMultiplier { get; set; }

        public override string ToString()
        {
            return $"{ModType} - {StatAllocation} ({SocketMultiplier})";
        }
    }
}
