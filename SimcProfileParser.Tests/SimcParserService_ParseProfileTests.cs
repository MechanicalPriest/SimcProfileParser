using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
                .WriteTo.File("logs\\SimcProfileParser.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Load a data file
            var testFile = @"RawData\Hierophant.simc";
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
            Assert.IsNotNull(ParsedProfile);
            Assert.IsNotNull(ParsedProfile.SimcAddonVersion);
            Assert.AreEqual(version, ParsedProfile.SimcAddonVersion);
        }

        [Test]
        public void SPS_Parses_Collection_Date()
        {
            // Arrange
            DateTime.TryParse("2020-09-27 01:41", out DateTime parsedDateTime);

            // Act

            // Assert
            Assert.IsNotNull(ParsedProfile);
            Assert.IsNotNull(ParsedProfile.CollectionDate);
            Assert.AreEqual(parsedDateTime, ParsedProfile.CollectionDate);
        }

        [Test]
        public void SPS_Parses_Character_Name()
        {
            // Arrange
            var charName = "Hierophant";

            // Act

            // Assert
            Assert.IsNotNull(ParsedProfile);
            Assert.IsNotNull(ParsedProfile.Name);
            Assert.AreEqual(charName, ParsedProfile.Name);
        }

        [Test]
        public void SPS_Parses_Level()
        {
            // Arrange
            var level = 60;

            // Act

            // Assert
            Assert.IsNotNull(ParsedProfile);
            Assert.IsNotNull(ParsedProfile.Level);
            Assert.AreEqual(level, ParsedProfile.Level);
        }
    }
}
