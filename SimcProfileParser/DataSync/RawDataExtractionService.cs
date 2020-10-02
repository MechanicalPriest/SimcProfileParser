using Newtonsoft.Json;
using SimcProfileParser.Interfaces.DataSync;
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
        }

        void IRawDataExtractionService.GenerateCombatRatingMultipliers()
        {
            var rawData = _cacheService.GetFileContents(Model.DataSync.SimcFileType.ScaleDataInc);

            Regex regexCR = new Regex(@"__combat_ratings_mult_by_ilvl.+?\{.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}).+?\};", RegexOptions.Singleline);

            Match matches = regexCR.Match(rawData);

            GroupCollection groups = matches.Groups;

            float[][] values = new float[4][];
            values[0] = ParseRatingGroup(groups[1]);
            values[1] = ParseRatingGroup(groups[1]);
            values[2] = ParseRatingGroup(groups[1]);
            values[3] = ParseRatingGroup(groups[1]);

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
            values[1] = ParseRatingGroup(groups[1]);
            values[2] = ParseRatingGroup(groups[1]);
            values[3] = ParseRatingGroup(groups[1]);

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

            foreach (var line in lines)
            {
                // Skip any lines without a text field
                if (!line.Contains('"'))
                    continue;

                var item = new SimcRawItem();

                var nameSegment = line.Substring(0, line.LastIndexOf("\""));
                var dataSegment = line.Substring(line.LastIndexOf("\"") + 1);

                item.Name = nameSegment.Substring(nameSegment.IndexOf('\"') + 1);

                // Split the remaining data up
                var data = dataSegment.Split(',');
                // Clean it up
                for(var i = 0; i < data.Length; i++)
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

                // 17 is item stats
                // TODO: item stats
                item.DbcStats = data[17];

                // 18 is dbc stats count
                item.DbcStatsCount = Convert.ToUInt32(data[18]);

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

            var generatedData = JsonConvert.SerializeObject(items);

            File.WriteAllText(
                Path.Combine(_cacheService.BaseFileDirectory, "ItemData.json"),
                generatedData);
        }
    }
}
