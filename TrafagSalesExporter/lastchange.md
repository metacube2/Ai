# Last Change 2026-05-04

## In-App-Schulungen und Finance-Detaildoku 2026-05-21

Geaendert:

- Neue HTML-/Razor-Schulungsseite `HR KPI Schulung` unter `/hr-kpi/schulung`.
- Neue HTML-/Razor-Schulungsseite `Finance Schulung` unter `/finance-cockpit/schulung`.
- Navigation erweitert:
  - `Finance Cockpit` enthaelt jetzt `Finance Schulung`.
  - `HR KPI (Login)` ist jetzt eine Gruppe mit `HR Dashboard` und `HR KPI Schulung`.
- Finance-Schulung ist wie die restlichen Finance-Seiten ueber die Finance-Cockpit-Entsperrung geschuetzt.
- Schulungsseiten enthalten Tabellen, Checklisten, Prozessablauf und eingebettete Grafiken aus `wwwroot/training`.
- Sprachtexte fuer die neuen Menuepunkte in Englisch, Spanisch, Italienisch und Hindi ergaenzt.
- Word-Schulungsdokumente fuer HR und Finance neu erzeugt und Umlaut-Schreibweisen korrigiert.
- Neue Markdown-Doku `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md` beschreibt den aktuellen Delta-/Vollfile-Stand:
  - UK kann Basis plus Deltas lesen.
  - Spanien und Deutschland muessen vollstaendige Dateien liefern.
  - Manual-Importe ersetzen pro Standort den aktuellen Stand in `CentralSalesRecords`.

Verifiziert:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal --no-restore -p:BaseOutputPath=.tmp_build\bin\ -p:BaseIntermediateOutputPath=.tmp_build\obj\`
- Normaler Debug-Build war lokal durch eine von Visual Studio/.NET Host gesperrte `bin\Debug\net8.0\BiDashboard.dll` blockiert.

## Lokaler Uebergangsserver bis IIS-Fix 2026-05-21

Zweck:

- Falls der zentrale IIS-Server noch nicht erreichbar ist, kann die App voruebergehend auf dem Entwicklungs-PC laufen.
- Andere Mitarbeitende greifen dann im Firmennetz ueber die IP des PCs zu.
- Ausfuehrliche Betriebsdoku: `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md`.

Start auf dem Entwicklungs-PC:

```powershell
dotnet run --urls "http://0.0.0.0:5000"
```

Nachtrag:

- `Properties/launchSettings.json` wurde so angepasst, dass das Entwicklungsprofil zusaetzlich auf `http://0.0.0.0:5000` lauscht.
- Bei Start aus Visual Studio bzw. ueber das Projektprofil bleibt `https://localhost:55415` lokal verfuegbar, Port `5000` ist aber zusaetzlich fuer andere PCs erreichbar.
- Falls bereits eine alte Visual-Studio-Instanz laeuft, App stoppen und neu starten, damit die geaenderte URL-Bindung aktiv wird.

Zugriff von anderen PCs:

```text
http://<PC-IP>:5000
```

Aktueller Stand vom 2026-05-21:

```text
PC-IP im WLAN/Firmennetz: 172.16.9.185
Lokale Test-URL: http://172.16.9.185:5000
```

IP des PCs ermitteln:

```powershell
ipconfig
```

Firewall-Regel einmalig in einer PowerShell "Als Administrator" anlegen:

```powershell
netsh advfirewall firewall add rule name="TrafagSalesExporter local web 5000" dir=in action=allow protocol=TCP localport=5000 profile=domain,private
```

Am 2026-05-21 wurde eine allgemeine Port-5000-Regel angelegt und danach auf alle Firewall-Profile erweitert:

```text
Regelname: Local Dev Web Port 5000
Aktiviert: Ja
Profile: Domaene, Privat, Oeffentlich
Protokoll: TCP
Lokaler Port: 5000
Aktion: Zulassen
```

Damit koennen spaeter auch andere lokale Entwicklungsprogramme auf Port 5000 von anderen Firmen-PCs erreicht werden, sofern sie an `0.0.0.0:5000` oder die konkrete PC-IP binden.

Pruefen:

```powershell
netsh advfirewall firewall show rule name="TrafagSalesExporter local web 5000"
```

Spaeter wieder entfernen:

```powershell
netsh advfirewall firewall delete rule name="TrafagSalesExporter local web 5000"
```

Hinweise:

- Die Firewall-Regel bleibt nach einem Windows-Neustart aktiv.
- Die Firewall-Regel bleibt normalerweise auch nach Windows-Updates aktiv.
- Da die Regel auf Domaene, Privat und Oeffentlich gilt, ist Port 5000 auch abgedeckt, wenn AlwaysOnVPN oder Windows das Netzwerk nicht als Domaenenprofil erkennt.
- Die App selbst startet nach einem Neustart nicht automatisch; `dotnet run ...` muss erneut gestartet werden.
- Die PC-IP kann sich nach Neustart, WLAN-Wechsel oder DHCP-Erneuerung aendern; dann `ipconfig` ausfuehren und die neue URL weitergeben.
- Der PC muss eingeschaltet bleiben und das PowerShell-Fenster muss offen bleiben.
- Nur im Firmennetz verwenden, nicht oeffentlich freigeben.
- Die lokale Uebergangs-URL ist bewusst HTTP, nicht HTTPS. Fuer diesen temporaeren internen Betrieb reicht das; lokales HTTPS waere moeglich, wuerde aber Zertifikats-/Trust-Aufwand fuer andere PCs verursachen.
- Finance Cockpit und HR KPI bleiben ueber ihre App-internen Logins geschuetzt.
- Wenn ein Finance-User im Buero die App ueber die VPN-IP des Entwicklungs-PCs trotzdem nicht erreicht, liegt es wahrscheinlich am AlwaysOnVPN-/Firmennetz-Routing. Das kann lokal auf dem PC nicht sicher freigeschaltet werden.

Serverbefund:

- Der IIS-Server fordert beim HTTPS/TLS-Handshake ein Client-Zertifikat (`RequestedClientCert=True`).
- Dadurch erreichen Requests weder `diag.txt` noch `BiDashboard.dll`.
- Marco/IT muss in IIS die SSL Settings pruefen und Client Certificates auf `Ignore` oder hoechstens `Accept` setzen, nicht `Require`.

## Adminbereich und Passwortwechsel 2026-05-21

Geaendert:

- Finance Cockpit und HR KPI Login-Masken haben einen Bereich `Passwort ändern`.
- Passwortaenderung verlangt Benutzername, aktuelles Passwort, neues Passwort und Wiederholung.
- Neue Passwoerter muessen mindestens 8 Zeichen haben.
- Gespeichert wird ein SHA-256-Hash in `appsettings.json`, kein Klartext.
- Neuer interner Adminbereich `/admin/sessions`.
- Der Adminbereich hat eine eigene App-interne Sperre `AdminAccess`.
- Adminseite `Aktive Logins` zeigt App-interne HR-/Finance-Entsperrungen seit dem letzten App-Start:
  - Bereich
  - Login-Name
  - IP-Adresse, soweit aus dem Request verfuegbar
  - Entsperrt seit
  - Zuletzt gesehen
- Hinweis: Da HR und Finance gemeinsame App-Logins verwenden, zeigt die Seite nicht zwingend die echte Person, sondern die verwendete App-Session.
- Standorte-Tabelle zeigt jetzt Icons fuer den Quellentyp:
  - Upload-Datei = Manual Excel / CSV
  - Cloud Sync = SAP OData
  - Storage = HANA / Server

Initialer Adminzugang:

```text
Username: admin
Initialpasswort: TrafagAdmin2026!
```

Nach erster Nutzung sollte das Adminpasswort ueber die Admin-Loginmaske geaendert werden.

Verifiziert:

- `dotnet build .\TrafagSalesExporter.csproj --no-restore --verbosity minimal -p:OutDir=C:\TMP\trafag_out\`
- Ergebnis: Build erfolgreich, nur bestehende MudBlazor-Analyzer-Warnungen zu `Dense` auf vorhandenen Controls.

## Markdown-Doku und Anwenderdokus nachgezogen 2026-05-20

Geaendert:

- Neue zentrale Markdown-Uebersicht `docs/MD_DOKUMENTENSTATUS_2026-05-20.md` erstellt.
- Markdown-Dateien werden dort als aktuell fuehrend, Detaildoku oder historisch eingeordnet.
- Alte Markdown-Dateien wurden nicht geloescht, weil sie Pruefwerte, Zwischenentscheide und Audit-Spuren enthalten.
- HR- und Finance-Word-Anleitungen wurden visuell ueberarbeitet:
  - Titelbereich
  - Tabellen
  - Hinweisboxen
  - eingebettete neutrale Cockpit-Vorschaugrafiken
- Neue Bilddateien:
  - `docs/hr_kpi_cockpit_preview.png`
  - `docs/finance_cockpit_preview.png`

Commits:

- `0bff161 Document HR cockpit feature list`
- `a044040 Improve cockpit user guide documents`

## Management Analyse auf Finance Summary ausgerichtet 2026-05-20

Geaendert:

- `Management Analyse` hat jetzt einen fuehrenden Reiter `Finance Summary`.
- Die Kennzahlen in diesem Reiter verwenden dieselbe `FinanceRuleEngine` wie das zentrale Excel-Blatt `Finance Summary`.
- Filter fuer Jahr, Land und Waehrung wirken auf das Endergebnis, nicht nur auf eine Rohdatenansicht.
- Die bisherige Management-Tabelle bleibt als separater Rohdaten-/Diagnose-Reiter erhalten.
- Fuer DE 2026 wird kein Fehler mehr geworfen. Da DE/Alphaplan fachlich auf 2025 gezwungen ist, zeigt das Dashboard fuer DE 2026 einen leeren Zustand mit Hinweis.

Verifiziert:

- Lokale Probe gegen DB und Excel zeigte, dass die alte `Management Analyse` wegen Rohwerten, anderem Datum und EUR-Umrechnung nicht mit der Finance Summary uebereinstimmte.
- Tests: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `77/77` bestanden.

Commit:

- `610e771 Add finance summary view and HR guide`

## HR KPI Cockpit erweitert und Anwenderdokus erstellt 2026-05-20

Geaendert:

- `HR KPI Cockpit` hat einen neuen Reiter `Anleitung` fuer HR-Anwenderinnen.
- Der Datenordner fuer Rexx-/SAP-Dateien ist im Cockpit sichtbar und je Lauf anpassbar; dauerhaft ueber `HrKpi:DataFolder` in `appsettings.json`.
- Dateistatus zeigt jetzt letzte Aenderung, Dateialter und Frischebewertung.
- Neue Auswertungen: Ampeln, Periodenvergleich, Datenqualitaets-Hinweise, Austritte nach Typ/Organisation und Absenzen nach Organisation.
- Managementsicht anonymisiert personenbezogene Details und reduziert die Anzeige auf aggregierte Kennzahlen.
- Print-/PDF-Funktion im Cockpit ergaenzt.

Anwenderdokus:

- `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
- `docs/FINANCE_COCKPIT_ANLEITUNG_FINANZ_2026-05-20.docx`

Verifiziert:

- Word-Dateien als gueltige DOCX-Pakete geprueft.
- Tests: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `77/77` bestanden.

Commit:

- `06fb560 Expand HR KPI cockpit and add user guides`

## Workflow-Konsistenz fuer Keyuser verbessert 2026-05-20

Geaendert:

- Export Dashboard zeigt jetzt Warnungen, wenn aktive Manual-Excel-Standorte noch keine Datei/Pfad hinterlegt haben.
- Nach einem Einzelstandortexport wird darauf hingewiesen, dass die zentrale Excel separat neu erzeugt werden muss.
- Dashboard markiert, wenn seit der letzten zentralen Excel ein Standortexport gelaufen ist.
- Neuer Keyuser-Menuepunkt `Manuelle Importe` fuer DE/UK/ES-artige Excel-/CSV-Quellen:
  - Pfad/SharePoint-Referenz pflegen
  - Datei hochladen
  - Standort aktiv/inaktiv setzen
  - Pfad pruefen
