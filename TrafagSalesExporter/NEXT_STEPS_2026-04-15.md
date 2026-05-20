# Next Steps

Stand: 2026-05-20

## Nachtrag 2026-05-20 Dokumentation bereinigt

Erledigt:

- Markdown-Bestand eingeordnet in `docs/MD_DOKUMENTENSTATUS_2026-05-20.md`.
- HR-Anwenderdoku als Word-Datei optisch ueberarbeitet:
  - `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
  - inklusive 10 neuer HR-Cockpit-Punkte, Tabellen, Hinweisboxen und Vorschaugrafik.
- Finance-Anwenderdoku als Word-Datei optisch ueberarbeitet:
  - `docs/FINANCE_COCKPIT_ANLEITUNG_FINANZ_2026-05-20.docx`
  - inklusive Finance Summary Workflow, Filterregeln und Pruefpunkten.
- Neue neutrale Vorschaubilder fuer die Word-Dokus:
  - `docs/hr_kpi_cockpit_preview.png`
  - `docs/finance_cockpit_preview.png`

Bewusst nicht geloescht:

- Alte Markdown-Dateien und alte Eintraege bleiben erhalten, wenn sie Pruefwerte, Zwischenentscheide, Mailkontext oder Audit-Spuren enthalten.
- Nicht mehr fuehrende Dateien sind in `docs/MD_DOKUMENTENSTATUS_2026-05-20.md` als historisch markiert.

## Nachtrag 2026-05-20 Workflow-Fixes nach Review

Umgesetzt:

- Dashboard warnt vor aktiven manuellen Standorten ohne Datei.
- Nach Einzelstandortexport wird sichtbar, dass die zentrale Excel neu erzeugt werden muss.
- Dashboard erkennt eine veraltete zentrale Excel nach neuem Standortexport.
- Neuer Menuepunkt `Manuelle Importe` fuer Keyuser.
- `Manuelle Importe` hat jetzt die Reiter `Importdateien` und `Anleitung`.
- Der Reiter `Anleitung` zeigt den Upload-/Export-/Zentraldatei-/Finance-Pruefprozess grafisch.
- Zentrale Excel hat ein Blatt `Finance Summary`.
- `Management Analyse` ist als Rohdaten-/Plausibilitaetssicht markiert.
- `Soll/Ist Vergleich` ist als verbindliche Finance-Sicht markiert.
- Export-Live-Status ist nicht mehr pauschal `HANA Abfrage...`.

Weiterhin offen:

- DE Alphaplan-Fachabgrenzung: Kundenlaender/Filter muessen von Munir/Finance bestaetigt werden.

## Nachtrag 2026-05-20 Keyuser Prozess-SVG

Erstellt:

```text
docs/KEYUSER_PROZESSDOKU_2026-05-20.svg
```

Zielgruppe:

- Finance Keyuser / Poweruser.

Inhalt:

- Vorbereitung in Settings und Standorte.
- Manual-Excel-Dateien fuer UK/ES/DE.
- Einzelstandortexport, Export aller Standorte und zentrale Excel.
- Finance-Filter im Endexcel.
- Soll/Ist Vergleich, Management Analyse, Logs.
- Fehlerbehandlung und fachliche Freigabe.

## Nachtrag 2026-05-20 Technische Architektur-SVG

Erstellt:

```text
docs/SYSTEMARCHITEKTUR_TECHNISCH_2026-05-20.svg
```

Zielgruppe:

- Systemarchitekt / Serveradmin / technischer Projektkontext.

Abgrenzung:

- Nur produktive Applikation.
- Keine Testapp, Probe-Tools oder temporaeren Analyseprogramme.

Status:

- Keyuser-Prozessdoku wurde als separate SVG erstellt.

## Nachtrag 2026-05-20 DE Alphaplan-Excel provisorisch eingebaut

Erledigt:

- Deutschland wird als manueller Excel-Standort vorbereitet:
  - `TSC = TRDE`
  - `Land = Deutschland`
  - `SourceSystem = MANUAL_EXCEL`
  - neuer Standort ist standardmaessig inaktiv, damit Export-All nicht ohne Datei scheitert
- Alphaplan-Mapping wird automatisch geseedet:
  - `NettoPreisGesamtX` -> `SalesPriceValue`
  - `Belegnummer` -> `InvoiceNumber`
  - `Position` -> `PositionOnInvoice`
  - `ArtikelNummer` -> `Material`
  - `ArtikelBezeichnung` -> `Name`
  - `Warengruppen-Bezeichnung` -> `ProductGroup`
  - `Anz. VE` -> `Quantity`
  - `Name/Land Lieferant`, `Name/Land Kunde`, `Branche`, `Versandbedingung`
  - `Belegdatum-Rechnung` -> `PostingDate` und `InvoiceDate`
  - `DocumentType = Alphaplan Excel`
- Datei erhalten:

```text
docs/2025_DataExport_DE.xlsx
```

Bedienung:

1. App starten.
2. `Standorte` oeffnen.
3. Deutschland / `TRDE` oeffnen.
4. Alphaplan-Excel hochladen oder Pfad setzen.
5. Standort aktivieren.
6. Standortexport fuer DE ausfuehren.
7. Danach zentrale Excel erzeugen; DE ist dann in `CentralSalesRecords` und im Endexcel enthalten.

Offen fachlich:

- Komplette Summe `NettoPreisGesamtX`: `4'154'690.05 EUR`.
- Nur `Land Kunde = Deutschland`: `3'455'276.64 EUR`.
- Sollwert DE: `3'635'923.00 EUR`.
- Finance/Munir muss bestaetigen, welche Kundenlaender oder Filter zum offiziellen DE-Ist gehoeren.

## Nachtrag 2026-05-20 IIS 500 aktueller Stand

Vollstaendige Doku:

```text
docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md
```

Was sicher bewiesen ist:

- `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/diag.txt` ist erreichbar.
- Browser zeigt dort:

```text
BiDashboard publish folder reached 2026-05-20T08:19:14.2667783+02:00
```

- Damit stimmt IIS-URL `/BiDashboard` und der Physical Path zum Publish-Ordner.
- Der verbleibende `500` ist kein falscher Pfad und kein HTTP/HTTPS-Verwechslungsproblem.

Was umgesetzt wurde:

- Publish weiterhin aus `TrafagSalesExporter`.
- Ausgabe weiterhin `BiDashboard.dll`, keine EXE.
- `web.config` auf `hostingModel="outofprocess"` umgestellt.
- `stdoutLogEnabled="true"` bleibt aktiv.
- `ASPNETCORE_DETAILEDERRORS=true` fuer Diagnose gesetzt.
- Neu publiziert auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.

Offen fuer Server-Spezialist:

- .NET 8 Hosting Bundle / AspNetCoreModuleV2 pruefen.
- App Pool pruefen:
  - `.NET CLR Version = No Managed Code`
  - `Managed Pipeline Mode = Integrated`
  - `Enable 32-bit Applications = False`
- Event Viewer lesen:
  - `IIS AspNetCore Module V2`
  - `.NET Runtime`
  - `Application Error`
- App-Pool-Identity mit `Modify` auf Publish-Ordner, `logs` und `trafag_exporter.db*` bestaetigen.

Wichtig:

- Der Server braucht kein installiertes Microsoft Excel.
- XLSX wird ueber ClosedXML/OpenXML gelesen.
- CSV-Umstellung ist fuer diesen 500-Fehler nicht noetig.

## Nachtrag 2026-05-20 IT Finance-Methode

Erledigt:

- IT-Methode gemaess Finance-Leiter umgesetzt.
- `CustomerName` enthaelt `Trafag Italia` wird fuer IT ausgeschlossen.
- Doppelte IT-Zeilen mit leerem `Supplier country` werden nur einmal gezaehlt.
- Regel greift im Finance-Vergleich/Testprogramm und in den Finance-Spalten der zentralen Excel.

Bewusster Entscheid:

- Die alte 2025-Kombination ist naeher am Soll, aber fachlich nicht zukunftssicher.
- Fuer 2026+ gilt die neue Methode, auch wenn sie 2025 in der aktuellen DB weiter vom Sollwert abweicht.

Naechster Check:

- Nach neuem IT-Export pruefen, ob die vollstaendige `Trafag Italia`-Summe aus den neuen Rohdaten sichtbar wird.
- Zentrale Excel fuer `Finance | Country Key = IT`, `Finance | Include = TRUE` filtern und gegen Finance-Vergleich kontrollieren.

## Nachtrag 2026-05-19 IIS Deployment / 500 Fehler

Vollstaendige Doku:

```text
docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md
```

Aktueller Stand:

- Publish erfolgt direkt aus `TrafagSalesExporter`.
- Publish-Ausgabe ist an das alte `BiDashboard` angepasst:
  - `BiDashboard.dll`
  - keine EXE
  - `web.config` startet `.\BiDashboard.dll`
  - Diagnose aktiv mit `stdoutLogEnabled=true`
- URL mit App-Pfad liefert laut Browser `500`:

```text
https://trch-webapp-bidashboard.trafagch.local/BiDashboard/
```

Wahrscheinlichstes offenes Thema:

- App-Pool/IIS hat auf dem Publish-Ordner nur Lesen/Ausfuehren.
- Die App schreibt beim Start in SQLite (`trafag_exporter.db`, `db-shm`, `db-wal`) und in `logs`.
- `icacls`-Versuch von lokal wurde vom Server mit `Zugriff verweigert` abgelehnt.

Naechster Schritt fuer Server-Spezialist:

- App-Pool-Identity ermitteln.
- `Modify` auf Publish-Ordner, `logs` und `trafag_exporter.db*` setzen.
- App-Pool neu starten.
- Danach URL neu testen und bei weiterem `500` stdout-Log/Event Viewer lesen.

## Nachtrag 2026-05-19 Finance-Cockpit-Login finalisieren

Aktueller Stand:

- Finance Cockpit hat einen separaten Login.
- HR-KPI-Login und Finance-Cockpit-Login sind technisch getrennte Services/Konfigurationen.
- Finance-Konfiguration liegt in `appsettings.json` unter `FinanceCockpitAccess`.
- Aktueller Benutzer: `finance`.
- Finance nutzt ein eigenes Passwort: `Trafag-Finance-Cockpit-2026!`.
- Globale AD-/Rollenpruefung ist fuer den Moment mit `Security.Enabled = false` deaktiviert.
- Die AD-Gruppen sind nicht geloescht und bleiben in `AccessGroups`/`AdminGroups` dokumentiert.

Wichtig:

- Finance- und HR-KPI-Sperren laufen weiter ueber eigene Passwortabfragen.
- AD/Rollen koennen spaeter durch `Security.Enabled = true` wieder aktiviert werden.

Noch offen:

1. Entscheiden, wann AD-/Rollenpruefung wieder aktiviert wird.
2. Bei Reaktivierung `Security.Enabled` auf `true` setzen und Gruppen pruefen.
3. Pruefen, ob direkte Run-/Export-/FinanceProbe-Endpunkte ebenfalls geschuetzt werden muessen.
4. In Browser testen:

```text
http://127.0.0.1:5099/finance-cockpit/vergleich
```

5. Nach Entsperren pruefen, dass Navigation und `Finance sperren` korrekt funktionieren.

## Nachtrag 2026-05-19 Finance-Vergleich / Formeldoku

Erledigt:

- `/finance-cockpit/vergleich` nutzt dieselbe `FinanceReconciliationService`-Logik wie die FinanceProbe.
- Leere Ist-Zeilen werden in der Haupt-App ausgefiltert.
- Berechnungslogik pro Land wurde dokumentiert:

```text
docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md
```

Naechster Check:

- Bei neuer Datenladung `/finance-cockpit/vergleich` und `/finance` gegeneinander vergleichen.
- Besonders ES, AT, UK und IT weiter fachlich klaeren.

## Nachtrag 2026-05-19 Zentrale Excel fuer Finance-Filter

Erledigt:

- Die zentrale Excel `Sales_All_yyyy-MM-dd.xlsx` enthaelt im Blatt `Sales` einen Finance-Spaltenblock:

```text
Finance | Year
Finance | Country Key
Finance | Date
Finance | Net Sales Actual
Finance | Currency
Finance | Include
Finance | Source Value Field
```

- Die zentrale Excel enthaelt ein Hilfsblatt `Finance Filter Hilfe`.
- Das Hilfsblatt erklaert, wie Finance dieselben Ist-Summen wie im Testprogramm erhaelt:

```text
Finance | Year = 2025
Finance | Country Key = Land
Finance | Include = TRUE
Summe Finance | Net Sales Actual
```

Geprueft:

- Excel-Finance-Spalten wurden gegen `FinanceReconciliationService` fuer 2025 verglichen.
- AT, CH, ES, FR, IN, IT, UK und US ergaben jeweils `MATCH` mit Differenz `0.00`.

Naechster praktischer Check:

- Nach dem naechsten echten Export die SharePoint-Datei `Sales_All_yyyy-MM-dd.xlsx` oeffnen und mit Finance die Filter-/Summenlogik einmal gemeinsam durchgehen.
- Dabei darauf achten, dass nicht versehentlich alte Spalten wie `Land`, `TSC`, `Document Total LC` oder `Sales Price/Value` direkt fuer CFO-Summen verwendet werden.

## Nachtrag 2026-05-11 UK_B1 Mapping fertigstellen

Aktueller Stand:

- UK/England bleibt auf Quelle `UK_B1`.
- Korrekte Quelle:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/UK_B1
```

