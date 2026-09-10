# Marktsegmente und Marktumfrage in der Anwendung

Stand: 2026-09-10

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
Statustext-Falle in `router.md` Regel 11. Ein Kopfzeilentest prueft vier
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

## 13. Jahresbezug und 3D-Analyse, 2026-08-14

Auftrag von Ingo: das Jahr soll in die Marktsegmente hinein, dazu eine drehbare 3D-Ansicht im
selben Reiter.

### 13.1 Jahresfilter fuer die ganze Seite

Oben auf der Seite steht ein Auswahlfeld `Jahr`. Es wirkt auf die Ergebnissicht, auf die
Pflegeliste und auf die drei Kacheln unter `Stand der Pflege`. Voreingestellt ist das juengste
Jahr im Bestand, weil das die uebliche Frage ist. `alle Jahre` bleibt waehlbar.

Das Jahr einer Verkaufszeile folgt **derselben Regel wie das zentrale Excel**: Buchungsdatum,
sonst Rechnungsdatum, sonst Extraktionsdatum. Waere die Regel hier eine andere, stuende
dieselbe Zeile in der Segmentsicht in einem anderen Jahr als in der Finance-Spalte `Year`, und
die beiden Zahlen liessen sich nicht mehr gegeneinander pruefen. Die Finance-Regel `ForceYear`
wird bewusst nicht ausgewertet; sie ist am 2026-06-29 entfernt worden.

Die Ergebnistabelle hat jetzt eine Spalte `Jahr` und je Jahr, Standort und Waehrung eine Zeile.
Jahre werden ebenso wenig addiert wie Waehrungen.

Bewusst **nicht** gemacht: die Zuordnung selbst bekommt kein Jahr. Ein Kunde gehoert zu einem
Segment oder nicht; eine Gueltigkeit je Jahr haette Tabelle, Resolver und die Logik des
zentralen Excel veraendert. In den Kacheln bleiben die Kundenzahlen deshalb
jahresunabhaengig, nur die Zeilenzahlen folgen dem Filter. Ein bestaetigter Kunde ohne Umsatz
im gewaehlten Jahr erscheint als `bestaetigt` mit null Zeilen, was genau richtig ist.

### 13.2 3D-Analyse im Ergebnisreiter

Unter der Ergebnistabelle steht eine drehbare 3D-Sicht. Sie nutzt die vorhandene Engine
`wwwroot/js/finance3d.js` aus dem Management Cockpit, es kommt keine neue Bibliothek dazu.

- X-Achse: `Standort` oder `Segment`.
- Z-Achse: Jahr.
- Y-Achse: `Umsatz`, `Verkaufszeilen` oder `Kunden`.

Zwei Festlegungen, die sich aus Abschnitt 1 und 7 ergeben:

1. Es wird **immer genau eine Waehrung** dargestellt, voreingestellt die umsatzstaerkste.
   Waehrungen zu addieren waere ohne Kurse falsch; die Umrechnung gehoert in die Finance-Sicht.
2. Die Zeitachse zeigt **immer alle Jahre**, auch wenn oben ein einzelnes Jahr gewaehlt ist.
   Eine einzelne Jahresscheibe ergaebe keinen Verlauf und damit kein Diagramm.

Auf der Standortachse sind alle Segmente zusammengefasst; die Oberflaeche sagt das an.

### 13.3 Zwei Fehler, die bei der Sichtpruefung aufgefallen sind

**Der Seitenkopf lag unter der Kopfleiste.** `MudMainContent` trug in
`Components/Layout/MainLayout.razor` die Klasse `pa-4`. Diese MudBlazor-Hilfsklasse setzt
`padding` mit `!important` und ueberschrieb damit den Abstand, den MudBlazor fuer die fest
positionierte Kopfleiste vergibt. Die obersten rund 48 Pixel **jeder Seite** waren dadurch
unsichtbar, auch die Seitentitel. Aufgefallen ist es erst, als der neue Jahresfilter im DOM
stand, aber nicht auf dem Bildschirm. Jetzt `px-4 pb-4`.

**Auswahlwerte lagen ueber ihrer Beschriftung.** Ein `MudSelect` mit einem Eintrag vom Wert
leer, beschriftet `alle`, gilt fuer MudBlazor als unbefuellt. Die Beschriftung blieb unten
stehen, waehrend der Text `alle` darueber geschrieben wurde. Behoben mit einem `Placeholder`
an vier Stellen: Land und Status in der Marktumfrage, Standort in der Pflege, Jahr und
zusaetzlich die beiden Filter `Jahr` und `TSC` im Finance-Pivot des Management Cockpits.

