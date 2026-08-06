# Last Change

Stand: 2026-08-06

WARNUNG fuer neue Sitzungen: `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md` Abschnitt 3 und
`docs/mails/Build-RanVijayFollowup.ps1` bitten Indien um Pflege von 1'271 Artikeln. Das ist
seit 2026-08-05 ueberholt und darf NICHT versendet werden — gueltig ist
`docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

Diese Datei ist fuer tokenarme RAG-Nutzung komprimiert.

## Offene Punkte (nicht erledigt)

- **Statustext `"OK"` steht als Zeichenkette in der Excel-Formel** des Nachweises
  (`Services/ExcelExportService.cs`, Blatt „Gruppenmarge Details", Spalten 19 und 20:
  `IF(B{Zeile}="OK",Q-R,"")`). Eine Umbenennung von `GroupMarginStatuses.Ok` liesse dort
  STILL alle Margen leer — der Compiler sieht nur einen String, und die Tests werten
  Formeln nicht aus. Kein aktueller Defekt, aber eine Falle fuer die naechste Umbenennung.
  Fix, Nachweisidee und Begruendung: `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md`
  Abschnitt 5a. Soll mit dem naechsten Deploy mitgehen.
- Waehrungsmaskierung (`status == OK && conversion.IsMasked`) steht an drei Aufrufstellen
  einzeln statt im Rechner — letzte gespiegelte Stelle der Rechnung.
- Mail an RanVijay (Cc Andreas) zu den offenen TRIN-Artikeln: Artikel-Liste vor dem Versand
  gegen den Datenstand 2026-08-06 neu ableiten, nicht die Datei vom 2026-08-05 wiederverwenden.
- Innenumsatz-Frage an Andreas: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 4a.

## Aktueller Kurzstand

- 2026-08-06, NEUE EINKAUF-/LOGISTIK-REITER LOKAL UMGESETZT, NOCH NICHT
  COMMITTED ODER DEPLOYED: fuenf getrennte additive Routen fuer
  Materialdisposition/Fehlteile, Bestellbedarf/Deckung, Materialabhaengigkeit,
  Dispositionspruefung und Lieferperformance-Datenstatus. Bestehende Spend-,
  Bestell-, Lieferanten- und Stuecklistenreiter sowie deren Berechnungen wurden
  nicht ersetzt. Filter wirken vor Kennzahlen, Prioritaetsbalken und Details.
  Echte OTIF bleibt wegen fehlendem Ist-Wareneingangsdatum sichtbar als
  Datenluecke. Details:
  `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md`.

- 2026-08-06, HR-KRANKENQUOTE UND FILTERVERTRAG DEPLOYED UND VERIFIZIERT
  (14:24 MESZ, Commit `9435a5d`, `438/438` Tests): Der
  Arbeitstage-Nenner zieht neu die neun gesetzlichen Feiertage des Kantons
  Zuerich ab (inkl. dynamischem Ostertermin; lokale/nicht gesetzliche Tage
  bewusst nicht). Bei nicht periodengenau eingrenzbaren Rexx-Absenzen zeigt
  jetzt auch die Uebersicht keine scheinbar genaue Absenzquote mehr; die Ampel
  bleibt gelb statt aus einem unzuverlaessigen Wert Rot/Gruen abzuleiten. Neuer
  Regressionstest prueft 128 Kombinationen aus Organisation, Kostenstelle,
  Mitarbeitertyp, Eintrittsjahr, GLZ, Restferien und Suche ueber alle sichtbaren
  HR-Ergebnisbloecke; ein weiterer Test kombiniert Zeitraum, Jahr,
  Fluktuationsfilter und alle Personenfilter. Details:
  `docs/HR_KPI_FEIERTAGE_FILTERTEST_2026-08-06.md`.

- 2026-08-06, ZDISPO NUR IM EINKAUF-SPEND-AUFRISS ERGAENZT, DEPLOYED UND
  VERIFIZIERT (13:57 MESZ, Commit `0a8a4c9`): `zdispo_grp.xlsx` ordnet
  Disponenten/-muster den DISPO-Gruppen zu, `zdispo_spart.xlsx` liefert die
  Produktnamen. Die Daten landen in der separaten Tabelle
  `PurchasingSpendDisponentRule`; die bestehende manuelle
  `PurchasingProductGroupMap` wird weder geloescht noch ueberschrieben und hat
  bei Treffern Vorrang. Exakte Regeln gewinnen vor Sternmustern. Doppelte
  Zuordnungen (`016`, `DS1`, `DS2`) bleiben getrennt und laufen in dieselbe
  summenerhaltende `1/n`-Allokation. Aenderung wirkt ausschliesslich in
  `Einkauf > Spend-Aufriss > Produktgruppe`, nicht in anderen Einkaufs- oder
  Finance-Sichten. Produktiv: `45` Regeln aus `42` Mustern, manuelle Map weiter
  `0`, `105` ZLO03-Zeilen mit Disponent; `D5` hat in der gelieferten Textdatei
  keinen Namen und erscheint deshalb als Code. `435/435` Tests, Startseite und
  direkter Aufriss HTTPS `200`; DLL `4'136'448` Bytes, SHA256
  `0F1CB29F6F766C8CB71903D45B78DB48B3AB94FE58638837F5376E9D2A9B01C1`.
  Details: `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

- 2026-08-06, EINKAUF PRODUKTGRUPPEN UND ABC/XYZ DEPLOYED UND VERIFIZIERT
  (12:31 MESZ, Funktionscommit `bb009bf`): Spend-Aufriss hat neu die Perspektive `Produktgruppe -> Lieferant ->
  Material`. `VknrDispo` wird aus ZLO03 persistiert; eine optionale
  `PurchasingProductGroupMap` bildet Disponent auf ZC23-Code/-Text ab. Fehlt die
  Referenz, zeigt die GUI ehrlich `Disponent <Code>`, unzugeordneter Spend bleibt
  als `ohne Produktgruppe` sichtbar. Mehrfach verwendete Komponenten werden
  gleichmaessig `1/n` auf unterschiedliche Gruppen verteilt, sodass die Summe
  erhalten bleibt. ABC/XYZ ist nun eine gemeinsame Massnahmenmatrix mit
  konkreten Pruefauftraegen, Spend, Materialien und Lieferanten. Gesamte Suite
  `435/435` gruen (darin `47/47` Einkauf/Schema und `6/6` Lokalisierung).
  Produktivartefakt: `BiDashboard.dll` `4'120'064` Bytes, SHA256
  `B5C72496A7A4E11AC38675D840A5DF9DBABA6999517DD70FE3D7C0CE07BAEC3C`;
  Startseite und `/einkauf/aufriss` HTTP `200`, `app_offline.htm` nicht aktiv.
  Produktivschema: `VknrDispo` und `PurchasingProductGroupMap` vorhanden,
  `105` ZLO03-Zeilen mit Disponent, manuelle ZC23-Map noch `0` Zeilen. Dieser
  Grundstand wurde um 13:57 durch die separate ZDISPO-Zusatzquelle ergaenzt;
  aktueller Stand siehe Eintrag direkt darueber. Details:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