- Ursache der grossen UK-Abweichung:
  - kein grafisches Mapping fuer `TRUK`
  - `Sales Price/Value` wurde als Positionswert gelesen
  - in UK_B1 ist es nach aktuellem Befund ein Stueckpreis
  - korrekte Formel ist `=[Sales Price/Value]*[Quantity]`

Bereits im Worktree umgesetzt:

- `ManualExcelImportService` kann berechnete Mapping-Quellen `=[Header A]*[Header B]`.
- `DatabaseSeedService` seedet/repariert UK_B1-Pfad und `TRUK`-Mapping.
- `DatabaseSeedService` ueberspringt den UK-Mapping-Seed, solange `ManualExcelColumnMappings` noch auf eine alte SQLite-Reparaturtabelle wie `Sites_repair_old` zeigt.
- Unit-Test fuer berechnetes Manual-Excel-Mapping ist vorhanden.
- Doku wurde in `docs/FINANCE_ENTSCHEIDE.md`, `lastchange.md` und `HANDOFF_2026-04-15.md` ergaenzt.
- Tests sind gruen: `59/59`.

Verifizierter Testlauf:

```text
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore -p:UseAppHost=false --verbosity minimal
```

Noch offen fuer den praktischen UK-Check:

1. SharePoint-/Graph-Zugriff reparieren.
   - letzter Fehler bei `/run/export/TRUK`:

```text
ClientSecretCredential authentication failed
127.0.0.1:9 connection refused
```

2. UK neu exportieren:

```text
http://127.0.0.1:5099/run/export/TRUK
```

3. Finance pruefen:

```text
http://127.0.0.1:5099/finance
```

4. Ergebnis bewerten:
   - wenn UK nahe `3'749'865 GBP` liegt: Mapping war Hauptursache.
   - wenn UK bei ca. `3'533'349 GBP` bleibt: Restdifferenz gegen weitere UK-Netto-/Discount-/Frachtspalten pruefen.

Nicht vergessen:

- Keine harte Spezialkorrektur fuer genau 2025 einbauen.
- Die Loesung muss ueber Mapping und allgemeine Positionslogik laufen, damit andere Jahre ebenfalls korrekt funktionieren.

## Nachtrag 2026-05-08 Manual Excel/CSV SharePoint-Automatik

Erledigt:

- SharePoint-Ordner koennen bei Manual Excel/CSV als Quelle hinterlegt werden.
- Bei Ordnern wird automatisch die neueste passende `.xlsx`/`.csv` ausgewaehlt.
- Dateinamenmuster fuer bevorzugte Auswahl: `ddMMyy_TSC.xlsx` bzw. `ddMMyy_TSC.csv`.
- Manual-Export schreibt die erzeugte Exportdatei in den Quellordner zurueck:
  - lokal: gleicher lokaler Ordner
  - SharePoint: gleicher SharePoint-Ordner
- England/TRUK ist lokal auf den SharePoint-Ordner `Import/Finance/UK_B1` korrigiert.
- Spanien-Fehler nach erfolgreichem Einlesen der SharePoint-CSV ist behoben.

Naechste konkrete Schritte:

1. App neu starten, damit die Seed-/Repair-Logik aktiv ist.
2. England/TRUK exportieren und pruefen, ob die App `010526_TRUK.xlsx` statt `010426_TRUK.xlsx` auswaehlt.
3. Im SharePoint-Ordner `Import/Finance/UK_B1` pruefen, ob die neue Exportdatei dort wieder abgelegt wird.
4. Deutschland/Alphaplan: im Standort den korrekten Alphaplan-Excel- oder SharePoint-Pfad hinterlegen.
5. Deutschland exportieren und Mapping gegen die Alphaplan-Datei validieren.
6. Falls UK-Dateinamen spaeter ein anderes Muster bekommen, Auswahlregel erweitern.

## Nachtrag 2026-05-08 FinanceProbe

Erledigt:

- FinanceProbe zeigt alle Finance-Referenzen 2025.
- Datenabdeckung je Standort wurde ergaenzt.
- CH/AT-Zuordnung wurde fuer `ZSCHWEIZ` geschaerft.

Naechste fachliche Schritte:

1. Nach Export von England, Schweiz/Oesterreich, Spanien und Deutschland die FinanceProbe neu laden.
2. In der Sektion `Datenabdeckung je Standort` pruefen, ob Zeilen 2025 und Periode plausibel sind.
3. Fuer Laender mit `Keine Daten` entscheiden:
   - Datenquelle fehlt
   - Standort deaktiviert
   - Mapping/Export noch nicht gelaufen
   - Referenz ist nur zukuenftig relevant
4. Fuer AT/CH nach `ZSCHWEIZ`-Export pruefen, ob `LAND1` korrekt `AT` bzw. `CH` liefert.

## Nachtrag 2026-05-11 FinanceProbe KI-Steuerung

Neue Test-Routen:

- `/run/export/{siteKey}` fuer Einzelstandortexporte
- `/run/export-all` fuer alle aktiven Standorte plus zentrale Datei
- `/run/consolidated` fuer nur zentrale Datei

Naechster sinnvoller Prueflauf:

1. FinanceProbe starten.
2. `/run/export/TRUK` fuer England testen.
3. `/run/export/Spanien` testen.
4. `/run/export/Deutschland` testen, sobald Alphaplan-Pfad korrekt ist.
5. `/run/export/ZSCHWEIZ` testen.
6. Danach `/finance` und `docs/finance_status_2025.svg` aktualisieren.

## Nachtrag 2026-05-07 nach Mapper-/Finance-Aufraeumung

Erledigt:

- SAP-OData- und HANA-Mapping laufen ueber `MappedSalesRecordComposer`.
- Doppelte SAP-Mapping-Normalisierung wurde entfernt.
- Konsolidierter Export liest eindeutig aus `CentralSalesRecords`.
- Manuelle Standortdateien duerfen `.xlsx` oder `.csv` sein.
- Finance-Sollwerte, Budgetkurse und Intercompany-Regeln sind DB-Konfiguration mit Seed.

Naechste technische Schritte:

1. App neu starten, damit Schema/Seed fuer `FinanceReferences`, `FinanceIntercompanyRules` und Budgetkurse laeuft.
2. In Settings Konfiguration exportieren und pruefen, ob Finance-Referenzen und IC-Regeln enthalten sind.
3. Fuer produktive Pflege spaeter eine kleine UI fuer `FinanceReferences` und `FinanceIntercompanyRules` bauen.
4. Manual Excel als naechsten Aufraeumpunkt vereinheitlichen: Header-Automatik und grafisches Mapping in eine gemeinsame Mapping-Engine ziehen.
5. Bestehende BI1/SAGE-Standorte mittelfristig auf grafisches HANA-Mapping migrieren; erst danach den alten B1-Spezialpfad entfernen.

## Nachtrag 2026-05-07 ZSCHWEIZ ueber SAP OData

Finaler Stand fuer Schweiz/Oesterreich:

- ABAP-Report `report.abap` fuellt SAP-Tabelle `ZSCHWEIZ`.
- Buchungskreis `1100` = Schweiz, `1200` = Oesterreich.
- `LAND1` in `ZSCHWEIZ` ist Reporting-Land aus Buchungskreis.
- `CUSTOMER_LAND` ist Kundenland aus `KNA1-LAND1`.
- Die App liest `ZSCHWEIZ` ueber SAP OData, nicht ueber direkten HANA-Spezialcode.

In der App:

- Quellsystem-Code `SAP` bleibt bestehen, DisplayName jetzt `SAP OData`.
- `SAP_HANA` ist nur fuer direkte HANA-Tabellen/Views und heisst `SAP HANA Tables/Views`.
- Der grafische Mapper funktioniert fuer SAP OData und fuer HANA-Tabellen/Views.
- Vorkonfigurierter Standort:
  - `TSC = ZSCHWEIZ`
  - `Land = Schweiz/Oesterreich`
  - `SourceSystem = SAP`
  - Quelle `Z`
  - EntitySet `ZSCHWEIZSet`
- Quelle und Feldmapping werden beim App-Start per Seed-/Repair-Logik nachgezogen, auch wenn der Standort bereits existiert.

Naechste Schritte:

1. App neu starten, damit die Seed-/Repair-Logik laeuft.
2. In `Settings -> Quellsysteme` pruefen, ob `SAP` als `SAP OData` angezeigt wird.
3. In `Standorte` den Standort `ZSCHWEIZ` oeffnen.
4. Falls die zentrale SAP-Service-URL noch auf `ZPOWERBI_EINKAUF_SRV` zeigt, beim Standort `SAP Service URL Override` auf den finalen OData-Service fuer `ZSCHWEIZ` setzen.
5. `Entity Sets refreshen`.
6. Quelle `Z` auf `ZSCHWEIZSet` kontrollieren.
7. `Felder aus Quellen laden`.
8. Grafisches Mapping kontrollieren; manuell mappen musst du nur, wenn Gateway-Feldnamen vom erwarteten `ZSCHWEIZ`-Layout abweichen.
9. Standort aktivieren und Export testen.

