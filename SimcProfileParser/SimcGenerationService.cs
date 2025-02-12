using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Interfaces.DataSync;
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
        private readonly ISimcVersionService _simcVersionService;
        private readonly ISimcTalentService _simcTalentService;
        private readonly ICacheService _cacheService;

        public SimcGenerationService(ILogger<SimcGenerationService> logger,
            ISimcParserService simcParserService,
            ISimcItemCreationService simcItemCreationService,
            ISimcSpellCreationService simcSpellCreationService,
            ISimcVersionService simcVersionService,
            ISimcTalentService simcTalentService,
            ICacheService cacheService)
        {
            _logger = logger;
            _simcParserService = simcParserService;
            _simcItemCreationService = simcItemCreationService;
            _simcSpellCreationService = simcSpellCreationService;
            _simcVersionService = simcVersionService;
            _simcTalentService = simcTalentService;
            _cacheService = cacheService;
        }

        public SimcGenerationService(ILoggerFactory loggerFactory)
            : this(loggerFactory.CreateLogger<SimcGenerationService>(), null, null, null, null, null, null)
        {
            var dataExtractionService = new RawDataExtractionService(
                loggerFactory.CreateLogger<RawDataExtractionService>());

            _cacheService = new CacheService(dataExtractionService,
                loggerFactory.CreateLogger<CacheService>());

            var utilityService = new SimcUtilityService(
                _cacheService,
                loggerFactory.CreateLogger<SimcUtilityService>());

            var spellCreationService = new SimcSpellCreationService(
                utilityService,
                loggerFactory.CreateLogger<SimcSpellCreationService>());

            _simcParserService = new SimcParserService(
                loggerFactory.CreateLogger<SimcParserService>());

            _simcItemCreationService = new SimcItemCreationService(
                _cacheService,
                spellCreationService,
                utilityService,
                loggerFactory.CreateLogger<SimcItemCreationService>());

            _simcVersionService = new SimcVersionService(
                utilityService,
                loggerFactory.CreateLogger<SimcVersionService>());

            _simcSpellCreationService = spellCreationService;

            _simcTalentService = new SimcTalentService(
                utilityService,
                loggerFactory.CreateLogger<SimcTalentService>());

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

                newItem.Equipped = item.Equipped;
            }

            // Populate the spell Ids of any conduits set
            foreach (var conduit in newProfile.ParsedProfile.Conduits)
            {
                conduit.SpellId = await _simcSpellCreationService
                    .GetSpellIdFromConduitIdAsync((uint)conduit.ConduitId);
            }

            // and populate the spell Ids of any conduits set)
            foreach (var soulbind in newProfile.ParsedProfile.Soulbinds)
            {
                foreach (var conduit in soulbind.SocketedConduits)
                {
                    conduit.SpellId = await _simcSpellCreationService
                        .GetSpellIdFromConduitIdAsync((uint)conduit.ConduitId);
                }
            }

            // Populate the details for each talent
            foreach(var talent in newProfile.ParsedProfile.Talents)
            {
                var newTalent = await _simcTalentService.GetTalentDataAsync(talent.TalentId, talent.Rank);
                if (newTalent != null)
                    newProfile.Talents.Add(newTalent);
            }

            return newProfile;
        }

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

        public async Task<string> GetGameDataVersionAsync()
        {
            return await _simcVersionService.GetGameDataVersionAsync();
        }

        public async Task<List<SimcTalent>> GetAvailableTalentsAsync(int classId, int specId)
        {
            return await _simcTalentService.GetAvailableTalentsAsync(classId, specId);
        }

        public bool UsePtrData
        {
            get => _cacheService.UsePtrData;
            set => _cacheService.SetUsePtrData(value);
        }

        public string UseBranchName
        {
            get => _cacheService.UseBranchName;
            set => _cacheService.SetUseBranchName(value);
        }
    }
}
