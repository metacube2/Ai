# HR-KPI-Cockpit: Formel-Review und Korrekturauftrag (2026-07-06)

Zweck: Arbeitsauftrag fuer die naechste Umsetzungs-Session (analog
`docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md`). Ergebnis eines Formel-/Logik-Reviews der
HR-KPI-Dokumentation gegen den tatsaechlichen Code. Jede Korrektur hat Prioritaet, Fundstelle,
Begruendung und konkreten Fix.

Gepruefte Dateien:

- `docs/rag/HR_KPI.md`, `docs/HR_KPI_NACHDOKU_2026-05-13.md`,
  `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` (Doku/Formeln)
- `Services/HrKpi/HrKpiDashboardBuilder.cs` (alle Berechnungen)
- `Services/HrKpiService.cs`, `Models/HrKpiModels.cs`
- `Components/HrKpi/HrKpiDashboardTabs.razor` (Anzeige)
- `TrafagSalesExporter.Tests/HrKpiServiceTests.cs`

Arbeitsregeln fuer die Umsetzung:

- Nach jeder Korrektur: `dotnet test TrafagSalesExporter.sln --verbosity minimal` gruen.
- Neue Logik mit Tests absichern (analog bestehender `HrKpiServiceTests`).
- `docs/HR_KPI_NACHDOKU_2026-05-13.md` per Nachtrag aktualisieren, nicht umschreiben.
- Kein Deploy ohne Ruecksprache.

---

## Gesamtbewertung des Ansatzes

Der Ansatz ist solide und deutlich reifer als beim ersten Review 2026-05-13: Fluktuation nach
`formeln.docx` (relevante AN-Kuendigungen / durchschnittlicher Headcount, Koepfe statt FTE),
Distinct-Zaehlung nach Personalnummer, Monats-Headcount als Mittel aus Monatsanfang/-ende,
Umlaut-/Encoding-Robustheit beim Excel-Import, Testpersonen-Ausschluss und Datenqualitaets-Tab
sind sauber umgesetzt. Die Tooltips beschreiben die Formeln korrekt.

Aber: **Der Periodenvergleich (Vorjahresvergleich) ist strukturell falsch, sobald ein
Austrittsjahr gewaehlt ist** — der Hauptanwendungsfall. Und die **Krankenquote hat zwei
Nenner-Probleme** (laufendes Jahr zaehlt zukuenftige Arbeitstage; ohne Zeitraumfilter passt der
21-Tage-Nenner nicht zum Dateiinhalt). Details unten, sortiert nach Prioritaet.

---

## H1 (HOCH, echter Bug): Vorjahresvergleich zeigt immer 0, wenn ein Austrittsjahr gewaehlt ist

**Fundstelle:** `Services/HrKpi/HrKpiDashboardBuilder.cs`, `BuildPeriodComparisonMetrics`
(ca. Zeile 631-665), aufgerufen mit `result.Leavers` (= `ApplyLeaverFilters`-Ergebnis).

**Problem:** `ApplyLeaverFilters` filtert die Austritte bereits auf das gewaehlte Austrittsjahr
bzw. Von/Bis (`MatchesLeaverDateFilter`). `BuildPeriodComparisonMetrics` zaehlt anschliessend
`previousLeavers` mit `Austrittsjahr == previousYear` **aus dieser bereits jahresgefilterten
Liste** — bei gesetztem Jahr 2025 enthaelt die Liste nur 2025er-Austritte, also ist das Vorjahr
2024 **immer 0 Austritte / 0.0 % Fluktuation**, und `Delta Fluktuation` entspricht dann einfach
der aktuellen Rate. Der Vergleich wird prominent im Ueberblick und im Periodenvergleichs-Tab
angezeigt (`HrKpiDashboardTabs.razor:19,86`).

Zweiter Teil desselben Bugs: Die Headcount-Intervalle fuer den Vergleich kommen aus
`turnoverHeadcountLeavers`, die per `MatchesLeaverEmploymentPeriodFilter` auf Ueberlappung mit dem
**gewaehlten** Zeitraum gefiltert sind. Austritte vor dem 01.01. des gewaehlten Jahres fehlen in
den Intervallen, dadurch wird der rekonstruierte Vorjahres-Headcount zu klein und die (ohnehin
falsche) Vorjahresrate zusaetzlich verzerrt.

**Korrektur:**
1. Fuer den Periodenvergleich eine **eigene, nicht datumsgefilterte** Austrittsliste verwenden:
   nur Organisation/Mitarbeitertyp/Eintrittsjahr/Suche anwenden (analog
   `ApplyTurnoverEmployeeFilters`), aber kein Jahr/Von-Bis. Konkret: in `Build` eine dritte Liste
   `comparisonLeavers` erzeugen und an `BuildPeriodComparisonMetrics` uebergeben.
