# Operations

Der Reiter Operations bündelt die Steuerung der Produktion. Unter **KPI** stehen die Kennzahlen der Operations, zuerst die Liefertreue der Trafag AG (Verkaufsorganisation 1100) mit OTD, OTIF und OTP. Unter **Shopfloor** liegt das digitale Shop-Floor-Board, das die Excel-Datei „PPA-Shop-Floor-File“ ersetzt: Pendenzen, Abteilungswerte, Rückstandslisten, Fehlteile, Forecast und die Auslastungsgrafik für das tägliche Shop-Floor-Meeting. Hauptnutzer sind die Leitung Operations, die Produktionsplanung (PPD), die Abteilungsleiter und Teamleiter der Produktion, Einkauf, Verkauf und Logistik. Der Zugang läuft über eine eigene Anmeldung (Benutzer „operations“) oder ein freigegebenes Windows-Konto. Die Liefertreue kommt monatlich direkt aus SAP, das Shopfloor wird werktags um 05:45 und 12:45 Uhr aus SAP ergänzt und sonst im Meeting von Hand gepflegt.

## KPI Übersicht

### Wozu

Die Übersicht ist der Einstieg in die Operations-Kennzahlen. Sie zeigt die drei Liefertreue-Werte auf einen Blick und führt per Klick zum ausführlichen Report. Weitere Operations-Kennzahlen kommen später dazu; ihren Inhalt legt die Leitung Supply Chain Operations fest.

### Was man sieht

- Drei Kacheln OTD, OTIF und OTP mit dem Wert mit Toleranz, darunter der Wert ohne Toleranz und die Basis (Anzahl Positionen bzw. Aufträge).
- Die Quelle und der Stand, zum Beispiel „SAP 14.10.2026“, sowie die Werte des Berichtsmonats.
- Den Bereich „Abruf aus SAP“ mit den gespeicherten Monatsständen und einem Knopf, der den Abruf sofort startet.
- Die Tabelle **Ausschlüsse**: Kunden und Materialien, die nicht in die Kennzahlen einfliessen.

### Wie gerechnet wird

Die Kacheln zeigen dieselben Zahlen wie der Report (siehe nächster Abschnitt). Der Abruf läuft automatisch am 2. Kalendertag jedes Monats für den Zeitraum 1. Januar bis Ende Vormonat und dauert einige Minuten. Pro Berichtsmonat wird ein Stand gespeichert; ein neuer Abruf ersetzt den Stand desselben Monats.

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

## Shopfloor: Startseite und Ablauf

### Wozu

Die Startseite führt durch das tägliche Shop-Floor-Meeting. Sie zeigt die Tagesordnung, welche Pflichtpunkte schon erfasst sind und wo es brennt, damit das Meeting in 10 bis 20 Minuten bleibt.

### Was man sieht

- Den **Ablauf** in der Reihenfolge der Tagesordnung, von Highlights über Pendenzen und die Abteilungen SEH, TR5, TX und DW bis Logistik, Einkauf und Verkauf; Pflichtpunkte sind orange markiert.
- Eine Zeitanzeige für das Meeting mit dem Ziel 10 bis 20 Minuten.
- Kacheln mit dem Fortschritt der Pflichtpunkte, den offenen und überfälligen Pendenzen, den Rückständen je Abteilung und der Fehlteile-Kennzahl ZD05.
- Am Freitag eine erweiterte Teilnehmerliste mit GFS, CZ, Masterdata, Qualität und weiteren Bereichen.

### Wie gerechnet wird

Ein Pflichtpunkt gilt als erfasst, sobald für den Tag Werte oder Einträge vorliegen. Überfällig ist ein Eintrag, dessen Termin vor heute liegt und dessen Status nicht abgeschlossen ist.

### Einsatz im Arbeitsalltag

**Moderation des Meetings:** von oben nach unten durch den Ablauf gehen und offene Pflichtpunkte ansprechen. **Abteilungsleiter:** vor dem Meeting die eigenen Werte eintragen, damit die Kachel grün ist.

