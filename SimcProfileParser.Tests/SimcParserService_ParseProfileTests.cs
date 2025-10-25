using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Serilog;
using SimcProfileParser.Model.Profile;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    class SimcParserService_ParseProfileTests
    {
        SimcParsedProfile ParsedProfile { get; set; }

        [OneTimeSetUp]
        public async Task InitOnce()
        {
            // Configure Logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.File("logs" + Path.DirectorySeparatorChar + "SimcProfileParser.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Load a data file
            var testFile = @"RawData" + Path.DirectorySeparatorChar + "Hierophant.simc";
            var testFileContents = await File.ReadAllLinesAsync(testFile);
            var testFileString = new List<string>(testFileContents);

            // Create a new profile service
            using var loggerFactory = LoggerFactory.Create(builder => builder
                .AddSerilog()
                .AddFilter(level => level >= LogLevel.Trace));
            var logger = loggerFactory.CreateLogger<SimcParserService>();
            var simcParser = new SimcParserService(logger);

            ParsedProfile = simcParser.ParseProfileAsync(testFileString);
        }


        [Test]
        public void SPS_Parses_Version()
        {
            // Arrange
            var version = "9.0.1-alpha-10";

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.SimcAddonVersion);
            ClassicAssert.AreEqual(version, ParsedProfile.SimcAddonVersion);
        }

        [Test]
        public void SPS_Parses_Collection_Date()
        {
            // Arrange
            DateTime.TryParse("2020-09-27 01:41", out DateTime parsedDateTime);

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.CollectionDate);
            ClassicAssert.AreEqual(parsedDateTime, ParsedProfile.CollectionDate);
        }

        [Test]
        public void SPS_Parses_Character_Name()
        {
            // Arrange
            var charName = "Hierophant";

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Name);
            ClassicAssert.AreEqual(charName, ParsedProfile.Name);
        }

        [Test]
        public void SPS_Parses_Level()
        {
            // Arrange
            var level = 60;

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Level);
            ClassicAssert.AreEqual(level, ParsedProfile.Level);
        }

        [Test]
        public void SPS_Parses_Race()
        {
            // Arrange
            var race = "undead";
            var raceId = (int)Race.Undead;

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Race);
            ClassicAssert.AreEqual(race, ParsedProfile.Race);
            ClassicAssert.AreEqual(raceId, ParsedProfile.RaceId);
        }

        [Test]
        public void SPS_Parses_Region()
        {
            // Arrange
            var region = "us";

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Region);
            ClassicAssert.AreEqual(region, ParsedProfile.Region);
        }

        [Test]
        public void SPS_Parses_Server()
        {
            // Arrange
            var server = "torghast";

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Server);
            ClassicAssert.AreEqual(server, ParsedProfile.Server);
        }

        [Test]
        public void SPS_Parses_Role()
        {
            // Arrange
            var role = "attack";

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Role);
            ClassicAssert.AreEqual(role, ParsedProfile.Role);
        }

        [Test]
        public void SPS_Parses_Spec()
        {
            // Arrange
            var spec = "holy";
            var specId = 257;

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Spec);
            ClassicAssert.AreEqual(spec, ParsedProfile.Spec);
            ClassicAssert.AreEqual(specId, ParsedProfile.SpecId);
        }

        [Test]
        public void SPS_Parses_Renown()
        {
            // Arrange
            var renown = 40;

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Renown);
            ClassicAssert.AreEqual(renown, ParsedProfile.Renown);
        }

        [Test]
        public void SPS_Parses_Talents()
        {
            // Arrange
            
            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Talents);
            ClassicAssert.NotZero(ParsedProfile.Talents.Count);
            ClassicAssert.AreEqual(43, ParsedProfile.Talents.Count);
            ClassicAssert.AreEqual(92811, ParsedProfile.Talents[0].TalentId);
            ClassicAssert.AreEqual(1, ParsedProfile.Talents[0].Rank);
            ClassicAssert.AreEqual(92812, ParsedProfile.Talents[1].TalentId);
            ClassicAssert.AreEqual(2, ParsedProfile.Talents[1].Rank);
        }

        [Test]
        public void SPS_Parses_Class()
        {
            // Arrange
            var className = "priest";
            var classId = 5;

            // Act

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Class);
            ClassicAssert.AreEqual(className, ParsedProfile.Class);
            ClassicAssert.AreEqual(classId, ParsedProfile.ClassId);
        }

        [Test]
        public void SPS_Parses_Professions()
        {
            // Arrange
            // professions=tailoring=1/jewelcrafting=1
            var professions = new List<SimcParsedProfession>()
            {
                new SimcParsedProfession()
                {
                    Name = "tailoring",
                    Level = 1
                },
                new SimcParsedProfession()
                {
                    Name = "jewelcrafting",
                    Level = 1
                }
            };

            // Act
            var expectedProfessions = JsonSerializer.Serialize(professions);
            var actualProfessions = JsonSerializer.Serialize(ParsedProfile.Professions);

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Professions);
            ClassicAssert.NotZero(ParsedProfile.Professions.Count);
            ClassicAssert.AreEqual(expectedProfessions, actualProfessions);
        }


        [Test]
        public void SPS_Parses_Items()
        {
            // Arrange
            // back=,id=178301,enchant_id=6204,bonus_id=6788/1487/6646
            var fourthItem = new SimcParsedItem()
            {
                Slot = "back",
                ItemId = 178301,
                EnchantId = 6204,
                BonusIds = new List<int>()
                {
                    6788, 1487, 6646
                },
                Equipped = true
            };

            // Act
            var expectedItem = JsonSerializer.Serialize(fourthItem);
            var actualItem = JsonSerializer.Serialize(ParsedProfile.Items[3]);

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Items);
            ClassicAssert.NotZero(ParsedProfile.Items.Count);
            ClassicAssert.AreEqual(expectedItem, actualItem);

        }


        [Test]
        public void SPS_Parses_ItemLevelSpecific()
        {
            // Arrange
            // back=,id=178301,enchant_id=6204,bonus_id=6788/1487/6646
            var sixteenthItem = new SimcParsedItem()
            {
                Slot = "off_hand",
                ItemId = 178478,
                BonusIds = new List<int>()
                {
                    7150, 1507, 6646
                },
                Equipped = true,
                ItemLevel = 190
            };

            // Act
            var expectedItem = JsonSerializer.Serialize(sixteenthItem);
            var actualItem = JsonSerializer.Serialize(ParsedProfile.Items[15]);

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Items);
            ClassicAssert.NotZero(ParsedProfile.Items.Count);
            ClassicAssert.AreEqual(expectedItem, actualItem);

        }


        [Test]
        public void SPS_Parses_CraftedItemStats()
        {
            // Arrange
            // back=,id=178301,enchant_id=6204,bonus_id=6788/1487/6646
            var seventhItem = new SimcParsedItem()
            {
                Slot = "waist",
                ItemId = 193516,
                BonusIds = new List<int>()
                {
                    8836, 8840, 8902, 8801, 8793
                },
                CraftedStatIds = new List<int>()
                {
                    36, 40
                },
                Equipped = true,
                ItemLevel = 392
            };

            // Act
            var expectedItem = JsonSerializer.Serialize(seventhItem);
            var actualItem = JsonSerializer.Serialize(ParsedProfile.Items[7]);

            // Assert
            ClassicAssert.IsNotNull(ParsedProfile);
            ClassicAssert.IsNotNull(ParsedProfile.Items);
            ClassicAssert.NotZero(ParsedProfile.Items.Count);
            ClassicAssert.AreEqual(expectedItem, actualItem);

        }
    }
}
