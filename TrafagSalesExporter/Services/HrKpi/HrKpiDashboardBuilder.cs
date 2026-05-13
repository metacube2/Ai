using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

internal sealed class HrKpiDashboardBuilder
{
    private readonly HrKpiDataSourceOptions _dataSources;

    public HrKpiDashboardBuilder(HrKpiDataSourceOptions dataSources)
    {
        _dataSources = dataSources.Normalize();
    }

    public HrKpiResult Build(HrKpiOptions options)
    {
        var normalizedOptions = new HrKpiOptions
        {
            DataFolder = string.IsNullOrWhiteSpace(options.DataFolder) ? _dataSources.DataFolder : options.DataFolder.Trim(),
            Year = options.Year <= 0 ? DateTime.Today.Year : options.Year,
            FromDate = options.FromDate?.Date,
            ToDate = options.ToDate?.Date,
            EntryYear = options.EntryYear,
            Organisationseinheit = NormalizeFilter(options.Organisationseinheit),
            KostenstelleText = NormalizeFilter(options.KostenstelleText),
            Mitarbeitertyp = NormalizeFilter(options.Mitarbeitertyp),
            FluktuationFilter = NormalizeFilter(options.FluktuationFilter),
            GlzAmpel = NormalizeFilter(options.GlzAmpel),
            RestferienAmpel = NormalizeFilter(options.RestferienAmpel),
            SearchText = NormalizeFilter(options.SearchText)
        };

        var result = new HrKpiResult { Options = normalizedOptions };
        var context = new ImportContext(result, normalizedOptions.DataFolder);

        var timeRows = LoadTimeRows(context);
        var sapRows = LoadSapRows(context);
        var employees = LoadEmployees(context, timeRows, sapRows);
        var absences = LoadAbsences(context);
        var leavers = LoadLeavers(context);

        result.OrganisationOptions = employees
            .Select(x => x.Organisationseinheit)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        result.KostenstelleOptions = employees
            .Select(x => x.KostenstelleText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        result.EntryYearOptions = employees
            .Where(x => x.Eintrittsdatum.HasValue)
            .Select(x => x.Eintrittsdatum!.Value.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();
        result.MitarbeitertypOptions = employees
            .Select(x => x.Mitarbeitertyp)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var analysisYear = ResolveAnalysisYear(normalizedOptions);
        var filteredEmployees = ApplyEmployeeFilters(employees, normalizedOptions).ToList();
        var filteredEmployeeNumbers = filteredEmployees
            .Where(x => x.Personalnummer.HasValue)
            .Select(x => x.Personalnummer!.Value)
            .ToHashSet();

        employees = filteredEmployees;
        absences = ApplyAbsenceFilters(absences, normalizedOptions, filteredEmployeeNumbers).ToList();
        leavers = ApplyLeaverFilters(leavers, normalizedOptions).ToList();

        result.Employees = employees;
        result.Absences = absences;
        result.Leavers = leavers;
        result.Metrics = BuildOverviewMetrics(employees, absences, leavers, analysisYear);
        result.TurnoverMetrics = BuildTurnoverMetrics(employees, leavers, analysisYear, ResolveTurnoverAnchorDate(normalizedOptions, analysisYear));
        result.AbsenceMetrics = BuildAbsenceMetrics(employees, absences);
        result.TimeVacationMetrics = BuildTimeVacationMetrics(employees);
        result.TurnoverVisuals = BuildTurnoverVisuals(employees, leavers, analysisYear);
        result.HeadcountByOrganisation = employees
            .GroupBy(x => BlankAsUnknown(x.Organisationseinheit), StringComparer.OrdinalIgnoreCase)
            .Select(g => new HrKpiGroupValue
            {
                Label = g.Key,
                Count = g.Select(x => x.Personalnummer).Distinct().Count(),
                Value = g.Sum(x => x.Fte)
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
        result.CriticalTimeBalances = employees
            .OrderByDescending(x => Math.Abs(x.StundenSaldo))
            .Take(25)
            .ToList();
        result.FluctuationRelevantLeavers = leavers
            .Where(x => x.IstFluktuationsrelevant)
            .OrderByDescending(x => x.Austrittsdatum)
            .Take(25)
            .ToList();

        if (employees.Count == 0)
            result.Notices.Add($"Keine aktiven Mitarbeitenden geladen. Pruefe {_dataSources.MainFile} und die Filter.");
        var missingEmployeeNumberCount = employees.Count(x => !x.Personalnummer.HasValue);
        if (missingEmployeeNumberCount > 0)
            result.Notices.Add($"{missingEmployeeNumberCount:N0} aktive Mitarbeitendenzeilen ohne Personalnummer werden in Headcount-Distinct-Kennzahlen nicht mitgezaehlt.");
        var missingFteCount = employees.Count(x => !x.BeschaeftigungsgradProzent.HasValue);
        if (missingFteCount > 0)
            result.Notices.Add($"{missingFteCount:N0} aktive Mitarbeitendenzeilen ohne SAP-Beschaeftigungsgrad verwenden einen FTE-Fallback aus Rexx-Arbeitszeitmodell/Sollzeit.");
        if (!context.HasFile(_dataSources.MainFile))
            result.Notices.Add($"Hauptdatei fehlt: {_dataSources.MainFile}. Ohne diese Datei sind keine HR-KPIs moeglich.");
        if (!context.HasFile(_dataSources.SapFile))
            result.Notices.Add($"SAP-Datei {_dataSources.SapFile} fehlt. SAP-only Felder wie Geschlecht, Beschaeftigungsgrad, BU/NBU und Planstelle bleiben leer.");
        if (!context.HasFile(_dataSources.AbsenceFile))
            result.Notices.Add("Rexx-Absenzen fehlen. Absenzquote und Krankheitstage bleiben 0.");
        if (!context.HasFile(_dataSources.LeaverFile))
            result.Notices.Add("Rexx-Austritte fehlen. Fluktuationskennzahlen bleiben 0.");

        return result;
    }

    private List<HrKpiEmployeeRow> LoadEmployees(
        ImportContext context,
        IReadOnlyDictionary<string, TimeRow> timeRows,
        IReadOnlyDictionary<string, SapRow> sapRows)
    {
        return context.ReadRows(_dataSources.MainFile, "Rexx #757 Saldi", (row, headers) =>
        {
            var personalnummer = ReadInt(row, headers, "Personalnummer");
            var name = ReadString(row, headers, "Nachname, Vorname (Link Personal)", "Name_Rexx");
            var key = BuildPersonalKey(personalnummer);
            timeRows.TryGetValue(NormalizeKey(name), out var time);
            if (!sapRows.TryGetValue(key, out var sap) && personalnummer.HasValue)
                sapRows.TryGetValue(personalnummer.Value.ToString(CultureInfo.InvariantCulture), out sap);

            var entryDate = ReadDate(row, headers, "Eintrittsdatum");
            var birthDate = time?.Geburtsdatum;
            var status = ReadString(row, headers, "Personal Status", "Personal_Status");
            var rawBalance = ReadString(row, headers, "Stunden Saldo", "Stunden_Saldo_Raw");
            var balance = ParseTimeBalance(rawBalance);
            var percent = sap?.BeschaeftigungsgradProzent;
            var arbeitzeitmodell = time?.Arbeitszeitmodell ?? string.Empty;
            var fte = ResolveFte(percent, arbeitzeitmodell, time?.AvgSollzeitTag);

            var nameParts = SplitName(name);
            var urlaubsanspruch = ReadDecimal(row, headers, "Urlaubsanspruch", "Urlaubsanspruch_Raw");
            var urlaubRest = ReadDecimal(row, headers, "Urlaub Rest", "Urlaub_Rest_Raw");
            var ferienAusstehend = ReadDecimal(row, headers, "Ferien ausstehend (Tage)", "Ferien_Ausstehend_Raw");
            var ferienBezogen = urlaubsanspruch - urlaubRest - ferienAusstehend;

            return new HrKpiEmployeeRow
            {
                Personalnummer = personalnummer,
                NameVoll = name,
                Vorname = nameParts.Vorname,
                Nachname = nameParts.Nachname,
                Organisationseinheit = ReadString(row, headers, "Organisation", "Organisation_Text"),
                KostenstelleText = ReadString(row, headers, "Kostenstelle", "Kostenstelle_Rexx"),
                Kostenstelle = ParseCostCenter(ReadString(row, headers, "Kostenstelle", "Kostenstelle_Rexx")),
                Stelle = ReadString(row, headers, "Stelle", "Stelle_Rexx"),
                Leitung = ReadString(row, headers, "Leitung j/n", "Leitung"),
                Eintrittsdatum = entryDate,
                Geburtsdatum = birthDate,
                AlterJahre = YearsSince(birthDate),
                Altersgruppe = BuildAgeGroup(YearsSince(birthDate)),
                GeschlechtText = MapGender(sap?.Geschlecht),
                BeschaeftigungsgradProzent = percent,
                Fte = fte,
                IstTeilzeit = percent.HasValue && percent.Value > 0
                    ? percent.Value < 100
                    : string.Equals(arbeitzeitmodell, "Teilzeit", StringComparison.OrdinalIgnoreCase),
                Dienstjahre = YearsSince(entryDate),
                IstAktiv = string.Equals(status, "Aktiv", StringComparison.OrdinalIgnoreCase),
                Mitarbeitertyp = BuildEmployeeType(ReadString(row, headers, "Stelle", "Stelle_Rexx")),
                StundenSaldo = balance,
                GlzAmpel = BuildTrafficLight(balance),
                UrlaubRest = urlaubRest,
                Urlaubsanspruch = urlaubsanspruch,
                FerienAusstehend = ferienAusstehend,
                Ferientage = ferienBezogen < 0 ? 0 : ferienBezogen,
                RestferienAmpel = urlaubRest <= 5 ? "Gruen" : "Rot",
                Bruttolohn = ReadDecimal(row, headers, "Lohn", "Lohn_Raw"),
                LohnWaehrung = ReadString(row, headers, "Lohn Waehrung", "Lohn WÃ¤hrung"),
                BuTage = sap?.BuTage ?? 0,
                NbuTage = sap?.NbuTage ?? 0,
                Buchungskreis = sap?.Buchungskreis ?? string.Empty,
                Personalbereich = sap?.Personalbereich ?? string.Empty,
                Personalteilbereich = sap?.Personalteilbereich ?? string.Empty,
                Mitarbeitergruppe = sap?.Mitarbeitergruppe ?? string.Empty,
                Mitarbeiterkreis = sap?.Mitarbeiterkreis ?? string.Empty,
                Planstelle = sap?.Planstelle ?? string.Empty,
                SollStelle = sap?.SollStelle ?? string.Empty,
                Periode = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };
        })
        .Where(x => x.IstAktiv)
        .OrderBy(x => x.Personalnummer ?? int.MaxValue)
        .ToList();
    }

    private Dictionary<string, TimeRow> LoadTimeRows(ImportContext context)
    {
        var rows = context.ReadRows(_dataSources.TimeFile, "Rexx #732 Kommen/Gehen", (row, headers) =>
        {
            var name = ReadString(row, headers, "Nachname, Vorname (Link Personal)");
            return new TimeRow(
                NormalizeKey(name),
                ReadDate(row, headers, "Geburtsdatum"),
                ReadString(row, headers, "Arbeitszeitmodell"),
                ReadDecimal(row, headers, "O taegliche Sollarbeitszeit (Woche)", "Ã˜ tÃ¤gliche Sollarbeitszeit (Woche)", "ÃƒËœ tÃƒÂ¤gliche Sollarbeitszeit (Woche)"));
        });

        return rows
            .Where(x => !string.IsNullOrWhiteSpace(x.NameKey))
            .GroupBy(x => x.NameKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, SapRow> LoadSapRows(ImportContext context)
    {
        var rows = context.ReadRows(_dataSources.SapFile, "SAP HR KPI", (row, headers) =>
        {
            var pernr = ReadInt(row, headers, "Personalnummer");
            return new SapRow(
                BuildPersonalKey(pernr),
                ReadString(row, headers, "Buchungskreis"),
                ReadString(row, headers, "Personalbereich"),
                ReadString(row, headers, "Personalteilbereich"),
                ReadString(row, headers, "Mitarbeitergruppe"),
                ReadString(row, headers, "Mitarbeiterkreis"),
                ReadString(row, headers, "Teilzeitkraft", "Teilzeitkennzeichen"),
                ReadDecimalNullable(row, headers, "Beschaeftigungsgrad %", "BeschÃ¤ftigungsgrad %", "BeschÃƒÂ¤ftigungsgrad %", "Beschaeftigungsgrad_Prozent"),
                ReadInt(row, headers, "Geschlecht"),
                ReadString(row, headers, "Planstelle"),
                ReadString(row, headers, "Stellenschluessel", "StellenschlÃ¼ssel", "StellenschlÃƒÂ¼ssel", "Soll_Stelle"),
                ReadDecimal(row, headers, "Nichtberufsunfall Tage", "NBU_Tage"),
                ReadDecimal(row, headers, "Berufsunfall Tage", "BU_Tage"),
                ReadString(row, headers, "Abrechnungskreis"));
        });

        return rows
            .Where(x => !string.IsNullOrWhiteSpace(x.PersonalKey))
            .GroupBy(x => x.PersonalKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    private List<HrAbsenceRow> LoadAbsences(ImportContext context)
    {
        return context.ReadRows(_dataSources.AbsenceFile, "Rexx #744 Absenzen", (row, headers) =>
        {
            var kurz = ReadDecimal(row, headers, "Krankheit angetreten (Stunden Ind.)", "Krankheit_Kurz_Std");
            var lang = ReadDecimal(row, headers, "Krank nicht buchbar angetreten (Stunden Ind.)", "Krankheit_Lang_Std");
            var gesamt = kurz + lang;
            var tage = Math.Round(gesamt / 8.4m, 1);
            return new HrAbsenceRow
            {
                Personalnummer = ReadInt(row, headers, "Personalnummer"),
                Name = ReadString(row, headers, "Nachname, Vorname (Link Personal)", "Name"),
                Organisationseinheit = ReadString(row, headers, "Organisation"),
                Stelle = ReadString(row, headers, "Stelle"),
                Status = ReadString(row, headers, "Personal Status", "Status"),
                KrankheitKurzStd = kurz,
                KrankheitLangStd = lang,
                KrankheitGesamtStd = gesamt,
                KrankheitstageGesamt = tage,
                KrankheitstageKurz = Math.Round(kurz / 8.4m, 1),
                KrankheitstageLang = Math.Round(lang / 8.4m, 1),
                KrankenquoteMa = tage == 0 ? 0 : tage / 21m
            };
        })
        .Where(x => string.Equals(x.Status, "Aktiv", StringComparison.OrdinalIgnoreCase))
        .ToList();
    }

    private List<HrLeaverRow> LoadLeavers(ImportContext context)
    {
        return context.ReadRows(_dataSources.LeaverFile, "Rexx #381 Ausgeschieden", (row, headers) =>
        {
            var name = ReadString(row, headers, "Nachname, Vorname (Link Personal)", "Name_Voll");
            var nameParts = SplitName(name);
            var austritt = ReadDate(row, headers, "Austrittsdatum");
            var eintritt = ReadDate(row, headers, "Eintrittsdatum");
            var stelle = ReadString(row, headers, "Stelle-1", "Stelle");
            var type = BuildEmployeeType(stelle);
            var reason = ReadString(row, headers, "Austrittsart");
            var normalizedReason = NormalizeReason(reason);
            var isEmployeeResignation =
                normalizedReason.Contains("arbeitnehmer", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("mitarbeiter", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("kuendigung an", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("an kuendigung", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("eigenkuendigung", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("kuendigung ma", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("kuendigung durch ma", StringComparison.OrdinalIgnoreCase);
            var isExcluded =
                !string.Equals(type, "Festangestellt", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("befrist", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("pension", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("rente", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("trafag", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("arbeitgeber", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("ag-kuendigung", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("ag kuendigung", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.Contains("kuendigung ag", StringComparison.OrdinalIgnoreCase);
            var isRelevant = isEmployeeResignation && !isExcluded;

            return new HrLeaverRow
            {
                Personalnummer = ReadInt(row, headers, "Personalnummer"),
                NameVoll = name,
                Vorname = nameParts.Vorname,
                Nachname = nameParts.Nachname,
                Organisationseinheit = ReadString(row, headers, "Organisation-1", "Organisationseinheit"),
                Stelle = stelle,
                Status = ReadString(row, headers, "Personal Status", "Status"),
                Austrittsdatum = austritt,
                Eintrittsdatum = eintritt,
                VerweildauerMonate = austritt.HasValue && eintritt.HasValue
                    ? Math.Round((decimal)(austritt.Value - eintritt.Value).TotalDays / 30.44m, 1)
                    : null,
                Austrittsart = reason,
                AustrittsartNormalisiert = normalizedReason,
                Mitarbeitertyp = type,
                IstArbeitnehmerkuendigung = isEmployeeResignation,
                IstFluktuationAusgeschlossen = isExcluded,
                IstFluktuationsrelevant = isRelevant,
                FluktuationAusschlussgrund = isRelevant ? null : BuildExclusionReason(type, normalizedReason, isEmployeeResignation),
                Austrittsmonat = austritt.HasValue ? new DateTime(austritt.Value.Year, austritt.Value.Month, 1) : null,
                Austrittsjahr = austritt?.Year
            };
        })
        .ToList();
    }

    private static IEnumerable<HrKpiEmployeeRow> ApplyEmployeeFilters(IEnumerable<HrKpiEmployeeRow> rows, HrKpiOptions options)
        => rows.Where(x => MatchesFilter(x.Organisationseinheit, options.Organisationseinheit) &&
                           MatchesFilter(x.KostenstelleText, options.KostenstelleText) &&
                           MatchesFilter(x.Mitarbeitertyp, options.Mitarbeitertyp) &&
                           MatchesFilter(x.GlzAmpel, options.GlzAmpel) &&
                           MatchesFilter(x.RestferienAmpel, options.RestferienAmpel) &&
                           (!options.EntryYear.HasValue || x.Eintrittsdatum?.Year == options.EntryYear.Value) &&
                           MatchesEmployeeSearch(x, options.SearchText));

    private static IEnumerable<HrAbsenceRow> ApplyAbsenceFilters(
        IEnumerable<HrAbsenceRow> rows,
        HrKpiOptions options,
        IReadOnlySet<int> filteredEmployeeNumbers)
        => rows.Where(x => MatchesFilter(x.Organisationseinheit, options.Organisationseinheit) &&
                           x.Personalnummer.HasValue &&
                           filteredEmployeeNumbers.Contains(x.Personalnummer.Value) &&
                           MatchesTextSearch(options.SearchText, x.Name, x.Personalnummer?.ToString(CultureInfo.InvariantCulture) ?? string.Empty));

    private static IEnumerable<HrLeaverRow> ApplyLeaverFilters(IEnumerable<HrLeaverRow> rows, HrKpiOptions options)
        => rows.Where(x => MatchesLeaverDateFilter(x, options) &&
                           MatchesFilter(x.Organisationseinheit, options.Organisationseinheit) &&
                           MatchesFilter(x.Mitarbeitertyp, options.Mitarbeitertyp) &&
                           MatchesFluctuationFilter(x, options.FluktuationFilter) &&
                           MatchesTextSearch(options.SearchText, x.NameVoll, x.Personalnummer?.ToString(CultureInfo.InvariantCulture) ?? string.Empty));

    private static List<HrKpiMetric> BuildOverviewMetrics(
        IReadOnlyCollection<HrKpiEmployeeRow> employees,
        IReadOnlyCollection<HrAbsenceRow> absences,
        IReadOnlyCollection<HrLeaverRow> leavers,
        int year)
    {
        var activeCount = CountDistinctPersons(employees.Select(x => x.Personalnummer));
        var fixedCount = CountDistinctPersons(employees
            .Where(x => string.Equals(x.Mitarbeitertyp, "Festangestellt", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Personalnummer));
        var fte = employees.Sum(x => x.Fte);
        var sickDays = absences.Sum(x => x.KrankheitstageGesamt);
        var absenceRate = activeCount == 0 ? 0 : sickDays / (activeCount * 21m);
        var relevantLeavers = CountDistinctPersons(leavers.Where(x => x.IstFluktuationsrelevant).Select(x => x.Personalnummer));
        var employeeLeavers = CountDistinctPersons(leavers.Where(x => x.IstArbeitnehmerkuendigung).Select(x => x.Personalnummer));
        var turnover = fixedCount == 0 ? 0 : relevantLeavers / (decimal)fixedCount;
        var avgBalance = activeCount == 0 ? 0 : employees.Average(x => x.StundenSaldo);
        var redBalance = employees.Count(x => x.GlzAmpel == "Rot");

        return
        [
            new() { Label = "Headcount aktiv", Value = activeCount.ToString("N0"), Detail = $"{fixedCount:N0} festangestellt", Severity = "Normal" },
            new() { Label = "FTE", Value = fte.ToString("N1"), Detail = "Summe Beschaeftigungsgrad", Severity = "Normal" },
            new() { Label = "Krankheitstage", Value = sickDays.ToString("N1"), Detail = $"Absenzquote {absenceRate:P1}", Severity = absenceRate > 0.05m ? "Warning" : "Normal" },
            new() { Label = $"Fluktuation {year}", Value = turnover.ToString("P1"), Detail = $"{relevantLeavers:N0} relevant von {employeeLeavers:N0} AN-Kuendigungen", Severity = turnover > 0.12m ? "Warning" : "Normal" },
            new() { Label = "GLZ Schnitt", Value = avgBalance.ToString("N1"), Detail = $"{redBalance:N0} Personen > 100h absolut", Severity = redBalance > 0 ? "Warning" : "Normal" },
            new() { Label = "Unfalltage", Value = employees.Sum(x => x.BuTage + x.NbuTage).ToString("N1"), Detail = $"BU {employees.Sum(x => x.BuTage):N1} / NBU {employees.Sum(x => x.NbuTage):N1}", Severity = "Normal" }
        ];
    }

    private static List<HrKpiMetric> BuildTurnoverMetrics(
        IReadOnlyCollection<HrKpiEmployeeRow> employees,
        IReadOnlyCollection<HrLeaverRow> leavers,
        int year,
        DateTime anchorDate)
    {
        var fixedHeadcount = CountDistinctPersons(employees
            .Where(x => string.Equals(x.Mitarbeitertyp, "Festangestellt", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Personalnummer));
        var totalLeavers = CountDistinctPersons(leavers.Select(x => x.Personalnummer));
        var employeeResignations = leavers
            .Where(x => x.IstArbeitnehmerkuendigung)
            .Select(x => x.Personalnummer)
            .ToList();
        var relevantLeavers = leavers
            .Where(x => x.IstFluktuationsrelevant)
            .Select(x => x.Personalnummer)
            .ToList();
        var nonRelevantLeavers = leavers
            .Where(x => !x.IstFluktuationsrelevant)
            .Select(x => x.Personalnummer)
            .ToList();
        var employeeResignationCount = CountDistinctPersons(employeeResignations);
        var relevantLeaverCount = CountDistinctPersons(relevantLeavers);
        var nonRelevantLeaverCount = CountDistinctPersons(nonRelevantLeavers);

        var currentMonth = anchorDate.Month;
        var currentQuarter = ((currentMonth - 1) / 3) + 1;
        var quarterLeavers = leavers
            .Where(x => x.IstFluktuationsrelevant &&
                        x.Austrittsdatum.HasValue &&
                        x.Austrittsdatum.Value.Year == year &&
                        ((x.Austrittsdatum.Value.Month - 1) / 3) + 1 == currentQuarter)
            .Select(x => x.Personalnummer)
            .ToList();
        var monthLeavers = leavers
            .Where(x => x.IstFluktuationsrelevant &&
                        x.Austrittsdatum.HasValue &&
                        x.Austrittsdatum.Value.Year == year &&
                        x.Austrittsdatum.Value.Month == currentMonth)
            .Select(x => x.Personalnummer)
            .ToList();
        var yearLeavers = leavers
            .Where(x => x.IstFluktuationsrelevant && x.Austrittsjahr == year)
            .Select(x => x.Personalnummer)
            .ToList();
        var quarterLeaverCount = CountDistinctPersons(quarterLeavers);
        var monthLeaverCount = CountDistinctPersons(monthLeavers);
        var yearLeaverCount = CountDistinctPersons(yearLeavers);

        var monthRate = fixedHeadcount == 0 ? 0 : monthLeaverCount / (decimal)fixedHeadcount;
        var quarterRate = fixedHeadcount == 0 ? 0 : quarterLeaverCount / (decimal)fixedHeadcount;
        var forecastRate = quarterRate * 4;
        var yearRate = fixedHeadcount == 0 ? 0 : yearLeaverCount / (decimal)fixedHeadcount;

        return
        [
            new() { Label = "Headcount Festangestellt", Value = fixedHeadcount.ToString("N0"), Detail = "Nenner fuer Fluktuation", Severity = "Normal" },
            new() { Label = "Austritte Total Rexx", Value = totalLeavers.ToString("N0"), Detail = "Alle Austritte in Rexx", Severity = "Normal" },
            new() { Label = "Austritte Arbeitnehmerkuendigung", Value = employeeResignationCount.ToString("N0"), Detail = "AN-/MA-Kuendigungen", Severity = "Normal" },
            new() { Label = "Austritte Fluktuationsrelevant", Value = relevantLeaverCount.ToString("N0"), Detail = "Nach HR-Definition", Severity = "Normal" },
            new() { Label = "Austritte Nicht relevant", Value = nonRelevantLeaverCount.ToString("N0"), Detail = "Ausgeschlossen oder unklar", Severity = nonRelevantLeaverCount > relevantLeaverCount ? "Warning" : "Normal" },
            new() { Label = "Fluktuation Monat %", Value = monthRate.ToString("P1"), Detail = $"{monthLeaverCount:N0} Austritte im Monat", Severity = monthRate > 0.03m ? "Warning" : "Normal" },
            new() { Label = "Avg Headcount Quartal", Value = fixedHeadcount.ToString("N0"), Detail = "Stichtagsdaten: entspricht aktuellem Headcount", Severity = "Normal" },
            new() { Label = "Austritte Quartal", Value = quarterLeaverCount.ToString("N0"), Detail = $"Quartal {currentQuarter}/{year}", Severity = "Normal" },
            new() { Label = "Fluktuation Quartal %", Value = quarterRate.ToString("P1"), Detail = "Austritte Quartal / Headcount", Severity = quarterRate > 0.08m ? "Warning" : "Normal" },
            new() { Label = "Fluktuation Hochrechnung Jahr %", Value = forecastRate.ToString("P1"), Detail = "Quartalsrate x 4", Severity = forecastRate > 0.12m ? "Warning" : "Normal" },
            new() { Label = "Avg Headcount Jahr", Value = fixedHeadcount.ToString("N0"), Detail = "Stichtagsdaten: entspricht aktuellem Headcount", Severity = "Normal" },
            new() { Label = "Austritte Jahr", Value = yearLeaverCount.ToString("N0"), Detail = $"Fluktuationsrelevant {year}", Severity = "Normal" },
            new() { Label = "Fluktuation Jahr Effektiv %", Value = yearRate.ToString("P1"), Detail = "Austritte Jahr / Headcount", Severity = yearRate > 0.12m ? "Warning" : "Normal" },
            new() { Label = "Ausschlussgrund Anzahl", Value = totalLeavers.ToString("N0"), Detail = "Basis fuer Ausschlussgrund-Tabelle", Severity = "Normal" }
        ];
    }

    private static List<HrKpiMetric> BuildAbsenceMetrics(
        IReadOnlyCollection<HrKpiEmployeeRow> employees,
        IReadOnlyCollection<HrAbsenceRow> absences)
    {
        var headcount = CountDistinctPersons(employees.Select(x => x.Personalnummer));
        var totalSick = absences.Sum(x => x.KrankheitstageGesamt);
        var shortSick = absences.Sum(x => x.KrankheitstageKurz);
        var longSick = absences.Sum(x => x.KrankheitstageLang);
        var absenceRate = headcount == 0 ? 0 : totalSick / (headcount * 21m);
        var bu = employees.Sum(x => x.BuTage);
        var nbu = employees.Sum(x => x.NbuTage);

        return
        [
            new() { Label = "Krankheitstage Gesamt", Value = totalSick.ToString("N1"), Detail = $"{absences.Count:N0} aktive Absenzenzeilen", Severity = absenceRate > 0.05m ? "Warning" : "Normal" },
            new() { Label = "Krankheit Kurz", Value = shortSick.ToString("N1"), Detail = "Rexx kurz / 8.4h", Severity = "Normal" },
            new() { Label = "Krankheit Lang", Value = longSick.ToString("N1"), Detail = "Rexx lang / 8.4h", Severity = longSick > shortSick ? "Warning" : "Normal" },
            new() { Label = "Krankenquote", Value = absenceRate.ToString("P1"), Detail = "Krankheitstage / 21 Tage / Headcount", Severity = absenceRate > 0.05m ? "Warning" : "Normal" },
            new() { Label = "BU-Tage", Value = bu.ToString("N1"), Detail = "SAP HR KPI", Severity = "Normal" },
            new() { Label = "NBU-Tage", Value = nbu.ToString("N1"), Detail = "SAP HR KPI", Severity = "Normal" },
            new() { Label = "Unfalltage Total", Value = (bu + nbu).ToString("N1"), Detail = "BU + NBU", Severity = "Normal" }
        ];
    }

    private static List<HrKpiMetric> BuildTimeVacationMetrics(IReadOnlyCollection<HrKpiEmployeeRow> employees)
    {
        var headcount = employees.Count;
        var avgBalance = headcount == 0 ? 0 : employees.Average(x => x.StundenSaldo);
        var red = employees.Count(x => x.GlzAmpel == "Rot");
        var yellow = employees.Count(x => x.GlzAmpel == "Gelb");
        var vacationEntitlement = employees.Sum(x => x.Urlaubsanspruch);
        var vacationUsed = employees.Sum(x => x.Ferientage);
        var vacationLeft = employees.Sum(x => x.UrlaubRest);
        var vacationOpen = employees.Sum(x => x.FerienAusstehend);
        var restVacationRed = employees.Count(x => x.RestferienAmpel == "Rot");

        return
        [
            new() { Label = "GLZ-Saldo Schnitt", Value = avgBalance.ToString("N1"), Detail = "Stunden pro Mitarbeiter", Severity = Math.Abs(avgBalance) > 50 ? "Warning" : "Normal" },
            new() { Label = "GLZ Gelb", Value = yellow.ToString("N0"), Detail = "51-100h absolut", Severity = yellow > 0 ? "Warning" : "Normal" },
            new() { Label = "GLZ Rot", Value = red.ToString("N0"), Detail = ">100h absolut", Severity = red > 0 ? "Warning" : "Normal" },
            new() { Label = "Ferienanspruch", Value = vacationEntitlement.ToString("N1"), Detail = "Summe Tage", Severity = "Normal" },
            new() { Label = "Ferien bezogen", Value = vacationUsed.ToString("N1"), Detail = "Anspruch - Rest - ausstehend", Severity = "Normal" },
            new() { Label = "Ferien Rest", Value = vacationLeft.ToString("N1"), Detail = "Rexx Urlaub Rest", Severity = restVacationRed > 0 ? "Warning" : "Normal" },
            new() { Label = "Ferien ausstehend", Value = vacationOpen.ToString("N1"), Detail = "Rexx ausstehend", Severity = "Normal" },
            new() { Label = "Restferien Rot", Value = restVacationRed.ToString("N0"), Detail = ">5 Tage Rest", Severity = restVacationRed > 0 ? "Warning" : "Normal" }
        ];
    }

    private static HrTurnoverVisuals BuildTurnoverVisuals(
        IReadOnlyCollection<HrKpiEmployeeRow> employees,
        IReadOnlyCollection<HrLeaverRow> leavers,
        int year)
    {
        var fixedHeadcount = CountDistinctPersons(employees
            .Where(x => string.Equals(x.Mitarbeitertyp, "Festangestellt", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Personalnummer));
        var totalLeavers = CountDistinctPersons(leavers.Select(x => x.Personalnummer));
        var employeeResignations = CountDistinctPersons(leavers.Where(x => x.IstArbeitnehmerkuendigung).Select(x => x.Personalnummer));
        var relevantLeavers = CountDistinctPersons(leavers.Where(x => x.IstFluktuationsrelevant).Select(x => x.Personalnummer));
        var notRelevant = Math.Max(0, totalLeavers - relevantLeavers);
        var ratePercent = fixedHeadcount == 0 ? 0 : relevantLeavers / (decimal)fixedHeadcount * 100m;
        var gaugeColor = ratePercent > 12m ? "#c62828" : ratePercent >= 8m ? "#f9a825" : "#2e7d32";

        var maxFunnel = Math.Max(totalLeavers, 1);
        var reasonColors = new[] { "#455a64", "#7b1fa2", "#0277bd", "#ef6c00", "#8d6e63", "#ad1457", "#558b2f" };
        var reasons = leavers
            .GroupBy(x => x.FluktuationAusschlussgrund ?? "Fluktuationsrelevant", StringComparer.OrdinalIgnoreCase)
            .Select((g, index) => new HrKpiGroupValue
            {
                Label = g.Key,
                Count = g.Count(),
                Value = g.Count(),
                Percent = totalLeavers == 0 ? 0 : g.Count() / (decimal)totalLeavers * 100m,
                Color = reasonColors[index % reasonColors.Length]
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var relevantByOrg = leavers
            .Where(x => x.IstFluktuationsrelevant)
            .GroupBy(x => BlankAsUnknown(x.Organisationseinheit), StringComparer.OrdinalIgnoreCase)
            .Select(g => new HrKpiGroupValue
            {
                Label = g.Key,
                Count = g.Count(),
                Value = g.Count(),
                Percent = relevantLeavers == 0 ? 0 : g.Count() / (decimal)relevantLeavers * 100m,
                Color = "#1565c0"
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        var monthly = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var count = leavers.Count(x =>
                    x.IstFluktuationsrelevant &&
                    x.Austrittsdatum.HasValue &&
                    x.Austrittsdatum.Value.Year == year &&
                    x.Austrittsdatum.Value.Month == month);
                return new HrKpiGroupValue
                {
                    Label = CultureInfo.GetCultureInfo("de-CH").DateTimeFormat.GetAbbreviatedMonthName(month),
                    Count = count,
                    Value = count,
                    Percent = relevantLeavers == 0 ? 0 : count / (decimal)relevantLeavers * 100m,
                    Color = "#00897b"
                };
            })
            .ToList();

        return new HrTurnoverVisuals
        {
            YearRatePercent = ratePercent,
            YearRateLabel = (ratePercent / 100m).ToString("P1"),
            GaugeColor = gaugeColor,
            GaugeRotationDegrees = Math.Clamp(ratePercent / 20m, 0m, 1m) * 180m,
            FunnelSteps =
            [
                new() { Label = "Austritte Total", Count = totalLeavers, Value = totalLeavers, Percent = 100m, Color = "#546e7a" },
                new() { Label = "Arbeitnehmerkuendigungen", Count = employeeResignations, Value = employeeResignations, Percent = employeeResignations / (decimal)maxFunnel * 100m, Color = "#1976d2" },
                new() { Label = "Fluktuationsrelevant", Count = relevantLeavers, Value = relevantLeavers, Percent = relevantLeavers / (decimal)maxFunnel * 100m, Color = "#2e7d32" },
                new() { Label = "Nicht relevant", Count = notRelevant, Value = notRelevant, Percent = notRelevant / (decimal)maxFunnel * 100m, Color = "#8d6e63" }
            ],
            ExclusionReasons = reasons,
            RelevantByOrganisation = relevantByOrg,
            MonthlyRelevantLeavers = monthly
        };
    }

    private static string? NormalizeFilter(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int ResolveAnalysisYear(HrKpiOptions options)
        => (options.ToDate ?? options.FromDate)?.Year ?? options.Year;

    private static DateTime ResolveTurnoverAnchorDate(HrKpiOptions options, int analysisYear)
    {
        if (options.ToDate.HasValue)
            return options.ToDate.Value.Date;
        if (options.FromDate.HasValue)
            return options.FromDate.Value.Date;
        return analysisYear == DateTime.Today.Year
            ? DateTime.Today
            : new DateTime(analysisYear, 12, 31);
    }

    private static bool MatchesLeaverDateFilter(HrLeaverRow row, HrKpiOptions options)
    {
        var hasRange = options.FromDate.HasValue || options.ToDate.HasValue;
        if (hasRange)
        {
            if (!row.Austrittsdatum.HasValue)
                return false;
            return (!options.FromDate.HasValue || row.Austrittsdatum.Value.Date >= options.FromDate.Value) &&
                   (!options.ToDate.HasValue || row.Austrittsdatum.Value.Date <= options.ToDate.Value);
        }

        return row.Austrittsjahr.HasValue && row.Austrittsjahr.Value == options.Year;
    }

    private static int CountDistinctPersons(IEnumerable<int?> personalNumbers)
        => personalNumbers
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .Count();

    private static decimal ResolveFte(decimal? employmentPercent, string workingTimeModel, decimal? averageHoursPerDay)
    {
        if (employmentPercent.HasValue && employmentPercent.Value > 0)
            return employmentPercent.Value / 100m;

        if (averageHoursPerDay.HasValue && averageHoursPerDay.Value > 0)
            return Math.Clamp(averageHoursPerDay.Value / 8.4m, 0.1m, 1.2m);

        if (string.Equals(workingTimeModel, "Vollzeit", StringComparison.OrdinalIgnoreCase))
            return 1m;

        if (string.Equals(workingTimeModel, "Teilzeit", StringComparison.OrdinalIgnoreCase))
            return 0.5m;

        return 0m;
    }

    private static bool MatchesFilter(string value, string? filter)
        => string.IsNullOrWhiteSpace(filter) || string.Equals(value?.Trim(), filter, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesEmployeeSearch(HrKpiEmployeeRow row, string? search)
        => MatchesTextSearch(search, row.NameVoll, row.Personalnummer?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, row.Organisationseinheit, row.KostenstelleText, row.Stelle);

    private static bool MatchesTextSearch(string? search, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;
        return values.Any(value => value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesFluctuationFilter(HrLeaverRow row, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter) || string.Equals(filter, "Alle", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(filter, "Fluktuationsrelevant", StringComparison.OrdinalIgnoreCase))
            return row.IstFluktuationsrelevant;
        if (string.Equals(filter, "Arbeitnehmerkuendigung", StringComparison.OrdinalIgnoreCase))
            return row.IstArbeitnehmerkuendigung;
        if (string.Equals(filter, "Ausgeschlossen", StringComparison.OrdinalIgnoreCase))
            return !row.IstFluktuationsrelevant;
        return true;
    }

    private static string BlankAsUnknown(string value)
        => string.IsNullOrWhiteSpace(value) ? "Unbekannt" : value;

    private static string BuildPersonalKey(int? personalnummer)
        => personalnummer?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string NormalizeKey(string value)
        => value.Trim().ToUpperInvariant();

    private static int? ParseCostCenter(string value)
    {
        var raw = value.Split('/')[0].Trim();
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static (string Nachname, string Vorname) SplitName(string value)
    {
        var parts = value.Split(',', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : (value.Trim(), string.Empty);
    }

    private static int? YearsSince(DateTime? date)
    {
        if (!date.HasValue)
            return null;
        var today = DateTime.Today;
        var years = today.Year - date.Value.Year;
        if (date.Value.Date > today.AddYears(-years))
            years--;
        return years;
    }

    private static string BuildAgeGroup(int? age)
    {
        if (!age.HasValue) return "Unbekannt";
        if (age.Value < 30) return "< 30";
        if (age.Value < 40) return "30-39";
        if (age.Value < 50) return "40-49";
        if (age.Value < 60) return "50-59";
        return "60+";
    }

    private static string MapGender(int? value)
        => value switch
        {
            1 => "Maennlich",
            2 => "Weiblich",
            _ => "Unbekannt"
        };

    private static decimal ParseTimeBalance(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;
        var trimmed = value.Trim();
        var negative = trimmed.StartsWith("-", StringComparison.Ordinal);
        trimmed = trimmed.TrimStart('-');
        var parts = trimmed.Split(':');
        if (parts.Length == 0)
            return 0;
        var hours = ParseDecimal(parts[0]);
        var minutes = parts.Length > 1 ? ParseDecimal(parts[1]) : 0;
        var result = hours + minutes / 60m;
        return negative ? -result : result;
    }

    private static string BuildTrafficLight(decimal balance)
    {
        var absolute = Math.Abs(balance);
        if (absolute <= 50) return "Gruen";
        return absolute <= 100 ? "Gelb" : "Rot";
    }

    private static string BuildEmployeeType(string position)
    {
        var lower = NormalizeReason(position);
        if (lower.Contains("praktik", StringComparison.OrdinalIgnoreCase)) return "Praktikant";
        if (lower.Contains("werkstudent", StringComparison.OrdinalIgnoreCase)) return "Werkstudent";
        if (lower.Contains("aushilfe", StringComparison.OrdinalIgnoreCase)) return "Aushilfe";
        if (lower.Contains("lehrling", StringComparison.OrdinalIgnoreCase)) return "Lehrling";
        return "Festangestellt";
    }

    private static string NormalizeReason(string value)
    {
        var normalized = RemoveDiacritics(value).Trim().ToLowerInvariant();
        return normalized
            .Replace("Ã¤", "ae", StringComparison.OrdinalIgnoreCase)
            .Replace("Ã¶", "oe", StringComparison.OrdinalIgnoreCase)
            .Replace("Ã¼", "ue", StringComparison.OrdinalIgnoreCase)
            .Replace("ÃŸ", "ss", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildExclusionReason(string employeeType, string reason, bool isEmployeeResignation)
    {
        if (!string.Equals(employeeType, "Festangestellt", StringComparison.OrdinalIgnoreCase)) return employeeType;
        if (string.IsNullOrWhiteSpace(reason)) return "Austrittsart leer/unklar";
        if (reason.Contains("befrist", StringComparison.OrdinalIgnoreCase)) return "Befristeter Vertrag";
        if (reason.Contains("pension", StringComparison.OrdinalIgnoreCase) || reason.Contains("rente", StringComparison.OrdinalIgnoreCase)) return "Pensionierung";
        if (reason.Contains("trafag", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("arbeitgeber", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("ag-kuendigung", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("ag kuendigung", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("kuendigung ag", StringComparison.OrdinalIgnoreCase)) return "Kuendigung durch Trafag";
        return isEmployeeResignation ? "Ausgeschlossen" : "Keine Arbeitnehmerkuendigung";
    }

    private static string ReadString(IXLRow row, IReadOnlyDictionary<string, int> headers, params string[] aliases)
    {
        var index = FindHeader(headers, aliases);
        return index.HasValue ? row.Cell(index.Value).GetFormattedString().Trim() : string.Empty;
    }

    private static int? ReadInt(IXLRow row, IReadOnlyDictionary<string, int> headers, params string[] aliases)
    {
        var value = ReadString(row, headers, aliases);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        var decimalValue = ParseDecimalNullable(value);
        return decimalValue.HasValue ? (int)Math.Truncate(decimalValue.Value) : null;
    }

    private static decimal ReadDecimal(IXLRow row, IReadOnlyDictionary<string, int> headers, params string[] aliases)
        => ReadDecimalNullable(row, headers, aliases) ?? 0;

    private static decimal? ReadDecimalNullable(IXLRow row, IReadOnlyDictionary<string, int> headers, params string[] aliases)
    {
        var index = FindHeader(headers, aliases);
        if (!index.HasValue)
            return null;
        var cell = row.Cell(index.Value);
        if (cell.TryGetValue<decimal>(out var decimalValue))
            return decimalValue;
        if (cell.TryGetValue<double>(out var doubleValue))
            return (decimal)doubleValue;
        return ParseDecimalNullable(cell.GetFormattedString());
    }

    private static DateTime? ReadDate(IXLRow row, IReadOnlyDictionary<string, int> headers, params string[] aliases)
    {
        var index = FindHeader(headers, aliases);
        if (!index.HasValue)
            return null;
        var cell = row.Cell(index.Value);
        if (cell.TryGetValue<DateTime>(out var dateValue))
            return dateValue.Date;
        if (cell.TryGetValue<double>(out var serialValue))
            return DateTime.FromOADate(serialValue).Date;
        var value = cell.GetFormattedString().Trim();
        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("de-CH"), DateTimeStyles.None, out var parsed) ||
            DateTime.TryParse(value, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out parsed) ||
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            return parsed.Date;
        if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out serialValue))
            return DateTime.FromOADate(serialValue).Date;
        return null;
    }

    private static int? FindHeader(IReadOnlyDictionary<string, int> headers, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (headers.TryGetValue(NormalizeHeader(alias), out var index))
                return index;
        }

        return null;
    }

    private static decimal ParseDecimal(string value)
        => ParseDecimalNullable(value) ?? 0;

    private static decimal? ParseDecimalNullable(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim()
            .Replace("'", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal);
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("de-CH"), out var result) ||
            decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("de-DE"), out result) ||
            decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            return result;
        return null;
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = RemoveDiacritics(value)
            .Replace("ÃƒÂ¼", "u", StringComparison.OrdinalIgnoreCase)
            .Replace("ÃƒÂ¤", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("ÃƒÂ¶", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ÃƒËœ", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("Ã¸", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("Ã˜", "o", StringComparison.OrdinalIgnoreCase);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                builder.Append(ch);
        }
        return builder.ToString();
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record TimeRow(string NameKey, DateTime? Geburtsdatum, string Arbeitszeitmodell, decimal AvgSollzeitTag);

    private sealed record SapRow(
        string PersonalKey,
        string Buchungskreis,
        string Personalbereich,
        string Personalteilbereich,
        string Mitarbeitergruppe,
        string Mitarbeiterkreis,
        string Teilzeitkennzeichen,
        decimal? BeschaeftigungsgradProzent,
        int? Geschlecht,
        string Planstelle,
        string SollStelle,
        decimal NbuTage,
        decimal BuTage,
        string Abrechnungskreis);

    private sealed class ImportContext
    {
        private readonly HrKpiResult _result;
        private readonly string _folder;

        public ImportContext(HrKpiResult result, string folder)
        {
            _result = result;
            _folder = folder;
        }

        public bool HasFile(string fileName)
            => File.Exists(BuildPath(fileName));

        public List<T> ReadRows<T>(string fileName, string label, Func<IXLRow, IReadOnlyDictionary<string, int>, T> map)
        {
            var path = BuildPath(fileName);
            var status = new HrKpiFileStatus
            {
                Label = label,
                Path = path,
                Exists = File.Exists(path)
            };
            _result.FileStatuses.Add(status);

            if (!status.Exists)
            {
                status.Message = "Datei nicht gefunden";
                return [];
            }

            try
            {
                using var workbook = new XLWorkbook(path);
                var worksheet = workbook.Worksheets.First();
                var headerRow = worksheet.FirstRowUsed();
                if (headerRow is null)
                {
                    status.Message = "Leeres Arbeitsblatt";
                    return [];
                }

                var headers = headerRow.CellsUsed()
                    .GroupBy(c => NormalizeHeader(c.GetString()))
                    .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                    .ToDictionary(g => g.Key, g => g.First().Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

                var rows = worksheet.RowsUsed()
                    .Where(r => r.RowNumber() > headerRow.RowNumber())
                    .Where(r => !r.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetFormattedString())))
                    .Select(r => map(r, headers))
                    .ToList();

                status.RowCount = rows.Count;
                status.Message = "OK";
                return rows;
            }
            catch (Exception ex)
            {
                status.Message = ex.Message;
                _result.Notices.Add($"{label}: {ex.Message}");
                return [];
            }
        }

        private string BuildPath(string fileName)
            => Path.Combine(_folder, fileName);
    }
}
