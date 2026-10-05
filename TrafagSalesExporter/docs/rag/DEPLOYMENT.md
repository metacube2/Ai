# RAG Deployment

Stand: 2026-10-01

## Werkzeug und drei Fallen im Publish selbst

- **FALLE, am 2026-08-07 selbst hineingelaufen: `dotnet publish` NIEMALS ueber das
  Bash-Werkzeug auf den UNC-Pfad.** Git Bash macht aus
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$` das lokale Verzeichnis
  `C:\trch-webapp-bidashboard.trafagch.local\BiDashboard$` — der Publish meldet
  Erfolg, legt 120 Dateien auf der lokalen Platte ab und **der Server bekommt
  nichts**. Weil `app_offline.htm` zu dem Zeitpunkt bereits gesetzt war, stand die
  Anwendung still, ohne dass etwas ausgeliefert wurde. Erkennungsmerkmal in der
  Ausgabe: die letzte Zeile nennt `C:\trch-webapp-...` statt `\\trch-webapp-...`.
  **Publish immer aus PowerShell**, dort bleibt der UNC-Pfad erhalten. Danach
  zwingend die SHA256 der Server-DLL gegen `bin/Release/net8.0/BiDashboard.dll`
  pruefen — genau dieser Vergleich hat den Fehlschlag sichtbar gemacht.


- **Deploy-Konsole `Tools/DeployConsole`** (2026-08-07, erstmals produktiv erfolgreich
  gelaufen am 2026-08-11): schreibt den Ablauf fest —
  Bestandsaufnahme, `app_offline` unmittelbar vor/nach dem Publish, Publish als
  Argumentliste mit Guard gegen jedes Profil-Argument, Vergleich vorher/nachher,
  Typen-/Literalpruefung in der DLL (UTF-8 und UTF-16), Abrufpruefung, fertiger
  Protokollabsatz. Gegen einen nachgebauten Share verifiziert
  (`Tools/DeployConsole.Probe`, 22 Pruefungen gruen). Der erste Produktivlauf erfolgte
  kopflos ueber `.tmp_tools/DeployHeadless` und endete ohne Alarm.
  Details: `docs/DEPLOYMENT.md` und der Kurzstand unten.

- **FALLE, gemessen am 2026-08-07: ein erfolgreicher Publish kann `BiDashboard.dll`
  stillschweigend ueberspringen.** Die Hauptbaugruppe wird mit PreserveNewest kopiert;
  ist die Datei im Ziel **neuer** als der frische Build, bleibt sie liegen — die alte
  Version laeuft weiter, `dotnet publish` meldet Erfolg. Nachweis: nach einem Publish
  muss die SHA256 der ausgelieferten DLL mit der von `bin/Release/net8.0/BiDashboard.dll`
  **uebereinstimmen**. Eine Abweichung ist hier NICHT der bekannte Nicht-Determinismus
  zweier Builds (siehe Hinweis weiter unten) — der gilt nur beim Vergleich zweier
  getrennter Uebersetzungen, nicht beim Vergleich einer Datei mit ihrer eigenen Kopie.

- **BEFUND 2026-08-07: `check.xlsx`, `zdispo_grp.xlsx` und `zdispo_spart.xlsx` im
  Publish-Verzeichnis sind Build-Ausgabe, kein Datenbestand.** Sie stehen in der csproj
  mit `CopyToPublishDirectory="Always"` und werden bei JEDEM Publish mit dem
  Repository-Stand ueberschrieben. Direkt auf dem Share bearbeiten heisst: Aenderung
  beim naechsten Deploy weg, ohne Meldung.

## Kurzstand

- **Deploy 2026-10-05 15:11, Einkauf Interaktiv (`ba13744`).** `946/946`, `BiDashboard.dll` `14:58:22`, `8'623'616` Bytes, SHA256 `4C03171C...DF24E4`, bitgleich, Alarm nur WAL. Neun Einkaufsansichten per Edge headless geoeffnet.
- **Deploy 2026-10-05 14:25, Verkauf Interaktiv (`25da8a1`).** `944/944`, `BiDashboard.dll` `14:12:48`, `8'553'472` Bytes, SHA256 `465E5CF5...110668`, bitgleich, Alarm nur WAL; DB-Sicherung 552 s. Neun Ansichten per Edge headless geoeffnet.
- **Deploy 2026-10-05 13:34, Logistik Kapazitaet SAP (`dd4c627`).** `936/936`, `BiDashboard.dll` `13:30:18`, SHA256 `8A596F53...59D833`, bitgleich, ohne Alarm. Umschalter Kapazitaet zeigt Vorschau Warenausgang mit P76-Zukunftsdaten (Teil B importiert) und den Hinweis, dass `LogKapSet` in P76 fehlt (`T76K912662` nicht importiert).
- **Deploys 2026-10-05 Logistik live:** 10:31 Produktivitaet (`9fdae38`, `926/926`); 11:20 Kapazitaet je Bereich und Durchlaufzeit (`b776115`, `930/930`, SHA256 `9C0914E9...0C11BE`, ohne Alarm); 12:40 Namen nach Anmeldung, Vorschau Warenausgang, bis Warenausgang (`f9dc3eb`, `935/935`, SHA256 `4EA66149...2E35B2`, Alarm nur WAL). 12:47 Fusszeile ohne „Ohne Personendaten“ (`1c7e4ee`, `935/935`, SHA256 `CA537E5C...552342`, Alarm nur WAL, DB-Dateien vollstaendig). Alle bitgleich, per Edge headless angesehen.
- **Deploy 2026-10-05 10:23, Logistik 3D Platznamen (`71981b6`).** `923/923`, `BiDashboard.dll` `09:55:41`, `8198144` Bytes, SHA256 `96DDD2EA4D8C62E94A994A4BC09627ADF9326402D8F260BFDD1A7D7A1B074729`, bitgleich; Alarm nur WAL. Zwei Laeufe davor (09:56, 10:04) brachen in der DB-Sicherung mit `disk I/O error` ab: Always On VPN trennt mehrmals taeglich mit RAS-Code 829, Heimnetz stabil.
- **Deploy 2026-10-05 09:45, Weltlage EZB-Kurse (`1e3fd0f`).** `919/919`, `BiDashboard.dll` `09:29:16`, `8'197'632` Bytes, SHA256 `08FED7E48F00A74A951AF8B8B9E60E16DD9561E17A84E30C98E3E57A376D559E`, bitgleich; Alarm nur WAL, DB-Dateien vollstaendig. Vorher zwei Abbrueche, weil der fertige Lauf von 09:11 als Prozess haengen blieb und `DeployConsole.dll` sperrte (Ingo beendete ihn). EZB-Quelle vom Server verbunden.
- **Deploy 2026-10-05 09:21, Weltlage und Logistik 3D (`e54190a`, `98146c6`).** `918/918`, `BiDashboard.dll` `09:09:36`, `8'190'976` Bytes, SHA256 `616076E96A110C3D4BE18BF6270A8600702D46382A372D3325F9F005F4A5453B`, bitgleich; Alarm nur WAL (DB-Dateien vollstaendig). Erster Anlauf 08:51 brach bei der Sicherung mit SQLite `disk I/O error` ab (VPN weg), zweiter Lauf nach VPN automatisch. Weltlage-Quellen vom Server: GDELT und Eurostat verbunden, FRED und IMF Firewall blockiert.
- **Deploy 2026-10-05 08:18, Finance Controlling (`cb08f18`).** `896/896`, `BiDashboard.dll` `08:14:39`, `7'943'680` Bytes, SHA256 `14CB016EBB7A4AFB6632F21A017883144BAC1B6C2F1D917CEF40581B4D2471F6`, bitgleich, ohne Alarm; nach VPN-Unterbruch automatisch gestartet. `/finance-cockpit/controlling` ohne Passwort, per Edge headless angesehen.
- **Deploys 2026-10-02 13:51 bis 14:46, Reiter Verkauf.** 13:51 `84fd1fd` (ohne Passwort), 14:05 `fa83ad1` (Vergleich gleicher Zeitraeume), 14:32 `a2bd605` (Korrekturen, 890/890, SHA256 `D019301D40E07ECCDDEAD30EDE9554CAD5A2C2C38F2D448994FC88BE3BA8D574`), 14:46 `81a4132` (Prognoseachse, SHA256 `F4BCA1B5853A94EC638AF7C9C17CDE4C101A4CB6110096777799BFCF751C78FE`). Alarme jeweils nur `-wal`/`-shm` bzw. Checkpoint, Dateien wieder da. Alle acht Seiten per Edge headless angesehen.
- **Deploy 2026-10-02 13:24, Reiter Verkauf (`e2fb8ce`).** `883/883`, `BiDashboard.dll` `13:12:54`, `7'824'896` Bytes, SHA256 `A6608447135F975278F8B968936DE2A9D8DF5923C68CB5290D56004A40D040FD`, bitgleich, ohne Alarm. Die DB-Sicherung dauerte 616 s, weil `-wal` 243 MB nicht zurueckgeschriebene Seiten enthielt (seitenweiser Weg). `/verkauf` zeigt ohne Freischaltung das Passwortfeld; die Seiten selbst sind nicht von mir angesehen (Passwort).
- **Deploy 2026-10-02 12:00, Netzwerk: Migration, Bericht, Geraetelandkarte, Altlasten (`7d31df5`, davor 11:49 `c6b2d29`).** `869/869`, `BiDashboard.dll` `11:59:57`, `7'567'872` Bytes, SHA256 `78761D9CEC9ADFBAE0415D2F02718D463B08EB2E5446D4F8CB27456CCDB9223D`, bitgleich. Alarm nur `-wal`/`-shm` verschwunden und `.db` veraendert (Checkpoint beim Neustart); beide Dateien um 12:00 wieder da.
- **Deploy 2026-10-02 10:14, Netzwerk: AD-Infrastruktur, Gruppenrichtlinien, DNS, Verlauf (`50b6c4a`, enthaelt `65e3a88`).** `866/866`, `BiDashboard.dll` `10:07:27`, `7'469'568` Bytes, SHA256 `F378A76B0F683662D6839AD0D5AF156DF661B548CBCB4A29B64AF3D7D16682A4`, bitgleich, ohne Alarm. Neue Tabellen `NetworkAdMetrics`, `NetworkAdChanges`, `NetworkAdComputers`; erster Schnappschuss 02.10. Vier neue Seiten per Edge headless angesehen. Damit ist „`65e3a88` nicht deployed“ darunter ueberholt.
- **Deploy 2026-10-02 08:43, Netzwerk AD Korrekturen (`3e21220`).** `850/850`, `BiDashboard.dll` `08:33:07`, SHA256 `B6D0C0686E7BCB256C09A69537C464329CADA208A743D3A6202439AD5B9F4C5E`, bitgleich, ohne Alarm. Davor **08:29** `ce9e06a` (Klassen `dir-`), SHA256 `A7E7CCCC86F22F746A95AF4B9A702F5C176AB2C369629EEE46AD48BE533937F3`, Alarm nur `-wal`/`-shm` (wieder da). **`65e3a88` nicht deployed**: Laptop ohne VPN („Produktiv-DB fehlt“), Release-Worktree steht auf `65e3a88`, Runner vorbereitet (853/853).
- **Deploy 2026-10-02 08:07, Netzwerk: Unterreiter Active Directory (`b57d20f`).** `849/849`, `BiDashboard.dll` `08:00:11`, `7'149'056` Bytes, SHA256 `16FEE75D4F59423E48FF78C5C4B197AEEC6D011138875D454ED86A7AC9568420`, bitgleich, fuenf Routen 200, **ohne Alarm** (0 verschwunden). `/netzwerk/ad`, `/netzwerk`, `/netzwerk/sicherheit` mit Windows-Anmeldung 200, `app.css` enthaelt `ad3d-scene`. Erster Versuch brach ab, weil der Laptop ohne VPN war („Produktiv-DB fehlt“).
- **Deploy 2026-10-02 07:34, Netzwerk: AD-Computer eingeschaltet (`48121df`).** Nur `appsettings.json` geaendert (`NetworkProbe:AdEnabled = true`), `824/824`, `BiDashboard.dll` `07:34:12`, SHA256 `45CE08E916005F5339E22113FBD5171B4770A7A6B753EB3BE39B2DCD385F12BE`, bitgleich, fuenf Routen 200, Alarm nur `-wal`/`-shm`. Damit ist „`AdEnabled` aus“ im Eintrag darunter ueberholt.
- **Deploy 2026-10-02 07:20, Reiter Netzwerk (`1635b46`).** `824/824`, `BiDashboard.dll` `07:20:16`, `6'993'408` Bytes, SHA256 `D73F77B5B044F6DC3522349E6E1D4D1D8EF11335410EEFC8E19C44348DCB5A68`, bitgleich, fuenf Routen und fuenf Netzwerk-Seiten 200. Alarm nur `-wal`/`-shm`. Neuer Hintergrunddienst `NetworkProbeService` (alle 5 Min., erste Runde 07:21 alle 7 Ziele ok); `NetworkProbe:AdEnabled` aus.
- **Deploy 2026-10-01 16:05, Verwendung & Risiko (`1a754e0`).** `814/814`, `BiDashboard.dll` `16:05:31`, `6'754'816` Bytes, SHA256 `E24A59A5F954338F8022D61A64C2231F91C25FCBC360D793570892DB38CD37DA`, bitgleich, fuenf Routen und `/logistik/verwendung-risiko` 200. Alarm nur `-wal`/`-shm` (16:05:38 neu).
- **Deploy 2026-10-01 15:01, Logistik live (`2be8623`).** Stand `6b4bf21`, `810/810`, `BiDashboard.dll` `15:01:10`, `6'695'424` Bytes, SHA256 `226536E119D548B7EF04E9E4BF97586EA058BE64FC3FDA61F5FB1B68B00D82C3`, bitgleich, fuenf Routen und `/logistik/live` 200. Alarm nur `-wal`/`-shm` (um 15:01:14 neu angelegt). Vorher `T76K912650` in P76 (Ingo).
- **Deploy 2026-10-01 14:26: Logo direkt auf Orange mit „Dashboard“ (`92e012e`), Berndeutsch, Hintergrund-Dimmer, zwei abwechselnd blinkende Statuslampen (`102c20c`).** `803/803`, `BiDashboard.dll` `14:26:37`, `6'617'088` Bytes, SHA256 `F0A8C1E8C73AD42A481A48FF536DD5A07D6BE8B5A5F7ED718128E06B6B0FF069`, bitgleich, fuenf Routen 200, ohne Alarm.
- **Deploy 2026-10-01 14:14, Skin Trafag CI mit Umschalter (`7d1e5b5`).** Stand `73577b1`, gleicher Worktree, ohne Finance_All. `801/801`. `BiDashboard.dll`
  `01.10.2026 14:14:13`, `6475776` Bytes, SHA256 `8AD664AF24FD7BB8673980791002218CB3ABD4851ED843A1E89FE1C202298D94`, bitgleich. `/` und `/einkauf` 200, `app.css` mit `ci-brand` ausgeliefert.
  Sicherung `trafag_exporter.db.before-skin-trafag-ci-20261001-141308.bak`. Alarm „2 verschwunden“, diesmal im ungefilterten Protokoll belegt: `trafag_exporter.db-wal` und `-shm`; beide um 14:14:20 neu angelegt.
- **Deploy 2026-10-01 11:21, Einkauf: alter Stand waehrend der Neuberechnung (`4e2b55a`) und Texte D1/D5 (`3407868`).** Gleicher Worktree, ohne Finance_All. `801/801`.
  `BiDashboard.dll` `01.10.2026 11:21:49`, `6'471'680` Bytes, SHA256 `8E824F234C444B3D8BC8546A312133F989BBF79A94216524F0738CFC163AA135`, gegen den Release-Build nachgemessen bitgleich;
  `app_offline.htm.disabled`, `/einkauf` 200. **FALLE, selbst passiert:** im selben Befehl stand versehentlich ein zweiter `dotnet run ... DeployHeadless --no-build | head -0`. Er ist nach Lage
  beim ersten Schreiben in die geschlossene Pipe abgebrochen (keine zweite Sicherung, DLL und `app_offline` korrekt), das Protokoll des ersten Laufs ging dabei durch den `grep`-Filter verloren.
  **Den Deploy nie mit `head` oder einem zweiten Aufruf koppeln** und das Protokoll immer ungefiltert behalten.
- **Deploy 2026-10-01 10:50, HR-Cockpit: Krankheit und Ferien je Fall aus SAP `HrAbsenzSet` (`c749d32`, `c12b34a`).** Stand `b54a503`, gleicher Worktree, ohne Finance_All. `795/795`.
  `BiDashboard.dll` `01.10.2026 10:50:33`, `6'465'024` Bytes, SHA256 `DC76FE33FD67D8264BD1FB9E61A7BFBBFDF8C261D3B29C8A607FF2C34A04ADD6`, bitgleich. Fuenf Routen `200`,
  Produktiv-DB unveraendert, Sicherung `trafag_exporter.db.before-hr-sap-absenzen-20261001-104907.bak`. Ein Alarm „2 verschwunden“: nach Lage die bekannten `trafag_exporter.db-wal`/`-shm`,
  beide danach wieder vorhanden, `hrdata` vollstaendig. Erster Absenzen-Abruf 10:52 (4'948 Faelle).
- **Deploy 2026-10-01 09:44, HR-Cockpit: Unfalltage als laufender Monat beschriftet (`931f151`).** Gleicher Worktree, ohne Finance_All. `786/786`. `BiDashboard.dll` `01.10.2026 09:44:38`, `6'445'568` Bytes,
  SHA256 `605EC034C401AC3435CDAAC44F7345C09CBC452191C18CFE74685A879FA6719D`, bitgleich. Fuenf Routen `200`, Produktiv-DB unveraendert, ohne Alarm.
  Nebenwirkung des Neustarts: erster erfolgreicher `HrKpiSet`-Abruf um 09:46 (1'168 Personen).
- **Deploy 2026-10-01 09:12, HR-Cockpit: SAP-Abruf `HrKpiSet` (`4051506`), Absenz-Ampel ab 5 % (`81140fa`), Kader 8,1 h (`c51445c`).** Stand `ba9cf5c`,
  Worktree `C:TMPTrafagSalesExporter_release_eafd1c5`, ohne Finance_All. `785/785`. `BiDashboard.dll` `01.10.2026 09:12:34`, `6'444'544` Bytes,
  SHA256 `5E25C0E5A432AE981328B8B4693F509C060CBB0ED77956E0E1633CF5454F480B`, bitgleich. Fuenf Routen `200`, Produktiv-DB unveraendert,
  Sicherung `trafag_exporter.db.before-hr-sap-kader-ampel-20261001-091049.bak`. Ein Alarm, harmlos: das erwartete Literal `h je Tag), sonst aus dem Arbeitszeitmodell.`
  fehlt, weil `c51445c` diesen Hinweis selbst um „Kader 8,1h“ erweitert hat. **Der Tagesabruf laeuft produktiv, findet aber bis zum
  Transport `T76K912530` kein `HrKpiSet` in P76** und laesst die Handdatei stehen (Fehler im AppEventLog, Kategorie HR).
- **Deploy 2026-09-30 09:36, HR-Cockpit: Arbeitstag 8,0 h statt 8,4 h (`e3545c4`).** Stand `2047add`,
  gleicher Worktree `C:\TMP\TrafagSalesExporter_release_eafd1c5`, ohne Finance_All. `773/773`. `BiDashboard.dll`
  `30.09.2026 09:34:14`, `6'425'600` Bytes, SHA256 `6DDE3BB4F7D4FC2CBD9AEF6793FB9545FED39A318DB1BD6E84975231F763B369`, bitgleich.
  Fuenf Routen `200`, Sicherung `trafag_exporter.db.before-hr-workday-8h-20260930-093459.bak`, Produktiv-DB
  unveraendert. Sperrpruefung: `Sollzeit / 8.4h` und `Stunden / 8.4h` nicht mehr in der DLL. **Ohne Alarm.**

- **Deploy 2026-09-29 15:58, Gutschriften nach Belegart und Excel-Statustexte als Konstante (ISS-026/027).**
  Stand `b699fa5`, gleicher Worktree, ohne Finance_All. `772/772`. `BiDashboard.dll` `29.09.2026 15:57:11`,
  `6'425'600` Bytes, SHA256 `B0511D6D3B0A40AC7165DD2B9BE646CF9348F4716EBE8A916FCE5294E8596EE1`, bitgleich.
  Fuenf Routen `200`. Sicherung von 13:50 wiederverwendet. **Ohne Alarm.**

- **Deploy 2026-09-29 15:23, Italien: provisorischer Kundenausschluss entfernt (ISS-021 Teil 2).** Stand
  `f77840a`, gleicher Worktree, ohne Finance_All. `759/759`. `BiDashboard.dll` `29.09.2026 15:22:22`,
  `6'425'600` Bytes, SHA256 `57BD95CB4255583F3726BE76C6D3F94DB99E1A7900EDA6C47F991F7B9C5F6B21`, bitgleich.
  Fuenf Routen `200`. Sicherung von 13:50 wiederverwendet. Nur der bekannte WAL/SHM-Alarm, beide um
  15:23:11 wieder da, Hauptdatei unveraendert. Sperrpruefung: `C_IT01_0022987` nicht mehr in der DLL.
  Zusammen mit 15:02 erwartet ab dem TRIT-Export am 30.09.: Italien 2025 rund 7,70 Mio. EUR (Hauptbuch 47005).

- **Deploy 2026-09-29 15:02, B1-Abfrage nur eine Lieferanten-Rechnungsadresse (ISS-021).** Stand `d8d425f`
  (Funktionscommit `4a1baa0`), Worktree `C:TMPTrafagSalesExporter_release_eafd1c5`, ohne Finance_All.
  `757/757` Release-Tests. `BiDashboard.dll` `29.09.2026 15:01:09`, `6'425'600` Bytes, SHA256
  `18D4C063B4E50F484E2433E30FFD9B6C8E054E873B2E56A39C1298BA73151D80`, bitgleich. Fuenf Routen `200`.
  Sicherung von 13:50 wiederverwendet (DB unveraendert). **Ohne Alarm.** Wirknachweis `BillToDef`,
  `sup_adr_any`. **Wirkung erst mit dem naechsten B1-Export** (TRIT, TRFR, TRUS, TRIN, am 30.09. um 12:00):
  danach doppelte TRIT-Positionen nachmessen, erwartet Italien 2025 −3,79 Mio., 2026 −3,01 Mio. EUR.

- **Deploy 2026-09-29 13:59, Dunkelmodus: gedaempfte Kopfleiste.** Stand `a397440`, gleicher Worktree, ohne
  Finance_All. `749/749`. `BiDashboard.dll` `29.09.2026 13:58:41`, `6'424'576` Bytes, SHA256
  `729D261A0E4D0F71A9864EEA1BDCF5E6250EEFA2AD00511241B6C88DE0F817D9`, bitgleich. Fuenf Routen `200`.
  Sicherung von 13:50 wiederverwendet (DB seit 13:18:35 unveraendert). Nur der bekannte WAL/SHM-Alarm,
  beide um 13:59:37 wieder da. Anlass: Screenshot von Ingo, die Kopfleiste leuchtete im Dunkelmodus hellrot.

- **Deploy 2026-09-29 13:51, Cockpit-Dunkelmodus.** Stand `5f4eb5f`, Worktree
  `C:TMPTrafagSalesExporter_release_eafd1c5` (auf `5f4eb5f` gestellt), ohne Finance_All. `749/749` Release-Tests.
  `BiDashboard.dll` `29.09.2026 13:46:23`, `6'424'576` Bytes, SHA256
  `2A2617BB12E36535223A9B988F500B20D9A355547D2014DDA940F9CBE54952BB`, bitgleich. Fuenf Routen `200`.
  Sicherung `trafag_exporter.db.before-cockpit-darkmode-20260929-135057.bak`. Ein Alarm, der bekannte:
  WAL/SHM beim Neustart kurz weg, um 13:52:05 wieder da, Hauptdatei unveraendert (13:18:35).
  Wirknachweis `trafagTheme.set`, `Hell/Dunkel umschalten`. Nicht belegt: Sichtpruefung im Browser
  (Chrome-Erweiterung nach PC-Neustart nicht verbunden).
  **Befehlsform:** den Runner ueber Bash als `dotnet run --project .tmp_tools/DeployHeadless -c Release`
  starten. Ein PowerShell-Aufruf mit vorangestelltem `Set-Location` passt auf keine Erlaubnisregel und
  wurde im Auto-Modus zweimal abgelehnt.

- **Deploy 2026-09-29 10:22, Cockpit ISS-018.2 bis .4.** Stand `91fd9dc`, Worktree
  `C:TMPTrafagSalesExporter_release_eafd1c5` (auf `91fd9dc` gestellt), ohne Finance_All. `749/749` Release-Tests.
  `BiDashboard.dll` `29.09.2026 10:20:36`, `6'416'384` Bytes, SHA256
  `25BAFAC22774D68598FB65F9A0BDA74E2B3A521A168166D1FF5C701DC6F91A71`, bitgleich. Fuenf Routen `200`.
  Sicherung `trafag_exporter.db.before-cockpit-labels-capnote-20260929-102112.bak` (Blockkopie 38 s). Ohne Alarm.
  Wirknachweis `Dieser Export ist gekappt`. Nicht belegt: angemeldete Sichtpruefung des Cockpits.

- **Deploy 2026-09-29 10:07, HR- und Einkaufs-Review-Reparaturen von Codex.** Stand `c1fdfa2`
  (Funktionscommit `eafd1c5`, Uebersetzungen `c1fdfa2`), sauberer Worktree
  `C:TMPTrafagSalesExporter_release_eafd1c5`, ohne Finance_All. `747/747` Release-Tests; der erste Lauf
  hatte einen roten Uebersetzungstest (vier neue Texte ohne es/it/hi/sq/tr/tlh), behoben in `c1fdfa2`.
  `BiDashboard.dll` `29.09.2026 09:56:18`, `6'410'752` Bytes, SHA256
  `4782582CA860B5B1BDB7437D7F664EEDBB16D76DFF145594941675890FD21371`, bitgleich. Fuenf Routen `200`.
  Sicherung `trafag_exporter.db.before-hr-purchasing-review-20260929-095654.bak` (die Kopie ueber das
  Netz dauerte rund zehn Minuten, die Anwendung blieb online). Ohne Alarm. Nicht belegt: Sichtpruefung
  der HR- und Einkaufsseiten angemeldet.
- **Nachweis ISS-020 (Deploy 09:25):** Nach dem Spanien-Import enthaelt `Sales_All_2026-09-29.xlsx`
  8'109 TRES-Zeilen (vorher 6'772) mit Rechnungsdatum bis 28.09.2026; August 314, September 486 Zeilen.

- **Deploy 2026-09-29 09:25, SharePoint-Ordnerliste mit Folgeseiten (ISS-020, Spanien-Import).** Stand
  `60cc569`, sauberer Worktree `C:TMPTrafagSalesExporter_release_60cc569`, ohne Finance_All und ohne
  die laufenden HR/Einkauf-Reparaturen von Codex. `741/741` Release-Tests (vier neue Paging-Tests).
  `BiDashboard.dll` `29.09.2026 09:22:58`, `6'407'680` Bytes, SHA256
  `4000AAE5747FA369D8A87891D427085CB2D0A7FBCF0884A0C06E3F7B7D55B327`, bitgleich. Fuenf Routen `200`.
  Sicherung `trafag_exporter.db.before-sharepoint-paging-20260929-092417.bak`. Produktiv-DB in Laenge
  und Schreibzeit unveraendert. Wirknachweis `SharePoint-Ordner liefert mehr als` in der DLL. Ohne Alarm.
  **Noch nicht belegt:** dass der naechste Spanien-Import die Dateien ab dem 16.07. einliest; das zeigt
  erst der naechste Lauf.

- **Deploy 2026-09-28 13:50, Finanzvergleich-Cache und Einkauf-Status eine Zeile je Lauf.** Stand
  `02bd0cd` (Funktionscommit `eaa9d43`), sauberer Worktree `C:\TMP\TrafagSalesExporter_release_02bd0cd`,
  ohne Finance_All. `737/737` Release-Tests. `BiDashboard.dll` `28.09.2026 13:49:47`, `6'403'072`
  Bytes, SHA256 `ADFE606DBC1490C3BB596484C151EEDDA55E5DDBBD8FB8BC4948F09FD5C71980`, bitgleich. Fuenf
  Routen `200`. Sicherung wiederverwendet (`...before-cockpit-sqlite-cache-20260928-133159.bak`).
  **Ein Alarm, erklaert:** Hauptdatei mit neuer Schreibzeit (13:50:20, gleiche Groesse), WAL/SHM kurz
  verschwunden und um 13:50:29 wieder da. Ursache: beim Start um 13:33 setzte der
  `PurchasingRefreshRunner` die liegengebliebene Running-Zeile Id 48 (der alte Doppelzeilen-Fehler)
  auf „Abgebrochen", Warnung im stdout-Protokoll; diese 16 KB WAL wurden beim Herunterfahren in die
  Hauptdatei zurueckgeschrieben. Die Sicherung von 13:31 enthaelt diese eine Statusaenderung nicht.
  Nachgeprueft nur lesend: `PRAGMA quick_check` = `ok`, Id 48 steht auf `Abgebrochen` mit Zeitpunkt
  13:33:25. Ab diesem Stand schreibt ein Einkauf-Lauf nur noch eine Zeile, der Fall entfaellt kuenftig.

- **Deploy 2026-09-28 13:32, Tempo: Cockpit-Wechselkurse, Export-Dashboard, SQLite-Cache,
  CSV-Einlesen.** Stand `744faaf` (Funktionscommits `5a26596`, `c35d3fa`, `977a198`), sauberer
  Worktree `C:\TMP\TrafagSalesExporter_release_744faaf`, ohne Finance_All. Per Timer auf Ingos Wunsch
  erst nach dem Einkauf-Delta, das um 13:19 erfolgreich endete. `734/734` Release-Tests.
  `BiDashboard.dll` `28.09.2026 12:59:52`, `6'400'000` Bytes, SHA256
  `F7D31941AC32AF1756D693F5C036A997E711273D28D4FB0673779F36D0A44AB4`, bitgleich. **Ohne Alarm.**
  Neue Sicherung `trafag_exporter.db.before-cockpit-sqlite-cache-20260928-133159.bak`
  (`integrity_check` ok). Wirknachweis: `NotifyRatesChanged`, `GetSourceStampAsync`,
  `PRAGMA mmap_size`. Fuenf Routen `200`.
  **Befund Wachhalten:** der Worker lief vom Deploy um 11:25 bis zu diesem Deploy ohne Neustart,
  der 12:00-Lauf startete regulaer (12:00:16), das Einkauf-Delta um 12:32:41 und endete 13:19.
  **Befund SQLite-Cache:** erste Vorberechnung nach dem Deploy 24,4 s statt 86,9 bis 104,7 s.
  Details `docs/PLATTFORM_TEMPO_2026-09-28.md` Abschnitt 9.

- **Deploy 2026-09-28 11:25, HR-Vorgaben und Einkauf sofort liefern und vorwaermen.** Stand
  `4aa27ae` (Funktionscommits `5ae7f31` HR, `d32a6aa` Einkauf-Cache und Vorwaermen), sauberer
  Worktree `C:\TMP\TrafagSalesExporter_release_4aa27ae`, ohne Finance_All. `727/727`
  Release-Tests. `BiDashboard.dll` `28.09.2026 11:25:07`, `6'393'344` Bytes, SHA256
  `83595EA62341F06FAF024BA124CC2C155FE1B153685D5F8B4E3F995A44A3B9DD`, bitgleich. **Ohne Alarm.**
  Sicherung wiederverwendet (DB seit 09:43 unveraendert). Wirknachweis: `Fluktuation Prognose
  gleitend`, `Einkauf-Standardansicht vorberechnet in {Seconds:N1} s`. Fuenf Routen `200`. Auf
  Ingos Wunsch vor dem 12:00-Lauf deployed; der Worker muss damit ab 11:25 ohne Neustart bis
  zum Delta um etwa 12:30 durchhalten.

- **Deploy 2026-09-28 10:31, Oberflaeche sofort sichtbar: Vorrendern aus, Ladebalken.** Stand
  `4f2c62f`, sauberer Worktree `C:\TMP\TrafagSalesExporter_release_4f2c62f`, ohne Finance_All.
  `715/715` Release-Tests. `BiDashboard.dll` `28.09.2026 10:30:54`, `6'384'128` Bytes, SHA256
  `CD57F6EDD4777FBF07D5CD932B5D77DF12ECF883B24D1C9E9E159B9038C4C398`, bitgleich. Sicherung
  wiederverwendet (`...before-purchasing-catchup-keepalive-20260928-100353.bak`, DB seither
  unveraendert). Bekannter WAL/SHM-Alarm, beide um 10:32:06 wieder da. **Ab diesem Deploy
  liefern alle Routen nur noch die Huelle (rund 4,7 KB)**: die Groessen im Protokoll fallen
  deshalb stark, und ein HTML-Grep nach Seiteninhalt funktioniert nicht mehr, weil der Inhalt
  erst im Circuit entsteht. `/einkauf` antwortet in `0,11 s` statt `98,60 s` vorher. Das
  Beobachtungsfenster fuer das Wachhalten (ISS-017) beginnt neu um 10:31. Details:
  `docs/PLATTFORM_TEMPO_2026-09-28.md`.

- **Deploy 2026-09-28 10:04, Einkauf-Lauf nachholen und IIS-Worker wachhalten.** Stand `e83591f`,
  gebaut aus dem sauberen Worktree `C:\TMP\TrafagSalesExporter_release_e83591f`, wieder **ohne**
  die unfertigen Finance_All-Aenderungen (Sperrpruefung `FinanceAllExportService` negativ).
  `715/715` Release-Tests. `BiDashboard.dll` `28.09.2026 10:02:20`, `6'380'544` Bytes, SHA256
  `CCBA7C3340DD567D4A976E834F92B12219C1CA16E8F61F329224D8BDBA30BBC2`, bitgleich. Ziel: 0 neu,
  37 geaendert, 2337 unveraendert, 2 verschwunden. Sicherung
  `trafag_exporter.db.before-purchasing-catchup-keepalive-20260928-100353.bak` (`integrity_check` ok).
  Wirknachweis: `Einkauf-Delta nachgeholt`, `BiDashboard/favicon.svg` plus alle Texte des Deploys von
  09:33. Fuenf Routen `200`. **Der eine Alarm ist der bekannte:** `trafag_exporter.db-wal` (`0` Bytes)
  und `-shm` verschwanden beim Neustart und waren um 10:04:56 wieder da; die Hauptdatei blieb
  unveraendert. **Wirkung noch nicht gemessen:** ob der Selbstaufruf den Worker haelt, zeigt sich
  daran, dass die mehrfachen Neustarts pro Tag in `logs/stdout_*.log` ausbleiben und der
  12:00-Lauf samt Einkauf-Delta durchlaeuft. Hintergrund: ISS-017,
  `docs/EINKAUF_LAGERWERT_2026-08-18.md` Abschnitt 13.6.

- **Deploy 2026-09-28 09:33, Lagerwert-Verlauf je Woche, Marktsegment-Vorschlaege, CH/AT-Journal
  monatsweise.** Stand `7cb7f20`, gebaut aus dem **sauberen Worktree**
  `C:\TMP\TrafagSalesExporter_release_7cb7f20`, weil der Arbeitsbaum unkommittierte, unfertige
  Finance_All-Aenderungen enthaelt (`Program.cs`, `TimerBackgroundService.cs`, `.csproj`, zwei neue
  Services). Ausgeliefert: `29c0f87`/`8b939e2` (Lagerwert-Verlauf), `4d0b9cd` (Marktsegment-Vorschlaege),
  `16bc901`/`caaf377` (CH/AT-Journal monatsweise). `709/709` Release-Tests gruen im Worktree.
  `BiDashboard.dll` `28.09.2026 09:28:51`, `6'372'864` Bytes, SHA256
  `326E0750B761FC166A5B0F88FCE1A27ECD052BB904D0B37FAF3AA986C4750C05`; lokaler Release-Build und Server
  bitgleich. Ziel: 4 neu, 32 geaendert, 2335 unveraendert, 0 verschwunden. **Ohne Alarm.**
  Vorher-Sicherung `trafag_exporter.db.before-stockvalue-history-20260928-093218.bak` (gepruefte
  Blockkopie, `integrity_check` ok); Produktiv-DB in Laenge und Schreibzeit unveraendert
  (`471'834'624` Bytes, `25.09.2026 13:15:16`). Wirknachweis in der DLL:
  `PurchasingStockValueHistory`, `Erster Verlaufspunkt:`, `UX_SegmentNamePatterns_Pattern`,
  `vor dem Startdatum verworfen=`, `de_customer_invoice_map_2026-09-10.json`; **Sperrpruefung
  `FinanceAllExportService` nicht enthalten.** Fuenf Routen HTTPS `200`; `/einkauf` brauchte beim
  ersten Aufruf `100.50 s` (Kaltstart). Die gerenderte Seite `/einkauf` zeigt das neue Verlaufspanel
  mit einem Punkt. **Befund dabei:** Kachel und erster Verlaufspunkt tragen den Stand
  `10.09.2026 13:19` — der Lagerwert ist seit 18 Tagen nicht neu gelesen worden, siehe
  `docs/EINKAUF_LAGERWERT_2026-08-18.md` Abschnitt 13.5. Browserpruefung nicht moeglich
  (Chrome-Erweiterung nicht verbunden), geprueft wurde das vorgerenderte HTML.
 . "- **Deploy 2026-09-10 09:40, Einkaufsbeschriftungen und SAP-Feldherkunft.** Funktionscommit `d5bc321`
  von Codex, ausgeliefert von Claude, weil Codex ohne Guthaben war. **691/691** Release-Tests gruen
  im eigenen Lauf vor dem Publish. `BiDashboard.dll` `10.09.2026 09:20:22`, `6'229'504` Bytes,
  SHA256 `8E84FA80BD8FF7880FB5199A84DFC6798ABE6EC4BBEA2F9146A44A6BA40A7CBF`; lokaler
  Release-Build und Server bitgleich. Ziel: 0 neu, 5 geaendert, 2108 unveraendert, 0 verschwunden.
  **Ohne Alarm.** Produktiv-DB in Laenge und Schreibzeit unveraendert (`469'819'392` Bytes).
  Wirknachweis in der DLL: `Aktive Lieferanten im Zeitraum`, `Bestellpositionen mit Kontraktbezug`,
  und weiterhin `de_customer_invoice_map_2026-09-10.json` aus dem Deploy von 08:53.
  Fuenf Routen HTTPS `200`, darunter `/einkauf/aufriss`.
  **Zwei Befunde:** `/einkauf/aufriss` brauchte `79.29 s` beim ersten Aufruf nach dem Neustart,
  deutlich mehr als die uebrigen Routen; das ist Kaltstart der schwersten Seite und gehoert
  beobachtet. Und Codex' letzte Ersetzung von drei Statusbeschriftungen (`Gebuchter Spend`
  statt `Bestellwert im Zeitraum`) war **nicht committet** und ist deshalb NICHT ausgeliefert;
  der alte Text steht weiterhin an vier Stellen in `PurchasingDashboard.razor`.
" . "- **Deploy 2026-09-10 08:53, Journal-Ausgleichsfelder und erweiterte DE-Belegbruecke.** Funktionscommits
  `9ffe7af`/`8905e5d`/`8208c62` (Ausgleichsfelder) und `ee46d18` (Bruecke); Stand beim Publish `1a977ff`.
  **691/691** Release-Tests gruen im eigenen Lauf vor dem Publish. `BiDashboard.dll` `10.09.2026 08:53:05`,
  `6'229'504` Bytes, SHA256 `63BC290443DCF6B7A31B9D36779048E76106010EA86115968D0D82BD47FB221D`;
  lokaler Release-Build und Server bitgleich. Ziel: 1 neu, 7 geaendert, 2102 unveraendert.
  Wirknachweis in der DLL: `ClearingDate`, `ReconciliationDate`, `ClearingReference`,
  `IsClearingCancelled`, `de_customer_invoice_map_2026-09-10.json`; **nicht mehr enthalten**
  `de_customer_invoice_map_2026-09-09.json`. Vier Routen HTTPS `200`.
  **Der eine Alarm ist erklaert:** `trafag_exporter.db-wal` und `-shm` verschwanden und die DB
  wechselte den Zeitstempel bei gleicher Laenge — das ist der Checkpoint beim App-Neustart,
  derselbe Befund wie am 2026-08-21 und 2026-09-08. Beide Dateien sind danach wieder da.
  **FALLE, dabei selbst hineingelaufen:** eine Pruefkopie nur der `.db` ohne `-wal` zeigte die
  fuenf neuen Spalten als FEHLEND. Erst die Kopie aus `.db`, `-wal` und `-shm` zusammen zeigte
  `34` statt `29` Spalten. Bei WAL-Modus immer alle drei Dateien kopieren, sonst fehlt der
  gerade migrierte Stand.
  Datenstand nach dem Deploy gegengeprueft: TRDE `7'622` Zeilen, `7'592` mit fachlicher Nummer,
  `24` Segmente, `SalesPriceValue` `7'033'623.00`, `quick_check` `ok`.
  Vorher-Sicherungen `trafag_exporter.db.before-de-country-fix-20260910-080942.bak` und die vom
  Deploywerkzeug angelegte `before-journal-clearing-und-de-bruecke`.
- **Datenkorrektur 2026-09-10 08:52, Laendercode in den DE-Verkaufszeilen.** `Tools/DeCountryFix`
  hat `3'037` Zeilen von Laendertext auf Code umgestellt (`Deutschland` auf `D`, `China` auf `PRC`
  und so weiter). Grund war ein Fehler im Nachzug vom Vortag: dort wurde Spalte 5 des
  Kundenstamms gelesen statt Spalte 4. Danach `D` `7'127`, `PRC` `182`, `CZ` `65`, `SGP` `60`.
  **Nicht angefasst:** 15 Zeilen des Kunden `55011` mit `US` statt `USA`; sie stammen aus einem
  aelteren Weg, nicht aus dem Nachzug. Das Werkzeug meldet sie und laesst sie stehen.
  **Betriebsbefund:** 3'037 Einzelupdates ueber SMB brauchten rund 25 Minuten; die Anwendung
  blieb waehrenddessen ansprechbar (`200` in `1.02 s`).
