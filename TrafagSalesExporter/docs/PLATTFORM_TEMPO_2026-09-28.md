# Tempo der Webapp: sofort sichtbare Oberflaeche und Ladezeiten

Stand: 2026-11-24, zuletzt Abschnitt 11.6 (erster Mitternachtslauf: 49 Min., davon Materialanteil 37 Min.); davor 2026-11-23, Abschnitt 11 (Dashboard zaeh am 20.11.: Einkauf-Delta war ein verkappter Full Load, Nachtslot und Deploy-Pruefung gebaut, nicht deployed; Vorberechnung seit 09.10. dreimal langsamer); davor 2026-10-01, Abschnitt 10 (Einkauf nach Delta-Lauf, `4e2b55a`); Grundmessung 2026-09-28. Zurueck: `docs/router/plattform.md`.

## 0. Stand auf einen Blick

| | |
| --- | --- |
| **Ausloeser** | Rueckmeldung der Nutzer laut Ingo: man sieht lange gar nichts von der Oberflaeche. Ingo: Tempo ist „der groesste Kritikpunkt an der ganzen Webapp", es muss ueberall schnell sein. |
| **Schritt 1, erledigt** | Oberflaeche erscheint sofort: Vorrendern aus, Ladebalken. Commit `4f2c62f`, **produktiv seit 2026-09-28 10:31**. |
| **Schritt 2: alles produktiv seit 13:32. Server-Einkaufsberechnung 104,7 s -> 24,4 s (Abschnitt 9), Cockpit lokal 41 s -> 3,3 s** | Kein einzelner Engpass (55 gleich teure Abfragen, lokal 12 s, Server 98,6 s). Stattdessen Stand sofort liefern und vorwaermen, Commit `d32a6aa`, Abschnitt 5. Uebrige Seiten gemessen (Abschnitt 6), Cockpit und Export-Dashboard behoben, SQLite-Cache (Abschnitt 7, `c35d3fa`). Server rund viermal schneller (Abschnitt 9). |
| **Verwandt** | Worker-Neustarts durch IIS-Leerlauf, ISS-017, `docs/EINKAUF_LAGERWERT_2026-08-18.md` Abschnitt 13.6. |

## 1. Ursache: das Vorrendern wartete auf alle Daten

`Components/App.razor` renderte `Routes` mit `InteractiveServer` **mit** Vorrendern. Dabei schickt
der Server die Seite erst, wenn jede Komponente ihr `OnInitializedAsync` beendet hat, also auch
Menue (`NavMenu` liest die Datenbank) und Rahmen. Solange eine Seite rechnete, blieb der Browser
weiss. Ausserdem lud jede Seite ihre Daten zweimal: einmal fuer das Vorrendern, einmal im Circuit.

## 2. Umsetzung, Commit `4f2c62f`

| Datei | Aenderung |
| --- | --- |
| `Components/App.razor` | `HeadOutlet` und `Routes` mit `new InteractiveServerRenderMode(prerender: false)`; schmale Ladeanzeige `#app-loading` vor `Routes` |
| `wwwroot/css/app.css` | Ladebalken oben, ausgeblendet ueber `body:has(.mud-layout)`, kein Overlay |
| `Components/PageLoadingBar.razor` (neu) | einheitlicher Ladebalken mit Text fuer Seiten |
| `Components/Pages/ManagementCockpit.razor` | `_initializing`, Ladebalken beim ersten Laden |
| `Components/Pages/HrKpi.razor` | `_loading` startet auf `true`, Ladebalken |
| `Components/Pages/PurchasingDashboard.razor` | Kacheln zeigen waehrend des Ladens „…" statt „wartet auf EKPO" |
| `Services/UiTextGeneratedTranslations.cs` | vier neue Texte in sechs Sprachen |

Entscheidungen:

- **Ein zentraler Schalter statt 20 Seitenumbauten.** Im Circuit zeichnet sich jede Komponente,
  sobald ihr `OnInitializedAsync` zum ersten Mal wartet. Layout, Menue und Ladezustand kommen damit
  vor den Daten. Die Seiten mit eigenem `@rendermode InteractiveServer` bleiben unveraendert gueltig.
- **Freischaltungen unberuehrt.** Finance-, Admin- und HR-Freischaltung lesen das Cookie ueber
  `IHttpContextAccessor`; das tun sie schon bisher auch im Circuit, sonst waere nach dem Vorrendern
  jeder Nutzer wieder gesperrt gewesen.
