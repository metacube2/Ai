# Produktsparten-Mapping Group Sales Report

Stand: 2026-05-27

## Anlass

Fuer die Zuordnung der Artikel aus dem Group Sales Report zu Produktbereichen wurde abgestimmt, die Zuordnung von TR AG als fuehrende Referenz zu verwenden.

Ziel ist, Artikel bzw. Materialnummern aus dem Group Sales Report in einem ersten Schritt folgenden Elementen zuzuordnen:

- Produkthierarchie
- Produktfamilie
- Produktsparte

## Vorgeschlagene Fachlogik

1. Materialnummer aus dem Group Sales Report lesen.
2. Materialnummer gegen Artikelstammdaten der TR AG aufloesen.
3. Produkthierarchie direkt aus den Artikelstammdaten uebernehmen.
4. Produktfamilie und Produktsparte anschliessend ueber eine separate Mapping-Tabelle ableiten.
5. Artikel ohne Treffer in den TR-AG-Stammdaten automatisch unter `Sonstige/ohne Zuordnung` fuehren.

## Mapping-Tabelle

Nach aktuellem Verstaendnis definiert die separate Mapping-Tabelle die Zuordnung von Produktgruppen bzw. Produkthierarchie-Bereichen zu Produktfamilien und Produktsparten.

Moegliche technische Regeln, die fachlich zu bestaetigen sind:

- exakte Codes
- Prefix-Regeln
- Von/Bis-Ranges
- Prioritaet bei ueberlappenden Bereichen
- Gueltigkeit nach Datum oder Version

Kendra kann laut Aufgabenbeschreibung weitere Details zur bestehenden Logik und zur konkreten Umsetzung geben.

## Aktueller technischer Stand Im Projekt

Im aktuellen Datenmodell existieren bereits:

- `Material`
- `ProductGroup`

Noch nicht explizit vorhanden sind:

- `Produkthierarchie`
- `Produktfamilie`
- `Produktsparte`

Relevante aktuelle Modelle:

- `Models/SalesRecord.cs`
- `Models/CentralSalesRecord.cs`

Hinweis aus der Code-Sichtung:

- SAP-Seed-Mapping nutzt aktuell `Z.Matnr` fuer `Material`.
- SAP-Seed-Mapping nutzt aktuell `Z.Prodh` fuer `ProductGroup`.
- Ob `Z.Prodh` fachlich bereits der gewuenschten Produkthierarchie entspricht, muss bestaetigt werden.

## Empfohlene Umsetzung

Die Produktspartenlogik sollte als eigene sichtbare Mapping-Schicht umgesetzt werden, nicht als versteckte Sonderlogik in Finance oder Management Cockpit.

Sinnvolle technische Bausteine:

- TR-AG-Artikelstamm-Quelle fuer Materialnummer -> Produkthierarchie.
- Mapping-Tabelle fuer Produkthierarchie/ProductGroup/Range -> Produktfamilie und Produktsparte.
- Fallback-Kategorie `Sonstige/ohne Zuordnung`.
- Export-/Excel-Spalten fuer die drei neuen Klassifikationen.
- Pruefansicht fuer nicht zugeordnete Materialnummern.

## Nachtrag 2026-05-28 SAP-/ABAP-Zielbild

Nach weiterer Analyse mit Andreas-/SAP-Kontext ist das fuehrende Zielbild:

- SAP TR AG bleibt Quelle der Wahrheit fuer die Artikelzuordnung.
- Die Dashboard-App baut die KEDR-/KE30-Ableitungslogik nicht direkt in C# nach.
- Stattdessen wird eine flache Referenztabelle aus SAP bereitgestellt:
  - `MATNR`
  - `MAKTX`
  - `PAPH1`
  - `PAPH1_TEXT`
  - `WWPFA`
  - `WWPFA_TEXT`
  - `WWPSP`
  - `WWPSP_TEXT`
  - `IS_ASSIGNED`
- Das Dashboard mappt spaeter Umsatzzeilen ueber `Material`/`MATNR` gegen diese Referenz.
- Falls die Materialnummer nicht in der Referenz enthalten ist oder keine eindeutige Ableitung existiert, wird die Zeile unter `Nicht zugeordnet` gefuehrt.

SAP-Felder / Annahmen:

- Materialnummer: `MATNR`
- Produkthierarchie aus Vertriebssicht: `MVKE-PRODH`
- CO-PA Merkmal fuer erste Produkthierarchieebene: `PAPH1`
- Produktfamilie: `WWPFA`
- Produktsparte: `WWPSP`
- Reale Ableitung kommt aus CO-PA/KEDR und ist in `CE11000` sichtbar.

ABAP-Artefakte wurden als Arbeitsstand im Repo abgelegt:

| Datei | Zweck |
| --- | --- |
| `docs/abap/ZCL_PRODSPARTE_PROVIDER.abap` | Globale Provider-Klasse fuer ALV und spaeter OData |
| `docs/abap/Z_PRODSPARTE_REPORT.abap` | ALV-Testreport, ruft Provider-Klasse |
| `docs/abap/Z_PRODSPARTE_MAP_BUILD.abap` | Baut `ZPRODSPARTE_MAP` aus eindeutigen CO-PA-Kombinationen |
| `docs/abap/README_PRODSPARTE.md` | Hinweise zu DDIC-Objekten und Pruefpunkten |

Vorgeschlagene SAP-Architektur:

1. `Z_PRODSPARTE_MAP_BUILD` liest eindeutige Kombinationen `PAPH1 -> WWPFA -> WWPSP` aus `CE11000`.
2. Mehrdeutige `PAPH1` werden protokolliert und nicht in die Mapping-Tabelle geschrieben.
3. Eindeutige Zuordnungen werden in `ZPRODSPARTE_MAP` geschrieben.
4. `ZCL_PRODSPARTE_PROVIDER` liest verkaufsrelevante Materialien aus `MVKE`, Texte aus `MAKT`/`T179T`/`T25A0`/`T25A1` und verbindet sie mit `ZPRODSPARTE_MAP`.
5. `Z_PRODSPARTE_REPORT` dient als ALV-Test.
6. Ein spaeterer SAP-Gateway/OData-Service ruft dieselbe Provider-Klasse auf.

Bewusst korrigierte Punkte im ABAP-Arbeitsstand:

- Provider-Logik ist global auslagerbar, nicht nur lokale Reportklasse.
- `MAKT` wird per `LEFT OUTER JOIN` gelesen, damit Materialien ohne Text nicht verschwinden.
- `VTWEG` ist optionaler Selektionsparameter.
- Bei mehreren Vertriebswegen gewinnt aktuell bewusst der kleinste `VTWEG`; dies ist noch fachlich zu bestaetigen.
- Fallback setzt technischen Code `UNASS`, Text `Nicht zugeordnet` und `IS_ASSIGNED = abap_false`.
- Mapping-Build schreibt die Tabelle nicht leer, falls keine eindeutigen Saetze aufgebaut wurden.

Noch zu pruefen:

- Ist `PAPH1 = MVKE-PRODH(5)` im Trafag-System exakt korrekt?
- Sind `T25A0` und `T25A1` die richtigen Text-/Customizingtabellen fuer Produktfamilie und Produktsparte?
- Ist `CE11000` der richtige CO-PA-Einzelposten fuer den relevanten Ergebnisbereich?
- Ist der Fallback-Code `UNASS` in Feld `WWPSP` zulaessig/lang genug?
- Soll `VTWEG` zwingend vorgegeben werden, statt den kleinsten Vertriebsweg zu verwenden?
- Welche VKORG ist fuer TR AG im produktiven Lauf massgebend?

## Offene Fragen Fuer Andreas / Kendra

| Frage | Warum wichtig |
| --- | --- |
| Woher kommt der fuehrende TR-AG-Artikelstamm technisch? | Datenquelle und Aktualisierung festlegen |
| Welches Feld ist die eindeutige Materialnummer? | Normalisierung, fuehrende Nullen, Varianten |
| Ist `Z.Prodh` die gewuenschte Produkthierarchie? | Bestehendes Mapping evtl. wiederverwendbar |
| Wie sieht die bestehende Mapping-Tabelle aus? | Datenmodell und Importlogik festlegen |
| Werden Ranges, Prefixe oder exakte Werte verwendet? | Matching-Regeln eindeutig implementieren |
| Was gilt bei ueberlappenden Ranges? | Prioritaet / deterministisches Ergebnis |
| Soll die Zuordnung historisiert werden? | Reproduzierbarkeit alter Reports |
| Soll `Sonstige/ohne Zuordnung` nur im Report erscheinen oder auch in Daten persistiert werden? | Datenmodell und Auditierbarkeit |

