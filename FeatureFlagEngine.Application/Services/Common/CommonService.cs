using FeatureFlagEngine.Application.Interfaces.Repositories.Common;
using FeatureFlagEngine.Application.Interfaces.Services.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FeatureFlagEngine.Application.Services.Common
{
    [ExcludeFromCodeCoverage]
    public abstract class CommonService<TEntity, TDto> : ICommonService<TDto>
        where TEntity : class
        where TDto : class
    {
        protected readonly ICommonRepository<TEntity> _repository;

        protected CommonService(ICommonRepository<TEntity> repository)
        {
            _repository = repository;
        }

        public virtual async Task<List<TDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(MapToDto).ToList();
        }

        public virtual async Task<TDto?> GetByIdAsync(params object[] ids)
        {
            var entity = await _repository.GetByIdAsync(ids);
            return entity == null ? null : MapToDto(entity);
        }

        public virtual async Task<TDto> CreateAsync(TDto dto)
        {
            var entity = MapToEntity(dto);
            await _repository.AddAsync(entity);
            return MapToDto(entity);
        }

        public virtual async Task UpdateAsync(TDto dto)
        {
            var entity = MapToEntity(dto);
            await _repository.UpdateAsync(entity);
        }

        public virtual async Task DeleteAsync(params object[] ids)
        {
            var entity = await _repository.GetByIdAsync(ids);
            if (entity != null)
                await _repository.DeleteAsync(entity);
        }

        public Task<bool> ExistsAsync(params object[] ids)
            => _repository.ExistsAsync(ids);

        protected abstract TDto MapToDto(TEntity entity);
        protected abstract TEntity MapToEntity(TDto dto);
    }
}
