# Operations

Der Reiter Operations bündelt die Steuerung der Produktion am Standort Bubikon. Unter **KPI** stehen die Kennzahlen der Operations, zuerst die Liefertreue der Trafag AG (Verkaufsorganisation 1100) mit OTD, OTIF und OTP. Unter **Shopfloor** liegt das digitale Shop-Floor-Board, das die Excel-Datei „PPA-Shop-Floor-File“ ersetzt. Dort stehen Pendenzen, Abteilungswerte, Rückstandslisten, Fehlteile, Lieferzeiten, Forecast und die Auslastungsgrafik für das tägliche Shop-Floor-Meeting. Hauptnutzer sind die Leitung Operations und Supply Chain, die Produktionsplanung (PPD), die Abteilungs- und Teamleiter der Produktion sowie Einkauf, Verkauf, Logistik und Unterhalt. Seit dem 16.10.2026 ist das Shopfloor für alle Windows-Benutzer ohne eigene Anmeldung offen; eine Administratorin oder ein Administrator kann die Anmeldung (Benutzer „operations“) wieder einschalten. Die Liefertreue kommt monatlich direkt aus SAP. Das Shopfloor wird werktags um 04:00 und 12:45 Uhr aus SAP ergänzt (Fehlteile ZD05, Plan- und Fertigungsaufträge, Logistik-Mengen, Tagesmengen SEH, TR5 und seit 19.10.2026 DW, FPY SEH) und sonst im Meeting von Hand gepflegt.

## Schnellüberblick

### Wozu

Diese Seite zeigt auf einen Blick, wie Operations zusammenhängt: woher die Zahlen kommen, welche Seite welche Frage beantwortet, wie die Seiten untereinander verbunden sind und wie aus dem Meeting Massnahmen werden, die wieder in SAP wirken. Wer nur fünf Minuten hat, liest diesen Abschnitt.

![Operations: Prozess und Datenfluss. Links die Quellen (SAP, MES, Eingaben von Hand), in der Mitte das Cockpit mit den Seiten und ihren Verbindungen, rechts das tägliche Meeting und die Rückwirkung auf SAP](bilder/operations_prozess.svg)

### Was man sieht

| Seite | Zeigt | Quelle | Pflegt | Wichtigste Frage |
|---|---|---|---|---|
| Startseite | Ablauf, erfasst ja/nein, Ampelpunkte, Pendenzen, Rückstände, ZD05 | Board | alle | Wo brennt es heute? |
| Abteilungen SEH, TR5, TX, DW | Vorrat, Tagesmenge, Ziel, Personal, FPY, Lieferzeiten | SAP, MES, von Hand | Teamleiter | Haben wir gestern geschafft, was wir wollten? |
| Ampel | Grün, Orange, Rot je Bereich mit Erfüllung | von Hand | Teamleiter | Wer braucht heute Hilfe? |
| Lieferzeiten | Lieferzeit je Produktgruppe gegen Ziel, Grund | von Hand | Teamleiter | Halten wir unser Versprechen an den Markt? |
| PPD | Menge in Verzug, Terminverschiebungen, Prio-Liste | von Hand (SAP geplant) | Disposition | Wird der Verzug kleiner? |
| Rückstandslisten | Kundenpositionen mit Ursache und neuem Termin | von Hand (SAP geplant) | Disposition | Welche Kunden warten, und warum? |
| Logistik | verpackte Menge gegen Tagesmenge | SAP (ZC27) | automatisch, Personal von Hand | Stauen sich fertige Geräte vor dem Versand? |
| Einkauf ZD05 | Fehlteile mit Code und Termin, Bewertungskennzahl | SAP | Einkauf | Ist die Versorgung im Griff? |
| Verkauf | Auftragseingang, Umsatz, Go-for-it, Book to Bill | von Hand (SAP geplant) | Verkauf | Kommt genug Arbeit herein? |
| Forecast und PPA-Grafik | Menge gegen Kapazität je Tag, Vorrat über 26 Wochen | SAP und Board | PPD | Reicht das Personal nächste Woche? |
| Listen | Pendenzen, TVAV, Angebote, CZ ↔ CH, Einkauf ↔ Produktion | von Hand | alle | Wer macht was bis wann? |
| KPI Liefertreue | OTD, OTIF, OTP je Monat, Abweichungsliste | SAP | automatisch | Wie zuverlässig liefern wir? |

**Wie die Seiten verbunden sind:** Die Verzugsmengen der PPD erscheinen auf den Abteilungsseiten. Die Tagesmenge der Abteilungen ist das Soll der Logistik. Tagesmenge und Personal ergeben die Leistung pro Person, mit der der Forecast die Kapazität rechnet. Ampel, Pflichtpunkte, Pendenzen, Rückstände und die ZD05-Kennzahl laufen auf der Startseite zusammen. Was im Meeting beschlossen wird, wird als Pendenz oder neuer Termin festgehalten und in SAP umgesetzt; das zeigt sich am nächsten Morgen in den Tageswerten und am Monatsanfang in der Liefertreue.

### Einsatz im Arbeitsalltag

| Wann | Was passiert |
|---|---|
| werktags 04:00 | SAP-Abgleich: Fehlteile ZD05, Plan- und Fertigungsaufträge, Logistik-Mengen, Tagesmengen SEH, TR5 und DW, FPY SEH (seit 19.10.2026 auf dem Produktionstag, heute und die drei Tage davor) |
| vor dem Meeting | Teamleiter erfassen Tagesziel, Personal, Ampel und Bemerkungen; Einkauf codiert neue Fehlteile; PPD führt Verzug nach |
| Shop-Floor-Meeting | Ablauf auf der Startseite von oben nach unten, nur Abweichungen besprechen, Massnahmen als Pendenz festhalten |
| werktags 12:45 | zweiter SAP-Abgleich mit den Buchungen des Vormittags |
| Donnerstag oder Freitag | PPD und Abteilungsleiter planen im Forecast die nächste Woche |
| Freitag | erweitertes Meeting mit CZ, MTF, GFS, Qualität, Masterdata, IT |
| ab dem 2. des Monats | Liefertreue des Vormonats aus SAP, Monatsgespräch der Leitung |

### Grenzen und Fallstricke

Die Grafik zeigt den Stand vom 16.10.2026. Was „von Hand (SAP geplant)“ heisst, wird schrittweise automatisch; die Seite „Schnittstelle SAP / MES“ zeigt den aktuellen Stand je Feld.

## Geschäftsprozess: das tägliche Shop-Floor-Meeting

### Wozu

Das Shop-Floor-Meeting ist der tägliche Steuerungskreis der Produktion. In 10 bis 20 Minuten klären alle Bereiche, die einen Kundenauftrag berühren, wo es gestern gelaufen ist, wo es heute klemmt und wer bis wann etwas tut. Das Board ist das gemeinsame Gedächtnis dafür: Es zeigt die Zahlen des Vortags, hält Massnahmen mit Termin und Verantwortung fest und macht sichtbar, was liegen bleibt. Ziel ist eine hohe Liefertreue (siehe OTD/OTIF/OTP) bei planbarer Auslastung.

### Was man sieht

