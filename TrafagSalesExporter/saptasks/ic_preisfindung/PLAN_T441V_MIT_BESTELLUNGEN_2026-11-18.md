# Plan: T441V „Mit Bestellungen“ fuer Pruefgruppe 02 / Pruefregel AE in T76/100 von X auf A (Vergleichstest Fabio)

Stand: 2026-11-18 18:13 (Phase 2 ausgefuehrt 18:06 bis 18:12, Abschnitt 5a: **BESTP fuer 02 / AE seit 18:11:06 auf A**,
Auftrag T76K912928 lokal und offen; **Rueckbau auf X steht aus**, Abschnitt 5); davor Phase 1, nur gelesen (17:53 bis 18:06),
Agent sap-t76. Auftrag Ingo 18.11.2026 abends im Chat, Wunsch Fabio Palma. Die Freigabe Ingos gilt genau fuer diese eine
Feldaenderung in T76/100; eine Produktivaenderung ist ausdruecklich nicht Teil des Auftrags. Phase 2 lief im selben Lauf,
weil Phase 1 jede Haltebedingung aus Abschnitt 2a abgehakt hat.

Ablageort: `saptasks/ic_preisfindung/`, weil es derselbe Testfall ist wie am Mittag (IC Trafag AG 1100 an Trafag AT 1200,
Bestellart NB mit Lieferwerk 1100; `PLAN_T161V_ATPCONFMRP_2026-11-18.md`). Thema ist die Verfuegbarkeitspruefung.

## 1. Was verlangt ist

Tabelle T441V (Umfang der Verfuegbarkeitspruefung), Mandant 100, Pruefgruppe 02, Pruefregel AE, Feld „Mit Bestellungen“
von **X** („Einbeziehen (fuer UB Auftragsmenge verwenden)“) auf **A** („Einbeziehen (fuer UB bestaetigte Menge verwenden)“).
Alle anderen Felder der Zeile und alle anderen Zeilen bleiben. Nach dem Test wieder X (auf Zuruf Ingos, nicht Teil dieses Laufs).

Testfall Fabio: Kundenauftrag 411403 Position 10, Material 52704, Werk 1200, Bestellung 45148518 Position 10. Mit X zaehlt
CO09 60 Stueck am 04.12.2026 aus Einteilung 0001; die bestaetigten 60 Stueck stehen auf Einteilung 0002 mit WE-Ende 16.12.2026.

## 2. Ist-Stand T76/100 (alles per RFC gelesen, 18.11.2026 17:53 bis 18:06)

