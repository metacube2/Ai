# IC-Preisfindung Trafag AG (1100) an Trafag AT (1200): Preisliste und Kurstyp in der IV-Faktura

Stand: 2026-11-10 abends (Abschnitt 8.9: Abnahme Fabio bestanden, beide Auftraege in P76 importiert, **Routine 601 in P76 aber
noch nicht eingebunden**: VOFM 601 in P76 aktivieren, macht Ingo); davor 2026-11-06
(Abschnitt 8: Staffelbasis Bestellmenge in T76 umgesetzt und getestet, Routine 601, Auftraege
T76K912853 und T76K912856, *damals* nicht freigegeben; Abschnitt 8.8 vom Nachmittag: Tests S6/S7 und Gegenprobe, Testtabelle
S1 bis S7, Zeile „Transportfertig“). Abschnitte 0 bis 7 vom 2026-11-05 (Agent sap-t76, damals nur lesend in
T76/100; P76 lesend verglichen in Abschnitt 7, Freigabe Ingo „mache punkt 1“). Die Aussagen „keine Testbelege, keine
Aenderung in T76“ in Abschnitt 4 gelten nur fuer den 05.11.

Unterlagen: `a.xlsx` (IC_Umsetzung_V1, Blaetter 2 bis 4) und `b.pdf` (IC-Preisboard) in der Repository-Wurzel.
Geplanter Schritt 1 (Erweiterung ZICAA1 um Z999, Z000, Z001, Z002) ist **nicht** Teil dieser Pruefung und wurde nicht
angefasst.

Alle Belege in den Abschnitten 1 bis 6 stammen aus T76 (Datenkopie etwa Fruehjahr 2026). Fuer P76 sind sie Hinweise,
kein Nachweis. P76 wurde in diesem ersten Auftrag nicht gelesen; der spaetere Abgleich mit P76 (Kundenstamm und
Kurstabellen) steht in Abschnitt 7.

## 0. Ergebnis in zwei Saetzen

1. **Ja, die Preisliste kommt in der IV-Faktura an.** Sie steht in `VBRK-PLTYP` (und `VBRP-PLTYP_AUFT`), stammt bei
   der Faktura aus einer Umlagerungslieferung ohne Kundenauftrag aus dem Kundenstamm `KNVV-PLTYP` des internen Kunden
   (`LIKP-KUNIV` = 820, Vertriebsbereich `LIKP-VKOIV/VTWIV/SPAIV` = 1100/01/01) und wird der Preisfindung als
   `KOMK-PLTYP` uebergeben, das die Zugriffe A503 (Z999, Z000) und A523 (Z001, Z002) lesen.
2. **Kurstyp P funktioniert fuer eine CHF-Kondition in einer EUR-Faktura**, weil nur die Richtung EUR nach CHF gebraucht
   wird und die in TCURR/TCURF fuer P gepflegt ist (2026: 0.94). `KNVV-KURST` wirkt bei der IV-Faktura aber **nicht nur
   auf die Preisfindung, sondern auch auf den Buchungskurs `VBRK-KURRF`**, der als Kurs an die Finanzbuchhaltung geht.

Eine Testkette war nicht noetig, weil Schritt 1 lesend eindeutig wurde. Deshalb gab es auch keinen Schritt 3 (Kurstyp
im Kunden 820 setzen). In T76 haette ein solcher Test ohnehin keinen Unterschied gezeigt (Abschnitt 3.4).

## 1. Daten in T76

### 1.1 Kundenstamm 820

`KNVV` (KUNNR 0000000820): VKORG 1100, VTWEG 01, SPART 01, **PLTYP 14**, **KURST leer**, KALKS 1, WAERS EUR,
INCO1 DAP, ZTERM ZB01. Das deckt sich mit dem P76-Screenshot von Ingo.

### 1.2 Vorhandene IV-Fakturen fuer Kunde 820

`VBRK` mit KUNAG 0000000820 und FKART IV: 12 Belege, alle FKTYP I, KALSM **ZICAA1**, **PLTYP 14**, **KURST leer**,
WAERK EUR, STWAE CHF, **KURRF 0.94**, BUKRS 1100, LAND1 AT:

0090381136, 0090381137, 0090381142, 0090381143, 0090381144, 0090381177, 0090381179, 0090381181, 0090381189,
0090381198, 0090381199, 0090381200 (FKDAT 03.07. bis 25.10.2026).

Achtung: Diese Belege sind **nach der Datenkopie in T76 entstanden** (Erfasser MEEY_HAST, FPA, MEV; Juli bis Oktober
2026). Es sind Testbelege anderer Leute, keine Produktivbelege.

Kette am Beispiel 0090381200: Position 10, Material 39101, 1 ST, PSTYV NLC, `VBRP-VGBEL` 0080432792 (Lieferung),
`VBRP-AUBEL` 0045148482 (Bestellung, AUTYP V), `VBRP-KURSK` 0.94, `VBRP-PLTYP_AUFT` 14, `VBRP-VKORG_AUFT` 1100.
Lieferung 0080432792: `LIKP-LFART` NLCC, `KUNIV` 0000000820, `VKOIV/VTWIV/SPAIV` 1100/01/01, `FKAIV` IV.
Lieferpositionen NLC mit `VGTYP` V (Bestellbezug), also ohne Kundenauftrag.

Preiselemente (`PRCD_ELEMENTS`, KNUMV 0000748057): PR00 57.77 EUR, KKURS 0.94; VPRS 55.03 EUR (KNTYP G), KKURS 0.94.
Der Preis kommt heute aus PR00 (Stufe 011), nicht aus der Preisliste, weil Z999/Z000 noch nicht im Schema stehen.

Buchhaltung (`BKPF`, AWTYP VBRK): 0090381136, 0090381142, 0090381200 je BUKRS 1100, BLART RV, WAERS EUR, HWAER CHF,
**KURSF 0.94**, WWERT = Fakturadatum.

### 1.3 Felder im Fakturabeleg (DD03L)

`PLTYP` gibt es in VBRK (und VBRKVB, VBKD, KNVV, KOMK); in VBRP gibt es nur `PLTYP_AUFT` (Preisliste aus dem Auftrag
bzw. Kundenstamm). `KURST` gibt es in VBRK, KNVV, VBAK, KOMK. `KURRF` in VBRK/VBKD/KOMK, `KURSK` in VBRP/VBKD/KOMP.
Texte (DD04T): KURST „Kurstyp“, KURRF „Waehrungskurs fuer Buchungen an die Finanzbuchhaltung“, KURSK „Kurs fuer
Preisfindung“.

### 1.4 Kopiersteuerung Lieferung nach IV (TVCPF, FKARN IV)

| LFARV | PSTYV | GRBED (Kopierbedingung) | GRURP (Datenuebernahme VBRK/VBRP) | KNPRS (Preisfindungsart) | PFKUR | FKMGK | PLMIN |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NLCC | (Kopf) | 014 „Kopf lieferbez. IV“ | 001 | B | leer | B | + |
| NLCC | NLC | 018 „Pos. liefbez.o.A. IV“ | 003 „Einzelrechnung“ | **B „Neue Preisfindung durchfuehren“** | **leer** | B | + |

Texte aus TFRMT (Gruppen FBED, FDAT) und DD07T (Domaenen KNPRS, PFKUR). Die Datenuebernahme 003 „Einzelrechnung“
betrifft den Rechnungssplit, nicht die Preisliste. Die Fakturaart IV hat in TVFK VBTYP 5 (interne Verrechnung) und
KALVG I.

### 1.5 Kondition und Zugriffe fuer den geplanten Schritt 1 (nur gelesen)

- T685A: Z000 KRECH C, Z999 KRECH C, Z001/Z002 KRECH A; KNTYP bei allen leer (wichtig fuer Abschnitt 3.2).
- T685: Zugriffsfolgen Z000 → Z000, Z999 → Z999, Z001 → Z001, Z002 → Z001.
- T682I/T682Z: Z999 Stufe 020 = A503 (VKORG aus KOMK-VKORG, **PLTYP aus KOMK-PLTYP**, MATNR aus KOMP-PMATN); Z000
  Stufe 030 = A503 ebenso, Stufe 040 = A504 (VKORG, MATNR); Z001 Stufe 025 = A523 (VKORG, **PLTYP aus KOMK-PLTYP**).
- Konditionssaetze fuer das Testmaterial 46690 in T76:
  - Z999 A503 VKORG 1100, PLTYP 14, gueltig ab 01.01.2026 bis 31.12.9999, KNUMH 0000953669: **89.60 EUR je 1 ST**.
  - Z000 A503 PLTYP 14: nur KNUMH 0000221071 (bis 31.12.2018, Loeschvormerkung X, 59.14 CHF), also ohne Treffer 2026.
  - Z000 A504 VKORG 1100, gueltig ab 22.10.2024, KNUMH 0000901309: **208.90 CHF je 1 ST**.
- ZICAA1 heute (T683S): unter anderem EK01 (008), PR00 (011), PB00 (013), VA00, K-Rabatte, NETP, PN00, PMIN, IV02
  (909), IV01 (910), PC00, SKTV, MWST, DIFF, VPRS (940), EK02. Kein Z999/Z000/Z001/Z002.

## 2. Frage 1: Kommt die Preisliste in der IV-Faktura an? Ja.

### 2.1 Datenbeleg

Alle 12 IV-Fakturen fuer 820 tragen `VBRK-PLTYP` 14 und `VBRP-PLTYP_AUFT` 14, gleich `KNVV-PLTYP`. Der Kurstyp ist
in allen leer, gleich `KNVV-KURST`.

### 2.2 Quelltextbeleg (Fakturierung, Programm SAPLV60A, T76)

1. `LV60AA20` (FORM `FAKTURAPOSI_LIEFERBEZOGEN_O_AP`, „Erzeugen Fakturakopf und -position aus Lieferposition ohne
   Auftragsposition“), Zweig „Lieferung ohne Auftrag“ (Zeile 101): bei interner Verrechnung werden AG/RG/RE auf
   `likp-kuniv` gesetzt (Zeilen 159 bis 167, ohne AG-Partner Zeile 188), dann `VIEW_KUAG2` (ab Zeile 237) liefert die Auftraggebersicht
   `KUAGV`; Zeile 284/285 `MOVE-CORRESPONDING kuagv TO vbkd.` / `... TO vbak.`. Der Vertriebsbereich kommt fuer IV aus
   `likp-vkoiv/vtwiv/spaiv` (Zeilen 68 bis 70).
