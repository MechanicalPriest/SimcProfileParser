using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
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
        protected IDictionary<SimcParsedFileType, object> _cachedFileData;
        protected readonly IRawDataExtractionService _rawDataExtractionService;
        protected readonly ILogger _logger;

        public IReadOnlyCollection<CacheFileConfiguration> RegisteredFiles => new ReadOnlyCollection<CacheFileConfiguration>(_registeredFiles);

        public CacheService(IRawDataExtractionService rawDataExtractionService,
            ILogger logger)
        {
            _registeredFiles = new List<CacheFileConfiguration>();
            _cachedFileData = new Dictionary<SimcParsedFileType, object>();

            BaseFileDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "SimcProfileParserData");

            _eTagCacheData = GetCacheData();
            _rawDataExtractionService = rawDataExtractionService;
            _logger = logger;

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemDataNew.json",
                ParsedFileType = SimcParsedFileType.ItemDataNew,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/item_data.inc" },
                    { "ItemEffect.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/item_effect.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemDataOld.json",
                ParsedFileType = SimcParsedFileType.ItemDataOld,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/item_data.inc" },
                    { "ItemEffect.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/item_effect.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatingMultipliers.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/sc_scale_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "StaminaMultipliers.json",
                ParsedFileType = SimcParsedFileType.StaminaMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/sc_scale_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "RandomPropPoints.json",
                ParsedFileType = SimcParsedFileType.RandomPropPoints,
                RawFiles = new Dictionary<string, string>()
                {
                    { "RandomPropPoints.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/rand_prop_points.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "SpellData.json",
                ParsedFileType = SimcParsedFileType.SpellData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "SpellData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/sc_spell_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemBonusData.json",
                ParsedFileType = SimcParsedFileType.ItemBonusData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemBonusData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/item_bonus.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "GemData.json",
                ParsedFileType = SimcParsedFileType.GemData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "GemData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/gem_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemEnchantData.json",
                ParsedFileType = SimcParsedFileType.ItemEnchantData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemEnchantData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/spell_item_enchantment.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "SpellScalingMultipliers.json",
                ParsedFileType = SimcParsedFileType.SpellScaleMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/sc_spell_data.inc" }
                }
            });
        }

        /// <summary>
        /// Get the parsed .json file contents deserialised into T, based on the provided file type.
        /// </summary>
        /// <typeparam name="T">Type to deserialise the json into</typeparam>
        /// <param name="fileType">Type of file to return data from</param>
        T ICacheService.GetParsedFileContents<T>(SimcParsedFileType fileType)
        {
            // First check if we already have the data loaded:
            if(_cachedFileData.ContainsKey(fileType))
            {
                var cachedData = _cachedFileData[fileType];

                if (cachedData is T t)
                {
                    return t;
                }

                try
                {
                    return (T)Convert.ChangeType(cachedData, typeof(T));
                }
                catch (InvalidCastException)
                {
                    return default;
                }
            }

            var configuration = _registeredFiles.Where(f => f.ParsedFileType == fileType).FirstOrDefault();
            var localPath = new Uri(Path.Combine(BaseFileDirectory, configuration.LocalParsedFile)).LocalPath;

            if (configuration == null)
                throw new ArgumentOutOfRangeException("Supplied fileType has not been registered.");
            
            // If the file doesn't exist, generate it.
            if (!File.Exists(localPath))
            {
                ((ICacheService)this).GenerateParsedFile(fileType);
            }

            var fileText = File.ReadAllText(localPath);

            var deserialisedData = JsonConvert.DeserializeObject<T>(fileText);

            _cachedFileData.Add(fileType, deserialisedData);

            return deserialisedData;
        }

        /// <summary>
        /// Generates a parsed .json file for the specified configuration by calling the RawDataExtractionService
        /// </summary>
        /// <param name="fileType">Type of file to generate data for</param>
        void ICacheService.GenerateParsedFile(SimcParsedFileType fileType)
        {
            var configuration = _registeredFiles.Where(f => f.ParsedFileType == fileType).FirstOrDefault();

            if (configuration == null)
                throw new ArgumentOutOfRangeException("Supplied fileType has not been registered.");

            // Gather together all the raw data the extraction service needs to run its process
            var rawData = new Dictionary<string, string>();
            foreach (var rawFile in configuration.RawFiles)
            {
                var data = GetRawFileContents(configuration, rawFile.Key);
                rawData.Add(rawFile.Key, data);
            }

            // Generate the parsed .json file
            var parsedData = _rawDataExtractionService.GenerateData(configuration.ParsedFileType, rawData);
            var localPath = Path.Combine(BaseFileDirectory, configuration.LocalParsedFile);

            File.WriteAllText(localPath, JsonConvert.SerializeObject(parsedData));
        }

        void ICacheService.RegisterFileConfiguration(CacheFileConfiguration configuration)
        {
            var exists = _registeredFiles
                .Where(f => f.ParsedFileType == configuration.ParsedFileType)
                .FirstOrDefault();

            if (exists != null)
            {
                _registeredFiles.Remove(exists);
            }

            _registeredFiles.Add(configuration);
        }

        internal string GetRawFileContents(CacheFileConfiguration configuration, string localRawFile)
        {
            var localPath = new Uri(Path.Combine(BaseFileDirectory, localRawFile)).LocalPath;
            if (!File.Exists(localPath))
            {
                var destinationRawFile = configuration.RawFiles.Where(r => r.Key == localRawFile).FirstOrDefault();
                DownloadFileIfChanged(new Uri(destinationRawFile.Value),
                    new Uri(Path.Combine(BaseFileDirectory, destinationRawFile.Key)));
            }

            var data = File.ReadAllText(localPath);

            return data;
        }

        /// <summary>
        /// Check if the local file exists and the cache matches
        /// </summary>
        /// <param name="sourceUri"></param>
        /// <param name="destinationUri"></param>
        /// <returns></returns>
        internal bool DownloadFileIfChanged(Uri sourceUri, Uri destinationUri)
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

            // Grab the cache info and the files last modified date.
            var eTag = _eTagCacheData
                .Where(e => e.Filename == destinationUri.LocalPath)
                .FirstOrDefault();

            DateTime lastModified = DateTime.UtcNow;

            if(File.Exists(destinationUri.LocalPath))
                lastModified = File.GetLastWriteTimeUtc(destinationUri.LocalPath);

            // Check if we need to download it or not.
            if (eTag != null && // If there is an etag
                response.Headers.ETag.Tag == eTag.ETag && // and they match
                eTag.LastModified == lastModified) // and the last modified's match
                return true; // Then we don't need to download it.

            var downloadResponse = DownloadFile(sourceUri, destinationUri);

            // If the download was successful, save the etag.
            if(downloadResponse)
            {
                lastModified = File.GetLastWriteTimeUtc(destinationUri.LocalPath);
                UpdateCacheData(destinationUri.LocalPath, response.Headers.ETag.Tag, lastModified);
            }

            return downloadResponse;
        }

        internal bool DownloadFile(Uri sourceUri, Uri destinationUri)
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

        internal void SaveParsedFile(SimcParsedFileType fileType, object contents)
        {
            var fileConfig = _registeredFiles.Where(f => f.ParsedFileType == fileType).FirstOrDefault();
            var localPath = new Uri(Path.Combine(BaseFileDirectory, fileConfig.LocalParsedFile)).LocalPath;

            var data = JsonConvert.SerializeObject(contents);

            File.WriteAllText(localPath, data);
        }

        #region eTag Cache

        private readonly string _etagCacheDataFile = "FileDownloadCache.json";
        protected List<FileETag> _eTagCacheData = new List<FileETag>();

        /// <summary>
        /// Update the cache with an entry
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="eTag"></param>
        internal void UpdateCacheData(string filename, string eTag, DateTime lastModified)
        {
            if (_eTagCacheData.Count == 0)
                _eTagCacheData = GetCacheData();

            var existing = _eTagCacheData.Where(e => e.Filename == filename).FirstOrDefault();

            if (existing != null)
                existing.ETag = eTag;
            else
            {
                _eTagCacheData.Add(new FileETag()
                {
                    Filename = filename,
                    ETag = eTag,
                    LastModified = lastModified
                });
            }

            SaveCacheData(_eTagCacheData);
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
