using SimcProfileParser.Model.Generated;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcSpellCreationService
    {
        Task<SimcSpell> GenerateItemSpellAsync(SimcItem item, uint spellId);
        Task<SimcSpell> GenerateItemSpellAsync(SimcSpellOptions spellOptions);
        Task<SimcSpell> GeneratePlayerSpellAsync(uint playerLevel, uint spellId);
        Task<SimcSpell> GeneratePlayerSpellAsync(SimcSpellOptions spellOptions);
        Task<uint> GetSpellIdFromConduitIdAsync(uint conduitId);
    }
}