## Abgrenzung

Dieser Task ist keine Finance-Soll/Ist-Regel. Die Klassifikation kann spaeter Finance- und Management-Auswertungen ergaenzen, sollte aber fachlich getrennt von Net-Sales-Abgrenzungen bleiben.

## Nachtrag 2026-05-29 Umsetzung SAP Gateway Und Web

SAP/DDIC:

- Struktur `ZSTR_PRODSPARTE_OUT` wurde fuer den Gateway-Entity-Type verwendet.
- `PAPH1`, `WWPFA` und `WWPSP` duerfen in SE11 nicht vorausgesetzt als globale Datenelemente typisiert werden.
- Pragmatische Typisierung ist moeglich:
  - `PAPH1`: `CHAR 5`
  - `WWPFA`: Laenge wie `CE11000-WWPFA`
  - `WWPSP`: Laenge wie `CE11000-WWPSP`
  - Textfelder: z.B. `CHAR 50`
- Aktivierungsfehler bei `ZSTR_PRODSPARTE_OUT` waren durch nicht vorhandene/aktive Datenelemente fuer `PAPH1`, `WWPFA`, `WWPSP` verursacht.

SAP/ABAP:

- `ZCL_PRODSPARTE_PROVIDER` wurde als globale Klasse in SE24/quelltextbasiert angelegt.
- `Z_PRODSPARTE_REPORT` und `Z_PRODSPARTE_MAP_BUILD` wurden als Reports angelegt.
- `Z_PRODSPARTE_MAP_BUILD` hat `ZPRODSPARTE_MAP` befuellt.
- Beispielhafte Mapping-Luecken aus dem ALV-Test:
  - `0509 Multistat` nicht zugeordnet
  - `0540 Multistat` nicht zugeordnet
  - `0519 Multistat` zugeordnet zu `0007` / `0001`
- Diese Luecken werden spaeter fachlich geprueft; technisch funktioniert der Provider.

SAP Gateway:

- Bestehender Service wird weiterverwendet:
  - `ZPOWERBI_EINKAUF_SRV`
  - Service Root: `http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/`
- Es wurde ein zusaetzlicher Entity Type aus `ZSTR_PRODSPARTE_OUT` erstellt:
  - Entity Type: `ProductDivisionRef`
  - Entity Set: `ProductDivisionRefSet`
- Der bestehende Sales-EntitySet bleibt separat:
  - `FinanzdataSchweizOeSet` bzw. generierte Methode `FINANZDATASCHWEI_GET_ENTITYSET`
  - Diese Methode muss weiter aus `ZSCHWEIZ` lesen und darf nicht durch Produktsparten-Code ersetzt werden.
- Produktsparten-Code gehoert in die separat generierte/redefinierte Methode:
  - `PRODUCTDIVISIONR_GET_ENTITYSET`
- Test-URL:
  - `http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/ProductDivisionRefSet`
- OData-Feldnamen aus dem Gateway sind CamelCase:
  - `Matnr`
  - `Maktx`
  - `Paph1`
  - `Paph1Text`
  - `Wwpfa`
  - `WwpfaText`
  - `Wwpsp`
  - `WwpspText`
  - `IsAssigned`
- Erfolgreicher Testdatensatz aus Gateway:
  - `Matnr = VCP1000`
  - `Maktx = VC TRANSMITTER`
  - `Paph1 = 9999`
  - `Paph1Text = Zubehoer`
  - `Wwpsp = UNASS`
  - `WwpspText = Nicht zugeordnet`
  - `IsAssigned = false`

Wichtige Gateway-Korrektur:

- Fehler `/IWFND/MED/170` trat auf, weil Service und EntitySet ohne Slash zusammengesetzt wurden.
- Falsch:
  - `.../ZPOWERBI_EINKAUF_SRVProductDivisionRSet`
- Richtig:
  - `.../ZPOWERBI_EINKAUF_SRV/ProductDivisionRefSet`

Webprogramm / Datenmodell:

- `SalesRecord` und `CentralSalesRecord` wurden um folgende Felder erweitert:
  - `ProductHierarchyCode`
  - `ProductHierarchyText`
  - `ProductFamilyCode`
  - `ProductFamilyText`
  - `ProductDivisionCode`
  - `ProductDivisionText`
  - `ProductMappingAssigned`
