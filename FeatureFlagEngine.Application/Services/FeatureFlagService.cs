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
    public class FeatureFlagService(IFeatureFlagRepository featureRepo, IFeatureOverrideRepository featureOverrideRepo)
        : CommonService<FeatureFlag, FeatureFlagDto>(featureRepo), IFeatureFlagService
    {
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


        public async Task<bool> EvaluateAsync(string key, string? userId = null, string? groupId = null)
        {
            var feature = await featureRepo.GetByKeyWithOverridesAsync(key)
                          ?? throw new NotFoundException($"Feature '{key}' not found");

            var overrides = feature.Overrides;

            // 1️. User override (highest priority)
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userOverride = overrides.FirstOrDefault(o =>
                    o.OverrideType == FeatureOverrideType.User &&
                    o.TargetId == userId);

                if (userOverride != null)
                    return userOverride.IsEnabled;
            }

            // 2️. Group override
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                var groupOverride = overrides.FirstOrDefault(o =>
                    o.OverrideType == FeatureOverrideType.Group &&
                    o.TargetId == groupId);

                if (groupOverride != null)
                    return groupOverride.IsEnabled;
            }

            // 3️. Global default
            return feature.IsEnabled;
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
        }


        public async Task UpdateGlobalStateAsync(string key, bool isEnabled)
        {
            var feature = await featureRepo.GetByKeyWithOverridesAsync(key)
                          ?? throw new NotFoundException($"Feature '{key}' not found");

            feature.IsEnabled = isEnabled;
            await featureRepo.UpdateAsync(feature);
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
                Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                Key = dto.Key,
                IsEnabled = dto.IsEnabled,
                Description = dto.Description,
                Overrides = dto.Overrides.Select(o => new FeatureOverride
                {
                    Id = o.Id == Guid.Empty ? Guid.NewGuid() : o.Id,
                    OverrideType = o.OverrideType,
                    TargetId = o.TargetId,
                    IsEnabled = o.IsEnabled
                }).ToList()
            };
        }

    }
}
