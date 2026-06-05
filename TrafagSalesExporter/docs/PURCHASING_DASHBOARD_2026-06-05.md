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
  - `Uebersicht`
  - `Spend`
  - `Offene Bestellungen`
  - `Kontrakte`
  - `Lieferanten`
  - `PBIX Vorlage`
  - `3D Simulation`
- Unterpunkt `Einkauf > Datenquellen` fuer SAP/OData-Verbindung, Quellen, Join-Fluss und Zielmappings.
- Die Seite ist als Cockpit-Struktur umgesetzt und zweisprachig ueber den vorhandenen UI-Sprachservice vorbereitet.
- Die Kennzahlen sind noch nicht live an SAP gebunden.

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

## 3D Simulation

Das Einkaufsdashboard hat eine eigene 3D-Simulation fuer wichtige Einkaufsindikatoren:

- Spend CHF.
- Offener Bestellwert.
- Offene Menge.
- Kontrakt-Restwert.
- Lieferantenperformance.

Die Simulation nutzt feste Canvas-Groessen, sichtbare Achsen, waehlbare Diagrammarten, Labelgroesse und einen Szenario-Slider fuer Preis-/Wechselkurswirkung.

## Naechster Schritt fuer Live-Daten

Fuer echte Werte muessen die Einkaufsquellen sauber gemappt werden:

- Bestellkopf, z. B. `EKKOSet`.
- Bestellpositionen, z. B. `EKPOSet`.
- Offene Liefer-/Terminmengen, voraussichtlich Termin-/Schedule-Daten.
- Kontrakte und offene Verpflichtungen.
- Lieferantenbewertung / Performance, falls im SAP-System als OData- oder HANA-Quelle verfuegbar.

Danach koennen Filter, Aggregationen und Delta-/Refresh-Prozess analog zu Finance/Spain umgesetzt werden.
