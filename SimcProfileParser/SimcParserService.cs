using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model.Profile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            var runtime = Stopwatch.StartNew();

            var profile = new SimcParsedProfile();

            // Loop through each of the lines and parse them on their own merit.

            List<SimcParsedLine> validLines = new List<SimcParsedLine>();

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

                _logger?.LogDebug($"New raw line: {currentLine}");

                var kvp = currentLine.Split('=');
                var identifier = kvp.FirstOrDefault();
                var valueString = string.Join("", kvp.Skip(1)); // All but the identifier

                var parsedLine = new SimcParsedLine()
                {
                    RawLine = rawLine,
                    CleanLine = currentLine,
                    Identifier = identifier,
                    Value = valueString
                };

                validLines.Add(parsedLine);
            }

            _logger?.LogInformation($"Found {validLines.Count} valid lines");

            if (validLines.Count == 0)
                return profile;

            profile.ProfileLines = validLines;

            foreach (var line in validLines)
            {
                _logger?.LogTrace($"Processing line: {line.CleanLine} " +
                    $"Identifier: ({line.Identifier}) Value: {line.Value}");

                switch (line.Identifier)
                {
                    // Items
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
                        _logger?.LogDebug($"Trying to parse item for slot: ({line.Identifier}) with values: {line.Value}");
                        // TODO: parse items
                        break;

                    // TODO: Add the remaining specs
                    case "priest":
                    case "paladin":
                    case "monk":
                    case "shaman":
                    case "druid":
                    case "rogue":
                    case "mage":
                        _logger?.LogDebug($"Setting player name for class ({line.Identifier}) with value: {line.Value}");
                        profile.Name = line.Value.Trim().Trim('"');
                        break;

                    case "level":
                        _logger?.LogDebug($"Trying to set level ({line.Identifier}) with value: {line.Value}");
                        TryApplyLevel(profile, line.Value.Trim());
                        break;

                    case "race":
                        _logger?.LogDebug($"Trying to set race ({line.Identifier}) with value: {line.Value}");
                        profile.Race = line.Value.Trim();
                        break;

                    case "region":
                        _logger?.LogDebug($"Trying to set region ({line.Identifier}) with value: {line.Value}");
                        profile.Region = line.Value.Trim();
                        break;

                    case "server":
                        _logger?.LogDebug($"Trying to set server ({line.Identifier}) with value: {line.Value}");
                        profile.Server = line.Value.Trim();
                        break;

                    case "role":
                        _logger?.LogDebug($"Trying to set role ({line.Identifier}) with value: {line.Value}");
                        profile.Role = line.Value.Trim();
                        break;

                    case "professions":
                        _logger?.LogDebug($"Trying to parse profession ({line.Identifier}) with value: {line.Value}");
                        // TODO: Parse profession
                        break;

                    case "talents":
                        _logger?.LogDebug($"Trying to parse talents ({line.Identifier}) with value: {line.Value}");
                        // TODO: Parse profession
                        break;

                    case "spec":
                        _logger?.LogDebug($"Trying to set spec ({line.Identifier}) with value: {line.Value}");
                        profile.Spec = line.Value.Trim();
                        break;

                    case "covenant":
                        _logger?.LogDebug($"Trying to parse covenant ({line.Identifier}) with value: {line.Value}");
                        // TODO: Parse covenant
                        break;

                    case "soulbind":
                        _logger?.LogDebug($"Trying to parse soulbind ({line.Identifier}) with value: {line.Value}");
                        // TODO: Parse soulbind
                        break;

                    case "conduits_available":
                        _logger?.LogDebug($"Trying to parse conduits_available ({line.Identifier}) with value: {line.Value}");
                        TryApplyConduitData(profile, line.Value);
                        break;

                    case "renown":
                        _logger?.LogDebug($"Trying to parse renown ({line.Identifier}) with value: {line.Value}");

                        if (int.TryParse(line.Value.Trim(), out int renown))
                        {
                            profile.Renown = renown;
                        }
                        else
                            _logger?.LogWarning($"Invalid renown value: {line.Value}");
                        break;

                    default:
                        _logger?.LogWarning($"Unrecognised identifier found: {line.Identifier}");
                        break;
                }
            }

            runtime.Stop();
            _logger?.LogInformation($"Done processing profile in {runtime.ElapsedMilliseconds}ms");

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

        private void TryApplyConduitData(SimcParsedProfile profile, string valueString)
        {
            if(profile.Conduits.Count > 0)
            {
                _logger?.LogWarning($"Overriding existing conduits. " +
                    $"There should only be one conduits_available provided per profile.");
            }    

            // Valid conduit string
            // conduits_available=116:1/78:1/82:1/84:1/101:1/69:1/73:1/67:1/66:1
            if (valueString.Contains(":"))
            {
                var results = new List<SimcParsedConduit>();

                var conduitParts = valueString.Split('/');

                foreach(var part in conduitParts)
                {
                    var kvp = part.Split(':');

                    if (kvp.Length != 2 ||
                        !int.TryParse(kvp[0], out int conduitId) ||
                        !int.TryParse(kvp[1], out int conduitRank))
                    {
                        _logger?.LogWarning($"Invalid conduit found in part ({part}): {valueString}");
                        continue;
                    }

                    _logger?.LogDebug($"Adding new conduit ({conduitId}) at rank: {conduitRank}");

                    var conduit = new SimcParsedConduit()
                    {
                        ConduitId = conduitId,
                        Rank = conduitRank
                    };

                    results.Add(conduit);
                }

                profile.Conduits = new ReadOnlyCollection<SimcParsedConduit>(results);
            }
            else
            {
                _logger?.LogDebug($"No valid conduits found in string: {valueString}");
            }
        }
    }
}
