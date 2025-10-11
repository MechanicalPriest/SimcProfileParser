using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model.Profile;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace SimcProfileParser
{
    public class SimcParserService : ISimcParserService
    {
        private readonly ILogger<SimcParserService> _logger;

        public SimcParserService(ILogger<SimcParserService> logger)
        {
            _logger = logger;
        }

        public SimcParsedProfile ParseProfileAsync(List<string> profileLines)
        {
            _logger?.LogInformation("Parsing a profileString with {profileLines.Count} lines.", profileLines.Count);
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

                _logger?.LogDebug("New raw line: {currentLine}", currentLine);

                var kvp = currentLine.Split('=');
                var identifier = kvp.FirstOrDefault();
                var valueString = string.Join("=", kvp.Skip(1)); // All but the identifier

                var parsedLine = new SimcParsedLine()
                {
                    RawLine = rawLine,
                    CleanLine = currentLine,
                    Identifier = identifier,
                    Value = valueString
                };

                validLines.Add(parsedLine);
            }

            _logger?.LogInformation("Found {validLines.Count} valid lines", validLines.Count);

            if (validLines.Count == 0)
                return profile;

            profile.ProfileLines = validLines;

            foreach (var line in validLines)
            {
                _logger?.LogTrace("Processing line: {line.CleanLine} " +
                    "Identifier: ({line.Identifier}) Value: {line.Value}", line.CleanLine, line.Identifier, line.Value);

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
                        _logger?.LogDebug("Trying to parse item for slot: ({line.Identifier}) with values: {line.Value}", line.Identifier, line.Value);
                        TryApplyItem(profile, line);
                        break;

                    // TODO: Add the remaining specs
                    case "priest":
                    case "paladin":
                    case "monk":
                    case "shaman":
                    case "druid":
                    case "rogue":
                    case "mage":
                    case "deathknight":
                    case "demonhunter":
                    case "hunter":
                    case "warlock":
                    case "warrior":
                        _logger?.LogDebug("Setting player name for class ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        profile.Name = line.Value.Trim().Trim('"');
                        TryApplyClass(profile, line.Identifier.Trim());
                        break;

                    case "level":
                        _logger?.LogDebug("Trying to set level ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        TryApplyLevel(profile, line.Value.Trim());
                        break;

                    case "race":
                        _logger?.LogDebug("Trying to set race ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        TryApplyRace(profile, line);
                        break;

                    case "region":
                        _logger?.LogDebug("Trying to set region ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        profile.Region = line.Value.Trim();
                        break;

                    case "server":
                        _logger?.LogDebug("Trying to set server ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        profile.Server = line.Value.Trim();
                        break;

                    case "role":
                        _logger?.LogDebug("Trying to set role ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        profile.Role = line.Value.Trim();
                        break;

                    case "professions":
                        _logger?.LogDebug("Trying to parse profession ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        TryApplyProfessions(profile, line);
                        break;

                    case "class_talents":
                        _logger?.LogDebug("Trying to parse class talents ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        TryApplyTalents(profile, line.Value);
                        break;

                    case "spec":
                        _logger?.LogDebug("Trying to set spec ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        profile.Spec = line.Value.Trim();
                        break;

                    case "covenant":
                        _logger?.LogDebug("Trying to parse covenant ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        profile.Covenant = line.Value.Trim();
                        break;

                    case "soulbind":
                        _logger?.LogDebug("Trying to parse soulbind ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        TryApplySoulbind(profile, line);
                        break;

                    case "conduits_available":
                        _logger?.LogDebug("Trying to parse conduits_available ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);
                        TryApplyConduitData(profile, line.Value);
                        break;

                    case "renown":
                        _logger?.LogDebug("Trying to parse renown ({line.Identifier}) with value: {line.Value}", line.Identifier, line.Value);

                        if (int.TryParse(line.Value.Trim(), out int renown))
                        {
                            profile.Renown = renown;
                        }
                        else
                            _logger?.LogWarning("Invalid renown value: {line.Value}", line.Value);
                        break;

                    default:
                        _logger?.LogWarning("Unrecognised identifier found: {line.Identifier}", line.Identifier);
                        break;
                }
            }

            // Call this last in case the class line is after the spec line.
            TryApplySpecId(profile);

            runtime.Stop();
            _logger?.LogInformation("Done processing profile in {runtime.ElapsedMilliseconds}ms", runtime.ElapsedMilliseconds);

            return profile;
        }

        private void TryApplySpecId(SimcParsedProfile profile)
        {
            var specId = Specialisation.SPEC_NONE;

            // from specialization_string in sc_const_data.cpp
            switch ((Class)profile.ClassId)
            {
                case Class.Priest:
                    if (profile.Spec == "discipline")
                        specId = Specialisation.PRIEST_DISCIPLINE;
                    if (profile.Spec == "holy")
                        specId = Specialisation.PRIEST_HOLY;
                    if (profile.Spec == "shadow")
                        specId = Specialisation.PRIEST_SHADOW;
                    break;

                case Class.Warrior:
                    if (profile.Spec == "arms")
                        specId = Specialisation.WARRIOR_ARMS;
                    if (profile.Spec == "fury")
                        specId = Specialisation.WARRIOR_FURY;
                    if (profile.Spec == "protection")
                        specId = Specialisation.WARRIOR_PROTECTION;
                    break;

                case Class.Paladin:
                    if (profile.Spec == "holy")
                        specId = Specialisation.PALADIN_HOLY;
                    if (profile.Spec == "protection")
                        specId = Specialisation.PALADIN_PROTECTION;
                    if (profile.Spec == "retribution")
                        specId = Specialisation.PALADIN_RETRIBUTION;
                    break;

                case Class.Hunter:
                    if (profile.Spec == "beast_mastery")
                        specId = Specialisation.HUNTER_BEAST_MASTERY;
                    if (profile.Spec == "marksmanship")
                        specId = Specialisation.HUNTER_MARKSMANSHIP;
                    if (profile.Spec == "survival")
                        specId = Specialisation.HUNTER_SURVIVAL;
                    break;

                case Class.Rogue:
                    if (profile.Spec == "assassination")
                        specId = Specialisation.ROGUE_ASSASSINATION;
                    if (profile.Spec == "outlaw")
                        specId = Specialisation.ROGUE_OUTLAW;
                    if (profile.Spec == "subtlety")
                        specId = Specialisation.ROGUE_SUBTLETY;
                    break;

                case Class.DeathKnight:
                    if (profile.Spec == "blood")
                        specId = Specialisation.DEATH_KNIGHT_BLOOD;
                    if (profile.Spec == "frost")
                        specId = Specialisation.DEATH_KNIGHT_FROST;
                    if (profile.Spec == "unholy")
                        specId = Specialisation.DEATH_KNIGHT_UNHOLY;
                    break;

                case Class.Shaman:
                    if (profile.Spec == "elemental")
                        specId = Specialisation.SHAMAN_ELEMENTAL;
                    if (profile.Spec == "enhancement")
                        specId = Specialisation.SHAMAN_ENHANCEMENT;
                    if (profile.Spec == "restoration")
                        specId = Specialisation.SHAMAN_RESTORATION;
                    break;

                case Class.Mage:
                    if (profile.Spec == "arcane")
                        specId = Specialisation.MAGE_ARCANE;
                    if (profile.Spec == "fire")
                        specId = Specialisation.MAGE_FIRE;
                    if (profile.Spec == "frost")
                        specId = Specialisation.MAGE_FROST;
                    break;

                case Class.Warlock:
                    if (profile.Spec == "affliction")
                        specId = Specialisation.WARLOCK_AFFLICTION;
                    if (profile.Spec == "demonology")
                        specId = Specialisation.WARLOCK_DEMONOLOGY;
                    if (profile.Spec == "destruction")
                        specId = Specialisation.WARLOCK_DESTRUCTION;
                    break;

                case Class.Monk:
                    if (profile.Spec == "brewmaster")
                        specId = Specialisation.MONK_BREWMASTER;
                    if (profile.Spec == "mistweaver")
                        specId = Specialisation.MONK_MISTWEAVER;
                    if (profile.Spec == "windwalker")
                        specId = Specialisation.MONK_WINDWALKER;
                    break;

                case Class.Druid:
                    if (profile.Spec == "balance")
                        specId = Specialisation.DRUID_BALANCE;
                    if (profile.Spec == "feral")
                        specId = Specialisation.DRUID_FERAL;
                    if (profile.Spec == "guardian")
                        specId = Specialisation.DRUID_GUARDIAN;
                    if (profile.Spec == "restoration")
                        specId = Specialisation.DRUID_RESTORATION;
                    break;

                case Class.DemonHunter:
                    if (profile.Spec == "havoc")
                        specId = Specialisation.DEMON_HUNTER_HAVOC;
                    if (profile.Spec == "vengeance")
                        specId = Specialisation.DEMON_HUNTER_VENGEANCE;
                    break;

                case Class.Evoker:
                    if (profile.Spec == "devastation")
                        specId = Specialisation.EVOKER_DEVASTATION;
                    if (profile.Spec == "preservation")
                        specId = Specialisation.EVOKER_PRESERVATION;
                    break;

                case Class.None:
                default:
                    break;
            }

            profile.SpecId = (int)specId;
        }

        private void TryApplyClass(SimcParsedProfile profile, string classValue)
        {
            profile.Class = classValue;
            var classId = (classValue) switch
            {
                "priest" => Class.Priest,
                "paladin" => Class.Paladin,
                "monk" => Class.Monk,
                "shaman" => Class.Shaman,
                "druid" => Class.Druid,
                "rogue" => Class.Rogue,
                "mage" => Class.Mage,
                "deathknight" => Class.DeathKnight,
                "demonhunter" => Class.DemonHunter,
                "hunter" => Class.Hunter,
                "warlock" => Class.Warlock,
                "warrior" => Class.Warrior,
                "evoker" => Class.Evoker,
                _ => Class.None
            };
            profile.ClassId = (int)classId;
        }

        private void TryApplyRace(SimcParsedProfile profile, SimcParsedLine line)
        {
            // Set Race and RaceId
            profile.Race = line.Value.Trim();
            var raceId = (profile.Race.ToLower()) switch
            {
                "human" => Race.Human,
                "orc" => Race.Orc,
                "dwarf" => Race.Dwarf,
                "night_elf" => Race.NightElf,
                "undead" => Race.Undead,
                "tauren" => Race.Tauren,
                "gnome" => Race.Gnome,
                "troll" => Race.Troll,
                "goblin" => Race.Goblin,
                "blood_elf" => Race.BloodElf,
                "draenei" => Race.Draenei,
                "dark_iron_dwarf" => Race.DarkIronDwarf,
                "vulpera" => Race.Vulpera,
                "maghar_orc" => Race.MagharOrc,
                "mechagnome" => Race.Mechagnome,
                "worgen" => Race.Worgen,
                "pandaren" => Race.Pandaren,
                "pandaren_alliance" => Race.PandarenAlliance,
                "pandaren_horde" => Race.PandarenHorde,
                "nightborne" => Race.Nightborne,
                "highmountain_tauren" => Race.HighmountainTauren,
                "void_elf" => Race.VoidElf,
                "lightforged_draenei" => Race.LightforgedDraenei,
                "zandalari_troll" => Race.ZandalariTroll,
                "kul_tiran" => Race.KulTiran,
                _ => Race.NoRace,
            };

            profile.RaceId = (int)raceId;
        }

        /// <summary>
        /// Check a line for the SimC Addon version string, and set it on result if present
        /// </summary>
        private void TryApplySimcVersion(SimcParsedProfile result, string valueString)
        {
            var versionPrefix = "SimC Addon ";

            if (valueString.Length > versionPrefix.Length &&
                valueString.Substring(0, versionPrefix.Length) == versionPrefix)
            {
                var version = valueString[versionPrefix.Length..];

                _logger?.LogDebug("Found SimC version string ({version}) on line: {valueString}", version, valueString);

                result.SimcAddonVersion = version;
            }
        }

        private void TryApplyProfileDate(SimcParsedProfile result, string valueString)
        {
            // 38 is the minimum length for the character line
            // ??? - SPEC - YYYY-MM-DD HH:NN - US/REA
            if (valueString.Length > 38)
            {
                var parts = valueString.Split(" - ");
                // There are 4 parts if it's the correct line
                if (parts.Length == 4)
                {
                    // and we want the third
                    var dateTime = parts[2].Trim();

                    DateTime.TryParse(dateTime, out DateTime parsedDateTime);

                    _logger?.LogDebug("Found SimC collection date string ({parsedDateTime}) on line: {valueString}", parsedDateTime, valueString);

                    result.CollectionDate = parsedDateTime;
                }

            }
        }

        private void TryApplyLevel(SimcParsedProfile result, string valueString)
        {
            int.TryParse(valueString, out int level);

            _logger?.LogDebug("Setting level to ({level}) from: {valueString}", level, valueString);

            result.Level = level;
        }

        private void TryApplyConduitData(SimcParsedProfile profile, string valueString)
        {
            if (profile.Conduits.Count > 0)
            {
                _logger?.LogWarning("Overriding existing conduits. " +
                    "There should only be one conduits_available provided per profile.");
            }

            // Valid conduit string
            // conduits_available=116:1/78:1/82:1/84:1/101:1/69:1/73:1/67:1/66:1
            if (valueString.Contains(":"))
            {
                var results = new List<SimcParsedConduit>();

                var conduitParts = valueString.Split('/');

                foreach (var part in conduitParts)
                {
                    var kvp = part.Split(':');

                    if (kvp.Length != 2 ||
                        !int.TryParse(kvp[0], out int conduitId) ||
                        !int.TryParse(kvp[1], out int conduitRank))
                    {
                        _logger?.LogWarning("Invalid conduit found in part ({part}): {valueString}", part, valueString);
                        continue;
                    }

                    _logger?.LogDebug("Adding new conduit ({conduitId}) at rank: {conduitRank}", conduitId, conduitRank);

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
                _logger?.LogDebug("No valid conduits found in string: {valueString}", valueString);
            }
        }

        private void TryApplyTalents(SimcParsedProfile profile, string valueString)
        {
            if (valueString.Length > 0)
            {
                var talents = new List<SimcParsedTalent>();

                var talentParts = valueString.Split('/');

                foreach(var talentPart in talentParts)
                {
                    if(talentPart.Contains(':'))
                    {
                        var talentSplit = talentPart.Split(':');
                        talents.Add(new SimcParsedTalent()
                        {
                            TalentId = Convert.ToInt32(talentSplit[0]),
                            Rank = Convert.ToInt32(talentSplit[1])
                        });
                    }
                    else
                    {
                        _logger?.LogWarning("Unable to parse talent: {talentPart}", talentPart);
                    }
                }

                profile.Talents = talents;
            }
            else
                _logger?.LogDebug("No valid talents found in string: {valueString}", valueString);
        }

        private void TryApplySoulbind(SimcParsedProfile profile, SimcParsedLine line)
        {
            // Valid soublind string
            // # soulbind=niya,342270/82:1/73:1/320662/69:1/84:1/320668/322721
            if (line.Value.Contains(","))
            {
                var result = new SimcParsedSoulbind();

                // Get the soulbind name
                var soulbindName = line.Value.Split(',').FirstOrDefault();
                if (soulbindName.Length > 0)
                {
                    if (soulbindName.Contains(':'))
                    {
                        var soulbindNameParts = soulbindName.Split(':');
                        result.Name = soulbindNameParts[0];
                        result.SoulbindId = Convert.ToInt32(soulbindNameParts[1]);
                    }
                    else
                        result.Name = soulbindName;
                }
                else
                {
                    _logger?.LogWarning("Unable to parse soulbind name on line: {line.RawLine}", line.RawLine);
                }

                // Set if it's active
                result.IsActive = !(line.RawLine[0] == '#');

                // Now split all the pairs and grab the soulbind spells and socketed conduits
                var soulbindParts = line.Value.Split(',').LastOrDefault().Split('/');

                var soulbindSpells = new List<int>();
                var socketedConduits = new List<SimcParsedConduit>();

                foreach (var part in soulbindParts)
                {
                    if (part.Contains(':'))
                    {
                        // It's a socketed conduit 
                        var kvp = part.Split(':');

                        if (kvp.Length != 2 ||
                            !int.TryParse(kvp[0], out int conduitId) ||
                            !int.TryParse(kvp[1], out int conduitRank))
                        {
                            _logger?.LogWarning("Invalid socketed conduit found in part ({part}): {line.CleanLine}", part, line.CleanLine);
                            continue;
                        }

                        _logger?.LogDebug("Adding new socketed conduit ({conduitId}) at rank: {conduitRank}", conduitId, conduitRank);

                        var conduit = new SimcParsedConduit()
                        {
                            ConduitId = conduitId,
                            Rank = conduitRank
                        };

                        socketedConduits.Add(conduit);
                    }
                    else
                    {
                        // It's a soulbind spell
                        if (int.TryParse(part, out int soulbindSpellId))
                        {
                            _logger?.LogDebug("Adding soulbind ({soulbindSpellId}) from: {line.CleanLine}", soulbindSpellId, line.CleanLine);
                            soulbindSpells.Add(soulbindSpellId);
                        }
                        else
                        {
                            _logger?.LogWarning("Unable to parse soulbind spell or conduit from part ({part}) in: {line.CleanLine}", part, line.CleanLine);
                        }
                    }
                }

                // Add the soulbind spells and socketed conduits
                result.SocketedConduits = socketedConduits;
                result.SoulbindSpells = soulbindSpells;

                // Add the soulbind to the current ones.
                var soulbinds = new List<SimcParsedSoulbind>(profile.Soulbinds)
                {
                    result
                };
                profile.Soulbinds = soulbinds;
            }
            else
            {
                _logger?.LogDebug("No valid soulbinds found in string: {line.CleanLine}", line.CleanLine);
            }
        }

        private void TryApplyProfessions(SimcParsedProfile profile, SimcParsedLine line)
        {
            var professions = new List<SimcParsedProfession>();

            // professions=tailoring=1/jewelcrafting=1
            var professionParts = line.Value.Split('/');

            foreach (var part in professionParts)
            {
                if (!string.IsNullOrEmpty(part.Trim()))
                {
                    // tailoring=1
                    var kvp = part.Split('=');

                    var professionName = kvp[0];
                    if (!int.TryParse(kvp[1], out int professionLevel))
                    {
                        _logger?.LogWarning("Unable to get profession level ({part}) from string {line.RawLine}", part, line.RawLine);
                    }

                    professions.Add(new SimcParsedProfession()
                    {
                        Name = professionName,
                        Level = professionLevel
                    });
                }
            }

            profile.Professions = professions;
        }

        private void TryApplyItem(SimcParsedProfile profile, SimcParsedLine line)
        {
            // trinket2=,id=177157,bonus_id=6938/603,drop_level=50
            // main_hand=,id=178473,enchant_id=6229,bonus_id=6774/1507/6646
            // hands=,id=175862,enchant_id=6205,bonus_id=6781/1487/6646

            var itemResult = new SimcParsedItem();

            var itemParts = line.Value.Split(',');

            itemResult.Slot = line.CleanLine.Split('=').FirstOrDefault();
            itemResult.Equipped = !(line.RawLine[0] == '#');

            foreach (var part in itemParts)
            {
                if (string.IsNullOrEmpty(part.Trim()))
                    continue;

                // Split the part token down the middle by =
                var kvp = part.Split('=');

                if (kvp.Length != 2)
                    continue;

                switch (kvp[0])
                {
                    case "id":
                        if (!uint.TryParse(kvp[1], out uint itemId))
                        {
                            _logger?.LogWarning("Unable to get itemid ({part}) from string {line.RawLine}", part, line.RawLine);
                            continue;
                        }
                        itemResult.ItemId = itemId;
                        break;

                    case "enchant_id":
                        if (!int.TryParse(kvp[1], out int enchantId))
                        {
                            _logger?.LogWarning("Unable to get enchantid ({part}) from string {line.RawLine}", part, line.RawLine);
                            continue;
                        }
                        itemResult.EnchantId = enchantId;

                        break;

                    case "context":
                        if (!int.TryParse(kvp[1], out int context))
                        {
                            _logger?.LogWarning("Unable to get context ({part}) from string {line.RawLine}", part, line.RawLine);
                            continue;
                        }
                        itemResult.Context = context;

                        break;

                    case "drop_level":
                        if (!int.TryParse(kvp[1], out int dropLevel))
                        {
                            _logger?.LogWarning("Unable to get drop_level ({part}) from string {line.RawLine}", part, line.RawLine);
                            continue;
                        }
                        itemResult.DropLevel = dropLevel;

                        break;

                    case "bonus_id":
                        var bonusIds = kvp[1].Split('/');
                        var bonusIdResult = new List<int>();

                        foreach (var bonus in bonusIds)
                        {
                            if (string.IsNullOrEmpty(bonus.Trim()))
                                continue;

                            if (int.TryParse(bonus, out int bonusId))
                                bonusIdResult.Add(bonusId);
                        }

                        itemResult.BonusIds = bonusIdResult;

                        break;

                    case "gem_id":
                        var gemIds = kvp[1].Split('/');
                        var gemIdResult = new List<int>();

                        foreach (var gem in gemIds)
                        {
                            if (string.IsNullOrEmpty(gem.Trim()))
                                continue;

                            if (int.TryParse(gem, out int gemId))
                                gemIdResult.Add(gemId);
                        }

                        itemResult.GemIds = gemIdResult;

                        break;

                    case "ilevel":
                        if (!int.TryParse(kvp[1], out int itemLevel))
                        {
                            _logger?.LogWarning("Unable to get ilevel ({part}) from string {line.RawLine}", part, line.RawLine);
                            continue;
                        }
                        itemResult.ItemLevel = itemLevel;

                        break;

                    case "crafted_stats":
                        var statIds = kvp[1].Split('/');
                        var statIdResult = new List<int>();

                        foreach (var stat in statIds)
                        {
                            if (string.IsNullOrEmpty(stat.Trim()))
                                continue;

                            if (int.TryParse(stat, out int statId))
                                statIdResult.Add(statId);
                        }

                        itemResult.CraftedStatIds = statIdResult;

                        break;

                    default:
                        break;
                }
            }

            // Add the item to the list.
            var items = new List<SimcParsedItem>(profile.Items)
            {
                itemResult
            };
            profile.Items = items;
        }
    }
}
