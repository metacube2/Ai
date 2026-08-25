# Spanien: fehlendes Buchungsdatum (PostingDate)

Stand: 2026-08-03

Anlass: Andreas hat als wichtigsten offenen Punkt bei Spanien das **fehlende Buchungsdatum**
benannt. Die bisherige Doku und der Mailentwurf an Santi nannten stattdessen nur
„231 Zeilen ohne jedes Datum" — das ist die Teilmenge, nicht das Problem.

Messgrundlage: `Finance_Dashboard_Audit_All_2026-07-29.csv`, TSC `TRES`, 5'504 Zeilen.

## 1. Der gemessene Befund

**`PostingDate` ist bei Spanien auf ALLEN 5'504 Zeilen leer.** Spanien ist damit der einzige
Standort ohne Buchungsdatum.

| TSC | Zeilen | PostingDate leer | InvoiceDate leer | beide leer |
| --- | ---: | ---: | ---: | ---: |
| TRAT | 1'790 | 0 | 0 | 0 |
| TRCH | 47'142 | 0 | 0 | 0 |
| TRDE | 7'171 | 0 | 0 | 0 |
| **TRES** | **5'504** | **5'504** | **231** | **231** |
| TRFR | 2'577 | 0 | 0 | 0 |
| TRIN | 6'990 | 0 | 0 | 0 |
| TRIT | 19'534 | 0 | 0 | 0 |
| TRUK | 2'955 | 6 | 6 | 6 |
| TRUS | 1'504 | 0 | 0 | 0 |

Die frühere Aussage „231 Zeilen ohne jedes Datum" ist richtig, aber sie beschreibt nur den
Sonderfall, in dem **zusätzlich** das Rechnungsdatum fehlt. Der eigentliche Punkt ist, dass
Spanien überhaupt kein Buchungsdatum liefert.

## 2. Warum das fachlich zählt

Die Jahres-/Periodenabgrenzung läuft überall über
`Year(PostingDate ?? InvoiceDate ?? ExtractionDate)`.

Folge für Spanien:

- **Alle 5'504 Zeilen** fallen auf `InvoiceDate` zurück. Rechnungsdatum ist nicht
  Buchungsdatum — eine im Dezember fakturierte, im Januar gebuchte Position landet für
  Spanien im falschen Geschäftsjahr, und zwar unsichtbar, weil kein Feld fehlt, sondern
  ein Fallback greift.
- **231 Zeilen** fallen eine Stufe weiter auf `ExtractionDate`, also auf das Datum des
  Exportlaufs. Diese Zeilen tragen **140'598.19 EUR** und werden dadurch pauschal dem
  Jahr des Exports zugeordnet.

Kein akuter Jahresfehler: alle 231 haben ein gefülltes `OrderDate` im Jahr **2026**, und
der Export lief 2026 — sie zählen aktuell also zufällig im richtigen Jahr. Die
Jahresverteilung, wie das Dashboard TRES heute zählt: 2025 = 4'315 Zeilen,
2026 = 1'189 Zeilen. Das Risiko ist strukturell, nicht aktuell realisiert: über einen
Jahreswechsel hinweg würde derselbe Mechanismus still falsch zuordnen.

**`OrderDate` ist bei allen 231 gefüllt, wird aber von der Fallback-Kette nicht
berücksichtigt.** Das ist eine offene Entscheidung, keine Empfehlung: ob `OrderDate` als
letzte Stufe vor `ExtractionDate` fachlich zulässig ist, muss Finance entscheiden — ein
Auftragsdatum ist kein Buchungsdatum.

## 3. Warum das Feld fehlt — es ist unsere Query

Wie bei DE/Alphaplan liegt es **nicht** daran, dass Spanien etwas nicht liefert.
`SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1`
Zeilen 184-186 selektiert:

```
c.FechaFactura   AS InvoiceDate
c.FechaAlbaran   AS DeliveryDate
l.FechaRegistro  AS LineRegistrationDate
```

Ein Buchungsdatum wird nicht selektiert. Gelesen werden
`dbo.CabeceraAlbaranCliente` (Lieferschein-Kopf) und `dbo.LineasAlbaranCliente` —
nicht die Rechnungs-/Buchhaltungstabellen. Dieselbe Query steht ein zweites Mal in
`Run-SpainRangeExportAndUpload-AllInOne.ps1` Zeilen 233-235; **Änderungen müssen an beiden
Stellen erfolgen**, sonst laufen Voll- und Range-Export auseinander.

