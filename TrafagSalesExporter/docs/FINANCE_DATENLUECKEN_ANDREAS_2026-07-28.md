# Datenluecken 2026 — Andreas' rote Markierungen geprueft

> **STATUS UEBERHOLT. Stand der Feststellung: 2026-08-13.**
> Gemessen auf `84'788` Zeilen vom 2026-07-27, heute sind es `97'537`. Von Andreas' vier
> roten Bereichen ist nur die spanische Luecke Januar bis Mai 2026 noch offen; UK 2025 ist
> am 2026-08-11 abgenommen, und die CH/AT-Luecke ist seit dem taeglichen SAP-Batchjob vom
> 2026-08-12 geschlossen. NEU und hier noch nicht enthalten: Frankreich liefert seit dem
> 2026-07-30 keine Rechnung mehr. Gueltiger Status:
> `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`.

Stand: 2026-07-28

Anlass: Andreas hat in einer Pivot-Auswertung (`docs/Bild.png`, Summe je TSC / Jahr / Monat)
vier Bereiche rot markiert und als „falsch" bezeichnet. Alle vier sind auf Produktivdaten
bestaetigt; die Ursachen sind unterschiedlich.

Datenbasis: produktive `trafag_exporter.db` vom Server, Stand **2026-07-27 13:16**,
read-only Kopie ausgewertet. Tabelle `CentralSalesRecords` (`84'788` Zeilen).

## Zusammenfassung

