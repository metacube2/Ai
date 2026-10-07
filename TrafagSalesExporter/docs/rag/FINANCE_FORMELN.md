# RAG Finance-Formeln (Zeilenverarbeitung/Mechanik)

Stand: 2026-09-29 (Punkt 4, Kostenwaehrungsschalter, Supplier-Konflikt und Periodenregel nach
der Konsistenzpruefung vom 29.09. korrigiert; uebriger Stand 2026-08-07)

**Rechenfehler aus der Pruefung vom 2026-09-29** (`docs/FINANCE_REVIEW_2026-09-29.md`, `ISS-018`):
A3 (lokales Waehrungslabel auf CHF-Summen) und A4 (Stueckpreis mit Konzernwaehrung) sind seit
2026-09-29 10:22 behoben (`91fd9dc`); die Cockpit-Exporte weisen ihre Kappung jetzt aus (A5).
**Offen:** A1, der CHF-Schalter aendert die Gruppenmarge, ohne dass das Pruefbuch mitgeht; die
Korrektur legt den Kurs fest und braucht Andreas.

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
   den Spaltenfiltern. **Korrigiert am 2026-09-29:** Auch die Excel-Exporte aus dem
   Cockpit (Pruefbuch und Gruppenmarge) enthalten nur diese 1'000 Zeilen. Fuer eine
   vollstaendige Nachrechnung gilt allein das Nachweis-Excel, das ungekappt ist
   (`docs/FINANCE_REVIEW_2026-09-29.md` Befund A5). Der fruehere Rat, dafuer den
   Excel-Export zu nehmen, war falsch.

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

Jahresabgrenzung: `Year(PostingDate ?? InvoiceDate ?? ExtractionDate)`, **ausser Spanien**:
dort gilt seit dem Entscheid vom 2026-09-02 das Rechnungsdatum zuerst (`ISS-004.2`). Die
Marktsegment-Sicht wendet diese Spanien-Regel nicht an und ist deshalb nicht voll mit Finance
abstimmbar (`docs/FINANCE_REVIEW_2026-09-29.md` Befund B5).
Formeln im Detail: `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`,
IT-Sonderfall: `docs/FINANCE_IT_VORGEHEN_2026-05-18.md`, UK-Korrektur:
`docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md`.

## 3. Waehrungsumrechnung — drei getrennte Konzepte

**a) Hauswaehrung (Standard-Ist)** — fuehrt den offiziellen Soll/Ist-Abgleich, keine Umrechnung.

**b) Group-Currency/CHF (Anzeige, Management Cockpit)**

**Geltende Kursregeln, Stand 2026-09-29 (aus dem Code ermittelt).** Es gibt drei getrennte Regeln:
*Stand 2026-10-07, Entscheid Ingo „wir arbeiten immer mit Budgetkursen": Standard ist jetzt `BudgetRate`, der Kurs mit Notiz `Budget <Finance-Jahr>`, auch wenn ein juengerer Tageskurs existiert; die Datenbank wird einmal umgestellt (Marker `BudgetRateDecision20261007Applied`). Fehlt einer Waehrung das Budget, bleibt CHF leer; nur ein Jahr ganz ohne Budgetkurse faellt auf den 31.12.-Kurs zurueck. Der CHF-Schalter im Cockpit folgt seither ebenfalls dem Profil, Regel 2 und `ISS-018.1` sind damit erledigt. Die Aufzaehlung unten ist der Stand vom 2026-09-29.*

1. Das **Kursprofil** (`GroupMarginChfRateMode`, ~~Standard und produktiv `CurrentDailyRate`~~ bis 2026-10-07, also
   der juengste heute gueltige Kurs; Alternative `FinanceYearEndRate` = 31.12. des Finance-Jahres)
   bestimmt die CHF-Werte in Pruefbuch, Nachweis-Excel und `Sales_All` sowie in Gruppenmarge und
   Finance-Pivot, **solange der Schalter „Group-Waehrung (CHF)" aus ist**.
2. *(Ueberholt 2026-10-07: der Schalter folgt jetzt dem Profil.)* Ist der **Schalter an**, rechnet das Cockpit Net Sales, Gruppenmarge und Pivot unabhaengig vom
   Profil fix zum 31.12. des Zeilenjahres um. Das Pruefbuch rechnet weiter nach Profil, deshalb
   koennen die CHF-Margen abweichen (`ISS-018.1`, offen). Ohne Schalter rechnet das Cockpit nicht
   um; bei mehreren Waehrungen im Filter steht „Mixed".