## 4. Wo das Buchungsdatum vermutlich liegt — NICHT belegt

Im vorhandenen Sage-Schema-Auszug (`obj/candidate_objects.csv`) gibt es genau zwei
Tabellen mit einer Buchungsdatumsspalte: **`FacturasTB`** und `FacturasSII`.

`FacturasTB` trägt `FechaAsiento` und `Asiento` — inhaltlich das Gesuchte.
**Trotzdem ist das kein belegter Weg**, aus drei Gründen:

1. `FacturasTB` hat zusätzlich `NumeroFacturaInicial_` und `NumeroFacturaFinal_`. Das
   deutet auf eine **Sammelbuchung über einen Nummernbereich** hin, also nicht eine Zeile
   je Rechnung. Ein Join müsste dann als Bereichsjoin gebaut werden — fragil.
2. Der echte Rechnungskopf `CabeceraFacturaCliente` **fehlt im Auszug vollständig**.
3. Der Auszug ist **abgeschnitten**: die Discovery kappt bei 80 Kandidaten je Datenbank,
   und genau 80 Objekte liegen vor. Er ist also keine vollständige Schemaliste.

Gemeinsame Spalten von `FacturasTB` und `CabeceraAlbaranCliente` sind nur `CodigoEmpresa`
und `FechaFactura` — der Schlüssel ist damit nicht aus dem Auszug ableitbar.

**Deshalb keine Tabellen-/Joinnamen erfinden und keine Query bauen, bevor das Schema
live geprüft ist.** Das ist derselbe Fehlertyp, der bei UK-2025 und beim IT-Superlativ
schon zugeschlagen hat: eine Annahme statt einer Messung.

## 5. Was ohne neue Tabelle sofort möglich wäre

`CabeceraAlbaranCliente` — die Tabelle, die wir **schon lesen** — trägt bereits:

| Spalte | Nutzen |
| --- | --- |
| `SerieFactura`, `NumeroFactura`, `EjercicioFactura` | Rechnungsreferenz und Geschäftsjahr der Faktura |
| `StatusContabilizado` | Kennzeichen, ob der Beleg verbucht ist |
| `StatusFacturado` | Kennzeichen, ob fakturiert |

Diese Felder sind rein additiv mitnehmbar, ohne neuen Join. `EjercicioFactura` ist zwar
kein Buchungsdatum, würde aber die Geschäftsjahr-Zuordnung belastbarer machen als der
heutige Fallback über das Rechnungsdatum. Ob das fachlich ausreicht, entscheidet Finance.

## 6. Offene Punkte

- Live-Schemaprüfung der spanischen Sage-Datenbank: wo liegt das Buchungsdatum je
  Rechnung, und über welchen Schlüssel ist es an den Lieferschein/die Position gebunden?
- Fachentscheid Finance: darf `OrderDate` als Fallback-Stufe vor `ExtractionDate`
  treten, oder sollen Zeilen ohne Buchungs-/Rechnungsdatum sichtbar ausgewiesen statt
  still zugeordnet werden?
- Fachentscheid Finance: reicht `EjercicioFactura` als Geschäftsjahr-Anker, solange kein
  Buchungsdatum verfügbar ist?
- Nach Klärung: Query an **beiden** Stellen erweitern (`Export-SageSpainSalesCsv.ps1`
  und `Run-SpainRangeExportAndUpload-AllInOne.ps1`), danach Reimport und Jahresverteilung
  TRES neu messen.
- Mailentwurf an Santi (`docs/mails/Build-StandortMails.ps1` Mail 6) führt bisher die
  2026-Datenlücke als Punkt 1 und das Datum als Punkt 2 mit falschem Schwerpunkt — auf
  Buchungsdatum umstellen, sobald der Weg geklärt ist.

## 7. Reproduzierbar

