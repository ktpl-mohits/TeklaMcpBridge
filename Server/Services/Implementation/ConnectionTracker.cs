using Server.Services.Interface;
using System.Collections.Concurrent;
namespace Server.Services.Implementation
{
    public class ConnectionTracker : IConnectionTracker
    {
        private readonly ConcurrentDictionary<string, string> _connections = new();
        public string? GetConnectionId(string userId)
        {
            return _connections.TryGetValue(userId, out var connectionId) ? connectionId : null;
        }

        public void Register(string userId, string connectionId)
        {
            _connections.TryAdd(userId, connectionId);
        }

        public void Unregister(string userId, string connectionId)
        {
            _connections.TryRemove(KeyValuePair.Create(userId, connectionId));
        }
    }
}
