# Architekturreview: Static-Methoden und Hardcodings

Stand: 2026-05-15

## Ergebnis

Viele `static`-Methoden sind im aktuellen Code nicht automatisch falsch. Reine Hilfsfunktionen ohne Zustand sind als `static` fachlich und technisch akzeptabel.

Das eigentliche Architekturthema ist nicht `static` selbst, sondern dass einige grosse Klassen fachliche Regeln, Datenimport, Filterung, KPI-Berechnung und Visualisierungsvorbereitung gleichzeitig enthalten.

## Befunde

| Prioritaet | Bereich | Befund | Empfehlung |
| --- | --- | --- | --- |
| Medium | HR KPI | Testpersonen sind aktuell im Code ausgeschlossen. | In `appsettings.json` oder DB-Tabelle `HrKpiExclusionRules` verschieben. |
| Medium | Finance Vergleich | Vergleich ist aktuell auf Jahr `2025` und Referenztext `check.xlsx / Power BI Stand 29.04.2026` fixiert. | Jahr auswählbar machen und Referenzstand aus Daten/Konfiguration lesen. |
| Medium | Finance Reconciliation | Hauswaehrung je Land wird im Service aufgelöst. | Langfristig in Standort-/Finance-Konfiguration verschieben. |
| Low/Medium | Database Seed | Finance-Sollwerte, Budgetkurse und IC-Defaultregeln werden per Seed angelegt. | Fuer Produktion Import/Pflegeoberflaeche vorsehen. |
| Low | UI/Formatierung | Viele kleine `static`-Formatierungs- und Mappingmethoden. | Akzeptabel, solange sie klein und zustandslos bleiben. |

## Grosse Klassen

| Klasse | Umfang | Bewertung |
| --- | ---: | --- |
| `Services/HrKpi/HrKpiDashboardBuilder.cs` | ca. 1'145 Zeilen | Zu viel Verantwortung in einer Klasse. |
| `Services/ManagementCockpitService.cs` | ca. 811 Zeilen | Analyse, Import, Aggregation und Hinweise liegen stark gebündelt. |
| `Services/FinanceReconciliationService.cs` | ca. 370 Zeilen | Noch akzeptabel, aber fachliche Teilregeln koennen spaeter ausgelagert werden. |

## Was korrekt ist

Diese Arten von `static`-Methoden sind unkritisch:

- Textnormalisierung
- Datum-/Zahlenformatierung
- kleine Parser
- einfache Mappingfunktionen
- lokale UI-Formatierung
- deterministische Berechnung ohne externe Abhängigkeiten

## Was verbessert werden sollte

1. HR-Testpersonen aus dem Code in Konfiguration oder DB verschieben.
2. Finance-Vergleich von fixem Jahr `2025` auf auswählbares Jahr umstellen.
3. Referenztext und Referenzstand nicht hart im UI pflegen.
4. Hauswaehrungen je Land aus Konfiguration oder Finance-Stammdaten lesen.
5. Grosse Klassen schrittweise aufteilen:
   - Reader
   - Filter
   - Rules
   - Metrics
   - VisualBuilder

## Empfehlung

Nicht alle `static`-Methoden entfernen. Das waere kein sinnvoller Refactor.

Zuerst sollten die fachlich veraenderbaren Regeln aus dem Code herausgezogen werden. Danach kann die Klassenstruktur gezielt verkleinert werden.

