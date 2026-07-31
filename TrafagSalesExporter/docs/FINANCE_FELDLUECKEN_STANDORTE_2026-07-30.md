# Feldlücken je Standort — Soll/Ist und Mailtext

Stand: 2026-07-30

Datenbasis: `Finance_Dashboard_Audit_All_2026-07-29.csv` (95'168 Zeilen, Audit-Export vom
2026-07-29, identische Quelle wie die Dashboards — siehe `UseAuditCsvAsCentralSource`).
Diese Datei ist **neuer** als `docs/FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28.md`
(Produktiv-DB Stand 2026-07-27); die UK-Zahlen darin sind überholt, und die
Kostenfüllgrad-Tabelle darin ist irreführend (siehe Abschnitt 3).

## 1. Gemessener Ist-Stand

| TSC | Zeilen | Lieferant | Kosten | Kunde Name | Kunde Land | weitere Auffälligkeit |
| --- | --- | --- | --- | --- | --- | --- |
| TRCH | 47'142 | 0 % | 97 % | 100 % | 100 % | 2026er Daten kommen von T76 (SAP-seitig) |
| TRAT | 1'790 | 0 % | 100 % | 100 % | 100 % | dito |
| TRDE | 7'171 | **0 %** | 69 % | **0 %** | **0 %** | 2'903 Artikeltexte mit RTF-Schriftmüll |
| TRES | 5'504 | **0 %** | 81 % | 100 % | 100 % | 231 Zeilen ohne jedes Datum; Jan–27.05.2026 fehlt |
| TRFR | 2'577 | 5 % | 52 % | 100 % | 98 % | — |
| TRIN | 6'990 | 12 % | 99 % | 100 % | 99 % | — |
| TRIT | 19'534 | **71 %** | 96 % | 100 % | 99 % | bester B1-Standort |
| TRUK | 2'955 | **100 %** | 93 % | 100 % | 100 % | ~~2025 fehlt komplett~~ **falsch, siehe Korrektur unten** |
| TRUS | 1'504 | 0.4 % | 90 % | 100 % | 99 % | — |

`Lieferant` = mindestens `SupplierNumber` oder `SupplierName` gefüllt. `Kosten` =
`StandardCost` numerisch > 0. **Die Kostenspalte nicht als Datenqualität lesen** —
siehe Abschnitt 3.

### UK ist erledigt

