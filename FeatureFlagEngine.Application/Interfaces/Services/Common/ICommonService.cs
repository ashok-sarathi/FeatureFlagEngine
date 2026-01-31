using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Services.Common
{
    public interface ICommonService<TDto>
        where TDto : class
    {
        Task<List<TDto>> GetAllAsync();
        Task<TDto?> GetByIdAsync(params object[] ids);
        Task<TDto> CreateAsync(TDto dto);
        Task UpdateAsync(TDto dto);
        Task DeleteAsync(params object[] ids);
        Task<bool> ExistsAsync(params object[] ids);
    }
}
