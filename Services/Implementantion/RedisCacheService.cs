using OverflowBackend.Services.Interface;
using StackExchange.Redis;

namespace OverflowBackend.Services.Implementantion
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _redisDatabaseQueue;
        private readonly IDatabase _redisDatabaseMatches;
        private readonly IRedisService _redisService;

        public RedisCacheService(IRedisService redisService)
        {
            _redisService = redisService;
            _redisDatabaseQueue = redisService.GetDatabase(1);
            _redisDatabaseMatches = redisService.GetDatabase(2);
        }

        public string GetRandomValueFromQueue()
        {
            // Get a random key from Redis
            RedisKey randomKey = _redisDatabaseQueue.KeyRandom();

            // Get the value associated with the random key
            var value = _redisDatabaseQueue.StringGet(randomKey);
            
            RemoveFromQueue(randomKey);
            return value;
        }

        public bool UserExistsInQueue(string key)
        {
            return _redisDatabaseQueue.StringGet(key).HasValue;
        }

        public void AddToQueue(string username)
        {
            var match = GetRandomValueFromQueue();
            _redisDatabaseMatches.StringSet(match, username);
            _redisDatabaseQueue.StringSet(username, "");
            
        }
        

        public void RemoveFromQueue(string username)
        {
            _redisDatabaseQueue.KeyDelete(username);
        }

        public void GetRandomValueFromQueue(string key)
        {
            throw new NotImplementedException();
        }
    }
}
