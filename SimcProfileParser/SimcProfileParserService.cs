using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser
{
    public class SimcProfileParserService : ISimcProfileParserService
    {
        private readonly ILogger<SimcProfileParserService> _logger;

        public SimcProfileParserService(ILogger<SimcProfileParserService> logger)
        {
            _logger = logger;
        }

        public SimcProfileParserService()
            : this(NullLogger<SimcProfileParserService>.Instance)
        {

        }

        public SimcProfile GenerateProfileAsync(List<string> profileString)
        {
            throw new NotImplementedException();
        }

        public SimcProfile GenerateProfileAsync(string profileString)
        {
            throw new NotImplementedException();
        }

        public SimcProfile GenerateProfile(List<string> profileString)
        {
            throw new NotImplementedException();
        }

        public SimcProfile GenerateProfile(string profileString)
        {
            throw new NotImplementedException();
        }

        public SimcItem GenerateItemAsync(SimcItemOptions options)
        {
            throw new NotImplementedException();
        }

        public SimcItem GenerateItem(SimcItemOptions options)
        {
            throw new NotImplementedException();
        }

        public SimcSpell GenerateSpellAsync(SimcSpellOptions options)
        {
            throw new NotImplementedException();
        }

        public SimcSpell GenerateSpell(SimcSpellOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
