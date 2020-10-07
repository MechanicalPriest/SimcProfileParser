using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.DataSync
{
    public class CacheFileConfiguration
    {
        internal SimcParsedFileType ParsedFileType { get; set; }
        /// <summary>
        /// Dictionary in the form of Local file : Remote file
        /// </summary>
        internal Dictionary<string, string> RawFiles { get; set; }
        internal string LocalParsedFile { get; set; }
    }
}
