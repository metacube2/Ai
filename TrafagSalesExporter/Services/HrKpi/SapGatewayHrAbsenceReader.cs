using System.Globalization;
using System.Net;
using System.Text.Json;
using ClosedXML.Excel;

namespace TrafagSalesExporter.Services;

/// <summary>Eine Abwesenheit aus HrAbsenzSet (SAP PA2001): ein Fall mit vollem Von/Bis.</summary>
public sealed record HrAbsenceSapCase(
    string Personalnummer,
    string Abwesenheitsart,
    DateTime Von,
    DateTime Bis,
    string Folgenummer,
    decimal Abwesenheitstage,
    decimal Stunden,
    decimal Kalendertage);

/// <summary>
/// Liest die Abwesenheiten je Fall ueber OData und schreibt <c>hrdata/HR_Absenzen_SAP.xlsx</c>.
/// Rexx liefert Krankheit nur als Summe je Person und Ferien gar nicht je Fall
/// (<c>docs/HR_KPI.md</c> 8.5); SAP hat jeden Fall mit Von/Bis (8.7, Entscheid Ingo 2026-10-01).
///
/// Geliefert werden alle Abwesenheitsarten, auch Unfall und Arzt. Welche als Krankheit, Ferien,
/// Kompensation oder Militaer zaehlen, steht im Builder (<c>HrKpiDashboardBuilder.SickAbsenceTypes</c>
/// und folgende), damit eine Aenderung keinen SAP-Transport braucht.
/// </summary>
public class SapGatewayHrAbsenceReader
{
    public const string EntitySet = "HrAbsenzSet";

    internal static readonly string[] Headers =
    [
        "Personalnummer", "Abwesenheitsart", "Von", "Bis", "Folgenummer",
        "Abwesenheitstage", "Stunden", "Kalendertage"
    ];

    /// <summary>
    /// Liest die genannten Jahre. Ein Fall ueber den Jahreswechsel kommt in beiden Jahren und wird
    /// nur einmal behalten.
    /// </summary>
    public async Task<IReadOnlyList<HrAbsenceSapCase>> ReadAsync(
        string serviceUrl, string username, string password, IEnumerable<int> years,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = serviceUrl.TrimEnd('/') + "/";
        using var client = SapGatewayHrKpiReader.CreateClient(username, password);
        var cases = new List<HrAbsenceSapCase>();

        foreach (var year in years.Distinct())
        {
            for (var page = 0; ; page++)
            {
                if (page >= SapGatewayHrKpiReader.MaxPages)
                    throw new InvalidOperationException(
                        $"{EntitySet} {year} lieferte nach {SapGatewayHrKpiReader.MaxPages} Seiten immer noch volle Seiten. " +
                        "Abbruch, weil das Set das Paging vermutlich ignoriert.");

                var url = BuildPageUrl(baseUrl, year, page * SapGatewayHrKpiReader.PageSize);
                using var response = await client.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"SAP OData {EntitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}). " +
                        (response.StatusCode == HttpStatusCode.NotFound
                            ? "Das EntitySet ist im Zielsystem vermutlich noch nicht transportiert. "
                            : string.Empty) +
                        $"URL={url} Antwort={(body.Length <= 500 ? body : body[..500])}",
                        null, response.StatusCode);
                }

                var pageRows = ParseRows(await response.Content.ReadAsStringAsync(cancellationToken));
                cases.AddRange(pageRows);
                if (pageRows.Count < SapGatewayHrKpiReader.PageSize)
                    break;
            }
        }

        return Deduplicate(cases);
    }

    internal static string BuildPageUrl(string baseUrl, int year, int skip)
    {
        var filter = $"Gjahr eq '{year:0000}'";
        return $"{baseUrl}{EntitySet}?$format=json&$top={SapGatewayHrKpiReader.PageSize}&$skip={skip}&$filter={Uri.EscapeDataString(filter)}";
    }

    internal static List<HrAbsenceSapCase> ParseRows(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("d", out var d) ||
            !d.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
            return [];

        var rows = new List<HrAbsenceSapCase>();
        foreach (var item in results.EnumerateArray())
        {
            var pernr = Text(item, "Pernr");
            var von = ParseSapDate(Text(item, "Begda"));
            var bis = ParseSapDate(Text(item, "Endda"));
            // Ohne Personalnummer oder Datum laesst sich der Fall keinem Zeitraum zuordnen.
            if (string.IsNullOrWhiteSpace(pernr) || von is null || bis is null)
                continue;
            rows.Add(new HrAbsenceSapCase(
                pernr, Text(item, "Awart"), von.Value, bis.Value, Text(item, "Seqnr"),
                Number(item, "Abwtg"), Number(item, "Stdaz"), Number(item, "Kaltg")));
        }

        return rows;
    }

    internal static List<HrAbsenceSapCase> Deduplicate(IEnumerable<HrAbsenceSapCase> cases)
        => cases
            .GroupBy(x => (x.Personalnummer, x.Abwesenheitsart, x.Von, x.Bis, x.Folgenummer))
            .Select(g => g.First())
            .OrderBy(x => x.Personalnummer, StringComparer.Ordinal)
            .ThenBy(x => x.Von)
            .ToList();

    /// <summary>SAP liefert JJJJMMTT; das offene Ende 99991231 ist ein gueltiges Datum.</summary>
    internal static DateTime? ParseSapDate(string value)
        => DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    /// <summary>Wie <see cref="SapGatewayHrKpiReader.WriteWorkbook"/>: erst temporaer, dann ersetzen.</summary>
    internal static void WriteWorkbook(IReadOnlyList<HrAbsenceSapCase> cases, string path)
    {
        var tempPath = Path.Combine(Path.GetDirectoryName(path) ?? ".",
            Path.GetFileNameWithoutExtension(path) + ".tmp.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Absenzen");
            for (var column = 0; column < Headers.Length; column++)
                sheet.Cell(1, column + 1).Value = Headers[column];

            var line = 2;
            foreach (var row in cases)
            {
                sheet.Cell(line, 1).Value = row.Personalnummer;
                sheet.Cell(line, 2).Value = row.Abwesenheitsart;
                sheet.Cell(line, 3).Value = row.Von;
                sheet.Cell(line, 4).Value = row.Bis;
                sheet.Cell(line, 5).Value = row.Folgenummer;
                sheet.Cell(line, 6).Value = row.Abwesenheitstage;
                sheet.Cell(line, 7).Value = row.Stunden;
                sheet.Cell(line, 8).Value = row.Kalendertage;
                line++;
            }

            workbook.SaveAs(tempPath);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    private static string Text(JsonElement item, string name)
        => item.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? (value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString())?.Trim() ?? string.Empty
            : string.Empty;

    private static decimal Number(JsonElement item, string name)
        => decimal.TryParse(Text(item, name), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
}
