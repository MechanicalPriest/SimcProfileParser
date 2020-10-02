using NUnit.Framework;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces.DataSync;
using SimcProfileParser.Model.DataSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        [SetUp]
        public void Init()
        {
            ICacheService cache = new CacheService();

            // Wipe out the directory before testing as a workaround for file access not being abstracted
            if(Directory.Exists(cache.BaseFileDirectory))
                Directory.Delete(cache.BaseFileDirectory, true);
        }

        [Test]
        /// Integration test of sorts. Checking the file download works.
        public void CS_Downloads_File()
        {
            // Arrange
            ICacheService cache = new CacheService();

            cache.RegisterFileConfiguration(SimcFileType.ItemDataInc,
                "ItemData.raw",
                "https://raw.githubusercontent.com/simulationcraft/simc/shadowlands/engine/dbc/generated/item_data.inc"
                );

            // Act
            var data = cache.GetFileContents(SimcFileType.ItemDataInc);

            // Assert
            DirectoryAssert.Exists(cache.BaseFileDirectory);
            FileAssert.Exists(Path.Combine(cache.BaseFileDirectory, "ItemData.raw"));
            Assert.NotNull(data);
        }

        [Test]
        public void CS_Cache_Updates()
        {
            // Arrange
            CacheService cache = new CacheService();

            // Act
            var filename = "test.txt";
            var eTag = "12345";
            var fileContents = @"[" + Environment.NewLine +
                @"  {" + Environment.NewLine +
                @"    ""Filename"": ""test.txt""," + Environment.NewLine +
                @"    ""ETag"": ""12345""" + Environment.NewLine +
                @"  }" + Environment.NewLine +
                @"]";

            cache.UpdateCacheData(filename, eTag);
            var data = File.ReadAllText(Path.Combine(cache.BaseFileDirectory, "FileDownloadCache.json"));

            // Assert
            FileAssert.Exists(Path.Combine(cache.BaseFileDirectory, "FileDownloadCache.json"));
            Assert.AreEqual(fileContents, data);
        }

        [Test]
        public void CS_Cache_Reads()
        {
            // Arrange
            CacheService cache = new CacheService();

            // Act
            var filename = "test.txt";
            var eTag = "12345";
            cache.UpdateCacheData(filename, eTag);
            var cacheData = cache.GetCacheData();

            // Assert
            Assert.IsNotNull(cacheData);
            Assert.NotZero(cacheData.Count);
            Assert.AreEqual(filename, cacheData.FirstOrDefault().Filename);
            Assert.AreEqual(eTag, cacheData.FirstOrDefault().ETag);
        }

        [Test]
        public void CS_Cache_Saves()
        {
            // Arrange
            CacheService cache = new CacheService();

            // Act
            var filename = "test.txt";
            var eTag = "12345";
            var eTagEntry = new FileETag()
            {
                Filename = filename,
                ETag = eTag
            };
            var entries = new List<FileETag>()
            {
                eTagEntry
            };
            var fileContents = @"[" + Environment.NewLine +
                @"  {" + Environment.NewLine +
                @"    ""Filename"": ""test.txt""," + Environment.NewLine +
                @"    ""ETag"": ""12345""" + Environment.NewLine +
                @"  }" + Environment.NewLine +
                @"]";

            cache.SaveCacheData(entries);
            var data = File.ReadAllText(Path.Combine(cache.BaseFileDirectory, "FileDownloadCache.json"));


            // Assert
            Assert.IsNotNull(data);
            FileAssert.Exists(Path.Combine(cache.BaseFileDirectory, "FileDownloadCache.json"));
            Assert.AreEqual(fileContents, data);
        }

        [OneTimeTearDown]
        public void TearDownOnce()
        {
            ICacheService cache = new CacheService();
            if (Directory.Exists(cache.BaseFileDirectory))
                Directory.Delete(cache.BaseFileDirectory, true);
        }
    }
}
