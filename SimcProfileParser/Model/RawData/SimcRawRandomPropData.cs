namespace SimcProfileParser.Model.RawData
{
    /// <summary>
    /// rand_prop_points.hpp - random_prop_data_t
    /// </summary>
    class SimcRawRandomPropData
    {
        public uint ItemLevel { get; set; }
        public double DamageReplaceStat { get; set; }
        public double DamageSecondary { get; set; }
        public float[] Epic { get; set; }
        public float[] Rare { get; set; }
        public float[] Uncommon { get; set; }
    }
}
