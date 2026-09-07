using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.DataSources;

/// <summary>
/// Baut aus der zentralen HANA-Konfiguration des Quellsystems und den standortspezifischen
/// Overrides den effektiven Verbindungsdatensatz.
///
/// Gemeinsam genutzt vom Export (<see cref="HanaDataSourceAdapter"/>) und von der
/// Server-Analyse (<see cref="ServerAnalysisBackgroundService"/>), damit eine Diagnose
/// zwangslaeufig dieselben Zugangsdaten und dieselbe Aufloesungsreihenfolge verwendet wie der
/// produktive Export - eine Diagnose, die anders verbindet als der Export, beweist nichts.
/// </summary>
internal static class HanaServerResolver
{
    public static async Task<HanaServer> BuildEffectiveServerAsync(
        AppDbContext db,
        Site site,
        SourceSystemDefinition sourceDefinition,
        CancellationToken cancellationToken = default)
    {
        var centralServer = await db.HanaServers
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.SourceSystem == sourceDefinition.Code, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Fuer Quellsystem '{sourceDefinition.Code}' ist keine zentrale HANA-Konfiguration vorhanden.");

        var credentials = DataSourceCredentials.Resolve(site, sourceDefinition);

        return new HanaServer
        {
            Id = centralServer.Id,
            SourceSystem = centralServer.SourceSystem,
            Name = centralServer.Name,
            Host = centralServer.Host,
            Port = centralServer.Port,
            Username = credentials.Username,
            Password = credentials.Password,
            DatabaseName = centralServer.DatabaseName,
            UseSsl = centralServer.UseSsl,
            ValidateCertificate = centralServer.ValidateCertificate,
            AdditionalParams = centralServer.AdditionalParams
        };
    }
}
