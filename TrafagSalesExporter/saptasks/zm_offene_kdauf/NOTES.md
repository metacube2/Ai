# ZM_OFFENE_KDAUF beschleunigen

Stand: 2026-10-12 11:55, in T76 eingesetzt: `ZM_OFFENE_KDAUF` (ZC23) hat den neuen Quelltext, Transport `T76K912728` bereit; Freigabe und Import nach P76 offen (Ingo)

Auftrag Ingo 2026-10-12: Der Report liefert richtige Zahlen, ist aber sehr langsam. Frühere
schnelle Fassungen (verschiedene Claude-Versionen) hatten am Ende falsche Zahlen. Gearbeitet
wird mit einer Kopie, die gegen das Original verglichen wird. Das Original bleibt
unverändert; die schnelle Fassung kopiert Ingo erst nach bestandenem Vergleich zurück.

## Ergebnis in einem Satz

`ZM_OFFENE_KDAUF_OPT` liefert dieselbe Liste wie das Original (mit ETENR-Sortierung) in etwa
**1/35 bis 1/50 der Laufzeit**, kann als Hintergrundjob laufen und schreibt `ZPOWERBI_KDAUF`
mit einem Sammelaufruf für die IDs.

| Selektion | Zeilen | OPT | Original (REF) | Ergebnis |
| --- | ---: | ---: | ---: | --- |
| 01.–15.03.2026, Werk 1100 | 1'108 | 1.7 s | 55 s | identisch |
| 01.04.–30.06.2026, Werk 1100 (6 Aufgaben, Vorgaben) | 4'392 | **5.1 s** | 187 s | identisch |
| Variante `BI UPDATE` (fest 01.01.–31.07.2025), Job, 10 Aufgaben | 15'820 | **23.6 s** | 1'227 s | identisch |
| `BI UPDATE` mit Fortschreibung `ZPOWERBI_KDAUF`, Job | 15'820 | 38 s gesamt | bricht ab (siehe unten) | 15'820 Zeilen, IDs lückenlos, keine doppelt |

„Identisch“ heißt: alle 60 Felder je Zeile über VBELN/POSNR/ETENR, alle Summen und die
Listenreihenfolge, gemessen mit Korrekturschalter „alte Logik“ gegen REF.

## Stand in T76 (2026-10-12)

Lokale Objekte (`$TMP`, kein Transport), alle aktiv; das Original `ZM_OFFENE_KDAUF` ist
unverändert:

| Objekt | Zweck |
| --- | --- |
| `ZM_OFFENE_KDAUF_OPT` | schnelle Fassung, Quelltext zum Zurückkopieren (`ZM_OFFENE_KDAUF_OPT.abap`) |
| `ZM_OFFENE_KDAUF_REF` | Original wörtlich plus ETENR-Sortierung, Messpunkte, Export ins Memory |
| `ZM_OFFENE_KDAUF_CMP` | Vergleichsreport (Dialog, Hintergrundjob `P_BG`, Ergebnis `P_ZEIG`, Fortschreibungstest `P_UPD`) |
| Funktionsgruppe `ZKDAUF_PAR`, Baustein `Z_KDAUF_PAR_RECHNEN` (RFC) | Parallelrechnung, Kurzfassungen von Basispreisliste und Belegfluss (`Z_KDAUF_PAR_RECHNEN.abap`, `LZKDAUF_PARTOP.abap`) |
| `Z_KDAUF_NR_TEST` | Messung von `NUMBER_GET_NEXT` (verbraucht Nummern in T76) |

REF und OPT sind per SE38-Kopie aus dem Original entstanden (mit Textelementen, Oberfläche und
Dynpros). Nebenbefund: Im Objektkatalog steht `ZM_OFFENE_KDAUF_TURBO` (Paket ZPP) ohne
Programm, vermutlich ein Rest eines früheren Versuchs. Nicht angefasst.

## Was OPT schneller macht

