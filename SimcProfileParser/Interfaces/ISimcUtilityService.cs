using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    interface ISimcUtilityService
    {
        // Static Lookups
        int GetClassId(PlayerScaling scaleType);
        CombatRatingMultiplayerType GetCombatRatingMultiplierType(InventoryType inventoryType);
        bool GetIsCombatRating(ItemModType modType);
        PlayerScaling GetScaleClass(int scaleType);
        int GetSlotType(ItemClass itemClass, int itemSubClass, InventoryType itemInventoryType);
        // Data Methods
        Task<double> GetCombatRatingMultiplierAsync(int itemLevel, CombatRatingMultiplayerType combatRatingType);
        Task<SimcRawGemProperty> GetGemPropertyAsync(int gemId);
        Task<string> GetClientDataVersionAsync();
        Task<double> GetItemBudgetAsync(int itemLevel, ItemQuality itemQuality, int maxItemlevel);
        Task<SimcRawItemEnchantment> GetItemEnchantmentAsync(uint enchantId);
        Task<SimcRawRandomPropData> GetRandomPropsAsync(int itemLevel);
        Task<SimcRawItem> GetRawItemDataAsync(uint itemId);
        Task<SimcRawSpell> GetRawSpellDataAsync(uint spellId);
        /// <summary>
        /// Get the ids of spells modified by a spell effect, matched by class family flags, spell category or spell label.
        /// simc: dbc_t::effect_affects_spells / dbc_t::spells_by_category
        /// </summary>
        Task<List<uint>> GetSpellIdsAffectedByEffectAsync(SimcRawSpell spell, SimcRawSpellEffect effect);
        /// <summary>
        /// Get the ids of spells that have an effect modifying the given spell, matched by class family flags, spell category or spell label.
        /// simc: dbc_t::effects_affecting_spell / dbc_t::effect_categories_affecting_spell
        /// </summary>
        Task<List<uint>> GetSpellIdsAffectingSpellAsync(SimcRawSpell spell);
        Task<int> GetScaledModValueAsync(SimcItem item, ItemModType modType, int statAllocation, int currentStatValue = 0, int newItemLevel = 0);
        Task<List<SimcRawRppmEntry>> GetSpellRppmModifiersAsync(uint spellId);
        Task<double> GetSpellScalingMultiplierAsync(int scaleIndex, int playerLevel);
        Task<double> GetStaminaMultiplierAsync(int itemLevel, CombatRatingMultiplayerType staminaRatingType);
        Task<SimcRawItemEffect> GetItemEffectAsync(uint itemEffectId);
        Task<SimcRawTrait> GetTraitDataAsync(int traitEntryId);
        Task<List<SimcRawTrait>> GetTraitsByClassSpecAsync(int classId, int specId);
    }
}
