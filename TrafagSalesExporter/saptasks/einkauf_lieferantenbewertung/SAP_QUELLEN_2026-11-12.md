# Lieferantenbewertung: SAP-Quellen (T76, nur gelesen)

Stand: 2026-11-12. Auftrag: Wochen_Todo 122. Fachlicher Hintergrund: `docs/EINKAUF_SITZUNG_2026-10-22.md` (Abschnitte 2, 5 und 6).

**Rahmen.** Alles stammt aus T76, Mandant 100. Gelesen wurde per RFC (`.tmp_sap_probe/RunSapProbe.ps1`, `table-read` und `abap-read`, Passwort aus der DPAPI-Ablage). In SAP wurde nichts geschrieben, angelegt oder aktiviert, und P76 wurde nicht angefasst. T76 ist eine Datenkopie. Die Masse der Bewegungsdaten reicht bis etwa April 2026: S013 hat rund 450 bis 590 Sätze je Monat bis `SPMON 202604` und danach nur noch Einzelsätze bis 202611, S012 reicht mit abnehmender Zahl bis 202612 und hat Ausreißer bis `SPMON 2315`, QMEL endet bei `ERDAT 20260407`. Welches Datum SPMON bestimmt, ist ungeprüft. Einige Einstellungen sind jünger, zum Beispiel die ALE-Änderungen vom Mai bis September 2026 und das Fiori-Customizing von Benutzer NTT_NOFE vom 27.10.2026.

**Grenze der Aussagen.** Zahlen aus T76 sind nur Hinweise auf P76, keine Belege. Wo eine Aussage nicht gelesen, sondern abgeleitet ist, steht „Vermutung“.

## 1. Übersicht: Baustein der Bewertung, SAP-Quelle, Beleg, offen

| Baustein | SAP-Quelle (Tabelle/Feld/Transaktion) | Beleg (T76) | offen |
| --- | --- | --- | --- |
| Einkaufsvolumen (EVO) je Lieferant | Der Dashboard-Cache aus `EKKOSet`/`EKPOSet` ist vorhanden (Spend-Aufriss). Daneben gibt es in SAP die LIS-Tabelle `S012` „Einkauf“ mit den Schlüsseln LIFNR, MATNR, WERKS, EKORG und SPMON und den Werten `NETWR` (MC_EFFWR „Bestelleffektivwert“), `WEWRT` (Wareneingangswert) und `REWRT` (Rechnungsbetrag). | `S012` hat 232'710 Sätze mit EKORG 1100 und VRSIO 000, die Hauptmasse monatlich bis 2026-04 (siehe Rahmen). Die Texte stehen in DD04T. | Ob EVO als Bestellwert, Wareneingangswert oder Rechnungswert zählt, ist offen (Entscheid E1 und Definition). Rückgaben und Währung sind ebenfalls offen. |
| A/B/C je Lieferant | In SAP gibt es dafür **keine Rechnung**. Das /ITS/-Programm klassifiziert nur Material (Abschnitt 3). Ein Stammdatenfeld gibt es mit `LFM1-LFABC` „ABC-Kennzeichen“ je Lieferant und EKORG. | `LFM1-LFABC` ist in T76 bei 144 Lieferanten in EKORG 1100 gefüllt: 63 A, 80 B, 1 C. | Wer `LFM1-LFABC` pflegt und nach welcher Regel, ist unbekannt. Die Rechnung im Dashboard nach 70/90 ist die vorgesehene Lösung (Abschnitt 3.8). |
| Liefertreue (Termin) | `EKET-SLFDT` „Statistikrelevantes Lieferdatum“, `EKET-EINDT` „Lieferdatum der Position“ und `EKBE` mit `VGABE = '1'` (Wareneingang), `BUDAT`, `BWART` 101/102/122 und `SHKZG`. Fertige Statistik: `S012-TABW1..5` (Lieferterminabweichung in den Intervallen aus `T147H`) und `S013-PWTT1/2` (Punktwert Termintreue). | `SLFDT` ist in allen 5'927 EKET-Einteilungen ab 2026 gefüllt, in 688 davon weicht es von `EINDT` ab. `eketSet` kennt die Property `Slfdt` bereits (MPC `CM00X` Zeile 103). | Welcher Wareneingang zählt (erster, letzter, mengengewichtet, Storno) und ob SLFDT oder EINDT die Basis ist, ist offen (Abschnitt 5). |
| Mengentreue | `EKET-MENGE/WEMNG`, `EKBE-MENGE`. Statistik: `S012-MABW1..5` (Intervalle `T147L`) und `S013-PWMT1/2`. Fiori-Kriterium 02 „Menge“. | Die Texte stehen in DD04T. `T147L` EKORG 1100: −50 / −10 / +10 / +50 %. | Toleranz und Regel sind fachlich offen. |
| Preis | Klassisch Hauptkriterium 01 „Preis“ mit Teilkriterien „Preisniveau“ (KRTYP 4) und „Preisverhalten“ (KRTYP 5) aus der Einkaufsstatistik. Fiori-Kriterium 04 „Preis“. | In ELBP sind 970 Sätze mit HKRIT 01 vorhanden (Durchschnitt 72,6). Im Fiori-Customizing ist Preis mit **0 %** gewichtet (`SEWS_CRITRN_WT-EVALUATIONPRICEWEIGHT 0.00`). | Ob Preis in Fiori bewusst weggelassen ist, sollten NTT und der Einkauf klären. |
| Service | Klassisch Hauptkriterium 04 „Service“, Teilkriterium 01 „Zusammenarbeit“, KRTYP leer = „Keine automatische Ermittlung“ (also manuell). Ein Fiori-Gegenstück wäre der Fragebogen (Kriterium 05). | Nur 58 ELBP-Sätze mit HKRIT 04. Der Fragebogen ist mit 0 % gewichtet. | Ob „Servicegrad“ künftig über den Fragebogen kommt, ist offen. |
| Qualität / Q2 | `QMEL` mit `QMART = 'Q2'` („Mängelrüge an Lief.“, TQ80_T), `QMEL-LIFNUM` „Kontonummer des Lieferanten“, `QMEL-MATNR`, `QMEL-EBELN`, `QMEL-EKORG`, `QMEL-ERDAT`, `QMEL-QMTXT`. Der Status steht in `JEST` (I0068/I0070 offen, I0072 abgeschlossen, I0076 Löschvormerkung). Klassisch HKRIT 02 (WE-Prüfung KRTYP 7, Reklamation KRTYP 8). Fiori-Kriterien 01 „Prüflos“ und 06 „Qualitätsmeldung“. | Es gibt 9'893 Q2-Meldungen, LIFNUM ist immer gefüllt, MATNR fehlt bei 187. 99 sind offen (Abschnitt 6). | `QmMeldungSet` liefert heute nur Q1. Für Q2 braucht es eine Erweiterung (eigener Plan). |
| Klassische Bewertung | ME61 bis ME6H, Tabellen `ELBK` (Kopf, Gesamtnote `GESBU`), `ELBP` (Note je Hauptkriterium `BEURT`, Teilnoten `KRT01..20`), `ELBM` (materialbezogen), Customizing `T147*`. LIS `S013` „EKS: Statistik für Lieferantenbeurteilung“. | 4'992 ELBK-Sätze, EKORG 1100, aber **Noten gibt es nur vom 05.01.2026**: 1'328 Lieferanten mit `GESBU` > 0, Benutzer ROT. Alle älteren 3'657 Sätze haben die Note 0. Details in Abschnitt 4. | Ob der Lauf über einen Job, ME63 oder ME6G kommt, ist in T76 nicht belegt: T76 führt Jobschritte (341'494 TBTCP-Sätze ab 2026), aber keine mit `RM06L*` oder `RMMEBTCH`. Die P76-Nutzung ist nicht geprüft. |
| Fiori-Bewertung (S/4) | Apps F1661 (Menge), F1662 (operativ), F1663 (Preis), F1664 (Zeit), F2309/F3295 (Qualität), F2234 (Fragebogen), F2019 (gesamt), F5061 (Punktzahlausgabe), F3811/F5062 (Historie), alle in TSTC/TSTCT. Tabellen: `SEWS_CRITERIA(_C)`, `SEWS_CRITRN_WT`, `SEWS_SCORES(_C)`, `SEWS_VAR_SCORE`, `MMPUR_ANA_SECONF`, `MMPUR_ANA_CRTCON`, Punkthistorie `MMPUR_ANA_SESCR` (Monat × Lieferant × Warengruppe) und `MMPUR_ANA_SESAGR` (aggregiert, mit `SUPPLIER_CLASSIFICATION`), Laufinfo `MMPUR_ANA_RUNINF`. | Das Customizing ist gepflegt (NTT_NOFE, 27.10.2026): Prüflos, Menge, Zeit und Qualitätsmeldung je 25 %, Preis und Fragebogen 0 %. Die Punkthistorie ist **leer** (SESCR, SESAGR, RUNINF und SE haben 0 Sätze). Details in Abschnitt 7. | Der Historienjob ist in T76 noch nicht gelaufen. Ob in P76 Punktzahlen persistiert werden, ist ungeprüft. Fiori-Info folgt am 01.12. |

