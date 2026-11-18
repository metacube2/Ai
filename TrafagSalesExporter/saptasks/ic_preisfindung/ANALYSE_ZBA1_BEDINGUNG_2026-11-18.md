# Analyse: Nachrichtenschema V10000, Bedingung 902 fuer ZBA1 „AB final“ am Testauftrag 411403/10 (T76/100)

Stand: 2026-11-18 18:21 (Uhrzeit abgelesen; gelesen 17:53 bis 18:19, Agent sap-t76, ausschliesslich per RFC als KOI in T76
Mandant 100, `travt762`). Nichts geschrieben, nichts ausgefuehrt, keine GUI-Sitzung benutzt, P76 nicht beruehrt. Die Belegdaten
sind eine Momentaufnahme (NAST, CDHDR und VBEP zu 411403 um 18:16 nochmals gelesen, unveraendert); gleichzeitig laeuft der Test
T441V 02/AE (`PLAN_T441V_MIT_BESTELLUNGEN_2026-11-18.md`), ein weiterer V_V2-Lauf kann Einteilungen und Nachrichten des Auftrags
wieder aendern.

Auftrag Ingo 18.11.2026 abends, Frage Fabio Palma im Wortlaut:

> Kannst du bitte im Nachrichtenschema V10000 die Bedingung 902 fuer ZBA1 „AB final“ ansehen? Beim Testauftrag 411403,
> Position 10, war der Planauftrag 2431940 um 15:42 Uhr fixiert. Auch nach V_V2 meldet die Findungsanalyse 502. Welche
> Voraussetzung fehlt hier genau, und wodurch wird ZBA1 nach der Fixierung automatisch erneut geprueft?

## 0. Ergebnis in vier Saetzen

1. **ZBA1 ist beim Auftrag 411403 inzwischen gefunden und verarbeitet:** NAST-Satz vom 18.11.2026 16:53:30, verarbeitet 16:53:31,
   in derselben Sekunde, in der ein V_V2-Lauf von FPA den Auftrag geaendert hat (neue Einteilung 0002 mit 60 ST bestaetigt zum
   04.12.2026). Nach heutigem Datenstand ist jede Abfrage der Bedingung 902 erfuellt.
2. **„502“ ist keine Bedingung, sondern die Meldung VE 502** „Nachricht ignoriert (Bedingung & nicht erfüllt)“; die Variable
   ist die Bedingungsnummer aus dem Schema, hier 902.
3. **Was bis 16:53:30 fehlte (hergeleitet, nicht direkt gelesen):** die bestaetigte Menge der Position. `VBAP-KBMENG` ist im
   Unvollstaendigkeitsschema Z3 des Positionstyps TAN ein Pflichtfeld; solange sie 0 ist, ist die Position unvollstaendig, der
   Kopfstatus „alle Positionen“ (`UVALS`) ist nicht C, und die erste Abfrage von `KOBED_902` lehnt ab. Den Planauftrag prueft
   die Bedingung 902 an keiner Stelle.
4. **Erneut geprueft wird ZBA1 nur, wenn der Kundenauftrag selbst gesichert wird** (VA02 oder ein V_V2-Lauf, der den Auftrag
   sichert). Das Fixieren des Planauftrags stoesst die Nachrichtenfindung des Kundenauftrags nicht an. Eine Z-Logik, die
   ZBA1 nachzieht, habe ich in den gelesenen Objekten nicht gefunden; der einzige eingeplante Lauf ist der Job
   `ZSD_NEUTERMIN_AT` (V_V2 im Hintergrund), dessen Variante in T76 das Testmaterial nicht enthaelt.

## 1. Schema und Nachrichtenart

| Punkt | Befund | Beleg |
| --- | --- | --- |
| Schema der Auftragsart | Auftragsart TA: Applikation V1, Nachrichtenschema **V10000**, Vorschlag BA00 | TVAK `AUART TA`: `KAPPL V1`, `KALSM V10000`, `KSCHL BA00` |
| Schemazeile ZBA1 | Stufe 011, Zaehler 001, **Bedingung 902** | T683S `MANDT 100, KVEWE B, KAPPL V1, KALSM V10000, STUNR 011, ZAEHK 001, KSCHL ZBA1, KOBED 0000902` |
| Uebrige Zeilen | 001 ESYM (22), 010 BA00 (2), 011/002 ZBA2 (0), 012/001 ZE01 (902), 012/002 ZE02 (0), 016 ZM01 (2), 020 MAIL (0), 030 KRML (9), 040 BAV0 (0), 050 ZV01 (0) | T683S, 11 Zeilen |
| Bedingung 902 | Gruppe PBEN (Bedingungen Nachrichten), aktiv, Applikation V1; im Sammelinclude eingebunden | TFRM `GRPZE PBEN, GRPNO 902, AKTIV X, KAPPL V1`; `RV61BNNN`: `INCLUDE RV61B902 . "Auftragsbestätigung` |
| Texte | ZBA1 „AB final“, ZBA2 „AB neuer LF Termin“, BA00 „Auftragsbestätigung“ | T685T, Sprache D |
| Zugriffsfolge | ZBA1: Z001 (Zugriff 001 Tabelle 150 exklusiv, Zugriff 010 Tabelle 013) | T685 `KOZGF Z001`; T682I |
| Konditionssatz | Tabelle 013 (Verkaufsorganisation): 1200 → Satz 2430, Partnerrolle AG, Medium 5 (externes Senden), **Zeitpunkt 4 (sofort)**; 1100 → Satz 1945. Tabelle 150 hat fuer ZBA1/1200 keinen Satz | B013, B150, NACH |
| Nachrichtenart | ZBA1: Zugriff auf Konditionen X, Vorschlag Medium 1, Zeitpunkt 1, Partner AG; Programm zur Versandzeit `Z_RSFNDTIM_ZBA1`, Routine `OUTPUT_TIME`; Mehrfachversendung leer | T685B `MANDT 100, KAPPL V1, KSCHL ZBA1` |
| Verarbeitung | Medium 1 und 5: Programm `Z_RVADOR01`, Routine `ENTRY`, Formular `Z_RVORDER01` | TNAPR |

