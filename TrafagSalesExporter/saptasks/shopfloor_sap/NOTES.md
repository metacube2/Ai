# Shopfloor: Mengen aus SAP (ShopRmSet, ShopProdSet)

Stand: 2026-11-13 15:05: juengster Inhalt ist Abschnitt 10, Layoutfilter ZC22/ZC27 (`Matex` an `ShopRmSet`; `Sttxt`, `Arbpl`, `Vornr`, `Avo` an `ShopAufSet`) in T76 aktiv und gegen ZC22/ZC27 geprueft, Auftrag `T76K912906` **nicht freigegeben**, nicht in P76. Davor 2026-11-12 (Doku-Pruefung: Kopf um Abschnitt 9 ergaenzt, Importstand `T76K912860` nachgetragen): Abschnitt 9 vom 2026-11-09, Auftragsvorrat Typ VR (`T76K912860`; laut Wochen_Todo 110 in P76 und seit 09.11. 15:11 live). Davor 2026-11-02, KEA0 fuer J2/J3 gelesen (Abschnitt 6, Nachtrag). Davor 2026-10-19 10:05, **produktiv, Produktionstag statt Abfragetag, DW prod** (Abschnitt 8). Vorher: 2026-10-16 09:51, **produktiv**: in P76 importiert, gegen Fabios Kontrollwerte gleich, Schreiben eingeschaltet (`9d6b5d2`), Erstlauf fuer den 16.10. geschrieben.
*Ueberholt:* „Cockpit-Abruf mit ausgeschaltetem Schreiben“ (bis 09:50).
Auftrag: Mail Fabio 16.10. (Datenabfragen J5 Logistik, J8 Tagesmengen SEH/TR5, J11 FPY SEH), von Ingo weitergeleitet.

## 1. Objekte in T76

| Objekt | Transport | Stand |
| --- | --- | --- |
| Struktur `ZSTR_SHOP_RM` (WERKS, ARBPL, ERSDA CHAR8, TYPCD CHAR10, LMNGA INT4, ANZAHL INT4), Paket ZPP | `T76K912740` (neu) | aktiv |
| Struktur `ZSTR_SHOP_PROD` (WERKS, DISPO, DATUM CHAR8, GAMNG INT4, WEMNG INT4, ANZAHL INT4), Paket ZPP | `T76K912740` | aktiv |
| `ZCL_ZPOWERBI_EINKAUF_MPC_EXT->DEFINE` + `ShopRm`/`ShopRmSet`, `ShopProd`/`ShopProdSet` | **`T76K912738`** (ZC23-Transport, Klasse dort gesperrt) | aktiv |
| `ZCL_ZPOWERBI_EINKAUF_DPC_EXT->GET_ENTITYSET` + zwei Bloecke vor der FinanzJournal-Weiche | **`T76K912738`** | aktiv |

Quelltext des Zusatzes: `docs/abap/ZSHOP_RM_ADD.abap`. Ganze Methoden: `methoden_bauen.py` aus dem Live-Stand
(gelesen per SE38/Class Builder, gleich dem Repo-Stand vom 15.10.) → `*_neu.abap`, vorher `*_vorher.abap`.

**Transport nach P76:** Die Klassen waren beim Sichern noch im offenen ZC23-Transport `T76K912738` gesperrt; SAP hat
die Aenderung deshalb ohne Abfrage dorthin geschrieben. Freigabe: zuerst `T76K912740` (Strukturen), dann `T76K912738`
(Klassen, enthaelt auch `Zc23Set`/`P_APP`). Danach in P76 `/IWFND/CACHE_CLEANUP` (Modell `ZPOWERBI_EINKAUF_MDL`) und
`/IWBEP/CACHE_CLEANUP`. Ingo macht den Transport selbst (16.10.).

## 2. Logik

*Nachtrag 2026-11-13:* P76-Befund `P76_BEFUND_2026-11-13.md`: die Vorgangsregel unten ist richtig und gleich ZC27; die Excel-Werte der
Teamleiter entstehen durch das ZC27-Standardlayout `/NIN` (Material E*, D*, F*, C* ausgeblendet) bzw. bei ZC22 durch `/ P00 TX`
(Status `FREI DRUC RÜCK VOKL*`, naechster Arbeitsplatz M*). Plan der optionalen Filter: `PLAN_LAYOUTFILTER_2026-11-13.md`;
*umgesetzt in T76 am 13.11. (Abschnitt 10), `T76K912906` noch nicht freigegeben.*


- **ShopRmSet = ZC27** (`ZPP_RM_MENGEN`, Quelltext in T76 am 16.10. gelesen): ARBID aus CRHD (Werk, Arbeitsplaetze der Auswahl);
  AFRU mit ERSDA, Werk, ARBID, `STOKZ = ' '`, `STZHL = 0`; Typencode `MARA-ZZTYP` nur wenn gefiltert; es zaehlt nur die
  Rueckmeldung zum hoechsten AFVC-Vorgang des Auftrags **an den gewaehlten Arbeitsplaetzen**. Ein Aufruf = ein ZC27-Lauf.
  Summiert wird LMNGA. Filter: Werks, Arbpl (1..30), Ersda (1..7 Tage, max. 400 zurueck), Typcd (mit `*` als Muster).
