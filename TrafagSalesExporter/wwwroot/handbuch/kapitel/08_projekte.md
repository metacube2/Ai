# Trafag Projekte

Trafag Projekte ist das Projektmanagement im Cockpit. Es ersetzt seit dem 9. Oktober 2026 die frühere „Poor Man's Project Management Suite“, die nie genutzt wurde. Es verbindet drei bekannte Werkzeuge: von Trello das Board mit Listen, Karten zum Ziehen, Farben und Labels; von Jira die Schlüssel wie LOG-12, die Aufgabentypen, Epics, Backlog, Sprints mit Ziel, Story Points und Burndown; von Microsoft Planner die Buckets, Checklisten, „Meine Aufgaben“, Kalender und Diagramme. Der Menüpunkt heisst „Trafag Projekte“ und liegt auf oberster Ebene. Öffentliche Projekte sehen alle; ändern dürfen die Mitglieder eines Projekts und Admins; Projekt, Team und Spalten verwaltet die Projektleitung. Seit dem 13. Oktober 2026 kann ein Projekt privat sein («Wer sieht es» beim Anlegen oder in den Einstellungen): Dann sieht es nur sein Team, auch Admins nicht, solange sie nicht im Team sind. Ins Team nimmt man Personen als Mitglied oder als Leser auf; Leser sehen alles und ändern nichts. Geschrieben wird mit dem eigenen Windows-Namen. Daneben gibt es unter dem Menüpunkt „Pause“ ein kleines Spiel für die Pause, das keine Geschäftsdaten verwendet.

## Projektübersicht und Meine Aufgaben

### Wozu

Die Übersicht zeigt alle Projekte auf einen Blick und, im zweiten Reiter, alle Aufgaben, die einem selbst zugewiesen sind, über alle Projekte hinweg.

### Was man sieht

Oben ein Band mit Suche, der Schaltfläche „Neues Projekt“ und Zahlen zu Projekten, offenen und überfälligen Aufgaben. Jedes Projekt ist eine Kachel mit Farbe, Symbol, Schlüssel, Vorlage, Fortschrittsbalken (offen, in Arbeit, erledigt), Team, Ende und Zahl der überfälligen Aufgaben. Eigene Projekte stehen zuerst. „Meine Aufgaben“ gruppiert wie Planner nach Überfällig, Heute, Diese Woche, Später und Ohne Datum.

### Wie gerechnet wird

- **Fortschritt:** erledigte Aufgaben geteilt durch alle Aufgaben des Projekts, ohne Epics und ohne archivierte.
- **Erledigt, in Arbeit, offen:** nach der Art der Spalte, in der eine Aufgabe liegt. Jede Spalte gehört zu einer der drei Arten.
- **Überfällig:** Fälligkeitsdatum vor heute und nicht erledigt.

### Einsatz im Arbeitsalltag

- **Alle Mitarbeitenden:** Morgens „Meine Aufgaben“ öffnen; überfällige und heutige Aufgaben stehen oben.
- **Abteilungsleiter und Geschäftsleitung:** Die Kacheln zeigen ohne Klick, welche Projekte hinterherhinken.

### Grenzen und Fallstricke

Es gibt keine Mail-Benachrichtigung; offene Seiten aktualisieren sich aber live, wenn jemand etwas ändert.

## Neues Projekt

### Wozu

Ein Projekt wird mit einer Vorlage angelegt, die Spalten und Arbeitsweise vorgibt.

### Was man sieht

Fünf Vorlagen: **Kanban-Board** (Trello, Spalten Zu erledigen, In Arbeit, Review, Erledigt), **Scrum mit Sprints** (Jira, Spalten Zu erledigen, In Arbeit, Test, Erledigt und ein erster Sprint), **Planner mit Buckets** (Ideen, Geplant, In Arbeit, Erledigt), seit dem 29. Oktober 2026 **Produktentwicklung** (Phasen bis M2 Design-Freeze und M3 produktiv mit fertigem Plan, Abhängigkeiten und Standardrisiken) und **IT-Projekt nach HERMES** (Initialisierung, Konzept, Realisierung, Einführung mit Entscheidungspunkten). Bei den beiden neuen Vorlagen wird der Plan ab dem Startdatum gerechnet. Dazu Name, Schlüssel (2 bis 6 Grossbuchstaben, wird aus dem Namen vorgeschlagen), Beschreibung, Farbe und Symbol.