2. `VIEW_KUAG2` (`LV05EU02`, Zeile 140 `PERFORM lesen_kuagv_knvv_03`) → `LV05EF2C` FORM `LESEN_KUAGV_KNVV_03`:
   liest KNVV und fuellt `KUAGV` (`PERFORM move_lknvv_to_kuagv`, Kundeninclude `V05EZZAG`:
   `MOVE-CORRESPONDING LKNVV TO KUAGVZ. MOVE-CORRESPONDING KUAGVZ TO KUAGV.`). `KUAGV` ist eine Struktur (DD02L INTTAB)
   mit den Feldern PLTYP, KURST, KONDA, KALKS.
3. `LV60AA95` (FORM `VBRK_VBRP_DATENTRANSPORT`), Zeilen 124 bis 133, „Sonderbehandlung interne Verrechnung“:
   `vbrk-waerk = kuagv-waerk.` `vbrk-kurst = kuagv-kurst.` ... `vbrk-pltyp = kuagv-pltyp.`
4. `LV60AA93` (FORM `VBRK_VBRP_FUELLEN`), Zeile 28 `MOVE-CORRESPONDING vbkd TO vbrk.` und Zeile 253
   `vbrp-pltyp_auft = vbkd-pltyp.` (zweiter Weg, gleiche Quelle KUAGV).
5. `LV60AA58` (FORM `PREISFINDUNG_VORBEREITEN`), Zeile 53 `MOVE-CORRESPONDING xvbrk TO tkomk.`, Zeilen 137/138
   `IF tkomk-pltyp = space. tkomk-pltyp = xvbrp-pltyp_auft.`; Zeile 515 `MOVE-CORRESPONDING xvbrp TO tkomp.`
6. Keine Kundenaenderung dazwischen: `RV60AFZC` (`USEREXIT_FILL_VBRK_VBRP`) und `RV60AFZZ`
   (`USEREXIT_PRICING_PREPARE_TKOMK`) sind leer, `USEREXIT_PRICING_PREPARE_TKOMP` setzt nur `ZZEUFLG`/`ZZLGORT`;
   `ENHINCINX` zeigt fuer SAPLV60A, SAPLV61A und SAPLV05E keine Erweiterung mit Z oder Y (nur SAP-Branchenerweiterungen
   OIA_/OIU_/OIC_).

Folge: `KOMK-PLTYP` = 14 fuer jede IV-Faktura an 820, solange `KNVV-PLTYP` 14 bleibt; die Zugriffe A503/A523 treffen
die Preisliste 14. Eine Aenderung der Preisliste im Kundenstamm wirkt auf neue IV-Fakturen.

## 3. Frage 2: Kurstyp P fuer CHF nach EUR

### 3.1 Kurstabellen (T76, Mandant 100)

GDATU ist invertiert: 99999999 minus GDATU. 79739898 = 01.01.2026, 80009898 = 01.01.1999, 81999898 = 01.01.1800.

| Tabelle | Schluessel | Wert |
| --- | --- | --- |
| TCURR | P, EUR → CHF, ab 01.01.2026 | **UKURS 0.94** (davor 2025 0.95, 2024 0.97, 2023 1.00 usw., Reihe bis 2002) |
| TCURR | P, CHF → EUR | **kein Eintrag** |
| TCURF | P, EUR → CHF, ab 01.01.1999 | FFACT 1, TFACT 1, ABWCT leer |
| TCURF | P, CHF → EUR | **kein Eintrag** |
| TCURV | P | XINVR leer, BWAER leer, XFIXD leer, XEURO leer |
| TCURW | P | „Standardumrechnung fuer die Kostenplanung“ |
| TCURR | M, EUR → CHF, ab 01.01.2026 | UKURS 0.94 (einziger M-Satz EUR/CHF) |
| TCURF | M, EUR → CHF und CHF → EUR, ab 01.01.1999 | FFACT/TFACT 1, **ABWCT EURX** (Umleitung) |
| TCURV | EURX | BWAER EUR, XBWRL Y, XEURO X |
| TCURR | EURX, CHF → EUR, ab 01.01.2026 | UKURS 0.94- (Mengennotiz), davor 0.95-, 0.97- ... |

`XINVR` heisst laut DD04T/DOKTL „Rechnen mit invertiertem Kurs erlaubt“: fehlt der Kurs in der gewuenschten Richtung,
darf die umgekehrte Relation benutzt werden. Fuer P ist das **nicht** gesetzt. Belegt im Quelltext `LSCUNF01`
FORM `DATEN_BEREITSTELLEN`: die Gegenrichtung wird nur bei `tcurv-xinvr NE space` gelesen (Zeile 314).

### 3.2 Welche Richtung braucht die IV-Faktura?

Nur EUR → CHF (Fremdwaehrung = Belegwaehrung EUR, Hauswaehrung = `T001-WAERS` von 1100 = CHF):

- Buchungskurs: `LV60AA95` Zeilen 795 bis 816, „Umrechnungskurs bei interner Verrechnung“:
  `kurstyp = vbrk-kurst. IF kurstyp IS INITIAL. kurstyp = 'M'. ENDIF.` dann `READ_EXCHANGE_RATE` mit
  `foreign_currency = vbrk-waerk`, `local_currency = t001-waers`, `type_of_rate = kurstyp`, Datum `vbrk-kurrf_dat`
  (= `vbrk-fkdat`, Zeilen 761 bis 763). Vorher werden KURRF/KURSK fuer IV geleert (Zeilen 543 bis 551).
- Preisfindungskurs: `LV60AA95` Zeilen 881 bis 884: `IF vbrp-kursk = 0. vbrp-kursk = vbrk-kurrf.` Da `TVCPF-PFKUR`
  fuer NLCC/NLC leer ist, bleibt es dabei (der CASE ab Zeile 885 laeuft nur bei PFKUR ungleich leer).
- In der Preisfindung (SAPLV61A): `LV61AA12` FORM `KONDITIONSVORSTEP`, Zeilen 222 bis 225:
  `kurstyp = komk-kurst. IF kurstyp IS INITIAL. kurstyp = 'M'. ENDIF.` `KOMK-KURST` kommt per
  `MOVE-CORRESPONDING xvbrk TO tkomk` aus `VBRK-KURST`.
- CHF-Kondition in EUR-Beleg: `LV61AA43` FORM `XKOMV_KWERT_ERMITTELN`, „currency conversion before multiplication“
  (Zeile 130). Weil Konditionswaehrung CHF = `KOMK-HWAER`, entfaellt der direkte Weg und der Schritt Kondition →
  Hauswaehrung (Zeilen 177ff.); es bleibt „From local currency into document currency“ (Zeilen 205 bis 217): fuer
  KNTYP ungleich G `CONVERT_TO_FOREIGN_CURRENCY` mit `foreign_currency = komk-waerk` (EUR), `local_currency =
  komk-hwaer` (CHF), **`rate = komp-kursk`**, `type_of_rate = kurstyp`.
- `CONVERT_TO_FOREIGN_CURRENCY` (`LSCUNU02`, Zeilen 79 bis 84): bei `rate` ungleich 0 gilt der uebergebene Kurs; TCURR
  wird nicht gelesen. Die Faktoren holt `LSCUNF02` FORM `DATEN_BEREITSTELLEN_OHNE_TCURR` bei positivem Kurs aus TCURF
  mit `fcurr = for_curr` (EUR) und `tcurr = loc_curr` (CHF) (ab Zeile 268 `IF p_rate GT 0.`). Der Eintrag P EUR → CHF existiert.
- `LV61AA44` FORM `XKOMV_KKURS_ERMITTELN`: Konditionswaehrung = Belegwaehrung → `xkomv-kkurs = komp-kursk`
  (Zeilen 17/18, so in 0090381200 bei PR00 sichtbar); sonst `READ_EXCHANGE_RATE` Konditionswaehrung → Hauswaehrung,
  bei CHF → CHF liefert der Baustein 1 (`LSCUNU04`, `IF FOREIGN_CURRENCY = LOCAL_CURRENCY`).

**Ergebnis:** Eine IV-Faktura in EUR mit `VBRK-KURST` P findet fuer eine CHF-Kondition einen Kurs, und zwar den
P-Kurs EUR → CHF zum Fakturadatum, 2026 = **0.94**. Das fehlende CHF → EUR und das leere XINVR spielen auf diesem
Weg keine Rolle. Gaebe es fuer das Fakturadatum keinen P-Satz, nimmt `READ_EXCHANGE_RATE` den letzten gueltigen
(`gdatu GE datc`, `LSCUNF01` Zeilen 224 bis 233); ein 2027 nicht nachgepflegter P-Kurs liefe also still mit 0.94 weiter.

### 3.3 Rechenbeispiel (Material 46690, 5 ST, Kurs 0.94)

- Fall A, CHF-Liste Z000 (A504, KNUMH 0000901309): 208.90 CHF je ST. Umrechnung vor der Multiplikation:
  208.90 / 0.94 = 222.234 EUR je ST, gerundet 222.23 EUR; 5 ST = **1'111.15 EUR** (ohne Rundung des Satzes
  1'111.17 EUR; welche der beiden Zahlen SAP zeigt, ist **Vermutung**, nicht getestet).
- Preisliste 14 in EUR, Z999 (A503, KNUMH 0000953669): 89.60 EUR je ST, keine Umrechnung; 5 ST = **448.00 EUR**.
- Buchung FI: Betrag EUR × KURRF 0.94 in CHF (z. B. 448.00 EUR → 421.12 CHF).

### 3.4 Welcher Kurstyp gilt fuer den Buchungskurs KURRF? KNVV-KURST wirkt darauf.

- **Beleg:** Fuer die IV-Faktura bestimmt `VBRK-KURST` (= `KNVV-KURST` von 820, Abschnitt 2.2 Punkt 3) den Kurstyp
  von `VBRK-KURRF`; leer heisst M (`LV60AA95` Zeilen 795 bis 816). Bei normalen Kundenfakturen ist das anders: dort
  ruft `LV60AA95` Zeilen 775 bis 793 `READ_EXCHANGE_RATE` ohne `type_of_rate` auf, also mit dem Vorschlagswert M
  (`LSCUNU04` Zeile 9: `VALUE(TYPE_OF_RATE) LIKE TCURR-KURST DEFAULT 'M'`), unabhaengig vom Kurstyp.
