# Einkaufsdashboard: Formel-Review und Korrekturauftrag (2026-07-06)

Zweck: Diese Datei ist der Arbeitsauftrag fuer die naechste Session (Modell: Opus). Sie enthaelt
das Ergebnis eines Formel-/Logik-Reviews von `docs/PURCHASING_DASHBOARD_2026-06-05.md` gegen den
tatsaechlichen Code. Jede Korrektur hat Prioritaet, Fundstelle, Begruendung und konkreten Fix.

Gepruefte Dateien:

- `docs/PURCHASING_DASHBOARD_2026-06-05.md` (Doku/Formeln)
- `Services/PurchasingDashboardService.cs` (Berechnungen, Cache-SQL)
- `Services/PurchasingDataRefreshService.cs` (Full Load / Delta)
- `Services/DatabaseInitializationService.SchemaSql.cs` (Cache-Schema)
- `Services/IPurchasingDashboardService.cs` (Filter/State)
- `Components/Pages/PurchasingDashboard.razor` (KPI-Labels)

Arbeitsregeln fuer die Umsetzung (aus persona.md / bisheriger Praxis):

- Nach jeder Korrektur: `dotnet test TrafagSalesExporter.sln --verbosity minimal` muss gruen sein.
- Neue Logik mit Tests absichern (analog `PurchasingDashboardServiceTests`).
- `docs/PURCHASING_DASHBOARD_2026-06-05.md` per Nachtrag aktualisieren, nicht umschreiben.
- Kein Deploy ohne Ruecksprache; Schema-Aenderungen nur ueber die bestehende Schema-Maintenance
  (Spalten ergaenzen, keine Migration noetig).

---

## Gesamtbewertung des Ansatzes

Der Grundansatz ist in Ordnung: SAP/OData -> lokale SQLite-Cache-Tabellen (EKKO/EKPO/EKET),
Dashboard liest Cache-first mit Live-Probe als Fallback. Paging mit `$top/$skip/$orderby`,
Matnr-/Lifnr-Normalisierung und die MARA/LFA1-Maps sind sauber umgesetzt.

Aber: Mehrere Kernformeln sind fachlich falsch oder irrefuehrend. Die wichtigsten drei:
**(1) "Spend CHF" summiert ungeprueft Belegwaehrungen**, **(2) das Delta verpasst Wareneingaenge
dauerhaft** (offene Werte veralten), **(3) der Zeitraumfilter schneidet den gesamten zukuenftigen
Zulauf ab** (offener Bestellwert zeigt nur Rueckstand). Details unten, sortiert nach Prioritaet.

---

## K1 (KRITISCH): "Spend CHF" summiert Belegwaehrungen ohne Umrechnung

**Fundstellen:**
- `Services/PurchasingDashboardService.cs:192-196` (`SpendChfSample`) und alle weiteren
  `SUM(CAST(p.Netwr AS REAL))`-Queries (TopSupplier, Charts, Matrix, Konzentration).
- `Services/PurchasingDataRefreshService.cs:51` — Full Load selektiert `Waers,Wkurs` aus `EKKOSet`,
  aber `UpsertEkkoAsync` (Zeile 209-227) schreibt sie **nicht** in Spalten; sie landen nur im
  `RawJson`.
- Schema `PurchasingEkkoCache` (`DatabaseInitializationService.SchemaSql.cs:240-251`): keine
  Spalten `Waers`/`Wkurs`.

**Problem:** `EKPO.NETWR` ist in **Belegwaehrung** (EKKO-WAERS), nicht CHF. Das PBIX-Feld hiess
explizit `Netwr CHF` — dort war eine CHF-Sicht/Umrechnung vorhanden. Das Dashboard addiert aktuell
CHF-, EUR-, USD-Betraege usw. als waeren alle CHF. Alle "CHF"-KPIs (Spend, offener Wert,
Kontraktwert, Preisentwicklung) sind davon betroffen, sobald Fremdwaehrungsbestellungen existieren.

**Korrektur:**
1. Ist-Zustand messen (geht ohne Reload, `RawJson` enthaelt die Felder):
   `SELECT json_extract(RawJson,'$.Waers') AS W, COUNT(*) FROM PurchasingEkkoCache GROUP BY W;`
   Wenn alles CHF ist, K1 nur dokumentieren und Spalten trotzdem nachziehen (Zukunftssicherheit).
2. Schema-Maintenance: Spalten `Waers TEXT NOT NULL DEFAULT ''` und `Wkurs TEXT NOT NULL DEFAULT '0'`
   in `PurchasingEkkoCache` ergaenzen (gleicher Mechanismus wie `SupplierName`/`Mstae`).
