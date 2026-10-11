using System.Diagnostics;
using System.Net;
using System.Text;

namespace DeployConsole;

public sealed class DeployRequest
{
    public string Title { get; init; } = "";
    public string Commit { get; init; } = "";
    public bool TestsGreen { get; init; }
    public string TestCount { get; init; } = "";
    public List<string> Expected { get; init; } = new();
    public List<string> Forbidden { get; init; } = new();
    public bool RunSmokeTests { get; init; } = true;
    /// <summary>Preflight and proof only - no app_offline, no publish.</summary>
    public bool DryRun { get; init; }
}

public sealed record RouteResult(string Route, string Status, long Bytes, double Seconds, string? Error);

public sealed class DeployReport
{
    public DateTime StartedAt { get; init; }
    public string Branch { get; set; } = "";
    public string Commit { get; set; } = "";
    public bool WorkingTreeDirty { get; set; }
    public string DatabaseFileName { get; set; } = "";
    public FileState? DatabaseBefore { get; set; }
    public FileState? DatabaseAfter { get; set; }
    public SnapshotDiff? Diff { get; set; }
    public string? ServerDllSha { get; set; }
    public long ServerDllBytes { get; set; }
    public DateTime ServerDllWritten { get; set; }
    public string? LocalDllSha { get; set; }
    public List<EvidenceHit> Expected { get; } = new();
    public List<EvidenceHit> Forbidden { get; } = new();
    public List<RouteResult> Routes { get; } = new();
    public bool PublishRan { get; set; }
    public bool Succeeded { get; set; }
    public List<string> Alarms { get; } = new();
}

/// <summary>
/// Performs the deploy in the one order that is known to be safe, and proves afterwards
/// that only build output changed.
/// </summary>
public sealed class DeployRunner
{
    // Anything that could route the publish through a profile is refused outright.
    // FolderProfile.pubxml carries DeleteExistingFiles=true and the target directory
    // holds the production database and every .bak - so this is not a preference,
    // it is the reason the tool exists. Arguments are passed as a list, never as a
    // shell string, so nothing can be appended by quoting either.
    private static readonly string[] ForbiddenArgumentFragments =
    {
        "publishprofile", "pubxml", "deleteexistingfiles", "/p:", "-p:",
    };

    private const string OfflineName = "app_offline.htm";
    private const string OfflineDisabledName = "app_offline.htm.disabled";

    private const string OfflineHtml = """
        <!doctype html>
        <html lang="de"><head><meta charset="utf-8"><title>Wartung</title></head>
        <body style="font-family:Segoe UI,sans-serif;padding:40px">
        <h1>BiDashboard wird aktualisiert</h1>
        <p>Die Anwendung ist fuer wenige Minuten nicht erreichbar.</p>
        </body></html>
        """;

    private readonly DeploySettings _settings;
    private readonly Action<string> _log;

    public DeployRunner(DeploySettings settings, Action<string> log)
    {
        _settings = settings;
        _log = log;
    }

    public static void AssertSafeArguments(IEnumerable<string> arguments)
    {
        foreach (var arg in arguments)
        {
            var lower = arg.ToLowerInvariant();
            foreach (var forbidden in ForbiddenArgumentFragments)
            {
                if (lower.Contains(forbidden, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Argument '{arg}' enthaelt '{forbidden}'. Publish ueber ein Profil ist gesperrt "
                        + "(DeleteExistingFiles=true wuerde die Produktivdatenbank und alle .bak loeschen).");
                }
            }
        }
    }

