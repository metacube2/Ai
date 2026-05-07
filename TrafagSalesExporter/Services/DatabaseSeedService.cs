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
        EnsureSpainManualExcelSite(db);
        EnsureSapODataDachSite(db);
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
            new SourceSystemDefinition { Code = "SAP", DisplayName = "SAP OData", ConnectionKind = SourceSystemConnectionKinds.SapGateway, IsActive = true },
            new SourceSystemDefinition { Code = "SAP_HANA", DisplayName = "SAP HANA Tables/Views", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
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
            else if ((current.Code == "SAP" && current.DisplayName == "SAP") ||
                     (current.Code == "SAP_HANA" && current.DisplayName == "SAP HANA"))
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

    private static void EnsureSpainManualExcelSite(AppDbContext db)
    {
        if (db.Sites.Count() <= 1)
            return;

        var existing = db.Sites
            .OrderBy(x => x.Id)
            .FirstOrDefault(x =>
                x.TSC == "TRSE" ||
                x.TSC == "TRES" ||
                x.Land == "Spanien" ||
                x.Land == "Spain");

        if (existing is not null)
        {
            var changed = false;

            if (string.IsNullOrWhiteSpace(existing.TSC))
            {
                existing.TSC = "TRES";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Land))
            {
                existing.Land = "Spanien";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.SourceSystem))
            {
                existing.SourceSystem = "MANUAL_EXCEL";
                changed = true;
            }

            if (changed)
                db.SaveChanges();

            return;
        }

        db.Sites.Add(new Site
        {
            Schema = string.Empty,
            TSC = "TRES",
            Land = "Spanien",
            SourceSystem = "MANUAL_EXCEL",
            IsActive = false
        });
        db.SaveChanges();
    }

    private static void EnsureSapODataDachSite(AppDbContext db)
    {
        if (db.Sites.Count() <= 1)
            return;

        var existing = db.Sites
            .OrderBy(x => x.Id)
            .FirstOrDefault(x =>
                x.TSC == "ZSCHWEIZ" ||
                x.Land == "Schweiz/Oesterreich" ||
                x.Land == "DACH");

        if (existing is not null)
        {
            var changed = false;

            if (string.IsNullOrWhiteSpace(existing.TSC))
            {
                existing.TSC = "ZSCHWEIZ";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Land))
            {
                existing.Land = "Schweiz/Oesterreich";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.SourceSystem) ||
                string.Equals(existing.SourceSystem, "SAP_HANA", StringComparison.OrdinalIgnoreCase))
            {
                existing.SourceSystem = "SAP";
                changed = true;
            }

            if (changed)
                db.SaveChanges();

            EnsureSapODataDachMapping(db, existing.Id);
            return;
        }

        var site = new Site
        {
            Schema = string.Empty,
            TSC = "ZSCHWEIZ",
            Land = "Schweiz/Oesterreich",
            SourceSystem = "SAP",
            IsActive = false
        };
        db.Sites.Add(site);
        db.SaveChanges();
        EnsureSapODataDachMapping(db, site.Id);
    }

    private static void EnsureSapODataDachMapping(AppDbContext db, int siteId)
    {
        var existingSources = db.SapSourceDefinitions.Where(x => x.SiteId == siteId).ToList();
        var existingMappings = db.SapFieldMappings.Where(x => x.SiteId == siteId).ToList();

        if (existingSources.Count > 0 || existingMappings.Count > 0)
        {
            var changed = false;
            foreach (var source in existingSources.Where(x =>
                         string.Equals(x.Alias, "Z", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(x.EntitySet, "ZSCHWEIZ", StringComparison.OrdinalIgnoreCase)))
            {
                source.EntitySet = "ZSCHWEIZSet";
                changed = true;
            }

            if (changed)
                db.SaveChanges();

            return;
        }

        db.SapSourceDefinitions.Add(new SapSourceDefinition
        {
            SiteId = siteId,
            Alias = "Z",
            EntitySet = "ZSCHWEIZSet",
            IsPrimary = true,
            IsActive = true,
            SortOrder = 0
        });

        var mappings = new (string Target, string Source, bool Required)[]
        {
            (nameof(SalesRecord.Tsc), "Z.TSC", true),
            (nameof(SalesRecord.Land), "Z.LAND1", true),
            (nameof(SalesRecord.DocumentEntry), "Z.VBELN", false),
            (nameof(SalesRecord.InvoiceNumber), "Z.VBELN", true),
            (nameof(SalesRecord.PositionOnInvoice), "Z.POSNR", true),
            (nameof(SalesRecord.InvoiceDate), "Z.FKDAT", true),
            (nameof(SalesRecord.Material), "Z.MATNR", false),
            (nameof(SalesRecord.Name), "Z.ARKTX", false),
            (nameof(SalesRecord.ProductGroup), "Z.PRODH", false),
            (nameof(SalesRecord.Quantity), "Z.FKIMG", false),
            (nameof(SalesRecord.CustomerNumber), "Z.KUNNR", false),
            (nameof(SalesRecord.CustomerName), "Z.NAME1", false),
            (nameof(SalesRecord.CustomerCountry), "Z.CUSTOMER_LAND", false),
            (nameof(SalesRecord.StandardCost), "=0", false),
            (nameof(SalesRecord.StandardCostCurrency), "Z.HWAER", false),
            (nameof(SalesRecord.SalesPriceValue), "Z.NETWR_HC", true),
            (nameof(SalesRecord.SalesCurrency), "Z.HWAER", true),
            (nameof(SalesRecord.DocumentCurrency), "Z.WAERK", false),
            (nameof(SalesRecord.DocumentTotalForeignCurrency), "Z.NETWR_DC", false),
            (nameof(SalesRecord.DocumentTotalLocalCurrency), "Z.NETWR_HC", false),
            (nameof(SalesRecord.VatSumForeignCurrency), "Z.TAX_DC", false),
            (nameof(SalesRecord.VatSumLocalCurrency), "Z.TAX_HC", false),
            (nameof(SalesRecord.DocumentRate), "Z.KURRF", false),
            (nameof(SalesRecord.CompanyCurrency), "Z.HWAER", true),
            (nameof(SalesRecord.DocumentType), "Z.FKART", false)
        };

        for (var i = 0; i < mappings.Length; i++)
        {
            db.SapFieldMappings.Add(new SapFieldMapping
            {
                SiteId = siteId,
                TargetField = mappings[i].Target,
                SourceExpression = mappings[i].Source,
                IsRequired = mappings[i].Required,
                IsActive = true,
                SortOrder = i
            });
        }

        db.SaveChanges();
    }
}