### Wie gerechnet wird

- **Schlüssel:** bei mehreren Wörtern die Anfangsbuchstaben, sonst die ersten vier Buchstaben. Er bleibt danach fest, damit Verweise wie LOG-12 gültig bleiben.
- **Projektleitung:** wer das Projekt anlegt; diese Person kann das Team zusammenstellen.

### Einsatz im Arbeitsalltag

- **Projektleiter:** Nach dem Anlegen unter „Einstellungen“ das Team aus dem AD suchen und aufnehmen.

### Grenzen und Fallstricke

Die Vorlage legt nur den Start fest; Spalten lassen sich jederzeit umbenennen, ergänzen, verschieben oder löschen.

## Board

### Wozu

Das Board ist die tägliche Arbeitsfläche: jede Aufgabe ist eine Karte, jede Spalte ein Arbeitsschritt.

### Was man sieht

Spalten mit Karten. Eine Karte zeigt Farbband, Labels, Epic, Titel, Typ, Schlüssel, Priorität, Fälligkeit (orange bei bald, rot bei überfällig), Fortschritt der Checkliste und der Unteraufgaben, Kommentare, Story Points und die zugewiesene Person. Karten zieht man mit der Maus in eine andere Spalte oder an eine andere Stelle. Am Spaltenende legt „Karte hinzufügen“ eine neue Karte an. Über der Fläche stehen Filter: Suche, Personen (Klick auf die Avatare), Nur meine, Überfällig, Typ, Priorität, Label, Epic, dazu Swimlanes nach Person, Epic oder Priorität. Bei Scrum zeigt das Board nur den laufenden Sprint.

### Wie gerechnet wird

- **WIP-Grenze:** liegt mehr als die eingestellte Zahl Karten in einer Spalte, wird sie rot (Jira Kanban).
- **Erledigt-Zeitpunkt:** wird gesetzt, wenn eine Karte in eine Spalte der Art „Erledigt“ kommt, und gelöscht, wenn sie zurückgeht.
- **Ausblenden:** Erledigte Karten älter als 14 Tage sind standardmässig ausgeblendet.
- **Swimlanes:** Zieht man eine Karte in eine andere Swimlane, ändert sich auch Person, Epic oder Priorität.

### Einsatz im Arbeitsalltag

- **Teams:** Im Stand-up das Board nach Person gruppieren und von rechts nach links durchgehen: Was ist fast fertig?
- **Projektleiter:** WIP-Grenzen setzen, damit nicht zu viel gleichzeitig angefangen wird.

### Grenzen und Fallstricke

Ziehen braucht einen Browser mit Maus; auf dem Telefon ändert man den Status im Aufgabenpanel. Nicht-Mitglieder sehen das Board, können aber nichts ziehen.

## Aufgabe im Detail

### Wozu

Ein Klick auf eine Karte öffnet rechts das Panel mit allen Angaben. Jede Aufgabe hat eine eigene Adresse und lässt sich als Link teilen.

### Was man sieht

Titel, Status, zugewiesene Person (mit „Mir zuweisen“), Typ (Aufgabe, Story, Fehler, Epic, Unteraufgabe), Priorität in fünf Stufen, Start, Fälligkeit, Story Points, Epic, Sprint, Labels, Farbe, Beschreibung mit Formatierung, Checkliste mit Fortschrittsbalken, Unteraufgaben, Kommentare und der Verlauf aller Änderungen. Änderungen werden sofort gespeichert.

### Wie gerechnet wird

