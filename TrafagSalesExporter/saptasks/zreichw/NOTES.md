# ZREICHW Reichweitenanalyse (`Z_KOI_REICHW_TURBO`)

Stand: 2026-11-02

## 1. Ausgangslage

Teams-Anfrage an Ingo (02.11., Absender nicht im Repo): Mit ZREICHW wurde bisher immer ausgedruckt; gewuenscht ist,
dass nur ein SAP-Spoolauftrag entsteht.

Befund (T76 per RFC, P76 lesend in eigener GUI-Sitzung):

- ZREICHW ist eine Reporttransaktion auf `Z_KOI_REICHW_TURBO`, Paket `ZPP`, in T76 und P76 gleich (SE93).
- Der aktive Stand stammt vom 15.12.2025 (Auftrag `T76K911529` „Reichweitenanalyse Ttiel“, freigegeben; davor
  `T76K911524` „ZREICHW Fast“). T76 und P76 waren damit vor dieser Aenderung gleich.
- Das Programm druckt selbst nicht. Es zeigt eine SALV-Liste (Grid) an. Gedruckt wurde, wenn jemand in der Liste
  „Drucken“ waehlte und im Druckdialog „Sofort ausgeben“ angehakt war (Festwert im Benutzerstamm).
- In P76 gibt es keinen Hintergrundjob mit `*REICHW*` (SM37, alle Benutzer, 2025 bis 2026).
- Die Vorgaenger `Z_REICHW` (inaktive Fassung bei KOI liegt noch in T76, nicht angefasst) und `Z_KOI_REICHW2` gibt es
  in T76 weiter; die Transaktion zeigt nicht mehr darauf.

## 2. Umsetzung „Nur Spoolauftrag“ (Entscheid Ingo 02.11.: Ankreuzfeld im Programm)

- Neues Ankreuzfeld `P_SPOOL` im Block Ausgabe, Beschriftung „Nur Spoolauftrag (nicht drucken)“ ueber
  `SELECTION-SCREEN COMMENT` wie die uebrigen Zeilen (kein neuer Selektionstext noetig).
- Ist es gesetzt (und der Lauf nicht im Hintergrund): `GET_PRINT_PARAMETERS` ohne Dialog mit dem Ausgabegeraet aus dem
  Benutzerstamm, `IMMEDIATELY = space`, `RELEASE = space`, neue Liste `ZREICHW`, Titel „Reichweitenanalyse“, Format
  65 x 255; `PRIMM` wird zur Sicherheit noch einmal geleert. Danach `NEW-PAGE PRINT ON ... NO DIALOG`, die SALV-Liste
  im Listmodus (`list_display`), `NEW-PAGE PRINT OFF`, Meldung „Spoolauftrag n erzeugt (nicht gedruckt), siehe SP01“.
  Ohne Ausgabegeraet im Benutzerstamm bricht der Lauf mit einer Meldung ab.
- Ohne Haken bleibt alles wie bisher (Grid). Im Hintergrund (F9) gelten wie immer die Druckparameter des Jobs.
- Quelltext: `Z_KOI_REICHW_TURBO_vorher.abap` (954 Zeilen, Live-Stand) und `Z_KOI_REICHW_TURBO_neu.abap` (1014 Zeilen).
- T76: eigener neuer Auftrag **`T76K912799`** (Aufgabe `T76K912800`, Text „ZREICHW Nur Spoolauftrag (Claude 02.11.)“),
  einziges Objekt `LIMU REPS Z_KOI_REICHW_TURBO`. Syntaxpruefung ohne Fehler, aktiviert nur dieses Objekt (die inaktive
  `Z_REICHW` blieb unmarkiert), aktiver Quelltext per RFC zurueckgelesen: **zeilengleich** mit `_neu.abap`.
- Test T76, eigene Sitzung, Material 35799 bis 37009, Werk 1100, Haken gesetzt: 10,7 s, Meldung „Spoolauftrag
  0001264623 erzeugt“. SP02: Status „-“ (**Spoolauftrag ohne Ausgabeauftrag**, also nicht gedruckt), 20 Seiten, Titel
  „Reichweitenanalyse“, Geraet `0003`, Format `X_65_255`; keine Zeile in `TSP02`. Alle 29 Spalten stehen im Spool
  (Breite 237 Zeichen, mit Kurzueberschriften wie beim Druck aus dem Grid).

