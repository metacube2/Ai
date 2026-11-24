# Zoll CZ: Quelltext Ursprungslandberechnung aus P76

Fuer Fabio (Analyse der Logik, um Zollkosten zu senken). Nur gelesen, in P76 nichts geaendert.

## Quelle

- System **P76, Mandant 100**, gelesen am **2026-11-24 um 13:03** (Dateizeit des ersten Downloads).
- Weg: SE38, Programm **anzeigen** (nicht aendern), Hilfsmittel > Weitere Hilfsmittel > Upload/Download > Download
  (Funktionscode `ED_DOWNLOAD`), Encoding 4110 (UTF-8). Eigene GUI-Sitzung, danach geschlossen.
- Dateien sind UTF-8 ohne BOM mit CRLF, unveraendert so, wie SAP sie geschrieben hat.
- Alle Aussagen unten stammen nur aus dem sichtbaren Quelltext in P76. Was nicht belegt ist, steht als „Vermutung“.

## Dateien

| Datei | Zeilen | Bytes |
| --- | --- | --- |
| `ZLO_CALC_URSPRUNGSLAND_2.abap` | 926 | 29'367 |
| `Z_MARA_DELTA_AUSN.abap` | 212 | 7'669 |
| Nachtrag 13:05 bis 13:15 (Freigabe Ingo „ja auch holen“): | | |
| `ZLO_UPDATE_MAPE.abap` (Funktionsbaustein, Include `LZZLO2U01`, mit Schnittstellenkommentar) | 54 | |
| `SAPLZZLO2.abap` (Rahmenprogramm der Funktionsgruppe `ZZLO2`) | 14 | |
| `LZZLO2TOP.abap` (globale Daten) | 17 | |
| `LZZLO2UXX.abap` (generierte Bausteinliste) | 6 | |
| `LZZLO2F01.abap` (Unterroutinen, Batch-Input) | 188 | |
| `ZLO_CALC_URSPRUNGSLAND_2_Texte.txt` (Selektionstexte 12, Textsymbole 16, Deutsch) | 35 | |
| `Z_MARA_DELTA_AUSN_Texte.txt` (Selektionstexte 10, keine Textsymbole, Deutsch) | 20 | |
| `ZSD_HERKL_Struktur.txt` (27 Felder) | 37 | |
| `ZSD_HERKL_CH_Struktur.txt` (4 Felder) | 14 | |
| `ZLO_URSPR_AUSN_Struktur.txt` (6 Felder) | 16 | |
| `ZSD_HERKL_CH_Inhalt.csv` (71 Datenzeilen plus Kopf) | 72 | |

**Includes:** Beide Programme binden nur das SAP-Standard-Include `<icon>` ein (Zeile 123 bzw. 33). Eigene Z- oder
Y-Includes gibt es nicht, der ganze Quelltext steht in den zwei Dateien. Die Funktionsgruppe `ZZLO2` des Bausteins hat
die Includes `LZZLO2TOP`, `LZZLO2UXX` (nur `LZZLO2U01`, also nur dieser eine Baustein) und `LZZLO2F01`; alle sind
heruntergeladen.

**Weiterhin nicht gelesen:** der im Kopf von `Z_MARA_DELTA_AUSN` erwaehnte „Upload-Report“ (Zeile 17), die englischen
Textelemente (nicht geprueft, ob vorhanden) und das Aenderungsdatum der beiden Reports.

## Nachtrag: Funktionsbaustein ZLO_UPDATE_MAPE (Funktionsgruppe ZZLO2)

SE37 zeigt: Funktionsgruppe `ZZLO2` „Präferenz-Berechnung“, Kurztext „Update MAPE (Zollpräferenzen)“, Paket `ZLO1`,
aktiv, letzte Aenderung 14.09.2020. Schnittstelle nur `TABLES MAPE_UEB STRUCTURE MAPE` und `PROTO STRUCTURE
WRPLCHK_GS_PROTO` (Zeilen 3 bis 6 der Datei), keine Import-, Export- oder Ausnahmeparameter.

