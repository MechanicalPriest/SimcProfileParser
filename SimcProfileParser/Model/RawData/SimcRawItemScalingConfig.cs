namespace SimcProfileParser.Model.RawData
{
    /// <summary>
    /// From item_scaling_config_data_t (item_scaling.hpp). Used by the Midnight ITEM_BONUS_SCALE_CONFIG bonuses
    /// to set an item's level.
    /// </summary>
    class SimcRawItemScalingConfig
    {
        public int Id { get; set; }
        public int ItemOffsetCurveId { get; set; }
        public int ItemLevel { get; set; }
        public int PlayerLevel { get; set; }
        public int SquishEraId { get; set; }
    }
}