- **ShopProdSet**: AFKO/AFPO mit Istendtermin `AFKO-GETRI` in Datum, Disponent (mit `*`), Gesamtmenge AFKO-GAMNG je Auftrag,
  WE-Menge AFPO-WEMNG. Filter: Werks, Datum (1..7 Tage), Dispo.
- Ohne Pflichtfilter: HTTP 400 ohne Datenbankzugriff (Dienst ist auch die Power-BI-Quelle).

## 3. Pruefung in T76 (Daten bis etwa April 2026)

| Fall | ZC27 (SE38, Grid) | ShopRmSet |
| --- | --- | --- |
| MLE01, 14.04.2026 | 6 Zeilen, 197 | 197 / 6 |
| MLE08 + MLE09 zusammen, 14.04.2026 | 306 + 1'044 | 306 + 1'044 |
| Variante `/PPA EXX72` (11 Arbeitsplaetze EDF72, EDB72, EAC72, EAP72, EAN72, EAM72, EDD72, EEX72, EAS72, EDS72, EVA72; Typencode 8*), 10.03.2026 | 21 Zeilen, 836 | 836 / 21 |

Ohne Filter 400 mit Meldung; `ShopProdSet` Werk 1100, 14.04., S* liefert je Disponent; bestehende Sets 200.

## 4. Cockpit

`Services/Shopfloor/ShopfloorSapMengen.cs`, eingebunden in `ShopfloorSapSync` (gleiche Zeiten wie ZD05, 04:00 und 12:45):
*Ueberholt am 2026-10-19 (Abschnitt 8):* Eintrag vom Tag D = Stand bis D-1, Montag Freitag bis Sonntag. Gruppen: MLE01 → `logistik.tr5_mle01`, MLE02 → `tr5_mle02`,
MLM01 → `tx_mlm01`, MLE04 → `dw_mle04`, MLE08+MLE09 → `tx_mle08` (je eigener Lauf), EXX72-Gruppe → `tr5.prod`;
SEH: WE-Menge S* → `seh.prod`; FPY SEH = WE / Gesamt der KW des Vortags → `seh.fpy`. Manuell erfasste Werte bleiben.
Schalter `Shopfloor:SapMengenEnabled` (aus). Vorschau `GET shopfloor/api/sap/mengen?tag=JJJJ-MM-TT`, schreiben `POST`.

## 5. Fallen

1. Klassen im offenen Transport eines anderen Auftrags gesperrt: Sichern fragt nicht, die Aenderung landet dort (Abschnitt 1).
2. Methodenname mit `/` nicht ueber Git Bash geben (wurde zu `C:/PROGRAM FILES/GIT/IWBEP/...`), PowerShell nehmen.
3. Auftragstext hoechstens 60 Zeichen, sonst bricht `SapGuiAuftragNeu.vbs` im Dialog ab.
4. SapProbe braucht ein Passwort (`SAP_NCO_PASSWORD`), das in dieser Sitzung fehlte; Quelltexte deshalb per SE38/Class Builder
   gelesen (`GetLineCount`/`GetLineText`, 1-basiert; Editor im Class Builder unter `subEDITORSUBSCREEN:SAPLEDITOR_START:8430`).
5. ZC27-Typencode ist ein `txt`-Feld (`txtS_TYPCD-LOW`), die anderen sind `ctxt`.

## 6. Offen

- *Erledigt 16.10.:* Transporte nach P76, Caches leeren (siehe 7).
- *Erledigt 16.10. (siehe 7, Schreiben eingeschaltet mit `9d6b5d2`):* Danach Vorschau fuer den 14.10. (ERSDA 13.10.) gegen Fabios Kontrollwerte: MLE01 673, MLE02 526, MLE04 542, MLM01 193,
  MLE08+09 1'065; FPY SEH KW38 bis KW41 gegen 99.8 / 99.7 / 97.1 / 99.5. Erst dann `SapMengenEnabled = true`.
