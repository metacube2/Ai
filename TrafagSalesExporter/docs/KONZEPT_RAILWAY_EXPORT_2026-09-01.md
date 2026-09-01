# Konzept: Railway-Excel-Export per Mausklick

Stand: 2026-09-01
Zieltermin: 2026-09-08 (PM-08, Statuspraesentation von Rohail Munir)
Grundlage: `docs/FRAGEBOGEN_RAILWAY_EXPORT_PATRIK_2026-09-01.md`

Dieses Konzept beantwortet die zwoelf Fragen des Fragebogens mit gesetzten Defaultwerten
und beschreibt daraus den fertigen Lieferumfang. Der Fragebogen selbst bleibt unveraendert,
er ist das Abstimmungsdokument. Wo Patrik oder Andreas anders entscheiden, wird nur die
betroffene Zeile hier ueberschrieben; der Rest des Konzepts haelt.

**Umsetzungsstand 2026-09-01: gebaut, getestet und PRODUKTIV DEPLOYED um 09:21.** Der Knopf
ist umgesetzt, `668/668` Release-Tests sind gruen, dreizehn davon neu fuer diesen Export.
Offen ist allein der angemeldete Sichtprueflauf. Belege zum Deploy stehen im Kurzstand von
`docs/rag/DEPLOYMENT.md`. Wo die Umsetzung vom urspruenglichen Entwurf abweicht, steht es
unten an der jeweiligen Stelle.

## 1. Was der Knopf tut

Auf `/marktsegmente` steht neben dem Jahresfilter die Schaltflaeche **Export (Excel)**. Ein
Klick erzeugt die Arbeitsmappe im Speicher und laedt sie sofort als Datei herunter. Es gibt
keinen Zwischendialog, keine Zusatzauswahl und keine serverseitige Ablage.

Abweichung vom Entwurf, bewusst: der Knopf heisst nicht `Railway-Export` und filtert auch
nicht auf Railway. Er exportiert **alle gepflegten Segmente**, und die Spalte `Segment`
unterscheidet sie. Ein stiller Segmentfilter wuerde sonst spaeter gepflegte Segmente aus
einer Datei heraushalten, die vollstaendig aussieht. Heute sind ohnehin alle Zuordnungen
Railway, der Inhalt ist also derselbe; das Blatt `Anleitung` nennt die enthaltenen Segmente.

Von den Filtern der Seite gehen **Jahr und Standort** in den Export ein. Der
Namensfilter des Suchfeldes wird bewusst **nicht** angewendet: er dient dem Suchen eines
einzelnen Kunden auf der Seite, und eine Datei, die nach einer stehengebliebenen Sucheingabe
still nur sechs statt aller Kunden enthaelt, sieht aus wie ein vollstaendiger Export. Das
Blatt `Anleitung` nennt darum ausdruecklich beides: welche Filter gewirkt haben und welcher
bewusst ignoriert wurde.

Dateiname: `Marktsegmente_Export_<Jahr oder ALLE>_<yyyyMMdd_HHmmss>.xlsx`, zum Beispiel
`Marktsegmente_Export_2026_20260908_071500.xlsx`.

## 2. Die zwoelf Entscheidungen mit gesetzten Defaultwerten

