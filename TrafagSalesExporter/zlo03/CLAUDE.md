# ZLO03 / ZM_LZCODE20_OPT — Arbeitskontext

Übergabe aus einer Chat-Session vom 27.07.2026. Ziel dieser Datei: Claude Code
kann ohne Rückfragen weiterarbeiten und die offenen Punkte direkt gegen das
SAP-System verifizieren.

---

## Harte Regeln

**Klassische ABAP-Syntax.** Keine Inline-Deklarationen (`DATA(x) = ...`), kein
`VALUE #( )`, kein `REDUCE`/`FOR`, keine String-Templates `|...|`. Das Programm
läuft auf S4CORE 108, aber der Bestand ist bewusst abwärtskompatibel gehalten.
Alle `DATA`- und `TYPES`-Deklarationen an den Anfang der jeweiligen FORM.

**Diagnose vor Code.** Keine Produktionsänderung ohne vorher gelesene echte
Systemdaten. Wenn eine Annahme nicht durch eine Abfrage gedeckt ist, erst
abfragen, dann schreiben.

**Kleine Diffs.** Bei kleinen Änderungen gezielte Patches statt kompletter
Neuausgabe des Programms.

**Nichts stillschweigend mitfixen.** Jede Änderung bekommt einen
`*** FIX n:`-Kommentar mit Begründung, warum das alte Verhalten falsch war.

---

## System und Objekte

| | |
|---|---|
| System | travt762, S4CORE 108, SAP_BASIS 758 |
| Test | T76/100 |
| Transportziel | B76 |
| Werk | 1100 (seit FIX 17 Selektionsparameter `p_werks`) |

**Programm:** `ZM_LZCODE20_OPT`, Transaktion `ZLO03`
*Ueberholt/zu pruefen (2026-10-08):* In T76 startet die Transaktion ZLO03 laut `TSTC` das Programm
`Z_ZLO03_TURBO2`. P76 ist nicht geprueft. Siehe `zlo03/EXKLUSIV_LOGIK_2026-10-08.md`.
**Quelltabellen:** `ZPOWERBI_VC_TXT`, `ZMD04_CALC`, `MARA`, `MAKT`, `MARC`,
`MBEW`, `MAST`
**Befüllt durch:** `Z_VC_ZLO03_TXT` (nächtlich) → `ZPOWERBI_VC_TXT`,
`Z_LO03_MD04_BATCH` → `ZMD04_CALC`

---

## Fachlicher Hintergrund

Ann-Katrin Michel und Sandro Moltisanti nutzen ZLO03 im Phase-Out-Prozess:

1. **Top-Down** auf ein Gerät → welche Komponenten sind exklusiv?
2. Für exklusive Komponenten → Mengenkontrakte kündigen
3. **Bottom-Up** auf die Komponente → Gegenprobe

Daraus folgt die Risikorichtung: Ein fälschlich gesetztes `Exklusiv = X` führt
dazu, dass ein Mengenkontrakt gekündigt wird, obwohl das Teil weiter gebraucht
wird. Falsch-negativ ist harmlos, falsch-positiv nicht.

---

## Zwei Datenfallen, die alles erklären

### 1. MENGE ist CHAR 40

`ZPOWERBI_VC_TXT-MENGE` ist mit dem Datenelement `SOBJID` typisiert
(CHAR 40, kein Konvertierungsexit), Inhalt z. B. `1.000`.

Die alte Zuweisung `lv_menge_in = lv_str` interpretiert den String nach der
**Dezimaldarstellung des Benutzers** (SU3). Bei `1.234.567,89` wird `1.000` zu
**tausend**, bei `1,234,567.89` zu **eins**. Derselbe Report liefert je nach
Benutzer Mengen um Faktor 1000 daneben — ohne Fehlermeldung.

→ gefixt durch `FORM parse_menge_str` (FIX 10).

### 2. KOMPNR und MATNR haben unterschiedliche Domänen

In **derselben Tabelle**:

| Feld | Datenelement | Konvertierungsexit | Speicherung |
|---|---|---|---|
| `MATNR` | MATNR | MATN1 | mit führenden Nullen |
| `KOMPNR` | SOBJID | keiner | wie geschrieben |
| `KOM_MSTAE` | MATNR | MATN1 | enthält faktisch einen Materialstatus |
| `MAT_MSTAV` | SOBJID | keiner | enthält faktisch einen Materialstatus |
| `STUFE` | MATNR | MATN1 | enthält faktisch eine Stufennummer |

