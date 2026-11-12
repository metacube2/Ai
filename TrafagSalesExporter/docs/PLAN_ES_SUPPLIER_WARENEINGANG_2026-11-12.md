# Plan: Spanien Supplier aus dem letzten Wareneingang (ISS-003.3)

Stand: 2026-11-12 (Doku-Pruefung: Kopf nachgezogen, Phase 2 ist umgesetzt, siehe Nachtrag); davor 2026-11-12, Phase 1 (nur gelesen, nichts geaendert). Grundlage: Entscheid Andreas Stoller vom 2026-11-12,
Freigabe Ingo zur Umsetzung. Phase 2 setzt genau diesen Plan um, nachdem Ingo die offenen Entscheide in Abschnitt 8
beantwortet hat. Commit-Stand beim Lesen: `6b6efc1` (Zweig `feature/supplier-overrides-trit`).

*Nachtrag 2026-11-12 11:40: Phase 2 ist nach diesem Plan umgesetzt (Commit `c9208b5`, nicht deployed, noch nicht bei
Santi eingespielt); die Entscheide von Ingo stehen in Abschnitt 13. Zeilenangaben im Plan beziehen sich auf den Stand
`6b6efc1` vor der Umsetzung.*

Fachregel (Andreas, 2026-11-12): Pro spanischer Verkaufsposition gilt der Lieferant des **letzten Wareneingangs
desselben Artikels** in Sage als Supplier (`docs/STANDORT_ES_SAGE.md` Zeile 63-67).

## 0. Kurzfassung

1. Die Export-SQL bekommt ein zweites `OUTER APPLY ... TOP 1` auf `LineasAlbaranProveedor` mit
   `CabeceraAlbaranProveedor`; neue Spalten werden **am Ende** angehaengt, darunter genau `SupplierNumber`,
   `SupplierName`, `SupplierCountry`.
2. **Eine Zuordnung in den Einstellungen ist nach heutigem Beleg nicht noetig.** Spanien hatte am 2026-08-25 produktiv
   keine eigenen Mappingzeilen, der generische Kopfzeilen-Leser kennt die drei Spaltennamen bereits. Das muss in
   Phase 2 Schritt 1 produktiv nachgesehen werden.
3. **Kritischer Befund:** Ein einmaliger Bereichsexport ab 2025-01-01 wuerde in der App von allen spaeter beginnenden
   alten Range-Dateien ohne Lieferantenspalten wieder ueberschrieben, weil sie nach ihrem Startdatum gelesen werden
   und die zuletzt gelesene Zeile gewinnt (Abschnitt 2.6). Ohne Gegenmassnahme haetten praktisch alle 2026er-Zeilen
   keinen Lieferanten. Empfehlung: kleine C#-Regel beim Spanien-Dedupe (Abschnitt 4.2), daher ist ein Deploy noetig.
4. Santi bekommt ein Paket mit den geaenderten Skripten, einem read-only Pruefskript und einer kurzen englischen
   Anleitung (Abschnitt 6).

## 1. Ist-Stand Export (Sage-Seite) mit Belegen

### 1.1 Alle Stellen der Query

