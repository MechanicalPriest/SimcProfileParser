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
        Task<int> GetScaledModValueAsync(SimcItem item, ItemModType modType, int statAllocation);
        Task<List<SimcRawRppmEntry>> GetSpellRppmModifiersAsync(uint spellId);
        Task<double> GetSpellScalingMultiplierAsync(int scaleIndex, int playerLevel);
        Task<double> GetStaminaMultiplierAsync(int itemLevel, CombatRatingMultiplayerType staminaRatingType);
        Task<List<SimcRawSpellConduitRankEntry>> GetSpellConduitRanksAsync(uint spellId);
        Task<uint> GetSpellConduitSpellIdAsync(uint conduitId);
        Task<SimcRawItemEffect> GetItemEffectAsync(uint itemEffectId);
        Task<SimcRawTrait> GetTraitDataAsync(int traitEntryId);
        Task<List<SimcRawTrait>> GetTraitsByClassSpecAsync(int classId, int specId);
    }
}
