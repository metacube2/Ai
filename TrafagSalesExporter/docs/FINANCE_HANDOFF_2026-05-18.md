# Finance Handoff 2026-05-18

Dieses Dokument fasst den aktuellen Stand der Finance-Abstimmung zusammen, damit die Arbeit ohne Wissensverlust fortgesetzt werden kann.

## Aktueller Stand

Testprogramm:

```text
http://127.0.0.1:5099/finance
```

Die App laeuft lokal auf `127.0.0.1:5099`. Der letzte Check der Seite ergab HTTP `200`.

Aktuelle Excel:

```text
docs/FINANCE_AMPEL_LAENDER_2026-05-18_21-27.xlsx
```

Aktuelle Cache-Dateien:

```text
docs/it_cache_2025.csv
docs/spain_cache_2025.csv
```

## Nachtrag 2026-05-19

Nach diesem Handoff wurden noch vier relevante Schritte umgesetzt und committed:

1. Haupt-App-Finance-Vergleich an FinanceProbe angeglichen.
2. Leere Ist-Zeilen ohne belastbaren Ist-Wert aus dem Finance-Vergleich gefiltert.
3. Berechnungsformeln je Land dokumentiert.
4. Finance Cockpit mit separatem Login technisch geschuetzt; fachliche/produktive Abnahme noch offen.

Wichtige neue Doku:

```text
docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md
```

Diese Datei beschreibt die aktuell verwendete Soll/Ist-Logik fuer `/finance-cockpit/vergleich` und `/finance`, inklusive Jahresfilter, Kandidatenberechnung, Deduplizierung, bevorzugter Ist-Variante und landesspezifischer Quellen/Formeln.

Neue Finance-Cockpit-Sperre, Stand technisch:

- `FinanceCockpitAccessService`
- `FinanceCockpitAccessOptions`
- `FinanceCockpitUnlockPanel`
- Konfiguration in `appsettings.json` unter `FinanceCockpitAccess`
- DI-Registrierung in `Program.cs`
- Route-/Navigation-Schutz in `Routes.razor` und `NavMenu.razor`

Wichtig: Der HR-KPI-Login bleibt separat. Die neue Sperre betrifft das Finance Cockpit und laeuft wie HR-KPI ueber Benutzername plus SHA-256-Passwort-Hash. Finance hat ein eigenes Passwort:

```text
Benutzer: finance
Passwort: Trafag-Finance-Cockpit-2026!
```

AD-/Rollenpruefung ist fuer den Moment nicht geloescht, sondern in `appsettings.json` mit `Security.Enabled = false` deaktiviert. Die vorhandenen `AccessGroups` und `AdminGroups` bleiben in der Konfiguration stehen und koennen spaeter wieder aktiviert werden. Die Finance-Sperre bleibt davon unabhaengig aktiv.

### Zentrale Excel fuer CFO-/Finance-Filter

Die zentrale Datei `Sales_All_yyyy-MM-dd.xlsx` enthaelt am rechten Ende einen zusammengehoerigen Finance-Spaltenblock:

```text
Finance | Year
Finance | Country Key
Finance | Date
Finance | Net Sales Actual
Finance | Currency
Finance | Include
Finance | Source Value Field
```

Zusaetzlich wird nur in der zentralen Datei ein Hilfsblatt erzeugt:

```text
Finance Filter Hilfe
```

Damit soll Finance dieselben Ist-Summen aus Excel filtern koennen wie im Testprogramm bzw. auf `/finance-cockpit/vergleich`.

Vorgehen im Excel:

1. `Finance | Year` auf das gewuenschte Jahr filtern, z. B. `2025`.
2. `Finance | Country Key` auf Land filtern, z. B. `IT`, `UK`, `ES`, `AT`.
3. `Finance | Include = TRUE` filtern.
4. `Finance | Net Sales Actual` summieren.

Gepruefter Vergleich gegen `FinanceReconciliationService` fuer 2025:

