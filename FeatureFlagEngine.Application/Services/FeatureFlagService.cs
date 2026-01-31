using FeatureFlagEngine.Application.Interfaces.Repositories;
using FeatureFlagEngine.Application.Interfaces.Repositories.Common;
using FeatureFlagEngine.Application.Services.Common;
using FeatureFlagEngine.Domain.Dtos.FeatureFlag;
using FeatureFlagEngine.Domain.Dtos.FeatureOverride;
using FeatureFlagEngine.Domain.Entities;
using FeatureFlagEngine.Domain.Enums;
using FeatureFlagEngine.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Services
{
    public class FeatureFlagService(IFeatureFlagRepository featureRepo, IFeatureOverrideRepository featureOverrideRepo, IRedisCacheService cache)
        : CommonService<FeatureFlag, FeatureFlagDto>(featureRepo), IFeatureFlagService
    {
        private const string CachePrefix = "feature_eval:";

        public async Task<List<FeatureFlagDto>> GetAllAsync(bool includeOverrides)
        {
            return includeOverrides
                ? (await featureRepo.GetAllWithOverridesAsync())
                    .Select(MapToDto)
                    .ToList()
                : await base.GetAllAsync();
        }

        public override async Task<FeatureFlagDto> CreateAsync(FeatureFlagDto dto)
        {
            var existing = await featureRepo.GetByKeyWithOverridesAsync(dto.Key);
            if (existing != null)
                throw new BadRequestException($"Feature '{dto.Key}' already exists.");

            dto.Id = Guid.NewGuid();
            return await base.CreateAsync(dto);
        }


        public async Task<(bool, bool)> EvaluateAsync(string key, string? userId = null, string? groupId = null)
        {
            var cacheKey = BuildCacheKey(key, userId, groupId);

            var cached = await cache.GetAsync<bool?>(cacheKey);
            if (cached.HasValue)
                return (cached.Value, true);

            var feature = await featureRepo.GetByKeyWithOverridesAsync(key)
                          ?? throw new NotFoundException($"Feature '{key}' not found");

            var overrides = feature.Overrides;
            bool result;

            // 1️. User override (highest priority)
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userOverride = overrides.FirstOrDefault(o =>
                    o.OverrideType == FeatureOverrideType.User &&
                    o.TargetId == userId);

                if (userOverride != null)
                {
                    result = userOverride.IsEnabled;
                    await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                    return (result, false);
                }
            }

            // 2️. Group override
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                var groupOverride = overrides.FirstOrDefault(o =>
                    o.OverrideType == FeatureOverrideType.Group &&
                    o.TargetId == groupId);

                if (groupOverride != null)
                {
                    result = groupOverride.IsEnabled;
                    await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                    return (result, false);
                }
            }

            // 3️. Global default
            result = feature.IsEnabled;

            await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            return (result, false);
        }

        public async Task AddOverrideAsync(string key, FeatureOverrideDto dto)
        {
            var feature = await featureRepo.GetByKeyWithOverridesAsync(key)
                          ?? throw new NotFoundException($"Feature '{key}' not found");

            var existing = feature.Overrides.FirstOrDefault(o =>
                o.OverrideType == dto.OverrideType &&
                o.TargetId == dto.TargetId);

            if (existing != null)
            {
                existing.IsEnabled = dto.IsEnabled;
                await featureOverrideRepo.UpdateAsync(existing);
            }
            else
            {
                await featureOverrideRepo.AddAsync(new FeatureOverride
                {
                    Id = Guid.NewGuid(),
                    FeatureFlagId = feature.Id,
                    OverrideType = dto.OverrideType,
                    TargetId = dto.TargetId,
                    IsEnabled = dto.IsEnabled
                });
            }
            await InvalidateFeatureCache(BuildCacheKey(key, dto.OverrideType == FeatureOverrideType.User ? dto.TargetId : null, dto.OverrideType == FeatureOverrideType.Group ? dto.TargetId : null));
        }

        public async Task RemoveOverrideAsync(string key, FeatureOverrideType type, string targetId)
        {
            var feature = await featureRepo.GetByKeyWithOverridesAsync(key)
                          ?? throw new NotFoundException($"Feature '{key}' not found");

            var existing = feature.Overrides.FirstOrDefault(o =>
                o.OverrideType == type &&
                o.TargetId == targetId);

            if (existing == null)
                throw new NotFoundException("Override not found");

            feature.Overrides.Remove(existing);
            await featureRepo.UpdateAsync(feature);
            await InvalidateFeatureCache(BuildCacheKey(key, type == FeatureOverrideType.User ? targetId : null, type == FeatureOverrideType.Group ? targetId : null));
        }


        public async Task UpdateGlobalStateAsync(string key, bool isEnabled)
        {
            var feature = await featureRepo.GetByKeyWithOverridesAsync(key)
                          ?? throw new NotFoundException($"Feature '{key}' not found");

            feature.IsEnabled = isEnabled;
            await featureRepo.UpdateAsync(feature);
            await InvalidateFeatureCache(key);
        }


        protected override FeatureFlagDto MapToDto(FeatureFlag entity)
        {
            return new FeatureFlagDto
            {
                Id = entity.Id,
                Key = entity.Key,
                IsEnabled = entity.IsEnabled,
                Description = entity.Description,
                Overrides = entity.Overrides.Select(o => new FeatureOverrideDto
                {
                    Id = o.Id,
                    FeatureFlagId = o.FeatureFlagId,
                    OverrideType = o.OverrideType,
                    TargetId = o.TargetId,
                    IsEnabled = o.IsEnabled
                }).ToList()
            };
        }

        protected override FeatureFlag MapToEntity(FeatureFlagDto dto)
        {
            return new FeatureFlag
            {
                Id = !dto.Id.HasValue || dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id.Value,
                Key = dto.Key,
                IsEnabled = dto.IsEnabled,
                Description = dto.Description,
                Overrides = dto.Overrides?.Select(o => new FeatureOverride
                {
                    Id = !o.Id.HasValue || o.Id == Guid.Empty ? Guid.NewGuid() : o.Id.Value,
                    OverrideType = o.OverrideType,
                    TargetId = o.TargetId,
                    IsEnabled = o.IsEnabled
                }).ToList() ?? []
            };
        }

        private static string BuildCacheKey(string key, string? userId, string? groupId)
        {
            var result = $"{CachePrefix}{key}";
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                result += $":group:{groupId}";
            }
            if(!string.IsNullOrWhiteSpace(userId))
            {
                result += $":user:{userId}";
            }
            return result;
        }

        private async Task InvalidateFeatureCache(string key)
        {
            // Simple approach: remove all evaluations for this feature
            await cache.RemoveAsync($"{CachePrefix}{key}");
        }
    }
}
