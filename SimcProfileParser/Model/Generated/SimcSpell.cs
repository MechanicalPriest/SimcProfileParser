using System.Collections.Generic;

namespace SimcProfileParser.Model.Generated
{
    public class SimcSpell
    {
        public uint SpellId { get; set; }
        public string Name { get; set; }
        public List<SimcSpellEffect> Effects { get; set; }
        public uint School { get; internal set; }
        public int ScalingType { get; internal set; }
        public double MinRange { get; internal set; }
        public double MaxRange { get; internal set; }
        public uint Cooldown { get; internal set; }
        public uint Gcd { get; internal set; }
        public uint Category { get; internal set; }
        public uint CategoryCooldown { get; internal set; }
        public uint Charges { get; internal set; }
        public uint ChargeCooldown { get; internal set; }
        public int MaxTargets { get; internal set; }
        public double Duration { get; internal set; }
        public uint MaxStacks { get; internal set; }
        public uint ProcChance { get; internal set; }
        public uint InternalCooldown { get; internal set; }
        public double Rppm { get; internal set; }
        public int CastTime { get; internal set; }
        /// <summary>
        /// Calculated field, generated from the associated item or player level scaling
        /// to get the scale budget for the SP coefficient
        /// </summary>
        public double ScaleBudget { get; internal set; }
        /// <summary>
        /// Combat Rating Multiplier, used for Item spells to store an optional multiplier
        /// </summary>
        public double CombatRatingMultiplier { get; internal set; }
        /// <summary>
        /// This is the PercentCost from the spellpower_data_t (SimcRawSpellPower)
        /// This is a list of percentage power costs that have a key relating to the AuraID 
        /// that modifies this power usage. For example holy priest aura id is 137031. 
        /// Just getting percent mana is fine for our purposes now.
        /// </summary>
        public Dictionary<uint, double> PowerCosts { get; internal set; }

        /// <summary>
        /// Set if this spell is a conduit
        /// </summary>
        public uint ConduitId { get; set; }
        /// <summary>
        /// Contains each of the ranks stored as N-1 (0-indexed). Entry [0] is Rank 1.
        /// </summary>
        public Dictionary<uint, double> ConduitRanks { get; set; }
        public List<SimcSpellRppmModifier> RppmModifiers { get; internal set; }

        public SimcSpell()
        {
            Effects = new List<SimcSpellEffect>();
            RppmModifiers = new List<SimcSpellRppmModifier>();
            ConduitRanks = new Dictionary<uint, double>();
            PowerCosts = new Dictionary<uint, double>();
        }
    }
}
