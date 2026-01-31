using FeatureFlagEngine.Application.Interfaces.Services.Common;
using FeatureFlagEngine.Domain.Dtos.FeatureFlag;
using FeatureFlagEngine.Domain.Dtos.FeatureOverride;
using FeatureFlagEngine.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Services
{
    public interface IFeatureFlagService : ICommonService<FeatureFlagDto>
    {
        Task<List<FeatureFlagDto>> GetAllAsync(bool includeOverrides);
        Task<bool> EvaluateAsync(string key, string? targetId = null, string? groupId = null);
        Task AddOverrideAsync(string key, FeatureOverrideDto overrideDto);
        Task UpdateGlobalStateAsync(string key, bool isEnabled);
        Task RemoveOverrideAsync(string key, FeatureOverrideType type, string targetId);
    }
}
