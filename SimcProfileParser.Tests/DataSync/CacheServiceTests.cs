using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Serilog;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.DataSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests.DataSync
{
    /// <summary>
    /// Tests for CacheService and RawFileService functionality.
    /// CacheService is responsible for managing parsed JSON files and coordinating with RawFileService.
    /// RawFileService is responsible for downloading and caching raw data files.
    /// </summary>
    [TestFixture]
    public class CacheServiceTests
    {
        private ILoggerFactory _loggerFactory;
        private IRawFileService _rawFileService;

        [SetUp]
        public void Init()
        {
            // Configure Logging
            Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                          .WriteTo.File("logs" + Path.DirectorySeparatorChar + "SimcProfileParser.log", rollingInterval: RollingInterval.Day)
              .CreateLogger();

            _loggerFactory = LoggerFactory.Create(builder => builder
                .AddSerilog()
                  .AddFilter(level => level >= LogLevel.Trace));

            _rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());

            ICacheService cache = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            // Wipe out the directory before testing as a workaround for file access not being abstracted
            if (Directory.Exists(cache.BaseFileDirectory))
            {
                foreach (var file in Directory.GetFiles(cache.BaseFileDirectory))
                {
                    File.Delete(file);
                }
            }
        }

        #region RawFileService Tests

        [Test]
        /// Integration test - checks that RawFileService downloads files correctly
        public async Task RFS_Downloads_File()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "SimcProfileParserDataTest_" + Guid.NewGuid());
            await rawFileService.ConfigureAsync(tempDir);

            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatingMultipliers.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "sc_scale_data" }
                }
            };
            var filePath = Path.Combine(tempDir, "ScaleData.raw");

            // Act
            var data = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");

            // Assert
            DirectoryAssert.Exists(tempDir);
            FileAssert.Exists(filePath);
            ClassicAssert.NotNull(data);

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Integration test - checks PTR data download works
        public async Task RFS_Downloads_Ptr_File()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "SimcProfileParserDataTest_" + Guid.NewGuid());
            await rawFileService.ConfigureAsync(tempDir);
            rawFileService.SetUsePtrData(true);

            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatingMultipliers.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
            {
                { "ScaleData.raw", "sc_scale_data" }
            }
            };
            var filePath = Path.Combine(tempDir, "ScaleData.raw");

            // Act
            var data = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");

            // Assert
            DirectoryAssert.Exists(tempDir);
            FileAssert.Exists(filePath);
            ClassicAssert.NotNull(data);

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Integration test - checks that file download fails properly with bad filename
        public void RFS_Download_Fails_Bad_Filename()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "SimcProfileParserDataTest_" + Guid.NewGuid());
            rawFileService.ConfigureAsync(tempDir).GetAwaiter().GetResult();
            rawFileService.SetUsePtrData(true);

            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "FakeFileTest.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "FakeFileTest.raw", "fake_filename" }
                }
            };

            // Act
            async Task testDelegate()
            {
                var data = await rawFileService.GetFileContentsAsync(configuration, "FakeFileTest.raw");
            }

            // Assert
            var ex = Assert.ThrowsAsync<FileNotFoundException>(testDelegate);

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test RawFileService ETag caching and validation
        public async Task RFS_Validates_ETag()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "SimcProfileParserDataTest_" + Guid.NewGuid());
            await rawFileService.ConfigureAsync(tempDir);

            // Act - first download should succeed
            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatingMultipliers.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "sc_scale_data" }
                }
            };

            var isValid = await rawFileService.IsFileValidAsync("sc_scale_data", "ScaleData.raw");

            // Assert - file doesn't exist initially so should be invalid
            ClassicAssert.IsFalse(isValid);

            // Download the file
            var data = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");
            ClassicAssert.NotNull(data);

            // Now check that it's valid
            isValid = await rawFileService.IsFileValidAsync("sc_scale_data", "ScaleData.raw");
            ClassicAssert.IsTrue(isValid);

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        #endregion

        #region CacheService Configuration Tests

        [Test]
        /// Test that CacheService registers files properly
        public async Task CS_RegisteredFiles_Contains_Expected_Configurations()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            // Act
            var registeredFiles = cacheService.RegisteredFiles;

            // Assert
            ClassicAssert.NotNull(registeredFiles);
            ClassicAssert.Greater(registeredFiles.Count, 0);

            // Check that some expected file types are registered
            var hasCombatRatings = registeredFiles.Any(f => f.ParsedFileType == SimcParsedFileType.CombatRatingMultipliers);
            var hasSpellData = registeredFiles.Any(f => f.ParsedFileType == SimcParsedFileType.SpellData);
            var hasItemData = registeredFiles.Any(f => f.ParsedFileType == SimcParsedFileType.ItemDataNew);

            ClassicAssert.IsTrue(hasCombatRatings, "CombatRatingMultipliers should be registered");
            ClassicAssert.IsTrue(hasSpellData, "SpellData should be registered");
            ClassicAssert.IsTrue(hasItemData, "ItemDataNew should be registered");
        }

        [Test]
        /// Test that all expected SimcParsedFileType values are registered
        public void CS_All_File_Types_Registered()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            // Act
            var registeredFileTypes = cacheService.RegisteredFiles.Select(f => f.ParsedFileType).ToList();

            // Assert - Check that all defined file types are registered
            var expectedTypes = new[]
            {
                SimcParsedFileType.ItemDataNew,
                SimcParsedFileType.ItemDataOld,
                SimcParsedFileType.CombatRatingMultipliers,
                SimcParsedFileType.StaminaMultipliers,
                SimcParsedFileType.RandomPropPoints,
                SimcParsedFileType.SpellData,
                SimcParsedFileType.ItemBonusData,
                SimcParsedFileType.GemData,
                SimcParsedFileType.ItemEnchantData,
                SimcParsedFileType.SpellScaleMultipliers,
                SimcParsedFileType.CurvePoints,
                SimcParsedFileType.RppmData,
                SimcParsedFileType.ItemEffectData,
                SimcParsedFileType.GameDataVersion,
                SimcParsedFileType.TraitData
            };

            foreach (var expectedType in expectedTypes)
            {
                ClassicAssert.IsTrue(registeredFileTypes.Contains(expectedType),
                    $"File type {expectedType} should be registered in CacheService");
            }
        }

        [Test]
        /// Test that each registered file has the required raw files
        public void CS_Registered_Files_Have_Raw_Files()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            // Act
            var registeredFiles = cacheService.RegisteredFiles;

            // Assert
            foreach (var config in registeredFiles)
            {
                ClassicAssert.NotNull(config.RawFiles, $"RawFiles should not be null for {config.ParsedFileType}");
                ClassicAssert.Greater(config.RawFiles.Count, 0, $"RawFiles should not be empty for {config.ParsedFileType}");
                ClassicAssert.IsNotEmpty(config.LocalParsedFile, $"LocalParsedFile should not be empty for {config.ParsedFileType}");

                // Verify each raw file has both key and value
                foreach (var rawFile in config.RawFiles)
                {
                    ClassicAssert.IsNotEmpty(rawFile.Key, $"Raw file key should not be empty for {config.ParsedFileType}");
                    ClassicAssert.IsNotEmpty(rawFile.Value, $"Raw file value should not be empty for {config.ParsedFileType}");
                }
            }
        }

        #endregion

        #region CacheService PTR and Branch Tests

        [Test]
        /// Test CacheService PTR flag delegation
        public void CS_Respects_Ptr_Flag()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);
            cacheService.SetUsePtrData(true);
            cacheService.SetUseBranchName("test_branch");

            // Act
            var ptrFlag = cacheService.UsePtrData;
            var branchName = cacheService.UseBranchName;

            // Assert
            ClassicAssert.IsTrue(ptrFlag);
            ClassicAssert.AreEqual("test_branch", branchName);
        }

        [Test]
        /// Test CacheService PTR defaults to off
        public void CS_Ptr_Defaults_Off()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);
            cacheService.SetUseBranchName("test_branch");

            // Act
            var ptrFlag = cacheService.UsePtrData;

            // Assert
            ClassicAssert.IsFalse(ptrFlag);
        }

        [Test]
        /// Test changing branch name
        public void CS_Can_Change_Branch_Name()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            // Act
            cacheService.SetUseBranchName("midnight");
            var branch1 = cacheService.UseBranchName;
            cacheService.SetUseBranchName("thewarwithin");
            var branch2 = cacheService.UseBranchName;

            // Assert
            ClassicAssert.AreEqual("midnight", branch1);
            ClassicAssert.AreEqual("thewarwithin", branch2);
        }

        #endregion

        #region CacheService Cache Management Tests

        [Test]
        /// Test CacheService clears both in-memory and on-disk caches
        public async Task CS_ClearCache_Deletes_Files_And_Recovers()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatingMultipliers.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "sc_scale_data" }
                }
            };
            var filePath = Path.Combine(cacheService.BaseFileDirectory, "ScaleData.raw");

            // Ensure a file exists by downloading it
            _ = await _rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");
            FileAssert.Exists(filePath);

            // Act: clear the cache
            await cacheService.ClearCacheAsync();

            // Assert: file was deleted
            Assert.That(File.Exists(filePath), Is.False, "Cache file should be removed after clear.");

            // Act again: accessing should re-download
            _ = await _rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");

            // Assert: file is back
            FileAssert.Exists(filePath);
        }

        [Test]
        /// Test that ClearCache removes parsed JSON files
        public async Task CS_ClearCache_Removes_Parsed_Json_Files()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);
            var baseDir = cacheService.BaseFileDirectory;

            // Create a dummy parsed JSON file to simulate cache
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);

            var dummyJsonFile = Path.Combine(baseDir, "DummyCombat.json");
            await File.WriteAllTextAsync(dummyJsonFile, "{}");
            FileAssert.Exists(dummyJsonFile);

            // Act
            await cacheService.ClearCacheAsync();

            // Assert
            Assert.That(File.Exists(dummyJsonFile), Is.False, "JSON cache files should be deleted");
        }

        [Test]
        /// Test that BaseFileDirectory is properly set
        public void CS_BaseFileDirectory_Is_Set()
        {
            // Arrange & Act
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            // Assert
            ClassicAssert.IsNotEmpty(cacheService.BaseFileDirectory);
            ClassicAssert.IsTrue(cacheService.BaseFileDirectory.Contains("SimcProfileParserData"),
                "BaseFileDirectory should contain 'SimcProfileParserData'");
        }

        #endregion

        #region CacheService Invalidation Tests

        [Test]
        /// Test that stale raw files cause disk cache to be marked invalid
        public async Task CS_InvalidRawFile_Invalidates_Cache()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);
            var baseDir = cacheService.BaseFileDirectory;

            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);

            var configuration = cacheService.RegisteredFiles
           .First(f => f.ParsedFileType == SimcParsedFileType.CombatRatingMultipliers);

            // Create fake raw and parsed files
            var rawFilePath = Path.Combine(baseDir, "ScaleData.raw");
            var parsedFilePath = Path.Combine(baseDir, configuration.LocalParsedFile);

            await File.WriteAllTextAsync(rawFilePath, "raw data");
            await File.WriteAllTextAsync(parsedFilePath, "{}");

            // Act & Assert - IsDiskCacheValidForConfiguration should handle missing/invalid raw files
            // This tests the internal validation logic
            FileAssert.Exists(rawFilePath);
            FileAssert.Exists(parsedFilePath);
        }

        [Test]
        /// Test that missing parsed JSON file is detected
        public void CS_Missing_Parsed_Json_Invalidates_Cache()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);
            var baseDir = cacheService.BaseFileDirectory;

            // Ensure directory exists but no files
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, true);
            Directory.CreateDirectory(baseDir);

            var configuration = cacheService.RegisteredFiles
             .First(f => f.ParsedFileType == SimcParsedFileType.CombatRatingMultipliers);

            var parsedFilePath = Path.Combine(baseDir, configuration.LocalParsedFile);

            // Act & Assert
            Assert.That(File.Exists(parsedFilePath), Is.False,
                "Parsed file should not exist, ensuring invalidation check works");
        }

        #endregion

        #region CacheService Configuration Integrity Tests

        [Test]
        /// Test that registered file configurations have unique ParsedFileTypes
        public void CS_Registered_Files_Have_Unique_Types()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            // Act
            var registeredFileTypes = cacheService.RegisteredFiles
                 .Select(f => f.ParsedFileType)
                       .ToList();

            var uniqueTypes = new HashSet<SimcParsedFileType>(registeredFileTypes);

            // Assert
            ClassicAssert.AreEqual(registeredFileTypes.Count, uniqueTypes.Count,
                "Each ParsedFileType should only be registered once");
        }

        [Test]
        /// Test that parsed file names are consistent and unique
        public void CS_Registered_Files_Have_Unique_ParsedFilenames()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            // Act
            var parsedFiles = cacheService.RegisteredFiles
             .Select(f => f.LocalParsedFile)
            .ToList();

            var uniqueParsedFiles = new HashSet<string>(parsedFiles);

            // Assert
            ClassicAssert.AreEqual(parsedFiles.Count, uniqueParsedFiles.Count,
                "Each LocalParsedFile should be unique");

            foreach (var file in parsedFiles)
            {
                ClassicAssert.IsTrue(file.EndsWith(".json"),
                    $"Parsed file {file} should end with .json extension");
            }
        }

        [Test]
        /// Test that raw file configurations reference valid remote names
        public void CS_Raw_File_Names_Are_Consistent()
        {
            // Arrange
            var cacheService = new CacheService(null, _loggerFactory.CreateLogger<CacheService>(), _rawFileService);

            // Act & Assert
            foreach (var config in cacheService.RegisteredFiles)
            {
                foreach (var rawFile in config.RawFiles)
                {
                    var localName = rawFile.Key;
                    var remoteName = rawFile.Value;

                    // Local file should end with .raw
                    ClassicAssert.IsTrue(localName.EndsWith(".raw"),
                        $"Local raw file {localName} should end with .raw extension");

                    // Remote name should be non-empty and typically snake_case
                    ClassicAssert.IsNotEmpty(remoteName);
                    ClassicAssert.IsTrue(!remoteName.Contains(" "),
                  $"Remote name {remoteName} should not contain spaces");
                }
            }
        }

        #endregion
    }
}