| Punkt | Befund | Beleg |
| --- | --- | --- |
| Tabelle | T441V, transparent, mandantenabhaengig, Auslieferungsklasse G | DD02L `TABCLASS TRANSP`, `CLIDEP X`, `CONTFLAG G` |
| Schluessel | MANDT, MTVFP (Pruefgruppe), PRREG (Pruefregel) | DD03L T441V, `KEYFLAG X` |
| Feld | `BESTP`, CHAR 1, Position 7, Datenelement und Domaene `BESTP_APPL`, Langtext „Mit Bestellungen“, Kurzbeschreibung „Verfuegbarkeitspruefung: Mit Bestellungen und Lieferplaenen“ | DD03L, DD04T (D) |
| Festwerte | leer = „Ausschliessen“; **X = „Einbeziehen (für UB Auftragsmenge verwenden)“**; **A = „Einbeziehen (für UB bestätigte Menge verwenden)“** (englisch „Include (for STO, use order quantity)“ / „Include (for STO, use confirmed quantity)“). Es gibt genau diese drei Werte | DD07T `DOMNAME BESTP_APPL`, Sprache D und E; DD07L |
| Zielzeile | genau **eine** Zeile 100 / 02 / AE: **BESTP X**, FERTP X, PLAFP A, VBBDP X, VBBLP X, OWBZP X, WZTWE 000, alle uebrigen Felder leer | `T441V_02_AE_VORHER_2026-11-18.txt` Abschnitt 1 |
| Ganze Tabelle | 75 Zeilen in Mandant 100 (Pruefgruppen 01, 02, CH, DR, Y2, Z2); BESTP = A steht heute in keiner Zeile | VORHER-Datei Abschnitt 2 |
| Pflegesicht | `V_441VC` „Umfang der Verfuegbarkeitspruefung“: Sichtvariante (DD25L `VIEWCLASS V`) auf die Pflegesicht `V_441V` (Funktionsgruppe 0PP1, Uebersichtsbild 0220, Detailbild 0221); enthaelt BESTP (DD27S Position 7). Transaktion **OVZ9** ruft `SM30 VIEWNAME=V_441VC;UPDATE=X`; IMG-Aktivitaet `SIMG_CFMENUOLSDOVZ9` | DD25L, DD25T, DD26S, DD27S, DD28S (Selektionsbedingung `TMVF-ADV_ATP NE 'A'`), TVDIR (`V_441V`), TSTC/TSTCP OVZ9; CUS_ACTOBJ nur fuer Aktivitaet und Transaktion (die Zeile fuehrt die Aktivitaet unter dem Objekt `V_441V`). In E071K kommen **beide** Sichten vor: `V_441VC` z. B. in T76K912181 (`100Z2AE`, 30.09.2026) und T76K912183, `V_441V` z. B. in T76K912167 (`09002IC`, 25.09.2026) und T76K903213 (`10002AE`, 2008); gewaehlt ist `V_441VC`, weil OVZ9 sie aufruft |
| Nicht Teil der Sicht | `QUANTITY_USED_FOR_STO` („Menge in ATP-Pruefung fuer ULB“, C = bestaetigte Menge), `PAC_BUCKET_LOGIC`, `NO_BATCH_CHECK` stehen nur in `V_441V`/`V_441VA` (erweiterte Verfuegbarkeitspruefung). Das ist **ein anderes Feld** und wird nicht angefasst; Pruefgruppe 02 hat `TMVF-ADV_ATP` leer (klassische Pruefung) | DD27S, DD04T, TMVF |
| Auftragspflicht | Mandant 100: Kategorie T, `CCCORACTIV 1` (Aenderungen werden aufgezeichnet), also Abfrage Customizing-Auftrag | T000 |
| Sperren | Schluessel `10002AE` steht nur in T76K903213 (I001067, 03.09.2008, **R**, „Verfuegbarkeitspruefung (Sibe)“). Kein offener Auftrag mit diesem Schluessel | E071K `OBJNAME = 'T441V'` (91 Zeilen), E070, E07T |
| Nachbarn | Offene Aufgaben mit anderen T441V-Schluesseln: T76K911043 (LAA, „ATP-Editing“, `10002PP`), T76K912183 (TRIM_DAAD, „PP: ATP-Pruefung“, `10002PP` und `100Y2*`), T76K912187 (TRIM_DAAD, Mandant 090). `09002AE` nur in freigegebenen Auftraegen (T76K908015/016, T76K910033/034). Keiner beruehrt `10002AE` | E071K, E070, E070C, E07T |
| Eigener Testauftrag vom Mittag | T76K912926 / Aufgabe T76K912927 (W/Q, D, Ziel leer, KOI): wird **nicht** verwendet und nicht freigegeben | E070 |
| T161V F/NB/1100 | unveraendert zum Stand NACHHER vom Mittag (ATPCONFMRP leer, 11 Zeilen) | gelesen 18:00 |

### 2.1 Trifft der Testfall die Zeile 02 / AE? Ja.