Mandant 090 ist **nicht geprueft**: `table-read` (RFC_READ_TABLE) liefert nur den Anmeldemandanten 100, alle gelesenen Zeilen
tragen `MANDT 100`. Belegt ist nur, dass die Nachrichtenart ZBA1 am 06.11.2026 im Mandanten 090 gepflegt und transportiert
wurde (E071K zu T76K912849/850: `T685B 090V1ZBA1`, Text „IC AT: Nachrichtenart ZBA1 anpassen“, MEEY_HAST, freigegeben).

## 2. Was „502“ ist

Meldung der Findungsanalyse, nicht eine zweite Bedingung:

- T100, Sprache D, `ARBGB VE`, `MSGNR 502`: **„Nachricht ignoriert (Bedingung & nicht erfüllt)“**.
- Die Nachrichtenfindung schreibt sie an zwei Stellen ins Protokoll, jeweils mit der Bedingungsnummer aus T683S als Variable:
  - Vorstep, Include `LV61BF0K`, Form `KONDITIONSVORSTEP`, Zeile 200 bis 213: `kon_bev_frm-ind = t683s-kobed.`
    `PERFORM (kon_bev_frm) IN PROGRAM saplv61b IF FOUND.` … `koprt-msgnr = '502'.` `koprt-msgv1 = t683s-kobed.`
  - Hauptschritt, Include `LV61BF0X`, Zeile 51 bis 65: `KON_BED_FRM-IND = KOMT1-KOBED.`
    `PERFORM (KON_BED_FRM) IN PROGRAM SAPLV61B IF FOUND.` … `KOPRT-MSGNR = '502'.` `KOPRT-MSGV1 = KOMT1-KOBED.`
- Andere Meldungen mit Nummer 502 (V1, VK, V2, VN, VV) haben einen anderen Wortlaut; VN 502 heisst „Einteilung & zu Position &
  in Beleg & nicht vorhanden“ und wird hier nicht verwendet.

Die Analyse sagt also: „ZBA1 ignoriert, Bedingung 902 nicht erfuellt“. Welche Abfrage innerhalb der 902 abgelehnt hat, zeigt die
Analyse nicht.

## 3. Quelltext der Bedingung 902 (Include `RV61B902`, aktive Fassung in T76)

Kopf des Includes: Kopie der Standardbedingung 002, MEEY Harald Streit 2022-11-09 („Additional chek if sales document item is
incomplete“) und 2023-09-11 („Switch off Check for printing flag in delivery block“). Die entscheidenden Zeilen woertlich:

```abap
FORM kobed_902.
  sy-subrc = 0.
  IF komkbv1-uvals NE 'C'.            "added: completeness of item
    sy-subrc = 4.
    EXIT.
  ENDIF.
  IF komkbv1-prevent_issue_output = abap_true. "e.g. document in approval
    sy-subrc = 4.
    EXIT.
  ENDIF.
  IF komkbv1-uvall NE 'C'. "document incomplete
    sy-subrc = 4.
    EXIT.
  ENDIF.
  IF komkbv1-costa NE space AND komkbv1-costa NE 'C'. "not confirmed completely
    sy-subrc = 4.
    EXIT.
  ENDIF.
  IF NOT komkbv1-lifsk IS INITIAL.
*    (Pruefung TVLS-SPEDR auskommentiert, 2023-09-11 MEEY Harald Streit)
    sy-subrc = 4.
    EXIT.
  ENDIF.
  IF komkbv1-cmgst CA 'BC'. "credit block
    sy-subrc = 4.
    EXIT.
  ENDIF.
  IF komkbv1-retro_purchdoc_creation IS NOT INITIAL.
    IF komkbv1-bstkd IS INITIAL.
      sy-subrc = 4.
      EXIT.
    ENDIF.
  ENDIF.
ENDFORM.
```

`FORM kobev_902` (Vorstep) hat dieselben Abfragen mit zwei Unterschieden: die Abfrage auf `UVALS` fehlt, und die Liefersperre
lehnt nur ab, wenn `TVLS-SPEDR` gesetzt ist (`SELECT SINGLE * FROM tvls WHERE lifsp = komkbv1-lifsk` … `IF sy-subrc = 0 AND
tvls-spedr NE space`). Aufgerufene Z-Funktionsbausteine oder Z-Tabellen gibt es in beiden Formen nicht; der Planauftrag (PLAF)
kommt nicht vor.

