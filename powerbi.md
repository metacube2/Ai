Erstelle ein Word-Dokument: "Power BI Schulungshandbuch für HR" mit Schritt-für-Schritt-Anleitungen.

ZIELGRUPPE:
- 3-4 HR-Mitarbeiterinnen, Schweiz
- Excel-Kenntnisse: Basis + SVERWEIS
- Technikaffinität: 5-6/10
- Keine Power BI Vorkenntnisse

DATENQUELLEN DER TEILNEHMER:
- SAP HCM/HRM (alle Infotypen, besonders PA0001, PA0002, PA0008, PA2001)
- Rexx HR-System (Stellenplan, Pulsumfrage, MA-Zufriedenheit)
- Excel/CSV (Kununu-Score, Refline/Time-to-hire)

KPIs DIE ABGEBILDET WERDEN SOLLEN:
- Headcount/FTE (monatlich)
- Fluktuation (monatlich)
- Krankenquote gesamt + ohne Langzeitkrankheiten >30 Tage (Quartal)
- Überstunden (Quartal)
- Produktivstunden (wöchentlich)
- Ferientage/GLZ-Saldi (jährlich)
- Stellenplan Soll vs Ist (monatlich, aus Rexx)
- Lohnkosten (monatlich)
- Time to hire (Quartal)
- Kununu Score (monatlich)
- Pulsumfrage (Quartal, aus Rexx)
- MA-Zufriedenheitsumfrage (jährlich, aus Rexx)

ZIELGRUPPEN DER REPORTS:
- Geschäftsleitung
- Verwaltungsrat
- Finanzbuchhaltung
- Abteilungsleiter

STRUKTUR DES DOKUMENTS:

1. MODUL 1: GRUNDLAGEN & DATENIMPORT
1.1 Power BI Desktop installieren und starten
- Wo herunterladen, Installation, erster Start
1.2 Oberfläche kennenlernen
- Berichtsansicht, Datenansicht, Modellansicht erklären
- Wo findet man was (Menüband, Felder-Bereich, Visualisierungen)
1.3 Excel-Datei importieren
- Schritt-für-Schritt: Daten abrufen → Excel → Datei wählen → Navigator → Laden
- Häufige Probleme und Lösungen
1.4 CSV importieren
- Unterschiede zu Excel, Encoding-Probleme Schweiz (Umlaute)
1.5 SAP-Export importieren
- Typische SAP-Exportformate verarbeiten
- Spaltenüberschriften aus erster Zeile

2. MODUL 2: POWER QUERY EDITOR
2.1 Power Query öffnen
- Daten transformieren → Button finden
2.2 Erste Zeile als Header verwenden
- Schritt-für-Schritt mit Menüpfad
2.3 Datentypen ändern
- Datum, Zahl, Text erkennen und korrigieren
- Schweizer Datumsformat beachten
2.4 Spalten entfernen/behalten
- Nur relevante Spalten behalten
2.5 Zeilen filtern
- Beispiel: Nur aktive Mitarbeiter, nur bestimmter Zeitraum
2.6 Werte ersetzen
- null durch 0 ersetzen, Codes durch Klartext
2.7 Spalten teilen/zusammenführen
2.8 Berechnete Spalte hinzufügen
2.9 Schliessen und Laden
- Unterschied: Laden vs. Laden in

3. MODUL 3: DATENMODELL
3.1 Zur Modellansicht wechseln
3.2 Beziehungen verstehen
- 1:n, 1:1 erklären
- Warum Beziehungen wichtig sind
3.3 Beziehung erstellen
- Drag & Drop zwischen Tabellen
- Beziehung bearbeiten (Kardinalität, Kreuzfilterrichtung)
3.4 Datumstabelle erstellen
- Warum eigene Datumstabelle nötig
- DAX-Formel zum Erstellen:
  Datum = ADDCOLUMNS(CALENDAR(DATE(2020,1,1), TODAY()), "Jahr", YEAR([Date]), "Monat", MONTH([Date]), "MonatName", FORMAT([Date],"MMMM"), "Quartal", "Q" & QUARTER([Date]), "KW", WEEKNUM([Date]))
- Als Datumstabelle markieren (Menüpfad)
3.5 PERNR als Schlüssel
- Personalnummer verbindet alle SAP-Tabellen

4. MODUL 4: DAX MEASURES
4.1 Was ist ein Measure vs. berechnete Spalte
4.2 Neues Measure erstellen
- Menüpfad: Modellierung → Neues Measure
4.3 Basis-Measures für HR:

Headcount:
Headcount = COUNTROWS(Mitarbeiter)

FTE:
FTE = SUMX(Mitarbeiter, Mitarbeiter[Beschäftigungsgrad]/100)

Krankheitstage:
Krankheitstage = SUM(Abwesenheiten[Kalendertage])

Sollarbeitstage:
Sollarbeitstage = [Headcount] * 21

Krankenquote:
Krankenquote = DIVIDE([Krankheitstage], [Sollarbeitstage], 0)

Krankenquote ohne Langzeit (>30 Tage):
Krankenquote_ohne_LZ = 
VAR KrankheitstageKurz = CALCULATE([Krankheitstage], FILTER(Abwesenheiten, Abwesenheiten[Kalendertage] <= 30))
RETURN DIVIDE(KrankheitstageKurz, [Sollarbeitstage], 0)