- **Ladeanzeige ohne Skript und ohne Overlay**, Lehre aus dem Reconnect-Overlay vom 2026-08-21.
- Nur dort eigener Ladezustand, wo der Zustand vor dem Laden wie echte Daten aussah.

Tests `730/730` im Arbeitsbaum, `715/715` im sauberen Worktree `C:\TMP\TrafagSalesExporter_release_4f2c62f`.
Lokal nicht gestartet: der Timer wuerde beim Start einen Nachhol-Export gegen die echten Systeme
ausloesen.

## 3. Messung vor und nach dem Deploy

Erste Antwort des Servers, `Invoke-WebRequest` mit Windows-Anmeldung:

| Route | vorher (10:28) | nachher (10:31) |
| --- | --- | --- |
| `/einkauf` | **98,60 s**, 108'129 Bytes | **0,11 s**, 4'756 Bytes (Huelle) |
| `/` | 0,30 s | 2,85 s beim ersten Aufruf nach Neustart |
| `/management-cockpit` | 0,27 s (Sperrpanel) | 0,01 s |

In der ausgelieferten Huelle nachgewiesen: `id="app-loading"`, `blazor.web.js`, Circuit-Marker.

**Wichtiger Befund: 98,6 s fuer `/einkauf` waren kein Kaltstart.** Der Worker lief seit 10:04
ohne Neustart. Die Einkaufsberechnung selbst braucht so lange, sobald der 15-Minuten-Snapshot
(`PurchasingDashboardSnapshotCache`) abgelaufen ist. Frueher dokumentierte „9-11 s warm" stimmen
fuer diesen Fall nicht. Seit `4f2c62f` sieht man die Seite sofort, **die Zahlen kommen aber
weiterhin erst nach dieser Zeit.** Das ist der erste Punkt fuer Schritt 2.

**Nicht belegt:** das sichtbare Verhalten im Browser. Die Chrome-Erweiterung war nicht verbunden.
Ingo prueft `/einkauf`, Management-Cockpit und HR selbst.

## 4. Naechste Schritte (Schritt 2)

1. Ingo sieht sich die Oberflaeche im Browser an: erscheint sie sofort, und fuellt sie sich sichtbar.
2. Ladezeit je Seite im Circuit messen, nicht nur die erste HTTP-Antwort (die ist jetzt immer schnell).
3. *Erledigt am 2026-09-28, siehe Abschnitt 5:* `/einkauf`: klaeren, welcher Teil der rund 100 s die Zeit kostet, und den Snapshot so halten,
   dass kein Nutzer die Neuberechnung abwartet (z. B. im Hintergrund vorberechnen statt beim Aufruf).

## 5. Schritt 2 fuer `/einkauf`: Messung und Umsetzung, 2026-09-28

### 5.1 Messung

Sonde `.tmp_tools/PurchasingLoadProfile0928` (nur lesend, gegen die lokale, gepruefte Kopie
der Produktiv-DB von 10:03): baut `PurchasingDashboardService` ohne Cache auf und schreibt jede
SQL-Anweisung ueber `sqlite3_profile` mit ihrer Laufzeit mit.

| | Wert |
| --- | --- |
| Gesamtzeit lokal, zwei Laeufe | **12,4 s** und **12,9 s** |
| davon SQL | 11,5 bis 12,6 s in **55 Anweisungen** |
| teuerste Einzelanfrage | 0,6 bis 0,8 s (offene Menge ueber EKET/EKPO/EKKO) |
| Server am selben Tag | **98,6 s** |

Es gibt **keinen Engpass**, den man gezielt beheben koennte: 55 Aggregationen ueber denselben
Einkaufscache, jede 0,2 bis 0,8 s. Der Server ist fuer dieselbe Arbeit rund achtmal langsamer
als der Entwicklungsrechner; die Ursache (CPU, Speicher, Platte der VM) ist nicht gemessen.

### 5.2 Umsetzung, Commit `d32a6aa`, produktiv seit 2026-09-28 11:25