Woher die Felder kommen: `KOMKBV1_FILL` (`LVCOMU01`) uebernimmt mit `move-corresponding com_vbuk to com_vbak-vbak_status.` und
`move-corresponding com_vbak to com_kbv1.` die Statusfelder des Kopfs; `prevent_issue_output` kommt aus
`cl_sd_sls_comp_handler=>so_instance->get_output_control`. Der Kunden-Exit `USEREXIT_KOMKBV1_FILL` (`RVCOMFZZ`) haengt nur
den Partner MP mit dem Benutzernamen an und aendert keines der geprueften Felder.

## 4. Auswertung je Abfrage gegen 411403 / Position 10

Beleg: VBAK `0000411403` (TA, Verkaufsorganisation 1200, angelegt 18.11.2026 15:28:19 von FPA), VBAP Position 000010 (Material
52704, Werk 1200, Positionstyp TAN, 60 ST, Bedarfsart KEV, Sonderbestand E), VBKD, VBUV; gelesen 18.11.2026 17:57 bis 18:10.

| Nr. | Abfrage in `KOBED_902` | Wert heute (nach 16:53:30) | heute | Stand bis 16:53:30 |
| --- | --- | --- | --- | --- |
| 1 | `UVALS NE 'C'` (Unvollstaendigkeitsstatus alle Positionen) | VBAK-UVALS = C; VBAP-UVALL = C; VBUV ohne Eintrag | erfuellt | **nicht erfuellt (hergeleitet, Abschnitt 5)** |
| 2 | `PREVENT_ISSUE_OUTPUT = abap_true` | Feld ist nicht gespeichert; VBAK-APM_APPROVAL_STATUS leer | erfuellt (ZBA1 wurde gefunden) | nicht gelesen |
| 3 | `UVALL NE 'C'` (Unvollstaendigkeitsstatus Kopf) | VBAK-UVALL = C | erfuellt | erfuellt: BA00 (Bedingung 2) entstand 15:28:54 |
| 4 | `COSTA NE space AND COSTA NE 'C'` (Bestaetigungsstatus fuer Einkauf) | VBAK-COSTA leer | erfuellt | kein Hinweis auf Aenderung |
| 5 | `NOT LIFSK IS INITIAL` (Liefersperre Kopf) | VBAK-LIFSK leer | erfuellt | kein Aenderungsbeleg auf LIFSK |
| 6 | `CMGST CA 'BC'` (Kreditstatus) | VBAK-CMGST leer | erfuellt | kein Hinweis auf Aenderung |
| 7 | `RETRO_PURCHDOC_CREATION` gesetzt und `BSTKD` leer | VBKD-BSTKD = „Testauftrag 18.11.2026 3_Termin“ | erfuellt | erfuellt |

Zur Spalte „Stand bis 16:53:30“: Zum Auftrag gibt es genau einen Aenderungsbeleg (CDHDR 0021051663, FPA, 18.11.2026 16:53:30,
Transaktion V_V2) mit genau einer Position (CDPOS: Tabelle VBEP, Schluessel Position 000010 Einteilung 0002, `KEY`, Kennzeichen
I = eingefuegt). Kopffelder wie LIFSK haben sich laut Aenderungsbelegen nie geaendert.

## 5. Antwort 1: Welche Voraussetzung fehlte?

**Die bestaetigte Menge der Position 10.** Kette, jedes Glied mit Beleg:

1. Positionstyp TAN hat das Unvollstaendigkeitsschema **Z3** „KD Einzel best. Meng“ (TVAP `PSTYV TAN`, `FEHGR Z3`; TVUVT).
2. Schema Z3 enthaelt das Feld **`VBAP-KBMENG`** („Kumulierte bestätigte Menge in Verkaufsmengeneinheit“) mit Statusgruppe 01
   (TVUVF `FEHGR Z3, TBNAM VBAP, FDNAM KBMENG, STATG 01`). Statusgruppe 01 setzt nur den allgemeinen Status (TVUVS `STATG 01`:
   `UVALL X`, alle anderen leer).
3. Eine Position mit `KBMENG` 0 ist damit allgemein unvollstaendig; der Kopfstatus „alle Positionen“ (`VBAK-UVALS`, Datenelement
   `UVALL_SU`) ist dann nicht C, und die erste Abfrage `IF komkbv1-uvals NE 'C'` lehnt ab. BA00 ist davon nicht betroffen:
   Bedingung 2 im Schema, und `kobed_002` (Include `LV61B002`) hat dieselben Abfragen wie 902, aber keine auf `UVALS`.
4. Bei 411403/10 war bis 16:53:30 nichts bestaetigt: Einteilung 0001 (Wunsch 26.11.2026, 60 ST) traegt `BMENG` 0 und hat keinen
   Aenderungsbeleg; die Einteilung 0002 (04.12.2026, `BMENG` 60 ST) wurde erst 16:53:30 eingefuegt. Dass Aenderungen an `BMENG`
   in diesem System Aenderungsbelege schreiben, zeigen 411400 und 411401 (CDPOS `VBEP-BMENG`, Kennzeichen U, 14:34:36/37).
