# Logistik-Stuecklisten-Dashboard

Stand: 2026-08-01

Ergaenzung 2026-08-06: Die bestehende Stuecklistenanalyse bleibt unveraendert.
Zwei getrennte, additive Logistik-Reiter fuer `Materialdisposition & Fehlteile`
und `Dispositionspruefung` verwenden denselben Cache nur lesend. Details:
`docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md`.

## Zweck und aktueller Umfang

Die Seite `Logistik > Stuecklistenanalyse` stellt den zuletzt geladenen
`MaterialUsageCache` nicht mehr nur als Rohdatentabelle dar. Sie bietet eine
richtungsabhaengige Uebersicht fuer die aufgeloeste Stueckliste:

- Top-Down: Kopfmaterial zu Komponenten.
- Bottom-Up: Komponente zu verwendenden Elternmaterialien.
- Vier Kennzahlen je Richtung.
- Top-12-Balken nach Verwendungsbreite.
- Bestandslage der unterschiedlichen Komponenten.
- Verteilung der LZ-Codes.
- Weiterhin die durchsuchbare Detailtabelle mit maximal 200 Zeilen.

Die Kennzahlen und Diagramme werden aus dem gesamten gefilterten Cache
berechnet. Die Begrenzung auf 200 Datensaetze betrifft ausschliesslich die
Detailtabelle.

## Fachliche Lesart

### Top-Down

Die Kennzahlen zeigen Kopfmaterialien, unterschiedliche Komponenten,
Kopf-Komponenten-Beziehungen und als exklusiv gelieferte Komponenten. Die
Top-12-Liste beantwortet, welche Kopfmaterialien die groesste Teilebreite
besitzen. Exklusivitaet zusammen mit fehlendem oder negativem Endbestand ist
ein sinnvoller Einstieg fuer die Risikopruefung.

### Bottom-Up

Die Kennzahlen zeigen geladene Komponenten, gefundene Elternmaterialien sowie
mehrfach und nur einmal verwendete Komponenten. Die Top-12-Liste beantwortet,
welche Komponenten in den meisten unterschiedlichen Elternmaterialien
vorkommen. Mehrfach verwendete Teile koennen eine breite Auswirkung haben;
nur einmal verwendete Teile koennen auf spezifische Abhaengigkeiten hinweisen.

`Mehrfach verwendet` und `nur einmal verwendet` beziehen sich auf den aktuell
geladenen, optional gefilterten Cache und sind keine Aussage ueber nicht
geladene SAP-Daten.

## Bestands- und LZ-Auswertung

Jede unterschiedliche Komponente wird genau einer Bestandsklasse zugeordnet:

- positiver Endbestand,
- Endbestand null,
- negativer Endbestand,
- Bestand nicht geliefert.

Bestandswerte werden bewusst nicht ueber Stuecklistenbeziehungen summiert.
Eine gemeinsam verwendete Komponente koennte sonst mehrfach gezaehlt und damit
irrefuehrend bewertet werden. Auch eine aggregierte Bestandsbewertung in CHF
wird aus diesem Grund nicht angezeigt.

Die LZ-Code-Verteilung zaehlt unterschiedliche Komponenten je Code. Ein leerer
LZ-Code wird als `Ohne Code` dargestellt. Die Verteilung ist eine operative
Segmentierung, ersetzt aber keine fachliche Interpretation des jeweiligen
LZ-Codes.

## Filter und Datenaktualisierung

- Der Umschalter trennt Top-Down und Bottom-Up konsequent in Analyse und
  Detailtabelle; Daten der Gegenrichtung werden nicht beigemischt.
- Die Suche filtert per Teilstring auf Kopfmaterial- oder Komponentennummer.
- Ein SAP-Load ersetzt den Cache der gewaehlten Richtung vollstaendig. Ein
  Delta-Load existiert nicht, weil die Quelle kein belastbares
  Aenderungsdatum liefert.
- Bei historischen Materialien kann `Auch geloeschte Materialien` fuer eine
  Top-Down-Suche erforderlich sein.
- Die Anzeige nutzt die vom SAP-Report bereits aufgeloesten Daten als flache
  Kopf-Komponenten-Paare. Ein interaktiv rekursiver Baum ueber mehrere
  Baugruppenebenen ist nicht Teil dieser Ansicht.

## Direkt gepruefter Produktiv-Snapshot

Die folgenden Werte wurden am 2026-08-01 lesend aus dem zu diesem Zeitpunkt
vorhandenen Live-Cache geprueft. Sie sind ein zeitgebundener Snapshot und keine
dauerhaft festgeschriebenen Sollwerte:

| Richtung | Beziehungen | Kopf-/Elternmaterialien | Komponenten |
| --- | ---: | ---: | ---: |
| Top-Down | 61 | 1 | 61 |
| Bottom-Up | 85 | 85 | 4 |

Im Bottom-Up-Snapshot war die groesste Verwendungsbreite 36
Elternmaterialien. Die vier geladenen Komponenten kamen in 36, 27, 15 und 7
unterschiedlichen Elternmaterialien vor. Der Top-Down-Snapshot enthielt 56
Komponenten mit positivem und 5 mit Endbestand null; negative Bestaende wurden
in diesem Snapshot nicht gefunden. Diese Zahlen koennen sich mit jedem neuen
SAP-Load aendern.

## Technik und Qualitaetssicherung

- UI: `Components/Pages/BomAnalysis.razor`
- Cache-Auswertung: `MaterialUsageDataRefreshService.GetCachedAnalysisAsync`
- Datenmodell: `MaterialUsageAnalysisResult`
- Sprachen: Deutsch, Englisch, Spanisch, Italienisch, Hindi, Albanisch,
  Tuerkisch und Klingonisch; Klingonisch verwendet bewusst Umschreibungen fuer
  fehlende Fachwoerter.
- Automatisierte Tests: Aggregation beider Richtungen, Richtungsfilter,
  Bestandsklassen, Top-Gruppen, LZ-Codes und vollstaendige Uebersetzungskeys.
- Verifikation am 2026-08-01: 353 von 353 Release-Tests bestanden.

Die technische SAP-/Gateway-Basis und deren noch offene Betriebsfragen stehen
in `docs/abap/README_LZCODE_WEBSERVICE.md`.

## Commit und produktiver Deploy

- Feature-Commit: `9e28086 Add logistics BOM dashboard`.
- Produktiv veroeffentlicht am 2026-08-03 auf
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$`.
- Release-Pruefung: 353 von 353 Tests bestanden.
- Produktive `BiDashboard.dll`: `4'024'832` Bytes, Zeitstempel
  `03.08.2026 06:59:38`, SHA256
  `8D5586E5536C83A9EDB409472C332D190488898C3FE8E8DB2097C3131779B554`.
- Lokales Release und Server-DLL sind bitgleich.
- Die Produktivdatenbank blieb in Laenge (`338'419'712` Bytes), Schreibzeit
  (`01.08.2026 12:25:11`) und SHA256
  (`B23249F54F4667332FAE7A9A270EE7E10765656D2DEFA184DA9F015C2B87BE94`)
  unveraendert.
- `app_offline.htm` ist entfernt, Port 443 ist erreichbar und der
  authentifizierte Aufruf von
  `/BiDashboard/logistik/stuecklistenanalyse` liefert HTTP `200`.