3. `UpsertEkkoAsync`: `$Waers`/`$Wkurs` aus der Zeile schreiben. Bestandsdaten einmalig per
   `UPDATE PurchasingEkkoCache SET Waers = json_extract(RawJson,'$.Waers'), ...` befuellen
   (Backfill in Schema-Maintenance oder als Einmal-Statement beim naechsten Full Load).
4. Bewertung in den Queries: `CASE WHEN k.Waers IN ('','CHF') THEN CAST(p.Netwr AS REAL)
   ELSE CAST(p.Netwr AS REAL) * CAST(k.Wkurs AS REAL) END`.
   **Achtung fachlich zu klaeren:** `EKKO.WKURS` rechnet Belegwaehrung -> Hauswaehrung des
   Buchungskreises (`Bukrs` ist im Cache). Wenn nicht alle Bukrs CHF-Hauswaehrung haben, muss
   entweder auf Trafag-CH-Bukrs eingeschraenkt oder ueber die Finance-Kurslogik
   (`FINANCE_KURS_WORKFLOW_2026-06-09.md`) umgerechnet werden. Vor Umsetzung Verteilung pruefen:
   `SELECT Bukrs, json_extract(RawJson,'$.Waers'), COUNT(*) FROM PurchasingEkkoCache GROUP BY 1,2;`
5. Test: Aggregation mit gemischten Waehrungen (CHF + EUR mit Kurs) gegen erwarteten CHF-Wert.

---

## K2 (KRITISCH): Delta verpasst Wareneingaenge — offene Werte veralten dauerhaft

**Fundstelle:** `Services/PurchasingDataRefreshService.cs:86-141` (`RunDeltaAsync`).

**Problem:** Das Delta selektiert geaenderte Belege ueber `EKKO.Aedat ge <deltaFrom>`. In SAP ist
`EKKO-AEDAT` das **Anlagedatum** des Belegs, kein Aenderungsdatum. Ein Wareneingang aendert
`EKET.WEMNG`, aber nicht `EKKO.AEDAT`. Folge: Fuer alle Belege, die vor `deltaFrom` angelegt
wurden, wird `Wemng` nie aktualisiert — "Offener Bestellwert", "Offene Menge", Liefertermin-Risiko
und Restverpflichtung bleiben zu hoch, und zwar wachsend mit jedem Tag ohne Full Load.

**Korrektur (pragmatisch, ohne neue SAP-Objekte):**
1. Im Delta zusaetzlich alle Belege nachladen, die im Cache noch offene Mengen haben:
   `SELECT DISTINCT e.Ebeln FROM PurchasingEketCache e WHERE CAST(e.Menge AS REAL) > CAST(e.Wemng AS REAL);`
   Diese Ebeln-Liste mit den Aedat-Treffern vereinigen und wie bisher EKPO/EKET je Beleg nachlesen.
2. Alternativ/zusaetzlich: EKET fuer offene Belege periodisch komplett neu laden (EKET-Full ist
   laut Full-Load-Zahlen ~243k Zeilen, das ist tragbar).
3. In der Statusmeldung ausweisen, wie viele Belege aus "offen im Cache" nachgeladen wurden.
4. Doku-Nachtrag: klarstellen, dass `Aedat` Anlagedatum ist und das Delta deshalb erweitert wurde.
5. Test: Cache mit offenem Beleg vor `deltaFrom` -> Delta muss ihn in die Nachlade-Liste nehmen.

**Hinweis Performance (M13, gleich mitfixen):** Das Delta macht je Beleg 2 HTTP-Requests
(N+1, Zeile 107-111). Bei der erweiterten Beleg-Liste in Batches filtern:
`$filter=Ebeln eq 'A' or Ebeln eq 'B' or ...` (URL-Laenge beachten, z.B. 20 Belege pro Request).

---

## K3 (KRITISCH): Zeitraumfilter schneidet zukuenftigen Zulauf ab; Risiko-Buckets tot

**Fundstellen:**
- `Services/PurchasingDashboardService.cs:157` — `eketPeriod = e.Eindt >= from AND e.Eindt <= to`.
- Default-Filter `BuildDefaultFilter` (Zeile 21-25): `ToDate = heute`.
- Liefertermin-Risiko `ApplyIdeaAnalyticsAsync` (Zeile 288-321): Buckets `0-7 Tage`, `8-30 Tage`,
  `Spaeter`.

