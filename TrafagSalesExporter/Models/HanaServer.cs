using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace TrafagSalesExporter.Models;

public class HanaServer
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 30015;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Name der Tenant-Datenbank bei Multi-Tenant Database Container (MDC) Setups.
    /// Leer lassen, wenn direkt auf einen Tenant-Port verbunden wird.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// SSL/TLS Verschlüsselung aktivieren (encrypt=true).
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// SSL-Zertifikat validieren. Bei self-signed Zertifikaten auf false setzen.
    /// </summary>
    public bool ValidateCertificate { get; set; }

    /// <summary>
    /// Zusätzliche Verbindungsparameter (Semikolon-getrennt), z.B. "sslCryptoProvider=openssl".
    /// </summary>
    public string AdditionalParams { get; set; } = string.Empty;

    public string BuildConnectionString()
    {
        var builder = new DbConnectionStringBuilder();
        builder["ServerNode"] = BuildServerNode();
        builder["UserName"] = Username.Trim();
        builder["Password"] = Password;

        if (!string.IsNullOrWhiteSpace(DatabaseName))
            builder["DatabaseName"] = DatabaseName.Trim();

        if (UseSsl)
        {
            builder["encrypt"] = true;
            builder["sslValidateCertificate"] = ValidateCertificate;
        }

        AppendAdditionalParams(builder);

        return builder.ConnectionString;
    }

    public string GetConnectionStringPreview()
    {
        var pwdMasked = string.IsNullOrEmpty(Password) ? "" : "***";
        var copy = new HanaServer
        {
            Host = Host,
            Port = Port,
            Username = Username,
            Password = pwdMasked,
            DatabaseName = DatabaseName,
            UseSsl = UseSsl,
            ValidateCertificate = ValidateCertificate,
            AdditionalParams = AdditionalParams
        };

        return copy.BuildConnectionString();
    }

    private string BuildServerNode()
    {
        var normalizedHost = NormalizeHost(Host);
        if (string.IsNullOrWhiteSpace(normalizedHost))
            throw new InvalidOperationException("HANA Host darf nicht leer sein.");

        if (HasExplicitPort(normalizedHost))
            return normalizedHost;

        return $"{normalizedHost}:{Port}";
    }

    private static string NormalizeHost(string host)
    {
        var value = host.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        }

        var schemeIndex = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex >= 0)
            value = value[(schemeIndex + 3)..];

        var slashIndex = value.IndexOf('/');
        if (slashIndex >= 0)
            value = value[..slashIndex];

        return value.Trim();
    }

    private static bool HasExplicitPort(string host)
    {
        if (host.StartsWith('['))
            return host.Contains("]:", StringComparison.Ordinal);

        return host.Count(c => c == ':') == 1;
    }

    private void AppendAdditionalParams(DbConnectionStringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(AdditionalParams))
            return;

        foreach (var rawPart in AdditionalParams.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = rawPart.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == rawPart.Length - 1)
                continue;

            var key = rawPart[..separatorIndex].Trim();
            var value = rawPart[(separatorIndex + 1)..].Trim();
            if (key.Length == 0)
                continue;

            builder[key] = value;
        }
    }
}

