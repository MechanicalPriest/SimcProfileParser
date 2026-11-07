namespace SimcProfileParser.Model.Generated
{
    /// <summary>
    /// How a talent's per-rank values apply to a spell effect's base value.
    /// simc: trait_definition_op
    /// </summary>
    public enum SimcTalentRankOperation
    {
        /// <summary>
        /// The rank value replaces the effect's base value (op=set)
        /// </summary>
        Set = 0,
        /// <summary>
        /// The effect's base value is multiplied by the rank value (op=mul)
        /// </summary>
        Multiply = 1,
    }
}