"- **Datennachzug 09.09.2026 14:37, DE-Kundenfelder auf Rohails Belegbruecke.** Kein Deploy,
  reine Datenaenderung bei laufender Anwendung. `Tools/DeCustomerBackfill` schrieb 3'043 von
  7'622 TRDE-Zeilen in einer Transaktion: fachliche Kundennummer, Name, Land und Branche.
  Fachliche Nummern von `4'549` auf `7'592`, bestaetigte Segmente von `19` auf `24`,
  `quick_check` `ok`, Zeilenzahl und `SalesPriceValue` mit `7'033'623.00` unveraendert.
  Vorher-Sicherung `trafag_exporter.db.before-de-customer-backfill-20260909-143601.bak`
  (`458'653'696` Bytes). Danach `Tools/DeCustomerBackfillFiles` fuer die veroeffentlichten
  Dateien: `Sales_ProcessedMergeInput_TRDE_2026-09-09.csv` SHA256
  `982F03E118175A7EB9DBB56CA54BE9E5C689B0D4DE106C58035F97CAFE2BF392` und
  `Sales_TRDE_2026-09-09.xlsx` SHA256
  `7B60972CCE6C1CC7FF601B3F46FD5C7BC8F934569F0799B1C6902510A6DC468C`, im Serverordner
  `output` bytegleich zurueckgelesen und ueber `Publish --ersetzen` auch auf SharePoint.
  **Nachweis der Unversehrtheit:** in der CSV null Zellen ausserhalb der vier Kundenspalten,
  in der Excel null fremde Zellen und null Formeln veraendert. Vier Routen HTTPS `200`.
  **Betriebsfalle bestaetigt:** ein Aufruf ohne Windows-Anmeldung liefert `401`, nicht `200`;
  `Invoke-WebRequest` braucht `-UseDefaultCredentials`. **Zweite Falle:** der Produktivstand
  hatte sich seit der Analyse bewegt (Standortexport um 13:07, 7'622 statt 7'615 Zeilen),
  deshalb wurde der Vorher-Snapshot unmittelbar vor dem Schreiben neu genommen. Detail:
  `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md` Abschnitt 9.
