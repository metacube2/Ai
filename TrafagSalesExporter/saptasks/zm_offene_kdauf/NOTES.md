# ZM_OFFENE_KDAUF beschleunigen

Stand: 2026-10-12, in T76 angelegt und verglichen; Zurückkopieren und Entscheide offen

Auftrag Ingo 2026-10-12: Der Report liefert richtige Zahlen, ist aber sehr langsam. Frühere
schnelle Fassungen (verschiedene Claude-Versionen) hatten am Ende falsche Zahlen. Gearbeitet
wird mit einer Kopie, die gegen das Original verglichen wird. Das Original bleibt
unverändert; die schnelle Fassung kopiert Ingo erst nach bestandenem Vergleich zurück.

## Stand in T76 (2026-10-12)

`ZM_OFFENE_KDAUF_REF`, `ZM_OFFENE_KDAUF_OPT` und `ZM_OFFENE_KDAUF_CMP` sind lokale Objekte
(`$TMP`, kein Transport), aktiv. REF und OPT sind per SE38-Kopie aus dem Original entstanden
(mit Textelementen, Oberfläche und Dynpros), danach mit dem Quelltext aus diesem Ordner
überschrieben. Das Original `ZM_OFFENE_KDAUF` ist unverändert.

Nebenbefund: Im Objektkatalog steht `ZM_OFFENE_KDAUF_TURBO` (Paket ZPP) ohne Programm in
TRDIR, vermutlich ein Rest eines früheren Versuchs. Nicht angefasst.

## Ergebnisse

| Lauf | Zeilen | Ergebnis | OPT | REF |
| --- | ---: | --- | ---: | ---: |
| 01.–15.03.2026, Werk 1100, ohne ETENR-Sortierung, OPT gegen REF | 1'108 | 4 Zeilen mit vertauschten Aufträgen | 29 s | 45 s |
| dieselbe Selektion, REF gegen REF | 1'108 | 6 Zeilen mit vertauschten Aufträgen: **das Original ist nicht reproduzierbar** | | |
| dieselbe Selektion mit ETENR-Sortierung, REF gegen REF | 1'108 | identisch | | |
| dieselbe Selektion mit ETENR-Sortierung, OPT gegen REF | 1'108 | **identisch**, alle 60 Felder, Summen, Listenreihenfolge | 28 s | 45 s |
| 01.04.–30.06.2026, Werk 1100, OPT gegen REF | 4'392 | **identisch** | 59 s | 158 s |
| Variante `KDAUF_INGO` (fest 29.–31.12.2017) | 4 | identisch | 1 s | 0.3 s |
| Variante `BI UPDATE` im Dialog | | TIME_OUT nach 600 s im Belegfluss-Baustein; seither als Hintergrundjob | | |

Wo die Zeit im Original liegt (Q2 2026, 158 s): Belegfluss `RV_ORDER_FLOW_INFORMATION` je
Einteilung 51 s, `Z_BASISPREISLISTE` je Einteilung 36 s, übrige Einzelzugriffe rund 70 s.
In OPT bleiben 36 s Belegfluss (2'363 Aufrufe, 2'148 gespart, weil der Beleg gar keinen
Belegfluss hat) und 20 s `Z_BASISPREISLISTE` (einmal je Material). Alles andere ist auf
1.5 s Vorablesen geschrumpft.

**„Identisch“ heißt: identisch zu REF mit ETENR-Sortierung.** Gegen eine alte Ausgabe des
unveränderten Originals können sich Positionen mit mehreren Fertigungsaufträgen und gleichem
Lieferdatum in der Zuordnung der Aufträge unterscheiden, weil das Original sie zufällig
vergibt.

## Entscheide für Ingo

1. **ETENR als letztes Sortierfeld übernehmen** (empfohlen). Macht das Ergebnis
   reproduzierbar; ändert nur die bisher zufällige Zuordnung bei gleichem Lieferdatum.
2. Eigenheiten des Originals beheben oder lassen (siehe unten Punkt 7), jeweils einzeln.
3. Nächster Tempo-Schritt nur mit neuem Objekt: Belegfluss parallel über einen
   RFC-fähigen Wrapper-Baustein. Das bleibt exakt, braucht aber einen Transport.
4. `P_UPDBI` (Fortschreibung `ZPOWERBI_KDAUF`) ist nicht gemessen: `NUMBER_GET_NEXT` je
   Zeile. Ein Aufruf mit `QUANTITY` wäre schneller, ändert aber die ID-Werte.
5. Zurückkopieren: OPT enthält den Vergleichsschalter `P_VGL` (NO-DISPLAY) und Zeitmessung.
   Beides ist harmlos und erlaubt später wieder einen Vergleich; ohne sie gibt es eine
   bereinigte Fassung auf Wunsch.

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
7. **Unveränderte Eigenheiten des Originals, als eigene Entscheidung für Ingo:**
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

## Was OPT ändert

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
