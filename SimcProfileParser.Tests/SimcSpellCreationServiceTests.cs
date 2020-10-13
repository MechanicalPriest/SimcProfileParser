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
using System.Text;

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
                cacheService,
                utilityService,
                _loggerFactory.CreateLogger<SimcSpellCreationService>());
        }

        [Test]
        public void SSC_Creates_Item_Spell_Spell_Options()
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
            var spell = _spellCreationService.GenerateItemSpell(spellOptions);

            // Assert
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(2, spell.Effects.Count);
            Assert.AreEqual(40, spell.ScaleBudget);
            Assert.AreEqual(300.020416, spell.Effects[0].Coefficient);
            Assert.AreEqual(371.653076, spell.Effects[1].Coefficient);
        }

        [Test]
        public void SSC_Creates_Item_Spell_Raw_Obj()
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
            var spell = _spellCreationService.GenerateItemSpell(item, 343538);

            // Assert
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(2, spell.Effects.Count);
            Assert.AreEqual(40, spell.ScaleBudget);
            Assert.AreEqual(300.020416, spell.Effects[0].Coefficient);
            Assert.AreEqual(371.653076, spell.Effects[1].Coefficient);
        }

        [Test]
        public void SSC_Creates_Player_Spell_Spell_Options()
        {
            // Arrange
            // Use the 
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 274740,
                PlayerLevel = 60
            };

            // Act
            var spell = _spellCreationService.GeneratePlayerSpell(spellOptions);

            // Assert
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(95, spell.ScaleBudget);
        }

        [Test]
        public void SSC_Creates_Player_Spell_Raw()
        {
            // Arrange
            var playerLevel = 60u;
            var spellId = 274740u;

            // Act
            var spell = _spellCreationService.GeneratePlayerSpell(playerLevel, spellId);

            // Assert
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(95, spell.ScaleBudget);
        }
    }
}
