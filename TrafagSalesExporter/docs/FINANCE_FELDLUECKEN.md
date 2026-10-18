# Feldluecken je Standort: was fehlt, wer es besitzt, was nicht gefragt werden darf

Stand: 2026-08-17. Zusammengefuehrt aus drei Vorgaengerdateien (Datenluecken 2026-07-28,
Feldluecken Standorte 2026-07-30, Standort-Mails 2026-07-31), die alle als ueberholt
markiert waren.

**Aktuelle Fuellgrade und der Status je Punkt stehen in
`docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, nicht hier.** Die Messungen unten stammen
vom Auszug des 2026-07-29 mit 95'168 Zeilen (heute rund 97'500) und dienen der
Ursachenanalyse, nicht der Statusauskunft.

Die versandfertigen Mailtexte wurden bei der Zusammenfuehrung **ersatzlos entfernt**.
Zwei davon waren gegenstandslos geworden und haetten bei Versand Schaden angerichtet;
Begruendung in Abschnitt 1.

## 1. Die wichtigste Regel: erst die eigene Query pruefen

**Bevor ein Standort um Stammdatenpflege oder eine Exporterweiterung gebeten wird, pruefen,
ob die Information nicht schon vorliegt oder ob unsere eigene Export-SQL sie schlicht nicht
liest.** Das ist zweimal innerhalb einer Woche schiefgegangen:

| Datum | Bitte | Warum sie falsch war |
| --- | --- | --- |
| 2026-08-03 | Deutschland sollte den Export um Lieferant und Kundenname erweitern | Die Export-SQL ist **unsere** (`AlphaplanExportPackage/scripte/alphaplanExport.ps1`). Drei der vier Luecken waren Spalten, die unsere Query nie gelesen hat |
| 2026-08-05 | Indien sollte 1'271 Artikel mit Lieferanten pflegen | `OITM."U_Tasc_ST"` beantwortet die Frage bereits fuer 93 % der Artikel. Gueltig ist `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` |

Ein Standort, der ueberfluessige Pflege geliefert bekommt, nimmt die naechste Bitte nicht
mehr ernst. Auch Spanien liefert seine Laendercodes seit dem Reimport normiert — auch
diese Bitte hat sich erledigt.

## 2. Was NICHT bei den Standorten liegt

Vor jeder Standortanfrage gegen diese Tabelle pruefen:

| Feld | Ist | Warum keine Standortfrage |
| --- | --- | --- |
| `ProductDivisionCode` / `ProductFamilyCode` | 0 % ausser CH/AT | Sparte wird **zentral** ueber die Materialnummer gegen die TR-AG-Referenz (`ProductDivisionRefSet`) gematcht. Lokale ERP-Sparten werden bewusst nicht verwendet. Relevant ist nur eine zum TR-AG-Stamm passende Materialnummer |
| `StandardCost` Fuellgrad | 52–100 % | Messartefakt, siehe Abschnitt 3 |
| `StandardCostVariable` / `StandardCostFixed` | 0 % bei allen neun | Kein Importer schreibt die Felder, sie werden nur aus dem Audit-CSV zurueckgelesen. Eigene Baustelle |
| `DocumentRate` | 0 % bei DE/ES/UK | Umrechnung laeuft ueber die zentrale `CurrencyExchangeRates`-Tabelle, nicht ueber den Belegkurs. Kosmetisch |
| `CustomerIndustry`, `Incoterms2020`, `OrderDate`, `SalesResponsibleEmployee` | lueckenhaft | gemappt und durchleitbar, gehen aber in keine Berechnung ein |
| Lieferant CH/AT | 0 % | `FinanzdataSchweizOeSet` hat kein Lieferantenfeld und wird nie eines haben, VBRP ist kein Einkaufsbeleg. Ueber die TSC-Regel geloest |

## 3. Der Kostenfuellgrad ist ein Messartefakt

Ein erster Mailentwurf wollte Frankreich mitteilen, „48 % der Artikel haben keine
Kostenbasis". **Das waere falsch gewesen.** Die unkalkulierten Zeilen sind ganz
ueberwiegend gar keine Warenpositionen:

| TSC | Zeilen ohne Kosten | davon Fracht/Verpackung/Zertifikat/Service | Anteil |
| --- | ---: | ---: | ---: |
| TRDE | 2'250 | 2'185 | 97 % |
| TRFR | 1'249 | 1'201 | 96 % |
| TRES | 1'041 | 989 | 95 % |
| TRCH | 1'581 | 925 | 59 % |
| TRUS | 143 | 80 | 56 % |
| TRIT | 835 | 246 | 29 % |
| TRUK | 193 | 45 | 23 % |

Typische Positionen: `Frais et Port`, `Verpackung & Versicherung`, `Porte pagado`,
`Condiciones de entrega`, `EN10204-2.1 CERTIFICATE`, `Declaration of Conformity`,
`Calibration Certificate`. Auch bei TRIT und TRUK, wo der Nicht-Waren-Anteil scheinbar
niedrig ist, sind die restlichen Zeilen bei Sichtpruefung ueberwiegend ebenfalls keine
Waren.

**Konsequenz fuer uns, nicht fuer die Standorte:** Der ausgewiesene Kostenfuellgrad ist zu
pessimistisch. Fuer eine belastbare Kennzahl muessten diese Positionen ausgeschlossen
werden, etwa ueber die Materialnummer oder ein Positionstyp-Kennzeichen. Eigener
Arbeitspunkt.

## 4. Gemessener Ist-Stand, Auszug 2026-07-29

| TSC | Zeilen | Lieferant | Kosten | Kunde Name | Kunde Land | Auffaelligkeit |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| TRCH | 47'142 | 0 % | 97 % | 100 % | 100 % | 2026er Daten kamen von T76 |
| TRAT | 1'790 | 0 % | 100 % | 100 % | 100 % | dito |
| TRDE | 7'171 | 0 % | 69 % | 0 % | 0 % | 2'903 Artikeltexte mit RTF-Schriftmuell |
| TRES | 5'504 | 0 % | 81 % | 100 % | 100 % | 231 Zeilen ohne jedes Datum |
| TRFR | 2'577 | 5 % | 52 % | 100 % | 98 % | — |
| TRIN | 6'990 | 12 % | 99 % | 100 % | 99 % | Sales Type deckt die Klassifikation ab |
| TRIT | 19'534 | 71 % | 96 % | 100 % | 99 % | bester B1-Standort |
| TRUK | 2'955 | 100 % | 93 % | 100 % | 100 % | vollstaendig, nichts offen |
| TRUS | 1'504 | 0.4 % | 90 % | 100 % | 99 % | — |

### Lieferantennummer und -name laufen immer gemeinsam

| TSC | Zeilen | mit `SupplierNumber` | mit `SupplierName` |
| --- | ---: | ---: | ---: |
| TRCH, TRAT, TRDE, TRES | 61'607 | 0 | 0 |
| TRFR | 2'577 | 134 | 134 |
| TRIN | 6'990 | 809 | 809 |
| TRIT | 19'534 | 13'925 | 13'925 |
| TRUK | 2'955 | 2'955 | 2'955 |
| TRUS | 1'504 | 6 | 6 |

**Es gibt keinen Fall „Nummer gepflegt, Name leer".** Eine Auswertung, die fuer TRUK `0`
Lieferanten zeigt, kann ihre Zahl nicht aus dieser Quelle haben.

### Lieferantenluecke in der richtigen Mengeneinheit

Gepflegt wird der Lieferant am **Artikel** (`OITM.CardCode`), nicht an der Belegzeile. Fuer
eine Bitte an einen Standort ist deshalb die Artikelzahl die relevante Groesse:

| TSC | Zeilen ohne Lieferant | betroffene Materialien | von insgesamt |
| --- | ---: | ---: | ---: |
| TRIT | 5'602 | 939 | 3'280 |
| TRIN | 6'154 | 1'271 | 1'437 |
| TRFR | 2'434 | 374 | 433 |
| TRUS | 1'435 | 518 | 521 |

## 5. Trafag-Erkennung ist verifiziert und nicht der Engpass

Die Erkennung ist kein `*`-Wildcard, sondern ein Wortgrenzen-Regex ueber
`SupplierNumber + SupplierName + SupplierCountry`
(`Services/GroupMarginSupplierClassifier.cs`):

```
\b(TRAFAG|TR-AG|TRCH|TRIT|TRIN|GFS|GESELLSCHAFT FUER SENSORIK|GESELLSCHAFT FUR SENSORIK)\b
```

Vollklassifikation auf Produktivdaten:

| TSC | mit Lieferant | intern erkannt | extern | ohne Lieferant |
| --- | ---: | ---: | ---: | ---: |
| TRFR | 134 | 83 | 51 | 2'443 |
| TRIN | 809 | 677 | 132 | 6'181 |
| TRIT | 13'925 | 6'848 | 7'077 | 5'609 |
| TRUK | 2'955 | 2'803 | 152 | 0 |
| TRUS | 6 | 2 | 4 | 1'498 |

Ueber alle 38 unterschiedlichen Fremdlieferantennamen **kein einziger False Positive** —
die Entscheidung fuer Wortgrenzen statt Substring-Matching ist damit auf Produktivdaten
belegt, nicht nur im Codekommentar behauptet.

Zwei Praezisierungen:

- **Der Filter ist nicht der Engpass.** 95 % der FR- und 88 % der IN-Zeilen haben ueberhaupt
  keinen Lieferanten, dort kommt der Regex nie zum Zug.
- **Die Erkennung haengt am Namen, nicht an der Nummer.** Eine gepflegte Nummer ohne
  gepflegten Namen bringt nichts.

## 6. Deutschland: der eigentliche Blocker ist das Alphaplan-Schema

| Luecke | gemessen | Eigentuemer |
| --- | --- | --- |
| Lieferant (Nummer/Name/Land) | 7'171 von 7'171 leer | **uns** — Query liest keine Lieferantenspalte |
| Kundenname und -land | 7'171 leer, Kundennummer 7'171 gefuellt | **uns** — `RechnungsAdressenID` wird selektiert, aber nie aufgeloest |
| Artikelbezeichnung | 2'903 von 7'171 mit Font-Muell | **uns** — Rich-Text-Feld der Belegposition gelesen |
| `ArtikelNummer` = TR-AG-/SAP-`MATNR`? | 0 leer, Gleichheit unbelegt | **Deutschland** — echte Fachfrage |

Was fehlt, ist eine Tabellen- und Spaltenliste fuer `ApDaten`. Die DB liegt auf
`localhost\SQL2012` des DE-Servers hinter einem DPAPI-gebundenen Credential.
**Deshalb keine Tabellennamen erfinden** — ein geratenes `JOIN dbo.Adressen` im
ausgelieferten Skript waere derselbe Fehler wie bei UK-2025, nur mit laengerer Zuendschnur.

Gebraucht wird nur ein `INFORMATION_SCHEMA.COLUMNS`-Auszug, read-only, gefiltert auf
`%Adress%`, `%Artikel%`, `%Liefer%`, `%Kunde%`. Danach erweitern wir die Query selbst.
Offen bleibt zusaetzlich, ob Alphaplan ueberhaupt einen Lieferanten auf der
**Verkaufszeile** fuehrt oder nur einen Hauptlieferanten im Artikelstamm.

## 7. Lehren aus den Fehlern dieser Dateien

- **Uebernommene Auffaelligkeiten sind keine Messung.** Die Behauptung „UK 2025 fehlt
  komplett" stammte aus einer aelteren Datei und war falsch: TRUK hatte 1'867 Zeilen fuer
  2025. Der Backfill war laengst gelaufen.
- **Superlative gegen die Gesamtmenge pruefen.** Ein Entwurf nannte Italien „the
  best-performing site on supplier data" — TRUK stand bei 100 %, TRIT bei 71 %.
- **Zahlen nicht mit Formulierungen mitaendern.** Beim Umformulieren von Mailtexten sind
  die Messwerte unveraendert zu uebernehmen.
- **Technische Falle bei deutschen Mailtexten:** `docs/mails/Build-StandortMails.ps1` ist
  reines ASCII ohne BOM. PowerShell 5.1 liest eine BOM-lose Datei als Windows-1252, echte
  Umlaute landen als Mojibake. Umlaute daher als HTML-Entities (`&uuml;`, `&auml;`,
  `&ouml;`, `&szlig;`). Pruefung: die Datei darf kein Zeichen ausserhalb `\x00-\x7F`
  enthalten.

## Querverweise

- Status je Punkt: `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, dazu `docs/FINANCE_OFFENE_PUNKTE_2026-08-12.md`
- Supplier-Klassifikation und Laenderstatus: `docs/FINANCE_SUPPLIER.md`
- Indien, Sales Type statt Preferred Vendor: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`
- Ansprechpartner je Standort: `docs/ANSPRECHPARTNER.md`
- Export-SQL Deutschland: `docs/STANDORT_DE_ALPHAPLAN.md`
- Export-SQL Spanien: `docs/STANDORT_ES_SAGE.md`