- Historie nachladen (Verkauf ab 01.01., Logistik/Tagesmengen ab 01.10.) erst danach, nur leere Tage.
- Nicht gebaut: J2/J3 Verkauf (Ergebnisbereich, Wertfelder, Merkmal fehlen; KEA0 bei Ingo), J6 Auftragsvorrat (ZC22).
  *Teilweise ueberholt am 2026-11-02:* KEA0 in T76 gelesen: Ergebnisbereich 1000, `CE11000`, Produktsparte `WWPSP` (beides seit Juni
  aus dem Produktsparten-Mapping bekannt), Wertfelder `VV100`/`VV110`/`VV200`/`VV210`/`VV220`/`VV230`/`VV250` und Skonto `VV240` oder
  `VV245` (selbst gegen Fabios Sep-Kontrollwerte entscheiden). J4 Verkauf laeuft ueber Fiori
  `C_SALESANALYTICSQRY_1_CDS`, kein eigenes Set. Einzelheiten `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 16.

## 7. P76 (2026-10-16)

- Ingo hat `T76K912728`, `T76K912736`, `T76K912740`, `T76K912738` importiert. SapProbe-Pruefung (`.tmp_sap_probe/P76Pruefung_Shopfloor_2026-10-16.ps1`,
  Anwendungsserver **`trasp76s`**, nicht `travp762`): alle vier in E070/E071, `ZM_OFFENE_KDAUF`, MPC und DPC zeilengleich mit dem Repo, Strukturen aktiv.
- **Falle:** P76 hat zwei Anwendungsserver. Das OData laeuft ueber `travp762` (10.194.64.30), die SAP GUI war an `trasp76s` (10.194.64.19).
  Cache-Leeren auf `trasp76s` half nicht (weiter 404 „metadata definition is incorrect“). Erst `/IWFND/CACHE_CLEANUP` auf `travp762`
  (Ingo per SM51) gab die Sets frei.
  *Nachtrag 2026-11-02 (Import `T76K912785`):* SM51 zeigte in P76 nur **eine** Instanz `trasp76s_P76_00`, kein `travp762`. Die Caches
  (`/IWFND/CACHE_CLEANUP` Modell `ZPOWERBI_EINKAUF_MDL`, `/IWBEP/CACHE_CLEANUP` alle Modelle) wurden deshalb auf `trasp76s` geleert
  (eigene Sitzung, `SapGuiP76Hilfe.vbs`); danach im P76-Gateway-Client `ZloTopDownSet` 200. Ob das Dashboard ueber `travp762:8000`
  dasselbe sieht, zeigt erst der naechste Einkauf-Lauf. Wie Ingo am 16.10. auf `travp762` kam, ist offen.
  *Nachtrag 2026-11-02 abends (Import `T76K912797` PPD-Verzug, `T76K912793` Zeivr):* wieder nur `trasp76s_P76_00`. Erst
  `/IWFND/CACHE_CLEANUP`, dann `/IWBEP/CACHE_CLEANUP` (alle Modelle, ohne Meldung in der Statusleiste): danach
  `MARA001Set?$select=Matnr,Zeivr` **404**. Ein zweites `/IWFND/CACHE_CLEANUP` gab es frei (200, `$metadata` 381'841 auf 382'030).
  **Reihenfolge daher: zuerst `/IWBEP`, dann `/IWFND`** (oder `/IWFND` am Schluss wiederholen), sonst baut eine Anfrage zwischen
  den beiden Schritten den Frontend-Cache aus dem alten Backend-Modell neu auf. `ShopAufSet` Typ VZ 200.
- Vorschau 14.10. (ERSDA 13.10.): MLE01 673, MLE02 526, MLM01 193, MLE04 542, MLE08+09 1'065 (465 + 600): **alle gleich** Fabios ZC27-Werten.
  FPY SEH KW38 99.8, KW39 99.7, KW40 97.1, KW41 99.5: **alle gleich**. TR5-Tagesmenge 13.10.: 815; SEH 2'431.
- Erstlauf 16.10. (Stand 15.10.): Logistik MLE01 24, MLE02 792, MLM01 149, MLE04 779, MLE08+09 532; TR5 854; SEH 1'837; FPY SEH 99.7 (KW 42 bis Do).
  Geschrieben als „SAP-Schnittstelle“, Quelle SAP an jedem Feld, die von der Logistik gesetzte Ampel blieb.
- Offen: Historie nachladen (Logistik/Tagesmengen ab 01.10., nur leere Tage) erst auf Wunsch Fabio; Gegenkontrolle der Tagesmengen SEH/TR5 durch Fabio.

## 8. Produktionstag statt Abfragetag, DW prod (2026-10-19, Feedback Logistik ueber Fabio)

Mail Fabio, von Ingo am 19.10. weitergegeben. Commits `ded2b65`, `c4dca10`, `c3c45cd`, produktiv 2026-10-19 10:04.

- **Tag der Werte:** Logistik (MLE01/02/04/08+09, MLM01) und die Produktionsmengen (SEH, TR5, DW) stehen auf dem
  **Produktionstag** (ERSDA bzw. Istendtermin = D), nicht mehr auf dem Abfragetag. Die SOLL-Zeilen der Logistik holen prod
  vom selben Tag. Die Montagsregel Fr+Sa+So entfaellt. FPY SEH = KW des Produktionstags bis zu diesem Tag.
- **Abgleich 04:00/12:45:** schreibt heute (laufend) und die drei Tage davor neu (`SyncDays`), am Montag also Fr, Sa, So einzeln.
  Vorrat, offen_kw, Verzug kommen nicht aus diesem Abgleich und bleiben auf dem Abfragetag.
- **Bereich ohne Rueckmeldung** an einem Tag (Wochenende): keine Nullen, frueher aus SAP geschriebene Werte werden entfernt
  (null loescht, leerer Tagessatz verschwindet). Je Bereich, weil SEH auch samstags Istenden hat (17.10.: SEH 751, Logistik
  nichts). Hat ein Bereich mindestens eine Zeile, bleibt 0 eine 0.
- **DW prod:** ZC27-Logik am Arbeitsplatz **DL790**, Summe LMNGA je ERSDA (`dw.prod`). 14.10. 1'033 (41 Rueckmeldungen),
  15.10. 761 (37), 16.10. 761 (39, gleiche Summe zufaellig), 19.10. bis 10:05 207 (8). Gegenkontrolle mit ZC27 durch Fabio offen.
- **Umbuchung:** 15. bis 19.10. per `POST shopfloor/api/sap/mengen?tag=` neu geschrieben. Der 15.10. traegt die Werte, die
  vorher auf dem 16.10. standen (MLE01 24, MLE02 792, MLM01 149, MLE04 779, MLE08+09 532, TR5 854, SEH 1'837, FPY 99.7);
  der 16.10. seine eigenen (322 / 1'121 / 78 / 543 / 424, TR5 904, SEH 3'900); 17./18.10. ohne Logistik, 18.10. ganz leer;
  der 19.10. ohne die alte Summe Fr bis So. Die Ampel der Logistik vom 16.10. (orange) blieb.
- **Falle:** POST ohne Header `X-User` liefert 403 „Ungueltige Anfrage“ (Schutz gegen Fremdaufrufe). Mit
  `Invoke-RestMethod -UseDefaultCredentials -Headers @{ 'X-User' = 'SAP-Schnittstelle' }`.
- **Fehler dabei:** der erste Lauf schrieb fuer Sa/So Nullen in die Produktion; korrigiert mit `c4dca10`/`c3c45cd` und
  erneutem Schreiben, Tagessaetze 17./18.10. Logistik danach weg (LEARNINGS).

### Noch nicht gebaut (Punkt 3 der Mail, TX)

- vorrat_total, vorrat_1m bis 3m, offen_kw, plan5 aus **ZC22**, Disponent EL*: im Cockpit gibt es dafuer noch keine SAP-Logik
  (auch nicht fuer SEH/TR5, dort erfasst Fabio); braucht ZC22 lesen und ein eigenes Set (J6).
- TX prod (ZC27 wie TR5): Arbeitsplatz/Variante klaert Fabio. TX fpy: MES Reportico `YIELDGESAMT`, ASIC_TX, nicht SAP.
- prod_s1/s2, Verzug, Terminverschiebungen, Hot-Anteil (Tagesplanungstool) und alle manuellen Vorgaben: bewusst nicht.
  *Ueberholt 2026-11-02 fuer Verzug und Terminverschiebungen:* Fabio will den PPD-Verzug aus ZC22 (Mail 01.11., Punkt 5; *gebaut
  2026-11-02 nachmittags:* `ShopAufSet` Typ VZ, Auftrag `T76K912797`, in T76 je Auftrag gleich ZC22, Cockpit `27badb2`, Schalter aus,
  `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 17); die Terminverschiebungen koennen per Patch 04 aus den Rueckstandslisten kommen (Schalter `tv_from_rs`, aus).
  `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 16.

## 9. Auftragsvorrat Typ VR (2026-11-09)

`ShopAufSet` Filter `Typ eq 'VR'`: wie VZ, aber ohne Eckendtermin-Grenze (`lv_sa_grenze = '99991231'`). Auftrag `T76K912860`
(Aufgabe `T76K912861`), Methode `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W`. Vorher/neu: `DPC_EXT_GET_ENTITYSET_2026-11-09_vorrat_*.abap`.
Test `Z_KOI_SHOP_VR_TEST.abap` (SEH 1'877 / 179'981 gleich ZC22). Fachlich: `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 19.
Hinweis P76: SAP GUI Scripting gibt die Zeilen der ZC22-Liste nicht als Elemente heraus; Liste ueber `%PC` sichern.