    public async Task<DeployReport> RunAsync(DeployRequest request, CancellationToken token)
    {
        var report = new DeployReport
        {
            StartedAt = DateTime.Now,
            Commit = request.Commit,
            DatabaseFileName = _settings.DatabaseFile,
        };
        var projectDir = Path.GetDirectoryName(Path.GetFullPath(_settings.ProjectPath))!;

        ReadGitState(projectDir, report);
        _log($"Branch {report.Branch}, Commit {report.Commit}"
             + (report.WorkingTreeDirty ? "  (Arbeitsverzeichnis nicht sauber)" : ""));

        if (!Directory.Exists(_settings.TargetDir))
        {
            throw new DirectoryNotFoundException($"Zielverzeichnis nicht erreichbar: {_settings.TargetDir}");
        }
        var serverDll = Path.Combine(_settings.TargetDir, _settings.MainAssembly);
        if (!File.Exists(serverDll))
        {
            throw new FileNotFoundException(
                $"Im Ziel liegt kein {_settings.MainAssembly}. Das sieht nicht nach dem Publish-Verzeichnis aus - "
                + "abgebrochen, bevor irgendetwas geschrieben wird.", serverDll);
        }

        _log("Bestandsaufnahme des Ziels...");
        var before = TargetSnapshot.Take(_settings.TargetDir);
        _log($"  {before.Files.Count:N0} Dateien erfasst.");
        foreach (var f in before.Matching(_settings.ProtectedPatterns))
        {
            _log($"  geschuetzt: {f.Describe()}");
        }
        report.DatabaseBefore = before.Find(_settings.DatabaseFile);
        if (report.DatabaseBefore is null)
        {
            report.Alarms.Add($"Die konfigurierte Produktivdatenbank '{_settings.DatabaseFile}' liegt nicht im Ziel - "
                              + "ohne sie ist die Aussage 'DB unveraendert' nicht belegbar.");
            _log($"  ALARM: {_settings.DatabaseFile} im Ziel nicht gefunden.");
        }

        if (request.DryRun)
        {
            _log("PRUEFLAUF - kein app_offline, kein Publish.");
        }
        else
        {
            var offlinePath = Path.Combine(_settings.TargetDir, OfflineName);
            try
            {
                // Set immediately before the publish and clear immediately after:
                // every second in between is downtime for everyone on the dashboard.
                File.WriteAllText(offlinePath, OfflineHtml, Encoding.UTF8);
                _log($"{OfflineName} gesetzt.");
                await PublishAsync(token);
                report.PublishRan = true;
            }
            finally
            {
                ClearOffline(offlinePath, report);
            }
        }

        _log("Nachweis...");
        var after = TargetSnapshot.Take(_settings.TargetDir);
        report.Diff = SnapshotDiff.Compare(before, after, _settings.ProtectedPatterns);
        _log(report.Diff.Describe());
        if (!report.Diff.IsClean)
        {
            report.Alarms.Add("Im Ziel sind Dateien verschwunden oder geschuetzte Dateien wurden veraendert.");
        }
        report.DatabaseAfter = after.Find(_settings.DatabaseFile);

        var dllInfo = new FileInfo(serverDll);
        report.ServerDllBytes = dllInfo.Length;
        report.ServerDllWritten = dllInfo.LastWriteTime;
        report.ServerDllSha = DllEvidence.Sha256(serverDll);
        _log($"{_settings.MainAssembly}: {dllInfo.Length:N0} Bytes, {dllInfo.LastWriteTime:dd.MM.yyyy HH:mm:ss}");
        _log($"  SHA256 {report.ServerDllSha}");

        if (File.Exists(_settings.LocalReleaseDll))
        {
            report.LocalDllSha = DllEvidence.Sha256(_settings.LocalReleaseDll);
            if (report.LocalDllSha == report.ServerDllSha)
            {
                _log("  lokaler Release-Build und Server bitgleich.");
            }
            else if (report.PublishRan)
            {
                // After a publish the deployed file is a byte copy of the local build
                // output, so a difference is not the non-determinism of two separate
                // builds - it means the copy did not happen. Measured cause: the main
                // assembly is copied with PreserveNewest, so a target file whose
                // timestamp is NEWER than the fresh build is skipped without a word,
                // and the previous version stays online while the publish reports
                // success.
                var localWritten = new FileInfo(_settings.LocalReleaseDll).LastWriteTime;
                report.Alarms.Add(
                    $"{_settings.MainAssembly} im Ziel ist NICHT die Kopie des lokalen Release-Builds - der Publish hat "
                    + "sie uebersprungen. Haeufigste Ursache: die Datei im Ziel ist neuer als der Build "
                    + $"(Ziel {report.ServerDllWritten:dd.MM.yyyy HH:mm:ss}, Build {localWritten:dd.MM.yyyy HH:mm:ss}). "
                    + "Die alte Version laeuft weiter.");
                _log($"  ALARM: Server-DLL weicht vom lokalen Release-Build ab (lokal {report.LocalDllSha}).");
            }
            else
            {
                _log("  Prueflauf: Server-DLL weicht vom lokalen Build ab (erwartbar ohne Publish).");
            }
        }

        report.Expected.AddRange(DllEvidence.Probe(serverDll, request.Expected));
        foreach (var hit in report.Expected)
        {
            _log($"  erwartet  {(hit.Found ? "JA " : "NEIN")}  {hit.Needle}   [{hit.Encoding}]");
            if (!hit.Found)
            {
                report.Alarms.Add($"Erwartet, aber nicht in der DLL: {hit.Needle}");
            }
        }
        report.Forbidden.AddRange(DllEvidence.Probe(serverDll, request.Forbidden));
        foreach (var hit in report.Forbidden)
        {
            _log($"  entfernt  {(hit.Found ? "NEIN" : "JA ")}  {hit.Needle}   [{hit.Encoding}]");
            if (hit.Found)
            {
                report.Alarms.Add($"Sollte entfernt sein, steht aber noch in der DLL: {hit.Needle}");
            }
        }

        if (request.RunSmokeTests)
        {
            await SmokeTestAsync(report, token);
        }
        else
        {
            _log("Abrufpruefung uebersprungen.");
        }

        report.Succeeded = report.Alarms.Count == 0;
        _log(report.Succeeded ? "Fertig, ohne Alarm." : $"Fertig, {report.Alarms.Count} Alarm(e).");
        return report;
    }

