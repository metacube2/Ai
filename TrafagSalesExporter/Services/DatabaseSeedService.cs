using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class DatabaseSeedService : IDatabaseSeedService
{
    public void SeedDefaults(AppDbContext db)
    {
        SeedIfEmpty(db);
        EnsureRecommendedTransformationRules(db);
        EnsureSourceSystemDefinitions(db);
        EnsureCentralHanaServerRecords(db);
    }

    private static void SeedIfEmpty(AppDbContext db)
    {
        if (db.Sites.Any() || db.HanaServers.Any() || db.SharePointConfigs.Any() || db.ExportSettings.Any())
            return;

        var serverBi1 = new HanaServer { SourceSystem = "BI1", Name = "BI1", Host = "travtrp0", Port = 30015, Username = "", Password = "" };
        var serverSage = new HanaServer { SourceSystem = "SAGE", Name = "SAGE", Host = "20.197.20.60", Port = 30015, Username = "", Password = "" };
        db.HanaServers.AddRange(serverBi1, serverSage);
        db.SaveChanges();

        db.Sites.AddRange(
            new Site { HanaServerId = serverBi1.Id, Schema = "fr01_p", TSC = "TRFR", Land = "Frankreich", SourceSystem = "BI1", IsActive = true },
            new Site { HanaServerId = serverBi1.Id, Schema = "it01_p", TSC = "TRIT", Land = "Italien", SourceSystem = "BI1", IsActive = true },
            new Site { HanaServerId = serverBi1.Id, Schema = "us01_p", TSC = "TRUS", Land = "USA", SourceSystem = "BI1", IsActive = true },
            new Site { HanaServerId = serverSage.Id, Schema = "TRAFAG_LIVE", TSC = "TRIN", Land = "Indien", SourceSystem = "SAGE", IsActive = true }
        );

        db.SharePointConfigs.Add(new SharePointConfig
        {
            SiteUrl = "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform",
            ExportFolder = "/Shared Documents/Exports/",
            CentralExportFolder = "",
            TenantId = "",
            ClientId = "",
            ClientSecret = ""
        });

        db.ExportSettings.Add(new ExportSettings
        {
            DateFilter = "2025-01-01",
            TimerHour = 3,
            TimerMinute = 0,
            TimerEnabled = true,
            DebugLoggingEnabled = false,
            LocalSiteExportFolder = "",
            LocalConsolidatedExportFolder = ""
        });

        db.SaveChanges();
    }

    private static void EnsureRecommendedTransformationRules(AppDbContext db)
    {
        var recommendedRules = new[]
        {
            new FieldTransformationRule
            {
                SourceSystem = "MANUAL_EXCEL",
                SourceField = nameof(SalesRecord.SalesCurrency),
                TargetField = nameof(SalesRecord.SalesCurrency),
                TransformationType = "Replace",
                RuleScope = "Value",
                Argument = "$=>USD",
                SortOrder = 100,
                IsActive = true
            },
            new FieldTransformationRule
            {
                SourceSystem = "MANUAL_EXCEL",
                SourceField = nameof(SalesRecord.StandardCostCurrency),
                TargetField = nameof(SalesRecord.StandardCostCurrency),
                TransformationType = "Replace",
                RuleScope = "Value",
                Argument = "$=>USD",
                SortOrder = 110,
                IsActive = true
            }
        };

        var hasChanges = false;

        foreach (var rule in recommendedRules)
        {
            var exists = db.FieldTransformationRules.Any(existing =>
                existing.SourceSystem == rule.SourceSystem &&
                existing.RuleScope == rule.RuleScope &&
                existing.SourceField == rule.SourceField &&
                existing.TargetField == rule.TargetField &&
                existing.TransformationType == rule.TransformationType &&
                existing.Argument == rule.Argument);

            if (exists)
                continue;

            db.FieldTransformationRules.Add(rule);
            hasChanges = true;
        }

        if (hasChanges)
            db.SaveChanges();
    }

    private static void EnsureCentralHanaServerRecords(AppDbContext db)
    {
        var centralSystems = db.SourceSystemDefinitions
            .AsNoTracking()
            .Where(x => x.ConnectionKind == SourceSystemConnectionKinds.Hana)
            .OrderBy(x => x.Code)
            .Select(x => x.Code)
            .ToList();
        var changed = false;

        foreach (var sourceSystem in centralSystems)
        {
            var existingCentral = db.HanaServers
                .OrderBy(x => x.Id)
                .FirstOrDefault(x => x.SourceSystem == sourceSystem);

            if (existingCentral is not null)
            {
                if (string.IsNullOrWhiteSpace(existingCentral.Name))
                {
                    existingCentral.Name = sourceSystem;
                    changed = true;
                }

                continue;
            }

            var linkedServer = db.Sites
                .Include(x => x.HanaServer)
                .Where(x => x.SourceSystem == sourceSystem && x.HanaServerId != null && x.HanaServer != null)
                .Select(x => x.HanaServer!)
                .OrderBy(x => x.Id)
                .FirstOrDefault();

            if (linkedServer is not null)
            {
                linkedServer.SourceSystem = sourceSystem;
                if (string.IsNullOrWhiteSpace(linkedServer.Name))
                    linkedServer.Name = sourceSystem;
                changed = true;
                continue;
            }

            db.HanaServers.Add(new HanaServer
            {
                SourceSystem = sourceSystem,
                Name = sourceSystem,
                Host = string.Empty,
                Port = 30015,
                Username = string.Empty,
                Password = string.Empty,
                DatabaseName = string.Empty,
                AdditionalParams = string.Empty
            });
            changed = true;
        }

        if (changed)
            db.SaveChanges();
    }

    private static void EnsureSourceSystemDefinitions(AppDbContext db)
    {
        var defaults = new[]
        {
            new SourceSystemDefinition { Code = "SAP", DisplayName = "SAP", ConnectionKind = SourceSystemConnectionKinds.SapGateway, IsActive = true },
            new SourceSystemDefinition { Code = "BI1", DisplayName = "BI1", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
            new SourceSystemDefinition { Code = "SAGE", DisplayName = "SAGE", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
            new SourceSystemDefinition { Code = "MANUAL_EXCEL", DisplayName = "Manual Excel", ConnectionKind = SourceSystemConnectionKinds.ManualExcel, IsActive = true }
        };

        var existing = db.SourceSystemDefinitions.ToList();
        var changed = false;

        foreach (var item in defaults)
        {
            var current = existing.FirstOrDefault(x => x.Code == item.Code);
            if (current is null)
            {
                db.SourceSystemDefinitions.Add(item);
                existing.Add(item);
                changed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(current.DisplayName))
            {
                current.DisplayName = item.DisplayName;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(current.ConnectionKind))
            {
                current.ConnectionKind = item.ConnectionKind;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(current.CentralServiceUrl) &&
                string.Equals(current.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase))
            {
                var sapSite = db.Sites
                    .Where(x => x.SourceSystem == current.Code && !string.IsNullOrWhiteSpace(x.SapServiceUrl))
                    .OrderBy(x => x.Id)
                    .FirstOrDefault();

                if (sapSite is not null)
                {
                    current.CentralServiceUrl = sapSite.SapServiceUrl;
                    changed = true;
                }
            }
        }

        if (changed)
            db.SaveChanges();
    }
}
