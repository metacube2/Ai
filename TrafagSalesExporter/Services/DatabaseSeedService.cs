using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Security;

namespace TrafagSalesExporter.Services;

public class DatabaseSeedService : IDatabaseSeedService
{
    private const string SpainSharePointFolder = "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/Spanien";
    private const string FinanceImportRootFolder = "/Import/Finance";

    public void SeedDefaults(AppDbContext db)
    {
        SeedIfEmpty(db);
        EnsureFinanceCentralSharePointFolder(db);
        EnsureRecommendedTransformationRules(db);
        EnsureSourceSystemDefinitions(db);
        EnsureIndiaSageHanaConfiguration(db);
        EnsureCentralHanaServerRecords(db);
        EnsureSpainManualExcelSite(db);
        EnsureGermanyManualExcelSite(db);
        EnsureUkManualExcelFolder(db);
        EnsureSapODataDachSite(db);
        EnsurePurchasingSapSite(db);
        EnsureFinanceReferenceDefaults(db);
        EnsureBudgetExchangeRateDefaults(db);
        EnsureFinanceIntercompanyRuleDefaults(db);
        EnsureFinanceRuleDefaults(db);
        EnsureNavigationMenuDefaults(db);
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
            CentralExportFolder = FinanceImportRootFolder,
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
            LocalConsolidatedExportFolder = "",
            ExchangeRateDateField = ExchangeRateDateFields.PostingDate
        });

        db.SaveChanges();
    }

    private static void EnsureFinanceCentralSharePointFolder(AppDbContext db)
    {
        var config = db.SharePointConfigs.OrderBy(x => x.Id).FirstOrDefault();
        if (config is null || !string.IsNullOrWhiteSpace(config.CentralExportFolder))
            return;

        config.CentralExportFolder = FinanceImportRootFolder;
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

    private static void EnsureNavigationMenuDefaults(AppDbContext db)
    {
        var defaults = BuildDefaultNavigationMenuItems();
        var changed = false;

        foreach (var item in defaults)
        {
            var existing = db.NavigationMenuItems.FirstOrDefault(x => x.Key == item.Key);
            if (existing is null)
            {
                db.NavigationMenuItems.Add(item);
                changed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.TitleDe)) existing.TitleDe = item.TitleDe;
            if (string.IsNullOrWhiteSpace(existing.TitleEn)) existing.TitleEn = item.TitleEn;
            if (string.IsNullOrWhiteSpace(existing.Icon)) existing.Icon = item.Icon;
            if (string.IsNullOrWhiteSpace(existing.Href)) existing.Href = item.Href;
            if (string.IsNullOrWhiteSpace(existing.ItemType)) existing.ItemType = item.ItemType;
            if (string.IsNullOrWhiteSpace(existing.Match)) existing.Match = item.Match;
            if (string.IsNullOrWhiteSpace(existing.RequiredPolicy)) existing.RequiredPolicy = item.RequiredPolicy;
            if (existing.Key == "purchasing-ideas")
            {
                existing.ItemType = item.ItemType;
                existing.Href = item.Href;
                existing.Match = item.Match;
                existing.Icon = item.Icon;
                existing.IsExpanded = item.IsExpanded;
            }
            if (existing.Key == "management-home")
            {
                existing.ParentKey = item.ParentKey;
                existing.SortOrder = item.SortOrder;
                existing.Href = item.Href;
                existing.Match = item.Match;
                existing.Icon = item.Icon;
            }
            existing.IsSystem = true;
            changed = true;
        }

        if (changed)
            db.SaveChanges();
    }

    private static List<NavigationMenuItem> BuildDefaultNavigationMenuItems() =>
    [
        Link("management-home", null, "Home", "Home", "Home", string.Empty, 0, "All"),
        Group("finance", null, "Finance Cockpit", "Finance Cockpit", "Analytics", 10, expanded: true),
        Link("export-dashboard", "finance", "Export Dashboard", "Export dashboard", "Dashboard", "export-dashboard", 10),
        Group("management-analysis", "finance", "Management Analyse", "Management analysis", "QueryStats", 20),
        Link("management-quick", "management-analysis", "Schnelluebersicht", "Quick overview", "Speed", "management-cockpit", 10, "All"),
        Link("management-decisions", "management-analysis", "Management Entscheidungen", "Management decisions", "Rule", "management-cockpit?section=decisions", 20, "All"),
        Group("experts", "management-analysis", "Experten", "Experts", "Tune", 40),
        Link("finance-summary", "experts", "Finance Summary", "Finance summary", "Dashboard", "management-cockpit?section=summary", 10, "All"),
        Link("country-diagnostics", "experts", "Laender Diagnose", "Country diagnostics", "Public", "management-cockpit?section=countries", 20, "All"),
        Link("data-status", "experts", "Datenstatus", "Data status", "FactCheck", "management-cockpit?section=status", 30, "All"),
        Link("deviations", "experts", "Abweichungen", "Deviations", "WarningAmber", "management-cockpit?section=deviations", 40, "All"),
        Link("credits", "experts", "Gutschriften", "Credit notes", "AssignmentReturn", "management-cockpit?section=credits", 50, "All"),
        Link("data-quality", "experts", "Datenqualitaet", "Data quality", "Rule", "management-cockpit?section=quality", 60, "All"),
        Link("division-finance", "experts", "Sparten-Finanzanalyse", "Division finance", "PieChart", "management-cockpit?section=division&division=finance", 70, "All"),
        Link("division-central", "experts", "Zentrale Spartenzuordnung", "Central division mapping", "AccountTree", "management-cockpit?section=division&division=central", 80, "All"),
        Link("group-margin", "experts", "Gruppenmarge", "Group margin", "StackedLineChart", "management-cockpit?section=groupmargin", 90, "All"),
        Link("finance-3d", "experts", "3D Datenanalyse", "3D data analysis", "ViewInAr", "management-cockpit?section=3d", 100, "All"),
        Link("raw-diagnostics", "experts", "Rohdaten Diagnose", "Raw-data diagnostics", "QueryStats", "management-cockpit?section=raw", 110, "All"),
        Link("finance-comparison", "finance", "Soll/Ist Vergleich", "Actual/reference comparison", "CompareArrows", "finance-cockpit/vergleich", 30),
        Link("finance-training", "finance", "Finance Schulung", "Finance training", "School", "finance-cockpit/schulung", 40),
        Link("manual-imports", "finance", "Manuelle Importe", "Manual imports", "UploadFile", "manual-imports", 50),
        Group("finance-admin", "finance", "Admin", "Admin", "AdminPanelSettings", 60),
        Link("sites", "finance-admin", "Standorte", "Sites", "LocationOn", "standorte", 10, requiredPolicy: SecurityPolicies.AdminOnly),
        Link("transformations", "finance-admin", "Transformationen", "Transformations", "Transform", "transformations", 20, requiredPolicy: SecurityPolicies.AdminOnly),
        Link("finance-rules", "finance-admin", "Finance Regeln", "Finance rules", "Rule", "finance-rules", 30, requiredPolicy: SecurityPolicies.AdminOnly),
        Link("settings", "finance-admin", "Settings", "Settings", "Settings", "settings", 40, requiredPolicy: SecurityPolicies.AdminOnly),
        Link("menu-structure", "finance-admin", "Menuestruktur", "Menu structure", "AccountTree", "admin/menu-structure", 45, requiredPolicy: SecurityPolicies.AdminOnly),
        Link("logs", "finance-admin", "Logs", "Logs", "List", "logs", 50),
        Action("finance-lock", "finance", "Finance sperren", "Lock finance", "Lock", 70),
        Group("hr", null, "HR KPI (Login)", "HR KPI (login)", "Groups", 20),
        Link("hr-dashboard", "hr", "HR Dashboard", "HR dashboard", "Dashboard", "hr-kpi", 10, "All"),
        Link("hr-training", "hr", "HR KPI Schulung", "HR KPI training", "School", "hr-kpi/schulung", 20),
        Group("purchasing", null, "Einkauf", "Purchasing", "ShoppingCart", 30),
        Link("purchasing-dashboard", "purchasing", "Einkauf Dashboard", "Purchasing dashboard", "Dashboard", "einkauf", 10, "All"),
        Link("purchasing-spend", "purchasing", "Spend", "Spend", "Payments", "einkauf/spend", 20, "All"),
        Link("purchasing-open-orders", "purchasing", "Offene Bestellungen", "Open orders", "PendingActions", "einkauf/offene-bestellungen", 30, "All"),
        Link("purchasing-contracts", "purchasing", "Kontrakte", "Contracts", "Assignment", "einkauf/kontrakte", 40, "All"),
        Link("purchasing-suppliers", "purchasing", "Lieferanten", "Suppliers", "Verified", "einkauf/lieferanten", 50, "All"),
        Group("purchasing-ideas", "purchasing", "Ideen", "Ideas", "Lightbulb", 60, expanded: true),
        Link("purchasing-ideas-overview", "purchasing-ideas", "Uebersicht", "Overview", "Lightbulb", "einkauf/ideen", 10, "All"),
        Link("purchasing-idea-data-service", "purchasing-ideas", "Einkauf-Datenservice", "Purchasing data service", "Storage", "einkauf/ideen/datenservice", 20, "All"),
        Link("purchasing-idea-delivery-risk", "purchasing-ideas", "Liefertermin-Risiko", "Delivery due-date risk", "PendingActions", "einkauf/ideen/liefertermin-risiko", 30, "All"),
        Link("purchasing-idea-price-variance", "purchasing-ideas", "Preisentwicklung", "Price trend", "TrendingUp", "einkauf/ideen/preisabweichung", 40, "All"),
        Link("purchasing-idea-spend-concentration", "purchasing-ideas", "Spend-Konzentration", "Spend concentration", "PieChart", "einkauf/ideen/spend-konzentration", 50, "All"),
        Link("purchasing-idea-data-quality", "purchasing-ideas", "Datenqualitaet", "Data quality", "FactCheck", "einkauf/ideen/datenqualitaet", 60, "All"),
        Link("purchasing-kpi-catalog", "purchasing", "Kennzahlen-Katalog", "KPI catalogue", "Checklist", "einkauf/kennzahlen", 70, "All"),
        Link("purchasing-pbix", "purchasing", "PBIX Vorlage", "PBIX template", "InsertChart", "einkauf/pbix", 80, "All"),
        Link("purchasing-3d", "purchasing", "3D Simulation", "3D simulation", "ViewInAr", "einkauf/3d", 90, "All"),
        Link("purchasing-data-sources", "purchasing", "Datenquellen", "Data sources", "Hub", "einkauf/verbindungen", 100, "All"),
        Link("admin-sessions", null, "Admin Bereich", "Admin area", "PeopleAlt", "admin/sessions", 90)
    ];

    private static NavigationMenuItem Group(string key, string? parentKey, string titleDe, string titleEn, string icon, int sortOrder, bool expanded = false)
        => new()
        {
            Key = key,
            ParentKey = parentKey,
            TitleDe = titleDe,
            TitleEn = titleEn,
            Icon = icon,
            ItemType = NavigationMenuItemTypes.Group,
            IsExpanded = expanded,
            SortOrder = sortOrder
        };

    private static NavigationMenuItem Link(string key, string? parentKey, string titleDe, string titleEn, string icon, string href, int sortOrder, string match = "Prefix", string requiredPolicy = "")
        => new()
        {
            Key = key,
            ParentKey = parentKey,
            TitleDe = titleDe,
            TitleEn = titleEn,
            Icon = icon,
            Href = href,
            Match = match,
            RequiredPolicy = requiredPolicy,
            ItemType = NavigationMenuItemTypes.Link,
            SortOrder = sortOrder
        };

    private static NavigationMenuItem Action(string key, string? parentKey, string titleDe, string titleEn, string icon, int sortOrder)
        => new()
        {
            Key = key,
            ParentKey = parentKey,
            TitleDe = titleDe,
            TitleEn = titleEn,
            Icon = icon,
            ItemType = NavigationMenuItemTypes.Action,
            SortOrder = sortOrder
        };

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

    private static void EnsureIndiaSageHanaConfiguration(AppDbContext db)
    {
        const string sageSourceSystem = "SAGE";
        const string indiaTsc = "TRIN";
        const string indiaSchema = "TRAFAG_LIVE";
        const string indiaHost = "20.197.20.60";
        const int indiaPort = 30015;

        var site = db.Sites
            .Include(x => x.HanaServer)
            .OrderBy(x => x.Id)
            .FirstOrDefault(x => x.TSC == indiaTsc || x.Land == "Indien" || x.Land == "India");

        if (site is null)
            return;

        var changed = false;
        var sourceServer = db.HanaServers
            .OrderBy(x => x.Id)
            .FirstOrDefault(x => x.Host == indiaHost)
            ?? site.HanaServer;

        var centralSageServer = db.HanaServers
            .OrderBy(x => x.Id)
            .FirstOrDefault(x => x.SourceSystem == sageSourceSystem);

        if (centralSageServer is null)
        {
            centralSageServer = sourceServer ?? new HanaServer
            {
                Name = sageSourceSystem,
                Host = indiaHost,
                Port = indiaPort,
                Username = string.Empty,
                Password = string.Empty,
                DatabaseName = string.Empty,
                AdditionalParams = string.Empty
            };

            if (centralSageServer.Id == 0)
            {
                db.HanaServers.Add(centralSageServer);
            }
        }

        if (centralSageServer.SourceSystem != sageSourceSystem)
        {
            centralSageServer.SourceSystem = sageSourceSystem;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(centralSageServer.Name))
        {
            centralSageServer.Name = sageSourceSystem;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(centralSageServer.Host))
        {
            centralSageServer.Host = !string.IsNullOrWhiteSpace(sourceServer?.Host)
                ? sourceServer.Host
                : indiaHost;
            changed = true;
        }

        if (centralSageServer.Port <= 0)
        {
            centralSageServer.Port = sourceServer?.Port > 0 ? sourceServer.Port : indiaPort;
            changed = true;
        }

        if (sourceServer is not null && !ReferenceEquals(sourceServer, centralSageServer))
        {
            if (string.IsNullOrWhiteSpace(centralSageServer.DatabaseName) &&
                !string.IsNullOrWhiteSpace(sourceServer.DatabaseName))
            {
                centralSageServer.DatabaseName = sourceServer.DatabaseName;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(centralSageServer.AdditionalParams) &&
                !string.IsNullOrWhiteSpace(sourceServer.AdditionalParams))
            {
                centralSageServer.AdditionalParams = sourceServer.AdditionalParams;
                changed = true;
            }

            if (centralSageServer.UseSsl != sourceServer.UseSsl)
            {
                centralSageServer.UseSsl = sourceServer.UseSsl;
                changed = true;
            }

            if (centralSageServer.ValidateCertificate != sourceServer.ValidateCertificate)
            {
                centralSageServer.ValidateCertificate = sourceServer.ValidateCertificate;
                changed = true;
            }
        }

        if (site.SourceSystem != sageSourceSystem)
        {
            site.SourceSystem = sageSourceSystem;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(site.Schema))
        {
            site.Schema = indiaSchema;
            changed = true;
        }

        if (site.HanaServerId != centralSageServer.Id || site.HanaServerId is null)
        {
            site.HanaServer = centralSageServer;
            changed = true;
        }

        if (changed || centralSageServer.Id == 0)
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

            if (ShouldRepairSpainManualImportPath(existing.ManualImportFilePath))
            {
                existing.ManualImportFilePath = SpainSharePointFolder;
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
            ManualImportFilePath = SpainSharePointFolder,
            IsActive = false
        });
        db.SaveChanges();
    }

    private static bool ShouldRepairSpainManualImportPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;

        var normalized = path.Trim().Replace('\\', '/');
        return normalized.Contains("/Import/Finance/Spanien/Spain_Sales_", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith("/Import/Finance/Spanien/Spain_Sales_2025.csv", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureGermanyManualExcelSite(AppDbContext db)
    {
        if (db.Sites.Count() <= 1)
            return;

        var existing = db.Sites
            .OrderBy(x => x.Id)
            .FirstOrDefault(x =>
                x.TSC == "TRDE" ||
                x.Land == "Deutschland" ||
                x.Land == "Germany");

        if (existing is null)
        {
            existing = new Site
            {
                Schema = string.Empty,
                TSC = "TRDE",
                Land = "Deutschland",
                SourceSystem = "MANUAL_EXCEL",
                IsActive = false
            };
            db.Sites.Add(existing);
            db.SaveChanges();
        }
        else
        {
            var changed = false;

            if (string.IsNullOrWhiteSpace(existing.TSC))
            {
                existing.TSC = "TRDE";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Land))
            {
                existing.Land = "Deutschland";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.SourceSystem))
            {
                existing.SourceSystem = "MANUAL_EXCEL";
                changed = true;
            }

            if (changed)
                db.SaveChanges();
        }

        if (CanSeedSiteDependentTable(db, "ManualExcelColumnMappings"))
            EnsureGermanyManualExcelMapping(db, existing.Id);
    }

    private static void EnsureUkManualExcelFolder(AppDbContext db)
    {
        var existing = db.Sites
            .OrderBy(x => x.Id)
            .FirstOrDefault(x =>
                x.TSC == "TRUK" ||
                x.Land == "England" ||
                x.Land == "UK");

        if (existing is null)
            return;

        var changed = false;
        if (string.IsNullOrWhiteSpace(existing.SourceSystem))
        {
            existing.SourceSystem = "MANUAL_EXCEL";
            changed = true;
        }

        if (string.Equals(existing.SourceSystem, "MANUAL_EXCEL", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(existing.ManualImportFilePath) ||
             existing.ManualImportFilePath.Contains("/England", StringComparison.OrdinalIgnoreCase)))
        {
            existing.ManualImportFilePath = "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/UK_B1";
            changed = true;
        }

        if (changed)
            db.SaveChanges();

        if (CanSeedSiteDependentTable(db, "ManualExcelColumnMappings"))
            EnsureUkManualExcelMapping(db, existing.Id);
    }

    private static bool CanSeedSiteDependentTable(AppDbContext db, string tableName)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        var columns = DatabaseSchemaTools.GetTableColumns(conn, transaction: null, tableName);
        if (columns.Count == 0)
            return false;

        return !DatabaseSchemaTools.TableReferences(conn, tableName, "Sites_old") &&
               !DatabaseSchemaTools.TableReferencesObsoleteTable(conn, tableName, "Sites");
    }

    private static void EnsureUkManualExcelMapping(AppDbContext db, int siteId)
    {
        var mappings = new (string Target, string Source, bool Required)[]
        {
            (nameof(SalesRecord.Tsc), "TSC", false),
            (nameof(SalesRecord.Land), "Land", false),
            (nameof(SalesRecord.InvoiceNumber), "Invoice Number", true),
            (nameof(SalesRecord.PositionOnInvoice), "Position on invoice", false),
            (nameof(SalesRecord.Material), "Material", false),
            (nameof(SalesRecord.Name), "Name", false),
            (nameof(SalesRecord.ProductGroup), "Product Group", false),
            (nameof(SalesRecord.Quantity), "Quantity", true),
            (nameof(SalesRecord.CustomerNumber), "Customer number", false),
            (nameof(SalesRecord.CustomerName), "Customer name", false),
            (nameof(SalesRecord.CustomerCountry), "Customer country", false),
            (nameof(SalesRecord.SalesPriceValue), "=SageNetSales([Sales Price/Value], [Quantity], [Document Type], [DocumentType], [Type])", true),
            (nameof(SalesRecord.SalesCurrency), "=GBP", false),
            (nameof(SalesRecord.DocumentCurrency), "=GBP", false),
            (nameof(SalesRecord.CompanyCurrency), "=GBP", false),
            (nameof(SalesRecord.PostingDate), "invoice date", false),
            (nameof(SalesRecord.InvoiceDate), "invoice date", false),
            (nameof(SalesRecord.DocumentType), "Document Type", false)
        };

        var changed = false;
        for (var i = 0; i < mappings.Length; i++)
        {
            var mapping = db.ManualExcelColumnMappings
                .OrderBy(x => x.Id)
                .FirstOrDefault(x => x.SiteId == siteId && x.TargetField == mappings[i].Target);

            if (mapping is null)
            {
                db.ManualExcelColumnMappings.Add(new ManualExcelColumnMapping
                {
                    SiteId = siteId,
                    TargetField = mappings[i].Target,
                    SourceHeader = mappings[i].Source,
                    IsRequired = mappings[i].Required,
                    IsActive = true,
                    SortOrder = i
                });
                changed = true;
                continue;
            }

            if (mapping.SourceHeader != mappings[i].Source)
            {
                mapping.SourceHeader = mappings[i].Source;
                changed = true;
            }

            if (mapping.IsRequired != mappings[i].Required)
            {
                mapping.IsRequired = mappings[i].Required;
                changed = true;
            }

            if (!mapping.IsActive)
            {
                mapping.IsActive = true;
                changed = true;
            }

            if (mapping.SortOrder != i)
            {
                mapping.SortOrder = i;
                changed = true;
            }
        }

        if (changed)
            db.SaveChanges();
    }

    private static void EnsureGermanyManualExcelMapping(AppDbContext db, int siteId)
    {
        var mappings = new (string Target, string Source, bool Required)[]
        {
            (nameof(SalesRecord.ExtractionDate), "Export-Datum", false),
            (nameof(SalesRecord.Tsc), "=TRDE", false),
            (nameof(SalesRecord.Land), "=Deutschland", false),
            (nameof(SalesRecord.InvoiceNumber), "Belegnummer", true),
            (nameof(SalesRecord.PositionOnInvoice), "Position", false),
            (nameof(SalesRecord.Material), "ArtikelNummer", false),
            (nameof(SalesRecord.Name), "ArtikelBezeichnung", false),
            (nameof(SalesRecord.ProductGroup), "Warengruppen-Bezeichnung", false),
            (nameof(SalesRecord.Quantity), "Anz. VE", false),
            (nameof(SalesRecord.SupplierNumber), "Lieferanten Nummer", false),
            (nameof(SalesRecord.SupplierName), "Name Lieferant", false),
            (nameof(SalesRecord.SupplierCountry), "Land Lieferant", false),
            (nameof(SalesRecord.CustomerNumber), "AdressNummer-Kunde", false),
            (nameof(SalesRecord.CustomerName), "Name Kunde", false),
            (nameof(SalesRecord.CustomerCountry), "Land Kunde", false),
            (nameof(SalesRecord.CustomerIndustry), "Branche", false),
            (nameof(SalesRecord.StandardCost), "EinstandsPreis", false),
            (nameof(SalesRecord.StandardCostCurrency), "W\u00e4hrung", false),
            (nameof(SalesRecord.SalesPriceValue), "NettoPreisGesamtX", true),
            (nameof(SalesRecord.SalesCurrency), "W\u00e4hrung", false),
            (nameof(SalesRecord.DocumentCurrency), "W\u00e4hrung", false),
            (nameof(SalesRecord.CompanyCurrency), "W\u00e4hrung", false),
            (nameof(SalesRecord.Incoterms2020), "Versandbedingung", false),
            (nameof(SalesRecord.SalesResponsibleEmployee), "AdressNummer_V", false),
            (nameof(SalesRecord.PostingDate), "Belegdatum-Rechnung", false),
            (nameof(SalesRecord.InvoiceDate), "Belegdatum-Rechnung", false),
            (nameof(SalesRecord.OrderDate), "BelegDatum Auftrag", false),
            (nameof(SalesRecord.DocumentType), "=Alphaplan Excel", false)
        };

        var changed = false;
        for (var i = 0; i < mappings.Length; i++)
        {
            var mapping = db.ManualExcelColumnMappings
                .OrderBy(x => x.Id)
                .FirstOrDefault(x => x.SiteId == siteId && x.TargetField == mappings[i].Target);

            if (mapping is null)
            {
                db.ManualExcelColumnMappings.Add(new ManualExcelColumnMapping
                {
                    SiteId = siteId,
                    TargetField = mappings[i].Target,
                    SourceHeader = mappings[i].Source,
                    IsRequired = mappings[i].Required,
                    IsActive = true,
                    SortOrder = i
                });
                changed = true;
                continue;
            }

            if (mapping.SourceHeader != mappings[i].Source)
            {
                mapping.SourceHeader = mappings[i].Source;
                changed = true;
            }

            if (mapping.IsRequired != mappings[i].Required)
            {
                mapping.IsRequired = mappings[i].Required;
                changed = true;
            }

            if (!mapping.IsActive)
            {
                mapping.IsActive = true;
                changed = true;
            }

            if (mapping.SortOrder != i)
            {
                mapping.SortOrder = i;
                changed = true;
            }
        }

        if (changed)
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
        var changed = false;
        var source = db.SapSourceDefinitions
            .OrderBy(x => x.Id)
            .FirstOrDefault(x => x.SiteId == siteId && x.Alias == "Z");

        if (source is null)
        {
            db.SapSourceDefinitions.Add(new SapSourceDefinition
            {
                SiteId = siteId,
                Alias = "Z",
                EntitySet = "FinanzdataSchweizOeSet",
                IsPrimary = true,
                IsActive = true,
                SortOrder = 0
            });
            changed = true;
        }
        else
        {
            if (source.EntitySet != "FinanzdataSchweizOeSet")
            {
                source.EntitySet = "FinanzdataSchweizOeSet";
                changed = true;
            }

            if (!source.IsPrimary)
            {
                source.IsPrimary = true;
                changed = true;
            }

            if (!source.IsActive)
            {
                source.IsActive = true;
                changed = true;
            }

            if (source.SortOrder != 0)
            {
                source.SortOrder = 0;
                changed = true;
            }
        }

        var obsoleteSources = db.SapSourceDefinitions
            .Where(x => x.SiteId == siteId && x.Alias != "Z" && x.Alias != "P" && x.Alias != "M")
            .ToList();
        foreach (var obsoleteSource in obsoleteSources)
        {
            if (obsoleteSource.IsActive)
            {
                obsoleteSource.IsActive = false;
                changed = true;
            }

            if (obsoleteSource.IsPrimary)
            {
                obsoleteSource.IsPrimary = false;
                changed = true;
            }
        }

        var productSource = db.SapSourceDefinitions
            .OrderBy(x => x.Id)
            .FirstOrDefault(x => x.SiteId == siteId && x.Alias == "P");

        if (productSource is null)
        {
            db.SapSourceDefinitions.Add(new SapSourceDefinition
            {
                SiteId = siteId,
                Alias = "P",
                EntitySet = "ProductDivisionRefSet",
                IsPrimary = false,
                IsActive = true,
                SortOrder = 1
            });
            changed = true;
        }
        else
        {
            if (productSource.EntitySet != "ProductDivisionRefSet")
            {
                productSource.EntitySet = "ProductDivisionRefSet";
                changed = true;
            }

            if (productSource.IsPrimary)
            {
                productSource.IsPrimary = false;
                changed = true;
            }

            if (!productSource.IsActive)
            {
                productSource.IsActive = true;
                changed = true;
            }

            if (productSource.SortOrder != 1)
            {
                productSource.SortOrder = 1;
                changed = true;
            }
        }

        var productMapSource = db.SapSourceDefinitions
            .OrderBy(x => x.Id)
            .FirstOrDefault(x => x.SiteId == siteId && x.Alias == "M");

        if (productMapSource is null)
        {
            db.SapSourceDefinitions.Add(new SapSourceDefinition
            {
                SiteId = siteId,
                Alias = "M",
                EntitySet = "ProductDivisionMapSet",
                IsPrimary = false,
                IsActive = false,
                SortOrder = 2
            });
            changed = true;
        }
        else
        {
            if (productMapSource.EntitySet != "ProductDivisionMapSet")
            {
                productMapSource.EntitySet = "ProductDivisionMapSet";
                changed = true;
            }

            if (productMapSource.IsPrimary)
            {
                productMapSource.IsPrimary = false;
                changed = true;
            }

            if (productMapSource.IsActive)
            {
                productMapSource.IsActive = false;
                changed = true;
            }

            if (productMapSource.SortOrder != 2)
            {
                productMapSource.SortOrder = 2;
                changed = true;
            }
        }

        var productJoin = db.SapJoinDefinitions
            .OrderBy(x => x.Id)
            .FirstOrDefault(x =>
                x.SiteId == siteId &&
                x.LeftAlias == "Z" &&
                x.RightAlias == "P");

        if (productJoin is null)
        {
            db.SapJoinDefinitions.Add(new SapJoinDefinition
            {
                SiteId = siteId,
                LeftAlias = "Z",
                RightAlias = "P",
                LeftKeys = "Matnr",
                RightKeys = "Matnr",
                JoinType = "Left",
                IsActive = true,
                SortOrder = 1
            });
            changed = true;
        }
        else
        {
            if (productJoin.LeftKeys != "Matnr")
            {
                productJoin.LeftKeys = "Matnr";
                changed = true;
            }

            if (productJoin.RightKeys != "Matnr")
            {
                productJoin.RightKeys = "Matnr";
                changed = true;
            }

            if (productJoin.JoinType != "Left")
            {
                productJoin.JoinType = "Left";
                changed = true;
            }

            if (!productJoin.IsActive)
            {
                productJoin.IsActive = true;
                changed = true;
            }

            if (productJoin.SortOrder != 1)
            {
                productJoin.SortOrder = 1;
                changed = true;
            }
        }

        var productMapJoin = db.SapJoinDefinitions
            .OrderBy(x => x.Id)
            .FirstOrDefault(x =>
                x.SiteId == siteId &&
                x.LeftAlias == "Z" &&
                x.RightAlias == "M");

        if (productMapJoin is null)
        {
            db.SapJoinDefinitions.Add(new SapJoinDefinition
            {
                SiteId = siteId,
                LeftAlias = "Z",
                RightAlias = "M",
                LeftKeys = "Prodh",
                RightKeys = "Paph1",
                JoinType = "Left",
                IsActive = false,
                SortOrder = 2
            });
            changed = true;
        }
        else
        {
            if (productMapJoin.LeftKeys != "Prodh")
            {
                productMapJoin.LeftKeys = "Prodh";
                changed = true;
            }

            if (productMapJoin.RightKeys != "Paph1")
            {
                productMapJoin.RightKeys = "Paph1";
                changed = true;
            }

            if (productMapJoin.JoinType != "Left")
            {
                productMapJoin.JoinType = "Left";
                changed = true;
            }

            if (productMapJoin.IsActive)
            {
                productMapJoin.IsActive = false;
                changed = true;
            }

            if (productMapJoin.SortOrder != 2)
            {
                productMapJoin.SortOrder = 2;
                changed = true;
            }
        }

        var mappings = new (string Target, string Source, bool Required)[]
        {
            (nameof(SalesRecord.Tsc), "Z.Tsc", true),
            (nameof(SalesRecord.Land), "Z.Land1", true),
            (nameof(SalesRecord.DocumentEntry), "Z.Vbeln", false),
            (nameof(SalesRecord.InvoiceNumber), "Z.Vbeln", true),
            (nameof(SalesRecord.PositionOnInvoice), "Z.Posnr", true),
            (nameof(SalesRecord.PostingDate), "Z.Fkdat", true),
            (nameof(SalesRecord.InvoiceDate), "Z.Fkdat", true),
            (nameof(SalesRecord.Material), "Z.Matnr", false),
            (nameof(SalesRecord.Name), "Z.Arktx", false),
            (nameof(SalesRecord.ProductGroup), "Z.Prodh", false),
            (nameof(SalesRecord.ProductHierarchyCode), "P.Paph1", false),
            (nameof(SalesRecord.ProductHierarchyText), "P.Paph1Text", false),
            (nameof(SalesRecord.ProductFamilyCode), "P.Wwpfa", false),
            (nameof(SalesRecord.ProductFamilyText), "P.WwpfaText", false),
            (nameof(SalesRecord.ProductDivisionCode), "P.Wwpsp", false),
            (nameof(SalesRecord.ProductDivisionText), "P.WwpspText", false),
            (nameof(SalesRecord.ProductMappingAssigned), "P.IsAssigned", false),
            (nameof(SalesRecord.Quantity), "Z.Fkimg", false),
            (nameof(SalesRecord.CustomerNumber), "Z.Kunnr", false),
            (nameof(SalesRecord.CustomerName), "Z.Name1", false),
            (nameof(SalesRecord.CustomerCountry), "Z.CustomerLand", false),
            (nameof(SalesRecord.StandardCost), "=0", false),
            (nameof(SalesRecord.StandardCostCurrency), "Z.Hwaer", false),
            (nameof(SalesRecord.SalesPriceValue), "Z.NetwrHc", true),
            (nameof(SalesRecord.SalesCurrency), "Z.Hwaer", true),
            (nameof(SalesRecord.DocumentCurrency), "Z.Waerk", false),
            (nameof(SalesRecord.DocumentTotalForeignCurrency), "Z.NetwrDc", false),
            (nameof(SalesRecord.DocumentTotalLocalCurrency), "Z.NetwrHc", false),
            (nameof(SalesRecord.VatSumForeignCurrency), "=0", false),
            (nameof(SalesRecord.VatSumLocalCurrency), "=0", false),
            (nameof(SalesRecord.DocumentRate), "Z.Kurrf", false),
            (nameof(SalesRecord.CompanyCurrency), "Z.Hwaer", true),
            (nameof(SalesRecord.DocumentType), "Z.Fkart", false)
        };

        for (var i = 0; i < mappings.Length; i++)
        {
            var mapping = db.SapFieldMappings
                .OrderBy(x => x.Id)
                .FirstOrDefault(x => x.SiteId == siteId && x.TargetField == mappings[i].Target);

            if (mapping is null)
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
                changed = true;
                continue;
            }

            if (mapping.SourceExpression != mappings[i].Source)
            {
                mapping.SourceExpression = mappings[i].Source;
                changed = true;
            }

            if (mapping.IsRequired != mappings[i].Required)
            {
                mapping.IsRequired = mappings[i].Required;
                changed = true;
            }

            if (!mapping.IsActive)
            {
                mapping.IsActive = true;
                changed = true;
            }

            if (mapping.SortOrder != i)
            {
                mapping.SortOrder = i;
                changed = true;
            }
        }

        if (changed)
            db.SaveChanges();
    }

    private static void EnsurePurchasingSapSite(AppDbContext db)
    {
        if (db.Sites.Count() <= 1)
            return;

        var site = db.Sites
            .OrderBy(x => x.Id)
            .FirstOrDefault(x => x.TSC == PurchasingDataSourcePageService.PurchasingTsc);

        var changed = false;
        if (site is null)
        {
            site = new Site
            {
                Schema = string.Empty,
                TSC = PurchasingDataSourcePageService.PurchasingTsc,
                Land = "Einkauf SAP",
                SourceSystem = "SAP",
                IsActive = false
            };
            db.Sites.Add(site);
            db.SaveChanges();
        }
        else
        {
            if (site.SourceSystem != "SAP")
            {
                site.SourceSystem = "SAP";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(site.Land))
            {
                site.Land = "Einkauf SAP";
                changed = true;
            }
        }

        if (!db.SapSourceDefinitions.Any(x => x.SiteId == site.Id))
        {
            db.SapSourceDefinitions.AddRange(
                new SapSourceDefinition { SiteId = site.Id, Alias = "EKKO", EntitySet = "EKKOSet", IsPrimary = true, IsActive = true, SortOrder = 10 },
                new SapSourceDefinition { SiteId = site.Id, Alias = "EKPO", EntitySet = "EKPOSet", IsPrimary = false, IsActive = true, SortOrder = 20 },
                new SapSourceDefinition { SiteId = site.Id, Alias = "EKET", EntitySet = "eketSet", IsPrimary = false, IsActive = true, SortOrder = 30 },
                new SapSourceDefinition { SiteId = site.Id, Alias = "LIEF", EntitySet = "Data", IsPrimary = false, IsActive = true, SortOrder = 40 },
                new SapSourceDefinition { SiteId = site.Id, Alias = "WG", EntitySet = "Data2", IsPrimary = false, IsActive = true, SortOrder = 50 },
                new SapSourceDefinition { SiteId = site.Id, Alias = "MARA", EntitySet = "MARA001Set", IsPrimary = false, IsActive = true, SortOrder = 60 });
            changed = true;
        }

        if (!db.SapJoinDefinitions.Any(x => x.SiteId == site.Id))
        {
            db.SapJoinDefinitions.AddRange(
                new SapJoinDefinition { SiteId = site.Id, LeftAlias = "EKKO", RightAlias = "EKPO", LeftKeys = "Ebeln", RightKeys = "Ebeln", JoinType = "Left", IsActive = true, SortOrder = 10 },
                new SapJoinDefinition { SiteId = site.Id, LeftAlias = "EKPO", RightAlias = "EKET", LeftKeys = "Ebeln,Ebelp", RightKeys = "Ebeln,Ebelp", JoinType = "Left", IsActive = true, SortOrder = 20 },
                new SapJoinDefinition { SiteId = site.Id, LeftAlias = "EKKO", RightAlias = "LIEF", LeftKeys = "Lifnr", RightKeys = "Lifnr", JoinType = "Left", IsActive = true, SortOrder = 30 },
                new SapJoinDefinition { SiteId = site.Id, LeftAlias = "EKPO", RightAlias = "WG", LeftKeys = "Matkl", RightKeys = "Matkl", JoinType = "Left", IsActive = true, SortOrder = 40 },
                new SapJoinDefinition { SiteId = site.Id, LeftAlias = "EKPO", RightAlias = "MARA", LeftKeys = "Matnr", RightKeys = "Matnr", JoinType = "Left", IsActive = true, SortOrder = 50 });
            changed = true;
        }

        if (!db.SapFieldMappings.Any(x => x.SiteId == site.Id))
        {
            db.SapFieldMappings.AddRange(
                new SapFieldMapping { SiteId = site.Id, TargetField = "PurchaseOrder", SourceExpression = "EKKO.Ebeln", IsRequired = true, IsActive = true, SortOrder = 10 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "PurchaseOrderDate", SourceExpression = "EKKO.Bedat", IsRequired = true, IsActive = true, SortOrder = 20 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "SupplierNumber", SourceExpression = "EKKO.Lifnr", IsRequired = false, IsActive = true, SortOrder = 30 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "SupplierName", SourceExpression = "LIEF.Name", IsRequired = false, IsActive = true, SortOrder = 40 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "Position", SourceExpression = "EKPO.Ebelp", IsRequired = true, IsActive = true, SortOrder = 50 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "Material", SourceExpression = "EKPO.Matnr", IsRequired = false, IsActive = true, SortOrder = 60 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "MaterialText", SourceExpression = "EKPO.Txz01", IsRequired = false, IsActive = true, SortOrder = 70 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "MaterialGroup", SourceExpression = "EKPO.Matkl", IsRequired = false, IsActive = true, SortOrder = 80 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "MaterialGroupText", SourceExpression = "WG.WgKomplett", IsRequired = false, IsActive = true, SortOrder = 90 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "NetValueChf", SourceExpression = "EKPO.NetwrChf", IsRequired = false, IsActive = true, SortOrder = 100 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "NetValueChfPerPiece", SourceExpression = "EKPO.NetwrChfStk", IsRequired = false, IsActive = true, SortOrder = 110 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "OrderQuantity", SourceExpression = "EKPO.Menge", IsRequired = false, IsActive = true, SortOrder = 120 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "ScheduleDate", SourceExpression = "EKET.Eindt", IsRequired = false, IsActive = true, SortOrder = 130 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "ScheduleQuantity", SourceExpression = "EKET.Menge", IsRequired = false, IsActive = true, SortOrder = 140 },
                new SapFieldMapping { SiteId = site.Id, TargetField = "MaterialStatus", SourceExpression = "MARA.Mstae", IsRequired = false, IsActive = true, SortOrder = 150 });
            changed = true;
        }

        if (changed)
            db.SaveChanges();
    }

    private static void EnsureFinanceReferenceDefaults(AppDbContext db)
    {
        var defaults = new[]
        {
            new FinanceReference { Key = "AT", Label = "Trafag AT", Year = 2025, LocalCurrencyValue = 3443863m },
            new FinanceReference { Key = "CH", Label = "Trafag CH", Year = 2025 },
            new FinanceReference { Key = "CN", Label = "Trafag CN", Year = 2025 },
            new FinanceReference { Key = "CZ", Label = "Trafag CZ", Year = 2025, LocalCurrencyValue = 95458782m },
            new FinanceReference { Key = "DE", Label = "Trafag DE", Year = 2025, LocalCurrencyValue = 3652394.46m },
            new FinanceReference { Key = "ES", Label = "Trafag ES", Year = 2025, LocalCurrencyValue = 3082320.18m, Notes = "Sitzung 2026-06-01: ES-Ist 3'082'320.18 EUR fachlich bestaetigt; alter Sollwert 3'102'333.61 war Referenz-/Excel-Fehler." },
            new FinanceReference { Key = "FR", Label = "Trafag FR", Year = 2025, LocalCurrencyValue = 1450582m, CheckValue = 1471218m },
            new FinanceReference { Key = "GFS", Label = "Trafag GfS", Year = 2025, LocalCurrencyValue = 6495513m },
            new FinanceReference { Key = "IN", Label = "Trafag IN", Year = 2025, LocalCurrencyValue = 747341702m, CheckValue = 750936591m },
            new FinanceReference { Key = "IT", Label = "Trafag IT", Year = 2025, LocalCurrencyValue = 7669840m },
            new FinanceReference { Key = "JP", Label = "Trafag JP", Year = 2025, LocalCurrencyValue = 187739814m },
            new FinanceReference { Key = "MS", Label = "Trafag MS", Year = 2025, LocalCurrencyValue = 1850199m },
            new FinanceReference { Key = "MSA", Label = "Trafag MSA", Year = 2025, LocalCurrencyValue = 1445258m },
            new FinanceReference { Key = "PL", Label = "Trafag PL Poltraf", Year = 2025, LocalCurrencyValue = 11279297m },
            new FinanceReference { Key = "RU", Label = "Trafag RU", Year = 2025 },
            new FinanceReference { Key = "UK", Label = "Trafag UK", Year = 2025, LocalCurrencyValue = 3538972m },
            new FinanceReference { Key = "US", Label = "Trafag US", Year = 2025, LocalCurrencyValue = 3896728m, CheckValue = 3749865m }
        };

        var existing = db.FinanceReferences.ToList();
        var changed = false;
        foreach (var item in defaults)
        {
            var current = existing.FirstOrDefault(x => x.Year == item.Year && x.Key == item.Key);
            if (current is not null)
            {
                if (current.Key == "UK" && current.Year == 2025)
                {
                    if (current.LocalCurrencyValue != 3538972m)
                    {
                        current.LocalCurrencyValue = 3538972m;
                        changed = true;
                    }

                    if (current.CheckValue.HasValue)
                    {
                        current.CheckValue = null;
                        changed = true;
                    }
                }

                if (current.Key == "ES" && current.Year == 2025 && current.LocalCurrencyValue != 3082320.18m)
                {
                    current.LocalCurrencyValue = 3082320.18m;
                    current.CheckValue = null;
                    current.Notes = "Sitzung 2026-06-01: ES-Ist 3'082'320.18 EUR fachlich bestaetigt; alter Sollwert 3'102'333.61 war Referenz-/Excel-Fehler.";
                    changed = true;
                }

                if (current.Key == "DE" && current.Year == 2025 && current.LocalCurrencyValue != 3652394.46m)
                {
                    current.LocalCurrencyValue = 3652394.46m;
                    changed = true;
                }

                continue;
            }

            db.FinanceReferences.Add(item);
            changed = true;
        }

        if (changed)
            db.SaveChanges();
    }

    private static void EnsureBudgetExchangeRateDefaults(AppDbContext db)
    {
        var defaults = new (int Year, string From, string To, decimal Rate)[]
        {
            (2025, "CHF", "CHF", 1m),
            (2025, "USD", "CHF", 0.85m),
            (2025, "EUR", "CHF", 0.95m),
            (2025, "GBP", "CHF", 1.13m),
            (2025, "CNY", "CHF", 1m / 8.50m),
            (2025, "INR", "CHF", 1m / 90.91m),
            (2025, "CZK", "CHF", 1m / 25.64m),
            (2025, "PLN", "CHF", 0.22m),
            (2025, "JPY", "CHF", 1m / 156.25m),
            (2026, "CHF", "CHF", 1m),
            (2026, "USD", "CHF", 0.80m),
            (2026, "EUR", "CHF", 0.94m),
            (2026, "GBP", "CHF", 1.09m),
            (2026, "CNY", "CHF", 1m / 8.50m),
            (2026, "INR", "CHF", 1m / 110m),
            (2026, "CZK", "CHF", 1m / 26m),
            (2026, "PLN", "CHF", 0.22m),
            (2026, "JPY", "CHF", 1m / 175m)
        };

        var changed = false;
        foreach (var item in defaults)
        {
            var validFrom = new DateTime(item.Year, 1, 1);
            var notes = $"Budget {item.Year}";
            var exists = db.CurrencyExchangeRates.Any(x =>
                x.FromCurrency == item.From &&
                x.ToCurrency == item.To &&
                x.ValidFrom == validFrom &&
                x.Notes == notes);
            if (exists)
                continue;

            db.CurrencyExchangeRates.Add(new CurrencyExchangeRate
            {
                FromCurrency = item.From,
                ToCurrency = item.To,
                Rate = item.Rate,
                ValidFrom = validFrom,
                ValidTo = new DateTime(item.Year, 12, 31),
                Notes = notes,
                IsActive = true
            });
            changed = true;
        }

        if (changed)
            db.SaveChanges();
    }

    private static void EnsureFinanceIntercompanyRuleDefaults(AppDbContext db)
    {
        var defaults = new[]
        {
            new FinanceIntercompanyRule { CustomerNameContains = "TRAFAG", Notes = "Default IC name marker" },
            new FinanceIntercompanyRule { CustomerNameContains = "MAGNETIC SENSE", Notes = "Default IC name marker" },
            new FinanceIntercompanyRule { CustomerNameContains = "MAGNETS SENSE", Notes = "Default IC name marker" },
            new FinanceIntercompanyRule { CustomerNameContains = "GESELLSCHAFT FUER SENSORIK", Notes = "Default IC name marker" },
            new FinanceIntercompanyRule { CustomerNameContains = "GESELLSCHAFT FUR SENSORIK", Notes = "Default IC name marker" },
            new FinanceIntercompanyRule { ScopeKey = "IT", CustomerNumber = "C_IT01_0306794", Notes = "IT IC customer number" },
            new FinanceIntercompanyRule { ScopeKey = "IT", CustomerNumber = "C_CH01_0302179", Notes = "IT IC customer number" }
        };

        var changed = false;
        foreach (var item in defaults)
        {
            var exists = db.FinanceIntercompanyRules.Any(x =>
                x.ScopeKey == item.ScopeKey &&
                x.CustomerNumber == item.CustomerNumber &&
                x.CustomerNameContains == item.CustomerNameContains);
            if (exists)
                continue;

            db.FinanceIntercompanyRules.Add(item);
            changed = true;
        }

        if (changed)
            db.SaveChanges();
    }

    private static void EnsureFinanceRuleDefaults(AppDbContext db)
    {
        if (!CanUseTable(db, "FinanceRules"))
            return;

        var changed = false;
        foreach (var item in FinanceRuleEngine.CreateDefaultRules())
        {
            var exists = db.FinanceRules.Any(rule =>
                rule.ScopeKey == item.ScopeKey &&
                rule.RuleType == item.RuleType &&
                rule.FieldName == item.FieldName &&
                rule.MatchType == item.MatchType &&
                rule.MatchValue == item.MatchValue);

            if (exists)
                continue;

            db.FinanceRules.Add(item);
            changed = true;
        }

        // DE finance year now follows the invoice date (Fakturierungsdatum). Deactivate any
        // previously seeded DE ForceYear rule so existing databases stop pinning DE to 2025.
        // Kept (not deleted) so the change stays visible and reversible in the admin UI.
        var deForceYearRules = db.FinanceRules
            .Where(rule => rule.ScopeKey == "DE"
                && rule.RuleType == FinanceRuleTypes.ForceYear
                && rule.IsActive)
            .ToList();
        foreach (var rule in deForceYearRules)
        {
            rule.IsActive = false;
            changed = true;
        }

        if (changed)
            db.SaveChanges();
    }

    private static bool CanUseTable(AppDbContext db, string tableName)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        return DatabaseSchemaTools.GetTableColumns(conn, transaction: null, tableName).Count > 0;
    }
}
