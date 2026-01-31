using FeatureFlagEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Contexts
{
    [ExcludeFromCodeCoverage]
    public class FeatureFlagDbContext(DbContextOptions<FeatureFlagDbContext> options) : DbContext(options)
    {
        public DbSet<FeatureFlag> FeatureFlags { get; set; }
        public DbSet<FeatureOverride> FeatureOverrides { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeatureFlagDbContext).Assembly);
        }
    }
}
