# Projekte

Das Modul Projekte ist eine einfache Projektverwaltung für Status, Verantwortung, Termine und Fortschritt. Es richtet sich an Projektleiter, Abteilungsleiter und Geschäftsleitung, die eine gemeinsame Liste der laufenden Vorhaben führen wollen. Es liegt im Menü in der Gruppe „Poor Man's Project Management Suite“ und trägt den Namen Projekte. Die Einträge werden von den Anwendern selbst erfasst und in der Datenbank des Cockpits gespeichert; es gibt keine Anbindung an SAP oder andere Systeme. Die Daten sind so aktuell, wie sie jemand pflegt. Daneben gibt es unter dem Menüpunkt „Pause“ ein kleines Spiel für die Pause, das keine Geschäftsdaten verwendet und im Browser läuft.

## Projekte

### Wozu

Die Seite ersetzt die Excel-Liste „Wer macht was bis wann“. Sie zeigt auf einen Blick, wie viele Projekte aktiv sind, welche in Arbeit oder blockiert sind und was bereits abgeschlossen ist. Der Name sagt es: Es ist bewusst eine einfache Lösung ohne Ressourcenplanung, Abhängigkeiten oder Budget.

### Was man sieht

Oben vier Kennzahlen: aktive Projekte, in Arbeit, blockiert und abgeschlossen. Darunter ein Formular zum Erfassen und Bearbeiten mit Projekttitel, Verantwortlichem, Status (Idee, Geplant, In Arbeit, Blockiert, Abgeschlossen), Priorität (Niedrig, Normal, Hoch, Kritisch), Startdatum, Fälligkeitsdatum, Fortschritt in Prozent, Beschreibung und Notizen. Darunter die Projektübersicht als Tabelle mit Statusfarbe, Fälligkeitsdatum und Fortschrittsbalken. Mit dem Stift bearbeitet man ein Projekt, mit dem Archiv-Symbol legt man es ab. Ein Schalter blendet archivierte Projekte ein. Nur der Titel ist Pflicht.

### Wie gerechnet wird

- **Aktive Projekte:** alle nicht archivierten Projekte, die nicht den Status „Abgeschlossen“ haben.
- **In Arbeit, Blockiert, Abgeschlossen:** Anzahl der nicht archivierten Projekte mit diesem Status.
- **Sortierung:** Abgeschlossene Projekte stehen unten; innerhalb der Gruppen kommen zuerst die Projekte mit Fälligkeitsdatum, das früheste zuerst, danach Projekte ohne Datum, jeweils alphabetisch.
- **Fortschritt:** Der Prozentwert wird von Hand eingetragen und auf den Bereich 0 bis 100 begrenzt. Er wird nicht aus Aufgaben berechnet.

### Einsatz im Arbeitsalltag

- **Projektleiter:** Nach jedem Treffen Status und Fortschritt nachführen; ein blockiertes Projekt mit einer Notiz zur Ursache versehen.
- **Abteilungsleiter:** Vor dem Wochenmeeting die Tabelle durchgehen: Was ist fällig, was ist blockiert, wer ist verantwortlich?
- **Geschäftsleitung:** Die vier Kennzahlen oben als schnellen Überblick über die Projektlast verwenden.

### Grenzen und Fallstricke

Es gibt keine Benutzerrechte pro Projekt, keine Änderungshistorie und keine automatische Erinnerung bei überfälligen Terminen. Hinweis: nicht geprüft, wer das Menü sehen darf und ob Einträge für alle sichtbar sind; im Menü ist der Punkt für alle freigegeben. Überfällige Projekte werden nicht gesondert hervorgehoben; man erkennt sie nur am Datum. Archivieren ist in der Oberfläche vorgesehen, Löschen nicht. Der Fortschritt ist eine Schätzung der Person, die ihn einträgt.

## Kennzahlen-Glossar

| Kennzahl | Bedeutung | Berechnung in Worten |
|---|---|---|
| Aktive Projekte | Projekte, die noch laufen oder geplant sind | Nicht archivierte Projekte ohne Status „Abgeschlossen“ |
| In Arbeit | Projekte in Umsetzung | Nicht archivierte Projekte mit Status „In Arbeit“ |
| Blockiert | Projekte, die nicht weiterkommen | Nicht archivierte Projekte mit Status „Blockiert“ |
| Abgeschlossen | Fertige Projekte | Nicht archivierte Projekte mit Status „Abgeschlossen“ |
| Fortschritt | Geschätzter Erledigungsgrad | Von Hand eingetragener Prozentwert zwischen 0 und 100 |
