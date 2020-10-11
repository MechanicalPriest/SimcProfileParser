using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser
{
    public class SimcProfileParserService : ISimcProfileParserService
    {
        private readonly ILogger<SimcProfileParserService> _logger;
        private readonly ISimcParserService _simcParserService;
        private readonly ISimcItemCreationService _simcItemCreationService;
        private readonly ISimcSpellCreationService _simcSpellCreationService;

        public SimcProfileParserService(ILogger<SimcProfileParserService> logger,
            ISimcParserService simcParserService,
            ISimcItemCreationService simcItemCreationService,
            ISimcSpellCreationService simcSpellCreationService)
        {
            _logger = logger;
            _simcParserService = simcParserService;
            _simcItemCreationService = simcItemCreationService;
            _simcSpellCreationService = simcSpellCreationService;
        }

        public SimcProfileParserService(ILoggerFactory loggerFactory)
            : this(loggerFactory.CreateLogger<SimcProfileParserService>(), null, null, null)
        {
            var dataExtractionService = new RawDataExtractionService(
                loggerFactory.CreateLogger<RawDataExtractionService>());
            var cacheService = new CacheService(dataExtractionService,
                loggerFactory.CreateLogger<CacheService>());

            _simcParserService = new SimcParserService(
                loggerFactory.CreateLogger<SimcParserService>());

            _simcItemCreationService = new SimcItemCreationService(
                cacheService,
                loggerFactory.CreateLogger<SimcItemCreationService>());

            _simcSpellCreationService = new SimcSpellCreationService(
                cacheService,
                loggerFactory.CreateLogger<SimcSpellCreationService>());
        }

        public SimcProfileParserService()
            : this(NullLoggerFactory.Instance)
        {
            
        }

        public SimcProfile GenerateProfileAsync(List<string> profileString)
        {
            throw new NotImplementedException();
        }

        public SimcProfile GenerateProfileAsync(string profileString)
        {
            throw new NotImplementedException();
        }

        public SimcProfile GenerateProfile(List<string> profileString)
        {
            throw new NotImplementedException();
        }

        public SimcProfile GenerateProfile(string profileString)
        {
            throw new NotImplementedException();
        }

        public SimcItem GenerateItemAsync(SimcItemOptions options)
        {
            throw new NotImplementedException();
        }

        public SimcItem GenerateItem(SimcItemOptions options)
        {
            throw new NotImplementedException();
        }

        public SimcSpell GenerateSpellAsync(SimcSpellOptions options)
        {
            throw new NotImplementedException();
        }

        public SimcSpell GenerateSpell(SimcSpellOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
