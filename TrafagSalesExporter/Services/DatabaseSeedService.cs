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
        EnsureGermanyManualExcelSite(db);
        EnsureUkManualExcelFolder(db);
        EnsureSapODataDachSite(db);
        EnsureFinanceReferenceDefaults(db);
        EnsureBudgetExchangeRateDefaults(db);
        EnsureFinanceIntercompanyRuleDefaults(db);
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
            .Where(x => x.SiteId == siteId && x.Alias != "Z")
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

    private static void EnsureFinanceReferenceDefaults(AppDbContext db)
    {
        var defaults = new[]
        {
            new FinanceReference { Key = "AT", Label = "Trafag AT", Year = 2025, LocalCurrencyValue = 3443863m },
            new FinanceReference { Key = "CH", Label = "Trafag CH", Year = 2025 },
            new FinanceReference { Key = "CN", Label = "Trafag CN", Year = 2025 },
            new FinanceReference { Key = "CZ", Label = "Trafag CZ", Year = 2025, LocalCurrencyValue = 95458782m },
            new FinanceReference { Key = "DE", Label = "Trafag DE", Year = 2025, LocalCurrencyValue = 3635923m },
            new FinanceReference { Key = "ES", Label = "Trafag ES", Year = 2025, LocalCurrencyValue = 3102333.61m },
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

                if (current.Key == "ES" && current.Year == 2025 && current.LocalCurrencyValue != 3102333.61m)
                {
                    current.LocalCurrencyValue = 3102333.61m;
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
        var defaults = new (string From, string To, decimal Rate)[]
        {
            ("CHF", "CHF", 1m),
            ("USD", "CHF", 0.85m),
            ("EUR", "CHF", 0.95m),
            ("GBP", "CHF", 1.13m),
            ("CNY", "CHF", 1m / 8.50m),
            ("INR", "CHF", 1m / 90.91m),
            ("CZK", "CHF", 1m / 25.64m),
            ("PLN", "CHF", 0.22m),
            ("JPY", "CHF", 1m / 156.25m)
        };

        var changed = false;
        foreach (var item in defaults)
        {
            var exists = db.CurrencyExchangeRates.Any(x =>
                x.FromCurrency == item.From &&
                x.ToCurrency == item.To &&
                x.ValidFrom == new DateTime(2025, 1, 1) &&
                x.Notes == "Budget 2025");
            if (exists)
                continue;

            db.CurrencyExchangeRates.Add(new CurrencyExchangeRate
            {
                FromCurrency = item.From,
                ToCurrency = item.To,
                Rate = item.Rate,
                ValidFrom = new DateTime(2025, 1, 1),
                ValidTo = new DateTime(2025, 12, 31),
                Notes = "Budget 2025",
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
}