- **Verlauf:** jede Änderung mit Person und Zeit, zum Beispiel „hat LOG-12 geändert: Priorität High, fällig 20.10.2026“.
- **Unteraufgaben:** zählen als erledigt, wenn sie in einer Spalte der Art „Erledigt“ liegen.

### Einsatz im Arbeitsalltag

- **Alle Mitarbeitenden:** Grosse Aufgaben in Unteraufgaben oder eine Checkliste zerlegen; der Fortschritt erscheint auf der Karte.
- **Fachspezialisten:** Rückfragen als Kommentar statt per Mail; kommentieren darf jeder Angemeldete, auch ohne Mitgliedschaft.

### Grenzen und Fallstricke

Archivieren statt Löschen: archivierte Aufgaben verschwinden aus allen Ansichten, bleiben aber in der Datenbank. Zuweisen kann man nur an Mitglieder des Projekts.

## Backlog und Sprints

### Wozu

Wie in Jira: Aufgaben sammeln, nach Wichtigkeit ordnen und in Sprints einplanen.

### Was man sieht

Oben der laufende Sprint, darunter geplante Sprints und zuunterst der Backlog. Je Abschnitt Zahl der Aufgaben und Summe der Story Points. Aufgaben zieht man zwischen den Abschnitten und innerhalb der Liste. „Sprint starten“ fragt nach Ziel und Dauer (1 bis 4 Wochen), „Sprint abschliessen“ verschiebt offene Aufgaben in den nächsten Sprint oder zurück in den Backlog.

### Wie gerechnet wird

- **Es läuft höchstens ein Sprint gleichzeitig.**
- **Unteraufgaben** stehen nicht einzeln im Backlog; sie folgen ihrer Aufgabe.

### Einsatz im Arbeitsalltag

- **Product Owner und Projektleiter:** Den Backlog vor der Sprintplanung von oben nach unten ordnen.
- **Team:** In der Planung so viele Story Points in den Sprint ziehen, wie im letzten Sprint erledigt wurden.

### Grenzen und Fallstricke

Den Backlog gibt es auch bei Kanban- und Planner-Projekten; Sprints sind dort freiwillig.

## Liste, Zeitachse und Kalender

### Wozu

Dieselben Aufgaben in drei weiteren Sichten: als Tabelle, als Zeitplan und als Monatskalender.

### Was man sieht

Die **Liste** ist eine sortierbare Tabelle mit Status zum Umstellen und CSV-Export. Die **Zeitachse** zeigt Balken von Start bis Fälligkeit, nach Epic gruppiert, mit Heute-Linie; Epics zeigen ihren Fortschritt. Der **Kalender** zeigt die Aufgaben am Fälligkeitstag; zieht man eine Aufgabe auf einen anderen Tag, ändert sich die Fälligkeit.

### Wie gerechnet wird

- **Zeitachse:** ohne Startdatum gilt das Erstelldatum; ohne Fälligkeit erscheint ein Punkt.
- **Epic-Balken:** vom frühesten Start bis zur spätesten Fälligkeit der zugehörigen Aufgaben.

### Einsatz im Arbeitsalltag

- **Geschäftsleitung:** Die Zeitachse zeigt, welche Epics über das geplante Ende hinauslaufen.
- **Projektleiter:** Die Liste nach Fälligkeit sortieren und als CSV für Besprechungen exportieren.

### Grenzen und Fallstricke

Abhängigkeiten zwischen Aufgaben werden nicht gezeichnet.

## Diagramme

### Wozu

Planner-Diagramme und Jira-Berichte für den Stand des Projekts.

### Was man sieht

Ring nach Status mit Anzahl und Anteil, Balken nach Person (gestapelt offen, in Arbeit, erledigt), offene Aufgaben nach Priorität, Burndown des laufenden Sprints und Durchsatz der letzten acht Wochen (angelegt und erledigt je Kalenderwoche). Die Filter oben wirken auch hier.

### Wie gerechnet wird