| Aenderung | Wirkung |
| --- | --- |
| `PurchasingDashboardSnapshotCache` liefert einen abgelaufenen Stand sofort und rechnet im Hintergrund nach (stale-while-revalidate, single-flight) | Nach Ablauf wartet niemand mehr. Nach `Clear()` (Einkauf-Lauf) wird bewusst nicht der alte Stand geliefert *(ueberholt am 2026-10-01: seit `4e2b55a` wird der alte Stand mit Hinweis geliefert, Abschnitt 10)* |
| Lebensdauer 60 statt 15 Minuten | Die Daten aendern sich nur durch Einkauf-Laeufe, die den Cache leeren; spart Rechenzeit auf dem Server |
| `TimerBackgroundService` waermt `PurchasingDashboardFilter.Default` vor: eine Minute nach dem Start, danach alle fuenf Minuten, nur in Produktion | Die Standardansicht ist fertig, bevor jemand sie oeffnet, auch nach Neustart, Deploy und Einkauf-Lauf |

Warten muss damit nur noch, wer **in der ersten Minute nach einem Neustart** oder **mit einem
eigenen Filter** kommt. Ein eigener Filter rechnet einmal, danach gilt auch fuer ihn der Cache.

Tests: 3 neue in `PurchasingDashboardSnapshotCacheTests`, `742/742` im Arbeitsbaum.

**Offen:** die eigentliche Rechenzeit auf dem Server. Der Hebel dort waere, die 55 Abfragen
parallel oder zusammengefasst auszufuehren, oder die Ursache der achtfachen Serverlangsamkeit zu
finden. Beides erst, wenn sich zeigt, dass eigene Filter oft genug gebraucht werden.

## 6. Uebrige Seiten gemessen, Management-Cockpit behoben, 2026-09-28

### 6.1 Messung

Sonde `.tmp_tools/PageLoadProfile0928`: startet die Anwendung ueber `WebApplicationFactory`
gegen eine lokale Kopie der Produktiv-DB, **ohne jeden Hintergrunddienst** (kein Timer-Export,
kein SAP, kein Upload), und ruft die Ladefunktionen genau wie `OnInitializedAsync` der Seiten.
Seit dem Abschalten des Vorrenderns misst eine HTTP-Anfrage nur noch die Huelle, deshalb dieser
Weg. Werte lokal, zweiter (warmer) Lauf:

| Seite | vorher | nachher |
| --- | --- | --- |
| Menue (jede Seite) | 0,04 s | unveraendert |
| Standorte / Manuelle Importe | 0,01 s | unveraendert |
| Einkauf-Status (jede Einkaufsseite) | 0,03 s | unveraendert |
| HR-KPI (alte Dateien aus `C:\temp`) | 0,3 bis 1 s | unveraendert |
| Marktsegmente | 0,5 s | unveraendert |
| Supply Chain (Standard) | 1,2 s | unveraendert |
| Export-Dashboard | 2,3 s | **offen** *(erledigt in `c35d3fa`, 0,07 s, Abschnitt 7)* |
| **Management-Cockpit** | **41 s** | **3,7 s** |

Der Server ist fuer dieselbe Arbeit rund achtmal langsamer (Abschnitt 5.1). Das Cockpit lag dort
also im Bereich von **mehreren Minuten**, und das war vermutlich der groesste Einzelgrund fuer die
Rueckmeldung „es kommt lange nichts".

### 6.2 Ursache im Cockpit, mit Profil belegt

`dotnet-trace` (als Benutzer-Tool installiert) ueber den Cockpit-Lauf, ausgewertet mit einem
kleinen Node-Skript: **92 % der Finanzauswertung** lagen in
`CurrencyExchangeRateService.ResolveRate`. Die Methode oeffnete bei **jedem** Aufruf eine neue
Datenbankverbindung und stellte zwei bis sechs Abfragen. Das Cockpit ruft sie fuer jede der
109'508 Verkaufszeilen mehrfach auf (Audit-Ledger, Pivot, Konzernmarge) - mehrere hunderttausend
Abfragen je Oeffnen, fuer eine Kurstabelle, die sich fast nie aendert. Dazu las jedes Oeffnen die
Audit-CSVs neu ein (lokal 3 bis 4 s, auf dem Server rund 80 MB), weil deren Cache nur 10 Sekunden galt.

### 6.3 Behebung, Commit `5a26596` (produktiv seit 2026-09-28 13:32)

| Aenderung | Wirkung |
| --- | --- |
| `CurrencyExchangeRateService` laedt die aktiven Kurse einmal und sucht im Speicher; jedes Ergebnis je (von, nach, Tag) wird gemerkt. Suchregeln unveraendert | Finanzauswertung lokal 37,5 s -> 2,8 s |
| Frische: Stand hoechstens 10 s; Import, Einstellungen und Konfigurationstransfer rufen nach dem Speichern `NotifyRatesChanged`, dann wird sofort neu geladen | Ein geaenderter Kurs wird nie veraltet verwendet |
| `ManagementCockpitService` haelt die zentralen Datensaetze, solange sich die Quelle nicht aendert (Dateiliste mit Groesse und Schreibzeit bzw. Zeilenzahl der Tabelle), hoechstens 30 Minuten | Kein erneutes Einlesen der CSVs bei jedem Oeffnen; ein neuer Tagesexport wird sofort erkannt |

