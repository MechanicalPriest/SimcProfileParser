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

            AddGems(item, parsedItemData.GemIds);

            AddSpellEffects(item, rawItemData.ItemEffects);

            return item;
        }

        private void AddSpellEffects(SimcItem item, List<SimcRawItemEffect> itemEffects)
        {
            
            // From double spelleffect_data_t::average( const item_t* item )
            // Get the item budget from item_database::item_budget
            // For this we need the item with appropriate item level
            // and we need the spells maximum scaling level
            // Then we use the GetScaledModValue() method

            // Now we update get the budget multiplier using the spells scaling class

            // If the scaling is -7, apply combat rating multiplier to it
            // This is done using GetCombatRatingMultiplier() and setting the budget to it

            // If the scaling is -8, get the props for the items ilvl
            // set the buget to tbe the props damage replace stat value ... ???

            // If the scaling is PLAYER_NONE but the spells flags contains 
            // the spell_attribute::SX_SCALE_ILEVEL value (354U)
            // Then get random props again and use the damage_secondary property... ???

            // Otherwise just use the original budget.

            // Finally multiply the coefficient of the spell effect against the budget.

            // Note: there is a similar process inside this method:
            // double spelleffect_data_t::average( const player_t* p, unsigned level ) const
            // That is done to get scale values for non-item effects based instead on player level
            // This is for things like racial abilities and uses a simpler formula
            // It does use the spell scaling array values, which we already have.

            foreach (var effect in itemEffects)
            {
                // TODO: Factor in the level scaling caps from item_database::item_budget
                var spell = GetSpellData(effect.SpellId);

                var budget = GetItemBudget(item, spell.MaxScalingLevel);

                var spellScalingClass = GetScaleClass(spell.ScalingType);

                if(spellScalingClass == PlayerScaling.PLAYER_SPECIAL_SCALE7)
                {
                    var combatRatingType = GetCombatRatingMultiplierType(item.InventoryType);
                    var multi = GetCombatRatingMultiplier(item.ItemLevel, combatRatingType);
                    budget *= multi;
                }
                else if (spellScalingClass == PlayerScaling.PLAYER_SPECIAL_SCALE8)
                {
                    var props = GetRandomProps(item.ItemLevel);
                    budget = props.DamageReplaceStat;
                }
                else if (spellScalingClass == PlayerScaling.PLAYER_NONE)
                {
                    // This is from spelleffect_data_t::average's call to _spell->flags( spell_attribute::SX_SCALE_ILEVEL )
                    throw new NotImplementedException("ilvl scaling from spell flags not yet implemented.");
                }

                var effectSpell = new SimcSpell()
                {
                    SpellId = spell.Id,
                    Name = spell.Name,
                    School = spell.School,
                    ScalingType = spell.ScalingType,
                    MinRange = spell.MinRange,
                    MaxRange = spell.MaxRange,
                    Cooldown = spell.Cooldown,
                    Gcd = spell.Gcd,
                    Category = spell.Category,
                    CategoryCooldown = spell.CategoryCooldown,
                    Charges = spell.Charges,
                    ChargeCooldown = spell.ChargeCooldown,
                    MaxTargets = spell.MaxTargets,
                    Duration = spell.Duration,
                    MaxStacks = spell.MaxStack,
                    ProcChance = spell.ProcChance,
                    InternalCooldown = spell.InternalCooldown,
                    Rppm = spell.Rppm,
                    CastTime = spell.CastTime,
                    ItemScaleBudget = budget,
                };

                foreach(var spellEffect in spell.Effects)
                {
                    effectSpell.Effects.Add(new SimcSpellEffect()
                    {
                        Id = spellEffect.Id,
                        EffectIndex = spellEffect.EffectIndex,
                        EffectType = spellEffect.EffectType,
                        EffectSubType = spellEffect.EffectSubType,
                        Coefficient = spellEffect.Coefficient,
                        SpCoefficient = spellEffect.SpCoefficient,
                        Delta = spellEffect.Delta,
                        Amplitude = spellEffect.Amplitude,
                        Radius = spellEffect.Radius,
                        RadiusMax = spellEffect.RadiusMax,
                        BaseValue = spellEffect.BaseValue,
                    });
                }

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
                var gem = GetRawItemData((uint)gemId);

                var gemProperty = GetGemProperty(gem.GemProperties);

                var enchantmentProperties = GetItemEnchantment(gemProperty.EnchantId);

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
                    var scaleIndex = GetClassId((PlayerScaling)enchantmentProperties.ScalingId);

                    // Because the array is zero indexed, take one off the player level
                    // enchant breakdown from item_database::item_enchantment_effect_stats
                    // from dbc_t::spell_scaling
                    // TODO: Pull the players level through to here
                    var scaledValue = GetSpellScalingMultiplier(scaleIndex, 60);

                    //// Grab the stat this gem increases
                    var stat = (ItemModType)enchantmentProperties.SubEnchantments[0].Property;

                    //// Now add it to the item
                    // Create a new SimcItemGem with the stat, rating etc.
                    var newGem = new SimcItemGem();
                    newGem.StatRating = (int)scaledValue;
                    newGem.Type = stat;
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

        internal double GetItemBudget(SimcItem item, int maxItemlevel)
        {
            // from item_database::item_budget
            // Weirdly, ITEM_QUALITY_MAX (Heirloom) appears with the pic tier
            // and also for some reason pulling the first budget value, not compensating
            // for the item type using GetSlotType() ?
            double budget;
            var scale_ilvl = item.ItemLevel;

            if (maxItemlevel > 0)
                scale_ilvl = Math.Min(scale_ilvl, maxItemlevel);

            var ilvlRandomProps = GetRandomProps(scale_ilvl);

            switch (item.Quality)
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

        internal PlayerScaling GetScaleClass(int scaleType)
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

        internal int GetClassId(PlayerScaling scaleType)
        {
            // from util::class_id
            switch (scaleType)
            {
                case PlayerScaling.WARRIOR:
                    return 1;
                case PlayerScaling.PALADIN:
                    return 2;
                case PlayerScaling.HUNTER:
                    return 3;
                case PlayerScaling.ROGUE:
                    return 4;
                case PlayerScaling.PRIEST:
                    return 5;
                case PlayerScaling.DEATH_KNIGHT:
                    return 6;
                case PlayerScaling.SHAMAN:
                    return 7;
                case PlayerScaling.MAGE:
                    return 8;
                case PlayerScaling.WARLOCK:
                    return 9;
                case PlayerScaling.MONK:
                    return 10;
                case PlayerScaling.DRUID:
                    return 11;
                case PlayerScaling.DEMON_HUNTER:
                    return 12;
                case PlayerScaling.PLAYER_SPECIAL_SCALE:
                    return 13;
                case PlayerScaling.PLAYER_SPECIAL_SCALE2:
                    return 14;
                case PlayerScaling.PLAYER_SPECIAL_SCALE3:
                    return 15;
                case PlayerScaling.PLAYER_SPECIAL_SCALE4:
                    return 16;
                case PlayerScaling.PLAYER_SPECIAL_SCALE5:
                    return 17;
                case PlayerScaling.PLAYER_SPECIAL_SCALE6:
                    return 18;
                case PlayerScaling.PLAYER_SPECIAL_SCALE7:
                    return 13;
                case PlayerScaling.PLAYER_SPECIAL_SCALE8:
                    return 19;
                default:
                    return 0;
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

        internal double GetStaminaMultiplier(int itemLevel, CombatRatingMultiplayerType staminaRatingType)
        {
            var stamMultipliers = _cacheService.GetParsedFileContents<float[][]>(SimcParsedFileType.StaminaMultipliers);

            var stamMulti = stamMultipliers[(int)staminaRatingType][itemLevel - 1];

            return stamMulti;
        }

        internal SimcRawGemProperty GetGemProperty(int gemId)
        {
            var gemPropertyData = _cacheService.GetParsedFileContents<List<SimcRawGemProperty>>(SimcParsedFileType.GemData);

            var gemProperty = gemPropertyData.Where(g => g.Id == gemId).FirstOrDefault();

            return gemProperty;
        }

        internal SimcRawItemEnchantment GetItemEnchantment(uint enchantId)
        {
            var itemEnchantData = _cacheService.GetParsedFileContents<List<SimcRawItemEnchantment>>(SimcParsedFileType.ItemEnchantData);

            var enchantmentProperties = itemEnchantData.Where(e => e.Id == enchantId).FirstOrDefault();

            return enchantmentProperties;
        }

        internal double GetSpellScalingMultiplier(int scaleIndex, int playerLevel)
        {
            var spellScaleData = _cacheService.GetParsedFileContents<double[][]>(SimcParsedFileType.SpellScaleMultipliers);

            var result = spellScaleData[scaleIndex][playerLevel - 1];

            return result;
        }

        internal SimcRawSpell GetSpellData(uint spellId)
        {
            var spellData = _cacheService.GetParsedFileContents<List<SimcRawSpell>>(SimcParsedFileType.SpellData);

            var result = spellData.Where(s => s.Id == spellId).FirstOrDefault();

            return result;
        }
    }
}
