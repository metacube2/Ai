using System.Globalization;
using System.Net;
using System.Text.Json;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Eine Transportauftragsposition (LTAK/LTAP). Seit 2026-10-05 mit den TA-Benutzern (BNAME angelegt, ENAME Entnahme,
/// QNAME quittiert; HR-Freigabe laut Ingo); leer, solange P76 die Felder nicht liefert. Angezeigt nur nach Anmeldung.
/// </summary>
public sealed record LiveTransferItem(
    string Lgnum, string Tanum, string Tapos, string Bwlvs, string Delivery, string Material, string Plant,
    string FromType, string FromBin, string ToType, string ToBin, decimal Quantity, string Unit,
    DateTime? CreatedAt, bool Confirmed, DateTime? ConfirmedAt,
    string CreatedBy = "", string PickedBy = "", string ConfirmedBy = "");

/// <summary>Eine Lieferung mit Kommissionierstand (LIKP/LIPS).</summary>
public sealed record LiveDelivery(
    string Delivery, string DeliveryType, string ShippingPoint, string Lgnum, string Customer, string CustomerName,
    string PickingStatus, string GoodsIssueStatus, int Positions, int PositionsPicked, int PositionsPartial,
    DateTime? PlannedGoodsIssue = null, DateTime? GoodsIssueAt = null);

/// <summary>Kapazitaet je Tag (LogKapSet, Teil C 2026-10-05): Bedarf aus KBED, Angebot aus dem Kapazitaetskopf.</summary>
public sealed record LiveCapacityDay(
    string CapacityId, string Name, string Category, string WorkCenters, DateOnly Day,
    double DemandHours, double SupplyHours, int Operations, bool NoStandard)
{
    public double? LoadPercent => SupplyHours > 0 ? DemandHours / SupplyHours * 100 : null;
}

/// <summary>Eine Produktionsrueckmeldung (AFRU), ohne Personalnummer.</summary>
public sealed record LiveConfirmation(
    string Rueck, string Rmzhl, string Order, string Operation, string WorkCenter, string Plant,
    DateTime? At, decimal Yield, decimal Scrap, string Unit, bool Final, bool Cancelled,
    string Material, decimal OrderTarget, decimal OrderConfirmed);

/// <summary>
/// Liest die drei Logistik-Live-Sets aus ZPOWERBI_EINKAUF_SRV (docs/LOGISTIK_LIVE_2026-10-01.md).
/// Jede Anfrage traegt den Pflichtfilter Datum; mit <c>fromTime</c> nur, was ab dieser Uhrzeit
/// neu ist. Ohne Datum liefert SAP bewusst nichts (Schutz fuer P76).
/// </summary>
public class SapGatewayLogisticsLiveReader
{
    public const string TransferSet = "LogTaSet";
    public const string DeliverySet = "LogLiefSet";
    public const string ConfirmationSet = "LogRueckSet";
    public const string CapacitySet = "LogKapSet";

    /// <summary>Kapazitaet je Kapazitaet und Tag fuer ein Werk, hoechstens 14 Tage ab <paramref name="from"/>.</summary>
    public async Task<IReadOnlyList<LiveCapacityDay>> ReadCapacityAsync(
        HttpClient client, string baseUrl, string plant, DateOnly from, int days, CancellationToken ct)
        => ParseCapacity(await ReadAllAsync(client, baseUrl, CapacitySet,
            $"Werks eq '{plant}' and Datum eq '{from:yyyyMMdd}' and Tage eq '{Math.Clamp(days, 1, 14)}'", ct));

    internal static List<LiveCapacityDay> ParseCapacity(IEnumerable<JsonElement> rows)
        => rows.Select(x => (Row: x, Day: DateTimeOf(Text(x, "Tag"), "")))
            .Where(x => x.Day.HasValue && Text(x.Row, "Kapid").Length > 0)
            .Select(x => new LiveCapacityDay(
                Text(x.Row, "Kapid").TrimStart('0'), Text(x.Row, "Kapname"), Text(x.Row, "Kapar"), Text(x.Row, "Arbpl"),
                DateOnly.FromDateTime(x.Day!.Value), (double)Number(x.Row, "BedarfH"), (double)Number(x.Row, "AngebotH"),
                (int)Number(x.Row, "Vorgaenge"), Text(x.Row, "KeinStandard") == "X"))
            .ToList();
    internal const int PageSize = 2000;
    internal const int MaxPages = 15;

    public async Task<IReadOnlyList<LiveTransferItem>> ReadTransfersAsync(
        HttpClient client, string baseUrl, DateOnly day, TimeOnly fromTime, CancellationToken ct)
        => ParseTransfers(await ReadAllAsync(client, baseUrl, TransferSet, BuildFilter(day, fromTime), ct));

    public async Task<IReadOnlyList<LiveDelivery>> ReadDeliveriesAsync(
        HttpClient client, string baseUrl, DateOnly day, CancellationToken ct)
        => ParseDeliveries(await ReadAllAsync(client, baseUrl, DeliverySet, BuildFilter(day, null), ct));

    public async Task<IReadOnlyList<LiveConfirmation>> ReadConfirmationsAsync(
        HttpClient client, string baseUrl, DateOnly day, TimeOnly fromTime, CancellationToken ct)
        => ParseConfirmations(await ReadAllAsync(client, baseUrl, ConfirmationSet, BuildFilter(day, fromTime), ct));

    internal static string BuildFilter(DateOnly day, TimeOnly? fromTime)
        => fromTime is null
            ? $"Datum eq '{day:yyyyMMdd}'"
            : $"Datum eq '{day:yyyyMMdd}' and Abzeit eq '{fromTime.Value:HHmmss}'";

    internal static string BuildPageUrl(string baseUrl, string set, string filter, int skip)
        => $"{baseUrl.TrimEnd('/')}/{set}?$format=json&$top={PageSize}&$skip={skip}&$filter={Uri.EscapeDataString(filter)}";

    private static async Task<List<JsonElement>> ReadAllAsync(
        HttpClient client, string baseUrl, string set, string filter, CancellationToken ct)
    {
        var rows = new List<JsonElement>();
        for (var page = 0; page < MaxPages; page++)
        {
            var url = BuildPageUrl(baseUrl, set, filter, page * PageSize);
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"SAP OData {set} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase})" +
                    (response.StatusCode == HttpStatusCode.NotFound ? ", Set vermutlich noch nicht transportiert." : "."),
                    null, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var pageRows = Results(document).Select(x => x.Clone()).ToList();
            rows.AddRange(pageRows);
            if (pageRows.Count < PageSize)
                return rows;
        }

        throw new InvalidOperationException($"{set} lieferte nach {MaxPages} Seiten noch volle Seiten; Abbruch.");
    }

    private static IEnumerable<JsonElement> Results(JsonDocument document)
        => document.RootElement.TryGetProperty("d", out var d) &&
           d.TryGetProperty("results", out var results) &&
           results.ValueKind == JsonValueKind.Array
            ? results.EnumerateArray()
            : [];

    internal static List<LiveTransferItem> ParseTransfers(IEnumerable<JsonElement> rows)
        => rows.Select(x => new LiveTransferItem(
                Text(x, "Lgnum"), Text(x, "Tanum"), Text(x, "Tapos"), Text(x, "Bwlvs"), Text(x, "Vbeln"),
                Text(x, "Matnr").TrimStart('0'), Text(x, "Werks"), Text(x, "Vltyp"), Text(x, "Vlpla"),
                Text(x, "Nltyp"), Text(x, "Nlpla"), Number(x, "Menge"), Text(x, "Meins"),
                DateTimeOf(Text(x, "Bdatu"), Text(x, "Bzeit")),
                Text(x, "Pquit") == "X", DateTimeOf(Text(x, "Qdatu"), Text(x, "Qzeit")),
                Text(x, "Bname"), Text(x, "Ename"), Text(x, "Qname")))
            .Where(x => x.Tanum.Length > 0)
            .ToList();

    internal static List<LiveDelivery> ParseDeliveries(IEnumerable<JsonElement> rows)
        => rows.Select(x => new LiveDelivery(
                Text(x, "Vbeln"), Text(x, "Lfart"), Text(x, "Vstel"), Text(x, "Lgnum"), Text(x, "Kunnr").TrimStart('0'),
                Text(x, "Name1"), Text(x, "Kostk"), Text(x, "Wbstk"),
                (int)Number(x, "PosGes"), (int)Number(x, "PosKomm"), (int)Number(x, "PosTeil"),
                DateTimeOf(Text(x, "Wadat"), ""), DateTimeOf(Text(x, "WadatIst"), Text(x, "WaZeit"))))
            .Where(x => x.Delivery.Length > 0)
            .ToList();

    internal static List<LiveConfirmation> ParseConfirmations(IEnumerable<JsonElement> rows)
        => rows.Select(x => new LiveConfirmation(
                Text(x, "Rueck"), Text(x, "Rmzhl"), Text(x, "Aufnr").TrimStart('0'), Text(x, "Vornr"),
                Text(x, "Arbpl"), Text(x, "Werks"), DateTimeOf(Text(x, "Ersda"), Text(x, "Erzet")),
                Number(x, "Lmnga"), Number(x, "Xmnga"), Text(x, "Meinh"), Text(x, "Aueru") == "X",
                Text(x, "Stokz") == "X", Text(x, "Matnr").TrimStart('0'), Number(x, "Gamng"), Number(x, "Igmng")))
            .Where(x => x.Rueck.Length > 0)
            .ToList();

    /// <summary>SAP JJJJMMTT + HHMMSS; leer oder 00000000 ergibt null.</summary>
    internal static DateTime? DateTimeOf(string date, string time)
    {
        if (!DateTime.TryParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return null;
        return TimeSpan.TryParseExact(time, "hhmmss", CultureInfo.InvariantCulture, out var at) ? day + at : day;
    }

    private static string Text(JsonElement item, string name)
        => item.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? (value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString())?.Trim() ?? string.Empty
            : string.Empty;

    private static decimal Number(JsonElement item, string name)
        => decimal.TryParse(Text(item, name), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
}