| Nr. | Frage | Gesetzter Defaultwert | Begruendung |
| --- | --- | --- | --- |
| 1 | Wofuer nutzt Patrik die Datei? | **Offline analysieren, Entscheidungen im Dashboard pflegen.** Die Datei ist reines Lesegut. | Ein Rueckimport braucht Vorlagenformat, Validierung und eine Konfliktregel. Das ist bis zum 08.09. nicht sauber zu liefern und wuerde die Pflege im Dashboard als fuehrende Stelle aufweichen. |
| 2 | Welche Kunden gehoeren in die Datei? | **Offene Vorschlaege und bestaetigte Zuordnungen**, jeweils in einem eigenen Blatt. Nicht der gesamte Kundenstamm. | Patrik sieht damit die Pruefmenge und den bisherigen Fortschritt. Alle rund 4'900 Kunden waeren keine Pruefliste mehr, sondern ein Datenabzug. |
| 3 | Sollen verworfene Vorschlaege enthalten sein? | **Ja, aber nur als Ereignisliste aus dem Anwendungsprotokoll**, klar als Protokollspur beschriftet. Kein Zustandsblatt. | Ein Verwerfen loescht heute die Zuordnungszeile (`MarketSegmentPageService.ClearAsync`, Zeile 335). Es gibt keinen gespeicherten Zustand `verworfen`, den man auslesen koennte. Siehe Abschnitt 6. |
| 4 | Welche Jahre? | **Alle vorhandenen Jahre, mit `Jahr` als eigener Spalte.** Setzt Patrik auf der Seite ein Jahr, gilt dieses. | Das Jahr folgt derselben Regel wie das zentrale Excel: `PostingDate`, sonst `InvoiceDate`, sonst `ExtractionDate`. Die Finance-Regel `ForceYear` wird bewusst nicht angewendet, wie in der Seite auch. Nur so sind Export und Ergebnisansicht vergleichbar. |
| 5 | Welche Umsatzdarstellung? | **Rohwerte je Waehrung.** Keine Konzernwaehrung, keine Umrechnung, keine Mischsumme. | Waehrungen ohne Kurse zu addieren waere falsch. Die Umrechnung ist eine Finance-Entscheidung und gehoert in die Finance-Sicht, nicht in eine Segmentarbeitsdatei. |
| 6 | Sollen einzelne Verkaufszeilen exportiert werden? | **Beides.** Ein Detailblatt mit Verkaufszeilen und ein Summenblatt je Kunde, Jahr, Standort und Waehrung. | Patrik braucht die Summe zum Priorisieren und die Detailzeile zum Nachvollziehen eines Zweifelsfalls. |
| 7 | Soll die Marktumfrage enthalten sein? | **Ja, als eigenes Blatt.** | Umfragezeilen sind Marktwissen und kein Umsatz. In der Railway-Umfrage gibt es Interessenten ohne jeden ERP-Kunden. Vermischt mit fakturiertem Umsatz waere das eine Falschaussage. |
| 8 | Wie werden breit einkaufende Kunden behandelt? | **Einzeln entscheiden.** Der Export markiert sie nur, er entscheidet nicht. Ab vier verschiedenen Produktsparten steht in der Spalte `Warnung` der Hinweis `Breites Sortiment, bitte einzeln pruefen`. | Ein Konzern wie Siemens kauft quer durch alle Branchen. Pauschal Railway waere geraten, ausschliessen wuerde echte Bahnumsaetze verlieren. |
| 9 | Darf die Datei sensible Felder enthalten? | **Nur fachlich benoetigte Felder**, siehe die Spaltenlisten in Abschnitt 4. Keine internen Kostenfelder, keine Margen, keine Standardkosten. | Die Datei verlaesst das Dashboard und wird per Datei weitergereicht. Umsatz je Kunde ist fuer die Pruefung noetig, Kosten und Marge sind es nicht. |
| 10 | Wer darf den Export ausloesen? | **Dieselbe Berechtigung wie die Seite `/marktsegmente` heute**, also jeder angemeldete Nutzer der Anwendung. Kein zusaetzlicher Finance-Unlock. | Die Seite traegt heute kein `Authorize`-Attribut und keinen Unlock. Ein Export, der weniger zeigt als die Seite selbst, braucht keine strengere Huerde. Kommt von Andreas eine engere Vorgabe, wird sie an der Seite und am Knopf gemeinsam gesetzt. |
| 11 | Wie soll die Datei heissen und gespeichert werden? | **Browser-Download mit Zeitstempel im Dateinamen.** Keine Ablage auf dem Server. | Damit gibt es keine wachsende Sammlung personenbezogener Arbeitsdateien auf dem Server, und jede Fassung ist am Zeitstempel unterscheidbar. |
| 12 | Ist ein Rueckimport bis 08.09. zwingend? | **Nein.** Der Rueckimport ist ein eigener, spaeter zu entscheidender Lieferumfang. | Patrik bestaetigt oder verwirft im Dashboard. Das ist heute schon moeglich, protokolliert und ohne Abgleichrisiko. |

