# ZLO03 FIX 26: Positionsmenge durch die Basismenge der Stückliste (STKO-BMENG) (2026-11-05)

Stand: 2026-11-05. T76 aktiv, Auftrag **`T76K912839`** (Aufgabe `T76K912840`), nicht freigegeben. Alle Tests in T76 bestanden.
Freigabe und Import nach P76 macht Ingo. Zurück: `zlo03/CLAUDE.md`. Befund: `zlo03/BEFUND_EINHEIT_WAFER_2026-11-05.md`.

## 1. Ursache (Kurzfassung des Befunds)

`ZPOWERBI_VC_TXT-MENGE` ist `STPOX-MENGE` aus `CS_BOM_EXPL_MAT_V2`, also die Positionsmenge **je Basismenge der Stückliste**
(`STKO-BMENG`), nicht je 1 Stück. Die Chip-Stücklisten haben 3'738 bis 4'750 ST je 1 Rolle Wafer. ZLO03 rechnete deshalb 1 ROL
Wafer je verkauftem Chip; im Dashboard (Einkauf › Materialanteil) stand HW_ZUKAUFSORT bei CHF 957 Mio. Materialanteil auf
CHF 3.6 Mio. Umsatz.

## 2. Änderung (`*** FIX 26` in `Z_ZLO03_TURBO2`)

