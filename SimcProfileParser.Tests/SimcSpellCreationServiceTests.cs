using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Serilog;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.RawData;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    class SimcSpellCreationServiceTests
    {
        private ILoggerFactory _loggerFactory;
        private SimcSpellCreationService _spellCreationService;

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
            ICacheService cacheService = new CacheService(rawDataExtractionService, 
                _loggerFactory.CreateLogger<CacheService>(), 
                new RawFileService(_loggerFactory.CreateLogger<RawFileService>()));
            var utilityService = new SimcUtilityService(
                cacheService,
                _loggerFactory.CreateLogger<SimcUtilityService>());

            _spellCreationService = new SimcSpellCreationService(
                utilityService,
                _loggerFactory.CreateLogger<SimcSpellCreationService>());
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_Spell_Options()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 226,
                SpellId = 343538,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(2, spell.Effects.Count);
            ClassicAssert.AreEqual(127.8034668d, spell.Effects[0].ScaleBudget);
            ClassicAssert.AreEqual(460.97500600000001d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(621.39996299999996d, spell.Effects[1].Coefficient);
        }

        /// <summary>
        /// When an itemspell is attempted to be made and there is no spelldata for it, return an empty
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task SSC_Fails_Create_Item_Invalid_Spell()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 5,
                SpellId = 7823,
                ItemQuality = ItemQuality.ITEM_QUALITY_RARE,
                ItemInventoryType = InventoryType.INVTYPE_HEAD
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNull(spell);
        }

        [Test]
        public async Task SSC_Converts_OneScale_To_SevenScale()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 226,
                SpellId = 344227,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1, spell.Effects.Count);
            ClassicAssert.AreEqual(162.23414856664567d, spell.Effects[0].ScaleBudget);
            ClassicAssert.AreEqual(1.65, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(-7, spell.Effects[0].ScalingType);
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_Raw_Obj()
        {
            // Arrange
            // Use the Brimming Ember Shard spell 343538
            var item = new SimcItem()
            {
                ItemLevel = 226,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                InventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(item, 343538);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(2, spell.Effects.Count);
            ClassicAssert.AreEqual(127.8034668d, spell.Effects[0].ScaleBudget);
            ClassicAssert.AreEqual(460.97500600000001d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(621.39996299999996d, spell.Effects[1].Coefficient);
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_Raw_Obj_9()
        {
            // Arrange
            // Use the First Class Healing Distributor spell 352273
            var item = new SimcItem()
            {
                ItemLevel = 226,
                Quality = ItemQuality.ITEM_QUALITY_EPIC,
                InventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(item, 352273);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1, spell.Effects.Count);
            ClassicAssert.AreEqual(180.9646759d, spell.Effects[0].ScaleBudget);
            ClassicAssert.AreEqual(26.649944000000001d, spell.Effects[0].Coefficient);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_Spell_Options()
        {
            // Arrange
            // Use the 
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 274740,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1.716d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(131.08048629999999d, spell.Effects[0].ScaleBudget);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_With_Power()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 274740,
                PlayerLevel = 80
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1.716d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(33.631460769999997d, spell.Effects[0].ScaleBudget);
        }

        [Ignore("Was previously tested on Shadowfiend which has been removed. Need a new spell to test this.")]
        [Test]
        public async Task SSC_Creates_Player_Spell_With_Invalid_Trigger_Spell()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 34433,
                PlayerLevel = 80
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(15000d, spell.Duration);
            ClassicAssert.AreEqual(180000d, spell.Cooldown);
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_Raw()
        {
            // Arrange
            var playerLevel = 90u;
            var spellId = 274740u;

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(playerLevel, spellId);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.Effects);
            ClassicAssert.AreEqual(1.716d, spell.Effects[0].Coefficient);
            ClassicAssert.AreEqual(131.08048629999999d, spell.Effects[0].ScaleBudget); // Updated for midnight branch level 90
        }

        [Test]
        public async Task SSC_Creates_Player_Spell_WithPower()
        {
            // Arrange
            var playerLevel = 60u;
            var spellId = 589u;

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(playerLevel, spellId);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.PowerCosts);
            ClassicAssert.LessOrEqual(2, spell.PowerCosts.Count);
            ClassicAssert.AreEqual(0.3, spell.PowerCosts.Skip(1).First().Value);
            ClassicAssert.AreEqual(137031, spell.PowerCosts.Skip(1).First().Key);
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_RppmSpecModifiers()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 226,
                SpellId = 339343,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.RppmModifiers);
            ClassicAssert.AreEqual(7, spell.RppmModifiers.Count);
            ClassicAssert.AreEqual(339343, spell.RppmModifiers[4].SpellId);
            ClassicAssert.IsFalse(spell.RppmModifiers[4].RppmIsHasted);
            ClassicAssert.IsTrue(spell.RppmModifiers[4].RppmIsSpecModified);
            ClassicAssert.AreEqual(257, spell.RppmModifiers[4].RppmSpec);
            ClassicAssert.AreEqual(-0.5000, spell.RppmModifiers[4].RppmCoefficient);
        }

        [Test]
        public async Task SSC_Creates_Item_Spell_RppmHasteModifiers()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 226,
                SpellId = 339547,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            ClassicAssert.IsNotNull(spell);
            ClassicAssert.IsNotNull(spell.RppmModifiers);
            ClassicAssert.AreEqual(1, spell.RppmModifiers.Count);
            ClassicAssert.AreEqual(339547, spell.RppmModifiers[0].SpellId);
            ClassicAssert.IsTrue(spell.RppmModifiers[0].RppmIsHasted);
            ClassicAssert.IsFalse(spell.RppmModifiers[0].RppmIsSpecModified);
            ClassicAssert.AreEqual(0, spell.RppmModifiers[0].RppmSpec);
            ClassicAssert.AreEqual(1, spell.RppmModifiers[0].RppmCoefficient);
        }

        [Test]
        public async Task SSC_Player_Spell_Lists_Spells_Affecting_It_By_Family_Flags()
        {
            // Arrange
            // Power Word: Shield is modified by the Priest and Discipline Priest auras
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 17,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            Assert.That(spell.AffectedBySpellIds, Does.Contain(137030u));
            Assert.That(spell.AffectedBySpellIds, Does.Contain(137032u));
            Assert.That(spell.AffectedBySpellIds, Does.Not.Contain(17u), "A spell should not list itself as affecting itself");
            Assert.That(spell.AffectedBySpellIds, Is.Ordered);
        }

        [Test]
        public async Task SSC_Player_Spell_Effect_Lists_Affected_Spells_By_Family_Flags()
        {
            // Arrange
            // The Priest class aura modifies Power Word: Shield
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 137030,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            Assert.That(spell.Effects.Any(e => e.AffectedSpellIds.Contains(17u)), Is.True);
        }

        [Test]
        public async Task SSC_Player_Spell_Affects_Include_Category_Modifiers()
        {
            // Arrange
            // Vanguard has a Modify Recharge Time (Category) effect for Spell Reflection's category
            var vanguardOptions = new SimcSpellOptions()
            {
                SpellId = 71,
                PlayerLevel = 90
            };
            var spellReflectionOptions = new SimcSpellOptions()
            {
                SpellId = 23920,
                PlayerLevel = 90
            };

            // Act
            var vanguard = await _spellCreationService.GeneratePlayerSpellAsync(vanguardOptions);
            var spellReflection = await _spellCreationService.GeneratePlayerSpellAsync(spellReflectionOptions);

            // Assert
            Assert.That(vanguard.Effects.Any(e => e.AffectedSpellIds.Contains(23920u)), Is.True);
            Assert.That(spellReflection.AffectedBySpellIds, Does.Contain(71u));
        }

        [Test]
        public async Task SSC_Player_Spell_Affects_Include_Category_Cooldown_Effects()
        {
            // Arrange
            // Coldthirst uses the Modify Cooldown (Category) effect type rather than an aura to reduce Counterspell's cooldown
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 2139,
                PlayerLevel = 90
            };

            // Act
            var counterspell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            Assert.That(counterspell.AffectedBySpellIds, Does.Contain(378849u));
        }

        [Test]
        public async Task SSC_Player_Spell_Has_Labels()
        {
            // Arrange
            // Smite carries the Class Spells (16), Priest Spells (18), 292 and 954 labels
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 585,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            Assert.That(spell.Labels, Is.SupersetOf(new uint[] { 16, 18, 292, 954 }));
        }

        [Test]
        public async Task SSC_Player_Spell_Effect_Lists_Affected_Spells_By_Label()
        {
            // Arrange
            // Holy Priest aura effect #16 (1040388) is a percent label modifier (218) on label 2932, which Halo (120692) carries
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 137031,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);
            var effect = spell.Effects.Single(e => e.Id == 1040388);

            // Assert
            Assert.That(effect.EffectSubType, Is.EqualTo(218u));
            Assert.That(effect.MiscValue2, Is.EqualTo(2932));
            Assert.That(effect.AffectedSpellIds, Does.Contain(120692u));
        }

        [Test]
        public async Task SSC_Player_Spell_Lists_Label_Modifiers_Affecting_It()
        {
            // Arrange
            // The Holy Priest 12.0 4pc (1264853) modifies Divine Image (392990) and Healing Light (196809) through labels 5993 and 5992
            var setBonusOptions = new SimcSpellOptions()
            {
                SpellId = 1264853,
                PlayerLevel = 90
            };
            var divineImageOptions = new SimcSpellOptions()
            {
                SpellId = 392990,
                PlayerLevel = 90
            };
            var healingLightOptions = new SimcSpellOptions()
            {
                SpellId = 196809,
                PlayerLevel = 90
            };

            // Act
            var setBonus = await _spellCreationService.GeneratePlayerSpellAsync(setBonusOptions);
            var divineImage = await _spellCreationService.GeneratePlayerSpellAsync(divineImageOptions);
            var healingLight = await _spellCreationService.GeneratePlayerSpellAsync(healingLightOptions);

            // Assert
            Assert.That(divineImage.AffectedBySpellIds, Does.Contain(1264853u));
            Assert.That(healingLight.AffectedBySpellIds, Does.Contain(1264853u));
            Assert.That(setBonus.Effects.Any(e => e.AffectedSpellIds.Contains(392990u)), Is.True);
            Assert.That(setBonus.Effects.Any(e => e.AffectedSpellIds.Contains(196809u)), Is.True);
        }

        [Test]
        public async Task SSC_Player_Spell_Family_Flag_Modifiers_Still_Listed()
        {
            // Arrange
            // Apotheosis (200183) modifies Flash Heal (2061) through family flags
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 2061,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            Assert.That(spell.AffectedBySpellIds, Does.Contain(200183u));
            Assert.That(spell.AffectedBySpellIds, Is.Unique);
        }

        [Test]
        public async Task SSC_Item_Spell_Without_Labels_Or_Modifiers_Has_Empty_Lists()
        {
            // Arrange
            // Item spell 343538 has no labels and no family, category or label relationships
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 343538,
                ItemLevel = 226,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            Assert.That(spell.Labels, Is.Empty);
            Assert.That(spell.AffectedBySpellIds, Is.Empty);
            Assert.That(spell.Effects, Is.Not.Empty);
            Assert.That(spell.Effects.Select(e => e.AffectedSpellIds), Has.All.Empty);
        }

        [Test]
        public async Task SSC_Player_Spell_Effect_Has_Talent_Rank_Set_Values()
        {
            // Arrange
            // Light of the Naaru (2 ranks) sets effect #1 to 10 at rank 1 and 20 at rank 2
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 196985,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);
            var effect = spell.Effects.Single(e => e.EffectIndex == 0);

            // Assert
            Assert.That(effect.RankOperation, Is.EqualTo(SimcTalentRankOperation.Set));
            Assert.That(effect.RankValues, Is.EqualTo(new double[] { 10, 20 }));
            Assert.That(effect.BaseValue, Is.EqualTo(10), "BaseValue should stay as the raw spell data value");
        }

        [Test]
        public async Task SSC_Player_Spell_Effect_Has_Talent_Rank_Multiply_Values()
        {
            // Arrange
            // Critical Mass (2 ranks) multiplies effect #1 by the rank
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 117216,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);
            var effect = spell.Effects.Single(e => e.EffectIndex == 0);

            // Assert
            Assert.That(effect.RankOperation, Is.EqualTo(SimcTalentRankOperation.Multiply));
            Assert.That(effect.RankValues, Is.EqualTo(new double[] { 1, 2 }));
        }

        [Test]
        public async Task SSC_Player_Spell_Talent_Rank_Values_Are_Not_Float_Rounded()
        {
            // Arrange
            // Curve values are stored as floats; 0.6f and 1.2f should come through as 0.6 and 1.2
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 388511,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);
            var effect = spell.Effects.Single(e => e.EffectIndex == 0);

            // Assert
            Assert.That(effect.RankValues, Is.EqualTo(new double[] { 0.6, 1.2 }));
        }

        [Test]
        public async Task SSC_Player_Spell_Without_Talent_Rank_Values_Has_None()
        {
            // Arrange
            // Smite is not a talent with per-rank values
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 585,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);

            // Assert
            Assert.That(spell.Effects, Is.Not.Empty);
            Assert.That(spell.Effects.Select(e => e.RankOperation), Has.All.Null);
            Assert.That(spell.Effects.Select(e => e.RankValues), Has.All.Empty);
        }

        [Test]
        public async Task SSC_Player_Spell_With_Trigger_Cycle_Stops_Recursing()
        {
            // Arrange
            // 388511 triggers 388512, which triggers 388513, which triggers 388511 again
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 388511,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);
            var second = spell.Effects.Select(e => e.TriggerSpell).Single(s => s?.SpellId == 388512);
            var third = second.Effects.Select(e => e.TriggerSpell).Single(s => s?.SpellId == 388513);
            var backToStart = third.Effects.Single(e => e.TriggerSpellId == 388511);

            // Assert
            Assert.That(backToStart.TriggerSpell, Is.Null, "The cycle back to 388511 should not be generated again");
        }

        [Test]
        [TestCase(137031u, 3u, 0d, 1d, 90d, TestName = "SSC_Player_Spell_ScaledValue_From_Points_Per_Level")]
        [TestCase(126u, 1u, 0d, 5d, 365d, TestName = "SSC_Player_Spell_ScaledValue_Starts_At_Spell_Level")]
        [TestCase(19804u, 0u, 0d, 1d, 49d, TestName = "SSC_Player_Spell_ScaledValue_Capped_At_Spell_Max_Level")]
        public async Task SSC_Player_Spell_ScaledValue_Points_Per_Level(uint spellId, uint effectIndex,
            double baseValue, double pointsPerLevel, double scaledValue)
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = spellId,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);
            var effect = spell.Effects.Single(e => e.EffectIndex == effectIndex);

            // Assert
            Assert.That(effect.BaseValue, Is.EqualTo(baseValue));
            Assert.That(effect.PointsPerLevel, Is.EqualTo(pointsPerLevel));
            Assert.That(effect.ScaledValue, Is.EqualTo(scaledValue));
        }

        [Test]
        [TestCase(274738u, 224.9341d, TestName = "SSC_Player_Spell_ScaledValue_Coefficient_Overrides_Points_Per_Level")]
        [TestCase(10368u, 901.0145d, TestName = "SSC_Player_Spell_ScaledValue_Scaling_Class_9")]
        [TestCase(370607u, 2.1375e+07d, TestName = "SSC_Player_Spell_ScaledValue_Scaling_Class_10")]
        [TestCase(338385u, 0.003916739d, TestName = "SSC_Player_Spell_ScaledValue_No_Scaling_Class_With_Max_Scaling_Level")]
        public async Task SSC_Player_Spell_ScaledValue_Coefficient(uint spellId, double scaledValue)
        {
            // Arrange
            // Values from simc's allspells.txt at level 90
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = spellId,
                PlayerLevel = 90
            };

            // Act
            var spell = await _spellCreationService.GeneratePlayerSpellAsync(spellOptions);
            var effect = spell.Effects.Single(e => e.EffectIndex == 0);

            // Assert
            Assert.That(effect.ScaledValue, Is.EqualTo(scaledValue).Within(0.0001).Percent);
            Assert.That(effect.ScaledValue, Is.EqualTo(effect.Coefficient * effect.ScaleBudget));
        }

        [Test]
        public async Task SSC_Item_Spell_ScaledValue_Is_BaseValue()
        {
            // Arrange
            var spellOptions = new SimcSpellOptions()
            {
                ItemLevel = 226,
                SpellId = 343538,
                ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
                ItemInventoryType = InventoryType.INVTYPE_TRINKET
            };

            // Act
            var spell = await _spellCreationService.GenerateItemSpellAsync(spellOptions);

            // Assert
            Assert.That(spell.Effects, Is.Not.Empty);
            Assert.That(spell.Effects.Select(e => e.ScaledValue), Is.EqualTo(spell.Effects.Select(e => e.BaseValue)));
        }
    }
}
