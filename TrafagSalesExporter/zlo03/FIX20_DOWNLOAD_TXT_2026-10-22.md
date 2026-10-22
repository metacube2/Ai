# ZLO03 FIX 20: Download als .txt, weil Excel Text-.xls blockiert

Stand: 2026-10-22. Status: **in T76 aktiv, im Auftrag `T76K912742`** (zusammen mit FIX 12 und FIX 19; das Objekt ist dort
gesperrt, ein anderer Auftrag ist nicht moeglich). Freigabe und Import nach P76 macht Ingo.

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
3. Import `T76K912742` nach P76 (Ingo).
