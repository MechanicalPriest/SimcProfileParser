# Changelog

All notable changes to the `SimcProfileParser` NuGet package are listed here.
This project follows [Semantic Versioning](https://semver.org/).

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
