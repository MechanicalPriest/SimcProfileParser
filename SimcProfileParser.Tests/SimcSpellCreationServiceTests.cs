using Microsoft.Extensions.Logging;
using NUnit.Framework;
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
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(2, spell.Effects.Count);
            Assert.AreEqual(41.071998600000001d, spell.Effects[0].ScaleBudget);
            Assert.AreEqual(460.97500600000001d, spell.Effects[0].Coefficient);
            Assert.AreEqual(621.39996299999996d, spell.Effects[1].Coefficient);
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
            Assert.IsNull(spell);
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
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(1, spell.Effects.Count);
            Assert.AreEqual(202.44794180847748d, spell.Effects[0].ScaleBudget);
            Assert.AreEqual(1.65, spell.Effects[0].Coefficient);
            Assert.AreEqual(-7, spell.Effects[0].ScalingType);
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
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(2, spell.Effects.Count);
            Assert.AreEqual(41.071998600000001d, spell.Effects[0].ScaleBudget);
            Assert.AreEqual(460.97500600000001d, spell.Effects[0].Coefficient);
            Assert.AreEqual(621.39996299999996d, spell.Effects[1].Coefficient);
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
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(1, spell.Effects.Count);
            Assert.AreEqual(64.930999760000006d, spell.Effects[0].ScaleBudget);
            Assert.AreEqual(21.946373000000001d, spell.Effects[0].Coefficient);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_Spell_Options()
        {
            // Arrange
            // Use the 
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 274740,
                PlayerLevel = 60
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(95, spell.Effects[0].ScaleBudget);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_With_Power()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 274740,
                PlayerLevel = 60
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(95, spell.Effects[0].ScaleBudget);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_Raw()
        {
            // Arrange
            var playerLevel = 60u;
            var spellId = 274740u;

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(playerLevel, spellId);

            // Assert
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(95, spell.Effects[0].ScaleBudget);
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
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.PowerCosts);
            Assert.LessOrEqual(2, spell.PowerCosts.Count);
            Assert.AreEqual(0.3, spell.PowerCosts.Skip(1).First().Value);
            Assert.AreEqual(137031, spell.PowerCosts.Skip(1).First().Key);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_WithConduitRanks()
        {
            // Arrange
            var playerLevel = 60u;
            var spellId = 340609u;

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(playerLevel, spellId);
            var firstConduitRank = spell.ConduitRanks.FirstOrDefault();

            // Assert
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.ConduitRanks);
            Assert.AreEqual(270, spell.ConduitId);
            Assert.IsNotNull(firstConduitRank);
            Assert.AreEqual(15, firstConduitRank.Value);
            Assert.AreEqual(0, firstConduitRank.Key);
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
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.RppmModifiers);
            Assert.AreEqual(7, spell.RppmModifiers.Count);
            Assert.AreEqual(339343, spell.RppmModifiers[4].SpellId);
            Assert.IsFalse(spell.RppmModifiers[4].RppmIsHasted);
            Assert.IsTrue(spell.RppmModifiers[4].RppmIsSpecModified);
            Assert.AreEqual(257, spell.RppmModifiers[4].RppmSpec);
            Assert.AreEqual(-0.5000, spell.RppmModifiers[4].RppmCoefficient);
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
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.RppmModifiers);
            Assert.AreEqual(1, spell.RppmModifiers.Count);
            Assert.AreEqual(339547, spell.RppmModifiers[0].SpellId);
            Assert.IsTrue(spell.RppmModifiers[0].RppmIsHasted);
            Assert.IsFalse(spell.RppmModifiers[0].RppmIsSpecModified);
            Assert.AreEqual(0, spell.RppmModifiers[0].RppmSpec);
            Assert.AreEqual(1, spell.RppmModifiers[0].RppmCoefficient);
        }
    }
}
