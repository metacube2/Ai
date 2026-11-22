# RAG Finance

Stand: 2026-08-17

Kanonischer Live-Abgleich fuer UK-2025, Supplier-Felder und
`GroupStandardCosts`: `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.
Bei Abweichungen hat dieser direkt gepruefte Produktivstand Vorrang.

Formeln/Mechanik: `docs/rag/FINANCE_FORMELN.md`. Historische Messungen und
ersetzte Zwischenstaende stehen in den Detaildokumenten und in
`docs/raw_md_archive/`.

## Kurzstand

- ES BUCHUNGSDATUM 2026-08-17: die spanische Export-SQL selektiert jetzt
  `FacturasTB.FechaAsiento` als `PostingDate` und `FacturasTB.Asiento` als
  `PostingDocument`, per `OUTER APPLY` mit `TOP 1` statt `JOIN` (70 von 3'642
  Rechnungsschluesseln haben mehrere Buchungszeilen, ein `JOIN` haette den
  spanischen Umsatz vervielfacht). Belegt ist bisher NUR die Syntax, geprueft mit
  `ScriptDom` inklusive Gegenprobe. Trefferquote, Schluesselrichtigkeit und
  Gutschriften sind offen, dafuer braucht es den Lauf auf dem spanischen Server.
  Bis dahin bleiben alle TRES-Zeilen ohne Buchungsdatum und fallen auf das
  Rechnungsdatum zurueck. Danach ist zusaetzlich die Spaltenzuordnung `PostingDate`
  beim Standort Spanien noetig. Details:
  `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` Abschnitt 8.
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
- TR IT: Fuer den ersten Schritt ist `INV1.StockPrice` als Kostenbasis
  freigegeben. Die Bewertung einer Umstellung auf Moving Average und die
  Cost-Run-Frage folgen laut Paola Ende August 2026.
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
- Supplier-Fallback bei Fremdstandorten ohne Supplier und ohne Sales Type: Default
  `MARC/Werk 1100` (CH-Werkstamm), umschaltbar auf die alte
  `MBEW/GroupStandardCosts-1100`-Regel unter `Admin Bereich > Settings`. Ein
  expliziter Supplier gewinnt immer. Der MARC-Cache ist von den Kosten getrennt;
  ohne MBEW-Treffer werden keine Konzernkosten erfunden. Vollnachweis:
  `docs/FINANCE_SUPPLIER.md`.
- Produktiv deployed am 2026-08-11: `SupplierFallbackMode=ChPlantMaster`,
  `66'049` MARC-1100-Materialien dauerhaft vor und nach App-Neustart bestaetigt;
  `63'550` MBEW-Schluessel vollstaendig enthalten. Deploynachweis:
  `docs/DEPLOYMENT.md`.
- Produktiv deployed am 2026-08-12 10:23: Andreas' Nachtrag, dass ein sicherer
  MARC-Nichttreffer die Standardkosten der lokalen Gesellschaft verwendet. Die
  Kategorie heisst bewusst `Lokal`, nicht `Extern`. Gegen die produktive Datenbank
  nach dem Deploy gemessen: 22'950 Kandidatenzeilen, davon 10'817 CH-intern und
  12'023 `Lokal`; 6'749 der lokalen Zeilen haben einen positiven Standardpreis,
  110 Zeilen ohne Materialschluessel bleiben `Lieferant unklar`. Voraussetzung ist
  `SupplierFallbackMode=ChPlantMaster`; im Alt-Modus MBEW greift die Regel nicht.
  Detail: `docs/FINANCE_STANDARDKOSTEN.md`.

## Offene Fachpunkte

- Supplier-Mapping: 77'466 von 95'396 Live-Zeilen haben alle drei
  Supplier-Felder leer. Die Rohdatenluecke bleibt offen, die Ursache je Quelle ist
  weiterhin zu klaeren. Seit dem Deploy vom 2026-08-12 maskiert die Gruppenmarge
  aber nur noch die Zeilen ohne belastbare Pruefgrundlage: der CH-Werkstamm
  entscheidet intern gegen `Lokal`, und `Lieferant unklar` bleibt nur bei fehlendem
  Materialschluessel, fehlender TSC oder leerem MARC-Cache.
- B1-Upgrade ab 2026-08-03 nachpruefen: Import FR/IT/US/IN, Kostenfuellgrad
  und Bewertungsmethoden.
- TR IT: Moving-Average-/Cost-Run-Frage mit Paola Ende August abschliessen;
  der freigegebene Belegebenen-Weg ueber `INV1.StockPrice` bleibt davon
  unabhaengig.
- Budget-CHF: Finance muss Kurse/Freigabe, Pflegeprozess, Spaltenumfang,
  Fehlkursverhalten, Rundung und Anzeigeort entscheiden.
- CH/AT-Journal: SAP-EntitySet `FinanzJournalSet` bleibt Voraussetzung;
  Spezifikation: `docs/FINANCE_JOURNAL.md`.

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
