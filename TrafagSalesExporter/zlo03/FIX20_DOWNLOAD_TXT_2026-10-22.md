# ZLO03 FIX 20: Download als .txt, weil Excel Text-.xls blockiert

Stand: 2026-10-22. Status: **in T76 aktiv, Auftrag `T76K912742` am 22.10. von Ingo freigegeben** (zusammen mit FIX 12
und FIX 19). Import nach P76 macht Ingo; ob er schon erfolgt ist, ist hier nicht geprueft.
**Nachfolger FIX 21 (echtes `.xlsx`, Wunsch Ingo) ist in T76 aktiv, neuer Auftrag `T76K912752`**, Abschnitt 6.

## 1. Meldung

Ingo am 22.10.: „seit einiger Zeit erstellt ZLO03 defekte Excel“, Beispiel `Downloads\Material_Verwendung.xls`
(22.10. 08:01:55, 1,7 MB, 332 Zeilen, 2'097 Spalten, aus einem Lauf in P76).

## 2. Befund

Die Datei ist nicht defekt. ZLO03 schreibt seit jeher **Tab-getrennten Text** (`GUI_DOWNLOAD`, `filetype 'ASC'`) unter
der Endung `.xls`. Excel verweigert solche Dateien:

> Die Datei kann von Excel nicht geoeffnet werden, da das Dateiformat oder die Dateierweiterung ungueltig ist.

Grund ist die Gruppenrichtlinie `HKCU\Software\Policies\Microsoft\Office\16.0\Excel\Security`, Wert
`ExtensionHardening = 2` (blockieren, wenn Inhalt und Endung nicht zusammenpassen). Gesetzt hat sie die IT; seit wann,
ist nicht bekannt. Sie gilt vermutlich fuer alle Trafag-Rechner, also auch fuer Ann-Katrin, Sandro und Florian.

Gegenprobe mit Excel per COM, nur lesend auf Kopien:

| Datei | Ergebnis |
| --- | --- |
| Ingos Datei als `.xls` | abgewiesen (Meldung oben) |
| dieselbe Datei als `.txt` | geoeffnet, 331 x 2'097, Inhalt korrekt |
| T76-Lauf vom 22.10. (`.xls`) | abgewiesen |
| alte Datei `C:\temp\Material_Verwendung.xls` vom 06.08. | abgewiesen |

Die 2'097 Spalten sind nicht die Ursache (Excel-Blatt hat 16'384). Die Anfuehrungszeichen in Bezeichnungen wie
`G1/4"A` stehen mitten im Feld und stoeren nicht.

**Sofortloesung fuer Benutzer bis zum Import:** die Datei in `.txt` umbenennen und in Excel oeffnen (Datei, Oeffnen,
oder rechte Maustaste, Oeffnen mit Excel).

## 3. Aenderung in `Z_ZLO03_TURBO2`, FORM `download_dialog`

- `GUI_FILE_SAVE_DIALOG`: `default_extension = 'txt'` statt `'xls'`, Filter `Text Tab-getrennt (*.txt)`.
- Oeffnen: `cl_gui_frontend_services=>execute` mit `application = 'excel.exe'` und dem Pfad in Anfuehrungszeichen als
  `parameter` (Standardprogramm fuer `.txt` waere der Editor). Klappt das nicht, wie bisher `document = Pfad`.
  Beide Aufrufe jetzt mit `EXCEPTIONS OTHERS = 1` (vorher ohne, ein Fehler waere ein Kurzdump gewesen).
- `download_background` (Serverdatei `.csv`) unveraendert.
- Inhalt der Datei unveraendert. Quelltext `Z_ZLO03_TURBO2_FIX20_2026-10-22.abap` (Vorstand
  `Z_ZLO03_TURBO2_FIX19_ROHS_2026-10-22.abap`), 19 Zeilen mehr.

Kein echtes `.xlsx`: das braeuchte eine Bibliothek (abap2xlsx oder XCO) und waere ein deutlich groesserer Eingriff.

## 4. Test T76/100 (eigene Sitzung, 22.10.)