Die Tabelle ist offenbar per Feldkopie entstanden. Jeder Join `KOMPNR` gegen
`MARA-MATNR` oder `ZMD04_CALC-MATNR` läuft damit potenziell gegen ein anderes
Format. Bei **numerischen** Komponentennummern trifft er dann nichts — bei
alphanumerischen wie `B63485` schon. Deshalb ist es nie aufgefallen: die von
Hand geprüften Komponenten sind PCBAs mit Buchstabenpräfix.

→ gefixt durch `FORM norm_matnr` / `add_both_forms` / `build_komp_range`
(FIX 11).

---

## Angewandte Fixes

Alle im Quelltext mit `*** FIX n:` markiert.

| Nr | Bereich | Kurz |
|---|---|---|
| 10 | `convert_menge` | deterministischer Mengenparser statt impliziter Zuweisung |
| 11 | global | KOMPNR-Normalisierung, DB-Zugriffe über beide Schreibweisen |
| 12 | `process_main`, neu `refresh_ktab_kennzahlen` | KTAB wurde nur beim ersten Treffer gefüllt; war das eine Textposition, blieben alle Kennzahlen 0 |
| 13 | `process_main` | `CLEAR` vor `READ TABLE ... INTO ls_stamm_v` |
| 14 | `process_main` | `DELETE ... WHERE maktx IS INITIAL` → `WHERE postyp = 'T'` |
| 15 | `load_exclusivity_topdown` | dritter Zustand `?` für nicht entscheidbare Fälle |
| 16 | `ausgabe_spalten_details` | `sy-subrc` nach jedem READ einzeln sichern |
| 17 | Selektionsbild | Werk als Parameter statt Konstante |
| 18 | neu `run_diagnose` | Diagnosemodus `p_diag` |
| 19 | `Z_ZLO03_TURBO2`: neu `load_compliance_cache`, `get_compliance`, `create_excel_output` | Spalten REACH, SVHC, PFAS-Gehalt aus `Z_PACK_REGULATION` (2026-10-20), Nachtrag Spalte RoHS aus Merkmal `ROHS` (2026-10-22); T76 aktiv, Auftrag `T76K912742`, siehe `FIX19_STOFFCOMPLIANCE_SPALTEN_2026-10-20.md` |
| 20 | `Z_ZLO03_TURBO2`: `download_dialog` | Download als `.txt` und Oeffnen mit Excel statt `.xls`: Excel blockiert Tab-Text unter `.xls` (Gruppenrichtlinie `ExtensionHardening = 2`), Befund 2026-10-22; T76 aktiv, Auftrag `T76K912742`, siehe `FIX20_DOWNLOAD_TXT_2026-10-22.md` |
| 21 | `Z_ZLO03_TURBO2`: `download_dialog`, neu `build_xlsx` | echte `.xlsx` statt Tab-Text (Wunsch Ingo 2026-10-22); T76 aktiv, neuer Auftrag `T76K912752` (`T76K912742` war schon freigegeben), zellgleich mit dem Text-Inhalt; Quelltext `Z_ZLO03_TURBO2_FIX21_XLSX_2026-10-22.abap` |
| 22 | `Z_ZLO03_TURBO2`: Typen, `load_stammdaten_cache`, `fill_ktab`, `create_excel_output`, `build_xlsx` | Top-Down mit Spalten Disponent (`MARC-DISPO`) und Warengruppe (`MARA-MATKL`), als Text (Wunsch Armin 2026-10-29); T76 aktiv, neuer Auftrag `T76K912777` (`T76K912752` war schon freigegeben), siehe `FIX22_DISPO_MATKL_2026-10-29.md` |
| 23 | `Z_ZLO03_TURBO2`: neu `load_bom_units`, `bom_unit_menge`, `apply_bom_unit` | Top-Down: Mengen und Stueckkosten in der Einheit der Stueckliste ueber MARM statt `UNIT_CONVERSION_SIMPLE` 1:1, Bottom-Up: Komponentenspalte ebenso (H05039: Basis KAR = 600 ST, Preis je KAR, Bedarf in ST; Meldung Armin 2026-10-29); T76 aktiv, Auftrag `T76K912781`, siehe `FIX23_EINHEIT_STUECKLISTE_2026-10-29.md` |

Ältere Fixes 1, 2, 4, 5 stammen aus einer früheren Runde und sind unverändert.

### Verhaltensänderung, die kommuniziert werden muss

Die Spalte **Exklusiv** kann jetzt drei Werte haben:

