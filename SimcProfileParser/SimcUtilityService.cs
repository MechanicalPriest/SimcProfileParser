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
                    // Armor Handling is now done by slot
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
            // from util::class_id in util.cpp
            // These numbers come from the location of each in the array.
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
                PlayerScaling.EVOKER => 13,
                PlayerScaling.PLAYER_SPECIAL_SCALE => 14,
                PlayerScaling.PLAYER_SPECIAL_SCALE2 => 15,
                PlayerScaling.PLAYER_SPECIAL_SCALE3 => 16,
                PlayerScaling.PLAYER_SPECIAL_SCALE4 => 17,
                PlayerScaling.PLAYER_SPECIAL_SCALE5 => 18,
                PlayerScaling.PLAYER_SPECIAL_SCALE6 => 19,
                PlayerScaling.PLAYER_SPECIAL_SCALE7 => 14,
                PlayerScaling.PLAYER_SPECIAL_SCALE8 => 20,
                PlayerScaling.PLAYER_SPECIAL_SCALE9 => 20,
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

        public async Task<int> GetScaledModValueAsync(SimcItem item, ItemModType modType, int statAllocation, int currentStatValue = 0, int newItemLevel = 0)
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
                double rawValue = statAllocation * itemBudget * 0.0001d - socketPenalty;

                if (GetIsCombatRating(modType))
                {
                    // based on item_database::apply_combat_rating_multiplier
                    var combatRatingType = GetCombatRatingMultiplierType(item.InventoryType);
                    if (combatRatingType != CombatRatingMultiplayerType.CR_MULTIPLIER_INVALID)
                    {
                        var combatRatingMultiplier = await GetCombatRatingMultiplierAsync(
                            item.ItemLevel, combatRatingType);

                        if (combatRatingMultiplier != 0)
                            rawValue = rawValue * combatRatingMultiplier;
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
                            rawValue = rawValue * staminaRatingMultiplier;
                    }
                }

                return (int)Math.Round(rawValue);
            }
            else
            {
                if (currentStatValue == 0 || newItemLevel == 0 || item.ItemLevel == 0)
                {
                    _logger?.LogError($"Items and mods that don't scale are not yet implemented. modType: {modType}, slotType: {slotType}, statAllocation: {statAllocation}, itemBudget: {itemBudget}");
                    return 0;
                }
                else
                {
                    var approximateCoefficient = ApproximateScaleCoefficient(item.ItemLevel, newItemLevel);
                    return (int)Math.Floor(currentStatValue * approximateCoefficient);
                }
            }

                       

            // TODO: Some new items have a zero value for stats like stamina.
            //throw new NotImplementedException("Items and mods that don't scale are not yet implemented");
            //return 0; // Remove this if the exception is re-implemented.
        }

        private double ApproximateScaleCoefficient(int currentIlvl, int newIlvl)
        {
            if(currentIlvl == 0 && newIlvl == 0)
                return 1.0d;

            var diff = currentIlvl - newIlvl;
            return 1.0 / Math.Pow(1.15, diff / 15.0);
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

        #region Spell affects

        /// <summary>
        /// Aura subtypes whose MiscValue1 is a spell category. simc: dbc::effect_category_subtypes
        /// </summary>
        private static readonly HashSet<uint> _categoryEffectSubTypes =
        [
            148, // A_MOD_CHARGE_RECHARGE_RATE
            341, // A_MODIFY_CATEGORY_COOLDOWN
            411, // A_MOD_MAX_CHARGES
            453, // A_MOD_RECHARGE_TIME_CATEGORY
            454, // A_MOD_RECHARGE_TIME_PCT_CATEGORY
            457, // A_HASTED_CATEGORY
            500, // A_IGNORE_SPELL_CHARGE_COOLDOWN_CATEGORY
        ];

        /// <summary>
        /// Effect types whose MiscValue1 is a spell category
        /// </summary>
        private static readonly HashSet<uint> _categoryEffectTypes =
        [
            292, // E_MODIFY_COOLDOWN_IN_CATEGORY
            293, // E_RECHARGE_CATEGORY_COOLDOWN_IMMEDIATE
        ];

        /// <summary>
        /// Aura subtypes whose MiscValue1 is a spell label. simc: sc_spell_info.cpp / sc_const_data.cpp generate_indices
        /// </summary>
        private static readonly HashSet<uint> _labelMiscValue1EffectSubTypes =
        [
            143, // A_MOD_RECHARGE_RATE_LABEL
            307, // A_ENABLE_CAST_WHILE_MOVING_FOR_SPELL_LABEL
            320, // A_APPLY_HASTED_GCD_LABEL
            470, // A_MOD_TIME_RATE_BY_SPELL_LABEL
            507, // A_MOD_DAMAGE_FROM_SPELLS_LABEL
            537, // A_MOD_DAMAGE_FROM_CASTER_SPELLS_LABEL
        ];

        /// <summary>
        /// Aura subtypes whose MiscValue2 is a spell label (MiscValue1 is the property modified)
        /// </summary>
        private static readonly HashSet<uint> _labelMiscValue2EffectSubTypes =
        [
            218, // A_ADD_PCT_LABEL_MODIFIER
            219, // A_ADD_FLAT_LABEL_MODIFIER
        ];

        /// <summary>
        /// E_REMOVE_AURA_BY_SPELL_LABEL, MiscValue1 is a spell label
        /// </summary>
        private const uint _removeAuraByLabelEffectType = 212;

        private sealed class SpellAffectIndex
        {
            public List<SimcRawSpell> Source { get; init; }
            public Dictionary<uint, List<SimcRawSpell>> SpellsByFamily { get; } = [];
            public Dictionary<uint, List<uint>> SpellIdsByCategory { get; } = [];
            /// <summary>
            /// Category => ids of spells with an effect modifying that category
            /// </summary>
            public Dictionary<uint, List<uint>> SpellIdsAffectingCategory { get; } = [];
            public Dictionary<uint, List<uint>> SpellIdsByLabel { get; } = [];
            /// <summary>
            /// Label => ids of spells with an effect modifying spells with that label
            /// </summary>
            public Dictionary<uint, List<uint>> SpellIdsAffectingLabel { get; } = [];
        }

        private SpellAffectIndex _spellAffectIndex;

        public async Task<List<uint>> GetSpellIdsAffectedByEffectAsync(SimcRawSpell spell, SimcRawSpellEffect effect)
        {
            var index = await GetSpellAffectIndexAsync();
            var result = new SortedSet<uint>();

            if (spell.ClassFlagsFamily != 0
                && index.SpellsByFamily.TryGetValue(spell.ClassFlagsFamily, out var familySpells))
            {
                foreach (var familySpell in familySpells)
                {
                    if (SharesClassFlag(effect.ClassFlags, familySpell.ClassFlags))
                        result.Add(familySpell.Id);
                }
            }

            if (IsCategoryEffect(effect)
                && index.SpellIdsByCategory.TryGetValue((uint)effect.MiscValue1, out var categorySpellIds))
            {
                result.UnionWith(categorySpellIds);
            }

            var label = GetEffectLabel(effect);
            if (label != 0
                && index.SpellIdsByLabel.TryGetValue(label, out var labelSpellIds))
            {
                result.UnionWith(labelSpellIds);
            }

            return [.. result];
        }

        public async Task<List<uint>> GetSpellIdsAffectingSpellAsync(SimcRawSpell spell)
        {
            var index = await GetSpellAffectIndexAsync();
            var result = new SortedSet<uint>();

            if (spell.ClassFlagsFamily != 0
                && index.SpellsByFamily.TryGetValue(spell.ClassFlagsFamily, out var familySpells))
            {
                foreach (var familySpell in familySpells)
                {
                    if (familySpell.Id == spell.Id)
                        continue;

                    if (familySpell.Effects.Any(e => SharesClassFlag(e.ClassFlags, spell.ClassFlags)))
                        result.Add(familySpell.Id);
                }
            }

            if (spell.Category != 0
                && index.SpellIdsAffectingCategory.TryGetValue(spell.Category, out var categoryAffectors))
            {
                result.UnionWith(categoryAffectors);
            }

            foreach (var label in spell.Labels ?? [])
            {
                if (index.SpellIdsAffectingLabel.TryGetValue(label, out var labelAffectors))
                    result.UnionWith(labelAffectors);
            }

            return [.. result];
        }

        private async Task<SpellAffectIndex> GetSpellAffectIndexAsync()
        {
            var spells = await _cacheService.GetParsedFileContentsAsync<List<SimcRawSpell>>(SimcParsedFileType.SpellData);

            // Rebuild whenever the cache hands back a different list, e.g. after the cache is cleared or refreshed.
            var index = _spellAffectIndex;
            if (index != null && ReferenceEquals(index.Source, spells))
                return index;

            index = new SpellAffectIndex() { Source = spells };

            foreach (var spell in spells)
            {
                if (spell.ClassFlagsFamily != 0)
                    AddToIndex(index.SpellsByFamily, spell.ClassFlagsFamily, spell);

                if (spell.Category != 0)
                    AddToIndex(index.SpellIdsByCategory, spell.Category, spell.Id);

                foreach (var label in spell.Labels ?? [])
                    AddToIndex(index.SpellIdsByLabel, label, spell.Id);

                foreach (var effect in spell.Effects)
                {
                    // Duplicates are fine here, results are de-duplicated on lookup
                    if (IsCategoryEffect(effect))
                        AddToIndex(index.SpellIdsAffectingCategory, (uint)effect.MiscValue1, spell.Id);

                    var label = GetEffectLabel(effect);
                    if (label != 0)
                        AddToIndex(index.SpellIdsAffectingLabel, label, spell.Id);
                }
            }

            _spellAffectIndex = index;
            return index;
        }

        private static void AddToIndex<T>(Dictionary<uint, List<T>> index, uint key, T value)
        {
            if (!index.TryGetValue(key, out var list))
            {
                list = [];
                index[key] = list;
            }
            list.Add(value);
        }

        private static bool IsCategoryEffect(SimcRawSpellEffect effect)
            => effect.MiscValue1 > 0
            && (_categoryEffectSubTypes.Contains(effect.EffectSubType) || _categoryEffectTypes.Contains(effect.EffectType));

        /// <summary>
        /// Get the spell label an effect targets, or 0 if it doesn't target a label
        /// </summary>
        private static uint GetEffectLabel(SimcRawSpellEffect effect)
        {
            int label = 0;

            if (_labelMiscValue2EffectSubTypes.Contains(effect.EffectSubType))
                label = effect.MiscValue2;
            else if (_labelMiscValue1EffectSubTypes.Contains(effect.EffectSubType)
                || effect.EffectType == _removeAuraByLabelEffectType)
                label = effect.MiscValue1;

            return label > 0 ? (uint)label : 0;
        }

        private static bool SharesClassFlag(uint[] a, uint[] b)
        {
            if (a == null || b == null)
                return false;

            for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                if ((a[i] & b[i]) != 0)
                    return true;
            }

            return false;
        }

        #endregion

        public async Task<List<SimcRawRppmEntry>> GetSpellRppmModifiersAsync(uint spellId)
        {
            var modifiers = await _cacheService.GetParsedFileContentsAsync<List<SimcRawRppmEntry>>(SimcParsedFileType.RppmData);

            var result = modifiers.Where(m => m.SpellId == spellId).ToList();

            return result;
        }

        public async Task<SimcRawItemEffect> GetItemEffectAsync(uint itemEffectId)
        {
            var itemEffects = await _cacheService.GetParsedFileContentsAsync<List<SimcRawItemEffect>>(SimcParsedFileType.ItemEffectData);

            var result = itemEffects.Where(e => e.Id == itemEffectId).ToList().FirstOrDefault();

            return result;
        }

        public async Task<string> GetClientDataVersionAsync()
        {
            var clientDataVersion = await _cacheService.GetParsedFileContentsAsync<string>(SimcParsedFileType.GameDataVersion);

            return clientDataVersion;
        }

        #region Talent rank values

        private sealed class TalentRankIndex
        {
            public List<SimcRawTrait> TraitSource { get; init; }
            public List<SimcRawCurvePoint> CurveSource { get; init; }
            /// <summary>
            /// (Spell id, effect index) => (operation, curve values)
            /// </summary>
            public Dictionary<(uint SpellId, uint EffectIndex), (int Operation, List<double> Values)> Values { get; } = [];
        }

        private TalentRankIndex _talentRankIndex;

        public async Task<(int Operation, List<double> Values)?> GetTalentRankValuesAsync(uint spellId, uint effectIndex)
        {
            var index = await GetTalentRankIndexAsync();

            if (index.Values.TryGetValue((spellId, effectIndex), out var result))
                return (result.Operation, [.. result.Values]);

            return null;
        }

        private async Task<TalentRankIndex> GetTalentRankIndexAsync()
        {
            var traits = await _cacheService.GetParsedFileContentsAsync<List<SimcRawTrait>>(SimcParsedFileType.TraitData);
            var curvePoints = await _cacheService.GetParsedFileContentsAsync<List<SimcRawCurvePoint>>(SimcParsedFileType.CurvePoints);

            // Rebuild whenever the cache hands back different lists, e.g. after the cache is cleared or refreshed.
            var index = _talentRankIndex;
            if (index != null && ReferenceEquals(index.TraitSource, traits) && ReferenceEquals(index.CurveSource, curvePoints))
                return index;

            index = new TalentRankIndex() { TraitSource = traits, CurveSource = curvePoints };

            var neededCurves = traits
                .SelectMany(t => t.DefinitionEffects ?? [])
                .Select(e => e.CurveId)
                .ToHashSet();

            // Curve values in curve order, matching simc's curve_point_t::find
            var curveValues = curvePoints
                .Where(c => neededCurves.Contains(c.CurveId))
                .GroupBy(c => c.CurveId)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Index).Select(c => (double)(decimal)c.Primary2).ToList()); // via decimal so 1.2f is 1.2, not 1.2000000476837158

            foreach (var trait in traits)
            {
                foreach (var effect in trait.DefinitionEffects ?? [])
                {
                    // A spell used by several trait entries shares the same definition effects, so the first one wins
                    var key = (trait.SpellId, effect.EffectIndex);
                    if (index.Values.ContainsKey(key))
                        continue;

                    index.Values[key] = (effect.Operation,
                        curveValues.TryGetValue(effect.CurveId, out var values) ? values : []);
                }
            }

            _talentRankIndex = index;
            return index;
        }

        #endregion

        public async Task<SimcRawTrait> GetTraitDataAsync(int traitEntryId)
        {
            var talents = await _cacheService.GetParsedFileContentsAsync<List<SimcRawTrait>>(SimcParsedFileType.TraitData);

            var result = talents.Where(e => e.TraitNodeEntryId == traitEntryId).ToList().FirstOrDefault();

            return result;
        }

        public Task<List<SimcRawTrait>> GetTraitsByClassSpecAsync(int classId, int specId)
        {
            // TODO: Implement this. (Make it 'async')
            throw new NotImplementedException();
        }
    }
}
