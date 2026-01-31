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
    public class FeatureOverrideRepositoryTests
    {
        private static FeatureFlagDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<FeatureFlagDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new FeatureFlagDbContext(options);
        }

        [Fact]
        public async Task AddAsync_ShouldPersistOverride()
        {
            await using var context = CreateDbContext();
            var repo = new FeatureOverrideRepository(context);

            var featureOverride = new FeatureOverride { Id = Guid.NewGuid(), TargetId = "u1", IsEnabled = true };

            await repo.AddAsync(featureOverride);

            context.FeatureOverrides.Should().ContainSingle(o => o.TargetId == "u1");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyOverride()
        {
            await using var context = CreateDbContext();

            var featureOverride = new FeatureOverride { Id = Guid.NewGuid(), TargetId = "u1", IsEnabled = false };
            context.FeatureOverrides.Add(featureOverride);
            await context.SaveChangesAsync();

            var repo = new FeatureOverrideRepository(context);

            featureOverride.IsEnabled = true;
            await repo.UpdateAsync(featureOverride);

            var updated = await context.FeatureOverrides.FindAsync(featureOverride.Id);
            updated!.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveOverride()
        {
            await using var context = CreateDbContext();

            var featureOverride = new FeatureOverride { Id = Guid.NewGuid(), TargetId = "u1" };
            context.FeatureOverrides.Add(featureOverride);
            await context.SaveChangesAsync();

            var repo = new FeatureOverrideRepository(context);

            await repo.DeleteAsync(featureOverride);

            context.FeatureOverrides.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnOverride_WhenExists()
        {
            await using var context = CreateDbContext();

            var featureOverride = new FeatureOverride { Id = Guid.NewGuid(), TargetId = "u1" };
            context.FeatureOverrides.Add(featureOverride);
            await context.SaveChangesAsync();

            var repo = new FeatureOverrideRepository(context);

            var result = await repo.GetByIdAsync(featureOverride.Id);

            result.Should().NotBeNull();
            result!.TargetId.Should().Be("u1");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            await using var context = CreateDbContext();
            var repo = new FeatureOverrideRepository(context);

            var result = await repo.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllOverrides()
        {
            await using var context = CreateDbContext();

            context.FeatureOverrides.AddRange(
                new FeatureOverride { Id = Guid.NewGuid(), TargetId = "u1" },
                new FeatureOverride { Id = Guid.NewGuid(), TargetId = "u2" });

            await context.SaveChangesAsync();

            var repo = new FeatureOverrideRepository(context);

            var list = await repo.GetAllAsync();

            list.Should().HaveCount(2);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenExists()
        {
            await using var context = CreateDbContext();

            var featureOverride = new FeatureOverride { Id = Guid.NewGuid(), TargetId = "u1" };
            context.FeatureOverrides.Add(featureOverride);
            await context.SaveChangesAsync();

            var repo = new FeatureOverrideRepository(context);

            var exists = await repo.ExistsAsync(featureOverride.Id);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenNotExists()
        {
            await using var context = CreateDbContext();

            var repo = new FeatureOverrideRepository(context);

            var exists = await repo.ExistsAsync(Guid.NewGuid());

            exists.Should().BeFalse();
        }
    }
}