- Neuester verifizierter Deploy: **09.09.2026 08:43, RTF-Schrift- und Farbtabelle aus der
  Alphaplan-Artikelbezeichnung**, Funktionscommit `eb8cd43` (Stand beim Publish: `5363005`),
  **688/688** Release-Tests gruen im eigenen Lauf vor dem Publish. `BiDashboard.dll`
  `09.09.2026 08:43:53`, `6'404'096` Bytes, SHA256
  `A26514ADDEAC747F78F1204F8611B90BA81C9798D28FD269831029FEAC9B835C`; lokaler Release-Build
  und Server bitgleich. Ziel: 0 Dateien neu, 4 geaendert, 2061 unveraendert, 0 verschwunden.
  `app_offline.htm` gesetzt und danach auf `.disabled` umbenannt. Produktiv-DB in Laenge und
  Schreibzeit unveraendert (`458'055'680` Bytes, `09.09.2026 07:23:16`).

  **Routen:** Startseite `71'383` Bytes in `7.16 s` (Kaltstart), `/management-cockpit`
  `0.08 s`, `/marktsegmente` `13.36 s`, `/standorte` `0.03 s`, alle HTTPS `200`.
  **Wirknachweis in der DLL:** `RemoveRtfDiscardedGroups`, `RtfDiscardedDestinations`,
  `StartsRtfDiscardedGroup`. Das Arbeitsverzeichnis war beim Deploy nicht sauber, Branch
  `feature/supplier-overrides-trit`.

  **Der Deploy allein wirkt nur auf kuenftige DE-Importe.** Der Nachzug des Bestands lief
  im Anschluss am selben Tag, nach einem VPN-Ausfall zwischendurch: `3'089` von `7'615`
  TRDE-Zeilen, ausschliesslich die Spalte `Name`, in einer Transaktion. Vorher-/Nachher-
  Snapshot unter Schreibsperre, `quick_check` `ok`, identische Zeilenmenge, **null
  Abweichungen in den uebrigen 50 Spalten**, `SalesPriceValue` unveraendert bei
  `7'029'335.84`, Segmente unveraendert bei 191, kein Schriftrest mehr in der ganzen
  Tabelle. Anschliessend wurden `Sales_ProcessedMergeInput_TRDE_2026-09-09.csv` und
  `Sales_TRDE_2026-09-09.xlsx` im Serverordner `output` und im SharePoint-Ordner ersetzt
  und zurueckgelesen; nur Namensfelder weichen ab. **Betriebsfalle:** ein Lesezugriff im
  Nur-Lese-Modus scheitert ueber SMB mit `SQLite Error 14`, deshalb Probelauf gegen eine
  konsistente lokale Kopie und erst die Anwendung gegen den Produktivpfad. Details und
  Hashes: `docs/STANDORT_DE_ALPHAPLAN.md` Abschnitt 8.

- Vorheriger Deploy: **09.09.2026 07:20, DE-Belegbruecke und reine
  Railway-Branchen**, Commit `8a0cee6`, **687/687** Release-Tests. Ohne Alarm,
  vier Routen HTTP 200. DLL `6'403'584` Bytes, SHA256
  `0257C3DF25410FC2D1D09CDA8582E0E996C32B0A6BE207287799255DF8E53EE3`, lokal/Server
  bitgleich. Wirknachweise: `GermanCustomerInvoiceMapping`, `ALPHAPLAN-ID:`,
  eingebettete JSON-Belegbruecke. Deploy selbst ohne DB-Aenderung; anschliessend
  autorisierter Kunden-Nachzug: 4'549 direkte Belegzeilen und 19 bestaetigte
  DE-Railway-Kunden. 3'066 Zeilen bleiben offen, Finanzwerte unveraendert.
  Dashboard-CSV und Sales-Excel auf Server und SharePoint geprueft. Vollnachweis:
  `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md`, Abschnitt 8.

- Aktuellster produktiv verifizierter Deploy: **2026-09-08 23:51, DE-Kundenfelder und
  Railway-Zuordnung aus Alphaplan-Branche**, Funktionscommit `872fca9`, **680/680**
  Release-Tests gruen. Vorhandene unveraenderte Sicherung
  `trafag_exporter.db.before-ch-journal-gateway-20260908-151137.bak` wiederverwendet;
  Hauptdatenbank unveraendert bei `457'355'264` Bytes / `08.09.2026 14:30:04`.
  `BiDashboard.dll` `4'912'640` Bytes, SHA256
  `6DD72BAD51B2985D083832A245912BCBBEC3B61581711A4C03B796C2469F4B51`; Server und
  lokaler Release-Build bitgleich. HTTPS `200`: Startseite, `/management-cockpit`,
  `/marktsegmente` und `/standorte`. Wirknachweis: `IsGermanRailwayIndustry`,
  `Alphaplan Kundenstamm / Branche`, `DE-Bahnzuordnungen`.

  Die Deploy-Konsole meldete zunaechst einen Alarm, weil die fluechtigen Dateien
  `trafag_exporter.db-wal` und `-shm` beim App-Neustart verschwanden. Direkte Nachpruefung
  nach dem Start: beide wieder vorhanden, WAL `0` Bytes; Hauptdatenbank in Laenge und
  Schreibzeit unveraendert. Das war der normale SQLite-Neustart, kein Datenverlust.
  Produktivdaten wurden nicht neu geladen: Der SharePoint-Rohkopf vom 08.09.2026 liefert
  weiterhin nur `RechnungsAdressenID`. Die neue Logik wirkt erst nach einem angereicherten
  Alphaplan-Export mit fachlicher Kundennummer, Name, Land und Branche.

- Aktuellster produktiv verifizierter Deploy: **2026-09-08 15:12, CH/AT Journal:
  konfigurierbares EntitySet, Pflichtfeldpruefung und `Faedt`**, Funktionscommit
  `7b1f163`, **677/677** Release-Tests gruen. Neue gepruefte Blockkopie
  `trafag_exporter.db.before-ch-journal-gateway-20260908-151137.bak` (`457'355'264`
  Bytes, `integrity_check` `ok`). `BiDashboard.dll` `08.09.2026 15:12:58`,
  `4'904'448` Bytes, SHA256
  `CEE1FA97F8FD49540FC6ADE3A646D34ED65EDA871BDD7036B4BC42D0A22EEE89`; lokaler
  Release-Build und Server bitgleich. Ziel: 0 Dateien neu, 4 geaendert, 2053
  unveraendert, 0 verschwunden; Produktiv-DB in Laenge und Schreibzeit unveraendert.
  `app_offline.htm` gesetzt und danach auf `.disabled` umbenannt.

  **Routen:** Startseite `6.99 s`, `/management-cockpit` `0.08 s`,
  `/finance-journal-import` `30.20 s`, `/standorte` `0.04 s`, alle HTTPS `200`.
  **Wirknachweis in der DLL:** `Faedt`, `Journal-EntitySet ungeeignet`,
  `DefaultJournalEntitySet`; alle drei fehlten im Server-Binary vor dem Deploy.
  `FinanzdataSchweizOeSet` wurde bewusst nicht konfiguriert: Live-$metadata belegt
  Verkaufs-/Fakturadaten statt Hauptbuch. CH/AT bleibt bis zur SAP-Bereitstellung des
  in `docs/FINANCE_JOURNAL.md` spezifizierten Volljournal-EntitySets ohne Datenlauf.

