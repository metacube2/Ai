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
