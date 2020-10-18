namespace SimcProfileParser.Model.RawData
{
    class SimcRawSpellPower
    {
        public uint Id { get; set; }
        public uint SpellId { get; set; }
        public uint AuraId { get; set; }
        public int PowerType { get; set; }
        public int Cost { get; set; }
        public int CostMax { get; set; }
        public int CostPerTick { get; set; }
        public double PercentCost { get; set; }
        public double PercentCostMax { get; set; }
        public double PercentCostPerTick { get; set; }
    }
}
