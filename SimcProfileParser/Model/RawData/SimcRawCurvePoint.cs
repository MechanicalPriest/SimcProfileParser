namespace SimcProfileParser.Model.RawData
{
    class SimcRawCurvePoint
    {
        public uint CurveId { get; set; }
        public uint Index { get; set; }
        public float Primary1 { get; set; }
        public float Primary2 { get; set; }
        public float Secondary1 { get; set; }
        public float Secondary2 { get; set; }
    }
}