    private void ClearOffline(string offlinePath, DeployReport report)
    {
        try
        {
            ClearOfflineCore(offlinePath);
        }
        catch (Exception ex)
        {
            // Runs in a finally block: throwing here would replace a publish error with
            // this one AND leave the site offline. Record it loudly instead.
            report.Alarms.Add($"{OfflineName} konnte nicht entfernt werden - DIE ANWENDUNG IST NOCH OFFLINE: {ex.Message}");
            _log($"ALARM: {OfflineName} bleibt liegen: {ex.Message}");
        }
    }

    private void ClearOfflineCore(string offlinePath)
    {
        if (!File.Exists(offlinePath))
        {
            return;
        }
        var disabled = Path.Combine(_settings.TargetDir, OfflineDisabledName);
        if (File.Exists(disabled))
        {
            // A leftover from an earlier deploy makes the rename fail. Removing it is
            // the only delete this tool performs, so it goes on the record.
            var stale = new FileInfo(disabled);
            _log($"Alte {OfflineDisabledName} entfernt ({stale.Length:N0} Bytes, {stale.LastWriteTime:dd.MM.yyyy HH:mm:ss}).");
            File.Delete(disabled);
        }
        File.Move(offlinePath, disabled);
        _log($"{OfflineName} nach {OfflineDisabledName} umbenannt - Anwendung wieder online.");
    }

    private async Task PublishAsync(CancellationToken token)
    {
        var arguments = new List<string>
        {
            "publish", _settings.ProjectPath, "-c", "Release", "-o", _settings.TargetDir, "--nologo",
        };
        AssertSafeArguments(arguments);
        _log("dotnet " + string.Join(" ", arguments));

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(_settings.ProjectPath))!,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in arguments)
        {
            psi.ArgumentList.Add(a);
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) _log("  " + e.Data); };
        process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) _log("  " + e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(token);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet publish endete mit Exit-Code {process.ExitCode}.");
        }
        _log("Publish fertig.");
    }

    private async Task SmokeTestAsync(DeployReport report, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _log("Keine Basis-URL gesetzt - Abrufpruefung uebersprungen.");
            return;
        }
        using var handler = new HttpClientHandler { UseDefaultCredentials = true, AllowAutoRedirect = true };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(3) };

        foreach (var route in _settings.Routes)
        {
            var url = _settings.BaseUrl.TrimEnd('/') + "/" + route.TrimStart('/');
            var watch = Stopwatch.StartNew();
            try
            {
                using var response = await client.GetAsync(url, token);
                var body = await response.Content.ReadAsByteArrayAsync(token);
                watch.Stop();
                var status = ((int)response.StatusCode).ToString();
                report.Routes.Add(new RouteResult(route, status, body.LongLength, watch.Elapsed.TotalSeconds, null));
                _log($"  {status}  {url}  {body.LongLength:N0} Bytes  {watch.Elapsed.TotalSeconds:0.00} s");
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    report.Alarms.Add($"{url} antwortet mit {status}.");
                }
            }
            catch (Exception ex)
            {
                watch.Stop();
                report.Routes.Add(new RouteResult(route, "-", 0, watch.Elapsed.TotalSeconds, ex.Message));
                report.Alarms.Add($"{url} nicht abrufbar: {ex.Message}");
                _log($"  FEHLER {url}: {ex.Message}");
            }
        }
    }

    private static void ReadGitState(string projectDir, DeployReport report)
    {
        report.Branch = Git(projectDir, "rev-parse", "--abbrev-ref", "HEAD");
        var head = Git(projectDir, "rev-parse", "--short", "HEAD");
        if (string.IsNullOrWhiteSpace(report.Commit))
        {
            report.Commit = head;
        }
        report.WorkingTreeDirty = !string.IsNullOrWhiteSpace(Git(projectDir, "status", "--porcelain"));
    }

    private static string Git(string workingDir, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDir,
            };
            foreach (var a in args)
            {
                psi.ArgumentList.Add(a);
            }
            using var p = Process.Start(psi)!;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output.Trim();
        }
        catch
        {
            return "";
        }
    }
}
