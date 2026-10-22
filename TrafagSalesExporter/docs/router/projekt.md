# Unterrouter Projekt

Zurueck: `router.md`. Stand: 2026-10-15.

Agentenkoordination, Projektstatus, Roadmap, Arbeitsregeln, Aenderungsstand.

## Vor jeder Arbeit

| Bedarf | Datei |
| --- | --- |
| **Wer arbeitet gerade woran, welche Dateien sind reserviert** | `docs/AGENT_COORDINATION.md` |
| **Arbeitsregeln, Tests, fachliche Grenzen** | `persona.md` |

Eintragen, bevor die Arbeit beginnt; beim Abschluss Status, geaenderte Dateien und Nachweis
nachtragen und die Reservierung freigeben. Eintraege mit `abgeschlossen`, `deployed`,
`frei` oder `Historie` sind keine laufende Arbeit.

## Status und Planung

| Bedarf | Datei |
| --- | --- |
| **Persoenliche Aufgabenliste, „woran arbeite ich gerade", Auftraggeber und Termine** | `projektmanagement/PROJEKTSTATUS.md` (IDs `PM-01` ff.) |
| **Status kurz fuer den Chef** (eine A4-Seite, je Bereich Bereit / Laeuft / Wartet / Offen, Zahlen aus der Detailliste; seit 2026-10-01). **Pflege:** aendert sich der Stand eines Punkts, der dort steht, im selben Zug die Zeile in `Status_kurz.tsv` nachfuehren; neue Themen nur aufnehmen, wenn sie fuer den Chef zaehlen, Kleinigkeiten bleiben in Wochen_Todo | `projektmanagement/Status_kurz.tsv` (Quelle), Ausgabe `projektmanagement/Status_kurz.xlsx`, erzeugt mit `projektmanagement/wochen_todo_xlsx.py` |
| **Vorhaben nach HERMES fuer die Geschaeftsleitung**: Stufe, Auftraggeber, Anwendervertreter, Freigabe/Abnahme/Abschluss, Ampel; Vorlagen Projektauftrag und Abnahmeprotokoll | `projektmanagement/Vorhaben_HERMES.tsv` (Quelle), Ausgabe `projektmanagement/Wochen_Todo.xlsx` Blatt „HERMES Übersicht“, erzeugt mit `projektmanagement/wochen_todo_xlsx.py` |
| **Offene Punkte im Finance Dashboard, Status je Punkt** | `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, dazu `docs/FINANCE_OFFENE_PUNKTE_2026-08-12.md` |
| 180-Tage-Roadmap Analytics, BI, HR, Einkauf | `docs/INGO_TODOS_180_TAGE_2026-06-18.md` |
| **Managementbericht fuer den Vorgesetzten: Rolle, Scope-Abgrenzung, Entscheidungsbedarf** | `docs/Uebersicht_Aufgaben_Ingo_2026-09-02.docx`, erzeugt von `.tmp_tools/BuildChefUebersicht0902` |
| Aktueller Aenderungsstand | `lastchange.md` |
| **Zusammenfassung 14./15.10.2026**: alles Erledigte mit Stand, Fragen an Fabio (Logistik J5, Verkauf J2/J3), offene Punkte Ingo | `docs/ARBEITSSTAND_2026-10-14_15.md` |
| Ansprechpartner und Standortempfaenger | `docs/ANSPRECHPARTNER.md` |
| **Themenlandkarte**: alle Themen auf einer Seite, Datenquellen, Abhaengigkeiten, Ansprechpartner, offene Blocker (SVG, Stand 2026-10-15; neu erzeugen mit `projektmanagement/themenlandkarte.py`) | `docs/THEMENLANDKARTE_2026-10-15.svg` |

**Abgrenzung:** `PROJEKTSTATUS.md` fuehrt Ingos Arbeitspakete. Finance-Detailfragen
beantwortet es **nicht** — dafuer gilt das Issue-Log.

## Fallen in diesem Ast

- **Statusfragen nie aus einer datierten Arbeitsnotiz beantworten.** Gueltig ist das
  Issue-Log oder eine eigene Live-Messung.
- Das frueher hier gefuehrte ChatGPT-Rohprotokoll `projektmanagement/kontext.txt` ist
  entfernt. Es hatte am 2026-08-14 zwei Punkte falsch als offen gefuehrt. Gueltig ist
  `projektmanagement/PROJEKTSTATUS.md`.
- Historie liegt in `docs/archiv/` (seit 2026-10-22; das aeltere `docs/raw_md_archive/` nur noch
  in Git, siehe `docs/archiv/README.md`) und ist **kein** Sollstand.

## Vollstaendigkeit

Der gesamte Dateibestand mit Einordnung steht in `baum.md`.
