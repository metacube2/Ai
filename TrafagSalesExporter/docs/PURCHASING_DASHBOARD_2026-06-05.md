# Einkaufsdashboard 2026-06-05

Nachtrag 2026-06-18: Das Einkaufsdashboard wurde fuer die Management-/Einkaufssicht nachgezogen und deployed. Schwerpunkt war die Excel-aehnliche Lieferant/Jahr-Kaskadierung analog Referenzbild `einkauf.png`, Zeitraum 2020 bis aktuelles Jahr, Spend aktuelles Jahr je Lieferant, offene Bestellungen/Zulauf, Filter fuer Loeschkennzeichen und MARA-MSTAE sowie echte Lieferantennamen statt Platzhalter.

## Ziel

Der neue Bereich `Einkauf` soll die vorhandene Power-BI-Vorlage `x.pbix` aufnehmen und um weitere SAP-Einkaufsanalysen ergaenzen.

## Aus `x.pbix` uebernommene Struktur

Analysierte PBIX-Seiten:

- Beschaffungsvolumen CHF je Lieferant.
- Einkaufsvolumen CHF je Lieferant als Kuchenansicht.
- Balkenansicht Volumen je Lieferant und Warengruppe.
- Diagramm Volumen je Warengruppe.
- Einkaufsvolumen CHF je Region.
- Preisentwicklung CHF.
- Matrix Volumen je Warengruppe.

Sichtbare PBIX-Felder:

- `EKPOSet.Netwr CHF`
- `EKPOSet.Netwr CHF/Stk`
- `EKKOSet.Bedat`
- `Data.Name`
- `Data (2).WG komplett`
- `EKPOSet.Matnr`
- `EKPOSet.Txz01`

## Zusaetzlich aufgenommene SAP-Themen

Das Dashboard wurde fachlich um diese Bereiche erweitert:

- Spend total vergangen nach Jahr, Lieferant, Warengruppe und Artikel.
- Offene Bestellwerte und offene Mengen nach Lieferant, Warengruppe und Artikel.
- Offene Verpflichtungen / Mengenkontrakte nach Lieferant, Warengruppe und Artikel.
- Lieferantenbewertungen und Performance nach Lieferant, Warengruppe und Artikel.

## Aktueller Implementierungsstand

- Route: `/einkauf`.
- Hauptnavigation: eigener Punkt `Einkauf` mit Einkaufswagen-Icon.
- Tabs im Einkaufsdashboard:
  - Die frueheren Tabs wurden in echte linke Navigationspunkte unter `Einkauf` umgebaut.
- `Einkauf Dashboard`: Uebersicht, SAP-Datenfluss, Live-Status und Analyseachsen.
- `Spend`: Spend total vergangen nach Jahr, Lieferant, Warengruppe und Artikel.
- `Offene Bestellungen`: offene Werte, Mengen und Faelligkeiten.
- `Kontrakte`: offene Verpflichtungen und Kontrakt-Restwerte.
- `Lieferanten`: Lieferantenbasis, Performance und Datenstatus.
- `Ideen`: aufklappbarer Navigationspunkt fuer die naechsten Umsetzungsbausteine.
  - `Uebersicht`.
  - `Einkauf-Datenservice`.
  - `Liefertermin-Risiko`.
  - `Preisabweichung`.
  - `Spend-Konzentration`.
  - `Datenqualitaet`.
- `Kennzahlen-Katalog`: fachlicher KPI-Katalog fuer den naechsten Ausbau.
  - `PBIX Vorlage`: aus `x.pbix` uebernommene Seiten/Visuals.
  - `3D Simulation`: drehbare 3D-What-if-Analyse.