Der Prozess folgt der Tagesordnung auf der Startseite. Zuerst kommen Highlights und Zuständigkeiten, dann die Pendenzen, danach die Produktionsabteilungen SEH, TR5, TX und DW mit ihren Rückstandslisten. Es folgen die Planung (PPD) mit Auslastung, Terminanfragen, Angeboten, CZ-Anfragen und Forecast, dann Logistik, Unterhalt, Einkauf mit den Fehlteilen und zuletzt der Verkauf. Am Freitag kommen CZ und MTF dazu, und der Teilnehmerkreis ist grösser (GFS, CZ, Masterdata, Qualität, Teilefertigung, Gebäudeunterhalt, Prozesstechnik, IT).

### Wie gerechnet wird

Für die von Hand erfassten Tageswerte gilt eine Regel: **Der Eintrag vom Tag D zeigt den Stand bis D-1.** Was am Montag besprochen wird, sind also die Mengen vom Freitag. Automatisch aus SAP gelieferte Mengen stehen dagegen seit dem 19.10.2026 auf dem **Produktionstag**: Der Eintrag vom Tag D enthält die Rückmeldungen mit Erfassungsdatum D beziehungsweise die Aufträge mit Istendtermin D. Der Abgleich schreibt jeweils den heutigen Tag und die drei Tage davor neu; ein Tag ohne jede Rückmeldung eines Bereichs (zum Beispiel ein Wochenende) bleibt leer statt null. Werte aus SAP tragen einen farbigen Punkt mit der Quelle und dem Lieferzeitpunkt. Was von Hand erfasst ist, überschreibt die SAP-Lieferung nicht.

### Einsatz im Arbeitsalltag

**Abteilungs- und Teamleiter:** vor dem Meeting die eigenen Tageswerte, die Ampel und den Grund für lange Lieferzeiten erfassen. **PPD:** Verzug und Terminverschiebungen nachführen, Rückstände und Terminanfragen beantworten, am Donnerstag oder Freitag die nächste Woche im Forecast planen. **Einkauf:** jede neue Fehlteilposition mit Code und Liefertermin versehen. **Verkauf:** Auftragseingang, Umsatz und Terminwünsche einbringen. **Moderation:** von oben nach unten durch den Ablauf gehen und nur die Abweichungen besprechen; Massnahmen gehören als Pendenz mit Termin ins Board, nicht ins Protokoll.

### Grenzen und Fallstricke

Das Board ersetzt keine Ursachenanalyse. Es zeigt, dass etwas abweicht, nicht warum. Was im Meeting nicht als Pendenz oder Listeneintrag festgehalten wird, ist am nächsten Tag nicht mehr sichtbar. Automatisch gelieferte Zahlen sind nur so gut wie die Buchungen in SAP: Eine Rückmeldung, die erst am Nachmittag erfasst wird, erscheint erst im Lauf um 12:45 Uhr oder am nächsten Tag.

## KPI Übersicht

### Wozu

Die Übersicht ist der Einstieg in die Operations-Kennzahlen. Sie zeigt die drei Liefertreue-Werte auf einen Blick und führt per Klick zum ausführlichen Report. Weitere Operations-Kennzahlen kommen später dazu; ihren Inhalt legt die Leitung Supply Chain Operations fest.

### Was man sieht

- Drei Kacheln OTD, OTIF und OTP mit dem Wert mit Toleranz, darunter der Wert ohne Toleranz und die Basis (Anzahl Positionen bzw. Aufträge).
- Die Quelle und der Stand, zum Beispiel „SAP 14.10.2026“, sowie die Werte des Berichtsmonats.
- Den Bereich „Abruf aus SAP“ mit den gespeicherten Monatsständen und einem Knopf, der den Abruf sofort startet.
- Die Tabelle **Ausschlüsse**: Kunden und Materialien, die nicht in die Kennzahlen einfliessen.

### Wie gerechnet wird

Die Kacheln zeigen dieselben Zahlen wie der Report (siehe nächster Abschnitt). Der Abruf läuft automatisch ab dem 2. Kalendertag jedes Monats für den Zeitraum 1. Januar bis Ende Vormonat und dauert einige Minuten. Solange der Vormonat fehlt, versucht die App es alle zwei Stunden erneut, auch nach einem Neustart. Jeder Versuch wird gespeichert und steht in den Admin-Logs unter „Operations KPI“. Fehlt der Vormonat ab dem 3. noch immer, erscheint auf der Übersicht und im Report ein roter Warnhinweis. Pro Berichtsmonat wird ein Stand gespeichert; ein neuer Abruf ersetzt den Stand desselben Monats.

### Einsatz im Arbeitsalltag

**Leitung Operations:** Anfang Monat die Liefertreue des Vormonats prüfen und für das Monatsgespräch übernehmen. **Supply Chain:** Ausschlüsse pflegen, etwa wenn eine neue Konzerngesellschaft als Kunde dazukommt. Eine Änderung an den Ausschlüssen wirkt erst ab dem nächsten Abruf; wer sie sofort sehen will, startet den Abruf von Hand.

### Grenzen und Fallstricke

Konzerninterne Kunden (Trafag CZ, Trafag Controls India) und GFS Germany sind ausgeschlossen, ebenso Zertifikate nach EN 10204, die das System am Materialtext erkennt. Wer einen Ausschluss löscht, verändert die Vergleichbarkeit mit früheren Monaten. Der Abruf braucht in SAP die Berechtigung des technischen Cockpit-Benutzers für die Liefertreue-Abfrage; fehlt sie, meldet die Seite den Grund und zeigt weiter den letzten gültigen Stand.

## OTD / OTIF / OTP

### Wozu

Der Report beantwortet, wie zuverlässig die Trafag AG ihre Zusagen an die Kunden einhält: pünktlich gegenüber dem bestätigten Termin (OTD), vollständig und pünktlich pro Auftrag (OTIF) und gegenüber dem Wunschtermin des Kunden (OTP). Er ersetzt den monatlichen Liefertreue-Report als PDF und Excel.

### Was man sieht

- Vier Kacheln: OTD, OTIF, OTP und „Nicht pünktlich (OTD)“ mit dem durchschnittlichen Verzug in Arbeitstagen.
- Ein Diagramm mit drei Säulen je Monat (OTD blau, OTIF orange, OTP grün); der Wert ohne Toleranz ist als gestrichelte Marke eingezeichnet.
- Die Monatstabelle mit fälligen Positionen, pünktlichen Positionen, fälligen und kompletten Aufträgen, Positionen nach Wunschtermin und den zu früh gelieferten Positionen.
- **OTP nach Machbarkeit:** ob der Wunschtermin des Kunden überhaupt realistisch war, mit Tabelle je Produktgruppe.
- Die Analyse der verspäteten Positionen nach Verzugsklassen, die Top 10 Kunden und Top 10 Materialien, jeweils als Filter anklickbar.
- Die **Abweichungsliste** aller zu späten oder zu frühen Positionen mit Filter, Sortierung und Export als Excel.

### Wie gerechnet wird

Massgebend ist der letzte tatsächliche Warenausgang aller Lieferungen einer Auftragsposition, verglichen mit dem Termin aus dem Auftrag. Gezählt werden Arbeitstage Montag bis Freitag; Feiertage werden bewusst nicht abgezogen. Eine Position ist pünktlich, wenn sie vollständig geliefert ist und der letzte Warenausgang höchstens 4 Arbeitstage nach dem bestätigten Termin liegt (Toleranz für feste Liefertage). Jede Kennzahl gibt es zusätzlich ohne Toleranz, also auf den Tag genau. Offene oder teilgelieferte fällige Positionen zählen als nicht pünktlich. OTIF zählt einen Auftrag erst, wenn alle seine Positionen fällig sind, und nur dann als pünktlich, wenn jede Position pünktlich ist. OTP rechnet wie OTD, aber gegen den Wunschtermin. Zu früh ist eine Position, die 5 oder mehr Arbeitstage vor dem bestätigten Termin ausgeliefert wurde.

