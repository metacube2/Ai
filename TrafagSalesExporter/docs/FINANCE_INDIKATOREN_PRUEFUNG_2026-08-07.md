# Finance-Dashboard: Indikatoren durchgesehen

Stand: 2026-08-07

Status: produktiv deployed und verifiziert am 2026-08-07 10:22 MESZ,
Funktionscommits `0c8cff5` und `b2e7c4f`, `455/455` Tests. Nachweis in Abschnitt 6.

Anlass: dieselbe Durchsicht wie fuer den Einkauf
(`docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md`), jetzt fuer Finance.

Abgrenzung zur bestehenden Pruefung: `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md`
deckt den **Kostenbasis- und Gruppenmargen-Strang** ab (fehlende Kostenbasis,
Statusmaskierung, `IsCostBasisKnown`). Diese Durchsicht sucht die anderen
Fehlerklassen: erfundene Werte, falsche Beschriftung, Kachel gegen Tabelle nicht
abstimmbar, Filterreihenfolge, stille Null ausserhalb der Kostenrechnung, toter
Code. Der Kostenbasis-Strang wurde nicht erneut aufgerollt.

## 1. Ergebnis der Durchsicht

### A — rechnet echt (nicht angefasst)

Finance Summary (`NetSalesActual` und `Rows.Sum` stammen aus derselben Liste und
stimmen exakt ueberein), Laender-Tabelle, Datenstatus, **Daten-Heartbeat**
(bestgetesteter Teil: `RollingRowCount7` mit vier tagesgenauen Erwartungen,
Tagesstatus in fuenf Szenarien, Export-Streifen in vier), Rohdaten-Diagnose (echte
Erwartungswerte im Test; die Seite sagt selbst, dass sie nicht mit Finance
abstimmbar ist), Gruppenmarge-Kostenlogik (2026-08-06 geprueft), Finance
Schulung, Finance Regeln.

### B — Logik korrekt, Datenbasis fehlt

- **Deckungsbeitrag** ist ueberall `-`, weil `StandardCostVariable` in 0 von
  96'059 Zeilen gesetzt ist. Korrekt angezeigt, Feature wirkungslos.
- **Gruppenmarge %** bleibt meist leer, weil rund 71'900 von 96'059 Zeilen keine
  belastbare Kostenbasis haben. Ehrlich, aber praktisch leer.
- **Soll/Ist fuer CH, CN und RU**: kein Sollwert gepflegt (Abschnitt 3).

### C — zeigte eine erfundene oder falsch beschriftete Zahl

Neun Punkte, alle behoben (Abschnitt 2).

## 2. Was geaendert wurde

Leitregel wie beim Einkauf: **fehlende Datenbasis wird sichtbar gemacht, nicht
geschaetzt.** Keine neue Kennzahl, keine neue Datenquelle.

Zwei Entscheide von Ingo vorab: bei gemischten Waehrungen **nur warnen** (die
Zahl bleibt stehen), und der Finance-Pivot **folgt dem Seitenfilter**.

### 2a. `Laender OK` / `Zu pruefen` verschwiegen zwei von vier Status

`BuildFinanceStatus` (`Services/ManagementCockpitService.cs`) liefert **vier**
Werte: `OK`, `Pruefen`, `Kein Sollwert`, `Keine Daten`. Die Schnelluebersicht
zaehlte nur die ersten beiden. Laender ohne Sollwert und ohne Daten steckten in
keiner der beiden Kacheln — bei fehlenden Sollwerten standen beide auf `0` unter
„Soll/Ist ohne Abweichung" und „Abweichung oder offene Regel". Das liest sich wie
„alles sauber" und heisst „nichts geprueft".

