# ZLO03: Ergebnisse haengen von der Tageszeit ab (Meldung Ann-Katrin Michel)

Stand: 2026-10-13. Status: **ungeloest, Ursache nicht belegt.** Bisher nur Auswertung der
Meldung und der Jobliste; noch keine Quelltextpruefung in P76.

## 1. Meldung

Ann-Katrin Michel (Phase-Out, Cc Philip Steiger) hat am 13.04.2026 gemeldet, dass ZLO03 zu
verschiedenen Uhrzeiten unterschiedlich grosse Ergebnisse liefert:

- Typencodes 8474/8475: einmal 18 VKNR, rund 10 Minuten spaeter 40 VKNR (13.04.2026, zwischen
  17:04 und 17:13 Uhr).
- Schon am 23.03.2026 mit Philip beobachtet, Bottom-Up auf B64404 zwischen 16:19 und 17:14 Uhr:

| Lauf | VKNR |
| --- | --- |
| KW12 | 39101, 40752, 47815, 47813, 49969 |
| KW13 (23.03.) | 39101, 40752 |
| 23.03., Lauf 2 | 39101 |
| 23.03., Lauf 3 | 39101, 40752, 47815, 47813, 49969 |

Ingos Antwort vom 14.04.2026: Der spaeteste Batchlauf laufe von 6 bis etwa 7 Uhr, danach
sollten die Mengen gleich bleiben. Das erklaert Unterschiede am spaeten Nachmittag nicht.

Ann-Katrin hat nach Ingos Ferien erneut nachgefragt: Im Programm „5 to X" soll Anfang der
Folgewoche die Datenbasis fuer Entscheide besprochen werden (Umstellung Materialstatus, Umgang
mit Mengenkontrakten). Im Repository war die Tageszeit-Abhaengigkeit bis zum 2026-10-13 nirgends
untersucht.

## 2. Jobliste vom 13.10.2026 (SM37, von Ingo, nur Benutzer KOI)

Dauer in Sekunden.

| Job | Start | Dauer | Ende |
| --- | --- | --- | --- |
| `ZLO03_MATERIALISIEREN_ALLES` | 01:00:02 | 17'005 s, etwa 4 h 43 min | etwa 05:43 |
| `ZLO03_PRECALC` | 06:00:02 | 1'926 s, etwa 32 min | etwa 06:32 |
| `Z_FERT_BEST` | 08:00:02 | 12 s | 08:00 |

**Folgerung:** Ab etwa 06:32 schreibt keiner dieser drei Jobs mehr. Waeren sie die einzigen
Schreiber der Quelltabellen, muessten zwei Abfragen um 17:04 und 17:13 gleich ausfallen. Die
Batch-Erklaerung vom April traegt also nicht.

## 3. Was ausgeschlossen ist und was nicht

- **Der Report selbst schreibt nichts**, zumindest in der lokalen Fassung
  `zlo03/ZM_LZCODE20_OPT.abap`: kein `INSERT`, `MODIFY`, `UPDATE`, `DELETE FROM` und kein
  `COMMIT WORK` auf Z-Tabellen. Gleichzeitige ZLO03-Aufrufe stoeren sich damit nicht.
  Einschraenkung: Das echte Programm hinter ZLO03 ist `Z_ZLO03_TURBO2`
  (`BEFUND_SYSTEMABGLEICH_2026-08-03.md`) und wurde dafuer nicht gelesen.
- Die Fixes 10 bis 18 (`CLAUDE.md` in diesem Ordner) betreffen Mengenformat, Nummernformat und
  „Exklusiv", nicht die Tageszeit. Sie sind ausserdem nicht in `Z_ZLO03_TURBO2` (Wochen_Todo
  Zeile 9, PM-01).
  *Nachtrag 2026-10-16:* Eine Minimalfassung von FIX 12 ist in T76 aktiv (`T76K912742`), siehe
  `MK_B63485_FIX12_2026-10-16.md`. Der Rest gilt weiter. `Z_ZLO03_TURBO2` wurde dafuer gelesen und
  schreibt keine Z-Tabellen; `ZM_LZCODE20_BG` hat in P76 nur auskommentierten Code.

## 4. Verbleibende Kandidaten (ungeprueft)

1. **Jobs anderer Benutzer.** Die Liste oben ist auf KOI gefiltert. SM37 mit Benutzer `*` und
   Start ab 08:00 zeigt, ob tagsueber etwas in die Quelltabellen schreibt.
2. **ZLO03N und ZLO03NN** starten `ZM_LZCODES20_BG` und `ZM_LZCODE20_BG`. Leeren und fuellen diese
   Programme beim Aufruf selbst die Quelltabellen (`ZPOWERBI_VC_TXT`, `ZMD04_CALC` oder die
   Ziele der Materialisierung), sieht jedes gleichzeitige ZLO03 fuer die Dauer des Laufs zu
   wenig. Das passt zu „Lauf 2 weniger, Lauf 3 wieder alles". Wahrscheinlichster Kandidat.
3. **Neu 2026-10-16, ungeprueft, starker Kandidat:** Reihenfolge der Saetze. `LT_POWERBI` kommt per
   `FOR ALL ENTRIES` und unsortiert. Ist im Bottom-Up die erste Zeile einer VKNR eine Textposition, bleibt
   deren Text leer, und ohne `P_TXTPO` loescht `DELETE gt_ktab WHERE maktx IS INITIAL` die ganze VKNR. Die
   Anzahl VKNR kann so von Lauf zu Lauf schwanken, passend zu B64404 (5, 2, 1, 5). Pruefen: in P76
   `ZPOWERBI_VC_TXT` mit `KOMPNR = B64404` auf `POSTYP = T` lesen. FIX 12 behebt das, wenn es zutrifft.
4. **Einplanung im Maerz/April war anders**, etwa ein zusaetzlicher Lauf am Nachmittag, der heute
   nicht mehr eingeplant ist. Protokolle aus dem April sind vermutlich geloescht.

## 5. Naechste Schritte

- In P76 nur lesend: Quelltexte `ZM_LZCODES20_BG`, `ZM_LZCODE20_BG`, `Z_ZLO03_TURBO2`; Programme
  in den Schritten der drei Jobs (`TBTCP`); Schreibzugriffe auf `ZPOWERBI_VC_TXT` und
  `ZMD04_CALC` suchen. Vorher `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` lesen.
- SM37 ohne Benutzerfilter ab 08:00 ansehen.
- Ann-Katrin fragen, ob sie den Effekt noch sieht und wann zuletzt.
- Ergebnis vor dem „5 to X"-Termin an Ann-Katrin geben (Wochen_Todo Zeile 94).
