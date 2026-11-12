# Plan Customizing „IC Variante V1“ in T76/100 (Phase 1 Plan, Phase 2 ausgefuehrt am 2026-11-11)

Stand: 2026-11-12 (Doku-Pruefung: Vermerk in Abschnitt 6.5 auf Fabios Tests, `NOTES.md` 9.1); davor 2026-11-11 vormittags, Agent sap-t76. Phase 1 von 2: Diese Datei ist der Ausfuehrungsplan fuer Phase 2.

*Nachtrag 2026-11-11 09:40: Phase 2 ist ausgefuehrt (zweiter Anlauf 09:20 bis 09:36, erster Anlauf 08:58 mangels
GUI-Sitzung nach Schritt 0 angehalten). Customizing-Auftrag **T76K912884** (Aufgabe T76K912885), offen, nicht freigegeben.
Einzelheiten Abschnitt 6. Der folgende Satz beschreibt nur Phase 1:*
In Phase 1 wurde in SAP **nichts** geaendert, kein Auftrag angelegt, nichts gesichert, keine GUI-Sitzung geoeffnet. P76 nicht
gelesen und nicht beruehrt.

Zugang: ausschliesslich RFC `.tmp_sap_probe/RunSapProbe.ps1` (DPAPI-Ablage T76, `Test-SapPassword`: vorhanden seit
10.09.2026), nur `table-read` und `abap-read`. Vorher-Staende als Dateien in `saptasks/ic_preisfindung/v1_vorher_2026-11-11/`.

Grundlagen: `IC_Umsetzung_V1 1 (2).xlsx` (Blaetter 2, 3, 4, 8; inhaltlich gleich mit `IC_Umsetzung_V1.xlsx` fuer Blatt 1 bis 4),
`IC-Preisboard 1100_1200.pdf` (Fabio, v0.4, 07.11.2026, P76-Angaben), `NOTES.md` dieses Ordners (1.4, 1.5, 3, 8).

Alle Werte unten stammen aus T76 Mandant 100 (Datenkopie etwa Fruehjahr 2026 plus Testbelege). Fuer P76 sind sie Hinweise.

---

## 1. Ist-Stand T76 je Objekt (mit Belegen)

*Hinweis 2026-11-11 09:45: Dieser Abschnitt beschreibt den Stand **vor** Phase 2. Seit Phase 2 gibt es ZPR0 (T684), ZPR0/PR00
(T684G), T684S-Zeilen fuer ZICAA1, die Stufen 010/012/113/115 in ZICAA1 und `KOFRS 0000601` bei Z999/Z002; Stand nachher in
Abschnitt 6.4 und `v1_nachher_2026-11-11/`.*

### 1.1 Kalkulationsschema-Kopf (T683)

| Schema | AUSVF | BONSM | KNPRS_V | Text (T683U) |
| --- | --- | --- | --- | --- |
| ZICAA1 | leer | X | leer | Trafag Interne Verrechnung |
| ZVAA01 | leer | leer | leer | Trafag |

**Frage „muss der Ausschluss im Schemakopf aktiviert werden?“ beantwortet: nein.** T683 hat kein Aktivierungskennzeichen
fuer den Ausschluss (DD03L T683: KALSM, AUSVF, BONSM, KNPRS_V, PRODUCTIVE, INCLUSIONPOINT). `AUSVF` heisst „Ausschlussverfahren“
mit den Festwerten leer = „Positionsausschluss“, A = „Kopfausschluss“ (DD07T Domaene AUSVF). Quelltext `LV61AA28` FORM
`KONDITIONSAUSSCHLUSS` Zeilen 19/20: `check: p_kopf = yes and t683-ausvf = 'A' or p_kopf = no and t683-ausvf = ' '.`
Danach liest die Form direkt `T684S` fuer das Schema (Zeilen 28 bis 31). (Zeilennummern hier und unten: Quelltextzeilen
laut `abap-read` T76, ohne die 7 Kopfzeilen der SapProbe-Ausgabe.) Mit `AUSVF` leer (ZICAA1 wie ZVAA01) laeuft der
Positionsausschluss, sobald `T684S`-Zeilen fuer ZICAA1 da sind. ZVAA01 hat ebenfalls `AUSVF` leer und einen wirksamen
Ausschluss (Beleg 1.6).

### 1.2 Steuerdaten ZICAA1 und ZVAA01 (T683S, KVEWE A, KAPPL V)

Volle Listen: `v1_vorher_2026-11-11/T683S_ZICAA1.csv` (54 Datenzeilen plus Kopfzeile), `T683S_ZVAA01.csv` (47 plus Kopfzeile). Spalten-Zuordnung
V/08 zu T683S (DD04T): Stufe `STUNR`, Zaehler `ZAEHK`, KArt `KSCHL`, Von `STUNB` („Von-Bezugsstufe“ = FKSTU), Bis `STUN2`
(= TOSTU), Manuell `KAUTO` („nicht automatisch“), Obligat. `KOBLI`, Statistik `KSTAT`, Druck `DRUKZ`, Zwischensumme `KZWIW`,
Bedingung `KOBED`, Rechenformel `KOFRM`, Basisformel `KOFRA`, Kontoschluessel `KVSL1`, Rueckstellung `KVSL2`.

ZICAA1, Ausschnitt bis Stufe 300 (vollstaendig in der CSV):

| STUNR | ZAEHK | KSCHL | STUNB | STUN2 | KAUTO | KOBED | KZWIW | KSTAT | KOFRM | KOFRA | KVSL1 | DRUKZ | Text (T683T) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 008 | 000 | EK01 | 000 | 000 | X | 0 | | | 0 | 0 | ERL | | |
| 011 | 000 | PR00 | 000 | 000 | | 2 | | | 0 | 0 | ERL | | |
| 013 | 000 | PB00 | 000 | 000 | X | 2 | | | 0 | 0 | ERL | | |
| 020 | 000 | VA00 | 000 | 000 | | 2 | | | 0 | 0 | ERL | X | |
| 100 | 000 | – | 000 | 000 | | 0 | 9 | | 2 | 0 | | X | **Brutto** |
| 101 bis 109 | 000 | KA00, K032, K005, K007, K004, K020, K029, K030, K031 | 000 | 000 | | 2 | | | 0 | 0 | ERS | X | |
| 110 | 001 bis 005 | RA01 (STUNB 100), RA00, RC00, RB00, RD00 | | | X | 2 | | | 0 | 0 | ERS | X | |
| 111 | 000 | HI01 | 000 | 000 | | 2 | | | 0 | 0 | ERS | X | |
| 112 | 000 | HI02 | 000 | 000 | | 2 | | | 0 | 0 | ERS | X | |
| 120 | 000 | VA01 | 000 | 000 | | 2 | | | 0 | 0 | ERS | X | |
| 300 | 000 | – | 101 | 299 | | 0 | | | 0 | 0 | | | Rabattbetrag |

Weiter: 302 NETP, 310 PN00, 320 PMIN, 400 Bonusbasis, 800 Positionsnetto, 805 KP00..KP03, 810 HA00/HB00/HD00, 815 KF00,
820 HM00, 900 Nettowert 2, **909 IV02 und 910 IV01 (KOBED 22)**, 912 PC00, 914 SKTV, 915 MWST, 919 DIFF, 920 Endbetrag,
930 SKTO, 932 RL00, 933 MW15, 935 GRWR, 940 VPRS, 941 EK02, 950 Deckungsbeitrag, 970/971 EDI1/EDI2.

**Freie Stufen:** 010 und 012 (zwischen 008 EK01, 011 PR00, 013 PB00) sind frei. Im 1xx-Bereich sind 113 bis 119 frei
(`T683S` ZICAA1 `STUNR BETWEEN '113' AND '119'`: keine Zeile), ebenso 121 bis 299.

ZVAA01, Vorlagezeilen (T683S):

| STUNR | KSCHL | STUNB | STUN2 | KAUTO | KOBED | KSTAT | KOFRM | KOFRA | KVSL1 | DRUKZ |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 010 | Z999 | 000 | 000 | | 2 | | 0 | 0 | ERL | |
| 011 | Z000 | 000 | 000 | | 2 | | 0 | 0 | ERL | |
| 100 | – (Bruttowert 1, KZWIW 1, KOFRM 2) | | | | 0 | | 2 | 0 | | X |
| 105 | Z004 | 000 | 000 | | 2 | | 0 | 0 | Z04 | X |
| 106 | – (Bruttowert 2, KOFRM 2) | | | | 0 | | 2 | 0 | | |
| 110 | Z001 | **000** | **000** | | 2 | | 0 | 0 | Z01 | X |
| 115 | Z002 | **000** | **000** | | 2 | | 0 | 0 | Z02 | X |
| 120 | Z003 | 000 | 000 | X | 2 | | 0 | 0 | ERS | X |

In ZVAA01 haben Z001 und Z002 **keine** Bezugsstufe. Rechenregel `KRECH A` (Prozent) ohne Bezugsstufe rechnet auf dem
laufenden Wert; belegt in 1.6 (Kaskade).

### 1.3 Ausschlussgruppen und Ausschluss je Schema

| Tabelle | Schluessel | Wert |
| --- | --- | --- |
| T684 (KVEWE A, KAPPL V) | Gruppen | Z001, Z999, ZR01, ZR02, ZVW1, ZVW2. **ZPR0 gibt es nicht.** |
| T684T (D) | Z001 / Z999 | „Bruttopreisfindung“ / „Nettopreisfindung“ (Textfeld `VTEXT`, Datenelement VTXTK, 20 Zeichen) |
| T684G | Z001 | HI01, HI02, Z000, Z001, Z002, Z003, Z004, Z050 |
| T684G | Z999 | Z999 |
| T684G | ZVW1 / ZVW2 | EK02 / VPRS |
| T684G | PR00 | **in keiner Gruppe** |
| T684S | ZVAA01 | 010 D Z999 → Z001; 015 D ZR01 → ZR02; 020 D ZVW1 → ZVW2 |
| T684S | ZICAA1 | **keine Zeile** |

Pflegetransaktionen (TSTCP/TSTCT): OV31 = `VV_T684_VA` „Pflege Ausschlussgruppe“, OV32 = `VV_T684G_VA` „Pflege KondArten zur
Ausschlussgruppe“, **OV37 = `VVC_T683A_VA` „KondAusschluss: KalkulatSchema in SD“** (Ausschluss je Schema). OV33 ist etwas
anderes (`V_TVFK_KON`). Die bisherigen T684S-Eintraege fuer ZVAA01 stehen in E071K mit `MASTERNAME VVC_T683A_VA`
(T76K900174, T76K900897, T76K904146), also ueber OV37 gepflegt.

