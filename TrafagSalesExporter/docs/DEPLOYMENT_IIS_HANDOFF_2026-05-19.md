# Deployment / IIS Handoff 2026-05-19

## Ziel

`TrafagSalesExporter` bleibt das fuehrende Projekt, wird aber fuer den Server als ASP.NET/IIS-Webanwendung im bisherigen `BiDashboard`-Schema veroeffentlicht.

Das separate lokale Projekt `..\BiDashboard` wird fuer die aktuelle Weiterarbeit nicht benoetigt. Die relevanten Publish-/Hosting-Einstellungen wurden in `TrafagSalesExporter` uebernommen.

## Server / URL / Publish-Pfad

Server / DNS:

```text
trch-webapp-bidashboard.trafagch.local
tragvapp401.trafagch.local
10.120.1.17
```

Publish-Share:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\
```

Wahrscheinliche Browser-URL:

```text
https://trch-webapp-bidashboard.trafagch.local/BiDashboard/
```

Hinweis: Ohne `/BiDashboard` bzw. ueber HTTP kam lokal weiterhin `404 Microsoft-HTTPAPI/2.0`. Mit `https://.../BiDashboard` meldete der Browser laut Test `500`, was bedeutet, dass IIS die Application erreicht.

## Aktueller Publish-Stand

Das Projekt `TrafagSalesExporter.csproj` erzeugt jetzt eine `BiDashboard`-Publish-Ausgabe:

```text
BiDashboard.dll
BiDashboard.deps.json
BiDashboard.runtimeconfig.json
BiDashboard.staticwebassets.endpoints.json
web.config
wwwroot
runtimes
trafag_exporter.db
Microsoft.AspNetCore.Authentication.Negotiate.dll
```

Bewusst:

- keine EXE / kein AppHost
- `UseAppHost=false`
- `AssemblyName=BiDashboard`
- `RootNamespace=TrafagSalesExporter`
- `Microsoft.AspNetCore.Authentication.Negotiate` ist enthalten wie im alten `BiDashboard`-Projekt
- lokale Build-/Probe-Ordner werden vom Publish ausgeschlossen

Relevante Commits:

```text
8d10372 Configure Trafag web publish profile
f128d35 Publish web app without apphost
e9b616f Align Trafag publish output with BiDashboard
1533570 Exclude local build artifacts from web publish
5087a7c Enable IIS publish diagnostics
```

## Veroeffentlichen

Standard-Publish:

```powershell
dotnet publish .\TrafagSalesExporter.csproj -c Release --no-restore /p:PublishProfile=FolderProfile --verbosity minimal
```

Das Publish-Profil liegt hier:

```text
Properties/PublishProfiles/FolderProfile.pubxml
```

## Diagnose-web.config

Im Repo liegt eine explizite `web.config`, damit IIS/ANCM Diagnoseinformationen liefern kann:

```xml
<httpErrors errorMode="Detailed" existingResponse="PassThrough" />
<aspNetCore processPath="dotnet"
            arguments=".\BiDashboard.dll"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess" />
```

Der Ordner `logs` wurde auf dem Share angelegt:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\logs
```

Nach einem Browser-Reload mit `500` war der Ordner weiterhin leer. Das deutet auf eines der folgenden Themen:

- App-Pool darf nicht in `logs` schreiben.
- Fehler passiert auf IIS/ANCM-Ebene vor dem App-Start.
- IIS verwendet einen anderen Physical Path als den Share-Ordner.

## Rechtebefund

ACL auf dem Publish-Ordner zeigte:

```text
IIS_IUSRS: ReadAndExecute
TRAFAGCH\koi: FullControl
```

Versuch, Rechte selbst zu setzen:

```powershell
icacls "\\trch-webapp-bidashboard.trafagch.local\BiDashboard$" /grant "IIS_IUSRS:(OI)(CI)M" /T
```

Ergebnis:

```text
Zugriff verweigert
```

Auch per SID fuer `IIS_IUSRS` wurde es abgelehnt. Wir koennen publishen und Dateien schreiben, aber keine NTFS-/Share-Rechte auf dem Server aendern.

## Wahrscheinlichster aktueller Fehler

Die App startet in `Program.cs` sofort die Datenbankinitialisierung:

```text
DatabaseInitializationService.InitializeAsync()
db.Database.EnsureCreatedAsync()
PRAGMA journal_mode=WAL
Schema-Wartung
SeedDefaults
```

SQLite schreibt bzw. aendert dabei mindestens:

```text
trafag_exporter.db
trafag_exporter.db-shm
trafag_exporter.db-wal
```

Wenn der App-Pool nur Lesen/Ausfuehren hat, kann das beim Start als `500` enden.

## Server-Spezialist: konkrete Bitte

Bitte auf dem Server `tragvapp401` pruefen:

1. IIS Application `/BiDashboard` zeigt auf genau den Ordner mit dieser Datei:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\web.config
```

oder auf den entsprechenden lokalen physischen Pfad.

2. App-Pool-Identity ermitteln.

3. Dieser konkreten App-Pool-Identity `Modify` geben auf:

```text
Publish-Ordner
logs\
trafag_exporter.db
trafag_exporter.db-shm
trafag_exporter.db-wal
```

Alternativ voruebergehend `IIS_IUSRS` mit `Modify`, wenn die genaue App-Pool-Identity nicht klar ist.

4. App-Pool neu starten.

5. URL testen:

```text
https://trch-webapp-bidashboard.trafagch.local/BiDashboard/
```

6. Wenn weiter `500`, bitte pruefen:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\logs
```

und Windows Event Viewer:

```text
IIS AspNetCore Module V2
Application Error
.NET Runtime
```

## Aktueller Restzustand im Git-Working-Tree

Es gibt weiterhin alte/unabhaengige lokale Dateien und geloeschte Alt-Dokumente im Working Tree. Diese wurden bewusst nicht committed:

```text
../BiDashboard/
.tmp_tools/
Tools/FinanceProbe/.tmp_tools/
verify_probe_out*/
docs/CFO_Kurzbericht_270515_NEU.docx
docs/FINANCE_AMPEL_LAENDER_2026-05-19.xlsx
financeprobe.*.log
mainapp.*.log
```

Ausserdem sind mehrere alte Dateien als geloescht markiert. Nicht blind committen, bevor klar ist, ob sie wirklich entfernt werden sollen.
