using FeatureFlagEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Contexts
{
    /// <summary>
    /// Entity Framework Core database context for the Feature Flag Engine.
    /// Manages entity sets and applies model configurations for persistence.
    /// </summary>
    /// <remarks>
    /// This context is responsible only for database mapping and configuration.
    /// Business logic should not be placed here.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public class FeatureFlagDbContext(DbContextOptions<FeatureFlagDbContext> options)
        : DbContext(options)
    {
        /// <summary>
        /// Represents the FeatureFlags table in the database.
        /// </summary>
        public DbSet<FeatureFlag> FeatureFlags { get; set; }

        /// <summary>
        /// Represents the FeatureOverrides table in the database.
        /// </summary>
        public DbSet<FeatureOverride> FeatureOverrides { get; set; }

        /// <summary>
        /// Configures entity mappings and relationships using configuration classes
        /// defined in the same assembly.
        /// </summary>
        /// <param name="modelBuilder">Model builder used to configure entity models.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Automatically applies IEntityTypeConfiguration<T> implementations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeatureFlagDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
