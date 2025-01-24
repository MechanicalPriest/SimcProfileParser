﻿using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.DataSync;
using SimcProfileParser.Model.RawData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimcProfileParser.DataSync
{
    /// <summary>
    /// The purpose of thie class is to process a raw file into 
    /// </summary>
    internal class RawDataExtractionService : IRawDataExtractionService
    {
        private readonly ILogger<RawDataExtractionService> _logger;

        public RawDataExtractionService(ILogger<RawDataExtractionService> logger)
        {
            _logger = logger;
        }

        public object GenerateData(SimcParsedFileType fileType, Dictionary<string, string> incomingRawData)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            object result = fileType switch
            {
                SimcParsedFileType.CombatRatingMultipliers => GenerateCombatRatingMultipliers(incomingRawData),
                SimcParsedFileType.StaminaMultipliers => GenerateStaminaMultipliers(incomingRawData),
                SimcParsedFileType.ItemDataNew => GenerateItemData(incomingRawData, 157759),
                SimcParsedFileType.ItemDataOld => GenerateItemData(incomingRawData, 0, 157760),
                SimcParsedFileType.ItemBonusData => GenerateItemBonusData(incomingRawData),
                SimcParsedFileType.RandomPropPoints => GenerateRandomPropData(incomingRawData),
                SimcParsedFileType.SpellData => GenerateSpellData(incomingRawData),
                SimcParsedFileType.GemData => GenerateGemData(incomingRawData),
                SimcParsedFileType.ItemEnchantData => GenerateItemEnchantData(incomingRawData),
                SimcParsedFileType.SpellScaleMultipliers => GenerateSpellScalingMultipliers(incomingRawData),
                SimcParsedFileType.CurvePoints => GenerateCurveData(incomingRawData),
                SimcParsedFileType.RppmData => GenerateRppmData(incomingRawData),
                SimcParsedFileType.CovenantData => GenerateConduitRankData(incomingRawData),
                SimcParsedFileType.ItemEffectData => GenerateItemEffectData(incomingRawData),
                SimcParsedFileType.GameDataVersion => GenerateGameDataVersion(incomingRawData),
                SimcParsedFileType.TraitData => GenerateTraitData(incomingRawData),
                _ => throw new ArgumentOutOfRangeException($"FileType {fileType} is invalid."),
            };
            sw.Stop();
            _logger?.LogTrace($"Time taken to process {fileType}: {sw.ElapsedMilliseconds}ms");

            return result;
        }

        internal float[][] GenerateCombatRatingMultipliers(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "ScaleData.raw").FirstOrDefault().Value;

            Regex regexCR = new Regex(@"__combat_ratings_mult_by_ilvl.+?\{.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}).+?\};", RegexOptions.Singleline);

            Match matches = regexCR.Match(rawData);

            GroupCollection groups = matches.Groups;

            float[][] values = new float[4][];
            values[0] = ParseRatingGroup(groups[1]);
            values[1] = ParseRatingGroup(groups[2]);
            values[2] = ParseRatingGroup(groups[3]);
            values[3] = ParseRatingGroup(groups[4]);

            return values;
        }

        internal float[][] GenerateStaminaMultipliers(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "ScaleData.raw").FirstOrDefault().Value;

            Regex regexCR = new Regex(@"__stamina_mult_by_ilvl.+?\{.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}),.+?(\{.+?\}).+?\};", RegexOptions.Singleline);

            Match matches = regexCR.Match(rawData);

            GroupCollection groups = matches.Groups;

            float[][] values = new float[4][];
            values[0] = ParseRatingGroup(groups[1]);
            values[1] = ParseRatingGroup(groups[2]);
            values[2] = ParseRatingGroup(groups[3]);
            values[3] = ParseRatingGroup(groups[4]);

            return values;
        }

        private float[] ParseRatingGroup(Group g)
        {
            Regex items = new Regex(@"\s+([\d]\.?\d*),");

            float[] values = new float[1300];

            int i = 0;
            foreach (Match m in items.Matches(g.Value))
            {
                values[i++] = float.Parse(m.Groups[1].Value);
            }

            return values;
        }

        internal List<SimcRawItem> GenerateItemData(Dictionary<string, string> incomingRawData,
            int lowerBoundItemId = 0, int upperBoundItemId = int.MaxValue)
        {
            var rawData = incomingRawData.Where(d => d.Key == "ItemData.raw").FirstOrDefault().Value;

            var itemEffects = GenerateItemEffectData(incomingRawData);

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

                    var itemMod = new SimcRawItemMod
                    {
                        // 0 is the typeId
                        ModType = (ItemModType)Convert.ToInt32(data[0]),

                        // 1 is the stat allocation
                        StatAllocation = Convert.ToInt32(data[1]),

                        // 2 is the socket penalty
                        SocketMultiplier = ToDoubleClean(data[2])
                    };

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

                    if (item.Id < lowerBoundItemId || item.Id > upperBoundItemId)
                        continue;

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
                    item.Quality = (ItemQuality)Convert.ToInt32(data[9]);

                    // 10 is inventory type
                    // TODO: parse this to the enum value ?
                    item.InventoryType = (InventoryType)Convert.ToInt32(data[10]);

                    // 11 is item class
                    item.ItemClass = (ItemClass)Convert.ToInt32(data[11]);

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

                    // Add the items effects
                    var specificEffects = itemEffects.Where(i => i.ItemId == item.Id)?.ToList();

                    if (specificEffects != null && specificEffects.Count > 0)
                        item.ItemEffects.AddRange(specificEffects);

                    items.Add(item);
                }
            }

            return items;
        }

        internal List<SimcRawRandomPropData> GenerateRandomPropData(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "RandomPropPoints.raw").FirstOrDefault().Value;

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var randomProps = new List<SimcRawRandomPropData>();

            foreach (var line in lines)
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

                var newProp = new SimcRawRandomPropData
                {
                    // 0 is the ilvl
                    ItemLevel = Convert.ToUInt32(props[0]),

                    // 1 is the damage replace stat
                    DamageReplaceStat = ToDoubleClean(props[1]),

                    // 2 is damage secondary
                    DamageSecondary = ToDoubleClean(props[2]),

                    // 3, 4, 5, 6, 7 are the Epic property data
                    // 8, 9, 10, 11, 12 are the rare prop data
                    // 13, 14, 15, 16, 17 are the uncommon prop data
                    Epic = new float[5],
                    Rare = new float[5],
                    Uncommon = new float[5]
                };

                for (var i = 0; i < 5; i++)
                {
                    newProp.Epic[i] = ToFloatClean(props[i + 3]);
                    newProp.Rare[i] = ToFloatClean(props[i + 8]);
                    newProp.Uncommon[i] = ToFloatClean(props[i + 13]);
                }

                randomProps.Add(newProp);
            }

            return randomProps;
        }
        internal List<SimcRawSpell> GenerateSpellData(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "SpellData.raw").FirstOrDefault().Value;

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var spells = new List<SimcRawSpell>();
            var effects = new List<SimcRawSpellEffect>();
            var spellPowers = new List<SimcRawSpellPower>();

            foreach (var line in lines)
            {
                // First try and do an effect - they have 33 total fields.
                // spelleffect_data_t
                if (line.Split(',').Count() == 35)
                {
                    var effect = new SimcRawSpellEffect();

                    // Split the data up
                    var data = line.Split(',');
                    // Clean it up
                    for (var i = 0; i < data.Length; i++)
                    {
                        data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                    }

                    // 0 is Id
                    effect.Id = Convert.ToUInt32(data[0]);

                    // 1 is spellid
                    effect.SpellId = Convert.ToUInt32(data[1]);

                    // 2 is effect index
                    effect.EffectIndex = Convert.ToUInt32(data[2]);

                    // 3 is effect type
                    effect.EffectType = Convert.ToUInt32(data[3]);

                    // 4 is effect subtype
                    effect.EffectSubType = Convert.ToUInt32(data[4]);

                    // 5 is effect scaling type
                    effect.ScalingType = Convert.ToInt32(data[5]);

                    // 6 is effect attribute

                    // 7 is (average) spell scaling coefficient 
                    effect.Coefficient = ToDoubleClean(data[7]);

                    // 8 is (delta) spell scaling coefficient 
                    effect.Delta = ToDoubleClean(data[8]);

                    // 9 is an unused unknown effect multiplier

                    // 10 is sp coeff
                    effect.SpCoefficient = ToDoubleClean(data[10]);

                    // 11 is ap coeff
                    effect.ApCoefficient = ToDoubleClean(data[11]);

                    // 12 is Amplitude (tick time)
                    effect.Amplitude = ToDoubleClean(data[12]);

                    // 13 is Radius
                    effect.Radius = ToDoubleClean(data[13]);

                    // 14 is RadiusMax
                    effect.RadiusMax = ToDoubleClean(data[14]);

                    // 15 is effect base value
                    effect.BaseValue = ToDoubleClean(data[15]);

                    // 16 is Misc Value 1?
                    effect.MiscValue1 = Convert.ToInt32(data[16]);

                    // 17 is Misc Value 2?
                    effect.MiscValue2 = Convert.ToInt32(data[17]);

                    // 18, 19, 20, 21 are class flags.
                    effect.ClassFlags = new uint[4];
                    effect.ClassFlags[0] = Convert.ToUInt32(data[18]);
                    effect.ClassFlags[1] = Convert.ToUInt32(data[19]);
                    effect.ClassFlags[2] = Convert.ToUInt32(data[20]);
                    effect.ClassFlags[3] = Convert.ToUInt32(data[21]);

                    // 22 is trigger spell id
                    effect.TriggerSpellId = Convert.ToUInt32(data[22]);

                    // 23 is chain multi
                    effect.ChainMultiplier = ToDoubleClean(data[23]);

                    // 24 is effect points per combo point
                    effect.ComboPoints = ToDoubleClean(data[24]);

                    // 25 is real points per level
                    effect.RealPpl = ToDoubleClean(data[25]);

                    // 26 is mechanic
                    effect.Mechanic = Convert.ToUInt32(data[26]);

                    // 27 is number of chain targets
                    effect.ChainTargets = Convert.ToInt32(data[27]);

                    // 28 is targeting 1
                    effect.Targeting1 = Convert.ToUInt32(data[28]);

                    // 29 is targeting 2
                    effect.Targeting2 = Convert.ToUInt32(data[29]);

                    // 30 is value
                    effect.Value = ToDoubleClean(data[30]);

                    // 31 is pvp coefficient 
                    effect.PvpCoeff = ToDoubleClean(data[31]);

                    effects.Add(effect);
                }
                else if (line.Split(',').Count() == 11)
                {
                    // If it has 10 it's very likely a spellpower_data_t entry
                    var spellPower = new SimcRawSpellPower();

                    // Split the data up
                    var data = line.Split(',');
                    // Clean it up
                    for (var i = 0; i < data.Length; i++)
                    {
                        data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                    }

                    if (data[0].Contains("&__spell_data"))
                        continue;

                    // 0 is Id
                    spellPower.Id = Convert.ToUInt32(data[0]);

                    // 1 is Spell Id
                    spellPower.SpellId = Convert.ToUInt32(data[1]);

                    // 2 is AuraId
                    spellPower.AuraId = Convert.ToUInt32(data[2]);

                    // 3 is power type
                    spellPower.PowerType = Convert.ToInt32(data[3]);

                    // 4 is cost
                    spellPower.Cost = Convert.ToInt32(data[4]);

                    // 5 is max cost
                    spellPower.CostMax = Convert.ToInt32(data[5]);

                    // 6 is cost per tick
                    spellPower.CostPerTick = Convert.ToInt32(data[6]);

                    // 7 is percent cost
                    spellPower.PercentCost = ToDoubleClean(data[7]);

                    // 8 is percent cost max
                    spellPower.PercentCostMax = ToDoubleClean(data[8]);

                    // 9 is percent cost per tick
                    spellPower.PercentCostPerTick = ToDoubleClean(data[9]);

                    spellPowers.Add(spellPower);
                }
                else
                {
                    // spell_data.hpp - spell_data_t

                    if (!line.Contains('"') || line.Split(',').Length < 20)
                        continue;

                    var nameSegment = line.Substring(0, line.LastIndexOf("\""));
                    var dataSegment = line.Substring(line.LastIndexOf("\"") + 1);

                    var spell = new SimcRawSpell();

                    // Split the remaining data up
                    var data = dataSegment.Split(',');
                    // Clean it up
                    for (var i = 0; i < data.Length; i++)
                    {
                        data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                    }

                    spell.Name = nameSegment.Substring(nameSegment.IndexOf('\"') + 1);

                    // 0 is blank, 1 is spellId
                    spell.Id = Convert.ToUInt32(data[1]);

                    // 2 is spell school
                    spell.School = Convert.ToUInt32(data[2]);

                    // 3 is projectile speed
                    spell.ProjectileSpeed = ToDoubleClean(data[3]);

                    // 4 is projectile delay
                    spell.ProjectileDelay = ToDoubleClean(data[4]);

                    // 5 is minimum travel time
                    spell.MinimumTravelTime = ToDoubleClean(data[5]);

                    // 6 is a hex race mask
                    ulong.TryParse(data[6].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out ulong raceMask);
                    spell.RaceMask = raceMask;

                    // 7 is a hex class mask
                    uint.TryParse(data[7].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out uint classMask);
                    spell.ClassMask = classMask;

                    // 8 is max scaling level
                    spell.MaxScalingLevel = Convert.ToInt32(data[8]);

                    // 9 is the min scaling level
                    spell.MinScalingLevel = Convert.ToInt32(data[9]);

                    // 10 forces ilevel scaling at this ilevel for this item, overriding spell attributes
                    spell.ScaleFromILevel = Convert.ToInt32(data[10]);

                    // 11 is spell level
                    spell.SpellLevel = Convert.ToUInt32(data[11]);

                    // 12 is max level
                    spell.MaxLevel = Convert.ToUInt32(data[12]);

                    // 13 is required max level
                    spell.RequireMaxLevel = Convert.ToUInt32(data[13]);

                    // 14 is minimum range
                    spell.MinRange = ToDoubleClean(data[14]);

                    // 15 is maximum range
                    spell.MaxRange = ToDoubleClean(data[15]);

                    // 16 is cooldown
                    spell.Cooldown = Convert.ToUInt32(data[16]);

                    // 17 is gcd
                    spell.Gcd = Convert.ToUInt32(data[17]);

                    // 18 is category cd
                    spell.CategoryCooldown = Convert.ToUInt32(data[18]);

                    // 19 is category flags
                    spell.CategoryFlags = Convert.ToUInt32(data[19]);

                    // 20 is charges
                    spell.Charges = Convert.ToUInt32(data[20]);

                    // 21 is charges cd
                    spell.ChargeCooldown = Convert.ToUInt32(data[21]);

                    // 22 is category type mask
                    spell.CategoryTypeMask = Convert.ToUInt32(data[22]);

                    // 23 is category
                    spell.Category = Convert.ToUInt32(data[23]);

                    // 24 is dmg class
                    spell.DamageClass = Convert.ToUInt32(data[24]);

                    // 25 is max targets
                    spell.MaxTargets = Convert.ToInt32(data[25]);

                    // 26 is Duration
                    spell.Duration = ToDoubleClean(data[26]);

                    // 27 is max stacks
                    spell.MaxStack = Convert.ToUInt32(data[27]);

                    // 28 is proc chance
                    spell.ProcChance = Convert.ToUInt32(data[28]);

                    // 29 is proc charges
                    spell.ProcCharges = Convert.ToInt32(data[29]);

                    // 30 is proc flags
                    ulong.TryParse(data[30].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out ulong procFlags);
                    spell.ProcFlags = procFlags;

                    // 31 is icd
                    spell.InternalCooldown = Convert.ToUInt32(data[31]);

                    // 32 is rppm
                    spell.Rppm = ToDoubleClean(data[32]);

                    // 33 is eq class
                    spell.EquippedClass = Convert.ToUInt32(data[33]);

                    // 34 is eq class inventory type mask
                    _ = uint.TryParse(data[34].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out uint eqEquipmentInventoryType);
                    spell.EquippedInventoryTypeMask = eqEquipmentInventoryType;

                    // 35 is eq class subclass mask
                    _ = uint.TryParse(data[35].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out uint eqEquippedSubclass);
                    spell.EquippedSubclassMask = eqEquippedSubclass;

                    // 36 is cast time
                    spell.CastTime = Convert.ToInt32(data[36]);

                    // 37 - 51. Next up is something of length NUM_SPELL_FLAGS = 15
                    spell.Attributes = new uint[15];
                    for (var i = 0; i < spell.Attributes.Length; i++)
                    {
                        spell.Attributes[i] = Convert.ToUInt32(data[i + 37]);
                    }

                    // 52 - 55. Next up is something of length NUM_CLASS_FAMILY_FLAGS = 4
                    spell.ClassFlags = new uint[4];
                    for (var i = 0; i < spell.ClassFlags.Length; i++)
                    {
                        spell.ClassFlags[i] = Convert.ToUInt32(data[i + 52]);
                    }

                    // 56 is class flags family
                    spell.ClassFlagsFamily = Convert.ToUInt32(data[56]);

                    // 57-58 is aura interrupt flags, skip these

                    // 59-60 is channel interrupt flags, skip these

                    // 61 is stance mask
                    uint.TryParse(data[61].Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber, null, out uint stanceMask);
                    spell.StanceMask = stanceMask;

                    // 62 is mechanic
                    spell.Mechanic = Convert.ToUInt32(data[62]);

                    // 63 is az power id
                    spell.PowerId = Convert.ToUInt32(data[63]);

                    // 64 is essence id
                    spell.EssenceId = Convert.ToUInt32(data[64]);

                    // We don't have a practical use for the counts metadata
                    // 65 is effects count

                    // 66 is power count

                    // 67 is driver count

                    // 68 is label count

                    spells.Add(spell);
                }
            }


            foreach (var spell in spells)
            {
                // Add the spell effects to spells
                foreach (var effect in effects.Where(e => e.SpellId == spell.Id))
                {
                    spell.Effects.Add(effect);
                }
                // Add any spell power data
                foreach (var spellPower in spellPowers.Where(s => s.SpellId == spell.Id))
                {
                    spell.SpellPowers.Add(spellPower);
                }
            }



            return spells;
        }

        internal List<SimcRawItemBonus> GenerateItemBonusData(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "ItemBonusData.raw").FirstOrDefault().Value;

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var itemBonuses = new List<SimcRawItemBonus>();

            foreach (var line in lines)
            {
                // Split the data up
                var data = line.Split(',');

                // Only process valid lines
                if (data.Count() < 9)
                    continue;

                var itemBonus = new SimcRawItemBonus();

                // Clean the data up
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                }

                // 0 is Id
                itemBonus.Id = Convert.ToUInt32(data[0]);

                // 1 is bonus id
                itemBonus.BonusId = Convert.ToUInt32(data[1]);

                // 2 is type
                itemBonus.Type = (ItemBonusType)Convert.ToUInt32(data[2]);

                // 3 is value 1
                itemBonus.Value1 = Convert.ToInt32(data[3]);

                // 4 is value 2
                itemBonus.Value2 = Convert.ToInt32(data[4]);

                // 5 is value 3
                itemBonus.Value3 = Convert.ToInt32(data[5]);

                // 6 is value 4
                itemBonus.Value4 = Convert.ToInt32(data[6]);

                // 7 is index
                itemBonus.Index = Convert.ToUInt32(data[7]);

                itemBonuses.Add(itemBonus);
            }

            return itemBonuses;
        }

        internal List<SimcRawItemEffect> GenerateItemEffectData(Dictionary<string, string> incomingRawData)
        {
            var rawEffectData = incomingRawData.Where(d => d.Key == "ItemEffectData.raw").FirstOrDefault().Value;

            var lines = rawEffectData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var itemEffects = new List<SimcRawItemEffect>();

            foreach (var line in lines)
            {
                // Split the data up
                var data = line.Split(',');

                // Only process valid lines
                if (data.Count() < 9)
                    continue;

                var itemEffect = new SimcRawItemEffect();

                // Clean the data up
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                }

                // 0 is Id
                itemEffect.Id = Convert.ToUInt32(data[0]);

                // 1 is spell id
                itemEffect.SpellId = Convert.ToUInt32(data[1]);

                // 2 is item id
                itemEffect.ItemId = Convert.ToUInt32(data[2]);

                // 3 is index
                itemEffect.Index = Convert.ToInt32(data[3]);

                // 4 is type
                itemEffect.Type = Convert.ToInt32(data[4]);

                // 5 is cooldown group
                itemEffect.CooldownGroup = Convert.ToInt32(data[5]);

                // 6 is cd duration
                itemEffect.CooldownDuration = Convert.ToInt32(data[6]);

                // 7 is cd group duration
                itemEffect.CooldownGroupDuration = Convert.ToInt32(data[7]);

                itemEffects.Add(itemEffect);
            }

            return itemEffects;
        }

        internal List<SimcRawGemProperty> GenerateGemData(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "GemData.raw").FirstOrDefault().Value;

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var gems = new List<SimcRawGemProperty>();

            foreach (var line in lines)
            {
                // Split the data up
                var data = line.Split(',');

                // Only process valid lines
                if (data.Count() < 4)
                    continue;

                var gem = new SimcRawGemProperty();

                // Clean the data up
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                }

                // 0 is Id
                gem.Id = Convert.ToUInt32(data[0]);

                // 1 is enchant id
                gem.EnchantId = Convert.ToUInt32(data[1]);

                // 2 is color
                gem.Colour = uint.Parse(data[2].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

                gems.Add(gem);
            }

            return gems;
        }
        internal List<SimcRawItemEnchantment> GenerateItemEnchantData(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "ItemEnchantData.raw").FirstOrDefault().Value;

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var enchants = new List<SimcRawItemEnchantment>();

            foreach (var line in lines)
            {
                // Split the data up
                var data = line.Split(',');

                // Only process valid lines
                if (data.Count() < 20)
                    continue;

                var enchant = new SimcRawItemEnchantment();

                // Clean the data up
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                }

                // 0 is Id
                enchant.Id = Convert.ToUInt32(data[0]);

                // 1 is gem id
                enchant.GemId = Convert.ToUInt32(data[1]);

                // 2 is color
                enchant.ScalingId = Convert.ToInt32(data[2]);

                // 3 is min scaling lvl
                enchant.MinScalingLevel = Convert.ToUInt32(data[3]);

                // 4 is max scaling lvl
                enchant.MaxScalingLevel = Convert.ToUInt32(data[4]);

                // 5 is min item lvl
                enchant.MinItemLevel = Convert.ToUInt32(data[5]);

                // 6 is max item lvl
                enchant.MaxItemLevel = Convert.ToUInt32(data[6]);

                // 7 is max scaling lvl
                enchant.RequiredSkill = Convert.ToUInt32(data[7]);

                // 8 is max scaling lvl
                enchant.RequiredSkillLevel = Convert.ToUInt32(data[8]);

                // 7 8 9 are enchant types (item_enchant)
                // 10 11 12 are enchant amounts
                // 13 14 15 are props (item_mod_type)
                // 16 17 18 are saling coefficients
                for (var i = 0; i < 3; i++)
                {
                    var subEffect = new SimcRawItemSubEnchantment
                    {
                        Type = Convert.ToUInt32(data[9 + i]),
                        Amount = Convert.ToInt32(data[12 + i]),
                        Property = Convert.ToUInt32(data[15 + i]),
                        Coefficient = ToDoubleClean(data[18 + i])
                    };
                    enchant.SubEnchantments.Add(subEffect);
                }

                // 19 is spellid
                enchant.SpellId = Convert.ToUInt32(data[21]);

                // 20 is the name
                enchant.Name = data[22].Trim('"').Trim();

                enchants.Add(enchant);
            }

            return enchants;
        }

        /// <summary>
        /// Generate spell scaling multipliers, thanks to Phate408
        /// </summary>
        /// <returns></returns>
        internal double[][] GenerateSpellScalingMultipliers(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "ScaleData.raw").FirstOrDefault().Value;

            var numSpellScalingTables = 21;

            double[][] spellScalingTable = new double[numSpellScalingTables][];
            for (int i = 0; i < numSpellScalingTables; i++)
            {
                spellScalingTable[i] = new double[80];
            }

            string key = "__spell_scaling[][80] = {";

            int start = rawData.IndexOf(key) + key.Length;
            int end = rawData.IndexOf("};", start);

            string firstArray = rawData[start..end];

            Regex innerArrayRX = new Regex(@"\{.+?\},", RegexOptions.Singleline);
            Regex valuesRX = new Regex(@"(\d+(?:\.\d+)?),");

            int j = 0;

            foreach (Match m in innerArrayRX.Matches(firstArray))
            {
                int k = 0;
                foreach (Match m2 in valuesRX.Matches(m.Value))
                {
                    double f = double.Parse(m2.Groups[1].Value);
                    spellScalingTable[j][k++] = f;
                }
                j++;
            }


            return spellScalingTable;
        }
        internal List<SimcRawCurvePoint> GenerateCurveData(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "CurveData.raw").FirstOrDefault().Value;

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var curvePoints = new List<SimcRawCurvePoint>();

            foreach (var line in lines)
            {
                // Split the data up
                var data = line.Split(',');

                // Only process valid lines
                if (data.Count() != 7)
                    continue;

                var curvePoint = new SimcRawCurvePoint();

                // Clean the data up
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                }

                // 0 is curve Id
                curvePoint.CurveId = Convert.ToUInt32(data[0]);

                // 1 is Index
                curvePoint.Index = Convert.ToUInt32(data[1]);

                // 2 is Primary1
                curvePoint.Primary1 = ToFloatClean(data[2]);

                // 3 is Primary2
                curvePoint.Primary2 = ToFloatClean(data[3]);

                // 4 is Secondary1
                curvePoint.Secondary1 = ToFloatClean(data[4]);

                // 5 is value Secondary2
                curvePoint.Secondary2 = ToFloatClean(data[5]);

                curvePoints.Add(curvePoint);
            }

            return curvePoints;
        }
        internal List<SimcRawRppmEntry> GenerateRppmData(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "RppmData.raw").FirstOrDefault().Value;

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var rppmData = new List<SimcRawRppmEntry>();

            foreach (var line in lines)
            {
                // Split the data up
                var data = line.Split(',');

                // Only process valid lines
                if (data.Count() != 5)
                    continue;

                var rppmEntry = new SimcRawRppmEntry();

                // Clean the data up
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                }

                // 0 is curve Id
                rppmEntry.SpellId = Convert.ToUInt32(data[0]);

                // 1 is Type
                rppmEntry.Type = Convert.ToUInt32(data[1]);

                // 2 is ModifierType
                rppmEntry.ModifierType = (RppmModifierType)Convert.ToUInt32(data[2]);

                // 3 is Coefficient
                rppmEntry.Coefficient = ToDoubleClean(data[3]);

                rppmData.Add(rppmEntry);
            }

            return rppmData;
        }

        internal List<SimcRawSpellConduitRankEntry> GenerateConduitRankData(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "CovenantData.raw").FirstOrDefault().Value;

            // Split the raw data to only be the parts we want.
            string key = "__conduit_rank_data {";

            int start = rawData.IndexOf(key) + key.Length;
            int end = rawData.IndexOf("};", start);

            var dataChunk = rawData[start..end];

            var lines = dataChunk.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var conduitRankEntries = new List<SimcRawSpellConduitRankEntry>();

            foreach (var line in lines)
            {
                // Split the data up
                var data = line.Split(',');

                // Only process valid lines
                if (data.Count() != 5)
                    continue;

                var conduitRank = new SimcRawSpellConduitRankEntry();

                // Clean the data up
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                }

                // 0 is curve Id
                conduitRank.ConduitId = Convert.ToUInt32(data[0]);

                // 1 is rank
                conduitRank.Rank = Convert.ToUInt32(data[1]);

                // 2 is spell id
                conduitRank.SpellId = Convert.ToUInt32(data[2]);

                // 3 is spell id
                conduitRank.Value = ToDoubleClean(data[3]);

                conduitRankEntries.Add(conduitRank);
            }

            return conduitRankEntries;
        }

        internal string GenerateGameDataVersion(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "GameDataVersion.raw").FirstOrDefault().Value;

            var lines = rawData.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var gameDataVersion = "";

            foreach (var line in lines)
            {
                var key = "#define CLIENT_DATA_WOW_VERSION ";
                if (line.StartsWith(key))
                {
                    gameDataVersion = line.Substring(key.Length).Trim('"');
                }
            }

            return gameDataVersion;
        }

        internal List<SimcRawTrait> GenerateTraitData(Dictionary<string, string> incomingRawData)
        {
            var rawData = incomingRawData.Where(d => d.Key == "TraitData.raw").FirstOrDefault().Value;

            // Split the raw data to only be the parts we want.
            string key = "__trait_data_data { {";

            int start = rawData.IndexOf(key) + key.Length;
            int end = rawData.IndexOf("} };", start);

            var dataChunk = rawData[start..end];

            var lines = dataChunk.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var traitEntries = new List<SimcRawTrait>();

            foreach (var line in lines)
            {
                // Split the data up
                var data = line.Split(',');

                // Only process valid lines
                if (data.Count() != 22)
                    continue;

                var nameSegment = line.Substring(0, line.LastIndexOf("\""));

                var trait = new SimcRawTrait();

                // Clean the data up
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace("}", "").Replace("{", "").Trim();
                }

                // 0 is tree index
                trait.TreeIndex = Convert.ToUInt32(data[0]);

                // 1 is class id
                trait.ClassId = Convert.ToUInt32(data[1]);

                // 2 is trait node entry id
                trait.TraitNodeEntryId = Convert.ToUInt32(data[2]);

                // 3 is node id
                trait.NodeId = Convert.ToUInt32(data[3]);

                // 4 is max ranks
                trait.MaxRanks = Convert.ToUInt32(data[4]);

                // 5 is required points
                trait.RequiredPoints = Convert.ToUInt32(data[5]);

                // 6 is trait definition id
                trait.TraitDefinitionId = Convert.ToUInt32(data[6]);

                // 7 is spell id
                trait.SpellId = Convert.ToUInt32(data[7]);

                // 8 is override spell id
                trait.SpellOverrideId = Convert.ToUInt32(data[8]);

                // 9 is row
                trait.Row = Convert.ToInt32(data[9]);

                // 10 is column
                trait.Column = Convert.ToInt32(data[10]);

                // 11 is selection index
                trait.SelectionIndex = Convert.ToInt32(data[11]);

                // 12 is name
                trait.Name = nameSegment.Substring(nameSegment.IndexOf('\"') + 1);

                // 13 14 15 16 are all spec IDs
                trait.SpecId = new uint[4]
                {
                Convert.ToUInt32(data[13]),
                Convert.ToUInt32(data[14]),
                Convert.ToUInt32(data[15]),
                Convert.ToUInt32(data[16])
                };

                // 17 18 19 20 are all spec starter IDs
                trait.SpecStarterId = new uint[4]
                {
                Convert.ToUInt32(data[17]),
                Convert.ToUInt32(data[18]),
                Convert.ToUInt32(data[19]),
                Convert.ToUInt32(data[20])
                };

                traitEntries.Add(trait);
            }

            return traitEntries;
        }

        private double ToDoubleClean(string data)
        {
            // We need to first remove the "f" from the end of all the doubles
            data = data.Replace("f", "");

            return Convert.ToDouble(data);
        }

        private float ToFloatClean(string data)
        {
            // We sometimes need to remove the "f" from the end
            data = data.Replace("f", "");

            float.TryParse(data, out float outFloat);

            return outFloat;
        }
    }
}
