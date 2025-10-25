# Simc Profile Parser
![Appveyor Build Status](https://ci.appveyor.com/api/projects/status/github/MechanicalPriest/SimcProfileParser?branch=master&svg=true)
![Publish to nuget](https://github.com/MechanicalPriest/SimcProfileParser/workflows/Publish%20to%20nuget/badge.svg?branch=master)
![Build & Run Tests (.NET Core)](https://github.com/MechanicalPriest/SimcProfileParser/workflows/Build%20&%20Run%20Tests%20(.NET%20Core)/badge.svg?branch=master)

A library to parse items in the simc import format into functional objects.

**PLEASE NOTE**: This library is still a work in progress and is being created to support another project. 
Please raise an issue if something isn't working as you would expect or throws a `NotYetImplemented` 
exception and it may be prioritised. 

## Usage
### Initialising

#### Instance Creation
A new instance can be manually created. 

```csharp
ISimcGenerationService sgs = new SimcGenerationService();
```

To provide logging to the new instance and its children, supply an `ILoggerFactory`:
```csharp
ISimcGenerationService sgs = new SimcGenerationService(myLoggerFactory);
```

#### Using Dependency Injection

To implement this using Dependency Injection register it alongside your other services configuration 
using the `AddSimcProfileParser()` extension method when configuring your DI services:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSimcProfileParser();
}
```

Then you request an instance of `ISimcGenerationService` through DI in your class constructors:
```csharp
class MyClass
{
    private readonly ISimcGenerationService _simcGenerationService;

    public MyClass(ISimcGenerationService simcGenerationService)
    {
        _simcGenerationService = simcGenerationService;
    }
}
```

### Examples

#### Parsing profile files/strings
Generating a profile object from a simc import file named `import.simc`:

```csharp
ISimcGenerationService sgs = new SimcGenerationService();

// Using async
var profile = await sgs.GenerateProfileAsync(File.ReadAllText("import.simc"));

// Output some details about the profile
Console.WriteLine($"Profile: {profile.ParsedProfile.Name} - Level {profile.ParsedProfile.Level}");
Console.WriteLine($"Items loaded: {profile.GeneratedItems.Count}");
Console.WriteLine($"Talents loaded: {profile.Talents.Count}");

var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions() { WriteIndented = true });
await File.WriteAllTextAsync("profile.json", json);
```

You can also generate a profile object from individual lines of an import file:

```csharp
ISimcGenerationService sgs = new SimcGenerationService();

var lines = new List<string>()
{
    "level=90",
    "main_hand=,id=237728,bonus_id=6652/10356/13446/1540/10255"
};

var profile = await sgs.GenerateProfileAsync(lines);

// Access parsed data
Console.WriteLine($"Profile object created for a level {profile.ParsedProfile.Level}");

// Access enriched items with full stats
var firstItem = profile.GeneratedItems.FirstOrDefault();
Console.WriteLine($"Wielding {firstItem.Name} with {firstItem.Mods.Count} stats");

var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions() { WriteIndented = true });
await File.WriteAllTextAsync("profile.json", json);
```

**Understanding Profile Output:**
- `ParsedProfile` - Raw parsed character data (name, class, spec, level, etc.)
- `GeneratedItems` - Fully enriched items with calculated stats, gems, sockets, and effects
- `Talents` - Talent details including spell IDs, names, and ranks

#### Creating a single item
There are some basic options to manually create an item using `ISimcGenerationService.GenerateItemAsync`.

```csharp
ISimcGenerationService sgs = new SimcGenerationService();

var itemOptions = new SimcItemOptions()
{
    ItemId = 242392,
    Quality = ItemQuality.ITEM_QUALITY_EPIC,
    ItemLevel = 730
};

var item = await sgs.GenerateItemAsync(itemOptions);

Console.WriteLine($"Item: {item.Name} (iLevel {item.ItemLevel})");
Console.WriteLine($"Stats: {item.Mods.Count}, Sockets: {item.Sockets.Count}, Effects: {item.Effects.Count}");

var json = JsonSerializer.Serialize(item, new JsonSerializerOptions() { WriteIndented = true });
await File.WriteAllTextAsync("item.json", json);
```

Available item options:

```csharp
public uint ItemId { get; set; }                // Required: The game item ID
public int ItemLevel { get; set; }              // Override item level (takes precedence over bonus IDs)
public IList<int> BonusIds { get; set; }        // Bonus IDs that modify stats, sockets, or item level
public IList<int> GemIds { get; set; }          // Gem item IDs to socket into the item
public IList<int> CraftedStatIds { get; set; }  // Stat IDs for player-crafted items
public ItemQuality Quality { get; set; }        // Override item quality
public int DropLevel { get; set; }              // Character level when item dropped (affects scaling)
```

**Item Generation Details:**
- **Bonus IDs**: Modify items by adding stats, sockets, changing item level, or quality
- **Gems**: Add secondary stats when socketed into items with sockets
- **Drop Level**: Affects item level scaling for items with dynamic scaling curves
- **Crafted Stats**: Specify which secondary stats player-crafted gear should have
- **Explicit ItemLevel**: If set, overrides any item level modifications from bonus IDs

#### Creating a single spell
There are some basic options to manually create a spell using `ISimcGenerationService.GenerateSpellAsync`.

**There are two types of spell scaling:**

**Item Scaling** - For spells that scale with item level (trinkets, enchants, weapon procs):
```csharp
ISimcGenerationService sgs = new SimcGenerationService();

