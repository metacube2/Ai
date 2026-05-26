using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Security;

namespace TrafagSalesExporter.Services;

public interface IAdminAccessService
{
    bool IsEnabled { get; }
    bool IsConfigured { get; }
    bool IsUnlocked { get; }
    bool TryUnlock(string username, string password);
    bool TryChangePassword(string username, string currentPassword, string newPassword);
    void Lock();
}

public sealed class AdminAccessService : IAdminAccessService
{
    private readonly AdminAccessOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AdminAccessService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminAccessService(
        IOptions<AdminAccessOptions> options,
        IHostEnvironment environment,
        ILogger<AdminAccessService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsEnabled => _options.Enabled;

    public bool IsConfigured =>
        !IsEnabled ||
        !string.IsNullOrWhiteSpace(_options.Username) &&
        (!string.IsNullOrWhiteSpace(_options.PasswordHash) || !string.IsNullOrEmpty(_options.Password));

    public bool IsUnlocked =>
        _isUnlocked ||
        AccessUnlockCookie.IsUnlocked(
            _httpContextAccessor.HttpContext,
            AccessUnlockCookie.AdminCookieName,
            _options.PasswordHash);

    private bool _isUnlocked;

    public bool TryUnlock(string username, string password)
    {
        if (!IsEnabled)
        {
            _isUnlocked = true;
            _logger.LogInformation("Admin access unlocked because AdminAccess is disabled.");
            return true;
        }

        if (!IsConfigured ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password) ||
            !FixedEquals(username.Trim(), _options.Username.Trim()))
        {
            _logger.LogWarning(
                "Admin access unlock failed before password check. IsConfigured={IsConfigured}, HasUsername={HasUsername}, PasswordLength={PasswordLength}, UsernameMatches={UsernameMatches}",
                IsConfigured,
                !string.IsNullOrWhiteSpace(username),
                password?.Length ?? 0,
                !string.IsNullOrWhiteSpace(username) && FixedEquals(username.Trim(), _options.Username.Trim()));
            return false;
        }

        var valid = !string.IsNullOrWhiteSpace(_options.PasswordHash)
            ? VerifyPasswordHash(password, _options.PasswordHash)
            : FixedEquals(password, _options.Password);

        _isUnlocked = valid;
        _logger.Log(
            valid ? LogLevel.Information : LogLevel.Warning,
            "Admin access password check completed. Success={Success}, Username={Username}, PasswordLength={PasswordLength}, UsesHash={UsesHash}",
            valid,
            username.Trim(),
            password.Length,
            !string.IsNullOrWhiteSpace(_options.PasswordHash));
        return valid;
    }

    public bool TryChangePassword(string username, string currentPassword, string newPassword)
    {
        if (!IsEnabled ||
            !IsConfigured ||
            string.IsNullOrWhiteSpace(newPassword) ||
            newPassword.Length < 8 ||
            !TryUnlock(username, currentPassword))
        {
            return false;
        }

        var passwordHash = AccessPasswordSettingsWriter.HashPassword(newPassword);
        AccessPasswordSettingsWriter.SavePasswordHash(_environment.ContentRootPath, AdminAccessOptions.SectionName, passwordHash);
        _options.PasswordHash = passwordHash;
        _options.Password = string.Empty;
        _isUnlocked = true;
        return true;
    }

    public void Lock() => _isUnlocked = false;

    private static bool VerifyPasswordHash(string password, string configuredHash)
    {
        var passwordHash = AccessPasswordSettingsWriter.HashPassword(password);
        return FixedEquals(passwordHash, configuredHash.Trim());
    }

    private static bool FixedEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
            CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
