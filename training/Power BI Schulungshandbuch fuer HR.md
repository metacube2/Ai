# Power BI Schulungshandbuch für HR
Word-Version: Nicht im Repo enthalten (Binary-Dateien werden beim PR-Erstellen nicht unterstützt).

Zielgruppe: 3–4 HR-Mitarbeiterinnen (Schweiz), Excel-Basis + SVERWEIS, Technikaffinität 5–6/10, keine Power BI Vorkenntnisse.

Datenquellen: SAP HCM/HRM (PA0001, PA0002, PA0008, PA2001), Rexx HR-System (Stellenplan, Pulsumfrage, MA-Zufriedenheit), Excel/CSV (Kununu-Score, Refline/Time-to-hire).

KPIs: Headcount/FTE (monatlich), Fluktuation (monatlich), Krankenquote gesamt & ohne Langzeit >30 Tage (Quartal), Überstunden (Quartal), Produktivstunden (wöchentlich), Ferientage/GLZ-Saldi (jährlich), Stellenplan Soll vs Ist (monatlich), Lohnkosten (monatlich), Time to hire (Quartal), Kununu Score (monatlich), Pulsumfrage (Quartal), MA-Zufriedenheitsumfrage (jährlich).

Zielgruppen der Reports: Geschäftsleitung, Verwaltungsrat, Finanzbuchhaltung, Abteilungsleiter.

## 1. MODUL 1: GRUNDLAGEN & DATENIMPORT

### 1.1 Power BI Desktop installieren und starten
1. Schritt: Gehe auf https://powerbi.microsoft.com/de-de/desktop/ und lade Power BI Desktop herunter.
2. Schritt: Installiere die Anwendung mit den Standardoptionen (Weiter → Installieren → Fertigstellen).
3. Schritt: Starte Power BI Desktop über das Startmenü.

[Screenshot: Startfenster von Power BI Desktop mit leeren Berichtsvorlagen].

Tipp: Wenn der Download blockiert ist, wende Dich an die IT (Admin-Rechte erforderlich).

### 1.2 Oberfläche kennenlernen
1. Schritt: Wechsle links zwischen Berichtsansicht, Datenansicht und Modellansicht.
2. Schritt: Erkenne die Bereiche: Menüband oben, Visualisierungen rechts, Felder-Bereich rechts, Seiten-Navigation links.
3. Schritt: Klicke auf eine leere Seite, damit Visualisierungen verfügbar werden.

[Screenshot: Power BI Desktop mit markierter Berichtsansicht, Visualisierungen und Felder-Bereich].

### 1.3 Excel-Datei importieren
1. Schritt: Reiter → Start → Daten abrufen → Excel.
2. Schritt: Datei auswählen → Öffnen.
3. Schritt: Im Navigator Tabelle oder Blatt auswählen → Laden.

Warnung: Wenn Du im Navigator mehrere Tabellen auswählst, kann die Ladezeit steigen.

Häufige Probleme und Lösungen:
1. Problem: Falsche Spaltennamen → Lösung: Erste Zeile als Header setzen (siehe Modul 2).
2. Problem: Zahlen als Text → Lösung: Datentyp korrigieren (siehe Modul 2).

### 1.4 CSV importieren
1. Schritt: Reiter → Start → Daten abrufen → Text/CSV.
2. Schritt: Datei auswählen → Öffnen.
3. Schritt: Im Vorschaufenster Trennzeichen und Kodierung prüfen.

Warnung: In der Schweiz sind Umlaute oft nur mit UTF-8 korrekt. Stelle Kodierung auf UTF-8, falls nötig.

Hinweis: CSV hat keine Formeln oder Formatierungen – nur Rohdaten.

### 1.5 SAP-Export importieren
1. Schritt: SAP-Export (z. B. TXT/CSV/XLSX) in einen lokalen Ordner speichern.
2. Schritt: Reiter → Start → Daten abrufen → Text/CSV oder Excel wählen.
3. Schritt: Im Navigator prüfen, ob die erste Zeile die Spaltenüberschriften enthält.

Tipp: Wenn die Überschriften fehlen, nutze Power Query → Erste Zeile als Überschriften.

## 2. MODUL 2: POWER QUERY EDITOR

### 2.1 Power Query öffnen
1. Schritt: Reiter → Start → Daten transformieren.

[Screenshot: Button 'Daten transformieren' im Menüband].

### 2.2 Erste Zeile als Header verwenden
1. Schritt: Reiter → Transformieren → Erste Zeile als Überschriften.
2. Schritt: Prüfe, ob die Spaltennamen korrekt sind.

### 2.3 Datentypen ändern
1. Schritt: Spalte auswählen (z. B. Eintrittsdatum).
2. Schritt: Reiter → Transformieren → Datentyp → Datum.
3. Schritt: Bei Zahlen Datentyp → Dezimalzahl oder Ganze Zahl.