### Einsatz im Arbeitsalltag

**Leitung Operations und Geschäftsleitung:** Entwicklung über die Monate verfolgen und mit Zielen vergleichen. **Produktionsplanung:** in der Abweichungsliste die grössten Verzüge nach Material und Kunde suchen und die Ursachen im Shop-Floor-Meeting klären. **Verkauf:** mit „OTP nach Machbarkeit“ zeigen, bei welchen Produktgruppen Kunden Termine wünschen, die kürzer als die übliche Lieferzeit sind. **Kundenbetreuung:** vor einem Kundengespräch die Liste nach dem Kunden filtern.

### Grenzen und Fallstricke

Der bestätigte Termin ist ein Liefertermin; bei Export-Positionen mit Transitzeit ist der Vergleich mit dem Warenausgang deshalb grosszügiger, als er für den Kunden wirkt. Fehlt zu einer gelieferten Position der Warenausgang, übernimmt der Report die SAP-Kennzahl „Verzug“; solche Positionen sind in der Liste markiert. Nachbuchungen in SAP können einen bereits berichteten Monat beim nächsten Abruf leicht verändern. Die Werte gelten nur für die Verkaufsorganisation 1100.

**Interpretation:** Ein guter OTD bei schwachem OTP heisst, dass die Produktion ihre Zusagen hält, die Zusagen aber oft später liegen als der Kundenwunsch; das ist ein Lieferzeit- oder Kapazitätsthema, kein Ausführungsproblem. Liegt OTIF deutlich unter OTD, scheitern Aufträge an einzelnen Positionen, oft an einem Fehlteil. Ein hoher Anteil „zu früh“ deutet auf Planung nach Verfügbarkeit statt nach Termin hin und bindet Lager.

## Shopfloor: Startseite und Ablauf

### Wozu

Die Startseite führt durch das tägliche Shop-Floor-Meeting. Sie zeigt die Tagesordnung, welche Pflichtpunkte schon erfasst sind, wie es um jeden Bereich steht und wo es brennt, damit das Meeting in 10 bis 20 Minuten bleibt.

### Was man sieht

- Den **Ablauf** in der Reihenfolge der Tagesordnung: Highlights, Zuständigkeiten, 1. Pendenzen, 2. Entwicklungsaufträge, 3. bis 6. die Abteilungen SEH, TR5, TX und DW mit ihren Rückstandslisten, 7. PPD mit PPA-Grafik, Terminverbesserungsanfragen, Angeboten, CZ-Anfragen und Forecast, 8. Logistik, 9. Unterhalt, 10. Einkauf mit Fehlteilen und Kommunikation, 11. Verkauf, am Freitag F1. CZ und F2. MTF, dazu die Sensorbestände.
- Pflichtpunkte sind orange markiert; neben jedem Punkt steht, ob er für heute erfasst ist und von wem.
- Ein farbiger **Ampelpunkt** je Bereich mit dem letzten Status (Grün, Orange, Rot) und der Erfüllung in Prozent.
- Eine Zeitanzeige für das Meeting mit dem Ziel 10 bis 20 Minuten.
- Kacheln mit dem Fortschritt der Pflichtpunkte, den offenen und überfälligen Pendenzen, den Rückständen je Abteilung und der Fehlteile-Kennzahl ZD05 mit ihrem Verlauf.
- Am Freitag die erweiterte Teilnehmerliste.

### Wie gerechnet wird

Ein Pflichtpunkt gilt als erfasst, sobald für den Tag Werte oder Einträge vorliegen. Auch eine gesetzte Ampel allein zählt als Eintrag. Überfällig ist ein Eintrag, dessen Termin vor heute liegt und dessen Status nicht abgeschlossen ist (erledigt, verfallen, bestellt). Der Ampelpunkt zeigt den jüngsten Status der letzten 14 Tage.

### Einsatz im Arbeitsalltag

**Moderation:** von oben nach unten durch den Ablauf gehen, bei roten und orangen Punkten nachfragen, offene Pflichtpunkte ansprechen. **Abteilungsleiter:** vor dem Meeting die eigenen Werte eintragen, damit der Punkt als erfasst erscheint. **Leitung Operations:** auf einen Blick sehen, welche Bereiche heute Unterstützung brauchen.

### Grenzen und Fallstricke

Die Startseite zeigt nur, ob etwas erfasst ist, nicht ob es stimmt. Ein Bereich mit nur einer Ampel und ohne Zahlen erscheint trotzdem als erfasst. Der Ablauf ist fest eingestellt; Änderungen an der Tagesordnung macht die Plattform-Betreuung.

## Shopfloor: Highlights und Zuständigkeiten

### Wozu

**Highlights** halten fest, was positiv oder bemerkenswert war: ein grosser Auftrag, eine gelöste Störung, ein Kundenlob. **Zuständigkeiten** zeigen, wer heute welche Schicht leitet und ob die Teamleiter anwesend sind, damit klar ist, wen man bei einem Problem anspricht.

### Was man sieht

Highlights sind eine Liste mit Datum, Bereich, Titel und Beschreibung. Zuständigkeiten sind ein Tagesblatt je Bereich (SEH, TR5, TX, DW, MTF, Unterhalt, Logistik) mit Anwesenheit des Teamleiters und der Schichtleitung pro Schicht, ausgewählt aus festen Namenslisten, dazu Angaben wie „nur bis 17:00“ oder „ohne Leitung“.

### Wie gerechnet wird

Hier wird nichts gerechnet. Highlights bekommen eine fortlaufende Nummer (SFH-Jahr-Nummer).

### Einsatz im Arbeitsalltag

**Teamleiter:** die Schichtleitung des Tages vor dem Meeting eintragen, besonders wenn eine Schicht ohne Leitung ist. **Moderation:** mit einem Highlight beginnen, das hebt die Stimmung und zeigt, was funktioniert.

### Grenzen und Fallstricke

Die Namenslisten sind fest eingestellt; neue Personen trägt die Plattform-Betreuung ein. Zuständigkeiten sind kein Personalplan, die Besetzung in FTE steht auf den Abteilungsseiten.

## Shopfloor: Pendenzen und Entwicklungsaufträge

### Wozu

**Pendenzen** sind die Massnahmen aus dem Meeting: wer macht was bis wann. **Entwicklungsaufträge (EA)** sind Aufträge der Entwicklung an die Produktion, etwa Muster oder Spezialaufbauten, die neben dem Seriengeschäft eingeplant werden müssen.

### Was man sieht

Jede Liste zeigt Einträge mit Nummer, Termin, Zuständigkeit und Status, dazu ein Statusband „im Termin, überfällig, erledigt“. Eine Pendenz hat Abteilung, Produkt, Material, Beschreibung, Massnahme, Verantwortlichen, Zieltermin und Status. Ein Entwicklungsauftrag hat einen Teil, den die Entwicklung ausfüllt (Wunschtermin, Priorität, Baugruppe, Menge, Auftrag, Grund der Dringlichkeit, Ansprechpartner), und einen Teil für die Disposition (Zieltermin, Abteilung, Bemerkung, Status).

