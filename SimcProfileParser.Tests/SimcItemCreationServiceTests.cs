using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Serilog;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.Profile;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    public class SimcItemCreationServiceTests
    {
        private ILoggerFactory _loggerFactory;

        ISimcItemCreationService _ics;

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

            var spellCreationService = new SimcSpellCreationService(
                utilityService,
                _loggerFactory.CreateLogger<SimcSpellCreationService>());

            _ics = new SimcItemCreationService(
                cacheService,
                spellCreationService,
                utilityService,
                _loggerFactory.CreateLogger<SimcItemCreationService>());
        }

        [Test]
        public async Task ICS_Parses_Entire_Data_file()
        {
            // Arrange

            // Load a data file
            var testFile = @"RawData" + Path.DirectorySeparatorChar + "Alfouhk.simc";
            var testFileContents = await File.ReadAllLinesAsync(testFile);
            var testFileString = new List<string>(testFileContents);

            // Create a new item creation service
            var simcParser = new SimcParserService(_loggerFactory.CreateLogger<SimcParserService>());
            var parsedProfile = simcParser.ParseProfileAsync(testFileString);

            // Act
            var items = new List<SimcItem>();
            foreach (var parsedItemData in parsedProfile.Items)
            {
                var item = await _ics.CreateItemAsync(parsedItemData);
                items.Add(item);
            }

            // Assert
            ClassicAssert.IsNotNull(items);
            ClassicAssert.NotZero(items.Count);
        }

        [Test]
        public async Task ICS_Builds_Item_From_Options()
        {
            // Arrange
            // # Entropic Convergence Loop (519)
            // # finger1=,id=202572,bonus_id=6652/10532/7981/10335/10884/1576/8767
            //var itemOptions = new SimcItemOptions()
            //{
            //    ItemId = 202572,
            //    Quality = ItemQuality.ITEM_QUALITY_COMMON,
            //    ItemLevel = 415,
            //    BonusIds = new List<int>() { 6652, 10532, 7981, 10335, 10884, 1576, 8767 },
            //};

            // Etchings of the Captive Revenant (519)
            // back =,id = 202573,bonus_id = 6652 / 10874 / 7981 / 10335 / 10884 / 1576 / 8767
            // 702 Armor, 3422 Stamina, 460 Crit, 211 Haste
            var itemOptions = new SimcItemOptions()
            {
                ItemId = 202573,
                Quality = ItemQuality.ITEM_QUALITY_COMMON,
                ItemLevel = 415,
                BonusIds = new List<int>() { 6652, 10874, 7981, 10335, 10884, 1576, 8767 },
            };


            // Act
            var item = await _ics.CreateItemAsync(itemOptions);

            // Assert
            ClassicAssert.IsNotNull(item);
            ClassicAssert.AreEqual(519, item.ItemLevel);
            ClassicAssert.AreEqual(ItemQuality.ITEM_QUALITY_EPIC, item.Quality);
            ClassicAssert.AreEqual(InventoryType.INVTYPE_CLOAK, item.InventoryType);
            ClassicAssert.AreEqual(202573, item.ItemId);

            // Stam
            ClassicAssert.AreEqual(892, item.Mods[0].StatRating);
            ClassicAssert.AreEqual(ItemModType.ITEM_MOD_STAMINA, item.Mods[0].Type);
            // Crit rating
            ClassicAssert.AreEqual(269, item.Mods[1].StatRating);
            ClassicAssert.AreEqual(ItemModType.ITEM_MOD_CRIT_RATING, item.Mods[1].Type);
            // Haste rating
            ClassicAssert.AreEqual(123, item.Mods[2].StatRating);
            ClassicAssert.AreEqual(ItemModType.ITEM_MOD_HASTE_RATING, item.Mods[2].Type);
        }

        [Test]
        public async Task ICS_BonusIds_Override_Quality()
        {
            // Arrange
            // Hopebreakers Badge
            // trinket1=,id=177813,bonus_id=6907/6652/603/7215,drop_level=50
            var itemOptions = new SimcItemOptions()
            {
                ItemId = 177813,
                Quality = ItemQuality.ITEM_QUALITY_RARE,
                ItemLevel = 226,
                BonusIds = new List<int>() { 6907, 6652, 603, 7215 },
                DropLevel = 50
            };

            // Act
            var item = await _ics.CreateItemAsync(itemOptions);

            // Assert
            ClassicAssert.IsNotNull(item);
            ClassicAssert.AreEqual(ItemQuality.ITEM_QUALITY_EPIC, item.Quality);
        }

        [Test]
        public async Task ICS_ItemOptions_Correct_iLvl_Scaling()
        {
            // Arrange
            var itemOptions = new SimcItemOptions()
            {
                ItemId = 181360,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemLevel = 226
            };

            // Act
            var item = await _ics.CreateItemAsync(itemOptions);

            // Assert
            ClassicAssert.IsNotNull(item);
            ClassicAssert.AreEqual(226, item.ItemLevel);
            ClassicAssert.AreEqual(ItemQuality.ITEM_QUALITY_EPIC, item.Quality);
            ClassicAssert.AreEqual(181360, item.ItemId);
            // This will make sure the scale value that's being pulled for spells is using the right
            // item level. 
            ClassicAssert.AreEqual(1.2376681566238403d, item.Effects[0].Spell.CombatRatingMultiplier);
        }

        [Test]
        public async Task ICS_Creates_Item_Crafted_Stats()
        {
            // Arrange
            // Hopebreakers Badge
            // trinket1=,id=177813,bonus_id=6907/6652/603/7215,drop_level=50
            var itemOptions = new SimcItemOptions()
            {
                ItemId = 193516,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemLevel = 392,
                BonusIds = new List<int>() { 8836, 8840, 8902, 8801, 8793 },
                CraftedStatIds = new List<int>() { 36, 40 }
            };

            // Act
            var item = await _ics.CreateItemAsync(itemOptions);

            // Assert
            ClassicAssert.IsNotNull(item);
            ClassicAssert.AreEqual(ItemQuality.ITEM_QUALITY_EPIC, item.Quality);
            ClassicAssert.That(item.Mods.Where(m => m.Type == ItemModType.ITEM_MOD_INTELLECT).Any(), Is.True);
            ClassicAssert.That(item.Mods.Where(m => m.Type == ItemModType.ITEM_MOD_STAMINA).Any(), Is.True);
            ClassicAssert.That(item.Mods.Where(m => m.Type == ItemModType.ITEM_MOD_HASTE_RATING).Any(), Is.True);
            ClassicAssert.That(item.Mods.Where(m => m.Type == ItemModType.ITEM_MOD_VERSATILITY_RATING).Any(), Is.True);
        }



        /// <summary>
        /// Test that fixes #81 - recursion when looking up recursive trigger spells/effects
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task ICS_ItemOptions_Avoids_Recursion()
        {
            // Arrange
            var itemOptions = new SimcItemOptions()
            {
                ItemId = 186423,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemLevel = 226
            };

            // Act
            var item = await _ics.CreateItemAsync(itemOptions);

            // Assert
            ClassicAssert.IsNotNull(item);
            ClassicAssert.AreEqual(226, item.ItemLevel);
            ClassicAssert.AreEqual(ItemQuality.ITEM_QUALITY_EPIC, item.Quality);
            ClassicAssert.AreEqual(186423, item.ItemId);
        }

        [Test]
        public async Task ICS_ItemOptions_Correct_iLvl_Heal_EffectScaling()
        {
            // Arrange
            var itemOptions = new SimcItemOptions()
            {
                ItemId = 178809,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemLevel = 226
            };

            // Act
            var item = await _ics.CreateItemAsync(itemOptions);

            // Assert
            ClassicAssert.IsNotNull(item);
            ClassicAssert.AreEqual(226, item.ItemLevel);
            ClassicAssert.AreEqual(ItemQuality.ITEM_QUALITY_EPIC, item.Quality);
            ClassicAssert.AreEqual(178809, item.ItemId);
            // This will make sure the scale value that's being pulled for spells with healing/damage effects is using the right
            // item level. 
            ClassicAssert.AreEqual(180.9646759d, item.Effects[0].Spell.Effects[0].ScaleBudget);
        }

        [Test]
        public async Task ICS_Builds_Trinket_From_ParsedItem_Secondary_Stat_UseEffect()
        {
            // Arrange
            // Flame of Battle (226)
            // trinket1=,id=181501,bonus_id=6652/7215,drop_level=50
            // 226 ilvl. 77 int, 1211 vers proc for 6s (90s cd)
            var parsedData = new SimcParsedItem()
            {
                ItemId = 181501,
                BonusIds = new ReadOnlyCollection<int>(new List<int>()
                {
                    6652, 7215
                }),
                DropLevel = 50
            };

            // Act
            var item = await _ics.CreateItemAsync(parsedData);

            // Assert
            ClassicAssert.IsNotNull(item);
            ClassicAssert.IsNotNull(item.Effects);
            ClassicAssert.AreEqual(2, item.Effects.Count);
            ClassicAssert.AreEqual(126201, item.Effects[0].EffectId);
            ClassicAssert.IsNotNull(item.Effects[0].Spell);
            ClassicAssert.AreEqual(336841, item.Effects[0].Spell.SpellId);
            ClassicAssert.AreEqual(90000, item.Effects[0].Spell.Cooldown);
            ClassicAssert.AreEqual(12000.0d, item.Effects[0].Spell.Duration);
            ClassicAssert.AreEqual(1.2376681566238403d, item.Effects[0].Spell.CombatRatingMultiplier);
            ClassicAssert.AreEqual(180.9646759d, item.Effects[0].Spell.Effects[0].ScaleBudget);
            ClassicAssert.IsNotNull(item.Effects[0].Spell.Effects);
            ClassicAssert.AreEqual(1, item.Effects[0].Spell.Effects.Count);
            ClassicAssert.AreEqual(2.955178d, item.Effects[1].Spell.Effects[0].Coefficient);
        }

        [Test]
        public async Task ICS_Builds_Trinket_From_ParsedItem_HealDmg_UseEffect()
        {
            // Arrange
            // Brimming Ember Shard (226)
            // trinket1=,id=175733,bonus_id=6706/7215,drop_level=50
            // 226 ilvl. 100 vers, 14866 health over 6s split between allies
            // 12001 damage over 6s split between enemies
            // 40yd beam, 90s cd.
            var parsedData = new SimcParsedItem()
            {
                ItemId = 175733,
                BonusIds = new ReadOnlyCollection<int>(new List<int>()
                {
                    6706, 7215
                }),
                DropLevel = 50
            };

            // Act
            var item = await _ics.CreateItemAsync(parsedData);
            item = await _ics.CreateItemAsync(parsedData);

            // Assert
            ClassicAssert.IsNotNull(item);
            ClassicAssert.IsNotNull(item.Effects);
            ClassicAssert.AreEqual(2, item.Effects.Count);
            // First effect
            ClassicAssert.AreEqual(126207, item.Effects[0].EffectId);
            ClassicAssert.IsNotNull(item.Effects[0].Spell);
            ClassicAssert.AreEqual(336866, item.Effects[0].Spell.SpellId);
            ClassicAssert.AreEqual(90000, item.Effects[0].Spell.Cooldown);
            ClassicAssert.AreEqual(6000, item.Effects[0].Spell.Duration);
            ClassicAssert.AreEqual(1.2376681566238403d, item.Effects[0].Spell.CombatRatingMultiplier);
            ClassicAssert.AreEqual(180.9646759d, item.Effects[0].Spell.Effects[0].ScaleBudget);
            // Second effect
            ClassicAssert.AreEqual(135863, item.Effects[1].EffectId);
            ClassicAssert.IsNotNull(item.Effects[1].Spell);
            ClassicAssert.AreEqual(343538, item.Effects[1].Spell.SpellId);
            ClassicAssert.AreEqual(1.2376681566238403d, item.Effects[1].Spell.CombatRatingMultiplier);
            ClassicAssert.AreEqual(127.8034668d, item.Effects[1].Spell.Effects[0].ScaleBudget);
            ClassicAssert.IsNotNull(item.Effects[1].Spell.Effects);
            ClassicAssert.AreEqual(2, item.Effects[1].Spell.Effects.Count);
            // Second effect's spells first effect
            ClassicAssert.AreEqual(460.97500600000001d, item.Effects[1].Spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(621.39996299999996d, item.Effects[1].Spell.Effects[1].Coefficient);
        }

        [Test]
        public async Task ICS_Builds_Trinket_From_ParsedItem_Primary_ProcEffectt()
        {
            // Arrange
            // Misfiring Centurion Controller (226)
            // trinket1=,id=173349,bonus_id=6706/7215,drop_level=50
            // 226 ilvl. 100 crit, 164 int for 15s proc
            var parsedData = new SimcParsedItem()
            {
                ItemId = 173349,
                BonusIds = new ReadOnlyCollection<int>(new List<int>()
                {
                    6706, 7215
                }),
                DropLevel = 50
            };

            // Act
            var item = await _ics.CreateItemAsync(parsedData);

            // Assert
            ClassicAssert.IsNotNull(item);
            ClassicAssert.IsNotNull(item.Effects);
            ClassicAssert.AreEqual(1, item.Effects.Count);
            ClassicAssert.AreEqual(226, item.ItemLevel);
            // First effect
            ClassicAssert.AreEqual(135894, item.Effects[0].EffectId);
            ClassicAssert.IsNotNull(item.Effects[0].Spell);
            ClassicAssert.AreEqual(344117, item.Effects[0].Spell.SpellId);
            ClassicAssert.AreEqual(1.5, item.Effects[0].Spell.Rppm);
            ClassicAssert.AreEqual(1.2376681566238403d, item.Effects[0].Spell.CombatRatingMultiplier);
            ClassicAssert.AreEqual(180.9646759d, item.Effects[0].Spell.Effects[0].ScaleBudget);
            // First effect's spells first effect trigger spells first effect (lol)
            // This is basically testing that the trigger spell gets linked. This particular spell
            // stores the proc coefficient in the trigger spell and multiplies it by 155.
            // amusingly the previous lines have "trigger spell" lined up vertically.
            ClassicAssert.AreEqual(1.406452, item.Effects[0].Spell.Effects[0].TriggerSpell.Effects[0].Coefficient);
        }

        [Test]
        public void ICS_ParsedData_Invalid_ItemId_Throws()
        {
            // Arrange
            // Glowing Endmire Stinger (226)
            // trinket1=,id=179927,bonus_id=6652/7215,drop_level=50
            var parsedData = new SimcParsedItem()
            {
                ItemId = 12333333,
                BonusIds = new ReadOnlyCollection<int>(new List<int>()
                {
                    6706, 7215
                }),
                DropLevel = 50
            };

            // Act

            // Assert
            ClassicAssert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () => await _ics.CreateItemAsync(parsedData));
        }

        [Test]
        public void ICS_Options_Invalid_ItemId_Throws()
        {
            // Arrange
            // Glowing Endmire Stinger (226)
            // trinket1=,id=179927,bonus_id=6652/7215,drop_level=50
            var itemOptions = new SimcItemOptions()
            {
                ItemId = 12333333,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemLevel = 226
            };

            // Act

            // Assert 
            ClassicAssert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () => await _ics.CreateItemAsync(itemOptions));
        }

        [Test]
        public async Task ICS_Ilevel_Forces_Item_Level()
        {
            // Arrange
            var itemOptions = new SimcParsedItem()
            {
                Slot = "off_hand",
                ItemId = 178478,
                BonusIds = new List<int>()
                {
                    7150, 1507, 6646
                },
                Equipped = true,
                ItemLevel = 138
            };

            // Act
            var item = await _ics.CreateItemAsync(itemOptions);

            // Assert 
            ClassicAssert.IsNotNull(item);
            ClassicAssert.AreEqual(178478, item.ItemId);
            ClassicAssert.AreEqual(138, item.ItemLevel);
        }

        [Test]
        public async Task ICS_Creates_Premade_Ilvl_Scaling()
        {
            // Arrange
            var itemOptions = new SimcParsedItem()
            {
                Slot = "trinket1",
                ItemId = 193748,
                BonusIds = new List<int>()
                {
                    8967, 7977, 6652, 1617, 8767
                },
                Equipped = true,
            };

            // Act
            var item = await _ics.CreateItemAsync(itemOptions);

            // Assert 
            ClassicAssert.IsNotNull(item);
            ClassicAssert.AreEqual(193748, item.ItemId);
            ClassicAssert.AreEqual(395, item.ItemLevel);
            ClassicAssert.AreEqual(1, item.Mods.Count);
            ClassicAssert.AreEqual(179, item.Mods[0].StatRating);
        }

        [Test]
        public async Task ICS_Creates_Premade_Ilvl_Scaling_HealEffect()
        {
            // Arrange
            var itemOptions = new SimcParsedItem()
            {
                Slot = "trinket1",
                ItemId = 194307,
                BonusIds = new List<int>()
                {
                    7979, 6652, 6652, 8767
                },
                Equipped = true,
            };

            // Act
            var item = await _ics.CreateItemAsync(itemOptions);

            // Assert 
            ClassicAssert.IsNotNull(item);
            ClassicAssert.AreEqual(194307, item.ItemId);
            ClassicAssert.AreEqual(398, item.ItemLevel);
            ClassicAssert.AreEqual(1, item.Mods.Count);
            ClassicAssert.AreEqual(325, item.Mods[0].StatRating);
        }
    }
}
