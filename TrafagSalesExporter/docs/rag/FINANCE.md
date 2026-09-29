# RAG Finance

Stand: 2026-09-29 (Konsistenzbefunde ISS-018, TR IT Moving Average und Referenzkost nachgefuehrt; Management-Cockpit-Tempo vom 2026-09-28; uebriger Kurzstand vom 2026-09-10)

Live-Abgleich vom Juli fuer UK-2025, Supplier-Felder und
`GroupStandardCosts`: `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.
Das ist eine **historische Messreferenz vom Juli 2026**, kein Vorrang fuer heute: sie
kennt zum Beispiel nur Schweizer Konzernkosten. Vorrang hat nach `router.md` Regel 1 immer der
juengste direkt gepruefte Beleg zum jeweiligen Thema.

Formeln/Mechanik: `docs/rag/FINANCE_FORMELN.md`. Historische Messungen und
ersetzte Zwischenstaende stehen in den Detaildokumenten und in
`docs/raw_md_archive/`.

## Kurzstand

- **TEMPO MANAGEMENT-COCKPIT 2026-09-28:** Das Oeffnen dauerte lokal 41 s, auf dem Server Minuten.
  Ursache laut Profil: `CurrencyExchangeRateService.ResolveRate` fragte je Aufruf die DB ab, fuer
  jede der rund 110'000 Verkaufszeilen mehrfach (92 % der Finanzauswertung), dazu wurden die
  Audit-CSVs bei jedem Oeffnen neu eingelesen. Jetzt Kurse im Speicher (Schreiber melden
  Aenderungen ueber `NotifyRatesChanged`) und Datensaetze gecacht, solange sich die Quelle nicht
  aendert: lokal 3,7 s. Commit `5a26596`. Details `docs/PLATTFORM_TEMPO_2026-09-28.md` Abschnitt 6.
 . "- MARKTSEGMENTE, AUTOMATIK GEMESSEN 2026-09-10: Die Segmentzuweisung laeuft **fuer Deutschland
  bereits automatisch**, fuer die anderen acht Standorte nicht, und der Unterschied liegt am
  Quellsystem, nicht am Programm. Von 196 Zuordnungen sind 25 bestaetigt, davon **24 aus TRDE**
  mit Quelle `Alphaplan Kundenstamm / Branche`; die uebrigen 171 sind weiter
  Namensabgleich-Vorschlaege aus der Marktumfrage. Fuellgrad `CustomerIndustry`: TRDE 7'433 von
  7'622 Zeilen und 543 Kunden, TRFR 221/20, TRIN 21/6, TRIT 10/2, TRSE/TRUK/TRUS/ZSCHWEIZ **0**.
  ZSCHWEIZ ist mit 52'276 Zeilen der groesste Standort und hat kein Branchenfeld.
  **Fund:** die 543 deutschen Kunden verteilen sich auf 38 Branchenwerte und decken
  6'887'039 von 7'033'623 EUR DE-Umsatz ab (97,9 Prozent); genutzt werden bisher nur die 24 mit
  `00 Bahn`. In `CustomerMarketSegments` existiert ueberhaupt nur ein Segmentwert, `Railway`.
  **Naechster Schritt ist kein Code, sondern ein fachlicher Entscheid** Branche auf Segment,
  rund 38 Zeilen, beim Vertrieb beziehungsweise Andreas. Damit ist auch die Vorfrage aus
  ISS-014 beantwortet: das Segment haengt am KUNDEN, Schluessel ist Kundennummer plus
  gepflegte Branche. Vollstaendige Liste und zwei Datenmaengel im Kundenstamm (Nummer `52`
  doppelt belegt, zwei Werte mit Komma am Ende) in
  `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` Abschnitt 18.
"
- BAHNMARKT DE, SCHLUESSEL GELOEST 2026-09-09 12:51: Rohail Munir hat
  `Rechnungen_20260909.xlsx` geliefert, 60'539 Rechnungen mit `Rech.-Nr.` und der
  **fachlichen** `Adressnr._R`. Die interne `RechnungsAdressenID` wird als Bruecke nicht
  mehr gebraucht. Geprueft: deckt 7'531 der 7'615 DE-Zeilen, stimmt in 4'455 Faellen mit
  dem bisherigen Belegnachweis ueberein, unabhaengig gegen `docs/2025_DataExport_DE.xlsx`
  mit 99,66 Prozent bestaetigt, alle 358 neuen Nummern im Kundenstamm auffindbar.
  `Bahnmarkt_DE_Kundenzuordnung_2026-09-09.xlsx` ist darauf neu gebaut: 7'592 von 7'622
  Zeilen zugeordnet, 30 offen, 24 Bahnkunden, 657 Bahnzeilen. Deutscher Bahnumsatz
  erstmals belegbar: 2025 rund 578'636 EUR, 2026 bis 09.09. rund 432'237 EUR.
  **Produktiv nachgezogen am 2026-09-09 um 14:37**: 3'043 Zeilen in einer Transaktion,
  fachliche Nummern von 4'549 auf 7'592, Segmente von 19 auf 24, `quick_check` `ok`,
  Zeilenzahl und `SalesPriceValue` mit `7'033'623.00` unveraendert. CSV und Sales-Excel im
  Serverordner `output` und auf SharePoint ersetzt und zurueckgelesen; in der CSV null
  Zellen ausserhalb der vier Kundenspalten, in der Excel null fremde Zellen und null
  Formeln veraendert. Vier Routen HTTPS `200`. Sicherung
  `trafag_exporter.db.before-de-customer-backfill-20260909-143601.bak`.
  **Offen bleiben 30 Gutschriftenzeilen** ohne fachliche Nummer, weil Rohails Datei keine
  Gutschriften enthaelt, sowie die 40 Zeilen der drei Doppelnummern Sonepar, EMS und
  Magnetic Sense, die der Nachzug bewusst nicht angefasst hat. Rohails Branchenspalte ist
  unbrauchbar; die Branche kommt aus dem Kundenstamm. Detail:
  `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md` Abschnitt 9.
- BAHNMARKT DE, ERSTER NACHZUG 2026-09-09 07:20, **durch den Nachzug um 14:37 ueberholt**:
  Von 7'615 deutschen Verkaufszeilen wurden
  4'549 ueber die Kombination aus Rechnungsnummer und interner Alphaplan-Adress-ID
  einer fachlichen Kundennummer, Name, Land und Branche zugeordnet. 19 DE-Kunden mit
  einer reinen Bahnbranche sind als `Railway` bestaetigt; bestaetigte Gegenentscheide
  bleiben erhalten. Mischbranchen werden nicht automatisch bestaetigt. 3'066 Zeilen
  bleiben als `ALPHAPLAN-ID:<ID>` sichtbar offen: 2'427 historisch ableitbare Kandidaten,
  624 ohne Kandidat und 15 Konfliktzeilen. Finanzwerte, Mengen und Lieferantenschluessel
  sind unveraendert. Dashboard-CSV und Sales-Excel sind auf Server und SharePoint
  verifiziert. Weitergabedateien: `Bahnmarkt_Rohail_2026-09-09.xlsx` und
  `Bahnmarkt_DE_Kundenzuordnung_2026-09-09.xlsx`. Detail und Grenzen:
  `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md`, Abschnitt 8.
- JOURNAL-FELDER, 2026-09-08: `JDT1.DueDate` live in FR/IT/US/IN vollstaendig belegt
  (469'661 Zeilen ab 2025), durch Modell/Schema/Reader/Finance_All implementiert und
  **am 2026-09-08 um 14:02 produktiv deployed** (Commit `646a998`, 675/675 Tests gruen).
  Gefuellt ist das Feld erst nach einem erneuten Ladelauf je Gesellschaft; der
  SAP-Gateway-Leser fuer CH/AT ist nicht mit angepasst. `ProfitCode` und `OcrCode2-5` sind in allen
  vier Quellen leer, Dimension 2-5 inaktiv. Konzernkonto wartet auf Andreas' Mapping.
  Neues Exportblatt `Feldstatus` weist die tatsaechliche Belegung aus. Detail:
  `docs/FINANCE_JOURNAL.md`.
- AUSGLEICHSFELDER, 2026-09-09 (ISS-006.1): fuenf Felder je Buchungszeile eingebaut und in
  `Finance_All` ausgewiesen — `ClearingDate` (`JDT1.MthDate`), `ReconciliationDate`
  (juengstes `OITR.ReconDate`), `ClearingReference`, `ClearingCount` und
  `IsClearingCancelled`. Live gemessen ab 2025: FR 5'680 von 20'198, IT 64'677 von
  162'833, US 9'902 von 21'816 Zeilen mit Ausgleichsdatum. Indien ist ungemessen: der
  Standort war am 2026-09-09 auch mit VPN weder per Ping noch per TCP erreichbar, waehrend
  FR/IT/US ueber dieselbe Verbindung antworteten.
  **Zwei Fallen, die die Umsetzung bestimmt haben:** `JDT1.IntrnMatch` ist in FR/IT/US
  durchgehend `0`/`-1` und als Bruecke unbrauchbar, verknuepft wird ueber
  `ITR1.TransId`/`TransRowId`; und eine Zeile kann mehrfach ausgeglichen sein (FR 56,
  IT 559, US 324, bis zu neun Vorgaenge), weshalb vor dem Join aggregiert wird — ein
  direkter `JOIN` haette den Journalsaldo verfaelscht. Dritter Befund: `ReconNum` wird
  nicht in Datumsreihenfolge vergeben, die hoechste Nummer gehoert bei FR 374 von 1'412
  mehrfach ausgeglichenen Zeilen nicht zum juengsten Ausgleich; die Unterabfrage laeuft
  deshalb zweistufig. Die zweistufige Fassung ist live gegengezaehlt: `basis = produktiv`
  bei FR 20'198, IT 162'843, US 21'816, abgeleitete Zahlen unveraendert. Werkzeug
  `.tmp_tools/JournalClearingProbe0909`; nach jeder Aenderung an der Ausgleichs-Query
  erneut laufen lassen, weil die Unit-Tests nur Teilzeichen pruefen und einen
  HANA-Syntaxfehler nicht fangen — genau der ist beim ersten Versuch aufgetreten.
  Bewusst **nicht** eingebaut: ein
  abgeleitetes `date paid` und die Zahlungsbelege `ORCT`/`OVPM`; welche der vier
  Bedeutungen fuehrend wird, entscheidet Andreas. Gefuellt sind die Spalten erst nach
  einem erneuten Ladelauf je Gesellschaft. **Nicht deployed.** Detail:
  `docs/FINANCE_JOURNAL.md`, Abschnitt „Ausgleichsfelder".
- ES BUCHUNGSDATUM, STAND 2026-09-02: das Feld ist eingebaut, live geprueft und die
  Fachfrage ist entschieden. Die spanische Export-SQL selektiert
  `FacturasTB.FechaAsiento` als `PostingDate` und `FacturasTB.Asiento` als
  `PostingDocument`, per `OUTER APPLY` mit `TOP 1` statt `JOIN` (70 von 3'642
  Rechnungsschluesseln haben mehrere Buchungszeilen, ein `JOIN` haette den
  spanischen Umsatz vervielfacht). Der Schluessel
  `CodigoEmpresa`/`Ejercicio`/`Serie`/`Factura` ist am 2026-08-17 auf dem spanischen
  Server live bestaetigt, 53 von 53 Treffern auf einem gebuchten Fenster.
  **Fachentscheid Andreas vom 2026-08-26: mit Buchungsdatum ist das RECHNUNGSDATUM
  gemeint.** Umgesetzt als Regel `UseInvoiceDate` mit `ScopeKey = ES`, produktiv
  deployed am 2026-08-26 15:26 (Commit `91830c2`). Die Umstellung war
  wirkungsneutral: ueber `100'558` Zeilen wechselt keine einzige das Jahr.
  Fuellgrad selbst gemessen am 2026-09-02 gegen `Sales_All_2026-09-01.xlsx`:
  `PostingDate` `1'523/7'071` (21,5 %), `InvoiceDate` `6'838/7'071` (96,7 %).
  ~~OFFEN ist nur noch, dass Santi Gomez die 7-Tage- gegen die 35-Tage-Version des
  Exportskripts tauscht~~ (ueberholt: am 2026-09-02 entschieden, die 7-Tage-Version bleibt;
  seit Mitte Juli kommen aber gar keine neuen Spanien-Zeilen an, `ISS-020`); die absolute Zahl `1'523` steht seit dem 2026-08-26
  unveraendert, es kommen also derzeit keine neuen Buchungsdaten nach. Details:
  `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` Abschnitte 8 bis 12.
