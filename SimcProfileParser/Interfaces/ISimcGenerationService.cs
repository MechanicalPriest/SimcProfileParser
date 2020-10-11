using SimcProfileParser.Model;
using SimcProfileParser.Model.Generated;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    interface ISimcGenerationService
    {
        Task<SimcProfile> GenerateProfileAsync(List<string> profileString);
        Task<SimcProfile> GenerateProfileAsync(string profileString);

        Task<SimcItem> GenerateItemAsync(SimcItemOptions options);
        Task<SimcSpell> GenerateSpellAsync(SimcSpellOptions options);
    }
}