Neu: dritte Kachel `Nicht geprueft` mit Untertitel „ohne Sollwert oder ohne
Daten", plus ein Warnhinweis, wenn `OK` und `Pruefen` beide `0` sind und es
ungeprueffte Laender gibt. Die Statustexte stehen jetzt in der neuen Klasse
`Services/FinanceCountryStatuses.cs` und werden nicht mehr als Literale
verglichen — dieselbe Bauform, die `GroupMarginStatuses` fuer die Marge bereits
verwendet. `ManagementCockpitService`, `FinanceReconciliationService`,
`ManagementCockpit.razor` und `FinanceComparison.razor` nutzen die Konstanten.

Warum das mehr ist als ein Schoenheitsfehler: siehe die Messung in Abschnitt 3.

### 2b. `Net Sales Actual` und die Gruppenmarge-Kacheln addierten Waehrungen ungerechnet

`NetSalesActual` summiert ueber Zeilen, die nach `{Jahr, Land, Waehrung}`
gruppiert sind — CHF, EUR, GBP, INR und USD werden numerisch addiert.
`BuildDisplayCurrencyLabel` liefert dafuer den String `"Mixed"`, die Anzeige
lautet also z. B. `748'123'456.00 Mixed`. Mit INR im Bestand dominiert die
Rupienzahl die Summe.

Der Sparten-Reiter warnte davor bereits (`IsProductFinanceMixedCurrency`). Die
beiden `Net Sales Actual`-Kacheln (Schnelluebersicht, Finance Summary) und die
sieben Gruppenmarge-Kacheln hatten den Hinweis nicht, obwohl
`GroupMarginSummary.SalesValue`/`CostBasisValue` genauso mischen.

Neu: ein gemeinsamer Hinweis (`MixedCurrencyWarning`) an allen drei Stellen. Die
Zahl bleibt unveraendert — Entscheid Ingo.

### 2c. Finance-Pivot: Filter, verlorene Zeilen, Beschriftung, toter Zweig

Vier Befunde an derselben Kachelreihe:

1. **`BuildFinancePivotResult(allRows, year)`** rechnete auf der Menge VOR dem
   Land- und Waehrungsfilter. Mit `Land = DE` zeigte `Net Sales Actual` nur DE
   und `YTD Umsatz` daneben weiterhin alle Laender — die beiden Kacheln im selben
   Filterpanel waren nicht gegeneinander abstimmbar. Jetzt `scopedRows`.
2. **Verworfene Zeilen waren unsichtbar.** Zeilen ohne CHF-Jahresendkurs und
   Zeilen mit leerem TSC fallen aus der Pivotsicht heraus. Die Kachel
   `Zeilenbasis` zeigte die Zahl NACH dem Verwerfen unter dem Untertitel „Finance
   Include in CHF" und wich damit still von „Enthaltene Zeilen" im Finance
   Summary ab. Neu: `MissingRateRowCount` und `MissingTscRowCount` werden
   mitgefuehrt und als Hinweis ausgewiesen — dasselbe Muster, das der zentrale
   Rohbericht im selben Dienst schon verwendet.
3. **`YTD Umsatz` war ein Jahreswert**, und der Untertitel schrieb dazu „Alle
   Jahre", waehrend der Wert ein einzelnes Jahr war. Jetzt `Jahresumsatz` bzw.
   `Monatsumsatz`, und der Untertitel nennt das tatsaechlich verwendete Jahr.
4. **`YtdSalesChf` / `MtdSalesChf` waren tot.** Sie wurden berechnet und nirgends
   gelesen; die GUI rechnet dieselben Kennzahlen selbst — auf dem **gewaehlten**
   Jahr, waehrend die Dienstfassung das juengste Jahr nahm. Zwei Umsetzungen
   derselben Kennzahl mit verschiedenen Ergebnissen; die tote ist entfernt.

### 2d. `Ausgeschlossen` zaehlte Nullwertzeilen als Regelausschluss

`ResolveNetSalesActual` gibt fuer ausgeschlossene Zeilen `0` zurueck. Nach
`var include = rawInclude && value != 0m;` waren „durch eine Finance-Regel
ausgeschlossen" und „Betrag ist echt null" derselbe Zustand. Folgen:

- Die Kachel `Ausgeschlossen` hatte den Untertitel „Finance-Regeln", zaehlte aber
  auch kostenlose bzw. nullwertige Rechnungszeilen.
- Die Pruefpunkte `Nullwerte im Finance-Wert` und `Ausgeschlossene Zeilen` im
  Reiter Datenqualitaet meldeten dieselben Zeilen **zweimal** — und beide fliessen
  in den Entscheidungsradar, der sie damit doppelt bewertete.

Neu: `FinanceAggregationRow.IsExcludedByRule` traegt den Regelausschluss
getrennt. Der Pruefpunkt `Nullwerte` zaehlt nur noch Zeilen, die keine Regel
ausgeschlossen hat; `Ausgeschlossene Zeilen` zaehlt nur Regelausschluesse. Der
Kachel-Untertitel heisst jetzt „Finance-Regeln oder Wert 0", weil die Zahl beides
enthaelt.

### 2e. `Soll/Ist Vergleich` stand auf 2025 fest und behauptete ein Ergebnis

- Das Jahr `2025` hing an drei Stellen fest (Ueberschrift, Spaltenkopf,
  Serviceaufruf), dazu der Dienst-Default. Es gab keine Jahresauswahl, waehrend
  die Seite sich „Verbindliche Finance-Sicht aus der aktuellen zentralen
  Datenquelle" nennt. Neu: Jahresauswahl aus `FinanceReferences`
  (`GetAvailableReferenceYearsAsync`) — ohne Sollwert gibt es nichts zu
  vergleichen, deshalb ist das die richtige Liste. Standard ist das juengste
  Jahr mit Sollwerten. Ueberschrift und Spaltenkopf folgen der Auswahl.
- In der Spalte **„Berechnung"** stand fuer `FR`, `IN` und `US` unbedingt der Satz
  „**Passt gegen Soll**; Sales Price/Value ist bevorzugte Variante." — eine
  Ergebnisbehauptung aus einer fest verdrahteten Laenderliste, direkt neben einem
  **gerechneten** Statuschip, der fuer dieselbe Zeile `Pruefen` sagen kann. Die
  Zweige fuer ES, UK, IT und DE daneben beschreiben Regeln, keine Ergebnisse.
  Neu: „Sales Price/Value ist die bevorzugte Variante; Ergebnis siehe Ampel."
- Nachtrag beim Deploy-Nachweis: der alte Satz stand nach dem ersten Publish noch
  im Literalbereich der DLL — als verwaister Uebersetzungsschluessel in sechs
  Sprachen. Nachweis in Abschnitt 6, Bereinigung in Commit `b2e7c4f`.
- Ebenfalls behoben: die Kandidatenbeschriftung „CHF Budget 2025" war unabhaengig
  vom uebergebenen Jahr fest verdrahtet, obwohl die Budgetkurse je Jahr geladen
  werden.

### 2f. `Materialien` zaehlte Gruppen, nicht Materialien

`DistinctMaterialCount = rows.Count`, wobei die Zeilen nach
`{MaterialKey, Material, Bezeichnung, Land, TSC, Quelle, Waehrung}` gruppiert
sind. Ein Material aus drei Standorten zaehlte dreifach — angezeigt als
`Materialien` und zusaetzlich als Volumenfaktor im Entscheidungsscore.

Neu: die Kachel heisst `Pruefzeilen` (nur so summieren sich die Statuskacheln
daneben wieder auf diesen Wert), und die echte Zahl steht als Untertitel
(`DistinctMaterialNumberCount`). Die Spaltenueberschrift der Landtabelle ebenso.

Zweite Stelle: der Rollup auf Sparten-Ebene in der GUI addierte bereits distinkte
Zaehlungen (`group.Sum(row => row.MaterialCount)`) und doppelte damit jedes
Material aus mehreren Familien oder Waehrungen. Neu tragen die Zeilen ihre
`MaterialKeys` mit, und der Rollup vereinigt sie.

