# TrafagSalesExporter / BiDashboard — Korrektur-Beispiele fuer LLM

Stand: 2026-06-19. Begleitdatei zu `LLM_REPO_SUMMARY.md`. Zweck: zeigt an
**konkreten, durchgespielten Beispielen**, wie eine Korrektur/ein Bugfix in
dieser App abläuft — als Muster fuer beliebige andere Korrekturen. Kein echter
Bugreport, sondern repraesentative Faelle pro Bereich.

## Wie eine Korrektur immer ablaeuft (Template)

1. **Symptom** praezise fassen (was ist falsch, wo sichtbar, welcher Bereich).
2. **Lokalisieren** ueber `LLM_REPO_SUMMARY.md`: Abschnitt 5b (Aufgabentyp →
   Dateien) und Katalog 6c (Service → Methode).
3. **Code anfordern**: „brauche `Service.MethodeXy`“ — nicht raten. Erst mit der
   echten Implementierung den Fix planen.
4. **Fix** nach Konventionen (Abschnitt 3 der Summary): Logik im Service, nicht
   im Razor; `IDbContextFactory`; `T(de,en)`; `CancellationToken`.
5. **Test** im Projekt `TrafagSalesExporter.Tests` ergaenzen/anpassen
   (pro Service meist eine `…Tests.cs`), `dotnet test TrafagSalesExporter.sln`
   gruen halten.
6. **Build/Deploy**-Fakten siehe Summary Abschnitt 7.

> In jedem Beispiel ist der gezeigte Code ein **Muster**. Vor dem Schreiben die
> reale Methode anfordern, da die Originalimplementierung mehr Sonderfaelle hat.

---

## Beispiel 1 — Finance: Kennzahl falsch berechnet (Waehrung/Soll-Ist)

**Symptom:** Im Finance Cockpit weicht „Net Sales Actual CHF“ ab; vermutet wird
ein falscher Umrechnungskurs (falsches Stichtagsdatum).

**Lokalisieren:** Summary 5b „Finance-Berechnung korrigieren“ →
`FinanceRuleEngine` + `IFinanceReconciliationService`; Waehrung
`ICurrencyExchangeRateService.ResolveRate(from,to,date?)`. Detailformeln:
`docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`,
Kursfluss: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`.

**Code anfordern:** `FinanceReconciliationService.<Methode die CHF rechnet>` und
`CurrencyExchangeRateService.ResolveRate`.

**Typischer Fix (Muster):** statt Heute-Datum das Belegdatum als
`effectiveDate` durchreichen:
```csharp
// falsch: rate = _rates.ResolveRate(rec.Currency, "CHF", DateTime.Today);
var rate = _rates.ResolveRate(rec.Currency, "CHF", rec.DocumentDate); // Belegdatum
var chf  = rec.NetSales * (rate ?? 1m);
```
**Test:** `FinanceReconciliationServiceTests` — Fall mit Beleg in Vorperiode +
abweichendem Tageskurs, erwartetes CHF gegen Kurs des Belegdatums pruefen.

**Stolperstellen:** `NormalizeCurrencyCode` vor `ResolveRate` (z. B. „CHF “ →
„CHF“); fehlender Kurs gibt `null` → bewusste Fallback-Entscheidung treffen,
nicht still `1m`.

---

## Beispiel 2 — Einkauf: falsche Spend-Summe (Filter/Loekz-MSTAE)

**Symptom:** Spend pro Lieferant zu hoch; geloeschte EKPO-Positionen
(`Loekz='L'`) bzw. gesperrte Materialien (`MSTAE`) werden mitgezaehlt, obwohl der
Filter „Loeschkennzeichen raus“ aktiv ist.

**Lokalisieren:** Summary 5b „Einkauf“ → `IPurchasingDashboardService.LoadAsync`;
Filtertyp `PurchasingDashboardFilter(ExcludeDeletedItems, ExcludeBlockedMaterials)`;
SQL-Helper `ActiveItemFilterSql(filter, itemAlias)` (siehe Summary 6b.2).

**Code anfordern:** `PurchasingDashboardService` — die Methode, die die
Spend-Matrix-SQL baut (z. B. `BuildSupplierYearSpendSql` o. ä.).

**Typischer Fix (Muster):** Helper auch im betroffenen Query wirklich anhängen:
```sql
-- WHERE ... fehlt die Filterbedingung:
WHERE k.Bedat BETWEEN @from AND @to
  AND {ActiveItemFilterSql(filter, "p")}   -- COALESCE(p.Loekz,'')='' AND COALESCE(p.Mstae,'')=''
