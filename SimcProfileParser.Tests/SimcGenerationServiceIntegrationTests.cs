using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Serilog;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Assert.That(profile.Talents, Is.Not.Empty);
            Assert.That(profile.Talents, Has.All.Matches<SimcTalent>(t => t.SpellId > 0 && !string.IsNullOrEmpty(t.Name) && t.Rank > 0));
            return;
        }

        [Test]
        public async Task SGS_Generated_Items_Have_Slots()
        {
            // Arrange

            // Act
            var profile = await _sgs.GenerateProfileAsync(_profileString);

            // Assert
            Assert.That(profile.GeneratedItems.Select(i => i.Slot),
                Is.EqualTo(profile.ParsedProfile.Items.Select(i => i.Slot)));
            Assert.That(profile.GeneratedItems.Select(i => i.Slot), Has.None.Null);
        }

        [Test]
        public async Task SGS_Gets_Class_Base_Stats()
        {
            // Arrange
            // Priest (5) at level 90

            // Act
            var stats = await _sgs.GetClassBaseStatsAsync(5, 90);

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That((stats.Strength, stats.Agility, stats.Stamina, stats.Intellect), Is.EqualTo((360, 515, 4600, 620)));
        }

        [Test]
        [TestCase(5, 91, TestName = "SGS_Class_Base_Stats_Unfilled_Level_Is_Null")]
        [TestCase(5, 0, TestName = "SGS_Class_Base_Stats_Level_Zero_Is_Null")]
        [TestCase(99, 90, TestName = "SGS_Class_Base_Stats_Unknown_Class_Is_Null")]
        public async Task SGS_Class_Base_Stats_Without_Data_Is_Null(int classId, int level)
        {
            // Arrange

            // Act
            var stats = await _sgs.GetClassBaseStatsAsync(classId, level);

            // Assert
            Assert.That(stats, Is.Null);
        }

        [Test]
        [TestCase(7, -3, 1, -1, 3, TestName = "SGS_Race_Base_Stats_Gnome")]
        [TestCase(1, 0, 0, 0, 0, TestName = "SGS_Race_Base_Stats_Human")]
        [TestCase(16, 0, 0, 0, 0, TestName = "SGS_Race_Base_Stats_Dracthyr_Horde")]
        [TestCase(17, 0, 0, 0, 0, TestName = "SGS_Race_Base_Stats_Dracthyr_Alliance")]
        [TestCase(12, 2, -2, 1, -1, TestName = "SGS_Race_Base_Stats_Dark_Iron_Dwarf")]
        [TestCase(0, 0, 0, 0, 0, TestName = "SGS_Race_Base_Stats_No_Race")]
        public async Task SGS_Gets_Race_Base_Stats(int raceId, int strength, int agility, int stamina, int intellect)
        {
            // Arrange
            // Race IDs are simc's, the same as SimcParsedProfile.RaceId

            // Act
            var stats = await _sgs.GetRaceBaseStatsAsync(raceId);

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That((stats.Strength, stats.Agility, stats.Stamina, stats.Intellect), Is.EqualTo((strength, agility, stamina, intellect)));
        }

        [Test]
        public async Task SGS_Race_Base_Stats_Unknown_Race_Is_Null()
        {
            // Arrange

            // Act
            var stats = await _sgs.GetRaceBaseStatsAsync(999);

            // Assert
            Assert.That(stats, Is.Null);
        }

        [Test]
        public async Task SGS_Gets_Base_Mana()
        {
            // Arrange
            // Priest (5) at level 90

            // Act
            var mana = await _sgs.GetBaseManaAsync(5, 90);
            var unknownLevel = await _sgs.GetBaseManaAsync(5, 200);

            // Assert
            Assert.That(mana, Is.EqualTo(250000));
            Assert.That(unknownLevel, Is.Null);
        }

        [Test]
        public async Task SGS_Gets_Combat_Ratings()
        {
            // Arrange

            // Act
            var ratings = await _sgs.GetCombatRatingsAsync(90);
            var unknownLevel = await _sgs.GetCombatRatingsAsync(200);

            // Assert
            Assert.That(ratings, Is.Not.Null);
            Assert.That(ratings.CritSpell, Is.EqualTo(46));
            Assert.That(ratings.HasteSpell, Is.EqualTo(44));
            Assert.That(ratings.Mastery, Is.EqualTo(46));
            Assert.That(ratings.VersatilityHealingDone, Is.EqualTo(54));
            Assert.That(ratings.Leech, Is.EqualTo(69).Within(0.01));
            Assert.That(ratings.Speed, Is.EqualTo(11.5).Within(0.01));
            Assert.That(ratings.Avoidance, Is.EqualTo(36.8).Within(0.01));
            Assert.That(unknownLevel, Is.Null);
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
            ClassicAssert.AreEqual(127.8034668d, spell.Effects[0].ScaleBudget);
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
                PlayerLevel = 90
            };

            // Act
            var spell = await _sgs.GenerateSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1.716, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(131.08048629999999d, spell.Effects[0].ScaleBudget);
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

        [Test]
        public async Task SGS_ClearsCache()
        {
            // Arrange: Warm up by generating something that touches the cache
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel =226,
                SpellId =343538,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };
            var spell = await _sgs.GenerateSpellAsync(spellOptions);
            ClassicAssert.IsNotNull(spell);

            // Act: clear the cache
            await _sgs.ClearCacheAsync();

            // Assert: call again should still work (re-hydrates cache)
            var spell2 = await _sgs.GenerateSpellAsync(spellOptions);
            ClassicAssert.IsNotNull(spell2);
        }
    }
}
