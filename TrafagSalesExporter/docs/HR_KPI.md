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
| 2 | Kurz- gegen Langzeitkrankheit | Die Rexx-Arten nicht unterscheiden, „Krankheit" ziehen; ab dem **61. Krankheitstag** gilt die Krankheit als Langzeitkrankheit | **Geaendert.** Beide Rexx-Felder zaehlen als Krankheit (Entscheid Ingo). Kurz/lang je Person nach `HrKpiDashboardBuilder.ClassifySickness`: Summe der Krankheitstage im Export (Stunden / 8,4) ab 61 = langzeitkrank, dann zaehlt die ganze Krankheit als lang. Rexx liefert keine einzelnen Faelle, deshalb die Summe (Entscheid Ingo). Kachel „Krankheit Lang" nennt die Anzahl Langzeitkranke |
| 3 | FTE-Fallback 0,5 | FTE ist bei echten Mitarbeitenden nie leer und entspricht der Sollzeit (0,8 = 32 h/Woche); leer nur bei Reminderprofilen wie ICT | **Geaendert.** Ohne SAP-Beschaeftigungsgrad **und** ohne Rexx-Sollzeit gilt eine Zeile als Reminderprofil und faellt aus allen Kennzahlen (Entscheid Ingo, `IstReminderprofil`). Nur wenn die Zeitdatei die Person gefunden hat: ein fehlgeschlagener Namens-Join schliesst niemanden aus. Fehlt nur SAP, gilt FTE = Sollzeit / 8,4 h; das FTE ist auf 0,1 bis 1,2 begrenzt. Die bekannten Profile „ICT Trafag" und „Empfaenger Reminder" standen schon in der Ausschlussliste |
| 4 | GLZ-Ampel 50/100 h | Korrekt, positiv und negativ gleich, auch bei Teilzeit | unveraendert |
| 5 | Restferien-Ampel | Q1: bis 5 Tage gruen, mehr rot. Ab Q2: jeder Resttag rot | **Geaendert**, `ResolveRestferienAmpel`, massgeblich ist das Quartal des heutigen Tags (die Saldidatei ist ein Stichtagsstand) |
| 6 | Prognose Quartalsrate x 4 | Beibehalten, zusaetzlich gleitender Durchschnitt und Vorjahresvergleich | **Ergaenzt**: Kacheln „Fluktuation Prognose gleitend" und „Fluktuation Vorjahr", beide aus der nur strukturgefilterten Austrittsliste wie der Vorjahresvergleich (H1) |
| 7 | Definition fluktuationsrelevant | Stimmt | unveraendert |
| 8 | Kuendigungserkennung | Nur „Kuendigung AN" fliesst in die Fluktuation | unveraendert, die Erkennung deckt `Kündigung AN` und `Kuendigung AN` ab |
| 9 | Ausschluss Praktikanten, Werkstudenten, Aushilfen, Lehrlinge | Korrekt, keine Ausnahmen | unveraendert |

*Ueberholt am 2026-09-30:* ~~Offen bleibt die **8,4-Stunden-Umrechnung** je Krankheitstag; danach wurde nicht gefragt.~~
Entschieden, 8,0 h je Tag, siehe Abschnitt 8.4. Die Stunden in der Tabelle oben (Stunden / 8,4)
gelten seither mit 8,0.

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

### 8.3 Offene Wuensche und Faeden aus der Mailhistorie (gesichtet 2026-09-29)

Beim Durchsehen der Mails und Teams-Chats mit Sonja Richter und Nadja Brandenberger gefunden. Nichts
davon war bisher hier dokumentiert.