- SQLite-Schema wurde erweitert:
  - Neue Installationen erhalten die Felder in `CentralSalesRecords`.
  - Bestehende Datenbanken erhalten die Felder per `DatabaseSchemaMaintenanceService`.
- `CentralSalesRecordService` schreibt und liest die neuen Felder.
- `ConfigTransferService` erhaelt die Produktfelder beim Preserve bestehender `CentralSalesRecords`.
- Excel-Export fuehrt die neuen Produktfelder im Blatt `Sales` direkt nach `Product Group`.
- Finance-Spalten im Export verschieben sich dadurch nach hinten; Tests wurden angepasst.
- Manual-Excel-Import und Auto-Match kennen die neuen Feldnamen ebenfalls.

Aktive lokale Web-Konfiguration:

- Standort:
  - `Schweiz/Oesterreich`
  - `TSC = ZSCHWEIZ`
  - `SourceSystem = SAP`
- SAP Service URL:
  - `http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/`
- SAP-Quellen:
  - Alias `Z`: bestehender Sales-EntitySet
  - Alias `P`: `ProductDivisionRefSet`
- Join:
  - `Z.Matnr = P.Matnr`
- Feldmappings:
  - `ProductHierarchyCode <- P.Paph1`
  - `ProductHierarchyText <- P.Paph1Text`
  - `ProductFamilyCode <- P.Wwpfa`
  - `ProductFamilyText <- P.WwpfaText`
  - `ProductDivisionCode <- P.Wwpsp`
  - `ProductDivisionText <- P.WwpspText`
  - `ProductMappingAssigned <- P.IsAssigned`

Lokaler Neustart / Validierung:

- Lokaler Webprozess wurde neu gestartet.
- App antwortet lokal mit HTTP 200 auf `http://localhost:55416/`.
- Neue Spalten sind in `CentralSalesRecords` vorhanden.
- Validierung:
  - `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-productmapping`
  - Ergebnis: `79/79` Tests gruen.

Naechster fachlicher/technischer Schritt:

- Standort `ZSCHWEIZ` im Export Dashboard einmal neu laufen lassen.
- Danach pruefen:
  - Sind Produktfelder in `CentralSalesRecords` gefuellt?
  - Stimmen Join-Treffer fuer bekannte Materialien?
  - Wie viele Zeilen bleiben `UNASS` / `Nicht zugeordnet`?
- SAP-seitig muss `FINANZDATASCHWEI_GET_ENTITYSET` auf den alten `ZSCHWEIZ`-Select-Code zurueckgesetzt sein, falls er versehentlich mit Produktsparten-Code ueberschrieben wurde.

## Nachtrag 2026-05-29 Zentrale Spartenzuordnung

Fachliches Ziel aus Finance-Input:

- Die Produktsparten-/Produktfamilienzuordnung der anderen Laender-ERPs ist nicht fuehrend.
- Fuehrend ist die Trafag-AG-/SAP-Referenz aus dem eigenen SAP-System.
- Jede Umsatzzeile aus `CentralSalesRecords` wird ueber ihre Materialnummer gegen die TR-AG-Referenz geprueft.
- Wenn die Materialnummer im TR-AG-Stamm vorhanden ist, wird die dortige Produktzuordnung angezeigt.
- Wenn die Materialnummer nicht im TR-AG-Stamm vorhanden ist, gilt der Status `Nicht im TR-AG-Stamm`.
- Wenn die Materialnummer im TR-AG-Stamm vorhanden ist, aber dort `UNASS`/nicht zugeordnet ist, gilt der Status `Nicht zugeordnet`.

Umsetzung im Web:

- Neuer Reiter in `Management Analyse`:
  - `Zentrale Spartenzuordnung`
- Der Reiter arbeitet auf dem bestehenden Finance-Filter:
  - Jahr
  - Land
  - Waehrung
- Die Referenz wird aus zentral gespeicherten Zeilen mit Produktfeldern gebildet.
- Der Abgleich erfolgt ueber normalisierte Materialnummer:
  - Land-ERP-Material links
  - TR-AG-Referenz-Material plus Produktzuordnung rechts
- Angezeigte Statuswerte:
  - `Zugeordnet`
  - `Nicht zugeordnet`
  - `Nicht im TR-AG-Stamm`
  - `Material fehlt`

UI-Inhalte:

- Kennzahlen:
  - Materialien
  - Zugeordnet
  - Nicht zugeordnet
  - Nicht im Stamm
  - Material fehlt
  - TR-AG Referenz
