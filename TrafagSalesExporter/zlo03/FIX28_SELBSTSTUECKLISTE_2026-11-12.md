# ZLO03 FIX 28: Zeilen aus Selbst-Stuecklisten nicht doppelt zaehlen (2026-11-12)

Stand: 2026-11-12. T76 aktiv, neuer Auftrag **`T76K912902`** (Aufgabe `T76K912903`), nicht freigegeben, nicht in P76.
Zurueck: `zlo03/CLAUDE.md`. Grundlage: `FIX26_BASISMENGE_2026-11-05.md` Abschnitt 6 (Befund FP_THERMOST, dort als eigener
Fix offen) und Befund G4 in `zlo03/CLAUDE.md`.

## 1. Anlass

Mail Armin Güntensperger 12.11.2026 12:41 „ZLO03_Fehler in der Stücklistenmenge“, Anhang `ZLO03_Mengenauflösung_20261112.xlsx`
(P76, Top-Down): D13006 „Schalterabdeckung T1319/1“ steht in jedem Geraet mit **2** statt 1, Spalte Elternmaterial
„D15066,D15172,**D13006**,D15167“. Auftrag Ingo 12.11.: „also muss der fix bereinigt werden neue version“, Plan freigegeben
mit „setze den fix um in sap“.

## 2. Ursache (belegt in T76, P76 nur aus Armins Datei)

1. **Selbst-Stuecklisten.** D13006 hat in Werk 1100 die Stueckliste 00044813 (Verwendung 3, angelegt 14.02.2025), deren
   einzige Position D13006 selbst ist (1 ST, `STPO-REKRS = X` „Rekursiv erlaubt“). Ebenso D15009 (00045448), E01752
   (00045449), K60313 (00044412) und viele weitere. In T76 tragen 286 Stuecklistenpositionen `REKRS = X`; bis 2023 wenige je
   Jahr, 2024: 49, 2025: 173, 2026: 19 (ab Oktober 2024 gehaeuft).
2. **Der Nachtjob steigt eine Stufe hinein.** `ZPOWERBI_VC_TXT` fuer 5501: D13006 auf Stufe 02 unter D15167 (ID 00051410) und
   auf Stufe 03 mit `KOM_MSTAE` = D13006 und `MAT_MSTAE` = 00051410, also unter sich selbst. Dasselbe Muster bei D85020 in
   41019 (Befund G4 vom 29.10.: Stufe 03 unter D85020).
3. **ZLO03 summiert seit FIX 2 jede Zeile.** Versionshistorie `Z_ZLO03_TURBO2` in T76 (`SVRS_GET_REPS_FROM_OBJECT`):
   Versionen 1 bis 10 (01.12.2025 bis 02.03.2026) entfernen Duplikate je Geraet und Komponente
   (`DELETE ADJACENT DUPLICATES ... COMPARING matnr kompnr`), D13006 ergab dort zufaellig 1. Ab Version 11 (Auftrag
   `T76K911787` „ZLO03 Materialisierung textpos“, freigegeben 10.03.2026) ist das als FIX 2 entfernt, weil es echte
   Mehrfachverwendungen verschluckte; seitdem zaehlt die Zusatzzeile mit. Wann `T76K911787` in P76 importiert wurde, ist
   von T76 aus nicht sichtbar.

## 3. Aenderung (`*** FIX 28` in `Z_ZLO03_TURBO2`)

Quelltext `zlo03/Z_ZLO03_TURBO2_FIX28_2026-11-12.abap` (Vorstand FIX 27, gegen den T76-Live-Stand vorher zeilengleich
gelesen; 59 Zeilen dazu, 5 geaendert).

| Stelle | Aenderung |
| --- | --- |
| Kopf „FIXES APPLIED“ | Zeile FIX 28 |
| neu `FORM ist_selbstzeile` | X, wenn `KOM_MSTAE` (MATN1-Ausgabeform) = `KOMPNR`, verglichen ohne Leerzeichen und fuehrende Nullen; Stufe 01 hat `KOM_MSTAE` leer und ist nie betroffen |
| `process_main` | Kennzeichen je Zeile nach dem FIX-26-Block; Top-Down `COLLECT` in MTAB und Bottom-Up MTAB-Zuordnung nur ohne Kennzeichen. Damit stimmen VKNR-Spalten, Spalten „je 1 Stk“ und `app_export`/`ZloTopDownSet` |
| `load_bom_units` (FIX 23) | Selbstzeilen zaehlen nicht bei der Wahl der Stuecklisten-Einheit (H20201: die Selbstzeile in KG ueberstimmte die echte Position in MM, 115 MM wurden als 115 KG gerechnet) |
| `load_elternmaterial_cache` | das Material selbst nicht als eigenes Elternmaterial (D13006: „D15167“ statt „D13006,D15167“) |

Unveraendert: Exklusivitaet, „Weitere VKNR“, `load_bmeng` (zaehlen nur verschiedene Geraete bzw. Eltern), Summieren echter
Mehrfachverwendungen (FIX 2).

## 4. Einspielen (eigene T76-Sitzung `/app/con[0]/ses[1]`, ohne Passwort)

