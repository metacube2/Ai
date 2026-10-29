# Logistik: Verwendung & Risiko (Stückliste über mehrere Stufen)

Stand: 2026-10-01, produktiv 16:05 (`1a754e0`). Seite `/logistik/verwendung-risiko`, Menü Logistik.

## Auftrag und Entscheid

Ingo: Was kann man bei der Stücklistenanalyse noch einbauen, was SAP nicht bietet
(Vererbung, Komplexität)? Dann: „mach die Dinge, wo die Daten reichen“. Nach der Messung unten
Entscheid Ingo: **jetzt mit der LZ-Code-Teilmenge**, statt zuerst ein vollständiges
Stücklisten-Set aus SAP zu bauen.

## Gemessen (Kopie der Produktiv-DB vom 01.10., nur lesend)

| Bestand | Inhalt |
| --- | --- |
| `MaterialUsageCache` | 85 Zeilen Bottom-Up, 1 Zeile Top-Down (Stand 02.09.); taugt nicht |
| `MaterialParentCache` | 25'279 Paare Komponente → Eltern (Stand 02.09.); nach Bereinigung 24'979: 4'487 Komponenten, 8'176 Eltern, 1'658 beides, bis 5 Stufen. **Nur Komponenten mit LZ-Code** (Quelle LZ-Code-Report) |
| Einkauf EKKO/EKPO/EKET | 16'282 Bestellpositionen der letzten 24 Monate zu diesen Komponenten |
| Umsatz | Über diese Daten erreichbar: CH 2025 **4,2 von 74 Mio. CHF (6 %)**, übrige Standorte nahe 0 % |

Folgerung: **kein Umsatz je Komponente und keine Gleichteil-/Komplexitätswerte**, weil sie mit 94 %
fehlendem Umsatz bzw. fehlenden Teilen je Produkt falsch wären. Machbar und korrekt (innerhalb der
Teilmenge): Wirkungsbreite, Tiefe, vererbtes Lieferantenrisiko.

## Was die Seite zeigt

- Hinweis oben: nur LZ-Code-Komponenten, deshalb kein Umsatz und keine Gleichteile.
- Kennzahlen: Komponenten, Endprodukte (oberste Stufe), Baugruppen dazwischen, grösste Tiefe,
  Komponenten mit nur einem Lieferanten, mit überfälliger Bestellung.
- **Komponenten mit der grössten Wirkung**: direkte Eltern, betroffene Endprodukte über alle Stufen,
  Tiefe, Lieferanten (letzte 24 Monate), Marken „eine Quelle“ und „überfällig“. Spitzenreiter am
  01.10.: `C18947` Skala-Grundkörper in 2'258 Endprodukten; `H10085` Rundmaterial 483 Endprodukte
  über 5 Stufen, nur ein Lieferant.
- **Verwendungsbaum nach oben** für die angeklickte Komponente (höchstens 4 Stufen, 25 je Stufe).
- **Endprodukte mit vererbtem Risiko**: Komponenten darunter mit nur einem Lieferanten oder überfällig.

## Regeln in der Rechnung (`BomInheritanceService`)

- Lieferant = `EKKO-LIFNR` aus Normalbestellungen (`BSTYP F`), nicht gelöscht, Bestelldatum in den
  letzten 24 Monaten. „Kein Einkauf“ heisst meist Eigenfertigung oder älter als 24 Monate.
- Überfällig = offene Einteilung (`EKET`, Wareneingang kleiner als Menge, Position nicht endgeliefert),
  **Liefertermin in den letzten 12 Monaten**. Ohne diese Grenze waren es 41'434 Einteilungen bis 2002
  zurück, fast nur nie geschlossene Altbestellungen; mit Grenze 2'065 Einteilungen, 306 Komponenten.
- Texte aus dem Einkauf, sonst aus den Schweizer Verkaufszeilen.
- Kreisschutz in den Daten; Ergebnis gemerkt bis sich `MaterialParentCache`, der Einkaufscache oder
  der Tag ändert. Rechnung rund 1 s, lesen rund 1 s.
- Tests `BomInheritanceTests` (Tiefe, Vorfahren, vererbtes Risiko, Kreis, führende Nullen).

## Später, wenn die vollständige Stückliste aus SAP kommt

Neues Set aus `MAST/STKO/STPO` (mehrstufig, nur lesend, mit Schutz wie Logistik live, eigener
Transport). Dann zusätzlich: Umsatz je Komponente, Komplexität und Gleichteile, kumulierte
Wiederbeschaffungszeit (kritischer Pfad), Kostenhochrechnung, Stoffvererbung (PPWR).

## Nachtrag 2026-10-16: Stuecklisten-Netz („Gehirn“) im Einkauf > Spend-Aufriss (Prototyp)

Wunsch Ingo: die Vererbungsbaeume wie Nervenbahnen eines Gehirns, Farbe je Code, Kreuzpunkte sichtbar. Entscheide: Farbe
umschaltbar (LZ-Code, Lieferantenrisiko, Stufe), Prototyp mit dieser Teilmenge, Ort zuoberst im Spend-Aufriss, WebGL fuer Edge.
*Ueberholt am 2026-10-22 (`77778a5`): Das Netz ist im Spend-Aufriss eingeklappt hinter „Stuecklisten-Netz (Prototyp)
anzeigen“ (Feedback Erich, Nachtrag 2026-10-22 in `docs/PURCHASING_DASHBOARD_2026-06-05.md`).*

- Daten: `Services/Logistics/BomBrainGraph.cs` aus `BomInheritanceService` plus `PurchasingMaterialLzCache` (Knoten mit Stufe,
  Risiko, LZ-Code, Verwendungen; Kanten Eltern -> Kind). Test `BomBrainGraphTests`.
- Darstellung: `Components/Pages/PurchasingBomBrain.razor`, `wwwroot/js/bombrain.js` mit three.js (schon im Cockpit). Ein
  Linienpuffer fuer alle Straenge, ein Punktpuffer fuer alle Knoten, feste Anordnung (Endprodukte auf Ellipsoid-Schale in zwei
  Haelften, Komponenten im Schwerpunkt ihrer Verwendungen nach innen). *Ueberholt:* erste Fassung mit `3d-force-graph` (ein Objekt
  je Strang) wieder entfernt.
- Live 16.10.2026 10:48 (`d6ca646`): 11'005 Materialien, 24'979 Straenge, 15 LZ-Codes; Klick auf C18947 zeigt 2'258 Verwendungen.
- Spaeter: ganze Stueckliste aus SAP (MAST/STKO/STPO), Produktsparte als vierte Farbgebung, Filter „nur Kreuzpunkte“.
