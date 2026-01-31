using FeatureFlagEngine.Application.Interfaces.Repositories;
using FeatureFlagEngine.Application.Interfaces.Services;
using FeatureFlagEngine.Infrastructure.Repositories;

namespace FeatureFlagEngine.Api.Helpers.Bootstraps
{
    public static class ServiceBootstrap
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            // Register repositories
            services.AddTransient<IFeatureFlagRepository, FeatureFlagRepository>();
            services.AddTransient<IFeatureOverrideRepository, FeatureOverrideRepository>();

            // Register application services
            services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        }
    }
}