```
**Test:** `PurchasingDashboardServiceTests` — Cache mit 1 aktiver + 1 geloeschter
Position; bei `ExcludeDeletedItems=true` darf nur die aktive in die Summe.

**Stolperstellen:** Einkauf liest aus dem **rohen SQLite-Cache**
(`PurchasingEkpoCache`), nicht aus `CentralSalesRecords`; `MSTAE` kommt aus MARA
und muss im Cache vorhanden sein, sonst greift `ExcludeBlockedMaterials` nicht.

---

## Beispiel 3 — Transformation: Waehrungscode wird nicht normalisiert

**Symptom:** Im zentralen Export tauchen `EUR ` / `eur` gemischt auf; eine Regel
„NormalizeCurrencyCode“ wirkt nicht.

**Lokalisieren:** Summary 5b „Feld-Mapping/Transformation“ →
`ITransformationStrategy`-Impls (in `Program.cs` registriert),
`NormalizeCurrencyCodeTransformationStrategy`, Anwendung
`IRecordTransformationService.Apply(records, rules)`, Katalog `TransformationCatalog`.

**Code anfordern:** `RecordTransformationService.Apply` und die betroffene
Strategy-Klasse.

**Typischer Fix (Muster):** Regel ist nicht im Katalog/DI registriert oder
`RuleScope`/`Key` passt nicht zum Feld:
```csharp
// Program.cs — Strategy muss als Singleton registriert sein:
builder.Services.AddSingleton<ITransformationStrategy, NormalizeCurrencyCodeTransformationStrategy>();
```
**Test:** `TransformationStrategiesTests` / `TransformationCatalogTests` — Input
`"eur "` → Output `"EUR"`.

**Stolperstellen:** Reihenfolge der Regeln; eine spaetere Copy-Regel kann das
normalisierte Feld wieder ueberschreiben.

---

## Beispiel 4 — Export: Spalte fehlt / falsche Formatierung in Excel

**Symptom:** In der zentralen Excel fehlt eine Spalte oder ein Datum steht als
Text statt als Datum.

**Lokalisieren:** Summary 5b „Export“ → `IExcelExportService`
(`CreateConsolidatedExcelFile` / `CreateDashboardProofExcelFile`), ClosedXML.

**Code anfordern:** `ExcelExportService.CreateConsolidatedExcelFile`.

**Typischer Fix (Muster):** Zelle typisiert setzen + Zahlenformat:
```csharp
ws.Cell(row, col).Value = rec.DocumentDate;          // DateTime, nicht ToString()
ws.Cell(row, col).Style.DateFormat.Format = "dd.MM.yyyy";
```
**Test:** `ExcelExportServiceTests` — erzeugte Datei oeffnen, Zellwert/Typ der
neuen Spalte pruefen.

**Stolperstellen:** Spaltenindex-Verschiebung zieht Header und alle Folgespalten
mit; Header und Datenschleife konsistent halten.

---

## Beispiel 5 — Navigation: neuer/falsch einsortierter Menuepunkt

**Symptom:** Ein Menuepunkt erscheint nicht oder an falscher Stelle / mit
falschem Icon.

**Lokalisieren:** Summary 5b „Menue/Navigation“ → `NavigationMenuItems` (DB) via
`INavigationMenuService`; Default-Seed in `DatabaseSeedService`; Icons
`NavigationIconResolver`; UI `Admin > Menuestruktur`.

**Code anfordern:** `NavigationMenuService.GetItemsAsync`/`SaveItemsAsync` und
den Menue-Seed in `DatabaseSeedService`.

**Typischer Fix (Muster):** Seed-Eintrag ergaenzen/korrigieren
(`ParentKey`/`SortOrder`/`Icon`); bei bereits initialisierter DB greift Seed nur,
wenn fehlend → ggf. `ResetToDefaultsAsync` oder gezielt per Admin-UI umhaengen.

**Stolperstellen:** Route in `@page` muss existieren, sonst zeigt der Menuepunkt
ins Leere; `IsVisible`/Berechtigung pruefen.

---

## Beispiel 6 — UI/Text: fehlende oder falsche Uebersetzung

**Symptom:** Ein Label erscheint nur deutsch / ein englischer Text ist falsch.

**Lokalisieren:** Summary 5b „Uebersetzung/Text“ → `T("de","en")` in der `.razor`;
zentrale Texte `IUiTextService`.

**Typischer Fix (Muster):**
```razor
@* vorher: <MudText>Lieferant</MudText> *@
<MudText>@T("Lieferant", "Supplier")</MudText>
```
**Test:** `UiTextServiceTests` fuer zentrale Texte. **Stolperstelle:** Technische
SAP-Feldnamen (EKKO, MATNR, Entity-Sets) bewusst NICHT uebersetzen.

---

## Beispiel 7 — Datenmodell: neues Feld/Spalte sauber ergaenzen

**Symptom:** Eine Auswertung braucht ein Feld, das es in der Entitaet/Tabelle
noch nicht gibt (z. B. `Incoterm` auf `CentralSalesRecord`).

**Lokalisieren:** Summary 5b „DB-Feld/Tabelle hinzufuegen“ → Entity in `Models/`
+ DbSet in `AppDbContext` + Schema in
`DatabaseInitializationService(.SchemaSql).cs` (kein klassisches Migrations-
Setup; `DatabaseSchemaMaintenanceService.EnsureSchema` sichert die Spalte beim
Start nach).

**Typischer Fix (Muster):**
```csharp
// Models/CentralSalesRecord.cs
public string? Incoterm { get; set; }
// DatabaseInitializationService.SchemaSql.cs — Spalte additiv anlegen:
// ALTER TABLE CentralSalesRecords ADD COLUMN Incoterm TEXT NULL;  (idempotent/IF NOT EXISTS-Logik beachten)
```
**Test:** `DatabaseInitializationServiceTests` — nach Init existiert die Spalte;
alte DB ohne Spalte wird nachgezogen (Schema-Maintenance).

**Stolperstellen:** SQLite kann Spalten nur additiv per `ALTER TABLE ADD COLUMN`;
keine destructive Aenderung. Seed/Reader, die `SELECT *` erwarten, mitziehen.

---

## Kurz-Checkliste vor „fertig“

- [ ] Logik im Service, nicht im Razor; DbContext via Factory.
- [ ] Neue Services/Strategies in `Program.cs` registriert (richtige Lifetime).
- [ ] Texte `T(de,en)`; technische Feldnamen unveraendert.
- [ ] Test ergaenzt; `dotnet test TrafagSalesExporter.sln` gruen.
- [ ] Bei DB-Aenderung: additiv + Schema-Maintenance greift.
- [ ] Reale Methode war angefordert (nicht geraten).