5. Seit 16:53:30: VBAP-KBMENG = 60 ST von 60 ST, kein VBUV-Eintrag, VBAP-UVALL = C, VBAK-UVALS = C, ZBA1 vorhanden.

**Hergeleitet, nicht gelesen:** Der Wert von `UVALS` und `KBMENG` vor 16:53:30 steht in keinem Aenderungsbeleg. Die Herleitung
stuetzt sich auf Glied 4 und auf zwei Vergleiche im selben System:

Vergleich „vorher“, Auftrag 411361 (FPA, 09.11.2026, TA, Verkaufsorganisation 1200, keine Liefersperre): VBAP-KBMENG = 0 von
100 ST, VBUV-Eintrag `VBAP-KBMENG`, Schema Z3, Statusgruppe 01; VBAP-UVALL = A, VBAK-UVALL = C, **VBAK-UVALS = A**; NAST enthaelt
nur BA00 (09.11.2026 15:44:07), **kein ZBA1**. Im Nummernbereich 411300 bis 411999 stehen 27 VBUV-Eintraege auf `VBAP-KBMENG`
(26 mit Schema Z3, einer mit Schema 24).

Vergleich „nachher“, alle fuenf Testauftraege von FPA vom 18.11.2026: ZBA1 entstand jedes Mal in der Sekunde des
Aenderungsbelegs, der eine bestaetigte Menge brachte.

| Auftrag | BA00 (Anlage) | Aenderungsbeleg mit Bestaetigung | ZBA1 angelegt | ZBA1 verarbeitet |
| --- | --- | --- | --- | --- |
| 411399 | 10:56:15 | V_V2 14:34:36 (VBEP 0002 eingefuegt) | 14:34:36 | 15:07:14 |
| 411400 | 11:08:59 | VA02 11:13:08 (VBEP 0001 Termine, 0002 eingefuegt) | 11:13:08 | nicht verarbeitet (Zeitpunkt 2, Status 0) |
| 411401 | 14:10:41 | V_V2 14:34:37 (VBEP 0001 `BMENG` geaendert) | 14:34:37 | 14:43:00 |
| 411402 | 14:49:40 | V_V2 15:07:14 (VBEP 0002 eingefuegt) | 15:07:14 | 15:07:15 |
| **411403** | 15:28:54 | **V_V2 16:53:30 (VBEP 0002 eingefuegt)** | **16:53:30** | 16:53:31 |

Quellen: NAST (`KAPPL V1`, `OBJKY` 0000411399 bis 0000411403), CDHDR/CDPOS (`OBJECTCLAS VERKBELEG`), VBEP.

Am deutlichsten ist 411401: Zwei Sicherungen in VA02 um 14:11:55 (Wunschlieferdatum, Fakturadatum) und 14:12:45 (Termine der
Einteilung 0001) brachten **kein** ZBA1; erst der Aenderungsbeleg 0021051619 von 14:34:37 mit `VBEP-BMENG` **alt 0.000, neu
60.000** brachte es in derselben Sekunde. Bei 411400 ebenso `BMENG` alt 0.000, neu 60.000 (0021051618).

Einschraenkung zur Spalte „verarbeitet“: Bei 411399 und 411401 wurde ZBA1 mit dem damaligen Zeitpunkt 2 angelegt (der
Konditionssatz wurde erst 14:42:26 auf 4 gestellt) und steht heute mit Zeitpunkt 4 ohne Versanddatum als verarbeitet da
(14:43:00 bzw. 15:07:14, je mit Ablagebeleg `ARCHIVELINK_CD`), **ohne** dass es zu diesen Zeiten einen Aenderungsbeleg am
Auftrag gibt; 15:07:14 ist die Sekunde des V_V2-Laufs, der 411402 geaendert hat. Die Nachricht eines Auftrags kann also neu
bestimmt oder verarbeitet werden, ohne dass ein Aenderungsbeleg entsteht. Auf welchem Weg das geschah (erneutes Sichern durch
V_V2, Sichern im Auftrag ohne Feldaenderung, Aenderung im Nachrichtenbild), ist nicht belegt. Bei 411400 blieb ZBA1 dagegen
auf Zeitpunkt 2 mit Versanddatum 19.11.2026, 04:00 bis 24:00 Uhr, unverarbeitet.

Zwei Folgerungen aus dem Customizing (nicht getestet): Fuer die Bedingung genuegt eine bestaetigte Menge groesser 0, eine
Vollbestaetigung verlangt 902 nicht (`KBMENG` ist nur als Pflichtfeld hinterlegt). Und jede Liefersperre im Kopf verhindert
ZBA1, unabhaengig davon, ob die Sperre den Druck sperrt (Aenderung MEEY vom 2023-09-11).

## 6. Zeitablauf am 18.11.2026 (alle Zeiten Ortszeit)

