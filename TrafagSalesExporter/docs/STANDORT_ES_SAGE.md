# Standort Spanien: Sage-Export und Upload

Stand: 2026-08-17. Zusammengefuehrt aus `SAGE_SPAIN_EXPORT_2026-05-05.md` (Repo-Wurzel)
und `SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03.md`.

**Die Export-SQL gehoert uns**, nicht Spanien. Fehlt ein Feld, liest zuerst unsere Query
es nicht — siehe `docs/FINANCE_FELDLUECKEN.md` Abschnitt 1.

## 1. Systemstand Spanien

Ermittelt mit `scripts/Get-SageSqlEnvironment.ps1`:

| Merkmal | Wert |
| --- | --- |
| Server | `WIN-4BJQJ9S1PVJ`, Windows Server 2019 Standard |
| ERP | Sage 200c, Version `2026.56.000` |
| SQL Server | 2019 Standard, Build `15.0.2155.2` |
| Instanz | Default `MSSQLSERVER`, erreichbar als `localhost` |
| Datenbank | `Sage` |
| Collation | `Latin1_General_CI_AI` |

Sage dokumentiert die internen SQL-Tabellen nicht oeffentlich; Tabellen- und Feldfragen
lassen sich nur am System selbst klaeren, nicht ueber die Sage-Hilfe.

## 2. Quelle und Query

| Rolle | Tabelle |
| --- | --- |
| Belegkopf (Albaran/Lieferschein) | `dbo.CabeceraAlbaranCliente` |
| Belegpositionen | `dbo.LineasAlbaranCliente` |
| Buchungsdaten | `dbo.FacturasTB` (per `OUTER APPLY`, siehe unten) |

Join Kopf zu Position: `CodigoEmpresa`, `EjercicioAlbaran`, `SerieAlbaran`, `NumeroAlbaran`.

Datumsfilter, je nach `-DateFilter`:

- `InvoiceDate` — `CabeceraAlbaranCliente.FechaFactura`
- `LineRegistrationDate` — `LineasAlbaranCliente.FechaRegistro` mit Fallback auf
  `FechaFactura`

Verkaufswert ist `LineasAlbaranCliente.ImporteNeto`. Gutschriften werden negativ gedreht,
erkannt an `TipoNuevaFra = 2`, `SerieFactura = 'REC'` oder `StatusAbono <> 0`.
Waehrung ist EUR, weil Sage `EnEuros_ = -1` liefert und `CodigoDivisa` leer ist.

Weitere Kandidatentabellen aus der Discovery, aktuell nicht gelesen: `dbo.EstadisVenta`
(Statistik, im Auszug ohne 2025-Zeilen), `dbo.MovimientosFacturas`,
`dbo.Vis_RTDV_EfectosFactura`.

### Buchungsdatum

`PostingDate` und `PostingDocument` kommen seit 2026-08-17 aus `dbo.FacturasTB`
(`FechaAsiento`, `Asiento`) per `OUTER APPLY` mit `TOP 1`. Der Schluessel
`CodigoEmpresa`/`Ejercicio`/`Serie`/`Factura` ist live verifiziert (53 von 53 Treffer auf
einem bereits gebuchten Fenster). Vollstaendige Begruendung, der Buchungsverzug von zwei
bis drei Wochen und die Konsequenz fuer das Zeitfenster des Delta-Laufs:
**`docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md`**.

## 3. Skripte

Alle im Paket `SageSpainExportPackage/SageSpainFinalExportPackage/`. Die Bedienung steht
in dessen `README.txt`, das mit den Skripten ausgeliefert wird und die operative Quelle
ist.

| Skript | Zweck |
| --- | --- |
| `Run-SpainRangeExportAndUpload-AllInOne.ps1` | **Der produktive Weg.** Eigenstaendig, macht Export, SharePoint-Pruefung und Upload. Laeuft taeglich per Taskexecuter. Default-Fenster 35 Tage |
| `Export-SageSpainSalesCsv.ps1` | reiner CSV-Export, kein Upload. Modi `Full` und `Range` |
| `Run-SpainExportAndUpload.ps1` | Export plus Upload, benoetigt das Exportskript |
| `Start-SpainRangeExportAndUpload.ps1` | einfacher Starter |
| `Analyze-SpainPostingDateKey.ps1` | read-only Diagnose fuer den Buchungsdatum-Schluessel |

`scripts/Export-SageSpainSalesCsv.ps1` ist eine byte-identische Spiegelung im Repo.
**Die Query steht mehrfach** — Aenderungen immer an allen Stellen nachziehen, sonst
laufen Voll- und Range-Export auseinander.

