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

- 2026-10-09: gebaut (`df4fa60`, Fix `9d819ae`). Deploystatus siehe Abschnitt „Deploy“.

## Deploy

(wird nach dem Deploy nachgetragen)

## Offen

- Keine Mail-Benachrichtigung bei Zuweisung oder Kommentar.
- Abhängigkeiten zwischen Aufgaben (blockiert durch) gibt es nicht.
- Abgleich mit Philip Steiger (Project Power Pack) bleibt sinnvoll, blockiert aber nichts.
