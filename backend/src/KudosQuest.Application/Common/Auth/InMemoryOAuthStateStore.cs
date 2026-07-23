using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace KudosQuest.Application.Common.Auth;

public sealed class InMemoryOAuthStateStore : IOAuthStateStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _states = new();

    public string Create()
    {
        Cleanup();
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _states[state] = DateTimeOffset.UtcNow.Add(Ttl);
        return state;
    }

    public bool TryConsume(string state)
    {
        Cleanup();

        if (!_states.TryRemove(state, out var expiresAt))
        {
            return false;
        }

        return expiresAt >= DateTimeOffset.UtcNow;
    }

    private void Cleanup()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var pair in _states)
        {
            if (pair.Value < now)
            {
                _states.TryRemove(pair.Key, out _);
            }
        }
    }
}
