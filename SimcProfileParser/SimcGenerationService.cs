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
    /// <summary>
    /// Main service for parsing and generating SimulationCraft (SimC) profile data.
    /// This service provides functionality to parse SimC addon exports, generate items with bonus IDs and gems,
    /// create spells with proper scaling, retrieve talent information, and fetch game data versions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SimC addon exports character data in a specific text format that includes items, talents,
    /// character stats, and other game data. This service parses that format and enriches it with
    /// detailed information from SimulationCraft's game data files.
    /// </para>
    /// <para>
    /// This service can be used standalone by creating a new instance, or through dependency injection
    /// using the AddSimcProfileParser() extension method.
    /// </para>
    /// </remarks>
    public class SimcGenerationService : ISimcGenerationService
    {
        private readonly ILogger<SimcGenerationService> _logger;
        private readonly ISimcParserService _simcParserService;
        private readonly ISimcItemCreationService _simcItemCreationService;
        private readonly ISimcSpellCreationService _simcSpellCreationService;
        private readonly ISimcVersionService _simcVersionService;
        private readonly ISimcTalentService _simcTalentService;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimcGenerationService"/> class with all dependencies.
        /// This constructor is primarily used for dependency injection scenarios.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <param name="simcParserService">Service for parsing SimC profile strings into structured data.</param>
        /// <param name="simcItemCreationService">Service for creating and enriching item data with bonuses, gems, and effects.</param>
        /// <param name="simcSpellCreationService">Service for generating spell data with proper scaling calculations.</param>
        /// <param name="simcVersionService">Service for retrieving game data version information.</param>
        /// <param name="simcTalentService">Service for fetching and processing talent data.</param>
        /// <param name="cacheService">Service for caching and retrieving raw game data files.</param>
        internal SimcGenerationService(ILogger<SimcGenerationService> logger,
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SimcGenerationService"/> class with automatic service initialization.
        /// This constructor creates all necessary internal services and is suitable for standalone usage.
        /// </summary>
        /// <param name="loggerFactory">Logger factory for creating logger instances for internal services.</param>
        /// <remarks>
        /// This constructor will automatically initialize all required services including data extraction,
        /// caching, parsing, and generation services with the provided logger factory.
        /// </remarks>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SimcGenerationService"/> class with no logging.
        /// This is the simplest constructor for basic usage scenarios.
        /// </summary>
        /// <remarks>
        /// This constructor uses a <see cref="NullLoggerFactory"/> which means no logging output will be generated.
        /// Use one of the other constructors if you need logging capabilities.
        /// </remarks>
        public SimcGenerationService()
            : this(NullLoggerFactory.Instance)
        {

        }

        /// <summary>
        /// Generates a complete SimC profile from a collection of profile lines.
        /// </summary>
        /// <param name="profileString">A list of individual lines from a SimC profile export, such as character data, items, talents, etc.</param>
        /// <returns>
        /// A fully populated <see cref="SimcProfile"/> with parsed character information,
        /// enriched item data including stats and effects, and talent details.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="profileString"/> is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the profile string is invalid or produces no results.</exception>
        /// <remarks>
        /// <para>
        /// This method performs the following operations:
        /// <list type="number">
        /// <item><description>Parses the profile lines to extract character, item, and talent data</description></item>
        /// <item><description>For each item: fetches base item data, applies bonus IDs, adds gems, and calculates final stats</description></item>
        /// <item><description>For each item effect: generates spell data with proper scaling based on item level and quality</description></item>
        /// <item><description>For each talent: retrieves spell ID, name, and rank information</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The resulting profile contains both the raw parsed data (<see cref="SimcProfile.ParsedProfile"/>)
        /// and enriched generated data (<see cref="SimcProfile.GeneratedItems"/> and <see cref="SimcProfile.Talents"/>).
        /// </para>
        /// </remarks>
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

            // Populate the details for each talent
            foreach (var talent in newProfile.ParsedProfile.Talents)
            {
                var newTalent = await _simcTalentService.GetTalentDataAsync(talent.TalentId, talent.Rank);
                if (newTalent != null)
                    newProfile.Talents.Add(newTalent);
            }

            return newProfile;
        }

        /// <summary>
        /// Generates a complete SimC profile from a single multi-line string.
        /// This is a convenience overload that splits the string into lines before processing.
        /// </summary>
        /// <param name="profileString">
        /// A multi-line string containing the complete SimC profile export.
        /// Lines can be separated by any combination of \r\n, \r, or \n.
        /// </param>
        /// <returns>
        /// A fully populated <see cref="SimcProfile"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="profileString"/> is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the profile string is invalid or produces no results.</exception>
        /// <remarks>
        /// <para>
        /// This method is useful when reading SimC profile data from a file or API response as a single string.
        /// It automatically handles different line ending formats (Windows, Unix, Mac) and empty lines.
        /// </para>
        /// <para>
        /// The string is split using <see cref="StringSplitOptions.RemoveEmptyEntries"/> to ignore blank lines.
        /// </para>
        /// </remarks>
        /// <seealso cref="GenerateProfileAsync(List{string})"/>
        public async Task<SimcProfile> GenerateProfileAsync(string profileString)
        {
            if (string.IsNullOrEmpty(profileString))
            {
                _logger?.LogWarning("Incoming profileString is empty.");
                throw new ArgumentNullException(nameof(profileString));
            }

            _logger?.LogInformation("Splitting a string with {Length} character(s)", profileString.Length);

            var lines = profileString.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries).ToList();

            _logger?.LogInformation("Created {Count} lines to be processed.", lines.Count);

            return await GenerateProfileAsync(lines);
        }

        /// <summary>
        /// Generates a detailed item with all stats, effects, gems, and bonuses applied.
        /// </summary>
        /// <param name="options">
        /// Configuration object specifying the item ID and optional modifiers including item level,
        /// bonus IDs, gem IDs, crafted stats, quality, and drop level.
        /// </param>
        /// <returns>
        /// A <see cref="SimcItem"/> with:
        /// <list type="bullet">
        /// <item><description>Base item properties (name, class, inventory type)</description></item>
        /// <item><description>Calculated stats from item mods, scaled to the specified item level</description></item>
        /// <item><description>Socket information and gem bonuses</description></item>
        /// <item><description>All item effects with complete spell data including scaling</description></item>
        /// <item><description>Quality-based stat budgets</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="SimcItemOptions.ItemId"/> is 0 or the item ID is not found in the game data.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method processes items through multiple stages:
        /// <list type="number">
        /// <item><description>Fetches base item data from SimulationCraft's game data</description></item>
        /// <item><description>Applies item level scaling to adjust base stats</description></item>
        /// <item><description>Processes bonus IDs which may add stats, change quality, add sockets, or modify item level</description></item>
        /// <item><description>Applies gems to sockets and calculates their stat contributions</description></item>
        /// <item><description>Handles crafted stats for player-crafted items</description></item>
        /// <item><description>Generates spell effects for on-use, on-equip, and proc effects with proper scaling</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// If both <see cref="SimcItemOptions.ItemLevel"/> and bonus IDs that modify item level are provided,
        /// the explicit item level takes precedence and bonus ID item level modifications are ignored.
        /// </para>
        /// </remarks>
        /// <seealso cref="SimcItemOptions"/>
        /// <seealso cref="SimcItem"/>
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

        /// <summary>
        /// Generates a spell with proper scaling based on either item level or player level.
        /// </summary>
        /// <param name="options">
        /// Configuration object specifying the spell ID and scaling parameters.
        /// Use <see cref="SimcSpellOptions.ItemLevel"/> for item-based spells (trinkets, enchants),
        /// or <see cref="SimcSpellOptions.PlayerLevel"/> for player-based spells (racials, class abilities).
        /// </param>
        /// <returns>
        /// A <see cref="SimcSpell"/> with:
        /// <list type="bullet">
        /// <item><description>Basic spell properties (name, school, cooldown, duration, etc.)</description></item>
        /// <item><description>All spell effects with properly scaled coefficients and values</description></item>
        /// <item><description>Proc information (RPPM, proc chance, internal cooldown)</description></item>
        /// <item><description>Power costs scaled to player level or spec</description></item>
        /// <item><description>Scaling budgets for effects that scale with item level or player level</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// There are two types of spell scaling handled by this method:
        /// </para>
        /// <para>
        /// <b>Item Scaling</b> (when <see cref="SimcSpellOptions.ItemLevel"/> is non-zero):
        /// Used for spells from items like trinkets, weapons, and enchants. Scaling is based on:
        /// <list type="bullet">
        /// <item><description>Item level - higher level items have stronger effects</description></item>
        /// <item><description>Item quality - epic items scale better than rare items</description></item>
        /// <item><description>Inventory type - different slots have different scaling budgets</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Player Scaling</b> (when <see cref="SimcSpellOptions.ItemLevel"/> is 0):
        /// Used for player abilities, racials, and class spells. Scaling is based on:
        /// <list type="bullet">
        /// <item><description>Player level - abilities scale as the character levels up</description></item>
        /// <item><description>Class and spec - different classes have different scaling coefficients</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Each spell effect contains a <see cref="SimcSpellEffect.ScaleBudget"/> property that can be
        /// multiplied with the effect's <see cref="SimcSpellEffect.Coefficient"/> or 
        /// <see cref="SimcSpellEffect.SpCoefficient"/> to get the final scaled value.
        /// </para>
        /// </remarks>
        /// <seealso cref="SimcSpellOptions"/>
        /// <seealso cref="SimcSpell"/>
        /// <seealso cref="SimcSpellEffect"/>
        public async Task<SimcSpell> GenerateSpellAsync(SimcSpellOptions options)
        {
            SimcSpell spell;

            if (options.ItemLevel != 0)
            {
                spell = await _simcSpellCreationService.GenerateItemSpellAsync(options);
            }
            else
            {
                spell = await _simcSpellCreationService.GeneratePlayerSpellAsync(options);
            }

            return spell;
        }

        /// <summary>
        /// Retrieves the game data version string from SimulationCraft's data files.
        /// </summary>
        /// <returns>
        /// A version string in the format "Major.Minor.Patch.Build" 
        /// (e.g., "10.2.6.53840"), representing the World of Warcraft client version that
        /// the current game data was extracted from.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The version is extracted from SimulationCraft's CLIENT_DATA_WOW_VERSION or 
        /// PTR_CLIENT_DATA_WOW_VERSION preprocessor definitions, depending on whether
        /// <see cref="UsePtrData"/> is enabled.
        /// </para>
        /// </remarks>
        /// <seealso cref="UsePtrData"/>
        /// <seealso cref="UseBranchName"/>
        public async Task<string> GetGameDataVersionAsync()
        {
            return await _simcVersionService.GetGameDataVersionAsync();
        }

        /// <summary>
        /// Retrieves all available talents for a specific class and specialisation.
        /// </summary>
        /// <param name="classId">
        /// The numeric class ID (1-13). Examples: 1=Warrior, 2=Paladin, 5=Priest, 11=Druid, etc.
        /// See SimulationCraft's class enumeration for complete mapping.
        /// </param>
        /// <param name="specId">
        /// The numeric specialization ID. Examples: 256=Discipline Priest, 257=Holy Priest, 258=Shadow Priest.
        /// See SimulationCraft's specialization enumeration for complete mapping.
        /// </param>
        /// <returns>
        /// A list of all <see cref="SimcTalent"/> objects available to the
        /// specified class and specialization, including:
        /// <list type="bullet">
        /// <item><description>Talent name and spell ID</description></item>
        /// <item><description>Trait node entry ID for talent tree positioning</description></item>
        /// <item><description>Maximum ranks available for the talent</description></item>
        /// <item><description>Position information (row, column) in the talent tree</description></item>
        /// </list>
        /// Returns an empty list if no talents are found for the specified combination.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The talent data includes both class-wide talents and specialization-specific talents.
        /// Talents are returned without rank information - use the <see cref="SimcTalent.SpellId"/> 
        /// to fetch detailed spell information for each rank if needed.
        /// </para>
        /// <para>
        /// Note: This method returns the raw talent tree structure. It does not validate talent
        /// prerequisites, choice nodes, or point requirements.
        /// </para>
        /// </remarks>
        /// <seealso cref="SimcTalent"/>
        public async Task<List<SimcTalent>> GetAvailableTalentsAsync(int classId, int specId)
        {
            return await _simcTalentService.GetAvailableTalentsAsync(classId, specId);
        }

        /// <summary>
        /// Gets or sets whether to use PTR (Public Test Realm) data files instead of live game data.
        /// Defaults to <c>false</c> (uses live data).
        /// </summary>
        /// <value>
        /// <c>true</c> to use PTR data files with the "_ptr" suffix; <c>false</c> to use live game data files.
        /// </value>
        /// <remarks>
        /// <para>
        /// SimulationCraft maintains separate data files for the live game servers and the PTR servers
        /// within the same GitHub branch. PTR data files have a "_ptr" suffix (e.g., "sc_spell_data_ptr.inc"
        /// vs "sc_spell_data.inc").
        /// </para>
        /// <para>
        /// <b>Important:</b> Changing this property will clear all cached data to ensure consistency.
        /// This may cause a temporary performance impact as data files are re-downloaded and re-parsed.
        /// </para>
        /// </remarks>
        /// <seealso cref="UseBranchName"/>
        /// <seealso cref="GetGameDataVersionAsync"/>
        public bool UsePtrData
        {
            get => _cacheService.UsePtrData;
            set => _cacheService.SetUsePtrData(value);
        }

        /// <summary>
        /// Gets or sets the GitHub branch name to use when fetching game data files from SimulationCraft's repository.
        /// Defaults to <c>"thewarwithin"</c>.
        /// </summary>
        /// <value>
        /// The branch name as a string (e.g., "midnight", "thewarwithin", "dragonflight").
        /// </value>
        /// <remarks>
        /// <para>
        /// SimulationCraft maintains separate branches for different World of Warcraft expansions.
        /// Recent branch names include:
        /// <list type="bullet">
        /// <item><description>"midnight" - Midnight (WoW 12.x)</description></item>
        /// <item><description>"thewarwithin" - The War Within (WoW 11.x)</description></item>
        /// <item><description>"dragonflight" - Dragonflight (WoW 10.x)</description></item>
        /// <item><description>"shadowlands" - Shadowlands (WoW 9.x)</description></item>
        /// <item><description>"bfa-dev" - Battle for Azeroth (WoW 8.x)</description></item>
        /// <item><description>"legion-dev" - Legion (WoW 7.x)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Important:</b> Changing this property will clear all cached data to prevent mixing
        /// data from different game versions. This may cause a temporary performance impact as
        /// data files are re-downloaded and re-parsed.
        /// </para>
        /// <para>
        /// Data files are fetched from: 
        /// <c>https://raw.githubusercontent.com/simulationcraft/simc/{BranchName}/engine/dbc/generated/</c>
        /// </para>
        /// </remarks>
        /// <seealso cref="UsePtrData"/>
        /// <seealso cref="GetGameDataVersionAsync"/>
        public string UseBranchName
        {
            get => _cacheService.UseBranchName;
            set => _cacheService.SetUseBranchName(value);
        }

        /// <summary>
        /// Clears the cached data for profiles, items, spells, and talents.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method will remove all cached entries associated with the current character or profile.
        /// It is useful for refreshing data after game updates, profile changes, or debugging.
        /// </para>
        /// <para>
        /// After clearing the cache, the next profile generation will
        /// re-download and re-process all required data from SimulationCraft's data files.
        /// </para>
        /// </remarks>
        /// <seealso cref="SimcProfileParser.Interfaces.ICacheService.ClearCacheAsync"/>
        public async Task ClearCacheAsync()
        {
            await _cacheService.ClearCacheAsync();
        }
    }
}
