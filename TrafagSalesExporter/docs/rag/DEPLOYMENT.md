# RAG Deployment

Stand: 2026-10-22 (Kurzstand auf Deploys seit dem 2026-10-13 gekuerzt, Entscheid Ingo; letzter Deploy 2026-10-27 10:02)

Der Abschnitt „Kurzstand“ fuehrt nur Deploys seit dem Deploy-Ablauf vom 2026-10-13 und die Fallen. Aeltere
Eintraege (2026-08-03 bis 2026-10-09) stehen verbatim in `docs/archiv/DEPLOYMENT_KURZSTAND_ARCHIV_bis_2026-10-12.md`;
wer hier einen neuen Deploy eintraegt, verschiebt beim Aufraeumen Eintraege, die aelter als zwei Wochen sind, dorthin.

## Deploy-Ablauf seit 2026-10-13 (Entscheid Ingo)

- **DB-Sicherung einmal pro Woche** (Entscheid Ingo 2026-10-13 nachmittags, nachdem eine Sicherung zu lange
  dauerte und abgebrochen wurde). `DeployHeadless` sichert von selbst, wenn die neueste `trafag_exporter.db.before-*.bak`
  auf dem Share aelter als sieben Tage ist (Etikett `wochensicherung`); `--backup` erzwingt eine Sicherung,
  `--no-backup` unterdrueckt auch die Wochensicherung. Eine Sicherung kostet bis 13 Minuten (seitenweise Kopie bei
  offenem `-wal`). *Ueberholt:* vormittags am 2026-10-13 galt „nur auf Ansage mit `--backup`“, davor „bei jedem Deploy“.
