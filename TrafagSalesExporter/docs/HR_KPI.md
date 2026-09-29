# HR-KPI-Cockpit: Fachlogik, Datenquellen und Grenzen

Stand: 2026-08-17, fachliche Antworten von HR eingearbeitet am 2026-09-28 (Abschnitt 8). Zusammengefuehrt aus `HR_KPI_NACHDOKU_2026-05-13.md`,
`HR_KPI_KORREKTUREN_2026-07-06.md` und `HR_KPI_FEIERTAGE_FILTERTEST_2026-08-06.md`.

Kurzstand und Zugangsdaten: `docs/rag/HR_KPI.md`.
Fachpruefung gegen Schweizer Praxis: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`.

Aktueller Produktivstand: Commit `5ae7f31`, deployed am 2026-09-28 um 11:25 (Abschnitt 8).
Historie: deployed und verifiziert am 2026-08-06 14:24 MESZ, Commit `9435a5d`,
Gesamtsuite `438/438` gruen.

## 1. Aufbau

### Reparaturstand 2026-09-29 — lokal, noch nicht deployed

Nach Konsistenzreview: Von/Bis hat fuer Berechnungsjahr und Vergleichswerte Vorrang;
Monat/Quartal/YTD zaehlen ihre volle Kalenderperiode, nicht nur Austritte ab dem freien
Von-Filter. Historischer HC beruecksichtigt auch vorher ausgeschiedene Personen.
Auswahlkennzahlen bleiben frei gefiltert. Die Maske fuer nicht periodisierbare
Krankenquoten gilt auch in Personentabellen; die allgemeinen Personenlisten sind nicht
mehr vor dem Pager auf 100/250 Zeilen gekappt. PDF bleibt ein als solcher bezeichneter
Druck der aktuellen Tabellenseite. Leeren des Jahresfilters bleibt erhalten.
Die Managementsicht verbirgt Namen, ist aber wegen Personalnummern keine anonyme Aggregation.

138/138 gezielte Tests bestanden. Vollstaendige Befunde, Reparaturumfang und Abgleich
bestehender Fachfragen: `docs/HR_EINKAUF_REVIEW_2026-09-29.md`.

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
| Prognose Jahr | aktuelle Quartals-Fluktuation **x 4** — bewusst nicht vom 01.01. hochgerechnet, von HR bestaetigt |
| Prognose gleitend (seit 2026-09-28) | relevante Austritte der letzten 12 Monate bis Stichtag / durchschnittlicher Headcount dieser 12 Monate |
| Vorjahr (seit 2026-09-28) | relevante Austritte des ganzen Vorjahres / durchschnittlicher Headcount des Vorjahres, mit Abstand der beiden Prognosen in Prozentpunkten |
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
| M8 | Historische Korrektur des Stichtags bei nur gesetztem `Von Austritt`. Die damalige Aussage „Austrittsjahr hat Vorrang“ widerspricht dem Filtervertrag in Abschnitt 6; Vorrang Von/Bis lokal am 29.09.2026 einheitlich repariert, siehe Reviewbericht |
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

*Ueberholt am 2026-09-28: HR hat diese Punkte bis auf die 8,4-Stunden-Umrechnung beantwortet,
siehe Abschnitt 8. Die Liste bleibt als Stand vom 2026-08-17 stehen.*

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

## 8. Antworten von HR und Umsetzung, 2026-09-28

Sonja Richter (HR) hat Ingos Fragenmail vom 2026-08-19 waehrend seiner Ferien beantwortet.
Umgesetzt in `5ae7f31`, **produktiv seit 2026-09-28 11:25**.
Wo die Antwort mehr als eine Umsetzung zuliess, hat Ingo am 2026-09-28 entschieden.

| # | Frage | Antwort HR | Umsetzung |
| --- | --- | --- | --- |
| 2 | Kurz- gegen Langzeitkrankheit | Die Rexx-Arten nicht unterscheiden, „Krankheit" ziehen; ab dem **61. Krankheitstag** gilt die Krankheit als Langzeitkrankheit | **Geaendert.** Beide Rexx-Felder zaehlen als Krankheit (Entscheid Ingo). Kurz/lang je Person nach `HrKpiDashboardBuilder.ClassifySickness`: Summe der Krankheitstage im Export (Stunden / 8,4) ab 61 = langzeitkrank, dann zaehlt die ganze Krankheit als lang. Rexx liefert keine einzelnen Faelle, deshalb die Summe (Entscheid Ingo). Kachel „Krankheit Lang" nennt die Anzahl Langzeitkranke |
| 3 | FTE-Fallback 0,5 | FTE ist bei echten Mitarbeitenden nie leer und entspricht der Sollzeit (0,8 = 32 h/Woche); leer nur bei Reminderprofilen wie ICT | **Geaendert.** Ohne SAP-Beschaeftigungsgrad **und** ohne Rexx-Sollzeit gilt eine Zeile als Reminderprofil und faellt aus allen Kennzahlen (Entscheid Ingo, `IstReminderprofil`). Nur wenn die Zeitdatei die Person gefunden hat: ein fehlgeschlagener Namens-Join schliesst niemanden aus. Fehlt nur SAP, gilt FTE = Sollzeit / 8,4 h; das FTE ist auf 0,1 bis 1,2 begrenzt. Die bekannten Profile „ICT Trafag" und „Empfaenger Reminder" standen schon in der Ausschlussliste |
| 4 | GLZ-Ampel 50/100 h | Korrekt, positiv und negativ gleich, auch bei Teilzeit | unveraendert |
| 5 | Restferien-Ampel | Q1: bis 5 Tage gruen, mehr rot. Ab Q2: jeder Resttag rot | **Geaendert**, `ResolveRestferienAmpel`, massgeblich ist das Quartal des heutigen Tags (die Saldidatei ist ein Stichtagsstand) |
| 6 | Prognose Quartalsrate x 4 | Beibehalten, zusaetzlich gleitender Durchschnitt und Vorjahresvergleich | **Ergaenzt**: Kacheln „Fluktuation Prognose gleitend" und „Fluktuation Vorjahr", beide aus der nur strukturgefilterten Austrittsliste wie der Vorjahresvergleich (H1) |
| 7 | Definition fluktuationsrelevant | Stimmt | unveraendert |
| 8 | Kuendigungserkennung | Nur „Kuendigung AN" fliesst in die Fluktuation | unveraendert, die Erkennung deckt `Kündigung AN` und `Kuendigung AN` ab |
| 9 | Ausschluss Praktikanten, Werkstudenten, Aushilfen, Lehrlinge | Korrekt, keine Ausnahmen | unveraendert |

Offen bleibt die **8,4-Stunden-Umrechnung** je Krankheitstag; danach wurde nicht gefragt.

### 8.1 Periodengenaue Krankenquote: die Datumsfelder reichen nicht

HR schreibt, die Datumsfelder stuenden in der Excel-Datei und der Zeitraum lasse sich unter
Abwesenheiten eingrenzen. Nachgeprueft am 2026-09-28 an der produktiven
`hrdata/Abwesenheitinstunden.xlsx` (Stand 08.07.2026, nur Spaltenkoepfe und Formate gelesen,
keine Personendaten):

- Die Datei hat **eine Zeile je Person** (270 Zeilen) mit **kumulierten** Stunden des
  Exportzeitraums.
- Die Spalten `Krankheit angetreten (Zeitraum)` und `Krank nicht buchbar angetreten (Zeitraum)`
  enthalten ein Datum oder eine Spanne `TT.MM.JJJJ - TT.MM.JJJJ`. Das ist der Zeitraum des
  **juengsten** Falls, nicht aller Faelle (Befund H2 vom 2026-07-06). Unser Leser sucht nach
  `Von Datum`/`Bis Datum` und liest diese Spalten nicht; mit ihnen liesse sich die Summe aber
  ohnehin nicht auf einen anderen Zeitraum aufteilen.

Damit ist die Krankenquote nur dann periodengenau, wenn **der Export in Rexx schon auf den
gewuenschten Zeitraum eingegrenzt** wird, was HR mit „unter Abwesenheiten eingegrenzt"
vermutlich meint. Die Anzeige bleibt bei einem Zeitraumfilter deshalb beim Hinweis
„Zeitraum nicht bestimmbar". HR hat angeboten, das im naechsten Call gemeinsam anzusehen.

### 8.2 Nebenbefund: die HR-Dateien auf dem Server sind alt

Stand der Dateien in `hrdata` am 2026-09-28: die Rexx-Exporte vom **08.07.2026**, die SAP-Datei
`HR_KPI_EXPORT.xlsx` vom **26.05.2026**. Das Cockpit zeigt also den Stand von Juli. Neue
Mitarbeitende seit Mai haben keinen SAP-Beschaeftigungsgrad und laufen ueber die Sollzeit.

## Querverweise

- Kurzstand, Zugang und Anwenderdoku: `docs/rag/HR_KPI.md`
- Fachpruefung Schweizer Praxis: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`
- Anwenderdoku HR: `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