2. Die Intervalle fuer den Vergleich aus derselben ungefilterten Liste bauen, damit Vorjahres-
   Headcounts alle damals Beschaeftigten enthalten.
3. Test: Seed mit Austritten in 2024 und 2025, Filter `Year = 2025` -> Kachel
   `Austritte 2025` korrekt, `Vorjahr` muss die 2024er-Werte zeigen (nicht 0).

---

## H2 (HOCH, konzeptionell): Krankenquote-Nenner falsch fuer laufendes Jahr und ohne Zeitraumfilter

**Fundstellen:**
- `ResolveEmploymentPeriod` (Jahr -> 01.01. bis 31.12.) + `ResolveAnalysisPeriod` (Workdays aus
  voller Periode), `HrKpiDashboardBuilder.cs` ca. Zeile 1109-1135.
- `BuildOverviewMetrics`/`BuildAbsenceMetrics`: `absenceRate = sickDays / (fte * Workdays)`.
- Ohne Zeitraumfilter: `Workdays = 21` pauschal.

**Problem:**
1. **Laufendes Jahr:** Bei `Austrittsjahr = 2026` (heute 06.07.2026) zaehlt der Nenner die
   Arbeitstage des **ganzen** Jahres (~261), waehrend der Zaehler nur die bis heute angefallenen
   Krankheitsstunden enthaelt. Die Krankenquote wird dadurch etwa um Faktor 2 unterschaetzt —
   und schrumpft scheinbar, je frueher im Jahr man schaut.
2. **Ohne Zeitraumfilter:** Nenner = 21 Arbeitstage (ein Monat), Zaehler = alle Zeilen der
   Absenzdatei. Enthaelt die Datei mehr als einen Monat (typisch: Jahresexport), ist die Quote
   massiv ueberhoeht; enthaelt sie weniger, unterschaetzt. Der bestehende Hinweis beschreibt das,
   aber die Kachel zeigt trotzdem einen als Prozent formatierten Wert, der so nicht stimmt.

**Korrektur:**
1. In `ResolveAnalysisPeriod` das Periodenende auf `DateTime.Today` kappen, wenn es in der
   Zukunft liegt (gilt fuer Jahr- und Von/Bis-Auswahl).
2. Ohne Zeitraumfilter: Periode aus der Absenzdatei ableiten (min `VonDatum` bis max `BisDatum`,
   gekappt auf heute), statt pauschal 21 Tage. Nur wenn die Datei keine Datumsfelder hat, auf die
   21-Tage-Naeherung zurueckfallen und die Kachel als Naeherung labeln ("~, Annahme 1 Monat").
3. `KrankenquoteMa` (pro Zeile `tage / 21`, angezeigt in `HrKpiDashboardTabs.razor:122`) auf
   denselben Perioden-Nenner umstellen — oder die Spalte in "Krankheitstage" umbenennen, wenn die
   Pro-Person-Quote ohne Sollzeit nicht sauber bestimmbar ist.
4. Test: Jahr = laufendes Jahr -> Workdays = Arbeitstage 01.01. bis heute; vergangenes Jahr ->
   volles Jahr.

---

## M3 (MITTEL): Top-Absenzen ranken Zeilen statt Personen

**Fundstelle:** `Build`, `result.CriticalAbsences` (ca. Zeile 119-129).

**Problem:** Die Top-25-Liste sortiert einzelne **Absenzzeilen** nach `KrankheitstageGesamt`.
Eine Person mit vielen kleineren Absenzzeilen (in Summe hoch) erscheint zu tief oder mehrfach;
das Ranking "kritische Absenzen" ist damit nicht das Ranking der tatsaechlich am staerksten
betroffenen Personen.

**Korrektur:** Erst pro `Personalnummer` aggregieren (Summe Krankheitstage), dann Top 25 Personen
bilden und die Mitarbeiterzeile dazu aufloesen. Duplikate pro Person verschwinden automatisch.

---

## M4 (MITTEL): Vergleichszaehlung inkonsistent — Zeilen statt Distinct-Personen

**Fundstelle:** `BuildPeriodComparisonMetrics`, `selectedAbs`/`previousAbs`
(`leavers.Count(x => x.Austrittsjahr == ...)`).

**Problem:** Die dokumentierte Regel lautet "Fluktuationsvisuals zaehlen Austritte distinct nach
Personalnummer statt Zeilen" — genau diese zwei Kacheln zaehlen aber Zeilen. Bei Personen mit
Mehrfachzeilen weicht `Austritte <Jahr>` von den uebrigen Kacheln ab.

**Korrektur:** `CountDistinctPersons(...)` wie ueberall sonst verwenden. (Wird automatisch Teil
des H1-Umbaus, dort mitfixen und testen.)

