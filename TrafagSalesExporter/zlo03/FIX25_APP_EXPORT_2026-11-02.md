# ZLO03 FIX 25: Top-Down-Ergebnis fuer das Dashboard (P_APP, ZloTopDownSet)

Stand: 2026-11-02, T76 aktiv, Auftrag `T76K912785` (zusammen mit FIX 24), Freigabe und Import offen (Ingo).
Zurueck: `zlo03/CLAUDE.md`. Fachlicher Zusammenhang: `saptasks/zc23_materialanteil/BAUPLAN_PHASE2_MATERIALANTEIL_2026-11-02.md`.

## Was geaendert ist

*** FIX 25 in `Z_ZLO03_TURBO2` (Quelltext `Z_ZLO03_TURBO2_FIX25_2026-11-02.abap`, gegen den Live-Stand FIX 24 zeilengleich
gelesen, danach rueckgelesen identisch):

- Unsichtbarer Schalter `PARAMETERS: p_app TYPE char1 NO-DISPLAY`. Im Dialog wirkungslos.
- Mit `P_APP = X` endet der Lauf nach `process_main` in der neuen `FORM app_export`: je VKNR und Komponente die
  Stuecklistenmenge je 1 VKNR (`gt_mtab`, ueber alle Stufen, ohne Textpositionen), Stueckkosten und Wert in der Einheit der
  Stueckliste (gleiche Rechnung wie die Excel-Spalten, `apply_bom_unit` aus FIX 23/24), Disponent, Warengruppe, Text. Export
  ins ABAP-Memory `ZZLO03_APP` als Tabelle der Struktur `ZSTR_ZLO_TOPDOWN`. Kein Excel, kein Download.
- Warum: Bisher ging das Ergebnis nur als Excel-Download hinaus; kein Programm konnte es lesen. Das Dashboard braucht es fuer den
  Materialanteil je Produktgruppe, ohne zweiten Rechenweg.

Dazu in demselben Auftrag: Struktur `ZSTR_ZLO_TOPDOWN` (Paket `ZPP`), `ZCL_ZPOWERBI_EINKAUF_MPC_EXT->DEFINE` und
`ZCL_ZPOWERBI_EINKAUF_DPC_EXT->GET_ENTITYSET` mit dem Set `ZloTopDownSet` (`docs/abap/ZZLO_TOPDOWN_ADD.abap`; Pflichtfilter
Werks 1100 und 1 bis 200 Vknr, optional Dispo). Gateway-Caches in T76 geleert (`/IWFND/CACHE_CLEANUP` Modell
`ZPOWERBI_EINKAUF_MDL`, `/IWBEP/CACHE_CLEANUP` alle Modelle).

## Pruefung in T76

- Gateway Client: `$metadata` 200, `ZloTopDownSet` fuer 41019 200 (50 KB, 6,7 s), bestehendes `ShopRmSet` 200.
- `$TMP`-Testreport `Z_KOI_ZLO_APP_TEST` (SUBMIT wie das Set): 41019 ergibt 94 Komponenten, Wert 216.73 CHF je Stueck;
  H05039 = 3 ST zu 0.014133 CHF je ST, gleich dem FIX-23-Beleg; D85020 = 2 ST wie im Befund G4.

## Fehler dabei

Die erste Fassung hatte `PARAMETERS p_app AS CHECKBOX DEFAULT '' NO-DISPLAY`, was ABAP nicht zulaesst. Der Dialog „Fehler beim
Aktivieren“ wurde fuer eine Warnung gehalten und mit „Aktivieren“ bestaetigt; SAP hat trotzdem aktiviert, ZLO03 war in T76 von
10:53 bis 11:03 nicht lauffaehig (ST22 `SYNTAX_ERROR`). Korrigiert auf `TYPE char1 NO-DISPLAY`, danach ohne Fehlerdialog aktiv.
LEARNINGS 2026-11-02, Anleitung `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` Abschnitt 3 berichtigt.
