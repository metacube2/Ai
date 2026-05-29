# Last Change

Stand: 2026-05-29

Diese Datei ist fuer tokenarme RAG-Nutzung komprimiert.

## Aktueller Kurzstand

- Fuehrender Kurzkontext: `docs/rag/PROJECT.md`.
- Themenrouter: `docs/RAG_ROUTER.md`.
- Letzter dokumentierter Code-Stand: `36ca822 Add browser favicon`, alle Aenderungen bis 2026-05-29 13:47 deployt.
- Letzte dokumentierte Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-favicon` mit `80/80` Tests gruen.
- Neu dokumentiert: Produktsparten-Mapping fuer Group Sales Report ueber TR-AG-Artikelstamm und separate Mapping-Tabelle.
- Neu dokumentiert: Upgreat-Firewall-Freigabe muss fuer den publizierten Webserver `10.120.1.17` erfolgen, nicht fuer den lokalen Entwicklungs-PC.
- Neu umgesetzt: `Management Analyse` im Finance Cockpit hat zusaetzliche Reiter fuer Laender, Datenstatus, Abweichungen, Gutschriften-Kandidaten und Datenqualitaet.
- Neu erstellt: ABAP-Arbeitsstand fuer Produktsparten-Mapping mit Provider-Klasse, ALV-Report und Mapping-Build-Report.
- Neu umgesetzt: Produktspartenfelder im Web-Datenmodell, Gateway-Join-Konfiguration fuer `ProductDivisionRefSet` und Excel-Ausgabe.
- Neu umgesetzt und deployed: Reiter `Zentrale Spartenzuordnung` in `Management Analyse`, der Finance-Materialien gegen die fuehrende TR-AG-/SAP-Referenz prueft.
- Neu umgesetzt und deployed: Reiter `Sparten-Finanzanalyse` in `Management Analyse`, der Umsatzabdeckung und Umsatz nach Produktsparte aus der zentralen Spartenzuordnung berechnet.
- Neu umgesetzt und deployed: `Management Analyse` ist in der linken Navigation aufklappbar; direkte Links springen in Finance Summary, Laender, Datenstatus, Abweichungen, Gutschriften, Datenqualitaet, Spartenanalyse und Rohdaten Diagnose.
- Neu umgesetzt und deployed: Spartenanalyse ist als Hauptreiter mit Unterreitern `Finanzanalyse` und `Zentrale Zuordnung` strukturiert.
- Neu umgesetzt und deployed: Sparten-Finanzanalyse kann nach `PAPH1 Detail`, `Produktfamilie` oder `Produktsparte` aggregieren, optional `Top 10` anzeigen und Laender mit Flaggen darstellen.
- Neu umgesetzt und deployed: Produktsparte zeigt visuelle Kategorie-Icons fuer Gas/Density, Pressure/Druck, Temperatur/Thermostat, Switch/Schalter, Access/Zubehoer, UNASS und Sonstige.
- Neu umgesetzt und deployed: Finance-Schulung hat einen neuen Tab `Spartenanalyse` mit Navigation, Gruppierung, Top 10, Flaggen, Icons und Statusinterpretation.
- Neu umgesetzt und deployed: Browser-Favicon `wwwroot/favicon.svg` und Head-Link in `Components/App.razor`.
- Letzter Deploy: 2026-05-29 13:47 auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Letzte Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-favicon` mit `80/80` Tests gruen.

## Nachtrag 2026-05-29 Management Analyse UX / Spartenanalyse / Favicon

Umgesetzt und deployed:

- `Management Analyse` ist in der linken Navigation als `MudNavGroup` aufklappbar.
- Direkte Navigationspunkte:
  - `Finance Summary`
  - `Laender`
  - `Datenstatus`
  - `Abweichungen`
  - `Gutschriften`
  - `Datenqualitaet`
  - `Sparten-Finanzanalyse`
  - `Zentrale Spartenzuordnung`
  - `Rohdaten Diagnose`
