using FeatureFlagEngine.Application.Interfaces.Repositories;
using FeatureFlagEngine.Application.Interfaces.Services;
using FeatureFlagEngine.Infrastructure.Contexts;
using FeatureFlagEngine.Infrastructure.Repositories;
using FeatureFlagEngine.Infrastructure.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace FeatureFlagEngine.Api.Helpers.Bootstraps
{
    public static class ServiceBootstrap
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<FeatureFlagDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("FeatureFlagsDb"));
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["RedisSettings:CacheServer"];
            });

            // Register repositories
            services.AddTransient<IFeatureFlagRepository, FeatureFlagRepository>();
            services.AddTransient<IFeatureOverrideRepository, FeatureOverrideRepository>();

            // Register application services
            services.AddScoped<IFeatureFlagService, FeatureFlagService>();
            services.AddScoped<IRedisCacheService, RedisCacheService>();
        }

        public static void UseMigration(this WebApplication builder)
        {
            // Do auto migration
            using (var scope = builder.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FeatureFlagDbContext>();
                db.Database.Migrate();
            }
        }
    }
}