Keine manuelle Feldliste ist noetig, wenn der Gateway-Service `$metadata` korrekt liefert.

## Nachtrag 2026-05-05 Abschlussstand FinanceProbe / Spanien / Deutschland

Aktueller lokaler Testpunkt:

```text
http://localhost:55417/finance
```

FinanceProbe enthaelt jetzt:

- `Meeting Ampel 2025` fuer alle Laender aus `check.xlsx`
- Ampel:
  - Gruen: rechnerisch passend
  - Gelb: Differenz oder fachliche Abgrenzung offen
  - Grau: keine belastbaren Ist-Daten
- `Detail alle Laender`
- `Spain CSV direct check`
- `Germany Excel sample check`

Spanien:

- finale v2-Datei liegt unter `sagespain/v2/Spain_Sales_2025.csv`
- Zeilen: `4'341`
- Ist `SalesPriceValue`: `3'082'320.18` EUR
- Soll aus `check.xlsx`: `3'102'333.61`
- Differenz: `-20'013.43`
- Status: Gelb / Pruefen
- Export technisch lesbar, Differenz fachlich mit Spanien/Finance klaeren

Deutschland:

- Beispielfile liegt im Projektordner:

```text
DE_Beispiel_Export_Daten.xlsx
```

- relevante Spalte: `NettoPreisGesamtX`
- Mapping-Ziel: `SalesPriceValue`
- Betragszeilen: `2`
- Summe: `8'290.70` EUR
- das ist nur ein Sample, keine finale DE-Jahreszahl
- Deutschland bleibt fuer die finale Ampel offen/grau, bis ein vollstaendiger DE-Jahresfile 2025 oder ein bestaetigter Importlauf vorliegt

Offen fuer das Finance-Meeting / danach:

1. Spanien Differenz `-20'013.43` klaeren:
   - Periodendatum
   - Serien `REG`, `LAT`, `PRO`, `REC`
   - Gutschriften / `REC`
   - offizielle Sage-Auswertung mit identischem Filter
2. Deutschland finalen Jahresfile 2025 anfordern oder Importlauf mit finaler Datei ausfuehren.
3. Fuer Laender mit Grau pruefen, ob Exportdaten fehlen oder Standort deaktiviert/ohne Datei ist.
4. Fuer CHF-Aussage beachten:
   - CHF nur direkt, wenn Quelle CHF liefert
   - sonst Mandanten-/Originalwaehrung und separate FX-Regel noetig

Letzte Verifikation:

```text
dotnet build .\Tools\FinanceProbe\FinanceProbe.csproj --verbosity minimal --no-restore
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal --no-restore
```

Ergebnis:

- FinanceProbe Build erfolgreich
- Tests erfolgreich
- `50/50` Tests gruen
- Web UI `HTTP 200`

## Nachtrag 2026-04-29 Dashboard-Referenzcheck

Das Dashboard enthaelt jetzt oben einen `Net Sales Actuals 2025`-Referenzcheck gegen die Zahlen aus `check.xlsx` / Power BI Stand 2026-04-29.

Technischer Stand:

- Ist-Wert wird automatisch aus dem besten Kandidaten gegen die Referenz gewaehlt:
  - `SalesPriceValue`
  - `DocumentTotalForeignCurrency - VatSumForeignCurrency`
  - `DocumentTotalLocalCurrency - VatSumLocalCurrency`
- Belegkopfwerte werden per `TSC` + `DocumentType` + `DocumentEntry` dedupliziert; Fallback ist `InvoiceNumber`
- Jahr 2025 ueber `InvoiceDate`, fallback `ExtractionDate`
- Vergleich gegen Power-BI-Wert, falls vorhanden, sonst LC-Referenz
- Dashboard zeigt das verwendete `Summenfeld`

Noch fachlich zu pruefen:

- IT bleibt als bekannter `not ok`-Fall offen
- UK/US bleiben offen, bis die richtige Quelle bzw. Config geklaert ist
- bei weiteren Standorten erst Referenzwert und Datenquelle bestaetigen
- bestehende zentrale Altdaten enthalten fuer die neuen B1-Felder noch `0`; fuer den echten Feldvergleich ist ein neuer Export/Rebuild noetig

Konkreter Ablauf nach Neustart/PC-Absturz:

1. App starten und Dashboard oeffnen: `http://localhost:55416`
2. `Alle exportieren` ausfuehren oder betroffene Standorte einzeln exportieren.
3. Danach `Zentrale Datei neu erzeugen` ausfuehren.
4. Im oberen Dashboard-Block `Net Sales Actuals 2025 Referenz` die Spalte `Summenfeld` kontrollieren.
5. Wenn `Status = OK`, passt die Summe zur hinterlegten Referenz.
6. Wenn `Status = Pruefen`, zuerst kontrollieren:
   - richtige Standortquelle/Config
   - richtiges Jahr
   - ob nach der Codeaenderung wirklich neu exportiert wurde
   - ob das gewaehlte Summenfeld fachlich Sinn macht

Naechster technischer Schritt fuer neue Jahre:

- Jahresauswahl im Dashboard einbauen.
- Fuer Jahre ohne Referenz trotzdem Ist-Summen und verwendetes Summenfeld anzeigen.
- Sobald eine neue Referenzdatei fuer 2026/2027 vorliegt, Referenzwerte ergaenzen.

Export-all-Abbruch am 2026-04-29:

- Fehler war SQLite-Schema: `ExportLogs`, `AppEventLogs`, `CentralSalesRecords` zeigten noch auf `"Sites_repair_old"`
- Schema-Reparatur wurde erweitert und beim App-Start erfolgreich angewendet
- gepruefter Zustand danach: alle drei Tabellen referenzieren wieder `Sites`
- Export kann jetzt erneut getestet werden
- falls erneut Fehler kommt, sollte die Snackbar die Inner Exception anzeigen und die Logs sollten nicht mehr selbst den Export abbrechen

Nachtest Export all:

- HANA-Schema-Fehler fuer Frankreich/Italien/USA wurde auf HANA-Quoting zurueckgefuehrt und korrigiert
- Indien bleibt Auth-/Credential-Thema
- England, Spanien und Deutschland sind aktuell `MANUAL_EXCEL` ohne hinterlegte Datei
- Fuer einen sauberen Export-all-Lauf:
  - HANA-Standorte mit korrigierter Query nochmals testen
  - Indien Credentials pruefen
  - manuelle Standorte entweder Datei hinterlegen oder deaktivieren, falls sie nicht im Export-all laufen sollen

## Nachtrag 2026-04-29 B1-Belegwaehrungsfelder

Der HANA/B1-Export zieht jetzt zusaetzliche Belegwaehrungsfelder:

- `DocEntry`
- `DocCur`
- `DocTotalFC`
- `DocTotal`
- `VatSumFC`
- `VatSum`
- `DocRate`
- `OADM.MainCurncy`

Neue Zielfelder:

- `DocumentEntry`
- `DocumentCurrency`
- `DocumentTotalForeignCurrency`
- `DocumentTotalLocalCurrency`
- `VatSumForeignCurrency`
- `VatSumLocalCurrency`
- `DocumentRate`
- `CompanyCurrency`

Zusaetzlich gilt jetzt:

