using System.Collections.Concurrent;

namespace OverflowBackend.Services.Interface
{
    public interface IConnectionManager
    {
        ConcurrentDictionary<string, string> UserConnections { get; }
    }
}
