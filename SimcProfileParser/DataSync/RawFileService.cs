using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.DataSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimcProfileParser.DataSync
{
    /// <summary>
    /// The intent of this class is to manage access to the raw data files. 
    /// </summary>
    internal class RawFileService(ILogger<RawFileService> logger) : IRawFileService
    {
        private string BaseFileDirectory = Path.Combine(AppContext.BaseDirectory, "SimcDataCache");
        private readonly string _rawFileCacheDataFile = "FileDownloadCache.json";
        protected List<FileETag> _rawFileEtagCacheData = new();
        private static readonly HttpClient downloadClient = new();

        private readonly string rawDataFileExtension = "raw";
        private bool usePtrData = false;
        private string useBranchName = "midnight";
        internal string _getUrl(string fileName) => "https://raw.githubusercontent.com/simulationcraft/simc/"
                + useBranchName + "/engine/dbc/generated/"
                + fileName
                + (usePtrData ? "_ptr" : "")
                + ".inc";

        public async Task<string> GetFileContentsAsync(CacheFileConfiguration configuration, string localRawFile)
        {
            // Always: If we download a raw file or generate a.json file, we must update FileDownloadCache.json
            // If it's been more than an hour since we last checked the ETag for a file,
            //  when accessing in-memory processed data we should revalidate the associated raw file
            // If a raw file needs to be validated, we do this by checking the file exists on disk still,
            //  and it's stored ETag matches against the remote file using a head request

            var localPath = Path.Combine(BaseFileDirectory, localRawFile);

            var destinationRawFile = configuration.RawFiles.FirstOrDefault(r => r.Key == localRawFile);
            var remoteFileUri = new Uri(_getUrl(destinationRawFile.Value));
            var localFilePath = Path.Combine(BaseFileDirectory, destinationRawFile.Key);

            var downloaded = await DownloadFileIfInvalidAsync(remoteFileUri, localFilePath);

            if (!downloaded)
            {
                if(!File.Exists(localPath))
                    throw new FileNotFoundException($"Failed to obtain raw file '{destinationRawFile.Key}'", localPath);

                logger?.LogError("Unable to download [{destinationRawFile}] to [{localPath}]", destinationRawFile, localPath);
                if (Directory.Exists(BaseFileDirectory))
                {
                    logger?.LogTrace("Listing directory contents for [{BaseFileDirectory}]", BaseFileDirectory);
                    foreach (var file in Directory.GetFiles(BaseFileDirectory))
                    {
                        logger?.LogTrace("File: {file}", file);
                    }
                }
                else
                {
                    logger?.LogError("Directory does not exist: [{BaseFileDirectory}]", BaseFileDirectory);
                }
            }

            var data = await File.ReadAllTextAsync(localPath);

            return data;
        }

        public async Task<bool> IsFileValidAsync(string remoteFile, string localFile)
        {
            var remoteUri = new Uri(_getUrl(remoteFile));
            var localUri = Path.Combine(BaseFileDirectory, localFile);

            return await IsFileValidAsync(remoteUri, localUri);
        }

        /// <summary>
        /// A file is valid if it exists, and its etag has been checked within 
        /// the last hour and matches the remote etag.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsFileValidAsync(Uri remoteUri, string localUri)
        {
            // First, check if the file exists.
            if (!File.Exists(localUri))
                return false;

            // Get the etag from the cache
            var cachedEtag = _rawFileEtagCacheData.FirstOrDefault(e => e.Filename == localUri);

            if (cachedEtag == null)
            {
                logger?.LogTrace("No etag found for {destinationUri.LocalPath}", localUri);
                return false;
            }
            logger?.LogTrace("etag for this file is {eTag}", cachedEtag);

            // Now, if we haven't checked in the last hour, check if the etag is valid.
            if (cachedEtag.LastChecked <= DateTime.UtcNow.AddHours(-1))
            {
                var remoteEtag = await GetRemoteEtagAsync(remoteUri);

                if (string.IsNullOrWhiteSpace(remoteEtag))
                {
                    logger?.LogTrace("No remote etag found for {sourceUri}", remoteUri);
                    return false;
                }

                var valid = string.Equals(cachedEtag.ETag, remoteEtag, StringComparison.Ordinal);
                logger?.LogTrace("File {destinationUri.LocalPath} etag valid: {valid}", localUri, valid);

                if (valid)
                {
                    cachedEtag.LastChecked = DateTime.UtcNow;
                    await SaveEtagDetailsToDiskAsync();
                }

                return valid;
            }

            logger?.LogTrace("File {destinationUri.LocalPath} exists and etag was checked recently.", localUri);
            return true;
        }

        /// <summary>
        /// Check if the local file exists and the cache matches
        /// </summary>
        internal async Task<bool> DownloadFileIfInvalidAsync(Uri remoteUri, string localUri)
        {
            // A file is invalid if:
            // - It does not exist
            // - The etag does not match
            // The etag should be checked every LastChecked + 1 hour.
            if(await IsFileValidAsync(remoteUri, localUri))
            {
                logger?.LogTrace("File {destinationUri.LocalPath} is valid, no download needed.", localUri);
                return true;
            }

            // If we've reached this point, the local file is either missing or invalid.
            var downloadResponse = await DownloadFileAsync(remoteUri, localUri);

            // If the download was successful, save the etag.
            if (downloadResponse)
            {
                logger?.LogTrace("Successfully downloaded {sourceUri} to {destinationUri.LocalPath}", remoteUri, localUri);

                var remoteEtag = await GetRemoteEtagAsync(remoteUri);

                await UpdateRawFileDetailsAsync(localUri, 
                    remoteEtag, 
                    File.GetLastWriteTimeUtc(localUri));
            }
            else
            {
                logger?.LogError("Failure downloading file.");
            }

            return downloadResponse;
        }

        private async Task<string> GetRemoteEtagAsync(Uri sourceUri)
        {
            HttpRequestMessage request =
               new(HttpMethod.Head,
                  sourceUri);

            HttpResponseMessage response;

            try
            {
                response = await downloadClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error checking etag for {sourceUri}", sourceUri);
                return string.Empty;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger?.LogError("Error checking etag for {sourceUri}. Status code: {StatusCode}", sourceUri, response.StatusCode);
                return string.Empty;
            }

            var tag = response.Headers.ETag?.Tag;
            if (string.IsNullOrWhiteSpace(tag))
                return string.Empty;

            logger?.LogTrace("ETag for {sourceUri} is {ETag}", sourceUri, tag);

            return tag;
        }

        /// <summary>
        /// Update the cache with an entry
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="eTag"></param>
        internal async Task UpdateRawFileDetailsAsync(string filename, string eTag, DateTime lastModified)
        {
            var existing = _rawFileEtagCacheData.FirstOrDefault(e => e.Filename == filename);

            if (existing != null)
            {
                existing.ETag = eTag;
                existing.LastModified = lastModified;
                existing.LastChecked = DateTime.UtcNow;
            }
            else
            {
                _rawFileEtagCacheData.Add(new FileETag()
                {
                    Filename = filename,
                    ETag = eTag,
                    LastModified = lastModified,
                    LastChecked = DateTime.UtcNow
                });
            }

            await SaveEtagDetailsToDiskAsync();
        }

        /// <summary>
        /// Load the cached etag data from file
        /// </summary>
        internal async Task<List<FileETag>> GetCacheDataAsync()
        {
            var results = new List<FileETag>();
            var cacheDataFile = Path.Combine(BaseFileDirectory, _rawFileCacheDataFile);

            if (File.Exists(cacheDataFile))
            {
                var data = await File.ReadAllTextAsync(cacheDataFile);

                var deserialised = JsonSerializer.Deserialize<List<FileETag>>(data);

                if (deserialised != null)
                    results = deserialised;
            }

            return results;
        }

        /// <summary>
        /// Save the cached etag data to file. Do this each time an update is made.
        /// </summary>
        internal async Task SaveEtagDetailsToDiskAsync()
        {
            var baseDirectory = new Uri(BaseFileDirectory);
            if (!Directory.Exists(baseDirectory.LocalPath))
                Directory.CreateDirectory(baseDirectory.LocalPath);

            var cacheDataFile = Path.Combine(BaseFileDirectory, _rawFileCacheDataFile);
            var dataString = JsonSerializer.Serialize(_rawFileEtagCacheData, new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(cacheDataFile, dataString);
        }

        /// <summary>
        /// Configures the base directory used for file operations.
        /// </summary>
        /// <param name="baseFileDirectory">The path to the directory that will be set as the base for file operations. Cannot be null.</param>
        public async Task ConfigureAsync(string baseFileDirectory)
        {
            BaseFileDirectory = baseFileDirectory;
            _rawFileEtagCacheData = await GetCacheDataAsync();
        }

        public async Task DeleteAllFiles()
        {
            _rawFileEtagCacheData.Clear();
            await SaveEtagDetailsToDiskAsync();
            try
            {
                if (Directory.Exists(BaseFileDirectory))
                {
                    foreach (var file in Directory.GetFiles(BaseFileDirectory, $"*.{rawDataFileExtension}"))
                    {
                        try 
                        { 
                            File.Delete(file); 
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Failed to delete cache file {file}", file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error clearing cache directory {BaseFileDirectory}", BaseFileDirectory);
            }
        }

        /// <summary>
        /// Download a file from sourceUri to destinationUri, overwriting if it exists.
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> DownloadFileAsync(Uri sourceUri, string destinationFile)
        {
            try
            {
                if (!Directory.Exists(BaseFileDirectory))
                    Directory.CreateDirectory(BaseFileDirectory);

                using var s = await downloadClient.GetStreamAsync(sourceUri);

                if (File.Exists(destinationFile))
                    File.Delete(destinationFile);

                using var fs = new FileStream(destinationFile, FileMode.CreateNew);
                await s.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unable to DownloadFileAsync [{sourceUri}] to [{destinationUri}]", sourceUri, destinationFile);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Get the flag used PTR data for data extraction
        /// </summary>
        public bool UsePtrData { get => usePtrData; }
        public void SetUsePtrData(bool usePtrData) => this.usePtrData = usePtrData;

        /// <summary>
        /// Get the github branch name used for data extraction
        /// </summary>
        public string UseBranchName { get => useBranchName; }
        public void SetUseBranchName(string branchName) => useBranchName = branchName;
    }
}
