# CoPaSumSet: Verkauf AE / Umsatz IST aus CO-PA (CE11000) fuer das Shopfloor-Cockpit

Stand: 2026-11-10. T76 aktiv, Auftrag **`T76K912866`** (Aufgabe `T76K912867`), nicht freigegeben. Fachlich:
`docs/SHOPFLOOR_2026-10-07.md` Abschnitt 20 (Mail Fabio 09.11., Punkt d).

## 1. Fachliche Grundlage (P76 lesend, 10.11.)

SE16 `CE11000`, Periode `009.2026` (externe Form, intern `PERIO 2026009`, `GJAHR 2026`, `PERDE 009`), 71'544 Zeilen, per
Spaltenauswahl nur `PALEDGER VRGAR PERIO BUKRS WWPSP VV100 VV110 VV200 VV210 VV220 VV230 VV240 VV245 VV250`, `%PC` gesichert.
Buchungskreis 1100, Waehrungstyp 01:

| Satzart | VV100 + VV110 - VV200 - VV210 - VV220 - VV250 - VV230 | - VV240 | Fabio (KE30 Z0001-000006) |
| --- | ---: | ---: | ---: |
| F Faktura | 7'878'417.89 | **7'878'114.69** | 7'878'114.69 |
| A Auftragseingang | 6'531'394.41 | **6'531'197.94** | 6'531'197.94 |

`VV245` ist 0, `VV250` 0. Je Sparte (WWPSP, CE11000 schreibt `0003`, `0005` usw.): F 0003 DW 3'613'791.49, F 0005 Transmitter
2'940'107.89; A 0003 2'768'728.51, A 0005 2'684'433.17. Buchungskreis 1200 hat eigene Zeilen (nicht in Fabios Wert).

## 2. Objekte (T76, `T76K912866`)

| Objekt | Inhalt |
| --- | --- |
| Struktur `ZSTR_COPA_SUM` (Paket `ZPP`) | `BUKRS CHAR4, GJAHR CHAR4, PERDE CHAR3, VRGAR CHAR1, WWPSP CHAR10, NETTO /SCMTMS/DECIMAL_VALUE, VV240 /SCMTMS/DECIMAL_VALUE, ANZAHL INT4` (`ZSTR_COPA_SUM_felder.txt`) |
| `ZCL_ZPOWERBI_EINKAUF_MPC_EXT->DEFINE` | Entity `CoPaSum` (Schluessel Bukrs/Gjahr/Perde/Vrgar/Wwpsp), Set `CoPaSumSet`, Block am Ende |
| `ZCL_ZPOWERBI_EINKAUF_DPC_EXT->GET_ENTITYSET` | Zweig `CoPaSumSet` nach `QmMeldungSet`: Pflichtfilter Bukrs, Gjahr, Perde (sonst 400), optional Vrgar; `SELECT ... SUM(...) COUNT(*) FROM ce11000 WHERE paledger = '01' AND bukrs/gjahr/perde AND vrgar IN (A, F) GROUP BY vrgar wwpsp`, Netto wie oben |

Quelle der Wahrheit `ZCOPA_SUM_ADD.abap`; `*_vorher.abap` (Live-Stand, zeilengleich mit QmMeldungSet- bzw. Vorrat-Fassung),
`*_neu.abap` (erzeugt mit `methoden_bauen.py`). Eingespielt in eigener Sitzung (`ses[2]`), beide Methoden mit `T76K912866`
gesichert, Klassen einzeln ueber SE24 aktiviert, **rueckgelesen identisch** (MPC 3'020, DPC 2'717 Zeilen). E071 `T76K912867`:
`R3TR TABL ZSTR_COPA_SUM`, `LIMU METH ...MPC_EXT DEFINE`, `LIMU METH ...DPC_EXT GET_ENTITYSET`. Caches T76 (`/IWBEP` alle, `/IWFND`
Modell) geleert; Gateway Client: `CoPaSumSet` mit Filter 200 (7'416 Byte, Periode 003.2026), ohne Filter 400, `QmMeldungSet` 200.

## 3. Nach dem Import (Ingo)

1. P76: `/IWBEP/CACHE_CLEANUP` (alle Modelle), dann `/IWFND/CACHE_CLEANUP` (`ZPOWERBI_EINKAUF_MDL`).
2. Vorschau `GET shopfloor/api/sap/copa?tag=2026-10-01` (= September): erwartet ae_aktuell 6'531'197.94, umsatz_ist 7'878'114.69.
3. `Shopfloor:SapCoPaEnabled = true`, Deploy; Historie nur leere Tage (Paket 16.10.).