- **Bestand am 2026-10-13 aufgeraeumt:** 96 alte Sicherungen (rund 36,8 GB) auf Ingos Entscheid geloescht. Behalten:
  `trafag_exporter.db.before-menue-hervorhebung-20261013-101656.bak` (neueste) und
  `trafag_exporter.db.before-verkauf-korrekturen-20261002-142056.bak` (rund zehn Tage alt). Nicht angefasst:
  Ordner `deploy_backups\` und zwei `.bak` unter `output\`.
- **Monitor-Fenster** (Wunsch Ingo): vor dem Deploy oeffnen, es liest das Protokoll nur mit:
  Protokollpfad in `.tmp_tools/DeployMonitor/DeployMonitor.logpath.txt` schreiben (**ohne BOM**, nicht mit `Set-Content -Encoding utf8`;
  sonst wartet das Fenster ewig, Fall 2026-10-16), dann
  `explorer.exe "<Repo>\.tmp_tools\DeployMonitor\ZeigeLetztenDeploy.cmd"`; nur ueber `explorer.exe` erscheint das
  Fenster auf Ingos Desktop (ein direktes `Start-Process` aus Claudes Shell blieb am 2026-10-13 unsichtbar).
  Danach der Runner mit Ausgabe in genau diese Datei. Das Fenster zeigt Schritt, Fortschrittsbalken, Laufzeit
  und die letzten Zeilen; der Kopf wird gruen (ohne Alarm), gelb (Alarm, Claude prueft) oder rot (abgebrochen),
  danach schliesst es sich nach 5 Sekunden selbst (Wunsch Ingo).
- **Runner beendet sich jetzt hart** (`Environment.Exit` am Ende). Vorher blieb er nach „Fertig“ haengen und
  sperrte `DeployConsole.dll` (2026-10-05 und zweimal am 2026-10-13). Haengt trotzdem einer, den naechsten Lauf
  mit eigenem Ausgabeordner bauen: `dotnet run --project .tmp_tools/DeployHeadless -c Release -p:BaseOutputPath=binmon/`.

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

- **Deploy 2026-10-27 10:02, Handbestellwesen: Freigabe wie PHP, umschaltbar (`a0030e8`).** Von Ingo per `!` gestartet. Keine Sicherung (Wochenregel), Suite 1291/1291, DLL `10:02:32`, `app_offline.htm` entfernt, `appsettings.json` auf dem Server mit `FreigabeNurGleicheKostenstelle` (false), HTTPS antwortet 401. Von Ingo um 10:03 ein zweites Mal gestartet (gleicher Commit): DLL bitgleich (SHA256 `659FAA4F...C0D621`), zehn Routen 200, Alarm nur WAL/SHM-Checkpoint beim Herunterfahren (harmlos; Protokollabsatz des Runners nennt einen alten Text). Im Browser noch nicht angesehen.
- **Deploy 2026-10-27 09:47, Handbestellwesen klassisch: Link „Zur neuen Version“ (`9ef7787`).** Von Ingo per `!` gestartet. Keine Sicherung (Wochenregel), Suite 1290/1290, DLL `09:47:27`, `app_offline.htm` entfernt, CSS mit `hb-k-neu` auf dem Server, HTTPS antwortet 401 (Anmeldung). Im Browser noch nicht angesehen.
- **Deploy 2026-10-27 08:26, Spend-Aufriss Schalter „Nur Einkaufsteile (Disponent 001 bis 005)“ (`c3147e1`).** Von Ingo per `!` gestartet. Keine Sicherung (Wochenregel, letzte 22.10.), Suite 1290/1290, DLL `08:26:53`, `app_offline.htm` entfernt, HTTPS antwortet 401 (Anmeldung). Im Browser noch nicht angesehen.
- **Deploy 2026-10-26 14:38, Spend-Aufriss Disponent (DISPOKENNZ wie ZC23) und Disponent Verwendung (ZLO03) (`bddf9f9`).** Von Ingo per `!` gestartet (Auto-Modus blockte), nach dem Einkauf-Delta. Keine Sicherung (Wochenregel), Suite 1289/1289, DLL `14:38:21`, `app_offline.htm` entfernt, HTTPS antwortet 401 (Anmeldung). Im Browser noch nicht angesehen.
- **Deploy 2026-10-26 09:39, Produktgruppe aus eigenem Disponenten wie ZM_OFFENE_KDAUF (`0dc3c68`).** Kein Einkauf-Lauf aktiv
  (den Delta, der die neue Spalte `MarcDispo` fuellt, startete Ingo danach von Hand; bis zu dessen Ende steht alles unter „ohne Produktgruppe“, Ergebnis auf Wunsch Ingo nicht nachgeprueft), keine Sicherung
  (Wochenregel), Suite 1288/1288, DLL `09:39:39` bitgleich, zehn Routen 200, Spalte im Schema (WAL) gesehen, Alarm nur
  WAL/SHM-Checkpoint.
- **Deploy 2026-10-23 09:05, Produktgruppen-Sichten ohne Deckelung (`8b4a93d`).** Kein Einkauf-Lauf aktiv, keine Sicherung
  (Wochenregel), Suite 1289/1289, DLL `09:05:47` bitgleich, zehn Routen 200, Alarm nur WAL/SHM-Checkpoint.
- **Deploy 2026-10-23 07:46, Spend-Aufriss Produktgruppe : Warengruppe (`c025b55`).** Kein Einkauf-Lauf aktiv (Start 12:30), keine
  Sicherung (Wochenregel), Suite 1288/1288, DLL `07:46:40` bitgleich, zehn Routen 200, Alarm nur WAL/SHM-Checkpoint.
- **Deploy 2026-10-22 15:48, HR-Cockpit Kader = Mitarbeiterkreis 10, 8,2 h (`ede56b6`).** Suite 1288/1288, DLL `15:48:48` bitgleich,
  zehn Routen 200, Alarm nur WAL/SHM-Checkpoint beim Herunterfahren.
- **Deploy 2026-10-22 15:22, Handbestellwesen Admin `gun` (`1f04d02`, nur appsettings).** DLL `15:22:54` bitgleich, zehn Routen 200,
  Alarm nur WAL/SHM-Checkpoint beim Herunterfahren; Server-`appsettings.json` zeigt `"AdminUsers": [ "koi", "gun" ]`.
- **Deploy 2026-10-22 14:52, Handbestellwesen moderne Seite `/bestellwesen` (`d7478cd`).** Suite 1286/1286, DLL `14:52:06`,
  SHA256 `8625EE08...986F1A28`, bitgleich, zehn Routen 200, ohne Alarm; `/bestellwesen` 200.
- **Deploy 2026-10-22 14:40, Handbestellwesen Bereinigung (`37e5d7b`).** Suite 1286/1286, DLL `14:40:44`, SHA256 `56B53211...C421E26A`,
  bitgleich, zehn Routen 200. Alarm: `-wal` (148 KB) und `-shm` beim Herunterfahren eingecheckpointet, `.db` +4 KB; danach neu angelegt.
- **Deploy 2026-10-22 14:22, Handbestellwesen Altdaten-Import (`3609b3e`).** Keine Sicherung (Wochensicherung 13:37). Release-
  Worktree auf `3609b3e`; Whisper-Quellen seit `798585d` per `Compile Remove="whisper\**"` ausgeschlossen. Suite 1285/1285 im
  Arbeitsbaum. DLL `14:22:44`, SHA256 `D0B51181...032972B7`, bitgleich, zehn Routen 200, Alarm nur `trafag_exporter.db-shm`.
  Runner mit `--no-build` gestartet, weil der Runner von 13:24 nach „Fertig“ haengen blieb (PID 23600, auch `taskkill /F`
  half 5 Min. nicht) und seine DLL sperrte; der Runner-Code war unveraendert. Dump liegt unter `hbdaten\import\db.sql`.
- **Deploy 2026-10-22 13:38, Einkauf-Feedback Erich/Sandro/Marco und Modul Handbestellwesen (`77778a5`), Freigabe Ingo.**
  Erst nach Ende des Einkauf-Deltas (gestartet 12:29, Vorberechnung danach, WAL 241 MB, nicht mehr wachsend). Wochensicherung
  `trafag_exporter.db.before-wochensicherung-20261022-132426.bak` (seitenweise, 797 s + 29 s). Release-Worktree auf `77778a5`,
  ohne Finance_All. Tests: Suite 1279/1279 im Arbeitsbaum am selben Commit (Runner-Text nennt die alte Zahl). DLL
  `13:38:46`, SHA256 `2B66E17B...BB2F42F`, bitgleich, zehn Routen 200, ohne Alarm. Server-Log: „Handbestellwesen: Datenbank
  bereit unter C:\inetpub\wwwcust\BiDashboard\hbdaten\handbestellwesen.db“. Nicht im Browser angesehen (Erweiterung nicht
  verbunden). Reiter Handbestellwesen wird unsichtbar angelegt.
- **Deploy 2026-10-19 09:54, 09:57 und 10:04, Shopfloor: Mengen auf dem Produktionstag, DW prod (`ded2b65`, `c4dca10`, `c3c45cd`).**
  WAL vorher 745 / 202 / 383 KB, Release-Worktree `1248` / `1249` / `1250` gruen vor jedem Publish, zuletzt `BiDashboard.dll` `10:04:49`,
  SHA256 `826E5E5FA59BA78FC895C12F445EF102B3B3122526BE8CCB997FECA225122E6E`, bitgleich, zehn Routen 200, Alarm nur WAL/SHM und Checkpoint.
  Danach 15. bis 19.10. per POST (`X-User`-Header noetig) neu geschrieben, Logistik-Tage 15./16./19.10. gespeichert, Sa/So leer.
  Hinweis: der Protokollabsatz des Runners nannte als Titel noch den alten Einkauf-Text; massgebend ist der Funktionscommit.

- **Deploy 2026-10-16 10:52, Handbuch Einkauf: Abschnitt Stuecklisten-Netz (`1823bac`).** `1248/1248`, bitgleich, zehn Routen 200, Alarm nur WAL/SHM
  (beide DBs); Kapitel 02 auf dem Server mit neuem Abschnitt.

- **Deploy 2026-10-16 10:48, Einkauf Spend-Aufriss: Stuecklisten-Netz „Gehirn“ (`d188806` + Uebersetzungen `d6ca646`).** WAL vorher 482 KB,
  Release-Worktree `1248/1248`, `BiDashboard.dll` `10:47:46`, SHA256 `AC22186FFB6AE7B6E37A1EED922A11D38A1F7218552FC106D8BDC7C256C046A6`,
  bitgleich, zehn Routen 200, Alarm nur WAL/SHM. In Chrome geprueft: Netz geladen, Klick C18947 (2'258 Verwendungen), Farbumschaltung.
  **Fehler davor:** Deploy 10:40 (`d188806`) lief parallel zum Testlauf, der 1 Fehler meldete (fehlende Uebersetzungen); Betrieb nicht
  gestoert (Rueckfall Deutsch), mit `d6ca646` behoben. Einkauf-Datenservice danach geprueft: kein abgebrochener Lauf.

- **Deploys 2026-10-16 10:17 und 10:19, Menue dezenter (`cf238ff`, `2a7d7c7`).** Wunsch Ingo „dezenter, kein Rosa“: gedeckte Rot-/Ziegeltoene,
  schwaecherer Glanz, langsamer Lauf; alle Kaesten mit gleichem Rand. Je `1247/1247`, bitgleich (zuletzt SHA256 `FC08CF88CE7A59167224EF571A975D9FCC5C4E583380B8FBC0B0DA976EFFA90E`),
  zehn Routen 200, Alarm nur WAL/SHM (auch shopfloor.db). In Chrome geprueft.

- **Deploy 2026-10-16 10:13, Menue: alle Hauptbereiche farbig (`1931084`).** WAL vorher 552 KB, `1247/1247`, `BiDashboard.dll` `10:12:58`,
  SHA256 `6306A270600003330EBFFD819BC82E09CB8F796E3B7BF5175F1002C4EBA2E0B6`, bitgleich, zehn Routen 200, Alarm nur `-wal`/`-shm`. In Chrome geprueft:
  Home orange pulsierend, Finance/HR/Einkauf/Verkauf/Logistik je eigener Rotton, Admin grau.

- **Deploy 2026-10-16 10:02, Handbuch Operations: Schnellueberblick mit Prozessgrafik, Bildzeilen (`2fd32d6`).** WAL vorher 573 KB,
  `1247/1247`, `BiDashboard.dll` `10:02:02`, SHA256 `256F65E1BCC031032C3D8150107E6E09BB943208DA0A29713F1655F3F50FC448`, bitgleich, zehn Routen 200,
  ohne Alarm. Reiter Handbuch in Chrome: Grafik geladen (1400 x 880), auch im Dunkelmodus auf weissem Grund; Word-Seite 116 mit Grafik.

- **Deploy 2026-10-16 09:51, Shopfloor: Mengen aus SAP eingeschaltet (`9d6b5d2`).** Nach P76-Pruefung (Vorschau gleich ZC27/FPY-Kontrollwerten).
  WAL vorher 358 KB, `1246/1246`, `BiDashboard.dll` `09:50:47`, SHA256 `DBD71E805EA19A1079FBECDA24E16046AFC599C769B815DC003039C1ACFB2A22`, bitgleich,
  zehn Routen 200, Alarm nur `-wal`/`-shm`. Erstlauf `POST shopfloor/api/sap/mengen?tag=2026-10-16` geschrieben (logistik, tr5, seh).

- **Deploy 2026-10-16 09:18, Benutzerhandbuch Kapitel Operations erweitert (`db76079`).** WAL vorher 185 KB, `1246/1246`,
  `BiDashboard.dll` `09:17:57`, SHA256 `1B5D8D7EFEFDC5078A9306FECA00025AA58A792A7A639FC753E8AD5C8C5FF023`, bitgleich, zehn Routen 200, Alarm nur
  `-wal`/`-shm` und Checkpoint beider DBs. Reiter Handbuch in Chrome geprueft (neue Abschnitte sichtbar), Word-Download 200.

- **Deploy 2026-10-16 09:00, Shopfloor: Mengen aus SAP mit Vorschau, Schreiben aus (`2bc54bd`, davor `180f9ef` leerer Tagessatz
  wird geloescht).** WAL vorher 544 KB, `1246/1246`, `BiDashboard.dll` `09:00:00`, SHA256
  `273CA91B9658CA1E30411F6274CC4B45B6C6F69DA43377C2A5307028496D2EAA`, bitgleich, zehn Routen 200, Alarm nur `-wal`/`-shm` (auch
  `shopfloor.db`). `GET shopfloor/api/sap/mengen?tag=2026-10-14` antwortet 502 „Set noch nicht transportiert“ (erwartet).

- **Deploy 2026-10-16 07:48, Operations ohne Login (`Shopfloor:OpenForAll = true`, `70a02b1`, Tests `1c03156`).** Auftrag Ingo. WAL vorher 136 KB,
  `1241/1241`, `BiDashboard.dll` `07:48:40`, SHA256 `FAB1D51FFF49F9F2231BFB988A02195BB6D0B12288D77EE5769228C2FEAF366A`, bitgleich, zehn Routen 200,
  ohne Alarm. Server-`appsettings.json` zeigt `OpenForAll: true`. Wirkung fuer andere Benutzer nicht selbst geprueft (Ingo ist in `AllowedUsers`).

- **Deploy 2026-10-16 07:37, Shopfloor: Ampel loeschbar, `planned_source` aus Fabios Paketen (`e6c007f`).** WAL vorher 556 KB, `1241/1241`,
  `BiDashboard.dll` `07:37:22`, SHA256 `F6013EE429E8AAE7420D1B6C8073EBAEBAE6FD641F245E4F1BD7AF05EE443944`, bitgleich, zehn Routen 200, Alarm
  nur `-wal`/`-shm`. Monitor-Pfaddatei ohne BOM. In Chrome geprueft: Ampel setzen und per zweitem Klick entfernen.

- **Deploy 2026-10-16 07:28, Shopfloor: ZD05-Abgleich 04:00 und 12:45 (`a285009`).** Entscheid Ingo. WAL vorher 260 KB, `1240/1240`,
  `BiDashboard.dll` `07:28:07`, SHA256 `30706F0782FEBEEBE734CBF5950A9E3F636687A51889806224CCA855D3B09D84`, bitgleich, zehn Routen 200,
  Alarm nur `-wal`/`-shm`. Server-`appsettings.json` zeigt `SapSyncTimes` 04:00/12:45, Handbuch Kapitel 10 und Word neu.

- **Deploy 2026-10-16 07:06, Shopfloor: Paket Fabio 2026-10-16 (`b2103a4`, Doku `5c1e29e`).** Auftrag Ingo („bitte einspielen“),
  WAL vorher 124 KB (kein Einkauf-Delta). Release-Worktree `1240/1240` (Titel/Testzahl im Runner-Protokoll sind die alten),
  `BiDashboard.dll` `07:03:00`, SHA256 `B207E6D61E44A4460C244D986C8B9882195388294198CABB6FFDE594258EEA9C`, bitgleich, zehn Routen 200,
  Alarm nur `-wal`/`-shm`. Nachpruefung in Ingos Chrome: `api/config` mit Ampel in zehn Modulen und DS-Praefixen, Seite
  „Lieferzeiten“, Ampel-Karte SEH, Forecast mit Zeile DS (DS1 40, DS2 7 Auftraege) und DW1 (52) ohne Abteilung; keine JS-Fehler.

- **Deploy 2026-10-15 12:48, Einkauf: Platzhalter NA ohne Anteil (`7b62544`, Doku `d4e8cdb`).** Auftrag Ingo. `1235/1235`,
  `BiDashboard.dll` `12:48:13`, SHA256 `018A5D4646E5C377D807C196FE6EEBF4C466F749B90503963FAC684987A02BEA`, bitgleich, zehn
  Routen 200, Alarm nur `-wal`/`-shm`. **WAL vorher 53,8 MB** (deutlich mehr als 14,5 MB um 12:12): ob ein Einkauf-Lauf
  abgebrochen wurde, ist nicht geprueft (Datenservice-Seite ohne Browser nicht lesbar); Ingo schaut nach.

- **Deploy 2026-10-15 12:15, Operations: Admin-Schalter „Login noetig“ (`8bfe424`, Doku `bed8097`).** Auftrag Ingo, Freigabe
  der Produktionsleitung fuer „offen fuer alle“ laut Ingo erteilt. `1234/1234`, `BiDashboard.dll` `12:15:07`, SHA256
  `AB65F17888DEED6DE1ED8A3343E42892793538A0E00DD68E026E6B76E707F84B`, bitgleich, zehn Routen 200, ohne Alarm. WAL vorher 14,5 MB
  (kein Hinweis auf einen grossen Einkauf-Lauf). Nachpruefung `/admin/passwoerter` und `/shopfloor/index.html` 200. Der Schalter
  steht auf der Vorgabe „Login noetig“, bis Ingo ihn umlegt.

- **Deploy 2026-10-15 11:06, Shopfloor: Push-Format aus Fabios Paket (`f4e3f1b`).** Auftrag Ingo („pass an und deploye“).
  `1233/1233`, `BiDashboard.dll` `11:05:37`, SHA256 `C14F44E349A462FA56373E3CAD4D62A74C9E3A6B32E113D207A5B73755C3A480`, bitgleich,
  zehn Routen 200, Alarm nur `-wal`/`-shm` (WAL 655 KB, kein grosser Schreiblauf). Nachpruefung: beide Push-Adressen mit falschem
  Token 401, kein Schreibaufruf.

- **Deploy 2026-10-15 10:56, Admin: Passwoerter aller Bereiche in der DB (`e1afd49`, Doku `1519cb3`).** Freigabe Ingo im Chat
  („du kannst deployen“), WAL vorher nur 259 KB, also kein Einkauf-Delta aktiv. `1232/1232`, `BiDashboard.dll` `10:57:21`,
  SHA256 `A76A1D688853B3A03541385556846BA57BA92EB9608A92DD447EA4A24AE926B6`, bitgleich, zehn Routen 200, Alarm nur `-wal`/`-shm`.
  Nachpruefung: `/admin/passwoerter` 200, `/shopfloor/api/login` mit falschem Passwort 401 (PostConfigure laeuft ohne Fehler).
  Die angemeldete Seite hat Claude nicht gesehen (Admin-Passwort nur bei Ingo). Folge: alle Operations-Anmeldungen einmal neu.

- **Deploy 2026-10-15 ~08:50, Shopfloor-Patches Fabio (`52b3b75`): ZD05 „Zu besprechen“, Verkauf Go-for-it.** Ingo bestaetigt:
  kein Einkauf-Delta aktiv. Release-Worktree `1224/1224`, DLL bitgleich, zehn Routen `200`, Alarm nur WAL/SHM (klein).
  Ausgelieferte `shopfloor/app.js` enthaelt `zdTalk` und `renderGoForIt` (per curl geprueft).

- **Deploy 2026-10-14 ~14:16, anonyme Nutzungsmessung, Admin > Nutzung (`4615268`).** Release-Worktree `1224/1224`, DLL
  bitgleich, zehn Routen `200`, Alarm nur WAL/SHM (WAL 1,1 MB). Tabellen `UsagePageViews`/`UsagePageUsers` entstehen beim ersten
  Schreiben. Doku `docs/NUTZUNG_2026-10-14.md`.

- **Deploy 2026-10-14 ~13:52, Startseite und Browser-Tab „Trafag Dashboard“ (`b5b4727`).** Release-Worktree `1218/1218`, DLL
  bitgleich, zehn Routen `200`, Alarm nur WAL/SHM (klein). Einkauf-Delta laut Ingo kurz vorher nicht aktiv.

- **Deploy 2026-10-14 ~13:46, Startseite Manometer-Schalter, Titel „Trafag Dashboard“ (`cc9c6ab`).** Vorher Ingo gefragt: kein
  Einkauf-Delta aktiv. Release-Worktree `1218/1218`, DLL bitgleich, zehn Routen `200`, Alarm nur WAL/SHM (WAL 0,6 MB). Neue Spalte
  `ExportSettings.ShowLandingManometer` (Standard 0) legt die Schemapflege beim Start an. Handbuch-Nachtrag Kapitel 10 ist damit live.

- **Deploy 2026-10-14 ~13:36, Operations KPI Monatsabruf abgesichert (`55b42d9`).** Release-Worktree `1217/1217`, DLL bitgleich,
  zehn Routen `200`, Alarm nur WAL/SHM. Produktiv geprueft: `letzter_abruf` neutral (`ok = null`), `warnung` leer (September da).

- **Deploy 2026-10-14 ~13:30, Startseite animierte Standortgrafik (`ce5d27d`).** Release-Worktree `1211/1211`, DLL bitgleich,
  zehn Routen `200`. **Vor dem Neustart war `trafag_exporter.db-wal` 241 MB gross:** ein grosser Schreiblauf war aktiv und wurde
  vermutlich abgebrochen; das Einkauf-Delta war vor dem Deploy nicht geprueft (LEARNINGS). Ingo hat die Grafik live gesehen („super“).

- **Deploy 2026-10-14, Kopfleiste nach Designpruefung (`5b51011`):** Dimmer mit sichtbarer Schiene (MudBlazor zeichnete
  sie bei Color.Default auf Rot unsichtbar), CPU-Anzeige 5 px flacher und Skala 9 px, Logo 8 px Abstand rechts. Release-Worktree
  `1205/1205`, DLL bitgleich, zehn Routen `200`, **ohne Alarm**. Vorschau mit MudBlazor- und App-CSS per Edge headless geprueft.

- **Sicherung 2026-10-14 11:36 ohne Deploy (Auftrag Ingo).** Neuer Schalter `--only-backup` im Runner
  (`.tmp_tools/DeployHeadless`): sichert und beendet sich, kein Publish, kein Neustart. Aufruf einzeln:
  `dotnet run --project .tmp_tools/DeployHeadless -c Release -- --only-backup`. Ergebnis
  `trafag_exporter.db.before-sicherung-20261014-113629.bak` (`614'862'848` Bytes), seitenweise (`-wal` offen) 727 s plus 60 s
  Uebertragung. Lokale Zwischenkopie in Temp danach geloescht. Die naechste Wochensicherung ist damit erst nach dem 21.10. faellig.

- **Deploy 2026-10-14 11:13, Benutzerhandbuch Kapitel Operations und Kopfleiste, Word neu (`f38020f`).** Release-Worktree,
  `1205/1205`, DLL bitgleich, zehn Routen `200`, Alarm nur `-wal`/`-shm`.

- **Deploy 2026-10-14 11:04, Startseite neuer Untertitel (`d911cb1`).** Release-Worktree, `1205/1205`, DLL bitgleich, zehn
  Routen `200`, Alarm nur WAL/SHM und Checkpoint beim Neustart.

- **Deploy 2026-10-14 11:00, Pegelanzeige Beschriftung „CPU LOAD“ (`5235e16`)** und **10:51 CPU-Pegelanzeige in der Kopfleiste
  (`a735b41`).** Release-Worktree, je `1205/1205`, DLL bitgleich, zehn Routen `200`. Alarme nur `-wal`/`-shm` und Checkpoint von
  `trafag_exporter.db` und `shopfloor.db` beim Neustart. Sichtpruefung: Vorschau mit gleichem Markup per Edge headless; die
  Produktivseite rendert headless nicht (Blazor-Verbindung), Ingo hat sie live angesehen („sieht gut aus“).

- **Deploy 2026-10-14 10:11, Operations KPI Fehlertext/Kodierung (`982e193`).** Release-Worktree, `1197/1197`, DLL bitgleich,
  zehn Routen `200`. Alarm nur `-wal`/`-shm` von `trafag_exporter.db` und `shopfloor\shopfloor.db` (Neustart). Keine Sicherung
  (Wochenregel, neueste vom 13.10.).

- **Deploy 2026-10-14 10:04, Operations: Menue Operations > KPI > OTD/OTIF/OTP und Shopfloor, Liefertreue (`89e9c7c`, `f51bdeb`),
  Shopfloor-Feldmapping (`b613f7c`).** Release-Worktree ohne Finance_All, `1196/1196`, DLL SHA256 `659F0D45...15AA` bitgleich,
  zehn Routen `200` inkl. `/operations/kpi` und `/operations/kpi/liefertreue`, Alarm nur `-wal`/`-shm`. Gleich danach war das VPN
  rund eine Minute weg (Freigabe und Server nicht erreichbar), ab 10:06 wieder da. Produktiv geprueft: KPI-API mit Startstand
  (OTD 90.6 %), Seite und Excel `200`, Schnittstelle 129 SAP / 5 MES, Shopfloor-Abgleich 10:05 nur noch **294** Auftraege ohne
  Abteilung (vorher 1'912). Liefertreue-Live-Abruf: HTTP 400 „No authority for query 2CCSDSLSORDDLVPRFQ“
  (`docs/OPERATIONS_KPI_2026-10-14.md` 3a). **Befehlsform:** Push und Deploy nur einzeln; am Vormittag lehnte der Auto-Modus einen
  mit Pipe und PowerShell gekoppelten `git push` ab (LEARNINGS).

- **Deploy 2026-10-13 14:43, Trafag Projekte privat, Leser, Wochen_Todo-Abgleich (`6cc0035`).** Release-Worktree, `1161/1161`, DLL bitgleich, acht Routen `200`, Alarm nur `-wal`/`-shm` (Checkpoint). Schema beim Start ergaenzt: `PmProjects.Visibility`, `PmTasks.ExternalRef`. **Sicherung mit `--backup` auf Wunsch Ingo abgebrochen** („dauert ewig“): Blockkopie wegen 144 KB ungeschriebener WAL-Seiten unbrauchbar, der seitenweise Weg ueber das Netz ist sehr langsam; keine halbe Datei auf dem Share; Rueckweg waere `before-menue-hervorhebung-20261013-101656.bak`. Ingo dazu: „Sicherung auf diesem PC würde reichen, der wird ja auch in Microsoft gesynct“; noch nicht umgesetzt (Kopie der Produktiv-DB auf den Laptop lehnte der Auto-Modus bisher ab; 614 MB Geschaeftsdaten im OneDrive sind eine Datenschutzfrage).

- **Deploys 2026-10-13 10:30 bis 11:00, Menue-Hervorhebung (Wunsch Ingo).** `fffe31b` 10:30 (mit Sicherung `...before-menue-hervorhebung-20261013-101656.bak`, 783 s; Selektor griff nicht, weil `MudNavLink` die Klasse auf das umschliessende `div.mud-nav-item` setzt), `bedc57e` 10:37 (Selektor korrigiert, erster Deploy ohne Sicherung), `1265ff9` 10:44 (Netzwerk mit Kabel, Trafag Reddit orange, Benutzerhandbuch hellgrau; Kabel unsichtbar, weil MudBlazors Ripple `::after` belegt, Netzwerk-Balken lief ueber den Rand), `50eef03` (Kabel auf `::before`, groesser, Breite begrenzt; vorher an einem lokalen Muster geprueft), `c8b91a7` (ohne `overflow: visible`, sonst ragte der Ripple heraus und das Menue bekam eine waagrechte Scrollleiste). Alle bitgleich, acht Routen 200. Alarme jeweils nur SQLite-Begleitdateien (`-wal`/`-shm` verschwunden, Hauptdatei beim Herunterfahren per Checkpoint geschrieben). Runner ab `bedc57e` mit `-p:BaseOutputPath=binmon/` gebaut (alter Runner hing) und beendet sich jetzt selbst.

- **Deploy 2026-10-13 09:47, HERMES-Darstellungsfix (`47990ad`, Phasenbalken volle Breite, kleiner Pfad ohne Ueberlappung).** Gestartet mit `--no-build`, weil der haengende Runner von 09:21 `DeployConsole.dll` sperrte und Ingo ihn ohne Adminrechte nicht beenden konnte; der Runner baut die App trotzdem frisch aus dem Release-Worktree (`Commit 47990ad` im Protokoll). Titel und Testzahl im Runner-Protokoll sind deshalb die alten; gueltig: Release-Tests `1158/1158`. DLL `09:47:20`, SHA256 `25553C00...99A7` bitgleich, acht Routen 200. Sicherung wiederverwendet (`...before-projekte-hermes-20261013-092117.bak`, Hauptdatei seit 09:18 unveraendert). „1 Alarm“ = nur `trafag_exporter.db-shm` veraendert (Neustart, bekannt). Sichtpruefung per Edge headless: Pfad in Tabelle und Einstellungen korrekt.

- **Deploy 2026-10-13 09:34, Trafag Projekte HERMES (`7e20f93`): HERMES-Angaben je Projekt, Reiter Portfolio (HERMES), Demoprojekt HDEMO.** Release-Worktree ohne Finance_All, Tests dort `1158/1158` (nach dem Deploy im Worktree nachgemessen; im Runner-Protokoll stand noch fest eingetragen `1150/1150` und der Titel des Vor-Deploys). DLL `09:35:01`, `9'916'416` Bytes, SHA256 `C07E6318...A81C` bitgleich, acht Routen 200 inkl. `/projekte`, „Fertig, ohne Alarm“. Sicherung `trafag_exporter.db.before-projekte-hermes-20261013-092117.bak` (seitenweise, `-wal` offen, 761 s). Sichtpruefung per Edge headless (CDP): HDEMO angelegt, Portfolio, Karte, Einstellungen und alle acht Ansichten ohne Fehler. Gefunden: Phasenbalken im HERMES-Pfad nur als Striche, kleiner Pfad in der Tabelle ueberlappt; Fix `47990ad`. *Danach:* Der Runner beendete sich nicht und sperrte `DeployConsole.dll` (Fall 2026-10-05); der Fix-Deploy brach deshalb beim Bauen ab, ohne den Server zu beruehren.


- **FALLE: Admin-Schalter in `appsettings.json` ueberlebt den naechsten Deploy NICHT.**
  `appsettings.json` ist Build-Ausgabe und wird bei jedem Publish durch den
  Repository-Stand ersetzt — gemessen am 2026-08-10, der Server stand auf
  `Pause:Enabled = true`, danach auf `false`. Betrifft alles, was zur Laufzeit dorthin
  geschrieben wird: `Pause:Enabled` und `LandingPage:ShowWalkingLabFigure`. Wer eine
  Einstellung dauerhaft will, muss sie im Repository setzen. Dieselbe Klasse wie die
  XLSX weiter unten. Vor jedem Deploy lohnt ein Vergleich der beiden Dateien; am
  2026-08-10 war der Pausen-Schalter der einzige Unterschied.
  *Nachtrag 2026-10-15:* Die Passwort-Hashes der Bereichs-Logins (Admin, Finance, HR,
  Operations, Logistik) waren dieselbe Falle: „Passwort ändern“ schrieb in
  `appsettings.json`, der naechste Deploy setzte den Repository-Hash zurueck. Seit
  `e1afd49` liegen sie in der Cockpit-DB (Tabelle `AccessPasswords`, Seite Admin Bereich >
  Passwoerter) und haben Vorrang; `appsettings.json` ist nur noch Rueckfall.


## Deployment-Historie

- Deploy-Eintraege vom 2026-08-03 bis 2026-10-09 stehen verbatim in `docs/archiv/DEPLOYMENT_KURZSTAND_ARCHIV_bis_2026-10-12.md`
  (verschoben 2026-10-22), die Erzaehlung dazu in `lastchange.md` und dessen Archiv. Diese Kurzdatei fuehrt im Kurzstand
  nur Deploys seit dem Deploy-Ablauf vom 2026-10-13 und die Fallen; aeltere Eintraege gehoeren ins Archiv.

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
