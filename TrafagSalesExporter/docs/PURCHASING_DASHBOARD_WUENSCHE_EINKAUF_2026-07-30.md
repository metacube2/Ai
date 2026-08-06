# Einkaufs-Dashboard — Auswertung Einkaufssitzung 2026-07-30

Quelle: Whisper-Transkript (Modell `large-v3`, Audio `…/einka/Data/audio.wav`), Teilnehmer
Ingo, Marco, Armin. Nachfolgesitzung zu `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`.
Diskussionsstand, keine finalisierte Spezifikation.

## Nachtrag 2026-08-06

Die drei am 2026-07-30 noch offenen fachlichen Punkte sind im Code umgesetzt:

- Produktgruppen-Aufriss als neue Perspektive
  `Produktgruppe -> Lieferant -> Material` ueber ZLO03-Disponent und optionalen
  ZC23-Referenztext;
- summenerhaltende `1/n`-Allokation bei Komponenten, die mehreren
  unterschiedlichen Produktgruppen dienen;
- gemeinsame ABC/XYZ-Massnahmenmatrix mit konkretem Pruefauftrag je Klasse.

Wichtig: Die echten ZC23-Referenztexte liegen noch nicht im Repository. Die GUI
zeigt bis zum Einspielen `Disponent <Code>` und weist fehlende Zuordnungen offen
aus; sie erfindet keine Produktgruppen. Vollstaendige Entscheidung, Regeln und
Abnahmegrenzen:
`docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

Produktivstand: deployed und technisch verifiziert am **2026-08-06 12:31
MESZ**, Funktionscommit `bb009bf`, `435/435` Tests. Die echte ZC23-Map bleibt
die offene fachliche Datenlieferung.

## Umsetzungsstand 2026-07-30 (alles umgesetzt, `346/346` Tests gruen)

| Punkt | Stand | Bemerkung |
| --- | --- | --- |
| Dritte Ebene in der Spend-Matrix (Warengruppe -> Material) | **gebaut** | Entscheid Marco gegen den Verweis auf den Reiter Spend-Aufriss, siehe Abschnitt 1a |
| Volumen nach Waehrung | **gebaut** | eigener Balkenblock im Spend-Reiter, CHF-bewertet + Originalsumme |
| Waehlbare Einstiegsdimension (Perspektiven) | **gebaut** | 5 Perspektiven im Reiter Spend-Aufriss, inklusive Produktgruppe seit 2026-08-06 |
| Delta klassifiziert den ganzen Cache | **gebaut** | Nachpflege wirkt jetzt ohne Full Load, siehe Abschnitt 2 |
| ZLO03: Excel-Paste (Trennzeichen) | **gebaut** | Komma, Semikolon, Leerzeichen, Tab, Zeilenumbruch; Duplikate raus |
| ZLO03: Mehrfachabfrage-Bug | **gebaut (Bypass)** | eine SAP-Anfrage je Nummer statt OR-Gruppe, siehe Abschnitt 5b |
| Refresh-Status pruefen (Nebenbefund) | **offen, nicht Code** | Handgriff im Dashboard, siehe Abschnitt 6 |
| Produktgruppen-Aufriss (ZC23) | **Code gebaut, Referenzdaten offen** | ZLO03/Disponent, 1/n-Allokation und GUI gebaut; echte ZC23-Texte noch einspielen |

Zwei Dinge sind bewusst NICHT gemessen und sollten beim ersten Nachtlauf beobachtet werden:

- **Laufzeit der Nachklassifizierung** ueber den ganzen Cache. Die Staging-Tabelle wird je
  *Material* gefuellt (nicht je Bestellposition), danach laeuft EIN `UPDATE` - der Aufwand haengt
  also an der Zahl der verschiedenen Materialnummern, nicht an den `237'217` Positionen. Trotzdem
  ungemessen: die Meldung des Delta-Laufs nennt jetzt die Zahl der geaenderten Cachezeilen, daran
  laesst sich das im Log ablesen.
- **Ob der Mehrfachabfrage-Bypass am SAP wirklich Treffer liefert.** Die Ursache im ABAP ist nicht
  verifiziert (kein SAP-Zugriff waehrend der Umsetzung); der Bypass umgeht sie, aber der Beweis ist
  ein Lauf mit mehreren Nummern gegen P76.

## Kurzfazit

Der Reiter **Spend** ist fachlich abgenommen. Marco hat live am **Produktivsystem** (nicht Test)
gegengerechnet und die Zahlen als plausibel bestätigt:

| Kennzahl | Wert in der Sitzung | Marcos Reaktion |
| --- | --- | --- |
| Spend total | 21 Mio. | — |
| Bestellungen im Zeitraum | 11 Mio. | „ja, genau" |
| Top-Warengruppe | 3.7 Mio. | „das könnte sehr stimmen" |

