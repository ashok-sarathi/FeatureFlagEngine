using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Services
{
    /// <summary>
    /// Provides abstraction for distributed caching using Redis.
    /// Used to store and retrieve serialized data to improve performance
    /// and reduce repeated database or computation overhead.
    /// </summary>
    public interface IRedisCacheService
    {
        /// <summary>
        /// Retrieves a cached value by key.
        /// </summary>
        /// <typeparam name="T">Expected type of the cached value.</typeparam>
        /// <param name="key">Unique cache key.</param>
        /// <returns>
        /// Deserialized cached value if present; otherwise null when the key does not exist.
        /// </returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Stores a value in the cache.
        /// </summary>
        /// <typeparam name="T">Type of the value to cache.</typeparam>
        /// <param name="key">Unique cache key.</param>
        /// <param name="value">Value to be serialized and stored.</param>
        /// <param name="expiry">
        /// Optional expiration time. If not provided, the default cache expiration policy is used.
        /// </param>
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

        /// <summary>
        /// Removes a cached entry by key.
        /// </summary>
        /// <param name="key">Unique cache key to remove.</param>
        Task RemoveAsync(string key);
    }
}