- **Beleg:** `VBRK-KURRF` geht als Kurs in die Buchhaltung: `LV60BF00` (SAPLV60B, Schnittstelle zum Rechnungswesen)
  setzt `xacccr-kursf = vbrk-kurrf` (Zeilen 435, 694, 859, 1580, 1949). Die Daten passen dazu (BKPF-KURSF 0.94 =
  VBRK-KURRF bei drei Belegen).
- Folge: Setzt man im Kunden 820 Kurstyp P, laufen **sowohl die Preisumrechnung als auch die FI-Buchung** der
  IV-Faktura mit dem festen P-Kurs. FI rechnet nicht selbst mit M nach; ob eine spaetere Bewertung (Fremdwaehrungs-
  bewertung der offenen Posten) mit M laeuft, ist eine FI-Frage ausserhalb dieses Belegs (**Vermutung**: ja, ueber die
  Bewertungsart, nicht ueber die Faktura).
- In T76 laesst sich P gegen M nicht unterscheiden: M EUR → CHF wird ueber TCURF-ABWCT nach EURX umgeleitet
  (`LSCUNF01` FORM `ABWCT_EXIST`), EURX 2026 = 0.94, P 2026 = 0.94. Die 12 vorhandenen Fakturen (KURST leer, also M)
  zeigen deshalb dieselben 0.94 wie P. Ein Test mit Kurstyp P in T76 haette dieselben Zahlen gezeigt.

## 4. Testbelege und Aenderungen in T76

- Angelegte Testbelege: **keine** (Bestellung, Lieferung, Warenausgang, IV-Faktura nicht angelegt, weil Frage 1
  lesend eindeutig wurde).
- Aenderungen in T76: **keine** (kein Customizing, kein Kundenstamm, kein Kurstyp gesetzt).
- Rueckgaengig zu machen: nichts.
- Zugang: ausschliesslich `.tmp_sap_probe/RunSapProbe.ps1` (RFC, DPAPI-Ablage T76, nur `table-read` und
  `abap-read`). Keine GUI-Sitzung geoeffnet, P76 nicht beruehrt.

## 5. Offene Punkte fuer Ingo und Fabio

1. **Kurstyp P wirkt auch auf die FI-Buchung der IV-Faktura** (Abschnitt 3.4). Wenn Finance die Faktura weiter zum
   Mittelkurs buchen will, waere P im Kundenstamm der falsche Hebel; dann muesste der feste Kurs anders in die
   Preisfindung (z. B. eigener Kurstyp nur fuer die Kondition, Kopiersteuerung `PFKUR`, oder Kurs in der Kondition).
   Entscheid Fabio/Finance.
2. P-Kurs jaehrlich in OB08 pflegen (EUR → CHF); ohne neuen Satz laeuft der alte still weiter. CHF → EUR fuer P ist
   nicht gepflegt und XINVR fuer P nicht gesetzt; fuer die IV-Faktura nicht noetig, fuer andere Verwendungen von P
   (Kostenplanung) unveraendert.
3. *(Erledigt am 2026-11-05, Abschnitt 7.)* In P76 ist zu pruefen (nur lesend, eigener Auftrag), ob `KNVV-PLTYP` 14
   und `KNVV-KURST` dort wie in T76 stehen, ob TCURR P EUR → CHF 2026 = 0.94 ist und ob M EUR → CHF dort ebenfalls nach
   EURX umgeleitet wird. Ergebnis: alles wie in T76; M EUR → CHF hat in P76 gar keinen eigenen TCURR-Satz.
4. Wenn Z999/Z000 in ZICAA1 kommen, stehen PR00 (Stufe 011), IV01/IV02 (909/910) und VPRS weiter im Schema; heute
   kommt der Preis der IV-Fakturen aus PR00. Reihenfolge, Ausschluss oder Bedingung sind beim Schemaentwurf zu klaeren.
5. Woher die 12 IV-Testfakturen in T76 stammen (MEEY_HAST, FPA, MEV, Juli bis Oktober 2026), ist offen; sie koennen
   als Vorlage fuer spaetere Tests dienen (Bestellungen 45148401 ff., Lieferungen NLCC 80432766 ff.).

## 6. Kandidaten fuer LEARNINGS

- `table-read` meldete zweimal `TABLE_WITHOUT_DATA`, weil in `--fields` ein Feld stand, das die Tabelle nicht hat
  (`KNVV-AEDAT`, `T685A-KOZGF`); ohne das Feld kamen die Daten. Wiederholung des Musters vom 19.10./03.11.: vor breiten
  `--fields` die Felder per `DD03L` pruefen.
- Bei Kursfragen in T76 die Umleitung `TCURF-ABWCT` beachten (M EUR/CHF → EURX): der M-Satz in TCURR ist dann nicht der
  gelesene Kurs, und gleiche Werte in M und P machen einen Test wertlos.
- Bei interner Verrechnung (IV) gelten fuer Kopfdaten andere Wege als bei Kundenfakturen: Kurstyp aus KNVV wirkt dort
  auf den Buchungskurs. Fachaussagen zu SD-Kursen nur mit Blick auf `LV60AA95` treffen.

## 7. P76 lesend 2026-11-05

Freigabe Ingo 2026-11-05 („mache punkt 1“): nur `KNVV` (820/1100/01/01), `TCURR`, `TCURF`, `TCURV` fuer die Kurstypen
P, M und EURX, EUR/CHF. Agent sap-t76.

**Zugang:** angemeldete P76-GUI von Ingo (P76/100, Anwendungsserver `trasp76s`), eigene neue Sitzung
`/app/con[1]/ses[2]` ueber `.tmp_sap_probe/P76Lesen.vbs neu:SE16`, Ingos Sitzung `ses[0]` (SE80) unberuehrt.
Transaktionen SE16 (Listen und Einzelanzeige) und einmal SE16N (KNVV, Ankreuzfeld „Eintraege pflegen“ nicht
angefasst). Kein Passwort, kein Anmeldebild, keine Aenderung, nichts gesichert, keine Caches. Am Ende mit
`P76SitzungSchliessen.vbs` geschlossen; danach zeigte `SapGuiP76Hilfe.vbs info` nur noch `ses[0]` SE80. **Geschlossen: ja.**
Nebenwirkung: SE16 generiert beim ersten Aufruf je Tabelle seinen Selektionsreport (`/1BCDWB/DBKNVV`, `DBTCURR`,
`DBTCURF`, `DBTCURV`; Meldung „Programm ... existiert noch nicht in Bibliothek“). Das ist das normale Verhalten von SE16,
keine Daten- oder Customizingaenderung.

**Feldnamen:** Der T76-Abgleich in `DD03L` per RFC wurde vom Auto-Modus abgelehnt und nicht wiederholt. Die Feldnamen sind
stattdessen in P76 belegt: Selektionsbilder von SE16 (TCURR: KURST, FCURR, TCURR, GDATU, UKURS, FFACT, TFACT; TCURF
zusaetzlich ABWCT, ABWGA; TCURV: KURST, XINVR, BWAER, XBWRL, GKUZU, BKUZU, XFIXD, XEURO, XNORL) und Einzelanzeige
„Tabelle KNVV anzeigen“ mit den Feldern `KNVV-PLTYP`, `KNVV-KURST`, `KNVV-WAERS`, `KNVV-KALKS`.

GDATU invertiert: 79739898 = 01.01.2026, 79749898 = 01.01.2025, 80009898 = 01.01.1999, 81999898 = 01.01.1800.

### 7.1 Werte P76 gegen T76

| Punkt | Schluessel | P76 (2026-11-05) | T76 (Abschnitt 1.1, 3.1) | gleich? |
| --- | --- | --- | --- | --- |
| KNVV | 820 / 1100 / 01 / 01 | PLTYP **14**, KURST **leer**, WAERS **EUR**, KALKS **1** (dazu INCO1 DAP Wolfurt, ERNAM I001068, ERDAT 28.11.2002) | PLTYP 14, KURST leer, WAERS EUR, KALKS 1 | ja |
| TCURR | P, EUR → CHF | 19 Saetze; 01.01.2026 **0.94000**, 01.01.2025 0.95, 2024 0.97, 2023 1.00, 2022 1.05 ... bis 01.12.2002 1.45; FFACT/TFACT 0 | 2026 0.94, 2025 0.95, 2024 0.97, 2023 1.00 | ja |
| TCURR | P, CHF → EUR | **kein Eintrag** | kein Eintrag | ja |
| TCURR | M, EUR → CHF | **kein Eintrag** (auch M mit FCURR EUR und beliebiger Zielwaehrung: keiner) | ein Satz 01.01.2026 0.94 | **nein**, ohne Wirkung (Umleitung, siehe TCURF) |
| TCURR | M, CHF → EUR | **kein Eintrag** (M mit FCURR CHF nur DKK, GBP, GRD, JPY, NOK, SEK, USD) | nicht gelesen | – |
| TCURR | EURX, EUR → CHF | **kein Eintrag** | nicht gelesen | – |
| TCURR | EURX, CHF → EUR | 18 Saetze; 01.01.2026 **0.94000-** (Mengennotiz, 1 EUR = 0.94 CHF), 2025 0.95-, 2024 0.97-, 2023 1.00-, 2022 1.05- ... 01.01.1999 1.62232- | 2026 0.94-, davor 0.95-, 0.97- | ja |
| TCURF | M, EUR → CHF | ab 01.01.1999: FFACT 1, TFACT 1, **ABWCT EURX**; ab 01.01.1800: 1 / 1, ABWCT leer | ab 01.01.1999 1 / 1, ABWCT EURX | ja |
| TCURF | M, CHF → EUR | ab 01.01.1999: 1 / 1, **ABWCT EURX**; ab 01.01.1800: 1 / 1, ABWCT leer | ab 01.01.1999 1 / 1, ABWCT EURX | ja |
| TCURF | P, EUR → CHF | ab 01.01.1999: 1 / 1, ABWCT **leer** | ab 01.01.1999 1 / 1, ABWCT leer | ja |
| TCURF | P, CHF → EUR | **kein Eintrag** | kein Eintrag | ja |
| TCURV | M | XINVR leer, BWAER leer, XBWRL leer, XEURO leer („Standardumrechnung zum Mittelkurs“) | nicht gelesen | – |
| TCURV | P | XINVR **leer**, BWAER leer, XFIXD leer, XEURO leer („Standardumrechnung fuer die Kostenplanung“) | XINVR leer, BWAER leer | ja |
| TCURV | EURX | BWAER **EUR**, XBWRL **Y**, XEURO **X**, XINVR leer („EWU-Vorschrift, variable Kurse“) | BWAER EUR, XBWRL Y, XEURO X | ja |