```powershell
$all = Import-Csv -Path 'Finance_Dashboard_Audit_All_2026-07-29.csv' -Delimiter ';' -Encoding UTF8
foreach ($t in ($all | Select-Object -ExpandProperty TSC -Unique | Sort-Object)) {
  $g = $all | Where-Object TSC -eq $t
  '{0}: {1} Zeilen, PostingDate leer {2}, InvoiceDate leer {3}' -f $t, $g.Count,
    ($g | Where-Object { -not $_.PostingDate }).Count,
    ($g | Where-Object { -not $_.InvoiceDate }).Count
}
```

## 8. Nachtrag 2026-08-17: Feld ist im Exportskript eingebaut

Abschnitt 4 sagt „keine Query bauen, bevor das Schema live geprueft ist". Diese Regel gilt
weiter fuer den produktiven Einsatz, der Code steht ihr aber nicht entgegen: das Feld ist
jetzt eingebaut, damit Ingo es in einer RDP-Sitzung auf dem spanischen Sage-Server
**messen** kann. Genau diese Messung fehlt bis heute.

Geaendert wurden beide Fundstellen der Query, wie in Abschnitt 3 gefordert, dazu die
byte-identische Spiegelung unter `scripts/`:

- `SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1`
- `SageSpainExportPackage/SageSpainFinalExportPackage/Run-SpainRangeExportAndUpload-AllInOne.ps1`
- `scripts/Export-SageSpainSalesCsv.ps1`

Neu im Select sind `f.FechaAsiento AS PostingDate` und `f.Asiento AS PostingDocument`,
geliefert von einem `OUTER APPLY` mit `TOP 1` auf `dbo.FacturasTB` ueber
`CodigoEmpresa`, `Ejercicio`, `Serie`, `Factura`.

**Warum kein gewoehnlicher `JOIN`.** Am Auszug `SageSpainExportPackage/v2/Sage.dbo.FacturasTB.csv`
nachgezaehlt: 3'788 Zeilen verteilen sich auf 3'642 Rechnungsschluessel, 70 Schluessel
kommen mehrfach vor, und bei 6 davon steht in den Doppelzeilen ein unterschiedliches
`FechaAsiento`. Ein `JOIN` haette fuer diese Rechnungen jede Verkaufszeile vervielfacht
und den spanischen Umsatz still erhoeht. `FechaAsiento` ist im Auszug bei allen 3'788
Zeilen gefuellt.

**Was damit belegt ist und was nicht.** Belegt ist nur die Syntax: alle vier erzeugten
SQL-Varianten, also beide Skripte mal `DateFilter InvoiceDate` und
`LineRegistrationDate`, wurden mit `Microsoft.SqlServer.TransactSql.ScriptDom` als
gueltiges T-SQL geparst, und eine Gegenprobe mit absichtlich kaputtem SQL wird vom selben
Parser abgelehnt. **Nicht** belegt sind Trefferquote, Schluesselrichtigkeit und das
Verhalten bei Gutschriften — dafuer braucht es die Sitzung in Spanien. Der Schluessel
bleibt eine begruendete Annahme und ist im Skript und im README des Pakets als solche
gekennzeichnet.

**Beim ersten Lauf in Spanien pruefen:** wie viele Zeilen leeres `PostingDate` haben, wie
weit Buchungs- und Rechnungsdatum auseinanderliegen, wie sich Gutschriften verhalten
(`SerieFactura = 'REC'` beziehungsweise `StatusAbono <> 0`), und vor allem, dass die
**Zeilenzahl gegenueber dem Vorlauf nicht gestiegen** ist. Eine hoehere Zeilenzahl waere
der Beweis, dass die Zuordnung doch mehrfach trifft.

**Danach in der Anwendung:** Spanien haengt als `MANUAL_EXCEL`-Standort an SharePoint und
hat, anders als UK und Deutschland, keine fest verdrahtete Spaltenzuordnung im Seed. Die
neue Spalte `PostingDate` muss deshalb in den Einstellungen beim Standort Spanien
zugeordnet werden, danach Reimport und Jahresverteilung TRES neu messen.

## 9. Nachtrag 2026-08-17: Live-Pruefung bestaetigt den Schluessel, Ursache war Buchungsverzug

Live mit Ingo per RDP auf dem spanischen Sage-Server geprueft, mit einem neuen read-only
Diagnosewerkzeug: `SageSpainExportPackage/SageSpainFinalExportPackage/Analyze-SpainPostingDateKey.ps1`.

