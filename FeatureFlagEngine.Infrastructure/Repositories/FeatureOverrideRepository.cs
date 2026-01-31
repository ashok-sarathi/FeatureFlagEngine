using FeatureFlagEngine.Application.Interfaces.Repositories;
using FeatureFlagEngine.Domain.Entities;
using FeatureFlagEngine.Infrastructure.Contexts;
using FeatureFlagEngine.Infrastructure.Repositories.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for <see cref="FeatureOverride"/> entities.
    /// Inherits common CRUD behavior from <see cref="CommonRepository{TEntity}"/>.
    /// </summary>
    /// <remarks>
    /// This repository exists to keep override persistence concerns separate from
    /// feature flag data access, allowing independent evolution of override queries
    /// and logic in the future.
    /// </remarks>
    public class FeatureOverrideRepository(FeatureFlagDbContext dbContext)
        : CommonRepository<FeatureOverride>(dbContext), IFeatureOverrideRepository
    {
        // No additional methods currently required.
        // Custom override-specific queries can be added here as the system evolves.
    }
}
