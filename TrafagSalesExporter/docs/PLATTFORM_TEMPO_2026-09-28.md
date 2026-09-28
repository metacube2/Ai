# Tempo der Webapp: sofort sichtbare Oberflaeche und Ladezeiten

Stand: 2026-09-28. Zurueck: `docs/router/plattform.md`.

## 0. Stand auf einen Blick

| | |
| --- | --- |
| **Ausloeser** | Rueckmeldung der Nutzer laut Ingo: man sieht lange gar nichts von der Oberflaeche. Ingo: Tempo ist „der groesste Kritikpunkt an der ganzen Webapp", es muss ueberall schnell sein. |
| **Schritt 1, erledigt** | Oberflaeche erscheint sofort: Vorrendern aus, Ladebalken. Commit `4f2c62f`, **produktiv seit 2026-09-28 10:31**. |
| **Schritt 2, `/einkauf` umgesetzt, nicht deployed** | Kein einzelner Engpass (55 gleich teure Abfragen, lokal 12 s, Server 98,6 s). Stattdessen Stand sofort liefern und vorwaermen, Commit `d32a6aa`, Abschnitt 5. Uebrige Seiten noch nicht gemessen. |
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

### 5.2 Umsetzung, Commit `d32a6aa` (noch nicht deployed)

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