Warnung: Schweizer Datumsformat (TT.MM.JJJJ) wird manchmal als Text erkannt. In diesem Fall zuerst Datentyp Text, dann Datum mit Gebietsschema Schweiz (Deutsch).

### 2.4 Spalten entfernen/behalten
1. Schritt: Unnötige Spalten markieren.
2. Schritt: Reiter → Start → Spalten entfernen.

Tipp: Nutze "Andere Spalten entfernen", um nur relevante Spalten zu behalten.

### 2.5 Zeilen filtern
1. Schritt: Filterpfeil in der Spalte Status.
2. Schritt: Nur aktive Mitarbeitende auswählen.
3. Schritt: Zeitraumfilter z. B. letztes Jahr.

### 2.6 Werte ersetzen
1. Schritt: Reiter → Transformieren → Werte ersetzen.
2. Schritt: null durch 0 ersetzen.
3. Schritt: Codes (z. B. 'A') durch Klartext (z. B. 'Aktiv') ersetzen.

### 2.7 Spalten teilen/zusammenführen
1. Schritt: Spalte auswählen.
2. Schritt: Reiter → Transformieren → Spalte teilen (nach Trennzeichen).
3. Schritt: Für Zusammenführen: Reiter → Transformieren → Spalten zusammenführen.

### 2.8 Berechnete Spalte hinzufügen
1. Schritt: Reiter → Spalte hinzufügen → Benutzerdefinierte Spalte.
2. Schritt: Formel eingeben (z. B. Beschäftigungsgrad/100).

### 2.9 Schliessen und Laden
1. Schritt: Reiter → Start → Schliessen & laden.
2. Schritt: Unterschied: "Laden" speichert in Modell, "Laden in" erlaubt gezielte Ziele (z. B. nur Verbindung).

## 3. MODUL 3: DATENMODELL

### 3.1 Zur Modellansicht wechseln
1. Schritt: Links auf die Modellansicht (Beziehungs-Icon) klicken.

### 3.2 Beziehungen verstehen
1. Schritt: 1:n = Eine Zeile in Tabelle A passt zu vielen Zeilen in Tabelle B.
2. Schritt: 1:1 = Jede Zeile passt genau zu einer anderen Zeile.

Warum wichtig: Beziehungen steuern, wie Filter zwischen Tabellen fliessen.

### 3.3 Beziehung erstellen
1. Schritt: Spalte in Tabelle A auf passende Spalte in Tabelle B ziehen (Drag & Drop).
2. Schritt: Beziehung prüfen → Kardinalität und Kreuzfilterrichtung einstellen.

Tipp: Nutze meistens Einweg-Filterrichtung, um Mehrdeutigkeiten zu vermeiden.

### 3.4 Datumstabelle erstellen
1. Schritt: Reiter → Modellierung → Neue Tabelle.
2. Schritt: DAX-Formel einfügen:

```
Datum = ADDCOLUMNS(CALENDAR(DATE(2020,1,1), TODAY()), "Jahr", YEAR([Date]), "Monat", MONTH([Date]), "MonatName", FORMAT([Date],"MMMM"), "Quartal", "Q" & QUARTER([Date]), "KW", WEEKNUM([Date]))
```

3. Schritt: Reiter → Tabellen-Tools → Als Datumstabelle markieren → Datum[Date] auswählen.

### 3.5 PERNR als Schlüssel
1. Schritt: Verwende die Personalnummer (PERNR) als Schlüssel zwischen allen SAP-Tabellen (PA0001, PA0002, PA0008, PA2001).

## 4. MODUL 4: DAX MEASURES

### 4.1 Was ist ein Measure vs. berechnete Spalte
1. Schritt: Measure berechnet sich dynamisch im Berichtskontext.
2. Schritt: Berechnete Spalte wird pro Zeile gespeichert und erhöht Modellgrösse.

### 4.2 Neues Measure erstellen
1. Schritt: Reiter → Modellierung → Neues Measure.
2. Schritt: Formel eingeben und mit Enter bestätigen.

### 4.3 Basis-Measures für HR
```
Headcount = COUNTROWS(Mitarbeiter)
FTE = SUMX(Mitarbeiter, Mitarbeiter[Beschäftigungsgrad]/100)
Krankheitstage = SUM(Abwesenheiten[Kalendertage])
Sollarbeitstage = [Headcount] * 21
Krankenquote = DIVIDE([Krankheitstage], [Sollarbeitstage], 0)
Krankenquote_ohne_LZ =
VAR KrankheitstageKurz = CALCULATE([Krankheitstage], FILTER(Abwesenheiten, Abwesenheiten[Kalendertage] <= 30))
RETURN DIVIDE(KrankheitstageKurz, [Sollarbeitstage], 0)
Austritte = CALCULATE(COUNTROWS(Mitarbeiter), Mitarbeiter[Austritt] <> BLANK())
Avg_Headcount = AVERAGEX(VALUES(Datum[Monat]), [Headcount])
Fluktuation = DIVIDE([Austritte], [Avg_Headcount], 0) * 100
```