### Wie gerechnet wird

Neue Einträge bekommen eine fortlaufende Nummer (SFP für Pendenzen, SFEA für Entwicklungsaufträge). Überfällig ist ein Eintrag mit Zieltermin vor heute, der nicht erledigt ist. Jede Änderung wird mit Windows-Konto und Zeit gespeichert und ist in der Änderungsgeschichte sichtbar.

### Einsatz im Arbeitsalltag

**Moderation:** jede Massnahme aus dem Meeting sofort als Pendenz mit Verantwortlichem und Termin erfassen. **Verantwortliche:** die eigenen Pendenzen vor dem Meeting nachführen und erledigte abschliessen. **Entwicklung:** Muster früh als EA erfassen, damit die Disposition sie einplanen kann. **Leitung Operations:** an der Zahl überfälliger Pendenzen sehen, ob Massnahmen umgesetzt werden.

### Grenzen und Fallstricke

Bei Zuständigkeitswechseln legt das System eine Mail an die Zuständigen in den Postausgang; versendet wird sie nicht automatisch, sondern über „In Outlook öffnen“. Eine Pendenz ohne Termin wird nie überfällig und fällt deshalb nicht auf.

## Shopfloor: Abteilungen SEH, TR5, TX und DW

### Wozu

Die Seiten der vier Produktionsabteilungen sind das Herz des Meetings. SEH fertigt Sensoren und Baugruppen, TR5 und TX sind die Transmitter-Abteilungen, DW fertigt Dichtewächter und Dichtesensoren. Jede Seite beantwortet drei Fragen: Wie viel Arbeit liegt vor uns, haben wir gestern geschafft, was wir wollten, und haben wir genug Leute dafür?

### Was man sieht

- Oben die Karte **Status Bereich** mit der Ampel (siehe eigener Abschnitt).
- Bis zu sechs **Verlaufskacheln** mit dem Trend der letzten 20 Erfassungen und der Veränderung zur vorherigen Erfassung: produzierte Tagesmenge, Auftragsvorrat 1 Monat, Auftragsvorrat total, offene Menge der laufenden Woche, erreichte Tagesleistung, bei SEH zusätzlich FPY.
- Ein Diagramm „Produziert gegen Tagesziel“ über die letzten acht Wochen.
- Die Wochentabelle mit einer Spalte pro Arbeitstag. Die Gruppen: **Auftragsvorrat** (total, 3, 2 und 1 Monat, Ø Geräte pro Tag, offene Menge der laufenden KW), **Tagesleistung** (Tagesziel, produzierte Menge, Abweichung, erreichte Leistung, Verzug und Terminverschiebungen aus PPD), **Personal** (FTE je Schicht, frei, krank), **Lieferzeiten und Qualität** (Lieferzeit je Produktgruppe in Wochen, Grund lange Lieferzeit, FPY) und Bemerkungen.
- TX hat zusätzlich die Planmenge der nächsten 5 Arbeitstage, Ziel und Ist je Schicht, den Abgleich-Modus und den Hot-Anteil. DW führt das Personal getrennt für DW und DS/DSM und zeigt neben der produzierten Menge (Arbeitsplatz DL790) die lieferbereite Menge (DLQ72).

### Wie gerechnet wird

Ø Geräte pro Tag 1. Monat ist der Auftragsvorrat 1 Monat geteilt durch 20 Arbeitstage. Die Abweichung ist produzierte Menge minus Tagesziel, die erreichte Tagesleistung ist produzierte Menge durch Tagesziel in Prozent. Verzug und Terminverschiebungen werden aus der PPD-Seite übernommen. Die Abteilung eines Auftrags ergibt sich aus dem Disponenten im Werk 1100: SEH Disponenten mit S, TR5 mit EK, TX mit EL, DW mit D, CZ 017 bis 028.

Automatisch aus SAP seit dem 16.10.2026 (Abgleich 04:00 und 12:45 Uhr): die **produzierte Tagesmenge SEH** ist die WE-Menge der Fertigungsaufträge der Disponenten S* mit Istendtermin am Tag des Eintrags (seit 19.10.2026; vorher am Vortag), wie im COOIS-Layout /SEH 001. Die **produzierte Tagesmenge TR5** rechnet wie der Report ZC27 mit der Variante /PPA EXX72: Gutmenge der Rückmeldungen an elf Arbeitsplätzen mit Typencode 8*, nur der letzte Vorgang. **FPY SEH** ist WE-Menge geteilt durch Gesamtmenge der Aufträge mit Istendtermin in der Kalenderwoche des Produktionstags bis zu diesem Tag (seit 19.10.2026; vorher Kalenderwoche des Vortags). Seit dem 19.10.2026 kommt auch die **produzierte Tagesmenge DW** aus SAP: Gutmenge der Rückmeldungen am Arbeitsplatz DL790 nach Erfassungsdatum, wie im Report ZC27; die Gegenkontrolle mit ZC27 durch die Supply Chain ist noch offen. FPY für TR5, TX, DW und CZ kommt aus dem MES.

### Einsatz im Arbeitsalltag

**Teamleiter:** vor dem Meeting Tagesziel, Personal und Bemerkungen erfassen, bei Abweichung kurz den Grund nennen. **Abteilungsleiter:** im Verlauf sehen, ob der Vorrat 1 Monat schneller wächst als die Tagesleistung; dann reicht die Kapazität nicht. **PPD:** die offene Menge der laufenden Woche gegen die Tagesleistung halten und früh Personal verschieben.

**Interpretation:** Steigt der Auftragsvorrat 1 Monat bei gleichbleibender Tagesleistung, wird die Lieferzeit länger, auch wenn heute noch alles pünktlich ist. Eine erreichte Tagesleistung dauerhaft über 100 % bei sinkendem Vorrat heisst, dass das Ziel zu tief liegt oder Arbeit vorgezogen wird. Viele Kranke oder Freie bei gleicher Leistung zeigen eine hohe Produktivität, aber auch ein Risiko, wenn es so weitergeht. Ein FPY unter dem üblichen Niveau kostet doppelt: Nacharbeit bindet Leute und verzögert Lieferungen.

### Grenzen und Fallstricke

Von Hand erfasste Werte gelten für den Vortag; die SAP-Mengen stehen auf dem Produktionstag (siehe oben). Ein Tagesziel, das niemand erfasst, macht Abweichung und Leistung leer. Felder, die später aus SAP kommen (zum Beispiel Auftragsvorrat), sind schon mit „SAP“ markiert, werden aber bis zur Freischaltung von Hand gepflegt; die Seite „Schnittstelle SAP / MES“ zeigt den Stand je Feld.

## Shopfloor: Status Bereich (Ampel)

### Wozu

Die Ampel ist die kürzeste Antwort auf die Frage „Wie geht es dem Bereich?“. Der Teamleiter oder die Stellvertretung bewertet den Bereich selbst, statt dass das Meeting es aus den Zahlen erraten muss. Es gibt sie für SEH, TR5, TX, DW, PPD, Logistik, Unterhalt, Verkauf, CZ und MTF.

### Was man sieht

Oben auf der Abteilungsseite eine Karte mit drei Knöpfen Grün, Orange und Rot, einem Feld „Erfüllung in %“ und einem kurzen Text „was klemmt, was ist zu tun“. Auf der Startseite erscheint der Status als Punkt neben dem Bereich, mit Prozentwert.