## 3. Datenlage, auf die dieses Konzept trifft

Diese Zahlen sind nicht in diesem Auftrag gemessen, sondern uebernommen. Quelle ist die
produktive Messung vom 2026-08-27 mit dem read-only Werkzeug `.tmp_tools/CheckRailwayDe`,
festgehalten in `docs/AGENT_COORDINATION.md` und `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md`.

- `173` Railway-Vorschlaege ueber acht Standorte, davon **`0` bestaetigt**.
- Deutschland hat `0` Vorschlaege, obwohl die Anfrage aus Deutschland kommt.
- Bei allen `7'526` deutschen Verkaufszeilen fehlen Kundenname und Kundenland, die
  Kundennummer ist zu 100 Prozent vorhanden.
- Die Marktumfrage hat `269` Zeilen, davon `179` mit einem Verkaufskunden verknuepft.

Daraus folgen zwei Dinge, die im Export sichtbar sein muessen und nicht weggeglaettet
werden duerfen. Erstens ist das Blatt `Bestaetigt` am Tag der Erzeugung leer, solange
niemand bestaetigt hat; das Blatt existiert trotzdem und traegt den Hinweis, dass leer hier
`noch nichts geprueft` bedeutet und nicht `nichts gefunden`. Zweitens erscheint Deutschland
in der Pruefliste praktisch nicht, weil der Namensabgleich dort mangels Kundennamen nichts
finden konnte. Das gehoert in das Blatt `Datenluecken` und in die `Anleitung`, weil es genau
die Frage betrifft, die in der Praesentation gestellt wird.

## 4. Aufbau der Arbeitsmappe

Acht Blaetter in dieser Reihenfolge. Jede Zeile der Zuordnungsblaetter ist ueber `TSC` plus
`Kundennummer` eindeutig, der Kundenname ist nur Lesehilfe.

### `Anleitung`

Kein Tabellenblatt im engeren Sinn, sondern Feld-Wert-Paare: Erzeugungszeitpunkt,
angemeldeter Nutzer aus der Windows-Anmeldung (dieselbe Quelle, die `NavMenu.razor` ueber
den `AuthenticationStateProvider` liest), gewaehltes Jahr, gewaehlter Standortfilter, der
Hinweis `Namensfilter wurde bewusst nicht angewendet`, Anzahl Zeilen je Blatt,
Erklaerung von `Vorschlag` gegen `Bestaetigt`, der Satz `Ein unbestaetigter Vorschlag ist
keine Reporting-Aussage`, die Datumsregel aus Entscheidung 4 und der Hinweis auf die
TRDE-Luecke.

### `Pruefung` und `Bestaetigt`

Beide Blaetter tragen dieselben Spalten, damit Patrik sie nebeneinander lesen kann.

| Spalte | Inhalt |
| --- | --- |
| `TSC` | Standortkuerzel der Verkaufszeilen |
| `Kundennummer` | lokale Kundennummer, der eigentliche Schluessel |
| `Kundenname` | Name aus dem Verkaufsbestand, Lesehilfe |
| `Kundenland` | Land aus dem Verkaufsbestand |
| `Segment` | zugeordnetes Segment, hier `Railway` |
| `Status` | `Vorschlag` oder `Bestaetigt` |
| `Quelle` | woher die Zuordnung stammt, zum Beispiel `Marktumfrage Railway 2026-05` |
| `Hinweis` | die gespeicherte Vorschlagsnotiz, etwa die Vertrauensstufe |
| `Verkaufszeilen` | Anzahl Verkaufszeilen des Kunden im gewaehlten Zeitraum |
| `Produktsparten` | Anzahl verschiedener Produktsparten |
| `Warnung` | gefuellt ab vier Produktsparten, siehe Entscheidung 8 |
| `Zuletzt geaendert` | Zeitstempel der Zuordnung |

