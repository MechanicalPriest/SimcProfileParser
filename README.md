# Simc Profile Parser
![Appveyor Build Status](https://ci.appveyor.com/api/projects/status/github/MechanicalPriest/SimcProfileParser?branch=master&svg=true)

A library to parse items in the simc import format into functional objects.

## Usage
### Initialising

#### Using Dependency Injection

To implement this using Dependency Injection simply register it alongside your other services configuration 
using the `AddSimcProfileParser()` extension method:

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

#### Manual Instance Creation
A new instance can be manually created. 

**Note**: logging will not function in a manually created instance as it is provided through DI.
```csharp
ISimcProfileParser spp = new SimcProfileParser();
```

### Examples

TODO: Show some examples of how to use the library to parse strings and manually create options/objects.

## Support
For bugs please search [issues](https://github.com/MechanicalPriest/SimcProfileParser/issues) 
then create a [new issue](https://github.com/MechanicalPriest/SimcProfileParser/issues) if needed.

For help using this library, please check the [wiki](https://github.com/MechanicalPriest/SimcProfileParser/wiki) or visit [discord](https://discord.gg/6Fwq4UX).