| Zeit | Vorgang | Beleg |
| --- | --- | --- |
| 14:42:26 | FPA aendert mit VV12 den ZBA1-Konditionssatz 2430 (Verkaufsorganisation 1200): Zeitpunkt **2 → 4** | CDHDR `COND_OC 0000002430BV1013ZBA1`, CDPOS `NACHAE-VSZTP` alt 2, neu 4 |
| 15:28:19 | Auftrag 411403 angelegt (FPA) | VBAK `ERDAT/ERZET` |
| 15:28:54 | BA00 angelegt, 15:28:57 verarbeitet | NAST |
| 15:33:18 | Bestellung 45148518 (NB, Lieferwerk 1100, Werk 1200, 60 ST, kontiert auf 411403/10) aus Banf 10340563 angelegt (ME21N) | CDHDR `EINKBELEG`, `BANF`; EKKO, EKPO, EKKN, EBAN |
| 15:41:51 | Planauftrag 2431940 zuletzt geaendert (FPA), Stand heute `AUFFX X` (fixiert), `STLFX` leer: Werk 1100, Material 52704, 60 ST, Start 18.11.2026, Ende 25.11.2026, Bezug 411403/000010. Dass diese Aenderung das Fixieren war, sagt Fabio; PLAF fuehrt nur den letzten Zeitstempel | PLAF; `PSTMP 20261118144151` ist UTC |
| 15:46:31 | V_V2 aendert die Bestellung 45148518, Einteilung 0002: Lieferdatum 12.03.2027 → 01.12.2026, Materialbereitstellung 10.03.2027 → 27.11.2026. **Kein Aenderungsbeleg am Kundenauftrag** | CDHDR 0021051658 (`EINKBELEG`, V_V2), CDPOS `EKET-EINDT/MBDAT/…`; EKET 0002 `MNG02` 60 ST |
| 16:53:30 | V_V2 aendert **den Kundenauftrag**: Einteilung 0002 eingefuegt (60 ST bestaetigt zum 04.12.2026) | CDHDR 0021051663, CDPOS `VBEP … 0002 KEY I`; VBEP |
| 16:53:30 | **ZBA1 angelegt** (Partner AG 306854, Medium 5, Zeitpunkt 4, Benutzer FPA, nicht manuell) | NAST `ERDAT/ERUHR` |
| 16:53:31 | ZBA1 verarbeitet (Status 1), Ablagebeleg zu VBAK 0000411403 | NAST `DATVR/UHRVR/VSTAT`; CDHDR `ARCHIVELINK_CD` |

Zeitstempel: `PLAF-PSTMP` und `VBAK-UPD_TMSTMP` sind UTC (VBAK `UPD_TMSTMP 20261118155330` gegen CDHDR 16:53:30, eine Stunde
Unterschied). 14:41:51 UTC ist 15:41:51 Ortszeit und passt zu Fabios „15:42“. Fuer den Planauftrag gibt es keinen
Aenderungsbeleg (CDHDR ohne Treffer zur Nummer 2431940 am 18.11.2026).

**Deutung (Vermutung):** Fabios Satz „auch nach V_V2 meldet die Findungsanalyse 502“ beschreibt den Stand zwischen 15:46 und
16:53: Der Lauf von 15:46:31 hat die Umlagerbestellung im Lieferwerk vorgezogen, an den Einteilungen des Kundenauftrags hat er
nichts geaendert (kein Aenderungsbeleg; `BMENG`-Aenderungen wuerden einen schreiben), die Position blieb unbestaetigt und damit
unvollstaendig. Ob der Auftrag in diesem Lauf trotzdem gesichert und die Findung durchlaufen wurde, ist **nicht belegt**; es
haette am Ergebnis nichts geaendert, weil die Abfrage 1 weiter abgelehnt haette. Ebenfalls **nicht belegt** ist, warum erst der
Lauf von 16:53:30 den Auftrag bestaetigt hat (bei 411402 lagen Bestellung 15:06:59 und Auftrag 15:07:14 nur 15 Sekunden
auseinander).

## 7. Antwort 2: Wodurch wird ZBA1 erneut geprueft, und wodurch nicht?

**Belegt: Die Nachrichtenfindung des Kundenauftrags laeuft im Sichern des Kundenauftrags.**

- `MV45AF0B_BELEG_SICHERN` (Form `BELEG_SICHERN`), Zeile 724: `perform trans_finalize changing lv_rejected.`
- `MV45AF0T_TRANS_FINALIZE`, Zeile 29 und 119: `perform beleg_bearbeiten_ende using lv_subrc.` und
  `perform final_determ_and_transfer using lv_subrc.`
- `MV45AF0B_BELEG_BEARBEITEN_ENDE`, Form `final_determ_and_transfer`: `call function 'COMMUNICATION_AREA_KOMKBV1'` und
  `call function 'MESSAGING'` mit `applikation = tvak-kappl`, `schema = tvak-kalsm` (fuer TA also V1 / V10000).
- Derselbe Baustein wird in SAPMV45A sonst nur aus den Nachrichtenbildern des Auftrags gerufen (`MV45AF0F_FCODE_KDOK`,
  `…_PDOK`, `MV45AF0N_NACHRICHTEN_POSITION`, `MV45AF0F_FCODE_LIST_CURRENT`; Verwendungsnachweis CROSS, `NAME MESSAGING`).