Der Baustein vergleicht jede uebergebene Zeile mit der heutigen `MAPE` (Material, Werk, `GZOLX`) und nimmt sie nur
auf, wenn `PREFE`, `PREDA`, `PRENE` oder `PRENG` abweichen (`ZLO_UPDATE_MAPE.abap` Zeilen 23 bis 33). Je Werk baut er
eine Batch-Input-Folge fuer **MM02** (Bild `SAPLMAPE 0100`, Felder `MAPE-PREFE(nn)` und `MAPE-PREDA(nn)` usw., Sichern
mit `=BU`, `LZZLO2F01.abap` Zeilen 8 bis 114) und startet sie mit **`CALL TRANSACTION 'MM02' ... MODE 'N' UPDATE 'S'`**
(Zeilen 145 bis 148). Erfolg gilt nur bei Meldung 801 oder 810 vom Typ S (Zeilen 150 bis 156). Bei Misserfolg legt er
die Folge mit `BDC_OPEN_GROUP` (Mappe **`ZOLL-PRAEF`**), `BDC_INSERT` und `BDC_CLOSE_GROUP` als Batch-Input-Mappe
ab (Zeilen 119 bis 135, 158 bis 165). Die Vermutung „SM35-Mappe ZOLL-PRAEF“ ist damit **bestaetigt**, aber nur fuer
den Fehlerfall. Eigene `UPDATE`, `MODIFY`, `INSERT` oder `COMMIT WORK` auf Datenbanktabellen enthaelt die
Funktionsgruppe nicht; die Aenderung laeuft ueber die MM02-Verbuchung. Aufgerufen wird er nur mit `P_MATUP`
(`ZLO_CALC_URSPRUNGSLAND_2` Zeilen 917 bis 922).

## Nachtrag: Tabellen

| Tabelle | Kurztext | Art, Auslieferungsklasse | Schluessel | Felder | Eintraege in P76 |
| --- | --- | --- | --- | --- | --- |
| `ZSD_HERKL` | Ergebnis Herkunftslandberechnung | transparent, A | MANDT, MATNR, WERKS | 27 | 189 |
| `ZSD_HERKL_CH` | Zolltarifnummern fuer Herkunft CH | transparent, A | MANDT, STAWN, GZOLX | 4 | 71 |
| `ZLO_URSPR_AUSN` | Ausnahmen fuer Zollvergabe | transparent, C | MANDT, MATNR | 6 | 6'897 |

Quellen: Felder, Datenelemente, Typen und Kurztexte aus SE11 (Anzeigen); Schluesselkennzeichen aus SE16 `DD03L`
(`KEYFLAG = X`); Eintraege mit SE16 „Anzahl der Eintraege“ ohne Selektion. Alle drei sind in SE16/SM30 als
„Anzeige/Pflege erlaubt“ eingestellt.

Auffaellig, nur aus diesen Abzuegen:

- **`ZLO_URSPR_AUSN` hat `WERKS` nicht im Schluessel**, obwohl der Kopf von `Z_MARA_DELTA_AUSN` (Zeile 25)
  „MANDT / MATNR / WERKS (Schluessel)“ sagt. Ein Material kann also nur fuer ein Werk als Ausnahme stehen; ein
  zweites Werk wuerde der `INSERT ... ACCEPTING DUPLICATE KEYS` still uebergehen.
- **`ZSD_HERKL_CH`** (Datei `ZSD_HERKL_CH_Inhalt.csv`): 9 Warennummern (`STAWN`), Grenzwerte 40 (9 Zeilen),
  45 (8) und 50 (54). 8 Zeilen haben eine leere Praeferenzzone; das sind die Zeilen, die die Herkunftsregel
  `ermittlung_herkl_neu` liest (`gzolx = space`, Zeile 703). `8536.5000` hat keine solche Zeile. Einen Eintrag
  `STAWN = 'DUMMY'` gibt es nicht; fuer eine Warennummer ohne Zeile in der Tabelle setzt das Programm deshalb nie CH
  (Zeilen 703 bis 711) und gibt bei der Praeferenz immer D (Zeilen 903 bis 913).

