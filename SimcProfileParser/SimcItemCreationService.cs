using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model;
using SimcProfileParser.Model.DataSync;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.Profile;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimcProfileParser
{
    internal class SimcItemCreationService : ISimcItemCreationService
    {
        private readonly ICacheService _cacheService;
        private readonly ISimcSpellCreationService _simcSpellCreationService;
        private readonly ISimcUtilityService _simcUtilityService;
        private readonly ILogger<SimcItemCreationService> _logger;

        internal SimcItemCreationService(ICacheService cacheService,
            ISimcSpellCreationService simcSpellCreationService,
            ISimcUtilityService simcUtilityService,
            ILogger<SimcItemCreationService> logger)
        {
            _cacheService = cacheService;
            _simcSpellCreationService = simcSpellCreationService;
            _simcUtilityService = simcUtilityService;
            _logger = logger;
        }

        SimcItem ISimcItemCreationService.CreateItem(SimcParsedItem parsedItemData)
        {
            var item = BuildItem(parsedItemData.ItemId);

            if (item == null)
                throw new ArgumentOutOfRangeException(
                    nameof(parsedItemData.ItemId), $"ItemId not found: {parsedItemData.ItemId}");

            UpdateItem(item,
                parsedItemData.BonusIds.ToList(),
                parsedItemData.GemIds.ToList(),
                parsedItemData.DropLevel);

            return item;
        }

        SimcItem ISimcItemCreationService.CreateItem(SimcItemOptions itemOptions)
        {
            var item = BuildItem(itemOptions.ItemId);

            if (item == null)
                throw new ArgumentOutOfRangeException(
                    nameof(itemOptions.ItemId), $"ItemId not found: {itemOptions.ItemId}");

            UpdateItem(item,
                itemOptions.BonusIds,
                itemOptions.GemIds,
                itemOptions.DropLevel);

            // Set the item level if provided
            if(itemOptions.ItemLevel > 0)
                AddItemLevel(item, itemOptions.ItemLevel - item.ItemLevel);

            // Set the quality if provided
            if(itemOptions.Quality != ItemQuality.ITEM_QUALITY_NONE)
                SetItemQuality(item, itemOptions.Quality);

            return item;
        }

        /// <summary>
        /// Setup a basic item
        /// </summary>
        /// <param name="itemId">The ID of the item</param>
        internal SimcItem BuildItem(uint itemId)
        {
            var rawItemData = _simcUtilityService.GetRawItemData(itemId);

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
        internal void UpdateItem(SimcItem item, 
            IList<int> bonusIds, IList<int> gemIds, int dropLevel)
        {
            var rawItemData = _simcUtilityService.GetRawItemData(item.ItemId);

            // Now add the base mods
            foreach (var mod in rawItemData.ItemMods)
            {
                if (mod.SocketMultiplier > 0)
                    throw new NotImplementedException("Socket Multiplier not yet implemented");
                AddItemMod(item, mod.ModType, mod.StatAllocation);
            }

            ProcessBonusIds(item, bonusIds, dropLevel);

            foreach (var mod in item.Mods)
            {
                mod.StatRating = _simcUtilityService.GetScaledModValue(item, mod.Type, mod.RawStatAllocation);
            }

            AddGems(item, gemIds);

            AddSpellEffects(item, rawItemData.ItemEffects);
        }

        private void AddSpellEffects(SimcItem item, List<SimcRawItemEffect> itemEffects)
        {
            // Note: there is a similar process inside this method:
            // double spelleffect_data_t::average( const player_t* p, unsigned level ) const
            // That is done to get scale values for non-item effects based instead on player level
            // This is for things like racial abilities and uses a simpler formula
            // It does use the spell scaling array values, which we already have.

            foreach (var effect in itemEffects)
            {
                var effectSpell = _simcSpellCreationService.GenerateItemSpell(item, effect.SpellId);

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
        }

        internal void AddGems(SimcItem item, IList<int> gemIds)
        {
            foreach (var gemId in gemIds)
            {
                var gem = _simcUtilityService.GetRawItemData((uint)gemId);

                var gemProperty = _simcUtilityService.GetGemProperty(gem.GemProperties);

                var enchantmentProperties = _simcUtilityService.GetItemEnchantment(gemProperty.EnchantId);

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
                    var scaledValue = _simcUtilityService.GetSpellScalingMultiplier(scaleIndex, 60);

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

        internal void ProcessBonusIds(SimcItem item, IList<int> bonusIds, int dropLevel = 0)
        {
            var bonuses = _cacheService.GetParsedFileContents<List<SimcRawItemBonus>>(SimcParsedFileType.ItemBonusData);

            // Go through each of the bonus IDs on the item
            foreach (var bonusId in bonusIds)
            {
                // Find the bonus data for this bonus id
                var bonusEntries = bonuses.Where(b => b.BonusId == bonusId).ToList();

                if (bonusEntries == null)
                    continue;

                // Process the bonus data
                foreach (var entry in bonusEntries)
                {
                    // From item_database::apply_item_bonus
                    switch (entry.Type)
                    {
                        case ItemBonusType.ITEM_BONUS_ILEVEL:
                            _logger?.LogDebug($"[{item.ItemId}] Item {item.Name} adding {entry.Value1} ilvl to {item.ItemLevel} => {item.ItemLevel + entry.Value1}");
                            AddItemLevel(item, entry.Value1);
                            break;

                        case ItemBonusType.ITEM_BONUS_MOD:
                            _logger?.LogDebug($"[{item.ItemId}] Item {item.Name} adding {entry.Value1} with more alloc: {entry.Value2}");
                            AddItemMod(item, (ItemModType)entry.Value1, entry.Value2);
                            break;

                        case ItemBonusType.ITEM_BONUS_QUALITY:
                            _logger?.LogDebug($"[{item.ItemId}] Item {item.Name} adjusting quality from {item.Quality} to {(ItemQuality)entry.Value1} ({entry.Value1})");
                            SetItemQuality(item, (ItemQuality)entry.Value1);
                            break;

                        case ItemBonusType.ITEM_BONUS_SOCKET:
                            _logger?.LogDebug($"[{item.ItemId}] Item {item.Name} adding {entry.Value1} sockets of type {entry.Value2}");
                            AddItemSockets(item, entry.Value1, (ItemSocketColor)entry.Value2);
                            break;

                        case ItemBonusType.ITEM_BONUS_ADD_ITEM_EFFECT:
                            _logger?.LogDebug($"[{item.ItemId}] Item {item.Name} adding effect {entry.Value1}");
                            AddItemEffect(item, entry.Value1);
                            break;

                        case ItemBonusType.ITEM_BONUS_SCALING_2:
                            _logger?.LogDebug($"[{item.ItemId}] Item {item.Name} adding item scaling {entry.Value4} from {dropLevel}");
                            AddBonusScaling(item, entry.Value4, dropLevel);
                            break;

                        // Unused bonustypes:
                        case ItemBonusType.ITEM_BONUS_DESC:
                        case ItemBonusType.ITEM_BONUS_SUFFIX:
                            break;

                        default:
                            var entryString = JsonConvert.SerializeObject(entry);
                            _logger?.LogTrace($"[{item.ItemId}] Unknown bonus entry: {entry.Type} ({bonusId}): {entryString}");
                            break;
                    }
                }
            }
        }

        internal void AddBonusScaling(SimcItem item, int curveId, int dropLevel)
        {
            // from item_database::apply_item_scaling (sc_item_data.cpp)
            if (curveId == 0)
                return;

            // Then the base_value becomes MIN(player_level, curveData.last().primary1);
            var curveData = FindCurvePointById(curveId);
            if (curveData.Count == 0)
                return;

            var baseValue = Math.Min(dropLevel, curveData.LastOrDefault().Primary1);

            double scaledResult = FindCurvePointValue(curveId, baseValue);

            int newItemLevel =  (int)Math.Floor(scaledResult + 0.5);

            _logger?.LogDebug($"[{item.ItemId}] Item {item.Name} setting scaled item level to {newItemLevel}");

            item.ItemLevel = newItemLevel;
        }

        internal List<SimcRawCurvePoint> FindCurvePointById(int curveId)
        {
            // from curve_point_t::find
            List<SimcRawCurvePoint> points = new List<SimcRawCurvePoint>();

            var curveData = _cacheService.GetParsedFileContents<List<SimcRawCurvePoint>>(SimcParsedFileType.CurvePoints);

            var curvePoints = curveData.Where(c => c.CurveId == curveId).ToList();

            return curvePoints;
        }

        internal double FindCurvePointValue(int curveId, double pointValue)
        {
            // From item_database::curve_point_value
            // First call dbc_t::curve_point and get the two curve points either side of the value
            var curveData = _cacheService.GetParsedFileContents<List<SimcRawCurvePoint>>(SimcParsedFileType.CurvePoints);

            var curvePoints = curveData
                .Where(c => c.CurveId == curveId)
                .ToList();

            // The one right below the value
            var curvePointFirst = curvePoints
                .Where(c => c.Primary1 <= pointValue)
                .OrderBy(c => c.Primary1)
                .Last();
            // The one right above the value
            var curvePointSecond = curvePoints
                .Where(c => c.Primary1 >= pointValue)
                .OrderBy(c => c.Primary1)
                .First();

            if (curvePointFirst == null)
                curvePointFirst = curvePointSecond;

            if (curvePointSecond == null)
                curvePointSecond = curvePointFirst;

            // Now back to item_database::curve_point_value
            double scaledResult = 0;
            // item_database::curve_point_value seems to get the primary 2 value from whichever of the relevant curve points
            if (curvePointFirst.Primary1 == pointValue)
                scaledResult = curvePointFirst.Primary2;
            else if (curvePointFirst.Primary1 > pointValue)
                scaledResult = curvePointFirst.Primary2;
            else if (curvePointSecond.Primary1 < pointValue)
                scaledResult = curvePointSecond.Primary2;
            else if (curvePointSecond.Primary1 == pointValue)
                throw new ArgumentOutOfRangeException("Incorrect curve data retreived");
            else
                scaledResult = curvePointFirst.Primary2 + (curvePointSecond.Primary2 - curvePointFirst.Primary2) *
                    (scaledResult - curvePointFirst.Primary1) / (curvePointSecond.Primary1 - curvePointFirst.Primary1);

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

        internal void AddItemEffect(SimcItem item, int effectId)
        {
            _logger?.LogError($"No item effect found when adding {effectId} to {item.ItemId}");
        }
    }
}