Tests: 6 neue (`CurrencyExchangeRateServiceTests`, `ManagementCockpitCentralRecordsCacheTests`),
`748/748`. Die Kurs-Suche ist nicht nur fuer das Cockpit schneller, sondern ueberall, wo
`ResolveRate` in Schleifen laeuft (Konzernmarge, Exporte).

### 6.4 Was offen bleibt

- *Erledigt, siehe Abschnitt 7:* Export-Dashboard mit 2,3 s lokal, auf dem Server vermutlich um die 20 s.
- Die achtfache Langsamkeit des Servers selbst. Ein Anfang waere ein groesserer SQLite-Cache pro
  Verbindung (`PRAGMA cache_size`, `mmap_size`); ob das wirkt, zeigt die Vorwaermzeit im
  Serverprotokoll (heute 104,7 s). Sonst CPU, Speicher oder Platte der VM, das ist Sache der IT.
- Der erste Cockpit-Aufruf nach einem Neustart rechnet zusaetzlich die Einkaufsansicht, sofern der
  Vorwaermer sie noch nicht fertig hat (lokal 14 s), und liest die CSVs einmal ein.

## 7. Export-Dashboard und SQLite-Cache, 2026-09-28, Commit `c35d3fa`

**Export-Dashboard:** Das Profil zeigte nur 0,7 s Rechenzeit. Der Rest war Warten auf SharePoint:
bei jedem Oeffnen wird fuer jeden Standort die neueste `Sales_ProcessedMergeInput_*.csv` gesucht
(parallel ueber die Standorte, aber je Standort rund 1,5 s). Die Dateien aendern sich nur beim
Tagesexport. `DashboardPageService` merkt sich die Antworten deshalb fuenf Minuten (statisch, weil
der Dienst je Circuit neu entsteht); Fehler werden nicht gespeichert. Warm lokal **2,3 s -> 0,07 s**.

**SQLite-Cache je Verbindung** (`Data/SqlitePerformanceInterceptor.cs`, in `Program.cs` registriert):
64 MB Seitencache statt rund 2 MB, 256 MB Memory-Mapping, Temp-Daten im Speicher. Gesetzt ueber
`DbConnection.StateChange`, weil die meisten Dienste die Verbindung selbst oeffnen und die
Opened-Hooks von EF dann nicht feuern. Lokal zusaetzlich schneller: Supply Chain 1,2 -> 0,7 s,
Marktsegmente 0,55 -> 0,31 s, erste Einkaufsberechnung 14 -> 10,5 s. **Ob es die achtfache
Serverlangsamkeit trifft, zeigt erst die Vorwaermzeit im Serverprotokoll nach dem Deploy**
(Vergleichswert 104,7 s am 2026-09-28 11:28).

Stand aller Seiten lokal, warm, nach diesem Commit:

| Seite | Zeit |
| --- | --- |
| Menue, Standorte, Einkauf-Status | unter 0,05 s |
| Export-Dashboard | 0,07 s |
| HR-KPI | 0,3 s |
| Marktsegmente | 0,3 s |
| Supply Chain | 0,7 s |
| Management-Cockpit | 3,3 s |
| Einkauf (Standardansicht, vorgewaermt) | sofort aus dem Cache |

Tests `749/749`. Die Finance_All-Zeile in `Program.cs` ist nicht Teil des Commits.

## 8. Finance-Seiten und Stuecklisten-Analyse gemessen, 2026-09-28

| Seite | vorher | nachher |
| --- | --- | --- |
| Finance-Regeln | 0,00 s | unveraendert |
| Stuecklisten-Analyse | 0,00 s (Cache) | unveraendert |
| Finance Journal-Import (Status) | 0,3 s | unveraendert |
| **Finanzvergleich** | **3,7 s** | **2,4 s** |
| Einlesen der Audit-CSVs (auch Cockpit nach neuem Tagesexport) | 3,3 s | **1,8 s** |

