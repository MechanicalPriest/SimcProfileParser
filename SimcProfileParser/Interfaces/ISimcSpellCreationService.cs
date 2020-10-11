using SimcProfileParser.Model.Generated;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcSpellCreationService
    {
        SimcSpell GenerateItemSpell(SimcItem item, uint spellId);
        SimcSpell GenerateItemSpell(SimcSpellOptions spellOptions);
        SimcSpell GeneratePlayerSpell(uint playerLevel, uint spellId);
        SimcSpell GeneratePlayerSpell(SimcSpellOptions spellOptions);
    }
}
