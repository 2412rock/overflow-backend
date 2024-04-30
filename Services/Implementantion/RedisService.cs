using OverflowBackend.Services.Interface;
using StackExchange.Redis;
using System.Net;

namespace OverflowBackend.Services.Implementantion
{
    public class RedisService : IRedisService
    {
        private readonly ConnectionMultiplexer _redisConnection;

        public RedisService()
        {
            string hostIp = "";
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses("host.docker.internal");
                if (addresses.Length > 0)
                {
                    // we are running in docker
                    hostIp = addresses[0].ToString();
                }
                else
                {
                    // running locally
                    hostIp = Environment.GetEnvironmentVariable("HOST_IP");
                }
            }
            catch
            {
                hostIp = "192.168.1.125";
            }

            var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");

            var configurationOptions = new ConfigurationOptions
            {
                EndPoints = { $"{hostIp}:6379" },
                Password = redisPassword,
                // Add other configuration options as needed
                // For example, configurationOptions.Ssl = true; for SSL/TLS
            };

            _redisConnection = ConnectionMultiplexer.Connect(configurationOptions);
        }

        public IDatabase GetDatabase(int dbIndex)
        {
            // Ensure dbIndex is within valid range (0-15)
            if (dbIndex < 0 || dbIndex > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(dbIndex), "Database index must be between 0 and 15.");
            }

            return _redisConnection.GetDatabase(dbIndex);
        }
    }
}
