# RAG Einkauf

Stand: 2026-11-12 (Abschnitt „Lieferantenbewertung“ neu; Deploy 11:33 `e956183`: ungenutzte Abfrage Lieferant/Jahr entfernt; Doku-Pruefung: Abschnitt „Materialanteil je Produktgruppe“ neu, Ueberholt-Vermerke zu FIX 26, FIX 22, Kontrakten, Altposten, Logistik live und zur Zahl der Einkaufstexte); davor 2026-11-05 (**Spend-Aufriss aufgeraeumt 12:37 (`785325f`): Matrix Lieferant / Jahr und Block „Bestellwert nach Lieferant und Jahr“ (alter Reiter Spend) entfernt, alles doppelt zu den Einstiegen; `/einkauf/spend` zeigt den Aufriss; Stuecklisten-Netz eingeklappt**; **Nachtrag 05.11.: F2, F3, F4, F6, F8, F9 und Teile von Punkt 10 aus der Pruefung im Code behoben, Commit `6ab94a7` als `01ab4b9`, deployed 2026-11-05 11:48 (`a17d623`): Matrix Lieferant/Jahr gruppiert gross/klein getrennt wie der Aufriss und addiert statt zu ueberschreiben (Summe Matrix = Aufriss), Stueckpreis mit zwei Nachkommastellen und Hinweis „kein Vorjahr im Zeitraum“, 3D-Delta nennt die Basis, Lieferperformance zaehlt Faelle wie die Balken (offener Wert dort ohne OK-Faelle, nicht ZLO03-Umfang), Marke „Wartet auf SAP“ an den geladenen Stand gebunden (Ursache `d5bc321`), Materialanteil-Anteil mit Basis je Stufe und „< 0.1 %“. F5: Einkauf-Delta haengt nicht an „Einkaufsquelle fuer Import aktivieren“ (Haken soll aus bleiben), laeuft im Timer-Slot nach dem Verkaufsexport (produktiv laut Doku 12:00), Nachholen erst nach dem Slot; die Luecke 04.11. 14:58 bis 05.11. 10:28 ist erwartet; Texte „Nachts … sofern aktiv“ in `PurchasingDataSources.razor` und Handbuch ueberholt, nicht geaendert. Details `docs/EINKAUF_PRUEFUNG_2026-11-05.md`**); 2026-11-05 (**Funktionspruefung aller Einkauf-Ansichten im Chrome, Fehlerliste F1-F10 in `docs/EINKAUF_PRUEFUNG_2026-11-05.md`. ACHTUNG: „Materialanteil je Produktgruppe“ ist falsch (HW_ZUKAUFSORT 957 Mio. CHF bei 3,6 Mio. Umsatz), weil `Z_ZLO03_TURBO2` die Stuecklisten-Basismenge `STKO-BMENG` ignoriert (Wafer: 1 Rolle je Chip statt 1/4'750), Befund `zlo03/BEFUND_EINHEIT_WAFER_2026-11-05.md`, FIX 26 freigegeben, noch nicht umgesetzt (Wochen_Todo 152) *(ueberholt, Vermerk vom 2026-11-12: FIX 26 und 27 (`T76K912839`) sind seit dem 2026-11-09 in P76, das Einkauf-Delta vom 09.11. zeigt HW_ZUKAUFSORT mit 1.27 Mio. Materialanteil auf 3.67 Mio. Umsatz statt 957 Mio.; siehe Abschnitt „Materialanteil je Produktgruppe“)*; weitere Fehler: Matrix Lieferant/Jahr verliert Spend bei Namen nur in anderer Gross-/Kleinschreibung (-17'885 bei 2020 bis heute), Preisentwicklung „CHF 1“, Nachtlauf seit 04.11. 14:58 offen**); 2026-11-02 (**Rechenweg des Anfragenden fuer Phase 2 Materialanteil erhalten (ZC23 -> ZLO03 Top-Down -> Disponenten 001-006 je Materialgruppe), `saptasks/zc23_materialanteil/NOTES.md` Nachtrag 2026-11-02; Entwurf Lieferantenbewertung V6 erhalten, `docs/EINKAUF_SITZUNG_2026-10-22.md` Abschnitt 5**; 2026-10-28: **neu Einstieg „Produktgruppe nach Verwendung : Warengruppe“ (Einkaufsvolumen, gewichtet nach verwendenden Nummern), Wunsch Sitzung 28.10., Commit `8653f6b`, Phase 2 Materialanteil offen, `saptasks/zc23_materialanteil/NOTES.md`**; **ganzer Inhalt des alten Reiters Spend (Kacheln, Spend pro Lieferant, Zusatzdiagramme, Matrix Lieferant / Jahr) jetzt unter dem Spend-Aufriss, Reiter Spend ausgeblendet (Route bleibt), Commits `45b2158`, `c501053`, deployed**; **Bild Warenkorb mit Sensoren im Kopf, Commit `8a568e5`**; **Ladestatus-Meldung im Kopf auf drei Zeilen gekuerzt, voller Text ueber den Knopf „Ladeprotokoll“ im Popup, Commit `ae67f7e`**; 2026-10-27: **Produktgruppe zeigt nur den SAP-Text, z. B. „FP_DICHTEWÄCH“ statt „D2 - FP_DICHTEWÄCH“, Commit `0917d9e`**; 2026-10-26: **Schalter „Nur Einkaufsteile (Disponent 001 bis 005)“ fuer alle Aufriss-Einstiege, Vorgabe an, Commit `c3147e1`**; **neu zwei Einstiege „Disponent“ (DISPOKENNZ wie ZC23 = eigener MARC-DISPO) und „Disponent Verwendung (ZLO03)“ (1/n), Commit `bddf9f9`**; **Produktgruppe aus dem eigenen Disponenten MARC-DISPO wie ZM_OFFENE_KDAUF, genau eine Gruppe, keine 1/n-Verteilung**; 2026-10-23: **Produktgruppen-Sichten jetzt ohne Deckelung wie alle Einstiege, keine „uebrige (n)“-Zeile mehr**; **neu Spend-Aufriss-Einstieg „Produktgruppe : Warengruppe“ (Produktgruppe -> Warengruppe -> Material) neben „Produktgruppe : Lieferant“, gleiche 1/n-Verteilung, Wunsch Einkauf fuer die Budgeterstellung; Nachtrag 2026-10-23**; 2026-10-22: **Feedback Erich/Sandro/Marco umgesetzt: Kontraktpositionen „vermutlich ersetzt“ (z. B. 46009671 Pos. 10) nicht im offenen Wert, Filter abgelaufene Kontrakte, Altposten = Textpositionen mit Termin > 1 Jahr vorbei nicht mehr offen/ueberfaellig (CHF 3,1 Mio.), Hinweis „Zeitraum nicht angewendet“, Lagerwert je Disponent, Top-Spend-Zeile weg, Bestellbedarf & Deckung / Materialabhaengigkeit / Interaktiv ausgeblendet, „Gehirn“ eingeklappt; `docs/PURCHASING_DASHBOARD_2026-06-05.md` Nachtrag 2026-10-22**; 2026-10-16: **neu Stuecklisten-Netz „Gehirn“ zuoberst im Spend-Aufriss, WebGL-Prototyp, Farbe LZ-Code/Lieferantenrisiko/Stufe, Kreuzpunkte, produktiv 2026-10-16 10:48 `d6ca646`, Teilmenge mit LZ-Code, Stuecklistenstand 02.09., `docs/LOGISTIK_STUECKLISTE_VERWENDUNG_RISIKO_2026-10-01.md` Nachtrag**; 2026-10-13: **neu Standardzeitraum `/einkauf` = Januar des laufenden Jahres bis heute statt 2020, Knopf „2020 bis heute“ fuer die Mehrjahressicht, Jahresdeckel 2026 entfernt, produktiv 2026-10-13 `4b9ebb1`**; 2026-10-08: **neu Produktgruppe im Spend-Aufriss ueber ZLO03 je Komponente aus dem Einkauf-Lauf, produktiv 2026-10-08 15:03**, Abschnitt „Produktgruppe 2026-10-08“; **neu Offener Mengenkontraktwert wie ME3L und LZ-/Sortiments-Einstiege im Spend-Aufriss, Cockpit produktiv 2026-10-08 15:03, SAP-Sets erst nach P76-Import**, Abschnitt „Kontrakte und LZ-Dimensionen 2026-10-08“; 2026-10-01: **neu Verwendung & Risiko** `/logistik/verwendung-risiko`, nur LZ-Code-Komponenten, `docs/LOGISTIK_STUECKLISTE_VERWENDUNG_RISIKO_2026-10-01.md`; **neu Logistik live** `/logistik/live`: Kommissionierung und Produktion aus SAP alle 30 s, nur bei offener Seite, ohne Personendaten *(ueberholt, Vermerk vom 2026-11-12: seit dem 2026-10-05 sind die Namen der TA-Benutzer nach Anmeldung sichtbar, ohne Anmeldung bleibt die Seite anonym; `docs/LOGISTIK_LIVE_2026-10-01.md` Abschnitt „Produktivität und Kapazität“)*, Sets `LogTaSet`/`LogLiefSet`/`LogRueckSet` in P76, `docs/LOGISTIK_LIVE_2026-10-01.md`; nach dem Auffrischen sofort alter Stand mit Hinweis `4e2b55a`; Texte D1/D5 `3407868`; vorher 2026-09-30: Rueckmeldung Armin zum Tempo, `PLATTFORM_TEMPO_2026-09-28.md` 10: viel schneller, beim Auffrischen noch langsam, Vorschlag offen; ZRL2-Konditionsart AT in P76, `docs/SAP_ZRL2_MENGENRABATT_AT_2026-09-30.md`; Codex-Reparaturen E1/E2/C1/C2 produktiv seit 10:07; Lagerwert-Verlauf und Ladezeit vom 2026-09-28; uebriger Kurzstand vom 2026-09-03)

