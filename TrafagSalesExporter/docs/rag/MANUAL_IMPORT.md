# RAG Manual Import

Stand: 2026-11-12 (Doku-Pruefung: ES-Buchungsdatum als erledigt, `ISS-020`, Skripthoheit-Tabelle Spanien, DE-Kundenfelder nachgezogen, `candidate_objects.csv`); davor 2026-11-12 (Spanien: Lesereihenfolge nach Startdatum, Erbregel Lieferant, Seed `EnsureSpainOptionalMappings`, Commit `c9208b5`, nicht deployed); davor 2026-09-09

## Kurzstand

- Manual-Importe ersetzen pro Standort den aktuellen Stand in `CentralSalesRecords`.
- Delta-Dateien muessen zusammen mit der passenden Basisdatei gelesen werden.
- UK liest Jahresdatei plus spaetere Deltas.
- BUGFIX 2026-07-13 (UK-Selbstfuetterung): Der Standortexport laedt eigene Ausgaben (`Sales_ProcessedMergeInput_<TSC>_*.csv` und `Sales_<TSC>_<yyyy-MM-dd>.xlsx`) in denselben SharePoint-Landesordner hoch, aus dem der Manual-Import liest. Seit ca. 30.06. (Audit-CSV produktiv) waehlte der UK-Import dadurch taeglich seine EIGENE Audit-CSV vom Vortag als "neueste TRUK-Datei" und ersetzte den UK-Bestand mit deren 2 Zeilen (Beweis: AppEventLog `Neueste SharePoint-Datei ausgewaehlt | UK_B1/Sales_ProcessedMergeInput_TRUK_*.csv`). Fix: `SharePointUploadService.IsOwnExportOutputFile` schliesst eigene Ausgaben aus der Kandidatenauswahl aus (SharePoint- und Lokalordner-Pfad).
- NEU 2026-07-13 (UK Basis+Delta im Tageslauf): Ohne explizites Importjahr las der Ordner-Import bisher NUR die neueste Datei — beim taeglichen Timer-Export wurde der UK-Bestand also durch das juengste Delta (`ddMMyy_TRUK.xlsx`, oft nur wenige Zeilen) ersetzt. Jetzt gilt auch ohne Jahresangabe das Basis+Delta-Modell: neueste Jahres-/Basisdatei plus alle neueren datierten Deltas werden zusammen gelesen und generisch dedupliziert (`SourceLineId`, sonst Invoice/Position/Material; spaetere Datei gewinnt). Gibt es keine Basisdatei, werden alle datierten Deltas gemeinsam gelesen.
- ES/Spanien liest im Ordner alle `Spain_Sales*.csv`, also Basisdatei plus taegliche `Spain_Sales_range_YYYYMMDD_to_YYYYMMDD.csv`.
- ES BUCHUNGSDATUM, STAND 2026-09-02 — ~~ERLEDIGT BIS AUF SANTIS DATEITAUSCH~~ ERLEDIGT (`ISS-004.2`; berichtigt
  2026-11-12: am 2026-09-02 ist entschieden worden, dass die 7-Tage-Version bleibt und kein Dateitausch bei Santi
  noetig ist, siehe den durchgestrichenen Unterpunkt unten). Der frueher hier
  gefuehrte Befund „`PostingDate` ist auf ALLEN 5'504 TRES-Zeilen leer" (2026-08-03) ist
  UEBERHOLT und wurde entfernt, weil er die naechste Sitzung falsch gestartet hat. Aktueller
  Stand, selbst gemessen gegen `Sales_All_2026-09-01.xlsx`: `PostingDate` `1'523/7'071`
  (21,5 %), `InvoiceDate` `6'838/7'071` (96,7 %).
  - Quelle: `FacturasTB.FechaAsiento` als `PostingDate`, `FacturasTB.Asiento` als
    `PostingDocument`, per `OUTER APPLY` mit `TOP 1`. Schluessel
    `CodigoEmpresa`/`Ejercicio`/`Serie`/`Factura`, am 2026-08-17 live auf dem spanischen
    Server bestaetigt (53 von 53 Treffern) — also KEINE Annahme mehr.
  - **Fachentscheid Andreas vom 2026-08-26: mit Buchungsdatum ist das RECHNUNGSDATUM
    gemeint.** Umgesetzt als Regel `UseInvoiceDate` mit `ScopeKey = ES`, produktiv seit
    2026-08-26 15:26 (Commit `91830c2`), wirkungsneutral gemessen. Die Rohspalte
    `posting date` im `Sales_All` bleibt bewusst das Buchungsdatum aus der Quelle.
  - Die Spaltenzuordnung ist inzwischen DOCH im Seed verdrahtet: `DatabaseSeedService`
    `EnsureSpainDateMappings` (seit `c9208b5`: `EnsureSpainOptionalMappings`, dazu die drei Lieferantenfelder) ergaenzt `PostingDate` und `LineRegistrationDate`, aber nur
    wenn Spanien bereits eine eigene Mappingliste pflegt — eine leere Liste bleibt beim
    generischen Kopfzeilen-Fallback.
  - ~~OFFEN ist nur noch, dass Santi Gomez die 7-Tage- gegen die 35-Tage-Version des
    Exportskripts tauscht.~~ Ueberholt: am 2026-09-02 entschieden, die 7-Tage-Version bleibt,
    Finance ES nutzt `InvoiceDate`. **Neu 2026-09-29:** Seit Mitte Juli kommen keine neuen
    Spanien-Zeilen in der Produktion an, der Bestand schrumpft (`ISS-020`). Ursache: die
    SharePoint-Ordnerliste las nur die erste Graph-Seite (200 Eintraege). Korrektur `60cc569`
    seit 2026-09-29 09:25 produktiv; der Nachimport ab 16.07. zeigt sich beim naechsten Lauf. Die absolute Zahl `1'523` steht seit dem 2026-08-26 unveraendert,
    es kommen also derzeit keine neuen Buchungsdaten nach. *(Ueberholt, hier nachgefuehrt am 2026-11-12: `ISS-020` ist
    seit dem 2026-09-29 erledigt, Spanien liefert wieder; `Sales_All_2026-09-29.xlsx` enthaelt 8'109 TRES-Zeilen mit
    Rechnungsdatum bis 28.09.2026, Issue-Log `ISS-020`.)* Details:
    `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` Abschnitte 8 bis 12.
