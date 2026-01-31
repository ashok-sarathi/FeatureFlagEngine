using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Services.Common
{
    /// <summary>
    /// Provides generic CRUD operations for application services working with DTOs.
    /// This abstraction allows reusable service logic across different domain entities.
    /// </summary>
    /// <typeparam name="TDto">Data Transfer Object type.</typeparam>
    public interface ICommonService<TDto>
        where TDto : class
    {
        /// <summary>
        /// Retrieves all records.
        /// </summary>
        /// <returns>List of DTOs.</returns>
        Task<List<TDto>> GetAllAsync();

        /// <summary>
        /// Retrieves a single record by its identifier(s).
        /// Supports composite keys when multiple IDs are provided.
        /// </summary>
        /// <param name="ids">Primary key value(s).</param>
        /// <returns>DTO if found; otherwise null.</returns>
        Task<TDto?> GetByIdAsync(params object[] ids);

        /// <summary>
        /// Creates a new record.
        /// </summary>
        /// <param name="dto">DTO containing data to create.</param>
        /// <returns>The created DTO with updated identifiers or system-generated values.</returns>
        Task<TDto> CreateAsync(TDto dto);

        /// <summary>
        /// Updates an existing record.
        /// </summary>
        /// <param name="dto">DTO containing updated data.</param>
        /// <remarks>
        /// The DTO must include valid identifier values to locate the existing record.
        /// </remarks>
        Task UpdateAsync(TDto dto);

        /// <summary>
        /// Deletes a record by its identifier(s).
        /// Supports composite keys when multiple IDs are provided.
        /// </summary>
        /// <param name="ids">Primary key value(s).</param>
        Task DeleteAsync(params object[] ids);

        /// <summary>
        /// Checks whether a record exists for the given identifier(s).
        /// </summary>
        /// <param name="ids">Primary key value(s).</param>
        /// <returns>True if the record exists; otherwise false.</returns>
        Task<bool> ExistsAsync(params object[] ids);
    }
}
