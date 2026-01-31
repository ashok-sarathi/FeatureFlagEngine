using FeatureFlagEngine.Application.Interfaces.Repositories;
using FeatureFlagEngine.Application.Interfaces.Services;
using FeatureFlagEngine.Domain.Dtos.FeatureFlag;
using FeatureFlagEngine.Domain.Dtos.FeatureOverride;
using FeatureFlagEngine.Domain.Entities;
using FeatureFlagEngine.Domain.Enums;
using FeatureFlagEngine.Domain.Exceptions;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Tests.Services
{
    public class FeatureFlagServiceTests
    {
        private readonly Mock<IFeatureFlagRepository> _featureRepo = new();
        private readonly Mock<IFeatureOverrideRepository> _overrideRepo = new();
        private readonly Mock<IRedisCacheService> _cache = new();

        private readonly FeatureFlagService _service;

        public FeatureFlagServiceTests()
        {
            _service = new FeatureFlagService(_featureRepo.Object, _overrideRepo.Object, _cache.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenKeyExists()
        {
            var dto = new FeatureFlagDto { Key = "f1" };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1"))
                .ReturnsAsync(new FeatureFlag());

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task CreateAsync_ShouldGenerateId_AndSave()
        {
            var dto = new FeatureFlagDto { Key = "f1" };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1"))
                .ReturnsAsync((FeatureFlag?)null);

            _featureRepo.Setup(r => r.AddAsync(It.IsAny<FeatureFlag>()))
                .Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(dto);

            result.Id.Should().NotBe(Guid.Empty);
            _featureRepo.Verify(r => r.AddAsync(It.IsAny<FeatureFlag>()), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithOverrides_ShouldCallSpecialRepo()
        {
            var entities = new List<FeatureFlag> { new() { Id = Guid.NewGuid(), Key = "f1", Overrides = [] } };

            _featureRepo.Setup(r => r.GetAllWithOverridesAsync()).ReturnsAsync(entities);

            var result = await _service.GetAllAsync(true);

            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task EvaluateAsync_ShouldReturnFromCache_WhenExists()
        {
            _cache.Setup(c => c.GetAsync<bool?>("feature_eval:f1")).ReturnsAsync(true);

            var (value, fromCache) = await _service.EvaluateAsync("f1");

            value.Should().BeTrue();
            fromCache.Should().BeTrue();
            _featureRepo.Verify(r => r.GetByKeyWithOverridesAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EvaluateAsync_ShouldUseUserOverride_First()
        {
            var feature = new FeatureFlag
            {
                Key = "f1",
                IsEnabled = false,
                Overrides =
                [
                    new FeatureOverride { OverrideType = FeatureOverrideType.User, TargetId = "u1", IsEnabled = true }
                ]
            };

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>())).ReturnsAsync((bool?)null);
            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1")).ReturnsAsync(feature);

            var (value, fromCache) = await _service.EvaluateAsync("f1", "u1");

            value.Should().BeTrue();
            fromCache.Should().BeFalse();
            _cache.Verify(c => c.SetAsync(It.IsAny<string>(), true, It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task EvaluateAsync_ShouldUseGroupOverride_IfNoUser()
        {
            var feature = new FeatureFlag
            {
                Key = "f1",
                IsEnabled = false,
                Overrides =
                [
                    new FeatureOverride { OverrideType = FeatureOverrideType.Group, TargetId = "g1", IsEnabled = true }
                ]
            };

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>())).ReturnsAsync((bool?)null);
            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1")).ReturnsAsync(feature);

            var (value, _) = await _service.EvaluateAsync("f1", null, "g1");

            value.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateAsync_ShouldFallbackToGlobal()
        {
            var feature = new FeatureFlag { Key = "f1", IsEnabled = true, Overrides = [] };

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>())).ReturnsAsync((bool?)null);
            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1")).ReturnsAsync(feature);

            var (value, _) = await _service.EvaluateAsync("f1");

            value.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateAsync_ShouldThrow_WhenFeatureNotFound()
        {
            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>())).ReturnsAsync((bool?)null);
            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1")).ReturnsAsync((FeatureFlag?)null);

            Func<Task> act = async () => await _service.EvaluateAsync("f1");

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task AddOverride_ShouldUpdate_WhenExists()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "f1",
                Overrides =
                [
                    new FeatureOverride { Id = Guid.NewGuid(), OverrideType = FeatureOverrideType.User, TargetId = "u1", IsEnabled = false }
                ]
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1")).ReturnsAsync(feature);

            await _service.AddOverrideAsync("f1", new FeatureOverrideDto
            {
                OverrideType = FeatureOverrideType.User,
                TargetId = "u1",
                IsEnabled = true
            });

            _overrideRepo.Verify(r => r.UpdateAsync(It.IsAny<FeatureOverride>()), Times.Once);
        }

        [Fact]
        public async Task AddOverride_ShouldInsert_WhenNotExists()
        {
            var feature = new FeatureFlag { Id = Guid.NewGuid(), Key = "f1", Overrides = [] };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1")).ReturnsAsync(feature);

            await _service.AddOverrideAsync("f1", new FeatureOverrideDto
            {
                OverrideType = FeatureOverrideType.User,
                TargetId = "u1",
                IsEnabled = true
            });

            _overrideRepo.Verify(r => r.AddAsync(It.IsAny<FeatureOverride>()), Times.Once);
        }

        [Fact]
        public async Task RemoveOverride_ShouldThrow_WhenNotFound()
        {
            var feature = new FeatureFlag { Key = "f1", Overrides = [] };
            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1")).ReturnsAsync(feature);

            Func<Task> act = async () => await _service.RemoveOverrideAsync("f1", FeatureOverrideType.User, "u1");

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task RemoveOverride_ShouldRemove_AndUpdate()
        {
            var overrideItem = new FeatureOverride { OverrideType = FeatureOverrideType.User, TargetId = "u1" };
            var feature = new FeatureFlag { Key = "f1", Overrides = [overrideItem] };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1")).ReturnsAsync(feature);

            await _service.RemoveOverrideAsync("f1", FeatureOverrideType.User, "u1");

            _featureRepo.Verify(r => r.UpdateAsync(feature), Times.Once);
        }

        [Fact]
        public async Task UpdateGlobalState_ShouldUpdateFeature()
        {
            var feature = new FeatureFlag { Key = "f1", IsEnabled = false };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1")).ReturnsAsync(feature);

            await _service.UpdateGlobalStateAsync("f1", true);

            feature.IsEnabled.Should().BeTrue();
            _featureRepo.Verify(r => r.UpdateAsync(feature), Times.Once);
        }
    }
}