- Spanien-Deltas werden vor dem Speichern dedupliziert: zuerst `SourceLineId`, sonst Invoice/Position/Material.
- DE/Alphaplan liest `invoice_headers.csv` + `invoice_lines.csv`; Vollbestand im Ordner plus 7-Tage-Delta im Unterordner `delta` werden zusammen gelesen. Seit 2026-07-03 werden zusaetzlich `Alphaplan*.zip` im SharePoint-Ordner automatisch entpackt und wie CSV-Paare ausgewertet.
- DE-Dedupe: primaer `BelegePositionenID` als `SourceLineId`, Fallback Invoice/Position/Material; Delta gewinnt gegen Vollbestand.
- DE-Material: `ArtikelNummer` bleibt lokale Alphaplan-Artikelnummer und ist nicht automatisch eine TR-AG-/SAP-`MATNR`.
- Wenn Audit-CSV aktiv ist, schreibt der Standortexport nach Mapping/Transformation zusaetzlich `Sales_ProcessedMergeInput_<TSC>_<Datum>.csv` in den Standort-Exportordner.
- Zentrale Auswertungen koennen per Setting aus den neuesten Audit-CSV je TSC statt direkt aus `CentralSalesRecords` lesen.

## Skripthoheit: die Export-SQL fuer DE und ES ist UNSERE

Befund 2026-08-03. Wer ein fehlendes Feld bei DE oder ES sieht, muss ZUERST hier schauen,
nicht den Standort anschreiben — sonst geht die Bitte an die falsche Stelle.

