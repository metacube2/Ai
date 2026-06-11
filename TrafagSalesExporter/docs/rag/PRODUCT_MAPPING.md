# RAG Product Mapping

Stand: 2026-06-11

## Kurzstand

- Neue Anforderung: Artikel aus dem Group Sales Report sollen anhand der TR-AG-Zuordnung klassifiziert werden.
- Ziel-Felder: `Produkthierarchie`, `Produktfamilie`, `Produktsparte`.
- SAP TR AG bleibt Quelle der Wahrheit.
- Dashboard soll KEDR-/KE30-Ableitung nicht in C# nachbauen.
- ABAP/Gateway soll eine flache Referenz liefern: `MATNR -> PAPH1 -> WWPFA -> WWPSP`.
- Nicht gefundene oder nicht eindeutig ableitbare Materialnummern laufen unter `Nicht zugeordnet`.
- Web produktiv: `Management Analyse` enthaelt `Spartenanalyse` mit Unterreitern `Finanzanalyse` und `Zentrale Zuordnung`.
- Sparten-Finanzanalyse kann nach `PAPH1 Detail`, `Produktfamilie` oder `Produktsparte` gruppieren und optional `Top 10` anzeigen.
- Laender werden mit Flaggen angezeigt; Produktsparte erhaelt visuelle Icons nach Textmuster.

## Aktueller Code-Stand

- Vorhanden: `Material`, `ProductGroup`.
- Neu vorhanden in `SalesRecord` und `CentralSalesRecord`:
  - `ProductHierarchyCode`
  - `ProductHierarchyText`
  - `ProductFamilyCode`
  - `ProductFamilyText`
  - `ProductDivisionCode`
  - `ProductDivisionText`
  - `ProductMappingAssigned`
- `CentralSalesRecords` wird per Schema-Maintenance um diese Spalten erweitert.
- Excel-Export fuehrt die neuen Produktfelder im Blatt `Sales` direkt nach `Product Group`.
- SAP-Seed-Mapping nutzt aktuell `Z.Matnr` -> `Material` und `Z.Prodh` -> `ProductGroup`.
- Zu klaeren: Ist `Z.Prodh` fachlich die Produkthierarchie?

## ABAP-Arbeitsstand

- `docs/abap/ZCL_PRODSPARTE_PROVIDER.abap`: Provider fuer ALV und spaeter OData.
- `docs/abap/Z_PRODSPARTE_REPORT.abap`: ALV-Testreport.
- `docs/abap/Z_PRODSPARTE_MAP_BUILD.abap`: baut `ZPRODSPARTE_MAP` aus `CE11000`.
- `docs/abap/README_PRODSPARTE.md`: DDIC- und Pruefhinweise.

## SAP-Zielbild

- `Z_PRODSPARTE_MAP_BUILD` liest reale CO-PA-Ableitungen aus `CE11000`.
- Eindeutige `PAPH1 -> WWPFA -> WWPSP` werden in `ZPRODSPARTE_MAP` gespeichert.
- Mehrdeutige PAPH1 werden protokolliert und nicht geschrieben.
- `ZCL_PRODSPARTE_PROVIDER` liest `MVKE-PRODH`, Texte und Mapping.
- OData-Service ruft dieselbe Provider-Klasse.

## Stand 2026-05-29

- SAP Gateway nutzt bestehenden Service `ZPOWERBI_EINKAUF_SRV`.
- Service Root: `http://travp762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/`.
- Produktmapping-EntitySet: `ProductDivisionRefSet`.
- Test-Endpoint liefert Daten, z.B. `Matnr=VCP1000`, `Paph1=9999`, `Wwpsp=UNASS`.
- OData-Felder sind CamelCase: `Matnr`, `Paph1`, `Paph1Text`, `Wwpfa`, `WwpfaText`, `Wwpsp`, `WwpspText`, `IsAssigned`.
- Bestehender Sales-EntitySet bleibt `FinanzdataSchweizOeSet`.
- Wichtig: `FINANZDATASCHWEI_GET_ENTITYSET` muss weiter den alten `ZSCHWEIZ`-Select enthalten.
- Produktmapping-Code gehoert in `PRODUCTDIVISIONR_GET_ENTITYSET`.
- Lokale App-Konfiguration fuer Standort `ZSCHWEIZ`:
  - Quelle `Z`: bestehender Sales-EntitySet.
  - Quelle `P`: `ProductDivisionRefSet`.
  - Join: `Z.Matnr = P.Matnr`.
  - Mappings: `P.Paph1`, `P.Paph1Text`, `P.Wwpfa`, `P.WwpfaText`, `P.Wwpsp`, `P.WwpspText`, `P.IsAssigned`.
- Lokale App wurde neu gestartet; `http://localhost:55416/` antwortet mit HTTP 200.
- Validierung: `79/79` Tests gruen mit separatem Artefaktpfad.

## Zentrale Spartenzuordnung

- Neuer Reiter in `Management Analyse`: `Zentrale Spartenzuordnung`.
- Zweck: Materialnummern aller Laender gegen die fuehrende TR-AG-/SAP-Referenz pruefen.
- Lokale ERP-Produktzuordnungen anderer Laender sind nicht fuehrend.
- Statuslogik:
  - Treffer mit zugeordneter TR-AG-Sparte: `Zugeordnet`.
  - Treffer mit `UNASS`/nicht zugeordnet: `Nicht zugeordnet`.
  - Kein Treffer im TR-AG-Stamm: `Nicht im TR-AG-Stamm`.
  - Leere Materialnummer: `Material fehlt`.
