using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    class SimcGenerationServiceIntegrationTests
    {
        private SimcGenerationService _sgs;
        private List<string> _profileString;

        [OneTimeSetUp]
        public async Task Init()
        {
            _sgs = new SimcGenerationService();

            var testFile = @"RawData" + Path.DirectorySeparatorChar + "Ardaysauk.simc";
            var testFileContents = await File.ReadAllLinesAsync(testFile);
            _profileString = new List<string>(testFileContents);
        }

        [Test]
        public async Task SGS_Creates_Profile()
        {
            // Arrange

            // Act
            var profile = await _sgs.GenerateProfileAsync(_profileString);

            // Assert
            Assert.IsNotNull(profile, "Profile not null");
            Assert.IsNotNull(profile.ParsedProfile, "Parsed profile not null");
            Assert.IsNotNull(profile.ParsedProfile.Name);
            Assert.NotZero(profile.ParsedProfile.Level);
            Assert.NotZero(profile.GeneratedItems.Count);
            return;
        }
    }
}
