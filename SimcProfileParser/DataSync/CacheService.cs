using Microsoft.Extensions.Logging;
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
using System.Threading.Tasks;

namespace SimcProfileParser.DataSync
{

    internal class CacheService : ICacheService
    {
        public string BaseFileDirectory { get; }

        protected IList<CacheFileConfiguration> _registeredFiles;
        protected IDictionary<SimcParsedFileType, object> _cachedFileData;
        protected readonly IRawDataExtractionService _rawDataExtractionService;
        protected readonly ILogger<CacheService> _logger;

        public IReadOnlyCollection<CacheFileConfiguration> RegisteredFiles => new ReadOnlyCollection<CacheFileConfiguration>(_registeredFiles);

        public CacheService(IRawDataExtractionService rawDataExtractionService,
            ILogger<CacheService> logger)
        {
            _registeredFiles = new List<CacheFileConfiguration>();
            _cachedFileData = new Dictionary<SimcParsedFileType, object>();

            BaseFileDirectory = Path.Combine(
                Path.GetTempPath(), "SimcProfileParserData");

            _rawDataExtractionService = rawDataExtractionService;
            _logger = logger;

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemDataNew.json",
                ParsedFileType = SimcParsedFileType.ItemDataNew,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/item_data.inc" },
                    { "ItemEffectData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/item_effect.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemDataOld.json",
                ParsedFileType = SimcParsedFileType.ItemDataOld,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/item_data.inc" },
                    { "ItemEffectData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/item_effect.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatingMultipliers.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/sc_scale_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "StaminaMultipliers.json",
                ParsedFileType = SimcParsedFileType.StaminaMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/sc_scale_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "RandomPropPoints.json",
                ParsedFileType = SimcParsedFileType.RandomPropPoints,
                RawFiles = new Dictionary<string, string>()
                {
                    { "RandomPropPoints.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/rand_prop_points.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "SpellData.json",
                ParsedFileType = SimcParsedFileType.SpellData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "SpellData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/sc_spell_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemBonusData.json",
                ParsedFileType = SimcParsedFileType.ItemBonusData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemBonusData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/item_bonus.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "GemData.json",
                ParsedFileType = SimcParsedFileType.GemData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "GemData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/gem_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemEnchantData.json",
                ParsedFileType = SimcParsedFileType.ItemEnchantData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemEnchantData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/spell_item_enchantment.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "SpellScalingMultipliers.json",
                ParsedFileType = SimcParsedFileType.SpellScaleMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/sc_scale_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "CurveData.json",
                ParsedFileType = SimcParsedFileType.CurvePoints,
                RawFiles = new Dictionary<string, string>()
                {
                    { "CurveData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/item_scaling.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "RppmData.json",
                ParsedFileType = SimcParsedFileType.RppmData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "RppmData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/real_ppm_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "CovenantData.json",
                ParsedFileType = SimcParsedFileType.CovenantData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "CovenantData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/covenant_data.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemEffectData.json",
                ParsedFileType = SimcParsedFileType.ItemEffectData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemEffectData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/item_effect.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "GameDataVersion.json",
                ParsedFileType = SimcParsedFileType.GameDataVersion,
                RawFiles = new Dictionary<string, string>()
                {
                    { "GameDataVersion.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/client_data_version.inc" }
                }
            });

            ((ICacheService)this).RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "TraitData.json",
                ParsedFileType = SimcParsedFileType.TraitData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "TraitData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/trait_data.inc" }
                }
            });
        }

        /// <summary>
        /// Get the parsed .json file contents deserialised into T, based on the provided file type.
        /// </summary>
        /// <typeparam name="T">Type to deserialise the json into</typeparam>
        /// <param name="fileType">Type of file to return data from</param>
        async Task<T> ICacheService.GetParsedFileContentsAsync<T>(SimcParsedFileType fileType)
        {
            // First check if we already have the data loaded:
            if (_cachedFileData.ContainsKey(fileType))
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

            var configuration = _registeredFiles.Where(f => f.ParsedFileType == fileType).FirstOrDefault()
                ?? throw new ArgumentOutOfRangeException(nameof(fileType), "Supplied fileType has not been registered.");

            var localPath = new Uri(Path.Combine(BaseFileDirectory, configuration.LocalParsedFile)).LocalPath;

            // If the file doesn't exist, generate it.
            if (!File.Exists(localPath))
            {
                _logger?.LogTrace("File [{localPath}] does not exist, generating it...", localPath);
                await ((ICacheService)this).GenerateParsedFileAsync(fileType);
            }

            var fileText = await File.ReadAllTextAsync(localPath);

            var deserialisedData = JsonConvert.DeserializeObject<T>(fileText);

            _cachedFileData.Add(fileType, deserialisedData);

            return deserialisedData;
        }