Ueber die Freigabe hinaus gelesen: `TCURF` fuer EURX, EUR → CHF und CHF → EUR (je ein Satz ab 01.01.1800, 1 / 1, ABWCT
leer). Das lag in derselben Abfrageschleife; es aendert nichts am Ergebnis und ist hier offen genannt.

P- und EURX-Reihe laufen seit 2012 parallel: gleiche Stichtage (jeweils 01.01.) und gleiche Werte (2026 0.94, 2025 0.95,
2024 0.97, 2023 1.00, 2022 1.05, 2018 1.10, 2016 1.05, 01.02.2015 1.00). Unterschiede nur davor (P 09.11.2011 1.20,
EURX 01.01.2012 1.20; EURX 31.12.2002 1.45330 und 1999 1.62232 ohne P-Gegenstueck).

### 7.2 Kernfrage: Rechnet eine IV-Faktura an 820 in P76 heute schon mit 0.94? Ja (aus den Tabellen belegt).

Rechenweg mit den P76-Werten, Logik aus T76 (Abschnitt 3.2 und 3.4):

1. `KNVV-KURST` von 820 ist in P76 leer → `VBRK-KURST` leer → `LV60AA95` setzt fuer den Buchungskurs der IV-Faktura
   Kurstyp **M** und liest EUR → CHF zum Fakturadatum.
2. `TCURF` M EUR → CHF, gueltig ab 01.01.1999, hat `ABWCT` EURX → `READ_EXCHANGE_RATE` rechnet mit Kurstyp **EURX**
   (`LSCUNF01` FORM `ABWCT_EXIST`, in T76 gelesen).
3. EURX hat Basiswaehrung EUR; gepflegt ist CHF → EUR, 01.01.2026 = 0.94- (Mengennotiz). Daraus folgt 1 EUR = **0.94 CHF**.
   Dass P76 keinen direkten M-Satz EUR → CHF hat, spielt wegen der Umleitung keine Rolle.
4. `VBRK-KURRF` = 0.94, und weil `TVCPF-PFKUR` fuer NLCC/NLC leer ist, wird `VBRP-KURSK` = `VBRK-KURRF` = 0.94
   (Abschnitt 3.2). Die CHF-Kondition wird also mit 0.94 in EUR umgerechnet, die FI-Buchung laeuft mit 0.94.
5. Mit Kurstyp P im Kunden 820 waere es P EUR → CHF 01.01.2026 = 0.94 (TCURF P ohne Umleitung) – **dieselbe Zahl**.

Beispiel (Material 46690, wie Abschnitt 3.3): Z000 208.90 CHF je ST, 5 ST. Mit M/EURX: 208.90 / 0.94 = 222.23 EUR je ST,
5 ST = 1'111.15 EUR; mit P: 208.90 / 0.94 = 222.23 EUR, 5 ST = 1'111.15 EUR. Kein Unterschied. FI: 1'111.15 EUR × 0.94
= 1'044.48 CHF in beiden Faellen.

**Folge:** Fuer 2026 ist Kurstyp P im Kunden 820 in P76 unnoetig; er aendert weder den Preis noch die Buchung. Er wuerde
nur dann einen Unterschied machen, wenn Finance die P-Reihe und die EURX-Reihe einmal verschieden pflegt. Umgekehrt
bedeutet das: Der „Mittelkurs“ M ist fuer EUR/CHF in P76 faktisch der jaehrlich feste EURX-Kurs, nicht ein Tageskurs.

**Vermutungen (nicht belegt):**

- Dass die echten IV-Fakturen an 820 in P76 `VBRK-KURRF` 0.94 tragen, ist aus den Kurstabellen abgeleitet. VBRK/BKPF in P76
  waren nicht freigegeben und wurden nicht gelesen.
- Der Programmablauf (`LV60AA95`, `LSCUNF01`, `LV61AA43`) ist in T76 gelesen; dass P76 denselben Stand ohne eigene
  Erweiterung hat, ist angenommen (SAP-Standard, in T76 ohne Z/Y-Erweiterung), aber in P76 nicht geprueft.
- Ohne neuen EURX-Satz zum 01.01.2027 laeuft M wie P still mit dem letzten Satz (0.94) weiter (Abschnitt 3.2 letzter
  Absatz); beide Reihen muessen also jaehrlich gepflegt werden, wenn sich der Planungskurs aendert.

## 8. Staffelbasis Bestellmenge (06.11.)

Auftrag Ingo 2026-11-06 („ja mach die umsetzung“) zu Fabios Vorschlag (Mail 06.11. 12:06, Blatt 8 „Staffelbasis“ in
`IC_Umsetzung_V1.xlsx`). Nur T76/100, Agent sap-t76. P76 nicht gelesen und nicht angefasst, nichts freigegeben.

### 8.1 Befund (Fall P76, nur aus der Beschreibung)

Bestellung 1200 45152176/10, Material 54150, 100 ST zu 30.30 EUR (PR00 Kunde 820, Staffel ab 100 ST). Lieferung 1100
80444806 mit 50 ST, IV-Faktura 90390967 nahm die Staffel ab 50 ST: 118.69 EUR/ST, 5'934.50 statt 1'515.00 EUR. Ursache:
Die Staffelbasis der IV wird mit der fakturierten (gelieferten) Menge gesucht, die Bestellung 1200 rechnet mit der
Bestellmenge.

Beleg im Standard (T76, SAPLV61A): `LV61AA73` FORM `STAFFELBASIS_ERMITTELN`, Mengenstaffel (`KZBZG` C):
`xmenge = komp-mgame`, bei abweichender Staffeleinheit `MATERIAL_UNIT_CONVERSION` in `xkomv-konms`, dann
`xkomv-kstbs = xmenge`. `KOMP-MGAME` ist in der Faktura die fakturierte Menge.

### 8.2 Mechanik, im System nachgesehen

| Punkt | Beleg |
| --- | --- |
| VOFM-Gruppe Staffelbasis | `TFRM`/`TFRMT` Gruppe `PFRS` (Standard 001, 002, 012, 023, 043, 060, 202, 425, 500, 501; 425 = `FV62A425` CWM). VOFM-Menue *Formeln > Staffelbasis* zeigt dieselbe Liste |
| Includes | Standard `FV62Annn` in `FV62ANNN`, Kunde `RV62Annn` in `RV62ANNN` („Formeln Staffelbasis“, vorher leer); beide in `SAPLV61A` (`D010INC`) |
| Schnittstelle | `FORM frm_staffelbas_nnn.` ohne Parameter; Aufruf `LV61AA55` Zeilen 625 bis 633: `xkwert = xkomv-kstbs. frm_staffelbas-nr = xkomv-kofrs. PERFORM (frm_staffelbas) IN PROGRAM saplv61a IF FOUND. xkomv-kstbs = xkwert.` (gleich bei 837 und 1024). Verfuegbar: `KOMK`, `KOMP`, `XKOMV`, `XKWERT` (`LIKE komv-kwert`, `LV61ATOP` Zeile 145) |
| Zahlenformat | `TRDIR-FIXPT` fuer `SAPLV61A` leer (ohne Festpunktarithmetik). `KOMV-KSTBS` CURR 15,2 erhaelt `KOMP-MGAME` QUAN 13,3 ungewandelt; die Staffelbasis traegt also Tausendstel (vgl. `FV62A023`, `FV62A202` `kstaf * 1000`). Die Routine fuehrt die Bestellmenge deshalb in `EKPO-MENGE`/`KOMP-MGAME` (3 Dezimalen) und verwendet keine Literale. Ergebnis im Test: `KSTBS` 10.000 bei 10 ST Bestellmenge, also richtig skaliert |
| V/06-Feld | `T685A-KOFRS` „Routine fuer alternative Berechnung der Staffelbasis“, Bild „Staffelroutine“. PR00 vorher `0000000` |
| Kontext Faktura | `KOMP-VGBEL/VGPOS` = Lieferung/Position, `KOMP-AUBEL/AUPOS` = Bestellung (VBRP 0090381200: AUBEL 0045148482/000010); `KOMK-KALSM` aus `VBRK-KALSM` |
| ZICAA1 | nur `KALVG` I (`T683V` 1100/01/01, 1100/01/02, 1200/01/01), Fakturaarten IG, IG2, IGA, IGS, IV, IVA, IVS (`TVFK`), keine Auftragsart. `TA` hat `KALVG` A → ZVAA01 (`T683V` 1100/01/01/A/1) |
| Nummer | 601 frei (keine `PFRS` 6xx in `TFRM`) |
| Zugangsschluessel | **keiner verlangt.** VOFM zeigte nur zwei Informationen („R3TR PROG RV62A601 ist generiert, Aenderungen sind moeglich“, „Fuehren Sie nur dringende Reparaturen in fremden Namensraeumen durch“). Der Editor laeuft als Modifikation (Reparatur) |
| Generierung | VOFM *Bearbeiten > Aktivieren* hat `RV62ANNN` selbst neu erzeugt (`INCLUDE RV62A601 . "IC Bestellmenge`), `TFRM PFRS 601 AKTIV X`, `GNDAT 20261106 GNZEI 132003`. Ein eigener RV80HGEN-Lauf war in T76 nicht noetig. `RV62ANNN`/`FV62ANNN` stehen in `E071` unter `RV80HGEN_TADIR_CORE` |

### 8.3 Routine 601 „IC Bestellmenge“

Quelltext: `saptasks/ic_preisfindung/staffelbasis_601.abap` (aktiv in T76, per `abap-read RV62A601` zurueckgelesen,
inhaltsgleich; vorher gab es `RV62A601` nicht). Ablauf: nur `KOMK-KALSM = ZICAA1` und `XKOMV-KZBZG = C`;
`KOMP-VGBEL/VGPOS` → `LIPS` (nur `VGTYP V`) → `EKPO` (`EBELP` aus `LIPS-VGPOS` NUMC 6 → NUMC 5, `LOEKZ` leer) →
`MENGE`/`MEINS`. Zieleinheit `XKOMV-KONMS`, sonst `KOMP-VRKME`; weicht `EKPO-MEINS` ab, `MD_CONVERT_MATERIAL_UNIT`
(MARM), bei Fehler Standard. Neue Staffelbasis = groesserer Wert aus umgerechneter Bestellmenge und bisheriger
Staffelbasis. Jede fehlende Stufe (keine Lieferung, keine Bestellung, Auftrag statt Faktura, Simulation) endet mit
`CHECK` und laesst die Staffelbasis unveraendert.