### 1.4 Konditionsarten (T685A, T685, T685T)

| KSCHL | Text | KRECH | KZBZG | KOAID | KMANU | KOFRS | Zugriffsfolge (T685-KOZGF) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PR00 | Preis | C | C | B | C | **0000601** | PR02 |
| PB00 | Preis Brutto | C | C | B | leer | 0 | – |
| Z999 | Nettopreis | C | C | B | C | 0 | Z999 |
| Z000 | Bruttopreis | C | C | B | C | 0 | Z000 |
| Z001 | Wiederverkaufsrabatt | A | B (Wertstaffel) | A | leer | 0 | Z001 |
| Z002 | Mengenrabatt | A | C (Mengenstaffel) | A | leer | 0 | Z001 |
| IV01 | PreisInterneVerrechn | C | C | B | C | 0 | PI01 (A055), `KFKIV X` |
| IV02 | PreisInterneVerrProz | A | – | B | C | 0 | PI02 (A056), `KFKIV X` |
| HI01 / HI02 | Hierarchie / Hierarchie/Material | A / C | B / C | A | C | 0 | HI01 / HI02 |

KOAID B = Preis (DD07T KOAID). Z999 und Z002 haben Mengenstaffel (KZBZG C), die Routine 601 (prueft `XKOMV-KZBZG = C`)
greift bei beiden.

### 1.5 Zugriffsfolgen (T682I, T682Z) und Felder in der IV-Faktura

| Folge | Stufe | Tabelle | Felder (Quelle) | exklusiv |
| --- | --- | --- | --- | --- |
| Z999 | 010 | A501 | VKORG, KUNNR (KOMK), PRODH1 (KOMP) | X |
| Z999 | 020 | A503 | VKORG, PLTYP (KOMK), MATNR (KOMP-PMATN) | leer |
| Z999 | 030 | A502 | VKORG, KUNNR, MATNR | leer |
| Z999 | 040 | A504 | VKORG, MATNR | leer |
| Z000 | 010/020/030/040 | A501 / A502 / A503 / A504 | wie oben | X/X/X/leer |
| Z001 (auch fuer Z002) | 000 | A527 | VKORG, KONDM, PRODH1 | X |
| | 005 | A528 | VKORG, KONDM | X |
| | 010 | A520 | VKORG, KUNNR, KONDM, PRODH1 | X |
| | 015 | A521 | VKORG, KUNNR, KONDM | X |
| | 020 | A522 | VKORG, KUNNR | X |
| | 025 | A523 | VKORG, PLTYP | X |
| | 030 | A524 | VKORG, HIENR (KOMK-HIENR01) | X |
| PR02 (PR00) | 010 / 020 / 030 / 040 | A305 (VKORG, VTWEG, KUNNR, MATNR) / A306 (PLTYP, WAERK) / A306 (WAERK aus KOMK-HWAER, KOBED 3) / A304 (VKORG, VTWEG, MATNR) | | X |

PR00 ausserhalb A305 (T76, 1100/01, gueltig nach 10.11.2026): A306 keine Saetze; **A304 6 Saetze** (u. a. 46999, 51967,
61406, 62195, C40227), keiner fuer die Testmaterialien. Ein A304-Materialpreis wird in der IV fuer **jeden** IC-Kunden gefunden
und schliesst nach V1 ueber ZPR0 Z999 und die Z000-Kette aus (heute gewinnt er ebenfalls, weil PR00 der einzige Preis ist).

Felder in der IV-Faktura (Ergaenzung Ingo, Punkt 2):

- `KOMK-PLTYP` = 14: belegt (NOTES 2.2, `LV60AA95` Zeile 133 `vbrk-pltyp = kuagv-pltyp`, `LV60AA58` Zeilen 137/138).
- `KOMK-KUNNR` = 820: belegt durch die Daten. PR00 wird in den IV-Fakturen ueber A305 (Schluessel KUNNR 820) gefunden, z. B.
  0090381200 PR00 57.77 EUR (NOTES 1.2); dazu `VBRK-KUNAG` 820.
- `KOMP-PRODH1`: wird gefuellt. `LV60AA58` (FORM `PREISFINDUNG_VORBEREITEN`) Zeilen 558/559: `MOVE xvbrp-prodh TO prodh.
  MOVE-CORRESPONDING prodh TO tkomp.` Struktur `PRODHS` hat PRODH1 (5 Zeichen), PRODH2, PRODH3 (DD03L). `VBRP-PRODH` ist in
  der IV gefuellt (0090381200: 8298, 0090381211: 8474). *Dass die lokale Variable `prodh` den Typ PRODHS hat, ist aus
  dem `MOVE-CORRESPONDING` geschlossen und nicht in `LV60ATOP` nachgelesen (Vermutung, sehr wahrscheinlich).*
- `KOMP-KONDM`: in der IV leer, weil `VBRP-KONDM` leer ist (0090381200, 0090381211) und `MVKE-KONDM` der Testmaterialien
  1100/01 leer ist (46690, 2585, 45366, 33148, 2367, 54150). A527/A528/A520/A521 treffen deshalb fuer diese Materialien nicht.
- `KOMK-HIENR01`: nicht nachgelesen (Vermutung: 820 ist Hierarchieknoten, `KNVH` HITYP A 1100/01/01 ohne Oberknoten). Fuer 820
  ohne Bedeutung, weil A522 (Stufe 020) vor A524 (Stufe 030) exklusiv trifft.

### 1.6 Wirkung Ausschluss und „spaetere Preiskondition“ (Quelltext und Beleg)

- `LV61AA55` (FORM `XKOMV_BEWERTEN`) Zeilen 2267 bis 2279: alle Zeilen vor dem letzten aktiven Preis (`letzter_preis`,
  KOAID B, nicht statistisch) bekommen `KINAK = 'Y'` („Wegen nachfolgendem Preis inaktiv“, DD07T KINAK).
- `LV61AA28` Zeile 79: in den Ausschluss gehen Zeilen mit `kinak ca ' ALWY'`, also auch die mit Y; Zeilen mit
  Wert 0 nur, wenn `ausschlusswertnull` gesetzt ist (Zeilen 80 bis 82).
- `LV61AA23` FORM `KOND_AUSSCHLUSS_GUENSTIGSTER_D`: ist Gruppe 1 aktiv (`active1` initial), wird Gruppe 2 ganz inaktiv
  (`LV61AA20` FORM `KOND_AUSSCHLUSS_GRUPPE_INAK`, `kinak = 'A'`). `LV61AA21` FORM `KOND_AUSSCHLUSS_GRUPPENWERT`: eine Gruppe,
  die schon ausgeschlossen ist (`xt684-kinak` nicht initial), gilt nicht mehr als aktiv. Die Regeln laufen in der
  Reihenfolge `KAULF` (`order by primary key`, `LV61AA28` Zeile 31).
- **Beleg ZVAA01** (T76, Auftrag 0000406808, TA, Kunde 301667, KNUMV 0000737525, Position 10): Z999 Stufe 010 180.50 CHF,
  KWERT 541.50, `KINAK` leer (aktiv); Z000 Stufe 011 1'454.00 CHF, KWERT 4'362.00, `KINAK A`; Z001 −37 %, KWERT −1'613.94,
  `KINAK A`; Z002 −10 %, KWERT −274.81, `KINAK A`; EK02 aktiv, VPRS `KINAK A` (ZVW1 → ZVW2). Der Ausschluss schlaegt also die
  Regel „spaeterer Preis gewinnt“: Z999 bleibt aktiv, obwohl Z000 danach kommt. Fabios Hinweis B ist damit bestaetigt.
- **Kaskade Z001/Z002 ohne Bezugsstufe** (gleicher Beleg): Z001 = 4'362.00 × −37 % = −1'613.94 (Basis = Z000-Wert);
  Z002 = (4'362.00 − 1'613.94) × −10 % = 2'748.06 × −10 % = −274.81. Z002 rechnet also auf dem Wert nach Z001.
- **Beleg ZICAA1, spaeterer Preis** (IV 0090381122 und 0090381123, Kunde 501230, MEEY_HAST, Juni 2026, KNUMV 0000747801 und
  747804): PB00 Stufe 013 100.00 bzw. 120.00 EUR `KINAK Y`, IV01 Stufe 910 66.00 EUR aktiv. Ein gefundener IV01/IV02 schlaegt
  heute jeden frueheren Preis in ZICAA1.

### 1.7 Kopiersteuerung Lieferung → IV (TVCPF, VTFL)

`TVCPF` FKARN IV, LFARV NLCC: Kopf GRBED 014 / GRURP 001 / **KNPRS B**; PSTYV NLC 018 / 003 / **B**; NLCG, NLCU, NLCZ je
018 / 000 / **B**. `PFKUR` leer, `FKMGK` B, `PLMIN` +. Auch NCC2, NCC3, NCCR, NK, NL (alle Positionstypen) haben KNPRS B.
Abweichend: LFARV **LF** (Kopf 014, Positionen CH00 bis CHA1 015 / 001) hat **KNPRS G**, Retouren REN/REP/... (GRBED 006) D.
Texte DD07T KNPRS: B = „Neue Preisfindung durchfuehren“, G = „Preiselemente unveraendert uebernehmen, Steuern neu ermitteln“,
D = „Preiselemente unveraendert uebernehmen“.

Die NLCC-Zeile wurde zuletzt mit T76K912762/T76K912763 „IC AT: Einzelrechnung IV -> Kopiersteuerung“ (MEEY_HAST, 27.10.2026,
freigegeben, **aus Mandant 090**, `E070C`) transportiert, Schluessel `090IV      NLCC    NLC`.

### 1.8 Kontenfindung (VKOA, nur gelesen)

- `KNVV` 820 / 1100 / 01 / 01: **KTGRD 03** („Erloes v. Unternehmen“, TVKTT), PLTYP 14, KALKS 1, KURST leer, WAERS EUR.
- `T001` 1100: KTOPL 1000, Waehrung CHF.
- Konditionsart KOFI/KOFK (Schema KOFI00, T683S KVEWE C), Zugriffsfolge KOFI (T682I KVEWE C): 005 C017 (PSTYV), 010 C001
  (KTGRD/KTGRM), 020 C002 (KTGRD), 030 C003 (KTGRM), 040 C005, 050 C004.
- C017: keine Saetze. C001 1100/03: nur KVSL ERL (KTGRM 11 → 0000030013 „Warenerloese Halbfabrikate verb. Unternehmen“,
  Kontenplan 1000; sonst Fremd-Kontenplaene). Die Testmaterialien haben `MVKE-KTGRM` leer, also greift C002.
- **C002 KTOPL 1000 / VKORG 1100 / KTGRD 03** (KOFI und KOFK gleich):

