# Last Change

Stand: 2026-07-28

Diese Datei ist fuer tokenarme RAG-Nutzung komprimiert.

## Aktueller Kurzstand

- BUCHUNGSDATUM SPANIEN GEFUNDEN 2026-07-28 (loest Andreas' Issue 6): Sage Spanien HAT ein
  Buchungsdatum — `FacturasTB.FechaAsiento` („asiento" = Buchungssatz), in der Stichprobe zu
  **100 % gefuellt** (3'788/3'788 Zeilen, 318 verschiedene Werte) und ein eigenstaendiges Datum,
  kein Duplikat von `FechaFactura` (233 Werte, andere Verteilung). Es fehlt im Dashboard, weil
  der Spanien-Export die **Lieferschein**-Tabellen liest (`CabeceraAlbaranCliente` +
  `LineasAlbaranCliente`, dort gibt es nur `FechaAlbaran`/`FechaFactura`/`FechaCreacion`/
  `FechaRegistro`) und die **Rechnungs**-Tabelle `FacturasTB` gar nicht joint. LOESUNG:
  `FacturasTB` im Skript `Export-SageSpainSalesCsv.ps1` joinen und `FechaAsiento` als Spalte
  ausgeben, danach im Spanien-Mapping auf `SalesRecord.PostingDate` mappen — Aenderung auf dem
  spanischen Sage-Server, nicht in der App. Offen: ueber welchen Schluessel gejoint wird
  (Rechnungsnummer/-serie/-jahr) und wie Gutschriften laufen. GUENSTIGER MOMENT, weil Santi
  den fehlenden Zeitraum ohnehin gerade manuell exportiert. Details:
  `docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md` §1.
- SPANIEN-EXPORTBEFEHL FUER SANTI 2026-07-28 (fehlender Zeitraum 2026-01 bis 2026-05):
  `.\Export-SageSpainSalesCsv.ps1 -ExportMode Range -FromDate "2026-01-01" -ToDate "2026-06-01"`.
  **`ToDate` ist EXKLUSIV** (README) — fuer „bis einschliesslich 31.05." muss `2026-06-01`
  stehen, sonst fehlt der 31. Mai. **Kein `-DateFilter` angeben**: ohne den Parameter filtert
  das Skript auf `FechaFactura` (richtig fuer historischen Nachtrag);
  `-DateFilter LineRegistrationDate` ist laut README nur fuer TAEGLICHE DELTAS gedacht und
  wuerde beim Backfill die falschen Zeilen liefern. Mit rclone in einem Schritt:
  `.\Run-SpainRangeExportAndUpload-AllInOne.ps1 -FromDate "2026-01-01" -ToDate "2026-06-01"`.
  Dateiname entsteht automatisch als `Spain_Sales_range_20260101_to_20260601.csv` und matcht
  damit `IsSpainSalesFile`. Quelle: `SageSpainExportPackage/SageSpainFinalExportPackage/README.txt`.
- SPANIEN LAENDERCODE-FIX VOLLSTAENDIG VERIFIZIERT 2026-07-28: Die Quelldatei hat 21
  verschiedene Laenderwerte, **alle 21 sind im neuen `NormalizeCountryCode`-Mapping abgedeckt**
  (ESPAÑA, BRASIL, MÉXICO, CHILE, PORTUGAL, PERÚ, COLOMBIA, ARGENTINA, GUATEMALA,
  ECUADOR (Inc.GALAPAGOS), EL SALVADOR, PARAGUAY, ESTADOS UNIDOS DE AMÉRICA, COSTA RICA,
  FRANCIA, REPÚBLICA DOMINICANA, ALEMANIA, INDIA, PANAMÁ, BOLIVIA, CHINA). Gegengeprueft:
  die Quelle hat zwar eine Spalte `CustomerCountryCode`, die enthaelt aber **numerische
  Sage-interne IDs** (108, 342, 303, 123, ...), KEINE ISO-Codes — ein Umschwenken auf diese
  Spalte waere also keine Alternative, das Namens-Mapping ist der richtige Weg. Encoding
  gegengeprueft: die CSV ist gueltiges UTF-8 (`ESPAÑA` = Bytes `C3 91` fuer Ñ), die
  Diakritika-Behandlung im Fix greift.
- BACKFILL UK/SPANIEN 2026-07-28 VORBEREITET (Datei gebaut, Upload+Import noch offen): Ziel ist,
  dass alle Laender ab 2025 Daten haben; fuer die Fruehphase existieren nur APP-EIGENE
  Exportdateien, weil die Standorte damals noch nichts geliefert haben. Zwei Dateien geprueft:
  `Sales_TRUK_2026-05-11.xlsx` und `Sales_TRSE_2026-05-20.xlsx`. ERGEBNIS: **UK ist echter
  Backfill, Spanien ist redundant.** UK: 1'868 eindeutige Belegschluessel, davon **0 in der DB,
  1'868 fehlen** (`invoice date` 2025 bei 1'881 von 1'882 Zeilen) — das sind die verlorenen
  UK-2025-Daten. Spanien: 4'315 Schluessel, davon **ALLE 4'315 bereits in der DB** — die Datei
  enthaelt nur 2025 (komplett vorhanden), die echte Luecke ist 2026 Jan-Mai und wird davon NICHT
  abgedeckt; Import waere wirkungslos. ZWEI FALLEN, die einen naiven „Datei in den Ordner
  legen"-Ansatz still scheitern lassen wuerden: (1) beide Dateinamen matchen
  `IsOwnExportOutputFile` (`^Sales_<TSC>_\d{4}-\d{2}-\d{2}$`, Selbstfuetterungs-Schutz vom
  2026-07-13) und werden vom Import IGNORIERT; (2) die Spanien-xlsx faellt zusaetzlich durch
  `IsSpainSalesFile` (verlangt `Spain_Sales*` + `.csv`). DRITTE, GEFAEHRLICHSTE FALLE BEWIESEN:
  das UK-Mapping rechnet `SageNetSales = amount * quantity`, aber die Exportspalte
  `Sales Price/Value` enthaelt den BEREITS berechneten Zeilenwert — nachgewiesen daran, dass sie
  in der Spanien-Datei bei allen 4'341 Zeilen identisch mit `Finance | Net Sales Actual` ist. Ein
  unveraenderter Reimport haette also ein zweites Mal mit der Menge multipliziert (faellt nicht
  auf, weil die Zahlen plausibel aussehen, nur zu hoch sind). LOESUNG: `.tmp_tools/BuildUkBaseFile`
  rechnet die Spalte auf den Stueckpreis zurueck (Zeilenwert/Menge, Menge bleibt unveraendert),
  Kontrollrechnung inkl. Gutschriften-Vorzeichenlogik stimmt EXAKT (395'605.82 vor und nach der
  Reimport-Simulation, Differenz 0.0000; 1 Zeile mit Menge 0 und Wert 0 belassen; 0 Zeilen mit
  Vorzeichenrisiko). Ergebnis: `C:\Users\koi\Downloads\UK_Backfill\TRUK_2025.xlsx`. DER NAME IST
  BEWUSST GEWAEHLT: `TRUK_2025` matcht den Selbstfuetterungs-Schutz nicht, wird aber von
  `TryParseAnnualSiteFileName` als Jahres-/Basisdatei erkannt (TSC-Token + Jahreszahl) und ist
  kein datiertes Delta (`ddMMyy_TRUK`). DAUERHAFTIGKEIT: ein direkter DB-Schreibvorgang waere beim
  naechsten UK-Standortexport weg (Manual-Import ersetzt je TSC); mit der Datei im Quellordner
  greift das Basis+Delta-Modell und 2025 ist bei JEDEM Lauf wieder dabei. ZU TUN: Datei nach
  SharePoint `Import/Finance/UK_B1` hochladen und dort liegen lassen, TRUK-Standortexport fahren.
  PLAUSIBILITAET OFFEN: 395'605.82 GBP fuer ein Jahr wirkt niedrig, ein belastbarer UK-2025-Sollwert
  existiert nicht (der alte `3'749'865` gilt laut Doku nicht mehr fuer UK) — Groessenordnung mit
  Andreas/UK gegenpruefen, die Datei kann selbst schon ein Teilstand gewesen sein. Details:
  `docs/FINANCE_BACKFILL_UK_ES_2026-07-28.md`.
- MBEW-ENDLOSSCHLEIFE GEFUNDEN UND BEHOBEN 2026-07-28 (Commit `a35c6d6`, deployed 18:04,
  `306/306` Tests): Ursache dafuer, dass `GroupStandardCosts` seit dem 2026-07-15 leer blieb.
  Die Doku sprach von einem „haengenden mbewSet-Read", die eigentliche Ursache war aber nie
  untersucht. AM PRODUKTIVSYSTEM GEMESSEN: `mbewSet` ignoriert `$top`, `$skip` UND `$orderby`
  gleichermassen — JEDE Anfrage liefert den vollen Bestand von `68'543` Zeilen / `124 MB` in
  ~28 s, unabhaengig von den Paginierungsparametern (fuenf Varianten getestet, alle identisch).
  Die Leseschleife brach erst ab, wenn eine Seite weniger als 1000 Zeilen hatte — bei 68'543
  Zeilen pro „Seite" konnte das NIE eintreten. Sie lief endlos und uebertrug in jeder Runde
  erneut 124 MB. Deshalb kehrte die Anreicherung nie zurueck und
  `PersistGroupStandardCostsAsync` wurde nie erreicht. FIX: eine einzige Anfrage ohne
  Paginierung (der Service liefert ohnehin alles), plus Abgleich der Zeilenzahl gegen `$count`
  mit Warn-Log bei Abweichung — sollte der Service kuenftig doch paginieren, bekaemen wir sonst
  stillschweigend nur einen Ausschnitt, genau die Fehlerart, die hier schon zweimal unbemerkt
  blieb. NACHSORGE: ZSCHWEIZ-Import anstossen, danach muss `GroupStandardCosts` gefuellt sein
  und in `Sales_All` (Blatt „Gruppenmarge Details") muessen TR-AG-Lieferantenzeilen
  `CostSource = Konzernkosten TR AG (MBEW-STPRS)` zeigen statt `Interner Standardpreis`.
- CH/AT-QUELLE AUF PRODUKTION UMGESTELLT 2026-07-28 18:06: `Sites.SapServiceUrl` fuer
  `ZSCHWEIZ` von `travt762` (TEST) auf `travp762` (PROD) gesetzt, im Wartungsfenster bei
  gestoppter App, mit vorheriger Sicherung
  (`trafag_exporter.db.before-travp762-switch-20260728.bak`, 313.9 MB) und Gegenprobe aus der
  Datenbank. Datenseitig vorher abgesichert: P76 liefert nach Ingos Report-Laeufen
  `Gjahr2025 = 30'642` (identisch zu T76) und `Gjahr2026 = 18'290` (gegen `9'864` auf T76,
  weil die Testdaten Mitte April enden), und `WAVWR_DC` ist auf P76 auch fuer 2025 gefuellt.
  ROLLBACK falls noetig: `SapServiceUrl` zurueck auf `travt762`, oder Sicherung einspielen.
  ACHTUNG: Der naechste ZSCHWEIZ-Import ersetzt die CH/AT-Zahlen mit dem Produktivstand —
  Mai bis Juli 2026 kommen dazu, bestehende Zahlen koennen sich aendern.
- ISSUE-LOG ANDREAS 2026-07-28 ABGEARBEITET, `294/294` Tests gruen, NOCH NICHT DEPLOYED:
  Alle sieben Punkte auf Produktivdaten geprueft — keiner ist ein Rechen-/Logikfehler.
  BEHOBEN IM CODE: „Customer Country code is not standardized". Befund war praeziser als der
  Titel: alle Nicht-ES-Gesellschaften liefern saubere ISO-2-Codes (65 verschiedene, keine
  Case-/Ziffernprobleme); die Inkonsistenz kommt AUSSCHLIESSLICH aus Spanien, das spanische
  Klartextnamen schreibt (`ESPAÑA` 3'815, `BRASIL` 227, `PORTUGAL` 215, `PERÚ` 202,
  `MÉXICO` 194, `ALEMANIA`, `FRANCIA`, `ESTADOS UNIDOS DE AMÉRICA`, ... 22 Werte). Fix: neue
  Value-Transformation `NormalizeCountryCode` analog zur vorhandenen `NormalizeCurrencyCode`
  (`Services/TransformationStrategies.cs`), DI in `Program.cs`, Beschreibung in
  `TransformationCatalog.cs`, zwei Seed-Defaults fuer `MANUAL_EXCEL` (CustomerCountry +
  SupplierCountry) in `DatabaseSeedService.cs`, Tests in `TransformationStrategiesTests.cs` +
  `DatabaseInitializationServiceTests.cs`. Vergleich ohne Diakritika (`PERÚ` = `PERU`).
  DESIGN-ENTSCHEID: unbekannte Klartextwerte bleiben UNVERAENDERT stehen statt geraten/geleert
  zu werden, damit Mapping-Luecken sichtbar bleiben. WIRKSAM ERST NACH DEPLOY + ES-REIMPORT
  (Transformation greift beim Import, schreibt bestehende Zeilen nicht rueckwirkend um).
  NEU GEFUNDEN, nicht von Andreas gemeldet: TRDE hat `CustomerCountry` bei ALLEN 7'167 Zeilen
  leer (Mapping-, kein Normalisierungsproblem) — passt zum DE-Supplier-Befund (dort ebenfalls
  0 von 7'167); Alphaplan-Exportspalten gemeinsam klaeren. PRAEZISIERT: Issue „Posting Date
  fehlt bei TR ES" betrifft **5'478 von 5'478** ES-Zeilen (100 %), nicht nur einige; die
  Fallback-Kette `PostingDate`->`InvoiceDate` rettet die Jahreszuordnung, der echte Defekt
  sind **229 Zeilen ohne jedes Datum**, die aus allen Auswertungen herausfallen. BEWUSST NICHT
  gefixt: `PostingDate = InvoiceDate` waere eine fachliche Annahme (Buchungs- vs.
  Fakturadatum) und wuerde die 229 Zeilen ohnehin nicht retten. Ausgefuelltes Log mit Owner/
  Naechster-Schritt je Punkt: `docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md`.
- P76-REPORT GELAUFEN 2026-07-28, EIN SCHRITT FEHLT NOCH: Ingo hat `Z_TRAFAG_DACH_EXPORT` auf
  P76 fuer Jahre ab 2026 ausgefuehrt. Ergebnis live gemessen — die Produktion ist jetzt die
  BESSERE Quelle: OData Gesamt P76 `48'932` vs T76 `40'506`, **Gjahr2026 P76 `18'290` vs T76
  `9'864`** (T76 endet Mitte April, P76 reicht bis zum aktuellen Tag, Stichprobe
  `FKDAT = 20260728`). `NETWR_DC`/`WAVWR_DC` in den 2026er P76-Zeilen korrekt gefuellt.
  RESTBEFUND: **2025 hat auf P76 keine Kostenbasis** — `WAVWR_DC` = 0.00 in ALLEN 2025er
  Zeilen, waehrend T76 fuer dieselben Belege Werte hat (9'870.85 / 1'081.48 / 4'540.30).
  Ursache genau wie im Report-Kopfkommentar beschrieben: die 2025er P76-Zeilen stammen aus
  einem aelteren Lauf vor Einfuehrung von `WAVWR_DC`, und der UPSERT ergaenzt neue Felder nur
  bei erneutem Lauf ueber dieselben Zeilen. Der Lauf „ab 2026" hat 2025 nicht angefasst.
  NOCH ZU TUN: Report auf P76 fuer `s_gjahr = 2025` laufen lassen (BUKRS 1100 + 1200) — erst
  danach ist die URL-Umstellung unbedenklich, sonst waere 2026 gut, aber die Gruppenmarge im
  Referenzjahr 2025 ohne Kostenbasis. WIE WEIT ZURUECK? NICHT bis 2022: `ExportSettings.DateFilter`
  = `2025-01-01`, die App importiert nichts vor 2025, und `CentralSalesRecords` enthaelt nur
  2025 (58'353) und 2026 (26'200). Ein Lauf 2022-2024 wuerde ~100k Zeilen schreiben, die nie
  gelesen werden, und nur Laufzeit auf Produktiv-SAP kosten. Nebenbefund bestaetigt: der
  NETWR_HC-Faktor-100-Bug lebt auf BEIDEN Systemen weiter (`NETWR_DC 19'000.00` vs
  `NETWR_HC 161.50`), C#-Kompensation greift, SAP-Fix weiter offen. Details:
  `docs/FINANCE_CHAT_2026_LUECKE_ROOTCAUSE_2026-07-28.md`.
- ESKALATION AUS ITALIEN 2026-07-28: Nach Paolas Antwort kam eine Mail von uebergeordneter
  Stelle (Bezug auf VARONE als B1-Partner, Paolas Arbeitslast, Area Sales Manager) mit der
  Bitte, die Moving-Average-Umstellung fuer Trafag Italia **erst ab 2027** zu starten. Fuenf
  Vorbehalte: (1) VARONE-Implementierungskosten nicht budgetiert, (2) Arbeitslast Paola /
  moegliche manuelle Importe, (3) Verifikation des neuen Bestandswerts, (4) Auswirkung auf die
  TRIT-Marge quantifizieren, (5) neue interne Prozesse mit Area Sales Manager definieren.
  WICHTIG — DAS BLOCKIERT DAS REPORTING NICHT: der von Andreas freigegebene Weg
  (`INV1.StockPrice`, Belegebene) funktioniert unabhaengig von der Bewertungsmethode (97 %
  Fuellgrad unter heutiger Chargenbewertung). Andreas kann 2027 zustimmen, ohne im Reporting
  etwas zu verlieren; die MA-Umstellung bleibt ein Bilanzierungs-/Governance-Thema. Zu deren
  Punkt 4 koennen wir beitragen (wir haben die tatsaechlichen Chargenkosten je Material) —
  dabei aber Bestandsbewertung/bilanzielle COGS klar von der Reporting-Marge trennen, sonst
  reden beide Seiten von verschiedenen Zahlen. Terminentscheid liegt bei Andreas/HQ, kein
  neuer Aktionspunkt fuer Ingo. Details:
  `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` Abschnitt 5e.
- ROOT CAUSE CH/AT-2026-LUECKE GEFUNDEN 2026-07-28: Der SAP-Report **`Z_TRAFAG_DACH_EXPORT`**
  wurde in der PRODUKTION (P76) **nie fuer 2026 ausgefuehrt**. Beweiskette (alles live,
  read-only): (a) OData-`$count` mit den App-Credentials (`POWERBI`): T76 liefert Gesamt
  `40'506` / Gjahr2025 `30'642` / **Gjahr2026 `9'864`**, P76 liefert Gesamt `30'642` /
  Gjahr2025 `30'642` / **Gjahr2026 `0`** — das TESTsystem hat mehr Daten als die Produktion.
  (b) RFC-Read der Z-Tabelle: `ZSCHWEIZ` hat auf P76 2025er Zeilen, aber KEINE 2026er; auf T76
  beide. Der Report schreibt per UPSERT dorthin (Zeile 494 `MODIFY zschweiz`), OData liest
  daraus. (c) KEIN fehlender Transport: der Quelltext ist auf T76 und P76 **byte-identisch**
  (577 Zeilen, `diff` leer; P76 zuletzt geaendert KOI 2026-07-22, T76 2026-07-16) — die
  Produktion hat also den vollen WAVWR/STPRS-Funktionsumfang, er wurde dort nur nicht
  ausgefuehrt. NAMENSFALLE: das Programm heisst im System `Z_TRAFAG_DACH_EXPORT`; der Name
  `Z_TRAFAG_SCHWEIZ_EXPORT` (lokale Datei `docs/abap/` und `REPORT`-Kopf) existiert in KEINEM
  der beiden Systeme. FIX: Report auf P76 fuer **2025 UND 2026** laufen lassen (BUKRS 1100 +
  1200) — 2025 mit, weil laut Kopfkommentar `WAVWR_DC` bei bestehenden Zeilen aus aelteren
  Laeufen sonst 0 bleibt (UPSERT ergaenzt neue Felder nur bei erneutem Lauf). Sicher, weil der
  Report NUR `MODIFY` macht, kein `DELETE` auf `ZSCHWEIZ`, und wiederholbar ist. Vorher in SE11
  pruefen, dass `WAVWR_DC`/`STPRS_HC` existieren. ERST DANACH darf `Sites.SapServiceUrl` auf
  `travp762` umgestellt werden — vorher wuerde die Umstellung die vorhandenen 9'864 2026er
  Zeilen ENTFERNEN. ZURUECKGEZOGEN: die tags zuvor hier notierte Empfehlung „URL-Wechsel behebt
  zwei Probleme" ist widerlegt; ebenso war die „Korrektur" des 2026-07-14-Eintrags falsch —
  dessen Aussage (`Gjahr eq '2026'` liefert nichts) trifft fuer P76 weiterhin exakt zu.
  `GroupStandardCosts` (0 Zeilen) bleibt ein SEPARATES Problem (haengender `mbewSet`-Read).
  Details: `docs/FINANCE_CHAT_2026_LUECKE_ROOTCAUSE_2026-07-28.md`.
- ANDREAS' ROTE MARKIERUNGEN GEPRUEFT 2026-07-28 (`docs/Bild.png`, Pivot TSC/Jahr/Monat) — alle
  vier bestaetigt, KEINER ist ein Rechen-/Logikfehler, alle sind Datenherkunft/-vollstaendigkeit:
  (1) CH/AT 2026 ab Mai fast leer (TRCH: Jan 2'662, Feb 2'616, Mrz 2'643, Apr 1'409, **Mai 47,
  Jun 43, Jul 87**; TRAT analog) — URSACHE IN DER PRODUKTIV-DB NACHGEWIESEN: `Sites.SapServiceUrl`
  fuer `ZSCHWEIZ` = `http://travt762.sap.trafag.com:8000/...`, also das TEST-System T76 statt
  `travp762`/Prod. Testdaten enden Mitte April 2026, daher der Schnitt. WICHTIGE KORREKTUR: Der
  Eintrag vom 2026-07-14 („CH/AT sieht das laufende Jahr nicht, `Gjahr eq '2026'` liefert
  nichts, Dashboard zeigt 0") ist UEBERHOLT — 2026er Daten kommen durch, Jan bis Mitte Apr sind
  vollstaendig; es ist kein `Gjahr`-Filterproblem, sondern ein Testsystem-Datenstandsproblem.
  EIN FIX BEHEBT ZWEI PROBLEME: `SapServiceUrl` auf `travp762` umstellen holt die fehlenden
  CH/AT-Monate UND befuellt endlich `GroupStandardCosts` (dessen Root Cause am 2026-07-16
  dieselbe war). VORSICHT: veraendert produktive Finance-Zahlen rueckwirkend, vorher mit
  Andreas/Marco abstimmen und DB sichern. (2) ES 2026 Jan-Apr = 0 Zeilen, Mai nur 35 — der
  Spanien-Range-Export beginnt erst am 28.05.2026, Jan-Apr wurde nie exportiert (kein Bug,
  fehlender Export). (3) UK 2025 = 0 Zeilen, UK hat nur 2026-01 bis 2026-07 (1'088) — fehlende
  Lieferung; Achtung, frueher dokumentierte UK-2025-Analysen (Restdifferenz -5'261.91 GBP)
  beruhen auf Daten, die aktuell NICHT in der DB sind. (4) Nebenbefunde: 229 ES-Zeilen und 6
  UK-Zeilen ohne Datum fallen aus jeder Jahres-/Monatsauswertung; TRCH hat je 1 Zeile mit
  Belegmonat 2026-09 und 2026-10 (zukunftsdatiert, Testsystem-Artefakt). Details:
  `docs/FINANCE_DATENLUECKEN_ANDREAS_2026-07-28.md`.
- PRIORITAETSUMKEHR 2026-07-28 (zwei Produktivbefunde, wichtig fuer die Wochenplanung):
  (1) `GroupStandardCosts` hat produktiv WEITERHIN `0` Zeilen — das am 2026-07-15 gebaute und
  deployte TR-AG-Konzernkostenfeature ist 12 Tage spaeter unveraendert wirkungslos (Root Cause
  vom 2026-07-16 nie behoben: `Sites.SapServiceUrl` fuer ZSCHWEIZ auf `travt762`/Test statt
  `travp762`/Prod + haengender `mbewSet`-Read). Folge: TR-AG-gelieferte TRIT-Zeilen nutzen
  weiter den IC-Verrechnungspreis als Kostenbasis und sind wegen vorhandener Supplier-Felder
  NICHT maskiert — sie zeigen also eine Marge auf fachlich falscher Basis, was schlechter ist
  als ein sichtbares „-". (2) Die fuer diese Woche zugesagte TR-IT/TR-IN-Verlinkung betrifft
  nur `443` von `84'788` Zeilen (TR IT 129 = 0.15 %, TR IN 314 = 0.37 %). Hebelvergleich:
  Supplier-Regel entscheiden = 63'008 Zeilen (74 %), `GroupStandardCosts` reparieren = 7'163
  Zeilen (8.4 %), TR-IT/TR-IN bauen = 443 Zeilen (0.5 %). EMPFEHLUNG: erst Andreas-Entscheid
  zur Supplier-Regel einholen, dann das bereits gebaute TR-AG-Feature aktivieren, dann
  DE-Supplier-Spalten pruefen — die TR-IT/TR-IN-Anbindung ist fachlich richtig, aber der
  kleinste Hebel und sollte gegenueber Andreas ggf. neu terminiert werden. Details:
  `docs/FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28.md` Abschnitte 7-8.
- SUPPLIER-LUECKE AUF PRODUKTIVDATEN QUANTIFIZIERT 2026-07-28 (Andreas' Zahl bestaetigt,
  groesster Hebel im Gruppenmarge-Thema): Ausgewertet wurde die PRODUKTIVE
  `trafag_exporter.db` vom Server (Stand 2026-07-27 13:16, read-only Kopie, WAL war 0 Bytes).
  `69'919` von `84'788` Zeilen (82.5 %) haben ALLE DREI Supplier-Felder leer — Andreas'
  „60-79 Tsd." trifft also zu, kein Zaehlfehler. KERNBEFUND: die drei Felder sind ausnahmslos
  GEMEINSAM leer (je TSC gilt `no_number` = `no_name` = `no_country` = `all_three_empty`, keine
  einzige Zeile mit nur einem fehlenden Feld) -> es ist ein Mapping-/Quellenproblem, kein
  Pflegeproblem. Strukturell 100 % leer: CH (39'043), DE (7'167), ES (5'478), AT (1'463),
  UK (1'088). Teilweise gefuellt nur die B1-Laender: IT 71 % gefuellt (13'921 Zeilen),
  IN 12 %, FR 5 %, US 0.4 %. DER PREIS DER REGEL: `63'008` Zeilen (74 % aller Zeilen) haben
  eine verwertbare Kostenbasis, zeigen aber wegen `Lieferant unklar` keine Marge — davon
  TRCH 37'680 und TRAT 1'462, d. h. die am 2026-07-16 hergestellte CH/AT-Kostenbasis
  (Fuellgrad 96.5 %/99.9 %) wirkt sich auf KEINER EINZIGEN Zeile aus. Damit ist die offene
  Fachfrage an Andreas vom 2026-07-17 (CH/AT per Regel als eigene Lieferkategorie werten?)
  bezifferbar und hat deutlich mehr Hebel als die TR-IT-/TR-IN-Kostenanbindung. NEUE OFFENE
  FRAGE: TRDE hat produktiv 0 von 7'167 Zeilen mit Lieferant, in einer Dev-Momentaufnahme vom
  2026-07-02 waren es 1'764 mit `Trafag AG` — liefert der Alphaplan-Export die Spalten
  `Lieferanten Nummer`/`Name Lieferant`/`Land Lieferant` noch? (Dev-Datei ist kein frueherer
  Produktivstand, daher NICHT als Rueckschritt belegt.) Details:
  `docs/FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28.md`.
- TR IN AUF PRODUKTIVDATEN BELEGT 2026-07-28: Der fuer TR IT freigegebene Belegebenen-Weg
  traegt fuer Indien nachweislich: `6'934` von `6'973` Zeilen mit Kosten (99.4 %) und
  `1'430` von `1'434` Materialien (99.7 %). Ein Artikelstamm-/Bewertungsmethoden-Check ist
  fuer die UMSETZUNG damit nicht noetig. Die Bewertungsmethode in Indiens B1 bleibt aber
  ungeprueft: `20.197.20.60:30015` ist vom Entwicklungsrechner nicht erreichbar, und eine
  Remote-Ausfuehrung auf dem Produktivserver war nicht moeglich (Berechtigungsebene blockierte
  `Invoke-Command`; Share ist lesbar, enthaelt aber nur die App-DB, keine B1-Stammdaten).
  Nachholen nur noetig, falls TR IN analog zu Paola/TR IT auf Moving Average angesprochen
  werden soll. WICHTIG ZUR BEGRIFFSKLARHEIT: Indien ist fachlich SAP B1 und laeuft nur
  historisch unter dem irrefuehrenden Quellsystem-Code `SAGE` — die B1-Tabellen
  (`OITM`/`OITW`/`OADM`) existieren dort also.
- TERMINWARNUNG B1-UPGRADE 2026-08-03 (gemeldet 2026-07-28 von Paola/TR IT, war vorher NICHT
  angekuendigt): Go-Live eines B1-Upgrades ueber ALLE Tochtergesellschaften, Final Tests
  2026-08-02. Betrifft direkt die taegliche Finance-Datenstrecke: `HanaQueryService` liest fuer
  FR (`fr01_p`), IT (`it01_p`), US (`us01_p`) und ueber denselben Adapter IN (`TRAFAG_LIVE`) aus
  `OINV`/`INV1`/`ORIN`/`RIN1` + `OADM`/`OITM`/`OITB`/`OCRD`/`CRD1`/`OOND`/`OSLP`/`ORDR`.
  Risiken: (1) Downtime am Wochenende -> Importfehler + Heartbeat-Luecken (dann
  erwartungskonform, kein Datenverlust); (2) Schema-/Feldaenderungen der neuen B1-Version,
  besonders kritisch `INV1.StockPrice`, weil das ab jetzt die TR-IT-Konzernkostenquelle ist,
  ausserdem `OITM.EvalSystem` und `OADM.MainCurncy`; (3) alle `EvalSystem`-Zahlen unten sind ein
  Stand VOR dem Upgrade. NACHSORGE ab 2026-08-03: Importlaeufe FR/IT/US/IN pruefen
  (`ExportLogs`/`AppEventLogs`, Daten-Heartbeat), `StandardCost`-Fuellgrad je TSC gegenpruefen,
  `EvalSystem`-Verteilung fuer `it01_p` neu erheben (Werkzeug `.tmp_tools/HanaQ`).
- TR-IT-KONZERNKOSTEN GEKLAERT 2026-07-27/28 (Analyse + Fachentscheid, Umsetzung noch OFFEN):
  Der seit 2026-07-15 dokumentierte Befund "TR IT hat in SAP B1 keine Standardkosten" war in der
  SCHLUSSFOLGERUNG falsch. Live-Read gegen `it01_p` bestaetigt die Zahlen (`OITM.PrdStdCst` = 0
  bei allen ~40'478 Artikeln, `OITW.AvgPrice` = 0 bei allen 1'902'456 Lagerzeilen), aber die
  Ursache ist die Bewertungsmethode: bei aktiven Lagerartikeln laufen 31'600 von 31'902
  (99.1 %) auf Chargen-/Seriennummernbewertung (`OITM.EvalSystem='B'`, empirisch bestaetigt ueber
  100 % Korrelation mit `ManBtchNum`), nur 296 (0.9 %) auf Moving Average. Dabei fuehrt B1 die
  Kosten je Charge, nicht im Artikelstamm - die Felder werden sich NIE fuellen, ein monatlicher
  Export daraus liefert dauerhaft Nullen. GEGENBEFUND: auf Belegebene ist `INV1.StockPrice` fuer
  2'019 von 2'082 in 2026 verkauften Materialien gefuellt (97.0 %). Andreas hat am 2026-07-27
  freigegeben, diesen Belegebenen-Weg als TR-IT-Kostenbasis zu nutzen (gleiches Prinzip wie
  `VBRP-WAVWR` bei CH/AT), keine kalkulierte Groesse noetig, keine monatliche Datenlieferung.
  TECHNISCH NOCH NICHT UMGESETZT: `GroupStandardCostAreas.ByEntity` enthaelt weiterhin nur
  `TrAg`; TR IT/TR IN fallen dadurch still auf die lokale Kostenbasis zurueck. Ausserdem
  transportiert der Kommentar in `Models/GroupStandardCost.cs` Zeile 12-16 noch die alte,
  ueberholte Schlussfolgerung. Details: `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md`.
- MOVING-AVERAGE-UMSTELLUNG TR IT: WARTET AUF PAOLA BIS ENDE AUGUST 2026 (Stand 2026-07-28):
  Paola/TR IT bestaetigt, dass die Umstellung Charge -> Moving Average fuer die ~31'600 Artikel
  als Massenupdate technisch machbar ist. OFFEN bleibt genau Andreas' Cost-Run-Frage: rechnet SAP
  den Durchschnittspreis nach der Umstellung automatisch fort, oder braucht der Bestand eine
  einmalige manuelle Bewertungsaktion? Das klaert sie mit ihrem SAP-Technikteam. Zeitplan: B1-
  Upgrade Go-Live 2026-08-03, danach ca. 2026-08-03 bis 2026-08-17 Ferien, Beurteilung Ende
  August. Sie haelt eine Analyse parallel zum Upgrade fuer nicht ratsam und fragt zurueck, ob
  Ende August fuer Andreas passt (ANTWORT AN PAOLA STEHT NOCH AUS). Unkritisch fuer die
  Gruppenmarge: der freigegebene Belegebenen-Weg funktioniert unabhaengig von der
  Bewertungsmethode; die MA-Umstellung ist Konzern-Richtlinienkonformitaet, kein Blocker.
- MAGNETIC SENSE ENDGUELTIG GEKLAERT 2026-07-27: Keine vierte Konzern-Standardkosten-Tabelle
  noetig. Datenbefund: `SupplierName LIKE '%MAGNET%'` -> 0 Zeilen, `CustomerName LIKE '%MAGNET%'`
  -> 101 Zeilen (alle TRDE). Magnetic Sense ist ausschliesslich Kunde, nie Lieferant. Andreas
  bestaetigt: "Fuer Magnetic Sense benoetigen wir aus meiner Sicht keine Daten." Es bleibt bei
  den drei Gesellschaften TR AG / TR Italien / TR Indien. Achtung Begriffsfalle: auf der
  KUNDENseite ist Magnetic Sense weiterhin ein IC-Marker (`FinanceIntercompanyRule`) und wird
  fuer DE per `FinanceRuleEngine` ausgeschlossen - das ist ein anderer Mechanismus als die
  Lieferantenklassifikation der Gruppenmarge.
- TR IN LIVE-CHECK WEITER BLOCKIERT 2026-07-28: Zugriff auf die Indien-Quelle
  (`20.197.20.60:30015`, Schema `TRAFAG_LIVE`) scheitert vom Entwicklungsrechner erneut mit
  Timeout (`rc=10060`), wie schon am 2026-07-15 - kein Netzwerkzugang. Aus dem lokalen Snapshot
  ist aber bekannt: TRIN-Zeilen haben zu 99.5 % (6'349/6'384) einen Kostenwert, interne
  "Trafag AG"-Lieferantenzeilen zu 99.2 % - der Belegebenen-Weg ist dort also aussichtsreich und
  braucht fuer die Umsetzung KEINEN Artikelstamm-Check. Ein `EvalSystem`-Check fuer Indien waere
  nur fuer eine Paola-analoge Anfrage an TR IN noetig und braucht dann VPN-/Firewall-Freigabe.
- NEUE DOKU/WERKZEUGE 2026-07-27/28: (1) `docs/rag/FINANCE_FORMELN.md` - kompakte, code-
  verifizierte Referenz WIE gerechnet wird (Datenfluss, Formel je Land, drei getrennte
  Waehrungskonzepte, Marge/Standardkosten, und die DREI verschiedenen
  Trafag/Magnetic-Sense/GFS-Filtermechanismen, die man leicht verwechselt). (2)
  `docs/FINANCE_GRUPPENMARGE_PROZESSFLUSS_2026-07-27.svg` - Filterkette pro Verkaufszeile als
  Entscheidungsfluss fuer Nicht-Finanzler. (3) `docs/TRIT_B1_VALUATION_EXPLAINED_2026-07-28.svg`
  - englische Erklaergrafik fuer Paola/TR IT. (4) `.tmp_tools/HanaQ` - generischer read-only
  HANA-Abfrager, siehe `docs/RAG_ROUTER.md` Abschnitt "Werkzeug: HANA-Direktzugriff (HanaQ)".
- ZZPRDAT/PP-THEMA - ALTLOESUNG IST TOT 2026-07-27 (SAP, eigenes Thema, nicht Dashboard):
  Quelltext-Read per SapProbe zeigt, dass die Schreiblogik des `PPCO0012`-Exits (CMOD `ZPP00012`,
  Transport `T76K911110`) KOMPLETT AUSKOMMENTIERT ist - `ZXCO1U11` und `ZXCO1U12` Zeile fuer
  Zeile mit `"`, `ZXCO1O01` leerer Rumpf, `ZXCO1I01` nur ein No-op
  (`MOVE-CORRESPONDING ci_aufk TO ci_aufk`). Es gibt also KEINEN aktiven Code-Pfad, der
  `AUFK-ZZPRDAT` schreibt - das Feld bleibt nicht "manchmal", sondern IMMER leer. Ein Live-Test
  mit Testauftrag war dadurch unnoetig. Folge: der Altcode taugt NICHT als Referenz fuer die
  BAdI-Neuimplementierung (`WORKORDER_UPDATE`), diese muss komplett aus den Anforderungen
  abgeleitet werden. Ausserdem widerlegt: Marcos Referenzfall `1214608` zeigt live in P76
  `ZZPRDAT = 00000000` (nicht 20.11.2025) bei `GLTRP = 08.01.2026`; `CDPOS` liefert fuer das Feld
  weder in T76 noch P76 Aenderungsbelege. Wahrscheinlichere Quelle einmalig gefuellter Werte:
  Adils Kopierprogramm (§10). Details: `saptasks/zzprdat-kontext.md`.
- DOKU-LEHRE 2026-07-28 (Arbeitsweise, gilt allgemein): Zwei Fehler dieser Session sind
  dokumentiert, weil sie sich wiederholen koennen. (1) Ein Nullwert ohne notierte Ursache ist
  kein Befund, sondern eine offene Frage - der 2026-07-15-Eintrag "TR IT hat keine Kosten" wurde
  dreimal zitiert (Doku, `Models/GroupStandardCost.cs`, `FinanceStdCostTodoExcel`), stammte aber
  aus EINER unbelegten Quelle ohne Materialnummern/Zeilenzahlen und fuehrte zur falschen
  Schlussfolgerung. (2) Prozentzahlen brauchen eine genannte Grundgesamtheit - eine erste
  Auswertung nannte "97.8 % Chargenbewertung" und "nur 40 % der MA-Artikel haben AvgPrice"; beide
  Zahlen waren durch Nicht-Lagerartikel verzerrt, korrekt sind 99.1 % bzw. 75.7 % auf Basis
  aktiver Lagerartikel. Die zweite Korrektur entzog einer geplanten Aussage gegenueber TR IT die
  Grundlage - rechtzeitig gemerkt, bevor sie rausging.

- IIS-HOSTING ROLLBACK AUF OUTOFPROCESS 2026-07-24, DEPLOYED (DLL 24.07.2026 14:39, Port 443
  offen, HTTP 401 = App oben; Commit `410cf70`, gleicher Deploy enthaelt auch den Ladebalken-
  Commit `f7ef248`): Ca. 1 Stunde nach dem Wechsel auf `inprocess` (Commit `4d2c6d3`) meldete Ingo
  "schleichend immer langsamer ueberall" (nicht nur Einkauf - auch Finance/HR). "Schleichend"
  (graduell, keine Stufenaenderung) spricht eher fuer aufgebaute Ressourcen-/Speicherlast (z.B.
  angesammelte getrennte SignalR-Circuits durch die zuvor haeufigen Reconnects) als fuer eine reine
  Konfigurationsregression - eine "inprocess ist grundsaetzlich schneller"-Annahme widerspricht
  einer echten Verlangsamung durch den Hosting-Wechsel selbst. Vorsichtshalber zurueck auf
  `outofprocess` (der vorher ueber Wochen stabile Zustand) - das erzwingt gleichzeitig einen
  sauberen Prozessneustart, der angesammelten Druck so oder so beseitigt. URSACHE DER
  SCHLEICHENDEN VERLANGSAMUNG NOCH NICHT ABSCHLIESSEND GEKLAERT - falls es nach diesem Neustart
  wieder auftritt (unabhaengig vom Hosting-Modus), ist es kein Hosting-Problem, sondern ein
  echter Leak/Ressourcenaufbau im Applikationscode oder in Blazor-Circuit-Handling - dann genauer
  untersuchen (z.B. Circuit-Retention-Einstellungen, SQLite-WAL-Wachstum, GC-Metriken).
  NACHBEOBACHTEN: Ingo soll melden, ob die Verlangsamung nach diesem Neustart weg ist/wiederkehrt.
- IIS-HOSTING ZURUECK AUF INPROCESS 2026-07-24, DEPLOYED (DLL 24.07.2026 13:20, Port 443 offen,
  HTTP 401 auf `/einkauf/spend` UND `/diag.txt` = App oben; Commit `4d2c6d3`): Ingo meldete, dass
  die Seite beim Wechsel zwischen Reitern haengt ("Attempting to reconnect to the server X of 8"),
  ueberall in der App, nicht nur Einkauf. Ursache im Git-Verlauf gefunden: `web.config` stand seit
  20.05.2026 auf `hostingModel="outofprocess"` - das war laut damaligem Commit NUR eine
  Diagnose-Massnahme fuer einen IIS-500-Startfehler beim allerersten Deploy, wurde aber nie
  zurueckgestellt. `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` ging faelschlich davon aus,
  die App liefe bereits auf `inprocess`. Outofprocess bedeutet ein zusaetzlicher Reverse-Proxy-Hop
  IIS -> separater `dotnet.exe`-Prozess fuer jeden Request inkl. der SignalR-WebSocket-Verbindung
  des Blazor-Circuits - deutlich instabiler fuer Dauerverbindungen, passt zum gemeldeten Symptom.
  Haengt vermutlich auch mit dem Vorfall vom 07.07.2026 zusammen (killed `dotnet`-Prozess startete
  nicht automatisch neu, kein AlwaysRunning). Fix: `web.config` zurueck auf `hostingModel="inprocess"`
  (Ursprungszustand vom 19.05.2026). ROLLBACK falls noetig: Zeile zurueck auf `outofprocess`,
  redeployen (2 Minuten, keine DB-/Codeaenderung). Nach Deploy verifiziert: stdout-Log des alten
  Out-of-Process-Workers zeigt sauberen Shutdown, kein Absturz; kein 500.30/502.5 (typisches
  In-Process-Ladeversagen), stattdessen sauberes 401 wie gewohnt.
- WARENGRUPPEN-TEXT (T023T) 2026-07-24, `277/277` Tests gruen, DEPLOYED (DLL 24.07.2026 12:37,
  Port 443 offen, HTTP 401 = App oben/Auth-Challenge; Commit `c44ae28`): Ingo hat den SAP-Export
  T023T (Sprache DE, ~72 Codes, WGBEZ auf 20 Zeichen abgeschnitten) direkt als Liste geliefert -
  neue Klasse `PurchasingMaterialGroupTextCatalog` (statisches Dictionary, kein DB-Upload noetig)
  loest Matkl/MaraMatkl-Codes auf "Code - Text" auf (z.B. "20.05.00 - Baelge"), unbekannte/
  kuenftige Codes bleiben roher Code (kein Verschwinden). Verdrahtet an allen 6 Stellen, die einen
  WG-Code anzeigen: Volumen-nach-Warengruppe-Chart, Top-Warengruppe-KPI, Lieferant/WG/Jahr-
  Drilldown, Kaskade + Region-Kuchen im Spend-Aufriss, Live-Vorschau ohne Cache. Stale UI-Hinweis
  ("MARA-MATKL liefert SAP noch nicht") korrigiert - Full Load vom 24.07. fuellt MaraMatkl zu
  80,7%. Full Load ausserdem live geprueft: SupplierCountry 100%, MaraAbc 78%, MaraXyz 65% gefuellt
  -> Region/ABC/XYZ im Spend-Aufriss zeigen jetzt echte Daten statt Leer-Hinweis (Warnungen sind
  datengetrieben, kein Codeaenderung noetig). Details: `docs/rag/PURCHASING.md`.
- SPEND-AUFRISS (NEUER REITER) 2026-07-24, `272/272` Tests gruen, DEPLOYED (DLL 24.07.2026 10:47,
  Port 443 offen, HTTP 401 = App oben/Auth-Challenge; Commit `4e7861d`): Neuer Einkauf-Reiter
  `/einkauf/aufriss` „Spend-Aufriss" (Nav-Link `purchasing-breakdown`, Sort 55, zwischen Lieferanten
  und Ideen) - bewusst EIGENER Reiter, damit der von Marco abzunehmende `Spend`-Reiter unangetastet
  bleibt (Ingo: „in neuem Reiter wenn noetig"; Ingo-Entscheid: „bau alle 3"). Umgesetzt sind die drei
  offenen Spend-Wuensche aus der Einkaufssitzung: (1) MEHRSTUFIGE KASKADE Lieferant -> Warengruppe
  -> Artikel (aufklappbar, je Ebene Top-N gedeckelt `[40,15,10]` mit „uebrige (n)"-Restzeile, sodass
  Elternsumme = Summe der Kinder bleibt - wichtig, weil Blazor Server den Baum serverseitig rendert;
  nutzt VORHANDENE Cache-Daten Beleg-WG/Matnr, zeigt also SOFORT echte Zahlen). (2) REGION-KUCHEN je
  Warengruppe (conic-gradient-Donut je Top-Warengruppe -> Anteil je Lieferantenland). (3) VOLUMEN
  NACH ABC / XYZ (Balken aus `MaraAbc`/`MaraXyz`). WICHTIGER VORBEHALT: Region, ABC und XYZ sind bis
  zum naechsten Einkauf-Full-Load (SupplierCountry/MaraAbc/MaraXyz noch leer bzw. 0 %) faktisch LEER
  und zeigen ehrliche UI-Hinweise; nur die Kaskade hat heute echte Daten. Umsetzung: neue
  State-Felder + Aggregationen (`ExecuteSpendCascadeRowsAsync`, `ExecuteRegionByMaterialGroupRowsAsync`,
  ABC/XYZ-Charts) in `PurchasingDashboardService` (laufen NUR beim Datenladen, nicht pro Render);
  neue Models `PurchasingSpendCascadeNode`/`PurchasingRegionPieGroup`; eigene, selbsttragende
  Komponente `PurchasingSpendExplorer.razor` (Basis-CSS bewusst dupliziert, da der `aufriss`-Case
  `PurchasingSection` nicht rendert). NICHT umgesetzt (bewusst): flexible Einstiegsdimension
  (Question 2 unbeantwortet - klare Lesart genommen) und Produktgruppen-Aufriss (ZC23-Mapping fehlt,
  siehe `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`). 4 neue Tests (Kaskade-Pivot,
  Artikel-Deckelung+Rest, Region-Slices, ABC/XYZ). DEPLOYED 2026-07-24 (siehe Kopf des Eintrags).
  ERINNERUNG: Region/ABC/XYZ bleiben leer, bis der naechste Einkauf-Full-Load (mit Marco/Andreas
  abgestimmt) SupplierCountry/MaraAbc/MaraXyz befuellt.
- PERFORMANCE-BEFUND COCKPIT 2026-07-23, `268/268` Tests gruen, DEPLOYED (DLL 15:30): Auf "die
  ganze Webanwendung wird immer ein wenig langsamer" gemessen statt geraten: DB-Datei 305 MB,
  `CentralSalesRecords` 84'298 Zeilen (kein Index), `FinancialJournalEntries` 187'589 (indiziert),
  Purchasing-Caches 172-242k (indiziert). Konkreter Befund: `ManagementCockpitService` ist
  Singleton; `LoadCentralRecordsAsync()` (kompletter, ungefilterter Read von `CentralSalesRecords`
  inkl. Materialisierung in 40-Feld-Objekte) wird bei JEDEM Cockpit-Seitenaufruf 2-4x unabhaengig
  neu aufgerufen (Init, Central-Tab, Finance-Tab, Heartbeat-Tab) - Kosten wachsen mit der
  taeglich wachsenden Tabelle. FIX: 10s-TTL-Cache um `LoadCentralRecordsAsync()` (nur die Rohliste,
  nicht die Analyseergebnisse) - faengt alle Mehrfachaufrufe eines Seitenbesuchs ab, 10s haelt das
  Korrektheitsrisiko nach Full Load/Export minimal. Vor dem Ship verifiziert: alle 4 Aufrufer +
  `BuildFinanceDataStatusRowsAsync` behandeln die Liste rein lesend (nur Select/GroupBy/Where in
  neue Objekte), keine In-Place-Mutation der geteilten `SalesRecord`-Elemente - Singleton-Cache
  ist damit nebenlaeufig sicher. WICHTIG: das ist eine bestaetigte, konkrete Ineffizienz, aber NICHT
  bestaetigt als alleinige Ursache der App-weiten Verlangsamung - der Nutzer konnte die
  Unterscheidungsfrage "wird es nach einem Neustart kurz schneller und dann wieder langsamer" nicht
  beantworten. Falls das Muster spaeter beobachtet wird: das deutet auf IIS-Worker-Alterung
  (App-Pool-Recycling, Betriebsthema) hin, nicht auf Code. Log-Tabellen (`AppEventLogs` 3'149,
  `ExportLogs` 173) sind zu klein, um relevant zu sein - kein Fix noetig.
- EINKAUF-NACHTLAUF + LADE-BUTTONS 2026-07-23, `268/268` Tests gruen, DEPLOYED (DLL 14:49):
  Auf der Einkauf-Datenquellen-Seite (`/einkauf/verbindungen`) neuer Bereich "Datenladung" mit
  Buttons "Full Load starten" und "Delta aktualisieren" (+ Fortschrittsbalken/Statuszeile), damit
  der Lauf auch von der Settings-Seite ausgeloest werden kann (nicht nur ueber `/einkauf`).
  Naechtlicher Automatik-Lauf: `TimerBackgroundService` ruft im PLANMAESSIGEN 03:00-Slot zusaetzlich
  `RunPurchasingDeltaAsync()` (nur Delta, leicht) - gegated auf Site `PURCHASING_SAP` IsActive,
  eigener DI-Scope (Refresh-Service ist Scoped), eigener try/catch (Einkauf-Fehler bricht den
  Finance-Stempel nicht). BEWUSST NICHT im Nachhol-Lauf (`CatchUpMissedRunAsync`): sonst wuerde ein
  Deploy-Restart nach 03:00 einen SAP-Lauf gegen travp762 ausloesen. Full Load bleibt manuell
  (kompletter Cache-Neuaufbau, 18-Mio-Last - mit Marco/Andreas abstimmen), Ingo-Entscheid:
  "Delta nachts, Full auf Knopf". Haengt am selben `TimerEnabled`-Schalter wie Finance.
- UI 2026-07-23, `268/268` Tests gruen: Erste neue Einkauf-Sicht gebaut - Balkenblock "Volumen
  nach Beschaffungsregion" (Lieferantenland LFA1.Land1 -> EKKO.SupplierCountry) im Spend-Reiter,
  neben "Volumen nach Warengruppe". Der Zusatz-Chart-Bereich von `PurchasingSection` ist von einem
  einzelnen Zweitchart auf eine generische Liste `ExtraCharts` (Model `PurchasingSectionExtraChart`)
  umgestellt, damit ABC/XYZ als weitere Bloecke sauber danebenpassen (Marcos "eine Sicht nach der
  anderen"). Neue Aggregation `RegionSpendRows` im `PurchasingDashboardService` (gleicher Filter/
  Zeitraum wie WG/Lieferant). Region-Werte fuellen sich erst mit dem naechsten Einkauf-Full-Load
  (SupplierCountry-Spalte noch leer). VknrDispo jetzt live bestaetigt (SEGW-Property angelegt +
  generiert, liefert `019`) - Datenvoraussetzung fuer den Produktgruppen-Aufriss steht, ZC23-
  Zuordnung noch offen. Details: `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`.
- SAP-FELDER + C#-LADESTRECKE 2026-07-23, `268/268` Tests gruen: Ingo hat weitere SAP-Felder
  transportiert (LFA1.Land1, MARC.Maabc, neues Set ZSTR_MAT_XYZSet fuer XYZ), alle live gegen
  travp762 verifiziert. C#-Ladestrecke (`PurchasingDataRefreshService`) angepasst: liest jetzt
  Lieferantenland (LFA1.Land1 -> neue Cache-Spalte `SupplierCountry`), ABC (MARCSet.Maabc ->
  `MaraAbc`) und XYZ (`ZSTR_MAT_XYZSet.Maxyz` -> `MaraXyz`). WICHTIG MARCSet: ignoriert
  $top/$skip/$filter (wie maracalc), deshalb EIN ungepagter Request + client-seitiger
  Werk-1100-Filter; XYZ-Set (eigener Methodenrumpf `docs/abap/ZSTR_MAT_XYZ_GET_ENTITYSET.abap`,
  von mir gebaut) pagt korrekt. XYZ-Quelle war Marcos „ITSCH-MAT-ABC-XYZ" = Tabelle
  `ZCA_MAT_ABC_XYZ`, Feld `/ITS/CA_M_MAXYZ` (XYZ ist KEIN SAP-Standard, nur ABC). Datenlage Werk
  1100: Land gefuellt; ABC 86 % leer (A/B/C echt vorhanden); XYZ-Set 4'388 Materialien, 99 %
  klassifiziert. Neue Cache-Spalten additiv; FUELLEN sich erst mit dem naechsten Einkauf-Full-Load
  (mit Marco/Andreas abstimmen). UI/Visuals bewusst noch NICHT gebaut (Marco: Sicht fuer Sicht).
  OFFEN: `VknrDispo` (Produktgruppen-Aufriss) - SE11-Struktur `ZSTR_LZCODE_USAGE` braucht noch das
  Feld `VKNR_DISPO` (DE `DISPO`), dann ZLO03-USAG-Rumpf erneut aktivieren. Details:
  `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`.
- SAP-ERWEITERUNG + LOADER-UMSTELLUNG 2026-07-23, `268/268` Tests gruen: Ingo hat `Matkl`
  (Materialstamm-Warengruppe) UND `Mstae` beide ins `MARA001Set` aufgenommen. Damit hat EIN Set
  wieder alle vom Einkauf-Loader benoetigten Materialstamm-Felder (Matnr+Mstae+Matkl); der Loader
  (`PurchasingDataRefreshService.LoadMaterialStatusMapAsync`) ist von `maracalcSet` zurueck auf
  `MARA001Set` umgestellt (`$select=Matnr,Mstae,Matkl`, ein ungepagter Request - MARA001Set
  ignoriert $top/$skip/$filter, liefert immer alle 68'125 Zeilen, live verifiziert). Vorgeschichte:
  bis 17.07. MARA001Set (hatte Mstae), 17.07. auf maracalcSet gewechselt (MARA001Set hatte Mstae
  verloren, aber kein Matkl), jetzt beide Felder in MARA001Set -> zurueck. LIVE VERIFIZIERT gegen
  travp762: MARA001Set `$select=Matnr,Mstae,Matkl` -> 200; Mstae 48,8 % mit Status (41 % `99`,
  2,4 % `98`) - MSTAE-98/99-Filter wirkt weiter; Matkl 35 % gefuellt, davon viel `01`, ~10 % echte
  Gruppen (65 % leer -> COALESCE-Fallback auf Beleg-WG). EKKO/EKPO-Loaderfelder
  (Bstyp/Bsart/Konnr/Elikz/Matkl) auf travp762 ebenfalls vorhanden -> Full Load laeuft durch (der
  2026-07-10-Blocker ist weg). NACHSORGE: `MaraMatkl` im Cache ist noch 0 % (Load-Stand 17.07.);
  wird erst mit dem naechsten Einkauf-Full-Load gefuellt - der ist mit Marco/Andreas abzustimmen
  (laufende 18-Mio-Abnahme, Datenbestand wechselt auf travp762). Details: `docs/rag/PURCHASING.md`.
- APP-AENDERUNG 2026-07-23, `268/268` Tests gruen (JETZT deployed, siehe Deploy-Eintrag): Einkauf-Reiter `Spend`
  hat ein zweites Balkendiagramm "Volumen nach Warengruppe" (PowerBI-Seite "Diagramm Vol./WG").
  Anlass: Ingo-Analyse der `li.pbix`/`x.pbix` (beide identisch, 7 Seiten) - das WG-Diagramm war in
  der App nicht als echtes Visual vorhanden, WG lebte nur als Drilldown-Ebene der Spend-Matrix.
  Bewusst im Spend-Reiter platziert (Volumenanalyse), nicht bei Lieferanten (Bewertung). Umsetzung
  rein C#/Razor: neue Aggregation `MaterialGroupSpendRows` in `PurchasingDashboardService`
  (COALESCE(MaraMatkl,Matkl,'ohne WG'), gleicher Filter/Zeitraum wie Lieferant-Matrix), zweiter
  optionaler Chart-Block in `PurchasingSection.razor`, verdrahtet im Spend-case von
  `PurchasingDashboard.razor` mit ehrlichem Datenhinweis. WICHTIGER BEFUND (an Prod-Cache
  gemessen, Load-Stand 17.07.): WG faktisch unbrauchbar bis SAP-Erweiterung - `MaraMatkl` 0 %
  gefuellt, `Matkl` zu 99,6 % in Sammelgruppe `01`; Diagramm zeigt daher aktuell fast nur eine
  Saeule (strukturell korrekt, aussagekraeftig erst nach `Matkl` im `maracalcSet`). BEWUSST NICHT
  nachgebaut aus PowerBI: Kuchen Lieferant (durch Top-Lieferanten-Balken abgedeckt), Kuchen Region
  (Lieferantenland fehlt im Cache - LFA1 laedt nur Name1, nicht Land1). Details:
  `docs/rag/PURCHASING.md`.
- ROOTCAUSE + FIX 2026-07-23, `268/268` Tests gruen: Numerische Materialnummern (z.B. `2217`)
  lieferten in der Stuecklistenanalyse IMMER 0 Zeilen, alphanumerische (`D15019`) gingen. Per
  SapProbe/RFC gegen travp762 (mit Ingos Prod-Passwort) + OData-Testbatterie eingegrenzt: MARA hat
  `000000000000002217` mit LEEREM LVORM (die 22d-Loeschvormerkungs-Theorie WAR FALSCH),
  ZPOWERBI_VC_TXT hat die Zeilen mit gefuellter Menge. Auch include_deleted (LVORM-Filter aus) gab
  0 -> Schritt 1 (SELECT FROM mara) fand die numerische Nummer nicht, weil
  CONVERSION_EXIT_ALPHA_INPUT (22c) sie NICHT zuverlaessig zero-paddete (zerstoerte sogar die
  bereits gepaddete Eingabe). FIX doppelt abgesichert: (1) C#
  (`MaterialUsageDataRefreshService.NormalizeMaterialToken`) paddet rein numerische Nummern vor dem
  $filter auf 18 Stellen; (2) ABAP (beide Methodenruempfe) nimmt den Rohwert IMMER in die RANGE auf
  (App schickt gepaddet -> sicherer Treffer) plus zusaetzlich die MATN1-Form fuer kurze manuelle
  Eingaben (CONVERSION_EXIT_MATN1_INPUT statt ALPHA). C#-Seite deployt (siehe Deploy-Eintrag
  unten); ABAP muss erneut auf travt762 UND travp762 eingefuegt/aktiviert werden. Details:
  `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag 2026-07-23. NEBENBEFUND: MAKTX kommt beim
  Service-User (POWERBI) leer zurueck (sprachabhaengiger MAKT-Join) - Zeile wird trotzdem
  ausgegeben (22b-Haertung greift), nur der Text fehlt; fachlich unkritisch, spaeter ggf.
  sy-langu-unabhaengig lesen.
- DEPLOYED 2026-07-22 (Commit `bacc614 Add option to include deletion-flagged materials in BOM
  analysis`, `267/267` Tests gruen, DLL `22.07.2026 14:26:01`, Laenge `3'076'096`, Port 443
  erreichbar, DB unveraendert): Loeschvorgemerkte Materialien optional einbeziehbar ist live
  (siehe Eintrag direkt darunter fuer Details). Publish nach
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\` via `dotnet publish -c Release
  -p:PublishProfile=FolderProfile`, `app_offline.htm` gesetzt/entfernt. NICHT Teil dieses Deploys:
  der ABAP-Fix (Richtung-Suffix ALLE, LVORM-Bypass) - Methodenrumpf liegt bereit und muss
  weiterhin manuell in SE80 auf travt762 UND travp762 eingefuegt/aktiviert werden.
- APP-AENDERUNG 2026-07-22, `267/267` Tests gruen (JETZT deployed, siehe Eintrag oben):
  Loeschvorgemerkte
  Materialien optional einbeziehbar (Wunsch Ingo, nach Live-Diagnose mit den Test-Nummern `1689,
  2163, 2217, 2286, 2366, 2367, 2434, 2537`). Live-Diagnose mit denselben Service-Credentials wie
  die App zeigte: Top-Down fuer "normales" Material (`D15019`) funktioniert, Bottom-Up fuer
  `Kompnr=C34882` findet `Vknr=2217` mit echten Daten, aber Top-Down fuer `Vknr=2217` (Kurz- UND
  Langform) liefert weiterhin 0 Zeilen - Ursache: Schritt 1 (MARA-Selektion) laesst per Default
  nur `LVORM = ' '` zu (wie Report-Default `p_lvorm=' '`), die Testnummern sind offenbar alte,
  loeschvorgemerkte Kopfmaterialien. FIX: `Richtung`-Wert akzeptiert jetzt Suffix `ALLE`
  (`TOPDOWNALLE`/`BOTTOMUPALLE`, ohne DDIC-Aenderung), neue Checkbox "Auch geloeschte Materialien"
  in `Components/Pages/BomAnalysis.razor`, neuer Parameter `includeDeleted` in
  `MaterialUsageDataRefreshService.RunFullLoadAsync`, 2 neue Tests fuer `BuildRichtungValue`.
  NACHARBEIT SAP (wie gehabt): Methodenrumpf `ZSTR_LZCODE_USAG_GET_ENTITYSET.abap` erneut auf
  travt762 UND travp762 einfuegen, aktivieren, `/IWFND/CACHE_CLEANUP`. Details:
  `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag 2026-07-22d.
- DEPLOYED 2026-07-22 (Commit `7d061d9 Support material number ranges (35-40) in BOM analysis
  material filter`, `265/265` Tests gruen, DLL `22.07.2026 13:22:34`, Laenge `3'075'584`, Port 443
  erreichbar, DB unveraendert): Bereichs-Syntax `35-40` im Materialfeld der Stuecklistenanalyse
  (siehe Eintrag direkt darunter fuer Details) ist live. Publish nach
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\` via `dotnet publish -c Release
  -p:PublishProfile=FolderProfile`, `app_offline.htm` gesetzt/entfernt. NICHT Teil dieses Deploys:
  die ABAP-Fixes (ALPHA-Konvertierung, ZPOWERBI_VC_TXT-Quelltabelle) - die liegen als
  Methodenruempfe in `docs/abap/` bereit und muessen weiterhin manuell in SE80 auf travt762 UND
  travp762 eingefuegt/aktiviert werden (siehe zwei Eintraege unten).
- APP-AENDERUNG 2026-07-22, `265/265` Tests gruen (JETZT deployed, siehe Eintrag oben): Neue
  Bereichs-Syntax im Materialfeld der Stuecklistenanalyse (`Components/Pages/BomAnalysis.razor`,
  Wunsch Ingo): `35-40` neben kommagetrennten Einzelwerten. Rein C#-seitig in neuer, public
  static Methode `MaterialUsageDataRefreshService.BuildMaterialClause` (5 neue Tests) -
  Bereichs-Token werden zu `(Vknr ge 'X' and Vknr le 'Y')`, gemischt mit Einzelwerten per `or`
  verknuepft. Keine ABAP-Aenderung noetig, siehe `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag.
- ROOTCAUSE + ABAP-FIX 2026-07-22c (kein App-Deploy; SAP-Nacharbeit durch Ingo/Lucas noetig,
  BESTAETIGTE Ursache): Zweiter Full-Load-Test gegen travp762 (nach dem ZPOWERBI_VC_TXT-Fix,
  siehe Eintrag darunter) lief technisch durch, lieferte aber fuer `Vknr=2217`/TOPDOWN 0 Zeilen.
  Ingo hat die Ursache selbst durch einen direkten Browser-Vergleichstest zweifelsfrei belegt:
  `$filter=... Vknr eq '2217'` (Kurzform) = 0 Treffer, `$filter=... Vknr eq
  '000000000000002217'` (18-stellig) = echte Treffer. Grund: `MARA`/`ZPOWERBI_VC_TXT` speichern
  Materialnummern intern padded; eine SELBSTGESCHRIEBENE GET_ENTITYSET-Methode bekommt
  `it_filter_select_options` aber ROH, die sonst automatische externe->interne
  ALPHA-Konvertierung des Gateway-Frameworks greift bei eigenem Code nicht. Produktionslog
  bestaetigte zusaetzlich: der App-Full-Load hatte exakt denselben unpadded Wert wie der erste
  fehlgeschlagene manuelle Test verwendet - kein Padding-Bug im C#-Code (der reicht Werte
  unveraendert durch, das ist korrekt so). FIX: Beide Methoden
  (`docs/abap/ZSTR_LZCODE_USAG_GET_ENTITYSET.abap`, `docs/abap/ZSTR_LZCODE_PARE_GET_ENTITYSET.abap`)
  konvertieren Low/High-Werte der Vknr/Kompnr-Filter jetzt per `CONVERSION_EXIT_ALPHA_INPUT`, bevor
  sie in die RANGE-Tabellen wandern - damit funktionieren Kurz- UND Langform gleichermassen.
  ZUSAETZLICHE HAERTUNG (Version 2026-07-22b, unabhaengiger Befund, weiterhin gueltig): der aus
  dem Report uebernommene Zeilen-Drop bei fehlendem MAKTX (`DELETE gt_ktab WHERE maktx IS
  INITIAL`) ist entfernt, weil die MAKT-Textsuche sprachabhaengig (`sy-langu`) ist und fuer einen
  Webservice keine Zeilen mit echten Bestandsdaten wegen einer fehlenden Uebersetzung verschwinden
  sollten (die urspruengliche Vermutung, DAS sei die Ursache des 0-Zeilen-Symptoms, war falsch und
  ist durch den ALPHA-Befund widerlegt - die Haertung bleibt trotzdem sinnvoll). NACHARBEIT SAP
  (wie beim vorigen Fix): Methodenruempfe erneut auf travt762 UND travp762 einfuegen, Klasse
  aktivieren, `/IWFND/CACHE_CLEANUP`. Details: `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag
  2026-07-22c.
- ROOTCAUSE + ABAP-FIX 2026-07-22 (kein App-Deploy; SAP-Nacharbeit durch Ingo/Lucas noetig):
  Nach dem travp762-Wechsel brachen ALLE EntitySets von `ZPOWERBI_EINKAUF_SRV` auf PROD mit
  `SYNTAX_ERROR` ab (Logistik-Full-Load UND Einkauf-Full-Load `EKKOSet`; Einkauf-Cache blieb dank
  Guardrail unveraendert auf dem Stand 2026-07-17). URSACHE (von Ingo identifiziert): Die
  DPC_EXT-Methodenruempfe vom 2026-07-21 basierten auf einer ALTEN ZLO03-Fassung und lasen aus
  `ZAT_VC` — diese Tabelle existiert auf travp762 nicht, dadurch kompilierte die komplette
  DPC_EXT-Klasse nicht und riss den ganzen Service mit (deshalb auch EKKOSet betroffen). Die
  aktuelle Reportfassung liegt seit 2026-07-22 als `docs/abap/originalzlo03.txt` vor und liest aus
  `ZPOWERBI_VC_TXT`. FIX: Beide Methodenruempfe (`docs/abap/ZSTR_LZCODE_USAG_GET_ENTITYSET.abap`,
  `docs/abap/ZSTR_LZCODE_PARE_GET_ENTITYSET.abap`) auf die neue Fassung umgeschrieben —
  Quelltabelle `ZPOWERBI_VC_TXT`, plus Report-FIXES uebernommen: FIX 1 (keine Mengen-Rundung auf
  0 Dezimalen mehr), FIX 2 (Mehrfachverwendungen summieren statt deduplizieren, deterministisch
  ueber SORTED TABLE), FIX 4 (Textpositionen `postyp='T'` und Zeilen ohne MAKTX im Default raus),
  neue Baugruppen-Logik (`(VC-Baugruppe ODER MAST) UND beskz<>'F'`), Stammdaten-JOIN ohne
  LVORM-Filter. DDIC-Strukturen und C#-Seite unveraendert. NACHARBEIT (manuell): Ruempfe auf
  travt762 UND travp762 einfuegen, Klasse aktivieren, `/IWFND/CACHE_CLEANUP`; erst danach sind
  Einkauf- und Logistik-Loads gegen P wieder moeglich. Details:
  `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag 2026-07-22.
- KONFIGURATION GEAENDERT 2026-07-22 (kein Deploy, reine DB-Konfiguration): Zentrale SAP-URL
  (`SourceSystemDefinitions.CentralServiceUrl`, Code `SAP`) von `travt762` (TEST) auf `travp762`
  (PROD) umgestellt — Anlass: Ingo wollte die neue Logistik/Stuecklistenanalyse (ZLO03-Webservice,
  siehe Eintrag darunter) mit echten Daten pruefen, `ZAT_VC` ist auf travt762 leer. Vor der
  Aenderung per Live-Query verifiziert (nicht wie in `docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md`
  A0 angenommen "wirkt fuer alle SAP-Bereiche"): `Sites` fuer `ZSCHWEIZ` hat einen EIGENEN, bereits
  explizit gesetzten Override (`SapServiceUrl` = travt762) und ist von der zentralen Aenderung
  NICHT betroffen — Finance CH/AT bleibt unveraendert auf travt762 (TEST). Betroffen ist NUR die
  Site `PURCHASING_SAP` (kein eigener Override), die sowohl vom Einkauf-Dashboard als auch von der
  neuen Logistik/Stuecklistenanalyse gemeinsam genutzt wird — beide zeigen ab sofort auf travp762
  (PROD). Aenderung gezielt nur auf `SourceSystemDefinitions WHERE Code='SAP'` beschraenkt (kein
  Touch von `Sites`), per kleinem C#/Microsoft.Data.Sqlite-Skript direkt gegen die Produktions-DB
  (`\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db`), analog dem
  bestehenden `spartenlogic/.tmp_update_sap_url`-Muster. Vorher Backup gezogen:
  `trafag_exporter.db.before-travp762-purchasing-switch-20260722.bak` (gleiche Konvention wie die
  bestehenden `.before-*`-Sicherungen). NACHSORGE/OFFENE RISIKEN (aus
  `docs/rag/PURCHASING.md` bereits bekannt, jetzt wirksam): (1) Einkauf-Cache enthaelt noch
  Testdaten -> Full Load noetig, sonst alte Zahlen im Spend-Reiter waehrend Marcos laufender
  18-Mio-Abnahme — mit Marco/Andreas abstimmen, bevor der naechste Full Load gefahren wird. (2)
  Direkter Basic-Auth-Test gegen travp762 gab zuletzt `HTTP 401` (Stand 2026-07-09, Status seither
  nicht erneut geprueft) — falls das weiterhin besteht, schlagen jetzt Einkauf- UND
  Logistik-Loads fehl. (3) `Bstyp`/`Bsart`/`Elikz` fehlten auf travp762 zuletzt im OData-Modell
  (Probe 2026-07-10) — betrifft die Stuecklistenanalyse nicht direkt (anderes EntitySet), verzerrt
  aber ggf. Einkauf-Zahlen (Beleg-Mix-Trennung), bis SAP das P-Modell nachzieht.
- DEPLOYED 2026-07-21 (Commit `a314881 Add ZLO03 BOM-analysis webservice: SAP entity methods,
  C# loader, Logistik tab`, `260/260` Tests gruen, DLL `21.07.2026 15:04:46`, Laenge
  `3'075'072`, Port 443 erreichbar, DB unveraendert `14:50:23`): NEUER ROOT-REITER **LOGISTIK**
  (Icon LocalShipping) mit Unterpunkt **Stuecklistenanalyse** (`Components/Pages/BomAnalysis.razor`,
  `/logistik/stuecklistenanalyse`, Seed-Keys `logistics`/`logistics-bom-analysis`) — macht den
  SAP-Report `ZM_LZCODE20_OPT` (Top-Down/Bottom-Up-Stuecklistenanalyse, bisher nur als
  Excel-Download) per Webservice ansprechbar. SAP-Seite: zwei neue OData-EntitySets am
  bestehenden Gateway-Service `ZPOWERBI_EINKAUF_SRV`, angelegt als DPC_EXT-Methodenruempfe
  OHNE eigene Klasse (`ZSTR_LZCODE_USAG_GET_ENTITYSET`/`ZSTR_LZCODE_PARE_GET_ENTITYSET`, beide
  am 2026-07-21 in SEGW fehlerfrei aktiviert) auf zwei neuen, feldweise verifizierten
  DDIC-Strukturen (`ZSTR_LZCODE_USAGE`/`ZSTR_LZCODE_PARENT`) — normalisiertes Zeilenmodell statt
  der dynamischen Pivot-Matrix des Reports, behebt dabei den in
  `docs/INGO_TODOS_180_TAGE_2026-06-18.md` genannten Nichtdeterminismus (HASHED-TABLE-Reihenfolge
  ohne SORT in `FORM get_elternmaterial`). C#-Seite (`MaterialUsageDataRefreshService`) loest die
  EntitySet-Namen dynamisch auf (SEGW hat nach den Strukturen benannt, nicht wie urspruenglich
  vorgeschlagen `MaterialUsageSet`) und schickt SAP-seitig erzwungene Materialfilter (Catch-all
  oder gezielte Liste aus der neuen UI). Live-Verifikation gegen T76/travt762 per SapProbe (RFC)
  bestaetigte vorab alle offenen Fachannahmen (`KOM_MSTAE` ist ein MATNR-Feld, `ZZLZCOD`/
  `ZZLZCODSORT` haben echte Datenelemente, `ZAT_VC`/`ZMD04_CALC` lesbar). GEGEN TEST SIND 0
  ZEILEN ERWARTET (ZAT_VC auf travt762 leer, echte Daten liegen auf travp762/PROD — bekannter
  travt/travp-Punkt aus `docs/rag/PURCHASING.md`, hier nicht angefasst). Perspektivisch auch fuer
  den Einkauf nutzbar (Exklusivitaet/Bestaende je Komponente), startet aber bewusst als eigener
  Reiter. Details: `docs/abap/README_LZCODE_WEBSERVICE.md`.
- ENTWURF + LIVE-VERIFIKATION 2026-07-21, EINKAUF/PRODUKTMAPPING (kein Deploy, Code noch NICHT
  committet, ausser SapProbe): Ingo bat darum, den Report `ZM_LZCODE20_OPT`/`zlo03.txt`
  (Top-Down/Bottom-Up-Stuecklistenanalyse) wie andere SAP-Tabellen per Webservice ansprechbar zu
  machen. Ergebnis ist ein Entwurfspaket fuer Lucas/SAP-Team (bewusst nicht produktiv, technische
  SAP-Anlage bleibt gemaess Abgrenzung in `docs/INGO_TODOS_180_TAGE_2026-06-18.md` beim SAP-Team):
  (1) Spezifikation `docs/abap/README_LZCODE_WEBSERVICE.md` mit normalisiertem Zeilenmodell
  `MaterialUsageSet`/`MaterialParentSet` (statt der dynamischen Pivot-Matrix des Reports) fuer den
  bestehenden Gateway-Service `ZPOWERBI_EINKAUF_SRV`. (2) Zwei ABAP-Klassenentwuerfe
  `ZCL_LZCODE_PROVIDER.abap` (mit privaten Hilfsmethoden) und `ZCL_LZCODE_PROVIDER_INLINE.abap`
  (gleiche Logik komplett in `GET_DATA` inline, falls nur eine DPC-Methode redefiniert werden
  soll) — behebt dabei den in den Ingo-Todos genannten Nichtdeterminismus (`FORM
  get_elternmaterial` haengt eine `HASHED TABLE` ohne `SORT` durch, Reihenfolge nicht definiert).
  (3) C#-Konsument `Services/MaterialUsageDataRefreshService.cs` + Schema
  (`MaterialUsageCache`/`MaterialParentCache`/`MaterialUsageSyncState`) + 2 Tests, analog
  `PurchasingDataRefreshService` — prueft EntitySet-Existenz vor dem Laden und meldet fachlich
  klar, wenn die SAP-Seite noch fehlt (kein Absturz). NACHTRAG spaeter am 2026-07-21: SAP-Seite
  ist inzwischen KOMPLETT angelegt (beide SE11-Strukturen feldweise verifiziert, beide
  DPC_EXT-Methoden `ZSTR_LZCODE_USAG/PARE_GET_ENTITYSET` fehlerfrei aktiviert — Variante 3 ohne
  eigene Klasse, Methodenruempfe in `docs/abap/ZSTR_LZCODE_*_GET_ENTITYSET.abap`). C#-Seite
  daran angepasst: EntitySet-Namen werden dynamisch aufgeloest (`ResolveEntitySetName`, SEGW hat
  nach Strukturnamen benannt), Property-Keys unterstrich-tolerant, Full Load schickt
  Guard-konforme Filter (`Vknr gt ''` Catch-all bzw. optionale Materialliste). UI seit
  2026-07-21 (Entscheid Ingo): neuer Root-Reiter LOGISTIK (Icon LocalShipping) mit Unterpunkt
  STUECKLISTENANALYSE (`Components/Pages/BomAnalysis.razor`, `/logistik/stuecklistenanalyse`,
  Seed `logistics`/`logistics-bom-analysis`) — SAP-Load mit Richtungs-Schalter und
  Materialfilter, Statusanzeige, durchsuchbare Cache-Vorschau. Daten sollen spaeter auch im
  Einkauf nutzbar sein, starten aber als eigener Reiter. `260/260` Tests gruen. INZWISCHEN
  committet und deployed, siehe Eintrag ganz oben (Commit `a314881`, 2026-07-21). Ende-zu-Ende
  gegen TEST liefert erwartungsgemaess 0 Zeilen (ZAT_VC dort leer), echter Datentest erst nach
  travt/travp-Umstellung. (4) LIVE-VERIFIKATION gegen `T76`/`travt762` (TEST) per `SapProbe`
  bestaetigt alle offenen Fachannahmen: `ZAT_VC-KOM_MSTAE` ist trotz irrefuehrenden Namens ein
  MATNR-Feld (Elternmaterial-Mapping korrekt), `MARA-ZZLZCOD`/`ZZLZCODSORT` haben echte
  Datenelemente (`CHAR 4`, keine PAPH1-Falle), `ZAT_VC`/`ZMD04_CALC` existieren und sind lesbar
  (Feldlisten passen zum Provider). (5) DDIC-ANLAGE PER TOOL GEPRUEFT UND VERWORFEN: `SapProbe`
  kann die noetigen SE11-Strukturen (`ZSTR_LZCODE_USAGE`/`ZSTR_LZCODE_PARENT`) NICHT selbst
  anlegen — `DDIF_STRU_PUT` existiert nicht (korrekt: `DDIF_TABL_PUT`/`DDIF_TABL_ACTIVATE`), und
  diese sind auf T76 nicht RFC-freigegeben (Invoke-Test: „ist nicht 'remote' aufrufbar",
  SAP-Community bestaetigt DDIF*-Bausteine generell als nicht remote-enabled). Empfehlung:
  Strukturen manuell in SE11 anlegen, Feldliste ist verifiziert und in
  `.tmp_sap_probe/ddic_lzcode/` als Kopiervorlage abgelegt.
- WERKZEUG-ERWEITERUNG 2026-07-20/21, COMMIT `346bea3` (SapProbe, `.tmp_sap_probe/`): Der
  RFC/NCo-Direktzugriff auf SAP (unabhaengig von der OData-Strecke der App, siehe
  `docs/RAG_ROUTER.md` Abschnitt „Werkzeug: SAP-Direktzugriff") kann jetzt `rfc-call --table
  NAME=datei.csv`/`--struct NAME=datei.csv`, um beliebige RFC-faehige Bausteine mit Tabellen-/
  Strukturparametern aus CSV zu fuellen, gesperrt hinter `--confirm-write`/`--dry-run` wie
  `abap-write`. `function-info` zeigt bei TABLE/STRUCTURE-Parametern jetzt auch die
  verschachtelten Feldnamen mit. Grenzen empirisch geklaert (s. Punkt oben): fuer DDIC-Anlage
  nicht nutzbar, weil die dafuer noetigen Bausteine auf T76 nicht RFC-freigegeben sind.

- PRODUKTIVDATEN 2026-07-17 EINKAUF (kein Code-Deploy, reiner Datenlauf gegen die Server-DB): Einkauf-Full-Load nach dem heutigen `maracalcSet`-Fix erfolgreich durchgelaufen (`EKKO=172'914, EKPO=234'083, EKET=242'734, MARA-Status=67'665, LFA1-Namen=6'747`). Verifiziert: `SupplierName` in `PurchasingEkkoCache` jetzt zu 99.99 % gefuellt (172'898/172'914), vorher 0/172'874 (letzter erfolgreicher Load war vom 07.06., vor dem LFA1-Namens-Fix; der einzige Load danach am 02.07. war am `MARA001Set`-404 gescheitert, bevor LFA1 ueberhaupt geladen wurde). Stichprobe bestaetigt echte Namen statt Nummern: `66952 -> BEPRO AG`, `70369 -> CPT Praezisionstechnik GmbH`, `66715 -> GFS`, `65058 -> HEITZ GMBH`. Der Spend-Reiter (Matrix `Kaskadierung Lieferant / Jahr`) zeigt damit ab sofort Lieferantennamen statt nur Nummern. OFFENER PUNKT (nicht angefasst, gehoert mit Marco/Andreas abgestimmt): Die zentrale SAP-Quelle fuer Einkauf zeigt weiterhin auf `travt762` (Test-Server), nicht `travp762` (Prod) — gleiches Grundthema wie das bekannte ZSCHWEIZ/2026-Problem.
- DEPLOYED 2026-07-17 (Commit `c34e593 Rename "Export all" button to clarify it reloads from source, not just DB`, `257/257` Tests gruen, DLL `17.07.2026 10:41:31`, Laenge `3'006'976`, Port 443 erreichbar, DB unveraendert): UI-TEXT (Export Dashboard, alle 5 Sprachen mitgezogen): Button „Alle exportieren"/„Export all" umbenannt in „Alle Standorte laden"/„Reload all sites" (ES „Recargar todos los sitios", IT „Ricarica tutte le sedi", HI „सभी साइटें लोड करें"). Anlass: Ingo empfand „Alle exportieren" als irrefuehrend, weil der Button nicht nur bereits geladene Daten exportiert, sondern je aktivem Standort frisch von der Quelle (SAP/HANA/manuelle Datei) liest und die DB neu befuellt — Verwechslungsgefahr mit dem daneben liegenden „Zentrale Datei neu erzeugen" (das NUR mit der DB arbeitet, nichts neu laedt). Reine Beschriftungsaenderung, keine Logikaenderung. `Services/UiTextService.cs` Uebersetzungs-Dictionary-Keys aktualisiert (Key = deutscher String), damit ES/IT/HI nicht auf Englisch zurueckfallen. `257/257` Tests gruen.
- DEPLOYED 2026-07-17 (Commit `3a4efb5 Add purchasing spend drilldown by material group, fix broken MARA status read`, `257/257` Tests gruen, DLL `17.07.2026 10:05:07`, Laenge `3'006'464`, Port 443 erreichbar, DB unveraendert — neue Spalte `PurchasingEkpoCache.MaraMatkl` wird additiv beim naechsten App-Start ergaenzt): SPEND-DRILLDOWN nach Feedback-Runde Marco/Armin — Leitplanke "ein Punkt nach dem anderen, zuerst Reiter Spend". (1) Die Matrix `Kaskadierung Lieferant / Jahr` hat eine zweite Ebene: Lieferant aufklappen zeigt Spend je Warengruppe/Jahr (Pivot-artig, Drill-Summen exakt = Lieferantenzeile, Zeitraumfilter wirkt auf beide Ebenen); neue Aggregation `ExecuteSupplierGroupYearRowsAsync`, Modell `PurchasingSpendGroupYearRow`, Toggle-UI in `PurchasingSection.razor`. (2) Warengruppe nach Marcos Vorgabe aus dem MATERIALSTAMM (`MARA-MATKL`), nicht aus dem Beleg (alte Belege = Dummy-Warengruppe): neue additive Spalte `PurchasingEkpoCache.MaraMatkl`, Drilldown nutzt `COALESCE(MaraMatkl, Matkl, 'ohne Warengruppe')` mit UI-Hinweis auf den Fallback. ABER: `Matkl` ist in KEINEM MARA-EntityType des Service vorhanden -> SAP-Erweiterungsanfrage (`maracalc` um `Matkl` ergaenzen); App-Seite fertig, danach nur `$select` erweitern. (3) WICHTIGER NEBENBEFUND, produktionskritisch: SAP hat das MARA-Set umgebaut — `MARA001Set` exponiert `Mstae` NICHT mehr (`$select=Mstae` -> 404), der bestehende Einkauf-Full-Load/Delta waere beim naechsten Lauf FEHLGESCHLAGEN. Fix: `LoadMaterialStatusMapAsync` liest jetzt das neue `maracalcSet` (verifiziert: 68'094 Zeilen, 33'242 mit Status); Achtung, das Set ignoriert `$top`/`$skip` wie `mbewSet`, deshalb bewusst EIN ungepagter Request statt Paging. (4) ABC/XYZ: Weg jetzt klar (ABC = `MARC-MAABC` Sicht O2, XYZ separate Tabelle, vorhandener Report extrahiert beides) — bewusst erst nach Spend-Abnahme. 2 neue Drilldown-Tests. NACH Deploy: Einkauf Full Load noetig (fuellt Mstae wieder; MaraMatkl bleibt leer bis SAP-Erweiterung). Doku: `docs/PURCHASING_DASHBOARD_2026-06-05.md` Nachtrag 2026-07-17.
- DEPLOYED 2026-07-17 (Commit `846e3f8 Prepare additive contribution-margin fields and document standard-cost sourcing`, `255/255` Tests gruen, DLL `17.07.2026 08:53:22`, Laenge `2'992'640`, Port 443 erreichbar, DB unveraendert — neue Spalten `StandardCostVariable`/`StandardCostFixed` werden additiv beim naechsten App-Start ergaenzt): DECKUNGSBEITRAG (DB) als rein ADDITIVE Strecke vorbereitet — auf Wunsch von Ingo nach Andreas' Fachinput (DB = Umsatz minus variable Kosten; fix/variabel-Trennung entscheidend). NICHTS Bestehendes geloescht oder umbenannt. Umfang: (1) Neue nullable Felder `StandardCostVariable`/`StandardCostFixed` (Stueckpreis, Waehrung wie `StandardCost`) auf `SalesRecord`/`CentralSalesRecord`, Schema additiv (`AddColumnIfMissing`, `TEXT NULL`), Insert/Read-Pfade inkl. `CentralSalesDataProvider` und Audit-CSV (neue Spalten AM ENDE, aeltere CSV bleiben lesbar, leer -> null). (2) Import-Strecken koennen den Split aufnehmen: Manual-Excel-Header `standardcostvariable`/`standardcostfixed`, SAP-Mapping und Manual-Mapping unterstuetzen jetzt `decimal?`-Zielfelder (leere Quelle bleibt null statt 0 — wichtig, damit der DB offen bleibt statt falsch 100 %). (3) Gemeinsame Rechenlogik `Services/ContributionMarginCalculator.cs` (Vorzeichenregel wie Margen-Kostenbasis, Waehrungsregel ueber denselben Mask/Convert-Schalter `GroupMarginCostCurrencyMode`), genutzt von `ManagementCockpitService` UND `ExcelExportService` — Dashboard und Excel identisch. (4) Anzeige: Gruppenmarge-Reiter hat neue KPI-Kachel `Deckungsbeitrag (DB)`, DB-Spalte in Laender- und Detailtabelle (immer `-`, solange kein Split geliefert); zentrales Excel `Gruppenmarge Details` hat 4 neue Spalten am Ende (`Variable Unit Cost`, `Variable Cost Basis`, `Deckungsbeitrag (DB)`, `DB %` — Spalten W-Z, bestehende Formeln unveraendert), `Gruppenmarge Summary` hat `Deckungsbeitrag (DB)` (SUMIFS ueber Y) und `DB Zeilen` (COUNTIFS `<>`). DB-Summen laufen bewusst NUR ueber Zeilen mit geliefertem Split, Anzahl wird ausgewiesen. WICHTIG: Alle DB-Werte bleiben LEER, bis eine Quelle den fix/variabel-Split tatsaechlich liefert (CH/AT braeuchte eine SAP-Erweiterung analog WAVWR/STPRS, z. B. Planpreis fix/variabel aus der Kalkulation) — nichts wird geschaetzt. 9 neue Tests (`ContributionMarginCalculatorTests`, CSV-Roundtrip inkl. Null-Fall). Schulungs-Reiter `Standardkosten & Marge` und SVG auf Stand `vorbereitet` gebracht. ZUSAETZLICH (Wunsch Ingo, gegen Rueckfragen von Andreas): Das Blatt `Finance Filter Hilfe` im zentralen `Sales_All` enthaelt jetzt eine komplette FELDDOKUMENTATION — je Feld der Gruppenmarge-Blaetter Bedeutung und Berechnungsformel (Quantity, Unit Cost, Known Cost Basis inkl. Vorzeichen- und Konzernkostenregel, Margin/%, Supplier Type, Cost Source, alle Statuswerte, die 4 neuen DB-Spalten, Summary-Formeln) plus eine Tabelle `Woher die Standardkosten je Land kommen` (CH/AT WAVWR/STPRS, DE Alphaplan-Ableitung, B1 StockPrice, ES Sage, UK offen, TR-AG-Konzernkosten). NACHTRAG selber Tag: zusaetzlicher Abschnitt `Wo finde ich die Standardkosten in dieser Datei?` als Blatt/Spalte-Tabelle — klärt direkt in der Datei, dass `Sales` (Spalte X/Y) nur den unveraenderten Rohwert zeigt, `Finance Details` GAR KEINE Standardkosten-Spalte hat, und die eigentliche Berechnung (Kostenbasis, Marge, DB) in `Gruppenmarge Details`/`Gruppenmarge Summary` steht.
- DEPLOYED 2026-07-17 (siehe Eintrag oben, gleicher Commit/Deploy): Finance-Schulung (`/finance-cockpit/schulung`) um eigenen Reiter `Standardkosten & Marge` erweitert (`Components/Pages/FinanceTraining.razor`), inkl. neuer Prozessgrafik `wwwroot/training/standardkosten-margenfluss.svg` im Stil der bestehenden Keyuser-SVGs. Inhalt: Kostenquellen je Land (CH/AT WAVWR/STPRS, DE Alphaplan-Ableitung, B1 StockPrice, ES Sage-Spalte, UK offen, TR-AG-Konzernkosten), Rechenregeln (Stueckpreis, Menge x StandardCost, Vorzeichen bei Gutschriften, Marge CHF mit Jahreskurs, Mask/Convert-Schalter), Statusverhalten bei fehlenden Feldern (Standardpreis fehlt / Lieferant unklar inkl. Befund 2026-07-17 / Kostenwaehrung abweichend / Kurs fehlt), Fundstellen im Dashboard und zentralen Excel (Sales_All-Blaetter Gruppenmarge Summary/Details, Nachweis, Pruefbuch). Zusaetzlich fachlicher Input von Andreas als eigener Abschnitt dokumentiert: Deckungsbeitrag im zweiten Schritt (Umsatz minus variable Kosten; fix/variabel-Trennung entscheidend; SAP-Struktur enthaelt Planpreis fix/variabel getrennt) — mit klarer Abgrenzung, dass die App heute mit dem GESAMTEN Standardpreis rechnet und ein DB nach variablen Kosten weder berechnet noch im zentralen Excel ausgewiesen wird (Ausbauschritt, Entscheid bei Finance). Der von Ingo vermutete Ausweis "nach Abzug im zentralen Excel" existiert also noch NICHT.
- NEUER FUND 2026-07-17, nur dokumentiert (kein Code geaendert), globales Problem: Supplier-Felder (`SupplierNumber`/`SupplierName`/`SupplierCountry`) sind je Quelle strukturell leer statt nur lueckenhaft. CH/AT (`ZSCHWEIZ`, SAP OData) und UK (Manual Excel) haben dafuer im Seed-Mapping ueberhaupt keine Spalte vorgesehen — die Quellen liefern kein Lieferantenfeld; ES ebenso ohne Mapping; DE haengt am tatsaechlichen Alphaplan-Exportspaltenumfang; FR/IT/US/IN (SAP B1/HANA) liefern nur `OITM.CardCode`, den Standardlieferanten aus dem Artikelstamm (nicht den Beleglieferanten), leer wenn im Artikel kein Default-Lieferant gepflegt ist. Fachliche Tragweite: `GroupMarginSupplierClassifier.Resolve` liefert bei drei leeren Feldern `Unklar`, und `ManagementCockpitService.ResolveGroupMarginStatus` setzt dadurch IMMER `Lieferant unklar` — unabhaengig davon, ob eine Kostenbasis vorhanden waere. Direkte Konsequenz: die am 2026-07-16 gefuellte CH/AT-Kostenbasis (WAVWR/STPRS, TRCH 96.5 %, TRAT 99.9 %) ist in der Gruppenmarge-Sicht dadurch aktuell WIRKUNGSLOS — jede ZSCHWEIZ-Zeile bleibt mangels Supplier-Feldern auf `Lieferant unklar` maskiert. Gleiches strukturell fuer UK/ES. Neue offene Fachfrage an Andreas (noch nicht auf dem Multiple-Choice-Bogen): CH/AT als selbst verkaufende Trafag AG per Regel automatisch als eigene Lieferkategorie werten, statt ueber die leeren Supplier-Textfelder zu erkennen? Details: `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` Nachtrag 2026-07-17.
- DEPLOYED 2026-07-14 (Commit `8e0f51e`, `203/203` Tests gruen, DLL `14.07.2026 17:30:30`, Laenge `2'923'008`, Port 443 erreichbar, DB unveraendert): KOSTENBASIS DER GRUPPENMARGE fuer CH/AT und DEUTSCHLAND gefuellt — das alte Thema `StandardCost`, nicht das Journal. AUSGANGSLAGE (an Prod-Daten gemessen): ZSCHWEIZ 40'292 Zeilen mit 0 % Kosten (`StandardCost` war im Seed hart auf `=0` gemappt, weil der Umsatz-Service kein Kostenfeld liefert), TRDE 6'879 Zeilen mit 0 % (Mapping wartete auf eine Spalte `EinstandsPreis`, die der Alphaplan-Export gar nicht hat), TRUK 0 %, TRFR 51 %, TRSE 81 %, TRUS 92 %, TRIT 96 %, TRIN 99 %. BEWEIS AUS SAP (ABAP-Report `docs/abap/ZFIN_ANALYSE_STPRS_JOURNAL.abap`, Ausgabe in `stdpreis.txt`): `mbewSet` ist im Service `ZPOWERBI_EINKAUF_SRV` BEREITS vorhanden — kein neues SAP-Objekt noetig; Bewertungskreis 1100 (Trafag AG, CH, CHF) hat 65'447 Materialien mit 96.3 % `STPRS > 0`, Bewertungskreis 1200 (Trafag Ges.m.b.H., AT, EUR) 2'564 mit 99.6 %; von den tatsaechlich fakturierten Zeilen haben 96.5 % einen Standardpreis (`VBRP-WAVWR` waere mit 92.3 % die Alternative, ist aber im Z-Service nicht exponiert); `PEINH` ist aktuell durchgaengig 1. UMGESETZT: (1) neuer `SapGatewayStandardCostReader` liest `mbewSet` gepaged (`$top`/`$skip`, Filter auf `Bwkey`), Schluessel ist **Material UND Bewertungskreis** (sonst bekaeme die CH-Zeile den AT-Preis), Material ueber `MaterialKeyNormalizer` normalisiert (fuehrende Nullen); (2) `StandardCostEnricher` ordnet je Umsatzzeile ueber `Land` -> Bewertungskreis zu (CH=1100, AT=1200, per T001K aus dem Report bestaetigt) und setzt `StandardCost`; (3) `SapGatewayDataSourceAdapter` reichert nach dem Umsatzimport an — schlaegt das Kostenlesen fehl, laeuft der Umsatzimport weiter (Warning im Eventlog), damit ein Kostenproblem nie den Tagesexport eines Landes kippt; (4) Deutschland: `ManualExcelImportService.DeriveAlphaplanUnitCost` leitet den Einstandswert aus `NettoPreisGesamt - RohertragGesamt` ab — Alphaplan muss NICHTS liefern, das Feld war immer da und wurde nur weggeworfen. ZENTRALE FALLE (in allen drei Pfaden geloest): `StandardCost` MUSS ein STUECKpreis sein, weil `ManagementCockpitService.ResolveGroupMarginCostBasis` mit `Menge x StandardCost` rechnet. `STPRS` gilt pro `PEINH` Stueck, `WAVWR` und der Alphaplan-Rohertrag sind ZEILENSUMMEN — ohne Division durch Preiseinheit bzw. Menge waere die Kostenbasis um genau diesen Faktor zu hoch. 14 neue Tests, `203/203` gruen. NACHSORGE: naechsten Export abwarten und die Kostenquote fuer ZSCHWEIZ/TRDE gegen die SAP-Erwartung (96.5 %) pruefen; Gruppenmarge fachlich mit Andreas plausibilisieren.
- OFFEN, WICHTIG (2026-07-14, aus demselben ABAP-Report): Der Report zeigt fuer Buchungskreis 1100 **9'573 Fakturapositionen mit Datum 2026** (1200: 360) und 383'493 Buchungsbelege 2026 — unser Dashboard zeigt fuer CH/AT im Jahr 2026 aber NULL Zeilen. Die bisher dokumentierte Erklaerung "SAP liefert keine 2026-Daten" ist damit WIDERLEGT: Die Daten sind da, der Fehler liegt in unserem Weg dorthin (`FinanzdataSchweizOeSet` gibt bei `Gjahr eq '2026'` nichts zurueck). Verdacht: Die Z-View fuellt `Gjahr` nicht oder filtert hart. Eigener Arbeitsstrang, fachlich vermutlich wertvoller als die Kostenspalte, weil CH/AT dadurch das LAUFENDE JAHR nicht sieht. Zusatz: In `Sites.SapServiceUrl` steht `travt762` (Test), nicht `travp762` (Prod) — vor einer Umstellung mit Andreas abstimmen, weil sich der komplette CH/AT-Datenbestand aendern wuerde.
- DEPLOYED 2026-07-14 (Commit `935561f`, `189/189` Tests gruen, DLL `14.07.2026 11:24:26`, Laenge `2'907'136`, Port 443 erreichbar, DB unveraendert — Spalte `CompanyCode` wird additiv beim naechsten App-Start ergaenzt): CH/AT (`ZSCHWEIZ`) im Journal-Import — App-Seite komplett, SAP-Seite offen. Neuer OData-Reader `SapGatewayFinancialJournalReader` liest das ECC-Hauptbuch (`BKPF`/`BSEG`) ueber das EntitySet `FinanzJournalSet` mit Gateway-Paging (`$top`/`$skip`/`$orderby`, 1000er-Seiten) und `$filter` auf `Budat`; `FinancialJournalRefreshService` routet nach Anschlussart (HANA -> B1-Reader, SAP_GATEWAY -> OData-Reader), `IsJournalSite` akzeptiert jetzt auch Gateway-Standorte mit aufloesbarer Service-URL. Neue additive Spalte `FinancialJournalEntries.CompanyCode` (= `Bukrs`) trennt CH von AT; `JournalEntryId = Bukrs/Gjahr/Belnr`; Soll/Haben aus `Shkzg`+`Dmbtr`/`Wrbtr` (Soll positiv, Haben negativ); `TransactionCurrency` nur bei echten Fremdwaehrungsbelegen; `IsManual = Blart SA` (Annahme, Andreas bestaetigen); `IsReversal = Stblg gesetzt`; fuehrende Nullen bei Konto/Kostenstelle/Profitcenter entfernt. WICHTIGE ABHAENGIGKEIT: Das EntitySet `FinanzJournalSet` existiert auf `travp762` noch NICHT — Felddefinition, ABAP-Skizze und Abnahmekriterien fuer das SAP-Team stehen in `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md`; bis zum SAP-Rollout prueft der Reader die Service-Metadata und meldet dem Anwender klar, dass das EntitySet fehlt (kein Datenschaden, andere Gesellschaften laden normal). Navigation/Seed-Titel von `B1 Journal Import` auf `Journal Import` verallgemeinert (Force-Update im Seed), Schulungsseite Abschnitt 7 aktualisiert. Tests: `189/189` gruen (MapRow-Vorzeichen/Composite-Key/Storno/SA-Tests, Gateway-Routing-Test, Statusliste inkl. ZSCHWEIZ).
- DEPLOYED 2026-07-14 (Commit `2977c74`, `186/186` Tests gruen, DLL `14.07.2026 10:33:06`, Laenge `2'893'824`, Port 443 erreichbar): B1-Journal-Import umfasst jetzt auch INDIEN (`TRIN`, Schema `TRAFAG_LIVE`). Klarstellung von Ingo: Indien IST SAP B1, es ist in der Konfiguration nur falsch angeschrieben (Quellsystem-Code `SAGE`, eigener HANA-Server `20.197.20.60:30015`). Die Standortauswahl grenzt deshalb nicht mehr ueber den Quellsystem-Code `BI1` ein, sondern ueber die Anschlussart HANA + vorhandenes Schema (`FinancialJournalRefreshService.IsJournalSite`, umbenannt von `IsB1JournalSite`). Damit sind FR/IT/US/IN abgedeckt; CH/AT (SAP OData) und die Manual-Excel-Laender bleiben bewusst aussen vor. Zusaetzlich: `HanaFinancialJournalReader` prueft vor dem Lesen ueber `sys.tables`, ob `OJDT`/`JDT1` im Schema existieren, und wirft sonst eine klare fachliche Meldung statt eines rohen SQL-Fehlers (wichtig, weil der Dev-PC die HANA-Ziele nicht erreicht und die Indien-Tabellen noch nicht live geprobt sind). Tests: `186/186` gruen (1 neuer Indien-Ladetest, Auswahl-/Ablehnungstests auf FR/IN/UK/ZSCHWEIZ erweitert). NACHSORGE: beim ersten Indien-Lauf bestaetigen, dass `OJDT`/`JDT1` in `TRAFAG_LIVE` vorhanden sind. NAECHSTER SCHRITT (separat, mit Fable geplant): CH/AT-Journal ueber SAP OData — braucht eigenen Reader (`BKPF`/`BSEG`/`ACDOCA`) UND ein neues EntitySet auf SAP-Seite, da der aktuelle Z-Service nur Umsatzdaten liefert.
- DEPLOYED 2026-07-14 (Commit `8db6350`, `185/185` Tests gruen, DLL `14.07.2026 08:27:29`, Laenge `2'885'120`, Port 443 erreichbar, DB unveraendert publiziert — `FinancialJournalEntries` wird additiv beim naechsten App-Start angelegt): B1-Journal-Import in separate Tabelle `FinancialJournalEntries` fuer Konsolidierung/Analysen nach der Prioliste von Andreas. Neuer Import liest je B1-Gesellschaft (FR `fr01_p`, IT `it01_p`, US `us01_p`; Quellsystem `BI1`) die Hauptbuch-Buchungszeilen aus `OJDT`/`JDT1` plus `OACT`-Kontonamen und `OADM`-Hauswaehrung — volles Hauptbuch, bewusst OHNE den IT-Umsatzkontenfilter der Sales-Strecke. Feldumfang exakt nach Prioliste inkl. Betrag mit Vorzeichen (Soll-Haben), FiscalYear/Periode aus RefDate, `IsManual` (TransType 30), `IsReversal` (StornoToTr/AutoStorno). Mechanik wie gehabt, aber eigene Tabelle: zentraler HANA-Konnektor + Credentials wie `HanaDataSourceAdapter`, Full Load mit `ExportSettings.DateFilter` auf `RefDate`, transaktionales Ersetzen je TSC, Guardrail gegen 0-Zeilen-Ueberschreiben; Logging in `AppEventLogs` Kategorie `Journal` (nicht `ExportLogs` — Heartbeat bleibt sauber). Neue Seite `Finance Cockpit > B1 Journal Import` (`/finance-journal-import`, Seed `finance-journal-import`) mit Laden je Gesellschaft/alle, Zeilenzahl, Buchungsdatum von/bis, letzter Load. Schema additiv via `EnsureFinancialJournalEntriesTable` (Create-if-not-exists + Indizes + Unique `(Tsc, JournalEntryId, JournalEntryLineId)`), keine Migration. 9 neue Tests; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `185/185` gruen. VOR ERSTEM PRODUKTIVLAUF: B1-Spaltennamen (`ProfitCode`, `OcrCode2`, `FCCurrency`, `StornoToTr`, `AutoStorno`) einmal live gegen `fr01_p` proben. Details/Feldmapping: `docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md`.
- DEPLOYED 2026-07-13 (Commits `78d2772`, `2a94395`, `176/176` Tests gruen, DLL `13.07.2026 21:03:09`, Laenge `2'836'992`, Port 443 erreichbar, DB unveraendert): Daten-Heartbeat-Ausbau (Exportlauf-Streifen aus `ExportLogs`, 7-Tage-Glaettungsschalter, erweiterter Excel-Export) und UK-Selbstfuetterungs-Fix (siehe zwei Eintraege unten fuer Details). NACHSORGE nach diesem Deploy: UK-Export einmal laufen lassen und den neuen Bestand/den Wert der Rechnung 0000043747 fachlich pruefen; ZSCHWEIZ hat weiterhin 0 Zeilen fuer 2026 (SAP-seitiges Problem, nicht Teil dieses Deploys).
- ROOTCAUSE GEFUNDEN + GEFIXT am 2026-07-13 (jetzt deployed, siehe Eintrag oben): UK/TRUK hatte nur noch 2 Zeilen, weil der Manual-Import sich SELBST fuetterte. Beweiskette aus Prod-AppEventLog: `Neueste SharePoint-Datei ausgewaehlt | Import/Finance/UK_B1/Sales_ProcessedMergeInput_TRUK_2026-07-13.csv` -> App liest ihre eigene Audit-CSV vom Vortag als "UK-Quelle" (2 Zeilen, Rechnung 0000043747), ersetzt damit `CentralSalesRecords` fuer UK (`Geloescht=2 | Neu=2`) und laedt danach wieder eine neue Audit-CSV nach `UK_B1` hoch. Aktiv seit Audit-CSV-Upload produktiv (~30.06.); echte UK-Dateien (`ddMMyy_TRUK.xlsx`, z. B. `070726_TRUK.xlsx`) verloren fast immer das "neueste Datei"-Rennen gegen die eigene CSV. Zweites Problem: auch bei korrekter Dateiwahl las der Tageslauf (ohne Importjahr) NUR die neueste Delta-Datei und ersetzte damit den ganzen UK-Bestand (ExportLogs: taeglich 2-23 Zeilen). FIX (Commit siehe unten): (1) `SharePointUploadService.IsOwnExportOutputFile` schliesst eigene Ausgaben (`Sales_ProcessedMergeInput_*`, `Sales_<TSC>_<yyyy-MM-dd>.*`) aus der Import-Kandidatenauswahl aus — SharePoint- und Lokalordner-Pfad; genuine Muster wie `070726_TRUK.xlsx` und `Sales_TRUK_2025.xlsx` (Jahresdatei) bleiben zulaessig. (2) Ordner-Import ohne explizites Jahr nutzt jetzt das Basis+Delta-Modell: neueste Jahres-/Basisdatei plus ALLE neueren datierten Deltas zusammen, generische Dedupe (`SourceLineId`, sonst Invoice/Position/Material, spaetere Datei gewinnt, `DeduplicateManualSalesRecords`); ohne Basisdatei werden alle datierten Deltas gemeinsam gelesen. 9 neue Tests; `dotnet test TrafagSalesExporter.sln` mit `176/176` gruen. NACHSORGE nach Deploy: UK-Export einmal laufen lassen und pruefen, ob `UK_B1` eine gueltige Jahres-/Basisdatei enthaelt (sonst ergibt sich der Bestand nur aus den vorhandenen Delta-Dateien); Zeile mit 130'900 GBP aus Rechnung 0000043747 fachlich pruefen (Wert koennte durch die Selbstfuetterungs-Schleife verfaelscht worden sein).
- Neu umgesetzt und getestet am 2026-07-13 (jetzt deployed, siehe Eintrag oben): Daten-Heartbeat-Ausbau nach Prod-Datenanalyse. Befund: Die vielen "Unterbrechungen" waren ueberwiegend echte Datenmuster, nicht Heartbeat-Fehler — ZSCHWEIZ hat seit Juni 0 Buchungen an jedem Tag (alle 40'292 CSV-Zeilen sind 2025; passt zu `FinanzdataSchweizOeSet Gjahr eq '2026' = 0`), TRUK ist mit 2 Zeilen faktisch leer, TRIT/TRUS/TRFR fakturieren in Batches mit vielen echten Null-Tagen; nur TRSE/TRIN liefern annaehernd taeglich. Nebenbefund: 11.07. (Sa) lief kein Timer-Export, 12.07. erst 19:46 als Catch-up — App-Pool `AlwaysRunning`/`idleTimeout=0` am Server weiterhin offen. Umgesetzt deshalb: (1) zweiter SVG-Streifen `Exportlauf` je Tag aus `ExportLogs` (`ManagementCockpitService.ApplyHeartbeatExportRuns`, pure/statisch: gruen OK-Lauf, rot nur Fehler-Laeufe, orange kein Lauf ab erstem Log im Fenster, hellgrau davor/unbekannt), Kopfzeile mit `Letzter Export OK` plus Warn-Chip fuer Tage ohne Lauf/Fehler — trennt Update-Gesundheit von Geschaeftsaktivitaet; (2) Schalter `7-Tage-Summe` glaettet Linie/Flaeche ueber `RollingRowCount7` (Berechnung in `BuildDataHeartbeatDays`); (3) Excel-Export um `RollingRowCount7`, `ExportRun`, `LastSuccessfulExportUtc`, `ExportMissedCount`, `ExportErrorCount` erweitert. 4 neue Unit-/Integrationstests; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `167/167` gruen. Fachlich zu eskalieren (kein Graph-Thema): ZSCHWEIZ-2026-Daten fehlen SAP-seitig komplett; UK liefert faktisch nichts.
- Neu umgesetzt, gefixt, committed und deployed am 2026-07-13: Finance-Daten-Heartbeat unter `Management Analyse > Experten > Daten-Heartbeat` (`management-cockpit?section=heartbeat`, Seed-Key `finance-heartbeat`). Der Reiter nutzt denselben zentralen Finance-Datenpfad wie Summary/Pivot (Audit-CSV bevorzugt, DB-Fallback), rendert je TSC/Land ein Inline-SVG mit Tageslinie und farbigem Heartbeat-Streifen, bietet 30/60/90 Tage/laufendes Jahr und `Export to Excel`. Statuslogik nach Live-Fix: Zeilen > 0 OK; Tage ohne Buchungen bleiben neutral, solange der Standort frisch aktualisiert wurde; bei fehlendem Freshness-Zeitstempel wird nach dem letzten Datentag `Warn` angezeigt; altes LastUpdate >2 Kalendertage erzwingt `Gap` fuer Tage nach dem letzten Datentag. `LatestStoredAtUtc` bleibt primaer, `ExtractionDate` ist Fallback fuer `Letztes Update`; `TRES`/`TRSE` wird Spanien zugeordnet. Commits: `abc59e3` Feature, `2cf227c` Routing-Fix, `aff78dd` Gap-Logik-Fix. Tests: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `163/163` gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$`, `app_offline.htm` entfernt, Port 443 erreichbar.

## Offene Punkte aus aelteren Eintraegen (Original im Archiv)

- Server/IIS (seit 2026-07-08, nur direkt am Server moeglich, WinRM gesperrt): App-Pool `startMode=AlwaysRunning` + `processModel.idleTimeout=00:00:00` setzen, damit der 12:00-Timer ohne vorherigen HTTP-Request laeuft. Bis dahin holt `CatchUpMissedRunAsync` verpasste Tageslaeufe beim naechsten Prozessstart nach.
- Betriebshinweis DE/Alphaplan (seit 2026-07-03): Der Alphaplan-Upload nach SharePoint muss VOR dem 12:00-Timer laufen, sonst verwendet der Tagesexport noch den vorherigen ZIP-Stand.

## Aeltere Eintraege / Historie

- Kurzstand-Eintraege 2026-06-04 bis 2026-07-08 und alle Nachtrag-Abschnitte (Mai/Juni 2026): verbatim in `docs/raw_md_archive/LASTCHANGE_ARCHIV_bis_2026-07-12.md`.
- Kanonische Detailhistorie davor: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; Original-Volltexte: `docs/raw_md_archive/original_history_raws.zip` (nur zur Wiederherstellung).

## Einstieg / Router

- Themenrouter (zuerst laden): `docs/RAG_ROUTER.md`.
- Fuehrender Kurzkontext: `docs/rag/PROJECT.md`.
- Naechster Chat: `docs/RAG_ROUTER.md` -> diese Datei -> passende Themen-Kurzdatei aus `docs/rag/`.
