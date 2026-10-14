using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Anonyme Nutzungsmessung (Wunsch Ingo 2026-10-14): Aufrufe und Nutzertage je Seite, ohne Namen.
/// Je Tag und Seite wird nur ein Hash des Benutzers gespeichert. Der Schluessel dafuer gilt einen Tag und liegt nur im
/// Arbeitsspeicher; nach dem Tageswechsel (oder einem Neustart) laesst sich niemand mehr zurueckrechnen. Damit zaehlt die
/// Auswertung „wie viele verschiedene Personen pro Tag“, nie „wer“. Ingos eigene Aufrufe zaehlen nicht mit.
/// Gepuffert und einmal pro Minute geschrieben; Tabellen legt der Dienst selbst an. Statisch, damit Program.cs unberuehrt bleibt.
/// Auswertung: Components/Pages/AdminUsage.razor, PageUsageReport.
/// </summary>
public static class PageUsageTracker
{
    /// <summary>Eigene Aufrufe des Betreuers verfaelschen die Zahlen und werden nicht gezaehlt.</summary>
    public static readonly HashSet<string> ExcludedUsers = new(StringComparer.OrdinalIgnoreCase) { "koi" };

    public const string AllRoutes = "*";

    private static readonly ConcurrentQueue<(string Day, string Route, string Key)> Buffer = new();
    private static readonly object SaltGate = new();
    private static string _saltDay = string.Empty;
    private static byte[] _salt = [];
    private static Timer? _timer;
    private static IDbContextFactory<AppDbContext>? _factory;
    private static int _flushing;

