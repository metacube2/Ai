# RAG Finance

Stand: 2026-07-01

## Kurzstand

- Fuehrende Sicht: `Finance Summary`.
- Aktuelle Schulung: `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md`.
- Neu deployed 2026-07-01: `Management Analyse > Experten > Finance Pivot` hat Excel-aehnliche Filter fuer `Jahr`, `MTD Monat` und `TSC`. Die Filter wirken auf Monatsmatrix, Tagesmatrix, YTD/MTD-Kacheln und den Export `Finance_Pivot_*.xlsx`. Fuer TSC wurde eine eigene Tagesaggregation je Standort ergaenzt, damit die Tagesmatrix bei TSC-Auswahl wirklich nur diesen Standort summiert. Commit `723a60c Add finance pivot filters`; `125/125` Tests gruen; Server-DLL `01.07.2026 07:07:36`.
- `Finance Pivot` basiert auf Andreas' Excel `sta.xlsx`, Blatt `piv`: Monatsmatrix `YYYY/MM/TSC`, Tagesmatrix `MM/D/Jahr`, Wertbasis `Net Sales in CHF`. Es ist eine Summen-/Pivot-Kontrolle; Einzelzeilen, Kurse und Kostenbasis werden weiterhin im `Finance Pruefbuch` geprueft.
- Neu deployed 2026-06-30: `Management Analyse > Experten > Finance Pruefbuch` als Excel-artige Detailpruefung fuer Andreas/Finance. Es ist keine Zusammenfassung, sondern zeigt je Finance-Zeile Originalbetrag/-waehrung, CHF-Kurs, CHF-Betrag, Kursquelle/-jahr, Kunde, Material, Lieferant, Lieferantentyp, Standardkosten, Kostenbasis CHF, Marge CHF, Pruefstatus und Datenquelle. Der Reiter hat einen eigenen `Export to Excel`; Export enthaelt das Blatt `Finance Pruefbuch` plus `Gruppenmarge Detail`. Navigation-Seed: `finance-audit-ledger`, URL `management-cockpit?section=ledger`.
- Produktiv ist die zentrale Auswertung aktuell auf Audit-CSV gestellt: `AuditCsvEnabled=1`, `UseAuditCsvAsCentralSource=1`, `LocalSiteExportFolder=''`. Die App nutzt dadurch ihren lokalen Output-Ordner `C:\inetpub\wwwcust\BiDashboard\output` (von aussen: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\output`). Finance Summary, Management Analyse und Finance Pruefbuch lesen bevorzugt die neuesten `Sales_ProcessedMergeInput_*.csv` je TSC; falls keine Standort-CSV vorhanden sind, faellt der Code auf die neueste zentrale `Finance_Dashboard_Audit_All_*.csv` zurueck. `Sales_All_*.xlsx` bleibt zentraler Excel-Export/Nachweis und ist nicht die Live-Quelle der Dashboard-Reiter. Prozessdoku: `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md`.
- `Finance Summary`, zentrale Excel, Soll/Ist und Management-Analyse koennen wahlweise aus Audit-CSV statt direkt aus `CentralSalesRecords` lesen. Die Audit-CSV werden nach Mapping und Transformation geschrieben und dienen der Nachvollziehbarkeit fuer Finance/Revision.
- Audit-CSV-Dateiname: `Sales_ProcessedMergeInput_<TSC>_<yyyy-MM-dd>.csv`; liegt im gleichen Ordner wie das Standort-Excel und wird beim Standortexport in denselben SharePoint-Landesordner hochgeladen.
- `Finance Summary` nutzt dieselbe `FinanceRuleEngine` wie das zentrale Excel.
- Nachweis fuer Excel-Fans: `Zentrale Datei neu erzeugen` erstellt zusaetzlich `Finance_Dashboard_Nachweis_<yyyy-MM-dd>.xlsx` im waehlbaren zentralen Exportordner. Die Datei enthaelt Formel-Summaries (`SUMIFS`, `COUNTIFS`, `IF`) und Detailblaetter fuer Finance, Soll/Ist, Sparten, Gruppenmarge und Datenqualitaet. Doku: `docs/FINANCE_DASHBOARD_NACHWEIS_2026-06-17.md`.
- Zentraler Finance-Export laedt nach SharePoint `Import/Finance/Alle`: `Sales_All_<yyyy-MM-dd>.xlsx`, `Finance_Dashboard_Audit_All_<yyyy-MM-dd>.csv` und danach `Finance_Dashboard_Nachweis_<TSC>_<Land>_<yyyy-MM-dd>.xlsx` bzw. Teil-Dateien. Die zentrale Audit-CSV enthaelt die aufbereiteten Audit-/Merge-Felder inkl. Produktsparte und nutzt bewusst kein `Sales_*`-Praefix. Seit 2026-06-30 kann sie als Fallback-Quelle fuer Dashboard/Pruefbuch gelesen werden, wenn keine Standort-CSV vorhanden sind.
- Zentraler Export laedt progressiv: zuerst `Sales_All`, danach `Finance_Dashboard_Audit_All`, danach Nachweis-Excel. Bei mehr als `50'000` Zentralzeilen wird der Nachweis als mehrere kleine Dateien pro TSC/Land erzeugt; pro Datei maximal ca. `25'000` Zeilen, bei Bedarf mit `_Teil01`, `_Teil02` usw. Die vielen Nachweis-Excel sind nur fuer Finance/Andreas zum Pruefen und Herunterladen; sie sind keine technische Dashboard-Quelle.
- Pruefstand 2026-06-17: Produktive `Finance_Dashboard_Nachweis_*_2026-06-17.xlsx` wurden gegen `Finance_Dashboard_Audit_All_2026-06-17.csv` abgeglichen. Gesamt `112'749` Zeilen in Audit und Nachweis, je TSC `delta=0`; USA-Datei `Finance_Dashboard_Nachweis_TRUS_USA_2026-06-17.xlsx` enthaelt `1'344` Zeilen und stimmt. Filterbezug zum Dashboard ueber `Year`, `Country Key`, `Currency`, TSC/Sparte.
- `Management Analyse` bleibt Diagnose-/Plausibilitaetssicht, nicht fuehrende Finance-Zahl.
- Nach UX-Vereinfachung gibt es links eine schnellere Finance-Uebersicht; tiefe Diagnosefunktionen sind unter `Experten` gebuendelt.
- Neuer Expertenpunkt: `3D Datenanalyse` fuer interaktive visuelle Analyse und Simulation.
- `3D Datenanalyse` hat zusaetzlich `Sparten-Kreis je Land`: je Land werden Produktsparte-Sektoren aus der zentralen Spartenzuordnung dargestellt.
- Neuer Expertenpunkt: `Gruppenmarge` als MVP-/Pruefsicht fuer Gruppenmarge je Land, Sparte und Detailzeile.
- Gruppenmarge-Doku: `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`; Multiple-Choice-Entscheidungsbogen: `docs/FINANCE_GRUPPENMARGE_MULTIPLE_CHOICE_2026-06-16.docx`.
- Andreas-Entscheide final 2026-06-29 (a.docx Budget-CHF + b.docx Gruppenmarge). UMGESETZT: (a) Interner-Lieferant-Erkennung: jeder Lieferant mit „Trafag" in Name/Nummer gilt als intern/Intercompany („weil wir die Trafag sind"); zentral in `Services/GroupMarginSupplierClassifier.cs`, mit Unit-Tests. (Hinweis: zuerst faelschlich auf 3 Gesellschaften eingegrenzt, Commit `29f4f82`, dann auf breiten Trafag-Match korrigiert, Commit `e9894ce`.) (b) DE-Finance-Jahr folgt dem Fakturierungsdatum (InvoiceDate) statt erzwungenem 2025: DE-`ForceYear`-Regel aus `CreateDefaultRules` entfernt und bestehende Regel im Seed deaktiviert (reversibel im Admin). Nach Deploy + DE-Import wandern 2026er DE-Rechnungen ins Jahr 2026. `123/123` Tests gruen.
- Andreas bestaetigt als bereits korrekt umgesetzt: externe Lieferanten nutzen Kosten aus der Verkaufszeile (Q2), interne Lieferkette nur eine Iteration (Q4), fehlende Standardkosten/Standardpreise werden als `Missing` markiert (nicht 50%, nicht geschaetzt), Anzeige auf 2 Dezimalstellen gerundet bei intern voller Praezision.
- Budget-CHF Entscheide (a.docx): Q1 Finance gibt Kurse frei; Q2 Finance pflegt direkt; Q8/Q9 lokale Waehrung bleibt fuehrend, Group-Currency (CHF) als zusaetzlicher Umschalter.
- Kostenbasis-Spezifikation Gruppenmarge (`Mappe1.xlsx`, von Andreas bestaetigt): `Group Margin = Umsatz + echte Konzern-Herstellkosten` der liefernden Trafag-Gesellschaft statt IC-Verrechnungspreis; Quellen TR AG = MBEW-STPRS, TR IN = SAP B1, TR IT; 3rd Party = Verkaufszeilen-Kosten. NOCH NICHT umgesetzt (braucht neue Datenquellen MBEW-STPRS/SAP B1) — eigenes Feature.
- Group-Currency-Umschalter (CHF) UMGESETZT 2026-06-29: Schalter „Group-Waehrung (CHF)" im Management-Cockpit-Filter. Lokale Waehrung bleibt fuehrend/Default; bei Aktivierung werden Ist-Werte UND Soll-/Referenzwerte mit dem Jahreskurs (aus `CurrencyExchangeRateService`, Datum 31.12. des Finance-Jahres) nach CHF umgerechnet, sodass laenderuebergreifende Summen aufgehen. `AnalyzeFinanceSummaryAsync(..., bool useGroupCurrency)`. Kursbasis = die im System vorhandenen Jahreskurse (aktuell Budgetkurse); Wechsel auf Ist-Jahreskurse jederzeit ueber die Kurspflege moeglich. `124/124` Tests gruen.
- OFFEN / NICHT umgesetzt: (1) Echte Konzern-Standardkosten je Liefergesellschaft (MBEW-STPRS / SAP B1) fuer die korrekte Gruppenmarge (`Mappe1.xlsx`-Logik). (2) Budget-CHF-Spaltenumfang (a.docx Q3) offen. (3) Kursbasis Group-Currency final bestaetigen (Budget- vs. Ist-Jahreskurs).
- Gruppenmarge zeigt `Umsatz` und `Bekannte Kostenbasis`; `Marge` und `%` werden `-`, wenn die Kostenbasis offen ist (`Standardpreis fehlt` oder `Lieferant unklar`).
- Aktueller Gruppenmargen-Datenbefund: AT/TRAT und CH/TRCH haben in den geprueften 2025-Zentraldaten `StandardCost=0` und leere Supplier-Felder. Keine 100%-Marge interpretieren; Status bleibt offen.
- `Management Analyse` hat zusaetzliche Finance-Reiter fuer Laender, Datenstatus, Abweichungen, Gutschriften-Kandidaten und Datenqualitaet.
- Andreas-Wunsch 2026-06-29 umgesetzt: Jeder Datenreiter im Management-Cockpit hat einen `Export to Excel`-Button (Schnelluebersicht, Finance Summary, Laender, Datenstatus, Abweichungen, Gutschriften, Datenqualitaet, Spartenanalyse/Finanzanalyse, Zentrale Zuordnung, Gruppenmarge). Der Button baut die sichtbaren Tabellen des Reiters als mehrblaettrige `.xlsx` in-memory und laedt sie direkt im Browser herunter (kein Server-Schreiben). Technik: `IExcelExportService.CreateWorkbookBytes(IReadOnlyList<ExcelSheetData>)` + JS `trafagDownload.saveBytes` (Base64-Blob). Spalten kommen per Reflection aus den Zeilenmodellen, Zahlen/Datum bleiben typisiert (summier-/sortierbar). `120/120` Tests gruen.
- `Management Analyse` ist links aufklappbar; direkte Navigationspunkte springen in die einzelnen Reiter.
- Neu: `Spartenanalyse` mit Unterreitern `Finanzanalyse` und `Zentrale Zuordnung`.
- Sparten-Finanzanalyse nutzt die TR-AG-/SAP-Referenz, nicht lokale ERP-Sparten anderer Laender.
- Sparten-Finanzanalyse bietet Gruppierung nach `PAPH1 Detail`, `Produktfamilie`, `Produktsparte`, optional `Top 10`, Laenderflaggen und visuelle Sparten-Icons.
- Spartenmapping ist auf den neuen vollstaendigen SAP-OData-Referenzservice vorbereitet: `ProductDivisionRefSet` fuehrend, `ProductDivisionMapSet` im Seed inaktiv, Produktfelder direkt aus `P.*`.
- `Übrige` (`ProductDivisionCode = 0008`) ist eigene gueltige Kategorie und wird getrennt von `Nicht zugeordnet` und `Nicht im TR-AG-Stamm` angezeigt.
- Der OData-Import-Join normalisiert `Matnr` beidseitig, damit SAP-18-stellig mit fuehrenden Nullen gegen lokale Nummern ohne fuehrende Nullen matcht.
- Live-Check nach SAP-Fix 2026-06-15: `travp762/.../ZPOWERBI_EINKAUF_SRV/ProductDivisionRefSet` liefert `48'897` Referenzzeilen, `48'895` assigned, `8'715` Uebrige (`0008`), `2` UNASS. Der vorherige Totalausfall durch falsche SAP-Methode ist nicht mehr aktuell.
- Finance-OData nach SAP-Fix: `FinanzdataSchweizOeSet/$count` liefert `30'642`; `Gjahr eq '2025'` ebenfalls `30'642`; `Gjahr eq '2026'` `0`. Refresh wurde danach noch nicht gestartet.
- Import-Guardrail verhindert weiter, dass ein komplett unzugeordneter Referenzlauf oder eine leere Umsatzquelle bestehende Dashboard-Daten ueberschreibt.
- Finance-Schulung `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` ist auf Stand 2026-06-17 nachgezogen: Nachweis-Excel, zentrale Audit-CSV, SharePoint `Import/Finance/Alle`, Gruppenmarge und 3D-Sparten-Kreis je Land.
- Filter fuer Jahr, Land und Waehrung wirken auf das Finance-Endergebnis.
- Standard-Ist bleibt inklusive Positionen; Intercompany/2nd-party wird separat ausgewiesen.
- Nach Sitzung 2026-06-01: ES-Referenz 2025 ist auf `3'082'320.18 EUR` korrigiert; alter Sollwert `3'102'333.61 EUR` war Referenz-/Excel-Fehler.
- Management Analyse zeigt in `Laender` jetzt IC/2nd-party und `Ist ohne IC` als Diagnose.
- Wechselkurs-Anwendungsdatum ist in Settings konfigurierbar und wird in der Rohdaten-Diagnose angezeigt.
- Spartenanalyse war mit >90% nicht zugeordnet fachlich unplausibel; Materialabgleich normalisiert fuehrende Nullen und warnt bei >=90% ungeklaerter Abdeckung.
- Budgetkurse wurden als Finance-Kurse behandelt; CHF-Sicht bleibt getrennte Reporting-/Kontrollsicht, nicht stiller Ersatz fuer Hauswaehrungsabgleich.
- Budget-CHF ist nachdokumentiert: `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` enthaelt nur offene Finance-Fragen; `docs/FINANCE_BUDGET_CHF_MULTIPLE_CHOICE_2026-06-16.docx` ist der Multiple-Choice-Entscheidungsbogen fuer den Finanzchef.
- Wechselkurs-Audit: zentrale Finance Summary und zentrales Excel bleiben Local Currency; fuer eine kuenftige Budget-CHF-Spalte muss explizit `Notes = Budget <Jahr>` genutzt werden, weil offene ECB-Kurse sonst Budgetkurse uebersteuern koennen.
- Fokusdoku zum isolierten Kursfluss: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`.
- India/TRIN: produktive Route nach Fix/Deploy 2026-06-10 ist `SAGE -> 20.197.20.60:30015`, Schema `TRAFAG_LIVE`; Standort-Override nutzt `TRAFAGCONTROLS`.
- DE/Alphaplan lokal umgesetzt: `invoice_headers.csv` und `invoice_lines.csv` werden als Paar gelesen; Vollbestand plus `delta`-Unterordner werden vor dem Speichern dedupliziert.
- DE-Spartenhinweis: Alphaplan `ArtikelNummer` wird als lokale Materialnummer importiert, aber nicht als garantierte TR-AG-/SAP-`MATNR`.
- Browser-Hinweis: 3D-Ansicht wurde in Chrome als korrekt bestaetigt; Firefox zeigte auf dem Client Interaktions-/Zoomprobleme.

## Wichtige Regeln

- Hauswaehrung des Landessystems ist fuehrend.
- Wertbasis ist Nettofakturawert pro Position.
- Jahresabgrenzung ueber `PostingDate`, Fallback `InvoiceDate`, danach `ExtractionDate`.
- Gutschriften/Storno laufen als negative Beleg-/Positionszeilen.
- Budget-CHF ist Kontroll-/Reporting-Kandidat, nicht Standardabgleich.
- Gruppenmarge ist bis zur Fachfreigabe nur Pruefsicht, nicht fuehrender Finance-Abschlusswert.
- `DocumentRate` aus dem ERP ist ein gespeichertes Quellfeld; die App-Kurstabelle wird nur bei Anzeige-Waehrung, expliziter `ConvertCurrency`-Transformation oder Budget-CHF-Kandidat verwendet.
- Schalter fuer Finance/Revision: `Einstellungen > Export Einstellungen > Audit-CSV / nachvollziehbarer Datenfluss`.

## Offene Fachpunkte

- DE: Finance/Munir muss bestaetigen, welche Kundenlaender/Filter zum offiziellen DE-Ist gehoeren.
- IT: Nach neuem IT-Export pruefen, ob die vollstaendige `Trafag Italia`-Summe sichtbar wird.
- UK: Sage-Restdifferenz ueber Exportvollstaendigkeit, Discounts, Freight/Charges und 2nd-party klaeren.
- Spartenanalyse: Falls weiterhin >90% nicht zugeordnet, TR-AG-Referenz/Join/Materialnummern pruefen.
- Produktsparten-OData: nach SAP-Fix plausibel; vor/bei Refresh trotzdem Guardrail und Ergebniszahlen pruefen. Naechster Schritt ist Deploy/App-Start mit Seed fuer direkte `P.*`-Mappings und danach ZSCHWEIZ-Refresh.
- Budget-CHF: Finanzchef muss Budgetkurse/Freigabe, Pflegeprozess, Spaltenumfang, Fehlkursverhalten, Rundung, Anzeigeort, DE-2026-Umschaltung und Kontrollnachweis entscheiden.
- Gruppenmarge: Andreas/Finance muss per Multiple Choice Lieferantenerkennung, externe/interne Kostenbasis, MBEW-STPRS-Fallback, Kettenlogik, Waehrung und Fehlkostenverhalten entscheiden.

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

- Entscheide: `docs/FINANCE_ENTSCHEIDE.md`, `entscheide.md`
- Finance-Schulung: `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md`
- Formeln je Land: `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`
- Isolierter Kurs-Workflow: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`
- IT Detail: `docs/FINANCE_IT_VORGEHEN_2026-05-18.md`
- UK Korrektur: `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md`
- ES Detail: `SAGE_SPAIN_EXPORT_2026-05-05.md`
- alter Finance-Handoff: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