| KVSL | Konto | Text (SKAT) | KTGRD 01 / 02 zum Vergleich |
| --- | --- | --- | --- |
| ERL | 0000030003 | Warenerloese verbundene Unternehmen | 30001 / 30002 |
| ERS | 0000030999 | Uebr. Erloesminderungen | 30999 / 30999 |
| **Z01** | **0000030100** | Wiederverkaufsrabatt | 30100 / 30100 |
| **Z02** | **0000030200** | Mengenrabatt | 30200 / 30200 |
| Z04 | 0000030300 | Waehrungsrabatt | – |
| ERF | 0000030050 | Verr. Fracht | – |

Z01 und Z02 laufen heute fuer verbundene Unternehmen auf **dieselben** Rabattkonten wie fuer Inland und Ausland. Es gibt
keine IC-eigenen Erloesschmaelerungskonten. Kontoschluessel Z01, Z02 existieren in T687 („Wiederverkaufsrabatt“,
„Mengenrabatt“, T687T).

### 1.9 Verwendung von ZICAA1 (T683V, TVFK) und vorhandene Belege

- `T683V`: KALVG I mit KALKS 1 → ZICAA1 fuer **1100/01/01, 1100/01/02 und 1200/01/01**.
- `TVFK` KALVG I: Fakturaarten **IG, IG2, IGA, IGS, IV, IVA, IVS**.
- `VBRK` IC-Fakturen ab 2025 in T76: 22 IV an 820 (ZICAA1), 10 IV an **501230** (ZICAA1, Juni 2026), 4 IV und 1 IVS an
  501230 mit ICAA01 (Juni 2026). `KNA1` 501230 „TRAFAG Gesellschaft m.b.H.-Inter Co“, Wolfurt; `KNVV` 501230 1100/01/01
  KTGRD 03, **PLTYP 14**. V1 wirkt also auch auf IV an 501230.
- In allen 32 ZICAA1-Fakturen vorkommende Konditionsarten (`PRCD_ELEMENTS`): PR00 30, PB00 2, IV01 3, KW00 5, VPRS 34, MWST 34,
  SKTO 34, GRWR 34, EK02 1. **Keine** der Stufen 101 bis 120 (KA00 … HI02, VA01) kommt vor.

### 1.10 Routine 601 und V/06 (Staffelbasis)

- `T685A` ist je Konditionsart (Schluessel MANDT/KAPPL/KSCHL, DD03L), nicht je Schema. Ein Eintrag `KOFRS 0000601` bei Z999
  und Z002 wirkt also in jedem Schema mit diesen Konditionsarten, auch in ZVAA01.
- Schutz: `staffelbasis_601.abap` Zeile 39 `CHECK komk-kalsm = lc_kalsm_ic.` (Konstante `ZICAA1`, Zeile 20) und Zeile 40
  `CHECK xkomv-kzbzg = lc_kzbzg_meng.` (C). In ZVAA01 endet die Routine in Zeile 39 und laesst `XKWERT` unveraendert; der
  Aufrufer setzt danach `xkomv-kstbs = xkwert` (NOTES 8.2, `LV61AA55` Aufruf), die Staffelbasis bleibt also die des Standards.
  `KOMK-KALSM` kommt in der Faktura aus `VBRK-KALSM` (NOTES 8.2). Ein isolierter Test fuer ZVAA01-Faktura mit Teillieferung
  fehlt weiterhin (NOTES 8.8); Fabios S5 (Auftrag 411364, „Z000 priorisiert ok“) zeigt nur „kein Schaden“ beim Auftrag.
- P76: Routine 601 ist importiert, aber noch nicht in `RV62ANNN` eingebunden (NOTES 8.9). Ein V/06-Eintrag bei Z999/Z002
  laeuft dort bis zur Einbindung wie bei PR00 ins Leere (`PERFORM ... IF FOUND`), ohne Abbruch.

### 1.11 Sperren und offene Auftraege (E071K, E071, E070)

- `E071K` mit `TABKEY LIKE '%ZICAA1%'` fuer T683/T683S/T683T/T683U/T684S: in Mandant 100 **kein Eintrag** (nur Mandant 090,
  T76K912167, freigegeben, Kopf und Texte ZICAA1).
- `E071K` fuer T684/T684G/T684T/T684S: nur SAP-Pakete und alte, freigegebene T76-Auftraege (T76K900174, T76K900435,
  T76K900897, T76K904146, T76K904335; alle `TRSTATUS R`).
- `E071K` fuer T685/T685A/T685T mit Z999, Z002, PR00: alte Auftraege (alle R) und T76K912856/T76K912857 (PR00, **R**).
- `E071` VDAT/CDAT auf `VVC_T683_VA`, `VV_T685A_VA`, `VV_T684_VA`, `VV_T684G_VA`, `VVC_T683A_VA`, `V_TVCPFL` seit T76K91….:
  nur T76K912166/167, T76K912762/763, T76K912856/857, alle `TRSTATUS R`.
- **Ergebnis: keine Sperre in einem offenen Auftrag.** Offene Customizing-Auftraege (`E070` W/D 2026) gibt es von LIP, RAN,
  LAA, CAT, TRIM_*, MEEY_*, NTT_NOFE, I201072 und KOI (T76K912750 „DEMO PS ... NICHT FREIGEBEN“, lokal ohne Ziel); keiner
  enthaelt die Schluessel oben.
- `T000`: Mandant 090 „Trafag AG 090“ Kategorie **C (Customizing)**, Mandant 100 **T (Test)**, beide `CCCORACTIV 1`.

---

## 2. Abweichungen zwischen Fabios Annahmen (P76) und T76

| Nr | Fabio (Excel/Board, P76) | T76 (Beleg) | Folge |
| --- | --- | --- | --- |
| A1 | ZICAA1 = PR00 (11), PB00 (13), VA00, K004..K032, RA..RD, HI01/02, IV01/IV02 (909/910), keine Z-Konditionen | gleich, dazu EK01 008, NETP/PN00/PMIN, KP00..KP03, HA00/HB00/HD00, KF00, HM00, PC00, SKTV, MWST, DIFF, SKTO, RL00, MW15, GRWR, VPRS 940, EK02 941, EDI1/2 (1.2) | keine; Plan fuegt nur ein |
| A2 | Z001/Z002 „Basis Bruttowert (Stufe 100)“ | ZVAA01 110/115 ohne Bezugsstufe, Kaskade belegt (1.6). Fabios T5: 479.15 × 0.56 × 0.75 = 201.24 CHF ist die Kaskade; mit Basis Stufe 100 fuer beide waeren es 479.15 × (1 − 0.44 − 0.25) = 148.54 CHF | Plan nimmt **STUNB/STUN2 000/000 wie ZVAA01**. Bruttowert ist in ZICAA1 Stufe 100 „Brutto“; weil zwischen 100 und 113 heute nie eine Kondition gefunden wird (1.9), ist der laufende Wert bei Z001 gleich dem Bruttowert |
| A3 | Z001 „104 → neu 1xx“ | Stufe 104 ist in ZICAA1 K007 (belegt); frei sind 113 bis 119 | Z001 = **113**, Z002 = **115** |
| A4 | Z001 820 −44 % ueber A522 (ab 29.09.2026) | A522 Z001 1100/820 gueltig ab 27.09.2010, KNUMH 0000156759, **0.00 %** (Ersteller KRÄ). A524 HIENR 820 hat −44 % (KNUMH 0000029005, Wertstaffel KONW ab 1.00 −44 %), kommt aber erst nach A522 (exklusiv). ZVAA01-Auftrag 404577 (820, 07.11.2025) zeigt Z001 0.00 | T4/T5 rechnen in T76 ohne −44 %, solange der P76-Satz (ab 29.09.2026) nicht in T76 steht |
| A5 | Z002 Preisliste 14 ab 2/5/10/25/50 ST −10/25/30/35/40 % | A523 Z002 1100/14 KNUMH 0000213888 ab 01.02.2015: KONM 2 / 5 / 10 / 25 / 50 ST = −10 / −25 / −30 / −35 / −40 % (KBETR in Promille gespeichert: 100.00- = −10 %) | gleich |
| A6 | Z999 Zugriff A501 → A503 → A502 → A504 | gleich (T682I). **Nur A501 ist exklusiv**; A503, A502 und A504 nicht. Findet Z999 in A503 und zusaetzlich in A502/A504 einen Satz, entstehen zwei Z999-Zeilen; die spaetere gewinnt als Preis (1.6). Fuer 820: A501 leer, A502 hat 23 Saetze (andere Materialien), A504 Z999 fuer die Testmaterialien leer | Hinweis, gilt heute schon in ZVAA01 |
| A7 | ZVAA01 Ausschluss 10 D Z999 → Z001; ZICAA1 ohne | gleich (1.3) | – |
| A8 | Kopiersteuerung Lieferung → IV „B oder G pruefen“, Entscheid 07.11. „neue Preisfindung“ | IV/NLCC Kopf und alle Positionen schon **B** (1.7) | **kein Aenderungsschritt** in T76; P76-Wert offen (5.1) |
| A9 | Kontoschluessel Z01/Z02 „auf IC-Konten“ | Z01 → 30100, Z02 → 30200 fuer KTGRD 01, 02 **und** 03 gleich; keine IC-Konten (1.8) | FI muss Konten nennen (5.3) |
| A10 | Ausschluss „im Schemakopf zu verifizieren“ | kein Kopfschalter noetig (1.1) | – |
| A11 | ZICAA1 nur fuer Kunde 820 gedacht | ZICAA1 gilt fuer alle KALVG-I-Fakturen in 1100/01/01, 1100/01/02, 1200/01/01; in T76 auch IV an 501230 (PLTYP 14) | Regressionstest R1 (Abschnitt 4) |
| A12 | Testmaterialien mit Werten aus P76 | 46690 hat PR00 820 (ab 09.11.2026), 45366 und 33148 haben **keine** PR00 820, 33148 hat keinen Infosatz (EINA), alle vier haben in 1100/0001 Bestand 0 (Abschnitt 4) | Tests brauchen Stammdaten/Bestand (5.2) |
| A13 | Fabio pflegt in P76 Mandant 100 | Fruehere IC-AT-Customizing-Auftraege (MEEY_HAST) wurden in T76 **Mandant 090** (Kategorie C) gepflegt, T76K912856 (KOI) in Mandant 100. ZICAA1 in 090 hat laut T76K912167 eine Stufe 911, in 100 nicht | Entscheid Ingo (5.1 Punkt 3), Risiko 5.4 Punkt 9 |

---

## 3. Ausfuehrungsplan Phase 2 (T76, Mandant 100)

### 3.0 Rahmen

