# ZM_OFFENE_FAUF: Bedarfsverursacher als einblendbare Felder

Stand: 2026-10-09 (seit dem 08.10. standardmaessig eingeblendet, am 09.10. nach P76 transportiert). Wunsch Ingo: die Angaben aus MD04 > Bedarfsverursacher (Dispoelement,
Nummer, Daten zum Dispoelement) als zusaetzliche Felder im Report `ZM_OFFENE_FAUF`
(„Uebersicht offene Fertigungsauftraege"). *Ueberholt:* am 2026-10-07 standardmaessig ausgeblendet;
seit 2026-10-08 auf Wunsch Ingo standardmaessig eingeblendet und ueber das Layout ausblendbar
(`NO_OUT` entfernt, gleicher Auftrag `T76K912714`, in T76 aktiv). Ein gespeichertes Standardlayout
des Benutzers geht weiterhin vor.
**Befund 2026-10-08 (T76, LTDXD):** Fuer den Report ist das globale Layout `/ P00 TX` als Standard hinterlegt
(Ersteller TDA), daneben rund 40 weitere globale Layouts (`/ P00 …`, `/ D00 …`). Ein gespeichertes Layout legt
die Spalten fest, neue Felder erscheinen darin nicht automatisch. In ZC22 sieht man die Bedarfsverursacher
deshalb erst, wenn man sie im Layout einblendet (Spaltentexte „Dispositionselement“, „Nummer Dispoelement“ …)
und das Layout sichert. Globale Layouts nur mit Absprache der Produktion aendern. **Erledigt in T76 am 2026-10-08 (Auftrag Ingo):** ALV-Puffer
mit `BALVBUFDEL` geleert, im Standardlayout `/ P00 TX` („Tisk trafag cz“) die Spalten Dispositionselement, Nummer
Dispositionselement und Daten zum Dispoelement ans Ende gestellt und gesichert (Filter/Sortierung unveraendert).
Gegenprobe Material 63500: Planauftrag 2359802 -> `VC 410313`, `410313/000010/0001`. Die Felder stehen im
Spaltenvorrat ganz unten (Zeilen 37 bis 45 von 45), deshalb uebersieht man sie leicht. **In P76 nach dem Import von
`T76K912714` dasselbe noch einmal** (Layouts sind Mandantendaten und kommen nicht mit dem Transport).

**Nachtrag 2026-10-08 nachmittags:** Feld `ZZPEG_WERKS` (Position 59) in `ZMM_UEB_FAUF` und im Report ergaenzt,
ebenfalls im Auftrag `T76K912714`, in T76 aktiv. Im Layout `/ P00 TX` als Spalte „Werk“ eingeblendet und gesichert
(Rueckfrage „wird vollstaendig ueberschrieben“ bestaetigt). Gegenprobe Material 63500: Werk 1100. In P76 nach dem
Import ebenfalls ins Layout aufnehmen.

## Stand

| | |
|---|---|
| System | **T76**, umgesetzt und getestet 2026-10-07 |
| Transport | **`T76K912714`** (Aufgabe `T76K912715`), am 2026-10-09 von Ingo nach P76 transportiert; enthaelt Struktur `ZMM_UEB_FAUF`, Report `ZM_OFFENE_FAUF` (Quelltext und Textpool) |
| Paket | `ZLO1` |
| P76 | noch nicht; Freigabe und Import durch Ingo |
| Quelltext | `docs/abap/ZM_OFFENE_FAUF.abap` (neu), `docs/abap/ZM_OFFENE_FAUF_vorher_2026-10-07.abap` (Stand vorher, fuer Rueckbau) |

## Was geaendert ist

1. **Struktur `ZMM_UEB_FAUF`** (INTTAB, keine Datenbanktabelle) um neun Felder am Ende:

| Feld | Datenelement | Bedeutung | Beispiel |
|---|---|---|---|
| `ZZPEG_DELKZ` | `DELKZ` | Dispositionselement | `VC` |
| `ZZPEG_DELNR` | `DEL12` | Nummer Dispoelement | `0000410321` |
| `ZZPEG_DELPS` | `DELPS` | Position | `000010` |
| `ZZPEG_DELET` | `DELET` | Einteilung | `0001` |
| `ZZPEG_EXTRA` | `EXTRA` | Daten zum Dispoelement | `0000410321/000010/0001` |
| `ZZPEG_BERID` | `BERID` | Dispobereich | `1100` |
| `ZZPEG_DAT00` | `DAT00` | Zugangs-/Bedarfstermin | `01.07.2026` |
| `ZZPEG_MNG01` | `MENGEP` | Menge (DEC, bewusst kein QUAN, sonst Referenzfeld noetig) | `80` |
| `ZZPEG_ANZ` | `INT4` | Anzahl Bedarfsverursacher | `1` |
| `ZZPEG_WERKS` | `WERKS_D` | Werk des Bedarfsverursachers (`MDRQ-WERKS`), neu 2026-10-08 auf Wunsch Fabio, z. B. 1200 bei einer BANF im Werk 1200 | `1100` |

   Praefix `ZZPEG_`, damit die vielen `MOVE-CORRESPONDING` im Report die Felder nicht
   versehentlich aus MARA/AFKO/AUFK fuellen.

2. **Report `ZM_OFFENE_FAUF`:**
   * neue Checkbox `P_PEG` „Bedarfsverursacher lesen", Vorschlag an;
   * neue Form `BEDARFSVERURSACHER`, aufgerufen in `FILL_UEBAUF` nach `ERSTER_BEZUG`: ruft
     den Standardbaustein **`MD_PEGGING_NODIALOG`** (das, was MD04 > Bedarfsverursacher zeigt)
     mit `EDELKZ = 'PA'` (Planauftrag, Dispobereich aus `PLAF-BERID`) bzw. `'FE'`
     (Fertigungsauftrag), schreibt den **ersten** Verursacher in die Zeile und die Anzahl in
     `ZZPEG_ANZ`;
   * Feldkatalog: die neun Felder ohne `NO_OUT` (bis 2026-10-07 mit `NO_OUT = 'X'`), also
     sichtbar und ueber „Layout aendern" ausblendbar.

## Test in T76 (2026-10-07)

* Pruefreport `Z_PEG_TEST` (`$TMP`, nicht im Transport) mit denselben Aufrufparametern fuer zehn
  Planauftraege von Material 63500: alle `RC 0`, je ein Verursacher, z. B. Planauftrag
  `2360008` -> `VC 0000410321/000010/0001`, Dispobereich 1100, 01.07.2026, 80 ST. Das entspricht
  dem MD04-Bild „Bedarfsverursacher" aus Ingos Screenshot (P76, Planauftrag 2496913 ->
  `VC 0000417957/000010/0001`, Dispobereich 1200).
* `ZM_OFFENE_FAUF` selbst fuer Material 63500 ohne Abbruch gelaufen, Liste wie vorher.
* `Z_PEG_TEST` kann geloescht werden, sobald Ingo zustimmt.

## Offen

* **„Bestellabruf"**: von Ingo genannt, im Bedarfsverursacher-Bild keine eigene Spalte. Angenommen
  ist, dass das Dispoelement samt Nummer gemeint ist; falls ein eigenes Feld (Lieferplan/Abruf)
  gemeint ist, nachziehen.
* **Mehrere Verursacher**: nur der erste steht in der Zeile, die Anzahl daneben. Falls je
  Verursacher eine Zeile gewuenscht ist, Report umbauen.
* **Laufzeit**: der Baustein liest je Auftrag die Dispoliste; bei grossen Selektionen `P_PEG`
  ausschalten. In P76 noch nicht gemessen.