**Problem:** Mit `ToDate = heute` fallen **alle zukuenftigen EKET-Einteilungen** (laut Doku bis
2027-04-20 vorhanden) aus `OpenValueSample`, `OpenQuantitySample`, Zulauf-Charts und
Restverpflichtung heraus. "Offener Bestellwert / disponierter Zulauf" zeigt damit nur den
**Rueckstand** (ueberfaellige Einteilungen), nicht den Zulauf. Die Risiko-Buckets `0-7 Tage`,
`8-30 Tage`, `Spaeter` koennen nie befuellt werden, weil nur `Eindt <= heute` den Filter uebersteht
— alles landet in `Ueberfaellig` (bzw. heutige Faelligkeit in `0-7 Tage`). Die Doku selbst sagt:
"fuer Einkauf ist die Zukunfts-/Faelligkeitssicht fachlich aussagekraeftiger" — genau die fehlt.

**Korrektur:**
1. Fuer offene Werte/Mengen/Zulauf/Risiko eine **eigene** Periode verwenden: Untergrenze wie bisher
   (`Eindt >= from` oder ganz offen), **keine Obergrenze auf heute** — entweder unbegrenzt oder
   `ToDate` nur anwenden, wenn der Nutzer explizit einen Bis-Monat in der Zukunft setzt.
   Empfehlung: offene Positionen unabhaengig von `ToDate` immer bis max(Eindt) einbeziehen und den
   Zeitraumfilter nur auf Vergangenheits-KPIs (Spend, Bestellanzahl ueber `Bedat`) anwenden.
2. Alternativ minimal-invasiv: `eketPeriod` Obergrenze auf `date(to, '+18 months')` setzen —
   aber Variante 1 ist fachlich sauberer.
3. Test: EKET-Zeile mit Eindt in der Zukunft muss in OpenValue/DeliveryRisk (`Spaeter`-Bucket)
   erscheinen, waehrend Spend sie nicht beruehrt.

---

## K4 (HOCH): "Kontrakte/Restverpflichtung" ist nur eine Kopie des offenen Bestellwerts

