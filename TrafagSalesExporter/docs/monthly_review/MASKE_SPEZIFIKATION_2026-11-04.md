# Spezifikation „Monthly Review Gesellschaften“ (Nachbau von `maske.html` in Blazor Server / MudBlazor)

Quelle: `C:\Users\koi\source\repos\Ai\TrafagSalesExporter\maske.html` (204 028 Byte, 3 164 Zeilen), vollständig gelesen am 04.11.2026.
Aufbau der Datei: `<style>` (Z. 7–261), HTML-Gerüst (Z. 264–298), `<script>` mit `const DATA = {…}` (Z. 305–706) und `DATA.tsc = {…}` (Z. 714–1939, laut Kommentar „Erzeugt mit tsc/gen_tsc.py“), `<script>` mit IIFE-Anwendung (Z. 1943–3160).
Alle Zahlenprüfungen in diesem Dokument wurden mit Node direkt gegen den Datenblock gerechnet (Abschnitt 3.10 „Erwartungswerte“).

Konventionen: Bezeichner bleiben englisch/original. „LC“ = Lokalwährung. „Tsd. CHF“ = Tausend CHF. Zeilennummern beziehen sich auf `maske.html`.

---

## 0. Architektur in Kürze

- Eine Seite mit drei Ansichten (Tabs): `review` (Monthly Review), `gaps` (Gap-Register), `pbi` (Analyse Power BI).
- In `review` ein Gesellschaftsfilter (Chips): `GRP` → `renderGroup()`, BI-Gesellschaft (TRCH, TRIN, TRIT, TRUS, TRUK, TRES, TRAT, TRDE, TRFR) → `renderCompany(c)`, TSC-Gesellschaft (TRJP, TRCZ, MS, MSA, GFS) → `renderTsc(t)`.
- Zustand: `state = { view: 'review', co: 'GRP' }` plus `gapFilter = 'all'` und Listen-Zustand `LISTS[id]` (Filter, Suche, Sortierung, Limit).
- Jede Zustandsänderung ruft `renderAll()` auf; diese baut die aktive Ansicht komplett neu (HTML-String), zeichnet SVG-Diagramme und schreibt den Hash.
- Die Seite rechnet Deltas, YTD, Prognose EOY, Budget-Erreichung, Margen und Diskussionspunkte selbst aus dem Datenblock (Footer-Text Z. 297).
- Laufzeit-Mutation von DATA: `TSC.forEach(t => { t.isTsc = true; CO[t.code] = t; })` (Z. 1978) und Cache `DATA.rmaCases._prep` (Z. 2449). Im C#-Nachbau als abgeleitete Strukturen modellieren, nicht als Mutation.

---

## 1. Datenmodell `DATA`

Top-Level-Schlüssel (geprüft): `meta, months, lastActual, companies, gm, gmLocal, gmMissing, gmMissingAgg, rmaCases, matTRAT, pbi, rma, churn, customers, gaps, pbiAnalysis, checks, tsc`. Weitere gibt es nicht.

### 1.1 `meta`

| Feld | Typ | Wert/Bedeutung | Verwendet |
|---|---|---|---|
| `title` | string | „Monthly Review Gesellschaften“ | **nicht verwendet** (HTML-Titel hart codiert) |
| `month` | string | „Oktober 2026“ (Berichtsmonat) | Kopfkarte Gesellschaft, TSC-Kopfkarte |
| `monthShort` | string | „Okt. 2026“ | Kachel „Umsatz Okt. 2026“, Diskussionspunkt |
| `pyMonthShort` | string | „Okt. 2025“ | Delta-Label „ggü. Okt. 2025“ |
| `ytd` | string | „01–10.2026“ | Kachel „Umsatz YTD …“, Kopfkarte, Wachstumskarte |
| `ytdPy` | string | „01–10.2025“ | Untertitel Wachstumskarte |
| `stand` | string | „03.11.2026“ | `#stand` im Kopf, Qualitäts-Untertitel |
| `sources[]` | 7 Objekte | `{ id, name, st, d, when }`; `st ∈ ok/part/miss` | Datenquellen-Leiste (`sources()`) |

`sources` (7): `bi` (ok, Internes BI · Verkauf & Prognose, 03.11.2026 16:11), `gm` (part, Internes BI · Gruppenmarge), `pbi` (part, Power BI · Trafag Sales Cockpit, 03.11.2026 15:37), `rma` (part, Power BI · RMA Statistic), `crm` (miss, Dynamics CRM · Leads & Opportunities, when „–“), `tsc` (part, TSC Targets & Progress (Excel)), `bud` (part, Budget & Forecast je Gesellschaft). Texte `d` siehe Z. 311–324.

### 1.2 `months`, `lastActual`

- `months`: 34 Strings `'MM.JJJJ'`, Index 0–21 = Ist 01.2025–10.2026, Index 22–33 = statistische Prognose 11.2026–10.2027 (Kommentar Z. 328).
- `lastActual: 21`.
- **Beide werden im Code nicht gelesen.** Alle Indizes (9, 12, 21, 22, 23) sind hart codiert (siehe Abschnitt 6). Im Nachbau aus `lastActual` ableiten.

### 1.3 `companies[]` (10 Einträge: GRP + 9 BI-Gesellschaften)

Quelle: Internes BI › Verkauf › Saison und Prognose, Sicht je Gesellschaft.