- UK 2025 ABGENOMMEN 2026-08-11: `3'529'861.80 GBP` = 99.7 % des Finance-Solls
  `3'538'972`, Marge +33.8 % statt −502.7 %, `1'867` Zeilen. Der bis dahin
  gefuehrte Wert `394'439` war ein Stueckpreis-statt-Zeilenwert-Fehler aus dem
  Backfill vom 2026-07-28. Nachweis:
  `docs/FINANCE_UK2025_WERTFEHLER_2026-08-10.md` Abschnitt „Abnahme 2026-08-11".
- LIVE-PRUEFUNG 2026-07-31: TRUK enthaelt 1'867 Zeilen fuer 2025 und 1'090
  fuer 2026; UK ist in allen drei Supplier-Feldern vollstaendig. Insgesamt
  sind 77'466 von 95'396 Verkaufszeilen in allen drei Supplier-Feldern leer.
  `GroupStandardCosts` enthaelt 63'506 Werte fuer Bewertungskreis 1100/CHF.
  Details: `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.
- B1-Upgrade: Go-live ueber alle Tochtergesellschaften ist fuer 2026-08-03
  angekuendigt. Danach Importlaeufe FR/IT/US/IN, `StandardCost`-Fuellgrad und
  `EvalSystem` erneut pruefen; Details in
  `docs/FINANCE_STANDARDKOSTEN.md`.
- Konzernkosten stehen fuer genau drei Gesellschaften: TR AG aus MBEW-STPRS (Kreis
  1100, CHF), TR IT und TR IN aus dem je Material juengsten positiven B1-Belegwert
  `INV1/RIN1.StockPrice` (EUR bzw. INR). Produktiv seit 2026-08-25 15:21 und um 16:15
  nachgemessen: 6'119 Zeilen ueber 1'242 Materialien fuer TR IN, 112 Zeilen ueber 40
  Materialien fuer TR IT; 6'231 Zeilen sind von `Interner Standardpreis` gewechselt.
  A2/A3-Kostenbasis faellt um rund 26 %, Indiens eigene Kostenbasis steigt 2025 um
  10.5 %. Die zuvor `32` maskierten Fremdwaehrungszeilen werden seit dem Deploy vom
  27.08.2026 mit Tageskurs umgerechnet. Detail: `docs/FINANCE_STANDARDKOSTEN.md`.
- TR IT: Fuer den ersten Schritt ist `INV1.StockPrice` als Kostenbasis
  freigegeben. **Moving Average (`ISS-007.1`): Stand 2026-09-29 laut Paolas SAP-Beratern
  am bestehenden Artikel umstellbar, aber nur mit Bestand null, abgeschlossenen Belegen,
  Betriebsunterbruch und Inventur.** Die fruehere Aussage vom 2026-09-04 (unabhaengig vom
  Bestand nicht aenderbar, nur Neucodierung ueber 31'600 Artikel) ist ueberholt. Andreas hat
  die Umstellung am 2026-09-09 zurueckgestellt, erneute Betrachtung 2027. **Kein
  Reporting-Blocker**, weil `INV1.StockPrice` auf Belegebene davon unabhaengig ist.
  **Referenzkost (`ISS-007.2`)** aus `OITM.StockValue / OITM.OnHand` ist am 2026-09-09
  gemessen und abgestimmt; Italien baut sie laut Mail vom 2026-09-29 mit seinen Beratern
  nach; Ingos Rueckmeldung an Paola ist am 2026-09-29 versendet, offen ist ihre Antwort zum
  bilanziellen Bestandswert. Details:
  `docs/FINANCE_IT_BEWERTUNGSMETHODE_PAOLA_2026-09-04.md`,
  `docs/FINANCE_IT_REFERENZKOSTEN_2026-09-09.md`.
- **Kostenkaskade entschieden am 2026-09-09:** externer Lieferant heisst lokaler
  Standardpreis, interner Lieferant heisst Kosten der **ersten** liefernden
  Konzerngesellschaft und dann Schnitt, ohne Kettenaufloesung. Intern sind nur TR AG,
  TR IN und TR IT. Das ist der heutige Standard im Code, also kein Umbau. Damit ist die
  Frage nach dem Schweizer `STPRS` unabhaengig von der liefernden Gesellschaft mit nein
  beantwortet. Akzeptierte Ungenauigkeit: Thermostate Italien aus Indien laufen ueber
  Preisliste minus 30 Prozent, rund 200-300k Gruppenumsatz, von Andreas bewusst in Kauf
  genommen. Eine zweite Stufe ist zurueckgestellt, nicht verworfen. Details:
  `docs/FINANCE_STANDARDKOSTEN_SCHNITT_ANDREAS_2026-09-09.md`.
- **Entscheid vom 2026-09-09:** Andreas gibt der Umstellung auf Moving Average keine
  Prioritaet, erneute Betrachtung im naechsten Jahr; Rueckmeldung an Paola am 2026-09-29 versendet.
  Stattdessen fragt er nach einer Referenzkost je Artikel aus Bestandswert geteilt durch
  Bestandsmenge. Der Teil "letzter Wert fuer Artikel ohne Bestand" ist mit
  `INV1.StockPrice` bereits produktiv; die Bestandskennzahl ist neu, der Artikelstamm als
  Quelle gemessen ausgeschlossen. **Am 09.09. gemessen und beantwortet: ja, beide Werte
  liegen auf Artikelebene vor.** `OITM.StockValue` und `OITM.OnHand` sind gepflegt (1'077
  Artikel, 987'909.28 EUR, 62'601 Stueck) und stimmen auf den Cent mit dem kumulierten
  Bestandsjournal, 0 Mengenabweichungen bei 9'323 Artikeln. `OITW.StockValue` ist dagegen
  durchgaengig null und `OBTN.CostTotal` ist NICHT der offene Bestandswert. Der Haken ist die
  Abdeckung: nur 568 von 2'216 der 2026 verkauften Materialien tragen Bestand, also 28.9 %
  des Umsatzes, und wo beide Werte existieren ist der Median des Verhaeltnisses zum Belegwert
  genau 1.00. Empfehlung deshalb: Belegwert bleibt fuehrend, Bestandskennzahl als monatliche
  Plausibilisierung. Sie traegt bei Trafag-Sachnummern die
  Intercompany-Ladung und gehoert deshalb an die Stelle der lokalen Standardkosten, nicht
  an die der Konzern-Herstellkosten. Details:
  `docs/FINANCE_IT_REFERENZKOSTEN_2026-09-09.md`.
- **Produktiv seit 2026-08-31 09:47:** Finance kann fuer
  IT/IN zwischen juengstem positivem B1-`StockPrice` (Default) und Durchschnitt aller positiven
  Werte des naechsten Standortimports waehlen. Fuer CHF kann zwischen Tageskurs (Default) und
  Jahresendkurs je Finance-Jahr umgeschaltet werden. Das CHF-Profil gilt einheitlich fuer
  Cockpit, Finance-Pivot, Pruefbuch, Nachweis-Excel und `Sales_All`. Produktiv nachgemessen:
  beide Spalten stehen auf dem bisherigen Verhalten, `LatestPositive` und `CurrentDailyRate`.
  Release-Tests `644/644` gruen. Die Hilfetexte in den Einstellungen nennen seit diesem Deploy
  auch die Abhaengigkeiten, also Vorrangkette, Wirkzeitpunkt und Grenzen. Detail:
  `docs/FINANCE_STANDARDKOSTEN.md`, Abschnitt 7c.
- Fuehrende fachliche Sicht ist `Finance Summary`; `Management Analyse` bleibt
  Diagnose-/Plausibilitaetssicht.

## Wichtige Regeln

- Hauswaehrung des Landessystems ist fuehrend.
- Wertbasis ist Nettofakturawert pro Position.
- Jahresabgrenzung ueber `PostingDate`, Fallback `InvoiceDate`, danach `ExtractionDate`.
- Gutschriften/Storno laufen als negative Beleg-/Positionszeilen.
- Budget-CHF ist Kontroll-/Reporting-Kandidat, nicht Standardabgleich.
- Gruppenmarge ist bis zur Fachfreigabe nur Pruefsicht, nicht fuehrender Finance-Abschlusswert.
- `DocumentRate` aus dem ERP ist ein gespeichertes Quellfeld; die App-Kurstabelle wird nur bei Anzeige-Waehrung, expliziter `ConvertCurrency`-Transformation oder Budget-CHF-Kandidat verwendet.
- Schalter fuer Finance/Revision: `Einstellungen > Export Einstellungen > Audit-CSV / nachvollziehbarer Datenfluss`.
- Supplier-Fallback ohne Sales Type oder expliziten Supplier: produktiver Default ist
  `MARC/Werk 1100 -> Intern / TR_AG / Schweizer STPRS`. Alternativ kann Finance seit dem
  Deploy vom 27.08.2026 16:12 die alte `MBEW/GroupStandardCosts-1100`-Regel oder direkt
  lokale Standardkosten ohne Lieferantenangabe waehlen. Davon getrennt ist die Kostenquelle
  bei internem Lieferanten steuerbar: Kosten der liefernden Gesellschaft (Default) oder
  Schweizer STPRS bei MARC 1100. Details: `docs/FINANCE_STANDARDKOSTEN.md`, 7a/7b.

## Offene Fachpunkte

- **Konsistenzbefunde vom 2026-09-29 (`ISS-018`), gegen den Code bestaetigt, nicht repariert.**
  Produktiv wirksam und sichtbar: der CHF-Schalter aendert die Gruppenmarge, das Pruefbuch
  nicht (A1); falsche Waehrungslabels bei CHF-Summen (A3) und beim Stueckpreis im Pruefbuch
  (A4); die Excel-Exporte aus dem Cockpit enthalten nur 1'000 Zeilen, vollstaendig ist allein
  das Nachweis-Excel (A5). Fachlich offen fuer Andreas: Gutschriften mit Menge 0 (B1),
  kostenlose Ware (B2), interner Lieferant ohne Konzernkosten meldet „OK" (B3.1). Details:
  `docs/FINANCE_REVIEW_2026-09-29.md`.
- `date paid` (ISS-006.1): die Ausgleichsfelder sind seit dem 2026-09-09 eingebaut, die
  Bedeutungsfrage ist offen. Andreas muss entscheiden, was fuehrend wird:
  Zahlungsbuchungsdatum, Ueberweisungsdatum, Datum des letzten Ausgleichs oder erst der
  vollstaendige Ausgleich. Erst danach eine einzelne fuehrende Kennzahl ableiten; bis
  dahin zeigt `Finance_All` die Quelllage. Offen bleiben zusaetzlich der Deploy samt
  Ladelauf je Gesellschaft und die Messung fuer Indien, das am 2026-09-09 nicht
  erreichbar war.
- Supplier-Mapping: 77'466 von 95'396 Live-Zeilen haben alle drei
  Supplier-Felder leer. Die Rohdatenluecke bleibt offen, die Ursache je Quelle ist
  weiterhin zu klaeren. Seit dem Deploy vom 2026-08-12 maskiert die Gruppenmarge
  aber nur noch die Zeilen ohne belastbare Pruefgrundlage: der CH-Werkstamm
  entscheidet intern gegen `Lokal`, und `Lieferant unklar` bleibt nur bei fehlendem
  Materialschluessel, fehlender TSC oder leerem MARC-Cache.
- B1-Upgrade ab 2026-08-03 nachpruefen: Import FR/IT/US/IN, Kostenfuellgrad
  und Bewertungsmethoden.
- TR IT: Bewertungsmethode ist seit 2026-09-09 entschieden und zurueckgestellt, Andreas
  priorisiert sie nicht und will das Gesamtbild im naechsten Jahr erneut betrachten; die
  Intercompany-Bewertung mit Lucas Castro ruht mit dem Projekt. Offen ist die Rueckmeldung
  an Paola und, als neuer Punkt `ISS-007.2`, die Messung der Referenzkost je Artikel aus
  Bestandswert geteilt durch Bestandsmenge; die Messung liegt vor, offen ist nur der
  Fachentscheid und eine Rueckfrage an Italien. Der freigegebene Belegebenen-Weg ueber
  `INV1.StockPrice` bleibt davon unabhaengig. **Paola hat am 10.09. geantwortet:** ihr Befund
  stammt von ihren SAP-Beratern, ein Workaround ueber Lucas Castro oder ANG ist nicht
  ausgeschlossen, und die Referenzkost will sie wegen Arbeitslast spaeter pruefen. Diese
  Pruefung ist erledigt und sollte ihr abgenommen werden.
- Budget-CHF: Finance muss Kurse/Freigabe, Pflegeprozess, Spaltenumfang,
  Fehlkursverhalten, Rundung und Anzeigeort entscheiden.
- CH/AT-Journal: SAP-EntitySet `FinanzJournalSet` ist seit dem 2026-09-11 auf
  **`T76/100` gebaut und geprueft** (beide Redefinitionen aktiv, `$metadata` und
  Datenabruf `HTTP 200`, Werte gegengelesen). **Produktiv fehlt es weiterhin:**
  Transport `T76K912530` ist nicht freigegeben, und die App liest `travp762`.
  Der Leser laedt seit derselben Aenderung je Buchungsperiode (`Gjahr` plus
  `Monat`) mit `$top=20000`; ein Jahreslauf dauert damit rund zwei Minuten
  statt der gemessenen 80. Spezifikation: `docs/FINANCE_JOURNAL.md`,
  Bau und Messungen: `docs/abap/README_FIN_JOURNAL_ENTITYSET.md`.

## Management-Analyse-Reiter

- `Finance Summary`: KPI-Karten und Summen wie im zentralen Excel.
- `Laender`: Ist, IC/2nd-party, Ist ohne IC, Soll, Differenz, Status, Quelle und TSC je Land/Waehrung.
- `Datenstatus`: Standortbestand, letzte Speicherung, letzter Export, Manual-Import-Hinweise.
- `Abweichungen`: Soll/Ist-Abweichungen sortiert nach Betrag.
- `Gutschriften`: technische Kandidaten ueber negative Werte und erkennbare Belegtypen/-nummern.
- `Datenqualitaet`: fehlende Materialnummern, ProductGroup, Waehrung, Kunde, Datum, Nullwerte und ausgeschlossene Zeilen.
- `Spartenanalyse > Finanzanalyse`: Umsatzabdeckung und Umsatz nach Produktsparte/Familie/PAPH1 auf Basis der TR-AG-Referenz.
- `Spartenanalyse > Zentrale Zuordnung`: Materialnummern aller Laender gegen TR-AG-Stamm pruefen.
- `Gruppenmarge`: Pruefsicht fuer Umsatz, bekannte Kostenbasis, offene Kostenbasis und belastbare Marge je Land/Sparte/Detail.
- `Finance Pruefbuch`: zeilenbasierte Excel-Pruefsicht fuer Originalwaehrung, CHF-Umrechnung, Lieferant, Standardkosten, Kostenbasis und Gruppenmargenstatus.
- `Rohdaten Diagnose`: direkte Plausibilitaets-/Rohdatensicht auf die zentrale Auswertungsquelle.
- `Daten-Heartbeat`: Datenkontinuitaet je TSC/Land mit Tageszeilen. Tage ohne Buchungen bleiben neutral, solange der Standort frisch aktualisiert wurde; fehlende Freshness wird als Warn angezeigt; ein altes Update (>2 Kalendertage) markiert Tage nach dem letzten Datentag als rote Gap-Segmente. Seit 2026-07-13 zusaetzlich: (1) zweiter Streifen `Exportlauf` aus `ExportLogs` je TSC/Tag (gruen = OK-Lauf, rot = nur Fehler-Laeufe, orange = kein Lauf nach erstem Log im Fenster, hellgrau = vor erstem Log/unbekannt) — trennt "Update lief nicht" sauber von "keine Buchungen an dem Tag"; Kopfzeile zeigt `Letzter Export OK` und einen Warn-Chip mit Anzahl Tage ohne Lauf/Fehler. (2) Schalter `7-Tage-Summe`: Linie/Flaeche zeigen die rollierende 7-Tage-Zeilensumme statt Tageswerte, damit Batch-Fakturierer (IT, US, FR) nicht staendig optisch einbrechen. Excel-Export enthaelt `RollingRowCount7`, `ExportRun`, `LastSuccessfulExportUtc`, `ExportMissedCount`, `ExportErrorCount`. Kernlogik: `ManagementCockpitService.ApplyHeartbeatExportRuns` (pure/statisch, mit Unit-Tests) und Rolling-Summe in `BuildDataHeartbeatDays`.

## Audit-CSV / Auswertungsquelle

- `Audit-CSV je Standort schreiben`: schreibt beim Laenderexport eine verarbeitete CSV nach Mapping und Transformation.
- `Zentrale Auswertung aus Audit-CSV`: zentrale Auswertungen lesen je TSC die neueste `Sales_ProcessedMergeInput_*.csv`; wenn keine Standort-CSV gefunden werden, wird die neueste zentrale `Finance_Dashboard_Audit_All_*.csv` als Fallback verwendet.
- Der Pfad ist der `Lokaler Standardpfad Standort-Dateien`; ein separater sichtbarer Audit-Pfad wird nicht verwendet.
- Standard ohne CSV-Schalter: zentrale Auswertungen lesen `CentralSalesRecords`.
- Wenn der CSV-Schalter aktiv ist und weder Standort-CSV noch zentrale `Finance_Dashboard_Audit_All_*.csv` vorhanden sind, ist die zentrale Auswertung nicht ausfuehrbar.

## Experten / 3D Datenanalyse

- Unter `Experten` gibt es den Punkt `3D Datenanalyse`.
- Zweck: Verlauf und Kennzahlen im Raum betrachten, nicht Ersatz fuer den offiziellen Soll/Ist-Wert.
- Funktionen:
  - drehbare 3D-Ansicht mit Maus.
  - Achsenbeschriftung fuer Zeit/Wert/Indikator.
  - Auswahl sinnvoller Finance-Indikatoren.
  - Diagrammarten wie Balken/Linien/weitere Analyseformen.
  - Sparten-Kreis je Land fuer Produktsparte-Anteile pro Land.
  - einstellbare Labelgroesse.
  - Schieberegler fuer Szenarien, u. a. Wechselkursveraenderungen.
  - Realtime-Neuberechnung bei Szenarioaenderungen.
- Bekannter Hinweis: Wenn Interaktion/Zoom in Firefox fehlerhaft ist, mit Chrome pruefen.

## Spartenanalyse Kurzlogik

- Statuswerte:
  - `Zugeordnet`: Material im TR-AG-Stamm gefunden und Sparte verwertbar.
  - `Übrige`: Material im TR-AG-Stamm gefunden, `ProductDivisionCode = 0008`; gueltige Sammel-Sparte, kein Fehler.
  - `Nicht zugeordnet`: TR-AG-Referenz vorhanden, aber `UNASS`/leer.
  - `Nicht im TR-AG-Stamm`: lokale Materialnummer hat keinen TR-AG-Treffer.
  - `Material fehlt`: Finance-Zeile ohne Materialnummer.
- Gruppierung:
  - `PAPH1 Detail`: feinste Hierarchie-Sicht.
  - `Produktfamilie`: Managementsicht fuer Familien wie Gas Density Monitor.
  - `Produktsparte`: oberste Verdichtung.
- `Top 10 anzeigen` filtert nur die Tabelle, nicht die Summary-Berechnung.
- Laender werden mit Flagge angezeigt.
- Icons sind rein visuell und werden aus Textmustern abgeleitet.

## Land-Kurzindex

| Land | Kurzregel |
| --- | --- |
| CH/AT | SAP OData `ZSCHWEIZ`, Trennung ueber Buchungskreis/Reporting-Land |
| DE | Alphaplan CSV-Paar `invoice_headers.csv`/`invoice_lines.csv`, Full + `delta`, `NettoPreisGesamt`, CreditNote/GS negativ, EUR |
| ES | Sage CSV, `ImporteNeto`, REC/Credit negativ; Referenz 2025 korrigiert auf `3'082'320.18 EUR` |
| IT | Hauswaehrung, `Trafag Italia` ausgeschlossen, Duplikatlogik fuer leeres Supplier country |
| UK | Sage/Manual Excel, GBP, `[Sales Price/Value] * [Quantity]`, Credit Notes negativ |
| IN | SAGE/HANA `TRIN`, Schema `TRAFAG_LIVE`, INR als Hauswaehrung |

## Rohquellen Nur Bei Bedarf

- Entscheide: `docs/FINANCE_ENTSCHEIDE.md`
- Finance-Schulung: `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md`
- Formeln je Land: `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`
- Isolierter Kurs-Workflow: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`
- IT Detail: `docs/FINANCE_IT_VORGEHEN_2026-05-18.md`
- UK Korrektur: `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md`
- ES Detail: `docs/STANDORT_ES_SAGE.md`
- alter Finance-Handoff: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
