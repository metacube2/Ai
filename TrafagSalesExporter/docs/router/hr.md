# Unterrouter HR

Zurueck: `router.md`. Stand: 2026-09-28.

HR-KPI-Cockpit, Fluktuation, Absenzen, Zeit und Ferien.

## Dateien

| Bedarf | Datei |
| --- | --- |
| Kurzstand, Zugang, Datenordner | `docs/rag/HR_KPI.md` |
| **Fachlogik, Datenquellen, Formeln, behobene Fehler, Grenzen** | `docs/HR_KPI.md` |
| Fachpruefung gegen Schweizer Praxis und HR-Best-Practices | `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` |
| Anwenderdoku fuer HR | `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx` |

## Fallen in diesem Ast

- **Arbeitstage sind nicht Montag bis Freitag.** `ZurichWorkdayCalendar` zieht die neun
  gesetzlichen Feiertage des Kantons Zuerich ab.
- **`Abwesenheitinstunden.xlsx` hat keine verlaesslichen Datumsfelder je Fall.** Die
  `(Zeitraum)`-Spalten nennen nur den juengsten Fall, die Stunden sind kumuliert. Bei einem
  Zeitraumfilter laesst sich der Zaehler nicht auf den Nenner eingrenzen; die Anzeige zeigt
  dann bewusst keine Prozentzahl und eine gelbe Ampel statt einer scheinbar genauen Quote.
- **Der Fluktuations-Nenner ist Headcount der Festangestellten, nicht FTE; die Krankenquote rechnet dagegen mit FTE.**
- **Kostenstelle, GLZ und Restferien filtern die Fluktuation NICHT**, weil diese Felder in
  der Austrittsdatei nicht stabil vorhanden sind. Das ist als Test gepinnt.
- **Seit 2026-09-28 nach Vorgabe HR:** langzeitkrank ab 61 Krankheitstagen je Person,
  Reminderprofile ohne FTE und Sollzeit ausgeschlossen, Restferien-Ampel nach Quartal,
  zusaetzlich gleitende Prognose und Vorjahr. `docs/HR_KPI.md` Abschnitt 8.
- *Ueberholt am 2026-09-28, bis auf die 8,4 h:* Mehrere Werte sind Annahmen ohne fachliche Bestaetigung (`8.4h = 1 Krankheitstag`,
  FTE-Fallback `0.5`, Ampelgrenzen). Siehe `docs/HR_KPI.md` Abschnitt 7.
