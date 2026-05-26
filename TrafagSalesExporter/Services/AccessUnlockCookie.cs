using System.Security.Cryptography;
using System.Text;

namespace TrafagSalesExporter.Services;

internal static class AccessUnlockCookie
{
    public const string FinanceCookieName = "TrafagFinanceUnlocked";
    public const string AdminCookieName = "TrafagAdminUnlocked";
    public const string HrCookieName = "TrafagHrUnlocked";

    public static bool IsUnlocked(HttpContext? httpContext, string cookieName, string passwordHash)
    {
        if (httpContext is null ||
            string.IsNullOrWhiteSpace(passwordHash) ||
            !httpContext.Request.Cookies.TryGetValue(cookieName, out var value))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(value),
            Encoding.UTF8.GetBytes(CreateValue(cookieName, passwordHash)));
    }

    public static void SetUnlocked(HttpContext httpContext, string cookieName, string passwordHash)
    {
        httpContext.Response.Cookies.Append(cookieName, CreateValue(cookieName, passwordHash), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = httpContext.Request.IsHttps,
            Path = string.IsNullOrWhiteSpace(httpContext.Request.PathBase) ? "/" : httpContext.Request.PathBase.Value!,
            Expires = DateTimeOffset.UtcNow.AddHours(12)
        });
    }

    private static string CreateValue(string cookieName, string passwordHash)
    {
        var input = $"TrafagSalesExporter|{cookieName}|{passwordHash.Trim()}";
        return AccessPasswordSettingsWriter.HashPassword(input);
    }
}
