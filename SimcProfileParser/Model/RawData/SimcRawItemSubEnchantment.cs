using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.RawData
{
    class SimcRawItemSubEnchantment
    {
        /// <summary>
        /// item_enchantment
        /// </summary>
        public uint Type { get; set; }
        public int Amount { get; set; }
        /// <summary>
        /// item_mod_type
        /// </summary>
        public uint Property { get; set; }
        /// <summary>
        /// item ecnhant scaling multiplier for data table
        /// </summary>
        public double Coefficient { get; set; }
    }
}
