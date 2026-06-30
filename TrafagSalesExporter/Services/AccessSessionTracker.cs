using System.Collections.Concurrent;

namespace TrafagSalesExporter.Services;

public interface IAccessSessionTracker
{
    IReadOnlyList<AccessSessionSnapshot> GetActiveSessions();
    void Register(string sessionId, string area, string username, string? remoteAddress);
    void Touch(string sessionId);
    void Unregister(string sessionId);
}

public sealed class AccessSessionTracker : IAccessSessionTracker
{
    private readonly ConcurrentDictionary<string, AccessSessionSnapshot> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<AccessSessionSnapshot> GetActiveSessions()
        => _sessions.Values
            .OrderByDescending(session => session.LastSeenAt)
            .ToList();

    public void Register(string sessionId, string area, string username, string? remoteAddress)
    {
        var now = DateTimeOffset.Now;
        _sessions[sessionId] = new AccessSessionSnapshot(
            sessionId,
            area,
            username,
            string.IsNullOrWhiteSpace(remoteAddress) ? "unbekannt" : remoteAddress,
            now,
            now);
    }

    public void Touch(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return;

        _sessions[sessionId] = session with { LastSeenAt = DateTimeOffset.Now };
    }

    public void Unregister(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}

public sealed record AccessSessionSnapshot(
    string SessionId,
    string Area,
    string Username,
    string RemoteAddress,
    DateTimeOffset StartedAt,
    DateTimeOffset LastSeenAt);