Wörtlich: „das sieht eigentlich schon ganz gut aus", „es sieht ja schon mal geil aus",
Armin: „das ist wirklich cool". Marcos Leitplanke „eine Sicht nach der anderen" aus der
Vorsitzung ist damit für Spend erfüllt.

Zwei Punkte wurden zugesagt und am Ende der Sitzung explizit rekapituliert (18:44–19:03):
Spend nach Währung und eine zweite Aufklappstufe. Dazu kam ein dritter, grösserer Wunsch
(flexible Einstiegsdimension) und eine Reihe von ZLO03-Themen.

## 1. Befund vor dem Umsetzen: die zweite Aufklappstufe ist schon gebaut

Marco (07:18–07:53, 19:34): nach dem Lieferanten noch eine Stufe tiefer, „nochmal abtauchen",
Ziel „identifizieren, wo wir noch Sachen nicht klassifiziert haben oder falsch sind".
Nächste Stufe = Artikel-/Materialnummer bzw. Komponenten. Ingo in der Sitzung: „Ich weiss aber
nicht, ob das geht … Das ist notiert."

**Es geht — es existiert bereits.** `PurchasingSpendCascadeNode`
(`Models/PurchasingSectionModels.cs:47`) implementiert die feste Kaskade
**Lieferant → Warengruppe → Artikel**, gebaut und deployed am 2026-07-24 (Commit `4e7861d`),
als eigener Reiter `/einkauf/aufriss` „Spend-Aufriss" (`PurchasingSpendExplorer.razor`),
Top-N je Ebene `[40,15,10]` plus „übrige (n)"-Restzeile, damit Elternsumme = Summe der Kinder
bleibt.

Die Sitzung fand ausschliesslich im Reiter **Spend** statt („Jetzt sind wir auf dem Spend",
01:03), und dort hat die Lieferantenmatrix bewusst nur **eine** Aufklappebene
(Lieferant → Warengruppe, `PurchasingSupplierYearSpendRow.MaterialGroups`). Der Reiter
Spend-Aufriss wurde nicht geöffnet — 19:14 „wenn das passt, kommen wir zum nächsten Reiter",
danach ging es um ZLO03.

**Gegengeprüft, weil es die Empfehlung kippen könnte:** Zeigen die beiden Sichten dieselbe
Warengruppe? Die Vorgänger-Doku notiert für die Kaskade „nutzt vorhandene Cache-Daten
(Beleg-WG/Matnr)", was auf die Belegwarengruppe hindeutet — dann würden Spend-Reiter
(`MaraMatkl` bevorzugt) und Spend-Aufriss für denselben Lieferanten unterschiedliche
Warengruppen anzeigen, was in einer Dummy-Suche fatal wäre. **Ist nicht so:**
`ExecuteSpendCascadeRowsAsync` verwendet
`COALESCE(NULLIF(p.MaraMatkl,''), NULLIF(p.Matkl,''), 'ohne Warengruppe')`
(`Services/PurchasingDashboardService.cs:985`) — zeichengleich mit der Spend-Matrix. Die
Formulierung in der 07-24-Doku bezog sich auf die damalige Datenlage, nicht auf das SQL. Beide
Sichten sind konsistent.

### 1a. Entscheid Marco: dritte Ebene direkt in die Spend-Matrix (umgesetzt)

Zur Wahl standen: (1) Marco den Reiter Spend-Aufriss zeigen, damit ohne Code fertig, oder (2) die
dritte Ebene zusätzlich in die Spend-Matrix des Spend-Reiters hängen. **Marco hat Variante 2
entschieden** und dabei das Zielbild genau beschrieben: „BEPRO AG" aufklappen → `01 - Dummy`,
diese Zeile wieder aufklappen → `MAT-123`, `MAT-2322`, dann die nächste Warengruppe.

Umgesetzt:

- `PurchasingSpendGroupYearRow.Articles` (`Models/PurchasingSectionModels.cs`) trägt die
  Materialebene, gefüllt aus `ExecuteSupplierGroupArticleYearRowsAsync`.
- Deckelung **25 Materialien je Warengruppe** plus `uebrige (n)`-Restzeile. Bewusst höher als die
  10 der Aufriss-Kaskade: hier wird gezielt eine Warengruppe geöffnet, nicht der ganze Baum, und
  für die Dummy-Suche soll die Liste nicht nach zehn Zeilen abbrechen. Die Restzeile summiert auch
  **je Jahr**, damit die Jahresspalten aufgehen und nicht nur die Gesamtspalte.
- Aufklapp-Schlüssel enthält den Lieferanten, sonst würde `01 - Dummy` bei allen Lieferanten
  gleichzeitig aufgehen.
- Gegengeprüft: gleiche Warengruppen- und Artikellogik wie die Aufriss-Kaskade, beide Sichten
  zeigen also dieselben Zahlen.

Nebenbefund dabei behoben: Der Hinweistext über der Matrix behauptete noch „Warengruppe aktuell aus
dem Bestellbeleg; Umstellung auf MARA-MATKL folgt, sobald SAP das Feld liefert". SAP liefert es seit
dem Full Load vom 24.07.; der Text war überholt und für Marco direkt irreführend.

