# Trafag Projekte (Projektmanagement wie Jira, Trello und Planner)

Stand: 2026-10-09

Auftrag Ingo 2026-10-09: die „Poor Man's Project Management Suite“ durch eine richtige Lösung ersetzen,
„ein Jira-Trello-Projektmanagementsystem, Funktionen von Jira, Ansicht Trello möglich und das Beste von
Microsoft Planner“. Entscheide Ingo am selben Tag:

- Name **Trafag Projekte**, oberste Menüebene.
- **Alle sehen, Mitglieder bearbeiten**; jeder Angemeldete darf ein Projekt anlegen und wird dessen Leitung.
- Die alten Einträge waren nie genutzt (produktiv 0 Projekte, geprüft an der Sicherung vom 09.10.) und
  werden **nicht übernommen**.
- **Deploy für alle**, auch nach dem Hinweis auf Philip Steigers Project-Power-Pack-Test (Memory vom
  2026-08-19): Philips PMO-Bedarf gilt als andere Zielgruppe.
- Sichtprüfung in Produktion mit einem Testprojekt, das danach archiviert wird.

## Funktionen nach Vorbild

| Vorbild | Funktion |
|---|---|
| Jira | Projektschlüssel und Nummern (LOG-12), Typen Aufgabe/Story/Fehler/Epic/Unteraufgabe, fünf Prioritäten, Story Points |
| Jira | Backlog, Sprints mit Ziel und Dauer, Sprint abschliessen mit Übertrag, Burndown, Swimlanes, WIP-Grenzen |
| Jira | Liste mit Sortierung und CSV, Filter (Suche, Personen, Typ, Priorität, Label, Epic, Überfällig), Verlauf je Aufgabe, Aktivität je Projekt |
| Trello | Board mit Listen, Karten ziehen und sortieren, Farbband, farbige Labels, Schnellerfassung am Spaltenende |
| Planner | Vorlage mit Buckets, Checklisten mit Fortschritt, „Meine Aufgaben“ über alle Projekte (Überfällig/Heute/Woche/Später), Kalender, Diagramme |
| eigen | Zeitachse nach Epic, Kalender mit Ziehen zum Umplanen, Live-Aktualisierung offener Seiten, AD-Personensuche |

Rechenregeln (Fortschritt, Überfällig, Burndown, Durchsatz) stehen im Handbuch Kapitel 8
(`wwwroot/handbuch/kapitel/08_projekte.md`) und im Code `Services/Projects/PmService.cs`.

## Rechte

| Wer | Darf |
|---|---|
| alle Angemeldeten | alles sehen, Projekte anlegen, Aufgaben kommentieren |
| Mitglieder | Aufgaben anlegen, ändern, ziehen, archivieren; Sprints anlegen, starten, abschliessen |
| Projektleitung | zusätzlich Projekt, Team und Spalten verwalten, Projekt archivieren |
| Admin (Admin-Passwort entsperrt) | wie Projektleitung in allen Projekten |

Zuweisen nur an Mitglieder. Aufgaben ausgetretener Mitglieder bleiben bearbeitbar; erledigte Aufgaben
behalten ihren abgeschlossenen Sprint (Fix `9d819ae`).

## Technik

| Was | Ort |
|---|---|
| Übersicht `/projekte`, Projekt `/projekte/{KEY}?view=…&task=KEY-n` | `Components/Pages/Projects.razor`, `Components/Pages/ProjectWorkspace.razor` |
| Ansichten und Panel | `Components/Projects/*.razor` (Board, Karte, Backlog, Liste, Zeitachse, Kalender, Diagramme, Einstellungen, Aufgabenpanel, Texte `PmLabels`) |
| Service, Rang, Burndown | `Services/Projects/PmService.cs`, DTOs und `PmNotifier` in `Services/Projects/PmModels.cs` |
| Tabellen `PmProjects`, `PmMembers`, `PmColumns`, `PmSprints`, `PmTasks`, `PmChecklistItems`, `PmComments`, `PmActivities` | `Models/PmModels.cs`, `DatabaseInitializationService.SchemaSql.cs` (`GetPmCreateSql`, Indizes), `DatabaseSchemaMaintenanceService.EnsurePmTables` |
| Menü `projects` (oberste Ebene, Sortierung 40), Migration der alten Gruppe | `Services/DatabaseSeedService.cs` |
| Personensuche im AD (anr, aktive Konten, höchstens 10) | `Services/Forum/ForumUserDirectory.SearchAsync` |
| CSS (Präfix `pm-`), Statusfarben validiert | `wwwroot/css/app.css` |
| Tests | `TrafagSalesExporter.Tests/PmServiceTests.cs` |

