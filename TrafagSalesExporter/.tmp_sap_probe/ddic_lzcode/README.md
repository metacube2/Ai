# DDIC-Feldlisten fuer ZSTR_LZCODE_USAGE / ZSTR_LZCODE_PARENT

Stand: 2026-07-21 (Feldliste 2026-07-21 am Live-System verifiziert)

**Status: Kopiervorlage fuer die MANUELLE SE11-Anlage.** Der urspruenglich geplante
Tool-Weg (SapProbe `rfc-call DDIF_TABL_PUT --table ...`) ist NICHT gangbar: `DDIF_TABL_PUT`/
`DDIF_TABL_ACTIVATE` sind auf T76 nicht RFC-freigegeben (Invoke-Test 2026-07-21: „ist nicht
'remote' aufrufbar"). Diese Dateien dienen daher als reine Referenz-/Abtippvorlage fuer SE11,
nicht als `--table`-Eingabe. Details: `docs/abap/README_LZCODE_WEBSERVICE.md`, Abschnitt
„Live-Verifikation 2026-07-21".

`usage_fields.csv` und `parent_fields.csv` enthalten die Feldliste aus
`docs/abap/README_LZCODE_WEBSERVICE.md` (Abschnitt "SE11 - Benoetigte DDIC-Strukturen") in einer
selbst gewaehlten, SAP-unabhaengigen Spaltenordnung (`FIELDNAME, ROLLNAME, BUILTIN_TYPE, LENG,
DECIMALS, KEYFLAG`) - **nicht** in der echten `DD03P`-Spaltennamens-Konvention. Grund: welche
Spalten `DDIF_STRU_PUT`'s Tabellenparameter (vermutlich `DD03P_TAB`) wirklich erwartet, ist bisher
nicht bestaetigt. Diese Datei ist die Rohdatenquelle, aus der die echte CSV fuer
`rfc-call ... --table` gebaut wird, sobald die Probe-Ergebnisse da sind.

## Voraussetzung, bevor daraus eine echte SapProbe-CSV wird

1. `function-search DDIF*STRU*` - ist `DDIF_STRU_PUT`/`DDIF_STRU_ACTIVATE` ueberhaupt RFC-faehig?
   Wenn nein: dieser ganze Ordner ist hinfaellig, anderer Weg noetig.
2. `function-info DDIF_STRU_PUT` - liefert die echten Parameternamen (Header-Struktur,
   Feldlisten-Tabelle) UND deren verschachtelte Feldnamen direkt mit (SapProbe druckt das jetzt
   automatisch bei TABLE/STRUCTURE-Parametern mit).
3. `table-fields MARA ZZLZCOD` - Zeile `ZZLZCOD`/`ZZLZCODSORT` in `usage_fields.csv` haengt
   direkt davon ab:
   - Liefert `ROLLNAME` einen echten Wert -> Zeile bleibt wie sie ist (`ROLLNAME=ZZLZCOD` bzw.
     `ZZLZCODSORT`).
   - Liefert `ROLLNAME` NICHTS (PAPH1-Falle, siehe `docs/abap/README_PRODSPARTE.md`) -> Zeile
     muss auf `BUILTIN_TYPE=CHAR` + die tatsaechliche `LENG` aus `table-fields MARA ZZLZCOD`
     (Spalte `LENG`) umgestellt werden, `ROLLNAME` dann leer lassen.

## Bekannte offene Annahmen in `usage_fields.csv`

- `EXKLUSIV`/`BAUGRUPPE` sind hier schon auf `BOOLE_D` gesetzt (nicht `ABAP_BOOL`) - siehe
  Warnung in der README zu diesem Punkt.
- `RICHTUNG`/`STUECKKOSTEN`/`WERT_*` nutzen bewusst `BUILTIN_TYPE` statt `ROLLNAME`, weil dafuer
  kein sinnvolles bestehendes Datenelement existiert (freie Textspalte bzw. Waehrungsbetrag ohne
  festen Bezug).
- Alle anderen `ROLLNAME`-Werte (MATNR, MSTAE, MENGE_D, MAKTX, MEINS, LABST, SALK3, DISMM, MINBE,
  DISLS, BSTFE, EISBE, MSTAV, BESKZ) sind Standard-SAP-Datenelemente und sollten in praktisch
  jedem System existieren - fuer diese ist keine Vorab-Pruefung noetig.

## Naechster Schritt, sobald die Probe-Ergebnisse da sind

Diese Datei in die vom `function-info`-Output bestaetigte Spaltenreihenfolge/Spaltennamen
umbauen (z. B. falls die Tabelle wirklich `DD03P_TAB` heisst und Spalten `FIELDNAME`, `POSITION`,
`KEYFLAG`, `ROLLNAME`, `DATATYPE`, `LENG`, `DECIMALS` erwartet), dann per

```text
rfc-call DDIF_STRU_PUT --set NAME=ZSTR_LZCODE_USAGE --struct <bestaetigter-Header-Param>=usage_header.csv --table <bestaetigter-Feldlisten-Param>=usage_fields_dd03p.csv --dry-run
```

zuerst als Dry-Run pruefen. Vor dem echten Schreiben (`--confirm-write`) erst an einem
Wegwerf-Objektnamen (z. B. `ZZ_TEST_STRUC`) testen und in SE11 gegenpruefen.
