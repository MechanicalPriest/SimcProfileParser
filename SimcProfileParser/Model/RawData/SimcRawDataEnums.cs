﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.RawData
{
    // from https://github.com/simulationcraft/simc/blob/shadowlands/engine/sc_enums.hpp#L926
    /// <summary>
    /// (stat_e) A list of all stats and their internal Simc reference numbers
    /// </summary>
    enum SimcStat
    {
        STAT_NONE = 0,
        STAT_STRENGTH,
        STAT_AGILITY,
        STAT_STAMINA,
        STAT_INTELLECT,
        STAT_SPIRIT,
        STAT_AGI_INT,
        STAT_STR_AGI,
        STAT_STR_INT,
        STAT_STR_AGI_INT,
        STAT_HEALTH,
        STAT_MANA,
        STAT_RAGE,
        STAT_ENERGY,
        STAT_FOCUS,
        STAT_RUNIC,
        STAT_MAX_HEALTH,
        STAT_MAX_MANA,
        STAT_MAX_RAGE,
        STAT_MAX_ENERGY,
        STAT_MAX_FOCUS,
        STAT_MAX_RUNIC,
        STAT_SPELL_POWER,
        STAT_ATTACK_POWER,
        STAT_EXPERTISE_RATING,
        STAT_EXPERTISE_RATING2,
        STAT_HIT_RATING,
        STAT_HIT_RATING2,
        STAT_CRIT_RATING,
        STAT_HASTE_RATING,
        STAT_MASTERY_RATING,
        STAT_VERSATILITY_RATING,
        STAT_LEECH_RATING,
        STAT_SPEED_RATING,
        STAT_AVOIDANCE_RATING,
        STAT_CORRUPTION,
        STAT_CORRUPTION_RESISTANCE,
        STAT_ARMOR,
        STAT_BONUS_ARMOR,
        STAT_RESILIENCE_RATING,
        STAT_DODGE_RATING,
        STAT_PARRY_RATING,
        STAT_BLOCK_RATING,
        STAT_PVP_POWER,
        STAT_WEAPON_DPS,
        STAT_WEAPON_OFFHAND_DPS,
        STAT_ALL,
        STAT_MAX
    };

    // from https://github.com/simulationcraft/simc/blob/shadowlands/engine/dbc/data_enums.hh#L456
    enum ItemModType
    {
        ITEM_MOD_NONE = -1,
        ITEM_MOD_MANA = 0,
        ITEM_MOD_HEALTH = 1,
        ITEM_MOD_AGILITY = 3,
        ITEM_MOD_STRENGTH = 4,
        ITEM_MOD_INTELLECT = 5,
        ITEM_MOD_SPIRIT = 6,
        ITEM_MOD_STAMINA = 7,
        ITEM_MOD_DEFENSE_SKILL_RATING = 12,
        ITEM_MOD_DODGE_RATING = 13,
        ITEM_MOD_PARRY_RATING = 14,
        ITEM_MOD_BLOCK_RATING = 15,
        ITEM_MOD_HIT_MELEE_RATING = 16,
        ITEM_MOD_HIT_RANGED_RATING = 17,
        ITEM_MOD_HIT_SPELL_RATING = 18,
        ITEM_MOD_CRIT_MELEE_RATING = 19,
        ITEM_MOD_CRIT_RANGED_RATING = 20,
        ITEM_MOD_CRIT_SPELL_RATING = 21,
        ITEM_MOD_CORRUPTION = 22,
        ITEM_MOD_CORRUPTION_RESISTANCE = 23, // TODO: Guess
        ITEM_MOD_BONUS_STAT_1 = 24,
        ITEM_MOD_BONUS_STAT_2 = 25,
        ITEM_MOD_CRIT_TAKEN_RANGED_RATING = 26,
        ITEM_MOD_CRIT_TAKEN_SPELL_RATING = 27,
        ITEM_MOD_HASTE_MELEE_RATING = 28,
        ITEM_MOD_HASTE_RANGED_RATING = 29,
        ITEM_MOD_HASTE_SPELL_RATING = 30,
        ITEM_MOD_HIT_RATING = 31,
        ITEM_MOD_CRIT_RATING = 32,
        ITEM_MOD_HIT_TAKEN_RATING = 33,
        ITEM_MOD_CRIT_TAKEN_RATING = 34,
        ITEM_MOD_RESILIENCE_RATING = 35,
        ITEM_MOD_HASTE_RATING = 36,
        ITEM_MOD_EXPERTISE_RATING = 37,
        ITEM_MOD_ATTACK_POWER = 38,
        ITEM_MOD_RANGED_ATTACK_POWER = 39,
        ITEM_MOD_VERSATILITY_RATING = 40,
        ITEM_MOD_SPELL_HEALING_DONE = 41,                 // deprecated
        ITEM_MOD_SPELL_DAMAGE_DONE = 42,                 // deprecated
        ITEM_MOD_MANA_REGENERATION = 43,
        ITEM_MOD_ARMOR_PENETRATION_RATING = 44,
        ITEM_MOD_SPELL_POWER = 45,
        ITEM_MOD_HEALTH_REGEN = 46,
        ITEM_MOD_SPELL_PENETRATION = 47,
        ITEM_MOD_BLOCK_VALUE = 48,
        ITEM_MOD_MASTERY_RATING = 49,
        ITEM_MOD_EXTRA_ARMOR = 50,
        ITEM_MOD_FIRE_RESISTANCE = 51,
        ITEM_MOD_FROST_RESISTANCE = 52,
        ITEM_MOD_HOLY_RESISTANCE = 53,
        ITEM_MOD_SHADOW_RESISTANCE = 54,
        ITEM_MOD_NATURE_RESISTANCE = 55,
        ITEM_MOD_ARCANE_RESISTANCE = 56,
        ITEM_MOD_PVP_POWER = 57,
        ITEM_MOD_MULTISTRIKE_RATING = 59,
        ITEM_MOD_READINESS_RATING = 60,
        ITEM_MOD_SPEED_RATING = 61,
        ITEM_MOD_LEECH_RATING = 62,
        ITEM_MOD_AVOIDANCE_RATING = 63,
        ITEM_MOD_INDESTRUCTIBLE = 64,
        ITEM_MOD_WOD_5 = 65,
        ITEM_MOD_WOD_6 = 66,
        ITEM_MOD_STRENGTH_AGILITY_INTELLECT = 71,
        ITEM_MOD_STRENGTH_AGILITY = 72,
        ITEM_MOD_AGILITY_INTELLECT = 73,
        ITEM_MOD_STRENGTH_INTELLECT = 74,
    };

    // from https://github.com/simulationcraft/simc/blob/shadowlands/engine/dbc/data_enums.hh#L261
    enum ItemClass
    {
        ITEM_CLASS_CONSUMABLE = 0,
        ITEM_CLASS_CONTAINER = 1,
        ITEM_CLASS_WEAPON = 2,
        ITEM_CLASS_GEM = 3,
        ITEM_CLASS_ARMOR = 4,
        ITEM_CLASS_REAGENT = 5,
        ITEM_CLASS_PROJECTILE = 6,
        ITEM_CLASS_TRADE_GOODS = 7,
        ITEM_CLASS_GENERIC = 8,
        ITEM_CLASS_RECIPE = 9,
        ITEM_CLASS_MONEY = 10,
        ITEM_CLASS_QUIVER = 11,
        ITEM_CLASS_QUEST = 12,
        ITEM_CLASS_KEY = 13,
        ITEM_CLASS_PERMANENT = 14,
        ITEM_CLASS_MISC = 15,
        ITEM_CLASS_GLYPH = 16
    };

    // from https://github.com/simulationcraft/simc/blob/shadowlands/engine/dbc/data_enums.hh#L282
    enum ItemSubclassWeapon
    {
        ITEM_SUBCLASS_WEAPON_AXE = 0,
        ITEM_SUBCLASS_WEAPON_AXE2 = 1,
        ITEM_SUBCLASS_WEAPON_BOW = 2,
        ITEM_SUBCLASS_WEAPON_GUN = 3,
        ITEM_SUBCLASS_WEAPON_MACE = 4,
        ITEM_SUBCLASS_WEAPON_MACE2 = 5,
        ITEM_SUBCLASS_WEAPON_POLEARM = 6,
        ITEM_SUBCLASS_WEAPON_SWORD = 7,
        ITEM_SUBCLASS_WEAPON_SWORD2 = 8,
        ITEM_SUBCLASS_WEAPON_WARGLAIVE = 9,
        ITEM_SUBCLASS_WEAPON_STAFF = 10,
        ITEM_SUBCLASS_WEAPON_EXOTIC = 11,
        ITEM_SUBCLASS_WEAPON_EXOTIC2 = 12,
        ITEM_SUBCLASS_WEAPON_FIST = 13,
        ITEM_SUBCLASS_WEAPON_MISC = 14,
        ITEM_SUBCLASS_WEAPON_DAGGER = 15,
        ITEM_SUBCLASS_WEAPON_THROWN = 16,
        ITEM_SUBCLASS_WEAPON_SPEAR = 17,
        ITEM_SUBCLASS_WEAPON_CROSSBOW = 18,
        ITEM_SUBCLASS_WEAPON_WAND = 19,
        ITEM_SUBCLASS_WEAPON_FISHING_POLE = 20,
        ITEM_SUBCLASS_WEAPON_INVALID = 31
    };

    // from https://github.com/simulationcraft/simc/blob/shadowlands/engine/dbc/data_enums.hh#L308
    enum ItemSubclassArmor
    {
        ITEM_SUBCLASS_ARMOR_MISC = 0,
        ITEM_SUBCLASS_ARMOR_CLOTH = 1,
        ITEM_SUBCLASS_ARMOR_LEATHER = 2,
        ITEM_SUBCLASS_ARMOR_MAIL = 3,
        ITEM_SUBCLASS_ARMOR_PLATE = 4,
        ITEM_SUBCLASS_ARMOR_COSMETIC = 5,
        ITEM_SUBCLASS_ARMOR_SHIELD = 6,
        ITEM_SUBCLASS_ARMOR_LIBRAM = 7,
        ITEM_SUBCLASS_ARMOR_IDOL = 8,
        ITEM_SUBCLASS_ARMOR_TOTEM = 9,
        ITEM_SUBCLASS_ARMOR_SIGIL = 10,
        ITEM_SUBCLASS_ARMOR_RELIC = 11
    };

    // from https://github.com/simulationcraft/simc/blob/shadowlands/engine/dbc/data_enums.hh#L324
    enum ItemSubclassConsumable
    {
        ITEM_SUBCLASS_CONSUMABLE = 0,
        ITEM_SUBCLASS_POTION = 1,
        ITEM_SUBCLASS_ELIXIR = 2,
        ITEM_SUBCLASS_FLASK = 3,
        ITEM_SUBCLASS_SCROLL = 4,
        ITEM_SUBCLASS_FOOD = 5,
        ITEM_SUBCLASS_ITEM_ENHANCEMENT = 6,
        ITEM_SUBCLASS_BANDAGE = 7,
        ITEM_SUBCLASS_CONSUMABLE_OTHER = 8
    };

    // from https://github.com/simulationcraft/simc/blob/shadowlands/engine/dbc/data_enums.hh#L337
    enum InventoryType
    {
        INVTYPE_NON_EQUIP = 0,
        INVTYPE_HEAD = 1,
        INVTYPE_NECK = 2,
        INVTYPE_SHOULDERS = 3,
        INVTYPE_BODY = 4,
        INVTYPE_CHEST = 5,
        INVTYPE_WAIST = 6,
        INVTYPE_LEGS = 7,
        INVTYPE_FEET = 8,
        INVTYPE_WRISTS = 9,
        INVTYPE_HANDS = 10,
        INVTYPE_FINGER = 11,
        INVTYPE_TRINKET = 12,
        INVTYPE_WEAPON = 13,
        INVTYPE_SHIELD = 14,
        INVTYPE_RANGED = 15,
        INVTYPE_CLOAK = 16,
        INVTYPE_2HWEAPON = 17,
        INVTYPE_BAG = 18,
        INVTYPE_TABARD = 19,
        INVTYPE_ROBE = 20,
        INVTYPE_WEAPONMAINHAND = 21,
        INVTYPE_WEAPONOFFHAND = 22,
        INVTYPE_HOLDABLE = 23,
        INVTYPE_AMMO = 24,
        INVTYPE_THROWN = 25,
        INVTYPE_RANGEDRIGHT = 26,
        INVTYPE_QUIVER = 27,
        INVTYPE_RELIC = 28,
        INVTYPE_MAX = 29
    };

    // from https://github.com/simulationcraft/simc/blob/shadowlands/engine/dbc/data_enums.hh#L371
    enum ItemQuality
    {
        ITEM_QUALITY_NONE = -1,
        ITEM_QUALITY_POOR = 0,
        ITEM_QUALITY_COMMON = 1,
        ITEM_QUALITY_UNCOMMON = 2,
        ITEM_QUALITY_RARE = 3,
        ITEM_QUALITY_EPIC = 4,
        ITEM_QUALITY_LEGENDARY = 5,
        ITEM_QUALITY_ARTIFACT = 6,
        ITEM_QUALITY_MAX = 7
    };
}