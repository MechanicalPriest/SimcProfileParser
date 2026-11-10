# Changelog

All notable changes to the `SimcProfileParser` NuGet package are listed here.
This project follows [Semantic Versioning](https://semver.org/).

## 4.7.0 - 2026-11-10

### Added
- **Catalysed items keep their original base stats.** Items converted through the catalyst are exported with
  `redirected_base_stats=<item id>`, the item they were converted from. They now take their stat types and allocations
  from that item, as in game, instead of from the tier item. Exposed as `SimcParsedItem.RedirectedBaseStatsItemId` and
  `SimcItemOptions.RedirectedBaseStatsItemId`.

### Fixed
- **Stamina on Midnight items was far too low** (e.g. 283 instead of 3,910). Stamina multipliers of 10 or more weren't
  being read from simc's data, which covers every item above roughly item level 210. Cached stamina data is re-parsed
  automatically on upgrade.
- **Gems with two stats only added the first, at the wrong value.** Gems now add every stat they have (e.g.
  "+16 Critical Strike & +7 Versatility"), scaled as simc does, instead of the first stat at a fixed level 70 value.
- **Crafted stats were too high** (e.g. 69 instead of 56). The stats chosen with `crafted_stats=` were applied after
  the item's stats were scaled, so they missed the combat rating multiplier.
- **Some crafted items had the wrong secondary stats.** Item bonuses that set a crafted item's stats
  (`ITEM_BONUS_MOD_ITEM_STAT`) weren't applied, so `crafted_stats=` was used instead even when the bonus had already
  chosen the stats.

## 4.6.0 - 2026-11-10

### Fixed
- **Midnight items had the wrong item level, and so the wrong stats.** Midnight items set their item level with new
  bonus types that weren't supported, so most current gear came out well below its real item level (e.g. 219 instead
  of 334). Item levels are now calculated the way simc does:
  - Item scaling config bonuses (`ITEM_BONUS_SCALE_CONFIG` / `ITEM_BONUS_SCALE_CONFIG_2`), squish curve bonuses and
    bonuses that apply another bonus list
  - Crafting quality and post-squish item level bonuses, applied after the bonuses that set the item level
  - Older item level bonuses no longer apply once a Midnight bonus has set the item level
  - Items without Midnight item level bonuses have their item level squished, as in game. **Items from before Midnight
    now have much lower item levels** (e.g. a 226 Shadowlands trinket is now 49), and their stats scale to match.
    Items created from `SimcItemOptions` with an `ItemLevel` aren't squished.
- **Item level curves gave wrong values between curve points.** Interpolating between two points of a curve used 0
  instead of the value being looked up, and values beyond the end of a curve threw an exception.

### Added
- `SimcParsedItem.ContentTuningId` and `SimcItemOptions.ContentTuningId`, from the `content_tuning=` item option. simc
  uses it to cap the level some Midnight items scale to.

## 4.5.0 - 2026-11-08

### Added
- **Chain targets.** `SimcSpellEffect.ChainTargets` and `SimcSpellEffect.ChainMultiplier` are the maximum number of
  targets the effect chains to and the multiplier applied on each jump, matching "Chain Targets" and "Chain Multiplier"
  in simc's `allspells.txt`.
- **Documented `SimcSpellEffect.Delta`**, the effect's spread ("delta=" in simc's `allspells.txt`): the value ranges
  from average × (1 − `Delta` / 2) to average × (1 + `Delta` / 2).

## 4.4.0 - 2026-11-07

### Added
- **Scaled values for player spells.** `SimcSpellEffect.ScaledValue` is the effect's value at the player level,
  matching "Scaled Value" in simc's `allspells.txt` (checked against every scaled effect in current data). For
  coefficient-scaled effects it is `Coefficient` × `ScaleBudget`; for effects with points per level it is `BaseValue`
  plus the per-level increase from the spell's level up to the player level. For item spells it is `BaseValue`.
- **Points per level.** `SimcSpellEffect.PointsPerLevel` is how much the effect's value increases per player level,
  matching "Points Per Level" in simc's `allspells.txt`.

### Fixed
- **Player spells with scaling class -9 or -10 had a `ScaleBudget` of 0.** These are now scaled like simc does. Spells
  with no scaling class but a max scaling level now use simc's fallback scaling for their coefficient-scaled effects.
- **A spell's max scaling level was applied to its trigger spells.** When a player spell had a max scaling level, trigger
  spells on its later effects were generated at the capped level instead of the player level. The cap now only applies to
  the spell's own scale budget.

## 4.3.0 - 2026-11-04

### Added
- **Talent rank values.** `SimcSpellEffect.RankOperation` and `SimcSpellEffect.RankValues` expose the per-rank values
  Blizzard's talent data defines for some talent effects, matching the "Talent Entry" line
  (`Effect#1 [op=set, values=(10, 20)]`) in simc's `allspells.txt`. `Set` means the rank value replaces `BaseValue`;
  `Multiply` means it multiplies `BaseValue`. `BaseValue` itself is unchanged.