## 2. Systemstand

T76 ist ein **S/4HANA-System**. Belegt ist das durch CVERS mit `S4CORE` Release 108 SP 0001, `S4FND` 108, `SAP_BASIS` 758 SP 0003 und `SAP_GWFND` 758 sowie durch `RFC_SYSTEM_INFO` mit Datenbank HDB auf travt763 und Kernel 793. `PRDVERS` (installierte Produktversionen, `INSTSTATUS +`) nennt „SAP S/4HANA 2023“, „SAP S/4HANA FOUNDATION 2023“, „SAP FIORI FOR SAP S/4HANA 2023“, „SAP FIORI FES 2023 FOR S/4HANA“ und „ABAP PLATFORM 2023“.

## 3. ABC-/XYZ-Analyse `/ITS/CA_MAT_ABC_*` (vertieft)

### 3.1 Transaktionen, Paket, Herkunft

| Transaktion | TSTC | TSTCT (D) |
| --- | --- | --- |
| `/ITS/CA_MAT_ABC_CU1` | Parametertransaktion, `TSTCP`: `/*SM30 VIEWNAME=/ITS/CA_MAX_V;UPDATE=X` | ABC-/XYZ-Analyse - Schwellenwerte |
| `/ITS/CA_MAT_ABC_CU2` | Parametertransaktion, `/*SM30 VIEWNAME=/ITS/CA_MAX_C2;UPDATE=X` | ABC-/XYZ-Analyse - Ablage der Werte |
| `/ITS/CA_MAT_ABC_XYZ` | Report `/ITS/CA_MAT_ABC_XYZ`, Dynpro 1000 | ABC- und XYZ-Analyse |

`CU1` ist also nur die Pflege der Schwellenwerte über SM30. Gerechnet wird mit der Transaktion `/ITS/CA_MAT_ABC_XYZ`, also dem gleichnamigen Report.

Das Paket ist `/ITS/CA_MAT_ABC_XYZ` mit dem Text „AN.00120 ABC- und XYZ-Analyse für Materialstamm“ (TDEVCT). Sein Oberpaket ist `/ITS/DI_1_CA_GEN`, die Softwarekomponente `HOME`, angelegt am 27.08.2012 von JKF (TDEVC). TADIR nennt `SRCSYSTEM SFD` und als Autoren JKF, ASK, MSCH, THT, GAN, DE26379 und DE28948, die Objekte stammen aus den Jahren 2016 bis 2020.

Zur Herkunft sagt der Quelltextkopf: „itelligence AG Zusatzentwicklung“, Produkt AN.01515 (S/4) bzw. 00120 (ECC), Version `AN.01515_ABC-XYZ_V1.5.0`, mit Lizenzprüfung über `/IT000/CL_LICENCE_CHECK` (Include `_S4` Zeilen 2–26, F01 Zeilen 577–590). In `TRNSPACE` von T76 gibt es keinen Eintrag `/ITS/`. Gesucht wurde mit `NAMESPACE LIKE '/I%'`, das lieferte 26 Treffer, `/ITS/` war nicht darunter. Dass itelligence heute zu NTT Data Business Solutions gehört, ist eine **Vermutung** und nicht aus SAP belegt.

