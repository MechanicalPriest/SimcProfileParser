using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace SimcProfileParser
{
    public class SimcParserService : ISimcParserService
    {
        private readonly ILogger<SimcParserService> _logger;

        public SimcParserService(ILogger<SimcParserService> logger = null)
        {
            _logger = logger;
        }

        public SimcParsedProfile ParseProfileAsync(string profileString)
        {
            if (string.IsNullOrEmpty(profileString))
            {
                _logger?.LogWarning("Incoming profileString is empty.");
                throw new ArgumentNullException(nameof(profileString));
            }

            _logger?.LogInformation($"Splitting a string with {profileString.Length} character(s)");

            var lines = profileString.Split("\r\n").ToList();

            _logger?.LogInformation($"Created {lines.Count} lines to be processed");

            return ParseProfileAsync(lines);
        }

        public SimcParsedProfile ParseProfileAsync(List<string> profileLines)
        {
            _logger?.LogInformation($"Parsing a profileString with {profileLines.Count} lines.");

            var profile = new SimcParsedProfile();

            // Loop through each of the lines and parse them on their own merit.

            List<string> validLines = new List<string>();

            foreach (var rawLine in profileLines)
            {
                // Lines either start with the identifier=value combo or are commented, then identifier or comment.
                // e.g.: # shoulder=,id=173247,bonus_id=6716/1487/6977/6649/6647

                // TODO: pull the datetime/simc version from here
                var currentLine = rawLine;


                // Remove the comment #
                if (!string.IsNullOrEmpty(rawLine) && rawLine[0] == '#')
                    currentLine = currentLine.Trim('#').Trim();

                TryApplySimcVersion(profile, currentLine);
                TryApplyProfileDate(profile, currentLine);

                // Check if there is an identifier
                if (string.IsNullOrEmpty(rawLine) || !rawLine.Contains('='))
                    continue;

                _logger?.LogDebug($"New line: {currentLine}");

                validLines.Add(currentLine);
            }

            _logger?.LogInformation($"Found {validLines.Count} valid lines");

            if (validLines.Count == 0)
                return profile;

            foreach (var line in validLines)
            {
                _logger?.LogDebug($"Processing line: {line}");

                var kvp = line.Split('=');
                var identifier = kvp.FirstOrDefault();
                var valueString = kvp.LastOrDefault();

                _logger?.LogTrace($"Identifier: ({identifier}) Value: {valueString}");

                switch (identifier)
                {
                    // Character stuff
                    case "head":
                    case "neck":
                    case "shoulder":
                    case "back":
                    case "chest":
                    case "wrist":
                    case "hands":
                    case "waist":
                    case "legs":
                    case "feet":
                    case "finger1":
                    case "finger2":
                    case "trinket1":
                    case "trinket2":
                    case "main_hand":
                    case "off_hand":
                        _logger.LogDebug($"Trying to parse item for slot: ({identifier}) with values: {valueString}");
                        break;
                    // TODO: Add the remaining specs
                    case "priest":
                    case "paladin":
                    case "monk":
                    case "shaman":
                    case "druid":
                    case "rogue":
                    case "mage":
                        _logger.LogDebug($"Setting player name for class ({identifier}) with value: {valueString}");
                        profile.Name = valueString.Trim().Trim('"');
                        break;
                    case "level":
                        _logger.LogDebug($"Trying to set level ({identifier}) with value: {valueString}");
                        TryApplyLevel(profile, valueString.Trim());
                        break;
                    default:
                        _logger?.LogInformation($"Unrecognised identifier found: {identifier}");
                        break;
                }
            }

            return profile;
        }

        /// <summary>
        /// Check a line for the SimC Addon version string, and set it on result if present
        /// </summary>
        private void TryApplySimcVersion(SimcParsedProfile result, string profileLine)
        {
            var versionPrefix = "SimC Addon ";

            if (profileLine.Length > versionPrefix.Length &&
                profileLine.Substring(0, versionPrefix.Length) == versionPrefix)
            {
                var version = profileLine
                    .Substring(versionPrefix.Length, profileLine.Length - versionPrefix.Length);

                _logger?.LogDebug($"Found SimC version string ({version}) on line: {profileLine}");

                result.SimcAddonVersion = version;
            }
        }

        private void TryApplyProfileDate(SimcParsedProfile result, string profileLine)
        {
            // 38 is the minimum length for the character line
            // ??? - SPEC - YYYY-MM-DD HH:NN - US/REA
            if(profileLine.Length > 38)
            {
                var parts = profileLine.Split(" - ");
                // There are 4 parts if it's the correct line
                if(parts.Length == 4)
                {
                    // and we want the third
                    var dateTime = parts[2].Trim();

                    DateTime.TryParse(dateTime, out DateTime parsedDateTime);

                    _logger?.LogDebug($"Found SimC collection date string ({parsedDateTime}) on line: {profileLine}");

                    result.CollectionDate = parsedDateTime;
                }

            }
        }

        private void TryApplyLevel(SimcParsedProfile result, string valueString)
        {
            int.TryParse(valueString, out int level);

            _logger?.LogDebug($"Setting level to ({level}) from: {valueString}");

            result.Level = level;
        }
    }
}
