# Trafag Projekte (Projektmanagement wie Jira, Trello und Planner)

Stand: 2026-10-29 (Smartsheet-Ansicht Raster & Gantt, Risikoregister, PMO aus der Sitzung vom 29.10., Uebernahme der Vorhaben, Abschnitt „Smartsheet-Ansicht, Risiken und PMO“; davor 2026-10-13: HERMES-Angaben, Portfolio und Demoprojekt ergänzt; nachmittags private Projekte, Rolle Leser und Abgleich der Wochen_Todo, Abschnitt „Sichtbarkeit und Wochen_Todo“)

Auftrag Ingo 2026-10-09: die „Poor Man's Project Management Suite“ durch eine richtige Lösung ersetzen,
„ein Jira-Trello-Projektmanagementsystem, Funktionen von Jira, Ansicht Trello möglich und das Beste von
Microsoft Planner“. Entscheide Ingo am selben Tag:

- Name **Trafag Projekte**, oberste Menüebene.
- **Alle sehen, Mitglieder bearbeiten**; jeder Angemeldete darf ein Projekt anlegen und wird dessen Leitung.
  *Ergänzt am 2026-10-13:* Ein Projekt kann privat sein; dann sieht es nur sein Team.
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

*Stand bis 2026-10-13 vormittags; für private Projekte gilt seither der Abschnitt „Sichtbarkeit und Wochen_Todo“.*

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

## HERMES (2026-10-13)

Wunsch Ingo am 2026-10-13: die HERMES-Elemente, die in `projektmanagement/Wochen_Todo.xlsx` und
`Vorhaben_HERMES.tsv` schon gepflegt werden, auch in Trafag Projekte, grafisch und interaktiv. Entscheide Ingo:
Felder je Projekt plus Portfolio-Reiter, **keine** Übernahme der Vorhaben PM-01 bis PM-12 (die Wochen-Todo bleibt
deren Quelle), dazu ein Demoprojekt zum Anschauen.

- **Felder je Projekt** (Einstellungen, Block „HERMES“, pflegen darf die Projektleitung oder ein Admin):
  Aufwand in Personentagen mit Skala und Stufenvorschlag (unter 5 Kleinauftrag, 5 bis 40 Vorhaben, darüber
  Projekt, wie im Blatt „HERMES Übersicht“), Stufe, Ampel, Auftraggeber, Anwendervertreter, Stand in einem Satz,
  nächster Entscheid, „Erledigt, wenn“ (Abnahmekriterien) und die Entscheidungspunkte ① Freigabe, ② Abnahme,
  ③ Abschluss mit Datum. Beim Kleinauftrag sind Rollen und Entscheidungspunkte ausgeblendet.
- **Regeln:** keine Abnahme ohne Freigabe, kein Abschluss ohne Abnahme, Daten in dieser Reihenfolge. Die Phase
  folgt aus den Punkten: Initialisierung, nach ① Umsetzung, nach ② Einführung, nach ③ abgeschlossen.
- **Grafik:** HERMES-Pfad (vier Phasen, Rauten ①②③, aktuelle Phase läuft animiert, Hinweise beim Darüberfahren);
  auf jeder Projektkarte Ampel, Stufe, Phase und die drei Rauten.
- **Reiter „Portfolio (HERMES)“** in der Projektübersicht: Ampelring und Stufenbalken filtern per Klick,
  Phasenlandkarte mit jedem Projekt als Kachel in seiner Phase (Rand in Ampelfarbe, Fortschritt, nächster
  Entscheid), Tabelle „Übersicht für die Geschäftsleitung“ (rote zuerst) mit kleinem Pfad je Zeile; Klick auf
  eine Phase filtert. Projekte ohne Angaben werden mit Schlüssel gelistet.