- Aktuellster produktiv verifizierter Deploy: **2026-09-08 14:02, Journal:
  Faelligkeitsdatum aus `JDT1.DueDate` lesen**, Funktionscommit `646a998`, **675/675**
  Release-Tests gruen (eigener Lauf vor dem Publish, 23 s). Gepruefte Blockkopie
  `trafag_exporter.db.before-journal-duedate-20260908-135738.bak`. `BiDashboard.dll`
  `08.09.2026 13:59:47`, `4'902'400` Bytes, SHA256
  `B6315BCF7E16D5D09269D28CDF4110F1A82AEC7A6002008C26C806EBA0E29BC7`; lokaler
  Release-Build und Server bitgleich. Ziel: 1 Datei neu, 5 geaendert, 2048
  unveraendert, 0 verschwunden. `app_offline.htm` gesetzt und danach wieder
  deaktiviert.

  **Routen:** Startseite `71'378` Bytes in `5.61 s` (Kaltstart), `/management-cockpit`
  `0.10 s`, `/finance-journal-import` `0.32 s`, `/standorte` `0.02 s`, alle HTTPS `200`.
  **Wirknachweis in der DLL:** `DueDate`, `due_date`, `FinancialJournalEntries`.

  **Was dieser Deploy noch nicht bedeutet:** Das Feld ist erst gefuellt, wenn die
  Gesellschaften erneut geladen werden. Der Schemadienst ergaenzt die Spalte beim
  Start; bestehende Zeilen bleiben bis zum naechsten Import leer. Der
  SAP-Gateway-Leser fuer CH/AT war in diesem Stand **nicht** mit angepasst. Dieser
  historische Restpunkt wurde mit dem Deploy von 15:12 direkt darueber erledigt.

- Vorheriger Deploy: **2026-09-03 08:17, Einkauf-Performance **2026-09-03 08:17, Einkauf-Performance
  und NuGet-Sicherheitsupdates**, Funktionscommits `756e931` und `2444731`, **674/674**
  Release-Tests gruen. Neue gepruefte Blockkopie
  `trafag_exporter.db.before-purchasing-cache-security-20260903-081528.bak`
  (`358'772'736` Bytes, `integrity_check` `ok`). `BiDashboard.dll` `03.09.2026 08:16:52`,
  `4'901'888` Bytes, SHA256
  `65F4CD90BC03C49496025BB6659FD62DBA42FAAC1975685A12A68691045A89D7`.
  Lokaler Referenz-Publish und Server sind fuer Haupt-DLL, `deps.json`, BCL, Kiota und
  native SQLite-Datei bitgleich; `app_offline.htm` fehlt und die Anwendung ist online.

  **Routen:** Startseite und Management-Cockpit sowie alle 15 Einkaufsrouten lieferten
  HTTPS `200`. Die einmalige Kaltberechnung nach Neustart brauchte `83.17 s`; die folgenden
  14 Einkaufsrouten lagen bei `0.05-0.14 s`. Vorher berechnete jede Unterseite denselben
  Snapshot erneut und brauchte warm rund `9-11 s`. Der neue filterabhaengige Cache gilt
  15 Minuten, arbeitet per Single-Flight und wird nach erfolgreichen Full-/Delta-Ladungen
  invalidiert.

  **Sicherheit:** Der Audit meldet in allen sieben Solution-Projekten keine anfaelligen
  Pakete mehr. Produktiv aufgeloest sind `Microsoft.Bcl.Memory 9.0.14`,
  `Microsoft.Kiota.Abstractions 1.22.0` und SQLitePCLRaw `2.1.12`; die direkte Negotiate-
  Referenz und DLL sind entfernt. SQLite-Smoke (`3.53.3`), echte SharePoint/Graph-Verbindung
  und Windows-Authentifizierung sind gruen.

  **Erster Publish vom 2026-09-02:** Der sichere Ablauf erkannte vor Abschluss, dass die
  Artikelpreistrend-Abfrage unter der aktualisierten SQLite-Version in das 120-Sekunden-
  Proxy-Timeout lief. Die Anwendung blieb online; `AS MATERIALIZED` stabilisierte den
  Query-Plan in Commit `2444731`. Erst der oben dokumentierte zweite Publish gilt als
  erfolgreicher Abschluss. Die nach dem Publish zunaechst fehlschlagende Zusatzpruefung
  verglich `deps.json` mit dem Build- statt mit dem Publish-Artefakt; der anschliessende
  Vergleich gegen einen frischen Referenz-Publish belegte alle fuenf Dateien bitgleich.
  Details: `docs/NUGET_SICHERHEIT_2026-09-01.md` und
  `docs/PURCHASING_DASHBOARD_2026-06-05.md`.

- Aktuellster produktiv verifizierter Deploy: **2026-09-02 10:59, Finance: zwei
  Code-gegen-Doku-Reparaturen**, Funktionscommit `8ae972f`, `670/670` Release-Tests gruen
  vor dem Publish. Neue gepruefte Blockkopie
  `trafag_exporter.db.before-swiss-stprs-guard-20260902-105839.bak`. `BiDashboard.dll`
  `02.09.2026 10:59:58`, `4'894'208` Bytes, SHA256
  `D9B680010D4C9618C931F32EAF86B45755FA807E7304FF1EED700938DFA8CFF8`; lokaler
  Release-Build und Server bitgleich. Ziel: `0` neu, `5` geaendert, `1'884` unveraendert.
  HTTPS `200`: Startseite (`71'378` Bytes), `/settings` (`72'241` Bytes),
  `/management-cockpit` (`72'304` Bytes). Werkzeug:
  `.tmp_tools/DeploySwissStprsGuard0902`.

  **Inhalt:** (M2) `GroupMarginCalculator` — der Schweizer-STPRS-Schalter greift nur noch,
  wenn eine liefernde Konzerngesellschaft erkannt wurde (`deliveringEntity is not null`);
  vorher setzte er auch bei `Extern`, `Lokal` und `Unklar` auf `TR_AG` und erfand damit
  einen Konzernbezug. (M3) `ExcelExportService` — Rueckfall ohne Datenbank geht ueber
  `GroupMarginCostCurrencyConverter.NormalizeMode(null)` statt hart auf `Mask`.

  **NACHWEIS BEWUSST OHNE LITERALPRUEFUNG.** Beide Aenderungen fuehren keine neue
  Zeichenkette ein, es sind eine Bedingung und ein Methodenaufruf. Der sonst uebliche
  Nachweis „Literal vorher NEIN, nachher JA" ist hier strukturell unmoeglich und wurde
  nicht vorgetaeuscht. Stattdessen dreifach binaer belegt: Server-DLL vorher
  `A029CB98...` (entsprach exakt dem dokumentierten Stand vom 2026-09-01 13:40), nachher
  `D9B68001...`, und nachher **bitgleich** zum lokalen Release-Build. Das Werkzeug prueft
  den Vorher-Hash gegen den dokumentierten Stand und meldet, wenn zwischenzeitlich jemand
  anders deployed hat.

  **PRODUKTIVE WIRKUNG: KEINE, und das ist beabsichtigt.** Read-only nachgemessen:
  `InternalSupplierCostSourceMode = DeliveringEntityCosts`, `SupplierFallbackMode =
  ChPlantMaster`, `GroupMarginCostCurrencyMode = Convert` — alle unveraendert. Die
  Korrektur wirkt erst, wenn Andreas den Schalter umstellt. Kein Schemawechsel, keine
  Migration.

  Der eine Alarm war die bekannte WAL-Falle und ist nachgemessen entkraeftet:
  `-wal` und `-shm` sind um `11:00:02` wieder da, die Hauptdatei ist in Laenge und
  Schreibzeit unveraendert (`358'440'960` Bytes, `01.09.2026 13:48:19`).
  `app_offline.htm` ist auf `app_offline.htm.disabled` zurueckbenannt. Offen bleibt der
  angemeldete Sichtprueflauf in `Admin Bereich > Settings`; sichtbar aendert sich dort
  aber nichts, weil nur die Wirkung des Schalters korrigiert wurde und nicht sein Text.

- Aktuellster produktiv verifizierter Deploy: **2026-09-01 13:40, Marktsegmente:
  vollstaendige Standardliste aus der Vertriebsvorgabe**, Funktionscommit `275fe95`,
  `668/668` Release-Tests gruen vor dem Publish. Neue gepruefte Blockkopie
  `trafag_exporter.db.before-market-segment-list-20260901-133820.bak` (`358'440'960`
  Bytes, `integrity_check` `ok`). `BiDashboard.dll` `01.09.2026 13:40:29`,
  `4'894'208` Bytes, SHA256
  `A029CB980D5EB3B4A89143874C69872EB8678A35717FEADC610D2F88EF530333`; lokaler
  Release-Build und Server bitgleich. Ziel: `0` neu, `5` geaendert, `1'877`
  unveraendert. HTTPS `200`: Startseite (`71'378` Bytes), `/marktsegmente` (`79'254`
  Bytes), `/management-cockpit` (`72'259` Bytes). Die neun vorab fehlenden Literale
  `Calibration services`, `Food & Beverage`, `General Industry`, `Large Engines`,
  `Power Distribution`, `Water Treatment`, `Automotive (MAG)`, `E-Bikes (MAG)` und
  `Robotics (MAG)` sind danach in der Server-DLL vorhanden; `Ship Building` und
  `Mobile Hydraulics` sind entfernt. Die sechs weiteren Sollwerte waren bereits im
  produktiven Binary enthalten. **Kein Schemawechsel, keine Migration und keine
  Kundenzuordnung geaendert:** die Aenderung betrifft ausschliesslich die angebotene
  Standardauswahl. Der eine Werkzeug-Alarm war die bekannte WAL-Falle und ist direkt
  nach dem Neustart entkraeftet: `trafag_exporter.db-wal` und `-shm` sind wieder da,
  die Hauptdatei blieb in Laenge und Schreibzeit unveraendert (`358'440'960` Bytes,
  `01.09.2026 13:16:14`). `app_offline.htm` ist entfernt. Werkzeug:
  `.tmp_tools/DeployMarketSegmentList0901`. Offen bleibt der visuelle Sichtprueflauf
  der aufgeklappten Auswahlliste im Browser.

- Aktuellster produktiv verifizierter Deploy: **2026-09-01 09:21, Marktsegmente: Excel-Export
  der Zuordnungen per Mausklick**, `668/668` Release-Tests gruen vor dem Publish. Neue gepruefte
  Blockkopie `trafag_exporter.db.before-market-segment-export-20260901-092034.bak`
  (`357'822'464` Bytes, `integrity_check` `ok`, `81,0 s`: Lesen `39,0 s`, Uebertragen `37,7 s`).
  `BiDashboard.dll` `01.09.2026 09:03:21`, `4'893'696` Bytes, SHA256
  `06E904A6F3821F31CB4165AB1354F179FC38532D49EB273FD775F86147781E8D`, lokaler Release-Build und
  Server bitgleich. Ziel: `0` neu, `5` geaendert, `1'846` unveraendert. HTTPS `200`: Startseite
  (`71'418` Bytes), `/marktsegmente` (`71'453` Bytes), `/management-cockpit` (`72'314` Bytes).
  Die Segmentseite waechst von `70'914` auf `71'453` Bytes, was zum neuen Knopf passt.
  Wirknachweis in der DLL: `MarketSegmentExportService`, `Verworfen (Protokoll)`,
  `Umsatz_Summen`, `Breites Sortiment, bitte einzeln pruefen`, `Marktsegmente_Export_` und die
  tuerkische Uebersetzung `Dışa aktar (Excel)`; alle sechs fehlten im Prueflauf nachweislich und
  waren vorab per `git grep` gegen `d346b32` als neu bestaetigt. Nichts entfernt, deshalb kein
  verbotenes Literal. **Kein Schemawechsel und keine Migration:** read-only nachgemessen hat
  `CustomerMarketSegments` unveraendert neun Spalten (`Id`, `Tsc`, `CustomerNumber`,
  `CustomerName`, `Segment`, `Source`, `UpdatedAtUtc`, `IsConfirmed`, `ProposalNote`).
  **Der eine Alarm ist die bekannte WAL-Falle und nachgemessen entkraeftet:** `-wal` und `-shm`
  galten als verschwunden, sind beim Neustart um `09:22:12` wieder da (`0` und `32'768` Bytes),
  und die Hauptdatei ist in Laenge und Schreibzeit unveraendert (`357'822'464` Bytes,
  `01.09.2026 00:00:41`). `app_offline.htm` wurde nach dem Publish auf
  `app_offline.htm.disabled` umbenannt. **Vor dem Publish gepruefte Besonderheit:** das
  Arbeitsverzeichnis trug uncommittete Codeaenderungen einer anderen Sitzung (fuenfter
  Finance-Schalter, `+335` Zeilen ueber 15 Dateien). Diese sind bereits seit dem 2026-08-31
  produktiv; belegt durch die Server-DLL VOR diesem Deploy, die `MarcForeignProcurementMode`,
  `ForeignProcurementEvidenceStore`, `CH/AT: Herstellerregel gegen Fremdbezugsbeleg` und
  `IsIntercompanySellingTsc` bereits enthielt. Dieser Publish hat also nur den Export neu
  ausgeliefert. Werkzeug: `.tmp_tools/DeployMarketSegmentExport0901`.
  **Nachtrag 2026-09-01:** der damals uncommittete Funktionsstand ist inzwischen im Commit
  `835b317` enthalten; der Produktivstand ist damit wieder aus Git reproduzierbar. `d346b32`
  im automatisch erzeugten Protokollabsatz bleibt historisch der Elterncommit des damaligen
  Arbeitsverzeichnisses.
  **Nebenbefund zur Datenhygiene:** die Sicherungsroutine laesst ihre Zwischendatei im lokalen
  `%TEMP%` liegen und meldet das auch. Dort lagen dadurch neun vollstaendige Kopien der
  Produktivdatenbank aus Deploys seit dem 2026-08-25, zusammen rund `3` GB Kundendaten
  ausserhalb der Freigabe. Die Kopie dieses Laufs ist nach Groessenvergleich mit der
  geprueften Kopie auf der Freigabe geloescht; die acht aelteren stehen noch offen.
  **Fachlicher Stand am Deploytag, read-only gemessen:** `173` Vorschlaege, davon `0`
  bestaetigt, `269` Umfragezeilen, `104'222` Verkaufszeilen, `7'526` TRDE-Zeilen ohne
  Kundenname, `0` Protokolleintraege `Segment entfernt`. Die Blaetter `Bestaetigt` und
  `Verworfen (Protokoll)` sind daher heute leer und tragen ihren Hinweistext.
  **Zusaetzlich ueber HTTP nachgeprueft, weil die Deploy-Konsole nur Statuscodes und Groessen
  misst:** ein Abruf von `/marktsegmente` mit den Anmeldedaten des angemeldeten Nutzers liefert
  `200` mit `71'438` Bytes, enthaelt die Knopfbeschriftung `Export (Excel)` und **kein**
  `Passwort`. Die Seite rendert also wirklich, statt nur erreichbar zu sein, und der Knopf ist
  im ausgelieferten HTML vorhanden.
  **Offen bleibt trotzdem:** dass ein Klick die Datei erzeugt und die acht Blaetter so
  aussehen wie beschrieben. Das belegt erst ein Klick im Browser mit Blick in die Datei.
  Konzept: `docs/KONZEPT_RAILWAY_EXPORT_2026-09-01.md`.

- Aktuellster produktiv verifizierter Deploy: **2026-08-31 11:32, Finance: CH/AT-
  Herstellerregel gegen aktiven Fremdbezugsbeleg steuerbar**, Funktionscommit `d346b32`,
  `655/655` Release-Tests gruen vor dem Publish. Neue gepruefte Blockkopie
  `trafag_exporter.db.before-finance-foreign-procurement-switch-20260831-113052.bak`;
  `BiDashboard.dll` `31.08.2026 11:22:18`, `4'854'784` Bytes, SHA256
  `7C986CB003F4BD6FCE421E2135E567216AB2AFE66F73E8D86C80F2DFF`, lokaler Release-Build und
  Server bitgleich. Ziel: `0` neu, `3` geaendert, `1'817` unveraendert, `0` verschwunden.
  HTTPS `200`: Startseite, `/settings`, `/management-cockpit`. Wirknachweis in der DLL:
  `MarcForeignProcurementMode`, `ForeignProcurementEvidenceStore`,
  `CH/AT: Herstellerregel gegen Fremdbezugsbeleg`,
  `Fremdbezugsbeleg klassifiziert als Extern`; alle vier fehlten im Prueflauf vor dem
  Deploy. Schema produktiv read-only nachgemessen: `MarcForeignProcurementMode = Ignore`,
  also bleibt die Herstellerregel standardmaessig bestehen. Der fachliche Entscheid von
  Andreas ist nicht vorweggenommen. `app_offline.htm` wurde nach dem Publish entfernt.
  Werkzeug: `.tmp_tools/DeployFinanceForeignProcurement0831`. Offen bleibt allein der
  Sichtprueflauf im angemeldeten Browser.