Aktivierung: Include gesichert und ueber den Dialog „Inaktive Objekte“ nur `RV62A601` aktiviert (`Z_REICHW` stand
daneben und blieb unberuehrt), kein Fehlerdialog. `abap-check SAPLV61A`: Syntax OK, dieselben neun Warnungen wie vorher
(keine neue).

Hinweis zur Arbeitsweise: VOFM *Aktivieren* aktiviert nur den TFRM-Eintrag und erzeugt `RV62ANNN`; das Include selbst
bleibt inaktiv („aktiv (ueberarbeitet)“, aktive Fassung = leerer Rumpf), bis es im Editor mit Strg+F3 aktiviert wird.

### 8.4 V/06 und Auftraege

- V/06 PR00: Staffelroutine `601` (Text „IC Bestellmenge“) eingetragen. Rueckgelesen `T685A` V/PR00: `KZBZG C`,
  `KOFRS 0000601`. Andere Konditionsarten mit Staffelroutine (unveraendert): KP03 23, LP03 (M) 23, LCFR 43, PBBS 202,
  UTXD 500.
- Workbench-Auftrag **T76K912853** „IC Staffelbasis Bestellmenge VOFM 601 (ZICAA1, PR00)“, Ziel P76, Aufgaben
  T76K912854 (Reparatur, `R3TR PROG RV62A601`, gesperrt) und T76K912855 (`TABU TFRM`, `TABU TFRMT`; `E071K`
  Schluessel `PFRS601` bzw. `DPFRS601`).
- Customizing-Auftrag **T76K912856** „IC Staffelbasis: V/06 PR00 Staffelroutine 601“ (T000 Mandant 100 `CCCORACTIV 1`,
  das Sichern verlangte einen Customizing-Auftrag), Aufgabe T76K912857 `VDAT VV_T685A_VA` mit Schluesseln T685/T685A/T685T
  `100V PR00`. Vorgeschlagen war der fremde Auftrag T76K912750 „DEMO PS T76 Projekte aktiv - NICHT FREIGEBEN“; nicht benutzt.
- Beide Auftraege offen, nicht freigegeben. `RV62ANNN` liegt in keinem der beiden; im Zielsystem muss es nach dem
  Import neu erzeugt werden (Abschnitt 8.6).

### 8.5 Tests in T76 (alle Belege KOI, 06.11.2026)

Testmaterial 2367 (P40 PRESSOSTAT), weil 54150 in T76 keine PR00-820-Staffel hat und 46690 in 1100/0001 keinen Bestand.
PR00 A305 1100/01/820/2367, KNUMH 0000970128, gueltig ab 01.11.2026, 166.00 EUR je 1 ST, Staffel (KONM, `KONMS` ST):
ab 1 ST 166.00, ab 2 ST 151.30, ab 5 ST 134.00, ab 10 ST 131.40, ab 25 ST 129.00. Kein Konditionssatz angelegt.
Bestand 1100/0001 vorher 8 ST (WM-Lagernummer 110, Quants 1G1).

Kette wie bei den Altbelegen (FPA, 45148482): ME21N NB, Lieferant 70953, EKORG 1200, EKGRP 120, BUKRS 1200, Werk 1200
(RESWK 1100 automatisch), VL10B (Versandstelle 1100), VL02N Menge, LT03 (Kommimenge 2) und LT12 (quittieren = WA),
VF01 Fakturaart IV. Alle IV mit `KALSM ZICAA1`, Kunde 820, EUR, FI-Beleg in 1100 (BKPF `KURSF 0.94`).

| Fall | Belege | PR00 Staffelbasis (`PRCD_ELEMENTS-KSTBS`) | PR00 Preis | Menge (`KAWRT`) | Wert | Ergebnis |
| --- | --- | --- | --- | --- | --- | --- |
| Vorher (ohne Routine), Teillieferung | Bestellung 45148502/10 ueber 10 ST; Lieferung 80432793 3 ST (TA 690454); IV **90381201** (13:12) | 3 | 151.30 EUR (ab 2) | 3 ST | **453.90 EUR** | Standard: Staffel nach Liefermenge (der P76-Fehler) |
| Nachher, Restteil derselben Bestellung (Muster S1/S2/S3) | Lieferung 80432794 3 ST (TA 690455); IV **90381202** (13:31) | **10** | **131.40 EUR** (ab 10) | 3 ST | **394.20 EUR** | OK: Staffel nach Bestellmenge, Wert mit fakturierter Menge |
| Nachher, Komplettlieferung (S4) | Bestellung 45148503/10 ueber 2 ST; Lieferung 80432795 2 ST (TA 690456); IV **90381203** | 2 | 151.30 EUR | 2 ST | **302.60 EUR** | OK: unveraendert (max(2, 2) = 2) |
| Gegenprobe Kundenauftrag (S5) | VA01 TA, Auftraggeber 820, 2367 3 ST, **nicht gesichert** (VBAK ERNAM KOI 06.11. leer) | – | 151.30 EUR | 3 ST | 453.90 EUR | OK: ZVAA01 (TA/KALVG A, KALKS 1) unveraendert |

Rechnung zum Fall nachher: Bestellmenge 10 ST, fakturiert 3 ST, Staffelbasis max(10, 3) = 10 → Stufe ab 10 = 131.40
EUR/ST → 3 × 131.40 = 394.20 EUR (vorher 3 × 151.30 = 453.90 EUR). Preiseinheit `KPEIN` 1, Einheit ST. Uebertragen auf
den P76-Fall: 50 ST × 30.30 = 1'515.00 EUR statt 50 × 118.69 = 5'934.50 EUR (Rechnung, nicht getestet).

ST22 06.11.2026: ein einziger Kurzdump, 08:02:12 KOI `CALL_FUNCTION_NOT_REMOTE` in `SAPLSIFP`, also vor dieser Arbeit
(ab etwa 12:40) und ohne Bezug. Keine Dumps aus VOFM, VF01, VA01.

Nicht getestet (Stand 13:40; S6 und S7 am Nachmittag weiter bearbeitet, Ergebnis in Abschnitt 8.8):

- S6 Bestellung in anderer Einheit als die Staffel (Zweig `MD_CONVERT_MATERIAL_UNIT`): in T76 sind alle beteiligten
  Einheiten ST; nur im Quelltext geprueft. *(Abschnitt 8.8: bestaetigt nicht testbar in T76.)*
- S7 MIRO 1200 (IDoc INVOIC) ohne Preisdifferenz nach WE: nicht gemacht. *(Abschnitt 8.8: IDoc-Kette belegt, WE
  gebucht, Rechnung noch nicht gebucht.)*
- Fakturaerzeugung automatisch beim Warenausgang (P76-Ablauf laut Fabio); in T76 per VF01 von Hand. Die Preisfindung ist
  dieselbe (Kopiersteuerung NLCC/NLC Preisfindungsart B, Abschnitt 1.4).
- IV-Storno (IVS) und interne Gutschrift (IG): nicht getestet; ohne Lieferbezug mit `VGTYP V` greift die Routine nicht.
- Neupreisfindung einer bestehenden IV per VF02: nicht versucht.
- Der Schutz `KOMK-KALSM = ZICAA1` selbst ist nicht isoliert getestet: Die Gegenprobe S5 (Kundenauftrag) zeigt nur „kein
  Schaden“, dort fehlt `KOMP-VGBEL` ohnehin. Eine Kundenfaktura mit Lieferbezug `VGTYP V` ausserhalb ZICAA1 gibt es in
  T76 nicht.

Testbelege bleiben in T76 stehen (Bestellungen 45148502, 45148503; Lieferungen 80432793, 80432794, 80432795; TA 690454
bis 690456; IV 90381201 bis 90381203 mit FI-Belegen in 1100; 8 ST 2367 aus 1100/0001 ausgebucht).

### 8.6 Offen und Aufgaben fuer Ingo

1. *Erledigt 2026-11-10 (Abschnitt 8.9):* Freigabe und Import nach P76: T76K912853 (Workbench) und T76K912856 (Customizing) **zusammen**; nur V/06 ohne Routine
   liefe in P76 ins Leere (`PERFORM ... IF FOUND`), nur die Routine ohne V/06 waere wirkungslos.
2. Nach dem Import in P76 pruefen: `RV62ANNN` enthaelt `INCLUDE RV62A601`, `TFRM PFRS 601 AKTIV X`, `RV62A601` aktiv;
   sonst in P76 VOFM *Formeln > Staffelbasis* 601 aktivieren bzw. RV80HGEN laufen lassen (macht Ingo).
3. *Erledigt 2026-11-10 (Abnahme Fabio, Abschnitt 8.9):* Vor dem Import mit Fabio den Fall S7 (MIRO 1200 gegen die IV) und die automatische Faktura beim WA in einer
   Abnahme pruefen; Einheiten heute immer ST (Fabio, zu verifizieren). *(Stand S7 und offene Schritte: Abschnitt 8.8.)*
4. Spaeter (V1): dieselbe Routine bei Z999/Z002 eintragen, erst wenn diese in ZICAA1 stehen. Alternative laut Fabio
   (eigene IC-Kopie von PR00, rund 950 PR00-Saetze 820 umziehen) nicht verfolgt.
5. SAPLV61A ist in T76 neu generiert; offene SD-Sitzungen anderer Benutzer laden die neue Fassung beim naechsten Aufruf.
6. T76K912854 ist eine Reparatur (`E070-TRFUNCTION R`). Beim Freigeben fragt SAP nach dem Zuruecksetzen des
   Reparaturkennzeichens; das ist bei VOFM-Routinen der Normalfall.
7. Rueckbau in T76, falls Fabio anders entscheidet: V/06 PR00 Staffelroutine leeren (Customizing-Auftrag) und VOFM 601
   deaktivieren; die Testbelege bleiben.

### 8.7 Kandidaten fuer LEARNINGS

- VOFM *Aktivieren* aktiviert das Kunden-Include nicht; die aktive Fassung blieb der leere Rumpf, `abap-read` zeigte es.
  Nach VOFM immer das Include im Editor aktivieren und per `abap-read` zuruecklesen.
- `RFC_READ_TABLE` mit `KOFRS <> '0000000'` lieferte `TABLE_WITHOUT_DATA`, obwohl sechs Saetze existieren; mit `>` kamen
  sie. Bei `TABLE_WITHOUT_DATA` nach einem `<>`-Filter gegenpruefen.
