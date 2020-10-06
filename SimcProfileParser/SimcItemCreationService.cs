using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model;
using SimcProfileParser.Model.DataSync;
using SimcProfileParser.Model.Profile;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            foreach(var mod in item.Mods)
            {
                mod.StatRating = GetScaledModValue(item, mod.Type, mod.RawStatAllocation);
            }

            return item;
        }

        internal void ProcessBonusIds(SimcItem item, IReadOnlyCollection<int> bonusIds)
        {
            var bonuses = _cacheService.GetParsedFileContents<List<SimcRawItemBonus>>(Model.DataSync.SimcParsedFileType.ItemBonusData);

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

        internal int GetScaledModValue(SimcItem item, ItemModType modType, int statAllocation)
        {
            // based on item_database::scaled_stat
            var slotType = GetSlotType(item.ItemClass, item.ItemSubClass, item.InventoryType);

            var itemBudget = 0.0d;

            // If the item has a slot and quality we can parse
            if(slotType != -1 && item.Quality > 0)
            {
                var ilvlRandomProps = GetRandomProps(item.ItemLevel);
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
            if(statAllocation > 0 && itemBudget > 0)
            {
                // Not yet implemented
                var socketPenalty = 0.0d;
                int rawValue = (int)(statAllocation * itemBudget * 0.0001d - socketPenalty + 0.5d);

                if(GetIsCombatRating(modType))
                {
                    // based on item_database::apply_combat_rating_multiplier
                    var combatRatingType = GetCombatRatingMultiplierType(item.InventoryType);
                    if(combatRatingType != CombatRatingMultiplayerType.CR_MULTIPLIER_INVALID)
                    {
                        var combatRatingMultiplier = GetCombatRatingMultiplier(item.ItemLevel, combatRatingType);
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
                        var staminaRatingMultiplier = GetStaminaMultiplier(item.ItemLevel, staminaRatingType);
                        if (staminaRatingMultiplier != 0)
                            rawValue = (int)(rawValue * staminaRatingMultiplier);
                    }
                }

                return rawValue;
            }

            throw new NotImplementedException("Items and mods that don't scale are not yet implemented");
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

        internal int GetSlotType(ItemClass itemClass, int itemSubClass, InventoryType itemInventoryType)
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

        internal bool GetIsCombatRating(ItemModType modType)
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

        internal CombatRatingMultiplayerType GetCombatRatingMultiplierType(InventoryType inventoryType)
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

        // TODO This is a hot mess. Need a service to retrieve data from these generated files.
        internal SimcRawItem GetRawItemData(uint itemId)
        {
            var rawItem = new SimcRawItem();

            var items = _cacheService.GetParsedFileContents<List<SimcRawItem>>(SimcParsedFileType.ItemDataNew);

            rawItem = items.Where(i => i.Id == itemId).FirstOrDefault();

            if(rawItem == null)
            {
                // If we can't find it in the new data, try all the older items
                items = _cacheService.GetParsedFileContents<List<SimcRawItem>>(SimcParsedFileType.ItemDataOld);
                rawItem = items.Where(i => i.Id == itemId).FirstOrDefault();
            }

            return rawItem;
        }

        // TODO This is a hot mess. Need a service to retrieve data from these generated files.
        internal SimcRawRandomPropData GetRandomProps(int itemLevel)
        {
            var rawProps = new SimcRawRandomPropData();

            var randomProps = _cacheService.GetParsedFileContents<List<SimcRawRandomPropData>>(SimcParsedFileType.RandomPropPoints);

            rawProps = randomProps.Where(p => p.ItemLevel == itemLevel).FirstOrDefault();

            return rawProps;
        }

        internal double GetCombatRatingMultiplier(int itemLevel, CombatRatingMultiplayerType combatRatingType)
        {
            var crMultipliers = _cacheService.GetParsedFileContents<float[][]>(SimcParsedFileType.CombatRatingMultipliers);

            var crMulti = crMultipliers[(int)combatRatingType][itemLevel - 1];

            return crMulti;
        }

        private double GetStaminaMultiplier(int itemLevel, CombatRatingMultiplayerType staminaRatingType)
        {
            var stamMultipliers = _cacheService.GetParsedFileContents<float[][]>(SimcParsedFileType.StaminaMultipliers);

            var stamMulti = stamMultipliers[(int)staminaRatingType][itemLevel - 1];

            return stamMulti;
        }
    }
}
