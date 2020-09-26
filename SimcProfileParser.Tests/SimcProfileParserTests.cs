using NUnit.Framework;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    public class SimcProfileParserTests
    {
        private SimcParserService _simcParserService;

        [SetUp]
        public void Init()
        {
            _simcParserService = new SimcParserService();
        }

        [Test]
        public void Test_Test_Method()
        {
            // Something to test the Testing integrations
            Assert.AreEqual(_simcParserService.Test(), "This is a test only!");
        }

        [Test]
        public void Test_Internal_Method()
        {
            // Something to test that internals are exposted to tests
            Assert.AreEqual(_simcParserService.InternalTest(), "An internal method, top secret!");
        }

        [Test]
        public void Test_That_Fails()
        {
            Assert.AreEqual(1, 0);
            Assert.Fail("This test has failed intentionally :(");
        }
    }
}
