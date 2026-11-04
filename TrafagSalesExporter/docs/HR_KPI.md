# HR-KPI-Cockpit: Fachlogik, Datenquellen und Grenzen

Stand: 2026-11-04 (Rexx-Abwesenheiten je Fall #777, Abschnitt 8.14; Rexx-Gesamtabfrage #775 als Hauptquelle, Abschnitt 8.13; Pruefung der Formeln mit drei Korrekturen, Abschnitt 8.10; Reiter Interaktiv, Abschnitt 9); davor 2026-11-03 (Rexx-Schnittstelle nach SAP, Abschnitt 8.9), 2026-10-29 (Konsistenzpruefung), urspruenglich 2026-08-17, fachliche Antworten von HR eingearbeitet am 2026-09-28 (Abschnitt 8). Zusammengefuehrt aus `HR_KPI_NACHDOKU_2026-05-13.md`,
`HR_KPI_KORREKTUREN_2026-07-06.md` und `HR_KPI_FEIERTAGE_FILTERTEST_2026-08-06.md`.

Kurzstand und Zugangsdaten: `docs/rag/HR_KPI.md`.
Fachpruefung gegen Schweizer Praxis: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`.

Aktueller Produktivstand des HR-Cockpits: Commit `6dd1a56`, deployed am 2026-11-04 um 11:46 (Rexx-Gesamtabfrage #775, 8.13;
davor `69a63b6` um 10:21, `cfe2631` um 09:57 und `b65f059` um 09:20, Abschnitte 8.10 bis 8.12 und 9);
Rexx-Daten seit 2026-11-04 11:46 aus `Hrcockpitgesamtdashboard.xlsx` vom selben Tag (8.13); die Einzeldateien vom 09:30 liegen noch als Rueckfall in `hrdata`.
*(ueberholt am 2026-11-04 11:46: hier stand `69a63b6` vom 10:21)*
*(ueberholt am 2026-11-04: hier stand `ede56b6` vom 2026-10-22 15:48, Abschnitt 8.8)*
*(ueberholt am 2026-10-29: hier stand `5ae7f31` vom 2026-09-28 11:25, siehe `docs/rag/DEPLOYMENT.md`)*
Historie: deployed und verifiziert am 2026-08-06 14:24 MESZ, Commit `9435a5d`,
Gesamtsuite `438/438` gruen.

## 1. Aufbau

### Reparaturstand 2026-09-29 — lokal, noch nicht deployed

*Ueberholt am 2026-10-29: produktiv seit 2026-09-29 10:07 (`eafd1c5`), siehe `docs/rag/HR_KPI.md`.*

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
| Tests | `TrafagSalesExporter.Tests/HrKpiServiceTests.cs`, `HrKpiInteractiveTests.cs` |
| Reiter Interaktiv (seit 2026-11-04, Abschnitt 9) | `Services/HrKpi/HrKpiInteractive.cs`, `Components/HrKpi/Interactive/` |

Reiter: `Ueberblick`, `Fluktuation`, `Ampel`, `Absenzen`, `Zeit / Ferien`, `Mitarbeitende`,
`Interaktiv` (seit 2026-11-04), `Datenstatus`, `Anleitung`.

Filter: Datenordner, Austrittsjahr, Von/Bis Austritt, Organisation, Eintrittsjahr,
Suche Name/Personalnummer, Kostenstelle, Mitarbeitertyp, Fluktuation, GLZ-Ampel,
Restferien-Ampel.

## 2. Datenquellen

Konfiguration in `appsettings.json` unter `HrKpi`, Standardordner `C:\temp`:

**Seit 2026-11-04 (Abschnitt 8.13):** Liegt `Hrcockpitgesamtdashboard.xlsx` (Rexx-Abfrage #775) im Ordner, ersetzt sie die
vier Rexx-Einzeldateien unten (#757, #732, #744, #381); diese werden dann nicht mehr gelesen. Ohne #775 gilt die Tabelle
unveraendert. Die SAP-Dateien bleiben in beiden Faellen.

| Datei | Inhalt |
| --- | --- |
| `Hr-cockpit-absenzen-je-fall.xlsx` (seit 2026-11-04) | Rexx #777 (Antragsliste), eine Zeile je Abwesenheit mit Von/Bis, Art, Status, Sollzeit; ergaenzt SAP-Faelle (8.14) |
| `Hrcockpitgesamtdashboard.xlsx` (seit 2026-11-04) | Rexx #775, eine Zeile je Person: Stamm, Status, Austritt, Saldo, Ferien, Sollzeit, Geburtsdatum, Summen Krankheit/Unfall/Ferien/Kompensation/Militaer 01.01. bis Exporttag |
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
| Prognose Jahr | aktuelle Quartals-Fluktuation **x 4** — bewusst nicht vom 01.01. hochgerechnet, von HR bestaetigt. *Seit 2026-11-04 (8.10):* ein laufendes Quartal wird vorher auf seine volle Laenge hochgerechnet |
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
durchschnittlicher Headcount rund `211.3`, Fluktuation rund `15.6 %`. *(Am 2026-11-04 nicht mehr nachvollziehbar: der
Rexx-Export vom 04.11. hat fuer 2025 nur 49 Austritte und 6 „Kündigung AN“; Frage an HR offen, Abschnitt 8.12.)*

## 4. Krankenquote und Arbeitstage

Die Arbeitstage im Nenner sind **nicht** einfach Montag bis Freitag. `ZurichWorkdayCalendar`
zieht die neun gesetzlichen, den Sonntagen gleichgestellten Feiertage des Kantons Zuerich
ab: Neujahr, Karfreitag, Ostermontag, 1. Mai, Auffahrt, Pfingstmontag, 1. August,
Weihnachten, Stephanstag.

Krankenquote = Krankheitstage / Summe der FTE-Arbeitstage. *Seit 2026-11-04 (8.10):* je Person zaehlen die
Arbeitstage erst ab dem Eintritt, und die Quote je Person teilt durch die eigenen FTE-Arbeitstage (mit Pensum).

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

*Ueberholt am 2026-10-29: Seit 2026-10-01 kommt die Krankheit je Fall aus SAP `PA2001`
(`hrdata/HR_Absenzen_SAP.xlsx`) und ist periodengenau; der Hinweis „Zeitraum nicht bestimmbar"
gilt nur noch ohne SAP-Datei oder fuer einen Zeitraum vor dem Vorjahr, siehe Abschnitt 8.7.*

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
als datierte Einzelereignisse liefert. *(ueberholt am 2026-10-01: periodengenau ueber SAP `PA2001`, Abschnitt 8.7)*

## 7. Bewusst nicht geaendert, fachliche Bestaetigung offen

*Ueberholt am 2026-09-28: HR hat diese Punkte bis auf die 8,4-Stunden-Umrechnung beantwortet,
siehe Abschnitt 8. Seit 2026-09-30 ist auch diese entschieden: 8,0 h je Tag (Abschnitt 8.4). Die
Liste bleibt als Stand vom 2026-08-17 stehen.*

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
| 2 | Kurz- gegen Langzeitkrankheit *(Umsetzung ueberholt am 2026-11-04: je Krankheit ab dem 61. Tag, 8.12)* | Die Rexx-Arten nicht unterscheiden, „Krankheit" ziehen; ab dem **61. Krankheitstag** gilt die Krankheit als Langzeitkrankheit | **Geaendert.** Beide Rexx-Felder zaehlen als Krankheit (Entscheid Ingo). Kurz/lang je Person nach `HrKpiDashboardBuilder.ClassifySickness`: Summe der Krankheitstage im Export (Stunden / 8,4) ab 61 = langzeitkrank, dann zaehlt die ganze Krankheit als lang. Rexx liefert keine einzelnen Faelle, deshalb die Summe (Entscheid Ingo). Kachel „Krankheit Lang" nennt die Anzahl Langzeitkranke |
| 3 | FTE-Fallback 0,5 | FTE ist bei echten Mitarbeitenden nie leer und entspricht der Sollzeit (0,8 = 32 h/Woche); leer nur bei Reminderprofilen wie ICT | **Geaendert.** Ohne SAP-Beschaeftigungsgrad **und** ohne Rexx-Sollzeit gilt eine Zeile als Reminderprofil und faellt aus allen Kennzahlen (Entscheid Ingo, `IstReminderprofil`). Nur wenn die Zeitdatei die Person gefunden hat: ein fehlgeschlagener Namens-Join schliesst niemanden aus. Fehlt nur SAP, gilt FTE = Sollzeit / 8,4 h; das FTE ist auf 0,1 bis 1,2 begrenzt. Die bekannten Profile „ICT Trafag" und „Empfaenger Reminder" standen schon in der Ausschlussliste |
| 4 | GLZ-Ampel 50/100 h | Korrekt, positiv und negativ gleich, auch bei Teilzeit | unveraendert |
| 5 | Restferien-Ampel | Q1: bis 5 Tage gruen, mehr rot. Ab Q2: jeder Resttag rot | **Geaendert**, `ResolveRestferienAmpel`, massgeblich ist das Quartal des heutigen Tags (die Saldidatei ist ein Stichtagsstand) |
| 6 | Prognose Quartalsrate x 4 | Beibehalten, zusaetzlich gleitender Durchschnitt und Vorjahresvergleich | **Ergaenzt**: Kacheln „Fluktuation Prognose gleitend" und „Fluktuation Vorjahr", beide aus der nur strukturgefilterten Austrittsliste wie der Vorjahresvergleich (H1) |
| 7 | Definition fluktuationsrelevant | Stimmt | unveraendert |
| 8 | Kuendigungserkennung | Nur „Kuendigung AN" fliesst in die Fluktuation | unveraendert, die Erkennung deckt `Kündigung AN` und `Kuendigung AN` ab |
| 9 | Ausschluss Praktikanten, Werkstudenten, Aushilfen, Lehrlinge | Korrekt, keine Ausnahmen | unveraendert |

*Ueberholt am 2026-09-30:* ~~Offen bleibt die **8,4-Stunden-Umrechnung** je Krankheitstag; danach wurde nicht gefragt.~~
Entschieden, 8,0 h je Tag, siehe Abschnitt 8.4. Die Stunden in der Tabelle oben (Stunden / 8,4)
gelten seither mit 8,0, fuer Kader (SAP-Mitarbeiterkreis 10) seit 2026-10-22 mit 8,2 (Abschnitt 8.8).

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
*(ueberholt am 2026-10-01: mit der SAP-Datei aus `PA2001` ist die Krankenquote periodengenau, Abschnitt 8.7)*

### 8.2 Nebenbefund: die HR-Dateien auf dem Server sind alt

Stand der Dateien in `hrdata` am 2026-09-28: die Rexx-Exporte vom **08.07.2026**, die SAP-Datei
`HR_KPI_EXPORT.xlsx` vom **26.05.2026**. Das Cockpit zeigt also den Stand von Juli. Neue
Mitarbeitende seit Mai haben keinen SAP-Beschaeftigungsgrad und laufen ueber die Sollzeit.
*(SAP-Teil ueberholt am 2026-10-01: `HR_KPI_Export.xlsx` wird taeglich ueber `HrKpiSet` geschrieben, Abschnitt 8.6; die Rexx-Dateien sind weiter vom 08.07.)*
*Erledigt 2026-11-04 09:30 (Auftrag Ingo „neueste Files nutzen, nahtlos mit alten verbinden“):* die vier Rexx-Dateien
in `hrdata` durch Ingos Exporte vom 04.11. ersetzt, die alten vom 08.07. liegen in `hrdata\archiv_rexx_2026-07-08`.
Vorher verglichen (nur Kopfzeilen und Zaehlwerte): Spaltenkoepfe identisch; Saldi, Kommen/Gehen und Absenzen 265 statt 270
Zeilen (Stichtagsstand); Austritte 128 statt 113, gleicher Beginn 30.11.2023, neu bis 09.10.2026. Die einzige nur im alten
Bestand stehende Austrittszeile (24.04.2026, ohne Austrittsart) gehoert zu einer Person, die laut neuem Saldi-Export aktiv
ist; der neue Export ersetzt den alten deshalb ohne Zusammenfuehren. Absenzfaelle in #744 bis 06.11.2026 (geplantes Ende
einer laufenden Krankmeldung; gezaehlt wird nur bis heute).
Ab naechster Woche soll die Rexx-Schnittstelle die Dateien liefern (Sitzung Rexx Hub, 8.9).

### 8.3 Offene Wuensche und Faeden aus der Mailhistorie (gesichtet 2026-09-29)

Beim Durchsehen der Mails und Teams-Chats mit Sonja Richter und Nadja Brandenberger gefunden. Nichts
davon war bisher hier dokumentiert.

**Gewuenscht (Sonja, Mail vom 2026-02-03, Bereich Zeit- und Absenzwesen), weder umgesetzt noch
beauftragt:** *(teilweise ueberholt am 2026-11-04: Ferien am Stueck und die Fit-&-Wohl-Ausloeser zeigt der Reiter
Interaktiv, Abschnitt 9; der Ruecklauf „Gespraech erfolgt“ und der Uebertrag Restferien sind weiter offen)*

| Wunsch | Inhalt |
| --- | --- |
| Ferien am Stueck | Wurden im Kalenderjahr mindestens zwei Wochen am Stueck bezogen? Jaehrlich, je Abteilung/KST |
| Fit-&-Wohl-Gespraeche | Ausloeser nach der 6. Absenz oder nach 12 Krankheitstagen (Arbeitstage); monatlich und je Quartal, je Abteilung/KST; Wunsch, den Ruecklauf zu verfolgen (Gespraech erfolgt ja/nein) |
| Uebertrag Restferien | Auswertung des Uebertrags ins neue Jahr, hoechstens 5 Tage |

Aus der KPI-Liste vom Oktober 2025 sind zudem nicht angegangen: Ueberstunden, Lohn-/Personalkosten,
Stellenplan Soll/Ist (Rexx), Pulsumfrage (quartalsweise, Rexx), Zufriedenheitsumfrage (jaehrlich),
Kununu-Score (monatlich, manuelle Excel), Time to hire (Refline, kuenftig Rexx) und Produktivstunden.
Die Absenz-Ampel wuenscht Sonja mit gruen bis 3 %, gelb bis 4 %, rot ab 5 %; der Code hat gruen unter
3 % und rot ab 5 %, gelb also 3 bis 5 %.

**Offene Faeden:**

- **Seit 2026-09-30: kein Rexx-Hub, Export ueber Upgreat (PM-07).** Ingo: „Hub wollen wir nicht“. Stattdessen
  hat Ingo am 2026-09-30 Ueli Kunzmann (Upgreat) per Mail gebeten, die vier Rexx-Berichte
  (`Saldiperstichdatum`, `Exportkommengehen`, `Abwesenheitinstunden` ab 01.01. des laufenden Jahres,
  `Personalausgeschieden`, je `.xlsx` mit unveraenderten Namen und Spalten) einmal taeglich in den
  HR-Datenordner auf dem BiDashboard-Server zu liefern, die alte Datei zu ueberschreiben und zu
  archivieren. Gefragt: ob Rexx die Berichte per SFTP bereitstellen kann, und ob Ausfaelle ueberwacht
  werden. Pfad und Berechtigung klaert Ingo mit Ueli. **Offen:** Antwort Ueli *(ueberholt am 2026-11-03: Upgreat-Ticket #12699584, am 19.10. geschlossen, Inhalt ungelesen; Abschnitt 8.9)*; ob der von Nadja
  erteilte Hub-Auftrag bei Rexx zurueckgenommen werden muss. Die SAP-Datei `HR_KPI_Export` ist
  nicht Teil davon.
- **Gegenrichtung schon automatisch: dormakaba nach Rexx (seit 2026-09-02).** Ueli Kunzmann hat auf
  `tragvapp404` einen geplanten Task eingerichtet: Start 00:14:50, danach alle 15 Minuten; neueste
  `.booking` aus `D:KabaB-COMM JavaDefaultBooking` wird als `bclan01_yyyy.MM.dd_HH.mm.ss.booking`
  nach `D:Transfer_Booking_to_REXXTransfer` verschoben, per SFTP nach Rexx `/import/dormakaba`
  geladen und in `D:Transfer_Booking_to_REXXArchiv` abgelegt (Teams, Alex Donato, 2026-09-02). Die
  Stempeldaten in Rexx sind damit aktuell, das Cockpit nicht.
- *Ueberholt am 2026-09-30 (Hub nicht gewollt):* **Rexx-Hub fuer den automatischen Export (PM-07):** Rexx (Artjom Yahno) hat am 2026-07-28 den
  Rexx-Hub fuer 4'800 EUR pro Jahr empfohlen (vier Einzelschnittstellen waeren 5'600 EUR). **Nadja hat
  am 2026-07-29 den Auftrag zur Umsetzung erteilt.** Stand bei Rexx nachfragen.
- **Q1/2026-Abgleich mit SAP:** Nadja bat am 2026-05-05, Q1/2026 mit Live-Daten zu testen, um die Quoten
  mit SAP zu vergleichen; Filter am 2026-05-13 per Teams (Fluktuation alle Abteilungen und
  Festangestellten, Q1 und Gesamtjahr 2026; Absenzquote Q1). Ob der Abgleich stattfand, ist nicht
  belegt.
- **Testpersonen ausschliessen** (Nadja, 2026-05-13): Angelina Jolie, Brad Pitt, Peter Muster, ICT
  Trafag, Empfaenger Reminder. Der Code kennt eine Ausschlussliste; die letzten beiden sind
  Reminderprofile.
- **Abrechnungskreise** (Sonja, 2026-03-23): 01 lohnrelevant, 99 nicht lohnrelevant, z. B.
  Temporaerbuero.
- **Mail an Sonja zum Umsetzungsstand:** liegt seit 2026-09-29 als Entwurf in Outlook, noch nicht
  versendet; im Entwurf fehlt Punkt 2 (aktuelle Exporte, Dateien vom 08.07. und 26.05.).
  *Nachtrag 2026-09-30:* ueberarbeitete Fassung als `Downloads\Mail_Sonja_HR_Cockpit_2026-09-30.eml`
  (Punkt 2 ergaenzt, Punkt 1 nimmt Sonjas Angebot auf, 8,4-h-Frage entfaellt nach Abschnitt 8.4).
  Versand durch Ingo, alter Entwurf ist zu loeschen.

### 8.4 Arbeitstag 8,0 h statt 8,4 h, 2026-09-30

Ingo hat am 2026-09-30 bestaetigt: Trafag rechnet mit **8,0-Stunden-Tagen**. Das deckt sich mit
Sonjas eigenen Angaben, die beim Durchsehen der Mails gefunden wurden: „Tagessoll 8 Stunden bei
einem 100 % Pensum" (Rundmail vom 2025-12-02) und „FTE 0.8 = 32h/Woche" (Antwort vom 2026-09-14).
Die 8,4 h im Code waren nie bestaetigt.

Umgesetzt in `e3545c4`: eine Konstante `HrKpiDashboardBuilder.HoursPerWorkday = 8.0` fuer
Krankheitstage (Stunden / 8,0), die 61-Tage-Grenze der Langzeitkrankheit und den FTE-Ersatz
aus der Rexx-Sollzeit (Sollzeit / 8,0). Wirkung: rund 5 % mehr Krankheitstage und eine hoehere
Krankenquote als bisher; Langzeitkrank gilt schon ab 488 statt 512,4 Stunden; wer nur ueber die
Sollzeit ein FTE bekommt, hat ein um 5 % hoeheres FTE. Neuer Test
`Krankheitstag_Hat_Acht_Stunden`, 788/788. **Produktiv seit 2026-09-30 09:36** (Deploy `2047add`, 773/773 im Release-Worktree, ohne Alarm).

### 8.5 HR-Sitzung vom 2026-09-30

Notizen Ingo nach der Sitzung mit HR, abgeglichen mit Code und den Dateien in `C:\temp` (nur
Kopfzeilen und Werteverteilungen, keine Namen; Sonde `.tmp_tools/HrHeaders0930`).

| Thema | Ergebnis HR | Stand Cockpit | Offen |
| --- | --- | --- | --- |
| Arbeitstag | **Kadermitarbeitende 8,1 h, normale Mitarbeitende 8,0 h** *(ueberholt am 2026-10-22: Kader 8,2 h, Kader = SAP-Mitarbeiterkreis 10 „ML mit Kadervertrag“, siehe 8.8)* | seit 30.09. einheitlich 8,0 h (8.4) | Kaderkennzeichen fehlt in allen Dateien. Kandidaten: SAP `Mitarbeiterkreis` (Monatszeilen 10=55, 15=754, 19=18, 20=308, 30=4; welcher Wert Kader ist, weiss nur HR) oder Rexx `Leitung j/n`. Rexx `Ø taegliche Sollarbeitszeit (Woche)` hilft nicht (Werte wie 5,71 = 40 h / 7 Tage). *Erledigt am 2026-10-01 in `c51445c`, produktiv seit 2026-10-01 09:12:* Ingo bestimmt Rexx **`Leitung j/n` = ja** als Kaderkennzeichen (gemessen in den Dateien vom 26.05. in `C:	emp`: `Abwesenheitinstunden.xlsx` und `Saldiperstichdatum.xlsx` je 44 ja / 218 nein, `Personalausgeschieden.xlsx` 108 nein, `Exportkommengehen.xlsx` ohne die Spalte). `HoursPerWorkdayFor` gibt dann 8,1 h, sonst 8,0 h; gilt fuer Krankheitstage, 61-Tage-Grenze und FTE aus der Sollzeit. Tests `Kader_Mit_Leitung_Ja_Hat_8_1_Stunden` und `BuildAsync_Rechnet_Krankheitstage_Fuer_Kader_Mit_8_1_Stunden`, 800/800. Hinweis: in Rexx kann eine Person der Geschaeftsfuehrung `Leitung = nein` haben; ob jeder Kader so gepflegt ist, liegt bei HR. *(Ueberholt am 2026-10-22: Kader = SAP-Mitarbeiterkreis 10 mit 8,2 h, `Leitung j/n` zaehlt nicht mehr, `ede56b6`, siehe 8.8)* |
| Abwesenheiten | In Rexx wird jeder Fall mit **Von/Bis** erfasst und hat einen **Status** (genehmigt / nicht genehmigt) | `Abwesenheitinstunden.xlsx` ist eine Zeile je Person mit Summen; auch die Spalten „genehmigt" sind Summen | Neuer Rexx-Bericht **eine Zeile je Fall** (Person, Art, Von, Bis, Stunden, Status) als fuenfte Datei. Macht die Krankenquote periodengenau und erlaubt „nur genehmigt". Beispieldatei von Sonja noetig. *Gemessen am 2026-10-01* an `Abwesenheitinstunden.xlsx` (#744, Stand 26.05., 262 Personen): `Krankheit angetreten (Zeitraum)` ist bei 187 Personen gefuellt, immer mit **genau einem** Zeitraum (letzter Fall); 74 davon ohne Stunden im Exportzeitraum (Fall 2024/2025), 46 passen zu Stunden / 8, bei 63 sind die Stunden groesser als der Zeitraum (mehrere Faelle). #744 hat keine Ferienspalte. Zusaetzliche Spalten im Personenbericht machen die Quote also nicht periodengenau |
| Absenz-Ampel | **bis 4,99 % gelb, ab 5 % rot** | Krankenquote gruen < 3 %, gelb 3 bis < 5 %, rot ab 5 % (`HrKpi:Absence*ThresholdPercent`): passt *Erledigt am 2026-09-30 in `81140fa`, produktiv seit 2026-10-01 09:12:* Die Uebersichtskachel „Krankheitstage" warnte erst **ueber** fest verdrahteten 5 % (`absenceRate > 0.05m`); jetzt ab der konfigurierten Rot-Grenze (`ReachesAbsenceRed`, `>=`), Test `Absenzampel_Ist_Ab_Fuenf_Prozent_Rot`, 791/791. Gruen unter 3 % hat HR nicht erwaehnt, gilt als unveraendert |

### 8.6 SAP-Datei `HR_KPI_Export.xlsx`: seit 2026-10-01 automatisiert (taeglicher Abruf `HrKpiSet`)

*Ueberschrift berichtigt am 2026-10-29; bis 2026-09-30 lautete sie „ist nicht automatisiert". Der folgende
Befund beschreibt den Stand vom 2026-09-30, produktiv ist der Abruf seit 2026-10-01 (Tabelle und Messungen unten).*

Die Datei kommt aus dem ABAP-Report `Z_HR_KPI_CONS` bzw. `Z_HR_KPI_CONSOLIDATE`, von Hand
gestartet (`powerbi/datenbeschaffung.txt`, `powerbi/infos.txt`: „CSV via Handstart"). Alle 1'139
Zeilen der Datei in `C:\temp` tragen `Angelegt von = KOI`, der Stand auf dem Server ist vom 26.05.
Es gibt also weder einen SAP-Hintergrundjob noch eine automatische Ablage. Ziel laut Ingo:
**mindestens einmal taeglich.** Moegliche Wege:

- (a) Report als Hintergrundjob (SM36) mit Ablage der Datei auf einem Share, den das Cockpit liest.
  Setzt voraus, dass der Report in eine Datei schreiben kann.
- (b) Die Daten ueber das vorhandene SAP-OData (`travp762`, wie CH/AT) taeglich direkt vom Cockpit
  lesen lassen.

*Ueberholt am 2026-09-30:* ~~Der Quelltext des Reports liegt nicht im Repository.~~ Seit 2026-09-30 als
Ausgangsstand in `docs/abap/Z_HR_KPI_CONS.abap` (aus T76 ueber die Zwischenablage gelesen, 637 Zeilen,
Kopf `REPORT zhr_kpi_consolidate`). Befund:

- **Ein Monat je Lauf** (`p_gjahr`/`p_monat`, Stichtag Monatsende), Quellen PA0000/0001/0002/0007/0008,
  PA2001, HRP1000.
- **Ausgabe nur im Dialog:** ALV oder CSV ueber `cl_gui_frontend_services` (Speicherdialog,
  `gui_download`). Beides geht im Hintergrundjob nicht; deshalb laesst sich der Report so nicht einplanen.
  Die Datei auf dem Server ist ein Excel-Export der ALV-Liste (Spaltentexte wie `Geschäftsjahr`,
  `Buchungsperiode`, `Beschäftigungsgrad %`).
- `save_data` in die Tabelle `ZHRKPI_CONSOLIDATED` ist vorbereitet, aber auskommentiert; die Tabelle gibt es
  nicht.
- Der Report enthaelt Namen, Geburtsdatum und **Bruttolohn**. Das Cockpit liest aus der SAP-Datei nur 13
  Felder (`LoadSapRows`: Personalnummer, Buchungskreis, Personalbereich, Personalteilbereich,
  Mitarbeitergruppe, Mitarbeiterkreis, Teilzeitkennzeichen, Beschaeftigungsgrad, Geschlecht, Planstelle,
  Stellenschluessel, NBU, BU, Abrechnungskreis), weder Namen noch Lohn aus SAP. Der Leser nimmt je Person
  die **erste** Zeile. *Praezisiert am 2026-09-30:* die 1'139 Zeilen sind **ein** Monat, nicht mehrere: PA0001 hat zum Stichtag auch Ausgetretene, Rentner und Passive (`HrKpiSet` in T76 fuer 09/2026: rund 1'150 Zeilen, aus der Antwortgroesse geschaetzt). Der Leser verknuepft ueber die Personalnummer mit den aktiven Rexx-Zeilen, die uebrigen stoeren nicht.

**Bauplan, Entscheid Ingo 2026-09-30:** kein Job und keine Z-Tabelle, sondern ein EntitySet
`HrKpiSet` im Service `ZPOWERBI_EINKAUF_SRV`, das bei jedem Abruf live aus PA0001, PA0002, PA0007 und
PA2001 rechnet (Stichtag Monatsende, Filter `Gjahr`/`Monat`, ohne Filter laufender Monat). Nur die
Felder, die das Cockpit liest; keine Namen, kein Geburtsdatum, kein Lohn. Das Cockpit holt taeglich ab
05:00 ab und schreibt `hrdata/HR_KPI_Export.xlsx` (vorher einmalig Sicherung der Handdatei als
`HR_KPI_Export.manuell.xlsx`); faellt SAP aus, bleibt die letzte Datei stehen.

| Teil | Stand 2026-09-30 |
| --- | --- |
| Struktur `ZSTR_HR_KPI` (16 Felder) | **aktiv in T76**, Paket `ZPP`, Transport `T76K912530`; per DD03L gegengelesen. Beim ersten Anlegen fehlte `TEILK`, weil `SapGuiStrukturFelder.vbs` an der Seitengrenze die neunte Zeile ueberschrieb (behoben) |
| Modell `DEFINE` | **aktiv**, Gesamtquelle `docs/abap/ZPOWERBI_EINKAUF_MPC_EXT_DEFINE.abap` (Journal plus HR), Include `ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001`, ueber RFC gegengelesen. Decimal: `EMPCT` DEC 5,2, `ABWTG` DEC 6,2 laut DD03L |
| Daten `GET_ENTITYSET` | **aktiv**, Gesamtquelle `docs/abap/ZPOWERBI_EINKAUF_DPC_EXT_GET_ENTITYSET.abap`, Include `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W`, ueber RFC gegengelesen |
| Transport | DPC_EXT und MPC_EXT sind durch `T76K912530` (Journal, nicht freigegeben *(Stand 2026-09-30; am 2026-10-01 nach P76 importiert)*) gesperrt; HR geht damit **zusammen mit dem Journal** nach P76 (Entscheid Ingo fuer diesen Service) |
| Berechtigung | Der Data Provider liest PA-Tabellen per `SELECT`, ohne `P_ORGIN`-Pruefung: wer `ZPOWERBI_EINKAUF_SRV` lesen darf, sieht diese Felder |
| Cockpit-Leser und Tagesabruf | `Services/HrKpi/SapGatewayHrKpiReader.cs` (`4051506`), nur Produktion, ab 05:00 einmal taeglich, Verbindung wie der Einkauf. 795/795 Tests, darunter der Rundweg Datei schreiben und mit dem echten HR-Leser lesen. **Produktiv seit 2026-10-01 09:12** (Stand `ba9cf5c`) |

**Gemessen im Gateway Client T76, 2026-09-30** (Antwortrumpf nicht lesbar, nur Status und Laenge): `$metadata` 200 (342'123 Zeichen statt 341'032); `HrKpiSet` `$top=5` 200 in 0,7 s; Filter 2026/09 Seite 1 200 (560'915), Seite 2 mit `$skip=1000` 200 (84'670), Monat 13 200 mit leerer Liste; Gegenproben `MAKTSet` und `FinanzJournalSet` 200. **Die Werte selbst sind noch nicht zeilenweise gegen den Report gegengelesen.** *(ueberholt am 2026-10-01: Wertestichprobe in P76 bestanden, siehe unten)*

**Offen bis produktiv:** (1) Stichprobe der Werte gegen `Z_HR_KPI_CONS` fuer 09/2026 (Screenshot oder Browser mit Sitzung) *(erledigt am 2026-10-01, Wertestichprobe P76 bestanden)*; *Erledigt am 2026-10-01:* (2) Transport `T76K912530` von Ingo nach P76 importiert; (3) Deploy des Cockpits um 09:12.

**Gemessen im Gateway Client P76, 2026-10-01** (nach dem Import): `$metadata` 200 (343'930); `HrKpiSet` 2026/09 Seite 1 200 in 1,6 s (560'979), Seite 2 200 (93'147), 2026/10 200 (560'979); `MAKTSet` 200; `FinanzJournalSet` mit `Bukrs eq '1200' and Gjahr eq '2026'` 200 in 0,9 s (ohne Filter 500 nach 154 s, das ist der bekannte Vollscan, kein Transportfehler). Der Abruf des Cockpits um 09:14 lief vor dem Import auf 404 (AppEventLog 15372) und versucht es erst am 02.10. ab 05:00 wieder (einmal pro Tag), ausser die App startet neu. *Wertestichprobe am 2026-10-01 bestanden:* Ingo hat `HrKpiSet?$format=json&$top=3` (P76, Monat 10) eingefuegt; Personen 2004, 2005, 2007 stimmen in allen uebertragenen Feldern (Buchungskreis, Personalbereich, -teilbereich, Mitarbeitergruppe, -kreis, Teilzeit, Beschaeftigungsgrad, Geschlecht, Planstelle, Stellenschluessel, Abrechnungskreis, NBU/BU 0) mit der Handdatei `Z_HR_KPI_CONS` vom 26.05. ueberein. Auffaellig, aber in beiden gleich und damit SAP-Stammdaten: 2005 hat Teilzeit `X` bei 100 % und Planstelle 99999999. **Offen:** *Entschieden am 2026-10-01 (Ingo: „entscheide du anhand dashboard“):* der Abruf holt weiter den **laufenden Monat**. Das Cockpit nimmt aus SAP fast nur Stammdaten zum Stichtag (Beschaeftigungsgrad fuer FTE und Krankenquote, Teilzeit, Geschlecht, Planstelle), die neben den Rexx-Dateien mit heutigem Stand stehen; mit dem Vormonat fehlten Eintritte des laufenden Monats und Pensumwechsel. Nur die Kachel Unfalltage (BU/NBU) zeigt damit „Monat bis heute“ und ist am Monatsanfang fast 0. *Umgesetzt in `931f151`, produktiv 2026-10-01 09:44:* Kacheln heissen „Unfalltage laufender Monat“, „BU-/NBU-Tage laufender Monat“, Detail „nur laufender Monat bis heute, unabhaengig vom Zeitraumfilter“, Schulungsseite angepasst, Test `BuildAsync_Beschriftet_Unfalltage_Als_Laufenden_Monat`, 801/801. Offen als Vorschlag: BU/NBU ab Jahresbeginn im DPC liefern (neuer Transport).

**Erster produktiver Abruf 2026-10-01 09:46** (durch den Neustart beim Deploy 09:44): AppEventLog 15373 „SAP-HR-Datei aus OData geschrieben“, **1'168 Personen**, Stichtag 2026-10; `hrdata/HR_KPI_Export.xlsx` 77'167 Bytes, Handdatei als `HR_KPI_Export.manuell.xlsx` (26.05.) gesichert. Die Rexx-Dateien auf dem Server sind weiter vom 08.07.

### 8.7 Abwesenheiten je Fall aus SAP `PA2001` statt Rexx (gemessen 2026-10-01)

Frage Ingo: lassen sich Abwesenheiten ueber SAP genauer bestimmen, sodass Rexx entfaellt? Per RFC in **T76** gelesen (nur Art und Beginn, keine Personen): `PA2001` hat **eine Zeile je Fall mit Beginn und Ende**. Seit 01.01.2026 572 Faelle, juengster Beginn 31.03.2026 (Stand der T76-Kopie, nicht P76): 0260 Krankheit bis 60 Tage 254, 0100 Ferien 198, 0400 Kompensation 104, 0600 Militaer/Zivilschutz 9, 0500 Absenzen bezahlt 5, 0290 Unfall ab 3. Tag 1, 0270 Krankheit groesser 60 Tage 1 (Texte `T554T`). Damit waeren Krankenquote und Ferien periodengenau, ohne die Rexx-Liste je Fall (8.5). **Nicht belegt:** wie aktuell `PA2001` in P76 ist (vermutlich Uebernahme aus Rexx fuer die Lohnabrechnung, dann hinkt der laufende Monat nach), ob nur genehmigte Faelle ankommen, und ob die Zahl der Faelle der in Rexx entspricht. Naechster Schritt: in P76 die juengsten `PA2001`-Eintraege ansehen (SE16N, Ingo), dann ein zweites EntitySet `HrAbsenzSet` (Person, Art, Von, Bis, Tage, Stunden) im selben Muster wie `HrKpiSet`. Fuer Saldi, Restferien, GLZ und Austritte bleibt Rexx vorerst noetig.

**Gebaut am 2026-10-01** (Ingo: P76 ist immer aktuell *(ueberholt am 2026-11-03 fuer Ferien, Kompensation, Militaer: sie kommen nur mit Sonjas Rexx-Import nach SAP, Abschnitt 8.9)*; Rexx bleibt, nur Krankenquote und Ferien aus SAP; Entscheide: Erweiterung im Code wie `HrKpiSet`, Tage und Stunden plus Kompensation und Militaer, **zweites Set je Fall** statt Monatssummen):

| Teil | Stand |
| --- | --- |
| SAP T76 | Struktur `ZSTR_HR_ABSENZ` (Paket ZPP; PERNR PERSNO, GJAHR, AWART, BEGDA/ENDDA CHAR8, SEQNR, ABWTG, STDAZ ABSTD, KALTG; per DD03L alle neun aktiv), `DEFINE` und `GET_ENTITYSET` erweitert (`docs/abap/ZHR_ABSENZ_*_ADD.abap`, Gesamtstaende per RFC gegengelesen), **neuer Transport `T76K912644`** (Aufgabe `T76K912645`, alle drei Objekte). Cache `/IWFND/CACHE_CLEANUP` fuer `ZPOWERBI_EINKAUF_MDL` |
| Gemessen Gateway Client T76 | `$metadata` 200 (345'782 statt 343'930), `HrAbsenzSet` 2026 Seite 1 200 (312'027), Seite 2 200 leer, 2025 200 (545'547); Gegenproben `HrKpiSet`, `MAKTSet`, gefiltertes `FinanzJournalSet` 200 |
| Set | Filter nur `Gjahr`; jeder Fall, der das Jahr beruehrt, mit vollem Von/Bis; **alle Arten**, gesperrte Saetze (`SPRPS = X`) nicht |
| Cockpit `c749d32` | `SapGatewayHrAbsenceReader`, Tagesdienst holt nach `HrKpiSet` Vorjahr und laufendes Jahr nach `hrdata/HR_Absenzen_SAP.xlsx` (eigener Fehlerpfad). Builder: **liegt die Datei vor, kommt die Krankheit aus SAP** (Arten 0220, 0230, 0240, 0260, 0270; 0210 Arzt nicht), je Fall nach Arbeitstagen auf den Zeitraum zugeschnitten, offenes Ende je Arbeitstag mit 8,0/8,1 h *(Kader seit 2026-10-22 8,2 h, 8.8)*; ohne Zeitraumfilter laufendes Jahr bis heute; 61-Tage-Regel unveraendert. Neu in „Zeit und Ferien“: Ferien bezogen, Kompensation, Militaer/Zivilschutz im Zeitraum (Tage). Fehlt die Datei, rechnet alles wie bisher aus Rexx. 809/809 |
| Nachbesserung `c12b34a` | (1) Ein Zeitraum vor dem 01.01. des Vorjahres (die Datei hat nur Vorjahr und laufendes Jahr) faellt mit Hinweis auf Rexx zurueck, statt eine leere, scheinbar verlaessliche 0-%-Quote zu zeigen. (2) Der Absenzen-Abruf hat einen eigenen Faelligkeitsstempel; ein Neustart nach dem Transport schreibt die Datei noch am selben Tag. (3) Offenes Ende: Stunden bis heute je Arbeitstag mal Pensum (FTE). 810/810 |
| Verhalten neu | Mit SAP-Datei zeigt die Ansicht **ohne Zeitraumfilter das laufende Jahr bis heute**, nicht mehr die Spanne der Rexx-Daten; HR sieht damit andere Standardzahlen |
| Pruefliste P76 nach dem Import | `$metadata` muss von 343'930 auf rund 345'782 Zeichen wachsen (T76-Zuwachs), sonst ist der Modellcache alt: `/IWFND/CACHE_CLEANUP` fuer `ZPOWERBI_EINKAUF_MDL` in P76. Dann `HrAbsenzSet` 200 mit Daten, Gegenproben `HrKpiSet`, `MAKTSet`, gefiltertes `FinanzJournalSet` |
| **Produktiv 2026-10-01** | Ingo hat `T76K912644` nach P76 importiert. P76: `$metadata` 345'782 (genau der T76-Wert, Cache aktuell), `HrAbsenzSet` 2026 zwei Seiten (545'549 + 284'236, Seite 3 leer), 2025 200, Gegenproben 200. Deploy **10:50** (Stand `b54a503`, 795/795 Release). Erster Abruf **10:52**: 4'948 Faelle 2025/2026, 265 Personen, kein offenes Ende (AppEventLog 15374) |
| Plausibilisiert | Krankheit 2025 (0240, 0260, 0270) 1'958,6 Tage, bei rund 250 FTE etwa 3,1 %; 2026 bis heute (0230, 0240, 0260, 0270) 1'313 Tage, etwa 2,8 %. Ferien 2025 1'736 Faelle / 4'771 Tage, Kompensation 2025 1'339 Tage, Militaer 2025 140 Tage. In den Daten kommen 0210 und 0220 gar nicht vor; 0250 Vaterschaftsurlaub und 0900 unbezahlter Urlaub zaehlen nicht als Krankheit. Ferien 2026 reichen bis 31.12. (geplant); die Kennzahl zaehlt nur bis heute |
| Grenze | Die Personenbasis kommt weiter aus den Rexx-Dateien vom 08.07.; wer danach eingetreten ist, fehlt im Nenner und mit seinen SAP-Faellen. Erst die taeglichen Rexx-Exporte (Upgreat, Wochen_Todo 55) schliessen das |
| Offen | HR bestaetigt die Krankheitsarten (praktisch nur noch, ob 0230/0240 wie 0260 zaehlen sollen); Sichtpruefung im Cockpit **HR bestaetigen lassen:** welche Arten Krankheit sind (0210 Arzt? 0220 „Krank kleiner 3 Tage“?), ob Ferien in SAP vollstaendig gebucht werden |


Rexx-Berichtsnummern aus denselben Notizen, fuer die Anfrage bei Upgreat: Abwesenheit in Stunden
`#744`, Export KOMMEN/GEHEN `#732`, Personal ausgeschieden `#381`, Abwesenheiten Uebersicht `#742`,
Stammdaten SAP `#735`.

### 8.8 Kader neu: SAP-Mitarbeiterkreis 10, 8,2 h (Entscheid Ingo 2026-10-22)

Ingo am 22.10.: „Korrektur fuer 8 Stunden, Feld dafuer Arbeitsverhaeltnis (10 ML mit Kadervertrag) waere Schluessel fuer 8,2
dezimal“. Die Rexx-Exporte fuehren das Feld nicht (Arbeitsverhaeltnis dort nur befristet/unbefristet); der taegliche
SAP-Abruf `HrKpiSet` liefert den Mitarbeiterkreis. T503T (T76, gelesen 2026-10-22): 10 „ML mit Kadervertrag“, 15 Monatslohn,
19 Lehrling, 20 Stundenlohn, 30 Heimarbeit.

- `HrKpiDashboardBuilder.HoursPerWorkdayFor(mitarbeiterkreis)`: Kreis 10 -> **8,2 h**, sonst 8,0 h; ohne SAP-Zeile 8,0 h.
  Gilt fuer FTE aus der Sollzeit, Krankheitstage aus Rexx #744 (Zuordnung ueber die Personalnummer zur SAP-Zeile) und aus
  SAP `PA2001`, und damit fuer die 61-Tage-Langzeitgrenze.
- Rexx „Leitung j/n“ zaehlt nicht mehr als Kader (bis 22.10.: 45 Personen; nach Kreis 10 rund 19 der Rexx-Liste).
- Tests `Kader_Mitarbeiterkreis_10_Hat_8_2_Stunden`, `BuildAsync_Rechnet_Krankheitstage_Fuer_Kader_Mit_8_2_Stunden`;
  Suite 1288/1288. Produktiv seit 2026-10-22 15:48.

*Abschnittsnummer am 2026-10-29 von „8.7" (doppelt) auf 8.8 berichtigt und vor die Querverweise gestellt.*

### 8.9 Wie die Rexx-Daten nach SAP kommen; Felder nur in SAP, beide, nur in Rexx (2026-11-03)

Untersucht in **T76, nur lesend** (P76 nicht angefasst). Volltext mit Tabellen, Mails und
Quelltexten: `saptasks/rexx_schnittstelle/NOTES.md`.

- **Schnittstelle:** Eigenentwicklung der **EXPRIS AG** (2023/24, Paket `Z_HR_PA_REXX`). Drei Importe
  Rexx → SAP, die **Sonja Richter (`RIS`) von Hand** im Dialog startet: `Z_HR_PA_REXX` „Interface
  REXX“ (Stammdaten, Vergleich Rexx/SAP ueber die AHV-Nummer, schreibt u. a. IT 0002/0007/0008,
  Massnahmen Z1/Z2/Z3/Z5 „aus Rexx“), `Z_HR_REXX_PA_ABS` (Abwesenheiten per Batch-Input PA30) und
  `Z_HR_REXX_PA_ZE` (Kommen/Gehen nach PA2011); dazu `Z_HR_REXX_PA_LA_EXP` SAP → Rexx (Lohnarten).
  Dateien vom PC oder aus `/usr/sap/rexx` (Alias `DIR_REXX`). Kein Job in T76; wer `/usr/sap/rexx`
  beschreibt, ist offen (P76 AL11).
- **Krankheit, Kind-Krank und Unfall uebernimmt der Absenz-Import ausdruecklich nicht.** Sie stehen
  trotzdem in PA2001, an anderen Tagen angelegt; vermutlich erfasst Sonja sie von Hand (nicht belegt).
- **Folge fuer 8.7:** Ferien, Kompensation, Militaer und bezahlte Absenzen kommen nur mit Sonjas
  Import nach SAP (in T76 am 20.11.2025, 17.12.2025 und 22.02.2026). *Die Aussage „P76 ist immer
  aktuell“ in 8.7 gilt fuer diese Arten nicht (ueberholt am 2026-11-03):* der laufende Monat ist im
  Cockpit in der Regel unvollstaendig.
- **Neu fuer die Quellenfrage:** Ferienkonten liegen in SAP PA2006 (Kontingentarten 10 Anspruch,
  11 Rest Vorjahr, 20 Kompensation, 30 Kader Zusatz); Austrittsart nur teilweise (ZC mit Grund,
  Z5 „aus Rexx“ ohne Grund); Kommen/Gehen kommt monatlich nach SAP, ob SAP den GLZ-Saldo rechnet,
  ist ungeprueft.

**Felder des Cockpits nach Quelle** (Herkunft heute am Code belegt, `HrKpiDashboardBuilder.cs`;
„beide“ heisst: SAP fuehrt das Feld nach den PA-Tabellen, nicht zeilenweise geprueft):

| Quelle | Felder |
| --- | --- |
| **nur SAP** (`HrKpiSet`, `HrAbsenzSet`) | Beschaeftigungsgrad, Mitarbeiterkreis (Kader), Mitarbeitergruppe, Teilzeitkennzeichen, Geschlecht, Buchungskreis, Personalbereich, Personalteilbereich, Planstelle, Stellenschluessel, Abrechnungskreis, BU-/NBU-Tage, Kompensation und Militaer je Fall |
| **beide**, heute aus SAP | Personalnummer (Schluessel), Krankheit je Fall, Ferien bezogen je Fall |
| **beide**, heute aus Rexx | Name, Eintritt, Austritt, Status, Geburtsdatum, Organisation, Kostenstelle, Stelle, Sollzeit/Arbeitszeitmodell, Bruttolohn |
| **nur Rexx** (Stand 03.11. vor der Untersuchung) | GLZ-Saldo, Ferienanspruch, Restferien, Ferien ausstehend (`Saldiperstichdatum`), Austrittsart (`Personalausgeschieden`), „Leitung j/n“ (seit 22.10. unbenutzt). *Nach dem Befund oben: Ferienkonten wohl auch in PA2006, Austrittsart teilweise; ungeprueft* |

**Idee Ingo (03.11., nicht gebaut):** eigenes Programm, das die drei Importe buendelt, die fehlenden
Felder mitnimmt und nur in eigene Z-Tabellen schreibt, taeglich als Job; darf nie Infotypen oder
Loehne veraendern. Voraussetzung ist, dass Rexx die Dateien taeglich in `/usr/sap/rexx` ablegt.
Leitplanken und offene Entscheide: `saptasks/rexx_schnittstelle/NOTES.md` Abschnitt 7.

**Mails (nur Betreff gelesen):** Aus der Mail an Ueli Kunzmann wurde das **Upgreat-Ticket
#12699584** (eroeffnet 01.10., geschlossen 19.10., Antwort Ingo 20.10.); am 29.10. kam
„WG: Angebot: AG36071 / Rexx Hub“. *Nachtrag 2026-11-03, Inhalte gelesen:* Upgreat schloss das Ticket mit „Unable to solve“, Rexx betreut Trafag selbst; Alex leitete am 29.10. Nadjas Hub-Angebot weiter (heute 8 Schnittstellen 11'200 EUR/Jahr, 4 weitere 5'600 EUR/Jahr, Hub 4'800 EUR/Jahr); Sonja fragte am 02.11., ob HR schon Live-Daten testen kann. Einzelheiten `saptasks/rexx_schnittstelle/NOTES.md` 11. *Nachtrag 03.11. nachmittags: Abschlussrunde (Rexx-Ansprechpartner Artjom Yahno), Sitzung Rexx Hub naechste Woche mit Alex und Artjom; Nadja und Sonja laut Alex nicht noetig.* *Der Stand in 8.3 („Antwort
Ueli offen“) ist damit ueberholt (2026-11-03).*

### 8.10 Pruefung aller Formeln, 2026-11-04: drei Rechenfehler behoben, drei fachliche Punkte offen

Frage Ingo: „sind alle Formeln im HR-Dashboard korrekt?“ Geprueft wurde der Rechenkern `HrKpiDashboardBuilder.cs`
gegen die Abschnitte 3, 4 und 8. **Korrekt:** Feiertagskalender ZH und Ostern, 8,0/8,2 h, Krankenquote-Zaehler
(Teilzeit in Zaehler und Nenner gleich gewichtet), Fluktuation Monat/Quartal/YTD/gleitend/Vorjahr, Ampeln (Absenz
3/5 %, GLZ 50/100 h, Restferien nach Quartal), anteiliger Zuschnitt der SAP-Faelle, Doppelfaelle ueber den Jahreswechsel.

**Behoben** (Auftrag Ingo „zuerst beheben“, Tests in `HrKpiServiceTests` und `HrKpiInteractiveTests`):

| # | Fehler | Wirkung bisher | Jetzt |
| --- | --- | --- | --- |
| 1 | Nenner der Krankenquote = Summe FTE x alle Arbeitstage des Zeitraums, ohne Eintrittsdatum | Wer im Juni eintrat, zaehlte ab 01.01. mit; in einer Ansicht 2025 auch alle Eintritte 2026. Quote zu tief, am staerksten bei vergangenen Jahren | `AbsenceDenominator`: je Person FTE x Arbeitstage ab Eintritt (`FteWorkdays`). Detail der Kachel nennt die FTE-Arbeitstage |
| 2 | Quote je Person = Tage / Arbeitstage, ohne Pensum | Eine 50-%-Stelle, die den ganzen Zeitraum krank war, zeigte 50 % | `AggregateAbsencesByPerson` teilt durch die eigenen FTE-Arbeitstage (ab Eintritt) |
| 3 | Prognose = Quartalsrate x 4 auch fuer ein angebrochenes Quartal | Am 04.11. nur 35 von 92 Tagen im Wert, Prognose rund Faktor 2,6 zu tief; am Quartalsanfang fast 0 | Vorgabe HR „x 4“ bleibt; ein laufendes Quartal wird vorher auf seine volle Laenge hochgerechnet (`QuarterProgress`, `ForecastFromQuarter`), HC Quartal nur bis zum Stichtagsmonat. Detail nennt „hochgerechnet aus n von m Tagen“. **Grenze:** Austritte haeufen sich am Monatsende; im ersten Quartalsmonat bleibt die Prognose deshalb meist nahe 0 und springt am ersten Monatsende um den Hochrechnungsfaktor. Alternative fuer einen Entscheid: nach abgeschlossenen Monaten statt Tagen hochrechnen |

**Offen, braucht Entscheid Ingo oder HR (nicht geaendert):** *(ueberholt am 2026-11-04: 61-Tage-Regel, Vorjahresvergleich,
Kachelbeschriftung und die gelbe Stufe sind nach den schriftlichen HR-Vorgaben umgesetzt, Abschnitt 8.12; Unfall zaehlt jetzt mit)*

- **61-Tage-Regel:** gerechnet wird die Summe je Person im gefilterten Zeitraum, aus FTE-Stunden. In einer
  Monatsansicht ist „Krankheit Lang“ deshalb immer 0, mehrere kurze Faelle koennen im Jahr „lang“ ergeben, eine
  50-%-Stelle erreicht die Grenze erst nach rund 122 Arbeitstagen. Die Summenregel vom 2026-09-28 war noetig, weil Rexx
  keine Faelle hatte; SAP unterscheidet selbst 0260 „bis 60 Tage“ und 0270 „groesser 60 Tage“.
- **Vorjahresvergleich im laufenden Jahr** (`BuildPeriodComparisonMetrics`): Austritte bis heute durch den Headcount
  ueber zwoelf Monate gegen das ganze Vorjahr; das Delta ist bis Dezember fast zwangslaeufig negativ.
- **Uebersichtskachel bei Von/Bis:** heisst „Fluktuation 2026“, zeigt aber z. B. Q3-Austritte durch den Headcount
  Januar bis September.
- **Nachtrag 2026-11-04 beim Pruefen der neuen Rexx-Exporte:** (a) Der Leser erkennt die echte Spalte
  „Ø tägliche Sollarbeitszeit (Woche)“ nicht (Aliase nur „O taegliche …“ und kaputte Umlaut-Varianten; mit einer
  Wegwerfprobe im eigenen Worktree belegt). Eine aktive Person ohne SAP-Beschaeftigungsgrad, die die Zeitdatei findet,
  faellt dadurch als „Reminderprofil“ aus allen Kennzahlen. Zudem ist der Wert Wochenstunden / 7 (5,71 bei 40 h); als FTE
  gelesen ergaebe er 0,71 statt 1. (b) Gezaehlt wird nur „Personal Status = Aktiv“; im Export vom 04.11. fehlen dadurch
  9 „Vertrag endend“ und 1 „Langzeitkrank“ in Headcount, FTE und Absenzen. *Erledigt 2026-11-04, siehe 8.11.*
- Kleinigkeiten: Kachel „Krankheitstage“ kennt nur rot, kein gelb; aktive Befristete zaehlen im Headcount (Typ nur aus
  dem Stellentext); 0290 Unfall zaehlt nicht als Krankheit; „Ferien bezogen = Anspruch − Rest − ausstehend“ ungeprueft
  gegen die Rexx-Logik; laufende Fluktuationswerte stehen auf 0, solange die Rexx-Dateien alt sind (8.2).

### 8.11 Uebergang bis zur Rexx-Schnittstelle: das Aktuellere gewinnt (2026-11-04)

Auftrag Ingo: „alles, was aktueller ist, vom File nehmen, so dass die Anzeige im Dashboard stimmt“. Anlass: SAP PA2001
hinkt nach (Abruf 04.11. 06:22): Ferien 0100 juengster Beginn 27.07., Kompensation 0400 29.07., bezahlte Absenzen 0500
24.07., Militaer 0600 20.07. (nur mit Sonjas Rexx-Import, 8.9); Krankheit 0260 bis 23.10. Im Rexx-Export vom selben Tag
fehlten 70 der 202 juengsten Krankheitsfaelle in SAP, 33 davon aus Oktober/November (gemessen nur ueber Zaehlwerte).

| Teil | Umsetzung |
| --- | --- |
| Status | „Aktiv“, „Vertrag endend“ und „Langzeitkrank“ zaehlen als angestellt (`ActiveStatuses`); vorher nur „Aktiv“ |
| Sollzeit | Spalte „Ø tägliche Sollarbeitszeit (Woche)“ wird erkannt (`NormalizeHeader` liest „ø“ als „o“). Der Wert ist Wochenstunden / Planungstage (5,71 = 40 h / 7, 6,67 = 40 h / 6) und taugt nicht als FTE; ohne SAP-Grad gilt deshalb „Vollzeit“ = 1,0 vor der Sollzeit. Betroffen: 4 aktive Personen ohne SAP-Zeile, die bisher als Reminderprofil ausfielen |
| Krankheit | SAP je Fall plus der juengste Rexx-Fall je Person („Krankheit angetreten (Zeitraum)“, „Krank nicht buchbar angetreten (Zeitraum)“), wenn kein SAP-Krankheitsfall der Person ihn ueberschneidet und er hoechstens 30 Tage vor dem juengsten SAP-Fall oder spaeter beginnt (`BuildRexxGapSicknessRows`). Stunden = Arbeitstage ZH x 8,0/8,2 h x Pensum. Aeltere Abweichungen bleiben (eher Artzuordnung). Auch im Reiter Interaktiv (Folgenummer `REXX`). Grenze: Rexx nennt je Person nur den juengsten Fall; hatte jemand im Rueckstandsfenster zwei Faelle, fehlt der fruehere |
| Ferien, Kompensation, Militaer | aus diesen Rexx-Dateien nicht je Fall ergaenzbar (#744 hat keine Ferien). Kachel-Detail nennt den juengsten SAP-Fall, Kachel gelb ab 30 Tagen Rueckstand, Hinweis oben und Warnung in „Ferien am Stueck“. „Ferien bezogen“ aus Rexx-Saldi ist aktuell. *Ueberholt mit #775 (8.13): fuer 01.01. bis Exporttag je Person der hoehere Wert aus SAP und Rexx; „Ferien am Stueck“ bleibt SAP* |

Probe mit den echten Serverdateien (nur Kennzahlen): 260 Mitarbeitende, FTE 238,0; 949 SAP-Krankheitsfaelle plus 34 aus
Rexx ergaenzt, 1'099,8 Krankheitstage, Krankenquote 2026 bis heute 2,3 %; Fluktuation YTD 4,8 % (11 relevante Austritte).
Suite 1381/1381. **Produktiv seit 2026-11-04 09:57** (`cfe2631`, `docs/rag/DEPLOYMENT.md`). Dauerhafte Loesung:
Rexx-Schnittstelle (Sitzung naechste Woche) oder ein Rexx-Bericht je Fall (#742).

### 8.12 HR-Vorgaben aus Mails und Teams umgesetzt (2026-11-04)

Auftrag Ingo: „alles umsetzen, Erkenntnisse Mails und Teams“. Gelesen (Outlook Web und Teams, nur lesend): Nadjas
Antworten vom 2025-10-08 und 2025-10-17 (Thread „Power BI Schulung – Klaerungsfragen“), Sonjas Anforderungsliste vom
2026-02-03 (selber Thread) und Sonjas Antworten vom 2026-09-14 („AW: Fragen zu HR Dashboard“, Grundlage von Abschnitt 8).
Teams: Chats Sonja und Nadja ohne Fachinhalt; Nadja 2026-02-24 „beim Dashboard ist Sonja im Lead“; Filtervorgaben
2026-05-13 (siehe 8.3). Wortlaut sinngemaess:

| Vorgabe HR | Quelle | Umsetzung |
| --- | --- | --- |
| Langzeit ab dem 61. Tag, dann zahlt die Krankentaggeldversicherung; in SAP Arten „Krank bis 60 Tage“ und „Krank ab 61. Tag“; Kurzzeit bis 60 Tage | Nadja 2025-10-17, Sonja 2026-02-03 und 2026-09-14 | Je Krankheit (SAP-Faelle ohne Arbeitstag dazwischen verbunden): bis zum 60. Kalendertag kurz, ab dem 61. lang, ein Fall ueber die Grenze wird nach Arbeitstagen geteilt; 0270 immer lang. Rexx ohne Faelle: erste 60 Tage der Summe kurz, Rest lang. *Ueberholt:* „ganze Krankheit lang, Summe je Person“ (Entscheid 2026-09-28, Abschnitt 8 Zeile 2) |
| Absenzquote unter „Krankheit &amp; Unfall“, Quartalsbericht „Krankheits- und Unfallstatistik (Absenzquote)“, Fit &amp; Wohl ueber Krankheit &amp; Unfall | Sonja 2026-02-03, Nadja 2025-10-08 | 0290 Unfall zaehlt in Krankenquote, Krankheitskalender und Fit &amp; Wohl; nicht in die 60-Tage-Regel. Kachel-Detail nennt Unfalltage |
| „Krankenquote 1x gesamt und 1x ohne Langzeitkrankheiten“ | Nadja 2025-10-08 und 2025-10-17 | Neue Kachel „Quote ohne Langzeit“ (Tage ab dem 61. Tag weggelassen, gleicher Nenner) |
| Ampel gruen bis 3 %, gelb bis 4 %, rot ab 5 % (Sitzung 30.09.: bis 4,99 % gelb) | Sonja 2026-02-03 | Uebersichtskachel „Krankheitstage“ jetzt mit Gelb ab der Gelb-Grenze und Rot ab der Rot-Grenze |
| „Vergleich zum Vorjahr“ monatlich und je Quartal | Sonja 2026-02-03 | Periodenvergleich im laufenden Jahr: 01.01. bis Stichtag gegen denselben Zeitraum des Vorjahres; Detail „(je 01.01.-TT.MM.)“ |
| (technisch) Fluktuationskachel bei Von/Bis | Pruefung 8.10 | Beschriftung „Fluktuation TT.MM.JJJJ-TT.MM.JJJJ“, Headcount-Mittel genau ueber die gewaehlten Monate, „nicht annualisiert“ |

Probe mit den echten Serverdateien (2026 bis 04.11., nur Kennzahlen): Krankenquote 2,5 % (vorher 2,3 %, jetzt mit 56
Unfalltagen), ohne Langzeit 2,2 %, 7 Langzeitkranke, Krankheit lang 112 Tage statt der ganzen Krankheit; Vorjahresvergleich
2026 4,8 % gegen 2025 1,4 % je bis 04.11.

**Neue Frage an HR:** Der Rexx-Export vom 04.11. hat fuer 2025 nur 49 Austritte (6 „Kündigung AN“, davon 3 Festangestellte;
24 „Kündigung AG“). Die „Kontrollwerte 2025“ in Abschnitt 3 (104 Austritte, 42 AN-Kuendigungen, 33 relevant) passen
dazu nicht; sie stammen aus einer aelteren oder anders abgegrenzten Datei. Offen bleiben ausserdem: aktive Befristete im
Headcount, ob 0210/0220/0280 Krankheit sind, die Formel „Ferien bezogen“ *(geklaert in 8.13: sie zog die geplanten Tage
doppelt ab; mit #775 direkt „Ferien angetreten (Tage)“)*. Suite 1386/1386. **Produktiv seit 2026-11-04
10:21** (`69a63b6`, `docs/rag/DEPLOYMENT.md`).

### 8.13 Rexx-Gesamtabfrage #775 statt vier Einzeldateien (2026-11-04)

Auftrag Ingo: „alle 5 Exporte in einer grossen Abfrage“, dann „pruefe besser vorher, dann baue alles ein und nimm Daten
von dort, wo sie aktueller und genauer sind“. In Rexx (Personal › Abfrage) angelegt: private Abfrage **#775 „HR-Cockpit
Gesamt (Dashboard)“**, eine Zeile je Person inklusive Ausgeschiedener (393 Zeilen am 04.11.), Filter „Zeitraum von/bis“
01.01.2026 bis 04.11.2026, gespeichert. Rexx erlaubt hoechstens **50 Spalten**; #775 ist voll. Export als
`Hrcockpitgesamtdashboard.xlsx` nach `hrdata`; liegt die Datei dort, liest das Cockpit nur sie (`HrKpiDataSourceOptions.GesamtFile`,
Code-Vorgabe, nicht in `appsettings.json`), sonst die vier Einzeldateien wie bisher.

**Befund vorher, Anzeigefehler seit 09:30:** Die gespeicherte Abfrage „Saldi per Stichdatum“ (#757) steht auf Stichtag
31.12.2024 und Zeitraum Dezember 2024. Der Export vom Morgen lief damit; 249 von 265 „Urlaub Rest“ wichen vom Stand heute
ab (Summe 510 statt 1'877 Tage), auch der Stundensaldo. Um 11:23 durch einen #757-Export mit Stichtag 04.11.2026 ersetzt
(alte Datei in `hrdata\archiv_rexx_2026-07-08\Saldiperstichdatum_2026-11-04_0929_Stichtag2024.xlsx`); im Produktivcockpit
gesehen: Ferien Rest 1'579.5.

Abgleich #775 gegen die Einzelexporte (nur Zaehlwerte): keine Person fehlt; Eintritt, Austritt, Austrittsart, Kostenstelle,
Sollzeit, Geburtsdatum, Anspruch und „Ferien ausstehend“ gleich (Abweichungen nur bei zwei Testpersonen). Spaltenwahl:

| Feld im Cockpit | Spalte #775 | Hinweis |
| --- | --- | --- |
| Organisation | „Organisation“, erster Teil vor „, “ | Rexx schreibt mehrere Zuordnungen als „A, B“; erster Teil = „Organisation-1“ aus #381 (125 von 128) |
| Austritte | Status „Ausgeschieden“ (128) | 29 weitere Personen haben ein kuenftiges Austrittsdatum und sind noch angestellt |
| Urlaub Rest | „Urlaub Rest“ + „Ferien ausstehend (Tage)“ | #775 zieht geplante Tage ab (263 von 265: Rest #775 = Rest #757 heute − ausstehend); das Cockpit fuehrt Rest wie bisher mit geplanten Tagen |
| Ferien bezogen | „Ferien angetreten (Tage)“ | alte Formel Anspruch − Rest − ausstehend zog die geplanten Tage doppelt ab (3'287.5 statt 4'666 Tage) |
| Krankheit | „Krankheit angetreten (Stunden)“ + „Krankheit (über 60 Tage) angetreten (Stunden)“ | 1'208 + 27 Tage; „Krank nicht buchbar“ und „bis 3 Tage“ leer |
| Unfall | „Unfall angetreten (Stunden)“ | 132 Tage; „Unfall gebucht gesamt“ enthaelt auch Kuenftiges |
| Kompensation, Militaer | „… angetreten (Stunden)“ / 8,0 h (Kader 8,2 h) | |
| Juengste Faelle | „Krankheit …“, „Krankheit (über 60 Tage) …“, „Unfall angetreten (Zeitraum)“ | Ergaenzung fehlender SAP-Faelle wie 8.11, jetzt auch fuer Unfall (0290) |
| nicht genutzt | „Organisation/Stelle/Kostenstelle Austritt“ (enthalten ein Datum), „Arbeitszeitmodell-1“ (historischer Eintrag, bei 180 Vollzeit-Personen leer), Leitung, Lohn | FTE kommt aus SAP (alle Aktiven haben einen Grad), sonst aus der Sollzeit |

Rechnung: SAP PA2001 bleibt die Grundlage je Fall (Zeitraumschnitt, Kurz/Lang je Krankheit, Interaktiv). Rexx ist
fuehrend, SAP bekommt die Absenzen nur ueber Sonjas Import (8.9). Deckt der Zeitraum genau die Rexx-Summen ab (Beginn
01.01. des Exportjahres, Ende am oder nach dem Exporttag, also die Standardansicht), gilt **je Person der hoehere Wert**:
Krankheit und Unfall werden um die Differenz ohne einzelne Daten aufgestockt (`BuildRexxTopUpRows`, Langzeit hoechstens die
Differenz bei „ueber 60 Tage“), Ferien, Kompensation und Militaer ebenso in den Kacheln „… im Zeitraum“. Bei anderen
Zeitraeumen oder dem Vorjahr gelten nur die SAP-Faelle, mit Hinweis. Militaer zeigt, warum je Person: SAP und Rexx
fuehren am 04.11. ganz verschiedene Personen (3 in SAP, 7 in Rexx), vermutlich andere Arten.

Probe mit den echten Dateien (2026 bis 04.11., nur Kennzahlen), Einzeldateien → #775: Headcount 260, FTE 238,0,
Austritte 128 und Fluktuation unveraendert; Unfalltage 56,0 → 131,9; Krankheits- und Unfalltage 1'155,8 → 1'429,3;
Krankenquote 2,5 % → 3,0 % (ohne Langzeit 2,2 % → 2,8 %); bei 52 Personen hatte Rexx mehr (193,6 Tage Krankheit, 67,9
Tage Unfall ergaenzt), 2 juengste Unfaelle als Fall ergaenzt; Ferien bezogen 3'287,5 → 4'666; Kompensation im Zeitraum
503 → 1'236,5 Tage; Militaer 99 → 161 Tage.

Pflege: #775 regelmaessig mit **Zeitraum 01.01. bis heute** exportieren (der gespeicherte Filter steht fest auf
04.11.2026). Ist die Datei aelter als 7 Tage, zeigt das Cockpit einen Hinweis. Neue Felder nur gegen eine leere Spalte
tauschen (50er-Grenze). Grenzen: die Aufstockung hat keine Daten, erscheint also nicht im Krankheitskalender oder in
Fit & Wohl; Rexx-Arten ausserhalb „Krankheit“ (z. B. Kind krank) zaehlen nur, soweit SAP sie fuehrt.

Tests: 12 neue in `HrKpiServiceTests` (Gesamtdatei ersetzt die Einzeldateien, hoeherer Wert je Person, kein Rexx bei
anderem Zeitraum, Kurz/Lang-Teilung, Zeitraumregel, Rexx-Unfall, erste Organisation). Suite 1398/1398. **Produktiv seit
2026-11-04 11:46** (`6dd1a56`, `docs/rag/DEPLOYMENT.md`), #775-Datei seit 11:46 in `hrdata`; in Chrome gesehen: Krankheitstage
1'429.3, Absenzquote 3.0 % (gelb), Hinweise zu #775, Reiter Interaktiv mit Krankheitskalender bis November.


### 8.14 Rexx-Abwesenheiten je Fall #777: Interaktiv und Fit & Wohl mit Datum (2026-11-04)

Auftrag Ingo: „es soll alles gruen sein“ (nach 8.13 waren Krankheitskalender, Fit & Wohl und „Ferien am Stueck“ nicht
aktuell, weil sie Faelle mit Datum brauchen und SAP Ferien nur bis 27.07. hatte). In Rexx (Start › Antragsliste) angelegt:
private Abfrage **#777 „HR-Cockpit Absenzen je Fall (Dashboard)“**, Kopie der Standardabfrage #122 plus die Spalten
Personalnummer, „Abwesenheit von/bis/Tage/halbe Tage/Status/Buchung/ID Buchung/Zeit“ und „Abwesenheit Tagessoll“ (= Sollzeit
des ganzen Falls in hh:mm, z. B. 40:00 fuer eine Woche, 6:24 je Tag bei Teilzeit). Export `Hr-cockpit-absenzen-je-fall.xlsx`,
2'683 Antraege ab 07.04.2026 (Start des Rexx-Antragswesens): Ferien 1'005, Kompensation 535, Krankheit 441, Unfall 60,
Krankheit ueber 60 Tage 7, Militaer 9 (dazu Weiterbildung, Dienstreise u. a., die das Cockpit nicht fuehrt).

Rechnung (`MergeRexxCases`): bestaetigte Faelle (Status „Genommen“, „Genehmigt“; nicht storniert, abgelehnt, geplant,
Korrektur) der Arten Ferien → 0100, Kompensation → 0400, Militaer → 0600, Krankheit → 0260, Krankheit ueber 60 Tage → 0270,
Unfall → 0290 kommen zu den SAP-Faellen, soweit kein SAP-Fall derselben Gruppe (Krankheit und Unfall eine Gruppe) dieselben
Tage abdeckt; bei teilweiser Ueberschneidung nur die fehlenden Tage, Stunden anteilig nach Arbeitstagen. Stunden = Rexx
„Tagessoll“, sonst Tage x 8,0/8,2 h x Pensum. Folgenummer `REXX`. Damit rechnen Zeitraumschnitt, Kurz/Lang, Krankheitskalender,
Fit & Wohl, „Ferien am Stueck“ und die Kacheln „… im Zeitraum“ mit Datum; die Warnung „SAP-Ferien bis 27.07.“ entfaellt.
Die Aufstockung aus #775 (8.13) bleibt als Rest ohne Datum.

Probe mit den echten Dateien (04.11., nur Kennzahlen), vorher → nachher: 1'203 Faelle ergaenzt (Ferien 730, Kompensation 355,
Krankheit 81, Krankheit ueber 60 Tage 6, Unfall 24, Militaer 7); juengster Ferien- und Kompensationsfall 27./29.07. → 04.11.;
„Ferien am Stueck“ (mindestens 10 Tage bezogen) 55 → 101 Personen; Fit & Wohl 28 → 31; Quote 3,0 % → 3,1 %, ohne Langzeit
2,8 % → 2,7 %; Rest ohne Datum 193,6 + 67,9 Tage bei 52 Personen → 122,6 + 11,0 Tage bei 28 Personen.

**Grenze, offen:** Rund 100 Krankheitstage bei 13 Personen stehen in Rexx, aber weder in SAP noch in der Antragsliste (HR
bucht sie direkt; eine Person allein 49,5 Tage mit 19 Buchungen, in SAP nur 10 Tage im Februar). Diese Tage zaehlen in Quote
und Summen, haben aber kein Datum, fehlen also im Krankheitskalender und koennen Fit & Wohl verfehlen. Versucht und verworfen:
#775-Kopie #776 mit Zeitraum je Monat (der Filter begrenzt „angetreten“ nicht, jeder Monat zeigte die Jahressumme) und der
iCal-Link des Rexx-Kalenders (geheimer Zugangsschluessel, nicht verwendet). Loesung: Rexx-Schnittstelle oder Nachbuchung in SAP;
Frage an Sonja, warum diese Faelle nicht importiert sind.

Pflege: #777 wie #775 regelmaessig exportieren (kein Datumsfilter, liefert alle Antraege). Rexx-Abfragen von Claude: #775,
#776 (Kopie von #775, ungenutzt, kann geloescht werden), #777; fremde Abfragen unveraendert.

Tests: 3 neue (`MergeRexxCases` bestaetigte Faelle ohne Ueberschneidung, teilweise Ueberschneidung, Einbau in Absenzen und
Interaktiv). Suite 1401/1401.

## 9. Reiter Interaktiv (2026-11-04)

Wunsch Ingo: „baue einen Interaktiv-Reiter analog verkauf/interaktiv, halt fuer HR und seine Anforderungen, z. B.
animiert Kuendigungen, Verhalten“. Reiter „Interaktiv“ im HR-Cockpit (hinter dem HR-Login, nutzt die Filter der Seite),
eigene Reiterleiste mit acht Ansichten. Rechnung `Services/HrKpi/HrKpiInteractive.cs` (Tests `HrKpiInteractiveTests`),
Darstellung `Components/HrKpi/Interactive/*View.razor`, CSS `six-` (vom Verkauf) und `hri-`.

| Ansicht | Was und wie |
| --- | --- |
| Kuendigungen im Zeitraffer | Abteilungen nach Austritten der letzten zwoelf Monate (gleitend, distinct je Person), je Monat ein Bild, hoechstens 60; Schalter nur fluktuationsrelevante; fester Massstab ueber alle Monate |
| Austrittsfluss | Sankey Austritte der Auswahl → Grund (wie die Fluktuation einordnet) → Verweildauer (unter 1, 1–3, 3–5, 5–10, ueber 10 Jahre, unbekannt); darunter relevante Austritte je Kalendermonat ueber alle Jahre |
| Abteilungs-Galaxie | 14 groesste Abteilungen je Quartal (12 Quartale): x Headcount Festangestellte am Quartalsende, y relevante Austritte 12 Monate / Mittel aus fuenf Quartalsend-Headcounts, Groesse alle Austritte; blass, wenn das Fenster vor den Austrittsdaten beginnt |
| Bindungskurve | Kaplan-Meier je Eintrittsjahr (bis 6 Jahrgaenge, 60 Monate); Ereignis = Austritt, mit Schalter nur relevante (andere zensiert); Jahrgaenge vor Beginn der Austrittsdatei fehlen |
| Krankheitskalender | krankgemeldete Personen je Arbeitstag ZH aus SAP-Krankheitsarten; Wochentag des Beginns kurzer Krankheiten (hoechstens 3 Arbeitstage), Bezug 20 % |
| Fit & Wohl | Wunsch Sonja (8.3): Gespraech nach der 6. Absenz (Beginn im Jahr) oder 12 Krankheitstagen (ABWTG anteilig bis heute); Faelle ohne Arbeitstag dazwischen = eine Absenz; Datum = Beginn der ausloesenden Absenz; ▶ spielt das Jahr ab |
| Ferien am Stueck | Wunsch Sonja (8.3): laengster Block aus Art 0100 ohne Arbeitstag dazwischen, mindestens 10 Ferientage; bezogen = Block bis heute vorbei, Schalter mit geplanten; Anteil je Abteilung und Liste ohne |
| Was waere wenn | Regler Kuendigungen, sonstige Austritte, Eintritte je Monat (Vorschlag: Mittel der letzten 12 Monate); Headcount fortgeschrieben, Jahresfluktuation = (Ist + Regler x Restmonate) / Mittel der 12 Monats-Headcounts, gegen Vorjahr |

Daten: `HrKpiResult.TimelineLeavers` (Austritte aller Jahre, nur Strukturfilter wie der Vorjahresvergleich),
`TimelineEmployees` (aktive Personen mit denselben Filtern wie die Fluktuation, also ohne Kostenstelle, GLZ und
Restferien; Abschnitt 6, Test `BuildAsync_Zeitreihen_Des_Reiters_Interaktiv_Ignorieren_Kostenstelle_GLZ_Und_Restferien`),
`AbsenceCases` (SAP-Faelle der gefilterten aktiven Personen), `AnchorDate` (Stichtag der Fluktuation). Galaxie,
Bindungskurve und Simulator nehmen den Headcount aus `TimelineEmployees`; Krankheitskalender, Fit & Wohl und Ferien sind
Personensichten und folgen allen Filtern.
Fit & Wohl und Ferien zeigen Namen, in der Managementsicht nur die Personalnummer.

Grenzen: aktive Personen zaehlen rueckwirkend in ihrer heutigen Abteilung; Ferien hinken mit Sonjas Rexx-Import (8.9);
ob ein Fit-&-Wohl-Gespraech stattfand, kennt das Cockpit nicht (Ruecklauf aus 8.3 nicht gebaut); der Simulator ist
eine Spielwiese.

*Ueberholt am 2026-11-04 09:20:* ~~committet, Tests gruen, nicht deployed~~. **Produktiv seit 2026-11-04 09:20**
(`b65f059`, `docs/rag/DEPLOYMENT.md`); *Sichtpruefung im Browser am 2026-11-04 11:50 erledigt (Zeitraffer, Krankheitskalender).*

## Querverweise

- Kurzstand, Zugang und Anwenderdoku: `docs/rag/HR_KPI.md`
- Fachpruefung Schweizer Praxis: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`
- Anwenderdoku HR: `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
