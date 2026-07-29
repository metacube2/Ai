# Pruefung der Importe CH/AT und Indien — 2026-07-29

Grundlage: read-only Kopie von `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db`
(Stand 2026-07-29 08:27, 337.8 MB). `PRAGMA quick_check` = `ok`, die Kopie ist also
auswertbar — anders als der defekte Snapshot vom 2026-07-28, bei dem waehrend laufender
Schreibvorgaenge kopiert wurde.

## 1. Beide Fixes vom 2026-07-28 wirken

**mbewSet-Endlosschleife behoben.** Der Standardpreis-Read laeuft durch und meldet sich
sauber ab:

```
2026-07-29 08:20:46  Standardpreis-Read gestartet   Filter=Bwkey eq '1100' or Bwkey eq '1200'
2026-07-29 08:21:07  Standardpreis-Read beendet     Zeilen=68543 | Materialien=66084 | laut $count=68543
```

21 Sekunden statt endlos. Die neue `$count`-Gegenprobe stimmt exakt (`68543 = 68543`), die
Warnung „Standardpreis-Read unvollstaendig" ist nicht aufgetreten.

**`GroupStandardCosts` ist gefuellt:** 63'494 Zeilen, Bewertungskreis 1100, alle mit
`RefreshedAtUtc = 2026-07-29 06:21:07`. Vorher war die Tabelle seit dem 2026-07-15 leer.

Dass nur Bewertungskreis 1100 in der Tabelle steht, ist **kein Fehler**:
`PersistGroupStandardCostsAsync` filtert bewusst auf `GroupStandardCostAreas.ByEntity[TrAg]`
= 1100, weil nur TR AG als liefernde Gesellschaft eine verifizierte Kostenquelle hat.
Nicht belegt und deshalb hier auch nicht behauptet: ob der OData-`$filter` ueberhaupt
ausgewertet wird — `Bwkey eq '1100' or Bwkey eq '1200'` liefert exakt dieselben 68'543
Zeilen wie `Bwkey eq '1100'` allein, und dass dieser Service `$top`/`$skip`/`$orderby`
ignoriert, ist bereits gemessen. Operativ ohne Belang, weil AT seine Kosten ueber WAVWR
bekommt (99.9 % Deckung).

**Produktivsystem wird genutzt:** die Log-URL zeigt `travp762`, nicht mehr `travt762`.

## 2. Datenstand: alle Laender haben Daten ab 2025

| TSC  | Zeilen | Rechnungsdatum von | bis        | Kostenbasis |
|------|--------|--------------------|------------|-------------|
| TRCH | 47'142 | 2025-01-06         | 2026-07-29 | 96.6 %      |
| TRAT |  1'790 | 2025-01-07         | 2026-07-28 | 99.9 %      |
| TRIT | 19'534 | 2025-01-07         | 2026-07-28 | 95.7 %      |
| TRDE |  7'171 | 2025-01-02         | 2026-07-27 | 68.6 %      |
| TRIN |  6'990 | 2025-01-06         | 2026-07-28 | 99.4 %      |
| TRES |  5'504 | 2025-01-02         | 2026-07-27 | 81.1 %      |
| TRUK |  2'955 | 2025-01-03         | 2026-07-24 | **0 %**     |
| TRFR |  2'577 | 2025-01-07         | 2026-07-28 | 51.5 %      |
| TRUS |  1'504 | 2025-01-03         | 2026-07-28 | 90.5 %      |

Monatsweise geprueft fuer TRCH/TRAT/TRIN: **keine Luecke** von 2025-01 bis 2026-07. Das
Ziel „alle Laender ab 2025" ist damit erreicht.