### Grenzen und Fallstricke

Die Startseite zeigt nur, ob etwas erfasst ist, nicht ob es stimmt. Der Ablauf ist fest eingestellt; Änderungen an der Tagesordnung macht die Plattform-Betreuung.

## Shopfloor: Listen (Pendenzen, Rückstände, Anfragen)

### Wozu

Die Listen halten fest, was über den Tag hinaus verfolgt werden muss: Pendenzen, Entwicklungsaufträge, Highlights, die Rückstandslisten je Abteilung, die Terminverbesserungsanfragen und Angebote des Verkaufs sowie die Kommunikationstools CZ ↔ CH und Einkauf ↔ Produktion.

### Was man sieht

Jede Liste zeigt Einträge mit Nummer, Termin, Zuständigkeit und Status, dazu ein Statusband „im Termin, überfällig, erledigt“. Ein Eintrag lässt sich öffnen, bearbeiten und mit seiner Änderungsgeschichte ansehen. Jede Liste kann als CSV exportiert werden.

### Wie gerechnet wird

Neue Einträge bekommen automatisch eine fortlaufende Nummer. Jede Änderung wird mit Windows-Konto und Zeit gespeichert. Gelöschte Einträge verschwinden aus der Liste, bleiben aber in der Datenbank erhalten.

### Einsatz im Arbeitsalltag

**PPD und Abteilungsleiter:** Rückstände je Kundenauftragsposition besprechen und den geplanten Wareneingang nachführen. **Verkauf:** Terminverbesserungsanfragen erfassen und die Antwort der Produktion verfolgen. **Einkauf:** offene Punkte mit der Produktion im Kommunikationstool klären.

### Grenzen und Fallstricke

Bei Zuständigkeitswechseln legt das System eine Mail in den Postausgang; versendet wird sie nicht automatisch, sondern über „In Outlook öffnen“. Die Rückstandslisten sollen künftig aus SAP gefüllt werden; bis dahin werden sie von Hand gepflegt.

## Shopfloor: Abteilungen und Tageswerte

### Wozu

Die Abteilungsseiten (SEH, TR5, TX, DW, CZ, MTF, PPD, Logistik, Unterhalt, Verkauf, Sensorbestände) erfassen pro Tag die Kennzahlen, die im Meeting besprochen werden, etwa Auftragsvorrat, offene Menge der laufenden Woche, produzierte Menge, Besetzung und FPY.

### Was man sieht

Eine Tabelle mit einer Zeile pro Tag; Werte werden direkt eingetragen und beim Verlassen des Feldes gespeichert. Darüber stehen bis zu vier Verlaufskacheln mit dem Trend der letzten Erfassungen und der Veränderung zum Vortag. Felder, die aus SAP oder dem MES kommen, tragen einen farbigen Punkt.

### Wie gerechnet wird

Einige Werte sind berechnet, zum Beispiel Summen über Schichten. Die Abteilung eines Auftrags ergibt sich aus dem Disponenten im Werk 1100: SEH = Disponenten mit S, TR5 = EK, TX = EL, DW = D, CZ = 017 bis 028.

### Einsatz im Arbeitsalltag

**Abteilungsleiter und Teamleiter:** vor dem Meeting die Tageswerte erfassen. **Leitung Operations:** in den Verlaufskacheln sehen, ob der Auftragsvorrat wächst oder die Produktion hinterherhinkt.

### Grenzen und Fallstricke

Aus dem MES kommen nur die FPY-Werte, alle anderen automatischen Werte aus SAP. Solange die Anbindung nicht steht, werden diese Felder von Hand erfasst; die Seite „Schnittstelle SAP / MES“ zeigt, welches Feld woher kommen soll und wie weit das Mapping ist.

## Shopfloor: Fehlteile (ZD05)

### Wozu

