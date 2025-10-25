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
Console.WriteLine($"Profile object created for player {profile.Name}.");

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

// Output some details about the profile
Console.WriteLine($"Profile object created for a level {profile.Level}");
Console.WriteLine($"They are weilding {profile.Items.FirstOrDefault().Name}.");

var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions() { WriteIndented = true });
await File.WriteAllTextAsync("profile.json", json);
```

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

var item = await sgs.GenerateItemAsync(spellOptions);

var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions() { WriteIndented = true });
await File.WriteAllTextAsync("item.json", json);
```

There are other options that can be set, including bonus ids, gems and the original drop level:

```csharp
public uint ItemId { get; set; }
public int ItemLevel { get; set; }
public IList<int> BonusIds { get; set; }
public IList<int> GemIds { get; set; }
public ItemQuality Quality { get; set; }
public int DropLevel { get; set; }
```

#### Creating a single spell
There are some basic options to manually create a spell using `ISimcGenerationService.GenerateSpellAsync`.

There are two types of generatable spells: 

 - Player Scaling: the type that scale with the player level / class, such as racials. 
 - Item Scaling: the type that scales with the item quality/level, such as trinkets.

Generating an item scaling spell (id 343538) for a **rare trinket at ilvl 226**:

```csharp
ISimcGenerationService sgs = new SimcGenerationService();

var spellOptions = new SimcSpellOptions()
{
    ItemLevel = 730,
    SpellId = 1238697,
    ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
    ItemInventoryType = InventoryType.INVTYPE_TRINKET
};

var spell = await sgs.GenerateSpellAsync(spellOptions);

var json = JsonSerializer.Serialize(spell, new JsonSerializerOptions() { WriteIndented = true });
await File.WriteAllTextAsync("spell.json", json);
```

Generating an player scaling spell (id 274740):

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

The spell object has a property `ScaleBudget` which can be multiplied with a coeffecient from a spells effect if required. 
Otherwise typically the BaseValue/ScaledValue of the effect will be what you're looking for.

## Support
For bugs please search [issues](https://github.com/MechanicalPriest/SimcProfileParser/issues) 
then create a [new issue](https://github.com/MechanicalPriest/SimcProfileParser/issues) if needed.

For help using this library, please check the [wiki](https://github.com/MechanicalPriest/SimcProfileParser/wiki) or 
visit [discord](https://discord.gg/6Fwq4UX).
