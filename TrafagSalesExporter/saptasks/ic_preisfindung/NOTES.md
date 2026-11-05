# IC-Preisfindung Trafag AG (1100) an Trafag AT (1200): Preisliste und Kurstyp in der IV-Faktura

Stand: 2026-11-05 (Agent sap-t76, nur lesend in T76/100; keine Testbelege, keine Aenderung in T76)

Unterlagen: `a.xlsx` (IC_Umsetzung_V1, Blaetter 2 bis 4) und `b.pdf` (IC-Preisboard) in der Repository-Wurzel.
Geplanter Schritt 1 (Erweiterung ZICAA1 um Z999, Z000, Z001, Z002) ist **nicht** Teil dieser Pruefung und wurde nicht
angefasst.

Alle Belege stammen aus T76 (Datenkopie etwa Fruehjahr 2026). Fuer P76 sind sie Hinweise, kein Nachweis. P76 wurde in
diesem Auftrag nicht gelesen.

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
3. In P76 ist zu pruefen (nur lesend, eigener Auftrag), ob `KNVV-PLTYP` 14 und `KNVV-KURST` dort wie in T76 stehen,
   ob TCURR P EUR → CHF 2026 = 0.94 ist und ob M EUR → CHF dort ebenfalls nach EURX umgeleitet wird.
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
