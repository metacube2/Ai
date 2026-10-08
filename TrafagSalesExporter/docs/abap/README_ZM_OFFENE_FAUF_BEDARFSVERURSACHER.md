# ZM_OFFENE_FAUF: Bedarfsverursacher als einblendbare Felder

Stand: 2026-10-08 (seit dem 08.10. standardmaessig eingeblendet, siehe unten). Wunsch Ingo: die Angaben aus MD04 > Bedarfsverursacher (Dispoelement,
Nummer, Daten zum Dispoelement) als zusaetzliche Felder im Report `ZM_OFFENE_FAUF`
(„Uebersicht offene Fertigungsauftraege"). *Ueberholt:* am 2026-10-07 standardmaessig ausgeblendet;
seit 2026-10-08 auf Wunsch Ingo standardmaessig eingeblendet und ueber das Layout ausblendbar
(`NO_OUT` entfernt, gleicher Auftrag `T76K912714`, in T76 aktiv). Ein gespeichertes Standardlayout
des Benutzers geht weiterhin vor.

## Stand

| | |
|---|---|
| System | **T76**, umgesetzt und getestet 2026-10-07 |
| Transport | **`T76K912714`** (Aufgabe `T76K912715`), offen; enthaelt Struktur `ZMM_UEB_FAUF`, Report `ZM_OFFENE_FAUF` (Quelltext und Textpool) |
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
