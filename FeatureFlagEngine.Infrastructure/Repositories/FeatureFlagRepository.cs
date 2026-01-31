using FeatureFlagEngine.Application.Interfaces.Repositories;
using FeatureFlagEngine.Domain.Entities;
using FeatureFlagEngine.Infrastructure.Contexts;
using FeatureFlagEngine.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for <see cref="FeatureFlag"/> entities.
    /// Extends the base repository with queries that include related override data
    /// required for feature evaluation logic.
    /// </summary>
    public class FeatureFlagRepository(FeatureFlagDbContext dbContext)
        : CommonRepository<FeatureFlag>(dbContext), IFeatureFlagRepository
    {
        /// <summary>
        /// Retrieves a feature flag by its unique key, including all associated override rules.
        /// </summary>
        /// <param name="key">Unique feature flag key.</param>
        /// <returns>Feature flag with overrides loaded; otherwise null if not found.</returns>
        /// <remarks>
        /// Overrides are eagerly loaded to avoid additional database calls during evaluation.
        /// </remarks>
        public async Task<FeatureFlag?> GetByKeyWithOverridesAsync(string key)
        {
            return await dbContext.FeatureFlags
                .Include(f => f.Overrides)
                .FirstOrDefaultAsync(f => f.Key == key);
        }

        /// <summary>
        /// Retrieves all feature flags along with their associated override rules.
        /// </summary>
        /// <returns>List of feature flags with overrides eagerly loaded.</returns>
        /// <remarks>
        /// Typically used for administrative dashboards or cache warm-up scenarios
        /// where full configuration data is required.
        /// </remarks>
        public virtual async Task<List<FeatureFlag>> GetAllWithOverridesAsync()
        {
            return await DbSet
                .Include(o => o.Overrides)
                .ToListAsync();
        }
    }
}
