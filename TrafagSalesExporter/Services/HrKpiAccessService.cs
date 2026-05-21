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
        !string.IsNullOrWhiteSpace(_options.Username) &&
        (!string.IsNullOrWhiteSpace(_options.PasswordHash) || !string.IsNullOrEmpty(_options.Password));

    public bool IsUnlocked { get; private set; }

    public bool TryUnlock(string username, string password)
    {
        if (!IsEnabled)
        {
            IsUnlocked = true;
            return true;
        }

        if (!IsConfigured ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password) ||
            !FixedEquals(username.Trim(), _options.Username.Trim()))
        {
            return false;
        }

        var valid = !string.IsNullOrWhiteSpace(_options.PasswordHash)
            ? VerifyPasswordHash(password, _options.PasswordHash)
            : FixedEquals(password, _options.Password);

        IsUnlocked = valid;
        if (valid)
            _sessionTracker.Register(_sessionId, "HR KPI", username.Trim(), GetRemoteAddress());
        return valid;
    }

    public void Lock()
    {
        IsUnlocked = false;
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
        AccessPasswordSettingsWriter.SavePasswordHash(_environment.ContentRootPath, HrKpiAccessOptions.SectionName, passwordHash);
        _options.PasswordHash = passwordHash;
        _options.Password = string.Empty;
        IsUnlocked = true;
        _sessionTracker.Register(_sessionId, "HR KPI", username.Trim(), GetRemoteAddress());
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
