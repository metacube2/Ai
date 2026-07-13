# Codex-Anweisung: Finance Daten-Heartbeat (Datenkontinuitaet je Land)

Stand: 2026-07-13
Auftraggeber: Ingo Kohler
Ziel-Repo: `TrafagSalesExporter` (publiziert als `BiDashboard`), .NET/Blazor Server, MudBlazor.

## 1. Ziel / Fachlicher Zweck

Neuer Reiter im Management-Cockpit (Finance Dashboard), der **je Land/TSC grafisch zeigt, ob die taeglichen Daten-Updates lueckenlos durchgelaufen sind**.

- Pro Land ein Linien-/Flaechendiagramm: X-Achse = Kalendertag, Y-Achse = Anzahl Buchungszeilen dieses Tages.
- Fehlt ein Tag (Update nicht gelaufen oder Zeilen fehlen), faellt die Linie sichtbar auf 0 ein — die Luecke muss sofort ins Auge springen.
- Wichtigste fachliche Unterscheidung: **"Update nicht gelaufen" vs. "an dem Tag gab es schlicht keine Buchungen"** (Wochenende/Feiertag). Ein normaler Sonntag darf NICHT als Fehler erscheinen.

## 2. Verbindliche Rahmenbedingungen (nicht verhandelbar)

1. **Keine neue Chart-Library.** Im Projekt gibt es keine Chart-Bibliothek; bestehende Grafiken sind handgebautes Inline-SVG (siehe `Components/Pages/Dashboard.razor` Manometer, `Components/Pages/ExportDashboard.razor`). Der Heartbeat-Graph wird als Inline-SVG direkt im Razor gerendert (Polyline/Path + Rechtecke). Kein JS-Interop noetig.
2. **Zweisprachige UI** ueber den vorhandenen `T("Deutsch", "English")`-Helper wie in `ManagementCockpit.razor` ueberall verwendet.
3. **Alle bestehenden Tests muessen gruen bleiben** (`dotnet test TrafagSalesExporter.sln`), neue Logik bekommt eigene Unit-Tests in `TrafagSalesExporter.Tests`.
4. **Bestehende Reiter-Indizes nicht verschieben.** Der neue Tab wird ans Ende der Experten-Reiter gehaengt; `ManagementFinanceTabIndexes` (in `ManagementCockpit.razor`, ca. Zeile 2977) nur ergaenzen, nie umnummerieren.
5. Datenquelle ist **dieselbe zentrale Quelle wie die uebrigen Finance-Reiter** (Audit-CSV bevorzugt, Fallback DB) — NICHT direkt `CentralSalesRecords` abfragen, sondern ueber den bestehenden Provider-Weg gehen, den auch `AnalyzeFinanceSummaryAsync`/Finance Pivot in `Services/ManagementCockpitService.cs` nutzen. Nur so zeigt der Graph wirklich das, was das Dashboard anzeigt.
6. Keine Schema-/DB-Migrationen.

## 3. Vorhandene Bausteine (wiederverwenden, nicht neu erfinden)

| Baustein | Ort | Nutzen |
| --- | --- | --- |
| Tagesaggregation je TSC | `Services/ManagementCockpitService.cs`, `BuildFinancePivotDailyYearRowsByTsc` (ca. Zeile 1552) und `DailyTotals` (ca. Zeile 295) | Vorlage fuer Gruppierung nach Tag+TSC; Datumsbasis dort ist das Finance-Datum (`PostingDate`, Fallback `InvoiceDate`, dann `ExtractionDate`) |
| Datums-Fallback-Logik | ebd. (Finance-Jahresabgrenzung) | identisch uebernehmen, damit Heartbeat und Pivot dieselben Tage zaehlen |
| Frische-Zeitstempel je Standort | `Services/ManagementCockpitService.cs` ca. Zeile 700–770 (`LatestStoredAtUtc`, `LatestExtractionDate`, `DashboardPageService.ResolveDataFreshness`) | fuer die Zusatzinfo "letztes Update" je Land. Achtung: bei Audit-CSV-Quelle ist `StoredAtUtc` teils null (Zeile 709) — dann `ExtractionDate` verwenden |
| Tab-/Section-Routing | `ManagementCockpit.razor` ca. Zeile 1794–1806 (`"ledger" => ...`) | neuen Section-Key `"heartbeat"` analog mappen |
| Navigations-Seed | `Services/DatabaseSeedService.cs` ca. Zeile 194–203 | neuer `Link("finance-heartbeat", "experts", "Daten-Heartbeat", "Data heartbeat", "MonitorHeart", "management-cockpit?section=heartbeat", 140, "All")` |
| Excel-Export pro Reiter | `IExcelExportService.CreateWorkbookBytes` + JS `trafagDownload.saveBytes` (Muster: jeder bestehende Reiter) | auch der neue Reiter bekommt einen `Export to Excel`-Button |