- Ausfuehrung nur ausserhalb der T76-Wartung (11.11.2026 16 bis 18 Uhr) und nur mit ausdruecklicher Freigabe Ingos fuer
  genau diese Schreibschritte (LEARNINGS 2026-10-15: Auto-Modus lehnt SAP-Schreiben sonst ab; nach Ablehnung nicht variieren).
- Kein Passwort. GUI nur in eigener Sitzung: `cscript //nologo .tmp_sap_probe\SapGuiT76Hilfe.vbs neu` (aus PowerShell oder
  aus Bash mit `MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*'`), nur die dort gemeldete `ses[n]`-Id verwenden, vor jedem Schritt
  `SapGuiT76Hilfe.vbs info`. Am Ende `SapGuiT76Hilfe.vbs zu <ses>`. Ingos `ses[0]` nie benutzen.
- Element-Ids je Bild mit `SapGuiT76Hilfe.vbs dump <ses>` ablesen; die fachlichen Werte stehen unten fest, es gibt nichts zu
  entscheiden. Jedes unerwartete Fenster zuerst auslesen (`dump <ses> 1`), dann handeln.
- RFC-Rueckleseproben immer aus Bash: `powershell -NoProfile -ExecutionPolicy Bypass -File .tmp_sap_probe/RunSapProbe.ps1
  table-read <Tabelle> --where "..." --fields "A,B" --rowcount 100 --format csv --quiet`, Felder mit Komma und in
  Anfuehrungszeichen, nie `<>` im Filter.
- Bei einer E-Meldung oder Abweichung: Schritt abbrechen (F12, „Daten verwerfen“), nichts sichern, anhalten und berichten.

### 3.1 Schritt 0: Vorpruefung (nur lesen)

1. `Test-SapPassword` (SapCredential.ps1): „Vorhanden: True“.
2. Sperren erneut pruefen, erwartet jeweils **keine Zeile** in einem Auftrag mit `TRSTATUS` D/L:
   - `table-read E071K --where "TABKEY LIKE '100AV ZICAA1%' OR TABKEY LIKE '100DAV ZICAA1%'" --fields "TRKORR,OBJNAME,TABKEY" --rowcount 100`
   - `table-read E071K --where "OBJNAME IN ('T684','T684G','T684T') AND TABKEY LIKE '100AV ZPR0%'" --fields "TRKORR,OBJNAME,TABKEY" --rowcount 100`
   - `table-read E071K --where "OBJNAME IN ('T685','T685A','T685T') AND ( TABKEY LIKE '%Z999%' OR TABKEY LIKE '%Z002%' )" --fields "TRKORR,OBJNAME,TABKEY" --rowcount 100`
     (erwartet nur T76K900012, T76K900174, T76K900569, T76K904335, T76K904861, alle R)
   - Gefundene Auftraege mit `table-read E070 --where "TRKORR IN (...)" --fields "TRKORR,TRSTATUS"` auf R pruefen.
3. Vorher-Stand gegen `v1_vorher_2026-11-11/` vergleichen (T683S ZICAA1 54 Datenzeilen, ZVAA01 47, T684 6 Gruppen, T684G 11 Zeilen, T684S 3
   Zeilen nur ZVAA01, T685A Z999/Z002 `KOFRS 0000000`). Weicht etwas ab: anhalten.

### 3.2 Schritt 1: Ausschlussgruppe ZPR0 anlegen (OV31) und eigenen Auftrag anlegen

- Transaktion `/nOV31` (Sicht `VV_T684_VA`, KVEWE A, KAPPL V) in der eigenen Sitzung.
- „Neue Eintraege“: Gruppe `ZPR0`, Bezeichnung `Sonderpreis PR00` (16 Zeichen, Feld 20 Zeichen).
- Sichern (Strg+S). Im Dialog „Abfrage Customizing-Auftrag“: **nicht** den vorgeschlagenen Auftrag nehmen, sondern
  „Auftrag anlegen“ (F8), Kurzbeschreibung **`IC V1: ZICAA1 Z999/Z000/Z001/Z002 + Ausschluss ZPR0`** (51 Zeichen; der
  Vorschlagstext mit „Konditionsausschluss“ hat 61 Zeichen und ist zu lang, LEARNINGS 2026-10-29). Ziel P76 (vorbelegt)
  lassen. Neue Auftragsnummer notieren (im Folgenden `<AUF>`), sie steht danach im Feld des Dialogs; mit Weiter bestaetigen.
- Rueckleseprobe:
  - `table-read T684 --where "KVEWE = 'A' AND KAPPL = 'V' AND KAUGR = 'ZPR0'"` → 1 Zeile.
  - `table-read T684T --where "SPRAS = 'D' AND KVEWE = 'A' AND KAPPL = 'V' AND KAUGR = 'ZPR0'"` → VTEXT `Sonderpreis PR00`.
  - `table-read E070 --where "TRKORR = '<AUF>' OR STRKORR = '<AUF>'" --fields "TRKORR,TRFUNCTION,TRSTATUS,TARSYSTEM,AS4USER"` →
    Auftrag W, D, P76, KOI; Aufgabe Q, D. `E070C` CLIENT 100.
  - `table-read E071K --where "TRKORR = '<Aufgabe>'" --fields "OBJNAME,MASTERNAME,TABKEY"` → T684 / T684T mit `ZPR0`.

### 3.3 Schritt 2: PR00 der Gruppe ZPR0 zuordnen (OV32)

- `/nOV32` (Sicht `VV_T684G_VA`). „Neue Eintraege“: Gruppe `ZPR0`, Konditionsart `PR00`. Sichern; im Auftragsdialog
  ausdruecklich `<AUF>` setzen bzw. bestaetigen.
- Nichts an den Gruppen Z001, Z999, ZR01, ZR02, ZVW1, ZVW2 aendern.
- Rueckleseprobe: `table-read T684G --where "KVEWE = 'A' AND KAPPL = 'V'" --rowcount 100 --format csv` → 12 Zeilen: die 11 aus
  `v1_vorher_2026-11-11/T684G.csv` unveraendert plus `ZPR0,PR00`.

### 3.4 Schritt 3: ZICAA1 Steuerdaten erweitern (V/08)

- `/nV/08`, Schema `ZICAA1` markieren (Positionieren ueber `btnVIM_POSI_PUSH`, NOTES 8.7), im Dialogstruktur-Baum den
  zweiten Knoten oeffnen (View-Cluster `VVC_T683_VA`: Objekt 01 `VV_T683_VA` = Schemata, Objekt 02 `V_T683S` = Steuerdaten;
  `VCLSTRUC`; die Knotenbezeichnung am Bild ablesen). Vor dem Einfuegen pruefen, dass die
  Stufen 010, 012, 113, 115 leer sind (sonst anhalten).
- „Neue Eintraege“, vier Zeilen, **jede Spalte genau so** (leer = Feld leer lassen, 0 = leer bzw. 0):

| STUNR | ZAEHK | KSCHL | STUNB (Von) | STUN2 (Bis) | KAUTO (Manuell) | KOBLI (Oblig.) | KSTAT (Statist.) | DRUKZ (Druck) | KZWIW (ZwiSu) | KOBED (Bedingung) | KOFRM (RechFormel) | KOFRA (BasisFormel) | KVSL1 (Kontoschl.) | KVSL2 (Rueckst.) | Kontenfindungsrelevant | Zugriffsebene |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 010 | 000 | Z999 | 0 | 0 | leer | leer | leer | leer | leer | 2 | 0 | 0 | ERL | leer | leer | 0 |
| 012 | 000 | Z000 | 0 | 0 | leer | leer | leer | leer | leer | 2 | 0 | 0 | ERL | leer | leer | 0 |
| 113 | 000 | Z001 | 0 | 0 | leer | leer | leer | X | leer | 2 | 0 | 0 | Z01 | leer | leer | 0 |
| 115 | 000 | Z002 | 0 | 0 | leer | leer | leer | X | leer | 2 | 0 | 0 | Z02 | leer | leer | 0 |

  Herleitung: je Zeile die ZVAA01-Zeile (010 Z999, 011 Z000, 110 Z001, 115 Z002) mit allen Feldern uebernommen, nur die
  Stufennummer geaendert; DRUKZ wie ZVAA01 (Z999/Z000 leer, Z001/Z002 X). Keine Bezugsstufe bei Z001/Z002 (Abweichung A2).
  Keine Zeilen fuer Z003, Z004, Z050. Keine bestehende Zeile anfassen (PR00 011, PB00 013, IV01/IV02, VPRS bleiben).
- Sichern; Auftrag `<AUF>` bestaetigen. Meldungen der Statuszeile notieren.
- Rueckleseprobe:
  - `table-read T683S --where "KVEWE = 'A' AND KAPPL = 'V' AND KALSM = 'ZICAA1'" --fields "STUNR,ZAEHK,KSCHL,STUNB,STUN2,KAUTO,KOBED,KZWIW,KSTAT,KOFRM,KOFRA,KVSL1,KVSL2,DRUKZ,KOBLI,IS_ACCT_DETN_RELEVANT,ACCESS_LEVEL" --rowcount 300 --format csv`
    → **58 Datenzeilen** (vorher 54; die Datei vorher hat eine Kopfzeile und keine Zeile `Exitcode`, die neue Ausgabe vor dem
    `diff` genauso bereinigen: `grep -v '^Exitcode'`). Mit `diff` gegen `v1_vorher_2026-11-11/T683S_ZICAA1.csv` (Exitcode von `diff` selbst auswerten): genau
    die vier neuen Zeilen, Werte wie Tabelle oben (CSV-Form: `010,000,Z999,000,000,,0000002,,,0000000,0000000,ERL,,,,,0000`,
    `012,000,Z000,000,000,,0000002,,,0000000,0000000,ERL,,,,,0000`, `113,000,Z001,000,000,,0000002,,,0000000,0000000,Z01,,X,,,0000`,
    `115,000,Z002,000,000,,0000002,,,0000000,0000000,Z02,,X,,,0000`).
  - `T683S` ZVAA01 unveraendert gegen `T683S_ZVAA01.csv`.
  - `table-read E071K --where "TRKORR = '<Aufgabe>' AND OBJNAME = 'T683S'"` → erwartet vier **Einzelschluessel**
    `100AV ZICAA1010000`, `100AV ZICAA1012000`, `100AV ZICAA1113000`, `100AV ZICAA1115000` (Muster T76K912167:
    `090AV ZICAA1008000` usw.). Schluesselform im Bericht nennen. Steht dort ein **Sammelschluessel** `100AV ZICAA1*`, ersetzt der
    Import das ganze Schema in P76: dann nicht weiterarbeiten, sondern melden (5.4 Punkt 9).

