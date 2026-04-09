using System.Security.Cryptography;
using System.Text;

namespace TrafagSalesExporter.Services;

public class CryptoService
{
    public string Encrypt(string plainText)
    {
        var input = Encoding.UTF8.GetBytes(plainText ?? string.Empty);
        var protectedBytes = ProtectedData.Protect(input, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return string.Empty;
        }

        var input = Convert.FromBase64String(cipherText);
        var unprotectedBytes = ProtectedData.Unprotect(input, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(unprotectedBytes);
    }
}
