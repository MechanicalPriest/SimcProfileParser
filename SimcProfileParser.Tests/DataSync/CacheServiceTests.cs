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
    /// TODO: Create better tests for the CacheService by abstracting the File/Web access components.
    /// Then test that this service does what it should, and seperately test the file/web work 
    /// just once instead of for each overall test - unit-testify it rather than integration test.
    /// </summary>
    [TestFixture]
    public class CacheServiceTests
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

            ICacheService cache = new CacheService(null, _loggerFactory.CreateLogger<CacheService>());

            // Wipe out the directory before testing as a workaround for file access not being abstracted
            if (Directory.Exists(cache.BaseFileDirectory))
            {
                foreach (var file in Directory.GetFiles(cache.BaseFileDirectory))
                {
                    File.Delete(file);
                }
            }
        }

        [Test]
        /// Integration test of sorts. Checking the file download works.
        public async Task CS_Downloads_File()
        {
            // Arrange
            CacheService cache = new CacheService(null, _loggerFactory.CreateLogger<CacheService>());

            var configuration = new CacheFileConfiguration()
            {
                LocalParsedFile = "CombatRatingMultipliers.json",
                ParsedFileType = SimcParsedFileType.CombatRatingMultipliers,
                RawFiles = new Dictionary<string, string>()
                {
                    { "ScaleData.raw", "https://raw.githubusercontent.com/simulationcraft/simc/thewarwithin/engine/dbc/generated/sc_scale_data.inc" }
                }
            };
            var filePath = Path.Combine(cache.BaseFileDirectory, "ScaleData.raw");

            // Act
            var data = await cache.GetRawFileContentsAsync(configuration, "ScaleData.raw");

            // Assert
            DirectoryAssert.Exists(cache.BaseFileDirectory);
            FileAssert.Exists(filePath);
            ClassicAssert.NotNull(data);
        }

        [Test]
        public async Task CS_Cache_Updates()
        {
            // Arrange
            CacheService cache = new CacheService(null, _loggerFactory.CreateLogger<CacheService>());
            var filename = "test.txt";
            var eTag = "12345";
            var fileContents = @"[" + Environment.NewLine +
                @"  {" + Environment.NewLine +
                @"    ""Filename"": ""test.txt""," + Environment.NewLine +
                @"    ""ETag"": ""12345""," + Environment.NewLine +
                @"    ""LastModified"": ""0001-01-01T00:00:00.0000001""" + Environment.NewLine +
                @"  }" + Environment.NewLine +
                @"]";
            var lastModified = new DateTime(1);

            // Act
            await cache.UpdateCacheDataAsync(filename, eTag, lastModified);
            var data = await File.ReadAllTextAsync(Path.Combine(cache.BaseFileDirectory, "FileDownloadCache.json"));

            // Assert
            FileAssert.Exists(Path.Combine(cache.BaseFileDirectory, "FileDownloadCache.json"));
            ClassicAssert.AreEqual(fileContents, data);
        }

        [Test]
        public async Task CS_Cache_Reads()
        {
            // Arrange
            CacheService cache = new CacheService(null, _loggerFactory.CreateLogger<CacheService>());
            var filename = "test.txt";
            var eTag = "12345";
            var lastModified = new DateTime(1);

            // Act
            await cache.UpdateCacheDataAsync(filename, eTag, lastModified);
            var cacheData = await cache.GetCacheDataAsync();

            // Assert
            ClassicAssert.IsNotNull(cacheData);
            ClassicAssert.NotZero(cacheData.Count);
            ClassicAssert.AreEqual(filename, cacheData.FirstOrDefault().Filename);
            ClassicAssert.AreEqual(eTag, cacheData.FirstOrDefault().ETag);
            ClassicAssert.AreEqual(lastModified, cacheData.FirstOrDefault().LastModified);
        }

        [Test]
        public async Task CS_Cache_Saves()
        {
            // Arrange
            CacheService cache = new CacheService(null, _loggerFactory.CreateLogger<CacheService>());

            // Act
            var filename = "test.txt";
            var eTag = "12345";
            var eTagEntry = new FileETag()
            {
                Filename = filename,
                ETag = eTag,
                LastModified = new DateTime(1)
            };
            var entries = new List<FileETag>()
            {
                eTagEntry
            };
            var fileContents = @"[" + Environment.NewLine +
                @"  {" + Environment.NewLine +
                @"    ""Filename"": ""test.txt""," + Environment.NewLine +
                @"    ""ETag"": ""12345""," + Environment.NewLine +
                @"    ""LastModified"": ""0001-01-01T00:00:00.0000001""" + Environment.NewLine +
                @"  }" + Environment.NewLine +
                @"]";

            await cache.SaveCacheDataAsync(entries);
            var data = await File.ReadAllTextAsync(Path.Combine(cache.BaseFileDirectory, "FileDownloadCache.json"));


            // Assert
            ClassicAssert.IsNotNull(data);
            FileAssert.Exists(Path.Combine(cache.BaseFileDirectory, "FileDownloadCache.json"));
            ClassicAssert.AreEqual(fileContents, data);
        }
    }
}