---

## M5 (MITTEL): Zwei "Fluktuations"-Kacheln mit verschiedenen Nennern fuer dasselbe Jahr

**Fundstellen:** `BuildOverviewMetrics` (Nenner = `ResolveTurnoverDenominator` = Durchschnitt
ueber **alle 12 Monate** des gewaehlten Jahres) vs. `Fluktuation YTD` in `BuildTurnoverMetrics`
(Nenner = Durchschnitt **01.01. bis Stichtag**).

**Problem:** Beim laufenden Jahr zeigen die Ueberblick-Kachel `Fluktuation <Jahr>` und die
Fluktuations-Kachel `Fluktuation YTD` fuer denselben Zaehler leicht unterschiedliche Prozentwerte,
weil der eine Nenner zukuenftige Monate (mit heutigem Bestand approximiert) einschliesst. Das ist
kein Rechenfehler, aber fuer HR nicht erklaerbar, wenn zwei Kacheln "dasselbe" zeigen sollen.

**Korrektur (Entscheid noetig, Empfehlung: Variante a):**
a) Ueberblick-Kachel beim laufenden Jahr ebenfalls auf den YTD-Nenner umstellen (konsistent zur
   wichtigsten Kachel), oder
b) Detailtext der Ueberblick-Kachel praezisieren ("Nenner: Jahresdurchschnitt inkl. Restjahr").

---

## M6 (MITTEL): SAP-Duplikate werden stillschweigend verworfen

**Fundstelle:** `LoadSapRows` (`GroupBy(...).First()`), analog `LoadTimeRows`.

**Problem:** Bei mehreren SAP-Zeilen pro Personalnummer gewinnt die erste; BU-/NBU-Tage der
uebrigen Zeilen gehen verloren, ohne Hinweis. Der Datenqualitaets-Tab meldet doppelte
Personalnummern nur fuer die Mitarbeiderdatei, nicht fuer SAP/Zeitdatei.

**Korrektur:** Datenqualitaets-Issue "Doppelte SAP-Personalnummer" (Count der Gruppen > 1)
ergaenzen; fachlich klaeren, ob BU/NBU bei Duplikaten summiert werden muessen (dann `Sum` statt
`First` fuer die Tages-Felder).

---

## M7 (MITTEL): Join-Trefferquote fuer den Namens-Join (#732) fehlt weiterhin

**Fundstelle:** `LoadEmployees` (`timeRows.TryGetValue(NormalizeKey(name), ...)`).

**Problem:** Der wichtigste technische Pruefpunkt aus
`HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` (Punkt 12) ist offen: Der Name-Join zur Zeitdatei ist
fehleranfaellig, aber es gibt keine Kennzahl, wie viele aktive Mitarbeitende **keinen**
Zeitdatei-Treffer haben (Folge: Geburtsdatum/Arbeitszeitmodell/FTE-Fallback leer).

**Korrektur:** Beim Laden zaehlen, wie viele Mitarbeitende keinen `TimeRow`-Treffer haben, und als
Datenqualitaets-Issue "Rexx ohne Zeitdatei (Name-Join)" ausweisen (analog "Rexx ohne SAP").

---

## M8 (MITTEL): Anker-Datum bei reinem Von-Filter ist der Zeitraum-Anfang

**Fundstelle:** `ResolveTurnoverAnchorDate` (ca. Zeile 1026-1044): `FromDate` wird als Anker
verwendet, wenn kein `ToDate` gesetzt ist.

**Problem:** Der Anker bestimmt Stichtag/aktuellen Monat/Quartal der Periodenkacheln. Bei nur
gesetztem `Von Austritt` (z.B. 01.03.2026, bis offen) rechnen Monats-/Quartals-/YTD-Kacheln auf
den **Beginn** des Zeitraums statt auf heute — YTD endet dann im Maerz, obwohl Austritte bis heute
gezaehlt werden (Zaehler/Nenner-Fenster passen nicht zusammen).

**Korrektur:** Bei nur `FromDate`: Anker = `min(DateTime.Today, 31.12. des Anker-Jahres)` bzw.
schlicht `DateTime.Today`. Test mit nur-Von-Filter.

---

## L9 (NIEDRIG): Kleinere Punkte, in einem Aufwasch

1. **Feiertage:** `CountWeekdays` zaehlt Mo-Fr inkl. CH-Feiertage -> Krankenquoten-Nenner leicht
   zu gross. Mindestens dokumentieren; optional fixe CH-Feiertagsliste (TG/SG) abziehen.
2. **FTE-Fallback-Klammer 1.2:** `Clamp(avgSollzeit/8.4, 0.1, 1.2)` erlaubt FTE > 1.0. Pruefen, ob
   gewollt (Mehrarbeitsmodelle); sonst auf 1.0 kappen und im Hinweistext ergaenzen.
