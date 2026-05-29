# Deployment / IIS Handoff 2026-05-19

Letzter Nachtrag: 2026-05-29

## Nachtrag 2026-05-29 Deploy Sparten-Finanzanalyse

Durchgefuehrt:

- Release-Publish aus `TrafagSalesExporter` nach:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\
```

- Befehl:

```powershell
dotnet publish .\TrafagSalesExporter.csproj -c Release --no-restore /p:PublishProfile=FolderProfile --verbosity minimal
```

- App wurde fuer den Publish kurz per `app_offline.htm` gestoppt und danach wieder online geschaltet.

Deploy-Inhalt:

- Neuer Reiter `Sparten-Finanzanalyse` in `Management Analyse`.
- Umsatzabdeckung nach Produktzuordnungsstatus:
  - Zugeordnet
  - Nicht zugeordnet
  - Nicht im TR-AG-Stamm
  - Material fehlt
- Umsatz nach Produktsparte, Produktfamilie und PAPH1.
- Umsatzabdeckung nach Land/TSC.
- Seed-Korrektur, damit SAP-Quelle `P = ProductDivisionRefSet` beim App-Start aktiv bleibt.

Share-/DB-Pruefung:

- `BiDashboard.dll` Zeitstempel `29.05.2026 10:42:44`.
- `app_offline.htm` wurde entfernt.
- Server-DB:
  - `ProductRows = 36'847`
  - `TR-AG Referenzmaterialien = 6'805`
  - `P ProductDivisionRefSet` aktiv

Validierung:

```powershell
dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-division-finance
```

Ergebnis:

```text
80/80 Tests gruen
```

## Nachtrag 2026-05-29 Deploy Produktsparten-Mapping

Durchgefuehrt:

- Release-Publish aus `TrafagSalesExporter` nach:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\
```

- Befehl:

```powershell
dotnet publish .\TrafagSalesExporter.csproj -c Release --no-restore /p:PublishProfile=FolderProfile --verbosity minimal
```

- Share-Pruefung nach Publish:
  - `BiDashboard.dll` Zeitstempel `29.05.2026 09:19:43`
  - `BiDashboard.deps.json` Zeitstempel `29.05.2026 09:19:44`
  - `web.config` Zeitstempel `29.05.2026 09:19:50`
  - `trafag_exporter.db` Zeitstempel `29.05.2026 09:18:42`

Deploy-Inhalt:

- Produktspartenfelder im Web-Datenmodell und Excel-Export.
- SAP-Gateway-Join-Konfiguration fuer `ProductDivisionRefSet`.
- Neuer Reiter `Zentrale Spartenzuordnung` in `Management Analyse`.
- Lokale SQLite-Konfiguration wurde mit publiziert; `ProductDivisionRefSet` ist dort als aktive zweite SAP-Quelle fuer `ZSCHWEIZ` konfiguriert.

Validierung:

```powershell
dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-deploy-20260529
```

Ergebnis:

```text
80/80 Tests gruen
```

Einschraenkung:

- `Invoke-WebRequest` gegen `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/` konnte von der Entwicklungsmaschine nicht als fachlicher Smoke-Test verwendet werden, weil die HTTPS-Verbindung lokal mit Empfangs-/Credential-Fehler abbricht. Das entspricht dem bereits dokumentierten lokalen Schannel-/Client-Credential-Thema.
- Der Publish selbst und die Share-Dateien wurden erfolgreich verifiziert.

Nacharbeit im Web:

- Im Export Dashboard `ZSCHWEIZ` erneut exportieren/laden, damit `CentralSalesRecords` die Produktfelder aus `ProductDivisionRefSet` erhaelt.
- Danach `Management Analyse` -> `Zentrale Spartenzuordnung` pruefen.
- Wenn dort `TR-AG Referenz = 0` steht, ist die zentrale Referenz noch nicht neu geladen oder der SAP-Join liefert im Webserver-Kontext keine Produktdaten.

## Nachtrag 2026-05-27: Upgreat Firewall-Freigabe fuer neuen Webserver

Wichtig: Upgreat muss die ausgehenden Verbindungen fuer den neuen IIS-/Publish-Webserver freischalten, nicht fuer den lokalen Entwicklungs-PC.

Quelle / Absender:

```text
trch-webapp-bidashboard.trafagch.local
tragvapp401.trafagch.local
10.120.1.17
```

Der lokale PC bzw. lokale Uebergangsserver auf Port `5000` ist nur fuer temporaere Tests relevant. Eine dort bereits gemachte Firewall-Freigabe ersetzt nicht die Freigabe fuer den produktiven/publizierten Webserver.

Bekannte Zielsysteme / Ports:

| Zweck | Ziel | Port | Richtung |
| --- | --- | ---: | --- |
| HANA Internal / BI1 / Standorte FR, IT, US | `10.194.65.22` | `30015` | Webserver -> Ziel |
| India HANA / Sage Indien | `20.197.20.60` | `30015` | Webserver -> Ziel |
| SAP OData / ZSCHWEIZ CH/AT | `10.194.64.29` | `8000` | Webserver -> Ziel |
| SharePoint / Graph / Manual-Importe / Upload | `trafagag.sharepoint.com` | `443` | Webserver -> Ziel |

Wahrscheinlich benoetigt Upgreat eine laengere Standort-/Zielsystemliste aus der produktiven Konfiguration, nicht nur diese Kurzliste. Die vollstaendige Liste sollte aus den in der App gepflegten Quellsystemen/Standorten bzw. `HanaServers`, `SourceSystemDefinitions`, SAP-Gateway-Konfiguration und SharePoint-Konfiguration exportiert oder in der Sitzung abgestimmt werden.

Mail-/Ticket-Kerntext fuer Upgreat:

```text
Bitte nicht den lokalen Entwicklungs-PC freischalten, sondern den neuen Webserver:

