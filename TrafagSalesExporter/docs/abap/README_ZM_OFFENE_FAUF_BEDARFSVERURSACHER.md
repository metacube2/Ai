# ZM_OFFENE_FAUF: Bedarfsverursacher als einblendbare Felder

Stand: 2026-11-03 (Umlagerungsbedarf statt Kundenauftrag, Abschnitt am Ende; T76 aktiv, `T76K912825`); davor 2026-10-09 (seit dem 08.10. standardmaessig eingeblendet, am 09.10. nach P76 transportiert). Wunsch Ingo: die Angaben aus MD04 > Bedarfsverursacher (Dispoelement,
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
`T76K912714` dasselbe noch einmal** (Layouts sind Mandantendaten und kommen nicht mit dem Transport). *Nachtrag 2026-11-03: das ist in P76 nie geschehen; `/ P00 TX` zeigte in P76 nur die alten Spalten bis Kundenauftrag/Position/Name 1.*

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

## 2026-11-03: Umlagerungsbedarf (Banf-Abruf / Bestellabruf) statt Kundenauftrag

**Anlass:** Mail Anh Nhi Tran 02.11. „Dispoelement in ZC22 ersichtlich“: in ZC22 die Bestellanforderung inklusive
Dispoelement sehen statt Kundenauftrag mit Position (Screenshot MD04: Banf-Abruf `0010395862/00010`, Material 46690,
Werk 1100, 30.10.2026, 7 ST). Fabio Palma und Sergio Rodrigues am 03.11. „bitte anpassen“; Auftrag Ingo „mach du“.

**Befund P76 (gelesen 03.11., eigene Sitzung, Layout nicht gesichert):** ZC22 fuer 46690: Auftraege 2519235 (1) und
2522216 (6) mit KndAuftr 419042/10 Andritz Hydro. Testweise eingeblendet: Bedarfsverursacher `VC 419042/10`, Werk **1200**,
Dispobereich 1200, Anzahl 1 bzw. 2. Ursache: `MD_PEGGING_NODIALOG` verfolgt den Bedarf ueber die Werksgrenze bis zum
Endbedarf (Kundenauftrag in 1200) und gibt die Zwischenstufe nicht aus (`EMDPSX` leer, in T76 mit `Z_PEG_TEST` geprueft;
Quelltext `LM61BU02`). Das Layout allein reicht deshalb nicht. In P76 (SE16, nur Anzeige) ist die BANF `0010395862/00010`
in `EBKN` auf Kundenauftrag `0000419042/000010` kontiert (7 ST, angelegt 30.10.2026).

**Aenderung (T76, Auftrag `T76K912825`, Aufgabe `T76K912826`, nur `REPS ZM_OFFENE_FAUF`):** neue Form
`UMLAGERUNGSBEDARF`, aufgerufen am Ende von `BEDARFSVERURSACHER`, wenn das Werk des ersten Endbedarfs vom Werk des
Auftrags abweicht. Sie sucht zum Kundenauftrag des Endbedarfs:
1. eine offene Umlagerungsbestellung (`EKKN` Kontierung auf den Kundenauftrag, `EKPO` gleiches Material, Werk = Werk des
   Endbedarfs, nicht geloescht, nicht endgeliefert, `EKKO-RESWK` = Werk des Auftrags) -> `U1` (BS-Abr) mit Bestellung/Position;
2. sonst eine offene BANF (`EBKN` Kontierung, `EBAN` gleiches Material, Werk des Endbedarfs, `RESWK` = Werk des Auftrags,
   nicht geloescht, nicht erledigt, `BSMNG < MENGE`) -> `U2` (BA-Abr) mit BANF/Position.
Gefunden: `ZZPEG_DELKZ`, `ZZPEG_DELNR`, `ZZPEG_DELPS`, `ZZPEG_EXTRA` (Nummer/Position) werden ersetzt, `ZZPEG_DELET` geleert;
Werk, Dispobereich, Termin und Menge bleiben die des Endbedarfs. Nicht gefunden (z. B. Fertigung in anderem Werk ohne BANF,
Material 39101 in T76): alles wie bisher. Endbedarf im eigenen Werk: unveraendert.

**Test T76:** Syntax ohne Fehler, nur `ZM_OFFENE_FAUF` aktiviert (fremde `Z_REICHW` unberuehrt), per RFC zurueckgelesen
gleich `docs/abap/ZM_OFFENE_FAUF.abap` (1'068 Zeilen). ZC22 Material 63502: Planauftrag 2404220 vorher `VC 411226` (1200),
jetzt **`U1 45148422/10`, Werk 1200** (BANF 0010336115 dort schon in Bestellung NB mit `RESWK` 1100 umgesetzt); Auftraege mit
Endbedarf in 1100 weiter `VC`. Material 39101: Endbedarf `VC 411294` in 1200 ohne BANF/Bestellung bleibt `VC`. Ein `U2`-Fall
existiert in T76 nicht (keine offene, kontierte BANF mit abgedecktem Planauftrag); in P76 ist es Anh Nhis Fall.
Vorher-Stand: `docs/abap/ZM_OFFENE_FAUF_vorher_2026-11-03.abap`.

**Offen:**
1. Ingo gibt `T76K912825` frei und importiert nach P76.
2. Danach in P76 das globale Layout `/ P00 TX` („Tisk trafag cz“): Kundenauftrag und Kundenauftragspos. ausblenden,
   Dispositionselement, Nummer Dispositionselement, Position Dispositionselement einblenden, sichern (Ingo hat „mach du“
   gesagt; Werkzeug `.tmp_sap_probe/SapGuiAlvListLayoutP76.vbs`). Pruefen mit Material 46690: `U2 0010395862/00010`.
   Name 1 bleibt, bis Fabio/Anh Nhi anders entscheiden.
3. Antwort an Anh Nhi, Fabio, Sergio (Ingo).

**Nachtrag 2026-11-03 nachmittags, P76:** `T76K912825` laut Ingo nach P76 transportiert. In eigener P76-Sitzung ZC22 fuer
46690 mit dem globalen Layout `/ P00 TX`: Kundenauftrag und Kundenauftragspos. ausgeblendet, Dispositionselement, Nummer
und Position Dispositionselement eingeblendet (Name 1 bleibt), gesichert global (nicht benutzerspezifisch, Filter/Sortierung/
Zwischensummen wie bisher). Das Ueberschreiben hat der Auto-Modus blockiert, Ingo hat es im Dialog selbst bestaetigt.
Frischer Lauf danach: Auftraege 2519235 und 2522216 zeigen **`U1 45152789/10`**: die BANF 0010395862 aus Anh Nhis Screenshot
ist inzwischen in die Umlagerungsbestellung 45152789 umgesetzt, deshalb Bestellabruf statt Banf-Abruf. Erledigt bis auf die
Antwort an Anh Nhi, Fabio, Sergio (Ingo). Hinweis: Wer ein eigenes Standardlayout gesichert hat, sieht die neuen Spalten erst,
wenn er `/ P00 TX` waehlt oder sie selbst einblendet.
