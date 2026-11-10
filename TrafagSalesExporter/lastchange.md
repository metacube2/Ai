# Last Change

Stand: 2026-11-10

## Uebergabe Stand 2026-11-10 (Auftrag Ingo „md nachfuehren, committen, pushen, Chat neu starten“)

Gilt vor allen aelteren Uebergaben weiter unten. Produktiv: Deploy 08:46 `436e8ad`; Doku bis `88e9025` gepusht.

**SAP-Transporte (Freigabe und Import nur Ingo):**

| Transport | Inhalt | Stand | naechster Schritt |
| --- | --- | --- | --- |
| `T76K912866` | `CoPaSumSet` (CE11000 Monatssummen, Struktur `ZSTR_COPA_SUM`, MPC DEFINE, DPC GET_ENTITYSET) | T76 aktiv, rueckgelesen, Gateway 200; **nicht freigegeben** | Ingo: freigeben, importieren. Danach Claude: P76 `/IWBEP` (alle) dann `/IWFND` (`ZPOWERBI_EINKAUF_MDL`) in eigener Sitzung leeren, `GET shopfloor/api/sap/copa?tag=2026-10-01` gegen Sep (AE 6'531'197.94, Umsatz 7'878'114.69), mit Ingos OK `Shopfloor:SapCoPaEnabled = true`, Deploy, `POST sap/copa` fuer heute. `saptasks/copa_verkauf/NOTES.md` |
| `T76K912853` + `T76K912856` | IC-Staffelbasis (VOFM 601, V/06 PR00) | T76 aktiv, nicht transportiert | Fabio testet in T76; nur zusammen und erst nach seinem OK |
| `T76K912490` | ZZPRDAT | offen | P76-Vorpruefung, Georg, Fabio, Abnahme |

**Wartet auf andere:**
- **Fabio:** Verzug einschalten? Schalter `Shopfloor:SapVerzugEnabled` (aus) und `Shopfloor:VerzugMaterialFilter` (`SEH,TR5,TX,DW`, leer = ohne Filter) in `appsettings.json`; Ingo entscheidet nach Fabios Antwort, dann Wert aendern und deployen. Ausserdem TX `plan5` (Annahme: naechste 5 Arbeitstage) bestaetigen.
- **Andreas:** ISS-032 Gruppenumsatz (Regulierer `VBRK-KUNRG` ins Set, IC-Regel auf den Regulierer), siehe `docs/VERKAUF_2026-10-02.md` Nachtrag und Issue-Log.

**Kleinigkeit offen:** Die Vorschau `sap/copa` meldet bei 404 den generischen Hinweis mit den alten Auftraegen `T76K912740/738` (aus `ReadAsync`); fuer CoPa waere `T76K912866` richtig. Erledigt sich mit dem Import.

**Erledigt heute:** ISS-032 belegt, Q-Faelle Status/Produktlinie, DLQ72, Backend-Schutz Patch 07, plan5, Verzug-Schalter, CO-PA gebaut, fuenf Claude-Testreports in T76 geloescht (`Z_KOI_ZLO_FIX26_TEST`, `Z_KOI_VC_TXT_KOPIE`, `Z_KOI_VC_TXT_TESTID`, `Z_KOI_ZLO03_ALT`, `Z_KOI_ZLO_APP_TEST`). Koordinationseintrag abgeschlossen, keine Reservierung offen.

## Tag 10.11. (Auftrag Ingo „12, 13, 1 erledigt; 14, 2-6 durch dich, 9 Prod offen fuer Zaehlung“)

- **9 / ISS-032 belegt:** TRCH-Export nimmt den Auftraggeber (`VBRK-KUNAG`); bei 14.37 Mio ist der Regulierer eine Trafag-Gesellschaft (12.82 doppelt, 1.56 JP/PL); Gruppe rund 100 statt 116 Mio, betrifft auch Finance „Ist ohne IC“. Loesung (Regulierer ins Set, IC-Regel) wartet auf Ingo/Andreas.
- **6 Q-Faelle:** P76 hat fuer QM kein Anwenderstatus-Schema; Status jetzt aus dem Systemstatus-Lebenszyklus, Produktlinie ueber die Materialnummer (deployed 08:16).
- **3 DLQ72** live, Logistik-Soll DW darauf, Historie ab 01.10. nachgeladen; **5** Backend-Schutz Patch 07 und TX plan5 (Annahme) live; **4** CE11000 Skonto = VV240 (Sep exakt), Set-Bau offen; **2** Verzug weiter aus (Fabio).
- Deploy 08:16 `e6f6f70`. `docs/SHOPFLOOR_2026-10-07.md` 20, `docs/VERKAUF_2026-10-02.md`, Monthly-Review-Doku.
- **Vormittag 08:46 (`436e8ad`):** Verzug-Materialfilter schaltbar (`VerzugMaterialFilter`, `SapVerzugEnabled` aus); **CO-PA** gebaut: SAP `CoPaSumSet` in T76 (**`T76K912866`**, rueckgelesen, Test 200), Cockpit mit Schalter `SapCoPaEnabled` aus. **Ingo: `T76K912866` transportieren.** Vier weitere alte Claude-Testreports in T76 geloescht.
- **14** T76: `$TMP`-Report `Z_KOI_ZLO_FIX26_TEST` geloescht (SE38, eigene Sitzung). Weitere lokale Claude-Testobjekte aus Okt/Nov: `Z_KOI_VC_TXT_KOPIE`, `Z_KOI_VC_TXT_TESTID`, `Z_KOI_ZLO03_ALT`, `Z_KOI_ZLO_APP_TEST` (auf Ingos Wort am 10.11. ebenfalls geloescht); aeltere `Z_KOI_*`/`Z_IKO_*` gehoeren Ingo.

## Eingeschaltet 15:11 (Auftrag Ingo „aktiviere was geht und schalte frei“), 2026-11-09

- P76-Importqueue (Bild Ingo): `T76K912839`, `T76K912858`, `T76K912860` ok, `T76K912837` mit Warnung (RC 4).
- Shopfloor Auftragsvorrat eingeschaltet (`SapVorratEnabled`, SEH-Tagesmenge-Config), heute geschrieben (SEH 157'571, TR5 63'921, TX 50'342, DW 117'271). Verzug weiter aus (Fabio).
- Monthly Review Q-Faelle eingeschaltet (`QmEnabled`), Caches P76 geleert, 345 offene Q1-Meldungen; offen: Status und Produktlinie leer (pruefen).
- ZC22 Systemstatus ungekuerzt live (`T76K912858`); Kollege Produktion: Spalte im Layout breiter ziehen.
- Deploy 15:11 `4585392`. Uebergabe-Tabelle unten (09.11. abends) ist damit fuer 837/858/860 erledigt.

## Uebergabe Stand 2026-11-09 abends (Auftrag Ingo „alles in md, committen, pushen, falls ich neu starten will“)

Gilt vor der Uebergabe vom 09.11. morgens weiter unten.

**SAP-Transporte (Freigabe und Import nur Ingo):**

| Transport | Inhalt | Stand | nach dem Import / naechster Schritt |
| --- | --- | --- | --- |
| `T76K912839` | ZLO03 FIX 26 Basismenge + FIX 27 (Excel alt plus „je 1 Stk“) | **in P76**, Einkauf-Lauf 13:29: HW_ZUKAUFSORT 1.27 Mio. statt 957 Mio. | erledigt; Ann-Katrin/Sandro ueber die neuen Spalten informieren |
| `T76K912860` | Shopfloor `ShopAufSet` Typ VR (Auftragsvorrat) | **in P76**, Vorschau = ZC22 exakt (SEH 1'914 / 157'556, DW 1'976 / 117'271, alle Buckets) | Ingo-OK: `Shopfloor:SapVorratEnabled = true`, `config_seh_tagesmenge_sap.js` (--check), Deploy, `POST sap/vorrat` fuer heute |
| `T76K912837` | `QmMeldungSet` (Q-Faelle Monthly Review) | freigegeben 09.11.; Import P76 unbestaetigt | `/IWBEP` dann `/IWFND` leeren, `MonthlyReview:QmEnabled` true, Deploy |
| `T76K912858` | ZC22 Systemstatus ungekuerzt (`ZMM_UEB_FAUF-STTXT` TEXT100, Form `STATUS_LANG`) | bereit in T76 | freigeben, importieren; wirkt sofort; Kollege Produktion: Spalte im Layout breiter ziehen |
| `T76K912853` + `T76K912856` | IC-Staffelbasis (VOFM 601, V/06 PR00) | in T76 aktiv, **nicht transportiert**; Fabio testet zuerst in T76 | nur zusammen und erst nach Fabios OK |
| `T76K912490` | ZZPRDAT | noch nicht | P76-Vorpruefung, Georg (`ZXCO1U06`, `T76K911783`), Fabio, Abnahme |

**Shopfloor (Mail Fabio 09.11., Ampel im Chat):** a) Vorrat bereit zum Einschalten (oben); b) Verzug = ZC22 exakt, Schalter aus bis Fabio zu
Material 1-99999 (SEH immer 0) und Layoutfilter `/ P00 TX` antwortet; c) DLQ72 und d) CE11000 nicht gebaut (naechste Aufgaben); TX `plan5`
Definition offen; Backend-Schutz Patch 07 offen. Antwortentwurf an Fabio im Chat (Ziel a) Mi 11.11.). `docs/SHOPFLOOR_2026-10-07.md` 19.

**Verkauf (Sitzung Patrick):** Sankey/Sparte deployed; ISS-032 offen (GFS/Poltraf als IC-Regel mit Andreas, Doppelzaehlung TRCH/Toechter
per P76-Lesepruefung Rechnung 90390762 belegen); Liste `Downloads/Sparte_fehlt_PRODH_2026-11-09.xlsx`; Entscheid zentrale Spartenliste
fuer Toechter offen. `docs/VERKAUF_2026-10-02.md` Abschnitt 2026-11-09.

**Einkauf (Armin):** Materialanteil abnahmebereit; offen Armins Entscheid Materialnummer 1-99999 im Dashboard; Fragen 1-6 (Mailentwurf
angeboten); V6 Lieferantenbewertung nicht gebaut (Logik SAP, Entscheide offen).

**T76-Aufraeumen:** `$TMP`-Report `Z_KOI_ZLO_FIX26_TEST` enthaelt zurzeit den VR-Test (`saptasks/shopfloor_sap/Z_KOI_SHOP_VR_TEST.abap`),
loeschen; meine T76/P76-Sitzungen sind geschlossen.

## Shopfloor Verzug P76 abgeglichen, Auftragsvorrat gebaut; Einkauf FIX 26 in P76 bestaetigt (Auftrag Ingo), 2026-11-09 nachmittags

- PPD-Verzug: Vorschau P76 = ZC22 exakt (DW 9/185, TR5 11/465, TX 11/310, SEH 0); offen fuer Fabio: Layoutfilter `/ P00 TX` (Arbeitsplatz, Systemstatus) und Material 1-99999 (SEH immer 0). Schalter aus.
- Auftragsvorrat: SAP Typ VR in T76 (`T76K912860`, SEH 1'877/179'981 gleich ZC22), Cockpit `ShopfloorSapVorrat` deployed 14:12 (`01fceec`), Schalter aus bis Import und Abgleich. `docs/SHOPFLOOR_2026-10-07.md` 19.
- Einkauf: `T76K912839` (ZLO03 FIX 26/27) von Ingo importiert; Einkauf-Delta 11:24–13:29: E11221 → E11283 0.000211 ROL, **HW_ZUKAUFSORT Materialanteil 1.27 Mio. statt 957 Mio.** (34.7 % von 3.67 Mio.).
- `T76K912837` (QmMeldungSet) von Ingo freigegeben; Import P76, Caches und `MonthlyReview:QmEnabled` noch offen.

## Verkauf Gruppenzahlen, Sankey und ZC22 (Auftrag Ingo), 2026-11-09

- Verkauf Interaktiv deployed 10:45 (`04b4faf`): Sankey ganz beschriftet, „Ohne Kundenland“, Sparte zusaetzlich ueber TRCH-Nummer (TRIN). Befund Gruppenumsatz zu hoch (GFS/Poltraf nicht als IC, vermutete Doppelzaehlung TRCH/Toechter, mindestens rund 6 Mio) als ISS-032; Liste fehlende Sparte/PRODH in Ingos Downloads. `docs/VERKAUF_2026-10-02.md` Abschnitt 2026-11-09.
- ZLO03 FIX 27 in T76 im selben Auftrag `T76K912839` wie FIX 26: Excel-VKNR-Spalten wieder mit der alten Menge je Basismenge, neue Menge je VKNR als Zusatzspalte „je 1 Stk“, Dashboard unveraendert neu (Wunsch Ingo). Test T76 bestanden. `zlo03/FIX27_ALTE_UND_NEUE_MENGE_2026-11-09.md`.
- ZC22 Systemstatus ungekuerzt in T76 (`T76K912858`: Struktur `ZMM_UEB_FAUF` STTXT TEXT100 und Form `STATUS_LANG`), nicht freigegeben. `docs/abap/README_ZM_OFFENE_FAUF_BEDARFSVERURSACHER.md`.

## Shopfloor-Paket Fabio 07.11. eingespielt (Auftrag Ingo), 2026-11-09

- Patch 06 (RS offen = ohne Abschlussdatum) und 07 (SAP/MES-Werte gesperrt) deployed 07:23 (`fd345e7`). Configs DW lieferbereit (DLQ72) und SEH Tagesziel warten auf die SAP-Felder; Backend-Schutz fuer Patch 07 offen. Wochen_Todo 156, `docs/SHOPFLOOR_2026-10-07.md` 18.

## Uebergabe Stand 2026-11-09 (vor Neustart des PCs, Auftrag Ingo „alles md nachfuehren“)

*Ueberholt durch die Uebergabe vom 09.11. abends oben (Transportstand).*

**Offene SAP-Transporte (alle T76, nicht freigegeben; Freigabe und Import nur Ingo):**

| Transport | Inhalt | bereit? | nach dem Import |
| --- | --- | --- | --- |
| `T76K912839` | ZLO03 FIX 26 Basismenge (Materialanteil Wafer), seit 09.11. mit FIX 27 (Excel alt plus neu) | ja | Einkauf-Lauf, Materialanteil pruefen (HW_ZUKAUFSORT ~1.27 Mio. statt 957 Mio.), Ann-Katrin/Sandro informieren |
| `T76K912837` | `QmMeldungSet` (Q-Faelle Monthly Review) | ja | `/IWBEP` dann `/IWFND` leeren, `MonthlyReview:QmEnabled` true committen und deployen |
| `T76K912853` + `T76K912856` | IC-Staffelbasis nach Bestellmenge (VOFM 601, V/06 PR00) | ja, mit Vorbehalt (S6 nur in P76 testbar, MIRO-Buchung in T76 offen) | nur zusammen; `RV62A601` in P76 aktiv pruefen; Fabio: endgueltige Unterlieferung bestaetigen |
| `T76K912490` | ZZPRDAT (BAdI, Reiter-Anzeige `ZXCO1U11`, Rueckgabe `ZXCO1U12` repariert, Georgs XCO1-Objekte) | noch nicht | vorher P76 lesend: Import `T76K911390`/`T76K911398` (Projekt ZPP00012), Append `ZCO_ORD1`; Georg zu `ZXCO1U06` und `T76K911783`; Fabio informieren; Abnahme Lucas/Marco/Adil (Testnachweis `saptasks/zzprdat/testdoku_2026-11-06/`) |

**T76:** Verbindung riss am 06.11. 15:27 ab, Ingo muss T76 neu anmelden. Offene Testbelege: MIRO 0051128478 (fehlerhaft, Buchung offen), Infosatz 5300015578 ohne SPEZ (Entscheid Ingo/Fabio). Hilfsreport `Z_KOI_ZLO_FIX26_TEST` aufraeumen.

**Mails/Teams offen:** Fabio Begleitmail zum Testnachweis ZZPRDAT und Stand Staffelbasis (Entwurf auf Wunsch); Johannes Zetterblom Teams-Nachricht zum CRM (Text im Chat vom 06.11., `docs/CRM_DYNAMICS365_2026-11-03.md`); Antwort an Fabio zu Kurs/Terminen (KW fuer Schritt 1/2 offen).

**Entscheide offen:** Einkauf-Doppelungen je Reiter (Wochen_Todo 154), FP_THERMOST.-Fix, Texte „Nachts laeuft automatisch ein Delta“, `segmente.png`/`Bahnmarkt_DE_Kundenzuordnung` in der Wurzel geloescht (nicht von Claude; `segmente.png` wird von der Seite Marktsegmente gebraucht — wiederherstellen?), neun Worktrees mit Aenderungen und Gruppe 3 (spartenlogic/.tmp_*, cinebbox, ersetzt, output) beim Aufraeumen.

*Ueberholt: Uebergabe vom 2026-11-05 15:00 weiter unten.*

## IC-Staffelbasis und ZZPRDAT-Nachweis (Auftraege Ingo), 2026-11-06