## 4. Umsetzung im Detail

### 4.1 Neues Ergebnismodell (`Models/ManagementCockpitModels.cs`)

```
ManagementDataHeartbeatResult
  List<ManagementDataHeartbeatCountryRow> Countries
  DateTime RangeStart / RangeEnd

ManagementDataHeartbeatCountryRow
  string Tsc, string Country, string CurrencyHint
  List<ManagementDataHeartbeatDay> Days
  DateTime? LastUpdateUtc          // max(StoredAtUtc, ExtractionDate) je TSC
  int GapCount                     // Anzahl Tage mit Status Gap
  string OverallStatus             // "Ok" | "Warn" | "Gap"

ManagementDataHeartbeatDay
  DateOnly Date
  int RowCount
  decimal Value                    // Nettowert des Tages (Lokalwaehrung), fuer Tooltip
  HeartbeatDayStatus Status        // Ok, WeekendOrNoBusiness, Gap, Future
```

### 4.2 Statuslogik (Kern der Aufgabe, unbedingt testen)

Fuer jeden Tag im Betrachtungsfenster (Default: letzte 60 Tage, per Dropdown 30/60/90/laufendes Jahr):

1. `RowCount > 0` → **Ok**.
2. `RowCount == 0` und Tag ist Samstag/Sonntag → **WeekendOrNoBusiness** (neutral grau, KEIN Fehler).
3. `RowCount == 0` und Werktag → zunaechst **Gap** (rot).
4. Toleranz gegen Fehlalarm: Manche Laender (z. B. UK Manual Excel, ES Range-Dateien) liefern nicht jeden Tag Buchungen. Deshalb je Land eine einfache Heuristik: Wenn im Betrachtungsfenster weniger als 40 % der Werktage ueberhaupt Buchungen haben, gilt das Land als "nicht-taeglich" und einzelne Werktags-Nullen werden **Warn** (gelb) statt **Gap** (rot). Schwellwert als Konstante mit Kommentar, keine Konfiguration noetig.
5. Tage nach dem letzten vorhandenen Datentag bis heute: wenn `LastUpdateUtc` aelter als 2 Kalendertage ist, die fehlenden Werktage als **Gap** markieren (das ist der Timer-Ausfall-Fall vom 2026-07-07).
6. Zukunftstage → **Future** (nicht gerendert).

### 4.3 Service (`Services/ManagementCockpitService.cs`)

Neue Methode `AnalyzeDataHeartbeatAsync(int windowDays)`:
- Records ueber denselben Weg holen wie die Finance-Summary (zentrale Quelle inkl. Audit-CSV-Schalter).
- Gruppierung: TSC → Finance-Tagesdatum → `Count()` + `Sum(Wert)`.
- Statuslogik aus 4.2 anwenden. Statuslogik als **pure, statische, testbare Methode** implementieren (Eingabe: Liste (Datum, RowCount) + LastUpdate + Fenster; Ausgabe: Days mit Status) — analog zum Stil der bestehenden `Build...`-Methoden.

### 4.4 UI (`Components/Pages/ManagementCockpit.razor`)

- Neuer `MudTabPanel` "Daten-Heartbeat" / "Data heartbeat" (Icon `MonitorHeart`) am Ende der Experten-Tabs.
- Pro Land eine Karte (`MudPaper`): Kopfzeile Flagge/TSC/Land + Badge (Ok gruen / Warn gelb / X Luecken rot) + "Letztes Update: <Datum/Zeit>".
- Darunter das SVG: Flaechen-/Linienpfad der RowCounts; unter der X-Achse pro Tag ein schmales Status-Rechteck (gruen/grau/gelb/rot) als "Heartbeat-Streifen" — so ist die Luecke auch erkennbar, wenn die Linie wegen Skalierung flach wirkt. `<title>`-Tooltip je Tagespunkt: Datum, Zeilen, Wert, Status.
- Y-Achse je Land autoskaliert; X-Achse mit Monatsmarken.
- Dropdown Betrachtungsfenster (30/60/90 Tage/laufendes Jahr), Default 60.
- Sortierung: Laender mit Gaps zuerst, dann alphabetisch.
- `Export to Excel`-Button: ein Blatt je Land oder ein Blatt gesamt mit Spalten TSC, Datum, Zeilen, Wert, Status.

