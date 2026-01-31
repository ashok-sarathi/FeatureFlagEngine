
using FeatureFlagEngine.Api.Helpers.Bootstraps;
using FeatureFlagEngine.Api.Helpers.Middlewares;
using FeatureFlagEngine.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FeatureFlagEngine.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<FeatureFlagDbContext>(options =>
            {
                options.UseSqlite(builder.Configuration.GetConnectionString("FeatureFlagsDb"));
            });

            builder.Services.AddApplicationServices();

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FeatureFlagDbContext>();
                db.Database.Migrate();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
