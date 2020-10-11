using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Serilog;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    public class SimcParserServiceTests
    {
        List<string> TestFileString { get; set; }
        ISimcParserService SimcParser { get; set; }

        [OneTimeSetUp]
        public void InitOnce()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.File("logs" + Path.DirectorySeparatorChar + "SimcProfileParser.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        [SetUp]
        public async Task Init()
        {
            // Load a data file
            var testFile = @"RawData" + Path.DirectorySeparatorChar + "Hierophant.simc";
            var testFileContents = await File.ReadAllLinesAsync(testFile);
            TestFileString = new List<string>(testFileContents);

            // Create a new profile service
            using var loggerFactory = LoggerFactory.Create(builder => builder
                .AddSerilog()
                .AddFilter(level => level >= LogLevel.Trace));
            var logger = loggerFactory.CreateLogger<SimcParserService>();
            SimcParser = new SimcParserService(logger);
        }

        [Test]
        public void SPS_Handles_Collection_Input()
        {
            // Arrange

            // Act
            var result = SimcParser.ParseProfileAsync(TestFileString);

            // Assert
            Assert.IsNotNull(result);
        }

        [Test]
        public void SPS_Processes_Without_Logger_Set()
        {
            // Arrange
            ISimcParserService sps = new SimcParserService(NullLogger<SimcParserService>.Instance);

            // Act
            void NoLoggerSet()
            {
                var result = SimcParser.ParseProfileAsync(TestFileString);
            }

            // Assert
            Assert.DoesNotThrow(NoLoggerSet);
        }
    }
}