- Aktuellster produktiv verifizierter Deploy: **2026-08-31 09:47, Finance: IT/IN-Kostenmethode
  und CHF-Kursprofil, dazu die Abhaengigkeiten in den GUI-Texten**, `644/644` Release-Tests
  gruen vor dem Publish. Neue gepruefte Blockkopie
  `trafag_exporter.db.before-finance-itin-chf-switches-20260831-094559.bak`. `BiDashboard.dll`
  `31.08.2026 09:17:43`, `4'830'720` Bytes, SHA256
  `DEA49DC63FFB1C261CF80AB073B2C4BC21A9EECC295763A9E8250BBEB0E4CDFF`, lokaler Release-Build und
  Server bitgleich. Ziel: `0` neu, `5` geaendert, `1'809` unveraendert. HTTPS `200`: Startseite,
  `/settings`, `/management-cockpit`. Wirknachweis in der DLL: `B1GroupStandardCostMode`,
  `GroupMarginChfRateMode`, `AveragePositive`, `FinanceYearEndRate`,
  `CHF-Finance-Umrechnung: Kursprofil`, `Greift nur bei Zeilen ohne Sales Type und ohne
  Lieferantenangabe`, `Aendert nur die Kostenquelle, nicht die Lieferantenklassifikation`; alle
  sieben fehlten im Prueflauf vorher. Nicht mehr enthalten sind die beiden ersetzten Hilfetexte
  vom 27.08. **Schemaerweiterung produktiv nachgemessen** (read-only, nach dem ersten Start):
  `ExportSettings` hat jetzt `B1GroupStandardCostMode = LatestPositive` und
  `GroupMarginChfRateMode = CurrentDailyRate`; `SupplierFallbackMode` blieb `ChPlantMaster` und
  `InternalSupplierCostSourceMode` blieb `DeliveringEntityCosts`. Alle vier Schalter stehen also
  auf dem bisherigen Verhalten, keine stille fachliche Aenderung. **Der eine Alarm ist die
  bekannte WAL-Falle und nachgemessen entkraeftet:** `-wal` und `-shm` galten als verschwunden,
  sind beim Neustart um `09:47:28` wieder da (`16'512` und `32'768` Bytes), und die Hauptdatei ist
  in Laenge und Schreibzeit unveraendert (`357'761'024` Bytes, `30.08.2026 17:18:50`). Werkzeug:
  `.tmp_tools/DeployFinanceSwitches0831`. **Sichtprueflauf durch Ingo am 2026-08-31 erledigt:**
  in `Admin Bereich > Settings` zeigen alle vier Auswahlfelder die neuen Abhaengigkeitstexte an
  der richtigen Stelle unter der Sektion `Finance: Kostenbasis und CHF-Umrechnung`, auf Deutsch
  und mit den erwarteten Standardwerten (`Neu: CH-Werkstamm (MARC 1100)`,
  `Kosten der liefernden Gesellschaft (Standard)`,
  `Juengster positiver Belegpreis je Material (Standard)`,
  `Aktueller Tageskurs (bisheriger Standard)`). Nebenbefund: die Server-DLL vor diesem Deploy war
  `27.08.2026 16:21:35`, `4'795'392` Bytes, SHA256
  `8A21A8AC997B9F24BA4B4B887EB13E83E65D8956777896AC71AE2074DF80C13C` und damit ein spaeterer
  Publish als der unten protokollierte Stand von `16:12`; der Literalnachweis belegt aber, dass
  es derselbe Funktionsumfang vom 27.08. war.

- Aktuellster produktiv verifizierter Deploy: **2026-08-27 16:12, Finance: zwei
  steuerbare Kostenquellen-Schalter fuer Andreas**, `638/638` Release-Tests gruen. Neue,
  konsistente Sicherung `trafag_exporter.db.before-finance-cost-switches-20260827-161130.bak`;
  `integrity_check` der Blockkopie ist `ok`. `BiDashboard.dll` `4'794'880` Bytes, SHA256
  `E0E2CF23F01D3A04A9E891D8AF674B381BEE683E57A5A938954309531D10E0DD`, lokaler
  Release-Build und Server bitgleich. Wirknachweis: `InternalSupplierCostSourceMode`,
  `Kostenquelle bei internem Lieferanten`, `Schweizer STPRS bei MARC Werk 1100` und
  `Lokale Standardkosten bei fehlendem Lieferanten`. HTTPS `200`: Startseite, `/settings`
  und `/management-cockpit`. Produktivdatenbank read-only nach Start geprueft:
  `SupplierFallbackMode = ChPlantMaster`,
  `InternalSupplierCostSourceMode = DeliveringEntityCosts`; das ist der bisherige Stand.
  Offen bleibt der angemeldete Sichtprueflauf hinter dem Finance-Unlock. Werkzeug:
  `.tmp_tools/DeployFinanceCostSwitches`.

- Aktuellster produktiv verifizierter Deploy: **2026-08-27 15:28, Sales Type vor
  Lieferantenfeldern, Waehrungsbeschluss B5/B6, ISS-013, Einkaufsdashboard**, Funktionscommit
  `686e1e1`, `635/635` Tests gruen (Release-Lauf vor dem Publish). Neue Sicherung
  `trafag_exporter.db.before-salestype-waehrung-einkauf-20260827-152748.bak` als gepruefte
  Blockkopie. Hinweis: das Arbeitsverzeichnis war nicht sauber (Branch
  `feature/supplier-overrides-trit`, dazu Bestandsaenderungen aus fruheren Sitzungen).
  `BiDashboard.dll` `27.08.2026 15:26:12`, `4'784'640` Bytes, SHA256
  `453A7CB8476A7721EBD192F41ADEC2495FC909E1A25649574BE7BF64FDF8FFB5`; lokaler Release-Build
  und Server bitgleich. `app_offline.htm` gesetzt und danach auf `app_offline.htm.disabled`
  umbenannt. Ziel: `0` neu, `5` geaendert, `1'734` unveraendert, `0` verschwunden.
  HTTPS `200`: Startseite (`71'408` Bytes), `/management-cockpit` (`72'299` Bytes),
  `/einkauf` (`101'251` Bytes). Wirknachweis in der DLL:
  `GroupMarginCostCurrencyDecision20260827Applied`, `TAGESKURS`, `Konzernmarge in CHF`,
  `Trafag Sachnummer`, `Bestellwert im Zeitraum`; nicht mehr enthalten
  `Kosten mit Jahreskurs in Verkaufswaehrung umrechnen`.
  **Schemaaenderung und einmalige Datenmigration produktiv nachgemessen** mit
  `.tmp_tools/SqlQ` (ReadOnly) nach dem ersten Start: `ExportSettings` traegt die neue
  Markerspalte `GroupMarginCostCurrencyDecision20260827Applied = 1`, und
  `GroupMarginCostCurrencyMode` steht jetzt auf `Convert` statt `Mask`. Der
  `SupplierFallbackMode` blieb erwartungsgemaess auf `ChPlantMaster`.
  Werkzeug: `.tmp_tools/DeployAugust27`.
  **Noch offen:** der angemeldete Sichtprueflauf. Die HTTP-`200` belegen Erreichbarkeit, nicht
  die Anzeige hinter dem Finance-Unlock. Anzusehen sind die Waehrungsbalken im Einkauf
  (starten sie jetzt an derselben Kante), die vollstaendige Lieferantenkaskade, und im
  Cockpit die Konzernmarge in CHF samt der `32` bisher maskierten Zeilen.

