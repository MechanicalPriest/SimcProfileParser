using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimcProfileParser
{
    internal class SimcSpellCreationService : ISimcSpellCreationService
    {
        private readonly ISimcUtilityService _simcUtilityService;
        private readonly ILogger<SimcSpellCreationService> _logger;

        public SimcSpellCreationService(ISimcUtilityService simcUtilityService,
            ILogger<SimcSpellCreationService> logger)
        {
            _simcUtilityService = simcUtilityService;
            _logger = logger;
        }

        public async Task<SimcSpell> GenerateItemSpellAsync(SimcItem item, uint spellId)
        {
            var spell = await BuildItemSpellAsync(spellId, item.ItemLevel, item.Quality, item.InventoryType);

            return spell;
        }

        public async Task<SimcSpell> GenerateItemSpellAsync(SimcSpellOptions spellOptions)
        {
            if (!spellOptions.ItemQuality.HasValue)
                throw new ArgumentNullException(nameof(spellOptions.ItemQuality),
                    "SpellOptions must include Item Quality to generate an item spell.");

            if (!spellOptions.ItemInventoryType.HasValue)
                throw new ArgumentNullException(nameof(spellOptions.ItemInventoryType),
                    "SpellOptions must include Item Inventory Type to generate an item spell.");

            var spell = await BuildItemSpellAsync(spellOptions.SpellId, spellOptions.ItemLevel,
                spellOptions.ItemQuality.Value, spellOptions.ItemInventoryType.Value);

            return spell;
        }

        public async Task<SimcSpell> GeneratePlayerSpellAsync(uint playerLevel, uint spellId)
        {
            var spellData = await _simcUtilityService.GetRawSpellDataAsync(spellId);

            var itemSpell = new SimcSpell()
            {
                SpellId = spellData.Id,
                Name = spellData.Name,
                School = spellData.School,
                MinRange = spellData.MinRange,
                MaxRange = spellData.MaxRange,
                Cooldown = spellData.Cooldown,
                Gcd = spellData.Gcd,
                Category = spellData.Category,
                CategoryCooldown = spellData.CategoryCooldown,
                Charges = spellData.Charges,
                ChargeCooldown = spellData.ChargeCooldown,
                MaxTargets = spellData.MaxTargets,
                Duration = spellData.Duration,
                MaxStacks = spellData.MaxStack,
                ProcChance = spellData.ProcChance,
                InternalCooldown = spellData.InternalCooldown,
                Rppm = spellData.Rppm,
                CastTime = spellData.CastTime
            };

            // Add power costs
            foreach (var power in spellData.SpellPowers)
            {
                itemSpell.PowerCosts.Add(power.AuraId, power.PercentCost);
            }

            // Add the RPPM modifiers
            var rppmModifiers = await _simcUtilityService.GetSpellRppmModifiersAsync(spellData.Id);

            foreach (var modifier in rppmModifiers)
            {
                var newRppmModifier = new SimcSpellRppmModifier()
                {
                    RppmIsHasted = modifier.ModifierType == RppmModifierType.RPPM_MODIFIER_HASTE,
                    RppmIsSpecModified = modifier.ModifierType == RppmModifierType.RPPM_MODIFIER_SPEC,
                    RppmCoefficient = modifier.Coefficient,
                    RppmSpec = modifier.ModifierType == RppmModifierType.RPPM_MODIFIER_SPEC ? modifier.Type : 0
                };

                itemSpell.RppmModifiers.Add(newRppmModifier);
            }

            // Populate the spell effects
            foreach (var spellEffect in spellData.Effects)
            {
                // Populate the trigger spell if one exists.
                SimcSpell triggerSpell = null;
                if (spellEffect.TriggerSpellId > 0)
                    triggerSpell = await GeneratePlayerSpellAsync(playerLevel, spellEffect.TriggerSpellId);

                // Get the scale budget

                var spellScalingClass = _simcUtilityService.GetScaleClass(spellEffect.ScalingType);

                double budget = 0;

                if (spellScalingClass != PlayerScaling.PLAYER_NONE)
                {
                    // Cap the scaling level if needed
                    if (spellData.MaxScalingLevel > 0)
                        playerLevel = Math.Min(playerLevel, (uint)spellData.MaxScalingLevel);

                    var scaleIndex = _simcUtilityService.GetClassId(spellScalingClass);

                    var scaledValue = await _simcUtilityService.GetSpellScalingMultiplierAsync(scaleIndex, (int)playerLevel);

                    budget = scaledValue;
                }

                itemSpell.Effects.Add(new SimcSpellEffect()
                {
                    Id = spellEffect.Id,
                    EffectIndex = spellEffect.EffectIndex,
                    EffectType = spellEffect.EffectType,
                    EffectSubType = spellEffect.EffectSubType,
                    ScalingType = spellEffect.ScalingType,
                    Coefficient = spellEffect.Coefficient,
                    SpCoefficient = spellEffect.SpCoefficient,
                    Delta = spellEffect.Delta,
                    Amplitude = spellEffect.Amplitude,
                    Radius = spellEffect.Radius,
                    RadiusMax = spellEffect.RadiusMax,
                    BaseValue = spellEffect.BaseValue,
                    TriggerSpellId = spellEffect.TriggerSpellId,
                    TriggerSpell = triggerSpell,
                    ScaleBudget = budget
                });
            }

            // Add the conduit info
            var conduitRanks = await _simcUtilityService.GetSpellConduitRanksAsync(spellData.Id);

            foreach (var rank in conduitRanks)
            {
                if (itemSpell.ConduitId == 0)
                    itemSpell.ConduitId = rank.ConduitId;

                itemSpell.ConduitRanks.Add(rank.Rank, rank.Value);
            }

            return itemSpell;
        }

        public async Task<SimcSpell> GeneratePlayerSpellAsync(SimcSpellOptions spellOptions)
        {
            if (!spellOptions.PlayerLevel.HasValue)
                throw new ArgumentNullException(nameof(spellOptions.PlayerLevel),
                    "SpellOptions must include Player Level to generate a player scaled spell.");

            if (spellOptions.SpellId <= 0)
                throw new ArgumentNullException(nameof(spellOptions.SpellId),
                    "SpellOptions must include Spell ID to generate a player scaled spell.");

            var spell = await GeneratePlayerSpellAsync(spellOptions.PlayerLevel.Value, spellOptions.SpellId);

            return spell;
        }

        public async Task<uint> GetSpellIdFromConduitIdAsync(uint conduitId)
        {
            var result = await _simcUtilityService.GetSpellConduitSpellIdAsync(conduitId);

            return result;
        }

        internal async Task<SimcSpell> BuildItemSpellAsync(uint spellId, int itemLevel,
            ItemQuality itemQuality, InventoryType inventoryType, List<uint> parentSpellIds = null)
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

            var spellData = await _simcUtilityService.GetRawSpellDataAsync(spellId);

            if(spellData == null)
            {
                // This can happen sometimes if it's a spell that isn't in use by anything/anyone.
                // For example +5 fishing bonus on a helm.
                // TODO: Create a blacklist of known spells that can be skipped to avoid reaching this error.
                _logger?.LogError($"Unable to find spellId {spellId}.");
                return default;
            }

            var combatRatingType = _simcUtilityService.GetCombatRatingMultiplierType(inventoryType);
            var multi = await _simcUtilityService.GetCombatRatingMultiplierAsync(itemLevel, combatRatingType);

            var itemSpell = new SimcSpell()
            {
                SpellId = spellData.Id,
                Name = spellData.Name,
                School = spellData.School,
                //ScalingType = spellData.ScalingType,
                MinRange = spellData.MinRange,
                MaxRange = spellData.MaxRange,
                Cooldown = spellData.Cooldown,
                Gcd = spellData.Gcd,
                Category = spellData.Category,
                CategoryCooldown = spellData.CategoryCooldown,
                Charges = spellData.Charges,
                ChargeCooldown = spellData.ChargeCooldown,
                MaxTargets = spellData.MaxTargets,
                Duration = spellData.Duration,
                MaxStacks = spellData.MaxStack,
                ProcChance = spellData.ProcChance,
                InternalCooldown = spellData.InternalCooldown,
                Rppm = spellData.Rppm,
                CastTime = spellData.CastTime,
                CombatRatingMultiplier = multi
            };

            // Add power costs
            foreach (var power in spellData.SpellPowers)
            {
                itemSpell.PowerCosts.Add(power.AuraId, power.PercentCost);
            }

            // Add the RPPM modifiers
            var rppmModifiers = await _simcUtilityService.GetSpellRppmModifiersAsync(spellData.Id);

            foreach (var modifier in rppmModifiers)
            {
                var newRppmModifier = new SimcSpellRppmModifier()
                {
                    SpellId = modifier.SpellId,
                    RppmIsHasted = modifier.ModifierType == RppmModifierType.RPPM_MODIFIER_HASTE,
                    RppmIsSpecModified = modifier.ModifierType == RppmModifierType.RPPM_MODIFIER_SPEC,
                    RppmCoefficient = modifier.Coefficient,
                    RppmSpec = modifier.ModifierType == RppmModifierType.RPPM_MODIFIER_SPEC ? modifier.Type : 0
                };

                itemSpell.RppmModifiers.Add(newRppmModifier);
            }

            // Populate the spell effects
            if (parentSpellIds == null)
                parentSpellIds = new List<uint>();

            parentSpellIds.Add(spellId);

            foreach (var spellEffect in spellData.Effects)
            {
                // Populate the trigger spell if one exists.
                SimcSpell triggerSpell = null;
                if (spellEffect.TriggerSpellId > 0)
                {
                    if(!parentSpellIds.Contains(spellEffect.TriggerSpellId))
                        triggerSpell = await BuildItemSpellAsync(
                            spellEffect.TriggerSpellId, itemLevel, itemQuality, inventoryType, parentSpellIds.ToList());
                }

                // Get the scale budget
                // simc: spelleffect_data_t::average - spell_data.cpp

                var budget = await _simcUtilityService.GetItemBudgetAsync(itemLevel, itemQuality, spellData.MaxScalingLevel);

                var spellScalingClass = _simcUtilityService.GetScaleClass(spellEffect.ScalingType);

                if (spellScalingClass == PlayerScaling.PLAYER_SPECIAL_SCALE)
                {
                    // This is some logic that azerite traits used. Seems to be used in some trinket effects too
                    // From azerite_power_t::check_combat_rating_penalty. See #77
                    foreach (var effect in spellData.Effects)
                    {
                        if (effect.EffectSubType == 189 // 189 == A_MOD_RATING
                            && effect.Coefficient > 0)
                        {
                            _logger?.LogTrace($"Changing scaling type from PLAYER_SPECIAL_SCALE (-1) to PLAYER_SPECIAL_SCALE7 (-7). Spell: {spellData.Id}");
                            spellScalingClass = PlayerScaling.PLAYER_SPECIAL_SCALE7;
                            break;
                        }
                    }
                }

                if (spellScalingClass == PlayerScaling.PLAYER_SPECIAL_SCALE7)
                {
                    budget *= multi;
                }
                else if (spellScalingClass == PlayerScaling.PLAYER_SPECIAL_SCALE8)
                {
                    var props = await _simcUtilityService.GetRandomPropsAsync(itemLevel);
                    budget = props.DamageReplaceStat;
                }
                else if (spellScalingClass == PlayerScaling.PLAYER_NONE || spellScalingClass == PlayerScaling.PLAYER_SPECIAL_SCALE9)
                {
                    // This is from spelleffect_data_t::average's call to _spell->flags( spell_attribute::SX_SCALE_ILEVEL )
                    // and from bool flags( spell_attribute attr ) const in spell_data.hpp
                    var ilvlScaleAttribute = 354u;
                    int bit = (int)(ilvlScaleAttribute % 32u);
                    var index = ilvlScaleAttribute / 32u;
                    var mask = 1u << bit;

                    if (spellData.Attributes.Length > index &&
                        (spellData.Attributes[index] & mask) != 0)
                    {
                        var props = await _simcUtilityService.GetRandomPropsAsync(itemLevel);
                        budget = props.DamageSecondary;
                    }
                    else
                    {
                        _logger?.LogError($"ilvl scaling from spell flags not yet implemented. Spell: {spellData.Id}");
                    }
                }

                itemSpell.Effects.Add(new SimcSpellEffect()
                {
                    Id = spellEffect.Id,
                    EffectIndex = spellEffect.EffectIndex,
                    EffectType = spellEffect.EffectType,
                    EffectSubType = spellEffect.EffectSubType,
                    ScalingType = spellEffect.ScalingType,
                    Coefficient = spellEffect.Coefficient,
                    SpCoefficient = spellEffect.SpCoefficient,
                    Delta = spellEffect.Delta,
                    Amplitude = spellEffect.Amplitude,
                    Radius = spellEffect.Radius,
                    RadiusMax = spellEffect.RadiusMax,
                    BaseValue = spellEffect.BaseValue,
                    TriggerSpellId = spellEffect.TriggerSpellId,
                    TriggerSpell = triggerSpell,
                    ScaleBudget = budget
                });
            }

            return itemSpell;
            // stat_buff_t::stat_buff_t
            // Checks if the effects subtype  is A_MOOD_RATING
            // Then grab the rating then translate value1 to get the stat type
            // and if its an item, apply the combat rating multi for the item.
        }
    }
}