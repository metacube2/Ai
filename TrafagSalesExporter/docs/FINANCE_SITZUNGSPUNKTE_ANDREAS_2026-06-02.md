# Finance Sitzungspunkte Andreas

Stand: 2026-06-02

Zweck: Kompakte Zusammenfassung der letzten Finance-/Andreas-Punkte und was davon bereits umgesetzt bzw. noch offen ist.

## Kurzfazit

- Das Finance Dashboard bleibt technisch produktiv nutzbar.
- Fuehrende Sicht fuer Soll/Ist bleibt `Finance Summary`.
- `Management Analyse` bleibt Diagnose: Laender, IC/2nd-party, Rohdaten, Abweichungen, Spartenanalyse.
- IC wird nicht automatisch aus dem Standard-Ist entfernt.
- Budgetkurse 2026 sind im Programm und auf dem Server eingespielt.
- Spanien-Sage-Serverexport kann jetzt Full oder Range exportieren.

## Letzte Sitzungspunkte

| Thema | Punkt von Andreas / Finance | Stand |
| --- | --- | --- |
| Intercompany | Pro Land klaeren, ob IC bereits in Quelle/Sollwert herausgerechnet ist. | Teilweise geklaert: FR, IN, US passen ohne IC-Abgrenzung; IT braucht Sonderabgrenzung; DE, UK, CH, AT bleiben offen. |
| Spanien Sollwert | ES hat keine echte Ist-Abweichung; alter Sollwert war falsch. | Fachlich dokumentiert: ES-Ist und korrigierter Sollwert `3'082'320.18 EUR`. |
| Spanien Export | Nicht jedes Mal Volljahr exportieren, wenn bereits Basisexport vorhanden ist. | Sage-Server-Script unterstuetzt jetzt `-ExportMode Full` und `-ExportMode Range`. |
| Wechselkurse | Offizieller Kurstyp und Kursdatum klaeren. | Budgetkurse 2026 wurden als Budgetkurse eingetragen. Kursdatum bleibt in Settings konfigurierbar. |
| Spartenanalyse | >90% nicht zugeordnet ist fachlich unplausibel. | Aktuelle Daten zeigen: Spartenfelder sind fast nur bei CH und teilweise AT gefuellt. Viele Laender haben keine Produktspartenfelder in `CentralSalesRecords`; deshalb ist es ein Mapping-/Referenzproblem, nicht fachlich "nur 10% Umsatz mit Sparte". |
| DE | Welche Kundenlaender / Filter gehoeren offiziell zum deutschen Ist? | Offen. Nicht einfach IC abziehen; Hauptfrage sind Kundenlaender/Filter. |
| UK | Sage-Differenz klaeren. | Offen: Exportvollstaendigkeit, Discounts, Freight/Charges und 2nd-party/IC pruefen. |
| IT | Neue IT-Abgrenzung pruefen. | IT ist Sonderfall: `Trafag Italia` aus externem IT-Ist ausschliessen; doppelte Einzelpositionen mit leerem Supplier country nur einmal zaehlen. |
| CH / AT | `FKDAT` als Perioden-/Buchungsdatum bestaetigen. | Offen. |
| Kosten | Entscheiden, ob Kosten/Marge Teil des Dashboards werden. | Offen; weiterhin nicht in Umsatzfreigabe mischen. |

## IC-Stand je Land

| Land | Aussage | Filter / Abgrenzung |
| --- | --- | --- |
| FR | Ohne IC-Abgrenzung stimmt der Soll/Ist-Vergleich. | IC-Filter ignorieren; B1 Positions-Netto verwenden. |
| IN | Ohne IC-Abgrenzung stimmt der Soll/Ist-Vergleich. | IC-Filter ignorieren; INR-Hauswaehrung verwenden. |
| US | Ohne IC-Abgrenzung stimmt der Soll/Ist-Vergleich. | IC-Filter ignorieren; B1 Positions-Netto verwenden. |
| IT | Ohne Abgrenzung stimmt es nicht sauber. | IT-Sonderabgrenzung anwenden: `Trafag Italia` aus externem Ist ausschliessen; Duplikate mit leerem `Supplier country` nur einmal zaehlen. |
| ES | IC ist aktuell nicht der relevante Punkt. | Sage `ImporteNeto`; Credit Notes/REC negativ; korrigierter Sollwert `3'082'320.18 EUR`. |
| DE | Nicht final beurteilbar. | Kundenlaender/Kundenfilter offen; nicht einfach IC abziehen. |
| UK | Nicht final beurteilbar. | Sage-Export, Discounts/Freight/Charges und 2nd-party/IC pruefen. |
| CH | Nicht beurteilbar. | Kein Sollwert im Seed. |
| AT | Abweichung vorhanden, Ursache unklar. | IC nicht automatisch abziehen; Filter/Datum weiter pruefen. |

