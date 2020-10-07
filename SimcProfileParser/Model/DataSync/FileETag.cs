using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.DataSync
{
    public class FileETag
    {
        public string Filename { get; set; }
        public string ETag { get; set; }
        public DateTime LastModified { get; set; }
    }
}
