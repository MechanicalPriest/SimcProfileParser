using Newtonsoft.Json;
using SimcProfileParser.Interfaces.DataSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SimcProfileParser.DataSync
{
    internal interface IRawDataExtractionService
    {
        void GenerateCombatRatingMultipliers();
        void GenerateStaminaMultipliers();
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
    }
}