*Nachtrag 2026-11-12 (Doku-Pruefung), Importstand:* `T76K912860` ist laut Wochen_Todo 110 in P76, TX vorrat und offen_kw
kommen seit 09.11. 15:11 live aus `ShopAufSet` Typ VR; ebenso `docs/rag/PROJECT.md` Kopf („T76K912839 und T76K912860 in
P76“, Stand 2026-11-09 abends). Selbst im System geprueft ist das hier nicht.

## 10. Layoutfilter ZC22 / ZC27 im SAP (2026-11-13, T76, Phase 2)

Ausfuehrung des Plans `PLAN_LAYOUTFILTER_2026-11-13.md` Abschnitt 3 (Schritte 0 bis 6), Freigabe Ingo 13.11. („bitte umsetzen“;
ZC27-Materialfilter wie `/NIN`, ZC22-Statusfilter fuer alle Abteilungen, Arbeitsplatz M* bauen, Cockpit-Schalter aus). Entscheide
der Hauptsitzung: E1 in `T76K912906` (Aufgabe `T76K912907`), E2 Statuszeile gesammelt mit Sprache D, E3 `Sttxt` immer, E4 Vergleich
gegen das interne Materialfeld, E5 `Vornr` mitliefern. Nur T76 Mandant 100; P76 nicht beruehrt. Eigene Sitzung `/app/con[0]/ses[1]`
(per `SapGuiT76Hilfe.vbs neu`, vorher nur Ingos `ses[0]`), am Ende mit `/i` geschlossen. **`T76K912906` ist nicht freigegeben.**

### 10.1 Objekte, Auftrag `T76K912906` / Aufgabe `T76K912907`