| Standort | Skript (in diesem Repo) | liest | Konsequenz |
| --- | --- | --- | --- |
| DE | `AlphaplanExportPackage/scripte/alphaplanExport.ps1` Z. 143-202, identisch in `alphaplandeltaexport.ps1` | nur `dbo.Belege` + `dbo.BelegePositionen` | Supplier, Kundenname/-land, saubere Bezeichnung fehlen, weil die Query sie nicht liest; `RechnungsAdressenID` wird selektiert, aber nie aufgeloest |
| ES | `SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1` ~~Z. 184-188 und Z. 229-237~~, identisch in `Run-SpainRangeExportAndUpload-AllInOne.ps1` ~~Z. 233-237 und Z. 278-286~~ (berichtigt 2026-11-12: die Zeilennummern stimmen nicht mehr, die jeweils zweite Angabe galt bis `c9208b5`, die erste schon davor nicht mehr; Suchanker in allen drei Skripten ist der Block `FROM dbo.CabeceraAlbaranCliente` mit den beiden folgenden `OUTER APPLY`), gespiegelt in `scripts/Export-SageSpainSalesCsv.ps1` | `dbo.CabeceraAlbaranCliente` + `dbo.LineasAlbaranCliente`, seit 2026-08-17 zusaetzlich `dbo.FacturasTB` per `OUTER APPLY`, seit `c9208b5` (2026-11-12, nicht deployed, noch nicht bei Santi) zusaetzlich `dbo.LineasAlbaranProveedor` mit `dbo.CabeceraAlbaranProveedor` per `OUTER APPLY` | Buchungsdatum ist seit 2026-08-17 als `PostingDate` selektiert und kommt an: `1'523/7'071` (21,5 %, gemessen 2026-09-02). Voll gefuellt ist nur der einmalige Nachtrag Januar bis Mai 2026; ~~der laufende Tageslauf holt erst nach, wenn Santi die 35-Tage-Version einspielt~~ (ueberholt: am 2026-09-02 ist entschieden worden, dass die 7-Tage-Version bleibt; `PostingDate` ist nur Informationsspalte, Finance ES rechnet mit dem Rechnungsdatum, `ISS-004.2`) |

Die Skripte laufen auf den Standortservern (DE `localhost\SQL2012`/`ApDaten`), die Query
darin stammt aber von uns. Zwei Regeln daraus:

- **Bei ES immer BEIDE Stellen aendern** (Voll- und Range-Export), sonst laufen sie auseinander.
- **Keine Tabellen-/Spaltennamen raten.** Fuer `ApDaten` existiert keine Schemaliste, der
  Sage-Auszug `obj/candidate_objects.csv` ist bei 80 Objekten abgeschnitten und enthaelt
  `CabeceraFacturaCliente` nicht. Erst Schema live klaeren, dann Query erweitern.
- Die ES-Erweiterung um `PostingDate` vom 2026-08-17 ist KEINE Ausnahme von dieser Regel: der
  Schluessel steht als offen gekennzeichnete Annahme im Skript, damit er in Spanien gemessen
  werden kann. Erst die Messung dort macht ihn zur Tatsache. Vor allem die Zeilenzahl gegen den
  Vorlauf pruefen — steigt sie, trifft die Zuordnung mehrfach und der Umsatz waere zu hoch.

## Laender

| Standort | Quelle | Delta | Finance-Wert |
| --- | --- | --- | --- |
| UK / `TRUK` | SharePoint `Import/Finance/UK_B1`, Sage Excel | ja | `[Sales Price/Value] * [Quantity]`, Credit Notes negativ, GBP |
| ES / `TRSE`/`TRES` | Sage CSV `Spain_Sales*.csv` | ja, wenn Ordner mit Basis + Deltas | `SalesPriceValue`/`ImporteNeto`, REC/Credit negativ, EUR |
| DE / `TRDE` | Alphaplan CSV-Paar oder `Alphaplan*.zip` mit `invoice_headers.csv` + `invoice_lines.csv` | ja, Full + `delta`-Unterordner/Delta-ZIP | `NettoPreisGesamt`, CreditNote/GS negativ, EUR |

## Zwei Betriebsfallen, die Daten vernichten koennen

