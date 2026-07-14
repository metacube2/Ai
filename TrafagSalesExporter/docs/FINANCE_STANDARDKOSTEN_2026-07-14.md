# Finance: Kostenbasis der Gruppenmarge (Standardkosten)

Stand: 2026-07-14 — umgesetzt, getestet (`203/203`), deployed (Commit `8e0f51e`)

Betrifft das **alte** Standardkosten-Thema aus `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`,
nicht den Journal-Import.

## Ausgangslage (an Produktivdaten gemessen, 14.07.2026)

| TSC | Zeilen | Kosten <> 0 | Quelle |
| --- | --- | --- | --- |
| ZSCHWEIZ (CH/AT) | 40'292 | **0 %** | SAP OData |
| TRDE | 6'879 | **0 %** | Alphaplan |
| TRUK | 1'033 | **0 %** | Sage UK |
| TRFR | 2'509 | 51 % | SAP B1 |
| TRSE | 5'229 | 81 % | Sage ES |
| TRUS | 1'427 | 92 % | SAP B1 |
| TRIT | 19'006 | 96 % | SAP B1 |
| TRIN | 6'779 | 99 % | SAP B1 |

Ursachen der beiden grossen Luecken:

- **CH/AT:** `StandardCost` war im Seed hart auf `=0` gemappt, weil der Umsatz-Service
  (`FinanzdataSchweizOeSet`) kein Kostenfeld liefert.
- **DE:** Das Mapping wartete auf eine Spalte `EinstandsPreis` — die es im Alphaplan-Export
  gar nicht gibt. Der Rohertrag war immer da und wurde nur weggeworfen.

## Was SAP dazu sagt (ABAP-Analysereport)

Report `docs/abap/ZFIN_ANALYSE_STPRS_JOURNAL.abap`, Ausgabe in `stdpreis.txt`:

- **`mbewSet` ist im Service `ZPOWERBI_EINKAUF_SRV` bereits vorhanden.** Es musste also
  nichts auf SAP-Seite gebaut werden — die urspruengliche Annahme (neues EntitySet noetig)
  war falsch.
- Bewertungskreis **1100** = Trafag AG (CH, CHF): 65'447 Materialien, **96.3 %** mit
  `STPRS > 0`, 100 % `VPRSV = S`.
- Bewertungskreis **1200** = Trafag Ges.m.b.H. (AT, EUR): 2'564 Materialien, **99.6 %**.
- Von den **tatsaechlich fakturierten** Zeilen (41'114 ab 2025) haben **96.5 %** einen
  Standardpreis. `VBRP-WAVWR` (Kostenwert direkt auf der Faktura) waere mit 92.3 % die
  Alternative, ist im Z-Service aber **nicht exponiert**.
- `PEINH` (Preiseinheit) ist derzeit durchgaengig 1.

## Umsetzung

| Baustein | Ort |
| --- | --- |
| MBEW-Leser | `Services/SapGatewayStandardCostReader.cs` — `mbewSet` gepaged (`$top`/`$skip`, Filter auf `Bwkey`), Metadata-Vorpruefung, pure `MapRow` |
| Zuordnung | `Services/StandardCostEnricher.cs` — `Land` -> Bewertungskreis (CH=1100, AT=1200, per `T001K` bestaetigt), setzt `StandardCost` je Zeile |
| Einhaengepunkt | `Services/DataSources/SapGatewayDataSourceAdapter.cs` — Anreicherung nach dem Umsatzimport |
| Deutschland | `Services/ManualExcelImportService.DeriveAlphaplanUnitCost` — Einstandswert = `NettoPreisGesamt - RohertragGesamt` |
| Tests | `TrafagSalesExporter.Tests/StandardCostTests.cs` (14 Tests) |

### Die zentrale Falle: Stueckpreis vs. Zeilensumme

`ManagementCockpitService.ResolveGroupMarginCostBasis` rechnet **`Menge x StandardCost`**.
`StandardCost` muss daher ein **Stueckpreis** sein. Aber:

- `MBEW-STPRS` gilt pro **`PEINH`** Stueck -> wird durch die Preiseinheit geteilt.
- `VBRP-WAVWR` ist eine **Zeilensumme**.
- Der Alphaplan-Rohertrag ist eine **Zeilensumme** -> wird durch die Menge geteilt.

Ohne diese Normalisierung waere die Kostenbasis um genau diesen Faktor zu hoch — und zwar
still, ausgerechnet in den Laendern, die repariert werden sollten. `PEINH = 1` heute
schuetzt nicht: ein einziges Material mit `PEINH = 100` wuerde reichen.

### Warum Material UND Bewertungskreis im Schluessel stehen

MBEW ist je Material **und** Bewertungskreis verschluesselt. Ein Join nur ueber das Material
wuerde CH-Zeilen den oesterreichischen Preis geben (und umgekehrt). Der Umsatz-Service
liefert keinen Bewertungskreis, deshalb wird er aus dem Land der Zeile abgeleitet.

### Guardrail

Schlaegt das Lesen der Standardpreise fehl, laeuft der **Umsatzimport trotzdem durch**
(Warning im Eventlog, `StandardCost` bleibt 0). Ein Kostenproblem darf nie den taeglichen
Umsatzexport eines ganzen Landes verhindern.

## Offen

1. **Interner Lieferant ist weiterhin eine Attrappe:** Die Gruppenmarge beschriftet solche
   Zeilen mit "Interner Standardpreis", rechnet aber identisch zu externen. Ein echter
   Konzern-Standardpreis (MBEW-STPRS der liefernden Gesellschaft) ist nicht angebunden —
   bewusst, weil die Fachentscheidung dazu aussteht.
2. **UK** liefert keine Kostenspalte im Sage-Export; **FR** hat bei 49 % der B1-Zeilen
   keinen `StockPrice` (Stammdatenfrage an FR).
3. **Waehrungsmisch-Bug** (`Marge Original`) bleibt latent: Kosten- und Verkaufswaehrung
   sind aktuell ueberall identisch. Sobald Konzernkosten in CHF gegen lokale Umsaetze
   laufen, wird er scharf.
4. **Drei Fachfragen an Andreas:** Welche Kostenart (lokaler Einstandswert vs.
   Konzern-Herstellkosten)? Bei internem Trafag-Lieferanten: Preis der liefernden oder der
   verkaufenden Gesellschaft? Lokal oder konzernweit rechnen?

## Nachsorge nach dem naechsten Export

- Kostenquote fuer `ZSCHWEIZ` und `TRDE` pruefen; erwartet werden fuer CH/AT rund **96 %**
  (SAP-Messwert). Deutlich weniger deutet auf ein Material-Matching-Problem hin.
- Gruppenmarge fuer CH/AT und DE fachlich mit Andreas plausibilisieren, bevor sie als
  belastbar kommuniziert wird.