| Objekt | Aenderung | Ruecklesen |
| --- | --- | --- |
| `ZSTR_SHOP_RM` | `MATEX CHAR40` an Position 7 (SE11, Zeile 6 vorher leer, Zeile 5 `ANZAHL`) | `DD03L` 7 Felder, `MATEX,0007,CHAR40,CHAR,000040,A`, die sechs alten unveraendert |
| `ZSTR_SHOP_AUF` | `STTXT TEXT100`, `ARBPL ARBPL`, `VORNR CHAR4`, `AVO CHAR1` an Position 9 bis 12 | `DD03L` 12 Felder: `STTXT,0009,TEXT100,CHAR,100`, `ARBPL,0010,ARBPL,CHAR,8`, `VORNR,0011,CHAR4,CHAR,4`, `AVO,0012,CHAR1,CHAR,1`, die acht alten unveraendert; keine inaktive Version |
| `MPC_EXT->DEFINE` (`CM001`) | Properties `Sttxt` (100), `Arbpl` (8), `Vornr` (4), `Avo` (1, filterbar) vor `bind_structure( 'ZSTR_SHOP_AUF' )`; `Matex` (40, filterbar) vor `bind_structure( 'ZSTR_SHOP_RM' )` | 3'062 Zeilen, `diff` gegen `MPC_EXT_DEFINE_neu_2026-11-13_layout.abap` rc 0; gegen vorher 35 Zugaenge, 0 Abgaenge |
| `DPC_EXT->GET_ENTITYSET` (`CM01W`) | sieben Einfuegungen (ShopRm a bis d, ShopAuf a bis c) | 2'886 Zeilen, `diff` gegen `DPC_EXT_GET_ENTITYSET_neu_2026-11-13_layout.abap` rc 0; gegen vorher 149 Zugaenge, 0 Abgaenge |

`E071` `T76K912907` danach genau: `LIMU TABD ZSTR_COPA_SUM`, `LIMU METH …MPC_EXT DEFINE`, `LIMU METH …DPC_EXT …GET_ENTITYSET` (Teil 1),
`LIMU TABD ZSTR_SHOP_RM`, `LIMU TABD ZSTR_SHOP_AUF`. `E070` am Ende (15:05) beide `D` (`T76K912907` zuletzt 14:41:17). `DWINACTIV`
fuer die vier Objekte leer, `TLOCK` fuer beide Strukturen auf `T76K912907`.

Dateien in diesem Ordner: `MPC_EXT_DEFINE_vorher_2026-11-13_layout.abap` und `DPC_EXT_GET_ENTITYSET_vorher_2026-11-13_layout.abap`
(RFC-Stand vor der Aenderung, byte-gleich mit `saptasks/copa_verkauf/*_neu_2026-11-13.abap`), `*_neu_2026-11-13_layout.abap`
(erzeugt mit `methoden_bauen_layout_2026-11-13.py`: die neun Bloecke woertlich aus den abap-Zaeunen des Plans geschnitten, nur
Einfuegungen, jeder Anker auf Eindeutigkeit und Nachbarzeile geprueft, Blocklaengen 26+1/7+1 und 12/9/21/1/28/1/77). Quelltext des
Zusatzes: `docs/abap/ZSHOP_LAYOUTFILTER_ADD.abap`. Testreport: `Z_KOI_SHOP_LAYOUT_TEST.abap` (in T76 als `$TMP` angelegt, nur er aus der
Liste „Inaktive Objekte“ aktiviert, am Ende geloescht: `TRDIR`, `TADIR`, `REPOSRC` ohne Eintrag, Gegenprobe `TRDIR ZPP_RM_MENGEN` liefert
die Zeile).

Ablauf: SE11 je Struktur mit Auftragsdialog (Vorschlag war `T76K912906`, ausdruecklich `T76K912906` gesetzt), aktiviert („Objekt wurde
aktiviert“). Beide Klassen mit `SapGuiKlasseAendernMitAuftrag.vbs 1 … T76K912906` geoeffnet (`KEINE_TRANSPORTABFRAGE`, erwartet: die
Methoden sind seit Teil 1 in `T76K912907` gesperrt), Methode mit `SapGuiKlassenMethodeQuelltext.vbs` gesichert (kein Auftragsdialog),
Syntaxpruefung je „Es wurden keine Syntaxfehler gefunden“, Klasse vom SE24-Einstieg aktiviert („Objekt(e) wurde(n) aktiviert“, kein
Fehlerdialog). Vor jedem Klassenskript `SapGuiT76Hilfe.vbs info` (eigene Sitzung Index 1).

### 10.2 Abweichungen und Handgriffe gegenueber dem Plan

1. **Zeilennummer im Plan um eins verschoben (ShopRm b):** Plan 1.2/3.5 nennen Zeile 1666 fuer `APPEND ls_rm_typ TO lr_rm_typ.`; im
   Live-Stand steht sie auf 1667, `WHEN OTHERS.` auf 1668. Eingefuegt wurde nach dem Textanker (genau einmal vorhanden, Folgezeile
   `WHEN OTHERS.`), also wie im Plan beschrieben. Die Ueberschrift 3.5 sagt „acht Einfuegungen“, Bloecke und Summenformel haben sieben.
2. **SE11 `ZSTR_SHOP_AUF`:** Im kleinen Fenster zeigte die Tabelle nur die Zeilen 0 bis 8. Statt zu blaettern wurde das eigene Fenster
   maximiert (`wnd[0].Maximize`, Id-geprueft); danach Zeilen 0 bis 17 sichtbar, `TC0-TOP_LINE` 1, Zeile 7 `MEINS`, 8 bis 11 leer.
   Vor dem Sichern nochmals geprueft: `TC0_SEC-COUNT` 12, die acht alten Zeilen unveraendert.