### 13.4 Nachweis

`520/520` Tests gruen. Neue Tests: Aufteilung eines Kunden in eine Zeile je Jahr, Wirkung des
Jahresfilters auf Ergebnis, Suche und Kacheln, sowie die Jahresliste.

Angemeldet lokal gegen `trafag_exporter.db` geprueft: Jahresfilter sichtbar und ohne
Ueberlagerung, Ergebnistabelle mit Jahresspalte, 3D-Sicht gezeichnet und mit der Maus gedreht.
Drei Testzuordnungen wurden dafuer angelegt und danach wieder entfernt. Die Produktivdatenbank
wurde dabei nicht angefasst.

Produktiv deployed am 2026-08-14 21:02, Funktionscommit `7419473`, ohne Alarm. Werkzeug
`.tmp_tools/DeployMarketSegmentYear`. Vorher-Sicherung
`trafag_exporter.db.before-segment-year-20260814-205358.bak`. `BiDashboard.dll`
`4'595'712` Bytes, SHA256
`D1FE3189A1C37401E8CF813134E0A882AAAC03D01F7996DA2D964B54A1613AE7`, lokaler Release-Build und
Server bitgleich. Wirknachweis mit Vorher-Messung: `MarketSegmentChartAxes`,
`MarketSegmentChartValues`, `GetAvailableYearsAsync`,
`Auf der Standortachse sind alle Segmente zusammengefasst.` und `px-4 pb-4` fehlten im
Prueflauf und sind danach enthalten; `/marktsegmente` waechst von `66'785` auf `68'598` Bytes.
Keine Migration, kein Schemawechsel, Produktiv-DB in Laenge und Schreibzeit unveraendert.

Bei der Tokenwahl bewusst vermieden, weil sie einen Treffer vorgetaeuscht haetten: `Jahr`,
`Segment`, `marktsegmente` und `MarketSegmentPageService` stehen seit dem 2026-08-13 in der
DLL, `Z: Jahr` ist ein Teilstring des vorhandenen `Z: Jahr / Zeit` und `Alle Jahre` steht im
Finance-Pivot des Management Cockpits.

**Produktiv NICHT belegt:** ein angemeldeter Sichtprueflauf. Die Finance-Routen liegen hinter
dem Unlock und liefern von aussen nur das Passwortpanel. Der erste angemeldete Aufruf von Ingo
oder Patrik ist der eigentliche Test.

## 14. Stand 2026-08-27: Termin gesetzt, zwei Blocker gemessen

### Der Termin

Rohail Munir aus Deutschland braucht den Railway-Export **bis spaetestens 2026-09-08**, um damit
den Projektmanagement-Status zu praesentieren. Gefuehrt wird der Punkt als PM-08 in
`projektmanagement/PROJEKTSTATUS.md`.

### Produktiv gemessen

Werkzeug `.tmp_tools/CheckRailwayDe`, read-only. Weil die Anwendung zum Messzeitpunkt
heruntergefahren war (`-wal` und `-shm` fehlten, ein Lesezugriff ueber SMB scheitert dann mit
`unable to open database file`), lief die Messung ueber eine lokale Wegwerfkopie.

| Standort | Vorschlaege | bestaetigt |
| --- | ---: | ---: |
| TRCH | 81 | 0 |
| TRIT | 40 | 0 |
| TRFR | 17 | 0 |
| TRUK | 13 | 0 |
| TRES | 9 | 0 |
| TRAT | 8 | 0 |
| TRIN | 4 | 0 |
| TRUS | 1 | 0 |
| **TRDE** | **0** | **0** |

Alle `173` Vorschlaege tragen die Herkunft
`Namensabgleich Marktumfrage Railway 2026-05, noch nicht bestaetigt`. **Ein Export waere heute
leer**, weil unbestaetigte Vorschlaege bewusst nicht ins zentrale Excel wirken (Abschnitt 4).

### Blocker 1: Deutschland hat keine Kundennamen

Der kritischste Befund, weil die Anfrage aus Deutschland kommt.

