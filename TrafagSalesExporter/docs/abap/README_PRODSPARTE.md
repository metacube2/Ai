# ABAP Produktsparten-Mapping

Stand: 2026-05-28

## Dateien

| Datei | Zweck |
| --- | --- |
| `ZCL_PRODSPARTE_PROVIDER.abap` | Wiederverwendbare Provider-Klasse fuer ALV und spaeter OData |
| `Z_PRODSPARTE_REPORT.abap` | Schlanker ALV-Testreport |
| `Z_PRODSPARTE_MAP_BUILD.abap` | Baut `ZPRODSPARTE_MAP` aus eindeutigen CO-PA-Kombinationen |

## Benoetigte SAP-Objekte

- Transparente Tabelle `ZPRODSPARTE_MAP`
  - `MANDT`
  - `PAPH1`
  - `WWPFA`
  - `WWPSP`
  - `CRDATE`
  - `CRUSER`
- Klasse `ZCL_PRODSPARTE_PROVIDER`
- Report `Z_PRODSPARTE_REPORT`
- Report `Z_PRODSPARTE_MAP_BUILD`

## Anlage In SAP

- `ZCL_PRODSPARTE_PROVIDER.abap` ist eine globale Klasse bzw. ein Class Pool, kein ausfuehrbarer Report.
  - In SE24 als Klasse `ZCL_PRODSPARTE_PROVIDER` anlegen und Definition/Implementation uebernehmen.
  - Alternativ in SE38/ADT als Programtyp `Class Pool` anlegen; die Datei beginnt deshalb mit `CLASS-POOL zcl_prodsparte_provider`.
- `Z_PRODSPARTE_REPORT.abap` und `Z_PRODSPARTE_MAP_BUILD.abap` sind normale ausfuehrbare Reports.

Optional fuer Gateway/DDIC:

- Struktur `ZSTR_PRODSPARTE_OUT`
- Tabellentyp `ZTT_PRODSPARTE_OUT`

## Gepruefte Anpassungen Gegenueber Erstentwurf

- Provider-Logik aus Report in globale Klasse ausgelagert.
- `MAKT` als `LEFT OUTER JOIN`, damit Materialien ohne Text nicht verloren gehen.
- `VTWEG` als optionaler Parameter.
- Bei mehreren Vertriebswegen gewinnt bewusst der kleinste `VTWEG`.
- Fallback setzt technischen Code `UNASS`, Text `Nicht zugeordnet` und `IS_ASSIGNED = abap_false`.
- `gt_ambig` im Mapping-Build ist korrekt als `ty_combo` typisiert.
- `p_erkrs` wurde entfernt, weil der Report fix aus `CE11000` liest.
- Leerschreiben von `ZPRODSPARTE_MAP` wird verhindert, wenn keine eindeutigen Saetze aufgebaut wurden.

## Noch Fachlich/Technisch Zu Pruefen

- Ist `PAPH1 = MVKE-PRODH(5)` im Trafag-System exakt korrekt?
- Sind `T25A0` fuer Produktfamilie und `T25A1` fuer Produktsparte die richtigen Texttabellen?
- Ist `CE11000` der richtige CO-PA-Einzelposten fuer den relevanten Ergebnisbereich?
- Ist Fallback-Code `UNASS` in Feld `WWPSP` lang genug/zulässig?
- Soll `VTWEG` zwingend selektiert werden statt "kleinster VTWEG gewinnt"?
