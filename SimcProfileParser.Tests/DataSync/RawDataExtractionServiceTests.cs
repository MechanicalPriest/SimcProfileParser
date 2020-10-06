using Newtonsoft.Json;
using NUnit.Framework;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ScaleData.raw", @"__combat_ratings_mult_by_ilvl[][1300] = { { 0.1, 0.2, }, { 0.3, 0.4, }, { 0.5, 0.6, }, { 0.7, 0.8, }, };" }
            };

            // Act
            var result = rawDataExtractionService.GenerateCombatRatingMultipliers(incomingRawData);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(1300, result[0].Length);
            Assert.AreEqual(0.1f, result[0][0]);
            Assert.AreEqual(0.2f, result[0][1]);
            Assert.AreEqual(0.3f, result[1][0]);
            Assert.AreEqual(0.4f, result[1][1]);
            Assert.AreEqual(0.5f, result[2][0]);
            Assert.AreEqual(0.6f, result[2][1]);
            Assert.AreEqual(0.7f, result[3][0]);
            Assert.AreEqual(0.8f, result[3][1]);
        }

        [Test]
        public void RDE_Generates_Stam_Multi()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ScaleData.raw", @"__stamina_mult_by_ilvl[][1300] = { { 0.1, 0.2, }, { 0.3, 0.4, }, { 0.5, 0.6, }, { 0.7, 0.8, }, };" }
            };

            // Act
            var result = rawDataExtractionService.GenerateStaminaMultipliers(incomingRawData);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(1300, result[0].Length);
            Assert.AreEqual(0.1f, result[0][0]);
            Assert.AreEqual(0.2f, result[0][1]);
            Assert.AreEqual(0.3f, result[1][0]);
            Assert.AreEqual(0.4f, result[1][1]);
            Assert.AreEqual(0.5f, result[2][0]);
            Assert.AreEqual(0.6f, result[2][1]);
            Assert.AreEqual(0.7f, result[3][0]);
            Assert.AreEqual(0.8f, result[3][1]);
        }

        [Test]
        public void RDE_Generates_ItemData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ItemData.raw", @"{ 32,  6667, 0.000000 },
                     { ""Gladiator's Medallion"", 184268, 0x00081000, 0x00006000, 0x00, 1, 35, 0, 0, 0, 12, 4, 0, 1, 0, 0.000000, 0.000000, &__item_stats_data[0], 1, 0xffff, 0xaa2aaaaa4e0ab3b2, { 0, 0, 0 }, 0, 0, 1458, 0, 0 }," },
                { "ItemEffect.raw", @"  { 135983,  42292, 184268,   0,   0, 1182,  120000,  120000 }, // PvP Trinket" }
            };

            // Act
            var result = rawDataExtractionService.GenerateItemData(incomingRawData);
            var resultTooHigh = rawDataExtractionService.GenerateItemData(incomingRawData, 0, 184267);
            var resultTooLow = rawDataExtractionService.GenerateItemData(incomingRawData, 184269);

            // Assert
            // TODO: Add tests for each field of each item in a new test class? Against known items with valid values
            Assert.IsNotNull(result);
            Assert.NotZero(result.Count);
            Assert.NotZero(result.FirstOrDefault().ItemMods.Count);
            Assert.NotZero(result.FirstOrDefault().ItemEffects.Count);

            Assert.IsNotNull(resultTooHigh);
            Assert.Zero(resultTooHigh.Count);

            Assert.IsNotNull(resultTooLow);
            Assert.Zero(resultTooLow.Count);
        }

        [Test]
        public void RDE_Generates_RandomPropData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "RandomPropPoints.raw", @"{  220,       38,       53, {      146,      110,       82,       73,       73 }, {      147,      110,       82,       73,       74 }, {      148,      110,       82,       73,       75 } }," }
            };

            // Act
            var result = rawDataExtractionService.GenerateRandomPropData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(firstResult);
            Assert.AreEqual(220, firstResult.ItemLevel);
            Assert.AreEqual(38, firstResult.DamageReplaceStat);
            Assert.AreEqual(53, firstResult.DamageSecondary);
            Assert.AreEqual(5, firstResult.Epic.Length);
            Assert.AreEqual(146, firstResult.Epic[0]);
            Assert.AreEqual(73, firstResult.Epic[4]);
            Assert.AreEqual(5, firstResult.Rare.Length);
            Assert.AreEqual(147, firstResult.Rare[0]);
            Assert.AreEqual(74, firstResult.Rare[4]);
            Assert.AreEqual(5, firstResult.Uncommon.Length);
            Assert.AreEqual(148, firstResult.Uncommon[0]);
            Assert.AreEqual(75, firstResult.Uncommon[4]);
        }

        [Test]
        public void RDE_Generates_SpellData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "SpellData.raw", @"  { ""The Hunt"" , 345396, 1, 0.000000, 0x0000000000000000, 0x00000800, 0, 0, " +
                "0, 0, 0, 0.000000, 50.000000,       0, 0, 0, 0, 0,    0, 1, 0, 1500, 0, 0, 0, 0, 0, 0.000000, 0, 0, " +
                "0, 0, { 128, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, { 0, 0, 262144, 0 }, 107, 0x00000000,  0, " + 
                "  0,  0, 0, 0, 0, 0, 1, 0, 1, 0 }, /* 871236 */" }
            };

            // Act
            var result = rawDataExtractionService.GenerateSpellData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            // TODO: Add tests for each field of each spell in a new test class? Against known items with valid values
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(firstResult);
            Assert.AreEqual("The Hunt", firstResult.Name);
            Assert.AreEqual(345396, firstResult.Id);
        }

        [Test]
        public void RDE_Generates_ItemBonuses()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ItemBonusData.raw", @"  { 12070, 6432, 13,    1737,       1,     424,   14309,  0 }," }
            };

            // Act
            var result = rawDataExtractionService.GenerateItemBonusData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(firstResult);
            Assert.AreEqual(12070, firstResult.Id);
            Assert.AreEqual(6432, firstResult.BonusId);
            Assert.AreEqual(13, (int)firstResult.Type);
            Assert.AreEqual(1737, firstResult.Value1);
            Assert.AreEqual(1, firstResult.Value2);
            Assert.AreEqual(424, firstResult.Value3);
            Assert.AreEqual(14309, firstResult.Value4);
            Assert.AreEqual(0, firstResult.Index);
        }
    }
}
