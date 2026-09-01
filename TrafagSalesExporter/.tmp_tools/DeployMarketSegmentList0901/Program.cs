using System.Diagnostics;
using DeployConsole;

// Produktivdeploy der vollstaendigen Marktsegment-Standardliste vom 2026-09-01.
// Rein code-/UI-seitig: kein Schemawechsel, keine Migration und keine Datenumschreibung.

var dryRun = args.Contains("--dry", StringComparer.OrdinalIgnoreCase);
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var settings = DeploySettings.Load(Path.Combine(root, "Tools", "DeployConsole", "deploy.settings.json"));
const string expectedTarget = @"\\trch-webapp-bidashboard.trafagch.local\BiDashboard$";
if (!string.Equals(settings.TargetDir.TrimEnd(Path.DirectorySeparatorChar), expectedTarget, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException($"Unerwartetes Deploy-Ziel: {settings.TargetDir}");

BackupResult? backup = null;
if (!dryRun)
{
    var watch = Stopwatch.StartNew();
    backup = await DatabaseBackup.EnsureBackupAsync(
        Path.Combine(settings.TargetDir, settings.DatabaseFile), "market-segment-list", DateTime.Now, Console.WriteLine);
    watch.Stop();
    Console.WriteLine($"MESSUNG Sicherung: {watch.Elapsed.TotalSeconds:N1} s ({(backup.Reused ? "wiederverwendet" : "neu angelegt")}).");
}

settings.Routes = ["", "marktsegmente", "management-cockpit"];
var request = new DeployRequest
{
    Title = "Marktsegmente: vollstaendige Standardliste aus Vertriebsvorgabe",
    TestsGreen = true,
    TestCount = "668/668",
    RunSmokeTests = true,
    Expected =
    [
        "Calibration services",
        "Food & Beverage",
        "General Industry",
        "Large Engines",
        "Power Distribution",
        "Water Treatment",
        "Automotive (MAG)",
        "E-Bikes (MAG)",
        "Robotics (MAG)"
    ],
    Forbidden = ["Ship Building", "Mobile Hydraulics"],
    DryRun = dryRun
};

var report = await new DeployRunner(settings, Console.WriteLine).RunAsync(request, CancellationToken.None);
Console.WriteLine();
if (backup is not null)
{
    Console.WriteLine("=== SICHERUNG ===");
    Console.WriteLine(backup.Path);
    Console.WriteLine(backup.Reason);
}
Console.WriteLine("=== PROTOKOLLABSATZ ===");
Console.WriteLine(ProtocolWriter.Build(request, report));
if (!report.Succeeded)
    Environment.ExitCode = 1;