- Die Sicht nutzt den bestehenden Finance-Filter fuer Jahr/Land/Waehrung.
- Sie zeigt Kennzahlen, Laenderabdeckung und Detailzeilen mit Land-Material links und TR-AG-Referenz rechts.
- Umsetzung ist eine Analyseansicht, keine persistente Mutation anderer Laenderzeilen.
- Validierung nach Umsetzung: `80/80` Tests gruen.

## Spartenanalyse UX 2026-05-29

- Navigation links:
  - `Management Analyse` ist aufklappbar.
  - Direkte Links springen per Query-Parameter in Finance Summary, Laender, Datenstatus, Abweichungen, Gutschriften, Datenqualitaet, Sparten-Finanzanalyse, Zentrale Spartenzuordnung und Rohdaten Diagnose.
- `Spartenanalyse` ist ein Top-Level-Reiter mit Unterreitern:
  - `Finanzanalyse`
  - `Zentrale Zuordnung`
- `Finanzanalyse`:
  - Kennzahlen Gesamt/Zugeordnet/Nicht zugeordnet/Nicht im Stamm.
  - Umsatz nach Produktsparte mit Gruppierung `PAPH1 Detail`, `Produktfamilie`, `Produktsparte`.
  - `Top 10 anzeigen` filtert nur die Anzeige.
  - Laender werden mit Flagge dargestellt.
  - Produktsparte zeigt Icon:
    - Gas/Density -> `Sensors`
    - Pressure/Druck -> `Compress`
    - Temp/Thermostat -> `DeviceThermostat`
    - Switch/Schalter -> `ToggleOn`
    - Access/Zubehoer -> `Extension`
    - UNASS -> `HelpOutline`
    - sonst -> `Category`
- Finance-Schulung hat einen neuen Tab `Spartenanalyse`.

## Stand 2026-06-02

- Niedrige Spartenabdeckung ist als Mapping-/Referenzproblem zu lesen, nicht als fachliche Umsatzverteilung.
- Lokaler DB-Befund: `75'089` CentralSalesRecords, davon `27'047` mit gueltiger zugeordneter Produktsparte und `9'800` mit `UNASS`.
- Produktfelder sind aktuell fast nur bei CH und teilweise AT gefuellt; IT, IN, DE, ES, FR, US und UK haben in den Daten keine Produktspartenfelder.
- Prozessfluss:
  - SAP Service `ZPOWERBI_EINKAUF_SRV`
  - Sales EntitySet `FinanzdataSchweizOeSet`
  - Produktmapping EntitySet `ProductDivisionRefSet`
  - Web-Join `Z.Matnr = P.Matnr`
  - Speicherung in `CentralSalesRecords`
  - Anwendung in `Management Analyse > Spartenanalyse`
- Vollstaendige ABAP-Dateien im Repo:
  - `docs/abap/ZCL_PRODSPARTE_PROVIDER.abap`
  - `docs/abap/Z_PRODSPARTE_MAP_BUILD.abap`
  - `docs/abap/Z_PRODSPARTE_REPORT.abap`
- Nicht als kompletter Gateway-Methodencode im Repo vorhanden:
  - `FINANZDATASCHWEI_GET_ENTITYSET`
  - `PRODUCTDIVISIONR_GET_ENTITYSET`
- Naechster Schritt: Gateway-Methodencode aus SAP sichern oder `PRODUCTDIVISIONR_GET_ENTITYSET` sauber neu formulieren.
- Browser-Favicon wurde ergaenzt: `wwwroot/favicon.svg`.
- Letzter dokumentierter Deploy: 2026-05-29 13:47, Tests `80/80` gruen.

## Offene Punkte Fuer Sitzung

- Normalisierung der Materialnummern.
- Struktur der Mapping-Tabelle von Kendra.
- Matching-Regeln: exakt, Prefix, Range, Prioritaet.
- Historisierung der Zuordnung fuer reproduzierbare Reports.
- Pruefansicht fuer nicht zugeordnete Artikel.
- `PAPH1 = MVKE-PRODH(5)` fachlich/technisch bestaetigen.
- Richtige Texttabellen fuer `WWPFA`/`WWPSP` bestaetigen.
- VKORG/VTWEG fuer TR-AG-Referenzlauf bestaetigen.
- Standort `ZSCHWEIZ` im Export Dashboard neu laufen lassen und Fuellung der neuen Produktfelder pruefen.

## Komponenten-Fallback 2026-06-11

- CH/TRCH enthaelt auch Komponenten-MATNR mit Buchstabenpraefix; `nicht_im_stamm_TRCH_alle_jahre.csv` enthaelt 804 solche Materialien ohne Treffer.
- Fachlicher Fallback: `ZPOWERBI_VC_TXT-KOMPNR -> ZPOWERBI_VC_TXT-MATNR` und danach Sparte vom Kopfmaterial erben.
- Minimaler Code-Ort: `ZCL_PRODSPARTE_PROVIDER=>GET_DATA`; keine SEGW-/Web-App-Metadata-Aenderung.
- Schutzregel: automatisch nur uebernehmen, wenn alle Kopfmaterialien dieselbe `WWPSP` ergeben.
- Prod `travp762` liefert `ProductDivisionRefSet`, `ProductDivisionMapSet`, `FinanzdataSchweizOeSet`; aktueller Check: 42'486 Ref-Zeilen, 804 CSV-Materialien, 0 Treffer. Fallback in OData noch nicht wirksam.
- Treffer-/Fehlerquote im Reiter `Zentrale Spartenzuordnung` pruefen.

## Rohquelle Nur Bei Bedarf

- Detaildoku: `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md`
