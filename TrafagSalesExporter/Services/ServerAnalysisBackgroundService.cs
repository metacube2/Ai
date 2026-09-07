using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services.DataSources;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Fuehrt lesende Diagnoseabfragen gegen die SAP-B1-/HANA-Systeme der Standorte aus - auf dem
/// Applikationsserver, weil einzelne Standortsysteme nur von dort erreichbar sind (Indiens
/// HANA <c>20.197.20.60:30015</c> ist vom Entwicklungsrechner nicht erreichbar, geprueft
/// 2026-08-05).
///
/// Warum ueberhaupt in der Anwendung und nicht als Konsolenwerkzeug: Auf dem Server ist
/// Remoteausfuehrung gesperrt (`Invoke-Command`, `schtasks`, `C$` -> Zugriff verweigert) und es
/// gibt keinen RDP-Zugang. Der Anwendungsordner ist dagegen beschreibbar. Damit ist die
/// laufende Anwendung der einzige Weg, ueberhaupt Code auf dem Server auszufuehren.
///
/// Ablauf: Liegt im Unterordner <c>_analysis</c> des Anwendungsordners die Datei
/// <c>run.trigger</c>, wird jede Datei aus <c>_analysis/sql</c> ausgefuehrt und das Ergebnis
/// nach <c>_analysis/results</c> geschrieben. Der Standort ergibt sich aus dem Dateinamen
/// (<c>TRIN__01_...sql</c> -> <c>TRIN</c>). Danach wird die Triggerdatei umbenannt, damit
/// derselbe Lauf nicht wiederholt wird.
///
/// Sicherheit:
/// - Nur <c>SELECT</c>/<c>WITH</c> (<see cref="ReadOnlySqlGuard"/>); jedes andere Statement
///   wird abgelehnt und die Ablehnung protokolliert.
/// - Ohne Triggerdatei passiert nichts ausser einem <c>File.Exists</c> je Intervall.
/// - Zugangsdaten kommen aus der Konfigurationsdatenbank, nie aus den SQL-Dateien.
/// - Fehler bleiben lokal: sie landen in der Ergebnisdatei und im Anwendungsprotokoll,
///   koennen den Anwendungsstart aber nicht stoeren.
///
/// Keine Rechteausweitung: wer <c>_analysis</c> beschreiben kann, kann auch die
/// Anwendungs-DLLs ersetzen. Ein reiner Lesezugriff aus Dateien ist dagegen die kleinere
/// Moeglichkeit, nicht die groessere.
/// </summary>
public class ServerAnalysisBackgroundService : BackgroundService
{
    public const string FolderName = "_analysis";
    public const string TriggerFileName = "run.trigger";

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);
    private const int MaxRowsPerStatement = 500;

    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ServerAnalysisBackgroundService> _logger;

    public ServerAnalysisBackgroundService(
        IServiceProvider serviceProvider,
        IHostEnvironment environment,
        ILogger<ServerAnalysisBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _environment = environment;
        _logger = logger;
    }

    private string AnalysisDirectory => Path.Combine(_environment.ContentRootPath, FolderName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var trigger = Path.Combine(AnalysisDirectory, TriggerFileName);
                if (File.Exists(trigger))
                    await RunAllAsync(trigger, stoppingToken);
            }
            catch (Exception ex)
            {
                // Eine fehlgeschlagene Diagnose darf den Dienst nicht beenden.
                _logger.LogError(ex, "Server-Analyse fehlgeschlagen.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task RunAllAsync(string triggerPath, CancellationToken cancellationToken)
    {
        var sqlDir = Path.Combine(AnalysisDirectory, "sql");
        var resultDir = Path.Combine(AnalysisDirectory, "results");

        // Trigger sofort entfernen: faellt der Prozess mitten im Lauf aus, wiederholt der
        // naechste Start nicht ungefragt dieselbe Abfrage gegen ein fremdes Produktivsystem.
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var consumed = triggerPath + ".done_" + stamp;
        File.Move(triggerPath, consumed, overwrite: true);

        var eventLog = _serviceProvider.GetRequiredService<IAppEventLogService>();

        if (!Directory.Exists(sqlDir))
        {
            await eventLog.WriteAsync("Server-Analyse", "Lauf ohne SQL-Ordner abgebrochen", "Warning",
                details: $"Erwartet: {sqlDir}");
            return;
        }

        Directory.CreateDirectory(resultDir);

        var files = Directory.GetFiles(sqlDir, "*.sql").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        await eventLog.WriteAsync("Server-Analyse", $"Lauf gestartet, {files.Count} Datei(en)",
            details: $"Ordner: {sqlDir}");

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var target = Path.Combine(resultDir, name + ".txt");
            try
            {
                var text = await RunFileAsync(file, cancellationToken);
                await File.WriteAllTextAsync(target, text, new UTF8Encoding(true), cancellationToken);
                await eventLog.WriteAsync("Server-Analyse", $"Analyse '{name}' ausgefuehrt",
                    details: $"Ergebnis: {target}");
            }
            catch (Exception ex)
            {
                var text = $"FEHLGESCHLAGEN: {ex.GetType().Name}{Environment.NewLine}{ex.Message}{Environment.NewLine}";
                await File.WriteAllTextAsync(target, text, new UTF8Encoding(true), CancellationToken.None);
                await eventLog.WriteAsync("Server-Analyse", $"Analyse '{name}' fehlgeschlagen", "Error",
                    details: ex.ToString());
            }
        }

        await eventLog.WriteAsync("Server-Analyse", "Lauf beendet",
            details: $"Trigger verbraucht: {Path.GetFileName(consumed)}");
    }

    private async Task<string> RunFileAsync(string file, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(file);
        var tsc = ServerAnalysisScript.ResolveTsc(fileName)
            ?? throw new InvalidOperationException(
                $"Dateiname '{fileName}' beginnt nicht mit '<TSC>_' - Standort nicht ableitbar.");

        var dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var hana = _serviceProvider.GetRequiredService<IHanaQueryService>();

        using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var site = await db.Sites.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TSC == tsc, cancellationToken)
            ?? throw new InvalidOperationException($"Kein Standort mit TSC '{tsc}' konfiguriert.");
        var sourceDefinition = await db.SourceSystemDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == site.SourceSystem, cancellationToken)
            ?? throw new InvalidOperationException($"Kein Quellsystem '{site.SourceSystem}' konfiguriert.");

        var server = await HanaServerResolver.BuildEffectiveServerAsync(db, site, sourceDefinition, cancellationToken);

        var output = new StringBuilder();
        output.AppendLine("Server-Analyse (nur lesend)");
        output.AppendLine($"Datei    : {fileName}");
        output.AppendLine($"Standort : {tsc} ({site.Land})");
        output.AppendLine($"Quelle   : {site.SourceSystem}");
        output.AppendLine($"Host     : {server.Host}:{server.Port}");
        output.AppendLine($"Schema   : {site.Schema}");
        output.AppendLine($"Zeitpunkt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        var raw = await File.ReadAllTextAsync(file, cancellationToken);
        var statements = ServerAnalysisScript.SplitStatements(raw);
        output.AppendLine($"Statements: {statements.Count}");

        foreach (var (label, statement) in statements)
        {
            var sql = ServerAnalysisScript.ApplySchema(statement, site.Schema);
            output.AppendLine();
            output.AppendLine("=== " + label + " ===");

            var rejection = ReadOnlySqlGuard.Validate(sql);
            if (rejection is not null)
            {
                output.AppendLine("ABGELEHNT: " + rejection);
                continue;
            }

            var watch = Stopwatch.StartNew();
            try
            {
                var result = await hana.RunReadOnlySelectAsync(server, sql, MaxRowsPerStatement, cancellationToken);
                watch.Stop();

                output.AppendLine(string.Join(" | ", result.ColumnNames));
                foreach (var row in result.Rows)
                    output.AppendLine(string.Join(" | ", row));

                if (result.Rows.Count == 0)
                    output.AppendLine("(keine Zeilen)");
                if (result.Truncated)
                    output.AppendLine($"... (bei {MaxRowsPerStatement} Zeilen abgeschnitten)");

                output.AppendLine($"-- {result.Rows.Count} Zeile(n), {watch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                watch.Stop();
                output.AppendLine($"FEHLER: {ex.Message}");
            }
        }

        return output.ToString();
    }
}
