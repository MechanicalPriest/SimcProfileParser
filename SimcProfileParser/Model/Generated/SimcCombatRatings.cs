namespace SimcProfileParser.Model.Generated
{
    /// <summary>
    /// The combat rating needed for 1% of each stat at a player level, e.g. 46 crit rating for 1% crit at level 90.
    /// Mastery is the rating for 1 point of mastery.
    /// </summary>
    /// <remarks>
    /// These are the values from simc's __combat_ratings table, before any class or specialization multiplier,
    /// such as a specialization's mastery coefficient. simc's dbc_t::combat_rating multiplies them by 100 to get
    /// the rating for 100%.
    /// </remarks>
    public class SimcCombatRatings
    {
        public double Dodge { get; internal set; }
        public double Parry { get; internal set; }
        public double Block { get; internal set; }
        public double CritMelee { get; internal set; }
        public double CritRanged { get; internal set; }
        public double CritSpell { get; internal set; }
        public double Leech { get; internal set; }
        public double HasteMelee { get; internal set; }
        public double HasteRanged { get; internal set; }
        public double HasteSpell { get; internal set; }
        public double Mastery { get; internal set; }
        public double VersatilityDamageDone { get; internal set; }
        public double VersatilityHealingDone { get; internal set; }
        public double VersatilityDamageTaken { get; internal set; }
        public double Speed { get; internal set; }
        public double Avoidance { get; internal set; }
    }
}
