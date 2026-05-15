# HR KPI Nachdokumentation 2026-05-13

## Ziel

Das HR KPI Cockpit wurde als separater, fachlich entkoppelter Reiter umgesetzt. Es nutzt PowerBI-M-/DAX-Logik nicht als generischen Interpreter, sondern als fachliche Vorlage, die in nachvollziehbare C#-Logik uebertragen wurde.

Der Reiter ist vom Finance-/Management-Cockpit getrennt. Er nutzt nur gemeinsame technische Infrastruktur wie Blazor, MudBlazor, DI, ClosedXML und bestehende Programmstruktur.

## Eingebaute HR KPI Funktion

Neue Navigation:

- `HR KPI` im Hauptmenue.
- Route: `/hr-kpi`.

Neue zentrale Dateien:

- `Components/Pages/HrKpi.razor`
- `Components/HrKpi/HrKpiDashboardTabs.razor`
- `Models/HrKpiModels.cs`
- `Services/HrKpiService.cs`
- `Services/HrKpi/HrKpiDashboardBuilder.cs`
- `TrafagSalesExporter.Tests/HrKpiServiceTests.cs`

## Datenquellen

Standard-Datenordner:

```text
C:\temp
```

Konfigurierbar ueber `appsettings.json`:

```json
"HrKpi": {
  "DataFolder": "C:\\temp",
  "MainFile": "Saldiperstichdatum.xlsx",
  "TimeFile": "Exportkommengehen.xlsx",
  "SapFile": "HR_KPI_Export.xlsx",
  "AbsenceFile": "Abwesenheitinstunden.xlsx",
  "LeaverFile": "Personalausgeschieden.xlsx"
}
```

Verarbeitete Dateien:

- `Saldiperstichdatum.xlsx`: aktive Mitarbeitende, Saldi, Ferien, Organisation, Kostenstelle.
- `Exportkommengehen.xlsx`: Arbeitszeitmodell, Sollzeit, Geburtsdatum.
- `HR_KPI_Export.xlsx`: SAP-HR-Felder wie Beschaeftigungsgrad, Geschlecht, BU/NBU, Planstelle.
- `Abwesenheitinstunden.xlsx`: Krankheit kurz/lang in Stunden.
- `Personalausgeschieden.xlsx`: Austritte, Austrittsart, Austrittsdatum.

## Dashboard-Reiter

Das Cockpit zeigt folgende Tabs:

- `Ueberblick`
- `Fluktuation`
- `Absenzen`
- `Zeit / Ferien`
- `Mitarbeitende`
- `Datenstatus`

Im Fluktuationsbereich wurden zusaetzliche Visualisierungen ergaenzt:

- Jahres-Fluktuations-Gauge
- Austritts-Funnel
- Donut nach Ausschlussgruenden
- relevante Austritte nach Organisation
- relevante Austritte pro Monat

## Filter

Aktuell vorhandene Filter:

- Datenordner
- Austrittsjahr
- Von Austritt
- Bis Austritt
- Organisation
- Eintrittsjahr
- Suche Name / Personalnummer
- Kostenstelle
- Mitarbeitertyp
- Fluktuation
- GLZ-Ampel
- Restferien-Ampel

## Korrektur Austrittsjahr / Von-Bis

Problem:

- `Austrittsjahr` war als `int` modelliert.
- Dadurch war immer ein Jahr gesetzt.
- Leeren bzw. "alle Austrittsjahre" war nicht moeglich.
- Aus Sicht UI wirkte es so, als ob leere Auswahl nicht uebernommen wird.

Umsetzung:

- `HrKpiOptions.Year` wurde von `int` auf `int?` geaendert.
- `Austrittsjahr` ist in der UI jetzt ein `MudSelect<int?>` mit `Clearable`.
- Die Jahresauswahl wird aus den vorhandenen Austrittsdaten gebaut.
- Neues Result-Feld: `ExitYearOptions`.
- Wenn `Austrittsjahr` leer ist, werden alle Austrittsjahre geladen.
- Wenn `Von Austritt` oder `Bis Austritt` gesetzt ist, hat dieser Zeitraum Vorrang vor `Austrittsjahr`.

