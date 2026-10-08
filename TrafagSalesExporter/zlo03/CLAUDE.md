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
  (die dort beschriebene Textpositions-These wurde widerlegt: B63485 hat
  in den 8475-VKNR keine Textpositionen — die eigentliche Ursache ist
  vermutlich das KOMPNR-Format, siehe Datenfalle 2)
