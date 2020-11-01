using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimcProfileParser
{
    internal class SimcVersionService : ISimcVersionService
    {
        private readonly ISimcUtilityService _simcUtilityService;
        private readonly ILogger<SimcVersionService> _logger;

        public SimcVersionService(ISimcUtilityService simcUtilityService,
            ILogger<SimcVersionService> logger)
        {
            _simcUtilityService = simcUtilityService;
            _logger = logger;
        }

        public async Task<string> GetGameDataVersionAsync()
        {
            return await _simcUtilityService.GetClientDataVersionAsync();
        }
    }
}
