using SimcProfileParser.Model.DataSync;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Interfaces.DataSync
{
    internal interface ICacheService
    {
        string BaseFileDirectory { get; }
        IReadOnlyCollection<CacheFileConfiguration> RegisteredFiles { get; }
        string GetFileContents(SimcFileType FileType);
        bool DownloadFileIfChanged(Uri sourceUri, Uri destinationUri, string eTag);
        bool DownloadFile(Uri sourceUri, Uri destinationUri);
        void RegisterFileConfiguration(SimcFileType fileType, string localFilename, string remoteFilename);
    }
}