Live-Abgleich vom Juli fuer den Einkauf-Delta-Status:
`docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.
Das ist eine **historische Messreferenz vom Juli 2026**, kein Vorrang fuer heute: sie
kennt zum Beispiel nur Schweizer Konzernkosten. Vorrang hat nach `router.md` Regel 1 immer der
juengste direkt gepruefte Beleg zum jeweiligen Thema. Der Einkauf-Lauf ist seit dem 2026-09-28 wieder
nachgewiesen (`docs/PLATTFORM_TEMPO_2026-09-28.md`, `ISS-017`).

Kurzdatei fuer Spend, offene Bestellungen, Kontrakte und Lieferanten. Historie
und technische Details: `docs/PURCHASING_DASHBOARD_2026-06-05.md`.

## Lieferantenbewertung und -entwicklung (Stand 2026-11-12, noch nichts gebaut)

Wochen_Todo 122. Fachliche Grundlage ist die Demo V6 (`docs/einkauf_lieferantenbewertung/`), ein Word-Dokument kommt nicht
mehr (Armin 12.11.). Zwei Pakete: **Paket 1** Lieferantenperformance aus SAP mit Q2-Meldungen und A/B/C je Lieferant
(A 70 %, B weitere 20 %, C Rest, nach Einkaufsvolumen aus dem Cache; nicht die Material-ABC `MaraAbc`); **Paket 2**
Bewertung und Entwicklung mit Zertifikaten, Audits, Business Reviews, Massnahmen und Dokumenten, **gepflegt im Dashboard**
(von Oscar am 12.11. freigegeben, geht ueber „Dashboard zeigt nur an“ vom 03.11. hinaus). SAP-Quellen in T76 gelesen
(`saptasks/einkauf_lieferantenbewertung/SAP_QUELLEN_2026-11-12.md`): `Slfdt` steckt schon im `eketSet`, das `EKBESet` ist
fuer die Liefertreue so nicht brauchbar, `QmMeldungSet` liefert nur Q1. Der SAP-Score (Fiori) haengt an Lucas/NTT, Info am
01.12. Offen: Zeitraum der A/B/C-Klasse, SLFDT oder EINDT, Rhythmus der Themenfelder, P76 lesen. Einzelheiten
`docs/EINKAUF_SITZUNG_2026-10-22.md` Abschnitt 5 bis 6.3.

## Materialanteil je Produktgruppe (Stand 2026-11-10 laut Wochen_Todo 123 und 152, Abschnitt angelegt am 2026-11-12)

Eigene aufklappbare Tabelle (`PurchasingMaterialShare.razor`), die ein rot markierter Knopf im Spend-Aufriss oeffnet;
produktiv seit dem 2026-11-02 11:18 (`f36e1e7`). Die Basis sind bestaetigte Kundenauftraege mal Stueckliste, **nicht** das
Einkaufsvolumen. Rechnung je Produktgruppe: bestaetigte ZC23-Menge der VerkNr (Termin bestaetigt im Zeitraum, Absagegrund
leer) mal ZLO03-Top-Down-Menge des Einkaufsteils je 1 VerkNr mal Stueckkosten, nur Einkaufsteile mit Disponent 001 bis 006;
Stufen Produktgruppe -> Materialgruppe -> Einkaufsteil. Quellen sind `Zc23Set` und `ZloTopDownSet` (ZLO03 FIX 25,
`T76K912785`, laut Ingo seit dem 02.11. in P76). Der Einkauf-Lauf laedt sie in die Tabellen `PurchasingMaterialShareSales`
und `PurchasingMaterialShareBom` (Monatsfenster Januar des Vorjahres bis Dezember des laufenden Jahres,
`Services/PurchasingMaterialShareLoader.cs`). Seit ZLO03 FIX 26 (`T76K912839`, in P76 seit dem 09.11.) wird die Menge durch
die Basismenge der Stueckliste geteilt: das Einkauf-Delta vom 09.11. zeigt HW_ZUKAUFSORT mit 1.27 Mio. Materialanteil auf
3.67 Mio. Umsatz (34.7 %) statt 957 Mio. Offen: Abgleich einer Produktgruppe mit Armin und Abnahme durch den Einkauf,
FP_THERMOST. (Selbststuecklisten H20201/H17500, eigener Fix, Entscheid Ingo), Verdacht FP_FROSTI, G4 Mehrstufigkeit,
Laufzeit des Laders in P76 (Wochen_Todo 123, 152, 154). Details:
`saptasks/zc23_materialanteil/BAUPLAN_PHASE2_MATERIALANTEIL_2026-11-02.md`, `zlo03/FIX26_BASISMENGE_2026-11-05.md`,
`docs/EINKAUF_PRUEFUNG_2026-11-05.md` (F1, F9).

## Handbestellwesen 2026-10-27

Eigenes Modul, eigener Reiter (seit 27.10. sichtbar), ersetzt die PHP-Anwendung (abgeschaltet wegen PHP-Luecke). Zwei
Masken auf derselben Datenbank `hbdaten\handbestellwesen.db`: `/handbestellwesen` (klassisch, mit Link „Zur neuen Version“)
und `/bestellwesen` (neu). Freigabe wie PHP: jede aktive Person mit genuegend Limit; strenge Regel per
`Handbestellwesen:FreigabeNurGleicheKostenstelle`. Stammdaten loeschen (nur ohne Bestellungen, Mitarbeitende sonst
deaktiviert) und Mitarbeitende aus dem AD uebernehmen, Konzept 6.9. Neue Mitarbeitende nur noch aus dem AD (Name, Mail,
Konto dort gepflegt), Altbestand aus PHP voll pflegbar, Konzept 6.10. Offen: Zugriff/Rollen, Grundtests, Limit und Kostenstelle der
Mitarbeitenden, Bereinigung, Rechnungsbetrag/Mail. Details `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` 6.8.

## Schalter Nur Einkaufsteile 2026-10-26

Spend-Aufriss: Schalter „Nur Einkaufsteile (Disponent 001 bis 006)“ (006 seit 2026-11-02, vorher 001 bis 005), Vorgabe an, gilt fuer alle Einstiege (Filter
`MarcDispo IN PurchasingPlanners`). Ausgeschaltet ganzer Einkauf. Diagramme darunter immer ganzer Einkauf. Details:
`docs/PURCHASING_DASHBOARD_2026-06-05.md`, Nachtrag 2026-10-26 (3).

## Disponenten-Sichten 2026-10-26

Spend-Aufriss hat zwei weitere Einstiege (Wunsch Ingo „mach beide“): „Disponent“ = DISPOKENNZ wie ZC23 (Spalte `DISPO`,
eigener MARC-DISPO Werk 1100, Werte 001, 002 ...), und „Disponent Verwendung (ZLO03)“ = Disponent der verkuerzten
Nummer, 1/n verteilt. Beide summenerhaltend. Einkaufsteile tragen meist 001 bis 006, in ZDISPO alle Gruppe H. Der
Bottom-Up-Abruf `PurchasingComponentDispoLoader` bleibt noetig. Details: `docs/PURCHASING_DASHBOARD_2026-06-05.md`,
Nachtrag 2026-10-26 (2).

## Produktgruppe 2026-10-26: eigener Disponent wie ZM_OFFENE_KDAUF

Entscheid Ingo 2026-10-26: Produktgruppe aus `MARC-DISPO` (Werk 1100) des Einkaufsmaterials, wie der Report
ZM_OFFENE_KDAUF, genau eine Gruppe je Material, keine Vererbung ueber die Verwendung und keine 1/n-Verteilung mehr.
Mehrere gleichwertige SAP-Regeln (DS1/DS2 -> D5/DS, 016 -> H/H2): kleinste Gruppennummer gewinnt, Kachel „Mehrdeutige
SAP-Regel“. Spalte `PurchasingEkpoCache.MarcDispo` fuellt erst der naechste Einkauf-Lauf. ZLO03 selbst kennt ZDISPO
nicht. Details: `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`, Nachtrag 2026-10-26.
Seit 2026-10-29 in P76 neue Gruppe `BM` = `BG_MECHANIK` fuer Disponent `026` (Wunsch Armin, Nachtrag 2026-10-29).
ZLO03 zeigt im Top-Down ab FIX 22 (`T76K912777`, T76, Import offen; *ueberholt, Vermerk vom 2026-11-12: laut Ingo am 2026-10-29 nach P76 importiert, Wochen_Todo 124*) Disponent und Warengruppe je Komponente.

## Produktgruppe 2026-10-08 (produktiv 15:03) *(ueberholt am 2026-10-26, siehe oben)*

Punkt 4 Gespraech Armin. Details: `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`, Nachtrag 2026-10-08.

- Bis hierhin las die Produktgruppe nur den Cache der Stuecklistenanalyse (86 Zeilen) und fast alles stand unter
  „ohne Produktgruppe“.
- Neu fragt der Einkauf-Lauf je bestellter Komponente `ZSTR_LZCODE_USAGESet` Bottom-Up ab (verkuerzte Nummer und
  deren Disponent), 8 Minuten je Lauf, jede Komponente hoechstens woechentlich; Cache `PurchasingComponentDispoCache`.
  Keine SAP-Aenderung. Erwartet rund 72 bis 77 % des Spends, der Rest hat in ZLO03 keine verkuerzte Nummer.
- Offen: Umlaut-Ersatzzeichen in den ZDISPO_SPART-Texten aus P76, doppelte Regeln DS1/DS2/016 (50/50; *seit 2026-10-26 keine Verteilung mehr, kleinste Gruppennummer; Bereinigung in SAP weiter offen*).

## Kontrakte und LZ-Dimensionen 2026-10-08 (Cockpit produktiv 15:03, SAP-Sets nach P76-Import) *(SAP-Sets seit 2026-10-09 in P76, siehe Stand-Zeile)*

*Stand 2026-10-09:* T76K912724 und T76K912718 in P76, Caches geleert, Delta 12:27 erfolgreich: 1'262 Kontraktpositionen, 35'042 LZ-Codes, offener Kontraktwert **CHF 18'670'239** inkl. abgelaufener Kontrakte. Armins ME3L-Erwartung war 16–17 Mio.; Abweichung mit Armin klaeren (abgelaufene Kontrakte, nur Belegart MK?). Produktgruppen-Verwendung im selben Lauf: 4'469 von 6'075 Komponenten mit verkuerzter Nummer, 1'280 folgen.

Wunsch Armin 2026-10-08. Details: `docs/PURCHASING_DASHBOARD_2026-06-05.md`, Nachtrag 2026-10-08.

- **Zwei Kontrakt-Kennzahlen, nicht verwechseln:**
  - **Offener Mengenkontraktwert (wie ME3L)** = Kachel „Offener Kontraktwert“ auf `/einkauf` und Block auf
    `/einkauf/kontrakte`. Je Kontraktposition `max(KTMNG - Summe EKAB.MENGE, 0) x NETPR / PEINH`
    (PEINH 0/leer = 1), in CHF mit `EKKO.WAERS/WKURS` wie das uebrige Dashboard, **inklusive
    abgelaufener Kontrakte** (`KDATE` vor heute), die getrennt ausgewiesen werden. Nur Belegart `MK`,
    Buchungskreis `1100`, ohne Loeschkennzeichen. Stand heute, ohne Zeitraumfilter. Erwartet rund 16 bis
    17 Mio CHF (15,5 Mio EUR + 2,7 Mio andere Waehrung), Abgleich gegen ME3L nach dem ersten Lauf offen.
    *(Vermerk vom 2026-11-12: Stand 05.11. produktiv CHF 18,23 Mio., darin abgelaufen 45 Positionen mit CHF 0,13 Mio.
    Seit dem 2026-10-22 zaehlen „vermutlich ersetzte“ Positionen nicht zum offenen Wert: nie abgerufen, und eine andere
    Position desselben Kontrakts mit gleichem Text oder gleicher Nummer bis auf einen vorangestellten Buchstaben hat ihre
    Zielmenge erreicht (`PurchasingContractCalculator.MarkSuperseded`); am 05.11. waren das 2 Positionen mit CHF 0,52 Mio.
    Der Abgleich der Positionsliste mit Armin gegen ME3L ist weiter offen, Wochen_Todo 88.)*
  - **Abrufbestellungen zu Kontrakten** (frueher „Offener Wert der Kontraktabrufe“, rund 5,2 Mio CHF) =
    offener Bestellwert nur fuer Bestellungen mit `EKKO.KONNR`. Das ist **nicht** der Kontraktwert.
- **Quelle:** SAP-Sets `EinkKontraktSet` und `EinkMatLzSet` (Service `ZPOWERBI_EINKAUF_SRV`, T76, bis zum
  Transport 404 in P76; *ueberholt: beide Sets sind seit dem 2026-10-09 in P76, siehe Stand-Zeile oben*). Caches `PurchasingContractCache`, `PurchasingMaterialLzCache`; Full Load und Delta
  lesen beide vollstaendig neu. **Ein 404 bricht den Lauf nicht ab:** Warnung im Log, alter Cache bleibt,
  Seite zeigt „Kontraktdaten noch nicht verfuegbar“ bzw. „–“ statt 0.
- **Spend-Aufriss:** neue Einstiege „Lebenszyklus-Code“ (`MARA-ZZLZCOD`) und „Sortiments-Code“
  (`MARA-ZZLZCODSORT`), je Code > Lieferant > Material, Materialien ohne Code unter „ohne Code“, plus zwei
  Balkendiagramme. Quelle fuer beide Codes ist `EinkMatLzSet` (nicht `MARA001Set`).
- Fallstrick: SAP-Zahlen mit nachgestelltem Minus („1.075-“) werden beim Laden normalisiert; Datum
  `00000000` heisst „kein Laufzeitende“ und ist nie abgelaufen.

## Kurzstand

- **2026-09-29: Review-Reparaturen von Codex, produktiv seit 10:07** (`eafd1c5`, `c1fdfa2`).
  Der Jahres-Spend kommt aus einer eigenen, ungekappten Jahresaggregation statt aus der Summe
  der Top 10; das Diagramm bleibt Top 10 (E1). Die Top-Warengruppe nutzt wie Diagramm und Matrix *(Matrix seit 05.11. entfernt, ihre Abfrage seit 2026-11-10, `19ca282`)*
  zuerst `MARA.MATKL` (E2). Bei unvollstaendigem Cache entstehen keine SAP-Live-Stichproben-KPIs
  mehr (C1). Fehlende Wechselkurse werden gezaehlt und ausdruecklich gewarnt; der 1:1-Rueckfall
  bleibt bis zu einem Fachentscheid, betroffene Werte gelten nicht als belastbar (C2).
  Bericht: `docs/HR_EINKAUF_REVIEW_2026-09-29.md`.
- **Geltende Betriebsregel seit 2026-09-28:** Der Snapshot gilt 60 Minuten
  (`Services/PurchasingDashboardSnapshotCache.cs`, `Lifetime`), ein abgelaufener Stand wird
  sofort geliefert und im Hintergrund neu gerechnet, beim Start wird vorgewaermt
  (`docs/PLATTFORM_TEMPO_2026-09-28.md`). Der folgende Punkt ist der Verlauf.
- Performancefix produktiv seit 2026-09-03: Alle 15 Routen des Einkaufsdashboards teilen
  pro Filter einen damals 15 Minuten gueltigen Snapshot (maximal 32 Filter, Single-Flight).
  Erfolgreiche Full- und Delta-Ladeprozesse invalidieren den Cache. Vorher brauchten selbst
  warme direkte Aufrufe von `/einkauf` und `/einkauf/aufriss` jeweils rund `9-11 s`.
  Nach der einmaligen Kaltberechnung (`83.17 s` direkt nach Neustart) lieferten alle 14
  weiteren Einkaufsrouten HTTPS `200` in `0.05-0.14 s`. Der SQLite-3.53-Planer benoetigt
  fuer die Artikelpreistrend-Abfrage eine materialisierte CTE; dies ist mit Tests und einer
  Produktionskopie abgesichert. Commits `756e931`, `2444731`; `674/674` Tests gruen.

- Direkte Produktgruppenquelle aus SAP am 2026-08-11 produktiv deployed und am
  2026-08-12 nach SAP-Aktivierung live abgeschlossen. Full Load und Delta lesen
  `ZDISPO_GRPSet` + `ZDISPO_SPARTSet`, laden atomar und verwenden keinen
  Excel-/manuellen Fallback. Live: `$metadata` HTTP 200 mit `62` Sets,
  `ZDISPO_GRPSet` `45` Zeilen, `ZDISPO_SPARTSet` `22` Zeilen. Der produktive
  Delta endete um `10:03:42 MESZ` mit `Success`; der Cache enthaelt danach
  `45` SAP-OData-Regeln und `0` Nicht-SAP-/Excel-Regeln. Offen *(ueberholt am 2026-10-01, siehe unten)*: Texte fuer `D1`
  und `D5` in `ZDISPO_SPART` pflegen und SEGW-Key von `ZDISPO_GRP` auf
  `DISPO_KZ + DISPO` korrigieren. *Stand 2026-10-01:* D1/D5 haben im Dashboard Ersatztexte (`3407868`, produktiv 11:21): D1 = `FP_DICHTESEN` (DISPO_KZ DS), D5 = `FP_DICHTESEN1/2` (DS1, DS2), Vorgabe Ingo, SAP vorerst nicht pflegen; ein spaeter in SAP gepflegter Text gewinnt. `ZDISPO_SPART` hat Auslieferungsklasse `A`, Pflege also direkt im System (SM30), kein Transport. SEGW-Key: Empfehlung, nicht umzusetzen (kein Nutzen fuer das Cockpit, Neugenerierung des Service mit Einkauf, Journal und HR noetig); Entscheid Ingo offen. Details:
  `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`.

- Produktiv deployed und verifiziert am 2026-08-07 08:40 MESZ (Commit `eef6374`,
  `449/449` Tests): SECHS Indikatoren zeigten eine erfundene oder falsch
  beschriftete Zahl und sind behoben. `Lieferanten` hat keine Bewertungsquelle —
  `Performance Score` und `Qualitaet` stehen jetzt auf `-` statt auf einer
  Konstante aus zwoelf Simulationszeilen bzw. dem Literal `"offen"`;
  `Preisindikator` zeigt den mengengewichteten Ø-Stueckpreis des juengsten
  Jahres mit Vorjahresveraenderung statt des Gesamt-Spends. Die Idee
  `Lieferantenrisiko` steht auf `Konzept` statt `berechenbar`. Im Reiter
  `Kontrakte` verwenden Kachel, Diagramm und `Top Verpflichtung` jetzt DIESELBE
  Grundmenge (`EKKO.Konnr`), der Rueckfall auf alle offenen Bestellungen bzw.
  auf Simulationsbalken ist weg, und `Faelligkeit` heisst `Letztes
  Bestelldatum` (der Wert ist `MAX(EKKO.Bedat)`). Auf allen fuenf
  Supply-Chain-Reitern zaehlen die Prioritaetsbalken vor dem Schalter
  `Nur Handlungsbedarf`; vorher stand `Ohne akuten Hinweis` im Standardaufruf
  garantiert auf `0`. `Fehlwert CHF` weist fehlende Stueckkosten aus, statt sie
  als bewertete `0` in die Summe laufen zu lassen. Details:
  `docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md`.
- Produktiv deployed und verifiziert am 2026-08-06 15:11 MESZ (Commit `01af1b8`,
  `446/446` Tests): fuenf additive
  Einkauf-/Logistik-Reiter fuer Materialdisposition, Bestellbedarf/Deckung,
  Materialabhaengigkeit, Dispositionspruefung und Lieferperformance-Datenstatus.
  Bestehende Reiter und Berechnungen bleiben unveraendert. Echte OTIF wird
  mangels Ist-Wareneingangsdatum bewusst nicht gerechnet. Details:
  `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md`.
- ZDISPO-Ergaenzung produktiv deployt und verifiziert am 2026-08-06 13:57
  MESZ, Commit `0a8a4c9`, `435/435` Tests. Startseite und direkter
  Spend-Aufriss liefern HTTPS `200`. Produktiv stehen `45` ZDISPO-Zuordnungen
  aus `42` Mustern; die bestehende manuelle ZC23-Tabelle blieb unveraendert bei
  `0` Eintraegen. `105` ZLO03-Zeilen tragen einen Disponenten.
- Seit 2026-08-06 bietet der Spend-Aufriss historisch die Perspektive
  `Produktgruppe -> Lieferant -> Material`. Die Zuordnung folgt
  `EKPO-MATNR -> ZLO03 -> VknrDispo -> Produktname`. Der noch produktive Altstand
  wurde am 2026-08-11 ersetzt; produktiv akzeptiert die Strecke nur noch SAP OData.
  Mehrfach verwendete Komponenten
  werden summenerhaltend gleichmaessig `1/n` auf unterschiedliche
  Produktgruppen verteilt; unzugeordneter Spend bleibt sichtbar. *(Ueberholt am 2026-10-26: Produktgruppe aus dem
  eigenen `MARC-DISPO`, genau eine Gruppe, keine 1/n-Verteilung mehr, siehe Abschnitt „Produktgruppe 2026-10-26“ oben, `0dc3c68`.)* Die SAP-Platzhaltergruppe `NA` („N/A“)
  zaehlt seit 2026-10-15 neben einer echten Gruppe nicht mit.
  Details: `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.
