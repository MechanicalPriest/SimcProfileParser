using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;

namespace SimcProfileParser
{
    internal class SimcSpellCreationService : ISimcSpellCreationService
    {
        private readonly ICacheService _cacheService;
        private readonly ISimcUtilityService _simcUtilityService;
        private readonly ILogger<SimcSpellCreationService> _logger;

        public SimcSpellCreationService(ICacheService cacheService,
            ISimcUtilityService simcUtilityService,
            ILogger<SimcSpellCreationService> logger)
        {
            _cacheService = cacheService;
            _simcUtilityService = simcUtilityService;
            _logger = logger;
        }

        public SimcSpell GenerateItemSpell(SimcItem item, uint spellId)
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

            var spellData = _simcUtilityService.GetRawSpellData(spellId);

            var budget = _simcUtilityService.GetItemBudget(item, spellData.MaxScalingLevel);

            var spellScalingClass = _simcUtilityService.GetScaleClass(spellData.ScalingType);

            if (spellScalingClass == PlayerScaling.PLAYER_SPECIAL_SCALE7)
            {
                var combatRatingType = _simcUtilityService.GetCombatRatingMultiplierType(item.InventoryType);
                var multi = _simcUtilityService.GetCombatRatingMultiplier(item.ItemLevel, combatRatingType);
                budget *= multi;
            }
            else if (spellScalingClass == PlayerScaling.PLAYER_SPECIAL_SCALE8)
            {
                var props = _simcUtilityService.GetRandomProps(item.ItemLevel);
                budget = props.DamageReplaceStat;
            }
            else if (spellScalingClass == PlayerScaling.PLAYER_NONE)
            {
                // This is from spelleffect_data_t::average's call to _spell->flags( spell_attribute::SX_SCALE_ILEVEL )
                _logger?.LogError($"ilvl scaling from spell flags not yet implemented. Item: {item.ItemId} Spell: {spellData.Id}");
            }

            var itemSpell = new SimcSpell()
            {
                SpellId = spellData.Id,
                Name = spellData.Name,
                School = spellData.School,
                ScalingType = spellData.ScalingType,
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
                ItemScaleBudget = budget,
            };

            foreach (var spellEffect in spellData.Effects)
            {
                itemSpell.Effects.Add(new SimcSpellEffect()
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

            return itemSpell;
        }

        public SimcSpell GeneratePlayerSpell(uint playerLevel, uint spellId)
        {
            var spellData = _simcUtilityService.GetRawSpellData(spellId);

            return null;
        }
    }
}