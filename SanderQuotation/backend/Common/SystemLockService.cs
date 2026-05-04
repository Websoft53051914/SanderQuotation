using Microsoft.Extensions.Caching.Memory;

namespace backend.Common
{
    public class SystemLockService : ISystemLockService
    {
        private readonly IMemoryCache _cache;

        public SystemLockService(IMemoryCache cache)
        {
            _cache = cache;
        }

        private string Key(string userId) => $"SYSTEM_LOCK_{userId}";

        public bool IsLocked(string userId)
            => _cache.TryGetValue(Key(userId), out _);

        public void Lock(string userId)
            => _cache.Set(Key(userId), true);

        public void Unlock(string userId)
            => _cache.Remove(Key(userId));
    }

}
