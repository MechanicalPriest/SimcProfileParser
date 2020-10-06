using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model;
using SimcProfileParser.Model.Profile;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SimcProfileParser
{
    public class SimcItemCreationService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger _logger;

        public SimcItemCreationService(ICacheService cacheService,
            ILogger logger = null)
        {
            _cacheService = cacheService;
            _logger = logger;

            _cacheService.RegisterFileConfiguration(Model.DataSync.SimcFileType.ItemDataInc,
                "ItemData.raw",
                "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/item_data.inc"
                );
        }

        internal object CreateItemsFromProfile(SimcParsedProfile parsedProfile)
        {
            var items = new List<SimcItem>();

            foreach(var parsedItemData in parsedProfile.Items)
            {
                var item = CreateItem(parsedItemData);
                items.Add(item);
            }

            return items;
        }

        internal SimcItem CreateItem(SimcParsedItem parsedItemData)
        {
            var rawItemData = GetRawItemData(parsedItemData.ItemId);

            // Setup the item
            var item = new SimcItem
            {
                Name = rawItemData.Name,
                ItemId = rawItemData.Id
            };

            foreach(var socketColour in rawItemData.SocketColour)
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

            return item;
        }

        internal void ProcessBonusIds(SimcItem item, IReadOnlyCollection<int> bonusIds)
        {
            var bonusData = File.ReadAllText(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "SimcProfileParserData",
                    "ItemBonusData.json")
                );

            var bonuses = JsonConvert.DeserializeObject<List<SimcRawItemBonus>>(bonusData);

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
            var newMod = new SimcItemMod();

            newMod.Type = modType;
            newMod.RawStatAllocation += statAllocation;

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
            item.ItemLevel = newItemLevel;
        }

        internal void AddItemEffect(SimcItem item, int effectId)
        {
            _logger?.LogError($"No item effect found when adding {effectId} to {item.ItemId}");
        }

        // TODO This is a hot mess. Need a service to retrieve data from these generated files.
        internal SimcRawItem GetRawItemData(uint itemId)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var rawItem = new SimcRawItem();

            var itemData = File.ReadAllText(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "SimcProfileParserData",
                    "ItemData.json")
                );

            var items = JsonConvert.DeserializeObject<List<SimcRawItem>>(itemData);

            rawItem = items.Where(i => i.Id == itemId).FirstOrDefault();

            sw.Stop();
            _logger?.LogDebug($"Loading ItemData.json took {sw.ElapsedMilliseconds}ms ({items.Count} items)");

            return rawItem;
        }
    }
}
