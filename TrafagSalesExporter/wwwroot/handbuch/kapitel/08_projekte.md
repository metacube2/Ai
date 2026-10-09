# Trafag Projekte

Trafag Projekte ist das Projektmanagement im Cockpit. Es ersetzt seit dem 9. Oktober 2026 die frühere „Poor Man's Project Management Suite“, die nie genutzt wurde. Es verbindet drei bekannte Werkzeuge: von Trello das Board mit Listen, Karten zum Ziehen, Farben und Labels; von Jira die Schlüssel wie LOG-12, die Aufgabentypen, Epics, Backlog, Sprints mit Ziel, Story Points und Burndown; von Microsoft Planner die Buckets, Checklisten, „Meine Aufgaben“, Kalender und Diagramme. Der Menüpunkt heisst „Trafag Projekte“ und liegt auf oberster Ebene. Alle sehen alle Projekte; ändern dürfen die Mitglieder eines Projekts und Admins; Projekt, Team und Spalten verwaltet die Projektleitung. Geschrieben wird mit dem eigenen Windows-Namen. Daneben gibt es unter dem Menüpunkt „Pause“ ein kleines Spiel für die Pause, das keine Geschäftsdaten verwendet.

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

Drei Vorlagen: **Kanban-Board** (Trello, Spalten Zu erledigen, In Arbeit, Review, Erledigt), **Scrum mit Sprints** (Jira, Spalten Zu erledigen, In Arbeit, Test, Erledigt und ein erster Sprint), **Planner mit Buckets** (Ideen, Geplant, In Arbeit, Erledigt). Dazu Name, Schlüssel (2 bis 6 Grossbuchstaben, wird aus dem Namen vorgeschlagen), Beschreibung, Farbe und Symbol.

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