## 2. Wichtigster Punkt: Nachpflegen wirkt erst nach einem Full Load

Marco (24:35): „wenn man jetzt die fehlenden Materialgruppen in den Materialstämmen
nachpflegen würde — würde er es dann wieder aktualisiert lesen, oder hat er eine statische
Liste im Hintergrund?" Antwort in der Sitzung: „Nein, es wird sich auch immer aktualisieren,
also das ist dann dynamisch."

**Konsequenz, die Marco kennen muss: pflegt der Einkauf nach, ändert sich im Dashboard
zunächst nichts.** Keine statische Liste — so weit stimmt die Antwort. Aber die Warengruppe
kommt aus der Cache-Spalte
`PurchasingEkpoCache.MaraMatkl` (`COALESCE(NULLIF(p.MaraMatkl,''), NULLIF(p.Matkl,''),
'ohne Warengruppe')`, u. a. `Services/PurchasingDashboardService.cs:369`), und diese Spalte wird
nur beim Laden geschrieben:

- **Nächtlich** läuft `RunPurchasingDeltaAsync` → `RunDeltaAsync`
  (`Services/TimerBackgroundService.cs:108-131`), **nicht** ein Full Load.
- Das Delta holt EKPO nur für `changedEbelns` (Filter `Aedat ge <Datum>`) plus die Belege mit
  offener Menge, und `UpsertEkpoAsync` wendet die Klassifizierungs-Map **ausschliesslich auf
  diese geholten Zeilen** an (`Services/PurchasingDataRefreshService.cs:101-137`).

Praktische Folge: Ein Material, das nur auf **alten, abgeschlossenen** Bestellungen vorkommt,
behält seine bisherige Warengruppe im Cache — auch nach Pflege im Materialstamm. Und genau
solche Materialien sind der Dummy-Fall, um den es Marco geht.

Sonst pflegt der Einkauf, schaut nach, sieht keine Änderung und schliesst fälschlich, das
Dashboard sei falsch.

### Umgesetzt: das Delta klassifiziert jetzt den ganzen Cache

`ApplyMaterialMasterToWholeCacheAsync` schreibt Warengruppe, Materialstatus, ABC und XYZ auf **alle**
EKPO-Cachezeilen, nicht nur auf die geholten Belege. Damit ist die Sitzungsaussage nachträglich wahr:
nachgepflegte Warengruppen kommen über Nacht an, ohne Full Load.

Wichtig war die Feststellung, dass **kein zusätzlicher SAP-Read** entsteht: `LoadMaterialStatusMapAsync`
und `LoadMaterialClassificationMapAsync` nehmen keine Materialliste als Parameter und sind im Delta
dieselben Aufrufe wie im Full Load — beide Maps liegen also schon vollständig vor. (Die Warengruppe
kommt übrigens aus der Status-Map, nicht aus der Klassifizierungs-Map; letztere trägt nur ABC/XYZ.)

Umsetzungsdetails, die beim Nachlesen zählen:

- Über eine temporäre Staging-Tabelle und **ein** `UPDATE`, nicht ein Statement je Zeile. Die Staging
  wird aus den im Cache **vorhandenen** Materialnummern gebaut, nicht aus den ~68'000 Map-Einträgen,
  und die Zielwerte mit denselben `Resolve*`-Funktionen wie der Upsert ermittelt. Letzteres ist keine
  Kosmetik: `NormalizeMatnr` entfernt führende Nullen, der Cache hält aber die zero-padded Form
  (`000000000000002217`) — ein in SQL nachgebauter Join hätte nichts gefunden und die Warengruppe
  sogar leergeschrieben. Genau das hat ein Test aufgedeckt.
- Die `WHERE`-Klausel aktualisiert nur Zeilen, bei denen sich wirklich etwas ändert, sonst würden
  jede Nacht alle Cachezeilen umgeschrieben. Der Delta-Statustext nennt jetzt die Zahl der
  geänderten Zeilen.
- Schutz gegen den teuren Fehlerfall: sind **beide** Maps leer (fehlgeschlagener Stammdaten-Read),
  wird nichts angefasst — sonst würde ein Leselehler die vorhandenen Warengruppen flächendeckend
  leerschreiben.
- Ist die Map befüllt, das Material aber nicht enthalten, gilt der Stamm als führend und die
  Stammgruppe wird geleert, damit im Dashboard der `COALESCE`-Fallback auf die Beleg-Warengruppe
  greift statt eine veraltete Stammgruppe zu zeigen.
- Positionen ohne Materialnummer (gekontierte Bestellungen — eine der beiden Dummy-Ursachen aus
  Abschnitt 6) werden nicht angefasst.

**Nicht gemessen:** die Laufzeit im Nachtlauf. Der Aufwand hängt an der Zahl der verschiedenen
Materialnummern, nicht an den `237'217` Positionen, und das `UPDATE` ist eines statt vieler — aber
gemessen ist es nicht. Beim ersten Nachtlauf die Delta-Meldung im Log ansehen.

