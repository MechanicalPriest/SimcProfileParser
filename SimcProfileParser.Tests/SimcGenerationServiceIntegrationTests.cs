using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Serilog;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    class SimcGenerationServiceIntegrationTests
    {
        private SimcGenerationService _sgs;
        private List<string> _profileString;

        [OneTimeSetUp]
        public async Task Init()
        {
            // Configure Logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.File("logs" + Path.DirectorySeparatorChar + "SimcProfileParser.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var loggerFactory = LoggerFactory.Create(builder => builder
                .AddSerilog()
                .AddFilter(level => level >= LogLevel.Trace));

            _sgs = new SimcGenerationService(loggerFactory);

            var testFile = @"RawData" + Path.DirectorySeparatorChar + "Ardaysauk.simc";
            var testFileContents = await File.ReadAllLinesAsync(testFile);
            _profileString = new List<string>(testFileContents);
        }

        [Test]
        public async Task SGS_Creates_Profile()
        {
            // Arrange

            // Act
            var profile = await _sgs.GenerateProfileAsync(_profileString);

            // Assert
            ClassicAssert.IsNotNull(profile, "Profile not null");
            ClassicAssert.IsNotNull(profile.ParsedProfile, "Parsed profile not null");
            ClassicAssert.IsNotNull(profile.ParsedProfile.Name);
            ClassicAssert.NotZero(profile.ParsedProfile.Level);
            ClassicAssert.NotZero(profile.GeneratedItems.Count);
            ClassicAssert.IsTrue(profile.GeneratedItems[0].Equipped);
            ClassicAssert.IsNotNull(profile.Talents);
            ClassicAssert.AreEqual(0, profile.Talents.Count);
            //Assert.AreEqual(103775, profile.Talents[0].TraitEntryId);
            //Assert.AreEqual(2050, profile.Talents[0].SpellId);
            //Assert.AreEqual("Holy Word: Serenity", profile.Talents[0].Name);
            //Assert.AreEqual(1, profile.Talents[0].Rank);
            return;
        }

        [Test]
        public async Task SGS_Creates_Item()
        {
            // Arrange
            var itemOptions = new SimcItemOptions()
            {
                ItemId = 177813,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemLevel = 226
            };

            // Act
            var item = await _sgs.GenerateItemAsync(itemOptions);

            // Assert
            ClassicAssert.IsNotNull(item);
        }

        [Test]
        public async Task SGS_Creates_ItemSpell()
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
            var spell = await _sgs.GenerateSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(2, spell.Effects.Count);
            ClassicAssert.AreEqual(25.512510299999999d, spell.Effects[0].ScaleBudget);
            ClassicAssert.AreEqual(460.97500600000001d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(621.39996299999996d, spell.Effects[1].Coefficient);
        }

        [Test]
        public async Task SGS_Creates_PlayerSpell()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 274740,
                PlayerLevel = 60
            };

            // Act
            var spell = await _sgs.GenerateSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1.716, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(258.2211327d, spell.Effects[0].ScaleBudget);
        }

        [Test]
        public async Task SGS_Gets_Game_Version()
        {
            // Arrange

            // Act
            var version = await _sgs.GetGameDataVersionAsync();

            // Assert
            ClassicAssert.IsNotNull(version);
            ClassicAssert.AreEqual("12.", version.Substring(0, 3));
        }
    }
}
