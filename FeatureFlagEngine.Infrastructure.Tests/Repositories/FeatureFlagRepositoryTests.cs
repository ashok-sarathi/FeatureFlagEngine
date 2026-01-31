using FeatureFlagEngine.Domain.Entities;
using FeatureFlagEngine.Infrastructure.Contexts;
using FeatureFlagEngine.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Tests.Repositories
{
    public class FeatureFlagRepositoryTests
    {
        private static FeatureFlagDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<FeatureFlagDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new FeatureFlagDbContext(options);
        }

        [Fact]
        public async Task GetByKeyWithOverridesAsync_ShouldReturnFeature_WithOverrides()
        {
            await using var context = CreateDbContext();

            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Key = "f1",
                IsEnabled = true,
                Overrides =
                [
                    new FeatureOverride { Id = Guid.NewGuid(), TargetId = "u1", IsEnabled = false }
                ]
            };

            context.FeatureFlags.Add(feature);
            await context.SaveChangesAsync();

            var repo = new FeatureFlagRepository(context);

            var result = await repo.GetByKeyWithOverridesAsync("f1");

            result.Should().NotBeNull();
            result!.Overrides.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByKeyWithOverridesAsync_ShouldReturnNull_WhenNotFound()
        {
            await using var context = CreateDbContext();
            var repo = new FeatureFlagRepository(context);

            var result = await repo.GetByKeyWithOverridesAsync("missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllWithOverridesAsync_ShouldReturnAllFeatures_WithOverridesLoaded()
        {
            await using var context = CreateDbContext();

            context.FeatureFlags.AddRange(
                new FeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Key = "f1",
                    Overrides = [new FeatureOverride { Id = Guid.NewGuid(), TargetId = "u1" }]
                },
                new FeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Key = "f2",
                    Overrides = []
                });

            await context.SaveChangesAsync();

            var repo = new FeatureFlagRepository(context);

            var result = await repo.GetAllWithOverridesAsync();

            result.Should().HaveCount(2);
            result.First(f => f.Key == "f1").Overrides.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddAsync_ShouldPersistEntity()
        {
            await using var context = CreateDbContext();
            var repo = new FeatureFlagRepository(context);

            var feature = new FeatureFlag { Id = Guid.NewGuid(), Key = "f1" };

            await repo.AddAsync(feature);

            context.FeatureFlags.Should().ContainSingle(f => f.Key == "f1");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyEntity()
        {
            await using var context = CreateDbContext();

            var feature = new FeatureFlag { Id = Guid.NewGuid(), Key = "f1", IsEnabled = false };
            context.FeatureFlags.Add(feature);
            await context.SaveChangesAsync();

            var repo = new FeatureFlagRepository(context);

            feature.IsEnabled = true;
            await repo.UpdateAsync(feature);

            var updated = await context.FeatureFlags.FindAsync(feature.Id);
            updated!.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEntity()
        {
            await using var context = CreateDbContext();

            var feature = new FeatureFlag { Id = Guid.NewGuid(), Key = "f1" };
            context.FeatureFlags.Add(feature);
            await context.SaveChangesAsync();

            var repo = new FeatureFlagRepository(context);

            await repo.DeleteAsync(feature);

            context.FeatureFlags.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenEntityExists()
        {
            await using var context = CreateDbContext();

            var feature = new FeatureFlag { Id = Guid.NewGuid(), Key = "f1" };
            context.FeatureFlags.Add(feature);
            await context.SaveChangesAsync();

            var repo = new FeatureFlagRepository(context);

            var exists = await repo.ExistsAsync(feature.Id);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenEntityMissing()
        {
            await using var context = CreateDbContext();
            var repo = new FeatureFlagRepository(context);

            var exists = await repo.ExistsAsync(Guid.NewGuid());

            exists.Should().BeFalse();
        }
    }
}
