using SimcProfileParser.Model.DataSync;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Interfaces.DataSync
{
    internal interface IRawDataExtractionService
    {
        object GenerateData(SimcParsedFileType fileType, Dictionary<string, string> incomingRawData);
    }
}