- **Demoprojekt `HDEMO`** „HERMES-Demo: Beispielvorhaben“: wird beim App-Start einmalig angelegt (Leitung
  koi), mit erfundenen Werten, fünf Aufgaben entlang der Entscheidungspunkte und Freigabe am Anlagetag.
  Archivieren blendet es aus; es kommt nicht wieder, solange der Schlüssel existiert.
- **Technik:** eigene Tabelle `PmHermes` (eine Zeile je Projekt, Index `UX_PmHermes_Project`), damit
  `PmProjects` unverändert bleibt; `PmService.Hermes.cs` (`SaveHermesAsync`, `ValidateHermes`,
  `SeedHermesDemoAsync`), Komponenten `PmHermesPath`, `PmHermesEffort`, `PmHermesPortfolio`, CSS-Präfixe
  `pm-hp-`, `pm-he-`, `pm-pf-`, `pm-light`, Texte in allen Sprachen, Tests in `PmServiceTests`.

## Stand

- 2026-10-09: gebaut (`df4fa60`, Fix `9d819ae`), **produktiv seit 10:18** (Stand `e879bab`).
- CSS-Korrektur (Formularbeschriftung, umbrechende Zeilen) **produktiv seit 11:07** (`41f2423`).
- 2026-10-13: HERMES-Angaben, Portfolio und Demoprojekt HDEMO **produktiv seit 09:34** (`7e20f93`), Darstellungsfix Phasenpfad **produktiv seit 09:47** (`47990ad`). Release-Tests 1158/1158; Sichtpruefung per Edge headless (alle Ansichten am Demoprojekt ohne Fehler, Bedienen wie Ziehen oder Speichern nicht angeklickt, durch Tests abgedeckt).
- 2026-10-13: private Projekte, Rolle Leser und Abgleich der Wochen_Todo **produktiv seit 14:43** (`6cc0035`), Release-Tests 1161/1161, ohne DB-Sicherung (abgebrochen, siehe `docs/rag/DEPLOYMENT.md`). Produktiv geprueft per Edge headless: Formular „Neues Projekt“ zeigt „Wer sieht es“, Einstellungen von HDEMO zeigen Sichtbarkeit, „Person hinzufügen als“ und „Wochen_Todo abgleichen“. Den ersten Abgleich macht Ingo selbst (privates Projekt anlegen, TSV hochladen).

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

## Sichtbarkeit und Wochen_Todo (2026-10-13)

Auftrag Ingo: „bring meine Wochen_Todo in die Trafag Projekte, stell auf privat, dass nur ich meine Projekte sehen kann; man soll als Owner sagen können, wer Projekte sieht“. Entscheide Ingo: Abgleich **wiederholbar** (TSV bleibt führend), **alle Zeilen** (erledigte in die Spalte Erledigt), privat heisst **auch keine Admins**.

| Wer | Öffentliches Projekt | Privates Projekt |
|---|---|---|
| alle Angemeldeten | sehen, anlegen, kommentieren | sehen es nicht (Übersicht, Portfolio, Link, Meine Aufgaben) |
| Leser (neue Rolle) | sehen | sehen alles, ändern nichts |
| Mitglieder | Aufgaben und Sprints ändern | Aufgaben und Sprints ändern |
| Projektleitung (Besitzer) | verwalten, Sichtbarkeit, Team mit Rolle | verwalten, Sichtbarkeit, Team mit Rolle |
| Admin | wie Projektleitung | **nichts**, ausser er ist im Team |