**1. UK-Reimport nur OHNE Jahresfilter starten.** Der Ordner `UK_B1` enthaelt Dateien
beider Jahre. `ReplaceForSiteAsync` loescht vorher **alle** Zeilen des Standorts, deshalb
vernichtet ein Import mit Jahresvorgabe das jeweils andere Jahr:

| `PreferredImportYear` | gelesene Dateien |
| --- | --- |
| **keins** (richtig) | `TRUK_2025.xlsx` **und** alle 2026er Dateien |
| 2025 | nur `TRUK_2025.xlsx` — die 2026er Zeilen gehen verloren |
| 2026 | alle 2026er **ohne** `TRUK_2025.xlsx` — 2025 geht verloren |

Kontrolle nach dem Reimport: die UK-Zeilenzahl muss ueber der Summe beider Jahre liegen,
nicht nur bei einem davon.

**2. Legendenzeile im Template.** Die erste Datenzeile in `TRUK_2025.xlsx` ist eine
Beschreibungszeile (`Tsc = "Subsidiary abbreviation / company identifier"`). Sie kam bei
jedem Reimport zurueck, weil der Adapter keinen Filter hatte.
`RemoveTemplateDescriptionRowsAsync` verwirft solche Zeilen jetzt vor der Deduplizierung
und protokolliert „Legendenzeile verworfen". Erkannt wird am TSC-Feld: echte Werte sind
kurze Codes ohne Leerzeichen, die Legende traegt dort einen ganzen Satz.

**Bewusst nicht gegen `site.TSC` verglichen:** Der Spanien-Standort heisst in `Sites`
`TRSE`, liefert in den Daten aber `TRES`. Ein Gleichheitstest wuerde dort saemtliche Zeilen
verwerfen.

## `Standard cost` und `Sales Price/Value` sind STUECKpreise

Die Margenlogik rechnet `Menge x StandardCost`. Waere die Spalte eine Zeilensumme, laege
die Marge um den Faktor Menge daneben. An 1'650 UK-Zeilen gegengeprueft: als Stueckpreis
gelesen sind 1'643 Zeilen plausibel (Margen 12–55 %), als Zeilensumme nur 1'253
(Margen 86–99 %).

`Sales Price/Value` ist ebenfalls ein Stueckpreis; das UK-Mapping fuehrt ihn durch
`=SageNetSales([Sales Price/Value], [Quantity], ...)`, und die Funktion rechnet
`amount * quantity`. Der Zeilenumsatz ist also `Menge x Sales Price/Value`.

## Bedienreihenfolge

1. Datei oder Delta im richtigen Ordner bereitstellen.
2. In `Manuelle Importe` Pfad/Standort pruefen.
3. Standortexport ausfuehren.
4. Optional Audit-CSV im Standort-Exportordner pruefen.
5. Zentrale Auswertungsquelle bewusst setzen: DB oder Audit-CSV.
6. Zentrale Datei neu erzeugen.
7. `Finance Summary` und `Finance Details` pruefen.

## Spanien Delta-Sync

- SharePoint-Ordner: `Import/Finance/Spanien`.
- Dateimuster:
  - Basis/Vollfile: z. B. `Spain_Sales_2025.csv`.
  - Delta/Range: `Spain_Sales_range_20260528_to_20260603.csv`.
- Die App liest bei Spanien-Ordnern alle `Spain_Sales*.csv`, nicht nur die neueste Datei.
- Reihenfolge: Basisdateien zuerst, danach Range-Dateien nach dem **Startdatum** im Dateinamen; die zuletzt gelesene Zeile
  ersetzt die fruehere ganz. Seit `c9208b5` (nicht deployed) Erbregel: ein komplett leerer Lieferant wird aus der frueheren
  Zeile derselben Position uebernommen (ISS-003.3).
- Deduplizierung:
  - primaer `SourceLineId`.
  - Fallback `TSC + InvoiceNumber + PositionOnInvoice + Material`.
- Danach ersetzt die App den Spanien-Stand in `CentralSalesRecords` mit diesem deduplizierten Gesamtstand.

