using NUnit.Framework;
using NUnit.Framework.Legacy;
using SimcProfileParser.DataSync;
using SimcProfileParser.Model.RawData;
using System.Collections.Generic;
using System.Linq;

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
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(4, result.Length);
            ClassicAssert.AreEqual(1300, result[0].Length);
            ClassicAssert.AreEqual(0.1f, result[0][0]);
            ClassicAssert.AreEqual(0.2f, result[0][1]);
            ClassicAssert.AreEqual(0.3f, result[1][0]);
            ClassicAssert.AreEqual(0.4f, result[1][1]);
            ClassicAssert.AreEqual(0.5f, result[2][0]);
            ClassicAssert.AreEqual(0.6f, result[2][1]);
            ClassicAssert.AreEqual(0.7f, result[3][0]);
            ClassicAssert.AreEqual(0.8f, result[3][1]);
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
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(4, result.Length);
            ClassicAssert.AreEqual(1300, result[0].Length);
            ClassicAssert.AreEqual(0.1f, result[0][0]);
            ClassicAssert.AreEqual(0.2f, result[0][1]);
            ClassicAssert.AreEqual(0.3f, result[1][0]);
            ClassicAssert.AreEqual(0.4f, result[1][1]);
            ClassicAssert.AreEqual(0.5f, result[2][0]);
            ClassicAssert.AreEqual(0.6f, result[2][1]);
            ClassicAssert.AreEqual(0.7f, result[3][0]);
            ClassicAssert.AreEqual(0.8f, result[3][1]);
        }

        [Test]
        public void RDE_Generates_Stam_Multi_Over_Ten()
        {
            // Arrange
            // Midnight stamina multipliers go above 10, e.g. "    9.989023589,	10.0360075,	10.0832124,"
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ScaleData.raw", "__stamina_mult_by_ilvl[][1300] = { { 9.989023589,\t10.0360075,\t10.0832124, }, "
                    + "{ 1, 2, }, { 3, 4, }, { 5, 6, }, };" }
            };

            // Act
            var result = rawDataExtractionService.GenerateStaminaMultipliers(incomingRawData);

            // Assert
            Assert.That(result[0][0], Is.EqualTo(9.989023589f));
            Assert.That(result[0][1], Is.EqualTo(10.0360075f));
            Assert.That(result[0][2], Is.EqualTo(10.0832124f));
            Assert.That(result[1][0], Is.EqualTo(1f));
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
                { "ItemEffectData.raw", @"  { 135983,  42292, 184268,   0,   0, 1182,  120000,  120000 }, // PvP Trinket" }
            };

            // Act
            var result = rawDataExtractionService.GenerateItemData(incomingRawData);
            var resultTooHigh = rawDataExtractionService.GenerateItemData(incomingRawData, 0, 184267);
            var resultTooLow = rawDataExtractionService.GenerateItemData(incomingRawData, 184269);

            // Assert
            // TODO: Add tests for each field of each item in a new test class? Against known items with valid values
            ClassicAssert.IsNotNull(result);
            ClassicAssert.NotZero(result.Count);
            ClassicAssert.NotZero(result.FirstOrDefault().ItemMods.Count);
            ClassicAssert.NotZero(result.FirstOrDefault().ItemEffects.Count);

            ClassicAssert.IsNotNull(resultTooHigh);
            ClassicAssert.Zero(resultTooHigh.Count);

            ClassicAssert.IsNotNull(resultTooLow);
            ClassicAssert.Zero(resultTooLow.Count);
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
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsNotNull(firstResult);
            ClassicAssert.AreEqual(220, firstResult.ItemLevel);
            ClassicAssert.AreEqual(38, firstResult.DamageReplaceStat);
            ClassicAssert.AreEqual(53, firstResult.DamageSecondary);
            ClassicAssert.AreEqual(5, firstResult.Epic.Length);
            ClassicAssert.AreEqual(146, firstResult.Epic[0]);
            ClassicAssert.AreEqual(73, firstResult.Epic[4]);
            ClassicAssert.AreEqual(5, firstResult.Rare.Length);
            ClassicAssert.AreEqual(147, firstResult.Rare[0]);
            ClassicAssert.AreEqual(74, firstResult.Rare[4]);
            ClassicAssert.AreEqual(5, firstResult.Uncommon.Length);
            ClassicAssert.AreEqual(148, firstResult.Uncommon[0]);
            ClassicAssert.AreEqual(75, firstResult.Uncommon[4]);
        }

        [Test]
        public void RDE_Generates_SpellData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {

                { "SpellData.raw", "{ { \"Flash Heal\"                        ,    2061, 2, 0.000000, 0.000000, 0.000000, " +
                "0x0000000000000000, 0x00000010, 0, 0, 0, 3, 0, 0, 0.000000, 40.000000,       0, 1500, 0, 0, 0, 0, 0,    " +
                "0, 1, 0, 0.000000, 0.000000, 0, 0, 0, 0, 0x0000000000000000, 0, 0.000000,  0, 0x00000000, 0x00000000, 1500, " +
                "{ 65536, 0, 524288, 0, 0, 0, 0, 0, 16781312, 0, 0, 0, 0, 1, 0, 0, 0 }, { 2048, 0, 0, 1073741824 }, 6, " +
                "{ 0, 0 }, { 0, 0 }, 0x88000000, 0,   0,  0, 0, 0, 0, 0, 1, 4, 1, 7 }, /* 613 */\r\n" +
                "{     613,   2061,  0,  10,   0, 0, 0x00000000, 0.000000, 0.050000, 0.000000, 3.410400, " +
                "0.000000, 0, 0.000000, 0.000000,      0.0000, 0, 0, { 0, 0, 0, 0 }, 0, 1.000000, 0.000000, " +
                "0.000000, 0,   0, 21, 0, 1.000000, 1.000000, 0, 0 },\r\n" +
                "  {    154,   2061,  137031,   0,     0,    0, 0,   3.600,   0.000,   0.000 }," }
            };

            // Act
            var result = rawDataExtractionService.GenerateSpellData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            // TODO: Add tests for each field of each spell in a new test class? Against known items with valid values
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsNotNull(firstResult);
            ClassicAssert.AreEqual("Flash Heal", firstResult.Name);
            ClassicAssert.AreEqual(2061, firstResult.Id);
            ClassicAssert.IsNotNull(firstResult.Effects);
            ClassicAssert.NotZero(firstResult.Effects.Count);
            ClassicAssert.AreEqual(613, firstResult.Effects[0].Id);
            ClassicAssert.IsNotNull(firstResult.SpellPowers);
            ClassicAssert.AreEqual(1, firstResult.SpellPowers.Count);
            ClassicAssert.AreEqual(154, firstResult.SpellPowers[0].Id);
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
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsNotNull(firstResult);
            ClassicAssert.AreEqual(12070, firstResult.Id);
            ClassicAssert.AreEqual(6432, firstResult.BonusId);
            ClassicAssert.AreEqual(13, (int)firstResult.Type);
            ClassicAssert.AreEqual(1737, firstResult.Value1);
            ClassicAssert.AreEqual(1, firstResult.Value2);
            ClassicAssert.AreEqual(424, firstResult.Value3);
            ClassicAssert.AreEqual(14309, firstResult.Value4);
            ClassicAssert.AreEqual(0, firstResult.Index);
        }

        [Test]
        public void RDE_Generates_GemData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "GemData.raw", @"  {   62,  2706, 0x0000000c }, // +$k1 Dodge and +$k2 Stamina" }
            };

            // Act
            var result = rawDataExtractionService.GenerateGemData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsNotNull(firstResult);
            ClassicAssert.AreEqual(62, firstResult.Id);
            ClassicAssert.AreEqual(2706, firstResult.EnchantId);
            ClassicAssert.AreEqual(12, firstResult.Colour);
        }

        [Test]
        public void RDE_Generates_ItemEnchantData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ItemEnchantData.raw", @"  { 5425,      0, -1,   0,  40, 0, 60,   0,   0, {   5,   0,   0 }, {    0,    0,    0 }, {     49,      0,      0 }, {  0.0883,  0.0000,  0.0000 }, 190868, ""+$k1 Mastery""                      }," }
            };

            // Act
            var result = rawDataExtractionService.GenerateItemEnchantData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsNotNull(firstResult);
            ClassicAssert.AreEqual(5425, firstResult.Id);
        }

        [Test]
        public void RDE_Generates_SpellScale_Multi()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ScaleData.raw", @"static constexpr double __spell_scaling[][90] = {
  {
    1,	0,	0,	0,	0,	//    5
  },
  {
    2,	0,	0,	0,	0,	//    5
  },
  {
    3,	0,	0,	0,	0,	//    5
  },
  {
    4,	0,	0,	0,	0,	//    5
  },
  {
    0,	5,	0,	0,	0,	//    5
  },
  {
    0,	0,	6,	0,	0,	//    5
  },
  {
    0,	0,	0,	7,	0,	//    5
  },
  {
    0,	0,	0,	0,	8,	//    5
  },
  {
    9,	0,	0,	0,	0,	//    5
  },
  {
    10,	0,	0,	0,	0,	//    5
  },
  {
    11,	0,	0,	0,	0,	//    5
  },
  {
    12,	0,	0,	0,	0,	//    5
  },
  {
    0,	13,	0,	0,	0,	//    5
  },
  {
    1.2,	1.4,	1.6,	1.8,	2,	//    5
  },
  {
    200,	200,	200,	200,	200,	//    5
  },
  {
    1.019694663,	1.019694663,	1.019694663,	1.019694663,	1.019694663,	//    5
  },
  {
    2,	2,	2,	2,	2,	//    5
  },
  {
    3,	3,	3,	3,	3,	//    5
  },
  {
    133.3540457,	146.6894502,	160.0248548,	173.3602594,	186.6956639,	//    5
  },
  {
    0.086378445,	0.100774853,	0.11517126,	0.129567668,	0.143964076,	//    5
  },
};" }
            };

            // Act
            var result = rawDataExtractionService.GenerateSpellScalingMultipliers(incomingRawData);

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(23, result.Length);
            ClassicAssert.AreEqual(90, result[0].Length);
            ClassicAssert.AreEqual(1d, result[0][0]);
            ClassicAssert.AreEqual(2d, result[1][0]);
            ClassicAssert.AreEqual(3d, result[2][0]);
            ClassicAssert.AreEqual(4d, result[3][0]);
            ClassicAssert.AreEqual(5d, result[4][1]);
            ClassicAssert.AreEqual(6d, result[5][2]);
            ClassicAssert.AreEqual(7d, result[6][3]);
            ClassicAssert.AreEqual(8d, result[7][4]);
            ClassicAssert.AreEqual(9d, result[8][0]);
            ClassicAssert.AreEqual(0.143964076d, result[19][4]);
        }

        [Test]
        public void RDE_Generates_CurveData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "CurveData.raw", @"  { 5481,  2,    0.79273,  -42.35294,    0.79273,  -42.35294 }," }
            };

            // Act
            var result = rawDataExtractionService.GenerateCurveData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsNotNull(firstResult);
            ClassicAssert.AreEqual(5481, firstResult.CurveId, "Curve Id");
            ClassicAssert.AreEqual(2, firstResult.Index, "Index");
            ClassicAssert.AreEqual(0.79273f, firstResult.Primary1, "Primary 1");
            ClassicAssert.AreEqual(-42.35294f, firstResult.Primary2, "Primary 2");
            ClassicAssert.AreEqual(0.79273f, firstResult.Secondary1, "Secondary 1");
            ClassicAssert.AreEqual(-42.35294f, firstResult.Secondary2, "Secondary 2");
        }

        [Test]
        public void RDE_Generates_ItemScalingConfigData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "CurveData.raw", "static constexpr std::array<item_scaling_config_data_t, 1> __item_scaling_config_data { {\r\n"
                    + "  { 323, 47, 334, 90, 2 },\r\n"
                    + "} };" }
            };

            // Act
            var result = rawDataExtractionService.GenerateItemScalingConfigData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(firstResult.Id, Is.EqualTo(323), "Id");
            Assert.That(firstResult.ItemOffsetCurveId, Is.EqualTo(47), "Item Offset Curve Id");
            Assert.That(firstResult.ItemLevel, Is.EqualTo(334), "Item Level");
            Assert.That(firstResult.PlayerLevel, Is.EqualTo(90), "Player Level");
            Assert.That(firstResult.SquishEraId, Is.EqualTo(2), "Squish Era Id");
        }

        [Test]
        public void RDE_Generates_ItemOffsetCurveData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "CurveData.raw", "static constexpr std::array<item_offset_curve_data_t, 1> __item_offset_curve_data { {\r\n"
                    + "  {  2, 6243, -6 },\r\n"
                    + "} };" }
            };

            // Act
            var result = rawDataExtractionService.GenerateItemOffsetCurveData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(firstResult.Id, Is.EqualTo(2), "Id");
            Assert.That(firstResult.CurveId, Is.EqualTo(6243), "Curve Id");
            Assert.That(firstResult.Offset, Is.EqualTo(-6), "Offset");
        }

        [Test]
        public void RDE_Generates_ContentTuningData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ContentTuning.raw", "static constexpr std::array<content_tuning_data_t, 1> __content_tuning_data { {\r\n"
                    + "  {    1, 0, -3, 1, 30, 0 },\r\n"
                    + "} };" }
            };

            // Act
            var result = rawDataExtractionService.GenerateContentTuningData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(firstResult.Id, Is.EqualTo(1), "Id");
            Assert.That(firstResult.Flags, Is.EqualTo(0), "Flags");
            Assert.That(firstResult.ExpansionId, Is.EqualTo(-3), "Expansion Id");
            Assert.That(firstResult.MinLevelSquish, Is.EqualTo(1), "Min Level Squish");
            Assert.That(firstResult.MaxLevelSquish, Is.EqualTo(30), "Max Level Squish");
            Assert.That(firstResult.ItemLevel, Is.EqualTo(0), "Item Level");
        }

        [Test]
        public void RDE_Generates_ClassBaseStats()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ExtraData.raw", "static constexpr stat_data_t __gt_class_stats_by_level[][2] =\r\n"
                    + "{\r\n"
                    + "  { // No Class\r\n"
                    + "    //    Str     Agi     Sta     Int     Spi\r\n"
                    + "    {      0,      0,      0,      0,      0 },\r\n"
                    + "    {      0,      0,      0,      0,      0 }, // 2\r\n"
                    + "  },\r\n"
                    + "  { // Warrior\r\n"
                    + "    {     10,     15,    292,     20,      0 },\r\n"
                    + "    {    360,    515,   4600,    620,      0 }, // 2\r\n"
                    + "  },\r\n"
                    + "};" }
            };

            // Act
            var result = rawDataExtractionService.GenerateClassBaseStats(incomingRawData);

            // Assert
            Assert.That(result, Has.Length.EqualTo(2), "Classes");
            Assert.That(result[1], Has.Length.EqualTo(2), "Levels");
            Assert.That(result[1][0], Is.EqualTo(new[] { 10, 15, 292, 20 }));
            Assert.That(result[1][1], Is.EqualTo(new[] { 360, 515, 4600, 620 }));
        }

        [Test]
        public void RDE_Generates_RaceBaseStats()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ExtraData.raw", "static constexpr stat_data_t __ptr_gt_race_stats[] =\r\n"
                    + "{\r\n"
                    + "  // Str Agi Sta Int Spi\r\n"
                    + "  {   0,  0,  0,  0,  0 }, // No Race\r\n"
                    + "  {  -3,  1, -1,  3,  0 }, // Gnome\r\n"
                    + "};" }
            };

            // Act
            var result = rawDataExtractionService.GenerateRaceBaseStats(incomingRawData);

            // Assert
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[1], Is.EqualTo(new[] { -3, 1, -1, 3 }));
        }

        [Test]
        public void RDE_Generates_BaseMana_And_CombatRatings()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ScaleData.raw", "static constexpr double __base_mp[][2] = {\r\n"
                    + "  {\r\n"
                    + "    0,\t0,\t//    2\r\n"
                    + "  },\r\n"
                    + "  // Warrior\r\n"
                    + "  {\r\n"
                    + "    50000,\t250000,\t//    2\r\n"
                    + "  },\r\n"
                    + "};\r\n"
                    + "static constexpr double __combat_ratings[][2] = {\r\n"
                    + "  // Dodge\r\n"
                    + "  {\r\n"
                    + "    3.455208736,\t52,\t//    2\r\n"
                    + "  },\r\n"
                    + "};\r\n"
                    + "static constexpr double __combat_ratings_mult_by_ilvl[][2] = {\r\n"
                    + "  {\r\n"
                    + "    1,\t1,\t//    2\r\n"
                    + "  },\r\n"
                    + "};" }
            };

            // Act
            var baseMana = rawDataExtractionService.GenerateBaseMana(incomingRawData);
            var combatRatings = rawDataExtractionService.GenerateCombatRatings(incomingRawData);

            // Assert
            Assert.That(baseMana, Has.Length.EqualTo(2));
            Assert.That(baseMana[1], Is.EqualTo(new[] { 50000d, 250000d }));
            Assert.That(combatRatings, Has.Length.EqualTo(1));
            Assert.That(combatRatings[0], Is.EqualTo(new[] { 3.455208736, 52d }));
        }

        [Test]
        public void RDE_Generates_RppmData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "RppmData.raw", @"  { 339343,  257,  4, -0.5000 }," }
            };

            // Act
            var result = rawDataExtractionService.GenerateRppmData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsNotNull(firstResult);
            ClassicAssert.AreEqual(339343, firstResult.SpellId, "Spell Id");
            ClassicAssert.AreEqual(257, firstResult.Type, "Type");
            ClassicAssert.AreEqual(4, (int)firstResult.ModifierType, "ModifierType");
            ClassicAssert.AreEqual(-0.5000, firstResult.Coefficient, "Coefficient");
        }

        [Test]
        public void RDE_Generates_ItemEffectData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "ItemEffectData.raw", @"  { 135983,  42292, 184268,   0,   0, 1182,  120000,  120000 }, // PvP Trinket" }
            };

            // Act
            var result = rawDataExtractionService.GenerateItemEffectData(incomingRawData);
            var firstResult = result.FirstOrDefault();

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsNotNull(firstResult);
            ClassicAssert.AreEqual(135983, firstResult.Id, "Id");
            ClassicAssert.AreEqual(42292, firstResult.SpellId, "Spell Id");
            ClassicAssert.AreEqual(184268, firstResult.ItemId, "Id");
            ClassicAssert.AreEqual(0, firstResult.Index, "Index");
            ClassicAssert.AreEqual(0, firstResult.Type, "Type");
            ClassicAssert.AreEqual(1182, firstResult.CooldownGroup, "CooldownGroup");
            ClassicAssert.AreEqual(120000, firstResult.CooldownDuration, "CooldownDuration");
            ClassicAssert.AreEqual(120000, firstResult.CooldownGroupDuration, "CooldownGroupDuration");
        }

        [Test]
        public void RDE_Generates_GameVersionData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "GameDataVersion.raw", "#define CLIENT_DATA_WOW_VERSION \"9.0.2.36401\"" }
            };

            // Act
            var result = rawDataExtractionService.GenerateGameDataVersion(incomingRawData);

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual("9.0.2.36401", result);
        }

        [Test]
        public void RDE_Generates_TraitData()
        {
            // Arrange
            RawDataExtractionService rawDataExtractionService =
                new RawDataExtractionService(null);

            var incomingRawData = new Dictionary<string, string>()
            {
                { "TraitData.raw", "static constexpr std::array<trait_data_t, 2> __trait_data_data { {\r\n" +
                @"  { 2,  5, 103764,  82629, 2, 20, 108769,  196985,      0,      0,  9,  2, 100,                     ""Light of the Naaru"", {  257,    0,    0,    0 }, {    0,    0,    0,    0 },   0, 0 }," + "\r\n" +
                @"  { 2,  1, 112271,  90402, 1,  8, 117276,  383959,      0,      0,  7,  1, 100,                  ""Cold Steel, Hot Blood"", {   72,    0,    0,    0 }, {    0,    0,    0,    0 },   0, 0 }," + "\r\n" +
                "} };\r\n" +
                "static constexpr std::array<trait_definition_effect_entry_t, 1> __trait_definition_effect_data { {\r\n" +
                "  { 108769, 0, 0, 59089 },\r\n" +
                "} };"
                }
            };

            // Act
            var result = rawDataExtractionService.GenerateTraitData(incomingRawData);
            var lightOfTheNaaru = result.FirstOrDefault(t => t.SpellId == 196985);
            var coldSteel = result.FirstOrDefault(t => t.SpellId == 383959);

            // Assert
            ClassicAssert.AreEqual(2, result.Count);
            ClassicAssert.IsNotNull(lightOfTheNaaru);
            ClassicAssert.AreEqual(2, lightOfTheNaaru.TreeIndex);
            ClassicAssert.AreEqual(5, lightOfTheNaaru.ClassId);
            ClassicAssert.AreEqual(103764, lightOfTheNaaru.TraitNodeEntryId);
            ClassicAssert.AreEqual(82629, lightOfTheNaaru.NodeId);
            ClassicAssert.AreEqual(2, lightOfTheNaaru.MaxRanks);
            ClassicAssert.AreEqual(20, lightOfTheNaaru.RequiredPoints);
            ClassicAssert.AreEqual(108769, lightOfTheNaaru.TraitDefinitionId);
            ClassicAssert.AreEqual(0, lightOfTheNaaru.SpellReplaceId);
            ClassicAssert.AreEqual(0, lightOfTheNaaru.SpellOverrideId);
            ClassicAssert.AreEqual(9, lightOfTheNaaru.Row);
            ClassicAssert.AreEqual(2, lightOfTheNaaru.Column);
            ClassicAssert.AreEqual(100, lightOfTheNaaru.SelectionIndex);
            ClassicAssert.AreEqual("Light of the Naaru", lightOfTheNaaru.Name);
            ClassicAssert.AreEqual(new uint[] { 257, 0, 0, 0 }, lightOfTheNaaru.SpecId);
            ClassicAssert.AreEqual(new uint[] { 0, 0, 0, 0 }, lightOfTheNaaru.SpecStarterId);
            ClassicAssert.AreEqual(0, lightOfTheNaaru.SubTreeId);
            ClassicAssert.AreEqual(0, lightOfTheNaaru.NodeType);

            ClassicAssert.AreEqual(1, lightOfTheNaaru.DefinitionEffects.Count);
            ClassicAssert.AreEqual(0, lightOfTheNaaru.DefinitionEffects[0].EffectIndex);
            ClassicAssert.AreEqual(0, lightOfTheNaaru.DefinitionEffects[0].Operation);
            ClassicAssert.AreEqual(59089, lightOfTheNaaru.DefinitionEffects[0].CurveId);

            // Names containing commas parse without shifting the other fields
            ClassicAssert.IsNotNull(coldSteel);
            ClassicAssert.AreEqual("Cold Steel, Hot Blood", coldSteel.Name);
            ClassicAssert.AreEqual(72, coldSteel.SpecId[0]);
            ClassicAssert.AreEqual(0, coldSteel.DefinitionEffects.Count);
        }
    }
}
