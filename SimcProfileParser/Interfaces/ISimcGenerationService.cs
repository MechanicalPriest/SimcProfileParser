using SimcProfileParser.Model.Generated;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcGenerationService
    {
        /// <summary>
        /// Set the flag to use PTR data for data extraction
        /// </summary>
        bool UsePtrData { get; set; }
        /// <summary>
        /// Set the github branch name to use for data extraction
        /// </summary>
        string UseBranchName { get; set; }

        Task<SimcProfile> GenerateProfileAsync(List<string> profileString);
        /// <summary>
        /// Helper method to generate a SimcProfile based on the contents of an entire profile string
        /// </summary>
        /// <param name="profileString"></param>
        /// <returns></returns>
        Task<SimcProfile> GenerateProfileAsync(string profileString);

        Task<SimcItem> GenerateItemAsync(SimcItemOptions options);
        Task<SimcSpell> GenerateSpellAsync(SimcSpellOptions options);

        Task<string> GetGameDataVersionAsync();
        // TODO: Make this public once implemented.
        //Task<List<SimcTalent>> GetAvailableTalentsAsync(int classId, int specId);
    }
}