- Unterpunkt `Einkauf > Datenquellen` fuer SAP/OData-Verbindung, Quellen, Join-Fluss und Zielmappings.
- Die Seite ist als Cockpit-Struktur umgesetzt und ueber den vorhandenen UI-Sprachservice mehrsprachig vorbereitet.
- EKKO, EKPO und EKET werden per SAP/OData in lokale Cache-Tabellen geladen.
- Das Cockpit liest zuerst den Cache und nutzt nur noch als Fallback eine begrenzte Live-Probe, falls noch kein Cache vorhanden ist.
- Seit 2026-06-18 ist der Zeitraumfilter standardmaessig auf 2020 bis aktuelles Jahr ausgerichtet.
- Seit 2026-06-18 gibt es eine Excel-aehnliche Kaskadierungstabelle Lieferant x Jahr mit Jahresspalten, Gesamtsumme und Top-down-Sortierung.
- Spend im aktuellen Jahr wird pro Lieferant separat analysiert.
- Bereits beschafft/gebucht und offene Bestellungen/Zulauf werden getrennt visualisiert.
- Geloeschte Positionen (`LOEKZ`) und Materialstatus (`MARA-MSTAE`) sind als Filterdimensionen vorgesehen; `MSTAE` wirkt, sobald das Feld im Cache gefuellt ist.
- Aktive Lieferanten werden aus echten Einkaufsbewegungen abgeleitet; generische Lieferantenplatzhalter werden nicht mehr erzeugt.

## Mehrsprachigkeit Stand 2026-06-11

Commit `1dbaa66 Add purchasing translations` hat die fehlenden UI-Texte fuer den Einkaufsbereich im zentralen `UiTextService` nachgezogen.

Abgedeckt:

- Hauptnavigation: `Einkauf`, `Einkauf Dashboard`, `Einkauf Datenquellen`.
- Einkaufsdashboard: Uebersicht, SAP-Datenfluss, Live-Status, Zeitraumfilter, KPI-Karten, Detailbereiche, Ideen, Kennzahlen-Katalog, PBIX-Vorlage und 3D-Simulation.
- `Einkauf > Datenquellen`: Verbindung, Quellen, Join-Fluss, Mapping, aktuelle Basis, Buttons, Hilfstexte und Speicher-/Reset-Meldungen.
- Sprachen: Spanisch, Italienisch und Hindi.

Bewusst nicht uebersetzt:

- Technische Namen und Feldnamen wie `EKKO`, `EKPO`, `EKET`, `EKKOSet`, `EKPOSet`, `eketSet`, SAP-Felder, Aliasnamen, TSC und Dateimuster.
- Power-BI-Seitentitel aus der importierten PBIX-Vorlage bleiben als fachliche Referenz sichtbar.

Deploy:

- Publiziert am 2026-06-11 auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- `BiDashboard.dll` Zeitstempel nach Deploy: `11.06.2026 12:30:27`.
- Validierung vor Publish: `dotnet test TrafagSalesExporter.sln --verbosity minimal`, Ergebnis `92/92` Tests gruen.

## Navigation und Admin-Steuerung

Stand 2026-06-05: Die Einkaufsbereiche sind nicht mehr als obere Tabs im Dashboard versteckt, sondern als eigene URLs umgesetzt:

- `/einkauf`
- `/einkauf/spend`
- `/einkauf/offene-bestellungen`
- `/einkauf/kontrakte`
- `/einkauf/lieferanten`
- `/einkauf/ideen`
- `/einkauf/ideen/datenservice`
- `/einkauf/ideen/liefertermin-risiko`
- `/einkauf/ideen/preisabweichung`
- `/einkauf/ideen/spend-konzentration`
- `/einkauf/ideen/datenqualitaet`
- `/einkauf/kennzahlen`
- `/einkauf/pbix`
- `/einkauf/3d`
- `/einkauf/verbindungen`

Die Defaults werden ueber `NavigationMenuItems` geseedet. Dadurch kann der Admin in `Admin > Menuestruktur` einzelne Einkaufs-Unterpunkte ausblenden, sortieren oder umhaengen.

## SAP/OData-Konfiguration

Vorbefuellte Quellen:

- `EKKO -> EKKOSet`
- `EKPO -> EKPOSet`
- `EKET -> eketSet`
- `LIEF -> Data`
- `WG -> Data2`

Vorbefuellte Joins:

- `EKKO.Ebeln = EKPO.Ebeln`
- `EKPO.Ebeln,Ebelp = EKET.Ebeln,Ebelp`
- `EKKO.Lifnr = LIEF.Lifnr`
- `EKPO.Matkl = WG.Matkl`

