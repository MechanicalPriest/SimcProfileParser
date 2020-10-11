﻿using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    public class SimcItemCreationServiceTests
    {
        private ILoggerFactory _loggerFactory;

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

            _loggerFactory = LoggerFactory.Create(builder => builder
                .AddSerilog()
                .AddFilter(level => level >= LogLevel.Trace));

            // Load a data file
            var testFile = @"RawData" + Path.DirectorySeparatorChar + "Ardaysauk.simc";
            var testFileContents = await File.ReadAllLinesAsync(testFile);
            var testFileString = new List<string>(testFileContents);

            // Create a new profile service
            var simcParser = new SimcParserService(_loggerFactory.CreateLogger<SimcParserService>());
            ParsedProfile = simcParser.ParseProfileAsync(testFileString);

            
        }
        [Test]
        public void ICS_Test()
        {
            // Arrange
            IRawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(_loggerFactory.CreateLogger<RawDataExtractionService>());
            ICacheService cacheService = new CacheService(rawDataExtractionService, _loggerFactory.CreateLogger<CacheService>());
            var utilityService = new SimcUtilityService(
                cacheService,
                _loggerFactory.CreateLogger<SimcUtilityService>());

            var spellCreationService = new SimcSpellCreationService(
                cacheService,
                utilityService,
                _loggerFactory.CreateLogger<SimcSpellCreationService>());

            ISimcItemCreationService ics = new SimcItemCreationService(
                cacheService,
                spellCreationService,
                utilityService,
                _loggerFactory.CreateLogger<SimcItemCreationService>());

            // Act
            var items = new List<SimcItem>();
            foreach (var parsedItemData in ParsedProfile.Items)
            {
                var item = ics.CreateItem(parsedItemData);
                items.Add(item);
            }

            // Assert
            Assert.IsNotNull(items);
            Assert.NotZero(items.Count);
        }
    }
}