### Wie gerechnet wird

Gespeichert wird im Tagesdatensatz des Bereichs. Gilt kein Status für heute, zeigt die Karte den letzten Status der vergangenen 14 Tage mit dessen Datum. Ein Klick auf eine Farbe aus einem Vortag bestätigt sie für heute. Ein zweiter Klick auf die heute gesetzte Farbe entfernt den heutigen Status wieder.

### Einsatz im Arbeitsalltag

**Teamleiter:** jeden Morgen die Ampel setzen. Grün heisst alles im grünen Bereich, Orange heisst, es klemmt und es gibt Handlungsbedarf, Rot heisst akutes Problem mit sofortigem Handlungsbedarf. Bei Orange und Rot gehört ein Satz in die Bemerkung. **Moderation:** Rot zuerst besprechen und daraus eine Pendenz machen.

### Grenzen und Fallstricke

Die Ampel ist eine Selbsteinschätzung, keine berechnete Kennzahl. Ein Bereich, der immer Grün meldet, während der Vorrat wächst, sollte nachgefragt werden. Die Erfüllung in Prozent ist frei erfasst; was 100 % bedeutet, legt jeder Bereich selbst fest.

## Shopfloor: Lieferzeiten

### Wozu

Die Seite zeigt auf einen Blick, wie lange Kunden je Produktgruppe derzeit auf ihre Geräte warten und ob das über dem Ziel liegt. Die Lieferzeit ist das Versprechen des Verkaufs an den Markt; wird sie länger, verliert Trafag Aufträge oder muss Termine verschieben.

### Was man sieht

Je Abteilung eine Tabelle mit den Produktgruppen (SEH: Baugruppen TR5 und TX; TR5: Cold, Hot, 8292; TX: Cold, Hot, 838x; DW: Standard, Hybrid, ND, 87x9, 8773/8774/8775). Spalten: aktuelle Lieferzeit in Wochen mit Datum der Erfassung, Ziel, Status und Verlauf. Darunter das Feld „Grund der langen Lieferzeit“.

### Wie gerechnet wird

Die aktuelle Lieferzeit ist der zuletzt auf der Abteilungsseite erfasste Wert. Der Status vergleicht ihn mit dem Zielwert. Solange kein Ziel hinterlegt ist, steht „kein Ziel“. Die Zielwerte legen die Teamleiter mit der Leitung Produktion fest; sie werden zentral in der Konfiguration eingetragen.

### Einsatz im Arbeitsalltag

**Teamleiter:** die Lieferzeiten wöchentlich aktualisieren und bei Überschreitung den Grund erfassen. **Verkauf:** vor einer Offerte die aktuelle Lieferzeit nachsehen statt einer alten Zahl. **Leitung Operations:** Gruppen über dem Ziel als Thema für Kapazität oder Material aufnehmen.

### Grenzen und Fallstricke

Die Lieferzeit ist von Hand erfasst und nicht aus SAP berechnet; ein Wert, der wochenlang nicht nachgeführt wird, wirkt aktuell, ist es aber nicht. Das Datum neben dem Wert zeigt, wie alt er ist. Stand 16.10.2026 sind noch keine Zielwerte hinterlegt.

## Shopfloor: Rückstandslisten

### Wozu

Die Rückstandslisten SEH, TR5, TX und DW führen jede Kundenauftragsposition, die ihren Termin nicht halten kann. Sie sind die Grundlage, um Kunden neue Termine zu nennen und die Ursachen systematisch abzubauen.

### Was man sieht

Je Position: Verkaufsnummer, Typencode, Kundenauftrag und Position, Kunde, Bestellmenge, Plan- und Fertigungsauftrag, Menge im Rückstand, Eckendtermin, Wunschtermin, bis zu drei neue Liefertermine (1. bis 3. Terminverschiebung), Verursacher und Ursache aus festen Listen, Fehlmaterial mit betroffener Komponente und geplantem Wareneingang, Status, Abschluss.

### Wie gerechnet wird

Jede Position bekommt eine Nummer (RS-Abteilung-Jahr-Nummer). Gezählt werden offene Positionen; überfällig ist eine Position, deren gültiger Termin vor heute liegt. Die Ursachenlisten (zum Beispiel Fehlmaterial, Ausschuss, Personalausfall, Engpass Ofenauslastung, Planungsfehler) und Verursacher (Einkauf, GFS, Trafag CZ, Logistik, Dispo, Qualität, Produktion und weitere) sind fest vorgegeben, damit sie sich auswerten lassen.

### Einsatz im Arbeitsalltag

**Disposition:** neue Rückstände erfassen, Ursache und Verursacher zuordnen und den neuen Termin nachführen. **Abteilungsleiter:** im Meeting die überfälligen Positionen durchgehen. **Leitung Operations:** im CSV-Export auswerten, welche Ursachen am häufigsten sind, und daraus Verbesserungsprojekte ableiten.

**Interpretation:** Eine dritte Terminverschiebung zeigt ein strukturelles Problem, das ein Kunde kaum noch akzeptiert. Häufen sich Fehlmaterial-Ursachen, liegt der Hebel im Einkauf; häufen sich Ausschuss oder Q-Freigabe, in der Qualität.

### Grenzen und Fallstricke

Die Listen sollen künftig aus SAP gefüllt werden; bis dahin werden sie von Hand gepflegt. Bei Zuständigkeitswechsel geht eine Mail an den Disponenten in den Postausgang, versendet wird sie über Outlook.

## Shopfloor: PPD (Produktionsplanung und Disposition)

### Wozu

PPD ist die Drehscheibe zwischen Verkauf, Einkauf und Produktion. Die Seite zeigt, wie viel Menge im Verzug ist, wie viele Termine verschoben wurden und welche Wareneingänge dringend erwartet werden.

### Was man sieht

- **Mengen in Verzug** je Abteilung SEH, TR5, TX und DW mit Total und Bemerkung.
- **Terminverschiebungen** je Abteilung (SEH, MTS, TR5, TX, DW) mit Total und Bemerkung.
- Die **Prio-Liste Fehlmaterial**: anstehende Wareneingänge mit sehr hoher Dringlichkeit.
- Die **Zuständigkeiten PPD**: wer heute welche Disponentengruppe betreut.
- Die Ampel und Verlaufskacheln für Verzug und Terminverschiebungen total und je Abteilung.

### Wie gerechnet wird

Total in Verzug ist die Summe SEH, TR5, TX und DW. Die Mengen in Verzug erscheinen auch auf den Abteilungsseiten. Seit dem 16.10.2026 zählt MTS nicht mehr zum Verzug.

### Einsatz im Arbeitsalltag

**Disponenten:** Verzug und Terminverschiebungen täglich erfassen und die Prio-Liste für den Einkauf und die Logistik pflegen. **Leitung Operations:** im Verlauf sehen, ob der Verzug abgebaut wird.

**Interpretation:** Steigende Terminverschiebungen bei gleichem Verzug heissen, dass Termine laufend nach hinten geschoben statt eingehalten werden; das drückt den OTP, auch wenn der OTD hält.

### Grenzen und Fallstricke

Die Verzugsmengen sind für eine SAP-Lieferung vorgesehen, werden aber bis dahin von Hand erfasst. Die Terminverschiebungen bleiben eine manuelle Zählung.

