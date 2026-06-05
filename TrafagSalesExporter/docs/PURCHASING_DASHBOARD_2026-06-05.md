# Einkaufsdashboard 2026-06-05

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
- Die Seite ist als Cockpit-Struktur umgesetzt und zweisprachig ueber den vorhandenen UI-Sprachservice vorbereitet.
- EKKO, EKPO und EKET werden per SAP/OData in lokale Cache-Tabellen geladen.
- Das Cockpit liest zuerst den Cache und nutzt nur noch als Fallback eine begrenzte Live-Probe, falls noch kein Cache vorhanden ist.

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
- Spend-, Offenwert- und Kontrakt-Diagramme verwenden Cache-Gruppierungen, sofern der Cache gefuellt ist.
- Ist der Cache leer oder nicht erreichbar, faellt das Dashboard auf eine begrenzte SAP-Live-Probe zurueck.
- Der Standardzeitraum ist rollierend auf die letzten drei Kalenderjahre bis heute gesetzt. Die Datumsabgrenzung erfolgt im Dashboard ueber `Von Monat` und `Bis Monat`.

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
- Der aktuelle SAP-OData-Service liefert produktiv `EKKOSet`, `EKPOSet` und `eketSet`.
- Tests auf `Data`, `Data2`, `DataSet` und `Data2Set` liefern aktuell `404 Resource not found`.
- Bis diese Mapping-Quelle angebunden ist, zeigt das Dashboard Lieferantennummern und Warengruppen-Codes statt vollstaendiger Namen.

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

- Mapping-Quelle fuer Lieferantennamen, Region und Warengruppentexte bereitstellen oder als eigene Cache-Tabelle laden.
- PowerBI-Zielwerte mit Marco/Finanzen anhand eines konkreten Monats und Lieferanten gegenpruefen.
- Kontrakte und offene Verpflichtungen, inkl. fachlicher Abgrenzung von normalen Bestellungen und Umlagerungen.
- Lieferantenbewertung / Performance, falls im SAP-System als OData- oder HANA-Quelle verfuegbar.

Der Delta-/Refresh-Prozess ist technisch vorbereitet und im Dashboard unter `Einkauf > Ideen > Einkauf-Datenservice` bedienbar.

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
- `Services/PurchasingDashboardService.cs`
  - Liest EKKO, EKPO und EKET aus dem Einkauf-Cache und nutzt SAP-Live nur als Fallback.
  - Berechnet Spend aus EKPO.
  - Berechnet offene Mengen/Werte aus EKET minus Wareneingangsmenge, bewertet mit EKPO-Netto-Stueckwert.
  - Erstellt Top-Gruppierungen fuer Lieferant, Warengruppe und Artikel.
- `Services/PurchasingDataRefreshService.cs`
  - Fuehrt Full Load und Delta-Refresh fuer EKKO/EKPO/EKET aus.
  - Beruecksichtigt das SAP-Seitenlimit von 1'000 Zeilen.
- `Services/DatabaseInitializationService.SchemaSql.cs`
  - Erstellt `PurchasingEkkoCache`, `PurchasingEkpoCache`, `PurchasingEketCache` und `PurchasingSyncState`.