3. **Vergleichsreport ZC22 je Disponentengruppe:** `ZM_OFFENE_FAUF` liefert ohne jede Selektion nichts (`FORM fill_iafpo`: jeder Zweig
   nur bei gefuellter Selektion), und `S_DISPO` liest alle `AFKO` der Disponenten (928'194 Saetze in T76, davon 515'840 mit Disponent
   `0…`). Deshalb acht Laeufe mit `S_DISPO` als Einzelwerte, zusammen genau die 62 Disponenten der 2'197 VF-Auftraege der VR-Menge
   (Pruefung im Report: „VF ohne Gruppe 0“), je Lauf unter der Dialoggrenze. `P_PLAF = ' '` (Planauftraege haben in ZC22 weder Status
   noch Vorgang), `P_PEG = ' '`, `P_AVO = 'X'`.
4. **`$metadata`-Rumpf** ist im Gateway Client eine einzige Zeile und ueber `GetLineText` nicht lesbar (leere Datei). Beleg fuer die
   neuen Properties sind die Antworten der Sets (`Matex`, `Sttxt`, `Arbpl`, `Vornr`, `Avo` in jeder Zeile) und dass `Matex eq …` und
   `Avo eq 'X'` jetzt angenommen werden (vorher 400). Die Laenge von `$metadata` haengt von der Sprache des Aufrufs ab (Labels): nachher
   386'862 und 385'044 Byte in zwei Aufrufen, vorher 386'192.

### 10.3 Caches T76

`/IWBEP/CACHE_CLEANUP` „Cache f. alle Modelle“ („Bereinig. Modell-Cache erfolgr.abgschlssn“), danach `/IWFND/CACHE_CLEANUP` Modell
`ZPOWERBI_EINKAUF_MDL` („Modellcachebereinigung erfolgreich beendet“), 14:44.

### 10.4 Tests Gateway Client (T76, eigene Sitzung, Rumpf gelesen)

