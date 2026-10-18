# Einkaufs-Dashboard: Anforderungen, Korrekturen und offene Punkte

Stand: 2026-08-17. Zusammengefuehrt aus sechs Sitzungs- und Review-Dateien
(Formel-Review 2026-07-06, Umsetzungsplan und Vorbereitung 2026-07-09, Review Marco
2026-07-10, Einkaufssitzungen 2026-07-23 und 2026-07-30).

Gegliedert nach **umgesetzt / offen / zurueckgestellt**, nicht nach Sitzungsdatum.
Beteiligte: Marco Widmer (Einkaufs-Koordinator), Armin, Ingo.

Laufende Hauptdoku: `docs/PURCHASING_DASHBOARD_2026-06-05.md`.

## 1. Fachliche Abnahme

Der Reiter **Spend** ist am 2026-07-30 fachlich abgenommen. Marco hat live am
**Produktivsystem** gegengerechnet:

| Kennzahl | Wert | Reaktion |
| --- | --- | --- |
| Spend total | 21 Mio. | — |
| Bestellungen im Zeitraum | 11 Mio. | „ja, genau" |
| Top-Warengruppe | 3.7 Mio. | „das koennte sehr stimmen" |

Marcos Leitplanke „eine Sicht nach der anderen" ist damit fuer Spend erfuellt.

## 2. Die SAP-Semantikfallen (dauerhaft gueltig)

Diese vier Punkte sind der bleibende Wert des Formel-Reviews. Sie sind behoben, aber die
Semantik gilt weiter und ist bei jeder Erweiterung zu beachten.

| Feld | Falsche Annahme | Tatsaechliche Bedeutung |
| --- | --- | --- |
| `EKKO.AEDAT` | Aenderungsdatum | **Anlagedatum.** Ein Wareneingang aendert `EKET.WEMNG`, aber nicht `AEDAT`. Ein Delta ueber `AEDAT` verpasst deshalb alle Wareneingaenge auf aelteren Belegen, und offene Werte veralten wachsend |
| `EKPO.NETWR` | CHF | **Belegwaehrung** (`EKKO.WAERS`). Ungerechnet addiert ergibt das eine Summe aus CHF, EUR und USD, die als CHF ausgewiesen wird |
| `EKET.EINDT` | Ist-Lieferdatum | **geplantes** Lieferdatum. Ein Wareneingangs-Zeitbezug braucht `EKBE`/`MSEG` als zusaetzliche Quelle |
| Zeitraum-Obergrenze `heute` | harmlos | schneidet **allen zukuenftigen Zulauf** ab. Offener Bestellwert zeigt dann nur den Rueckstand, und die Risiko-Buckets `0-7 Tage`/`8-30 Tage`/`Spaeter` koennen nie befuellt werden |

Konsequenz im Code: Der Zeitraumfilter wirkt nur auf Vergangenheits-KPIs ueber `Bedat`
(Spend, Bestellanzahl). Offene Werte, Mengen, Zulauf und Risiko laufen auf einer **eigenen**
Periode ohne Obergrenze auf heute.

## 3. Umgesetzt

### Formel- und Logikkorrekturen (2026-07-06, alle umgesetzt)

| ID | Punkt |
| --- | --- |
| K1 | Waehrungsumrechnung: `Waers`/`Wkurs` als echte Spalten in `PurchasingEkkoCache` statt nur im `RawJson` |
| K2 | Delta laedt zusaetzlich alle Belege nach, die im Cache noch offene Mengen haben, statt sich auf `AEDAT` zu verlassen |
| K3 | Eigene Periode fuer offene Werte und Risiko, keine Obergrenze auf heute |
| K4 | Kontrakte/Restverpflichtung ist keine Kopie des offenen Bestellwerts mehr |
| K5 | KPI-Label vereinheitlicht: „Bestellungen im Zeitraum" statt „Offene Bestellungen" |
| K6 | Jahresachse nicht mehr hart auf 2026 begrenzt |
| M7 | Endlieferungskennzeichen `ELIKZ` wirkt in der Offen-Logik |
| M8 | Offene Menge mit demselben Positionsfilter wie der offene Wert |
| M9 | Preisentwicklungs-Chart nicht mehr Minimum ueber alle Artikel |

### Aus dem Review Marco (2026-07-10)

- „Verpflichtungen" ist Stand-heute und zeitraumunabhaengig.
- Loeschkennzeichen wirkt nicht mehr auf den historischen Spend.
- Register `Lieferanten` reagiert auf den Zeitraum.
- Kachel-Beschriebe fuer EKPO/EKET praezisiert.

### Aus den Einkaufssitzungen (2026-07-23 und 2026-07-30)

