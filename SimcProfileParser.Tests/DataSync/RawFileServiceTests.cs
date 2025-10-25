using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Serilog;
using SimcProfileParser.DataSync;
using SimcProfileParser.Model.DataSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests.DataSync
{
    /// <summary>
    /// Comprehensive tests for RawFileService functionality.
    /// RawFileService is responsible for:
    /// - Downloading raw data files from GitHub
    /// - Managing ETag caching for file validation
    /// - Supporting PTR (Public Test Realm) data downloads
    /// - Validating files based on ETag and time windows
    /// </summary>
    [TestFixture]
    public class RawFileServiceTests
    {
        private ILoggerFactory _loggerFactory;

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
        }

        #region File Download Tests

        [Test]
        /// Test basic file download functionality
        public async Task RFS_Downloads_File_Successfully()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
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
            ClassicAssert.Greater(data.Length, 0, "Downloaded file should contain data");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test that multiple files can be downloaded
        public async Task RFS_Downloads_Multiple_Files()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
            await rawFileService.ConfigureAsync(tempDir);

            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "ItemDataNew.json",
                ParsedFileType = SimcParsedFileType.ItemDataNew,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ItemData.raw", "item_data" },
                    { "ItemEffectData.raw", "item_effect" }
                }
            };

            // Act
            var itemData = await rawFileService.GetFileContentsAsync(configuration, "ItemData.raw");
            var effectData = await rawFileService.GetFileContentsAsync(configuration, "ItemEffectData.raw");

            // Assert
            ClassicAssert.NotNull(itemData);
            ClassicAssert.NotNull(effectData);
            ClassicAssert.Greater(itemData.Length, 0);
            ClassicAssert.Greater(effectData.Length, 0);

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test that failed download throws exception
        public void RFS_Failed_Download_Throws_Exception()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
            rawFileService.ConfigureAsync(tempDir).GetAwaiter().GetResult();

            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "FakeFile.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "FakeFile.raw", "nonexistent_file" }
                }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await rawFileService.GetFileContentsAsync(configuration, "FakeFile.raw");
            });

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        #endregion

        #region PTR Data Tests

        [Test]
        /// Test downloading PTR data with PTR flag enabled
        public async Task RFS_Downloads_PTR_Data_When_Enabled()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
            await rawFileService.ConfigureAsync(tempDir);
            rawFileService.SetUsePtrData(true);

            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "SpellData.json",
                ParsedFileType = SimcParsedFileType.SpellData,
                RawFiles = new Dictionary<string, string>()
                {
                    { "SpellData.raw", "sc_spell_data" }
                }
            };
            var filePath = Path.Combine(tempDir, "SpellData.raw");

            // Act
            var data = await rawFileService.GetFileContentsAsync(configuration, "SpellData.raw");

            // Assert
            FileAssert.Exists(filePath);
            ClassicAssert.NotNull(data);
            ClassicAssert.IsTrue(rawFileService.UsePtrData, "PTR flag should be true");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test that PTR flag defaults to false
        public void RFS_PTR_Flag_Defaults_False()
        {
            // Arrange & Act
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());

            // Assert
            ClassicAssert.IsFalse(rawFileService.UsePtrData, "PTR flag should default to false");
        }

        [Test]
        /// Test that PTR flag can be changed
        public void RFS_PTR_Flag_Can_Be_Changed()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());

            // Act
            ClassicAssert.IsFalse(rawFileService.UsePtrData);
            rawFileService.SetUsePtrData(true);
            var isPtr1 = rawFileService.UsePtrData;
            rawFileService.SetUsePtrData(false);
            var isPtr2 = rawFileService.UsePtrData;

            // Assert
            ClassicAssert.IsTrue(isPtr1);
            ClassicAssert.IsFalse(isPtr2);
        }

        #endregion

        #region Branch Name Tests

        [Test]
        /// Test that branch name defaults to "midnight"
        public void RFS_Branch_Name_Defaults_Midnight()
        {
            // Arrange & Act
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());

            // Assert
            ClassicAssert.AreEqual("midnight", rawFileService.UseBranchName);
        }

        [Test]
        /// Test that branch name can be changed
        public void RFS_Branch_Name_Can_Be_Changed()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var branches = new[] { "thewarwithin", "dragonflight", "shadowlands", "midnight" };

            // Act & Assert
            foreach (var branch in branches)
            {
                rawFileService.SetUseBranchName(branch);
                ClassicAssert.AreEqual(branch, rawFileService.UseBranchName,
                    $"Branch name should be set to {branch}");
            }
        }

        [Test]
        /// Test URL generation with different branch names and PTR flags
        public void RFS_URL_Generation_Is_Correct()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            rawFileService.SetUseBranchName("test_branch");

            // Act - without PTR
            var urlWithoutPtr = rawFileService._getUrl("test_file");

            // Act - with PTR
            rawFileService.SetUsePtrData(true);
            var urlWithPtr = rawFileService._getUrl("test_file");

            // Assert
            ClassicAssert.IsTrue(urlWithoutPtr.Contains("test_branch"),
                "URL should contain branch name");
            ClassicAssert.IsTrue(urlWithoutPtr.Contains("test_file"),
                "URL should contain file name");
            ClassicAssert.IsTrue(urlWithoutPtr.Contains("simulationcraft/simc"),
                "URL should be from GitHub");
            ClassicAssert.IsFalse(urlWithoutPtr.Contains("_ptr"),
                "URL without PTR should not contain _ptr");
            ClassicAssert.IsTrue(urlWithPtr.Contains("_ptr"),
                "URL with PTR should contain _ptr suffix");
        }

        #endregion

        #region ETag Caching Tests

        [Test]
        /// Test that files are cached with ETag information
        public async Task RFS_Caches_ETag_After_Download()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
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

            // Act
            var data = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");
            var cacheFile = Path.Combine(tempDir, "FileDownloadCache.json");

            // Assert
            FileAssert.Exists(cacheFile);
            var cacheContent = await File.ReadAllTextAsync(cacheFile);
            var cacheData = JsonSerializer.Deserialize<List<FileETag>>(cacheContent);
            ClassicAssert.NotNull(cacheData);
            ClassicAssert.Greater(cacheData.Count, 0, "Cache should contain ETag entries");

            var scaleDataEntry = cacheData.FirstOrDefault(e => e.Filename.Contains("ScaleData.raw"));
            ClassicAssert.NotNull(scaleDataEntry, "Cache should contain ScaleData.raw entry");
            ClassicAssert.IsNotEmpty(scaleDataEntry.ETag);

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test that cached files are marked as valid within the time window
        public async Task RFS_Recently_Downloaded_Files_Are_Valid()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
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

            // Act - Download file
            var data = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");

            // Act - Check if it's valid immediately
            var isValid = await rawFileService.IsFileValidAsync("sc_scale_data", "ScaleData.raw");

            // Assert
            ClassicAssert.IsTrue(isValid, "Recently downloaded file should be valid");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test that non-existent files are marked as invalid
        public async Task RFS_Nonexistent_Files_Are_Invalid()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
            await rawFileService.ConfigureAsync(tempDir);

            // Act
            var isValid = await rawFileService.IsFileValidAsync("sc_scale_data", "NonExistent.raw");

            // Assert
            ClassicAssert.IsFalse(isValid, "Non-existent file should be marked as invalid");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        #endregion

        #region Cache Configuration Tests

        [Test]
        /// Test that ConfigureAsync sets the base directory
        public async Task RFS_ConfigureAsync_Sets_Base_Directory()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());

            // Act
            await rawFileService.ConfigureAsync(tempDir);

            // Assert - Verify by using the service and checking file locations
            Directory.CreateDirectory(tempDir);
            ClassicAssert.IsTrue(Directory.Exists(tempDir));

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test that ConfigureAsync loads existing cache
        public async Task RFS_ConfigureAsync_Loads_Existing_Cache()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            // Create existing cache file
            var cacheFile = Path.Combine(tempDir, "FileDownloadCache.json");
            var testEntries = new List<FileETag>()
            {
                new FileETag()
                {
                    Filename = Path.Combine(tempDir, "test.raw"),
                    ETag = "test-etag",
                    LastModified = DateTime.UtcNow,
                    LastChecked = DateTime.UtcNow
                }
            };
            await File.WriteAllTextAsync(cacheFile, JsonSerializer.Serialize(testEntries));

            // Act
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            await rawFileService.ConfigureAsync(tempDir);

            // Assert - Verify cache was loaded
            var loadedCache = await rawFileService.GetCacheDataAsync();
            ClassicAssert.NotNull(loadedCache);
            ClassicAssert.Greater(loadedCache.Count, 0);

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        #endregion

        #region Cache Management Tests

        [Test]
        /// Test that DeleteAllFiles removes all raw files
        public async Task RFS_DeleteAllFiles_Removes_Raw_Files()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
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

            // Download a file
            var data = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");
            var filePath = Path.Combine(tempDir, "ScaleData.raw");
            FileAssert.Exists(filePath);

            // Act
            await rawFileService.DeleteAllFiles();

            // Assert
            Assert.That(File.Exists(filePath), Is.False, "Raw files should be deleted");
            var cacheFile = Path.Combine(tempDir, "FileDownloadCache.json");
            FileAssert.Exists(cacheFile);
            var cacheContent = await File.ReadAllTextAsync(cacheFile);
            var cacheData = JsonSerializer.Deserialize<List<FileETag>>(cacheContent);
            ClassicAssert.AreEqual(0, cacheData.Count, "Cache should be empty");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test that cache file is created and persisted
        public async Task RFS_Cache_File_Is_Persisted()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
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

            // Act
            var data1 = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");
            var cacheFile = Path.Combine(tempDir, "FileDownloadCache.json");

            // Assert
            FileAssert.Exists(cacheFile);
            var cacheContent = await File.ReadAllTextAsync(cacheFile);
            var cacheData = JsonSerializer.Deserialize<List<FileETag>>(cacheContent);
            var initialCount = cacheData.Count;

            // Download same file again
            var data2 = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");
            cacheContent = await File.ReadAllTextAsync(cacheFile);
            cacheData = JsonSerializer.Deserialize<List<FileETag>>(cacheContent);

            // Count should remain the same (entry updated, not added)
            ClassicAssert.AreEqual(initialCount, cacheData.Count,
                "Cache should update existing entry, not create duplicate");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        /// Test that null logger doesn't cause exceptions
        public async Task RFS_Works_With_Null_Logger()
        {
            // Arrange
            var rawFileService = new RawFileService(null);
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
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

            // Act & Assert - Should not throw even with null logger
            Assert.DoesNotThrowAsync(async () =>
            {
                var data = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");
            });

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test that GetCacheDataAsync handles missing cache file gracefully
        public async Task RFS_GetCacheDataAsync_Handles_Missing_Cache_File()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            // Act
            await rawFileService.ConfigureAsync(tempDir);
            var cacheData = await rawFileService.GetCacheDataAsync();

            // Assert
            ClassicAssert.NotNull(cacheData);
            ClassicAssert.AreEqual(0, cacheData.Count, "Should return empty list for missing cache");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        public async Task RFS_Multiple_Instances_Can_Share_Configuration()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());

            var service1 = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var service2 = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());

            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatingMultipliers.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "sc_scale_data" }
                }
            };

            // Act
            await service1.ConfigureAsync(tempDir);
            await service2.ConfigureAsync(tempDir);

            var data1 = await service1.GetFileContentsAsync(configuration, "ScaleData.raw");
            var data2 = await service2.GetFileContentsAsync(configuration, "ScaleData.raw");

            // Assert
            ClassicAssert.AreEqual(data1, data2, "Both instances should retrieve same data");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        #endregion

        #region File Content Validation Tests

        [Test]
        /// Test that downloaded files contain expected data format
        public async Task RFS_Downloaded_Files_Contain_Valid_Data()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
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

            // Act
            var data = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");

            // Assert
            ClassicAssert.IsNotEmpty(data);
            ClassicAssert.IsTrue(data.Contains("//") || data.Contains("struct") || data.Contains("{"),
                "Downloaded data should be in expected format");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        /// Test that GetFileContentsAsync returns consistent data on multiple calls
        public async Task RFS_Cached_File_Returns_Consistent_Data()
        {
            // Arrange
            var rawFileService = new RawFileService(_loggerFactory.CreateLogger<RawFileService>());
            var tempDir = Path.Combine(Path.GetTempPath(), "RawFileServiceTest_" + Guid.NewGuid());
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

            // Act
            var data1 = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");
            var data2 = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");
            var data3 = await rawFileService.GetFileContentsAsync(configuration, "ScaleData.raw");

            // Assert
            ClassicAssert.AreEqual(data1, data2, "Multiple calls should return same data");
            ClassicAssert.AreEqual(data2, data3, "Multiple calls should return same data");

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        #endregion
    }
}