| Feld | Typ | Einheit | Bedeutung |
|---|---|---|---|
| `code` | string | – | GRP, TRCH, TRIN, TRIT, TRUS, TRUK, TRES, TRAT, TRDE, TRFR |
| `name` | string | – | Gruppe, Schweiz, Indien, Italien, USA, Grossbritannien, Spanien, Österreich, Deutschland, Frankreich |
| `legal` | string? | – | Firmenname (fehlt bei GRP und TRDE) |
| `land` | string | – | **nicht verwendet** |
| `cur` | string | – | Lokalwährung (Anzeige „Lokalwährung“) |
| `erp` | string? | – | nur TRCH („SAP S/4HANA“) und TRDE („Alphaplan (Kunden-IDs „TRDE#ALPHAPLAN-ID“)“), sonst Anzeige „tbd.“ |
| `ytd` | number | **Mio CHF** (2 Dez.) | Umsatz YTD 01–10.2026 |
| `ytdPy` | number | Mio CHF | Umsatz YTD 01–10.2025 |
| `fc12` | number | Mio CHF | statistische Prognose nächste 12 Monate (11.2026–10.2027) |
| `rev12` | number | Mio CHF | nur GRP: Umsatz letzte 12 Monate (116.19) |
| `cust12` | int | Anzahl | nur GRP: Kunden mit Umsatz 12 M (3147) |
| `multi` | int | Anzahl | nur GRP: Kunden bei mehreren Gesellschaften (708) |
| `rev[34]` | number[] | **Tsd. CHF** | Monatsumsatz, Index wie `months` |

Hinweise: `ytd`/`ytdPy`/`fc12`/`rev12` stimmen mit den Summen aus `rev` überein (geprüft). Monate 22–33 der Gesellschaften summieren sich nicht zur GRP-Prognose (Prognose je Sicht, nicht additiv; z. B. Index 33: GRP 13 865 vs. Σ 14 048). Ist-Monate 0–21 sind additiv.

### 1.4 `gm` (Gruppenmarge, CHF zum Jahreskurs)

Quelle: Internes BI › Management Analyse › Gruppenmarge, Finance-Jahr 2026. Umsatz inkl. konzerninterner Zeilen. Schlüssel: GRP + 9 BI-Gesellschaften.

| Feld | Einheit | Bedeutung |
|---|---|---|
| `u` | CHF | Umsatz FJ 2026 |
| `k` | CHF | bekannte Kostenbasis |
| `m` | CHF | nur TRAT: Marge (1 544 879) |
| `pct` | % / null | vom Tool ausgewiesene Marge; nur TRAT 47.1, sonst null |
| `intern`, `lokal`, `extern` | Zeilen | Zeilen nach Kostentyp (`extern` nur GRP) |
| `offen` | Zeilen | Zeilen ohne Kostenbasis (je Gesellschaft) |
| `missing` | Zeilen | nur GRP: 2 793 (= Σ `offen`) |
| `unclearSup` | Zeilen | nur GRP: 71 „Lieferant unklar“ |
| `clean` | % | saubere Kostenbasis (GRP 94.4, TRUS 91.6, TRAT 100) |
| `lines` | Zeilen | Detailzeilen (GRP 49 702, TRUS 830, TRAT 950) |
| `prov` | Objekt | nur TRUS: `{ cur:'USD', u, k, m, cover:99.25, openU:36335, openN:70, stdMissing:14, supUnclear:56, mats:357 }` (USD) |

Abstimmung GRP: `intern+lokal+extern = 49 631 = lines − unclearSup`; `Σ u` der 9 Gesellschaften = `GRP.u` (116 347 859).

### 1.5 `gmLocal` (Gruppenmarge in Lokalwährung, Basis der Margenrechnung)

Je BI-Gesellschaft (9): `{ cur, u, k, fx }`, `u`/`k` in LC, `fx` = Jahreskurs LC→CHF „wie im internen BI (Umsatz CHF ÷ Umsatz lokal)“. Werte: TRCH 1, TRIN 1/110, EUR-Gesellschaften 0.94, TRUS 0.80, TRUK 1.09.

### 1.6 `gmMissing` (aktuell `null`)

Zeilen ohne Kostenbasis aus Internes BI › Gruppenmarge › Detailprüfung. Erwartete Form `{ rows: [ { co, st, re, mat, txt, lief, typ, kq, cur, uLoc, uChf } ] }`:
`co` Gesellschaftscode, `st` Status (z. B. „Standardpreis fehlt“, „Lieferant unklar“), `re` Rechnung, `mat` Material, `txt` Bezeichnung, `lief` Lieferant, `typ` Typ, `kq` Kostenquelle, `cur` Währung, `uLoc` Umsatz LC, `uChf` Umsatz CHF (Jahreskurs). **Im Live-Betrieb muss dieser Zweig gerendert werden; in der Datei wurde er nie ausgeführt.**

### 1.7 `gmMissingAgg`

Ersatz, solange `gmMissing` fehlt: `{ TRAT: [0, 0], TRUS: [70, 36335] }` = `[Anzahl Zeilen, Umsatz lokal]`.

### 1.8 `rmaCases` (aktuell `null`)

Offene Q1-Meldungen als Einzelfälle (Power BI RMA Statistic). Erwartete Form `{ stand: 'JJJJ-MM-TT', rows: [[…12]] }`, Zeilenindex:
0 Meldung, 1 Status, 2 Kunde, 3 Materialnummer, 4 Materialkurztext, 5 Menge, 6 Fehlerbild, 7 angelegt, 8 Rücklieferung, 9 Produktion, 10 Massnahme angelegt, 11 Bearbeiter. Datumsfelder ISO. `stand` wird als Stichtag der Altersrechnung verwendet (Z. 2450). **Auch dieser Zweig wurde nie ausgeführt.**

### 1.9 `matTRAT` (Gruppenmarge Materialebene TRAT, EUR)

Quelle: Internes BI › Gruppenmarge › Detailprüfung, TRAT, alle 950 Detailzeilen aggregiert (Kommentar Z. 411).

`{ cur:'EUR', u:3489457, k:1845969, m:1643488, lines:950, mats:259, top20share:67, negMats:4, negM:-746, srcTRAG:3173515, srcLocal:315942, top[10], flop[8] }`.
`top`/`flop`-Zeile: `[Material, Bezeichnung, Umsatz EUR, Kostenbasis EUR, Gruppenmarge EUR, Zeilen, Kostenquelle 'TR AG'|'lokal']`. `top` ist nach Marge absteigend, `flop` ist **nicht** sortiert (Reihenfolge wie geliefert).

### 1.10 `pbi` (Power BI Trafag Sales Cockpit, nur TRCH inkl. IC)

| Feld | Einheit | Bedeutung |
|---|---|---|
| `period` | string | „Rolling 12M 01.12.2025–30.11.2026 (November laufend)“ |
| `oi`, `rev` | CHF | Auftragseingang (86 411 858), Umsatz (81 929 629) R12M |
| `oiYoY`, `revYoY` | % | beide 22.5 (auffällig gleich, siehe 6) |
| `db1, db2, db4` | % | 63.3, 53.1, 23.7 |
| `db1d, db2d, db4d` | Pp. | Veränderung ggü. VJ: 3.3, 4.4, 5.0 |
| `budget` | `{ rev: 71.5 (Mio), db1: 60 (%), db4: 10 (%) }` | statisches Budget (prelim.) |
| `mLabels[11]` | string | 'Dez 25' … 'Okt 26' |
| `oiM[11]`, `revM[11]` | Mio CHF | AE und Umsatz je Monat |
| `db1M[11]`, `db4M[11]` | % | DB1/DB4 je Monat |
| `nov` | Mio CHF | `{ oi: 0.4, rev: 0.8 }` laufender November, ausgeblendet |
| `cats[5]` | Zeile | `[Kategorie, AE CHF, Umsatz CHF, Menge fakt. ST, DB1 %, DB4 %, AE-Wachstum %]` |
| `keyAcc[7]` | `[Name, Mio CHF]` | Umsatz je Key Account |
| `accounts[15]` | Zeile | `[Account, Land, AE CHF, Umsatz CHF, DB1 %, DB4 %, AE-Wachstum % \| null, IC bool]` |
| `ic` | Objekt | `TRUK: { label, oi, rev, db1, db4, oiYoY }`, `TRIN: { label, oi, rev, db1, db4 }` (IC-Lieferungen TRCH an Tochter) |

### 1.11 `rma` (offene Q-Fälle, Aggregat, Stand 03.11.2026)

| Feld | Wert | Bedeutung |
|---|---|---|
| `totalStatus` | 131 | Summe Statusgrafik |
| `totalCust` | 135 | Summe Kundengrafik |
| `status[4]` | `[Status, n]` | Rücksendung offen 84, in Bearbeitung 27, Wareneingang 18, MIAR FRDV OFMA 2 (Σ 131) |
| `lines[5]` | `[Produktlinie, n]` | Dichtewächter 70, TR5 28, TX Linie 16, Trafag CZ 11, übrige 5 Produktlinien 8 (Σ **133**) |
| `queues[4]` | `[Queue, n]` | Detail-Analyse CSQ 12, Übergabe an Verkauf 8, WE CSQ machen 4, Interim Report 2 |
| `byCompany` | `{code: n}` | TRIN 13, TRAT 7, TRUK 4, TRFR 4, TRJP 5, TRIT 3, TRUS 2, TRES 1, TRDE 0 (Σ 39) |
| `companyLabel` | `{code: Kundenname in SAP QM}` | TRDE = „–“ |
| `otherCompanies` | `[]` | `[name, n]`, wird an Gesellschafts-Chart angehängt |
| `endCustomers` | 96 | Meldungen mit Endkunden (TRCH) |
| `endInternal` | 1 | **nicht verwendet** |
| `endTop[7]` | `[Endkunde, n]` | grösste Endkunden |

`Σ byCompany (39) + endCustomers (96) = 135 = totalCust`.

### 1.12 `churn[6]`

`[Quartal, neu, verloren | null]`; Quelle Internes BI › Neue und verlorene Kunden (Gruppe). `null` = Verlust noch nicht sichtbar (erst 6 Monate nach Quartalsende). Werte: 2025 Q2 635/348, Q3 447/311, Q4 392/366, 2026 Q1 341/426, Q2 340/null, Q3 365/null.

### 1.13 `customers[118]`

Quelle: Internes BI › Kunden, 12 Monate bis 10.2026 (Kommentar Z. 501).

`[Rang, Kunde, Land (ISO-2, teils ''), Gesellschaften (durch Leerzeichen getrennte Codes), Umsatz 12M Tsd. CHF, Δ % ggü. Vorperiode | null]`. Umsatz = Kundenumsatz über **alle** beteiligten Gesellschaften. Rang 1–195 mit Lücken, nach Umsatz absteigend sortiert. Land leer bei 6 TRDE-Kunden. `null`-Delta: X Tronics Inc, Astute Electronics Limited. Ausreisser: Pfreundt GmbH −43 405 %; ≥ 500 %: 6 Kunden.
Treffer je Gesellschaft: TRCH 80, TRIN 17, TRIT 24, TRUS 10, TRUK 17, TRES 14, TRAT 13, TRDE 12, TRFR 7.

### 1.14 `gaps[18]` (Gap-Register)

`{ id 'G01'…'G18', prio 1|2|3, area, kpi, status (Freitext), src, chain[4], impact, next }`.
`chain` = `[[Status, Text] × 4]` in Reihenfolge Datenmodell, Prozess, Schnittstelle, Reporting; Status `ok|part|miss|tbd|na`.
Prio-Verteilung: P1 = 6, P2 = 8, P3 = 4. Statuswerte: teilweise, fehlt, Datenqualität, nicht geprüft, veraltet, Darstellung.

### 1.15 `pbiAnalysis`

`pages[4]` `[Seite, Inhalte]`; `filters` string; `notShown[8]` `[Feld/Kennzahl, Tabelle, Nutzen, GapId|'']`; `notInModel[8]` `[Information, Folge, 'G02 · G09' | '']`; `issues[6]` strings; `workspace[5]` `[Bericht, Inhalt, Stand]`.

### 1.16 `checks[14]`

Prüfhinweise (Freitext), werden im Gap-Register als Liste angezeigt.

### 1.17 `tsc` (TSC-Gesellschaften aus „TSC Targets & Progress“ Excel)

`tsc.meta`: `{ stand '03.11.2026', read '04.11.2026', src, fx {EUR 0.94, CZK 0.039265, JPY 0.005239}, fxSrc }` – **wird im Code nicht gelesen** (fx steht zusätzlich je Gesellschaft; Footer hat die Kurse hart im HTML).

`tsc.companies[5]`: TRJP (JPY), TRCZ (CZK), MS (EUR, `label` „M/S“), MSA (EUR, `label` „M/S A“), GFS (EUR). Anzeigename `lab(c) = c.label || c.code`, intern bleibt `code` (auch im Hash).

| Feld | Typ / Einheit | Bedeutung |
|---|---|---|
| `code, label?, name, legal, land, cur, fx` | | `fx` = Konstantkurs LC→CHF; `land` nicht verwendet |
| `author` | string | „Verantwortlich (TSC-Datei)“ |
| `files[2]` | string | Dateinamen 2026/2025 |
| `lastM` | 1–12 | letzter Ist-Monat Umsatz (TRJP 10, TRCZ 10, MS 9, MSA 9, GFS 10); `m26` ab Index `lastM` = Forecast |
| `plM` | 1–12 | letzter Ist-Monat P&L (TRJP 9, TRCZ 10, MS 9, MSA 9, GFS 9). **Code liest stattdessen `pl.m`** (gleiche Werte) |
| `revDef` | string | Umsatzdefinition (Bruttoumsatz Dritte …) |
| `m25[12]`, `m26[12]` | LC | Monatsumsatz Dritte 2025 / 2026 |
| `bud` | LC | Budget GJ 2026 |
| `budM` | LC | Budget je Monat (= bud/12, gerundet) |
| `foc` | LC | Forecast GJ 2026 (= Σ m26 bis auf Rundung) |
| `lines[]` | `[Konto, YTD (bis lastM), VJ YTD, Budget GJ, FOC GJ]` LC | Umsatz je Konto; Σ YTD = Σ m26[0..lastM−1] |
| `ded` | `{ ytd (LC, negativ), n }` | Erlösminderungen (MS, MSA, GFS); nicht abgezogen |
| `ic` | `{ m25[12], m26[12], n, ytd, pyYtd, bud, foc, pyFy }` LC oder null | Intercompany 6010 (TRCZ, GFS) |
| `pl` | `{ net, db1, db2, db3, pers }` je `{ ytd, budYtd, bud, foc, pyYtd, pyFy }` LC, plus `m` | TSC-P&L bis Monat `pl.m`; `net` = Nettoumsatz inkl. IC nach Erlösminderungen. `pers` **nicht verwendet** (TRJP `pers.pyYtd` null) |
| `oi` | `{ ytd, foc, m26[12], revYtd }` LC oder null | nur TRJP: Auftragseingang (Verkaufsblatt); `revYtd` = Umsatz Trafag-Produkte YTD |
| `oiNote` | string | Grund, warum `oi` fehlt |
| `acc[]` | `[Kunde, Branche, Umsatz YTD, Budget GJ, AE YTD, Forecast GJ]` LC | Verkaufsblatt; nach Umsatz absteigend. TRJP 26, TRCZ 31, MS 0, MSA 0, GFS 5 Zeilen |
| `accCover` | `[Σ Kundenumsatz, Konto 6005]` oder null | nur TRCZ `[2225058, 2639489]` |
| `q` | `{ n, kind 'cust'|'line', label }` oder null | TRJP `cust` 5, TRCZ `line` 11, MS/MSA/GFS null |
| `trchIc` | `{ k12: 1740 (Tsd. CHF), pbiRev: 1621280 (CHF), pbiDb4: -36 (%) }` | **nur bei GFS** (nicht top-level): TRCH-Umsatz mit Kunde „GFS Germany“ |
| `parts` | – | wird im Code gelesen, **existiert in keinem Datensatz** (toter Zweig, siehe 6) |

Abstimmungen (geprüft): `pl.net.ytd = Σ m26[0..pl.m−1] + Σ ic.m26[0..pl.m−1] + ded.ytd`; TRJP `Σ acc[4] = oi.ytd`; TRJP `Σ acc[2] = lines[0][1] = oi.revYtd`; TRCZ `Σ acc[5] = accCover[0]`.

---

## 2. Ansichten und Layout

### 2.0 Shell (immer sichtbar)

**Kopfleiste** (`header.top`, sticky):
- Eyebrow „Trafag · Sales-Steuerung“, H1 „Monthly Review Gesellschaften“, Untertitel „Berichtsmonat Oktober 2026 · YTD 01–10.2026 · Datenstand {meta.stand} · statischer Snapshot“ (Monat/YTD hart im HTML, nur `stand` dynamisch).
- Tabs (role=tablist, aria-label „Ansicht“): „Monthly Review“ (`review`), „Gap-Register“ (`gaps`), „Analyse Power BI“ (`pbi`).
- Werkzeuge: „◐ Thema“ (title „Hell/Dunkel umschalten“), „⎙ Drucken“ (title „Aktuelle Ansicht drucken“).
- Filterzeile (nur bei `view === 'review'` sichtbar): Label „Gesellschaft“, Chips (role=radiogroup, aria-label „Gesellschaft wählen“): zuerst die 10 `companies` (GRP ohne Zusatz, sonst `code <small>name</small>`), dann Trenner „TSC-Dateien“, dann 5 TSC-Chips (gestrichelter Rahmen, title „Werte aus der TSC-Datei (Excel), nicht im internen BI“, Text `lab(t)` + `<small>name</small>`).

**Footer** (statischer Text, Z. 295–297): „Quellen: …“, „TSC-Gesellschaften (TRJP, TRCZ, M/S, M/S A, GFS): …“, „Aktualisierung: …“ mit Badge „⚠ GAP“. Texte vollständig in Abschnitt 5.

**Interaktionen der Shell**
- Hash-Routing: `#<view>/<co>`; `readHash()` akzeptiert nur `review|gaps|pbi` und Codes aus `CO` (BI + TSC, intern `MS`/`MSA`). `writeHash()` per `history.replaceState` (kein Browser-Verlauf). `hashchange` → `readHash(); renderAll()`.
- Tab-Klick → `state.view`; Chip-Klick → `state.co`; jeweils `renderAll()`.
- Klick auf `[data-gap]` (Gap-Link „→ Gxx“, auch per Enter): Wechsel zu `gaps`, `gapFilter='all'`, Zeile `#gap-Gxx` mittig scrollen, 1.6 s Outline `2px solid var(--s1)`.
- Klick auf `[data-tv]` („Tabelle“-Knopf einer Diagrammkarte): Tabellenansicht ein/aus, Knopftext wechselt „Tabelle“ ↔ „Diagramm“, Diagramm wird versteckt. Zustand geht beim nächsten `renderAll` verloren.
- Thema: Umschalten von `data-theme` light/dark (Ausgangswert ohne Attribut: `prefers-color-scheme`), gespeichert in `localStorage['mr-theme']`, danach `renderAll()` (Diagrammfarben werden beim Zeichnen aus CSS-Variablen gelesen).
- Drucken: `window.print()`; Print-CSS: Kopf nicht sticky, Tabs/Werkzeuge/`.tbtn` ausgeblendet, nur aktive Ansicht.
- Resize: debounce 160 ms → `renderAll()` (Diagrammbreite = Containerbreite, min. 280 px).
- Tooltip: ein globales `div.tip` (position fixed), Inhalt `{ title, rows: [{ k, v, color? }] }`, öffnet bei `pointerenter`/`focus` auf `[data-tip]`, folgt `pointermove`, schliesst bei `pointerleave`/`blur`; Positionierung 14 px rechts/12 px über Zeiger, bei Platzmangel gespiegelt. Hit-Flächen sind fokussierbar (`tabindex=0`, `aria-label`).

### 2.1 Gemeinsame Bausteine

- **Datenquellen-Leiste** `sources()`: `<details>` mit Summary „Datenquellen“ + je Quelle eine Pille (Badge + Name + `when`, title = `d`) + „Details“; aufgeklappt ein Raster mit Karte je Quelle (Badge, Name, `d`, „Stand: {when}“). Badge: `ok` → „✓ vollständig“, `part` → „~ teilweise“, sonst „✕ fehlt“.
- **Abschnittskopf** `sec(n, title, sub)`: nummeriertes Quadrat + H2 + grauer Untertitel.
- **Kachel** `tile(o)`: normal = Label (+ optional Badge), Wert (`valHtml`), Delta-Zeile (`dl`), optional Sparkline, Fusszeile (`ft`) + optional Gap-Link. Gap-Kachel (`o.gap`) = gelb gestrichelt, Badge „⚠ GAP“, Wert (Default „Daten fehlen“), Begründung (`why`), Fuss + Gap-Link.
- **Diagrammkarte** `chartCard(id, title, cap, legend, tableHtml)`: H3, Caption, optional Knopf „Tabelle“, Legende, `div.chart#id`, versteckte Tabelle `#tv-id`.
- **Legende** `lg(color, label, kind)`: Farbfeld (Linie bei `kind='line'`).
- **Abhängigkeitskette** `chain(ch)`: 4 Pillen „{Symbol} Datenmodell › Prozess › Schnittstelle › Reporting“, Symbol `ok ✓ / part ~ / miss ✕ / tbd ? / na –`, title „{Stufe}: {Text oder Status}“, CSS-Klasse = Status.
- **Gap-Link** `gapLink(id)`: „→ Gxx“.
- **Pipeline-Abschnitt** `pipelineSection(c)` (identisch für alle Gesellschaften, Abschnitt 3): Untertitel „Dynamics CRM ist nicht an das Reporting angebunden – Sicht vorbereitet, Werte fehlen.“; Gap-Panel „GAP: Keine Lead- und Opportunity-Daten für {GRP: ‚die Gesellschaften‘ | lab(c)}“, Text „Weder das Power BI Sales Cockpit noch das interne BI enthalten CRM-Daten. Ziele je KPI: **Details tbd.** → G03 → G04“; 8 Skelett-KPIs (Titel/Beschreibung, jeweils „– Daten fehlen · Ziel tbd.“): Pipeline offen (gewichtet) / Pipeline-Abdeckung / Neue Opportunities im Monat / Gewonnen / verloren & Win-Rate / Ø Alter & überfällige Abschlüsse / Neue Leads & Konversion / Top-10 Opportunities / Aktivitäten / Besuche (Beschreibungen Abschnitt 5); „Abhängigkeiten (Reihenfolge):“ + `chain(G03.chain)` + Liste „{Stufe}: {Text}“ für nicht-leere Texte.
- **Diskussionspunkte-Tabelle** `actionsTable(points)`: Karte „Diskussionspunkte aus den Daten“, Caption „Automatisch abgeleitet – Massnahmen, Verantwortliche und Termine im Meeting festlegen.“, Badge „⚠ Massnahmen-Tracking fehlt · → G14“; Spalten „# | Thema (aus Daten) | Bezug | Massnahme | Verantwortlich | Termin“; Bezug = Gap-Link oder „–“; die drei letzten Spalten immer „tbd.“.
- **Kundentabelle** `custTable(rows)`: Spalten „Rang | Kunde | Land | Gesellschaften | Umsatz 12M | Δ ggü. VJ“; Land leer → „–“; Gesellschaften mit „, “ verbunden; Umsatz `fKM`; Δ `null` → „neu“, sonst `delta(d,'',0)`; Flags: Kundenname exakt „GFS Germany“ (case-insensitive) → „IC · GfS“ (title „GFS Germany Gesellschaft für Sensorik GmbH = Konzerngesellschaft GfS, nicht als Konzernkunde gekennzeichnet“); `d ≤ −100` → „Daten prüfen“; `d ≥ 500` → „Basiseffekt“.

### 2.2 Listen-Engine (Drilldown mit Filter, Suche, Sortierung, CSV)

`listHtml(id, cfg)` / `listRender(id)` / `listCsv(id)`; Zustand je `id` in `LISTS` bleibt über Re-Renders erhalten (Filter, Suche, Sortierung; `limit` wird auf `cfg.limit` zurückgesetzt).
- Filter-Selects je `cfg.filters[{key,label}]`: Optionen = eindeutige, nicht-leere Werte sortiert; erste Option „Alle ({Anzahl})“.
- Suchfeld (Label „Suche“, Placeholder `cfg.search`): case-insensitiv, Teilstring über alle **angezeigten** Spalten (`cols[].k`).
- Zähler: „{gefiltert} von {gesamt} Einträgen“ + optional „ · Σ {sumFmt(Σ sumKey)}“.
- Knopf „⇩ CSV (Excel)“.
- Spaltenkopf (title „Sortieren“) klickbar: gleiche Spalte → Richtung umkehren; neue Spalte → numerisch absteigend (`num`), sonst aufsteigend; Pfeil „ ▲“/„ ▼“ im Kopf. `null` immer am Ende. Vergleich mit `>`/`<` (kein Locale-Vergleich).
- Anzeige der ersten `limit` Zeilen (150); Knopf „Weitere {min(500, Rest)} von {Rest} anzeigen“ erhöht um 500.
- Leere Menge: „Keine Einträge für diese Auswahl.“
- Nicht-numerische Zellen erhalten `title` mit vollem Wert (Ellipsis bei 280 px).
- CSV: UTF-8 mit BOM, Trennzeichen `;`, Zeilenende CRLF, Quoting bei `" ; \r \n`, Spalten aus `csvCols` (sonst `cols`), Werte via `col.csv` sonst roh; Dateiname `csvName`.

**Liste „Zeilen ohne Kostenbasis“** `missList(id, fixedCo)` (nur wenn `gmMissing.rows` vorhanden, sonst Panel „Liste der Zeilen ohne Kostenbasis noch nicht geladen“ / „Quelle: internes BI › Management Analyse › Gruppenmarge › Detailprüfung (je Land gefiltert). Beim Erstellen war das interne BI nicht erreichbar.“):
- rows = alle bzw. `co === fixedCo`; Suche „Material, Rechnung, Bezeichnung …“; Default-Sort Spalte 7 (`uChf`) absteigend; Filter: [Gesellschaft (nur ohne fixedCo)], Status, Typ.
- Spalten: Ges. (`co`), Status, Rechnung (mono), Material (mono), Bezeichnung, Typ, Kostenquelle (`kq`), Umsatz CHF (`fInt`), Umsatz lokal (`fInt + ' ' + cur`).
- CSV-Spalten: Gesellschaft, Status, Rechnung, Material, Bezeichnung, Lieferant, Typ, Kostenquelle, Währung, Umsatz lokal, Umsatz CHF (Jahreskurs); Beträge auf 2 Dezimalen mit Punkt.
- Dateiname `Zeilen_ohne_Kostenbasis_{fixedCo|alle}_FJ2026.csv`; Summe „Umsatz CHF {fInt}“.

**Liste „offene Q1-Meldungen“** `caseList(id, filterFn)` (nur wenn `rmaCases` vorhanden, sonst Panel „Einzelfall-Liste noch nicht geladen“ / „Quelle: Power BI „RMA Statistic“ (Tabelle „Meldung“). Beim Erstellen war Power BI nicht erreichbar.“):
- Suche „Meldung, Kunde, Material, Fehlerbild …“; Default-Sort Spalte 8 (`age`) absteigend; Filter Gesellschaft (`co`), Status, Bearbeiter.
- Spalten: Meldung (mono), Status, Ges., Kunde, Material (= Kurztext `txt`), Fehlerbild, Menge (`fInt`, null „–“), Angelegt (`fDate`), Offen seit (Tage) (rot + „ ▲“ wenn > 90), Rücklieferung, Tage seit Rücklief., Massnahme angelegt, Bearbeiter.
- CSV-Spalten: Meldung, Status, Gesellschaft, Kunde, Materialnummer, Materialkurztext, Reklamierte Menge, Fehlerbild, Angelegt am RMA, Offen seit (Tage), Rücklieferungsdatum, Tage seit Rücklieferung, Produktionsdatum, Angelegt am Massnahme, Tage bis Massnahme, Bearbeiter. Datumsfelder als TT.MM.JJJJ. Dateiname `Offene_Q1-Meldungen_{id}.csv`.

### 2.3 Ansicht `review` · Gruppe (`renderGroup`, co = GRP)

Reihenfolge:

1. Datenquellen-Leiste.
2. **Abschnitt 1 „Performance Gruppe“** – Untertitel „Umsatz nach Finance-Regeln, ohne Konzernkunden, CHF zum Kurs des Belegdatums.“ Kacheln:
   - „Umsatz {monthShort}“: `fKM(oct)`, Delta `octD` „ggü. {pyMonthShort}“, Sparkline `spark`, ft „Vorjahresmonat {fKM(octPy)}“.
   - „Umsatz YTD {ytd}“: `fMio(c.ytd)`, Delta `ytdD` „ggü. VJ“, ft „Vorjahr {fMio(ytdPy)} · 12 Monate {fMio(c.rev12)} · 9 Gesellschaften im internen BI“.
   - (falls TSC vorhanden) „TSC-Gesellschaften YTD“, Badge „separat“: `fMio(tY)`, Delta `pct(tY,tP)` „ggü. VJ“, ft „{lab je TSC mit ' · '} · Excel, CHF zu Konstantkurs[, {späte TSC mit ' und '} bis {mmYY(min lastM)}] · Gruppe inkl. ≈ {fMio(c.ytd + tY)} (indikativ) → G17“.
   - „Prognose EOY 2026“, Badge „statistisch“: `fKM(fy26e)`, Delta `fyD` „ggü. GJ 2025 ({fKM(fy25)})“, ft „Ist Jan–Okt + Prognose Nov–Dez, keine Planung → G09“.
   - „Kunden mit Umsatz (12 M)“: `cust12`, dl „{multi} kaufen bei mehreren Gesellschaften“, ft „2026 Q1: {churn[3][1]} neu · {churn[3][2]} verloren“.
   - Gap „Budget & Auftragseingang“: Wert „Nur teilweise“, why „Budget nur für die TSC-Gesellschaften (Excel) – kein Soll/Ist für Gruppe und BI-Gesellschaften → G01. Auftragseingang nur TRCH (R12M {fCHFM(pbi.oi)}, inkl. IC) und TRJP.“, Gap G02.
3. Raster g-2:
   - Diagrammkarte `ch-month` „Umsatz je Monat 2026 vs. 2025 · Gruppe“, Caption „Tsd. CHF · Nov./Dez. 2026 = statistische Prognose“, Legende „2026 Ist“ (s1), „2026 Prognose“ (s1-fc), „2025 Ist“ (deemph, Linie). Tabelle: Monat | 2025 | 2026 | Δ; Monate Nov/Dez mit Badge „Prognose“.
   - Diagrammkarte `ch-grow` „Wachstum YTD je Gesellschaft“, Caption „Umsatz {ytd} ggü. {ytdPy} · gestrichelt = Gruppe (internes BI)[ · * TSC-Datei, währungsbereinigt[, {späte} {per(min lastM)}]]“. Tabelle: Gesellschaft | YTD 2025 | YTD 2026 | Δ; TSC-Zeilen „{lab} * {per(n)}“.
4. Karte „Scorecard Gesellschaften“, Caption „Zeile anklicken → Monthly Review der Gesellschaft · Q1 = offene Q-Meldungen mit Gesellschaft als Kunde“. Spalten: Ges. | YTD 2026 | YTD 2025 | Δ YTD | Okt. 26 | Δ Okt. | Progn. EOY 26 (title „Ist Jan–Okt + statistische Prognose Nov–Dez“) | Progn. 12M (title „statistische Prognose 11.2026–10.2027“) | Gruppenmarge | Q1.
   - BI-Zeile: Code + Name; `fMio(ytd)`, `fMio(ytdPy)`; Δ YTD = Text `fPct` (eingefärbt) + Divergenzbalken (Breite `min(60,|dv|)/60*50` % je Seite, positiv s1 rechts, negativ rot links); `fKM(oct)`; `delta(octD)`; `fKM(fy26e)` + Sub „{fPct(fyD)} ggü. GJ 25“; `fMio(fc12)`; Gruppenmarge-Badge: `gm.pct` → grün „{pct} %“, sonst `gm.prov` → „{prov.m/prov.u·100} % vorl.“, sonst gelb „offen {gm.offen}“; Q1 = TRCH: `endCustomers` + Sub „Endkunden“, sonst `rma.byCompany[code]`.
   - Trennzeile „**TSC-Gesellschaften** · aus „TSC Targets & Progress“ (Excel) · Bruttoumsatz Dritte, CHF zu Konstantkurs · **nicht** im Gruppentotal → G17“.
   - TSC-Zeile: `lab` + Name; `fMio(st.ytd)` + Sub „[bis {mmYY(n)} · ]Budget {budAch} %“; `fMio(st.ytdPy)`; Δ YTD wie oben; Okt. = `fKM(st.oct)` oder „offen“ (title „TSC-Datei Stand {mmYY(n)}“); Δ Okt. oder „–“; `fKM(fy26e)` + Sub „{fPct(fyD)} ggü. GJ 25 · TSC-FOC“; Progn. 12M „–“ (title „keine 12-Monats-Prognose in den TSC-Dateien“); Badge „nicht im BI“ + Sub „DB1 lokal {db1} %“; Q1 = `byCompany[code]` oder „–“.
   - Note: „Budget, Auftragseingang und Pipeline fehlen für die BI-Gesellschaften (ausser AE für TRCH) – siehe → G01 → G02 → G03. Prognosen je Gesellschaft sind statistisch und nicht additiv zur Gruppenprognose. TSC-Gesellschaften: Budget-Erreichung YTD aus dem TSC-Budget (linear), Prognose EOY = Forecast der TSC-Datei, DB1 lokal aus der TSC-P&L (inkl. IC, bis 09.2026; TRCZ 10.2026).“
   - Zeilen mit `data-co` sind klickbar (Klick oder Enter) → `state.co` setzen, `renderAll()`, nach oben scrollen.
5. **Abschnitt 2 „Marge · Gruppenmarge & Deckungsbeitrag“** – Untertitel „Internes BI › Gruppenmarge, Finance-Jahr 2026. Berechnung in Lokalwährung je Gesellschaft, Zeilen ohne Kostenbasis ausgeschlossen, CHF zum Jahreskurs.“ Kacheln:
   - `exSub = gmExSum(SUBS)` vorhanden → „Gruppenmarge Tochtergesellschaften“, Badge „ohne fehlende Zeilen“, `fPctU(exSub.pct)`, dl „{fCHFM(m)} auf {fCHFM(uOk)} Umsatz“, ft „Summe TRIN, TRIT, TRUS, TRUK, TRES, TRAT, TRDE, TRFR · Endkundenpreis − TR AG-Standardkosten“; sonst Gap „Berechnung ausstehend“ / „Liste der Zeilen ohne Kostenbasis nicht geladen.“ (G05). **Im Snapshot: Gap**, weil nur TRAT/TRUS Werte haben.
   - `exCH` vorhanden → „Marge TRCH“, Badge „ohne fehlende Zeilen“, dl wie oben, ft „Abgrenzung IC-Zeilen offen – nicht mit Töchtern addieren → G07“ (im Snapshot nicht angezeigt).
   - „Umsatz ohne Kostenbasis“, Badge „⚠ {gm.GRP.missing} Zeilen“: Liste geladen → `fCHF(Σ uChf)` + dl „netto, {Anteil} % des Umsatzes“; sonst „{missing} Zeilen“ + dl „Liste nicht geladen“; ft „davon „Lieferant unklar“: {unclearSup} · Liste unten → G05“.
   - „Saubere Kostenbasis“: `nf1(clean) %`, dl „{lines} Zeilen“, ft „Typ Intern: … · Lokal: … · Extern: …“.
   - Gap „Deckungsbeitrag fix/variabel“: „Kein Split in den Daten“, why „Das interne BI zeigt 0 Zeilen mit fix/variabel-Aufteilung. DB-Stufen gibt es nur für TRCH im Power BI (DB1 63.3 % → DB2 53.1 % → DB4 23.7 %).“ (G05).
6. Karte „Gruppenmarge je Gesellschaft – ohne Zeilen ohne Kostenbasis“, Caption „Umsatz mit Kostenbasis − bekannte Kostenbasis (TR AG-Standardkosten bzw. lokaler Standardpreis) · Zeile anklicken → Gesellschaft“. Spalten: Ges. | Umsatz FJ 26 | Zeilen o. Kosten | Umsatz o. Kosten | Umsatz mit Kostenbasis | Kostenbasis | Gruppenmarge | Marge % | Tool-Ausweis | Materialebene.
   - Ohne `ex`: Code, `fCHFM(gm.u)`, `gm.offen`, colspan 5 „Berechnung ausstehend“, Tool-Ausweis, Materialebene.
   - Mit `ex`: Code + Sub Währung, `fCHFM(uChf)`, `nMiss`, `fCHF(uMissChf)` + Sub `missShare %`, `fCHFM(uOkChf)`, `fCHFM(kChf)`, `fCHFM(mChf)`, Inline-Balken `pct` (Breite `clamp(pct,1,100)` % von 90 px).
   - Tool-Ausweis: `gm.pct` → grün „{pct} %“, sonst gelb „keine“. Materialebene: TRAT „Top/Flop“, sonst „fehlt“.
   - Note: „Umsatz ohne Kosten ist netto (Gutschriften und Belastungen gleichen sich teils aus). Die TRCH-Zeilen enthalten vermutlich auch Lieferungen an die Tochtergesellschaften – deshalb TRCH nicht mit den Töchtern zu einer Gruppenmarge addieren → G07. Deckungsbeitrag fix/variabel: keine Daten → G05.“
7. Nur wenn `gmMissing.rows` vorhanden, Raster g-2:
   - Diagrammkarte `ch-misco` „Zeilen ohne Kostenbasis je Gesellschaft“, Caption „Anzahl Zeilen · Σ {n}“ (HBar „{code} · {name}“, nur > 0, absteigend).
   - Karte „Was sind die Zeilen ohne Kostenbasis?“, Caption „Nach Status und grösste Materialien (Σ |Umsatz|)“: Tabelle Status | Zeilen | Umsatz CHF netto (Statusreihenfolge = erste Vorkommnisse); darunter `missMatTable(null)`.
8. Karte „Liste: alle Zeilen ohne Kostenbasis“, Caption „Filter nach Gesellschaft, Status und Typ · Spaltenkopf klicken zum Sortieren · CSV öffnet in Excel“ + `missList('miss-all')` (oder Panel „noch nicht geladen“).
9. **Abschnitt 3** Pipeline (`pipelineSection`).
10. **Abschnitt 4 „Qualität · offene Q-Fälle“** – Untertitel „SAP QM-Meldungen Q1 aus Power BI „RMA Statistic“, Stand {rmaCases ? fDate(rmaCases.stand) : meta.stand}.“ Kacheln:
    - „Offene Q1-Meldungen“: `cs.length` bzw. `totalStatus`; dl „Rücksendung offen: {n}“ (`/Rücksendung/` auf Status bzw. `status[0][1]`); ft „Einzelfall-Liste unten“ bzw. „Statussicht 131 · Kundensicht 135“.
    - mit Fällen: „Offen seit (Ø / Median)“ `avg` + „Tage“, dl „Median {med} Tage“, ft „ab „Angelegt am RMA“ bis Datenstand“; ohne: Gap „Offen seit“ / „Liste nicht geladen“ / „Einzelfälle aus RMA Statistic fehlen noch.“ (G08).
    - nur mit Fällen: „Länger als 90 Tage offen“ (Badge „⚠“ wenn > 0), Wert `over90`, dl „{over90/n %} der offenen Fälle“, ft „Ältester Fall: {nr} · {age} Tage“.
    - „Übergabe an Verkauf“: `queues[1][1]`, dl „warten auf Sales Coordinator“, ft „Detail-Analyse CSQ: {queues[0][1]} · WE CSQ: {queues[2][1]}“.
11. Raster g-3: `ch-qstat` „Status“ / „Anzahl offener Meldungen“; zweite Karte mit Fällen `ch-qage` „Offen seit“ / „Anzahl Meldungen nach Alter“ (ageBuckets), ohne Fälle `ch-qline` „Produktlinie“ / „Anzahl offener Meldungen“; `ch-qco` „Gesellschaft / Kunde“ / Caption mit Fällen „Zuordnung über Kunde in SAP QM“, ohne „Anzahl · {endCustomers} weitere mit Endkunden“.
12. Nur mit Fällen, Raster g-2: `ch-qline` „Produktlinie (Quellbericht)“ + Karte „Offen seit je Status“ / „Tage seit Anlage der Meldung“: Status | Fälle | Ø Tage | Median | > 90 Tage.
13. Karte „Drilldown: offene Q1-Meldungen“, Caption „Filter nach Gesellschaft, Status und Bearbeiter · Sortierung per Spaltenkopf · „Offen seit“ ab Anlage der Meldung“ + `caseList('q-all')`.
14. **Abschnitt 5 „Kunden“** – „Kundenbewegung und Top-Kunden der Gruppe.“ Raster g-2: `ch-churn` „Neu gewonnene vs. verlorene Kunden je Quartal“, Caption „Gruppe · Verluste erst 6 Monate nach Quartalsende sichtbar · 2025 Q1 nicht belastbar (erstes Datenjahr)“, Legende „neu gewonnen“ (s1), „verloren“ (red); Tabelle Quartal | neu | verloren („offen“). Karte „Top 10 Kunden Gruppe (12 M)“ / „Internes BI › Kunden · ohne Konzernkunden“ + `custTable(top 10 nach Umsatz)`.
15. **Abschnitt 6 „Diskussionspunkte & Massnahmen“** + `actionsTable(gp)` (Regeln 3.7).

### 2.4 Ansicht `review` · BI-Gesellschaft (`renderCompany(c)`, `isCH = c.code === 'TRCH'`)

1. Datenquellen-Leiste.
2. Kopfkarte: „GESELLSCHAFT“, „{code} · {name}“, `legal`; dl „Lokalwährung {cur}“, „ERP {erp | tbd.}“; dl „Berichtsmonat {meta.month}“, „YTD {meta.ytd}“.
3. **Abschnitt 1 „Performance“** – „Umsatz nach Finance-Regeln, ohne Konzernkunden, CHF zum Kurs des Belegdatums.“ Kacheln:
   - „Umsatz {monthShort}“ wie Gruppe, ft „Vorjahresmonat {fKM(octPy)} · Verlauf 11.2025–10.2026“.
   - „Umsatz YTD {ytd}“, ft „Vorjahr {fMio(ytdPy)} · 12 Monate {fKM(r12)}“.
   - „Prognose EOY 2026“ (Badge „statistisch“), ft „Ist Jan–Okt + Prognose Nov–Dez (Vorjahresmonat × Wachstum), keine Planung“.
   - Gap „Budget-Erreichung“: TRCH „Kein Soll/Ist je Monat“ / „Power BI zeigt nur ein statisches Budget (prelim.): Umsatz p.a. 71.5 Mio, DB1 60 %, DB4 10 %. Abgrenzung (inkl. IC?) Details tbd.“; sonst „Kein Budget je Gesellschaft“ / „Weder im internen BI noch im Power BI ist ein Budget/Ziel für {code} hinterlegt.“ (G01).
   - TRCH: „Auftragseingang R12M“, Badge „inkl. IC“, `fCHFM(pbi.oi)`, Delta `oiYoY` „ggü. VJ (TD)“, ft „Book-to-Bill {oi/rev} (Okt. {oiM[10]/revM[10]}) · Power BI, Auftragsbestand fehlt → G02“. Sonst Gap „Auftragseingang & Book-to-Bill“ / „Nicht im Reporting“ / „Aufträge der Tochter-ERPs sind nicht angebunden, Book-to-Bill daher nicht berechenbar[. Hinweis: IC-Bestellungen „{ic.label}“ bei TRCH R12M {fCHFM(ic.oi)}[ ({fPct(ic.oiYoY)})]].“ (G02).
   - „Prognose nächste 12 Monate“ (Badge „statistisch“), `fMio(fc12)`, Delta `pct(fc12*1000, r12)` „ggü. letzte 12 M“, ft „Internes BI · 11.2026–10.2027 · Methode ohne Planung → G09“.
4. Nur TRCH: Warn-Note „Im TRCH-Umsatz ist der Kunde **„GFS Germany“** enthalten (12M 1.74 Mio, Power BI R12M 1.62 Mio mit DB4 −36.0 %). Das ist die Konzerngesellschaft GfS (Gesellschaft für Sensorik GmbH) – im Kundenstamm aber nicht als Konzernkunde gekennzeichnet. Umsatz TRCH extern damit zu hoch; GFS wird neu als eigene Gesellschaft gezeigt. → G07“ (Zahlen hart codiert).
5. Raster g-2: `ch-month` „Umsatz je Monat 2026 vs. 2025“, Caption „Tsd. CHF · Nov./Dez. 2026 = statistische Prognose[ · **Vorjahr 2025 durch Datenfehler verzerrt** → G13 (nur TRUK)]“; zweite Karte:
   - TRCH: `ch-oirev` „Auftragseingang vs. Umsatz je Monat (Power BI)“, Caption „Mio CHF · TRCH inkl. IC-Kunden · Nov. 2026 (laufend: AE 0.4 / Umsatz 0.8) ausgeblendet“, Legende „Auftragseingang“ (s2), „Umsatz“ (s1); Tabelle Monat | AE | Umsatz | B2B.
   - sonst Karte „Einordnung“ / „Kennzahlen aus den verfügbaren Quellen“: Zeilen „Umsatz letzte 12 Monate“ (`fKM(r12)`), „Umsatz GJ 2025“ (`fKM(fy25)`), „Anteil am Gruppenumsatz YTD“ (`c.ytd / GRP.ytd`), „Bester Monat 2026 (Ist)“ / „Schwächster Monat 2026 (Ist)“ (aus `rev[12..21]`, „{Mon} · {fKM}“), optional „IC-Lieferungen TRCH → {code} (R12M, Power BI)“ „{fCHFM(ic.rev)} · DB1 x % · DB4 y %“; Notes: TRUK „Vorjahreswerte 2025 enthalten einen bekannten Datenfehler (Stückpreis statt Zeilenwert) – Wachstumsraten mit Vorsicht lesen. → G13“; TRDE „Kunden aus Alphaplan erscheinen teils nur als „TRDE#ALPHAPLAN-ID:…“ ohne Name/Land. → G12“; TRIN (mit ic) „TRCH liefert an TRIN mit DB4 −24.3 % (R12M) – die lokale Marge von TRIN ist ohne Gruppenmarge nicht aussagekräftig. → G05“.
6. Nur TRCH, Raster g-2: `ch-marg` „Marge DB1 & DB4 je Monat (Power BI)“, Caption „TRCH · gestrichelt = Budget (prelim.) · DB1 Juli bitte gegenprüfen“, Legende DB1 (s1, Linie), DB4 (s2, Linie), Tabelle Monat | DB1 | DB4; `ch-ka` „Umsatz je Key Account R12M (Power BI)“, Caption „Mio CHF · Konzernzuordnung KONZS“, Tabelle Name | „x.x Mio“.
7. **Abschnitt 2 „Marge · Gruppenmarge auf Materialnummer-Ebene“** – „Gruppenmarge = Endkundenpreis der Gesellschaft − TR AG Standardkosten (MBEW-STPRS) über die Trafag-Sachnummer. Finance-Jahr 2026, Jahreskurs.“ Kacheln:
   - mit `ex = gmEx(code)`: „Gruppenmarge“, Badge `gm.pct != null` ? „✓ vollständig“ : „ohne fehlende Zeilen“, Wert `fPctU(ex.pct)`, dl „{fCHFM(mChf)} auf {fCHFM(uOkChf)} Umsatz“, ft („Kostenbasis vollständig, Wert wie im internen BI“ | „Internes BI weist wegen {nMiss} Zeilen ohne Kosten keine Marge aus – diese Zeilen sind hier ausgeschlossen“) + „ · gerechnet in {cur}“. Dann „Umsatz ohne Kostenbasis“, Badge „⚠ {nMiss} Zeilen“ | „✓ keine“, `fCHF(uMissChf)`, dl „netto, {missShare} % des Umsatzes“, ft „Liste und Materialien unten → G05“ | „alle Zeilen mit Kostenbasis“.
   - ohne `ex`: Gap „Gruppenmarge“ / „Berechnung ausstehend“ / „{offen} Zeilen ohne Kostenbasis – Liste beim Erstellen nicht geladen.“ (G05).
   - „Umsatz & bekannte Kostenbasis“ (TRCH Badge „IC-Abgrenzung offen“): `fCHFM(gm.u)`, dl „Kostenbasis {fCHFM(gm.k)}“, ft „Finance-Jahr 2026, Jahreskurs · nicht direkt mit Verkaufsumsatz vergleichbar“.
   - „Datenlage Kostenbasis“, Badge „⚠ offen“ | „✓“: „{offen} Zeilen ohne Kosten“, dl „Typ Intern: … · Lokal: …“, ft „Saubere Kostenbasis {clean} % ({lines} Zeilen)“ | „Zeilen nach Typ gemäss internem BI“.
   - TRCH: „DB1 R12M (Power BI)“ `63.3 %`, dl „{fPP(db1d)} ggü. VJ · Budget 60 %“ (immer grün), ft „DB2 53.1 % (+4.4 Pp.)“; „DB4 R12M (Power BI)“ `23.7 %`, dl analog, ft „inkl. IC-Kunden → G07“. Sonst Gap „Deckungsbeitrag fix/variabel“ / „Kein Split in den Daten“ / „Internes BI: 0 Zeilen mit fix/variabel-Aufteilung. Ausgewiesen werden kann nur die Gruppenmarge auf Standardkosten.“ (G05).
8. Materialebene (genau ein Zweig):
   - TRAT: Karte „Top 10 Materialien nach Gruppenmarge (EUR)“, Caption „TRAT · alle 950 Detailzeilen FJ 2026 aggregiert · 259 Materialien · Top 20 = 67 % des Umsatzes“; Karte „Tiefste Gruppenmarge (Umsatz ≥ 15k EUR)“, Caption „Gleiche Typenreihe mit sehr unterschiedlicher Marge (8473: 5 % bis 78 %) – Preis vs. Kostenbasis prüfen“; Spalten Material | Bezeichnung | Umsatz EUR | Kostenbasis | Gruppenmarge | Marge % (Inline-Balken) | Zeilen | Kostenquelle (Badge „lokal“ gelb / „TR AG“ grün); Note „{negMats} Materialien mit negativer Gruppenmarge (zusammen {negM} EUR). Kostenquelle: TR AG-Konzernkosten {srcTRAG/u} des Umsatzes, lokaler/interner Standardpreis {srcLocal/u} – auf diesen Zeilen ist die Gruppenmarge nur bedingt aussagekräftig.“
   - TRCH: Karte „Marge je Produktkategorie R12M (Power BI)“ / „TRCH · inkl. IC-Kunden · Mio CHF“: Kategorie | AE | Δ AE | Umsatz | DB1 | DB4 (Balken, Skala 40 % = 100 %); Note „Accessories: DB4 nur {cats[2][5]} % bei {cats[2][2]} Mio Umsatz. Thermostat-AE {cats[4][6]}.“ (Indizes hart). Gap-Panel „GAP: Materialebene TRCH nicht dargestellt“ mit Text + „**Kunden mit DB4 < 5 % (R12M):** {accounts mit db4 < 5, ‚ (IC)‘}“.
   - `gm.prov` (TRUS): Gap-Panel „GAP: Materialebene {code} noch nicht aggregiert“ / „{lines} Detailzeilen, {mats} Materialien. Offen: {openN} Zeilen ({openU} {cur} Umsatz) – davon „Standardpreis fehlt“ {stdMissing}, „Lieferant unklar“ {supUnclear}. Top/Flop-Liste je Material analog TRAT nachziehen. → G06“.
   - sonst: Gap-Panel „GAP: Gruppenmarge je Materialnummer für {code} nicht verfügbar“ + „**Benötigt:** Aggregation Umsatz / TR AG-Kosten / Gruppenmarge je Materialnummer und Gesellschaft, mit Kennzeichnung der Kostenquelle (TR AG · lokal · offen).“
9. Nur TRCH, Raster g-2: `ch-db` „Deckungsbeitragsstufen R12M (Power BI)“, Caption „TRCH · in % vom Nettoumsatz {fCHFM(rev)} · CHF-Werte ≈ % × Umsatz · DB3 im Modell vorhanden, im Bericht nicht dargestellt“, Tabelle Stufe | % | ≈ Mio CHF; Karte „Einordnung DB-Stufen“ / „Power BI Sales Cockpit, inkl. IC-Kunden“ mit 3 Punkten (DB1→DB2 −Pp., DB2→DB4 −Pp.; „Budget (prelim.): DB1 60 % / DB4 10 % – beide übertroffen.“ (hart, nicht geprüft); CO-PA-Hinweis → G05).
10. Wenn `gm.offen > 0`: Karte „Zeilen ohne Kostenbasis · {code}“, Caption „Diese Zeilen sind aus der Gruppenmarge oben ausgeschlossen · CSV öffnet in Excel“, optional „Grösste Materialien ohne Kostenbasis (Σ |Umsatz|)“ + `missMatTable(code)`, dann `missList('miss-'+code, code)`.
11. **Abschnitt 3** Pipeline.
12. **Abschnitt 4 „Qualität · offene Q-Fälle“** – „SAP QM-Meldungen Q1 (Kundenreklamation) aus Power BI „RMA Statistic“, Stand {meta.stand}.“ `myCase`: TRCH = `co ∈ {'Endkunde (TRCH)', 'TRCH intern'}`, sonst `co === code`. Kacheln:
    - TRCH „Offene Q1-Meldungen mit Endkunden“ / sonst „Offene Q1-Meldungen (Gesellschaft als Kunde)“: `qn` (Fälle oder `endCustomers` bzw. `byCompany[code]`), dl „von {cs.length | totalCust} offenen Meldungen gesamt“, ft TRCH „Direktgeschäft & nicht zuordenbare Endkunden (inkl. intern Trafag AG)“ / sonst „Kunde in SAP QM: {companyLabel[code]}“.
    - mit eigenen Fällen: „Offen seit (Ø / Median)“ (dl „Median {med} Tage · Gruppe Ø {avg aller}“) und „Länger als 90 Tage offen“ (dl „ältester Fall {nr}“, ft „{age} Tage offen · {Status}“); ohne Liste: Gap „Offen seit“ (G08).
    - „Rücksendung offen (Gruppe)“: Fälle mit /Rücksendung/ bzw. `status[0][1]`; dl mit Liste „davon {code}: n“, ohne „{status[0][1]/totalStatus} % aller offenen Meldungen“; ft „Ware vom Kunden noch nicht zurück – Nachfassen durch Verkauf“.
    - Gap „Endkunden-Fälle im Markt“ / „Nicht zuordenbar“ / „QM-Meldungen tragen nur den Kunden, nicht die verantwortliche Gesellschaft – Fälle von Endkunden im Markt von {code} laufen unter TRCH.“ (G08).
13. Nur TRCH, Raster g-2: `ch-qend` „Offene Q1-Meldungen – grösste Endkunden“ / „Anzahl Meldungen“; `ch-qline` „Offene Q1-Meldungen nach Produktlinie (Gruppe)“ / „Anzahl Meldungen“.
14. Karte „Drilldown: offene Q1-Meldungen · {code}“, Caption „{TRCH: ‚Endkunden & intern‘ | ‚Meldungen mit {code} als Kunde‘} · Sortierung per Spaltenkopf · CSV öffnet in Excel“; Liste leer → „Keine offenen Meldungen mit {code} als Kunde.“, sonst `caseList('q-'+code, myCase)`.
15. **Abschnitt 5 „Kunden“** – „Top-Kunden mit Beteiligung der Gesellschaft, 12 Monate bis 10.2026.“ Karte „Top {n} Kunden · {code}“, Caption „Umsatz = Kundenumsatz über **alle** beteiligten Gesellschaften (nicht nur {code}) · Quelle: internes BI › Kunden“, `custTable(rows)` (Filter `r[3]` enthält `code`, Top 10 nach Umsatz); bei < 10 Note „Nur {n} Kunden von {code} sind unter den Top 200 der Gruppe.“ Dann drei Gap-Kacheln: „Neu- & Verlustkunden {code}“ / „Nur Gruppensicht“ (G11); „Umsatz je Marktsegment (KdGr)“ / TRCH „Im Modell, nicht dargestellt“ sonst „Nicht verfügbar“ (G10); „Kundenzufriedenheit“ / „Veraltet“ / „Letzte Aktualisierung des Customer Survey Report: 20.12.2025.“ (G16).
16. **Abschnitt 6** + `actionsTable(points(c, s, g, rows))`.

### 2.5 Ansicht `review` · TSC-Gesellschaft (`renderTsc(t)`)

Hilfsformate: `LM(v)` = „{v/1e6, 2 Dez.} Mio {cur}“; `LC(v)` = `LM` ab 1e6, sonst „{fInt} {cur}“; `sg(v)` = „+“ vor positivem `fKM(v)`; `fxTxt` = „1 {cur} = {fx} CHF“.

1. Datenquellen-Leiste.
2. Kopfkarte: „GESELLSCHAFT · TSC-DATEI“, „{lab} · {name}“, `legal`; „Lokalwährung {cur} · {fxTxt}“, „Verantwortlich {author} (TSC-Datei)“; „Berichtsmonat {meta.month}“, „Ist: Umsatz bis {mmYY(n)} · P&L bis {mmYY(pl.m)}“.
3. Warn-Note „**Nicht im internen BI.** {revDef} CHF zu Konstantkurs (Finance-Kurstabelle), Vorjahr gleiche Monate. Quelle: {files mit ' · '}. → G17“.
4. **Abschnitt 1 „Performance“** – „Bruttoumsatz Dritte · CHF zu Konstantkurs {fxTxt} · TSC-Budget linear je Monat.“ Kacheln:
   - „Umsatz {monthLong[n−1]} 2026“: `fKM(last)`, Delta `lastD` „ggü. {monthLong[n−1]} 2025“, Sparkline, ft „Vorjahresmonat {fKM(lastPy)} · {LC(m26[n−1])}“.
   - nur `n < 10`: Gap „Umsatz {monthShort}“ / „Noch nicht gemeldet“ / „TSC-Datei Stand {monthLong[n−1]} 2026 – Oktober ist dort noch Forecast ({fKM(f(m26[9]))}).“ (G17).
   - „Umsatz YTD {per(n)}“: `fMio(ytd)`, Delta `ytdD`, ft „Vorjahr {fMio(ytdPy)} · {LM(ytdL)} · 12 Monate {fKM(r12)}“.
   - „Budget-Erreichung YTD“, Badge „TSC-Budget linear“: `fPctU(budAch,0)`, dl „{arrow} {sg(f(ytdL − budYtdL))} ggü. Budget YTD“ (Farbe `cls(budAch−100)`), ft „Budget YTD {fKM(budYtd)} · Budget GJ {fKM(bud)} ({LM(t.bud)})“.
   - „Prognose EOY 2026“, Badge „TSC-Forecast“: `fKM(fy26e)`, Delta `fyD` „ggü. GJ 2025 ({fKM(fy25)})“, ft „{focBud} % vom Budget GJ · Ist {per(n)} + Forecast {monthLong[n]}–Dez. der Datei → G09“.
   - `t.oi` (TRJP): „Auftragseingang YTD“, Badge „Verkaufsblatt“, `fKM(f(oi.ytd))`, dl „Book-to-Bill {oi.ytd/oi.revYtd} (Trafag-Produkte)“, ft „01–10.2026 · Forecast GJ {fKM(f(oi.foc))} · nicht im BI → G02“; sonst Gap „Auftragseingang & Book-to-Bill“ / „Nicht auswertbar“ / `oiNote` (G02).
   - `t.ic` (TRCZ, GFS): „Intercompany-Umsatz YTD“, Badge „separat“, `fKM(f(ic.ytd))`, Delta `pct(ic.ytd, ic.pyYtd)`, ft „Konto 6010 · {per(ic.n)} · {ic.ytd/(ic.ytd + Σ m26[0..ic.n−1])} des Gesamtumsatzes · FOC {fKM(f(ic.foc))}“.
5. Raster g-2: `ch-month` „Umsatz je Monat 2026 vs. 2025“, Caption „Tsd. CHF zu Konstantkurs · {monthLong[n]}–Dez. 2026 = Forecast der TSC-Datei“, Legende „2026 Ist“, „2026 Forecast“, „2025 Ist“; Tabelle mit Badge „Forecast“ ab `i ≥ n`. Karte „Umsatz nach Konto“ (bzw. „Umsatz nach Gesellschaft & Konto“ wenn `t.parts`), Caption „Tsd. CHF zu Konstantkurs · Δ VJ = gleiche Monate Vorjahr“:
   - (toter Zweig `t.parts`: Tabelle Gesellschaft | YTD | Δ VJ | Budget YTD | FOC GJ | DB1 lokal | DB3 YTD + „Konten MS + MSA zusammen“).
   - Kontotabelle: Konto | YTD {per(n)} | Δ VJ | Budget GJ | FOC GJ. Δ: `l[2]` ? `delta(pct(l[1],l[2]))` : (`l[1]` ? „neu“ : „–“); 0-Werte bei Budget/FOC → „–“. Zusatzzeilen: „Intercompany (6010)“ + Badge „separat“ (YTD + Sub `per(ic.n)`, Δ, Budget, FOC); „Erlösminderungen (P&L)“ + Badge „nicht abgezogen“ (YTD + Sub `per(ded.n)`, Rest „–“).
   - TRCZ-Note „TRCZ ist vor allem Produktionsstandort: {ic.ytd/(ic.ytd+ytdL)} des Umsatzes sind Lieferungen an die Gruppe (6010). Für die Sales-Steuerung zählt der Drittumsatz.“
   - Kennzahlentabelle: „Umsatz letzte 12 Monate“, „Umsatz GJ 2025“ (`fKM(fy25)` · `LM(fy25L)`), „Budget GJ 2026 · Forecast GJ 2026“, „Bester / schwächster Monat 2026 (Ist)“.
   - `t.trchIc` (GFS): Warn-Note „TRCH liefert an GFS (Kunde „GFS Germany“, 12M {fKM(k12)}, Power BI R12M DB4 {pbiDb4} %). Im TRCH-Umsatz als Drittumsatz gezählt – in einer Gruppensumme inkl. GFS doppelt. → G07“.
6. **Abschnitt 2 „Marge · lokale P&L“** – „TSC-P&L in {cur} (Ist {per(pl.m)}, inkl. Intercompany) · Gruppenmarge auf TR AG-Kosten nicht verfügbar.“ Kacheln:
   - „DB1 lokal YTD“, Badge „P&L bis {mmYY(pl.m)}“, `fPctU(db1)`, dl „{arrow} {fPP(db1 − db1Bud)} ggü. Budget {db1Bud} %“, ft „DB1 {fKM(f(db1.ytd))} auf Nettoumsatz {fKM(f(net.ytd))} · Vorjahr {db1Py} %“.
   - „DB3 YTD (nach Personalkosten)“: `fKM(f(db3.ytd))`, dl „{arrow} {sg(f(d3))} ggü. Budget YTD“, ft „Budget YTD … · Vorjahr … · {LM(db3.ytd)}“.
   - Gap „Gruppenmarge (TR AG-Kosten)“ / „Nicht verfügbar“ (G05); Gap „Deckungsbeitrag fix/variabel“ / „Kein Split“ (G05).
   - Karte „P&L-Stufen YTD {per(pl.m)}“, Caption „Tsd. CHF zu Konstantkurs · % vom Nettoumsatz inkl. IC · Quelle TSC-P&L“: Stufe | Ist YTD | % Umsatz | Budget YTD | % Umsatz | Vorjahr YTD | % Umsatz | Ist − Budget (rot bei < 0). Zeilen „Nettoumsatz (inkl. IC)“/„Nettoumsatz“, DB1, DB2, DB3. TRJP-Warn-Note (Verrechnungspreis, → G05); TRCZ-Note „DB-Werte gelten für den gesamten Umsatz inkl. Produktion für die Gruppe und sind für den Drittumsatz nicht aussagekräftig.“
7. **Abschnitt 3** Pipeline (mit `lab(t)`).
8. **Abschnitt 4 „Qualität · offene Q-Fälle“** – „SAP QM-Meldungen Q1 aus Power BI „RMA Statistic“ (System TRCH), Stand {meta.stand}.“
   - `q.kind === 'cust'` (TRJP): „Offene Q1-Meldungen (Gesellschaft als Kunde)“ = Fälle mit `co === code` oder `q.n`; dl „von {…} offenen Meldungen gesamt“; ft `q.label`; mit Fällen „Offen seit (Ø / Median)“, ohne Liste Gap „Offen seit“. Danach Karte „Drilldown: offene Q1-Meldungen · {lab}“ / „Meldungen mit {legal} als Kunde · CSV öffnet in Excel“ + `caseList`.
   - `q.kind === 'line'` (TRCZ): „Offene Q1-Meldungen Produktlinie“ `q.n`, dl „von {totalStatus} offenen Meldungen gesamt“, ft `q.label`; Gap „Fälle mit Kunden von {lab}“ / „Nicht zuordenbar“ (G08).
   - `q == null`: Gap „Q-Fälle {lab}“ / „Keine Zuordnung“ / „In RMA Statistic (SAP QM TRCH) weder als Kunde noch als Produktlinie geführt; ob ein eigenes QM-System ausgewertet werden kann: Details tbd.“ (G08).
9. **Abschnitt 5 „Kunden“** – Untertitel `acc.length` ? „Verkaufsblatt der TSC-Datei · Umsatz 01–10.2026 (Trafag-Produkte) · Tsd. CHF zu Konstantkurs.“ : „Keine Kundendaten in der TSC-Datei.“
   - Mit `acc`: Karte „Top {n} Kunden · {lab}“, Caption „Anteil am Umsatz des Verkaufsblatts{ · Ist vs. Budget YTD = Umsatz ÷ (Budget GJ × 10/12) − 1 | · kein Budget je Kunde gepflegt}“; Spalten Kunde | Branche | Umsatz YTD | Anteil (Balken) | [Budget GJ | Ist vs. Budget YTD] | [AE YTD] | Forecast GJ; `top` = Kunden mit Umsatz > 0, max. 12. `accCover` → Warn-Note „Verkaufsblatt unvollständig: Kundenumsatz {…} vs. {…} Trafag-Produkte in der P&L (6005).“ Raster g-2: bei ≥ 2 Branchen `ch-ind` „Umsatz nach Branche (Verkaufsblatt)“ / „Tsd. CHF · Branche frei gepflegt, keine KdGr“; Karte „Einordnung Kunden“ / „aus dem Verkaufsblatt abgeleitet“: Kundenkonzentration, Kunden > 40 % unter Budget (max. 4, nach Fehlbetrag), budgetierte Kunden ohne Umsatz (max. 5 + „ …“), „Keine Auftragseingänge je Kunde gepflegt.“.
   - Ohne `acc`: Gap „Top-Kunden {lab}“ / „Keine Kundendaten“ (G17).
   - Danach Gap-Kacheln „Neu- & Verlustkunden {lab}“ / „Nicht verfügbar“ (G11) und „Umsatz je Marktsegment (KdGr)“ / „Nur Branche (frei)“ bzw. „Nicht verfügbar“ (G10).
10. **Abschnitt 6** + `actionsTable(tscPoints(t, s))`.

### 2.6 Ansicht `gaps` (`renderGaps`)

1. Kacheln: „Gaps gesamt“ (18, ft „davon Datenqualität/Darstellung: {Anzahl status ~ /Daten|Darst/}“ = 3), „Priorität 1“ (Badge P1 rot, ft „blockiert Kern-KPI oder erzeugt falsche Zahlen“), „Priorität 2“ (Badge P2 gelb, „wichtig für Diagnose & Massnahmen“), „Priorität 3“ (Badge P3 grau, „Ergänzung / Hygiene“).
2. Kopf „Gap-Register“, Text „Abhängigkeiten in Reihenfolge Datenmodell › Prozess › Schnittstelle › Reporting · Owner & Termine bewusst offen (tbd.)“.
3. Filter-Chips (radiogroup „Priorität filtern“): „Alle“, „P1“, „P2“, „P3“ → `gapFilter`, nur `renderGaps()` neu.
4. Tabelle: ID | Prio | Bereich | KPI / Information (fett + Sub `src`) | Status | Abhängigkeiten (`chain` + Sub nicht-leere Texte mit „ · “) | Auswirkung | Nächster Schritt | Owner („tbd.“) | Termin („tbd.“). Zeilen-ID `gap-{id}` (Sprungziel).
5. Raster g-2: Karte „Empfohlene Reihenfolge (Abhängigkeiten)“ (4 Punkte, Text Abschnitt 5; einziger dynamischer Teil: TSC-Liste in G17-Satz); Karte „Legende“ (Kette mit ok/part/miss/tbd; „✓ vorhanden · ~ teilweise · ✕ fehlt · ? zu klären · – nicht relevant“; „P1 blockiert Kern-KPI / falsche Zahlen · P2 Diagnose · P3 Ergänzung“).
6. Warn-Note „**Prüfhinweise (abgeleitete oder unsichere Werte):**“ + Liste `DATA.checks`.

### 2.7 Ansicht `pbi` (`renderPbi`)

1. Kacheln: „Bericht“ / „Trafag Sales Cockpit“ / ft „Workspace „Trafag Sales Management Workspace“ · Modell-Owner Constantin Koch · tägliche Aktualisierung 01:00“ (hart); „Abdeckung“ (Badge „nur TRCH“) / „SAP S/4HANA TRCH“ / „keine Gesellschafts-Dimension; Tochtergesellschaften nur als IC-Kunden“; „Felder im Modell, nicht dargestellt“ / „{notShown.length} Gruppen“ / „Quick Wins ohne neue Datenquelle“; Gap „Nicht im Modell“ / „{notInModel.length} Informationsbereiche“ / „u. a. Budget, IC-Kennzeichen, CRM, Q-Fälle, Gruppenmarge“ (G01).
2. Kopf „Was der Bericht heute zeigt“, Text „Filter: {filters} · {pbi.period}“; Tabelle Seite | Inhalte; Note „Kopfzahlen R12M: Auftragseingang {fCHFM(oi)} ({fPct(oiYoY)}), Umsatz {fCHFM(rev)} ({fPct(revYoY)}), DB1 … (…), DB2 …, DB4 ….“
3. Raster g-2: „Im semantischen Modell vorhanden – nicht dargestellt“ / „Mit vorhandenen Daten umsetzbar“, Badge „Quick Wins“: Feld / Kennzahl | Tabelle | Nutzen im Monthly | Gap; „Im Modell nicht vorhanden“ / „Benötigt neue Quelle oder Modellerweiterung“, Badge „⚠ Gaps“: Information | Folge | Gap (mehrere über „ · “ getrennt → mehrere Links).
4. Raster g-2: „Darstellung & Methodik“ (Liste `issues`); „Weitere Berichte im Umfeld“: Bericht | Inhalt | Stand + Note „Das alte Modell „Trafag Sales Management Report“ enthält Budget, Forecast (FOC EOY), Upside/Downside und Sales Channel auf Kundenebene – diese Logik fehlt im neuen Sales Cockpit. → G01 → G09“.
5. Karte „Accounts R12M mit Margen (Power BI, TRCH)“ / „IC-Kunden markiert · DB4 < 5 % hervorgehoben“: Account (+ Badge „IC“ wenn `a[7]`, sonst bei Name „GFS GERMANY“ Badge „IC, nicht gekennzeichnet“) | Land | AE (`fCHFM`) | Δ AE | Umsatz | DB1 | DB4 (rot + „ ▼“ wenn < 5).

---

## 3. Berechnungen

### 3.1 Formatierung (für C#: Culture „de-CH“, Gruppentrenner U+2019 „’“, Dezimalpunkt)

```js
NF(d)  = Intl.NumberFormat('de-CH', {min/maxFractionDigits: d})
minus(s) = führendes '-' und ' -' → '−' (U+2212)
fInt(v)  = minus(nf0(round(v)))
fMio(v,d)= minus(nf2(v)) + ' Mio'            // v bereits in Mio; d===1 → nf1
fKM(k)   = |k| >= 1000 ? minus(nf2(k/1000)) + ' Mio' : minus(nf0(round(k))) + 'k'   // k in Tsd.
fCHFM(v) = minus(nf2(v/1e6)) + ' Mio'        // v in CHF
fCHF(v)  = |v| < 0.5 ? '0' : |v| >= 1e6 ? fCHFM(v) : fKM(v/1000)
fPct(v,d)= (v > 0 ? '+' : '') + minus(nf1 bzw. nf0 (d===0)) + ' %'
fPctU(v,d)= ohne '+'
fPP(v)   = (v > 0 ? '+' : '') + minus(nf1(v)) + ' Pp.'
pct(a,b) = b ? (a/b − 1)*100 : null
cls(v)   = null → 'neu'; v > 0.05 → 'up' (grün); v < −0.05 → 'dn' (rot); sonst 'neu'
arrow(v) = '▲' / '▼' / '●' (gleiche Schwellen), null → ''
delta(v,label,d) = null → '–' ; sonst <span cls>{arrow} {fPct(v,d)}</span> + optional Label grau
fDate('JJJJ-MM-TT') = 'TT.MM.JJJJ', leer → '–'
mmYY(n) = 'NN.2026';  per(n) = '01–NN.2026'
monthNames = Jan Feb Mär Apr Mai Jun Jul Aug Sep Okt Nov Dez
monthLong  = Jan. Feb. März Apr. Mai Juni Juli Aug. Sep. Okt. Nov. Dez.
```

### 3.2 `stats(c)` (BI-Gesellschaften und GRP; `rev` in Tsd. CHF)

```js
ytd   = sum(r,12,21)       // nur für fy26e benutzt
fy25  = sum(r,0,11);  r12 = sum(r,10,21)      // 11.2025–10.2026
fy26e = ytd + r[22] + r[23]                   // Prognose EOY = Ist Jan–Okt + stat. Prognose Nov–Dez
oct = r[21]; octPy = r[9]; octD = pct(r[21], r[9])
ytdD = pct(c.ytd, c.ytdPy)                    // aus den gerundeten Mio-Feldern, nicht aus rev
fyD  = pct(fy26e, fy25)
spark = r.slice(10, 22)                       // 12 Werte 11.2025–10.2026
```
Delta „Prognose nächste 12 Monate“: `pct(c.fc12 * 1000, s.r12)`.
Anteil am Gruppenumsatz YTD: `c.ytd / CO.GRP.ytd * 100`.
Bester/schwächster Monat: `argmax/argmin(rev[12..21])` (bei Gleichstand erster).

### 3.3 `tscStats(t)` (LC → Tsd. CHF über `kCHF(t,v) = v * t.fx / 1000`)

```js
n = t.lastM
ytdL = sum(m26,0,n-1); pyL = sum(m25,0,n-1); fy25L = sum(m25,0,11); budYtdL = budM * n
rev = m25.concat(m26).map(f)                  // 24 Werte Tsd. CHF
ytd = f(ytdL)/1000 (Mio); ytdPy = f(pyL)/1000; ytdD = pct(ytdL, pyL)     // in LC → währungsbereinigt
last = rev[11+n]; lastPy = rev[n-1]; lastD = pct(m26[n-1], m25[n-1])
oct = n>=10 ? rev[21] : null; octPy = rev[9]; octD = n>=10 ? pct(m26[9], m25[9]) : null
fy25 = f(fy25L); fy26e = f(t.foc); fyD = pct(t.foc, fy25L)
budAch = ytdL / budYtdL * 100; budYtd = f(budYtdL); bud = f(t.bud); focBud = t.foc / t.bud * 100
spark = rev.slice(n, n+12); r12 = sum(rev, n, n+11)
db1 = pl.db1.ytd / pl.net.ytd * 100; db1Bud = pl.db1.budYtd / pl.net.budYtd * 100; db1Py = pl.db1.pyYtd / pl.net.pyYtd * 100
```
Gruppe: `tY = Σ ytd`, `tP = Σ ytdPy` über alle TSC (gemischte Perioden 01–09/01–10), Delta `pct(tY,tP)`, „Gruppe inkl. ≈ c.ytd + tY“.
IC-Anteil: `ic.ytd / (ic.ytd + Σ m26[0..ic.n−1]) * 100`. TRCZ-Note: `ic.ytd / (ic.ytd + ytdL)`.
Book-to-Bill TRJP: `oi.ytd / oi.revYtd`. Book-to-Bill TRCH: `pbi.oi / pbi.rev`, Monat `oiM[i] / revM[i]`.
P&L-Tabelle: `% Umsatz = v / net (gleiche Spalte) * 100`, „Ist − Budget“ `sg(f(v.ytd − v.budYtd))`.
Kunden (TSC): `tot = Σ acc[2]`; Anteil `acc[2]/tot`; `vb = acc[3] ? (acc[2] / (acc[3]*10/12) − 1)*100 : null`;
`gapsB` = Kunden mit Budget > 0, Umsatz > 0 und `acc[2]/(acc[3]*10/12) < 0.6`, sortiert nach Fehlbetrag `acc[3]*10/12 − acc[2]` absteigend, max. 4;
`noRev` = Umsatz 0 und Budget > 0, nach Budget absteigend; `sh1 = top[0]/tot`, `sh3 = Σ top[0..2]/tot`.
Branchen `tscInd(t)`: Σ `acc[2]` je `acc[1]` (leer → „ohne Branche“), nur `acc[2] > 0`, absteigend.

### 3.4 Gruppenmarge ohne Zeilen ohne Kostenbasis (`gmEx`, `gmExSum`)

```js
gmEx(code):
  L = gmLocal[code]; own = gmMissing?.rows.filter(r => r.co === code) ?? null; agg = gmMissingAgg[code]
  wenn !L oder (!own && !agg) → null
  uMissLoc = own ? Σ own.uLoc : agg[1];  nMiss = own ? own.length : agg[0]
  uOk = L.u − uMissLoc;  m = uOk − L.k;  pct = m / uOk * 100
  CHF-Werte = LC * L.fx (uChf, uMissChf, uOkChf, kChf, mChf);  missShare = uMissLoc / L.u * 100
gmExSum(codes): null, sobald ein Teil null; sonst Σ uOkChf, mChf, uMissChf, uChf, nMiss; pct = m/uOk*100; missShare = uMiss/u*100
SUBS = TRIN, TRIT, TRUS, TRUK, TRES, TRAT, TRDE, TRFR   // TRCH bewusst separat
```
Hinweis: Die Kosten `L.k` werden **nicht** um die Kosten der ausgeschlossenen Zeilen reduziert (Zeilen ohne Kostenbasis haben per Definition keine Kosten).
Gruppe „Umsatz ohne Kostenbasis“ (Liste geladen): `Σ rows.uChf / Σ_{SUBS+TRCH} gmEx(k).uChf * 100`.
`missByMaterial(code, 12)`: gruppiert nach `co|mat`, `n`, `u = Σ uChf`, `abs = Σ |uChf|`, Statusmenge; sortiert nach `abs` absteigend. Tabelle `missMatTable`: [Ges.] | Material | Bezeichnung | Status | Zeilen | Umsatz CHF (netto) | Σ |Umsatz|.
Scorecard vorläufige Marge TRUS: `prov.m / prov.u * 100`.
TRAT-Materialtabelle: Marge % = `r[4]/r[2]*100`, Balken `clamp(…,1,100)`; Note-Anteile `srcTRAG/u`, `srcLocal/u`.
TRCH-Kategorien: AE/Umsatz `/1e6` (nf2), DB4-Balken `max(1, db4/40*100)` %.
DB-Stufen TRCH: `≈ Mio CHF = db/100 * pbi.rev / 1e6`.

### 3.5 RMA: `cases()`, `ageBuckets()`, `caseStats()`

```js
caseCo(kunde): erster Präfix-Treffer (lowercase, startsWith) in
  'trafag controls india'→TRIN, 'trafag gesellschaft'→TRAT, 'trafag (uk)'→TRUK, 'trafag s.a.r.l'→TRFR,
  'trafag italia'→TRIT, 'trafag inc'→TRUS, 'trafag españa'/'trafag espana'→TRES, 'trafag japan'→TRJP,
  'trafag ag'→'TRCH intern', 'trafag gmbh'→TRDE;  sonst 'Endkunde (TRCH)'
ref = Date(rmaCases.stand)
age = round((ref − ang)/Tag); rlAge = round((ref − rl)/Tag); toMass = round((mass − ang)/Tag)   // null wenn Datum fehlt
ageBuckets: ≤ 30 Tage | 31–60 Tage | 61–90 Tage | 91–180 Tage | > 180 Tage  (Fälle ohne age nicht gezählt)
caseStats: n = Anzahl; avg = Ø age; med = Median age (gerade Anzahl: Mittel der zwei mittleren); max;
           over90 = Anzahl age > 90; oldest = Fall mit grösstem age
```
Gruppe ohne Fälle: Gesellschafts-Chart = `byCompany` (> 0, absteigend) + `otherCompanies`; mit Fällen = Zählung nach `co`. Statusreihenfolge mit Fällen = nach Anzahl absteigend.

### 3.6 Gap-Prioritäten

Keine Berechnung; Prio ist Datenfeld. Zähler je Prio; „Datenqualität/Darstellung“ = Anzahl `status` mit Regex `/Daten|Darst/`. Filter `gapFilter ∈ all|1|2|3` vergleicht `String(g.prio)`.

### 3.7 Diskussionspunkte

Jeder Punkt = `[Text (HTML), GapId | '']`; Reihenfolge = Reihenfolge der Regeln.

**`points(c, s, g, rows)` (BI-Gesellschaft)** – `rows` = Top-10-Kunden der Gesellschaft:
1. `octD ≤ −15` → „Umsatz {monthShort} {fPct(octD)} ggü. Vorjahresmonat – Ursachen (Timing, Kunden, Preis) klären.“ (–)
2. `ytdD ≥ 25` → „Wachstum YTD {fPct} – Treiber und Nachhaltigkeit (Projekte vs. Run-Rate) einordnen.“ (G09)
3. `ytdD < 5` → „Wachstum YTD nur {fPct} (Gruppe {fPct(GRP)}) – Massnahmen zur Marktbearbeitung besprechen.“ (–)
4. TRAT → „Materialien 63503 / 64494 (8473) mit 5–7 % Gruppenmarge bei zusammen {fKM((182115+174004)/1000)} EUR Umsatz: Preis vs. Kostenbasis prüfen.“ (G06) – **hart codiert**
5. `ex && ex.nMiss` → „Gruppenmarge {pct} ohne {nMiss} Zeilen ohne Kostenbasis ({fCHF(uMissChf)} Umsatz netto) – fehlende Standardpreise/Lieferanten bereinigen.“ (G05); sonst wenn `!ex && g.offen` → „Gruppenmarge nicht belastbar: {offen} Zeilen ohne Kostenbasis[, {lokal} mit lokalem Standardpreis] – Bereinigung und Verantwortung klären.“ (G05)
6. nicht TRAT/TRCH → „Gruppenmarge je Materialnummer für {code} auswerten (Top/Flop analog TRAT).“ (G06)
7. TRCH → „Kunden mit DB4 < 5 % (Beijing Macrocontrol, Ecolab, GFS Germany) und IC-Lieferungen an TRIN mit negativem DB4 besprechen.“ (G07) – **hart**
8. TRCH → „„GFS Germany“ (12M {fKM(1740)}) im Kundenstamm als Konzernkunde kennzeichnen – heute als Drittumsatz gezählt.“ (G07) – **hart**
9. TRIN → „IC-Lieferungen TRCH → TRIN mit DB4 {pbi.ic.TRIN.db4} % (R12M): Verrechnungspreis und Gruppenmarge gemeinsam betrachten.“ (G05)
10. `qn > 0` (Fälle bzw. `endCustomers`/`byCompany`) → „{qn} offene Q1-Meldungen {mit Endkunden | mit {code} als Kunde}[, Ø {avg} Tage offen[, {over90} länger als 90 Tage (ältester {nr}, {age} Tage)]] – Status und Rücksendungen nachfassen.“ (G08)
11. Kunden mit `−100 < Δ ≤ −30` (max. 3) → „Rückgang bei {Name (fPct,0)}, … – Hintergrund klären.“ (–)
12. Kunden mit `Δ ≤ −100` → „Auffälliger Vorjahreswert bei {Namen} – Datenqualität prüfen.“ (G12)
13. TRUK → „Vorjahresbasis 2025 durch Datenfehler verzerrt – bereinigten Vergleich vereinbaren.“ (G13)
14. TRDE → „Kundenstamm Alphaplan ohne Namen/Land – Mapping klären.“ (G12)
15. immer → „Pipeline-Review nicht möglich: Opportunities/Leads fehlen im Reporting.“ (G03)
16. immer → „Budget/Ziel {GRP ? 'je Gesellschaft' : code} für den Soll/Ist-Vergleich festlegen bzw. bereitstellen.“ (G01)

**`tscPoints(t, s)`**:
1. `ytdD ≥ 25` → „Wachstum YTD {fPct} (währungsbereinigt) – Treiber und Nachhaltigkeit einordnen.“ (–)
2. `budAch < 95` → „Budget-Erreichung YTD {budAch} % – Lücke {fKM(f(budYtdL − ytdL))}; Massnahmen bis Jahresende festlegen.“ (G01)
3. `budAch > 110` → „Budget YTD um {budAch − 100} % übertroffen – Forecast und Budget 2027 daran ausrichten.“ (G09)
4. je Konto `l`: `l[2] && l[1]/ytdL ≥ 0.25 && pct(l[1],l[2]) ≤ −10` → „{Konto}: {fPct} YTD ggü. VJ[ – Gesamtwachstum kommt aus anderen Produktlinien (wenn ytdD > 0)].“ (–)
5. `restM = 12 − n; rest = foc − ytdL; rr = ytdL/n`; `restM > 0 && rest/restM > rr*1.3` → „Forecast GJ verlangt {fKM(f(rest))} in {restM} Monaten (Ø {…}/Monat vs. Ø {…} bisher) – Plausibilität prüfen.“ (G09)
6. `db1 < db1Bud − 3` → „DB1 lokal {db1} vs. Budget {db1Bud} ({fPP}){TRJP: ' – Verrechnungspreis TRCH → TRJP und Gruppenmarge gemeinsam betrachten.' | ' – Ursachen klären.'}“ (G05)
7. `db1 > db1Bud + 15` → „DB1 lokal {db1} deutlich über Budget {db1Bud} ({fPP}) – Abgrenzung (Bestandsveränderung, Kostenzuordnung) prüfen.“ (–)
8. `pl.db3.ytd < 0` → „DB3 YTD negativ ({fKM}) – Kostenbasis vs. Umsatzniveau besprechen.“ (–)
9. mit `acc`: `acc[0][2]/tot > 0.5` → „Kundenkonzentration: {Name} = {…} % des Kundenumsatzes YTD im Verkaufsblatt.“ (–); `noRev.length` → „{n} budgetierte Kunden ohne Umsatz YTD (Σ Budget {…}) – Status der Akquise klären.“ (G03)
10. (toter Zweig `t.parts`: je Teil Budget-Erreichung < 95 → Punkt, G01)
11. `n < 10` → „Oktober-Ist in der TSC-Datei noch nicht erfasst (Stand {monthLong[n−1]} 2026).“ (G17)
12. GFS → „TRCH-Umsatz mit „GFS Germany“ (12M {fKM(k12)}, DB4 {pbiDb4} %) als IC kennzeichnen und Verrechnungspreis prüfen.“ (G07)
13. TRCZ → „11 offene Q1-Meldungen zur Produktlinie „Trafag CZ“ – Ursachen und Rückmeldungen mit der Produktion klären.“ (G08) – **Zahl hart**
14. TRJP → „5 offene Q1-Meldungen mit Trafag Japan als Kunde – Status nachfassen.“ (G08) – **Zahl hart**
15. immer → „Pipeline-Review nicht möglich: Opportunities/Leads fehlen im Reporting.“ (G03)
16. immer → „Zahlen kommen aus Excel – Anbindung ans BI (Datenmodell, Schnittstelle) klären.“ (G17)

**Gruppe `gp`** (`renderGroup`):
1. „Gruppe YTD {fPct(ytdD)}, Oktober {fPct(octD)} – Rückgänge im Oktober bei {Gesellschaften mit octD ≤ −15: ‚CODE (fPct)‘}.“ (–) – bei leerer Menge entsteht „… bei .“
2. „Kundenbasis: 2026 Q1 mehr verlorene ({churn[3][2]}) als neue Kunden ({churn[3][1]}).“ (G11) – Index und Aussage hart, keine Bedingung
3. „Gruppenmarge nicht belastbar – offene Kostenbasis vor allem TRDE (970), TRCH (598), TRFR (572), TRES (367).“ (G05) – Auswahl hart, Werte aus `gm`
4. „{status[0][1]} von {totalStatus} offenen Q1-Meldungen warten auf Rücksendung des Kunden.“ (G08)
5. „Budget, Auftragseingang und Pipeline je Gesellschaft fehlen – Steuerung erfolgt heute nur über Ist-Umsatz.“ (G01)
6. „Power BI Sales Cockpit enthält IC-Kunden und keine Gesellschafts-Dimension – für das Monthly nur als TRCH-Sicht nutzbar.“ (G07)
7. (wenn TSC) „TSC-Gesellschaften ausserhalb des internen BI: {lab fPct(ytdD,0) (Budget budAch %)}, … – zusammen {fMio(tY)} YTD, nicht im Gruppentotal.“ (G17)
8. (wenn TSC) „Kunde „GFS Germany“ im TRCH-Umsatz (12M {fKM(1740)}) ist die Konzerngesellschaft GfS – als IC kennzeichnen, sonst Doppelzählung in einer Gruppensumme inkl. GFS.“ (G07)

### 3.8 `chain()` – siehe 2.1. Status → Symbol/Klasse: `ok ✓`, `part ~`, `miss ✕`, `tbd ?`, `na –`; Stufennamen fest.

### 3.9 `checks` – reine Textliste (keine Logik).

### 3.10 Erwartungswerte (aus dem Snapshot gerechnet, für Tests)

BI-Gesellschaften (`rev` Tsd. CHF):

| Code | Okt. 26 | Δ Okt. % | Δ YTD % | Progn. EOY | GJ 25 | Δ EOY % | R12M | Δ fc12 vs R12M % |
|---|---|---|---|---|---|---|---|---|
| GRP | 11 564 | +7.4 | +19.9 | 119 189 | 99 410 | +19.9 | 116 194 | +19.9 |
| TRCH | 8 184 | +11.9 | +19.9 | 79 996 | 66 735 | +19.9 | 78 181 | +19.9 |
| TRIN | 766 | +18.4 | +30.2 | 9 661 | 7 423 | +30.1 | 9 103 | +30.2 |
| TRIT | 628 | −22.6 | +7.2 | 7 555 | 7 054 | +7.1 | 7 479 | +7.1 |
| TRUS | 456 | +20.6 | +50.0 | 4 790 | 3 188 | +50.3 | 4 474 | +50.2 |
| TRUK | 100 | −64.4 | +6.5 | 4 250 | 3 987 | +6.6 | 4 211 | +6.6 |
| TRES | 534 | +93.5 | +50.7 | 4 396 | 2 914 | +50.9 | 4 058 | +50.8 |
| TRAT | 400 | −32.5 | +12.2 | 3 659 | 3 264 | +12.1 | 3 612 | +12.1 |
| TRDE | 341 | +4.3 | +7.1 | 3 718 | 3 471 | +7.1 | 3 681 | +7.0 |
| TRFR | 154 | +10.0 | +1.7 | 1 402 | 1 373 | +2.1 | 1 398 | +2.3 |

TSC-Gesellschaften:

| Code | n | Letzter Monat Tsd. CHF | Δ % | YTD Mio | VJ Mio | Δ YTD % | Budget-Erreichung % | Progn. EOY Tsd. | Δ EOY % | FOC/Budget % | DB1 % | DB1 Bud % | DB1 VJ % | R12M Tsd. |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TRJP | 10 | 97 | +23.4 | 0.917 | 0.777 | +18.0 | 87.0 | 1 111 | +13.0 | 87.9 | 5.7 | 17.1 | 2.1 | 1 124 |
| TRCZ | 10 | 19 | −17.6 | 0.140 | 0.126 | +10.6 | 107.7 | 164 | +4.7 | 105.1 | 80.3 | 82.1 | 85.4 | 170 |
| MS | 9 | 290 | +55.2 | 1.210 | 1.202 | +0.6 | 89.0 | 1 990 | +14.4 | 109.8 | 75.7 | 41.1 | 51.4 | 1 747 |
| MSA | 9 | 156 | +3.0 | 1.411 | 1.111 | +27.0 | 85.3 | 2 486 | +83.0 | 112.7 | 40.9 | 58.9 | 30.2 | 1 659 |
| GFS | 10 | 528 | +19.5 | 4.463 | 3.340 | +33.6 | 123.4 | 5 212 | +27.2 | 120.1 | 58.5 | 59.1 | 58.7 | 5 221 |

TSC gesamt: `tY = 8.141` Mio, `tP = 6.557` Mio, Δ +24.2 %, „Gruppe inkl. ≈ 109.28 Mio“.
`gmEx`: TRAT pct 47.10 %, mChf 1 544 879, uOkChf 3 280 090, nMiss 0; TRUS pct 56.40 %, mChf 2 179 208, uOkChf 3 863 724, uMissChf 29 068, missShare 0.75 %.

---

## 4. Diagramme (alle als inline-SVG, Breite = Containerbreite, min. 280 px, Fallback 600)

Gemeinsam: `niceScale(v, maxTicks=5)`: `p = 10^floor(log10(v/maxTicks))`, erster Schritt aus `m·p` mit `m ∈ [1,2,2.5,5,10,20,25,50]`, für den `ceil(v/step) ≤ maxTicks`; Rückgabe `{ max: ceil(v/step)*step, step }`; `v ≤ 0` → `{1, 0.25}`. `tickK(v, sc)`: 0 → „0“; `sc.max ≥ 1000` → „{v/1000} Mio“ mit 0/1/2 Dezimalen je nach `step/1000`; sonst „{v}k“. Achsentexte 11 px, Farbe `--muted`; `.lbl-strong` = `--ink` fett; `.lbl-2` = `--ink-2`. Gitterlinien `--grid`, Nulllinie `--axis`. Jede Kategorie hat ein transparentes Hit-Rechteck mit `data-tip`, `tabindex=0`, `aria-label`.

| Chart | Typ | Maße / Ränder | Skala | Serien → Farbe | Labels | Tooltip |
|---|---|---|---|---|---|---|
| `chartMonthly(host, c, {nA=10, fcName='Prognose'})` | Säulen 2026 + Linie 2025 | H 250; ml 46, mr 12, mt 18, mb 26; band = pw/12; Balkenbreite `min(24, band·0.56)` | `vMax=max(0,cy,py)`, `vMin=min(0,…)`; `step = niceScale((vMax−vMin)·1.08).step`; `lo = vMin<0 ? −ceil(−vMin/step)·step : 0`; `max = max(step, ceil(vMax·1.08/step)·step)`; Ticks von lo bis max | 2026 Ist (`i < nA`) `--s1`; 2026 Prognose/Forecast `--s1-fc`; 2025 Linie `--deemph` 2 px + Punkte r 3 (Rand `--card` 2 px). Negative Werte nach unten (roundBottom), Radius 4 | Monatskürzel unter jeder Säule, bei `band < 24` nur gerade Indizes; Datenlabel `fKM(cy[nA−1])` über Säule `nA−1` | Titel „{Mon} 2026“ bzw. „{Mon} 2026 · {fcName}“; Zeilen „2026 Ist“/„2026 {fcName}“, „2025 Ist“, „Δ ggü. VJ“ (`fPct(pct(v,p))`) |
| `chartOiRev(host)` (TRCH) | gruppierte Säulen | H 240; ml 40, mr 10, mt 16, mb 26; band = pw/11; bw `min(16,(band−10)/2)` | `niceScale(max(oiM,revM)·1.05)`; Achse „{n} Mio“ | AE links `--s2`; Umsatz rechts `--s1`; Radius 4 | erstes Wort von `mLabels` („Dez“, „Jan“ …), bei `band < 30` nur gerade | Titel `mLabels[i]`; „Auftragseingang x.x Mio“, „Umsatz x.x Mio“, „Book-to-Bill x.xx“ |
| `chartMargins(host)` (TRCH) | 2 Linien + 2 Referenzlinien | H 240; ml 40, mr 76, mt 14, mb 26; x = `ml + step·i`, `step = pw/(n−1)` | fix 0–80 %, Ticks 0/20/40/60/80 „x %“ | DB1 `--s1`, DB4 `--s2` (2 px, Endpunkt r 4); Budget DB1/DB4 gestrichelt `--muted` (4 4) mit Text „Budget DB1 60 %“ rechts, 14 px unter Linie | Endlabels „DB1 62.4 %“/„DB4 22.7 %“ rechts vom Endpunkt; x-Labels wie oben (`step ≥ 30`) | Titel `mLabels[i]`; „DB1 x %“, „DB4 x %“; vertikales Fadenkreuz (`--muted`, Opazität .6) |
| `chartHBar(host, items, opt)` | horizontale Balken, sortiert wie geliefert | Zeilenhöhe 26, Balkenhöhe 12; ml `min(opt.labelW‖170, W·0.45)`; mr `opt.mr‖64`; mt 20 mit Referenz, sonst 4; H = mt + n·26 + 4 | linear, `dmax = max(0, Werte, ref)`, `dmin = min(0, Werte)`; Nullachse `--axis` | positiv `--s1` (rechts gerundet), negativ `--red` (links gerundet); Referenzlinie `--ink-2` gestrichelt (3 3) mit Label oben | Kategorie links rechtsbündig; Wert `opt.fmt` oder `nf0` rechts vom Balken (negativ: rechts der Null) | Titel = Label; „{opt.valLabel‖'Wert'}“ + (ausser `share:false`) „Anteil {d/Σ·100, 0 Dez.} %“ |
| `chartChurn(host)` | divergierende Säulen | H 230; ml 40, mr 10, mt 16, mb 26; bw `min(24, band·0.5)` | `max = niceMax(max(neu, verloren)·1.1)`; Mitte = 0; Ticks −max, −max/2, 0, max/2, max (negativ mit „−“) | neu nach oben `--s1`; verloren nach unten `--red`; `null` → Text „offen“ unter Achse | Quartal unter jeder Säule | Titel Quartal; „neu gewonnen“, „verloren“ („noch offen“), „Saldo“ („+n“/„−n“/„–“) |
| `sparkSvg(arr)` | Sparkline | 120 × 26, Rand 2/3 px | min–max normiert | Linie `--deemph` 1.5 px; Endpunkt `--s1` r 3.5, Rand `--card` | – | – (aria-hidden) |

Verwendung chartHBar: `ch-grow` (Wachstum, fmt `fPct`, labelW 220, share false, Referenz „Gruppe {fPct}“, sortiert absteigend, TSC mit „ *“), `ch-ka` (keyAcc, fmt „x Mio“, labelW 120), `ch-qend` (labelW 210, share false), `ch-qline` (labelW 150 TRCH / 140 Gruppe), `ch-db` (DB-Stufen, fmt „x % · ≈ y Mio“, labelW 50, mr 140, share false), `ch-qstat` (labelW 130), `ch-qage` (labelW 100, valLabel „Meldungen“), `ch-qco` (labelW 120), `ch-misco` (labelW 170, valLabel „Zeilen ohne Kosten“), `ch-ind` (TSC Branchen, fmt `fKM`, valLabel „Umsatz YTD“, labelW 170).

**Farbpalette** (CSS-Tokens, Hell / Dunkel):

| Token | Hell | Dunkel |
|---|---|---|
| `--page` | #f4f3ef | #0d0d0d |
| `--surface` / `--card` | #fcfcfb / #ffffff | #1a1a19 / #1a1a19 |
| `--raised` | #f7f6f2 | #222220 |
| `--ink` / `--ink-2` / `--muted` | #0b0b0b / #52514e / #7a7873 | #f0efec / #c3c2b7 / #8f8d86 |
| `--grid` / `--axis` | #e7e6e0 / #c3c2b7 | #2c2c2a / #45443f |
| `--s1` / `--s1-fc` | #2a78d6 / rgba(42,120,214,.38) | #3987e5 / rgba(57,135,229,.40) |
| `--s2` | #eb6834 | #d95926 |
| `--red` | #e34948 | #e66767 |
| `--deemph` | #a9a79f | #6f6d67 |
| `--good-text` / `--bad-text` | #006300 / #c22f2f | #0ca30c / #ef6b6b |
| `--gap-bg` / `--gap-ink` / `--gap-line` | #fff7e3 / #6b4a00 / #e9b23c | #2b2412 / #f3cf7a / #b9871f |
| `--part-bg` / `--part-ink` | #eaf2fc / #1c4f8f | #16263b / #9cc4f3 |
| `--ok-bg` / `--ok-ink` | #e8f5e8 / #0a5c0a | #12261a / #7fd67f |
| `--chip` / `--chip-on` / `--chip-on-ink` | #eceae4 / #0b0b0b / #ffffff | #262624 / #f0efec / #0b0b0b |
| Badges P1 / P2 | `--critical` #d03b3b (Text weiss) / `--warning` #fab219 (Text schwarz) | gleich |

Badge-Klassen: `gap` (gelb, Rahmen), `part` (blau), `ok` (grün), `calc` (grau mit Rahmen), `p1/p2/p3`. Kettenstatus `st.ok/part/miss/tbd/na` analog. `.up` grün, `.dn` rot, `.neu` `--ink-2`.

---

## 5. Sichtbare deutsche UI-Texte

### 5.1 Shell, Navigation, Werkzeuge
Monthly Review Gesellschaften · Trafag · Sales-Steuerung · Berichtsmonat Oktober 2026 · YTD 01–10.2026 · Datenstand · statischer Snapshot · Ansicht · Monthly Review · Gap-Register · Analyse Power BI · ◐ Thema · Hell/Dunkel umschalten · ⎙ Drucken · Aktuelle Ansicht drucken · Gesellschaft · Gesellschaft wählen · TSC-Dateien · Werte aus der TSC-Datei (Excel), nicht im internen BI · Tabelle · Diagramm · Datenquellen · Details · Stand: · ✓ vollständig · ~ teilweise · ✕ fehlt · ⚠ GAP · Daten fehlen · → Gxx.

Footer (wörtlich):
- „Quellen: Internes BI (trch-webapp-bidashboard › Verkauf, Saison und Prognose, Neue und verlorene Kunden, Management Analyse › Gruppenmarge) · Power BI „Trafag Sales Cockpit“ inkl. semantischem Modell · Power BI „RMA Statistic“ · Workspace „Trafag Sales Management Workspace“. Alle Werte manuell am 03.11.2026 übernommen.“
- „TSC-Gesellschaften (TRJP, TRCZ, M/S, M/S A, GFS): SharePoint Global Sales & Business Development › TSC › Targets & Progress 2026/2025 (P&L und Verkaufsblatt, Stand der Dateien 30.10.–03.11.2026, übernommen am 04.11.2026). CHF zu Konstantkurs gemäss Finance-Kurstabelle (Budget 2027 › Conso_BD_TSC+KAMASM.xlsx): EUR 0.94 · CZK 0.039265 · JPY 0.005239. Diese Gesellschaften sind nicht im Gruppentotal des internen BI enthalten.“
- „Aktualisierung: Für den nächsten Monat den Datenblock DATA im Skript ersetzen (TSC-Gesellschaften: DATA.tsc, Werte in Lokalwährung); die Darstellung rechnet Deltas, Prognose EOY, Budget-Erreichung und Diskussionspunkte selbst. Lücken sind als ⚠ GAP markiert und im Gap-Register mit Priorität und Abhängigkeiten geführt.“

### 5.2 Abschnittstitel und Untertitel
Performance Gruppe · Performance · Marge · Gruppenmarge & Deckungsbeitrag · Marge · Gruppenmarge auf Materialnummer-Ebene · Marge · lokale P&L · Pipeline & Leads · Qualität · offene Q-Fälle · Kunden · Diskussionspunkte & Massnahmen · Gap-Register · Was der Bericht heute zeigt.
Untertitel: „Umsatz nach Finance-Regeln, ohne Konzernkunden, CHF zum Kurs des Belegdatums.“ · „Internes BI › Gruppenmarge, Finance-Jahr 2026. Berechnung in Lokalwährung je Gesellschaft, Zeilen ohne Kostenbasis ausgeschlossen, CHF zum Jahreskurs.“ · „Gruppenmarge = Endkundenpreis der Gesellschaft − TR AG Standardkosten (MBEW-STPRS) über die Trafag-Sachnummer. Finance-Jahr 2026, Jahreskurs.“ · „Bruttoumsatz Dritte · CHF zu Konstantkurs … · TSC-Budget linear je Monat.“ · „TSC-P&L in {cur} (Ist …, inkl. Intercompany) · Gruppenmarge auf TR AG-Kosten nicht verfügbar.“ · „Dynamics CRM ist nicht an das Reporting angebunden – Sicht vorbereitet, Werte fehlen.“ · „SAP QM-Meldungen Q1 aus Power BI „RMA Statistic“, Stand …“ · „SAP QM-Meldungen Q1 (Kundenreklamation) aus Power BI „RMA Statistic“, Stand …“ · „SAP QM-Meldungen Q1 aus Power BI „RMA Statistic“ (System TRCH), Stand …“ · „Kundenbewegung und Top-Kunden der Gruppe.“ · „Top-Kunden mit Beteiligung der Gesellschaft, 12 Monate bis 10.2026.“ · „Verkaufsblatt der TSC-Datei · Umsatz 01–10.2026 (Trafag-Produkte) · Tsd. CHF zu Konstantkurs.“ · „Keine Kundendaten in der TSC-Datei.“ · „Abhängigkeiten in Reihenfolge Datenmodell › Prozess › Schnittstelle › Reporting · Owner & Termine bewusst offen (tbd.)“

### 5.3 Kachel-Labels
Umsatz {Monat} · Umsatz YTD … · TSC-Gesellschaften YTD · Prognose EOY 2026 · Kunden mit Umsatz (12 M) · Budget & Auftragseingang · Budget-Erreichung · Budget-Erreichung YTD · Auftragseingang R12M · Auftragseingang & Book-to-Bill · Auftragseingang YTD · Intercompany-Umsatz YTD · Prognose nächste 12 Monate · Gruppenmarge · Gruppenmarge Tochtergesellschaften · Marge TRCH · Umsatz ohne Kostenbasis · Saubere Kostenbasis · Deckungsbeitrag fix/variabel · Umsatz & bekannte Kostenbasis · Datenlage Kostenbasis · DB1 R12M (Power BI) · DB4 R12M (Power BI) · DB1 lokal YTD · DB3 YTD (nach Personalkosten) · Gruppenmarge (TR AG-Kosten) · Offene Q1-Meldungen · Offene Q1-Meldungen mit Endkunden · Offene Q1-Meldungen (Gesellschaft als Kunde) · Offene Q1-Meldungen Produktlinie · Fälle mit Kunden von … · Q-Fälle … · Offen seit · Offen seit (Ø / Median) · Länger als 90 Tage offen · Rücksendung offen (Gruppe) · Übergabe an Verkauf · Endkunden-Fälle im Markt · Neu- & Verlustkunden … · Umsatz je Marktsegment (KdGr) · Kundenzufriedenheit · Top-Kunden … · Gaps gesamt · Priorität 1/2/3 · Bericht · Abdeckung · Felder im Modell, nicht dargestellt · Nicht im Modell.

Kachelwerte (Text): Kein Soll/Ist je Monat · Kein Budget je Gesellschaft · Nicht im Reporting · Nicht auswertbar · Noch nicht gemeldet · Nur teilweise · Berechnung ausstehend · Kein Split in den Daten · Kein Split · Nicht verfügbar · Liste nicht geladen · Nicht zuordenbar · Keine Zuordnung · Nur Gruppensicht · Im Modell, nicht dargestellt · Nur Branche (frei) · Veraltet · Keine Kundendaten · Trafag Sales Cockpit · SAP S/4HANA TRCH · Zeilen ohne Kosten · Zeilen · Tage · Gruppen · Informationsbereiche.

Delta-/Fuss-Fragmente: ggü. VJ · ggü. VJ (TD) · ggü. Okt. 2025 · ggü. GJ 2025 (…) · ggü. letzte 12 M · ggü. Budget YTD · ggü. Budget … · Vorjahresmonat … · Verlauf 11.2025–10.2026 · Vorjahr … · 12 Monate … · 9 Gesellschaften im internen BI · Excel, CHF zu Konstantkurs · Gruppe inkl. ≈ … (indikativ) · Ist Jan–Okt + Prognose Nov–Dez, keine Planung · Ist Jan–Okt + Prognose Nov–Dez (Vorjahresmonat × Wachstum), keine Planung · Internes BI · 11.2026–10.2027 · Methode ohne Planung · … kaufen bei mehreren Gesellschaften · 2026 Q1: … neu · … verloren · Book-to-Bill … (Okt. …) · Power BI, Auftragsbestand fehlt · … vom Budget GJ · Ist … + Forecast …–Dez. der Datei · Budget YTD … · Budget GJ … · Konto 6010 · … des Gesamtumsatzes · FOC … · Forecast GJ … · nicht im BI · (Trafag-Produkte) · … auf … Umsatz · netto, … des Umsatzes · Liste und Materialien unten · alle Zeilen mit Kostenbasis · Kostenbasis … · Finance-Jahr 2026, Jahreskurs · nicht direkt mit Verkaufsumsatz vergleichbar · Typ Intern: … · Lokal: … · Extern: … · Saubere Kostenbasis … (… Zeilen) · Zeilen nach Typ gemäss internem BI · Kostenbasis vollständig, Wert wie im internen BI · Internes BI weist wegen … Zeilen ohne Kosten keine Marge aus – diese Zeilen sind hier ausgeschlossen · gerechnet in … · DB2 … · inkl. IC-Kunden · DB1 … auf Nettoumsatz … · von … offenen Meldungen gesamt · Direktgeschäft & nicht zuordenbare Endkunden (inkl. intern Trafag AG) · Kunde in SAP QM: … · ab „Angelegt am RMA“ bis Datenstand · Median … Tage · Gruppe Ø … · ältester Fall … · … Tage offen · … · davon …: … · … aller offenen Meldungen · Ware vom Kunden noch nicht zurück – Nachfassen durch Verkauf · Rücksendung offen: … · Einzelfall-Liste unten · Statussicht … · Kundensicht … · … der offenen Fälle · Ältester Fall: … · warten auf Sales Coordinator · Detail-Analyse CSQ: … · WE CSQ: … · davon Datenqualität/Darstellung: … · blockiert Kern-KPI oder erzeugt falsche Zahlen · wichtig für Diagnose & Massnahmen · Ergänzung / Hygiene · Workspace „Trafag Sales Management Workspace“ · Modell-Owner Constantin Koch · tägliche Aktualisierung 01:00 · keine Gesellschafts-Dimension; Tochtergesellschaften nur als IC-Kunden · Quick Wins ohne neue Datenquelle · u. a. Budget, IC-Kennzeichen, CRM, Q-Fälle, Gruppenmarge.

Gap-Begründungen (`why`) wörtlich siehe Abschnitte 2.3–2.7.

### 5.4 Badges
statistisch · separat · inkl. IC · TSC-Budget linear · TSC-Forecast · Verkaufsblatt · ✓ vollständig · ohne fehlende Zeilen · ⚠ {n} Zeilen · ✓ keine · IC-Abgrenzung offen · ⚠ offen · ✓ · ⚠ · P&L bis … · Prognose · Forecast · nicht abgezogen · lokal · TR AG · offen {n} · {x} % vorl. · nicht im BI · keine · nur TRCH · Quick Wins · ⚠ Gaps · IC · IC, nicht gekennzeichnet · P1 · P2 · P3 · ⚠ Massnahmen-Tracking fehlt · Flags „IC · GfS“, „Daten prüfen“, „Basiseffekt“.

### 5.5 Karten- und Diagrammtitel, Captions
Umsatz je Monat 2026 vs. 2025 [· Gruppe] · Tsd. CHF · Nov./Dez. 2026 = statistische Prognose · Vorjahr 2025 durch Datenfehler verzerrt · Tsd. CHF zu Konstantkurs · …–Dez. 2026 = Forecast der TSC-Datei · Wachstum YTD je Gesellschaft · Umsatz … ggü. … · gestrichelt = Gruppe (internes BI) · * TSC-Datei, währungsbereinigt · Scorecard Gesellschaften · Zeile anklicken → Monthly Review der Gesellschaft · Q1 = offene Q-Meldungen mit Gesellschaft als Kunde · Gruppenmarge je Gesellschaft – ohne Zeilen ohne Kostenbasis · Umsatz mit Kostenbasis − bekannte Kostenbasis (TR AG-Standardkosten bzw. lokaler Standardpreis) · Zeile anklicken → Gesellschaft · Zeilen ohne Kostenbasis je Gesellschaft · Anzahl Zeilen · Σ … · Was sind die Zeilen ohne Kostenbasis? · Nach Status und grösste Materialien (Σ |Umsatz|) · Liste: alle Zeilen ohne Kostenbasis · Filter nach Gesellschaft, Status und Typ · Spaltenkopf klicken zum Sortieren · CSV öffnet in Excel · Zeilen ohne Kostenbasis · … · Diese Zeilen sind aus der Gruppenmarge oben ausgeschlossen · Grösste Materialien ohne Kostenbasis (Σ |Umsatz|) · Status · Anzahl offener Meldungen · Offen seit · Anzahl Meldungen nach Alter · Produktlinie · Produktlinie (Quellbericht) · Gesellschaft / Kunde · Zuordnung über Kunde in SAP QM · Anzahl · … weitere mit Endkunden · Offen seit je Status · Tage seit Anlage der Meldung · Drilldown: offene Q1-Meldungen [· …] · Filter nach Gesellschaft, Status und Bearbeiter · Sortierung per Spaltenkopf · „Offen seit“ ab Anlage der Meldung · Endkunden & intern · Meldungen mit … als Kunde · Keine offenen Meldungen mit … als Kunde. · Neu gewonnene vs. verlorene Kunden je Quartal · Gruppe · Verluste erst 6 Monate nach Quartalsende sichtbar · 2025 Q1 nicht belastbar (erstes Datenjahr) · Top 10 Kunden Gruppe (12 M) · Internes BI › Kunden · ohne Konzernkunden · Top … Kunden · … · Umsatz = Kundenumsatz über alle beteiligten Gesellschaften (nicht nur …) · Quelle: internes BI › Kunden · Nur … Kunden von … sind unter den Top 200 der Gruppe. · Einordnung · Kennzahlen aus den verfügbaren Quellen · Umsatz letzte 12 Monate · Umsatz GJ 2025 · Anteil am Gruppenumsatz YTD · Bester Monat 2026 (Ist) · Schwächster Monat 2026 (Ist) · IC-Lieferungen TRCH → … (R12M, Power BI) · Auftragseingang vs. Umsatz je Monat (Power BI) · Mio CHF · TRCH inkl. IC-Kunden · Nov. 2026 (laufend: AE … / Umsatz …) ausgeblendet · Marge DB1 & DB4 je Monat (Power BI) · TRCH · gestrichelt = Budget (prelim.) · DB1 Juli bitte gegenprüfen · Umsatz je Key Account R12M (Power BI) · Mio CHF · Konzernzuordnung KONZS · Top 10 Materialien nach Gruppenmarge (EUR) · TRAT · alle … Detailzeilen FJ 2026 aggregiert · … Materialien · Top 20 = … % des Umsatzes · Tiefste Gruppenmarge (Umsatz ≥ 15k EUR) · Gleiche Typenreihe mit sehr unterschiedlicher Marge (8473: 5 % bis 78 %) – Preis vs. Kostenbasis prüfen · Marge je Produktkategorie R12M (Power BI) · TRCH · inkl. IC-Kunden · Mio CHF · GAP: Materialebene TRCH nicht dargestellt · Kunden mit DB4 < 5 % (R12M): · GAP: Materialebene … noch nicht aggregiert · GAP: Gruppenmarge je Materialnummer für … nicht verfügbar · Benötigt: · Deckungsbeitragsstufen R12M (Power BI) · TRCH · in % vom Nettoumsatz … · CHF-Werte ≈ % × Umsatz · DB3 im Modell vorhanden, im Bericht nicht dargestellt · Einordnung DB-Stufen · Power BI Sales Cockpit, inkl. IC-Kunden · Umsatz nach Konto · Umsatz nach Gesellschaft & Konto · Tsd. CHF zu Konstantkurs · Δ VJ = gleiche Monate Vorjahr · Konten MS + MSA zusammen · Budget GJ 2026 · Forecast GJ 2026 · Bester / schwächster Monat 2026 (Ist) · P&L-Stufen YTD … · Tsd. CHF zu Konstantkurs · % vom Nettoumsatz inkl. IC · Quelle TSC-P&L · Nettoumsatz (inkl. IC) · Nettoumsatz · Intercompany (6010) · Erlösminderungen (P&L) · Anteil am Umsatz des Verkaufsblatts · Ist vs. Budget YTD = Umsatz ÷ (Budget GJ × 10/12) − 1 · kein Budget je Kunde gepflegt · Umsatz nach Branche (Verkaufsblatt) · Tsd. CHF · Branche frei gepflegt, keine KdGr · Einordnung Kunden · aus dem Verkaufsblatt abgeleitet · Kundenkonzentration: · Mehr als 40 % unter Budget YTD (nach Betrag): · budgetierte Kunden ohne Umsatz YTD (Σ Budget …): · Keine Auftragseingänge je Kunde gepflegt. · Verkaufsblatt unvollständig: … · Diskussionspunkte aus den Daten · Automatisch abgeleitet – Massnahmen, Verantwortliche und Termine im Meeting festlegen. · Empfohlene Reihenfolge (Abhängigkeiten) · Legende · Prüfhinweise (abgeleitete oder unsichere Werte): · Im semantischen Modell vorhanden – nicht dargestellt · Mit vorhandenen Daten umsetzbar · Im Modell nicht vorhanden · Benötigt neue Quelle oder Modellerweiterung · Darstellung & Methodik · Weitere Berichte im Umfeld · Accounts R12M mit Margen (Power BI, TRCH) · IC-Kunden markiert · DB4 < 5 % hervorgehoben · Kopfzahlen R12M: … · Liste der Zeilen ohne Kostenbasis noch nicht geladen · Einzelfall-Liste noch nicht geladen · GAP: Keine Lead- und Opportunity-Daten für … · Abhängigkeiten (Reihenfolge):

Pipeline-Skelett (Titel → Beschreibung): Pipeline offen (gewichtet) → Σ Opportunity-Wert × Wahrscheinlichkeit, je Gesellschaft · Pipeline-Abdeckung → gewichtete Pipeline ÷ Budget-Lücke Rest-GJ (benötigt G01) · Neue Opportunities im Monat → Anzahl & Wert, nach Marktsegment (KdGr) · Gewonnen / verloren & Win-Rate → Monat und rollierend 12 Monate, inkl. Verlustgründe · Ø Alter & überfällige Abschlüsse → offene Opportunities mit Abschlussdatum in der Vergangenheit · Neue Leads & Konversion → Leads im Monat, Lead → Opportunity-Quote (G04) · Top-10 Opportunities → Abschluss in den nächsten 90 Tagen · Aktivitäten / Besuche → optional, je Key Account · „– Daten fehlen · Ziel tbd.“

Empfohlene Reihenfolge (Gap-Register): „Zuerst Datenbasis: G07 IC-Kennzeichen (u. a. „GFS Germany“) und G05 Kostenbasis – sonst sind Umsatz- und Margenvergleiche zwischen den Gesellschaften verzerrt; G06 (Materialebene) setzt G05 voraus. G17: {TSC} kommen bis zur BI-Anbindung aus Excel.“ · „Dann Soll-Werte: G01 Budget je Gesellschaft & Monat – Voraussetzung für Soll/Ist, Pipeline-Abdeckung (G03) und Forecast (G09).“ · „Dann Vorlauf: G02 Auftragseingang/-bestand und G03/G04 CRM-Pipeline – setzen Prozessstandard (Phasen, Pflichtfelder) und die Verknüpfung CRM-Account ↔ SAP-Kunde voraus.“ · „Parallel möglich: G08 Q-Fälle je Gesellschaft, G10 KdGr-Visual (Quick Win), G18 Power BI-Darstellung, G14 Massnahmen-Tracking.“ Legende: vorhanden · teilweise · fehlt · zu klären · nicht relevant · blockiert Kern-KPI / falsche Zahlen · Diagnose · Ergänzung.

### 5.6 Tabellenköpfe
Monat · 2025 · 2026 · Δ · AE · Umsatz · B2B · DB1 · DB4 · Stufe · % · ≈ Mio CHF · Ges. · Gesellschaft · YTD 2026 · YTD 2025 · Δ YTD · Okt. 26 · Δ Okt. · Progn. EOY 26 · Progn. 12M · Gruppenmarge · Q1 · Umsatz FJ 26 · Zeilen o. Kosten · Umsatz o. Kosten · Umsatz mit Kostenbasis · Kostenbasis · Marge % · Tool-Ausweis · Materialebene · Material · Bezeichnung · Umsatz EUR · Zeilen · Kostenquelle · Kategorie · Δ AE · Rang · Kunde · Land · Gesellschaften · Umsatz 12M · Δ ggü. VJ · Quartal · neu · verloren · Fälle · Ø Tage · Median · > 90 Tage · Status · Umsatz CHF netto · Umsatz CHF (netto) · Σ |Umsatz| · Konto · YTD … · Δ VJ · Budget GJ · FOC GJ · Budget YTD · DB1 lokal · DB3 YTD · Ist YTD · % Umsatz · Vorjahr YTD · Ist − Budget · Branche · Umsatz YTD · Anteil · Ist vs. Budget YTD · AE YTD · Forecast GJ · # · Thema (aus Daten) · Bezug · Massnahme · Verantwortlich · Termin · ID · Prio · Bereich · KPI / Information · Abhängigkeiten · Auswirkung · Nächster Schritt · Owner · Seite · Inhalte · Feld / Kennzahl · Tabelle · Nutzen im Monthly · Gap · Information · Folge · Bericht · Inhalt · Stand · Account. Listen: Ges. · Status · Rechnung · Material · Bezeichnung · Typ · Kostenquelle · Umsatz CHF · Umsatz lokal · Meldung · Kunde · Fehlerbild · Menge · Angelegt · Offen seit (Tage) · Rücklieferung · Tage seit Rücklief. · Massnahme angelegt · Bearbeiter (CSV-Köpfe in 2.2).

### 5.7 Sonstiges
Alle (n) · Suche · ⇩ CSV (Excel) · {n} von {m} Einträgen · Σ Umsatz CHF … · Weitere {x} von {y} anzeigen · Keine Einträge für diese Auswahl. · Sortieren · neu · offen · noch offen · kein Budget · Saldo · neu gewonnen · Δ ggü. VJ · 2026 Ist · 2026 Prognose · 2026 Forecast · 2025 Ist · Budget DB1 · Budget DB4 · Book-to-Bill · Anteil · Wert · tbd. · ohne Branche · Endkunde (TRCH) · TRCH intern · Altersklassen ≤ 30 Tage / 31–60 Tage / 61–90 Tage / 91–180 Tage / > 180 Tage · Chain-Stufen Datenmodell · Prozess · Schnittstelle · Reporting.
Aria-Labels der Diagramme: „Monatsumsatz 2026 gegenüber 2025, {code}“, „TRCH Auftragseingang und Umsatz je Monat“, „TRCH Marge DB1 und DB4 je Monat“, „Neu gewonnene und verlorene Kunden je Quartal“, „Wachstum YTD je Gesellschaft“, „Umsatz je Key Account“, „Offene Q1-Meldungen nach Endkunde / Produktlinie / Status / Alter / Gesellschaft“, „Deckungsbeitragsstufen TRCH“, „Zeilen ohne Kostenbasis je Gesellschaft“, „Umsatz nach Branche {lab}“, Fallback „Balkendiagramm“.
Inhalt aus DATA (übersetzungsrelevant, nicht im Code): `meta.sources[].name/d`, `gaps[].*`, `pbiAnalysis.*`, `checks[]`, `rma.status/lines/queues`, `tsc.companies[].revDef/oiNote/q.label/lines[0]`.

---

## 6. Fehler, Inkonsistenzen, hart codierte Annahmen

### 6.1 Hart codiert statt aus Daten (bricht beim Monatswechsel)
1. `DATA.lastActual` und `DATA.months` werden nie gelesen. `stats()` nutzt feste Indizes (YTD 12–21, VJ 0–9, Okt. 21/9, Prognose 22/23, R12M 10–21); `chartMonthly` Default `nA = 10`; Monatstabellen markieren „Prognose“ ab `i > 9`.
2. HTML-Untertitel „Berichtsmonat Oktober 2026 · YTD 01–10.2026“ statt `meta.month/ytd`; Scorecard-Köpfe „Okt. 26“, „Δ Okt.“, „Progn. EOY 26“; Texte „Ist Jan–Okt + Prognose Nov–Dez“, „Verlauf 11.2025–10.2026“, „11.2026–10.2027“, „12 Monate bis 10.2026“, „Nov./Dez. 2026“, „Nov. 2026 (laufend …)“.
3. `mmYY()` hängt immer „.2026“ an; `per(10)` hart in TSC-Auftragseingang und TSC-Kundenabschnitt; Faktor `10/12` im Kunden-Budgetvergleich unabhängig von `lastM`; TSC-Gap-Kachel `n < 10` und `m26[9]` („Oktober ist dort noch Forecast“) setzen Oktober als Berichtsmonat voraus; `monthLong[n]` bei `n = 12` ergibt `undefined`.
4. `DATA.churn[3]` = „2026 Q1“ fest (Gruppe Kachel und Diskussionspunkt 2, ohne Prüfung, ob verloren > neu).
5. GFS-Germany-Werte doppelt geführt: `fKM(1740)`, `fCHFM(1621280)`, „DB4 −36.0 %“ hart in TRCH-Note, `points()` und `gp`, obwohl `tsc.GFS.trchIc` dieselben Werte enthält.
6. TRCH-Kachel „(Okt. …)“ nutzt `oiM[10]/revM[10]`; TRCH-Kategorien-Note nutzt `cats[2]` (Accessories) und `cats[4]` (Thermostat) per Index.
7. Diskussionspunkt TRAT: Materialien 63503/64494, „5–7 %“ und Umsatz `182115 + 174004` hart. Caption „8473: 5 % bis 78 %“ und „Umsatz ≥ 15k EUR“ hart.
8. Diskussionspunkt TRCH nennt drei Kunden (Beijing Macrocontrol, Ecolab, GFS Germany); der dynamische Filter `accounts[5] < 5` liefert vier (zusätzlich Trafag Controls India, IC).
9. `tscPoints`: TRCZ „11“ und TRJP „5“ offene Meldungen hart statt `t.q.n`.
10. `gp[2]` wählt TRDE, TRCH, TRFR, TRES fest aus (Werte dynamisch, Auswahl nicht).
11. Pbi-Kachel „Modell-Owner Constantin Koch · tägliche Aktualisierung 01:00“, Kachel „Kundenzufriedenheit“ „20.12.2025“, „9 Gesellschaften im internen BI“, „bis 09.2026; TRCZ 10.2026“ in Scorecard-Note, „Budget … – beide übertroffen.“ (nicht geprüft), „DB1 Juli bitte gegenprüfen“ – alles Text im Code.
12. `chartMargins` fixe Skala 0–80 %; DB-Werte > 80 % würden abgeschnitten. DB4-Balken TRCH-Kategorien skaliert auf 40 % = 100 %, ohne Obergrenze (Balken kann über 90 px wachsen).
13. Footer-Kurse und TSC-Liste hart im HTML, obwohl `tsc.meta.fx` existiert; `tsc.meta` wird gar nicht gelesen.

### 6.2 Rechen- und Anzeigefehler
14. `pct(a, 0)` liefert `null`, im Monats-Tooltip wird aber `fPct(null)` aufgerufen → Anzeige „0.0 %“ statt „–“ (in Node verifiziert: `Intl.NumberFormat('de-CH').format(null)` = „0.0“). Tritt bei MS August 2025 (`m25[7] = 0`) auf.
15. `pct()` mit negativer Basis (MSA Dezember 2025 `m25[11] = −91 011`) ergibt ein sinnloses Vorzeichen (−385 %) in Tabelle und Tooltip.
16. `stats().ytdD` rechnet aus den auf 2 Dezimalen gerundeten Mio-Feldern `c.ytd/c.ytdPy`, nicht aus `rev`; die exakt summierte Variable `ytd` wird nur für EOY verwendet. Rundungsabweichungen bis 0.4 Pp. (aus Mio-Feldern vs. aus `rev`: TRUS 50.0 % vs. 50.3 %, TRFR 1.7 % vs. 2.1 %, TRES 50.7 % vs. 50.9 %). Im Nachbau aus den Monatswerten rechnen. Gleiches gilt für die Wachstumskarte und die Scorecard (`pct(x.ytd, x.ytdPy)`).
17. Gruppen-Kachel „TSC-Gesellschaften YTD“ und „Gruppe inkl. ≈ …“ addieren unterschiedliche Perioden (MS/MSA 01–09, Rest 01–10), Brutto- (TSC) und Nettoumsatz (BI) und zählen GFS doppelt (TRCH → GFS). Als „indikativ“ gekennzeichnet, aber rechnerisch nicht sauber.
18. Gruppen-Diskussionspunkt 1 ergibt „… bei .“, wenn keine Gesellschaft `octD ≤ −15` hat.
19. Churn-Achse beschriftet die untere Hälfte mit negativen Zahlen (z. B. „−400“), obwohl verlorene Kunden als positive Anzahl gemeint sind.
20. `chartHBar` „Anteil“ für Key Accounts bezieht sich auf die Summe der 7 gezeigten Key Accounts, nicht auf den Gesamtumsatz.
21. Listen-Sortierung: `null` immer am Ende (auch absteigend), String-Vergleich ohne Locale; CSV-Beträge mit Dezimalpunkt (in deutschem Excel ggf. als Text).
22. Tabellen-/Diagrammumschaltung und Scrollposition gehen bei jedem `renderAll` (Resize, Theme, Chip) verloren.

### 6.3 Datenkonsistenz (im Snapshot)
23. RMA-Summen stimmen nicht überein: Status 131, Kunde 135, Produktlinien 133; `byCompany` 39 + `endCustomers` 96 = 135; `endInternal` 1 unklar (ob in 96 enthalten). `checks` nennt nur 131 vs. 135.
24. `gm.k` (CHF) ≠ `gmLocal.k × fx` wesentlich bei TRIT (3 948 891 vs. 3 986 168, 0.9 %), TRES (1 877 182 vs. 1 894 295, 0.9 %) und TRFR (309 906 vs. 315 198, 1.7 %); TRUK (138 CHF) und TRDE (22 CHF) sind Rundung. `u × fx` passt überall exakt. Die „fx = Umsatz CHF ÷ Umsatz lokal“-Regel gilt also nur für den Umsatz; die Margenrechnung nutzt ausschliesslich `gmLocal`.
25. TRUS: `intern + lokal + offen = 844`, aber `lines = 830`.
26. `pbi.oiYoY` und `pbi.revYoY` beide exakt 22.5 – möglicher Übertragungsfehler, bitte im Power BI prüfen.
27. `customers`: Pfreundt GmbH Δ −43 405 % (wird als „Daten prüfen“ markiert); Land leer bei 6 TRDE-Kunden.
28. `tsc.*.budM × 12 ≠ bud` (Rundung, z. B. MSA +7); `lines`-Summen weichen um wenige LC von `Σ m26` ab (TRCZ +5, MS +2).
29. GFS: `pl.net.bud` (6 813 750) ≠ `bud + ic.bud` (6 873 750); Differenz 60 000 vermutlich budgetierte Erlösminderungen – nicht dokumentiert.
30. `matTRAT.flop` ist nicht nach Marge sortiert.

### 6.4 Toter oder ungenutzter Code / Felder
31. `t.parts` (Tabelle „Umsatz nach Gesellschaft & Konto“, „Konten MS + MSA zusammen“, Budget-Punkt in `tscPoints`) – kein Datensatz hat `parts`; Überbleibsel einer früheren Zusammenlegung von MS und MSA.
32. `ST_SHORT`, `TM`, CSS-Klasse `.hot` (gesetzt, nicht gestylt), `meta.title`, `companies[].land`, `tsc.companies[].land`, `plM` (Code nutzt `pl.m`), `pl.pers`, `rma.endInternal`, `tsc.meta`.
33. In `points()` ist der Zweig `c.code === 'GRP'` (letzter Punkt) unerreichbar, weil GRP über `renderGroup`/`gp` läuft.
34. `rma.otherCompanies` wird nur im Zweig ohne Einzelfälle genutzt und ist leer.

### 6.5 Abweichungen Gruppe ↔ Einzelansicht
35. Scorecard-Spalte Q1 für TSC liest `rma.byCompany`; TRCZ zeigt „–“, obwohl die Einzelansicht 11 (Produktlinie) zeigt; GFS/MS/MSA ebenfalls „–“.
36. Gruppenmarge-Spalte der Scorecard zeigt für TRUS „vorl.“ aus `gm.prov`, die Einzelansicht rechnet dieselbe Marge über `gmEx` (identisch 56.4 %, aber zwei Quellen).

### 6.6 Unklar (nicht aus der Datei entscheidbar)
- Ob `rmaCases.stand` und `meta.stand` gleich sein müssen (Gruppe zeigt `fDate(rmaCases.stand)`, Einzelansichten `meta.stand`).
- Ob `caseCo('trafag gmbh') → TRDE` korrekt ist (TRDE-Firmenname ist im Datenblock nicht angegeben; `companyLabel.TRDE = '–'`). MS/MSA/GFS/TRCZ haben keine Zuordnung.
- Status-Werte von `gmMissing.rows.st` und `typ` sind nur über Texte bekannt („Standardpreis fehlt“, „Lieferant unklar“; Typ Intern/Lokal/Extern).
- Bedeutung der Prognosemethode „Vorjahresmonat × Wachstum“ steht nur als Text; die Werte kommen fertig aus dem internen BI.
- Ob `pbi.budget.rev` (71.5 Mio) IC enthält – im Code selbst als „Abgrenzung (inkl. IC?) Details tbd.“ markiert.

### 6.7 Hinweise für den Blazor-Nachbau
- Die Zweige „Liste geladen“ (`gmMissing`, `rmaCases`) wurden im Snapshot nie gerendert, sind aber mit Live-Daten der Normalfall; zuerst diese implementieren und testen.
- Statt HTML-Strings eigene Komponenten: `Tile`, `GapTile`, `SectionHeader`, `ChartCard` (mit Tabellen-Umschalter), `DependencyChain`, `DrillList<T>` (Filter/Suche/Sort/Limit/CSV), `ActionsTable`, plus SVG-Chart-Komponenten oder MudChart mit identischer Logik.
- Navigation per Route `/monthly-review/{view}/{co}` statt Hash; Theme über MudThemeProvider mit den Tokens aus Abschnitt 4.
- Alle in 6.1 genannten Konstanten aus dem Berichtsmonat (`lastActual`) ableiten.
