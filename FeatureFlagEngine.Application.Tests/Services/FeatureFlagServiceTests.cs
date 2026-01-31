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
            var dto = new FeatureFlagDto { Key = "f1", IsEnabled = true, Description = "test", Overrides = [new() { OverrideType = FeatureOverrideType.User, TargetId = "t1", IsEnabled = false }] };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("f1"))
                .ReturnsAsync(new FeatureFlag());

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task CreateAsync_ShouldGenerateId_AndSave()
        {
            var dto = new FeatureFlagDto { Key = "f1", IsEnabled = true, Description = "test", Overrides = [new() { OverrideType = FeatureOverrideType.User, TargetId = "t1", IsEnabled = false }] };

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
            var entities = new List<FeatureFlag> { new() { Id = Guid.NewGuid(), Key = "f1", Overrides = [
                    new FeatureOverride { OverrideType = FeatureOverrideType.User, TargetId = "u1", IsEnabled = true }
                ] } };

            _featureRepo.Setup(r => r.GetAllWithOverridesAsync()).ReturnsAsync(entities);

            var result = await _service.GetAllAsync(true);

            result.Should().HaveCount(1);
            result[0].Overrides.Should().HaveCount(1);
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

        [Fact]
        public async Task EvaluateAsync_ShouldReturnRegionOverride_WhenRegionMatch()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = false,
                Overrides =
                [
                    new FeatureOverride
            {
                OverrideType = FeatureOverrideType.Region,
                TargetId = "IN",
                IsEnabled = true
            }
                ]
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                           .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                     .ReturnsAsync((bool?)null);

            // Act
            var (result, fromCache) = await _service.EvaluateAsync("NewDashboard", null, null, "IN");

            // Assert
            result.Should().BeTrue();
            fromCache.Should().BeFalse();
        }

        [Fact]
        public async Task EvaluateAsync_ShouldFallbackToGlobal_WhenRegionDoesNotMatch()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = true,
                Overrides =
                [
                    new FeatureOverride
            {
                OverrideType = FeatureOverrideType.Region,
                TargetId = "US",
                IsEnabled = false
            }
                ]
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                           .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                     .ReturnsAsync((bool?)null);

            var (result, _) = await _service.EvaluateAsync("NewDashboard", null, null, "IN");

            result.Should().BeTrue(); // global value
        }

        [Fact]
        public async Task EvaluateAsync_UserOverride_ShouldTakePriority_OverRegion()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = false,
                Overrides =
                [
                    new FeatureOverride
            {
                OverrideType = FeatureOverrideType.Region,
                TargetId = "IN",
                IsEnabled = false
            },
            new FeatureOverride
            {
                OverrideType = FeatureOverrideType.User,
                TargetId = "user123",
                IsEnabled = true
            }
                ]
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                           .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                     .ReturnsAsync((bool?)null);

            var (result, _) = await _service.EvaluateAsync("NewDashboard", "user123", null, "IN");

            result.Should().BeTrue(); // user override wins
        }

        [Fact]
        public async Task EvaluateAsync_ShouldUseRegionInCacheKey()
        {
            _cache.Setup(c => c.GetAsync<bool?>("feature_eval:NewDashboard:region:IN"))
                     .ReturnsAsync(true);

            var (result, fromCache) = await _service.EvaluateAsync("NewDashboard", null, null, "IN");

            result.Should().BeTrue();
            fromCache.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateAsync_ShouldCacheResult_WithRegionKey()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = true,
                Overrides = []
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                           .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                     .ReturnsAsync((bool?)null);

            await _service.EvaluateAsync("NewDashboard", null, null, "IN");

            _cache.Verify(c => c.SetAsync(
                "feature_eval:NewDashboard:region:IN",
                true,
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task EvaluateAsync_ShouldReturnUserOverride_WhenUserMatch()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = false,
                Overrides =
                [
                    new FeatureOverride
            {
                OverrideType = FeatureOverrideType.User,
                TargetId = "user123",
                IsEnabled = true
            }
                ]
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                        .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                  .ReturnsAsync((bool?)null);

            var (result, fromCache) = await _service.EvaluateAsync("NewDashboard", "user123", null, null);

            result.Should().BeTrue();
            fromCache.Should().BeFalse();
        }

        [Fact]
        public async Task EvaluateAsync_ShouldFallbackToGlobal_WhenUserDoesNotMatch()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = true,
                Overrides =
                [
                    new FeatureOverride
            {
                OverrideType = FeatureOverrideType.User,
                TargetId = "otherUser",
                IsEnabled = false
            }
                ]
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                        .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                  .ReturnsAsync((bool?)null);

            var (result, _) = await _service.EvaluateAsync("NewDashboard", "user123", null, null);

            result.Should().BeTrue(); // global fallback
        }

        [Fact]
        public async Task EvaluateAsync_ShouldUseUserInCacheKey()
        {
            _cache.Setup(c => c.GetAsync<bool?>("feature_eval:NewDashboard:user:user123"))
                  .ReturnsAsync(true);

            var (result, fromCache) = await _service.EvaluateAsync("NewDashboard", "user123", null, null);

            result.Should().BeTrue();
            fromCache.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateAsync_ShouldCacheResult_WithUserKey()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = true,
                Overrides = []
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                        .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                  .ReturnsAsync((bool?)null);

            await _service.EvaluateAsync("NewDashboard", "user123", null, null);

            _cache.Verify(c => c.SetAsync(
                "feature_eval:NewDashboard:user:user123",
                true,
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task EvaluateAsync_ShouldReturnGroupOverride_WhenGroupMatch()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = false,
                Overrides =
                [
                    new FeatureOverride
            {
                OverrideType = FeatureOverrideType.Group,
                TargetId = "admin",
                IsEnabled = true
            }
                ]
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                        .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                  .ReturnsAsync((bool?)null);

            var (result, _) = await _service.EvaluateAsync("NewDashboard", null, "admin", null);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateAsync_ShouldFallbackToGlobal_WhenGroupDoesNotMatch()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = false,
                Overrides =
                [
                    new FeatureOverride
            {
                OverrideType = FeatureOverrideType.Group,
                TargetId = "users",
                IsEnabled = true
            }
                ]
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                        .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                  .ReturnsAsync((bool?)null);

            var (result, _) = await _service.EvaluateAsync("NewDashboard", null, "admin", null);

            result.Should().BeFalse(); // global fallback
        }

        [Fact]
        public async Task EvaluateAsync_ShouldUseGroupInCacheKey()
        {
            _cache.Setup(c => c.GetAsync<bool?>("feature_eval:NewDashboard:group:admin"))
                  .ReturnsAsync(true);

            var (result, fromCache) = await _service.EvaluateAsync("NewDashboard", null, "admin", null);

            result.Should().BeTrue();
            fromCache.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateAsync_ShouldCacheResult_WithGroupKey()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "NewDashboard",
                IsEnabled = false,
                Overrides = []
            };

            _featureRepo.Setup(r => r.GetByKeyWithOverridesAsync("NewDashboard"))
                        .ReturnsAsync(feature);

            _cache.Setup(c => c.GetAsync<bool?>(It.IsAny<string>()))
                  .ReturnsAsync((bool?)null);

            await _service.EvaluateAsync("NewDashboard", null, "admin", null);

            _cache.Verify(c => c.SetAsync(
                "feature_eval:NewDashboard:group:admin",
                false,
                It.IsAny<TimeSpan>()), Times.Once);
        }
    }
}