**Offen:** Ingo gibt `T76K912799` frei und importiert nach P76. Antwort an die anfragende Person: Haken „Nur
Spoolauftrag“ setzen, die Liste steht danach in SP01 bzw. „Eigene Spoolauftraege“ (SP02).

## 3. Befunde zur Optimierung (gelesen am 02.11., noch nicht umgesetzt)

Das Programm stammt von einer frueheren Claude-Version. Beim Lesen aufgefallen, getrennt nach Wirkung:

**Laufzeit, Ergebnis bleibt gleich**

1. `LOOP AT lt_mseg_raw WHERE matnr = ...` auf einer STANDARD-Tabelle je Material: quadratisch (Materialien x
   Bewegungen). Ebenso `READ TABLE lt_ekpo_open/lt_ekab_open WITH KEY` linear. Sortierte oder Hash-Tabellen bzw.
   Aggregation per `GROUP BY` in der Datenbank.
2. Letzte Bewegung (MC46): `MAX( budat )` ueber die **gesamte** MSEG-Historie aller Materialien. Gebraucht wird nur
   „Bewegung in den letzten 365 Tagen ja/nein“, also `budat >= sy-datum - 365`.
3. `READ_TEXT` dreimal je Material (ZEK_/ZVK_/ZFI_), meist fuer nicht vorhandene Texte. Vorher in `STXH` nachsehen,
   welche Namen es gibt.
4. `SELECT *` auf MARA/MARC/MBEW/MAKT/MARD, obwohl wenige Felder gebraucht werden; MSEG mit Join auf MKPF statt
   `budat_mkpf` (je nach Release pruefen).

**Fehler, die Zahlen veraendern**

5. `<fs_mbew>` bleibt aus dem vorigen Material zugewiesen, wenn ein Material keine MBEW-Zeile hat; dann rechnet es
   mit dem Preis bzw. `SALK3` des vorigen Materials.
6. Anfangsbestand: nur MARDH genau fuer den Vormonat von `p_dlow`; fehlt diese Zeile (MARDH wird nur bei Bewegung
   geschrieben) oder war der Bestand 0, wird der **heutige** MARD-Bestand genommen. Beispiel T76, Material 35799:
   Anfang 169 (= heutiger Bestand), Zugang 568, Abgang 399, Endbestand 338, obwohl MARD 169 zeigt; die Reichweite
   10,2 Monate ist damit zu hoch. Ausserdem beginnt der Bestand am Monatsende, die Bewegungen aber am Tag `p_dlow`.
7. Abgang = alle Haben-Zeilen der MSEG, also auch Umlagerungen im Werk (311/312 u. a. erzeugen Soll und Haben) und
   Sonderbestaende. Das blaeht den Verbrauch auf und verkuerzt die Reichweite.
8. „Offene Rahmenvertraege“ summieren `EKAB-MENGE` (Abrufdokumentation), also alle je abgerufenen Mengen, auch
   laengst gelieferte; die noch offenen Abrufe stehen zudem schon als Bestellungen in ME2M und zaehlen doppelt. Offen
   waere Zielmenge minus Abrufe (`EKPO-KTMNG` minus Summe `EKAB`) je gueltigem Kontrakt.
9. XYZ: alle zwoelf MVER-Monate der Jahre von bis, auch Monate ausserhalb des Zeitraums und kuenftige Nullmonate;
   das treibt den Variationskoeffizienten und schiebt Material nach Z.
10. Waehrung fest `CHF`; Perioden zaehlen nur Monatsdifferenzen ohne Tage.

Vorschlag: 1 bis 4 und 5 als eigener Auftrag mit Vorher/Nachher-Vergleich in T76 (gleiche Selektion, gleiche
Ergebnistabelle ausser den Materialien ohne MBEW); 6 bis 9 erst nach Entscheid der Fachabteilung, weil sich die
bekannten Reichweiten aendern. *Nachtrag:* Ingo entschied am selben Abend, nur ohne Ergebnisaenderung zu optimieren,
an einer Kopie (Abschnitt 4); Punkt 5 bleibt deshalb bewusst wie bisher.

