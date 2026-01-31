using FeatureFlagEngine.Application.Interfaces.Services;
using FeatureFlagEngine.Infrastructure.Services.Cache;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FeatureFlagEngine.Infrastructure.Tests.Services.Cache
{
    public class RedisCacheServiceTests
    {
        private readonly Mock<IDistributedCache> _cacheMock = new();
        private readonly IRedisCacheService _service;

        public RedisCacheServiceTests()
        {
            _service = new RedisCacheService(_cacheMock.Object);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnDefault_WhenKeyMissing()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[]?)null);

            // Act
            var result = await _service.GetAsync<int>("missing");

            // Assert
            result.Should().Be(0); // default(int)
        }

        [Fact]
        public async Task GetAsync_ShouldReturnDeserializedObject_WhenExists()
        {
            // Arrange
            var sample = new SampleClass { Id = 1, Name = "test" };
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(sample));

            _cacheMock.Setup(c => c.GetAsync("key1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(bytes);

            // Act
            var result = await _service.GetAsync<SampleClass>("key1");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("test");
        }

        [Fact]
        public async Task SetAsync_ShouldSerializeObjectAndCallCache()
        {
            // Arrange
            var sample = new SampleClass { Id = 2, Name = "abc" };

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()
            )).Returns(Task.CompletedTask);

            // Act
            await _service.SetAsync("key2", sample, TimeSpan.FromMinutes(5));

            // Assert
            _cacheMock.Verify(c => c.SetAsync(
                "key2",
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b).Contains("abc")),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow.HasValue && o.AbsoluteExpirationRelativeToNow.Value.TotalMinutes == 5),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SetAsync_ShouldUseDefaultExpiry_WhenNotProvided()
        {
            // Arrange
            var value = 123;

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()
            )).Returns(Task.CompletedTask);

            // Act
            await _service.SetAsync("key3", value);

            // Assert
            _cacheMock.Verify(c => c.SetAsync(
                "key3",
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow.HasValue && o.AbsoluteExpirationRelativeToNow.Value.TotalMinutes == 10),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_ShouldCallCacheRemove()
        {
            // Arrange
            _cacheMock.Setup(c => c.RemoveAsync("key4", It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            // Act
            await _service.RemoveAsync("key4");

            // Assert
            _cacheMock.Verify(c => c.RemoveAsync("key4", It.IsAny<CancellationToken>()), Times.Once);
        }

        private class SampleClass
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
    }
}