- **Burndown:** je Sprinttag die Summe der Story Points der noch nicht erledigten Aufgaben; ohne Punkte die Anzahl Aufgaben. Die gestrichelte Linie ist der ideale gleichmässige Verlauf bis null am Sprintende. Gerechnet wird mit der heutigen Sprintbelegung; später hinzugefügte Aufgaben zählen ab Sprintbeginn mit.
- **Durchsatz:** angelegt nach Erstelldatum, erledigt nach Erledigt-Zeitpunkt, je Kalenderwoche.

### Einsatz im Arbeitsalltag

- **Team und Scrum Master:** Liegt die Burndown-Linie deutlich über der Ideallinie, im Daily ansprechen, was den Sprint gefährdet.
- **Abteilungsleiter:** Mehr angelegt als erledigt über mehrere Wochen heisst: das Team nimmt mehr an, als es abschliessen kann.

### Grenzen und Fallstricke

Statusfarben: Bernstein = offen, Blau = in Arbeit, Grün = erledigt; die Zahl steht immer daneben.

## Aktivität und Einstellungen

### Wozu

Die Aktivität zeigt alles, was im Projekt passiert ist. Die Einstellungen verwalten Projekt, Team und Spalten.

### Was man sieht

**Aktivität:** die letzten 80 Änderungen mit Person, Text, Aufgabe und Zeit. **Einstellungen:** Name, Beschreibung, Start, Ende, Farbe, Symbol, Archivieren; Team mit Personensuche im AD (Name oder Kürzel); Spalten mit Name, Art (offen, in Arbeit, erledigt), WIP-Grenze, Verschieben und Löschen.

### Wie gerechnet wird

- **Spalte löschen:** Karten wandern in die erste Spalte derselben Art, sonst in die erste Spalte.

### Einsatz im Arbeitsalltag

- **Projektleiter:** Zum Projektstart Team aufnehmen und die Spalten an den eigenen Ablauf anpassen, zum Beispiel eine Spalte „Freigabe“.

### Grenzen und Fallstricke

Die Projektleitung bleibt immer im Team und kann nicht entfernt werden. Der Schlüssel lässt sich nicht ändern.

## Portfolio nach HERMES

### Wozu

Der Reiter **Portfolio (HERMES)** zeigt alle Projekte so, wie die Geschäftsleitung sie nach HERMES sehen will:
Stufe, Ampel, Phase und der nächste Entscheid auf einen Blick.

### Was man sieht

Oben ein Ampelring und die Verteilung auf Kleinauftrag, Vorhaben und Projekt; ein Klick darauf filtert. Darunter
die Phasenlandkarte mit den Phasen Initialisierung, Umsetzung, Einführung und abgeschlossen, getrennt durch die
Entscheidungspunkte ① Freigabe, ② Abnahme und ③ Abschluss. Jedes Projekt ist eine Kachel in seiner Phase, der
Rand hat die Farbe seiner Ampel. Ganz unten die Tabelle für die Geschäftsleitung, rote Projekte zuerst.

### Wie gerechnet wird

Die Phase ergibt sich aus den eingetragenen Entscheidungspunkten. Die Stufe schlägt die App nach dem geschätzten
Aufwand vor: unter 5 Personentagen Kleinauftrag, bis 40 Vorhaben, darüber Projekt. Gepflegt werden die Angaben in
den Einstellungen des Projekts im Block „HERMES“.

### Einsatz im Arbeitsalltag

Vor der Freigabe Auftraggeber, Anwendervertreter und „Erledigt, wenn“ eintragen; das Datum der Freigabe setzen,
sobald der Projektauftrag unterschrieben ist. Ampel und Stand in einem Satz bei jeder Statusrunde nachführen.
Das Demoprojekt HDEMO zeigt ein ausgefülltes Beispiel.

### Grenzen und Fallstricke

