# Backfill UK/Spanien aus App-eigenen Exporten

Stand: 2026-07-28

Anlass: Ziel ist, dass **alle Laender ab 2025 Daten haben**. Fuer die Anfangszeit haben die
Standorte noch keine Dateien geliefert — die Automatisierung setzte erst spaeter ein. Die
einzigen erhaltenen Nachweise dieser Fruehphase sind **App-eigene Exportdateien**, die Ingo
bereitgestellt hat:

- `Sales_TRUK_2026-05-11.xlsx` (0.33 MB)
- `Sales_TRSE_2026-05-20.xlsx` (0.84 MB)

Ergebnis der Vorpruefung: **Die UK-Datei ist wertvoll und wird gebraucht, die Spanien-Datei
ist redundant.** Details unten. Werkzeuge: `.tmp_tools/InspectImportXlsx`,
`.tmp_tools/CheckImportFiles`, `.tmp_tools/BuildUkBaseFile` (alle read-only bis auf das
Schreiben der neuen Basisdatei).

## 1. Beide Dateien sind App-Exporte, nicht Quellsystem-Dateien

Beide haben das kanonische Export-Layout (`extraction date`, `TSC`, `Document Entry`, ...,
`Land`, `Document Type`), die Spanien-Datei zusaetzlich die `Finance | *`-Spalten. Das ist
das Format, das die App selbst als Standortexport schreibt — nicht das Sage-Quellformat.

Daraus folgen zwei Huerden, die einen naiven „Datei in den Ordner legen"-Ansatz
**stillschweigend scheitern** lassen wuerden:

| Huerde | Regel | Folge |
| --- | --- | --- |
| Selbstfuetterungs-Schutz | `SharePointUploadService.IsOwnExportOutputFile`: `^Sales_<TSC>_\d{4}-\d{2}-\d{2}$` | **Beide Dateinamen werden vom Import ignoriert** (Schutz gegen den Bug vom 2026-07-13) |
| Spanien-Dateifilter | `IsSpainSalesFile`: Name beginnt mit `Spain_Sales` UND Endung `.csv` | Die Spanien-**xlsx** wird zusaetzlich ausgefiltert |

## 2. Bewiesen: Ein direkter Reimport wuerde die Werte verdoppeln

Das UK-Mapping berechnet

```text
SalesPriceValue = SageNetSales([Sales Price/Value], [Quantity], [Document Type], ...)
                = amount * quantity        (bei Gutschrift-Typ: -ABS(amount * quantity))
```

Im App-**Export** enthaelt die Spalte `Sales Price/Value` aber den **bereits berechneten
Zeilenwert**. Nachgewiesen an der Spanien-Datei, die beide Spalten hat:

> `Sales Price/Value` == `Finance | Net Sales Actual` bei **allen 4'341 Zeilen**, 0 Abweichungen.

Ein unveraenderter Reimport wuerde also ein **zweites Mal mit der Menge multiplizieren**.
Das war die gefaehrlichste Falle in diesem Vorgang — sie faellt nicht auf, weil die Zahlen
plausibel aussehen, nur zu hoch sind.

## 3. UK: echter Backfill, Datei ist gebaut

### Befund

| Kennzahl | Wert |
| --- | --- |
| Datenzeilen | 1'882 |
| eindeutige Belegschluessel (`Invoice|Position|Material`) | 1'868 |
| **davon bereits in `CentralSalesRecords`** | **0** |
| **davon NICHT in der DB** | **1'868** |
| `invoice date` | 2025 bei 1'881 Zeilen, 1 unlesbar |
| `Quantity = 0` | 1 Zeile (Wert ebenfalls 0, kein Verlust) |
| Gutschrift-Typ mit positivem Wert (Vorzeichenrisiko) | **0** |

