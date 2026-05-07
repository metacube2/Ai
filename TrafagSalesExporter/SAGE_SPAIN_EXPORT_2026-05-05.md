# Sage Spain Export

Stand: 2026-05-05

## Aktueller Kurzstatus

- Spanien-v2-Export ist technisch lauffaehig und im Testprogramm sichtbar.
- Datei: `sagespain/v2/Spain_Sales_2025.csv`
- Ist 2025: `3'082'320.18` EUR
- Soll aus `check.xlsx`: `3'102'333.61`
- Differenz: `-20'013.43`
- Status FinanceProbe: Gelb / Pruefen
- Finale Aussage: technisch importierbar, aber fachlich noch nicht abgestimmt.

FinanceProbe lokal:

```text
http://localhost:55417/finance
```

Relevante Abschnitte:

- `Meeting Ampel 2025`
- `Detail alle Laender`
- `Spain CSV direct check`

Wichtig:

- Spanien wird in der Detailtabelle nicht mehr als `Keine Daten` gezeigt, wenn `Spain_Sales_2025.csv` vorhanden ist.
- Stattdessen wird der v2-CSV-Wert mit Status `Pruefen` angezeigt.
- Die CSV-Datei kann spaeter als `MANUAL_EXCEL`-Quelle importiert werden.

## Ziel

Spanien soll Verkaufsdaten aus `Sage 200c` liefern koennen, damit der Standort in `TrafagSalesExporter` wie die anderen Laender in die zentrale Auswertung und Finance-Abgrenzung aufgenommen werden kann.

## Systemstand Spanien

Ermittelt mit `scripts/Get-SageSqlEnvironment.ps1`.

- Windows Server: `Microsoft Windows Server 2019 Standard`, Build `17763`
- Server: `WIN-4BJQJ9S1PVJ`
- Sage: `Sage 200c`
- Sage-Version: `2026.56.000`
- SQL Server: `Microsoft SQL Server 2019 Standard Edition (64-bit)`
- SQL Build: `15.0.2155.2`
- SQL Full Version: `Microsoft SQL Server 2019 (RTM-GDR) (KB5068405) - 15.0.2155.2 (X64)`
- SQL Instance: Default Instance `MSSQLSERVER`, erreichbar als `localhost`
- Datenbank: `Sage`
- Collation: `Latin1_General_CI_AI`

## Discovery

Ermittelt mit `scripts/Export-SageSqlCsv.ps1`.

Relevante Kandidaten:

- `dbo.CabeceraAlbaranCliente`
- `dbo.LineasAlbaranCliente`
- `dbo.EstadisVenta`
- `dbo.EstadisVentaTallas`
- `dbo.FacturasTB`
- `dbo.MovimientosFacturas`
- `dbo.Vis_RTDV_EfectosFactura`

Beobachtung:

- `CabeceraAlbaranCliente` ist der Verkaufs-/Albaran-Belegkopf.
- `LineasAlbaranCliente` enthaelt die Verkaufspositionen.
- `EstadisVenta` enthaelt Statistikdaten, aber im gelieferten Export keine 2025-Zeilen.
- `FacturasTB` und `MovimientosFacturas` wirken eher Finanz-/Steuer-/Buchungsdaten und enthalten gemischte Bewegungen.

## Export v2

Finaler Export-Kandidat wurde mit `SageSpainFinalExportPackage.zip` bzw. danach `v2.zip` erstellt.

Script:

- `scripts/Export-SageSpainSalesCsv.ps1`

Output von Spanien:

- `sagespain/v2/Spain_Sales_2025.csv`
- `sagespain/v2/Spain_Sales_2025_summary.txt`

Quelle:

- Header: `dbo.CabeceraAlbaranCliente`
- Lines: `dbo.LineasAlbaranCliente`
- Join:
  - `CodigoEmpresa`
  - `EjercicioAlbaran`
  - `SerieAlbaran`
  - `NumeroAlbaran`

Filter:

- `CabeceraAlbaranCliente.FechaFactura >= 2025-01-01`
- `CabeceraAlbaranCliente.FechaFactura < 2026-01-01`

Export-Spalten sind bereits auf das Zielmodell der App ausgerichtet, u. a.:

- `TSC`
- `Land`
- `InvoiceNumber`
- `PositionOnInvoice`
- `Material`
- `Name`
- `ProductGroup`
- `Quantity`
- `CustomerNumber`
- `CustomerName`
- `CustomerCountry`
- `StandardCost`
- `StandardCostCurrency`
- `PurchaseOrderNumber`
- `SalesPriceValue`
- `SalesCurrency`
- `DocumentCurrency`
- `CompanyCurrency`
- `InvoiceDate`
- `DocumentType`

## Ergebnis Export v2

Aus `Spain_Sales_2025_summary.txt`:

