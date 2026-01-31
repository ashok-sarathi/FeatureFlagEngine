using FeatureFlagEngine.Application.Interfaces.Repositories.Common;
using FeatureFlagEngine.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Repositories
{
    /// <summary>
    /// Provides data access operations specific to <see cref="FeatureFlag"/> entities,
    /// including retrieval of related override configurations required for evaluation logic.
    /// </summary>
    public interface IFeatureFlagRepository : ICommonRepository<FeatureFlag>
    {
        /// <summary>
        /// Retrieves a feature flag by its unique key, including all associated override rules.
        /// </summary>
        /// <param name="key">Unique feature flag key.</param>
        /// <returns>
        /// Feature flag entity with override data loaded; otherwise null if not found.
        /// </returns>
        /// <remarks>
        /// Overrides are included to ensure evaluation logic can run without additional database calls.
        /// </remarks>
        Task<FeatureFlag?> GetByKeyWithOverridesAsync(string key);

        /// <summary>
        /// Retrieves all feature flags with their associated override rules.
        /// </summary>
        /// <returns>List of feature flags with overrides eagerly loaded.</returns>
        /// <remarks>
        /// This method is typically used for administrative views or caching scenarios
        /// where full feature configuration is required.
        /// </remarks>
        Task<List<FeatureFlag>> GetAllWithOverridesAsync();
    }
}
