# RAG Finance-Formeln (Zeilenverarbeitung/Mechanik)

Stand: 2026-08-07

## Vorrang: was die Kacheln NICHT sagen (2026-08-07)

Vor jeder Aussage ueber eine Finance-Kennzahl diese vier Punkte kennen — alle
produktiv gemessen, Details in `docs/FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md`:

1. **Sollwerte gibt es nur fuer 2025.** `FinanceReferences` enthaelt 17 Zeilen,
   alle `Year = 2025`, davon 3 ohne Wert (`CH`, `CN`, `RU`). Das Standardjahr der
   Seite ist das juengste Jahr der Daten (`2026`, 35'841 Zeilen) — dort ist der
   Soll/Ist-Abgleich vollstaendig leer. Die Kachel `Nicht geprueft` zaehlt diese
   Laender seit 2026-08-07; `Laender OK` und `Zu pruefen` tun es NICHT.
2. **`CH` hat auch fuer 2025 keinen Sollwert** und wird damit gegen nichts
   geprueft — mit `17'608` Zeilen der groesste Standort.
3. **`Net Sales Actual` und die Gruppenmarge-Kacheln addieren Waehrungen
   numerisch**, wenn der Filter mehrere enthaelt; die Anzeige endet dann auf
   `Mixed`. Erst ein Land-/Waehrungsfilter oder `Group-Waehrung (CHF)` gibt eine
   umgerechnete Summe.
4. **Detailtabellen sind auf 1'000 Zeilen gekappt** (Pruefbuch und
   Gruppenmarge-Detail, von rund 92'000). Die Kappung wirkt beim Pruefbuch VOR
   den Spaltenfiltern. Fuer Nachrechnungen den Excel-Export nehmen.

Zweck: Kompakte, code-verifizierte Referenz WIE Waehrungsumrechnung, Marge/Standardkosten
und Land-Formeln rechnen — nicht Deploy-Historie (die steht in `docs/rag/FINANCE.md`).
Bei Detailfragen die verlinkte Rohquelle laden, nicht raten.

## 1. Gesamt-Datenfluss

```
Rohdaten je Standort (SAP OData / HANA-B1 / Sage-CSV / Alphaplan-CSV)
 -> Adapter (SapGatewayDataSourceAdapter / HanaDataSourceAdapter / ManualExcelDataSourceAdapter)
 -> SalesRecord-Liste (Mapping, Vorzeichen fuer Gutschriften bereits gesetzt)
 -> FieldTransformationRules (Normalisierung, optional ConvertCurrency)
 -> optional Audit-CSV "Sales_ProcessedMergeInput_<TSC>_<Datum>.csv"  <- Nachweis nach Mapping
 -> Standort-Excel "Sales_<TSC>_<Datum>.xlsx"
 -> CentralSalesRecords (DELETE+INSERT nur fuer diesen Standort)
 -> FinanceRuleEngine (Include/Exclude, Gutschriften-Vorzeichen, Land-Dedup)
 -> Finance Summary / Soll-Ist / Pruefbuch / Sales_All-Excel / Nachweis-Excel
```

Produktiv liest die zentrale Auswertung NICHT `CentralSalesRecords`, sondern je TSC die
neuesten `Sales_ProcessedMergeInput_*.csv` (Fallback `Finance_Dashboard_Audit_All_*.csv`).
Details: `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md`.

## 2. Formel pro Land

| Land | Quelle | Nettoformel (Hauswaehrung) | Gutschrift/Storno | Besonderheit |
| --- | --- | --- | --- | --- |
| CH/AT | SAP OData `ZSCHWEIZ`/`FinanzdataSchweizOeSet` | `Sum(Z.NetwrHc)` | ueber `Z.Fkart` | Sparten direkt per Join `Z.Matnr=P.Matnr`; Faktor-100-Bug bei Fremdwaehrung (s. Abschnitt 3) |
| DE (TRDE) | Alphaplan CSV-Paar `invoice_headers`/`invoice_lines`, Full+Delta | `invoice_lines.NettoPreisGesamt` | `DocumentType = Alphaplan CreditNote` | `ArtikelNummer` ist keine SAP-MATNR -> Sparte unsicher |
| ES (TRES) | Sage SQL/CSV | `ImporteNeto` | negativ bei `TipoNuevaFra=2` ODER `SerieFactura='REC'` | Soll 2025 korrigiert (alter Wert war Excel-Fehler) |
| FR (TRFR) | B1/HANA `fr01_p` | `INV1.LineTotal`; Credit: `RIN1.LineTotal * -1` | kompletter Zeilensatz `*-1` | Referenzfall, kaum Abweichung |
| IT (TRIT) | B1/HANA `it01_p` | wie FR, PLUS Kontenfilter `AcctCode LIKE '47005%' AND NOT LIKE '4700504%'` (hartcodiert) + Kundenausschluss „Trafag Italia" | wie FR | groesste Baustelle, Restdifferenz offen; Dublettenregel bei leerem Supplier Country |
| UK (TRUK) | Manual Excel aus Sage (Ordnername „UK_B1" ist irrefuehrend, KEIN SAP B1) | `[Sales Price/Value] * [Quantity]` | negativ bei CREDIT/ABONO/GUTSCHRIFT/CRN/CN | Restdifferenz `-5'261.91 GBP` ungeklaert |
| IN (TRIN) | Sage/HANA `TRAFAG_LIVE` | wie B1-Schema | — | INR fuehrend |
| US (TRUS) | B1/HANA `us01_p` | wie FR | wie FR | kaum Abweichung |

Jahresabgrenzung ueberall: `Year(PostingDate ?? InvoiceDate ?? ExtractionDate)`.
Formeln im Detail: `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`,
IT-Sonderfall: `docs/FINANCE_IT_VORGEHEN_2026-05-18.md`, UK-Korrektur:
`docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md`.

## 3. Waehrungsumrechnung — drei getrennte Konzepte

**a) Hauswaehrung (Standard-Ist)** — fuehrt den offiziellen Soll/Ist-Abgleich, keine Umrechnung.

**b) Group-Currency/CHF (Anzeige, Management Cockpit)**
```
Anzeige-Wert je Zeile = Quellwert * ResolveRate(Quellwaehrung, CHF, Kursdatum)
```
`ResolveRate`-Reihenfolge: gleiche Waehrung=1 -> direkter aktiver Kurs -> inverser Kurs
(1/Rate) -> Kreuzkurs ueber EUR -> sonst `null`. Kursdatum kommt aus der Belegzeile
(`PostingDate`->`InvoiceDate`->`ExtractionDate`), KEIN fixer 31.12.-Stichtag. Fehlender
Kurs: Zeile zaehlt mit 0, `MissingExchangeRateCount` erhoeht sich sichtbar.

