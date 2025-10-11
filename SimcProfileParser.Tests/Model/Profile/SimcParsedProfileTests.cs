using NUnit.Framework;
using NUnit.Framework.Legacy;
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
            ClassicAssert.IsNotNull(spp.Professions);
            ClassicAssert.IsNotNull(spp.Items);
        }

        [Test]
        public void SimcParsedProfile_Collections_Are_Readonly()
        {

            // TODO: Populate an spp and test each collection for editing its items
            // ClassicAssert.Throws(Type exceptionType, TestDelegate code);
        }

    }
}