Regel:

```text
Von/Bis gesetzt -> Austrittsdatum muss im Zeitraum liegen
Von/Bis leer und Austrittsjahr gesetzt -> Austrittsjahr muss passen
Von/Bis leer und Austrittsjahr leer -> alle Austritte
```

Nach der Architektur-/Formelpruefung wurde zusaetzlich korrigiert:

- `Austrittsjahr` ist auch beim Start leer und wird nicht mehr automatisch mit dem aktuellen Kalenderjahr vorbelegt.
- Bei leerem Austrittsjahr werden keine Jahres-/Quartals-/Monats-Fluktuationskennzahlen als Jahreswerte vorgetaeuscht; die Anzeige wird als `Fluktuation Auswahl` gefuehrt.
- Bei Von/Bis oder Mehrjahresauswahl zeigt die Timeline Jahresgruppen, wenn kein eindeutiges einzelnes Auswertungsjahr vorliegt.
- Die Fluktuationsberechnung nutzt fuer Mitarbeitendenfilter nur Felder, die in Mitarbeitendenbestand und Austrittsdaten vergleichbar sind: Organisation, Mitarbeitertyp, Eintrittsjahr und Suche.
- Kostenstelle, GLZ und Restferien filtern aktive Mitarbeitende/Absenzen, aber nicht Fluktuation, weil die Austrittsdatei diese Felder nicht stabil enthaelt. Das Cockpit weist darauf hin.
- Fluktuationsvisuals zaehlen Austritte distinct nach Personalnummer statt Zeilen.
- Fluktuationsraten nutzen Headcount, nicht FTE.
- `Headcount Monat` wird als Durchschnitt aus Monatsanfang und Monatsende berechnet.
- `Avg Headcount Quartal` ist der Durchschnitt der Monats-Headcounts im Quartal.
- `Avg Headcount Jahr` ist der Durchschnitt der Monats-Headcounts im Jahr.
- `Headcount nach Organisation` zaehlt Personalnummern distinct und ignoriert leere Personalnummern.
- Krankenquote nutzt neu `Krankheitstage / (FTE * 21 Tage)` statt `Krankheitstage / (Headcount * 21 Tage)`.

## Fluktuationslogik

Die Fluktuation wird aus den ausgeschiedenen Personen berechnet.

Grundlage gemaess `formeln.docx`:

- Monat: Arbeitnehmerkuendigungen des jeweiligen Monats / Headcount des Monats.
- Quartal: Arbeitnehmerkuendigungen des aktuellen Quartals / durchschnittlicher Headcount des Quartals.
- Hochrechnung Jahr: aktuelle Quartals-Fluktuation x 4.
- Effektiv Jahr: Arbeitnehmerkuendigungen des gesamten Jahres / durchschnittlicher Headcount des Jahres.
- Nenner ist Headcount der Festangestellten, nicht FTE.

Relevant ist ein Austritt, wenn:

- Austrittsart als Arbeitnehmer-/Mitarbeiterkuendigung erkannt wird.
- Mitarbeitertyp nicht ausgeschlossen ist.
- Austrittsgrund nicht als befristet, Pensionierung, Arbeitgeberkuendigung oder anderer Ausschlussgrund erkannt wird.

Ausgeschlossen werden unter anderem:

- Praktikant
- Werkstudent
- Aushilfe
- Lehrling
- befristeter Vertrag
- Pensionierung/Rente
- Kuendigung durch Trafag/Arbeitgeber

Zusaetzlich korrigiert:

- Rexx-Werte mit Umlaut wie `Kuendigung AN` werden trotz Originalschreibweise `Kündigung AN` als Arbeitnehmerkuendigung erkannt.
- `Kuendigung AG` bleibt als Arbeitgeberkuendigung ausgeschlossen.
- `Ruhestand` wird als Pensionierung ausgeschlossen.

