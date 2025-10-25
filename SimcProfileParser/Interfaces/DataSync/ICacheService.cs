using SimcProfileParser.Model.DataSync;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces.DataSync
{
    /// <summary>
    /// A service for keeping local parsed json files ready for use.
    /// </summary>
    internal interface ICacheService
    {
        /// <summary>
        /// The base directory the cache service stores its files
        /// </summary>
        string BaseFileDirectory { get; }

        /// <summary>
        /// Get back the parsed file contents from disk
        /// </summary>
        /// <typeparam name="T">Return type to attempt to deserialise into</typeparam>
        /// <param name="fileType">Type of parsed file to return</param>
        /// <returns></returns>
        Task<T> GetParsedFileContentsAsync<T>(SimcParsedFileType fileType);

        /// <summary>
        /// Set to TRUE to use PTR data for data extraction
        /// </summary>
        bool UsePtrData { get; }
        /// <summary>
        /// Set the flag to use PTR data for data extraction
        /// </summary>
        /// <param name="usePtrData">TUE for using PTR data</param>
        void SetUsePtrData(bool usePtrData);
        /// <summary>
        /// The github branch name to use for data extraction
        /// </summary>
        string UseBranchName { get; }
        /// <summary>
        /// Set the github branch name to use for data extraction
        /// </summary>
        /// <param name="branchName">e.g. thewarwithin</param>
        void SetUseBranchName(string branchName);

        /// <summary>
        /// Clears all cached data from memory and disk.
        /// </summary>
        Task ClearCacheAsync();
    }
}