Der manuelle Full-Load-Button (`PurchasingDashboard.razor:1745`) bleibt als schnellerer Weg, wenn
Marco nicht bis zum Nachtlauf warten will.

## 3. Zugesagt: Spend nach Währung

Marco (03:44–03:55): „Sollen wir noch eins machen mit nach Währung? … wieviel Umsatz machen
wir in welcher Währung", und 18:47: „das ist auch für die Finanzen dann interessant".

**Umgesetzt** als Balkenblock „Volumen nach Waehrung" im Spend-Reiter, neben Warengruppe und
Beschaffungsregion. Anzeige: CHF-bewerteter Betrag (Balkenlänge und erste Zahl), dahinter in Klammern
die Summe in der Belegwährung selbst — also das tatsächliche Währungsexposure. Bei CHF-Belegen
entfällt die Klammer, weil sie eine Wiederholung wäre. Belege ohne Währungskennzeichen laufen unter
„ohne Waehrung" und werden **nicht** stillschweigend zu CHF gezählt, sonst wäre eine Datenlücke als
Schweizer Volumen getarnt.

Der Hinweistext unter dem Block nennt ausdrücklich die Abgrenzung zur Beschaffungsregion, weil genau
die in der Sitzung für Verwirrung sorgte.

Auslöser war eine Präzisierung, die ins Doku gehört, weil sie leicht falsch gelesen wird:
**Beschaffungsregion ≠ Währung.** BIPRO ist Beschaffungsregion Schweiz, wird aber in EUR
fakturiert. Marco: „das hat nichts mit nach Währung zu tun" — richtig, die Region kommt aus dem
Lieferantenland (`LFA1.Land1` → `SupplierCountry`).

**Aufwand: gering.** `PurchasingEkkoCache` führt `Waers` (Belegwährung) und `Wkurs` bereits —
sie werden für die CHF-Bewertung genutzt (`ChfValueSql("p.Netwr","k.Waers","k.Wkurs")`,
`Services/PurchasingDashboardService.cs:44-65`), und das Delta liest `Waers,Wkurs` mit
(Zeile 101). Es braucht **kein** SAP-Feld und keinen Full Load — nur einen weiteren
`ExtraCharts`-Block „Volumen nach Währung" mit `GROUP BY k.Waers`. Der generische
`ExtraCharts`-Mechanismus ist seit 2026-07-23 genau dafür da.