Die Seite verwendet dieselben Grundtabellen wie die Finance-/Standorte-Quellenpflege: `Sites`, `SapSourceDefinitions`, `SapJoinDefinitions`, `SapFieldMappings`.

## SAP/OData Live-Stand 2026-06-05

Der SAP-Test hat bestaetigt, dass die Einkaufstabellen Daten enthalten:

- `EKKO` ab `01.01.2026`: 2'748 Koepfe.
- `EKPO` gesamt: 233'920 Positionen.
- `EKET` gesamt: 242'571 Einteilungen.
- Join `EKKO -> EKPO` ab `01.01.2026`: 3'464 Zeilen.
- Join `EKKO -> EKET` ab `01.01.2026`: 3'458 Zeilen.

Nach Aktivierung der angepassten SAP-Methoden liefern die OData-Services:

- `EKPOSet?$top=5`: HTTP 200 mit Daten.
- `eketSet?$top=5`: HTTP 200 mit Daten.
- `EKPOSet?$filter=Ebeln eq '45148366'`: 1 Zeile.
- `eketSet?$filter=Ebeln eq '45148366'`: 1 Zeile.

Wichtig: Die OData-Property heisst `Ebeln`. Ein Filter mit `EBELN` liefert HTTP 400.

## Full Load / Delta Stand 2026-06-05

Der erste vollstaendige SAP-Load wurde am 2026-06-05 ausgefuehrt.

Geladene Cache-Zeilen:

- `PurchasingEkkoCache`: 172'874 EKKO-Koepfe.
- `PurchasingEkpoCache`: 233'921 EKPO-Positionen.
- `PurchasingEketCache`: 242'572 EKET-Einteilungen.

Technische Logik:

- SAP liefert pro OData-Seite maximal 1'000 Zeilen.
- Der Loader liest deshalb mit `$top=1000`, `$skip` und stabiler Sortierung:
  - `EKKOSet`: `$orderby=Ebeln`.
  - `EKPOSet`: `$orderby=Ebeln,Ebelp`.
  - `eketSet`: `$orderby=Ebeln,Ebelp,Etenr`.
- Nicht vorhandene OData-Felder wurden entfernt:
  - `EKKOSet.Bsart` existiert in diesem Service nicht.
  - `EKPOSet.Meins` existiert in diesem Service nicht.
- Nach dem Full Load kann `Delta aktualisieren` genutzt werden. Delta liest geaenderte EKKO-Belege ab `Aedat` und laedt die zugehoerigen EKPO/EKET-Zeilen je Beleg nach.

## Live-Kennzahlen im Dashboard

Die Seite `/einkauf` zeigt nun echte Werte aus dem SAP-Cache:

- `Spend total`: Summe `EKPOSet.Netwr` aus dem Cache, begrenzt auf den gewaehlten Zeitraum.
- `Offene Bestellungen`: Anzahl EKKO-Belege im gewaehlten Zeitraum.
- `Kontrakte`: offener Restwert aus `EKET.Menge - EKET.Wemng` bewertet mit EKPO-Netto-Stueckwert.
- `Offener Bestellwert`: berechnet aus EKET-Offenmenge und EKPO-Netto-Stueckwert.
- `Offene Menge`: Summe offener EKET-Mengen.
- Top-Lieferant, Top-Warengruppe und Top-Artikel werden aus EKPO gruppiert.
- Top-Artikel zeigt nun Artikel, Lieferant und Bestellmonat, damit ein Wert wie `C42698: CHF 1` fachlich nachvollziehbar ist.
- Die Verpflichtungs-/Kontraktseite zeigt Top-Restverpflichtungen nach Lieferant, Artikel und Faelligkeitsmonat, nicht nur den Monatsverlauf.
- Offene Verpflichtungen werden nicht mehr primaer als reine Vergangenheits-Zeitreihe interpretiert; fuer Einkauf ist die Zukunfts-/Faelligkeitssicht nach Lieferant und Artikel fachlich aussagekraeftiger.
- Spend-, Offenwert- und Kontrakt-Diagramme verwenden Cache-Gruppierungen, sofern der Cache gefuellt ist.
- Ist der Cache leer oder nicht erreichbar, faellt das Dashboard auf eine begrenzte SAP-Live-Probe zurueck.
- Der Standardzeitraum ist seit 2026-06-18 auf 2020 bis heute ausgerichtet. Die Datumsabgrenzung erfolgt im Dashboard ueber `Von Monat` und `Bis Monat`.