**c) Budget-CHF** — separate, engere Formel:
```
Net Sales Actual CHF Budget = Net Sales Actual * Budgetkurs(Local->CHF, Finance-Jahr)
```
Kurs muss exakt `Notes = 'Budget <Jahr>'` tragen, damit offene ECB-Tageskurse den
Budgetkurs nicht ueberschreiben. Offen (Finanzchef): Freigabe, Pflegeprozess, Rundung,
Verhalten bei fehlendem Kurs. Details: `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md`.

`DocumentRate` aus dem ERP wird gespeichert, aber NIE automatisch fuer eine
Dashboard-Umrechnung verwendet — nur die drei Wege oben. Details:
`docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`.

**Bekannter Bug:** `NETWR_HC`-Faktor-100 bei CH/AT-Fremdwaehrungszeilen (~38.5% betroffen)
— ein Umsatzfehler, keiner der Kostenbasis. C#-seitig selbstdeaktivierend kompensiert
(`SapCompositionService.CorrectHouseCurrencyScaling`). Details:
`docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` Abschnitt 13/14.

## 4. Marge, Standardkosten, Deckungsbeitrag

Grundformel:
```
Kostenbasis (Zeile) = Menge * StandardCost
Marge                = Umsatz - Kostenbasis
Marge %              = Marge / Umsatz
```
`Marge`/`%` werden zu `-`, sobald die Kostenbasis fuer eine Zeile (und damit fuer die
ganze Land/Sparte-Gruppe) nicht vollstaendig geklaert ist.

