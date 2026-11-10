# QM-Meldungen: offene Q1-Meldungen (Kundenreklamation) als OData-Set `QmMeldungSet`

Stand: 2026-11-05, **in T76 fertig und geprueft**, Auftrag `T76K912837` (Aufgabe `T76K912838`), nicht freigegeben.
Freigabe und Import nach P76 macht Ingo. P76 wurde fuer diese Arbeit weder gelesen noch geschrieben.

Zweck: Verkauf, Unterreiter „Monthly Review Gesellschaften“, Abschnitt 4 „Qualitaet · offene Q-Faelle“ (Wunsch Patrick
Hertach, mehr Tiefe als der Power-BI-Bericht „RMA Statistic“). Datenvertrag und Maske:
`docs/monthly_review/MASKE_SPEZIFIKATION_2026-11-04.md` Abschnitte 1.8, 1.11 und 2.3 Punkt 10 bis 13.
Muster: `ShopAufSet` Typ VZ (`saptasks/shopfloor_sap/NOTES.md`, `T76K912797`) und `ZloTopDownSet`
(`zlo03/FIX25_APP_EXPORT_2026-11-02.md`, `docs/abap/ZZLO_TOPDOWN_ADD.abap`).

## 1. Objekte in T76 (Mandant 100)

| Objekt | Inhalt | Auftrag | Stand |
| --- | --- | --- | --- |
| Struktur `ZSTR_QM_MELDUNG`, Paket `ZPP` | 17 Felder, Liste in `ZSTR_QM_MELDUNG_felder.txt` (alle Datenelemente ohne Konvertierungsexit, `MENGE` als `/SCMTMS/DECIMAL_VALUE` = DEC 28,6 ohne Mengenreferenz wie `ZSTR_ZLO_TOPDOWN`) | `T76K912837` | aktiv, DD03L 17 Zeilen |
| `ZCL_ZPOWERBI_EINKAUF_MPC_EXT->DEFINE` | Entity-Typ `QmMeldung`, Set `QmMeldungSet`, Block am Ende vor `ENDMETHOD` (nach dem `Zeivr`-Block) | `T76K912837` | aktiv |
| `ZCL_ZPOWERBI_EINKAUF_DPC_EXT->/IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET` | eigener Zweig `IF iv_entity_set_name = 'QmMeldungSet'` nach `ZloTopDownSet`, vor der FinanzJournal-Weiche | `T76K912837` | aktiv |

E071 von `T76K912838`: `R3TR TABL ZSTR_QM_MELDUNG`, `LIMU METH ZCL_ZPOWERBI_EINKAUF_MPC_EXT DEFINE`,
`LIMU METH ZCL_ZPOWERBI_EINKAUF_DPC_EXT /IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET`. Kein fremdes Objekt.
Vorher geprueft: alle frueheren Auftraege mit diesen Klassen sind freigegeben (`T76K912738`, `785`, `793`, `797`: E070 `R`),
die Klassen waren nicht gesperrt; das Sichern fragte nach dem Auftrag, gesetzt wurde ausdruecklich `T76K912837`.
`DWINACTIV` fuer KOI und diese Klassen danach leer.

**Rueckleseabgleich** (RFC `abap-read`, CR entfernt, `diff` mit eigenem Exitcode):

