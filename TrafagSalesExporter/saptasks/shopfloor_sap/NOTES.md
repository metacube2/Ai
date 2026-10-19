# Shopfloor: Mengen aus SAP (ShopRmSet, ShopProdSet)

Stand: 2026-10-19 10:05, **produktiv, Produktionstag statt Abfragetag, DW prod** (Abschnitt 8). Vorher: 2026-10-16 09:51, **produktiv**: in P76 importiert, gegen Fabios Kontrollwerte gleich, Schreiben eingeschaltet (`9d6b5d2`), Erstlauf fuer den 16.10. geschrieben.
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
- Danach Vorschau fuer den 14.10. (ERSDA 13.10.) gegen Fabios Kontrollwerte: MLE01 673, MLE02 526, MLE04 542, MLM01 193,
  MLE08+09 1'065; FPY SEH KW38 bis KW41 gegen 99.8 / 99.7 / 97.1 / 99.5. Erst dann `SapMengenEnabled = true`.
- Historie nachladen (Verkauf ab 01.01., Logistik/Tagesmengen ab 01.10.) erst danach, nur leere Tage.
- Nicht gebaut: J2/J3 Verkauf (Ergebnisbereich, Wertfelder, Merkmal fehlen; KEA0 bei Ingo), J6 Auftragsvorrat (ZC22).

## 7. P76 (2026-10-16)

- Ingo hat `T76K912728`, `T76K912736`, `T76K912740`, `T76K912738` importiert. SapProbe-Pruefung (`.tmp_sap_probe/P76Pruefung_Shopfloor_2026-10-16.ps1`,
  Anwendungsserver **`trasp76s`**, nicht `travp762`): alle vier in E070/E071, `ZM_OFFENE_KDAUF`, MPC und DPC zeilengleich mit dem Repo, Strukturen aktiv.
- **Falle:** P76 hat zwei Anwendungsserver. Das OData laeuft ueber `travp762` (10.194.64.30), die SAP GUI war an `trasp76s` (10.194.64.19).
  Cache-Leeren auf `trasp76s` half nicht (weiter 404 „metadata definition is incorrect“). Erst `/IWFND/CACHE_CLEANUP` auf `travp762`
  (Ingo per SM51) gab die Sets frei.
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
