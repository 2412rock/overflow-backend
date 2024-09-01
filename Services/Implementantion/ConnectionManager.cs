

using OverflowBackend.Services.Interface;
using System.Collections.Concurrent;

namespace OverflowBackend.Services.Implementantion
{
    public class ConnectionManager : IConnectionManager
    {
        public ConcurrentDictionary<string, string> UserConnections { get; } = new ConcurrentDictionary<string, string>();
    }
}
