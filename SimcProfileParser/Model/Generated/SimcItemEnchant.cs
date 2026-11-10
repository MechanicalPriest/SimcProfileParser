using System.Collections.Generic;

namespace SimcProfileParser.Model.Generated
{
    /// <summary>
    /// A permanent enchant applied to an item with enchant_id=
    /// </summary>
    public class SimcItemEnchant
    {
        /// <summary>
        /// The enchant ID from enchant_id=
        /// </summary>
        public uint EnchantId { get; internal set; }
        /// <summary>
        /// The enchant's name, e.g. "Enchant Chest - Mark of the Magister". Some enchants use a description with
        /// placeholders instead, e.g. "+$k1 Intellect &amp; +$k2 Stamina"
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Stats the enchant adds, scaled as simc does
        /// </summary>
        public List<SimcItemEnchantStat> Stats { get; internal set; } = new List<SimcItemEnchantStat>();
        /// <summary>
        /// Spells the enchant applies, for enchants with effects other than stats (procs, equip and use effects)
        /// </summary>
        public List<uint> SpellIds { get; internal set; } = new List<uint>();
    }
}
