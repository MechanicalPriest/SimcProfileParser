using Newtonsoft.Json;
using NUnit.Framework;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimcProfileParser.Tests.DataSync
{
    [TestFixture]
    public class RawDataExtractionServiceTests
    {
        [Test]
        public void RDE_Generates_CR_Multi()
        {
            // Arrange
            ICacheService cacheService = new CacheService();
            IRawDataExtractionService rawDataExtractionService = 
                new RawDataExtractionService(cacheService);

            var filepath = Path.Combine(cacheService.BaseFileDirectory, "CombatRatingMultipliers.json");

            // Act
            rawDataExtractionService.GenerateCombatRatingMultipliers();
            var rawData = File.ReadAllText(filepath);
            var data = JsonConvert.DeserializeObject<float[][]>(rawData);

            // Assert
            FileAssert.Exists(filepath);
            Assert.IsNotNull(data);
            Assert.AreEqual(4, data.Length);
            Assert.AreEqual(1300, data[0].Length);
        }

        [Test]
        public void RDE_Generates_Stam_Multi()
        {
            // Arrange
            ICacheService cacheService = new CacheService();
            IRawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(cacheService);

            var filepath = Path.Combine(cacheService.BaseFileDirectory, "StaminaMultipliers.json");

            // Act
            rawDataExtractionService.GenerateStaminaMultipliers();
            var rawData = File.ReadAllText(filepath);
            var data = JsonConvert.DeserializeObject<float[][]>(rawData);

            // Assert
            FileAssert.Exists(filepath);
            Assert.IsNotNull(data);
            Assert.AreEqual(4, data.Length);
            Assert.AreEqual(1300, data[0].Length);
        }

        [Test]
        public void RDE_Generates_ItemData()
        {
            // Arrange
            ICacheService cacheService = new CacheService();
            IRawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(cacheService);

            var filepath = Path.Combine(cacheService.BaseFileDirectory, "ItemData.json");

            // Act
            rawDataExtractionService.GenerateItemData();
            var rawData = File.ReadAllText(filepath);

            var data = JsonConvert.DeserializeObject<List<SimcRawItem>>(rawData);

            // Assert
            // TODO: Add tests for each field of each item in a new test class? Against known items with valid values
            FileAssert.Exists(filepath);
            Assert.IsNotNull(data);
            Assert.LessOrEqual(68475, data.Count);
        }

        [Test]
        public void RDE_Generates_RandomPropData()
        {
            // Arrange
            ICacheService cacheService = new CacheService();
            IRawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(cacheService);

            var filepath = Path.Combine(cacheService.BaseFileDirectory, "RandomPropData.json");

            // Act
            rawDataExtractionService.GenerateRandomPropData();
            var rawData = File.ReadAllText(filepath);

            var data = JsonConvert.DeserializeObject<List<SimcRawItem>>(rawData);

            // Assert
            // TODO: Add tests for each field of each item in a new test class? Against known items with valid values
            FileAssert.Exists(filepath);
            Assert.IsNotNull(data);
            Assert.AreEqual(1300, data.Count);
        }
    }
}