Indien lief fehlerfrei (2026-07-28 18:10, HANA `TRAFAG_LIVE`, Invoice-Query 6'970 +
Credit-Query 20 = 6'990 Zeilen, Dauer 7.6 s, keine Warnungen oder Fehler im Log).

## 3. Die 1.1-%-Trefferquote beim Standardpreis-Fallback ist unauffaellig

Der Logeintrag „Zeilen mit Kosten=18 (1.1 %) | ohne Kosten=1582" sieht nach einem
Zuordnungsproblem aus, ist aber keines. Der Nenner sind **nicht** alle CH/AT-Zeilen: der
MBEW-Fallback greift laut `EnrichStandardCostsAsync` nur fuer Zeilen, die aus VBRP-WAVWR
keine Kostenbasis bekommen haben (`records.Where(r => r.StandardCost == 0m)`) — das waren
1'600 von 48'932.

Die 1'582 danach immer noch unbepreisten Zeilen sind **Sammel- und Dienstleistungsnummern
ohne Materialstamm**, kein Schluesselproblem:

| Material | Zeilen | Beispiel                          |
|----------|--------|-----------------------------------|
| RS99999  | 923    | 01.04.26/96.5kg/Euro 151.61       |
| V99999   | 554    | ASIC E11221                       |
| RS99998  | 29     | 400 Pieces of 8253 Pressure Transmitter |
| MGK*     | 44     | Kalibration / Justierung          |
| SCS-Z, TRCH-Z | 10 | Zertifikate                      |

Fuer solche Positionen existiert in MBEW zu Recht kein Standardpreis. Der
`MaterialKeyNormalizer` arbeitet korrekt — waeren die Schluessel falsch formatiert,
stuenden hier normale Artikelnummern.

## 4. Was der Fix (noch) NICHT bringt: die Konzernkostenbasis wird kaum genutzt

`GroupStandardCosts` kann nur greifen, wenn eine Zeile TR AG als liefernde Gesellschaft
ausweist. `GroupMarginSupplierClassifier.ResolveDeliveringEntity` liest dafuer den
**Klartext-`SupplierName`**. Gemessen ueber alle 95'168 Zeilen:

| TSC  | Zeilen mit `SupplierName` enthaelt „Trafag AG" |
|------|-----------------------------------------------|
| TRIT | 6'448                                          |
| TRIN |   677                                          |
| TRFR |    40                                          |
| TRUS |     2                                          |
| TRCH, TRAT, TRDE, TRES, TRUK | **0**           |

**7'167 von 95'168 Zeilen (7.5 %)** koennen die neu gefuellte Kostentabelle ueberhaupt
verwenden. Die Tabelle ist repariert, der Nutzen ist durch die Lieferantenluecke blockiert.
Bei TRCH/TRAT ist das erwartbar (TR AG ist dort der Absender, nicht der Lieferant); bei
TRDE, TRES und TRUK ist es die eigentliche offene Baustelle.

## 5. Behoben: UK-Mapping war unvollstaendig

`Sales_TRUK_2026-05-11.xlsx` **enthaelt** die fehlenden Spalten, sie waren nur nie gemappt
(Fuellgrad ohne die Legendenzeile gemessen, 1'881 Datenzeilen):

| Spalte                 | gefuellt | Beispiel     |
|------------------------|----------|--------------|
| Supplier number        | 100 %    | `TR08`       |
| Supplier name          | 100 %    | `Trafag AG`  |
| Supplier country       | 100 %    | `CH`         |
| Standard cost          | 94.3 %   | `42`         |
| Standard Cost Currency | 100 %    | `CHF`        |

Der UK-Standort hatte 18 aktive Mappings (DE: 29, ES: 25) — Lieferant und Kosten fehlten
komplett. Deshalb 0 % Kostendeckung ueber alle 2'955 UK-Zeilen und deshalb 0 UK-Zeilen mit
erkennbarem internem Lieferanten.

Ergaenzt in `DatabaseSeedService.EnsureUkManualExcelMapping`: `SupplierNumber`,
`SupplierName`, `SupplierCountry`, `StandardCost`, `StandardCostCurrency`. Der Seed ist
idempotent und legt fehlende Mappings beim App-Start an — nach dem Deploy sind sie da,
ohne Schreibzugriff auf die Produktivdatenbank.

Die Waehrungsspalte ist zwingend: UK fuehrt Kosten in **CHF**, Umsatz in **GBP**. Die
Konsolidierung rechnet die Kostenbasis ueber ihre eigene Waehrung um
(`ManagementCockpitService`, `RateToChf(row.StandardCostCurrency, rowRateDate)`) — ohne die
Spalte waere der CHF-Betrag stillschweigend als GBP gelesen worden.

Erwartung nach Deploy + UK-Reimport: ~94 % Kostendeckung statt 0 %, und 2'955 Zeilen, die
`SupplierName = "Trafag AG"` als internen Lieferanten erkennen — der erste echte Verbraucher
der Konzernkostentabelle ausserhalb Italiens.

`Customer Industry` ist in der Datei ebenfalls zu 100 % gefuellt und ebenfalls nicht
gemappt. Bewusst nicht mitgeaendert, weil es kein Margenfeld ist — bei Bedarf eine Zeile.

## 6. Legendenzeile: verschwindet von selbst

Die Zeile `Tsc = "Subsidiary abbreviation / company identifier"` (Id 2'841'937) steht noch
in `CentralSalesRecords`. Sie gehoert zu `SiteId = 5` (TRUK), und
`CentralSalesRecordService.ReplaceForSiteAsync` loescht **nach `site.Id`**, nicht nach dem
`Tsc`-Text. Der naechste UK-Import raeumt sie also mit weg; ein Einzel-Delete ist nicht noetig.

## 7. Datumsluecken

| TSC  | ohne Rechnungsdatum | ohne Buchungsdatum |
|------|---------------------|--------------------|
| TRES | 231                 | **5'504 (alle)**   |
| TRUK | 6                   | 6                  |

Spanien hat **kein einziges** Buchungsdatum — passend dazu existiert im Spanien-Mapping
kein `PostingDate`-Eintrag. Das ist Andreas' Issue 6 und haengt am Sage-Export
(`FacturasTB.FechaAsiento`, siehe `docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md` §1). Die
231 Zeilen ohne Rechnungsdatum sind in jeder monatsbasierten Auswertung unsichtbar.

UK mappt `PostingDate` bewusst auf die Spalte `invoice date`, weil die Spalte
`posting date` in der Quelldatei zu 0 % gefuellt ist.

## 8. Offen

1. **Lieferantenluecke** — TRDE (7'171), TRES (5'504), TRUK (2'955) ohne Lieferantenfelder.
   UK ist mit dem Mapping oben erledigt, sobald deployed. TRDE hat die Mappings
   (`Lieferanten Nummer`, `Name Lieferant`, `Land Lieferant`), die Alphaplan-Quelle liefert
   sie aber leer. TRES hat gar keine. Entscheidung Andreas ausstehend.
2. **Spanien Buchungsdatum** — Sage-Export um `FacturasTB.FechaAsiento` erweitern.
3. **Spanien Nachtrag** 2026-01 bis 2026-05 von Santi ausstehend.
4. **B1-Upgrade-Nachsorge Montag 2026-08-03** — FR/IT/US/IN pruefen, besonders
   `INV1.StockPrice`.
5. **TR IT / TR IN als liefernde Gesellschaft** — Konstanten vorhanden, keine Kostenquelle.
6. **`Models/GroupStandardCost.cs` Zeilen 12–16** tragen noch die widerlegte Aussage zu
   den TR-IT-Standardkosten.
7. Antworten offen: Paola (Ende August?), Italien (Zeitplan 2027).