| Aufruf | vorher (14:27) | nachher (14:45) |
| --- | --- | --- |
| `$metadata` | 200, 386'192 Byte | 200, 386'862 Byte |
| `ShopRmSet` `Werks eq '1100' and Arbpl eq 'DLQ72' and Ersda eq '20260414'` | 200, `Lmnga` 494, `Anzahl` 28 | 200, 494 / 28, `Matex` leer |
| dasselbe `and Matex eq 'E*,D*,F*,C*'` | 400 „Property Matex not found in type ShopRm“ | 200, **394 / 21**, `Matex` = `E*,D*,F*,C*` (= ZC27 T76 mit `/NIN`, Plan 1.1) |
| dasselbe `and Matex eq 'e*,d*,f*,c*'` | 400 (deutscher Text) | 200, 394 / 21, `Matex` = `e*,d*,f*,c*` |
| `ShopRmSet` MLE04 14.04.2026 ohne / mit `Matex` | 579 / 34 bzw. 400 | 579 / 34 und 579 / 34 |
| `ShopRmSet` MLE01 14.04.2026 | 197 / 6 | 197 / 6 |
| `ShopAufSet` VR `Datum eq '20261113'` `$top=3` | 200, 1'254 Byte: 1173719 (15, 14.03.2025), 1213839 (6), 1215081 (12) | 200, 1'484 Byte, gleiche Auftraege, Mengen und Daten; `Sttxt` 1173719 `TFRE DRUC VOKL ABRV MABS`, 1213839 `FREI DRUC TRÜC VOKL ABGR ABRV MABS WABE`, 1215081 `FREI DRUC RÜCK VOKL ABGR ABRV MABS WABE`; `Arbpl`/`Vornr`/`Avo` leer |
| dasselbe `and Avo eq 'X'` | 400 „Property Avo not found in type ShopAuf“ | 200; 1173719 `0010` `EVA72`, **1213839 `0800` `MLE04`**, 1215081 ohne offenen Vorgang (leer); `Avo` = `X` bei allen drei |
| `ShopAufSet` VZ `$top=3` | 200, 1'254 Byte | 200, 1'484 Byte, `Sttxt` gefuellt (gleiche drei Auftraege) |
| `CoPaSumSet` `Hzdat le '20260315'`, `QmMeldungSet?$top=2`, `ShopProdSet` 14.04. `S*` | 200 (7'689 / 1'373 / 2'916 Byte) | 200, Rumpf jeweils gleich vorher |

Sprache: Die Antwort mit `Avo` lief mit `"Meins" : "PC"` (Englisch), `Sttxt` kam trotzdem deutsch (`FREI`, `RÜCK`, `TFRE`), also wirkt
die feste Sprache D (E2). Die vorher gelesenen 400-Antworten trugen dreimal `"lang" : "en"` und einmal `"de"`.

### 10.5 Vollstaendiger Vergleich (Testreport `Z_KOI_SHOP_LAYOUT_TEST`, `$TMP`)

Der Report ruft `ZCL_ZPOWERBI_EINKAUF_DPC_EXT->GET_ENTITYSET` wie das Gateway (VR, Werk 1100, Stichtag 13.11.2026, ohne Paging) ohne und mit
`Avo`, liest die neuen Felder per `ASSIGN COMPONENT` (vorher nicht vorhanden) und bildet ein MD5 ueber `Aufnr|Typ|Werks|Matnr|Dispo|Datum|
Menge|Meins` in Ausgabereihenfolge.

| Lauf | Zeilen | VF | VP | Summe Menge | MD5 | Laufzeit DPC |
| --- | ---: | ---: | ---: | ---: | --- | ---: |
| vorher A, ohne / mit Avo (14:34) | 10'955 | 2'197 | 8'758 | 2'163'743 | `C2C3BF9235EC3F92D1EC5A8CA86ECBB9` | 129 / 110 ms |
| vorher B (alt gegen alt, 14:34) | 10'955 | 2'197 | 8'758 | 2'163'743 | gleich | 114 / 113 ms |
| nachher ohne Avo (9 Laeufe, 14:46 bis 15:00) | 10'955 | 2'197 | 8'758 | 2'163'743 | **gleich** | 224 bis 296 ms |
| nachher mit Avo (9 Laeufe) | 10'955 | 2'197 | 8'758 | 2'163'743 | **gleich** | **15'872 bis 19'100 ms** |

Nachher: Grundfelder ohne/mit Avo zeilenweise gleich, `Sttxt` ohne/mit Avo gleich, ohne Avo `Arbpl`/`Vornr`/`Avo` bei 0 Zeilen gefuellt;
mit Avo `Sttxt` bei allen 2'197 VF gefuellt, `Avo = 'X'` bei 2'197, `Arbpl`/`Vornr` bei 2'132 (65 ohne offenen Vorgang), VP-Zeilen ohne
neue Felder. Status `FREI DRUC RÜCK VOKL*` **63**, `Arbpl` M* **165**, beides 0; je Gruppe (ausgeblendet / alle) EK 8 / 162, EL 50 / 135,
S 1 / 224, uebrige 169 / 1'676: alles gleich der Planmessung 1.8. Beispiele gleich Plan 1.8: 1170555 (025) ohne Vorgang
`FREI DRUC RÜCK VOKL ABGR ABRV MABS WABE`; 1209407 (SE1) `FREI DRUC RÜCK VOKL SPER ABRV MABS WABE`; 1213839 (D11) 0800 MLE04
`FREI DRUC TRÜC VOKL ABGR ABRV MABS WABE`; 986726 (001) 0010 PTA00 `FREI DRUC VOKL ABRV MABS`.

Seiten zu 5'000 Zeilen (Sortierung des DPC): Seite 1 VF 2'129 / VP 2'871, Seite 2 VF 42 / VP 4'958, Seite 3 (955 Zeilen) VF 26 / VP 929.

ZC22 je Disponentengruppe (`SUBMIT ZM_OFFENE_FAUF`, `P_AVO = 'X'`), Vergleich je Fertigungsauftrag mit dem Set-Lauf mit Avo:

| Lauf | Disponenten (`S_DISPO`, Einzelwerte) | ZC22 Dauer | ZC22 Auftraege | Set-VF | gleich | Sttxt / Arbpl / Vornr anders | Status-Muster Set / ZC22 | M* Set / ZC22 |
| --- | --- | ---: | ---: | ---: | ---: | --- | ---: | ---: |
| 1 | SB1 SB2 SB3 SB4 SE1 SE2 SE4 SK3 SL1 SL2 SL3 SL4 SPB | 79,9 s | 224 | 224 | 224 | 0 / 0 / 0 | 1 / 1 | 0 / 0 |
| 2 | D10 D11 D12 D20 D21 D22 D23 D30 DEF DS1 DS2 DW1 TL1 | 44,8 s | 413 | 413 | 413 | 0 / 0 / 0 | 26 / 26 | 37 / 37 |
| 3 | EK1 bis EK9, EKB | 76,5 s | 162 | 162 | 162 | 0 / 0 / 0 | 1 / 1 | 7 / 7 |
| 4 | EL1 bis EL8, ELP | 85,8 s | 135 | 135 | 135 | 0 / 0 / 0 | 15 / 15 | 35 / 35 |
| 5 | 001 003 004 012 013 016 018 021 022 026 027 028 031 | 73,7 s | 502 | 502 | 502 | 0 / 0 / 0 | 9 / 9 | 23 / 23 |
| 6 | 019 | 70,4 s | 186 | 186 | 186 | 0 / 0 / 0 | 6 / 6 | 7 / 7 |
| 7 | 020 024 | 86,9 s | 277 | 277 | 277 | 0 / 0 / 0 | 0 / 0 | 23 / 23 |
| 8 | 025 | 41,3 s | 298 | 298 | 298 | 0 / 0 / 0 | 5 / 5 | 33 / 33 |
| **Summe** | 62 Disponenten | | **2'197** | **2'197** | **2'197** | **0 / 0 / 0** | **63 / 63** | **165 / 165** |

Ergebnis: Fuer alle 2'197 Fertigungsauftraege der VR-Menge ist `Sttxt` = ZC22 `STTXT` und (mit `Avo`) `Arbpl`/`Vornr` = ZC22
`ARBPL`/`VORNR`, **0 Abweichungen**; in keinem Lauf ein Auftrag „nur im Set“, ZC22 hatte je Auftrag genau eine Zeile. ZC22 lief im
Dialog mit Sprache DE, also vergleicht der Test `status_lang` (sy-langu = D) mit der gesammelten Fassung (Sprache D fest).
Vor der Aenderung lief Gruppe 1 einmal zur Zeitmessung (82 s, 224 Auftraege). ST22 „Heute“ (alle Mandanten) um 15:02: keine Kurzdumps.

### 10.6 Laufzeit im Gateway Client (VR, Stichtag 13.11.2026)

Gemessen 15:01 in der eigenen Sitzung (Skript nach dem Muster von `SapGuiGatewayStatus.vbs`, Sitzung ueber die gemerkte Id; Status,
Laenge, Dauer, kein Rumpf). VF je Seite aus dem Testreport (gleiche
Sortierung `datum aufnr`, gleiches `$skip`/`$top`).

| Seite | VF | ohne `Avo` | mit `Avo` | Mehrzeit mit `Avo` | je VF |
| --- | ---: | ---: | ---: | ---: | ---: |
| `$top=5000` | 2'129 | 2,96 s, 2'332'076 Byte | 20,25 s, 2'354'943 Byte | 17,3 s | 8,1 ms |
| `$skip=5000&$top=5000` | 42 | 2,83 s | 3,27 s | 0,44 s | 10 ms |
| `$skip=10000&$top=5000` (955 Zeilen) | 26 | 1,27 s | 1,44 s | 0,17 s | 7 ms |
| zusammen | 2'197 | 7,1 s | 25,0 s | 17,9 s | 8,1 ms |
| Vorprobe `$top=500` mit `Avo` | | | 4,49 s | | |

Im Testreport (ohne HTTP): DPC VR ohne `Avo` vorher 110 bis 129 ms, nachher 224 bis 296 ms (rund +110 bis +170 ms fuer 2'197 VF, 0,05 bis 0,08 ms je VF,
Statuszeile); mit `Avo` 15'872 bis 19'100 ms (7,2 bis 8,7 ms je VF). Eine Seitenmessung im Gateway vor der Aenderung gibt es nicht; der
Vergleich vorher/nachher ohne `Avo` steht deshalb nur im Report.

### 10.7 Offen, was Ingo tun muss

1. **`T76K912906` freigeben** (Ingo). Er enthaelt jetzt Teil 1 (`Hzdat`) und Teil 2 (Layoutfilter); beide Methoden gehen als ganze Methoden
   mit. In P76 nach dem Import: `/IWBEP/CACHE_CLEANUP` (alle Modelle), dann `/IWFND/CACHE_CLEANUP` (`ZPOWERBI_EINKAUF_MDL`), danach im
   Gateway Client `ShopRmSet … and Matex eq 'E*,D*,F*,C*'` und `ShopAufSet … Typ eq 'VR' and Avo eq 'X'&$top=1` (Laufzeit notieren,
   Plan 4 Punkt 1), erst dann die Cockpit-Schalter (`Shopfloor:RmMaterialAusschluss`, `Zc22StatusAusschluss`, `Zc22ArbplAusschluss`).
2. Laufzeit mit `Avo` in P76 unbekannt (T76 7,2 bis 8,7 ms je Fertigungsauftrag); F1 (fahren die Teamleiter ZC22 „Mit Vorgangsdaten“?) offen bei
   Fabio. F2 ist im Cockpit schon umgesetzt: `ShopfloorLayoutFilter.PruefeFelder` bricht bei aktivem Arbeitsplatzteil ab, wenn eine
   Zeile Typ VF nicht `Avo = 'X'` traegt (`Services/Shopfloor/ShopfloorLayoutFilter.cs` Zeilen 66 und 75). Typ VZ ist in T76 nur mit
   `$top=3` geprueft (wie im Plan), der vollstaendige Vergleich lief fuer VR.
3. Rueckbau nur auf Anweisung Ingos nach Plan 3.8.

### 10.8 Fallen dieses Laufs (Kandidaten fuer `docs/rag/LEARNINGS.md`, nicht eingetragen)

- `ZM_OFFENE_FAUF` (ZC22) liefert ohne jede Selektion eine leere Liste, und `S_DISPO` liest alle `AFKO` des Disponenten seit Beginn
  (T76: 928'194 Saetze, Disponenten `0…` allein 515'840). Ein Vergleich „ganzes Werk“ geht nur in Gruppen von rund 100'000 `AFKO`-Saetzen
  (je Lauf 41 bis 87 s), sonst droht die Dialoggrenze von 600 s.
- Klassische Liste in einer kleinen GUI-Sitzung: Labels rechts der sichtbaren Breite (hier ab Spalte 125) gibt Scripting nicht heraus;
  ein MD5 am Zeilenende kam als `C2C3` an. Testausgaben kurz halten (je Wert eine Zeile) oder das Fenster vorher maximieren.
- SE11-Tabelle zeigt in der kleinen eigenen Sitzung nur 9 Zeilen; `wnd[0].Maximize` (Id-geprueft) statt Blaettern macht die Zeilen
  sichtbar, ohne die Ueberschreibfalle vom 30.09.
- `$metadata` im Gateway Client ist eine einzige Zeile und ueber `GetLineText` nicht lesbar; ihre Laenge schwankt mit der Sprache des
  Aufrufs (386'862 / 385'044 Byte nacheinander). Fuer neue Properties die Set-Antwort lesen, nicht die Laenge vergleichen.
- Der Dialog „Inaktive Objekte“ in SE38 meldet `RowCount` 29 bei 5 Eintraegen (Leerzeilen als Unterstrichreihe); eine Pruefung
  „Liste laenger als sichtbar“ ueber `RowCount` bricht faelschlich ab.
