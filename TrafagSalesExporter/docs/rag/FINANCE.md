# RAG Finance

Stand: 2026-06-10

## Kurzstand

- Fuehrende Sicht: `Finance Summary`.
- Neu lokal: `Finance Summary`, zentrale Excel und Management-Analyse koennen wahlweise aus Audit-CSV statt direkt aus `CentralSalesRecords` lesen. Die Audit-CSV werden nach Mapping und Transformation geschrieben und dienen der Nachvollziehbarkeit fuer Finance/Revision.
- `Finance Summary` nutzt dieselbe `FinanceRuleEngine` wie das zentrale Excel.
- `Management Analyse` bleibt Diagnose-/Plausibilitaetssicht, nicht fuehrende Finance-Zahl.
- Nach UX-Vereinfachung gibt es links eine schnellere Finance-Uebersicht; tiefe Diagnosefunktionen sind unter `Experten` gebuendelt.
- Neuer Expertenpunkt: `3D Datenanalyse` fuer interaktive visuelle Analyse und Simulation.
- `Management Analyse` hat zusaetzliche Finance-Reiter fuer Laender, Datenstatus, Abweichungen, Gutschriften-Kandidaten und Datenqualitaet.
- `Management Analyse` ist links aufklappbar; direkte Navigationspunkte springen in die einzelnen Reiter.
- Neu: `Spartenanalyse` mit Unterreitern `Finanzanalyse` und `Zentrale Zuordnung`.
- Sparten-Finanzanalyse nutzt die TR-AG-/SAP-Referenz, nicht lokale ERP-Sparten anderer Laender.
- Sparten-Finanzanalyse bietet Gruppierung nach `PAPH1 Detail`, `Produktfamilie`, `Produktsparte`, optional `Top 10`, Laenderflaggen und visuelle Sparten-Icons.
- Finance-Schulung dokumentiert die neuen Spartenfunktionen im Tab `Spartenanalyse`.
- Filter fuer Jahr, Land und Waehrung wirken auf das Finance-Endergebnis.
- Standard-Ist bleibt inklusive Positionen; Intercompany/2nd-party wird separat ausgewiesen.
- Nach Sitzung 2026-06-01: ES-Referenz 2025 ist auf `3'082'320.18 EUR` korrigiert; alter Sollwert `3'102'333.61 EUR` war Referenz-/Excel-Fehler.
- Management Analyse zeigt in `Laender` jetzt IC/2nd-party und `Ist ohne IC` als Diagnose.
- Wechselkurs-Anwendungsdatum ist in Settings konfigurierbar und wird in der Rohdaten-Diagnose angezeigt.
- Spartenanalyse war mit >90% nicht zugeordnet fachlich unplausibel; Materialabgleich normalisiert fuehrende Nullen und warnt bei >=90% ungeklaerter Abdeckung.
- Budgetkurse wurden als Finance-Kurse behandelt; CHF-Sicht bleibt getrennte Reporting-/Kontrollsicht, nicht stiller Ersatz fuer Hauswaehrungsabgleich.
- Fokusdoku zum isolierten Kursfluss: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`.
- India/TRIN: produktive Route nach Fix/Deploy 2026-06-10 ist `SAGE -> 20.197.20.60:30015`, Schema `TRAFAG_LIVE`; Standort-Override nutzt `TRAFAGCONTROLS`.
- Browser-Hinweis: 3D-Ansicht wurde in Chrome als korrekt bestaetigt; Firefox zeigte auf dem Client Interaktions-/Zoomprobleme.

## Wichtige Regeln

- Hauswaehrung des Landessystems ist fuehrend.
- Wertbasis ist Nettofakturawert pro Position.
- Jahresabgrenzung ueber `PostingDate`, Fallback `InvoiceDate`, danach `ExtractionDate`.
- Gutschriften/Storno laufen als negative Beleg-/Positionszeilen.
- Budget-CHF ist Kontroll-/Reporting-Kandidat, nicht Standardabgleich.
- `DocumentRate` aus dem ERP ist ein gespeichertes Quellfeld; die App-Kurstabelle wird nur bei Anzeige-Waehrung, expliziter `ConvertCurrency`-Transformation oder Budget-CHF-Kandidat verwendet.

## Offene Fachpunkte

- DE: Finance/Munir muss bestaetigen, welche Kundenlaender/Filter zum offiziellen DE-Ist gehoeren.
- IT: Nach neuem IT-Export pruefen, ob die vollstaendige `Trafag Italia`-Summe sichtbar wird.
- UK: Sage-Restdifferenz ueber Exportvollstaendigkeit, Discounts, Freight/Charges und 2nd-party klaeren.
- Spartenanalyse: Falls weiterhin >90% nicht zugeordnet, TR-AG-Referenz/Join/Materialnummern pruefen.

## Management-Analyse-Reiter

- `Finance Summary`: KPI-Karten und Summen wie im zentralen Excel.
- `Laender`: Ist, IC/2nd-party, Ist ohne IC, Soll, Differenz, Status, Quelle und TSC je Land/Waehrung.
- `Datenstatus`: Standortbestand, letzte Speicherung, letzter Export, Manual-Import-Hinweise.
- `Abweichungen`: Soll/Ist-Abweichungen sortiert nach Betrag.
- `Gutschriften`: technische Kandidaten ueber negative Werte und erkennbare Belegtypen/-nummern.
- `Datenqualitaet`: fehlende Materialnummern, ProductGroup, Waehrung, Kunde, Datum, Nullwerte und ausgeschlossene Zeilen.
- `Spartenanalyse > Finanzanalyse`: Umsatzabdeckung und Umsatz nach Produktsparte/Familie/PAPH1 auf Basis der TR-AG-Referenz.
- `Spartenanalyse > Zentrale Zuordnung`: Materialnummern aller Laender gegen TR-AG-Stamm pruefen.
- `Rohdaten Diagnose`: direkte Plausibilitaets-/Rohdatensicht auf `CentralSalesRecords`.

## Experten / 3D Datenanalyse

- Unter `Experten` gibt es den Punkt `3D Datenanalyse`.
- Zweck: Verlauf und Kennzahlen im Raum betrachten, nicht Ersatz fuer den offiziellen Soll/Ist-Wert.
- Funktionen:
  - drehbare 3D-Ansicht mit Maus.
  - Achsenbeschriftung fuer Zeit/Wert/Indikator.
  - Auswahl sinnvoller Finance-Indikatoren.
  - Diagrammarten wie Balken/Linien/weitere Analyseformen.
  - einstellbare Labelgroesse.
  - Schieberegler fuer Szenarien, u. a. Wechselkursveraenderungen.
  - Realtime-Neuberechnung bei Szenarioaenderungen.
- Bekannter Hinweis: Wenn Interaktion/Zoom in Firefox fehlerhaft ist, mit Chrome pruefen.

## Spartenanalyse Kurzlogik

- Statuswerte:
  - `Zugeordnet`: Material im TR-AG-Stamm gefunden und Sparte verwertbar.
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
| DE | Alphaplan Excel, `NettoPreisGesamtX`, 2025-Zwang |
| ES | Sage CSV, `ImporteNeto`, REC/Credit negativ; Referenz 2025 korrigiert auf `3'082'320.18 EUR` |
| IT | Hauswaehrung, `Trafag Italia` ausgeschlossen, Duplikatlogik fuer leeres Supplier country |
| UK | Sage/Manual Excel, GBP, `[Sales Price/Value] * [Quantity]`, Credit Notes negativ |
| IN | SAGE/HANA `TRIN`, Schema `TRAFAG_LIVE`, INR als Hauswaehrung |

## Rohquellen Nur Bei Bedarf

- Entscheide: `docs/FINANCE_ENTSCHEIDE.md`, `entscheide.md`
- Formeln je Land: `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`
- Isolierter Kurs-Workflow: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`
- IT Detail: `docs/FINANCE_IT_VORGEHEN_2026-05-18.md`
- UK Korrektur: `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md`
- ES Detail: `SAGE_SPAIN_EXPORT_2026-05-05.md`
- alter Finance-Handoff: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