Austritte:
Austritte = CALCULATE(COUNTROWS(Mitarbeiter), Mitarbeiter[Austritt] <> BLANK())

Durchschnittlicher Headcount:
Avg_Headcount = AVERAGEX(VALUES(Datum[Monat]), [Headcount])

Fluktuation:
Fluktuation = DIVIDE([Austritte], [Avg_Headcount], 0) * 100

4.4 Zeitintelligenz-Measures:

Vorjahreswert:
Headcount_VJ = CALCULATE([Headcount], SAMEPERIODLASTYEAR(Datum[Date]))

Vormonat:
Headcount_VM = CALCULATE([Headcount], PREVIOUSMONTH(Datum[Date]))

Year-to-Date:
Headcount_YTD = TOTALYTD([Headcount], Datum[Date])

Delta zum Vorjahr:
Delta_VJ = [Headcount] - [Headcount_VJ]

Delta Prozent:
Delta_VJ_Proz = DIVIDE([Delta_VJ], [Headcount_VJ], 0)

4.5 Measures formatieren
- Prozent, Dezimalstellen, Währung einstellen

5. MODUL 5: VISUALISIERUNGEN
5.1 Visualisierungstypen und wann verwenden:
- Karte/Card: Einzelne KPI-Zahl (Headcount, Krankenquote)
- Balkendiagramm: Vergleiche (Abteilungen, Monate)
- Liniendiagramm: Zeitverläufe (Headcount über 12 Monate)
- Ringdiagramm: Anteile (Absenzen nach Typ)
- Tachometer: Ziel vs Ist (Stellenplan-Erfüllung)
- Tabelle/Matrix: Details mit Drill-down

5.2 Erste Visualisierung erstellen
- Schritt-für-Schritt: Visualisierung wählen → Felder reinziehen
5.3 Visualisierung formatieren
- Titel, Farben, Schriftgrössen
5.4 Filter hinzufügen
- Visualfilter, Seitenfilter, Berichtsfilter
5.5 Slicer erstellen
- Zeitraum-Auswahl, Abteilungs-Auswahl
5.6 Bedingte Formatierung
- Rot/Grün je nach Wert (Ampel-Logik)

6. MODUL 6: DASHBOARD BAUEN
6.1 Dashboard-Layout planen
- F-Muster: Wichtigstes oben links
- Max 6-8 Visualisierungen pro Seite
6.2 Seite 1: Management-Übersicht erstellen
- KPI-Karten oben: Headcount, Krankenquote, Fluktuation, Stellenplan
- Trendlinie Headcount
- Absenzquote nach Typ
6.3 Seite 2: Detailanalyse erstellen
- Matrix mit Drill-down nach Abteilung
- Filter für Zeitraum und Kostenstelle
6.4 Interaktionen zwischen Visualisierungen
- Klick auf Balken filtert andere Visuals
- Interaktionen bearbeiten (Menüpfad)
6.5 Design-Tipps
- Konsistente Farben (Firmen-CI)
- Genügend Weissraum
- Beschriftungen lesbar

7. MODUL 7: VERÖFFENTLICHEN & TEILEN
7.1 Power BI Service (app.powerbi.com)
- Konto erstellen/anmelden
- Unterschied Desktop vs Service
7.2 Bericht veröffentlichen
- Menüpfad: Datei → Veröffentlichen → Arbeitsbereich wählen
7.3 Arbeitsbereich einrichten
7.4 Dashboard erstellen (aus Bericht)
- Visualisierung anheften
7.5 Bericht teilen
- Link teilen, Zugriff verwalten
7.6 Automatische Aktualisierung einrichten
- Geplante Aktualisierung (täglich, wöchentlich)
- Gateway für lokale Daten (IT einbeziehen)
7.7 Row-Level Security (RLS)
- Abteilungsleiter sehen nur eigene Daten
- Rolle erstellen, DAX-Filter: [Abteilung] = USERPRINCIPALNAME()

8. TROUBLESHOOTING
8.1 Häufige Fehler beim Import
- Encoding-Probleme (UTF-8)
- Falsches Dezimaltrennzeichen (Punkt vs Komma)
- Datum wird als Text erkannt
8.2 Häufige DAX-Fehler
- Zirkelbezug
- Division durch Null (DIVIDE verwenden)
- Falscher Filterkontext
8.3 Beziehungsprobleme
- Mehrdeutige Beziehungen
- Fehlende Beziehung
8.4 Performance-Probleme
- Zu viele Spalten importiert
- Berechnete Spalten vs Measures

9. ANHANG
9.1 DAX Cheat Sheet (alle HR-Formeln auf einer Seite)
9.2 Checkliste: Neuen Report erstellen
9.3 Glossar (Power Query, DAX, Measure, etc.)

FORMAT-ANWEISUNGEN:
- Jeder Schritt nummeriert
- Menüpfade in Format: Reiter → Gruppe → Button
- DAX-Formeln in Codeblock/Monospace
- Tipps und Warnungen hervorheben
- Screenshots beschreiben wo sinnvoll: [Screenshot: Beschreibung was zu sehen sein sollte]
- Sprache: Deutsch (Schweiz), Du-Form
