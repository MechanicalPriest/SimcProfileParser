using System.Collections.Generic;

namespace SimcProfileParser.Model.Generated
{
    public class SimcSpellEffect
    {
        public uint Id { get; internal set; }
        public uint EffectIndex { get; internal set; }
        public uint EffectType { get; internal set; }
        public uint EffectSubType { get; internal set; }
        public int ScalingType { get; internal set; }
        public double Coefficient { get; internal set; }
        public double SpCoefficient { get; internal set; }
        /// <summary>
        /// The effect's spread: the full width of its value range as a fraction of the average value, e.g. 0.05 for ±2.5%.
        /// The value ranges from average × (1 − Delta / 2) to average × (1 + Delta / 2). 0 for effects with a fixed value.
        /// Equivalent to "delta=" in simc's spell dump.
        /// simc: spelleffect_data_t::m_delta / dbc_t::effect_min / dbc_t::effect_max
        /// </summary>
        public double Delta { get; internal set; }
        public double Amplitude { get; internal set; }
        /// <summary>
        /// The maximum number of targets the effect chains or jumps to. 0 if the effect doesn't chain.
        /// Equivalent to "Chain Targets" in simc's spell dump (simc only shows it when it's greater than 0).
        /// </summary>
        public int ChainTargets { get; internal set; }
        /// <summary>
        /// The multiplier applied to the effect's value each time it chains to another target, e.g. 0.5 for half on each jump.
        /// Equivalent to "Chain Multiplier" in simc's spell dump.
        /// </summary>
        public double ChainMultiplier { get; internal set; }
        public double Radius { get; internal set; }
        public double RadiusMax { get; internal set; }
        public double BaseValue { get; internal set; }
        /// <summary>
        /// How much the effect's value increases per player level. 0 for effects that don't scale with level.
        /// Equivalent to "Points Per Level" in simc's spell dump.
        /// </summary>
        public double PointsPerLevel { get; internal set; }
        /// <summary>
        /// For player spells, the effect's value at the player level, as simc calculates it:
        /// <list type="bullet">
        /// <item>Effects with a <see cref="Coefficient"/> and a scaling class: <see cref="Coefficient"/> × <see cref="ScaleBudget"/></item>
        /// <item>Otherwise: <see cref="BaseValue"/> plus <see cref="PointsPerLevel"/> for each level from the spell's level
        /// up to the player level (capped at the spell's max level, if it has one)</item>
        /// </list>
        /// Equal to <see cref="BaseValue"/> for effects that don't scale, and for item spells (use <see cref="ScaleBudget"/>
        /// there). Equivalent to "Scaled Value" in simc's spell dump; where simc shows a range (effects with a
        /// <see cref="Delta"/>), this is the midpoint.
        /// </summary>
        public double ScaledValue { get; internal set; }
        /// <summary>
        /// Effect misc value 1. Its meaning depends on the effect subtype, e.g. for spell modifiers
        /// (subtypes 107, 108, 218, 219) it is the property being modified; for category effects it is the spell category.
        /// </summary>
        public int MiscValue1 { get; internal set; }
        /// <summary>
        /// Effect misc value 2. For label modifiers (subtypes 218 and 219) this is the spell label being modified.
        /// </summary>
        public int MiscValue2 { get; internal set; }
        /// <summary>
        /// Calculated field, generated from the associated item or player level scaling
        /// to get the scale budget for the SP coefficient
        /// </summary>
        public double ScaleBudget { get; internal set; }
        public uint TriggerSpellId { get; internal set; }
        public SimcSpell TriggerSpell { get; internal set; }
        /// <summary>
        /// Ids of spells this effect modifies, matched by class family flags or spell category.
        /// Equivalent to "Affected Spells" in simc's spell dump.
        /// </summary>
        public List<uint> AffectedSpellIds { get; internal set; } = new List<uint>();
        /// <summary>
        /// For talent spells: how <see cref="RankValues"/> apply to <see cref="BaseValue"/>, or null if the talent
        /// has no per-rank values for this effect. Equivalent to "op=" on simc's "Talent Entry" line.
        /// </summary>
        public SimcTalentRankOperation? RankOperation { get; internal set; }
        /// <summary>
        /// For talent spells: the raw per-rank values for this effect, in curve order (normally index 0 = rank 1).
        /// With <see cref="SimcTalentRankOperation.Set"/> the value replaces <see cref="BaseValue"/>, with
        /// <see cref="SimcTalentRankOperation.Multiply"/> it multiplies it. Empty if the talent has no per-rank values.
        /// Equivalent to "values=" on simc's "Talent Entry" line.
        /// </summary>
        public List<double> RankValues { get; internal set; } = new List<double>();
    }
}