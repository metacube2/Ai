# Finance-Anzeige: vollstaendige Durchsicht 2026-08-06

Stand: 2026-08-06, deployed 11:06

Anlass: nach dem Zusammenfuehren der Gruppenmargen-Rechnung in eine Klasse
(`GroupMarginCalculator`, Deploy 2026-08-06 09:41) die Frage, ob damit auch alle
**Anzeigen** im Finance-Bereich stimmen. Diese Datei haelt fest, was geprueft wurde,
was daneben lag und was bewusst offen bleibt.

## 1. Der Hauptfund: Pruefbuch wies den vollen Umsatz als Marge aus

`ManagementCockpitService.BuildFinanceAuditLedgerRows` liess die Marge nur leer, wenn die
**Waehrungsmaske** griff:

```csharp
decimal? margin = conversion.IsMasked ? null : row.Value - conversion.CostBasis;
```

Eine **fehlende** Kostenbasis laeuft aber als `0` durch die Rechnung. `row.Value - 0` ist der
volle Umsatz. Die Spalte `Marge CHF` und `MarginPercent` zeigten deshalb 100 % — direkt neben
dem Statusfeld, das fuer dieselbe Zeile „Lieferant unklar" oder „Konzernkosten fehlen" sagte.
Der Kommentar ueber der Zeile beschrieb bereits das richtige Verhalten („sonst bleibt sie
leer"); der Code tat es nicht.

Betroffen:

| Ort | Vorher | Jetzt |
| --- | --- | --- |
| Pruefbuch-Tabelle im Cockpit, Spalte `Marge CHF` | voller Umsatz | leer |
| Excel-Export `Finance_Pruefbuch`, Blatt `Finance Pruefbuch` | voller Umsatz | leer |
| Zentraler Excel-Nachweis, Blatt `Gruppenmarge Details` | **war korrekt** | unveraendert |

Der zentrale Excel-Nachweis war nie betroffen, weil die Marge dort als Blattformel mit
`WENN(Status = OK)` steht — nicht als vorberechneter Wert.

### Warum `IsOpen` als Pruefung nicht reicht

Naheliegend waere gewesen, auf `GroupMarginStatuses.IsOpen` zu pruefen. Das waere falsch:

- **Kostenbasis unbekannt** (`Standardpreis fehlt`, `Lieferant unklar`, `Konzernkosten fehlen`):
  Kosten sind 0, jede Marge daraus ist Unsinn.
- **Kostenwaehrung abweichend**: die Kostenbasis IST bekannt, nur in einer anderen Waehrung als
  der Umsatz. Die Marge in Originalwaehrung bleibt offen (man wuerde Waehrungen mischen), die
  **CHF-Marge bleibt korrekt rechenbar**, weil beide Seiten einzeln nach CHF umgerechnet werden.

Beide Faelle sind „offen", nur der erste ist „ohne Kostenbasis". Deshalb neu
`GroupMarginStatuses.IsCostBasisKnown`. Der Unterschied ist durch einen bereits vorhandenen
Test gepinnt (`AnalyzeFinanceSummaryAsync_CostCurrencyMismatch_Masks_Margin_By_Default`
erwartet dort weiterhin `MarginChf = 43`) — er war der Pruefstein dafuer, dass der Fix nicht
zu grob greift.

### Groessenordnung

Naeherung ueber alle Jahre und **ohne** den `Include`-Filter (in SQL nicht nachbildbar),
gemessen am Produktivbestand 2026-08-06:

| Naeherung | Zeilen |
| --- | --- |
| ohne belastbare Kostenbasis | **~71'900** |
| davon `Lieferant unklar` | 71'718 |
| davon `Konzernkosten fehlen` | 137 |
| mit Kostenbasis | 24'204 |
| gesamt | 96'059 |

Also rund **drei Viertel aller Zeilen**. Der Schwerpunkt liegt bei TRCH, TRDE, TRES und TRAT,
deren Quellsysteme kein Lieferantenfeld liefern (siehe
`docs/FINANCE_SUPPLIER.md`) — nicht bei Indien.

## 2. Weitere Anzeigekorrekturen

- **Statusfarbe** (`ManagementCockpit.razor`): stand als eigene Aufzaehlung neben
  `GroupMarginStatuses.Open` und kannte `Kostenwaehrung abweichend` nicht — der Status
  erschien blau statt orange, obwohl die Kennzahl „offene Kostenbasis" ihn mitzaehlt. Die
  Farbe folgt jetzt `IsOpen`, also der Statusdefinition selbst.
- **Hinweistext im Gruppenmarge-Tab** und **Hinweis im Finance-Ergebnis**
  (`notices`): beide beschrieben noch die MVP-Regel und behaupteten, die echten
  Konzern-Standardkosten seien „noch nicht angebunden" — seit 2026-08-05 falsch. Jetzt die
  tatsaechliche Regelkette.
- **Schulungsseite `Finance > Grundlagen`**: erklaerte `Konzernkosten fehlen` gar nicht.
  Jetzt in der Statustabelle, mit der Abgrenzung zu `Standardpreis fehlt` — dort ist gar kein
  Preis da, hier ist einer da, aber es ist der IC-Einkaufspreis.
- **Kachel „Kostenbasis"** heisst wie die Tabellenspalte „Bekannte Kostenbasis": die Summe
  enthaelt offene Zeilen mit 0.
- **Detailtabelle**: Literal `"OK"` durch `GroupMarginStatuses.Ok` ersetzt. Sie prueft bewusst
  strenger als `IsCostBasisKnown` (naemlich auf `Ok`), weil sie Marge in **Verkaufswaehrung**
  zeigt und die maskierte Kostenbasis in Fremdwaehrung vorliegt.

## 3. Geprueft und in Ordnung

| Bereich | Befund |
| --- | --- |
| Kacheln „Laender OK" / „Zu pruefen" | Literale `OK`/`Pruefen` passen zum Erzeuger `BuildFinanceStatus`. Haetten sie nicht gepasst, stuende dort still eine 0 — das sieht aus wie saubere Daten, nicht wie ein Fehler. |
| Gruppenmarge Summary/Land/Sparte | maskieren ueber `MissingCostRows`, das aus `IsOpen` kommt |
| Gruppenmarge Detailtabelle | maskiert ueber Status |
| Datenqualitaet, Gutschriftkandidaten | reine Zaehlungen |
| Sparten-/Produktfinanzen | nur Umsatzsummen, `ProductAssignmentStatuses`-Konstanten statt Literale |
| Finance-Pivot | enthaelt keine Kostenlogik |
| `BuildFinanceSummaryRow` | ausgeschlossene Zeilen tragen Wert 0 (`ResolveNetSalesActual` gibt bei `include = false` 0 zurueck), die Summe ueber alle Zeilen ist daher gleich der Summe ueber die eingeschlossenen |

## 4. Nebenbefunde ohne Handlungsbedarf

- **Deckungsbeitrag ist ueberall „-"**, weil KEIN Standort einen fix/variabel-Split liefert:
  `StandardCostVariable` ist in 0 von 96'059 Zeilen gesetzt (alle neun TSC gemessen). Die
  Anzeige ist korrekt, das Feature heute aber wirkungslos.
- **`EstimatedMarginTotal`** im aelteren Cockpit-Teil rechnet Umsatz minus geschaetzte Kosten
  und traegt dasselbe Muster in sich, wird aber nirgends angezeigt oder exportiert (toter Code
  in `ManagementCockpitService` und `ManagementCockpitModels`).

## 5. Offen

### 5a. Statustext `"OK"` steht als Zeichenkette in der Excel-Formel (nicht behoben)

**Wo:** `Services/ExcelExportService.cs`, Blatt „Gruppenmarge Details", die beiden
Formelzuweisungen fuer Marge und Marge-%:

```csharp
ws.Cell(rowIndex, 19).FormulaA1 = $"IF(B{rowIndex}=\"OK\",Q{rowIndex}-R{rowIndex},\"\")";
ws.Cell(rowIndex, 20).FormulaA1 = $"IF(B{rowIndex}=\"OK\",IF(Q{rowIndex}=0,\"\",S{rowIndex}/Q{rowIndex}),\"\")";
```

**Das Problem:** die Formel vergleicht gegen das Literal `"OK"`, nicht gegen
`GroupMarginStatuses.Ok`. Wird der Statustext dort je umbenannt, trifft die Bedingung nie mehr
zu und **saemtliche Margen im zentralen Excel-Nachweis bleiben still leer**. Es gibt keine
Warnung dafuer:

- der Compiler sieht nur einen String,
- die Tests lesen Zellwerte, und ClosedXML wertet Formeln nicht aus — die Formel wird also
  geschrieben, aber nie gerechnet,
- `GroupMarginConsistencyTests` vergleicht Status, Lieferantentyp, Kostenquelle und
  Kostenbasis, NICHT die Marge (die stimmt bisher als Folge dieser vier ueberein).

Es ist dieselbe Fehlerklasse wie die Statusfarbe und die doppelte Statusliste, nur in einem
Formelstring, wo kein Werkzeug sie findet.

**Fix (klein):** die Konstante in die Formel interpolieren, z. B.

```csharp
$"IF(B{rowIndex}=\"{GroupMarginStatuses.Ok}\",Q{rowIndex}-R{rowIndex},\"\")"
```

**Nachweis danach:** einen Test ergaenzen, der die Zelle nicht auswertet, sondern ihren
`FormulaA1`-Text liest und prueft, dass `GroupMarginStatuses.Ok` darin vorkommt. Damit ist die
letzte Stelle abgedeckt, an der ein Statustext ausserhalb von `GroupMarginStatuses` steht.

**Warum nicht sofort erledigt:** der Deploy vom 2026-08-06 11:06 war zu diesem Zeitpunkt schon
draussen; die Aenderung soll mit dem naechsten Deploy mitgehen, nicht einen eigenen ausloesen.
Bis dahin besteht KEIN akutes Fehlverhalten — der Statustext heisst `OK`, die Formel stimmt.
Es ist eine Falle fuer die naechste Umbenennung, kein aktueller Defekt.

### 5b. Weitere offene Punkte

- Die Maskierung bei abweichender Kostenwaehrung (`status == OK && conversion.IsMasked`) steht
  weiterhin an drei Aufrufstellen einzeln statt im Rechner. Alle drei tun dasselbe (geprueft),
  aber es ist die letzte gespiegelte Stelle in der Rechnung.
- Geprueft wurde der Finance-Bereich. Einkauf, Logistik und Stammdaten sind nicht Teil dieser
  Durchsicht.

## Quellen

- Code: `Services/GroupMarginStatuses.cs`, `Services/ManagementCockpitService.cs`,
  `Components/Pages/ManagementCockpit.razor`, `Components/Pages/FinanceTraining.razor`
- Tests: `GroupMarginCalculatorTests.Fehlende_Kostenbasis_und_abweichende_Kostenwaehrung_sind_nicht_dasselbe`,
  `ManagementCockpitServiceTests.AnalyzeFinanceSummaryAsync_AuditLedger_LeavesMarginOpen_WhenCostBasisIsMissing`
- Vorgeschichte der Rechnung: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 7d