## Spanien Sage Server Export

Relevant ist das Paket:

```text
SageSpainFinalExportPackage.zip
```

Wichtige Datei im Paket:

```text
Export-SageSpainSalesCsv.ps1
```

Full Export:

```powershell
.\Export-SageSpainSalesCsv.ps1 -ExportMode Full -Year 2025
```

Range Export:

```powershell
.\Export-SageSpainSalesCsv.ps1 -ExportMode Range -FromDate "2025-05-01" -ToDate "2025-06-01"
```

Range nach Registrierungsdatum:

```powershell
.\Export-SageSpainSalesCsv.ps1 -ExportMode Range -DateFilter LineRegistrationDate -FromDate "2025-05-01" -ToDate "2025-06-01"
```

Hinweis:

- `ToDate` ist exklusiv.
- Wenn der alte Vollbestand bereits vorhanden ist, braucht Spanien fuer Folgeupdates nur noch Range-Dateien fuer den neuen oder korrigierten Zeitraum.
- Das Sage-Script liest nur Daten aus SQL Server und aendert nichts in Sage.
- Unser Importprozess wird danach separat entschieden; der Serverexport ist jetzt nur die Dateierzeugung.

## Spartenanalyse / Produktmapping

### Warum nur rund 10% Abdeckung?

Die Spartenanalyse zaehlt Umsatz nur als sauber zugeordnet, wenn eine Materialnummer gegen die zentrale TR-AG-/SAP-Referenz gematcht wird und dort eine gueltige Produktsparte vorhanden ist.

Aktueller Befund aus der lokalen Datenbank:

| Kennzahl | Wert |
| --- | ---: |
| `CentralSalesRecords` total | `75'089` |
| Zeilen 2025 nach Invoice/Extraction | `54'682` |
| Zeilen mit Produktsparte-Code | `36'847` |
| Zeilen mit gueltigem Assigned-Flag | `27'047` |
| Zeilen mit gueltiger Produktsparte | `27'047` |
| Zeilen `UNASS` | `9'800` |
| Zeilen ohne Materialnummer | `59` |

Laenderbefund:

| Land | Zeilen | Gueltig zugeordnet | `UNASS` | Keine Produktfelder |
| --- | ---: | ---: | ---: | ---: |
| CH | `38'838` | `26'337` | `9'187` | `3'314` |
| AT | `1'454` | `710` | `613` | `131` |
| Italien | `16'850` | `0` | `0` | `16'850` |
| Indien | `5'515` | `0` | `0` | `5'515` |
| Deutschland | `4'548` | `0` | `0` | `4'548` |
| Spanien | `4'341` | `0` | `0` | `4'341` |
| Frankreich | `2'285` | `0` | `0` | `2'285` |
| USA | `1'256` | `0` | `0` | `1'256` |
| England | `2` | `0` | `0` | `2` |

Interpretation:

- Die niedrige Abdeckung ist nicht als fachliche Umsatzverteilung zu lesen.
- Sie zeigt, dass das zentrale Produktmapping aktuell nur fuer CH und teilweise AT in den Daten ankommt.
- Bei IT, IN, DE, ES, FR, US und UK sind die Produktspartenfelder aktuell leer.
- Wahrscheinliche Ursachen: lokale Materialnummern passen nicht direkt zu TR-AG `MATNR`, fehlende Referenzdaten in `ProductDivisionRefSet`, Join trifft nicht oder Produktfelder werden bei diesen Laendern nicht befuellt.

### Prozessfluss vom Holen bis Anwenden

```text
1. SAP/TR-AG stellt zentrale Produktreferenz bereit
   EntitySet: ProductDivisionRefSet
   Felder: Matnr, Paph1, Paph1Text, Wwpfa, WwpfaText, Wwpsp, WwpspText, IsAssigned

2. Webprogramm holt beim SAP-Standort zwei Quellen
   Z = FinanzdataSchweizOeSet
   P = ProductDivisionRefSet

3. Join im Web-Exporter
   Z.Matnr = P.Matnr
   JoinType = Left

4. Mapping ins interne SalesRecord-Modell
   Z.Matnr      -> Material
   Z.Prodh      -> ProductGroup
   P.Paph1      -> ProductHierarchyCode
   P.Paph1Text  -> ProductHierarchyText
   P.Wwpfa      -> ProductFamilyCode
   P.WwpfaText  -> ProductFamilyText
   P.Wwpsp      -> ProductDivisionCode
   P.WwpspText  -> ProductDivisionText
   P.IsAssigned -> ProductMappingAssigned

5. Speicherung in CentralSalesRecords
   Die Produktfelder werden mit jeder Umsatzzeile gespeichert.

6. Management Analyse > Spartenanalyse
   Statuslogik:
   - Material leer -> Material fehlt
   - kein Treffer in zentraler Referenz -> Nicht im TR-AG-Stamm
   - Treffer, aber IsAssigned nicht wahr oder Division = UNASS -> Nicht zugeordnet
   - Treffer, IsAssigned wahr und Division nicht UNASS -> Zugeordnet

7. Sparten-Finanzanalyse
   Nur Status Zugeordnet geht in Umsatz nach Produktsparte / Produktfamilie / PAPH1.
```

