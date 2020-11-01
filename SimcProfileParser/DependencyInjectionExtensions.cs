using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimcProfileParser.DataSync;
using SimcProfileParser.Interfaces;
using SimcProfileParser.Interfaces.DataSync;

namespace SimcProfileParser
{
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Register the dependencies for the SimcProfileParser library
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddSimcProfileParser(this IServiceCollection services)
        {
            services.AddTransient<ISimcGenerationService, SimcGenerationService>((provider) =>
            {
                return ActivatorUtilities.CreateInstance<SimcGenerationService>(provider);
            });

            // The cache is a singleton as it keeps a bunch of stuff in memory.
            services.TryAddSingleton<ICacheService, CacheService>();
            services.TryAddSingleton<IRawDataExtractionService, RawDataExtractionService>();

            services.TryAddSingleton<ISimcParserService, SimcParserService>();
            services.TryAddSingleton<ISimcUtilityService, SimcUtilityService>();
            services.TryAddSingleton<ISimcSpellCreationService, SimcSpellCreationService>();
            services.TryAddSingleton<ISimcItemCreationService, SimcItemCreationService>();
            services.TryAddSingleton<ISimcVersionService, SimcVersionService>();

            return services;
        }
    }
}
