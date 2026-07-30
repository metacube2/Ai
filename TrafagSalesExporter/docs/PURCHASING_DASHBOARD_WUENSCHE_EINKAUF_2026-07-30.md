# Einkaufs-Dashboard — Auswertung Einkaufssitzung 2026-07-30

Quelle: Whisper-Transkript (Modell `large-v3`, Audio `…/einka/Data/audio.wav`), Teilnehmer
Ingo, Marco, Armin. Nachfolgesitzung zu `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`.
Diskussionsstand, keine finalisierte Spezifikation.

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

**Entscheid nötig, bevor Code angefasst wird:**

1. Marco den Reiter Spend-Aufriss zeigen — dann ist der Wunsch ohne eine Zeile Code erledigt, oder
2. die dritte Ebene zusätzlich in die Spend-Matrix hängen (Redundanz, aber Marco bleibt in
   einer Sicht).

Empfehlung: erst zeigen, dann entscheiden. Variante 1 kostet fünf Minuten.

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
Dashboard sei falsch. Zwei Optionen:

- kurzfristig: nach einer Pflegewelle Full Load manuell anstossen (Button ist da,
  `PurchasingDashboard.razor:1745`), und Marco das so kommunizieren;
- saubere Lösung: die Klassifizierungs-Map im Delta auf den **gesamten** Cache anwenden, nicht
  nur auf die geholten Zeilen. **Gegengeprüft:** `LoadMaterialClassificationMapAsync(client,
  baseUrl, ct)` nimmt keine Materialliste als Parameter und ist im Delta (Zeile 126) derselbe
  Aufruf wie im Full Load (Zeile 57) — die Map liegt im Delta also bereits vollständig vor.
  Der Fix ist damit ein zusätzliches UPDATE, kein zusätzlicher SAP-Read.

Letzteres würde die Sitzungsaussage nachträglich wahr machen und ist deshalb der bessere Weg.
**Nicht gemessen** ist die Laufzeit eines nächtlichen Klassifizierungs-Updates über den ganzen
Cache (produktiv `237'217` EKPO-Zeilen, SQLite) — vor der Umsetzung einmal messen, nicht
ungeprüft als „billig" einplanen.

## 3. Zugesagt: Spend nach Währung

Marco (03:44–03:55): „Sollen wir noch eins machen mit nach Währung? … wieviel Umsatz machen
wir in welcher Währung", und 18:47: „das ist auch für die Finanzen dann interessant".

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

Technische Einordnung aus der Sitzung: Das Web-Control kann die oberste Stufe nicht frei
wählbar machen („das Kontrollen vom Web unterstützt das nicht"), deshalb der diskutierte Weg —
je Perspektive ein eigener Kaskadenblock, untereinander scrollbar, notfalls ohne Grafik
(21:44, 23:20). Marco ist damit einverstanden: „es wäre auch easy ohne Grafik".

Anmerkung: Da die Kaskade bereits generisch als `PurchasingSpendCascadeNode` modelliert ist,
ist eine parametrisierte Einstiegsdimension vermutlich billiger als vier duplizierte Blöcke.
Vor dem Bauen prüfen.

## 5. ZLO03-Web — drei separate Punkte

Der grösste Zeitanteil der Sitzung, und der Teil mit echten Fehlern.

### 5a. Mehrfacheingabe: Copy-Paste aus Excel (zugesagt)

Marco (10:28): „Du kommst jetzt aus dem Excel mit 50 Verkaufsnummern. Kannst du die da rein
kopieren, oder musst du wie manuell nach jedem ein Komma machen?" Aktuell: Komma nötig. In SAP
kann man eine Spalte direkt einfügen.

Zusage: „Ich kann jetzt die Logik ändern. Dann kannst du es einfach schön reinkopieren vom
Excel." Umsetzung: Trennzeichen **Komma, Semikolon, Leerzeichen, Tab und Zeilenumbruch**
gleichwertig akzeptieren. Ingos Argument dafür ist tragfähig: „da hast du eh immer
zusammenhängende Nummern" — Materialnummern enthalten keine Leerzeichen.

### 5b. Bug: Mehrfachabfrage liefert kein Ergebnis

Live in der Sitzung reproduziert (12:42–14:34). Mehrere Nummern kommagetrennt, Bottom-Up,
Enter, Laden → Meldung „Full-Node abgeschlossen", danach Status aktualisieren → „hat es nicht
nur gelesen", kein Ergebnis. Ingos Einschätzung: „In diesem Fall hat es Probleme, wenn man
mehrere nicht tut" → „dann lasse ich das anschauen".

**Das ist der wichtigste ZLO03-Punkt**, denn er blockiert 5a: eine bequemere Eingabe nützt
nichts, wenn die Mehrfachabfrage danach nichts liefert. Zuerst 5b, dann 5a.

Zu prüfen: Verhält sich Einzel- vs. Mehrfacheingabe unterschiedlich im Request an
`ZSTR_LZCODE_USAGE`/`_PARENT`? Läuft die Abfrage in einen Timeout, dessen Statusmeldung wie ein
Erfolg aussieht? Die Meldung „Full-Node abgeschlossen" bei leerem Ergebnis ist selbst schon ein
Defekt — ein leeres Resultat darf nicht wie ein abgeschlossener Lauf aussehen.

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

| # | Punkt | Aufwand | Begründung |
| --- | --- | --- | --- |
| 0 | Refresh-Status prüfen: läuft das nächtliche Delta? (Abschnitt 6) | Minuten | wenn nein, sind alle Zahlen auf dem Stand 24.07. — schlägt alles andere |
| 1 | Reiter „Spend-Aufriss" zeigen (Abschnitt 1) | Minuten | Wunsch evtl. schon erfüllt; entscheidet, ob überhaupt gebaut wird |
| 2 | Marco die Full-Load-Bedingung sagen (Abschnitt 2) | Minuten | verhindert eine ins Leere laufende Pflegewelle |
| 3 | ZLO03 Mehrfachabfrage-Bug (5b) | klein–mittel | blockiert die laufende TR5-Aufgabe (Abschnitt 10) |
| 4 | Spend nach Währung (Abschnitt 3) | klein | Felder im Cache vorhanden, kein Full Load nötig |
| 5 | ZLO03 Copy-Paste-Trennzeichen (5a) | klein | erst nach 3 sinnvoll |
| 6 | Delta wendet Klassifizierung auf ganzen Cache an (Abschnitt 2) | klein–mittel | macht die Pflegeschleife über Nacht wirksam |
| 7 | Flexible Einstiegsdimension / Perspektiven (Abschnitt 4) | mittel–gross | die am 24.07. offen gelassene Frage ist jetzt beantwortet |
| 8 | ZLO03-Plausibilisierung abschliessen (5c) | mittel | Vergleich SAP vs. Web ist begonnen, nicht beendet |
| 9 | Wareneingangsdatum klären (Abschnitt 7) | gross | erst Bedarf bestätigen, neue SAP-Quelle nötig |
| 10 | Power-BI-Basisliste (Abschnitt 9) | offen | nur auf Marcos Zuruf |

Unverändert offen aus der Vorsitzung, in dieser Sitzung nicht berührt: **Produktgruppen-Aufriss**
(Referenzliste Disponent → Produktgruppe aus ZC23, Zurechnungsregel bei Mehrfachverwendung) und
die Klärung, welchen konkreten Dashboard-Nutzen ABC/XYZ haben sollen.