### 4.5 Navigation + Routing

- Section-Key `"heartbeat"` im Switch (ca. Zeile 1794) auf den neuen Tab-Index mappen.
- Seed-Link wie in Abschnitt 3 beschrieben. Seed-Reparaturlogik beachten: neue Links werden beim App-Start ergaenzt (Muster vorhandener Links uebernehmen).

## 5. Tests (TrafagSalesExporter.Tests)

Mindestens:
1. Werktag ohne Zeilen → Gap; Samstag/Sonntag ohne Zeilen → WeekendOrNoBusiness.
2. Land mit <40 % Werktagsabdeckung → Werktags-Null wird Warn, nicht Gap.
3. LastUpdate 3 Tage alt → Folgetage bis heute als Gap.
4. Luecke mitten in sonst taeglichem Land → genau diese Tage Gap, GapCount korrekt.
5. Datums-Fallback: Record ohne PostingDate faellt auf InvoiceDate/ExtractionDate zurueck (konsistent zum Pivot).
6. Fensterlogik: Tage ausserhalb windowDays erscheinen nicht.

## 6. Abnahmekriterien

- `dotnet test TrafagSalesExporter.sln` komplett gruen (Bestand + neu).
- Reiter erscheint unter `Management Analyse > Experten > Daten-Heartbeat`, Deep-Link `management-cockpit?section=heartbeat` funktioniert.
- Bei aktivem Audit-CSV-Modus (`UseAuditCsvAsCentralSource=1`) zeigt der Graph die CSV-Datenlage, nicht die DB.
- Ein fehlender Werktag in einem taeglich liefernden Land ist auf einen Blick als roter Einbruch + rotes Streifen-Segment sichtbar.
- Sonntage erzeugen keinen roten Alarm.
- UI vollstaendig zweisprachig (DE/EN via `T()`).
- Keine neuen NuGet-/JS-Abhaengigkeiten.

## 7. Doku-Pflicht nach Umsetzung

- `docs/rag/FINANCE.md` und `docs/rag/PROJECT.md`: Kurzeintrag (Reiter, Zweck, Statuslogik, Seed-Key).
- `lastchange.md` aktualisieren.

## 8. Umsetzungsnotiz 2026-07-13

Umgesetzt, getestet, committed und produktiv published.

- Feature-Commit: `abc59e3 Add finance data heartbeat`.
- Routing-Fix: `2cf227c Fix finance heartbeat tab routing` (`section=heartbeat` springt auf den richtigen Experten-Tab).
- Live-Fix fuer falsche Luecken: `aff78dd Fix finance heartbeat gap logic`.
- Tests: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `163/163` gruen.
- Publish: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$`; `app_offline.htm` danach entfernt, Port 443 erreichbar.

Wichtige Abweichung zur urspruenglichen Statuslogik in Abschnitt 4.2: Die `<40% Werktagsabdeckung`-Heuristik wurde nach Live-Befund entfernt. Ein Werktag ohne Buchungszeilen ist nicht automatisch eine Datenluecke, weil buchungsfreie Tage je Land/TSC normal sein koennen. Die produktive Logik trennt deshalb Import-Freshness von Buchungsaktivitaet:

1. `RowCount > 0` -> `Ok`.
2. `RowCount == 0`, Quelle ist frisch -> `WeekendOrNoBusiness`/neutral, auch an Werktagen.
3. Kein Freshness-Zeitstempel und Datum liegt nach dem letzten Datentag -> `Warn`.
4. Letztes Update aelter als 2 Kalendertage und Datum liegt nach dem letzten Datentag -> `Gap`.
5. `LatestStoredAtUtc` ist primaer; wenn nicht vorhanden, wird je TSC die maximale `ExtractionDate` als Fallback fuer `Letztes Update` genutzt.
6. `TRES`/`TRSE` wird als Spanien (`ES`) normalisiert.
