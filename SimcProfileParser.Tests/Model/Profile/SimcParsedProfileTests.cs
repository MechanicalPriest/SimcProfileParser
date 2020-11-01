using NUnit.Framework;
using SimcProfileParser.Model.Profile;

namespace SimcProfileParser.Tests.Model.Profile
{
    [TestFixture]
    class SimcParsedProfileTests
    {
        [Test]
        public void SimcParsedProfile_Collections_Not_Null()
        {
            // Arrange
            var spp = new SimcParsedProfile();

            // Assert
            Assert.IsNotNull(spp.Soulbinds);
            Assert.IsNotNull(spp.Conduits);
            Assert.IsNotNull(spp.Professions);
            Assert.IsNotNull(spp.Items);
        }

        [Test]
        public void SimcParsedProfile_Collections_Are_Readonly()
        {

            // TODO: Populate an spp and test each collection for editing its items
            // Assert.Throws(Type exceptionType, TestDelegate code);
        }

    }
}
