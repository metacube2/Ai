using DeployConsole;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var settingsPath = Path.Combine(root, "Tools", "DeployConsole", "deploy.settings.json");
var settings = DeploySettings.Load(settingsPath);
if (!string.Equals(settings.TargetDir.TrimEnd('\\'),
        @"\\trch-webapp-bidashboard.trafagch.local\BiDashboard$", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException($"Unerwartetes Deploy-Ziel: {settings.TargetDir}");

// Gebaut wird aus dem sauberen Worktree am Commit 6cc0035 (Ordnername eafd1c5), NICHT aus dem
// Arbeitsbaum: dort liegen unkommittierte Finance_All-Aenderungen, die nicht ausgeliefert werden duerfen.
const string ReleaseRoot = @"C:\TMP\TrafagSalesExporter_release_eafd1c5\TrafagSalesExporter";
settings.ProjectPath = Path.Combine(ReleaseRoot, "TrafagSalesExporter.csproj");
settings.LocalReleaseDll = Path.Combine(ReleaseRoot, "bin", "Release", "net8.0", "BiDashboard.dll");
if (!File.Exists(settings.ProjectPath)) throw new FileNotFoundException("Release-Worktree fehlt.", settings.ProjectPath);
if (File.Exists(Path.Combine(ReleaseRoot, "Services", "FinanceAllExportService.cs")))
    throw new InvalidOperationException("Finance_All liegt im Release-Worktree. Abbruch.");

var databasePath = Path.Combine(settings.TargetDir, settings.DatabaseFile);
if (!File.Exists(databasePath)) throw new FileNotFoundException("Produktiv-DB fehlt.", databasePath);

// Seit 2026-11-20: kein Deploy waehrend eines Einkauf-Laufs. Der Deploy um 12:51 brach einen Delta ab, der seit
// 12:31 lief; die Pruefung ueber die -wal-Groesse sah ihn nicht, weil der Delta erst am Ende schreibt. Gelesen
// wird das neueste Serverprotokoll (Start- gegen Ende-Zeile, Regel in Services/PurchasingRunLog.cs).
// Uebersteuern nur bewusst mit --trotz-einkauf-lauf.
#if PURCHASING_RUN_LOG
{
    var logDir = Path.Combine(settings.TargetDir, "logs");
    var newestLog = Directory.Exists(logDir)
        ? new DirectoryInfo(logDir).GetFiles("stdout_*.log").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault()
        : null;
    if (newestLog is null)
    {
        Console.WriteLine("Einkauf-Pruefung: kein Serverprotokoll gefunden, Pruefung nicht moeglich.");
    }
    else
    {
        List<string> lines;
        using (var stream = new FileStream(newestLog.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var reader = new StreamReader(stream))
        {
            lines = [];
            while (reader.ReadLine() is { } line) lines.Add(line);
        }
        var runState = TrafagSalesExporter.Services.PurchasingRunLog.Evaluate(
            lines, DateTime.Now, TrafagSalesExporter.Services.PurchasingRunLog.DefaultMaxRunAge);
        Console.WriteLine($"Einkauf-Pruefung ({newestLog.Name}): {runState.Reason}");
        if (runState.Running && !args.Contains("--trotz-einkauf-lauf"))
        {
            Console.WriteLine($"ABBRUCH: Einkauf-Lauf laeuft ({runState.StartLine}). Nach dessen Ende erneut starten " +
                              "oder bewusst mit --trotz-einkauf-lauf uebersteuern.");
            Environment.Exit(2);
        }
    }
}
#else
Console.WriteLine("Einkauf-Pruefung: im Release-Worktree fehlt Services/PurchasingRunLog.cs (Commit vor 2026-11-20), " +
                  "bitte im Serverprotokoll von Hand pruefen.");
#endif

// Entscheid Ingo 2026-10-13 nachmittags: Sicherung nur noch einmal pro Woche. Der Runner sichert von selbst,
// wenn die neueste Sicherung auf dem Share aelter als sieben Tage ist; --backup erzwingt eine, --no-backup
// unterdrueckt auch die Wochensicherung. Vorher galt: nur auf Ansage (--backup).
var newestBackup = Directory.GetFiles(settings.TargetDir, settings.DatabaseFile + ".before-*.bak")
    .Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
var backupAgeDays = newestBackup is null ? double.MaxValue : (DateTime.Now - newestBackup.LastWriteTime).TotalDays;
var weeklyDue = backupAgeDays > 7;
// --only-backup (Wunsch Ingo 2026-10-14): nur sichern, kein Publish, kein Neustart der App.
var onlyBackup = args.Contains("--only-backup");
if (onlyBackup || args.Contains("--backup") || (weeklyDue && !args.Contains("--no-backup")))
{
    Console.WriteLine(onlyBackup ? "Nur Sicherung (--only-backup), kein Deploy."
        : args.Contains("--backup")
        ? "Sicherung erzwungen (--backup)."
        : $"Wochensicherung faellig: neueste Sicherung {(newestBackup is null ? "keine" : $"{newestBackup.Name} vom {newestBackup.LastWriteTime:dd.MM.yyyy HH:mm}")}.");
    var backup = await DatabaseBackup.EnsureBackupAsync(
        databasePath, onlyBackup ? "sicherung" : "wochensicherung", DateTime.Now, Console.WriteLine);
    Console.WriteLine($"Backup-Nachweis: {backup.Path}");
    if (onlyBackup)
    {
        Console.WriteLine("Fertig, nur Sicherung.");
        Environment.Exit(0);
    }
}
else
{
    Console.WriteLine($"Keine Sicherung: neueste ist {newestBackup?.Name} vom {newestBackup?.LastWriteTime:dd.MM.yyyy HH:mm}, " +
        $"juenger als 7 Tage (Wochenregel, Entscheid Ingo 2026-10-13).");
}

settings.Routes =
[
    "",
    "einkauf",
    "marktsegmente",
    "management-cockpit",
    "einkauf/aufriss",
    "forum",
    "projekte",
    "operations",
    "operations/kpi",
    "operations/kpi/liefertreue"
];

var runner = new DeployRunner(settings, Console.WriteLine);
var request = new DeployRequest
{
    Title = "Nachzug 12.11.: Kleinigkeiten c5c9b4a/19ca282/548b10e, Rexx Hub vorbereitet Schalter aus (1e99745/d99fbe6), Stand e956183, ohne Finance_All",
    TestsGreen = true,
    TestCount = "1547/1547",
    RunSmokeTests = true,
    // Wirknachweis je ausgeliefertem Commit, dazu die DE-Ressource vom 2026-09-10, die bleiben muss.
    Expected =
    [
        "PurchasingStockValueHistory",
        "Trafag Reddit durchsuchen",
        "UX_PmTasks_Project_Number",
        "Swimlanes: Person",
        "UX_ForumVotes_Target_Voter",
        "Erster Verlaufspunkt:",
        "Einkauf-Delta nachgeholt",
        "BiDashboard/favicon.svg",
        "HR-Kennzahlen werden berechnet …",
        "Finanzdaten werden geladen …",
        "Fluktuation Prognose gleitend",
        "NotifyRatesChanged",
        "GetSourceStampAsync",
        "PRAGMA mmap_size",
        "CentralRecordsCacheLifetime",
        "Einkauf-Standardansicht vorberechnet in {Seconds:N1} s",
        "UX_SegmentNamePatterns_Pattern",
        "vor dem Startdatum verworfen=",
        "de_customer_invoice_map_2026-09-10.json",
        "SharePoint-Ordner liefert mehr als",
        "AbsenceRatesReliable",
        "MissingExchangeRatePositionCount",
        "Zeitstempel 0 (statisch oder ungealtert)",
        "Avg. unit price by year (quantity-weighted)",
        "Im Handbuch suchen",
        "SportsEsports",
        "Standard Cost CHF (per unit)",
        "BudgetRateDecision20261007Applied",
        "ShopZd05Set",
        "TrafagShopfloor",
        "PurchasingComponentDispoCache",
        "EinkKontraktSet",
        "Tabellen im PDF enthalten die aktuell angezeigte Seite",
        "Dieser Export ist gekappt",
        "hat die Wochen_Todo abgeglichen",
        "trafagTheme.set",
        "Hell/Dunkel umschalten",
        "BillToDef",
        "sup_adr_any",
        "HrKpiSapRefreshService",
        "HR_KPI_Export",
        "ReachesAbsenceRed",
        "HoursPerWorkdayFor",
        "Unfalltage laufender Monat",
        "SapGatewayHrAbsenceReader",
        "HR_Absenzen_SAP.xlsx",
        "Ferien bezogen im Zeitraum",
        "ResolveSapCoverageStart",
        "FP_DICHTESEN1/2",
        "IsShowingPreviousData",
        "Neue Einkaufsdaten werden eingerechnet.",
        "trafagTheme.setSkin",
        "Design umschalten: Trafag CI oder klassisch",
        "UiTextBerneseTranslations",
        "trafagTheme.setDim",
        "Hintergrund heller oder dunkler",
        "LogisticsLiveService",
        "LogRueckSet",
        "Abruf alle 30 Sekunden, nur solange diese Seite offen ist",
        "BomInheritanceService",
        "Verwendungsbaum nach oben",
        "NetworkProbeService",
        "/sap/public/ping",
        "ClientConnectionTracker",
        "AdAnalysis",
        "UncoveredRanges",
        "dc-ldap:",
        "AdInfrastructureService",
        "msDS-NCReplInboundNeighbors",
        "AdSnapshotService",
        "NetworkAdMetrics",
        "LegacyScores",
        "MigrationWaves",
        "DevicesPerSite",
        "SalesDataService",
        "CrossSellOpportunities",
        "verkauf/weltkarte",
        "ControllingAnalytics",
        "finance-cockpit/controlling",
        "WorldImpactAnalytics",
        "WorldGdeltFiles",
        "weltlage/ereignisse",
        "LogisticsBinLayout",
        "wh3d-pallet",
        "eurofxref-hist-90d.xml",
        "TrailingDigits",
        "LogisticsProductivity",
        "LogisticsCapacityDay",
        "LogisticsPeopleAccess",
        "LogKapSet",
        "SalesInteractive",
        "verkauf/interaktiv",
        "PurchasingInteractiveService",
        "KpiLiefertreueSync",
        "C_SLSORDDELIVPERFANLYTSQRY",
        "ServerLoadSampler",
        "UsagePageViews",
        "CREATE TABLE IF NOT EXISTS AccessPasswords",
        "admin/passwoerter",
        "/api/integration/push",
        "operations.login_required",
        "IsPlaceholder",
        "HrKpiInteractive",
        "TimelineEmployees",
        "ForecastFromQuarter",
        "DataOwnerRules",
        "BuildRexxGapSicknessRows",
        "ActiveStatuses",
        "juengste Krankmeldungen aus Rexx ergaenzt",
        "Quote ohne Langzeit",
        "AccidentAbsenceTypes",
        "TurnoverTileLabel",
        "Hrcockpitgesamtdashboard.xlsx",
        "BuildRexxTopUpRows",
        "Gesamtabfrage #775",
        "Hr-cockpit-absenzen-je-fall.xlsx",
        "MergeRexxCases",
        "Abwesenheiten je Fall, ab",
        "AccidentDaysCurrentMonth",
        "Unfallfaelle SAP und Rexx im Monat"
    ],
    // Finance_All ist unfertig und darf nicht mitkommen.
    Forbidden = ["Unfalltage Total", "FinanceAllExportService", "Sollzeit / 8.4h", "Stunden / 8.4h", "de_customer_invoice_map_2026-09-09.json", "C_IT01_0022987", "ProjectManagementService"],
    DryRun = false
};

var report = await runner.RunAsync(request, CancellationToken.None);
Console.WriteLine();
Console.WriteLine("=== PROTOKOLLABSATZ ===");
Console.WriteLine(ProtocolWriter.Build(request, report));
if (!report.Succeeded) Environment.ExitCode = 1;
// 2026-10-13: Der Runner blieb nach "Fertig" mehrfach haengen (offener Vordergrund-Thread) und sperrte
// DeployConsole.dll fuer den naechsten Build. Deshalb hart beenden.
Environment.Exit(Environment.ExitCode);