- `X` — gesichert exklusiv, Kontrakt kann gekündigt werden
- `?` — Komponente hat Nicht-FERT-Eltern außerhalb der Selektion, manuell prüfen
- leer — nicht exklusiv

Vorher stand in den ersten beiden Fällen `X`. Ann-Katrin muss das wissen, bevor
sie die nächste Auswertung interpretiert.

---

## Offene Punkte

### A) Elternmaterial-Spalte ist funktionslos

`load_elternmaterial_cache` liest `ZPOWERBI_VC_TXT-KOM_MSTAE` und schreibt es
als Elternmaterialnummer. Das Feld enthält laut Datenbestand aber einen
**Materialstatus** und ist in allen geprüften Sätzen leer. Die Spalte
„Elternmaterial" im Top-Down bleibt dadurch immer leer.

`ZPOWERBI_VC_TXT` hat kein Feld für die übergeordnete Baugruppe. Eine korrekte
Ermittlung braucht entweder eine Tabellenerweiterung in `Z_VC_ZLO03_TXT` oder
eine Auflösung über `STPO`/`MAST`.

**Bewusst nicht gefixt.** Erst mit dem Fachbereich klären, was in der Spalte
stehen soll.

### B) Exklusivität ist nicht rekursiv

`load_exclusivity_topdown` betrachtet nur direkte Verwendungen. Eine Komponente,
die über eine HALB-Baugruppe in nicht selektierten Geräten läuft, wird als `?`
markiert statt korrekt aufgelöst. FIX 15 ist eine ehrliche Notlösung, keine
Antwort. Eine saubere Lösung braucht eine rekursive Auflösung über
`ZPOWERBI_VC_TXT-STUFE` oder `STPO`.

### C) Zeilen 2 und 3 der Excel-Ausgabe sind kürzer

Kopfzeile und Datenzeilen haben deutlich mehr Spalten als die MSTAE- und die
Verbrauchszeile. Aktuell folgenlos, weil Excel fehlende Tabs toleriert.
Unverändert gelassen.

### D) 12 statt 21 VKNR im Bottom-Up

Datei `b63485-1.xlsx`: Report liefert 12 Zeilen, der transponierte Block listet
21 VKNR. Fehlend: `46105, 46123, 46124, 46125, 46126, 46127, 46128, 46130,
46835`.

Kandidaten in dieser Reihenfolge:
1. Löschvormerkung (`p_lvorm` ist per Default aus)
2. `menge = ''` oder `mengeneinheit = ''` in Schritt 2
3. Sätze fehlen in `ZPOWERBI_VC_TXT` → Problem liegt in `Z_VC_ZLO03_TXT`

### E) CS15 zeigt 42, ZLO03 zeigt 21

Nicht vergleichbar aufgebaut:

| | CS15 | ZLO03 |
|---|---|---|
| Quelle | STPO/MAST direkt | `ZPOWERBI_VC_TXT`, VC-aufgelöst |
| Werk | wählbar | `p_werks` |
| Materialart | alle | `FERT` bei Exklusivität und „Weitere VKNR" |
| Löschvormerkung | wählbar | `p_lvorm`, Default aus |
| Positionen ohne Menge | enthalten | ausgefiltert |
| Materialstatus 99 | enthalten | bei „Weitere VKNR" ausgefiltert |

**Fehlende Eingabe:** Sandros CS15-Einstellungen — Werk, Stichtag,
Stücklistenverwendung, ein- oder mehrstufig. Ohne die vier Werte wird gegen
eine geratene Einstellung verglichen. Muss erfragt werden.

### F) Ergebnisse haengen von der Tageszeit ab (seit 2026-10-13 erfasst)

Ann-Katrin meldet seit Maerz 2026 unterschiedlich grosse Ergebnisse zu verschiedenen Uhrzeiten
am Nachmittag. Die Batch-Jobs enden laut SM37 um etwa 06:32 und erklaeren das nicht. Ungeloest,
Kandidaten und naechste Schritte in `TAGESZEIT_ABHAENGIGKEIT_2026-10-13.md`.

### G) Top-Down-Pruefung vom 2026-10-29 (fuer FIX 24, noch nicht umgesetzt)

Nach FIX 23 gefragt (Ingo: „notieren, spaeter machen“). Belegt in T76, Quelltext Stand FIX 23:

1. **Stueckkosten auf 2 Nachkommastellen gerundet** (`ty_ktab-stueckkosten TYPE p DECIMALS 2`, in `fill_ktab` STPRS bzw.
   VERPR / PEINH). Alle Wert-Spalten (`wert_feste_zug` bis `wert_endbestand`) rechnen mit dem gerundeten Preis. Bei PEINH > 1
   falsch: B64003 0.01 je 1000 -> 0.00, alle Werte 0; AK70879 10.31 je 100 -> 0.10 statt 0.1031 (rund 3 % zu tief).
   Loesung: Werte aus dem ungerundeten Preis, Stueckkosten mit mehr Stellen ausgeben (wie `stk_bom` in FIX 23).
2. **Parameter `p_days` („Verbraeuche zurueck in Tagen“, 360) wirkt nicht:** setzt nur `gv_datum_von`, das nirgends gelesen
   wird; der Verbrauch kommt fest aus `ZMD04_CALC` (Zeitraum des Nachtjobs). Loesung: Feld ausblenden oder Zeitraum des Jobs
   nennen; Verbrauch je eingegebenem Zeitraum selbst rechnen waere ein groesserer Umbau.
3. **Textpositionen im Bedarf** (`process_main`, MTAB: `IF postyp <> 'T' OR p_txtpo = 'X'` vor `COLLECT`): mit gesetztem
   `p_txtpo` zaehlt die Menge einer Textposition zum Bedarf; steht eine Komponente als L- und T-Position im selben Geraet
   (Fall B63485), steigt der Bedarf allein durch das Kaestchen. Loesung: T-Positionen nie in MTAB summieren.
4. **Verdacht Mehrstufigkeit:** in 41019 steht D85020 auf Stufe 02 und 03 (je 1 ST), COLLECT ergibt 2 je Geraet. Ohne
   Elternfeld in `ZPOWERBI_VC_TXT` (Punkt A) nicht entscheidbar, ob doppelt oder richtig; ausserdem gilt eine Menge ab Stufe 2
   je uebergeordneter Baugruppe, nicht je Geraet (bei Mengen > 1 zu wenig Bedarf). Passt zu Punkt E (CS15 42 vs. ZLO03 21).
   Naechster Schritt: 41019 lesend gegen CS12 (mehrstufig) in T76 vergleichen, erst dann aendern.
5. Kommentar in FIX 22 („Werk p_werks“) ist falsch: `Z_ZLO03_TURBO2` kennt kein `p_werks`, das Werk ist die Konstante
   `gc_werks = '1100'` (FIX 17 gab es nur in `ZM_LZCODE20_OPT`). Beim naechsten Fix berichtigen.

---

## Verifikationsplan

### Schritt 1 — Diagnoselauf

```
ZLO03, p_diag = 'X'
  Lauf A: s_matnr = B63485, p_botu = 'X', p_werks = 1100
  Lauf B: s_matnr = 8475,   p_topd = 'X', p_werks = 1100
```

Erzeugt keine Datei. Ausgabe beantwortet:

- Abschnitt 1: Komponenten ohne MARA-Treffer nach Normalisierung
  → `> 0` bestätigt das Formatproblem
- Abschnitt 2: MK, Lagerbestand, Stückkosten, Postyp, Exklusiv je Zeile
  → in Lauf B muss für `B63485` ein MK-Wert stehen
- Abschnitt 3: Duplikate in `ZMD04_CALC` über alle Werke
  → `> 0` heißt, der HASHED-INSERT in `load_md04_bulk` verwirft still Sätze
- Abschnitt 4: Filterverluste Bottom-Up
- Abschnitt 5: Parser-Gegenprobe, muss für jeden Benutzer identisch sein

### Schritt 2 — direkte Abfragen

Falls SAP-Zugriff verfügbar, diese Abfragen laufen lassen und die Ergebnisse
gegen die Erwartung stellen:

```sql
-- 1) MK-Satz für B63485 vorhanden?
SELECT matnr, werks, mkmng, omkwr, labst, calc_date
  FROM zmd04_calc WHERE matnr LIKE '%B63485%'

-- 2) Duplikate in ZMD04_CALC
SELECT matnr, COUNT(*) FROM zmd04_calc
  GROUP BY matnr HAVING COUNT(*) > 1

-- 3) Schreibweise KOMPNR vs. MATNR
SELECT DISTINCT kompnr, LENGTH(kompnr) FROM zpowerbi_vc_txt
  WHERE kompnr LIKE '%63485%'
SELECT matnr, LENGTH(matnr) FROM mara WHERE matnr LIKE '%63485%'

-- 4) Die neun fehlenden VKNR
SELECT matnr, mtart, lvorm, mstae, zztyp_f4 FROM mara
  WHERE matnr IN ('46105','46123','46124','46125','46126',
                  '46127','46128','46130','46835')

-- 5) Deren Sätze in ZPOWERBI_VC_TXT
SELECT matnr, kompnr, menge, mengeneinheit, postyp
  FROM zpowerbi_vc_txt
  WHERE kompnr LIKE '%B63485%'

-- 6) Nicht-FERT-Eltern von B63485 (Beleg für Punkt B)
SELECT DISTINCT v.matnr, m.mtart, m.mstae
  FROM zpowerbi_vc_txt AS v
  JOIN mara AS m ON m.matnr = v.matnr
  WHERE v.kompnr LIKE '%B63485%'
```