### 2g. Eine echte Null im Pivot wurde als „kein Wert" gezeigt

`GetFinancePivotValue` gab bei Wert `0` `null` zurueck, was ueber
`FormatNullableValue` als `-` rendert — also wie fehlende Daten. Eine Zelle, in
der Rechnung und Gutschrift sich aufheben, ist aber ein gemessenes Ergebnis.
Jetzt liefert `-` nur noch der fehlende Schluessel.

### 2h. Die 1000er-Kappung war unsichtbar

`GroupMarginDetailRows` und `FinanceAuditLedgerRows` sind auf `.Take(1000)`
begrenzt. Das Pruefbuch verspricht „die pruefbaren Detailzeilen", zeigte aber
hoechstens 1'000 von rund 92'000, ohne Hinweis. Neu tragen beide Tabellen den
Hinweis „Gezeigt werden N von M Detailzeilen" — Muster uebernommen aus
`Components/Pages/SupplyChainAnalysis.razor`. Beim Pruefbuch steht zusaetzlich,
dass die Kappung **vor** den Spaltenfiltern wirkt.

### 2i. Zwei Zeilen, kein eigener Abschnitt

- `StandardCostTotal` faellt bei Menge `0` auf den **Stueckpreis** zurueck, hiess
  aber „Quantity * Standard cost". Der Feldname nennt die Ausnahme jetzt.
- Der Entscheidungsradar schrieb `ex.Message` in die Spalte `Kennzahl`, direkt
  neben Zahlenwerten. Die Meldung steht jetzt in der Wirkung, die Kennzahl ist
  ehrlich `-`.

## 3. Produktivmessung: die Sollwerte fehlen fuer das Standardjahr

Read-only gegen eine **Kopie** der Produktiv-DB (`trafag_exporter.db`,
`339'210'240` Bytes, `2026-08-07 08:49`), Sonde `.tmp_tools/FinanceRefProbe`:

| `FinanceReferences` | Zeilen | mit Wert | ohne Wert |
| --- | ---: | ---: | ---: |
| Jahr `2025` | **17** | 14 | 3 |
| Jahr `2026` | **0** | 0 | 0 |

Zeilen je Finance-Jahr (Naeherung ueber `PostingDate` → `InvoiceDate` →
`ExtractionDate`, ohne die laenderspezifischen Regeln): **2025 `60'222`**,
**2026 `35'841`**, je 9 Standorte.

Das Standardjahr der Seite ist das **letzte Jahr in den Daten**, also **2026** —
und fuer 2026 existiert kein einziger Sollwert. Beim Standardaufruf des Cockpits
stehen `Laender OK` und `Zu pruefen` deshalb beide auf `0`, der Reiter
Abweichungen ist leer, und die Finance-Aeste des Entscheidungsradars entfallen
vollstaendig. Ohne die neue dritte Kachel war das von „alles reconciled" nicht zu
unterscheiden.

Zusaetzlich fehlt der Sollwert selbst fuer 2025 bei drei Laendern: **`CH`, `CN`
und `RU`** stehen ohne `LocalCurrencyValue` und ohne `CheckValue`. `CH` ist im
juengsten Jahr mit **`17'608` Zeilen** der groesste Standort und wird damit gegen
nichts geprueft. Zeilen je TSC im juengsten Jahr: TRCH `17'608`, TRIT `7'697`,
TRIN `3'096`, TRDE `2'712`, TRES `1'344`, TRUK `1'173`, TRFR `949`, TRAT `682`,
TRUS `580`.

**Das ist eine Datenluecke, kein Codefehler.** Der Codefix macht sie sichtbar;
gepflegt werden muessen die Sollwerte fachlich (Andreas).

## 4. Nur berichtet, nicht geaendert — Fachfragen