**Gerechnet wird das genau einmal:** `Services/GroupMarginCalculator.cs` (Lieferantentyp,
Kostenbasis als geordnete Regelkette, Kostenquelle, Status) — gemeinsam fuer Excel-Nachweis
UND Cockpit, gepinnt durch `GroupMarginConsistencyTests` ueber beide Einstiegspunkte.
Statuswerte, die Definition von „offen" und die Sortierung stehen ausschliesslich in
`Services/GroupMarginStatuses.cs`. Zwei Pruefungen dort NICHT verwechseln:
`IsOpen` (Kostenbasis nicht belastbar, inkl. Waehrungsabweichung) und `IsCostBasisKnown`
(Kostenbasis ueberhaupt vorhanden). Wer eine Marge rechnet, braucht `IsCostBasisKnown` —
bei „Kostenwaehrung abweichend" IST die Kostenbasis bekannt, nur in anderer Waehrung.
Genau diese Verwechslung liess das Pruefbuch bis 2026-08-06 den vollen Umsatz als Marge
ausweisen. Hintergrund und OFFENER PUNKT (Statustext `"OK"` als Zeichenkette in der
Excel-Formel): `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md`.

**Kostenbasis-Herkunft:**
- Externer Lieferant: lokale Kostenzeile aus der Quelle (DE: `NettoPreisGesamt -
  RohertragGesamt`; FR/IT/US/IN: `OITM`-Preisfelder; ES/UK oft keine Kostenspalte).
- Interner Lieferant TR AG: echte Konzernkosten aus `GroupStandardCosts` (MBEW-STPRS,
  Bewertungskreis 1100, CHF) — ueberschreibt lokale Kostenbasis, unabhaengig vom
  Verkaufsland.
- TR IT/TR IN als interner Lieferant: weiterhin offen (SAP B1 IT liefert keinen
  befuellten `PrdStdCst`/`AvgPrice`).
- Erkennung „intern" (Lieferant): Klartext-Matching von `SupplierName` in
  `GroupMarginSupplierClassifier` — s. Abschnitt 5.

**CH/AT-Formel konkret:**
```
StandardCost         = WavwrDc / Fkimg     (eingefrorener Kostenwert zum Warenausgang)
StandardCostCurrency = Waerk

Fallback (~12% ohne Lieferbezug):
StandardCost         = StprsHc             (aktueller Materialstandardpreis)
StandardCostCurrency = Hwaer
```
`WAVWR` = historisch eingefroren, `STPRS` = aktueller Stand (fachlich schwaecherer,
aber akzeptabler Fallback). Details: `docs/FINANCE_STANDARDKOSTEN_ARBEITSNOTIZ_2026-07-17.md`,
`docs/FINANCE_STANDARDKOSTEN_2026-07-14.md`.

**Deckungsbeitrag (DB):** `StandardCostVariable`/`StandardCostFixed` +
`ContributionMarginCalculator` sind technisch vorbereitet, aber LEER — Fix/Variabel-Split
wird von keiner Quelle geliefert.

**Kostenwaehrungsschalter `GroupMarginCostCurrencyMode`:**
- `Mask` (Default): Kostenwaehrung != Verkaufswaehrung -> Status `Kostenwaehrung abweichend`, Marge bleibt `-`.
- `Convert`: Umrechnung mit Jahreskurs 31.12.

**Statuswerte:** `OK` (Marge berechnet) / `Standardpreis fehlt` / `Lieferant unklar` /
`Kostenwaehrung abweichend` (nur Mask).

**Aktuell groesster ungeloester Konflikt:** CH/AT, UK (teils ES) haben strukturell KEINE
Supplier-Felder. `GroupMarginSupplierClassifier` liefert bei 3 leeren Feldern immer
`Unklar` -> jede CH/AT-Zeile bekommt `Lieferant unklar` -> Marge maskiert, OBWOHL die
WAVWR/STPRS-Kostenbasis seit 2026-07-16 zu 96.5%/99.9% gefuellt ist. Offene Fachfrage an
Andreas: CH/AT regelbasiert als eigene interne Lieferkategorie werten? Details:
`docs/FINANCE_GRUPPENMARGE_2026-06-16.md` Nachtrag 2026-07-17.

