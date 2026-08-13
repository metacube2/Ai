# Marktsegmente und Marktumfrage in der Anwendung

Stand: 2026-08-13

Anlass: Patrik aus dem Vertrieb hat die Railway-Marktumfrage vom Mai 2026 geschickt mit dem
Wunsch, den Bahnumsatz im Sales-Dashboard auswerten zu koennen. Ingo hat entschieden, dass
die Umfrage selbst in die Anwendung gehoert, damit die Excel-Datei entfallen kann.

## 1. Zwei getrennte Dinge, bewusst nicht vermischt

| Tabelle | Inhalt | Wirkt im Export? |
| --- | --- | --- |
| `CustomerMarketSegments` | welcher Kunde zu welchem Segment gehoert | ja, aber NUR bestaetigte Zeilen |
| `MarketSurveyEntries` | die Marktumfrage selbst, inklusive Interessenten | nein |

Die Trennung ist fachlich zwingend. Die Umfrage beschreibt den MARKT und enthaelt
Interessenten mit Status `No Potential`, `Opportunity` oder `New`, zu denen es gar keinen
Umsatz gibt. Der Ist-Umsatz kommt weiterhin ausschliesslich aus den ERP-Zeilen. Wuerde man
beides mischen, stuenden Schaetzmengen neben fakturierten Werten.

## 2. Warum das Segment am KUNDEN haengt

Gemessen am Gesamtexport vom 2026-08-12:

- Von 105 zugeordneten Bahnkunden kaufen **91 hoechstens drei Produktfamilien**. Der Kunde
  ist also fast immer eindeutig.
- Die Produktkuerzel der Umfrage sind dagegen Standardfamilien quer durch alle Branchen:
  `NAT` in 7'417 Zeilen, `8252` in 6'217, `EPN` in 5'070. Ueber das Produkt wuerden weit
  ueber 30'000 Zeilen als Bahn markiert.
- Die Spalte `Material Number` der Umfrage ist in **0 von 269** Zeilen gefuellt.
- Eine Anwendung wie `Brakes` ist Verwendungszweck beim Kunden und steht in keiner
  Verkaufszeile als Stammdatum.

Gegenfall und Grenze der Regel: `Siemens SA` kauft vier Produktfamilien, die staerkste macht
nur 2,4 % seines Umsatzes. Siemens pauschal als Railway zu markieren wuerde den Bahnumsatz
massiv ueberzeichnen. Die Oberflaeche markiert Kunden ab vier Sparten deshalb farblich und
warnt beim Zuordnen.

## 3. Schluessel ist die Kundennummer, nicht der Name

`CustomerNumber` ist produktiv in allen neun Standorten zu **100 %** gefuellt, 4'888
verschiedene Nummern. Namen dagegen kollabieren beim Abgleich nachweislich:

- `BROT` trifft `K.S. & BROTHERS`
- `Stadler Rail` (CH) und `Stadler` (US) treffen beide `Stadler Rail Valencia S.A.U.` (ES)
- `Siemens` und `Siemens Mobility GmbH A&D LD` treffen beide `Siemens SA`

Dieselbe Kundennummer in zwei Standorten sind zwei verschiedene Kunden; ein Test deckt das ab.

## 4. Vorschlag gegen Bestaetigung

Der Namensabgleich liefert 173 Kundentreffer, die als **unbestaetigte Vorschlaege** in der
Tabelle liegen. Nur bestaetigte Zeilen erscheinen im zentralen Excel;
`MarketSegmentResolver.BuildLookup` filtert per Standard auf `IsConfirmed`.

Der Grund: ohne diese Trennung waere im Export nicht unterscheidbar, was der Vertrieb
geprueft hat und was der fehlbare Namensabgleich geraten hat. Mit ihr bekommt Patrik
dieselbe Bequemlichkeit — Durchklicken statt Tippen — ohne dass ungepruefte Zahlen als
Fakten im Reporting landen.

Ein verworfener Fehltreffer ist genauso Fortschritt wie ein bestaetigter Kunde.

## 5. Kein Rueckfall auf `CustomerIndustry`

Das Quellfeld existiert seit Langem und ist praktisch ungepflegt. Gemessen am 2026-08-12:

| TSC | Zeilen | Industry gefuellt |
| --- | ---: | ---: |
| TRFR | 2'598 | 210 (8,1 %) |
| TRIN | 7'179 | 21 (0,3 %) |
| TRIT | 19'955 | 8 |
| TRCH, TRAT, TRDE, TRES, TRUK, TRUS | 84'805 | **0** |