Bewusst **keine** Umsatzsumme auf diesen beiden Blaettern. Die Kundenzeile der Seite fuehrt
nur eine einzige Waehrung mit, obwohl ein Kunde in mehreren Waehrungen fakturiert sein kann;
eine Summe daneben waere still falsch. Der Umsatz steht waehrungsrein im Blatt
`Umsatz_Summen`, verknuepfbar ueber `TSC` und `Kundennummer`.

### `Verworfen (Protokoll)`

Die Ereignisse `Segment entfernt` aus der Tabelle `AppEventLogs`, Kategorie `Marktsegment`.
Spalten: `Zeitpunkt`, `TSC`, `Kundennummer` und `Vorheriger Stand`.

Zwei Einschraenkungen, die im Blattkopf stehen muessen und nicht wegzureden sind. Erstens
kennt `AppEventLog` **keinen Nutzer**; das Modell fuehrt nur `Timestamp`, `Level`,
`Category`, `SiteId`, `Land`, `Message` und `Details`. Wer verworfen hat, ist also nicht
ausweisbar, und die Spalte `Nutzer` gibt es folgerichtig nicht. Zweitens stehen `TSC`,
Kundennummer und vorheriger Stand in einem einzigen Textfeld: `ClearAsync` schreibt
`details` als `"{TSC}/{Kundennummer} | war {Segment}/{Vorschlag oder bestaetigt}"`
(`MarketSegmentPageService.cs`, Zeile 341). Die drei Spalten entstehen also durch Zerlegen
dieses Textes am ersten `/` und am ` | war `; passt eine Zeile nicht auf dieses Muster,
kommt sie unzerlegt in die Spalte `Vorheriger Stand` und wird nicht geraten.

Die Blattueberschrift sagt ausdruecklich, dass dies eine Protokollspur und kein gepflegter
Zustand ist. Sie beginnt beim aeltesten noch vorhandenen Protokolleintrag, nicht bei der
ersten je erfolgten Entfernung. Wird ein entfernter Kunde spaeter erneut vorgeschlagen,
steht er zu Recht wieder in `Pruefung` und zusaetzlich hier. Siehe Abschnitt 6.

### `Umsatz_Summen`

Eine Zeile je `TSC`, `Kundennummer`, `Jahr` und `Waehrung`, mit `Verkaufszeilen` und
`Umsatz`. Die Waehrung steht als eigene Spalte, es wird ausschliesslich innerhalb derselben
Waehrung summiert. Zusaetzlich `Status`, damit eine Auswertung dieses Blattes nicht
versehentlich Vorschlaege als Railway-Umsatz liest.

### `Umsatz_Detail`

Die Verkaufszeilen der Kunden aus `Pruefung` und `Bestaetigt`. Die Spalten mit den
tatsaechlichen Feldnamen aus `Models/CentralSalesRecord.cs`: `Tsc`, `CustomerNumber`,
`CustomerName`, das nach der Datumsregel abgeleitete `Jahr`, `PostingDate`, `InvoiceDate`,
`InvoiceNumber`, `Material`, `ProductFamilyText`, `Quantity`, `SalesCurrency`,
`SalesPriceValue` und `Status`. Keine Kosten- und keine Margenfelder, also weder
`StandardCost` noch die Fix/variabel-Aufteilung. Bei allen Jahren und der
heutigen Vorschlagsmenge ist mit einer fuenfstelligen Zeilenzahl zu rechnen, was Excel
problemlos traegt.

### `Marktumfrage`

