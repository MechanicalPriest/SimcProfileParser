using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Interfaces.DataSync;

namespace SimcProfileParser
{
    internal class SimcSpellCreationService : ISimcSpellCreationService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<SimcSpellCreationService> _logger;

        public SimcSpellCreationService(ICacheService cacheService, ILogger<SimcSpellCreationService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }
    }
}