- Live-Status startet nicht mehr pauschal mit `HANA Abfrage...`, sondern quellenneutral bzw. fuer Manual Excel/SAP passender.
- Zentrale Excel enthaelt ein neues Blatt `Finance Summary` mit Summen nach Jahr, Land und Waehrung.
- `Management Analyse` ist klarer als Rohdaten-/Plausibilitaetssicht markiert.
- `Soll/Ist Vergleich` ist klarer als verbindliche Finance-Sicht markiert.

Nachtrag:

- Unter `Manuelle Importe` gibt es jetzt einen zweiten Reiter `Anleitung`.
- Der Reiter zeigt den Keyuser-Ablauf grafisch:
  - Excel bereitstellen
  - speichern und aktivieren
  - Standort exportieren
  - zentrale Excel erzeugen
  - Finance pruefen
- Zusatzhinweise markieren die richtige Reihenfolge, den offenen DE-Fachentscheid und dass auf dem Server kein Microsoft Excel benoetigt wird.

Bewusst nicht geaendert:

- DE-Fachregel bleibt offen, bis Munir/Finance bestaetigt, welche Kundenlaender/Filter zum offiziellen DE-Ist gehoeren.

## Keyuser Prozessdoku SVG 2026-05-20

Erstellt:

- `docs/KEYUSER_PROZESSDOKU_2026-05-20.svg`

Inhalt:

- Prozess von Vorbereitung ueber Standortexport, zentrale Excel und Finance-Soll/Ist bis Fehlerbehandlung.
- Fokus auf Keyuser-Aktionen in der App: Settings, Standorte, Export Dashboard, Management Analyse, Soll/Ist Vergleich, Logs.
- Enthaltene Fachpunkte: Manual Excel fuer UK/ES/DE, DE Alphaplan, IT-Sonderregel, Finance-Spalten im Endexcel.
- Technische Implementierungsdetails und Testprogramme sind bewusst ausgeklammert.

## Technische Systemarchitektur SVG 2026-05-20

Erstellt:

- `docs/SYSTEMARCHITEKTUR_TECHNISCH_2026-05-20.svg`

Inhalt:

- Laufzeit und IIS-Publish als `BiDashboard.dll` ohne EXE/AppHost.
- Blazor-UI, Authentisierung, Start-/Background-Services.
- Applikationskern: Export-Orchestrierung, Standortexport, Adapter, Transformationen, zentrale Tabelle.
- Datenquellen: SAP HANA/BI1/SAGE, SAP Gateway/OData, Manual Excel/CSV, SharePoint.
- Persistenzmodell mit wichtigsten SQLite-Tabellen.
- Output-/SharePoint-Pfade, Finance-Sonderregeln, HR/Finance-Zugriff und Betriebspruefpunkte.
- Test-/Probeprogramme sind bewusst nicht enthalten.

## IT Finance-Methode fachlich bestaetigt 2026-05-20

Entscheid:

- Fuer Italien gilt die vom Finance-Leiter bestaetigte Methode.
- `CustomerName` enthaelt `Trafag Italia` wird aus dem IT-Finance-Ist ausgeschlossen.
- Doppelte IT-Zeilen mit leerem `Supplier country` werden nur einmal gezaehlt.
- Diese Regel gilt nur fuer IT.

Wichtig:

- Die bisherige Kundenausschluss-Kombination passte 2025 numerisch naeher an den Sollwert, ist aber nicht die belastbare Methode fuer Folgejahre.
- Der 2025-Zufallstreffer wird deshalb nicht als fachliche Regel weiterverwendet.

Gegen aktuelle DB getestet:

```text
Soll IT:                         7'669'840.00
Bisherige IT-Summe:              7'669'641.47
Bisherige Differenz:                  -198.53
Trafag Italia Abzug in DB:           6'495.71
Dubletten-Abzug SupplierCountry leer:    0.00
Neue fachliche Methode:          7'663'145.76
Neue Differenz:                    -6'694.24
```

Umsetzung:

- `Services/FinanceReconciliationService.cs`
- `Services/ExcelExportService.cs`
- Tests in `TrafagSalesExporter.Tests/FinanceReconciliationServiceTests.cs`

## IIS Deployment Handoff 2026-05-19

Aktueller Deployment-/IIS-Stand wurde hier dokumentiert:

```text
docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md
```

Kurzstand:

- `TrafagSalesExporter` veroeffentlicht jetzt als `BiDashboard.dll`.
- Keine EXE im Publish.
- Publish-Ziel: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Wahrscheinliche URL: `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/`.
- Diagnose-`web.config` ist aktiv mit `httpErrors Detailed` und `stdoutLogEnabled=true`.
- `logs`-Ordner existiert auf dem Share, blieb nach dem 500 aber leer.
- ACL-Befund: `IIS_IUSRS` hat nur `ReadAndExecute`; App braucht fuer SQLite/logs wahrscheinlich `Modify`.
- Rechte konnten lokal nicht gesetzt werden: `icacls` auf dem Share endete mit `Zugriff verweigert`.

Naechster Schritt:

- Server-Spezialist muss App-Pool-Identity bzw. `IIS_IUSRS` mit `Modify` auf Publish-Ordner, `logs` und `trafag_exporter.db*` berechtigen und danach App-Pool neu starten.

## ASP.NET Publish direkt aus TrafagSalesExporter 2026-05-19

Entscheid:

- `TrafagSalesExporter` bleibt das fuehrende Projekt.
- Das separate `BiDashboard`-Projekt wird fuer den aktuellen Stand nicht benoetigt.
- `TrafagSalesExporter` ist bereits eine ASP.NET/Blazor-Webanwendung (`Microsoft.NET.Sdk.Web`) und kann direkt veroeffentlicht werden.

Umsetzung:

- `OutputType=WinExe` wurde aus `TrafagSalesExporter.csproj` entfernt.
- Der `BiDashboard`-Verweis wurde aus `TrafagSalesExporter.sln` entfernt.
- Das Publish-Profil `Properties/PublishProfiles/FolderProfile.pubxml` zeigt auf den Server-Publish-Pfad:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$
```

Wichtig fuer Deployment:

- Die Anwendung wird nicht durch Doppelklick auf eine EXE gestartet.
- Der Server-Spezialist soll die publish-Ausgabe als ASP.NET-Webanwendung/IIS-App betreiben.
- Publish lokal:

```powershell
dotnet publish .\TrafagSalesExporter.csproj -c Release
```

## Finance Cockpit Login und Vergleichsnachtrag 2026-05-19

Nach dem Finance-Handoff vom 2026-05-18 wurden noch mehrere Schritte umgesetzt:

- Haupt-App-Seite `/finance-cockpit/vergleich` wurde an die Logik und Darstellung der FinanceProbe angeglichen.
- Leere Ist-Zeilen ohne belastbaren Ist-Wert werden im Finance-Vergleich ausgefiltert.
- Die verwendeten Berechnungsformeln je Land wurden dokumentiert:

```text
docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md
```

- Finance Cockpit erhielt einen separaten Login, unabhaengig vom HR-KPI-Login.

Technischer Stand Finance-Cockpit-Login:

- Konfiguration: `FinanceCockpitAccess` in `appsettings.json`
- Benutzer im aktuellen Stand: `finance`
- Passwort ist als SHA-256-Hash gespeichert.
- Finance nutzt ein eigenes Passwort: `Trafag-Finance-Cockpit-2026!`.
- HR-KPI nutzt weiterhin seine eigene `HrKpiAccess`-Konfiguration.
- Umsetzung:
  - `Services/FinanceCockpitAccessService.cs`
  - `Security/FinanceCockpitAccessOptions.cs`
  - `Components/FinanceCockpit/FinanceCockpitUnlockPanel.razor`
  - `Components/Routes.razor`
  - `Components/Layout/NavMenu.razor`
  - Registrierung in `Program.cs`

AD-/Rollenstand:

- `Security.Enabled = false` deaktiviert die globale AD-/Rollenpruefung fuer den Moment.
- Die vorhandenen `AccessGroups` und `AdminGroups` bleiben in `appsettings.json` stehen und wurden nicht geloescht.
- Wenn AD/Rollen wieder gelten sollen, `Security.Enabled` auf `true` setzen.
- Finance- und HR-KPI-Sperren bleiben auch bei deaktivierter AD-Pruefung aktiv.

Relevante Commits:

```text
8f1b1b8 Align main finance comparison with probe
f855e06 Filter empty actual finance rows
5c654ad Document finance formulas by country
9c544af Protect finance cockpit with login
```

## Zentrale Excel Finance-Filter 2026-05-19

Die zentrale Laenderdatei `Sales_All_yyyy-MM-dd.xlsx` wurde fuer den CFO-/Finance-Abgleich erweitert.

Im Blatt `Sales` gibt es rechts einen zusammengehoerigen Finance-Spaltenblock:

```text
Finance | Year
Finance | Country Key
Finance | Date
Finance | Net Sales Actual
Finance | Currency
Finance | Include
Finance | Source Value Field
```

Ziel:

- Finance kann im zentralen Excel dieselben Ist-Summen erzeugen wie im Testprogramm.
- Es muss nicht geraten werden, ob `Land`, `TSC`, `Sales Price/Value`, `Document Total LC`, `posting date` oder `invoice date` zu verwenden ist.

Filterregel fuer Finance:

```text
Finance | Year = 2025
Finance | Country Key = gewuenschtes Land
Finance | Include = TRUE
Summe ueber Finance | Net Sales Actual
```

Nur in der zentralen Datei wird ein zweites Blatt erzeugt:

```text
Finance Filter Hilfe
```

Dieses Hilfsblatt beschreibt die zusammengehoerigen Finance-Spalten und die konkrete Filter-/Summenlogik.

Verifikation:

- Build erfolgreich:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_finance_help_sheet\ --verbosity minimal
```

- Preview-Excel erzeugt und geprueft:

```text
.tmp_tools\GenerateConsolidatedPreview\out\Sales_All_2026-05-19.xlsx
```

- Gepruefte Blaetter:

```text
Sales | Finance Filter Hilfe
```

- Finance-Spaltenblock im Blatt `Sales`:

```text
36: Finance | Year
37: Finance | Country Key
38: Finance | Date
39: Finance | Net Sales Actual
40: Finance | Currency
41: Finance | Include
42: Finance | Source Value Field
```

- Summenvergleich gegen `FinanceReconciliationService` fuer 2025:

| Key | Finance-Service | Excel-Finance-Spalten | Status |
| --- | ---: | ---: | --- |
| AT | `3'438'121.37` | `3'438'121.37` | MATCH |
| CH | `43'521'390.82` | `43'521'390.82` | MATCH |
| ES | `3'082'320.18` | `3'082'320.18` | MATCH |
| FR | `1'471'218.44` | `1'471'218.44` | MATCH |
| IN | `750'936'591.38` | `750'936'591.38` | MATCH |
| IT | `7'669'641.47` | `7'669'641.47` | MATCH |
| UK | `3'533'710.09` | `3'533'710.09` | MATCH |
| US | `3'749'865.33` | `3'749'865.33` | MATCH |

Relevante Commits:

```text
ebbc5a1 Add finance filter columns to consolidated export
b23f73e Add finance filter help sheet
```

## UK_B1 Mapping / FinanceProbe Nachtrag 2026-05-11

Anlass:

- In der FinanceProbe zeigte UK/England fuer `TRUK` nur `395'605.82 GBP` Ist gegen `3'749'865.00 GBP` Soll.
- In den Varianten fehlten weitere sinnvolle Abgrenzungen; sichtbar war nur `Positions-Netto (Sales Price/Value)`.
- Der Standort soll weiterhin `UK_B1` verwenden.

Technischer Befund:

- Standort:
  - `Land = England`
  - `TSC = TRUK`
  - `SourceSystem = MANUAL_EXCEL`
