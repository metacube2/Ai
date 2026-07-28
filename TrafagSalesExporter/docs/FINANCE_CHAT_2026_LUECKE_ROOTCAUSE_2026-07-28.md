# CH/AT 2026 fehlt: Root Cause gefunden

Stand: 2026-07-28

Anlass: Andreas hat CH/AT ab Mai 2026 als falsch markiert (`docs/Bild.png`, siehe
`docs/FINANCE_DATENLUECKEN_ANDREAS_2026-07-28.md`). Dieses Dokument haelt die
Ursachenkette und den konkreten Fix fest.

## Ergebnis in einem Satz

Der SAP-Report **`Z_TRAFAG_DACH_EXPORT`** wurde in der **Produktion (P76) nie fuer 2026
ausgefuehrt**. Deshalb ist die Z-Tabelle `ZSCHWEIZ` dort ohne 2026er Zeilen, der
OData-Service liefert null, und das Dashboard bezieht seine 2026er CH/AT-Zahlen
ersatzweise aus dem **Testsystem T76**.

Es ist **kein** Programmfehler, **kein** fehlender Transport und **keine** falsche
Dashboard-Konfiguration.

## Beweiskette (alles am 2026-07-28 live gemessen, read-only)

### 1. Das Dashboard liest vom Testsystem

`Sites.SapServiceUrl` fuer `ZSCHWEIZ` in der **produktiven** App-DB:

```text
http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/
```

`travt762` = T76 = TEST.

### 2. Das Testsystem hat MEHR Daten als die Produktion

OData-`$count` mit den App-eigenen Credentials (User `POWERBI`) gegen beide Server:

| Abfrage | T76 (TEST) | P76 (PROD) |
| --- | --- | --- |
| `FinanzdataSchweizOeSet/$count` | **40'506** | 30'642 |
| `Gjahr eq '2025'` | 30'642 | 30'642 |
| `Gjahr eq '2026'` | **9'864** | **0** |

2025 ist auf beiden identisch. 2026 existiert nur im Testsystem.

### 3. Die Ursache liegt in der Z-Tabelle, nicht im Service

Der Report schreibt per UPSERT in `ZSCHWEIZ` (Zeile 494: `MODIFY zschweiz FROM TABLE
lt_chunk.`), der OData-Service liest daraus. Direkter RFC-Read der Tabelle:

| System | `ZSCHWEIZ` mit `GJAHR = '2025'` | `ZSCHWEIZ` mit `GJAHR = '2026'` |
| --- | --- | --- |
| P76 (PROD) | vorhanden (Stichprobe FKDAT 20250106) | **keine Zeilen** |
| T76 (TEST) | vorhanden | vorhanden (Stichprobe FKDAT 20260105) |

### 4. Es ist KEIN fehlender Transport

Der Programmquelltext ist auf beiden Systemen **byte-identisch**:

| System | Programm | Zeilen | Letzte Aenderung |
| --- | --- | --- | --- |
| P76 | `Z_TRAFAG_DACH_EXPORT` | 577 | KOI, 2026-07-22 |
| T76 | `Z_TRAFAG_DACH_EXPORT` | 577 | KOI, 2026-07-16 |

`diff` ueber beide Quelltexte: keine Unterschiede. Die Produktion hat also den vollen
Funktionsumfang inklusive `WAVWR_DC`/`STPRS_HC` (Stand 2026-07-16). Nur ausgefuehrt wurde
er dort nicht fuer 2026.

Hinweis zum Namen: Das Programm heisst im System `Z_TRAFAG_DACH_EXPORT`. Die lokale Datei
`docs/abap/Z_TRAFAG_SCHWEIZ_EXPORT.abap` und der `REPORT`-Kopf im Quelltext tragen noch den
alten Namen `Z_TRAFAG_SCHWEIZ_EXPORT` — dieser Name existiert in **keinem** der beiden
Systeme. Beim Suchen nicht darauf verlassen.

## Der Fix

**Report `Z_TRAFAG_DACH_EXPORT` auf P76 (Client 100) fuer 2026 ausfuehren.**

Selektion: Buchungskreise `1100` (CH) und `1200` (AT), Geschaeftsjahr `2026`.

### Warum das sicher ist