## Shopfloor: PPA-Grafik und Forecast

### Wozu

Die **PPA-Grafik** zeigt die Auslastung über die letzten 26 Wochen: Wächst der Auftragsvorrat, oder baut die Produktion ihn ab? Der **Forecast** plant die nächste Woche: Wie viel ist pro Abteilung und Tag zu produzieren, und reicht die geplante Besetzung?

### Was man sieht

Die PPA-Grafik zeigt Auftragsvorrat und produzierte Wochenmenge je Abteilung, berechnet aus den Tageswerten. Im Forecast stehen je Abteilung (SEH, TR5, TX, DW, DS für die Dichtesensoren und CZ) und Tag die geplante Menge, die Besetzung, die Kapazität, die Auslastung als Ampel und Empfehlungen wie „2. Schicht öffnen“ oder Hinweise auf Reserven, die man ausleihen kann; dazu eine Heatmap und ein Diagramm Menge gegen Kapazität. Aufträge ohne Abteilung stehen unter „Nicht zugeordnet“.

### Wie gerechnet wird

Die geplanten Mengen kommen aus den offenen Plan- und Fertigungsaufträgen in SAP für die nächsten 28 Tage, werktags um 04:00 und 12:45 Uhr. Die Abteilung ergibt sich aus dem Disponenten. Seit dem 16.10.2026 zählen zu DW nur die fertigen Dichtewächter (Disponenten D10 bis D40, DB1, DB2, DEF, DW2); die Dichtesensoren (DS1, DS2, DSN) sind eine eigene Zeile DS, und Rüstpapiere (DW1) sowie Nachschub (DL6) sind keiner Abteilung zugeordnet. Die Leistung pro Person und Tag ist der Median der letzten 40 Tage mit erfasster Produktion und Besetzung, sofern sie nicht von Hand eingestellt ist. Kapazität ist Besetzung mal Leistung, Auslastung ist geplante Menge geteilt durch Kapazität; 85 bis 105 % gilt als passend, darüber als überlastet, darunter als Reserve. Menge und Besetzung lassen sich pro Tag von Hand überschreiben.

### Einsatz im Arbeitsalltag

**PPD und Abteilungsleiter:** am Donnerstag oder Freitag die nächste Woche planen, Personal zwischen Abteilungen verschieben und Schichten öffnen oder schliessen. **Leitung Operations:** in der PPA-Grafik erkennen, ob der Auftragsvorrat dauerhaft steigt; das ist das Signal für zusätzliche Kapazität oder längere Lieferzeiten.

### Grenzen und Fallstricke

Ohne erfasste Besetzung oder Produktion kann der Forecast keine Leistung schätzen und zeigt „Leistung fehlt“; für DS muss die Leistung in Stück pro FTE von Hand erfasst werden. Aufträge unter „Nicht zugeordnet“ fehlen in der Planung der Abteilungen. Die Auslastung rechnet in Stück; verschieden aufwendige Geräte werden gleich gezählt.

## Shopfloor: Terminverbesserungsanfragen, Angebote und Kommunikation

### Wozu

Diese Listen ersetzen Mails und Zurufe zwischen den Bereichen. **TVAV** sind Anfragen des Verkaufs, einen bestätigten Liefertermin vorzuziehen. **PPA-Angebote (PPA-AV)** sind Terminanfragen des Verkaufs vor einer Bestellung, also für Offerten. **CZ ↔ CH** und **Einkauf ↔ Produktion** sind Kommunikationstools für Anfragen und Materialbestellungen zwischen den Standorten und Bereichen.

### Was man sieht

- **TVAV:** Verkaufsnummer, Kundenauftrag, Menge, bestätigter Termin, neues Wunschdatum, Priorität, Bemerkung; PPD antwortet mit „Vorzug möglich“ (ja, Teilmenge, nur mit Abtausch, leider nicht), bestmöglichem Termin, bestätigter Menge und Fehlmaterial.
- **Angebote:** Anfrage mit Menge, Wunschtermin und Priorität; PPD gibt den bestmöglichen Termin, die Lieferzeit ab Bestellung und die bestätigte Menge an; nach der Bestellung folgt der Kundenauftrag.
- **CZ ↔ CH und Einkauf ↔ Produktion:** Anfrage von und an einen Bereich, Material, Menge, Wunschtermin, Priorität (Prio 1 Produktionsstillstand bis Terminanfrage ohne Bestellung); der ausführende Bereich antwortet mit Termin, Menge und Status.

### Wie gerechnet wird

Jeder Eintrag bekommt eine Nummer (TVAV, PPA-AV, PPA-KT, PPA-KEP). Das Feld „Angebot gültig bis“ zeigt 7 Tage nach der letzten Änderung; den Status „verfallen“ setzt die PPD von Hand. Abgeschlossen sind Einträge mit Status erledigt, verfallen oder bestellt.

### Einsatz im Arbeitsalltag

**Verkauf:** Terminwünsche hier statt per Mail erfassen und die Antwort verfolgen. **PPD:** im Meeting offene Anfragen beantworten. **Einkauf und CZ:** Anfragen an den eigenen Bereich täglich abarbeiten.

**Interpretation:** Viele TVAV mit „leider nicht“ zeigen, dass die Bestätigungen keinen Spielraum haben. Viele verfallene Angebote können heissen, dass die Lieferzeit für den Kunden zu lang war.

### Grenzen und Fallstricke

Eine Mail an den angefragten Bereich wird vorbereitet, aber nur über „In Outlook öffnen“ verschickt. Die Priorität ist eine Angabe des Anfragenden, keine Regel; im Zweifel entscheidet das Meeting.

## Shopfloor: Logistik

### Wozu

Die Logistik-Seite zeigt, wie viele Geräte gestern verpackt wurden, verglichen mit dem, was die Produktion gemeldet hat. So fällt auf, wenn fertige Geräte nicht rechtzeitig verpackt und ausgeliefert werden.

### Was man sieht

Je Bereich die verpackten Mengen an den Arbeitsplätzen: TR5 (MLE01, MLE02), DW (MLE04), TX (MLE08 und MLE09 zusammen, MLM01). Daneben die Soll-Menge aus der produzierten Tagesmenge der Abteilung und die Abweichung Ist minus Soll, dazu Totale für alle Bereiche, die offenen Kundenaufträge, Personal und Ampel.

### Wie gerechnet wird

Automatisch aus SAP seit dem 16.10.2026 (Abgleich 04:00 und 12:45 Uhr): Die Mengen rechnen genau wie der Report **ZC27**. Gezählt wird die Gutmenge der Rückmeldungen nach **Erfassungsdatum**, ohne stornierte Rückmeldungen und ohne Stornosätze. Je Auftrag zählt nur der letzte Vorgang an den gewählten Arbeitsplätzen. Jede Gruppe ist ein eigener ZC27-Lauf, MLE08 und MLE09 laufen zusammen. Seit dem 19.10.2026 steht jede Menge auf dem Tag ihres Erfassungsdatums; ein Tag ohne Rückmeldung bleibt leer. Die Soll-Menge kommt aus der Tagesmenge der Abteilung.

### Einsatz im Arbeitsalltag

**Teamleiter Logistik:** Abweichungen zwischen Ist und Soll erklären und das Personal je Schicht erfassen. **Leitung Operations:** eine dauerhaft negative Abweichung als Engpass in der Logistik erkennen.