- `StandardCostCurrency` kommt im HANA-Pfad aus `OADM.MainCurncy`
- `Sales_All_*.xlsx` enthaelt die neuen Spalten
- `CentralSalesRecords` enthaelt die neuen Spalten
- bestehende SQLite-DBs werden beim Start um die Spalten erweitert
- Manual-Excel-Import kann die neuen Spalten lesen

### Wichtig fuer Auswertungen

Die neuen `DocumentTotal*`- und `VatSum*`-Werte sind Belegkopfwerte und werden in der positionsbasierten Datei pro Position wiederholt.

Power BI:

- nicht positionsweise summieren
- zuerst nach Beleg deduplizieren, bevorzugt `TSC` + `DocumentType` + `DocumentEntry`
- danach Belegkopfwerte summieren

Positionswerte wie `Sales Price/Value`, `Quantity` und `Standard cost` bleiben fuer positionsbasierte Summen geeignet.

### Verifikation

Geprueft:

```text
dotnet build .\TrafagSalesExporter.csproj --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal
```

Ergebnis:

- Build erfolgreich
- Tests erfolgreich
- `48/48` Tests gruen

## Nachtrag 2026-04-29 Clean-Code-/DI-Befund

Aktueller Architektur- und DI-Zustand nach den letzten Umbauten:

Gesamturteil:

- die App ist deutlich besser strukturiert als zu Beginn
- die Grundarchitektur ist brauchbar bis gut und fuer pragmatischen produktiven Einsatz geeignet
- Dependency Injection wird grundsaetzlich sinnvoll genutzt
- Clean Code ist mittel bis gut, aber noch nicht durchgehend konsequent

Was positiv ist:

- Kernservices laufen weitgehend ueber Interfaces und DI
- `DataSourceAdapter`-Pattern trennt `HANA`, `SAP_GATEWAY` und `MANUAL_EXCEL`
- `SiteExportService` ist dadurch deutlich schlanker als frueher
- UI-nahe Page-Services wurden eingefuehrt
- viele Razor-Seiten sind nicht mehr direkt `DbContext`-lastig
- `Scoped` fuer Page-Services und `Singleton` fuer gemeinsame Infrastruktur/Orchestrierung ist bewusst gewaehlt
- Tests decken wichtige Fachlogik ab, u. a. Transformationen, ConfigTransfer, DatabaseInitialization und ManagementCockpit

Was noch nicht ideal ist:

- `DatabaseInitializationService` bleibt ein produktiver Reparatur-/Migrationsblock und ist kein sauberes versioniertes Migrationssystem
- `Settings.razor` und `Standorte.razor` enthalten weiterhin relativ viel UI-/Workflow-Logik
- `ManagementCockpitService`, `ConfigTransferService` und Teile der Initialisierung sind noch sehr breit
- konsolidierter Export hat historisch noch Semantikreste zwischen Live-Snapshot und `CentralSalesRecords`
- Secrets/Zugangsdaten sind noch nicht ideal geloest
- zentraler Retry-/Resilience-Layer fuer SAP/HANA/SharePoint fehlt
- Auth ist jetzt pragmatisch mit User/Admin geschnitten, aber noch nicht fein nach `Viewer`, `Exporter`, `Admin`, `Finance`

Sinnvolle spaetere Clean-Code-Schritte:

1. `ManagementCockpitService` in kleinere Query-, Aggregation- und Currency-Komponenten teilen
2. `Settings.razor` und `Standorte.razor` weiter Richtung Page-/Application-Services entlasten
3. `DatabaseInitializationService` langfristig durch versionierte Migrationen ersetzen
4. Auth-Policies fachlich feiner schneiden, z. B. `Viewer`, `Exporter`, `Admin`, `Finance`
5. Retry/Timeout/Failure-Handling fuer externe Systeme zentralisieren
6. Secret-Store-Konzept umsetzen

## Nachtrag 2026-04-29 Authentifizierung / AD

Die App wurde nach IT-Rueckmeldung gegen anonymen Zugriff abgesichert.

Neuer Stand:

- globale Authentifizierungspflicht
- produktiv vorgesehen: Windows Authentication / Active Directory
- Zugriff und Adminrechte ueber AD-Gruppen
- kein eigener App-Login
- kein versteckter produktiver Backdoor
- lokaler Development-Bypass nur bei `ASPNETCORE_ENVIRONMENT=Development`

Neue/angepasste Dateien:

- `Program.cs`
- `Security/SecurityOptions.cs`
- `Security/SecurityPolicies.cs`
- `Security/DevelopmentAuthenticationHandler.cs`
- `Components/Routes.razor`
- `Components/_Imports.razor`
- `Components/Layout/NavMenu.razor`
- `Components/Layout/MainLayout.razor`
- `Components/Pages/Settings.razor`
- `Components/Pages/Standorte.razor`
- `Components/Pages/Transformations.razor`
- `appsettings.json`
- `appsettings.Development.json`

Aktuelle Default-Gruppen:

- `TRAFAG\TrafagSalesExporter-Users`
- `TRAFAG\TrafagSalesExporter-Admins`

### Noch mit IT zu klaeren

1. Exakte AD-Domain-/Gruppennamen bestaetigen
2. AD-Gruppen anlegen oder bestehende Gruppen verwenden
3. IIS-Zielumgebung festlegen
4. Auf IIS Windows Authentication aktivieren
5. Auf IIS Anonymous Authentication deaktivieren
6. Sicherstellen, dass produktiv nicht `ASPNETCORE_ENVIRONMENT=Development` gesetzt ist
7. Test mit einem normalen User und einem Admin-User durchfuehren

### Fachliche Rollenentscheidung

Aktuell:

- Admin:
  - `Settings`
  - `Standorte`
  - `Transformations`
- berechtigter User:
  - Dashboard
  - Management Cockpit
  - Logs

Noch zu entscheiden:

- ob `Logs` ebenfalls Admin-only sein soll
- ob Export-Buttons im Dashboard nur fuer eine eigene Rolle `Exporter` sichtbar sein sollen
- ob Management Cockpit fuer alle berechtigten User oder nur fuer Management/Finance-Gruppen sichtbar sein soll

### Verifikation

Geprueft:

```text
dotnet build .\TrafagSalesExporter.csproj --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal
```

Ergebnis:

- Build erfolgreich
- Tests erfolgreich
- `48/48` Tests gruen
- Auth-Policy-Tests fuer AccessGroup, AdminGroup und Development-Admin vorhanden
- lokaler Development-Auth-Start geprueft: `http://localhost:55416` antwortet mit HTTP `200`

## Nachtrag 2026-04-29 Management Cockpit

Seit dem 2026-04-17 wurden im `Management Cockpit` weitere Auswertmoeglichkeiten umgesetzt und nachtraeglich aus dem aktuellen Code rekonstruiert.

Aktueller neuer Stand:

- Summenfeld ist waehbar statt fest auf Umsatz:
  - `Sales Price/Value`
  - `Quantity`
  - `Standard cost`
  - `Quantity * Standard cost`
- Anzeige-Waehrung ist waehbar:
  - `EUR`
  - `USD`
  - `Original`
- betragliche Werte werden ueber `CurrencyExchangeRateService` umgerechnet
- nicht-betragliche Werte wie `Quantity` bleiben ohne Waehrung
- fehlende Wechselkurse werden gezaehlt und in der UI/Hinweisen sichtbar
- zentrale Roh-Auswertung kann weitere Summenfelder als Zusatzspalten in Jahres-, Monats- und Tageswerten anzeigen
- dateibasierte Excel-Analyse nutzt ebenfalls Summenfeld und Anzeige-Waehrung

