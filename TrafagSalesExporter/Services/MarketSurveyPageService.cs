using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>Kennzahlen einer Umfrage fuer die Kopfzeile.</summary>
public sealed record MarketSurveySummary(
    string SurveyName,
    int Entries,
    int Customers,
    int Countries,
    int LinkedEntries,
    int WithoutSales);

public interface IMarketSurveyPageService
{
    /// <summary>Umfragezeilen, optional gefiltert. Sortiert nach Land und Kunde.</summary>
    Task<List<MarketSurveyEntry>> SearchAsync(
        string? surveyName, string? textFilter, string? countryFilter, string? statusFilter, int take = 500);

    Task<MarketSurveyEntry?> GetAsync(int id);

    /// <summary>Legt eine Zeile an oder aktualisiert sie. Rueckgabe ist die gespeicherte Zeile.</summary>
    Task<MarketSurveyEntry> SaveAsync(MarketSurveyEntry entry);

    Task DeleteAsync(int id);

    Task<List<MarketSurveySummary>> GetSummariesAsync();

    /// <summary>Vorhandene Werte einer Spalte als Auswahlvorschlaege, plus die Standardliste.</summary>
    Task<List<string>> GetDistinctValuesAsync(string field);
}

/// <summary>
/// Pflege der Marktumfragen in der Anwendung. Ziel ist die Abloesung der Excel-Datei:
/// solange die Umfrage nur als Datei existiert, kursieren mehrere Fassungen und niemand
/// weiss, welche gilt.
///
/// Die Umfrage bleibt bewusst von den Verkaufsdaten getrennt. Sie beschreibt den MARKT,
/// einschliesslich Interessenten ohne jeden Umsatz, und ist damit keine Umsatzquelle. Die
/// Verknuepfung zu einem Verkaufskunden ist optional; der Ist-Umsatz kommt weiterhin
/// ausschliesslich aus <see cref="CustomerMarketSegment"/> und den ERP-Zeilen.
/// </summary>
public sealed class MarketSurveyPageService : IMarketSurveyPageService
{
    /// <summary>Standardwerte fuer die Statusauswahl, aus der Legende der Railway-Umfrage.</summary>
    public static readonly string[] DefaultStatuses =
        ["New", "Opportunity", "Existing Customer", "No Potential"];

    /// <summary>Standardwerte fuer Business Type, aus derselben Legende.</summary>
    public static readonly string[] DefaultBusinessTypes = ["Project", "Serial"];

    /// <summary>
    /// Rollen aus der Legende der Railway-Umfrage. Frei ergaenzbar, keine harte Schranke.
    /// </summary>
    public static readonly string[] DefaultCustomerTypes =
    [
        "OEM", "Tier 1", "Tier 2", "Operator / Maintenance", "Maintenance/retrofit",
        "Test & Measurement", "Infrastructure Integrator"
    ];

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppEventLogService _log;

    public MarketSurveyPageService(IDbContextFactory<AppDbContext> dbFactory, IAppEventLogService log)
    {
        _dbFactory = dbFactory;
        _log = log;
    }

    public async Task<List<MarketSurveyEntry>> SearchAsync(
        string? surveyName, string? textFilter, string? countryFilter, string? statusFilter, int take = 500)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.MarketSurveyEntries.AsNoTracking();

        var survey = surveyName?.Trim();
        if (!string.IsNullOrEmpty(survey))
            query = query.Where(x => x.SurveyName == survey);

        var country = countryFilter?.Trim();
        if (!string.IsNullOrEmpty(country))
            query = query.Where(x => x.Country == country);