Source:
- trch-webapp-bidashboard.trafagch.local / tragvapp401.trafagch.local
- IP: 10.120.1.17

Benötigte ausgehende Verbindungen:
- 10.194.65.22:30015 HANA Internal / BI1
- 20.197.20.60:30015 India HANA
- 10.194.64.29:8000 SAP OData / ZSCHWEIZ
- trafagag.sharepoint.com:443 SharePoint / Microsoft Graph

Bitte diese Verbindungen vom Webserver zu den Zielsystemen freischalten. Falls weitere Standort-HANA-/Sage-/SAP-Ziele in der produktiven Konfiguration vorhanden sind, diese bitte ebenfalls aufnehmen.
```

Offen:

- Vollstaendige Standortliste mit Host/IP/Port aus der produktiven App-Konfiguration pruefen.
- Klaeren, ob SharePoint/Graph zusaetzlich Microsoft-Login-/Graph-Endpunkte benoetigt oder ob `trafagag.sharepoint.com:443` fuer die Netzwerkfreigabe ausreichend ist.
- Nach Freigabe direkt auf dem Webserver Verbindungstests fuer HANA, SAP OData und SharePoint ausfuehren.

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
e3b9d8d Switch IIS hosting to out-of-process
1dc336d Enable IIS detailed startup diagnostics
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
            hostingModel="outofprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_DETAILEDERRORS" value="true" />
  </environmentVariables>
</aspNetCore>
```

Der Ordner `logs` wurde auf dem Share angelegt:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\logs
```

Nach einem Browser-Reload mit `500` war der Ordner weiterhin leer. Das deutet auf eines der folgenden Themen:

- App-Pool darf nicht in `logs` schreiben.
- Fehler passiert auf IIS/ANCM-Ebene vor dem App-Start.
- fehlendes oder defektes .NET 8 Hosting Bundle / AspNetCoreModuleV2.

## Nachtrag 2026-05-20: aktueller 500-Befund

Geprueft:

- `diag.txt` wurde direkt in den Publish-Ordner geschrieben:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\diag.txt
```

- Browser-Test:

```text
https://trch-webapp-bidashboard.trafagch.local/BiDashboard/diag.txt
```

- Ergebnis im Browser:

```text
BiDashboard publish folder reached 2026-05-20T08:19:14.2667783+02:00
```

Schlussfolgerung:

- IIS-URL `/BiDashboard` zeigt auf den richtigen Publish-Ordner.
- Binding/virtueller Pfad/Physical Path sind fuer statische Dateien korrekt.
- Der `500` kommt nicht mehr von einer falschen URL.
- Der Fehler entsteht beim ASP.NET-Core-App-Start oder im ASP.NET-Core-IIS-Modul.

Zusaetzlich geprueft:

- `BiDashboard.dll` konnte aus dem Publish-Ordner per `dotnet .\BiDashboard.dll` gestartet werden und brach nicht sofort mit einer Exception ab.
- Dabei blieb ein lokaler Testprozess `dotnet` kurz aktiv und sperrte `BiDashboard.dll`; Prozess wurde beendet und danach erfolgreich neu publiziert.
- Nach erneutem Browser-Aufruf blieb `logs` weiterhin leer.
- HTTPS-Test von der Entwickler-Maschine per `curl` scheitert vor HTTP wegen Schannel/Client-Credentials:

