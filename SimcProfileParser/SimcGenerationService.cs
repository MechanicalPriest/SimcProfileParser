using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimcProfileParser
{
    public class SimcGenerationService : ISimcGenerationService
    {
        private readonly ILogger<SimcGenerationService> _logger;
        private readonly ISimcParserService _simcParserService;
        private readonly ISimcItemCreationService _simcItemCreationService;
        private readonly ISimcSpellCreationService _simcSpellCreationService;

        public SimcGenerationService(ILogger<SimcGenerationService> logger,
            ISimcParserService simcParserService,
            ISimcItemCreationService simcItemCreationService,
            ISimcSpellCreationService simcSpellCreationService)
        {
            _logger = logger;
            _simcParserService = simcParserService;
            _simcItemCreationService = simcItemCreationService;
            _simcSpellCreationService = simcSpellCreationService;
        }

        public SimcGenerationService(ILoggerFactory loggerFactory)
            : this(loggerFactory.CreateLogger<SimcGenerationService>(), null, null, null)
        {
            var dataExtractionService = new RawDataExtractionService(
                loggerFactory.CreateLogger<RawDataExtractionService>());
            var cacheService = new CacheService(dataExtractionService,
                loggerFactory.CreateLogger<CacheService>());
            var utilityService = new SimcUtilityService(
                cacheService,
                loggerFactory.CreateLogger<SimcUtilityService>());

            var spellCreationService = new SimcSpellCreationService(
                utilityService,
                loggerFactory.CreateLogger<SimcSpellCreationService>());

            _simcParserService = new SimcParserService(
                loggerFactory.CreateLogger<SimcParserService>());

            _simcItemCreationService = new SimcItemCreationService(
                cacheService,
                spellCreationService,
                utilityService,
                loggerFactory.CreateLogger<SimcItemCreationService>());

            _simcSpellCreationService = spellCreationService;
        }

        public SimcGenerationService()
            : this(NullLoggerFactory.Instance)
        {

        }

        public async Task<SimcProfile> GenerateProfileAsync(List<string> profileString)
        {
            if (profileString == null || profileString.Count == 0)
                throw new ArgumentNullException(nameof(profileString), "profile string must contain valid entries");

            // Process the incoming profile string
            var parsedProfile = await Task<SimcParsedProfile>.Factory.StartNew(
                () => _simcParserService.ParseProfileAsync(profileString));

            if (parsedProfile == null)
                throw new ArgumentOutOfRangeException(nameof(profileString), "profileString provided was invalid or produced no results");

            // Build up the basics of the new object
            var newProfile = new SimcProfile
            {
                ParsedProfile = parsedProfile
            };

            // Now build up the items
            foreach (var item in newProfile.ParsedProfile.Items)
            {
                var newItem = await _simcItemCreationService.CreateItemAsync(item);
                newProfile.GeneratedItems.Add(newItem);
            }

            // Populate the spell Ids of any conduits set)
            foreach(var conduit in newProfile.ParsedProfile.Conduits)
            {
                conduit.SpellId = await _simcSpellCreationService
                    .GetSpellIdFromConduitIdAsync((uint)conduit.ConduitId);
            }

            return newProfile;
        }

        /// <summary>
        /// Helper method to generate a SimcProfile based on the contents of an entire profile string
        /// </summary>
        /// <param name="profileString"></param>
        /// <returns></returns>
        public async Task<SimcProfile> GenerateProfileAsync(string profileString)
        {
            if (string.IsNullOrEmpty(profileString))
            {
                _logger?.LogWarning("Incoming profileString is empty.");
                throw new ArgumentNullException(nameof(profileString));
            }

            _logger?.LogInformation($"Splitting a string with {profileString.Length} character(s)");

            var lines = profileString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            _logger?.LogInformation($"Created {lines.Count} lines to be processed.");

            return await GenerateProfileAsync(lines);
        }

        public async Task<SimcItem> GenerateItemAsync(SimcItemOptions options)
        {
            if (options == null)
            {
                _logger?.LogWarning($"Incoming item options invalid");
                throw new ArgumentNullException(nameof(options));
            }
            if (options.ItemId == 0)
            {
                _logger?.LogWarning($"Incoming item options has invalid ItemId:{options.ItemId}.");
                throw new ArgumentOutOfRangeException(nameof(options.ItemId),
                    $"Incoming item options has invalid ItemId:{options.ItemId}.");
            }

            var item = await _simcItemCreationService.CreateItemAsync(options);

            return item;
        }

        public async Task<SimcSpell> GenerateSpellAsync(SimcSpellOptions options)
        {
            SimcSpell spell;

            if (options.ItemLevel != 0)
            {
                // TODO: Remove this await once the rest of the library chain supports async better
                spell = await _simcSpellCreationService.GenerateItemSpellAsync(options);
            }
            else
            {
                // TODO: Remove this await once the rest of the library chain supports async better
                spell = await _simcSpellCreationService.GeneratePlayerSpellAsync(options);
            }

            return spell;
        }
    }
}
