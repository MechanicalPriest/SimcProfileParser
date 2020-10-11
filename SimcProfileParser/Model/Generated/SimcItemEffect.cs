namespace SimcProfileParser.Model.Generated
{
    public class SimcItemEffect
    {
        public uint EffectId { get; set; }

        public int Type { get; set; }
        public int CooldownGroup { get; set; }
        public int CooldownDuration { get; set; }
        public int CooldownGroupDuration { get; set; }

        public SimcSpell Spell { get; set; }
    }
}