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
            var rawItemData = _simcUtilityService.GetRawItemData(parsedItemData.ItemId);

            if (rawItemData == null)
            {
                _logger?.LogError($"Unable to find item {parsedItemData.ItemId}");
                return null;
            }

            // Setup the item
            var item = new SimcItem
            {
                Name = rawItemData.Name,
                ItemId = rawItemData.Id,
                ItemClass = rawItemData.ItemClass,
                ItemSubClass = rawItemData.ItemSubClass,
                InventoryType = rawItemData.InventoryType
            };

            foreach (var socketColour in rawItemData.SocketColour)
            {
                item.Sockets.Add((ItemSocketColor)socketColour);
            }

            AddItemLevel(item, rawItemData.ItemLevel);
            SetItemQuality(item, rawItemData.Quality);

            // Now add the base mods
            foreach (var mod in rawItemData.ItemMods)
            {
                if (mod.SocketMultiplier > 0)
                    throw new NotImplementedException("Socket Multiplier not yet implemented");
                AddItemMod(item, mod.ModType, mod.StatAllocation);
            }

            ProcessBonusIds(item, parsedItemData.BonusIds);

            foreach (var mod in item.Mods)
            {
                mod.StatRating = _simcUtilityService.GetScaledModValue(item, mod.Type, mod.RawStatAllocation);
            }

            AddGems(item, parsedItemData.GemIds);

            AddSpellEffects(item, rawItemData.ItemEffects);

            return item;
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

        internal void AddGems(SimcItem item, IReadOnlyCollection<int> gemIds)
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

        internal void ProcessBonusIds(SimcItem item, IReadOnlyCollection<int> bonusIds)
        {
            var bonuses = _cacheService.GetParsedFileContents<List<SimcRawItemBonus>>(SimcParsedFileType.ItemBonusData);

            // Go through each of the bonus IDs on the item
            foreach (var bonusId in bonusIds)
            {
                // Find the bonus data for this bonus id
                var bonusEntries = bonuses.Where(b => b.BonusId == bonusId);

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
