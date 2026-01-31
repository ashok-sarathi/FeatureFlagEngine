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
    public class FeatureFlagRepository(FeatureFlagDbContext dbContext) : CommonRepository<FeatureFlag>(dbContext), IFeatureFlagRepository
    {
        public async Task<FeatureFlag?> GetByKeyWithOverridesAsync(string key)
        {
            return await dbContext.FeatureFlags
                .Include(f => f.Overrides)
                .FirstOrDefaultAsync(f => f.Key == key);
        }

        public virtual async Task<List<FeatureFlag>> GetAllWithOverridesAsync()
        {
            return await DbSet.Include(o => o.Overrides).ToListAsync();
        }
    }
}