- `ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001`: zurueckgelesen **identisch** mit `MPC_EXT_DEFINE_neu.abap` (2'942 Zeilen);
  gegen `MPC_EXT_DEFINE_vorher.abap` (2'818 Zeilen) nur 124 angefuegte Zeilen, 0 geaenderte oder geloeschte.
- `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W`: zurueckgelesen **identisch** mit `DPC_EXT_GET_ENTITYSET_neu.abap` (2'608 Zeilen);
  gegen `DPC_EXT_GET_ENTITYSET_vorher.abap` (2'292 Zeilen) nur der eingefuegte Zweig ab Zeile 1947, 0 geaenderte oder
  geloeschte Zeilen. Der Vorher-Stand war zeilengleich mit `saptasks/shopfloor_sap/DPC_EXT_GET_ENTITYSET_2026-11-02_verzug_neu.abap`.

Gateway-Caches in T76 danach in der Reihenfolge `/IWBEP/CACHE_CLEANUP` (alle Modelle), dann `/IWFND/CACHE_CLEANUP`
(Modell `ZPOWERBI_EINKAUF_MDL`) geleert. Eigene Sitzung (`ses[1]`) danach geschlossen, Ingos Sitzung unberuehrt.

Dateien hier: `ZQM_MELDUNG_ADD.abap` (nur die beiden Bloecke, Quelle der Wahrheit), `*_vorher.abap` (Live-Stand vor
der Aenderung, per RFC gelesen), `*_neu.abap` (eingespielter Stand, erzeugt mit `methoden_bauen.py`),
`referenz_rfc.py` und `referenz_sysstat_menge.py` (unabhaengige Nachrechnung per RFC, Abschnitt 4).

## 2. Feld-Mapping (Datenvertrag, alle gelesen in T76 am 2026-11-05)

Meldungsart: `TQ80_T` Q1 = „Kundenreklamation“ (Q2 Maengelruege an Lieferant, Q3 interne Problemmeldung). `TQ80` Q1:
`QMTYP 02`, Statusschema `STSMA` **leer**, Partnerrollen `PARVW_KUND = WE`, `PARVW_VERA = VW`, `PARVW_INT = AB`,
Fehlerkatalog `FEKAT 9`, Massnahmenkatalog `MAKAT 2`.

| Property | Typ | Quelle / Logik | Beleg |
| --- | --- | --- | --- |
| `Qmnum` (Schluessel) | String 12 | `QMEL-QMNUM`, ohne fuehrende Nullen (`CONVERSION_EXIT_ALPHA_OUTPUT`) | |
| `Qmart` | String 2 | `QMEL-QMART`, immer `Q1` | |
| `Status` | String 80 | Anwenderstatus: aktive `JEST`-Eintraege `E*` des Objekts, Schema aus `JSTO-STSMA`, Text `TJ30T-TXT30` Sprache `D`; der Status mit Statusnummer (`TJ30-STONR > 0`) hat Vorrang, sonst alle aktiven mit „ / “ | Q1 hat in T76 **kein** Statusschema (`TQ80-STSMA` leer, `JSTO` der Q1-Objekte leer); in ganz T76 hat genau ein QM-Objekt ein Schema (`QM000000100311`, `QM_N_001`, abgeschlossen, aktiv `E0003` „Bearbeitung freigegeben“). In T76 bleibt `Status` daher fuer alle 313 leer; **dieser Teil des Zweigs ist in T76 ungetestet** (nur von Hand: 100311 ergaebe „Bearbeitung freigegeben“). Die Power-BI-Werte „Ruecksendung offen“, „in Bearbeitung“, „Wareneingang“ gibt es in `TJ30T` von T76 nicht (Abschnitt 6) |
| `Sysstat` | String 40 | `STATUS_TEXT_EDIT` (`SPRAS = 'D'`, `ONLY_ACTIVE = 'X'`), Feld `LINE`: Systemstatus-Kurztexte wie in QM03, z. B. „MOFN OFMA EIGV“; das Format entspricht dem Power-BI-Wert „MIAR FRDV OFMA“ (I0070 + I0161 + I0158) | Signatur aus `FUPARAREF`; Texte `TJ02T` |
| `Kunnr` | String 10 | `QMEL-KUNUM`, ohne fuehrende Nullen | |
| `Kunde` | String 35 | `KNA1-NAME1` zu `QMEL-KUNUM` | Spezifikation 3.5 ordnet ueber den Namensanfang zu („trafag controls india“ usw.) |
| `Matnr` | String 40 | `QMEL-MATNR`, `CONVERSION_EXIT_MATN1_OUTPUT` | 5 der 313 ohne Material |
| `Maktx` | String 40 | `MAKT-MAKTX`, Sprache `D`, Rueckfall `E` | |
| `Menge` | Decimal (28,6) | `QMEL-RKMNG` „Reklamierte Menge“ (`DD04T`); JSON z. B. `"4.000000"` | 11 der 313 mit 0 |
| `Mengeneinheit` | String 3 | `QMEL-MGEIN`, `CONVERSION_EXIT_CUNIT_OUTPUT` Sprache `D` | |
| `Fehlerbild` | String 40 | `QMEL-QMTXT` (Kurztext der Meldung, z. B. „Leckage“, „Switch point“) | Gemessen: nur 99 der 313 offenen haben eine Fehlerposition `QMFE` (Katalog 9), davon 85 mit Code `Z01001/0006` „zur Analyse“; die Kopfcodierung (`QMKAT D`, `Z01001`: Lieferfehler, Problem bei Inbetriebnahme, Feldausfall …) ist eine Kategorie, kein Fehlerbild. 21 der 313 haben keinen Kurztext |
| `Angelegt` | String 8 | `QMEL-ERDAT` als JJJJMMTT („Angelegt am RMA“); Alternative waere `QMDAT` (Meldungsdatum) | |
| `Ruecklieferung` | String 8 | `QMEL-RKDAT` „Datum der Ruecklieferung“ (`DD04T`), leer wenn initial | 221 der 313 leer |
| `Produktion` | String 8 | `QMEL-PRODDAT` „Produktionsdatum“ (`DD04T`), leer wenn initial | 19 leer |
| `Massnahme` | String 8 | kleinstes `QMSM-ERDAT` der Massnahmen mit `KZLOESCH = ' '` (Katalog 2, Codegruppe `Z01002` „Handling“: 0001 Meldungsuebergabe an Verkauf … 0006 Abklaerung durch Verkauf) | 5 der 313 ohne Massnahme. Die erste Massnahme (meist 0006) entsteht fast immer am Anlagetag |
| `Bearbeiter` | String 40 | Partnerrolle `VW` (Verantwortlicher, `TQ80-PARVW_VERA`) aus `IHPA` (`KZLOESCH = ' '`, kleinster `COUNTER`), `TPAR` VW = `PE` (Personalnummer), Name `PA0001-ENAME` des juengsten Satzes | alle 313 haben genau einen VW-Partner |
| `Produktlinie` | String 40 | **leer**, siehe Abschnitt 6 | |

Datumsfelder: `JJJJMMTT` oder leer (nie `00000000`). Das Set kennt keine Filter; `$top`/`$skip` setzt der Zweig selbst
um, sortiert nach `Qmnum`. `$select` erledigt das Gateway.

## 3. „Offen“

`QMEL-QMART = 'Q1'`, `QMEL-KZLOESCH = ' '` und in `JEST` weder `I0072` (MMAB „Meldung abgeschlossen“) noch `I0076`
(LOEVM „Loeschvormerkung“) aktiv (`INACT = ' '`). Statustexte aus `TJ02T`: I0068 MOFN „Meldung offen“, I0069 MRST
„Meldung zurueckgestellt“, I0070 MIAR „Meldung in Arbeit“, I0072 MMAB, I0076 LOEVM. Q1 in T76: 15'229 Meldungen, davon
14'853 MMAB, 63 MMAB + LOEVM, **313 offen** (205 MOFN, 92 MRST, 16 MIAR); keine mit `KZLOESCH`.

Offene nach Anlagejahr und Status (T76, Kopie mit Daten bis etwa April 2026):

| Jahr | MOFN | MRST | MIAR |
| --- | ---: | ---: | ---: |
| 2013 | | | 3 |
| 2017 | 1 | | |
| 2019 | 81 | 1 | 2 |
| 2022 | | 17 | 2 |
| 2023 | | 16 | |
| 2024 | | 19 | 5 |
| 2025 | 24 | 39 | 1 |
| 2026 | 99 | | 3 |

Power BI zeigte am 03.11. in P76 131 offene (Statussicht). Ohne MRST und ohne die 81 MOFN aus 2019 blieben in T76 rund
140. Das ist eine Vermutung zum Abgleich mit Patrick, kein eingebauter Filter: MRST steht in `Sysstat`, das Dashboard kann
es ohne neuen Transport ausblenden.

## 4. Pruefung in T76 (Gateway Client, eigene Sitzung)

| Aufruf | Ergebnis |
| --- | --- |
| `$metadata` | 200, 384'482 Byte |
| `QmMeldungSet?$top=5&$format=json` | 200, 3'401 Byte, 1,5 s |
| `QmMeldungSet?$skip=312&$top=5` / `$skip=313&$top=5` | 200, 658 Byte (eine Zeile) / 20 Byte (leer) → **313 Zeilen** |
| `QmMeldungSet?$format=json` (alles) | 200, 206'431 Byte, 1,4 s |
| bestehend: `ZloTopDownSet` (Werks 1100, Vknr 41019, `$top=2`), `EKKOSet?$top=2` | beide 200 |
| ST22 nach allen Aufrufen (eigene Sitzung) | „Heute 0 Laufzeitfehler“ (RFC auf `SNAP` geht nicht: `TABLE_NOT_AVAILABLE`) |

Den Antwortrumpf gibt der Gateway Client nicht ans Scripting heraus (`SAP_ARBEITSWEISE_UND_WERKZEUGE.md`). Deshalb
**byte-genauer Abgleich je Feld**: `referenz_rfc.py` baut die 313 Zeilen unabhaengig per RFC (QMEL, JEST, KNA1, MAKT,
QMSM, IHPA, PA0001) nach und rechnet je Property die erwartete Mehrlaenge von `$select=Qmnum,<Property>` gegenueber
`$select=Qmnum` (89'224 Byte). Alle 16 stimmen auf das Byte:

| Property | Gateway Δ Byte | RFC erwartet |
| --- | ---: | ---: |
| Qmart | 4'069 | 4'069 |
| Status | 3'756 | 3'756 (alle leer) |
| Sysstat | 8'871 | 8'871 (alle aktiven I-Status, `referenz_sysstat_menge.py`) |
| Kunnr | 5'235 | 5'235 |
| Kunde | 11'020 | 11'020 |
| Matnr | 4'979 | 4'979 |
| Maktx | 11'702 | 11'702 |
| Menge | 5'975 | 5'975 (sechs Nachkommastellen) |
| Mengeneinheit | 6'573 | 6'573 |
| Fehlerbild | 10'847 | 10'847 |
| Angelegt | 6'886 | 6'886 |
| Ruecklieferung | 6'996 | 6'996 |
| Produktion | 7'360 | 7'360 |
| Massnahme | 7'159 | 7'159 |
| Bearbeiter | 10'145 | 10'145 |
| Produktlinie | 5'634 | 5'634 (alle leer) |

Haeufigste `Sysstat`-Kombinationen der 313: MOFN OFMA EIGV 123, MRST OFMA EIGV 75, MOFN MMDR OFMA EIGV 55,
MOFN MMDR AMER EIGV 24, MRST AMER EIGV 12, MIAR AMER FRDV 7 (Reihenfolge im Feld wie `STATUS_TEXT_EDIT`).

Beispiel: die ersten fuenf Zeilen (`$top=5`, nach `Qmnum`), aus der RFC-Nachrechnung, deren Laengen mit dem Gateway
uebereinstimmen. Kundennamen und Bearbeiter hier bewusst nicht ausgeschrieben; in `Maktx` stehen nach dem Typencode
im System zwei Leerzeichen (z. B. `8726  2-FACH …`), die Tabelle zeigt eines.

| Qmnum | Kunnr | Matnr | Maktx | Menge | ME | Fehlerbild | Angelegt | Ruecklief. | Produktion | Massnahme |
| --- | --- | --- | --- | ---: | --- | --- | --- | --- | --- | --- |
| 107662 | 301038 | 42053 | 8726 2-FACH DICHTEWAECHTER | 4 | ST | Niederdruckanzeige keine Funktion | 20130408 | 20130408 | 20121101 | 20130408 |
| 107692 | 300018 | 43494 | 8783 3-FACH HYBRID-DICHTEWAECHTER | 2 | ST | Sensor hat falsches 0 Signal | 20130422 | 20130419 | 20130404 | 20130422 |
| 107783 | 300018 | 43495 | 8783 3-FACH HYBRID-DICHTEWAECHTER | 1 | ST | Sensor defekt | 20130627 | 20130626 | 20121123 | 20130627 |
| 110097 | 300880 | 44020 | PRESSURE SENSOR PVX | 5 | ST | PVX Signalfehler | 20170906 | 20170906 | 20150702 | 20170906 |
| 110929 | 304907 | 51147 | 8737 3-FACH DICHTEWAECHTER | 1 | ST | Die Anzeige ist defekt (Nadel verbogen)! | 20190319 | 20191108 | 20181130 | 20190319 |

Form der Antwort (OData V2): `{"d":{"results":[{"__metadata":{...,"type":"ZPOWERBI_EINKAUF_SRV.QmMeldung"},
"Qmnum":"107662","Qmart":"Q1","Status":"","Sysstat":"…","Kunnr":"301038","Kunde":"…","Matnr":"42053",…,
"Menge":"4.000000","Mengeneinheit":"ST",…,"Produktlinie":""}]}}`.

## 5. Wie gebaut (Werkzeuge, alles in einer eigenen T76-Sitzung)

1. Zu Beginn war keine T76-Sitzung offen; `SapGuiOpenT76.vbs` oeffnete nur das Anmeldebild (kein SSO). Diese Verbindung
   wurde ohne Anmeldung wieder geschlossen. Kurz danach war Ingos T76-Verbindung (`ses[0]` SE80) da; eigene Sitzung mit
   `SapGuiNeueSitzungT76.vbs` (Index 1).
2. `SapGuiStrukturAnlegen.vbs 1 ZSTR_QM_MELDUNG …`, `SapGuiStrukturFelder.vbs 1 ZSTR_QM_MELDUNG_felder.txt` (17),
   Sichern, Paket `ZPP`, neuer Auftrag (`btn[8]`, Text „Monthly Review: QmMeldungSet offene Q1-Meldungen (OData)“),
   Aktivieren. Warnung im Protokoll: „Feldname SYSSTAT ist reserviert (Strukt. nicht als Include in DB-Tab. verwenden)“;
   gilt nur fuer Includes in Datenbanktabellen, die Struktur ist aktiv.
3. `SapGuiKlasseAendernMitAuftrag.vbs 1 <Klasse> T76K912837` (keine Abfrage beim Oeffnen), `SapGuiKlassenMethodeQuelltext.vbs`
   mit `*_neu.abap`, Auftragsabfrage beim Sichern mit `T76K912837` beantwortet, Syntaxpruefung „keine Syntaxfehler“,
   Aktivieren vom SE24-Einstiegsbild (`btn[27]`, ohne Arbeitsvorrat-Dialog: „Objekt(e) wurde(n) aktiviert“).
4. Caches, Gateway Client, Abgleich wie oben; eigene Sitzung mit `SapGuiSitzungSchliessen.vbs 0 1` geschlossen.

## 6. Offene Punkte

1. **Anwenderstatus in P76 pruefen (Ingo, lesend):** `TQ80` Q1 `STSMA`, `JSTO` einiger offener Q1-Objekte, `TJ30T` des
   Schemas. Erwartung: P76 hat seit der T76-Kopie ein Schema mit „Ruecksendung offen“, „in Bearbeitung“, „Wareneingang“;
   dann fuellt der Zweig `Status` ohne Aenderung. Hat P76 kein Schema, kommt der Power-BI-Status aus einer eigenen
   Ableitung im Bericht (dann mit Patrick klaeren, welche).
2. **Statusfilter mit Patrick abstimmen:** Zaehlt MRST (zurueckgestellt) als offen? Was ist mit alten MOFN (81 aus 2019 in
   T76)? Heute liefert das Set alles ausser MMAB/LOEVM; filtern kann das Dashboard ueber `Sysstat` und `Angelegt`.
3. **Produktlinie:** bewusst leer. Die Power-BI-Linien (Dichtewaechter, TR5, TX Linie, Trafag CZ, …) sind weder Texte der
   Produkthierarchie (`QMEL-PRDHA` → `T179T`, z. B. 8736 „Dichtewaechter“, 8253 „Drucktransmitter“, 0987 „Pressostat“)
   noch die Produktgruppen `ZDISPO_SPART` (ueber `MARC-DISPO` Werk 1100 → `ZDISPO_GRP`: FP_DICHTEWAECH 68, FP_TRANSM. TX
   64, FP_TRANSM.TR 58, N/A 39, FP_DW-HYBRID 38, FP_PRESS,PICO 18, FP_PRESSO 9R5 12, ohne Material 5, Rest 12; zwei
   Materialien mehrdeutig). Das Dashboard kennt die Produktgruppe je Material schon aus dem Einkauf-Cache und kann sie
   ueber `Matnr` selbst zuordnen, ohne Transport. Entscheid Patrick/Ingo.
4. **Power-BI-„Queues“** (Detail-Analyse CSQ, Uebergabe an Verkauf, WE CSQ machen, Interim Report) sind vermutlich offene
   Massnahmen; in T76 gibt es im Katalog 2 / `Z01002` nur „Detailanalyse CSQ“ und „Meldungsuebergabe an Verkauf“, „WE CSQ
   machen“ und „Interim Report“ fehlen (wohl spaeter in P76 angelegt). Nicht im Datenvertrag; bei Bedarf ein weiteres Feld
   (offene Massnahme, Text `QPCT`) in einem Folgeauftrag.
5. **Import P76 (Ingo):** `T76K912837` freigeben und importieren, danach in P76 zuerst `/IWBEP/CACHE_CLEANUP`, dann
   `/IWFND/CACHE_CLEANUP` (Modell `ZPOWERBI_EINKAUF_MDL`), Gateway Client `QmMeldungSet?$top=5` und ein bestehendes Set.
   Der Auftrag enthaelt nur die drei Objekte oben; er haengt an keinem offenen Auftrag.
6. Die Personennamen (`Bearbeiter`) kommen aus `PA0001`. Ein SELECT im Gateway prueft keine HR-Berechtigung; falls das
   fuer das Dashboard nicht gewuenscht ist, Kuerzel statt Namen (Entscheid Ingo).

*Nachtrag 2026-11-09 15:13, produktiv:* `T76K912837` in P76 importiert (Importqueue: Warnung, RC 4), Caches in P76 auf Auftrag Ingo geleert (eigene Sitzung, `/IWBEP/CACHE_CLEANUP` alle Modelle, dann `/IWFND/CACHE_CLEANUP` `ZPOWERBI_EINKAUF_MDL`; Gateway Client `QmMeldungSet?$top=5` 200, 6'510 Byte; `ShopAufSet` 200). `MonthlyReview:QmEnabled = true` (`88e19f6`), deployed 15:11 (`4585392`). Im Browser: **345 offene Q1-Meldungen**, Ø 988 / Median 389 Tage offen, 264 laenger als 90 Tage, aeltester Fall 107662 (4'963 Tage). **Auffaellig, offen:** alle Meldungen ohne Status („–“) und ohne Produktlinie; Feldzuordnung im Set bzw. Daten in P76 pruefen (Statuswerte MMAB/LOeVM/NOCO/DLFL aus T76 erscheinen nicht).

*Nachtrag 2026-11-10:* P76 lesend `JSTO` (26'210 QM-Objekte): 26'209 ohne Statusschema, eines mit `QM_N_001`; P76 fuehrt fuer Q-Meldungen keinen Anwenderstatus, `Status` bleibt im Set leer. Dashboard (`MonthlyReviewQmCase.Status`): ohne Anwenderstatus der Lebenszyklus aus dem Systemstatus (MRST zurueckgestellt, MIAR in Arbeit, MOFN offen). Produktlinie: wo leer, ueber die Materialnummer aus dem Einkauf-Cache (ZC23-Produktgruppe `PurchasingMaterialShareSales`, sonst Disponent `PurchasingComponentDispoCache` ueber `PurchasingSpendDisponentRule`). Code `22d4371`, deployed 08:16 (`e6f6f70`). Ansicht im Browser noch nicht geprueft (Windows-Chrome nicht verbunden).
