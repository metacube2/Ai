# ZM_OFFENE_KDAUF beschleunigen

Stand: 2026-10-12 (in Arbeit, noch nicht in T76 angelegt)

Auftrag Ingo 2026-10-12: Der Report liefert richtige Zahlen, ist aber sehr langsam. Frühere
schnelle Fassungen (verschiedene Claude-Versionen) hatten am Ende falsche Zahlen. Gearbeitet
wird mit einer Kopie, die gegen das Original verglichen wird. Das Original bleibt
unverändert; die schnelle Fassung kopiert Ingo erst nach bestandenem Vergleich zurück.

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
- `FILL_FKDAT`: `RV_ORDER_FLOW_INFORMATION` einmal je Position.
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