| Schritt | Ergebnis |
| --- | --- |
| Ist-Stand | `abap-read` T76 gegen `Z_ZLO03_TURBO2_FIX27_2026-11-09.abap`: zeilengleich (4'379 Zeilen) |
| Quelltext | im SE38-Editor ersetzt, Sichern; beim Oeffnen kam keine Auftragsabfrage, erst beim Sichern |
| Auftrag | `SapGuiAuftragNeu.vbs`: **`T76K912902`** „ZLO03 FIX 28 Selbststueckliste nicht doppelt zaehlen“ (`T76K912839` war freigegeben) |
| Syntaxpruefung | nur die bekannte Warnung „Regex-Standard POSIX abgekuendigt“ (FIX 21, jetzt Zeile 4075), keine Fehler |
| Aktivieren | nur `Z_ZLO03_TURBO2` markiert (fremdes inaktives `Z_REICHW` uebergangen), „Objekt(e) wurde(n) aktiviert“, kein Fehlerdialog; `REPOSRC` nur Status A (12.11.2026 13:33:53) |
| Rueckgelesen | `abap-read` gegen die Repository-Datei: identisch, 10 Stellen `FIX 28`; `E071`: Aufgabe `T76K912903` enthaelt `LIMU REPS Z_ZLO03_TURBO2`, beide Auftraege Status D |
| ST22 | heute 0 Laufzeitfehler |

## 5. Tests in T76 vorher/nachher

Testreport `Z_KOI_ZLO_FIX28_TEST` (`$TMP`, Quelltext `zlo03/Z_KOI_ZLO_FIX28_TEST.abap`, nach dem Test in T76 geloescht):
Teil A ruft `ZloTopDownSet` ueber `ZCL_ZPOWERBI_EINKAUF_DPC_EXT` (derselbe `SUBMIT ... P_APP` wie das Gateway), Teil B
startet ZLO03 als Hintergrundjob und vergleicht den Excel-Inhalt Zelle fuer Zelle. Vorher-Lauf auf FIX 27, Nachher-Lauf auf
FIX 28; die Vergleichsdateien unter `/usr/sap/trans/zlo03_fix28_*` sind wieder geloescht.

| Fall | Vorher (FIX 27) | Nachher (FIX 28) | Bewertung |
| --- | --- | --- | --- |
| Set 5501 | 59 Zeilen, Summe Wert 68.380130; D13006, D15009, K60313, D15184, E01574, E01502, E00613, E01752, E01600, K70758, D15011 je 2 ST | 59 Zeilen, Summe **60.061700**; dieselben je **1 ST** | ✓ |
| Set 41019 | Summe 216.725069, D85020 2 ST | Summe 208.778469, D85020 **1 ST** | ✓ (G4-Fall) |
| Set 63759 | Summe 1'952.220930; H20201 116 KG zu 15.65 = **1'815.40**; H17500 1.02 M = 29.07 | Summe **95.993180**; H20201 **115 MM** zu 0.00042 = **0.0483**; H17500 **0.02 M** = 0.57 | ✓ (Befund FP_THERMOST, FIX 26 Abschnitt 6) |
| Set E11221 (FIX 26) | E11283 0.000211 ROL, Wert 0.249743 | unveraendert | ✓ |
| Excel Top-Down 5501 | 62 Zeilen | 62 Zeilen, **45 Zellen anders**: 15 Komponenten je VKNR-Spalte, „je 1 Stk“ 2 → 1 und Elternmaterial ohne das Material selbst; sonst nichts | ✓ |
| Excel Top-Down 63759 | 77 Zeilen | 77 Zeilen, 93 Zellen anders: 28 Komponenten, jeweils nur die Selbstzeile weg (E01518, K15019, R15046 3 → 2, weil die echte Position Menge 2 hat; E00203 1.07 → 0.07); bei jeder geaenderten Zeile stand vorher das Material selbst in der Elternspalte | ✓ |
| Excel Bottom-Up D13006 | 1'955 Zeilen | 1'955 Zeilen, in 58 Geraeten 2 → 1 (Spalte D13006 und „je 1 Stk“), sonst nichts | ✓ |

**Neben dem Ziel, aber gewollt:** H20201 steht in 63759 jetzt in der Stuecklisten-Einheit MM (FIX 23). Bestand, Zu- und
Abgaenge, Verbrauch und Losgroesse stehen damit ebenfalls in MM (Lagerbestand 634.667 KG = 23'673'079.1 MM), die
Stueckkosten Lager je MM (0.00042). Die Werte in CHF aendern sich dadurch nicht. Das ist dieselbe Regel wie bei H05039
(ST statt KAR), fuer Leser aber ungewohnt.

**Echte Mehrfachverwendungen bleiben summiert, mit Daten belegt:** 40738 (K15030, G4) liefert in T76 keine Zeilen
(Loeschvormerkung) und war nicht pruefbar. Stattdessen `ZPOWERBI_VC_TXT` fuer 63759 gelesen: H05140 (Stufe 01 19 MM und
unter D70268 2 MM), K16515 (unter U47127 und unter R14221) und K51001 (ebenso) haben je zwei L-Zeilen unter verschiedenen,
nicht eigenen Eltern. Keine der drei steht in der Vorher/Nachher-Differenz, sie werden also weiter summiert. E01518, K15019
und R15046 (3 → 2) haben dagegen eine echte Position mit Menge 2 und eine Selbstzeile.

## 6. Offen

1. **Ingo:** `T76K912902` freigeben und nach P76 importieren.
2. Danach in P76: ZLO03 Top-Down auf eines von Armins Geraeten (z. B. 5501) laufen lassen, D13006 muss 1 zeigen.
3. Danach den Einkauf-Lauf neu starten: der Materialanteil je Produktgruppe (`ZloTopDownSet`) sinkt, wo Selbst-Stuecklisten
   vorkommen; FP_THERMOST. besonders (H20201 1'815 CHF je Geraet fallen weg).
4. Antwort an Armin (und Hinweis an Ann-Katrin, Sandro), sobald P76 importiert ist; Entwurf auf Wunsch als `.eml`.
5. Fachlich klaeren (nicht Teil von FIX 28): ob die Selbst-Stuecklisten (Verwendung 3, „Rekursiv erlaubt“) gewollt sind.
   FIX 28 haengt davon nicht ab.