- Sichtbarkeit wählt man beim Anlegen („Wer sieht es“) oder in den Einstellungen; Wechsel steht im Verlauf. Bestehende Projekte bleiben öffentlich.
- Team: „Person hinzufügen als“ Mitglied oder Leser; die Rolle lässt sich je Person umstellen (`SetMemberRoleAsync`).
- **Abgleich Wochen_Todo** (Einstellungen, nur Projektleitung): `projektmanagement/Wochen_Todo.tsv` hochladen. Jede Zeile wird über ihre Nr. zu genau einer Aufgabe (`PmTasks.ExternalRef` = `WT-<Nr>`). Ein erneuter Abgleich legt neue Zeilen an und überschreibt Titel, Beschreibung (Nächster Schritt, Bereich, ID, Wer, Vorhaben, HERMES), Priorität, Termin und Labels (Bereich, Status, Dringlichkeit). Die Spalte wechselt nur bei anderer Art; Status „Erledigt…“ = erledigt, „Läuft/Umgesetzt…/In …/Teilweise…“ = in Arbeit, sonst offen. Prio Hoch/Mittel/Niedrig, Dringlichkeit „Heute“ = sehr hoch. „Wer“ mit Ingo wird dem Hochladenden zugewiesen. Aus der TSV verschwundene Zeilen bleiben stehen.
- Technik: `PmProjects.Visibility` (Public/Private) und `PmTasks.ExternalRef`, beim Start per `AddColumnIfMissing` nachgerüstet; Rechte zentral in `PmService.CanSee/CanManage/CanEditAsync`; Abgleich in `PmService.WochenTodo.cs`; Tests in `PmServiceTests` (privat/Leser, Abgleich, Schema-Nachrüstung). Commit `6cc0035`.

## Offen

- Keine Mail-Benachrichtigung bei Zuweisung oder Kommentar.
- ~~Abhängigkeiten zwischen Aufgaben (blockiert durch) gibt es nicht.~~ *Erledigt 2026-10-29: Ende-Anfang-Abhängigkeiten mit Versatz in der Smartsheet-Ansicht, siehe unten.*
- Abgleich mit Philip Steiger (Project Power Pack) bleibt sinnvoll, blockiert aber nichts.
  *Nachtrag 2026-10-29:* Philip testet das Project Power Pack weiter (Entscheid offen, `docs/SITZUNG_SAP_PS_PROJEKTMANAGEMENT_2026-10-29.md`); vor einer breiten Ankündigung der Smartsheet-Ansicht mit Philip bzw. Oskar abstimmen.

## Smartsheet-Ansicht, Risiken und PMO (2026-10-29)

Auftrag Ingo am 2026-10-29: „ein Modus, der genau wie Smartsheet aussieht, gleiche Funktionalität hat und sich vollständig
mit dem bestehenden Workflow einfügt; Balken in der Zeitachse per Maus verschieben, Zeiträume passen sich automatisch an;
alle Vorteile von Smartsheet“, dazu „Risikobewertung, Kritikalität“ und „alle bestehenden Projekte aus der Wochen_Todo
eintragen“. Am selben Tag nach der Sitzung SAP PS/Projektwerkzeug (`docs/SITZUNG_SAP_PS_PROJEKTMANAGEMENT_2026-10-29.md`):
Risiken in Franken, Vorlage Produktentwicklung, Projektstrukturplan für SAP, Decision Log. Entscheide Ingo: Vorhaben je ein
Projekt; Terminierung wie Smartsheet (Vorgänger, Nachfolger rücken mit, Arbeitstage, Sammelzeilen); Guide mit Rundgang,
Assistent und Projektprüfung (Phase 2); „sofort für alle“. Gebaut in Phasen; Phase 1 und PMO am 2026-10-29.

### Raster & Gantt (Reiter `sheet`)

- **Raster:** Nr. (Aufgabennummer, die auch die Vorgänger-Spalte nutzt), Aufgabe mit Einrückung und Auf-/Zuklappen,
  Zugewiesen, Status, Start, Ende, Dauer (Arbeitstage), Vorgänger („3, 5+2“), % erledigt, Priorität, Risiko, Kritisch.
  Bearbeiten per Klick in die Zelle, Enter speichert, Esc bricht ab, Pfeiltasten wechseln die Zelle. Werkzeugleiste: Zeile
  einfügen, Ein-/Ausrücken, hoch/runter, alle auf-/zuklappen, Zoom Tage/Wochen/Monate, Heute, kritischer Pfad,
  Basisplan anzeigen sowie setzen/löschen (Projektleitung), Suche, Person, Überfällig, Spalten ein-/ausblenden.
