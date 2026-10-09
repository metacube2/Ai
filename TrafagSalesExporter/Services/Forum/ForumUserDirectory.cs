using System.Collections.Concurrent;
using System.DirectoryServices;

namespace TrafagSalesExporter.Services.Forum;

/// <summary>
/// Anzeigename für den Windows-Login (TRAFAG\koi -> "Ingo Kohler") aus dem Active Directory, je Login
/// einen Tag gemerkt. Faellt AD aus, steht der Kurzname da; der Beitrag speichert den Namen beim Schreiben.
/// </summary>
public sealed class ForumUserDirectory
{
    private readonly ConcurrentDictionary<string, (string Name, DateTime ReadAt)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ForumUserDirectory> _logger;

    public ForumUserDirectory(ILogger<ForumUserDirectory> logger)
    {
        _logger = logger;
    }

    public static string ShortName(string? login)
    {
        var name = (login ?? string.Empty).Trim();
        var separator = name.LastIndexOf('\\');
        if (separator >= 0)
            name = name[(separator + 1)..];
        var at = name.IndexOf('@');
        if (at > 0)
            name = name[..at];
        return name.ToLowerInvariant();
    }

    public async Task<string> DisplayNameAsync(string? login)
    {
        var shortName = ShortName(login);
        if (shortName.Length == 0)
            return string.Empty;
        if (_cache.TryGetValue(shortName, out var hit) && hit.ReadAt > DateTime.UtcNow.AddDays(-1))
            return hit.Name;

        var name = shortName;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var found = await Task.Run(() => OperatingSystem.IsWindows() ? Lookup(shortName) : null).WaitAsync(TimeSpan.FromSeconds(5));
                if (!string.IsNullOrWhiteSpace(found))
                    name = found.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Trafag Reddit: Anzeigename für {Login} nicht aus AD lesbar: {Message}", shortName, ex.Message);
            }
        }

        _cache[shortName] = (name, DateTime.UtcNow);
        return name;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string? Lookup(string shortName)
    {
        using var root = new DirectoryEntry();
        var escaped = shortName.Replace("\\", "\\5c").Replace("*", "\\2a").Replace("(", "\\28").Replace(")", "\\29");
        using var searcher = new DirectorySearcher(root, $"(&(objectCategory=person)(sAMAccountName={escaped}))", ["displayName"])
        {
            SizeLimit = 1,
            ClientTimeout = TimeSpan.FromSeconds(4)
        };
        var result = searcher.FindOne();
        return result?.Properties["displayName"].Count > 0 ? Convert.ToString(result.Properties["displayName"][0]) : null;
    }
}
