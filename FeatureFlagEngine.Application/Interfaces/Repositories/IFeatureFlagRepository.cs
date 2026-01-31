using FeatureFlagEngine.Application.Interfaces.Repositories.Common;
using FeatureFlagEngine.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Repositories
{
    public interface IFeatureFlagRepository : ICommonRepository<FeatureFlag>
    {
        Task<FeatureFlag?> GetByKeyWithOverridesAsync(string key);
        Task<List<FeatureFlag>> GetAllWithOverridesAsync();
    }
}
