using FeatureFlagEngine.Application.Interfaces.Repositories.Common;
using FeatureFlagEngine.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Repositories.Common
{
    public abstract class CommonRepository<TEntity>(FeatureFlagDbContext dbContext) : ICommonRepository<TEntity> where TEntity : class
    {
        public readonly DbSet<TEntity> DbSet = dbContext.Set<TEntity>();

        public virtual async Task<List<TEntity>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        public virtual async Task<TEntity?> GetByIdAsync(params object[] keyValues)
        {
            return await DbSet.FindAsync(keyValues);
        }

        public virtual async Task AddAsync(TEntity entity)
        {
            DbSet.Add(entity);
            await dbContext.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(TEntity entity)
        {
            DbSet.Update(entity);
            await dbContext.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(TEntity entity)
        {
            DbSet.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        public virtual async Task<bool> ExistsAsync(params object[] keyValues)
        {
            var entity = await DbSet.FindAsync(keyValues);
            return entity != null;
        }
    }
}