Die Datei enthaelt also tatsaechlich die fehlenden UK-2025-Zeilen — keine einzige davon ist
heute in der Datenbank (die enthaelt nur 1'088 UK-Zeilen, alle 2026).

### Erzeugte Datei

`.tmp_tools/BuildUkBaseFile` rechnet die Spalte `Sales Price/Value` auf den **Stueckpreis**
zurueck (`Zeilenwert / Menge`), damit das bestehende Mapping wieder den korrekten Zeilenwert
errechnet. Die Menge bleibt unveraendert, es wird kein Feld erfunden.

**Kontrollrechnung (Reimport simuliert, inkl. Gutschriften-Vorzeichenlogik):**

```text
Summe original                  : 395'605.82
Summe nach Reimport-Simulation  : 395'605.82
Differenz                       : 0.0000   -> OK
```

Ergebnis: **`C:\Users\koi\Downloads\UK_Backfill\TRUK_2025.xlsx`**

### Warum der Dateiname genau so lauten muss

`TRUK_2025.xlsx` erfuellt alle vier Bedingungen:

| Bedingung | Pruefung |
| --- | --- |
| Nicht als eigener Export erkannt | matcht `^Sales_TRUK_\d{4}-\d{2}-\d{2}$` **nicht** |
| Als Jahres-/Basisdatei erkannt (`TryParseAnnualSiteFileName`) | enthaelt `TRUK` als Token **und** die Jahreszahl `2025` |
| Nicht als datiertes Delta erkannt | Deltas sind `ddMMyy_TRUK` (z. B. `110526_TRUK.xlsx`) |
| Sortiert vor Deltas | Sortierschluessel `0_...` (kein `_range_`) |

### Warum die Daten einen Reload ueberleben

Genau der Punkt, der wichtig war: Ein direkter Schreibvorgang in `CentralSalesRecords` waere
beim naechsten UK-Standortexport **weg**, weil Manual-Importe den Bestand je TSC ersetzen.

Mit der Datei im Quellordner gilt dagegen das Basis+Delta-Modell: bei **jedem** Lauf wird
die neueste Jahresdatei **plus** alle neueren datierten Deltas gemeinsam gelesen und
dedupliziert (`SourceLineId`, sonst Invoice/Position/Material; spaetere Datei gewinnt).
Solange `TRUK_2025.xlsx` im Ordner liegt, ist 2025 bei jedem Import wieder dabei.

### FEHLGESCHLAGEN im ersten Versuch 2026-07-28 — Ursache gefunden

`TRUK_2025.xlsx` wurde hochgeladen und der Standortexport lief um 15:26 erfolgreich durch
(`Export erfolgreich | Rows=1088`). **Die Datei wurde aber nicht gelesen.** Laut
`AppEventLogs` (Eintrag „Neueste SharePoint-Datei ausgewaehlt") waehlte der Import:

```text
Basis:  Import/Finance/UK_B1/110326_TRUK_2026YTD.xlsx
Deltas: 130326_TRUK.xlsx | 160326_TRUK.xlsx | 180326_TRUK.xlsx | ...
```

`TRUK_2025.xlsx` fehlt in der Liste. Ergebnis unveraendert: TRUK hat weiterhin `1'088`
Zeilen, davon `0` fuer 2025.

**Ursache — Denkfehler bei der Namenswahl:** Die Auswahl in
`SharePointUploadService.ResolveManualImportFilesAsync` nimmt **genau EINE** Jahresdatei,
naemlich die mit dem hoechsten Jahr:

```csharp
var newestAnnual = allCandidates
    .Where(x => x.AnnualYear is not null)
    .OrderByDescending(x => x.AnnualYear)      // 2026 schlaegt 2025
    .ThenByDescending(x => x.SnapshotDate ?? x.Item.LastModifiedDateTime ...)
    .FirstOrDefault();
```

`110326_TRUK_2026YTD.xlsx` wird ueber `TryParseAnnualSiteFileName` als Jahr **2026**
erkannt (die Regex `(?<!\d)(20\d{2})(?!\d)` trifft das `2026` in `2026YTD`), `TRUK_2025.xlsx`
als **2025**. 2026 gewinnt, 2025 wird verworfen — ohne Meldung.

Meine urspruengliche Pruefung war unvollstaendig: Ich hatte verifiziert, dass der Name als
Jahresdatei **erkannt** wird, aber nicht, dass pro Lauf nur die **neueste** davon verwendet
wird. Ein Name allein reicht also nicht.

### Strukturelle Folge: UK kann so nie mehrere Jahre haben

Solange nur eine Jahresdatei gelesen wird, koennen 2025 und 2026 bei UK nicht
nebeneinander bestehen. Zum Vergleich: **Spanien hat dieses Problem nicht** — dort werden
ALLE `Spain_Sales*.csv` gelesen (Basis + alle Ranges) und anschliessend dedupliziert. Die
beiden Manual-Import-Pfade verhalten sich also unterschiedlich.

### Drei Loesungswege

| Weg | Vorgehen | Dauerhaft? | Aufwand |
| --- | --- | --- | --- |
| **A) Delta-Name** | `TRUK_2025.xlsx` -> `280726_TRUK.xlsx` umbenennen (28.07.26 liegt nach dem Basisdatum ~11.03.26, wird damit als Delta mitgelesen) | **Nein** — sobald jemand eine neue 2026er Jahresdatei hochlaedt, liegt deren `LastModified` nach dem 28.07. und das Delta faellt raus | minimal, nur umbenennen |
| **B) In die Jahresdatei mischen** | 2025er Zeilen in `110326_TRUK_2026YTD.xlsx` aufnehmen und unter gleichem Namen ersetzen | ja, solange die Datei nicht ohne 2025 ersetzt wird | Datei herunterladen, zusammenfuehren, hochladen |
| **C) Code angleichen** | Alle Jahresdateien lesen statt nur die neueste — bringt UK auf dasselbe Verhalten wie Spanien | ja, strukturell | Codeaenderung + Tests + Deploy |

