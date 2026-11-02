namespace Chat.Server.Services;

public sealed class ChatUsersService()
{
    private readonly object _gate = new();

    private readonly Dictionary<string, string> _users = new();

    public bool TryRegister(string connectionId, string name)
    {
        lock (_gate)
        {
            if (_users.TryGetValue(connectionId, out var currentName))
                return string.Equals(
                    currentName,
                    name,
                    StringComparison.OrdinalIgnoreCase);

            bool occupied = _users.Values.Any(user =>
                string.Equals(
                    user,
                    name,
                    StringComparison.OrdinalIgnoreCase));

            if (occupied)
                return false;

            _users.Add(connectionId, name);
            return true;
        }
    }

    public string? GetName(string connectionId)
    {
        lock (_gate)
        {
            return _users.GetValueOrDefault(connectionId);
        }
    }

    public void Remove(string connectionId)
    {
        lock (_gate)
        {
            _users.Remove(connectionId);
        }
    }
}