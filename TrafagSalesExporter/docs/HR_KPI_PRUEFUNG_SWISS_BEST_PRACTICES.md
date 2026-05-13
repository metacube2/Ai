# HR-KPI-Pruefung gegen Schweizer Praxis und HR-Best-Practices

Stand: 2026-05-13

Zweck dieses Dokuments:

- fachliche Pruefpunkte fuer den neuen Reiter `HR KPI` sammeln
- keine Codeaenderung ausloesen
- sichtbar machen, welche Kennzahlen bereits plausibel sind und wo vor produktiver Nutzung noch HR-/Fachentscheid noetig ist

## Quellen und Massstab

Verwendeter Massstab:

- Schweizer Absenzverstaendnis gemaess BFS/AVOL: Absenzen sind Zeiten, in denen eine Person normalerweise haette arbeiten muessen, aber nicht gearbeitet hat. Ferien/Feiertage und flexible Arbeitszeitreduktionen sind keine Absenzen.
- Obsan/BFS-Definition fuer gesundheitsbedingte Absenzen: Krankheit und Unfall; Absenzenquote = Absenzen als Prozent der vertraglich festgelegten Jahresarbeitszeit.
- Internationale HR-Controlling-Praxis fuer Fluktuation: Austritte im Zeitraum geteilt durch durchschnittlichen Headcount im Zeitraum. Freiwillige und unfreiwillige Austritte sollten getrennt ausgewiesen werden.

Referenzen:

- BFS Arbeitsvolumenstatistik / Definitionen: https://www.bfs.admin.ch/bfs/de/home/statistiken/arbeit-erwerb/erhebungen/avol.html
- Obsan/BFS Absenzen Krankheit/Unfall: https://ind.obsan.admin.ch/de/indicator/pflemo/absenzen-durch-krankheitunfall
- BAG/Obsan MonAM Absenzen Krankheit/Unfall: https://ind.obsan.admin.ch/fr/indicator/monam/absences-au-travail-pour-cause-de-maladie-ou-d-accident-age-15
- CIPD Retention/Turnover Guidance: https://www.cipd.org/en/knowledge/guides/employee-retention/
- SHRM-nahe Turnover-Formel, oeffentlich referenziert: Separations / Average Employees * 100

## Aktueller Umsetzungsstand im Reiter

Der Reiter liest aktuell:

- `C:\temp\Saldiperstichdatum.xlsx` als Hauptquelle Rexx #757
- `C:\temp\Exportkommengehen.xlsx` fuer Geburtsdatum / Arbeitszeitmodell
- `C:\temp\HR_KPI_Export.xlsx` fuer SAP-Felder
- `C:\temp\Abwesenheitinstunden.xlsx` fuer Krankheit/Absenzen aus Rexx #744
- `C:\temp\Personalausgeschieden.xlsx` fuer Austritte/Fluktuation aus Rexx #381

Die Power-Query-/DAX-Logik wurde nicht als Interpreter umgesetzt, sondern als C#-Nachbau.

## Pruefpunkte mit moeglicher Abweichung

### 1. Fluktuationsnenner: Stichtags-Headcount statt Durchschnitt

Aktueller Reiter:

- `Headcount Festangestellt` wird aus dem aktuell geladenen Stichtagsbestand gerechnet.
- `Avg Headcount Quartal` und `Avg Headcount Jahr` entsprechen aktuell faktisch ebenfalls diesem Stichtagswert.

Best Practice:

- Fluktuation sollte fuer Monat, Quartal und Jahr mit durchschnittlichem Headcount des jeweiligen Zeitraums gerechnet werden.
- Bei stabiler Belegschaft ist der Unterschied klein.
- Bei Wachstum, Abbau oder saisonalen Schwankungen kann der Unterschied relevant sein.

Pruefen:

- Liefert Rexx/SAP monatliche Headcount-Snapshots?
- Falls ja: Monatsdurchschnitt fuer Quartal/Jahr berechnen.
- Falls nein: UI klar als `Stichtagsnahe Fluktuation` oder `Naeherung` beschriften.

Status:

- fachlich akzeptabel als erste Naeherung
- fuer offizielles HR-Reporting noch zu bestaetigen

### 2. Freiwillige vs. unfreiwillige Austritte

Aktueller Reiter:

- `Ist_Arbeitnehmerkuendigung` versucht freiwillige Arbeitnehmer-/Mitarbeiterkuendigungen anhand Textmustern zu erkennen.
- Praktikanten, Werkstudenten, Aushilfen, Lehrlinge, Pensionierungen, befristete Vertraege und Kuendigungen durch Trafag werden ausgeschlossen.

