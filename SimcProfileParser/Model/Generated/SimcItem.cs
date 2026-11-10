using SimcProfileParser.Model.RawData;
using System.Collections.Generic;

namespace SimcProfileParser.Model.Generated
{
    public class SimcItem
    {
        public uint ItemId { get; internal set; }
        /// <summary>
        /// The slot the item is in, from the simc profile, e.g. "head", "finger1" or "trinket2".
        /// Null for items created from <see cref="SimcItemOptions"/>
        /// </summary>
        public string Slot { get; internal set; }
        public string Name { get; internal set; }

        public int ItemLevel { get; internal set; }
        public bool ItemLevelForced { get; internal set; }
        public bool Equipped { get; internal set; }
        public ItemQuality Quality { get; internal set; }

        public List<SimcItemMod> Mods { get; internal set; }
        public List<ItemSocketColor> Sockets { get; internal set; }
        public List<SimcItemGem> Gems { get; set; }
        public List<SimcItemEffect> Effects { get; set; }
        /// <summary>
        /// The item's permanent enchant from enchant_id=, or null if it has none
        /// </summary>
        public SimcItemEnchant Enchant { get; internal set; }

        public InventoryType InventoryType { get; internal set; }
        public ItemClass ItemClass { get; internal set; }
        public int ItemSubClass { get; internal set; }

        public SimcItem()
        {
            Mods = new List<SimcItemMod>();
            Gems = new List<SimcItemGem>();
            Effects = new List<SimcItemEffect>();
            Sockets = new List<ItemSocketColor>();
            InventoryType = InventoryType.INVTYPE_NON_EQUIP;
            ItemClass = ItemClass.ITEM_CLASS_MISC; // Not a great default but at least its not used
            ItemLevelForced = false;
        }
    }
}