| TSC | Zeilen | ohne Kundenname | ohne Kundenland |
| --- | ---: | ---: | ---: |
| TRDE | 7'526 | **7'526** | **7'526** |
| alle anderen acht | 93'032 | 0 | 359 |

Gegenprobe: Es gibt in `CentralSalesRecords` **keine einzige** TRDE-Zeile mit gefuelltem
Kundennamen. Die Kundennummer ist dagegen zu 100 % da.

Damit konnte der Namensabgleich fuer Deutschland nichts finden — es gab nichts zu vergleichen.
Das ist **unsere Luecke**: laut `docs/FINANCE_FELDLUECKEN.md` Abschnitt 6 selektiert die
Alphaplan-Query die `RechnungsAdressenID`, loest sie aber nie zu einem Namen auf. Gebraucht wird
ein read-only Auszug aus `INFORMATION_SCHEMA.COLUMNS`, gefiltert auf `%Adress%`, `%Artikel%`,
`%Liefer%`, `%Kunde%`. **Keine Tabellennamen raten**, das war die Lehre aus UK-2025.

Die Daten waeren vorhanden: Die Marktumfrage fuehrt `67` deutsche Zeilen mit genau den
erwarteten Namen (DB Regio, DB Fahrzeuginstandhaltung, Bombardier Transportation,
AKW A+V Protec Rail, DEUTA-WERKE). Davon sind `27` mit keinem Verkaufskunden verknuepft, die
uebrigen mit TRIT (`18`), TRCH (`17`), TRAT (`3`), TRUK (`1`) und TRES (`1`) —
**mit TRDE keine einzige**, weil die Verknuepfung ueber den Namen laeuft.

### Blocker 2: die Zuordnung ist ungeprueft

Ingo hat am 2026-08-27 festgelegt: **Patrik prueft vorher, ob die Zuordnung passt, oder macht
sie gleich selbst.** Es wird nicht blind bestaetigt, und Ingo bestaetigt auch nicht
stellvertretend. Die Segmentzuordnung ist eine Vertriebsentscheidung und bleibt dort.

Einstieg ist die Liste der 30 mengenstaerksten Vorschlaege
(`docs/Railway_Kundenpruefung_Patrik_2026-08-13.xlsx`), sie decken rund zwei Drittel der
betroffenen Verkaufszeilen ab. Anleitung:
`docs/Anleitung_Marktsegmente_Vertrieb_2026-08-13.docx`.

### Empfehlung fuer den Termin

Die beiden Blocker parallel bearbeiten, nicht nacheinander. Blocker 2 ist in Tagen loesbar,
Blocker 1 haengt an einem Auszug vom deutschen Server und laesst sich von hier aus nicht
erzwingen.

**Rohail Munir sollte frueh wissen, dass Deutschland moeglicherweise fehlt.** Ein Export, der
Deutschland stillschweigend mit null Bahnumsatz zeigt, waere schlechter als einer, der die
Luecke benennt — besonders in einer Statuspraesentation.

## 15. Stand 2026-09-01: Excel-Export per Mausklick, PRODUKTIV DEPLOYED

Antwort auf den Termin aus Abschnitt 14: `/marktsegmente` hat jetzt neben dem Jahresfilter den
Knopf **Export (Excel)**. Ein Klick, kein Zwischendialog, Download im Browser, nichts wird auf
dem Server abgelegt. Fachliche Herleitung, alle zwoelf Entscheidungen mit gesetzten
Defaultwerten und die technische Umsetzung stehen vollstaendig in
`docs/KONZEPT_RAILWAY_EXPORT_2026-09-01.md`; dieser Abschnitt fasst nur zusammen, was fuer die
Fortsetzung dieser Fachdatei wichtig ist.

**Acht Blaetter:** `Anleitung`, `Pruefung`, `Bestaetigt`, `Verworfen (Protokoll)`,
`Umsatz_Summen`, `Umsatz_Detail`, `Marktumfrage`, `Datenluecken`. Jahr und Standort der Seite
wirken auf den Export, der Namensfilter bewusst nicht — sonst saehe eine Datei nach einer
stehengebliebenen Sucheingabe vollstaendig aus, obwohl sie es nicht ist.

**Zwei Fragen aus dieser Fachdatei beantwortet der Export, ohne sie zu loesen:**