- **SAP-Proformabelegarten `F5`/`F8` laufen in den Umsatz.** Gemessen an
  `Finance_Dashboard_Audit_All_2026-07-29.csv`, TSC `TRCH`:

  | Belegart | Zeilen | Summe `SalesPriceValue` |
  | --- | ---: | ---: |
  | `F2` (Rechnung) | 43'074 | 125'505'067.05 |
  | **`F8`** | **1'902** | **+6'049'560.28** |
  | **`F5`** | **194** | **+497'752.51** |
  | `L2` | 19 | +97'638.45 |
  | `G2`/`S1`/`S2` (Gutschrift/Storno) | 1'197 | −3'440'179.69 |

  `121` distinkte `F8`-Belege, **keine** Ueberschneidung mit `F2` nach
  `InvoiceNumber` oder `DocumentEntry` — es sind keine Dubletten. Keine
  `FinanceRule` filtert auf `DocumentType`. Ob Proforma in die Net Sales gehoert,
  ist eine Finanzentscheidung. Verschaerfend: `CH` hat keinen Sollwert
  (Abschnitt 3), die 6.5 Mio. koennen im Soll/Ist also gar nicht auffallen.
  Die Zahlen wurden zweimal unabhaengig gezaehlt und stimmen ueberein. **Vor
  einem Versand an Andreas trotzdem gegen den aktuellen Datenstand neu ableiten**
  — Regel aus `lastchange.md`, Muster hinter UK-2025 und dem IT-Superlativ.
- **Die Toleranz `Math.Abs(difference) <= 1m`** entscheidet ueber `OK` gegen
  `Pruefen` und ist waehrungsblind: 1 CHF und 1 INR sind dieselbe Toleranz. Sie
  steht dreifach — `ManagementCockpitService`, `FinanceReconciliationService`,
  `ExcelExportService` (dort als Excel-Formel `ABS(F)<=1`).
- **Gutschriftenerkennung**: die Schluesselwortliste traegt kaum. Gemessen:
  `CRN` `265` Zeilen, `G2` `647`, `S1`/`S2` `610` werden von keinem Schluesselwort
  erfasst — `1'522` von `1'674` Gutschriftzeilen haengen allein an `Value < 0`.
  Der eine Fall, in dem die Belegart zaehlen wuerde (Gutschriftbeleg mit
  positivem Betrag), wird verfehlt; im Bestand gibt es genau `1` solche Zeile
  (`TRUS|CRN`). Zusaetzlich matcht `"rec"` als blosser Teilstring global, obwohl
  die spanische Belegart `REC` gemeint ist.

## 5. Ausdruecklich NICHT gemessen

Strukturell moeglich, Auswirkung auf die Produktivzahlen **nicht gemessen** und
deshalb nicht mitgeaendert:

- `ResolveFinanceCountryKey` existiert zweimal und weicht bei Spanien ab:
  `TRSE` ergibt im Cockpit `ES`, im Abgleichsdienst `SE`. Im aktuellen Bestand
  steht `TRES`, nicht `TRSE` — heute wirkungslos.
- `ResolveFinanceCurrency` setzt die Waehrung aus einer festen
  Land-Waehrungs-Tabelle und ignoriert `SalesCurrency` der Zeile.
- `FinanceRuleEngine.ShouldInclude` veraendert beim Dedup Zustand; welche von
  mehreren gleichen Zeilen ueberlebt, haengt an der Lesereihenfolge. Summen
  bleiben stabil, die im Pruefbuch zitierte Belegzeile ist es nicht.
- `ManagementCockpitService` ist Singleton mit unsynchronisiertem Cache.
- Toter Code in `BuildSummary`: `SalesValueTotal` traegt den aggregierten Wert
  statt des Umsatzes, dazu fuenf nie gelesene Felder (`EstimatedCostTotal`,
  `EstimatedMarginTotal`, `EstimatedMarginPercent`, `ServiceSharePercent`,
  `MissingSupplierPercent`).

