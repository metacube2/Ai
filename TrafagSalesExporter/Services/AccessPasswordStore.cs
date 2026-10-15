using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Security;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Passwoerter der Bereichs-Logins deploy-fest in der Cockpit-DB (Wunsch Ingo 2026-10-15: der Admin setzt die
/// Passwoerter aller Bereiche an einer Stelle). Vorher lagen die Hashes nur in appsettings.json, die jeder Deploy
/// ueberschreibt (docs/rag/DEPLOYMENT.md, Falle Admin-Schalter). Gespeichert wird nur der SHA-256-Hash.
/// Ein Eintrag hier hat Vorrang vor appsettings; fehlt er, gilt appsettings weiter.
/// </summary>
public static class AccessPasswordAreas
{
    public const string Admin = "admin";
    public const string Finance = "finance";
    public const string Hr = "hr";
    public const string Operations = "operations";
    public const string Logistics = "logistik";

    /// <summary>Zusatzkonten HR KPI (HrKpiAccess:AdminUsers), Schluessel "hr:&lt;benutzer&gt;".</summary>
    public const string HrUserPrefix = "hr:";

    /// <summary>Schalter "Operations: Login noetig" (Wunsch Fabio 2026-10-15). "0" = alle Windows-Benutzer sehen den Shopfloor.</summary>
    public const string OperationsLoginRequiredKey = "operations.login_required";

    public static string HrUser(string username) => HrUserPrefix + username.Trim().ToLowerInvariant();
}

public sealed record AccessPasswordEntry(string Area, string PasswordHash, DateTime ChangedUtc, string ChangedBy);

public interface IAccessPasswordStore
{
    IReadOnlyDictionary<string, AccessPasswordEntry> ReadAll();
    void Save(string area, string passwordHash, string changedBy);

    /// <summary>Zugangsschalter (2026-10-15), z. B. Operations ohne Login. Null = nicht gesetzt, dann gilt appsettings.</summary>
    string? ReadSetting(string key);
    void SaveSetting(string key, string value, string changedBy);
}

public sealed class AccessPasswordStore : IAccessPasswordStore
{
    private static readonly object Gate = new();
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;
    private readonly ILogger<AccessPasswordStore>? _logger;

    // Ohne Cockpit-DB (z. B. Shopfloor-Testhost) bleibt der Speicher leer, es gilt appsettings.
    public AccessPasswordStore(IDbContextFactory<AppDbContext>? dbFactory = null, ILogger<AccessPasswordStore>? logger = null)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    // Die Tabelle legt der Speicher selbst an, damit er nicht davon abhaengt, wann die Schemapflege laeuft.
    private const string CreateSql =
        "CREATE TABLE IF NOT EXISTS AccessPasswords (Area TEXT NOT NULL PRIMARY KEY, PasswordHash TEXT NOT NULL, ChangedUtc TEXT NOT NULL, ChangedBy TEXT NOT NULL DEFAULT '')";

    /// <summary>Liest alle Eintraege. Bei einem DB-Fehler leer, dann gilt appsettings (Login darf an der DB nicht scheitern).</summary>
    public IReadOnlyDictionary<string, AccessPasswordEntry> ReadAll()
    {
        var result = new Dictionary<string, AccessPasswordEntry>(StringComparer.OrdinalIgnoreCase);
        if (_dbFactory is null)
            return result;
        try
        {
            lock (Gate)
            {
                using var db = _dbFactory.CreateDbContext();
                var connection = db.Database.GetDbConnection();
                connection.Open();
                Execute(connection, CreateSql);
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Area, PasswordHash, ChangedUtc, ChangedBy FROM AccessPasswords";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var changed = DateTime.TryParse(reader.GetString(2), System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var parsed) ? parsed : DateTime.MinValue;
                    result[reader.GetString(0)] = new AccessPasswordEntry(reader.GetString(0), reader.GetString(1), changed, reader.GetString(3));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Bereichs-Passwoerter konnten nicht aus der DB gelesen werden, es gilt appsettings.");
            result.Clear();
        }
        return result;
    }

    public void Save(string area, string passwordHash, string changedBy)
    {
        if (_dbFactory is null)
            throw new InvalidOperationException("Cockpit-Datenbank nicht verfuegbar.");
        lock (Gate)
        {
            using var db = _dbFactory.CreateDbContext();
            var connection = db.Database.GetDbConnection();
            connection.Open();
            Execute(connection, CreateSql);
            using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO AccessPasswords (Area, PasswordHash, ChangedUtc, ChangedBy) VALUES ($area, $hash, $changed, $by) " +
                "ON CONFLICT(Area) DO UPDATE SET PasswordHash = excluded.PasswordHash, ChangedUtc = excluded.ChangedUtc, ChangedBy = excluded.ChangedBy";
            AddParameter(command, "$area", area.Trim().ToLowerInvariant());
            AddParameter(command, "$hash", passwordHash.Trim().ToUpperInvariant());
            AddParameter(command, "$changed", DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            AddParameter(command, "$by", changedBy ?? string.Empty);
            command.ExecuteNonQuery();
        }
    }

    private const string CreateSettingsSql =
        "CREATE TABLE IF NOT EXISTS AccessSettings (Key TEXT NOT NULL PRIMARY KEY, Value TEXT NOT NULL, ChangedUtc TEXT NOT NULL, ChangedBy TEXT NOT NULL DEFAULT '')";

    public string? ReadSetting(string key)
    {
        if (_dbFactory is null)
            return null;
        try
        {
            lock (Gate)
            {
                using var db = _dbFactory.CreateDbContext();
                var connection = db.Database.GetDbConnection();
                connection.Open();
                Execute(connection, CreateSettingsSql);
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Value FROM AccessSettings WHERE Key = $key";
                AddParameter(command, "$key", key);
                return command.ExecuteScalar() as string;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Zugangsschalter {Key} konnte nicht gelesen werden, es gilt appsettings.", key);
            return null;
        }
    }

    public void SaveSetting(string key, string value, string changedBy)
    {
        if (_dbFactory is null)
            throw new InvalidOperationException("Cockpit-Datenbank nicht verfuegbar.");
        lock (Gate)
        {
            using var db = _dbFactory.CreateDbContext();
            var connection = db.Database.GetDbConnection();
            connection.Open();
            Execute(connection, CreateSettingsSql);
            using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO AccessSettings (Key, Value, ChangedUtc, ChangedBy) VALUES ($key, $value, $changed, $by) " +
                "ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value, ChangedUtc = excluded.ChangedUtc, ChangedBy = excluded.ChangedBy";
            AddParameter(command, "$key", key);
            AddParameter(command, "$value", value);
            AddParameter(command, "$changed", DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            AddParameter(command, "$by", changedBy ?? string.Empty);
            command.ExecuteNonQuery();
        }
    }

    private static void Execute(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void AddParameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

/// <summary>
/// Legt die Hashes aus der DB ueber die Werte aus appsettings, bevor irgendein Login sie liest. So greift die Aenderung
/// auch in den Anmelde-Endpunkten in Program.cs (/access/admin, /access/finance, /access/hr), die die Optionen direkt lesen.
/// </summary>
internal sealed class AccessPasswordPostConfigure :
    IPostConfigureOptions<AdminAccessOptions>,
    IPostConfigureOptions<FinanceCockpitAccessOptions>,
    IPostConfigureOptions<HrKpiAccessOptions>,
    IPostConfigureOptions<ShopfloorOptions>,
    IPostConfigureOptions<LogisticsPeopleAccessOptions>
{
    private readonly IAccessPasswordStore _store;

    public AccessPasswordPostConfigure(IAccessPasswordStore store) => _store = store;

    public void PostConfigure(string? name, AdminAccessOptions options)
    {
        if (Find(AccessPasswordAreas.Admin) is { } hash) { options.PasswordHash = hash; options.Password = string.Empty; }
    }

    public void PostConfigure(string? name, FinanceCockpitAccessOptions options)
    {
        if (Find(AccessPasswordAreas.Finance) is { } hash) { options.PasswordHash = hash; options.Password = string.Empty; }
    }

    public void PostConfigure(string? name, HrKpiAccessOptions options)
    {
        var all = _store.ReadAll();
        if (all.TryGetValue(AccessPasswordAreas.Hr, out var primary)) { options.PasswordHash = primary.PasswordHash; options.Password = string.Empty; }
        foreach (var user in options.AdminUsers.Where(u => !string.IsNullOrWhiteSpace(u.Username)))
        {
            if (all.TryGetValue(AccessPasswordAreas.HrUser(user.Username), out var entry)) { user.PasswordHash = entry.PasswordHash; user.Password = string.Empty; }
        }
    }

    public void PostConfigure(string? name, ShopfloorOptions options)
    {
        if (Find(AccessPasswordAreas.Operations) is { } hash) options.LoginPasswordHash = hash;
        // Admin-Schalter hat Vorrang vor Shopfloor:OpenForAll aus appsettings
        switch (_store.ReadSetting(AccessPasswordAreas.OperationsLoginRequiredKey))
        {
            case "0": options.OpenForAll = true; break;
            case "1": options.OpenForAll = false; break;
        }
    }

    public void PostConfigure(string? name, LogisticsPeopleAccessOptions options)
    {
        if (Find(AccessPasswordAreas.Logistics) is { } hash) options.PasswordHash = hash;
    }

    private string? Find(string area) => _store.ReadAll().TryGetValue(area, out var entry) ? entry.PasswordHash : null;
}

public sealed record AccessPasswordAccount(string Area, string Title, string Username, bool Configured, DateTime? ChangedUtc, string? ChangedBy);

/// <summary>Admin-Seite "Passwoerter": alle Bereichs-Logins anzeigen und neu setzen, ohne das alte Passwort zu kennen.</summary>
public interface IAccessPasswordAdministration
{
    IReadOnlyList<AccessPasswordAccount> ListAccounts();
    bool TrySetPassword(string area, string newPassword, string changedBy);

    /// <summary>true = Operations nur mit Login oder freigegebenem Windows-Konto; false = alle Windows-Benutzer.</summary>
    bool OperationsLoginRequired { get; }
    bool TrySetOperationsLoginRequired(bool required, string changedBy);
}

public sealed class AccessPasswordAdministration : IAccessPasswordAdministration
{
    public const int MinimumLength = 8;

    private readonly IAccessPasswordStore _store;
    private readonly IAdminAccessService _adminAccess;
    private readonly IOptions<AdminAccessOptions> _admin;
    private readonly IOptions<FinanceCockpitAccessOptions> _finance;
    private readonly IOptions<HrKpiAccessOptions> _hr;
    private readonly IOptionsMonitor<ShopfloorOptions> _shopfloor;
    private readonly IOptionsMonitorCache<ShopfloorOptions> _shopfloorCache;
    private readonly IOptionsMonitor<LogisticsPeopleAccessOptions> _logistics;
    private readonly IOptionsMonitorCache<LogisticsPeopleAccessOptions> _logisticsCache;

    public AccessPasswordAdministration(
        IAccessPasswordStore store,
        IAdminAccessService adminAccess,
        IOptions<AdminAccessOptions> admin,
        IOptions<FinanceCockpitAccessOptions> finance,
        IOptions<HrKpiAccessOptions> hr,
        IOptionsMonitor<ShopfloorOptions> shopfloor,
        IOptionsMonitorCache<ShopfloorOptions> shopfloorCache,
        IOptionsMonitor<LogisticsPeopleAccessOptions> logistics,
        IOptionsMonitorCache<LogisticsPeopleAccessOptions> logisticsCache)
    {
        _store = store;
        _adminAccess = adminAccess;
        _admin = admin;
        _finance = finance;
        _hr = hr;
        _shopfloor = shopfloor;
        _shopfloorCache = shopfloorCache;
        _logistics = logistics;
        _logisticsCache = logisticsCache;
    }

    public IReadOnlyList<AccessPasswordAccount> ListAccounts()
    {
        var stored = _store.ReadAll();
        AccessPasswordAccount Account(string area, string title, string username, bool configured)
            => stored.TryGetValue(area, out var e)
                ? new AccessPasswordAccount(area, title, username, true, e.ChangedUtc, e.ChangedBy)
                : new AccessPasswordAccount(area, title, username, configured, null, null);

        var admin = _admin.Value;
        var finance = _finance.Value;
        var hr = _hr.Value;
        var shopfloor = _shopfloor.CurrentValue;
        var logistics = _logistics.CurrentValue;
        var list = new List<AccessPasswordAccount>
        {
            Account(AccessPasswordAreas.Admin, "Admin", admin.Username, HasPassword(admin.PasswordHash, admin.Password)),
            Account(AccessPasswordAreas.Finance, "Finance", finance.Username, HasPassword(finance.PasswordHash, finance.Password)),
            Account(AccessPasswordAreas.Hr, "HR KPI", hr.Username, HasPassword(hr.PasswordHash, hr.Password)),
        };
        list.AddRange(hr.AdminUsers
            .Where(u => !string.IsNullOrWhiteSpace(u.Username))
            .Select(u => Account(AccessPasswordAreas.HrUser(u.Username), "HR KPI", u.Username, HasPassword(u.PasswordHash, u.Password))));
        list.Add(Account(AccessPasswordAreas.Operations, "Operations", shopfloor.LoginUsername, !string.IsNullOrWhiteSpace(shopfloor.LoginPasswordHash)));
        list.Add(Account(AccessPasswordAreas.Logistics, "Logistik", logistics.Username, !string.IsNullOrWhiteSpace(logistics.PasswordHash)));
        return list;
    }

    public bool TrySetPassword(string area, string newPassword, string changedBy)
    {
        // Schutz auch auf dem Server, nicht nur in der Oberflaeche: nur ein entsperrter Admin darf setzen.
        if (!_adminAccess.IsUnlocked || string.IsNullOrEmpty(newPassword) || newPassword.Length < MinimumLength)
            return false;

        area = (area ?? string.Empty).Trim().ToLowerInvariant();
        if (!ListAccounts().Any(a => a.Area == area))
            return false;

        var hash = AccessPasswordSettingsWriter.HashPassword(newPassword);
        _store.Save(area, hash, changedBy);
        Apply(area, hash);
        return true;
    }

    public bool OperationsLoginRequired => !_shopfloor.CurrentValue.OpenForAll;

    public bool TrySetOperationsLoginRequired(bool required, string changedBy)
    {
        if (!_adminAccess.IsUnlocked)
            return false;
        _store.SaveSetting(AccessPasswordAreas.OperationsLoginRequiredKey, required ? "1" : "0", changedBy);
        _shopfloorCache.TryRemove(Options.DefaultName);   // naechster Zugriff baut neu, PostConfigure liest den Schalter
        return true;
    }

    /// <summary>Uebernimmt den neuen Hash sofort in die laufenden Optionen (ohne Neustart).</summary>
    private void Apply(string area, string hash)
    {
        switch (area)
        {
            case AccessPasswordAreas.Admin:
                _admin.Value.PasswordHash = hash; _admin.Value.Password = string.Empty; break;
            case AccessPasswordAreas.Finance:
                _finance.Value.PasswordHash = hash; _finance.Value.Password = string.Empty; break;
            case AccessPasswordAreas.Hr:
                _hr.Value.PasswordHash = hash; _hr.Value.Password = string.Empty; break;
            case AccessPasswordAreas.Operations:
                _shopfloorCache.TryRemove(Options.DefaultName); break;   // naechster Zugriff baut neu, PostConfigure liest die DB
            case AccessPasswordAreas.Logistics:
                _logisticsCache.TryRemove(Options.DefaultName); break;
            default:
                var user = _hr.Value.AdminUsers.FirstOrDefault(u => AccessPasswordAreas.HrUser(u.Username) == area);
                if (user is not null) { user.PasswordHash = hash; user.Password = string.Empty; }
                break;
        }
    }

    private static bool HasPassword(string hash, string password) => !string.IsNullOrWhiteSpace(hash) || !string.IsNullOrEmpty(password);

    /// <summary>Registrierung; aufgerufen aus AddShopfloor, solange Program.cs fremde unkommittierte Aenderungen traegt.</summary>
    public static IServiceCollection Register(IServiceCollection services)
    {
        services.AddSingleton<IAccessPasswordStore, AccessPasswordStore>();
        services.AddSingleton<AccessPasswordPostConfigure>();
        services.AddSingleton<IPostConfigureOptions<AdminAccessOptions>>(sp => sp.GetRequiredService<AccessPasswordPostConfigure>());
        services.AddSingleton<IPostConfigureOptions<FinanceCockpitAccessOptions>>(sp => sp.GetRequiredService<AccessPasswordPostConfigure>());
        services.AddSingleton<IPostConfigureOptions<HrKpiAccessOptions>>(sp => sp.GetRequiredService<AccessPasswordPostConfigure>());
        services.AddSingleton<IPostConfigureOptions<ShopfloorOptions>>(sp => sp.GetRequiredService<AccessPasswordPostConfigure>());
        services.AddSingleton<IPostConfigureOptions<LogisticsPeopleAccessOptions>>(sp => sp.GetRequiredService<AccessPasswordPostConfigure>());
        services.AddScoped<IAccessPasswordAdministration, AccessPasswordAdministration>();
        return services;
    }
}
