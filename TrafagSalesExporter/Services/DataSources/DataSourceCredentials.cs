using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.DataSources;

internal static class DataSourceCredentials
{
    public static (string Username, string Password) Resolve(Site site, SourceSystemDefinition sourceDefinition)
        => (FirstNonEmpty(site.UsernameOverride, sourceDefinition.CentralUsername),
            FirstNonEmpty(site.PasswordOverride, sourceDefinition.CentralPassword));

    public static string ResolveSapServiceUrl(Site site, SourceSystemDefinition sourceDefinition)
        => FirstNonEmpty(site.SapServiceUrl, sourceDefinition.CentralServiceUrl);

    public static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }
}
