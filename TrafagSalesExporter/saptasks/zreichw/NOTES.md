# ZREICHW Reichweitenanalyse (`Z_KOI_REICHW_TURBO`)

Stand: 2026-11-03 (schnelle Fassung ins Original uebernommen, Abschnitt 6; davor 2026-11-02 abends Uebergabe in Abschnitt 5)

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

**Offen:** *(erledigt am 2026-11-03: uebernommen, Abschnitt 6)* Entscheid Ingo, ob der Quelltext von `Z_KOI_REICHW_OPT` in `Z_KOI_REICHW_TURBO` uebernommen wird (dann am
einfachsten in den noch offenen Auftrag `T76K912799`, ein Import fuer beides). Danach koennen die beiden lokalen
Objekte geloescht werden.

## 5. Weitermachen (Uebergabe 02.11. abends, Ingo schliesst die Sitzung)

*Schritte 1 und 2 am 2026-11-03 erledigt (Abschnitt 6). Offen bleiben 3 (Freigabe/Import P76), 4 (Teams-Antwort) und 5 (Fachabteilung).*

**Stand der Systeme**

- T76: `Z_KOI_REICHW_TURBO` aktiv mit Ankreuzfeld „Nur Spoolauftrag“, im offenen Auftrag `T76K912799` (Aufgabe `T76K912800`,
  nicht freigegeben). `Z_KOI_REICHW_OPT` und `Z_KOI_REICHW_VGL` lokal (`$TMP`), aktiv. Keine eigene GUI-Sitzung mehr offen.
- P76: unveraendert, ZREICHW dort noch ohne Ankreuzfeld (Stand `T76K911529`).
- Repository: alles committet bis `435d6d4` plus diese Uebergabe, nichts gepusht.

**Naechste Schritte, in dieser Reihenfolge**

1. Entscheid Ingo: Quelltext von `Z_KOI_REICHW_OPT` ins Original uebernehmen, ja oder nein.
2. Bei Ja, in einer eigenen T76-Sitzung (nie Sitzung 0):
   - `Z_KOI_REICHW_OPT.abap` aus diesem Ordner, Kopfkommentar „Report Z_KOI_REICHW_OPT“ auf `Z_KOI_REICHW_TURBO` zuruecksetzen.
   - In SE38 `Z_KOI_REICHW_TURBO` aendern, Quelltext ersetzen, sichern; Auftrag `T76K912799` waehlen (kein neuer).
   - Syntaxpruefung, nur dieses Objekt aktivieren (die inaktive `Z_REICHW` unmarkiert lassen), aktiven Stand per RFC
     zuruecklesen und zeilengleich pruefen.
   - Vergleich erneut laufen lassen: `Z_KOI_REICHW_VGL` vergleicht dann TURBO gegen OPT, beide muessen identisch sein, Laufzeit
     beider etwa 2 s fuer Werk 1100. Danach einmal ZREICHW mit Haken „Nur Spoolauftrag“ (Spool ohne Ausgabeauftrag wie in Abschnitt 2).
   - Danach `Z_KOI_REICHW_OPT` und `Z_KOI_REICHW_VGL` in T76 loeschen (lokal, kein Transport). Den Quelltext behalten wir hier.
3. Ingo gibt `T76K912799` frei und importiert nach P76. Danach in P76 lesend pruefen (SE38 Version bzw. ZREICHW Selektionsbild
   mit dem neuen Haken). Keine Gateway-Caches noetig, ZREICHW ist kein OData.
4. Antwort an die anfragende Person (Teams, Ingo sendet selbst), Vorschlag:
   „Ab sofort gibt es in ZREICHW im Block Ausgabe das Ankreuzfeld ‚Nur Spoolauftrag (nicht drucken)‘. Ist es gesetzt, wird die
   Liste weder angezeigt noch gedruckt, sondern als Spoolauftrag abgelegt. Die Nummer steht unten in der Statuszeile, den Auftrag
   findest du in SP01 bzw. unter System, Eigene Spoolauftraege. Ohne Haken laeuft alles wie bisher.“
   Erst senden, wenn der Import in P76 durch ist.
5. Fachabteilung: Befunde 5 bis 10 aus Abschnitt 3 vorlegen (veraendern die Reichweiten). Erst nach deren Entscheid umsetzen,
   wieder an einer Kopie mit Vergleich gegen den Ist-Stand.

**Werkzeuge** (Ordner `werkzeuge/`, aus dem Scratchpad der Sitzung gesichert, Aufruf mit `cscript //nologo`, erstes Argument
ist immer die eigene Sitzungsnummer in T76, Sitzung 0 bricht ab):

| Datei | Zweck |
| --- | --- |
| `se38aendern.vbs <ses> <programm>` | SE38, Programm im Aenderungsmodus oeffnen |
| `se38ersetzen.vbs <ses> <fenstertext> <datei>` | ganzen Editorinhalt durch die UTF-8-Datei ersetzen und sichern (prueft vorher das Bild) |
| `se38pruefen.vbs <ses>` / `syntaxliste.vbs <ses>` | Syntaxpruefung (Strg+F2) / Fehlerliste ausgeben |
| `se38aktiv.vbs <ses>` | aktivieren |
| `zreichw_lauf.vbs <ses> <von> <bis> <X/->` | ZREICHW mit Materialbereich, mit oder ohne Spool-Haken |
| `vgl_lauf.vbs <ses> <von/-> <bis/-> <optionen> [dlow]` | Vergleichsreport starten; Optionen i r a (Rechenoptionen), o (OPT zuerst), l (Write-Liste) |
| `sp02zeigen.vbs <ses>`, `t76dump.vbs <ses> [okcode]`, `t76nav.vbs <ses>` | Spool anzeigen, Bildinhalt ausgeben, zurueck |
| `reichw_opt.py`, `load_opt.abap` | so wurde `Z_KOI_REICHW_OPT.abap` aus dem Original erzeugt (`python reichw_opt.py <quelle> load_opt.abap <ziel>`) |