**Erster Testlauf, Zeitfenster 10.-13.08.2026 (Standardfenster damals, letzte 7 Tage):**
`PostingDate` bei allen 58 exportierten Zeilen leer, 0 von 22 Rechnungsschluesseln trafen
in `FacturasTB`.

**Ursache war NICHT der Join-Schluessel, sondern Buchungsverzug.** Eine direkte Suche
gegen `FacturasTB` ueber `CodigoEmpresa` + `FechaFactura` (unabhaengig vom Schluessel aus
Abschnitt 8) fand fuer keine der 10 Beispielrechnungen vom 10.-13.08. irgendeine Zeile —
diese Rechnungen waren zum Testzeitpunkt schlicht noch nicht gebucht. Das spaeteste
tatsaechlich gebuchte `FechaFactura` in `FacturasTB` war `2026-07-30`.

**Zweiter Test auf einem bereits gebuchten Fenster (23.-30.07.2026): 53 von 53
Rechnungsschluesseln treffen (100%).** Der Schluessel `CodigoEmpresa`/`Ejercicio`/
`Serie`/`Factura` aus Abschnitt 8 ist damit als korrekt bestaetigt, sobald der verglichene
Zeitraum tatsaechlich verbucht ist.

**Nebenbefund, der die erste Stichprobe irregefuehrt hatte:** `FacturasTB` fuehrt zwei
Bewegungstypen ueber `TipoIngreso`. Nur `TipoIngreso = 2` (3'540 Zeilen) fuehrt `Serie`
fast durchgehend (3'539 von 3'540 Zeilen gefuellt) — das sind die echten Verkaufsrechnungen.
`TipoIngreso = 1` (3'917 Zeilen) hat `Serie` fast nie gefuellt und `Factura` traegt dort
grosse, fortlaufende interne Nummern statt der Rechnungsnummer. Eine ungefilterte
Stichprobe der neuesten `FacturasTB`-Zeilen nach `FechaAsiento` zeigt bevorzugt diesen
zweiten Typ und sieht dadurch faelschlich nach einem falschen Schluessel aus.

**Bug im Ausfuehrungsskript gefunden und behoben, unabhaengig vom Buchungsdatum-Thema.**
`Resolve-RcloneExecutable` in `Run-SpainRangeExportAndUpload-AllInOne.ps1` nutzte
`Split-Path -Parent $MyInvocation.MyCommand.Path` INNERHALB einer Funktion. Dieser Wert
ist bei einem Funktionsaufruf in PowerShell zuverlaessig `$null`, nur auf Skript-Top-Level
ist er gefuellt. Fehlermeldung beim ersten echten Lauf: "No se puede enlazar el argumento
al parametro 'Path' porque es nulo." Fix: `$PSScriptRoot` statt `$MyInvocation.MyCommand.Path`,
das liefert den Skriptordner zuverlaessig auch innerhalb von Funktionen. Der Bug bestand
bereits in der Vor-Version vom 2026-08-11 (rclone-Fix), wurde aber vorher nie produktiv
ausgefuehrt, nur syntaktisch geprueft — deshalb ist er erst jetzt aufgefallen.

**Zeitfenster von 7 auf 35 Tage erweitert** in `Run-SpainRangeExportAndUpload-AllInOne.ps1`
und im Paket-README. Bei einem Buchungsverzug von rund 2-3 Wochen fiel eine Rechnung mit
dem alten 7-Tage-Fenster aus dem taeglichen Delta-Export heraus, bevor sie ueberhaupt ein
`PostingDate` haben konnte, und blieb dadurch dauerhaft leer. Laut `docs/rag/MANUAL_IMPORT.md`
dedupliziert die App Spanien-Zeilen ueber `SourceLineId`, die neuere Delta-Zeile gewinnt —
ein breiteres, ueberlappendes Fenster erzeugt deshalb keine Duplikate, es aktualisiert nur
still die alten, noch unverbuchten Zeilen sobald sie gebucht wurden.

**Einmaliger Nachtrag fuer die Vergangenheit:** Range-Export Januar bis Mai 2026 lokal
erzeugt (`Export-SageSpainSalesCsv.ps1 -ExportMode Range -FromDate 2026-01-01 -ToDate 2026-06-01`),
`1'571` Zeilen, `1'461'263.57 EUR`, `PostingDate` bei `100%` der Zeilen gefuellt und plausibel
(`PostingDate` = `InvoiceDate` bei den geprueften Beispielen). Per `rclone copy` nach
`Import/Finance/Spanien` hochgeladen.

**Weitergabe an Spanien:** Mailtext an Santi Gomez (`Santi.Gomez@trafag.es`, siehe
`docs/ANSPRECHPARTNER.md`) vorbereitet, NICHT von Claude versendet. Er soll die alte
`-7`-Tage-Version von `Run-SpainRangeExportAndUpload-AllInOne.ps1` auf dem Server durch die
neue `-35`-Tage-Version mit `PostingDate`/`PostingDocument` und dem `$PSScriptRoot`-Fix
ersetzen.

**Weiterhin offen:** Santi muss die Datei serverseitig ersetzen. Danach unveraendert wie in
Abschnitt 8 beschrieben: die `PostingDate`-Spaltenzuordnung im Seed fuer Spanien ist NICHT
verdrahtet, muss in den Einstellungen manuell gesetzt werden, danach Reimport und
Jahresverteilung TRES neu messen.

## 10. Nachtrag 2026-08-20: Fachbestaetigung durch Andreas/Saioa, Entry-Date-Frage offen

Ingo hat eine Mailkette geteilt: Andreas Stoller (Finance) hatte Saioa Ochoa (Trafag
Espana, Finance) zwischen 17. und 20.08.2026 den spanischen Rechnungsprozess erklaeren
lassen, dazu einen neuen manuellen Vollexport 01.01.2025-01.01.2026 mit `PostingDate`
angefordert und von Santi Gomez erhalten.

### 10a. Fachlicher Prozess laut Saioa Ochoa (woertlich zitiert)

Ablauf: Lieferschein -> Rechnungserstellung (Proforma in Sage) -> Validierung. Die
Validierung ist der rechtsverbindliche Schritt gegenueber der spanischen Steuerbehoerde
(digitale Signatur, QR-Code, Meldepflicht seit Ende 2024). Dabei wird automatisch das
Datum gesetzt, das unser Export als `PostingDate` liest (`FacturasTB.FechaAsiento`).
**Einmal validiert, ist die Rechnung gesperrt und nicht rueckdatierbar** — Korrekturen
laufen nur ueber Gutschrift plus Neufakturierung.

Saioa woertlich auf Andreas' Nachfrage, ob die Validierung auch der Buchungszeitpunkt im
Finanzsystem ist: *"Correct! The validated date is the posting date, which is when it
becomes revenue in the P&L."* Damit ist die offene Fachfrage aus Abschnitt 6 (welches
Feld das Umsatzrealisierungsdatum ist) fachlich beantwortet: **`PostingDate`**.

Zum Timing: *"we usually validate invoices the month of delivery. At the end of each
month and before the closing, we review all deliveries to ensure they are invoiced, if
for any reason we cannot invoice a delivery, we will post and validate it the following
month. Only at the end of the year, and if necessary, do we record a provision in order
to recognize the sale in the correct period."* Es gibt also **keine automatische
Monatsabgrenzung**, nur eine optionale Jahresend-Provision.

Beispiel aus der Mail: Lieferung am 28.08., Validierung am 02.09. -> Buchungsdatum 02.09.,
Umsatz zaehlt im September, nicht im August.

Das deckt sich mit Santis frueherer Erklaerung vom 18.08. (Abschnitt 9): leeres
`PostingDate` bei kuerzlich gelieferten, noch nicht validierten Rechnungen ist normal und
transient, kein Datenfehler.

### 10b. Andreas' offene Abstimmungsfrage — bisher UNBEANTWORTET

Andreas an Ingo (2026-08-20, per Mail an Ingo weitergeleitet über die Kette): *"So wie ich
das sehe, müssten die Daten nach dem Entry Date auf Tagesebene gezogen werden und nach
dem Posting Date reported werden (potenziell auch in die Vergangenheit)."*

Ingo hat dazu bislang **keine Antwort von Andreas erhalten**. Der Punkt bleibt offen zur
Klaerung, nicht als entschiedene Fachvorgabe zu behandeln.

Fachlich deckt sich Andreas' Vorschlag mit dem bereits gebauten Mechanismus aus Abschnitt
9: das 35-Tage-Exportfenster plus Dedup ueber `SourceLineId` aktualisiert aeltere, noch
unverbuchte Zeilen automatisch, sobald `PostingDate` bei einem spaeteren Lauf vorliegt —
das ist technisch schon "ruckwirkend in die Vergangenheit reporten".

**Nicht belegt, nur eine Annahme:** was Andreas mit "Entry Date" genau meint. Nach
Vorrangregel 5/Router-Fallenregel darf hier kein Feld einfach unterstellt werden.

### 10c. Messung am neuen Vollexport 2025 (`Spain_Sales_range_20250101_to_20251231.csv`)

Gemessen mit `Import-Csv -Delimiter ';'` (korrektes Quote-Handling, mehrzeilige Felder in
`DescriptionLine`). Datei erzeugt 2026-08-20 11:39 auf dem Sage-Server, Filtermodus
`InvoiceDate`, Zeitraum 01.01.2025 bis 01.01.2026 (exklusiv), **4'341 Zeilen**, Summe
`SalesPriceValue` 3'081'740.18 EUR (identisch mit dem mitgelieferten Summary).

| Kennzahl | Wert |
| --- | --- |
| `PostingDate` gefuellt | 4'264 / 4'341 (98.2 %), 77 leer, alle `DocumentType = Invoice` |
| Gutschriften (`Credit Note`) mit `PostingDate` | 101 / 101 (100 %) |
| Verzug `PostingDate` minus `InvoiceDate` | Ø 3.3 Tage, Median praktisch 0, Maximum 28 Tage |

Damit deutlich besser als der letzte dokumentierte Stand in Abschnitt 9/
`docs/AGENT_COORDINATION.md` (21.1 % Fuellgrad, nur Jan-Mai 2026). Das ist ein Sonderexport
fuer 2025, keine Aussage ueber den taeglichen Server-Delta-Lauf bei Santi.

### 10d. `LineRegistrationDate` als datengestuetzter Entry-Date-Kandidat

Ingos These: "PostingDate ist immer verzoegert geschrieben, Entry Date eventuell sofort
beim Erzeugen ohne Validierung/Approve." Am selben Export gemessen:

| Feld | Fuellgrad | Besonderheit |
| --- | --- | --- |
| `LineRegistrationDate` (`FechaRegistro`) | 100 % (4'341/4'341) | einzige Spalte mit Uhrzeit, z. B. `2025-01-10 09:42:35` |
| `PostingDate` | 98.2 % | reines Datum, erst bei Validierung gesetzt |

`LineRegistrationDate` liegt in **0 von 4'341 Faellen nach** `InvoiceDate` (in 594 Faellen
bis zu 23 Tage davor, Ø 0.64 Tage), und gegenueber `PostingDate` bis zu 30 Tage davor
(Ø 3.65 Tage). Es entsteht offenbar sofort bei Zeilenerzeugung (Proforma-Schritt), bevor
die Rechnung validiert wird — ein plausibler, aber von Andreas **nicht bestaetigter**
Kandidat fuer sein "Entry Date".

