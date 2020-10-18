using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.DataSync;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimcProfileParser
{
    class SimcUtilityService : ISimcUtilityService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<SimcUtilityService> _logger;

        public SimcUtilityService(ICacheService cacheService,
            ILogger<SimcUtilityService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public int GetSlotType(ItemClass itemClass, int itemSubClass, InventoryType itemInventoryType)
        {
            // Based on item_database::random_suffix_type
            switch (itemClass)
            {
                case ItemClass.ITEM_CLASS_WEAPON:
                    var subClass = (ItemSubclassWeapon)itemSubClass;
                    switch (subClass)
                    {
                        case ItemSubclassWeapon.ITEM_SUBCLASS_WEAPON_AXE2:
                        case ItemSubclassWeapon.ITEM_SUBCLASS_WEAPON_MACE2:
                        case ItemSubclassWeapon.ITEM_SUBCLASS_WEAPON_POLEARM:
                        case ItemSubclassWeapon.ITEM_SUBCLASS_WEAPON_SWORD2:
                        case ItemSubclassWeapon.ITEM_SUBCLASS_WEAPON_STAFF:
                        case ItemSubclassWeapon.ITEM_SUBCLASS_WEAPON_GUN:
                        case ItemSubclassWeapon.ITEM_SUBCLASS_WEAPON_BOW:
                        case ItemSubclassWeapon.ITEM_SUBCLASS_WEAPON_CROSSBOW:
                        case ItemSubclassWeapon.ITEM_SUBCLASS_WEAPON_THROWN:
                            return 0;
                        default:
                            return 3;
                    }
                case ItemClass.ITEM_CLASS_ARMOR:
                    switch (itemInventoryType)
                    {
                        case InventoryType.INVTYPE_HEAD:
                        case InventoryType.INVTYPE_CHEST:
                        case InventoryType.INVTYPE_LEGS:
                        case InventoryType.INVTYPE_ROBE:
                            return 0;

                        case InventoryType.INVTYPE_SHOULDERS:
                        case InventoryType.INVTYPE_WAIST:
                        case InventoryType.INVTYPE_FEET:
                        case InventoryType.INVTYPE_HANDS:
                        case InventoryType.INVTYPE_TRINKET:
                            return 1;

                        case InventoryType.INVTYPE_NECK:
                        case InventoryType.INVTYPE_FINGER:
                        case InventoryType.INVTYPE_CLOAK:
                        case InventoryType.INVTYPE_WRISTS:
                            return 2;

                        case InventoryType.INVTYPE_WEAPONOFFHAND:
                        case InventoryType.INVTYPE_HOLDABLE:
                        case InventoryType.INVTYPE_SHIELD:
                            return 3;

                        default:
                            return -1;
                    }
                default:
                    return -1;
            }
        }

        public bool GetIsCombatRating(ItemModType modType)
        {
            // based on util::is_combat_rating
            switch (modType)
            {
                case ItemModType.ITEM_MOD_MASTERY_RATING:
                case ItemModType.ITEM_MOD_DODGE_RATING:
                case ItemModType.ITEM_MOD_PARRY_RATING:
                case ItemModType.ITEM_MOD_BLOCK_RATING:
                case ItemModType.ITEM_MOD_HIT_MELEE_RATING:
                case ItemModType.ITEM_MOD_HIT_RANGED_RATING:
                case ItemModType.ITEM_MOD_HIT_SPELL_RATING:
                case ItemModType.ITEM_MOD_CRIT_MELEE_RATING:
                case ItemModType.ITEM_MOD_CRIT_RANGED_RATING:
                case ItemModType.ITEM_MOD_CRIT_SPELL_RATING:
                case ItemModType.ITEM_MOD_CRIT_TAKEN_RANGED_RATING:
                case ItemModType.ITEM_MOD_CRIT_TAKEN_SPELL_RATING:
                case ItemModType.ITEM_MOD_HASTE_MELEE_RATING:
                case ItemModType.ITEM_MOD_HASTE_RANGED_RATING:
                case ItemModType.ITEM_MOD_HASTE_SPELL_RATING:
                case ItemModType.ITEM_MOD_HIT_RATING:
                case ItemModType.ITEM_MOD_CRIT_RATING:
                case ItemModType.ITEM_MOD_HIT_TAKEN_RATING:
                case ItemModType.ITEM_MOD_CRIT_TAKEN_RATING:
                case ItemModType.ITEM_MOD_RESILIENCE_RATING:
                case ItemModType.ITEM_MOD_HASTE_RATING:
                case ItemModType.ITEM_MOD_EXPERTISE_RATING:
                case ItemModType.ITEM_MOD_MULTISTRIKE_RATING:
                case ItemModType.ITEM_MOD_SPEED_RATING:
                case ItemModType.ITEM_MOD_LEECH_RATING:
                case ItemModType.ITEM_MOD_AVOIDANCE_RATING:
                case ItemModType.ITEM_MOD_VERSATILITY_RATING:
                case ItemModType.ITEM_MOD_EXTRA_ARMOR:
                    return true;
                default:
                    return false;
            }
        }

        public CombatRatingMultiplayerType GetCombatRatingMultiplierType(InventoryType inventoryType)
        {
            switch (inventoryType)
            {
                case InventoryType.INVTYPE_NECK:
                case InventoryType.INVTYPE_FINGER:
                    return CombatRatingMultiplayerType.CR_MULTIPLIER_JEWLERY;
                case InventoryType.INVTYPE_TRINKET:
                    return CombatRatingMultiplayerType.CR_MULTIPLIER_TRINKET;
                case InventoryType.INVTYPE_WEAPON:
                case InventoryType.INVTYPE_2HWEAPON:
                case InventoryType.INVTYPE_WEAPONMAINHAND:
                case InventoryType.INVTYPE_WEAPONOFFHAND:
                case InventoryType.INVTYPE_RANGED:
                case InventoryType.INVTYPE_RANGEDRIGHT:
                case InventoryType.INVTYPE_THROWN:
                    return CombatRatingMultiplayerType.CR_MULTIPLIER_WEAPON;
                case InventoryType.INVTYPE_ROBE:
                case InventoryType.INVTYPE_HEAD:
                case InventoryType.INVTYPE_SHOULDERS:
                case InventoryType.INVTYPE_CHEST:
                case InventoryType.INVTYPE_CLOAK:
                case InventoryType.INVTYPE_BODY:
                case InventoryType.INVTYPE_WRISTS:
                case InventoryType.INVTYPE_WAIST:
                case InventoryType.INVTYPE_LEGS:
                case InventoryType.INVTYPE_FEET:
                case InventoryType.INVTYPE_SHIELD:
                case InventoryType.INVTYPE_HOLDABLE:
                case InventoryType.INVTYPE_HANDS:
                    return CombatRatingMultiplayerType.CR_MULTIPLIER_ARMOR;
                default:
                    return CombatRatingMultiplayerType.CR_MULTIPLIER_INVALID;
            }
        }

        public PlayerScaling GetScaleClass(int scaleType)
        {
            // from spell_data.hpp:scaling_class
            switch (scaleType)
            {
                case -8:
                    return PlayerScaling.PLAYER_SPECIAL_SCALE8;
                case -7:
                    return PlayerScaling.PLAYER_SPECIAL_SCALE7;
                case -6:
                    return PlayerScaling.PLAYER_SPECIAL_SCALE6;
                case -5:
                    return PlayerScaling.PLAYER_SPECIAL_SCALE5;
                case -4:
                    return PlayerScaling.PLAYER_SPECIAL_SCALE4;
                case -3:
                    return PlayerScaling.PLAYER_SPECIAL_SCALE3;
                case -2:
                    return PlayerScaling.PLAYER_SPECIAL_SCALE2;
                case -1:
                    return PlayerScaling.PLAYER_SPECIAL_SCALE;
                case 1:
                    return PlayerScaling.WARRIOR;
                case 2:
                    return PlayerScaling.PALADIN;
                case 3:
                    return PlayerScaling.HUNTER;
                case 4:
                    return PlayerScaling.ROGUE;
                case 5:
                    return PlayerScaling.PRIEST;
                case 6:
                    return PlayerScaling.DEATH_KNIGHT;
                case 7:
                    return PlayerScaling.SHAMAN;
                case 8:
                    return PlayerScaling.MAGE;
                case 9:
                    return PlayerScaling.WARLOCK;
                case 10:
                    return PlayerScaling.MONK;
                case 11:
                    return PlayerScaling.DRUID;
                case 12:
                    return PlayerScaling.DEMON_HUNTER;
                default:
                    break;
            }

            return PlayerScaling.PLAYER_NONE;
        }

        public int GetClassId(PlayerScaling scaleType)
        {
            // from util::class_id
            return scaleType switch
            {
                PlayerScaling.WARRIOR => 1,
                PlayerScaling.PALADIN => 2,
                PlayerScaling.HUNTER => 3,
                PlayerScaling.ROGUE => 4,
                PlayerScaling.PRIEST => 5,
                PlayerScaling.DEATH_KNIGHT => 6,
                PlayerScaling.SHAMAN => 7,
                PlayerScaling.MAGE => 8,
                PlayerScaling.WARLOCK => 9,
                PlayerScaling.MONK => 10,
                PlayerScaling.DRUID => 11,
                PlayerScaling.DEMON_HUNTER => 12,
                PlayerScaling.PLAYER_SPECIAL_SCALE => 13,
                PlayerScaling.PLAYER_SPECIAL_SCALE2 => 14,
                PlayerScaling.PLAYER_SPECIAL_SCALE3 => 15,
                PlayerScaling.PLAYER_SPECIAL_SCALE4 => 16,
                PlayerScaling.PLAYER_SPECIAL_SCALE5 => 17,
                PlayerScaling.PLAYER_SPECIAL_SCALE6 => 18,
                PlayerScaling.PLAYER_SPECIAL_SCALE7 => 13,
                PlayerScaling.PLAYER_SPECIAL_SCALE8 => 19,
                _ => 0,
            };
        }

        public async Task<double> GetItemBudgetAsync(int itemLevel, ItemQuality itemQuality, int maxItemlevel)
        {
            // from item_database::item_budget
            // Weirdly, ITEM_QUALITY_MAX (Heirloom) appears with the pic tier
            // and also for some reason pulling the first budget value, not compensating
            // for the item type using GetSlotType() ?
            double budget;
            var scale_ilvl = itemLevel;

            if (maxItemlevel > 0)
                scale_ilvl = Math.Min(scale_ilvl, maxItemlevel);

            var ilvlRandomProps = await GetRandomPropsAsync(scale_ilvl);

            switch (itemQuality)
            {
                case ItemQuality.ITEM_QUALITY_EPIC:
                case ItemQuality.ITEM_QUALITY_LEGENDARY:
                case ItemQuality.ITEM_QUALITY_MAX:
                    budget = ilvlRandomProps.Epic[0];
                    break;

                case ItemQuality.ITEM_QUALITY_RARE:
                    budget = ilvlRandomProps.Rare[0];
                    break;

                default: // Everything else
                    budget = ilvlRandomProps.Uncommon[0];
                    break;
            }

            return budget;
        }

        public async Task<int> GetScaledModValueAsync(SimcItem item, ItemModType modType, int statAllocation)
        {
            // based on item_database::scaled_stat
            var slotType = GetSlotType(item.ItemClass, item.ItemSubClass, item.InventoryType);

            var itemBudget = 0.0d;

            // If the item has a slot and quality we can parse
            if (slotType != -1 && item.Quality > 0)
            {
                var ilvlRandomProps = await GetRandomPropsAsync(item.ItemLevel);
                switch (item.Quality)
                {
                    case ItemQuality.ITEM_QUALITY_EPIC:
                    case ItemQuality.ITEM_QUALITY_LEGENDARY:
                        itemBudget = ilvlRandomProps.Epic[slotType];
                        break;

                    case ItemQuality.ITEM_QUALITY_RARE:
                    case ItemQuality.ITEM_QUALITY_MAX: // Heirloom
                        itemBudget = ilvlRandomProps.Rare[slotType];
                        break;

                    default: // Everything else
                        itemBudget = ilvlRandomProps.Uncommon[slotType];
                        break;
                }
            }

            // Scale the stat if we have an allocation & budget
            if (statAllocation > 0 && itemBudget > 0)
            {
                // Not yet implemented
                var socketPenalty = 0.0d;
                int rawValue = (int)(statAllocation * itemBudget * 0.0001d - socketPenalty + 0.5d);

                if (GetIsCombatRating(modType))
                {
                    // based on item_database::apply_combat_rating_multiplier
                    var combatRatingType = GetCombatRatingMultiplierType(item.InventoryType);
                    if (combatRatingType != CombatRatingMultiplayerType.CR_MULTIPLIER_INVALID)
                    {
                        var combatRatingMultiplier = await GetCombatRatingMultiplierAsync(
                            item.ItemLevel, combatRatingType);

                        if (combatRatingMultiplier != 0)
                            rawValue = (int)(rawValue * combatRatingMultiplier);
                    }
                }
                else if (modType == ItemModType.ITEM_MOD_STAMINA)
                {
                    // based on item_database::apply_stamina_multiplier
                    var staminaRatingType = GetCombatRatingMultiplierType(item.InventoryType);
                    if (staminaRatingType != CombatRatingMultiplayerType.CR_MULTIPLIER_INVALID)
                    {
                        var staminaRatingMultiplier = await GetStaminaMultiplierAsync(
                            item.ItemLevel, staminaRatingType);

                        if (staminaRatingMultiplier != 0)
                            rawValue = (int)(rawValue * staminaRatingMultiplier);
                    }
                }

                return rawValue;
            }

            _logger?.LogError("Items and mods that don't scale are not yet implemented");
            throw new NotImplementedException("Items and mods that don't scale are not yet implemented");
        }

        // TODO This is a hot mess. Need a service to retrieve data from these generated files.
        public async Task<SimcRawItem> GetRawItemDataAsync(uint itemId)
        {
            var rawItem = new SimcRawItem();

            var items = await _cacheService.GetParsedFileContentsAsync<List<SimcRawItem>>(SimcParsedFileType.ItemDataNew);

            rawItem = items.Where(i => i.Id == itemId).FirstOrDefault();

            if (rawItem == null)
            {
                // If we can't find it in the new data, try all the older items
                items = await _cacheService.GetParsedFileContentsAsync<List<SimcRawItem>>(SimcParsedFileType.ItemDataOld);
                rawItem = items.Where(i => i.Id == itemId).FirstOrDefault();
            }

            return rawItem;
        }

        // TODO This is a hot mess. Need a service to retrieve data from these generated files.
        public async Task<SimcRawRandomPropData> GetRandomPropsAsync(int itemLevel)
        {
            var rawProps = new SimcRawRandomPropData();

            var randomProps = await _cacheService.GetParsedFileContentsAsync<List<SimcRawRandomPropData>>(SimcParsedFileType.RandomPropPoints);

            rawProps = randomProps.Where(p => p.ItemLevel == itemLevel).FirstOrDefault();

            return rawProps;
        }

        public async Task<double> GetCombatRatingMultiplierAsync(int itemLevel, CombatRatingMultiplayerType combatRatingType)
        {
            var crMultipliers = await _cacheService.GetParsedFileContentsAsync<float[][]>(SimcParsedFileType.CombatRatingMultipliers);

            var crMulti = crMultipliers[(int)combatRatingType][itemLevel - 1];

            return crMulti;
        }

        public async Task<double> GetStaminaMultiplierAsync(int itemLevel, CombatRatingMultiplayerType staminaRatingType)
        {
            var stamMultipliers = await _cacheService.GetParsedFileContentsAsync<float[][]>(SimcParsedFileType.StaminaMultipliers);

            var stamMulti = stamMultipliers[(int)staminaRatingType][itemLevel - 1];

            return stamMulti;
        }

        public async Task<SimcRawGemProperty> GetGemPropertyAsync(int gemId)
        {
            var gemPropertyData = await _cacheService.GetParsedFileContentsAsync<List<SimcRawGemProperty>>(SimcParsedFileType.GemData);

            var gemProperty = gemPropertyData.Where(g => g.Id == gemId).FirstOrDefault();

            return gemProperty;
        }

        public async Task<SimcRawItemEnchantment> GetItemEnchantmentAsync(uint enchantId)
        {
            var itemEnchantData = await _cacheService.GetParsedFileContentsAsync<List<SimcRawItemEnchantment>>(SimcParsedFileType.ItemEnchantData);

            var enchantmentProperties = itemEnchantData.Where(e => e.Id == enchantId).FirstOrDefault();

            return enchantmentProperties;
        }

        public async Task<double> GetSpellScalingMultiplierAsync(int scaleIndex, int playerLevel)
        {
            var spellScaleData = await _cacheService.GetParsedFileContentsAsync<double[][]>(SimcParsedFileType.SpellScaleMultipliers);

            var result = spellScaleData[scaleIndex][playerLevel - 1];

            return result;
        }

        public async Task<SimcRawSpell> GetRawSpellDataAsync(uint spellId)
        {
            var spellData = await _cacheService.GetParsedFileContentsAsync<List<SimcRawSpell>>(SimcParsedFileType.SpellData);

            var result = spellData.Where(s => s.Id == spellId).FirstOrDefault();

            return result;
        }

        public async Task<List<SimcRawRppmEntry>> GetSpellRppmModifiersAsync(uint spellId)
        {
            var modifiers = await _cacheService.GetParsedFileContentsAsync<List<SimcRawRppmEntry>>(SimcParsedFileType.RppmData);

            var result = modifiers.Where(m => m.SpellId == spellId).ToList();

            return result;
        }

        public async Task<List<SimcRawSpellConduitRankEntry>> GetSpellConduitRanksAsync(uint spellId)
        {
            var conduitRanks = await _cacheService.GetParsedFileContentsAsync<List<SimcRawSpellConduitRankEntry>>(SimcParsedFileType.CovenantData);

            var result = conduitRanks.Where(m => m.SpellId == spellId).ToList();

            return result;
        }
    }
}
