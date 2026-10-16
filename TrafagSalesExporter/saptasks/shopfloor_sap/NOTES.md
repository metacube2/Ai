# Shopfloor: Mengen aus SAP (ShopRmSet, ShopProdSet)

Stand: 2026-10-16, in T76 fertig und gegen ZC27 geprüft; Cockpit-Abruf produktiv mit ausgeschaltetem Schreiben.
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
Eintrag vom Tag D = Stand bis D-1, Montag Freitag bis Sonntag. Gruppen: MLE01 → `logistik.tr5_mle01`, MLE02 → `tr5_mle02`,
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

- Ingo: Transporte nach P76, Caches leeren.
- Danach Vorschau fuer den 14.10. (ERSDA 13.10.) gegen Fabios Kontrollwerte: MLE01 673, MLE02 526, MLE04 542, MLM01 193,
  MLE08+09 1'065; FPY SEH KW38 bis KW41 gegen 99.8 / 99.7 / 97.1 / 99.5. Erst dann `SapMengenEnabled = true`.
- Historie nachladen (Verkauf ab 01.01., Logistik/Tagesmengen ab 01.10.) erst danach, nur leere Tage.
- Nicht gebaut: J2/J3 Verkauf (Ergebnisbereich, Wertfelder, Merkmal fehlen; KEA0 bei Ingo), J6 Auftragsvorrat (ZC22).
