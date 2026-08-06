# HR KPI: Zuercher Feiertage und Filtervertrag

Stand: 2026-08-06

Status: **produktiv deployed und technisch verifiziert am 2026-08-06,
14:24 MESZ**, Funktionscommit `9435a5d`, Gesamtsuite `438/438` gruen.

## Umsetzung

Die Arbeitstage im Nenner der Krankenquote sind nicht mehr nur Montag bis
Freitag. `ZurichWorkdayCalendar` zieht die neun gesetzlichen, den Sonntagen
gleichgestellten Feiertage des Kantons Zuerich ab:

- Neujahr;
- Karfreitag und Ostermontag;
- Tag der Arbeit (1. Mai);
- Auffahrt und Pfingstmontag;
- Bundesfeiertag (1. August);
- Weihnachten und Stephanstag.

Bewegliche Feiertage werden fuer jedes Jahr aus dem gregorianischen
Ostersonntag berechnet. Ein Feiertag reduziert den Nenner nur, wenn er auf einen
Wochentag faellt. Berchtoldstag und lokale Feiertage sind nicht automatisch
enthalten, weil sie im Kanton Zuerich nicht allgemein gesetzlich sind.

Fachquelle: Kanton Zuerich, `Feiertage`:
https://www.zh.ch/de/wirtschaft-arbeit/arbeitsbedingungen/arbeitsssicherheit-gesundheitsschutz/arbeits-ruhezeiten/feiertage.html

## Zusaetzliche Korrektur der Krankenquote

Die Rexx-Datei `Abwesenheitinstunden.xlsx` besitzt im produktiven Format keine
verlaesslichen Datumsfelder fuer die kumulierten Krankheitsstunden. Bei einem
Jahres-/Datumsfilter kann der Zaehler daher nicht sicher auf denselben Zeitraum
wie der Arbeitstage-Nenner eingegrenzt werden.

Die Detailkachel zeigte deshalb bereits `Zeitraum nicht bestimmbar`. Neu gilt
dies auch in der Uebersicht und in der Ampel:

- keine scheinbar genaue Prozentzahl in der Uebersicht;
- Ampel gelb statt einer aus unzuverlaessiger Quote abgeleiteten roten oder
  gruenen Bewertung;
- Krankheitstage bleiben sichtbar, aber mit Warnstatus.

## Automatischer Filtervertrag

Der Regressionstest
`BuildAsync_All_128_Global_Filter_Combinations_Keep_Every_Visible_Block_Consistent`
prueft alle 128 Ein-/Aus-Kombinationen dieser sieben personenbezogenen Filter:

1. Organisation;
2. Kostenstelle;
3. Mitarbeitertyp;
4. Eintrittsjahr;
5. GLZ-Ampel;
6. Restferien-Ampel;
7. Suche.

Je Kombination werden Mitarbeitende, Absenzen, Austritte, Uebersichts-KPIs,
Fluktuations-KPIs, Absenz-KPIs, Zeit/Ferien, Periodenvergleich, Ampeln,
Organisationsgruppen, kritische Listen und Fluktuationsvisuals auf gemeinsame
Konsistenz geprueft.

Ein zweiter Test kombiniert gleichzeitig Austrittsjahr, Von/Bis,
Fluktuationsfilter und alle sieben Personenfilter. Er pinnt auch die bestehende
fachliche Abgrenzung: Kostenstelle, GLZ und Restferien filtern die
Mitarbeitenden-/Absenzsicht, aber nicht die Fluktuation, weil diese Felder in
der Austrittsdatei nicht stabil vorhanden sind. Von/Bis hat Vorrang vor dem
Austrittsjahr.

## Abnahmegrenze

Die Tests beweisen die technische Filterkonsistenz innerhalb der vorhandenen
Quelldaten. Sie koennen fehlende Quellfelder nicht ersetzen. Eine echte
periodengenaue Krankenquote bleibt erst moeglich, wenn Rexx die kumulierten
Krankheitsstunden mit einem belastbaren Bezugszeitraum oder datierten
Einzelereignissen liefert.

## Deployment-Nachweis

- `BiDashboard.dll`: `4'137'472` Bytes, Zeitstempel `06.08.2026 14:24:10`;
- SHA256:
  `B8391FBFC69DBB6B45F93D1D6AF3D8FC621C34FD11405C14A0E52BF98397B7B0`;
- Startseite HTTPS `200` (`64'740` Bytes);
- `/BiDashboard/hr-kpi` HTTPS `200` (`65'874` Bytes);
- `app_offline.htm` nach dem Publish nicht mehr aktiv.