**Letzte Aenderung:** In keinem der beiden Koepfe steht ein Datum. `ZLO_CALC_URSPRUNGSLAND_2` hat Aenderungskommentare
mit Kuerzel, aber ohne Datum (z. B. Zeile 204); `Z_MARA_DELTA_AUSN` nennt im Kopf „S/4HANA, S4CORE 108, SAP_BASIS 758“
(Zeile 27). Das Aenderungsdatum steht in der Versionsverwaltung bzw. in `REPOSRC` und wurde nicht gelesen.

## ZLO_CALC_URSPRUNGSLAND_2: Berechnung Ursprungsland

**Selektionsbild** (Zeilen 21 bis 42): Material `S_MATNR` (Pflicht), Werk `P_WERKS` (Pflicht), Stichtag `P_STTAG`
(Vorschlag heute), Faktor `P_FAKTOR` (Vorschlag 1.45) und die Ankreuzfelder `P_LAND`, `P_DETAIL`, `P_PRAEF`, `P_LANDD`,
`P_TABUP`, `P_MATUP`, `P_LOEKZ`, `P_AUSN`. Beschriftungen in `ZLO_CALC_URSPRUNGSLAND_2_Texte.txt`, z. B.
`P_TABUP` „Update Tabelle ZSD_HERKL“, `P_MATUP` „Materialstamm ändern“, `P_FAKTOR` „Verkaufspreis (Faktor HK)“.

**Gelesene Tabellen:** MARC mit `BESKZ` = E oder X oder `SOBSL` = 30, nicht `SOBSL` = 50 (Zeilen 140 bis 152); mit
`P_AUSN` ohne die Materialien aus `ZLO_URSPR_AUSN` fuer das Werk (Zeilen 130 bis 145). KEKO mit `KALKA` = 01, `KADKY` bis
Stichtag, `TVERS` = 1, nicht geloescht, `FEH_STA` ungleich KF (Zeilen 184 bis 191), dazu CKIS und CKIT (Zeilen 469 bis
496). Weiter MAKT, MAPE, LFA1 (Land des Lieferanten bei Fremdleistung, Zeile 549), CRHD (Arbeitsplaetze `CZ%` zaehlen
als Herkunft CZ, sonst CH, Zeilen 158 und 532 bis 543), T005/T005T, T604G mit Land CH (Zeile 162), T024D
(Disponentenname, Zeilen 782 bis 795) und `ZSD_HERKL_CH` (Grenzwert `PROZCH` je `STAWN`, Zeilen 703 bis 711).

**Rechenweg im Code, nicht nur laut Kopf.** Der Kopf (Zeilen 7 bis 11) nennt drei Regeln. Im Code gilt:

1. Ursprung CH, wenn der CH-Anteil `w_proz` groesser ist als `ZSD_HERKL_CH-PROZCH` fuer die `STAWN` des Materials,
   ersatzweise fuer den Eintrag `STAWN = 'DUMMY'` (Zeilen 703 bis 711). Die Grenze ist also ein Tabellenwert, keine
   feste 50 im Code.
2. Sonst, nur mit `P_LAND`: das Land mit dem hoechsten Eigenfertigungswert `fert_e` (Zeilen 744 bis 757).
3. Die dritte Regel aus dem Kopf (hoechster auslaendischer Wert) ist auskommentiert (Zeilen 726 bis 740).

Weitere Einzelheiten: Eine Komponente (CKIS-Typ M oder I) wird ueber ihre eigene Kalkulation aufgeloest, wenn
`ickis-ukaln` gefuellt ist, `MARC-BESKZ` nicht F ist und der Standort (aus dem Disponentennamen, „CZ“ oder sonst CH)
gleich ist; sonst zaehlt sie mit `MARC-HERKL` als Material (Zeile 519). Die Kostenart 93100 wird im Verhaeltnis der
CH- und CZ-Eigenleistung aufgeteilt (Zeilen 556 bis 576). Verkaufspreis `w_vpreis` = Stueckkosten mal `P_FAKTOR`; ist
`w_hawa` gesetzt (Disponentenname enthaelt „Einkauf“ oder „CZ“), wird `w_vpreis` gar nicht zugewiesen (Zeilen 258 bis
264). Die fruehere Preisermittlung aus Kundenauftraegen ist auskommentiert (Zeilen 628 bis 663). Mit `P_PRAEF` (und
`P_LAND`) wird je `GZOLX` aus T604G der Wert der Komponenten mit `MAPE-PREFE` = E gesammelt (Zeilen 600 und 802 bis 827),
und ein neues `PREFE` (C, wenn der Anteil die Grenze erreicht, sonst D) ermittelt (Zeilen 902 bis 926). Ausgabe ist eine
Liste je Material mit Anteilen je Herkunft und neuem Ursprungsland.

