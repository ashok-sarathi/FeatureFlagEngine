using FeatureFlagEngine.Application.Interfaces.Repositories.Common;
using FeatureFlagEngine.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Repositories.Common
{
    /// <summary>
    /// Provides a reusable base implementation of repository operations using Entity Framework Core.
    /// Handles basic CRUD functionality and persists changes immediately after each operation.
    /// </summary>
    /// <typeparam name="TEntity">Entity type mapped to the database.</typeparam>
    /// <remarks>
    /// This class abstracts direct interaction with <see cref="DbContext"/> and <see cref="DbSet{TEntity}"/>.
    /// For performance-sensitive scenarios, derived repositories can override methods to customize queries.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public abstract class CommonRepository<TEntity>(FeatureFlagDbContext dbContext)
        : ICommonRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// EF Core DbSet representing the table for the entity.
        /// </summary>
        public readonly DbSet<TEntity> DbSet = dbContext.Set<TEntity>();

        /// <summary>
        /// Retrieves all entities from the database.
        /// </summary>
        public virtual async Task<List<TEntity>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        /// <summary>
        /// Retrieves an entity by its primary key value(s).
        /// Supports composite keys.
        /// </summary>
        /// <param name="keyValues">Primary key value(s).</param>
        /// <returns>Entity if found; otherwise null.</returns>
        public virtual async Task<TEntity?> GetByIdAsync(params object[] keyValues)
        {
            return await DbSet.FindAsync(keyValues);
        }

        /// <summary>
        /// Adds a new entity and immediately saves changes to the database.
        /// </summary>
        /// <param name="entity">Entity instance to add.</param>
        public virtual async Task AddAsync(TEntity entity)
        {
            DbSet.Add(entity);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an existing entity and immediately saves changes.
        /// </summary>
        /// <param name="entity">Entity instance with updated values.</param>
        /// <remarks>
        /// EF Core will mark the entire entity as modified.
        /// </remarks>
        public virtual async Task UpdateAsync(TEntity entity)
        {
            DbSet.Update(entity);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Removes an entity from the database and saves changes.
        /// </summary>
        /// <param name="entity">Entity instance to delete.</param>
        public virtual async Task DeleteAsync(TEntity entity)
        {
            DbSet.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Determines whether an entity exists for the given primary key value(s).
        /// </summary>
        /// <param name="keyValues">Primary key value(s).</param>
        /// <returns>True if the entity exists; otherwise false.</returns>
        public virtual async Task<bool> ExistsAsync(params object[] keyValues)
        {
            var entity = await DbSet.FindAsync(keyValues);
            return entity != null;
        }
    }
}