Betroffene Dateien:

- `Components/Pages/ManagementCockpit.razor`
- `Models/ManagementCockpitModels.cs`
- `Services/IManagementCockpitService.cs`
- `Services/ManagementCockpitPageService.cs`
- `Services/ManagementCockpitService.cs`
- `TrafagSalesExporter.Tests/ManagementCockpitServiceTests.cs`

Neue Tests:

- Umrechnung zentraler Werte in EUR
- Wechselkurs-Cache pro Waehrung/Ziel/Datum
- Mengen-Auswertung ohne Waehrungsumrechnung
- Zusatzwerte in Zeitreihen

### Jetzt sinnvoll zu pruefen

1. `dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal`
2. Management Cockpit in der App oeffnen
3. zentrale Auswertung mit `Sales Price/Value` in `EUR` pruefen
4. zentrale Auswertung mit `Quantity` pruefen und bestaetigen, dass keine Waehrung angezeigt wird
5. Zusatzfelder `Quantity` und `Quantity * Standard cost` in Jahres-/Monatswerten pruefen
6. Dateianalyse einer exportierten Excel mit unterschiedlichen Summenfeldern pruefen
7. fachlich klaeren, ob `CHF` neben `EUR` und `USD` als Anzeige-Waehrung angeboten werden soll
8. fachlich klaeren, ob fehlende Wechselkurse als `0` in Zielwaehrung korrekt sind oder separat ausgewiesen werden sollen

## Nachtrag 2026-04-17 Refactoring-Fortschritt

Mehrere frueher als hoch priorisiert markierte Architekturpunkte sind inzwischen bereits umgesetzt.

Erledigt:

- DataSourceAdapter-Pattern fuer `HANA`, `SAP_GATEWAY`, `MANUAL_EXCEL`
- `SiteExportService` deutlich verschlankt
- Page-Services auf `Scoped`
- `DatabaseInitializationService` in Schema-/Seed-/Orchestrator-Bloecke getrennt
- `Dashboard`, `Logs` und `Transformations` von direktem `DbContext`-Zugriff befreit
- HANA-SQL-Injection-Pfad geschlossen
- blockierende `.GetAwaiter().GetResult()`-Aufrufe im HANA-Pfad entfernt

Neuer verifizierter Stand:

- `dotnet build .\TrafagSalesExporter.csproj --verbosity minimal` erfolgreich
- `dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal`
- `36/36` Tests gruen

### Neue Top-Prioritaeten ab jetzt

#### 1. Adapter- und Resolver-Tests nachziehen

Prio hoch.

Warum:

- das neue `DataSourceAdapter`-Pattern ist architektonisch wichtig
- genau dieser neue Schnitt hat aktuell noch keine gezielten Unit-Tests

Sinnvoll waeren:

- `DataSourceAdapterResolver`-Tests
- `HanaDataSourceAdapter`-Tests
- `SapGatewayDataSourceAdapter`-Tests
- `ManualExcelDataSourceAdapter`-Tests

#### 2. Retry-/Robustheitslayer

Prio hoch.

Vor allem fuer:

- SharePoint
- SAP Gateway
- HANA-nahe Netzpfade

Aktuell brechen diese Integrationen bei transienten Problemen zu direkt ab.

#### 3. Secret-Store-Konzept

Prio hoch bis mittel.

Aktuell liegen Zugangsdaten weiterhin in der App-/DB-Konfiguration.
Langfristig sollte entschieden werden:

- Windows Credential Manager
- DPAPI / verschluesselte Ablage
- externer Secret Store

#### 4. `DatabaseInitializationService` weiter haerten, aber nicht mehr blind gross refactoren

Prio mittel.

Der schlimmste Architekturteil ist deutlich besser als vorher.
Weitere Arbeit dort sollte jetzt nur noch zielgerichtet passieren:

- Regressionstests fuer konkrete Legacy-/Repair-Zustaende
- spaeter moeglichst versionierte Migrationen

#### 5. MudBlazor-Analyzer-Warnungen bereinigen

Prio mittel.

Nicht kritisch fuer Produktion, aber sinnvoll fuer sauberen Build:

- `Logs.razor`
- `Transformations.razor`
- `Standorte.razor`

### Was im Vergleich zu frueher nicht mehr Top-Prioritaet ist

Nicht mehr ganz oben:

- generisches weiteres Page-Service-Refactoring um des Refactorings willen
- noch mehr strukturelles Verschieben ohne Risikoreduktion

Der wirtschaftlich sinnvolle Fokus liegt jetzt eher auf:

- Absicherung
- Robustheit
- Integrationsstabilitaet

## Nachtrag 2026-04-17

Der Punkt `CHF-Umrechnung / Wechselkurse` ist nicht mehr komplett offen.

Der aktuelle Ist-Stand ist:

- `CurrencyExchangeRateService` ist implementiert
- `ExchangeRateImportService` importiert ECB-Kurse
- `NormalizeCurrencyCode` und `ConvertCurrency` sind im Transformationssystem registriert
- fehlende Unit-Tests dafuer wurden am 2026-04-17 ergaenzt

Neuer Teststand:

- `dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal`
- erfolgreich
- `31/31` Tests gruen

Was fuer Waehrungen trotzdem noch offen bleibt:

- fachlicher Einsatz der `ConvertCurrency`-Regeln in echten Standortkonfigurationen pruefen
- UI-Flow fuer Wechselkurspflege in `Settings.razor` manuell gegenpruefen
- ECB-Import einmal real ueber die UI bzw. App-Funktion pruefen
- bestaetigen, fuer welche Sichten CHF die Zielwaehrung sein soll
- Management-Cockpit-Rohsicht nur dann auf CHF umstellen, wenn fachlich gewuenscht

## Architektur-Nachtrag 2026-04-17

Nach einer separaten Architekturpruefung wurden die naechsten Schritte neu priorisiert.

Wichtig:

- neue Fachfeatures sind aktuell **nicht** der erste Engpass
- zuerst muessen die Architektur-Risiken in Initialisierung, Config-Import und UI-Service-Schnitt bereinigt werden

### Neue Top-Prioritaeten

#### 1. `DatabaseInitializationService` absichern

Prio sehr hoch.

Gruende:

- Startlogik enthaelt manuelle Schema-Migrationen
- FK-Reparaturen laufen produktiv beim App-Start
- dort wurde ein konkretes Risiko fuer verschobene Spaltenwerte beim `Sites_old`-Kopierpfad erkannt

Vor weiterer Fachentwicklung:

- Initialisierungspfad genau pruefen
- SQL-Kopierlogik validieren
- moeglichst Richtung versionierte Migrationen bewegen

#### 2. `ConfigTransferService.ImportJsonAsync` neu denken

Prio sehr hoch.

Aktuelles Problem:

- Import loescht sehr viel und baut danach stueckweise neu auf
- nicht atomar
- potenziell teilzerstoerter Zustand bei Fehlern
- `CentralSalesRecords` werden mitimportiert/mitgeloescht, obwohl sie eher Laufzeitdaten als Konfiguration sind

Ziel:

- atomarer Import
- saubere Trennung zwischen Konfiguration und Betriebsdaten

#### 3. Razor-Seiten entlasten

Prio hoch.

Betroffen vor allem:

- `Components/Pages/Settings.razor`
- `Components/Pages/Standorte.razor`

Ziel:

- DB- und Fachlogik aus UI-Code in Services / Application-Layer verschieben
- Seiten nur noch fuer Interaktion und Formularzustand

#### 4. Konsolidierten Export semantisch klaeren

Prio mittel.

Offene Frage:

- zentrale Datei aus laufendem Snapshot
  oder
- zentrale Datei immer aus `CentralSalesRecords`

Aktuell ist die Verantwortung unscharf.

#### 5. Reporting verallgemeinern

Prio mittel.

Erst nach den Infrastrukturthemen:

- hartcodierte Jahreslogik im Cockpit entfernen
- fachlich entscheiden, ob und wo CHF-Rohsicht gebraucht wird

### Praktische Reihenfolge fuer den naechsten Wiedereinstieg

Wenn nach erneutem Absturz oder Kontextverlust weitergemacht wird:

1. `HANDOFF_2026-04-15.md` lesen, speziell die Architekturpruefung vom 2026-04-17
2. `DatabaseInitializationService` als ersten Risikoblock ansehen
3. `ConfigTransferService.ImportJsonAsync` als zweiten Risikoblock ansehen
4. erst danach wieder an Cockpit / CHF / weitere Fachfeatures gehen

## Nachtrag HANA-/Standort-Workflow 2026-04-17

Der doppelte HANA-Workflow wurde inzwischen bereits bereinigt.

Neuer Stand:

- oben zentrale HANA-Konfiguration pro Quellsystem `BI1` / `SAGE`
- unten im Standort keine eigene wirksame Voll-HANA-Konfiguration mehr
- HANA-basierte Standorte ziehen ihre technische Verbindung aus der zentralen Quellsystem-Konfiguration
- Standort bleibt fuer fachliche Daten und optionale Credential-Overrides zustaendig
- die frueher doppelte HANA-UI im Standortdialog ist inzwischen auch sichtbar entfernt
- der Verbindungstest in `Settings.razor` prueft und meldet jetzt die zentrale HANA-Verbindung klar

### Was dazu noch praktisch geprueft werden sollte

- `Standorte`-Seite im UI manuell durchklicken
- pruefen, ob `BI1`- und `SAGE`-Standort beim Speichern sauber auf die zentrale HANA-Konfiguration zeigen
- pruefen, ob Aenderung oben bei zentraler HANA-Konfiguration in nachfolgenden Exporten wirklich greift

### Anschlussarbeiten

- `ConfigTransferService` spaeter auf das neue zentrale HANA-Modell fachlich nachziehen und kritisch pruefen
- `DatabaseInitializationService` weiter konsolidieren, damit die Zuordnung alter HANA-Daten langfristig robuster wird

## Nachtrag Quellsystem-Verwaltung 2026-04-17

Die bisher hart codierten Quellsystem-Listen wurden ersetzt.

Neuer Stand:

- `SourceSystemDefinition` ist jetzt die zentrale Stammdatenquelle fuer Quellsysteme
- `Settings.razor` hat jetzt eine GUI zur Pflege von Quellsystemen
- `Standorte.razor` zieht seine Quellsystem-Auswahl aus diesen Stammdaten
- `Transformations.razor` zieht die Systemauswahl ebenfalls aus diesen Stammdaten
- zentrale Credentials haengen jetzt am Quellsystem selbst
- HANA-Zentralverbindungen werden nur noch fuer Quellsysteme mit Anschlussart `HANA` gezeigt
- alte zentrale Credential-Felder in `ExportSettings` sind aus dem aktiven Codepfad entfernt
- `ExportSettings` wird beim Start auch schematisch auf das neue Feldset bereinigt
- HANA speichert zentral keine eigenen Credentials mehr; dort bleiben nur technische Verbindungsdaten
- `HanaServer.Username` / `Password` sind nur noch Laufzeitfelder und nicht mehr im EF-Schema gemappt
- SAP Service URL wird jetzt zentral im Quellsystem gepflegt; der Standort haelt nur noch ein optionales Override
- Quellsysteme werden jetzt per Dialog bearbeitet statt nur ueber Inline-Tabellenfelder

### Was dazu noch praktisch geprueft werden sollte

- in `Settings` ein neues Quellsystem per GUI anlegen
- pruefen, ob es danach in `Standorte` und `Transformations` sofort auswählbar ist
- pruefen, ob deaktivierte Quellsysteme in neuen Standort-/Regelanlagen nicht mehr normal angeboten werden
- pruefen, ob Aenderung der Anschlussart von `HANA` auf `SAP_GATEWAY` oder `MANUAL_EXCEL` fachlich sauber wirkt
- pruefen, ob bestehende BI1/SAGE/SAP-Daten nach Startmigration korrekt in `SourceSystemDefinitions` stehen
- pruefen, ob Konfiguration-Export/Import ohne die alten Credential-Felder sauber mit `SourceSystemDefinitions` arbeitet
- pruefen, ob zentrale SAP Service URL ohne Override sauber fuer Refresh, Export und Dashboard greift
- pruefen, ob SAP Service URL Override am Standort die zentrale URL erwartungsgemaess uebersteuert

## Nachtrag 2026-04-16

Seit dem letzten Stand kamen mehrere groessere Erweiterungen dazu. Die offenen Punkte unten muessen deshalb im neuen Kontext gelesen werden.

## 0. Neuer Ist-Stand

Zusaetzlich zum alten Stand ist jetzt vorhanden:

- manueller Standort-Import ueber `MANUAL_EXCEL`
- Dashboard mit `Alle exportieren`, `Zentrale Datei neu erzeugen` und zentralem `Excel oeffnen`
- Roh-Auswertung im `Management Cockpit` direkt aus `CentralSalesRecords`
- erweitertes Transformationssystem mit `Value`- und `Record`-Regeln
- HANA-Schema-Lookup im Standortdialog
- Testprojekt mit aktuell 18 gruenden Tests

## 1. Status

Der Export geht jetzt wieder durch.

Die zuletzt gefundene Hauptursache war nicht mehr ein reiner SQLite-Lock beim Batch-Insert, sondern ein kaputter FK-Schemazustand in der bestehenden DB:

- SQLite referenzierte in mindestens einer Tabelle noch `main.Sites_old`
- dadurch scheiterte `SaveChangesAsync()` beim Schreiben z. B. in `AppEventLogs` oder `ExportLogs`
- sichtbarer Effekt: Export blieb nach `Zentrale Tabelle: ... Datensaetze gespeichert.` haengen

## 2. Umgesetzter Fix

Umgesetzt wurde:

- Dashboard-Live-Status liest waehrend laufendem Export nicht mehr staendig aus `AppEventLogs`, sondern nutzt den In-Memory-Status des `ExportOrchestrationService`
- SQLite `Default Timeout` in `Program.cs` auf `60` erhoeht
- `CentralSalesRecordService` setzt nach den Batches explizit `Zentrale Tabelle aktualisiert`
- `DatabaseInitializationService` repariert beim App-Start automatisch Tabellen, deren FK-SQL noch `Sites_old` referenziert

Betroffene Dateien:

- `Program.cs`
- `Components/Pages/Dashboard.razor`
- `Services/CentralSalesRecordService.cs`
- `Services/DatabaseInitializationService.cs`

## 3. Was noch getestet werden sollte

Kurz gegenpruefen:

- Export eines Standorts erneut
- `Excel oeffnen` nach erfolgreichem Export
- `Export erfolgreich` inkl. `Pfad=...`
- Dashboard-Live-Status setzt sich nach Abschluss sauber zurueck
- `Alle exportieren`
- `Zentrale Datei neu erzeugen`
- zentrale Datei im Dashboard oeffnen