### 3.2 Customizing (Inhalt T76)

`/ITS/CA_MAX_C` „Customizing I - ABC- / XYZ-Analyse Material“ hat den Schlüssel `WERKS` und genau einen Satz:

| WERKS | SCOPE | VALAB | VALBC | VALXY | VALYZ | VALCN | VALCNX | WOCHV |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1100 | H | 80.00 | 95.00 | 0.60 | 1.20 | leer | leer | T |

Die Bedeutungen kommen aus DD04T und DD07T. `VALAB` ist der Schwellenwert A–B, `VALBC` der Schwellenwert B–C (in %, kumuliert). `VALXY` und `VALYZ` sind Schwellen für den Variationskoeffizienten. `SCOPE H` bedeutet „alle Materialien mit Verbrauch im Auswertezeitraum“. `VALCN`/`VALCNX` leer heißt „keine Bewertung“ für Materialien ohne Umsatz. `WOCHV T` bedeutet, dass Wochenverbräuche „in Relation zu anteiligen Arbeitstagen“ auf die Monate verteilt werden.

`/ITS/CA_MAX_C2` „Customizing II“ enthält `TABNAME = ZCA_MAT_ABC_XYZ`. Das ist die Ablagetabelle für XYZ und die Zeiträume.

### 3.3 Datenbasis

Belegt aus dem Quelltext `/ITS/CA_MAT_ABC_XYZ_F01`; die Zeilen sind Quelltextzeilen.

- **Materialauswahl:** `MARC` INNER JOIN `MARA` je Werk aus `s_werks`, mit den Filtern Material, Materialart und Warengruppe (Zeilen 57–64).
- **Verbrauch:** Gelesen wird `MVER` (Zeile 86), wahlweise ergänzt um den Dispobereichsverbrauch `DVER` über `MDLV` (Häkchen `p_dispo`, Zeilen 100 und 2025–2075). Je Monat zählt `MGVnn` „Manuell korrigierter Gesamtverbrauch“. Ist dieser Wert 0, wird `GSVnn − UNVnn` genommen, also Gesamtverbrauch minus ungeplanter Verbrauch (Zeilen 449–471, Texte DD04T `MGVBR`, `GSVBR`, `UNVBR`).
- **Wert:** Verbrauch × Preis aus `MBEW` geteilt durch `PEINH` (Zeilen 124–130 und 187–196). Der Preis ist der Standardpreis `STPRS` bei `VPRSV = 'S'` bzw. der gleitende Preis `VERPR` bei `V`. Hat ein Material keinen MBEW-Satz, bleibt der Preis leer und `PEINH = 1`.
- **Nicht gelesen** werden EKPO, EKBE, MSEG/MATDOC, S012/S013 und LIFNR (Suche über alle Includes, 0 Treffer). **Der Lieferant spielt keine Rolle.** Es ist eine reine Material-ABC nach Verbrauchswert, nicht nach Einkaufsvolumen.

### 3.4 Zeitraum

Die Parameter sind `p_datum` (Vorschlag `sy-datum`) und `p_dauer` (Vorschlag 12) (E01 Zeilen 10–11). `calculate_periods` (F01 Zeilen 598–613) legt die Endperiode auf den **Monat von `p_datum`, einschließlich**, die Anfangsperiode liegt `p_dauer − 1` Monate davor. Mit den Vorschlagswerten ergibt das die letzten 12 Kalendermonate einschließlich des laufenden, angebrochenen Monats. Von- und Bis-Periode lassen sich auf dem Selektionsbild auch direkt überschreiben (E01 Zeilen 20–39, Prüfung mindestens 2 Perioden).

Für den letzten Lauf in T76 steht in `ZCA_MAT_ABC_XYZ` (`/ITS/CA_M_A_FROM/FROY/TILL/TILY`) bei 2'906 von 4'329 Materialien der Zeitraum **04.2025 bis 03.2026**. Bei den übrigen stehen ältere Zeiträume, zum Beispiel 253 mit 05.2024–04.2025 und 200 mit 02.2024–01.2025. Erklären lässt sich das aus dem Quelltext: Bei `SCOPE H` wird ein Material ohne Verbrauch im Auswertezeitraum übersprungen (F01 Zeile 175) und behält seine alte Klasse samt altem Zeitraum. Das betrifft auch `MaraAbc` im Dashboard, dort können also Klassen aus älteren Läufen stehen.

### 3.5 Klassengrenzen ABC

Gerechnet wird je Werk (F01 Zeilen 262–281). Die Materialien werden absteigend nach Wert sortiert, für jedes wird der eigene Anteil `eproz` am Gesamtwert ermittelt. Die Klasse hängt am **kumulierten Anteil vor dem Material** (`lf_gproz`, erst danach wird der eigene Anteil addiert):

- unter `VALAB` (80) wird es A,
- unter `VALBC` (95) wird es B,
- sonst C.
- Ein Material ohne Verbrauch bekommt `VALCN` (leer).

Das Material, das die Grenze überschreitet, bleibt damit in der oberen Klasse. Das ist **dieselbe Grenzfallregel wie im Entwurf V6** („der Lieferant, der die Grenze überschreitet, bleibt in der Klasse“, EINKAUF_SITZUNG 5.2).

**Rechenbeispiel** mit erfundenen Werten zur Regel, keine SAP-Daten. Fünf Objekte mit den Anteilen 50, 25, 15, 6 und 4 %:

| Objekt | Anteil | kumuliert vorher | /ITS/ mit 80/95 | Armin mit 70/90 |
| --- | ---: | ---: | --- | --- |
| 1 | 50 % | 0 % | A | A |
| 2 | 25 % | 50 % | A | A (überschreitet 70, bleibt A) |
| 3 | 15 % | 75 % | A | B |
| 4 | 6 % | 90 % | B | C (90 ist nicht kleiner als 90) |
| 5 | 4 % | 96 % | C | C |

### 3.6 XYZ-Logik

