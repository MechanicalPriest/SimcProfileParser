using Microsoft.Extensions.Logging;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.DataSync;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimcProfileParser.DataSync
{

    internal class CacheService : ICacheService
    {
        public string BaseFileDirectory { get; }

        protected List<CacheFileConfiguration> _registeredFiles = [];
        protected Dictionary<SimcParsedFileType, object> _cachedFileData = [];
        protected readonly IRawDataExtractionService _rawDataExtractionService;
        protected readonly ILogger<CacheService> _logger;
        private readonly IRawFileService rawFileService;

        private readonly string parsedDataFileExtension = "json";

        /// <summary>
        /// Stores the <see cref="CacheFileConfiguration.ParserVersion"/> each parsed file was generated with,
        /// in the form of Local parsed file : Parser version
        /// </summary>
        internal const string ParsedFileVersionsFileName = "ParsedFileVersions.json";
        private readonly System.Threading.SemaphoreSlim _parsedFileVersionsLock = new(1, 1);
        public IReadOnlyCollection<CacheFileConfiguration> RegisteredFiles => new ReadOnlyCollection<CacheFileConfiguration>(_registeredFiles);

        public CacheService(IRawDataExtractionService rawDataExtractionService,
            ILogger<CacheService> logger,
            IRawFileService rawFileService)
        {
            BaseFileDirectory = Path.Combine(
                Path.GetTempPath(), "SimcProfileParserData");

            _rawDataExtractionService = rawDataExtractionService;
            _logger = logger;
            this.rawFileService = rawFileService;
            this.rawFileService.ConfigureAsync(BaseFileDirectory).GetAwaiter().GetResult();

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemDataNew.json",
                ParsedFileType = SimcParsedFileType.ItemDataNew,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemData.raw", "item_data" },
                    { "ItemEffectData.raw", "item_effect" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemDataOld.json",
                ParsedFileType = SimcParsedFileType.ItemDataOld,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemData.raw", "item_data" },
                    { "ItemEffectData.raw", "item_effect" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatingMultipliers.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "sc_scale_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "StaminaMultipliers.json",
                ParsedFileType = SimcParsedFileType.StaminaMultipliers,
                // 2: fixed multipliers of 10 or more not being parsed
                ParserVersion = 2,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "sc_scale_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "RandomPropPoints.json",
                ParsedFileType = SimcParsedFileType.RandomPropPoints,
                RawFiles = new Dictionary<string, string>()
                {
                    { "RandomPropPoints.raw", "rand_prop_points" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "SpellData.json",
                ParsedFileType = SimcParsedFileType.SpellData,
                // 2: spell labels
                ParserVersion = 2,
                RawFiles = new Dictionary<string, string>()
                {
                    { "SpellData.raw", "sc_spell_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemBonusData.json",
                ParsedFileType = SimcParsedFileType.ItemBonusData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemBonusData.raw", "item_bonus" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "GemData.json",
                ParsedFileType = SimcParsedFileType.GemData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "GemData.raw", "gem_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemEnchantData.json",
                ParsedFileType = SimcParsedFileType.ItemEnchantData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemEnchantData.raw", "spell_item_enchantment" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "SpellScalingMultipliers.json",
                ParsedFileType = SimcParsedFileType.SpellScaleMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "sc_scale_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "CurveData.json",
                ParsedFileType = SimcParsedFileType.CurvePoints,
                RawFiles = new Dictionary<string, string>()
                {
                    { "CurveData.raw", "item_scaling" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "RppmData.json",
                ParsedFileType = SimcParsedFileType.RppmData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "RppmData.raw", "real_ppm_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemEffectData.json",
                ParsedFileType = SimcParsedFileType.ItemEffectData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemEffectData.raw", "item_effect" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "GameDataVersion.json",
                ParsedFileType = SimcParsedFileType.GameDataVersion,
                RawFiles = new Dictionary<string, string>()
                {
                    { "GameDataVersion.raw", "client_data_version" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "TraitData.json",
                ParsedFileType = SimcParsedFileType.TraitData,
                // 2: fixed parsing of the current trait_data_t layout, added trait definition effects
                ParserVersion = 2,
                RawFiles = new Dictionary<string, string>()
                {
                    { "TraitData.raw", "trait_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemScalingConfigData.json",
                ParsedFileType = SimcParsedFileType.ItemScalingConfigData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "CurveData.raw", "item_scaling" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemOffsetCurveData.json",
                ParsedFileType = SimcParsedFileType.ItemOffsetCurveData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "CurveData.raw", "item_scaling" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ContentTuningData.json",
                ParsedFileType = SimcParsedFileType.ContentTuningData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ContentTuning.raw", "content_tuning" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "ClassBaseStats.json",
                ParsedFileType = SimcParsedFileType.ClassBaseStats,
                RawFiles = new Dictionary<string, string>()
                {
                    // sc_extra_data.inc isn't generated, so lives in engine/dbc/ rather than engine/dbc/generated/
                    { "ExtraData.raw", "dbc/sc_extra_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "RaceBaseStats.json",
                ParsedFileType = SimcParsedFileType.RaceBaseStats,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ExtraData.raw", "dbc/sc_extra_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "BaseMana.json",
                ParsedFileType = SimcParsedFileType.BaseMana,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "sc_scale_data" }
                }
            });

            RegisterFileConfiguration(new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatings.json",
                ParsedFileType = SimcParsedFileType.CombatRatings,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "sc_scale_data" }
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
            var configuration = _registeredFiles.FirstOrDefault(f => f.ParsedFileType == fileType)
                ?? throw new ArgumentOutOfRangeException(nameof(fileType), "Supplied fileType has not been registered.");

            // First check if we already have the data loaded:
            if (_cachedFileData.TryGetValue(fileType, out object cachedData))
            {

                if (!await IsDiskCacheValidForConfiguration(configuration))
                {
                    // Anytime a raw file is invalidated, the raw file and parsed json file should be removed,
                    //  and the file re - obtained
                    await DeleteDiskCacheForConfiguration(configuration);

                    return await GetParsedFileContents<T>(configuration);
                }

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

            return await GetParsedFileContents<T>(configuration);
        }

        /// <summary>
        /// For a given configuration, parse and load the .json file contents into T
        /// </summary>
        /// <returns></returns>
        async Task<T> GetParsedFileContents<T>(CacheFileConfiguration configuration)
        {
            var localPath = Path.Combine(BaseFileDirectory, configuration.LocalParsedFile);

            // If the file doesn't exist, generate it.
            if (!File.Exists(localPath))
            {
                _logger?.LogTrace("File [{localPath}] does not exist, generating it...", localPath);
                await GenerateParsedFileAsync(configuration.ParsedFileType);
            }
            // If it was generated by an older version of the parser, regenerate it from the cached raw files.
            else if (await GetParsedFileVersionAsync(configuration.LocalParsedFile) != configuration.ParserVersion)
            {
                _logger?.LogTrace("File [{localPath}] was generated by an older parser version, regenerating it...", localPath);
                await GenerateParsedFileAsync(configuration.ParsedFileType);
            }

            var fileText = await File.ReadAllTextAsync(localPath);

            var deserialisedData = JsonSerializer.Deserialize<T>(fileText)
                ?? throw new InvalidDataException($"Failed to deserialize {configuration.LocalParsedFile} to {typeof(T).Name}.");

            _cachedFileData[configuration.ParsedFileType] = deserialisedData;

            return deserialisedData;
        }

        async Task<bool> IsDiskCacheValidForConfiguration(CacheFileConfiguration configuration)
        {
            // First, check that all the raw files exist
            foreach (var rawFile in configuration.RawFiles)
            {
                var remoteFileName = rawFile.Value;
                var localFileName = rawFile.Key;

                var isLocalFileValid = await rawFileService.IsFileValidAsync(remoteFileName, localFileName);
                if(!isLocalFileValid)
                {
                    return false;
                }
            }

            // Next, check that the parsed .json file exists
            var localParsedFilePath = Path.Combine(BaseFileDirectory, configuration.LocalParsedFile);
            if (!File.Exists(localParsedFilePath))
            {
                return false;
            }

            return true;
        }

        async Task DeleteDiskCacheForConfiguration(CacheFileConfiguration configuration)
        {
            // Clear the cached data for this file type
            _cachedFileData.Remove(configuration.ParsedFileType);

            // Now delete both the parsed .json file and the raw files
            var localParsedFilePath = Path.Combine(BaseFileDirectory, configuration.LocalParsedFile);
            if (File.Exists(localParsedFilePath))
            {
                File.Delete(localParsedFilePath);
            }
            foreach (var rawFile in configuration.RawFiles)
            {
                var localRawFilePath = Path.Combine(BaseFileDirectory, rawFile.Key);
                if (File.Exists(localRawFilePath))
                {
                    File.Delete(localRawFilePath);
                }
            }
        }

        /// <summary>
        /// Generates a parsed .json file for the specified configuration by calling the RawDataExtractionService
        /// </summary>
        /// <param name="fileType">Type of file to generate data for</param>
        async Task GenerateParsedFileAsync(SimcParsedFileType fileType)
        {
            var configuration = _registeredFiles.Where(f => f.ParsedFileType == fileType).FirstOrDefault() 
                ?? throw new ArgumentOutOfRangeException(nameof(fileType), "Supplied fileType has not been registered.");

            // Gather together all the raw data the extraction service needs to run its process
            var rawData = new Dictionary<string, string>();
            foreach (var rawFile in configuration.RawFiles)
            {
                var data = await rawFileService.GetFileContentsAsync(configuration, rawFile.Key);
                rawData.Add(rawFile.Key, data);
            }

            // Generate the parsed .json file
            var parsedData = _rawDataExtractionService.GenerateData(configuration.ParsedFileType, rawData);
            var localPath = Path.Combine(BaseFileDirectory, configuration.LocalParsedFile);

            _logger?.LogTrace("Saving parsed json data for [{configuration.ParsedFileType}] to [{localPath}]", configuration.ParsedFileType, localPath);
            await File.WriteAllTextAsync(localPath, JsonSerializer.Serialize(parsedData));
            await SetParsedFileVersionAsync(configuration.LocalParsedFile, configuration.ParserVersion);
        }

        /// <summary>
        /// Get the parser version a parsed file was generated with.
        /// Files with no recorded version were generated before versioning was added, so are treated as version 1.
        /// </summary>
        async Task<int> GetParsedFileVersionAsync(string localParsedFile)
        {
            await _parsedFileVersionsLock.WaitAsync();
            try
            {
                var versions = await ReadParsedFileVersionsAsync();
                return versions.TryGetValue(localParsedFile, out var version) ? version : 1;
            }
            finally
            {
                _parsedFileVersionsLock.Release();
            }
        }

        async Task SetParsedFileVersionAsync(string localParsedFile, int version)
        {
            await _parsedFileVersionsLock.WaitAsync();
            try
            {
                var versions = await ReadParsedFileVersionsAsync();
                versions[localParsedFile] = version;
                await File.WriteAllTextAsync(Path.Combine(BaseFileDirectory, ParsedFileVersionsFileName),
                    JsonSerializer.Serialize(versions));
            }
            finally
            {
                _parsedFileVersionsLock.Release();
            }
        }

        async Task<Dictionary<string, int>> ReadParsedFileVersionsAsync()
        {
            var path = Path.Combine(BaseFileDirectory, ParsedFileVersionsFileName);
            if (!File.Exists(path))
                return [];

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, int>>(await File.ReadAllTextAsync(path)) ?? [];
            }
            catch (JsonException ex)
            {
                // A corrupt versions file just means every parsed file gets treated as version 1
                _logger?.LogWarning(ex, "Unable to read {path}, ignoring it.", path);
                return [];
            }
        }

        void RegisterFileConfiguration(CacheFileConfiguration configuration)
        {
            var exists = _registeredFiles
                .FirstOrDefault(f => f.ParsedFileType == configuration.ParsedFileType);

            if (exists != null)
            {
                _registeredFiles.Remove(exists);
            }

            _registeredFiles.Add(configuration);
        }

        /// <summary>
        /// Get the flag used PTR data for data extraction
        /// </summary>
        public bool UsePtrData { get => rawFileService.UsePtrData; }
        public void SetUsePtrData(bool usePtrData) => rawFileService.SetUsePtrData(usePtrData);

        /// <summary>
        /// Get the github branch name used for data extraction
        /// </summary>
        public string UseBranchName { get => rawFileService.UseBranchName; }
        public void SetUseBranchName(string branchName) => rawFileService.SetUseBranchName(branchName);

        public async Task ClearCacheAsync()
        {
            // Clear in-memory caches
            _cachedFileData.Clear();

            // Clear on-disk cache
            if (Directory.Exists(BaseFileDirectory))
            {
                foreach (var file in Directory.GetFiles(BaseFileDirectory, $"*.{parsedDataFileExtension}"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to delete cache file {file}", file);
                    }
                }
            }

            // Clear raw file cache
            await rawFileService.DeleteAllFiles();
        }
    }
}