var spellOptions = new SimcSpellOptions()
{
    SpellId = 1238697,
    ItemLevel = 730,
    ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
    ItemInventoryType = InventoryType.INVTYPE_TRINKET
};

var spell = await sgs.GenerateSpellAsync(spellOptions);

// Calculate scaled effect values
var effect = spell.Effects.FirstOrDefault();
var scaledValue = effect.BaseValue + (effect.Coefficient * effect.ScaleBudget);
Console.WriteLine($"Spell: {spell.Name}, Scaled Value: {scaledValue}");

var json = JsonSerializer.Serialize(spell, new JsonSerializerOptions() { WriteIndented = true });
await File.WriteAllTextAsync("spell.json", json);
```

**Player Scaling** - For spells that scale with player level (racials, class abilities):
```csharp
ISimcGenerationService sgs = new SimcGenerationService();

var spellOptions = new SimcSpellOptions()
{
    SpellId = 274740,
    PlayerLevel = 90
};

var spell = await sgs.GenerateSpellAsync(spellOptions);

var json = JsonSerializer.Serialize(spell, new JsonSerializerOptions() { WriteIndented = true });
await File.WriteAllTextAsync("spell.json", json);
```

Each spell effect has a `ScaleBudget` property that should be multiplied with the effect's coefficient to get the final scaled value. 
For simple cases, the `BaseValue` property contains the unscaled value.

#### Working with Talents

```csharp
ISimcGenerationService sgs = new SimcGenerationService();

// Get all available talents for a class/spec
// Example: Holy Priest (classId: 5, specId: 257)
var talents = await sgs.GetAvailableTalentsAsync(classId: 5, specId: 257);

Console.WriteLine($"Found {talents.Count} talents for Holy Priest");
```

## Configuration

### Switching Game Data Versions

The library automatically downloads game data from [SimulationCraft's GitHub repository](https://github.com/simulationcraft/simc). 
You can control which version of the data to use:

```csharp
var service = new SimcGenerationService();

// Use PTR (Public Test Realm) data instead of live game data
service.UsePtrData = true;

// Switch between expansions
service.UseBranchName = "midnight";        // Midnight expansion (WoW 12.x) - default
// service.UseBranchName = "thewarwithin"; // The War Within (WoW 11.x)
// service.UseBranchName = "dragonflight"; // Dragonflight (WoW 10.x)

// Check which game version the data is from
var version = await service.GetGameDataVersionAsync();
Console.WriteLine($"Using game data from WoW version: {version}");
```

**Note:** Changing `UsePtrData` or `UseBranchName` will clear all cached data to prevent mixing incompatible game versions.

### Data Caching

Game data files are automatically downloaded and cached in your system's temp directory for performance. 
The library handles:
- Automatic downloading when data is first needed
- ETag-based cache validation to check for updates
- Efficient memory caching of parsed data

Cache location: `Path.GetTempPath() + "SimcProfileParserData"`

## Important Notes

### Supported Game Data
- **Item data**: Base stats, sockets, item levels, bonus IDs, gems, enchants
- **Spell data**: Effects, scaling, cooldowns, durations, proc rates (RPPM)
- **Talent data**: Talent trees, spell IDs, ranks, requirements
- **Scaling data**: Combat rating multipliers, stamina multipliers, spell scaling by level

### Limitations
- Some newer bonus ID types may not be fully implemented
- Complex talent prerequisites and choice nodes are not validated
- Covenant/Soulbind data is parsed but may not be enriched (legacy content)

### Error Handling
The library throws exceptions for invalid inputs:
- `ArgumentNullException` - When required parameters are null or empty
- `ArgumentOutOfRangeException` - When item/spell IDs are not found in the game data
- `NotImplementedException` - When encountering unsupported game features

Always wrap API calls in try-catch blocks when dealing with user input.

## Support
For bugs please search [issues](https://github.com/MechanicalPriest/SimcProfileParser/issues) 
then create a [new issue](https://github.com/MechanicalPriest/SimcProfileParser/issues) if needed.

For help using this library, please check the [wiki](https://github.com/MechanicalPriest/SimcProfileParser/wiki) or 
visit [discord](https://discord.gg/6Fwq4UX).
