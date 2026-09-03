# ZZPRDAT-Prototyp in T76/100

## Ziel und Grenze

Beim erstmaligen Freigeben eines Fertigungsauftrags wird der zu diesem Zeitpunkt
gueltige Eckendtermin (`GLTRP`) einmalig nach `AUFK-ZZPRDAT` kopiert. Spaetere
Terminverschiebungen duerfen `ZZPRDAT` nicht mehr aendern.

Der Prototyp ist ausschliesslich fuer **T76, Mandant 100, Paket `$TMP`** vorgesehen.
Keine Aenderung und kein Test in P76; kein Transportauftrag.

## Live bestaetigte Ausgangslage

- Feld: `AUFK-ZZPRDAT`, Typ `DATS`, Datenelement `ZCO_GLTRP`.
- Erweiterungsprojekt: `ZPP00012`, Komponente `PPCO0012`.
- In `ZXCO1U11` ist die alte Schreiblogik vollstaendig auskommentiert. Sie pruefte
  `FTRMI = SY-DATUM`, ein initiales `ZZPRDAT` und uebernahm `GLTRP`.
- Der alte Weg haengt vom Trafag-Dynpro ab und deckt MD04, CO40, CO41, COHV und
  automatische Freigaben nicht verlaesslich ab.
- Das BAdI `WORKORDER_UPDATE` mit Methode `AT_RELEASE` existiert im T76.
- Der historische Auftrag `1194970` zu Kundenauftrag `399566` hat
  `AUFK-ZZPRDAT = 00000000`; er war frueher freigegeben, ist heute aber technisch
  abgeschlossen und eignet sich deshalb nur als Fehlernachweis, nicht als neuer
  Erstfreigabetest.

## Vorgeschlagene temporaere Objekte

1. Funktionsgruppe in `$TMP`, zum Beispiel `ZPP_ZZPRDAT_TEST`.
2. Update-Funktionsbaustein `Z_PP_PRDDAT_SET` mit:
   - `IV_AUFNR TYPE AUFNR`
   - `IV_PRDDAT TYPE ZCO_GLTRP`
   - Verarbeitungsart: Update-Modul (Start sofort).
3. BAdI-Implementierung in `$TMP`, zum Beispiel `Z_ZZPRDAT_AT_RELEASE`, fuer
   `WORKORDER_UPDATE`.

Die einzusetzenden Vorlagen liegen unter:

- `saptasks/zzprdat/Z_PP_PRDDAT_SET.abap`
- `saptasks/zzprdat/WORKORDER_UPDATE_AT_RELEASE.abap`

## Vor Aktivierung im Methodeneditor pruefen

Die Methode `AT_RELEASE` und die Komponenten `IS_HEADER_DIALOG-AUFNR` sowie
`IS_HEADER_DIALOG-AUTYP` sind bestaetigt. Der Zugriff auf
`IS_HEADER_DIALOG-GLTRP` muss einmal per Syntaxpruefung im T76 bestaetigt werden,
weil die lokale Struktur `COBAI_S_HEADER_DIALOG` nicht RFC-lesbar war.

Falls `GLTRP` dort nicht enthalten ist, den Prototyp nicht passend machen oder
mit einem Ersatzdatum aktivieren. Dann auf `BEFORE_UPDATE` wechseln und den neuen
Kopf aus `IT_HEADER` verwenden; die genaue Tabellenzeilenstruktur zuerst im
Methodeneditor anzeigen.

## Testreihenfolge

1. Separaten, noch nicht freigegebenen Testauftrag in T76/100 anlegen.
2. Vor Freigabe pruefen: `AUFK-ZZPRDAT` ist initial.
3. BAdI-Implementierung und Update-Funktionsbaustein aktivieren.
4. Auftrag in CO02 freigeben und sichern, ohne den Tab **Trafag Daten** zu
   besuchen.
5. Pruefen: `AUFK-ZZPRDAT = AFKO-GLTRP` zum Freigabezeitpunkt.
6. Eckendtermin aendern und erneut sichern.
7. Pruefen: `AFKO-GLTRP` ist neu, `AUFK-ZZPRDAT` bleibt unveraendert.
8. Einen zweiten Auftrag ueber den realen Sammel-/Umsetzungsweg (MD04 oder CO40;
   fuer CZ zusaetzlich CO41/COHV) freigeben und dieselben Pruefungen wiederholen.
9. Erst nach erfolgreichem T76-Nachweis Disposition und Marco das Ergebnis zur
   fachlichen Abnahme geben. P76 und Transport bleiben ein separater Entscheid.

## Erwartete Ergebnisse

| Pruefung | Erwartung |
|---|---|
| Auftrag nur angelegt | `ZZPRDAT` initial |
| Erste Freigabe | `ZZPRDAT` entspricht dem damaligen `GLTRP` |
| Spaetere Terminverschiebung | `ZZPRDAT` unveraendert |
| Trafag-Tab nie besucht | Ergebnis trotzdem korrekt |
| Wiederholtes Sichern/Freigeben | Kein Ueberschreiben |

## Rueckfall und Diagnose

Die Implementierung liegt in `$TMP` und kann im T76 deaktiviert werden. Der
Prototyp enthaelt keinen `COMMIT WORK` und schreibt nur bei initialem `ZZPRDAT`.

Falls `ZZPRDAT` nach der Freigabe leer bleibt, zuerst SM13/ST22 pruefen. Eine
moegliche Ursache ist die Reihenfolge der Update-Tasks beim erstmaligen Anlegen.
Dann die `AT_RELEASE`-Variante nicht produktiv weiterverwenden, sondern die
`IN_UPDATE`-/`BEFORE_UPDATE`-Variante mit den live sichtbaren Kopfdaten entwickeln
und erneut isoliert in T76 testen.

## Aktueller Umsetzungsstand / Uebergabe

Der detaillierte Live-Stand, die in T76/100 bereits angelegten `$TMP`-Objekte und
die exakten Fortsetzungsschritte stehen im Abschnitt **Arbeitsuebergabe 2026-09-03**
in `saptasks/zzprdat-kontext.md`. Kurzstand: Hilfsbaustein gespeichert und
syntaxfehlerfrei, Aktivierungsdialog noch nicht bestaetigt; BAdI-Implementierung
gespeichert aber inaktiv und noch ohne Methodencode; kein Testauftrag angelegt.