Am 2026-07-27 war TRUK bei `0 %` Lieferant und `0 %` Kosten (1'088 Zeilen). Im Audit vom
2026-07-29 sind es **2'955 Zeilen, 100 % Lieferant, 93 % Kosten**. Der Reimport ist gelaufen,
das UK-Mapping wirkt. Ältere Tabellen mit „UK: braucht noch den Reimport" sind überholt.

**KORREKTUR 2026-07-31 — „2025 fehlt komplett" war falsch.** Die Notiz in der Tabelle oben war
aus der überholten Analyse vom 2026-07-28 übernommen und beim Erstellen dieser Datei nicht am
Audit-CSV nachgemessen. Tatsächlich enthält dasselbe CSV für TRUK:

| Jahr | Zeilen |
| --- | --- |
| 2025 | **1'867** |
| 2026 bis 27.07. | 1'082 |
| ohne `InvoiceDate` | 6 |

Der UK-2025-Backfill (`FINANCE_BACKFILL_UK_ES_2026-07-28.md`) ist also gelaufen und drin. **Für
TRUK ist damit gar nichts offen** — nicht Lieferant, nicht Kosten, nicht 2025. Der erste
UK-Mailentwurf enthielt deshalb eine falsche Aussage („the UK data we hold starts in January
2026") und wurde am 2026-07-31 ersetzt.

**Lehre:** übernommene Auffälligkeiten aus einer älteren Datei sind keine Messung. Die Spalte
„weitere Auffälligkeit" ist die einzige Spalte dieser Tabelle, die nicht aus dem
Reproduktionsskript in Abschnitt 7 stammt — und genau dort steckte der Fehler.

### Lieferant: Nummer und Name laufen immer gemeinsam

Am 2026-07-31 getrennt gemessen, weil eine andere Auswertung für TRUK `0` Lieferanten zeigte:

| TSC | Zeilen | mit `SupplierNumber` | mit `SupplierName` |
| --- | --- | --- | --- |
| TRCH, TRAT, TRDE, TRES | 61'607 | 0 | 0 |
| TRFR | 2'577 | 134 | 134 |
| TRIN | 6'990 | 809 | 809 |
| TRIT | 19'534 | 13'925 | 13'925 |
| TRUK | 2'955 | **2'955** | **2'955** |
| TRUS | 1'504 | 6 | 6 |

**Es gibt keinen Fall „Nummer gepflegt, Name leer".** Die beiden Felder sind in dieser Quelle
immer gemeinsam gefüllt oder gemeinsam leer. Eine Auswertung, die für TRUK `0` zeigt, kann das
also nicht aus diesem CSV haben — dort sind Zeilenzahl und Lieferantenzahl aller anderen
Standorte deckungsgleich, nur die UK-Zeile weicht ab. Verdacht: in jener Tabelle ist die
UK-Zeile inklusive Statustext („Mapping jetzt da — braucht noch den Reimport") unverändert aus
der Analyse vom 2026-07-28 übernommen und nicht neu gemessen worden. Wer eine solche Tabelle
gegen dieses Dokument stellt, sollte zuerst die Quelle jener UK-Zeile prüfen.

### Lieferantenlücke in der richtigen Mengeneinheit

Gepflegt wird der Lieferant am **Artikel** (`OITM.CardCode`), nicht an der Belegzeile. Für die
Bitte an die Standorte ist deshalb die Artikelzahl die relevante Grösse:

| TSC | Zeilen ohne Lieferant | betroffene Materialien | von insgesamt |
| --- | --- | --- | --- |
| TRIT | 5'602 | **939** | 3'280 |
| TRIN | 6'154 | **1'271** | 1'437 |
| TRFR | 2'434 | **374** | 433 |
| TRUS | 1'435 | **518** | 521 |

## 2. Was NICHT bei den Standorten liegt

Abgrenzung vor dem Mailversand geprüft — diese Lücken dürfen nicht an die Standorte gehen:

| Feld | Ist | Warum keine Standortfrage |
| --- | --- | --- |
| `ProductDivisionCode` / `ProductFamilyCode` | 0 % außer CH/AT | Sparte wird **zentral** über die Materialnummer gegen die TR-AG-Referenz (`ProductDivisionRefSet`) gematcht. Lokale ERP-Sparten werden bewusst nicht verwendet (`ManagementCockpit.razor:994`). Relevant ist nur eine zum TR-AG-Stamm passende Materialnummer. |
| `StandardCost` (Füllgrad) | 52–100 % | Die Lücke besteht zu 95–97 % aus Nicht-Warenpositionen, wo `0` korrekt ist. Messartefakt, siehe Abschnitt 3. |
| `StandardCostVariable` / `StandardCostFixed` | **0 % bei allen neun** | Kein Importer schreibt die Felder — sie werden nur aus dem Audit-CSV zurückgelesen (`ExportAuditCsvService.cs:349`). Eigene Baustelle. |
| `DocumentRate` | 0 % bei DE/ES/UK | Umrechnung läuft über die zentrale `CurrencyExchangeRates`-Tabelle (`CurrencyExchangeRateService`, `ManagementCockpitService.RateToChf`), nicht über den Belegkurs. Kosmetisch. |
| `CustomerIndustry`, `Incoterms2020`, `OrderDate`, `SalesResponsibleEmployee` | lückenhaft | In `HanaQueryService` und `ManualExcelImportService` gemappt, also durchleitbar, gehen aber in keine Berechnung ein. Nice to have, keine Anforderung. |
| Lieferant CH/AT | 0 % | `FinanzdataSchweizOeSet` hat kein Lieferantenfeld und wird nie eines haben (VBRP ist kein Einkaufsbeleg). Über die TSC-Regel in `GroupMarginSupplierClassifier` gelöst. |

## 3. Der Kostenfüllgrad ist ein Messartefakt — nicht an die Standorte melden

Ein erster Entwurf dieser Mail enthielt eine Bitte an Frankreich, „48 % der Artikel haben
keine Kostenbasis". **Das wäre falsch gewesen.** Aufschlüsselung der unkalkulierten Zeilen
nach Material:

| TSC | Zeilen ohne Kosten | davon Fracht/Verpackung/Zertifikat/Service | Anteil |
| --- | --- | --- | --- |
| TRDE | 2'250 | 2'185 | **97 %** |
| TRFR | 1'249 | 1'201 | **96 %** |
| TRES | 1'041 | 989 | **95 %** |
| TRCH | 1'581 | 925 | 59 % |
| TRUS | 143 | 80 | 56 % |
| TRIT | 835 | 246 | 29 % |
| TRUK | 193 | 45 | 23 % |

Die Top-Positionen sind eindeutig:

- TRFR: `M_FR01_000002 Frais et Port` (907), `M_FR01_000021 NF L 00-015B CERTIFICATE` (282)
- TRDE: `VV Verpackung & Versicherung` (1'492), `VP Verpackung` (296), `9999 Telekom
  Festnetzrechnung` (183), diverse `V00…`-Versandpositionen
- TRES: `- Entregado por Hugo Cuesta` (377), `P999 Porte pagado` (346), `CE001/CE002
  Condiciones de entrega` (202)
- TRUS: `Credit Memo … handling fee` (63), `H70210 EN10204-2.1 CERTIFICATE` (9)

Bei TRIT und TRUK, wo der Nicht-Waren-Anteil scheinbar niedrig ist, sind die restlichen
„Artikelzeilen" bei Sichtprüfung überwiegend ebenfalls keine Waren: `MANUALE USO E
MANUTENZIONE` (45), `RT - P - CUSTOM` (47), `BOMS_WARTSILA_…`, bei UK `DOC Declaration of
Conformity` (63), `HS: 9026.2000` (25), `H70313_CAL Calibration Certificate` (4).

**Konsequenz für uns, nicht für die Standorte:** Der ausgewiesene Kostenfüllgrad ist zu
pessimistisch, weil Fracht-, Verpackungs-, Zertifikats- und Dokumentationspositionen im
Nenner mitlaufen. Für eine belastbare Kennzahl müssten diese Positionen ausgeschlossen
werden — z. B. über die Materialnummer (nicht im TR-AG-Stamm) oder ein Positionstyp-Kennzeichen.
Eigener Arbeitspunkt.

## 4. Trafag-Erkennung: Verifikation FR/IN

Die Erkennung ist kein `*`-Wildcard, sondern ein Wortgrenzen-Regex über
`SupplierNumber + SupplierName + SupplierCountry` (`GroupMarginSupplierClassifier.cs:62`):

```
\b(TRAFAG|TR-AG|TRCH|TRIT|TRIN|GFS|GESELLSCHAFT FUER SENSORIK|GESELLSCHAFT FUR SENSORIK)\b
```

Gegen die 943 befüllten FR/IN-Zeilen geprüft:

| TSC | Lieferantwert | Zeilen | Klassifikation | Gesellschaft |
| --- | --- | --- | --- | --- |
| TRIN | `V0078 / Trafag AG / CH` | 677 | Intern | TR_AG |
| TRFR | `S_CH01_0070540 / Trafag Italia S.r.l. / IT` | 43 | Intern | TR_IT |
| TRFR | `S_CH01_0065180 / Trafag AG / DE` | 20 | Intern | TR_AG |
| TRFR | `S_CH01_0065180 / Trafag AG / CH` | 20 | Intern | TR_AG |

Vollklassifikation: **TRIN 677 Intern / 132 Extern / 6'181 unklar**, **TRFR 83 Intern /
51 Extern / 2'443 unklar**. Über alle 38 unterschiedlichen Fremdlieferantennamen (Cenlub,
SUNRAYS, MARS ASSOCIATES, …) **kein einziger False Positive** — die Wortgrenzen-Entscheidung
gegen reines Substring-Matching ist damit auf Produktivdaten belegt, nicht nur im
Codekommentar behauptet.

Zwei Präzisierungen:

- **Der Filter ist nicht der Engpass.** 95 % der FR- und 88 % der IN-Zeilen haben überhaupt
  keinen Lieferanten — dort kommt der Regex nie zum Zug.
- **Die Erkennung hängt am Namen, nicht an der Nummer.** `S_CH01_0065180` ist bei TRFR
  Trafag AG, wird aber nur erkannt, weil der Name „Trafag AG" enthält. Die Nummer allein
  würde nicht matchen (`GroupMarginSupplierClassifier.cs:118`). Eine gepflegte Nummer ohne
  gepflegten Namen bringt also nichts.

## 5. Empfänger

Stand 2026-07-30, von Ingo recherchiert. Vorher war in keinem Dokument dieses Repos eine
Mailadresse hinterlegt — deshalb hier festgehalten, damit die Zuordnung beim nächsten
Durchgang nicht erneut rekonstruiert werden muss.

| Standort | Empfänger | Adresse | Anmerkung |
| --- | --- | --- | --- |
| **TRES** Spanien | Santi Gomez | `Santi.Gomez@trafag.es` | — |
| **TRDE** Deutschland | Rohail Munir | `Rohail.Munir@trafag.de` | Abschnitt nennt „your Alphaplan/BI contact" — falls Rohail das nicht selbst ist, bitte weiterleiten lassen |
| **TRIT** Italien | Paola Castagna | `Paola.Castagna@trafag.com` | **Achtung, zwei Vorgänge** — siehe unten |
| **TRIN** Indien | RanVijay Kumar | `RanVijay.Kumar@trafag.com` | Trafag-intern, primärer Adressat |
| **TRIN** Indien | Anurag Gupta | `agupta@tasc.co.in` | **externe Domain** — siehe unten |
| **TRIN** Indien | Chandra Pratap Singh | `chandra.s@tasc.co.in` | **externe Domain** — siehe unten |
| **TRFR** Frankreich | *offen* | — | Abschnitt steht, Empfänger fehlt |
| **TRUS** USA | *offen* | — | Abschnitt steht, Empfänger fehlt |
| **TRUK** UK | Cornell Williams | `Cornell.Williams@trafag.com` | Nachtrag 2026-07-31; nur „no action required" plus Frage nach 2025 |
| **TRCH / TRAT** | *entfällt* | — | keine Standortbitte |

**Paola Castagna läuft doppelt.** Sie ist bereits Adressatin des Standardkosten-/Bewertungs-
themas aus der Sitzung mit Andreas (`FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md`,
Abschnitte 5c/5d) mit Zusage **Ende August 2026**. Die Bitte hier ist eine andere: *Preferred
Vendor* am Artikelstamm, Stammdatenpflege, nicht Bewertungsmethode. Beides in einer Mail zu
mischen kostet den klaren Termin des Bewertungsthemas — **getrennt verschicken**. Zeitlich
kommt hinzu: B1-Upgrade Go-Live 2026-08-03, danach zwei Wochen Ferien; eine Antwort auf die
Stammdatenbitte ist vor Ende August unrealistisch.

**`tasc.co.in` ist keine Trafag-Domain.** Zwei der drei indischen Adressen liegen bei einem
externen Dienstleister. Der Mailtext selbst ist unkritisch (Feldnamen, Zeilenzahlen), aber im
Text steht das Angebot „we can send the item list" — **1'271 Artikelnummern des TRIN-Stamms**.
Vor dem Versand einer solchen Liste an eine Fremddomain kurz klären, ob das gedeckt ist;
im Zweifel nur an `RanVijay.Kumar@trafag.com` und von dort intern weiterverteilen lassen.

**Noch zu beschaffen: FR und US.** Beide haben eine echte Bitte im Mailtext (FR 374 von 433,
US 518 von 521 Artikeln) — FR ist sogar der kleinste Aufwand aller Standorte. Ohne Empfänger
bleiben genau die zwei Abschnitte liegen, die am schnellsten zu schliessen wären.

## 6. Mailtext an die Standorte

Aufgebaut für **einen Sammelversand an alle Standorte**. Die Abschnitte lassen sich ohne
Anpassung einzeln herausschneiden, falls stattdessen pro Standort verschickt werden soll —
was wegen des Paola-Doppelvorgangs (Abschnitt 5) ohnehin der sicherere Weg ist.

**Nachtrag 2026-07-31:** Genau das ist geschehen. Die versandfertigen Einzelmails mit Empfänger,
Subject und angepasster Anrede stehen in `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md`, samt
Versandtabelle für Rückmeldungen. Der Sammeltext hier bleibt als Quelle der Formulierungen und
Zahlen stehen — Änderungen an Zahlen zuerst hier, dann in die Einzelmails übernehmen.

> **Subject:** BI Dashboard — missing data fields per site
>
> Dear all,
>
> we have completed a field-by-field check of the sales data that feeds the group BI
> Dashboard, measured on the consolidated extract of 29 July 2026 (95,168 invoice lines
> across all sites). Below is one short section per site listing only what is actually
> missing on your side. Where a site has nothing to do, it says so.
>
> Three things we explicitly do **not** need from you, so nobody spends time on them:
>
> - **Product division / product family.** Derived centrally from the Trafag AG material
>   master; local ERP product divisions are deliberately not used. The only thing that
>   matters is that the **material number** on the invoice line matches the Trafag AG master.
> - **Exchange rates on the document.** Currency conversion is done centrally.
> - **Item costs on freight, packaging, certificate and documentation lines.** We checked
>   these and they are correctly zero. Our own coverage figure was misleading here; that is
>   ours to fix, not yours.
>
> ---
>
> **Italy (TRIT) — 19,534 lines**
>
> You are the best-performing site on supplier data, thank you: 71% of lines carry the
> supplier. Remaining gap: **939 of your 3,280 item codes** have no *Preferred Vendor*
> maintained on the item master (`OITM.CardCode`, Purchasing Data tab). We read the supplier
> from exactly that field, so an item without it produces invoice lines we cannot classify as
> intercompany versus third-party purchase — which is what the group margin depends on.
>
> Could you have those item codes reviewed? We can send you the list.
>
> ---
>
> **India (TRIN) — 6,990 lines**
>
> Supplier present on 12% of lines. The good news is that the mechanism works: of the lines
> that do carry a supplier, 677 are correctly identified as Trafag AG deliveries. It is simply
> not maintained on most items.
>
> Same field as Italy: *Preferred Vendor* on the item master (`OITM.CardCode`). **1,271 of
> your 1,437 item codes** are affected. Filling it would move roughly 6,100 invoice lines from
> "supplier unknown" into a proper classification. We can send the item list.
>
> ---
>
> **France (TRFR) — 2,577 lines**
>
> Supplier present on 5% of lines — **374 of your 433 item codes** have no *Preferred Vendor*
> maintained on the item master (`OITM.CardCode`). Same field and same request as Italy and
> India. Given the small number of item codes, this is probably the quickest of all the sites
> to close.
>
> Note that the lines which do carry a supplier are recognised correctly (Trafag AG and
> Trafag Italia), so nothing else is needed beyond the master data.
>
> ---
>
> **USA (TRUS) — 1,504 lines**
>
> Supplier present on 6 of 1,504 lines — **518 of your 521 item codes** have no *Preferred
> Vendor* maintained on the item master (`OITM.CardCode`). Same field and same request as
> above.
>
> ---
>
> **Germany (TRDE) — 7,171 lines**
>
> Three points. All three concern the Alphaplan export as it currently reaches us, so these
> are questions for your Alphaplan/BI contact rather than for master data maintenance:
>
> 1. **No supplier information at all** — supplier number, name and country are empty on all
>    7,171 lines. Can the export be extended to include the supplier of the goods on each
>    invoice line? This is what we need to separate intercompany deliveries from third-party
>    purchases. If it is not feasible in the short term, please tell us so we can plan around
>    it.
> 2. **No customer name and no customer country** — empty on all 7,171 lines, while the
>    customer *number* is present on all of them. German customers therefore appear in group
>    reports as bare numbers. Adding name and country to the export would fix this.
> 3. **Product descriptions contain technical formatting text** — 2,903 of 7,171 descriptions
>    (40%) begin with font-table text, for example:
>    `MS Shell Dlg, Microsoft Sans Serif, , , 9B4.4274.769.04.15.46.V3 Picostat PST4B3.44 …`
>    It looks as though a rich-text field is exported including its formatting header. For
>    those lines the product name is unusable in reports.
>
> ---
>
> **Spain (TRES) — 5,504 lines**
>
> 1. **No supplier information** — empty on all 5,504 lines. Before we ask for a technical
>    change, one question: does the Sage sales/delivery data model carry a concept of
>    "supplier" on a sales document at all? This is typically a purchasing attribute rather
>    than a sales one. If it does, could it be added to the export? If it does not, please
>    tell us, so we can look at another way to identify intercompany deliveries for Spain.
> 2. **231 lines have no date whatsoever** — neither invoice date nor posting date. Those
>    lines drop silently out of every monthly and yearly report. Could you check what kind of
>    documents these are?
> 3. **1 January to 27 May 2026 has never been exported.** The range export we received
>    starts on 28 May 2026, so the first five months of 2026 are missing from group reporting
>    entirely. Please run and send the range export for 01.01.2026 – 27.05.2026.
>
> ---
>
> **United Kingdom (TRUK) — 2,955 lines — no action required**
>
> Supplier information is now complete and the data looks good. Thank you. One open point,
> and only if 2025 is needed for group reporting: the data we hold starts in January 2026, so
> 2025 is absent. Let us know and we will request that export separately.
>
> ---
>
> **Switzerland / Austria (TRCH, TRAT) — no action required from the site**
>
> Listed for completeness. The SAP billing feed has no supplier field by design — a customer
> invoice has a customer, not a vendor — and this is handled by a central rule instead. The
> known gap in 2026 data is a SAP-side topic we are following up internally.
>
> ---
>
> Happy to set up a short call per site if that is easier than email, and we can provide the
> affected item lists on request.
>
> Best regards
> Ingo

## 7. Reproduzierbar

```powershell
$rows = Import-Csv -Path 'Finance_Dashboard_Audit_All_2026-07-29.csv' -Delimiter ';' -Encoding UTF8 |
        Where-Object { $_.TSC -match '^TR' }

# Fuellgrade je TSC
$rows | Group-Object TSC | ForEach-Object {
  $g = $_.Group; $n = $g.Count
  $cost = ($g | Where-Object { $v=0.0; [double]::TryParse($_.StandardCost, [ref]$v) -and $v -gt 0 }).Count
  [pscustomobject]@{
    TSC = $_.Name; N = $n
    Lieferant  = [int][Math]::Round(100.0 * ($g | Where-Object { $_.SupplierName -or $_.SupplierNumber }).Count / $n)
    KostenPct  = [int][Math]::Round(100.0 * $cost / $n)
    RtfMuell   = ($g | Where-Object { $_.Name -match 'Shell Dlg|Microsoft Sans Serif' }).Count
    OhneDatum  = ($g | Where-Object { -not $_.InvoiceDate -and -not $_.PostingDate }).Count
  }
} | Sort-Object TSC | Format-Table -AutoSize

# Betroffene Materialien statt Zeilen (richtige Mengeneinheit fuer die Standortbitte)
foreach ($t in @('TRIT','TRIN','TRFR','TRUS')) {
  $g  = $rows | Where-Object TSC -eq $t
  $ns = $g | Where-Object { -not $_.SupplierName -and -not $_.SupplierNumber -and $_.Material }
  "{0}: {1} von {2} Materialien ohne Lieferant" -f $t, ($ns | Group-Object Material).Count,
      ($g | Where-Object Material | Group-Object Material).Count
}

# Gegenprobe Kostenluecke: welche Materialien sind es wirklich?
$rows | Where-Object { $_.TSC -eq 'TRFR' -and -not ($(try{[double]$_.StandardCost -gt 0}catch{$false})) } |
  Group-Object Material | Sort-Object Count -Descending | Select-Object -First 8 Count, Name
```