**Belegt im Quelltext: Gilt der Auftrag intern als unveraendert, wird nicht gesichert und nicht neu gefunden.**
`MV45AF0B_BELEG_SICHERN`, Zeile 323 bis 386: In der Neuterminierung (`call_activity = activity_atp_area`) gilt der Beleg als
unveraendert, wenn keine Einteilung ein Aenderungskennzeichen traegt (`loop at xvbep where updkz cn ' '.` …
`r185d-dataloss = abap_false.`); dann folgt `if r185d-dataloss = abap_false and force_update = abap_false and upd_vbuk is
initial and upd_vbup is initial …` mit `message s041.` („Keine Datenänderung“). `trans_finalize` steht im Sonst-Zweig dieser
Abfrage (ausgezaehlt: `if` Zeile 353, `else` Zeile 439, `if …is_save_phase_relevant( …finalize )` Zeile 443, `trans_finalize`
Zeile 724, zugehoeriges `endif` Zeile 787).

**Grenze dieser Aussage:** „Unveraendert“ haengt an internen Kennzeichen (Einteilungen, `force_update`, Statusfortschreibung
`upd_vbuk`/`upd_vbup`), nicht am Aenderungsbeleg. Ein fehlender Aenderungsbeleg beweist deshalb nicht, dass ein V_V2-Lauf den
Auftrag nicht gesichert hat (Gegenbeispiel 411399 um 15:07:14, Abschnitt 5). Umgekehrt gilt: Ein Lauf, der nichts aendert und
nichts erzwingt, endet bei „Keine Datenänderung“ vor der Findung.

**Belegt: V_V2 sichert ueber denselben Weg.** Transaktion V_V2 ist der Report `SDV03V02` (TSTC); er ruft
`SD_BACKORDER_CHECK_AND_SAVE` (`LV03RU03`), dieser `PERFORM beleg_sichern` (`LV03RF0B`, Zeile 1310 ff.) mit
`CALL FUNCTION 'SD_SALES_DOCUMENT_SAVE'`. Derselbe Baustein behandelt auch Umlagerbestellungen (`WHEN c_pur_order OR
c_pur_sched_agree`, `purdoc_update`), das erklaert die Aenderung der Bestellung um 15:46:31.

**Belegt: Das Fixieren des Planauftrags ruft die Nachrichtenfindung V1 nicht.** Der Verwendungsnachweis von `MESSAGING` zeigt
in der Bedarfsplanung nur `LM61UF3T` (Form `MAIL_SENDEN_DIREKTFERT`, Applikation P1, Schema P10001) und `LM61UF8G` (Form
`MAIL_SENDEN`, Applikation MD, Schema MD0001), beide ohne Bezug zum Kundenauftrag. Includes der Planauftragstransaktion
`SAPMM61P` (MD12, TSTC) erscheinen im Nachweis nicht. Der Nachweis ist statisch (CROSS); klassische BAdI-Implementierungen
zu `MD_PLDORD*` gibt es nicht (SXC_EXIT ohne Treffer), Erweiterungsspots der neuen Art sind nicht gelesen.

**Was ZBA1 nach der Fixierung tatsaechlich ausloest (Kette, Schritte 1 bis 3 belegt am Zeitablauf):**

1. Planauftrag fixieren aendert nur PLAF.
2. Ein V_V2-Lauf zieht die Umlagerbestellung im Lieferwerk 1100 vor (15:46:31).
3. Ein V_V2-Lauf, der die Position im Werk 1200 bestaetigt, aendert den Kundenauftrag und sichert ihn (16:53:30); im Sichern
   laeuft die Findung, die Position ist jetzt vollstaendig, 902 ist erfuellt, ZBA1 entsteht.
4. Gleichwertig ist jedes Sichern in VA02, das die bestaetigte Menge bringt (Beispiel 411400, 11:13:08).

**Automatik in T76:** Der Job `ZSD_NEUTERMIN_AT` laeuft taeglich 06:00 mit `SDV03V02`, Variante „AT TEST“, Benutzer TR_ORDERS,
eingeplant von MEEY_HAST (TBTCO/TBTCP: 18.11.2026 06:00:47 beendet, 19.11.2026 06:00 eingeplant). Die Variante enthaelt
(gelesen mit `RS_VARIANT_CONTENTS_RFC`) Werk 1200, **Material 39101 und 63502**, Verkaufsbelege ja (`P_SDDOCS X`),
Einkaufsbelege nein (`P_PURDOC` leer; im Report steuert `IF p_purdoc = 'X'` die Selektion der Einkaufsbelege), keine
Simulation (`P_SIMUL` leer). Das Testmaterial 52704 und die Umlagerbestellungen sind nicht enthalten; fuer 411403 haette der
Job also nichts ausgeloest. Andere Jobs mit `SDV03V02`,
`RSNAST00` oder einem Z-Programm mit Nachrichtenbezug gibt es in T76 seit dem 17.11.2026 nicht (TBTCP; daneben nur
`ZC_PRINT_LK04`).