### 10e. Vier Beispielzeilen (zur Vorlage an Andreas)

Aus demselben Export, Company Code 1:

| # | Lieferdatum | LineRegistrationDate | InvoiceDate | PostingDate | Typ | Wert EUR | Anmerkung |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Rechnung `20241332` | 2025-01-02 | 2025-01-02 11:15:36 | 2025-01-02 | 2025-01-02 | Invoice | 265.00 | Normalfall, alle Daten praktisch gleich |
| Rechnung `20242026` | 2025-06-06 | 2025-06-06 09:50:43 | 2025-06-30 | 2025-06-30 | Invoice | 46.50 | Validierung erst zum Monatsende |
| Rechnung `20242081` | 2025-07-08 | 2025-07-08 09:03:30 | 2025-07-08 | **leer** | Invoice | 1'080.00 | Zum Exportzeitpunkt noch nicht validiert |
| Rechnung `7024287` (Serie `LAT`) | 2025-07-29 | 2025-07-29 12:23:31 | 2025-08-13 | 2025-08-29 | Invoice | 1'231.30 | Latam-Extra-Freigabeschritt, groesster gemessener Verzug (31 Tage) |

Volle 51-Feld-Zeile fuer Rechnung `20242081` (Beispiel des unvalidierten Falls) auf
Anfrage reproduzierbar aus derselben CSV, `SourceLineId`
`62f1ba25-f1ea-4e9a-b0eb-3569be0c159f`.