Quelltext `zlo03/Z_ZLO03_TURBO2_FIX26_2026-11-05.abap` (Vorstand FIX 25, `diff` gegen den T76-Live-Stand vorher Exitcode 0;
4'350 Zeilen, 15 Stellen `FIX 26`). Gegen FIX 25 sind genau sechs alte Zeilen ersetzt, alles andere ist neu hinzugefügt.

| Stelle | Änderung |
| --- | --- |
| Kopf „FIXES APPLIED“ | Zeile FIX 26 |
| `ty_mtab-menge` | `p LENGTH 16 DECIMALS 10` statt `menge_d` (3 Stellen): 1 / 4'750 = 0.000211 wäre sonst 0.000 |
| neue Typen `ty_bmeng`, `tt_bmeng`, global `gt_bmeng` | Basismenge je Elternmaterial |
| neue FORM `load_bmeng` | Elternmaterialien aus den gelesenen `ZPOWERBI_VC_TXT`-Zeilen: Stufe 01 das Kopfmaterial (`MATNR`), ab Stufe 02 `KOM_MSTAE` (über `CONVERSION_EXIT_MATN1_INPUT`). `MAST` (Werk `gc_werks`) ⋈ `STKO` (`STLTY = 'M'`, `DATUV <= heute`) ⋈ `MARA`; je Stückliste der jüngste gültige Kopf, gelöschte (`LKENZ`/`LOEKZ`) zählen nicht. Behalten nur eindeutige Basismengen ≠ 1 in der Basiseinheit des Elternmaterials (`BMEIN = MARA-MEINS`); mehrere Stücklisten mit verschiedener Basismenge: nicht teilen. Jeder Eintrag steht zusätzlich unter der MATN1-Ausgabeform, weil `KOM_MSTAE` so geschrieben ist (numerische Nummern ohne führende Nullen) |
| `process_main` | Aufruf `load_bmeng` nach `load_bom_units`; nach dem FIX-23-Block `lv_menge_fix26 = lv_menge / BMENG des Elternmaterials` (nur wenn ein Eintrag existiert, keine Textposition). Top-Down (`COLLECT`) und Bottom-Up (beide Zuweisungen) nehmen `lv_menge_fix26` statt `lv_menge`. DATA am Anfang der FORM |
| `app_export` | `WERT = ungerundete Menge × STK` (statt der auf 6 Stellen gerundeten Menge) |
| `create_excel_output`, Mengenspalte je VKNR | über `zahl_text` (FIX 24): höchstens 6 Nachkommastellen, Nullen am Ende weg |

Bewusst nicht geändert: der Generator (`Z_VC_ZLO03*`, schreibt weiter `STPOX-MENGE`; kein BMENG-Feld in `ZPOWERBI_VC_TXT`), das
Dashboard (rechnet mit `ZloTopDownSet` richtig), G4 Mehrstufigkeit (Mengen ab Stufe 02 gelten weiter je Elternbaugruppe, nicht je
Gerät), FP_THERMOST (andere Ursache, Abschnitt 6).

**Formatänderung in der Excel-Ausgabe:** Mengen je VKNR stehen jetzt ohne Nullen am Ende („1“ statt „1.000“, „0.000211“). Für
Ann-Katrin und Sandro kurz erwähnen; die Zahlen sind gleich (Test C).

## 3. Einspielen (eigene Sitzung T76/100, ohne Passwort: GUI-Scripting an der angemeldeten Sitzung, Lesen per RFC mit DPAPI-Ablage)

| Schritt | Ergebnis |
| --- | --- |
| Sperre | `Z_ZLO03_TURBO2` in keinem offenen Auftrag (`E071`: letzte Aufträge `T76K912785/786`, freigegeben; `E070` offene Aufträge geprüft) |
| Auftrag | SAP schlug `T76K912837` vor (Monthly Review, heute, offen); nicht genommen. Neuer Workbench-Auftrag **`T76K912839`** „ZLO03 FIX 26 Basismenge STKO-BMENG (Wafer je Chip)“, Ziel P76, Aufgabe `T76K912840` mit `LIMU REPS Z_ZLO03_TURBO2` |
| Syntaxprüfung | nur die bekannte Warnung „Regex-Standard POSIX abgekündigt“ in Zeile 3992 (FIX 21, `build_xlsx`), keine Fehler |
| Aktivieren | nur `Z_ZLO03_TURBO2` markiert (fremdes inaktives `Z_REICHW` übergangen), „Objekt(e) wurde(n) aktiviert“, kein Fehlerdialog; `REPOSRC` nur Status A (05.11.2026 11:10:44) |
| Rückgelesen | `abap-read`, `diff` gegen die Patchdatei Exitcode 0 (4'350 Zeilen) |
| ST22 | heute 0 Laufzeitfehler |

## 4. Tests in T76 vorher/nachher

Testreport `Z_KOI_ZLO_FIX26_TEST` (`$TMP`, Quelltext `zlo03/Z_KOI_ZLO_FIX26_TEST.abap`). Teil A ruft
`ZCL_ZPOWERBI_EINKAUF_DPC_EXT->GET_ENTITYSET` für `ZloTopDownSet` auf (derselbe `SUBMIT ... P_APP` wie das Gateway). Teil B startet
`Z_ZLO03_TURBO2` als Hintergrundjob, weil der Windows-Speicherdialog nicht per Scripting bedienbar ist; im Batch schreibt ZLO03 den
Excel-Inhalt nach `/usr/sap/trans/zlo03_analyse_KOI.csv`, der Report liest die Datei. Die beiden eigenen Vergleichsdateien für 41019
sind danach wieder gelöscht. Die Repository-Datei ist die Fassung der Tests A bis F; in T76 steht seit dem Lauf für Abschnitt 6 eine
Kurzfassung (nur Teil A für 63759 und das Löschen der Vergleichsdateien).

| Fall | Auswahl | Vorher (FIX 25) | Nachher (FIX 26) | Erwartung |
| --- | --- | --- | --- | --- |
| A | Top-Down E11221 (Set) | E11283 1.000000 ROL, Stk 1'186.28, Wert 1'186.28 | **0.000211 ROL**, Stk 1'186.28, **Wert 0.249743** | 1 / 4'750; 1'186.28 / 4'750 = 0.249743 ✓ |
| B | Top-Down E11220 | E11282 1.000000, Wert 3'207.28 | **0.000268**, **Wert 0.858020** | 1 / 3'738 ✓ |
| B | Top-Down E11224 | E11283 1.000000, Wert 1'186.28 | **0.000220**, **Wert 0.260836** | 1 / 4'548 ✓ |
| B | Top-Down E11228 | E11301 1.000000, Wert 1'239.86 | **0.000220**, **Wert 0.272617** | 1 / 4'548 ✓ |
| C | Top-Down 41019, Excel-Inhalt Zelle für Zelle | 97 Zeilen | 97 Zeilen, **86 Zellen nur Format** („1.000“ → „1“, zahlengleich), **0 Zellen anders** | unverändert bis aufs Format ✓ |
| D | 41019 (Set) | 94 Komponenten, Summe Wert 216.725069; D85020 2 ST, H05039 3 ST zu 0.014133 | identisch | unverändert ✓ |
| E | Bottom-Up E11283, Excel-Inhalt | Spalte E11283: E11221 1.000, E11224 1.000 | **E11221 0.000211, E11224 0.00022** | ✓ |
| E | Top-Down E11221, Excel-Inhalt | Mengenspalte 1.000 | **0.000211**; Formel `=SUMMENPRODUKT($E$3:$E$3;E4:E4)` und übrige Spalten unverändert | ✓ |
| F | Gateway Client `ZloTopDownSet`, Werks 1100, Vknr E11221 bzw. 41019 | – | **HTTP 200**, 516 Byte (0,8 s) bzw. 200, 21'740 Byte (3,9 s) | 200 ✓; Werte über Teil A (gleicher Aufruf) |

Bottom-Up nimmt dieselbe Variable `lv_menge_fix26` wie Top-Down; Exklusivität und Bottom-Up-Details rechnen nicht mit
`gt_mtab-menge` und sind unberührt.

## 5. P76: welche Stücklisten sind betroffen (nur lesend, Freigabe Ingo 2026-11-05)

Gelesen über die angemeldete P76-GUI: `P76Lesen.vbs`, SE16 in einer eigenen Sitzung (Ingos Sitzung unberührt, danach geschlossen),
kein Passwort. Abfragen: `STKO` mit `STLTY = M` und `BMENG` 1.001 bis 9'999'999'999 bzw. 0 bis 0.999, dann `MAST` je gefundener
Stücklistennummer. Sonst nichts gelesen.

| STLNR | Material | Werk | Verwendung | BMENG | BMEIN | gültig ab | zuletzt geändert |
| --- | --- | --- | --- | ---: | --- | --- | --- |
| 00026393 | PLM8252 | 1100 | 8 | 100 | ST | 13.12.2013 | – |
| 00026504 | E11220 | 1100 | 3 | 3'738 | ST | 10.01.2014 | 24.02.2023 LAA |
| 00026939 | E11221 | 1100 | 3 | 4'750 | ST | 31.03.2014 | 01.04.2014 GUN |
| 00026940 | E11224 | 1100 | 3 | 4'548 | ST | 31.03.2014 | 02.09.2022 WID |
| 00038115 | E11228 | 1100 | 3 | 4'548 | ST | 05.02.2020 | 28.04.2023 WID |
| 00038116 | E11229 | 1100 | 3 | 4'548 | ST | 05.02.2020 | 02.09.2022 WID |

**6 Stücklisten mit `BMENG > 1`, keine mit `BMENG < 1`; genau dieselben wie in T76.** FIX 26 wirkt in P76 also auf die fünf
Chips (Wafer E11282, E11283, E11301) und auf PLM8252 (Verwendung 8, laut Befund ohne Positionen, ohne Wirkung).

Zuerst war dafür ein SapProbe-Fenster mit P76-Passworteingabe vorbereitet (Vorlage `P76Pruefung_*.ps1`). Das ist nicht der Weg:
Ingo gibt kein SAP-Passwort ein. Danach lief kein Prozess mit diesem Skript, und das Protokoll, das vor dem ersten Aufruf
geschrieben wird, fehlt: nichts eingegeben, kein Anmeldeversuch an P76. Die Dateien sind gelöscht (`docs/rag/LEARNINGS.md` 2026-11-05).

## 6. FP_THERMOST. (Einkauf-Prüfung F1): andere Ursache, nicht die Basismenge

T76 lesend, Werte über den Testreport (VKNR 63759, ohne Löschvormerkung; 1050 und 41506 sind in T76 löschvorgemerkt):

| Komponente | Basiseinheit | Stückliste im Gerät | ZLO03 rechnet je Gerät | richtig wäre |
| --- | --- | --- | --- | --- |
| H20201 Kapillarrohr | KG (MARM: 1 MM = 1/37'300 KG) | Stufe 01: 115 MM; Stufe 02: **1 KG H20201 unter H20201** | **116 KG**, CHF 1'815.40 von 1'952.22 je Gerät | 115 MM ≈ 0.0031 KG ≈ CHF 0.05 |
| H17500 Bimetall | M | Stufe 02: 0.020 M (unter R15068); Stufe 03: **1 M H17500 unter H17500** | **1.02 M**, CHF 29.07 | 0.02 M, CHF 0.57 |

Zwei Ursachen, beide mit `BMENG = 1` (`STKO` 00045280 H20201 1 KG, 00045289 H17500 1 M), FIX 26 ändert daran nichts:

1. **Stückliste, die sich selbst enthält.** H20201 (Stückliste 00045280, Verwendung 3, seit 24.04.2025) enthält 1 KG H20201,
   H17500 (00045289, seit 28.04.2025) 1 M H17500. Die Auflösung steigt eine Stufe hinein, und ZLO03 zählt die zusätzliche Zeile
   (1 KG bzw. 1 M je Gerät) zum Bedarf. Das Muster gibt es bei vielen Teilen (z. B. K31000, R14006, R13026, D34544 in 1050/41019),
   dort meist 1 ST unter 1 ST mit kleinem Preis.
2. **Einheit MM gegen KG (nur H20201).** Je Gerät steht H20201 einmal in MM und einmal (Selbststückliste) in KG. `load_bom_units`
   (FIX 23) zählt 1:1, die Gleichheit fällt alphabetisch auf KG = Basiseinheit, also kein MARM-Faktor; `convert_menge` rechnet
   MM → KG mit `UNIT_CONVERSION_SIMPLE`, das scheitert (Länge gegen Masse) und behält die Zahl: 115 MM werden 115 KG.

Vorschlag (eigener Fix, nicht Teil von FIX 26, erst mit Ingo entscheiden): Zeilen mit `KOM_MSTAE = KOMPNR` (Selbststückliste)
nicht in den Bedarf; bei Gleichstand in `load_bom_units` die Einheit vorziehen, die nicht die Basiseinheit ist, oder MM über MARM
umrechnen. Ob die Selbststücklisten fachlich gewollt sind (Verwendung 3, angelegt 2025), mit dem Stücklistenverantwortlichen klären.

## 7. KOM_MSTAE ist das Elternmaterial (Punkt A in `CLAUDE.md` überholt)

T76, `ZPOWERBI_VC_TXT` für 41019: Stufe 01 hat `KOM_MSTAE` leer, Stufe 02 D34544 hat `KOM_MSTAE` D34576 und `MAT_MSTAE` 00966183
(ID der Elternzeile), K70957 auf Stufe 02 hat C10418; in 1050 hat K17001 auf Stufe 02 U47123 mit `MAT_MSTAE` = ID der U47123-Zeile.
Der Generator schreibt `kom_mstae = CONVERSION_EXIT_MATN1_OUTPUT( parent_matnr )`. Für P76 ist der Inhalt nicht geprüft (Freigabe
umfasste nur STKO/MAST); ist das Feld dort leer oder anders belegt, teilt FIX 26 ab Stufe 02 nicht (vorsichtige Seite), Stufe 01
(Wafer unter den Chips) wirkt trotzdem.

## 8. Offen (Ingo)

1. `T76K912839` freigeben und nach P76 importieren.
2. Nach dem Import in P76: Einkauf-Lauf neu starten (Tabelle `PurchasingMaterialShareBom` wird ersetzt) und HW_ZUKAUFSORT prüfen.
   Erwartet: Wafer je Chip ≈ CHF 0.26 bis 0.86, HW_ZUKAUFSORT rund CHF 1.27 Mio. statt 957 Mio. (Befund 3.5).
3. Ann-Katrin und Sandro: Mengenformat in der Excel-Spalte („1“ statt „1.000“), Chips zeigen Wafer jetzt als Bruchteil einer Rolle.
4. FP_THERMOST (Abschnitt 6): eigener Fix nach Entscheid; Selbststücklisten H20201/H17500 fachlich klären.
5. `$TMP`-Testreport `Z_KOI_ZLO_FIX26_TEST` in T76 bei Gelegenheit löschen.
