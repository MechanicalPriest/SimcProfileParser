//using Newtonsoft.Json;
//using NUnit.Framework;
//using SimcProfileParser.DataSync;
//using SimcProfileParser.Interfaces.DataSync;
//using SimcProfileParser.Model.RawData;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;

//namespace SimcProfileParser.Tests.DataSync
//{
//    [TestFixture]
//    public class RawDataExtractionServiceTests
//    {
//        [Test]
//        public void RDE_Generates_CR_Multi()
//        {
//            // Arrange
//            ICacheService cacheService = new CacheService(null);
//            IRawDataExtractionService rawDataExtractionService = 
//                new RawDataExtractionService(cacheService);

//            var filepath = Path.Combine(cacheService.BaseFileDirectory, "CombatRatingMultipliers.json");

//            // Act
//            rawDataExtractionService.GenerateCombatRatingMultipliers();
//            var rawData = File.ReadAllText(filepath);
//            var data = JsonConvert.DeserializeObject<float[][]>(rawData);

//            // Assert
//            FileAssert.Exists(filepath);
//            Assert.IsNotNull(data);
//            Assert.AreEqual(4, data.Length);
//            Assert.AreEqual(1300, data[0].Length);
//        }

//        [Test]
//        public void RDE_Generates_Stam_Multi()
//        {
//            // Arrange
//            ICacheService cacheService = new CacheService(null);
//            IRawDataExtractionService rawDataExtractionService =
//                new RawDataExtractionService(cacheService);

//            var filepath = Path.Combine(cacheService.BaseFileDirectory, "StaminaMultipliers.json");

//            // Act
//            rawDataExtractionService.GenerateStaminaMultipliers();
//            var rawData = File.ReadAllText(filepath);
//            var data = JsonConvert.DeserializeObject<float[][]>(rawData);

//            // Assert
//            FileAssert.Exists(filepath);
//            Assert.IsNotNull(data);
//            Assert.AreEqual(4, data.Length);
//            Assert.AreEqual(1300, data[0].Length);
//        }

//        [Test]
//        public void RDE_Generates_ItemData()
//        {
//            // Arrange
//            ICacheService cacheService = new CacheService(null);
//            IRawDataExtractionService rawDataExtractionService =
//                new RawDataExtractionService(cacheService);

//            var filepath = Path.Combine(cacheService.BaseFileDirectory, "ItemData.json");

//            // Act
//            rawDataExtractionService.GenerateItemData();
//            var rawData = File.ReadAllText(filepath);

//            var data = JsonConvert.DeserializeObject<List<SimcRawItem>>(rawData);

//            // Assert
//            // TODO: Add tests for each field of each item in a new test class? Against known items with valid values
//            FileAssert.Exists(filepath);
//            Assert.IsNotNull(data);
//            Assert.LessOrEqual(68475, data.Count);
//        }

//        [Test]
//        public void RDE_Generates_RandomPropData()
//        {
//            // Arrange
//            ICacheService cacheService = new CacheService(null);
//            IRawDataExtractionService rawDataExtractionService =
//                new RawDataExtractionService(cacheService);

//            var rawFilepath = Path.Combine(cacheService.BaseFileDirectory, "RandomPropData.raw");

//            // Act
//            var rawData = File.ReadAllText(rawFilepath);
//            rawDataExtractionService.GenerateRandomPropData(new Dictionary<string, string>()
//            { 
//                {"RandomPropData.raw", rawData }
//            });

//            // Assert
//            //Assert.IsNotNull(data);
//            //Assert.AreEqual(1300, data.Count);
//        }

//        [Test]
//        public void RDE_Generates_SpellData()
//        {
//            // Arrange
//            ICacheService cacheService = new CacheService(null);
//            IRawDataExtractionService rawDataExtractionService =
//                new RawDataExtractionService(cacheService);

//            var filepath = Path.Combine(cacheService.BaseFileDirectory, "SpellData.json");

//            // Act
//            rawDataExtractionService.GenerateSpellData();
//            var rawData = File.ReadAllText(filepath);

//            var data = JsonConvert.DeserializeObject<List<SimcRawItem>>(rawData);

//            // Assert
//            // TODO: Add tests for each field of each spell in a new test class? Against known items with valid values
//            FileAssert.Exists(filepath);
//            Assert.IsNotNull(data);
//            Assert.LessOrEqual(16945, data.Count);
//        }

//        [Test]
//        public void RDE_Generates_ItemBonuses()
//        {
//            // Arrange
//            ICacheService cacheService = new CacheService(null);
//            IRawDataExtractionService rawDataExtractionService =
//                new RawDataExtractionService(cacheService);

//            var filepath = Path.Combine(cacheService.BaseFileDirectory, "ItemBonusData.json");

//            // Act
//            rawDataExtractionService.GenerateItemBonusData();
//            var rawData = File.ReadAllText(filepath);

//            var data = JsonConvert.DeserializeObject<List<SimcRawItem>>(rawData);

//            // Assert
//            FileAssert.Exists(filepath);
//            Assert.IsNotNull(data);
//            Assert.LessOrEqual(9813, data.Count);
//        }
//    }
//}