- Aktuellster produktiv verifizierter Deploy: **2026-08-25 15:21, Konzern-Standardkosten
  TR IT und TR IN aus B1 StockPrice**, Funktionscommit `b83ee84`, `601/601` Release-Tests
  gruen. Neue Sicherung
  `trafag_exporter.db.before-group-costs-trit-trin-20260825-150549.bak`
  (`354'263'040` Bytes); wegen `105'237'192` Bytes noch nicht eingecheckter WAL-Daten
  korrekt ueber den langsamen seitenweisen SQLite-Weg erstellt (`960,8 s`).
  `BiDashboard.dll` `25.08.2026 15:23:03`, `4'688'896` Bytes, SHA256
  `0DDFA2A57A6F1A402AF0EF5F6347B32313FDAF4A14EEBCBD060A858D8C35C874`; lokaler
  Release-Build und Server bitgleich. Ziel: `0` neu, `5` geaendert, `1'659`
  unveraendert, `0` verschwunden. HTTPS `200`: Startseite (`71'403` Bytes) und
  `/management-cockpit` (`72'264` Bytes). Wirknachweis in der DLL:
  `B1GroupStandardCostBuilder`, `TryResolveB1Source`,
  `Konzernkosten TR IT (B1 StockPrice)` und `Konzernkosten TR IN (B1 StockPrice)`;
  alle vier fehlten im Prueflauf und sind nach dem Deploy enthalten. Kein Schemawechsel.
  Noch nicht belegt: produktive Befuellung der Kostenbereiche `TRIT`/`TRIN`; dafuer muss
  nach dem Deploy je ein Standortimport laufen. **Nachtrag:** am selben Tag um 16:15 belegt,
  TR IN 6'119 Zeilen ueber 1'242 Materialien, TR IT 112 Zeilen ueber 40 Materialien
  (`docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 4).

- Aktuellster produktiv verifizierter Deploy: **2026-08-25 10:14, LineRegistrationDate als
  Spalte 52 im zentralen Sales_All**, Funktionscommit `d414427`, `586/586` Release-Tests
  gruen. Vorher-Sicherung `trafag_exporter.db.before-linereg-date-20260825-101315.bak`
  (`353'685'504` Bytes, geprueste Blockkopie, groessen- und zeitgleich zur Quelle).
  `BiDashboard.dll` `25.08.2026 10:15:48`, `4'675'072` Bytes, SHA256
  `D665E0DCA23159E1BCC85C86B04C710770542824C94B82760E29FD77FE8C3617`, lokaler Release-Build
  und Server bitgleich. Ziel: `0` neu, `5` geaendert, `1'623` unveraendert, `2` verschwunden.
  HTTPS `200`: Startseite (`71'413` Bytes), `/management-cockpit` (`72'279` Bytes).
  Wirknachweis mit Vorher-Messung fuer `ApplyLineRegistrationDateFallback`,
  `EnsureSpainDateMappings`, `Line Registration Date` und `fecharegistro`; alle vier fehlten
  im Prueflauf und sind danach enthalten. Nicht mehr enthalten: der alte Seed-Name
  `EnsureSpainPostingDateMapping`, der vorher nachweislich da war. Die vier Tokens wurden
  vorab per `git grep` gegen `22767e8` geprueft, damit keines einen Treffer vortaeuscht.
  **Der eine Alarm ist erklaert und nachgemessen:** `trafag_exporter.db-wal` (`0` Bytes) und
  `-shm` galten als verschwunden. Das ist der WAL-Flush beim Herunterfahren, nicht ein
  Datenverlust — beide Dateien sind um `10:15:55` beim Neustart wieder da, und die
  Hauptdatei blieb in Laenge und Schreibzeit unveraendert (`353'685'504` Bytes,
  `24.08.2026 13:48:05`).
  **Schemaaenderung produktiv nachgemessen** mit dem neuen read-only Werkzeug
  `.tmp_tools/CheckLineRegistrationColumn`: `CentralSalesRecords` hat jetzt `51` Spalten,
  `LineRegistrationDate` ist als letzte vorhanden. Fuellgrad ueber alle neun Standorte `0`,
  was vor dem ersten Spanien-Import korrekt ist. Zum Vergleich mitgemessen: `TRES` hat
  `7'168` Zeilen mit `1'523` Buchungsdaten (`21.2 %`), `TRUK` `3'106` Zeilen mit `3'090`.
  **Wichtiger Nebenbefund, der die Sichtpruefung betrifft:** Spanien fuehrt in
  `ManualExcelColumnMappings` **keine** eigenen Zeilen mehr und laeuft ueber den generischen
  Kopfzeilen-Fallback. `EnsureSpainDateMappings` hat deshalb bewusst nichts angelegt — genau
  so ist die Funktion gebaut, weil eine einzelne Zeile den Fallback abschalten und alle
  anderen Felder leer laufen lassen wuerde. Der Fallback erkennt `LineRegistrationDate`
  ueber `HeaderMap` von selbst, es ist also keine Handarbeit in den Einstellungen noetig.
  **NICHT belegt:** dass die Spalte im erzeugten Sales_All beim naechsten Gesamtexport
  gefuellt ankommt. Das braucht einen Spanien-Import und danach einen Blick in die Datei.

- Deploy davor: **2026-08-24 08:01, Lagerwert dauerhaft speichern**, Commits `45c3aa4` und
  `29a0f44`, `580/580` Tests gruen. Dieser Deploy ist hier nie als Kurzstand nachgetragen
  worden; die Belege stehen in `docs/AGENT_COORDINATION.md` und die dabei gefundene
  WAL-Sichtbarkeitsfalle unten in diesem Dokument.

- Deploy davor: **2026-08-21 14:17, Statusampel fuer alle
  Meldungen in der Kopfleiste**, Funktionscommit `669c920`, `564/564` Release-Tests gruen.
  Vorher-Sicherung `trafag_exporter.db.before-status-light-20260821-141006.bak`. Lokaler
  Release-Build und Server bitgleich. Ziel: `0` neu, `7` geaendert, `1'578` unveraendert,
  `0` verschwunden. HTTPS `200`: Startseite, `/management-cockpit`. Wirknachweis mit
  Vorher-Messung fuer `AppNotificationCenter`, `WorstUnseen` und `status-light`.
  **Ueber HTTP nachgeprueft:** die Startseite liefert `status-light`, `status-light-lamp`
  und `status-light-green` (gruen ist der richtige Anfangszustand), die Versionskennung ist
  von `639229094291882821` auf `639229112309249018` gewechselt (der Cache-Bruch wirkt), und
  das CSS enthaelt `.status-light-lamp`, `status-light-red`, `status-light-pulse` sowie
  `min-width: 0` fuer den Textumbruch.
  **ERSTER DEPLOY MIT DER NEUEN SICHERUNGSLOGIK, mit Messung:** `194,6 s` gesamt statt der
  vorherigen rund 35 Minuten, also etwa elffach schneller. Aufteilung: Lesen `96,1 s`,
  `integrity_check` `ok`, Uebertragen `93,8 s`. Begruendung und die widerlegte erste
  Erklaerung in `docs/DEPLOYMENT.md` Abschnitt 5a.
  **NICHT belegt:** dass die Ampel im Browser richtig aussieht und der Klick die Liste
  oeffnet. Die Zustandslogik ist durch 13 Tests abgedeckt, die Darstellung nicht.

- Vorheriger Deploy: **2026-08-21 13:35, Reconnect-Overlay
  gehaertet**, Funktionscommit `fa63849`, `547/547` Release-Tests gruen. Vorher-Sicherung
  `trafag_exporter.db.before-overlay-fix-20260821-130136.bak` (`353'378'304` Bytes).
  Lokaler Release-Build und Server bitgleich. Ziel: `0` neu, `7` geaendert, `1'577`
  unveraendert, `0` verschwunden. HTTPS `200`: Startseite, `/management-cockpit`.
  Wirknachweis mit Vorher-Messung fuer `AssetVersion`, `css/app.css?v=` und
  `style="display: none"`.
  **BEHOBENER FEHLGRIFF des vorherigen Deploys:** Das Overlay aus `99e92f2` versteckte sich
  ausschliesslich ueber `app.css`. Die Browser hielten die alte Datei im Zwischenspeicher,
  also fehlten die Regeln, und das Markup wurde nackt gerendert — dauerhaft sichtbar, im
  normalen Seitenfluss, mit allen drei Zustandstexten am Stueck. Der Server lieferte die
  richtige Datei, sie kam nur beim Benutzer nicht an. Lehre: **ausgeliefert ist nicht
  angekommen**; bei Aenderungen an `wwwroot`-Dateien immer eine Versionskennung mitgeben.
  **Ueber HTTP nachgeprueft, nicht nur am Publish:** Versionskennung im HTML aktiv
  (`v=639229094291882821`), Overlay und alle `3` Zustandszeilen tragen
  `style="display:none"`, das CSS ist unter der Versionsadresse erreichbar und enthaelt
  genau `4` Regeln mit `!important` (eine fuer das Overlay, drei fuer die Zustaende),
  `reconnect.js` ebenfalls versioniert.
  **NICHT belegt:** dass das Overlay im echten Verbindungsverlust erscheint und der
  automatische Reload greift. Das braucht eine Sichtpruefung.

- Vorheriger Deploy: **2026-08-21 11:02, Verbindungsanzeige und
  Selbstheilung fuer den Blazor-Circuit**, Funktionscommit `99e92f2`, `547/547`
  Release-Tests gruen. Vorher-Sicherung
  `trafag_exporter.db.before-reconnect-ui-20260821-104645.bak` (`353'378'304` Bytes).
  `BiDashboard.dll` `21.08.2026 11:03:21`, `4'635'648` Bytes, SHA256
  `951D5BD878C83B27A9D42F17CA672D439E598F3851AF0490AC07F2CE1DD6A661`; lokaler Release-Build
  und Server bitgleich. `app_offline.htm` gesetzt und danach auf `app_offline.htm.disabled`
  umbenannt. Ziel: `1` neu (`wwwroot/js/reconnect.js`), `7` geaendert, `1'544` unveraendert,
  `0` verschwunden. Produktiv-DB unveraendert. HTTPS `200`: Startseite,
  `/management-cockpit`, `/einkauf/aufriss`. Deploy ueber das neue Konsolenwerkzeug
  `.tmp_tools/DeployReconnectUi`, Prueflauf vorher gefahren.
  **Wirknachweis mit Vorher-Messung:** `components-reconnect-modal`,
  `Verbindung zum Server unterbrochen` und `js/reconnect.js` fehlten im Prueflauf in der
  Server-DLL und sind danach enthalten.
  **Zusaetzlich ueber HTTP nachgeprueft**, weil die Deploy-Konsole nur Routen abruft und
  keine statischen Dateien: `/js/reconnect.js` liefert `200` (`3'408` Bytes, enthaelt die
  Terminalzustands-Pruefung und `location.reload`), `/css/app.css` liefert `200`
  (`3'503` Bytes, lokal und Server byteidentisch, Overlay-Styles und alter Bestand beide
  vorhanden), und im HTML der Startseite stehen sowohl das Element
  `id="components-reconnect-modal"` als auch die Einbindung des Skripts. Die Seitengroesse
  der Startseite stieg von `69'014` auf `70'150` Bytes, was zum ergaenzten Markup passt.
  Behoben wird damit, dass ein abgerissener Circuit voellig unsichtbar war und die Seite auf
  keinen Klick mehr reagierte. **NICHT behoben und weiterhin offen: die Ursache der
  Verbindungsabrisse selbst.** Details: `docs/AGENT_COORDINATION.md`.
  **NICHT durch Tests abgedeckt:** Markup, CSS und JavaScript; dass das Overlay im Browser
  wirklich erscheint und der automatische Reload greift, ist noch nicht sichtgeprueft.

- Vorheriger Deploy: **2026-08-21 10:26, Endlosschleife im
  Lagerwert-Read behoben**, Funktionscommit `d36d9a2`, `547/547` Release-Tests gruen.
  Vorher-Sicherung `trafag_exporter.db.before-stockvalue-loopfix-20260821-101222.bak`
  (`276'226'048` Bytes ueber die SQLite-`BackupDatabase`-API). `BiDashboard.dll`
  `21.08.2026 10:27:18`, `4'633'088` Bytes, SHA256
  `92D6D4B8072F22C1E2920CCDA1294657222B67F765424E61EF60CC3374460584`; lokaler Release-Build
  und Server bitgleich. `app_offline.htm` gesetzt und danach auf `app_offline.htm.disabled`
  umbenannt. Ziel: `0` neu, `5` geaendert, `1'544` unveraendert, `0` verschwunden.
  Produktiv-DB in Laenge und Schreibzeit unveraendert (`353'378'304` Bytes). HTTPS `200`:
  Startseite, `/einkauf`, `/einkauf/aufriss`, `/management-cockpit`. Deploy ueber das neue
  Konsolenwerkzeug `.tmp_tools/DeployStockValueLoopFix` (Wiederverwendung von
  `Tools/DeployConsole/DeployRunner`), Prueflauf vorher gefahren.
  **Wirknachweis mit Vorher-Messung:** im Prueflauf fehlten `ParsePlannerMap`,
  `StockValueReadTimeout`, `Lagerwert-Read wegen Zeitgrenze abgebrochen` und
  `ignoriert $top: angefordert` allesamt in der Server-DLL und sind danach enthalten; der
  alte Formatstring `&$select=Matnr,Werks,Dispo&$filter=` war vorher vorhanden und ist weg.
  Behoben wurde damit, dass **jeder Einkauf-Lauf seit dem 2026-08-19 haengen blieb**, weil
  `MARCSet` `$top`/`$skip`/`$filter` ignoriert und die Paginierungsschleife im
  Lagerwert-Read deshalb nie abbrach. Details:
  `docs/EINKAUF_LAGERWERT_2026-08-18.md` Abschnitt 11.
  **NOCH NICHT belegt:** dass der Lagerwert wirklich gelesen wird — dafuer muss ein
  Einkauf-Lauf durchlaufen und `Lagerwert-Read beendet` im Log erscheinen. Der Cache liegt
  im Speicher und ist nach diesem Deploy leer, die Kachel zeigt bis dahin weiter
  „wartet auf Einkauf-Lauf". Der fachliche MB5L-Abgleich steht ebenfalls weiter aus.

- Vorheriger Deploy: **2026-08-20 10:33, Begruessungston auf der
  Startseite** (`herzlich.mp3`, autoplay, Schalter `LandingPage:PlayWelcomeSound` default an),
  Funktionscommit `86a7782`, `543/543` Release-Tests gruen. Vorher-Sicherung
  `trafag_exporter.db.before-welcome-sound-20260820-*.bak`. `BiDashboard.dll`
  `20.08.2026 10:12:02`, `4'631'552` Bytes, SHA256
  `475C811111EDE9946758367F0F539D038BD4528269347612F35F5BDAF0806625`; lokaler Release-Build
  und Server bitgleich. `app_offline.htm` gesetzt und danach auf `app_offline.htm.disabled`
  umbenannt. Ziel: `1` neu (die mp3), `7` geaendert, `1'508` unveraendert, `0` verschwunden.
  Produktiv-DB unveraendert. HTTPS `200`: Startseite, `/management-cockpit`,
  `/einkauf/aufriss`. **Wirknachweis mit Vorher-Messung:** im Prueflauf fehlten
  `PlayWelcomeSound` und `audio/herzlich.mp3` in der Server-DLL, danach sind beide
  enthalten. **NICHT belegt:** ob die Wiedergabe im Browser tatsaechlich hoerbar ist —
  Chromium-Autoplay-Policy (Firmenbrowser ist Edge) kann unmutigen Autoplay ohne vorherige
  Nutzerinteraktion blockieren; dann bleibt die Wiedergabe stumm, ohne Fehler. Rueckmeldung
  von Ingo nach eigenem Test steht noch aus. Details: siehe Commit-Beschreibung `86a7782`.

- Deploy davor: **2026-08-19 10:56, App-Titel umbenannt in
  "Trafag Cockpit"** (vorher "Trafag Finance/Sales Management Cockpit", Browser-Tab hatte
  zusaetzlich den Tippfehler "Finanze"), Funktionscommit `81e1eb3`, `543/543` Release-Tests
  gruen. Vorher-Sicherung `trafag_exporter.db.before-title-rename-20260819-104751.bak`
  (`353'099'776` Bytes). `BiDashboard.dll` `19.08.2026 10:44:30`, `4'631'040` Bytes, SHA256
  `BE9FA4DA0610A9DA85C4B2817F6F1850BCD8A460C1A49C9FD7F3C92D75DBBF5E`; lokaler Release-Build
  und Server bitgleich. `app_offline.htm` gesetzt und danach auf `app_offline.htm.disabled`
  umbenannt. Ziel: `0` neu, `5` geaendert, `1'476` unveraendert, `0` verschwunden.
  Produktiv-DB in Laenge und Schreibzeit unveraendert (`353'099'776` Bytes,
  `19.08.2026 10:39:20`). HTTPS `200`: Startseite (`68'901` Bytes), `/management-cockpit`
  (`69'965` Bytes), `/einkauf/aufriss` (`138'964` Bytes).
  **Wirknachweis mit Vorher-Messung:** `Trafag Cockpit` selbst war als Token ungeeignet, weil
  es als Substring bereits im unabhaengigen Schluessel `Trafag Global BI Cockpit` (spanische
  Uebersetzung `Trafag Cockpit BI global`) steckte und einen Treffer vorgetaeuscht haette.
  Stattdessen vier kollisionsfreie neue Uebersetzungswerte gewaehlt: `ट्रैफ़ैग कॉकपिट` (Hindi),
  `Kabina Trafag` (Albanisch), `Trafag Kokpiti` (Tuerkisch), `Trafag raQ` (Klingonisch)
  fehlten im Prueflauf und sind danach enthalten; `Trafag Finance/Sales Management Cockpit`
  und `Trafag Finanze/Sales Management Cockpit` waren vorher enthalten und sind jetzt weg.
  Betrifft nur den App-Titel (Browser-Tab in `App.razor`, Kopfzeile in `MainLayout.razor`)
  in allen acht Sprachen; reine Textaenderung, kein Schemawechsel, keine Migration.
  **Arbeitsverzeichnis war beim Deploy nicht sauber** (Branch `main`, mehrere andere
  Sitzungen arbeiten parallel im selben Ordner); die abweichenden Dateien sind Dokumentation
  und Fremdbestand, kein zusaetzlicher `.cs`-Code ausser dem committeten Titel-Diff.
  **NICHT belegt:** angemeldeter Sichtprueflauf des neuen Titels im Browser.

- Deploy davor: **2026-08-19 10:07, Lagerwert-KPI-Kachel im
  Einkauf-Cockpit (Wunsch Armin)**, Funktionscommit `08901bb`, `543/543` Tests gruen
  (Release-Lauf vor dem Publish, sowohl Debug- als auch Release-Konfiguration geprueft).
  Vorher-Sicherung `trafag_exporter.db.before-stock-value-kpi-20260819-095935.bak`
  (`353'099'776` Bytes). `BiDashboard.dll` `19.08.2026 09:53:41`, `4'631'552` Bytes, SHA256
  `9635E2E0A2EF97E228BAB39E59EDED3C49F65AFA644299CAAE090EE08E8DD4AB`; lokaler Release-Build
  und Server bitgleich. `app_offline.htm` gesetzt und danach auf `app_offline.htm.disabled`
  umbenannt. Ziel: `0` neu, `5` geaendert, `1'474` unveraendert, `0` verschwunden.
  Produktiv-DB in Laenge und Schreibzeit unveraendert (`353'099'776` Bytes,
  `18.08.2026 21:26:05`). HTTPS `200`: Startseite (`68'944` Bytes), `/einkauf` (`97'781`
  Bytes), `/management-cockpit` (`69'983` Bytes), `/einkauf/aufriss` (`139'269` Bytes).
  **Wirknachweis mit Vorher-Messung:** im Prueflauf (`.tmp_tools/DeployStockValueKpi
  --dry-run`) fehlten `SapGatewayStockValueReader`, `StockValueTotal`,
  `PurchasingPlanners`, `Lagerwert Einkaufsteile` und `wartet auf Einkauf-Lauf` in der
  Server-DLL, danach sind alle fuenf enthalten.
  **Rein additiv, kein Schemawechsel, keine Migration.** Die Kachel liest den Lagerwert
  ausschliesslich aus einem In-Memory-Cache, der vom Einkauf-Full-/Delta-Lauf gefuellt
  wird; bis zum naechsten Lauf zeigt sie „wartet auf Einkauf-Lauf" statt einer Zahl.
  **NICHT geprueft:** ob `MARCSet` das Feld `Dispo` und `mbewSet` das Feld `Salk3`
  liefert — beides wird beim naechsten Einkauf-Lauf zur Laufzeit sichtbar, ein Ausfall
  bricht den Lauf nicht ab (`RefreshStockValueSafeAsync` wirft nicht). Der MB5L-Abgleich
  und Armins drei Fachfragen zur Abgrenzung sind weiterhin offen; die angezeigte Zahl ist
  bis dahin fachlich unbestaetigt. Details: `docs/EINKAUF_LAGERWERT_2026-08-18.md`
  Abschnitt 10.

- Deploy davor: **2026-08-14 21:02, Marktsegmente mit
  Jahresbezug und drehbarer 3D-Analyse**, Funktionscommit `7419473`, `520/520`
  Release-Tests gruen. Vorher-Sicherung
  `trafag_exporter.db.before-segment-year-20260814-205358.bak`. `BiDashboard.dll`
  `14.08.2026 21:03:23`, `4'595'712` Bytes, SHA256
  `D1FE3189A1C37401E8CF813134E0A882AAAC03D01F7996DA2D964B54A1613AE7`; lokaler
  Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach auf
  `app_offline.htm.disabled` umbenannt. Ziel: `0` neu, `5` geaendert, `1'400` unveraendert,
  `0` verschwunden. Produktiv-DB in Laenge und Schreibzeit unveraendert
  (`346'648'576` Bytes, `14.08.2026 14:25:41`). HTTPS `200`: Startseite (`68'934` Bytes),
  `/marktsegmente` (`68'598`), `/management-cockpit` (`69'988`), `/settings` (`70'015`),
  `/einkauf/aufriss` (`138'016`), `/logistik/materialdisposition` (`81'543`).
  **Wirknachweis mit Vorher-Messung:** im Prueflauf fehlten `MarketSegmentChartAxes`,
  `MarketSegmentChartValues`, `GetAvailableYearsAsync`,
  `Auf der Standortachse sind alle Segmente zusammengefasst.` und `px-4 pb-4` in der
  Server-DLL, danach sind alle fuenf enthalten. `/marktsegmente` waechst dabei von
  `66'785` auf `68'598` Bytes.
  **FALLE bei der Tokenwahl, hier bewusst umgangen:** `Jahr`, `Segment`, `marktsegmente`
  und `MarketSegmentPageService` stehen seit dem 2026-08-13 in der DLL; `Z: Jahr` waere ein
  Teilstring des schon vorhandenen `Z: Jahr / Zeit` aus dem Management Cockpit und
  `Alle Jahre` steht dort im Finance-Pivot. Alle fuenf haetten einen Treffer vorgetaeuscht.
  Dieser Deploy enthaelt neben dem Jahresfilter und der 3D-Analyse zwei Korrekturen mit
  Wirkung ueber die Seite hinaus: `MudMainContent` trug `pa-4`, was per `!important` den
  Abstand zur festen Kopfleiste ueberschrieb und die obersten rund 48 Pixel **jeder Seite**
  unsichtbar machte, und sechs `MudSelect`-Filter schrieben ihren Wert `alle` ueber die
  eigene Beschriftung. Keine Migration, kein Schemawechsel.
  **NICHT belegt:** ein angemeldeter Sichtprueflauf produktiv. Lokal gegen
  `trafag_exporter.db` wurde er gefahren, produktiv liegen die Finance-Routen hinter dem
  Unlock. Details: `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` Abschnitt 13.

- Deploy davor: **2026-08-13 11:58, Marktumfrage in der
  Anwendung pflegbar**, Funktionscommit `1371260`, `517/517` Release-Tests gruen.
  Vorher-Sicherung `trafag_exporter.db.before-market-segments-20260813-114437.bak`,
  `345'686'016` Bytes. `BiDashboard.dll` `13.08.2026 11:58:10`, `4'560'384` Bytes, SHA256
  `24B007AC818A247046FDC6B73A44C0B0FB3AF50A5C4C72B2736CD7ACFABA0416`; lokaler
  Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach auf
  `app_offline.htm.disabled` umbenannt. Ziel: `0` neu, `5` geaendert, `1'334` unveraendert,
  `0` verschwunden. HTTPS `200`: Startseite (`68'934` Bytes), `/marktsegmente` (`66'815`),
  `/management-cockpit` (`69'948`), `/settings` (`69'975`), `/einkauf/aufriss` (`138'247`),
  `/logistik/materialdisposition` (`81'543`). Wirknachweis mit Vorher-Messung:
  `MarketSurveyEntry`, `MarketSurveyPageService`, `MarketSurveyEntries`,
  `EstimatedQuantity` und `Freitext, Bereiche erlaubt` fehlten im Prueflauf und sind danach
  enthalten. **FALLE dabei erneut aufgefallen:** `Marktumfrage` taugt NICHT als Nachweis, das
  Wort steckt schon im Hilfetext „Zum Beispiel: Marktumfrage Railway 2026-05" des
  vorherigen Stands; ebenso war `IsConfirmed` beim Deploy um 11:14 ein Falschtreffer, weil
  der Name anderswo im Code existiert. Nachweistokens gehoeren aus dem Diff und muessen im
  Prueflauf nachweislich FEHLEN.
  **OFFEN und bewusst nicht ausgefuehrt:** der Import der Umfrage in
  `MarketSurveyEntries`. Die Tabelle ist produktiv LEER; der Reiter Marktumfrage zeigt
  deshalb keine Zeilen. Werkzeug steht bereit: `.tmp_tools/ImportMarketSurvey` mit Prueflauf
  und `--apply`, Prueflauf gemessen 269 Zeilen, 236 Kunden, 12 Laender, davon 179
  verknuepfbar und 90 ohne Umsatz. Details:
  `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md`.

- Deploy davor am selben Tag: **2026-08-13 11:14, Vorschlag gegen Bestaetigung getrennt und
  Ergebnissicht**, Funktionscommit `ecaae3d`, `507/507` Tests gruen. Vorher-Sicherung
  `trafag_exporter.db.before-market-segments-20260813-110030.bak`, `345'628'672` Bytes.
  `BiDashboard.dll` `13.08.2026 11:14:01`, `4'479'488` Bytes, SHA256
  `0A3EF0C563C69705AE46059AD72FCE5CD98FA069E87F6FA1B4E337C23910A87C`; bitgleich.
  Wirknachweis: `ProposalNote`, `GetResultAsync`, `GetProgressAsync`,
  `MarketSegmentFilterModes` und `Speichern und bestaetigen` fehlten vorher, danach
  vorhanden. Migration produktiv bestaetigt: Spalten `IsConfirmed` und `ProposalNote` in
  `CustomerMarketSegments` angelegt. **Danach ausgefuehrt:** Import von `173`
  unbestaetigten Vorschlaegen ueber `.tmp_tools/ImportRailwayProposals --apply`, read-only
  nachgeprueft `IsConfirmed=0: 173 Zeilen` ueber acht Standorte. Behebt zwei Fehler: der
  Filter „nur zugeordnete" kappte vorher auf die obersten 2'000 Kunden und verlor
  zugeordnete kleine Kunden, und die Filterauswahl startete auf einem Wert, der leer sein
  kann.

- Deploy davor am selben Tag: **2026-08-13 09:00, Marktsegment-Pflege fuer
  den Vertrieb und konsolidierter Issue-Log**, Funktionscommits `488cc42` (Code) und
  `07356a9` (Doku), `500/500` Release-Tests gruen. Vorher-Sicherung
  `trafag_exporter.db.before-market-segments-20260813-084731.bak`, `345'620'480` Bytes.
  `BiDashboard.dll` `13.08.2026 09:00:42`, `4'431'360` Bytes, SHA256
  `9B5A3039414C12679C0AB8DF3C837C6C2EA7953B29516F036118365E68174854`; lokaler
  Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach auf
  `app_offline.htm.disabled` umbenannt. Ziel: `0` neu, `5` geaendert, `1'329`
  unveraendert, `0` verschwunden. HTTPS `200`: Startseite (`68'939` Bytes),
  **`/marktsegmente` (`329'931` Bytes)**, `/management-cockpit` (`69'958`), `/settings`
  (`69'995`), `/einkauf/aufriss` (`138'252`), `/logistik/materialdisposition` (`81'548`).
  **Wirknachweis mit Vorher-Messung:** im Prueflauf fehlten `MarketSegmentResolver`,
  `CustomerMarketSegment`, `MarketSegmentPageService`, `Market Segment Source` und
  `marktsegmente` in der Server-DLL, danach sind alle fuenf enthalten; `/marktsegmente`
  lieferte vorher keine Antwort. Die `329'931` Bytes sind hier mehr als ein
  Erreichbarkeitsnachweis: Finance-Seiten hinter dem Unlock liefern sonst rund `69'000`
  Bytes Passwortpanel, die Seite rendert also wirklich. Additive Migration produktiv
  bestaetigt: Tabelle `CustomerMarketSegments` vorhanden, eindeutiger Index
  `UX_CustomerMarketSegments_Tsc_Customer` angelegt, `0` Zuordnungen (erwartet, die Pflege
  beginnt erst), Menueeintrag `market-segments` vorhanden, `CentralSalesRecords`
  unveraendert bei `97'537` Zeilen. Das Arbeitsverzeichnis war nicht sauber; die
  abweichenden Dateien sind Fremdbestand aus dem Repo-Wurzelverzeichnis, kein `.cs`.
  **NICHT belegt:** ein angemeldeter Sichtprueflauf der Seite und das Speichern einer
  Zuordnung durch einen echten Benutzer. Anleitung fuer den Vertrieb:
  `docs/Anleitung_Marktsegmente_Vertrieb_2026-08-13.docx`.

- Deploy davor: **2026-08-12 10:23, Andreas-Nachtrag
  lokale Standardkosten bei CH-Werkstamm-Nichttreffer**, Funktionscommit `fc5ae75`,
  `478/478` Release-Tests am Deploytag selbst gruen. Vorher-Sicherung
  `trafag_exporter.db.before-andreas-local-20260812-101429.bak`, `345'202'688` Bytes.
  `BiDashboard.dll` `12.08.2026 10:12:33`, `4'364'800` Bytes, SHA256
  `BC566BB9AF27805524583E293D604481E560FD5D3DDEA8D8F75DC76B19D0BAF4`; lokaler
  Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach auf
  `app_offline.htm.disabled` umbenannt. Ziel: `0` neu, `5` geaendert, `1'294`
  unveraendert, `0` verschwunden; alle geschuetzten DB-/WAL-/SHM-/BAK-Dateien
  unveraendert. HTTPS `200`: Startseite (`68'441` Bytes), `/admin/sessions`
  (`69'560`), `/settings` (`69'502`), `/management-cockpit` (`69'500`),
  `/einkauf/aufriss` (`137'794`), `/logistik/materialdisposition` (`81'090`).
  **Wirknachweis mit Vorher-Messung** statt nur SHA-Vergleich: im Prueflauf vor dem
  Publish fehlten `IsConfirmedLocalMaterial`, `LocalSupplierRows` und
  `Standardkosten der lokalen Gesellschaft` in der Server-DLL, danach sind alle drei
  enthalten; das ersetzte Literal `'Kosten aus Verkaufszeile' (extern)` ist weg.
  **Das Wort `Lokal` allein taugt hier NICHT als Nachweis** — `Lokaler Standardpreis`
  stand schon im alten Stand. Nachweistokens gehoeren aus dem Diff, nicht aus der Doku.
  Produktivbedingung read-only bestaetigt: `SupplierFallbackMode=ChPlantMaster`,
  `96'298` Sales-Zeilen, `66'049` MARC-1100-Materialien; die Wirkungsmessung
  reproduziert nach dem Deploy `12'023` lokale Zeilen, davon `6'749` mit
  Standardpreis. Das Arbeitsverzeichnis war nicht sauber; die abweichenden Dateien
  sind ausschliesslich Dokumentation, kein `.cs`. **NICHT belegt:** dass die neue
  Lokal-Zahl im Cockpit rendert — `/management-cockpit` liegt hinter dem
  Finance-Unlock. Details:
  `docs/FINANCE_STANDARDKOSTEN.md`.

- Deploy davor: **2026-08-11 15:51, gesamter
  Workspace-Anwendungsstand**, ausdruecklich freigegeben trotz fehlender SAP-Sets
  `ZDISPO_GRP`/`ZDISPO_SPART`. `471/471` Release-Tests gruen. Konsistentes Backup
  `trafag_exporter.db.before-all-current-20260811-145332.bak`, `340'455'424` Bytes.
  `BiDashboard.dll` `4'362'752` Bytes, SHA256
  `2A5DBC034891F5B5D3FD1EE04C123A989CA987B5020CE04A0FE5161D037177F4`; lokaler
  Release-Build und Server bitgleich. Supplier-Fallback live mit
  `SupplierFallbackMode=ChPlantMaster` und `66'049` dauerhaft bestaetigten
  MARC-1100-Materialien; alle `63'550` MBEW-Schluessel enthalten, Sales-Bestand
  unveraendert `96'298`. Neun HTTP-Routen liefern 200. Einkaufs-SAP-only-Code ist
  live, aber die `45` historischen Excel-Regeln werden nun bewusst ignoriert; bis
  zur SAP-Aktivierung fehlen Produktgruppennamen und Refreshes koennen scheitern.
  Details: `docs/DEPLOYMENT.md`.

- Deploy davor: **2026-08-11 11:23, Admin-Bereiche
  zusammengefuehrt und FPV-Pausenspiel**, Funktionsstand aus schmutzigem Workspace auf Branch
  `main`, HEAD `09fb1fa`, `461/461` Tests gruen im Release-Lauf vor dem Publish; dazu
  `28/28` FPV- und `18/18` MOD-Pruefungen gruen.
  `BiDashboard.dll` `11.08.2026 11:19:47`, `4'332'032` Bytes, SHA256
  `D1A82215B25A3D5A86E74EDFBD11F7E5E810E2A2B77A739C5C550B74D19FD7AB`;
  lokaler Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach auf
  `app_offline.htm.disabled` umbenannt. HTTPS `200`: Startseite (`68'416` Bytes),
  `/admin/sessions` (`69'540`), `/pause` (`62'348`), `/js/pausegame.js` (`42'645`),
  `/management-cockpit` (`69'495`) und `/einkauf/aufriss` (`137'446`).
  Zielvergleich: `0` Dateien neu, `7` geaendert, `1'268` unveraendert, `0` verschwunden;
  alle geschuetzten DB-/WAL-/SHM-/BAK-Dateien im Publishvergleich unveraendert.
  Vor der Menue-Migration wurde per SQLite-Backup-API die konsistente Sicherung
  `trafag_exporter.db.before-admin-menu-merge-20260811-112250.bak` mit `340'369'408`
  Bytes erstellt. Produktiv read-only bestaetigt: genau eine Root-Gruppe `Admin Bereich`
  (`finance-admin`, Parent leer, Sort 90) mit den Kindern Aktive Logins, Standorte,
  Transformationen, Finance Regeln, Settings, Menuestruktur und Logs; kein Admin-Knoten mehr
  unter Finance. `wwwroot/js/pausegame.js` ist lokal und auf dem Server byteidentisch
  (SHA256 `DE09879793AD70B6AF9DDEA3CFDBB9F4AD4D853A1BEE37EA7E234B1D3129BB28`).
  Der Pausenreiter bleibt gemaess `Pause:Enabled=false` ausgeblendet; deployed ist der neue
  FPV-Code, nicht eine Aenderung des Betriebsschalters. Wirknachweis in der DLL:
  `DatabaseSeedService`, `Admin Bereich`, `Aktive Logins`, `finance-admin`, `pausegame.js`.
  Nicht belegt: angemeldeter visueller Browsertest der Admin-Seiten und manuelles
  Spielgefuehl. Details: `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md` und
  `docs/PAUSENSPIEL.md`.

- Deploy davor: **2026-08-10 07:05, Pausenreiter
  standardmaessig aus**, Funktionscommit `8e09774`, `459/459` Tests gruen im
  Release-Lauf vor dem Publish (vier neue, jeder vorher nachweislich rot).
  `BiDashboard.dll` `10.08.2026 07:04:59`, `4'331'008` Bytes, SHA256
  `1F0E3C79A9417FFAF61D2F090D5F929873BE4D7A43B07D08235D0D6E9BBCC6BF`; lokaler
  Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach auf
  `app_offline.htm.disabled` umbenannt. Produktiv-DB in Laenge und Schreibzeit
  unveraendert (`340'340'736` Bytes, `10.08.2026 00:10:51`), alle `16` `.bak`
  unveraendert. `Pause:Enabled` im Ziel steht auf `false`.
  Wirkung produktiv belegt: Startseite HTTPS `200` (`68'421` Bytes) **ohne** den
  Reiter `Pause`, mit `Einkauf` als Gegenprobe weiterhin vorhanden; `/pause` liefert
  `200` mit dem Hinweis „Der Pausenreiter ist ausgeschaltet." und **ohne** das
  Szenen-Element. Wirknachweis in der DLL: `PauseGameSettingsService`,
  `IPauseGameSettingsService` (UTF-8), Literale `Pausenreiter anzeigen`,
  `pause-game` und die tuerkische Uebersetzung `Mola sekmesini göster` (UTF-16).
  **NICHT belegt:** dass der Schalter unter Admin > Settings rendert — `/settings`
  liegt hinter dem Admin-Passwortpanel und liefert von hier aus nur dieses (Antwort
  enthaelt `Passwort`, aber weder `Einstellungen` noch `SharePoint`). Dafuer ist ein
  angemeldeter Sichtprueflauf noetig.
  Details: `docs/PAUSENSPIEL.md`.

- **FALLE: Admin-Schalter in `appsettings.json` ueberlebt den naechsten Deploy NICHT.**
  `appsettings.json` ist Build-Ausgabe und wird bei jedem Publish durch den
  Repository-Stand ersetzt — gemessen am 2026-08-10, der Server stand auf
  `Pause:Enabled = true`, danach auf `false`. Betrifft alles, was zur Laufzeit dorthin
  geschrieben wird: `Pause:Enabled` und `LandingPage:ShowWalkingLabFigure`. Wer eine
  Einstellung dauerhaft will, muss sie im Repository setzen. Dieselbe Klasse wie die
  XLSX weiter unten. Vor jedem Deploy lohnt ein Vergleich der beiden Dateien; am
  2026-08-10 war der Pausen-Schalter der einzige Unterschied.

- Deploy davor: **2026-08-07 17:05, Pausenspiel `/pause`**,
  Funktionscommits `ad0241d` (Spiel) und `b834d61` (Deploy-Konsole und Doku),
  `455/455` Tests gruen im Release-Lauf vor dem Publish, dazu zwei kopflose
  Prüfsonden mit je `18` Pruefungen gruen. `BiDashboard.dll` `07.08.2026 17:05:17`,
  `4'325'376` Bytes, SHA256
  `7F4FAB94B8C124042BC86D5E786F1D1EB54FDE159D9FB3D3CEF48D0FB3850811`; lokaler
  Release-Build und Server bitgleich (hier nicht nur Formsache — siehe die
  PreserveNewest- und die Bash-UNC-Falle oben). `app_offline.htm` gesetzt und danach
  auf `app_offline.htm.disabled` umbenannt, die alte `.disabled` vorher entfernt.
  HTTPS `200`: Startseite (`68'868` Bytes, kalt `29.59 s`), `/pause` (`62'349` Bytes,
  `0.29 s`); `/pause` enthaelt das Host-Element der Szene und **nicht** den
  Ausgeschaltet-Hinweis, der Schalter `Pause:Enabled` steht also wirksam auf `true`.
  Beide Spielmodule werden ausgeliefert: `js/pausegame.js` (`56'032` Bytes) und
  `js/modplayer.js` (`15'442` Bytes), jeweils HTTPS `200` und byteidentisch zur
  Quelle. Produktiv-DB in Laenge und Schreibzeit unveraendert (`340'205'568` Bytes,
  `07.08.2026 15:20:08`), alle `16` `.bak` unveraendert vorhanden, Dateien im Ziel
  `1219` -> `1221`. Wirknachweis in der DLL: `PauseGameOptions`, `PauseGame` (UTF-8)
  sowie die Literale `Der Pausenreiter ist ausgeschaltet.` und `pausegame.js`
  (UTF-16).
  **Rein additiv:** neue Route, eigenes JS-Modul, kein Datenzugriff, kein
  Serverzustand. Bestehende Seiten und Berechnungen unveraendert.
  **NICHT belegt:** dass das Spiel im Browser laeuft. Es gibt hier keine
  Browser-Automatisierung — der `200` und die ausgelieferten Module beweisen
  Erreichbarkeit, nicht dass die 3D-Szene erscheint, die Kamera brauchbar ist oder
  die Musik durchlaeuft. Erster Aufruf durch Ingo ist ein echter Test.
  Ausblenden ohne Deploy: `IsVisible` am Menueintrag `pause-game`; hart:
  `"Pause": { "Enabled": false }` in `appsettings.json`.
  Details: `docs/PAUSENSPIEL.md`.

- Deploy davor: **2026-08-07 10:22, Finance-Indikatoren
  ehrlich gemacht**, Funktionscommits `0c8cff5` und `b2e7c4f`, `455/455` Tests gruen
  (Release-Lauf vor dem Publish). `BiDashboard.dll` `07.08.2026 10:21:53`,
  `4'320'768` Bytes, SHA256
  `B43A9E4B49ADC3186A1DC7216F61E2C220BF5541C9A4180FBA9C51C7CA80E43D`; lokaler
  Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach
  umbenannt. HTTPS `200`: Startseite (`68'411` Bytes), `/management-cockpit`
  (`69'490`), `/finance-cockpit/vergleich` (`69'539`). Produktiv-DB in Laenge und
  Schreibzeit unveraendert (`339'210'240` Bytes, `07.08.2026 08:49:20`).
  Wirknachweis in der DLL: `FinanceCountryStatuses`, `IsExcludedByRule`,
  `MissingRateRowCount`, `DistinctMaterialNumberCount`, `MaterialKeys`,
  `GetAvailableReferenceYearsAsync` sowie die Literale `Nicht geprueft`,
  `Jahresumsatz`, `Pruefzeilen`; `YtdSalesChf` und `Passt gegen Soll` sind nicht
  mehr enthalten.
  **ACHTUNG zur Reichweite dieses Nachweises:** beide Finance-Routen liegen hinter
  dem Finance-Unlock und liefern von der Entwicklungsmaschine aus das
  Passwortpanel, nicht die Seite (geprueft: Antwort enthaelt `Finance Cockpit` und
  `Passwort`, aber nicht `Schnelluebersicht`). Der `200` belegt Erreichbarkeit,
  **nicht** dass die geaenderten Kacheln rendern — dafuer ist ein angemeldeter
  Sichtprueflauf noetig.
  **Behebt neun Indikatoren**, u. a. `Laender OK`/`Zu pruefen`, die zwei von vier
  Status verschwiegen (produktiv gemessen: `FinanceReferences` hat nur Zeilen fuer
  `2025`, Standardjahr der Seite ist `2026` — beide Kacheln standen auf `0`),
  Waehrungsmischung ohne Hinweis, Finance-Pivot auf ungefilterten Zeilen, und
  „Passt gegen Soll" als fest verdrahtete Ergebnisbehauptung.
  Details: `docs/FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md`.

- Deploy davor: **2026-08-07 08:40, Einkauf-Indikatoren
  ehrlich gemacht**, Funktionscommit `eef6374`, `449/449` Tests gruen (Release-Lauf
  vor dem Publish). `BiDashboard.dll` `07.08.2026 08:40:33`, `4'293'632` Bytes,
  SHA256 `214C51E3D08479847813D49B04ED754D6AE5DA614CF458E806BE4AF256BD093A`;
  lokaler Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach
  auf `app_offline.htm.disabled` umbenannt. HTTPS `200` mit Inhalt: Startseite
  (`68'466` Bytes), `/einkauf/lieferanten` (`101'751`, warm `8.46 s`),
  `/einkauf/kontrakte` (`102'019`), `/einkauf/bestellbedarf` (`92'159`),
  `/logistik/materialdisposition` (`81'070`). Produktiv-DB in Laenge und
  Schreibzeit unveraendert (`339'210'240` Bytes, `07.08.2026 08:00:54`).
  Wirknachweis in der ausgelieferten DLL: `HasUnitCost`, `ApplyScopeFilter`,
  `LatestAverageUnitPriceLabel` sowie die Literale
  `Bewertungsdaten (EKBE/QM) nicht angebunden` und `Letztes Bestelldatum`;
  `Simulation bis Bewertungsdaten kommen` ist nicht mehr enthalten.
  **Behebt sechs Indikatoren, die eine erfundene oder falsch beschriftete Zahl
  zeigten** (u. a. `Performance Score` als Konstante aus zwoelf
  Simulationszeilen, `Preisindikator` als Gesamt-Spend unter Stueckpreis-Label,
  Kontrakt-KPI und -Diagramm mit verschiedenen Grundmengen, gruener Risikobalken
  strukturell `0`) und macht fehlende Stueckkosten im Fehlwert sichtbar.
  Details: `docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md`.
  **WICHTIG fuer kuenftige Deploys:** Publish ueber
  `dotnet publish -c Release -o <UNC>`, NICHT ueber `/p:PublishProfile=FolderProfile` —
  das Profil hat `DeleteExistingFiles=true` und das Zielverzeichnis enthaelt
  `trafag_exporter.db` samt aller `.bak`-Sicherungen.

- Deploy davor: **2026-08-06 15:11, fuenf neue Supply-Chain-Reiter
  (Einkauf/Logistik)**, Funktionscommit `01af1b8`, `446/446` Tests gruen (vor dem Commit
  nachgerechnet, nicht uebernommen). `BiDashboard.dll` `06.08.2026 15:11:34`, `4'291'072`
  Bytes, SHA256 `29B9DFC6F46F74840431966E82040066F7B66FDD3AC8F12F73B4DF8F04761A61`.
  `app_offline.htm` gesetzt und wieder entfernt. Startseite und **alle fuenf neuen Routen**
  liefern HTTPS `200` mit Inhalt: `/logistik/materialdisposition` (81'078 Bytes),
  `/logistik/dispositionspruefung` (82'156), `/einkauf/bestellbedarf` (92'149),
  `/einkauf/materialabhaengigkeit` (102'922), `/einkauf/lieferperformance` (110'214).
  Wirknachweis in der DLL: `SupplyChainAnalysisService`, `SupplyChainAnalysisKind`,
  `SupplyChainUiTextCatalog`, `DeliveryPerformance`.
  Die Aenderung ist additiv: `Program.cs` +1 Zeile (DI), `DatabaseSeedService`,
  `NavigationIconResolver` und `UiTextService` zusammen +17 Zeilen; bestehende Einkaufs-,
  Spend-, Lieferanten- und Stuecklisten-Reiter unveraendert. Der Dienst ist rein lesend
  (kein `INSERT`/`UPDATE`/`DELETE`).
  **Bewusst keine OTIF-Kennzahl:** das Ist-Wareneingangsdatum aus EKBE/MSEG/MATDOC fehlt,
  deshalb weist die Lieferperformance nur das Plantermin-Risiko aus EKET aus und benennt
  die Luecke, statt eine Zahl zu schaetzen.
  Produktiv-DB: `339'210'240` Bytes / `06.08.2026 15:11:04` gegenueber `339'197'952` /
  `12:40:26` davor — die Aenderung stammt aus dem laufenden Betrieb bzw. dem WAL-Flush beim
  Herunterfahren, nicht aus dem Publish (keine Migration in diesem Stand).
  Details: `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md`.

- Deploy davor: **2026-08-06 14:24, HR-ZH-Feiertage
  und Filtervertrag**, Funktionscommit `9435a5d`, `438/438` Tests gruen.
  `BiDashboard.dll` `06.08.2026 14:24:10`, `4'137'472` Bytes, SHA256
  `B8391FBFC69DBB6B45F93D1D6AF3D8FC621C34FD11405C14A0E52BF98397B7B0`.
  `app_offline.htm` gesetzt und danach aus dem aktiven Namen entfernt;
  Startseite und `/BiDashboard/hr-kpi` liefern HTTPS `200`. Details:
  `docs/HR_KPI.md`.

- Deploy davor: **2026-08-06 13:57, ZDISPO-Zusatz
  nur fuer den Einkauf Spend-Aufriss**, Funktionscommit `0a8a4c9`, `435/435`
  Tests gruen. `BiDashboard.dll` `06.08.2026 13:57:11`, `4'136'448` Bytes,
  SHA256 `0F1CB29F6F766C8CB71903D45B78DB48B3AB94FE58638837F5376E9D2A9B01C1`.
  `app_offline.htm` gesetzt und danach aus dem aktiven Namen entfernt;
  Startseite HTTPS `200` (`64'770` Bytes),
  `/BiDashboard/einkauf/aufriss` HTTPS `200` (`133'542` Bytes, warm `10.15 s`).
  Produktiv lesend belegt: `45` Zeilen in `PurchasingSpendDisponentRule` aus
  `42` Mustern, `0` Zeilen in der unveraenderten manuellen
  `PurchasingProductGroupMap`, `105` ZLO03-Zeilen mit Disponent. Beide
  ZDISPO-XLSX-Dateien liegen im Publish-Verzeichnis. Details:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

- Deploy davor: **2026-08-06 12:31, Einkauf
  Produktgruppen und ABC/XYZ**, Funktionscommit `bb009bf`, `435/435` Tests
  gruen. `BiDashboard.dll` `06.08.2026 12:31:27`, `4'120'064` Bytes, SHA256
  `B5C72496A7A4E11AC38675D840A5DF9DBABA6999517DD70FE3D7C0CE07BAEC3C`.
  `app_offline.htm` gesetzt und danach aus dem aktiven Namen entfernt;
  Startseite HTTP `200` (`64'755` Bytes),
  `/BiDashboard/einkauf/aufriss` HTTP `200` (`133'577` Bytes, warm `8.43 s`).
  Additive Produktivmigration lesend belegt: `MaterialUsageCache.VknrDispo`
  und `PurchasingProductGroupMap` vorhanden; `105` Usage-Zeilen mit Disponent,
  ZC23-Map noch leer. Wirknachweis in der DLL: Typen
  `PurchasingProductGroupAllocationSummary`, `PurchasingAbcXyzActionRow` und
  `PurchasingProductGroupMap` enthalten. Die produktive Haupt-DB blieb beim
  Publish bei `339'185'664` Bytes / `06.08.2026 12:27:49`; die additive
  Migration liegt im aktiven WAL. Details:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

- Deploy davor: **2026-08-06 11:06, Finance-Anzeige durchgesehen**,
  Commit `d9d9a4f`, `433/433` Tests gruen. `BiDashboard.dll` `06.08.2026 11:06:26`,
  `4'057'600` Bytes, SHA256
  `E6CCF3C4AC6484DC8605338004A949835184DF67B9C9AEDFA6E13103C86FAF7E`. `app_offline.htm`
  gesetzt und wieder entfernt, `https://…/BiDashboard/` liefert HTTP `200` (64'720 Bytes),
  Produktiv-DB in Laenge und Schreibzeit unveraendert (`339'140'608` Bytes,
  `06.08.2026 09:17:59`). Wirknachweis im Deploy-Artefakt: `IsCostBasisKnown` und
  `CostBasisUnknown` sind in der ausgelieferten DLL enthalten (zur SHA-Pruefung siehe
  den Hinweis weiter unten).
  **Behebt einen produktiven Anzeigefehler:** das Finance-Pruefbuch liess die Marge nur bei
  der Waehrungsmaske leer. Eine fehlende Kostenbasis laeuft als 0 durch, also wies die Spalte
  `Marge CHF` den vollen Umsatz und 100 % aus — neben einem Status, der „Lieferant unklar"
  bzw. „Konzernkosten fehlen" sagte. Naeherungsweise ~71'900 von 96'059 Zeilen betroffen
  (Tabelle im Cockpit und Excel-Export `Finance_Pruefbuch`; der zentrale Excel-Nachweis war
  korrekt, dort steht die Marge als Blattformel mit `WENN(Status=OK)`).
  Details: `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md`.

- Deploy davor: **2026-08-06 09:41, Gruppenmarge in einer Klasse**,
  Commit `515ab9d` (`GroupMarginCalculator`: Lieferantentyp, Kostenbasis, Kostenquelle und Status
  fuer Excel-Nachweis UND Cockpit aus einer Hand; Kostenbasisregeln als geordnete Kette),
  `431/431` Tests gruen. `BiDashboard.dll` `06.08.2026 09:41:56`, `4'054'528` Bytes, SHA256
  `CF750722BE3D9AA9377B77D4A9B5C53969D9F7326136D4313CFF557C3D54AA3D`. `app_offline.htm` gesetzt
  und wieder entfernt, `https://…/BiDashboard/` liefert HTTP `200` (64'735 Bytes), Produktiv-DB
  in Laenge und Schreibzeit unveraendert (`339'140'608` Bytes, `06.08.2026 09:17:59`).
  Wirknachweis im Deploy-Artefakt: `GroupMarginCalculator`, `GroupMarginCostRules`,
  `GroupDistributionWithoutGroupCost` und `GroupMarginLine` sind in der ausgelieferten DLL
  enthalten. Der Deploy behebt eine seit 2026-08-05 15:48 produktive Abweichung: das Cockpit
  zeigte fuer LRD-Zeilen ohne Konzernkostentreffer „Standardpreis fehlt", der Excel-Nachweis
  „Konzernkosten fehlen", und die Kennzahl „offene Kostenbasis" zaehlte diese Zeilen nicht mit.
  Details: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 7d.

  **Hinweis zur SHA-Pruefung:** der Build ist NICHT deterministisch — zwei Uebersetzungen
  derselben Quelle ergeben verschiedene Hashes (MVID). Der Vergleich „Server gleich lokaler
  Build" belegt deshalb nur, dass beide aus demselben Zwischenstand (`obj/`) kopiert wurden.
  Fuer den inhaltlichen Nachweis dient die Typenpruefung in der DLL.

- Wirkung am Produktivbestand geprueft (2026-08-06 09:45): der TRIN-Export vom selben Tag 06:54
  fuellt die neuen Felder — **6'664 von 7'094 TRIN-Zeilen tragen einen Sales Type (93,9 %)**,
  3'625 eine Trafag-Sachnummer (`FFM` 5'923, `LRD` 718, `CM` 23, leer 430). Alle anderen
  Standorte stehen erwartungsgemaess auf 0. Von 718 `LRD`-Zeilen finden 581 die Schweizer
  Konzernkosten (ueber die lokale Artikelnummer waeren es 4), 137 erhalten den Status
  `Konzernkosten fehlen`; 5'868 `FFM`/`CM`-Zeilen wechseln von „Lieferant unklar" auf intern.

- Deploy davor: **2026-08-05 15:48, Sales Type und
  Trafag-Sachnummer im Export** (`SalesType`/`GroupMaterialNumber` aus dem Artikelstamm,
  Klassifikation und Konzernkostenschluessel darauf umgestellt), `406/406` Tests gruen.
  `BiDashboard.dll` `05.08.2026 15:48:20`, `4'045'824` Bytes, SHA256
  `0C65C9971460EE47A9C1999FB328E43BEBC63AB71AE7EFCD6D07010588A4E5EF`; Release-Build und
  Server bitgleich. `app_offline.htm` gesetzt und entfernt, HTTP `200`. Additive Migration
  wirksam: `CentralSalesRecords.SalesType` und `.GroupMaterialNumber` sind produktiv als
  `TEXT NOT NULL DEFAULT ''` vorhanden. Gefuellt seit dem TRIN-Export 2026-08-06 06:54
  (Nachweis oben). Details: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

- Deploy davor: 2026-08-05 10:59, **Server-Analyse**, Commit `cc72e6d`
  (`ServerAnalysisBackgroundService`: lesende Diagnoseabfragen gegen Standort-B1,
  ausgeloest ueber eine Triggerdatei in `_analysis`), `385/385` Tests gruen.
  `BiDashboard.dll` `05.08.2026 10:59:50`, `4'037'632` Bytes, SHA256
  `56AFD5AF156CD496A0EF42DFC5CF2E1FA724299BB632F3202FE0132131161B41`;
  Release-Build und Server bitgleich. Produktiv-DB in Laenge und Schreibzeit
  unveraendert (`338'472'960` Bytes, `03.08.2026 12:26:05`), `app_offline.htm`
  gesetzt und wieder entfernt, `https://…/BiDashboard/` liefert HTTP `200`
  (64'755 Bytes). Wirknachweis: Triggerlauf 11:13 und 11:20 haben Ergebnisdateien
  erzeugt, Protokollkategorie `Server-Analyse` zeigt Start/Ende ohne Fehler.
  Details: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

- Deploy davor: 2026-08-03, Commit `9e28086`
  (`Logistik > Stuecklistenanalyse`: Top-Down-/Bottom-Up-Dashboard), `353/353`
  Tests gruen. `BiDashboard.dll` `03.08.2026 06:59:38`, `4'024'832` Bytes,
  SHA256 `8D5586E5536C83A9EDB409472C332D190488898C3FE8E8DB2097C3131779B554`;
  Release-Build und Server bitgleich. Produktiv-DB in Laenge, Schreibzeit und
  SHA256 unveraendert, `app_offline.htm` entfernt, Port 443 offen und der
  authentifizierte Aufruf von `/BiDashboard/logistik/stuecklistenanalyse`
  liefert HTTP `200`.

## Deployment-Historie

- Ersetzte Deploy-Zwischenstaende stehen in `lastchange.md` und den Archiven unter
  `docs/raw_md_archive/`; diese Kurzdatei fuehrt nur den aktuell verifizierten Deploy.

## Upgreat Firewall

- Upgreat muss den neuen Webserver freischalten, nicht den lokalen Entwicklungs-PC.
- Webserver / Source:
  - `trch-webapp-bidashboard.trafagch.local`
  - `tragvapp401.trafagch.local`
  - `10.120.1.17`
- Bekannte Ziele:
  - HANA Internal / BI1: `10.194.65.22:30015`
  - India HANA: `20.197.20.60:30015`
  - SAP OData / ZSCHWEIZ: `10.194.64.29:8000`
  - SharePoint / Graph: `trafagag.sharepoint.com:443`
- Offen: vollstaendige Standortliste aus produktiver App-Konfiguration exportieren/pruefen.

## Rohquellen Nur Bei Bedarf

- IIS-Handoff: `docs/DEPLOYMENT.md`
- historischer lokaler Uebergangsserver: `docs/DEPLOYMENT.md`

- **FALLE, am 2026-08-24 selbst hineingelaufen: ein Schreibvorgang von aussen in die
  produktive Datenbank ist fuer die Anwendung unsichtbar, bis das WAL geschrieben ist.** Die
  produktive `trafag_exporter.db` laeuft im Modus `wal` (nachgemessen). Ein Werkzeug von einem
  Arbeitsplatzrechner schreibt seinen Commit in `trafag_exporter.db-wal` auf der Freigabe und
  liest ihn selbst korrekt zurueck; die Anwendung auf dem Server sieht ihn nicht, weil Leser
  neue Commits ueber den gemeinsamen Speicherindex `-shm` finden und diese Koordination ueber
  SMB zwischen zwei Rechnern nicht verlaesslich ist. Erkennungsmerkmal: Hauptdatei alt,
  `-wal` frisch und gross, Anwendung zeigt weiter den alten Stand, und eine Sonde vom eigenen
  Rechner findet die Daten trotzdem. Abhilfe `PRAGMA wal_checkpoint(TRUNCATE)`
  (`.tmp_tools/CheckpointWal`), danach ist `-wal` leer und die Hauptdatei traegt die Aenderung.
  **Besser: Daten, die die Anwendung lesen soll, von der Anwendung selbst schreiben lassen.**
  Details in `docs/EINKAUF_LAGERWERT_2026-08-18.md` Abschnitt 12.8.