| Punkt | Bemerkung |
| --- | --- |
| Reiter `Spend-Aufriss` mit Perspektiven | 5 Perspektiven, Drilldown statt fester Hierarchie |
| Dritte Ebene direkt in der Spend-Matrix | Entscheid Marco gegen den Verweis auf einen eigenen Reiter |
| Spend nach Waehrung | eigener Balkenblock, CHF-bewertet plus Originalsumme |
| Waehlbare Einstiegsdimension | beantwortet die offene Frage vom 2026-07-24 |
| Delta klassifiziert den ganzen Cache | **Nachpflege wirkt jetzt ohne Full Load** |
| Warengruppen-Texte | `PurchasingMaterialGroupTextCatalog` loest rund 72 T023T-Codes zu „Code - Text" auf; unbekannte Codes bleiben roher Code |
| ZLO03 Excel-Paste | Komma, Semikolon, Leerzeichen, Tab, Zeilenumbruch; Duplikate raus |
| ZLO03 Mehrfachabfrage | **Bypass**: eine SAP-Anfrage je Nummer statt OR-Gruppe |
| Produktgruppen-Aufriss | `Produktgruppe -> Lieferant -> Material` ueber ZLO03-Disponent, mit summenerhaltender `1/n`-Allokation bei Mehrfachverwendung |
| ABC/XYZ-Massnahmenmatrix | mit konkretem Pruefauftrag je Klasse |

Produktivstand 2026-08-06 13:57 MESZ, Funktionscommit `0a8a4c9`, `435/435` Tests.
Vollstaendige Entscheidung und Abnahmegrenzen fuer Produktgruppen und ABC/XYZ:
`docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

Der Full Load vom 2026-07-24 lieferte: `SupplierCountry` 100 % gefuellt, `MaraAbc` 78 %,
`MaraXyz` 65 %, `MaraMatkl` 80,7 %.

## 4. Offen

| # | Punkt | Aufwand | Warum |
| --- | --- | --- | --- |
| 1 | **Laeuft das naechtliche Delta?** `MAX(Bedat)` gegen den Tag des letzten Full Loads pruefen | Minuten | Greift das Delta nicht, stehen alle Zahlen auf dem alten Stand — das schlaegt jeden anderen Punkt |
| 2 | Delta-Meldung im Log ansehen: Laufzeit und Zahl nachklassifizierter Cachezeilen | Minuten | die einzige ungemessene Groesse am Delta-Fix |
| 3 | ZLO03 mit mehreren Nummern gegen P76 laufen lassen | Minuten | Beweis, dass der Bypass greift. Die ABAP-Ursache ist **nicht** verifiziert |
| 4 | ZLO03-Plausibilisierung Bottom-Up abschliessen | mittel | Vergleich SAP gegen Web ist begonnen, nicht beendet. Solange Marco zweifelt, nutzt er das Werkzeug nicht |
| 5 | Wareneingangsdatum: echter Bedarf oder Randbemerkung? | gross | Braucht `EKBE`/`MSEG` als neue SAP-Quelle. **Nicht ohne explizite Priorisierung anfangen** |
| 6 | SAP-Textpflege fuer die Disponentencodes `D1` und `D5` | klein | `D5` hat in der gelieferten Textdatei keine Beschreibung |
| 7 | Power-BI-Basisliste: Authentifizierung defekt | offen | nur auf Marcos Zuruf, er hat nicht darauf bestanden |

### Warum ZLO03 Bottom-Up dringend ist

Marco hat mit Ann-Kathrin einen konkreten Fall zur **TR5-Umstellung**: 1'563 Komponenten,
die in fuenf Jahren weniger als 100-mal gebraucht wurden, teils auf Status 98. Zwei Fragen:
zu welchen Verkaufsnummern gehoeren sie, und koennen diese Typen von der TR5-Migration
ausgenommen werden? Das ist genau der Bottom-Up-Verwendungsnachweis ueber eine groessere
Nummernliste. **Der Mehrfacheingabe-Bug ist damit kein Komfortthema, sondern blockiert eine
laufende Aufgabe.**

Fuer die Typidentifikation schlug Armin den **Disponenten** vor, weil Bezeichnungen allein
nicht reichen („Navitrack ist ein uraltes Produkt").

## 5. Zurueckgestellt

- **Sortiments- und Lebenszyklusgueter** — in Arbeit ueber ZLO03, keine neue Anforderung.
  Haengt an der abgeschlossenen Plausibilisierung.
- **Lieferanten-Factsheet und -Vergleich** („wie Galaxus") — aufgenommen, nicht terminiert.
- **Echte Mengenkontrakte** im Register Kontrakte — aufgenommen, nicht terminiert.
- **Termintreue-Kachel** — Prioritaet durch Marco bestaetigt, Umsetzung offen.

## 6. Argumentarium

Fuer das Dashboard braucht niemand eine Power-BI-Lizenz. Power BI bleibt nur fuer
Einzelanalysen ohne allgemeines Interesse noetig. In der Sitzung vom 2026-07-30 von Ingo
gebracht und unwidersprochen.

## Querverweise

- Hauptdoku und Formeln: `docs/PURCHASING_DASHBOARD_2026-06-05.md`
- Produktgruppen, ZC23, ABC/XYZ: `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`
- Produktgruppen direkt aus SAP OData: `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`
- Welche Indikatoren echt rechnen: `docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md`
- Supply-Chain-Reiter: `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md`
- ZLO03-Webservice: `docs/abap/README_LZCODE_WEBSERVICE.md`