**Gewuenscht (Sonja, Mail vom 2026-02-03, Bereich Zeit- und Absenzwesen), weder umgesetzt noch
beauftragt:**

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
  werden. Pfad und Berechtigung klaert Ingo mit Ueli. **Offen:** Antwort Ueli; ob der von Nadja
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
| Arbeitstag | **Kadermitarbeitende 8,1 h, normale Mitarbeitende 8,0 h** | seit 30.09. einheitlich 8,0 h (8.4) | Kaderkennzeichen fehlt in allen Dateien. Kandidaten: SAP `Mitarbeiterkreis` (Monatszeilen 10=55, 15=754, 19=18, 20=308, 30=4; welcher Wert Kader ist, weiss nur HR) oder Rexx `Leitung j/n`. Rexx `Ø taegliche Sollarbeitszeit (Woche)` hilft nicht (Werte wie 5,71 = 40 h / 7 Tage). *Erledigt am 2026-10-01 in `c51445c` (noch nicht deployed):* Ingo bestimmt Rexx **`Leitung j/n` = ja** als Kaderkennzeichen (gemessen in den Dateien vom 26.05. in `C:	emp`: `Abwesenheitinstunden.xlsx` und `Saldiperstichdatum.xlsx` je 44 ja / 218 nein, `Personalausgeschieden.xlsx` 108 nein, `Exportkommengehen.xlsx` ohne die Spalte). `HoursPerWorkdayFor` gibt dann 8,1 h, sonst 8,0 h; gilt fuer Krankheitstage, 61-Tage-Grenze und FTE aus der Sollzeit. Tests `Kader_Mit_Leitung_Ja_Hat_8_1_Stunden` und `BuildAsync_Rechnet_Krankheitstage_Fuer_Kader_Mit_8_1_Stunden`, 800/800. Hinweis: in Rexx kann eine Person der Geschaeftsfuehrung `Leitung = nein` haben; ob jeder Kader so gepflegt ist, liegt bei HR |
| Abwesenheiten | In Rexx wird jeder Fall mit **Von/Bis** erfasst und hat einen **Status** (genehmigt / nicht genehmigt) | `Abwesenheitinstunden.xlsx` ist eine Zeile je Person mit Summen; auch die Spalten „genehmigt" sind Summen | Neuer Rexx-Bericht **eine Zeile je Fall** (Person, Art, Von, Bis, Stunden, Status) als fuenfte Datei. Macht die Krankenquote periodengenau und erlaubt „nur genehmigt". Beispieldatei von Sonja noetig |
| Absenz-Ampel | **bis 4,99 % gelb, ab 5 % rot** | Krankenquote gruen < 3 %, gelb 3 bis < 5 %, rot ab 5 % (`HrKpi:Absence*ThresholdPercent`): passt *Erledigt am 2026-09-30 in `81140fa` (noch nicht deployed):* Die Uebersichtskachel „Krankheitstage" warnte erst **ueber** fest verdrahteten 5 % (`absenceRate > 0.05m`); jetzt ab der konfigurierten Rot-Grenze (`ReachesAbsenceRed`, `>=`), Test `Absenzampel_Ist_Ab_Fuenf_Prozent_Rot`, 791/791. Gruen unter 3 % hat HR nicht erwaehnt, gilt als unveraendert |

### 8.6 SAP-Datei `HR_KPI_Export.xlsx` ist nicht automatisiert (Stand 2026-09-30)

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
| Transport | DPC_EXT und MPC_EXT sind durch `T76K912530` (Journal, nicht freigegeben) gesperrt; HR geht damit **zusammen mit dem Journal** nach P76 (Entscheid Ingo fuer diesen Service) |
| Berechtigung | Der Data Provider liest PA-Tabellen per `SELECT`, ohne `P_ORGIN`-Pruefung: wer `ZPOWERBI_EINKAUF_SRV` lesen darf, sieht diese Felder |
| Cockpit-Leser und Tagesabruf | `Services/HrKpi/SapGatewayHrKpiReader.cs` (`4051506`), nur Produktion, ab 05:00 einmal taeglich, Verbindung wie der Einkauf. 795/795 Tests, darunter der Rundweg Datei schreiben und mit dem echten HR-Leser lesen. **Nicht deployed** |

**Gemessen im Gateway Client T76, 2026-09-30** (Antwortrumpf nicht lesbar, nur Status und Laenge): `$metadata` 200 (342'123 Zeichen statt 341'032); `HrKpiSet` `$top=5` 200 in 0,7 s; Filter 2026/09 Seite 1 200 (560'915), Seite 2 mit `$skip=1000` 200 (84'670), Monat 13 200 mit leerer Liste; Gegenproben `MAKTSet` und `FinanzJournalSet` 200. **Die Werte selbst sind noch nicht zeilenweise gegen den Report gegengelesen.**

**Offen bis produktiv:** (1) Stichprobe der Werte gegen `Z_HR_KPI_CONS` fuer 09/2026 (Screenshot oder Browser mit Sitzung); (2) Transport `T76K912530` nach P76, zusammen mit dem Journal, Freigabe Ingo; (3) Deploy des Cockpits (bis zum Transport meldet der Abruf taeglich einen 404 und laesst die Handdatei stehen).

Rexx-Berichtsnummern aus denselben Notizen, fuer die Anfrage bei Upgreat: Abwesenheit in Stunden
`#744`, Export KOMMEN/GEHEN `#732`, Personal ausgeschieden `#381`, Abwesenheiten Uebersicht `#742`,
Stammdaten SAP `#735`.

## Querverweise

- Kurzstand, Zugang und Anwenderdoku: `docs/rag/HR_KPI.md`
- Fachpruefung Schweizer Praxis: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`
- Anwenderdoku HR: `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
