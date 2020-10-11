namespace SimcProfileParser.Model.RawData
{
    class SimcRawSpellEffect
    {
        /// <summary>
        /// Spell Effect Id
        /// </summary>
        public uint Id { get; set; }
        /// <summary>
        /// The spell this effect belongs to
        /// </summary>
        public uint SpellId { get; set; }
        /// <summary>
        /// The index of this effect. Commonly labelled as #1, #2 etc.
        /// </summary>
        public uint EffectIndex { get; set; }
        public uint EffectType { get; set; }
        public uint EffectSubType { get; set; }
        /// <summary>
        /// Effect average spell scaling multiplier
        /// </summary>
        public double Coefficient { get; set; }
        /// <summary>
        /// Effect delta spell scaling multiplier
        /// </summary>
        public double Delta { get; set; }
        /// <summary>
        /// unused
        /// </summary>
        public double Unk { get; set; }
        /// <summary>
        /// Effect spell power coefficient
        /// </summary>
        public double SpCoefficient { get; set; }
        /// <summary>
        /// Effect attack power coefficient
        /// </summary>
        public double ApCoefficient { get; set; }
        /// <summary>
        /// Effect amplitude (e.g., tick time)
        /// </summary>
        public double Amplitude { get; set; }
        /// <summary>
        /// Minimum spell radius
        /// </summary>
        public double Radius { get; set; }
        /// <summary>
        /// Maximum Spell Radius
        /// </summary>
        public double RadiusMax { get; set; }
        /// <summary>
        /// Effect value
        /// </summary>
        public double BaseValue { get; set; }
        /// <summary>
        /// Effect misc. value 1
        /// </summary>
        public int MiscValue1 { get; set; }
        /// <summary>
        /// Effect misc. value 2
        /// </summary>
        public int MiscValue2 { get; set; }
        /// <summary>
        /// Class family flags: class_flags[NUM_CLASS_FAMILY_FLAGS]; 
        /// </summary>
        public uint[] ClassFlags { get; set; }
        /// <summary>
        /// The spellid this effect triggers
        /// </summary>
        public uint TriggerSpellId { get; set; }
        /// <summary>
        /// Effect Chain Multiplier
        /// </summary>
        public double ChainMultiplier { get; set; }
        /// <summary>
        /// Effect points per combo point
        /// </summary>
        public double ComboPoints { get; set; }
        /// <summary>
        /// Effect real points per level
        /// </summary>
        public double RealPpl { get; set; }
        /// <summary>
        /// Effect Mechanic
        /// </summary>
        public uint Mechanic { get; set; }
        /// <summary>
        /// Number of targets for chained spells
        /// </summary>
        public int ChainTargets { get; set; }
        /// <summary>
        /// Targeting related field 1
        /// </summary>
        public uint Targeting1 { get; set; }
        /// <summary>
        /// Targeting related field 2
        /// </summary>
        public uint Targeting2 { get; set; }
        /// <summary>
        /// Misc multiplier used for some spells(?)
        /// </summary>
        public double Value { get; set; }
        /// <summary>
        /// Pvp Coefficient
        /// </summary>
        public double PvpCoeff { get; set; }
    }
}
