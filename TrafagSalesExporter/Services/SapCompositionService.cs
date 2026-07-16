using System.Globalization;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class SapCompositionService : ISapCompositionService
{
    private readonly ISapGatewayService _sapGatewayService;
    private readonly IMappedSalesRecordComposer _composer;
    private readonly IAppEventLogService _appEventLogService;

    public SapCompositionService(
        ISapGatewayService sapGatewayService,
        IMappedSalesRecordComposer composer,
        IAppEventLogService appEventLogService)
    {
        _sapGatewayService = sapGatewayService;
        _composer = composer;
        _appEventLogService = appEventLogService;
    }

    public async Task<List<SalesRecord>> BuildSalesRecordsAsync(
        Site site,
        IReadOnlyList<SapSourceDefinition> sources,
        IReadOnlyList<SapJoinDefinition> joins,
        IReadOnlyList<SapFieldMapping> mappings,
        string username,
        string password,
        int? preferredYear = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(site.SapServiceUrl))
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine SAP Service URL.");

        var activeSources = sources
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToList();
        if (activeSources.Count == 0)
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine aktiven SAP-Quellen.");

        var primarySource = activeSources.FirstOrDefault(s => s.IsPrimary) ?? activeSources.First();
        var sourceRows = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in activeSources)
        {
            await _appEventLogService.WriteDebugAsync("SAP", "Quelle wird gelesen", site.Id, site.Land,
                $"Alias={source.Alias} | EntitySet={source.EntitySet}");
            var filter = BuildODataYearFilter(source.EntitySet, preferredYear);
            var rows = await _sapGatewayService.GetEntityRowsAsync(site.SapServiceUrl, source.EntitySet, username, password, filter, cancellationToken);
            ValidateSourceRows(site, source, rows);
            if (string.Equals(source.EntitySet, "FinanzdataSchweizOeSet", StringComparison.OrdinalIgnoreCase))
                NormalizeZschweizRows(rows);
            sourceRows[source.Alias] = rows;
            await _appEventLogService.WriteDebugAsync("SAP", "Quelle gelesen", site.Id, site.Land,
                $"Alias={source.Alias} | EntitySet={source.EntitySet} | Zeilen={rows.Count}");
        }

        await _appEventLogService.WriteDebugAsync("SAP", "Mapping ins Zielschema gestartet", site.Id, site.Land,
            $"Primaerquelle={primarySource.Alias} | Mappings={mappings.Count(x => x.IsActive)}");
        var result = _composer.Compose(site, activeSources, joins, mappings, sourceRows, "SAP");
        await _appEventLogService.WriteDebugAsync("SAP", "Mapping ins Zielschema beendet", site.Id, site.Land,
            $"SalesRecords={result.Count} | Mappings={mappings.Count(x => x.IsActive)}");
        return result;
    }

    private static string? BuildODataYearFilter(string entitySet, int? preferredYear)
    {
        if (preferredYear is null)
            return null;

        return string.Equals(entitySet, "FinanzdataSchweizOeSet", StringComparison.OrdinalIgnoreCase)
            ? $"Gjahr eq '{preferredYear.Value}'"
            : null;
    }

    /// <summary>
    /// Zwei Rohdaten-Korrekturen fuer FinanzdataSchweizOeSet, bevor das generische
    /// SapFieldMapping darauf zugreift: die WAVWR/STPRS-Kostenbasis (<see cref="ResolveStandardCost"/>)
    /// und ein bekannter SAP-seitiger Waehrungsfehler in NETWR_HC (<see cref="CorrectHouseCurrencyScaling"/>).
    /// </summary>
    private static void NormalizeZschweizRows(List<Dictionary<string, object?>> rows)
    {
        foreach (var row in rows)
        {
            CorrectHouseCurrencyScaling(row);
            ResolveStandardCost(row);
        }
    }

    /// <summary>
    /// FinanzdataSchweizOeSet liefert keinen direkten Kostenwert; ZSCHWEIZ.WAVWR_DC (per
    /// ABAP-Report ergaenzt, siehe docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md) ist die
    /// zum Verkaufszeitpunkt eingefrorene Kostenzeilensumme in Belegwaehrung (Waerk).
    /// StandardCost ist downstream ein STUECKpreis (Menge x StandardCost), deshalb hier
    /// durch Fkimg geteilt. ZSCHWEIZ.STPRS_HC (aktueller MBEW-Stueckpreis, Hauswaehrung)
    /// ist der Fallback fuer die ~12 % Zeilen ohne Lieferbezug (kein WAVWR gebucht).
    /// Ergebnis wird als synthetische Felder in die Rohzeile geschrieben, damit das
    /// bestehende SapFieldMapping (Z.ResolvedStandardCost/-Currency) sie direkt referenzieren
    /// kann, ohne die Mapping-Ausdruckssprache um Arithmetik erweitern zu muessen.
    /// </summary>
    private static void ResolveStandardCost(Dictionary<string, object?> row)
    {
        var wavwrDc = ParseDecimal(row, "WavwrDc");
        var fkimg = ParseDecimal(row, "Fkimg");
        if (wavwrDc != 0m && fkimg != 0m)
        {
            row["ResolvedStandardCost"] = wavwrDc / fkimg;
            row["ResolvedStandardCostCurrency"] = ReadString(row, "Waerk");
            return;
        }

        var stprsHc = ParseDecimal(row, "StprsHc");
        if (stprsHc != 0m)
        {
            row["ResolvedStandardCost"] = stprsHc;
            row["ResolvedStandardCostCurrency"] = ReadString(row, "Hwaer");
            return;
        }

        row["ResolvedStandardCost"] = 0m;
        row["ResolvedStandardCostCurrency"] = ReadString(row, "Hwaer");
    }

    /// <summary>
    /// Bekannter SAP-seitiger Bug (docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md Abschnitt 13):
    /// ZSCHWEIZ.NETWR_HC (Umsatz Hauswaehrung, hier gemappt auf SalesPriceValue) kommt bei
    /// Fremdwaehrungsbelegen (Waerk != Hwaer) exakt Faktor 100 zu klein aus dem ABAP-Export
    /// (`to_house_currency` -&gt; CONVERT_TO_LOCAL_CURRENCY, vermutlich TCURR-Faktorproblem).
    /// An allen produktiven Fremdwaehrungszeilen bestaetigt (EUR/USD, 2026-07-16); bei
    /// gleicher Waehrung (Waerk = Hwaer) tritt der Fehler nicht auf.
    ///
    /// Die Korrektur ist bewusst SELBSTDEAKTIVIEREND statt eines blinden "*100": sie greift
    /// nur, wenn das Hochskalieren den Wert naeher an den erwarteten Betrag (NetwrDc * Kurrf)
    /// bringt als der unkorrigierte Rohwert. Sobald SAP die Ursache behebt (TCURR-Pflege oder
    /// ABAP-Fix), verschwindet die Abweichung und diese Korrektur wird von selbst wirkungslos —
    /// kein Risiko einer Doppelkorrektur, falls der Codeblock dann vergessen wird zu entfernen.
    /// </summary>
    private static void CorrectHouseCurrencyScaling(Dictionary<string, object?> row)
    {
        var waerk = ReadString(row, "Waerk");
        var hwaer = ReadString(row, "Hwaer");
        if (string.IsNullOrWhiteSpace(waerk) || string.IsNullOrWhiteSpace(hwaer) ||
            string.Equals(waerk, hwaer, StringComparison.OrdinalIgnoreCase))
            return;

        var kurrf = ParseDecimal(row, "Kurrf");
        if (kurrf == 0m)
            return;

        var netwrDc = ParseDecimal(row, "NetwrDc");
        var expected = netwrDc * kurrf;
        var raw = ParseDecimal(row, "NetwrHc");
        var scaledUp = raw * 100m;
        if (Math.Abs(scaledUp - expected) < Math.Abs(raw - expected))
            row["NetwrHc"] = scaledUp;
    }

    private static decimal ParseDecimal(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) &&
           decimal.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;

    private static void ValidateSourceRows(Site site, SapSourceDefinition source, IReadOnlyCollection<Dictionary<string, object?>> rows)
    {
        if (!string.Equals(source.EntitySet, "ProductDivisionRefSet", StringComparison.OrdinalIgnoreCase))
            return;

        if (rows.Count < 100)
            return;

        var assigned = rows.Count(IsAssignedProductDivisionReference);
        if (assigned > 0)
            return;

        var misc = rows.Count(row => IsProductDivisionCode(row, "0008"));
        var unassigned = rows.Count(row => IsProductDivisionCode(row, "UNASS"));
        throw new InvalidOperationException(
            $"SAP ProductDivisionRefSet fuer Standort '{site.TSC}' liefert {rows.Count:N0} Referenzzeilen, aber keine zugeordneten Sparten. " +
            $"UNASS={unassigned:N0}, Uebrige(0008)={misc:N0}. Import abgebrochen, damit das Dashboard nicht mit 'Nicht zugeordnet' ueberschrieben wird. " +
            "Bitte SAP-Service-URL bzw. neuen Produktsparten-OData-Service pruefen.");
    }

    private static bool IsAssignedProductDivisionReference(Dictionary<string, object?> row)
    {
        var divisionCode = ReadString(row, "Wwpsp");
        return IsTruthy(ReadString(row, "IsAssigned")) &&
               !string.IsNullOrWhiteSpace(divisionCode) &&
               !string.Equals(divisionCode.Trim(), "UNASS", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProductDivisionCode(Dictionary<string, object?> row, string expectedCode)
        => string.Equals(ReadString(row, "Wwpsp").Trim(), expectedCode, StringComparison.OrdinalIgnoreCase);

    private static string ReadString(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

    private static bool IsTruthy(string value)
        => value.Equals("X", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("JA", StringComparison.OrdinalIgnoreCase);
}