| Frage | Befund | Beleg |
| --- | --- | --- |
| Material | Im System `000000000000052704` (18 Stellen mit fuehrenden Nullen) | MARC |
| Pruefgruppe im pruefenden Werk | Werk 1200: `MTVFP 02`; Werk 1100: `MTVFP 02` (dort Gesamtwiederbeschaffungszeit `WZEIT 70`, Sonderbeschaffung leer, Beschaffung E) | MARC `MATNR 000000000000052704`, WERKS 1200 und 1100 |
| Position 411403/10 | Auftragsart TA, Verkaufsorganisation 1200, angelegt 18.11.2026 von FPA; Position: Material 52704, **Werk 1200**, Typ TAN, 60 ST, Bedarfsart `BEDAE KEV`, **Sonderbestand `SOBKZ E`**, `MTVFP 02` | VBAK, VBAP |
| Wird geprueft? | Bedarfsart KEV gehoert zur Bedarfsklasse 045 mit `ATPPR X` (Verfuegbarkeitspruefung), `BEDSD X`, Kontierungstyp M; Einteilungstyp CP mit `ATPPR X` | T459A, T459K, TVEP |
| Pruefregel | Die SD-Pruefung baut die Pruefregel aus Aufrufer und Sonderbestand: `p_atpcs-prreg = tmvfp-obvfp. p_atpcs-prreg+1(1) = p_mverf_pos-sobkz.` Der Aufrufer ist beim Kundenauftrag A (`RV_AVAILABILITY_CHECK`, Parameter `AUFRUFER ... DEFAULT 'A'`; Konstante `CL_ATP_S4_C=>CHECKING_RULE_SALES_ORDER = 'A'`). A + E = **AE**. Text der Regel: AE „SD-Auftrag Einzelbestand“, A „SD-Auftrag“, B „SD-Lieferung“, IC „Intercompany“ | Quelltext `LV03VFA7` Zeile 68/69, `LV03VU01` Zeile 5, `CL_ATP_S4_C` (CU) Zeile 21; TMVFP 02 mit OBVFP A, B, C; T441W (D) |
| Einteilungen des Auftrags | 0001: Wunsch 60 am 26.11.2026, bestaetigt 0; 0002: bestaetigt 60 am 04.12.2026. Deckt sich mit Fabios „60 Stueck am 04.12.2026“ | VBEP, VBBE (`SOBKZ E`) |
| Bestellung 45148518/10 | Belegart **NB**, Lieferant 70953, **Lieferwerk 1100**, angelegt 18.11.2026 von FPA; Position: Material 52704, Werk 1200, 60 ST, Kontierungstyp M, Sonderbestand E, kontiert auf 411403/10, WE-Bearbeitungszeit 10 Tage | EKKO, EKPO, EKKN |
| Einteilungen der Bestellung | 0001: Lieferdatum 18.11.2026, **Menge 60**, bestaetigte Menge (`MNG02`) 0; 0002: Lieferdatum 01.12.2026, Menge 0, **bestaetigte Menge 60** | EKET |

Hergeleitet, nicht an der Aufrufstelle gelesen: dass der Verkaufsbeleg `RV_AVAILABILITY_CHECK` mit dem Vorschlagswert A
ruft (Aufruf in SAPMV45A nicht gelesen). Gestuetzt wird es durch den Regeltext „SD-Auftrag Einzelbestand“ und dadurch, dass
der Auftrag mit dem heutigen Umfang 02/AE (Bestellungen einbezogen) auf den 04.12.2026 bestaetigt ist.

Erwartung vorher/nachher am Beispiel (**Vermutung**, bis Fabios CO09 es zeigt): Mit X zaehlt die Pruefung die Auftragsmenge
der Einteilung 0001 (60 ST, heute sichtbar am 04.12.2026); mit A die bestaetigte Menge der Einteilung 0002 (60 ST,
WE-Ende laut Fabio 16.12.2026), der Auftrag waere dann erst zum spaeteren Termin bestaetigbar. Ob „fuer UB“ bei einer
Bestellung der Belegart NB mit Lieferwerk greift, sagt der Festwerttext nicht; genau das zeigt der Test. Die Daten 04.12. und
16.12. sind nicht gegen den Fabrikkalender nachgerechnet (T76 weicht im Dezember von P76 ab, LEARNINGS 2026-11-17).

### 2.2 Wer nutzt die Zeile 02 / AE sonst, und die Schwesterzeilen

Die Zeile gilt in T76/100 fuer jede Kundenauftragsposition mit Kundeneinzelbestand (Sonderbestand E) und Material der
Pruefgruppe 02, in allen Werken, und fuer jede CO09-Abfrage mit Pruefregel AE; sie ist nicht auf den Testfall begrenzt.

| Zeile | Regeltext | BESTP heute |
| --- | --- | --- |
| 02 / A | SD-Auftrag | X |
| 02 / AE | SD-Auftrag Einzelbestand | **X** (Ziel: A) |
| 02 / B | SD-Lieferung | leer |
| 02 / IC | Intercompany | X |

Die drei Schwesterzeilen werden nur gelesen und bleiben.

## 2a. Haltebedingungen (alle abgehakt, Phase 2 darf laufen)