**Interpretation:** Liegt das Ist dauerhaft unter dem Soll, stauen sich fertige Geräte vor dem Versand; das verschlechtert den OTD, obwohl die Produktion pünktlich war. Ein Ist über dem Soll entsteht, wenn Rückstände vom Vortag abgebaut werden.

### Grenzen und Fallstricke

Eine Rückmeldung, die erst später erfasst wird, zählt zum Tag ihrer Erfassung, nicht zum Tag der Arbeit. Von Hand erfasste Werte überschreibt die SAP-Lieferung nicht.

## Shopfloor: Unterhalt

### Wozu

Die Seite zeigt, wie viel Zeit der Unterhalt für Störungen und wie viel für vorbeugende Instandhaltung aufwendet. Das Verhältnis beider ist ein Gradmesser für die Zuverlässigkeit der Anlagen.

### Was man sieht

Anzahl Störungen und Einsätze, Störungsdauer in Stunden, Anzahl und Aufwand vorbeugender Instandhaltungen, offene Instandhaltungen, Personal und die Anteile Störungsdauer und vorbeugende Instandhaltung an der Anwesenheit.

### Wie gerechnet wird

Die Anteile sind Stunden geteilt durch die Anwesenheit (Mitarbeitende Schicht 1 und 2 mal 8 Stunden).

### Einsatz im Arbeitsalltag

**Teamleiter Unterhalt:** täglich erfassen. **Leitung Operations:** steigt der Störungsanteil, während die vorbeugende Instandhaltung sinkt, droht ein Teufelskreis, in dem nur noch repariert wird.

### Grenzen und Fallstricke

Alle Werte sind von Hand erfasst. Die 8 Stunden je Person sind eine feste Annahme.

## Shopfloor: Einkauf, Fehlteile (ZD05)

### Wozu

Die Seite ersetzt den täglichen ZD05-Export aus SAP. Sie zeigt, bei welchen Materialien eine Unterdeckung droht, die sich durch Vorziehen einer Bestellung noch abwenden lässt, und wie gut der Einkauf die Fehlteile im Griff hat.

### Was man sieht

Die Positionen des Tages mit Material, Kurztext, Unterdeckungsdatum, Dispomerkmal, Lieferzeit-Code, Verbrauch in der Wiederbeschaffungszeit, optimalem und aktuellem Sicherheitsbestand, Planungsanteil, Bemerkung, Liefertermin und Code. Filter: Alle, **Zu besprechen**, Im Horizont, Code 2, Ohne Code, Neu. Kacheln mit der Bewertungskennzahl und ihrem Verlauf, den Positionen im Horizont und den Fehlmaterialien zu Kundenaufträgen (Code 2) nach ABC-Klasse.

### Wie gerechnet wird

Die Daten kommen werktags um 04:00 und 12:45 Uhr aus SAP (Logik des Reports ZD05 für die Disponenten 001 und 003). Eine Position entsteht, wenn der Bestand unter null fällt und danach noch ein Zugang kommt, eine Umterminierung also möglich ist. Bemerkung, Liefertermin und Code werden je Material vom Vortag übernommen. **Zu besprechen** ist eine Position, die seit dem letzten Import neu ist, noch keinen Code hat oder deren Liefertermin überschritten ist; im laufenden Meeting ist dieser Filter automatisch aktiv. Die **Bewertungskennzahl** ist der Durchschnitt der vergebenen Codes über alle Positionen, deren Unterdeckung bis heute plus 3 Arbeitstage eintritt; das Ziel ist höchstens 1,20.

### Einsatz im Arbeitsalltag

**Einkäufer:** vor dem Meeting jede Position unter „Zu besprechen“ mit Code und Liefertermin versehen. **Meeting:** nur die markierten Positionen besprechen. **Leitung Operations:** an der Kennzahl und ihrem Verlauf sehen, ob die Versorgung stabil ist.

### Grenzen und Fallstricke

Positionen ohne Code fliessen nicht in die Kennzahl ein; eine gute Kennzahl kann also auch bedeuten, dass Codes fehlen. Darum zeigt die Startseite zusätzlich die Zahl der Positionen ohne Code. Hinweis: Die Bedeutung der einzelnen Codes ausser Code 2 (Fehlmaterial Kundenauftrag) ist in diesem Handbuch nicht geprüft.

## Shopfloor: Verkauf

### Wozu

Die Verkaufsseite verbindet das Meeting mit dem Markt: Kommen genug Aufträge herein, und reicht der Umsatz, um das Monatsziel zu erreichen?

### Was man sieht

Oben drei Kacheln: das **Go-for-it-Ziel** des Monats mit dem Umsatz Ist in Prozent davon, der fehlende Betrag bis zum Ziel (mit dem geplanten Umsatz aus ZC23) und der Auftragseingang mit Book to Bill. Darunter die Tageswerte: Auftragseingang und Umsatz Ist kumuliert im Monat, Tagesumsatz, geplanter Umsatz aus ZC23, Abweichung, Auftragseingang und Umsatz je Bereich (TR5, Transmitter TX und TR5, DW, Rest), Gutschriften.

### Wie gerechnet wird

Auftragseingang und Umsatz sind Monatswerte kumuliert bis zum Vortag; am Monatsersten gilt der ganze Vormonat. Der Tagesumsatz ist die Differenz zum vorherigen Eintrag. Book to Bill ist Auftragseingang geteilt durch Umsatz. Das Go-for-it-Ziel wird einmal pro Monat im Feld „Ziel festlegen“ erfasst. Vorgesehen ist die Lieferung aus SAP CO-PA wie im Bericht KE30 (Nettoerlös, Buchungskreis 1100); Stand 16.10.2026 werden die Werte von Hand erfasst.

### Einsatz im Arbeitsalltag

**Verkauf:** täglich Auftragseingang und Umsatz eintragen, Anfang Monat das Go-for-it-Ziel. **Leitung Operations:** am Book to Bill die Richtung erkennen.

**Interpretation:** Ein Book to Bill über 1 heisst, es kommen mehr Aufträge herein, als ausgeliefert werden; der Vorrat und die Lieferzeiten wachsen. Unter 1 baut sich der Vorrat ab, mittelfristig droht Unterauslastung. Liegt der Umsatz Ist deutlich unter dem geplanten Umsatz aus ZC23, sind geplante Lieferungen nicht hinausgegangen; dann lohnt der Blick in die Rückstände.

### Grenzen und Fallstricke

Ohne Eintrag am Vortag ist der Tagesumsatz die Summe mehrerer Tage. Die Bereichsaufteilung folgt der Produktsparte in SAP, nicht dem Disponenten.

## Shopfloor: CZ, MTF und Sensorbestände

### Wozu

**CZ** (Trafag CZ) und **MTF** werden am Freitag besprochen. Die **Sensorbestände** zeigen den Vorrat an Sensoren aus SEH, der für TR5 und TX bereitliegt.

### Was man sieht

- **CZ:** Auftragsvorrat wie bei den Produktionsabteilungen, Tagesziel, produzierte Menge je Produktgruppe (Druckschalter, Picostaten, Frostschutzstaten, Mini- und Ambistaten, Navistaten, Picotherm, DTP, Baugruppen, Membranen) mit Total, Personal, FPY.
- **MTF:** Auftragsbestand, Rückstände, Q-Fälle und Störungen, Projekte, Kundenbestellungen, Auslastung und Anlagenverfügbarkeit, Personal.
- **Sensorbestände:** Stahlzellen SE1 und SE2 für TR5, SL1, SL2 und SL4 für TX (davon mit Batterie), Keramik-Anschlüsse SL3, Totale und der Grenzwert von 7000 Stück.

