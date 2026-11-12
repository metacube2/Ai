# Unterrouter HR

Zurueck: `router.md`. Stand: 2026-11-12 (Doku-Pruefung: Rexx-Hub-Abruf seit dem Deploy vom 2026-11-12 11:33 auf dem Server, Schalter aus; Falle `Abwesenheitinstunden.xlsx` als ueberholt markiert); davor 2026-11-10 (Entscheid Rexx Hub, Lieferfelder Rexx, Hub-Produktbeschreibung gelesen); davor 2026-11-04 (Rexx-Abwesenheiten je Fall #777; Rexx-Gesamtabfrage #775 ersetzt die vier Rexx-Dateien; Formelpruefung mit drei Korrekturen, Reiter Interaktiv); davor 2026-11-03 (Rexx-Schnittstelle nach SAP, Idee Z-Tabelle/Tagesjob), 2026-10-29 (Kader seit 2026-10-22, Review 29.09. produktiv), 2026-09-30.

HR-KPI-Cockpit, Fluktuation, Absenzen, Zeit und Ferien.

## Dateien

| Bedarf | Datei |
| --- | --- |
| Konsistenzreview und technische Reparaturen 29.09. (*ueberholt „lokal, nicht deployed“: produktiv seit 2026-09-29 10:07, `eafd1c5`*); Zuordnung bestehender Fachfragen | `docs/HR_EINKAUF_REVIEW_2026-09-29.md` |
| **Wie Rexx-Daten nach SAP kommen (EXPRIS-Schnittstelle `Z_HR_PA_REXX`, Sonja importiert von Hand), Felder nur SAP/beide/nur Rexx, Idee eigenes Programm mit Z-Tabelle als Tagesjob, Mails Upgreat-Ticket** | `saptasks/rexx_schnittstelle/NOTES.md`, Kurzfassung `docs/HR_KPI.md` 8.9 |
| Kurzstand, Zugang, Datenordner | `docs/rag/HR_KPI.md` |
| **Fachlogik, Datenquellen, Formeln, behobene Fehler, Grenzen** | `docs/HR_KPI.md` |
| **Formelpruefung 04.11. (drei Korrekturen, offene Fachpunkte)** | `docs/HR_KPI.md` 8.10 |
| **Uebergang bis Rexx-Schnittstelle: Rexx ergaenzt SAP-Krankheit, Status, Sollzeit-Spalte, SAP-Ferienstand** | `docs/HR_KPI.md` 8.11 |
| **Was HR schriftlich vorgegeben hat (Mails 10/2025, 02/2026, 09/2026) und wie es umgesetzt ist: 61 Tage je Krankheit, Krankheit &amp; Unfall, Quote ohne Langzeit, Vorjahr gleicher Zeitraum** | `docs/HR_KPI.md` 8.12 |
| **Rexx-Gesamtabfrage #775 (`Hrcockpitgesamtdashboard.xlsx`) statt #757/#732/#744/#381; je Person der hoehere Wert aus SAP und Rexx; Spaltenwahl, 50-Spalten-Grenze, Pflege des Exports** | `docs/HR_KPI.md` 8.13 |
| **Rexx-Abwesenheiten je Fall #777 (Antragsliste, `Hr-cockpit-absenzen-je-fall.xlsx`): Faelle mit Datum ergaenzen SAP; Grenze direkt gebuchter Krankheit** | `docs/HR_KPI.md` 8.14 |
| **Entscheid 10.11.: Rexx Hub wird genommen; welche Felder Rexx liefern muss (Spaltennamen, Ampel fehlend/ungeprueft)** | `saptasks/rexx_schnittstelle/NOTES.md` 12, Kurzfassung `docs/HR_KPI.md` 8.17 |
| **Rexx Hub technisch: REST-API (kein Dateiexport), Endpunkte, Grenzen (1 Anfrage gleichzeitig, 50'000 pro Monat), Data-Warehouse-Abfrage als naechster Weg; Mailverlauf AG36071 (Hub nicht bestellt)** | `saptasks/rexx_schnittstelle/NOTES.md` 13 |
| **Rexx-Hub-Abruf gebaut und seit dem Deploy vom 2026-11-12 11:33 auf dem Server (`e956183`), Schalter `HrKpi:RexxHub:Enabled` weiter aus (bis 2026-11-12 stand hier nur „gebaut (Schalter aus)“): Umschaltanleitung, Einstellungen, Sicherungen, Testabruf, offene Annahmen** | `saptasks/rexx_schnittstelle/NOTES.md` 14, Kurzfassung `docs/HR_KPI.md` 8.18 |
| **Reiter Interaktiv (Zeitraffer, Galaxie, Bindungskurve, Krankheitskalender, Fit & Wohl, Ferien am Stueck, Simulator)** | `docs/HR_KPI.md` 9 |
| Fachpruefung gegen Schweizer Praxis und HR-Best-Practices | `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` |
| Anwenderdoku fuer HR | `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx` |

## Fallen in diesem Ast

- **Seit 2026-09-30 kommt die SAP-Datei automatisch** ueber `HrKpiSet` (seit 01.10. in P76 und produktiv, erster Abruf 02.10. 05:00,
  `docs/HR_KPI.md` 8.6). HR-Sitzung vom 30.09. (Kader 8,1 h *(ueberholt am 2026-10-22, siehe unten)*, Absenzen je Fall, Ampel): 8.5. Absenzen je Fall aus SAP `PA2001` (`HrAbsenzSet`, produktiv seit 01.10. 10:50, ersetzt Rexx fuer Krankheit und Ferien): 8.7.

- **Seit 2026-11-04 liest das Cockpit nur `Hrcockpitgesamtdashboard.xlsx` (Rexx #775), sobald sie in `hrdata` liegt;** die vier
  Einzeldateien liegen dort noch als Rueckfall, werden aber nicht gelesen. Wer Saldi oder Austritte aktualisieren will, exportiert
  #775 mit Zeitraum 01.01. bis heute. Gespeicherte Rexx-Abfragen behalten ihren Filter: #757 stand auf Stichtag 31.12.2024
  (`docs/HR_KPI.md` 8.13).
- **Arbeitstage sind nicht Montag bis Freitag.** `ZurichWorkdayCalendar` zieht die neun
  gesetzlichen Feiertage des Kantons Zuerich ab.
- *Ueberholt am 2026-10-01 bzw. 2026-11-04 (gilt nur noch ohne SAP-Datei oder fuer Zeitraeume vor dem Vorjahr; mit #775 im
  Ordner wird die Datei nicht mehr gelesen, `docs/HR_KPI.md` 4 und 8.13):* **`Abwesenheitinstunden.xlsx` hat keine verlaesslichen Datumsfelder je Fall.** Die
  `(Zeitraum)`-Spalten nennen nur den juengsten Fall, die Stunden sind kumuliert. Bei einem
  Zeitraumfilter laesst sich der Zaehler nicht auf den Nenner eingrenzen; die Anzeige zeigt
  dann bewusst keine Prozentzahl und eine gelbe Ampel statt einer scheinbar genauen Quote.
- **Der Fluktuations-Nenner ist Headcount der Festangestellten, nicht FTE; die Krankenquote rechnet dagegen mit FTE**,
  seit 2026-11-04 je Person erst ab Eintritt (`docs/HR_KPI.md` 8.10).
- **Kostenstelle, GLZ und Restferien filtern die Fluktuation NICHT**, weil diese Felder in
  der Austrittsdatei nicht stabil vorhanden sind. Das ist als Test gepinnt.
- **Seit 2026-11-04 (8.12): Kurz/Lang je Krankheit, ab dem 61. Tag lang; Quote = Krankheit und Unfall; zusaetzlich Quote ohne
  Langzeit.** Die fachlichen Antworten von HR stehen in den Mails (Nadja 10/2025, Sonja 02/2026 und 09/2026), nicht in Teams.
- *Ueberholt am 2026-11-04 (61-Tage-Regel):* **Seit 2026-09-28 nach Vorgabe HR:** langzeitkrank ab 61 Krankheitstagen je Person,
  Reminderprofile ohne FTE und Sollzeit ausgeschlossen, Restferien-Ampel nach Quartal,
  zusaetzlich gleitende Prognose und Vorjahr. `docs/HR_KPI.md` Abschnitt 8.
- **Seit 2026-09-30: ein Arbeitstag hat 8,0 h** (`HoursPerWorkday`), die frueheren 8,4 h sind ueberholt.
  *(Ueberholt am 2026-10-22: Kader (Rexx `Leitung j/n` = ja) 8,1 h ab `c51445c`.)* **Seit 2026-10-22: Kader =
  SAP-Mitarbeiterkreis 10 „ML mit Kadervertrag“ mit 8,2 h** (`HoursPerWorkdayFor`, `ede56b6`, produktiv seit 2026-10-22 15:48);
  Rexx `Leitung j/n` zaehlt nicht mehr. `docs/HR_KPI.md` 8.8.
- *Ueberholt am 2026-09-28, die 8,4 h am 2026-09-30:* Mehrere Werte sind Annahmen ohne fachliche Bestaetigung (`8.4h = 1 Krankheitstag`,
  FTE-Fallback `0.5`, Ampelgrenzen). Siehe `docs/HR_KPI.md` Abschnitt 7.