## 5. Trafag / Magnetic Sense / GFS — DREI verschiedene Filter (nicht verwechseln!)

Code-verifiziert 2026-07-27 (`Services/GroupMarginSupplierClassifier.cs`,
`Services/FinanceRuleEngine.cs`, `Services/ManagementCockpitService.cs`,
`Services/DatabaseSeedService.cs`):

| Mechanismus | Wofuer | Marker | Matching |
| --- | --- | --- | --- |
| `FinanceIntercompanyRule` (DB, admin-pflegbar via Einstellungen) | KUNDEN-Diagnose IC/2nd-party in `Management Analyse > Laender` (Ist vs. Ist-ohne-IC) | `TRAFAG`, `MAGNETIC SENSE`, `MAGNETS SENSE`, `GESELLSCHAFT FUER/FUR SENSORIK` + 2 IT-Kundennummern (`DatabaseSeedService.EnsureFinanceIntercompanyRuleDefaults`) | simples `Contains`, case-insensitive, Umlaute normalisiert (`NormalizeRuleText`) |
| `FinanceRuleEngine` (hartcodierte Seed-Regeln) | KUNDEN komplett aus dem Land-Ist AUSSCHLIESSEN (nicht nur Diagnose) | z.B. DE: `"Magnetic Sense"` (Weiterberechnung), IT: `"Trafag Italia"` | simples `Contains` |
| `GroupMarginSupplierClassifier` (Gruppenmarge-Feature) | LIEFERANTEN intern/extern fuer die Margenberechnung | `TRAFAG`, `TR-AG`, `TRCH`, `TRIT`, `TRIN`, `GFS`, `GESELLSCHAFT FUER/FUR SENSORIK` — **KEIN** „Magnetic Sense" | **Wortgrenzen-Regex** (bewusst kein Contains) |

Wichtig: Nur die ersten zwei (Kunden-Klassifizierung) filtern auch auf „Magnetic Sense".
Die Gruppenmarge-Lieferantenklassifizierung filtert NUR auf Trafag/GFS-Begriffe, bewusst
OHNE Magnetic Sense, und nutzt Wortgrenzen statt Contains — ein simples Contains haette
sonst „Triton"->`TRIT`, „Trinity"->`TRIN`, „AGFS-100"->`GFS` faelschlich als intern
erkannt (echter Bug, gefixt in Commit `5c9749c`; Historie: `29f4f82` volatil auf 3 Firmen
eingegrenzt -> `e9894ce` auf Trafag-breit korrigiert -> `058f487` GFS ergaenzt). Kein
`*`-Wildcard im Code — Abgrenzung laeuft entweder ueber Contains (Kunden) oder
Wortgrenzen-Regex (Lieferanten). Unit-Tests: `TrafagSalesExporter.Tests/GroupMarginSupplierClassifierTests.cs`.
Fachgrundlage Kunden-Marker: `docs/FINANCE_ENTSCHEIDE.md` Abschnitt „Intercompany / 2nd Party".

## Rohquellen

- `docs/rag/FINANCE.md` — Kurzstand/Deploy-Historie
- `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` — Kurs-/Umrechnungsworkflow
- `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` — Budget-CHF offene Fragen
- `docs/FINANCE_ENTSCHEIDE.md` — Entscheide, Kunden-IC-Marker
- `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` — Gruppenmarge-Fachlogik
- `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md` / `docs/FINANCE_STANDARDKOSTEN_ARBEITSNOTIZ_2026-07-17.md` — Standardkosten CH/AT
- `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` — WAVWR/STPRS-Spezifikation, NETWR_HC-Bug
- `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` — Formeln je Land im Detail
- `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` — Gesamt-Datenfluss im Detail
- `docs/FINANCE_IT_VORGEHEN_2026-05-18.md` / `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` — Land-Sonderfaelle