1. **Vorablesen** (`PREFETCH_OPT`): Die rund 20 Einzelzugriffe je Einteilung (VBAK, KNA1,
   VBAP, VBBE, MARA, MAKT, VBEP, VBKD, MARC, VBFA, MBEW, AFPO, AFKO, AUFK, AUFM, Dispo-Join)
   laufen einmal gesammelt (FOR ALL ENTRIES mit ganzem Schlüssel), die Schleife ist Anweisung
   für Anweisung die alte.
2. **Parallelrechnung** (`PARALLEL_RECHNEN`): Pakete zu 50 Positionen und Materialien gehen
   mit `STARTING NEW TASK ... DESTINATION IN GROUP DEFAULT` an `Z_KDAUF_PAR_RECHNEN`,
   höchstens `P_PTASK` gleichzeitig (NO-DISPLAY, Vorgabe 6). Fehlt ein Ergebnis, rechnet die
   Schleife wie bisher selbst.
3. **Basispreisliste kurz** (`P_BPMOD = K`, Vorgabe): `BP_KURZ` rechnet wie
   `Z_BASISPREISLISTE`/`FILL_BPTAB`/`READ_TABLE`, hört aber nach der dritten Zeile auf (der
   Report liest nur Zeile 2 oder 3). Typ 87 ganz ohne Aufruf (Ergebnis wird verworfen).
   Prüfmodus `P` über Q2 und `BI UPDATE`: 0 Materialien mit Abweichung. `Z_BASISPREISLISTE`
   selbst bleibt unverändert (andere Programme nutzen ihn).