- ABC und XYZ werden seit 2026-08-06 zusaetzlich als gemeinsame
  Massnahmenmatrix mit Spend, Material-/Lieferantenzahl und konkretem
  Pruefauftrag je Klasse ausgewertet. Es erfolgt keine automatische
  Dispositionsaenderung.
- Seit 2026-08-01 rendern Einkaufsdashboard und Einkaufs-Datenquellen nach
  einem Sprachwechsel sofort neu. Die Sprache ist pro Benutzersitzung getrennt.
  Ein expliziter Katalog deckt 77 dynamische Einkaufstexte *(Zahl ueberholt, Vermerk vom 2026-11-12: die Detaildatei
  nennt seit dem 2026-08-06 85 Textpaare; der heutige Stand im Code ist nicht nachgezaehlt)* in `es`, `it`, `hi`,
  `sq`, `tr` und `tlh` ab. Details und Testgrenzen:
  `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md`.
- Delta-Fix `66a34da` ist deployed und nicht mehr an `Sites.IsActive`
  gebunden. Beim Live-Check 2026-07-31 10:21 MESZ lag noch kein produktiver
  Delta-Lauf nach dem Deploy vor; korrekte Aussage bis zum Nachweis:
  **Fix deployed, Live-Wirkung offen.** *(Ueberholt: der Einkauf-Lauf ist seit dem 2026-09-28 wieder nachgewiesen,
  siehe Kopf dieser Datei, `ISS-017`.)*