- Korrekte Quelle:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/UK_B1
```

- Lokal waren fuer `TRUK` keine `ManualExcelColumnMappings` vorhanden.
- Der Import lief deshalb ueber die Header-Automatik.
- Die Header-Automatik behandelte `Sales Price/Value` als fertigen Positionswert.
- In der UK-B1-Datei ist `Sales Price/Value` nach aktuellem Befund aber ein Stueckpreis.
- Der Finance-Positionswert muss deshalb berechnet werden:

```text
[Sales Price/Value] * [Quantity]
```

Probe auf den bereits geladenen UK-Daten:

| Berechnung | Wert |
| --- | ---: |
| Bisher importiert: Summe `SalesPriceValue` | `395'605.82 GBP` |
| Rekonstruiert: Summe `SalesPriceValue * Quantity` | `3'533'348.89 GBP` |
| Soll `check.xlsx` | `3'749'865.00 GBP` |
| Restdifferenz nach Multiplikation | ca. `216'516.11 GBP` |

Umgesetzte Codeaenderung:

- `Services/ManualExcelImportService.cs`
  - grafische Manual-Excel-Mappings koennen jetzt einfache berechnete Quellen auswerten
  - aktuell benoetigte Syntax:

```text
=[Header A]*[Header B]
```

  - Konstanten wie `=GBP` bleiben unveraendert gueltig

- `Services/DatabaseSeedService.cs`
  - England/TRUK wird auf den SharePoint-Ordner `Import/Finance/UK_B1` repariert, wenn der alte/falsche Pfad `Import/Finance/England` oder ein leerer Pfad vorhanden ist
  - fuer `TRUK` wird ein grafisches Manual-Excel-Mapping geseedet
  - wichtigste Zuordnung:

```text
SalesPriceValue <- =[Sales Price/Value]*[Quantity]
SalesCurrency   <- =GBP
DocumentCurrency<- =GBP
CompanyCurrency <- =GBP
PostingDate     <- invoice date
InvoiceDate     <- invoice date
```

- `TrafagSalesExporter.Tests/ManualExcelImportServiceTests.cs`
  - neuer Test fuer Multiplikationsausdruck im Manual-Excel-Mapping
  - prueft, dass `123.45 * 7 = 864.15` als `SalesPriceValue` importiert wird

Aktueller Verifikationsstand:

```text
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore -p:UseAppHost=false --verbosity minimal
```

Ergebnis:

- Tests erfolgreich.
- `59/59` Tests gruen.
- Bekannte Warnungen bleiben die bestehenden MudBlazor-Analyzerwarnungen zu `Dense`.

Zusatzfix:

- `DatabaseSeedService` wurde gehaertet.
- Der UK-Mapping-Seed wird nur ausgefuehrt, wenn `ManualExcelColumnMappings` sauber auf `Sites` referenziert.
- Dadurch wird der Initialisierungslauf nicht blockiert, wenn eine bestehende SQLite-DB gerade noch aus alten Reparaturtabellen wie `Sites_repair_old` bereinigt wird.

Naechster praktischer Schritt:

- Lokale DB wurde direkt aktualisiert:
  - `TRUK` zeigt auf `https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/UK_B1`
  - `TRUK` hat `18` aktive Manual-Excel-Mapping-Zeilen
  - `SalesPriceValue <= =[Sales Price/Value]*[Quantity]`
- FinanceProbe wurde auf `http://127.0.0.1:5099` neu gestartet.
- `/finance` antwortet mit HTTP `200`.
- `/run/export/TRUK` wurde angestossen, konnte aber wegen lokaler SharePoint-/Graph-Authentifizierung nicht neu laden:

```text
ClientSecretCredential authentication failed
Es konnte keine Verbindung hergestellt werden, da der Zielcomputer die Verbindung verweigerte. (127.0.0.1:9)
```

Damit gilt:

- Code, Seed und lokale Mapping-Konfiguration sind vorbereitet.
- Die zentrale Tabelle `CentralSalesRecords` enthaelt fuer UK noch den alten Importstand, bis der SharePoint-Zugriff wieder funktioniert und `TRUK` neu exportiert wird.
- Aktueller alter Zentralstand bleibt deshalb:
  - `1'882` Zeilen
  - `395'605.82 GBP` Summe `SalesPriceValue`
  - rekonstruiert `3'533'348.89 GBP` ueber `SalesPriceValue * Quantity`

Offen fachlich fuer UK:

- Nach neuem Export mit Mapping muss die Restdifferenz gegen `check.xlsx` erneut gemessen werden.
- Wenn der Wert bei ca. `3.53 Mio. GBP` liegt, UK-Datei auf Rabatte, Fracht, Nebenpositionen oder eine andere Netto-Spalte pruefen.
- Wenn der Wert auf `3.75 Mio. GBP` steigt, war das Mapping die Hauptursache.

## Manual Excel/CSV SharePoint-Ordner und Quellordner-Export 2026-05-08

Umgesetzte Anpassungen:

- Manual Excel/CSV Quellen erzeugen nun immer eine neue Exportdatei; die Quelldatei wird nicht als Exportdatei weitergereicht.
- Lokale Manual-Dateien schreiben die neue Exportdatei in denselben lokalen Ordner wie die Quelldatei.
- SharePoint-Manual-Dateien schreiben die neue Exportdatei in denselben SharePoint-Ordner wie die Quelldatei.
- SharePoint-Referenzen ohne Dateiendung werden als Ordner behandelt.
- Bei SharePoint-Ordnern sucht die App die neueste passende Excel-/CSV-Datei fuer den Standort.
- Fuer datierte Dateien wird das Muster `ddMMyy_TSC.xlsx` bzw. `ddMMyy_TSC.csv` ausgewertet.
- Beispiel England/UK:
  - Ordner: `https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/UK_B1`
  - `010526_TRUK.xlsx` wird vor `010426_TRUK.xlsx` gewaehlt.
  - Falls kein Datum aus dem Dateinamen gelesen werden kann, faellt die Auswahl auf das SharePoint-Aenderungsdatum zurueck.

Technischer Befund aus den Logs:

- Spanien konnte die SharePoint-Datei lesen (`4'341` Zeilen), fiel danach aber auf einen ungueltigen lokalen Pfad, weil die URL als lokale Exportdatei behandelt wurde.
- Fehlerpfad war sinngemaess `...\https:\trafagag.sharepoint.com\...\Spain_Sales_2025.csv`.
- Deutschland hatte keinen manuellen Dateipfad hinterlegt.
- England/TRUK zeigte lokal versehentlich auf die Deutschland-Alphaplan-Datei; die lokale DB wurde auf den UK_B1-Ordner korrigiert.

Codeaenderungen:

- `DataSourceFetchResult` enthaelt optionale Overrides fuer lokalen Output-Ordner und SharePoint-Zielordner.
- `ManualExcelDataSourceAdapter` erkennt SharePoint-Dateien vs. SharePoint-Ordner und waehlt bei Ordnern die neueste passende Datei.
- `SharePointUploadService` kann den neuesten passenden Datei-Eintrag in einem SharePoint-Ordner aufloesen.
- `SiteExportService` nutzt fuer Manual-Quellen den Quellordner als Zielordner.
- `StandortePageService` erlaubt fuer Manual-Importe nun auch SharePoint-Ordnerreferenzen.
- Standort-UI-Hilfetext wurde entsprechend angepasst.
- `DatabaseSeedService` repariert England/TRUK auf den UK_B1-Ordner, wenn der Manual-Pfad leer ist.

Letzte technische Verifikation:

```text
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore --verbosity minimal
```

Ergebnis:

- Tests erfolgreich, `55/55`
- Bekannte MudBlazor-Analyzerwarnungen zu `Dense` bleiben bestehen.

## FinanceProbe erweitert fuer alle Finance-Referenzen 2026-05-08

Umgesetzte Anpassungen:

- FinanceProbe zeigt nun alle aktiven `FinanceReferences` fuer 2025, auch wenn noch kein aktiver/importierter Standort dazu Daten liefert.
- Damit werden auch Laender wie AT, CH, CN, CZ, GFS, JP, MS, MSA, PL und RU sichtbar als `Keine Daten`, bis Ist-Daten vorhanden sind.
- Zusaetzliche Sektion `Datenabdeckung je Standort`:
  - Standort / TSC
  - Quellsystem und Anschlussart
  - Manual-Datei- oder SharePoint-Pfad
  - Aktivstatus
  - Anzahl 2025-Zeilen in `CentralSalesRecords`
  - Summe `SalesPriceValue`
  - Waehrungen
  - importierte Periode
  - letzter Exportstatus und Hinweis
- Referenzschluessel-Erkennung wurde fuer CH/AT praezisiert:
  - `AT`, `AUT`, `Oesterreich`/`Austria` -> `AT`
  - `CH`, `CHE`, `Schweiz`/`Switzerland` -> `CH`
- Damit koennen Zeilen aus `ZSCHWEIZ` mit `LAND1 = AT` fachlich Oesterreich zugeordnet werden.

Verifikation:

- `Tools/FinanceProbe` Build erfolgreich.
- Haupttests wurden mit separatem Output/Obj-Pfad ausgefuehrt, damit die laufende App nicht stoert.

## FinanceProbe als KI-Steuerprogramm 2026-05-11

Die FinanceProbe ist bewusst als temporaeres Test-/KI-Steuerprogramm erweitert worden. Die produktive Blazor-App bleibt davon getrennt.

Neue Routen:

- `/run/export/{siteKey}`
  - startet einen Standortexport nach `Id`, `TSC` oder `Land`
  - Beispiele: `/run/export/TRUK`, `/run/export/Spanien`, `/run/export/7`
- `/run/export-all`
  - startet Export aller aktiven Standorte
  - erzeugt danach die zentrale Datei
- `/run/consolidated`
  - erzeugt nur die zentrale Datei aus `CentralSalesRecords`

Nach jedem Lauf zeigt die FinanceProbe eine Run Summary:

- neue Exportlogs seit Start
- Finance-Abgleich gegen `check.xlsx`
- Datenabdeckung je Standort

Zweck:

- Exporte und Finance-Abgleich koennen fuer Tests von der KI per HTTP angestossen werden.
- Die Funktion ist nicht als produktive Bedienoberflaeche gedacht und kann spaeter wieder entfernt werden.

## Mapper-/Finance-Konfiguration konsolidiert 2026-05-07

Umgesetzte Aufraeumarbeiten:

- Die doppelte SAP-OData/HANA-Mapping-Engine wurde entfernt.
- Neuer gemeinsamer Service: `MappedSalesRecordComposer`.
- `SapCompositionService` und `HanaQueryService.GetMappedSalesRecordsAsync` laden ihre Quellen weiterhin separat, nutzen danach aber denselben Composer fuer:
  - Primaerquelle
  - Left Joins
  - `SapFieldMapping` nach `SalesRecord`
  - Konstanten wie `=SAP` / `=HANA`
  - Datums-/Zahlenkonvertierung
- Der alte HANA-B1-Pfad fuer `OINV/INV1/ORIN/RIN1` bleibt bewusst bestehen, damit BI1/SAGE ohne grafisches Mapping weiter laufen.
- Die SAP-Mapping-Normalisierung liegt nur noch in `StandorteSapEditorService`; `StandortePageService` ruft diesen Service beim Speichern auf.
- Der tote Parameter im konsolidierten Export wurde entfernt. `ConsolidatedExportService.ExportAsync()` liest eindeutig aus `CentralSalesRecords`.
- Manueller Import erlaubt in UI und Service jetzt `.xlsx` und `.csv`.

Finance-Konfiguration:

- Neue Tabelle `FinanceReferences` fuer Soll-/check.xlsx-Referenzen je Jahr.
- Neue Tabelle `FinanceIntercompanyRules` fuer 2nd-party/IC-Erkennung nach `ScopeKey`, Kundennummer oder Namensmarker.
- Budgetkurse 2025 werden in `CurrencyExchangeRates` mit `Notes = Budget 2025` geseedet.
- `FinanceReconciliationService` liest Sollwerte, Budgetkurse und IC-Regeln aus der DB.
- Config-Export/-Import enthaelt jetzt `FinanceReferences` und `FinanceIntercompanyRules`.

Noch bewusst offen:

- HANA-B1-Spezialpfad und generischer HANA-Mapper laufen parallel. Das ist aktuell noetig fuer bestehende BI1/SAGE-Standorte ohne Mapping.
- Manual Excel hat weiterhin Header-Automatik und grafisches Mapping. Naechster Aufraeumpunkt waere eine gemeinsame Import-Mapping-Engine.

Letzte technische Verifikation:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore --verbosity minimal
```

Ergebnis:

- Build erfolgreich
- Tests erfolgreich, `52/52`
- Bekannte MudBlazor-Analyzerwarnungen zu `Dense` bleiben bestehen.

## SAP OData / ZSCHWEIZ / HANA Mapping 2026-05-07

Aktueller Entscheid:

- `ZSCHWEIZ` wird nicht direkt als SAP-HANA-Spezialfall gelesen.
- `ZSCHWEIZ` wird ueber den bestehenden SAP-OData/Gateway-Pfad gelesen.
- Der grafische Quellen- und Feldmapper bleibt dafuer aktiv.
- Feldinfos muessen nicht hart codiert werden, solange der Gateway-Service `$metadata` fuer das EntitySet liefert.

Quellsystem-Namen wurden zur Entwirrung geschaerft:

- Code `SAP` bleibt technisch bestehen, DisplayName ist jetzt `SAP OData`.
- Code `SAP_HANA` bleibt fuer direkte HANA-Tabellen/Views bestehen, DisplayName ist jetzt `SAP HANA Tables/Views`.
- Bestehende Konfigurationen bleiben dadurch kompatibel.

Seed / Vorkonfiguration:

- Standort `ZSCHWEIZ` / Land `Schweiz/Oesterreich` wird als inaktiver Standort angelegt bzw. repariert.
- `SourceSystem = SAP`.
- Quelle: Alias `Z`, EntitySet `ZSCHWEIZSet`.
- Mapping ist grafisch editierbar und wird auf die Felder der Tabelle `ZSCHWEIZ` gesetzt.
- Die Seed-/Repair-Logik zieht Quelle und Mapping auch bei bereits vorhandener ZSCHWEIZ-Konfiguration nach; manuelles Mapping ist nur noetig, wenn die Gateway-Feldnamen vom erwarteten `ZSCHWEIZ`-Layout abweichen.

Wichtig fuer die UI:

1. App neu starten, damit Seed/Repair laeuft.
2. `Settings -> Quellsysteme`: `SAP` sollte als `SAP OData` erscheinen.
3. `Standorte -> ZSCHWEIZ`:
   - Quellsystem `SAP OData (SAP)`
   - SAP Service URL Override auf den finalen OData-Service fuer `ZSCHWEIZ` setzen, falls die zentrale SAP-URL noch auf `ZPOWERBI_EINKAUF_SRV` zeigt.
   - `Entity Sets refreshen`.
   - Quelle `Z` soll auf `ZSCHWEIZSet` zeigen.
   - `Felder aus Quellen laden`.
   - Mapping kontrollieren.

ABAP / SAP:

- ABAP-Report liegt in `report.abap`.
- Report fuellt Tabelle `ZSCHWEIZ` aus Buchungskreis `1100` = Schweiz und `1200` = Oesterreich.
- `LAND1` ist Reporting-Land aus Buchungskreis.
- `CUSTOMER_LAND` ist Kundenland aus `KNA1-LAND1`.
- Upsert erfolgt per `MODIFY zschweiz FROM TABLE`.

Letzte technische Verifikation:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore --verbosity minimal
```

Ergebnis:

- Build erfolgreich
- Tests erfolgreich, `50/50`

## Finance-Abgrenzung: Antworten Andreas 2026-05-07

Fachliche Vorgabe nach Rueckmeldung:

- Net Sales Actuals werden in Hauswaehrung gerechnet.
- Massgebend ist der Nettofakturawert.
- Umrechnung nach CHF erfolgt mit Budgetkursen, nicht mit Tageskursen.
- Umrechnung/Summierung soll pro Artikel bzw. Belegposition erfolgen.
- Indien wird in INR betrachtet.
- Italien wird in Hauswaehrung betrachtet; Intercompany-/2nd-party-Abgrenzung wird separat angeschaut.
- UK wird in GBP betrachtet.
- Gutschriften haben eigene Rechnungsnummern/Rechnungspositionen und sollen ueber Artikelnummern/Positionen behandelt werden.
- Intercompany soll im zweiten Schritt als 2nd-party/3rd-party-Klassifikation pflegbar werden.
- Genannte 2nd-party/Intercompany-Indikatoren: Trafag, Magnetic Sense/Magnets Sense, Gesellschaft fuer Sensorik; Nummern/Uebersetzungen koennen je Land abweichen.

Budgetkurse 2025 fuer CHF-Ausweis:

```text
USD/CHF = 0.85
EUR/CHF = 0.95
GBP/CHF = 1.13
CHF/INR = 90.91
CHF/CZK = 25.64
PLN/CHF = 0.22
CHF/JPY = 156.25
```

Umsetzung in der FinanceProbe:

- Auswahl der Ist-Variante bevorzugt nun `Nettofakturawert Hauswaehrung` (`DocTotal - VatSum`).
- `Sales Price/Value` bleibt als Vergleichsvariante sichtbar.
- Zusaetzlicher Kandidat `Nettofakturawert Hauswaehrung -> CHF Budget 2025`.
- Referenz in der Oberflaeche wird als `check.xlsx Sollwert` bezeichnet, nicht mehr als fuehrende Power-BI-Referenz.
- Intercompany-Anzeige wurde fachlich als `2nd-party/IC` beschriftet; Regeln werden jetzt in `FinanceIntercompanyRules` geseedet und per Config exportiert/importiert.

## Finance Probe / Sales-Abgrenzung

Ziel der heutigen Arbeit:

- separate kleine Pruef-GUI fuer Finanz-/Sales-Abgrenzungen bauen
- moeglichst viel Logik aus dem Hauptprogramm wiederverwenden
- verschiedene Summenlogiken pro Land nebeneinander sichtbar machen
- gegen `check.xlsx` vergleichen

Wichtiges fachliches Verstaendnis nach Klaerung im Chat:

- `check.xlsx` kommt von Rhino und enthaelt die Soll-Zahlen von Andreas.
- Aus den Landessystemen kommt der Ist-Wert.
- Power BI soll in der fachlichen Kommunikation nicht als fuehrende Referenz genannt werden.
- Ziel ist nicht, zufaellig die passendste technische Variante zu nehmen, sondern je Land/System die fachlich korrekte Abgrenzungslogik zu klaeren.

## Commit

Rollback-Commit fuer die Finance-Probe wurde erstellt:

```text
15dec06 Add finance reconciliation probe
```

Dieser Commit enthaelt gezielt:

- `Services/FinanceReconciliationService.cs`
- `Tools/FinanceProbe/FinanceProbe.csproj`
- `Tools/FinanceProbe/Program.cs`
- DI-Registrierung in `Program.cs`
- Dashboard nutzt den ausgelagerten Finance-Service
- `TrafagSalesExporter.csproj` schliesst `Tools/**` aus dem Hauptprojekt aus
- `TrafagSalesExporter.sln` enthaelt das neue Tool-Projekt

Andere bereits vorhandene Worktree-Aenderungen wurden nicht mitcommitted.

## Neues Tool

Neues separates Probe-GUI:

```text
Tools/FinanceProbe
```

Start:

```powershell
dotnet run --project Tools\FinanceProbe\FinanceProbe.csproj --urls http://localhost:55417
```

URL:

```text
http://localhost:55417/finance
```

Aktueller Start im Chat:

- Probe-GUI wurde auf `localhost:55417` gestartet
- HTTP `200` bestaetigt

Hinweis Netzwerk:

- Start mit `localhost` ist nur lokal auf dem Laptop erreichbar.
- Andere im Trafag-Netz koennen es so normalerweise nicht ueber Laptop-IP oeffnen.
- Fuer Netzwerkzugriff waere `http://0.0.0.0:55417` noetig.
- Probe-GUI hat aktuell keine Authentifizierung, daher nicht unkontrolliert im Netzwerk freigeben.

## FinanceReconciliationService

Neue wiederverwendbare Logik:

```text
Services/FinanceReconciliationService.cs
```

Interface:

```csharp
IFinanceReconciliationService
```

Aktuelle Funktion:

```csharp
Task<List<NetSalesReferenceRow>> BuildNetSalesReferenceRowsAsync(int year = 2025)
```

Logik:

- liest `CentralSalesRecords`
- filtert Jahr ueber `InvoiceDate`, fallback `ExtractionDate`
- gruppiert pro Referenz-Key/Land
- berechnet Kandidaten:
  - `SalesPriceValue`
  - `DocTotalFC - VatSumFC`
  - `DocTotal - VatSum`
- Belegkopfwerte werden vor Summierung dedupliziert:
  - bevorzugt `TSC + DocumentType + DocumentEntry`
  - fallback `TSC + DocumentType + InvoiceNumber`
- erkennt aktuell Intercompany nur pragmatisch fuer IT/TRIT anhand bekannter Kunden
- liefert pro Kandidat Wert, Waehrung, IC-Wert, Differenzen

## FinanceProbe Darstellung

Die Tabelle zeigt aktuell:

- Status
- Firma
- gewaehlte Abgrenzung
- Ist-Waehrung
- Ist 2025
- Referenz-Waehrung
- Referenz
- Excel LC
- Excel CHF
- Excel Power BI
- Excel Status
- Differenz
- Differenz ohne IC
- Waehrung
- Zeilen
- Varianten aufklappbar

Wichtig:

- Die Bezeichnung `Power BI` ist in der Probe-Oberflaeche noch sichtbar, weil `check.xlsx` diese Spalte enthaelt.
- Fachlich soll in Kommunikation gegen Andreas aber `check.xlsx` / Soll-Zahl genannt werden, nicht Power BI als fuehrende Referenz.
- Eine sinnvolle naechste UI-Bereinigung waere, die Spalte/Labels in der Probe auf `Excel Sollwert` oder `Rhino Sollwert` umzubenennen.

## Probe-Output vom 2026-05-04 09:55

Zusammenfassung:

```text
8 Standorte
4 OK
1 Pruefen
3 Keine Daten
Excel-Referenzen gelesen: 17
```

Befunde:

### CH

- Keine Ist-Daten
- keine sichtbare Soll-Zahl

### DE

- Keine Ist-Zeilen aus Systemdaten
- Soll/LC aus Excel vorhanden:
  - Referenz ca. `3'635'923`
  - Excel LC `3'635'922.91`
  - Excel CHF `3'407'000.00`

Offen:

- Quelle fuer DE klaeren
- evtl. MANUAL_EXCEL oder noch nicht exportiert

### ES

- Keine Ist-Zeilen aus Systemdaten
- Soll/LC aus Excel vorhanden:
  - Referenz ca. `3'102'334`
  - Excel LC `3'102'333.61`
  - Excel CHF `2'907'000.00`

Offen:

- Quelle fuer ES klaeren
- evtl. MANUAL_EXCEL oder noch nicht exportiert

### FR

- Status OK
- gewaehlte Abgrenzung: `Sales Price/Value`
- Ist-Waehrung: `EUR`
- Ist: `1'471'218.44`
- Soll/Referenz: `1'471'218.00`
- Differenz: `0.44`
- Zeilen: `1649`

Befund:

- FR passt praktisch exakt mit `Sales Price/Value` in EUR.

Offene Frage an Andreas:

- Ist `Sales Price/Value` in EUR fuer FR fachlich korrekt?

### IN

- Status OK
- gewaehlte Abgrenzung: `Sales Price/Value`
- Ist-Waehrungen: `CHF, EUR, GBP, INR, JPY, USD`
- Ist: `750'936'591.38`
- Soll/Referenz: `750'936'591.00`
- Differenz: `0.38`
- Zeilen: `4000`

Befund:

- IN passt rechnerisch fast exakt, aber Waehrungen sind gemischt.

Offene Frage an Andreas:

- Ist diese gemischte Summe fachlich korrekt?
- Oder muss nach CHF umgerechnet bzw. nach Waehrung getrennt werden?

### IT

- Status Pruefen
- gewaehlte Abgrenzung: `DocTotal - VatSum`
- Ist-Waehrung: `EUR`
- Ist: `11'866'896.53`
- Soll/Referenz LC: `7'669'840.00`
- Differenz: `4'197'056.53`
- Differenz ohne IC: `3'733.67`
- Zeilen: `15883`

Befund:

- IT liegt ohne IC-Abzug stark daneben.
- Mit erkanntem IC-Abzug ist die Differenz sehr klein.

Offene Frage an Andreas:

- Soll IT mit Intercompany-Abzug gerechnet werden?
- Falls ja: nach welchen Kunden/Kriterien erkennt Finance Intercompany?

### UK

- Status OK
- gewaehlte Abgrenzung: `Sales Price/Value`
- Ist-Waehrung: `USD`
- Ist: `3'749'865.33`
- Soll/Referenz: `3'749'865.00`
- Differenz: `0.33`
- Zeilen: `942`

Befund:

- UK passt praktisch exakt mit `Sales Price/Value` in USD.

Offene Frage an Andreas:

- Ist USD fuer UK korrekt?
- Oder muss fuer offizielles Reporting nach CHF umgerechnet werden?

### US

- Status OK
- gewaehlte Abgrenzung: `Sales Price/Value`
- Ist-Waehrung: `USD`
- Ist: `3'749'865.33`
- Soll/Referenz: `3'749'865.00`
- Differenz: `0.33`
- Zeilen: `942`

Befund:

- US zeigt denselben Ist-Wert wie UK.
- Das wirkt auffaellig und sollte fachlich/technisch geprueft werden.

Offene Frage:

- Welche Quelle und Logik ist fuer US korrekt?
- Ist US im aktuellen System richtig zugeordnet?

## Word-Datei fuer Andreas

Erstellt:

```text
FINANZ_OFFENE_FRAGEN_ANDREAS.docx
```

Inhalt:

- kurze Mail an Andreas
- `check.xlsx` als Soll-Zahl von Andreas/Rhino formuliert
- Power BI fachlich nicht als Referenz genannt
- bisherige Befunde pro Land:
  - FR
  - IN
  - IT
  - UK
  - US
  - DE / ES
- offene Fragen zu:
  - Waehrung und CHF-Umrechnung
  - Umsatzdefinition
  - Periodenabgrenzung
  - Gutschriften/Storno
  - Intercompany
  - Entscheid-Tabelle pro Land

## Markdown-Datei fuer Andreas

Erstellt/angepasst:

```text
FINANZ_FRAGEN_ANDREAS.md
```

Aktuelle Formulierung:

- `check.xlsx` kommt von Rhino und enthaelt Soll-Zahlen von Andreas.
- Landessysteme liefern Ist-Werte.
- offen ist, welche fachliche Logik pro Land/System zur Soll-Zahl fuehren soll.
- Power BI ist nicht mehr als fuehrende Referenz formuliert.

## Verifikation

Ausgefuehrt:

```powershell
dotnet build .\TrafagSalesExporter.csproj --verbosity minimal
dotnet build .\Tools\FinanceProbe\FinanceProbe.csproj --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal
```

Ergebnis:

- Hauptprojekt baut erfolgreich
- FinanceProbe baut erfolgreich
- Tests erfolgreich
- `48/48` Tests gruen

Bekannte Warnungen:

- `NU1900` im Probe-Build, weil NuGet-Sicherheitsdaten wegen Netzwerk/nuget.org nicht geladen werden konnten
- bekannte MudBlazor Analyzer-Warnungen zu `Dense`

## Offene sinnvolle naechste Schritte

1. In der Probe-UI `Power BI`-Labels fachlich bereinigen:
   - z. B. `Excel Sollwert` / `Rhino Sollwert`
2. Andreas' Antworten in eine Konfiguration ueberfuehren:
   - Land/System
   - Summenlogik
   - System-Waehrung
   - CHF-Umrechnung ja/nein
   - Periodendatum
   - IC-Regel
3. DE/ES Quelle klaeren:
   - aktuell keine Ist-Daten
4. US/UK Doppelwert pruefen:
   - US zeigt denselben Ist-Wert wie UK
5. IT Intercompany-Regel fachlich bestaetigen
6. Wenn Regeln bestaetigt sind:
   - Finance-Probe erweitert anzeigen
   - spaeter produktiv ins Hauptprogramm uebernehmen

---

## Nachtrag 2026-05-04: Excel-Spaltenmapper fuer manuelle Land-Excel-Dateien

Ausloeser:

- Deutschland hat ein eigenes Excel-Beispiel geliefert.
- Das Format entspricht nicht dem bisherigen Standard-Excel-Import.
- Ziel war, nicht fuer jedes Land statischen Spezialcode zu schreiben, sondern die Spaltenzuordnung konfigurierbar zu machen.

Beispielhafte deutsche Spalten:

- `Export-Datum`
- `Firma`
- `Belegnummer`
- `Position`
- `ArtikelBezeichnung`
- `Warengruppen-Bezeichnung`
- `Anz. VE`
- `Lieferanten Nummer`
- `Name Lieferant`
- `Land Lieferant`
- `AdressNummer-Kunde`
- `Name Kunde`
- `Land Kunde`
- `Branche`
- `EinstandsPreis`
- `Währung`
- `BestellNummer`
- `NettoPreisEinzelX`
- `NettoPreisGesamtX`
- `Versandbedingung`
- `AdressNummer_V`
- `Belegdatum-Rechnung`
- `BelegDatum Auftrag`
- `ArtikelNummer`

Wichtige fachliche/technische Interpretation fuer Deutschland:

- `NettoPreisGesamtX` wird als `SalesPriceValue` verwendet.
- `Währung` wird fuer `SalesCurrency`, `DocumentCurrency`, `CompanyCurrency` und `StandardCostCurrency` verwendet.
- `Belegdatum-Rechnung` wird als `InvoiceDate` verwendet.
- `BelegDatum Auftrag` wird als `OrderDate` verwendet.
- `ArtikelNummer` wird als `Material` verwendet.
- Kommentar-/Info-Zeilen ohne echte Position und ohne Betrag werden beim Import ignoriert.

## Neue Datenstruktur

Neue Tabelle / neues Model:

```text
ManualExcelColumnMappings
Models/ManualExcelColumnMapping.cs
```

Felder:

- `SiteId`
- `TargetField`
- `SourceHeader`
- `IsRequired`
- `IsActive`
- `SortOrder`

Zweck:

- Pro Standort kann festgelegt werden, welche Excel-Spalte auf welches internes `SalesRecord`-Feld gemappt wird.
- Konstanten sind moeglich, wenn `SourceHeader` mit `=` beginnt, z. B. `=Manual Excel`.

## Geaenderte Hauptlogik

Geaendert:

```text
Services/ManualExcelImportService.cs
```

Neue Logik:

- Beim manuellen Excel-Import werden zuerst aktive `ManualExcelColumnMappings` des Standorts geladen.
- Wenn Mapping-Zeilen vorhanden sind, wird dieses Mapping verwendet.
- Wenn kein Mapping vorhanden ist, laeuft weiterhin die bisherige statische Standarderkennung.
- Damit bleiben bestehende manuelle Excel-Imports abwaertskompatibel.

Wichtig:

- Der Mapper ersetzt nicht die fachliche Finanzlogik.
- Er sorgt nur dafuer, dass fremde Excel-Spalten korrekt in die internen Felder geschrieben werden.
- Welche Summe spaeter fuer Finance gilt, muss weiterhin fachlich entschieden werden.

## Geaenderte Standort-UI

Geaendert:

```text
Components/Pages/Standorte.razor
Services/StandortePageService.cs
```

In der Standortbearbeitung fuer manuelle Excel-Standorte gibt es neu:

- Bereich `Excel-Spaltenmapping`
- Button `Spalten aus Excel laden`
- Button `Auto-Match`
- Button `Mapping hinzufuegen`
- Tabelle mit:
  - Zielfeld
  - Excel-Spalte / Konstante
  - Pflicht
  - Aktiv
  - Loeschen

Auto-Match erkennt aktuell u. a. die deutschen Spalten und schlaegt passende Zuordnungen vor.

## Config-Export / Import

Geaendert:

```text
Services/ConfigTransferService.cs
Models/ConfigTransferPackage.cs
```

Neu:

- `ManualExcelColumnMappings` werden im Konfigurationspaket mit exportiert.
- Beim Import werden die Mapping-Zeilen wieder hergestellt.

Damit kann die Konfiguration spaeter zwischen Umgebungen mitgenommen werden.

## Datenbank-Schema

Geaendert:

```text
Data/AppDbContext.cs
Services/DatabaseInitializationService.SchemaSql.cs
Services/DatabaseSchemaMaintenanceService.cs
```

Neu:

- `DbSet<ManualExcelColumnMapping>`
- `CREATE TABLE ManualExcelColumnMappings`
- Schema-Wartung legt die Tabelle nachtraeglich an, falls sie in einer bestehenden DB fehlt.
- Beim Loeschen eines Standorts werden dessen manuelle Excel-Mappings mit geloescht.

## Deutschland lokal eingerichtet

Am 2026-05-04 wurde Deutschland in der lokalen Datenbank direkt ohne UI eingerichtet.

Lokale DB:

```text
C:\Users\koi\source\repos\Ai\TrafagSalesExporter\trafag_exporter.db
```

Gefundener/konfigurierter Standort:

```text
Id=8
TSC=TRDE
Land=Deutschland
SourceSystem=MANUAL_EXCEL
```

Aktive Mapping-Zeilen:

```text
26
```

Konkrete Zuordnung fuer DE:

```text
ExtractionDate           <- Export-Datum
InvoiceNumber            <- Belegnummer
PositionOnInvoice        <- Position
Material                 <- ArtikelNummer
Name                     <- ArtikelBezeichnung
ProductGroup             <- Warengruppen-Bezeichnung
Quantity                 <- Anz. VE
SupplierNumber           <- Lieferanten Nummer
SupplierName             <- Name Lieferant
SupplierCountry          <- Land Lieferant
CustomerNumber           <- AdressNummer-Kunde
CustomerName             <- Name Kunde
CustomerCountry          <- Land Kunde
CustomerIndustry         <- Branche
StandardCost             <- EinstandsPreis
StandardCostCurrency     <- Währung
PurchaseOrderNumber      <- BestellNummer
SalesPriceValue          <- NettoPreisGesamtX
SalesCurrency            <- Währung
DocumentCurrency         <- Währung
CompanyCurrency          <- Währung
Incoterms2020            <- Versandbedingung
SalesResponsibleEmployee <- AdressNummer_V
InvoiceDate              <- Belegdatum-Rechnung
OrderDate                <- BelegDatum Auftrag
DocumentType             <- =Manual Excel
```

Wichtig fuer Rollback/Umzug:

- Diese DE-Einrichtung wurde direkt in `trafag_exporter.db` gespeichert.
- Die DB-Aenderung ist kein Git-Commit-Inhalt, weil SQLite-Datenbankdaten normalerweise nicht sauber versioniert werden.
- Der Code fuer den Mapper ist aktuell im Worktree vorhanden, aber noch nicht committed.
- Wenn die DB zurueckgerollt oder neu erstellt wird, muss das DE-Mapping erneut ueber die UI, Config-Import oder ein Hilfsskript eingerichtet werden.

## Tests

Ergaenzt:

```text
TrafagSalesExporter.Tests/ManualExcelImportServiceTests.cs
```

Neuer Test:

```text
ReadSalesRecordsAsync_Uses_Configured_Manual_Excel_Mapping_For_German_Headers
```

Der Test prueft:

- deutsches Excel-Headerformat
- Kommentarzeile ohne echte Position wird ignoriert
- echte Belegposition wird importiert
- `NettoPreisGesamtX` mit Schweizer Tausenderzeichen wird korrekt als Dezimalzahl gelesen
- Waehrung `EUR` wird in Sales-/Document-/Company-Currency uebernommen
- Rechnungsdatum und Auftragsdatum werden korrekt gelesen