- 2026-08-06, FINANCE-PRUEFBUCH WIES DEN VOLLEN UMSATZ ALS MARGE AUS — DEPLOYED UND VERIFIZIERT
  (Deploy 11:06, `BiDashboard.dll` `4'057'600` Bytes, SHA256 `E6CCF3C4…`, HTTP 200,
  `IsCostBasisKnown` in der ausgelieferten DLL belegt, Produktiv-DB unveraendert):
  beim Durchgehen der Finance-Anzeige gefunden. `BuildFinanceAuditLedgerRows` liess die Marge
  nur bei der Waehrungsmaske leer. Eine FEHLENDE Kostenbasis laeuft aber als 0 durch, also
  ergab „Umsatz minus Kosten" den vollen Umsatz — Spalte `Marge CHF` und `MarginPercent`
  zeigten 100 %, direkt neben dem Status, der „Lieferant unklar" bzw. „Konzernkosten fehlen"
  sagte. Betroffen sind die Pruefbuch-Tabelle im Cockpit UND der Excel-Export
  `Finance_Pruefbuch`. Der zentrale Excel-Nachweis war NICHT betroffen: dort steht die Marge
  als Blattformel mit `WENN(Status=OK)`. Naeherung ueber alle Jahre und ohne den
  `Include`-Filter (in SQL nicht nachbildbar): rund **71'900 von 96'059 Zeilen (~75 %)** haben
  keine belastbare Kostenbasis, im Wesentlichen `Lieferant unklar` bei TRCH/TRDE/TRES/TRAT.
  Neu entscheidet `GroupMarginStatuses.IsCostBasisKnown`. Die Unterscheidung ist noetig, weil
  `IsOpen` dafuer zu grob ist: bei „Kostenwaehrung abweichend" IST die Kostenbasis bekannt,
  nur in anderer Waehrung — die CHF-Marge bleibt dort korrekt rechenbar und wird weiter
  gezeigt (durch einen bestehenden Test gepinnt).
