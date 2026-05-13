# Fluktuation Nachdokumentation - 2026-05-12

## Ausgangslage

Die Fluktuationsformeln aus `formeln.docx` sollten in die Power-BI-Logik uebernommen werden.

Fachliche Definition laut HR:

- Zaehler: nur Arbeitnehmerkuendigungen
- Nicht zaehlen: befristete Vertraege, Aushilfen, Pensionierungen und Kuendigungen durch Trafag
- Nenner: durchschnittlicher Headcount, nicht FTE
- Monat: Austritte des Monats / Headcount des Monats
- Quartal: Austritte des Quartals / durchschnittlicher Headcount des Quartals
- Jahreshochrechnung: aktuelle Quartals-Fluktuation x 4
- Effektives Jahr: Austritte des Jahres / durchschnittlicher Headcount des Jahres

## Geaenderte Dateien

### `rexx_ausgeschieden.txt`

Die bestehende Power-Query fuer `C:\temp\Personalausgeschieden.xlsx` wurde erweitert.

Neu bzw. angepasst:

- robuste Umwandlung von `Austrittsdatum` und `Eintrittsdatum`
  - Date
  - DateTime
  - Excel-Seriennummer, z.B. `45396.0`
  - Text im Format `dd.MM.yyyy`
- Normalisierung von `Austrittsart`
  - Kleinbuchstaben
  - Umlaute nach ASCII, z.B. `kuendigung`
- neue fachliche Spalten:
  - `Austrittsart_Normalisiert`
  - `Ist_Arbeitnehmerkuendigung`
  - `Ist_Fluktuation_Ausgeschlossen`
  - `Ist_Fluktuationsrelevant`
  - `Fluktuation_Ausschlussgrund`

Wichtig: `Kündigung AN` aus Rexx wird jetzt als Arbeitnehmerkuendigung erkannt.

### `fluktuation_measures_dax.txt`

Neues DAX-File fuer die Fluktuations-Measures.

Tabellenreferenzen wurden auf `HR_KPI_DATEN_SAP` gesetzt.

Enthaltene Measures:

- `Headcount Festangestellt`
- `Headcount Aktiv Total`
- `Austritte Total Rexx`
- `Austritte Arbeitnehmerkuendigung`
- `Austritte Fluktuationsrelevant`
- `Austritte Nicht Fluktuationsrelevant`
- `Fluktuation Monat %`
- `Avg Headcount Quartal`
- `Austritte Quartal`
- `Fluktuation Quartal %`
- `Fluktuation Hochrechnung Jahr %`
- `Avg Headcount Jahr`
- `Austritte Jahr`
- `Fluktuation Jahr Effektiv %`
- `Fluktuation Ausschlussgrund Anzahl`

Die Austritts-Measures verwenden `TREATAS` auf `Rexx_Ausgeschieden[Austrittsmonat]`, damit die Filterung ueber `HR_KPI_DATEN_SAP[Periode]` auch ohne direkte Beziehung funktionieren kann.

## Konsolenpruefung der Rexx-Datei

Gepruefte Datei:

```text
C:\temp\Personalausgeschieden.xlsx
```

Gefundene Austritte:

```text
104 total
42 Kuendigung AN
34 Kuendigung AG
15 Befristung
7 leer
5 Ruhestand
1 Aufhebungsvertrag
```

Nach der korrigierten Logik:

```text
33 fluktuationsrelevante Austritte
```

Die Differenz zu 42 `Kuendigung AN` entsteht, weil Aushilfen, Praktikanten, Werkstudenten und Lehrlinge nicht in die Fluktuationsberechnung einfliessen.

## Ursache fuer 0/leere Fluktuation

Die erste Erkennung suchte nach Begriffen wie:

```text
arbeitnehmer
mitarbeiter
eigenkuendigung
kuendigung ma
```

Rexx liefert aber:

```text
Kündigung AN
```

Dadurch war `Ist_Arbeitnehmerkuendigung` ueberall `false`, und die Fluktuations-Measures hatten keinen Zaehler.

## Erwartete Kontrollwerte in Power BI

Nach Aktualisierung der Queries sollten ohne zusaetzliche Filter ungefaehr folgende Werte sichtbar sein:

```text
Austritte Total Rexx = 104
Austritte Arbeitnehmerkuendigung = 42
Austritte Fluktuationsrelevant = 33
```

Wenn `Fluktuation Monat %`, `Fluktuation Quartal %` oder `Fluktuation Jahr Effektiv %` leer bleiben, zuerst diese Punkte pruefen:

- ist `Rexx_Ausgeschieden` geladen?
- heisst die Haupttabelle wirklich `HR_KPI_DATEN_SAP`?
- existieren `HR_KPI_DATEN_SAP[Periode]` und `Rexx_Ausgeschieden[Austrittsmonat]` als Date-Spalten?
- liefert `Headcount Festangestellt` einen Wert groesser 0?
- gibt es aktive Filter auf Jahr, Monat, Organisation oder Kostenstelle?

## Nachtrag: Leere Quartals-/Jahres-Measures

Am 2026-05-12 wurden die DAX-Measures in `fluktuation_measures_dax.txt`
nochmals angepasst, weil folgende Kennzahlen in Power BI leer waren:

- `Austritte Jahr`
- `Austritte Quartal`
- `Fluktuation Hochrechnung Jahr %`
- `Fluktuation Quartal %`
- `BU_Tage_Total`

Wahrscheinliche Ursache:

`HR_KPI_DATEN_SAP[Periode]` wird in `hr_kpi_daten_query.txt` aktuell als
aktueller Monat aus `DateTime.LocalNow()` erzeugt. Dadurch enthalten die
Perioden in der Haupttabelle nicht zwingend dieselben Monate wie
`Rexx_Ausgeschieden[Austrittsmonat]`. Die bisherigen `DATESQTD`- und
`DATESYTD`-Measures konnten deshalb keine passenden Austritte finden und
lieferten leere Werte.

Anpassung in `fluktuation_measures_dax.txt`:

- `Austritte Quartal` rechnet jetzt ueber Quartalsstart und Quartalsende.
- `Austritte Jahr` filtert jetzt ueber das Jahr von `Austrittsmonat`.
- Prozent-Measures sind mit `COALESCE(..., 0)` gegen leere Werte abgesichert.
- Basis-Measures fuer Headcount und Austritte geben ebenfalls `0` statt leer zurueck.
- `BU_Tage_Total`, `NBU_Tage_Total` und `Unfalltage Total` wurden ergaenzt.

Wichtig:

Die `.pbix` wurde weiterhin nicht direkt bearbeitet. Die geaenderten Measures
muessen in Power BI Desktop manuell ersetzt bzw. eingefuegt werden. Falls die
Haupttabelle im Modell nicht `HR_KPI_DATEN_SAP`, sondern z.B. `HR_KPI_Daten`
heisst, muss der Tabellenname in den DAX-Measures entsprechend angepasst werden.

## Power-BI-Datei / PBIX

Die `.pbix`-Datei wurde nicht direkt bearbeitet.

Grund:

- `.pbix` ist kein normales Textprojekt.
- Power-Query-Code und DAX-Measures liegen intern in Power-BI-Modellstrukturen.
- Direktes Bearbeiten kann die Datei beschaedigen.
- Ohne Power BI Desktop, Tabular Editor oder ein `.pbip`-Projekt ist das direkte Patchen riskant und unverhaeltnismaessig.

Empfohlener Weg fuer diese Aenderung:

1. Power BI Desktop oeffnen.
2. Query `Rexx_Ausgeschieden` im Power Query Editor oeffnen.
3. Inhalt durch den aktuellen Code aus `rexx_ausgeschieden.txt` ersetzen.
4. Modell aktualisieren.
5. Nur die geaenderten bzw. benoetigten DAX-Measures aus `fluktuation_measures_dax.txt` ersetzen/einfuegen.

Nicht alle DAX-Measures muessen neu kopiert werden. Zwingend relevant sind vor allem:

- `Headcount Festangestellt`
- `Austritte Fluktuationsrelevant`
- `Avg Headcount Quartal`
- `Austritte Quartal`
- `Avg Headcount Jahr`
- `Austritte Jahr`

Optional als Diagnose:

- `Headcount Aktiv Total`
- `Austritte Total Rexx`
- `Austritte Arbeitnehmerkuendigung`

Falls das Projekt spaeter als `.pbip` statt `.pbix` gespeichert wird, koennen Modell-/Query-Dateien deutlich besser versioniert und direkt angepasst werden.

## Nicht geaenderte Dateien

Nicht angepasst wurden:

- `hr_kpi_daten_query.txt`
- `REXX_aBSENZEN.txt`
- `formeln.docx`
- `HANDOFF_2026-05-11.md`
- `HR_KPI_Formeln_CH.xlsx`
- `infos.txt`
- `infos2.txt`