Letzter bekannter Teststand nach Mapper-Arbeit:

```text
dotnet build .\TrafagSalesExporter.csproj --verbosity minimal
dotnet build .\Tools\FinanceProbe\FinanceProbe.csproj --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal --no-restore
```

Ergebnis:

- Hauptprojekt baut erfolgreich
- FinanceProbe baut erfolgreich
- Tests erfolgreich
- `49/49` Tests gruen

Bekannte Warnung:

- `NU1900`, weil NuGet-Sicherheitsdaten wegen Netzwerk/nuget.org nicht geladen werden konnten

## Aktueller Laufstand

Die Haupt-App war nach der DE-Konfiguration erreichbar:

```text
http://localhost:55416/standorte
HTTP 200
```

Hinweis:

- Der Browser kann geschlossen sein, waehrend der Serverprozess weiterlaeuft.
- Wenn ein Build wegen gesperrter Dateien fehlschlaegt, zuerst den laufenden `TrafagSalesExporter`-Prozess beenden.

## Noch offen nach Excel-Spaltenmapper

1. Mapper-Code committen, sobald der aktuelle Stand als Rollback-Punkt gesichert werden soll.
2. In der Standort-UI Deutschland oeffnen und visuell pruefen, ob die 26 Mapping-Zeilen angezeigt werden.
3. Mit echtem DE-Excel einen Importlauf testen.
4. Danach Finance-Probe erneut pruefen:
   - ob DE nicht mehr `Keine Daten` ist
   - ob `SalesPriceValue` gegen Soll aus `check.xlsx` passt
5. Falls weitere Laender eigene Excel-Formate liefern:
   - nicht statischen Code bauen
   - neues Mapping pro Standort pflegen
6. Klaeren, ob DE fachlich `NettoPreisGesamtX` in EUR als Ist-Wert verwenden soll oder ob CHF-Umrechnung noetig ist.

---

## Nachtrag 2026-05-05: FinanceProbe Ampel, Spanien v2 und Deutschland-Beispielfile

### FinanceProbe Management-Ansicht

Das Testprogramm `Tools/FinanceProbe` wurde fuer das Finance-Meeting erweitert.

URL lokal:

```text
http://localhost:55417/finance
```

Neue Ansicht:

- `Meeting Ampel 2025`
- Ampel pro Land:
  - Gruen: Zahl passt rechnerisch gegen Referenz
  - Gelb: Differenz oder fachliche Abgrenzung offen
  - Grau: keine belastbaren Importdaten
- Anzeige pro Land:
  - Ist
  - Soll / Referenz
  - Differenz
  - passender technischer Wert
  - Waehrung / CHF-Hinweis
  - kurze fachliche Begruendung

Wichtig zur Waehrung:

- Wenn Quelle `CHF` liefert, kann CHF direkt gezeigt werden.
- Wenn Quelle `EUR`, `USD`, `GBP`, `INR` usw. liefert, ist es Mandanten-/Originalwaehrung.
- CHF-Ausweis braucht dann eine separate FX-Regel bzw. offiziellen Umrechnungskurs.

### Spanien v2 im Testprogramm

Spanien wird im FinanceProbe nicht mehr nur als normaler Zentralimport betrachtet.

Direkter CSV-Check:

```text
sagespain/v2/Spain_Sales_2025.csv
```

Gelesene Werte:

- Zeilen: `4'341`
- Ist 2025 / `SalesPriceValue`: `3'082'320.18`
- Waehrung: `EUR`
- Soll aus `check.xlsx`: `3'102'333.61`
- Differenz: `-20'013.43`

Status:

- Ampel: Gelb / Pruefen
- Grund: Export technisch lesbar, aber Differenz zu `check.xlsx` offen.

Offen fuer Spanien:

- korrekte Datumsabgrenzung (`FechaFactura` vs. Alternativen)
- Serien `REG`, `LAT`, `PRO`, `REC`
- Behandlung von Gutschriften / `REC`
- offizielle Sage-Auswertung mit identischem Filter zur Sollzahl

### Deutschland-Beispielfile

Neues File im Projektordner:

```text
DE_Beispiel_Export_Daten.xlsx
```

Hinweis:

- Der Benutzer hatte zuerst `.xls` genannt, vorhanden ist `.xlsx`.
- Das File ist als Beispielfile zu behandeln, nicht als finale Jahresdatei.

Technischer Check:

- relevante Spalte: `NettoPreisGesamtX`
- Mapping-Ziel: `SalesPriceValue`
- Betragszeilen: `2`
- Summe `NettoPreisGesamtX`: `8'290.70`
- Waehrung: `EUR`

Einbau im FinanceProbe:

- eigener Abschnitt `Germany Excel sample check`
- zeigt Datei, Zeilenzahl, Summe und Referenz aus `check.xlsx`
- markiert explizit, dass die Differenz nur Sample-Charakter hat
- in der Management-Ampel wird Deutschland weiter nicht als OK gewertet, solange kein finaler DE-Jahresexport/import vorliegt

Fachliche Interpretation fuer Deutschland:

- Das Mapping funktioniert technisch.
- `NettoPreisGesamtX` kann als Kandidat fuer `SalesPriceValue` gelesen werden.
- Das Beispielfile darf nicht gegen die Jahresreferenz `3'635'922.91` als finale Ist-Zahl verwendet werden.
- Fuer das Meeting ist die Aussage:
  - Deutschland-Format ist technisch verstanden.
  - Finale DE-Zahl fehlt noch.
  - Benoetigt wird ein vollstaendiger DE-Jahresfile 2025 oder ein bestaetigter Importlauf.

### Verifikation 2026-05-05

Ausgefuehrt:

```text
dotnet build .\Tools\FinanceProbe\FinanceProbe.csproj --verbosity minimal --no-restore
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal --no-restore
```

Ergebnis:

- FinanceProbe Build erfolgreich
- Tests erfolgreich
- `50/50` Tests gruen
- Web UI liefert `HTTP 200`
- FinanceProbe enthaelt:
  - `Meeting Ampel 2025`
  - `Spain CSV direct check`
  - `Germany Excel sample check`

## Financechef-Regeln abgesichert 2026-05-11

Umgesetzt:

- `PostingDate` als eigenes Feld in `SalesRecord` und `CentralSalesRecord`.
- Zentrale SQLite-Tabelle erhaelt `PostingDate` automatisch per Schema-Maintenance.
- HANA-B1 liest `DocDate` als Buchungsdatum und `TaxDate` als Fakturadatum.
- Excel/CSV-Import erkennt `posting date`, `Buchungsdatum` und `LineRegistrationDate`.
- Finance-Abgleich filtert das Jahr nach `PostingDate`, mit Fallback auf `InvoiceDate` und danach `ExtractionDate`.
- Finance-Abgleich bevorzugt Nettofakturawert in Hauswaehrung positionsweise.
- Wenn lokale Belegkopfwerte pro Position wiederholt wirken, wird die Ueberzaehlung erkannt:
  - B1-Positionswert `SalesPriceValue` wird dann als Positions-Netto bevorzugt.
  - deduplizierter Belegkopfwert bleibt als Kandidat sichtbar.
- Intercompany wird weiterhin separat ausgewiesen und nicht still entfernt.

Verifikation:

```text
dotnet build .\Tools\FinanceProbe\FinanceProbe.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\verify_probe_out\ --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore -p:UseAppHost=false --verbosity minimal
```

Ergebnis:

- FinanceProbe Build erfolgreich.
- Tests erfolgreich: `57/57`.
- Bekannte externe Warnung: NuGet-Sicherheitsdaten konnten wegen fehlendem Zugriff auf `api.nuget.org` nicht geladen werden.
- Lokaler Smoke-Test `/finance`: `HTTP 200`.
- Hinweis: Ein bestehender `dotnet`-Prozess sperrt den normalen FinanceProbe-Build-Output. Der Smoke-Test wurde deshalb ohne Rebuild direkt aus dem vorhandenen Output gestartet.

## Finance-Entscheide dokumentiert 2026-05-11

Neue Doku:

```text
docs/FINANCE_ENTSCHEIDE.md
```

Enthaelt die verbindlichen Financechef-Entscheide:

- Hauswaehrung ist fuehrend.
- CHF-Umrechnung ueber Budgetkurse.
- Aggregation pro Artikel/Belegposition.
- Net Sales Actuals = Nettofakturawert.
- Jahresabgrenzung ueber Buchungsdatum.
- Gutschriften separat ueber Beleg-/Positionslogik.
- Intercompany/2nd-party separat ausweisen.
- Indien fachlich immer in `INR`.

## FinanceProbe / UK Nachdokumentation 2026-05-11

Ergaenzt in `docs/FINANCE_ENTSCHEIDE.md`:

- Pruefstand der Finance-Regeln.
- Testergebnis `58/58`.
- UK/England-Befund:
  - `TRUK`
  - `1'881` geladene Zeilen
  - `395'605.82 GBP` Ist
  - `3'749'865.00` Soll
  - Differenz `-3'354'259.18`
  - Interpretation: vermutlich Teilmenge/Monatsfile statt Jahreswert.
- Offener UK-Entscheid: Monatsdateien aufsummieren oder kumulierten Jahresfile lesen.

Ergaenzt in `docs/PROGRAMM_DIAGRAMME.md`:

- FinanceProbe-Start und Hinweis zu Console-Logging.
- Hinweis zu DLL-Sperren durch Visual Studio bzw. alte `dotnet`-Prozesse.

## HR KPI Cockpit und Filterkorrektur 2026-05-13

Ergaenzt:

- Separater HR-KPI-Reiter `/hr-kpi`.
- Dashboard-Tabs fuer Ueberblick, Fluktuation, Absenzen, Zeit/Ferien, Mitarbeitende und Datenstatus.
- Fluktuationsvisuals: Gauge, Funnel, Donut, Organisation-Balken und Monatsbalken.
- Architektur-Cleanup: `HrKpiService` als Fassade, Build-Pipeline in `Services/HrKpi/HrKpiDashboardBuilder.cs`, UI-Tabs in `Components/HrKpi/HrKpiDashboardTabs.razor`.
- Konfigurierbare HR-Dateiquellen ueber `HrKpi` in `appsettings.json`.
- HR-KPI-Regressionstests.

Korrigiert:

- `Austrittsjahr` ist jetzt optional.
- Leeres Austrittsjahr bedeutet: alle Austritte.
- Von/Bis-Austritt hat Vorrang vor Austrittsjahr.
- Die Austrittsjahr-Auswahl wird aus den vorhandenen Austrittsdaten aufgebaut.
- `Austrittsjahr` ist beim Start leer statt automatisch aktuelles Jahr.
- Fluktuation nutzt nur vergleichbare Filter auf Mitarbeitenden- und Austrittsdaten.
- Kostenstelle, GLZ und Restferien filtern nicht die Fluktuation, weil die Austrittsdatei diese Felder nicht stabil enthaelt; das Cockpit zeigt dazu einen Hinweis.
- Bei Mehrjahresauswahl wird die Fluktuation als Auswahlwert statt als Jahreswert gefuehrt.
- Fluktuationsvisuals zaehlen distinct nach Personalnummer.
- Fluktuationsraten nutzen nun durchschnittlichen Headcount statt Stichtags-Headcount: Monat, Quartal und Jahr folgen `formeln.docx`.
- Krankenquote nutzt den FTE-Nenner: `Krankheitstage / (FTE * 21 Tage)`.
- Rexx-Austrittsarten mit Umlaut werden korrekt normalisiert: `Kündigung AN` zaehlt als Arbeitnehmerkuendigung, `Kündigung AG` als Arbeitgeberkuendigung-Ausschluss, `Ruhestand` als Pensionierung.

Nachdokumentation:

```text
docs/HR_KPI_NACHDOKU_2026-05-13.md
```

Verifikation:

- `dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_app\ --verbosity minimal`
- `dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_tests\ --verbosity minimal`
- Ergebnis: `69/69` Tests bestanden.
- Kontrollwert `C:\temp\Personalausgeschieden.xlsx`: `104` Austritte total, `42` `Kündigung AN`, `34` `Kündigung AG`, `33` fluktuationsrelevant.
- Kontrollwert neuer Nenner: Avg Headcount 2025 `211.3`, Fluktuation Jahr effektiv `15.6%`.

## FinanceProbe Finanzchef-Uebersicht 2026-05-13

Ergaenzt:

- Neuer Reiter `Finanzchef Uebersicht` in `Tools/FinanceProbe`.
- Kompakte Soll/Ist-Sicht nur fuer offene Laender.
- Spalten reduziert auf Status, Land, Waehrung, Ist, Soll, Abweichung und Pruefgrund.
- Bestehende Detailtabellen bleiben unveraendert fuer Analyse/Nachvollzug.

Verifikation:

- `dotnet build .\Tools\FinanceProbe\FinanceProbe.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_financeprobe\ --verbosity minimal`
- Ergebnis: Build erfolgreich, `0` Fehler.
- Hinweis: `NU1900` wegen nicht erreichbarer NuGet-Sicherheitsdaten im eingeschraenkten Netzwerk.

## Finance CFO Word-Kurzbericht 2026-05-13

Erstellt:

- `docs/FINANCE_CHEF_SUMMARY_2026-05-13.docx`
- Kurzbericht fuer Finance/CFO mit Kernaussagen und Massnahmen.
- Enthalten: FR, IN, US, AT, ES, UK/EN, DE, CH, IT.
- Ausgeschlossen: GFS und reine 0-/Leer-Faelle ohne operative Aussage.

Inhaltlicher Fokus:

- Freigabefaehige Laender: FR, IN, US.
- Kleine/mittlere Klaerung: AT, ES.
- Hohe Prioritaet: UK/EN, DE, CH.
- Kritisch: IT wegen groesster Abweichung und offener Berechnungsart.

## Finance CFO Word-Kurzbericht Erweiterung 2026-05-15

Ergaenzt:

- Aktuelle Fassung: `docs/FINANCE_CHEF_SUMMARY_2026-05-15.docx`
- Erweiterte Tabellenansicht mit Status, Ist, Soll/Rhino, Abweichung, Pruefquelle, Massnahme und Prioritaet.
- Grafische Ampel-Uebersicht fuer OK/Klaeren/Hoch/Kritisch.
- Prioritaetsgrafik fuer IT, DE, UK/EN, CH, AT/ES.
- Abschnitt `Geprueft gegen` mit Rhino/Andreas `check.xlsx`, FinanceProbe/CentralSalesRecords, Spain CSV, Deutschland-Beispielfile und UK_B1.

Verifikation:

- DOCX enthaelt `word/document.xml`.
- Inhalte `Rhino / Andreas check.xlsx`, `Management-Ampel`, `Prioritaetsgrafik` und `Laendertabelle mit Massnahmen` wurden im Dokumentpaket geprueft.

## Finance Spanien Mailentwurf 2026-05-15

Erstellt:

- `docs/FINANCE_ES_MAIL_ABWEICHUNG_2026-05-15.md`
- Spanischer Mailentwurf zur Abweichung Spanien Net Sales 2025.
- Enthaltene Pruefpunkte: Zeitraum, Serien `REG/LAT/PRO/REC`, Abonos/Credit Notes, Datumslogik und verwendetes Netto-Umsatzfeld.

## Finance IT und UK Mailentwuerfe 2026-05-15

Erstellt:

- `docs/FINANCE_IT_MAIL_ABWEICHUNG_2026-05-15.md`
- `docs/FINANCE_UK_MAIL_ABWEICHUNG_2026-05-15.md`

Inhalt:

- Italien: grosse Abweichung `+7.034.496,29 EUR`, Fokus Berechnungsart, Beleg/Position-Deduplizierung, Intercompany, Credit Notes, Datumslogik und Waehrung.
- UK/England: Restdifferenz `-216,154.91 GBP`, Fokus Jahresvollstaendigkeit, Periodenbereich, Credit Notes, Nettofeld, Discounts/Freight/Charges, 2nd-/3rd-party und Waehrung.

## Finance Entscheide Extraktion 2026-05-15

Erstellt:

- `entscheide.md`

Inhalt:

- Fragen und Entscheide aus der Finance-Abstimmung extrahiert.
- Festgehaltene Kernentscheide: Hauswaehrung je Land, Budgetkurse fuer CHF-Sicht, Berechnung pro Artikel/Belegposition, Nettofakturawert, Buchungsdatum, separate Gutschriftenausweisung und Intercompany/2nd-party als eigenes Auswahlfeld.
- Intercompany-Marker dokumentiert: `MAGNETS SENSE`, `MAGNETIC SENSE`, `TRAFAG`, `GESELLSCHAFT FUER SENSORIK`, `GESELLSCHAFT FUR SENSORIK`.

## Finance Dokumentgueltigkeit 2026-05-15

Erstellt:

- `docs/FINANCE_WELCHES_DOKUMENT_GILT_2026-05-15.md`

Festgelegt:

- Fuehrendes CFO-Dokument: `docs/FINANCE_CHEF_SUMMARY_2026-05-15.docx`
- Alte CFO-Version `docs/FINANCE_CHEF_SUMMARY_2026-05-13.docx` entfernt, weil sie durch die Version vom 2026-05-15 ersetzt wurde.
- Entscheidbasis: `entscheide.md` und `docs/FINANCE_ENTSCHEIDE.md`.

## Finance Dashboard Todo 2026-05-15

Erstellt:

- `docs/FINANCE_DASHBOARD_TODO_2026-05-15.md`

Inhalt:

- Todo-Liste fuer Group Sales Reporting Intranet-Dashboard.
- Priorisierte Punkte fuer CFO-Dokument, offene Laenderabweichungen, Intercompany, Budgetkurse und Berechtigungskonzept.

## Navigation und HR-KPI-Zugriff 2026-05-15

Geaendert:

- Linke Navigation reduziert:
  - Hauptgruppe `Finance Cockpit`
  - eigener Hauptpunkt `HR KPI (Login)`
- Bisherige Finance-Seiten liegen als Unterpunkte unter `Finance Cockpit`:
  - Dashboard
  - Management Cockpit
  - Standorte
  - Transformationen
  - Settings
  - Logs
- HR KPI hat eine separate zweite Zugriffssperre mit Name und Passwort.
- HR-Daten werden erst geladen und angezeigt, wenn die HR-KPI-Sperre erfolgreich entsperrt wurde.

Konfiguration:

- Abschnitt `HrKpiAccess` in `appsettings.json`
- Benutzer: `hr`
- Passwortvorschlag: `Trafag-HR-KPI-2026!`
- Im Repo ist nur der SHA-256-Hash gespeichert, nicht das Klartextpasswort.

Verifikation:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_hrlogin\ --verbosity minimal
```

Ergebnis:

- Build erfolgreich.
- 3 bestehende MudBlazor-Analyzer-Warnungen in `Logs.razor`, `Transformations.razor` und `Standorte.razor`.

## IIS 500 Diagnose und Hosting-Modell 2026-05-20

Geaendert:

- `web.config` fuer IIS auf `hostingModel="outofprocess"` umgestellt.
- `stdoutLogEnabled="true"` bleibt aktiv, Logziel bleibt `.\logs\stdout`.
- `ASPNETCORE_DETAILEDERRORS=true` fuer die temporaere IIS-Fehlerdiagnose gesetzt.
- Ziel: Wenn IIS/ASP.NET Core vor dem App-Start scheitert, sollen eher verwertbare Startlogs entstehen; ausserdem wird die App nicht mehr direkt im IIS Worker-Prozess gehostet.

Aktueller Stand:

- Publish-Ordner `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\` enthaelt `BiDashboard.dll`, `web.config`, `wwwroot`, `runtimes`, `trafag_exporter.db` und `logs`.
- `logs` war trotz aktivem stdout-Logging leer.
- Die veroeffentlichte DLL liess sich vom Publish-Ordner aus starten und brach nicht sofort mit einer Exception ab.
- Remote-Pruefung der installierten .NET-Runtimes per WinRM war nicht moeglich; der Serveradmin muss deshalb am Server pruefen, ob das .NET 8 Hosting Bundle installiert ist.

Naechste Server-Pruefpunkte:

- URL `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/diag.txt` testen.
- Wenn `diag.txt` nicht erreichbar ist, stimmt IIS-Anwendung/virtueller Pfad/Binding nicht.
- Wenn `diag.txt` erreichbar ist, aber die App 500 liefert, Windows Event Viewer pruefen:
  - Windows Logs > Application
  - Quellen: `IIS AspNetCore Module V2`, `.NET Runtime`, `Application Error`
- App Pool pruefen:
  - .NET CLR Version: `No Managed Code`
  - Pipeline: `Integrated`
  - 32-bit Applications: `False`
  - Identity muss Modify-Rechte auf Publish-Ordner und `logs` haben.

## Architekturreview Static/Hardcoding 2026-05-15

Erstellt:

- `docs/ARCHITEKTUR_REVIEW_STATICS_HARDCODING_2026-05-15.md`

Inhalt:

- Bewertung der vielen `static`-Methoden im Code.
- Ergebnis: `static` ist fuer kleine zustandslose Helper akzeptabel; problematisch sind fachliche Regeln und grosse Klassen mit zu vielen Verantwortungen.
- Dokumentierte Befunde:
  - HR-Testpersonen sind aktuell Code-Regel und sollten in Konfiguration/DB.
  - Finance Vergleich ist aktuell fix auf `2025` und Referenztext.
  - Hauswaehrungen je Land sollten langfristig in Finance-/Standortkonfiguration.
  - Finance-Sollwerte, Budgetkurse und IC-Regeln sind als Seed okay, aber produktiv pflegbar machen.
- Empfehlung: nicht blind alle `static`-Methoden entfernen, sondern zuerst fachlich veraenderbare Regeln auslagern.

## CFO-Bericht IT/Intercompany Diagnose 2026-05-15

Ergaenzt:

- `docs/CFO_Kurzbericht_270515.docx`
- `docs/FINANCE_DASHBOARD_TODO_2026-05-15.md`

Inhalt:

- IT/Intercompany-Diagnose fuer die grosse Italien-Abweichung.
- Marker dokumentiert: `TRAFAG`, `MAGNETIC SENSE`, `MAGNETS SENSE`, `GESELLSCHAFT FUER SENSORIK`, `GESELLSCHAFT FUR SENSORIK`.
- Zahlen:
  - IT Ist vor IC-Abzug: `14.704.336,29 EUR`
  - IC-/2nd-party-Abzug: `4.397.746,90 EUR`
  - IT Ist exkl. IC: `10.306.589,39 EUR`
  - Rhino/check.xlsx Soll: `7.669.840,00 EUR`
  - Restabweichung nach IC: `+2.636.749,39 EUR`

Bewertung:

- Intercompany/2nd-party erklaert einen grossen Teil der IT-Abweichung.
- Restabweichung bleibt offen und muss ueber Summenlogik, Beleg/Position-Deduplizierung, Gutschriften/Storno und weitere lokale IC-Kunden oder Schreibweisen geprueft werden.

## HR KPI Testpersonen-Ausschluss 2026-05-15

Geaendert:

- Folgende Testpersonen werden zentral aus dem HR-KPI-Dashboard ausgeschlossen:
  - Angelina Jolie
  - Brad Pitt
  - Peter Muster
  - ICT Trafag
  - Empfanger Reminder / Empfaenger Reminder
- Der Ausschluss erfolgt vor KPI-, Filter- und Tabellenberechnung.
- Betroffen sind aktive Mitarbeitende, Absenzen und Austritte.
- Im Dashboard erscheint eine Notice, wie viele Testpersonen-Zeilen ausgeschlossen wurden.

Verifikation:

```text
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_hr_exclusions\ --verbosity minimal
```

Ergebnis:

- 70/70 Tests erfolgreich.
- 3 bestehende MudBlazor-Analyzer-Warnungen in `Logs.razor`, `Transformations.razor` und `Standorte.razor`.

## KI-Arbeitsanweisung 2026-05-15

Erstellt:

- `persona.md`

Inhalt:

- Rolle der KI als Entwicklungs-, Analyse- und Dokumentationswerkzeug.
- Grenzen der KI bei fachlicher Verantwortung, Finance, HR, Datenschutz und Freigaben.
- Arbeitsprinzipien fuer dieses Projekt: bestehende Architektur nutzen, kritisch testen, sauber dokumentieren und offene fachliche Punkte als Pruefpunkte markieren.

## Navigation in Finance/HR/Admin gegliedert 2026-05-15

Geaendert:

- Linke Navigation neu gegliedert:
  - `Finance Cockpit`
  - `HR KPI (Login)`
  - `Admin`
- Unter `Finance Cockpit` stehen:
  - `Export Dashboard`
  - `Management Analyse`
  - `Soll/Ist Vergleich`
- Unter `Admin` stehen:
  - `Standorte`
  - `Transformationen`
  - `Settings`
  - `Logs`
- Seitentitel wurden an die neuen Menuebezeichnungen angepasst.

Verifikation:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_nav_groups\ --verbosity minimal
```

