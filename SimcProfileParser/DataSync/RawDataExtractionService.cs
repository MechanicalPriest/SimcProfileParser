using Newtonsoft.Json;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SimcProfileParser.DataSync
{
    internal interface IRawDataExtractionService
    {
        void GenerateCombatRatingMultipliers();
        void GenerateStaminaMultipliers();
        void GenerateItemData();
        void GenerateRandomPropData();
        void GenerateSpellData();
    }

    /// <summary>
    /// The purpose of thie class is to process a raw file into 
    /// </summary>
    internal class RawDataExtractionService : IRawDataExtractionService
    {
        private readonly ICacheService _cacheService;

        public RawDataExtractionService(ICacheService cacheService)
        {
            _cacheService = cacheService;

            _cacheService.RegisterFileConfiguration(
                Model.DataSync.SimcFileType.ScaleDataInc,
                "ScaleData.raw",
                "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/sc_scale_data.inc");

            _cacheService.RegisterFileConfiguration(
               Model.DataSync.SimcFileType.ItemDataInc,
               "ItemData.raw",
               "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/item_data.inc");

            _cacheService.RegisterFileConfiguration(
               Model.DataSync.SimcFileType.RandomPropPointsInc,
               "RandomPropPoints.raw",
               "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/rand_prop_points.inc");

            _cacheService.RegisterFileConfiguration(
               Model.DataSync.SimcFileType.ScSpellDataInc,
               "SpellData.raw",
               "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/sc_spell_data.inc");
        }

        void IRawDataExtractionService.GenerateCombatRatingMultipliers()
        {
            var rawData = _cacheService.GetFileContents(Model.DataSync.SimcFileType.ScaleDataInc);

            Regex regexCR = new Regex(@"__combat_ratings_mult_by_ilvl.+?\{.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}).+?\};", RegexOptions.Singleline);

            Match matches = regexCR.Match(rawData);

            GroupCollection groups = matches.Groups;

            float[][] values = new float[4][];
            values[0] = ParseRatingGroup(groups[1]);
            values[1] = ParseRatingGroup(groups[2]);
            values[2] = ParseRatingGroup(groups[3]);
            values[3] = ParseRatingGroup(groups[4]);

            var generatedData = JsonConvert.SerializeObject(values);

            File.WriteAllText(
                Path.Combine(_cacheService.BaseFileDirectory, "CombatRatingMultipliers.json"), 
                generatedData);
        }

        void IRawDataExtractionService.GenerateStaminaMultipliers()
        {
            var rawData = _cacheService.GetFileContents(Model.DataSync.SimcFileType.ScaleDataInc);

            Regex regexCR = new Regex(@"__stamina_mult_by_ilvl.+?\{.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}).+?\};", RegexOptions.Singleline);

            Match matches = regexCR.Match(rawData);

            GroupCollection groups = matches.Groups;

            float[][] values = new float[4][];
            values[0] = ParseRatingGroup(groups[1]);
            values[1] = ParseRatingGroup(groups[2]);
            values[2] = ParseRatingGroup(groups[3]);
            values[3] = ParseRatingGroup(groups[4]);

            var generatedData = JsonConvert.SerializeObject(values);

            File.WriteAllText(
                Path.Combine(_cacheService.BaseFileDirectory, "StaminaMultipliers.json"),
                generatedData);
        }

        private float[] ParseRatingGroup(Group g)
        {
            Regex items = new Regex(@"\s+([01]\.?\d*),");

            float[] values = new float[1300];

            int i = 0;
            foreach (Match m in items.Matches(g.Value))
            {
                values[i++] = float.Parse(m.Groups[1].Value);
            }

            return values;
        }

        void IRawDataExtractionService.GenerateItemData()
        {
            var rawData = _cacheService.GetFileContents(Model.DataSync.SimcFileType.ItemDataInc);

            // Split by the last occurance of "". There is only one string in this data model.
            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var items = new List<SimcRawItem>();
            var itemMods = new List<SimcRawItemMod>();

            foreach (var line in lines)
            {
                // Lines without a text field are very likely item mod data
                if (!line.Contains('"'))
                {
                    // First check if its an item mod data line
                    if (line.Split(',').Count() < 4)
                        continue;

                    // Split the data up
                    var data = line.Split(',');
                    // Clean it up
                    for (var i = 0; i < data.Length; i++)
                    {
                        data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                    }

                    var itemMod = new SimcRawItemMod();

                    // 0 is the typeId
                    itemMod.ModType = (ItemModType)Convert.ToInt32(data[0]);

                    // 1 is the stat allocation
                    itemMod.StatAllocation = Convert.ToInt32(data[1]);

                    // 2 is the socket penalty
                    itemMod.SocketMultiplier = Convert.ToDouble(data[2]);

                    itemMods.Add(itemMod);

                }
                else
                {

                    var item = new SimcRawItem();

                    var nameSegment = line.Substring(0, line.LastIndexOf("\""));
                    var dataSegment = line.Substring(line.LastIndexOf("\"") + 1);

                    item.Name = nameSegment.Substring(nameSegment.IndexOf('\"') + 1);

                    // Split the remaining data up
                    var data = dataSegment.Split(',');
                    // Clean it up
                    for (var i = 0; i < data.Length; i++)
                    {
                        data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                    }

                    // 0 is blank. 1 is itemId
                    item.Id = Convert.ToUInt32(data[1]);

                    // 2 is Flags1
                    uint.TryParse(data[2].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out uint flags1);
                    item.Flags1 = flags1;

                    // 3 is Flags2
                    uint.TryParse(data[3].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out uint flags2);
                    item.Flags2 = flags2;

                    // 4 is TypeFlags
                    uint.TryParse(data[4].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out uint typeFlags);
                    item.TypeFlags = typeFlags;

                    // 5 is base ilvl
                    item.ItemLevel = Convert.ToInt32(data[5]);

                    // 6 is required level
                    item.RequiredLevel = Convert.ToInt32(data[6]);

                    // 7 is required skill
                    item.RequiredSkill = Convert.ToInt32(data[7]);

                    // 8 is required skill level
                    item.RequiredSkillLevel = Convert.ToInt32(data[8]);

                    // 9 is quality
                    item.Quality = Convert.ToInt32(data[9]);

                    // 10 is inventory type
                    // TODO: parse this to the enum value ?
                    item.InventoryType = Convert.ToInt32(data[10]);

                    // 11 is item class
                    item.ItemClass = Convert.ToInt32(data[11]);

                    // 12 is item subclass
                    item.ItemSubClass = Convert.ToInt32(data[12]);

                    // 13 is bind type
                    item.BindType = Convert.ToInt32(data[13]);

                    // 14 is delay
                    float.TryParse(data[14], out float delay);
                    item.Delay = delay;

                    // 15 is damage range
                    float.TryParse(data[15], out float dmgRange);
                    item.DamageRange = dmgRange;

                    // 16 is item modifier
                    float.TryParse(data[16], out float itemMod);
                    item.ItemModifier = itemMod;

                    // 17 is item stats in the form: '0' or '&__item_stats_data[0]'
                    if (data[17] == "0")
                        item.DbcStats = 0;
                    else
                    {
                        item.DbcStats = Convert.ToInt32(
                            data[17].Replace("&__item_stats_data[", "").Trim(']'));
                    }

                    // 18 is dbc stats count
                    item.DbcStatsCount = Convert.ToUInt32(data[18]);

                    // So the DBC stats are the itemMods collection index, and stats count is how many
                    if (item.DbcStatsCount > 0)
                    {
                        for (var i = 0; i < item.DbcStatsCount; i++)
                        {
                            item.ItemMods.Add(itemMods[item.DbcStats + i]);
                        }
                    }

                    // 19 is class mask
                    uint.TryParse(data[19].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out uint classMask);
                    item.ClassMask = classMask;

                    // 20 is race mask
                    ulong.TryParse(data[20].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out ulong raceMask);
                    item.RaceMask = raceMask;

                    // 21, 22 and 23 are the 3 socket colours
                    item.SocketColour = new int[3]
                    {
                    Convert.ToInt32(data[21]),
                    Convert.ToInt32(data[22]),
                    Convert.ToInt32(data[23])
                    };

                    // 24 is gem properties
                    item.GemProperties = Convert.ToInt32(data[24]);

                    // 25 is socket bonus id
                    item.SocketBonusId = Convert.ToInt32(data[25]);

                    // 26 is set bonus id
                    item.SetId = Convert.ToInt32(data[26]);

                    // 27 is curve id
                    item.CurveId = Convert.ToInt32(data[27]);

                    // 28 is artifact id
                    item.ArtifactId = Convert.ToUInt32(data[28]);

                    items.Add(item);
                }
            }

            var generatedData = JsonConvert.SerializeObject(items);

            File.WriteAllText(
                Path.Combine(_cacheService.BaseFileDirectory, "ItemData.json"),
                generatedData);
        }

        void IRawDataExtractionService.GenerateRandomPropData()
        {
            var rawData = _cacheService.GetFileContents(Model.DataSync.SimcFileType.RandomPropPointsInc);

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var randomProps = new List<SimcRawRandomPropData>();

            foreach(var line in lines)
            {
                // Only process lines containing props
                if (!line.Contains(','))
                    continue;

                var props = line.Split(',');

                // Skip the line if it doesn't have enough props on it
                if (props.Length < 10)
                    continue;

                // Clean it up
                for (var i = 0; i < props.Length; i++)
                {
                    props[i] = props[i].Replace("}", "").Replace("{", "").Trim();
                }

                var newProp = new SimcRawRandomPropData();

                // 0 is the ilvl
                newProp.ItemLevel = Convert.ToUInt32(props[0]);

                // 1 is the damage replace stat
                newProp.DamageReplaceStat = Convert.ToUInt32(props[1]);

                // 2 is damage secondary
                newProp.DamageSecondary = Convert.ToUInt32(props[2]);

                // 3, 4, 5, 6, 7 are the Epic property data
                // 8, 9, 10, 11, 12 are the rare prop data
                // 13, 14, 15, 16, 17 are the uncommon prop data
                newProp.Epic = new float[5];
                newProp.Rare = new float[5];
                newProp.Uncommon = new float[5];

                for (var i = 0; i < 5; i++)
                {
                    float.TryParse(props[i + 3], out float epicValue);
                    float.TryParse(props[i + 8], out float rareValue);
                    float.TryParse(props[i + 13], out float uncommonValue);

                    newProp.Epic[i] = epicValue;
                    newProp.Rare[i] = rareValue;
                    newProp.Uncommon[i] = uncommonValue;
                }

                randomProps.Add(newProp);
            }

            var generatedData = JsonConvert.SerializeObject(randomProps);

            File.WriteAllText(
                Path.Combine(_cacheService.BaseFileDirectory, "RandomPropData.json"),
                generatedData);
        }
        void IRawDataExtractionService.GenerateSpellData()
        {
            var rawData = _cacheService.GetFileContents(Model.DataSync.SimcFileType.ScSpellDataInc);

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var spells = new List<SimcRawSpell>();

            foreach(var line in lines)
            {
                if (!line.Contains('"'))
                    continue;

                var nameSegment = line.Substring(0, line.LastIndexOf("\""));
                var dataSegment = line.Substring(line.LastIndexOf("\"") + 1);

                var spell = new SimcRawSpell();

                spell.Name = nameSegment.Substring(nameSegment.IndexOf('\"') + 1);

                // Split the remaining data up
                var data = dataSegment.Split(',');
                // Clean it up
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                }

                // 0 is blank, 1 is spellId
                spell.Id = Convert.ToUInt32(data[1]);

                // 2 is spell school
                spell.School = Convert.ToUInt32(data[2]);

                // 3 is projectile sped
                spell.ProjectileSpeed = Convert.ToDouble(data[3]);

                // 4 is a hex race mask
                ulong.TryParse(data[4].Replace("0x", ""),
                    System.Globalization.NumberStyles.HexNumber, null, out ulong raceMask);
                spell.RaceMask = raceMask;

                // 5 is a hex class mask
                uint.TryParse(data[5].Replace("0x", ""),
                    System.Globalization.NumberStyles.HexNumber, null, out uint classMask);
                spell.RaceMask = classMask;

                // 6 is scaling type
                spell.ScalingType = Convert.ToInt32(data[6]);

                // 7 is max scaling level
                spell.MaxScalingLevel = Convert.ToInt32(data[7]);

                // 8 is spell level
                spell.SpellLevel = Convert.ToUInt32(data[8]);

                // 9 is max level
                spell.MaxLevel = Convert.ToUInt32(data[9]);

                // 10 is spell level
                spell.RequireMaxLevel = Convert.ToUInt32(data[10]);

                // 11 is minimum range
                spell.MinRange = Convert.ToDouble(data[11]);

                // 12 is minimum range
                spell.MaxRange = Convert.ToDouble(data[12]);

                // 13 is cooldown
                spell.Cooldown = Convert.ToUInt32(data[13]);

                // 14 is gcd
                spell.Gcd = Convert.ToUInt32(data[14]);

                // 15 is category cd
                spell.CategoryCooldown = Convert.ToUInt32(data[15]);

                // 16 is charges
                spell.Charges = Convert.ToUInt32(data[16]);

                // 17 is charges cd
                spell.ChargeCooldown = Convert.ToUInt32(data[17]);

                // 18 is category
                spell.Category = Convert.ToUInt32(data[18]);

                // 19 is dmg class
                spell.DamageClass = Convert.ToUInt32(data[19]);

                // 20 is max targets
                spell.MaxTargets = Convert.ToInt32(data[20]);

                // 21 is Duration
                spell.Duration = Convert.ToDouble(data[21]);

                // 22 is max stacks
                spell.MaxStack = Convert.ToUInt32(data[22]);

                // 23 is proc chance
                spell.ProcChance = Convert.ToUInt32(data[23]);

                // 24 is proc charges
                spell.ProcCharges = Convert.ToInt32(data[24]);

                // 25 is proc chance
                spell.ProcFlags = Convert.ToUInt32(data[25]);

                // 26 is icd
                spell.InternalCooldown = Convert.ToUInt32(data[26]);

                // 27 is rppm
                spell.Rppm = Convert.ToDouble(data[27]);

                // 28 is eq class
                spell.EquippedClass = Convert.ToUInt32(data[28]);

                // 29 is eq class
                spell.EquippedInventoryTypeMask = Convert.ToUInt32(data[29]);

                // 30 is eq class
                spell.EquippedSubclassMask = Convert.ToUInt32(data[30]);

                // 31 is cast time
                spell.CastTime = Convert.ToInt32(data[31]);

                // 32 - 46. Next up is something of length NUM_SPELL_FLAGS = 15
                spell.Attributes = new uint[15];
                for(var i = 0; i < spell.Attributes.Length; i++)
                {
                    spell.Attributes[i] = Convert.ToUInt32(data[i + 32]);
                }

                // 47 - 50. Next up is something of length NUM_CLASS_FAMILY_FLAGS = 4
                spell.ClassFlags = new uint[4];
                for (var i = 0; i < spell.ClassFlags.Length; i++)
                {
                    spell.ClassFlags[i] = Convert.ToUInt32(data[i + 47]);
                }

                // 51 is class flags family
                spell.ClassFlagsFamily = Convert.ToUInt32(data[51]);

                // 52 is hex class flags family
                uint.TryParse(data[52].Replace("0x", ""),
                    System.Globalization.NumberStyles.HexNumber, null, out uint stanceMask);
                spell.StanceMask = stanceMask;

                // 53 is mechanic
                spell.Mechanic = Convert.ToUInt32(data[53]);

                // 54 is az power id
                spell.PowerId = Convert.ToUInt32(data[54]);

                // 55 is mechanic
                spell.EssenceId = Convert.ToUInt32(data[55]);

                // We don't have a practice use for the counts metadata
                // 56 is effects count

                // 57 is power count

                // 58 is driver count

                // 59 is lebel count

                spells.Add(spell);
            }

            var generatedData = JsonConvert.SerializeObject(spells);

            File.WriteAllText(
                Path.Combine(_cacheService.BaseFileDirectory, "SpellData.json"),
                generatedData);
        }
    }
}
