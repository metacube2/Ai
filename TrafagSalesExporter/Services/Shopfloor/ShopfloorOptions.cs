using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Einstellungen des Reiters "Operations (Shopfloor)" (appsettings, Abschnitt "Shopfloor").
/// Doku: docs/SHOPFLOOR_2026-10-07.md.
/// </summary>
public sealed class ShopfloorOptions
{
    public const string SectionName = "Shopfloor";

    /// <summary>Pfad der SQLite-Datei. Leer = &lt;ContentRoot&gt;/shopfloor/shopfloor.db.</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>Ordner der Tagessicherungen. Leer = "backups" neben der Datenbank.</summary>
    public string BackupDirectory { get; set; } = string.Empty;

    public int BackupKeepDays { get; set; } = 60;

    /// <summary>Bearer-Token fuer POST /shopfloor/api/integration/push. Leer = Schnittstelle gesperrt.</summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>Windows-Konten mit Zugriff (mit oder ohne Domaene, ohne Gross-/Kleinschreibung). Leer = niemand.</summary>
    public List<string> AllowedUsers { get; set; } = [];

    /// <summary>Login fuer alle anderen (2026-10-08, analog Finance/HR): Benutzername.</summary>
    public string LoginUsername { get; set; } = "operations";

    /// <summary>SHA-256-Hash des Passworts (Hex, wie HR KPI). Leer = Login ausgeschaltet, nur AllowedUsers.</summary>
    public string LoginPasswordHash { get; set; } = string.Empty;

    /// <summary>Gueltigkeit der Anmeldung in Stunden.</summary>
    public int LoginHours { get; set; } = 12;

    /// <summary>true = alle angemeldeten Benutzer. Erst nach Freigabe durch die Leitung Produktion setzen.</summary>
    public bool OpenForAll { get; set; }

    /// <summary>Adresse der Oberflaeche fuer Links in Mails, z. B. https://server/BiDashboard/shopfloor/index.html.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>SAP-Abgleich (ShopZd05Set, ShopAufSet). Erst einschalten, wenn T76K912718 in P76 ist.</summary>
    public bool SapSyncEnabled { get; set; }

    /// <summary>Uhrzeiten des Abgleichs (HH:mm, Werktage).</summary>
    public List<string> SapSyncTimes { get; set; } = ["05:45", "12:45"];

    public string SapPlant { get; set; } = "1100";

    /// <summary>Disponenten fuer ZD05 (je Disponent ein Aufruf, wie die Selektion im Report).</summary>
    public List<string> Zd05Dispo { get; set; } = ["001", "003"];

    /// <summary>Tage ab heute fuer Plan- und Fertigungsauftraege (Forecast).</summary>
    public int ForecastDays { get; set; } = 28;
}

public interface IShopfloorAccess
{
    bool IsAllowed(ClaimsPrincipal? user);
    string EditorName(ClaimsPrincipal? user);
}

/// <summary>
/// Zugriffsregel: neue Reiter sind erst nach Freigabe durch die Leitung Produktion sichtbar.
/// Ohne Eintrag in AllowedUsers und ohne OpenForAll hat niemand Zugriff.
/// </summary>
public sealed class ShopfloorAccess : IShopfloorAccess
{
    private readonly IOptionsMonitor<ShopfloorOptions> _options;

    public ShopfloorAccess(IOptionsMonitor<ShopfloorOptions> options) => _options = options;

    public bool IsAllowed(ClaimsPrincipal? user) => IsAllowed(_options.CurrentValue, user?.Identity?.Name);

    public string EditorName(ClaimsPrincipal? user) => EditorName(user?.Identity?.Name);

    public static bool IsAllowed(ShopfloorOptions options, string? windowsName)
    {
        if (options.OpenForAll)
            return true;
        if (string.IsNullOrWhiteSpace(windowsName))
            return false;
        var full = windowsName.Trim();
        var shortName = ShortName(full);
        foreach (var entry in options.AllowedUsers)
        {
            var e = (entry ?? string.Empty).Trim();
            if (e.Length == 0)
                continue;
            // Eintrag "DOM\\koi" muss exakt passen, Eintrag "koi" passt auf jede Domaene.
            if (string.Equals(e, full, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e, shortName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>"TRAFAG\\koi" und "koi@trafag.com" ergeben "koi".</summary>
    public static string ShortName(string name)
    {
        var s = name.Trim();
        var slash = s.LastIndexOf('\\');
        if (slash >= 0)
            s = s[(slash + 1)..];
        var at = s.IndexOf('@');
        if (at > 0)
            s = s[..at];
        return s;
    }

    /// <summary>Kennung fuer updated_by/created_by: Windows-Konto ohne Domaene, klein geschrieben, maximal 40 Zeichen.</summary>
    public static string EditorName(string? windowsName)
    {
        if (string.IsNullOrWhiteSpace(windowsName))
            return "anonym";
        var s = ShortName(windowsName).ToLowerInvariant();
        return s.Length > 40 ? s[..40] : s;
    }
}
