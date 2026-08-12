using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrafagSalesExporter.Services;

internal static class AccessPasswordSettingsWriter
{
    private static readonly object FileLock = new();

    public static string HashPassword(string password)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    public static void SavePasswordHash(string contentRootPath, string sectionName, string passwordHash)
    {
        var path = Path.Combine(contentRootPath, "appsettings.json");

        lock (FileLock)
        {
            var json = File.Exists(path)
                ? File.ReadAllText(path, Encoding.UTF8)
                : "{}";

            var root = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            var section = root[sectionName] as JsonObject;
            if (section is null)
            {
                section = new JsonObject();
                root[sectionName] = section;
            }

            section["PasswordHash"] = passwordHash;
            section["Password"] = string.Empty;

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, root.ToJsonString(options), new UTF8Encoding(false));
        }
    }
}
