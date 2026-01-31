using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Repositories.Common
{
    public interface ICommonRepository<TEntity> where TEntity : class
    {
        Task<List<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(params object[] keyValues);
        Task AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task<bool> ExistsAsync(params object[] keyValues);
    }
}
