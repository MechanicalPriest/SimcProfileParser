using System.Collections.Generic;

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
        /// <summary>
        /// Version of the parsed file's format. Bump this whenever the parser output changes
        /// (e.g. a new field is extracted) so existing parsed files are regenerated from the cached raw files.
        /// </summary>
        internal int ParserVersion { get; set; } = 1;
    }
}