## 6. Deploy-Nachweis (2026-08-07 10:22 MESZ)

- Funktionscommits `0c8cff5` (Indikatoren) und `b2e7c4f` (verwaiste
  Uebersetzungen), Release-Build und Release-Testlauf `455/455` erfolgreich VOR
  dem Publish.
- `app_offline.htm` vor dem Publish gesetzt und danach auf
  `app_offline.htm.disabled` umbenannt.
- Publish ueber `dotnet publish -c Release -o \\trch-webapp-bidashboard...\BiDashboard$`,
  bewusst NICHT ueber `FolderProfile` (`DeleteExistingFiles=true`, im
  Zielverzeichnis liegen Produktiv-DB und alle `.bak`).
- `BiDashboard.dll` `07.08.2026 10:21:53`, `4'320'768` Bytes, SHA256
  `B43A9E4B49ADC3186A1DC7216F61E2C220BF5541C9A4180FBA9C51C7CA80E43D`;
  lokaler Release-Build und Server bitgleich.
- Wirknachweis in der ausgelieferten DLL: `FinanceCountryStatuses`,
  `IsExcludedByRule`, `MissingRateRowCount`, `MissingTscRowCount`,
  `DistinctMaterialNumberCount`, `MaterialKeys`, `GetAvailableReferenceYearsAsync`
  und `FinanceAuditLedgerTotalRowCount` enthalten; die Literale
  `Nicht geprueft`, `Jahresumsatz`, `Pruefzeilen`,
  `Finance-Regeln oder Wert 0` und `Ergebnis siehe Ampel` vorhanden;
  `YtdSalesChf` und `Passt gegen Soll` nicht mehr enthalten.
- **Zwischenbefund, der den zweiten Publish ausgeloest hat:** nach dem ersten
  Publish (`59398983…`) war `Passt gegen Soll` noch im Literalbereich — als
  verwaister Uebersetzungsschluessel in sechs Sprachen. Der Code verwendete ihn
  nicht mehr, aber die Behauptung blieb im Repo und im Artefakt auffindbar.
  Commit `b2e7c4f` entfernt die sechs Eintraege, danach der zweite Publish.
- Produktiv-DB in Laenge und Schreibzeit unveraendert: `339'210'240` Bytes,
  `07.08.2026 08:49:20`, vor und nach dem Deploy identisch.
- HTTPS `200`: Startseite (`68'411` Bytes, `10.41 s` kalt),
  `/management-cockpit` (`69'490`), `/finance-cockpit/vergleich` (`69'539`).

**Grenze dieses Nachweises, ausdruecklich:** beide Finance-Routen liegen hinter
dem Finance-Unlock und liefern von hier aus das **Passwortpanel**, nicht die
Seite. Geprueft: die Antwort enthaelt `Finance Cockpit` und `Passwort`, aber
weder `Schnelluebersicht` noch `Net Sales Actual` noch `Nicht geprueft`. Der
`200` belegt also, dass die Anwendung laeuft und die Routen erreichbar sind —
**nicht**, dass die geaenderten Kacheln richtig rendern. Dafuer ist ein
angemeldeter Sichtprueflauf durch Ingo noetig (Abschnitt 7).

## 7. Tests

`455/455` gruen (vorher `449`). Sechs neue Tests in
`ManagementCockpitServiceTests`:

| Test | Deckt ab | Gegenprobe |
| --- | --- | --- |
| `CountsCountriesWithoutReference_SoTheTilesDoNotHideThem` | 2a, Statusvertrag | gruen auch ohne Fix |
| `FinancePivot_FollowsTheCountryFilter` | 2c.1 | **rot ohne Fix** |
| `FinancePivot_CountsRowsItHadToDropForAMissingRate` | 2c.2 | Feld neu |
| `SeparatesRuleExclusionFromGenuineZeroValue` | 2d | **rot ohne Fix** |
| `CountsTheSameMaterialFromTwoSitesOnce` | 2f | **rot ohne Fix** |
| `FinancePivot_KeepsAMeasuredZeroInsteadOfDroppingIt` | 2g, Dienstseite | gruen auch ohne Fix |

