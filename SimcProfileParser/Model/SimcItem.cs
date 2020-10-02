using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimcProfileParser.Model
{
    public class SimcItem
    {
        public uint ItemId { get; set; }
        public uint ItemLevel { get; set; }
        public List<SimcItemMod> Mods { get; set; }
        public int SlotType { get; set; }
        public int Quality { get; internal set; }
        public InventoryType InventoryType { get; internal set; }

        public SimcItem()
        {
            Mods = new List<SimcItemMod>();
            InventoryType = InventoryType.INVTYPE_NON_EQUIP;
        }

        /// <summary>
        /// Create or update the mod for the relevant stat
        /// </summary>
        /// <param name="stat"></param>
        /// <param name="amount"></param>
        internal void AddModRatingAmount(ItemModType stat, int amount)
        {
            var mod = Mods.Where(m => m.Type == stat).FirstOrDefault();

            if (mod == null)
            {
                mod = new SimcItemMod
                {
                    Type = stat
                };
                Mods.Add(mod);
            }

            mod.StatRating += amount;

        }
    }
}