- **Gantt:** Skala mit Wochenenden und Zürcher Feiertagen, Heute-Linie, Balken mit Fortschritt, Sammelzeilen als Klammer,
  Meilensteine als Raute, kritischer Pfad rot, Basisplan grau, rechtwinklige Abhängigkeitspfeile. Ziehen in
  `wwwroot/js/pm-gantt.js`: Balken verschieben, linken/rechten Griff ziehen, Vorschau rastet auf Arbeitstage, beim Loslassen
  ein Aufruf an den Server; vom Punkt am Balkenende auf einen anderen Balken ziehen legt eine Abhängigkeit an.
- **Gliederung** = Epic → Aufgabe → Unteraufgabe; Ein-/Ausrücken setzen `EpicId`/`ParentId`, damit Board, Backlog und Liste
  dieselbe Struktur sehen. Zeilenfolge `PmTasks.SheetOrder` (ohne eigene Folge gilt der Rang).
- **Regeln** (`Services/Projects/PmSchedule.cs`, Tests `PmSheetTests`): Arbeitstage Mo bis Fr ohne Zürcher Feiertage, Ende
  inklusive, Meilenstein Dauer 0; Ende-Anfang mit Versatz in Arbeitstagen, der Nachfolger folgt in beide Richtungen;
  ein Balken später als erlaubt erhöht den Versatz zum bestimmenden Vorgänger, früher rastet er auf den frühesten Start;
  Sammelzeilen nehmen Spanne und dauergewichteten Fortschritt der Kinder und sind nicht ziehbar; Zyklen abgewiesen;
  kritischer Pfad per Rückwärtsrechnung (Puffer 0); Zeile in einer Erledigt-Spalte = 100 %.
- **Ein Einstieg für alle Terminänderungen:** `PmService.ApplyScheduleAsync` läuft auch nach Aufgabenpanel, Kalender,
  Board-Spaltenwechsel, Archivieren, Wochen_Todo-Abgleich und Vorlagen, damit kein alter Weg Abhängigkeiten bricht.
- Technik: `Components/Projects/PmSheetView.razor`, `Services/Projects/PmService.Sheet.cs`, `PmSheetLayout.cs` (Pixel, Skala,
  Pfeilwege, mit Tests), `wwwroot/css/pm-sheet.css` (`pm-sh-`), Tabelle `PmTaskLinks`, Spalten `PmTasks.PercentComplete`,
  `IsMilestone`, `SheetOrder`, `BaselineStart`, `BaselineEnd`.
- Aussehen an Smartsheet angelehnt, ohne Logo oder Marke von Smartsheet.

### Risiken (Reiter `risks`)

- Kopf mit aktiven Risiken je Stufe, höchster Stufe, überfälligen Überprüfungen, Summen Erwartungswert und Reserve;
  Risikomatrix 5×5 (Zelle mit Wert und Anzahl, Klick filtert), die fünf grössten Risiken, Liste mit Filtern, Seitenpanel.
- Kritikalität = Wahrscheinlichkeit × Auswirkung: 1–4 tief, 5–9 mittel, 10–15 hoch, 16–25 kritisch (`PmRiskLevels`).
- **In Franken** (Sitzung 29.10.): Kosten bei Eintritt, Erwartungswert = Kosten × Wahrscheinlichkeit (Stufe 1–5 = 10, 30,
  50, 70, 90 %), Reserve (Contingency) je Risiko und Summe je Projekt. **Risiko-Burndown** aus einem Tagesstand je Projekt
  (`PmRiskSnapshots`, bei jeder Änderung überschrieben).
