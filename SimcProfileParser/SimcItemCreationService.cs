using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.DataSync;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.Profile;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimcProfileParser
{
    internal class SimcItemCreationService : ISimcItemCreationService
    {
        private readonly ICacheService _cacheService;
        private readonly ISimcSpellCreationService _simcSpellCreationService;
        private readonly ISimcUtilityService _simcUtilityService;
        private readonly ILogger<SimcItemCreationService> _logger;

        /// <summary>
        /// Curve used to squish item levels for Midnight. From SQUISH_CURVE_MIDNIGHT in sc_enums.hpp
        /// </summary>
        internal const int SquishCurveMidnight = 92181;

        public SimcItemCreationService(ICacheService cacheService,
            ISimcSpellCreationService simcSpellCreationService,
            ISimcUtilityService simcUtilityService,
            ILogger<SimcItemCreationService> logger)
        {
            _cacheService = cacheService;
            _simcSpellCreationService = simcSpellCreationService;
            _simcUtilityService = simcUtilityService;
            _logger = logger;
        }

        async Task<SimcItem> ISimcItemCreationService.CreateItemAsync(SimcParsedItem parsedItemData)
        {
            var item = await BuildItemAsync(parsedItemData.ItemId);

            if (item == null)
                throw new ArgumentOutOfRangeException(
                    nameof(parsedItemData.ItemId), $"ItemId not found: {parsedItemData.ItemId}");

            // Force item level if provided with ilevel=
            if(parsedItemData.ItemLevel > 0)
            {
                AddItemLevel(item, parsedItemData.ItemLevel - item.ItemLevel);
                item.ItemLevelForced = true;
            }

            await UpdateItemAsync(item,
                parsedItemData.BonusIds.ToList(),
                parsedItemData.GemIds.ToList(),
                parsedItemData.CraftedStatIds.ToList(),
                parsedItemData.DropLevel,
                parsedItemData.ContentTuningId);

            await UpdateItemEffects(item);

            return item;
        }

        async Task<SimcItem> ISimcItemCreationService.CreateItemAsync(SimcItemOptions itemOptions)
        {
            var item = await BuildItemAsync(itemOptions.ItemId);

            if (item == null)
                throw new ArgumentOutOfRangeException(
                    nameof(itemOptions.ItemId), $"ItemId not found: {itemOptions.ItemId}");

            // Set the item level if provided. This needs to be done first as item stats scale off it.
            if (itemOptions.ItemLevel > 0)
                AddItemLevel(item, itemOptions.ItemLevel - item.ItemLevel);

            // Set the quality if provided
            if (itemOptions.Quality != ItemQuality.ITEM_QUALITY_NONE)
                SetItemQuality(item, itemOptions.Quality);

            // Only squish the item level if it came from the item data, not if it was provided
            await UpdateItemAsync(item,
                itemOptions.BonusIds,
                itemOptions.GemIds,
                itemOptions.CraftedStatIds,
                itemOptions.DropLevel,
                itemOptions.ContentTuningId,
                applyItemLevelSquish: itemOptions.ItemLevel <= 0);

            await UpdateItemEffects(item);

            return item;
        }

        /// <summary>
        /// Setup a basic item
        /// </summary>
        /// <param name="itemId">The ID of the item</param>
        internal async Task<SimcItem> BuildItemAsync(uint itemId)
        {
            var rawItemData = await _simcUtilityService.GetRawItemDataAsync(itemId);

            if (rawItemData == null)
            {
                _logger?.LogError($"Unable to find item {itemId}");
                return null;
            }

            // Setup the item
            var item = new SimcItem
            {
                Name = rawItemData.Name,
                ItemId = rawItemData.Id,
                ItemClass = rawItemData.ItemClass,
                ItemSubClass = rawItemData.ItemSubClass,
                InventoryType = rawItemData.InventoryType,
            };

            foreach (var socketColour in rawItemData.SocketColour)
            {
                item.Sockets.Add((ItemSocketColor)socketColour);
            }

            AddItemLevel(item, rawItemData.ItemLevel);
            SetItemQuality(item, rawItemData.Quality);

            return item;
        }

        /// <summary>
        /// Update the item with all the options
        /// </summary>
        /// <param name="item">Base item to update</param>
        /// <param name="bonusIds">Bonus IDs to apply</param>
        /// <param name="gemIds">Gem IDs to apply</param>
        /// <param name="craftedStatIds">Crafted stat IDs to apply</param>
        /// <param name="dropLevel">Character level the item dropped at, used for item level scaling</param>
        /// <param name="contentTuningId">The content_tuning= item option, used for item level scaling</param>
        /// <param name="applyItemLevelSquish">Apply the Midnight item level squish to items without Midnight item level bonuses</param>
        internal async Task UpdateItemAsync(SimcItem item,
            IList<int> bonusIds, IList<int> gemIds, IList<int> craftedStatIds, int dropLevel,
            int contentTuningId = 0, bool applyItemLevelSquish = true)
        {
            var rawItemData = await _simcUtilityService.GetRawItemDataAsync(item.ItemId);

            // Store the original item level before any modifications
            var originalItemLevel = item.ItemLevel;

            // Now add the base mods
            foreach (var mod in rawItemData.ItemMods)
            {
                if (mod.SocketMultiplier > 0)
                    throw new NotImplementedException("Socket Multiplier not yet implemented");
                AddItemMod(item, mod.ModType, mod.StatAllocation);
            }

            var hasMidnightScaling = await ProcessBonusIdsAsync(item, bonusIds, dropLevel, contentTuningId);

            // Starting with the Midnight expansion, item squishes need to be applied to any old item in the game.
            // From item_database::download_item
            if (applyItemLevelSquish && !hasMidnightScaling && !item.ItemLevelForced)
            {
                var squishedItemLevel = RoundItemLevel(await FindCurvePointValueAsync(SquishCurveMidnight, item.ItemLevel));
                _logger?.LogDebug($"[{item.ItemId}] Item {item.Name} squishing ilvl {item.ItemLevel} => {squishedItemLevel}");
                item.ItemLevel = squishedItemLevel;
            }

            foreach (var mod in item.Mods)
            {
                mod.StatRating = await _simcUtilityService.GetScaledModValueAsync(item, mod.Type, mod.RawStatAllocation);
            }

            if (item.ItemLevel != originalItemLevel)
            {
                foreach (var mod in item.Mods)
                {
                    // Use the new overload with current stat value and new item level
                    mod.StatRating = await _simcUtilityService.GetScaledModValueAsync(
                        item,
                        mod.Type,
                        mod.RawStatAllocation,
                        mod.StatRating, // Current calculated value
                        item.ItemLevel  // New item level after bonus processing
                    );
                }
            }

            await AddGemsAsync(item, gemIds);

            ProcessCraftedIds(item, craftedStatIds);
        }

        private void ProcessCraftedIds(SimcItem item, IList<int> craftedStatIds)
        {
            foreach(var mod in item.Mods)
            {
                if(craftedStatIds.Count > 0 && mod.Type == ItemModType.ITEM_MOD_BONUS_STAT_1)
                    mod.Type = (ItemModType)craftedStatIds[0];
                else if (craftedStatIds.Count > 1 && mod.Type == ItemModType.ITEM_MOD_BONUS_STAT_2)
                    mod.Type = (ItemModType)craftedStatIds[1];
            }
        }

        internal async Task UpdateItemEffects(SimcItem item)
        {
            var rawItemData = await _simcUtilityService.GetRawItemDataAsync(item.ItemId);

            foreach (var effect in rawItemData.ItemEffects)
            {
                await AddSpellEffectAsync(item, effect);
            }
        }

        private async Task AddSpellEffectAsync(SimcItem item, SimcRawItemEffect effect)
        {
            // Note: there is a similar process inside this method:
            // double spelleffect_data_t::average( const player_t* p, unsigned level ) const
            // That is done to get scale values for non-item effects based instead on player level
            // This is for things like racial abilities and uses a simpler formula
            // It does use the spell scaling array values, which we already have.

            var effectSpell = await _simcSpellCreationService.GenerateItemSpellAsync(item, effect.SpellId);

            var newEffect = new SimcItemEffect
            {
                EffectId = effect.Id,
                CooldownDuration = effect.CooldownDuration,
                CooldownGroup = effect.CooldownGroup,
                CooldownGroupDuration = effect.CooldownGroupDuration,
                Spell = effectSpell
            };

            item.Effects.Add(newEffect);
        }

        internal async Task AddGemsAsync(SimcItem item, IList<int> gemIds)
        {
            foreach (var gemId in gemIds)
            {
                var gem = await _simcUtilityService.GetRawItemDataAsync((uint)gemId);

                var gemProperty = await _simcUtilityService.GetGemPropertyAsync(gem.GemProperties);

                var enchantmentProperties = await _simcUtilityService.GetItemEnchantmentAsync(gemProperty.EnchantId);

                // Here we can either veer off and grab enchant details from the spell id
                // 1) If there is an enchantmentProperties.spellid
                // 2) otherwise process it with raw item enchantment data

                if (enchantmentProperties.SpellId > 0)
                {
                    throw new NotImplementedException("Enchantments with attached spells not yet implemented.");
                }
                else
                {
                    // 2) raw item enchantment data
                    var scaleIndex = _simcUtilityService.GetClassId((PlayerScaling)enchantmentProperties.ScalingId);

                    // Because the array is zero indexed, take one off the player level
                    // enchant breakdown from item_database::item_enchantment_effect_stats
                    // from dbc_t::spell_scaling
                    // TODO: Pull the players level through to here
                    var scaledValue = await _simcUtilityService.GetSpellScalingMultiplierAsync(scaleIndex, 70);

                    //// Grab the stat this gem increases
                    var stat = (ItemModType)enchantmentProperties.SubEnchantments[0].Property;

                    //// Now add it to the item
                    // Create a new SimcItemGem with the stat, rating etc.
                    var newGem = new SimcItemGem
                    {
                        StatRating = (int)scaledValue,
                        Type = stat
                    };
                    item.Gems.Add(newGem);
                }
            }
        }

        /// <summary>
        /// Tracks state while applying an item's bonus IDs.
        /// From the item_t::parsed fields used by item_database::apply_item_bonus
        /// </summary>
        private class ItemBonusState
        {
            /// <summary>
            /// Set by the Midnight item level bonuses. Stops older item level bonuses applying and
            /// stops the Midnight item level squish being applied to the item afterwards.
            /// </summary>
            public bool HasMidnightScaling { get; set; }
            /// <summary>
            /// Lowest priority ITEM_BONUS_SET_ILEVEL_2 applied so far (lower values win)
            /// </summary>
            public int BaseLevelPriority { get; set; } = int.MaxValue;
            /// <summary>
            /// Item level added by bonuses, kept when ITEM_BONUS_SET_ILEVEL_2 sets the base item level
            /// </summary>
            public int BonusLevel { get; set; }
        }

        /// <summary>
        /// Apply an item's bonus IDs to it
        /// </summary>
        /// <param name="item">Item to update</param>
        /// <param name="bonusIds">Bonus IDs to apply</param>
        /// <param name="dropLevel">Character level the item dropped at, used for item level scaling</param>
        /// <param name="contentTuningId">The content_tuning= item option, used for item level scaling</param>
        /// <returns>True if a Midnight item level bonus set the item's level</returns>
        internal async Task<bool> ProcessBonusIdsAsync(SimcItem item, IList<int> bonusIds, int dropLevel = 0, int contentTuningId = 0)
        {
            // Skip loading the bonus IDs if we have none
            if (bonusIds.Count == 0)
                return false;

            var bonuses = await _cacheService.GetParsedFileContentsAsync<List<SimcRawItemBonus>>(SimcParsedFileType.ItemBonusData);

            var state = new ItemBonusState();

            // Go through each of the bonus IDs on the item, in the order simc applies them
            foreach (var bonusId in SortItemBonuses(bonusIds, bonuses))
            {
                // Find the bonus data for this bonus id
                var bonusEntries = bonuses.Where(b => b.BonusId == bonusId).ToList();

                // Process the bonus data
                foreach (var entry in bonusEntries)
                {
                    await ApplyItemBonusAsync(item, bonusId, entry, bonusIds, bonuses, dropLevel, contentTuningId, state);
                }
            }

            return state.HasMidnightScaling;
        }

        /// <summary>
        /// Sort bonus IDs so item level adjustments are applied after the bonuses that set the item level.
        /// From item_database::sort_item_bonuses
        /// </summary>
        internal static List<int> SortItemBonuses(IList<int> bonusIds, List<SimcRawItemBonus> bonuses)
        {
            // Crafting quality and post-squish item level bonuses go last, everything else is in bonus ID order
            bool IsItemLevelAdjustment(int bonusId) => bonuses.Any(b => b.BonusId == bonusId
                && (b.Type == ItemBonusType.ITEM_BONUS_POST_SQUISH_ITEM_LEVEL || b.Type == ItemBonusType.ITEM_BONUS_CRAFTING_QUALITY));

            return bonusIds
                .OrderBy(IsItemLevelAdjustment)
                .ThenBy(b => b)
                .ToList();
        }

        /// <summary>
        /// Apply a single bonus entry to an item.
        /// From item_database::apply_item_bonus
        /// </summary>
        private async Task ApplyItemBonusAsync(SimcItem item, int bonusId, SimcRawItemBonus entry, IList<int> bonusIds,
            List<SimcRawItemBonus> bonuses, int dropLevel, int contentTuningId, ItemBonusState state)
        {
            switch (entry.Type)
            {
                // Recursively apply another bonus list
                case ItemBonusType.ITEM_BONUS_APPLY_BONUS:
                    _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} applying bonus list {entry.Value1}");
                    foreach (var appliedEntry in bonuses.Where(b => b.BonusId == entry.Value1).ToList())
                    {
                        await ApplyItemBonusAsync(item, entry.Value1, appliedEntry, bonusIds, bonuses, dropLevel, contentTuningId, state);
                    }
                    break;

                // Set item level using a constant and a curve, and then apply any relevant squishes
                case ItemBonusType.ITEM_BONUS_SQUISH_CURVE:
                    {
                        if (SkipForcedItemLevel(item, bonusId, entry))
                            break;

                        var itemLevel = RoundItemLevel(await FindCurvePointValueAsync(entry.Value1, entry.Value2));
                        // simc only applies the Midnight squish here when value 3 is 1
                        if (entry.Value3 == 1)
                            itemLevel = RoundItemLevel(await FindCurvePointValueAsync(SquishCurveMidnight, itemLevel));

                        _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} setting ilvl from squish curve {entry.Value1}: {item.ItemLevel} => {itemLevel}");
                        item.ItemLevel = itemLevel;
                        state.HasMidnightScaling = true;
                        break;
                    }

                // Set item level from an item scaling config and its item offset curve
                case ItemBonusType.ITEM_BONUS_SCALE_CONFIG:
                    {
                        if (SkipForcedItemLevel(item, bonusId, entry))
                            break;

                        var (scalingConfig, offsetCurve) = await GetItemScalingConfigAsync(entry.Value1);
                        if (scalingConfig == null || offsetCurve == null)
                        {
                            _logger?.LogWarning($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} unable to find item scaling config {entry.Value1}");
                            break;
                        }

                        // Configs from before the Midnight squish era are squished, newer ones use the offset curve
                        var curveId = scalingConfig.SquishEraId < 2 ? SquishCurveMidnight : offsetCurve.CurveId;
                        var itemLevel = RoundItemLevel(await FindCurvePointValueAsync(curveId, scalingConfig.ItemLevel)) + offsetCurve.Offset;

                        _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} setting ilvl from scaling config {entry.Value1}: {item.ItemLevel} => {itemLevel}");
                        item.ItemLevel = itemLevel;
                        state.HasMidnightScaling = true;
                        break;
                    }

                // Set item level from an item scaling config's offset curve, using the drop level if there is one
                case ItemBonusType.ITEM_BONUS_SCALE_CONFIG_2:
                    {
                        if (SkipForcedItemLevel(item, bonusId, entry))
                            break;

                        var (scalingConfig, offsetCurve) = await GetItemScalingConfigAsync(entry.Value1);
                        if (scalingConfig == null || offsetCurve == null)
                        {
                            _logger?.LogWarning($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} unable to find item scaling config {entry.Value1}");
                            break;
                        }

                        int itemLevel;
                        if (dropLevel > 0)
                        {
                            // The content tuning caps the drop level used
                            var level = dropLevel;
                            var contentTuning = await GetContentTuningAsync(contentTuningId);
                            if (contentTuning != null && contentTuning.MaxLevelSquish > 0 && level > contentTuning.MaxLevelSquish)
                                level = contentTuning.MaxLevelSquish;

                            itemLevel = RoundItemLevel(await FindCurvePointValueAsync(offsetCurve.CurveId, level));
                        }
                        else
                            itemLevel = RoundItemLevel(await FindCurvePointValueAsync(offsetCurve.CurveId, scalingConfig.ItemLevel));

                        itemLevel += offsetCurve.Offset;

                        _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} setting ilvl from scaling config {entry.Value1}: {item.ItemLevel} => {itemLevel}");
                        item.ItemLevel = itemLevel;
                        state.HasMidnightScaling = true;
                        break;
                    }

                // Adjust item level, value 1 is the item level and value 3 is the crafting quality
                case ItemBonusType.ITEM_BONUS_CRAFTING_QUALITY:
                    if (SkipForcedItemLevel(item, bonusId, entry))
                        break;

                    _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} adding {entry.Value1} crafting quality ilvl to {item.ItemLevel} => {item.ItemLevel + entry.Value1}");
                    state.BonusLevel += entry.Value1;
                    AddItemLevel(item, entry.Value1);
                    break;

                // Adjust item level, value 1 is the item level. Only applies to items with Midnight scaling
                case ItemBonusType.ITEM_BONUS_POST_SQUISH_ITEM_LEVEL:
                    if (!state.HasMidnightScaling || SkipForcedItemLevel(item, bonusId, entry))
                        break;

                    _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} adding {entry.Value1} ilvl to {item.ItemLevel} => {item.ItemLevel + entry.Value1}");
                    state.BonusLevel += entry.Value1;
                    AddItemLevel(item, entry.Value1);
                    break;

                case ItemBonusType.ITEM_BONUS_ILEVEL:
                    if(item.ItemLevelForced)
                    {
                        _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} SKIPPING adding {entry.Value1} " +
                            $"ilvl to {item.ItemLevel} => {item.ItemLevel + entry.Value1} due to item level being forced through ilevel=");
                        break;
                    }
                    // Starting with Midnight, the item level bonuses above stop other item level adjustments applying.
                    // simc also skips them when bonus IDs 7215 or 7250 are present.
                    if (state.HasMidnightScaling || bonusIds.Contains(7215) || bonusIds.Contains(7250))
                    {
                        _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} SKIPPING adding {entry.Value1} " +
                            $"ilvl to {item.ItemLevel} => {item.ItemLevel + entry.Value1} due to Midnight item level scaling or bonus IDs 7215/7250");
                        break;
                    }
                    if (bonusIds.Contains(6652))
                    {
                        _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} SKIPPING adding {entry.Value1} " +
                            $"ilvl to {item.ItemLevel} => {item.ItemLevel + entry.Value1} due to presence of bonusId 6652 (bug #68.)");
                        //break; // Bug fix for Simc#5490 (#68) - invalid item scaling on unnatural items
                    }

                    _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} adding {entry.Value1} ilvl to {item.ItemLevel} => {item.ItemLevel + entry.Value1}");
                    state.BonusLevel += entry.Value1;
                    AddItemLevel(item, entry.Value1);
                    break;

                // Set the base item level, value 1 is the item level and value 2 is the priority (lowest wins)
                case ItemBonusType.ITEM_BONUS_SET_ILEVEL_2:
                    if (SkipForcedItemLevel(item, bonusId, entry))
                        break;
                    if (state.HasMidnightScaling || bonusIds.Contains(7215) || bonusIds.Contains(7250))
                        break;

                    if (entry.Value2 < state.BaseLevelPriority)
                    {
                        _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} setting base ilvl to {entry.Value1}: {item.ItemLevel} => {entry.Value1 + state.BonusLevel}");
                        item.ItemLevel = entry.Value1 + state.BonusLevel;
                        state.BaseLevelPriority = entry.Value2;
                    }
                    break;

                case ItemBonusType.ITEM_BONUS_MOD:
                    _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} adding {entry.Value1} with more alloc: {entry.Value2}");
                    AddItemMod(item, (ItemModType)entry.Value1, entry.Value2);
                    break;

                case ItemBonusType.ITEM_BONUS_QUALITY:
                    _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} adjusting quality from {item.Quality} to {(ItemQuality)entry.Value1} ({entry.Value1})");
                    SetItemQuality(item, (ItemQuality)entry.Value1);
                    break;

                case ItemBonusType.ITEM_BONUS_SOCKET:
                    _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} adding {entry.Value1} sockets of type {entry.Value2}");
                    AddItemSockets(item, entry.Value1, (ItemSocketColor)entry.Value2);
                    break;

                case ItemBonusType.ITEM_BONUS_ADD_ITEM_EFFECT:
                    _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} adding effect {entry.Value1}");
                    await AddItemEffect(item, entry.Value1);
                    break;

                case ItemBonusType.ITEM_BONUS_SCALING_2:
                    if (item.ItemLevelForced)
                    {
                        _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} SKIPPING adding {entry.Value1} " +
                            $"ilvl to {item.ItemLevel} => {item.ItemLevel + entry.Value1} due to item level being forced through ilevel=");
                        break;
                    }
                    _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} adding item scaling {entry.Value4} from {dropLevel}");
                    await AddBonusScalingAsync(item, entry.Value4, dropLevel);
                    break;

                // Unused bonustypes:
                case ItemBonusType.ITEM_BONUS_DESC:
                case ItemBonusType.ITEM_BONUS_SUFFIX:
                    break;

                default:
                    var entryString = JsonSerializer.Serialize(entry);
                    _logger?.LogTrace($"[{item.ItemId}] [{bonusId}:{entry.Type}] Unknown bonus entry: {entry.Type} ({bonusId}): {entryString}");
                    break;
            }
        }

        /// <summary>
        /// Item level bonuses don't apply when the item level is forced through ilevel=
        /// </summary>
        private bool SkipForcedItemLevel(SimcItem item, int bonusId, SimcRawItemBonus entry)
        {
            if (!item.ItemLevelForced)
                return false;

            _logger?.LogDebug($"[{item.ItemId}] [{bonusId}:{entry.Type}] Item {item.Name} SKIPPING item level bonus " +
                $"due to item level being forced through ilevel=");
            return true;
        }

        /// <summary>
        /// Get an item scaling config and the item offset curve it uses.
        /// From item_scaling_config_data_t::find and item_offset_curve_data_t::find
        /// </summary>
        internal async Task<(SimcRawItemScalingConfig ScalingConfig, SimcRawItemOffsetCurve OffsetCurve)> GetItemScalingConfigAsync(int scalingConfigId)
        {
            var scalingConfigs = await _cacheService.GetParsedFileContentsAsync<List<SimcRawItemScalingConfig>>(SimcParsedFileType.ItemScalingConfigData);
            var scalingConfig = scalingConfigs.FirstOrDefault(c => c.Id == scalingConfigId);
            if (scalingConfig == null)
                return (null, null);

            var offsetCurves = await _cacheService.GetParsedFileContentsAsync<List<SimcRawItemOffsetCurve>>(SimcParsedFileType.ItemOffsetCurveData);
            var offsetCurve = offsetCurves.FirstOrDefault(c => c.Id == scalingConfig.ItemOffsetCurveId);

            return (scalingConfig, offsetCurve);
        }

        /// <summary>
        /// Get a content tuning entry, or null if there isn't one.
        /// From content_tuning_data_t::find
        /// </summary>
        internal async Task<SimcRawContentTuning> GetContentTuningAsync(int contentTuningId)
        {
            if (contentTuningId <= 0)
                return null;

            var contentTuning = await _cacheService.GetParsedFileContentsAsync<List<SimcRawContentTuning>>(SimcParsedFileType.ContentTuningData);

            return contentTuning.FirstOrDefault(c => c.Id == contentTuningId);
        }

        /// <summary>
        /// Round an item level calculated from a curve. From util::round
        /// </summary>
        internal static int RoundItemLevel(double itemLevel) => (int)Math.Round(itemLevel, MidpointRounding.AwayFromZero);

        internal async Task AddBonusScalingAsync(SimcItem item, int curveId, int dropLevel)
        {
            // from item_database::apply_item_scaling (sc_item_data.cpp)
            if (curveId == 0)
                return;

            // Then the base_value becomes MIN(player_level, curveData.last().primary1);
            var curveData = await FindCurvePointByIdAsync(curveId);
            if (curveData.Count == 0)
                return;

            var baseValue = Math.Min(dropLevel, curveData.LastOrDefault().Primary1);

            double scaledResult = await FindCurvePointValueAsync(curveId, baseValue);

            int newItemLevel = (int)Math.Floor(scaledResult + 0.5);

            _logger?.LogDebug($"[{item.ItemId}] Item {item.Name} setting scaled item level to {newItemLevel}");

            item.ItemLevel = newItemLevel;
        }

        internal async Task<List<SimcRawCurvePoint>> FindCurvePointByIdAsync(int curveId)
        {
            // from curve_point_t::find
            List<SimcRawCurvePoint> points = new List<SimcRawCurvePoint>();

            var curveData = await _cacheService.GetParsedFileContentsAsync<List<SimcRawCurvePoint>>(SimcParsedFileType.CurvePoints);

            var curvePoints = curveData.Where(c => c.CurveId == curveId).ToList();

            return curvePoints;
        }

        internal async Task<double> FindCurvePointValueAsync(int curveId, double pointValue)
        {
            // From item_database::curve_point_value
            // First call dbc_t::curve_point and get the two curve points either side of the value
            var curvePoints = await FindCurvePointByIdAsync(curveId);

            if (curvePoints.Count == 0)
            {
                _logger?.LogWarning($"Unable to find curve {curveId}");
                return 0;
            }

            // The one right below the value, and the one right above it. Either can be missing if the value is
            // outside the curve, in which case dbc_t::curve_point uses the other one for both
            var curvePointFirst = curvePoints
                .Where(c => c.Primary1 <= pointValue)
                .OrderBy(c => c.Index)
                .LastOrDefault();
            var curvePointSecond = curvePoints
                .Where(c => c.Primary1 >= pointValue)
                .OrderBy(c => c.Index)
                .FirstOrDefault();

            curvePointFirst ??= curvePointSecond;
            curvePointSecond ??= curvePointFirst;

            // Now back to item_database::curve_point_value
            double scaledResult;
            // Lands on a value, use data
            if (curvePointFirst.Primary1 == pointValue)
                scaledResult = curvePointFirst.Primary2;
            // Below lower bound, use lower bound value
            else if (curvePointFirst.Primary1 > pointValue)
                scaledResult = curvePointFirst.Primary2;
            // Above upper bound, use upper bound value
            else if (curvePointSecond.Primary1 < pointValue)
                scaledResult = curvePointSecond.Primary2;
            else if (curvePointSecond.Primary1 == pointValue)
                throw new ArgumentOutOfRangeException("Incorrect curve data retreived");
            // Linear interpolation
            else
                scaledResult = curvePointFirst.Primary2 + (curvePointSecond.Primary2 - curvePointFirst.Primary2) *
                    (pointValue - curvePointFirst.Primary1) / (curvePointSecond.Primary1 - curvePointFirst.Primary1);

            return scaledResult;
        }

        internal void AddItemMod(SimcItem item, ItemModType modType, int statAllocation)
        {
            var newMod = new SimcItemMod
            {
                Type = modType,
                RawStatAllocation = statAllocation
            };

            item.Mods.Add(newMod);
        }

        internal void AddItemSockets(SimcItem item, int numSockets, ItemSocketColor socketColor)
        {
            // Based on item_database::apply_item_bonus case ITEM_BONUS_SOCKET:
            // This original method just adds colours to existing sockets.
            var addedSockets = 0;
            // Basically just loop through each of the sockets on the item and attempt to add colours
            for (var i = 0; i < item.Sockets.Count && addedSockets < numSockets; i++)
            {
                // Can't add colours if there are no uncoloured sockets left
                if (item.Sockets.Where(s => s == ItemSocketColor.SOCKET_COLOR_NONE).Count() > 0)
                {
                    var socket = item.Sockets.Where(s => s == ItemSocketColor.SOCKET_COLOR_NONE).FirstOrDefault();
                    socket = socketColor;
                    addedSockets++;
                }
                else
                    _logger?.LogError($"Trying to apply a colour to a socket, but no sockets left.");
            }
        }

        internal void SetItemQuality(SimcItem item, ItemQuality newQuality)
        {
            item.Quality = newQuality;
        }

        internal void AddItemLevel(SimcItem item, int newItemLevel)
        {
            item.ItemLevel += newItemLevel;
        }

        internal async Task AddItemEffect(SimcItem item, int effectId)
        {
            // Try to add effect
            var itemEffect = await _simcUtilityService.GetItemEffectAsync((uint)effectId);

            if (itemEffect == null)
                _logger?.LogError($"No item effect found when adding {effectId} to {item.ItemId}");

            _logger?.LogError($"Adding item effect {effectId} to {item.ItemId} (SpellId: {itemEffect.SpellId})");
            await AddSpellEffectAsync(item, itemEffect);
        }
    }
}