Best Practice:

- Total Turnover und Voluntary Turnover getrennt ausweisen.
- Fuer Retention ist freiwillige Fluktuation meist entscheidender als Gesamtaustritte.

Pruefen:

- Sind alle Rexx-Austrittsarten stabil und vollstaendig gemappt?
- Gibt es lokale Schreibweisen wie `Kdg AN`, `Eigenkuendigung`, `Aufhebungsvereinbarung`, `Mutual agreement`, `Ende Probezeit`?
- Soll `Aufhebungsvereinbarung` zaehlen oder separat ausgewiesen werden?

Status:

- HR-gepruefte Grundlogik vorhanden
- Mappingliste muss bei neuen Austrittsarten gepflegt/validiert werden

### 3. Fluktuation Quartal/Jahr bei nur einem aktuellen Bestand

Aktueller Reiter:

- Quartals-/Jahresraten werden ueber Austrittsdatum gefiltert.
- Headcount bleibt aktueller Stichtagsbestand.

Risiko:

- Wenn der aktuelle Bestand z. B. Ende Jahr niedriger/hoeher ist als im Quartal, verzerrt das die historische Rate.

Pruefen:

- Fuer Quartal/Jahr entweder echte historische Headcounts laden oder die Kennzahl explizit als operative Naeherung fuehren.

Status:

- Darstellung gut fuer operatives Cockpit
- nicht automatisch als auditierbare Jahreskennzahl verwenden

### 4. Absenzenquote: 21 Arbeitstage pauschal

Aktueller Reiter:

- Krankheitstage = Stunden / 8.4
- Krankenquote je Mitarbeiter = Krankheitstage / 21
- Gesamtquote = Krankheitstage / (Headcount * 21)

Schweizer/BFS-nahe Praxis:

- Absenzenquote wird als Dauer der Absenzen in Prozent der vertraglich festgelegten Arbeitszeit berechnet.
- Bei Teilzeit und unterschiedlichen Sollzeiten sollte der Nenner aus Sollarbeitszeit/Solltagen kommen.

Pruefen:

- Soll der Nenner pro Person aus `Avg_Sollzeit_Tag`, Arbeitszeitmodell oder Beschaeftigungsgrad berechnet werden?
- Fuer Teilzeit nicht pauschal 21 Vollzeittage verwenden, falls die Quote offiziell sein soll.
- Krankheit und Unfall separat ausweisen, wenn Datenquelle das erlaubt.

Status:

- 21-Tage-Naeherung gut fuer schnelle Sicht
- fuer Schweizer Standard-Absenzquote fachlich zu ungenau

### 5. Krankheit kurz/lang Definition

Aktueller Reiter:

- `Krankheit angetreten` = kurz
- `Krank nicht buchbar angetreten` = lang
- Umrechnung pauschal Stunden / 8.4

Pruefen:

- Bedeutet `Krank nicht buchbar` fachlich wirklich Langzeitkrankheit?
- Oder ist es ein Buchungs-/Workflowstatus?
- HR muss bestaetigen, ob diese Felder Kurz-/Langzeitkrankheit abbilden.

Status:

- benoetigt HR-/Rexx-Felddefinition

### 6. Unfalltage aus SAP vs. Rexx-Absenzen

Aktueller Reiter:

- Krankheit kommt aus Rexx-Stunden.
- BU/NBU kommt aus SAP-HR-KPI-Datei.

Pruefen:

- Sind BU/NBU in SAP und Krankheit in Rexx zeitlich gleich abgegrenzt?
- Sind Unfalltage in den Rexx-Krankheitsstunden enthalten oder getrennt?
- Gibt es Doppelzaehlung, wenn Krankheit/Unfall spaeter zusammengefuehrt werden?

Status:

- getrennte Anzeige ist korrekt
- Gesamtabsenzquote aus Krankheit + Unfall erst nach Quellenabgleich bilden

### 7. FTE-Berechnung

Aktueller Reiter:

- FTE = Beschaeftigungsgrad aus SAP / 100.
- Wenn SAP-Wert fehlt: Vollzeit = 1, sonst 0.5.

Best Practice:

- FTE sollte aus vertraglichem Beschaeftigungsgrad oder Sollarbeitszeit pro Person kommen.
- Pauschal 0.5 fuer Nicht-Vollzeit ist nur Fallback.

Pruefen:

- Ist `Beschaeftigungsgrad %` fuer alle aktiven Mitarbeitenden verfuegbar?
- Wenn nein: kann Rexx `Arbeitszeitmodell` oder Sollzeit genauer liefern?