- **Rang:** Kommazahl; Einfügen vor einer Karte nimmt die Mitte zum Vorgänger, ans Ende +1000.
- **Schreibschleuse:** Anlegen (Nummernvergabe), Ziehen und Einplanen laufen nacheinander.
- **Erledigt-Zeitpunkt:** beim Wechsel in eine Spalte der Art „Erledigt“ gesetzt, beim Zurückziehen gelöscht.
- **Statusfarben:** Offen Bernstein, In Arbeit Blau, Erledigt Grün; hell `#C98A12/#1E88E5/#2F9E44`,
  dunkel `#B98322/#3D8FE0/#3A9F55`, mit dem Palettenprüfer der Dataviz-Vorgaben geprüft (alle Prüfungen
  bestanden; Bernstein hell unter 3:1 Kontrast, darum stehen die Zahlen immer daneben).
- **Alte Projektsuite:** Seite, Service, Modell und Tests entfernt; Tabelle `ProjectItems` bleibt in
  bestehenden Datenbanken ungenutzt liegen. Die Menügruppe „Poor Man's Project Management Suite“ wird beim
  Start entfernt, der Link `projects` wandert auf die oberste Ebene.

## Stand

- 2026-10-09: gebaut (`df4fa60`, Fix `9d819ae`), **produktiv seit 10:18** (Stand `e879bab`).
- CSS-Korrektur (Formularbeschriftung, umbrechende Zeilen) **produktiv seit 11:07** (`41f2423`).

## Deploy

- 2026-10-09 10:18 aus dem sauberen Worktree (ohne Finance_All), Release-Tests 1150/1150, DLL bitgleich,
  `/projekte` 200. Protokoll in `docs/rag/DEPLOYMENT.md`. *Korrigiert:* Der Runner hing nicht, er lief nur
  lange; die fehlende Ausgabe kam vom angehaengten `grep`.
- Sichtpruefung produktiv per Edge headless mit Windows-Anmeldung und dem Testprojekt „ZZ Test Trafag
  Projekte“ (ZTTP, Scrum): Projekt angelegt, sechs Aufgaben, vier per Ziehen in den Sprint, Sprint mit Ziel
  gestartet, Aufgabe im Panel zugewiesen, Prioritaet, Faelligkeit, Punkte, Checkliste und Unteraufgabe gesetzt,
  Karte auf dem Board in „In Arbeit“ gezogen; Board, Backlog, Liste, Zeitachse, Kalender, Diagramme
  (Burndown, Durchsatz), Aktivitaet, Einstellungen, Uebersicht und Meine Aufgaben angesehen. Danach
  archiviert; die Uebersicht zeigt wieder 0 Projekte (sichtbar mit „Archivierte zeigen“).
- 2026-10-09 11:07 zweiter Deploy mit der CSS-Korrektur, produktiv nachgeprueft.
- Gefunden und behoben (seit 11:07 produktiv): die Klasse `pm-label` war fuer Formularbeschriftung und
  Trello-Label doppelt vergeben (Beschriftung „Person hinzufuegen“ abgeschnitten); Eingabefelder in Zeilen
  brachen um (Zuweisen im Panel, neue Spalte).

## Offen

- Keine Mail-Benachrichtigung bei Zuweisung oder Kommentar.
- Abhängigkeiten zwischen Aufgaben (blockiert durch) gibt es nicht.
- Abgleich mit Philip Steiger (Project Power Pack) bleibt sinnvoll, blockiert aber nichts.
