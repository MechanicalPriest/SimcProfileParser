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
        /// Calculated field, generated from the associated item to get the scale budget for the SP coefficient
        /// </summary>
        public double ItemScaleBudget { get; internal set; }

        public SimcSpell()
        {
            Effects = new List<SimcSpellEffect>();
        }
    }
}