- [x] Festwert A mit dem Text „Einbeziehen (fuer UB bestaetigte Menge verwenden)“ und X „Einbeziehen (fuer UB Auftragsmenge verwenden)“ vorhanden (DD07T).
- [x] Genau eine Zeile 100 / 02 / AE, Feld BESTP heute X.
- [x] Pflegesicht belegt (`V_441VC` ueber OVZ9 bzw. SM30), sie enthaelt BESTP.
- [x] Mandant 100 zeichnet Customizing auf (T000), Abfrage Customizing-Auftrag ist zu erwarten.
- [x] Schluessel `10002AE` in keinem offenen Auftrag (E071K/E070).
- [x] Pruefgruppe des Materials 52704 im pruefenden Werk 1200 ist 02 (MARC, VBAP).
- [x] Pruefregel der Position 411403/10 ist AE (Sonderbestand E, Quelltext), nicht nachweislich eine andere.
- [x] T76K912926 wird nicht verwendet; kein Auftrag wird freigegeben.
- [x] DPAPI-Ablage vorhanden (`Test-SapPassword`: T76, koi, seit 10.09.2026), SAP Logon laeuft (`Get-Process saplogon`), `info` 18:00: nur `ses[0]` SESSION_MANAGER.

Haltebedingungen waehrend Phase 2: 02/AE in der Sicht nicht sichtbar oder nicht aenderbar (dann **nicht** auf `V_441V`
ausweichen); Feld laesst A nicht zu; die Tabelle weicht unmittelbar vor `neu` vom Stand 17:54 ab; E- oder W-Meldung ausser
der erwarteten Warnung zum lokalen Auftrag; Sperrmeldung; Anzeigemodus; Anmeldebild; unerwartetes Fenster; abgelehnter Befehl.
Dann F12, verwerfen, eigene Sitzung schliessen, anhalten und berichten.

## 3. Schritte Phase 2 (genau diese, nichts dazu)

Rahmen: kein Passwort; GUI nur in eigener Sitzung (`SapGuiT76Hilfe.vbs neu`, nur die gemeldete Id, Ingos `ses[0]` nie);
Ausgabe von cscript in Dateien; P76 und Mandant 090 werden weder gelesen noch beruehrt.

| Nr | Schritt | Erwartet |
| --- | --- | --- |
| 0 | Ganze Tabelle T441V noch einmal per RFC lesen und gegen den Stand 17:54 vergleichen; T161V ebenso | zeichengleich, sonst anhalten |
| 1 | `info`, dann `neu`, dann `info` | vorher nur `ses[0]`, nachher genau eine Sitzung mehr mit neuer Id |
| 2 | In der eigenen Sitzung `/nSM30`, Sicht `V_441VC`, zuerst **„Anzeigen“**: Uebersichtsbild und Detailbild 02/AE ausmessen (Ids, Feldtypen), zurueck | Bild „Sicht "Umfang der Verfuegbarkeitspruefung" anzeigen“; evtl. Hinweis „mandantenabhaengig“ (S/I) |
| 3 | `/nSM30`, `V_441VC`, **„Pflegen“** | Aendern-Modus, Liste der 75 Zeilen; moegliches Fenster „Positionieren“ nur, wenn selbst aufgerufen |
| 4 | Zeile bestimmen: derselbe Zeilenindex traegt Pruefgruppe `02` und Pruefregel `AE`; Detailbild oeffnen | Detailbild mit Pruefgruppe 02, Pruefregel AE |
| 5 | Auf dem Detailbild alle Felder lesen (Ankreuzfelder ueber `.Selected`, Auswahllisten ueber `.Key`) und gegen VORHER vergleichen: BESTP X, FERTP X, PLAFP A, VBBDP X, VBBLP X, OWBZP X, WZTWE 000, Rest leer | gleich, sonst anhalten |
| 6 | **Nur** „Mit Bestellungen“ auf A setzen (Auswahlliste: `.Key = "A"`; Eingabefeld: Text A); Enter; alle Felder noch einmal lesen | nur dieses Feld anders; keine E/W-Meldung |
| 7 | Sichern (Strg+S). Dialog „Abfrage Customizing-Auftrag“: Vorschlag **nicht** uebernehmen (auch wenn T76K912926 oder T76K912750 vorbelegt ist), „Auftrag anlegen“ (F8) | Dialog „Auftrag anlegen“ |
| 8 | Kurzbeschreibung `T441V 02/AE Mit Bestellungen X->A Test Fabio NICHT FREIGEBEN` (**60 Zeichen**, mit `.Length` gezaehlt, ohne Umlaut; LEARNINGS 2026-10-29). **Zielsystem leeren** (lokaler Auftrag, LEARNINGS 2026-10-20). Sichern; neue Nummer aus dem Abfragedialog lesen, sie darf weder T76K912926 noch T76K912750 sein; in E070 nachlesen (W, D, Ziel leer, KOI); erst dann bestaetigen | Warnung „Lokaler Auftrag … wird nicht transportiert“ (einzige erlaubte W-Meldung), danach „Daten wurden gesichert“ |
| 9 | Uhrzeit mit `Get-Date` ablesen | – |
| 10 | Auftrag **nicht** freigeben. Eigene Sitzung schliessen (`zu`), `info`: nur noch Ingos Sitzung | – |

