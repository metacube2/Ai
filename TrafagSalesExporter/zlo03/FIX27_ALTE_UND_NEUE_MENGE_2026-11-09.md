# ZLO03 FIX 27: Excel zeigt die alte Menge je Basismenge und zusaetzlich die Menge je 1 Stueck (2026-11-09)

Stand: 2026-11-09. T76 aktiv, im **selben Auftrag `T76K912839`** wie FIX 26 (Aufgabe `T76K912840`), nicht freigegeben.
Zurueck: `zlo03/CLAUDE.md`. Grundlage: `FIX26_BASISMENGE_2026-11-05.md`.

## 1. Auftrag

Ingo 2026-11-09: „beim FIX 26 das alte Feld lassen plus das neue drinlassen, Dashboard arbeitet mit neuem, eventuell brauchen
Leute noch das alte zusaetzlich“. Entscheid Ingo (Auswahl): **alte VKNR-Spalten bleiben wie bisher, neue Menge je VKNR als
Zusatzspalte am Zeilenende**; Summenformeln bleiben auf den alten Spalten.

## 2. Aenderung (`*** FIX 27` in `Z_ZLO03_TURBO2`)

Quelltext `zlo03/Z_ZLO03_TURBO2_FIX27_2026-11-09.abap` (Vorstand FIX 26, gegen den T76-Live-Stand vorher zeilengleich gelesen;
4'379 Zeilen; eine Zeile ersetzt, 30 hinzugefuegt).

| Stelle | Aenderung |
| --- | --- |
| Kopf „FIXES APPLIED“ | Zeile FIX 27 |
| `ty_mtab` | neues Feld `menge_alt` (p 16, 10 Nachkommastellen): Menge je Basismenge wie vor FIX 26 |
| `process_main` Top-Down | `ls_mtab-menge_alt = lv_menge` neben `ls_mtab-menge = lv_menge_fix26` (COLLECT summiert beide) |
| `process_main` Bottom-Up | beide Zweige setzen `menge_alt` ausdruecklich (Summe bzw. Erstwert) |
| `create_excel_output`, VKNR-Spalten | `zahl_text` mit `menge_alt` statt `menge`: wieder die Werte wie vor FIX 26 (Format wie FIX 26, „1“ statt „1.000“) |
| `create_excel_output`, Kopfzeile | am Zeilenende je VKNR „<VKNR> je 1 Stk“ (nach Compliance bzw. Disponent/Warengruppe) |
| `create_excel_output`, Datenzeilen | am Zeilenende je VKNR die FIX-26-Menge (`menge`), gleiche Reihenfolge wie die VKNR-Spalten |

Unveraendert: `app_export`/`ZloTopDownSet` (Dashboard rechnet weiter mit der FIX-26-Menge), Summenformeln, Exklusivitaet,
Verbrauchs- und Statuszeile (bleiben kuerzer, offener Punkt C).

## 3. Einspielen (eigene T76-Sitzung, ohne Passwort)

`SapGuiSetReportSourceAuftrag.vbs` mit Auftrag `T76K912839`, gesichert; aktiviert nur `Z_ZLO03_TURBO2` (fremdes inaktives
`Z_REICHW` uebergangen), „Objekt(e) wurde(n) aktiviert“ ohne Fehlerdialog. Rueckgelesen: 8 Stellen `FIX 27`. `E071`: Aufgabe
`T76K912840` enthaelt `REPS Z_ZLO03_TURBO2`, beide Auftraege Status D.

## 4. Test T76 (Testreport `Z_KOI_ZLO_FIX26_TEST`, `$TMP`, Fassung `zlo03/Z_KOI_ZLO_FIX27_TEST.abap`)

Teil A ruft das Dashboard-Set, Teil B startet ZLO03 als Hintergrundjob und liest den Excel-Inhalt (Spalte 1, Spalte 5 = erste
VKNR-Spalte, letzte zwei Spalten).

| Fall | VKNR-Spalte (alt) | Zusatzspalte „je 1 Stk“ | Set (Dashboard) |
| --- | --- | --- | --- |
| Top-Down E11221, Zeile E11283 (Wafer) | 1 | 0.000211, Kopf „E11221 je 1 Stk“, davor Warengruppe | 0.000211 ROL, Wert 0.249743 |
| Bottom-Up E11283, Zeilen E11221 / E11224 | 1 / 1, Summe `=SUMME(E3:E4)` | 0.000211 / 0.00022, Kopf „E11283 je 1 Stk“ | – |
| 41019 (nicht betroffen) | – | – | 94 Zeilen, Summe Wert 216.725069 wie vor FIX 26 |

## 5. Fehler dabei

Beim Anpassen des Testreports hat die Suche nach `FORM teil_b` zuerst `PERFORM teil_b` getroffen; die DATA-Zeile landete am
falschen Ort, SAP meldete „Fehler beim Aktivieren“. Der Dialog wurde mit **Abbrechen** geschlossen (nicht „Aktivieren“, siehe
LEARNINGS 2026-11-02), danach richtig eingefuegt. Derselbe Suchfehler (`form x` trifft `perform x`) war am selben Tag schon
bei ZC22 passiert; LEARNINGS 2026-11-09.