## 4. Optimierte Kopie `Z_KOI_REICHW_OPT` (Auftrag Ingo 02.11. abends: „arbeite an Kopie, optimiere alles ohne dass sich was veraendert“)

**Objekte in T76, beide lokal (`$TMP`, kein Transport):** `Z_KOI_REICHW_OPT` (Kopie von `Z_KOI_REICHW_TURBO` mit
Textelementen, Quelltext `Z_KOI_REICHW_OPT.abap`, 1062 Zeilen, enthaelt auch „Nur Spoolauftrag“) und der
Vergleichsreport `Z_KOI_REICHW_VGL` (`Z_KOI_REICHW_VGL.abap`). Das Original ist unberuehrt.

**Was anders gemacht wird, Ergebnis gleich** (T76 ist S/4HANA, `SAP_BASIS` 758, `S4CORE` 108):

1. Die Materialliste geht als interne Tabelle in die Abfragen (`INNER JOIN @gt_mat`), statt als Range mit tausenden
   Einzelwerten in jedes `IN`. Bei Selektion ueber Materialnummern ist die Liste die MARA-Treffer, sonst wie bisher
   alle Materialien mit frei verwendbarem Bestand im Werk.
2. Bewegungen in der Datenbank verdichtet (`SUM` je Material, Soll/Haben, Bewegungsart) statt aller Einzelzeilen;
   Buchungsdatum ueber `MSEG-BUDAT_MKPF` ohne Join auf MKPF (S/4: beides aus MATDOC). Einzelzeilen nur noch fuer die
   Ausgabeart Write.
3. MARD und MARDH je Material summiert in der Datenbank, nur die benoetigten Felder statt `SELECT *`.
4. Langsamdreher (MC46): statt `MAX(Buchungsdatum)` ueber die ganze Historie nur „gibt es eine Bewegung ab heute -
   365“ (gleichwertig).
5. Texte: einmal in `STXH` nachsehen, welche `ZEK_`/`ZVK_`/`ZFI_`-Texte es gibt, `READ_TEXT` nur fuer diese.
6. Alle Zugriffe je Material ueber Hash- oder sortierte Schluessel statt linearer Suche in Standardtabellen.
7. Bewusst gleich gelassen: alle Rechenregeln samt der Befunde aus Abschnitt 3 (auch das stehenbleibende
   `<fs_mbew>`, im Code kommentiert), Spalten, Reihenfolge der Zeilen (nach Materialnummer), ALV und Spool.

**Vergleich in T76** mit `Z_KOI_REICHW_VGL` (beide Programme per `SUBMIT` mit gleicher Selektion, Ergebnistabelle
ueber `cl_salv_bs_runtime_info`, jede Zelle verglichen):

| Lauf | Zeilen | Zellen | Abweichend | alt | neu |
| --- | --- | --- | --- | --- | --- |
| Material 35799 bis 37009, Standardoptionen | 1'157 | 34'710 | 0 | 4,2 s | 1,5 s |
| ganzes Werk 1100, 02.11.2025 bis 02.11.2026 | 5'076 | 152'280 | 0 | 155,5 s | 2,2 s |
| ganzes Werk, alle drei Rechenoptionen umgestellt, neu zuerst | 5'076 | 152'280 | 0 | 134,5 s | 2,5 s |
| ganzes Werk ab 15.01.2026 (Vormonat im Vorjahr) | 5'076 | 152'280 | 0 | 96,3 s | 2,0 s |
| Ausgabeart Write, Material 35799 bis 37009, Listenzeilen sortiert | 38'551 Zeilen | | 0 | | |

Die Debug-Liste wird sortiert verglichen, weil die Reihenfolge der Einzelbewegungen schon bisher die der Datenbank
war.

**Offen:** Entscheid Ingo, ob der Quelltext von `Z_KOI_REICHW_OPT` in `Z_KOI_REICHW_TURBO` uebernommen wird (dann am
einfachsten in den noch offenen Auftrag `T76K912799`, ein Import fuer beides). Danach koennen die beiden lokalen
Objekte geloescht werden.