### Fixed
- **Talent data was not being parsed.** simc added fields to its talent data, and the parser skipped every entry, so
  `GetAvailableTalentsAsync` returned no talents and generated profiles had an empty `Talents` list. Talent names
  containing commas (e.g. "Cold Steel, Hot Blood") are now parsed correctly too. Cached talent data is re-parsed
  automatically on upgrade.
- **Generating a player-scaled spell could crash with a stack overflow.** If a spell's trigger spells looped back to it
  (e.g. A triggers B triggers A), generation recursed forever. This affected over 1,000 spells in current data. Trigger
  spells already being generated further up the chain are now left as `TriggerSpell = null` (with `TriggerSpellId`
  still set), as item spells already did.

## 4.2.0 - 2026-11-04

### Added
- **Label-based spell relationships.** Effects that modify spells by spell label (e.g. Apply Flat/Percent Modifier w/ Label,
  subtypes 219/218) are now included in `SimcSpell.AffectedBySpellIds` and `SimcSpellEffect.AffectedSpellIds`.
  Many newer talents, spec auras and set bonuses use these. For example, the Holy Priest 12.0 4pc (1264853) now lists
  Divine Image and Healing Light, and they list it.
- `SimcSpell.Labels`: the spell's label IDs.
- `SimcSpellEffect.MiscValue1` and `SimcSpellEffect.MiscValue2`. For spell modifiers, `MiscValue1` is the property being
  modified. For label modifiers, `MiscValue2` is the label.

### Changed
- **`AffectedBySpellIds` and `AffectedSpellIds` may contain more spells than in 4.1.x**, because label matches are added
  to the existing family flag and category matches. Each list still contains every spell ID once. Effects targeting broad
  labels such as "Class Spells" (16) list every spell with that label.
- Parsed data files are now versioned (recorded in `ParsedFileVersions.json` in the cache directory). When the parser
  output changes, existing parsed files are regenerated from the already-downloaded raw files, without a re-download.
  Upgrading from 4.1.x re-parses the spell data once to pick up labels.

## 4.1.1 - 2026-11-04

### Added
- `ISimcGenerationService.GetAvailableTalentsAsync(classId, specId)`. This was previously only on the
  concrete `SimcGenerationService` class, so it couldn't be used through dependency injection.
- The package now includes XML documentation, so IntelliSense shows the documented members.
- Release notes in the package and this changelog.

### Fixed
- The README Talents example now compiles.

## 4.1.0 - 2026-11-04

### Added
- **Spell relationships.** Generated spells now list which spells modify each other, matching the
  "Affected Spells" / "Affecting Spells" output in simc's `allspells.txt`:
  - `SimcSpell.AffectedBySpellIds`: IDs of spells with an effect that modifies this spell
    (talents, class auras, set bonuses, etc.).
  - `SimcSpellEffect.AffectedSpellIds`: IDs of spells this effect modifies.

  Relationships are matched by spell class family flags and by spell category (cooldown and
  charge modifiers). Spell label based modifiers are not included. These properties are filled in
  for player spells, item spells and trigger spells, and are never `null`.

### Changed
- Packages are now published from GitHub Actions with NuGet trusted publishing.

## 4.0.0 - 2026-11-04

Not published to nuget.org. 4.1.0 was the first 4.x release and includes all of these changes.

### Breaking changes
- **.NET 10 is now required.** The package targets `net10.0` only. .NET 9 is no longer supported.
  This is the reason for the major version bump.
- **Dependency minimum versions raised** from 9.0.9 to 10.0.14 for
  `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging.Abstractions`.

No public API was removed or renamed in 4.0.0. Code that compiled against 3.4.0 compiles
unchanged against 4.x once it targets .NET 10.

### Upgrading from 3.x
1. Change your project's `TargetFramework` to `net10.0`.
2. If you reference `Microsoft.Extensions.*` 9.x packages directly, update them to 10.x to avoid
   NuGet downgrade errors (NU1605).

## 3.4.0 - 2026-04-28
### Fixed
- Parsing updated for changes to simc's spell data format.

## 3.3.0 - 2026-01-25
### Fixed
- Parsing updated for changes to simc's data layout.

## 3.2.0 - 2025-10-26
### Changed
- Improved cache logic for loading outdated data files.

## 3.1.2 - 2025-10-25
### Changed
- **Breaking:** internal service interfaces (`ICacheService`, `ISimcItemCreationService`,
  `ISimcParserService`, `ISimcSpellCreationService`, `ISimcTalentService`, `ISimcVersionService`)
  and `SimcParserService` are no longer public. Use `ISimcGenerationService` instead.

## 3.1.1 - 2025-10-25
### Added
- `ClearCacheAsync()` to clear all cached data.

## 3.1.0 - 2025-10-25
### Changed
- Game data now comes from the `midnight` simc branch (WoW 12.x).
- Replaced the `Newtonsoft.Json` dependency with `System.Text.Json`.

## 3.0.0 - 2025-10-12
### Changed
- Updated the simc data branch and dependencies, and removed the Covenant configuration.