- 2026-08-06, ANZEIGE NACHGEZOGEN (im Deploy 11:06 enthalten): die Statusfarbe im Cockpit stand als
  eigene Aufzaehlung neben `GroupMarginStatuses.Open` und kannte „Kostenwaehrung abweichend"
  nicht — der Status wurde blau statt orange gezeigt, obwohl die Kennzahl „offene Kostenbasis"
  ihn mitzaehlt. Die Farbe folgt jetzt `IsOpen`, also der Statusdefinition selbst. Die
  Schulungsseite `Finance > Grundlagen` erklaerte „Konzernkosten fehlen" ueberhaupt nicht,
  obwohl der Status seit heute 137 indische Zeilen betrifft; die Tabelle fuehrt ihn jetzt
  mit der Abgrenzung zu „Standardpreis fehlt". Der Hinweistext im Gruppenmarge-Tab beschrieb
  noch die MVP-Regel von vor dem Konzernkosten-Umbau und ist jetzt die tatsaechliche
  Regelkette; derselbe veraltete Stand stand als Hinweis im Finance-Ergebnis
  („echte Konzern-Standardkosten sind noch nicht angebunden" — seit 2026-08-05 falsch).
  Kachel „Kostenbasis" heisst wie die Tabellenspalte „Bekannte Kostenbasis" —
  die Summe enthaelt offene Zeilen mit 0. `433/433` Tests gruen.
- 2026-08-06, DURCHGESEHEN UND IN ORDNUNG (Finance): Laenderstatus und die Kacheln
  „Laender OK"/„Zu pruefen" (Literale passen zum Erzeuger `BuildFinanceStatus`), Datenqualitaet,
  Gutschriftkandidaten, Sparten-/Produktfinanzen, Finance-Pivot (keine Kostenlogik enthalten),
  `BuildFinanceSummaryRow` (ausgeschlossene Zeilen tragen Wert 0, Summe ueber alle Zeilen ist
  daher gleich der Summe ueber die eingeschlossenen). Deckungsbeitrag ist ueberall „-", weil
  KEIN Standort einen fix/variabel-Split liefert (0 von 96'059 Zeilen gemessen) — korrekt
  angezeigt, das Feature ist heute aber wirkungslos. `EstimatedMarginTotal` im aelteren
  Cockpit-Teil rechnet Umsatz minus geschaetzte Kosten, wird aber nirgends angezeigt oder
  exportiert (toter Code). Vollstaendiges Pruefprotokoll:
  `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md`.
- 2026-08-06, DAS FELD IST PRODUKTIV ANGEKOMMEN: der TRIN-Export 06:54 fuellt Sales Type auf
  **6'664 von 7'094 Zeilen (93,9 %)** (`FFM` 5'923, `LRD` 718, `CM` 23, leer 430), Trafag-
  Sachnummer auf 3'625. **5'868** `FFM`/`CM`-Zeilen wechseln von „Lieferant unklar" auf intern.
  Von 718 `LRD`-Zeilen finden **581 die Schweizer Konzernkosten — ueber die lokale Artikelnummer
  waeren es 4**; die uebrigen 137 stehen auf `Konzernkosten fehlen` und weisen bewusst keine
  Marge aus. Andere Standorte 0 (nur Indien fuehrt diese UDFs).
- 2026-08-06, GRUPPENMARGE JETZT IN EINER KLASSE — DEPLOYED UND VERIFIZIERT (`515ab9d`,
  Deploy 09:41, SHA256 `CF750722…`, HTTP 200, neue Typen in der ausgelieferten DLL belegt).
  Die Kostenlogik stand doppelt da
  — `ExcelExportService` auf `SalesRecord`, `ManagementCockpitService` auf
  `FinanceAggregationRow`, 48 von rund 95 Zeilen identisch — und war beim Einbau von
  „Konzernkosten fehlen" bereits AUSEINANDERGELAUFEN: das Cockpit rief die Statusfunktion ohne
  das neue Kennzeichen auf und zeigte fuer dieselbe Zeile „Standardpreis fehlt", der Audit-Ledger
  kannte den Status gar nicht, Sortierung, Offen-Zaehler und Statusfarbe uebergingen ihn, und die
  Excel-Formel je Land widersprach der Gesamtsumme im selben Nachweis. Jetzt rechnet nur noch
  `Services/GroupMarginCalculator.cs`; beide Dienste bilden ihre Zeile auf `GroupMarginLine` ab.
  Die drei Abweichungen zur Kostenbasis sind benannte Regeln in einer geordneten Kette
  (`GroupStandardCost` → `GroupDistributionWithoutGroupCost` → `LocalStandardCost`), die
  Reihenfolge ist die Fachregel und wird getestet. Statuswerte, Offen-Definition und Sortierung
  stehen vollstaendig in `GroupMarginStatuses`, die Excel-Formeln werden daraus erzeugt.
  `GroupMarginConsistencyTests` schickt dieselbe Zeile durch BEIDE oeffentlichen Einstiegspunkte
  und verlangt gleiche Ergebnisse — ein reiner Test der Rechenklasse waere gruen geblieben,
  waehrend die Aufrufstelle das Ergebnis wegwirft. `431/431` Tests gruen (vorher `406`), Saldo
  −298/+158 Zeilen in den beiden Diensten. Details:
  `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 7d.
- 2026-08-06, DEUTSCHLAND-MAIL WAR FALSCH ADRESSIERT (`0f15b1e`): die Alphaplan-Export-SQL ist
  UNSERE (`AlphaplanExportPackage/scripte/alphaplanExport.ps1`), Lieferant und Kundenname fehlen,
  weil unsere Query sie nicht liest. Einzige echte Bitte an Rohail ist ein Schemaauszug. Gilt
  genauso fuer Spanien (`PostingDate` auf allen 5'504 Zeilen leer). Siehe `docs/rag/MANUAL_IMPORT.md`
  Abschnitt „Skripthoheit".
- CALL 2026-08-05, INDIEN: DAS FELD HEISST „SALES TYPE", PREFERRED-VENDOR-BITTE IST UEBERHOLT.
  RanVijay hatte eingewandt, dass viele Artikel bei TR IN lokal gefertigt werden — bei
  Eigenfertigung gibt es keinen Vorlieferanten, `OITM.CardCode` waere dort sachlich falsch
  gepflegt. Ergebnis des Calls: im indischen Artikelstamm gibt es das Feld „Sales Type" mit
  „full-fledged manufacturing" (Produktion im indischen Werk) und „LRD" (Import von Trafag
  Schweiz, Weiterverkauf; dort sind Lieferant Schweiz und Einkaufspreis laut RanVijay bereits
  gepflegt). Er erwartet, dass die 1'271 offenen Artikel damit auf „maybe 50 60" schrumpfen, und
  pflegt den Rest sofort selbst. PRODUKTIVDATEN STUETZEN DAS: `PT0` (laut Call LRD) hat 319
  Zeilen, davon nur 37 ohne Lieferant; `PS0`/`DM0`/`TS0` (2'469/2'223/1'590 Zeilen) sind fast
  durchgaengig ohne. TRIN gesamt: 6'236 Zeilen ohne Lieferant (1'278 Artikel, 1'057'121'097
  INR), ALLE mit Kostenbasis — es fehlt nur die Klassifikation. Die exportierte Artikelgruppe
  ersetzt das Feld NICHT (trennt nach Materialart, nicht nach Fertigungsort). OFFEN ist nur der
  technische Spaltenname; er wird ERMITTELT, nicht geraten. ENTSCHEID INGO: Eigenfertigung gilt
  als intern mit liefernder Gesellschaft TR IN, lokale Kostenbasis = Gruppenkostenbasis (kein
  IC-Aufschlag, gleiche Logik wie CH/AT). Umsetzung landet in UNSERER Query
  `Services/HanaQueryService.cs` (OITM bereits gejoint, Query steht ZWEIMAL: OINV/INV1 und
  ORIN/RIN1) und in `Services/GroupMarginSupplierClassifier.cs`; pauschale TSC-Regel wie CH/AT
  ist bei TRIN unzulaessig, 141 Zeilen sind echte indische Fremdlieferanten. Der Entwurf
  `docs/mails/Build-RanVijayFollowup.ps1` darf nicht mehr raus. Details:
  `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

- BEFUND 2026-08-05, FELD GEFUNDEN UND AUSGEWERTET: Das Feld heisst **`OITM."U_Tasc_ST"`**
  (UDF `Tasc_ST`, FieldID 14, Beschriftung „Sales Type"), ermittelt aus `CUFD`/`UFD1` — nicht
  geraten. Werte laut `UFD1`: `FFM` Full Fledged Manufacturing, `LRD` Limited Risk Distributor,
  `CM` Contract Manufacturing, `--` ungepflegt. **`CM` kam im Call nicht vor** und ist echt
  extern (Fremdfertigung). VERTEILUNG auf Artikeln mit Rechnungszeilen ab 2025 (1'449 Artikel,
  7'018 Zeilen): `FFM` ohne Vendor **1'184 Artikel / 5'830 Zeilen** (korrekt so, brauchen keinen
  Lieferanten), `LRD` mit Vendor 93/454 (fertig), `LRD` ohne Vendor 30/256 + `CM` ohne Vendor
  2/23, Sales Type ungepflegt 130 Artikel/377 Zeilen (zweite, neu entdeckte Baustelle), `FFM`
  MIT Vendor 10/78 (Widerspruch). HEBEL: rund 5'830 der 6'236 maskierten TRIN-Zeilen (93 %)
  werden allein durch das Lesen des Feldes klassifizierbar, ohne jede Stammdatenpflege in
  Indien. **ENTSCHEIDENDE ZUSATZPRUEFUNG (Runde 3):** ALLE 93 `LRD`-Artikel mit Vendor zeigen
  auf `V0078` = Trafag AG/CH, ohne Ausnahme. `LRD` bestimmt die liefernde Gesellschaft damit
  ALLEIN — die 30 `LRD`-Artikel ohne Vendor brauchen KEINE Pflege. Ebenso haben 64 der 130
  Artikel ohne Sales Type schon einen Vendor und sind dadurch klassifiziert. **RESTLISTE damit
  nicht 32, sondern: 66 Artikel Sales Type pflegen (Blocker), 10 Artikel `FFM`-mit-Vendor
  bestaetigen (Fehlklassifikationsrisiko), 2 `CM`-Artikel (IC15415, IC15037) nur „waere schoen"
  — `CM` heisst schon extern, fuer die Marge fehlt dort nichts.** Dazu zwei Fragen ohne
  Datenbezug: bedeutet `LRD` IMMER Trafag Schweiz (heute 93/93, aber Messung ist keine Regel),
  und soll der Sales Type bei neuen Artikeln Pflicht werden (im Gesamtstamm 2'838 von 5'337
  ohne Wert). Ohne diese Pruefung waere Indien um Pflege gebeten worden, die unser
  eigenes Feld schon leistet — derselbe Fehlertyp wie die ueberholte Preferred-Vendor-Bitte.
  GEGENPROBE bestanden: `PT000003`/`PT000010` = `LRD` mit `V0078`, `DM000001` = `FFM`;
  `DM000083` ist `LRD` OHNE Vendor und widerlegt die Heuristik „kein Lieferant =
  Eigenfertigung". KONSISTENZ artikelgenau: unsere DB zaehlt 167 TRIN-Artikel MIT Lieferant,
  B1 ergibt 93+64+10 = exakt 167; ohne Lieferant 1'278 vs. 1'282 (Differenz = nur auf
  Gutschriften). Liefergegenstand: `output/TRIN_Sales_Type_Offen_2026-08-05.xlsx` (4 Blaetter)
  via `.tmp_tools/BuildTrinSalesTypeExcel`. NOCH OFFEN: Mail an RanVijay, Export-Umsetzung
  (`U_Tasc_ST` in `Services/HanaQueryService.cs`, ZWEIMAL — OINV/INV1 und ORIN/RIN1 —, Feld auf
  `CentralSalesRecord` per `AddColumnIfMissing`, Auswertung im
  `GroupMarginSupplierClassifier`). Details: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

- DEPLOY 2026-08-05 15:48, SALES TYPE UND TRAFAG-SACHNUMMER IM EXPORT UMGESETZT: `SalesType` und
  `GroupMaterialNumber` werden aus dem Artikelstamm gelesen, gespeichert, im Audit-CSV
  ausgewiesen und in der Gruppenmarge ausgewertet. `FFM`/`CM` -> intern mit liefernder
  Gesellschaft TR IN und lokaler Kostenbasis, `LRD` -> intern TR AG mit Konzernkosten ueber die
  Trafag-Sachnummer. NEUER STATUS `Konzernkosten fehlen`: LRD-Zeilen ohne Konzernkostentreffer
  zeigen KEINE Marge mehr (vorher eine Marge auf dem IC-Einkaufspreis — plausibel aussehend und
  falsch); als Konstante in `Services/GroupMarginStatuses.cs`, weil Excel, Cockpit und
  Pruefsummenformel denselben Text brauchen. WIRKUNG erst mit dem naechsten TRIN-Export
  (Timer 12:00): dann wechseln rund 5'830 Zeilen von „Lieferant unklar" auf intern, und 569
  statt 185 LRD-Zeilen bekommen eine Schweizer Kostenbasis. Spalten sind produktiv angelegt.
  406/406 Tests gruen, `BiDashboard.dll` 4'045'824 Bytes / SHA256 `0C65C997…`, bitgleich.
  DREI FEHLER, die erst durch Tests und Messung sichtbar wurden: (1) die B1-Query ist von ALLEN
  Standorten geteilt — ein festes `itm."U_Tasc_ST"` haette den ITALIEN-EXPORT mit „invalid
  column name" abgebrochen, jetzt Spaltensuche mit `'' AS sales_type` als Rueckfall; (2) das
  vorhandene `HasColumnAsync` schreibt Spaltennamen GROSS, Indiens Spalte heisst aber gemischt
  `U_Tasc_ST` — die Suche nach `U_TASC_ST` liefert produktiv 0 Treffer, das Feld waere fuer
  Indien STILL nie selektiert worden; jetzt `ResolveColumnNameAsync`, schreibweisenunabhaengig
  und mit dem GEFUNDENEN Namen im SELECT (HANA quotet case-sensitiv); (3) der Schreibweg ist ein
  Bulk-INSERT mit ausdruecklicher Spaltenliste — ein Feld am Modell genuegt nicht, aufgefallen
  durch `NOT NULL constraint failed`. Details:
  `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 7.

- BEFUND 2026-08-05, WAS `CM` IST — OHNE RUECKFRAGE BEI INDIEN ERSCHLOSSEN, UND MEINE ERSTE
  EINORDNUNG WAR FALSCH: `Sales Type` beschreibt NICHT die Herkunft, sondern die
  verrechnungspreisliche ROLLE von TR IN. `FFM` = voll risikotragender Hersteller (produziert
  und verkauft auf eigene Rechnung), `LRD` = Vertrieb mit begrenztem Risiko (bezieht Fertigware
  aus CH, verkauft lokal weiter), `CM` = Auftragsfertiger fuer den Prinzipal. Ich hatte `CM` als
  „Fremdfertigung durch Dritte, also extern" eingeordnet — falsch. FUENF unabhaengige Belege:
  (1) Kunde der beiden CM-Artikel `IC15415`/`IC15037` ist AUSSCHLIESSLICH Trafag AG/CH (23
  Zeilen, 53'842'559 INR); (2) Marge 31.2 % und 31.7 % — nahezu identischer Aufschlag auf zwei
  verschiedenen Artikeln = Kostenaufschlag, keine Marktpreisbildung; (3) Artikelgruppe
  `Sub Assemblies`; (4) beide MIT `Drawing No`, aber OHNE `Material No` und OHNE `Ordering Code`
  — nach Zeichnung des Prinzipals gebaut; (5) kein Preferred Vendor, konsistent mit
  Eigenfertigung. FOLGE: `CM` ist intern mit liefernder Gesellschaft TR IN und lokaler
  Kostenbasis wie `FFM`, der Preferred Vendor ist UNNOETIG. Die Bitte an Indien schrumpft damit
  auf 66 Artikel (Sales Type) + 10 Bestaetigungen; kein Vendor-Pflegefall mehr.

- BEFUND 2026-08-05, BEI `LRD` IST DER LOKALE WERT DIE FALSCHE KOSTENBASIS: Weil LRD-Artikel in
  CH hergestellt und von Trafag AG bezogen werden (Bestaetigung Ingo), ist `INV1.StockPrice`
  dort der IC-EINKAUFSPREIS, nicht die Herstellkostenbasis — genau der Wert, den die
  Gruppenmarge laut `Mappe1.xlsx` ersetzen soll. Richtige Basis waere `GroupStandardCosts`
  (Bewertungskreis 1100, CHF, 63'506 Zeilen). GEMESSEN greift der Weg aber kaum: nur 34 von 135
  TRIN-Artikeln mit Lieferant Trafag AG (185 von 687 Zeilen, 27 %) finden ueber die
  Artikelnummer einen Treffer — die indischen Nummern sind TASC-Eigennummern, keine Trafag-MATNR.
  BRUECKE GEFUNDEN UND GEMESSEN: das UDF **`U_TASC_OMN` („Material No") IST die
  Trafag-Sachnummer** — sie steckt bei vielen Artikeln auch in der Bezeichnung (`PT000003` =
  „EPR10.0A(**57291**)-8283", `U_TASC_OMN` = `57291`), und alle acht Stichproben stehen mit
  CHF-Stueckkosten in `GroupStandardCosts`; das Schluesselformat passt (37'392 der Schluessel
  sind fuenfstellig). VOLLMESSUNG ueber alle 123 LRD-Artikel: ueber `ItemCode` 34 Artikel /
  27 % der Zeilen, ueber `U_TASC_OMN` **118 von 123 Artikeln (95.9 %) und 569 von 710 Zeilen
  (80.1 %)** — von den 118 Artikeln MIT echter Nummer treffen 118, also 100 %. Die fuenf
  Ausfaelle sind genau die ohne Nummer: `DM000083` (108 Zeilen, groesste Einzelluecke),
  `DM000084` (27), `H90101` (4), `FA000028`/`FA000029` (je 1, Anlagegueter). FOLGE FUER DEN
  EXPORT: ZWEI neue Felder lesen, nicht eines — `U_Tasc_ST` und `U_TASC_OMN`; der
  Konzernkosten-Lookup muss fuer TRIN auf den Trafag-Schluessel gehen statt auf `Material`
  (heute `NormalizeMaterialKey(record.Material)`). Der Platzhalter aus zwei Bindestrichen ist
  wie leer zu behandeln. REGEL: bei LRD-Zeilen ohne Konzernkostentreffer NICHT auf den lokalen
  Wert zurueckfallen — das ergaebe eine plausibel aussehende, falsche Marge (derselbe Fehler wie
  bei TRIT, siehe `docs/FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28.md` Abschnitt 7a). NEUE
  BITTE AN INDIEN dadurch: 3 LRD-Artikel brauchen die `Material No` (139 Zeilen) — das blockiert
  die Kostenbasis, nicht die Klassifikation.

- OFFENE FRAGE AN ANDREAS 2026-08-05, INNENUMSATZ IST GRUPPENWEIT NICHT AUSGESCHLOSSEN (beim
  CM-Nachgraben aufgefallen, deutlich groesser als das Indien-Thema): Zeilen mit einer
  Trafag-Gesellschaft als KUNDE — TRCH 11'034 von 47'142 (23.4 %, 16'347'706 CHF), TRIN 737
  (10.4 %, 145'181'191 INR), TRIT 657 (3.3 %, 576'130 EUR), Rest je unter 15 Zeilen. In
  `FinanceRules` gibt es dazu nur ZWEI von Hand angelegte Kundenausschluesse: Id 2 (DE,
  `CustomerName` = `Trafag AG`) und Id 6 (IT, enthaelt `Trafag Italia`). Die IT-Regel greift nur
  fuer Trafag Italia, die uebrigen 657 TRIT-Zeilen bleiben drin; fuer TRCH und TRIN existiert
  keine Regel. Verkauft TR IN an Trafag Italia und Italia danach an den Endkunden, stehen beide
  Umsaetze im Dashboard — fuer eine KONZERN-Umsatzzahl ist derselbe Warenwert doppelt enthalten.
  KEINE Empfehlung von mir: ob die Umsatzzahl brutto oder konsolidiert gemeint ist, ist eine
  Finanzentscheidung. Zu klaeren, bevor die Gruppenmarge als belastbar bezeichnet wird.

- DEPLOY 2026-08-05, SERVER-ANALYSE PRODUKTIV: `Services/ServerAnalysisBackgroundService.cs`
  fuehrt lesende Diagnoseabfragen gegen Standort-B1 aus — auf dem Server, weil einzelne
  Standortsysteme nur von dort erreichbar sind. ZUGRIFFSLAGE GEMESSEN: Share = FullControl,
  aber `Invoke-Command`/`schtasks`/`C$` auf `tragvapp401` = Zugriff verweigert, KEIN RDP
  vorhanden; der DNS-Name `trch-webapp-bidashboard` ist ein CNAME auf `tragvapp401`, mit dem
  Aliasnamen scheitert schon Kerberos. Deshalb ist die LAUFENDE ANWENDUNG der einzige Weg,
  Code auf dem Server auszufuehren: alle 20 s Pruefung auf `_analysis/run.trigger`, dann
  `_analysis/sql/*.sql` -> `_analysis/results`. Guardrail `Services/ReadOnlySqlGuard.cs` (nur
  SELECT/WITH, Positivliste), Zugangsdaten ueber den neuen gemeinsamen
  `Services/DataSources/HanaServerResolver.cs` — dieselbe Aufloesung wie der Export.
  Fernbedienung `docs/analyse/Run-ServerAnalysis.ps1 -Action Run|Fetch|Clean` (VERSIONIERT,
  bewusst nicht unter `.tmp_tools/` — das ist gitignoriert, und die Abfragen sind der Nachweis
  fuer eine fachliche Entscheidung; Abfragen in `docs/analyse/sql/`, Belege in
  `docs/analyse/ergebnisse/`).
  385/385 Tests gruen, `BiDashboard.dll` 4'037'632 Bytes / SHA256 `56AFD5AF…`, bitgleich mit
  dem Release-Build, Produktiv-DB unveraendert, HTTP 200. FALLE: zwei Bindestriche koennen
  nicht als Zeichenkettenliteral in einer Analyseabfrage stehen (gelten als Kommentar).
  Vor dem Deploy lokal gegen Italien verifiziert (`.tmp_tools/ServerAnalysisLocalTest`) und
  dabei zwei SQL-Fehler gefunden: `LIKE 'U_%'` matcht wegen des Platzhalter-Unterstrichs auch
  `UserSign`/`UserText` (jetzt `ESCAPE '\'`), und `SCHEMA_NAME = '{SCHEMA}'` findet klein
  geschriebene Schemata nicht (jetzt `UPPER(...)`). NEBENBEFUND, betraf auch den naechsten
  Produktivdeploy: `dotnet publish` des Hauptprojekts brach ab, weil die csproj drei im Working
  Tree geloeschte Content-Dateien einbindet (`DE_Beispiel_Export_Daten.xlsx`, `login.png`,
  `manometer.png`); behoben mit `Condition="Exists('...')"` nach dem vorhandenen Muster.

- BEFUND 2026-08-03, SPANIEN HAT KEIN BUCHUNGSDATUM (Prio von Andreas): `PostingDate` ist auf
  ALLEN 5'504 TRES-Zeilen leer — Spanien ist der einzige Standort ohne Buchungsdatum, alle
  anderen haben es zu 100 % gefuellt (TRUK 6 Ausnahmen). Die bisherige Doku und der
  Mailentwurf an Santi nannten nur „231 Zeilen ohne jedes Datum" — das ist die TEILMENGE, in
  der zusaetzlich das Rechnungsdatum fehlt, nicht das Problem. Folge: alle 5'504 Zeilen fallen
  auf `InvoiceDate` zurueck (Rechnungsdatum ist nicht Buchungsdatum -> ueber einen Jahreswechsel
  still falsche Periode), 231 Zeilen eine Stufe weiter auf `ExtractionDate` und zaehlen damit
  pauschal im Exportjahr — 140'598.19 EUR. KEIN akuter Jahresfehler, weil alle 231 ein
  `OrderDate` in 2026 haben und der Export 2026 lief; `OrderDate` ist gefuellt, wird von der
  Fallback-Kette aber nicht genutzt. URSACHE WIE BEI DE: unsere eigene Query.
  `Export-SageSpainSalesCsv.ps1` Z. 184-186 selektiert `FechaFactura`/`FechaAlbaran`/
  `FechaRegistro`, aber kein Buchungsdatum, und liest `CabeceraAlbaranCliente` +
  `LineasAlbaranCliente`, nicht die Buchhaltungstabellen. Query steht ZWEIMAL (auch in
  `Run-SpainRangeExportAndUpload-AllInOne.ps1` Z. 233-235) — Aenderungen immer an beiden
  Stellen. KANDIDAT, NICHT BELEGT: `FacturasTB.FechaAsiento` ist der einzige brauchbare Treffer
  im Schema-Auszug, aber die Tabelle hat `NumeroFacturaInicial_`/`NumeroFacturaFinal_` (riecht
  nach Sammelbuchung ueber Nummernbereich), `CabeceraFacturaCliente` fehlt im Auszug ganz, und
  der Auszug ist bei 80 Objekten abgeschnitten — gemeinsame Spalten mit dem Lieferscheinkopf
  sind nur `CodigoEmpresa` und `FechaFactura`, der Join ist also NICHT ableitbar. Erst live
  pruefen. Sofort additiv moeglich ohne neuen Join: `SerieFactura`, `NumeroFactura`,
  `EjercicioFactura`, `StatusContabilizado` liegen schon in der gelesenen Tabelle. Offen
  fuer Finance: darf `OrderDate` Fallback-Stufe werden, reicht `EjercicioFactura` als
  Jahresanker. Details: `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md`.

- DOKU 2026-08-03, RAG-Luecke geschlossen, die den DE-Fehlgriff ueberhaupt ermoeglicht hat: Die
  Export-Pakete `AlphaplanExportPackage/` und `SageSpainExportPackage/` standen in NEUN bzw.
  SIEBEN Markdown-Dateien, aber in KEINER auf dem RAG-Einstiegspfad — nicht im
  `RAG_ROUTER.md`, nicht im `RAG_DETAIL_INDEX.md`, nicht in `docs/rag/MANUAL_IMPORT.md`. Wer
  ueber Router -> `lastchange.md` -> Kurzdatei einstieg, lernte „DE liefert kein Supplier-Feld"
  und schloss daraus „Standort fragen", ohne je zu erfahren, dass die Query uns gehoert. Jetzt
  ergaenzt: neue Vorrangregel 7 im Router („bei fehlendem Feld in DE/ES ZUERST die eigene
  Export-SQL pruefen"), zwei Themenzeilen im Router, drei Zeilen im Detailindex (Export-SQL DE,
  Export-SQL ES, Schema-Discovery) und ein neuer Abschnitt „Skripthoheit" in
  `docs/rag/MANUAL_IMPORT.md` mit Skript, gelesenen Tabellen und Konsequenz je Standort.

- BEFUND + MAIL 2026-08-03, DE/Alphaplan war die falsche Bitte an die falsche Stelle: Die alte
  DE-Mail bat Rohail um drei Export-Erweiterungen (Lieferant, Kundenname/-land, RTF-Muell).
  FALSCH — die Export-SQL ist UNSERE: `AlphaplanExportPackage/scripte/alphaplanExport.ps1`
  Zeilen 143-202 und `alphaplandeltaexport.ps1` mit identischer Query, geschrieben in diesem
  Repo, lesen nur `dbo.Belege` + `dbo.BelegePositionen`. Drei der vier DE-Luecken sind Spalten,
  die unsere Query nicht liest; `RechnungsAdressenID` wird sogar selektiert, aber nie auf einen
  Namen aufgeloest. Nur `ArtikelNummer` vs. TR-AG-/SAP-`MATNR` ist eine echte Fachfrage an DE
  (offen seit 2026-06-01) — und die ist heikel, weil der Standard-Vorspann aller Standortmails
  „Produktsparte ist egal, solange die Materialnummer passt" behauptet, was fuer DE gerade
  unbelegt ist; die DE-Mail hat deshalb eine eigene Kastenfassung ohne diesen Satz. ECHTER
  BLOCKER ist das fehlende Alphaplan-Schema fuer `ApDaten`: `candidate_objects.csv` im Repo-Root
  ist nur eine Kopfzeile, `obj/candidate_objects.csv` ist Sage Spanien, die DB liegt auf
  `localhost\SQL2012` des DE-Servers hinter DPAPI-Credential. DESHALB KEINE TABELLENNAMEN RATEN —
  ein erfundenes `JOIN dbo.Adressen` im ausgelieferten Skript waere derselbe Fehlertyp wie
  UK-2025 und das IT-Superlativ. Neue DE-Mail bittet nur noch um einen read-only
  `INFORMATION_SCHEMA.COLUMNS`-Auszug und stellt die `ArtikelNummer`-Frage. Sie ist als EINZIGE
  der sieben Standortmails auf DEUTSCH (Rohail sitzt bei der Trafag GmbH), Betreff
  „BI Dashboard - Alphaplan-Export: eine Schemaliste und eine Frage zu den Artikelnummern";
  alle englischen DE-Entwuerfe in Outlook sind Loeschkandidaten. FALLE dabei:
  `Build-StandortMails.ps1` ist reines ASCII ohne BOM, PowerShell 5.1 liest so eine Datei als
  Windows-1252 — echte Umlaute wuerden als Mojibake in der Mail landen, deshalb stehen alle
  Umlaute als HTML-Entities (`&uuml;` etc.). Alle vier DE-Zahlen
  am 2026-08-03 neu gemessen und exakt bestaetigt: 7'171 Zeilen, Supplier 7'171 leer,
  CustomerName/-Country 7'171 leer bei 7'171 gefuellter CustomerNumber, 2'903 Bezeichnungen mit
  Font-Muell, Material 0 leer. Details:
  `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md` Abschnitt „Korrektur Deutschland, 2026-08-03".

- VERSAND 2026-08-03, Indien-Nachfassung: RanVijay hat auf die Mail vom 31.07. geantwortet, dass
  er die Frage nicht versteht, und um einen Teams-Call gebeten. Ursache mutmasslich die
  Doppelbenennung — SAP nennt das Feld UI-seitig `Preferred Vendor` (Reiter `Purchasing Data`),
  unser Datenmodell nennt dasselbe Feld `Supplier`/`OITM.CardCode`. Antwortentwurf liegt in
  Outlook (an RanVijay, Cc Andreas), erklaert die Gleichsetzung in einem Satz und haengt die
  konkrete Artikelliste an: `output/TRIN_Fehlende_Preferred_Vendor_2026-08-03.xlsx`, erzeugt von
  `.tmp_tools/BuildTrinSupplierGapExcel` aus `Finance_Dashboard_Audit_All_2026-07-29.csv` mit der
  in `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitt 7 dokumentierten Gruppierung —
  1'271 von 1'437 Artikeln, 6'154 betroffene Zeilen, deckungsgleich mit der bereits gesendeten
  Zahl. Skript fuer die Mail: `docs/mails/Build-RanVijayFollowup.ps1` (`-Mode Preview` aendert
  nichts, `-Mode Draft` legt den Entwurf an, sendet nie).

- DEPLOYED 2026-08-03, Commit `9e28086`: Logistik > Stuecklistenanalyse hat ein neues
  richtungsabhaengiges Dashboard fuer Top-Down und Bottom-Up mit vier
  Kennzahlen, Top-12-Verwendungsbreite, Bestandsklassen und LZ-Code-Verteilung.
  Die Aggregate verwenden den gesamten gefilterten Cache; nur die bestehende
  Rohdatentabelle bleibt auf 200 Zeilen begrenzt. Gemeinsam verwendete
  Komponenten werden bei der Bestandslage genau einmal klassifiziert und
  Bestandswerte nicht ueber Stuecklisten summiert. Alle acht UI-Sprachen sind
  abgedeckt. Live-Cache-Snapshot und fachliche Grenzen:
  `docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md`. Release-Test:
  353/353 bestanden. Produktive `BiDashboard.dll` `03.08.2026 06:59:38`,
  `4'024'832` Bytes, SHA256
  `8D5586E5536C83A9EDB409472C332D190488898C3FE8E8DB2097C3131779B554`;
  Release und Server bitgleich. Produktiv-DB in Laenge, Schreibzeit und SHA256
  unveraendert, `app_offline.htm` entfernt, Port 443 offen und authentifizierter
  Aufruf von `/BiDashboard/logistik/stuecklistenanalyse` mit HTTP `200`.

- DOKU 2026-07-31, RAG-Inhaltsverzeichnis konsolidiert: `docs/RAG_ROUTER.md`
  von 289 auf 51 Zeilen reduziert und auf Vorrangregeln/Themenrouting
  beschraenkt. Detailquellen, Live-Werkzeuge und Suchbegriffe stehen jetzt in
  `docs/RAG_DETAIL_INDEX.md`. Der vorherige Router ist vollstaendig und
  zeilengleich in
  `docs/raw_md_archive/RAG_ROUTER_ARCHIV_2026-07-31.md` erhalten.

- DEPLOYED 2026-07-31, finaler Stand der Spend-Matrix fuer
  Praesentations-Screenshots (Code-Commits `4a3271b`, `f740eb9`, `4498bd4`):
  dunkler Primaertext und deutlichere Ebenenhintergruende; Tabellenkopf,
  Lieferanten, Warengruppen und Materialien fett (`700`); Lieferanten und
  Warengruppen `1.05rem`, Materialien `1rem`. `346/346` Tests gruen.
  Produktive `BiDashboard.dll` `31.07.2026 11:43:06`, `3'226'624` Bytes,
  SHA256 `E64BF04327D3FD7668D424C0FA52EC78A00F076E9118E253D57601730F24A247`;
  Release und Server bitgleich, Produktiv-DB unveraendert, Port 443 offen,
  authentifizierter HTTPS-Aufruf `200`.

- VERSAND 2026-07-31, Stand bei Chatende (Detail: `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md`
  Abschnitt „Stand bei Chatende"): INDIEN IST RAUS - gesendet 09:56 an `RanVijay.Kumar@trafag.com`,
  Betreff von Ingo ergaenzt auf „... (Trafag India) -> Supplier Name", NICHT an `tasc.co.in`, damit
  ist die Fremddomain-Frage fuer diesen Versand erledigt. In Entwuerfen liegen sechs: DE, ES, UK, IT
  versandfertig, FR und US mit LEERER An-Zeile. ZWEITE KORREKTUR an der Italien-Mail: sie nannte
  TR IT „the best-performing site on supplier data" - FALSCH, TRUK hat 100 % gegen TRIT 71 %.
  Eingeschraenkt auf „of our SAP Business One sites" und der zweite Balken ersetzt, weil Artikel-
  und Zeilenebene bei IT beide auf 71 % fielen und zwei identische Balken wie ein Copy-Paste-Fehler
  aussahen; jetzt Standortvergleich UK/IT/IN/FR/US. MUSTER HINTER BEIDEN FEHLERN (UK-2025 und
  IT-Superlativ): eine Behauptung war aus aelterer Doku uebernommen statt gemessen. Vor jedem
  Mailversand die Zahlen gegen das Audit-CSV nachrechnen, auch die scheinbar harmlosen Nebensaetze.
  NICHT ANGEFASST: Ingos eigener Entwurf „Missing supplier information in sales export data"
  (30.07., 48'708 B) - ueberholte Sammelfassung mit der falschen UK-Aussage, Loeschkandidat.
- PRUEFUNG 2026-07-31, alle sieben Standort-Entwuerfe gegen `Finance_Dashboard_Audit_All_2026-07-29.csv`
  nachgemessen. SECHS stimmen, EINER war falsch: die UK-Mail behauptete „the UK data we hold starts
  in January 2026, so 2025 is absent". FALSCH - TRUK hat **1'867 Zeilen fuer 2025** und 1'082 fuer
  2026 bis 27.07., der UK-2025-Backfill ist gelaufen. Fehlerkette: die Spalte „weitere
  Auffaelligkeit" in `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitt 1 ist die EINZIGE
  Spalte, die nicht aus dem Reproduktionsskript stammt - dort war „2025 fehlt komplett" aus der
  ueberholten 28.07.-Analyse uebernommen und nie nachgemessen. Der Entwurf mit der Falschaussage
  ist geloescht und ersetzt (`-Only TRUK` am Skript ergaenzt), fuer TRUK ist damit NICHTS offen.
  ZWEITER BEFUND, gegen eine Fremdauswertung die fuer TRUK `0` Lieferanten zeigte: es gibt in dieser
  Quelle KEINEN Fall „SupplierNumber gepflegt, SupplierName leer" - beide Felder sind immer
  gemeinsam gefuellt (TRUK 2'955/2'955, TRIT 13'925/13'925, TRIN 809/809, TRFR 134/134, TRUS 6/6,
  CH/AT/DE/ES 0/0). Jene Tabelle stimmt in JEDER anderen Zelle mit dem Audit-CSV ueberein, nur die
  UK-Zeile weicht ab und traegt noch den alten Statustext „Mapping jetzt da - braucht noch den
  Reimport": mutmasslich eine unveraendert uebernommene Zeile, keine Messung. Trafag-Erkennung
  gegengeprueft: TRFR 83 intern / 51 extern, TRIN 677/132, TRIT 6'848/7'077, TRUK 2'803/152,
  TRUS 2/4 - der Regex greift ueberall, wo ein Lieferant steht; das fehlende Feld ist der Engpass.
- WERKZEUG 2026-07-31, grafische Mailfassung: `docs/mails/Build-StandortMails.ps1` baut die sieben
  Standortmails mit Outlook-taugliche Grafiken (Balken Artikelstamm/Rechnungszeilen, Feld-Schema
  `Purchasing Data` -> `Preferred Vendor`, Statustabelle DE, Vorher/Nachher-Kasten zum RTF-Muell,
  Monatsstreifen ES 2026, Standort- und Jahresvergleich UK). `-Mode Preview` (Default) schreibt
  `.tmp_standort_mails/Vorschau_Standortmails.html`, `-Mode Draft` legt Outlook-Entwuerfe an
  (schreibt ins Postfach, sendet nichts). Ausgabeordner ist gitignored - enthaelt Empfaengeradressen.
  BEFUND, WICHTIG FUER KUENFTIGE VERSUCHE: `MailItem.SaveAs` ist auf diesem Arbeitsplatz GESPERRT -
  `.msg`, `.oft` und `.txt`, jeder Zielordner, immer `E_ABORT` (0x80004004). Endpoint-Security/DLP,
  kein Skriptfehler; `MailItem.Save()` in Entwuerfe geht. `Word.Application`-COM haengt ebenfalls,
  daher kein automatisches .docx - Vorschau-HTML von Hand in Word oeffnen. Zwei Darstellungsregeln:
  Artikelbalken mit EXAKTEN Stueckzahlen, Zeilenbalken NUR in Prozent (die Zeilenzahlen je Kategorie
  waeren aus gerundeten Prozenten abgeleitet und wuerden Scheingenauigkeit erzeugen); keine Bilder,
  nur Tabellen mit `bgcolor`, weil Outlook externe Bilder beim Empfaenger blockiert.
- DOKU 2026-07-31, versandfertige Einzelmails je Standort (kein Code):
  `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md`. Der Sammeltext aus
  `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitt 6 ist in SIEBEN Einzelmails aufgeteilt,
  jede mit To/Subject/Anrede; Zahlen unveraendert (Messung 29.07.2026, 95'168 Zeilen). Aenderungen
  an Zahlen kuenftig ZUERST im Sammeltext, dann uebernehmen. UK-EMPFAENGER NACHGETRAGEN:
  `Cornell.Williams@trafag.com` (von Ingo geliefert) - damit sind 5 von 7 versandfertig.
  NOCH BLOCKIERT: TRFR und TRUS, es fehlt AUSSCHLIESSLICH die Adresse, die Mails stehen.
  Drei bewusste Textentscheide: (1) Italien-Mail beginnt mit der ausdruecklichen Abgrenzung
  „a separate topic from the inventory valuation discussion ... no deadline attached", sonst liest
  Paola sie als Erinnerung an ihre Zusage Ende August; Verweis auf B1-Upgrade 03.08. (2) Indien geht
  per To NUR an RanVijay, `tasc.co.in` bleibt Cc-Option nach Klaerung, und das Listenangebot ist auf
  „I can send you the list directly" umformuliert, damit die 1'271 Artikelnummern nicht in einen
  Fremddomain-Cc laufen. (3) Spanien-Mail zieht den Range-Export von Punkt 3 auf Punkt 1 vor (das ist
  die sichtbare 2026-Luecke) und verweist auf den bereits dokumentierten Befehl in
  `FINANCE_BACKFILL_UK_ES_2026-07-28.md`. UK-Mail ohne den „was wir nicht brauchen"-Vorspann, weil
  reine Rueckfrage nach 2025. Am Dateiende eine Versandtabelle - ohne die ist spaeter nicht
  unterscheidbar, ob ein Standort nicht geantwortet oder nie eine Mail bekommen hat.
- DOKU 2026-07-31, zentrales Ansprechpartner-Register angelegt (kein Code): `docs/ANSPRECHPARTNER.md`,
  im `RAG_ROUTER.md` als eigenes Thema und ueber Suchwoerter verlinkt. Anlass: Kontaktangaben lagen
  in DREI Dokumenten verstreut - Rollen in der Ticket-Beteiligtenliste `saptasks/zzprdat-kontext.md:189`,
  Domaenenzustaendigkeiten in `docs/INGO_TODOS_180_TAGE_2026-06-18.md:23-25`/`:159-166`, Adressen nur
  in `docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md:137`. Beim BLP-Vorfall 2026-07-30 war dadurch
  klar WER zustaendig ist, aber nicht wie er zu erreichen ist. Inhalt: Standortempfaenger (Abschnitt 1),
  interne Rollen (2), externe Partner (3), Eskalationspfad plus Paola-Doppelvorgang und
  `tasc.co.in`-Einschraenkung (4), Verwechslungsgefahren (5), offene Luecken (6).
  REGEL DER DATEI: keine aus Namensmustern abgeleiteten Adressen - was nicht belegt ist, steht als
  `offen`. GROESSTE LUECKE: fuer KEINEN internen Ansprechpartner (Lucas, Adil, Fabio, Andreas, Sonja,
  Alex, Ramon) ist eine Adresse dokumentiert, und Andreas/Sonja/Alex/Ramon haben nicht einmal einen
  belegten Nachnamen. Neu festgehaltene Verwechslungsgefahr: ZWEI Marcos - Marco Di Menco (Etiketten/PP)
  vs. Marco Widmer (Einkauf, Autor `PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`).
- DOKU 2026-07-30, Empfaenger fuer die Standort-Mail (kein Code): `docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md`
  hat einen neuen Abschnitt 5 `Empfaenger` (alter Abschnitt 5 Mailtext -> 6, Reproduzierbar -> 7).
  Anlass: In KEINEM `.md` des Repos stand vorher eine Mailadresse (Regex ueber alle Markdown-Dateien:
  null Treffer) - benannt war als einziger Standortkontakt Paola (TR IT), und zwar nur fuer das
  Bewertungsthema. Von Ingo recherchiert und jetzt festgehalten: ES `Santi.Gomez@trafag.es`,
  DE `Rohail.Munir@trafag.de`, IT `Paola.Castagna@trafag.com`, IN `RanVijay.Kumar@trafag.com` plus
  `agupta@tasc.co.in` und `chandra.s@tasc.co.in`. DREI PUNKTE, die beim Versand zaehlen:
  (1) **FR und US fehlen weiterhin** - genau die zwei Standorte mit einer echten Bitte im Text
  (FR 374 von 433, US 518 von 521 Artikeln ohne `OITM.CardCode`), FR ist sogar der kleinste Aufwand
  aller Standorte. (2) **Paola laeuft doppelt**: sie ist bereits Adressatin des Standardkosten-/
  Bewertungsthemas mit Zusage Ende August (B1-Go-Live 2026-08-03 + 2 Wochen Ferien) - die Bitte hier
  ist Stammdatenpflege und ein anderer Vorgang; getrennt verschicken, sonst kostet es den klaren
  Termin des Bewertungsthemas. Querverweis dazu jetzt auch in
  `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` Abschnitt 5d. (3) **`tasc.co.in` ist
  keine Trafag-Domain** - zwei der drei indischen Adressen liegen bei einem externen Dienstleister.
  Der Mailtext selbst ist unkritisch, aber er enthaelt das Angebot "we can send the item list"
  (1'271 Artikelnummern des TRIN-Stamms); vor dem Versand einer solchen Liste an eine Fremddomain
  klaeren, ob das gedeckt ist, im Zweifel nur an die Trafag-Adresse und von dort weiterverteilen.
  Nicht verwechseln: `Hugo Cuesta` in Abschnitt 3 ist ein Artikeltext aus den Spanien-Daten
  (`- Entregado por Hugo Cuesta`, 377 Zeilen), kein Ansprechpartner.
- GEFIXT 2026-07-30 (Entscheid Ingo, Variante B): Das naechtliche Einkauf-Delta haengt nicht mehr an
  `Sites.IsActive`, sondern nur noch daran, DASS die Site `PURCHASING_SAP` konfiguriert ist. Damit
  bleibt `IsActive = 0` und der Sales-Export unveraendert - die Variante mit dem Ausfiltern in
  `ExportAllAsync` wurde bewusst NICHT genommen, weil sie die Strecke anfasst, die Andreas'
  Finanzzahlen fuettert. Zusaetzlich wird das Ueberspringen jetzt als `Warning` geloggt: der stille
  Aussteiger war der eigentliche Grund, warum der Ausfall sechs Tage unentdeckt blieb. Fehlende
  Zugangsdaten meldet `RunDeltaAsync` selbst als `Error`-Status, statt vorab geprueft zu werden -
  dann ist die Ursache im Refresh-Status sichtbar statt unsichtbar. NACHSORGE: Delta-Button im
  Einkaufs-Dashboard einmal druecken, damit nicht bis zum Nachtlauf gewartet werden muss; danach
  muss in `PurchasingSyncState` ein `Delta`-Eintrag stehen und die Meldung die Zahl der
  nachklassifizierten Cachezeilen nennen.
## Offene Punkte aus aelteren Eintraegen (Original im Archiv)

- Server/IIS (seit 2026-07-08, nur direkt am Server moeglich, WinRM gesperrt): App-Pool `startMode=AlwaysRunning` + `processModel.idleTimeout=00:00:00` setzen, damit der 12:00-Timer ohne vorherigen HTTP-Request laeuft. Bis dahin holt `CatchUpMissedRunAsync` verpasste Tageslaeufe beim naechsten Prozessstart nach.
- Betriebshinweis DE/Alphaplan (seit 2026-07-03): Der Alphaplan-Upload nach SharePoint muss VOR dem 12:00-Timer laufen, sonst verwendet der Tagesexport noch den vorherigen ZIP-Stand.

## Aeltere Eintraege / Historie

- Kurzstand-Eintraege 2026-06-04 bis 2026-07-08 und alle Nachtrag-Abschnitte (Mai/Juni 2026): verbatim in `docs/raw_md_archive/LASTCHANGE_ARCHIV_bis_2026-07-12.md`.
- Kurzstand-Eintraege 2026-07-13 bis 2026-07-30: verbatim in `docs/raw_md_archive/LASTCHANGE_ARCHIV_2026-07-13_bis_2026-07-30.md`.
- Kanonische Detailhistorie davor: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; Original-Volltexte: `docs/raw_md_archive/original_history_raws.zip` (nur zur Wiederherstellung).

## Einstieg / Router

- Themenrouter (zuerst laden): `docs/RAG_ROUTER.md`.
- Fuehrender Kurzkontext: `docs/rag/PROJECT.md`.
- Naechster Chat: `docs/RAG_ROUTER.md` -> diese Datei -> passende Themen-Kurzdatei aus `docs/rag/`.