**Versandzeit:** `Z_RSFNDTIM_ZBA1`, Form `OUTPUT_TIME` (MEEY Harald Streit 2026-11-05, „Zeitverzögerte Verarbeitung Nachricht
ZBA1 für Trafag AT“, T76K912847 freigegeben): fuer ZBA1 mit Medium 5 wird beim ersten Finden der naechste Arbeitstag laut
Fabrikkalender CH mit 04:00 bis 24:00 Uhr gesetzt, bei erneutem Finden bleibt das urspruengliche Datum. Der Kommentar nennt
einen Job `ZSD_NEUTERMIN_LIEFERVORRAT`; den gibt es in T76 seit 01.10.2026 nicht (TBTCO). Die Routine bestimmt nur den
Versandzeitpunkt, sie stoesst keine Findung an. Ihre Wirkung zeigt 411400 (ZBA1 mit Zeitpunkt 2, Versanddatum 19.11.2026,
04:00 bis 24:00 Uhr). Bei 411403 steht im NAST-Satz Zeitpunkt 4 ohne Versanddatum, weil der Konditionssatz seit 14:42:26 auf 4
steht.

**Z-Logik, die ZBA1 nachzieht: in den gelesenen Objekten keine.** Im Verwendungsnachweis von `MESSAGING` (CROSS, 83 Includes)
steht kein Include aus dem Kundennamensraum (kein `Z…`, `LZ…`, `ZCL…`). Gelesen: `MV45AFZZ` (`USEREXIT_SAVE_DOCUMENT` und
`USEREXIT_SAVE_DOCUMENT_PREPARE` enthalten nur auskommentierten Code), `MV45AFZB`, `MV45AFZ4`, `RVCOMFZZ`, `RVCOMFZ1`,
`RVCOMFZ4` (keine Treffer zu ZBA1, PLAF, KBMENG, UVALS, MESSAGING), `ZSD_CREATE_ZE02`, `ZTRIM_SEND_MAIL_DISPO` (kein Bezug zu
ZBA1). Einen Report `ZSD_MISSING_ZBA1` gab es (Auftraege T76K912672 bis T76K912709, Texte „ZSD_MISSING_ZBA1 V2“ bis „v19“);
er wurde mit T76K912722 („ZSD_MISSING_ZBA1 deletion“, CAT, Status R, 08.10.2026) geloescht und steht weder in TRDIR noch in
TADIR. Was er tat, ist nicht mehr lesbar.

## 8. Belegt, Vermutung, nicht geprueft

**Belegt (gelesen):** Schemazeile ZBA1 mit Bedingung 902; Wortlaut und Fundstellen der Meldung VE 502; Quelltext beider Formen
der 902; Unvollstaendigkeitsschema Z3 mit `VBAP-KBMENG`; heutiger Status von 411403/10; NAST-Satz ZBA1 16:53:30; Aenderungsbelege
zu Auftrag, Bestellung und Konditionssatz; Planauftrag 2431940 fixiert; Aufrufkette Sichern → Nachrichtenfindung; Ausstieg bei
intern unveraendertem Beleg (Quelltext); Job und Variante.

**Hergeleitet:** dass bis 16:53:30 genau die Abfrage 1 (`UVALS`) abgelehnt hat. Dafuer sprechen Glied 4 in Abschnitt 5, der
Vergleichsauftrag 411361, die fuenf Testauftraege und besonders 411401 (zwei Sicherungen ohne ZBA1, ZBA1 erst mit `BMENG` 0 →
60); dagegen spricht nichts Gelesenes.

**Vermutung:** dass Fabios „502 nach V_V2“ den Lauf von 15:46:31 meint; dass der Lauf von 16:53:30 den Auftrag bestaetigen
konnte, weil die Umlagerbestellung inzwischen zum 01.12.2026 bestaetigt war (Verfuegbarkeitspruefung 02/AE zaehlt Bestellungen,
siehe `PLAN_T441V_MIT_BESTELLUNGEN_2026-11-18.md`); dass eine Teilbestaetigung fuer 902 genuegt.

**Nicht geprueft:**

- Mandant 090 (per RFC nicht lesbar) und P76 (nicht Teil des Auftrags). T76 ist eine Datenkopie, die Aussagen gelten fuer T76.
- Die Findungsanalyse selbst (nur in der GUI sichtbar) und was `PREVENT_ISSUE_OUTPUT` vor 16:53:30 lieferte.
- Warum der V_V2-Lauf von 15:46:31 den Kundenauftrag nicht bestaetigt hat, ob er ihn gesichert hat und welche Selektion Fabio
  in seinen Laeufen benutzt hat; auf welchem Weg ZBA1 bei 411399 und 411401 ohne Aenderungsbeleg verarbeitet wurde.
- Werkzeuge: `table-read`, `abap-read` und ein `rfc-call` auf den lesenden Baustein `RS_VARIANT_CONTENTS_RFC` (Jobvariante).
- Ob `RV61B902` in T76 und P76 gleich ist. Das Include wurde am 05.11.2026 17:30:27 von MEEY_HAST zuletzt gesichert (TRDIR
  `UDAT`, VRSD Version 00000 mit Auftrag T76K912846); diesen Auftrag gibt es in E070 nicht mehr, der aktive Quelltext traegt
  keinen Vermerk von 2026. Ein Vergleich mit Version 00003 (2023-09-11, T76K909021) ist nicht gemacht.