**Empfehlung: C**, weil es die eigentliche Ursache behebt und das Ziel „alle Laender ab
2025" strukturell absichert. A ist ein Workaround mit bekanntem Ablaufdatum und
widerspricht der Anforderung, dass die Daten einen Reload ueberstehen muessen.

**Noch zu pruefen:** Ob im Ordner `UK_B1` weitere Jahresdateien liegen, die aus demselben
Grund seit jeher stillschweigend uebergangen werden. Das Log zeigt nur die ausgewaehlten
Dateien, nicht die verworfenen.

**Plausibilitaet noch offen:** `395'605.82 GBP` fuer ein ganzes Jahr wirkt niedrig. Der
frueher genannte UK-Vergleichswert `3'749'865` gilt laut
`docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` ausdruecklich **nicht mehr** fuer UK, ein
belastbarer Sollwert fuer UK 2025 liegt also nicht vor. Die Groessenordnung sollte mit
Andreas/UK gegengepruef werden, bevor UK 2025 als vollstaendig gilt — die Datei kann selbst
schon ein Teilstand gewesen sein (sie ist ein Export vom 11.05.2026 und erbt, was damals in
der DB stand).

## 4. Spanien: Datei ist redundant, echte Luecke liegt anders

| Kennzahl | Wert |
| --- | --- |
| Datenzeilen | 4'341 |
| eindeutige Belegschluessel | 4'315 |
| **davon bereits in `CentralSalesRecords` (TRES)** | **4'315 = alle** |
| **davon NICHT in der DB** | **0** |
| `invoice date` | 2025 bei allen 4'341 Zeilen |