- Ein Zustandsblatt `Verworfen` wie im urspruenglichen Fragebogen vorgesehen gibt es nicht.
  `MarketSegmentPageService.ClearAsync` loescht die Zuordnungszeile, ein Zustand `verworfen`
  wird nirgends gespeichert. Der Export liefert stattdessen die Protokollspur aus
  `AppEventLogs`, Kategorie `Marktsegment`, Nachricht `Segment entfernt` — ohne Nutzerspalte,
  weil `AppEventLog` keinen Nutzer fuehrt. Eine echte gespeicherte Ablehnung waere ein eigener
  Auftrag mit Schemawechsel und ist nicht Teil dieses Deploys.
- Zum damaligen Stand war Blocker 1 aus Abschnitt 14 (Deutschland ohne Kundennamen)
  **nicht geloest**. Das Blatt `Datenluecken` wies ihn offen aus, statt ihn als Umsatz von
  null zu zeigen. Der Produktivnachtrag vom 09.09. am Ende dieser Datei ersetzt diesen
  historischen Status: 4'549 Zeilen sind inzwischen belegt, 3'066 bleiben offen.

**Produktiv am Deploytag read-only gemessen** (deckt sich mit der Messung aus Abschnitt 14, kein
neuer Zuwachs seither): `173` Vorschlaege, `0` bestaetigt, `269` Umfragezeilen, `104'222`
Verkaufszeilen, `7'526` TRDE-Zeilen ohne Kundenname, `0` Protokolleintraege `Segment entfernt`.
Die Blaetter `Bestaetigt` und `Verworfen (Protokoll)` sind deshalb heute leer und tragen ihren
Hinweistext statt Zeilen — der erwartete Zustand, kein Fehler.

**Deploy:** 2026-09-01, 09:21 Uhr, `668/668` Release-Tests gruen, Details und Nachweise in
`docs/rag/DEPLOYMENT.md` Kurzstand und `docs/AGENT_COORDINATION.md`. Der Funktionsstand ist
inzwischen im Commit `835b317` enthalten und aus Git reproduzierbar. Der Knopf wurde im
ausgelieferten HTML nachgewiesen; offen bleibt der echte Klicktest, dass der Browser die Datei
erzeugt und die acht Blaetter so aussehen wie hier beschrieben.

## 16. Segmentliste aus der Vertriebsvorgabe, 2026-09-01

Die bisherige kleine Vorschlagsliste war unvollstaendig. Als verbindliche Standardauswahl in
der Kundenpflege gelten jetzt die 15 Werte aus `segmente.png`:

`Calibration services`, `Food & Beverage`, `General Industry`, `Hydraulics`, `Hydrogen`,
`Large Engines`, `Power Distribution`, `Railway`, `Shipbuilding`, `Test & Measurement`,
`Water Treatment`, `Others`, `Automotive (MAG)`, `E-Bikes (MAG)` und `Robotics (MAG)`.

Die Schreibweise wird exakt aus der Vorgabe uebernommen; insbesondere ersetzt `Shipbuilding`
den bisherigen Standardvorschlag `Ship Building`. Bereits gespeicherte freie Sonderwerte
bleiben zusaetzlich sichtbar, damit keine bestehende Zuordnung durch die neue Liste verborgen
wird. Es werden keine Kunden automatisch umklassifiziert und keine Vorschlaege bestaetigt.

**Produktiv deployed am 2026-09-01 um 13:40:** Funktionscommit `275fe95`, `668/668`
Release-Tests gruen, Server-DLL und lokaler Build bitgleich. Die neun vorher produktiv
fehlenden neuen Literale sind nach dem Publish vorhanden; `Ship Building` und
`Mobile Hydraulics` sind nicht mehr enthalten. Startseite, `/marktsegmente` und
`/management-cockpit` antworten HTTPS `200`. Kein Schemawechsel und keine Datenmigration;
die Produktivdatenbank blieb in Laenge und Schreibzeit unveraendert. Der genaue
Deploynachweis steht in `docs/rag/DEPLOYMENT.md`. Offen bleibt der visuelle Sichtprueflauf
der aufgeklappten Auswahlliste im Browser.

## 17. Produktivnachtrag Deutschland 09.09.2026

Deutschland hat jetzt 19 bestaetigte Railway-Kunden aus direkten Belegnachweisen und
einer reinen Alphaplan-Bahnbranche. Die Pruefung verlangt exakt `00 Bahn` oder
`05 rw Railways / Bahntechnik`; Mischbranchen bleiben offen. Von 7'615 DE-Verkaufszeilen
sind 4'549 fachlich zugeordnet. 3'066 Zeilen tragen weiterhin einen sichtbar technischen
Schluessel `ALPHAPLAN-ID:<ID>` und werden nicht ueber Nummernueberschneidungen,
Lieferantennummern oder Namensaehnlichkeit zugeordnet.