Nur acht verschiedene Werte, dominiert von `Ship Building` mit 150 Zeilen; `Railway` steht
auf genau 6 Zeilen. Wo das Feld gefuellt ist, nutzt jeder Standort seine eigene Taxonomie.
Ein solcher Wert unter einer Spalte, die Leser als verbindlich verstehen, waere schlimmer als
ein leeres Feld. Deshalb bleibt Unzugeordnetes leer.

## 6. Menge und Preis sind TEXT

Die Umfrage enthaelt Bereiche wie `500-600 pcs` und gemischte Waehrungen wie `15k€` neben
`CHF 45`, obwohl die Spalte `Estimated Sales Price / Pc. In CHF` heisst. Als Zahl gespeichert
wuerde das zu falschen Summen verleiten. Fuellgrad in der Quelle: Menge 40 von 269, Preis 54
von 269.

## 7. Zwei Excel-Spalten am ENDE

`Market Segment` und `Market Segment Source` stehen als Position 50 und 51 hinter allen
bestehenden Spalten. Ein Einschub in der Mitte waere still toedlich, weil der zentrale
Nachweis Blattformeln auf Spaltenpositionen enthaelt — dieselbe Fehlerklasse wie die
Statustext-Falle in `docs/RAG_ROUTER.md` Regel 11. Ein Kopfzeilentest prueft vier
Ankerpositionen und schlaegt an, sobald jemand mittig einfuegt.

## 8. Wo es liegt

- Seite: `Finance Cockpit > Marktsegmente`, Route `/marktsegmente`. Bewusst NICHT im
  Admin-Bereich, weil die Zuordnung eine fachliche Aussage des Vertriebs ist.
- Drei Reiter: `Ergebnis` (Umsatz je Segment, Land und Waehrung), `Marktumfrage` (Pflege der
  Umfrage), `Pflege` (Zuordnung der Kunden).
- Kernlogik: `Services/MarketSegmentResolver.cs` (rein und statisch),
  `Services/MarketSegmentPageService.cs`, `Services/MarketSurveyPageService.cs`.
- Jede Aenderung landet im Ereignisprotokoll unter den Kategorien `Marktsegment` und
  `Marktumfrage`, mit Vorher-Nachher-Wert.

## 9. Behobener Fehler beim Filter

Der erste Stand holte fuer den Filter „nur zugeordnete" erst die obersten 2'000 Kunden nach
Zeilenzahl und filterte danach. Ein zugeordneter kleiner Kunde von rund 4'900 fiel dadurch
still aus der Liste. Jetzt wird die Menge VOR dem Kappen auf die betroffenen Kunden
eingeschraenkt; ein Regressionstest deckt genau diesen Fall ab.

Ebenfalls behoben: die Filterauswahl startete auf einem Wert, der leer sein kann. Ohne offene
Vorschlaege springt sie jetzt auf bestaetigte beziehungsweise alle Kunden. Ein leerer
Standardfilter sieht wie ein Defekt aus, auch wenn er fachlich richtig rechnet.

## 10. Offene Fachfragen

- Gelten breit einkaufende Kunden wie Siemens pauschal als Railway? Entscheid Vertrieb.
- Soll die Pflege langfristig zentral bleiben oder in die Quellsysteme wandern? Fuer zentral
  spricht, dass `CustomerIndustry` in neun Standorten praktisch gescheitert ist.
- Weitere Segmente ausser Railway sind vorgesehen (`Ship Building`, `Hydrogen`,
  `Industrial`), brauchen aber eine abgestimmte Bezeichnungsliste.

## 11. Produktivstand am 2026-08-13

Drei Deploys an einem Tag, alle drei ohne Alarm und mit Vorher-Messung belegt:

| Zeit | Commit | Inhalt | Tests |
| --- | --- | --- | --- |
| 09:00 | `488cc42`, `07356a9` | Tabelle, Resolver, zwei Excel-Spalten, erste Pflegeseite | 500/500 |
| 11:14 | `ecaae3d` | Vorschlag gegen Bestaetigung, Ergebnissicht, Filterfehler behoben | 507/507 |
| 11:58 | `1371260` | Marktumfrage in der Anwendung pflegbar | 517/517 |

Datenstand produktiv, read-only geprueft:

- `CustomerMarketSegments`: **173 Zeilen, alle unbestaetigt**, ueber acht Standorte.
  Groesste Brocken Faiveley Transport Italia TRCH mit 693 Verkaufszeilen, RICA TRIT 164,
  CAF TRES 144, Medha Servo Drives TRCH 141.
