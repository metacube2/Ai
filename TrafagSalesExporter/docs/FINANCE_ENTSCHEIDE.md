# Finance-Entscheide fuer Net Sales Actuals

Stand: 2026-05-11

Dieses Dokument haelt die fachlichen Entscheide fuer den Finance-Abgleich fest. Es ist die verbindliche Grundlage fuer das Testprogramm, die zentrale Tabelle und den Soll/Ist-Abgleich gegen `check.xlsx`.

## Grundsatz

Net Sales Actuals werden pro Land aus dem Landessystem gelesen, in der fachlich fuehrenden Hauswaehrung bewertet und gegen die Sollwerte aus `check.xlsx` verglichen.

Die Logik darf nicht auf einzelne Testzahlen optimiert werden. Sie muss je Jahr gleich funktionieren, sofern Sollwerte und Budgetkurse fuer das jeweilige Jahr gepflegt sind.

## Entscheide

| Thema | Entscheid |
| --- | --- |
| Fuehrende Waehrung | Immer Hauswaehrung des Landessystems. |
| CHF-Umrechnung | Nur als separater Kontroll-/Reporting-Kandidat ueber Budgetkurse. Keine SNB-Tageskurse fuer den Standardabgleich. |
| Aggregation | Pro Artikel bzw. Belegposition rechnen und summieren. |
| Wertbasis | Nettofakturawert. |
| Jahresabgrenzung | Buchungsdatum. |
| Gutschriften/Storno | Separat als eigene Beleg-/Positionszeilen ausweisen. Immer ueber Artikelnummer/Positionslogik behandeln. |
| Intercompany | In einem zweiten Schritt als 2nd-party/IC ausweisen. Nicht still aus dem Standard-Ist entfernen. |

## Landesspezifische Praezisierungen

| Land | Entscheid / Regel |
| --- | --- |
| IN | Immer indische Rupien (`INR`) als Hauswaehrung. Gemischte Belegwaehrungen duerfen nicht als fachliche Summenwaehrung ausgewiesen werden. |
| IT | Hauswaehrung verwenden. Intercompany separat ausweisen und weiter fachlich abgrenzen. |
| UK | Hauswaehrung `GBP` verwenden. Die aktuell geladene Zahl wirkt wie eine Teilmenge und muss gegen vollstaendige Jahresquelle geprueft werden. |
| CH / AT | SAP-ZSCHWEIZ liefert Schweiz und Oesterreich aus gleichem System; Trennung ueber Buchungskreis bzw. Reporting-Land. |
| DE | Alphaplan-Excel; finaler Jahresfile erforderlich. Sample darf nicht als Jahres-Ist verwendet werden. |
| ES | SAGE-Excel/CSV; Serien, Gutschriften und Datumsbasis bleiben Kontrollpunkte bis fachlich final bestaetigt. |

## Intercompany / 2nd Party

Intercompany wird ueber stabile Kundenregeln klassifiziert. Aktuelle fachliche Marker:

- `TRAFAG`
- `MAGNETIC SENSE`
- `MAGNETS SENSE`
- `GESELLSCHAFT FUER SENSORIK`
- `GESELLSCHAFT FUR SENSORIK`

Weitere Uebersetzungen, Kundennummern oder lokale Schreibweisen muessen bei Bedarf ergaenzt werden.

Ergebnis im Reporting:

- Standard-Ist bleibt inklusive aller Positionen.
- 2nd-party/IC wird als separater Betrag und als Sicht "ohne 2nd-party" gezeigt.
- Finance entscheidet danach, ob und wo IC fuer offizielle Abgrenzungen ausgeschlossen wird.

## Technische Umsetzung im Programm

| Regel | Umsetzung |
| --- | --- |
| Buchungsdatum | `PostingDate` in `SalesRecord` und `CentralSalesRecord`; Finance-Abgleich filtert nach `PostingDate`. |
| Fallback Datum | Nur falls Quelle kein Buchungsdatum liefert: `InvoiceDate`, danach `ExtractionDate`. |
| Hauswaehrung | Finance-Abgleich weist bekannte Land-Hauswaehrungen aus, z. B. `INR` fuer Indien und `GBP` fuer UK. |
| Nettofakturawert | Kandidat `Nettofakturawert Hauswaehrung pro Position`. |
| B1-Belegkopfwerte | Wiederholte `DocTotal - VatSum`-Werte werden erkannt, damit Belegkopfwerte nicht pro Position multipliziert werden. |
| Budget-CHF | Budgetkurs-Kandidat wird aus Hauswaehrung pro Position gerechnet. |
| IC | `FinanceIntercompanyRules` klassifizieren 2nd-party/IC. |