Die 171 offenen Vorschlaege der anderen Standorte bleiben fachlich zu pruefen. Der
deutsche Kundenstamm ist kein Nachweis fuer deren lokale Kundenschluessel. Aktuelle
Arbeitsdateien: `Bahnmarkt_Rohail_2026-09-09.xlsx` und
`Bahnmarkt_DE_Kundenzuordnung_2026-09-09.xlsx`. Vollstaendiger technischer und
fachlicher Nachweis: `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md`, Abschnitt 8.

## 18. Kann die Segmentzuweisung automatisch laufen? Messung 2026-09-10

Ingos Frage nach dem produktiven Nachzug Deutschlands: „kann man dadurch die zuweisung
segment zu kunde automatisch machen oder muss das weiterhin noch manuell erfolgen?"

Die Antwort ist zweigeteilt, und der Unterschied liegt nicht am Programm, sondern daran, ob
das Quellsystem ein Branchenfeld pflegt.

### Ist-Stand der Zuordnungen, produktiv gemessen

| Standort | Eintraege | bestaetigt | Vorschlag |
| --- | ---: | ---: | ---: |
| **TRDE** | 24 | **24** | 0 |
| TRCH | 80 | 0 | 80 |
| TRIT | 40 | 0 | 40 |
| TRFR | 17 | 0 | 17 |
| TRUK | 13 | 0 | 13 |
| TRES | 9 | 0 | 9 |
| TRAT | 8 | 0 | 8 |
| TRIN | 4 | 1 | 3 |
| TRUS | 1 | 0 | 1 |

Von 196 Eintraegen sind 25 bestaetigt, davon **24 aus Deutschland** mit der Quelle
`Alphaplan Kundenstamm / Branche`. Die uebrigen 171 sind weiterhin Namensabgleich-Vorschlaege
aus der Marktumfrage. In der ganzen Tabelle existiert bisher genau **ein** Segmentwert,
`Railway`.

### Warum nur Deutschland: Fuellgrad von `CustomerIndustry`

| Standort | Zeilen | mit Branche | Kunden mit Branche |
| --- | ---: | ---: | ---: |
| TRDE | 7'622 | 7'433 | **543** |
| TRFR | 2'684 | 221 | 20 |
| TRIN | 7'548 | 21 | 6 |
| TRIT | 24'935 | 10 | 2 |
| TRSE | 6'915 | 0 | 0 |
| TRUK | 3'181 | 0 | 0 |
| TRUS | 1'631 | 0 | 0 |
| **ZSCHWEIZ** | **52'276** | **0** | **0** |

Die Schweiz ist mit 52'276 Zeilen der groesste Standort und hat kein einziges Branchenfeld.
Ohne Quellfeld gibt es nichts zu automatisieren; das ist keine Frage der Umsetzung.

### Der eigentliche Fund: Deutschland traegt weit mehr als Bahn

543 deutsche Kunden tragen eine gepflegte Branche ueber 38 verschiedene Werte, zusammen
**6'887'039 EUR von 7'033'623 EUR DE-Umsatz, also 97,9 Prozent**. Genutzt werden davon heute
nur die 24 Kunden mit `00 Bahn`.

