using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Interfaces
{
    interface ISimcUtilityService
    {
        int GetClassId(PlayerScaling scaleType);
        double GetCombatRatingMultiplier(int itemLevel, CombatRatingMultiplayerType combatRatingType);
        CombatRatingMultiplayerType GetCombatRatingMultiplierType(InventoryType inventoryType);
        SimcRawGemProperty GetGemProperty(int gemId);
        bool GetIsCombatRating(ItemModType modType);
        double GetItemBudget(int itemLevel, ItemQuality itemQuality, int maxItemlevel);
        SimcRawItemEnchantment GetItemEnchantment(uint enchantId);
        SimcRawRandomPropData GetRandomProps(int itemLevel);
        SimcRawItem GetRawItemData(uint itemId);
        SimcRawSpell GetRawSpellData(uint spellId);
        PlayerScaling GetScaleClass(int scaleType);
        int GetScaledModValue(SimcItem item, ItemModType modType, int statAllocation);
        int GetSlotType(ItemClass itemClass, int itemSubClass, InventoryType itemInventoryType);
        List<SimcRawRppmEntry> GetSpellRppmModifiers(uint spellId);
        double GetSpellScalingMultiplier(int scaleIndex, int playerLevel);
        double GetStaminaMultiplier(int itemLevel, CombatRatingMultiplayerType staminaRatingType);
    }
}