```text
SEC_E_NO_CREDENTIALS (0x8009030e)
```

Diese lokale `curl`-Einschraenkung ist nicht der IIS-500-Fehler im Browser.

Aktueller Verdacht in Prioritaet:

1. .NET 8 Hosting Bundle / AspNetCoreModuleV2 fehlt oder ist nicht korrekt installiert.
2. App Pool ist nicht passend fuer ASP.NET Core eingestellt.
3. App-Pool-Identity hat noch nicht alle noetigen Rechte fuer Start, SQLite oder Logs.
4. Details stehen nur im Windows Event Viewer, weil stdout nicht erzeugt wird.

Wichtig:

- Der Server braucht kein installiertes Microsoft Excel.
- XLSX wird ueber ClosedXML/OpenXML gelesen und geschrieben.
- Eine Umstellung auf CSV ist fuer dieses Deployment-Problem nicht noetig.

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

Spaeterer Befund:

- Auf Publish-Ordner und `logs` war eine konkrete App-Pool-SID mit `Modify` sichtbar.
- `IIS_IUSRS` hatte weiterhin nur `ReadAndExecute`.
- Trotz dieser sichtbaren App-Pool-SID blieben stdout-Logs leer; daher reicht der ACL-Befund allein nicht zur Erklaerung.

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

Wenn der App-Pool nur Lesen/Ausfuehren hat, kann das beim Start als `500` enden. Da spaeter aber eine konkrete App-Pool-SID mit `Modify` sichtbar war, muessen zusaetzlich .NET Hosting Bundle, App-Pool-Konfiguration und Event Viewer geprueft werden.

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

## Kurzmeldung an Server-Spezialist

```text
diag.txt unter https://trch-webapp-bidashboard.trafagch.local/BiDashboard/diag.txt ist erreichbar.
Der IIS-Pfad stimmt also.

Die App selbst liefert weiterhin 500, und \\...\BiDashboard$\logs bleibt leer, obwohl stdoutLogEnabled=true gesetzt ist.
Die App ist als .NET 8 ASP.NET-Core-App publiziert, ohne EXE/AppHost, Start via:
dotnet .\BiDashboard.dll

Bitte am Server pruefen:
1. .NET 8 Hosting Bundle installiert/repariert?
2. AspNetCoreModuleV2 im IIS vorhanden?
3. App Pool:
   - .NET CLR Version = No Managed Code
   - Managed Pipeline Mode = Integrated
   - Enable 32-bit Applications = False
4. Event Viewer > Windows Logs > Application:
   - IIS AspNetCore Module V2
   - .NET Runtime
   - Application Error
5. App-Pool-Identity hat Modify auf Publish-Ordner, logs und trafag_exporter.db*

Microsoft Excel muss nicht installiert sein; XLSX wird ueber ClosedXML/OpenXML gelesen.
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

## Nachtrag 2026-05-20 IIS /BiDashboard PathBase

Die App ist fuer Betrieb unter `/BiDashboard` vorbereitet.

Relevant:

- `web.config` setzt:

```xml
<environmentVariable name="ASPNETCORE_PATHBASE" value="/BiDashboard" />
```

- `Program.cs` liest `ASPNETCORE_PATHBASE` und ruft `UsePathBase(...)` auf.
- `Components/App.razor` setzt `<base href>` dynamisch:
  - lokal ohne PathBase: `/`
  - Server mit PathBase: `/BiDashboard/`

Damit ist die erwartete Server-URL:

```text
https://trch-webapp-bidashboard.trafagch.local/BiDashboard/
```

Wenn die App im stdout-Log startet, aber Browser weiter `404` zeigt, zuerst IIS Application/Binding pruefen:

- Ist `BiDashboard` eine echte IIS Application und nicht nur ein Ordner?
- Zeigt sie auf `C:\inetpub\wwwcust\BiDashboard` bzw. den aktuellen Publish-Ordner?
- Stimmen Hostname, Port 443 und Zertifikat?

Bekannter Login-Stand:

- Wenn ein Browser-Popup `Diese Website fordert Sie auf, sich anzumelden` erscheint, ist das IIS/Windows Authentication.
- Die App selbst hat in `appsettings.json` aktuell `Security.Enabled=false`.
- Ein Login-Popup kommt daher von IIS, nicht von den App-internen HR-/Finance-Passwortseiten.
