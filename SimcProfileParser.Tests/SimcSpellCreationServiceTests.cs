using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Serilog;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    class SimcSpellCreationServiceTests
    {
        private ILoggerFactory _loggerFactory;
        private SimcSpellCreationService _spellCreationService;

        [OneTimeSetUp]
        public void InitOnce()
        {
            // Configure Logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.File("logs" + Path.DirectorySeparatorChar + "SimcProfileParser.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _loggerFactory = LoggerFactory.Create(builder => builder
                .AddSerilog()
                .AddFilter(level => level >= LogLevel.Trace));

            IRawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(_loggerFactory.CreateLogger<RawDataExtractionService>());
            ICacheService cacheService = new CacheService(rawDataExtractionService, _loggerFactory.CreateLogger<CacheService>());
            var utilityService = new SimcUtilityService(
                cacheService,
                _loggerFactory.CreateLogger<SimcUtilityService>());

            _spellCreationService = new SimcSpellCreationService(
                utilityService,
                _loggerFactory.CreateLogger<SimcSpellCreationService>());
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_Spell_Options()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 226,
                SpellId = 343538,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(2, spell.Effects.Count);
            ClassicAssert.AreEqual(25.512510299999999d, spell.Effects[0].ScaleBudget);
            ClassicAssert.AreEqual(460.97500600000001d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(621.39996299999996d, spell.Effects[1].Coefficient);
        }

        /// <summary>
        /// When an itemspell is attempted to be made and there is no spelldata for it, return an empty
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task SSC_Fails_Create_Item_Invalid_Spell()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 5,
                SpellId = 7823,
                ItemQuality = ItemQuality.ITEM_QUALITY_RARE,
                ItemInventoryType = InventoryType.INVTYPE_HEAD
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNull(spell);
        }

        [Test]
        public async Task SSC_Converts_OneScale_To_SevenScale()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 226,
                SpellId = 344227,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1, spell.Effects.Count);
            ClassicAssert.AreEqual(125.98769760131836d, spell.Effects[0].ScaleBudget);
            ClassicAssert.AreEqual(1.65, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(-7, spell.Effects[0].ScalingType);
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_Raw_Obj()
        {
            // Arrange
            // Use the Brimming Ember Shard spell 343538
            var item = new SimcItem()
            {
                ItemLevel = 226,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                InventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(item, 343538);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(2, spell.Effects.Count);
            ClassicAssert.AreEqual(25.512510299999999d, spell.Effects[0].ScaleBudget);
            ClassicAssert.AreEqual(460.97500600000001d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(621.39996299999996d, spell.Effects[1].Coefficient);
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_Raw_Obj_9()
        {
            // Arrange
            // Use the First Class Healing Distributor spell 352273
            var item = new SimcItem()
            {
                ItemLevel = 226,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                InventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(item, 352273);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1, spell.Effects.Count);
            ClassicAssert.AreEqual(25.512510299999999d, spell.Effects[0].ScaleBudget);
            ClassicAssert.AreEqual(21.946373000000001d, spell.Effects[0].Coefficient);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_Spell_Options()
        {
            // Arrange
            // Use the 
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 274740,
                PlayerLevel = 80
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1.716d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(3828.969615d, spell.Effects[0].ScaleBudget);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_With_Power()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 274740,
                PlayerLevel = 80
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1.716d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(3828.969615d, spell.Effects[0].ScaleBudget);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_With_Invalid_Trigger_Spell()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 34433,
                PlayerLevel = 80
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(15000d, spell.Duration);
            ClassicAssert.AreEqual(180000d, spell.Cooldown);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_Raw()
        {
            // Arrange
            var playerLevel = 70u;
            var spellId = 274740u;

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(playerLevel, spellId);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1.716d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(453.3443671d, spell.Effects[0].ScaleBudget);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_WithPower()
        {
            // Arrange
            var playerLevel = 60u;
            var spellId = 589u;

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(playerLevel, spellId);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.PowerCosts);
            ClassicAssert.LessOrEqual(2, spell.PowerCosts.Count);
            ClassicAssert.AreEqual(0.3, spell.PowerCosts.Skip(1).First().Value);
            ClassicAssert.AreEqual(137031, spell.PowerCosts.Skip(1).First().Key);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_WithConduitRanks()
        {
            // Arrange
            var playerLevel = 80u;
            var spellId = 340609u;

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(playerLevel, spellId);
            var firstConduitRank = spell.ConduitRanks.FirstOrDefault();

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.ConduitRanks);
            ClassicAssert.AreEqual(270, spell.ConduitId);
            ClassicAssert.IsNotNull(firstConduitRank);
            ClassicAssert.AreEqual(15, firstConduitRank.Value);
            ClassicAssert.AreEqual(0, firstConduitRank.Key);
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_RppmSpecModifiers()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 226,
                SpellId = 339343,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.RppmModifiers);
            ClassicAssert.AreEqual(7, spell.RppmModifiers.Count);
            ClassicAssert.AreEqual(339343, spell.RppmModifiers[4].SpellId);
            ClassicAssert.IsFalse(spell.RppmModifiers[4].RppmIsHasted);
            ClassicAssert.IsTrue(spell.RppmModifiers[4].RppmIsSpecModified);
            ClassicAssert.AreEqual(257, spell.RppmModifiers[4].RppmSpec);
            ClassicAssert.AreEqual(-0.5000, spell.RppmModifiers[4].RppmCoefficient);
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_RppmHasteModifiers()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 226,
                SpellId = 339547,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.RppmModifiers);
            ClassicAssert.AreEqual(1, spell.RppmModifiers.Count);
            ClassicAssert.AreEqual(339547, spell.RppmModifiers[0].SpellId);
            ClassicAssert.IsTrue(spell.RppmModifiers[0].RppmIsHasted);
            ClassicAssert.IsFalse(spell.RppmModifiers[0].RppmIsSpecModified);
            ClassicAssert.AreEqual(0, spell.RppmModifiers[0].RppmSpec);
            ClassicAssert.AreEqual(1, spell.RppmModifiers[0].RppmCoefficient);
        }
    }
}