Die Seite ersetzt den täglichen ZD05-Export aus SAP und zeigt, bei welchen Materialien eine Unterdeckung droht und wie gut der Einkauf die Fehlteile im Griff hat.

### Was man sieht

Die Positionen des Tages mit Unterdeckungsdatum, Lieferzeit-Code, Sicherheitsbestand, Bemerkung, Liefertermin und Code, dazu Filter (im Horizont, Code 2, ohne Code, neu) und Kacheln mit der Bewertungskennzahl, den Positionen im Horizont und den Fehlmaterialien zu Kundenaufträgen (Code 2) nach ABC-Klasse.

### Wie gerechnet wird

Die Daten kommen werktags um 05:45 und 12:45 Uhr aus SAP (Report ZD05, Disponenten 001 und 003). Bemerkung, Liefertermin und Code werden je Material vom Vortag übernommen. Die **Bewertungskennzahl** ist der Durchschnitt der vergebenen Codes über alle Positionen, deren Unterdeckung bis heute plus 3 Arbeitstage eintritt; das Ziel ist höchstens 1,20.

### Einsatz im Arbeitsalltag

**Einkäufer:** vor dem Meeting jede neue Position mit Code und Liefertermin versehen. **Leitung Operations:** an der Kennzahl und ihrem Verlauf sehen, ob die Versorgung stabil ist.

### Grenzen und Fallstricke

Positionen ohne Code fliessen nicht in die Kennzahl ein; eine gute Kennzahl kann also auch bedeuten, dass Codes fehlen. Hinweis: Die Bedeutung der einzelnen Codes ausser Code 2 (Fehlmaterial Kundenauftrag) ist in diesem Handbuch nicht geprüft.

## Shopfloor: Forecast und PPA-Grafik

### Wozu

Der **Forecast** plant die nächste Woche: Wie viel ist pro Abteilung und Tag zu produzieren, und reicht die geplante Besetzung? Die **PPA-Grafik** zeigt die Auslastung über die letzten 26 Wochen.

### Was man sieht

Im Forecast je Abteilung und Tag die geplante Menge, die Besetzung, die Kapazität, die Auslastung als Ampel und Empfehlungen wie „2. Schicht öffnen“; dazu eine Heatmap und ein Diagramm Menge gegen Kapazität. Die PPA-Grafik zeigt Auftragsvorrat und produzierte Wochenmenge je Abteilung.

### Wie gerechnet wird

Die geplanten Mengen kommen aus den offenen Plan- und Fertigungsaufträgen in SAP für die nächsten 28 Tage. Die Leistung pro Person und Tag ergibt sich aus dem Median der letzten 40 Tage mit erfasster Produktion und Besetzung, sofern sie nicht von Hand eingestellt ist. Kapazität ist Besetzung mal Leistung, Auslastung ist geplante Menge geteilt durch Kapazität; über 105 % gilt als überlastet, unter 85 % als unterlastet. Menge und Besetzung lassen sich pro Tag von Hand überschreiben.

### Einsatz im Arbeitsalltag

**PPD und Abteilungsleiter:** am Donnerstag oder Freitag die nächste Woche planen und Personal verschieben. **Leitung Operations:** in der PPA-Grafik erkennen, ob der Auftragsvorrat dauerhaft steigt.

### Grenzen und Fallstricke

Aufträge, deren Disponent keiner Abteilung zugeordnet ist, erscheinen unter „Nicht zugeordnete Aufträge“ und fehlen in der Planung der Abteilungen. Ohne erfasste Besetzung oder Produktion kann der Forecast keine Leistung schätzen und fragt danach.

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
| Bewertungskennzahl ZD05 | Güte der Fehlteilbearbeitung | Durchschnitt der Codes aller Positionen mit Unterdeckung bis heute plus 3 Arbeitstage; Ziel höchstens 1,20 |
| Auslastung (Forecast) | Belastung einer Abteilung an einem Tag | geplante Menge geteilt durch Kapazität (Besetzung mal Leistung pro Person) |