Ergebnis:

- Build erfolgreich.
- 3 bestehende MudBlazor-Analyzer-Warnungen in `Logs.razor`, `Transformations.razor` und `Standorte.razor`.

## DE Alphaplan-Excel provisorisch vorbereitet 2026-05-20

Geaendert:

- Deutschland wird beim Start als manueller Excel-Standort vorbereitet:
  - `TSC = TRDE`
  - `Land = Deutschland`
  - `SourceSystem = MANUAL_EXCEL`
  - `IsActive = false`, damit der Gesamtexport ohne gesetzte Datei nicht scheitert.
- Alphaplan-Mapping wird fuer Deutschland geseedet:
  - `NettoPreisGesamtX` -> `SalesPriceValue`
  - `Belegnummer`, `Position`, `ArtikelNummer`, `ArtikelBezeichnung`
  - `Warengruppen-Bezeichnung`, `Anz. VE`
  - Lieferant/Kunde/Land/Branche
  - `Waehrung`, `Versandbedingung`, `AdressNummer_V`
  - `Belegdatum-Rechnung` fuer Posting-/Invoice-Date
  - `DocumentType = Alphaplan Excel`
- Testdatei erhalten und eingeordnet:
  - `docs/2025_DataExport_DE.xlsx`

Erster Befund:

- `NettoPreisGesamtX` komplett: `4'154'690.05 EUR`
- `Land Kunde = Deutschland`: `3'455'276.64 EUR`
- `Land Kunde = Deutschland + China`: `3'647'592.44 EUR`
- Sollwert DE: `3'635'923.00 EUR`

Offen:

- Finance/Munir muss bestaetigen, welche Kundenlaender und Filter in Alphaplan fuer den offiziellen DE-Istwert gelten.
- Manager-Input nennt Warengruppen- und Versandbedingungs-Codes; im Excel sind aktuell vor allem Bezeichnungen/Texte sichtbar.

Verifikation:

```text
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_de_alphaplan\ --verbosity minimal
```

Ergebnis:

- 75/75 Tests erfolgreich.
- Bestehende Warnungen: NU1900 wegen lokaler Paket-Sicherheitsdatenabfrage, sowie bekannte MudBlazor-Analyzer-Warnungen zu `Dense`.

## Management Cockpit zentrale Filterkopplung 2026-05-15

Geaendert:

- Die untere `Zentrale Roh-Auswertung` im Management Cockpit ist nicht mehr nur global.
- Neue Filterfelder: `Landfilter` und `TSC`.
- Wenn oben eine Einzeldatei analysiert wird, uebernimmt die zentrale Auswertung automatisch Land und TSC aus dieser Datei.
- Beispiel: Auswahl `USA | TRUS | Sales_TRUS_2026-05-08.xlsx` setzt unten automatisch `USA / TRUS`.
- Button `Global` leert die Filter, falls wieder alle Laender/Standorte ausgewertet werden sollen.
- Jahres-, Monats-, Jahreswerte-, Monatswerte-, Tageswerte-, Quellen- und Laendertabellen verwenden denselben Land/TSC-Filter.

Verifikation:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_management_scope2\ --verbosity minimal
```

Ergebnis:

- Build erfolgreich.
- 3 bestehende MudBlazor-Analyzer-Warnungen in `Logs.razor`, `Transformations.razor` und `Standorte.razor`.

## Finance Vergleich als eigener Reiter 2026-05-15

Geaendert:

- `Net Sales Actuals 2025 Referenz` aus dem Start-Dashboard entfernt.
- Neue Seite `Finance Vergleich` unter `Finance Cockpit` angelegt.
- Route: `/finance-cockpit/vergleich`
- Die Seite zeigt den Soll/Ist-Vergleich gegen `check.xlsx` separat, inklusive IC-Abzug, Referenzwert, Summenfeld, Differenz, Waehrung, Zeilen und Status.
- `DashboardPageService` laedt die Finance-Referenzdaten nicht mehr automatisch mit dem operativen Dashboard.

Verifikation:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_finance_compare_tab\ --verbosity minimal
```

Ergebnis:

- Build erfolgreich.
- 3 bestehende MudBlazor-Analyzer-Warnungen in `Logs.razor`, `Transformations.razor` und `Standorte.razor`.

## Finance-Regeln und Dashboard-Basis-Spalte 2026-05-20

Geaendert:

- Neuer Admin-Reiter `Finance Regeln` angelegt.
  - Route: `/finance-rules`
  - Navigation: `Admin -> Finance Regeln`
  - Zugriff wie andere Admin-Seiten ueber `AdminOnly`.
- Neue Tabelle/Model `FinanceRules`.
- Finance-Regeln werden beim Start geseedet und sind danach in der UI pflegbar.
- Die Regeln wirken auf die Finance-Sicht, nicht auf Rohdaten und nicht auf das technische Spaltenmapping.
- DE- und IT-Sonderlogik wurde aus dem zentralen Excel-Export in eine generische Regel-Engine verschoben.
- `ConfigTransferService` exportiert/importiert `FinanceRules` mit.
- `FinanceReconciliationService` nutzt dieselbe Regel-Engine wie das zentrale Excel, damit Soll/Ist-Vergleich und Endexcel dieselbe Finance-Sicht verwenden.
- Export Dashboard:
  - neue Spalte `Basis` direkt nach `Land`
  - zeigt Datenbasis mit Icon und Text:
    - `Excel-Datei`
    - `CSV-Datei`
    - `SAP Service`
    - `Server`
    - `Manuelle Datei`

Aktuelle Default-Finance-Regeln:

- `DE`
  - Jahr auf `2025` erzwingen fuer das Alphaplan-Jahresfile.
  - `CustomerName = Trafag AG` ausschliessen.
  - `CustomerName contains Magnetic Sense` ausschliessen.
  - `InvoiceNumber = GS2510095` ausschliessen, weil bereits 2024 erfasst.
  - `InvoiceNumber starts with GS` als negativer Betrag zaehlen.
- `IT`
  - `CustomerName contains Trafag Italia` ausschliessen.
  - doppelte Zeilen ohne `SupplierCountry` deduplizieren.

DE-Fachabgleich nach Rueckmeldung Deutschland:

```text
Gesamtumsatz NettoPreisGesamtX:                 4'154'690.05
- Weiterberechnungen Trafag AG:                   391'655.88
- Weiterberechnungen Magnetic Sense 2025:          55'648.21
- Gutschriften GS als negativ statt positiv:        28'205.60 doppelte Wirkung
- GS2510095 nicht in 2025:                           1'419.70
= DE Jahresabschluss-Umsatz:                    3'652'394.46
```

Verifikation:

```text
dotnet test TrafagSalesExporter.sln --verbosity minimal
```

Ergebnis:

- 76/76 Tests erfolgreich.

Echter DE-Import und zentrale Excel erneut geprueft:

```text
CentralSalesRecords DE 2025 rows: 4'430
CentralSalesRecords DE 2025 SalesPriceValue: 3'652'394.46
Central Excel Sales sheet Finance DE 2025 sum: 3'652'394.46
Central Excel Finance Summary DE 2025 sum: 3'652'394.46
```

Technische Hauptdateien:

- `Models/FinanceRule.cs`
- `Services/FinanceRuleEngine.cs`
- `Services/FinanceRulesPageService.cs`
- `Components/Pages/FinanceRules.razor`
- `Services/ExcelExportService.cs`
- `Services/FinanceReconciliationService.cs`
- `Services/DashboardPageService.cs`
- `Components/Pages/Dashboard.razor`
- `Data/AppDbContext.cs`
- `Services/DatabaseInitializationService.SchemaSql.cs`
- `Services/DatabaseSchemaMaintenanceService.cs`
- `Services/DatabaseSeedService.cs`
- `Services/ConfigTransferService.cs`

## Finales zentralisiertes Excel `finall.xlsx` geprueft 2026-05-20

Gepruefte Datei:

- `C:\Users\koi\Downloads\finall.xlsx`

Ergebnis:

- Datei ist als zentralisierter Export lesbar.
- Blaetter:
  - `Finance Summary`
  - `Sales`
  - `Finance Filter Hilfe`
- `Sales` enthaelt 67'247 Datenzeilen.
- `Finance Summary` stimmt gegen die Finance-Spalten im Blatt `Sales` exakt:
  - Differenz je Land/Jahr/Waehrung: `0.00`
- Die Summen entsprechen der lokal erzeugten zentralen Datei `output\Sales_All_2026-05-20.xlsx`.
- Deutschland 2025 bleibt korrekt:
  - `DE 2025 EUR = 3'652'394.46`

Finance-Summen 2025 aus `finall.xlsx`:

```text
AT  EUR   3'438'121.37
CH  CHF  43'521'390.82
DE  EUR   3'652'394.46
ES  EUR   3'082'320.18
FR  EUR   1'471'218.44
IN  INR 750'936'591.38
IT  EUR   7'663'145.76
UK  GBP   3'533'710.09
US  USD   3'749'865.33
```

Hinweis:

- Der direkte Vergleich gegen die lokale SQLite-Datei `trafag_exporter.db` war nicht aussagekraeftig, weil diese lokale DB keine passenden `CentralSalesRecords` fuer diesen Stand enthaelt.
- Die Excel-interne Summenpruefung und der Vergleich gegen `output\Sales_All_2026-05-20.xlsx` waren konsistent.

## Admin Bereich und Startseite aktualisiert 2026-05-21

Admin Bereich:

- `/admin/sessions` ist nicht mehr durch den Finance-Cockpit-Login vorgeschaltet.
- Der Admin Bereich nutzt weiterhin ein eigenes Admin-Passwort über `AdminAccess`.
- Initialer Benutzer: `admin`
- Initiales Passwort: `TrafagAdmin2026!`
- Das Admin-Passwort ist unabhängig vom Finance-Cockpit-Passwort.
- Dokumentation ergänzt: `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md`

Startseite:

- Corporate-Schrift auf `Open Sans` mit Trafag-nahen Fallbacks angepasst.
- Manometer-Startgrafik bleibt auf weißem Hintergrund, schwarz gezeichnet und mit Trafag-Schriftzug.
- Willkommenstext ist sprachabhängig.
- Optionales Strichmännchen mit Kittel unter dem Willkommenstext ergänzt.
- Das Strichmännchen ist standardmäßig deaktiviert.
- Aktivierung über `Admin Bereich` -> `Strichmännchen anzeigen`.
- Einstellung wird in `appsettings.json` unter `LandingPage.ShowWalkingLabFigure` gespeichert.

Technische Dateien:

- `Components/Routes.razor`
- `Components/App.razor`
- `wwwroot/css/app.css`
- `Components/Pages/Dashboard.razor`
- `Components/Pages/AdminSessions.razor`
- `Program.cs`
- `Security/LandingPageOptions.cs`
- `Services/LandingPageSettingsService.cs`
- `Services/UiTextService.cs`
- `appsettings.json`