### 10f. Offene Punkte

- Andreas' Antwort auf die Entry-Date-Frage steht aus; bis dahin `LineRegistrationDate`
  nicht als bestaetigtes Feld behandeln.
- Ob und wodurch der Fuellgrad-Sprung von 21.1 % auf 98.2 % zustande kam (Santi hat das
  Serverskript ersetzt? Sonderlauf?), ist nicht geklaert.
- Wie in Abschnitt 8/9: die taegliche Server-Export-Version bei Santi und der laufende
  Reimport in die App sind davon unabhaengig zu pruefen, dieser Nachtrag betrifft nur den
  manuell zugesandten Vollexport 2025.

## 11. Nachtrag 2026-08-25: LineRegistrationDate ist als eigene Spalte eingebaut und deployed

Andreas hat auf die Entry-Date-Frage aus Abschnitt 10b bis heute nicht geantwortet. Ingo hat
entschieden, das Feld trotzdem mitzufuehren, ausdruecklich mit der Moeglichkeit, es wieder zu
entfernen. Umgesetzt am 2026-08-25, `586/586` Tests gruen (vorher `580`), `dotnet build -c
Release` ohne Fehler. **Produktiv deployed am 2026-08-25 10:14**, Commit `d414427`, lokaler
Release-Build und Server bitgleich; Deploy-Nachweis in `docs/rag/DEPLOYMENT.md`.

