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
  - `Ideen`: Roadmap fuer weitere Einkaufsanalysen.
  - `Kennzahlen-Katalog`: fachlicher KPI-Katalog fuer den naechsten Ausbau.
  - `PBIX Vorlage`: aus `x.pbix` uebernommene Seiten/Visuals.
  - `3D Simulation`: drehbare 3D-What-if-Analyse.
- Unterpunkt `Einkauf > Datenquellen` fuer SAP/OData-Verbindung, Quellen, Join-Fluss und Zielmappings.
- Die Seite ist als Cockpit-Struktur umgesetzt und zweisprachig ueber den vorhandenen UI-Sprachservice vorbereitet.
- EKKO, EKPO und EKET werden live ueber SAP/OData gelesen.
- Die Kennzahlen im Cockpit nutzen aktuell eine begrenzte Live-Probe, damit das Dashboard sofort echte Einkaufsdaten zeigt.

## Navigation und Admin-Steuerung

Stand 2026-06-05: Die Einkaufsbereiche sind nicht mehr als obere Tabs im Dashboard versteckt, sondern als eigene URLs umgesetzt:

- `/einkauf`
- `/einkauf/spend`
- `/einkauf/offene-bestellungen`
- `/einkauf/kontrakte`
- `/einkauf/lieferanten`
- `/einkauf/ideen`
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

## Live-Kennzahlen im Dashboard

Die Seite `/einkauf` zeigt nun echte Werte aus SAP:

- `Spend total`: Summe `EKPOSet.Netwr` aus der Live-Probe.
- `Offene Bestellungen`: Anzahl EKKO-Belege seit Jahresbeginn.
- `Kontrakte`: offener Restwert aus `EKET.Menge - EKET.Wemng` bewertet mit EKPO-Netto-Stueckwert.
- `Offener Bestellwert`: berechnet aus EKET-Offenmenge und EKPO-Netto-Stueckwert.
- `Offene Menge`: Summe offener EKET-Mengen.
- Top-Lieferant, Top-Warengruppe und Top-Artikel werden aus EKPO gruppiert.
- Spend-, Offenwert- und Kontrakt-Diagramme verwenden Live-Gruppierungen, sofern EKPO/EKET Daten liefern.

Aktuelle technische Begrenzung:

- Das Dashboard laedt fuer EKPO/EKET eine begrenzte Probe mit `$top=1000`.
- Filter ist `Ebeln ge <erste aktuelle EKKO-Bestellnummer>`.
- Damit sind die Werte echte SAP-Werte, aber noch keine vollstaendige Jahresaggregation.
- Fuer definitive Management-Summen braucht es als naechsten Schritt serverseitige OData-Filter/Aggregation oder einen eigenen Import-/Cache-Prozess analog Finance.

## Ideen und Kennzahlen-Katalog

Der Ideenbereich wurde fuer den Einkauf erweitert:

- Lieferantenrisiko.
- Preisabweichung.
- Maverick Buying.
- Rahmenvertragsnutzung.
- Working Capital.
- Datenqualitaet.
- Liefertermin-Risiko.
- Spend-Konzentration.
- Savings Tracker.
- Bestellrhythmus.

Der separate Kennzahlen-Katalog enthaelt nun konkrete Ausbau-KPIs mit Dimension und Datenbasis, darunter:

- Spend CHF.
- Top-10-Lieferantenanteil.
- Risiko-Score 0-100.
- Preisdelta in Prozent und CHF.
- Letzter Preis vs. Vorjahr.
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

Fuer definitive Vollwerte muessen die Live-Quellen noch fachlich fertig aggregiert werden:

- Jahres-/Periodenfilter fuer `EKKOSet.Bedat`.
- Vollstaendige Aggregation von `EKPOSet.Netwr` nach Jahr, Lieferant, Warengruppe und Artikel.
- Vollstaendige offene Werte/Mengen aus `EKET` und `EKPO`.
- Kontrakte und offene Verpflichtungen, inkl. fachlicher Abgrenzung von normalen Bestellungen.
- Lieferantenbewertung / Performance, falls im SAP-System als OData- oder HANA-Quelle verfuegbar.

Danach koennen Filter, Aggregationen und Delta-/Refresh-Prozess analog zu Finance/Spain umgesetzt werden.

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
  - Laedt EKKO, EKPO und EKET.
  - Berechnet Spend aus EKPO.
  - Berechnet offene Mengen/Werte aus EKET minus Wareneingangsmenge, bewertet mit EKPO-Netto-Stueckwert.
  - Erstellt Top-Gruppierungen fuer Lieferant, Warengruppe und Artikel.
