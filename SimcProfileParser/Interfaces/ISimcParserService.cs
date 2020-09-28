using SimcProfileParser.Model.Profile;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    interface ISimcParserService
    {
        SimcParsedProfile ParseProfileAsync(string profileString);
        SimcParsedProfile ParseProfileAsync(List<string> profileString);
    }
}
