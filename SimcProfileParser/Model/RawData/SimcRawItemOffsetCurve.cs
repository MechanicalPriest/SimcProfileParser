namespace SimcProfileParser.Model.RawData
{
    /// <summary>
    /// From item_offset_curve_data_t (item_scaling.hpp). The curve and flat offset an item scaling config uses
    /// to get its item level.
    /// </summary>
    class SimcRawItemOffsetCurve
    {
        public int Id { get; set; }
        public int CurveId { get; set; }
        public int Offset { get; set; }
    }
}
