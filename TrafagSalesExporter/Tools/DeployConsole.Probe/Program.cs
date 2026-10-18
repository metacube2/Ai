using System.Security.Cryptography;
using System.Text;
using DeployConsole;

// Full end-to-end exercise of DeployRunner against a REBUILT share in the scratch
// directory - never against the production UNC path. The fake target carries exactly
// the things that must survive a publish: a database, WAL/SHM sidecars, .bak backups
// and the ZDISPO workbooks, plus a stale app_offline.htm.disabled to reproduce the
// rename collision that broke a real deploy on 2026-08-07.

var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var scratch = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "deployprobe");
var target = Path.Combine(scratch, "share");

Console.WriteLine($"Repo : {repo}");
Console.WriteLine($"Ziel : {target}");

var failures = 0;
void Check(string name, bool ok, string detail)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}  ->  {detail}");
    if (!ok) failures++;
}

string Sha(string p) { using var s = File.OpenRead(p); return Convert.ToHexString(SHA256.HashData(s)); }

Dictionary<string, string> BuildFakeShare(DateTime placeholderStamp)
{
    if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
    Directory.CreateDirectory(target);
    Directory.CreateDirectory(Path.Combine(target, "wwwroot"));

    var rng = new Random(1234);
    void Fake(string name, int size)
    {
        var bytes = new byte[size];
        rng.NextBytes(bytes);
        File.WriteAllBytes(Path.Combine(target, name), bytes);
    }

    Fake("trafag_exporter.db", 2_000_000);
    // A second database that sorts BEFORE the real one. With the old "first *.db we
    // find" lookup the protocol would have made its central claim about this file.
    Fake("alte_kopie.db", 15_000);
    Fake("trafag_exporter.db-wal", 40_000);
    Fake("trafag_exporter.db-shm", 32_000);
    Fake("trafag_exporter_2026-08-01.bak", 500_000);
    Fake("trafag_exporter_2026-08-05.bak", 500_000);
    Fake("zdispo_grp.xlsx", 12_000);
    Fake("zdispo_spart.xlsx", 9_000);
    File.WriteAllText(Path.Combine(target, "app_offline.htm.disabled"), "alter rest von einem frueheren deploy");
    File.WriteAllText(Path.Combine(target, "wwwroot", "eigene-datei.txt"), "nicht aus dem build");

    var dll = Path.Combine(target, "BiDashboard.dll");
    File.WriteAllText(dll, "platzhalter, wird vom publish ersetzt");
    // The main assembly is copied with PreserveNewest: the timestamp decides whether
    // the publish replaces it at all. That is the whole point of scenario B.
    File.SetLastWriteTime(dll, placeholderStamp);

    return Directory.GetFiles(target, "*", SearchOption.AllDirectories)
        .ToDictionary(f => Path.GetRelativePath(target, f), Sha, StringComparer.OrdinalIgnoreCase);
}

DeploySettings Settings() => new()
{
    ProjectPath = Path.Combine(repo, "TrafagSalesExporter.csproj"),
    TargetDir = target,
    LocalReleaseDll = Path.Combine(repo, "bin", "Release", "net8.0", "BiDashboard.dll"),
    MainAssembly = "BiDashboard.dll",
    DatabaseFile = "trafag_exporter.db",
    BaseUrl = "",                       // no smoke tests: production must not be touched
    Routes = new List<string>(),
    ProtectedPatterns = new List<string> { "*.db", "*.db-wal", "*.db-shm", "*.bak" },
};

DeployRequest Request(bool dryRun) => new()
{
    Title = "Prueflauf der Deploy-Konsole",
    TestsGreen = true,
    TestCount = "455/455",
    Expected = new List<string> { "ManagementCockpitService", "FinanceCountryStatuses", "Nicht geprueft" },
    Forbidden = new List<string> { "Passt gegen Soll", "YtdSalesChf" },
    RunSmokeTests = false,
    DryRun = dryRun,
};

// ---------------------------------------------------------------- Guard
foreach (var evil in new[]
         {
             new[] { "publish", "/p:PublishProfile=FolderProfile" },
             new[] { "publish", "-p:DeleteExistingFiles=true" },
             new[] { "publish", "Properties/PublishProfiles/FolderProfile.pubxml" },
         })
{
    var refused = false;
    try { DeployRunner.AssertSafeArguments(evil); }
    catch (InvalidOperationException) { refused = true; }
    Check("Guard weist ab: " + string.Join(" ", evil), refused, refused ? "abgewiesen" : "DURCHGELASSEN");
}

// ------------------------------------------------- A: normaler Deploy
Console.WriteLine();
Console.WriteLine("=== A: normaler Deploy ===");
var beforeA = BuildFakeShare(new DateTime(2020, 1, 1, 8, 0, 0));
var dbPath = Path.Combine(target, "trafag_exporter.db");
var dbShaBefore = Sha(dbPath);
var logA = new StringBuilder();
var runnerA = new DeployRunner(Settings(), l => { logA.AppendLine(l); Console.WriteLine("   | " + l); });
var reportA = await runnerA.RunAsync(Request(dryRun: false), CancellationToken.None);