## 4. Rueckleseprobe (RFC, nach Schritt 8)

1. Zielzeile mit vollem Schluessel: `table-read T441V --where "MTVFP = '02' AND PRREG = 'AE'"` mit allen 31 Feldern.
   Erwartet: `100,02,AE,,,,A,X,,A,,,,X,X,X,,,000,,,,,,,,,,,,` (nur die 7. Stelle BESTP ist A statt X).
2. Ganze Tabelle mit demselben Befehl wie vorher (`MTVFP LIKE '%'`, `--rowcount 5000 --format csv`): weiter **75 Zeilen**;
   Vergleich gegen die Vorher-Ausgabe zeilenweise, Ergebnis des Vergleichs selbst ausgewertet: genau eine Zeile anders,
   darin genau ein Feld.
3. Auftrag: `E070` neuer Auftrag W, D, `TARSYSTEM` leer, KOI; Aufgabe darunter; `E070C` Mandant 100; `E071` der Aufgabe nur
   `VDAT V_441VC`; `E071K` genau ein Schluessel `T441V` / `10002AE`. Steht dort mehr (z. B. ein Textschluessel), im Bericht nennen.
4. T161V F/NB/1100 und die ganze T161V: zeichengleich mit `T161V_F_NB_1100_NACHHER_2026-11-18.txt`.
5. T76K912926/927 unveraendert (D, Ziel leer, E071K nur `100FNB  1100`).
6. Stand ablegen: `T441V_02_AE_NACHHER_2026-11-18.txt`.

## 5. Rueckbau (spaeter, nur auf Zuruf Ingos)

SM30, Sicht `V_441VC` (oder OVZ9), „Pflegen“, Zeile 02 / AE (vor dem Setzen Pruefgruppe, Pruefregel und den Wert A auf dem
Detailbild lesen), „Mit Bestellungen“ wieder auf X, sichern. Kommt der Auftragsdialog, den lokalen Auftrag dieses Laufs
**T76K912928** (Abschnitt 5a) ausdruecklich setzen, nie T76K912926 und nie einen Auftrag mit Ziel P76; der Dialog schlaegt den
zuletzt benutzten Auftrag vor (am 18.11. abends war es T76K912926). *Vermutung:* Der Dialog bleibt aus,
weil Sicht und Schluessel schon in der eigenen offenen Aufgabe stehen (so am 11.11. bei V/06 Z002); dann nach dem Sichern in
E071K pruefen, dass `10002AE` weiter nur in dieser Aufgabe steht. Rueckleseprobe: Zielzeile und ganze Tabelle zeichengleich
mit `T441V_02_AE_VORHER_2026-11-18.txt` (BESTP = X, 75 Zeilen). Der lokale Auftrag wird nie freigegeben; nach dem Rueckbau
kann Ingo ihn loeschen oder stehen lassen.

## 5a. Ausfuehrung Phase 2 (2026-11-18, 18:06 bis 18:12)