Grundlage ist der Variationskoeffizient der Monatsverbräuche im Zeitraum, also Standardabweichung durch Mittelwert. Die Zahl der Monate ist `gf_n` (E01 Zeile 35), die Formel steht in F01 Zeilen 200–216. Die Einstufung ist X bei höchstens `VALXY` (0,60), Y bei höchstens `VALYZ` (1,20), sonst Z (Zeilen 218–225). Ohne Verbrauch gilt `VALCNX`.

### 3.7 Ziel der Ergebnisse, Lauf

- **ABC** schreibt nach `MARC-MAABC` über `BAPI_MATERIAL_SAVEDATA` (`PLANTDATA-ABC_ID`, F01 Zeilen 803–823). Das geschieht nur, wenn das Häkchen `p_abc` gesetzt ist.
- **XYZ** und die Zeiträume landen per `MODIFY` in der Tabelle aus `/ITS/CA_MAX_C2`, in T76 also `ZCA_MAT_ABC_XYZ`. Die Felder sind `/ITS/CA_M_MAXYZ`, `/ITS/CA_M_A_FROM/FROY/TILL/TILY` und `/ITS/CA_M_X_*` (F01 Zeilen 855–897). Das geschieht nur mit dem Häkchen `p_xyz`. Ohne die Häkchen ist der Lauf eine reine Simulation mit ALV-Liste.
- **Bestand in T76:** `MARC-MAABC` ist nur in Werk 1100 gefüllt, mit 432 A, 718 B und 8'201 C. `ZCA_MAT_ABC_XYZ` hat 4'329 Sätze in Werk 1100, davon 623 X, 737 Y, 2'946 Z und 23 leer. Warum es 9'351 ABC-Werte, aber nur rund 4'300 XYZ-Sätze gibt, ist nicht geklärt.
- **Wie gestartet:** T76 führt Jobschritte (341'494 TBTCP-Sätze ab 01.01.2026), aber keinen mit `PROGNAME LIKE '/ITS/%'`. Es gibt auch keinen Job mit „ABC“ im Namen (`TBTCO`), keine Variante (`VARID`) und keine Änderungsbelege mit `TCODE = '/ITS/CA_MAT_ABC_XYZ'` (`CDHDR`). Die MAABC-Änderungsbelege in T76 (`CDPOS` MARC/MAABC, 247 Belege) kamen alle von **Benutzer ALE_VC mit `TCODE MM02(ALE)`**: 1 am 13.05.2026, 129 am 30.06.2026 und 117 am 30.09.2026.
  - Belegt ist damit nur, dass MAABC seit Mai 2026 in T76 per ALE geändert wird.
  - Die 9'351 Werte standen schon vorher da, ältere MAABC-Belege gibt es in T76 nicht. Woher der Grundbestand kommt (Kopie aus P76 oder früherer Lauf), ist ungeklärt.
  - Zwei Punkte sind nur **Vermutung**: dass der ALE-Sender P76 ist, und dass dort quartalsweise gerechnet wird (Lauf zum Quartalsende). Belegen lässt sich das nur in P76 (TBTCP/VARID nach Programmname), und dafür gibt es keine Freigabe.

### 3.8 Bezug zum Dashboard und zur A/B/C je Lieferant

Das Dashboard liest heute genau diese Material-Klassen. `MaraAbc` ist `MARC-MAABC` in Werk 1100 über `MARCSet`, `MaraXyz` ist `ZCA_MAT_ABC_XYZ-/ITS/CA_M_MAXYZ` über `ZSTR_MAT_XYZSet` (`Services/PurchasingDataRefreshService.cs` Zeilen 972–1012). Mit einer Lieferanten-A/B/C haben sie nichts zu tun.

Für Armins A/B/C je Lieferant (A bis 70 %, B weitere 20 %, C 10 % nach Einkaufsvolumen) lässt sich das Programm **nicht direkt nutzen**. Es kennt keinen Lieferanten, rechnet nach Verbrauchswert statt nach Einkaufsvolumen, schreibt in den Materialstamm und hat Schwellen je Werk für Material. Übernehmen lassen sich zwei Dinge:

- die Grenzfallregel (Abschnitt 3.5, sie entspricht V6),
- auf Wunsch das Zeitfenster „12 Monate bis Stichtag“ als Vorschlag für Entscheid E1.

Bei der Übernahme sind zwei Unterschiede zu beachten. Das Programm nimmt den laufenden, angebrochenen Monat mit. Die Schwellen sind 80/95, nicht 70/90. Das passt zum Stand in Wochen_Todo 122 („A/B/C je Lieferant 70/20/10 aus dem Cache“, Bestätigung Armin): Gerechnet wird im Dashboard.

## 4. Klassische Lieferantenbeurteilung (ME61 bis ME6H)

Transaktionen (TSTC/TSTCT, Sprache D):

| Transaktion | Text | Programm |
| --- | --- | --- |
| ME61 | Pflegen Lieferantenbeurteilung | SAPMM06L |
| ME62 | Anzeigen Lieferantenbeurteilung | SAPMM06L |
| ME63 | Beurteilung automat. Teilkriterien | RM06LAUB |
| ME64 | Beurteilungsvergleich | RM06LSIM |
| ME65 | Beurteilungslisten | RM06LBEU |
| ME6A | Änd. zur Lieferantenbeurteilung | RM06LA00 |
| ME6B | Liefer.beurteil. zum Material anz. | RM06LB00 |
| ME6C | Lieferanten ohne Beurteilung | RM06LC00 |
| ME6D | Lieferanten nicht beurteilt seit | RM06LD00 |
| ME6E | Beurteilungssätze ohne Gewichtung | RM06LE00 |
| ME6F | Drucken | RM06LF00 |
| ME6G | Lieferantenbeurteilung im Hintergr. | RMMEBTCH |
| ME6H | Standardanalyse Lieferantenbeurtlng. | RMCE0130 |

Die Einträge belegen nur, dass die Transaktionen existieren, nicht dass sie genutzt werden. In T76 stecken ME61 bis ME6H in den Rollen `Z_LOGI_ALLES_RW`, `Z_LOGI_ALLES_RW_CZ` und `Z_LOGI_ALLES_RW_TEMP` (`AGR_TCODES`).

**Customizing EKORG 1100** (die Texte stammen aus T147B, T147D, DD04T und DD07T):

| Hauptkriterium (T147B) | Teilkriterium (T147D) | Ermittlung (T147I-KRTYP, DD07T) | Gewicht Teilkriterium (T147C) |
| --- | --- | --- | --- |
| 01 Preis | 01 Preisniveau | 4 automatisch aus Einkaufsstatistik: Preisniveau | 1 |
| 01 Preis | 02 Preisverhalten | 5 automatisch aus Einkaufsstatistik: Preisverhalten | 1 |
| 02 Qualität | 01 WE-Prüfung | 7 automatisch aus Qualitätsstatistik: WE-Prüfung | 1 |
| 02 Qualität | 02 Reklamation | 8 automatisch aus Qualitätsstatistik: Qualitätsmeldungen | 1 |
| 03 Lieferung | 01 Termintreue | 3 automatisch aus Termintreuestatistik | 1 |
| 03 Lieferung | 02 Mengentreue | 2 automatisch aus Mengentreuestatistik | 1 |
| 04 Service | 01 Zusammenarbeit | leer: keine automatische Ermittlung | 100 |

Für die Gewichtung der Hauptkriterien hat EKORG 1100 nur den Gewichtungsschlüssel 01 „Gleichgewichtung“ (`T147E` mit Gewicht 1 je Kriterium 01 bis 04). Alle 4'992 ELBK-Sätze tragen `GEWSL 01`. `T147` für 1100 enthält: `MAXBW 100` („Beste Note“), `STAGE 365` („Geltungsdauer“), `WELFZ 010` („Normierungswert Lieferterminabweichung“), `WEPRZ 10.0` („Mindestliefermenge in Prozent“), `QMUAT 10.0`, Kalender `CH`, Formular `ZZ_MELDRUCK`. Die Bedeutung von `LFABW 0.20`, `MGABW 0.30` und `VVABW 0.25` ist ungeprüft, weil DD04T dazu keinen Text liefert.

Die Statistikintervalle sind: `T147H` (Lieferterminabweichung in Tagen) für 1100 −5 / −2 / +2 / +5 und `T147L` (Mengenabweichung in %) −50 / −10 / +10 / +50. `T147K` „Zuordnung Prozentwerte - Punktwerte“ ordnet je Ermittlungsmethode (`KRTYP`) und Vorzeichen (`KRVRZ`) einem Prozentsatz (`KRPRZ`, DD04T „Prozentsatz für ein Teilkriterium“; `KRPAC` ist der Wert gepackt mit Vorzeichen) eine Note (`KRPKT`) zu. Das ist das vorhandene SAP-Punktesystem für Termin und Menge, in **Prozent, nicht in Tagen**. Für EKORG 1100 gilt:

| KRTYP | Vorzeichen | KRPRZ bis | KRPAC | Note |
| --- | --- | ---: | ---: | ---: |
| 2 Mengentreue | + | 0 / 20 / 100 / 150 / 200 / 300 / 999 | 100.0 / 102.0 / 110.0 / 115.0 / 120.0 / 130.0 / 199.9 | 100 / 100 / 100 / 90 / 70 / 40 / 10 |
| 2 Mengentreue | − | 10 / 20 / 50 / 100 / 150 / 200 / 999 | 99.0 / 98.0 / 95.0 / 90.0 / 85.0 / 80.0 / 0.1 | 100 / 100 / 100 / 85 / 70 / 50 / 1 |
| 3 Termintreue | + | 200 / 500 / 999 | 120.0 / 150.0 / 199.9 | 100 / 50 / 10 |
| 3 Termintreue | − | 200 / 500 / 999 | 80.0 / 50.0 / 0.1 | 100 / 85 / 60 |

Wie SAP die Terminabweichung in Prozent umrechnet (Bezug auf `T147-WELFZ` oder die Lieferzeit), wurde nicht gelesen.

**Daten in T76:**

- `ELBK` hat 4'992 Sätze, alle EKORG 1100, keine Löschvormerkung. Nach Jahr von `LEBDT`: 2004 1'642, …, 2024 89, 2025 8, 2026 1'335. Der jüngste Wert ist 20260105. Die Bewerter (`LEBEW`) sind ROT 2'049, GUN 2'942 (Jahre bis 2019) und MSO 1.
- **Noten gibt es nur vom 05.01.2026.** Alle 3'657 Sätze mit `LEBDT` vor 2026 haben `GESBU` 0. Von den 1'335 Sätzen vom 05.01.2026 haben 1'328 eine Note. Ob die Sätze von 2024 und 2025 (10.01.2024: 89, 06.01.2025: 8, beide ROT) echte Bewertungsläufe waren, deren Noten später überschrieben oder zurückgesetzt wurden, ist nicht belegt. Dass Anfang Januar regelmäßig bewertet wird, ist eine **Vermutung**.
- In T76 gibt es Jobschritte (341'494 TBTCP-Sätze ab 2026), aber keinen mit `RM06L*` (ME63 bis ME6F) oder `RMMEBTCH` (ME6G).
- `ELBP` hat 3'317 Sätze, alle mit `LEBDT 20260105`. Nach Hauptkriterium sind es: 01 Preis 970 (Durchschnitt 72,6), 02 Qualität 1'123 (95,7), 03 Lieferung 1'166 (71,0), 04 Service 58 (84,3). Eine Historie gibt ELBP in T76 nicht her, es stehen nur die aktuellen Noten darin.
- Von den 1'328 Gesamtnoten (`ELBK-GESBU`) aus 2026, die größer als 0 sind, liegen 618 bei 85 oder mehr, 396 zwischen 70 und 84 und 314 darunter. Der Durchschnitt ist 78,0.
- `ELBM` (materialbezogen) hat 0 Sätze.
- **Nachgerechnet**, Lieferant 65001: ELBP liefert Preis 90 (KRT01 90, KRT02 90), Qualität 100 (100/100) und Lieferung 92 (KRT01 87, KRT02 97, also (87+97)/2 = 92). Bei Gleichgewichtung ergibt das (90+100+92)/3 = 94,0, und das ist genau `ELBK-GESBU 94`. Service hat keinen Satz und zählt deshalb nicht mit.
- **LIS:** `S013` „EKS: Statistik für Lieferantenbeurteilung“ hat 137'274 Sätze, EKORG 1100, VRSIO 000, SPMON von 2002 bis 2026-04, monatlich rund 450 bis 590 Sätze. Kennzahlen (DD04T): `PWTT1/2` Termintreue, `PWMT1/2` Mengentreue, `PWEV1/2` Versandvorschrift, `PWQA1/2` Qualitätsaudit, `PWWE1/2` und `PWFR1/2` Qualität und Termin der Leistung, `LAVI1/2` Lieferavis. `S012` „Einkauf“ wird ebenfalls monatlich fortgeschrieben (Abschnitt 1). Der Inhalt der Kennzahlen wurde nicht ausgewertet. **Widerspruch, ungeklärt:** Lieferant 65001 hat in ELBP eine Termintreue 87 vom 05.01.2026, in S013 aber nur Sätze bis 2008-10. Woraus ME63 die Teilnoten rechnet (S013, Belege oder Zeitraum `T147-STAGE`), wurde nicht gelesen. Ein Lieferant mit S013-Sätzen 2026 ist zum Beispiel 70369.

## 5. Liefertreue nach Marcos Idee (statistisches Lieferdatum gegen Wareneingang)

**Felder** (DD03L/DD04T): `EKET-SLFDT` (DATS) „Statistikrelevantes Lieferdatum“, `EKET-EINDT` „Lieferdatum der Position“, `EKET-MENGE`, `EKET-WEMNG`. Dazu `EKBE-VGABE` (Domäne VGABE: 1 = Wareneingang, 2 = Rechnungseingang …), `EKBE-BEWTP`, `EKBE-BUDAT`, `EKBE-CPUDT`, `EKBE-BWART`, `EKBE-SHKZG` und `EKBE-LFBNR` (Referenzbeleg; über ihn lassen sich Stornos ihrem Ursprung zuordnen). `EKBE-ETENS` heißt „Laufende Nummer Lieferantenbestätigung“. Ein Feld `ETENR` hat EKBE nicht (DD03L EKBE, aktive Version), ein Bezug zur Einteilung besteht damit nicht.

In der Stichprobe (EKBE, `VGABE = '1'`, BUDAT im Januar 2026, 1'007 Zeilen) gab es nur die Kombinationen `BEWTP E` mit BWART 101 (S, 983), 102 (H, 15) und 122 (H, 9).

**Was das Dashboard heute liest:**

- `eketSet` mit `Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng` (`Services/PurchasingDataRefreshService.cs` Zeilen 311 und 390). `Slfdt` liest es nicht.
- Die Property `Slfdt` gibt es im Modell aber schon (MPC `ZCL_ZPOWERBI_EINKAUF_MPC` Methode `DEFINE_EKET`, Include `CM00X`, Quelltextzeile 103). Die DPC_EXT liest `SELECT * FROM eket` mit Filtern auf EBELN, EBELP und EINDT (`CM010` Zeile 67). **Für SLFDT genügt deshalb vermutlich, `Slfdt` in `$select` aufzunehmen, ohne SAP-Änderung.** Geprüft ist das nur im Quelltext, nicht per Gateway-Aufruf.
- Ein `EKBESet` gibt es im Modell (`DEFINE_EKBE`, `CM00A`). Es hat aber nur 9 Properties (Budat, Mandt, Ebeln, Ebelp, Gjahr, Belnr, Buzei, Bwart, Menge), **ohne VGABE und SHKZG**. Die DPC_EXT liest `SELECT * FROM ekbe` **ohne jeden Filter** (`EKBESET_GET_ENTITYSET`, `CM008` Zeile 12). So ist das Set für den Abruf nicht brauchbar, weil es die ganze EKBE lesen würde. Für Wareneingangsdaten braucht es einen eigenen gefilterten Zweig, also einen Schreibauftrag nach Plan.
- Der Reiter Lieferperformance zeigt bewusst „Ist-Termin-Abdeckung 0 %, Quelle fehlt“ (`docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md`, Abschnitt Lieferperformance).
- `operations/kpi/liefertreue` (OTD/OTIF/OTP) misst **Kundenaufträge** über die Query `2CCSDSLSORDDLVPRFQ` (`docs/OPERATIONS_KPI_2026-10-14.md` Zeile 78), nicht Lieferanten.

**Beispiele** (T76, echte Positionen, alle EKORG 1100, Werk 1100, Einheit ST laut `EKPO-MEINS`). Die Abweichung ist das Buchungsdatum des Wareneingangs minus Datum. Marcos Beispielfenster ist −5 bis +1 Tag = grün.

| Bestellung/Pos. | Lieferant, Material | Menge | SLFDT | EINDT | Wareneingänge (EKBE VGABE 1) | Abw. zu SLFDT | Abw. zu EINDT |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 45145501/10 | 66383, E01227 | 5'000 ST | 15.01.2026 | 15.01.2026 | 16.01.: 101 5'000 (Beleg 5001244496), Storno 102 5'000 (5001244519, LFBNR 5001244496), dann 101 2'000 + 2'000 + 1'000 (5001244520) | +1 Tag, grün | +1, grün |
| 45143145/10 | 70563, C29596 | 2'592 ST | 24.10.2025 | 24.10.2025 | 15.01.2026: 101 2'592 (5001244306), Storno 102 2'592, dann 3 × 101 864 (5001244311) | +83 Tage, rot | +83, rot |
| 45144688/10 | 65188, Textposition ohne Material | 3 ST | 12.12.2025 | 16.01.2026 | 16.01.2026: 101 3 | +35, rot | 0, grün |
| 45144688/30 | 65188, ohne Material | 1 ST | 12.12.2025 | 19.01.2026 | 16.01.2026: 101 1 | +35, rot | −3, grün |
| 45144688/20 | 65188, ohne Material | 2 ST | 12.12.2025 | 19.01.2026 | 13.02.2026: 101 2 | +63, rot | +25, rot |

Nachgerechnet: 12.12.2025 bis 16.01.2026 sind 19 + 16 = 35 Tage. Bis 13.02.2026 sind es 19 + 31 + 13 = 63 Tage. 24.10.2025 bis 15.01.2026 sind 7 + 30 + 31 + 15 = 83 Tage.

Die Beispiele zeigen die Entscheide, die vor einer Liefertreue-Kennzahl fallen müssen:

1. SLFDT oder EINDT als Basis? Bei 45144688 entscheidet das zwischen grün und rot.
2. Welcher Wareneingang zählt: der erste, der letzte oder mengengewichtet?
3. Wie werden Stornos (102/122 über LFBNR) behandelt?
4. Was geschieht mit Positionen ohne Material?
5. Wie wird bei mehreren Einteilungen je Position zugeordnet? EKBE kennt keine Einteilungsnummer.

SAP selbst rechnet in S012 (TABW-Intervalle −5/−2/+2/+5 aus T147H) und in S013 (Punktwert Termintreue) bereits fertige Termintreue-Statistiken. Wie genau SAP dort zuordnet, wurde nicht gelesen.

## 6. Q2-Meldungen

- **Meldungsart:** `TQ80_T`: Q1 Kundenreklamation, **Q2 „Mängelrüge an Lief.“**, Q3 interne Problemmeldung. `TQ80-STSMA` ist für Q1, Q2 und Q3 leer, es gibt also kein Anwenderstatusschema. In `JSTO` hat nur ein einziges QM-Objekt ein Schema (`QM000000100311`, `QM_N_001`, aus dem Q1-Bestand).
- **Felder** (DD03L/DD04T): `QMEL-LIFNUM` „Kontonummer des Lieferanten“, `QMEL-MATNR`, `QMEL-EBELN`/`EBELP`, `QMEL-EKORG`, `QMEL-MAWERK`, `QMEL-ERDAT`, `QMEL-QMDAT`, `QMEL-QMTXT` (Kurztext), `QMEL-RKMNG` (reklamierte Menge), `QMEL-MGEIN`, `QMEL-PRUEFLOS`, `QMEL-MBLNR`/`MJAHR` und `QMEL-OBJNR`.
- **Bestand T76:** 9'893 Q2-Meldungen, `KZLOESCH` nie gesetzt.
  - `LIFNUM` ist in allen gefüllt. `MATNR` fehlt bei 187, `EBELN` bei 1'733.
  - EKORG 1100 haben 8'164, 1'729 sind leer.
  - Ab 2025 gibt es 121 Meldungen, davon 3 ohne Material.
  - Nach Jahr: 2017 148, 2018 110, 2019 143, 2020 160, 2021 129, 2022 97, 2023 101, 2024 142, 2025 97, 2026 24 (bis 07.04.).
- **Status** (aktive `JEST`-Einträge, `INACT` leer):
  - abgeschlossen: 9'762 mit I0072 MMAB und 32 mit MMAB + I0076 LOEVM,
  - **offen 99**: 84 mit I0070 MIAR „in Arbeit“ und 15 mit I0068 MOFN „offen“, keine mit I0069 MRST.
  - Die offenen stammen zu großen Teilen aus 2005 bis 2013 (62 Stück); aus 2026 sind 10 offen. Für das Dashboard ist deshalb eine Altersgrenze oder eine Bereinigung zu klären.
- Die Regel „offen“ entspricht der in `saptasks/qm_meldungen/NOTES.md` Abschnitt 3 für Q1 (nicht I0072 und nicht I0076). `QmMeldungSet` liefert heute nur Q1. Q2 bräuchte einen eigenen Zweig oder einen Parameter mit Lieferant (`LIFNUM`) und Bestellung (`EBELN`). Das ist ein Schreibauftrag und braucht zuerst einen Plan.

## 7. Fiori-Lieferantenbewertung (S/4)

- **Apps** (TSTC/TSTCT, Sprachen D/E): F1661/F1661A nach Menge, F1662/F1662A operativ, F1663/F1663A nach Preis, F1664/F1664A nach Zeit, F2309/F2309A und F3295/F3295A nach Qualität, F2234/F2234A Fragebogen, F3842A benutzerdefinierte Kriterien, F2019 gesamt, F5061 Punktzahlausgabe, F3811/F5062 Punkthistorie.
  - In `AGR_TCODES` von T76 stehen sie in keiner Rolle. Gesucht wurde mit `TCODE LIKE 'F166%'` und der Liste F2019/F2234/F2309/F3295/F5061/F5062/F3811, Ergebnis 0. Fiori-Apps werden aber meist über Kataloge zugeordnet, deshalb sagt das über die Zuordnung nichts. Kataloge und Rollen wurden nicht gelesen.
- **Registrierte OData-Dienste:** In TADIR gibt es Service-Gruppen (`IWSG`) im Kundenpaket `ZFIORI`. Das belegt, dass sie registriert sind, nicht dass sie aktiv sind oder genutzt werden.
  - 13 Gruppen wurden am 27.11.2022 von OSM angelegt (u. a. `ZC_SUPLREVALBYQUALITY_CDS_0001`, `ZC_SUPLREVALSCORECARDDISPLAY_SRV_0001`, `ZC_SUPLREVALUSRDFNDCRITERIA_CDS_0001`).
  - 3 weitere legte CAT am 13.05.2026 an (`ZC_SUPLREVALBYQNAIREQRY`, `…BYQUALITYQRY`, `…BYQUANTITYQRY`).
  - Am 27.05.2026 kam `ZC_SUPLREVALCHANGEDSCORES_CDS_0001` dazu, in Paket ZPP, und am 17.09.2026 `ZC_SUPLREVALSCRHISTJOBMNTR_CDS_0001` (Job-Monitor Punkthistorie).
  - Dazu kommt `ZC_OPERATIONALSUPLREVALQRY_CDS_0001` in ZFIORI; Autor und Datum wurden nicht gelesen.
  - Dass CAT und NTT_NOFE NTT-Berater sind, ist eine **Vermutung**.
- **Customizing in T76:**
  - `SEWS_CRITERIA_C`, zuletzt geändert von NTT_NOFE am 27.10.2026: Kriterien 01 „Prüflos“, 02 „Menge“, 03 „Uhrzeit“ (Domäne `MM_CRITERION`, gemeint ist Zeit/Termin) und 06 „Qualitätsmeldung“, je 25 %, aktiv.
  - `SEWS_CRITRN_WT` (ohne Einkaufskategorie und EKORG): Zeit 25, Menge 25, Qualität 25, Qualitätsmeldung 25, Preis 0, Fragebogen 0, Summe 100.
  - `SEWS_SCORES_C` und `SEWS_VAR_SCORE` enthalten Abweichungs-zu-Punkte-Tabellen. Für Zeit (03) gilt zum Beispiel: 0 → 100, 1 → 95, 2 → 90, 3 → 80, 7 → 50, 14 → 20, 21 → 0, −1 → 90. Ob die Einheit bei Zeit Tage oder Prozent ist, ist **ungeprüft**: `SEWS_VAR_SCORE` führt für 03 die Spannen 99–121 als Prozent.
  - `SEWS_SWITCH_C` Schalter 1 „Lieferantenbewertung nach Einkaufskategorie“ ist aktiv (X). Die kategoriebezogenen Tabellen `SEWS_CRITERIA` und `SEWS_SCORES` sind aber leer. Welche Gewichtstabelle beim Rechnen wirkt, ist deshalb ungeprüft.
  - `MMPUR_ANA_SECONF` 1 „Bestellungen“ ist aktiv.
  - `MMPUR_ANA_CRTCON`: `QTYVAR` = `SET` (Endlieferkennzeichen).
- **Persistierte Punktzahlen:** Es gibt eigene Tabellen, aber in T76 sind sie **leer**. `MMPUR_ANA_SESCR` „Lieferantenbewertung operative Note persistieren“ hat den Schlüssel JOB_RUN_ID, Jahr, Monat, SUPPLIER, MATERIALGROUP und die Felder `TIMEVARIANCESCORE`, `QUANTITYVARIANCESCORE`, `PRICEVARIANCESCORE`, `INSPECTIONLOTQUALITYSCORE`, `QUALITYNOTIFICATIONSCORE` und `SUPPLIEROPERATIONALSCORE`. `MMPUR_ANA_SESAGR` „aggregierte Punktzahlen“ hat Durchschnitte und `SUPPLIER_CLASSIFICATION`. `MMPUR_ANA_RUNINF` (Läufe mit Von/Bis-Datum), `MMPUR_ANA_SE` (Analysedaten je Bestellposition mit erstem und letztem WE-Datum) und `MMPUR_ANA_JOBTRK` sind ebenfalls leer.
- **Folgerung:** Sobald der Historienjob läuft, kann das Dashboard den SAP-Score aus `MMPUR_ANA_SESCR`/`SESAGR` lesen, ohne selbst zu rechnen. Das passt zum Entscheid „Logik in SAP, Dashboard zeigt an“. Das ist eine **Vermutung**, solange keine Sätze da sind. Sonst bleiben nur die OData-Dienste der Apps (CDS-Abfragen, die zur Laufzeit rechnen; nicht geprüft).

## 8. Offene Punkte

1. **P76 (Freigabe Ingo nötig, nur lesend):**
   - Gibt es in P76 Sätze in `MMPUR_ANA_SESCR`/`SESAGR`/`RUNINF`?
   - Sind die Fiori-Dienste aktiv und Rollen zugeordnet?
   - Läuft der klassische Lauf über einen Job (`TBTCP` mit `RM06LAUB`/`RMMEBTCH`)?
   - Wann und wie oft läuft `/ITS/CA_MAT_ABC_XYZ` (`TBTCP`/`VARID` nach Programmname)?
   - Wie viele Q2 sind offen?
2. **Fachlich mit dem Einkauf bzw. NTT:**
   - Preis 0 % und Fragebogen 0 % in Fiori bewusst?
   - Wird die klassische Bewertung (Januar-Lauf) abgelöst?
   - Liefertreue: SLFDT oder EINDT, welcher WE zählt, Umgang mit Stornos?
   - EVO als Bestell-, WE- oder Rechnungswert, Zeitraum (E1)?
3. **Technisch, jeweils ein eigener Plan (Schreibauftrag T76):**
   - Q2 im `QmMeldungSet` oder als eigenes Set.
   - Gefilterter WE-Zweig statt des ungefilterten `EKBESet`.
   - `Slfdt` in den Dashboard-Abruf `eketSet` aufnehmen (Code im Dashboard, keine SAP-Änderung).
4. **Ungeprüft geblieben:**
   - Woraus ME63 die Teilnoten rechnet (Widerspruch S013 und ELBP bei Lieferant 65001).
   - Wie T147K die Terminabweichung in Prozent fasst.
   - Bedeutung von `T147-LFABW/MGABW/VVABW`.
   - Einheit der Fiori-Zeitabweichung.
   - Inhalt der S012/S013-Kennzahlen.
   - Sender der ALE-Materialänderungen.

## 9. Abfragen (zum Wiederholen)

Alle Abfragen liefen als `RunSapProbe.ps1 table-read <Tabelle> --fields '…' --where "…" --rowcount n --format csv --quiet`, Quelltext per `abap-read`. Die Bedingungen in `--where` sollten kurz bleiben. Drei Fallen sind dabei aufgetreten:

- Eine geklammerte Bedingung mit `OR` und `IN` über mehr als eine Zeile (`(TABNAME LIKE 'ELB%' OR … IN (…)) AND …`) brach mit `OPTION_NOT_VALID` ab. Kurze `IN`-Listen gingen.
- `TMDIR` hat das Feld `CLASSNAME`, nicht `CLSNAME`. Ein falscher Feldname in `--where` meldet ebenfalls `OPTION_NOT_VALID`.
- `CDPOS` mit `VALUE_NEW`/`VALUE_OLD` meldete `DATA_BUFFER_EXCEEDED` (Zeile über 512 Byte). Mit nur `OBJECTID,CHANGENR` ging es.

Methoden-Includes: Der Klassenname wird mit `=` auf 30 Zeichen aufgefüllt, also `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM008` und `ZCL_ZPOWERBI_EINKAUF_MPC======CM00A`. Den Methodenindex liefert `TMDIR` (`METHODINDX`, Basis 36).