4. **Belegfluss eigene Lesung** (`P_FLMOD = H`, Vorgabe): `FLUSS_KURZ` liest die
   Fakturazeilen direkt aus VBFA, abwärts nur über direkte Verknüpfungen (`STUFE = '00'`),
   jeder Knoten mit Belegtyp (dieselbe Nummer kommt in mehreren Belegarten vor, etwa Handling
   Unit und Kundenauftrag), je Beleg auch die Kopfverknüpfung. Haben zwei Fakturazeilen
   dasselbe Datum, ruft H für diese Position doch `RV_ORDER_FLOW_INFORMATION` auf, weil die
   Reihenfolge gleich datierter Zeilen am ganzen Baum des Originals hängt. Prüfmodus `P` über
   `BI UPDATE` (rund 16'000 Positionen): 0 Positionen mit anderen Fakturazeilen, 58 mit nur
   anderer Reihenfolge bei gleichem Datum (dort Rückfall auf das Original).
5. **IDs für `ZPOWERBI_KDAUF`**: ein Sammelaufruf `NUMBER_GET_NEXT` mit `QUANTITY` statt je
   Zeile. Gemessen (`Z_KDAUF_NR_TEST`): Er liefert die **letzte** Nummer des Blocks; 2'000
   Nummern 4 ms statt 777 ms. Die Nummern im Hauptspeicherpuffer werden dabei übersprungen.

Nicht gemacht: ein neuer Datenbankindex (Entscheid Ingo 2026-10-12: kein neuer Index).

## Korrekturen (Entscheid Ingo 2026-10-12)

Schalter im Selektionsbild, Block „Korrekturen“:

| Schalter | Wirkung |
| --- | --- |
| `P_KVOLL` „Korrigiert“ (Vorgabe) | Erfasser und Erstelldatum immer aus dem Kundenauftrag; das Original überschrieb ERDAT mit dem Absagegrund und bei zugeordnetem Fertigungsauftrag ERNAM/ERDAT mit dessen Werten |
| `P_KABGR` „Nur Absagegrund“ | nur ERDAT nicht mehr mit dem Absagegrund überschrieben |
| `P_KALT` „Alte Logik“ | wie bis Oktober 2026, Fehler eingeschlossen (für Vergleiche) |

In allen Stellungen außer „alte Logik“ zusätzlich: kein zweites `DELETE` derselben Zeile im
Auftragsart/FEVOR-Filter, und der ALV-Feldkatalog enthält das nicht vorhandene Feld `NETWR`
nicht mehr. Ohne diese Korrektur bricht die Liste im Hintergrund mit `GETWA_NOT_ASSIGNED`
(`SAPLKKBL`, `GEN_FIELD_OUT2`) ab, bevor `ZPOWERBI_KDAUF` geschrieben wird: **das Original
kann deshalb in T76 nicht als Job laufen** (REF-Job 11075700 am 2026-10-12 so abgebrochen).
Der Vergleich Absagegrund gegen Werk (`abgru NE p_werk`, immer wahr) ist ersetzt, ohne
Wirkung auf das Ergebnis. Gemessen: „korrigiert“ gegen REF weicht nur in ERNAM (2'060 von
4'392 Zeilen) und ERDAT (4'345) ab.

ETENR als letztes Sortierfeld (Entscheid Ingo: ja): `SORT ivbep BY vbeln posnr edatu etenr`.
Das Original ordnet Einteilungen mit gleichem Lieferdatum zufällig und vergibt dabei die
Fertigungsaufträge zufällig; REF gegen REF zeigte das (6 Zeilen).

## Transport nach P76 (vorbereitet 2026-10-12 11:50)

Auftrag **`T76K912728`** „ZM_OFFENE_KDAUF schnell (ZC23, ZKDAUF_PAR)“, Aufgabe `T76K912729`, beide offen:

| Objekt | Inhalt |
| --- | --- |
| `R3TR FUGR ZKDAUF_PAR` | Funktionsgruppe mit `Z_KDAUF_PAR_RECHNEN` (RFC), Paket von `$TMP` nach `ZPP1` umgehängt (RSWBO052) |
| `LIMU REPS ZM_OFFENE_KDAUF` | Quelltext aus `ZM_OFFENE_KDAUF_OPT.abap`, in T76 aktiv |
| `LIMU ADIR ... SUSH Z_KDAUF_PAR_RECHNEN` | von SAP angelegte Berechtigungsvorschlagswerte des RFC-Bausteins |

Gegenprobe nach dem Einsetzen: `ZM_OFFENE_KDAUF` gegen REF, 01.04.–30.06.2026, „alte Logik“: identisch,
4.5 s statt 173 s. Rücksicherung: `ZM_OFFENE_KDAUF_original.abap` im Repo, P76-Stand von Ingo auf GitHub,
`ZM_OFFENE_KDAUF_REF` in T76. `ZC23` ruft weiter `ZM_OFFENE_KDAUF` (TSTC), Varianten, Layouts und Jobs
bleiben am Programmnamen.

Für P76 zu beachten:

* **Berechtigung `S_RFC`** für Funktionsgruppe `ZKDAUF_PAR` in den Rollen der ZC23-Benutzer. Fehlt sie,
  rechnet das Programm richtig, aber ohne Parallelrechnung (Rückfall auf Einzelaufrufe, etwa 1/3 statt
  1/35 der alten Laufzeit).
* **Andere Werte für Erfasser und Erstelldatum** (Vorgabe „korrigiert“); `ZPOWERBI_KDAUF` enthält sie nicht.
* **Job `BI UPDATE`** in P76 in SM37 ansehen: Mit dem alten Programm bricht er im Hintergrund in der
  ALV-Liste ab (in T76 belegt).

## Offen für P76

1. **Transport `T76K912728` freigeben und nach P76 importieren** (Ingo). *Erledigt in T76 2026-10-12 11:50:* Paket und Auftrag vorbereitet, Quelltext eingesetzt.
2. **Dialogprozesse in P76** prüfen und `P_PTASK` danach setzen (T76 hat rund 20, Vorgabe 6).
3. **Textelemente**: Der Korrekturblock braucht keine (Texte im Code). `P_VGL`, `P_PTASK`,
   `P_PPAK`, `P_BPMOD`, `P_FLMOD` sind NO-DISPLAY.
4. Nach dem Import in P76 einmal eine echte Variante mit „alte Logik“ laufen lassen und mit
   der gewohnten Ausgabe vergleichen; der vollständige Beweis (CMP, REF) liegt in T76.
5. Eigenheit noch offen: VBEP wird ohne Werk gelesen; Einteilungen anderer Werke fallen erst
   am Schluss heraus (heute nur Laufzeit, kein falsches Ergebnis).

## Verlauf der Messungen (2026-10-12)

| Lauf | Zeilen | Ergebnis | OPT | REF |
| --- | ---: | --- | ---: | ---: |
| 01.–15.03.2026, Werk 1100, ohne ETENR-Sortierung, OPT gegen REF | 1'108 | 4 Zeilen mit vertauschten Aufträgen | 29 s | 45 s |
| dieselbe Selektion, REF gegen REF | 1'108 | 6 Zeilen mit vertauschten Aufträgen: **das Original ist nicht reproduzierbar** | | |
| dieselbe Selektion mit ETENR-Sortierung, REF gegen REF | 1'108 | identisch | | |
| dieselbe Selektion mit ETENR-Sortierung, OPT gegen REF | 1'108 | **identisch**, alle 60 Felder, Summen, Listenreihenfolge | 28 s | 45 s |
| 01.04.–30.06.2026, Werk 1100, OPT gegen REF | 4'392 | **identisch** | 59 s | 158 s |
| Variante `KDAUF_INGO` (fest 29.–31.12.2017) | 4 | identisch | 1 s | 0.3 s |
| Variante `BI UPDATE` im Dialog | | TIME_OUT nach 600 s im Belegfluss-Baustein; seither als Hintergrundjob | | |
| Variante `BI UPDATE` (fest 01.01.–31.07.2025), Job, OPT ohne Parallelrechnung | 15'820 | **identisch** | 711 s | 1'277 s |
| Variante `BI UPDATE`, Job, OPT **parallel, 10 Aufgaben** (319 Pakete, 0 Ausfälle) | 15'820 | **identisch** | **51 s** | 1'129 s |
| 01.04.–30.06.2026, OPT **parallel, 6 Aufgaben**, gegen REF | 4'392 | **identisch** | 13 s | 189 s |
| dieselbe Selektion, OPT **parallel, 10 Aufgaben**, Typ 87 ohne Basispreisliste | 4'392 | **identisch** | 7.9 s | 191 s |

Wo die Zeit im Original liegt (Q2 2026, 158 s): Belegfluss `RV_ORDER_FLOW_INFORMATION` je
Einteilung 51 s, `Z_BASISPREISLISTE` je Einteilung 36 s, übrige Einzelzugriffe rund 70 s.
In OPT bleiben 36 s Belegfluss (2'363 Aufrufe, 2'148 gespart, weil der Beleg gar keinen
Belegfluss hat) und 20 s `Z_BASISPREISLISTE` (einmal je Material). Alles andere ist auf
1.5 s Vorablesen geschrumpft.

**„Identisch“ heißt: identisch zu REF mit ETENR-Sortierung.** Gegen eine alte Ausgabe des
unveränderten Originals können sich Positionen mit mehreren Fertigungsaufträgen und gleichem
Lieferdatum in der Zuordnung der Aufträge unterscheiden, weil das Original sie zufällig
vergibt.

## Dateien

| Datei | Inhalt |
| --- | --- |
| `ZM_OFFENE_KDAUF_original.abap` | Quelltext aus T76, gelesen 2026-10-12 per `abap-read` (Stand im System 2026-01-22, KOI) |
| `ZM_OFFENE_KDAUF_REF.abap` | Original wörtlich, dazu nur Messpunkte und Export ins Memory (Schalter `P_VGL`) |
| `ZM_OFFENE_KDAUF_OPT.abap` | schnelle Fassung, gleiche Logik und Reihenfolge, gepufferte Zugriffe |
| `ZM_OFFENE_KDAUF_CMP.abap` | Vergleichsreport: startet A und B mit derselben Selektion, vergleicht Zeile für Zeile und Feld für Feld |
| `teile/*.abap`, `bauen.js` | Bausteine; `node bauen.js` erzeugt REF und OPT aus dem Original und prüft dabei die Zeilen, an denen eingegriffen wird |

## Warum frühere Fassungen falsche Zahlen brachten

Der Report verhält sich wie eine Zustandsmaschine. Das Ergebnis hängt an der
Verarbeitungsreihenfolge und an Werten, die von einer Zeile in die nächste übergehen. Wer
umbaut (Joins, Vorfilter, Feldlisten statt `SELECT *`, „offensichtliche“ Fehler beheben),
ändert still das Ergebnis. Diese Stellen hält OPT bewusst genau so bei:

1. **`IAFPO` ist global und wird nie geleert.** Jede Einteilung bekommt den ersten
   Fertigungsauftrag ihrer Position (AFPO nach Primärschlüssel), der noch nicht vergeben ist.
   Einteilung 1 bekommt also Auftrag 1, Einteilung 2 Auftrag 2, weitere Einteilungen keinen.
   Auch Zeilen, die die Nachfilter später entfernen, verbrauchen Aufträge.
2. **`FILL_FKDAT` trägt einen Zustand** in `GV_CURSOR`, `GV_LASTVBELN` und `GV_LASTPOS`.
   Er wird nur bei einem Treffer fortgeschrieben. Der Belegfluss-Baustein bleibt deshalb
   derselbe; OPT verwendet sein Ergebnis nur für die direkt folgende Einteilung derselben
   Position wieder.
3. **„Letzte Zeile nach Primärschlüssel“ über `ORDER BY PRIMARY KEY ... ENDSELECT`.**
   - VBFA: Der Schlüssel ist in T76 `MANDT, RUUID` (S/4). Das Vorgabe-Fakturadatum ist also
     das Datum der Zeile mit der größten RUUID, nicht das späteste Datum.
   - VBEP mit `BMENG = 0`: Es zählt die letzte Zeile nach `ETENR`, nicht das späteste Datum.
4. **`MOVE-CORRESPONDING` ganzer Zeilen in fester Reihenfolge.** Spätere Tabellen
   überschreiben gleichnamige Felder früherer, zum Beispiel aufk/afko/afpo nach vbak/vbap.
   OPT puffert deshalb ganze Zeilen.
5. **`AFKO` wird zwischen den Zeilen nie geleert.** Fehlt AFKO zu einem Auftrag, bleibt der
   Wert der Vorzeile stehen. OPT bildet das genauso nach.
6. **Felder, die bei „nicht gefunden“ ihren Wert behalten:** BSTDK (dann aus VBAK), DZEIT,
   DISPO, STANDARDPREIS, RANGE, ZZMBDAT, FKDAT.
7. **Eigenheiten des Originals** (entschieden am 2026-10-12, siehe „Korrekturen“ oben; mit „alte Logik“ bleiben sie):
   - `gt_uebauf-erdat = vbap-abgru` überschreibt das Erstelldatum mit dem Absagegrund.
   - `IF wa_uebauf2-abgru NE p_werk` vergleicht den Absagegrund mit dem Werk und ist deshalb
     immer wahr.
   - Im AUART/FEVOR-Filter kann `DELETE ivbep` in einem Durchlauf zweimal laufen.
   - KNA1 wird mit `WERKS = p_werk` gelesen, obwohl KUNNR allein der Schlüssel ist.
   - VBEP wird ohne Werk gelesen. Einteilungen anderer Werke laufen durch die ganze
     Verarbeitung und fallen erst am Schluss heraus.
   - `SELECT SINGLE stprs FROM mbew` ohne BWTAR ist bei getrennter Bewertung nicht
     eindeutig. OPT zählt diese Fälle (`MBEW mehrfach` im Vergleich).
8. **Beide SORT sind nicht stabil.** Gleichstände ordnet SAP nach der Eingangsreihenfolge;
   die Lese- und Löschschleifen in `FILL_IVBEP` bleiben deshalb unverändert.

## Was OPT ändert (erster Schritt, ergänzt durch „Was OPT schneller macht“ oben)

- `FILL_IVBEP`: Der Pflichtfilter über VBAK (Kunde, Erfasser, Datum, VBTYP) und KNA1-NAME1
  läuft einmal für alle Belege statt je Einteilung. Die übrigen Filterschleifen (Material,
  Disponent, Auftrag, Woche, Auftragsart, FEVOR) laufen nur mit diesen Selektionen und
  bleiben wörtlich.
- `FILL_UEBAUF`: VBAK, KNA1, VBAP, VBBE, MARA, MAKT, VBEP (BMENG=0), VBKD, MARC, VBFA, MBEW,
  AFPO, AFKO, AUFK und AUFM werden einmal vorab gelesen (`PREFETCH_OPT`, FOR ALL ENTRIES mit
  ganzem Schlüssel). Der Join ZDISPO_GRP/ZDISPO_SPART läuft einmal statt je Zeile.
- `BEREICH`: `Z_BASISPREISLISTE` einmal je Material.
- `FILL_FKDAT`: `RV_ORDER_FLOW_INFORMATION` einmal je Position, und gar nicht für Belege
  ohne jeden VBFA-Satz (weder Vorgänger noch Nachfolger). Ohne Belegfluss kann der Baustein
  keine Fakturazeile (`VBTYP_N = 'M'`) liefern, die Schleife täte nichts.
- `FILL_UEBAUF`, in REF und OPT gleich: `SORT ivbep BY vbeln posnr edatu etenr` statt ohne
  `etenr` (siehe Ergebnisse).
- `CONVERT_TO_LOCAL_CURRENCY` und `DATE_GET_WEEK` bleiben je Zeile, damit die Rundung gleich
  bleibt.

## Vergleich

`ZM_OFFENE_KDAUF_CMP` mit einer Variante von ZM_OFFENE_KDAUF (Liste in T76: `BI UPDATE`,
`SHOPFLOOR`, `KDAUF_INGO`, `OFFENE K-AUF`, `OTD` und weitere), optional mit eigenem Lieferdatum
und Werk. Reihenfolge der Prüfung:

1. REF gegen REF: Stimmt das Original mit sich selbst überein?
2. OPT gegen REF mit mehreren Varianten.
3. Erst danach als eigener Schritt ein Vorfilter auf das Werk, und nur, wenn der Vergleich
   sauber bleibt.

Zu beachten: Die Testdaten in T76 enden im März 2026.

Bedienung per Skript aus der Repository-Wurzel (Sitzung 1 in SE38):

```
sh saptasks/zm_offene_kdauf/einspielen.sh REF OPT CMP      # Quelltext setzen, aktivieren
cscript //nologo .tmp_sap_probe/SapGuiKdaufVergleich.vbs 1 - 1100 01.04.2026 30.06.2026
cscript //nologo .tmp_sap_probe/SapGuiKdaufVergleich.vbs 1 "BI UPDATE" - - - - - bg    # Job
cscript //nologo .tmp_sap_probe/SapGuiKdaufVergleich.vbs 1 - - - - - - zeig           # Jobergebnis
cscript //nologo .tmp_sap_probe/SapGuiListeAlleSeiten.vbs 1 > lauf.txt                # Liste lesen
```

Im Dialog gilt die Grenze von 600 s je Dialogschritt; größere Varianten als Job (`P_BG`).
REF gegen REF: `... 1 - 1100 <von> <bis> ZM_OFFENE_KDAUF_REF ZM_OFFENE_KDAUF_REF`.

## Nachtrag 2026-10-15: P_APP fuer die Fiori-App

`PARAMETERS p_app NO-DISPLAY` und Form `APP_EXPORT` (nach `materialanteil_rechnen`): Ergebnis ins Memory `ZM_OFFENE_KDAUF_APP`, kein ALV, keine Fortschreibung. Gesetzt vom DPC_EXT fuer `Zc23Set`. REF unveraendert, OPT/produktiv +14 Zeilen aus `bauen.js`. Aktiv in T76, Transport `T76K912738`. Details `saptasks/fiori_zc23/NOTES.md`.
