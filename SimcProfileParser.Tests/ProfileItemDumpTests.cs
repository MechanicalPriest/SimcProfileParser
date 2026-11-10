using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SimcProfileParser.Model.RawData;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    /// <summary>
    /// Prints every equipped item generated from a profile, with its stats and a stat total, for checking generated items
    /// against a character's in-game stats. Also checks each item's level against the "# Item Name (334)" comment the
    /// simc addon writes above it. Run with:
    /// dotnet test --filter "FullyQualifiedName~ProfileItemDumpTests" --logger "console;verbosity=detailed"
    /// </summary>
    [TestFixture]
    class ProfileItemDumpTests
    {
        [Test]
        [TestCase("Afternoon.simc")]
        public async Task Dump_Equipped_Items(string profileFile)
        {
            // Arrange
            var sgs = new SimcGenerationService(NullLoggerFactory.Instance);
            var lines = await File.ReadAllLinesAsync(Path.Combine("RawData", profileFile));

            // Act
            var profile = await sgs.GenerateProfileAsync(new List<string>(lines));

            // Assert / output
            var parsedItems = profile.ParsedProfile.Items;
            var generatedItems = profile.GeneratedItems;
            Assert.That(generatedItems, Has.Count.EqualTo(parsedItems.Count), "Generated items should line up with parsed items");

            // The simc addon writes the in-game item level in the comment above each item, e.g. "# Item Name (334)"
            var expectedItemLevels = new Dictionary<string, int>();
            int? lastCommentItemLevel = null;
            foreach (var line in lines)
            {
                var comment = Regex.Match(line, @"^#.*\((\d+)\)\s*$");
                if (comment.Success)
                {
                    lastCommentItemLevel = int.Parse(comment.Groups[1].Value);
                    continue;
                }

                var itemLine = Regex.Match(line, @"^(\w+)=,id=");
                if (itemLine.Success && lastCommentItemLevel.HasValue)
                    expectedItemLevels[itemLine.Groups[1].Value] = lastCommentItemLevel.Value;

                lastCommentItemLevel = null;
            }

            var output = new StringBuilder();
            var totals = new SortedDictionary<ItemModType, int>();
            var itemLevelMismatches = new List<string>();

            for (var i = 0; i < parsedItems.Count; i++)
            {
                var parsed = parsedItems[i];
                var item = generatedItems[i];

                if (!item.Equipped)
                    continue;

                output.AppendLine($"{parsed.Slot}: {item.Name} ({item.ItemId}) ilvl {item.ItemLevel}{(item.ItemLevelForced ? " (forced)" : "")} {item.Quality}");

                if (expectedItemLevels.TryGetValue(parsed.Slot, out var expectedItemLevel) && expectedItemLevel != item.ItemLevel)
                    itemLevelMismatches.Add($"{parsed.Slot}: {item.Name} ({item.ItemId}) is ilvl {item.ItemLevel}, expected {expectedItemLevel}");
                output.AppendLine($"    bonus_id={string.Join("/", parsed.BonusIds)}"
                    + (parsed.EnchantId > 0 ? $" enchant_id={parsed.EnchantId}" : "")
                    + (parsed.GemIds.Count > 0 ? $" gem_id={string.Join("/", parsed.GemIds)}" : "")
                    + (parsed.CraftedStatIds.Count > 0 ? $" crafted_stats={string.Join("/", parsed.CraftedStatIds)}" : ""));

                foreach (var mod in item.Mods)
                {
                    output.AppendLine($"    {mod.Type,-32} {mod.StatRating,7}   (allocation {mod.RawStatAllocation})");
                    totals[mod.Type] = totals.GetValueOrDefault(mod.Type) + mod.StatRating;
                }

                foreach (var gem in item.Gems)
                {
                    output.AppendLine($"    gem {gem.Type,-28} {gem.StatRating,7}");
                    totals[gem.Type] = totals.GetValueOrDefault(gem.Type) + gem.StatRating;
                }

                if (item.Enchant != null)
                {
                    output.AppendLine($"    enchant {item.Enchant.EnchantId} {item.Enchant.Name}"
                        + (item.Enchant.SpellIds.Count > 0 ? $" (spells {string.Join("/", item.Enchant.SpellIds)})" : ""));
                    foreach (var stat in item.Enchant.Stats)
                    {
                        output.AppendLine($"    enchant {stat.Type,-24} {stat.StatRating,7}");
                        totals[stat.Type] = totals.GetValueOrDefault(stat.Type) + stat.StatRating;
                    }
                }

                foreach (var effect in item.Effects)
                    output.AppendLine($"    effect {effect.EffectId} spell {effect.Spell?.SpellId} {effect.Spell?.Name}");
            }

            output.AppendLine();
            output.AppendLine("Totals from item mods, gems and enchants (no base stats or effects):");
            foreach (var (type, value) in totals)
                output.AppendLine($"    {type,-32} {value,7}");

            TestContext.Out.WriteLine(output.ToString());

            Assert.That(itemLevelMismatches, Is.Empty, "Item levels should match the profile's item comments");
        }
    }
}