Offene Detailfrage: Anzeige in **CHF bewertet** (vergleichbar, konsistent mit allen anderen
Blöcken) oder in **Originalwährung** (zeigt das echte Währungsexposure)? Für Marcos Zweck
(„was für ein Einkaufsvolumen haben wir je Währung") ist der CHF-Wert je Währungsgruppe das
Richtige; die Originalsumme kann als zweite Spalte daneben. Ohne Rückfrage so gebaut.

## 4. Flexible Einstiegsdimension — die offene Frage vom 24.07. ist beantwortet

Am 2026-07-24 wurde dieser Punkt **bewusst nicht** gebaut, mit der Begründung „Question 2 der
Rückfrage unbeantwortet → klare Lesart (feste Kaskade) genommen; sauberer Folgeschritt"
(Vorgänger-Doku, Zeile 28). **Diese Rückfrage hat Marco jetzt von selbst und ausführlich
beantwortet** (21:19–24:07):

> „Gibt es die Möglichkeit, dass ich die Produktgruppe habe und dann schaue, welche
> Lieferanten und was für Materialnummern — dass ich wie quasi den hierarchischen Aufriss
> wählen kann."

Sein Zielbild, wörtlich „Perspektiven": Lieferant / Beschaffungsregion / Warengruppe /
Währung — und aus jeder Perspektive weiter kaskadieren, z. B. Beschaffungsregion → Lieferant →
Warengruppe → Material. „Für jede Anforderung, die du hast, könntest du dann ins entsprechende
Feld reingehen und entsprechend auftröseln."

Das deckt sich exakt mit der Perspektiven/Aufriss-Trennung, die in der Vorsitzung schon
festgehalten wurde (Vorgänger-Doku, Abschnitt „Grundkonzept"). Zusage in der Sitzung: „Nein,
das kann ich noch einbauen, das ist kein Problem."

**Umgesetzt — und billiger als der in der Sitzung diskutierte Weg.** Diskutiert war, je Perspektive
einen eigenen, untereinander scrollbaren Kaskadenblock zu bauen („das Kontrollen vom Web unterstützt
das nicht", 21:44/23:20). Nötig war das nicht: die Kaskade ist generisch parametriert, es gibt
weiterhin **eine** Tabelle plus einen Umschalter.

| Perspektive | Ebenenfolge | Deckelung je Ebene |
| --- | --- | --- |
| Lieferant (Standard) | Lieferant → Warengruppe → Material | 40 / 15 / 10 |
| Beschaffungsregion | Region → Lieferant → Warengruppe → Material | 12 / 15 / 10 / 8 |
| Warengruppe | Warengruppe → Lieferant → Material | 20 / 15 / 10 |
| Währung | Währung → Lieferant → Warengruppe → Material | 8 / 15 / 10 / 8 |

Die Region-Kette ist genau Marcos Beispiel („nach Beschaffungsregion, dann Lieferant, dann
Warengruppen und wieder Material"). Umsetzungsdetails:

- `SpendPerspective`/`SpendDimension` in `PurchasingDashboardService` halten SQL-Ausdruck,
  Beschriftung und Deckelung je Ebene; die SQL-Definition bleibt bewusst im Service, die UI bekommt
  über `PurchasingSpendPerspectiveResult` nur Schlüssel, Beschriftungen und Baum.
- Deckelungen sind je Perspektive eigen: bei wenigen Einstiegswerten (Währung, Region) darf die
  erste Ebene klein und die Tiefe grosszügiger sein, bei vielen (Lieferant) umgekehrt.
- Alle Perspektiven werden **beim Datenladen vorberechnet**, das Umschalten kostet also keine
  DB-Runde. Preis: vier SQL-Groupings statt einem pro Laden.
- Die Anzeige wickelt den Baum in C# zu einer flachen Zeilenliste ab (`BuildVisibleRows`), statt die
  Ebenen im Markup zu verschachteln — die Perspektiven sind unterschiedlich tief (3 bzw. 4 Ebenen),
  eine feste Verschachtelung könnte das nicht abbilden.
- Beim Perspektivenwechsel wird der Aufklappzustand verworfen: die Schlüssel tragen den Pfad der
  alten Dimensionsfolge.
- Der Spaltenkopf zeigt die Ebenenfolge („Beschaffungsregion > Lieferant > Warengruppe > Material"),
  damit nach dem Umschalten klar ist, was man aufreisst.

## 5. ZLO03-Web — drei separate Punkte

Der grösste Zeitanteil der Sitzung, und der Teil mit echten Fehlern.

### 5a. Mehrfacheingabe: Copy-Paste aus Excel (zugesagt)

Marco (10:28): „Du kommst jetzt aus dem Excel mit 50 Verkaufsnummern. Kannst du die da rein
kopieren, oder musst du wie manuell nach jedem ein Komma machen?" Aktuell: Komma nötig. In SAP
kann man eine Spalte direkt einfügen.

**Umgesetzt** (`MaterialUsageDataRefreshService.ParseMaterialTokens`): Trennzeichen **Komma,
Semikolon, Leerzeichen, Tab und Zeilenumbruch** gleichwertig, Duplikate entfernt (in der Sitzung
genau passiert — „Hast du mehrmals das gleiche kopiert?"; jedes Duplikat wäre sonst eine SAP-Anfrage
ohne neue Zeilen). Das Argument für den Whitespace-Split ist tragfähig: „da hast du eh immer
zusammenhängende Nummern" — Materialnummern enthalten keine Leerzeichen.

Das Eingabefeld ist jetzt mehrzeilig (3 Zeilen, wächst mit) und zeigt daneben, wie viele Nummern
erkannt wurden — nach einem Excel-Einfügen sieht man damit sofort, ob richtig zerlegt wurde.
Die Bereichsschreibweise `35-40` bleibt unberührt; `35 - 40` mit Leerzeichen zerfällt dagegen in drei
Tokens, das ist bewusst nicht unterstützt.

Auch das Suchfeld über dem Cache akzeptiert jetzt dieselben Trennzeichen, damit man die gerade
geladene Spalte zum Filtern wiederverwenden kann statt Nummer für Nummer zu suchen.

### 5b. Bug: Mehrfachabfrage liefert kein Ergebnis

Live in der Sitzung reproduziert (12:42–14:34). Mehrere Nummern kommagetrennt, Bottom-Up,
Enter, Laden → Meldung „Full-Node abgeschlossen", danach Status aktualisieren → „hat es nicht
nur gelesen", kein Ergebnis. Ingos Einschätzung: „In diesem Fall hat es Probleme, wenn man
mehrere nicht tut" → „dann lasse ich das anschauen".

**Umgesetzt als Bypass, nicht als Ursachenbehebung** — und das ist eine bewusste Entscheidung, die
man kennen muss.

Bisher baute `BuildMaterialClause` aus mehreren Nummern EINE gemeinsame OR-Gruppe:
`Richtung eq 'BOTTOMUP' and (Kompnr eq 'A' or Kompnr eq 'B')`. Bei einer Nummer entsteht die
einfache Form ohne Klammer — und die funktioniert. Die naheliegende Vermutung ist, dass die gemischte
`and`/`or`-Struktur bei der Umwandlung in Select-Options im Gateway nicht ankommt und der
selbstgeschriebene `GET_ENTITYSET` mit einer leeren Range-Tabelle arbeitet. **Verifiziert ist das
nicht** — für einen Gegentest am ABAP fehlte während der Umsetzung der SAP-Zugriff.

Statt auf die Ursache zu setzen, stellt der Aufrufer jetzt **eine eigene Anfrage je Nummer**
(`BuildMaterialClauses`) und führt die Ergebnisse zusammen. Das umgeht die Umwandlung vollständig,
unabhängig davon, wo sie schiefgeht. Zwei Nebeneffekte, die es ohnehin rechtfertigen:

- **Trefferzahl je Nummer.** Nummern ohne Verwendung werden namentlich in der Statusmeldung genannt
  (gedeckelt auf 20 plus „+n weitere"). Für die TR5-Aufgabe aus Abschnitt 10 — welche Komponenten
  werden nirgends verbaut? — ist genau diese Liste das eigentliche Ergebnis, nicht ein Nebenprodukt.
- **Keine URL-Längengrenze.** 50 Nummern à 18 Stellen in einem `$filter` wären ~1'500 Zeichen; das
  Gateway kann dabei eigene Grenzen haben.

Deduplizierung über `(Richtung, Vknr, Kompnr)`, weil sich zwei Bereichsangaben überlappen können und
`MaterialUsageCache` per `INSERT` (nicht `UPSERT`) gefüllt wird — ohne das läge dieselbe Zeile
mehrfach im Cache.

Ebenfalls behoben: **die falsche Erfolgsmeldung.** „Full Load abgeschlossen" bei 0 Zeilen sah nach
Erfolg aus. Jetzt zeigen Alert und Snackbar in diesem Fall **Warnung statt grün**, und der bisher nur
für Top-Down vorhandene 0-Zeilen-Hinweis gibt es auch für Bottom-Up — genau Marcos Fall in der
Sitzung. Der Bottom-Up-Text weist ausdrücklich darauf hin, vor dem Schluss „wird nirgends verbaut"
mit „Auch gelöschte Materialien" gegenzuprüfen.

**Offen bleibt der Beweis:** ein Lauf mit mehreren Nummern gegen P76. Falls er weiterhin leer
bleibt, liegt es nicht an der Filterstruktur und die Ursache ist im ABAP zu suchen.

### 5c. Plausibilisierung Bottom-Up (Zweifel von Marco)

Marco (11:26–12:14): er habe ZLO03 zuletzt wenig genutzt, „ich hatte das Gefühl, dass das
Resultat nicht konsistent gewesen ist" — er habe Komponenten in Stücklisten erwartet und nicht
gefunden. Ausdrücklich als Gefühl deklariert, nicht als Befund.

Antwort in der Sitzung: „Doch, sind es" konsistent, „zu 98 % müsste es stimmen, aber es kann
sein, dass in der Textposition oder irgendetwas nicht drin ist". Zusage: „ich kann es mal
plausibilisieren".

Der Vergleichstest wurde in der Sitzung **begonnen und nicht abgeschlossen** — in SAP wurde
ZLO03 mit denselben Nummern gestartet (F8, Job `JS27` erzeugt, gelbe Markierung der Zeilen mit
Komponenten, 15:07–16:15), der Abgleich gegen die Web-Ausgabe steht aber noch aus: „dann kann
man vergleichen mit der Webseite, ob das gleich ist. Müssen ja genau gleich sein."

**Offen: diesen Vergleich fertig durchführen und das Ergebnis dokumentieren.** Solange Marco
den Zweifel hat, nutzt er das Werkzeug nicht — unabhängig davon, ob der Zweifel berechtigt ist.

## 6. Dummy-Warengruppen und die 94'000

Marco (04:14–06:44, 07:11): „diese Dummy-Geschichte mit diesen Dummy-Warengruppen", er will
wissen, „wo die 94'000 herkommen". Beim Durchklicken sichtbar: Warengruppe **`01`**
(„0-1, Warengruppe 0-1"), Positionen wie Materialkosten, Prüfadapter, Arbeitsaufwände.

Marcos eigene Erklärungshypothesen aus der Sitzung, beide plausibel:

- **Gekontierte Bestellungen** — Bestellungen ohne Materialstammbezug tragen keine
  Warengruppe aus dem Stamm.
- **Anfangsjahre** — alte Belege ohne gepflegte Warengruppe.

### Am Produktivstand gegengeprüft

Read-only gegen `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db`
(Stand 2026-07-30 12:28, `237'217` EKPO- / `175'355` EKKO-Zeilen), Verteilung `MaraMatkl`:

| `MaraMatkl` | Zeilen | Anteil |
| --- | --- | --- |
| (leer) | 45'861 | 19.3 % |
| **`01`** | **36'295** | **15.3 %** |
| 20.01.01 | 17'974 | 7.6 % |
| 10.08.00 | 14'124 | 6.0 % |
| 20.03.01 | 13'979 | 5.9 % |

**Marcos Lesart ist damit bestätigt:** `01` ist nach „leer" der zweitgrösste Topf und keine
Fehlinterpretation des Transkripts. Zusammen sind „leer" und `01` **34.6 % aller
Bestellpositionen** ohne verwertbare Warengruppe. Das deckt sich mit dem Stand vom 2026-07-23
(`MARA-MATKL` damals 65 % leer, 24 % `01`, ~10 % echt) — der Full Load vom 24.07. hat den
Füllgrad verbessert, die Struktur des Problems aber nicht verändert.

Das `01` ist also kein Dashboard-Artefakt, sondern die Dummy-Gruppe im SAP-Materialstamm.
**Es ist ein SAP-Stammdatenthema, kein Dashboard-Thema** — genau so war es schon in der
Vorsitzung festgehalten.

**Marcos konkrete Zahl 94'000 konnte ich nicht zuordnen** — die Spend-Summen-Abfragen gegen die
Produktiv-DB wurden von der Werkzeug-Berechtigungsschicht blockiert. Nachzuholen mit:
`SELECT p.MaraMatkl, SUM(CAST(p.Netwr AS REAL)) FROM PurchasingEkpoCache p JOIN
PurchasingEkkoCache k ON k.Ebeln=p.Ebeln WHERE k.Bedat>='2026-01-01' GROUP BY p.MaraMatkl`.
Ob 94'000 der `01`-Topf, ein einzelner Lieferant oder eine Position darin war, ist offen.

### Nebenbefund, der geprüft werden sollte

`MAX(EKKO.Bedat)` im Produktiv-Cache ist **2026-07-24** — genau der Tag des Full Loads. Seit
sechs Tagen also keine neuere Bestellung im Cache, obwohl das nächtliche Delta laufen sollte.
Das kann harmlos sein (Bestellungen ohne neues `Bedat`), sieht aber nach einem nicht
greifenden Delta aus. `PurchasingSyncState` liess sich nicht auslesen (ebenfalls blockiert) —
**bitte im Dashboard den Refresh-Status ansehen**: steht dort ein erfolgreiches Delta von
heute Nacht? Falls nicht, ist das dringender als alle Wünsche in diesem Dokument, weil dann
das ganze Einkaufs-Dashboard auf dem Stand vom 24.07. eingefroren ist.

Was das Dashboard beitragen kann und was Marco auch genau so formuliert hat: über den Drilldown
bis zur Materialnummer die betroffenen Positionen sichtbar machen, damit der Einkauf gezielt
nachpflegen kann. „Wenn du abspringen kannst auf Materialnummer, wenn im Drilldown die
Materialnummer drin ist, dann findest du einen." Das ist derselbe Bedarf wie Punkt 1 — und
damit ist Punkt 1 der Schlüssel zu Punkt 6.

Reihenfolge daraus: **erst Punkt 1 klären (Reiter zeigen), dann pflegt der Einkauf, dann Punkt 2
(Full Load / Delta-Fix), dann sieht Marco die Wirkung.** In dieser Reihenfolge, sonst läuft die
Pflegewelle ins Leere.

## 7. Belegdatum vs. Wareneingangsdatum (offen, nicht zugewiesen)

Marco (05:20): „das ist das Belegdatum, nicht das Wareneingangsdatum. Das ist ja, glaube ich,
das Problem, das wir immer haben, oder?" Bestätigt mit „ja, genau", danach nicht weiter
verfolgt.

Sachstand: Das Dashboard periodisiert durchgehend auf **`EKKO.Bedat`** (Belegdatum), siehe
`ekkoPeriod`/`joinedEkkoPeriod` in `Services/PurchasingDashboardService.cs:206-207`. Das ist
für „Spend nach Bestelldatum" korrekt und war so abgestimmt.

Ein Wareneingangs-Zeitbezug wäre eine **eigene Kennzahl**, keine Korrektur: die Mengen dazu
sind über `EKET.Wemng` im Cache vorhanden (wird im Delta bewusst nachgeladen, siehe
`LoadOpenOrderEbelnsAsync`), das **Wareneingangsdatum** selbst aber nicht — `EKET.Eindt` ist das
geplante Lieferdatum, nicht das Ist. Dafür bräuchte es `EKBE`/`MSEG` (Bestellhistorie) als
zusätzliche Quelle.

**Zu klären mit Marco:** Ist das ein echter Bedarf („Spend nach Wareneingang") oder war es eine
Randbemerkung? Der Aufwand ist deutlich grösser als bei allen anderen Punkten hier (neue
SAP-Quelle), deshalb nicht ohne explizite Priorisierung anfangen.

## 8. Sortiments- und Lebenszyklusgüter

Marco (08:41): Frage nach „Sortiments- und Lebenszyklusgütern". Antwort: „das habe ich bei
Logistik drin, da ist ein ZLO03 abgebildet", plus Zusage „das würde ich auch noch verknüpfen
miteinander, das bin ich auch dran" — Idee: die Informationen der vererbten Materialien rauf
und runter nutzbar machen.

Status: **in Arbeit**, keine neue Anforderung aus dieser Sitzung. Bedingung, die in der Sitzung
richtig benannt wurde: „ich muss mal schauen, dass alles stimmt mit dem ZLO03. Wenn es dort
passt, dann stimmt es ja dann da auch." Hängt also an Punkt 5c.

## 9. Power-BI-Basisliste: Authentifizierung defekt (niedrige Priorität)

Marco (20:12–21:17): Die frühere Basisliste, mit der er selbst herumspielen konnte, lässt sich
nicht mehr aktualisieren — „beim Aktualisieren ist gekommen, dass wir nicht mehr
authentifizieren konnten". Konkreter Anlass: Florian zeigen, wie sich die Preise von
Keramikzellen entwickeln.

Bewusst zurückgestellt („ich habe es nicht mehr geschaut, weil ich das jetzt hier abgebildet
habe"), aber Angebot steht: „wenn du sagst, du willst spezielle Sachen fahren, dann kann ich
schon anschauen, warum es nicht mehr kommt". Marco hat nicht darauf bestanden.

Nebenbefund fürs Argumentarium, in der Sitzung von Ingo gebracht und unwidersprochen: Für das
Dashboard braucht niemand eine Power-BI-Lizenz; Power BI bleibt nur für Einzelanalysen ohne
allgemeines Interesse nötig.

## 10. Fachlicher Hintergrund, warum ZLO03 Bottom-Up zählt

Nicht Dashboard, erklärt aber die Dringlichkeit von Punkt 5 (16:55–18:15):

Marco hat mit Ann-Kathrin einen konkreten Fall zur **TR5-Umstellung**. Es gibt **1'563
Komponenten, die in den letzten 5 Jahren weniger als 100-mal gebraucht wurden**, teilweise
schon auf Status 98. Zwei Fragen dazu:

1. Zu welchen **Verkaufsnummern** gehören diese Komponenten — lohnt sich die weitere
   Bewirtschaftung?
2. Für Ann-Kathrin: Kann man diese Typen **von der TR5-Migration ausnehmen**?

Beides ist genau der ZLO03-**Bottom-Up**-Verwendungsnachweis über eine grössere Liste von
Nummern — also 5a und 5b zusammen. Damit ist der Mehrfacheingabe-Bug nicht Komfort, sondern
blockiert eine laufende Aufgabe. Das hebt 5b in der Priorität.

Armins Vorschlag für die Typidentifikation war der **Disponent**; Beispiel aus der Sitzung, dass
Bezeichnungen allein nicht reichen („Navitrack ist ein uraltes Produkt"). Passt zum bereits
verifizierten `VknrDispo`-Feld aus der Vorsitzung.

## 11. Priorisierung (Vorschlag zur Abstimmung mit Marco)

Die Code-Punkte sind umgesetzt (siehe Umsetzungsstand oben). Was noch offen ist, in dieser
Reihenfolge:

| # | Punkt | Aufwand | Begründung |
| --- | --- | --- | --- |
| 1 | **Refresh-Status prüfen: läuft das nächtliche Delta?** (Abschnitt 6) | Minuten | `MAX(Bedat)` = Tag des Full Loads. Wenn das Delta nicht greift, sind alle Zahlen auf dem Stand 24.07. — schlägt alles andere |
| 2 | Delta-Meldung im Log ansehen: Laufzeit und Zahl der nachklassifizierten Zeilen | Minuten | die eine ungemessene Grösse am Delta-Fix |
| 3 | ZLO03 mit mehreren Nummern gegen P76 laufen lassen | Minuten | Beweis, dass der Bypass aus 5b greift |
| 4 | ZLO03-Plausibilisierung abschliessen (5c) | mittel | Vergleich SAP vs. Web ist begonnen, nicht beendet — solange Marco zweifelt, nutzt er das Werkzeug nicht |
| 5 | Wareneingangsdatum klären (Abschnitt 7) | gross | erst Bedarf bestätigen, neue SAP-Quelle (`EKBE`/`MSEG`) nötig |
| 6 | Produktgruppen-Aufriss (ZC23-Referenzliste + Zurechnungsregel) | gross | unverändert aus der Vorsitzung, grösster Restposten |
| 7 | Power-BI-Basisliste (Abschnitt 9) | offen | nur auf Marcos Zuruf |

Unverändert offen aus der Vorsitzung, in dieser Sitzung nicht berührt: **Produktgruppen-Aufriss**
(Referenzliste Disponent → Produktgruppe aus ZC23, Zurechnungsregel bei Mehrfachverwendung) und
die Klärung, welchen konkreten Dashboard-Nutzen ABC/XYZ haben sollen.

Der vorstehende Abschnitt dokumentiert die Priorisierung **zum Sitzungsende
2026-07-30**. Der aktuelle Umsetzungsentscheid vom 2026-08-06 steht im Nachtrag
am Dateianfang und in
`docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.
