namespace SimcProfileParser.Model.RawData
{
    class SimcRawRandomPropData
    {
        public uint ItemLevel { get; set; }
        public uint DamageReplaceStat { get; set; }
        public uint DamageSecondary { get; set; }
        public float[] Epic { get; set; }
        public float[] Rare { get; set; }
        public float[] Uncommon { get; set; }
    }
}