### SAP-Objektnamen und Code-Stand

| Objekt | Name |
| --- | --- |
| SAP Gateway Service | `ZPOWERBI_EINKAUF_SRV` |
| Sales EntitySet | `FinanzdataSchweizOeSet` |
| Sales Gateway-Methode | `FINANZDATASCHWEI_GET_ENTITYSET` |
| Produktsparten EntitySet | `ProductDivisionRefSet` |
| Produktsparten Gateway-Methode | `PRODUCTDIVISIONR_GET_ENTITYSET` |
| Provider-Klasse | `ZCL_PRODSPARTE_PROVIDER` |
| Mapping-Aufbau-Report | `Z_PRODSPARTE_MAP_BUILD` |
| Test-/ALV-Report | `Z_PRODSPARTE_REPORT` |
| Mapping-Tabelle | `ZPRODSPARTE_MAP` |
| DDIC-Struktur | `ZSTR_PRODSPARTE_OUT` |

Vorhandener Code im Repo:

- `docs/abap/ZCL_PRODSPARTE_PROVIDER.abap`
- `docs/abap/Z_PRODSPARTE_MAP_BUILD.abap`
- `docs/abap/Z_PRODSPARTE_REPORT.abap`

Nicht als vollstaendiger Code im Repo vorhanden:

- `FINANZDATASCHWEI_GET_ENTITYSET`
- `PRODUCTDIVISIONR_GET_ENTITYSET`

Diese beiden Gateway-Methoden sind in den MDs beschrieben, aber der komplette DPC_EXT-Methodencode muss aus SAP geholt oder neu formuliert werden.

## Budgetkurse

### Budget 2026

Budget 2026 wurde eingetragen und deployed:

| Von | Nach | Rate |
| --- | --- | ---: |
| CHF | CHF | 1 |
| USD | CHF | 0.80 |
| EUR | CHF | 0.94 |
| GBP | CHF | 1.09 |
| CNY | CHF | 0.11764706 |
| INR | CHF | 0.00909091 |
| CZK | CHF | 0.03846154 |
| PLN | CHF | 0.22 |
| JPY | CHF | 0.00571429 |

Technische Ablage:

```text
CurrencyExchangeRates
Notes = Budget 2026
ValidFrom = 2026-01-01
ValidTo = 2026-12-31
```

### Budget 2025

Andreas hat neue Budget-2025-Werte geliefert. Noch nicht final eingetragen, weil CNY noch fehlt:

| Waehrung | Rate nach CHF |
| --- | ---: |
| EUR | 0.937034700 |
| GBP | 1.093822350 |
| INR | 0.009532034 |
| PLN | 0.221032130 |
| RUB | 0.009958327 |
| CZK | 0.037963103 |
| JPY | 0.005553195 |
| USD | 0.830651790 |
| CNY | offen |

## Deploy-Stand

Deploy wurde am 2026-06-02 ausgefuehrt:

- Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`
- `BiDashboard.dll` Zeitstempel nach Deploy: `2026-06-02 15:15:20`
- Server-DB enthaelt `9` Budget-2026-Kurse.
- Testlauf vor Deploy: `82/82` Tests gruen.

## Naechste klare To-dos

1. CNY Budget 2025 von Andreas nachliefern lassen.
2. Budget 2025 Seedwerte danach gesammelt aktualisieren und deployen.
3. DE: offizielle Kundenlaender/Kundenfilter bestaetigen.
4. UK: Sage-Exportvollstaendigkeit und Discounts/Freight/Charges pruefen.
5. CH/AT: `FKDAT` als fachliches Periodendatum bestaetigen.
6. Spartenmapping weiter pruefen: Gateway-Methodencode `PRODUCTDIVISIONR_GET_ENTITYSET` aus SAP holen oder sauber neu formulieren; danach pruefen, warum Produktfelder nur bei CH/AT gefuellt sind.
7. Entscheiden, ob CHF-Sicht nur Reporting bleibt oder offizieller Vergleichswert wird.
