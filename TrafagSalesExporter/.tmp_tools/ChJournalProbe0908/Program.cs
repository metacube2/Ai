using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using TrafagSalesExporter.Data;

// Ein lesender Metadata-Aufruf, keine Wiederholung, keine Zugangsdaten oder Buchungswerte im Output.
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ChJournalProbe0908 <dbPath> <entitySet> [entitySet ...]");
    return 2;
}

var dbPath = Path.GetFullPath(args[0]);
var entitySets = args.Skip(1).Select(value => value.Trim()).ToArray();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath};Mode=ReadOnly;Default Timeout=30").Options;
await using var db = new AppDbContext(options);
var site = await db.Sites.AsNoTracking().SingleAsync(x => x.TSC == "ZSCHWEIZ");
var source = await db.SourceSystemDefinitions.AsNoTracking().SingleAsync(x => x.Code == site.SourceSystem);
var serviceUrl = string.IsNullOrWhiteSpace(site.SapServiceUrl) ? source.CentralServiceUrl : site.SapServiceUrl;
var username = string.IsNullOrWhiteSpace(site.UsernameOverride) ? source.CentralUsername : site.UsernameOverride;
var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? source.CentralPassword : site.PasswordOverride;
if (string.IsNullOrWhiteSpace(serviceUrl) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    throw new InvalidOperationException("Produktive SAP-Gateway-Konfiguration ist unvollstaendig.");

var baseUrl = serviceUrl.TrimEnd('/') + "/";
using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
    "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

using var response = await client.GetAsync(baseUrl + "$metadata");
Console.WriteLine($"HTTP {(int)response.StatusCode}");
if (!response.IsSuccessStatusCode) return 1;

var document = XDocument.Parse(await response.Content.ReadAsStringAsync());
foreach (var entitySet in entitySets)
{
    var set = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "EntitySet" &&
        string.Equals(x.Attribute("Name")?.Value, entitySet, StringComparison.OrdinalIgnoreCase));
    var fullType = set?.Attribute("EntityType")?.Value;
    if (string.IsNullOrWhiteSpace(fullType))
    {
        Console.Error.WriteLine($"EntitySet {entitySet} nicht im Metadata-Dokument gefunden.");
        return 1;
    }

    var typeName = fullType.Split('.').Last();
    var type = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "EntityType" &&
        string.Equals(x.Attribute("Name")?.Value, typeName, StringComparison.OrdinalIgnoreCase));
    if (type is null) throw new InvalidOperationException($"EntityType {typeName} fehlt.");

    Console.WriteLine($"EntitySet {entitySet} -> {fullType}");
    foreach (var property in type.Elements().Where(x => x.Name.LocalName == "Property"))
        Console.WriteLine($"{property.Attribute("Name")?.Value} | {property.Attribute("Type")?.Value} | nullable={property.Attribute("Nullable")?.Value ?? "true"}");
}
return 0;