- Weitere Z-Objekte mit Zugriff auf NAST (D010TAB): `SAPLZSD000`, `SAPLZ_SD_BASIC_BAPIS`, `ZCL_SD_ZE02`, `SAPLZEDI_IDOC_ORDERS`,
  `SAPLZV56C` und die Druckprogramme; ausserdem Erweiterungsimplementierungen an SAPMV45A (z. B. `beleg_sichern_*`).
- Ob T76 die verarbeiteten ZBA1-Nachrichten (Medium 5, Partner 306854) nach aussen zugestellt hat (SOST/SCOT nicht gelesen).

## 9. Nebenbefunde fuer Ingo

1. Der ZBA1-Konditionssatz fuer Verkaufsorganisation 1200 steht in T76/100 seit 18.11.2026 14:42:26 auf Zeitpunkt 4 (sofort),
   vorher 2. Stammdatenaenderung von FPA fuer den Test; ob sie zurueckgestellt werden soll, entscheidet Fabio.
2. In T76 gibt es keinen eingeplanten `RSNAST00`-Lauf; Nachrichten mit Zeitpunkt 1 oder 2 bleiben dort liegen (Beispiel ZBA1
   zu 411400, Status 0).
3. Die Jobvariante „AT TEST“ deckt nur zwei Materialien ab und keine Umlagerbestellungen. Fuer den Zielablauf (fixieren, dann
   automatisch „AB final“) braeuchte es einen Lauf, der zuerst die Bestellungen im Lieferwerk und danach die Kundenauftraege
   im Werk 1200 neu terminiert. Das ist eine Folgerung aus dem Zeitablauf, kein gelesener Entscheid.

## 10. Antwortentwurf an Fabio (Ingo sendet selbst)

Hallo Fabio

ZBA1 ist beim Testauftrag 411403 inzwischen gefunden worden: Dein V_V2-Lauf um 16:53:30 hat den Auftrag geändert, und in
derselben Sekunde ist „AB final“ entstanden und verarbeitet worden. Die 502 in der Findungsanalyse ist die Meldungsnummer von
„Nachricht ignoriert (Bedingung 902 nicht erfüllt)“, also keine zweite Bedingung. Die Bedingung 902 schaut den Planauftrag gar
nicht an; sie verlangt, dass der Kopf und alle Positionen vollständig sind und dass keine Liefersperre und keine Kreditsperre
gesetzt ist. Bei Position 10 fehlte nach den Belegen bis 16:53 die bestätigte Menge: Sie ist im Unvollständigkeitsschema der
Position ein Pflichtfeld, und solange sie null ist, gilt die Position als unvollständig und 902 lehnt ab. Das Fixieren des
Planauftrags 2431940 um 15:42 stösst im Kundenauftrag nichts an, ZBA1 wird nur neu geprüft, wenn der Kundenauftrag selbst
gesichert wird, also beim Ändern des Auftrags von Hand oder wenn V_V2 ihn sichert. Dein V_V2-Lauf um 15:46 hat die Bestellung
45148518 vorgezogen (vom 12.03.2027 auf den 01.12.2026), an den Einteilungen des Kundenauftrags aber nichts geändert; erst der
Lauf um 16:53 hat die 60 Stück zum 04.12.2026 bestätigt. Warum der Lauf um 15:46 den Auftrag noch nicht bestätigt hat, konnte
ich aus den Belegen nicht ablesen. Automatisch
würde das in T76 heute nicht passieren: Der tägliche Neuterminierungsjob um 06:00 läuft nur für Werk 1200 mit den Materialien
39101 und 63502 und ohne Bestellungen. Ein Hinweis noch: Der ZBA1-Konditionssatz für 1200 steht seit deiner Änderung um 14:42
auf sofortigem Versand statt auf Zeitpunkt 2. Gelesen habe ich nur T76, Mandant 100.

Gruss Ingo

## 11. Kandidaten fuer `docs/rag/LEARNINGS.md`

- Vor der Frage „warum fehlt eine Nachricht“ zuerst NAST und die Aenderungsbelege des Belegs mit Uhrzeit lesen: der Stand kann
  sich seit der Frage geaendert haben (hier ZBA1 um 16:53:30 entstanden, Auftrag an den Agenten 17:51).
- Eine Zahl aus der Findungsanalyse (hier 502) ist die Meldungsnummer der Klasse VE, nicht eine Bedingung; die
  Protokollnummern stehen als `koprt-msgnr` in `LV61BF0K`/`LV61BF0X` und erscheinen nicht im Verwendungsnachweis CROSS.
- `PLAF-PSTMP` und `VBAK-UPD_TMSTMP` sind UTC, CDHDR und NAST Ortszeit; beim Zeitablauf eine Stunde (Winterzeit) umrechnen.
- `table-read` liest nur den Anmeldemandanten; ein Vergleich 090 gegen 100 geht per RFC nicht (Briefing nannte ihn als
  moeglich; LEARNINGS 2026-11-18 sagt es schon fuer den Filter).