## PowerBI-Abgleich

Das Einkaufsdashboard wurde gegen die sichtbaren Auswertungen aus `x.pbix` abgeglichen:

- `Besch.Volumen CHF/Lieferant`: `Sum(EKPOSet.Netwr CHF)` nach Jahr, Lieferant, Warengruppe und Artikel.
- `Eink.Vol. CHF / Lieferant Kuchen`: `Sum(EKPOSet.Netwr CHF)` nach Lieferant.
- `Balken Vol./Lief/WG`: `Sum(EKPOSet.Netwr CHF)` nach Jahr und Lieferant.
- `Diagramm Vol./WG`: `Sum(EKPOSet.Netwr CHF)` nach Jahr und Warengruppe.
- `Eink.Vol. CHF / Region`: `Sum(EKPOSet.Netwr CHF)` nach Region.
- `Preisentwicklung CHF`: `Min(EKPOSet.Netwr CHF/Stk)` nach Artikel und Jahr.
- `Matrix Vol./WG`: `Sum(EKPOSet.Netwr CHF)` nach Warengruppe, Lieferant und Artikel.

Umgesetzt ist die gleiche Kernaggregation:

- Spend und Volumen verwenden `SUM(EKPO.Netwr)` mit Zeitraumfilter auf `EKKO.Bedat`.
- Preisentwicklung verwendet `MIN(EKPO.Netwr / EKPO.Menge)` je Artikel und Jahr mit Zeitraumfilter auf `EKKO.Bedat`.
- Offene Werte verwenden `MAX(EKET.Menge - EKET.Wemng, 0) * (EKPO.Netwr / EKPO.Menge)`.

Noch nicht final 1:1 ist die Namensauflösung:

- PowerBI nutzt fuer Lieferanten- und Warengruppennamen `Data.Name`, `Data.Lieferant`, `Data (2).Warengruppe` und `Data (2).WG komplett`.
- Der aktuelle SAP-OData-Service liefert produktiv `EKKOSet`, `EKPOSet` und `eketSet`; die Cache-Tabellen sind seit 2026-06-18 um optionale Felder fuer `SupplierName` und `Mstae` erweitert.
- Tests auf `Data`, `Data2`, `DataSet` und `Data2Set` liefern aktuell `404 Resource not found`.
- Bis diese Mapping-Quelle angebunden ist, verwendet das Dashboard vorhandene Lieferantennamen aus Payload bzw. Cache. Fehlt der Name, bleibt die Lieferantennummer sichtbar; es werden keine erfundenen Lieferantenlabels verwendet.

## Nachtrag 2026-06-18 Excel-Matrix und Einkaufsfilter

Umgesetzt:

- Neue Matrix `Kaskadierung Lieferant / Jahr` in der Einkaufssicht.
- Jahresachse aus den tatsaechlichen Spend-Jahren, im Standard 2020 bis aktuelles Jahr.
- Lieferanten werden Top-down nach Gesamt-Spend sortiert.
- Aktuelles Jahr: Spend pro Lieferant als separate Analyse.
- Gebuchter/beschaffter Wert und offener Zulauf werden in der Uebersicht getrennt dargestellt.
- Standardfilter fuer `LOEKZ` und vorbereiteter Filter fuer `MARA-MSTAE`.
- Lieferantennamen werden aus dem echten Einkaufsdaten-Payload gelesen, sofern SAP/OData sie liefert.
- Schema-Maintenance ergaenzt fehlende Cache-Spalten automatisch:
  - `PurchasingEkkoCache.SupplierName`
  - `PurchasingEkpoCache.Mstae`

Validierung:

- Testlauf: `dotnet test TrafagSalesExporter.sln --verbosity minimal`
- Ergebnis: `101/101` Tests gruen.
- Commit: `4f45805 Improve purchasing dashboard matrix`.

Deploy:

- Publiziert am 2026-06-18 auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- `app_offline.htm` wurde fuer den Publish gesetzt und danach entfernt.
- Produktive Datei: `BiDashboard.dll`, Zeitstempel `18.06.2026 09:29:11`.
- Servercheck: Port 443 erreichbar, `app_offline.htm` nicht mehr vorhanden.

## Nachtrag 2026-06-19 MARA-MSTAE Loeschkennzeichen

Ausgangslage:

- Das Loeschkennzeichen sollte fuer das Einkaufs-Cockpit ueber `MARA-MSTAE = 98` oder `99` ausgewertet werden.
- Frueher war `MARA-MSTAE` ueber OData nicht erreichbar (`Data/Data2/DataSet/Data2Set -> 404`); der Schalter `MARA-MSTAE raus` war daher wirkungslos.
- Neu: MARA ist ueber das OData-EntitySet `MARA001Set` verfuegbar (Felder `Matnr`, `Mstae`).

Umgesetzt:

- `PurchasingDataRefreshService` laedt `MARA001Set` (`Matnr,Mstae`) bei Full Load und Delta in eine Status-Map.
- Beim EKPO-Upsert wird `Mstae` ueber den normalisierten Join `EKPO.Matnr -> MARA.Matnr` aufgeloest und in `PurchasingEkpoCache.Mstae` geschrieben.
- Matnr-Normalisierung: Whitespace entfernen, `ToUpperInvariant`, fuehrende Nullen entfernen. Damit matcht SAP-18-stellig mit fuehrenden Nullen gegen lokale Nummern.
- Filterlogik in `PurchasingDashboardService.ActiveItemFilterSql`: `ExcludeDeletedItems` schliesst jetzt `EKPO.Loekz <> ''` ODER `Mstae in ('98','99')` aus.
- Der bisher separate, wirkungslose Schalter `ExcludeBlockedMaterials` wurde mit dem Loeschkennzeichen zusammengelegt und aus `PurchasingDashboardFilter`, Filter-SQL und Razor-UI entfernt.
- UI: eine Checkbox `Loeschkennzeichen raus (inkl. MARA-MSTAE 98/99)`; Statuszeile entsprechend angepasst.
- Datenquellen-Pflege ergaenzt um Quelle `MARA -> MARA001Set`, Join `EKPO.Matnr = MARA.Matnr` und Mapping `MaterialStatus -> MARA.Mstae` in `DatabaseSeedService` und `PurchasingDataSourcePageService`.

Wichtig:

- Die Quellen-Defaults werden nur fuer eine leere Quellenliste geseedet; die produktive DB behaelt ihre bestehenden Quellen. Der Filter funktioniert trotzdem, weil der Refresh-Service `MARA001Set` fest laedt.
- Damit `Mstae` real gefuellt ist, muss nach dem Deploy ein Einkauf-Full-Load oder Delta laufen.

