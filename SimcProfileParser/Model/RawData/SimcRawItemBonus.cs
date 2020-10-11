namespace SimcProfileParser.Model.RawData
{
    class SimcRawItemBonus
    {
        public uint Id { get; set; }
        public uint BonusId { get; set; }
        /// <summary>
        /// TODO this is one of the enums, update it.
        /// </summary>
        public ItemBonusType Type { get; set; }
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
        public int Value4 { get; set; }
        public uint Index { get; set; }
    }
}