- ZZPRDAT: `ZXCO1U12` repariert (Reiterwechsel loeschte das Datum, CO02-Sichern haette es geloescht), Durchspieltest 1241850, Testnachweis Word mit 21 Screenshots (Auftrag 1241851), alles in `T76K912490`.
- IC-Staffelbasis bei Unterlieferung (Mail Fabio 06.11., Blatt 8 `IC_Umsetzung_V1.xlsx`): VOFM 601 nur ZICAA1, Test IV 90381202 (394.20 statt 453.90 EUR), S7 Stufe Bestellung = IV, S6 nicht testbar in T76. `saptasks/ic_preisfindung/NOTES.md` 8.
- CRM: Fragen an Johannes nur in der Doku, nie gestellt; Antworten teils abgeleitet (kein Navision in den bekannten Verkaufsquellen).

## ZZPRDAT Abnahmesitzung und Transport gebuendelt (Auftrag Ingo), 2026-11-05/06

- Abnahmetest: Logik schreibt korrekt (AUFK), Reiter „TRAFAG Daten“ zeigte nichts, weil `ZXCO1U11` in Georgs offenem Auftrag `T76K911783` (Mandant 090) auskommentiert war. Auf Ingos Entscheid Georgs Objekte entsperrt (`RSWBO099`, Aufgabe `T76K911784`), Anzeige eingebaut, CO03 1241847 zeigt 11.11.2026.
- Alle ZZPRDAT-Objekte in `T76K912490` (Aufgaben `T76K912491` BAdI-Loesung, `T76K912852` XCO1-Objekte inkl. `ZXCO1TOP` gegen Syntaxfehler in P76). Nicht freigegeben. Offen: P76-Importstand ZPP00012/ZCO_ORD1, zweiter Schreibweg `ZXCO1U06`, Georg informieren. `saptasks/ZZPRDAT_TRANSPORTPLAN.md`.

## Aufraeumen Wurzel und Arbeitskopien (Auftrag Ingo), 2026-11-05 nachmittags

- Logs nach `.tmp_logs/` (`032722f`), sechs versionierte Dateien ohne Code-Verweis nach `docs/daten/` und `docs/abap/` (`e27d8af`).
- Von Ingo selbst ausgefuehrt (Auto-Modus lehnte Loeschen ab), Skript von Claude: 20 saubere Worktrees entfernt (15 alte `C:\TMP\…release_*`, `dev_projekte*`, zwei Juni-Kopien, zwei Agenten-Worktrees vom 05.11.), Build-Reste geloescht (`build_verify`, `verify_probe_out*`, `bintest`, `Tests/bintest`, `.tmp_hb_build`, `binmon`, `__pycache__`), rund 20 GB.
- Stehen gelassen: `C:\TMP\TrafagSalesExporter_release_eafd1c5` (Deploy-Werkzeug), neun Worktrees mit nicht committeten Aenderungen (dev_hri, localization_verify, release_a471ce2, sechs Agenten-Worktrees aus dem Oktober), `output/` (Excel-Auswertungen), Ordner aus Gruppe 3 (spartenlogic/.tmp_*, .tmp_pbix_x_extract, cinebbox-connect-downloader, ersetzt, AlphaplanExportPackage/delta) bis Entscheid Ingo.

## Uebergabe Stand 2026-11-05 15:00 (falls die Sitzung abbricht) *(ueberholt durch Uebergabe 2026-11-09 oben)*

**Zuletzt:** Antwort an Fabio Palma (IC-Preisfindung, Mail „AW: IC-Preisfindung automatisieren …“ vom 05.11. 14:04, allen antworten) als **Entwurf in Outlook Web gespeichert 14:59, nicht gesendet**; Inhalt: Preisliste 14 kommt in der IV an, Korrektur Kurs (M -> EURX 0.94, Kurstyp P unnoetig), ERL fuer Z001/Z002, Schritt 2 Job 4 bis 6 PT. Offen: Ingo traegt „KW __“ ein und sendet (oder gibt OK zum Senden).

**IC-Preisfindung (Wochen_Todo 153):** T76 und P76 lesend geprueft, `saptasks/ic_preisfindung/NOTES.md` (Abschnitte bis 7). Naechste Schritte: Customizing ZICAA1 in T76 (Z999, Z000, Z001/Z002 mit ERL, Konditionsausschluss ZPR0/Z999/Z001, Reihenfolge neben PR00/IV01/IV02/VPRS klaeren), Tests T1-T10 mit Fabio/Sandro (`a.xlsx` Blatt 4), Baustein fuer Infosatzpflege in P76 pruefen (Schritt 2). Fabios Unterlagen liegen ungetrackt als `a.xlsx`/`b.pdf` in der Repo-Wurzel (Geschaeftsdaten, nicht committen).

**Offen bei Ingo:** Transporte freigeben und nach P76 importieren: `T76K912839` (ZLO03 FIX 26 Basismenge, danach Einkauf-Lauf, Materialanteil HW_ZUKAUFSORT erwartet rund 1.27 Mio. statt 957 Mio.; Ann-Katrin/Sandro informieren) und `T76K912837` (QmMeldungSet, danach Caches /IWBEP dann /IWFND, `MonthlyReview:QmEnabled` true committen und deployen). Einkauf-Delta wurde 12:38 fuer den Deploy bewusst abgebrochen; Nachholen pruefen (Datenservice). Entscheide: Doppelungen Einkauf-Reiter (Wochen_Todo 154), FP_THERMOST.-Fix, Texte „Nachts laeuft automatisch ein Delta“, Patrick: Q-Statusfilter/Produktlinie/Bearbeiter; T76 aufraeumen `Z_KOI_ZLO_FIX26_TEST`.

**Arbeitsweise neu:** Agenten nur mit `.claude/agents/` (sap-t76, einkauf, finance-verkauf, hr; wirken erst in einer neuen Sitzung als Typ, bis dahin Datei zuerst lesen lassen), SAP nie mit Passwort (`router.md` Abschnitt Agenten, LEARNINGS 05.11.).

## Einkauf-Fixes deployed, ZLO03 FIX 26 in T76, Agenten-Definitionen (Auftraege Ingo), 2026-11-05 nachmittags

- Einkauf F2, F3, F4, F6, F8, F9 (`01ab4b9`, Doku `a17d623`), 1460/1460, deployed 11:48, im Browser bestaetigt; F5: Delta haengt nicht an IsActive, laeuft im Timer-Slot (laut Doku 12:00).
- ZLO03 FIX 26 in T76 (`T76K912839`, `zlo03/FIX26_BASISMENGE_2026-11-05.md`), P76 lesend: dieselben 6 Stuecklisten; FP_THERMOST. andere Ursache (Selbststuecklisten).
- Agenten-Definitionen `.claude/agents/` (sap-t76, einkauf, finance-verkauf, hr), Verweis in `router.md`.
- Spend-Aufriss aufgeraeumt (Wunsch Ingo): Matrix Lieferant / Jahr (`08caa7a`) und Block „Bestellwert nach Lieferant und Jahr“ (`785325f`) entfernt, doppelt zu den Einstiegen; `/einkauf/spend` zeigt den Aufriss; Gehirn bleibt eingeklappt; Handbuch nachgefuehrt; deployed 12:37, Delta dafuer bewusst abgebrochen.
- Neu Wochen_Todo 154: Einkaufsseite aufraeumen (Wiederholungen, mehrere Aufrisse zu einem), erst wenn die Werte stimmen (Wunsch Ingo).

## Verkauf Monthly Review mit Q-Faellen, SAP QmMeldungSet, Einkauf-Pruefung, Mails (Auftraege Ingo), 2026-11-05

- **Monthly Review Gesellschaften** (Wunsch Patrick Hertach): Unterreiter `/verkauf/monthly-review`, Phase 1 und Abschnitt 4
  „Qualitaet · offene Q-Faelle“ live aus SAP (`26ab30b`, `8ff3685`, Korrektur Statuscodes `47abb30`), 1443/1443, deployed
  10:33 (`docs/rag/DEPLOYMENT.md`). Q-Faelle-Abruf aus, bis `T76K912837` in P76 ist. `docs/monthly_review/MONTHLY_REVIEW_2026-11-04.md`.
- **SAP T76:** `QmMeldungSet` in `ZPOWERBI_EINKAUF_SRV`, Auftrag `T76K912837` (313 offene Q1 in T76), `saptasks/qm_meldungen/NOTES.md`.
- **Einkauf:** Funktionspruefung aller Ansichten im Chrome, Fehlerliste `docs/EINKAUF_PRUEFUNG_2026-11-05.md`; Materialanteil
  957 Mio. durch ignorierte Stuecklisten-Basismenge (`zlo03/BEFUND_EINHEIT_WAFER_2026-11-05.md`), FIX 26 freigegeben, noch nicht umgesetzt.
- **Mails:** Antwort an Fabio Palma zur IC-Preisfindung (ZICAA1, Wochen_Todo 153) und an Adrian Jenni zur SPC-Adapterauswertung
  (Wochen_Todo 151) gesendet. Wochen_Todo 150 bis 153 samt `.xlsx`, LEARNINGS (4), Router Einkauf, baum, Koordination.

## Nachfuehrung zum Chatschluss (Auftrag Ingo „md nachfuehren“), 2026-11-04 13:10

- Verkauf Monthly Review: Auftrag, Entscheide, Phasen und T&P-Befund in `docs/monthly_review/MONTHLY_REVIEW_2026-11-04.md`,
  Spezifikation und Entwurf daneben (bisher nur im Scratchpad bzw. ungetrackt in der Wurzel). Router Finance, baum, VERKAUF (Marge),
  Wochen_Todo 150, Koordination (pausiert). HR: Ampelstand und Fragen an HR in `docs/HR_KPI.md` 8.16, rag/HR_KPI, Wochen_Todo 149.

## HR-Cockpit: Kachel „Unfalltage laufender Monat“ aus SAP- und Rexx-Faellen (Auftrag Ingo), 2026-11-04