Status:

- korrekt, wenn SAP-Datei vollstaendig ist
- Fallback fuer offizielle FTE zu grob

### 8. GLZ-Ampel 50/100 Stunden

Aktueller Reiter:

- Gruen: absolut <= 50h
- Gelb: absolut <= 100h
- Rot: absolut > 100h

Pruefen:

- Sind diese Schwellen HR-/GL-/Reglement-konform?
- Soll negative GLZ gleich behandelt werden wie positive?
- Gibt es unterschiedliche Regeln fuer Teilzeit?

Status:

- als Management-Ampel plausibel
- Schwellen fachlich bestaetigen lassen

### 9. Ferien-Rest-Ampel

Aktueller Reiter:

- Restferien <= 5 Tage = Gruen
- > 5 Tage = Rot

Pruefen:

- Ist >5 Tage wirklich kritisch oder nur zum Jahresende relevant?
- Soll der Stichtag im Jahr beruecksichtigt werden?
- Soll Anspruch, bezogen, ausstehend und Rest getrennt nach Kalenderjahr gezeigt werden?

Status:

- sehr grobe Ampel
- saisonale Logik fehlt

### 10. Lohn / Datenschutz

Aktueller Reiter:

- Bruttolohn wird im Model geladen, aber aktuell nicht prominent als KPI angezeigt.

Pruefen:

- Darf Bruttolohn im HR-KPI-Reiter angezeigt werden?
- Falls ja: welche Rollen duerfen ihn sehen?
- Falls nein: Feld im UI konsequent ausblenden oder gar nicht laden.

Status:

- vor produktivem Einsatz mit Datenschutz/HR klaeren

### 11. Altersgruppen / Geschlecht

Aktueller Reiter:

- Alter und Geschlecht werden berechnet/gemappt.
- Noch keine spezifischen Diversity-/Altersstruktur-Kacheln.

Pruefen:

- Soll Geschlecht nach Schweizer Datenschutz-/HR-Kontext im Cockpit sichtbar sein?
- Aggregiert ja/nein?
- Mindestgruppengroessen fuer Anzeige definieren, damit keine Einzelpersonen ableitbar sind.

Status:

- Daten vorhanden
- Anzeige/Datenschutz noch nicht entschieden

### 12. Personalschluessel / Join-Qualitaet

Aktueller Reiter:

- Rexx #757 und SAP werden ueber Personalnummer verbunden.
- Rexx #732 wird ueber Name verbunden, weil keine Personalnummer vorhanden ist.

Risiko:

- Name-Join ist fehleranfaellig bei gleichen Namen, Namensaenderungen, Sonderzeichen oder Formatabweichungen.

Pruefen:

- Gibt es in #732 doch eine stabile ID?
- Falls nein: Join-Trefferquote anzeigen.
- Nicht gematchte Namen separat ausweisen.

Status:

- wichtigster technischer Qualitaetspruefpunkt

## Empfohlene Mindestkontrollen vor produktiver Nutzung

1. Kontrollwerte aus Power BI / HR gegen neuen Reiter vergleichen:
   - `Austritte Total Rexx = 104`
   - `Austritte Arbeitnehmerkuendigung = 42`
   - `Austritte Fluktuationsrelevant = 33`
2. Headcount aktiv gegen Rexx/HR-Stichtagszahl vergleichen.
3. FTE-Summe gegen SAP/HR vergleichen.
4. Krankheitstage aus Rexx direkt gegen Export-Summe vergleichen.
5. BU/NBU-Tage gegen SAP-Datei summieren.
6. Stichprobe von mindestens 10 Mitarbeitenden pruefen:
   - Personalnummer
   - Organisation
   - FTE
   - GLZ
   - Ferien Rest
   - Krankheitstage
7. Join-Qualitaet dokumentieren:
   - Anzahl Rexx-Hauptzeilen
   - Anzahl SAP-Treffer
   - Anzahl #732-Name-Treffer
   - Anzahl nicht gematcht

## Empfehlung fuer die naechste Umsetzung

Noch keine Formel aendern, bevor die Kontrollwerte protokolliert sind.

Sinnvolle naechste technische Erweiterungen:

- Tab `Datenstatus` um Join-Trefferquoten erweitern.
- Tab `Fluktuation` mit Kontrollwerten Power BI/HR anzeigen.
- Absenzenquote optional auf vertragliche Sollzeit/FTE umstellen.
- Kennzahlen mit `Naeherung` markieren, solange nur ein Stichtagsbestand statt historischer Monats-Snapshots vorhanden ist.