| Datei (Repo) | Rolle | Stellen | im Git |
| --- | --- | --- | --- |
| `SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1` | reiner CSV-Export, Modi Full/Range | `ValidateSet` DateFilter Z. 6, Datumsfilter Z. 134-140, SELECT Z. 142-203, FROM/JOIN Z. 204-209, `OUTER APPLY` FacturasTB Z. 229-237, Summary Z. 248-282 | ja |
| `scripts/Export-SageSpainSalesCsv.ps1` | byte-identische Spiegelung, md5 `bf961c35...` an beiden Stellen gemessen | wie oben | ja |
| `SageSpainExportPackage/SageSpainFinalExportPackage/Run-SpainRangeExportAndUpload-AllInOne.ps1` | eigenstaendig, Export plus Upload | `ValidateSet` Z. 4, Datumsfilter Z. 183-189, SELECT Z. 191-252, FROM/JOIN Z. 253-258, `OUTER APPLY` Z. 278-286, Summary Z. 302-337 | ja |
| `SageSpainExportPackage/SageSpainFinalExportPackage/Run-SpainExportAndUpload.ps1` | ruft `Export-SageSpainSalesCsv.ps1` auf, keine eigene Query | `ValidateSet` DateFilter Z. 6; **Z. 86 `--verbose`** (bekannte Falle „Can't set -v and --log-level“, `docs/STANDORT_ES_SAGE.md` Z. 127-129) | ja |
| `SageSpainExportPackage/SageSpainFinalExportPackage/Start-SpainRangeExportAndUpload.ps1` | Starter, ruft `Run-SpainExportAndUpload.ps1` | keine Query | ja |
| `SageSpainExportPackage/SageSpainFinalExportPackage/README.txt` | operative Anleitung fuer Santi | Quellenliste Z. 35-46, PostingDate-Abschnitt Z. 49-72 | ja |
| `SageSpainExportPackage/SageSpainFinalExportPackage/SageSpainFinalExportPackage.zip` | ausgeliefertes Paket (5 Dateien, Stand 2026-08-17) | Kopien der obigen Dateien | ja |

Keine eigene Verkaufsquery enthalten (gelesen und ausgeschlossen): `Export-SageSpainSales2025*.ps1` (Tabellenauszug,
Objektlisten), `Export-SageSqlCsv.ps1` (Discovery), `Analyze-SpainPostingDateKey.ps1` und `analyse.ps1`
(identisch, md5 `d8fabeeb`, Buchungsdatum-Diagnose). Die Dateien `*_old.ps1`/`README_old.txt` sind nicht versioniert
und bleiben unangetastet.

Welche Datei Santis Taskexecuter taeglich aufruft, ist nicht belegt: `docs/STANDORT_ES_SAGE.md` Z. 86 nennt das
All-in-One-Skript (Default 35 Tage, Z. 6 des Skripts), Z. 169-173 aber ein 7-Tage-Fenster; 7 Tage ist der Default von
`Run-SpainExportAndUpload.ps1` Z. 9 und `Start-...ps1` Z. 2. Deshalb werden alle vier Skripte geliefert.

Die Skripte sind reines ASCII mit LF-Zeilenenden (`file` meldet „ASCII text“). Neue Kommentare bleiben ohne Umlaute,
weil PowerShell 5.1 Dateien ohne BOM als ANSI liest.

### 1.2 Schemaauszug: was belegt ist und was nicht

Quelle `SageSpainExportPackage/v2/candidate_objects.csv` (81 Zeilen, Spalten `DatabaseName; SchemaName; ObjectName;
ObjectType; Score; ColumnCount; Columns`). **Der Auszug enthaelt nur Spaltennamen, keine Datentypen.** Typen sind
deshalb nicht belegt und werden in Santis Pruefabfrage (Abschnitt 5, Teil A) gelesen.

| Tabelle (Zeile im Auszug) | fuer den Plan benoetigte Spalten, im Auszug vorhanden |
| --- | --- |
| `dbo.LineasAlbaranProveedor` (Z. 21, 103 Spalten) | `CodigoEmpresa`, `EjercicioAlbaran`, `SerieAlbaran`, `NumeroAlbaran`, `CodigoArticulo`, `Unidades`, `UnidadesRecibidas`, `Orden`, `FechaAlbaran`, `FechaRegistro`, `TipoArticulo`, `CodigodelProveedor` |
| `dbo.CabeceraAlbaranProveedor` (Z. 13, 147 Spalten) | `CodigoEmpresa`, `EjercicioAlbaran`, `SerieAlbaran`, `NumeroAlbaran`, `CodigoProveedor`, `RazonSocial`, `Nombre`, `SiglaNacion`, `CodigoNacion`, `Nacion`, `FechaAlbaran`, `StatusAbono`, `IdAlbaranPro` |
| `dbo.LineasAlbaranCliente` (Z. 7) | `TipoArticulo`, `StatusStock`, `CodigoArticulo`, `FechaAlbaran` |

Zwei Praezisierungen gegenueber der bisherigen Doku:

- Die Positionstabelle hat **kein** `CodigoProveedor`, nur `CodigodelProveedor` (dem Namen nach die Artikelnummer
  beim Lieferanten, Vermutung). Die Lieferantennummer kommt daher nur aus dem Kopf. `docs/FINANCE_SUPPLIER.md`
  Z. 385-386 („`LineasAlbaranProveedor` u. a. mit `CodigoProveedor`“) ist ungenau.
- Der Join Kopf zu Position ueber `CodigoEmpresa`/`EjercicioAlbaran`/`SerieAlbaran`/`NumeroAlbaran` ist nur durch die
  Spaltennamen gestuetzt, analog zur Kundenseite (`Export-SageSpainSalesCsv.ps1` Z. 205-209). Er bleibt eine
  begruendete Annahme, bis Teil C der Pruefabfrage die Eindeutigkeit gemessen hat.

### 1.3 Gemessen am Verkaufsauszug `v2/Sage.dbo.LineasAlbaranCliente.csv` (2025, 4'814 Zeilen, lokal gelesen)

Das ist ein Auszug vom 2026-05-05, also ein Beleg fuer diesen Stand, nicht fuer heute.

| `TipoArticulo`, `StatusStock` | Zeilen | Summe `ImporteNeto` EUR | haeufigste Codes |
| --- | ---: | ---: | --- |
| `M`, `-1` | 3'873 | 3'064'348.51 | 46600, E082073, 52871, 53882 |
| `I`, `0` | 470 | 10'638.58 | `-` (427), GIRO_BANCARIO, E102170 |
| `C`, `0` | 443 | 13'084.23 | P999 (271, „Porte pagado“), CE001 (110), CE002 (50), RECTIF |
| `I`, `-1` | 24 | 6'108.00 | CERTIF.CONDUCTIVIDAD, E322068, H70313 |
| `M`, `0` | 4 | 295.00 | H09031, MKT1822, `-`, CE002 |

Befund: Ein leerer `CodigoArticulo` kommt im Auszug **nicht** vor (0 von 4'814); Zeilen ohne Artikel tragen den
Platzhalter `-` (428 Zeilen). Fracht (`P999`), Lieferbedingungen (`CE00x`) und Zertifikate stehen unter `C`/`I`.
Deutung `M` = Lagerware, `I` = Text/Dienst, `C` = Kosten/Konzept: **Vermutung** aus den Beispielen, nicht aus einer
Sage-Doku. `M` traegt 99.0 % des Nettowerts im Auszug (3'064'643.51 von 3'094'474.32 EUR).

Laendercodes im Kundenkopf (`v2/Sage.dbo.CabeceraAlbaranCliente.csv`): `SiglaNacion` ist ISO-2 (`ES`, `BR`, `PT`),
`CodigoNacion` ein Sage-Zahlencode (`108`), `Nacion` der Name (`ESPAÑA`). `Nombre` und `RazonSocial` weichen in 577 von
1'973 Kopfzeilen ab (z. B. Kurzname gegen Name mit „S.L.“). Dass der Lieferantenkopf dieselbe Belegung hat, ist eine
Vermutung (gleiche Spaltennamen).

## 2. Ist-Stand App-Seite mit Belegen

### 2.1 Feldnamen

`Models/SalesRecord.cs` Z. 23-25 und `Models/CentralSalesRecord.cs` Z. 31-33: `SupplierNumber`, `SupplierName`,
`SupplierCountry`.

### 2.2 Zuordnung der CSV-Spalten: keine Einstellung noetig, solange die Mappingliste leer ist

- `Services/ManualExcelImportService.cs` Z. 366-371: Gibt es aktive Mappingzeilen, gilt nur die Liste
  (`ReadMappedCsvRows`), sonst der generische Leser `ReadDefaultCsvRows`.
- Z. 38-40: der generische `HeaderMap` kennt `suppliernumber`, `suppliername`, `suppliercountry`; Z. 404-406 liest die
  drei Felder. `NormalizeHeader` (Z. 1221-1228) entfernt Nicht-Alphanumerisches und wandelt in Kleinbuchstaben.
  Unbekannte Spalten werden ignoriert (Z. 635-641). `SupplierCountryName` wird zu `suppliercountryname` und kollidiert
  damit nicht mit `SupplierCountry`.
- **Produktiv gemessen am 2026-08-25:** „Spalten-Zuordnung Spanien: keine eigenen Zeilen, generischer
  Kopfzeilen-Fallback aktiv“ (`docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` Z. 461-472,
  `docs/archiv/DEPLOYMENT_KURZSTAND_ARCHIV_bis_2026-10-12.md` Z. 724-731).
- `Services/DatabaseSeedService.cs` Z. 874-923 `EnsureSpainDateMappings`: ergaenzt `PostingDate` und
  `LineRegistrationDate` nur, wenn Spanien schon eine eigene Liste hat (Z. 886), sonst tut sie bewusst nichts.

**Widerspruch in der Doku:** `docs/STANDORT_ES_SAGE.md` Z. 144-146 („Spaltenzuordnung NICHT im Seed verdrahtet ...
muessen in den Einstellungen zugeordnet werden“) ist seit dem 2026-08-25 ueberholt; `docs/rag/MANUAL_IMPORT.md`
Z. 26-29 stimmt. Das wird in Phase 2 nachgefuehrt.

Damit gilt: **Heissen die neuen CSV-Spalten genau `SupplierNumber`, `SupplierName`, `SupplierCountry`, kommen sie ohne
Codeaenderung an**, solange die produktive Liste weiter leer ist. Das Muster `EnsureUkManualExcelMapping`
(`DatabaseSeedService.cs` Z. 1022 ff.) passt hier nicht, denn es schreibt den kompletten Satz und wuerde den
generischen Leser fuer Spanien abschalten.

Fuer `MANUAL_EXCEL` gilt zusaetzlich die Transformation `NormalizeCountryCode` auf `SupplierCountry`
(`DatabaseSeedService.cs` Z. 131-140). Der ISO-2-Code aus `SiglaNacion` passt dazu; B1 liefert ebenfalls einen
Laendercode (`HanaQueryService.cs` Z. 533-536).

### 2.3 Klassifikation bei gefuelltem ES-Lieferanten

`Services/GroupMarginSupplierClassifier.cs`:

- Reihenfolge in `Resolve` (Z. 93-168): CH/AT-Kurzschluss, dann Sales Type, dann **expliziter Lieferant** (Z. 138-144),
  erst danach Fallback (lokal oder MARC 1100). Ein gefuellter ES-Lieferant schaltet den MARC-Fallback also ab. TRES hat
  keinen Sales Type (`docs/FINANCE_SUPPLIER.md` Z. 137-139).
- **Intern oder Extern** (Z. 142-143): Wortgrenzen-Regex ueber `Nummer Name Land` mit den Markern `TRAFAG`, `TR-AG`,
  `TR AG`, `TRCH`, `TRIT`, `TR IT`, `TRIN`, `TR IN`, `GFS`, `GESELLSCHAFT FUER/FUR SENSORIK` (Z. 52-69). Jeder Name mit
  dem Wort „Trafag“ ist damit intern.
- **Liefernde Gesellschaft** (`ResolveDeliveringEntity` Z. 332-344): nur aus `SupplierName`.
  `TRAFAG AG` oder `TR AG`/`TR-AG` ergibt `TR_AG` (Z. 71-73), `TRAFAG ITALIA/ITALY` `TR_IT`, `TRAFAG (CONTROLS) INDIA`
  `TR_IN`. Jede andere Schreibweise mit „Trafag“ (z. B. „TRAFAG, A.G.“, Vermutung, wie Sage es schreibt, ist offen)
  ergibt **Intern ohne liefernde Gesellschaft**: keine Konzernkosten, Rueckfall auf den lokalen Preis mit dem Etikett
  „Interner Standardpreis“ (`GroupMarginCalculator.cs` Z. 115-127, 365-373). Deshalb muss die Pruefabfrage die exakte
  Schreibweise liefern (Teil F).

### 2.4 Kostenbasis

`Services/GroupMarginCalculator.cs`: Kette `GroupStandardCost` -> `GroupDistributionWithoutGroupCost` (nur Sales Type
`LRD`) -> `LocalStandardCost` (Z. 29-34). Konzernkosten nur mit erkannter liefernder Gesellschaft und Treffer in
`GroupStandardCosts` (Z. 320-335). Lokale Kosten sind `|Menge| x |StandardCost|` (Z. 365-379); `StandardCost` ist bei
ES `LineasAlbaranCliente.PrecioCoste` in EUR (`Export-SageSpainSalesCsv.ps1` Z. 167-169).

Waehrungs-Guard: `GroupMarginCostCurrencyMode` Default `Convert` (`Models/ExportSettings.cs` Z. 28), produktiv auf
`Convert` (`docs/FINANCE_STANDARDKOSTEN.md` Z. 880); CHF-Konzernkosten werden mit dem Tageskurs in EUR umgerechnet,
maskiert wird nur ohne Kurs (`Services/GroupMarginCostCurrencyConverter.cs` Z. 5-17).

### 2.5 Wirkung auf die ES-Zeilen (Einschaetzung, nicht gemessen)

Ausgangslage, gemessen am 2026-08-12 (`docs/FINANCE_STANDARDKOSTEN.md` Z. 87-96): TRES 5'712 Fallback-Kandidaten,
davon 3'282 CH-intern (MARC-Treffer) und 2'430 Lokal, 1'372 der Lokal-Zeilen mit Standardkosten. Die Messung ist ein
Beleg fuer ihren Tag.

Welcher Fallback-Modus produktiv aktiv ist, ist aus der Doku nicht eindeutig: `docs/rag/FINANCE.md` Z. 208-213 nennt
MARC 1100 als produktiven Default, `ISS-003.5` im Issue-Log sagt „Andreas hat am 27.08.2026 entschieden: steht kein
Lieferant, gelten die lokalen Standardkosten“. Phase 2 Schritt 1 liest den Schalter in den Einstellungen ab.

| Heute (ohne Lieferant) | Neu mit Lieferant | Wirkung auf die Zahl | Wirkung auf das Etikett |
| --- | --- | --- | --- |
| Intern/TR_AG ueber MARC (Modus MARC) | `TRAFAG AG` | keine | „Konzernkosten“ bleibt, nur explizit statt abgeleitet (wie Italien 4'089 Zeilen, `docs/FINANCE_SUPPLIER.md` Z. 436-446) |
| Intern/TR_AG ueber MARC | Fremdlieferant | **ja**: Kosten wechseln von TR-AG-STPRS (CHF, umgerechnet) auf `PrecioCoste` (EUR) | „Kosten aus Verkaufszeile“; Korrektur wie Italien 670 Zeilen |
| Intern/TR_AG ueber MARC | Trafag in anderer Schreibweise | **ja**: Konzernkosten fallen weg, lokaler IC-Preis | „Interner Standardpreis“, Status trotzdem „OK“ (ISS-018 B3.1) |
| Lokal | Fremdlieferant | keine (beide `LocalStandardCost`) | „Standardkosten der lokalen Gesellschaft“ -> „Kosten aus Verkaufszeile“ |
| Lokal | `TRAFAG AG` | im Modus MARC vermutlich keine (Material nicht in MARC; dass es dann auch keinen MBEW-Treffer hat, ist aus „0 entfallende Treffer“ vom 2026-08-11 abgeleitet, `docs/FINANCE_SUPPLIER.md` Z. 62-67, heute nicht gemessen); im Modus „lokal“ **ja**, falls STPRS vorhanden | „Interner Standardpreis“ bzw. Konzernkosten |
| Lokal/Unklar | kein Wareneingang gefunden | keine | keine |

Rechenbeispiel aus der Test-CSV `TrafagSalesExporter.Tests/ManualExcelImportServiceTests.cs` Z. 313-315 (Artikel 52871,
Menge 1, `StandardCost` 160.76 EUR, Umsatz 265.00 EUR): Wird die Zeile Extern, ist die Kostenbasis 1 x 160.76 =
160.76 EUR, die Marge 265.00 - 160.76 = 104.24 EUR, also 104.24 / 265.00 = 39.34 %. Waere sie bisher TR_AG mit einem
STPRS von `s` CHF gewesen, lag die Kostenbasis bei `s x k` (Tageskurs `k` CHF->EUR); die Differenz ist genau die
Wirkung der Zeile. Konkrete STPRS-Werte sind hier nicht gelesen worden (keine Produktiv-DB).

Die echte Verteilung ist erst nach dem ersten Lauf messbar; das Messpaket steht in Abschnitt 7 Schritt 12.

### 2.6 Kritisch: Reihenfolge der Spanien-Dateien und Dedupe

- `Services/SharePointUploadService.cs` Z. 206-213: Spanien-Dateien werden gelesen als Basisdateien zuerst, dann
  Range-Dateien **nach Startdatum aufsteigend**, dann Enddatum, dann Name.
- `Services/DataSources/ManualExcelDataSourceAdapter.cs` Z. 375-389 `DeduplicateSpainSalesRecords`: je Schluessel
  (`SourceLineId`, sonst Rechnung/Position/Material, Z. 410-425) ersetzt die zuletzt gelesene Zeile die fruehere ganz
  (`keyed[key] = record`, Z. 387). Der lokale Ordnerweg sortiert genauso nach Startdatum (Z. 352-360).
- Folge: Ein Backfill `Spain_Sales_range_20250101_to_...csv` wird **vor** dem Nachtrag `..._20260101_to_20260531.csv`
  und vor jeder taeglichen Range-Datei seit 2026-05-28 gelesen. Alle diese alten Dateien haben keine Lieferantenspalten,
  ihre Zeilen ersetzen die Backfill-Zeilen, der Lieferant waere fuer praktisch alle 2026er-Zeilen wieder leer.
- Dieselbe Falle trifft jede alte 7-Tage-Datei, die Santis Aufgabenplanung noch nach dem Backfill erzeugt, solange er
  die Skripte nicht ersetzt hat.
- Die Doku sagt dazu „Die neuere Delta-Zeile gewinnt“ (`docs/STANDORT_ES_SAGE.md` Z. 141-143,
  `docs/rag/MANUAL_IMPORT.md` Z. 129-133). Das stimmt nur fuer gleich lange, taeglich erzeugte Fenster; genau
  gilt „die spaeter beginnende Datei gewinnt“. Kandidat fuer LEARNINGS.

## 3. Aenderungen am Export (vorher/nachher)

Gilt identisch fuer `Export-SageSpainSalesCsv.ps1` (Paket und `scripts/`) und `Run-SpainRangeExportAndUpload-AllInOne.ps1`.

### 3.1 SELECT: neue Spalten am Ende

Vorher (`Export-SageSpainSalesCsv.ps1` Z. 200-203, All-in-One Z. 249-252):

```sql
    CASE
        WHEN c.TipoNuevaFra = 2 OR c.SerieFactura = 'REC' OR c.StatusAbono <> 0 THEN 'Credit Note'
        ELSE 'Invoice'
    END AS DocumentType
FROM dbo.CabeceraAlbaranCliente c
```

Nachher:

```sql
    CASE
        WHEN c.TipoNuevaFra = 2 OR c.SerieFactura = 'REC' OR c.StatusAbono <> 0 THEN 'Credit Note'
        ELSE 'Invoice'
    END AS DocumentType,
    l.TipoArticulo AS ArticleType,
    wp.CodigoProveedor AS SupplierNumber,
    COALESCE(NULLIF(LTRIM(RTRIM(wp.RazonSocial)), ''), wp.Nombre) AS SupplierName,
    wp.SiglaNacion AS SupplierCountry,
    wp.Nacion AS SupplierCountryName,
    wp.FechaAlbaran AS SupplierReceiptDate,
    wp.ReceiptDocument AS SupplierReceiptDocument
FROM dbo.CabeceraAlbaranCliente c
```

Festgelegte Spaltennamen: `ArticleType`, `SupplierNumber`, `SupplierName`, `SupplierCountry`, `SupplierCountryName`,
`SupplierReceiptDate`, `SupplierReceiptDocument`. Nur die drei `Supplier...`-Kernfelder werden von der App gelesen; die
uebrigen sind Pruef- und Nachweisspalten und werden vom generischen Leser ignoriert. `RazonSocial` hat Vorrang, weil es
der volle Firmenname ist (Kundenkopf zeigt „..., S.L.“ dort, Kurzname in `Nombre`, Abschnitt 1.3).

### 3.2 Zweites OUTER APPLY (nach dem FacturasTB-Block)

Vorher (`Export-SageSpainSalesCsv.ps1` Z. 236-238, All-in-One Z. 285-287):

```sql
    ORDER BY t.FechaAsiento, t.Asiento
) f
WHERE $datePredicate
```

Nachher (Variante V1, Empfehlung; Kommentare ASCII):

```sql
    ORDER BY t.FechaAsiento, t.Asiento
) f
-- Lieferant (Supplier), ISS-003.3, Entscheid Andreas Stoller 2026-11-12: es gilt der Lieferant
-- des letzten Wareneingangs desselben Artikels am oder vor dem Lieferdatum der Verkaufsposition.
-- OUTER APPLY mit TOP 1 wie beim Buchungsdatum: hoechstens eine Zeile je Verkaufsposition, keine
-- Vervielfachung des Umsatzes; OUTER, damit Positionen ohne Wareneingang erhalten bleiben.
-- Nur Lagerware (TipoArticulo = 'M') mit echtem Artikelcode; Fracht, Konzepte, Texte und '-'
-- bleiben leer. Nur Zugaenge mit positiver Menge, Ruecksendungen an den Lieferanten zaehlen nicht.
-- Join Kopf/Position ueber CodigoEmpresa/EjercicioAlbaran/SerieAlbaran/NumeroAlbaran: begruendete
-- Annahme aus dem Schemaauszug, geprueft mit Analyze-SpainSupplierLookup.ps1.
-- Hintergrund: docs/PLAN_ES_SUPPLIER_WARENEINGANG_2026-11-12.md im BiDashboard-Repository.
OUTER APPLY (
    SELECT TOP 1
        cp.CodigoProveedor,
        cp.RazonSocial,
        cp.Nombre,
        cp.SiglaNacion,
        cp.Nacion,
        cp.FechaAlbaran,
        CONCAT(cp.EjercicioAlbaran, '/', cp.SerieAlbaran, '/', cp.NumeroAlbaran) AS ReceiptDocument
    FROM dbo.LineasAlbaranProveedor lp
    JOIN dbo.CabeceraAlbaranProveedor cp
      ON cp.CodigoEmpresa    = lp.CodigoEmpresa
     AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
     AND cp.SerieAlbaran     = lp.SerieAlbaran
     AND cp.NumeroAlbaran    = lp.NumeroAlbaran
    WHERE l.TipoArticulo = 'M'
      AND NULLIF(LTRIM(RTRIM(l.CodigoArticulo)), '') IS NOT NULL
      AND l.CodigoArticulo <> '-'
      AND lp.CodigoEmpresa  = l.CodigoEmpresa
      AND lp.CodigoArticulo = l.CodigoArticulo
      AND lp.Unidades > 0
      AND cp.FechaAlbaran < DATEADD(day, 1, CAST(COALESCE(c.FechaAlbaran, c.FechaFactura) AS date))
    ORDER BY cp.FechaAlbaran DESC, cp.EjercicioAlbaran DESC, cp.SerieAlbaran DESC,
             cp.NumeroAlbaran DESC, lp.Orden DESC
) wp
WHERE $datePredicate
```

Begruendungen:

- **Keine Zeilenvervielfachung:** `TOP 1` im `OUTER APPLY` liefert hoechstens eine Zeile; dasselbe Muster wie beim
  Buchungsdatum (`docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` Z. 195-203). Ein gewoehnlicher JOIN wuerde bei jedem
  Artikel mit mehreren Wareneingaengen den Umsatz vervielfachen, genau der Fehler, der bei Italien ueber die
  Lieferantenadresse passiert ist (ISS-021, `docs/rag/FINANCE.md`).
- **Deterministisch:** Bei mehreren Eingaengen am selben Tag entscheidet der hoechste Belegschluessel, dann die hoechste
  Position.
- **Verkaufsdatum:** `c.FechaAlbaran` (Lieferscheindatum, exportiert als `DeliveryDate`, Z. 185), Rueckfall
  `c.FechaFactura`. Der ganze Liefertag zaehlt (`< Tag + 1`), weil Zeitanteile nicht belegt sind.
- **Gate `TipoArticulo = 'M'`:** Abschnitt 1.3; Entscheid E3. Ohne Gate bekaeme z. B. Fracht `P999` den Spediteur als
  Lieferanten, falls `P999` auch auf Einkaufslieferscheinen vorkommt (Pruefabfrage Teil G misst das).
- **`lp.Unidades > 0`:** Ruecksendungen sind kein Wareneingang. Dass Ruecksendungen mit negativer Menge gebucht werden,
  ist eine Vermutung; Teil C misst `Unidades <= 0` und `StatusAbono <> 0`. Entscheid E4.
- Varianten fuer Entscheid E1: **V2 „letzter ueberhaupt“** = dieselbe Abfrage ohne die Zeile mit `cp.FechaAlbaran <`.
  **V3 „letzter vor dem Verkauf, sonst erster danach“** = zusaetzliches `OUTER APPLY ... wn` mit
  `cp.FechaAlbaran >= DATEADD(day, 1, ...)` und `ORDER BY cp.FechaAlbaran ASC, ...`, in der SELECT-Liste
  `COALESCE(wp.x, wn.x)`.

### 3.3 Einmaliger Bereichsexport: zwei Laeufe mit den vorhandenen Filtern, keine Skriptaenderung

Der Backfill soll genau die Zeilenmenge neu liefern, die heute schon im Bestand ist, damit sich nur der Lieferant
aendert und nicht der Umsatz. Die heutige Menge ist aus zwei Schnitten zusammengesetzt:

- Basisdatei 2025 und Nachtrag Januar bis Mai 2026 nach **Rechnungsdatum** (Summaries
  `Spain_Sales_range_20250101_to_20251231_summary (1).txt` und
  `SageSpainFinalExportPackage/Spain_Sales_range_20260101_to_20260531_summary.txt`: „Date filter mode: InvoiceDate“,
  To date 2026-01-01 bzw. 2026-06-01);
- Tagesdateien ab 2026-05-28 nach **Registrierdatum** (`docs/STANDORT_ES_SAGE.md` Z. 176-179, erste Datei
  `Spain_Sales_range_20260528_to_20260603.csv`).

Deshalb zwei Laeufe mit dem All-in-One-Skript, beide Filter gibt es schon (`ValidateSet` Z. 4):

| Lauf | Befehl (Kern) | Datei | deckt ab |
| --- | --- | --- | --- |
| R1 | `-DateFilter InvoiceDate -FromDate 2025-01-01 -ToDate 2026-06-01` | `Spain_Sales_range_20250101_to_20260531.csv` | Basis 2025 und Nachtrag |
| R2 | `-DateFilter LineRegistrationDate -FromDate 2026-05-28 -ToDate <heute>` | `Spain_Sales_range_20260528_to_<gestern>.csv` | alle Tagesdateien |

Ein frueher erwogener Vereinigungsfilter („Rechnungs- oder Registrierdatum ab 2025-01-01“) ist verworfen: Er holte
nie fakturierte Lieferscheinzeilen aus der Zeit vor dem 2026-05-28 neu herein. Solche Zeilen ohne `InvoiceDate` landen
ueber `UseInvoiceDate` (`Services/FinanceRuleEngine.cs` Z. 45-46: `InvoiceDate ?? PostingDate ?? ExtractionDate`) im
Jahr des Imports und wuerden den Umsatz 2026 still erhoehen.

**Nebenbefund, vermutlich eine bestehende Luecke (nicht gemessen):** Zeilen, die vor dem 2026-05-28 registriert und
erst nach dem 2026-05-31 fakturiert wurden, liegen in keiner der beiden Mengen, also auch heute nicht im Bestand.
Teil K der Pruefabfrage zaehlt sie. Ob sie nachgeholt werden, ist ein eigener Entscheid (E9) und gehoert nicht in
diesen Lieferanten-Backfill.

### 3.4 Summary um Fuellgrad erweitern

`Export-QueryToCsv` (Export Z. 45-104, All-in-One Z. 44-103) zaehlt zusaetzlich, ueber die Spaltenindizes von
`SupplierNumber`, `ArticleType`, `InvoiceDate`, `SupplierName`:

- Zeilen mit nicht leerem `SupplierNumber`, gesamt und je `ArticleType` (Zeilen, mit Lieferant, Summe `SalesPriceValue`);
- `SalesPriceValue`-Summe und Zeilenzahl je Jahr von `InvoiceDate` (Kontrolle gegen 2025: 4'341 Zeilen,
  3'081'740.18 EUR am 2026-08-20; Referenzwert Finance 3'082'320.18 EUR, `docs/STANDORT_ES_SAGE.md` Z. 150-164);
- die 15 haeufigsten Lieferanten (`SupplierNumber` + `SupplierName`, Zeilen).

Summary-Text nachher (am Ende beider Summary-Bloecke, Export Z. 275-281, All-in-One Z. 332-336):

```text
Supplier (ISS-003.3):
Rows with supplier: <n> of <rows> (<pct> %)
By ArticleType: M <n>/<rows> ... ; I ... ; C ...
Rows and SalesPriceValue by invoice year: 2025 <rows> <sum>; 2026 ...; (no invoice date) ...
Top suppliers: <number> <name> <rows>; ...
Rule: last goods receipt of the same article on or before the delivery date (LineasAlbaranProveedor/CabeceraAlbaranProveedor), only TipoArticulo 'M', only positive units.
```

Die Zeile `Source:` (Export Z. 269-270, All-in-One Z. 326-327) wird um
`, supplier from dbo.LineasAlbaranProveedor/dbo.CabeceraAlbaranProveedor (OUTER APPLY TOP 1)` ergaenzt.

### 3.5 Nebenkorrektur

`Run-SpainExportAndUpload.ps1` Z. 86 `--verbose` entfernen (dokumentierte Falle, `docs/STANDORT_ES_SAGE.md` Z. 127-129,
README Z. 169-179). Sonst bricht der Upload ab, falls Santi diesen Weg nutzt.

### 3.6 README

`README.txt` neuer Abschnitt nach Z. 72 „Supplier (new columns, ISS-003.3)“ mit Regel, Spaltenliste, Gate und dem
Befehl fuer den einmaligen Bereichsexport; Quellenliste Z. 35-46 um die beiden Einkaufstabellen ergaenzen.

### 3.7 Performance

Indizes auf Sage sind nicht bekannt; die Skripte aendern Sage nicht und legen keine an. Das `OUTER APPLY` sucht je
Verkaufsposition in `LineasAlbaranProveedor` nach Firma und Artikel. Ohne passenden Index ist das ein Scan je Zeile.
Der Tageslauf hat vermutlich wenige hundert Zeilen (nicht gemessen), R1 rund 5'900 (2025: 4'341, Januar bis Mai 2026:
1'571, laut Summaries), R2 die Tagesmenge seit 2026-05-28 (nicht gemessen). `CommandTimeout = 0` (Export Z. 54) bedeutet: Ein langsamer Lauf bricht nicht ab, er haengt nur. Teil B
und J der Pruefabfrage messen Indizes, Tabellengroesse und Laufzeit. Falls zu langsam: Ausweichform mit vorab
gerankter Tabelle (`ROW_NUMBER() OVER (PARTITION BY lp.CodigoEmpresa, lp.CodigoArticulo ORDER BY cp.FechaAlbaran DESC,
...)`) in einem CTE; die passt aber nur zu V2 und waere damit ein neuer Entscheid.

## 4. Aenderungen in der App (C#)

### 4.1 Empfehlung zur Zuordnung: Seed vorsorglich erweitern

`EnsureSpainDateMappings` (`DatabaseSeedService.cs` Z. 874-923) wird zu `EnsureSpainOptionalMappings` umbenannt (beide
Aufrufe Z. 836, Z. 854) und ergaenzt neben den beiden Datumsfeldern `SupplierNumber`, `SupplierName`, `SupplierCountry`
mit `SourceHeader` = Feldname. Vorher Z. 892-896:

```csharp
        foreach (var targetField in new[]
                 {
                     nameof(SalesRecord.PostingDate),
                     nameof(SalesRecord.LineRegistrationDate)
                 })
```

Nachher:

```csharp
        foreach (var targetField in new[]
                 {
                     nameof(SalesRecord.PostingDate),
                     nameof(SalesRecord.LineRegistrationDate),
                     // ISS-003.3: der Sage-Export liefert seit 2026-11 den Lieferanten des letzten
                     // Wareneingangs unter genau diesen Spaltennamen.
                     nameof(SalesRecord.SupplierNumber),
                     nameof(SalesRecord.SupplierName),
                     nameof(SalesRecord.SupplierCountry)
                 })
```

Die Bedingung Z. 886 (leere Liste: nichts tun) bleibt. Bei leerer produktiver Liste ist die Aenderung wirkungslos; sie
schuetzt nur den Fall, dass jemand spaeter eine Liste anlegt. Der Kommentar Z. 866-873 wird entsprechend angepasst.
Entscheid E8 (Empfehlung ja, weil fuer 4.2 ohnehin deployt wird).

### 4.2 Empfehlung zum Ueberschreibschutz: Lieferant beim Spanien-Dedupe erben

`ManualExcelDataSourceAdapter.cs` Z. 375-389, vorher:

```csharp
        foreach (var record in ordered)
        {
            var key = BuildSpainSalesRecordKey(record);
            if (string.IsNullOrWhiteSpace(key))
                unkeyed.Add(record);
            else
                keyed[key] = record;
        }
```

Nachher:

```csharp
        foreach (var record in ordered)
        {
            var key = BuildSpainSalesRecordKey(record);
            if (string.IsNullOrWhiteSpace(key))
            {
                unkeyed.Add(record);
                continue;
            }

            // ISS-003.3: Aeltere Range-Dateien ohne Lieferantenspalten werden nach dem
            // Startdatum gelesen und koennen deshalb nach einem Bereichsexport mit Lieferant
            // kommen. Die spaetere Zeile gewinnt weiter in allen Feldern, nur ein komplett
            // leerer Lieferant wird aus der frueheren Zeile derselben Position uebernommen.
            if (keyed.TryGetValue(key, out var previous))
                InheritSupplierIfMissing(record, previous);

            keyed[key] = record;
        }
```

mit

```csharp
    private static void InheritSupplierIfMissing(SalesRecord target, SalesRecord source)
    {
        if (HasSupplier(target) || !HasSupplier(source))
            return;

        target.SupplierNumber = source.SupplierNumber;
        target.SupplierName = source.SupplierName;
        target.SupplierCountry = source.SupplierCountry;
    }

    private static bool HasSupplier(SalesRecord record)
        => !string.IsNullOrWhiteSpace(record.SupplierNumber) ||
           !string.IsNullOrWhiteSpace(record.SupplierName) ||
           !string.IsNullOrWhiteSpace(record.SupplierCountry);
```

Nur Spanien, `DeduplicateManualSalesRecords` (UK, DE) bleibt unveraendert. Die Reihenfolge der Ausgabe bleibt gleich,
weil `Dictionary` beim Ersetzen eines Werts die Position behaelt. Grenze: Wird ein Lieferant in Sage spaeter wirklich
leer (z. B. Wareneingang geloescht), bleibt der alte stehen; bei V1 ist das Ergebnis je Zeile stabil, der Fall ist
deshalb selten. Dasselbe Prinzip „nur leere Felder fuellen“ gilt schon fuer die Italien-Uebergangsliste
(`docs/FINANCE_SUPPLIER.md` Z. 417-421).

Erwogene Alternativen (Entscheid E2):

- **(a) Ohne Code:** Nach dem Backfill alle alten `Spain_Sales_*.csv` im SharePoint-Ordner `Import/Finance/Spanien` in
  einen Unterordner verschieben (die Ordnerliste ist nicht rekursiv, `SharePointUploadService.cs` Z. 137-146 liest nur
  Dateien direkt im Ordner). Voraussetzung: Der Backfill ist eine Obermenge der verschobenen Dateien. Dafuer
  Zeilenzahl und Summe je Rechnungsjahr gegen die alten Summaries pruefen. Risiko: Zeilen, die nur in alten Dateien
  stehen, verschwinden beim naechsten Import aus `CentralSalesRecords`, weil der Import den Spanien-Bestand ersetzt.
  Ausserdem wirkt der Schutz nicht, solange Santis Server noch alte Tagesdateien erzeugt.
- **(c) Reihenfolge nach Enddatum statt Startdatum** (Z. 209-211): deterministisch, aber auch hier gewinnt eine nach dem
  Backfill erzeugte alte Tagesdatei. Nicht empfohlen.

## 5. Read-only Pruefabfrage fuer Santi: `Analyze-SpainSupplierLookup.ps1` (neu)

Aufbau wie `Analyze-SpainPostingDateKey.ps1` (Verbindung Z. 28-36, `Invoke-Query` Z. 38-51, nur `SELECT`). Parameter
`-R1From` (Default `2025-01-01`), `-R1To` (Default `2026-06-01`), `-R2From` (Default `2026-05-28`), `-ToDate` (Default
heute), `-OutputDirectory` (optional, schreibt je Teil eine CSV). Wo unten `@FromDate` steht, ist `@R1From` gemeint.
Jeder Teil gibt Laufzeit in Sekunden aus (Stopwatch).

```sql
-- A: Datentypen der benutzten Spalten
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME IN ('LineasAlbaranProveedor', 'CabeceraAlbaranProveedor', 'LineasAlbaranCliente')
  AND COLUMN_NAME IN ('CodigoEmpresa', 'EjercicioAlbaran', 'SerieAlbaran', 'NumeroAlbaran', 'CodigoArticulo',
                      'CodigoProveedor', 'RazonSocial', 'Nombre', 'SiglaNacion', 'Nacion', 'FechaAlbaran',
                      'Unidades', 'Orden', 'TipoArticulo', 'StatusAbono', 'FechaRegistro')
ORDER BY TABLE_NAME, COLUMN_NAME;

-- B: Indizes (eine Zeile je Indexspalte)
SELECT t.name AS TableName, i.name AS IndexName, i.type_desc, i.is_unique, i.is_primary_key,
       ic.key_ordinal, c.name AS ColumnName
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.name IN ('LineasAlbaranProveedor', 'CabeceraAlbaranProveedor')
ORDER BY t.name, i.name, ic.key_ordinal;

-- C: Mengen, Schluessel-Eindeutigkeit, Ruecksendungen
SELECT
  (SELECT COUNT(*) FROM dbo.LineasAlbaranProveedor) AS PurchaseLines,
  (SELECT COUNT(*) FROM dbo.CabeceraAlbaranProveedor) AS PurchaseHeaders,
  (SELECT COUNT(*) FROM (SELECT CodigoEmpresa, EjercicioAlbaran, SerieAlbaran, NumeroAlbaran
                         FROM dbo.CabeceraAlbaranProveedor
                         GROUP BY CodigoEmpresa, EjercicioAlbaran, SerieAlbaran, NumeroAlbaran
                         HAVING COUNT(*) > 1) d) AS DuplicateHeaderKeys,
  (SELECT COUNT(*) FROM dbo.LineasAlbaranProveedor lp
   WHERE NOT EXISTS (SELECT 1 FROM dbo.CabeceraAlbaranProveedor cp
                     WHERE cp.CodigoEmpresa = lp.CodigoEmpresa AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
                       AND cp.SerieAlbaran = lp.SerieAlbaran AND cp.NumeroAlbaran = lp.NumeroAlbaran)) AS LinesWithoutHeader,
  (SELECT MIN(FechaAlbaran) FROM dbo.CabeceraAlbaranProveedor) AS FirstReceipt,
  (SELECT MAX(FechaAlbaran) FROM dbo.CabeceraAlbaranProveedor) AS LastReceipt,
  (SELECT COUNT(*) FROM dbo.LineasAlbaranProveedor WHERE Unidades <= 0) AS LinesUnitsNotPositive,
  (SELECT COUNT(*) FROM dbo.CabeceraAlbaranProveedor WHERE StatusAbono <> 0) AS HeadersStatusAbono;
```

Teil D (Fuellgrad und Grund je Verkaufszeile), Grundlage fuer die Zahlen an Andreas. Verkaufszeilen wie im Export
(gleicher Join, Gutschriftendrehung), Menge = R1 vereinigt mit R2 aus Abschnitt 3.3, aber **ohne** Gate, damit
sichtbar wird, was das Gate abschneidet. Die Parameter heissen hier `@R1From` (2025-01-01), `@R1To` (2026-06-01),
`@R2From` (2026-05-28), `@ToDate` (heute):

```sql
WITH s AS (
  SELECT l.CodigoEmpresa, l.CodigoArticulo, l.TipoArticulo,
         COALESCE(c.FechaAlbaran, c.FechaFactura) AS SaleDate,
         CASE WHEN c.TipoNuevaFra = 2 OR c.SerieFactura = 'REC' OR c.StatusAbono <> 0
              THEN -ABS(l.ImporteNeto) ELSE l.ImporteNeto END AS NetValue
  FROM dbo.CabeceraAlbaranCliente c
  JOIN dbo.LineasAlbaranCliente l
    ON l.CodigoEmpresa = c.CodigoEmpresa AND l.EjercicioAlbaran = c.EjercicioAlbaran
   AND l.SerieAlbaran = c.SerieAlbaran AND l.NumeroAlbaran = c.NumeroAlbaran
  WHERE (c.FechaFactura >= @R1From AND c.FechaFactura < @R1To)
     OR (COALESCE(l.FechaRegistro, c.FechaFactura) >= @R2From AND COALESCE(l.FechaRegistro, c.FechaFactura) < @ToDate)
)
SELECT s.TipoArticulo,
       CASE WHEN NULLIF(LTRIM(RTRIM(s.CodigoArticulo)), '') IS NULL OR s.CodigoArticulo = '-' THEN '0 no article code'
            WHEN v1.CodigoProveedor IS NOT NULL THEN '1 receipt on or before sale'
            WHEN v2.CodigoProveedor IS NOT NULL THEN '2 receipt only after sale'
            WHEN anyr.Found = 1 THEN '3 only non-positive receipt lines'
            ELSE '4 no receipt at all' END AS Result,
       COUNT(*) AS Lines, SUM(s.NetValue) AS NetValue
FROM s
OUTER APPLY (SELECT TOP 1 cp.CodigoProveedor
             FROM dbo.LineasAlbaranProveedor lp
             JOIN dbo.CabeceraAlbaranProveedor cp ON cp.CodigoEmpresa = lp.CodigoEmpresa
              AND cp.EjercicioAlbaran = lp.EjercicioAlbaran AND cp.SerieAlbaran = lp.SerieAlbaran
              AND cp.NumeroAlbaran = lp.NumeroAlbaran
             WHERE lp.CodigoEmpresa = s.CodigoEmpresa AND lp.CodigoArticulo = s.CodigoArticulo AND lp.Unidades > 0
               AND cp.FechaAlbaran < DATEADD(day, 1, CAST(s.SaleDate AS date))
             ORDER BY cp.FechaAlbaran DESC) v1
OUTER APPLY (SELECT TOP 1 cp.CodigoProveedor
             FROM dbo.LineasAlbaranProveedor lp
             JOIN dbo.CabeceraAlbaranProveedor cp ON cp.CodigoEmpresa = lp.CodigoEmpresa
              AND cp.EjercicioAlbaran = lp.EjercicioAlbaran AND cp.SerieAlbaran = lp.SerieAlbaran
              AND cp.NumeroAlbaran = lp.NumeroAlbaran
             WHERE lp.CodigoEmpresa = s.CodigoEmpresa AND lp.CodigoArticulo = s.CodigoArticulo AND lp.Unidades > 0
             ORDER BY cp.FechaAlbaran DESC) v2
OUTER APPLY (SELECT TOP 1 1 AS Found FROM dbo.LineasAlbaranProveedor lp
             WHERE lp.CodigoEmpresa = s.CodigoEmpresa AND lp.CodigoArticulo = s.CodigoArticulo) anyr
GROUP BY s.TipoArticulo,
       CASE WHEN NULLIF(LTRIM(RTRIM(s.CodigoArticulo)), '') IS NULL OR s.CodigoArticulo = '-' THEN '0 no article code'
            WHEN v1.CodigoProveedor IS NOT NULL THEN '1 receipt on or before sale'
            WHEN v2.CodigoProveedor IS NOT NULL THEN '2 receipt only after sale'
            WHEN anyr.Found = 1 THEN '3 only non-positive receipt lines'
            ELSE '4 no receipt at all' END
ORDER BY 1, 2;
```

Weitere Teile, gleicher Aufbau (CTE `s`, V1 mit Gate `TipoArticulo = 'M'`):

- **E** Top 25 Lieferanten nach Zeilen: `CodigoProveedor`, `RazonSocial`, `Nombre`, `SiglaNacion`, `Nacion`, Zeilen,
  `SUM(NetValue)`.
- **F** Alle Lieferanten im Wareneingang mit `RazonSocial LIKE '%TRAFAG%' OR Nombre LIKE '%TRAFAG%'` (Collation
  `Latin1_General_CI_AI`, `docs/STANDORT_ES_SAGE.md` Z. 20), dazu `CodigoProveedor`, `SiglaNacion`, Anzahl Eingaenge,
  letzter Eingang. Ergebnis entscheidet E5.
- **G** Verkaufte Codes mit `TipoArticulo <> 'M'` oder `-`, die im Wareneingang vorkommen: Code, Verkaufszeilen,
  Einkaufszeilen, haeufigster Lieferant. Zeigt, was ohne Gate einen Lieferanten bekaeme.
- **H** Artikel mit mehr als einem Lieferanten seit `@FromDate`: Anzahl Artikel, 20 Beispiele mit Lieferantenliste.
  Belegt die Einschraenkung „bei mehreren Lieferanten zaehlt der zuletzt gelieferte“.
- **I** Zeilenzahl der Verkaufsmenge mit und ohne beide `OUTER APPLY` (muss gleich sein).
- **J** Laufzeit der kompletten Exportabfrage fuer R1 und R2, nur `COUNT(*)` ueber das Ergebnis.
- **K** Luecken ausserhalb des Bestands (nur Zaehlen, Zeilen und Summe Nettowert): (1) registriert vor 2026-05-28 und
  fakturiert ab 2026-06-01; (2) registriert ab 2025-01-01 und vor 2026-05-28, nie fakturiert (`FechaFactura IS NULL`).
  Belegt den Nebenbefund aus 3.3 und die Gruende gegen den Vereinigungsfilter.
- **L** Kopfzeile der Exportdatei: Pruefen, dass `SourceLineId` (`l.LineasPosicion`) je Verkaufsposition eindeutig und
  nie leer ist (`COUNT(*)` gegen `COUNT(DISTINCT l.LineasPosicion)` ueber R1 vereinigt mit R2).

## 6. Uebergabepaket an Santi

Dateien (alle aus `SageSpainExportPackage/SageSpainFinalExportPackage/`, als neues `SageSpainFinalExportPackage.zip`):

1. `Run-SpainRangeExportAndUpload-AllInOne.ps1` (geaendert)
2. `Export-SageSpainSalesCsv.ps1` (geaendert)
3. `Run-SpainExportAndUpload.ps1` (nur `--verbose` entfernt)
4. `Start-SpainRangeExportAndUpload.ps1` (unveraendert, der Vollstaendigkeit halber)
5. `Analyze-SpainSupplierLookup.ps1` (neu, read-only)
6. `README.txt` (geaendert)

Empfohlen sind **zwei Gaenge** (E6): erst nur das Pruefskript, nach unserer Auswertung die Skripte und der Backfill.
Grund: Sobald Santis Tagesauftrag die neuen Skripte faehrt, gewinnen dessen Dateien (spaeterer Start, nicht leerer
Lieferant) gegen jeden spaeteren Korrektur-Backfill. Ein falscher Schluessel, ein falsches Gate oder eine spaetere
Aenderung von E1/E3 liesse sich dann nur korrigieren, indem die betroffenen Tagesdateien aus dem SharePoint-Ordner
entfernt und ein neuer Backfill eingespielt wird.

Anleitung Gang 1, Entwurf (einfaches Englisch, fuer eine Uebersetzung ins Spanische geeignet; Versand macht Ingo):

```text
Hello Santi,

we want to add the supplier to the Spanish sales export: for every sales line, the supplier of the
most recent goods receipt (albaran de proveedor) of the same article. Before we change the export,
please run one check script. It only reads data and does not change Sage or SQL Server.

1. Copy Analyze-SpainSupplierLookup.ps1 into the folder with the Spain scripts.
2. Run:
   powershell -ExecutionPolicy Bypass -File .\Analyze-SpainSupplierLookup.ps1 -OutputDirectory C:\Trafag\SageSpain\check
3. Send us the screen output and the CSV files from C:\Trafag\SageSpain\check.

Please do not replace the other scripts yet. Thank you!
```

Anleitung Gang 2, Entwurf:

```text
Hello Santi,

thank you for the check. Please now install the new export:

1. Unpack the attached zip into the folder where the current Spain scripts are.
   Replace the existing files. Keep your scheduled task as it is.
2. Run the export twice; each run also uploads its file to SharePoint:
   powershell -ExecutionPolicy Bypass -File .\Run-SpainRangeExportAndUpload-AllInOne.ps1 -DateFilter InvoiceDate -FromDate "2025-01-01" -ToDate "2026-06-01"
   powershell -ExecutionPolicy Bypass -File .\Run-SpainRangeExportAndUpload-AllInOne.ps1 -DateFilter LineRegistrationDate -FromDate "2026-05-28" -ToDate "<TODAY yyyy-MM-dd>"
3. Send us the two files ending with _summary.txt.

The daily job continues as before; from now on its files also contain the supplier.
If a step shows an error, please stop and send us the message.

Thank you!
```

Upload wie bisher per rclone nach `trafag-bi:Import/Finance/Spanien`.

## 7. Ablauf Phase 2, Schritt fuer Schritt

1. **Produktiv nur lesen (Browser, Arbeits-Chrome, `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/...`):**
   Spaltenzuordnung des Standorts TRES/TRSE (leer erwartet), `SupplierFallbackMode`, `InternalSupplierCostSourceMode`,
   `GroupMarginCostCurrencyMode` in den Export-Einstellungen. Braucht die Seite die Freischaltung, das melden und nicht
   umgehen. Ergebnis in den Plan schreiben. Ist die Liste nicht leer, gilt 4.1 nicht nur vorsorglich, sondern zwingend.
   Dazu, nur mit Ingos OK (SharePoint-Geschaeftsdaten): Dateiliste von `Import/Finance/Spanien` und die **Kopfzeile jeder
   Dateigeneration**, vor allem der Basisdatei 2025. Alle lokal vorliegenden Generationen fuehren `SourceLineId`
   (gelesen: `SageSpainExportPackage/Spain_Sales_range_20260528_to_20260603.csv`,
   `.../Spain_Sales_range_20260101_to_20260531.csv`, `.../export.csv`; die Spalte steht seit dem ersten Commit
   `8477894` in der Query). Fehlt sie in einer produktiven Datei, wuerden deren Zeilen ueber Rechnung/Position/Material
   geschluesselt, die Backfill-Zeilen ueber `source:...`; beide blieben stehen und der Umsatz waere doppelt. Dann
   anhalten und Ingo melden (Ausweg: diese eine Datei nach R1 in einen Unterordner, weil R1 ihre Obermenge ist).
2. Koordination und LEARNINGS (Hauptsitzung).
3. Skripte nach Abschnitt 3 aendern: Paketdatei `Export-SageSpainSalesCsv.ps1`, danach nach `scripts/` kopieren und md5
   vergleichen; All-in-One; `Run-SpainExportAndUpload.ps1`; README; neues Analyze-Skript. Danach pruefen: kein Byte
   ausserhalb ASCII (`grep -nP '[^\x00-\x7F]'`), Zeilenenden unveraendert LF (`file`).
4. SQL-Syntax pruefen wie am 2026-08-17: alle erzeugten Varianten (2 Skripte x 2 Datumsfilter, dazu die Analyze-Teile)
   mit `Microsoft.SqlServer.TransactSql.ScriptDom` parsen, Gegenprobe mit kaputtem SQL. Werkzeug im Scratchpad, nicht
   im Repo.
5. C# nach Abschnitt 4 aendern, Tests nach Abschnitt 9 schreiben.
6. Ganze Suite gruen im Release-Worktree (`git checkout --detach <commit>`, dort `dotnet test`).
7. Zip neu bauen (6 Dateien) und Inhalt mit `unzip -l` pruefen.
8. Commit nur eigener Dateien (Liste Abschnitt 10), nie `-A`; fremde unkommittierte Dateien (Finance_All,
   `Program.cs`, `TimerBackgroundService.cs`, `.csproj`) bleiben draussen.
9. Deploy erst nach Freigabe Ingo, mit dem Werkzeug nach `CLAUDE.md` Abschnitt 5 (Einkauf-Delta pruefen, Monitor,
   Commit-Zeile im Protokoll). Ingo vorher sagen, welche weiteren nicht deployten Commits des Zweigs mitgehen (E7).
   Die Reihenfolge Deploy/Backfill ist unkritisch, weil jeder Import alle Dateien neu liest; erst nach Deploy und
   Backfill ist das Ergebnis vollstaendig.
10. Gang 1 an Santi: nur `Analyze-SpainSupplierLookup.ps1` (Ingo versendet; Mailentwurf als `.eml`, nie selbst
    senden). Das Pruefskript kann schon vor Schritt 5 bis 9 hinaus, es haengt nicht am Deploy.
11. Auswertung von Santis Analyze-Ausgabe: Schluessel eindeutig (Teil C `DuplicateHeaderKeys = 0`,
    `LinesWithoutHeader`), `SourceLineId` eindeutig (Teil L), Gate (Teil G), Trafag-Schreibweise (Teil F), Laufzeit
    (Teil J), Luecken (Teil K, fuer E9). Weicht etwas ab: anhalten, Ingo melden, nicht variieren. Danach Gang 2
    (Zip, R1, R2).
12. Nach dem naechsten Spanien-Import: Messung gegen das neue `Sales_All` (Andreas' Datei) mit einem read-only Werkzeug
    unter `.tmp_tools/`: TRES-Zeilen gesamt (darf gegen vorher nicht steigen), Summe `SalesPriceValue` 2025 und 2026,
    Fuellgrad Lieferant als `TRIM(Feld) <> ''` (nie `> 0`), getrennt nach Jahr; ohne Lieferant nach Grund aus Santis
    Summary (`ArticleType`). Ergebnis als Antwortentwurf an Andreas.
13. Doku nachfuehren (Hauptsitzung): `docs/STANDORT_ES_SAGE.md` (Abschnitt 2, 3, 5 inkl. ueberholter Aussage
    Z. 144-146 und „neuere Delta-Zeile gewinnt“), `docs/FINANCE_SUPPLIER.md` (Z. 87, 157, 383-388), `docs/rag/MANUAL_IMPORT.md`
    (Reihenfolge, Erbregel), Issue-Log ISS-003.3, Wochen_Todo, lastchange, LEARNINGS.

## 8. Offene Entscheide fuer Ingo

| Nr | Frage | Empfehlung |
| --- | --- | --- |
| E1 | Zeitregel: V1 letzter Wareneingang am oder vor dem Lieferdatum, V2 letzter ueberhaupt, V3 V1 sonst erster danach | **V1**: reproduzierbar (ein Export von 2025 liefert spaeter denselben Lieferanten; bei V2 haengt der Lieferant vom Exporttag ab) und naeher an „diese Ware kam von“. Teil D zeigt, wie viele Zeilen nur unter V2/V3 einen Lieferanten haetten; Andreas sagte „letzter Wareneingang“ ohne Zeitbezug, deshalb ihm das Ergebnis mitteilen |
| E2 | Ueberschreibschutz: (b) Erbregel im Dedupe (Code, Deploy), (a) alte Dateien in Unterordner, (c) Reihenfolge | **(b)** |
| E3 | Gate nur `TipoArticulo = 'M'` oder alle Codes ausser `-` | **nur `M`** (99.0 % des Nettowerts im Auszug); nach Teil G erweiterbar |
| E4 | Ruecksendungen ausschliessen (`lp.Unidades > 0`) | **ja** |
| E5 | Trafag AG in Sage anders geschrieben als `TRAFAG AG`: Muster `TrAgEntityPattern` erweitern (Code) oder als „Intern ohne Gesellschaft“ lassen | erst nach Teil F; bei Abweichung erweitern, mit Test |
| E6 | Santi in einem Gang (Analyze, Skripte, Backfill) oder erst Analyze, dann Rest | **zwei Gaenge** (Abschnitt 6): ein Fehler in neuen Tagesdateien laesst sich nicht per neuem Backfill korrigieren, nur durch Entfernen der Tagesdateien. Kostet einen Tag mehr gegenueber der Zusage „1-2 Arbeitstage“ an Andreas |
| E9 | Bestehende Luecke (registriert vor 2026-05-28, fakturiert ab 2026-06-01; Teil K) nachholen? | eigener Punkt nach Teil K, nicht im Lieferanten-Backfill; aendert den Umsatz und braucht dann die Zustimmung von Andreas |
| E7 | Deploy-Zeitpunkt; welche nicht deployten Commits des Zweigs mitgehen (u. a. `c5c9b4a`, `19ca282`, Rexx Hub `1e99745`/`d99fbe6` laut Koordinationsdatei „nicht deployed“) *(Ueberholt 2026-11-12 11:33: diese Commits sind mit `e956183` deployed; offen ist nur noch `c9208b5`.)* | Ingo entscheidet |
| E8 | Seed vorsorglich erweitern (4.1) | **ja**, wirkungslos bei leerer Liste |

Fachpunkt fuer Andreas (nur zur Kenntnis): Ware von Trafag AG, deren spanische Artikelnummer nicht im CH-Werkstamm
steht, wird Intern/TR_AG ohne Konzernkostentreffer und rechnet mit dem lokalen IC-Preis, Status „OK“. Das ist der
offene Punkt ISS-018 B3.1 und bleibt so, bis er entscheidet.

## 9. Tests

Bestehende, betroffen:

- `DatabaseInitializationServiceTests.InitializeAsync_Adds_Date_Mappings_For_Spain_Without_Touching_Existing_Rows`
  (Z. 276): erwartet 4 Zeilen, nach 4.1 sieben; drei neue Asserts (`SupplierNumber`/`SupplierName`/`SupplierCountry`,
  `SourceHeader` = Feldname, SortOrder 4/5/6), zweiter Lauf weiter sieben.
- `...Does_Not_Add_Date_Mappings_When_Spain_Has_No_Explicit_Mappings` (Z. 343): unveraendert gruen (Liste bleibt leer).
- `ManualImportFileSelectionTests.Spain_Reads_Base_File_Before_Range_Files` (Z. 116): unveraendert (Reihenfolge bleibt).
- `ManualExcelDataSourceAdapterTests.FetchAsync_Reads_Local_Spain_Folder_And_Deduplicates_DeltaRows` (Z. 112):
  unveraendert gruen.
- `ManualExcelImportServiceTests.ReadSalesRecordsAsync_Reads_Sage_Spain_Csv_Format` (Z. 305): unveraendert (alte Datei
  ohne Lieferant bleibt lesbar).

Neu:

1. `ManualExcelImportServiceTests.ReadSalesRecordsAsync_Reads_Sage_Spain_Supplier_Columns`: CSV mit allen neuen
   Spalten am Ende, generischer Leser; erwartet `SupplierNumber`, `SupplierName`, `SupplierCountry = "CH"`, und dass
   `SupplierCountryName` das Land nicht ueberschreibt.
2. `ManualExcelDataSourceAdapterTests.FetchAsync_Spain_Keeps_Backfill_Supplier_When_Older_Range_File_Has_None`: Backfill
   `Spain_Sales_range_20250101_to_20261111.csv` mit Lieferant, danach gelesene `..._20261105_to_20261111.csv` ohne
   Lieferantenspalten mit anderem Umsatz; erwartet Umsatz aus der zweiten Datei, Lieferant aus dem Backfill.
   Helfer `WriteSpainCsv` um optionale Lieferantenspalten erweitern.
3. `ManualExcelDataSourceAdapterTests.FetchAsync_Spain_Later_Supplier_Wins`: beide Dateien mit verschiedenem
   Lieferanten, die spaetere gewinnt.
4. `ManualExcelDataSourceAdapterTests.FetchAsync_Uk_Does_Not_Inherit_Supplier`: Erbregel wirkt nicht ausserhalb Spaniens.
4a. `ManualExcelDataSourceAdapterTests.FetchAsync_Spain_Mixed_Keys_Are_Not_Merged`: Kennzeichnungstest fuer das
   Risiko aus Schritt 1: eine Datei ohne `SourceLineId`, eine mit; erwartet zwei Zeilen (heutiges Verhalten, nicht
   geaendert). Der Test macht sichtbar, warum die Kopfzeilen vor dem Backfill geprueft werden.
5. `GroupMarginSupplierClassifierTests.Resolve_Spain_Explicit_External_Supplier_Beats_Marc_Fallback`: TSC `TRES`,
   Material im MARC-Set, Lieferant „ACME INSTRUMENTACION SL“ -> `Extern`; `ResolveDeliveringEntity` -> `null`.
6. `GroupMarginSupplierClassifierTests.Resolve_Spain_Trafag_Supplier_Is_Internal_TrAg`: TSC `TRES`, Name in der
   Schreibweise aus Teil F (bis dahin `TRAFAG AG`), Land `CH` -> `Intern`, `TR_AG`.
7. `GroupMarginCalculatorTests.Evaluate_Spain_External_Supplier_Uses_Local_Cost`: Menge 1, `StandardCost` 160.76 EUR,
   Umsatz 265.00, MARC-Treffer mit TR-AG-Kosten vorhanden; erwartet Kostenbasis 160.76, Kostenquelle
   „Kosten aus Verkaufszeile“, Status OK.
8. `GroupMarginCalculatorTests.Evaluate_Spain_TrafagAg_Supplier_Uses_Group_Cost`: gleiche Zeile mit Lieferant
   `TRAFAG AG` und TR-AG-Kosten in `GroupStandardCosts`; erwartet `IsGroupCost`, Quelle
   „Konzernkosten TR AG (MBEW-STPRS)“, Kostenwaehrung CHF.

Zielerwartung: ganze Suite gruen im Release-Worktree. Skripte haben keine automatischen Tests; Ersatz ist Schritt 3
und 4 in Abschnitt 7.

## 10. Dateiliste Phase 2

| Datei | Art |
| --- | --- |
| `SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1` | geaendert |
| `scripts/Export-SageSpainSalesCsv.ps1` | Kopie, byte-identisch |
| `SageSpainExportPackage/SageSpainFinalExportPackage/Run-SpainRangeExportAndUpload-AllInOne.ps1` | geaendert |
| `SageSpainExportPackage/SageSpainFinalExportPackage/Run-SpainExportAndUpload.ps1` | geaendert (nur `--verbose` entfernt) |
| `SageSpainExportPackage/SageSpainFinalExportPackage/README.txt` | geaendert |
| `SageSpainExportPackage/SageSpainFinalExportPackage/Analyze-SpainSupplierLookup.ps1` | neu |
| `SageSpainExportPackage/SageSpainFinalExportPackage/SageSpainFinalExportPackage.zip` | neu gebaut |
| `Services/DataSources/ManualExcelDataSourceAdapter.cs` | geaendert (4.2) |
| `Services/DatabaseSeedService.cs` | geaendert (4.1) |
| `TrafagSalesExporter.Tests/ManualExcelDataSourceAdapterTests.cs` | Tests 2-4 |
| `TrafagSalesExporter.Tests/ManualExcelImportServiceTests.cs` | Test 1 |
| `TrafagSalesExporter.Tests/DatabaseInitializationServiceTests.cs` | angepasst |
| `TrafagSalesExporter.Tests/GroupMarginSupplierClassifierTests.cs` | Tests 5-6 |
| `TrafagSalesExporter.Tests/GroupMarginCalculatorTests.cs` | Tests 7-8 |
| Doku (Hauptsitzung) | siehe Abschnitt 7 Schritt 13 |

Keine UI-Texte, damit keine Eintraege in `UiTextGeneratedTranslations.cs`/`UiTextBerneseTranslations.cs` noetig.
Faellt E5 auf „erweitern“, kommt `Services/GroupMarginSupplierClassifier.cs` (Z. 71-73) dazu.

## 11. Risiken

| Risiko | Wirkung | Gegenmittel |
| --- | --- | --- |
| Join-Schluessel Kopf/Position falsch oder nicht eindeutig | falscher Lieferant, aber keine Umsatzvervielfachung (`TOP 1`) | Teil C, vor der Meldung an Andreas |
| Alte Range-Dateien ueberschreiben den Backfill | Lieferant 2026 leer | Erbregel 4.2 und Test 2 |
| Trafag in Sage anders geschrieben | Intern ohne TR_AG, Konzernkosten fallen weg | Teil F, E5, Test 6 |
| Fracht/Dienst bekommt Spediteur als Lieferant | falsche Extern-Klassifikation kleiner Zeilen | Gate `M`, Teil G |
| Laufzeit des Backfills | Skript haengt ohne Timeout | Teil B/J, Ausweichform 3.7 |
| Santis Server faehrt weiter alte Skripte | neue Zeilen ohne Lieferant | Erbregel schuetzt Altzeilen; neue Zeilen fehlen bis zum Tausch, sichtbar im Fuellgrad |
| MARC-Zeilen wechseln auf lokale Kosten | Marge ES aendert sich sichtbar | ist die gewollte Korrektur; Messung Schritt 12 vor der Meldung |
| Nicht-ASCII oder CRLF in den Skripten | Parserfehler auf Santis Server | Pruefung Schritt 3 |
| Produktive Datei ohne `SourceLineId` | Backfill-Zeilen doppelt neben Altzeilen, Umsatz doppelt | Kopfzeilenpruefung Schritt 1, Test 4a, Zeilenzahl-Kontrolle Schritt 12 |
| Backfill mit breiterem Filter als heute | Umsatzaenderung als Nebenwirkung | R1/R2 genau nach den heutigen Schnitten (3.3), Teil K |

## 12. Kandidaten fuer LEARNINGS

- Spanien-Range-Dateien werden nach Startdatum gelesen, die zuletzt gelesene Zeile gewinnt ganz; ein breiter Backfill
  verliert gegen jede spaeter beginnende Altdatei. Die Doku „neuere Delta-Zeile gewinnt“ war ungenau. Regel: vor jedem
  Backfill die Lesereihenfolge (`SharePointUploadService.SelectManualImportFileNames`) und die Dedupe-Regel lesen.
- `docs/STANDORT_ES_SAGE.md` Abschnitt 5 behauptete noch am 2026-11-12, die ES-Zuordnung muesse in den Einstellungen
  gepflegt werden, obwohl seit dem 2026-08-25 produktiv gemessen keine Liste existiert (Fall „Doku nicht vollstaendig
  nachgefuehrt“, `CLAUDE.md` 3 Punkt 2).
- Der Schemaauszug `candidate_objects.csv` hat nur Spaltennamen; Typaussagen daraus sind nicht moeglich.

## 13. Entscheide Ingo (2026-11-12, im Chat)

| Nr. | Entscheid |
| --- | --- |
| E1 | **V1**: letzter Wareneingang am oder vor dem Lieferdatum |
| E2 | **(b)** Erbregel im Spanien-Dedupe (Code, Tests, Deploy) |
| E3 | **nur `TipoArticulo = 'M'`** |
| E4 | **ja**, Ruecksendungen ausschliessen |
| E5 | nach Teil F der Pruefabfrage |
| E6 | **zwei Gaenge** mit Santi (zuerst nur Analyze) |
| E7 | **offen**: Deploy-Zeitpunkt und mitgehende Commits; vor dem Deploy Liste an Ingo, Deploy erst nach ausdruecklicher Freigabe |
| E8 | **ja**, Seed vorsorglich erweitern |
| E9 | nach Teil K, getrennt, nur mit Zustimmung Andreas |
| Schritt 1 | **lesend erlaubt**: Produktiv-Einstellungen (Spaltenzuordnung TRES, Supplier-/Kostenmodi) und SharePoint `Import/Finance/Spanien` Dateiliste mit Kopfzeilen |

Phase 2 laeuft genau nach Abschnitt 7, Schritte 1 bis 8 und 10 (Mailentwurf `.eml` fuer Santi, Gang 1); Schritt 9 (Deploy) erst nach E7.

**Reihenfolgeaenderung (Freigabe Ingo, 2026-11-12, Weg b):** Schritt 1 war im ersten Anlauf nicht moeglich. Die
Dashboard-Seiten `/standorte` und `/settings` verlangen die Finance-Freischaltung, SharePoint verlangte eine Anmeldung
mit Windows Hello; beides wurde abgebrochen, nicht umgangen. Die Schritte 3 bis 8 und 10 laufen deshalb vorher.
Schritt 1 (Mappingliste TRES, Supplier-/Kostenmodi, Dateiliste und Kopfzeilen in `Import/Finance/Spanien`) wird nachgeholt,
wenn Ingo angemeldet ist, und zwar **vor Gang 2 an Santi**. Gang 1 (nur Pruefskript) haengt nicht daran.