### 4.4 Zeitintelligenz-Measures
```
Headcount_VJ = CALCULATE([Headcount], SAMEPERIODLASTYEAR(Datum[Date]))
Headcount_VM = CALCULATE([Headcount], PREVIOUSMONTH(Datum[Date]))
Headcount_YTD = TOTALYTD([Headcount], Datum[Date])
Delta_VJ = [Headcount] - [Headcount_VJ]
Delta_VJ_Proz = DIVIDE([Delta_VJ], [Headcount_VJ], 0)
```

### 4.5 Measures formatieren
1. Schritt: Measure auswählen.
2. Schritt: Reiter → Measure-Tools → Format → Prozent, Dezimalstellen, Währung einstellen.

## 5. MODUL 5: VISUALISIERUNGEN

### 5.1 Visualisierungstypen und wann verwenden
1. Karte/Card: Einzelne KPI-Zahl (Headcount, Krankenquote).
2. Balkendiagramm: Vergleiche (Abteilungen, Monate).
3. Liniendiagramm: Zeitverläufe (Headcount über 12 Monate).
4. Ringdiagramm: Anteile (Absenzen nach Typ).
5. Tachometer: Ziel vs Ist (Stellenplan-Erfüllung).
6. Tabelle/Matrix: Details mit Drill-down.

### 5.2 Erste Visualisierung erstellen
1. Schritt: Visualisierung im Bereich Visualisierungen auswählen.
2. Schritt: Felder per Drag & Drop in Achse/Werte ziehen.
3. Schritt: Visualisierung auf der Seite positionieren.

### 5.3 Visualisierung formatieren
1. Schritt: Visual auswählen → Reiter Visual → Format (Pinsel).
2. Schritt: Titel, Farben, Schriftgrössen anpassen.

### 5.4 Filter hinzufügen
1. Schritt: Filterbereich öffnen.
2. Schritt: Felder in Visualfilter, Seitenfilter oder Berichtsfilter ziehen.

### 5.5 Slicer erstellen
1. Schritt: Visualisierung → Datenschnitt (Slicer) wählen.
2. Schritt: Feld (z. B. Zeitraum, Abteilung) hinzufügen.

### 5.6 Bedingte Formatierung
1. Schritt: In Tabelle/Matrix auf Wertefeld klicken → Bedingte Formatierung.
2. Schritt: Regeln definieren (z. B. Rot/Grün je nach Wert).

Tipp: Ampel-Logik funktioniert gut für Krankenquote und Fluktuation.

## 6. MODUL 6: DASHBOARD BAUEN

### 6.1 Dashboard-Layout planen
1. Schritt: F-Muster beachten – Wichtigstes oben links.
2. Schritt: Maximal 6–8 Visualisierungen pro Seite.

### 6.2 Seite 1: Management-Übersicht erstellen
1. Schritt: KPI-Karten oben: Headcount, Krankenquote, Fluktuation, Stellenplan.
2. Schritt: Trendlinie Headcount über 12 Monate.
3. Schritt: Absenzquote nach Typ als Ringdiagramm.

### 6.3 Seite 2: Detailanalyse erstellen
1. Schritt: Matrix mit Drill-down nach Abteilung.
2. Schritt: Filter für Zeitraum und Kostenstelle (Slicer).

### 6.4 Interaktionen zwischen Visualisierungen
1. Schritt: Reiter → Format → Interaktionen bearbeiten.
2. Schritt: Prüfen, ob Klick auf Balken andere Visuals filtert oder hervorhebt.

### 6.5 Design-Tipps
1. Schritt: Konsistente Farben (Firmen-CI).
2. Schritt: Genügend Weissraum.
3. Schritt: Beschriftungen gut lesbar.

## 7. MODUL 7: VERÖFFENTLICHEN & TEILEN

### 7.1 Power BI Service (app.powerbi.com)
1. Schritt: Konto erstellen/anmelden.
2. Schritt: Unterschied Desktop vs Service: Desktop = Modell/Bericht, Service = Teilen/Dashboard.

### 7.2 Bericht veröffentlichen
1. Schritt: Reiter → Datei → Veröffentlichen → Arbeitsbereich wählen.

### 7.3 Arbeitsbereich einrichten
1. Schritt: Im Service → Arbeitsbereich erstellen.
2. Schritt: Zugriffsrechte für Geschäftsleitung/Finanzbuchhaltung setzen.

