using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model.Generated;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimcProfileParser
{
    internal class SimcTalentService : ISimcTalentService
    {
        private readonly ISimcUtilityService _simcUtilityService;
        private readonly ILogger<SimcTalentService> _logger;

        public SimcTalentService(ISimcUtilityService simcUtilityService,
            ILogger<SimcTalentService> logger)
        {
            _simcUtilityService = simcUtilityService;
            _logger = logger;
        }

        public async Task<SimcTalent> GetTalentDataAsync(int traitEntryId, int rank)
        {
            var traitData = await _simcUtilityService.GetTraitDataAsync(traitEntryId);

            if(traitData != null)
            {
                var talent = new SimcTalent
                {
                    TraitEntryId = traitData.TraitNodeEntryId,
                    SpellId = traitData.SpellId,
                    Name = traitData.Name,
                    Rank = rank
                };

                return talent;
            }

            _logger?.LogWarning("Unable to find data for trait {0}", traitEntryId);

            return default(SimcTalent);
        }

        public async Task<List<SimcTalent>> GetAvailableTalentsAsync(int classId, int specId)
        {
            var traits = await _simcUtilityService.GetTraitsByClassSpecAsync(classId, specId);

            var talents = new List<SimcTalent>();

            if (traits != null)
            {
                foreach(var traitData in traits)
                {
                    var talent = new SimcTalent
                    {
                        TraitEntryId = traitData.TraitNodeEntryId,
                        SpellId = traitData.SpellId,
                        Name = traitData.Name
                    };

                    talents.Add(talent);
                }
            }
            else
            {
                _logger?.LogWarning("Unable to find trait data for class {0} spec {1}", classId, specId);
            }

            return talents;
        }
    }
}