3. Die **Kostenbasis** in fremder Kostenwaehrung wird im Modus `Convert` immer zum heutigen Kurs
   in die Verkaufswaehrung umgerechnet (Beschluss vom 2026-08-27), unabhaengig vom Profil.
Ausserhalb von Finance rechnet die zentrale Auswertung mit dem Datum der Belegzeile
(`ExchangeRateDateField`, Standard `PostingDate` -> `InvoiceDate` -> `ExtractionDate`). Fehlt ein
Kurs, wird nie geschaetzt: die Zeile bleibt je nach Ausgabe in Lokalwaehrung mit Hinweis, zaehlt 0
oder bleibt leer. Code-Kommentare und der Cockpit-Hinweistext, die ein einheitliches Profil fuer
alle Sichten behaupten, sind falsch und gehoeren zur Reparatur `ISS-018.1`.

Die folgende Formel beschreibt die zentrale Auswertung ausserhalb von Finance (Regel oben,
letzter Absatz); fuer die Finance-Sichten gelten die drei Regeln:
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
  RohertragGesamt`; FR/IT/US/IN: `StockPrice` der Belegposition `INV1`/`RIN1`, nicht die
  `OITM`-Preisfelder; ES: Sage `PrecioCoste` aus unserer Export-SQL; UK: Sage-Spalte `Standard cost`). Korrigiert
  am 2026-09-29, vorher stand hier `OITM`-Preisfelder und fuer ES/UK "oft keine Kostenspalte".
- Interner Lieferant TR AG: echte Konzernkosten aus `GroupStandardCosts` (MBEW-STPRS,
  Bewertungskreis 1100, CHF) — ueberschreibt lokale Kostenbasis, unabhaengig vom
  Verkaufsland.
- TR IT/TR IN als interner Lieferant: **seit 2026-08-25 produktiv** (`b83ee84`, Deploy 15:21).
  Die Gruppenmarge ersetzt den lokalen Wert durch die Kosten aus den B1-Belegen der liefernden
  Gesellschaft, je Material den juengsten positiven `StockPrice` (`LatestPositive`, umschaltbar
  auf `AveragePositive`), abgelegt in `GroupStandardCosts` unter `TRIT` (EUR) und `TRIN` (INR).
  `OITM.PrdStdCst`/`AvgPrice` sind leer, werden dafuer aber nicht gebraucht. Offen sind nur
  Fachentscheide: juengster Wert oder Durchschnitt/Stichtag, Referenzkost `ISS-007.2`, und dass
  Italiens Wert bei Trafag-Sachnummern ein Verrechnungspreis ist (3.48-fach STPRS, akzeptiert
  mit dem Schnitt vom 2026-09-09). Die fruehere Aussage "weiterhin offen" stammte von vor dem
  25.08.
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
aber akzeptabler Fallback). Details: `docs/FINANCE_STANDARDKOSTEN.md`,
`docs/FINANCE_STANDARDKOSTEN.md`.

**Deckungsbeitrag (DB):** `StandardCostVariable`/`StandardCostFixed` +
`ContributionMarginCalculator` sind technisch vorbereitet, aber LEER — Fix/Variabel-Split
wird von keiner Quelle geliefert.

**Kostenwaehrungsschalter `GroupMarginCostCurrencyMode`:**
- `Mask` (bewusst waehlbare Ausnahme; **Code-Standard ist seit dem Beschluss vom 2026-08-27 `Convert`**, `Models/ExportSettings.cs`, Fallback in `GroupMarginCostCurrencyConverter`): Kostenwaehrung != Verkaufswaehrung -> Status `Kostenwaehrung abweichend`, Marge bleibt `-`.
- `Convert` (**Code-Standard und produktiv**): Umrechnung mit aktuellem Tageskurs (seit dem Beschluss vom 27.08.2026);
  fehlender Kurs laesst die Zeile offen.

**Statuswerte:** `OK` (Marge berechnet) / `Standardpreis fehlt` / `Lieferant unklar` /
`Kostenwaehrung abweichend` (nur Mask).

> **UEBERHOLT seit 2026-08-12:** Der folgende Absatz beschreibt den Stand vom Juli. CH/AT
> werden seit dem 11./12.08.2026 ueber den CH-Werkstamm-Fallback und lokale Standardkosten
> klassifiziert (`ISS-003.1`, erledigt), die UK-Lieferantenfelder sind gemappt, und die
> Konzernkostenkaskade ist am 2026-09-09 entschieden. Geltender Stand:
> `docs/FINANCE_SUPPLIER.md` und `docs/FINANCE_STANDARDKOSTEN.md`.

**Damals groesster ungeloester Konflikt:** CH/AT, UK (teils ES) haben strukturell KEINE
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
- `docs/FINANCE_STANDARDKOSTEN.md` / `docs/FINANCE_STANDARDKOSTEN.md` — Standardkosten CH/AT
- `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` — WAVWR/STPRS-Spezifikation, NETWR_HC-Bug
- `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` — Formeln je Land im Detail
- `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` — Gesamt-Datenfluss im Detail
- `docs/FINANCE_IT_VORGEHEN_2026-05-18.md` / `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` — Land-Sonderfaelle

## Standardkosten in CHF im Blatt Finance Details (Sales_All), 2026-10-06

Wunsch Andreas im Gespraech 2026-10-06: Umsatz, Standardkosten und Marge je Artikel in einer Waehrung vergleichen. Finance Details (Sales_All) hat dafuer neue Spalten AH bis AP:

| Spalte | Inhalt |
|---|---|
| Standard Cost, Standard Cost Currency | Stueckkosten der Zeile wie importiert |
| Standard Cost CHF Rate, Standard Cost CHF (per unit) | Kurs der Kostenwaehrung nach demselben Kursprofil wie `Net Sales CHF` (Abschnitt 3, seit 2026-10-07 `BudgetRate`: Budgetkurs des Finance-Jahres) |
| Cost Basis CHF, Margin CHF | Kostenbasis aus der Gruppenmarge (Kaskade, eine Stufe), mit dem Kurs der Verkaufswaehrung; leer, wenn die Kostenbasis nicht bekannt ist |
| Cost Source, Margin Status | welche Stufe gegriffen hat (TR AG / TR IT / TR IN / Beleg / lokal) und Status wie in Gruppenmarge Details |
| Cost Timing | historisch oder aktuell, siehe unten |

*Ueberholt am selben Tag durch den Budgetkurs-Entscheid (oben, Abschnitt 3); Messung als Historie:* **Welcher Kurs, gemessen 2026-10-07 in `Sales_All_2026-10-07.xlsx` und der Deploy-Sicherung vom 06.10.:** Profil `CurrentDailyRate`, also der heute gueltige Kurs fuer **alle Jahre**, auch 2025. Heute gueltig ist bei EUR der Eintrag `ECB daily reference rate` 0.923 vom **16.04.2026** (offen, seither nicht nachgefuehrt), bei USD 0.80, GBP 1.09, INR 1/110 der `Budget 2026`-Kurs (EUR-Budget 2026 waere 0.94). Die Umrechnung mischt damit einen veralteten EZB-Kurs mit Budgetkursen. Das ist kein neuer Fehler der CHF-Spalten, sondern gilt fuer `Net Sales CHF` seit jeher; offen fuer Andreas, ob Budget- oder Jahreskurs je Finance-Jahr gelten soll (Profil `FinanceYearEndRate` gibt je Jahr den 31.12.-Kurs, also das Budget des Jahres).

**Historisch oder aktuell (Frage Andreas):** Kosten aus der Verkaufszeile und lokale Standardkosten sind der Wert im Beleg (historisch); CH/AT nehmen WAVWR zum Warenausgang (historisch), bei rund 12 % ohne Lieferbezug STPRS (aktuell), je Zeile nicht unterscheidbar. **Konzernkosten** (TR AG MBEW-STPRS, TR IT / TR IN juengster StockPrice) sind der Stand beim letzten Abgleich und werden **nicht taeglich historisiert**; eine Abweichung je Artikel bei Konzernkosten kann also aus einer seither geaenderten Kostenbasis stammen.

**Eine Stufe:** Die Kaskade bricht nach der liefernden Gesellschaft ab (Schnitt 2026-09-09). Ein Thermostat Indien -> Italien -> Deutschland bekommt in DE die Kosten von TR IT (Italiens Einstandspreis, Verrechnungspreis), nicht Indiens Herstellkosten. Genau diesen Fall will Andreas mit Beispielen pruefen.

## Eine Stufe: Lieferantenerkennung und gemessene Luecken, 2026-10-07

Entscheid Andreas 2026-09-09 (`ISS-007.3`): extern = Standardkosten der verkaufenden Gesellschaft; intern = Kosten der **ersten liefernden** Konzerngesellschaft, dann Schnitt. Umsetzung `GroupMarginSupplierClassifier` + `GroupMarginCalculator`, gemeinsam fuer Cockpit und Excel.

**Vorrang (vor den Lieferantenfeldern):** 1. Verkauf durch TRCH/TRAT -> immer TR AG. 2. Sales Type (heute nur TRIN): LRD -> TR AG, FFM/CM -> eigene Fertigung. 3. Lieferantenfelder vorhanden -> Schritte unten. 4. Lieferant leer -> CH-Werkstamm MARC 1100 kennt den Artikel -> TR AG, sonst lokal.

**Schritt 1, intern oder extern:** Regex mit Wortgrenzen auf Lieferant-Nr, -Name und -Land: `TRAFAG`, `TR AG`, `TR-AG`, `TRCH`, `TRIT`, `TR IT`, `TRIN`, `TR IN`, `GFS`, `GESELLSCHAFT FUER SENSORIK` (Wortgrenze, damit „Triton"/„Trinity" extern bleiben).

**Schritt 2, welche Gesellschaft (nur Lieferantenname):** `TRAFAG AG`/`TR AG` -> TR AG (MBEW-STPRS, CHF); `TRAFAG ITALIA|ITALY`/`TR IT` -> TR IT (B1 StockPrice, EUR); `TRAFAG (CONTROLS) INDIA`/`TR IN` -> TR IN (B1 StockPrice, INR). Intern ohne Treffer (z. B. Trafag GmbH, GFS) -> keine Konzernkosten, „Interner Standardpreis" (lokaler Wert). „TRAFAG" allein entscheidet also nur intern/extern, nicht die Kostentabelle.

**Artikelsuche in der Kostentabelle:** ueber die Trafag-Sachnummer der Zeile (`GroupMaterialNumber`, heute nur Indien via `U_TASC_OMN`), sonst ueber die Artikelnummer. Kein Treffer -> „Interner Standardpreis" = Einkaufspreis der verkaufenden Gesellschaft; bei Indien mit Sales Type LRD bleibt die Zeile offen.

**Gemessen in `Sales_All_2026-10-07 (1).xlsx`, Thermostat-Weg Indien -> Italien -> Deutschland:**

| Verkauf | Lieferant laut Regel | Soll (1 Stufe) | Ist |
|---|---|---|---|
| TR IT, Lieferant „Trafag AG" (8'820 Zeilen) | TR AG | Konzernkosten TR AG | ok |
| TR IT, Lieferant „Trafag Controls India" (420) | TR IN | Konzernkosten TR IN | nur 5; 414 „Interner Standardpreis" (Italiens Einkaufspreis inkl. Indien-Marge). Ursache: Sachnummer in 0 von 420 Zeilen, Artikelnummern italienisch (`ITS000681`, `I37749` ...) -> `ISS-007.4` |
| TR IT, Lieferant „Trafag Italia" (429) | TR IT | Konzernkosten TR IT | 275 ok, 154 Standardpreis fehlt |
| TR DE (7'346) | TR IT | Konzernkosten TR IT | Lieferantenfeld leer in 7'346 von 7'346 (`ISS-003.3`) -> 7'212 lokale Standardkosten (DE-Einkaufspreis) |

Die von Andreas akzeptierte Ungenauigkeit (`ISS-007.3`, Margen von Indien und Italien bleiben beim Weiterverkauf an die Toechter drin, 200-300k Gruppenumsatz) setzt voraus, dass die erste Stufe greift. Heute greift sie fuer TR IT mit Ware aus Indien und fuer TR DE nicht; Andreas' Thermostat-Test zeigt deshalb Einkaufspreise der Gesellschaften, nicht die Regel.