**Benennung.** Die Spalte heisst `Line Registration Date` und nicht `Entry Date`. Dass Andreas
genau dieses Feld meint, ist unbestaetigt; nach Vorrangregel 5 traegt sie deshalb den
Quellfeldnamen und nicht die fachliche Deutung. Wird die Gleichsetzung bestaetigt, ist eine
Umbenennung ein Einzeiler.

**Wo das Feld jetzt durchlaeuft.** Vollstaendige Kette, damit es nicht auf halbem Weg
verschwindet:

| Stelle | Aenderung |
| --- | --- |
| `Models/SalesRecord.cs`, `Models/CentralSalesRecord.cs` | neues Feld `LineRegistrationDate` |
| `Services/DatabaseInitializationService.SchemaSql.cs`, `Services/DatabaseSchemaMaintenanceService.cs` | Spalte `LineRegistrationDate TEXT NULL`, additiv per `AddColumnIfMissing` |
| `Services/CentralSalesRecordService.cs`, `Services/CentralSalesDataProvider.cs` | Insert, Lesen und Rueckabbildung der zentralen Tabelle |
| `Services/ExportAuditCsvService.cs` | Spalte am ENDE der Kopfzeile, Schreiben und Lesen |
| `Services/ExcelExportService.cs` | Spalte **52** im Blatt `Sales` des zentralen Sales_All |
| `Services/ManualExcelImportService.cs` | eigenes Zielfeld statt Alias auf `PostingDate` |
| `Services/DatabaseSeedService.cs` | Spanien-Zuordnung, aus `EnsureSpainPostingDateMapping` wurde `EnsureSpainDateMappings` |

**Position 52, bewusst am Ende.** Vor der Spalte stehen unveraendert `Market Segment` (50) und
`Market Segment Source` (51). Der Kopfzeilentest in
`TrafagSalesExporter.Tests/CentralExcelMarketSegmentTests.cs` sichert weiter die vier
Ankerpositionen 2, 20, 23 und 49 ab. Ein Einschub in der Mitte waere im Excel-Nachweis mit
seinen Blattformeln still toedlich.

