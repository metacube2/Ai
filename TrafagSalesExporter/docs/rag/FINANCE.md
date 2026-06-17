# RAG Finance

Stand: 2026-06-17

## Kurzstand

- Fuehrende Sicht: `Finance Summary`.
- Aktuelle Schulung: `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md`.
- `Finance Summary`, zentrale Excel, Soll/Ist und Management-Analyse koennen wahlweise aus Audit-CSV statt direkt aus `CentralSalesRecords` lesen. Die Audit-CSV werden nach Mapping und Transformation geschrieben und dienen der Nachvollziehbarkeit fuer Finance/Revision.
- Audit-CSV-Dateiname: `Sales_ProcessedMergeInput_<TSC>_<yyyy-MM-dd>.csv`; liegt im gleichen Ordner wie das Standort-Excel und wird beim Standortexport in denselben SharePoint-Landesordner hochgeladen.
- `Finance Summary` nutzt dieselbe `FinanceRuleEngine` wie das zentrale Excel.
- Nachweis fuer Excel-Fans: `Zentrale Datei neu erzeugen` erstellt zusaetzlich `Finance_Dashboard_Nachweis_<yyyy-MM-dd>.xlsx` im waehlbaren zentralen Exportordner. Die Datei enthaelt Formel-Summaries (`SUMIFS`, `COUNTIFS`, `IF`) und Detailblaetter fuer Finance, Soll/Ist, Sparten, Gruppenmarge und Datenqualitaet. Doku: `docs/FINANCE_DASHBOARD_NACHWEIS_2026-06-17.md`.
- Zentraler Finance-Export laedt nach SharePoint `Import/Finance/Alle`: `Sales_All_<yyyy-MM-dd>.xlsx`, `Finance_Dashboard_Nachweis_<yyyy-MM-dd>.xlsx` und `Finance_Dashboard_Audit_All_<yyyy-MM-dd>.csv`. Die zentrale Audit-CSV enthaelt die aufbereiteten Audit-/Merge-Felder inkl. Produktsparte und nutzt bewusst kein `Sales_*`-Praefix, damit sie nicht als zusaetzlicher Audit-Input wieder eingelesen wird.
- `Management Analyse` bleibt Diagnose-/Plausibilitaetssicht, nicht fuehrende Finance-Zahl.
- Nach UX-Vereinfachung gibt es links eine schnellere Finance-Uebersicht; tiefe Diagnosefunktionen sind unter `Experten` gebuendelt.
- Neuer Expertenpunkt: `3D Datenanalyse` fuer interaktive visuelle Analyse und Simulation.
- `3D Datenanalyse` hat zusaetzlich `Sparten-Kreis je Land`: je Land werden Produktsparte-Sektoren aus der zentralen Spartenzuordnung dargestellt.
- Neuer Expertenpunkt: `Gruppenmarge` als MVP-/Pruefsicht fuer Gruppenmarge je Land, Sparte und Detailzeile.
- Gruppenmarge-Doku: `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`; Multiple-Choice-Entscheidungsbogen: `docs/FINANCE_GRUPPENMARGE_MULTIPLE_CHOICE_2026-06-16.docx`.
- Gruppenmarge zeigt `Umsatz` und `Bekannte Kostenbasis`; `Marge` und `%` werden `-`, wenn die Kostenbasis offen ist (`Standardpreis fehlt` oder `Lieferant unklar`).
- Aktueller Gruppenmargen-Datenbefund: AT/TRAT und CH/TRCH haben in den geprueften 2025-Zentraldaten `StandardCost=0` und leere Supplier-Felder. Keine 100%-Marge interpretieren; Status bleibt offen.
- `Management Analyse` hat zusaetzliche Finance-Reiter fuer Laender, Datenstatus, Abweichungen, Gutschriften-Kandidaten und Datenqualitaet.
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
- `Rohdaten Diagnose`: direkte Plausibilitaets-/Rohdatensicht auf die zentrale Auswertungsquelle.

## Audit-CSV / Auswertungsquelle

- `Audit-CSV je Standort schreiben`: schreibt beim Laenderexport eine verarbeitete CSV nach Mapping und Transformation.
- `Zentrale Auswertung aus Audit-CSV`: zentrale Auswertungen lesen je TSC die neueste `Sales_ProcessedMergeInput_*.csv`.
- Der Pfad ist der `Lokaler Standardpfad Standort-Dateien`; ein separater sichtbarer Audit-Pfad wird nicht verwendet.
- Standard ohne CSV-Schalter: zentrale Auswertungen lesen `CentralSalesRecords`.
- Wenn der CSV-Schalter aktiv ist und keine passenden CSV vorhanden sind, ist die zentrale Auswertung nicht ausfuehrbar.

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