- `MarketSurveyEntries`: **269 Zeilen**, importiert am 2026-08-13 nach Freigabe durch Ingo.
  Read-only nachgeprueft: `179` mit Verkaufskunde verknuepft, `13` Laender, `240` Kunden.
- Im zentralen Excel stehen noch keine Segmente, was korrekt ist: unbestaetigte
  Vorschlaege wirken dort nicht.

Statusverteilung der Umfrage, gemessen nach dem Import:

| Status | Zeilen |
| --- | ---: |
| (leer) | 142 |
| `Existing Customer` | 71 |
| `No Potential` | 25 |
| `Opportunity` | 19 |
| `New` | 12 |

Die 56 Zeilen mit `No Potential`, `Opportunity` oder `New` belegen nachtraeglich, warum die
Verknuepfung optional sein musste: ein Pflichtfeld haette genau diese Interessenten beim
Import verworfen.

Zwei Zaehlarten, kein Datenverlust: der Prueflauf meldete 236 Kunden und 12 Laender, die
Datenbank 240 und 13. Die Datenbank zaehlt einen leeren Landeswert als eigene Gruppe und
gruppiert Kunden ohne Beachtung der Gross-/Kleinschreibung anders.

Befehl fuer eine weitere Umfrage:

```powershell
dotnet run --project .tmp_tools\ImportMarketSurvey\ImportMarketSurvey.csproj -- `
  "\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db" `
  <umfrage.xlsx> "<Umfragename>" --apply
```

Ohne `--apply` laeuft nur die Pruefung. Der Import bricht ab, wenn fuer dieselbe Umfrage
schon Zeilen existieren, damit ein zweiter Lauf keine Doppel erzeugt und keine in der
Anwendung gepflegten Aenderungen verdeckt.

**Nicht belegt:** ein angemeldeter Sichtprueflauf und das Speichern einer Zuordnung oder
Umfragezeile durch einen echten Benutzer. Die Routen liefern HTTP 200 und die Seite rendert
Inhalt, aber kein Mensch hat produktiv geklickt. Der erste Klick von Ingo oder Patrik ist
damit der eigentliche Test.

## 11a. Was als Naechstes zu tun ist

1. Angemeldet `/marktsegmente` oeffnen und den Reiter `Marktumfrage` gegen die Excel-Datei
   stichprobenweise vergleichen. Erst danach die Datei archivieren.
2. Auf dem Reiter `Pflege` einen Vorschlag bestaetigen. Erwartetes Verhalten: die Zahl in der
   Filterbeschriftung faellt von 173 auf 172, und im Reiter `Ergebnis` erscheint der erste
   Bahnumsatz je Land und Waehrung.
3. Danach die 30 mengenstaerksten Vorschlaege durchgehen; sie decken rund zwei Drittel der
   betroffenen Verkaufszeilen ab. Grundlage:
   `docs/Railway_Kundenpruefung_Patrik_2026-08-13.xlsx`.
4. Fachentscheid einholen, ob breit einkaufende Kunden wie Siemens pauschal als Railway
   gelten. Die Oberflaeche warnt ab vier Produktsparten, entscheiden muss der Vertrieb.
5. Anleitung fuer Patrik: `docs/Anleitung_Marktsegmente_Vertrieb_2026-08-13.docx`. Der
   Mailtext dazu wurde im Chat entworfen und ist NICHT im Repository abgelegt.

## 12. Werkzeuge und Nachweise

- Machbarkeit und Schluesselwahl: `.tmp_tools/RailwayMappingCheck`,
  `.tmp_tools/RailwaySegmentKeyCheck` (beide read-only).
- Vorschlagslisten: `docs/Railway_Segment_Vorschlag_2026-08-12.xlsx` (312 Zeilen),
  `docs/Railway_Kundenpruefung_Patrik_2026-08-13.xlsx` (30 mengenstaerkste).
- Import: `.tmp_tools/ImportRailwayProposals` (Vorschlaege),
  `.tmp_tools/ImportMarketSurvey` (Umfrage). Beide mit Prueflauf und `--apply`.
- Anleitung fuer den Vertrieb: `docs/Anleitung_Marktsegmente_Vertrieb_2026-08-13.docx`.
- Quelle der Umfrage: `Railway_MarketSurvey_TSC_2026_05.xlsx`. Nach dem Import archivieren,
  aber erst nach einer Gegenpruefung in der Anwendung loeschen.