- Der Report macht **UPSERT** (`MODIFY zschweiz`), kein `DELETE`. Ein Lauf fuer 2026
  ergaenzt Zeilen und fasst 2025 nicht an — im gesamten Quelltext gibt es keine
  DELETE-Anweisung auf `ZSCHWEIZ`.
- Er ist **wiederholbar**: derselbe Lauf zweimal erzeugt dasselbe Ergebnis.
- `COMMIT WORK AND WAIT` in Chunks (Zeile 500), also kein Riesen-Commit.

### Voraussetzung pruefen (laut Kopfkommentar des Reports)

Vor dem ersten Lauf der 2026-07-16er Version muessen in `ZSCHWEIZ` (SE11) existieren:

- `WAVWR_DC` — Typ CURR, gleiche Laenge/Dezimalen wie `NETWR_DC`
- `STPRS_HC` — Typ CURR, Vorschlag 15,4

Da der Quelltext auf P76 identisch und vom 2026-07-22 ist, sind die Felder dort
**wahrscheinlich** schon angelegt (sonst waere das Programm nicht aktivierbar gewesen) —
vor dem Lauf trotzdem in SE11 verifizieren.

### Wichtig fuer die Gruppenmarge: auch 2025 erneut laufen lassen

Der Kopfkommentar sagt ausdruecklich: Nach dem Deploy der WAVWR-Version muss der Report
**einmal fuer den vollen historischen Bestand** laufen, sonst bleibt `WAVWR_DC` fuer
bereits bestehende Zeilen leer — der UPSERT ergaenzt die neuen Felder nur bei einem
erneuten Lauf ueber dieselben Zeilen. Da P76 fuer 2025 Zeilen aus einem aelteren Lauf hat,
ist deren `WAVWR_DC` dort vermutlich `0`.

Empfehlung: auf P76 **fuer 2025 UND 2026** laufen lassen.

## Was NICHT der Fix ist

**`Sites.SapServiceUrl` auf `travp762` umstellen — solange der Report auf P76 nicht
gelaufen ist, wuerde das die vorhandenen 9'864 2026er Zeilen entfernen** und CH/AT auf
2025 zurueckwerfen. Eine frueher in diesem Projekt notierte Empfehlung in diese Richtung
ist damit widerlegt und zurueckgezogen (siehe
`docs/FINANCE_DATENLUECKEN_ANDREAS_2026-07-28.md`).

**Richtige Reihenfolge:**

1. `ZSCHWEIZ`-Felder auf P76 in SE11 verifizieren (`WAVWR_DC`, `STPRS_HC`).
2. `Z_TRAFAG_DACH_EXPORT` auf P76 fuer 2025 und 2026 laufen lassen (BUKRS 1100 + 1200).
3. Gegenpruefen: OData `FinanzdataSchweizOeSet/$count?$filter=Gjahr eq '2026'` auf
   `travp762` muss dann > 0 liefern (Werkzeug: `.tmp_tools/OdataProbe`).
4. **Erst dann** `Sites.SapServiceUrl` auf `travp762` umstellen — mit DB-Sicherung und
   nach Abstimmung mit Andreas/Marco, weil sich produktive Finance-Zahlen dabei aendern.
5. ZSCHWEIZ-Import in der App neu anstossen, `GroupStandardCosts` und Fuellgrade pruefen.

## Stand NACH dem Report-Lauf auf P76 (2026-07-28, spaeter Nachmittag)

Ingo hat den Report auf P76 fuer Jahre **ab 2026** ausgefuehrt. Ergebnis, live gemessen:

| Abfrage | T76 (TEST) | P76 (PROD) |
| --- | --- | --- |
| `FinanzdataSchweizOeSet/$count` | 40'506 | **48'932** |
| `Gjahr eq '2025'` | 30'642 | 30'642 |
| `Gjahr eq '2026'` | 9'864 | **18'290** |

**Die Produktion ist jetzt die bessere Quelle** — 18'290 statt 9'864 Zeilen fuer 2026, weil
T76 nur bis Mitte April reicht, P76 dagegen bis zum aktuellen Tag (Stichprobe mit
`FKDAT = 20260728`). Stichproben bestaetigen: `NETWR_DC` und `WAVWR_DC` sind in den 2026er
P76-Zeilen korrekt gefuellt (z. B. `NETWR_DC 40'003.38 / WAVWR_DC 25'467.75`).

