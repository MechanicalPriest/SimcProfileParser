using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Model.Generated;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimcProfileParser.Tests
{
    [TestFixture]
    class DependencyInjectionTests
    {
        [SetUp]
        public void Init()
        {
            
        }

        IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Common services
                    services.AddSimcProfileParser();
                    // Add DI Tester
                    services.AddHostedService<DiTester>();
                });

        [Test]
        public void TryTestDIExtensions()
        {
            Assert.DoesNotThrowAsync(async () => await CreateHostBuilder(null).Build().RunAsync());
        }
    }

    class DiTester : IHostedService
    {
        private readonly ISimcGenerationService _simcGenerationService;

        public DiTester(ISimcGenerationService simcGenerationService,
            IHostApplicationLifetime lifeTime)
        {
            _simcGenerationService = simcGenerationService;
            lifeTime.StopApplication();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var spellOptions = new SimcSpellOptions()
            {
                SpellId = 274740,
                PlayerLevel = 60
            };

            var spell = await _simcGenerationService.GenerateSpellAsync(spellOptions);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Delay(1);
        }
    }
}
