namespace SimcProfileParser.Model.RawData
{
    /// <summary>
    /// From content_tuning_data_t (content_tuning.hpp). Used to cap the level an item scales to with the
    /// content_tuning= item option.
    /// </summary>
    class SimcRawContentTuning
    {
        public uint Id { get; set; }
        public int Flags { get; set; }
        public int ExpansionId { get; set; }
        public int MinLevelSquish { get; set; }
        public int MaxLevelSquish { get; set; }
        public int ItemLevel { get; set; }
    }
}
