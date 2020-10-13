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
ISimcProfileParser spp = new SimcProfileParser();
```

To provide logging to the new instance and its children, supply an `ILoggerFactory`:
```csharp
ISimcProfileParser spp = new SimcProfileParser(myLoggerFactory);
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

Then you request an instance of `ISimcProfileParserService` through DI in your class constructors:
```csharp
class MyClass
{
    private readonly ISimcProfileParserService _simcProfileParserService;

    public MyClass(ISimcProfileParserService simcProfileParserService)
    {
        _simcProfileParserService = simcProfileParserService;
    }
}
```
### Examples
#### Parsing profile files/strings
Generating a profile object from a simc import file named `import.simc`:

```csharp
ISimcProfileParser spp = new SimcProfileParser();

// Using async
var profile = await spp.GenerateProfileAsync(File.ReadAllText("import.simc"));

Console.WriteLine($"Profile object created for player {profile.Name}.");
```

You can also generate a profile object from individual lines of an import file:

```csharp
ISimcProfileParser spp = new SimcProfileParser();

var lines = new List<string>()
{
    "level=60",
    "main_hand=,id=178473,bonus_id=6774/1504/6646"
};

var profile = await spp.GenerateProfileAsync(lines);

Console.WriteLine($"Profile object created for a level {profile.Level}");
Console.WriteLine($"They are weilding {profile.Items.FirstOrDefault().Name}.");
```

#### Creating a single spell
There are some basic options to manually create an item using `ISimcGenerationService.GenerateSpellAsync`.

There are two types of generatable spells: 

 - Player Scaling: the type that scale with the player level / class, such as racials. 
 - Item Scaling: the type that scales with the item quality/level, such as trinkets.

Generating an item scaling spell (id 343538) for a **rare trinket at ilvl 226**:

```csharp
ISimcProfileParser spp = new SimcProfileParser();

var spellOptions = new SimcSpellOptions()
{
    ItemLevel = 226,
    SpellId = 343538,
    ItemQuality = ItemQuality.ITEM_QUALITY_EPIC,
    ItemInventoryType = InventoryType.INVTYPE_TRINKET
};

var spell = await spp.GenerateSpellAsync(spellOptions);
```

Generating an player scaling spell (id 274740):

```csharp
ISimcProfileParser spp = new SimcProfileParser();

var spellOptions = new SimcSpellOptions()
{
    SpellId = 274740,
    PlayerLevel = 60
};

var spell = await spp.GenerateSpellAsync(spellOptions);
```

The spell object has a property `ScaleBudget` which can be multiplied with a coeffecient from a spells effect if required. 
Otherwise typically the BaseValue/ScaledValue of the effect will be what you're looking for.

## Support
For bugs please search [issues](https://github.com/MechanicalPriest/SimcProfileParser/issues) 
then create a [new issue](https://github.com/MechanicalPriest/SimcProfileParser/issues) if needed.

For help using this library, please check the [wiki](https://github.com/MechanicalPriest/SimcProfileParser/wiki) or 
visit [discord](https://discord.gg/6Fwq4UX).
