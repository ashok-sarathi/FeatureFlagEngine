using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics.CodeAnalysis;

namespace FeatureFlagEngine.Api.Helpers.Bootstraps
{
    [ExcludeFromCodeCoverage]
    public static class AppHealthChecks
    {
        public static void AddAppHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("FeatureFlagsDb")!,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy)
            .AddRedis(
                configuration["RedisSettings:CacheServer"]!,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy);
        }

        public static void UseAppHealthChecks(this WebApplication builder)
        {
            builder.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var result = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            error = e.Value.Exception?.Message,
                            duration = e.Value.Duration.ToString()
                        })
                    };

                    await context.Response.WriteAsJsonAsync(result);
                }
            });
        }

    }
}