### Wie gerechnet wird

Bei CZ ist die produzierte Menge total die Summe der Gruppen. Bei den Sensorbeständen sind die Totale Summen der Gruppen; der Grenzwert ist fest 7000 Stück.

### Einsatz im Arbeitsalltag

**Teamleiter CZ und MTF:** vor dem Freitagsmeeting erfassen. **SEH und PPD:** fällt der Sensorbestand unter den Grenzwert, droht TR5 oder TX ein Materialengpass.

### Grenzen und Fallstricke

Die Werte sind Stand 16.10.2026 von Hand erfasst. Der Grenzwert ist eine feste Zahl und berücksichtigt keine saisonalen Schwankungen.

## Shopfloor: Personen und Mail, Schnittstelle SAP / MES, Export

### Wozu

Diese Seiten halten das Board am Laufen. **Personen und Mail** legt fest, wer bei Zuständigkeiten per Mail informiert wird. **Schnittstelle SAP / MES** zeigt, welche Felder automatisch geliefert werden sollen und wie weit das ist. Der **Export** gibt jede Liste und jede Abteilung als CSV aus.

### Was man sieht

Personen und Mail: Namen und Mailadressen; Einträge wie „Unterhalt/PT“ oder „Samuel, Kujtim“ werden in einzelne Empfänger aufgelöst. Schnittstelle: alle Felder mit geplanter Quelle (SAP oder MES), Modul, Feld, Einheit, vorgeschlagener SAP-Quelle, Selektion, Status und Bemerkung. Jede Seite hat „Export (CSV)“ und zu jedem Eintrag eine Änderungsgeschichte.

### Wie gerechnet wird

Automatische Lieferungen schreiben unter dem Namen „SAP-Schnittstelle“ und setzen die Quelle am Feld. Ein Wert, der von Hand erfasst wurde, wird nicht automatisch überschrieben; ein Wert aus einer früheren SAP-Lieferung schon. Jede Änderung, ob von Hand oder aus SAP, steht in der Änderungsgeschichte.

### Einsatz im Arbeitsalltag

**Leitung Supply Chain:** auf der Schnittstellen-Seite den Fortschritt der Anbindung verfolgen. **Bereichsleiter:** für Auswertungen den CSV-Export in Excel öffnen.

### Grenzen und Fallstricke

Stand 16.10.2026 kommen aus SAP die Fehlteile ZD05, die Plan- und Fertigungsaufträge für den Forecast, die Logistik-Mengen, die Tagesmengen SEH und TR5 sowie FPY SEH, seit dem 19.10.2026 auch die Tagesmenge DW. Die Logistik-Mengen und FPY SEH sind vor dem Einschalten mit Echtdaten gegen ZC27 und die Kontrollwerte der Supply Chain geprüft worden (13.10.2026 und KW 38 bis 41, alle gleich). Verkauf (CO-PA), Auftragsvorrat und Rückstandslisten folgen später.

## Kennzahlen-Glossar

| Kennzahl | Bedeutung | Berechnung in Worten |
|---|---|---|
| OTD (On Time Delivery) | Liefertreue gegenüber dem bestätigten Termin | Anteil der fälligen Auftragspositionen, die vollständig geliefert sind und deren letzter Warenausgang höchstens 4 Arbeitstage nach dem bestätigten Termin liegt |
| OTIF (On Time In Full) | Liefertreue pro Kundenauftrag | Anteil der vollständig fälligen Aufträge, bei denen jede Position OTD erfüllt; Monat = spätester bestätigter Termin |
| OTP (On Time Performance) | Liefertreue gegenüber dem Kundenwunsch | wie OTD, aber gegen den Wunschliefertermin; Monat = Monat des Wunschtermins |
| Ohne Toleranz | strenge Lesart jeder Liefertreue | wie die Kennzahl, aber pünktlich nur bei Warenausgang am oder vor dem Termin |
| Zu früh | vorgezogene Lieferungen | Positionen mit Warenausgang 5 oder mehr Arbeitstage vor dem bestätigten Termin |
| Arbeitstage | Zähleinheit für Verzug und Lieferzeit | Montag bis Freitag, Feiertage werden nicht abgezogen |
| Wunsch realistisch / zu kurz | Machbarkeit des Kundenwunsches | Lieferzeit vom Auftragsdatum bis zum Wunschtermin verglichen mit dem Median der bestätigten Lieferzeit derselben Produktgruppe (bei weniger als 20 Positionen der Gesamtmedian) |
| Stand D-1 | Zeitbezug der von Hand erfassten Tageswerte | der Eintrag vom Tag D zeigt den Vortag; SAP-Mengen stehen seit dem 19.10.2026 auf dem Produktionstag |
| Auftragsvorrat 1/2/3 Monate | Arbeit, die in diesem Zeitraum fällig wird | offene Menge der Aufträge mit Termin bis Ende des laufenden, nächsten oder übernächsten Monats |
| Ø Geräte pro Tag 1. Monat | nötige Tagesleistung für den Vorrat | Auftragsvorrat 1 Monat geteilt durch 20 Arbeitstage |
| Erreichte Tagesleistung | Zielerreichung des Tages | produzierte Menge geteilt durch Tagesziel in Prozent |
| FPY (First Pass Yield) | Anteil ohne Nacharbeit oder Ausschuss | SEH: WE-Menge geteilt durch Gesamtmenge der Aufträge mit Istendtermin in der KW des Produktionstags bis zu diesem Tag; übrige Abteilungen aus dem MES |
| Logistik-Menge (ZC27) | verpackte Gutmenge je Arbeitsplatz | Gutmenge der Rückmeldungen nach Erfassungsdatum, ohne Stornos, nur der letzte Vorgang des Auftrags an den gewählten Arbeitsplätzen |
| Abweichung Ist–Soll Logistik | Stau vor dem Versand | verpackte Menge minus produzierte Tagesmenge der Abteilung |
| Total in Verzug | Menge hinter Plan | Summe der Verzugsmengen SEH, TR5, TX und DW (ohne MTS) |
| Status Bereich (Ampel) | Selbsteinschätzung des Bereichs | Grün, Orange oder Rot mit Erfüllung in Prozent, gesetzt vom Teamleiter |
| Lieferzeit gegen Ziel | Versprechen an den Markt | zuletzt erfasste Lieferzeit in Wochen je Produktgruppe verglichen mit dem Zielwert |
| Bewertungskennzahl ZD05 | Güte der Fehlteilbearbeitung | Durchschnitt der Codes aller Positionen mit Unterdeckung bis heute plus 3 Arbeitstage; Ziel höchstens 1,20 |
| Auslastung (Forecast) | Belastung einer Abteilung an einem Tag | geplante Menge geteilt durch Kapazität (Besetzung mal Leistung pro Person); 85 bis 105 % passend |
| Book to Bill | Verhältnis Auftragseingang zu Umsatz | Auftragseingang des Monats geteilt durch Umsatz des Monats; über 1 wächst der Vorrat |
| Angebot gültig bis | Bindung einer Terminzusage | 7 Tage nach der letzten Änderung des Angebots |