Validierung:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal`
- Ergebnis: `103/103` Tests gruen, inkl. neuem `PurchasingDashboardServiceTests` (Filter aktiv/inaktiv).

## Ideen und Kennzahlen-Katalog

Der Ideenbereich wurde fuer den Einkauf erweitert:

- Lieferantenrisiko.
- Preisentwicklung CHF.
- Maverick Buying.
- Rahmenvertragsnutzung.
- Working Capital.
- Datenqualitaet.
- Liefertermin-Risiko.
- Spend-Konzentration.
- Savings Tracker.
- Bestellrhythmus.

Stand nach Ausbau: Unter `/einkauf/ideen` ist jede Idee als aufklappbarer Baustein beschrieben. Pro Idee sind Ziel, Datenbasis, Kennzahlen, Berechnungslogik, Visualisierung und naechster Umsetzungsschritt hinterlegt.

Der separate Kennzahlen-Katalog enthaelt nun konkrete Ausbau-KPIs mit Dimension und Datenbasis, darunter:

- Spend CHF.
- Top-10-Lieferantenanteil.
- Risiko-Score 0-100.
- Min. Netto-Stueckpreis nach Artikel und Jahr.
- Preisentwicklung analog PowerBI.
- Anteil ausserhalb Vertrag.
- Abrufquote.
- Ueberfaelliger offener Wert.
- Offene Menge faellig in 30 Tagen.
- Cash Forecast.
- Kleinstbestellungen.
- Realisierte Einsparung.
- Mapping-Abdeckung.
- Fehlende Warengruppe / fehlender Artikeltext.

## 3D Simulation

Das Einkaufsdashboard hat eine eigene 3D-Simulation fuer wichtige Einkaufsindikatoren:

- Spend CHF.
- Offener Bestellwert.
- Offene Menge.
- Kontrakt-Restwert.
- Lieferantenperformance.

Die Simulation nutzt feste Canvas-Groessen, sichtbare Achsen, waehlbare Diagrammarten, Labelgroesse und einen Szenario-Slider fuer Preis-/Wechselkurswirkung.

## Naechster Schritt fuer Live-Daten

Die technische Vollbasis ist geladen. Fuer fachlich finale Management-Sichten muessen noch diese Abgrenzungen abgestimmt werden:

- Mapping-Quelle fuer Lieferantennamen, Region und Warengruppentexte final bereitstellen oder als eigene Cache-Tabelle laden. Falls `SupplierName` und `Mstae` nicht im bestehenden OData-Payload kommen, muessen Data/LFA1/MARA-Quelle und EntitySet-Namen fachlich/technisch geklaert werden.
- PowerBI-Zielwerte mit Marco/Finanzen anhand eines konkreten Monats und Lieferanten gegenpruefen.
- Kontrakte und offene Verpflichtungen, inkl. fachlicher Abgrenzung von normalen Bestellungen und Umlagerungen.
- Lieferantenbewertung / Performance, falls im SAP-System als OData- oder HANA-Quelle verfuegbar.

Der Delta-/Refresh-Prozess ist technisch vorbereitet und im Dashboard unter `Einkauf > Ideen > Einkauf-Datenservice` bedienbar.

## Server-Restore und Full Load 2026-06-08

Beim Publish wurde frueher die Runtime-Datei `trafag_exporter.db` mitpubliziert. Dadurch war die Server-DB zeitweise wieder leer. Das ist im Projektfile korrigiert: `trafag_exporter.db`, `trafag_exporter.db-wal` und `trafag_exporter.db-shm` werden nicht mehr in das Publish-Paket kopiert.

Wiederherstellung am Server:

- Server-DB zuerst aus der lokalen Haupt-DB wiederhergestellt, damit Finance-Daten, Navigation und SAP-Credentials wieder vorhanden sind.
- Backup vor Restore:
  - `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-restore-20260605-144709.bak`
- Danach Einkauf-Full-Load nicht direkt ueber die UNC-Server-DB ausgefuehrt, sondern lokal gegen eine DB-Kopie:
  - Arbeitsordner: `C:\TMP\purchasing-fullload-20260607-205623`
  - Grund: langer SAP-Abruf plus SQLite ueber UNC ist fragil.
- Lokaler Full Load erfolgreich abgeschlossen:
  - `PurchasingEkkoCache`: 172'874
  - `PurchasingEkpoCache`: 233'921
  - `PurchasingEketCache`: 242'572
- Die fertig geladene DB wurde anschliessend auf den Server kopiert.
- Backup vor dem Zurueckkopieren der Full-Load-DB:
  - `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-purchasing-fullload-20260608-061149.bak`

Wichtiger Fix nach dem Kopieren:

- Auf dem Server lagen noch alte SQLite-Sidecar-Dateien neben der neuen Haupt-DB:
  - `trafag_exporter.db-wal`
  - `trafag_exporter.db-shm`
- Diese passten nicht mehr zur neuen Hauptdatei und verursachten beim App-Start `SQLite Error 11: database disk image is malformed`.
- Beide Sidecar-Dateien wurden gesichert und entfernt:
  - `trafag_exporter.db-wal.before-cleanup-20260608-065012.bak`
  - `trafag_exporter.db-shm.before-cleanup-20260608-065012.bak`

Verifizierter Serverstand nach Cleanup:

- HTTP-Check `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/`: Status 200.
- Server-DB:
  - `SourceSystemDefinitions`: 5
  - `Sites`: 9
  - `SapSourceDefinitions`: 8
  - `SapJoinDefinitions`: 5
  - `SapFieldMappings`: 47
  - `NavigationMenuItems`: 47
  - `CentralSalesRecords`: 75'089
  - `PurchasingEkkoCache`: 172'874
  - `PurchasingEkpoCache`: 233'921
  - `PurchasingEketCache`: 242'572
  - SAP-Credentials vorhanden.
  - Neueste EKKO-Bestelldaten: `2026-06-05`.
  - Neueste EKET-Einteilung: `2027-04-20`.

Empfehlung fuer kuenftige grosse Einkauf-Ladevorgaenge:

- Full Load immer lokal gegen eine Kopie der produktiven DB ausfuehren.
- Erst nach erfolgreichem Abschluss die fertige DB auf den Server kopieren.
- Beim Ersetzen der SQLite-Hauptdatei immer `trafag_exporter.db-wal` und `trafag_exporter.db-shm` passend mitsichern/entfernen.
- Danach HTTP-Start und Cache-Counts pruefen.

## Geaenderte Programmstellen

- `Components/Pages/PurchasingDashboard.razor`
  - KPI-Karten, Detailtabellen und Diagramme lesen jetzt Live-Werte aus `PurchasingDashboardLiveState`.
  - Fallback-Simulation bleibt sichtbar, falls SAP/OData nicht antwortet.
  - Die alten Tabs wurden in routenbasierte Seiten unter `/einkauf/...` umgebaut.
  - Ideen und Kennzahlen-Katalog sind getrennte Seiten.
- `Services/DatabaseSeedService.cs`
  - Neue Einkaufs-Unterpunkte werden in `NavigationMenuItems` geseedet.
  - Admins koennen die Unterpunkte ueber die Menuestruktur ausblenden, sortieren oder umhaengen.
- `Services/IPurchasingDashboardService.cs`
  - Live-State um Spend, offene Menge, offenen Wert, Kontraktwert und Live-Diagrammzeilen erweitert.
  - Seit 2026-06-18: Live-State um Jahresachsen, Lieferant/Jahr-Matrix und Spend aktuelles Jahr je Lieferant erweitert.
- `Services/PurchasingDashboardService.cs`
  - Liest EKKO, EKPO und EKET aus dem Einkauf-Cache und nutzt SAP-Live nur als Fallback.
  - Berechnet Spend aus EKPO.
  - Berechnet offene Mengen/Werte aus EKET minus Wareneingangsmenge, bewertet mit EKPO-Netto-Stueckwert.
  - Erstellt Top-Gruppierungen fuer Lieferant, Warengruppe und Artikel.
  - Seit 2026-06-18: filtert geloeschte Positionen und optional Materialstatus, erzeugt die Lieferant/Jahr-Matrix und vermeidet kuenstliche Lieferanten-Platzhalter.
- `Services/PurchasingDataRefreshService.cs`
  - Fuehrt Full Load und Delta-Refresh fuer EKKO/EKPO/EKET aus.
  - Beruecksichtigt das SAP-Seitenlimit von 1'000 Zeilen.
  - Seit 2026-06-18: schreibt optionale Payload-Felder fuer Lieferantennamen und `Mstae`, falls SAP/OData sie liefert.
- `Services/DatabaseInitializationService.SchemaSql.cs`
  - Erstellt `PurchasingEkkoCache`, `PurchasingEkpoCache`, `PurchasingEketCache` und `PurchasingSyncState`.
  - Seit 2026-06-18: Schema kennt `SupplierName` in `PurchasingEkkoCache` und `Mstae` in `PurchasingEkpoCache`; bestehende Datenbanken werden ueber Schema-Maintenance ergaenzt.
