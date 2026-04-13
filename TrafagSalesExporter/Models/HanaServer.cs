using System.ComponentModel.DataAnnotations;

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
        var parts = new List<string>
        {
            $"ServerNode={Host}:{Port}",
            $"UserName={Username}",
            $"Password={Password}"
        };

        if (!string.IsNullOrWhiteSpace(DatabaseName))
            parts.Add($"DatabaseName={DatabaseName}");

        if (UseSsl)
        {
            parts.Add("encrypt=true");
            parts.Add($"sslValidateCertificate={(ValidateCertificate ? "true" : "false")}");
        }

        if (!string.IsNullOrWhiteSpace(AdditionalParams))
            parts.Add(AdditionalParams.Trim().Trim(';'));

        return string.Join(";", parts);
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
}