- Laenderuebersicht:
  - Land
  - TSC
  - Materialanzahl
  - Zugeordnet
  - Nicht zugeordnet
  - Nicht im Stamm
  - Material fehlt
  - Trefferquote
- Detailtabelle:
  - Status
  - Land
  - TSC
  - Land-Material
  - Land-Text
  - TR-AG-MATNR
  - PAPH1
  - Produktfamilie
  - Produktsparte
  - Zeilen
  - Finance-Wert

Technische Dateien:

- `Models/ManagementCockpitModels.cs`
  - neue Modelle fuer Produktzuordnungs-Summary, Laenderzeilen und Detailzeilen.
- `Services/ManagementCockpitService.cs`
  - baut die TR-AG-Referenz aus Produktfeldern.
  - prueft gefilterte Finance-Zeilen ueber `Material`.
  - erzeugt Summary, Laenderabdeckung und Detailzeilen.
- `Components/Pages/ManagementCockpit.razor`
  - neuer Reiter `Zentrale Spartenzuordnung`.
- `TrafagSalesExporter.Tests/ManagementCockpitServiceTests.cs`
  - Test fuer Treffer, fehlende Referenz und `UNASS`.

Validierung:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-central-product-assignment`
- Ergebnis: `80/80` Tests gruen.

Wichtig:

- Die Sicht ist zunaechst eine Pruef-/Analyseansicht.
- Sie veraendert noch keine bestehenden Umsatzzeilen der anderen Laender.
- Persistente Anreicherung aller `CentralSalesRecords` kann spaeter folgen, wenn die Treffer-/Fehlerquote fachlich akzeptiert ist.

## Nachtrag 2026-05-29 Sparten-Finanzanalyse

Fachliches Ziel:

- Nach der Abgrenzung `Zugeordnet`, `Nicht zugeordnet`, `Nicht im TR-AG-Stamm` und `Material fehlt` werden die gleichen Statuswerte finanztechnisch bewertet.
- Die Sicht beantwortet nicht nur, wie viele Materialien zugeordnet sind, sondern wie viel Umsatz sauber einer TR-AG-Produktsparte zugeordnet ist.

Umsetzung im Web:

- Neuer Reiter in `Management Analyse`:
  - `Sparten-Finanzanalyse`
- Der Reiter nutzt dieselben Finance-Filter wie die bestehende Analyse:
  - Finance-Jahr
  - Land
  - Waehrung
- Grundlage sind die bereits gebildeten Materialpruefzeilen aus `Zentrale Spartenzuordnung`.

Kennzahlen:

- Gesamtumsatz
- Zugeordneter Umsatz
- Nicht zugeordneter Umsatz
- Umsatz nicht im TR-AG-Stamm
- Prozentuale Abdeckung nach Umsatz

Tabellen:

- Umsatz nach Produktsparte:
  - Produktsparte
  - Produktfamilie
  - PAPH1
  - Umsatz
  - Anteil
  - Materialien
  - Zeilen
  - Laender
- Umsatzabdeckung nach Land:
  - Land
  - TSC
  - Gesamtumsatz
  - Zugeordneter Umsatz
  - Nicht zugeordneter Umsatz
  - Nicht im Stamm
  - Material fehlt
  - Abdeckungsquote

Technisch:

- Neue Modelle:
  - `ManagementProductFinanceSummary`
  - `ManagementProductDivisionFinanceRow`
  - `ManagementProductFinanceCountryRow`
- Neue Berechnungen in `ManagementCockpitService`:
  - Umsatzabdeckung aus `ProductAssignmentRows`
  - Umsatz je Produktsparte nur fuer Status `Zugeordnet`
  - Laenderabdeckung nach Umsatz und Status
- Neuer UI-Reiter in `Components/Pages/ManagementCockpit.razor`.
- Test erweitert:
  - `AnalyzeFinanceSummaryAsync_Builds_Central_Product_Assignment_Tab_Data` prueft jetzt auch Umsatzabdeckung und Spartentabelle.

Validierung:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-division-finance`
- Ergebnis: `80/80` Tests gruen.

Deploy:

- Deployed auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- `BiDashboard.dll` Zeitstempel nach Deploy: `29.05.2026 10:42`.
- Server-DB nach Deploy geprueft:
  - `ProductRows = 36'847`
  - `TR-AG Referenzmaterialien = 6'805`
  - `P ProductDivisionRefSet` aktiv.
