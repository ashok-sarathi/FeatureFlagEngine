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
    /// <summary>
    /// Application service responsible for managing feature flags and executing
    /// runtime evaluation logic including overrides and caching.
    /// </summary>
    /// <remarks>
    /// Evaluation priority:
    /// 1. User override
    /// 2. Group override
    /// 3. Global feature state (default)
    /// </remarks>
    public class FeatureFlagService(
        IFeatureFlagRepository featureRepo,
        IFeatureOverrideRepository featureOverrideRepo,
        IRedisCacheService cache)
        : CommonService<FeatureFlag, FeatureFlagDto>(featureRepo), IFeatureFlagService
    {
        private const string CachePrefix = "feature_eval:";

        /// <summary>
        /// Retrieves all feature flags with optional inclusion of override data.
        /// </summary>
        public async Task<List<FeatureFlagDto>> GetAllAsync(bool includeOverrides)
        {
            return includeOverrides
                ? (await featureRepo.GetAllWithOverridesAsync())
                    .Select(MapToDto)
                    .ToList()
                : await base.GetAllAsync();
        }

        /// <summary>
        /// Creates a new feature flag if the key does not already exist.
        /// </summary>
        /// <exception cref="BadRequestException">Thrown if a feature with the same key already exists.</exception>
        public override async Task<FeatureFlagDto> CreateAsync(FeatureFlagDto dto)
        {
            var existing = await featureRepo.GetByKeyWithOverridesAsync(dto.Key);
            if (existing != null)
                throw new BadRequestException($"Feature '{dto.Key}' already exists.");

            dto.Id = Guid.NewGuid();
            return await base.CreateAsync(dto);
        }

        /// <summary>
        /// Evaluates whether a feature is enabled for a given user and/or group context.
        /// Results are cached to reduce repeated database lookups.
        /// </summary>
        /// <returns>
        /// Tuple:
        /// Item1 → evaluation result,
        /// Item2 → indicates whether the result was returned from cache.
        /// </returns>
        public async Task<(bool, bool)> EvaluateAsync(string key, string? userId = null, string? groupId = null)
        {
            var cacheKey = BuildCacheKey(key, userId, groupId);

            // Attempt to serve evaluation result from cache first
            var cached = await cache.GetAsync<bool?>(cacheKey);
            if (cached.HasValue)
                return (cached.Value, true);

            var feature = await featureRepo.GetByKeyWithOverridesAsync(key)
                          ?? throw new NotFoundException($"Feature '{key}' not found");

            var overrides = feature.Overrides;
            bool result;

            // 1. User-specific override (highest priority)
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

            // 2. Group-specific override
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

            // 3. Global feature state (fallback)
            result = feature.IsEnabled;

            await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            return (result, false);
        }

        /// <summary>
        /// Adds or updates an override rule for a feature flag.
        /// Existing overrides for the same target are updated instead of duplicated.
        /// </summary>
        public async Task AddOverrideAsync(string key, FeatureOverrideDto dto)
        {
            var feature = await featureRepo.GetByKeyWithOverridesAsync(key)
                          ?? throw new NotFoundException($"Feature '{key}' not found");

            var existing = feature.Overrides.FirstOrDefault(o =>
                o.OverrideType == dto.OverrideType &&
                o.TargetId == dto.TargetId);

            if (existing != null)
            {
                // Update existing override
                existing.IsEnabled = dto.IsEnabled;
                await featureOverrideRepo.UpdateAsync(existing);
            }
            else
            {
                // Create new override
                await featureOverrideRepo.AddAsync(new FeatureOverride
                {
                    Id = Guid.NewGuid(),
                    FeatureFlagId = feature.Id,
                    OverrideType = dto.OverrideType,
                    TargetId = dto.TargetId,
                    IsEnabled = dto.IsEnabled
                });
            }

            // Invalidate cached evaluations for this feature/target combination
            await InvalidateFeatureCache(BuildCacheKey(
                key,
                dto.OverrideType == FeatureOverrideType.User ? dto.TargetId : null,
                dto.OverrideType == FeatureOverrideType.Group ? dto.TargetId : null));
        }

        /// <summary>
        /// Removes an override rule from a feature flag.
        /// </summary>
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

            // Clear related cached evaluations
            await InvalidateFeatureCache(BuildCacheKey(
                key,
                type == FeatureOverrideType.User ? targetId : null,
                type == FeatureOverrideType.Group ? targetId : null));
        }

        /// <summary>
        /// Updates the global default state of a feature flag.
        /// </summary>
        public async Task UpdateGlobalStateAsync(string key, bool isEnabled)
        {
            var feature = await featureRepo.GetByKeyWithOverridesAsync(key)
                          ?? throw new NotFoundException($"Feature '{key}' not found");

            feature.IsEnabled = isEnabled;
            await featureRepo.UpdateAsync(feature);

            // Invalidate base cache entry for the feature
            await InvalidateFeatureCache(key);
        }

        /// <summary>
        /// Maps a domain FeatureFlag entity to its DTO representation.
        /// </summary>
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

        /// <summary>
        /// Maps a FeatureFlag DTO back to its domain entity.
        /// </summary>
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

        /// <summary>
        /// Builds a cache key uniquely identifying an evaluation context.
        /// </summary>
        private static string BuildCacheKey(string key, string? userId, string? groupId)
        {
            var result = $"{CachePrefix}{key}";
            if (!string.IsNullOrWhiteSpace(groupId))
                result += $":group:{groupId}";
            if (!string.IsNullOrWhiteSpace(userId))
                result += $":user:{userId}";
            return result;
        }

        /// <summary>
        /// Removes cached evaluation entries for a feature.
        /// </summary>
        private async Task InvalidateFeatureCache(string key)
        {
            await cache.RemoveAsync($"{CachePrefix}{key}");
        }
    }
}