## 3a. Manuellen Excel-Import pruefen

Zu testen:

- Standort auf `MANUAL_EXCEL` stellen
- Excel im Standort hochladen
- Standort exportieren
- pruefen, ob `CentralSalesRecords` fuer diesen Standort ersetzt wurden
- pruefen, ob der zentrale Export den Standort korrekt enthaelt

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/ManualExcelImportService.cs`
- `Services/SiteExportService.cs`

## 3b. HANA-Schema-Lookup pruefen

Zu testen:

- bei `BI1`-Standort `Schemas laden`
- bei `SAGE`-Standort `Schemas laden`
- wird ein plausibles B1-Schema angeboten?
- funktioniert danach Export ohne manuelle Schema-Eingabe?
- zeigt England / Spezialstandort jetzt schneller, wenn Schema oder Rechte nicht passen?

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/HanaQueryService.cs`

## 4. Falls wieder ein Fehler auftritt

In dieser Reihenfolge pruefen:

1. Exakte Fehlermeldung aus `AppEventLogs` bzw. Console notieren
2. Pruefen, ob die Reparaturlogik beim Start gelaufen ist
3. Pruefen, ob noch weitere Tabellen mit veralteter FK-Referenz existieren
4. Erst danach wieder am Batch-/Commit-Pfad der zentralen Speicherung arbeiten

## 5. SAP-Funktionalitaet kurz gegenpruefen

Zu testen:

- `Quellen refreshen`
- `Felder aus Quellen laden`
- `Auto-Match`
- SAP-Export eines Standorts

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/SapGatewayService.cs`
- `Services/SapCompositionService.cs`

## 6. Management Cockpit pruefen

Zu testen:

- vorhandene Excel-Datei auswaehlbar
- Analyse laeuft
- Kennzahlen plausibel
- Roh-Auswertung aus `CentralSalesRecords` laeuft
- Jahr/Monat-Filter funktionieren
- Summen nach Quelle / Land plausibel

Dateien:

- `Components/Pages/ManagementCockpit.razor`
- `Services/ManagementCockpitService.cs`

## 6a. Fachlich bewusst noch offen

Noch nicht final umsetzen ohne Rueckmeldung Fachseite:

- Intercompany-Filter
- fachliche Nutzung der CHF-Umrechnung in Cockpit / Reports
- Budgetvergleich
- Gruppenlogik
- Spartenlogik
- Margenlogik

Diese Punkte sollen spaeter moeglichst dynamisch auf dem neuen Transformations-/Mapping-Ansatz aufsetzen, aber aktuell nicht hart geraten werden.

## 6b. Naechste sinnvolle Testkandidaten

Wenn weiter in Tests investiert wird, sind die naechsten Kandidaten:

- `ExportOrchestrationService`
- spaeter End-to-End-Tests fuer den Wechselkurs-/Transformationspfad
- spaeter evtl. SQLite-nahe Integrationstests fuer `DatabaseInitializationService`

Aktueller Teststatus:

- `dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal`
- erfolgreich
- `31/31` Tests gruen

## 7. Referenzdatei

Fuer den vollstaendigen Kontext zuerst lesen:

- `HANDOFF_2026-04-15.md`

## 8. Letzte bereinigte UI-Irritation

Stand 2026-04-17:

- In `Standorte` wurde die obere Box auf `Zentrale HANA-Technik` geklaert.
- Dort gibt es keinen `Server hinzufuegen`-Pfad mehr.
- Grund: zentrale HANA-Eintraege werden aus `Quellsystemen` mit Anschlussart `HANA` abgeleitet.
- `SAP` gehoert fachlich nicht in diese Box, sondern in `Settings -> Quellsysteme`.

Wichtig fuer den naechsten Wiedereinstieg:

- Wenn ein Benutzer fragt `wo ist SAP?`, ist die richtige Antwort: nicht in der HANA-Box, sondern in der zentralen Quellsystem-Verwaltung.
- Wenn ein HANA-System oben fehlt, zuerst `Settings -> Quellsysteme` pruefen und dort Anschlussart `HANA` setzen.

## 9. Config-Transfer erneut geprueft

Stand 2026-04-17:

- Der aktuelle Config-Import/-Export passt zum neuen Datenmodell.
- Zentral verwaltete Quellsysteme, SAP-Zentral-URL, HANA-Technik ohne HANA-Credentials und Standort-Overrides werden korrekt im Transferformat abgebildet.
- Die vorhandenen `ConfigTransferServiceTests` bestaetigen den aktuellen Rundlauf.

Fuer den naechsten Wiedereinstieg wichtig:

- Das aktuelle Format ist fuer heutige Exporte konsistent.
- `ImportJsonAsync` ist aber weiterhin nicht atomar und loescht zuerst produktive Konfiguration.
- Zusaetzlich gibt es ein Altformat-Risiko:
  - aeltere JSONs mit `SourceSystemDefinitions`, aber ohne `ConnectionKind`, koennen wegen DTO-Default falsch als `HANA` interpretiert werden.

Naechste saubere Haertung fuer dieses Thema:

- Config-Import transaktional machen
- Legacy-Fallback fuer fehlendes `ConnectionKind` einbauen

## 10. Nachtrag 2026-05-20: Finance-Regeln statt harte Laenderlogik

Aktueller Stand:

- Es gibt jetzt `Admin -> Finance Regeln`.
- Die fachliche Abgrenzung fuer Finance wird dort als Regel gepflegt:
  - Land/Scope
  - Jahr
  - Regeltyp
  - Feld
  - Vergleich
  - Wert
  - Notiz
  - Sortierung/Aktiv
- Diese Regeln wirken auf:
  - zentrales Excel (`Finance | ...` und `Finance Summary`)
  - Soll/Ist Vergleich
- Sie veraendern nicht:
  - Rohdatenimport
  - Mapping in `Admin -> Standorte`
  - technische Transformationen in `Admin -> Transformationen`

UI-Logik fuer Keyuser:

```text
Admin -> Standorte          = Quelle und Spaltenmapping
Admin -> Transformationen   = technische Feldnormalisierung/-berechnung
Admin -> Finance Regeln     = CFO-/Finance-Abgrenzung
```

Wichtige Default-Regeln:

- DE:
  - Alphaplan-Jahresfile -> Finance-Jahr 2025
  - Trafag AG ausschliessen
  - Magnetic Sense ausschliessen
  - GS2510095 ausschliessen
  - GS-Gutschriften negativ
- IT:
  - Trafag Italia ausschliessen
  - doppelte Blank-Supplier-Country-Zeilen deduplizieren

Nach jedem Regelwechsel testen:

1. passenden Standort exportieren
2. zentrale Datei neu erzeugen
3. im Endexcel `Finance Summary` kontrollieren
4. `Soll/Ist Vergleich` kontrollieren

Letzter DE-Pruefstand:

```text
DE 2025 im zentralen Excel: 3'652'394.46
```

## 11. Nachtrag 2026-05-20: Export Dashboard Datenbasis

Im Export Dashboard steht direkt nach `Land` die Spalte `Basis`.

Angezeigt wird:

- `Excel-Datei` mit Tabellen-Icon
- `CSV-Datei` mit Datei-Icon
- `SAP Service` mit Cloud-Sync-Icon
- `Server` mit Storage-Icon
- `Manuelle Datei`, falls manuelle Quelle ohne erkennbaren Pfad

Die Spalte kommt aus `DashboardPageService.ResolveDataBasis`.