Gegenprobe durchgefuehrt: mit zurueckgebauten Fixes scheitern **drei** der sechs
(`3 Fehler / 3 erfolgreich`), mit Fixes laufen alle `455` durch.

**Ehrlich zur Aussagekraft der drei uebrigen:** sie pinnen Vertraege unterhalb
von Aenderungen, die in der Razor sitzen und von einem Dienst-Test nicht
erreichbar sind — die dritte Kachel (2a) und `GetFinancePivotValue` (2g). Beide
Tests tragen einen Kommentar, der genau das sagt. Die GUI-Seite dieser beiden
Punkte ist **nur durch Sichtpruefung** abgedeckt, und die steht noch aus.

Der Fixture-Aufbau fuer 2d ist bewusst kreuzweise: eine **regelausgeschlossene
Zeile mit Betrag** (`CustomerName = "Trafag AG"`, Standardregel DE) und eine
**eingeschlossene Zeile mit Betrag 0**. Der bestehende Test
`AnalyzeFinanceSummaryAsync_Builds_Dashboard_Tab_Data` kann den Fehler nicht
aufdecken, weil dort die einzige Nullwertzeile zugleich die einzige
ausgeschlossene ist.

Nebenbefund aus dem Testlauf: zwei der neuen Tests fielen zuerst durch, weil das
Fixture keinen EUR-Kurs saet und der Pivot Zeilen ohne CHF-Kurs verwirft — genau
die Luecke, die 2c.2 sichtbar macht. Daraus wurde ein eigener Test
(`CountsRowsItHadToDropForAMissingRate`).

## 8. Lokalisierung

17 neue deutsche Schluessel, in `es`, `it`, `hi`, `sq`, `tr`, `tlh` ergaenzt
(102 Eintraege). Sechs verwaiste Eintraege zu „Passt gegen Soll" entfernt.

Wie beim Einkauf ist das eine technische Vollstaendigkeits- und
Platzhalterpruefung, **keine Zertifizierung durch muttersprachliche
Uebersetzer**.

Falle, die dabei zugeschlagen hat und fuer kuenftige Arbeiten gilt: ein
PowerShell-Skript mit nicht-ASCII-Text im Quelltext wird von PowerShell 5.1 als
Windows-1252 gelesen und scheitert am Parser (dieselbe Ursache wie bei
`docs/mails/Build-StandortMails.ps1`). Loesung hier: Uebersetzungen als
UTF-8-JSON, Skript rein ASCII, Datei explizit als UTF-8 eingelesen.

## 9. Offen

- **Sichtprueflauf durch Ingo** (angemeldet) auf `/management-cockpit`
  (Schnelluebersicht, Finance Summary, Finance Pivot, Datenqualitaet,
  Spartenanalyse, Gruppenmarge, Finance Pruefbuch) und
  `/finance-cockpit/vergleich`. Der HTTPS-Nachweis erreicht die Seiten nicht,
  siehe Abschnitt 6.
- **Sollwerte 2026 in `FinanceReferences` pflegen** (Andreas) — sonst bleibt der
  Soll/Ist-Abgleich im Standardjahr leer und die neue Kachel `Nicht geprueft`
  zaehlt alle Laender.
- **Sollwert fuer CH** (und CN, RU) klaeren: die groesste Gesellschaft wird
  aktuell gegen nichts geprueft.
- **Belegarten `F5`/`F8`** fachlich entscheiden, siehe Abschnitt 4.
- Die drei Fachfragen aus Abschnitt 4 und die fuenf ungemessenen Punkte aus
  Abschnitt 5 bleiben unveraendert offen.