| Key | Finance-Service | Excel-Finance-Spalten | Differenz |
| --- | ---: | ---: | ---: |
| AT | `3'438'121.37` | `3'438'121.37` | `0.00` |
| CH | `43'521'390.82` | `43'521'390.82` | `0.00` |
| ES | `3'082'320.18` | `3'082'320.18` | `0.00` |
| FR | `1'471'218.44` | `1'471'218.44` | `0.00` |
| IN | `750'936'591.38` | `750'936'591.38` | `0.00` |
| IT | `7'669'641.47` | `7'669'641.47` | `0.00` |
| UK | `3'533'710.09` | `3'533'710.09` | `0.00` |
| US | `3'749'865.33` | `3'749'865.33` | `0.00` |

Hinweis: Fuer AT/CH waehlt der Finance-Service intern `Nettofakturawert Hauswaehrung pro Position`; in den aktuellen Daten ist dieser Wert identisch mit `SalesPriceValue`, daher stimmen die Excel-Finance-Spalten exakt.

## Aktuelle Soll/Ist-Werte

| Land | Ist | Soll | Differenz | Waehrung | Status |
| --- | ---: | ---: | ---: | --- | --- |
| FR | `1'471'218.44` | `1'471'218.00` | `0.44` | EUR | OK |
| IN | `750'936'591.38` | `750'936'591.00` | `0.38` | INR | OK |
| US | `3'749'865.33` | `3'749'865.00` | `0.33` | USD | OK |
| IT | `7'669'641.47` | `7'669'840.00` | `-198.53` | EUR | Fast passend, Filter fachlich bestaetigen |
| UK | `3'533'710.09` | `3'538'972.00` | `-5'261.91` | GBP | Kleine Restdifferenz |
| AT | `3'438'121.37` | `3'443'863.00` | `-5'741.63` | EUR | Kleine Restdifferenz |
| ES | `3'082'320.18` | `3'102'333.61` | `-20'013.43` | EUR | Groesste Restdifferenz |

Prioritaet fuer Folgearbeit:

1. ES pruefen: vermutlich Zuschlaege/Fracht/Nebenkosten/Rhino-Auswertungslogik.
2. AT und UK Restdifferenzen klaeren.
3. IT fachlich bestaetigen, weil der aktuelle Filter noch provisorisch ist.

## Grundregeln

- Net Sales Actuals werden in der Hauswaehrung des Landes verglichen.
- CHF ist nur Kontroll-/Reporting-Sicht ueber Budgetkurse, nicht Standardvergleich.
- Fuehrend ist Netto ohne VAT/MwSt.
- Gutschriften/Credit Notes muessen negativ in die Summe laufen.
- Standard-Ist bleibt inklusive Intercompany; IC/2nd-party wird separat gezeigt, ausser ein Land hat fachlich bestaetigte Ausschluesse.
- Jahresabgrenzung: bevorzugt Buchungsdatum/PostingDate. Wenn Quelle kein PostingDate liefert: InvoiceDate, danach ExtractionDate.

## Italien

Quelle:

- `BI1` / SAP B1 via HANA
- TSC `TRIT`
- Schema `it01_p`

Wichtige Erkenntnisse:

- Italien ist B1, nicht Sage.
- Frankreich und Italien kommen beide aus BI1/B1; FR passt mit `SalesPriceValue`.
- Screenshot `italien.png` zeigte fuer Italien die relevante B1-/Finance-Sicht auf Konto:

```text
47005 - Ricavi vendite e prestazioni
```

Aktueller technischer Arbeitsfilter in `Services/HanaQueryService.cs` fuer `it01_p`:

```sql
AcctCode LIKE '47005%'
AcctCode NOT LIKE '4700504%'
CardCode NOT IN (
  'C_IT01_0022987',
  'C_IT01_0306928',
  'C_IT01_0306138',
  'C_IT01_0309653',
  'C_IT01_0304885',
  'C_IT01_0306475'
)
```

Aus Cache berechnete Herleitung:

```text
10'603'550.59 - 2'933'909.12 = 7'669'641.47 EUR
Soll/Rhino = 7'669'840.00 EUR
Differenz = -198.53 EUR
```

