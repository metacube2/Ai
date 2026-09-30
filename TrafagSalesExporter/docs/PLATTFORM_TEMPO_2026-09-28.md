# Tempo der Webapp: sofort sichtbare Oberflaeche und Ladezeiten

Stand: 2026-09-28, zuletzt Abschnitt 6 (Management-Cockpit). Zurueck: `docs/router/plattform.md`.

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
| `PurchasingDashboardSnapshotCache` liefert einen abgelaufenen Stand sofort und rechnet im Hintergrund nach (stale-while-revalidate, single-flight) | Nach Ablauf wartet niemand mehr. Nach `Clear()` (Einkauf-Lauf) wird bewusst nicht der alte Stand geliefert |
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
| Export-Dashboard | 2,3 s | **offen** |
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