Projekte ohne HERMES-Angaben erscheinen nicht auf der Landkarte, nur als Liste darüber. Abnahme und Abschluss
lassen sich erst nach der Freigabe eintragen. Die Vorhaben aus der Wochen-Todo kommen nicht von selbst; seit dem 29. Oktober 2026 übernimmt sie der Knopf „Vorhaben aus Wochen_Todo übernehmen“ in der Übersicht (siehe unten).

## Raster & Gantt

### Wozu

Die Ansicht für die Terminplanung, wie man sie aus Smartsheet kennt: links eine Tabelle mit allen Zeilen des Projekts, rechts das Gantt-Diagramm. Termine, Dauern und Abhängigkeiten werden hier geplant; Board, Backlog und Liste zeigen dieselben Aufgaben.

### Was man sieht

- **Tabelle:** Nr. (die Aufgabennummer), Aufgabe mit Einrückung (Epic, Aufgabe, Unteraufgabe; Sammelzeilen auf- und zuklappbar), Zugewiesen, Status, Start, Ende, Dauer in Arbeitstagen, Vorgänger, % erledigt, Priorität, Risiko und Kritisch.
- **Gantt:** Balken je Zeile mit dunklerem Fortschrittsanteil, Sammelzeilen als schwarze Klammer, Meilensteine als Raute, Pfeile von Vorgänger zu Nachfolger, Heute-Linie, Wochenenden und Zürcher Feiertage schattiert. Eingeschaltet: kritischer Pfad rot und Basisplan als grauer Balken darunter.
- **Werkzeugleiste:** Zeile einfügen, einrücken, ausrücken, hoch, runter, alle auf- und zuklappen, Zoom Tage, Wochen, Monate, Heute, Basisplan setzen oder löschen (nur Projektleitung), Suche, Person, Überfällig und Spalten ein- und ausblenden.

### Wie gerechnet wird

- **Arbeitstage:** Montag bis Freitag ohne die gesetzlichen Feiertage des Kantons Zürich. Ein Termin auf einem freien Tag rückt auf den nächsten Arbeitstag. Das Ende zählt mit; ein Meilenstein hat die Dauer 0.
- **Vorgänger:** Nummern mit Komma, zum Beispiel „3, 5+2“ heisst „nach 3 und zwei Arbeitstage nach 5“. Der Nachfolger beginnt am Arbeitstag nach dem Ende des Vorgängers plus Versatz. Verschiebt sich der Vorgänger, rückt der Nachfolger mit, auch nach vorne.
- **Ziehen:** Ein ganzer Balken verschiebt Start und Ende bei gleicher Dauer; die Griffe links und rechts ändern Start oder Ende. Wird ein Nachfolger früher gezogen als erlaubt, rastet er auf den frühesten Start; später erhöht den Versatz zum Vorgänger. Vom Punkt am Balkenende auf einen anderen Balken ziehen legt eine Abhängigkeit an.
- **Sammelzeilen:** Start und Ende sind die früheste und späteste Unterzeile, der Fortschritt ist nach Dauer gewichtet. Sie lassen sich nicht ziehen.
- **Kritischer Pfad:** Zeilen ohne Puffer bis zum Projektende; eine Verschiebung dort verschiebt das Projektende. In der Spalte Kritisch steht sonst der Puffer in Arbeitstagen.
- **Erledigt:** Eine Aufgabe in einer Spalte der Art „Erledigt“ hat 100 %.
- **Basisplan:** friert die aktuellen Termine ein, um später Plan und Ist zu vergleichen.

### Einsatz im Arbeitsalltag

- **Projektleiter:** Den Plan mit Phasen als Epics aufbauen, Vorgänger eintragen und den Basisplan setzen, sobald der Plan freigegeben ist. In der Statusrunde den kritischen Pfad einschalten und die roten Zeilen besprechen.
- **Teammitglied:** % erledigt und Termine der eigenen Zeilen nachführen; die Nachfolger passen sich von selbst an.

### Grenzen und Fallstricke

