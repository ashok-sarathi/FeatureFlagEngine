using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Repositories.Common
{
    /// <summary>
    /// Defines generic data access operations for entities.
    /// This abstraction isolates the application layer from the underlying ORM or database technology.
    /// </summary>
    /// <typeparam name="TEntity">Entity type mapped to a data store.</typeparam>
    public interface ICommonRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Retrieves all entities from the data store.
        /// </summary>
        /// <returns>List of entities.</returns>
        Task<List<TEntity>> GetAllAsync();

        /// <summary>
        /// Retrieves an entity by its primary key value(s).
        /// Supports composite keys when multiple key values are provided.
        /// </summary>
        /// <param name="keyValues">Primary key value(s).</param>
        /// <returns>Entity if found; otherwise null.</returns>
        Task<TEntity?> GetByIdAsync(params object[] keyValues);

        /// <summary>
        /// Adds a new entity to the data store.
        /// </summary>
        /// <param name="entity">Entity instance to add.</param>
        Task AddAsync(TEntity entity);

        /// <summary>
        /// Updates an existing entity in the data store.
        /// </summary>
        /// <param name="entity">Entity instance with updated values.</param>
        /// <remarks>
        /// The entity must already exist in the data store and have valid key values.
        /// </remarks>
        Task UpdateAsync(TEntity entity);

        /// <summary>
        /// Removes an entity from the data store.
        /// </summary>
        /// <param name="entity">Entity instance to delete.</param>
        Task DeleteAsync(TEntity entity);

        /// <summary>
        /// Checks whether an entity exists for the given primary key value(s).
        /// </summary>
        /// <param name="keyValues">Primary key value(s).</param>
        /// <returns>True if the entity exists; otherwise false.</returns>
        Task<bool> ExistsAsync(params object[] keyValues);
    }
}