Ausgabedateien: `Spain_Sales_full_YYYY0101_to_YYYY1231.csv` beziehungsweise
`Spain_Sales_range_YYYYMMDD_to_YYYYMMDD.csv`, dazu je eine `_summary.txt`.
**`ToDate` ist exklusiv.**

## 4. Upload nach SharePoint

Ziel:

```text
trafag-bi:Import/Finance/Spanien
```

Der rclone-Remote `trafag-bi` zeigt auf die Bibliothek `Shared Documents` von
`https://trafagag.sharepoint.com/sites/WorldwideBIPlatform`.

Ablageorte auf dem spanischen Server: Exportausgabe und Logs unter `C:\Trafag\SageSpain`,
Skripte und `rclone.exe` unter `C:\Tools\rclone`.

rclone-Suchreihenfolge des All-in-One-Skripts: Parameter `-RcloneExe`, dann derselbe
Ordner wie das Skript, dann `C:\Tools\rclone.exe`, `C:\Tools\rclone\rclone.exe`,
`C:\Tools\rclone\rclone\rclone.exe`, zuletzt `rclone` aus dem `PATH`.

Pruefen:

```powershell
rclone lsd trafag-bi:
rclone lsf trafag-bi:"Import/Finance/Spanien"
```

### Zwei bekannte Fallen

1. **`CRITICAL: Can't set -v and --log-level`** — der Server laeuft auf einer alten
   Skriptkopie, die noch `--verbose` im rclone-Block hat. Datei durch die aktuelle
   Fassung ersetzen.
2. **`Split-Path: Path ist null`** — behoben am 2026-08-17. `Resolve-RcloneExecutable`
   nutzte `$MyInvocation.MyCommand.Path` **innerhalb einer Funktion**, wo dieser Wert in
   PowerShell zuverlaessig `$null` ist. Jetzt `$PSScriptRoot`. Wer eine aeltere Kopie
   einsetzt, laeuft erneut hinein.

## 5. Import in die Anwendung

Spanien ist ein `MANUAL_EXCEL`-Standort und haengt an SharePoint:

- Die App liest im Ordner **alle** Dateien nach dem Muster `Spain_Sales*.csv`, nicht nur
  die neueste.
- Dedupliziert wird primaer ueber `SourceLineId`, sonst ueber Invoice, Position und
  Material. Die neuere Delta-Zeile gewinnt. Deshalb sind ueberlappende Zeitfenster
  unproblematisch.
- **Die Spaltenzuordnung ist bei Spanien NICHT im Seed verdrahtet**, anders als bei UK und
  Deutschland. Neue Spalten wie `PostingDate` muessen in den Einstellungen beim Standort
  zugeordnet werden.

Details: `docs/rag/MANUAL_IMPORT.md`.

## 6. Referenzwert 2025

| Kennzahl | Wert |
| --- | --- |
| Zeilen | 4'341 |
| `SalesPriceValue` Summe | `3'082'320.18 EUR` |
| davon Rechnungen | `3'140'921.50` |
| davon Gutschriften (Serie `REC`) | `-58'601.32` |

Nach Serie: `REG` 2'407'451.30, `LAT` 480'199.20, `PRO` 253'271.00, `REC` -58'601.32.

**Finance hat am 2026-06-01 bestaetigt, dass Spanien keine echte Ist-Abweichung hat.**
Der Wert `3'082'320.18 EUR` ist die ES-Referenz 2025. Der frueher genannte Sollwert
`3'102'333.61 EUR` war ein Referenz- beziehungsweise Excel-Fehler und darf nicht mehr
als Vergleichsgroesse verwendet werden.

## 7. Offen

- Santi Gomez muss die aktuelle Fassung von `Run-SpainRangeExportAndUpload-AllInOne.ps1`
  (35-Tage-Fenster, `PostingDate`, `$PSScriptRoot`-Fix) auf dem Server einspielen.
- Danach `PostingDate` beim Standort Spanien in den Einstellungen zuordnen, Reimport,
  Jahresverteilung TRES neu messen.
- Datenluecke Januar bis 27.05.2026: der Range-Export begann erst Ende Mai. Ein
  Nachtragsexport Januar bis Mai wurde am 2026-08-17 erzeugt (1'571 Zeilen,
  `1'461'263.57 EUR`, `PostingDate` zu 100 % gefuellt) und hochgeladen.

Ansprechpartner: Santi Gomez, siehe `docs/ANSPRECHPARTNER.md`.

## Querverweise

- Buchungsdatum im Detail: `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md`
- Manual-Import und Dedupe: `docs/rag/MANUAL_IMPORT.md`
- Feldluecken und Regel „erst die eigene Query": `docs/FINANCE_FELDLUECKEN.md`
