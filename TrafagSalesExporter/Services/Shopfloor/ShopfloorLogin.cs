using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Login fuer den Reiter Operations (Shopfloor), 2026-10-08 (Wunsch Ingo: Reiter fuer alle sichtbar, Zugang mit eigenem
/// Login analog Finance/HR). Benutzer und Passwort-Hash (SHA-256, wie HR KPI) in Shopfloor:LoginUsername /
/// Shopfloor:LoginPasswordHash. Nach erfolgreicher Anmeldung setzt der Server ein HttpOnly-Cookie, das per Data Protection
/// geschuetzt ist und Shopfloor:LoginHours gilt. Es schuetzt Oberflaeche UND API, nicht nur die Cockpit-Seite.
/// Windows-Konten in Shopfloor:AllowedUsers brauchen kein Login.
/// </summary>
public static class ShopfloorLogin
{
    public const string CookieName = "TrafagShopfloor";
    private const string Purpose = "TrafagSalesExporter.Shopfloor.Login.v1";
    private const string Marker = "shopfloor-ok";

    public static bool IsConfigured(ShopfloorOptions options)
        => !string.IsNullOrWhiteSpace(options.LoginUsername) && !string.IsNullOrWhiteSpace(options.LoginPasswordHash);

    /// <summary>Benutzer ohne Gross-/Kleinschreibung, Passwort ueber den Hash, Vergleich in konstanter Zeit.</summary>
    public static bool Verify(ShopfloorOptions options, string? user, string? password)
    {
        if (!IsConfigured(options) || string.IsNullOrEmpty(password))
            return false;
        var userOk = string.Equals((user ?? string.Empty).Trim(), options.LoginUsername.Trim(), StringComparison.OrdinalIgnoreCase);
        var hash = AccessPasswordSettingsWriter.HashPassword(password);
        var hashOk = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(options.LoginPasswordHash.Trim().ToUpperInvariant()));
        return userOk & hashOk;
    }

    /// <summary>
    /// Das Cookie traegt einen Fingerabdruck des Passwort-Hashes (2026-10-15): setzt der Admin ein neues Passwort,
    /// sind alle bestehenden Anmeldungen ungueltig, wie bei Finance, HR und Admin.
    /// </summary>
    public static string CreateCookieValue(IDataProtectionProvider provider, DateTimeOffset expiresUtc, string passwordHash)
        => provider.CreateProtector(Purpose).Protect(
            Marker + "|" + expiresUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) + "|" + Fingerprint(passwordHash));

    public static bool IsValidCookie(IDataProtectionProvider provider, string? value, DateTimeOffset nowUtc, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            var plain = provider.CreateProtector(Purpose).Unprotect(value);
            var parts = plain.Split('|');
            return parts.Length == 3 && parts[0] == Marker &&
                   long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var exp) &&
                   DateTimeOffset.FromUnixTimeSeconds(exp) > nowUtc &&
                   CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(parts[2]), Encoding.UTF8.GetBytes(Fingerprint(passwordHash)));
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string Fingerprint(string passwordHash)
        => AccessPasswordSettingsWriter.HashPassword("shopfloor|" + (passwordHash ?? string.Empty).Trim().ToUpperInvariant())[..16];
}
