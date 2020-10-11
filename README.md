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

TODO: Show some more examples of how to use the library to parse strings and manually create options/objects.

## Support
For bugs please search [issues](https://github.com/MechanicalPriest/SimcProfileParser/issues) 
then create a [new issue](https://github.com/MechanicalPriest/SimcProfileParser/issues) if needed.

For help using this library, please check the [wiki](https://github.com/MechanicalPriest/SimcProfileParser/wiki) or 
visit [discord](https://discord.gg/6Fwq4UX).
