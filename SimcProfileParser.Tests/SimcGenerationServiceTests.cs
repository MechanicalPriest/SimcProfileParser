using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    class SimcGenerationServiceTests
    {
        [Test]
        public async Task SGS_Throws_Empty_ProfileString()
        {
            // Arrange
            var sgs = new SimcGenerationService();
            string inputData = null;

            // Act

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await sgs.GenerateProfileAsync(inputData));
            return;
        }

        [Test]
        public async Task SGS_Throws_Null_StringList()
        {
            // Arrange
            var sgs = new SimcGenerationService();
            List<string> inputData = null;

            // Act

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await sgs.GenerateProfileAsync(inputData));
            return;
        }
    }
}
