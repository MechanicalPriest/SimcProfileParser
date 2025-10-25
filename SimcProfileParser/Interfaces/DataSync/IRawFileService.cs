using SimcProfileParser.Model.DataSync;
using System;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces.DataSync
{
    internal interface IRawFileService
    {
        bool UsePtrData { get; }
        string UseBranchName { get; }

        Task DeleteAllFiles();
        Task ConfigureAsync(string baseFileDirectory);
        Task<string> GetFileContentsAsync(CacheFileConfiguration configuration, string localRawFile);
        void SetUsePtrData(bool usePtrData);
        void SetUseBranchName(string branchName);
        Task<bool> IsFileValidAsync(string remoteFile, string destinationFile);
    }
}