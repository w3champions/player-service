using player_service_net_test4.Models;

namespace player_service_net_test4.Hubs;

public class ConnectionMapping
{
    private readonly Dictionary<string, User> _connections = new Dictionary<string, User>();

    public List<User> GetUsers()
    {
        lock (_connections)
        {
            return _connections.Values.Select(v => v).OrderBy(r => r.BattleTag).ToList();
        }
    }

    public void Add(string connectionId, User user)
    {
        lock (_connections)
        {
            if (!_connections.ContainsKey(connectionId))
            {
                _connections.Add(connectionId, user);
            }
        }
    }

    public User? GetUser(string connectionId)
    {
        lock (_connections)
        {
            _connections.TryGetValue(connectionId, out User? user);
            return user;
        }
    }

    public void Remove(string connectionId)
    {
        lock (_connections)
        {
            _connections.Remove(connectionId);
        }
    }
}
