using FeatureFlagEngine.Application.Interfaces.Repositories;
using FeatureFlagEngine.Domain.Entities;
using FeatureFlagEngine.Infrastructure.Contexts;
using FeatureFlagEngine.Infrastructure.Repositories.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Repositories
{
    public class FeatureOverrideRepository(FeatureFlagDbContext dbContext) : CommonRepository<FeatureOverride>(dbContext), IFeatureOverrideRepository
    {
    }
}
