using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
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
            var testFile = @"RawData" + Path.DirectorySeparatorChar + "Alfouhk.simc";
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

            var ics = new SimcItemCreationService(cacheService,
                _loggerFactory.CreateLogger<SimcItemCreationService>());

            // Act
            var result = ics.CreateItemsFromProfile(ParsedProfile);

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