- Die Navigation nutzt Query-Parameter (`section`, `division`), und `ManagementCockpit.razor` bindet diese auf feste Reiter-Indizes.
- Die bisherigen Top-Level-Reiter `Sparten-Finanzanalyse` und `Zentrale Spartenzuordnung` wurden in einen Top-Level-Reiter `Spartenanalyse` mit Unterreitern zusammengefuehrt:
  - `Finanzanalyse`
  - `Zentrale Zuordnung`
- `Sparten-Finanzanalyse` hat neue Controls:
  - Dropdown `Gruppierung`: `PAPH1 Detail`, `Produktfamilie`, `Produktsparte`
  - Button `Top 10 anzeigen` mit Filter-Icon
  - dynamische Spaltenausblendung je Gruppierung
- Aggregation:
  - Umsatz, Anteil, Zeilen und Laender werden je Gruppierung neu berechnet.
  - `Top 10` filtert nur die Anzeige, nicht die zugrunde liegende Berechnungsbasis.
  - Laender werden mit Flagge formatiert.
- Visuelle Produktsparte-Icons:
  - Gas/Density -> `Sensors`
  - Pressure/Druck -> `Compress`
  - Temp/Thermostat -> `DeviceThermostat`
  - Switch/Schalter -> `ToggleOn`
  - Access/Zubehoer -> `Extension`
  - UNASS/Nicht zugeordnet -> `HelpOutline`
  - sonst -> `Category`
- Finance-Schulung:
  - Neuer Schulungs-Tab `Spartenanalyse`.
  - Dokumentiert Navigation, Gruppierung, Top 10, Flaggen, Icons und Statusinterpretation.
- Browser:
  - Neues SVG-Favicon `wwwroot/favicon.svg`.
  - Eingebunden in `Components/App.razor` via `<link rel="icon" type="image/svg+xml" href="favicon.svg" />`.

Commits:

- `dc2bc7d Group division analysis tabs`
- `0a7aafb Add management analysis navigation group`
- `3c82747 Add division finance grouping controls`
- `18208cb Add product division category icons`
- `61de1be Document division analysis in finance training`
- `674c103 Expose management analysis tabs in navigation`
- `36ca822 Add browser favicon`

Validierungen:

- Mehrfach `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit separaten Artefaktpfaden.
- Letzter dokumentierter Testlauf: `80/80` Tests gruen.
- Letzter Webserver-Deploy: `BiDashboard.dll` aktualisiert am `29.05.2026 13:47:36`.

## Nachtrag 2026-05-29 Produktsparten-Mapping Gateway/Web

SAP/Gateway:

- Bestehender Service wird verwendet: `ZPOWERBI_EINKAUF_SRV`.
- Service Root: `http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/`.
- Neuer Entity Type/Entity Set:
  - `ProductDivisionRef`
  - `ProductDivisionRefSet`
- Entity Type basiert auf `ZSTR_PRODSPARTE_OUT`.
- Gateway-Test liefert Daten, Beispiel:
  - `Matnr = VCP1000`
  - `Paph1 = 9999`
  - `Wwpsp = UNASS`
  - `WwpspText = Nicht zugeordnet`
- Wichtig: `FINANZDATASCHWEI_GET_ENTITYSET` ist der bestehende Sales-EntitySet und muss den alten `ZSCHWEIZ`-Select behalten. Produktspartenlogik gehoert in `PRODUCTDIVISIONR_GET_ENTITYSET`.
- Fehler `/IWFND/MED/170` wurde als fehlender Slash zwischen Service und EntitySet identifiziert.

Web/App:

- Neue Felder in `SalesRecord` und `CentralSalesRecord`:
  - `ProductHierarchyCode`
  - `ProductHierarchyText`
  - `ProductFamilyCode`
  - `ProductFamilyText`
  - `ProductDivisionCode`
  - `ProductDivisionText`
  - `ProductMappingAssigned`
- `CentralSalesRecords` erhaelt die Spalten per Schema-Maintenance.
- `CentralSalesRecordService` liest/schreibt die Felder.
- Excel-Export fuehrt die Produktfelder im Blatt `Sales` direkt nach `Product Group`.
- Manual-Excel-Header-Mapping kennt die neuen Feldnamen.
- Lokale DB-Konfiguration fuer Standort `ZSCHWEIZ`:
  - Quelle `P`: `ProductDivisionRefSet`
  - Join: `Z.Matnr = P.Matnr`
  - Mappings: `P.Paph1`, `P.Paph1Text`, `P.Wwpfa`, `P.WwpfaText`, `P.Wwpsp`, `P.WwpspText`, `P.IsAssigned`
- Lokaler Neustart durchgefuehrt; `http://localhost:55416/` antwortet mit HTTP 200.
- Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-productmapping` mit `79/79` Tests gruen.

Offen:

- `ZSCHWEIZ` im Export Dashboard neu laufen lassen.
- Danach Fuellung der neuen Produktfelder und Quote `UNASS` pruefen.
- Fachliche Mapping-Luecken wie `0509`/`0540` spaeter mit Andreas/Kendra klaeren.
- Wenn `TR-AG Referenz = 0` angezeigt wird, ist die zentrale Referenz im Web noch leer. Dann `ZSCHWEIZ` nach aktivem `ProductDivisionRefSet`-Join erneut exportieren/laden.

## Nachtrag 2026-05-29 Zentrale Spartenzuordnung

Umgesetzt:

- Neuer Reiter in `Management Analyse`: `Zentrale Spartenzuordnung`.
- Fachlogik:
  - Andere Laender-ERPs sind fuer Produktsparten nicht fuehrend.
  - Fuehrend ist die TR-AG-/SAP-Referenz aus `ProductDivisionRefSet`.
  - Umsatzzeilen aus `CentralSalesRecords` werden ueber `Material` gegen die TR-AG-Referenz geprueft.
- Statuswerte:
  - `Zugeordnet`
  - `Nicht zugeordnet`
  - `Nicht im TR-AG-Stamm`
  - `Material fehlt`
- Der Reiter zeigt:
  - Summary-Kennzahlen
  - Abdeckung nach Land/TSC
  - Detailtabelle mit Land-Material links und TR-AG-MATNR/PAPH1/Familie/Sparte rechts.
- Die Sicht verwendet die bestehenden Finance-Filter fuer Jahr, Land und Waehrung.
- Noch keine persistente Mutation anderer Laenderzeilen; es ist bewusst eine Pruefansicht.

Technisch:

- Neue Modelle in `ManagementCockpitModels`.
- Produktzuordnungsanalyse in `ManagementCockpitService`.
- Neuer Reiter in `Components/Pages/ManagementCockpit.razor`.
- Test ergaenzt: `AnalyzeFinanceSummaryAsync_Builds_Central_Product_Assignment_Tab_Data`.
- Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-central-product-assignment` mit `80/80` Tests gruen.

## Nachtrag 2026-05-29 Sparten-Finanzanalyse

Umgesetzt:

- Neuer Reiter in `Management Analyse`: `Sparten-Finanzanalyse`.
- Grundlage sind die bestehenden Statuswerte aus `Zentrale Spartenzuordnung`, damit Materialstatus und Finanzwerte identisch abgegrenzt sind.
- Kennzahlen:
  - Gesamtumsatz
  - Zugeordneter Umsatz
  - Nicht zugeordneter Umsatz
  - Umsatz nicht im TR-AG-Stamm
- Tabellen:
  - Umsatz nach Produktsparte mit Produktsparte, Produktfamilie, PAPH1, Umsatz, Anteil, Materialanzahl, Zeilen und Laendern.
  - Umsatzabdeckung nach Land/TSC mit Gesamt, Zugeordnet, Nicht zugeordnet, Nicht im Stamm, Material fehlt und Abdeckungsquote.
- Seed-Fix:
  - SAP-Quelle `P = ProductDivisionRefSet` wird beim App-Start nicht mehr deaktiviert.
  - Join `Z.Matnr = P.Matnr` und Produktfeld-Mappings werden als Standard gepflegt.