Die Umfragezeilen mit `Land`, `Kundenname`, `Kundentyp`, `Geschaeftsart`, `Anwendung`,
`Status`, `Produkt`, `geschaetzte Menge`, `geschaetzter Preis`, `Wettbewerber`,
`Bemerkung` sowie `Verknuepfter TSC` und `Verknuepfte Kundennummer`. Menge und Preis
bleiben Text, weil die Quelle Bereiche und gemischte Waehrungen wie `500-600 pcs` oder
`15k€` enthaelt. Eine Zahl daraus waere Scheingenauigkeit.

### `Datenluecken`

Je Standort die Anzahl Verkaufszeilen ohne Kundenname und ohne Kundenland sowie die Anzahl
Umfragezeilen ohne Verknuepfung. TRDE erscheint hier mit der vollen Zeilenzahl. Das Blatt
ist der Beleg dafuer, dass ein leeres deutsches Ergebnis eine Datenluecke ist und nicht
ein Umsatz von null.

## 5. Technische Umsetzung

Es wurde nichts Neues erfunden, sondern das vorhandene Muster des Management Cockpits
wiederverwendet. Umgesetzt sind:

1. Neuer Dienst `Services/MarketSegmentExportService.cs` mit
   `BuildSheetsAsync(int? year, string? tscFilter, string requestedBy)`, der die acht
   Blaetter als `IReadOnlyList<ExcelSheetData>` liefert. Bewusst ein eigener Dienst und
   nicht eine Methode an `MarketSegmentPageService`: dieser kennt nur die Pflege mit ihrer
   Seitenbegrenzung von 200 Zeilen, der Export braucht den vollstaendigen Bestand.
   Registriert in `Program.cs` neben dem Pflegedienst.
2. Mappe erzeugt mit `IExcelExportService.CreateWorkbookBytes`.
3. Download in `Components/Pages/MarketSegments.razor` in `ExportExcelAsync` ueber
   `JsRuntime.InvokeVoidAsync("trafagDownload.saveBytes", ...)`, wie im Management Cockpit,
   samt Fehlermeldung ueber den Snackbar. Der Nutzername fuer das Blatt `Anleitung` kommt
   aus dem `AuthenticationStateProvider`.
4. Die Knopfbeschriftung `Export (Excel)` ist in allen sechs generierten Sprachen
   uebersetzt. Ohne das faellt `UiTextServiceTests` um, was beim ersten Lauf auch passiert
   ist. Die Blattnamen bleiben absichtlich unuebersetzt: sie sind Teil des Dateiformats,
   und eine Datei, deren Blaetter je nach Spracheinstellung des Erzeugers anders heissen,
   waere nicht auswertbar.
5. Neue Testdatei `TrafagSalesExporter.Tests/MarketSegmentExportServiceTests.cs` mit
   dreizehn Faellen: alle acht Blaetter in der vereinbarten Reihenfolge; ein Vorschlag
   landet nie im Blatt `Bestaetigt`; ein Kunde mit zwei Waehrungen erzeugt zwei Summenzeilen
   und die Mischsumme kommt nirgends vor; `Pruefung` fuehrt gar keine Umsatzspalte; die
   Jahresregel `PostingDate ?? InvoiceDate ?? ExtractionDate`; die Warnung ab vier
   Produktsparten; die Zerlegung des Protokolltextes samt unzerlegbarem Sonderfall und
   fehlender Nutzerspalte; leere Blaetter mit Aussage; die TRDE-Luecke; der Standortfilter
   mitsamt seinem Ausweis in der `Anleitung`; das Fehlen von Kosten- und Margenfeldern; und
   zuletzt die wirklich erzeugte Mappe, mit ClosedXML zurueckgelesen und auf Blattnamen und
   Kopfzeile geprueft.

Kein Schemawechsel, keine Migration, keine neue Bibliothek, kein SAP-Zugriff. Der Export
liest ausschliesslich, er schreibt nichts in die Datenbank und nichts auf die Platte.

## 6. Bewusst nicht im Lieferumfang