**Die eine Falle, die dabei aufgefallen ist.** Der Kopfzeilen-Alias
`["lineregistrationdate"]` zeigte im Importer direkt auf `PostingDate`, aus der Zeit, als der
spanische Export noch kein Buchungsdatum lieferte. Mit beiden Spalten in derselben Datei
entschied allein die Spaltenreihenfolge, welche gewinnt. Jetzt hat jedes Feld sein eigenes
Ziel, und der Rueckfall ist ausdruecklich **spaltenweise statt zeilenweise**: fehlt die Spalte
`PostingDate` ganz, tritt `LineRegistrationDate` an ihre Stelle (Verhalten wie vorher, damit
aeltere Dateien beim Reimport ihr Buchungsdatum nicht verlieren). Ist die Spalte vorhanden,
bleiben **leere Zellen leer**. Eine in Spanien noch nicht validierte Rechnung darf kein
Buchungsdatum bekommen, sonst zaehlt sie Umsatz in einer Periode, in der sie fachlich keinen
hat. Genau dieser Fall ist als Test abgesichert
(`CentralExcelLineRegistrationDateTests.ManualImport_KeepsPostingDateAndLineRegistrationDateApart`).

**Was sich fachlich NICHT aendert.** Die Periodenabgrenzung bleibt
`Year(PostingDate ?? InvoiceDate ?? ExtractionDate)`. Das neue Feld geht in keine
Finance-Spalte, keine Marge und keine Summe ein; es wird nur mitgefuehrt und ausgegeben. Im
Excel steht es als reines Datum ohne Uhrzeit, wie die drei bestehenden Datumsspalten, obwohl
die Quelle eine Uhrzeit fuehrt.

**Rueckbau, falls Andreas widerspricht.** Ein Commit zurueck. Die Datenbankspalte bleibt dabei
stehen und stoert nicht, weil sie `NULL` erlaubt und nirgends gelesen wird.

**Produktiv nachgemessen nach dem Deploy** mit dem neuen read-only Werkzeug
`.tmp_tools/CheckLineRegistrationColumn`:

| Messung | Ergebnis |
| --- | --- |
| Spalten in `CentralSalesRecords` | `51`, `LineRegistrationDate` als letzte vorhanden |
| Fuellgrad `LineRegistrationDate`, alle neun Standorte | `0` — vor dem ersten Spanien-Import korrekt |
| `TRES` Zeilen / davon mit `PostingDate` | `7'168` / `1'523` (`21.2 %`) |
| `TRUK` Zeilen / davon mit `PostingDate` | `3'106` / `3'090` (die bekannten `16` ohne Datum) |
| Spalten-Zuordnung Spanien | **keine eigenen Zeilen**, generischer Kopfzeilen-Fallback aktiv |

**Die letzte Zeile aendert den Rest dieses Dokuments an einer Stelle.** Abschnitt 8 und 9
sagen, die neue Spalte muesse fuer Spanien von Hand in den Einstellungen zugeordnet werden,
weil Spanien anders als UK und Deutschland keine verdrahtete Zuordnung im Seed hat. Das
stimmt heute nicht mehr: Spanien fuehrt in `ManualExcelColumnMappings` ueberhaupt keine
Zeilen mehr und laeuft ueber den generischen Kopfzeilen-Fallback, der alle Felder ueber
`HeaderMap` selbst erkennt. `EnsureSpainDateMappings` hat deshalb bewusst nichts angelegt —
eine einzelne Zeile wuerde den Fallback abschalten und alle uebrigen Felder leer laufen
lassen. **Handarbeit in den Einstellungen ist also nicht noetig**, weder fuer
`PostingDate` noch fuer `LineRegistrationDate`.

**Ein Alarm im Deploy, erklaert und nachgemessen:** `trafag_exporter.db-wal` (`0` Bytes) und
`-shm` galten als verschwunden. Das ist der WAL-Flush beim Herunterfahren durch
`app_offline.htm`, kein Datenverlust. Beide Dateien sind um `10:15:55` beim Neustart wieder
da, die Hauptdatei blieb in Laenge und Schreibzeit unveraendert.

**Offen:** die Sichtpruefung im erzeugten Sales_All. Der Fuellgrad ist heute `0`, weil seit
dem Deploy kein Spanien-Import lief; erst danach traegt die Spalte Werte. Weiterhin offen
sind Andreas' Antwort auf Abschnitt 10b und der Blocker aus ISS-004.2: Santi Gomez muss die
35-Tage-Version des Exportskripts auf dem spanischen Server ersetzen.