- `SapGuiTabelleSuchen.vbs` setzt `txt` vor den Feldnamen und sucht fest in Spalte 5; mit `txtV_T685A-KSCHL` lief es
  ohne Treffer durch alle Bildlaufpositionen. In SM30-Sichten „Positionieren...“ (`btnVIM_POSI_PUSH`) nehmen.
- Lieferungen aus 1100/0001 sind WM-pflichtig (Lagernummer 110): WA nur ueber LT03 + LT12 (Kommimenge 2), nicht direkt
  in VL02N.
- Bash-Heredoc mit vielen Backticks und Apostrophen brach ab (wiederholt, Regel 2026-10-12); Doku-Abschnitte mit dem
  Edit-Werkzeug anhaengen.

### 8.8 Tests S6/S7 und Gegenprobe (06.11., Nachmittag)

Auftrag Ingo 2026-11-06 („ja mach staffelbasis“): S6 und S7 in T76 durchfuehren, nichts freigeben, P76 nicht anfassen.
Agent sap-t76. Zugang: RFC `RunSapProbe.ps1` (DPAPI-Ablage T76, nur lesen) und eine eigene GUI-Sitzung
`/app/con[0]/ses[2]` (ueber `SapGuiT76Hilfe.vbs neu`); Ingos Sitzung `ses[0]` (SESSION_MANAGER) nicht benutzt. Kein
Passwort, keine Aenderung an Customizing, Stammdaten oder Quelltext. Routine und V/06 unveraendert (zurueckgelesen
15:40: `T685A` V/PR00 `KZBZG C`, `KOFRS 0000601`; `TFRM PFRS 601 AKTIV X`; `E070` T76K912853/54/55/56/57 alle
`TRSTATUS D`, also offen).

#### S6 Bestellmenge in anderer Einheit: nicht testbar in T76

- In T76 gibt es fuer Kunde 820 nur sieben gueltige PR00-Saetze (`A305` KAPPL V, PR00, 1100/01/820, `DATBI` ab
  06.11.2026): 2366, 2367, 2537, 39101, 46689, 46690, 63502 (`KONH-ERNAM`: FPA 01. und 04.11.2026, fuer 39101 und
  63502 MEEY_HAST 03.07.2026, also nach der Datenkopie angelegte Testsaetze).
- Keines dieser Materialien hat eine alternative Mengeneinheit: `MARM` zeigt je Material nur `ST` 1/1, `MARA-MEINS`
  ST, `MARA-BSTME` leer, `MVKE` 1100/01 `VRKME` und `SCHME` leer.
- Ein Material mit alternativer Einheit (z. B. H05039: `MARA-MEINS` KAR, `MARM` ST 1/600) hat keine PR00 fuer 820 und
  in 1100/0001 `LABST` 0. 2367 hat nach den Tests von 13 Uhr in 1100/0001 `LABST` 0.
- Angelegt wurde deshalb nichts (keine MARM-Einheit, keine Kondition, keine Bestandsbuchung).
- Lesend belegt: Die Routine rechnet die Bestellmenge `EKPO-MENGE` aus `EKPO-MEINS` in die Staffeleinheit
  `XKOMV-KONMS` um, ohne Staffeleinheit in `KOMP-VRKME` (`staffelbasis_601.abap` Zeilen 70 bis 96). Das ist dieselbe
  Zieleinheit wie im Standard: `LV61AA73` FORM `STAFFELBASIS_ERMITTELN`, Zeilen 203 bis 219 (`xmenge = komp-mgame`, bei
  `komp-vrkme NE xkomv-konms` und `konms` nicht leer Umrechnung nach `xkomv-konms`). Der Baustein
  `MD_CONVERT_MATERIAL_UNIT` hat die Schnittstelle `I_MATNR` (MARA-MATNR), `I_IN_ME`/`I_OUT_ME` (MARA-MEINS), `I_MENGE`
  und `E_MENGE` (EKPO-MENGE, QUAN 13,3) laut `FUPARAREF`, passt also ohne Typwandlung zu den Variablen der Routine.
  Er rechnet laut LEARNINGS 2026-10-29 ueber MARM (UMREZ/UMREN), anders als `UNIT_CONVERSION_SIMPLE`.
- Einzeltest des Bausteins: per RFC abgewiesen („Incompatible Call Rejected, see note 2295840“, obwohl `TFDIR-FMODE`
  R), nicht wiederholt. Der geplante Ersatz, SE37-Testumgebung mit H05039, 2 KAR nach ST (erwartet 1'200 ST), war nicht
  mehr moeglich, weil die GUI-Verbindung um 15:27 abriss (siehe S7). **Vermutung:** Ergebnis 1'200.000.
- Rechenbeispiel, wie die Routine es tun wuerde (nicht getestet): Bestellung 2 KAR eines Materials mit 1 KAR = 600 ST,
  Staffel in ST ab 1/500/1'000, Lieferung und Faktura 600 ST. Standard: Staffelbasis 600 ST → Stufe ab 500. Routine:
  2 KAR → 1'200 ST, max(1'200, 600) = 1'200 → Stufe ab 1'000; Wert = Preis der Stufe ab 1'000 × 600 ST.

#### S7 MIRO 1200 zur IV 90381202: IDoc-Kette belegt, WE gebucht, Rechnung noch nicht gebucht

**Weg in T76 (im System belegt):** Die IV-Faktura erzeugt selbst die Eingangsrechnung in 1200, eine MIRO von Hand ist
nicht der T76-Weg.

| Schritt | Beleg | Quelle |
| --- | --- | --- |
| Nachricht an der IV | `NAST` V3 0090381202 `ZRD4`, Partner 820 (RE), `NACHA` 6 (EDI), `VSTAT` 1, 06.11. 13:31:40 | NAST |
| IDoc ausgehend | 0000000015343208, INVOIC02, an LS EXPOWIN, Status 03 | EDIDC |
| IDoc eingehend | 0000000015343209, INVOIC01, `MESCOD` MM, Sender LI 0000070953, Empfaenger KU 0000000820, Status 53, Meldung M8 661 „Beleg Nr. 0051128478 wurde hinzugefuegt“ | EDIDC, EDIDS, T100 |
| Rechnungsbeleg 1200 | `RBKP` 0051128478/2026, BLART RE, BUKRS 1200, Lieferant 70953, `XBLNR` 0090381202, `RMWWR` 394.20 EUR, `KURSF` 1.00000, `TCODE` MR1M, `IVTYP` 4 (EDI), **`RBSTAT` 3 „Fehlerhaft“** | RBKP, DD07T |

Dasselbe gilt fuer die anderen beiden Testfakturen (90381201 → IDoc 15343204 → 0051128477, 453.90 EUR; 90381203 → IDoc
15343215 → 0051128479, 302.60 EUR), alle `RBSTAT` 3. Von den aelteren Fremdtests sind 0051128466 und 0051128473
(MEEY_HAST, Juli/August) gebucht (`RBSTAT` 5), 0051128474 bis 0051128476 (FPA, 25.10.) ebenfalls „fehlerhaft“.

**Warum „fehlerhaft“:** Die Rechnungsposition (`RBDRSEG` 0051128478/000001, Bestellung 45148502/00010) traegt
`KZMEK` 1 „Ungeklaerter Fehler: Rechnung vorerfassen“ (Domaene `KZMEKORR`, gleich bei allen sechs fehlerhaften
Rechnungen 0051128474 bis 479), Betrag und Menge der Position sind leer (`RBDRSEG` MENGE 0, WRBTR 0). Im MIR4-Bild der
Vorher-Rechnung 0051128477 standen die IDoc-Werte in `DRSEG-RBWWR` 453.90 und `DRSEG-RBMNG` 3, `DRSEG-WEMNG` 0,
Bestellnetto `DRSEG-NETPR` 51.51. Das Pruefprotokoll in MIR4
(Springen > Meldungen) zeigt genau einen Fehler: „Rechnung enthaelt noch mindestens eine ungeklaerte Korrektur“.
**Vermutung zur Ursache:** fehlender Wareneingang beim Eingang des IDocs. Dafuer spricht FPA 0051128476: Bestellpreis
57.77 = IV-Preis 57.77, trotzdem fehlerhaft, IDoc 25.10. 19:10:16, WE erst 19:13:17 (`MKPF` 5001255586). Die
urspruengliche Einzelmeldung der IDoc-Verarbeitung ist in EDIDS nicht gespeichert.

**Wareneingang (gebucht):** Die Bestellposition hat Bestaetigungssteuerung `EKPO-BSTAE` 0004 (Anlieferung); MIGO
„Wareneingang Auslieferung“ 80432794 meldete deshalb „Fuer Einkaufsbeleg 45148502 00010 ist kein Wareneingang
moeglich“. Die Anlieferung entsteht aus der Nachricht `SPED` der Auslieferung (`NAST` V2, `VSZTP` 3, bei meinen
Lieferungen `VSTAT` 0; bei FPA ueber den Transport verarbeitet). Ablauf:

1. VL71, Nachrichtenart SPED, Medium 8, Lieferung 80432794 → „Alle selektierten Nachrichten wurden erfolgreich
   verarbeitet“ (`NAST` VSTAT 1, 15:19:50). Ergebnis Anlieferung **0180000073** (`VBFA` Typ 7; `EKES` 45148502/00010
   LA 3 ST).
2. VL32N 0180000073 (2367, 3 ST, Werk 1200, Lagerort 0001, PSTYV ELN), „Wareneingang buchen“ → Materialbeleg
   **5001255589**/2026 (`MKPF` TCODE2 VL32N, KOI, 15:21:01; `MSEG` 101, 3 ST, 154.53 EUR, BUKRS 1200; `EKBE` VGABE 1,
   154.53 EUR). Werk 1200 ist nicht WM-gefuehrt (`T320` ohne Satz fuer 1200).

Die Lieferung 80432793 (Vorher-IV 90381201) hat keine Anlieferung und keinen WE; ihre 3 ST stehen weiter im Transit.

**Preisvergleich (gerechnet aus den Belegen):**