Profil des Finanzvergleichs: der groesste Posten war `ExportAuditCsvService.NormalizeHeader`. Es
normalisierte den festen Spaltennamen bei jedem Feldzugriff jeder Zeile neu, also Millionen Mal.
Dazu kam `FinanceReconciliationService.NormalizeRuleText`, das je Verkaufszeile und Konzernregel
lief. Beide merken sich jetzt ihr Ergebnis je Text, Commit `977a198`. Offen:
der Finanzvergleich las die CSVs bei jedem Oeffnen neu ein. *Erledigt am 2026-09-28 in `eaa9d43`:* er haelt die Datensaetze jetzt wie das Cockpit, solange die Quelle gleich ist (hoechstens 30 Minuten). Produktiv seit 28.09. 13:50.

## 9. Ergebnis auf dem Server nach dem Deploy von 13:32

Die Vorwaermzeit der Einkauf-Standardansicht im Serverprotokoll ist der Vergleichswert, weil
dieselbe Berechnung jedes Mal gleich laeuft:

| Zeitpunkt | Stand | Zeit |
| --- | --- | --- |
| 28.09. 11:28 | ohne SQLite-Cache, kurz nach Deploy | 104,7 s |
| 28.09. ~12:00 | ohne SQLite-Cache | 86,9 s |
| 28.09. ~13:2x | ohne SQLite-Cache, **gleichzeitig mit dem Einkauf-Delta** | 303,1 s |
| **28.09. 13:34** | **mit SQLite-Cache (`c35d3fa`)** | **24,4 s** |

**Der SQLite-Cache macht den Server rund viermal schneller.** Die Vermutung aus Abschnitt 5.1
ist damit weitgehend bestaetigt: mit rund 2 MB Seitencache las jede Abfrage von der Platte der
VM. Der Abstand zum Entwicklungsrechner (12 s) ist von Faktor 8 auf Faktor 2 geschrumpft; der Rest
ist CPU oder Platte der VM. Da alle Seiten ueber dieselbe Datenbank laufen, profitieren sie alle.

Nebenbefund: gleichzeitig mit einem Einkauf-Lauf dauerte die Vorberechnung 303 s. Nach Ablauf des
Snapshots rechnet der Cache im Hintergrund nach, auch wenn gerade ein Lauf schreibt. Das stoert
niemanden, weil der alte Stand solange ausgeliefert wird.

## 10. Rueckmeldung Armin zum Einkauf, 2026-09-30

Ingo hat Armin gefragt, ob die Einkaufsseite jetzt schneller ist. Antwort sinngemaess: viel
schneller als vorher, absolut aber noch nicht „turbo", auch beim Auffrischen.

Einordnung gegen die Messungen: Das Oeffnen der Standardansicht kommt vorgerechnet aus dem Cache
(Abschnitt 5). Gewartet wird noch in drei Faellen, jeweils auf die volle Serverrechnung von rund
24 s (Abschnitt 9): (1) nach **„Delta aktualisieren"**, weil der Lauf den Cache leert und bewusst
nicht den alten Stand zeigt; (2) beim ersten Aufruf mit eigenem Filter; (3) in der ersten Minute
nach Neustart oder Deploy. „Auffrischen" meint sehr wahrscheinlich Fall 1.

