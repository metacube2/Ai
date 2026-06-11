using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Security;

namespace TrafagSalesExporter.Services;

public interface IFinanceCockpitAccessService
{
    bool IsEnabled { get; }
    bool IsConfigured { get; }
    bool IsUnlocked { get; }
    bool TryUnlock(string username, string password);
    bool TryChangePassword(string username, string currentPassword, string newPassword);
    void Lock();
}

public sealed class FinanceCockpitAccessService : IFinanceCockpitAccessService, IDisposable
{
    private readonly FinanceCockpitAccessOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAccessSessionTracker _sessionTracker;
    private readonly ILogger<FinanceCockpitAccessService> _logger;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    public FinanceCockpitAccessService(
        IOptions<FinanceCockpitAccessOptions> options,
        IHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        IAccessSessionTracker sessionTracker,
        ILogger<FinanceCockpitAccessService> logger)
    {
        _options = options.Value;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _sessionTracker = sessionTracker;
        _logger = logger;
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
            AccessUnlockCookie.FinanceCookieName,
            _options.PasswordHash);

    private bool _isUnlocked;

    public bool TryUnlock(string username, string password)
    {
        if (!IsEnabled)
        {
            _isUnlocked = true;
            _logger.LogInformation("Finance Cockpit access unlocked because FinanceCockpitAccess is disabled.");
            return true;
        }

        if (!IsConfigured ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password) ||
            !FixedEquals(username.Trim(), _options.Username.Trim()))
        {
            _logger.LogWarning(
                "Finance Cockpit unlock failed before password check. IsConfigured={IsConfigured}, HasUsername={HasUsername}, PasswordLength={PasswordLength}, UsernameMatches={UsernameMatches}",
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
            "Finance Cockpit password check completed. Success={Success}, Username={Username}, PasswordLength={PasswordLength}, UsesHash={UsesHash}",
            valid,
            username.Trim(),
            password.Length,
            !string.IsNullOrWhiteSpace(_options.PasswordHash));
        if (valid)
            _sessionTracker.Register(_sessionId, "Finance Cockpit", username.Trim(), GetRemoteAddress());
        return valid;
    }

    public void Lock()
    {
        _isUnlocked = false;
        _sessionTracker.Unregister(_sessionId);
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
        AccessPasswordSettingsWriter.SavePasswordHash(_environment.ContentRootPath, FinanceCockpitAccessOptions.SectionName, passwordHash);
        _options.PasswordHash = passwordHash;
        _options.Password = string.Empty;
        _isUnlocked = true;
        _sessionTracker.Register(_sessionId, "Finance Cockpit", username.Trim(), GetRemoteAddress());
        return true;
    }

    public void Dispose()
    {
        _sessionTracker.Unregister(_sessionId);
    }

    private string? GetRemoteAddress()
        => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

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