| Rot markiert | Befund auf Produktivdaten | Ursache | Status |
| --- | --- | --- | --- |
| TRCH 2026 ab ca. Mai | Zeilen je Monat: Jan 2'662, Feb 2'616, Mrz 2'643, **Apr 1'409, Mai 47, Jun 43, Jul 87** | ZSCHWEIZ liest vom **TEST**-Server `travt762` | Ursache identifiziert, Fix bekannt |
| TRAT 2026 ab ca. Mai | Jan 115, Feb 78, Mrz 88, **Apr 65, Mai 0, Jun 8, Jul 1** | dieselbe Quelle, dieselbe Ursache | dito |
| TRES 2026 Jan–Apr | **0 Zeilen** Jan–Apr; Mai nur 35, Jun 542, Jul 357 | Spanien-Range-Export beginnt erst **28.05.2026** | fehlender Export, kein Bug |
| TRUK 2025 komplett | **0 Zeilen** fuer 2025; UK hat nur 2026-01 bis 2026-07 (1'088 Zeilen) | UK-Manual-Import enthaelt kein 2025 | fehlender Import, kein Bug |

## 1. CH/AT: der Import liest vom Testsystem (Hauptbefund)

Direkt in der **Produktiv**-Datenbank nachgewiesen (`Sites`-Tabelle):

```text
ZSCHWEIZ | SAP | SapServiceUrl = http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/
```

`travt762` = **T76, das TEST-System**. Produktiv waere `travp762` (P76). Damit liest die
Finance-Strecke fuer CH/AT den Testmandanten.

Das erklaert den Zeitschnitt exakt: Ein Testsystem enthaelt nur Daten bis zu seinem letzten
Refresh aus der Produktion. Die Zeilenzahlen brechen **Mitte April 2026** ein:

| Monat 2026 | TRCH Zeilen | TRAT Zeilen |
| --- | --- | --- |
| 01 | 2'662 | 115 |
| 02 | 2'616 | 78 |
| 03 | 2'643 | 88 |
| 04 | **1'409** (halber Monat) | 65 |
| 05 | **47** | 0 |
| 06 | **43** | 8 |
| 07 | **87** | 1 |

### WARNUNG: Die naheliegende „Loesung" (URL auf travp762) waere SCHAEDLICH

**Am 2026-07-28 live gemessen, mit den App-eigenen Credentials (User `POWERBI`, aus
`Sites.PasswordOverride`), read-only gegen beide Server:**

| Abfrage | TEST `travt762` (aktuell konfiguriert) | PROD `travp762` (vermeintliches Ziel) |
| --- | --- | --- |
| `FinanzdataSchweizOeSet/$count` | **40'506** | 30'642 |
| `$filter=Gjahr eq '2025'` | 30'642 | 30'642 |
| `$filter=Gjahr eq '2026'` | **9'864** | **0** |

**Das Testsystem hat MEHR Daten als die Produktion.** Die 2026er Finance-Zeilen existieren
in diesem OData-Service **ausschliesslich auf T76**; P76 liefert dafuer null. Eine Umstellung
von `travt762` auf `travp762` wuerde also die vorhandenen `9'864` 2026er CH/AT-Zeilen
**loeschen**, nicht die fehlenden Monate ergaenzen — das genaue Gegenteil eines Fixes.

**Damit ist eine frueher hier notierte Empfehlung zurueckgezogen.** Ein erster Entwurf dieses
Dokuments behauptete, ein URL-Wechsel behebe gleichzeitig Andreas' rote Markierungen und die
leere Tabelle `GroupStandardCosts`. Das ist widerlegt. Ebenso war die Behauptung falsch, der
Eintrag vom 2026-07-14 („`FinanzdataSchweizOeSet` liefert bei `Gjahr eq '2026'` nichts") sei
ueberholt — er ist fuer **P76 weiterhin exakt zutreffend**. Ueberholt ist daran nur der
Zusatz „Dashboard zeigt 0": das Dashboard zeigt inzwischen Januar bis Mitte April 2026, weil
es vom Testsystem liest.

### Was das eigentliche Problem ist

Der ABAP-Report belegte 2026 `9'573` Fakturapositionen in SAP (BUKRS 1100) — die Daten
**existieren** also produktiv in VBRK/VBRP. Nur der **OData-Service auf P76 gibt sie nicht
heraus**, waehrend derselbe Service auf T76 `9'864` Zeilen liefert. Das ist ein
**SAP-seitiges Problem des produktiven Service/Extraktors**, keine Dashboard-Konfiguration.

Naheliegendste Hypothese (zu verifizieren): Der ABAP-Stand bzw. die Service-Implementierung
auf P76 ist **aelter als auf T76** — der Transport, der die 2026-Faehigkeit brachte, wurde nie
nach Produktion bewegt. Das wuerde erklaeren, warum ein Testsystem mehr liefert als die
Produktion, was sonst kaum vorkommt.

**Zu tun (SAP-Seite, nicht App-Seite):** Mit dem SAP-Entwickler klaeren, warum
`FinanzdataSchweizOeSet` auf P76 keine 2026er Zeilen liefert — Versionsvergleich des
Extraktors/der Provider-Klasse zwischen T76 und P76, offene Transporte pruefen.

**Nicht tun:** `Sites.SapServiceUrl` auf `travp762` umstellen, solange P76 fuer 2026 null
liefert. Und die aktuelle Abhaengigkeit von einem Testsystem ist trotzdem ein Risiko, das
adressiert werden muss — nur eben SAP-seitig.

### Konsequenz fuer `GroupStandardCosts`

Die leere Tabelle (`0` Zeilen, siehe
`docs/FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28.md` Abschnitt 7a) laesst sich damit **nicht**
per URL-Wechsel beheben. Ihr Root Cause muss separat untersucht werden: der
`mbewSet`/Standardpreis-Read schlug am 2026-07-15 zweimal fehl (500-Fehler bzw. Haenger).
Das ist unabhaengig von der Server-Frage zu analysieren.

### Nebenbefund: zukunftsdatierte Zeilen

TRCH hat je eine Zeile mit Belegmonat **2026-09** und **2026-10** — Datum in der Zukunft.
Typisches Testsystem-Artefakt. Betragsmaessig unbedeutend, sollte nach der Umstellung auf
Produktivdaten verschwinden; falls nicht, eigener Datenqualitaetspunkt.

## 2. Spanien: Range-Export beginnt erst Ende Mai 2026

| Zeitraum | Zeilen |
| --- | --- |
| 2025-01 bis 2025-12 | vollstaendig (244–545 je Monat, 4'315 gesamt) |
| 2026-01 bis 2026-04 | **0** |
| 2026-05 | 35 (nur ab 28.05.) |
| 2026-06 | 542 |
| 2026-07 | 357 |
| ohne Datum | 229 |

Spanien wird ueber manuelle Range-Exporte geliefert; im Repo liegt
`SageSpainExportPackage/Spain_Sales_range_20260528_to_20260603.csv`, also ein Bereich ab
**28.05.2026**. Januar bis April 2026 wurde nie exportiert — deshalb fehlen sie, und Mai ist
nur ein Teilmonat. Das ist kein Programmfehler, sondern ein fehlender Export.

**Zu tun:** Range-Export fuer `2026-01-01` bis `2026-05-27` nachziehen und importieren.
Anleitung: `docs/SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03.md`.

Zusaetzlich: **229 spanische Zeilen haben kein Datum** und fallen damit aus jeder Jahres-/
Monatsauswertung heraus (sie erscheinen in Andreas' Pivot nirgends). Eigener Punkt, sollte
geprueft werden.

## 3. UK: kein 2025 vorhanden

| Zeitraum | Zeilen |
| --- | --- |
| 2025 | **0** |
| 2026-01 bis 2026-07 | 1'082 (123–217 je Monat) |
| ohne Datum | 6 |

Der UK-Manual-Import (Sage-Export) enthaelt ausschliesslich 2026er Belege. Die komplette
2025er-Zeile in Andreas' Pivot ist daher leer, inklusive Jahresergebnis. Kein Programmfehler,
sondern fehlende Lieferung.

Hinweis: Frueher dokumentierte UK-2025-Analysen (z. B. „Sage-Restdifferenz `-5'261.91 GBP`"
in `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md`) beruhen auf Daten, die aktuell **nicht**
in der Datenbank sind — beim Nachvollziehen nicht verwirren lassen.

**Zu tun:** UK-2025-Export anfordern/importieren, falls 2025 im Reporting gebraucht wird.

## 4. Prioritaet und Zuordnung

| Punkt | Wer | Aufwand | Bemerkung |
| --- | --- | --- | --- |
| CH/AT: `SapServiceUrl` auf `travp762` | Ingo, nach Abstimmung mit Andreas/Marco | klein (Konfiguration + Reimport) | behebt zusaetzlich `GroupStandardCosts` — hoechster Hebel |
| ES: Range-Export 2026-01 bis 2026-05-27 | Spanien/Ingo | mittel | reiner Nachschub |
| UK: 2025-Export | UK/Ingo | mittel | nur falls 2025 gebraucht |
| ES: 229 Zeilen ohne Datum | Ingo | klein (Analyse) | fallen aus allen Auswertungen |
| CH: 2 zukunftsdatierte Zeilen | beobachten | — | vermutlich Testsystem-Artefakt |

Keiner der vier roten Bereiche ist ein Rechen- oder Logikfehler im Dashboard. Es sind
Datenherkunfts- und Datenvollstaendigkeitsprobleme: einmal falsches Quellsystem (CH/AT),
zweimal fehlende Lieferung (ES, UK).
