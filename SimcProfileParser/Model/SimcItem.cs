using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimcProfileParser.Model
{
    public class SimcItem
    {
        public uint ItemId { get; internal set; }
        public string Name { get; internal set; }

        public int ItemLevel { get; internal set; }
        public ItemQuality Quality { get; internal set; }

        public List<SimcItemMod> Mods { get; internal set; }
        public List<ItemSocketColor> Sockets { get; internal set; }
        public List<SimcItemGem> Gems { get; set; }

        public InventoryType InventoryType { get; internal set; }
        public ItemClass ItemClass { get; internal set; }
        public int ItemSubClass { get; internal set; }


        public SimcItem()
        {
            Mods = new List<SimcItemMod>();
            Gems = new List<SimcItemGem>();
            Sockets = new List<ItemSocketColor>();
            InventoryType = InventoryType.INVTYPE_NON_EQUIP;
            ItemClass = ItemClass.ITEM_CLASS_MISC; // Not a great default but at least its not used
        }
    }
}
