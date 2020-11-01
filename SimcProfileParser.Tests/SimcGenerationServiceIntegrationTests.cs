using NUnit.Framework;
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
            _sgs = new SimcGenerationService();

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
            Assert.IsNotNull(profile, "Profile not null");
            Assert.IsNotNull(profile.ParsedProfile, "Parsed profile not null");
            Assert.IsNotNull(profile.ParsedProfile.Name);
            Assert.NotZero(profile.ParsedProfile.Level);
            Assert.NotZero(profile.GeneratedItems.Count);
            Assert.IsTrue(profile.GeneratedItems[0].Equipped);
            Assert.NotZero(profile.ParsedProfile.Conduits.Count);
            Assert.NotZero(profile.ParsedProfile.Conduits[0].SpellId);
            Assert.NotZero(profile.ParsedProfile.Soulbinds.Count);
            Assert.NotZero(profile.ParsedProfile.Soulbinds[0].SocketedConduits.Count);
            Assert.NotZero(profile.ParsedProfile.Soulbinds[0].SocketedConduits[0].SpellId);
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
            Assert.IsNotNull(item);
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
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(2, spell.Effects.Count);
            Assert.AreEqual(40, spell.ScaleBudget);
            Assert.AreEqual(294.97500600000001d, spell.Effects[0].Coefficient);
            Assert.AreEqual(455.39999399999999d, spell.Effects[1].Coefficient);
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
            Assert.IsNotNull(spell);
            Assert.IsNotNull(spell.Effects);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(1.32, spell.Effects[0].Coefficient);
            Assert.AreEqual(95, spell.ScaleBudget);
        }

        [Test]
        public async Task SGS_Gets_Game_Version()
        {
            // Arrange

            // Act
            var version = await _sgs.GetGameDataVersionAsync();

            // Assert
            Assert.IsNotNull(version);
            Assert.AreEqual("9.", version.Substring(0, 2));
        }
    }
}