Check("Publish ausgefuehrt", reportA.PublishRan, reportA.PublishRan.ToString());
Check("Server-DLL ersetzt und bitgleich mit dem lokalen Build",
    reportA.ServerDllSha == reportA.LocalDllSha && reportA.ServerDllBytes > 1_000_000,
    $"{reportA.ServerDllBytes:N0} Bytes");
Check("Datenbank byteidentisch", Sha(dbPath) == dbShaBefore, "SHA256 unveraendert");
Check("Es ist wirklich trafag_exporter.db, nicht irgendeine .db",
    reportA.DatabaseFileName == "trafag_exporter.db"
    && reportA.DatabaseAfter?.Length == 2_000_000
    && reportA.DatabaseAfter?.RelativePath == "trafag_exporter.db",
    $"{reportA.DatabaseAfter?.RelativePath} / {reportA.DatabaseAfter?.Length:N0} Bytes (die zweite .db hat 15'000)");
Check("Datenbank in Laenge und Zeit unveraendert",
    reportA.DatabaseBefore is not null && reportA.DatabaseAfter is not null
    && reportA.DatabaseBefore.Length == reportA.DatabaseAfter.Length
    && reportA.DatabaseBefore.LastWriteUtc == reportA.DatabaseAfter.LastWriteUtc,
    $"{reportA.DatabaseAfter?.Length:N0} Bytes");

var buildOutput = new[] { "BiDashboard.dll", "app_offline.htm.disabled", "zdispo_grp.xlsx", "zdispo_spart.xlsx" };
var damaged = beforeA.Keys
    .Where(rel => !buildOutput.Contains(rel, StringComparer.OrdinalIgnoreCase))
    .Where(rel => !File.Exists(Path.Combine(target, rel)) || Sha(Path.Combine(target, rel)) != beforeA[rel])
    .ToList();
Check("Alle Nicht-Build-Dateien unveraendert vorhanden", damaged.Count == 0,
    damaged.Count == 0 ? "inkl. .bak, WAL/SHM, wwwroot/eigene-datei.txt" : string.Join(", ", damaged));

// Measured, not assumed: the workbooks ARE build output and get replaced by the
// repository copy on every publish. Anyone editing them on the share loses the edit.
var zdispoReplaced = new[] { "zdispo_grp.xlsx", "zdispo_spart.xlsx" }
    .All(n => File.Exists(Path.Combine(target, n)) && Sha(Path.Combine(target, n)) != beforeA[n]);
Check("ZDISPO-XLSX werden vom Publish ersetzt (Befund, kein Fehler)", zdispoReplaced, "durch Repo-Stand ueberschrieben");

Check("Diff meldet keine verschwundene Datei", reportA.Diff!.Vanished.Count == 0, $"{reportA.Diff.Vanished.Count}");
Check("Diff meldet keine veraenderte Schutzdatei", reportA.Diff.ProtectedChanged.Count == 0, $"{reportA.Diff.ProtectedChanged.Count}");
Check("app_offline.htm ist wieder weg", !File.Exists(Path.Combine(target, "app_offline.htm")), "nicht vorhanden");
Check("app_offline.htm.disabled liegt vor", File.Exists(Path.Combine(target, "app_offline.htm.disabled")), "vorhanden");
Check("Alte .disabled wurde protokolliert entfernt",
    logA.ToString().Contains("Alte app_offline.htm.disabled entfernt", StringComparison.Ordinal), "Logzeile vorhanden");
Check("Erwartete Typen und Literale gefunden", reportA.Expected.All(h => h.Found),
    string.Join(", ", reportA.Expected.Select(h => $"{h.Needle}={h.Encoding}")));
Check("Verbotene Texte nicht enthalten", reportA.Forbidden.All(h => !h.Found), "beide entfernt");
Check("Ohne Alarm", reportA.Succeeded, string.Join(" | ", reportA.Alarms));

Console.WriteLine();
Console.WriteLine("---- erzeugtes Protokoll ----");
Console.WriteLine(ProtocolWriter.Build(Request(dryRun: false), reportA));
Console.WriteLine("-----------------------------");

// ------------------------------------------- A2: Prueflauf schreibt nicht
var shaBeforeDry = Sha(Path.Combine(target, "BiDashboard.dll"));
var reportDry = await runnerA.RunAsync(Request(dryRun: true), CancellationToken.None);
Check("Prueflauf publiziert nicht", !reportDry.PublishRan, "PublishRan=false");
Check("Prueflauf laesst die DLL unangetastet", Sha(Path.Combine(target, "BiDashboard.dll")) == shaBeforeDry, "SHA256 gleich");

// -------------------------- B: Zieldatei neuer als der Build (PreserveNewest)
Console.WriteLine();
Console.WriteLine("=== B: Ziel-DLL neuer als der Build - Publish ueberspringt sie ===");
BuildFakeShare(DateTime.Now.AddDays(1));
var runnerB = new DeployRunner(Settings(), l => Console.WriteLine("   | " + l));
var reportB = await runnerB.RunAsync(Request(dryRun: false), CancellationToken.None);