    /// <summary>Einen Seitenaufruf vormerken. Ohne Anmeldung oder fuer ausgenommene Konten geschieht nichts.</summary>
    public static void Record(string? windowsName, string relativePath, IDbContextFactory<AppDbContext> factory, DateTime? now = null)
    {
        if (string.IsNullOrWhiteSpace(windowsName)) return;
        var user = ShortName(windowsName);
        if (ExcludedUsers.Contains(user)) return;
        var day = (now ?? DateTime.Now).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Buffer.Enqueue((day, NormalizeRoute(relativePath), UserKey(day, user)));
        _factory ??= factory;
        _timer ??= new Timer(_ => Flush(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>„TRAFAG\koi“ und „koi@trafag.com“ ergeben „koi“.</summary>
    public static string ShortName(string name)
    {
        var s = name.Trim();
        var slash = s.LastIndexOf('\\');
        if (slash >= 0) s = s[(slash + 1)..];
        var at = s.IndexOf('@');
        if (at > 0) s = s[..at];
        return s.ToLowerInvariant();
    }

    /// <summary>
    /// Seite ohne Abfrage und Anker, hoechstens zwei Pfadteile, Zahlen und lange Kennungen als {id}; Startseite „/“.
    /// Die Unterseiten des Management-Cockpits (?section=) zaehlen gemeinsam.
    /// </summary>
    public static string NormalizeRoute(string relativePath)
    {
        var p = (relativePath ?? string.Empty).Split('?', '#')[0].Trim('/').ToLowerInvariant();
        if (p.Length == 0) return "/";
        var parts = p.Split('/', StringSplitOptions.RemoveEmptyEntries).Take(2)
            .Select(s => s.All(char.IsAsciiDigit) || (s.Length >= 16 && s.All(char.IsAsciiLetterOrDigit) && s.Any(char.IsAsciiDigit)) ? "{id}" : s);
        return string.Join('/', parts);
    }

    private static string UserKey(string day, string user)
    {
        byte[] salt;
        lock (SaltGate)
        {
            if (_saltDay != day)
            {
                _saltDay = day;
                _salt = RandomNumberGenerator.GetBytes(32);
            }
            salt = _salt;
        }
        var hash = HMACSHA256.HashData(salt, Encoding.UTF8.GetBytes(user));
        return Convert.ToHexString(hash, 0, 8);
    }

    public const string Schema = """
        CREATE TABLE IF NOT EXISTS UsagePageViews (Day TEXT NOT NULL, Route TEXT NOT NULL, Views INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (Day, Route));
        CREATE TABLE IF NOT EXISTS UsagePageUsers (Day TEXT NOT NULL, Route TEXT NOT NULL, UserKey TEXT NOT NULL, PRIMARY KEY (Day, Route, UserKey));
        """;

    /// <summary>Puffer in die Datenbank schreiben (auch fuer Tests direkt aufrufbar).</summary>
    public static void Flush(IDbContextFactory<AppDbContext>? factory = null)
    {
        factory ??= _factory;
        if (factory is null || Buffer.IsEmpty || Interlocked.Exchange(ref _flushing, 1) == 1) return;
        try
        {
            var items = new List<(string Day, string Route, string Key)>();
            while (Buffer.TryDequeue(out var item)) items.Add(item);
            if (items.Count == 0) return;
            using var db = factory.CreateDbContext();
            var con = (SqliteConnection)db.Database.GetDbConnection();
            con.Open();
            using var tx = con.BeginTransaction();
            Exec(con, tx, Schema);
            foreach (var g in items.GroupBy(i => (i.Day, i.Route)))
                Exec(con, tx, "INSERT INTO UsagePageViews (Day, Route, Views) VALUES ($d, $r, $n) ON CONFLICT(Day, Route) DO UPDATE SET Views = Views + $n",
                    ("$d", g.Key.Day), ("$r", g.Key.Route), ("$n", g.Count()));
            foreach (var (day, route, key) in items.SelectMany(i => new[] { i, (i.Day, AllRoutes, i.Key) }).Distinct())
                Exec(con, tx, "INSERT OR IGNORE INTO UsagePageUsers (Day, Route, UserKey) VALUES ($d, $r, $k)", ("$d", day), ("$r", route), ("$k", key));
            tx.Commit();
        }
        catch (Exception)
        {
            // Messung darf die App nie stoeren; ein verlorener Puffer kostet nur ein paar Zaehler.
        }
        finally
        {
            Interlocked.Exchange(ref _flushing, 0);
        }
    }

    private static void Exec(SqliteConnection con, SqliteTransaction tx, string sql, params (string Name, object Value)[] args)
    {
        using var cmd = con.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
        cmd.ExecuteNonQuery();
    }
}

/// <summary>Eine Zeile der Auswertung: Seite mit Aufrufen und Nutzertagen (Summe der verschiedenen Personen je Tag).</summary>
public sealed record PageUsageRow(string Route, string Title, int Views7, int UserDays7, int Views30, int UserDays30, string LastDay);

/// <summary>Personen pro Tag ueber alle Seiten.</summary>
public sealed record PageUsageDay(string Day, int Users, int Views);

public static class PageUsageReport
{
    public static (List<PageUsageRow> Rows, List<PageUsageDay> Days) Load(AppDbContext db, DateOnly today, IReadOnlyDictionary<string, string> titles)
    {
        var con = (SqliteConnection)db.Database.GetDbConnection();
        if (con.State != System.Data.ConnectionState.Open) con.Open();
        using (var create = con.CreateCommand())
        {
            create.CommandText = PageUsageTracker.Schema;
            create.ExecuteNonQuery();
        }
        var from30 = today.AddDays(-29).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var from7 = today.AddDays(-6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var views = new List<(string Day, string Route, int Views)>();
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = "SELECT Day, Route, Views FROM UsagePageViews WHERE Day >= $f";
            cmd.Parameters.AddWithValue("$f", from30);
            using var r = cmd.ExecuteReader();
            while (r.Read()) views.Add((r.GetString(0), r.GetString(1), r.GetInt32(2)));
        }
        var users = new List<(string Day, string Route, int Users)>();
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = "SELECT Day, Route, COUNT(*) FROM UsagePageUsers WHERE Day >= $f GROUP BY Day, Route";
            cmd.Parameters.AddWithValue("$f", from30);
            using var r = cmd.ExecuteReader();
            while (r.Read()) users.Add((r.GetString(0), r.GetString(1), r.GetInt32(2)));
        }
        return (Build(views, users, from7, titles), Days(views, users, today));
    }

    internal static List<PageUsageRow> Build(List<(string Day, string Route, int Views)> views, List<(string Day, string Route, int Users)> users,
        string from7, IReadOnlyDictionary<string, string> titles)
    {
        return views.Select(v => v.Route).Distinct()
            .Select(route => new PageUsageRow(
                route,
                titles.TryGetValue(route, out var t) ? t : route,
                views.Where(v => v.Route == route && string.CompareOrdinal(v.Day, from7) >= 0).Sum(v => v.Views),
                users.Where(u => u.Route == route && string.CompareOrdinal(u.Day, from7) >= 0).Sum(u => u.Users),
                views.Where(v => v.Route == route).Sum(v => v.Views),
                users.Where(u => u.Route == route).Sum(u => u.Users),
                views.Where(v => v.Route == route).Max(v => v.Day)))
            .OrderByDescending(r => r.UserDays30).ThenByDescending(r => r.Views30)
            .ToList();
    }

    internal static List<PageUsageDay> Days(List<(string Day, string Route, int Views)> views, List<(string Day, string Route, int Users)> users, DateOnly today)
        => Enumerable.Range(0, 30).Select(i => today.AddDays(i - 29).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Select(d => new PageUsageDay(d,
                users.Where(u => u.Day == d && u.Route == PageUsageTracker.AllRoutes).Sum(u => u.Users),
                views.Where(v => v.Day == d).Sum(v => v.Views)))
            .ToList();
}
