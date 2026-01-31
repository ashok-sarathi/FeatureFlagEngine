using FeatureFlagEngine.Application.Interfaces.Repositories.Common;
using FeatureFlagEngine.Application.Interfaces.Services.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FeatureFlagEngine.Application.Services.Common
{
    /// <summary>
    /// Provides a reusable base implementation for common CRUD service operations.
    /// Handles interaction with the repository layer and delegates entity ↔ DTO mapping
    /// to derived classes.
    /// </summary>
    /// <typeparam name="TEntity">Domain entity type.</typeparam>
    /// <typeparam name="TDto">Data Transfer Object type.</typeparam>
    /// <remarks>
    /// This class centralizes standard CRUD behavior to reduce duplication across services.
    /// Business-specific logic should be implemented in derived service classes.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public abstract class CommonService<TEntity, TDto> : ICommonService<TDto>
        where TEntity : class
        where TDto : class
    {
        /// <summary>
        /// Underlying repository used for data persistence operations.
        /// </summary>
        protected readonly ICommonRepository<TEntity> _repository;

        protected CommonService(ICommonRepository<TEntity> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Retrieves all entities and maps them to DTOs.
        /// </summary>
        public virtual async Task<List<TDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Retrieves an entity by its identifier(s) and maps it to a DTO.
        /// </summary>
        /// <param name="ids">Primary key value(s).</param>
        /// <returns>Mapped DTO if found; otherwise null.</returns>
        public virtual async Task<TDto?> GetByIdAsync(params object[] ids)
        {
            var entity = await _repository.GetByIdAsync(ids);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new entity from the provided DTO and persists it.
        /// </summary>
        /// <param name="dto">DTO containing data for the new entity.</param>
        /// <returns>The created DTO, including any generated values.</returns>
        public virtual async Task<TDto> CreateAsync(TDto dto)
        {
            var entity = MapToEntity(dto);
            await _repository.AddAsync(entity);

            // Map again in case the entity received generated values (e.g., IDs)
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing entity using values from the provided DTO.
        /// </summary>
        /// <param name="dto">DTO containing updated data.</param>
        public virtual async Task UpdateAsync(TDto dto)
        {
            var entity = MapToEntity(dto);
            await _repository.UpdateAsync(entity);
        }

        /// <summary>
        /// Deletes an entity identified by its primary key value(s).
        /// </summary>
        /// <param name="ids">Primary key value(s).</param>
        public virtual async Task DeleteAsync(params object[] ids)
        {
            var entity = await _repository.GetByIdAsync(ids);

            // Only attempt deletion if the entity exists
            if (entity != null)
                await _repository.DeleteAsync(entity);
        }

        /// <summary>
        /// Checks whether an entity exists for the given identifier(s).
        /// </summary>
        /// <param name="ids">Primary key value(s).</param>
        /// <returns>True if the entity exists; otherwise false.</returns>
        public Task<bool> ExistsAsync(params object[] ids)
            => _repository.ExistsAsync(ids);

        /// <summary>
        /// Maps a domain entity to its corresponding DTO.
        /// Must be implemented in derived classes.
        /// </summary>
        protected abstract TDto MapToDto(TEntity entity);

        /// <summary>
        /// Maps a DTO to its corresponding domain entity.
        /// Must be implemented in derived classes.
        /// </summary>
        protected abstract TEntity MapToEntity(TDto dto);
    }
}
