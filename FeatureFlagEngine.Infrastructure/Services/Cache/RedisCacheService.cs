using FeatureFlagEngine.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FeatureFlagEngine.Infrastructure.Services.Cache
{
    /// <summary>
    /// Redis-based implementation of <see cref="IRedisCacheService"/> using <see cref="IDistributedCache"/>.
    /// Handles JSON serialization and deserialization for cached objects.
    /// </summary>
    public class RedisCacheService(IDistributedCache cache) : IRedisCacheService
    {
        /// <summary>
        /// Retrieves a cached value by key and deserializes it to the specified type.
        /// </summary>
        /// <typeparam name="T">Expected return type.</typeparam>
        /// <param name="key">Unique cache key.</param>
        /// <returns>Deserialized value if found; otherwise default.</returns>
        public async Task<T?> GetAsync<T>(string key)
        {
            var data = await cache.GetStringAsync(key);

            // Return default if key does not exist in cache
            return data is null ? default : JsonSerializer.Deserialize<T>(data);
        }

        /// <summary>
        /// Stores a value in Redis cache with optional expiration.
        /// </summary>
        /// <typeparam name="T">Type of value being cached.</typeparam>
        /// <param name="key">Unique cache key.</param>
        /// <param name="value">Value to serialize and store.</param>
        /// <param name="expiry">
        /// Optional expiration time. Defaults to 10 minutes if not specified.
        /// </param>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                // Sets absolute expiration relative to now
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(10)
            };

            var json = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, json, options);
        }

        /// <summary>
        /// Removes a cached entry from Redis.
        /// </summary>
        /// <param name="key">Cache key to remove.</param>
        public async Task RemoveAsync(string key)
        {
            await cache.RemoveAsync(key);
        }
    }
}