- `AccidentDaysCurrentMonth`, hoeherer Wert aus SAP-Tagesdatei und Unfallfaellen; `dac3098` (12:36) und Korrektur `f86b0ed` (12:41,
  auch aus #775 ergaenzte Unfaelle), 3 Tests, Suite 1404/1404, produktiv, Kachel 4,2 statt 0,0. Rexx-Zeitmanagement lesend geprueft,
  Tab fror dreimal ein. Doku HR_KPI 8.15, rag/HR_KPI, rag/DEPLOYMENT, LEARNINGS, Koordination.

## HR-Cockpit: Rexx-Abwesenheiten je Fall #777, Interaktiv mit Datum (Auftrag Ingo „alles gruen“), 2026-11-04

- Rexx: private Abfrage #777 in der Antragsliste (Von/Bis, Art, Status, Tagessoll). Cockpit: `MergeRexxCases` ergaenzt SAP um
  1'203 bestaetigte Faelle mit Datum (nur nicht abgedeckte Tage). `4bdbc18`, 3 Tests, Suite 1401/1401, **produktiv seit 12:21**,
  in Chrome geprueft (Ferien am Stueck 101 von 260, Fit & Wohl 31). Offen: rund 100 direkt in Rexx gebuchte Krankheitstage ohne
  Datum (13 Personen). Doku HR_KPI 8.14, rag/HR_KPI, rag/DEPLOYMENT, Router HR, Schulungsseite, Handbuch, LEARNINGS, Wochen_Todo 149.

## HR-Cockpit: Rexx-Gesamtabfrage #775 statt vier Rexx-Dateien, das Aktuellere je Person (Auftrag Ingo), 2026-11-04

- Rexx: private Abfrage #775 „HR-Cockpit Gesamt (Dashboard)“ (50 Spalten). Cockpit liest `Hrcockpitgesamtdashboard.xlsx` statt
  #757/#732/#744/#381, wenn sie in `hrdata` liegt; je Person der hoehere Wert aus SAP und Rexx fuer 01.01. bis Exporttag;
  Rexx-Unfaelle ergaenzen SAP; Ferien bezogen direkt aus Rexx (alte Formel zog geplante Tage doppelt ab). `6dd1a56`, 12 Tests,
  Suite 1398/1398, **produktiv seit 11:46**, in Chrome geprueft. Vorher 11:23: Saldi-Export mit Stichtag 31.12.2024 in `hrdata`
  ersetzt. Doku `docs/HR_KPI.md` 8.13, rag/HR_KPI, rag/DEPLOYMENT, Router HR, Handbuch Kapitel HR, Schulungsseite, LEARNINGS (5),
  Wochen_Todo 149, Koordination.

## HR-Cockpit: Formeln geprueft, drei Rechenfehler behoben, neuer Reiter Interaktiv (Auftrag Ingo), 2026-11-04

- Pruefung aller Formeln (`docs/HR_KPI.md` 8.10). Behoben in `HrKpiDashboardBuilder`: Krankenquote-Nenner je Person erst ab
  Eintritt (`AbsenceDenominator`, `FteWorkdays`), Quote je Person mit Pensum, Prognose rechnet ein laufendes Quartal vorher auf
  die volle Laenge hoch (`QuarterProgress`, `ForecastFromQuarter`). Offen fuer Entscheid: 61-Tage-Regel, Vorjahresvergleich im
  laufenden Jahr, Kachelbeschriftung bei Von/Bis.
- Reiter Interaktiv im HR-Cockpit (`docs/HR_KPI.md` 9): `Services/HrKpi/HrKpiInteractive.cs`, `Components/HrKpi/Interactive/`
  (acht Ansichten), CSS `hri-`, 66 Texte in sieben Sprachkatalogen, Tests `HrKpiInteractiveTests` und drei neue in
  `HrKpiServiceTests`. Schulungsseite und Handbuch Kapitel HR nachgefuehrt. Router HR, rag/HR_KPI, Wochen_Todo 149,
  Status_kurz. *Ueberholt „nicht deployed“:* nach Nachbesserung Filtervertrag (`b65f059`) **produktiv seit 09:20**,
  mit Datenverantwortung und Shopfloor-Startseite (`docs/rag/DEPLOYMENT.md`). Um 09:30 Rexx-Dateien in `hrdata` durch
  Ingos Exporte vom 04.11. ersetzt (alte archiviert, `docs/HR_KPI.md` 8.2); dabei zwei offene Befunde (Sollzeit-Spalte
  nicht erkannt, nur Status „Aktiv“), 8.10. LEARNINGS: GIF-Aufnahme der Chrome-Erweiterung ist kein Video.
- Danach Uebergang bis zur Rexx-Schnittstelle (`docs/HR_KPI.md` 8.11, Auftrag Ingo „das Aktuellere nehmen“): Status
  „Vertrag endend“/„Langzeitkrank“ zaehlen, Sollzeit-Spalte „Ø tägliche …“ erkannt (Vollzeit = FTE 1,0), juengste
  Rexx-Krankmeldungen ergaenzen SAP (`BuildRexxGapSicknessRows`, am 04.11. 34 Faelle), SAP-Stand bei Ferien/Kompensation/
  Militaer sichtbar. Vier neue Tests, Suite 1381/1381, Probe gegen echte Serverdateien. Handbuch, Router, rag, LEARNINGS.
  **Produktiv seit 09:57** (`cfe2631`, Delta vorher geprueft).
- Danach HR-Vorgaben aus Mails und Teams umgesetzt (`docs/HR_KPI.md` 8.12; Mails Nadja 10/2025, Sonja 02/2026 und 09/2026
  in Outlook Web gelesen, Teams ohne Fachinhalt): Kurz/Lang je Krankheit ab dem 61. Tag (SAP-Faelle verbunden, 0270 lang),
  Unfall 0290 in Quote, Krankheitskalender und Fit &amp; Wohl, Kachel „Quote ohne Langzeit“, Gelb auf der Uebersichtskachel,
  Vorjahresvergleich gleicher Zeitraum, Von/Bis-Kachel mit Spanne und Headcount ueber die Spanne. Sechs neue Tests, Suite
  1386/1386, Probe mit echten Daten. Neue Frage an HR: Kontrollwerte 2025 passen nicht zum Rexx-Export. Handbuch, Router,
  rag, LEARNINGS, Wochen_Todo 149, Status_kurz. **Produktiv seit 10:21** (`69a63b6`, Delta vorher geprueft).


## CRM Dynamics 365 Sales: Befund und Idee lesende Anbindung festgehalten (Auftrag Ingo „notiere das alles in md“), 2026-11-03

- `docs/CRM_DYNAMICS365_2026-11-03.md` (neu): Scorecard Opportunity Pipeline (Johannes) kommt aus Dynamics 365 Sales, nicht Navision; Kette, IDs,
  Felder (`ava_sapid` = SAP-Kunde), Zahlen Stand 20.07., Vorschlag Dataverse Web API mit Leserolle, offene Fragen.
  Router Standortdaten, baum, LEARNINGS (Power BI schickt Zugriffsanfragen automatisch). Kein Code, kein Deploy.

## Wochen_Todo auf den Stand vom 03.11. gebracht (Frage Ingo), 2026-11-03

- Erledigt: 86 (ZC22-Layout P76), 116 (Abschlussrunde), 134 (Aufgaben Chef eingeordnet). Nachgetragen: 43, 45, 54, 131, 133, 145.
  Neu 148 (voller HR-Export fuer Sonja, Zusage heute).

## Inventar: Scan-Skript fuer Bericht-Excel und Power BI vorbereitet (Auftrag Ingo „bereite vor“), 2026-11-03

- `Tools/Inventar/Scan-BerichtDateien.ps1` mit README: nur Metadaten, Benutzer/Passwoerter werden entfernt; getestet auf eigenen
  Ordnern (Simons Excel als Power Query auf `tragvsqlc400` erkannt). Ausfuehrung erst nach OK von Alex. Wochen_Todo 138, PROJEKTSTATUS
  PM-14, baum, LEARNINGS.

## Doku nachgefuehrt: Status kurz fuer den Chef, Rexx-Hub-Sitzung, Ansprechpartner (Frage Ingo „alle md nachgefahren?“), 2026-11-03

- `Status_kurz.tsv` samt `.xlsx`: Rexx, Abwesenheiten, ZC22, Abschlussrunde, Trafag Projekte aktualisiert; neu ZREICHW, PM14,
  Seite Datenverantwortung, Berechtigungskonzept.
- Rexx-Hub-Sitzung naechste Woche (Alex, Artjom) in rag/HR_KPI, HR_KPI 8.9, Rexx-Notizen; ANSPRECHPARTNER mit Artjom und
  Abschnitt Datenverantwortliche.

## Datenverantwortung: neue Seite mit Eingabeformular fuer Dataowner (Wunsch Chef, Auftrag Ingo), 2026-11-03

- `/datenverantwortung`: Datentopf, System, Verantwortlich, Stellvertretung, Key User, Status vorlaeufig/bestaetigt, Bemerkung;
  lesen fuer alle, bearbeiten mit Admin-Login; Tabelle `DataOwners` mit Startbelegung aus der Abschlussrunde.
- Code `c267651`, Release-Worktree 1360/1360; Deploy nach dem Einkauf-Lauf. Doku Sitzungsprotokoll 6, Router Plattform,
  rag/ARCHITECTURE, Wochen_Todo 145.

## Projektmanagement: Aufgabenliste des Chefs vom 03.11. eingeordnet, 2026-11-03

- Neu Wochen_Todo 145 (Dataowner-Dashboard), 146 (Projektmanagement deaktivieren, Rueckfrage an den Chef), 147
  (Berechtigungskonzept mit AD-Abgleich); 138 und 139 ergaenzt. Sitzungsprotokoll Abschnitt 6, PROJEKTSTATUS PM-14. Nur Doku.

## Trafag Projekte: Projekt PM14 Datenqualitaet und Abloesung angelegt (Auftrag Ingo „ja anlegen“), 2026-11-03

- Ueber „Vorhaben aus Wochen_Todo uebernehmen“ mit nur dem Vorhaben PM-14 und `PM-14_TrafagProjekte_Import.tsv` (per Chrome,
  Adresse mit `/BiDashboard/`): 1 Projekt neu, 53 Aufgaben neu; privat, Leitung Ingo. Wochen_Todo 139 in Arbeit, PROJEKTSTATUS,
  LEARNINGS (Chrome-Sperre war die fehlende Pfadangabe).

## Projektmanagement: PM-14 fuer Trafag Projekte vorbereitet, Wochen_Todo 55/137, 2026-11-03

- `projektmanagement/PM-14_TrafagProjekte_Import.tsv`: 53 Aufgaben (DQ-01 bis DQ-30, INV-01 bis INV-19, Datenverantwortung, Aufwand) im Wochen_Todo-Format; Ingo laedt sie im Projekt PM14 ueber Abgleich Wochen_Todo hoch.
- Wochen_Todo 55: Sitzung Rexx Hub naechste Woche mit Alex und Artjom (Nadja/Sonja laut Alex nicht noetig); 137 erledigt (Link an Dragan).

## Projekte: Abschlussrunde Dashboard und Data Lake gehalten, Protokoll aus dem Transkript, 2026-11-03

- Rueckmeldungen Einkauf, Operations, HR, Finance, Produktion; Datenverantwortliche; Oscar: Power-BI-/Smartsheet-Inventar,
  Taskliste offener Wuensche mit Aufwand und Datenqualitaet in Trafag Projekte, abgeloeste Z-Objekte loeschen.
- `docs/SITZUNG_ABSCHLUSSRUNDE_DASHBOARD_2026-11-03.md` (neu), Wochen_Todo 55 und 137 bis 144, PROJEKTSTATUS PM-10/11/12/14,
  Router Projekt, rag/PROJECT, baum. Nur Doku.

## SAP ZC22: Banf-Abruf statt Kundenauftrag als Bedarfsverursacher bei Umlagerung (Wunsch Anh Nhi, Auftrag Ingo „mach du“), 2026-11-03

- P76 gelesen: ZC22 zeigte fuer 46690 KndAuftr 419042; der Bedarfsverursacher (`MD_PEGGING_NODIALOG`) liefert den Endbedarf
  `VC` in Werk 1200, nicht den Banf-Abruf; Layout `/ P00 TX` in P76 ohne Bedarfsverursacher-Spalten (nach dem 09.10. nie nachgezogen).
- T76: neue Form `UMLAGERUNGSBEDARF` in `ZM_OFFENE_FAUF` (offene Umlagerungsbestellung `U1` bzw. offene BANF `U2`, kontiert auf
  den Kundenauftrag des Endbedarfs, aus dem Werk des Auftrags), Auftrag `T76K912825`, aktiv, zurueckgelesen, Test 63502 `U1 45148422/10`.
- Am 03.11. in P76 importiert (Ingo); Layout `/ P00 TX` in P76 angepasst und gesichert (Ueberschreiben von Ingo bestaetigt);
  Pruefung 46690: `U1 45152789/10` (BANF inzwischen in Umlagerungsbestellung umgesetzt). Offen nur die Antwort an Anh Nhi.
  Doku `docs/abap/README_ZM_OFFENE_FAUF_BEDARFSVERURSACHER.md`, Router SAP, Wochen_Todo 136, LEARNINGS, SAP-Werkzeuge.

## HR Rexx: Mails zum Upgreat-Ticket und zum Rexx-Hub-Angebot gelesen (Outlook Web, Auftrag Ingo), 2026-11-03

- Upgreat schloss Ticket #12699584 am 19.10. mit „Unable to solve“ (Rexx betreut Trafag selbst); Alex leitete am 29.10. Nadjas
  Hub-Angebot weiter (8 Schnittstellen 11'200 EUR/Jahr, 4 weitere 5'600 EUR/Jahr, Hub 4'800 EUR/Jahr); Rexx +2 % ab 2027; Sonja:
  HR konnte noch nichts testen. Nachgefuehrt: `saptasks/rexx_schnittstelle/NOTES.md` 11, `HR_KPI.md` 8.9, rag/HR_KPI,
  ANSPRECHPARTNER (Rexx Systems Schweiz), Wochen_Todo 55, PM-14-Listen. Nur gelesen, nichts beantwortet.

## Projektmanagement: neues Vorhaben PM-14 Datenqualitaet und Abloesung (Sitzung Chef, Auftrag Ingo), 2026-11-03

- PM-14 in PROJEKTSTATUS, HERMES-Sicht und Wochen_Todo 134; zwei Listen im Ordner `projektmanagement`: 30 Befunde zur
  Datenqualitaet (wer bereinigt was) und Inventar von 19 Power-BI-, Smartsheet- und Excel-Ablaeufen, dazu
  `PM-14_Datenqualitaet_Abloesung.xlsx` (`pm14_listen_xlsx.py`). Eingesparte Stunden nirgends beziffert, Spalten zum Schaetzen.
- Neu Wochen_Todo 135: Klartext-Zugangsdaten der Tochtergesellschaften in der Produktiv-DB (Tabelle `Sites`). Kein Code, kein Deploy.

## SAP ZREICHW: schnelle Fassung ins Original uebernommen (Entscheid Ingo „ja uebernehmen“), 2026-11-03

- T76: `Z_KOI_REICHW_TURBO` durch die schnelle Fassung ersetzt, im selben Auftrag `T76K912799` (nur `REPS Z_KOI_REICHW_TURBO`),
  aktiv, per RFC zeilengleich zurueckgelesen. Vergleich ganzes Werk 1100: 152'280 Zellen, 0 Abweichungen, 2,3 s statt 155 s.
  Spool-Test 1265145 ohne Ausgabeauftrag. Lokale Kopien `Z_KOI_REICHW_OPT`/`_VGL` geloescht.
- Am 03.11. von Ingo nach P76 importiert; P76 gelesen: Haken da, Werk 1100 in rund 5 s. Laut Ingo erledigt und passend (03.11.). Doku `saptasks/zreichw/NOTES.md` 6, Router SAP, Wochen_Todo 132.

## HR: Rexx-Schnittstelle nach SAP gefunden, Idee eigenes Programm mit Z-Tabelle (Auftrag Ingo „alles in md festhalten“), 2026-11-03

- T76 nur gelesen: Eigenentwicklung der EXPRIS AG (Paket `Z_HR_PA_REXX`); Sonja Richter (`RIS`) importiert von Hand
  Stammdaten (`Z_HR_PA_REXX`), Absenzen (`Z_HR_REXX_PA_ABS`, Batch-Input PA30) und Kommen/Gehen (`Z_HR_REXX_PA_ZE`), kein Job.
- Krankheit/Unfall nimmt der Absenz-Import nicht mit; Ferien, Kompensation und Militaer kommen nur mit dem Import nach SAP,
  `HrAbsenzSet` hinkt dafuer nach (Aussage „P76 immer aktuell“ in `HR_KPI.md` 8.7 als ueberholt markiert).
- Mails (nur Betreff): Upgreat-Ticket #12699584 am 19.10. geschlossen, Angebot AG36071 Rexx Hub 29.10.; Inhalt ungelesen.
- Idee Ingo: eigenes Programm nur in Z-Tabellen, taeglicher Job; nicht gebaut. Doku `saptasks/rexx_schnittstelle/NOTES.md`
  (neu, mit Quelltexten aus T76), `docs/HR_KPI.md` 8.9, rag/HR_KPI, Router HR, ANSPRECHPARTNER (EXPRIS), PROJEKTSTATUS PM-07,
  Wochen_Todo 55/133, LEARNINGS (drei Zeilen). Kein Code, kein Deploy.

## SAP ZREICHW: nur Spoolauftrag statt Sofortdruck (Teams-Anfrage, Auftrag Ingo), 2026-11-02 abends

- `Z_KOI_REICHW_TURBO` druckte nie selbst; gedruckt wurde ueber den Druckdialog des Grids mit „Sofort ausgeben“. Neu: Ankreuzfeld
  „Nur Spoolauftrag (nicht drucken)“, die Liste geht ohne Sofortdruck in einen Spoolauftrag (SP01/SP02).
- T76 eigener Auftrag `T76K912799`, aktiv, zurueckgelesen zeilengleich; Test: Spool 1264623 ohne Ausgabeauftrag, alle Spalten.
- Offen: Freigabe und Import P76 (Ingo). Befunde zur Optimierung in `saptasks/zreichw/NOTES.md` Abschnitt 3.
- Danach optimierte Kopie `Z_KOI_REICHW_OPT` ($TMP) ohne Ergebnisaenderung: Werk 1100 von 155 s auf 2,2 s, gegen das Original
  zellgleich (5'076 Zeilen, 152'280 Zellen, auch mit allen Optionen und Januarbeginn; Debug-Liste gleich). Uebernahme ins Original offen.
- Uebergabe fuer den 03.11. mit Reihenfolge der naechsten Schritte, Antworttext fuer Teams und Werkzeugen in `saptasks/zreichw/NOTES.md`
  Abschnitt 5; Hilfsskripte aus dem Scratchpad nach `saptasks/zreichw/werkzeuge/` gesichert.

## Operations KPI: Rubrik Kunden auf OTD / OTIF / OTP (Spezifikation Fabio), 2026-11-02

- Zaehler je Auftraggeber und Monat im Monatsabruf, Namen fuer alle Kunden, Suche nach Nummer oder Name, Kacheln, Monatstabelle,
  Trefferliste, „Abweichungen dieser Kunden“, Excel Kunden YTD / pro Monat (`aa12a39`, Tests 1358/1358).
- Deploy 13:57 (`187f8a0`, mit dem PPD-Verzug bei ausgeschaltetem Schalter), Oktober um 14:02 neu abgerufen, Abnahme bestanden:
  alle Pruefwerte Fabios exakt (alle Kunden, Hitachi 32 Kunden / 2'462 / 94.2 %, 682, 24157, 9204, 24940, 682 im Oktober).

## Shopfloor: PPD-Verzug wie ZC22 gebaut (Mail Fabio), 2026-11-02 nachmittags

- Fabios Antwort: DW = `D*` ohne DW1/DL6, Eckendtermin ohne Untergrenze bis Vortag, positive Werte. ZC22-Logik aus dem Quelltext
  gelesen (Menge = Positionsmenge PSMNG/GSMNG, Planauftraege nur mit Umsetzkennzeichen).
- SAP T76, eigener Auftrag `T76K912797`: `ShopAufSet` Modus `Typ eq 'VZ'` (nur DPC, MPC unberuehrt). Abgleich gegen ZC22 je Auftrag
  gleich (DW 1'570 / 49'526, TR5 786 / 41'127, TX 1'349 / 73'895), 0,1 s statt 6 min.
- Cockpit `27badb2`: Verzug je Abteilung, Endpunkte `sap/verzug`, Schalter `SapVerzugEnabled` aus, Tests 1345/1345, nicht deployed.
- Offen: SEH ist mit Material 1-99999 immer 0 (alphanumerische Materialnummern), Rueckfrage an Fabio.
- Abends: Ingo hat `T76K912797` und `T76K912793` (Zeivr) nach P76 importiert, Gateway-Caches in P76 geleert. Vorschau 02.11. aus P76:
  TR5 507, TX 64, DW 60, SEH 0; `Zeivr` 200 erst nach einem zweiten `/IWFND/CACHE_CLEANUP` (LEARNINGS). Offen: Abgleich mit ZC22, dann Schalter an.

## Einkauf Phase 2 gebaut: Materialanteil je Produktgruppe nach Materialgruppe, 2026-11-02

- SAP T76 (Auftrag `T76K912785`, zusammen mit ZLO03 FIX 24): ZLO03 FIX 25 (`P_APP`, Ergebnis ins Memory), Struktur `ZSTR_ZLO_TOPDOWN`,
  `ZloTopDownSet` in MPC/DPC, Caches geleert; Test 41019 (H05039 3 ST zu 0.014133 CHF wie FIX 23). Ein Syntaxfehler in der ersten
  Fassung legte ZLO03 in T76 zehn Minuten lahm (LEARNINGS, Anleitung berichtigt).
- Dashboard `d6c0287`: Lader im Einkauf-Lauf (ZC23 je Monat, Absagegrund leer, ZLO03 je Block, Disponent 001 bis 006, Probe vor dem
  Lesen), eigene aufklappbare Tabelle mit rotem Knopf im Spend-Aufriss, Texte in allen Sprachen, Tests 1325/1325; deployed 11:18 (`f36e1e7`).
- Deploy 10:09 (`a445f30`): Disponent 006, Verkaufsabgleich Shopfloor, Handbuch. CLAUDE.md Abschnitt 5 (Deploy immer mit dem Werkzeug).

## Zeichnungsaenderung: SAP-Daten fuer die MS-Liste der Entwicklung, 2026-11-02

- Anfrage Simon Klaey (ueber Andreas Wild, Alex Donato, Lucas Castro): Benennung, Index und Materialstatus sollen sich in
  der MS-Liste nach Eingabe der Materialnummer fuellen. Simons Excel liest dafuer direkt `P76.MARA`/`P76.MAKT` per SQL;
  die Formel findet bei rund einem Drittel keinen Index und zieht teils die franzoesische Benennung.
- Entscheid (Mail Ingo, von Simon bestaetigt): `Zeivr` an `MARA001Set`, das Dashboard legt naechtlich eine Liste mit vier
  Spalten im SharePoint ab, die MS-Liste liest sie mit Standard-Connectoren. Suche nur ueber die Materialnummer.
- Offen: Zustimmung Lucas, SharePoint-Schreibrecht (Alex), Ablageort (Andreas). `saptasks/zeichnungsaenderung/NOTES.md`,
  Router SAP, `baum.md`, Wochen_Todo 131.
- T76, 12:05: `Zeivr` (MARA-ZEIVR, CHAR 2) an `MARA001Set` ueber einen Zusatz in `ZCL_ZPOWERBI_EINKAUF_MPC_EXT->DEFINE`
  (`docs/abap/ZMARA_ZEIVR_ADD.abap`), eigener Auftrag `T76K912793`, aktiv und zeilengleich rueckgelesen. Gateway Client:
  altes `$select=Matnr,Mstae,Matkl` weiter 200, neues Feld 200 mit Werten (Laengenvergleich), `ZloTopDownSet` und `MAKTSet`
  200. Lesecode unveraendert. `SapGuiSitzungSchliessen.vbs` korrigiert (Index per `CLng()`).
- P76, abends: Ingo hat `T76K912793` importiert, Caches auf `trasp76s` geleert (parallele Sitzung). `$metadata`, `Matnr,Zeivr`
  und die alte Abfrage `Matnr,Mstae,Matkl` 200. SAP-Seite damit fertig; der Dashboard-Abzug wartet auf Lucas, Alex, Andreas.

## Einkauf: Disponenten 001 bis 006 und Bauplan Materialanteil Phase 2, 2026-11-02

- Neuer Einkaufsdisponent 006 (Entscheid Ingo): `PurchasingPlanners` jetzt 001 bis 006, wirkt im Schalter „Nur Einkaufsteile“ aller
  Aufriss-Einstiege, im Lagerwert und im Verlauf je Disponent (rückwirkend, weil alle Disponenten gespeichert sind); Texte in allen
  Sprachen, Handbuch Einkauf. Commit `9a26acc`, Release-Worktree 1318/1318, **nicht deployed**.
- Bauplan `saptasks/zc23_materialanteil/BAUPLAN_PHASE2_MATERIALANTEIL_2026-11-02.md`: Schritt 1 über das vorhandene `Zc23Set`
  (Absagegrund im Dashboard filtern), Schritt 2 braucht `P_APP` in ZLO03 und ein neues Set; Armins Bedarfsmenge war der
  ZLO03-Verbrauch, nicht die ZC23-Menge (Frage F1). Entscheide Ingo und Absender Armin in `NOTES.md`.
- Koordinationsdatei repariert: Zeile „ZLO03 FIX 19“ war in `1209baf` mitgelöscht worden, wiederhergestellt; LEARNINGS.

## Einkauf: Entwurf Lieferantenbewertung V6 und Rechenweg Materialanteil Phase 2 festgehalten, 2026-11-02

- Nur Doku, kein Code. `a.txt`/`a.html` (Lieferantendashboard V6: vier Reiter, drei offene Entscheide A/B/C-Basis,
  Bewertungsrhythmus, zentrale Datenhaltung) in `docs/EINKAUF_SITZUNG_2026-10-22.md` Abschnitt 5, Demo abgelegt unter
  `docs/einkauf_lieferantenbewertung/`. `b.txt`/`b.png` (ZC23 Termin bestätigt -> ZLO03 Top-Down -> Disponenten 001 bis 006 je
  Materialgruppe, Standardpreis x Bedarfsmenge) als Nachtrag zu Phase 2 in `saptasks/zc23_materialanteil/NOTES.md`.
  Die vier Dateien in der Repo-Wurzel kann Ingo löschen. Wochen_Todo 122/123, Router Einkauf, `rag/PURCHASING.md`, `baum.md`.

## Shopfloor: Paket Fabio vom 01.11., Verkauf über Fiori (J4), KEA0, 2026-11-02

- Patch 04 (PPD Terminverschiebungen aus den Rückstandslisten, standardmässig aus) und Patch 05 mit Fabios `apply_patch.js`
  eingespielt, Verkaufs-Config mit `config_verkauf_fiori.js`: Transmitter = TR5 + TX (`ae_tr5`/`u_tr5` entfallen), erwarteter
  Umsatz aus Fiori statt ZC23. Je zuerst `--check`, Ersetzungsstellen und Feldnamen gegen den Port geprüft.
- J4 neu: `ShopfloorSapVerkauf` holt erwarteten Umsatz (Umsatz + offene Lieferungen + offene Aufträge) und Gutschriften aus
  `C_SALESANALYTICSQRY_1_CDS`, Monat des Vortags, Vorschau `GET shopfloor/api/sap/verkauf`, Schalter `SapVerkaufEnabled`.
- KEA0 in T76 gelesen: Ergebnisbereich 1000, `CE11000`, Produktsparte `WWPSP`, Wertfelder des Nettoerlöses (`CE11000`/`WWPSP`
  waren seit Juni aus dem Produktsparten-Mapping bekannt); offen nur Skonto `VV240`/`VV245`, das entscheiden wir selbst gegen
  Fabios Sep-Kontrollwerte. Commit `82f4f52`, Release-Worktree 1318/1318, produktiv 08:21 bitgleich ohne Alarm.
  Vorschau 10/2026 = Fabios Kontrollwerte (8'827'624.28 / 110'412.21 CHF), heute per POST geschrieben, danach
  `SapVerkaufEnabled = true` im Repo (wirkt nach dem nächsten Deploy). `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 16, Wochen_Todo 130.

## ZLO03 FIX 24: Stückkosten ungerundet, Textpositionen nicht im Bedarf, Bottom-Up-Details in Stück, 2026-10-30

- Befunde G aus `zlo03/CLAUDE.md` umgesetzt, nachdem FIX 12 bis 23 am 29.10. in P76 importiert waren. Stückkosten jetzt mit
  10 Nachkommastellen (vorher 2: K50057 mit 0.02 je 10 ergab 0.00 und Wert 0), Feld „Verbräuche zurück in Tagen“ ausgeblendet
  (wirkungslos), Textpositionen zählen in Top-Down und Bottom-Up nicht mehr zum Bedarf, Bottom-Up-Details stehen in der
  Einheit der Stückliste. G4 Mehrstufigkeit nicht geändert, erst gegen CS12 prüfen.
- T76 aktiv in `T76K912785`, rückgelesen identisch. Fünf Läufe Zelle für Zelle gegen den Vortag: nur Stückkosten und Werte
  ändern sich (58944: K50057 0.002, Wert 19.67), 46123/B63485 1 statt 2 ST in beiden Richtungen, H05039-Details in ST.
  Freigabe und Import offen (Ingo). `zlo03/FIX24_RUNDUNG_TEXTPOS_DETAILS_2026-10-29.md`, Wochen_Todo 129.

## ZLO03 FIX 23: Top-Down in Stücklisten-Einheit (Meldung Armin), 2026-10-29

- H05039 in 41019: Basis KAR (1 KAR = 600 ST, Standardpreis je KAR), Stückliste in ST (Top-Down und Bottom-Up-Komponentenspalte); `UNIT_CONVERSION_SIMPLE` setzte ST 1:1
  in KAR um, Bedarfsmenge × Stückkosten war 600-mal zu hoch. Jetzt über MARM: Mengen und Stückkosten in der Einheit der
  Stückliste, Werte in CHF unverändert. T76 aktiv in `T76K912781`, rückgelesen identisch, Lauf 41019 geprüft (0.014133 CHF je ST).
  Freigabe und Import offen (Ingo). `zlo03/FIX23_EINHEIT_STUECKLISTE_2026-10-29.md`, Wochen_Todo 128.

## Trafag Projekte: Smartsheet-Ansicht, Risiken, PMO, Vorhaben, 2026-10-29

- Auftrag Ingo: Smartsheet-Modus mit gleicher Funktion, Balken ziehen mit automatischer Anpassung, Risikobewertung mit
  Kritikalität, alle Vorhaben der Wochen_Todo als Projekte; nach der Sitzung SAP PS zusätzlich Risiken in Franken mit
  Burndown und PMO-Vorlage, Vorlage Produktentwicklung, Projektstrukturplan für SAP, Decision Log.
- Neu: Reiter Raster & Gantt, Risiken, Projektstrukturplan, Entscheide; Vorlagen Produktentwicklung und IT-Projekt mit
  fertigem Plan; Knopf „Vorhaben aus Wochen_Todo übernehmen“ (Projekte PMnn, privat). Terminierung in Arbeitstagen ohne
  Zürcher Feiertage mit Ende-Anfang-Abhängigkeiten, Sammelzeilen und kritischem Pfad, ein Einstieg für alle Terminwege.
- Zusammengeführt als `1e3300d` (Branches `feature/projekte-smartsheet`, `-risk`, `-pmo`), Suite `1311/1311`, deployed
  2026-10-29 12:23 bitgleich ohne Alarm. Im Browser noch nicht gesehen (Chrome-Erweiterung, Edge headless ohne Blazor).
- Doku: `docs/TRAFAG_PROJEKTE_2026-10-09.md` Abschnitt „Smartsheet-Ansicht, Risiken und PMO“, Handbuch Kapitel 8,
  `docs/rag/DEPLOYMENT.md`. Offen: Phase 2 Guide, Phase 3 Auslastung/Automatisierungen/Formulare/Excel des Rasters,
  Abstimmung mit Philip vor breiter Ankündigung.

## Doku: Sitzung SAP PS und Projektmanagement-Werkzeug zusammengefasst, 2026-10-29

- Aufnahme mit Whisper `large-v3` transkribiert, Zusammenfassung als `docs/SITZUNG_SAP_PS_PROJEKTMANAGEMENT_2026-10-29.md`
  (Wunsch Ingo „für zukünftige Tasks“): Projektwerkzeug oben (Philip testet Project Power Pack, Entscheid offen), SAP PS nur
  Geld und Zeit, Schnittstelle Projektstrukturplan; Lucas fragt NTT Data an, Philip liefert Testprojekte 133.2 und 103.46.
  Router SAP, `baum.md`, PROJEKTSTATUS PM-10/PM-13, Wochen_Todo 43 und neu 126 samt `.xlsx`.

## Wuensche Armin: ZLO03 FIX 22 (Disponent, Warengruppe) und ZC23-Gruppe BM fuer Disponent 026, 2026-10-29

- ZLO03 Top-Down: Spalten Disponent (`MARC-DISPO`) und Warengruppe (`MARA-MATKL`) hinter PFAS-Gehalt, im .xlsx als Text
  (`026` bleibt `026`). T76 aktiv in neuem Auftrag `T76K912777` (`T76K912752` war schon freigegeben), rueckgelesen
  identisch (3'813 Zeilen), Lauf Typencode `8475*` 126 x 184. Freigabe und Import nach P76 offen (Ingo).
  `zlo03/FIX22_DISPO_MATKL_2026-10-29.md`.
- ZC23/ZDISPO: in P76 neue Gruppe `BM` = `BG_MECHANIK` und `026` -> `BM`; SM30 von Claude ausgefuellt, von Ingo gesichert,
  lesend gegengeprueft. `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md` Nachtrag 2026-10-29. Wochen_Todo 124, 125.

## Doku: Widersprueche in allen Markdown-Dateien aufgeloest, 2026-10-29

- Auftrag Ingo („alle 163“): alle getrackten `*.md` ausser Archiv und Koordinations-Historie in sieben Aesten geprueft
  (Einstieg/RAG, Finance, Einkauf/Logistik, SAP, HR/Operations/Plattform, Projektstatus, Handbuch). Aufgeloest nur mit
  Beleg (Code, git log, Deploy-Protokoll, Issue-Log, spaeterer datierter Eintrag); datierte Aussagen sind als
  *ueberholt am …* markiert, nicht umgeschrieben. Rund 110 Stellen in etwa 80 Dateien (`4b1f20c`). Nachpruefung im
  Folge-Commit: 21 Finance-Dateien, die der erste Durchgang nur per Suche gesehen hatte, vollstaendig gelesen (rund 60
  weitere Stellen), dazu `.tmp_sap_probe/ddic_lzcode/README.md` und die Befunde 1, 2, 7, 8 der Konsistenzpruefung vom 29.09.
  Neuer Befund: Kursumweg A2 (`ISS-018`) ist seit dem Budgetkurs-Standard vom 07.10. nach Code-Lesung wirksam, nicht latent.
- Schwerpunkte: „nicht deployed“ nach dem Deploy (Einkauf, Netzwerk, Logistik, Journal-Ausgleichsfelder); `T76K912530`
  seit 01.10. und `T76K912728/36/38/40` seit 16.10. in P76; Produktgruppe ohne 1/n seit 26.10.; Kader 8,2 h
  (Mitarbeiterkreis 10) seit 22.10.; `docs/HR_KPI.md` 8.6 neu betitelt, doppelte 8.7 als 8.8; Koordinations-/Abschlussregel
  in `AGENTS.md`, `docs/router/projekt.md`, `docs/rag/init.md` an `CLAUDE.md` (Kurzauskunft 3a, LEARNINGS) angeglichen;
  Handbuch-Kapitel Einleitung, Finance, Einkauf, HR, Projekte, Operations auf den App-Stand berichtigt (Reiter erst nach
  Deploy, Word nicht neu erzeugt; `UserManualContentTests` 4/4).
- Status: Issue-Log `ISS-004` erledigt (4.1 bis 4.3 erledigt), `ISS-006.1` Deploystand, `ISS-007.2` Paola-Antwort versendet;
  Wochen_Todo 9, 66, 93, 114 und Status_kurz (Kader, ZLO03, ZC12) nachgefuehrt, `.xlsx` neu erzeugt.
- Nicht geaendert, nur gemeldet: `zlo03/` und `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` (reserviert), `CLAUDE.md`
  und `Tools/Qdrant/README.md` (fremde unkommittierte Aenderungen). Offene Entscheide fuer Ingo im Chat vom 29.10.
- Qdrant: Index war seit dem abgebrochenen Lauf vom 27.10. nur zu 1'248 von rund 4'400 Abschnitten gefuellt. Neu aufgebaut nach dem Folge-Commit,
  direkt ueber `uv run --python 3.12 Tools/Qdrant/index_docs.py` (Reindex-Docs.ps1 bricht mit Umleitung unter PowerShell 5.1
  ab, Wochen_Todo 118): 165 Dateien, 4'467 Abschnitte, `points/count` = 4'467, Exitcode 0.

## Einkauf Spend-Aufriss: Produktgruppe nach Verwendung : Warengruppe, 2026-10-28

- Wunsch aus der Sitzung 28.10. (Kostenstruktur Material je Produktgruppe, Phase 1): neuer Einstieg Produktgruppe nach
  Verwendung -> Warengruppe -> Material, Basis Einkaufsvolumen, gewichtet nach verwendenden Nummern (ZLO03), Summe exakt.
  SAP-Regel-CTE gemeinsam mit der Produktgruppe genutzt. Commit `8653f6b`, Suite im Release-Worktree 1280/1280, deployed 2026-10-28. Phase 2
  (Materialanteil aus der Standardkalkulation, ZC23 erweitern) offen: `saptasks/zc23_materialanteil/NOTES.md`, Wochen_Todo 123.

## Einkauf: Matrix Lieferant / Jahr im Spend-Aufriss, Reiter Spend ausgeblendet, 2026-10-28

- Wunsch Ingo: Die Matrix „Kaskadierung Lieferant / Jahr“ (aufklappbar bis Warengruppe und Material) ist jetzt eine eigene
  Komponente `PurchasingSupplierYearMatrix` und steht unten im Spend-Aufriss. Der alte Reiter Spend wird beim Start einmalig
  ausgeblendet (Merker `migration-2026-10-28-einkauf-spend-ausblenden`), die Route `/einkauf/spend` bleibt; ein Admin kann ihn
  unter Menuestruktur wieder einblenden. Commit `45b2158`. Nachtrag: der ganze Inhalt des alten Reiters (Kacheln, Spend pro
  Lieferant, Datenstatus, Volumen nach Warengruppe/Region/Waehrung, Matrix, Quellen) steht jetzt unter dem Aufriss, Commit
  `c501053`, Suite im Release-Worktree 1279/1279, deployed 2026-10-28.

## Einkauf: Bild im Kopf, 2026-10-28

- Wunsch Ingo: Bild „Warenkorb mit Sensoren“ (`wwwroot/einkauf/warenkorb-sensoren.jpg`, 676x560, 88 KB) zwischen Titel
  und Ladestand, auf die Kopfhoehe eingepasst; der schwarze Bildgrund verschwindet per `mix-blend-mode: screen`, unter
  1250 px ausgeblendet. Commit `8a568e5`, Suite im Release-Worktree 1279/1279. Deployed 2026-10-28 10:05.

## Einkauf: Ladestatus im Kopf gekuerzt, voller Text im Popup, 2026-10-28

- Wunsch Ingo: Die lange Ladestatus-Meldung rechts im Kopf des Einkauf-Cockpits verzog den Kopfbereich. Sie ist jetzt auf
  drei Zeilen gekuerzt; der Knopf „Ladeprotokoll“ zeigt den vollen Text in einem Popup. Commits `ae67f7e`, `abf3a92`,
  `5c568b0` (Uebersetzungen inkl. Berndeutsch), Suite im Release-Worktree 1279/1279. Deployed 2026-10-28 09:45, siehe `docs/rag/DEPLOYMENT.md`.

## Handbestellwesen: neue Mitarbeitende nur aus dem AD, 2026-10-27

- Entscheid Ingo: AD- und Altbestand-Eintraege unterschieden (Spalte `AusAd`); neue nur aus dem AD, dort gepflegt (Name,
  Mail, Konto gesperrt, „Aus AD aktualisieren“); Altbestand voll pflegbar. Commit `3ff16f5`, Suite 1294/1294, deployed 13:45. Konzept 6.10.
  Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Einkauf Produktgruppe nur mit SAP-Text, 2026-10-27

- Wunsch Ingo: Spend-Aufriss zeigt „FP_DICHTEWÄCH“ statt „D2 - FP_DICHTEWÄCH“. Commit `0917d9e`, Suite 1293/1293.
  Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Handbestellwesen: Stammdaten loeschen, Mitarbeitende aus dem AD, 2026-10-27

- Loeschen nur ohne Bestellungen (Mitarbeitende mit Bestellungen werden deaktiviert), AD-Suche fuellt Name, Mail, Konto.
  Commit `a8e9eec`, Suite 1293/1293. Details Konzept 6.9. Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Handbestellwesen: Freigabe wie PHP, umschaltbar, 2026-10-27

- Entscheid Ingo (1a): Freigeben darf jede aktive Person mit genuegend Limit, gleiche Kostenstelle zuerst. Schalter
  `Handbestellwesen:FreigabeNurGleicheKostenstelle` fuer die strenge Regel. Grund: 929 von 1'032 Bestellungen der letzten
  12 Monate haetten sonst keinen Freigeber. PHP ist abgeschaltet, Reiter sichtbar. Commit `a0030e8`, Suite 1291/1291.
  Details Konzept 6.8. Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Handbestellwesen klassisch: Link „Zur neuen Version“, 2026-10-27

- Wunsch Ingo: Auf `/handbestellwesen` steht oben unter dem Logo ein blauer Link auf `/bestellwesen`. Commit `9ef7787`,
  Suite 1290/1290. Details `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` 6.6. Deploy siehe `docs/rag/DEPLOYMENT.md`.

## SAP ZZPRDAT: Live-Pruefung vor dem Abnahmetest, 2026-10-26

- In T76/100 nur lesend geprueft (RFC ueber SapProbe, in SAP nichts geaendert, P76 nicht beruehrt): Der aktive
  Quelltext ist identisch mit `saptasks/zzprdat/produktiv/`, die letzte Aenderung war am 07.09., das BAdI
  `Z_ZZPRDAT_UPDATE` ist aktiv, `T76K912490`/`T76K912491` sind offen, und es gibt keinen weiteren Transport.
- Neu dokumentiert sind sieben Testauftraege der Benutzer TAN/FPA (1241831 bis 1241844, 08. bis 23.09.). Sie passen
  zum Sollverhalten, die Aenderungsbelege wurden nicht gelesen.
- Nachgefuehrt: `saptasks/ZZPRDAT_TRANSPORTPLAN.md` (Abschnitt „Live-Pruefung vom 2026-10-26“), `saptasks/zzprdat-kontext.md`,
  `docs/rag/PROJECT.md`, `projektmanagement/PROJEKTSTATUS.md` (PM-03), `Status_kurz.tsv`, `Wochen_Todo.tsv` (Zeile 56)
  samt `.xlsx`, `docs/rag/LEARNINGS.md`. Ergebnis des Abnahmetests noch offen.

## Einkauf: Schalter „Nur Einkaufsteile (Disponent 001 bis 005)“ im Spend-Aufriss, 2026-10-26

- Entscheid Ingo: Schalter fuer alle Einstiege, Vorgabe an; ausgeschaltet ganzer Einkauf. Diagramme unveraendert ganzer
  Einkauf. Commit `c3147e1`, Suite 1290/1290. Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Einkauf: Spend-Aufriss nach Disponent (zwei Sichten), 2026-10-26

- Wunsch Ingo: gruppieren nach ZC23-Feld DISPOKENNZ (eigener MARC-DISPO, 001, 002 ...) und zusaetzlich nach dem
  Disponenten der verkuerzten Nummer aus ZLO03 (1/n verteilt). Beide Summen exakt. Commit `bddf9f9`, Suite 1289/1289.
  Deployed 2026-10-26 14:38 (von Ingo gestartet). Bottom-Up-Abruf bleibt deshalb an.

## Einkauf: Produktgruppe aus eigenem Disponenten wie ZM_OFFENE_KDAUF, 2026-10-26

- Entscheid Ingo: MARC-DISPO (Werk 1100) des Einkaufsmaterials -> ZDISPO_GRP/ZDISPO_SPART -> genau eine Gruppe; keine
  Vererbung ueber die Verwendung, keine 1/n-Verteilung. Mehrdeutige SAP-Regeln (DS1/DS2, 016): kleinste Gruppennummer.
  Neue Spalte `MarcDispo`, fuellt der naechste Einkauf-Lauf. Suite 1288/1288. Deployed 2026-10-26 09:39, Delta danach von Ingo von Hand gestartet (Ergebnis nicht nachgeprueft).

## Einkauf: Produktgruppen-Sichten ohne „uebrige (n)“, 2026-10-23

- Wunsch Einkauf „alle Positionen anzeigen“: Produktgruppen-Einstiege waren als einzige noch auf 20/15/10 gedeckelt;
  jetzt ungedeckelt wie alle anderen seit 2026-08-27. Suite 1289/1289. Deployed 2026-10-23 09:05.

## Einkauf: Spend-Aufriss Produktgruppe : Warengruppe, 2026-10-23

- Zweiter Produktgruppen-Einstieg Produktgruppe -> Warengruppe -> Material (Wunsch Einkauf, Budgeterstellung); der
  bisherige heisst jetzt „Produktgruppe : Lieferant“. Gleiche Summen, gleiche 1/n-Verteilung. Suite 1288/1288. Deployed 2026-10-23 07:46.

## Whisper Studio: neue Oberflaeche, lokale KI, `whisper/` versioniert, 2026-10-22

- Auftrag Ingo: Fortschrittsanzeige, dann Ausbau zum „Whisper Studio“ mit moderner Oberflaeche. Entscheide Ingo:
  Dateiname `JJJJ-MM-TT_HHMM_Titel`, KI nur lokal, Funktionen Aufnahme, Word/SRT/VTT, Editor und Verlauf, KI-Protokoll.
- Neu `whisper/gui/WhisperStudio` (WPF, Fluent) und `whisper/gui/WhisperStudio.Tests` (31 Tests gruen, darunter echte
  Laeufe von KI, Whisper und Aufnahme). `transcribe.py`/`.ps1`: Fortschrittszeilen, `-Task translate`, `.json` mit Segmenten.
- Lokale KI: llama.cpp `llama-server` b11799 und Qwen2.5-3B-Instruct Q4 (2,1 GB) in `whisper/llm/` (nicht versioniert).
  Protokoll eines kurzen Testtextes in 25 s.
- Entwickelt in der Kopie `.tmp_tools/whisper_kopie`, weil im Original ein Lauf aktiv war; uebernommen per Skript mit
  Zip-Sicherung `whisper/backup/whisper_quellen_20261022_130852.zip`. Seither sind die Quellen von `whisper/` in Git.
- Nicht geprueft: Bedienung mit der Maus (Drag&Drop, Tabelle bearbeiten, KI-Knoepfe). Kein Deploy, Werkzeug laeuft lokal.
- Nachtrag: Alte WinForms-GUI `whisper/gui/WhisperGui` auf Wunsch Ingo entfernt (Quellen bis `ae599c2` in Git), die
  Desktop-Verknuepfung „Whisper“ startet jetzt das Studio.
- Nachtrag: Aufnahmeseite mit analogem VU-Meter (Zeiger, roter Bereich ab 0 VU, PEAK-Lampe ab -1 dBFS) statt Pegelbalken,
  Wunsch Ingo. `Controls/VuMeter.cs`, `Recorder.Measure` (Spitze und RMS), 35 Tests gruen, Release neu gebaut.
- Details: `whisper/HANDOFF.md`.

## Doku: Markdown-Aufraeumen, Archiv `docs/archiv/`, 2026-10-22

- Auftrag Ingo: alle Markdown-Dateien aufraeumen, keine Redundanzen, keine Informationen verlieren. Nur Doku,
  kein Code, kein Deploy. Entscheide Ingo: neues Archiv `docs/archiv/` statt Wiederherstellung von
  `docs/raw_md_archive/` (bleibt nur in Git); `rag/DEPLOYMENT.md` behaelt Deploys ab 2026-10-13.
- Verschoben, jeweils verbatim und mit Zeilenvergleich belegt (jede nichtleere Originalzeile ist in Haupt- oder
  Archivdatei vorhanden): 51 Kurzindex-Zeilen vor dem 2026-10-15 aus `docs/AGENT_COORDINATION.md` in die
  Historie (146 -> 95 Zeilen); 58 Abschnitte 2026-07-30 bis 2026-09-30 aus dieser Datei nach
  `docs/archiv/LASTCHANGE_ARCHIV_2026-08-01_bis_2026-09-30.md` (2'643 -> 560 Zeilen); 101 Kurzstand-Eintraege
  2026-08-03 bis 2026-10-09 aus `docs/rag/DEPLOYMENT.md` nach `docs/archiv/DEPLOYMENT_KURZSTAND_ARCHIV_bis_2026-10-12.md`
  (1'440 -> 270 Zeilen, Fallen bleiben).
- Befunde (Doku-Drift, in LEARNINGS): die Historie hatte seit dem 2026-09-30 keinen Volltext mehr, obwohl
  Hauptdatei und `router.md` ihn versprachen; `rag/DEPLOYMENT.md` behauptete „nur den aktuell verifizierten
  Deploy“ bei 1'340 Zeilen Historie; drei Deploy-Eintraege (10.09., 09.09.) trugen Reste eines PowerShell-Strings
  (`" . "`) am Zeilenanfang (im Archiv so belassen, vermerkt); `baum.md` fehlten 17 versionierte Dateien
  (Handbuchkapitel, `saptasks/*/NOTES.md`, Berechtigungsfall AT) und fuehrte `docs/rag/init.md` als nicht versioniert.
- Nachgefuehrt: `baum.md` (Ast Projekt nach Themen aufgeteilt, Abschnitt Benutzerhandbuch, ZLO03, Archiv),
  `router.md` Regel 6, `docs/rag/init.md`, `docs/router/projekt.md`, `docs/rag/{ARCHITECTURE,FINANCE,PROJECT}.md`
  (Archivverweise), `docs/DEPLOYMENT.md` (Sicherungsregel seit 13.10. als ueberholt markiert), `docs/archiv/README.md`.
- Nicht angefasst, als Folgearbeit benannt: die Koordinations- und Abschlussregel steht viermal (`CLAUDE.md`,
  `AGENTS.md`, `router.md` Regel 6, `docs/router/projekt.md`); `CLAUDE.md` war von einer anderen Sitzung geaendert.

## HR-Cockpit: Kader = SAP-Mitarbeiterkreis 10, 8,2 h, 2026-10-22

- Entscheid Ingo: Kader ist der Mitarbeiterkreis 10 „ML mit Kadervertrag“ (T503T) mit 8,2 h je Tag statt Rexx „Leitung j/n“
  mit 8,1 h. Wirkt auf FTE aus Sollzeit, Krankheitstage und Langzeitgrenze. Suite 1288/1288. `docs/HR_KPI.md` 8.7.

## Handbestellwesen: moderne Oberflaeche /bestellwesen, 2026-10-22

- Neue Seite mit MudBlazor, gleiche Logik: Kacheln, Kreditor-Suche, Live-Pruefung der Berechtigung, Freigeber-Vorschlag,
  Doppelbestellungs-Hinweis, Nummer kopieren, Filter offene, Stammdaten/Nutzung fuer Admin. Deploy 14:52 (`d7478cd`), ohne
  Alarm. Konzept 6.6.

## Handbestellwesen: Datenpruefung und Bereinigung, 2026-10-22

- Nach Ingos Import (14:27) geprueft: keine doppelten Nummern; 25 Doppelklick-Doubletten, rund 930 offene aelter als 1 Jahr.
  Admin-Kachel „Bereinigung“ (Vorschau, Doubletten stornieren, alte offene ausbuchen, protokolliert). Deploy 2026-10-22 14:40 (37e5d7b), bitgleich, zehn Routen 200, Alarm nur WAL/SHM-Checkpoint beim Herunterfahren.

## Handbestellwesen: Altdaten-Import, 2026-10-22

- Deployed 14:22 (`3609b3e`), bitgleich, Alarm nur SHM. Dump liegt auf dem Server unter `hbdaten\import\db.sql`; Pruefen und
  Importieren macht Ingo unter `/handbestellwesen/import`.

- Admin-Kachel „Altdaten-Import“ (Pruefen, dann Importieren in leere DB), Fehler der PHP-Daten korrigiert statt uebernommen
  (Platzhalter, Text im Betrag als `WertText`, Datumstexte, Doubletten, Zaehler). Probelauf echter Dump: 12'629 Bestellungen.
  Konzept 6.3. Suite 1285/1285.

## Deploy 2026-10-22 13:38: Einkauf-Feedback und Modul Handbestellwesen

- `77778a5` produktiv nach Ende des Einkauf-Deltas, Wochensicherung, bitgleich, zehn Routen 200, ohne Alarm. Reiter
  Handbestellwesen unsichtbar angelegt. Dump (`db.sql`, 8 Tabellen) und neueste PHP-Dateien erhalten, Befund im Konzept 6.2
  (Basic-Auth-Datei heisst `htaccess` ohne Punkt, Schutz nicht aktiv).

## Einkauf: Feedback Erich/Sandro/Marco umgesetzt, 2026-10-22

- Kontraktpositionen „vermutlich ersetzt“ (46009671 Pos. 10, insgesamt 4 Pos. CHF 1,29 Mio.) nicht mehr im offenen Wert;
  Filter abgelaufene Kontrakte; Altposten (Textpositionen, Termin > 1 Jahr vorbei, 168 Pos. CHF 3,1 Mio.) nicht mehr
  offen/ueberfaellig; Hinweis „Zeitraum nicht angewendet“ und „Rechnet“; Lagerwert je Disponent; Top-Spend-Zeile weg;
  Bestellbedarf & Deckung, Materialabhaengigkeit, Interaktiv ausgeblendet (Verwendungs-Cache nur 5 Materialien),
  „Gehirn“ eingeklappt. Reiter Handbestellwesen wird unsichtbar angelegt. Suite 1279/1279.
  `docs/PURCHASING_DASHBOARD_2026-06-05.md` Nachtrag 2026-10-22.

## Einkauf: Handbestellwesen lebt weiter, Nutzung wird festgehalten, 2026-10-22

- Entscheid nach der Sitzung (Ingo): weiterbetreiben, aber festhalten, wer es wann wie viel braucht. Modul ergaenzt:
  Aufruf je Konto und Tag im Protokoll, Kachel „Nutzung“ (nur Admin) mit Auswertung je Person, Kostenstelle und Monat.
  Test neu, Suite 1275/1275. Nicht deployed.

## Einkauf: Sitzung 2026-10-22

- Ergebnisse festgehalten (`docs/EINKAUF_SITZUNG_2026-10-22.md`): Einkaufs-Dashboard bis 28.10. stabilisieren, keine
  neuen Funktionen; Feedbackliste Erich/Sandro/Marco kommt; neuer Wunsch Lieferantenbewertung/-entwicklung (Pilot
  danach, Datenquellen offen); Handbestellwesen: Abschalten statt Abloesen steht im Raum, Entscheid offen.
  Wochen_Todo 120 angepasst, 121 und 122 neu.

## Einkauf: Handbestellwesen, Konzept zur Abloesung der PHP-Anwendung, 2026-10-22

- **Phase 1 programmiert, nicht deployed:** neuer Reiter `/handbestellwesen` (Kacheln und Farben wie im PHP), Modul
  `Modules/Handbestellwesen/`, eigene DB `hbdaten/handbestellwesen.db` mit Sicherung, Limit/Freigabe/Wareneingang/Loeschen
  auf dem Server geprueft, Protokoll je Aktion. Tests 9 neu, Suite 1274/1274. Import der Altdaten folgt mit dem Dump.

- Alte PHP-Anwendung analysiert (Passwoerter im Code, SQL-Injection, Freigabelimit nur im Browser, privater Hoster,
  Nummernkollision). Entscheide Ingo: Modul im Dashboard, Altdaten uebernehmen, Stammdaten erst lokal dann SAP,
  Oberflaeche wie im PHP. Nachtrag: Betrieb lokal bei Trafag (nicht externer Hoster), Benutzer ohne SAP-Konto, Plugin/KISS,
  AD, spaeter Schreiben nach SAP (Lizenzfrage). Kein Code. Wartet auf MySQL-Dump. Wochen_Todo 98 geschlossen (Delta 14.10.
  war erfolgreich). `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md`;
  `handbestellwesen/` in `.gitignore`.

## SAP: ZLO03 FIX 20, „defekte Excel“ ist eine Excel-Sperre, Download als .txt, 2026-10-22

- Meldung Ingo: ZLO03 erzeugt seit einiger Zeit defekte Excel. Befund: Die Datei ist Tab-Text unter `.xls`; Excel weist
  das ab, seit die Gruppenrichtlinie `ExtensionHardening = 2` gilt (auch alte Dateien vom August). Als `.txt` oeffnet
  dieselbe Datei korrekt (331 x 2'097). Sofortloesung: umbenennen in `.txt`.
- FIX 20 in `Z_ZLO03_TURBO2` (`download_dialog`): Vorschlag `.txt`, Oeffnen mit `excel.exe`, Rueckfall Standardprogramm.
  T76 aktiv, rueckgelesen identisch, Lauf `8475*` oeffnete Excel direkt, Inhalt byteweise gleich. Auftrag `T76K912742`.
- Offen: welches Programm ZLO03 in P76 startet (ungeprueft), andere Z-Reports mit gleichem Muster.
  Doku `zlo03/FIX20_DOWNLOAD_TXT_2026-10-22.md`.
- Nachtrag: `T76K912742` am 22.10. von Ingo freigegeben (mit FIX 12, 19, 20). Auf Wunsch Ingo FIX 21 vorbereitet
  (echtes `.xlsx` per `CL_ABAP_ZIP`, Prototyp zellgleich mit dem Text-Import). Danach auf Auftrag Ingo eingespielt:
  neuer Auftrag `T76K912752`, aktiv, rueckgelesen, Lauf oeffnet Excel direkt, 0 Unterschiede zum Textinhalt, Lasttest
  722 x 3'114 Zellen in 14 s. Import nach `T76K912742` (Ingo).

## SAP: ZLO03 Spalte RoHS (Nachtrag FIX 19), 2026-10-22

- Adil hat in T76 das Merkmal `ROHS` angelegt („RoHS Compliant“, CHAR 3, COM/UNC/UND, Klasse `Z_PACK_REGULATION`).
  ZLO03 (`Z_ZLO03_TURBO2`) gibt jetzt vier Spalten hinter `Postyp` aus: RoHS, REACH, SVHC, PFAS-Gehalt.
- T76 aktiv (22.10. 08:00), rueckgelesen identisch, im offenen Auftrag `T76K912742`. Lauf `8475*` fehlerfrei, 182 Spalten,
  Werte leer (T76 ohne gepflegte Materialien). Ob `ROHS` in P76 existiert, ist ungeprueft.
- Doku `zlo03/FIX19_STOFFCOMPLIANCE_SPALTEN_2026-10-20.md` Abschnitt 6, Quelltext `zlo03/Z_ZLO03_TURBO2_FIX19_ROHS_2026-10-22.abap`.

## SAP PS: Rolle je Zeitbuchung (Frage Philip Steiger), Demo T76, 2026-10-21

- Antwort: Rolle als eigene Leistungsart (gleicher Tarif moeglich). In T76/100 auf Freigabe Ingo: KL01 `PL`, `ENG`, `TST`, KP26
  66 CHF/h auf Kostenstelle 240, KB21N Beleg 311258908 (Personalnummer 3216: 2 h PL, 4 h ENG, 3 h TST), CJI3 zeigt Rolle,
  Stunden und Person. Doku Abschnitt 4b in `docs/SAP_PS_EINFUEHRUNG_DEMO_2026-10-20.md`, Bilder 19-22, Folien 17-19 in
  `20260223 - Abgleich TPG - Trafag SAP PS.pptx` (nicht versioniert).

## Status: Wochen_Todo, Abgleich Code gegen Doku, Qdrant-Index, 2026-10-21

- Code-Commits seit 01.10. gegen Doku geprueft: nach dem letzten Deploy `c3c45cd` (19.10.) gibt es keinen App-Code; produktiver Stand
  und HEAD stimmen ueberein. Einzige Abweichung ist die fremde, unkommittierte Finance_All-Automatik (Wochen_Todo 117, Entscheid Ingo).
- Qdrant-Index neu aufgebaut: 4245 Abschnitte statt 3884, vorheriger Stand 13.10. Reindex-Skript bricht bei umgeleiteter
  Fehlerausgabe ab (Wochen_Todo 118, LEARNINGS). Zeile 15 (Journal-Ladelauf CH/AT) mit Pruefhinweis versehen.
- Mail und Teams nicht durchgesehen (M365-Sitzung abgelaufen).

## Doku: HR-Cockpit SAP-Abruf als produktiv nachgefuehrt, 2026-10-20

- `docs/rag/HR_KPI.md` und lastchange sagten noch „`HrKpiSet` nicht in P76“ bzw. „SAP-Datei nicht automatisiert“. Live geprueft:
  beide SAP-Dateien in `hrdata` taeglich um 05:07 geschrieben (heute), Rexx-Dateien vom 08.07. Aussagen als ueberholt markiert.

## Projekte: Abschlussrunde Dashboard und Data Lake eingeladen, 2026-10-20

- Ingo hat die Runde mit Finance, Einkauf, Supply Chain, HR und Sales (90 Min.) am 2026-10-20 in Outlook verschickt.
  Agenda: Ziel (5), Stand Dashboard (15), Runde durch die Bereiche (40), Data Lake Zielbild und Datenverantwortung (15),
  Abschluss und Abnahme je Bereich (10), naechste Schritte (5). Entwurf `Downloads/Einladung_Abschlussrunde_Dashboard_DataLake_2026-10-20.eml`.
  Nachgefuehrt in Status_kurz, Wochen_Todo 116 und PROJEKTSTATUS (PM-10, PM-12).

## SAP PS: Einfuehrung und Demo in T76, 2026-10-20

- Auftrag Chef: wie funktioniert PS (Zeit/Leistung auf Projekte, Teilprojekte, externe Kosten), einfach oder schwierig.
  In T76/100 Projekt `DEMO-PS` mit zwei Teilprojekten angelegt und freigegeben; Befund: PS fuer Trafag nicht eingerichtet
  (Standardprofil auf 0001, Komponente Projekte im KoKrs 1000 aus, CATS-Profil ohne Projektspalte). Auf Wunsch Ingo
  „Projekte aktiv“ in OKKP nur in T76, lokaler Auftrag `T76K912750` ohne Ziel. KB21N 6 h auf `DEMO-PS.1` (396 CHF, Beleg
  311258907), in CJI3 sichtbar. Doku `docs/SAP_PS_EINFUEHRUNG_DEMO_2026-10-20.md`, Bilder `docs/img/ps_demo_2026-10-20/`.
- Nachtrag: externe Kosten durchgespielt (Bestellung ME21N mit Kontierung P; in T76 brach die Verbuchung an der Nachricht ab,
  deshalb Rechnung FB60 1'500 CHF auf `DEMO-PS.2`, Beleg 19102004); CJI3 zeigt zusammen 1'896 CHF. P76 lesend geprueft:
  PS nicht eingerichtet, keine Projekte, keine CATS-Zeiten, Projekte laufen als Innenauftraege (44 seit 2025).
- Folien 11 bis 16 in der Kopie `20260223 - Abgleich TPG - Trafag (mit SAP PS).pptx` (18 Folien, Original unveraendert).

## SAP: ZLO03 FIX 19 Spalten REACH, SVHC, PFAS-Gehalt, 2026-10-20

- Wunsch Florian Waechter: Stoff-Compliance in ZLO03. Befund: in P76 nur Klasse `Z_PACK_REGULATION` (8 Merkmale, kein RoHS),
  `ZCOMP_STOFF` nur in T76 und leer. FIX 19 liest `REACHCOM`, `SVHCCOMP`, `PFAS` aus `AUSP` und gibt drei Spalten hinter
  `Postyp` aus. T76 aktiv, im Auftrag `T76K912742` zusammen mit FIX 12 (Entscheid Ingo). Lauf `8475*` fehlerfrei, Spalten leer.
  Doku `zlo03/FIX19_STOFFCOMPLIANCE_SPALTEN_2026-10-20.md`. RoHS: Adil soll ein Merkmal anlegen (*erledigt, Spalte seit 22.10.*).

## SAP: Testuser UBIG mit GIB-DCP-Rolle in T76/200, 2026-10-19

- Auftrag Ingo: User `UBIG` in T76 Mandant 200 angelegt, nur Rolle `/GIB/DCP_SUPERVISOR` (Profil generiert, abgeglichen).
  Kennwort von SAP zufaellig erzeugt; Ingo setzt das Initialkennwort selbst. Bestand der GIB-Rollen und Transaktionen in
  `saptasks/gib_dcp/NOTES.md`. Laut Ingo sind die GIB-Module installiert und die Pilotphase ist abgeschlossen.

## Doku: Word GIB Dispo-Cockpit DCP, 2026-10-19

- `docs/GIB_DispoCockpit_DCP_Zusammenfassung_2026-10-19.docx` (3 Seiten): Module DCC/DCO/DCP/DCF, was DCP macht, Technik, Rollout-Schritte,
  Ueberschneidung mit Operations, ZM_OFFENE_FAUF, Logistik live und Einkauf, offene Fragen. Aus oeffentlichen Quellen, Trafag-Umfang offen.

## Doku: Word ZC23 Materialanteil, Beschleunigung, 2026-10-19

- `saptasks/zc23_materialanteil/ZC23_Materialanteil_Beschleunigung_2026-10-19.docx` (4 Seiten) mit Generator `word_beschleunigung.js`. Nur Doku.

## SAP ZLO03: FIX 12 in T76 am nachgestellten Fall bestaetigt, 2026-10-19

- Testdaten `ZPOWERBI_VC_TXT` in T76 gefuellt, Textposition B63485/46123 vor die echte Position gelegt. Alte Fassung: B63485 fehlt;
  mit FIX 12: MK 5'009 wie `ZMD04_CALC`, uebrige 122 Zeilen gleich. `T76K912742` bereit fuer P76. `zlo03/MK_B63485_FIX12_2026-10-16.md` Abschnitt 6.

## Operations: Mengen aus SAP auf dem Produktionstag, DW prod, 2026-10-19 (`ded2b65`, `c4dca10`, `c3c45cd`)

- Logistik und Produktionsmengen stehen auf dem Produktionstag (ERSDA/Istendtermin), Montagsregel weg; Abgleich schreibt heute und
  drei Tage zurueck; Bereich ohne Rueckmeldung bleibt leer. Neu DW prod aus DL790. 15. bis 19.10. umgebucht. Produktiv 10:04.

## SAP ZLO03: FIX 12 in `Z_ZLO03_TURBO2` (MK-Abweichung B63485), 2026-10-16, T76 `T76K912742`

- Mechanismus in P76 belegt (nur lesend), Nachweis am Fall erst nach Import: B63485 hat vier Textpositionen (46123/24/26/28); die Kennzahlen wurden nur beim ersten Treffer
  gefuellt, bei einer Textposition blieben MK, Bestand und Kosten 0. Fix in beiden Zweigen, in T76 aktiv; Import P76 durch Ingo.
- Ueberholt: die Aussage in `zlo03/CLAUDE.md`, die Textpositions-These sei widerlegt. `zlo03/MK_B63485_FIX12_2026-10-16.md`.

## Doku: Arbeitsplatz Ingo, iPhone-Hotspot trennt alle zwei Minuten, 2026-10-16

- Nachtrag im Abschnitt „wiederkehrende WLAN-Aussetzer“ in `docs/router/plattform.md`: Muster aus dem Ereignisprotokoll, Ausschluesse,
  Loesung durch neuen Hotspot-Namen (frisches Profil), naechste Schritte falls es zurueckkehrt. Nur Doku.

## Einkauf: Stücklisten-Netz „Gehirn“ im Spend-Aufriss, 2026-10-16 (`d188806`, `d6ca646`)

- Prototyp zuoberst im Spend-Aufriss: Stuecklisten als leuchtendes Netz (WebGL, three.js), Endprodukte aussen, Komponenten innen,
  Kreuzpunkte gross; Farbe umschaltbar LZ-Code/Lieferantenrisiko/Stufe; Klick hebt Bahnen hervor. 11'005 Materialien, 24'979 Straenge.
- Handbuch Einkauf um den Abschnitt ergaenzt.

## Menue: alle Hauptbereiche farbig, Admin grau, Home pulsiert, 2026-10-16 (`1931084`)

- *Ueberholt (10:17, „dezenter, kein Rosa“):* Finance Backstein, HR Rostrot, Einkauf Dunkelrot, Verkauf Terrakotta, Logistik Ziegelbraun, leiser animiert.
- Erste Fassung 10:13: Finance (Bordeaux), HR (Himbeere), Einkauf (Weinrot), Verkauf (Korallrot), Logistik (Ziegelrot) wie Operations hervorgehoben
  und animiert; Admin Bereich grau hinterlegt; Home im Trafag-Orange mit Pulsieren. `NavMenuNode.razor`, `app.css`.

## Operations: Mengen aus SAP live, Handbuch mit Prozessgrafik und Schnellueberblick, 2026-10-16

- Logistik (ZC27), Tagesmengen SEH/TR5 und FPY SEH kommen seit 09:51 automatisch aus P76 (`9d6b5d2`); vorher gegen Fabios
  Kontrollwerte geprueft, alle gleich. Erstlauf fuer den 16.10. geschrieben.
- Handbuch Kapitel Operations: neuer Abschnitt „Schnellueberblick“ mit Prozessgrafik im Trafag-CI (Quellen, Cockpit, Meeting,
  Rueckwirkung auf SAP), Seitentabelle und Tagesrhythmus; Handbuch kann jetzt Bilder (Reiter SVG, Word PNG).

## Benutzerhandbuch: Kapitel Operations erweitert, 2026-10-16

- Neuer Abschnitt zum Geschaeftsprozess (taegliches Shop-Floor-Meeting, Stand D-1), jede Shopfloor-Seite mit Wozu, Inhalt,
  Rechnung, Einsatz nach Rolle, Interpretation und Grenzen (Ampel, Lieferzeiten, Rueckstaende, PPD, Forecast mit DS, Logistik nach
  ZC27, Verkauf mit Book to Bill, CZ/MTF/Sensoren, Schnittstelle); Glossar von 9 auf 21 Begriffe. Word-Datei neu.

## Operations (Shopfloor): Mengen aus SAP vorbereitet, 2026-10-16 (`2bc54bd`, T76)

- Neue SAP-Sets `ShopRmSet` (ZC27-Logik) und `ShopProdSet` in T76, gegen ZC27 gleich. Das Cockpit rechnet daraus Logistik
  (MLE01/02/04, MLM01, MLE08+09), Tagesmengen SEH/TR5 und FPY SEH; Vorschau `sap/mengen` produktiv, Schreiben aus bis zum
  Transport nach P76 und dem Abgleich mit Fabios Kontrollwerten. Details `saptasks/shopfloor_sap/NOTES.md`.

## Operations ohne Login, 2026-10-16 07:48 (`70a02b1`)

- Auftrag Ingo: `Shopfloor:OpenForAll = true`, alle Windows-Benutzer sehen und bearbeiten den Shopfloor ohne Anmeldung. Der Admin-Schalter
  bleibt und haette Vorrang, wenn ihn jemand setzt.

## Operations (Shopfloor): Paket Fabio 2026-10-16, Ampel, Lieferzeiten, Forecast DS (`b2103a4`, produktiv 2026-10-16 07:06)

- Fabios Patch 03 (Wuensche D. Tobler) und Config-Patch 04 eingespielt: Verlaufskacheln, Ampel pro Bereich, Menue
  „Lieferzeiten“ mit Grund, PPD ohne Verzug MTS, Forecast mit eigener Zeile DS. Zwei Korrekturen am Paket (FPY SEH ueber
  `src`, DW1/DL6 ohne Abteilung). ZD05-Abgleich seit 07:28 um 04:00 und 12:45 (`a285009`). Ampel per zweitem Klick loeschbar, `planned_source` aus Fabios Paketen wird gelesen (`e6c007f`). Die SAP-Jobs J2 bis J11 sind nicht gebaut. Details `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 14.

## SAP Fiori: App ZC23 live mit Selektion, 2026-10-15 (T76 fertig, Transport `T76K912738` offen)

- `http://travt762.sap.trafag.com:8000/sap/bc/ui5_ui5/sap/zfi_zc23/index.html?sap-client=100`: Selektion wie ZC23,
  Kacheln (Positionen, offener Wert, Standardwert, Materialanteil, Arbeit CZ, Kunden), Liste mit Suche und CSV, Trafag-Logo.
- Die Logik bleibt in ZC23: neues EntitySet `Zc23Set` in `ZPOWERBI_EINKAUF` startet `ZM_OFFENE_KDAUF` mit `P_APP`
  (Ergebnis nach dem Materialanteil ins Memory, keine Liste, keine Power-BI-Fortschreibung). Q2-Abgleich gleich
  `ZPOWERBI_KDAUF` (4'392 Zeilen, alle drei Summen). Details `saptasks/fiori_zc23/NOTES.md`.

## SAP Fiori: erste Test-App `ZFI_ERSTE_APP`, 2026-10-15 (in T76 hochgeladen, laeuft im Browser)

- Kleine SAPUI5-App als Harness für künftige Erfassungs-Apps: liest 50 Zeilen `ProductDivisionRefSet` aus
  `ZPOWERBI_EINKAUF_SRV`, nur lesen, T76 `$TMP`. Quelle und Vorgehen `saptasks/fiori_erste_app/` (NOTES.md).
- Läuft im Browser: `http://travt762.sap.trafag.com:8000/sap/bc/ui5_ui5/sap/zfi_erste_app/index.html?sap-client=100`,
  Hochgeladen per RFC `/UI5/REPO_LOAD_FROM_ZIP` mit SapProbe (`--set "NAME=@datei:<Pfad>"`, neu), ICF-Knoten aktiv.
  Geprueft in Chrome: 50 Zeilen. Fallen: Seite ohne `sap.m.App` 0 px hoch, `$top` wird vom Dienst ignoriert,
  UI5-Dateien ein Jahr im Browser-Cache.

## SAP ZC23: neue Spalten Materialanteil, 2026-10-15 (T76 fertig, Transport `T76K912736`, Import P76 offen)

- ZC23 zeigt neben dem Standardwert den Materialanteil (Zukaufmaterial aus der Standardkalkulation, Regel wie ZM03),
  je Einheit und gesamt; Block „Materialanteil“ im Selektionsbild: ein/aus, freigegebene oder jüngste Kalkulation,
  Fremdleistung zum Material. Standardwert und Materialanteil auch in `ZPOWERBI_KDAUF` und im OData-Dienst (Power BI).
  Dazu Spalte „Arbeit CZ“ (Eigenleistung an CZ-Arbeitsplätzen wie ZM03), Spalten direkt hinter dem Standardwert,
  Hauswährung des Werks in der Überschrift. Details `saptasks/zc23_materialanteil/NOTES.md`.
- Anwenderdoku als Word: `docs/ZC23_Materialanteil_Formeln_2026-10-15.docx` (Felder, Formeln, Beispiel B87813,
  Bedienung, Grenzen, Prüfung), Generator `Tools/BuildZc23Formeln`. Formal geprüft (OpenXML, 21 Pflichttexte),
  visuell noch nicht gesichtet.

## Einkauf Spend-Aufriss: Platzhalter „NA - N/A“ nimmt keinen Anteil mehr, 2026-10-15 (`7b62544`, produktiv 12:48)

- Materialien mit einer echten Produktgruppe und dem SAP-Platzhalter `NA` („N/A“) wurden halb/halb verteilt
  (gemeldet an B53173: T2 und NA). Jetzt zählt `NA` neben einer echten Gruppe nicht mit; gilt für alle Materialien.
  Nur unter `NA` verwendete Materialien bleiben dort sichtbar. Mehrere echte Gruppen werden weiter 1/n verteilt.

## Operations: Login per Admin-Schalter abschaltbar, 2026-10-15 (`8bfe424`, produktiv 12:15; Freigabe Produktionsleitung laut Ingo erteilt, Schalter legt Ingo um)

- Admin Bereich > Passwörter, Schalter „Operations: Login nötig“ (Wunsch Fabio). Aus: Alle mit Windows-Anmeldung sehen und
  bearbeiten den Shopfloor ohne eigenes Login. Gespeichert in der Datenbank, übersteht Deploys. Standard unverändert: Login nötig.

## Shopfloor: Schnittstelle nimmt Fabios Push-Format an, 2026-10-15 (`f4e3f1b`, produktiv 11:06)

- Ein einzelnes Item ohne `items`-Hülle und `values` statt `fields` werden angenommen; zusätzliche Adresse
  `/BiDashboard/api/integration/push`. Fabio muss am Format seines Pakets nichts ändern, er braucht nur Server-Adresse und Token.

## Admin: Passwörter aller Bereiche an einer Stelle, 2026-10-15 (`e1afd49`, produktiv 10:56)

- Neue Seite Admin Bereich > Passwörter (`/admin/passwoerter`): Der entsperrte Admin setzt die Passwörter von Admin, Finance,
  HR KPI (auch `hradmin`), Operations und Logistik (Namen anzeigen) neu, ohne das alte zu kennen.
- Gespeichert wird nur der SHA-256-Hash, in der Tabelle `AccessPasswords` der Cockpit-DB. Ein Deploy überschreibt ihn nicht mehr;
  fehlt ein Eintrag, gilt weiter `appsettings.json`. Die bisherigen Felder „Passwort ändern“ bei Admin, Finance und HR schreiben
  ebenfalls in die DB.
- Nach einer Änderung müssen sich alle im Bereich neu anmelden, jetzt auch bei Operations. Einmalig nach dem Deploy gilt das für
  alle Operations-Anmeldungen, weil das Cookie ein neues Format hat.

## Shopfloor: Patches von Fabio, 2026-10-15 (`52b3b75`, produktiv 08:50)

- ZD05: Filter „Zu besprechen“ (neu, ohne Code oder Liefertermin überschritten), im Meeting automatisch aktiv.
- Verkauf: Kacheln mit Go-for-it-Ziel des Monats, Umsatz IST in % vom Ziel, Rest und Book to Bill.

## Anonyme Nutzungsmessung, 2026-10-14 (`4615268`, produktiv)

- Admin Bereich > Nutzung: Aufrufe und Nutzertage je Seite, Personen pro Tag, ohne Namen; Ingos eigene Aufrufe zählen nicht.
  Befund und Massnahmen in `docs/NUTZUNG_2026-10-14.md`.

## Startseite: Manometer abschaltbar, Titel „Trafag Dashboard“, 2026-10-14 (`cc9c6ab`, produktiv)

- Manometer auf der Startseite standardmässig aus; Schalter „Manometer anzeigen“ unter Admin Bereich > Aktive Logins,
  in der Datenbank gespeichert (übersteht Deploys).
- Kopfleiste „Trafag Dashboard“ statt „Trafag Cockpit“, Mitte der Standortgrafik „BI Dashboard“.
- Überschrift der Startseite „Trafag Global BI Dashboard“, Browser-Tab „Trafag Dashboard“ (`b5b4727`).

## Startseite: animierte Standortgrafik, 2026-10-14 (`ce5d27d`, produktiv)

- Schwarze, animierte Grafik statt des festen Bildes: neun Länder mit Flagge rund um „Trafag Global BI Cockpit“, Ring in der
  Farbe des letzten Exports (grün aktuell, gelb veraltet, rot Fehler), Datenpakete zur Mitte, Infobox beim Darüberfahren,
  Klick öffnet den Datenstatus.

## Operations KPI: Monatsabruf abgesichert, 2026-10-14 (`55b42d9`, produktiv)

- Abrufversuche bleiben über einen Neustart gespeichert, Warnhinweis wenn der Vormonat fehlt, Einträge in den Admin-Logs,
  neuer Versuch alle zwei Stunden (Wunsch Fabio).

## Kopfleiste nach Designprüfung, 2026-10-14 (`5b51011`)

- Dimmer zeigt jetzt eine helle Schiene, CPU-Anzeige flacher mit besser lesbarer Skala, Logo mit Abstand zum rechten Rand.

## Benutzerhandbuch: Kapitel Operations, 2026-10-14 (`f38020f`, produktiv 11:13)

- Neues Kapitel 10 Operations: KPI Übersicht, OTD / OTIF / OTP, Shopfloor (Ablauf, Listen, Abteilungen, ZD05, Forecast,
  PPA-Grafik) mit Glossar. Einleitung: Rolle Produktion/Operations im Wegweiser, Abschnitt Kopfleiste mit der CPU-Anzeige.
  Word-Datei neu erzeugt.

## Startseite: neuer Untertitel, 2026-10-14 (`d911cb1`, produktiv 11:04)

- „Die Trafag-Gruppe auf einen Blick: alle Standorte, alle Bereiche, täglich aktuell.“ statt „Automatisch angebundene
  Länderabfragen mit täglichem Refresh“; Bildbeschreibung entsprechend. In allen Sprachen inklusive Berndeutsch.

## Kopfleiste: CPU-Last als Fluoreszenz-Pegelanzeige, 2026-10-14 (`a735b41`, `5235e16`, produktiv 10:51 und 11:00)

- Ganz links in der rechten Kopfleiste, für alle: zwei Balken wie das VU-Meter eines Kassettendecks, S = Server gesamt,
  C = Cockpit-Prozess, Skala 0 bis 100 %, Beschriftung „CPU LOAD“, Spitze hält 2 Sekunden.
- Messung im Server alle 1,5 Sekunden (ein Takt für alle Besucher), unter 900 px Breite ausgeblendet.

## Operations: KPI Liefertreue OTD / OTIF / OTP und neues Menü, 2026-10-14 (`89e9c7c`, produktiv 10:04, live aus SAP seit 10:33, Abnahme bestanden)

- Menü Operations mit den Unterreitern KPI (Übersicht, OTD / OTIF / OTP) und Shopfloor.
- Liefertreue der Trafag AG nach Fabios Spezifikation: Kennzahlen, Diagramm, Monatstabelle, Machbarkeit des Wunschtermins,
  Abweichungsliste mit Excel-Export. Abnahmewerte Jan–Sep 2026 werden genau getroffen.
- Monatlicher Abruf aus SAP am 2. Kalendertag, Abruf auf Knopfdruck, Ausschlusstabelle auf der KPI-Seite pflegbar.

## Operations (Shopfloor): Feld-Mapping aus dem Excel, 2026-10-14 (`b613f7c`, produktiv 10:04)

- Abteilungen über den Disponenten: SEH = S*, TR5 = EK*, TX = EL*, DW = D*, CZ = 017 bis 028; 012 nicht mehr SEH.
- Aus dem MES kommen nur noch die FPY-Werte, die übrigen 129 Felder aus SAP.
- Seite „Schnittstelle SAP / MES“ zeigt je Feld SAP-Tabelle und -Feld (Vorschlag kursiv), Selektion, Status und Bemerkung.

## Menü-Hervorhebung, Deploy ohne Sicherung, Deploy-Monitor, 2026-10-13

- Menü: Trafag Projekte und Operations rot, Netzwerk rot mit zuckendem Kabel und Funke, Trafag Reddit orange,
  Benutzerhandbuch hellgrau; alle mit weisser bzw. dunkler fetter Schrift und Animation (`NavMenuNode.razor`,
  CSS `nav-hl*`). Produktiv bis `c8b91a7`.
- Deploy sichert die DB nur noch mit `--backup` (Entscheid Ingo); 96 alte Sicherungen (36,8 GB) gelöscht.
- Neues Monitor-Fenster `.tmp_tools/DeployMonitor/` mit Fortschrittsbalken; Runner beendet sich jetzt selbst.
  Ablauf in `docs/rag/DEPLOYMENT.md` und `router.md` Regel 11.

## Trafag Projekte: HERMES-Angaben, Portfolio und Demoprojekt, 2026-10-13 (`7e20f93`, produktiv 09:34; Fix `47990ad` produktiv 09:47)

- Je Projekt ein HERMES-Block in den Einstellungen: Aufwand mit Skala und Stufenvorschlag (unter 5 / 5 bis 40 /
  über 40 Personentage), Stufe, Ampel, Auftraggeber, Anwendervertreter, Stand in einem Satz, nächster Entscheid,
  „Erledigt, wenn“ und die Entscheidungspunkte ① Freigabe, ② Abnahme, ③ Abschluss; die Phase folgt daraus.
- Grafik: HERMES-Pfad mit animierter aktueller Phase, Projektkarten mit Ampel, Stufe, Phase und Rauten,
  neuer Reiter „Portfolio (HERMES)“ mit Ampelring, Stufenbalken, Phasenlandkarte und Tabelle für die
  Geschäftsleitung, alles per Klick filterbar.
- Demoprojekt `HDEMO` wird beim Start einmalig angelegt. Neue Tabelle `PmHermes`, 49 Texte in allen Sprachen,
  8 neue Tests (1173/1173). Doku `docs/TRAFAG_PROJEKTE_2026-10-09.md`, Handbuch Kapitel 8.

## SAP ZZPRDAT: Abnahmetest verschoben, 2026-10-12

- Abnahmetest mit Lucas, Marco, Adil und Ingo: war auf 12.10.2026 16:00 gelegt, am 12.10. um zwei Wochen verschoben (voraussichtlich Woche ab 26.10.2026, genauer Termin offen). Stand in T76 am 12.10. live geprüft: seit 07.09. unverändert, T76K912490/T76K912491 offen, BAdI Z_ZZPRDAT_UPDATE aktiv, Z_ZZPRDAT_SET als V2.
- Nachgeführt: `saptasks/ZZPRDAT_TRANSPORTPLAN.md`, `saptasks/zzprdat-kontext.md`, `docs/rag/PROJECT.md`,
  `docs/router/sap.md`, `projektmanagement/Wochen_Todo.tsv` Zeile 56 (fällig 2026-10-26).

## SAP T76: ZM_OFFENE_KDAUF (offene Kundenaufträge) schnell und geprüft, 2026-10-12 (in T76 eingesetzt, Transport T76K912728 bereit)

- Schnelle Kopie `ZM_OFFENE_KDAUF_OPT` liefert dieselbe Liste wie das Original, Feld für Feld gegen eine Referenzkopie
  verglichen: April bis Juni 2026 5 s statt 187 s, Variante `BI UPDATE` 24 s statt rund 20 Minuten.
- Wege: gesammeltes Vorablesen statt rund 20 Einzelzugriffen je Zeile, Parallelrechnung über den neuen RFC-Baustein
  `Z_KDAUF_PAR_RECHNEN` (Funktionsgruppe `ZKDAUF_PAR`), Kurzfassung der Basispreisliste, eigene Belegfluss-Lesung mit
  Rückfall auf den SAP-Baustein bei gleich datierten Rechnungen, IDs für `ZPOWERBI_KDAUF` in einem Sammelaufruf.
- Befunde: Das Original vergibt Fertigungsaufträge bei gleichem Lieferdatum zufällig (jetzt ETENR-Sortierung) und
  bricht als Hintergrundjob in der ALV-Liste ab (Feldkatalog mit nicht vorhandenem Feld `NETWR`). Korrekturschalter
  im Selektionsbild (Vorgabe „korrigiert“, „alte Logik“ für Vergleiche). Alles `$TMP`, Original unverändert.
  Doku `saptasks/zm_offene_kdauf/NOTES.md`.

## Trafag Projekte ersetzt die Poor Man's Project Management Suite, 2026-10-09 (produktiv 10:18)

- Projektmanagement wie Jira, Trello und Planner: Board mit Ziehen und Swimlanes, Backlog und Sprints mit Burndown,
  Liste mit CSV, Zeitachse nach Epic, Kalender, Diagramme, „Meine Aufgaben“ über alle Projekte, Aufgabenpanel mit
  Checkliste, Unteraufgaben, Kommentaren und Verlauf. Alle sehen, Mitglieder bearbeiten, Leitung verwaltet.
  Die alte Suite (nie genutzt) ist entfernt. Doku `docs/TRAFAG_PROJEKTE_2026-10-09.md`, Handbuch Kapitel 8 neu.

## Neuer Reiter Trafag Reddit, 2026-10-09 (produktiv 09:20)

- Internes Forum für alles, was in SharePoint keinen Platz hat: Communities, Abstimmen hoch/runter, Sortierung Angesagt/Neu/Top/Aktiv/
  Offene Fragen/Kontrovers, verschachtelte Kommentare, Fragen mit akzeptierter Antwort, Tags, Reputation mit Stufen, ähnliche Beiträge
  beim Tippen, Speichern. Autor ist der Windows-Name, für alle sichtbar (Entscheid Ingo). Doku `docs/TRAFAG_REDDIT_2026-10-09.md`,
  Handbuchkapitel 9.

## Einkauf: Produktgruppe im Spend-Aufriss über ZLO03, 2026-10-08 (produktiv 15:03)

- Gespräch Armin Punkt 4: Fast der ganze Einkauf stand unter „ohne Produktgruppe“, weil die Zuordnung nur den Cache der
  Stücklistenanalyse las (86 Zeilen, nur von Hand eingegebene Nummern).
- Neu fragt der Einkauf-Lauf je bestellter Komponente ZLO03 Bottom-Up ab (verkürzte Nummer und deren Disponent), höchstens
  8 Minuten je Lauf, jede Komponente höchstens einmal pro Woche. Keine SAP-Änderung.
- Erwartete Abdeckung nach dem Aufbau rund drei Viertel des Spends; der Rest hat in ZLO03 keine verkürzte Nummer.

## Einkauf: offener Mengenkontraktwert und LZ-/Sortiments-Code, 2026-10-08 (produktiv 15:03, SAP-Sets erst nach P76-Import)

- Gespräch Armin: offener Kontraktwert wie ME3L (Zielmenge minus EKAB-Abrufe × Nettopreis, inkl. abgelaufen) auf Kontrakte-Seite und
  Dashboard-Kachel; bisherige 5.2 Mio heissen jetzt „Abrufbestellungen zu Kontrakten“.
- Spend-Aufriss: Ebenen Lebenszyklus-Code und Sortiments-Code (bis Material) mit Grafik.
- SAP T76: EinkKontraktSet, EinkMatLzSet (Strukturen T76K912724, Klassen T76K912718). Bis zum P76-Import zeigt das Cockpit „noch nicht verfügbar“.

## Operations (Shopfloor): Cockpit-Design, Grafiken und Login, 2026-10-08, produktiv 09:23

- Reiter für alle sichtbar, Zugang mit eigenem Login (Benutzer `operations`, analog Finance/HR); Ingo ohne Login.

- Shopfloor sieht aus wie das Cockpit (CI-Orange, Open Sans, Karten, Kacheln) und folgt Hell/Dunkel und Dimmer des Cockpits.
- Grafiken: Fortschrittsring Pflichtpunkte, Rückstände je Abteilung, ZD05-Kennzahl gegen Ziel 1.20, Trendkacheln je Abteilung,
  Statusbalken in Listen, Auslastung je Abteilung im Forecast, Abteilungskacheln in der PPA-Grafik.

## Operations (Shopfloor), 2026-10-07, produktiv 2026-10-08 06:39

- Neuer Reiter `/operations`: PPA-Shopfloor im Cockpit (Oberfläche übernommen, Python-Backend in C#), Daten aus dem Excel-Stand.
- Zugriff vorerst nur Ingo, bis die Leitung Produktion/Operations freigibt.
- SAP: ShopZd05Set (ZD05) und ShopAufSet (Forecast) in T76 fertig (T76K912718); Abgleich im Cockpit gebaut, ausgeschaltet bis P76.

## SAP: ZM_OFFENE_FAUF Bedarfsverursacher sichtbar und mit Werk, 2026-10-08 (T76)

- Wunsch Ingo/Fabio: Die Spalten sind jetzt standardmässig eingeblendet; neues Feld „Werk“ (Werk des Bedarfsverursachers, z. B. 1200 bei einer BANF in 1200).
- Das globale Standardlayout `/ P00 TX` versteckte neue Spalten; in T76 um DE, NR-Disp, Daten zum Dispoelement und Werk ergänzt.
- Alles im selben Auftrag T76K912714. In P76 nach dem Import das Layout noch einmal anpassen (Layouts reisen nicht mit).

## ZLO03: Bedeutung von „Exklusiv“ geklärt, 2026-10-08

- Frage Ann-Katrin (B63491): „Exklusiv“ gilt relativ zur Selektion, ohne Status 99 und Nicht-FERT; Bottom-Up zählt alles. Notiz `zlo03/EXKLUSIV_LOGIK_2026-10-08.md`, Antwort als .eml.

## SAP: ZM_OFFENE_FAUF mit Bedarfsverursacher, 2026-10-07 (T76)

- Neun einblendbare Felder (Dispoelement, Nummer, Position, Einteilung, Daten zum Dispoelement, Dispobereich, Termin,
  Menge, Anzahl) über MD_PEGGING_NODIALOG; Transport T76K912714, in T76 getestet, P76 offen.

## Doku: eine Stufe und Lieferantenerkennung, 2026-10-07

- `FINANCE_FORMELN.md`: Vorrang, Regex intern/extern, Zuordnung TR AG/IT/IN, gemessene Lücken (TR IT mit Indien-Ware, TR DE ohne Lieferant).
- Neu `ISS-007.4`, drei Todos (Brücke Sachnummer Italien, Andreas informieren, Kostenwährung Budget oder Tageskurs).

## Budgetkurse und farbige Standardkosten-Spalten, 2026-10-07, produktiv 10:06

- Entscheid Ingo: Finance rechnet immer mit Budgetkursen. CHF in Cockpit, Pruefbuch, Nachweis und Sales_All jetzt mit dem
  Budgetkurs des Finance-Jahres (2025 = Budget 2025, EUR 2026 = 0.94 statt EZB 0.923); der CHF-Schalter folgt demselben Profil.
- Sales_All: alle Spalten mit Standardkosten-Bezug farbig (Kopf kraeftig orange, Spalte hell).

## Finance Details mit Standardkosten in CHF, 2026-10-06, produktiv 15:45

- Sales_All, Blatt Finance Details: Standardkosten je Stück in CHF, Kostenbasis und Marge in CHF aus der Gruppenmarge,
  Kostenquelle, Status und Kostenstand (historisch/aktuell). Wunsch Andreas aus dem Gespräch vom 06.10.

## Weltlage unter Finance Cockpit, 2026-10-06, produktiv 14:36

- Weltlage ist keine Hauptgruppe mehr, sondern liegt unter Finance Cockpit und verlangt das Finance-Passwort.
  Auftraggeber ist Ingo, Rückfragen landeten bisher bei Andreas.

## Benutzerhandbuch, 2026-10-06, produktiv 10:16

- Neuer Reiter **Benutzerhandbuch** auf oberster Ebene (`/handbuch`, nach Home): alle Module aus betriebswirtschaftlicher
  Sicht, je Seite Wozu, Lesart, Rechnung, Einsatz je Rolle und Fallstricke, Kapitelnavigation und Suche.
- Word-Datei im Trafag-CI (105 Seiten) zum Herunterladen; Quelle und Neuerzeugung in `docs/handbuch/README.md`.

## Pruefbefund-Fixes, 2026-10-05, produktiv 2026-10-06 10:16

- **Endlieferkennzeichen:** EKPO.Elikz steht im Cache als `True`/`False`, nicht als `X`. Der Ausschluss endgelieferter
  Positionen (M7) im Einkaufsdashboard und in der Supply Chain griff deshalb nie, und die Weltlage zaehlte nichts als offen
  (Logistik 0.0). Auf der Deploy-Sicherung sinkt der offene Bestellwert von 28.7 auf 12.8 Mio CHF (10'757 auf 1'548 Einteilungen).
- **Nur Bestellungen** jetzt auch in Einkauf Interaktiv und Weltlage; Weltlage offen nach offener Menge, ohne Konzernlieferanten,
  gleiches 12-Monats-Fenster fuer Verkauf und Einkauf.
- **Logistik live:** Timeout als Meldung statt Absturz, Fehler nur 1 Minute gemerkt, nur heute quittierte Positionen zaehlen,
  Ausblick mit heute und ueberfaellig, Lgnum 110, Ausbringung je letztem Vorgang. ABAP-Teil (Warenausgangszeit, offene TAs,
  STZHL, Kapazitaet verteilt und nach KEINH) liegt in `docs/abap/`, wirkt erst nach neuem T76-Transport.
- **Netzwerk/AD:** Windows 11 nach Build, Abbruch nur bei Wiederverbindung, eine Inaktiv-Definition, GPO-Vererbung, BitLocker,
  /24-Abdeckung, DNS-Alterung und weitere Einzelpunkte.
- **Verkauf/Controlling/Interaktiv/Weltlage:** Referenzmonat nach letztem Werktag, Galaxie ohne +100 % im ersten Quartal,
  Landschaft ohne NaN, Bruecke rundungsgenau, Prognoseband ab 0, UK/EL normiert, Ereignisse dedupliziert, Eurostat-Arrayform.

## Einkauf: Interaktiv, 2026-10-05, produktiv 15:11

- Menü Einkauf → Interaktiv mit denselben neun Ansichten auf den Bestellungen; Korrekturen Verkauf Interaktiv (Galaxie-Achse,
  Versandpauschalen).

## Verkauf: Unterreiter Interaktiv, 2026-10-05, produktiv 14:25

- Neun animierte Ansichten (Kunden-Galaxie, Rennen, Umsatzfluss, Sonnenstrahl, Was wäre wenn, Bestellrhythmus, Kalender,
  Artikel-Netzwerk, 3D-Landschaft) unter `/verkauf/interaktiv`, bestehende Unterreiter unverändert.

## Logistik live: Produktivität, Kapazität, Namen nach Anmeldung, 2026-10-05, produktiv 10:31 bis 12:40

- Umschalter Produktivität (Durchlaufzeit je Lieferung, Leistung je Stunde, anonym, produktiv 10:31) und Kapazität (tägliche
  Erfassung je Bereich nach Nevas Kennzahlen: Produktivität, Bedarf, Belastung, Lücke). SAP-Teil für Namen und Warenausgang in T76
  (`T76K912658`), Aktivierung offen. `docs/LOGISTIK_LIVE_2026-10-01.md`.

## Logistik live: 3D-Lagerplatzansicht, 2026-10-05, produktiv 09:21

- Umschalter Original | 3D-Lager: heute bewegte Plätze als Regale mit Paletten, Klick zeigt einen Platz mit seinen
  Bewegungen. `docs/LOGISTIK_LIVE_2026-10-01.md`.

## Reiter Weltlage, 2026-10-05, produktiv 09:21 (Währungskorrektur danach)

- Neuer Reiter (Radar je Abteilung mit drehendem Strahl, Länder, Rohstoffe und Währungen, Ereignisse) aus frei
  abrufbaren Quellen, verknüpft mit Verkauf, Einkauf und Kursen; Firewall-Zustand je Quelle. `docs/WELTLAGE_2026-10-05.md`.

## Finance Cockpit: Unterreiter Controlling, 2026-10-05, produktiv 08:18

- `cb08f18`: Umsatzbruecke gegen Vorjahr (Menge, Mix, Preis, neue/weggefallene Artikel, Waehrung, Uebrige) und
  Hochrechnung des laufenden Jahres; offene Fragen separat (Eigeninitiative, kein Auftraggeber).

## Reiter Verkauf: ohne Passwort, Vergleich gleicher Zeitraeume, Korrekturen, 2026-10-02, produktiv 14:05 bis 14:46

- `84fd1fd` ohne Finance-Passwort (Ingo), `fa83ad1` Vergleich 01–09.2026 gegen 01–09.2025 statt 12 gegen 12 Monate (Daten ab 01.2025),
  `a2bd605` Veraenderung je Gesellschaft, Verluste erst nach 6 Monaten, Preisstreuung ohne Platzhalter/Leistungen,
  alter Stand waehrend Neuladen, Achsanker; `81a4132` Prognoseachse.

## Reiter Verkauf, 2026-10-02, produktiv 13:24

- `e2fb8ce`: acht Unterreiter (Kunden, Rueckgang, neue und verlorene Kunden, Konzentration, Cross-Selling, Preisstreuung,
  Saison und Prognose, Weltkarte) auf den Finance-Daten, ohne Konzernkunden, CHF zum Belegdatum, hinter der
  Finance-Freischaltung. `docs/VERKAUF_2026-10-02.md`.

## Netzwerk: Migration, Bericht, Geraetelandkarte, Altlasten-Score, 2026-10-02, produktiv 12:00

- `c6b2d29`, `7d31df5`: Unterreiter Migration (Wellen, Zeitachse, Fortschritt) und Bericht (PDF ueber Browser-Druck),
  Geraetelandkarte in AD-Infrastruktur/Standorte, Altlasten-Score in Active Directory.

## Netzwerk: AD-Infrastruktur, Gruppenrichtlinien, DNS, Verlauf, 2026-10-02, produktiv 10:14

- `50b6c4a`: vier neue Unterreiter (Replikationsnetz, Sonnenstrahl der OUs, DNS-Alterung, Countdown und Verlauf),
  Drucker in Arbeitsplaetze, Excel-Export, taeglicher AD-Schnappschuss. Enthaelt `65e3a88` (Veeam gruppiert).

## Netzwerk AD: Korrekturen, 2026-10-02, produktiv 08:29 und 08:43

- `ce9e06a`: Klassen `dir-` statt `ad-` (Edge-Werbefilter blendete die inneren Reiter aus). `3e21220`: LAPS/BitLocker
  „nicht lesbar“ statt 0 %, RODC als DC, Turmhoehe nach Wurzel. `65e3a88` (Veeam gruppiert): Deploy offen, VPN weg.

## Netzwerk: Unterreiter Active Directory, 2026-10-02, produktiv 08:07

- `b57d20f`: 3D-OU-Stadt, Lebenszyklus, Aufraeumen, Sicherheit (LAPS, BitLocker, Delegation), Dienste je Server,
  Subnetze ohne AD-Standort, DNS und DCs; DCs als Pruefziele. Erster Deploy-Versuch scheiterte, Laptop nicht im Firmennetz; mit VPN um 08:07 deployed.

## Netzwerk: AD-Computer eingeschaltet, 2026-10-02, produktiv 07:34

- `48121df`: `NetworkProbe:AdEnabled = true` nach Erlaubnis der IT (laut Ingo). Nur Computerkonten, keine Personen.

## Reiter Netzwerk, 2026-10-02, produktiv 07:20

- `1635b46`: Netzkarte, Verfuegbarkeit mit SAP-Fruehwarnung, Netz oder Daten, Sicherheit, Arbeitsplaetze. Pruefung alle
  5 Minuten, nur feste Ziele, SAP nur ueber den oeffentlichen Ping. AD-Auswertung ausgeschaltet. `docs/NETZWERK_2026-10-02.md`.

## Logistik: Verwendung & Risiko, 2026-10-01, produktiv 16:05

- `1a754e0`: Seite `/logistik/verwendung-risiko`, mehrstufige Verwendung und vererbtes Lieferantenrisiko fuer die
  LZ-Code-Komponenten; kein Umsatz je Komponente, weil nur 6 % des CH-Umsatzes erreichbar sind.

## Logistik live, 2026-10-01, produktiv 15:01

- `2be8623`: Seite `/logistik/live` mit Kommissionierung und Produktion aus SAP, Abruf alle 30 s nur bei offener
  Seite, ohne Personendaten. SAP-Sets in `T76K912650`. `docs/LOGISTIK_LIVE_2026-10-01.md`.

## Cockpit: Berndeutsch, Dimmer, Doppel-LED, Logo auf Orange, 2026-10-01, produktiv 14:26

- `92e012e`, `102c20c`. `docs/rag/ARCHITECTURE.md`.

## Cockpit: neuer Skin Trafag CI, 2026-10-01, produktiv 14:14

- `7d1e5b5`: orange Kopfleiste, Logo im hellblauen Kasten mit „Cockpit“ daneben; das bisherige Rot ist ueber den
  Paletten-Knopf weiter waehlbar. `docs/rag/ARCHITECTURE.md`.

## Einkauf: alter Stand waehrend der Neuberechnung, Texte D1/D5, 2026-10-01, produktiv 11:21

- `4e2b55a`: Nach „Delta aktualisieren“ kommt die Seite sofort mit dem Stand vor dem Lauf und einem Hinweis und
  wechselt von selbst auf die neuen Zahlen (Rueckmeldung Armin, `PLATTFORM_TEMPO_2026-09-28.md` 10).
- `3407868`: D1 = FP_DICHTESEN, D5 = FP_DICHTESEN1/2 im Dashboard, solange SAP keinen Text hat.

## HR-Cockpit: Krankheit und Ferien je Fall aus SAP, 2026-10-01, produktiv 10:50

- `c749d32`: neues EntitySet `HrAbsenzSet` (T76, Transport `T76K912644`) liefert jede Abwesenheit aus
  `PA2001` mit Von/Bis. Das Cockpit schneidet Krankheitsfaelle auf den Zeitraum zu und zeigt Ferien,
  Kompensation und Militaer im Zeitraum. Ohne die neue Datei bleibt alles bei Rexx. `docs/HR_KPI.md` 8.7.

## HR-Cockpit: Unfalltage als laufender Monat, 2026-10-01, produktiv 09:44

- `931f151`: BU/NBU kommen nur fuer den laufenden Monat aus SAP; die Kacheln sagen das jetzt. Erster SAP-Abruf
  produktiv um 09:46 mit 1'168 Personen. `docs/HR_KPI.md` 8.6, Absenzen aus `PA2001` 8.7.

## HR-Cockpit: Kader mit 8,1 h je Arbeitstag, 2026-10-01, produktiv 09:12

- `c51445c`: Kader ist, wer in Rexx `Leitung j/n` = ja hat (Ingo, Wochen_Todo 66). Fuer sie gelten 8,1 h
  statt 8,0 h bei Krankheitstagen, Langzeitgrenze und FTE aus der Sollzeit. 800/800. `docs/HR_KPI.md` 8.5.

## Offene Punkte aus aelteren Eintraegen (Original im Archiv)

Status nach `router.md` Regel 2 immer aus dem Issue-Log, nicht von hier. Die Liste „Offene Punkte (nicht erledigt)“ vom August 2026 steht verbatim im Archiv `docs/archiv/LASTCHANGE_ARCHIV_2026-08-01_bis_2026-09-30.md`.

- Server/IIS (seit 2026-07-08, nur direkt am Server moeglich, WinRM gesperrt): App-Pool `startMode=AlwaysRunning` + `processModel.idleTimeout=00:00:00` setzen, damit der 12:00-Timer ohne vorherigen HTTP-Request laeuft. Bis dahin holt `CatchUpMissedRunAsync` verpasste Tageslaeufe beim naechsten Prozessstart nach.
- Betriebshinweis DE/Alphaplan (seit 2026-07-03): Der Alphaplan-Upload nach SharePoint muss VOR dem 12:00-Timer laufen, sonst verwendet der Tagesexport noch den vorherigen ZIP-Stand.

## Aeltere Eintraege / Historie

- Abschnitte 2026-07-30 bis 2026-09-30 (einschliesslich der alten Listen „Offene Punkte (nicht erledigt)“ und „Aktueller Kurzstand“): verbatim in `docs/archiv/LASTCHANGE_ARCHIV_2026-08-01_bis_2026-09-30.md` (verschoben am 2026-10-22).
- Aeltere Archive bis 2026-07-30 (`LASTCHANGE_ARCHIV_bis_2026-07-12.md`, `LASTCHANGE_ARCHIV_2026-07-13_bis_2026-07-30.md`, `HISTORY_CANONICAL.md.raw`, `original_history_raws.zip`) liegen seit `835b317` (2026-09-01) nur noch in Git: `git show 835b317^:TrafagSalesExporter/docs/raw_md_archive/<Datei>`. Entscheid Ingo 2026-10-22: nicht wiederherstellen, neue Archive unter `docs/archiv/`.

## Einstieg / Router

- Ladeweg (verbindlich, `router.md` und `CLAUDE.md`): `router.md` -> Unterrouter `docs/router/` -> Detaildatei. Diese Datei ist nur Aenderungsstand und wird bei Bedarf gelesen (`docs/rag/init.md`).
- Fuehrender Kurzkontext zum Projektstand: `docs/rag/PROJECT.md`.