Die sechs provisorisch ausgeschlossenen Kunden:

| Kunde | Betrag |
| --- | ---: |
| FAIVELEY TRANSPORT ITALIA S.P.A. | `1'689'857.70` |
| SYSTEM CERAMICS S.P.A. | `323'409.00` |
| WABTEC MZT | `282'647.40` |
| FINCANTIERI NEXTECH S.P.A | `268'166.37` |
| METAL WORK SERVICE S.R.L. | `203'425.15` |
| ELEMASTER S.P.A. | `166'403.50` |

Wichtig: Dieser IT-Filter ist ein Arbeits-/Prueffilter, noch nicht fachlich final bestaetigt.

Detaildokument:

```text
docs/FINANCE_IT_VORGEHEN_2026-05-18.md
```

## UK

Quelle:

- Fachlich Sage, nicht SAP B1.
- TSC `TRUK`.
- App-Anschluss: `MANUAL_EXCEL`.
- SharePoint-Ordner heisst technisch `Import/Finance/UK_B1`, aber das bedeutet nicht B1.

Aktuelle Berechnung:

```text
SageNetSales([Sales Price/Value], [Quantity], [Document Type], [DocumentType], [Type])
```

Bedeutung:

- `Sales Price/Value * Quantity`
- Credit Notes werden bei erkennbarem Sage-Typ negativ erzwungen.
- Waehrung ist GBP.

Wichtige Korrektur:

- UK wird gegen Local Currency/GBP geprueft.
- Der fruehere `CheckValue 3'749'865.00` war fuer UK falsch und wurde entfernt.
- Korrektes Soll fuer UK ist `LocalCurrencyValue = 3'538'972.00 GBP`.

Aktueller Stand:

```text
Ist = 3'533'710.09 GBP
Soll = 3'538'972.00 GBP
Differenz = -5'261.91 GBP
```

Detaildokument:

```text
docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md
```

## Spanien

Quelle:

- Sage CSV
- TSC `TRES`
- Datei: `sageSpain/v2/Spain_Sales_2025.csv`
- Cache: `docs/spain_cache_2025.csv`

Berechnung:

- `SalesPriceValue` aus `LineasAlbaranCliente.ImporteNeto`
- Credit Notes/REC negativ
- Datumsbasis: `FechaFactura`
- Waehrung: EUR

Aktuelle Zerlegung:

```text
Zeilen = 4'341
Invoice = 3'140'921.50 EUR
Credit Notes = -58'601.32 EUR
Ist = 3'082'320.18 EUR
Soll/Rhino = 3'102'333.61 EUR
Differenz = -20'013.43 EUR
```

Nach Serie:

| Serie | Zeilen | Betrag |
| --- | ---: | ---: |
| REG | `3'079` | `2'407'451.30` |
| LAT | `1'118` | `480'199.20` |
| PRO | `43` | `253'271.00` |
| REC | `101` | `-58'601.32` |

Bewertung:

- Differenz ist ca. `0.65%`.
- Gutschriften sind nicht das Problem, sie sind bereits negativ.
- Wahrscheinlich fehlen oder unterscheiden sich kleinere Sage-/Rhino-Bestandteile: Fracht, Portes, Zuschlaege, Rundungen, Versicherung, Finanzierung, nicht-artikelbezogene Belegpositionen oder eine andere Sage-Auswertung.
- Nicht einfach auf Belegkopf `DocumentNetAmount` wechseln: deduplizierter Belegkopf liegt bei `2'907'901.79` und passt schlechter.

## Geaenderte Programmteile

Wichtige Dateien:

```text
Services/HanaQueryService.cs
Services/ManualExcelImportService.cs
Services/DatabaseSeedService.cs
scripts/Export-SageSpainSalesCsv.ps1
SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1
TrafagSalesExporter.Tests/ManualExcelImportServiceTests.cs
```

Wichtige technische Aenderungen:

- IT: provisorischer B1-Konto-/Kundenausschlussfilter fuer `it01_p`.
- UK: `SageNetSales(...)` im Manual-Excel-Importer.
- UK: `FinanceReference` nutzt fuer UK nur `LocalCurrencyValue = 3'538'972`, kein `CheckValue`.
- ES: Sage-Spain-SQL erzwingt Credit Notes mit `-ABS(...)`.
- Test ergaenzt, der positive Credit-Note-Rohbetraege negativ macht.

## Validierung

Build:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false -p:OutDir=.\obj\verify_uk_reference\ --verbosity minimal
```

Ergebnis: erfolgreich.

Tests:

```text
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore -p:UseAppHost=false --verbosity minimal
```

Ergebnis:

```text
71/71 Tests gruen
```

FinanceProbe:

```text
http://127.0.0.1:5099/finance
```

Ergebnis: HTTP `200`.

## Commits

Relevante Commits:

| Commit | Inhalt |
| --- | --- |
| `fb85e2e` | Sage-Berechnungen korrigiert, IT/UK-Doku und Ampel ergaenzt |
| `3d40d76` | UK auf GBP Local Currency als Referenz umgestellt |
| `f721d95` | Aktuelle Excel und Spanien-Cache ergaenzt |
| `bc6bfdf` | Finance-Handoff dokumentiert |
| `8f1b1b8` | Haupt-Finance-Vergleich an Probe angeglichen |
| `f855e06` | Leere Ist-Zeilen im Finance-Vergleich gefiltert |
| `5c654ad` | Finance-Berechnungsformeln je Land dokumentiert |
| `9c544af` | Finance Cockpit mit Login technisch geschuetzt |
| `ebbc5a1` | Finance-Filterspalten in zentrale Excel ergaenzt |
| `b23f73e` | Finance-Hilfsblatt in zentrale Excel ergaenzt |

Dieses Handoff wurde danach als weiterer Commit hinzugefuegt.

## Nicht aufraeumen ohne Ruecksprache

Es gibt noch untracked/temporaere Arbeitsdateien und alte Word-/Excel-Backups. Diese wurden bewusst nicht committed, weil sie entweder temporär, defekt, Logdateien oder Zwischenstaende sind.

Bekannt uncommitted:

```text
.tmp_tools/
Tools/FinanceProbe/.tmp_tools/
verify_probe_out*/
financeprobe.*.log
docs/CFO_Kurzbericht_270515.docx
docs/CFO_Kurzbericht_270515*.bak.docx
docs/CFO_Kurzbericht_270515_NEU*.docx
docs/FINANCE_AMPEL_LAENDER_2026-05-18.xlsx
docs/FINANCE_AMPEL_LAENDER_2026-05-18_20-32.xlsx
docs/FINANCE_AMPEL_LAENDER_2026-05-19.xlsx
docs/it_cache_2025.csv
italien.png
financeprobe.*.log
mainapp.*.log
```

Wenn weitergearbeitet wird, zuerst `git status --short` pruefen und keine fremden/alten Dateien blind loeschen.

## Naechste sinnvolle Schritte

1. ES: Sage-/Rhino-Unterschied von `20'013.43 EUR` gegen Fracht/Zuschlaege/Nebenkosten pruefen.
2. AT: Differenz `-5'741.63 EUR` analysieren.
3. UK: Restdifferenz `-5'261.91 GBP` klaeren, aber UK ist jetzt nahe am LC-Soll.
4. IT: provisorischen Kundenausschluss fachlich bestaetigen oder durch offizielle B1/Rhino-Filterregel ersetzen.
5. AD-/Rollenpruefung spaeter wieder aktivieren, sobald geklaert ist, welche Gruppen produktiv gelten. Dazu `Security.Enabled` wieder auf `true` setzen; Gruppen sind nicht geloescht.
6. `/finance-cockpit/vergleich` und `/finance` nebeneinander pruefen, wenn neue Daten geladen wurden; beide sollen dieselbe `FinanceReconciliationService`-Logik nutzen.
