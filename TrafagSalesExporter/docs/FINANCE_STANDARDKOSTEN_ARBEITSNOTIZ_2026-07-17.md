# Finance Standardkosten / Margenreporting - Arbeitsnotiz

Stand: 2026-07-17

## Anlass

In der Diskussion mit Finance/Andreas ging es um die Datenquelle fuer Standardkosten im
Margen-Reporting und um die Frage, ob die aktuell verwendete CH/AT-Logik
betriebswirtschaftlich korrekt ist.

Ausgangspunkt:

- Alle angebundenen Gesellschaften liefern Rechnungspositionsdaten mit Kunde, Material,
  Menge, Preis, Standardkosten und - soweit vorhanden - Lieferant.
- Ziel ist eine Gruppenmarge aus Konzernsicht.
- Bei internen Trafag-Lieferanten sollen nicht zwingend die lokalen Verkaufszeilenkosten
  gelten, sondern die Standardkosten der liefernden Trafag-Gesellschaft.
- Fuer CH/AT ist die Materialbewertung in SAP relevant: `MBEW-STPRS`, nicht `MARA`.
- Performance des direkten `mbewSet`-Massenscans ist nicht tragfaehig.
- Finance will zusaetzlich eine Fix-/Variabel-Trennung fuer den Deckungsbeitrag.

## Erzeugte Nachweisdateien

### Stichprobe Standardkosten fuer Andreas

Datei:

```text
output/Stichprobe_Standardkosten_Andreas_2026-07-17.xlsx
```

Gepruefte Materialien:

| Material | SAP-Referenz laut Stichprobe | Zweck |
| --- | ---: | --- |
| `39101` | `51.73 CHF` | Vergleich gegen Excel `Sales!X/Y` und `Gruppenmarge Details!P/R` |
| `44997` | `25.59 CHF` | Vergleich gegen Excel `Sales!X/Y` und `Gruppenmarge Details!P/R` |

Die Datei enthaelt:

- `Zusammenfassung`
- `Sales Stichprobe`
- `Gruppenmarge Stichprobe`
- `Hinweis`

Wichtiges Ergebnis: In `Sales_All_2026-07-17.xlsx` steht der Excel-Standardkostenwert
fuer CH/TRCH nicht zwingend als derselbe CHF-Wert wie in SAP. Die App schreibt in
`Sales!X` den importierten Stueckkostenwert und in `Sales!Y` dessen Kostenwaehrung.
Bei `WAVWR` kann das die Belegwaehrung sein. Deshalb kann ein SAP-Referenzwert in CHF
in Excel als Kostenwert in EUR erscheinen, wenn die Faktura in EUR lief.

### Todo-Liste Standardkosten / Margenreporting

Datei:

```text
output/TODO_Standardkosten_Margenreporting_2026-07-17.xlsx
```

Die Datei enthaelt:

- `Todo`: Aufgaben mit Status, Prioritaet, Owner, naechstem Schritt und Nachweis
- `Befund`: Abgleich aus Markdown-Doku und Code
- `Quellen`: relevante Doku- und Code-Stellen

## Gepruefte Quellen

Markdown:

- `docs/rag/FINANCE.md`
- `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`
- `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md`
- `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md`
- `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md`

Code:

- `Services/SapCompositionService.cs`
- `Services/ContributionMarginCalculator.cs`
- `Models/SalesRecord.cs`
- `Services/GroupMarginSupplierClassifier.cs`
- `Services/ExcelExportService.cs`
- `Services/ManagementCockpitService.cs`
- `Services/ExportAuditCsvService.cs`

## Fachliche Bewertung CH/AT: WAVWR vs. STPRS

Aktuelle CH/AT-Regel in der App:

```text
StandardCost = WavwrDc / Fkimg
StandardCostCurrency = Waerk

Fallback:
StandardCost = StprsHc
StandardCostCurrency = Hwaer
```

Bewertung:

- Fuer historische Margenanalyse ist `VBRP-WAVWR / Menge` als primaere Quelle
  betriebswirtschaftlich sinnvoll, weil es den zur Fakturaposition bzw. zum
  Warenausgang gehoerenden Kostenwert abbildet.
- `MBEW-STPRS` ist fachlich der richtige SAP-Ort fuer den Standardpreis, weil die
  Bewertung nicht global in `MARA`, sondern je Bewertungskreis/Material in `MBEW`
  liegt.
