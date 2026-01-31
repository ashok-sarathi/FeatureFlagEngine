using FeatureFlagEngine.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FeatureFlagEngine.Infrastructure.Services.Cache
{
    public class RedisCacheService(IDistributedCache cache) : IRedisCacheService
    {
        public async Task<T?> GetAsync<T>(string key)
        {
            var data = await cache.GetStringAsync(key);
            return data is null ? default : JsonSerializer.Deserialize<T>(data);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(10)
            };

            var json = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, json, options);
        }

        public async Task RemoveAsync(string key)
        {
            await cache.RemoveAsync(key);
        }
    }
}