| Branche | Kunden | Zeilen | Umsatz EUR |
|---|---:|---:|---:|
| 31 Ersatzteilhändler Schiffbau | 83 | 1355 | 535'848 |
| 30 Händler | 80 | 655 | 448'889 |
| 32 Ersatzteilhändler Industrie | 40 | 233 | 90'054 |
| 25 Allgemeiner Anlagenbau | 26 | 356 | 262'348 |
| 03 Schiffbau (Systeme) | 25 | 679 | 850'522 |
| 00 Bahn | 24 | 657 | 1'010'874 |
| 34 Reederei | 24 | 166 | 66'594 |
| 40 Kleinbetriebe | 23 | 132 | 645'297 |
| 33 Ersatzteilhändler S/I | 21 | 241 | 101'168 |
| 05 Prüfstände | 20 | 214 | 333'051 |
| 35 Instandhaltung/Wartung | 18 | 90 | 26'545 |
| 09 Mobilhydraulik | 14 | 321 | 511'485 |
| 01 Motoren | 14 | 269 | 476'036 |
| 02 Schiffbau (Werft) | 14 | 322 | 115'898 |
| 10 Hydraulik | 13 | 206 | 193'753 |
| 04 Gasdichte (SF6) | 13 | 69 | 144'076 |
| 06 Klimatechnik | 11 | 336 | 179'072 |
| 07 Pumpen | 10 | 161 | 203'906 |
| 12 Automatisierungstechnik | 9 | 96 | 72'091 |
| 11 Getriebe | 8 | 65 | 54'103 |
| 08 Kompressoren | 7 | 67 | 50'987 |
| 52 Wasserstoffanwendungen | 7 | 66 | 28'388 |
| 16 Wasseranwendungen | 7 | 53 | 25'170 |
| 23 Brandschutz | 5 | 86 | 61'885 |
| 19 Medizintechnik | 4 | 23 | 34'342 |
| 27 Baumaschinen | 3 | 176 | 92'591 |
| 15 EX-Anwendungen | 3 | 12 | 5'430 |
| 29 Drehmomentanwendungen | 2 | 173 | 97'548 |
| 21 Lebensmittelindustrie | 2 | 26 | 87'610 |
| 20 Energietechnik | 2 | 34 | 48'133 |
| 17 Verpackungsanlagen | 2 | 59 | 22'125 |
| 26 Erneuerbare Energien | 2 | 11 | 5'771 |
| 22 Filtertechnik | 2 | 6 | 1'594 |
| 52 Allgemeiner Anlagenbau | 1 | 3 | 1'427 |
| 36 Kunststofftechnik | 1 | 6 | 1'242 |
| 13 Flugzeugbau | 1 | 3 | 727 |
| 25 Allgemeiner Anlagenbau, | 1 | 3 | 231 |
| 30 Händler, | 1 | 3 | 228 |
| **Summe** | **543** | **7'433** | **6'887'039** |

Allein die schiffbaunahen Branchen `31`, `03`, `34`, `02` und `33` ergeben rund 167 Kunden.

**Zwei Datenmaengel im deutschen Kundenstamm, beim Messen aufgefallen:** die Nummer `52`
traegt zwei verschiedene Bezeichnungen (`Wasserstoffanwendungen` und `Allgemeiner
Anlagenbau`), und zwei Werte enden auf ein Komma (`25 Allgemeiner Anlagenbau,` und
`30 Händler,`). Betroffen sind zusammen neun Kunden. Das gehoert mit Rohail geklaert, bevor
eine Zuordnungstabelle auf diesen Werten aufsetzt.

### Was fehlt, damit mehr automatisch geht

1. **Ein fachlicher Entscheid, welche Alphaplan-Branche auf welches Marktsegment abbildet.**
   Das ist eine Tabelle mit rund 38 Zeilen und gehoert dem Vertrieb beziehungsweise Andreas,
   nicht der Entwicklung. Erst danach kann die Automatik ueber `00 Bahn` hinausgehen.
2. **Ein zweites Segment ueberhaupt.** Heute kennt `CustomerMarketSegments` nur `Railway`.
   Die Segmentliste aus der Vertriebsvorgabe steht in Abschnitt 16.
3. **Fuer die anderen acht Standorte eine Quelle.** Entweder pflegen sie ein Branchenfeld im
   eigenen System, oder es braucht je Standort eine einmalig kuratierte Liste auf
   Kundennummer. Ein Namensabgleich zur Laufzeit bleibt untauglich; der Beleg dafuer steht in
   Abschnitt 3 und im Issue-Log bei ISS-014.

### Was damit beantwortet ist

Die Vorfrage aus ISS-014 vom 2026-08-12 lautete, ob Railway am Kunden oder an der Anwendung
bestimmt wird und welcher Schluessel gepflegt wird. **Deutschland beantwortet beides:
Das Segment haengt am Kunden, und der belastbare Schluessel ist die Kundennummer plus ein
gepflegtes Branchenfeld im Quellsystem.** Es ist damit kein theoretischer Vorschlag mehr,
sondern seit dem 2026-09-09 produktiv belegt.

Messwerkzeug: `.tmp_tools/SegmentCheck`, rein lesend gegen einen konsistenten Abzug der
Produktivdatenbank vom 2026-09-10.
