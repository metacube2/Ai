# Finance: offene Punkte aus der PDF fuer Andreas, mit Daten geklaert (Stand 2026-11-24 12:38, Auswertung P76 und Import-Datei ergaenzt)

Auftrag Ingo 24.11.: offene Punkte der PDF `docs/finance_andreas/Finance_Lieferantenzuordnung_Mapping_Stand_2026-11-24.pdf`
selbst klaeren, soweit es mit Daten geht (Aufgaben 7, 4, 6, Vorbereitung P76). **Nur gelesen:** kein Code, keine Aenderung an
Produktiv-DB oder Mapping, kein Deploy, nichts committet. Rohauszuege mit Kunden- und Lieferantennamen liegen nur im Scratchpad
der Sitzung (`...\scratchpad\fin\`), hier stehen nur Zaehlungen, Materialnummern und Skripte.

## Quelle und Methode

- **Exporte** (Kopie aus `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\output\`, gezielt per Name gesucht):
  `Sales_All_2026-11-23.xlsx` (12:10, neuester; ein Export vom 24.11. lag um 11:59 noch nicht vor), `Mapping_2026-11-23.xlsx`,
  zum Vergleich `Sales_All_2026-11-20.xlsx`, `Mapping_2026-11-20.xlsx` und `Sales_All_2026-11-18.xlsx` (Fassung 12:10, nicht 10:15).
- **Methode wie FINANCE_SUPPLIER 12/12a und Plan Stufe 2 Abschnitt 00 A:** Blatt Finance Details (Kopf Zeile 4), Year 2025/2026.
  Lieferant = `Supplier number` oder `Supplier name` gefuellt. Entscheid 1 = Issue Topic 5; Umsatz = `Net Sales CHF`,
  HK TR AG = `Cost Basis CHF`, lokal = `Standard Cost CHF (per unit)` x `Quantity`. Gruppen mit Marge aus „Gruppenmarge Details“
  gezaehlt (offen, sobald eine Zeile Status „Lieferant unklar“, „Kostenwaehrung abweichend“ oder „Konzernkosten fehlen“ hat),
  nie aus den Formelzellen der Summary.
- **Kalibriert vor der Messung:** dasselbe Skript liefert am 18.11. FR 2'437/2'846, US 1'129/1'790 (genau wie dokumentiert) und am
  20.11. Entscheid 1 = 5'888 Zeilen, 5,488 Mio. CHF, 2,236 gegen 3,075, Diff 0,839, 126 ohne lokalen Preis, 11 von 18 Gruppen,
  Topic TRDE 1: 5'518 / 6: 2'143, TRES 5: 5'204, TRUK 2: 1'485, TRCH 3: 1'243, TRAT 2: 217, TRIN 8: 153, DE 732 Vorschlaege
  5,716 Mio. EUR (alles wie dokumentiert). IT 18.11. 20'221/20'227 statt 20'214/20'220 und IN 8'454 statt 8'453: vermutlich,
  weil die kopierte Fassung von 12:10 ist und die Doku die von 10:15 nennt.
- **Indien Code 8:** Die PDF-Zahl „6 Artikel, DM000083 112 Zeilen, 9,09 Mio. INR“ laesst sich aus Finance Details nicht
  nachbilden, auch nicht am 20.11., den die PDF zitiert (dort 5 Artikel, 122 Zeilen, 9,62 Mio. INR). Das Mapping-Blatt ist
  nicht die Quelle (TRIN-Vorschlaege sind andere Artikel). Herkunft der PDF-Zahl unbekannt (Vermutung: B1-Messung 12a,
  „bei 5 Artikeln fehlt die Sachnummer“, mit anderem Zeitraum). H7-Nebenbefund: 274 statt 272 Zeilen am 20.11. (die Definition von damals ist nicht dokumentiert; hier
  Konzernkosten TR AG je Einheit = `Cost Basis CHF / Quantity` unter 5 % von `Standard Cost CHF (per unit)`, Menge ungleich 0).
- **B1:** `.tmp_tools/HanaQ` (nur SELECT, Zugang aus dem lokalen DB-Abbild wie am 18.11.), Schema als `"{SCHEMA}"` (gross;
  `"{schema}"` scheitert mit „invalid schema name FR01_p“). TRFR, TRUS, TRIT gelesen; **TRIN nicht** (nur ueber den Server).
- **Skripte:** `skripte/` (`extract.py` Auszug, `analyze.py` Aufgabe 7, `dumps.py` CSV-Vorbereitung, `italien162.py`, `zusatz.py`,
  `extr_dates.py`, `mk_*.py` erzeugen die `q_*.sql` fuer HanaQ). Aufruf mit uv-Cache-Python `-I -X utf8` im Scratchpad-Ordner.

## Ergebnis 7: PDF gegen neuesten Export

| Kennzahl | PDF (Stand) | 20.11. nachgerechnet | 23.11. neu |
| --- | --- | --- | --- |
| FR mit Lieferant | 2'437 von 2'846 (18.11.) | 2'440 von 2'850 | 2'440 von 2'850 |
| US mit Lieferant | 1'129 von 1'790 (18.11.) | 1'145 von 1'810 | 1'146 von 1'811 |
| IT mit Lieferant | 20'214 von 20'220 (18.11.) | 20'227 von 20'233 | 20'228 von 20'234 |
| IN mit Lieferant | 1'380 von 8'453 (18.11.) | 1'380 von 8'484 | 1'381 von 8'497 |
| UK mit Lieferant | 3'115 von 3'115 | 3'139 von 3'139 | 3'139 von 3'139 |
| ES mit Lieferant | 0 von 8'105 | 0 von 8'156 | 0 von 8'230 |
| DE mit Lieferant | 0 von 7'800 | 0 von 7'800 | 0 von 7'853 |
| Topic TRDE | 1: 5'518, 6: 2'143 | gleich | 1: 5'555, 6: 2'158 (5: 133, 4: 7) |
| Topic TRES 5 | 5'204 | 5'204 | 5'248 (1: 2'191, 6: 791) |
| Topic TRUK 2 / TRCH 3 / TRAT 2 / TRIN 8 | 1'485 / 1'243 / 217 / 153 | gleich | gleich |
| Topic TRIT | (nicht genannt) | 2: 176, 7: 251, 9: 6 | 2: 176, **3: 191**, 7: 60, 9: 6 (Paket C wirkt) |
| Gruppen mit Marge (Summary) | 11 von 18 | 11 von 18 | 11 von 18, dieselben 7 offen |
| Entscheid 1 (Topic 5) | 5'888 Z., 5,49 Mio., 2,24 gegen 3,07 = 0,84 | 5'888, 5,488, 2,236 / 3,075 = 0,839 | **5'933 Z., 5,529 Mio., 2,252 / 3,097 = 0,845 Mio. CHF**, 126 ohne lokalen Preis |
| davon je Land 23.11. | – | – | ES 5'248 (0,558), US 499 (0,289), DE 133 (0,006), IN 50 (−0,008), FR 3 (0) |
| bestaetigte Mappings | 0 | 0 | **0** (Vorschlag 917, offen 1'250; DE 732 Vorschlaege 5,754 Mio. EUR) |
| Supplier-Origin-Werte | 7 Werte beschrieben | Spalte fehlt (Export 12:10, Deploy 13:23) | **5 Werte**: Rule CH/AT 55'212, Source field 27'317, none 11'350, Rule Sales Type 8'004, CH plant master 5'943 |
| Indien Code 8 | 153 Z., 6 Artikel, DM000083 112 Z. 9,09 Mio. INR | 153 Z., **5 Artikel**, DM000083 **122 Z., 9,62 Mio. INR** | gleich wie 20.11. |
| H7-Nebenbefund | 272 Z., rund 77'000 CHF | 274 Z., 76'399 CHF | 276 Z., 76'455 CHF |

Offene Gruppen 23.11. (aus Details gezaehlt): FR 2025 (5 „Lieferant unklar“), FR 2026 (7), US 2025 (19), US 2026 (58),
IT 2025 (6), IN 2025 (22 unklar, 85 Konzernkosten fehlen), IN 2026 (14 unklar, 68 Konzernkosten fehlen).

**Befund Supplier Origin (neu, Ursache als Vermutung):** Im Export 23.11. kommen „Last goods receipt“ und „IT override list“
gar nicht vor. Die Laender sind nach dem Deploy 20.11. 13:23 neu geladen: `extraction date` (HANA-Zeitstempel des Imports) ist je
TSC verschieden, FR 23.11. 12:00:10, IT 12:00:37, US 12:00:55, TRIN 16:31 (indische Ortszeit, = 12:01 MEZ), und US/IT haben je eine
Zeile mehr als am 20.11. FR/US/IT haben ihre Lieferanten nachweislich aus dem Wareneingang (FR 2'440 statt 120); der Rueckfall ohne
Wareneingang ist also nicht gelaufen. Da auch die Uebergangsliste fehlt, geht das Feld `SupplierSource` auf dem Weg zum Export
verloren. **Vermutete Kette (aus dem Code, nicht produktiv belegt):** `SiteExportService` (Zeile 81 ff.) schreibt die Audit-CSV,
speichert dann die DB und laedt danach auf SharePoint; `ConsolidatedExportService` holt vor Sales_All die neueste SharePoint-CSV
und setzt ihr die SharePoint-Zeit (Zeilen 212-248), und ohne `UseAuditCsvAsCentralSource` nimmt
`CentralSalesDataProvider.GetLatestRecordsBySiteAsync` je TSC die CSV, sobald sie neuer ist als `StoredAtUtc` der DB
(Zeilen 134-142). Die Audit-CSV fuehrt `SupplierSource` nicht (`ExportAuditCsvService` Kopf Zeilen 65-68, Leser 331-334).
Pruefen mit Ingos Freigabe: produktiv `SELECT Tsc, SupplierSource, COUNT(*) FROM CentralSalesRecords GROUP BY 1, 2` und
App-Protokoll „Neueste SharePoint-Audit-CSV lokal synchronisiert“ vom 23.11. Bis dahin ist die PDF-Tabelle „Supplier Origin“ fuer
FR/US/IT falsch: dort steht heute „Source field“.

## Ergebnis 4: US und FR „unklar“ (Topic 9)

**Die Annahme der PDF stimmt nicht.** Code 9 heisst nicht „Lieferantenfelder widersprechen sich“: `Unklar` entsteht nur, wenn
alle drei Lieferantenfelder leer sind, kein Sales Type da ist und kein Materialschluessel geprueft werden kann
(`GroupMarginSupplierClassifier.cs` 177-180, `IssueTopics.SupplierUnclear` Kommentar Zeile 443). Gemessen 23.11.: 131 Zeilen
Unklar = Topic 9 (FR 12, US 77, IT 6, IN 36), **alle 131 ohne Material, 0 mit einem Lieferantenfeld**. `OITM.CardCode` und der
letzte Wareneingang sind ohne Artikel nicht anwendbar. Echte Widersprueche (UK TR09, eine Zeile) laufen nicht ueber Code 9.

B1 nach Belegnummer (`OINV/ORIN` + `INV1/RIN1`): **alle Zeilen FR/US/IT sind Dienstleistungsbelege (DocType `S`) ohne ItemCode.**

| Land | Zeilen | Umsatz lokal | was es ist (Konto in B1) |
| --- | ---: | ---: | --- |
| FR | 12 (2025: 5, 2026: 7) | 2025 −360, 2026 −5'300 EUR | 8 Gutschriften Gewaehrleistungsfaelle „HDF-S-…“ auf 709700 Rabais (−5'380); Rechnung und Gutschrift 611000 Sous-traitance (+686,40/−686,40); 1 Zeile 707000 (−112); **1 Zeile auf 445710 TVA collectee (−167,32)** |
| US | 77 (2025: 19, 2026: 58) | 2025 5'202, 2026 18'985 USD | 23 Zeilen 41155000 **Sales Revenue Tariff Add-On** (26'068, Zollzuschlag; Text mit Teilenummern, Rechnungen 400586, 401402); 29 Gutschriften 51150000 COGS Freight (Bearbeitungsgebuehren, 227); 11 Zeilen 41150000 Shipping/Handling/CC Fees; Rest einzelne Korrekturen (41050000, 51050000, 51000000, 71000000, 71050000, 62000000, 33050000) |
| IT | 6 (alle 2025) | 80'442 EUR | 2 Rechnungen 47005070 Ricavi per prestito di personale (86'550) und 47005020 Prestazioni servizi (250); 4 Gutschriften 47005100 Premi su vendite (−6'358) |
| IN | 36 | 4,87 Mio. INR (rund 50'700 CHF) | nicht gelesen (Indien-B1 nur ueber den Server) |

**Wirkung:** Diese Zeilen allein sperren in der Summary FR 2025 und 2026, US 2025 und 2026 und IT 2025 (dort gibt es keine
andere offene Zeile); IN bleibt auch ohne sie wegen Code 8 offen.

**Vorschlag je Fall (Entscheid Ingo/Andreas, nicht umgesetzt):**
- Zeilen ohne Artikel aus Dienstleistungsbelegen nicht „unklar“ fuehren, sondern als eigene Gruppe „ohne Artikel / Nebenerloes“
  mit Kosten 0 (wie use 0) oder ganz aus der Gruppenmarge nehmen; Lieferant oder Stammpflege ist hier sinnlos. Nachgerechnet
  (`skripte/gruppen_ohne_unklar.py`, Export 23.11.): ohne diese Zeilen haetten **16 von 18 Gruppen** eine Marge; offen blieben
  nur IN 2025 (85) und IN 2026 (68 Zeilen „Konzernkosten fehlen“ = die 153 Zeilen Code 8).
- Fachfrage an Finance: Gehoeren in den Ist-Umsatz (a) FR-Zeile auf dem Mehrwertsteuerkonto 445710, (b) US Tariff Add-On
  (26'068 USD, Erloes ohne Kosten), (c) IT Personalverleih 86'550 EUR 2025, (d) US-Gutschriften auf COGS-Konten?
- Der PDF-Text zu Code 9 und Frage 5 („gewinnt der Name oder die Nummer“) ist zu ersetzen; die Frage betrifft nur UK TR09.

## Ergebnis 6: Italien, 162 Artikel ohne Standardlieferant

- Liste: Anhang des Entwurfs an Paola (`TRIT_items_without_preferred_vendor_2026-11-13.xlsx`, 162 Artikel, 492 Zeilen, 987'611 EUR).
- **B1 heute: bei allen 162 ist `OITM.CardCode` weiter leer** (162 von 162 im Stamm gefunden); Paola hat nicht gepflegt.
- **Export 23.11.: 155 der 162 Artikel kommen in Finance Details 2025/2026 vor, mit 510 Zeilen und 1'020'337 EUR, und alle 510
  Zeilen haben jetzt einen Lieferanten** (keine teilweise oder ganz leeren Artikel). Herkunft: letzter Wareneingang (Stamm leer,
  Uebergangsliste enthaelt sie nicht; die Spalte Supplier Origin zeigt es wegen des Befunds oben nicht). B1-Gegenprobe (nur
  Ausgangsrechnungen `OINV/INV1` ab 2025, ohne Gutschriften): 521 Zeilen, davon 514 mit Wareneingang am oder vor dem
  Rechnungsdatum (448 Trafag AG mit 957'592 EUR, 66 bei drei italienischen Lieferanten) und 7 ohne Wareneingang (0 EUR).
- Zuordnung im Export: 443 Zeilen Intern / TR AG mit Konzernkosten TR AG, 67 Zeilen Extern (50 Artikel, alle `GC…`-Handelsware:
  Manometer, Thermometer, Niveauschalter; keine numerische Trafag-Sachnummer), davon 2 Zeilen Code 7 (keine lokalen Kosten).
- **Rest in Finance: 0 Zeilen, 0 EUR ohne Lieferant.** Nicht in Finance Details 2025/2026 sind 7 Artikel, im Blatt Sales alle
  mit `Finance | Include = FALSE`: `66176` (1 Zeile, 183 EUR, 15.10.2026, Lieferant Trafag AG aus dem Wareneingang) und sechs
  Hausnummern `M_IT01_000311`, `_000320`, `_000531`, `_000760`, `_000842` (2 Zeilen), `_001778` mit 7 Zeilen zu 0 EUR
  (28.08. bis 12.11.2026), **ohne Standardlieferant und ohne Wareneingang**.
- **Restliste fuer Paola:** nur diese sechs Hausnummern (7 Zeilen, 0 EUR, nicht in Finance), Datei `italien_rest.csv` im
  Scratchpad. Fuer die Gruppenmarge braucht es Paolas Pflege der 162 nicht mehr. Sinnvoll bleiben die Plausibilisierung der
  67 Zeilen mit italienischem Lieferanten und die Schenker-Frage (Abschnitt 9 FINANCE_SUPPLIER).

## Vorbereitung fuer P76 (CSV im Scratchpad, Namen moeglich)

Pfad: `C:\Users\koi\AppData\Local\Temp\claude\5071e751cc66269e\1760985e-ab7a-4534-abe9-4d0da34eaa12\scratchpad\fin\`

- `h7_zeilen.csv`: 276 Zeilen (Export 23.11.), Material, Menge, lokale Kosten, Konzernkosten je Einheit, Cost Source.
  **Mengeneinheit gibt es in Sales_All nicht**, Spalte leer. Muster: H703xx 185, H702xx 60 Zeilen (Texte „EN10204-2.1
  Werksbescheinigung“, „3.1 Abnahmepruefzeugnis“), E999xx 12; Konzernkosten 0,01 bis 0,03 CHF gegen 10 bis 60 CHF lokal.
  Vermutung: Platzhalter-STPRS bei Zeugnis-Materialien statt Mengeneinheit; mit P76 MBEW/MARA pruefen.
- `uk_code2.csv`: 277 UK-Artikel mit Code 2, 1'485 Zeilen, 1'535'415 GBP (1'698'104 CHF).
- `de_mapping.csv`: 1'011 TRDE-Zeilen aus `Mapping_2026-11-23.xlsx` (732 Vorschlag, 279 offen), mit lokalen Kosten aus
  Finance Details; Vorschlaege 5'754'140 EUR Umsatz, lokale Kosten 3'876'026 EUR; Verfahren: 705 Regel Typencode/Sachnummer,
  14 Regel Trafag-Nummer im Text, 13 Textaehnlichkeit.
- `in_lrd_code8.csv`: 5 Artikel, 153 Zeilen: DM000083 122 Z. 9'624'656 INR, DM000084 28 Z. 1'393'200, FA000028, FA000029,
  DM000354 je 1 Zeile.
- Ausserdem: `unklar_zeilen.csv` (131 Zeilen Topic 9), `italien_rest.csv` (6 Hausnummern, 7 Zeilen, 0 EUR).
- P76-Ordner `p76/`: um 12:05 lagen MAKT, MARA, MARC_1100, MBEW_1100 als CSV, aber noch kein `FERTIG.txt` *(ueberholt:
  `FERTIG.txt` 12:09, ausgewertet im Abschnitt „Auswertung P76“ unten)*.
- Export vom 24.11. lag um 12:05 noch nicht auf dem Server; alle Zahlen sind vom Export 23.11.

## Offene Punkte

1. Supplier Origin ohne „Last goods receipt“: vermutete Kette Audit-CSV ohne SupplierSource mit Produktiv-DB-Lesen bestaetigen (Freigabe Ingo), dann Code-Auftrag.
2. Entscheid zu Zeilen ohne Artikel (Topic 9) und die vier Fachfragen oben (Andreas).
3. PDF nachfuehren: Code-9-Text, Frage 5, Supplier-Origin-Tabelle, Indien „6 Artikel / 112 Zeilen / 9,09 Mio.“ (heute 5 / 122 /
   9,62), Paola-Punkt bei IT (Liste erledigt sich ueber den Wareneingang).
4. Indien 36 Topic-9-Zeilen in B1 pruefen (nur ueber den Server).
5. ~~H7-Zeilen mit den P76-Daten gegen MBEW-STPRS/PEINH und Mengeneinheit pruefen.~~ *(erledigt 12:15, Abschnitt unten: kein
   Preiseinheitsfehler, PEINH wird richtig angewendet.)*
6. DE-Mappings: Pruefliste in `Downloads\Mapping_DE_Vorpruefung_P76_2026-11-24.xlsx`; bestaetigen nur durch Ingo/DE. ~~Entscheid,
   ob die Faktorgrenze fuer DE bei 3 bleibt.~~ *(Entscheid Ingo 24.11.: Faktor 0,3 bis 8, Abschnitt „Import-Datei“ unten.)*
   Offen: Import der Datei `Downloads\Mapping_Import_DE_UK_sicher_2026-11-24.xlsx` durch Ingo.
7. UK-Typencode: ~~Bau erst nach Entscheid Ingo.~~ *(24.11.: kein Code, die 96 eindeutigen Treffer gehen als Mapping-Zeilen in
   die Import-Datei.)*
8. Indien Code 8: Kandidaten an Indien zur Bestaetigung; FA000028/29 sind Maschinen, kein Mapping.

## Auswertung P76 (Stand 2026-11-24 12:15)

Quelle: `p76/` (MARA 68'840, MBEW 1100, MARC 1100, MAKT D/E; gelesen 24.11. 11:50-12:04, README dort). Export 23.11. wie oben.
Skripte: `skripte/p76lib.py` (Laden, Typencode-Index), `p1_h7.py`, `p1b_peinh.py`, `p2_uk.py`, `p3_de.py`, `p5_in.py`,
`ref_faktor.py`, `de_kosten_check.py`.

**Typencode-Vergleich (gilt fuer 2, 3, 5):** Im Text ein Punkt-getrennter Code mit Familie aus 3-4 Ziffern (z. B.
`8252.84.2517.01.19.34.44.61`). Gegen MARA ohne Loeschvormerkung: `ZZTYP` ohne Leerzeichen, wahlweise ohne Null-Fueller
(`0000`), und `ZZTYP_F4`. „Ganzer Code“ = alle Glieder des Textcodes treffen; „Teilcode“ = erst nach Abschneiden hinterer Glieder.

### 1. H7 (Nebenbefund 272/276 Zeilen)

- Laut SAP-Agent hat H70313 in P76 STPRS 0,01 je 1 ST; kein Preiseinheitsfehler.
- **PEINH wird richtig angewendet**, gemessen an allen 74'169 Zeilen mit Konzernkosten TR AG im Export 23.11.: Bei CHF-Gesellschaften
  sind Konzernkosten je Einheit = STPRS/PEINH auf 1 % genau (50'727 Zeilen mit PEINH 1, 1'137 mit PEINH > 1). Bei EUR/GBP/USD/INR
  liegt das Verhaeltnis bei 0,8-1,25 (Median EUR 1,029, GBP 1,037, USD 1,063, INR 1,21), auch bei PEINH > 1. Vermutlich ein
  Kurseffekt der CHF-Rueckrechnung, nicht geprueft. **Keine einzige Zeile hat STPRS ohne Teilung durch PEINH** (Klasse „= STPRS“
  leer bei 268 Materialien mit PEINH 10/100/1000/…). 18 Zeilen (EUR 6, GBP 12) liegen ausserhalb 0,8-1,25.
- Die 276 Zeilen: PEINH 1 bei 214, PEINH 10 bei 60, PEINH 100 bei 2 Zeilen.
  - **Zeugnisse/Zertifikate: 245 Zeilen, 53'018 CHF Umsatz, 8 Materialien** (H70210, H70310, H70312-H70317; Text EN10204).
  - **Platzhalterpreis** (STPRS/PEINH hoechstens 0,05 CHF, kein Zeugnistext): 19 Zeilen, 21'127 CHF, 4 Materialien (E00624, E99999,
    H99999, R65104).
  - Uebrige: 12 Zeilen, 2'310 CHF, 7 Materialien (59544, C40226, E01830, F90188, H03702, H09020, H09035).
- Folgerung: Die Kosten sind formal richtig gerechnet; der Befund ist fachlich (Zeugnisse und Platzhalter mit Centbetrag in MBEW).
  Frage an Andreas: Zeugnisse wie Dienstleistung behandeln (Kosten 0 / ausnehmen) oder STPRS in CH pflegen lassen.

### 2. UK Code 2 (277 Artikel, 1'485 Zeilen, 1'535'415 GBP)

| Klasse | Artikel | Zeilen | GBP |
| --- | ---: | ---: | ---: |
| eindeutig (ganzer Code, genau ein Material) | 99 | 549 | 427'670 |
| mehrdeutig (ganzer Code, mehrere Materialien) | 14 | 75 | 28'584 |
| nur Teilcode, eindeutig | 10 | 36 | 170'590 |
| nur Teilcode, mehrdeutig | 2 | 2 | 921 |
| kein Treffer | 152 | 823 | 907'650 |

- Plausibilitaet: Faktor = lokale UK-Kosten CHF je Einheit / (STPRS/PEINH). Referenz: UK-Zeilen, die die Konzernkosten heute schon
  finden, haben Median 2,70 (P10 1,35, P90 5,99), weil UK zum Konzernpreis einkauft. Bei den 99 eindeutigen: Median 3,41;
  96 im Band 0,5-6 (536 Zeilen, 417'752 GBP), 1 ueber 6, 2 ohne Faktor; 97 Ziele aktiv in MARA und MARC 1100.
- Beispiele eindeutig: NAT400.0A -> 44979 (Faktor 1,35), NAT250.0A -> 45227, NAH400.0A -> 47150, ECT16.0A -> 46688, NAT25.0A -> 45229,
  NAT400.0AM -> 46565, ISNT11015 -> 901115 (ohne STPRS), NAT40.0A -> 45230, N10.0 -> 27356, NAE40.0A -> 47230.
- Beispiele mehrdeutig: NAT10.0A (44890/48776/55122), ECT10.0A (46600/59572), ECTN10.0A, NAT400.0V, FPT2.5A, AS40 (35808/I35808),
  A33 (35570/I35570), ECT0.4A, NAT25.0AM, MST9540.
- Beispiele nur Teilcode: 51078-MDM7910 -> 36005 (Faktor 16,7, falsch: die UK-Nummer beginnt mit Sachnummer 51078),
  DPS250.0PAP1 -> 48991, DPS10.0PAP1, DPS60.0PAP1, DPS40.0PAP1, EXP6, 53372.1.5"TRICLAMP, DPS25.0PAP1, EXPD3.4, DPC10.0PAP1.
- Kein Treffer: meist UK-eigene Nummern ohne Trafag-Code im Text (z. B. 131654 PTM/N/EX…, DVB32, PST4B16F4, 102568).
- Liste: `uk_typencode_vorschlag.csv` im Scratchpad (mit Texten).

### 3. DE-Vorschlaege (732)

*(Einteilung mit Faktor 0,3 bis 3; ueberholt durch die Neueinteilung mit Faktor 0,3 bis 8 im Abschnitt „Import-Datei“ unten.)*

Lokale Kosten = Median `Standard Cost CHF (per unit)` der TRDE-Zeilen 2025/2026 (DE-Kosten sind plausibel: Median 72 % des
Verkaufspreises). Regeln wie vorgegeben: **sicher** = Nummer (nach dem Schraegstrich oder als 5-6-stellige Zahl in der DE-Nummer)
oder ganzer Typencode zeigt eindeutig auf das Ziel, kein Typencode zeigt auf ein anderes Material, Ziel in MARA und MARC 1100 ohne
Loeschvormerkung, Faktor 0,3-3. **wohl falsch** = Ziel geloescht/fehlt, oder Typencode zeigt eindeutig auf ein anderes Material
ohne stuetzende Nummer, oder Faktor unter 0,1/ueber 10. **pruefen** = Rest.

| Klasse | Vorschlaege | Zeilen | EUR |
| --- | ---: | ---: | ---: |
| sicher | 186 | 940 | 3'572'473 |
| pruefen | 455 | 945 | 1'808'411 |
| wohl falsch | 91 | 2'282 | 373'257 |

- wohl falsch: 86 Ziele in MARA und MARC 1100 zum Loeschen vorgemerkt (meist alte Sachnummer hinter dem Schraegstrich, z. B.
  645.2502.402/42981, 414.1024.022/52388), 9 Faktor ausserhalb 0,1-10, 4 Typencode zeigt anders; darunter VV/VP (Verpackung,
  1'723 bzw. 346 Zeilen).
- pruefen: 443 nur wegen Faktor ausserhalb 0,3-3; 48 Typencode zeigt auf ein anderes Material bei passender Nummer (kurzer
  Code wie `904.2377.903` trifft ein altes Material); 4 ohne Faktor; 9 ohne exakte Nummer.
- **Faktor:** Median 3,89 (n = 722); DE kauft zum Konzernpreis. Mit Obergrenze 8 statt 3 waeren 382 weitere Vorschlaege (733
  Zeilen, 1'260'200 EUR) „sicher“. Entscheid Grenze: Ingo.
- Liste: `C:\Users\koi\Downloads\Mapping_DE_Vorpruefung_P76_2026-11-24.xlsx` (Blatt Übersicht, Blatt Liste mit Autofilter,
  sortiert nach Klasse und Umsatz). Nur Pruefhilfe, nichts bestaetigt.

### 5. Indien Code 8 (5 Artikel, 153 Zeilen)

| Artikel | Verkauf | Kandidaten Trafag AG (STPRS/PEINH CHF) | Begruendung |
| --- | --- | --- | --- |
| DM000083 GDM insulation assy (Kundenteilnummer im Text), 122 Z., 3'178 St., rund 31 CHF/St. | ein Kunde | C16421 Freilufthaube mit Fenster 87X6 belueftet / „foam cover with window“ (31,00); C14917 Freilufthaube mit Sichtfenster „31“ / „foam cover with window“ (29,35); F90179 Temperaturisolation „06“ / „thermal insulation“ (1,42) | Kein MAKT-Treffer fuer die Kundenteilnummer; „Insulation“ fuer GDM entspricht dem Schaumdeckel; Preisniveau passt zu C16421/C14917, F90179 nur nach Text. Schwach, Indien muss bestaetigen |
| DM000084 GDM insulation assy (anderes Kundenteil), 28 Z., 400 St., rund 36 CHF/St. | ein Kunde | wie DM000083 (C16421, C14917, F90179) | gleiches Bauteil, andere Kundennummer |
| DM000354 Rain cover X6 with thermal insulation (Kundenteilnummer im Text), 1 Z., 2 St., 22 CHF/St. | 1 Kunde | F90162 Wetterhaube plus Isolierschlauch / „rain cover and temperature insulation“ (4,70); C16421 (31,00); C14904 Freilufthaube fuer 87X6 mit Sichtfenster (19,00) | F90162 fast wortgleich auf Englisch; C14904/C16421 wegen „X6“ = Baureihe 87X6 |
| FA000028, FA000029 Welding machine (complete), je 1 Z., 303 CHF/St. | 1 Kunde | keiner | Anlagegut (Praefix FA), kein Trafag-Produkt; MARA kennt nur Schweissteile, keine Maschine. Kein Mapping, als Nebenerloes behandeln |

Keine der Indien-Nummern traegt einen Trafag-Typencode; ZZTYP lieferte keinen Treffer.

## Import-Datei Mapping DE/UK (Stand 2026-11-24 12:38, Auftrag Ingo „ja mach alles“, nichts importiert)

**DE neu eingeteilt mit Faktor 0,3 bis 8** (Entscheid Ingo 24.11.; die Tabelle in Abschnitt 3 oben mit Grenze 3 ist damit
ueberholt), Skript `skripte/p3_de_f8.py`, Datei `Downloads\Mapping_DE_Vorpruefung_P76_2026-11-24.xlsx` ueberschrieben, Vermerk im
Blatt Übersicht:

| Klasse | Vorschlaege | Zeilen | EUR |
| --- | ---: | ---: | ---: |
| sicher | 568 | 1'673 | 4'832'673 |
| pruefen | 73 | 212 | 548'211 |
| wohl falsch | 91 | 2'282 | 373'257 |

pruefen: 51 Typencode zeigt auf ein anderes Material bei passender Nummer, 11 Faktor ausserhalb 0,3-8, 4 ohne Faktor, 9 ohne exakte
Nummer. wohl falsch unveraendert.

**Importformat (Code im Worktree `C:\TMP\TrafagSalesExporter_rx2311`, `6503c1d`):** `MaterialMappingExcel.Parse` liest das Blatt
„Mapping“ (sonst das erste), Spalten ueber die Kopfzeile; Pflicht sind `TSC`, `Local Material`, `Group Material Number`, `Status`;
gelesen werden zusaetzlich Local Text, Text Trafag AG, Delivering Entity, Delivering Material Number, Method, Method Detail,
Confidence, Reviewed By (Rows, Net Sales, Effective, Reviewed At nicht). `MaterialMappingStore.ImportAsync` ist alles oder nichts:
abgelehnt werden unbekannter TSC (gegen `Sites`), fehlende Nummer, Status ausser offen/Vorschlag/bestaetigt/abgelehnt (auch
„bestätigt“, englisch), Delivering Entity ausser leer/TR AG/TR IT/TR IN, Confidence ausserhalb 0-100, doppelter Schluessel (TSC +
lokale Nummer ohne Leerzeichen und fuehrende Nullen). Delivering Entity mit Delivering Material Number ersetzt die Gruppennummer
nur fuer Ware von TR IT/TR IN (Paket B); fuer DE/UK bleibt sie leer. **Status „bestaetigt“ im Import bestaetigt die Zeile**
(Pruefer = angemeldeter Benutzer, Zeitpunkt = Import); bestehende Zeilen desselben Schluessels werden aktualisiert.

**Datei `C:\Users\koi\Downloads\Mapping_Import_DE_UK_sicher_2026-11-24.xlsx`** (Skript `skripte/build_import.py`): 664 Zeilen,
alle Status bestaetigt. TRDE 568 (Klasse sicher, Local Material, Methode und Sicherheit genau aus `Mapping_2026-11-23.xlsx`,
4'832'673 EUR); TRUK 96 (Typencode eindeutig, Ziel aktiv, Faktor 0,5-6; Methode „Hand“, Sicherheit 100; 417'752 GBP; alle 96 stehen
im Mapping-Export schon als offen bzw. 1 als Vorschlag und werden ueberschrieben). Nicht aufgenommen: UK mehrdeutig, Teilcode
(auch 51078-MDM7910), 1 Faktor ueber 6, 2 ohne Faktor. **Gegenprobe** `skripte/check_import.py` (Regeln wie Parse/ImportAsync,
TSC gegen `Sites` im lokalen DB-Abbild 12.11.): 664 gueltig, 0 Fehler, 0 doppelte Schluessel. Negativtest am Mapping-Export 23.11.:
der Pruefer meldet dort 617 Fehler (TSC TRES fehlt im lokalen Abbild), kann also rot werden.

**Wirkung nach dem Import:** Die Zeilen wirken bei der naechsten Berechnung, die die Mapping-Tabelle laedt: Sales_All beim naechsten
Export (`ExcelExportService.LoadMaterialMappings`), das Cockpit beim naechsten Neuaufbau (`ManagementCockpitService` Zeile 370;
ob ein gecachter Stand bis zur naechsten Datenaenderung stehen bleibt, ist nicht geprueft). Die Gruppennummer wird nur gesetzt, wo
die Quelle keine liefert; fuer DE ohne Lieferant fuehrt sie ueber den Werkstamm zu Intern/TR AG und Konzernkosten (Code 5 statt 1/6),
fuer UK zu Konzernkosten statt Code 2. Rueckgaengig: Zeilen auf der Seite ablehnen oder eine Datei mit Status abgelehnt importieren.
