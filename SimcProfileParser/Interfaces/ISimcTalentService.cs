using SimcProfileParser.Model.Generated;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    internal interface ISimcTalentService
    {
        Task<SimcTalent> GetTalentDataAsync(int talentId, int rank);
        Task<List<SimcTalent>> GetAvailableTalentsAsync(int classId, int specId);
    }
}