## Aktuelle Kontrollpunkte

- UK: Aktuell ca. `395'605.82 GBP` bei `1'881` Zeilen gegen Soll `3'749'865.00`; Ursache ist primaer das fehlende UK-Manual-Mapping, weil `Sales Price/Value` als Stueckpreis statt als Positionswert gelesen wurde.
- IN: Anzeige muss fachlich `INR` zeigen, auch wenn Quellzeilen verschiedene Belegwaehrungen enthalten.
- IT: IC-Kundenliste final bestaetigen.
- CH / AT: echtes SAP-Buchungsdatum pruefen, falls `ZSCHWEIZ` aktuell nur Fakturadatum liefert.
- DE: finalen Jahresfile laden.
- ES: Serien und Gutschriften fachlich final bestaetigen.

## Pruefstand 2026-05-11

Die Finance-Regeln wurden im Code abgesichert und mit Tests geprueft.

Geprueft:

- Finance-Abgleich nutzt `PostingDate` fuer die Jahresabgrenzung.
- FinanceProbe-Coverage nutzt ebenfalls `PostingDate`.
- Indien wird in der Finance-Logik als `INR`-Hauswaehrung ausgewiesen, auch wenn die Quellzeilen verschiedene Belegwaehrungen enthalten.
- UK wird als `GBP`-Hauswaehrung ausgewiesen.
- Wiederholte B1-Belegkopfwerte werden erkannt, damit `DocTotal - VatSum` nicht pro Position multipliziert wird.
- 2nd-party/IC bleibt separat sichtbar.

Testergebnis:

```text
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore -p:UseAppHost=false --verbosity minimal
```

Ergebnis:

```text
58/58 Tests gruen
```

Bekannte Warnungen:

- MudBlazor Analyzer meldet bestehende `Dense`-Attribute in einzelnen Razor-Komponenten.
- NuGet-Sicherheitsdaten konnten lokal nicht von `api.nuget.org` geladen werden.

## UK / England Befund

England/UK ist im System vorhanden und wird im FinanceProbe-Abgleich angezeigt.

Aktueller Befund aus der Probe:

| Kennzahl | Wert |
| --- | ---: |
| Land | UK / England |
| TSC | `TRUK` |
| Hauswaehrung | `GBP` |
| Geladene Zeilen | `1'881` |
| Ist-Wert | `395'605.82 GBP` |
| Sollwert check.xlsx | `3'749'865.00` |
| Differenz | `-3'354'259.18` |

Interpretation:

- Die UK-Zahl ist fachlich nicht plausibel als Jahreswert.
- Wahrscheinlich wurde nur eine Teilmenge bzw. eine Monatsdatei geladen.
- Der SharePoint-Ordner `Import/Finance/UK_B1` enthaelt Dateien nach Muster `ddMMyy_TRUK.xlsx`, z. B. `010426_TRUK.xlsx` und `010526_TRUK.xlsx`.
- Die App soll die neueste passende Datei lesen. Fuer einen Jahresvergleich muss geklaert werden, ob die neueste Datei kumulierte Jahresdaten oder nur Monatsdaten enthaelt.

Naechster fachlicher Check fuer UK:

- Bestaetigen, ob `010526_TRUK.xlsx` kumuliert Januar bis Mai oder nur Mai enthaelt.
- Falls Monatsdateien geliefert werden, muss Finance entscheiden:
  - alle Monatsdateien 2025 aufsummieren, oder
  - nur einen kumulierten Jahresfile lesen.

## Nachtrag 2026-05-11: UK_B1 Mapping

Der UK-Befund wurde nachtraeglich technisch untersucht.

Wichtige Feststellungen:

- Quelle bleibt `UK_B1`.
- Der Standort ist `England`, `TSC = TRUK`, `SourceSystem = MANUAL_EXCEL`.
- Der korrekte SharePoint-Ordner ist:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/UK_B1
```

- Lokal war fuer `TRUK` kein grafisches Manual-Excel-Mapping vorhanden.
- Dadurch hat der Fallback-Importer `Sales Price/Value` direkt als Positionswert uebernommen.
- In der UK-B1-Datei ist `Sales Price/Value` aber ein Stueckpreis.
- Der fachliche Positionswert muss pro Belegposition berechnet werden:

```text
Positionswert = [Sales Price/Value] * [Quantity]
```

Technische Probe auf den bereits importierten UK-Zeilen:

| Variante | Wert |
| --- | ---: |
| Bisherige Summe `SalesPriceValue` | `395'605.82 GBP` |
| Rekonstruierte Summe `SalesPriceValue * Quantity` | `3'533'348.89 GBP` |
| Sollwert check.xlsx | `3'749'865.00 GBP` |
| Restdifferenz nach Multiplikation | ca. `-216'516.11 GBP` |

Bewertung:

- Die grosse UK-Abweichung war hauptsaechlich ein Mapping-Fehler.
- Nach korrekter Multiplikation bleibt eine relevante Restdifferenz.
- Diese Restdifferenz muss gegen UK-spezifische Netto-/Discount-/Fracht-/Nebenpositionsspalten oder eine andere Abgrenzung im UK-Export geprueft werden.
- Die bisherige Interpretation "nur Monatsfile/Teilmenge" ist nicht mehr die wahrscheinlichste Hauptursache, bleibt aber als Datenvollstaendigkeitscheck offen.

Ziel-Mapping fuer `TRUK`:

| Zielfeld | Quelle |
| --- | --- |
| `Tsc` | `TSC` |
| `Land` | `Land` |
| `InvoiceNumber` | `Invoice Number` |
| `PositionOnInvoice` | `Position on invoice` |
| `Material` | `Material` |
| `Name` | `Name` |
| `ProductGroup` | `Product Group` |
| `Quantity` | `Quantity` |
| `CustomerNumber` | `Customer number` |
| `CustomerName` | `Customer name` |
| `CustomerCountry` | `Customer country` |
| `SalesPriceValue` | `=[Sales Price/Value]*[Quantity]` |
| `SalesCurrency` | `=GBP` |
| `DocumentCurrency` | `=GBP` |
| `CompanyCurrency` | `=GBP` |
| `PostingDate` | `invoice date` |
| `InvoiceDate` | `invoice date` |
| `DocumentType` | `=Manual Excel` |

Code-Stand dazu:

- `ManualExcelImportService` unterstuetzt im grafischen Manual-Excel-Mapping einfache Multiplikationsausdruecke mit Excel-Headern:

```text
=[Header A]*[Header B]
```

- Konstanten wie `=GBP` funktionieren unveraendert.
- `DatabaseSeedService` repariert den alten/falschen England-Pfad auf `UK_B1` und seedet das `TRUK`-Mapping.
- Ein Unit-Test prueft, dass `SalesPriceValue = [Sales Price/Value] * [Quantity]` korrekt gelesen wird.

Aktueller Verifikationsstand:

- Die neue UK-Mapping-Logik ist implementiert.
- `DatabaseSeedService` seedet das UK-Mapping nur, wenn `ManualExcelColumnMappings` sauber auf `Sites` referenziert.
- Damit blockieren alte SQLite-Reparaturreferenzen wie `Sites_repair_old` den Initialisierungslauf nicht mehr.
- Der volle Testlauf ist gruen:

```text
59/59 Tests gruen
```

Naechster praktischer Schritt:

- App oder FinanceProbe starten, damit die lokale DB den Seed/Repair bekommt.
- Danach UK per `/run/export/TRUK` gegen SharePoint `UK_B1` neu laden.
- Anschliessend `/finance` erneut gegen `check.xlsx` pruefen.

Praktischer Nachtrag:

- Lokale DB ist aktualisiert: `TRUK` hat den `UK_B1`-Pfad und `18` aktive Mapping-Zeilen.
- FinanceProbe laeuft auf `http://127.0.0.1:5099` und `/finance` antwortet.
- Der neue `/run/export/TRUK`-Lauf konnte noch nicht abgeschlossen werden, weil die lokale SharePoint-/Graph-Authentifizierung scheitert:

```text
ClientSecretCredential authentication failed
127.0.0.1:9 connection refused
```

- Bis dieser Zugriff funktioniert, bleibt `CentralSalesRecords` fuer UK auf dem alten Importstand.