## Deutschland Alphaplan Full/Delta

- Erwartetes Paar je Ordner: `invoice_headers.csv` und `invoice_lines.csv`.
- Vollbestand liegt im Standortordner; 7-Tage-Rueckblick liegt im Unterordner `delta`.
- Lokal und in SharePoint werden passende Paare rekursiv gesucht; in SharePoint werden zusaetzlich `Alphaplan*.zip` erkannt, temporaer entpackt und deren CSV-Paare eingelesen.
- Header und Positionen werden ueber `BelegeID` verbunden.
- Dedupe: primaer `SourceLineId = Alphaplan:<BelegePositionenID>`, sonst Invoice/Position/Material; Delta-Zeilen gewinnen.
- `SalesPriceValue = NettoPreisGesamt`; `DocumentTotal... = NettoPreisEndSumme`; `CreditNote`/GS/Gutschriften werden negativ gerechnet.
- `CustomerNumber = RechnungsAdressenID`; Kundenname und Kundenland sind im aktuellen CSV-Paar nicht enthalten.
  *(Nachgefuehrt 2026-11-12, Stand der Daten seit 2026-09-09 14:37: Der Bestand ist einmalig nachgezogen, 7'592 von
  7'622 DE-Zeilen tragen fachliche Kundennummer, Name, Land und Branche, `ISS-016`. Der Nachzug ist eine
  Momentaufnahme: die Query loest `RechnungsAdressenID` weiter nicht auf, neue Rechnungen kommen also wieder nur mit
  dieser ID an, bis der Join in `alphaplanExport.ps1` steht; `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md` Abschnitt 9.)*
- `Material = ArtikelNummer`; diese lokale Alphaplan-Nummer ist nicht garantiert identisch mit TR-AG-/SAP-`MATNR`.
- Die Artikelbezeichnung ist ein RTF-Feld. Schrift- und Farbtabelle werden seit dem 2026-09-09
  beim Import klammerbalanciert entfernt; davor begannen 3'089 von 7'615 DE-Zeilen mit
  `MS Shell Dlg, Microsoft Sans Serif, , ,`. Ursache, Messung und Nachzugswerkzeug:
  `docs/STANDORT_DE_ALPHAPLAN.md` Abschnitt 8. Ein Artikelstammtext aus der Quelle ist dafuer
  nicht noetig.
- URSACHE DER DE-FELDLUECKEN (Befund 2026-08-03): Lieferant und Kundenname/-land fehlen, weil
  **unsere eigene Export-Query** sie nicht liest — nicht weil Deutschland sie nicht liefern
  koennte. `AlphaplanExportPackage/scripte/alphaplanExport.ps1`
  Zeilen 143-202 und `alphaplandeltaexport.ps1` (identische Query) lesen nur `dbo.Belege` und
  `dbo.BelegePositionen`; `RechnungsAdressenID` wird selektiert, aber nie auf einen Namen
  aufgeloest. Blocker ist das fehlende Alphaplan-Schema fuer `ApDaten` (`candidate_objects.csv`
  im Repo-Root ~~ist leer~~ ist eine Kopie des Sage-Auszugs, berichtigt 2026-11-12: 81 Zeilen, bytegleich mit
  `obj/candidate_objects.csv` und `SageSpainExportPackage/v2/candidate_objects.csv`; ein Alphaplan-Auszug existiert
  nicht; `obj/candidate_objects.csv` ist Sage Spanien) — deshalb KEINE
  Tabellen-/Spaltennamen raten, sondern Schema-Auszug anfordern. Details und Mailstand:
  `docs/FINANCE_FELDLUECKEN.md` Abschnitt 6.
- Das alte Alphaplan-Excel-Mapping bleibt technisch vorhanden, ist aber nicht mehr der bevorzugte DE-Pfad. Produktiver DE-Pfad seit 2026-07-03: `Import/Finance/Deutschland/AlphaplanRaw`.

## Rohquellen Nur Bei Bedarf

- Detailstand: `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md`