Check("Publish meldet Erfolg", reportB.PublishRan, "true");
Check("DLL wurde tatsaechlich NICHT ersetzt", reportB.ServerDllBytes < 1000, $"{reportB.ServerDllBytes} Bytes");
Check("Konsole schlaegt Alarm statt es durchgehen zu lassen",
    reportB.Alarms.Any(a => a.Contains("uebersprungen", StringComparison.Ordinal)),
    reportB.Alarms.FirstOrDefault() ?? "(kein Alarm)");
Check("Protokoll benennt den Fehlschlag",
    ProtocolWriter.Build(Request(dryRun: false), reportB).Contains("nicht die Kopie des lokalen Release-Builds", StringComparison.Ordinal),
    "ACHTUNG-Zeile im Protokoll");

// ------------------------------------- C: das Fenster, das Ingo wirklich anklickt
// Never opened on screen, run as a pure dry run with the smoke step off, so nothing
// is written and no production URL is touched.
Console.WriteLine();
Console.WriteLine("=== C: GUI-Fenster oeffnen und einen Prueflauf durchklicken ===");
var guiThread = new Thread(GuiProbe);
guiThread.SetApartmentState(ApartmentState.STA);   // WinForms needs an STA thread
guiThread.Start();
guiThread.Join();

void GuiProbe()
{
try
{
    const System.Reflection.BindingFlags Priv =
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var formType = typeof(MainForm);
    var form = new MainForm();
    // Button.PerformClick does nothing on a control that is not visible, so the form
    // has to be shown to exercise the real click path - parked off-screen and
    // transparent so nothing appears in front of the user.
    form.StartPosition = FormStartPosition.Manual;
    form.Location = new System.Drawing.Point(-32000, -32000);
    form.ShowInTaskbar = false;
    form.Opacity = 0;
    form.Show();

    // Point the form at the fake share instead of the production target.
    var settingsField = formType.GetField("_settings", Priv)!;
    var live = (DeploySettings)settingsField.GetValue(form)!;
    var probe = Settings();
    live.ProjectPath = probe.ProjectPath;
    live.TargetDir = probe.TargetDir;
    live.LocalReleaseDll = probe.LocalReleaseDll;
    live.MainAssembly = probe.MainAssembly;
    live.DatabaseFile = probe.DatabaseFile;
    live.BaseUrl = "";
    live.Routes = new List<string>();
    live.ProtectedPatterns = probe.ProtectedPatterns;

    ((CheckBox)formType.GetField("_dryRun", Priv)!.GetValue(form)!).Checked = true;
    ((CheckBox)formType.GetField("_smoke", Priv)!.GetValue(form)!).Checked = false;
    ((CheckBox)formType.GetField("_testsGreen", Priv)!.GetValue(form)!).Checked = true;
    ((TextBox)formType.GetField("_testCount", Priv)!.GetValue(form)!).Text = "455/455";
    ((TextBox)formType.GetField("_title", Priv)!.GetValue(form)!).Text = "GUI-Prueflauf";
    ((TextBox)formType.GetField("_expected", Priv)!.GetValue(form)!).Text = "ManagementCockpitService";

    var runButton = (Button)formType.GetField("_run", Priv)!.GetValue(form)!;
    var protocolBox = (TextBox)formType.GetField("_protocol", Priv)!.GetValue(form)!;
    var copyButton = (Button)formType.GetField("_copy", Priv)!.GetValue(form)!;
    var statusLabel = (Label)formType.GetField("_status", Priv)!.GetValue(form)!;
    Check("Fenster baut sich ohne Layoutfehler auf", form.Controls.Count > 0, $"{form.Controls.Count} Wurzelcontrol(s)");

    runButton.PerformClick();
    var deadline = DateTime.UtcNow.AddMinutes(3);
    while (DateTime.UtcNow < deadline
           && !(runButton.Enabled && protocolBox.TextLength > 0)
           && !statusLabel.Text.StartsWith("Abgebrochen", StringComparison.Ordinal))
    {
        Application.DoEvents();
        Thread.Sleep(50);
    }
    Application.DoEvents();

    Check("Prueflauf ueber den Knopf laeuft durch", runButton.Enabled && protocolBox.TextLength > 0, statusLabel.Text);
    Check("Protokollfeld ist gefuellt", protocolBox.TextLength > 200, $"{protocolBox.TextLength} Zeichen");
    Check("Kopierknopf ist freigeschaltet", copyButton.Enabled, copyButton.Enabled.ToString());
    form.Dispose();
}
catch (Exception ex)
{
    Check("GUI-Prueflauf ohne Ausnahme", false, ex.Message);
}
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? "ALLE PRUEFUNGEN GRUEN" : $"{failures} PRUEFUNG(EN) ROT");
return failures == 0 ? 0 : 1;