**Fundstellen:**
- `Services/PurchasingDashboardService.cs:204` — `state.ContractValueSample = state.OpenValueSample;`
- Zeile 273-275 — `ContractChartRows` = Kopie der Commitment-/OpenValue-Charts.
- Razor zeigt beides als getrennte KPIs ("Offener Bestellwert" und "Restverpflichtung/
  Kontrakt-Restwert", z.B. `PurchasingDashboard.razor:1431,1437`).

**Problem:** Zwei unterschiedlich beschriftete Management-KPIs zeigen denselben Wert. Echte
Kontrakte (Rahmenvertraege/Abrufe) sind nicht abgegrenzt. Die Doku nennt die Abgrenzung selbst als
offen — aber die UI tut so, als gaebe es den Kontraktwert schon.

**Korrektur:**
1. `EKKO.Konnr` wird im Full Load bereits selektiert (`PurchasingDataRefreshService.cs:51`), aber
   nicht persistiert. Spalte `Konnr TEXT NOT NULL DEFAULT ''` in `PurchasingEkkoCache` ergaenzen,
   im Upsert schreiben, Backfill via `json_extract(RawJson,'$.Konnr')`.
2. Kontrakt-KPI neu definieren: offener Restwert **nur** fuer Positionen, deren Kopf `Konnr <> ''`
   hat (Abrufe zu Kontrakten). `EKPO.Ktmng` (Zielmenge, wird bereits selektiert, ebenfalls nicht
   persistiert) optional als Spalte nachziehen fuer Abrufquote.
3. Solange keine Konnr-Daten vorhanden sind (Verteilung pruefen:
   `SELECT COUNT(*) FROM PurchasingEkkoCache WHERE json_extract(RawJson,'$.Konnr') <> '';`),
   den KPI in der UI als "in Klaerung" kennzeichnen statt den OpenValue zu duplizieren.
4. `Bsart` ist als Spalte vorhanden, wird aber **nie geliefert** (`$select` enthaelt kein Bsart;
   laut Doku existiert das Feld im Service nicht) — `UpsertEkkoAsync` schreibt immer ''.
   Entweder beim SAP-Team `Bsart` in den Service aufnehmen lassen (wichtig auch fuer die
   Abgrenzung von **Umlagerungen (UB)**, die den Spend aufblaehen) oder den toten Parameter
   entfernen und in der Doku als offenen Punkt fuehren.

---

## K5 (HOCH): KPI "Offene Bestellungen" zaehlt alle Bestellungen im Zeitraum

**Fundstellen:**
- `Services/PurchasingDashboardService.cs:175-179` — `PurchaseOrderCount` =
  `COUNT(DISTINCT k.Ebeln)` im Bedat-Zeitraum.
- `Components/Pages/PurchasingDashboard.razor:707` — Label "Offene Bestellungen / Open orders".
- Doku Zeile 181: "Offene Bestellungen: Anzahl EKKO-Belege im gewaehlten Zeitraum."

**Problem:** Der Wert ist die Gesamtzahl der Bestellungen im Zeitraum, nicht die offenen. Andere
Stellen im Razor labeln denselben Wert korrekt ("Bestellungen im Zeitraum").

**Korrektur (eine der beiden, fachlich abstimmen):**
- Label auf "Bestellungen im Zeitraum" vereinheitlichen (kleinster Eingriff), oder
- echte offene Bestellungen zaehlen:
  `COUNT(DISTINCT k.Ebeln) WHERE EXISTS (SELECT 1 FROM PurchasingEketCache e WHERE e.Ebeln = k.Ebeln AND CAST(e.Menge AS REAL) > CAST(e.Wemng AS REAL))`
  plus aktiver Positionsfilter. Doku entsprechend nachtragen.

---

## K6 (HOCH): Jahresachse hart auf 2026 begrenzt — bricht am 1.1.2027

**Fundstellen:** `Services/PurchasingDashboardService.cs:57,160,653` (`year <= 2026` in LINQ) und
Zeile 672 (`BETWEEN 2020 AND 2026` im SQL von `ExecuteSupplierYearSpendRowsAsync`).

**Problem:** Ab Januar 2027 verschwindet das aktuelle Jahr aus `SpendYears` und der
Lieferant/Jahr-Matrix — stiller Datenverlust in der Management-Sicht.

**Korrektur:** Obergrenze dynamisch: `DateTime.Today.Year` (bzw. `filter.ToDate.Year`).
Untergrenze 2020 kann bleiben (fachliche Vorgabe). Alle drei Stellen + SQL anpassen; Test mit
gemocktem Jahreswechsel oder ueber Filter mit ToDate 2027.

---

## M7 (MITTEL): Endlieferungskennzeichen (ELIKZ) fehlt in der Offen-Logik

**Problem:** Positionen, die in SAP als endgeliefert gelten (`EKPO-ELIKZ = 'X'`) obwohl
`Wemng < Menge` (Unterlieferung akzeptiert), zaehlen dauerhaft als offen. Das ist neben K2 die
zweite Quelle fuer ueberhoehte offene Werte.

**Korrektur:** Pruefen, ob `Elikz` im `EKPOSet` existiert (`$metadata` bzw. `$top=1` mit
`$select=Ebeln,Elikz`; Vorsicht: `Bsart`/`Meins` existierten nicht). Falls ja: in `$select`,
Schema-Spalte und Offen-Formeln aufnehmen (`AND COALESCE(p.Elikz,'') <> 'X'`). Falls nein: als
offenen Punkt mit SAP-Team in die Doku.

---

## M8 (MITTEL): "Offene Menge" ohne Positionsfilter — inkonsistent zum offenen Wert

**Fundstelle:** `Services/PurchasingDashboardService.cs:197` (`OpenQuantitySample`).

**Problem:** Die Mengen-Query summiert alle EKET-Zeilen ohne Join auf EKPO und ohne
`activeItemFilter`; die Wert-Query (Zeile 198-203) filtert geloeschte Positionen. Menge und Wert
passen nicht zusammen, sobald geloeschte/gesperrte Positionen existieren.

**Korrektur:** Gleiche Join-/Filterstruktur wie `OpenValueSample` verwenden (LEFT JOIN EKPO +
`activeItemFilter`). Test: geloeschte Position darf weder in Wert noch Menge zaehlen.

---

## M9 (MITTEL): Preisentwicklungs-Chart zeigt Minimum ueber alle Artikel

**Fundstelle:** `Services/PurchasingDashboardService.cs:346-362`
(`PriceVarianceChartRows` -> `PriceTrendChartRows`).

**Problem:** Die CTE bildet je Artikel/Jahr den Min-Stueckpreis (das entspricht PBIX
`Min(Netwr CHF/Stk)` — ok), aber das Chart aggregiert danach `MIN(UnitPrice) GROUP BY Year` ueber
**alle Artikel**. Die Kurve zeigt also den billigsten Stueckpreis irgendeines Artikels pro Jahr
(praktisch immer ein Cent-Artikel) — als "Preisentwicklung" fachlich aussagelos. Im PBIX ist der
Artikel eine Achsen-Dimension.

**Korrektur:** Entweder (a) Chart je Artikel rendern (Top-N-Artikel nach Spend, eine Serie pro
Artikel), oder (b) einen mengen-/wertgewichteten Durchschnittspreis-Index je Jahr zeigen
(`SUM(Netwr)/SUM(Menge)` je Jahr, gefiltert `Menge > 0`). Variante (a) entspricht der
PBIX-Vorlage. Beschriftung "PowerBI: Min(Netwr CHF/Stk)" (Zeile 338) nur behalten, wenn die
Semantik danach wirklich stimmt.

---

## M10 (NIEDRIG): Kleinere Punkte, in einem Aufwasch erledigen

1. **GetDecimal CurrentCulture-Fallback** (`PurchasingDashboardService.cs:755-762`): Der Fallback
   auf `CurrentCulture` kann je nach Serverkultur Zahlen umdeuten. SAP/OData liefert invariant
   formatiert — Fallback entfernen.
2. **Doku-Formel praezisieren:** `docs/PURCHASING_DASHBOARD_2026-06-05.md` Zeile 209 sagt
   `MAX(EKET.Menge - EKET.Wemng, 0) * (EKPO.Netwr / EKPO.Menge)` — ergaenzen, dass bei
   `EKPO.Menge = 0` der Stueckwert 0 gesetzt wird (so implementiert) und dass der Wert in
   Belegwaehrung ist, bis K1 umgesetzt ist.
3. **Matrix-Kappung dokumentieren:** `ExecuteSupplierYearSpendRowsAsync` nimmt `Take(40)`
   Lieferanten — in der Doku/UI ausweisen ("Top 40"), sonst wirkt die Gesamtsumme unvollstaendig.
4. **SupplierCount** zaehlt nur Lieferanten mit mindestens einer Position `Netwr <> 0`
   (Zeile 186-190) — bewusste Entscheidung? Kurz dokumentieren.
5. **Live-Probe kennzeichnen:** Der SAP-Live-Fallback (`Ebeln ge firstEbeln`, `$top=1000`,
   Zeile 112-131) summiert eine willkuerliche Stichprobe auch ausserhalb des Zeitraums. In den
   KPI-Karten klar als "Stichprobe, keine Summe" markieren, wenn `UsesCache = false`.

---

## Empfohlene Reihenfolge fuer die Umsetzung

1. **K6** (Jahresgrenze) — 15 Minuten, kein Risiko, sofort machen.
2. **K5** (Label offene Bestellungen) + **M8** (Mengenfilter) — klein, testbar.
3. **K3** (Zulauf-Zeitraum) — Kernkorrektur fuer die Offen-Sicht, inkl. Risiko-Buckets testen.
4. **K1** (Waehrung) — Schema + Backfill + Bewertung; vorher Waers/Bukrs-Verteilung messen.
5. **K2** (Delta/Wareneingaenge) + M13-Batching — danach einmal Full Load als sauberer Basiszustand.
6. **K4** (Kontrakte via Konnr) und **M7** (Elikz) — brauchen je einen SAP-Metadaten-Check.
7. **M9** (Preisentwicklung) und **M10** — Rest.

Nach Abschluss: Nachtrag in `docs/PURCHASING_DASHBOARD_2026-06-05.md` (Datum, Was/Warum,
Testresultat), Tests gruen, dann Deploy-Entscheid mit Ingo.

## Validierung nach Umsetzung (Abnahme-Checks)

- `dotnet test TrafagSalesExporter.sln --verbosity minimal` — alle Tests gruen.
- Waehrungscheck: Spend-Summe je Waers vor/nach K1 vergleichen; CHF-Anteil muss unveraendert sein.
- Zulauf-Check: `SELECT SUM(MAX(CAST(Menge AS REAL)-CAST(Wemng AS REAL),0)) FROM PurchasingEketCache WHERE Eindt > date('now');`
  muss nach K3 im "Offener Bestellwert" sichtbar sein (vorher: 0 sichtbar).
- Risiko-Buckets: Nach K3 muessen `0-7 Tage`/`8-30 Tage`/`Spaeter` Werte zeigen, solange offene
  zukuenftige Einteilungen existieren.
- Delta-Check nach K2: Beleg mit Wareneingang von gestern (Wemng-Aenderung, Aedat alt) muss nach
  Delta aktualisiert sein.
- Plausibilisierung mit Marco/Finanzen: ein konkreter Monat + Lieferant gegen PowerBI-Zahlen
  (offener Punkt aus der Doku, gilt weiterhin).