### 3.5 Schritt 4: Konditionsausschluss fuer ZICAA1 (OV37)

- `/nOV37` (View-Cluster `VVC_T683A_VA`), Schema `ZICAA1` markieren, im Dialogstruktur-Baum den zweiten Knoten oeffnen
  (`VCLSTRUC`: Objekt 01 `VV_T683A_VA` = Schemata, Objekt 02 `V_T684S` = Ausschluss; Bezeichnung am Bild ablesen).
- „Neue Eintraege“, drei Zeilen:

| KAULF (lfd. Nr.) | KAUVF (Verfahren) | KAUGR1 (Gruppe 1) | KAUGR2 (Gruppe 2) |
| --- | --- | --- | --- |
| 005 | D | ZPR0 | Z999 |
| 006 | D | ZPR0 | Z001 |
| 010 | D | Z999 | Z001 |

- Sichern; Auftrag `<AUF>`. ZVAA01-Zeilen nicht anfassen.
- Rueckleseprobe: `table-read T684S --where "KVEWE = 'A' AND KAPPL = 'V'" --rowcount 50 --format csv` → 6 Zeilen: die drei
  ZVAA01-Zeilen unveraendert plus `100,A,V,ZICAA1,005,D,ZPR0,Z999`, `...,006,D,ZPR0,Z001`, `...,010,D,Z999,Z001`.
  `T683` ZICAA1 `AUSVF` weiter leer. `E071K` der Aufgabe enthaelt T684S mit `100AV ZICAA1005` / `006` / `010`.

Wirkung (Quelltext 1.6), drei Faelle:
- PR00 gefunden: Regel 005 schliesst Z999 aus, Regel 006 die Z001-Gruppe (Z000, Z001, Z002, HI01, HI02 …); Regel 010 wirkt
  nicht mehr, weil Z999 schon ausgeschlossen ist → PR00 ist der Preis.
- Kein PR00, Z999 gefunden: 005/006 ohne Wirkung (ZPR0 inaktiv), 010 schliesst die Z001-Gruppe aus → Z999 ist der Preis.
- Weder PR00 noch Z999: keine Regel greift → Z000 − Z001 − Z002.

### 3.6 Schritt 5: Staffelroutine 601 bei Z999 und Z002 (V/06)

- `/nV/06`, Konditionsart `Z999` → Detail, Feld „Staffelroutine“ (`T685A-KOFRS`) = `601` (Text „IC Bestellmenge“). Sichern,
  Auftrag `<AUF>`. Dasselbe fuer `Z002`. Kein anderes Feld aendern; PR00 hat 601 schon.
- Rueckleseprobe: `table-read T685A --where "KAPPL = 'V' AND KSCHL IN ('Z999','Z002','PR00','Z000','Z001')" --fields "KSCHL,KRECH,KZBZG,KOAID,KMANU,KOFRS" --rowcount 10`
  → Z999 `KOFRS 0000601`, Z002 `KOFRS 0000601`, PR00 `0000601`, Z000/Z001 `0000000`; alle anderen Felder wie
  `v1_vorher_2026-11-11/T685A.csv`. `E071K` der Aufgabe: T685/T685A/T685T `100V Z999`, `100V Z002` (Muster T76K912857).
- Hinweis fuer den Bericht: wirkt in P76 erst, wenn `RV62ANNN` dort `INCLUDE RV62A601` enthaelt (NOTES 8.9).

### 3.7 Schritt 6: Kopiersteuerung Lieferung → IV (VTFL), nur pruefen

- Keine Aenderung. Entscheid 07.11. „neue Preisfindung“ ist in T76 erfuellt: `KNPRS B` (1.7). B ist richtig, G waere falsch:
  G uebernimmt die Preiselemente der Vorlage unveraendert und ermittelt nur Steuern neu (DD07T); eine Umlagerungslieferung
  NLCC hat keine Auftragskonditionen, und die neuen Konditionen Z999/Z000/Z001/Z002 muessen in der IV neu ermittelt werden.
  Beleg fuer die Neuermittlung heute: alle IV an 820 tragen PR00 aus A305 (0090381200 u. a.).
- Rueckleseprobe: `table-read TVCPF --where "FKARN = 'IV' AND LFARV = 'NLCC'" --fields "PSTYV,GRBED,GRURP,KNPRS,PFKUR"` →
  fuenf Zeilen, alle `KNPRS B`, `PFKUR` leer (wie `v1_vorher_2026-11-11/TVCPF_IV_NLCC.txt`).

### 3.8 Schritt 7: Kontenfindung, nur lesen

- Keine Aenderung in VKOA. Rueckleseprobe wie 1.8 (`C002 ... KTGRD = '03'`); Ergebnis in den Bericht.

### 3.9 Schritt 8: Abschluss

1. `table-read E071 --where "TRKORR = '<Aufgabe>'"` → nur VDAT/CDAT der Sichten `VV_T684_VA`, `VV_T684G_VA`, `VVC_T683_VA`,
   `VVC_T683A_VA`, `VV_T685A_VA`. `E071K` ohne fremde Schluessel (nur ZPR0, ZICAA1, Z999, Z002).
2. Auftrag **nicht** freigeben (macht Ingo). Eigene Sitzung schliessen (`SapGuiT76Hilfe.vbs zu <ses>`), danach `info`: nur
   noch Ingos Sitzungen.
3. Nachher-Staende als CSV nach `saptasks/ic_preisfindung/v1_nachher_<datum>/` legen.

### 3.10 Rueckbau (nur auf Anweisung Ingos)

In derselben Reihenfolge rueckwaerts, im selben Auftrag: V/06 Z999/Z002 `KOFRS` leeren; OV37 ZICAA1 Zeilen 005/006/010
loeschen; V/08 ZICAA1 Zeilen 010/012/113/115 loeschen; OV32 `ZPR0/PR00` loeschen; OV31 `ZPR0` loeschen. Rueckleseprobe:
Tabellen gleich `v1_vorher_2026-11-11/`.

---

## 4. Testplan nach der Umsetzung (T76)

Ablauf je Fall wie Blatt 4 und NOTES 8.5: ME21N NB Lieferant 70953, EKORG 1200, Werk 1200 (RESWK 1100) → VL10B → LT03/LT12
(WM-Lagernummer 110, WA) → VF01 IV → Konditionsanalyse (`PRCD_ELEMENTS` per RFC) → Rechnungseingang 1200 (IDoc, MIR4/MIRO
nach WE). Fakturakurs `KURRF` 0.94 (NOTES 3, 7).

### 4.1 Stammdaten T76 je Testmaterial (gelesen 11.11.2026)

