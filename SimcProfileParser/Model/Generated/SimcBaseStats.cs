namespace SimcProfileParser.Model.Generated
{
    /// <summary>
    /// Base primary attributes, either a class's base attributes at a level or a race's attribute modifiers.
    /// From stat_data_t (sc_extra_data.inc)
    /// </summary>
    public class SimcBaseStats
    {
        public int Strength { get; internal set; }
        public int Agility { get; internal set; }
        public int Stamina { get; internal set; }
        public int Intellect { get; internal set; }
    }
}
