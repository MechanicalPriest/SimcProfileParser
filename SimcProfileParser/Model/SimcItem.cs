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
        public int ItemLevel { get; set; }
        public List<SimcItemMod> Mods { get; set; }
        public List<ItemSocketColor> Sockets { get; set; }
        public int SlotType { get; set; }
        public ItemQuality Quality { get; internal set; }
        public InventoryType InventoryType { get; internal set; }
        public string Name { get; set; }

        public SimcItem()
        {
            Mods = new List<SimcItemMod>();
            Sockets = new List<ItemSocketColor>();
            InventoryType = InventoryType.INVTYPE_NON_EQUIP;
        }
    }
}
