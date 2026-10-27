using System.DirectoryServices;

namespace TrafagSalesExporter.Modules.Handbestellwesen;

/// <summary>Person aus dem AD fuer das Mitarbeiterverzeichnis (Wunsch Ingo 2026-10-27: Namen aus dem AD ziehen).</summary>
public sealed record HbAdPerson(string Konto, string Vorname, string Nachname, string Mail, string Anzeige);

public interface IHbAdSuche
{
    Task<IReadOnlyList<HbAdPerson>> SucheAsync(string? begriff);
}

/// <summary>
/// AD-Suche wie <c>ForumUserDirectory.SearchAsync</c> (gleicher Filter, gleiche Maskierung, laeuft so produktiv unter dem
/// App-Pool-Konto), laedt aber zusaetzlich Vorname, Nachname und Mail. Nur aktive Personenkonten, hoechstens 15 Treffer.
/// </summary>
public sealed class HbAdSuche(ILogger<HbAdSuche> logger) : IHbAdSuche
{
    public async Task<IReadOnlyList<HbAdPerson>> SucheAsync(string? begriff)
    {
        begriff = (begriff ?? string.Empty).Trim();
        if (begriff.Length < 2 || !OperatingSystem.IsWindows())
            return [];
        try
        {
            return await Task.Run(() => OperatingSystem.IsWindows() ? Suche(begriff) : []).WaitAsync(TimeSpan.FromSeconds(6));
        }
        catch (Exception ex)
        {
            logger.LogInformation("Handbestellwesen: Personensuche im AD fehlgeschlagen: {Message}", ex.Message);
            return [];
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static List<HbAdPerson> Suche(string begriff)
    {
        using var root = new DirectoryEntry();
        var maskiert = begriff.Replace("\\", "\\5c").Replace("*", "\\2a").Replace("(", "\\28").Replace(")", "\\29");
        var filter = $"(&(objectCategory=person)(objectClass=user)(anr={maskiert})(!(userAccountControl:1.2.840.113556.1.4.803:=2)))";
        using var searcher = new DirectorySearcher(root, filter, ["sAMAccountName", "givenName", "sn", "mail", "displayName"])
        {
            SizeLimit = 15,
            ClientTimeout = TimeSpan.FromSeconds(5)
        };
        var liste = new List<HbAdPerson>();
        using var treffer = searcher.FindAll();
        foreach (SearchResult r in treffer)
        {
            string Wert(string name) => r.Properties[name].Count > 0 ? Convert.ToString(r.Properties[name][0]) ?? "" : "";
            var konto = Wert("sAMAccountName").ToLowerInvariant();
            if (konto.Length == 0) continue;
            var anzeige = Wert("displayName");
            liste.Add(new HbAdPerson(konto, Wert("givenName"), Wert("sn"), Wert("mail"), anzeige.Length > 0 ? anzeige : konto));
        }
        return liste.OrderBy(x => x.Anzeige, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