### RESTBEFUND: 2025 hat auf P76 keine Kostenbasis

Genau der im Report-Kopfkommentar beschriebene Fall ist eingetreten. Fuer 2025 gilt auf P76:

| Feld | P76 (2025) | T76 (2025, dieselben Belege) |
| --- | --- | --- |
| `NETWR_DC` | gefuellt (z. B. 19'000.00 / 2'031.25 / 6'822.50) | identisch |
| `WAVWR_DC` | **0.00 — keine einzige Zeile mit Wert** | gefuellt (9'870.85 / 1'081.48 / 4'540.30) |

Ursache: Die 2025er Zeilen auf P76 stammen aus einem **aelteren Report-Lauf**, bevor
`WAVWR_DC` existierte. Der Report macht UPSERT — er ergaenzt neue Felder nur, wenn er
erneut ueber dieselben Zeilen laeuft. Der Lauf „ab 2026" hat 2025 nicht angefasst.

Wuerde man jetzt auf `travp762` umstellen, waere die Folge: 2026 vollstaendig und besser als
heute, aber **2025 ohne CH/AT-Kostenbasis** — also Gruppenmarge fuer das Referenzjahr
kaputt. Deshalb:

**NOCH ZU TUN: Report auf P76 fuer `s_gjahr = 2025` laufen lassen** (BUKRS 1100 + 1200).
Danach ist P76 fuer beide Jahre vollstaendig und die URL-Umstellung ist unbedenklich.

### Wie weit zurueck? Antwort: 2025 reicht, NICHT bis 2022

Geprueft in der produktiven App-DB:

- `ExportSettings.DateFilter = 2025-01-01` — die App importiert **nichts vor 2025**.
- `CentralSalesRecords` enthaelt ausschliesslich die Jahre 2025 (58'353 Zeilen) und
  2026 (26'200) plus 235 Zeilen ohne Datum.

Ein Report-Lauf fuer 2022–2024 wuerde also ~100'000 Zeilen in `ZSCHWEIZ` schreiben, die die
App **nie liest**, und dafuer erhebliche Laufzeit auf dem Produktivsystem kosten (VBRK/VBRP
+ MBEW-Joins ueber drei zusaetzliche Jahre). Kein Nutzen, nur Last.

**Empfehlung: `s_gjahr = 2025` (2026 ist bereits erledigt).** Wer auf Nummer sicher gehen
will, kann `2025` bis `2026` angeben — der UPSERT ist idempotent, ein erneuter 2026-Lauf
schadet nicht.

Falls die Finance-Historie spaeter doch weiter zurueck reichen soll, ist das ein eigener
Entscheid: dann muessten auch `ExportSettings.DateFilter`, Sollwerte und Jahreskurse fuer
diese Jahre gepflegt werden — nicht nur die SAP-Tabelle.

### Nebenbefund bestaetigt: NETWR_HC-Faktor-100-Bug lebt weiter

Die Stichprobe zeigt auf **beiden** Systemen `NETWR_DC 19'000.00` gegen `NETWR_HC 161.50` —
Faktor ~100 zu klein, genau der am 2026-07-16 dokumentierte SAP-seitige Skalierungsfehler.
Die App kompensiert das selbstdeaktivierend (`SapCompositionService.CorrectHouseCurrencyScaling`),
der SAP-seitige Fix bleibt offen. Kein neues Problem, aber weiterhin unbehoben.

## Offen bleibt: `GroupStandardCosts` (0 Zeilen)

Die leere Konzernkostentabelle ist ein **separates** Problem und wird durch die obigen
Schritte nicht automatisch geloest. Ihr dokumentierter Root Cause (2026-07-15/16) war ein
fehlgeschlagener bzw. haengender `mbewSet`-Read. Nach Schritt 5 erneut pruefen; falls
weiterhin leer, eigene Analyse.

## Verwendete Werkzeuge

- `.tmp_sap_probe/SapProbe.exe` — RFC: `table-read ZSCHWEIZ`, `table-read TRDIR`,
  `abap-read Z_TRAFAG_DACH_EXPORT` gegen beide Systeme
- `.tmp_tools/OdataProbe` — vergleicht `FinanzdataSchweizOeSet/$count` auf T76 und P76 mit
  den App-eigenen Credentials aus der DB, ohne das Passwort auszugeben