- Zeilen: `4'341`
- `SalesPriceValue` Summe: `3'082'320.18`
- `SalesPriceValue` = `LineasAlbaranCliente.ImporteNeto`
- Waehrung: `EUR`

Aufteilung:

- Invoices: `3'140'921.50`
- Credit Notes / REC: `-58'601.32`
- Total: `3'082'320.18`

Nach Serie:

- `REG`: `2'407'451.30`
- `LAT`: `480'199.20`
- `PRO`: `253'271.00`
- `REC`: `-58'601.32`

## Abgleich gegen check.xlsx

Sollwert fuer Spanien aus `check.xlsx`:

- `3'102'333.61`

Aktueller Export v2:

- `3'082'320.18`

Differenz:

- `-20'013.43`

Fruehere breite Positionssumme aus `LineasAlbaranCliente.ImporteNeto` ohne Join-/Rechnungsdatumsfilter lag bei:

- `3'094'474.32`
- Differenz zur Sollzahl: `-7'859.29`

## Offene fachliche Klaerung

Spanien / Finance muss noch klaeren, woher die Differenz kommt.

Zu pruefen:

1. Ist `FechaFactura` das korrekte Periodendatum?
2. Oder muss `FechaAlbaran` bzw. `FechaRegistro` verwendet werden?
3. Muessen Zeilen ohne `EjercicioFactura = 2025` in die Sollzahl?
4. Sind alle Serien `REG`, `LAT`, `PRO`, `REC` enthalten?
5. Muessen `REC`-Abos negativ abgezogen werden?
6. Gibt es weitere Serien oder Dokumenttypen ausserhalb `CabeceraAlbaranCliente` / `LineasAlbaranCliente`?
7. Gibt es eine offizielle Sage-Auswertung, die `3'102'333.61` erzeugt und deren Filter genannt werden koennen?

## Einbau ins Hauptprogramm

Umgesetzt:

- `ManualExcelImportService` kann jetzt neben `.xlsx` auch semikolongetrennte `.csv`-Dateien lesen.
- Der CSV-Reader unterstuetzt quotierte Felder und mehrzeilige Texte.
- Das Spanien-v2-CSV ist damit als `MANUAL_EXCEL`-Quelle importierbar.
- `Tools/FinanceProbe` hat einen direkten `Spain CSV direct check`.
  - Die Probe sucht automatisch nach `Spain_Sales_2025.csv`, bevorzugt unter `sagespain/v2`.
  - Angezeigt werden Zeilen, `SalesPriceValue`, Sollwert `3'102'333.61`, Differenz, Aufteilung nach `DocumentType` und `InvoiceSeries`.
  - Spanien wird in der FinanceProbe-Detailtabelle mit dem v2-CSV-Wert angezeigt, nicht mehr als `Keine Daten`.
  - In der Management-Ampel bleibt Spanien gelb, bis die Differenz fachlich geklaert ist.
- `DatabaseSeedService` stellt einen deaktivierten Spanien-Standort bereit, falls noch kein Spanien-Standort existiert:
  - `TSC = TRES`
  - `Land = Spanien`
  - `SourceSystem = MANUAL_EXCEL`
  - `IsActive = false`

Wichtig:

- Das Programm setzt den Dateipfad nicht automatisch, weil der Pfad pro Umgebung unterschiedlich ist.
- In der UI muss beim Standort Spanien die Datei `Spain_Sales_2025.csv` hinterlegt werden.
- Danach kann Spanien wie ein manueller Standort exportiert werden; die Daten landen in `CentralSalesRecords`.

## Naechster Schritt

1. App starten.
2. `Standorte` oeffnen.
3. Spanien pruefen bzw. aktivieren.
4. `SourceSystem = MANUAL_EXCEL`.
5. `Spain_Sales_2025.csv` als manuelle Datei hinterlegen.
6. Standort Spanien exportieren.
7. Finance-Probe / Dashboard erneut pruefen.
8. Differenz zu `check.xlsx` fachlich mit Spanien/Finance klaeren.

## Abgrenzung Deutschland

Am selben Tag wurde auch ein Deutschland-Beispielfile gefunden:

```text
DE_Beispiel_Export_Daten.xlsx
```

Dieses File ist nicht Teil des Spanien-Exports, aber im FinanceProbe als separater `Germany Excel sample check` sichtbar.

Deutschland-Sample:

- relevante Spalte: `NettoPreisGesamtX`
- Summe: `8'290.70` EUR
- Betragszeilen: `2`
- Bewertung: technisch lesbar, aber kein finaler DE-Jahresfile

Fuer die Gesamtampel heisst das:

- Spanien: technische v2-Datei vorhanden, Differenz offen
- Deutschland: Format verstanden, aber finale Jahresdatei fehlt