Es gibt nur Ende-Anfang-Abhängigkeiten. Die Gliederung hat drei Ebenen (Epic, Aufgabe, Unteraufgabe). Monats- und Tagesnamen der Skala sind immer deutsch. Ein Leser sieht die Ansicht, kann aber nichts ziehen oder ändern.

## Risiken

### Wozu

Das Risikoregister des Projekts: Risiken erfassen, nach einer einheitlichen Skala bewerten, in Franken beziffern, mit Massnahmen und Verantwortlichen führen und ihren Verlauf verfolgen.

### Was man sieht

- **Kopf:** aktive Risiken je Stufe, höchste Stufe, überfällige Überprüfungen, Summe Erwartungswert und Summe Reserve.
- **Risikomatrix 5×5:** Wahrscheinlichkeit senkrecht, Auswirkung waagrecht; jede Zelle zeigt den Wert und die Anzahl Risiken, ein Klick filtert die Liste.
- **Liste und Seitenpanel:** Titel, Kategorie, Wahrscheinlichkeit, Auswirkung, Wert, Kritikalität, Status, Verantwortlich, verknüpfte Aufgabe, Überprüfung, Massnahme, Notfallplan, Kosten bei Eintritt, Erwartungswert und Reserve.
- **Risiko-Burndown:** Verlauf der aktiven Risiken und des Erwartungswerts über die Zeit.
- Auf jeder Projektkarte ein Abzeichen mit der höchsten Stufe, im Portfolio eine Spalte Risiko.

### Wie gerechnet wird

- **Kritikalität** = Wahrscheinlichkeit × Auswirkung (je 1 bis 5): 1 bis 4 tief, 5 bis 9 mittel, 10 bis 15 hoch, 16 bis 25 kritisch.
- **Erwartungswert** = Kosten bei Eintritt × Wahrscheinlichkeit, mit 10, 30, 50, 70 und 90 % für die Stufen 1 bis 5.
- **Reserve:** von Hand je Risiko, als Vorschlag der Erwartungswert; die Summe gehört ins Projektbudget.
- **Aktiv** sind die Status offen, in Bearbeitung und eingetreten; geschlossene und archivierte zählen nicht.
- **Burndown:** je Projekt und Tag ein Stand, bei jeder Änderung neu geschrieben.

### Einsatz im Arbeitsalltag

- **Projektleiter:** Zu Projektbeginn die Standardrisiken der Vorlage übernehmen und anpassen, jedes Risiko einer Person zuordnen und ein Überprüfungsdatum setzen. Hohe und kritische Risiken in jeder Statusrunde.
- **PMO und Finanzen:** Die Summe der Reserven je Projekt zeigt, was aus kaufmännischer Vorsicht einzuplanen ist (Sitzung vom 29. Oktober 2026).

### Grenzen und Fallstricke

Die Skala und die Standardrisiken sind ein Startvorschlag; verbindlich macht sie erst das PMO. Die Franken sind Schätzungen und werden nicht nach SAP übertragen.

## Projektstrukturplan

### Wozu

Der Projektstrukturplan ist die Schnittstelle zu SAP PS: je Element Geld und Zeit, so wie es Finanzen und Controlling brauchen.

### Was man sieht

PSP-Elemente mit Code (zum Beispiel LIN.01.02), Bezeichnung, Start, Ende, Soll-Stunden, Soll-Franken, Kostenart CAPEX oder OPEX und Verantwortlich; darunter die Summen und der Export als CSV für Excel und SAP.

### Wie gerechnet wird

Der Code folgt der Gliederung aus Raster & Gantt. Die Summe eines Elements ist der eigene Wert plus alle Unterelemente; CAPEX und OPEX zählen nur die eigenen Werte jeder Zeile, damit nichts doppelt läuft.

### Einsatz im Arbeitsalltag

- **Projektleiter:** Zur Freigabe Soll-Stunden und Soll-Franken je Arbeitspaket eintragen und die Kostenart wählen, dann den Export an das Controlling geben.

