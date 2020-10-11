namespace SimcProfileParser.Model.Profile
{
    public class SimcParsedConduit
    {
        public int ConduitId { get; internal set; }
        public int Rank { get; internal set; }
        public override string ToString()
        {
            return $"{ConduitId}:{Rank}";
        }
    }
}