3. **Ferien bezogen:** negative Werte werden pro Person auf 0 gekappt (`Ferientage`), die Summe
   "Ferien bezogen" ist dadurch leicht geschoent; besser die Kachel aus den Summen der Rohwerte
   rechnen (`Sum(Anspruch) - Sum(Rest) - Sum(Ausstehend)`).
4. **Tote Variable:** `hasRange` in `ResolveTurnoverPeriodScope` wird nie verwendet — entfernen.
5. **`ParseInt`/String-Roundtrip in `BuildTrafficLights`:** Ampeln parsen formatierte Strings
   zurueck (CurrentCulture). Robuster: numerische Werte im `HrKpiMetric` mitfuehren.
6. **Absenz-Statusfilter:** `LoadAbsences` behaelt nur `Status == "Aktiv"` — Absenzen von im Jahr
   ausgetretenen Personen fehlen in Jahresbetrachtungen. Fuer die aktuelle Definition ok, aber in
   der Doku/Kachel als Abgrenzung ausweisen ("nur aktuell Aktive").
7. **Prognose-Kachel:** `Quartalsrate x 4` ist frueh im Quartal stark verzerrt (6 Tage Quartal
   -> Hochrechnung fast 0). Spec-konform zu `formeln.docx`, aber empfehlenswert: zusaetzlich
   YTD-annualisiert (`YTD-Rate * 365 / verstrichene Tage`) als zweite Prognose oder Hinweis im
   Tooltip "zu Quartalsbeginn wenig aussagekraeftig".

---

## Bereits korrekt (gegen Doku verifiziert, keine Aenderung)

- Fluktuationsformeln Monat/Quartal/YTD entsprechen `formeln.docx` inkl. Headcount-Durchschnitt
  aus Monatsanfang/-ende und Koepfe-statt-FTE.
- Distinct-Zaehlung nach Personalnummer in Kacheln, Funnel, Timeline und Org-Gruppen (Ausnahme
  M4).
- Ausschlusslogik (Praktikant/Werkstudent/Aushilfe/Lehrling, befristet, Pension/Rente/Ruhestand,
  AG-Kuendigung/Trafag) inkl. Umlaut-/Encoding-Normalisierung; `Kuendigung AN`/`AG`/`Ruhestand`
  werden korrekt klassifiziert (durch Tests abgedeckt).
- Krankheitstage-Umrechnung `Stunden / 8.4` und Kurz-/Lang-Trennung wie dokumentiert (fachliche
  Bestaetigung 8.4h und "nicht buchbar = lang" bleibt offener HR-Punkt, kein Codefehler).
- Keine hartcodierten Jahresgrenzen (anders als im Einkaufsdashboard).

---

## Empfohlene Reihenfolge fuer die Umsetzung

1. **H1** (Periodenvergleich) — wichtigster sichtbarer Fehler, inkl. **M4** im selben Umbau.
2. **H2** (Krankenquote-Nenner kappen/ableiten) inkl. `KrankenquoteMa`-Spalte.
3. **M3** (Top-Absenzen pro Person) — kleiner, isolierter Fix.
4. **M8** (Anker bei nur-Von) und **M5** (Nenner-Konsistenz, Entscheid a/b).
5. **M6/M7** (Datenqualitaets-Issues SAP-Duplikate + Name-Join-Quote).
6. **L9**-Sammelposten.

Nach Abschluss: Nachtrag in `docs/HR_KPI_NACHDOKU_2026-05-13.md` und `docs/rag/HR_KPI.md`
(Datum, Was/Warum, Testresultat), Tests gruen, Deploy-Entscheid mit Ingo.

## Validierung nach Umsetzung (Abnahme-Checks)

- `dotnet test TrafagSalesExporter.sln --verbosity minimal` — alle Tests gruen.
- Kontrollwerte aus der Nachdoku bleiben unveraendert: Austritte total 104, `Kündigung AN` 42,
  fluktuationsrelevant 33, Avg Headcount 2025 ≈ 211.3, Fluktuation 2025 ≈ 15.6 %
  (H1/H2 aendern Vorjahresvergleich und Krankenquote, nicht diese Werte).
- Periodenvergleich: Jahr 2025 gewaehlt -> Vorjahreskacheln zeigen echte 2024er-Werte, nicht 0.
- Krankenquote: laufendes Jahr -> Nenner endet heute; Quote steigt nicht scheinbar zum
  Jahresende hin ab.
- Stichprobe 10 Mitarbeitende (Personalnummer, Organisation, FTE, GLZ, Ferien Rest,
  Krankheitstage) gegen Rexx/SAP — offener Punkt aus der Best-Practices-Doku, gilt weiterhin.
