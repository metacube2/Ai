# Forecast Dichtesensoren (DS): Spalte «Nächster Vorgang»

Stand 19.11.2026. Wunsch von Sergio Rodrigues (Mail an Fabio, «Shopfloor: Woher kommen eure Zahlen?»): Bei den Dichtesensoren soll im Forecast der nächste Vorgang des Auftrags sichtbar sein, wie in der MD04.

## Was die MD04 zeigt

Bilder: `Quellen/Sergio_MD04_Naechster_Vorgang_1.png` und `_2.png` (Material 64941).

- Spalte «Nächster Vorgang», technisch Struktur `MDEZ`, Feld `USEX1` (Datenelement `USEEX`), Programm SAPMM61R, Dynpro 0750.
- Die F1-Hilfe sagt: «Feld zum Füllen durch User Exit».
- Format im Bild: `<Vorgang> / <Arbeitsplatz>`, z. B. `0800 / DSQ72`, `0550 / EL930`, `0700 / EL911`, `0010 / DL068`.
- Nur Zeilen von Fertigungsaufträgen haben einen Wert; Planaufträge bleiben leer.

## Wo die Logik steht (in P76 gelesen am 19.11.2026)

| Tabelle | Schlüssel | Wert |
|---|---|---|
| MODSAP | MEMBER = EXIT_SAPMM61R_001 | Erweiterung M61X0002 «User Exits PP-MRP Bedarfsplanung Auswertungen» |
| TFDIR | FUNCNAME = EXIT_SAPMM61R_001 | Funktionsgruppe XM61, Include-Nummer 04 |
| TRDIR | NAME = ZXM61U* | ZXM61U01, angelegt I001067 am 18.03.2014, zuletzt geändert LAA am 29.07.2024 |

Den Quelltext von ZXM61U01 konnte ich nicht lesen (keine Berechtigung für CODE_SCANNER in P76). Welche Regel «nächster Vorgang» genau bestimmt (z. B. erster nicht endrückgemeldeter Vorgang), ist deshalb nicht belegt.

## Bitte an Ingo

1. **Backend (Forecast-Import, 04:00):** Je Auftrag ein neues Feld `naechster_vorgang` mitliefern, mit genau dem Wert, den die MD04 in `MDEZ-USEX1` zeigt. Am besten die Logik aus ZXM61U01 übernehmen oder dieselbe Routine aufrufen, statt sie nachzubauen. Format `VORNR / ARBPL`. Planaufträge leer lassen.
2. Vorschlag: für alle Fertigungsaufträge füllen, nicht nur für DS. Die Spalte erscheint im Forecast dann in jeder Abteilung, die Werte hat. Wenn es nur für DS sein soll, im Backend auf die DS-Disponenten (DS1, DS2, DSN) beschränken.
3. **Frontend:** Patch 09 (`Patch_09_naechster_vorgang`) zeigt die Spalte in der Auftragsliste des Forecasts, sobald mindestens ein Auftrag das Feld hat. Ohne Feld bleibt die Ansicht unverändert; der Patch kann deshalb schon vor dem Backend eingespielt werden.
4. **Aktualität:** Die MD04 zeigt den Stand live. Im Dashboard ist der Wert so alt wie der letzte Forecast-Import (heute 04:00). Wenn das für die Dichtesensoren nicht reicht, bitte einen zweiten Lauf über Mittag vorsehen.

## Abnahme

Für zwei oder drei DS-Aufträge der nächsten Woche den Wert im Dashboard mit der MD04 desselben Zeitpunkts vergleichen (z. B. Material C15610, Aufträge 1279556 bis 1279559, und Material 34063, Auftrag 1278404).
