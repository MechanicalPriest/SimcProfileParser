using SimcProfileParser.Interfaces;
using System;

namespace SimcProfileParser
{
    public class SimcParserService : ISimcParserService
    {
        /// <summary>
        /// Test method to test github actions and nuget workflow.
        /// </summary>
        /// <returns></returns>
        public string Test()
        {
            return "This is a test only!";
        }

        internal string InternalTest()
        {
            return "An internal method, top secret!";
        }
    }
}