- Server-DB nach Deploy geprueft:
  - `ProductRows = 36'847`
  - `TR-AG Referenzmaterialien = 6'805`
  - `ProductDivisionRefSet` aktiv.
- Deploy: `BiDashboard.dll` auf Server aktualisiert am `29.05.2026 10:42`.
- Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-division-finance` mit `80/80` Tests gruen.

## Nachtrag 2026-05-28 ABAP Produktsparten-Mapping

Erstellt:

- `docs/abap/ZCL_PRODSPARTE_PROVIDER.abap`
- `docs/abap/Z_PRODSPARTE_REPORT.abap`
- `docs/abap/Z_PRODSPARTE_MAP_BUILD.abap`
- `docs/abap/README_PRODSPARTE.md`

Dokumentierter Zielansatz:

- SAP TR AG bleibt Quelle der Wahrheit fuer `MATNR -> PAPH1 -> WWPFA -> WWPSP`.
- Mapping-Build liest reale CO-PA-Ableitungen aus `CE11000` und schreibt eindeutige Saetze in `ZPRODSPARTE_MAP`.
- Provider liest verkaufsrelevante Materialien aus `MVKE`, Texte aus SAP-Texttabellen und Mapping aus `ZPRODSPARTE_MAP`.
- ALV-Report und spaeter OData sollen dieselbe Provider-Methode verwenden.
- Nicht zugeordnete Materialien erhalten Fallback `UNASS` / `Nicht zugeordnet`.

Offen:

- `PAPH1 = MVKE-PRODH(5)` bestaetigen.
- Texttabellen `T25A0`/`T25A1` bestaetigen.
- Relevante `VKORG`/`VTWEG` fuer TR AG festlegen.
- `CE11000` als richtige CO-PA-Quelle bestaetigen.

## Nachtrag 2026-05-28 Finance Management Analyse Reiter

Umgesetzt:

- `Management Analyse` erweitert die bestehende `Finance Summary` um weitere Reiter im Cockpit-Stil.
- Neue Reiter:
  - `Laender`
  - `Datenstatus`
  - `Abweichungen`
  - `Gutschriften`
  - `Datenqualitaet`
- Grundlage sind vorhandene Daten aus `CentralSalesRecords`, `FinanceReferences`, `Sites` und `ExportLogs`.
- Keine neuen Fachregeln eingefuehrt:
  - Gutschriften-Reiter zeigt technische Kandidaten.
  - Datenqualitaet zeigt technische Pruefpunkte.
  - Produktsparten-/Produktfamilienlogik bleibt bis Kendra-Mapping offen.
- Test ergaenzt: `AnalyzeFinanceSummaryAsync_Builds_Dashboard_Tab_Data`.
- Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `79/79` Tests gruen.

## Nachtrag 2026-05-27 Produktsparten-Mapping

Dokumentiert:

- Neue Detaildoku `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md`.
- Neue RAG-Kurzdatei `docs/rag/PRODUCT_MAPPING.md`.
- Router-Eintrag fuer Themen `Group Sales Report`, `Produkthierarchie`, `Produktfamilie`, `Produktsparte`.
- Fachliche Annahme: Materialnummern aus Group Sales Report werden gegen TR-AG-Artikelstamm aufgeloest; nicht gefundene Artikel laufen unter `Sonstige/ohne Zuordnung`.
- Offene Sitzungspunkte: Quelle des Artikelstamms, Bedeutung von `Z.Prodh`, Mapping-Tabelle von Kendra, Range-/Prefix-Regeln, Historisierung.

## Volltext Bei Bedarf

Die kanonische Detailhistorie liegt hier:

```text
docs/raw_md_archive/HISTORY_CANONICAL.md.raw
```

Die frueheren Original-Volltexte liegen als Wiederherstellungs-Backup hier:

```text
docs/raw_md_archive/original_history_raws.zip
```

Nur laden, wenn genaue Chronologie, alte Zwischenstaende, Commit-Historie oder Audit-Spuren benoetigt werden.