- Letzter verifizierter Full Load vom 2026-07-24: `SupplierCountry` 100 %,
  `MaraAbc` 78 %, `MaraXyz` 65 % und `MaraMatkl` 80,7 % gefuellt. Der
  Spend-Aufriss zeigt damit echte Region-/ABC-/XYZ-Daten.
- *Ueberholt am 2026-11-10: Die Spend-Matrix gibt es seit dem 05.11. nicht mehr auf der Seite, ihre Abfrage ist mit `19ca282` entfernt; Lieferant -> Warengruppe -> Material zeigt der Spend-Aufriss (Einstieg Lieferant).* Die Spend-Matrix bietet Lieferant -> Warengruppe -> Material. Warengruppe
  kommt aus `MARA-MATKL`, mit Fallback auf die Beleg-Warengruppe. T023T-Texte
  werden ueber `PurchasingMaterialGroupTextCatalog` angezeigt; unbekannte
  Codes bleiben sichtbar.
- Finaler Praesentationsstand 2026-07-31: Tabellenkopf, Lieferanten,
  Warengruppen und Materialien sind fett (`700`); Lieferanten/Warengruppen
  `1.05rem`, Materialien `1rem`; dunkler Primaertext und staerkere
  Ebenenhintergruende. Code-Commits `4a3271b`, `f740eb9`, `4498bd4`.
