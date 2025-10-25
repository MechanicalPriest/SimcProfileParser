using SimcProfileParser.Model.Profile;
using System.Collections.Generic;

namespace SimcProfileParser.Interfaces
{
    internal interface ISimcParserService
    {
        SimcParsedProfile ParseProfileAsync(List<string> profileString);
    }
}
