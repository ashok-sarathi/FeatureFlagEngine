using FeatureFlagEngine.Application.Interfaces.Repositories.Common;
using FeatureFlagEngine.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Repositories
{
    /// <summary>
    /// Provides data access operations for <see cref="FeatureOverride"/> entities.
    /// Overrides represent targeting rules that take precedence over global feature flag state.
    /// </summary>
    public interface IFeatureOverrideRepository : ICommonRepository<FeatureOverride>
    {
        // Intentionally left without additional methods.
        // Specialized override queries can be added here if needed in the future.
    }
}