**Die Datei enthaelt ausschliesslich 2025er Daten, die vollstaendig vorhanden sind.** Spanien
2025 ist in der DB komplett (Jan–Dez, 4'315 Zeilen).

Die tatsaechliche Spanien-Luecke ist **2026 Januar bis Mai** (0 Zeilen Jan–Apr, Mai nur 35 ab
dem 28.05.) — und die deckt diese Datei **nicht** ab. Ein Import waere also wirkungslos und
wuerde nur das Verdopplungsrisiko aus Abschnitt 2 einbringen.

**Zu tun (Spanien/Santi):** Sage-Range-Export fuer den fehlenden Zeitraum erzeugen. Der
Dateiname entsteht automatisch im richtigen Schema (`Spain_Sales_range_*.csv`), damit
`IsSpainSalesFile` greift.

### Befehl fuer Santi (auf dem spanischen Sage-SQL-Server)

Quelle: `SageSpainExportPackage/SageSpainFinalExportPackage/README.txt`.

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Export-SageSpainSalesCsv.ps1 -ExportMode Range -FromDate "2026-01-01" -ToDate "2026-06-01"
```

**ACHTUNG `ToDate` ist EXKLUSIV** (ausdruecklich im README vermerkt). Fuer „bis
einschliesslich 31.05.2026" muss deshalb `2026-06-01` angegeben werden — mit
`-ToDate "2026-05-31"` wuerde der 31. Mai fehlen.

**Kein `-DateFilter` angeben.** Ohne den Parameter filtert das Skript auf
`CabeceraAlbaranCliente.FechaFactura` (Rechnungsdatum) — das ist fuer einen historischen
Nachtrag richtig. `-DateFilter LineRegistrationDate` filtert dagegen auf den technischen
Erfassungszeitpunkt und ist laut README nur fuer **taegliche Deltas** gedacht; fuer einen
Backfill wuerde er die falschen Zeilen liefern.

Ergebnis liegt in einem Ordner auf dem Desktop
(`Sage_Spain_Sales_Export_YYYYMMDD_HHMMSS`) und heisst
`Spain_Sales_range_20260101_to_20260601.csv` plus eine `*_summary.txt`.

Falls SQL-Instanz/Datenbank abweichen: `-ServerInstance "localhost" -Database "Sage"`.

Wenn rclone auf dem Server eingerichtet ist, geht Export und Upload in einem Schritt:

```powershell
.\Run-SpainRangeExportAndUpload-AllInOne.ps1 -FromDate "2026-01-01" -ToDate "2026-06-01"
```

Andernfalls die erzeugte CSV manuell nach
`Import/Finance/Spanien` hochladen. Das Skript liest nur, es veraendert Sage/SQL nicht.

### Gelegenheit: Buchungsdatum gleich mitbestellen

Sage Spanien **hat** ein Buchungsdatum (`FacturasTB.FechaAsiento`, 100 % gefuellt), es ist
nur nicht im Export, weil dieser die Lieferschein- statt der Rechnungstabelle liest. Details
und Loesungsweg: `docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md` §1. Da Santi das Skript
ohnehin gerade anfasst, ist jetzt der guenstige Moment, das zu ergaenzen — behebt Andreas'
Issue 6.

## 5. Was NICHT gemacht wurde und warum

- **Kein Schreibzugriff auf die Produktivdatenbank.** Ein `UPDATE`/`INSERT` in die laufende
  SQLite-DB ueber SMB ist ein Korruptionsrisiko, und der Effekt waere ohnehin beim naechsten
  Import weg (s. Abschnitt 3). Der Import gehoert ueber den Standortexport gefahren.
- **Kein Upload nach SharePoint.** Dafuer ist hier kein Zugang konfiguriert; die Datei liegt
  lokal bereit.
- **Spanien-Datei nicht konvertiert**, weil sie fachlich nichts beitraegt (Abschnitt 4).
- **`PostingDate` nicht erfunden.** Nebeneffekt des UK-Backfills: das UK-Mapping setzt
  `PostingDate` und `InvoiceDate` beide aus `invoice date`, wodurch die UK-2025-Zeilen
  automatisch ein Buchungsdatum bekommen. Fuer Spanien bleibt die `PostingDate`-Frage offen
  (siehe `docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md` §1).
