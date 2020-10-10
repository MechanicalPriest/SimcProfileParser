using SimcProfileParser.Model;
using SimcProfileParser.Model.Profile;
using System;
using System.Collections.Generic;
using System.Text;

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
