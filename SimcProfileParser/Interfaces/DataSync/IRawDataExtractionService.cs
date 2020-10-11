using SimcProfileParser.Model.DataSync;
using System.Collections.Generic;

namespace SimcProfileParser.Interfaces.DataSync
{
    internal interface IRawDataExtractionService
    {
        object GenerateData(SimcParsedFileType fileType, Dictionary<string, string> incomingRawData);
    }
}