| | Vorher: IV 90381201 / Rechnung 0051128477 | Nachher: IV 90381202 / Rechnung 0051128478 |
| --- | --- | --- |
| IV PR00 (`PRCD_ELEMENTS`, KNUMV 748082 / 748083) | 151.30 EUR je 1 ST, `KSTBS` 3 | 131.40 EUR je 1 ST, `KSTBS` 10 |
| Rechnungsbetrag 3 ST | 453.90 EUR | 394.20 EUR |
| Bestellung PB00 (KNUMV 1000158003, KNUMH 970133, Staffel nach Bestellmenge, `KSTBS` 10) | 131.40 EUR | 131.40 EUR |
| Differenz auf Stufenebene | +19.90 EUR je ST, +59.70 EUR | **0.00** |
| Bestellnetto T76 (PB00 − RL01 44 % − ZRL2 30 %) | 51.51 EUR je ST (515.09 / 10) | 51.51 EUR je ST |
| WE-Wert 3 ST (5001255589) | (kein WE) | 154.53 EUR |
| Differenz gegen T76-Bestellnetto | 453.90 − 154.53 = 299.37 EUR | 394.20 − 154.53 = **239.67 EUR** |
| Kurs | Rechnung EUR in BUKRS 1200 (Hauswaehrung EUR, `T001`), `RBKP-KURSF` 1.00000; der IV-Kurs 0.94 (`VBRK-KURRF`) betrifft nur die FI-Buchung in 1100 (CHF) | dito |

Rechnung Bestellnetto: 131.40 × 0.56 = 73.584; × 0.70 = 51.5088 → 51.51 EUR; 10 ST: 1'314.00 − 578.16 − 220.75 = 515.09 EUR
(Werte aus `PRCD_ELEMENTS` 1000158003). Die Restdifferenz kommt nicht aus der IV, sondern aus den Einkaufsrabatten des
T76-Infosatzes: `EINA` 5300015578 (2367/70953, FPA 30.09.) hat `LTSNR` leer, deshalb greifen RL01 und ZRL2 ueber
`A044` (KNUMH 970106 RL01 −44 %, 970109 ZRL2 mit Mengenstaffel: `KONP` Grundwert −10 %, in der Bestellung bei
`KSTBS` 10 −30 %). Fuer das Teilsortiment SPEZ gibt es in T76
schon `A069` 70953/1200/SPEZ mit RL01 0 % (KNUMH 970107) und ZRL2 0 % (KNUMH 970110), Zugriffsfolge 0006 Stufe 020
vor `A044` Stufe 030 (`T682I`). Mit `LTSNR` SPEZ, wie Fabio es in P76 am 04.11. fuer 2367 gepflegt hat
(`IC_Umsetzung_V1.xlsx` Blatt 5, Infosatz P76 5300015770), waere das Bestellnetto 131.40 EUR = IV-Preis nachher, die
Differenz 0.00 EUR; vorher blieben +59.70 EUR. Der T76-Infosatz wurde **nicht** geaendert.

**Toleranz 1200 (T76, `T169G` PP):** obere Grenze absolut 100.00 EUR, obere Grenze 4 %, beide aktiv. **Vermutung,
nicht gebucht:** 239.67 EUR (155 %) wuerde in T76 zur Zahlungssperre fuehren; im P76-Stand waere nachher 0.00 EUR ohne
Sperre, vorher 59.70 EUR (15 %) ueber 4 % und damit gesperrt. P76-`T169G` wurde nicht gelesen.

**Abbruch beim Buchen:** In MIR4 0051128478 im Aendern-Modus Betrag 394.20, Menge 3 und `KZMEK` „Kein Fehler“
eingetragen, danach Enter. Um 15:27 riss die GUI-Verbindung ab („T76: connection to partner
'trast76s.sap.trafag.com:3200' broken, WSAECONNRESET“, Windows-Meldungsfenster von SAP GUI). Die Skripte hingen,
ich habe sie beendet und das Fenster mit „Nein“ geschlossen; danach `Verbindungen=0`, also auch Ingos idle Sitzung
`ses[0]` weg. RFC lief weiter. Ergebnis per RFC geprueft: `RBKP` 0051128478 weiter `RBSTAT` 3, kein `RSEG`, kein `EKBE`
VGABE 2; die Eingaben sind nicht gesichert. Neu anmelden kann ich nicht (Passwort), deshalb ist die Rechnung offen.

**Panne:** Vorher hatte ich in MIR4 die Vorher-Rechnung 0051128477 im Aendern-Modus geoeffnet und
`menu[0]/menu[2]` gewaehlt, das im Anzeigemodus „Beleg simulieren“ heisst, im Aendern-Modus aber „Merken“. SAP meldete
„Rechnungsbeleg 51128477 / 2026 wurde gesichert“. Per RFC gegengeprueft: `RBSTAT` 3, `RBDRSEG` unveraendert (Menge und
Betrag 0, `KZMEK` 1), kein `RSEG`, kein `EKBE`; inhaltlich nichts geaendert, nur neu gemerkt.

**Offene Schritte S7 (naechste Sitzung, T76):** MIR4 0051128478/2026 → Anzeigen/Aendern → Position 1: Betrag 394.20,
Menge 3, `KZMEK` „Kein Fehler“, Haken OK → Springen > Meldungen lesen → Rechnungsbeleg > Buchen (im Aendern-Modus
`menu[0]/menu[3]`, Menues vorher neu auslesen) → per RFC `RBKP` (`RBSTAT` 5, `ZLSPR`), `RSEG`, `EKBE` VGABE 2,
`BKPF` pruefen. Erwartet in T76: buchbar, Preisdifferenz 239.67 EUR, Zahlungssperre R (Vermutung, Toleranz PP). Die
Vorher-Rechnung 0051128477 nicht buchen, sie bleibt als Vergleich fehlerhaft stehen.

#### Gegenprobe Kundenfaktura F2 (ZVAA01): nur lesend, keine neue Preisfindung

- Seit der Aktivierung (13:20) gibt es in T76 keine F2 (`VBRK` ab 01.11.2026: nur die drei IV 90381201 bis 203,
  `KALSM` ZICAA1; ab 06.11. mit `KALSM` ZVAA01: keine).
- Offene fakturierbare Kundenlieferungen 1100 (`LIKP` LFART LF, `FKSTK` A, `WBSTK` C): genau eine, 0080429152
  (Kunde 500436, F90180 5 ST, Auftrag 0000408146/10 TA, `KALSM` ZVAA01, Auftragsmenge 5 ST = Liefermenge, USD, PR00
  nicht aus `A305`). Das waere nur eine Komplettlieferung und zeigt so wenig wie S5. Die Preisfindung per VF01 ohne
  Sichern war nach dem Verbindungsabriss nicht mehr moeglich.
- Lesend belegt bleibt: Die Routine endet fuer ZVAA01 in der ersten Anweisung (`CHECK komk-kalsm = lc_kalsm_ic`,
  `staffelbasis_601.abap` Zeile 39); `KOMK-KALSM` kommt aus `VBRK-KALSM`, und ZICAA1 ist nur ueber `KALVG` I
  (Fakturaarten IG, IG2, IGA, IGS, IV, IVA, IVS) erreichbar (Abschnitt 8.2). S5 (VA01, ZVAA01) zeigte 151.30 EUR bei
  3 ST wie ohne Routine. Ein isolierter Test einer F2 mit Teillieferung fehlt weiterhin.

#### Testtabelle S1 bis S7 (Blatt 8, Ist)

| Nr | Testfall (Fabio) | Ist in T76 | OK? |
| --- | --- | --- | --- |
| S1 | Bestellung 100 ST 54150, Lieferung 50 ST | 54150 hat in T76 keine PR00-820-Staffel; nachgestellt mit 2367: Bestellung 45148502 10 ST, Lieferung 80432794 3 ST, IV 90381202 `KSTBS` 10, 131.40 EUR (Stufe ab 10), 394.20 EUR | ja (Muster) |
| S2 | Restlieferung zur selben Bestellung | 80432794 ist die zweite Teillieferung zu 45148502 (erste 80432793 vor der Routine, IV 90381201 151.30 EUR); nachher 131.40 EUR | ja (Muster) |
| S3 | Bestellung 5 ST 46690, Lieferung 3 ST | 46690 ohne Bestand in 1100/0001; gleiches Muster wie S1 mit 2367 | ja (Muster) |
| S4 | Komplettlieferung | 45148503 2 ST, Lieferung 80432795 2 ST, IV 90381203 `KSTBS` 2, 151.30 EUR, 302.60 EUR | ja |
| S5 | Kundenauftrag ZVAA01 mit PR00 | VA01 TA 820/2367 3 ST, nicht gesichert, 151.30 EUR; F2-Gegenprobe nur lesend (oben) | ja (ohne F2-Teillieferung) |
| S6 | Bestellmenge in anderer Einheit | nicht testbar in T76 (kein PR00-820-Material mit alternativer Einheit); Zweig gegen Standard `LV61AA73` gelesen, Bausteintest nicht moeglich | offen, nicht testbar in T76 |
| S7 | MIRO 1200 (IDoc INVOIC) ohne Preisdifferenz nach WE | IDoc-Kette 90381202 → 15343208/15343209 → Rechnung 0051128478 (fehlerhaft, `KZMEK` 1); WE 5001255589 3 ST gebucht; Stufe gleich (131.40 = 131.40), Restdifferenz 239.67 EUR nur aus RL01/ZRL2 des T76-Infosatzes; Buchung wegen Verbindungsabriss nicht abgeschlossen | teilweise; Routine ok, MIRO-Buchung offen |

**Transportfertig: ja, mit Vorbehalt.** Die Routine und der V/06-Eintrag sind mit S1 bis S5 in T76 belegt, und S7 zeigt
auf Stufenebene, dass Bestellung und IV nachher denselben Preis tragen. Die beiden Vorbehalte betreffen nicht den Code:
S6 ist in T76 nicht testbar (nur Quelltext gegen Standard), und „keine Preisdifferenz in MIRO“ haengt vom P76-Infosatz
mit Teilsortiment SPEZ ab; die MIRO-Buchung in T76 ist nach dem Verbindungsabriss noch offen. Ob Ingo vor der Freigabe
auf diese Buchung wartet, ist sein Entscheid.

**Testbelege dieses Abschnitts in T76 (KOI, 06.11.):** Anlieferung 0180000073, Materialbeleg 5001255589 (101, 3 ST
2367 in 1200/0001); `NAST` SPED 80432794 verarbeitet; Rechnung 0051128477 einmal unveraendert gemerkt. Nicht angelegt
oder geaendert: Stammdaten, Konditionen, Infosatz, Customizing, Quelltext.

### 8.9 Abnahme Fabio und Import nach P76 (2026-11-10)