**Ergebnis: ausgefuehrt und rueckgelesen. „Mit Bestellungen“ (BESTP) fuer Pruefgruppe 02 / Pruefregel AE steht in T76/100
seit 18:11:06 auf A** (mit `Get-Date` abgelesen direkt nach „Daten wurden gesichert“; der Klick dazu lag zwischen 18:11:04
und 18:11:06; als Stuetze E070-AS4TIME der Aufgabe: 18:11:04 Serverzeit). Auftrag **T76K912928** (Customizing, lokal ohne
Ziel, offen, nicht freigegeben), Aufgabe T76K912929. P76 und Mandant 090 nicht gelesen und nicht beruehrt. Kein Befehl
abgelehnt, kein Anmeldebild, keine E-Meldung, kein unerwartetes Fenster.

| Nr | Ist |
| --- | --- |
| 0 | 18:06:13 T441V (75 Zeilen) und T161V (11 Zeilen) erneut gelesen: beide zeichengleich mit 17:54 bzw. mit `T161V_F_NB_1100_NACHHER_2026-11-18.txt` |
| 1 | `info` vorher: nur `ses[0]` (SESSION_MANAGER, Ingo). `neu`: „vorher=1 nachher=2“, eigene Sitzung Index 1, Id `/app/con[0]/ses[1]`. Nur dort gearbeitet |
| 2 | `/nSM30`, `V_441VC`, „Anzeigen“: Bild „Sicht "Umfang der Verfuegbarkeitspruefung" anzeigen: Uebersicht“ (SAPL0PP1, Dynpro 220), ohne Rueckfrage, **„Eintrag 1 von 72“** (nicht 75: die Sicht blendet die drei Zeilen der Pruefgruppe Y2 aus, Selektionsbedingung `TMVF-ADV_ATP NE 'A'`, DD28S; Y2 hat `ADV_ATP A`). 12 Zeilen je Seite; 02/AE ist Eintrag 23. Detailbild (Dynpro 221) ausgemessen: „Mit Bestellungen“ ist eine **Auswahlliste** `cmbV_441V-BESTP`, Schluessel X |
| 3 | `/nSM30`, `V_441VC`, „Pflegen“: „… aendern: Uebersicht“, ohne Rueckfrage und ohne Meldung |
| 4 | Auf Eintrag 23 geblaettert; Zeile 0 traegt Pruefgruppe 02, Pruefregel AE, Text „SD-Auftrag Einzelbestand“ (vom Skript vor dem Oeffnen geprueft); F2 oeffnet „… aendern: Detail“ mit Pruefgruppe 02 „Einzelbedarf“, Pruefregel AE |
| 5 | Alle 22 Eingabeelemente gelesen (Ankreuzfelder ueber `.Selected`, Auswahllisten ueber `.Key`): BESTP X „X Einbeziehen (für UB Auftragsmenge verwenden)“, FERTP X „Alle“, PLAFP A „Nur fixierte“, VBBDP und VBBLP gesetzt, OWBZP X, TBBEP und ARREP leer „Ausschliessen“, alle uebrigen Ankreuzfelder leer, WZTWE leer: gleich VORHER. Nicht am Bild (nur per RFC verglichen, sieben Felder): QUANTITY_USED_FOR_STO, PAC_BUCKET_LOGIC, NO_BATCH_CHECK, VWBEST, PRIBED sowie QUANTITY_DIST und ENABLE_SUPPLY_PROTECTION (die beiden letzten stehen laut DD27S in der Sicht, das Detailbild 221 zeigt sie aber nicht) |
| 6 | Nur `cmbV_441V-BESTP` auf Schluessel A („A Einbeziehen (für UB bestätigte Menge verwenden)“); Enter ohne Meldung; alle Elemente erneut gelesen: einzig diese Auswahlliste anders (45 Zeilen vorher/nachher verglichen) |
| 7 | Sichern 18:09: Dialog „Abfrage Customizing-Auftrag“ fuer `V_441VC`, Auftragsfeld **mit T76K912926 vorbelegt** (Testauftrag vom Mittag); nicht uebernommen, „Auftrag anlegen“ (F8) |
| 8 | Dialog „Auftrag anlegen“: Customizing-Auftrag, Inhaber KOI, Quellmandant 100, Ziel **P76 vorbelegt** und geleert, Text 60 Zeichen zurueckgelesen. Neue Nummer **T76K912928**; vor dem Bestaetigen in E070 gelesen (W, D, Ziel leer, KOI; Aufgabe T76K912929). Nach „Weiter“ das Fenster „Warnung: Lokaler Auftrag T76K912928 wird nicht transportiert“, bestaetigt; Statuszeile „Daten wurden gesichert“ (S) |
| 9 | 18:11:06 (`Get-Date`); Serverzeit der Aufgabe 18:11:04 |
| 10 | Auftrag nicht freigegeben. Am Bild nach dem Sichern noch einmal gelesen: 02 / AE, Schluessel A. `zu 1`: „Sitzungen jetzt=1“, `info`: nur `ses[0]` SESSION_MANAGER |