| Material | PR00 820 (A305 1100/01) | Z999 PL14 (A503) | Z000 (A504 1100) | Z001 820 | Z002 PL14 | Infosatz 70953 (EINA, LTSNR) | PB00 A017 1200/1200 | Bestand 1100/0001 (MARD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 46690 | **ja**: 970131 (01.–08.11.2026) und 970139 (ab 09.11.2026), 89.60 EUR je 1 ST, Staffel 2/5/10/25 ST = 81.40/72.00/70.80/69.60 | 953669 ab 01.01.2026, 89.60 EUR je 1 ST, Staffel 2/5/10/25 = 81.40/72.00/70.80/69.60 | 901309, 208.90 CHF je 1 ST | A522 156759: **0 %** | 213888 | 5300015589, **SPEZ** | 970137 ab 01.11.2026, 89.60 EUR, Staffel wie Z999 | **0** |
| 2585 | 970130 nur 01.11.2026 bis 01.11.2026 (268.32 CHF, Staffel bis 50 ST) → ab 02.11. **keiner** | keiner | 949566 ab 28.12.2025, 479.15 CHF je 1 ST | 0 % | 213888 | 5300015583, leer | 970135 ab 01.11.2026, 509.73 EUR | **0** |
| 45366 | **keiner** | keiner | 937747, 271.15 CHF | 0 % | 213888 | 5300015504, leer | 970032 ab 25.09.2026, **2'700.00 EUR je 100 ST** = 27.00 EUR/ST | **0** |
| 33148 | **keiner** | keiner | 947830, 957.85 CHF | 0 % | 213888 | **kein EINA** | 970136 ab 01.11.2026, 323.70 EUR, Staffel erst **ab 10 ST** | **0** |

Mengen und Preise: alle Konditionen `KPEIN` 1, `KMEIN` ST, ausser 45366 PB00 (`KPEIN` 100). Z002-Staffel in Promille gespeichert
(100.00- = −10 %).

### 4.2 Erwartung in T76 je Testfall (gerechnet)

Z000 in CHF wird je Einheit mit 0.94 in EUR umgerechnet (NOTES 3.2/3.3; Fabio rechnet ebenso: 479.15 / 0.94 = 509.73).
Rundung der Positionswerte ±0.02 EUR ist **Vermutung** (Fabio: 1'070.43 Bestellung gegen 1'070.45 IV).

| Nr | Fall | Erwartung Fabio (P76) | Erwartung T76 heute | Hindernis / Voraussetzung |
| --- | --- | --- | --- | --- |
| T1 | 46690, 1 ST | Z999 89.60 aktiv | Preis 89.60 EUR, aber **PR00 aktiv, Z999 `KINAK A`** (Regel 005) | PR00 970139 gueltig. Fuer „Z999 aktiv“: PR00 46690 in T76 abgrenzen (Fabio) **oder** Fakturadatum vor 01.11.2026 (Preisdatum = Fakturadatum: Vermutung, ungeprueft). Bestand fehlt |
| T2 | 46690, 5 ST | 72.00 | 72.00 EUR/ST, 360.00 EUR (Quelle wie T1) | wie T1 |
| T3 | 46690, 10 ST | 70.80 | 70.80 EUR/ST, 708.00 EUR (Quelle wie T1) | wie T1 |
| T4 | 2585, 1 ST | Z000 479.15 CHF −44 % = 268.32 CHF = 285.45 EUR | Z000 509.73 EUR, Z001 **0 %**, kein Z002 (Staffel ab 2 ST) → **509.73 EUR** | Z001 820 −44 % (A522 wie P76 ab 29.09.2026) fehlt in T76; mit −44 %: 509.73 × 0.56 = 285.45 EUR |
| T5 | 2585, 5 ST | 479.15 −44 % −25 % = 201.24 CHF = 214.09 EUR | ohne −44 %: 5 × 509.73 = 2'548.65; Z002 −25 % = −637.16; = 1'911.49 EUR (382.30 EUR/ST). Mit −44 %: 2'548.65 − 1'121.41 = 1'427.24; −25 % = −356.81; = **1'070.43 EUR** (214.09 EUR/ST) | wie T4. Gegenrechnung mit Bezugsstufe 100 fuer beide (nicht geplant): 2'548.65 × (1 − 0.69) = 790.08 EUR |
| T6 | 45366, 1 ST | PR00 27.00 EUR | **kein PR00** → Z000 271.15 CHF = 288.46 EUR (Z001 0 %) | PR00 820 45366 27.00 EUR in T76 anlegen (Fabio) |
| T7 | 33148, 5 ST | PR00 427.97 | **kein PR00** → Z000 957.85 CHF / 0.94 = 1'018.99 EUR/ST (Z001 0 %), Z002 −25 % → 764.24 EUR/ST | PR00 820 33148 mit Staffel A wie P76 anlegen; Infosatz fehlt (Bestellung 1200 nicht moeglich ohne EINA, Vermutung) |
| T8 | 33148, 10 ST | 323.70 | wie T7 | wie T7 |
| T9 | 46690, 5 ST, MIRO | PO = IV = 72.00 | PB00 970137 Staffel ab 5 = 72.00, SPEZ → RL01/ZRL2 0 % (A069, NOTES 8.8); IV 72.00 → Differenz 0.00 | Bestand, WE in 1200 (Anlieferung ueber NAST SPED, NOTES 8.8) |
| T10 | 46690, 5 ST, Kontierung | IC-Erloeskonten | Z999 KVSL ERL → **30003**; bei Fall A Z000 ERL → 30003, Z001 Z01 → **30100**, Z002 Z02 → **30200** (C002) | FI-Entscheid 5.3 |

### 4.3 Zusaetzliche Tests (vom Plan verlangt)

| Nr | Zweck | Ablauf | Erwartung |
| --- | --- | --- | --- |
| R1 | Zweiter IC-Kunde | IV an 501230 (PLTYP 14) mit einem Material ohne PR00 501230 | Z999/Z000-Kette wie bei 820; Ingo/Fabio entscheiden, ob gewollt (5.1) |
| R2 | ZVAA01 unveraendert | VA01 TA, Auftraggeber 820 oder 301667, ein Material mit Z999 und Z000, **nicht sichern** (wie S5) | wie Auftrag 406808: Z999 aktiv, Z000/Z001/Z002 `A`; Staffelbasis = Auftragsmenge (601 endet in Zeile 39) |
| R3 | Routine 601 bei Z999 | Bestellung 10 ST 46690, Lieferung 3 ST, IV (Z999-Fall, siehe T1) | Z999 `KSTBS` 10, 70.80 EUR/ST, Wert 3 × 70.80 = 212.40 EUR (Standard ohne 601: Staffel ab 2 = 81.40, 244.20 EUR) |
| R4 | Routine 601 bei Z002 | Bestellung 5 ST 2585, Lieferung 2 ST, IV | Z002 `KSTBS` 5 → −25 % statt −10 % |
| R5 | IV01 | Material 35450 (A055 IV01 1100/1100 gueltig) als IV | IV01 Stufe 910 gewinnt als spaeterer Preis (1.6) – Verhalten heute schon so, nur dokumentieren |
| R7 | IV aus Streckengeschaeft (LF, KNPRS G) | Kundenauftrag VKORG 1200 mit Lieferung aus 1100 (Muster Lieferung 0080432730), IV an 501230 | Pruefen, welche Konditionen aus dem Auftrag in die IV kommen und welcher Preis gewinnt; Ergebnis Ingo/Fabio vorlegen (5.4 Punkt 2) |
| R6 | Kurzdumps | ST22 des Testtags in eigener Sitzung | keine Dumps aus SAPLV61A/SAPLV60A |

---

## 5. Offene Fragen, Risiken, FI-Punkte

### 5.1 Entscheide Ingo (vor Phase 2)

*Stand 2026-11-11 09:45: Punkt 1 (Freigabe „Ja, Plan ausfuehren“), Punkt 3 (Mandant 100), Punkt 4 („Kaskade wie ZVAA01“)
und Punkt 5 (601 im selben Auftrag) sind entschieden und in Phase 2 umgesetzt. Offen: Punkt 2 (P76-Wert VTFL), ob Mandant
090 nachgezogen wird, Punkt 6 (501230).*

1. **Freigabe der Schreibschritte 3.2 bis 3.6 in T76** (Auto-Modus, LEARNINGS 2026-10-15/2026-11-05).
2. **VTFL**: In T76 ist IV/NLCC schon `KNPRS B`; ein Aenderungsschritt entfaellt (Abweichung zur Ergaenzung 1). Ob P76 ebenfalls
   B hat, ist nicht gelesen. T76K912762 (MEEY_HAST, aus Mandant 090) hat die Zeile NLCC/NLC transportiert; Ingo pruefe in P76
   `TVCPF` IV/NLCC oder nenne den Importstand von T76K912762.
3. **Mandant**: IC-AT-Customizing wurde bisher in T76 Mandant 090 (Kategorie C) gepflegt, die Routine-601-Eintraege in 100. Soll
   V1 in 100 (wie Auftrag) oder in 090 gepflegt werden? In 100 gepflegt fehlt V1 danach in 090; eine spaetere Mandantenkopie
   090 → 100 koennte es ueberschreiben (Vermutung, Mandantenstrategie unbekannt).
4. **Bezugsstufe Z001/Z002**: Plan nimmt „wie ZVAA01“ (ohne Bezugsstufe, Kaskade, deckt T5). Bestaetigung Fabio, dass „Basis
   Bruttowert“ fuer Z002 nicht woertlich gemeint war (sonst 148.54 statt 201.24 CHF bei T5).
5. **V/06 601 bei Z999/Z002 im selben Auftrag** (Schritt 5) ist eingeplant; wenn Ingo es getrennt will, Schritt 3.6 weglassen.
6. **501230**: Soll V1 auch fuer IV an 501230 gelten? Technisch automatisch (ZICAA1, PLTYP 14).

### 5.2 Voraussetzungen fuer die Tests (Stammdaten, Fabio, nicht Teil von Phase 2)

- Bestand 1100/0001 fuer 46690, 2585, 45366, 33148 (heute je 0).
- 46690: PR00 820 abgrenzen oder anderes Fall-B-Material ohne PR00 820 waehlen, damit T1–T3 „Z999 aktiv“ zeigen.
- 820: Z001 A522 −44 % wie in P76 (ab 29.09.2026) in T76 anlegen, sonst laufen T4/T5 mit 0 %.
- 45366 und 33148: PR00 820 wie in P76 anlegen; 33148 Infosatz 70953/1200.

### 5.3 Nur FI kann klaeren

- Z01 (30100 Wiederverkaufsrabatt) und Z02 (30200 Mengenrabatt) sind fuer KTGRD 01/02/03 gleich, also keine IC-Konten. Entscheid
  07.11. „Z01/Z02 auf IC-Konten“: FI nennt die Zielkonten; dann sind C002 1100/03 Z01 und Z02 zu aendern (VKOA, eigener Schritt).
- Fall A bucht kuenftig brutto auf 30003 (ERL, Z000) plus Rabatte auf Z01/Z02-Konten, statt wie heute netto (PR00) auf 30003.
  Ist das fuer IC-Erloese und Konsolidierung gewollt?
- ERS → 30999 betrifft K-Rabatte; heute in IV nicht vorhanden.

### 5.4 Risiken

1. **ZICAA1 gilt fuer alle KALVG-I-Fakturen** (IG, IG2, IGA, IGS, IV, IVA, IVS; 1100/01/01, 1100/01/02, 1200/01/01). Neue
   IV-Fakturen bekommen ab Import Z999/Z000-Preise, wo kein PR00 da ist. Bestehende Fakturen aendern sich nicht, solange sie
   nicht neu bepreist werden (VF02 „neue Preisfindung“, Storno IVS mit KNPRS D uebernimmt die alten Werte – Vermutung).
2. **IV aus LF-Lieferungen** (TVCPF IV/LF, Positionstyp TAN, KNPRS G „Preiselemente unveraendert uebernehmen“). Solche IV
   gibt es in T76: 0090381122 und 0090381124 (an 501230, Lieferungen 0080432730/0080432732, `LIKP-LFART` LF, VKORG 1200,
   Warenempfaenger 302827, `KUNIV` 501230; Streckengeschaeft). In 0090381122/0090381123 steht PB00 100.00 bzw. 120.00 EUR mit
   `KINAK Y` und IV01 66.00 EUR aktiv. **Vermutung (nicht geprueft):** Bei G werden Auftragskonditionen uebernommen, soweit
   ZICAA1 die Konditionsart kennt; nach V1 kaemen damit auch Z999/Z000/Z001/Z002 aus dem Kundenauftrag in die IV. Gewinnt
   dann kein IV01, waere der Endkundenpreis der IC-Preis. Regressionstest R7.
3. **Uebergang**: Solange die 821 PR00-Spiegelsaetze 820 (ab 01.11.2026) gueltig sind, gewinnt PR00 (Regel 005/006); V1 zeigt
   erst nach deren Abgrenzung Wirkung (Fabio, Hinweis F). Abgrenzung gehoert in denselben Go-live-Plan.
4. **IV01/IV02 an 909/910** sind Preise (KOAID B) und gewinnen als spaetere Preiskondition gegen PR00 und Z999 (Beleg 1.6). In T76
   ist nur ein IV01-Satz gueltig (A055 1100/1100/35450). P76 nicht gelesen.
5. **HI01/HI02** stehen in Gruppe Z001 und werden ausgeschlossen, sobald PR00 oder Z999 gefunden wird. Heute kommen sie in keiner
   ZICAA1-Faktura vor (1.9), also ohne Wirkung.
6. **Routine 601 global**: `T685A` wirkt in allen Schemas; Schutz nur ueber Zeile 39/40 der Routine. In P76 wirkt 601 noch nicht
   (RV62ANNN, Entscheid RV80HGEN offen, NOTES 8.9).
7. **Z999 nicht exklusiv** ab A503 (A6, T682I `KZEXL` leer). **Vermutung:** Mit Saetzen in A503 und A502/A504 entstehen zwei
   Z999-Zeilen, und die spaetere gilt als Preis. Nicht belegt; fuer 820 und die Testmaterialien heute ohne Fall (A504 Z999 leer).
8. **Waehrung Fall A**: Z000 in CHF, IV in EUR mit `KURSK` = `KURRF` 0.94 (M → EURX), die Bestellung 1200 rechnet PB00 in EUR
   (Fabio: Z000/0.94). Gleich, solange P- und EURX-Kurs gleich gepflegt sind (NOTES 7.1).
9. **Transport nach P76 und Mandantenstand**: T76/100 und T76/090 sind bei ZICAA1 **nicht gleich**. Auftrag T76K912167 (MEEY_HAST,
   Mandant 090, freigegeben) enthaelt fuer ZICAA1 Einzelschluessel aller Stufen, darunter **`090AV ZICAA1911000`**; in T76/100 hat
   ZICAA1 keine Stufe 911. Die Juni-Fakturen an 501230 tragen KW00 auf Stufe 911 (1.9). P76 entspricht vermutlich dem Stand aus
   090. Deshalb: Phase 2 muss Einzelschluessel erzeugen (3.4), und Ingo vergleicht vor dem Import in P76 `T683S` ZICAA1 (Stufen
   010/012/113/115 frei?), `T684` (ZPR0 frei?), `T684S` ZICAA1 (leer?) und `T685A` Z999/Z002 `KOFRS` gegen T76. Kein Cache-Leeren
   noetig (kein Gateway).
10. **A304-Materialpreise PR00** (6 Saetze 1100/01) schlagen nach V1 ueber ZPR0 die Preislisten fuer jeden IC-Kunden (1.5).

### 5.5 Kandidaten fuer LEARNINGS

- Prozentkonditionen ohne Bezugsstufe rechnen auf dem laufenden Wert (Kaskade); „Basis Bruttowert“ in einer Spezifikation
  vor dem Abtippen gegen die erwarteten Testwerte nachrechnen.
- Customizing-Pflege kann in einem anderen Mandanten liegen als die Tests (T76: 090 Kategorie C, 100 Test); vor dem Anlegen
  `E070C`-Mandanten frueherer Auftraege desselben Themas lesen.
- Konditionswerte in `KONP`/`KONM` fuer Prozent stehen in Promille (100.00- = −10 %); Preiseinheit `KPEIN` je Satz pruefen
  (45366 PB00 2'700.00 EUR je **100** ST).

---

## 6. Ausfuehrung Phase 2 (2026-11-11)

Agent sap-t76, Auftrag der Hauptsitzung mit Freigabe Ingo („Ja, Plan ausfuehren“; Bezugsstufe „Kaskade wie ZVAA01“;
Mandant 100; Routine 601 im selben Auftrag).

**Ergebnis: Schritte 0 bis 8 ausgefuehrt und rueckgelesen (zweiter Anlauf 09:20 bis 09:36, Abschnitt 6.4).** Der erste
Anlauf (08:58) hielt nach Schritt 0 an, weil SAP GUI nicht lief (6.1 bis 6.3, dort als erster Anlauf stehen gelassen).

**Auftragsnummer / Aufgabe:** Customizing-Auftrag **T76K912884** „IC V1: ZICAA1 Z999/Z000/Z001/Z002 + Ausschluss ZPR0“
(W, D, Ziel P76, KOI, `E070C` Mandant 100), Aufgabe **T76K912885** (Q, D). Nicht freigegeben. P76 nicht gelesen und nicht
beruehrt.

### 6.1 Erster Anlauf: Schritt 0 (Vorpruefung, nur lesen, 08:58 bis 09:02) – ok

| Pruefung | Erwartet | Ist |
| --- | --- | --- |
| `Test-SapPassword` | Vorhanden True | True, System T76, Benutzer koi, angelegt 10.09.2026 |
| E071K `TABKEY LIKE '100AV ZICAA1%' OR '100DAV ZICAA1%'` | keine Zeile | keine Zeile |
| E071K T684/T684G/T684T `100AV ZPR0%` | keine Zeile | keine Zeile |
| E071K T685/T685A/T685T mit Z999/Z002 | nur T76K900012, T76K900174, T76K900569, T76K904335, T76K904861, alle R | diese fuenf (alle `TRSTATUS R`) **und zusaetzlich** T76K908109/T76K908110 (MEEY_HAST, 16.12.2022) und T76K908190/T76K908191 (MEEY_HAST, 10.01.2023), alle `R`, Schluessel nur Mandant **090** (`090AV Z999`, `090V Z002` usw.) |
| T683S ZICAA1 / ZVAA01 | 54 / 47 Datenzeilen wie `v1_vorher_2026-11-11/` | 54 / 47, `diff` (sortiert) Exitcode 0 |
| T684 / T684G / T684S | 6 Gruppen / 11 Zeilen / 3 Zeilen nur ZVAA01 | 6 / 11 / 3, `diff` Exitcode 0 (T684G, T684S) |
| T685A Z999/Z002 `KOFRS` | 0000000 | 0000000; alle 13 Zeilen gleich `T685A.csv` (`diff` Exitcode 0) |
| T683 ZICAA1 `AUSVF` | leer | leer (BONSM X), ZVAA01 leer |
| TVCPF IV/NLCC | 5 Zeilen, KNPRS B | 5 Zeilen, KNPRS B, PFKUR leer |

Abweichung: Die vier zusaetzlichen Auftraege sind freigegeben und betreffen Mandant 090; das Kriterium „keine Zeile in einem
Auftrag mit `TRSTATUS` D/L“ ist erfuellt, eine Sperre besteht nicht. Sie bestaetigen Risiko 5.4 Punkt 9: Z999/Z002 wurden
2022/2023 in Mandant 090 gepflegt und transportiert.

Zusaetzlich gesichert (Vorher-Stand fuer die Rueckleseprobe von Schritt 5, weil V/06 neben T685A auch T685 und T685T in den
Auftrag schreibt): `v1_vorher_2026-11-11/T685_Z999_Z002_PR00.csv` (KOZGF Z999 → Z999, Z002 → Z001, PR00 → PR02),
`T685T_Z999_Z002.csv` (D/E/F), `T685A_voll_Z999_Z002_teil1..3.csv` (alle Felder laut DD03L), dazu `T684T.csv` und
`T683_ZICAA1_ZVAA01.csv`.

### 6.2 Erster Anlauf: Schritt 1 – nicht ausgefuehrt (ueberholt durch 6.4)

`SapGuiT76Hilfe.vbs info` brach in Zeile 24 (`GetObject("SAPGUI")`) mit „Ungueltige Syntax“ ab; `Get-Process` zeigt keinen
`saplogon`-Prozess (09:02). SAP GUI war also nicht gestartet, vermutlich seit dem PC-Neustart vom 10.11. abends. Eine eigene
Sitzung braucht eine angemeldete T76/100-Verbindung; anmelden hiesse ein Passwort eingeben, das ist ausgeschlossen. Die
Schreibschritte 1 bis 5 (GUI) deshalb nicht begonnen. Schritte 6 und 7 sind RFC-Lesepruefungen nach den Aenderungen und
gehoeren zum naechsten Anlauf; die TVCPF-Werte aus Schritt 0 entsprechen schon der Erwartung von Schritt 6. Schritt 8 entfaellt.
Keine Nachher-Staende (`v1_nachher_2026-11-11/` nicht angelegt, weil sich nichts geaendert hat).

Gelesen um 09:07, damit der naechste Anlauf nicht am Scripting scheitert: Serverparameter `sapgui/user_scripting` = `TRUE`
(RFC `TH_GET_PARAMETER`, RC 0); Registry `HKCU\Software\SAP\SAPGUI Front\SAP Frontend Server\Security`: `UserScripting 1`,
`WarnOnAttach 0`, `WarnOnConnection 0`. Beides ist bereits richtig eingestellt.

### 6.3 Was fuer den naechsten Anlauf galt (im zweiten Anlauf beachtet)

- Voraussetzung: Ingo startet SAP Logon und meldet sich an T76 Mandant 100 an (Scripting ist server- und arbeitsplatzseitig
  schon eingeschaltet, 6.2). Danach Phase 2 ab Schritt 0 wiederholen (Schritt 0 ist schnell und muss wegen moeglicher neuer
  Sperren neu laufen).
- Passwort: Die RFC-Ablage hat heute noch funktioniert, der am 10.11. empfohlene Wechsel des T76-Passworts ist also noch
  nicht passiert. Wechselt Ingo es bei der Anmeldung, muss er vor dem naechsten Anlauf selbst `Set-SapPassword` ausfuehren;
  `Test-SapPassword` zeigt nur, dass eine Ablage existiert, nicht dass sie stimmt, und Fehlanmeldungen per RFC koennen KOI
  sperren.
- Vor `SapGuiT76Hilfe.vbs neu` erst `info`; weiter nur, wenn die Sitzungszahl genau um eins steigt und die neue Id vorher
  nicht da war (das Skript nimmt sonst still die letzte vorhandene Sitzung).
- Fuer Ingo vor dem Import (zu 5.4 Punkt 9): V/06 schreibt Schluessel fuer T685, T685A und T685T in den Auftrag (Beleg
  NOTES 8.4: T76K912857 mit `100V PR00` fuer alle drei Tabellen). Dass dabei die ganze Tabellenzeile transportiert wird und
  nicht nur das geaenderte Feld `KOFRS`, ist Standardverhalten von Tabellenschluesseln im Transport, hier aber **nicht
  gemessen** (Vermutung). In P76 deshalb vor dem Import die vollstaendigen Zeilen Z999/Z002 gegen die Dateien oben
  vergleichen, nicht nur die Staffelroutine.

### 6.4 Zweiter Anlauf (09:20 bis 09:36): Schritte 0 bis 8

Ingo meldete sich um 09:19 an T76/100 an (`saplogon` PID 7180, Passwort nicht gewechselt). `Test-SapPassword` vorher:
vorhanden. Eigene Sitzung: `info` vorher nur `ses[0]` (SESSION_MANAGER, Ingo), `neu` meldete „vorher=1 nachher=2“, Id
`/app/con[0]/ses[1]`, `info` nachher `ses[0]` und `ses[1]`. Gearbeitet nur in `ses[1]`; am Ende mit `zu 1` geschlossen,
danach nur noch `ses[0]`. Ingos Sitzung nicht benutzt. Kein Befehl abgelehnt, kein Anmeldebild, keine E- oder W-Meldung.

| Schritt | Ergebnis | Rueckleseprobe erwartet | Rueckleseprobe ist |
| --- | --- | --- | --- |
| 0 Vorpruefung (09:20) | ok | wie 6.1 | wie 6.1: E071K ZICAA1 und ZPR0 keine Zeile; Z999/Z002 nur die bekannten freigegebenen Auftraege (darunter die 090-Auftraege, laut Hauptsitzung bekannt); T683S 54/47, T684 6, T684G 11, T684S 3, T685A 13 Zeilen, alle `diff` Exitcode 0 |
| 1 OV31 Gruppe ZPR0 (09:23) | ok, „Daten wurden gesichert“ | T684 1 Zeile, T684T `Sonderpreis PR00`, Auftrag W/D/P76/KOI, Aufgabe Q/D, E070C 100 | T684 `100,A,V,ZPR0`; T684T `D,ZPR0,Sonderpreis PR00` (nur D); T76K912884 W/D/P76/KOI, T76K912885 Q/D; E070C CLIENT 100. Auftragsdialog: Feld leer vorbelegt, mit F8 neu angelegt, Text 51 Zeichen zurueckgelesen |
| 2 OV32 ZPR0/PR00 (09:25) | ok, „Daten wurden gesichert“; Auftrag im Dialog vorgeschlagen T76K912884, ausdruecklich gesetzt | T684G 12 Zeilen = 11 vorher + `ZPR0,PR00` | 12 Zeilen; sortierter `diff` gegen `T684G.csv` zeigt genau `> 100,A,V,ZPR0,PR00` |
| 3 V/08 ZICAA1 Steuerdaten (09:29) | ok, „Daten wurden gesichert“; Auftrag gesetzt | 58 Zeilen, genau die vier neuen, ZVAA01 unveraendert | 58 Zeilen; sortierter `diff` gegen `T683S_ZICAA1.csv` zeigt genau `010,000,Z999,000,000,,0000002,,,0000000,0000000,ERL,,,,,0000`, `012,000,Z000,...,ERL,,,,,0000`, `113,000,Z001,...,Z01,,X,,,0000`, `115,000,Z002,...,Z02,,X,,,0000` (zeichengleich mit der CSV-Form in 3.4); ZVAA01 `diff` Exitcode 0. Am Bild vor dem Sichern alle 18 Spalten der vier Zeilen ausgelesen, auch die Ankreuzfelder (nur DRUKZ X bei Z001/Z002) |
| 4 OV37 Ausschluss ZICAA1 (09:31) | ok, „Daten wurden gesichert“; Auftrag gesetzt | T684S 6 Zeilen, ZVAA01 unveraendert, T683 `AUSVF` leer | ZVAA01 010/015/020 unveraendert, neu `ZICAA1,005,D,ZPR0,Z999`, `006,D,ZPR0,Z001`, `010,D,Z999,Z001`; T683 ZICAA1 `AUSVF` leer, `BONSM X` |
| 5 V/06 Z999 und Z002 (09:32 bis 09:34) | ok; Z999 mit Auftragsdialog (gesetzt), Z002 sicherte **ohne** Dialog direkt („Daten wurden gesichert“), weil die Sicht `VV_T685A_VA` schon in der Aufgabe stand; Schluessel landeten in derselben Aufgabe (E071K) | T685A Z999/Z002 `KOFRS 0000601`, sonst unveraendert | sortierter `diff` gegen `T685A.csv`: nur Z999 und Z002 `0000000` → `0000601`; volle Zeilen (alle Felder laut DD03L, drei Teile): nur `KOFRS` anders; T685 und T685T `diff` Exitcode 0. Text am Bild „IC Bestellmenge“ |
| 6 VTFL nur lesen | ok, keine Aenderung | 5 Zeilen KNPRS B, PFKUR leer | 5 Zeilen, alle KNPRS B, PFKUR leer, FKMGK B |
| 7 VKOA nur lesen | ok, keine Aenderung | wie 1.8 | C002 KTOPL 1000/VKORG 1100/KTGRD 03, KOFI und KOFK gleich: ERF 30050, ERL 30003, ERS 30999, Z01 30100, Z02 30200, Z04 30300 |
| 8 Abschluss | ok | E071 nur die fuenf Sichten, E071K nur eigene Schluessel | siehe unten; Auftrag nicht freigegeben; eigene Sitzung geschlossen |

**E071 der Aufgabe T76K912885** (Datei `v1_nachher_2026-11-11/E071_T76K912885.csv`): `VDAT VV_T684_VA`, `VDAT VV_T684G_VA`,
`CDAT VVC_T683_VA`, `CDAT VVC_T683A_VA`, `VDAT VV_T685A_VA`, alle `OBJFUNC K`, `LOCKFLAG` leer. Der Auftrag T76K912884 selbst
hat keine Objektzeilen (normal bei Customizing). Nichts Fremdes.

**E071K-Schluesselform** (20 Zeilen, `v1_nachher_2026-11-11/E071K_T76K912885.csv`), alles **Einzelschluessel**, kein
Sammelschluessel `100AV ZICAA1*`:

- T684 `100AV ZPR0`, T684T `100DAV ZPR0`, T684G `100AV ZPR0PR00`
- T683S `100AV ZICAA1010000`, `...012000`, `...113000`, `...115000`
- T683T `100DAV ZICAA1010000`, `...012000`, `...113000`, `...115000`
- T684S `100AV ZICAA1005`, `...006`, `...010`
- T685 `100AV Z999`, `100AV Z002`; T685A `100V Z999`, `100V Z002`; T685T `100DAV Z999`, `100DAV Z002`

**Abweichung zum Plan 3.4:** Neben den vier T683S-Schluesseln hat V/08 vier T683T-Schluessel (Stufentexte, Sprache D) in die
Aufgabe geschrieben. In T683T gibt es fuer diese Stufen **keine** Zeile (gelesen: ZICAA1 hat Texte nur fuer 100, 300, 400,
800, 900, 920, 950 in D/E/F). Es sind Einzelschluessel, kein Sammelschluessel; der Plan nennt das nicht als Haltegrund, das
Sichern war zudem schon erfolgt. **Ich habe danach mit Schritt 4 und 5 weitergemacht**; ob die Schluessel bleiben oder Ingo
sie vor der Freigabe aus T76K912885 entfernt, ist sein Entscheid (nicht selbst entfernt). Vorbild: Der freigegebene Auftrag
T76K912167 (MEEY_HAST, Mandant 090) enthaelt T683T-Schluessel fuer **alle** ZICAA1-Stufen (`090DAV ZICAA1008000`,
`...011000`, `...013000` usw.), also auch fuer Stufen ohne Text; V/08 schreibt die Textschluessel demnach je Stufe mit
(E071K gelesen 09:45). *Vermutung (nicht gemessen):* Ein Schluessel ohne Quellzeile loescht beim Import eine gleichnamige
Zeile im Ziel; fuer ZICAA1-Stufen 010/012/113/115 ohne Text in P76 ohne Wirkung. Ingo kann das vor dem Import in P76
`T683T` ZICAA1 mitpruefen.

Statusmeldungen: nach jedem Sichern „Daten wurden gesichert“ (Typ S), V/06-Positionieren ohne Meldung, OV37-Sicht
„Ausschluss“ vor dem Anlegen „Keine Eintraege gefunden, die den Selektionskriterien entsprechen“ (S, erwartet: ZICAA1 hatte
keine T684S-Zeile). Keine anderen Fenster als „Abfrage Customizing-Auftrag“, „Auftrag anlegen“ und „Anderer Eintrag“
(Positionieren).

Bedienung, die vom Plan abwich (ohne fachliche Folge): Im Dialogstruktur-Baum von V/08 und OV37 (`SAP.TableTreeControl`)
oeffnete `DoubleClickNode` den Knoten nicht; erst `SelectItem`/`DoubleClickItem` mit Spalte `Column1` ging. Die Schemazeile
wurde dafuer ueber `GetAbsoluteRow(...).Selected` markiert (Hilfsskripte im Scratchpad, nur fuer die eigene Sitzungs-Id). Die
Tabelle in V/08 zeigt nur drei Zeilen; Zeile 4 (115 Z002) nach Enter in der naechsten freien Zeile erfasst.

**Nachher-Staende:** `v1_nachher_2026-11-11/` mit T683S_ZICAA1, T683S_ZVAA01, T683_ZICAA1_ZVAA01, T684, T684T, T684G, T684S,
T685A, T685A_voll_Z999_Z002_teil1..3, T685_Z999_Z002_PR00, T685T_Z999_Z002, TVCPF_IV_NLCC, C002_1000_1100_KTGRD03,
E071_T76K912885, E071K_T76K912885 (alle CSV mit Kopfzeile).

### 6.5 Offen nach Phase 2

*(Vermerk 2026-11-12, Doku-Pruefung: Die Liste ist der Stand vom 2026-11-11. Seither hat Fabio in T76/100 getestet und
T76K912884 fachlich freigegeben (`NOTES.md` 9.1, Angaben Fabio, im System nicht nachgelesen): gemeldet sind T1 bis T8,
Kontierung, MIRO und zwei 601-Teillieferungen; die Stammdaten aus Punkt 2 hat Fabio in T76 selbst vorbereitet; R1 und R7 hat
er nicht gemeldet. Punkt 5 („nicht gelaufen“, „ohne Preistest nicht belegt“) und Punkt 2 sind damit in diesem Umfang
ueberholt. Der Import nach P76 wartet laut Ingo (12.11.) auf Fabios Z999-Preise in P76. Die gueltige Liste „Offen vor
Freigabe und Import“ steht in `NOTES.md` 9.1.)*

1. **Ingo:** Auftrag T76K912884 erst freigeben, wenn die Tests bestanden sind und Fabio zugestimmt hat. Vor dem Import in P76
   gegenpruefen (5.4 Punkt 9): `T683S` ZICAA1 Stufen 010/012/113/115 frei, `T683T` dieser Stufen leer, `T684` ZPR0 frei,
   `T684S` ZICAA1 leer, `T685`/`T685A`/`T685T` Z999 und Z002 vollstaendig gegen `v1_nachher_2026-11-11/` (nicht nur `KOFRS`).
   Routine 601 wirkt in P76 erst, wenn `RV62ANNN` `INCLUDE RV62A601` enthaelt (NOTES 8.9, Entscheid offen).
2. **Fabio (Plan 5.2):** Stammdaten fuer die Tests T1 bis T10 in T76: Bestand 1100/0001 fuer 46690, 2585, 45366, 33148; PR00
   820 46690 abgrenzen (oder anderes Material ohne PR00 820), damit T1 bis T3 „Z999 aktiv“ zeigen; Z001 820 −44 % (A522) wie
   P76; PR00 820 fuer 45366 und 33148; Infosatz 70953/1200 fuer 33148.
3. **FI (5.3):** Zielkonten fuer Z01/Z02 bei IC; heute 30100/30200 wie fuer Dritte (Schritt 7).
4. Mandant: Ingo hat Mandant 100 entschieden (Freigabe 11.11.). Offen ist nur, ob Mandant 090 nachgezogen wird (V1 fehlt
   dort, 5.1 Punkt 3). Offen bleibt auch 5.1 Punkt 6 (501230).
5. Tests T1 bis T10 und R1 bis R7 sind nicht gelaufen (nicht Teil von Phase 2). **V1 wirkt in T76/100 seit etwa 09:23 bis
   09:34** fuer jede neue Faktura mit KALVG I (ZICAA1), auch fuer IV anderer Tester an 820 und 501230; die Wirkung ist ohne
   Preistest nicht belegt.
