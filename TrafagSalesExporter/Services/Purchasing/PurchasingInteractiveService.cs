using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Einkauf Interaktiv (Wunsch Ingo 2026-10-05, „analog Verkaufs-Interaktivtools“): Bestellpositionen aus dem Einkaufscache
/// in dieselbe Zeilenform wie der Verkauf (<see cref="SalesFact"/>), damit die neun Ansichten aus
/// <c>Components/Sales/Interactive</c> unverändert rechnen. Zuordnung: Buchungskreis → Gesellschaft, Lieferant → Kunde,
/// Lieferland → Kundenland, Hauptwarengruppe → Sparte, Warengruppe → Artikel, Bestellung → Rechnung, Bestelldatum → Datum,
/// Netto-Bestellwert in CHF wie im Einkaufsdashboard (EKKO.Wkurs). Ohne gelöschte Positionen. Nur lesend, 30 Minuten gemerkt.
/// Doku docs/VERKAUF_2026-10-02.md, Abschnitt „Interaktiv“, und docs/router/einkauf.md.
/// </summary>
public sealed class PurchasingInteractiveService
{
    private const string ChfValue = @"(CASE
        WHEN COALESCE(k.Waers, '') IN ('', 'CHF') THEN CAST(p.Netwr AS REAL)
        WHEN CAST(k.Wkurs AS REAL) > 0 THEN CAST(p.Netwr AS REAL) * CAST(k.Wkurs AS REAL)
        WHEN CAST(k.Wkurs AS REAL) < 0 THEN CAST(p.Netwr AS REAL) / (-CAST(k.Wkurs AS REAL))
        ELSE CAST(p.Netwr AS REAL) END)";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SalesDataset? _cache;

    public PurchasingInteractiveService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<SalesDataset> GetAsync()
    {
        await _gate.WaitAsync();
        try
        {
            // Fehlerergebnisse nur 1 Minute merken, damit ein voruebergehend nicht lesbarer Cache nicht eine halbe Stunde haengt.
            if (_cache is { } c && DateTime.Now - c.LoadedAt < (c.Error is null ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(1)))
                return c;
            _cache = await LoadAsync();
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Hauptwarengruppe „10.00.00“ aus „10.04.00“.</summary>
    public static string MainGroup(string matkl)
    {
        var m = (matkl ?? "").Trim();
        if (m.Length == 0) return "ohne Warengruppe";
        var parts = m.Split('.');
        var main = parts.Length >= 3 ? $"{parts[0]}.00.00" : m;
        return PurchasingMaterialGroupTextCatalog.Resolve(main);
    }

    private async Task<SalesDataset> LoadAsync()
    {
        var facts = new List<SalesFact>();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
SELECT k.Bedat, COALESCE(k.Bukrs, ''), COALESCE(k.Lifnr, ''), COALESCE(k.SupplierName, ''), COALESCE(k.SupplierCountry, ''),
       COALESCE(p.Matnr, ''), COALESCE(NULLIF(p.MaraMatkl, ''), p.Matkl, ''), CAST(p.Menge AS REAL), k.Ebeln, CAST(p.Netwr AS REAL),
       COALESCE(k.Waers, ''), {ChfValue}
FROM PurchasingEkpoCache p JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE k.Bedat >= $from AND COALESCE(p.Loekz, '') = ''
  AND (COALESCE(k.Bstyp, '') = '' OR (k.Bstyp = 'F' AND COALESCE(k.Bsart, '') <> 'UB'))";
            cmd.Parameters.AddWithValue("$from", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            await using var r = await cmd.ExecuteReaderAsync();
            var groupText = new Dictionary<string, (string Main, string Group)>(StringComparer.OrdinalIgnoreCase);
            while (await r.ReadAsync())
            {
                if (r.IsDBNull(0) || !DateOnly.TryParse(r.GetString(0)[..Math.Min(10, r.GetString(0).Length)], CultureInfo.InvariantCulture, out var date))
                    continue;
                var chf = r.IsDBNull(11) ? 0 : Convert.ToDecimal(r.GetDouble(11));
                if (chf == 0) continue;
                var matkl = r.GetString(6);
                if (!groupText.TryGetValue(matkl, out var g))
                    groupText[matkl] = g = (MainGroup(matkl), matkl.Length == 0 ? "ohne Warengruppe" : PurchasingMaterialGroupTextCatalog.Resolve(matkl));
                var lifnr = r.GetString(2).TrimStart('0');
                facts.Add(new SalesFact(date, r.GetString(1), r.GetString(1), lifnr.Length == 0 ? "ohne Lieferant" : lifnr, r.GetString(3),
                    SalesAnalytics.NormalizeCountry(r.GetString(4)), r.GetString(5).TrimStart('0'), g.Group, g.Main, chf,
                    r.IsDBNull(7) ? 0 : Convert.ToDecimal(r.GetDouble(7)), r.GetString(8),
                    r.IsDBNull(9) ? 0 : Convert.ToDecimal(r.GetDouble(9)), r.GetString(10).Trim().ToUpperInvariant()));
            }
        }
        catch (Exception ex)
        {
            return new SalesDataset { LoadedAt = DateTime.Now, Error = "Einkaufscache nicht lesbar: " + ex.GetBaseException().Message };
        }

        var refEnd = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var dataStart = facts.Count == 0 ? refEnd.AddMonths(-12) : new DateOnly(facts.Min(f => f.Date).Year, facts.Min(f => f.Date).Month, 1);
        return new SalesDataset
        {
            LoadedAt = DateTime.Now,
            Facts = facts,
            ReferenceEnd = refEnd,
            DataStart = dataStart,
            CompareMonths = SalesAnalytics.CompareMonths(dataStart, refEnd)
        };
    }
}