- **Ein Zustandsblatt `Verworfen`.** Der Fragebogen sieht es vor, die Datenlage traegt es
  nicht: `ClearAsync` loescht die Zeile, ein Zustand `verworfen` wird nirgends gespeichert.
  Wer das wirklich braucht, braucht zuerst eine gespeicherte Ablehnung, also eine additive
  Spalte auf `CustomerMarketSegments` und eine Aktion, die markiert statt loescht. Das ist
  ein eigener Auftrag mit Schemawechsel und Deploy und gehoert nicht in den Termin vom
  08.09. Bis dahin liefert das Protokollblatt die Nachvollziehbarkeit.
- **Rueckimport der Datei.** Siehe Entscheidung 12.
- **Konzernwaehrung und Umrechnung.** Siehe Entscheidung 5.
- **Kosten, Marge und Standardkosten.** Siehe Entscheidung 9.
- **Automatisches Bestaetigen der Vorschlaege.** Ingo hat festgelegt, dass Patrik oder der
  Vertrieb fachlich bestaetigt und niemand das stellvertretend automatisiert tut.

## 7. Abnahmekriterien

- Ein Klick auf `Railway-Export (Excel)` liefert ohne weitere Eingabe eine Datei.
- Blatt `Anleitung` nennt Erzeugungszeitpunkt und die tatsaechlich gewaehlten Filter.
- Jede Zuordnungszeile ist ueber `TSC` plus `Kundennummer` eindeutig.
- Ein Vorschlag ist als `Vorschlag` beschriftet und erscheint in keinem Blatt als
  Railway-Umsatz.
- In `Umsatz_Summen` wird nur innerhalb derselben Waehrung summiert. Ein Kunde mit zwei
  Waehrungen ergibt zwei Zeilen.
- Das Jahr im Export stimmt fuer dieselbe Verkaufszeile mit der Ergebnisansicht der Seite
  ueberein.
- Die TRDE-Luecke steht im Blatt `Datenluecken` und erscheint nirgends als Umsatz von null.
- Alle `173` Vorschlaege aus der produktiven Messung sind im Blatt `Pruefung` wiederfindbar.

## 8. Offene Punkte ausserhalb dieses Knopfes

1. Der deutsche Quell-Export muss einen Kundennamen liefern. Ursache ist die eigene
   Alphaplan-Abfrage, die `RechnungsAdressenID` selektiert, aber nie zu einem Namen
   aufloest. Ohne diesen Namen bleibt Deutschland im Export leer, und genau danach wird am
   08.09. gefragt. Das ist der wichtigste Punkt dieser Liste und unabhaengig vom Exporter
   zu loesen.
2. Patrik oder der Vertrieb muss die `173` Vorschlaege fachlich bestaetigen oder verwerfen.
   Solange das nicht geschehen ist, bleibt das Blatt `Bestaetigt` leer.
3. Ob eine gespeicherte Ablehnung gebaut wird, entscheidet Ingo nach dem Termin.
4. Ob ein Rueckimport gewuenscht ist, entscheidet Patrik nach der ersten Nutzung.

## 9. Stand und naechster Schritt

Umsetzung, Tests und Uebersetzungen sind am 2026-09-01 fertig geworden, `668/668`
Release-Tests gruen, und um 09:21 ist der Stand produktiv ausgeliefert worden.

Offen ist noch der angemeldete Sichtprueflauf mit einer echten heruntergeladenen Datei. Erst
er belegt, dass Patrik die Datei so bekommt, wie sie hier beschrieben ist; die HTTP-`200` auf
`/marktsegmente` belegen nur, dass die Seite erreichbar ist.

Am Deploytag read-only gemessen: `173` Vorschlaege, davon `0` bestaetigt, und `0`
Protokolleintraege zu entfernten Zuordnungen. Die Blaetter `Bestaetigt` und
`Verworfen (Protokoll)` tragen deshalb heute ihren Hinweistext statt Zeilen. Das ist der
erwartete Zustand und kein Fehler; er aendert sich, sobald Patrik die erste Zuordnung
bestaetigt oder verwirft.