        var status = statusFilter?.Trim();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        // Freitextsuche ueber die Felder, in denen ein Anwender tatsaechlich sucht.
        var text = textFilter?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            var pattern = $"%{text}%";
            query = query.Where(x =>
                EF.Functions.Like(x.CustomerName, pattern) ||
                EF.Functions.Like(x.CustomerShort, pattern) ||
                EF.Functions.Like(x.Application, pattern) ||
                EF.Functions.Like(x.Product, pattern) ||
                EF.Functions.Like(x.Competitor, pattern) ||
                EF.Functions.Like(x.Comments, pattern));
        }

        return await query
            .OrderBy(x => x.Country)
            .ThenBy(x => x.CustomerName)
            .ThenBy(x => x.Application)
            .Take(take)
            .ToListAsync();
    }

    public async Task<MarketSurveyEntry?> GetAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.MarketSurveyEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<MarketSurveyEntry> SaveAsync(MarketSurveyEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.CustomerName))
            throw new InvalidOperationException("Ohne Kundennamen laesst sich die Zeile nicht zuordnen.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        MarketSurveyEntry target;
        var isNew = entry.Id == 0;
        if (isNew)
        {
            target = new MarketSurveyEntry();
            db.MarketSurveyEntries.Add(target);
        }
        else
        {
            target = await db.MarketSurveyEntries.FirstOrDefaultAsync(x => x.Id == entry.Id)
                     ?? throw new InvalidOperationException("Diese Umfragezeile existiert nicht mehr.");
        }

        target.SurveyName = Trim(entry.SurveyName);
        target.Country = Trim(entry.Country).ToUpperInvariant();
        target.CustomerName = Trim(entry.CustomerName);
        target.CustomerShort = Trim(entry.CustomerShort);
        target.CustomerType = Trim(entry.CustomerType);
        target.BusinessType = Trim(entry.BusinessType);
        target.Application = Trim(entry.Application);
        target.ApplicationDescription = Trim(entry.ApplicationDescription);
        target.Status = Trim(entry.Status);
        target.TrafagUsp = Trim(entry.TrafagUsp);
        target.Competitor = Trim(entry.Competitor);
        target.Product = Trim(entry.Product);
        target.MaterialNumber = Trim(entry.MaterialNumber);
        target.EstimatedQuantity = Trim(entry.EstimatedQuantity);
        target.EstimatedPrice = Trim(entry.EstimatedPrice);
        target.Comments = Trim(entry.Comments);
        target.LinkedTsc = Trim(entry.LinkedTsc).ToUpperInvariant();
        target.LinkedCustomerNumber = Trim(entry.LinkedCustomerNumber).ToUpperInvariant();
        target.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await _log.WriteAsync(
            "Marktumfrage",
            isNew ? "Umfragezeile angelegt" : "Umfragezeile geaendert",
            details: $"{target.SurveyName} | {target.Country} | {target.CustomerName} | {target.Application}");

        return target;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.MarketSurveyEntries.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null) return;

        db.MarketSurveyEntries.Remove(existing);
        await db.SaveChangesAsync();

        await _log.WriteAsync(
            "Marktumfrage",
            "Umfragezeile geloescht",
            details: $"{existing.SurveyName} | {existing.Country} | {existing.CustomerName} | {existing.Application}");
    }

    public async Task<List<MarketSurveySummary>> GetSummariesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var all = await db.MarketSurveyEntries.AsNoTracking().ToListAsync();

        return all
            .GroupBy(x => x.SurveyName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new MarketSurveySummary(
                g.Key,
                g.Count(),
                g.Select(x => x.CustomerName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                g.Select(x => x.Country).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                g.Count(x => x.LinkedCustomerNumber.Length > 0),
                g.Count(x => x.LinkedCustomerNumber.Length == 0)))
            .OrderBy(x => x.SurveyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<List<string>> GetDistinctValuesAsync(string field)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.MarketSurveyEntries.AsNoTracking();

        var used = field.ToLowerInvariant() switch
        {
            "status" => await query.Select(x => x.Status).Distinct().ToListAsync(),
            "businesstype" => await query.Select(x => x.BusinessType).Distinct().ToListAsync(),
            "customertype" => await query.Select(x => x.CustomerType).Distinct().ToListAsync(),
            "application" => await query.Select(x => x.Application).Distinct().ToListAsync(),
            "country" => await query.Select(x => x.Country).Distinct().ToListAsync(),
            "surveyname" => await query.Select(x => x.SurveyName).Distinct().ToListAsync(),
            _ => []
        };

        var defaults = field.ToLowerInvariant() switch
        {
            "status" => DefaultStatuses,
            "businesstype" => DefaultBusinessTypes,
            "customertype" => DefaultCustomerTypes,
            _ => []
        };

        return used
            .Concat(defaults)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Trim(string? value) => (value ?? string.Empty).Trim();
}