**Aendert Daten: ja, mit zwei Ankreuzfeldern.**

- `P_TABUP`: `MODIFY zsd_herkl` schreibt das Ergebnis in die Datenbanktabelle `ZSD_HERKL` (Zeilen 385 bis 388).
- `P_MATUP`: `MATERIAL_MAINTAIN_DARK` mit `TCODE` MM02 setzt **MARC-HERKL** im Materialstamm, wenn es vom berechneten
  Wert abweicht (Zeilen 378 bis 381 und 834 bis 897). Ausserdem gehen geaenderte `MAPE-PREFE` an den Z-Baustein
  `ZLO_UPDATE_MAPE` (Zeilen 430 bis 446, 917 bis 922). *(Nachtrag: Der Baustein ist inzwischen gelesen, siehe unten;
  er aendert die Praeferenzen per `CALL TRANSACTION 'MM02'` und legt im Fehlerfall die Mappe `ZOLL-PRAEF` an. Die
  Vermutung zur Meldung in Zeile 442 ist damit bestaetigt.)*
- Ohne `P_TABUP` und `P_MATUP` liest das Programm nur und gibt eine Liste aus; die uebrigen `MODIFY` (Zeilen 197, 372)
  betreffen interne Tabellen. Wer testen will: beide Felder leer lassen und vorher pruefen, dass keine gesicherte
  Variante sie vorbelegt.

## Z_MARA_DELTA_AUSN: Ausnahmetabelle aus dem MARA-Delta fuellen

**Zweck laut Kopf** (Zeilen 4 bis 22): fuellt monatlich als Hintergrundjob die Ausnahmetabelle `ZLO_URSPR_AUSN` mit neu
angelegten Materialien, damit sie nicht mehr per SM30 gepflegt werden muessen. Das ist dieselbe Tabelle, die
`ZLO_CALC_URSPRUNGSLAND_2` mit `P_AUSN` von der Berechnung ausschliesst (dort Zeilen 130 bis 145).

**Selektionsbild** (Zeilen 36 bis 48): Werk `P_WERKS` (Pflicht), `P_TAGE` (Vorschlag 31), optional `S_ERNAM`,
`S_MTART`, `S_MATNR`, dazu `P_GRUND` (Vorschlag „Auto-Delta MARA“), `P_NOCHK` (MARC-Pruefung ueberspringen) und `P_TEST`
(Testlauf, **Vorschlag angekreuzt**).

**Gelesen:** MARA mit `ERSDA` ab heute minus `P_TAGE` (Zeilen 86 bis 91), die vorhandenen Eintraege von
`ZLO_URSPR_AUSN` fuer das Werk und MARC zur Pruefung, ob das Material im Werk angelegt ist (Zeilen 110 bis 136).
**Ausgabe:** Protokoll mit Werk, Stichtag und Zaehlern (gelesen, neu, schon vorhanden, nicht in MARC) und Modus
(Zeilen 192 bis 212).

**Aendert Daten: ja.** Ohne Testlauf `INSERT zlo_urspr_ausn FROM TABLE ... ACCEPTING DUPLICATE KEYS` mit `COMMIT WORK`
(Zeilen 171 bis 181), Felder `GRUND`, `ERDAT` = Tagesdatum, `ERNAM` = Benutzer (Zeilen 139 bis 145). Mit `P_TEST`
schreibt es nichts (Zeilen 165 bis 169). Der Lauf nimmt alle neu angelegten Materialien des Zeitraums als Ausnahme auf,
eingeschraenkt nur durch die optionalen Felder und die MARC-Pruefung fuer das Werk.
