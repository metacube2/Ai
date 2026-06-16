using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Security;

namespace TrafagSalesExporter.Services;

public interface IHrKpiAccessService
{
    bool IsEnabled { get; }
    bool IsConfigured { get; }
    bool IsUnlocked { get; }
    bool TryUnlock(string username, string password);
    bool TryChangePassword(string username, string currentPassword, string newPassword);
    void Lock();
}

public sealed class HrKpiAccessService : IHrKpiAccessService, IDisposable
{
    private readonly HrKpiAccessOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAccessSessionTracker _sessionTracker;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    public HrKpiAccessService(
        IOptions<HrKpiAccessOptions> options,
        IHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        IAccessSessionTracker sessionTracker)
    {
        _options = options.Value;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _sessionTracker = sessionTracker;
    }

    public bool IsEnabled => _options.Enabled;

    public bool IsConfigured =>
        !IsEnabled ||
        IsPrimaryUserConfigured ||
        _options.AdminUsers.Any(IsUserConfigured);

    public bool IsUnlocked =>
        _isUnlocked ||
        IsCookieUnlocked();

    private bool _isUnlocked;

    public bool TryUnlock(string username, string password)
    {
        if (!IsEnabled)
        {
            _isUnlocked = true;
            return true;
        }

        if (!IsConfigured ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password))
        {
            return false;
        }

        var valid = MatchesPrimaryUser(username, password) ||
            _options.AdminUsers.Any(user => MatchesUser(user, username, password));

        _isUnlocked = valid;
        if (valid)
            _sessionTracker.Register(_sessionId, "HR KPI", username.Trim(), GetRemoteAddress());
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
            !MatchesPrimaryUser(username, currentPassword))
        {
            return false;
        }

        var passwordHash = AccessPasswordSettingsWriter.HashPassword(newPassword);
        AccessPasswordSettingsWriter.SavePasswordHash(_environment.ContentRootPath, HrKpiAccessOptions.SectionName, passwordHash);
        _options.PasswordHash = passwordHash;
        _options.Password = string.Empty;
        _isUnlocked = true;
        _sessionTracker.Register(_sessionId, "HR KPI", username.Trim(), GetRemoteAddress());
        return true;
    }

    public void Dispose()
    {
        _sessionTracker.Unregister(_sessionId);
    }

    private string? GetRemoteAddress()
        => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private bool IsCookieUnlocked()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (AccessUnlockCookie.IsUnlocked(httpContext, AccessUnlockCookie.HrCookieName, _options.PasswordHash))
            return true;

        return _options.AdminUsers
            .Where(user => !string.IsNullOrWhiteSpace(user.PasswordHash))
            .Any(user => AccessUnlockCookie.IsUnlocked(httpContext, AccessUnlockCookie.HrCookieName, user.PasswordHash));
    }

    private bool IsPrimaryUserConfigured =>
        !string.IsNullOrWhiteSpace(_options.Username) &&
        (!string.IsNullOrWhiteSpace(_options.PasswordHash) || !string.IsNullOrEmpty(_options.Password));

    private static bool IsUserConfigured(HrKpiAccessUserOptions user)
        => !string.IsNullOrWhiteSpace(user.Username) &&
            (!string.IsNullOrWhiteSpace(user.PasswordHash) || !string.IsNullOrEmpty(user.Password));

    private bool MatchesPrimaryUser(string username, string password)
        => IsPrimaryUserConfigured &&
            FixedEquals(username.Trim(), _options.Username.Trim()) &&
            MatchesPassword(password, _options.PasswordHash, _options.Password);

    private static bool MatchesUser(HrKpiAccessUserOptions user, string username, string password)
        => IsUserConfigured(user) &&
            FixedEquals(username.Trim(), user.Username.Trim()) &&
            MatchesPassword(password, user.PasswordHash, user.Password);

    private static bool MatchesPassword(string password, string configuredHash, string configuredPassword)
        => !string.IsNullOrWhiteSpace(configuredHash)
            ? VerifyPasswordHash(password, configuredHash)
            : FixedEquals(password, configuredPassword);

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
