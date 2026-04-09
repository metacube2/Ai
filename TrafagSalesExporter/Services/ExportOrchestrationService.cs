using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExportOrchestrationService(
    IDbContextFactory<AppDbContext> dbFactory,
    CryptoService cryptoService,
    HanaQueryService hanaQueryService,
    ExcelExportService excelExportService,
    SharePointUploadService sharePointUploadService)
{
    public async Task ExportAllActiveSitesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var siteIds = await db.Sites.Where(x => x.IsActive).Select(x => x.Id).ToListAsync(ct);

        foreach (var siteId in siteIds)
        {
            await ExportSiteAsync(siteId, ct);
        }
    }

    public async Task ExportSiteAsync(int siteId, CancellationToken ct = default)
    {
        var started = DateTime.UtcNow;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var site = await db.Sites.Include(x => x.HanaServer).SingleAsync(x => x.Id == siteId, ct);
        var settings = await db.ExportSettings.OrderBy(x => x.Id).FirstAsync(ct);
        var sp = await db.SharePointConfigs.OrderBy(x => x.Id).FirstAsync(ct);

        var log = new ExportLog
        {
            Timestamp = DateTime.UtcNow,
            SiteId = site.Id,
            Land = site.Land,
            TSC = site.TSC,
            Status = "Error",
            RowCount = 0,
            FileName = string.Empty,
            DurationSeconds = 0
        };

        try
        {
            var hanaServer = site.HanaServer ?? throw new InvalidOperationException("HANA Server fehlt.");
            var hanaPassword = cryptoService.Decrypt(hanaServer.EncryptedPassword);
            var clientSecret = cryptoService.Decrypt(sp.EncryptedClientSecret);

            var records = hanaQueryService.QuerySales(
                hanaServer.Host,
                hanaServer.Port,
                hanaServer.Username,
                hanaPassword,
                site.Schema,
                site.TSC,
                site.Land,
                settings.DateFilter);

            var filePath = excelExportService.CreateFile(AppContext.BaseDirectory, site.Land, site.TSC, records);

            await sharePointUploadService.UploadAsync(
                sp.SiteUrl,
                sp.ExportFolder,
                sp.TenantId,
                sp.ClientId,
                clientSecret,
                site.Land,
                filePath);

            log.Status = "OK";
            log.RowCount = records.Count;
            log.FileName = Path.GetFileName(filePath);
            log.ErrorMessage = null;
        }
        catch (Exception ex)
        {
            log.ErrorMessage = ex.Message;
        }
        finally
        {
            log.DurationSeconds = (DateTime.UtcNow - started).TotalSeconds;
            db.ExportLogs.Add(log);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<DateTime?> GetNextRunAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var settings = await db.ExportSettings.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (settings is null || !settings.TimerEnabled)
        {
            return null;
        }

        var now = DateTime.Now;
        var next = new DateTime(now.Year, now.Month, now.Day, settings.TimerHour, settings.TimerMinute, 0);
        if (next <= now)
        {
            next = next.AddDays(1);
        }

        return next;
    }

    public async Task<Dictionary<int, ExportLog?>> GetLatestLogsPerSiteAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var grouped = await db.ExportLogs
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(ct);

        return grouped
            .GroupBy(x => x.SiteId ?? 0)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());
    }
}
