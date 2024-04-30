namespace OverflowBackend.Services.Interface
{
    public interface IRedisCacheService
    {

        public void GetRandomValueFromQueue(string key);

        public bool UserExistsInQueue(string key);

        public void AddToQueue(string username);

        public void RemoveFromQueue(string username);

    }
}
