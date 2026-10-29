# ZLO03 FIX 22: Disponent und Warengruppe im Top-Down (2026-10-29)

Stand: 2026-10-29. T76 aktiv, Auftrag `T76K912777`, nicht freigegeben.

## 1. Wunsch

Armin (Einkauf), 29.10.: „wenn es keine grosse Herausforderung ist im ZLO03 Top-Down die Felder MARC-DISPO; MARA-MATKL
auszugeben, würde mir dies weiterhelfen im Rahmen der Warengruppenauswertung.“ Beide Felder kamen im Quelltext
`Z_ZLO03_TURBO2` bisher gar nicht vor.

## 2. Umsetzung

Quelltext `Z_ZLO03_TURBO2_FIX22_DISPO_MATKL_2026-10-29.abap` (Vorstand FIX 21, in T76 vorher gegengelesen: identisch).

- `ty_ktab`, `ty_stamm`, `ty_stamm_raw` um `dispo` (`MARC-DISPO`) und `matkl` (`MARA-MATKL`) erweitert; der vorhandene
  Stammdaten-Join in `load_stammdaten_cache` liest `c~dispo` (Werk wie bisher `gc_werks`) und `m~matkl` mit, kein
  zusaetzlicher Datenbankzugriff. `fill_ktab` uebernimmt beide.
- `create_excel_output`: nur bei `p_topd = 'X'` zwei Spalten **Disponent** und **Warengruppe** hinter `PFAS-Gehalt`.
  Bottom-Up unveraendert. Textpositionen bleiben leer.
- `build_xlsx`: Spalten mit der Ueberschrift `Disponent` oder `Warengruppe` werden immer als Text abgelegt. Ohne das
  haette `ist_zahl` den Disponenten `026` als Zahl 26 geschrieben.

## 3. Einspielen und Test (eigene Sitzung T76/100)

| Schritt | Ergebnis |
| --- | --- |
| Auftrag | `T76K912752` (FIX 21) war schon freigegeben (`E070` Status R); deshalb neuer Workbench-Auftrag **`T76K912777`** „ZLO03 FIX 22 Top-Down Disponent und Warengruppe“ |
| Aktivieren | nur `Z_ZLO03_TURBO2` markiert (`Z_REICHW` uebergangen), „Objekt(e) wurde(n) aktiviert“ |
| Rueckgelesen | per RFC identisch mit der Patchdatei (3'813 Zeilen, 16 Stellen `FIX 22`) |
| Lauf Top-Down Typencode `8475*` | 126 x 184 (vorher 126 x 182), Kopf Spalten 83/84 `Disponent`, `Warengruppe`; 123 Datenzeilen als Text, z. B. `003` (46), `001` (43), `024`, `A99`, `TPT`; Warengruppen wie `40.02.03`, `01`; Formel `=SUMPRODUCT($E$3:$AX$3,E4:AX4)` unveraendert |

Falle beim Lauf: Mit Material `8475*` meldet ZLO03 „Keine Daten gefunden“; `8475*` gehoert in das Feld **Typencode**
(`S_TYPCD`). Den Windows-Dialog „Datei speichern“ erreicht das Scripting nicht; Ingo hat gespeichert.

## 4. Offen

1. Freigabe und Import `T76K912777` nach P76 (Ingo), **nach** `T76K912742` und `T76K912752`.
2. Rueckmeldung an Armin, sobald der Import durch ist.
