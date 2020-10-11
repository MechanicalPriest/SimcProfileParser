using SimcProfileParser.Model.Generated;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcSpellCreationService
    {
        SimcSpell GenerateItemSpell(SimcItem item, uint spellId);
        SimcSpell GeneratePlayerSpell(uint playerLevel, uint spellId);
    }
}