### Schritt 3 — Syntaxcheck

Zwei Stellen sind ungeprüft und am ehesten kritisch:

- `FORM build_komp_range` mit `CHANGING pt_range TYPE ANY TABLE` und lokal
  deklarierter Range-Struktur
- die beiden `SELECT ... INTO @lt_...` in `load_exclusivity_topdown`, wo
  strenge und klassische Syntax nebeneinanderstehen

### Schritt 4 — Regressionstest

Vor/Nachher-Vergleich auf denselben Selektionen:

| Fall | Selektion | Erwartung |
|---|---|---|
| 1 | `B63485` Bottom-Up | mehr als 12 Zeilen, oder begründet weiterhin 12 |
| 2 | `8475` Top-Down | `B63485` mit MK ≠ 0 |
| 3 | eine rein numerische Komponente Bottom-Up | vorher vermutlich leer, jetzt Treffer |
| 4 | beliebig, zwei Benutzer mit unterschiedlicher SU3-Dezimaldarstellung | identische Mengen |
| 5 | Top-Down mit Textpositionen, `p_txtpo` aus und an | Kennzahlen unverändert |

Fall 4 ist der wichtigste — er ist der einzige, der FIX 10 wirklich beweist.

---

## Beteiligte

- **Ann-Katrin Michel** — Fachanwenderin Phase-Out, hat die MK-Diskrepanz
  gemeldet (B63485: Bottom-Up 3100 Stk, Top-Down 0)
- **Sandro Moltisanti** — hat CS15 42 vs. ZLO03 21 gemeldet
- **Ingo Kohler** — Entwicklung

---

## Dateien

- `ZM_LZCODE20_OPT.abap` — vollständiges korrigiertes Programm
- `ZM_LZCODE20_OPT_fixes.md` — frühere Fix-Beschreibung, teils überholt
  (*Überholt am 2026-10-16:* Hier stand, die Textpositions-These sei widerlegt und
  die Ursache vermutlich das KOMPNR-Format. Beides falsch: B63485 hat in P76 vier
  Textpositionen in 46123/46124/46126/46128, und die These stimmt. Siehe
  `MK_B63485_FIX12_2026-10-16.md`)
- `FIX19_STOFFCOMPLIANCE_SPALTEN_2026-10-20.md` — **FIX 19 in `Z_ZLO03_TURBO2`** (REACH/SVHC/PFAS, seit 2026-10-22 auch RoHS), liegt seit 2026-10-20
  im selben Auftrag `T76K912742` wie FIX 12
- `FIX20_DOWNLOAD_TXT_2026-10-22.md` — **FIX 20 in `Z_ZLO03_TURBO2`** (Download `.txt` statt `.xls`, Excel-Sperre);
  aktueller Quelltext `Z_ZLO03_TURBO2_FIX20_2026-10-22.abap` (FIX 12, 19 inkl. RoHS, 20)
- `MK_B63485_FIX12_2026-10-16.md` — Beleg aus P76 und **FIX 12 in `Z_ZLO03_TURBO2`**
  (T76 aktiv, Transport `T76K912742`); die übrigen Fixes sind weiter nur lokal
  *(ueberholt am 2026-10-29: FIX 12, 19, 20 in `T76K912742`, freigegeben 22.10.; FIX 21 in `T76K912752`, freigegeben;
  FIX 22 in `T76K912777`, offen)*
- `FIX22_DISPO_MATKL_2026-10-29.md` — **FIX 22**: Top-Down mit Disponent und Warengruppe, aktueller Quelltext
- `FIX23_EINHEIT_STUECKLISTE_2026-10-29.md` — **FIX 23**: Top-Down in Stuecklisten-Einheit (MARM), Quelltext `Z_ZLO03_TURBO2_FIX23_EINHEIT_2026-10-29.abap`, Auftrag `T76K912781`
  `Z_ZLO03_TURBO2_FIX22_DISPO_MATKL_2026-10-29.abap`, Auftrag `T76K912777`
