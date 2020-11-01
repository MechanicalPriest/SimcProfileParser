using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    class SimcGenerationServiceTests
    {
        [Test]
        public void SGS_Throws_Empty_ProfileString()
        {
            // Arrange
            var sgs = new SimcGenerationService();
            string inputData = null;

            // Act

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await sgs.GenerateProfileAsync(inputData));
        }

        [Test]
        public void SGS_Throws_Null_StringList()
        {
            // Arrange
            var sgs = new SimcGenerationService();
            List<string> inputData = null;

            // Act

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await sgs.GenerateProfileAsync(inputData));
        }
    }
}
