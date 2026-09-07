# HR-KPI-Cockpit: Fachlogik, Datenquellen und Grenzen

Stand: 2026-08-17. Zusammengefuehrt aus `HR_KPI_NACHDOKU_2026-05-13.md`,
`HR_KPI_KORREKTUREN_2026-07-06.md` und `HR_KPI_FEIERTAGE_FILTERTEST_2026-08-06.md`.

Kurzstand und Zugangsdaten: `docs/rag/HR_KPI.md`.
Fachpruefung gegen Schweizer Praxis: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`.

Produktiv deployed und verifiziert am 2026-08-06 14:24 MESZ, Commit `9435a5d`,
Gesamtsuite `438/438` gruen.

## 1. Aufbau

Das Cockpit ist ein fachlich entkoppelter Reiter unter `/hr-kpi`, getrennt vom
Finance-/Management-Cockpit und nur ueber gemeinsame technische Infrastruktur verbunden.
Die PowerBI-M-/DAX-Logik wurde **nicht** als generischer Interpreter uebernommen, sondern
als fachliche Vorlage in nachvollziehbare C#-Logik uebertragen.

| Baustein | Ort |
| --- | --- |
| Seite | `Components/Pages/HrKpi.razor` |
| Reiter | `Components/HrKpi/HrKpiDashboardTabs.razor` |
| Modelle | `Models/HrKpiModels.cs` |
| Service | `Services/HrKpiService.cs` |
| Aufbau | `Services/HrKpi/HrKpiDashboardBuilder.cs` |
| Arbeitstage | `Services/HrKpi/ZurichWorkdayCalendar.cs` |
| Tests | `TrafagSalesExporter.Tests/HrKpiServiceTests.cs` |

Reiter: `Ueberblick`, `Fluktuation`, `Absenzen`, `Zeit / Ferien`, `Mitarbeitende`,
`Datenstatus`.

Filter: Datenordner, Austrittsjahr, Von/Bis Austritt, Organisation, Eintrittsjahr,
Suche Name/Personalnummer, Kostenstelle, Mitarbeitertyp, Fluktuation, GLZ-Ampel,
Restferien-Ampel.

## 2. Datenquellen

Konfiguration in `appsettings.json` unter `HrKpi`, Standardordner `C:\temp`:

| Datei | Inhalt |
| --- | --- |
| `Saldiperstichdatum.xlsx` | aktive Mitarbeitende, Saldi, Ferien, Organisation, Kostenstelle |
| `Exportkommengehen.xlsx` | Arbeitszeitmodell, Sollzeit, Geburtsdatum |
| `HR_KPI_Export.xlsx` | SAP-HR-Felder: Beschaeftigungsgrad, Geschlecht, BU/NBU, Planstelle |
| `Abwesenheitinstunden.xlsx` | Krankheit kurz und lang in Stunden |
| `Personalausgeschieden.xlsx` | Austritte, Austrittsart, Austrittsdatum |

## 3. Fluktuationslogik

Grundlage `formeln.docx`. **Nenner ist immer Headcount der Festangestellten, nicht FTE.**

| Kennzahl | Formel |
| --- | --- |
| Monat | Arbeitnehmerkuendigungen des Monats / Headcount des Monats |
| Quartal | Arbeitnehmerkuendigungen des Quartals / durchschnittlicher Headcount des Quartals |
| Prognose Jahr | aktuelle Quartals-Fluktuation **x 4** — bewusst nicht vom 01.01. hochgerechnet |
| YTD | fluktuationsrelevante Kuendigungen seit 01.01. bis Stichtag / durchschnittlicher Headcount im bisherigen Jahr |

Ein Austritt ist **fluktuationsrelevant**, wenn die Austrittsart als
Arbeitnehmerkuendigung erkannt wird, der Mitarbeitertyp nicht ausgeschlossen ist und der
Grund nicht als befristet, Pensionierung oder Arbeitgeberkuendigung gilt.

Ausgeschlossen: Praktikant, Werkstudent, Aushilfe, Lehrling, befristeter Vertrag,
Pensionierung/Rente, Kuendigung durch den Arbeitgeber.

**Schreibweisen-Falle:** Rexx liefert `Kündigung AN` mit Umlaut; die Erkennung akzeptiert
auch `Kuendigung AN`. `Kuendigung AG` bleibt als Arbeitgeberkuendigung ausgeschlossen,
`Ruhestand` als Pensionierung.

Kontrollwerte 2025: Austritte total `104`, `Kündigung AN` `42`, davon relevant `33`,
durchschnittlicher Headcount rund `211.3`, Fluktuation rund `15.6 %`.

## 4. Krankenquote und Arbeitstage

Die Arbeitstage im Nenner sind **nicht** einfach Montag bis Freitag. `ZurichWorkdayCalendar`
zieht die neun gesetzlichen, den Sonntagen gleichgestellten Feiertage des Kantons Zuerich
ab: Neujahr, Karfreitag, Ostermontag, 1. Mai, Auffahrt, Pfingstmontag, 1. August,
Weihnachten, Stephanstag.

Bewegliche Feiertage werden je Jahr aus dem gregorianischen Ostersonntag berechnet. Ein
Feiertag reduziert den Nenner nur, wenn er auf einen Wochentag faellt. Berchtoldstag und
lokale Feiertage sind **nicht** enthalten, weil sie im Kanton Zuerich nicht allgemein
gesetzlich sind.

### Wenn der Zeitraum nicht bestimmbar ist

`Abwesenheitinstunden.xlsx` hat im produktiven Format **keine verlaesslichen Datumsfelder**
fuer die kumulierten Krankheitsstunden. Bei einem Jahres- oder Datumsfilter laesst sich der
Zaehler deshalb nicht sicher auf denselben Zeitraum wie der Nenner eingrenzen.

Konsequenz in der Anzeige: keine scheinbar genaue Prozentzahl, Ampel **gelb** statt einer
aus unzuverlaessiger Quote abgeleiteten Bewertung, Krankheitstage bleiben sichtbar mit
Warnstatus.

Ampelgrenzen konfigurierbar ueber `HrKpi:AbsenceYellowThresholdPercent` und
`AbsenceRedThresholdPercent`. Default bis zur fachlichen Bestaetigung: gruen unter 3.0 %,
gelb unter 5.0 %, rot ab 5.0 %. Die verwendeten Grenzen stehen in der Kachelbeschreibung,
damit HR sie sichtbar pruefen kann.

## 5. Behobene Berechnungsfehler (Review 2026-07-06, alle umgesetzt)

| ID | Fehler und Behebung |
| --- | --- |
| H1 | **Vorjahresvergleich zeigte immer 0.** `BuildPeriodComparisonMetrics` bekam die bereits auf das Austrittsjahr gefilterte Liste, `Delta Fluktuation` entsprach dadurch der aktuellen Rate. Jetzt eigene, nur struktur- statt datumsgefilterte Liste; Austritte zaehlen distinct nach Personalnummer |
| H2 | **Krankenquote-Nenner beim laufenden Jahr** zaehlte die Arbeitstage des ganzen Jahres, die Quote war dadurch rund Faktor 2 zu niedrig. `ResolveAnalysisPeriod` kappt das Periodenende auf heute; ohne Zeitraumfilter wird die Periode aus den Absenzdaten abgeleitet statt pauschal 21 Tage |
| M3 | Top-Absenzen rankten Zeilen statt Personen. `AggregateAbsencesByPerson` aggregiert vor der Anzeige |
| M4 | Vergleichszaehlung nutzte Zeilen statt distinct Personen — mit H1 behoben |
| M5 | Zwei Fluktuationskacheln mit verschiedenen Nennern fuer dasselbe Jahr. `ResolveTurnoverDenominator` mittelt beim laufenden Jahr nur bis zum Stichtagsmonat |
| M6 | Doppelte SAP-Personalnummern wurden still verworfen; jetzt Datenqualitaetshinweis. Bei Duplikaten gewinnt die erste Zeile, BU/NBU der uebrigen gehen verloren |
| M7 | Trefferquote des fehleranfaelligen Namens-Joins zur Zeitdatei wird ausgewiesen |
| M8 | Bei nur gesetztem `Von Austritt` ist der Stichtag jetzt heute statt des Zeitraumanfangs. Austrittsjahr hat Vorrang vor `Von` |
| L9 | `Ferien bezogen` wird aus den Summen gerechnet, nicht aus den pro Person auf 0 gekappten Einzelwerten |

## 6. Filtervertrag als Regressionstest

`BuildAsync_All_128_Global_Filter_Combinations_Keep_Every_Visible_Block_Consistent` prueft
alle 128 Ein-/Aus-Kombinationen der sieben personenbezogenen Filter (Organisation,
Kostenstelle, Mitarbeitertyp, Eintrittsjahr, GLZ-Ampel, Restferien-Ampel, Suche) ueber
alle sichtbaren Ergebnisbloecke.

Ein zweiter Test kombiniert zusaetzlich Austrittsjahr, Von/Bis und Fluktuationsfilter. Er
pinnt die fachliche Abgrenzung: **Kostenstelle, GLZ und Restferien filtern die
Mitarbeitenden- und Absenzsicht, aber nicht die Fluktuation**, weil diese Felder in der
Austrittsdatei nicht stabil vorhanden sind. Von/Bis hat Vorrang vor dem Austrittsjahr.

**Abnahmegrenze:** Die Tests beweisen technische Filterkonsistenz innerhalb der vorhandenen
Quelldaten. Sie ersetzen keine fehlenden Quellfelder. Eine periodengenaue Krankenquote
bleibt erst moeglich, wenn Rexx die Krankheitsstunden mit belastbarem Bezugszeitraum oder
als datierte Einzelereignisse liefert.

## 7. Bewusst nicht geaendert, fachliche Bestaetigung offen

Diese Werte sind keine Codefehler, sondern Annahmen, die HR bestaetigen muss:

- `8.4 Stunden = 1 Krankheitstag` als Standardumrechnung
- Definition kurz gegen lang bei Absenzen
- FTE-Fallback `0.5` bei fehlendem SAP-Beschaeftigungsgrad
- GLZ- und Restferien-Schwellen gegen die internen HR-Grenzwerte
- Prognose als Quartalsrate mal vier
- Ob die Abgrenzung „fluktuationsrelevant" exakt der Trafag-HR-Definition entspricht
- Ob Arbeitnehmerkuendigungen anhand der vorhandenen Austrittsart-Texte vollstaendig
  erkannt werden
- Ob Praktikanten, Werkstudenten, Aushilfen und Lehrlinge immer auszuschliessen sind

Sonja muss die Absenzen weiterhin gegen Rexx abgleichen.

## Querverweise

- Kurzstand, Zugang und Anwenderdoku: `docs/rag/HR_KPI.md`
- Fachpruefung Schweizer Praxis: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`
- Anwenderdoku HR: `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