- Nachtlauf Einkauf ist Delta; Full Load bleibt manuell. *(Praezisiert 2026-11-05: kein Nachtlauf, sondern der taegliche Timer-Slot nach dem Verkaufsexport, produktiv laut `EINKAUF_LAGERWERT_2026-08-18.md` 12:00, unabhaengig von `Sites.IsActive`; siehe Kopf.)* Die zentrale
  Einkaufsquelle zeigt auf `travp762`. Der Sales-Export bleibt von der
  Einkaufs-Pseudo-Site getrennt.
- Spend-Regeln: nur echte Bestellungen (`Bstyp F`, `Bsart <> UB`), stornierte
  Positionen (`Loekz`) aus historischem Spend ausgeschlossen; offene Werte
  sind Stand-heute, zeitraumunabhaengig und schliessen MSTAE 98/99 sowie
  endgelieferte Positionen aus (`Elikz` steht im Cache als `True`/`False`, Filter seit 2026-10-05 `NOT IN ('X','True','1')`, vorher griff `='X'` nie; offener Bestellwert produktiv ab 2026-10-06 10:16 rund 12.8 statt 28.7 Mio CHF), seit 2026-10-22 ausserdem ohne Altposten (Textposition ohne Material, Liefertermin mehr als 365 Tage vorbei, `PurchasingDashboardService.StaleTextItemSql`; am 05.11. 169 Positionen mit CHF 3,08 Mio., getrennt ausgewiesen; ergaenzt am 2026-11-12); CHF-Bewertung ueber `Waers`/`Wkurs`.