### 7.4 Dashboard erstellen (aus Bericht)
1. Schritt: Im Service Visualisierung auswählen → Anheften.
2. Schritt: Neues Dashboard erstellen oder bestehendes wählen.

### 7.5 Bericht teilen
1. Schritt: Teilen → Link generieren.
2. Schritt: Zugriff verwalten (Rollen/Personen).

### 7.6 Automatische Aktualisierung einrichten
1. Schritt: Datensatz → Geplante Aktualisierung (täglich/wöchentlich).
2. Schritt: Für lokale Daten Gateway einrichten (IT einbeziehen).

### 7.7 Row-Level Security (RLS)
1. Schritt: Reiter → Modellierung → Rollen verwalten.
2. Schritt: Rolle erstellen, Filter setzen: [Abteilung] = USERPRINCIPALNAME().

Warnung: RLS muss im Service getestet werden (Als Rolle anzeigen).

## 8. TROUBLESHOOTING

### 8.1 Häufige Fehler beim Import
1. Problem: Encoding-Probleme (UTF-8) → Lösung: Kodierung im CSV-Import anpassen.
2. Problem: Dezimaltrennzeichen (Punkt vs Komma) → Lösung: Datentyp mit Gebietsschema Schweiz setzen.
3. Problem: Datum als Text → Lösung: Datentyp Datum und richtiges Gebietsschema.

### 8.2 Häufige DAX-Fehler
1. Problem: Zirkelbezug → Lösung: Berechnete Spalten vermeiden, Measures nutzen.
2. Problem: Division durch Null → Lösung: DIVIDE() verwenden.
3. Problem: Falscher Filterkontext → Lösung: Filter mit CALCULATE prüfen.

### 8.3 Beziehungsprobleme
1. Problem: Mehrdeutige Beziehungen → Lösung: Eine Beziehung aktiv, andere inaktiv setzen.
2. Problem: Fehlende Beziehung → Lösung: Schlüsselspalten prüfen (PERNR, Datum).

### 8.4 Performance-Probleme
1. Problem: Zu viele Spalten importiert → Lösung: Spalten reduzieren.
2. Problem: Zu viele berechnete Spalten → Lösung: Measures bevorzugen.

## 9. ANHANG

### 9.1 DAX Cheat Sheet (alle HR-Formeln)
```
Headcount = COUNTROWS(Mitarbeiter)
FTE = SUMX(Mitarbeiter, Mitarbeiter[Beschäftigungsgrad]/100)
Krankheitstage = SUM(Abwesenheiten[Kalendertage])
Sollarbeitstage = [Headcount] * 21
Krankenquote = DIVIDE([Krankheitstage], [Sollarbeitstage], 0)
Krankenquote_ohne_LZ = VAR KrankheitstageKurz = CALCULATE([Krankheitstage], FILTER(Abwesenheiten, Abwesenheiten[Kalendertage] <= 30))
RETURN DIVIDE(KrankheitstageKurz, [Sollarbeitstage], 0)
Austritte = CALCULATE(COUNTROWS(Mitarbeiter), Mitarbeiter[Austritt] <> BLANK())
Avg_Headcount = AVERAGEX(VALUES(Datum[Monat]), [Headcount])
Fluktuation = DIVIDE([Austritte], [Avg_Headcount], 0) * 100
Headcount_VJ = CALCULATE([Headcount], SAMEPERIODLASTYEAR(Datum[Date]))
Headcount_VM = CALCULATE([Headcount], PREVIOUSMONTH(Datum[Date]))
Headcount_YTD = TOTALYTD([Headcount], Datum[Date])
Delta_VJ = [Headcount] - [Headcount_VJ]
Delta_VJ_Proz = DIVIDE([Delta_VJ], [Headcount_VJ], 0)
```

### 9.2 Checkliste: Neuen Report erstellen
1. Schritt: Datenquellen klären (SAP, Rexx, Excel/CSV).
2. Schritt: Daten importieren (Modul 1).
3. Schritt: Daten bereinigen in Power Query (Modul 2).
4. Schritt: Beziehungen und Datumstabelle erstellen (Modul 3).
5. Schritt: Measures erstellen (Modul 4).
6. Schritt: Visuals bauen und formatieren (Modul 5).
7. Schritt: Dashboard layouten (Modul 6).
8. Schritt: Veröffentlichen und teilen (Modul 7).

### 9.3 Glossar
Power Query: Datenaufbereitungstool in Power BI.

DAX: Formelsprache für Berechnungen in Power BI.

Measure: Dynamische Kennzahl, abhängig vom Filterkontext.

Berechnete Spalte: Feste Berechnung pro Zeile.

RLS: Row-Level Security für zeilenbasierte Zugriffssteuerung.
