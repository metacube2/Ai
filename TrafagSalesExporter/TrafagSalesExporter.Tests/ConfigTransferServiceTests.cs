using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ConfigTransferServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;
    private readonly ConfigTransferService _service;

    public ConfigTransferServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.Database.EnsureCreated();
        }

        _dbFactory = new TestDbContextFactory(options);
        _service = new ConfigTransferService(_dbFactory);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task ExportJsonAsync_Excludes_Secrets_When_Requested()
    {
        await SeedExportConfigurationAsync();

        var json = await _service.ExportJsonAsync(includeSecrets: false);
        var package = JsonSerializer.Deserialize<ConfigTransferPackage>(json)
            ?? throw new InvalidOperationException("Package missing.");

        Assert.False(package.IncludesSecrets);
        Assert.NotNull(package.ExportSettings);
        Assert.NotNull(package.SharePointConfig);
        Assert.Null(package.SharePointConfig.ClientSecret);
        Assert.NotEmpty(package.SourceSystemDefinitions);
        Assert.All(package.SourceSystemDefinitions, system =>
        {
            Assert.Null(system.CentralUsername);
            Assert.Null(system.CentralPassword);
        });

        Assert.Single(package.HanaServers);

        var site = Assert.Single(package.Sites);
        Assert.Null(site.UsernameOverride);
        Assert.Null(site.PasswordOverride);
        Assert.Equal("C:\\imports\\manual.xlsx", site.ManualImportFilePath);

        var rule = Assert.Single(package.FieldTransformationRules);
        Assert.Equal("Record", rule.RuleScope);
        Assert.Equal("FirstNonEmpty", rule.TransformationType);
    }

    [Fact]
    public async Task ImportJsonAsync_Preserves_Existing_Secrets_When_Import_Excludes_Secrets()
    {
        await SeedExistingSecretsAsync();

        var package = new ConfigTransferPackage
        {
            IncludesSecrets = false,
            SharePointConfig = new ConfigTransferSharePoint
            {
                SiteUrl = "https://new.sharepoint.local",
                ExportFolder = "/new",
                TenantId = "new-tenant",
                ClientId = "new-client",
                ClientSecret = null
            },
            ExportSettings = new ConfigTransferExportSettings
            {
                DateFilter = "2026-01-01",
                TimerHour = 5,
                TimerMinute = 30,
                TimerEnabled = false,
                DebugLoggingEnabled = true,
                LocalSiteExportFolder = "D:\\site",
                LocalConsolidatedExportFolder = "D:\\consolidated"
            },
            SourceSystemDefinitions =
            [
                new ConfigTransferSourceSystemDefinition
                {
                    Code = "SAP",
                    DisplayName = "SAP",
                    ConnectionKind = SourceSystemConnectionKinds.SapGateway,
                    IsActive = true,
                    CentralUsername = null,
                    CentralPassword = null
                },
                new ConfigTransferSourceSystemDefinition
                {
                    Code = "BI1",
                    DisplayName = "BI1",
                    ConnectionKind = SourceSystemConnectionKinds.Hana,
                    IsActive = true,
                    CentralUsername = null,
                    CentralPassword = null
                },
                new ConfigTransferSourceSystemDefinition
                {
                    Code = "SAGE",
                    DisplayName = "SAGE",
                    ConnectionKind = SourceSystemConnectionKinds.Hana,
                    IsActive = true,
                    CentralUsername = null,
                    CentralPassword = null
                },
                new ConfigTransferSourceSystemDefinition
                {
                    Code = "MANUAL_EXCEL",
                    DisplayName = "Manual Excel",
                    ConnectionKind = SourceSystemConnectionKinds.ManualExcel,
                    IsActive = true,
                    CentralUsername = null,
                    CentralPassword = null
                }
            ],
            HanaServers =
            [
                new ConfigTransferHanaServer
                {
                    Key = "server-1",
                    Name = "Server A",
                    Host = "hana-a",
                    Port = 30015,
                    DatabaseName = "DB1",
                    UseSsl = true,
                    ValidateCertificate = false,
                    AdditionalParams = "x=y"
                }
            ],
            Sites =
            [
                new ConfigTransferSite
                {
                    Key = "site-1",
                    HanaServerKey = "server-1",
                    Schema = "schema_a",
                    TSC = "TRCH",
                    Land = "Schweiz",
                    SourceSystem = "MANUAL_EXCEL",
                    UsernameOverride = null,
                    PasswordOverride = null,
                    ManualImportFilePath = "D:\\manual\\trch.xlsx",
                    ManualImportLastUploadedAtUtc = new DateTime(2026, 4, 16, 8, 0, 0, DateTimeKind.Utc),
                    IsActive = true
                }
            ],
            FieldTransformationRules =
            [
                new FieldTransformationRule
                {
                    SourceSystem = "MANUAL_EXCEL",
                    SourceField = "",
                    TargetField = nameof(SalesRecord.CustomerName),
                    TransformationType = "FirstNonEmpty",
                    RuleScope = "Record",
                    Argument = "CustomerName|SupplierName|Name",
                    SortOrder = 10,
                    IsActive = true
                }
            ]
        };

        await _service.ImportJsonAsync(JsonSerializer.Serialize(package));

        await using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.SingleAsync();
        var sharePoint = await db.SharePointConfigs.SingleAsync();
        var systems = await db.SourceSystemDefinitions.OrderBy(x => x.Code).ToListAsync();
        var server = await db.HanaServers.SingleAsync();
        var site = await db.Sites.SingleAsync();
        var rule = await db.FieldTransformationRules.SingleAsync();

        Assert.Equal("2026-01-01", settings.DateFilter);
        Assert.Equal(5, settings.TimerHour);
        Assert.Equal(30, settings.TimerMinute);
        Assert.False(settings.TimerEnabled);
        Assert.True(settings.DebugLoggingEnabled);
        Assert.Equal("D:\\site", settings.LocalSiteExportFolder);
        Assert.Equal("D:\\consolidated", settings.LocalConsolidatedExportFolder);

        Assert.Equal("preserved-sharepoint-secret", sharePoint.ClientSecret);
        Assert.Equal("new-tenant", sharePoint.TenantId);

        var sapSystem = Assert.Single(systems, x => x.Code == "SAP");
        Assert.Equal("preserved-sap-user", sapSystem.CentralUsername);
        Assert.Equal("preserved-sap-password", sapSystem.CentralPassword);
        var bi1System = Assert.Single(systems, x => x.Code == "BI1");
        Assert.Equal("preserved-bi1-user", bi1System.CentralUsername);
        Assert.Equal("preserved-bi1-password", bi1System.CentralPassword);
        var sageSystem = Assert.Single(systems, x => x.Code == "SAGE");
        Assert.Equal("preserved-sage-user", sageSystem.CentralUsername);
        Assert.Equal("preserved-sage-password", sageSystem.CentralPassword);

        Assert.Equal(string.Empty, server.Username);
        Assert.Equal(string.Empty, server.Password);
        Assert.True(server.UseSsl);

        Assert.Equal("preserved-site-user", site.UsernameOverride);
        Assert.Equal("preserved-site-password", site.PasswordOverride);
        Assert.Equal("D:\\manual\\trch.xlsx", site.ManualImportFilePath);
        Assert.Equal("MANUAL_EXCEL", site.SourceSystem);

        Assert.Equal("Record", rule.RuleScope);
        Assert.Equal("FirstNonEmpty", rule.TransformationType);
    }

    [Fact]
    public async Task ImportJsonAsync_Preserves_CentralSalesRecords()
    {
        await SeedExistingSecretsAsync();

        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.CentralSalesRecords.Add(new CentralSalesRecord
            {
                StoredAtUtc = new DateTime(2026, 4, 17, 8, 0, 0, DateTimeKind.Utc),
                SiteId = 1,
                SourceSystem = "MANUAL_EXCEL",
                ExtractionDate = new DateTime(2026, 4, 17),
                Tsc = "TRCH",
                InvoiceNumber = "INV-1",
                PositionOnInvoice = 1,
                Material = "MAT-1",
                Name = "Material 1",
                ProductGroup = "PG",
                Quantity = 1m,
                SupplierNumber = "SUP-1",
                SupplierName = "Supplier 1",
                SupplierCountry = "CH",
                CustomerNumber = "CUS-1",
                CustomerName = "Customer 1",
                CustomerCountry = "CH",
                CustomerIndustry = "Industry",
                StandardCost = 10m,
                StandardCostCurrency = "CHF",
                PurchaseOrderNumber = "PO-1",
                SalesPriceValue = 20m,
                SalesCurrency = "CHF",
                Incoterms2020 = "EXW",
                SalesResponsibleEmployee = "Owner",
                InvoiceDate = new DateTime(2026, 4, 17),
                OrderDate = new DateTime(2026, 4, 16),
                Land = "Schweiz",
                DocumentType = "Invoice"
            });
            await db.SaveChangesAsync();
        }

        var package = new ConfigTransferPackage
        {
            IncludesSecrets = false,
            SourceSystemDefinitions = BuildStandardSourceSystems(),
            Sites =
            [
                new ConfigTransferSite
                {
                    Key = "site-1",
                    Schema = "schema_a",
                    TSC = "TRCH",
                    Land = "Schweiz",
                    SourceSystem = "MANUAL_EXCEL",
                    IsActive = true
                }
            ]
        };

        await _service.ImportJsonAsync(JsonSerializer.Serialize(package));

        await using var verifyDb = await _dbFactory.CreateDbContextAsync();
        Assert.Single(verifyDb.CentralSalesRecords);
    }

    [Fact]
    public async Task ImportJsonAsync_Uses_Legacy_ConnectionKind_Fallbacks()
    {
        var packageJson = """
{
  "includesSecrets": false,
  "sourceSystemDefinitions": [
    { "code": "SAP", "displayName": "SAP", "isActive": true },
    { "code": "BI1", "displayName": "BI1", "isActive": true },
    { "code": "MANUAL_EXCEL", "displayName": "Manual Excel", "isActive": true }
  ]
}
""";

        await _service.ImportJsonAsync(packageJson);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var systems = await db.SourceSystemDefinitions.OrderBy(x => x.Code).ToListAsync();

        Assert.Equal(SourceSystemConnectionKinds.Hana, Assert.Single(systems, x => x.Code == "BI1").ConnectionKind);
        Assert.Equal(SourceSystemConnectionKinds.ManualExcel, Assert.Single(systems, x => x.Code == "MANUAL_EXCEL").ConnectionKind);
        Assert.Equal(SourceSystemConnectionKinds.SapGateway, Assert.Single(systems, x => x.Code == "SAP").ConnectionKind);
    }

    private async Task SeedExportConfigurationAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.SharePointConfigs.Add(new SharePointConfig
        {
            SiteUrl = "https://sharepoint.local",
            ExportFolder = "/exports",
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret"
        });
        db.ExportSettings.Add(new ExportSettings());
        db.SourceSystemDefinitions.AddRange(
            new SourceSystemDefinition
            {
                Code = "SAP",
                DisplayName = "SAP",
                ConnectionKind = SourceSystemConnectionKinds.SapGateway,
                IsActive = true,
                CentralUsername = "sap-user",
                CentralPassword = "sap-password"
            },
            new SourceSystemDefinition
            {
                Code = "BI1",
                DisplayName = "BI1",
                ConnectionKind = SourceSystemConnectionKinds.Hana,
                IsActive = true,
                CentralUsername = "bi1-user",
                CentralPassword = "bi1-password"
            },
            new SourceSystemDefinition
            {
                Code = "SAGE",
                DisplayName = "SAGE",
                ConnectionKind = SourceSystemConnectionKinds.Hana,
                IsActive = true,
                CentralUsername = "sage-user",
                CentralPassword = "sage-password"
            },
            new SourceSystemDefinition
            {
                Code = "MANUAL_EXCEL",
                DisplayName = "Manual Excel",
                ConnectionKind = SourceSystemConnectionKinds.ManualExcel,
                IsActive = true
            });
        db.HanaServers.Add(new HanaServer
        {
            Id = 1,
            Name = "Server A",
            Host = "hana-a",
            Port = 30015,
            Username = string.Empty,
            Password = string.Empty,
            DatabaseName = "DB1"
        });
        db.Sites.Add(new Site
        {
            Id = 1,
            HanaServerId = 1,
            Schema = "schema_a",
            TSC = "TRCH",
            Land = "Schweiz",
            SourceSystem = "MANUAL_EXCEL",
            UsernameOverride = "site-user",
            PasswordOverride = "site-password",
            ManualImportFilePath = "C:\\imports\\manual.xlsx",
            ManualImportLastUploadedAtUtc = new DateTime(2026, 4, 16, 7, 0, 0, DateTimeKind.Utc),
            IsActive = true
        });
        db.FieldTransformationRules.Add(new FieldTransformationRule
        {
            SourceSystem = "MANUAL_EXCEL",
            SourceField = "",
            TargetField = nameof(SalesRecord.CustomerName),
            TransformationType = "FirstNonEmpty",
            RuleScope = "Record",
            Argument = "CustomerName|SupplierName|Name",
            SortOrder = 10,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedExistingSecretsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.SharePointConfigs.Add(new SharePointConfig
        {
            SiteUrl = "https://old.sharepoint.local",
            ExportFolder = "/old",
            TenantId = "old-tenant",
            ClientId = "old-client",
            ClientSecret = "preserved-sharepoint-secret"
        });
        db.ExportSettings.Add(new ExportSettings());
        db.SourceSystemDefinitions.AddRange(
            new SourceSystemDefinition
            {
                Code = "SAP",
                DisplayName = "SAP",
                ConnectionKind = SourceSystemConnectionKinds.SapGateway,
                IsActive = true,
                CentralUsername = "preserved-sap-user",
                CentralPassword = "preserved-sap-password"
            },
            new SourceSystemDefinition
            {
                Code = "BI1",
                DisplayName = "BI1",
                ConnectionKind = SourceSystemConnectionKinds.Hana,
                IsActive = true,
                CentralUsername = "preserved-bi1-user",
                CentralPassword = "preserved-bi1-password"
            },
            new SourceSystemDefinition
            {
                Code = "SAGE",
                DisplayName = "SAGE",
                ConnectionKind = SourceSystemConnectionKinds.Hana,
                IsActive = true,
                CentralUsername = "preserved-sage-user",
                CentralPassword = "preserved-sage-password"
            },
            new SourceSystemDefinition
            {
                Code = "MANUAL_EXCEL",
                DisplayName = "Manual Excel",
                ConnectionKind = SourceSystemConnectionKinds.ManualExcel,
                IsActive = true
            });
        db.HanaServers.Add(new HanaServer
        {
            Id = 1,
            Name = "Server A",
            Host = "hana-a",
            Port = 30015,
            Username = "preserved-server-user",
            Password = "preserved-server-password",
            DatabaseName = "DB1"
        });
        db.Sites.Add(new Site
        {
            Id = 1,
            HanaServerId = 1,
            Schema = "schema_a",
            TSC = "TRCH",
            Land = "Schweiz",
            SourceSystem = "MANUAL_EXCEL",
            UsernameOverride = "preserved-site-user",
            PasswordOverride = "preserved-site-password",
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static List<ConfigTransferSourceSystemDefinition> BuildStandardSourceSystems()
    {
        return
        [
            new ConfigTransferSourceSystemDefinition
            {
                Code = "SAP",
                DisplayName = "SAP",
                ConnectionKind = SourceSystemConnectionKinds.SapGateway,
                IsActive = true
            },
            new ConfigTransferSourceSystemDefinition
            {
                Code = "BI1",
                DisplayName = "BI1",
                ConnectionKind = SourceSystemConnectionKinds.Hana,
                IsActive = true
            },
            new ConfigTransferSourceSystemDefinition
            {
                Code = "SAGE",
                DisplayName = "SAGE",
                ConnectionKind = SourceSystemConnectionKinds.Hana,
                IsActive = true
            },
            new ConfigTransferSourceSystemDefinition
            {
                Code = "MANUAL_EXCEL",
                DisplayName = "Manual Excel",
                ConnectionKind = SourceSystemConnectionKinds.ManualExcel,
                IsActive = true
            }
        ];
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }
}
