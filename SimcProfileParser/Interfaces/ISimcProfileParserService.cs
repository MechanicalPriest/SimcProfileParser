using SimcProfileParser.Model;
using System.Collections.Generic;

namespace SimcProfileParser.Interfaces
{
    interface ISimcProfileParserService
    {
        SimcProfile GenerateProfileAsync(List<string> profileString);
        SimcProfile GenerateProfileAsync(string profileString);
        SimcProfile GenerateProfile(List<string> profileString);
        SimcProfile GenerateProfile(string profileString);

        SimcItem GenerateItemAsync(SimcItemOptions options);
        SimcItem GenerateItem(SimcItemOptions options);

        SimcSpell GenerateSpellAsync(SimcSpellOptions options);
        SimcSpell GenerateSpell(SimcSpellOptions options);
    }
}
