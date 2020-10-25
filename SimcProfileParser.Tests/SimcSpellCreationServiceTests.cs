using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            Assert.AreEqual(40, spell.ScaleBudget);
            Assert.AreEqual(294.97500600000001d, spell.Effects[0].Coefficient);
            Assert.AreEqual(455.39999399999999d, spell.Effects[1].Coefficient);
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
            Assert.AreEqual(40, spell.ScaleBudget);
            Assert.AreEqual(294.97500600000001d, spell.Effects[0].Coefficient);
            Assert.AreEqual(455.39999399999999d, spell.Effects[1].Coefficient);
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
            Assert.AreEqual(95, spell.ScaleBudget);
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
            Assert.AreEqual(95, spell.ScaleBudget);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_WithPower()
        {
            // Arrange
            var playerLevel = 60u;
            var spellId = 2061u;

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(playerLevel, spellId);

            // Assert
            Assert.IsNotNull(spell);
            Assert.AreEqual(3.6, spell.PowerCost);
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
            Assert.AreEqual(10, firstConduitRank.Value);
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