- Arbeitsweise aus dem Marco-Review: jeweils einen Reiter vollstaendig
  abnehmen, bevor der naechste erweitert wird.

- Lagerwert Einkaufsteile (Disponenten 001-006, 006 seit 2026-11-02, Verlauf rechnet rueckwirkend mit; `MBEW.SALK3`, Bewertungskreis 1100): Kachel
  produktiv und in der Datenbank gespeichert, Wert fachlich **noch nicht gegen MB5L
  abgeglichen**. Seit 2026-09-28 zusaetzlich ein **Verlauf je Kalenderwoche, produktiv seit
  2026-09-28 09:33**: Tagesstand je Lauf in `PurchasingStockValueHistory`, Anzeige Stand Ende Woche,
  erster Punkt 10.09.2026, kein Rueckblick. Der Einkauf-Lauf fiel vom 10.09. bis 28.09. aus (IIS-Leerlauf); seit 28.09. Nachhol-Delta und Wachhalten, am 28.09. um 13:19 erstmals wieder erfolgreich. Server-Einstellung offen als ISS-017. Details `docs/EINKAUF_LAGERWERT_2026-08-18.md` Abschnitt 13.

- **Ladezeit `/einkauf` gemessen 2026-09-28: 98,6 s bei laufendem Worker**, also kein
  Kaltstart, sondern die Neuberechnung nach Ablauf des 15-Minuten-Snapshots. Die fruehere
  Angabe „9-11 s warm" trifft diesen Fall nicht. Seit `4f2c62f` erscheint die Seite sofort mit
  Ladebalken; seit `d32a6aa` (produktiv 28.09. 11:25) liefert der Cache abgelaufene Staende sofort und die Standardansicht wird vorgewaermt; seit dem SQLite-Cache (28.09. 13:32) rechnet der Server sie in 24,4 s statt rund 100 s. Details in
  `docs/PLATTFORM_TEMPO_2026-09-28.md` Abschnitt 5.

## Offene Punkte

- Marco-Abnahme: Offenwert gegen SAP und WKURS-Richtung an einem echten
  Fremdwaehrungsbeleg pruefen.
- *(Ueberholt am 2026-10-29: ZLO03-Full-Load und Summenabnahme an einem Mehrfachverwendungsfall sind seit dem
  2026-10-26 gegenstandslos, weil die Produktgruppe nicht mehr ueber die Verwendung vererbt wird; `DISPO D1` und `D5`
  zeigen seit dem 2026-10-01 einen Ersatztext, `3407868`.)*
- SAP-SEGW-Key fuer `ZDISPO_GRP` von nur `DISPO` auf den zusammengesetzten Key
  `DISPO_KZ + DISPO` korrigieren: Empfehlung liegt vor, **Entscheid Ingo offen**.

## Rohquellen Nur Bei Bedarf

- Hauptdoku: `docs/PURCHASING_DASHBOARD_2026-06-05.md`
- Umsetzungsplan: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Formel-Korrekturen: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Marco-Review: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Wuensche Einkaufssitzung: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Nachfolgesitzung 2026-07-30: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Produktgruppen-/ABC-XYZ-Entscheid 2026-08-06:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`