Rueckleseprobe (RFC, 18:11:27 bis 18:11:29), Einzelheiten in `T441V_02_AE_NACHHER_2026-11-18.txt`:

- Zielzeile: `100,02,AE,,,,A,X,,A,,,,X,X,X,,,000,,,,,,,,,,,,`, wie in Abschnitt 4 erwartet.
- Ganze Tabelle: 75 Zeilen vorher und nachher, dieselben 75 Schluessel; je Schluessel verglichen genau eine geaenderte Zeile,
  darin nur Feld 7 BESTP X -> A. `Compare-Object` zeigt dieselbe eine Zeile. BESTP = A steht nur in 02/AE.
- T161V: 11 Zeilen, zeichengleich mit dem Stand NACHHER vom Mittag (F/NB/1100 ATPCONFMRP leer).
- Auftrag: E070 T76K912928 W/D/Ziel leer/KOI, T76K912929 Q/D; E070C beide 100; E071 nur `VDAT V_441VC`; E071K genau ein
  Schluessel `T441V` / `10002AE`. Kein Textschluessel, nichts Fremdes. T76K912926/927 unveraendert.

Abweichungen vom Plan (keine davon aendert, was geschrieben wurde):

1. Die Sicht zeigt 72 statt der im Plan genannten 75 Zeilen (Pruefgruppe Y2 gehoert zur Sicht `V_441VA`); die Zielzeile war
   sichtbar und aenderbar.
2. Ein zeilenweiser Vergleich nach Position taugt nicht: RFC_READ_TABLE lieferte die geaenderte Zeile nach dem Sichern am Ende
   der Ausgabe. Verglichen wurde deshalb nach Schluessel (und zusaetzlich als Menge mit `Compare-Object`).
3. Die Warnung zum lokalen Auftrag kam als eigenes Fenster „Warnung“ (wnd[2]) beim Bestaetigen der Auftragsabfrage, nicht
   in der Statuszeile.

Zur Wirksamkeit: T441V ist voll gepuffert (DD09L `PUFFERUNG X`), T76 hat genau einen Anwendungsserver (`TH_SERVER_LIST`:
travt762_T76_00). *Vermutung (Standardverhalten, nicht gemessen):* Die Aenderung gilt auf diesem Server ab dem Sichern ohne
Wartezeit; eine bereits offene Transaktion (z. B. ein vor 18:11 geoeffnetes CO09 oder VA02) kann den alten Umfang noch im
Speicher halten, Fabio sollte den Test also neu aufrufen.

## 6. Wirkung waehrend des Testfensters

Die Zeile gilt je Pruefgruppe und Pruefregel, nicht je Auftrag oder Benutzer: Ab dem Sichern wirkt A in T76/100 fuer alle
Kundenauftragspositionen mit Kundeneinzelbestand und Pruefgruppe 02, auch fuer andere Tester. Zusammen mit dem Test vom Mittag
(T161V F/NB/1100 ATPCONFMRP leer seit 12:06) sind damit zwei Schalter gleichzeitig gegenueber dem Ausgangsstand veraendert;
ein Vergleich „nur X gegen A“ gilt bei leerem ATPCONFMRP. P76 und Mandant 090 sind nicht betroffen und wurden nicht gelesen.

**Rueckbau ausgefuehrt 2026-11-20 (siehe `NOTES.md` 13):** zurueck auf X, rueckgelesen. Der Auftragsdialog kam trotz „Vermutung“ in Abschnitt 5 und schlug den falschen Auftrag vor; die Nummer wurde ausdruecklich gesetzt.

*Berichtigung 2026-11-20 09:03: Der Rueckbau-Vermerk oben ist fuer T441V ueberholt. 02/AE steht seit 09:02:55 wieder auf A (Ingo wollte nur T161V zurueck), siehe `NOTES.md` 13.*