**Fabio (Mail an Ingo, 10.11.):** „Alle Tests wurden erfolgreich durchgebucht“, mit Screenshot und Testdokument; „hiermit freigegeben
fuer den Transport im Livesystem“. Seine Testtabelle (T76, alle Zeilen gruen, Bild im Chat 10.11.):

| Testfall T76 | Erwartung | Ergebnis laut Fabio |
| --- | --- | --- |
| Stammdaten und Preise 54150/46690 angepasst | | erledigt |
| Bestellung 100 ST 54150, Lieferung 50 ST | IV 30.30 EUR/ST (Staffel ab 100) | Auftrag 411361, Bestellung 45148506, LS 80432799, IV 90381208, ok |
| Restlieferung 50 ST zur selben Bestellung | IV 30.30 EUR/ST | LS 80432799, IV 90381209, Preis ok |
| Bestellung 5 ST 46690, Lieferung 3 ST | IV 72.00 EUR/ST (Stufe ab 5), nicht 81.40 | Auftrag 411362, Bestellung 45148507, LS 80432801, IV 90381210, ok |
| Komplettlieferung 54150 | unveraendert 30.30 EUR/ST | Auftrag 411363, Bestellung 45148508, LS 80432804, IV 90381211, ok |
| Normaler Kundenauftrag ZVAA01 mit PR00 | unveraendert | 411364, Z000 priorisiert, ok |
| Bestellmenge in anderer Einheit | korrekt umgerechnet | Fall unnoetig, Stk., ok |
| MIRO 1200 (IDoc INVOIC) | keine Preisdifferenz, Beleg gebucht (nach WE) | ok |

Damit ist auch der offene Punkt S7 (MIRO, Abschnitt 8.8) aus Fabios Sicht erledigt.

**Import:** Laut Ingo am 10.11. nach P76 importiert, gemeldet als „*854 und *856“. T76K912854 ist die Aufgabe (Reparatur
`RV62A601`) unter dem Workbench-Auftrag T76K912853; transportiert wird der Auftrag, also **T76K912853 und T76K912856**. Im System
nicht nachgesehen (P76 nur mit Freigabe Ingo).

**Offen:**
1. Pruefung nach dem Import in P76 (Abschnitt 8.6 Punkt 2): `RV62ANNN` enthaelt `INCLUDE RV62A601`, `TFRM PFRS 601 AKTIV X`, `RV62A601`
   aktiv, `T685A` V/PR00 `KOFRS 0000601`; sonst VOFM 601 aktivieren bzw. RV80HGEN (Ingo). Lesend durch Claude nur mit Freigabe Ingo.
   *(Geprueft 10.11. abends, unten: alles da ausser `INCLUDE RV62A601` in `RV62ANNN`; offen ist jetzt VOFM 601 in P76 aktivieren.)*
2. Fall S6 (Unterlieferung in P76) war in T76 nicht vollstaendig testbar: erste echte IV mit Unterlieferung in P76 beobachten.
3. Schritt 1 der Anfrage vom 04.11. (ZICAA1 um Z999/Z000/Z001/Z002 mit Konditionsausschluss) ist nicht begonnen; danach dieselbe
   Routine 601 bei Z999/Z002 eintragen (Abschnitt 8.6 Punkt 4). T76-Wartung 11.11. 16 bis 18 Uhr beachten.

**Zwischenbefund P76, 2026-11-10 nachmittags (unvollstaendig, unbestaetigt):** Die lesende Pruefung in P76 (Freigabe Ingo „ja mache
test“, Agent sap-t76) wurde wegen eines PC-Neustarts abgebrochen. Ihre letzte Meldung: „`D010INC` bestaetigt: `SAPLV61A` bindet nur
`RV62ANNN` ein, `RV62A601` wird von keinem Programm eingebunden.“ Trifft das zu, fehlt in P76 die Zeile `INCLUDE RV62A601` in
`RV62ANNN` (Punkt 4 der Pruefliste), und **die Staffelroutine 601 wirkt in P76 noch nicht**, obwohl die Auftraege importiert sind. Das
war erwartet, weil `RV62ANNN` in keinem Auftrag liegt (Abschnitt 8.4). Abhilfe laut Abschnitt 8.6 Punkt 2: in P76 VOFM *Formeln >
Staffelbasis* 601 aktivieren (erzeugt `RV62ANNN` neu) bzw. RV80HGEN, macht Ingo. Die uebrigen Punkte (Importstand, `TFRM` 601 aktiv,
`RV62A601` aktiv und inhaltsgleich, `T685A` V/PR00 `KOFRS 0000601`) sind **nicht gemeldet**. Die Pruefung ist nach dem Neustart zu
wiederholen, bevor jemand handelt. *(Wiederholt und bestaetigt am selben Abend, siehe unten.)*

**P76-Pruefung nach dem Import, 2026-11-10 abends (bestaetigt, vollstaendig):** Freigabe Ingo „ja mache test“, nach dem Neustart
„mach weiter“. Claude selbst, angemeldete GUI Ingos (P76/100, `trasp76s`), eigene Sitzung `/app/con[0]/ses[1]` ueber
`.tmp_sap_probe/P76Lesen.vbs neu:SE16`, nur SE16, danach mit `P76SitzungSchliessen.vbs` geschlossen (danach nur noch `ses[0]`).
Nichts geaendert; SE16 hat wie ueblich den Selektionsreport `/1BCDWB/DBTRDIR` erzeugt.

| Punkt | Befund P76 | Bewertung |
| --- | --- | --- |
| `TFRM` PFRS 601 | `AKTIV X`, `KAPPL V`, Text „IC Bestellmenge“, `GNDAT 06.11.2026 GNZEI 13:20:03` | importiert; Zeitstempel ist der aus T76, nicht in P76 neu generiert |
| `RV62A601` (`TRDIR`) | vorhanden, `CNAM`/`UNAM` KOI, `CDAT`/`UDAT` 10.11.2026, `SUBC I` | Include importiert (Auftrag T76K912853); inhaltlicher Vergleich mit T76 nicht gemacht |
| `RV62ANNN` (`TRDIR`) | zuletzt geaendert `TRIM_MILI` 16.01.2026, Version 44 | **nicht neu erzeugt**, enthaelt die Zeile `INCLUDE RV62A601` also nicht |
| `D010INC` | `INCLUDE = RV62A601`: keine Eintraege; `RV62ANNN` wird von `SAPLV61A` eingebunden | **`RV62A601` von keinem Programm eingebunden** |
| `T685A` V/PR00 | `KZBZG C`, `KOFRS 0000601` | Customizing importiert (T76K912856) |

**Ergebnis:** Beide Auftraege sind in P76 angekommen, aber **die Staffelroutine 601 wirkt in P76 noch nicht**. PR00 verweist auf
Routine 601, `SAPLV61A` kennt sie jedoch nicht; der Aufruf `PERFORM (frm_staffelbas) IN PROGRAM saplv61a IF FOUND` laeuft ins Leere,
und die IV-Faktura rechnet weiter mit der Liefermenge als Staffelbasis (also wie vor der Aenderung, kein Abbruch). Das war erwartet,
weil `RV62ANNN` in keinem Auftrag liegt (Abschnitt 8.4).

**Abhilfe (macht Ingo, schreibend in P76):** VOFM *Formeln > Staffelbasis*, Zeile 601 markieren, *Bearbeiten > Aktivieren*; das
erzeugt `RV62ANNN` neu mit `INCLUDE RV62A601`. Alternativ Report `RV80HGEN` (SE38) laufen lassen. Danach lesend nachpruefen:
`D010INC` `INCLUDE = RV62A601` mit `MASTER SAPLV61A` vorhanden, `TRDIR` `RV62ANNN` mit neuem `UDAT`. Erst dann gilt die
Staffelbasis in P76 als produktiv; die erste echte IV mit Unterlieferung (Punkt 2 oben) danach beobachten.

**Einbindung versucht, 2026-11-10 abends (Auftrag Ingo „kannst du das bitte machen“; nichts geaendert):** Vorpruefung lesend in
eigener Sitzung (SE16, SE38 nur *Anzeigen*, Zeilen ueber `GuiAbapEditor.GetLineText`):

- `TPALOG`: T76K912853 und T76K912856 am 10.11. 14:33/14:34 importiert, alle Schritte `RETCODE 0000`. Bei 853 kein Schritt `G`
  (Generierung) und keine Nachimportmethode; es gibt also nichts, was `RV62ANNN` nach dem Import neu erzeugt haette.
- `RV62A601` in P76 ist **zeilengleich** mit `staffelbasis_601.abap` (103 Zeilen).
- `RV62ANNN` in P76 enthaelt nur die sechs Kommentarzeilen (Rueckfallstand, Kopie im Scratchpad der Sitzung).
- VOFM in P76: Popup „SAP-System hat den Status ‚nicht aenderbar‘“, mit *Abbrechen* verlassen. **Der Weg ueber VOFM *Aktivieren*
  geht in P76 nicht**, solange die Systemaenderbarkeit zu ist.
- `RV80HGEN` (91 Zeilen, kein Selektionsbild) ruft `PERFORM aktivieren_alle(SAPMV80H) USING 'X'` und erzeugt **alle** VOFM-Gruppen
  neu. In `TFRM` sind seit dem 16.01.2026 genau zwei Routinen neu aktiv: PFRS 601 und **FBED 908** „Pos. auftr.Proforma“
  (`GNDAT 05.07.2026`). `RV60B908` (TRIM_MILI, 02.08.2026, Kennung `T76K912226`) steht in P76, `RV60BNNN` ist aber ebenso leer und
  `D010INC` kennt kein Programm mit `RV60B908`. 908 haengt an der Kopiersteuerung `TVCPF` TA → F5, Positionstyp TAW, und prueft
  Fakturasperre (Position und Kunde), Fakturarelevanz und bereits fakturierten Fakturaplan. **Ein `RV80HGEN`-Lauf wuerde 908 also
  mit scharf schalten**, und die Proforma F5 aus TA/TAW wuerde danach anders geprueft als heute.

Wege (Entscheid Ingo): (a) `RV62ANNN` aus T76 (dort mit `INCLUDE RV62A601`) als Reparatur in einem eigenen Auftrag transportieren,
wirkt nur auf die Staffelbasis; (b) `RV80HGEN` in P76, wenn 908 ebenfalls wirken soll (vorher klaeren, wer 908 bestellt hat);
(c) Systemaenderbarkeit kurz oeffnen und VOFM 601 aktivieren (Basis-Eingriff in der Produktion, nicht empfohlen).
