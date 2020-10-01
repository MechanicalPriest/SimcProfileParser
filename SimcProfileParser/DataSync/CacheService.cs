using Newtonsoft.Json;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.DataSync;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;

namespace SimcProfileParser.DataSync
{

    internal class CacheService : ICacheService
    {
        public string BaseFileDirectory { get; }
        protected IList<CacheFileConfiguration> _registeredFiles;

        public IReadOnlyCollection<CacheFileConfiguration> RegisteredFiles => new ReadOnlyCollection<CacheFileConfiguration>(_registeredFiles);

        public CacheService()
        {
            _registeredFiles = new List<CacheFileConfiguration>();

            BaseFileDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "SimcProfileParserData");

            ETagCacheData = GetCacheData();
        }

        public CacheService(IList<CacheFileConfiguration> registeredFiles)
            : this()
        {
            _registeredFiles = registeredFiles;
        }

        string ICacheService.GetFileContents(SimcFileType FileType)
        {
            var fileConfig = _registeredFiles.Where(f => f.FileType == FileType).FirstOrDefault();

            if (fileConfig != null)
            {
                var eTag = ETagCacheData
                    .Where(e => e.Filename == fileConfig.LocalFile.LocalPath)
                    .FirstOrDefault();

                // If there is no cache data, or the file doesn't exist then re-fetch it
                if (eTag == null || !File.Exists(fileConfig.LocalFile.LocalPath))
                    eTag = null;

                ((ICacheService)this).DownloadFileIfChanged(
                    fileConfig.RemoteFile, fileConfig.LocalFile, eTag?.ETag);

                var data = File.ReadAllText(fileConfig.LocalFile.LocalPath);

                return data;
            }
            else
                return "";
        }

        bool ICacheService.DownloadFile(Uri sourceUri, Uri destinationUri)
        {
            WebClient client = new WebClient();

            try
            {
                var baseDirectory = new Uri(destinationUri, ".");
                if (!Directory.Exists(baseDirectory.OriginalString))
                    Directory.CreateDirectory(baseDirectory.OriginalString);

                client.DownloadFile(sourceUri, destinationUri.LocalPath);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        bool ICacheService.DownloadFileIfChanged(Uri sourceUri, Uri destinationUri, string eTag)
        {
            HttpClient httpClient = new HttpClient();

            HttpRequestMessage request =
               new HttpRequestMessage(HttpMethod.Head,
                  sourceUri);

            HttpResponseMessage response;

            try
            {
                response = httpClient.SendAsync(request).Result;
            }
            catch (Exception)
            {
                return false;
            }

            if (eTag != null && response.Headers.ETag.Tag == eTag)
                return true;

            var downloadResponse = ((ICacheService)this).DownloadFile(sourceUri, destinationUri);

            // If the download was successful, save the etag.
            if(downloadResponse)
            {
                UpdateCacheData(destinationUri.LocalPath, response.Headers.ETag.Tag);
            }

            return downloadResponse;
        }

        void ICacheService.RegisterFileConfiguration(SimcFileType fileType, string localFilename, string remoteFilename)
        {
            var exists = _registeredFiles.Where(f => f.FileType == fileType).FirstOrDefault();

            if(exists != null)
            {
                _registeredFiles.Remove(exists);
            }

            var configuration = new CacheFileConfiguration()
            {
                FileType = fileType,
                RemoteFile = new Uri(remoteFilename),
                LocalFile = new Uri(Path.Combine(BaseFileDirectory, localFilename))
            };

            _registeredFiles.Add(configuration);
        }

        #region eTag Cache

        private readonly string _etagCacheDataFile = "FileDownloadCache.json";
        internal List<FileETag> ETagCacheData = new List<FileETag>();

        /// <summary>
        /// Update the cache with an entry
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="eTag"></param>
        internal void UpdateCacheData(string filename, string eTag)
        {
            if (ETagCacheData.Count == 0)
                ETagCacheData = GetCacheData();

            var existing = ETagCacheData.Where(e => e.Filename == filename).FirstOrDefault();

            if (existing != null)
                existing.ETag = eTag;
            else
            {
                ETagCacheData.Add(new FileETag()
                {
                    Filename = filename,
                    ETag = eTag
                });
            }

            SaveCacheData(ETagCacheData);
        }

        /// <summary>
        /// Load the cached etag data from file
        /// </summary>
        internal List<FileETag> GetCacheData()
        {
            var results = new List<FileETag>();
            var cacheDataFile = Path.Combine(BaseFileDirectory, _etagCacheDataFile);

            if (File.Exists(cacheDataFile))
            {
                var data = File.ReadAllText(cacheDataFile);

                var deserialised = JsonConvert.DeserializeObject<List<FileETag>>(data);

                if (deserialised != null)
                    results = deserialised;
            }

            return results;
        }

        /// <summary>
        /// Save the cached etag data to file
        /// </summary>
        internal void SaveCacheData(List<FileETag> data)
        {
            var baseDirectory = new Uri(BaseFileDirectory);
            if (!Directory.Exists(baseDirectory.OriginalString))
                Directory.CreateDirectory(baseDirectory.OriginalString);

            var cacheDataFile = Path.Combine(BaseFileDirectory, _etagCacheDataFile);
            var dataString = JsonConvert.SerializeObject(data, Formatting.Indented);

            File.WriteAllText(cacheDataFile, dataString);
        }

        #endregion
    }
}
