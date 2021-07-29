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
        public double Delta { get; internal set; }
        public double Amplitude { get; internal set; }
        public double Radius { get; internal set; }
        public double RadiusMax { get; internal set; }
        public double BaseValue { get; internal set; }
        /// <summary>
        /// Calculated field, generated from the associated item or player level scaling
        /// to get the scale budget for the SP coefficient
        /// </summary>
        public double ScaleBudget { get; internal set; }
        public uint TriggerSpellId { get; internal set; }
        public SimcSpell TriggerSpell { get; internal set; }
    }
}