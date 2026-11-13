# CoPaSumSet: Verkauf AE / Umsatz IST aus CO-PA (CE11000) fuer das Shopfloor-Cockpit

Stand: 2026-11-13 (Abschnitt 4: Stichtagsfilter `Hzdat`, T76 aktiv in **`T76K912906`**, nicht freigegeben). Davor 2026-11-10:
T76 aktiv, Auftrag **`T76K912866`** (Aufgabe `T76K912867`), ~~nicht freigegeben~~ *(ueberholt, gelesen 13.11. in T76 `E070`:
`T76K912866` und `T76K912867` stehen auf `R`, freigegeben am 12.11.; laut `ZCOPA_SUM_HZDAT_2026-11-13.abap` Kopf in P76)*.
Fachlich: `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 20 (Mail Fabio 09.11., Punkt d).

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

## 4. Stichtagsfilter `Hzdat` (2026-11-13, T76)

Auftrag Ingo 13.11. („bitte umsetzen“), Paket Fabio 13.11. Punkt 8: „Stichtag bis Vortag nach HZDAT (Angelegt am), nicht
BUDAT“. Vorgabe `ZCOPA_SUM_HZDAT_2026-11-13.abap`, genau so umgesetzt; zusaetzlich nur je eine Kommentarzeile mit dem Verweis
auf die Vorgabe (MPC vor der Property, DPC vor der neuen Schleife).

**Vorpruefung (RFC, T76):** `DD03L` `CE11000-HZDAT` vorhanden, Datenelement `ERDAT`, `DATS` 8. `TLOCK` ohne Eintrag fuer
`ZCL_ZPOWERBI%` und `ZSTR_COPA%` (Gegenprobe `HIKEY LIKE 'Z%'` liefert Zeilen, die Abfrage funktioniert). Alle Auftraege mit
den drei Objekten in `E071` stehen in `E070` auf `R` (zuletzt `T76K912866`/`867`, freigegeben 12.11.). Live-Stand beider
Methoden zeilengleich mit `MPC_EXT_DEFINE_neu.abap` / `DPC_EXT_GET_ENTITYSET_neu.abap` vom 10.11. (3'020 / 2'717 Zeilen, DPC
MD5 ohne CR `d7d42ae7…` wie in P76 laut `saptasks/shopfloor_sap/P76_BEFUND_2026-11-13.md` 3.1).

**Objekte, Auftrag `T76K912906`** (Workbench, Ziel P76, Text „Shopfloor: CoPaSumSet Hzdat, ShopRm/ShopAuf Filter“), Aufgabe
`T76K912907`, beide `D`. `E071` der Aufgabe genau: `LIMU TABD ZSTR_COPA_SUM`, `LIMU METH ZCL_ZPOWERBI_EINKAUF_MPC_EXT DEFINE`,
`LIMU METH ZCL_ZPOWERBI_EINKAUF_DPC_EXT /IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET`. Der Auftrag ist bewusst offen: der Plan
`saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md` legt zwei weitere Aenderungen in denselben Auftrag.

| Objekt | Aenderung | Ruecklesen |
| --- | --- | --- |
| `ZSTR_COPA_SUM` | Feld `HZDAT` Datenelement `CHAR8` an Position 9 (SE11, Zeile 8 vorher leer) | `DD03L`: 9 Felder, `HZDAT,0009,CHAR8,CHAR,8,A`, die acht alten unveraendert |
| `MPC_EXT->DEFINE` (`CM001`) | 7 Zeilen vor `bind_structure( ... 'ZSTR_COPA_SUM' ...)`: Property `Hzdat` (Edm.String, 8) | 3'027 Zeilen, `diff` gegen `MPC_EXT_DEFINE_neu_2026-11-13.abap` rc 0; gegen vorher 7 Zugaenge, 0 Abgaenge |
| `DPC_EXT->GET_ENTITYSET` (`CM01W`) | Zweig `CoPaSumSet`: `DATA` (3), Schleife Hzdat (14 inkl. Leer- und Kommentarzeile), `AND hzdat IN lr_cp_hzdat` in beiden SELECTs, `ls_cp_out-hzdat` | 2'737 Zeilen, `diff` gegen `DPC_EXT_GET_ENTITYSET_neu_2026-11-13.abap` rc 0; gegen vorher 20 Zugaenge, 0 Abgaenge |

Dateien: `*_vorher_2026-11-13.abap` (RFC-Stand vor der Aenderung), `*_neu_2026-11-13.abap` (erzeugt mit
`methoden_bauen_hzdat_2026-11-13.py`, nur Einfuegungen, jeder Anker auf Eindeutigkeit geprueft). Eingespielt in eigener Sitzung
`/app/con[0]/ses[1]` (Ingos `ses[0]` unberuehrt), beim Sichern beider Methoden `T76K912906` ausdruecklich gesetzt, Syntaxpruefung
je „keine Syntaxfehler“, beide Klassen einzeln vom SE24-Einstieg aktiviert („Objekt(e) wurde(n) aktiviert“, kein Fehlerdialog).
Caches T76: `/IWBEP/CACHE_CLEANUP` alle Modelle („erfolgreich abgeschlossen“), dann `/IWFND/CACHE_CLEANUP`
`ZPOWERBI_EINKAUF_MDL` („erfolgreich beendet“). ST22 „Heute“: keine Kurzdumps.

**Tests T76 (Gateway Client, Antwortrumpf gelesen), Periode 003.2026, Bukrs 1100:**

| Aufruf | vorher (13:24) | nachher (13:31) |
| --- | --- | --- |
| `Bukrs/Gjahr/Perde` ohne Hzdat | 200, 7'416 Byte, 15 Zeilen | 200, 7'581 Byte (15 × `"Hzdat":""`), Netto, Vv240, Anzahl je Zeile gleich vorher, `Hzdat` leer |
| dazu `and Hzdat le '20260315'` | 400 „Eigenschaft Hzdat in Typ CoPaSum nicht gefunden“ | 200, 15 Zeilen, `Hzdat` = `20260315`, je Zeile gleich der Gegenprobe |
| dazu `and Hzdat le '20260331'` | – | 200, gleich Gegenprobe; F 0008 Anzahl 481 statt 482 (ein Satz mit HZDAT 14.04.2026, Netto 0) |
| ohne Filter / nur `Hzdat` | ohne Filter 400 (Pflichtfilter); nur `Hzdat` vorher nicht gefahren | beide 400 „CoPaSumSet braucht Bukrs, Gjahr und Perde“ |
| Zweig mit `Vrgar eq 'F'` (zweiter SELECT), ohne / mit `Hzdat le '20260315'` (14:03, zweite eigene Sitzung) | – | 200, 7 Zeilen; 8'474'548.74 / 2'392 bzw. 3'948'138.54 / 1'120, gleich F in der Tabelle unten |
| `$metadata` | – | 200, 386'192 Byte |
| `QmMeldungSet?$top=2`, `ShopRmSet` MLE01 14.04.2026 | – | 200; 200 mit 197 Stk. / 6 (gleich Abschnitt 3 in `saptasks/shopfloor_sap/NOTES.md`) |

Summen je Satzart (Netto CHF, Buchungskreiswaehrung, Anzahl Saetze):

| Satzart | ganze Periode | bis 15.03.2026 | bis 31.03.2026 |
| --- | ---: | ---: | ---: |
| A Auftragseingang | 8'996'500.23 / 13'577 | 3'476'745.63 / 7'504 | 8'996'500.23 / 13'577 |
| F Faktura | 8'474'548.74 / 2'392 | 3'948'138.54 / 1'120 | 8'474'548.74 / 2'391 |

Beispiel je Zeile (A, Sparte 0003): ganze Periode 4'593'795.77 / 2'657 Saetze, bis 15.03. 1'477'515.32 / 1'530.
Kleinste Sparte A 0010: 890.00 / 5 ganze Periode, bis 15.03. 890.00 / 3 (zwei spaetere Saetze mit zusammen Netto 0).

**Gegenprobe CE11000:** per RFC (`RFC_READ_TABLE`, gleiche Tabelle wie SE16), `PALEDGER = '01' AND BUKRS = '1100' AND GJAHR = '2026'
AND PERDE = '003' AND VRGAR IN ('A','F')`, Felder `VRGAR, WWPSP, HZDAT, VV100 … VV250`: 15'969 Saetze. Netto je Satz
`VV100 + VV110 - VV200 - VV210 - VV220 - VV250 - VV230 - VV240` wie im DPC, dann nach Satzart/Sparte summiert, einmal ohne und
einmal mit `HZDAT <= Stichtag`. Ergebnis: in allen drei Faellen fuer alle 15 Gruppen Netto, Vv240 und Anzahl gleich dem Set
(0 Abweichungen). SE16 selbst wurde nicht benutzt, weil dort Wertfelder breiter Tabellen fehlen und `PALEDGER` nicht
eingeschraenkt werden kann (`docs/rag/LEARNINGS.md` 2026-11-10). Hilfsskripte (Scratchpad, nicht im Repo): `ce_auswertung.py`,
`vergleich.py`, `GwGetLesenT76.vbs` (Gateway Client T76, Rumpf ueber `GetLineText` auf `shellcont[3]`, Sitzung ueber die gemerkte Id).

Verteilung `HZDAT` in 003.2026 (Auszug, kumuliert Netto): A 02.03. 636'143.89, 13.03. 3'476'745.63, 31.03. 8'996'500.23;
F 02.03. 209'770.63, 13.03. 3'948'138.54, 31.03. 8'474'548.74. Die Teilsumme ist nicht monoton (A 18.03. 4'966'572.15,
19.03. 4'893'123.90), weil Gutschriften negativ eingehen; „kleiner oder gleich“ ist also nur zusammen mit der Gegenprobe ein Beleg.

Nebenbefund Sprache: Die 400-Antworten derselben Sitzung trugen abwechselnd `"lang" : "en"` und `"de"` (13:24:55 en, 13:24:56 de,
13:31:24 en, 13:31:25 de). Fuer `CoPaSumSet` ohne Belang (keine Texte), fuer sprachabhaengige Felder siehe
`saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md` 1.4.

Eigene Sitzungen: `/app/con[0]/ses[1]` fuer Einspielen und Tests bis 13:56 (danach Teil 2 des Auftrags), `/app/con[0]/ses[2]` 14:03
nur fuer den `Vrgar`-Test; beide mit `/i` geschlossen, danach in T76 nur Ingos `ses[0]`. Die Skripte `SapGuiKlasseAendernMitAuftrag.vbs`
und `SapGuiKlassenMethodeQuelltext.vbs` pruefen die Sitzungs-Id nicht; sie liefen nur mit dem Index aus `neu` und mit `info` direkt davor.

### Nach Freigabe und Import (Ingo)

1. `T76K912906` erst freigeben, wenn die Aenderungen aus `saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md` drin oder
   bewusst weggelassen sind (gleicher Auftrag, gleiche Klassen).
2. P76: `/IWBEP/CACHE_CLEANUP` (alle Modelle), dann `/IWFND/CACHE_CLEANUP` (`ZPOWERBI_EINKAUF_MDL`), danach im Gateway Client
   `CoPaSumSet` mit `and Hzdat le '<Vortag>'` (200, `Hzdat` gefuellt; ohne Import 400 „Eigenschaft Hzdat … nicht gefunden“).
3. Cockpit-Schalter `Shopfloor:SapCoPaHzdat` erst danach; Vorschau gegen Fabios Kontrollwerte
   (`docs/shopfloor_paket_2026-11-13/2026-11-12_verkauf/Kontrollwerte_Verkauf_CE11000_2026-11.csv`, z. B. bis 10.11.:
   umsatz_ist 2'391'295.84, u_tx 807'998.72, u_dw 1'219'138.35).
