using StackExchange.Redis;

namespace OverflowBackend.Services.Interface
{
    public interface IRedisService
    {
        public IDatabase GetDatabase(int index);
    }
}
