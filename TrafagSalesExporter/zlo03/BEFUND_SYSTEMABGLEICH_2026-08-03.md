# ZLO03 — Systemabgleich T76, Befund 2026-08-03

Anlass: Frage, welches Programm Daten von `ZPOWERBI_VC` nach `ZPOWERBI_VC_TXT`
materialisiert. Antwort: **keines** — und dabei sind drei gravierendere Fehler in
`zlo03/CLAUDE.md` aufgefallen.

Alle Angaben live gelesen auf **T76 / travt762 / Mandant 100** (read-only, `SapProbe`:
`program-read`, `table-read` auf `TSTC`, `D010TAB`, `DD02L`). Kernel 793, DB HDB.

---

## 1. Transaktion ZLO03 startet NICHT `ZM_LZCODE20_OPT`

`TSTC`:

| TCODE | PGMNA |
| --- | --- |
| **ZLO03** | **`Z_ZLO03_TURBO2`** |
| ZLO03B | `ZM_LZCODE20` |
| ZLO03N | `ZM_LZCODES20_BG` |
| ZLO03NN | `ZM_LZCODE20_BG` |

`CLAUDE.md` Zeile 37 sagt „**Programm:** `ZM_LZCODE20_OPT`, Transaktion `ZLO03`".
Das ist falsch. `ZM_LZCODE20_OPT` hängt an **keiner** Transaktion.

**Warum der Irrtum entstand:** `Z_ZLO03_TURBO2` enthält intern
`REPORT zm_lzcode20_opt LINE-SIZE 132.` und im Kopfkommentar „Report ZM_LZCODE20_OPT".
Der Name *im Quelltext* stimmt also, das *SAP-Objekt* heisst anders. In diesem System ist
das ein Muster, kein Einzelfall: `Z_VC_ZLO03` und `Z_VC_ZLO03_TXT` tragen beide
`REPORT z_vc_batch.`

## 2. Die Fixes 10-18 sind NICHT im laufenden Programm

`Z_ZLO03_TURBO2` (das echte ZLO03) enthält im Kopf nur:

```
FIX 1, FIX 2, FIX 4, FIX 5
```

Nicht vorhanden: `parse_menge_str`, `norm_matnr`, `add_both_forms`, `build_komp_range`,
`refresh_ktab_kennzahlen`, `run_diagnose` — alle mit 0 Treffern. `p_diag` und `p_werks`
existieren dort nicht.