### Grenzen und Fallstricke

Nach SAP wird nichts geschrieben; der Export ist eine Vorlage. Ist-Werte aus SAP fehlen noch.

## Entscheide

### Wozu

Das Decision Log hält fest, wer wann was entschieden hat und warum, damit spätere Fragen ohne Mailsuche beantwortet sind.

### Was man sieht

Je Entscheid Schlüssel (zum Beispiel LIN-D1), Datum, Titel, Status (vorgeschlagen, entschieden, verworfen, abgelöst), Gremium oder Person, Begründung, Alternativen und eine verknüpfte Aufgabe.

### Wie gerechnet wird

Ein entschiedener Eintrag braucht ein Datum. Die Liste ist nach Datum sortiert, neueste zuerst.

### Einsatz im Arbeitsalltag

- **Projektleiter:** Jeden Entscheid aus Lenkungsausschuss oder Statusrunde gleich eintragen und mit der betroffenen Aufgabe verknüpfen.

### Grenzen und Fallstricke

Ein abgelöster Entscheid bleibt sichtbar; den neuen als eigenen Eintrag erfassen.

## Vorhaben aus der Wochen-Todo übernehmen

### Wozu

Bringt die Vorhaben PM-01 und folgende aus der Wochen-Todo als eigene Projekte in Trafag Projekte.

### Was man sieht

In der Übersicht der Knopf „Vorhaben aus Wochen_Todo übernehmen“: zwei Dateien hochladen (Vorhaben_HERMES.tsv, dazu optional Wochen_Todo.tsv), danach das Ergebnis mit Links zu den Projekten.

### Wie gerechnet wird

Je Vorhaben ein Projekt mit dem Schlüssel PM01, PM02 und so weiter, die HERMES-Angaben aus der Datei und die Zeilen der Wochen-Todo mit derselben ID als Aufgaben. Ein erneuter Abgleich ändert dieselben Projekte; die Datei bleibt führend.

### Einsatz im Arbeitsalltag

- **Ingo:** Nach dem Nachführen der Wochen-Todo beide Dateien erneut hochladen.

### Grenzen und Fallstricke

Neue Projekte sind privat; nur das Team sieht sie. Die Sichtbarkeit lässt sich in den Einstellungen umstellen.

## Kennzahlen-Glossar

| Kennzahl | Bedeutung | Berechnung in Worten |
|---|---|---|
| Fortschritt | Erledigungsgrad eines Projekts | Erledigte Aufgaben geteilt durch alle Aufgaben, ohne Epics und Archiv |
| Offen, In Arbeit, Erledigt | Arbeitsstand | Nach der Art der Spalte, in der die Aufgabe liegt |
| Überfällig | Termin verpasst | Fälligkeit vor heute und nicht erledigt |
| WIP-Grenze | Höchstzahl Karten in Arbeit | Von der Projektleitung je Spalte gesetzt, 0 = keine |
| Story Points | Geschätzter Aufwand | Von Hand je Aufgabe, 0 bis 100 |
| Burndown | Restaufwand im Sprint | Story Points (sonst Anzahl) der unerledigten Sprint-Aufgaben je Tag |
| Durchsatz | Tempo des Teams | Angelegte und erledigte Aufgaben je Kalenderwoche |
| Dauer | Länge einer Zeile | Arbeitstage von Start bis Ende, beide eingeschlossen, ohne Wochenenden und Zürcher Feiertage |
| Kritischer Pfad | Zeilen ohne Puffer | Rückwärtsrechnung vom Projektende über die Vorgänger |
| Kritikalität Risiko | Gewicht eines Risikos | Wahrscheinlichkeit mal Auswirkung, je 1 bis 5 |
| Erwartungswert | Erwartete Kosten eines Risikos | Kosten bei Eintritt mal Wahrscheinlichkeit in Prozent |
| Soll-Franken gesamt | Budget eines PSP-Elements | Eigener Wert plus alle Unterelemente |