Vorschlag an Ingo, **noch nicht entschieden**: (a) waehrend der Neuberechnung nach dem Delta den
alten Stand mit Hinweis „wird aktualisiert" zeigen (wenig Aufwand, trifft Armins Punkt; Preis: 20
bis 30 s lang die Zahlen vor dem Delta); (b) die 55 Abfragen zusammenfassen oder parallel ausfuehren
(mehr Aufwand, hilft auch bei eigenen Filtern). Die Zurueckhaltung aus Abschnitt 5.2 („erst, wenn
eigene Filter oft genug gebraucht werden") ist mit dieser Rueckmeldung zu ueberdenken.

*Entschieden und umgesetzt am 2026-10-01 (Ingo: „mach mal a“), produktiv 11:21, `4e2b55a`:* `PurchasingDashboardSnapshotCache.Clear()` behaelt den Stand je Filter als Vorgaenger. Ein Aufruf ohne neuen Stand bekommt sofort den alten, die Neuberechnung laeuft im Hintergrund, `IsPrevious` meldet das der Seite. Die Seite zeigt den Hinweis „Neue Einkaufsdaten werden eingerechnet ...“ und laedt alle 5 s nach (hoechstens 5 Minuten), bis die neuen Zahlen da sind. Ein Filter ohne frueheren Stand wartet wie bisher. Tests im Cache-Test, 816/816. Nebenwirkung: die Logzeile „Einkauf-Standardansicht vorberechnet in ... s“ erscheint nach einem Lauf nicht mehr, weil das Vorwaermen sofort den alten Stand bekommt; die Rechenzeit selbst ist damit nicht mehr im Protokoll. **(b) bleibt offen** (Abfragen buendeln, zuerst messen).

## 11. Dashboard zaeh am 2026-11-20 nachmittags: Einkauf-Delta, Ursachen und Umbau (2026-11-23)

*Auftrag Ingo 20.11. („anscheinend ist das Dashboard extrem langsam, was ist los“, danach „mache das alles, ev Anzeige im
Dashboard Achtung ... laeuft, ist langsam“). Belegt mit den stdout-Protokollen des Servers (718 Dateien seit 28.09., einmal in den
Scratchpad kopiert), ohne Produktiv-DB.*

### 11.1 Was am 20.11. passiert ist

| Zeit | Ereignis |
| --- | --- |
| 12:31 | Taeglicher Einkauf-Delta startet hinter dem 12:00-Export |
| 12:51 | Deploy Shopfloor Patch 10 startet die App neu, der Delta stirbt (im Deploy-Eintrag stand „Einkauf-Delta lief nicht“) |
| 13:23 | Zweiter Deploy (Finance Stufe 2) |
| 13:39 | Nachhol-Lauf startet in der Arbeitszeit |
| 15:09 | Delta fertig: „geaenderte Belege=178'505“ bei 178'505 Belegen gesamt |

### 11.2 Ursachen

1. **Der Delta war seit langem ein verkappter Full Load.** `EKKOSet` reicht `$filter` nicht an den Data Provider durch
   (`saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`, Abschnitt zu `set_filterable`). `Aedat ge '<gestern>'` lieferte alle Koepfe,
   danach wurden EKPO und EKET fuer jeden Beleg in Paketen zu 20 nachgeladen: rund 18'000 Anfragen, 90 Minuten. Am 19.11.
   dasselbe Bild (178'479).
2. **Der Delta lief mittags**, im Timer-Slot hinter dem Verkaufsexport, also in der Arbeitszeit und mitten in den ueblichen
   Deploy-Zeiten.
3. **Die Deploy-Pruefung „kleine -wal-Datei = kein Delta“ ist blind:** der Delta liest zuerst ueber eine Stunde aus SAP und
   schreibt erst am Ende.
4. **Die Einkauf-Vorberechnung ist seit dem 09.10. rund dreimal langsamer** (Abschnitt 11.4). Das ist eine zweite, vom Delta
   unabhaengige Last.

*Berichtigung 2026-11-23:* Im Chat vom 20.11. stand, der Delta habe die Datenbank 90 Minuten „belegt“. Geschrieben wird erst am
Ende; belastet haben ihn die SAP-Anfragen und die parallel laufende Vorberechnung (196,7 s statt rund 100 s waehrend des Laufs).

### 11.3 Umgesetzt im Zweig `einkauf-tempo-2011` (`e76df02`, `d4e9a18`, `155276e`), deployed 2026-11-23 (siehe 11.5)

- Delta im eigenen **Nachtslot** (zuerst 03:00, *seit 155276e 00:00, nachgeholt nur bis 06:00, siehe 11.5*) (`Purchasing:DeltaHour`/`DeltaMinute`), Start immer ueber den Runner (Laufsperre).
  Nachholen, wenn seit dem juengsten Slot kein Erfolg da ist, hoechstens ein Versuch je Slot und Prozess. Der Worker lief nicht
  jede Nacht (19.11. 21:20 bis 20.11. 06:21 kein Prozess); dann holt der erste Start am Morgen den Lauf nach.
- **Echtes Delta:** Auswahl in der App (`PurchasingDeltaSelection`): Belege, die im Cache fehlen oder deren `Aedat` ab dem
  Stichtag liegt, dazu wie bisher alle Belege mit offener Menge. Stichtag = letzter erfolgreicher Lauf minus ein Tag. Die
  Erfolgsmeldung nennt jetzt die Dauer je Abschnitt. Erwartung (nicht gemessen): die offenen Belege (am 20.11. 28'723) bestimmen
  die Dauer, rund ein Sechstel der bisherigen Anfragen. Die erste Meldung nach dem Deploy liefert die echte Zahl.
- **Deploy-Pruefung aus dem Serverprotokoll:** der Runner schreibt „Einkauf-Lauf {Mode} gestartet“, `PurchasingRunLog` wertet
  Start gegen Ende aus, `DeployHeadless` bricht bei laufendem Lauf ab (`--trotz-einkauf-lauf` uebersteuert).
- **Hinweisbalken** oben auf jeder Seite, solange ein Einkauf-Lauf oder Verkaufsexport laeuft. Die Wochensicherung der
  Datenbank laeuft im Deploy-Werkzeug ausserhalb der App und erscheint dort nicht. Im Browser noch nicht gesehen.
- **Sonntags 03:00 ein Full Load** statt Delta (`Purchasing:WeeklyFullLoadDay`, leer oder `none` schaltet ab). Grund: das
  echte Delta laedt nur neue und offene Belege; Aenderungen an abgeschlossenen Bestellungen (Preis, Loeschkennzeichen, Storno
  eines Wareneingangs, neue Position) kamen bis zum 20.11. nur, weil der Delta unbeabsichtigt alles neu lud. Der Full Load liest
  die drei Tabellen in ganzen Seiten statt je 20 Belege. Neu: er ersetzt den Bestand nicht, wenn SAP weniger als die Haelfte
  der bisherigen Bestellkoepfe liefert (Review 20.11. E9). Ein verpasster Sonntag wird nur als Delta nachgeholt.
- **Fehler des neuen Timer-Fangnetzes vor dem Commit behoben:** die erste Fassung haette nach einem DB-Fehler eine Stunde
  spaeter einen Verkaufsexport ausgeloest. Jetzt: kein Export, neuer Berechnungsversuch nach 15 Minuten.
- **Grenzen:** Ein gescheiterter Nachtlauf wird bis zur naechsten Nacht oder bis zu einem Neustart nicht wiederholt (hoechstens
  ein Versuch je Slot und Prozess). Ob SAP um 03:00 erreichbar ist, ist nicht geprueft. Der erste Deploy startet sofort einen
  Nachhol-Delta, falls seit dem letzten 03:00 kein Lauf erfolgreich war.

### 11.4 Vorberechnung: ein Teil behoben, die Verdreifachung offen

Median der Vorberechnung je Prozess (ohne den ersten Lauf nach dem Start), aus den Serverprotokollen:

| Zeitraum | Median im laufenden Betrieb |
| --- | --- |
| 28.09. bis 08.10. | 33 bis 40 s |
| 09.10. bis 23.10. | 121 bis 150 s |
| 26.10. | 37 s, danach wieder steigend |
| 29.10. bis 20.11. | 97 bis 121 s |

**Behoben (Teil 1): Benutzer warteten stuendlich die ganze Neuberechnung ab.** Die Logzeile erscheint rund einmal pro Stunde,
auch an Sonntagen, also im Takt der Lebensdauer von 60 Minuten. Laut Abschnitt 10 haette der Aufrufer nach Ablauf aber sofort
den alten Stand bekommen sollen. Ursache: der Cache startete die „Hintergrund“-Berechnung direkt aus dem Aufruf, und die
Berechnung laeuft trotz async-Signatur synchron (SQLite-Aufrufe sind auch als *Async sofort fertig). Wer nach Ablauf als
Erster `/einkauf` oeffnete, wartete deshalb die ganze Berechnung ab, rund 100 s. Nachgewiesen mit einem Test, der genau das
zeigte (2,0 s Wartezeit bei einer synchronen 2-s-Berechnung), behoben mit `Task.Run` in
`PurchasingDashboardSnapshotCache.StartOrJoinLoad`. Drei Cache-Tests setzten die alte, synchrone Reihenfolge voraus und
wurden reihenfolgeunabhaengig gemacht; einer davon blockierte sich sonst selbst. *Die erste Vermutung (ganzer Cache durch
`TrimIfNeeded` geleert) war falsch: die Zeile kommt auch sonntags ohne Benutzer.*

**Offen (Teil 2): die Berechnung selbst dauert seit dem 09.10. dreimal so lang.** Am 09.10. wurden Trafag Projekte, Trafag
Reddit und der Shopfloor-SAP-Abgleich ausgeliefert; keiner aendert den Einkauf-Code, der Abgleich laeuft nur zu festen Zeiten.
Am 26.10. fiel die Dauer einmal auf 37 s und stieg danach ueber Wochen wieder an, was eher nach Datenwachstum aussieht
(Datenbank 613 MB am 13.10., 690 MB am 20.11.). Pruefen laesst sich das nur mit echten Daten: Zeitmessung je Abfrage gegen eine
Kopie der Wochensicherung vom 16.11. Dafuer braucht es Ingos Freigabe (Produktivdaten). Nach Teil 1 rechnet das weiterhin
stuendlich im Hintergrund, aber niemand wartet mehr auf diese stuendliche Neuberechnung (nur die allererste Berechnung eines
Filters nach einem Neustart wartet weiter).

### 11.5 Deploy 2026-11-23 und Abbruch des Nachhol-Laufs

- **07:37 Deploy `d4e9a18`.** Um 07:38 startete der Nachhol-Delta (letzter Erfolg 20.11. 15:10, am Wochenende kein Erfolg).
  Der Hinweisbalken erschien auf `/einkauf` wie geplant. Der Lauf brauchte aber deutlich laenger als geschaetzt: nach 58 Minuten
  war er noch nicht fertig (WAL um 08:34 bei 193 MB, er schrieb also schon). Die Dauer je Abschnitt ist nicht bekannt, weil die
  Meldung erst am Ende kommt. Damit ist offen, ob das echte Delta schneller ist als der alte Lauf von 90 Minuten.
- **Wunsch Ingo 08:30:** „beende den Delta, er blockiert weitere Arbeiten, soll Mitternacht laufen“. Ein Abbruch geht nur ueber
  einen Neustart; ein blosser Neustart haette den Nachhol-Lauf nach 30 Sekunden wieder gestartet.
- **08:36 Deploy `155276e`** mit `--trotz-einkauf-lauf`: Slot 00:00, Nachholen nur bis 06:00 (`PurchasingCatchUpWindow`). Der
  Runner setzte den Lauf beim Start auf „Abgebrochen“; ein neuer Lauf startete nicht. Tests 1'817/1'817.
- **Folge:** Der Einkaufsstand bleibt bis heute Nacht der vom 20.11. 15:10 (plus was der abgebrochene Lauf schon geschrieben hat).
  Der erste Mitternachtslauf zeigt die Dauer je Abschnitt in der Erfolgsmeldung (Datenservice-Seite, Admin-Log).
- **Selbstkritik:** Dass der erste Deploy tagsueber sofort einen Nachhol-Lauf ausloest, stand im Chat vorher als Hinweis, war
  aber kein Grund, den Deploy zu verschieben oder das Nachholen tagsueber zu sperren. Genau das ist jetzt nachgezogen.

### 11.6 Erster Mitternachtslauf 2026-11-24 (aus dem Serverprotokoll gelesen am 24.11.)

Quelle: `stdout_20261123133622_9920.log` (Prozess seit dem Deploy 14:36), Zeilen „Einkauf-Lauf Delta gestartet/beendet“.

- Gestartet 00:00:06 im Slot, erfolgreich beendet; Ladestand im Dashboard „2026-11-24 00:49“ bzw. Materialanteil „24.11.2026 00:48“.
- **Dauer rund 49 Minuten** (Summe der Abschnitte 2'965 s), gegen rund 90 Minuten beim alten verkappten Full Load (11.1).
- Abschnitte: EKKO lesen 318 s, EKPO/EKET fuer 28'755 Belege 323 s, Stammdaten 27 s, Cache schreiben 18 s, Kontrakte/LZ 6 s,
  Dispo 1 s, **Materialanteil 2'240 s (37 Min., 76 % des Laufs)**, Lagerwert 32 s.
- Mengen: Belege aus SAP 178'539, davon neu oder ab 19.11. 92, offene Belege nachgeladen 28'728; Materialanteil 31'051
  ZC23-Summen (8'895 verkuerzte Nummern), 229'438 Stuecklistenzeilen Disponent 001 bis 006.
- **Folgerung:** Der Delta-Teil selbst ist mit rund 11 Minuten schnell. Was die Nacht lang macht, ist der Materialanteil
  (ZLO03 Top-Down ueber alle verkuerzten Nummern). Ihn bei Bedarf nur sonntags rechnen waere der naechste Hebel; Entscheid Ingo.
- *Offene Frage, nicht geprueft:* Die Meldung sagt „Stammdaten aktualisiert auf=0 Cachezeilen“. Ob das beim Delta so gewollt ist
  (keine geaenderten Stammdaten) oder ein Fehler, ist im Code nicht nachgesehen.
- Naechste Pruefung: Sonntag 29.11. 00:00 der erste automatische Full Load, am 30.11. Dauer und Erfolg.