Der komplette letzte Korrekturlauf (FIX 10-18: Mengenparser, KOMPNR-Normalisierung,
KTAB-Kennzahlen, dritter Zustand `?`, Werk als Parameter, Diagnosemodus) liegt
**ausschliesslich lokal** in `zlo03/ZM_LZCODE20_OPT.abap` (94'859 Bytes) und wurde nie
ins System gebracht.

Folgen, die unmittelbar zählen:

- Ann-Katrin und Sandro haben ihre Befunde (MK-Diskrepanz B63485, CS15 42 vs. 21) an der
  Version mit **nur FIX 1/2/4/5** erhoben. Die offenen Punkte D und E im Handover
  beschreiben also das Verhalten des unkorrigierten Programms.
- Der Verifikationsplan (`CLAUDE.md` Schritt 1) ist so nicht ausführbar: `p_diag` gibt es
  auf T76 nicht.
- Vor jedem weiteren Schritt ist zu entscheiden, **wohin** die Fixes gehören —
  `Z_ZLO03_TURBO2` ist das Objekt, das die Fachbereiche starten.

### Falle: es gibt ein Objekt `ZM_LZCODE20_OPT` auf T76

`program-read ZM_LZCODE20_OPT` liefert 72'923 Bytes, `REPORT zm_lzcode20 LINE-SIZE 132.`,
grosse Teile auskommentiert, **0** Treffer für `zpowerbi_vc_txt`, **0** FIX-Marker. Das ist
eine alte Ablage, nicht die Arbeitsversion. Wer dieses Objekt bearbeitet, arbeitet am
falschen Programm — und merkt es nicht, weil der Name exakt passt.

## 3. Niemand schreibt `ZPOWERBI_VC_TXT` — und es gibt keinen Kopierschritt

`DD02L`: `ZPOWERBI_VC`, `ZPOWERBI_VC_TXT`, `ZAT_VC`, `ZAT_VC_TXT` sind alle `TRANSP`,
also echte Tabellen — `ZPOWERBI_VC_TXT` ist **kein View**.

Schreibzugriffe laut Quelltext:

| Programm | schreibt | Datenstand 2026-08-03 |
| --- | --- | --- |
| `Z_VC_ZLO03` | `ZPOWERBI_VC`, `ZSTULIMAT_VC` | `ZPOWERBI_VC` **hat Daten** |
| `Z_VC_ZLO03_TXT` | `ZAT_VC_TXT`, `ZSTULIMAT_VC` | `ZAT_VC_TXT` **leer** |
| `Z_FERT_BEST` | `ZFER_FERTBEST` | — |
| `Z_LO03_MD04_BATCH` | `ZMD04_CALC`, `ZMD04_CONTROL` | — (deckt sich mit CLAUDE.md) |
| **niemand** | **`ZPOWERBI_VC_TXT`** | **leer** |

`D010TAB` für `ZPOWERBI_VC_TXT` nennt nur lesende bzw. Diagnoseprogramme: `Z_TEST2`,
`Z_TEST_TO_PROD`, `Z_ZLO03_TURBO2`, `ZTEST3`, `Z_ZLO03_VERIFY`,
`ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CP`. Keines schreibt sie — geprüft im Quelltext. Die
Testprogramme sprechen selbst von „dem Befüller" bzw. „dem Materialisierer".

`D010TAB` ist für diese Frage verlässlich: die Gegenprobe auf `ZAT_VC_TXT` liefert korrekt
genau `Z_VC_ZLO03_TXT`.

**Es gibt keinen Kopierschritt `ZPOWERBI_VC` -> `ZPOWERBI_VC_TXT`.** `Z_VC_ZLO03` und
`Z_VC_ZLO03_TXT` sind zwei Kopien **desselben** Generators (beide `REPORT z_vc_batch.`,
identischer Kopf „VC3-Optimiertes Programm für Stücklistenverarbeitung"). Jede Kopie löst
Stücklisten per `CS_BOM_EXPL_MAT_V2` neu auf und schreibt in ihre **eigene** Zieltabelle;
`ZAT_VC_TXT` ist schlicht das Ziel der zweiten Kopie. Der Unterschied der Kopien ist
inhaltlich: `Z_VC_ZLO03_TXT` trägt zusätzlich FIX 3/4/7/8 für **Textpositionen**
(`delnl = ' '`, damit Typ T nicht gelöscht wird; `idnrk` aus `POTX1`).

### Konsequenz für „Tabelle ist noch leer, warte..."

Warten hilft auf T76 nicht: kein Job schreibt `ZPOWERBI_VC_TXT`. Wer sie füllen will, muss
entweder `Z_VC_ZLO03_TXT` wieder auf `ZPOWERBI_VC_TXT` zeigen lassen statt auf
`ZAT_VC_TXT`, oder das lesende Programm auf `ZAT_VC_TXT` umstellen — und dann den
Generator laufen lassen (`ZAT_VC_TXT` ist ebenfalls leer, der TXT-Lauf ist also auch dort
nicht durch).

Nicht geprüft und offen: ob `ZPOWERBI_VC_TXT` auf dem **Produktivsystem** (travp762)
befüllt wird. Der Befund oben gilt für T76.

---

## Offene Entscheidungen (fachlich, nicht technisch)

1. Sollen FIX 10-18 nach `Z_ZLO03_TURBO2` portiert werden — dem Objekt, das ZLO03
   wirklich startet?
2. Soll `Z_VC_ZLO03_TXT` auf `ZPOWERBI_VC_TXT` zurückgezeigt werden, oder ist
   `ZAT_VC_TXT` das gewollte neue Ziel?
3. `CLAUDE.md` korrigieren: Programmname (Zeile 37), Befüller-Aussage (Zeile 40) und die
   davon abgeleiteten offenen Punkte A und D (Zeilen 137, 166 verweisen auf
   `Z_VC_ZLO03_TXT` als Ursache für fehlende Sätze in `ZPOWERBI_VC_TXT` — dieses Programm
   schreibt diese Tabelle nicht).

## Reproduzierbar

```
SapProbe.exe table-read TSTC     --fields TCODE,PGMNA --where "TCODE LIKE 'ZLO%'"
SapProbe.exe table-read D010TAB  --fields MASTER,TABNAME --where "TABNAME = 'ZPOWERBI_VC_TXT'"
SapProbe.exe table-read D010TAB  --fields MASTER,TABNAME --where "TABNAME = 'ZAT_VC_TXT'"
SapProbe.exe table-read DD02L    --fields TABNAME,TABCLASS --where "TABNAME LIKE 'ZPOWERBI%' OR TABNAME LIKE 'ZAT_VC%'"
SapProbe.exe program-read Z_ZLO03_TURBO2
SapProbe.exe program-read Z_VC_ZLO03
SapProbe.exe program-read Z_VC_ZLO03_TXT
```
