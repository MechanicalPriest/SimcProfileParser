using System;

namespace SimcProfileParser.Model.DataSync
{
    public class FileETag
    {
        public string Filename { get; set; }
        public string ETag { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime LastChecked { get; set; }
    }
}