| Schritt | Ergebnis |
| --- | --- |
| Sichern, Aktivieren | nur `Z_ZLO03_TURBO2` markiert, „Objekt(e) wurde(n) aktiviert“ |
| Rueckgelesen | identisch mit der Patchdatei (3'550 Zeilen) |
| Lauf Top-Down `8475*` | Speicherdialog schlaegt `Material_Verwendung.txt` vor; nach dem Sichern oeffnete sich Excel mit der Datei (Fenster „ZLO03_FIX20_T76.txt - Excel“) |
| Inhalt | byteweise gleich wie der Lauf mit `.xls` kurz davor |
| Excel per COM | 126 x 182, Spalten RoHS/REACH/SVHC/PFAS-Gehalt hinter `Postyp`, Formel `=SUMMENPRODUKT(...)` wird gerechnet, Bezeichnung `G3/4"a` bleibt in einer Zelle |

## 5. Offen

1. **Welches Programm startet ZLO03 in P76?** In T76 ist es laut `TSTC` `Z_ZLO03_TURBO2`; P76 ist ungeprueft (siehe
   `zlo03/CLAUDE.md`). Ist es dort ein anderes Programm, kommt FIX 20 bei den Benutzern nicht an. Pruefung nur lesend und
   nur auf Auftrag Ingo.
2. **Andere Z-Reports** mit `GUI_DOWNLOAD` `'ASC'` unter `.xls` haben dasselbe Problem; Liste noch nicht erstellt.
3. Import `T76K912742` nach P76 (Ingo). Freigegeben am 22.10.

## 6. FIX 21: echte Excel-Datei (.xlsx), T76 aktiv

Ingo am 22.10.: „jetzt ist aber txt Endung, kannst du nicht echtes Excel schreiben“. Als ich FIX 21 in T76 sichern
wollte, war `T76K912742` bereits freigegeben (`E070` Status R). Gesichert habe ich deshalb nichts; ein neuer Auftrag
ist Ingos Entscheid.

Ansatz (Quelltext `Z_ZLO03_TURBO2_FIX21_XLSX_2026-10-22.abap`, Vorstand FIX 20): neue FORMs `build_xlsx`, `xlsx_add`,
`ist_zahl`, `xml_escape`. ZLO03 baut die Teile einer minimalen Arbeitsmappe selbst (Content-Types, Beziehungen,
`workbook.xml` mit `fullCalcOnLoad`, `styles.xml` mit fetter Kopfzeile, `sheet1.xml`) und packt sie mit `CL_ABAP_ZIP`;
Download `GUI_DOWNLOAD` `BIN` mit `bin_filesize`, Oeffnen mit dem Standardprogramm fuer `.xlsx`. Regeln wie beim
frueheren Text-Import: `-?Ziffern[.Ziffern]` als Zahl, Formeln mit englischen Namen (`SUMPRODUCT`, `SUM`) und Komma,
alles andere als Text. Kopfzeile fett und fixiert. Keine Zusatzbibliothek (abap2xlsx, XCO); SALV-Export haette alles als
Text abgelegt und die Formeln verloren.

Gepruefter Prototyp (Python, dieselben XML-Teile und Regeln) gegen den Text-Import in Excel, Zelle fuer Zelle
`Value2` und `Formula`:

| Datei | Groesse | Unterschiede |
| --- | --- | --- |
| T76-Lauf `8475*` | 126 x 182 | 0 Werte, 0 Formeln |
| Ingos P76-Datei vom 22.10. | 331 x 2'097 | 0 Werte, 0 Formeln |

Einspielen nach Auftrag Ingo („mach ein neuen Auftrag und spiel Excel-Fix ein“), eigene Sitzung T76/100:

| Schritt | Ergebnis |
| --- | --- |
| Sichern | Auftragsdialog, neuer Workbench-Auftrag **`T76K912752`** „ZLO03 FIX 21 Download als echte Excel-Datei xlsx“ |
| Aktivieren | Syntax ok, nur `Z_ZLO03_TURBO2` markiert, „Objekt(e) wurde(n) aktiviert“ |
| Rueckgelesen | identisch mit der Patchdatei (3'781 Zeilen) |
| Lauf `8475*` | Speicherdialog schlaegt `Material_Verwendung.xlsx` vor; Excel oeffnete die Datei direkt, ohne Reparaturhinweis; 74'914 Byte, gleich gross wie der Prototyp |
| Vergleich mit Textlauf | Excel-Format 51 (xlsx), 126 x 182, **0 Wert- und 0 Formelunterschiede**, Kopfzeile fett, `=SUMPRODUCT($E$3:$AX$3,E4:AX4)` |
| Lasttest `84*` | 722 x 3'114 (2,2 Mio. Zellen), Aufbau und Download 14 s, 5,9 MB, in Excel ohne Fehler geoeffnet |

Offen: Freigabe und Import `T76K912752` nach P76 (Ingo); der Auftrag muss **nach** `T76K912742` importiert werden.