- **PMO-Vorlage** (`Services/Projects/PmRiskTemplates.cs`): eine Skala für Wahrscheinlichkeit und Auswirkung mit Franken- und
  Terminschwellen, Standardrisiken je Projektart (Produktentwicklung, IT-Projekt, allgemein), per Knopf übernehmbar. Die
  Inhalte sind ein Startvorschlag; Inhaber der Vorlage wäre das PMO.
- Risiko-Badge auf den Projektkarten, Spalte Risiko im Portfolio. Tabellen `PmRisks`.

### Vorlagen mit fertigem Plan

- **Produktentwicklung** nach Philips Smartsheet-Vorlage: M1 Projektstart; Phase 1 Spezifikation und Entwicklung
  (Spezifikation, Fertigungskonzept, Softwarekonzept Betriebsmittelbau, Design mit Design-FMEA, Mechanik, Hardware/PCB,
  Firmware, Konzept Produktverpackung, Typenschild, Prototypen); M2 Design-Freeze; Phase 2 Industrialisierung; M3 Produktiv;
  Phase 3 Optimierung; Projektabschluss. Verkettet über Abhängigkeiten mit Vorschlagsdauern, plus Standardrisiken.
- **IT-Projekt nach HERMES:** Initialisierung, Konzept, Realisierung, Einführung mit je einem Entscheidungspunkt.
- Technik: `Services/Projects/PmService.Templates.cs`, `PmTemplates.ProductDevelopment`/`ItProject`.

### Projektstrukturplan für SAP PS (Reiter `wbs`)

- Je Element Soll-Stunden, Soll-Franken, Kostenart CAPEX/OPEX; Codes wie PSP-Elemente (`LIN.01.02`), Summen nach oben;
  CSV-Export (Semikolon, UTF-8) als Vorlage für SAP PS. Nach SAP wird nichts geschrieben; das ist die in der Sitzung
  genannte Schnittstelle zwischen Projektwerkzeug und PS. Spalten `PmTasks.PlannedHours`, `PlannedCostChf`, `CostType`.

### Decision Log (Reiter `decisions`)

- Je Entscheid Titel, Entscheid, Begründung, Alternativen, Status (vorgeschlagen, entschieden, verworfen, abgelöst), Datum,
  Gremium/Person, verknüpfte Aufgabe; Schlüssel `LIN-D1`. Tabelle `PmDecisions`.

### Übernahme der Vorhaben

- Übersicht, Knopf „Vorhaben aus Wochen_Todo übernehmen“: `Vorhaben_HERMES.tsv` (Pflicht) und `Wochen_Todo.tsv` hochladen.
  Je Vorhaben PM-nn ein Projekt `PMnn` (ExternalRef `PM-nn`), HERMES-Angaben aus der TSV, Aufgaben aus den Wochen_Todo-Zeilen
  mit ID oder Vorhaben PM-nn. **Neue Projekte sind privat** mit dem Hochladenden als Leitung (wie der Wunsch vom 13.10.);
  Sichtbarkeit danach in den Einstellungen umstellbar. Wiederholbar, die TSV bleibt führend. Technik
  `PmService.Vorhaben.cs`, `Components/Projects/PmVorhabenImport.razor`.

### Texte

Eigener Katalog je Ansicht `Services/Projects/PmUiTextCatalog.{Sheet,Risk,Guide}.cs` mit allen Sprachen inklusive
Berndeutsch; er füllt in `UiTextService` nur Lücken und überschreibt keine bestehende Übersetzung. Test
`Projects_Catalog_Has_Every_Language_Without_Duplicates_And_Keeps_Placeholders`.

### Offen

- Phase 2: Guide (Rundgang, Projekt-Assistent, geführte Projektprüfung).
- Phase 3: Auslastung je Person, Automatisierungen und Erinnerungen, Formulare für Anfragen, Excel-Export des Rasters.
- Abstimmung mit Philip Steiger (Project Power Pack) vor breiter Ankündigung.