## Architektur-Cleanup

Vorher:

- `HrKpiService` enthielt Import, Mapping, Filter, KPI-Berechnung, Visual-Daten und Excel-Parsing in einer grossen Klasse.
- `HrKpi.razor` enthielt Route, Filter, alle Tabs, Tabellen, Visualisierungen und CSS.

Nachher:

- `HrKpiService.cs` ist nur noch DI-/Service-Fassade.
- `HrKpiDashboardBuilder.cs` enthaelt die Build-Pipeline fuer Import, Mapping, Filter und KPI-Berechnung.
- `HrKpi.razor` bleibt fuer Route, Filter und Laden zustaendig.
- `HrKpiDashboardTabs.razor` enthaelt die Tabs, Tabellen, Fluktuationsvisuals und Styles.
- HR-Datenquellen sind ueber `HrKpiDataSourceOptions` konfigurierbar.

## Tests

Neue HR-KPI-Regressionstests:

- Organisation-Filter wirkt auch auf Absenzen.
- Von/Bis-Austrittsdatum hat Vorrang vor Austrittsjahr.
- Leeres Austrittsjahr liefert Austritte aus allen Jahren.
- Austrittsjahr ist standardmaessig leer.
- Employee-only Filter verzerren die Fluktuationsbasis nicht.
- Fluktuationsvisuals zaehlen distinct nach Personalnummer.
- Rexx-Austrittsarten `Kündigung AN`, `Kündigung AG` und `Ruhestand` werden korrekt klassifiziert.
- Fluktuationsraten verwenden durchschnittlichen Headcount statt aktuellen Stichtagsbestand.
- Mitarbeitende ohne Personalnummer werden nicht im Distinct-Headcount gezaehlt.
- FTE-Fallback aus Arbeitszeitmodell/Sollzeit wird verwendet, wenn SAP-Beschaeftigungsgrad fehlt.
- Fluktuationsrelevanz und Visual-Daten werden klassifiziert.

Aktueller Teststand nach der Korrektur:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore -p:UseAppHost=false --verbosity minimal
```

Ergebnis:

- Build erfolgreich.
- Tests erfolgreich: `69/69`.

Kontrollwert aus `C:\temp\Personalausgeschieden.xlsx`:

- Austritte total: `104`
- `Kündigung AN`: `42`
- `Kündigung AG`: `34`
- Fluktuationsrelevant nach aktueller HR-Logik: `33`
- Avg Headcount 2025 nach Intervalllogik: `211.3`
- Fluktuation Jahr effektiv 2025: `15.6%`

## Offene fachliche Pruefpunkte

Diese Punkte sind nicht automatisch geloest und muessen fachlich von HR bestaetigt werden:

- Ob die Abgrenzung "fluktuationsrelevant" exakt der Trafag-HR-Definition entspricht.
- Ob Arbeitnehmerkuendigungen anhand der vorhandenen Austrittsart-Texte vollstaendig erkannt werden.
- Ob Praktikanten, Werkstudenten, Aushilfen und Lehrlinge immer aus der Fluktuation ausgeschlossen werden sollen.
- Ob FTE-Fallback bei fehlendem SAP-Beschaeftigungsgrad fachlich akzeptiert ist.
- Ob `8.4 Stunden = 1 Krankheitstag` als Standardumrechnung fuer alle relevanten Gruppen korrekt ist.
- Ob GLZ- und Restferien-Ampeln mit den internen HR-Grenzwerten uebereinstimmen.

## Commit-Stand

Bereits erstellt:

- `20be752 Add HR KPI cockpit`
- `1cd0ad9 Refactor HR KPI cockpit architecture`
- `001e2a7 Commit pending finance and Power BI work`

Noch nicht committed zum Zeitpunkt dieser Nachdoku:

- Korrektur `Austrittsjahr` optional / Von-Bis Vorrang.
- Diese Nachdokumentation.
