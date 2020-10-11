using SimcProfileParser.Model.Profile;
using System.Collections.Generic;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcParserService
    {
        SimcParsedProfile ParseProfileAsync(List<string> profileString);
    }
}