Neue Sitzung, Auftrag und Inaktivliste mit den festen Skripten aus `.tmp_sap_probe/` (`SapGuiNeueSitzungT76.vbs`,
`SapGuiInaktiveMarkieren.vbs <ses> pruefen|aktivieren <name>`), schliessen mit `SapGuiSitzungSchliessen.vbs`; Quelltext per RFC
zuruecklesen mit `.tmp_sap_probe/RunSapProbe.ps1` (ueber Bash `powershell -NoProfile -ExecutionPolicy Bypass -File`).
`SapGuiSetReportSource.vbs` nicht verwenden, es greift irgendeine T76-Sitzung.

## 6. Schnelle Fassung ins Original uebernommen (Entscheid Ingo 2026-11-03 „ja uebernehmen, ich gebe dann frei“)

Ausgefuehrt in T76 in einer eigenen Sitzung (Verbindung 1, Sitzung 1; Ingos Sitzung 0 unberuehrt), danach geschlossen.

1. Vorher geprueft: der aktive Stand von `Z_KOI_REICHW_TURBO` war zeilengleich mit `Z_KOI_REICHW_TURBO_neu.abap`
   (niemand hatte ihn seit dem 02.11. geaendert); Textsymbole und Parameter von OPT und TURBO identisch. Der Kopf des
   Originals hiess schon immer „Report Z_KOI_REICHW_OPT“ / `REPORT z_koi_reichw_opt.`; das bleibt so.
2. Neue Quelle `Z_KOI_REICHW_TURBO_schnell_2026-11-03.abap` = `Z_KOI_REICHW_OPT.abap`, nur die Kommentarzeilen 4 und 5
   angepasst (1'062 Zeilen).
3. SE38 aendern, Quelltext ersetzt, gesichert ohne Auftragsabfrage (Objekt lag schon in `T76K912799`); Syntaxpruefung
   ohne Fehler; im Dialog „Inaktive Objekte“ nur `Z_KOI_REICHW_TURBO` ueber den Namen markiert und aktiviert (fremde
   `Z_REICHW` unberuehrt).
4. Aktiven Stand per RFC zurueckgelesen: **zeilengleich**, 1'062 Zeilen. `E071`: der Auftrag `T76K912799` (Aufgabe
   `T76K912800`) enthaelt nur `LIMU REPS Z_KOI_REICHW_TURBO`; beide noch aenderbar (`D`).
5. Vergleich `Z_KOI_REICHW_VGL` (TURBO gegen OPT), ganzes Werk 1100, 03.11.2025 bis 03.11.2026, Standardoptionen:
   5'076 Zeilen, 152'280 Zellen, **0 Abweichungen**; Laufzeit TURBO **2,3 s** (vorher 155 s), OPT 2,1 s.
6. ZREICHW mit Haken „Nur Spoolauftrag“, Material 35799 bis 37009: Spool **1265145** erzeugt, in `TSP02` kein
   Ausgabeauftrag, also nicht gedruckt.
7. `Z_KOI_REICHW_OPT` und `Z_KOI_REICHW_VGL` (beide `$TMP`) in SE38 geloescht, `TADIR` zeigt nur noch
   `Z_KOI_REICHW` und `Z_KOI_REICHW_TURBO`. Die Quelltexte bleiben in diesem Ordner. Loeschskript:
   `werkzeuge/se38loeschen.vbs` (erlaubt nur diese zwei Namen, nie Sitzung 0).

**Offen:** Ingo gibt `T76K912799` frei und importiert nach P76; danach P76 lesend pruefen (Selektionsbild mit Haken,
Laufzeit) und die Teams-Antwort aus Abschnitt 5 senden. Die Befunde 5 bis 10 (Abschnitt 3) bleiben bei der
Fachabteilung. Fuer einen spaeteren Vergleich gegen den alten Stand liegt `Z_KOI_REICHW_TURBO_neu.abap` hier.

**Nachtrag 2026-11-03, P76 nach Import (Ingo: „ist im P“), nur lesend in eigener Sitzung (`SapGuiP76Hilfe.vbs`, ses[1],
danach mit `/i` geschlossen; Ingos ses[0] unberuehrt):** Selektionsbild ZREICHW zeigt das Ankreuzfeld „Nur Spoolauftrag
(nicht drucken)“ (`P_SPOOL`). Lauf ganzes Werk 1100 als ALV-Anzeige ohne Spool: **4'934 Zeilen in rund 5 s** (gemessen samt
Skriptwartezeit von 4 s; vorher in T76 rund 155 s). Damit ist der Import wirksam. Offen nur noch die Teams-Antwort (Abschnitt 5,
Ingo sendet). Hinweis: `P76SitzungSchliessen.vbs` schliesst nur SE16/VA43/ME33K-Sitzungen; eine ZREICHW-Sitzung mit
`SapGuiP76Hilfe.vbs ok <ses> /i` schliessen.

**Abschluss 2026-11-03:** Ingo meldet „ist erledigt, scheint zu passen“. Das Thema ist abgeschlossen; die Befunde 5 bis 10 (Abschnitt 3) bleiben als eigene Frage bei der Fachabteilung.
