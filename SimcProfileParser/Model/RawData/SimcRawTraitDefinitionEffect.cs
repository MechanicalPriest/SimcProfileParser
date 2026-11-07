namespace SimcProfileParser.Model.RawData
{
    /// <summary>
    /// Per-rank override for a talent spell's effect value.
    /// simc: trait_data.hpp - trait_definition_effect_entry_t
    /// </summary>
    class SimcRawTraitDefinitionEffect
    {
        public uint TraitDefinitionId { get; set; }
        /// <summary>
        /// 0-based index of the spell effect being overridden
        /// </summary>
        public uint EffectIndex { get; set; }
        /// <summary>
        /// simc trait_definition_op: -1 = none, 0 = set, 1 = multiply
        /// </summary>
        public int Operation { get; set; }
        /// <summary>
        /// Curve holding the value for each rank (primary1 = rank, primary2 = value)
        /// </summary>
        public uint CurveId { get; set; }
    }
}