        /// <summary>
        /// Generates a parsed .json file for the specified configuration by calling the RawDataExtractionService
        /// </summary>
        /// <param name="fileType">Type of file to generate data for</param>
        async Task ICacheService.GenerateParsedFileAsync(SimcParsedFileType fileType)
        {
            var configuration = _registeredFiles.Where(f => f.ParsedFileType == fileType).FirstOrDefault() 
                ?? throw new ArgumentOutOfRangeException(nameof(fileType), "Supplied fileType has not been registered.");

            // Gather together all the raw data the extraction service needs to run its process
            var rawData = new Dictionary<string, string>();
            foreach (var rawFile in configuration.RawFiles)
            {
                var data = await GetRawFileContentsAsync(configuration, rawFile.Key);
                rawData.Add(rawFile.Key, data);
            }

            // Generate the parsed .json file
            var parsedData = _rawDataExtractionService.GenerateData(configuration.ParsedFileType, rawData);
            var localPath = Path.Combine(BaseFileDirectory, configuration.LocalParsedFile);

            _logger?.LogTrace("Saving parsed json data for [{configuration.ParsedFileType}] to [{localPath}]", configuration.ParsedFileType, localPath);
            await File.WriteAllTextAsync(localPath, JsonConvert.SerializeObject(parsedData));
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

        internal async Task<string> GetRawFileContentsAsync(CacheFileConfiguration configuration, string localRawFile)
        {
            var localPath = new Uri(Path.Combine(BaseFileDirectory, localRawFile)).LocalPath;
            if (!File.Exists(localPath))
            {
                var destinationRawFile = configuration.RawFiles.Where(r => r.Key == localRawFile).FirstOrDefault();

                _logger?.LogTrace("Path does not exist: [{localPath}] - attempting to download file from [{destinationRawFile}].", localPath, destinationRawFile);

                var downloaded = await DownloadFileIfChangedAsync(new Uri(destinationRawFile.Value),
                    new Uri(Path.Combine(BaseFileDirectory, destinationRawFile.Key)));

                if (!downloaded)
                {
                    _logger?.LogError("Unable to download [{destinationRawFile}] to [{localPath}]", destinationRawFile, localPath);
                    if(Directory.Exists(BaseFileDirectory))
                    {
                        _logger?.LogTrace("Listing directory contents for [{BaseFileDirectory}]", BaseFileDirectory);
                        foreach(var file in Directory.GetFiles(BaseFileDirectory))
                        {
                            _logger?.LogTrace("File: {file}", file);
                        }
                    }
                    else
                    {
                        _logger?.LogError("Directory does not exist: [{BaseFileDirectory}]", BaseFileDirectory);
                    }
                }
            }

            var data = await File.ReadAllTextAsync(localPath);

            return data;
        }

        /// <summary>
        /// Check if the local file exists and the cache matches
        /// </summary>
        /// <param name="sourceUri"></param>
        /// <param name="destinationUri"></param>
        /// <returns></returns>
        internal async Task<bool> DownloadFileIfChangedAsync(Uri sourceUri, Uri destinationUri)
        {
            using HttpClient httpClient = new();

            HttpRequestMessage request =
               new(HttpMethod.Head,
                  sourceUri);

            HttpResponseMessage response;

            try
            {
                response = await httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error downloading {sourceUri} to {destinationUri}", sourceUri, destinationUri);
                return false;
            }

            // Grab the cache info and the files last modified date.
            var eTagCacheData = await GetCacheDataAsync();
            var eTag = eTagCacheData
                .Where(e => e.Filename == destinationUri.LocalPath)
                .FirstOrDefault();

            if (eTag != null)
                _logger?.LogTrace("etag for this file is {eTag}", eTag);
            else
                _logger?.LogTrace("No etag found for {destinationUri.LocalPath}", destinationUri.LocalPath);

            DateTime lastModified = DateTime.UtcNow;

            if (File.Exists(destinationUri.LocalPath))
                lastModified = File.GetLastWriteTimeUtc(destinationUri.LocalPath);

            // Check if we need to download it or not.
            if (eTag != null && // If there is an etag
                response.Headers.ETag.Tag == eTag.ETag && // and they match
                eTag.LastModified == lastModified) // and the last modified match
                return true; // Then we don't need to download it.

            var downloadResponse = await DownloadFileAsync(sourceUri, destinationUri);

            // If the download was successful, save the etag.
            if (downloadResponse)
            {
                _logger?.LogTrace("Successfully downloaded {sourceUri} to {destinationUri.LocalPath}", sourceUri, destinationUri.LocalPath);
                lastModified = File.GetLastWriteTimeUtc(destinationUri.LocalPath);
                await UpdateCacheDataAsync(destinationUri.LocalPath, response.Headers.ETag.Tag, lastModified);
            }
            else
            {
                _logger?.LogError("Failure downloading file.");
            }

            return downloadResponse;
        }

        internal async Task<bool> DownloadFileAsync(Uri sourceUri, Uri destinationUri)
        {
            using HttpClient client = new();

            try
            {
                var baseDirectory = new Uri(destinationUri, ".");
                if (!Directory.Exists(baseDirectory.OriginalString))
                    Directory.CreateDirectory(baseDirectory.OriginalString);

                using var s = await client.GetStreamAsync(sourceUri);

                if (File.Exists(destinationUri.LocalPath))
                    File.Delete(destinationUri.LocalPath);

                using var fs = new FileStream(destinationUri.LocalPath, FileMode.CreateNew);
                await s.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unable to DownloadFileAsync [{sourceUri}] to [{destinationUri}]", sourceUri, destinationUri);
                return false;
            }
            return true;
        }

        #region eTag Cache

        private readonly string _etagCacheDataFile = "FileDownloadCache.json";
        protected List<FileETag> _eTagCacheData = new();

        /// <summary>
        /// Update the cache with an entry
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="eTag"></param>
        internal async Task UpdateCacheDataAsync(string filename, string eTag, DateTime lastModified)
        {
            var eTagCacheData = await GetCacheDataAsync();

            var existing = eTagCacheData.Where(e => e.Filename == filename).FirstOrDefault();

            if (existing != null)
                existing.ETag = eTag;
            else
            {
                eTagCacheData.Add(new FileETag()
                {
                    Filename = filename,
                    ETag = eTag,
                    LastModified = lastModified
                });
            }

            await SaveCacheDataAsync(eTagCacheData);
        }

        /// <summary>
        /// Load the cached etag data from file
        /// </summary>
        internal async Task<List<FileETag>> GetCacheDataAsync(bool force = false)
        {
            if (!force && _eTagCacheData.Count > 0)
                return _eTagCacheData;

            var results = new List<FileETag>();
            var cacheDataFile = Path.Combine(BaseFileDirectory, _etagCacheDataFile);

            if (File.Exists(cacheDataFile))
            {
                var data = await File.ReadAllTextAsync(cacheDataFile);

                var deserialised = JsonConvert.DeserializeObject<List<FileETag>>(data);

                if (deserialised != null)
                    results = deserialised;
            }

            return results;
        }

        /// <summary>
        /// Save the cached etag data to file
        /// </summary>
        internal async Task SaveCacheDataAsync(List<FileETag> data)
        {
            var baseDirectory = new Uri(BaseFileDirectory);
            if (!Directory.Exists(baseDirectory.OriginalString))
                Directory.CreateDirectory(baseDirectory.OriginalString);

            var cacheDataFile = Path.Combine(BaseFileDirectory, _etagCacheDataFile);
            var dataString = JsonConvert.SerializeObject(data, Formatting.Indented);

            await File.WriteAllTextAsync(cacheDataFile, dataString);
        }

        #endregion
    }
}
