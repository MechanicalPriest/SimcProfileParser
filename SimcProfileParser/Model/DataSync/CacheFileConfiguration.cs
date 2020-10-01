using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.DataSync
{
    public class CacheFileConfiguration
    {
        internal SimcFileType FileType { get; set; }
        internal Uri LocalFile { get; set; }
        internal Uri RemoteFile { get; set; }
    }
}
