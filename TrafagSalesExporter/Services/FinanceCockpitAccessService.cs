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
    void Lock();
}

public sealed class FinanceCockpitAccessService : IFinanceCockpitAccessService
{
    private readonly FinanceCockpitAccessOptions _options;

    public FinanceCockpitAccessService(IOptions<FinanceCockpitAccessOptions> options)
    {
        _options = options.Value;
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
        return valid;
    }

    public void Lock() => IsUnlocked = false;

    private static bool VerifyPasswordHash(string password, string configuredHash)
    {
        var passwordHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
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
