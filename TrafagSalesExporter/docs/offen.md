# Offene / unklare Punkte

Stand: 2026-07-15

Zweck: Sammelliste dessen, was aktuell fachlich oder technisch noch offen ist.
Schwerpunkt ist der Finance-/Gruppenmargen-Strang (Kontext der letzten Sessions);
angrenzende offene Punkte sind mit aufgefuehrt, damit nichts durchrutscht.

## 1. Gruppenmarge — fachliche Entscheidungen (Andreas)

Diese Punkte kann kein Schalter loesen, weil eine Datenquelle bzw. eine Regel fehlt.

- **Frage A — Kostenart.** Lokaler Einstandswert vs. echte Konzern-Herstellkosten der
  liefernden Gesellschaft.
  - Status: Fuer CH/AT ist der **lokale Einstandswert** (STPRS der *verkaufenden*
    Gesellschaft, CH=1100 / AT=1200) seit 2026-07-14 gefuellt. Waehlt Andreas
    "lokaler Einstandswert", ist A fuer CH/AT damit erledigt.
  - Offen ist nur die Variante "echte Konzern-Herstellkosten der liefernden
    Gesellschaft" — die unterscheidet sich vom heutigen Stand ausschliesslich bei
    internen Lieferzeilen und faellt damit inhaltlich mit Frage B zusammen.
- **Frage B — interner Lieferant: Preis der liefernden oder der verkaufenden Gesellschaft.**
  - Status: **nicht umgesetzt.** Der `StandardCostEnricher` verschluesselt die
    Kostenbasis immer am *eigenen Land der Verkaufszeile*, nie am Lieferanten. Eine
    interne Lieferzeile nutzt heute also implizit den Preis der **verkaufenden**
    Gesellschaft.
  - Fuer die "liefernde Gesellschaft" fehlt: (a) Mapping interner Lieferant -> Trafag-
    Gesellschaft -> Bewertungskreis, (b) Laden dieses zusaetzlichen Bewertungskreises,
    (c) Verschluesselung des Enrichers am Lieferanten statt am eigenen Land.
  - Erleichterung: Der MBEW-Reader kann per dynamischem `$filter` jeden `Bwkey` lesen,
    und `1100` (Trafag AG, typischer interner Fertiger) wird ohnehin schon geladen.
    Es fehlt also die Aufloesung/Verdrahtung, nicht der Datenzugriff.
- **Frage C — lokal vs. konzernweit rechnen.** Erledigt: Group-Currency-(CHF)-Umschalter
  im Management-Cockpit deckt das ab.
- **Frage D — abweichende Kostenwaehrung: maskieren vs. umrechnen.** Technisch erledigt
  als Schalter `ExportSettings.GroupMarginCostCurrencyMode` (Mask=Default / Convert),
  wirkt identisch auf Dashboard, Pruefbuch, zentrale Excel und Nachweis-Excel. Der
  *fachliche* Entscheid Mask vs. Convert liegt weiter bei Andreas; bis dahin gilt Mask.
  Fuer CH/AT ist der Fall heute neutral (Kostenwaehrung = Verkaufswaehrung); scharf wird
  er erst mit Frage B (z. B. CH-Verkauf mit EUR-Kosten einer Liefergesellschaft).

## 2. Gruppenmarge / Standardkosten — Datenluecken

- **UK:** Der Sage-Export liefert keine Kostenspalte -> keine Kostenbasis, Marge offen.
- **FR:** ~49 % der B1-Zeilen haben keinen `StockPrice` (Stammdatenfrage an FR).
- **Waehrungsmisch-Bug (`Marge Original`)**: durch den D-Schalter entschaerft; bleibt
  latent relevant, sobald Konzernkosten in CHF gegen lokale Umsaetze laufen (Frage B).

## 3. Nachsorge nach dem naechsten Export (verifizieren)

- Kostenquote fuer `ZSCHWEIZ` und `TRDE` pruefen; erwartet fuer CH/AT rund **96 %**
  (SAP-Messwert). Deutlich weniger deutet auf ein Material-Matching-Problem hin.
- Gruppenmarge fuer CH/AT und DE fachlich mit Andreas plausibilisieren, bevor sie als
  belastbar kommuniziert wird.

## 4. CH/AT laufendes Jahr fehlt (hohe Prioritaet, separat vom Gruppenmargen-Thema)

- Symptom: CH/AT zeigt 2026 im Dashboard `0`, obwohl der ABAP-Report **9'573**
  Fakturapositionen 2026 (BUKRS 1100) belegt.
- Ursache liegt NICHT bei fehlenden SAP-Daten, sondern im OData-Weg:
  `FinanzdataSchweizOeSet` liefert bei `Gjahr eq '2026'` nichts.
- Zusaetzlich zeigt `Sites.SapServiceUrl` auf `travt762` (Test) statt `travp762` (Prod)
  — DB-Konfiguration, kein Codefix.

## 5. Journal-Import (Hauptbuch)

- **CH/AT:** Das OData-EntitySet `FinanzJournalSet` (BKPF/BSEG) existiert auf SAP-Seite
  noch nicht. Spez fuer das SAP-Team liegt in `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md`.
  Bis dahin meldet der CH/AT-Lauf eine klare Fehlermeldung; die anderen Gesellschaften
  laden normal.
- **`IsManual = Blart 'SA'`** ist eine Annahme und mit Andreas zu bestaetigen.
- **B1-Spaltennamen** (`ProfitCode`, `OcrCode2`, `FCCurrency`, `StornoToTr`, `AutoStorno`)
  vor dem ersten echten Produktivlauf einmal live proben.

## 6. HR-KPI (mit diesem Stand deployed)

- **Sonja** muss die Absenzen weiterhin gegen Rexx abgleichen.
- Die finalen Ranges/Farben fuer die Kranken-/Absenzquote sind fachlich zu bestaetigen.
  Die neuen Schwellen sind konfigurierbar (`AbsenceYellowThresholdPercent` /
  `AbsenceRedThresholdPercent`), also ohne Codeaenderung anpassbar. Default bis zur
  Bestaetigung: Gruen < 3.0 %, Gelb < 5.0 %, Rot ab 5.0 %.

## 7. Budget-CHF (Finanzchef)

- Offen: Budgetkurse/Freigabe, Pflegeprozess, Spaltenumfang, Fehlkursverhalten, Rundung,
  Anzeigeort, DE-2026-Umschaltung, Kontrollnachweis. Fragenkatalog:
  `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md`.
