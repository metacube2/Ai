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