- Als Fallback ist `MBEW-STPRS` akzeptabel, aber nicht gleichwertig mit `WAVWR`:
  `STPRS` ist ein Standardpreis aus der Materialbewertung und kann durch spaetere
  Kalkulationslaeufe vom historischen Kostenwert der konkreten Faktura abweichen.
- Deshalb muss der Fallback transparent bleiben: Er ist eine Standardpreisbewertung,
  nicht der eingefrorene Kostenwert der konkreten Verkaufszeile.

Praezisere Formulierung fuer Finance/Andreas:

```text
CH/AT: Primaer wird der historische Kostenwert der Fakturaposition VBRP-WAVWR als
Stueckkostenbasis verwendet (WAVWR / Menge). Wenn kein lieferbezogener Kostenwert
vorhanden ist, faellt die App auf den Materialbewertungs-Standardpreis MBEW-STPRS
zurueck. Der Fallback ist eine Standardpreisbewertung, nicht derselbe historische
Kostenbelegwert. Waehrung je Zeile: WAVWR in Belegwaehrung, STPRS in Haus-/
Bewertungswaehrung.
```

## Aktueller Umsetzungsstand

### Umgesetzt / weitgehend erledigt

- Rechnungspositionsbasis ist vorhanden.
- CH/AT lokale Kostenbasis ist seit 2026-07-16 ueber `WAVWR_DC/FKIMG` mit Fallback
  `STPRS_HC` umgesetzt.
- Der direkte `mbewSet`-Massenscan ist als problematisch erkannt; der bevorzugte Weg ist
  ein SAP-/ABAP-seitig vorbereiteter kleiner Auszug.
- TR AG als liefernde Gesellschaft ist im Code fuer Konzernkosten vorbereitet:
  `GroupStandardCosts`, Bewertungskreis `1100`, Waehrung `CHF`.
- Der Kostenwaehrungsschalter `GroupMarginCostCurrencyMode` verhindert stilles Mischen
  von Verkaufs- und Kostenwaehrung.
- Deckungsbeitrag ist technisch vorbereitet:
  `StandardCostVariable`, `StandardCostFixed`, `ContributionMarginCalculator` und
  Excel-Spalten fuer DB.

### Teilweise / offen

- TR AG-Konzernkosten muessen produktiv verifiziert werden:
  `GroupStandardCosts` muss gefuellt sein und `Sales_All` muss bei passenden Zeilen
  `CostSource = Konzernkosten TR AG (MBEW-STPRS)` zeigen.
- TR IN und TR IT haben noch keine belastbare Konzernkostenquelle.
- CH/AT hat strukturell keine Supplier-Felder; dadurch bleibt die Gruppenmarge trotz
  vorhandener Kostenbasis auf `Lieferant unklar`, bis Finance eine Regel freigibt.
- UK hat weiterhin keine Kostenquelle.
- FR hat nur teilweise `StockPrice`-Abdeckung.
- Fix-/Variabel-Split ist noch nicht aus SAP/Quellsystemen geliefert; DB bleibt deshalb
  bewusst leer.
- `StandardCostCurrency = Waerk` fuer den WAVWR-Pfad sollte vom SAP-Entwickler noch
  explizit bestaetigt werden.
- Der SAP-seitige `NETWR_HC`-Faktor-100-Fehler ist C#-seitig kompensiert, sollte aber
  im SAP/ABAP-Prozess sauber geloest werden.

## Empfohlene naechste Schritte

1. Mit Andreas entscheiden, wie CH/AT ohne Supplier-Felder in der Gruppenmarge
   klassifiziert werden soll.
2. TR AG-Konzernkosten produktiv gegen `GroupStandardCosts` und `Sales_All` pruefen.
3. Fuer TR IN und TR IT die echte Herstellkosten-/Standardkostenquelle klaeren.
4. SAP-Fix-/Variabel-Felder identifizieren und in den vorbereiteten ABAP-Auszug aufnehmen.
5. Nach Lieferung des Fix-/Variabel-Splits DB-Stichprobe mit Andreas durchfuehren.
6. `WAVWR`-Waehrung (`Waerk`) und `STPRS_HC`-Waehrung (`Hwaer`) durch SAP bestaetigen
   lassen.
7. Monatlichen Prozess definieren:

```text
Kalkulationslauf / Standardkostenpflege
-> SAP-/ABAP-Auszug aktualisieren
-> Import/Reimport
-> Kostenquote pruefen
-> Sales_All und Todo-/Stichprobennachweis erzeugen
```

