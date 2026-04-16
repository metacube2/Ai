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
        Assert.Null(package.ExportSettings.SapUsername);
        Assert.Null(package.ExportSettings.SapPassword);
        Assert.NotNull(package.SharePointConfig);
        Assert.Null(package.SharePointConfig.ClientSecret);

        var server = Assert.Single(package.HanaServers);
        Assert.Null(server.Username);
        Assert.Null(server.Password);

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
                LocalConsolidatedExportFolder = "D:\\consolidated",
                SapUsername = null,
                SapPassword = null,
                Bi1Username = null,
                Bi1Password = null,
                SageUsername = null,
                SagePassword = null
            },
            HanaServers =
            [
                new ConfigTransferHanaServer
                {
                    Key = "server-1",
                    Name = "Server A",
                    Host = "hana-a",
                    Port = 30015,
                    Username = null,
                    Password = null,
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
        var server = await db.HanaServers.SingleAsync();
        var site = await db.Sites.SingleAsync();
        var rule = await db.FieldTransformationRules.SingleAsync();

        Assert.Equal("preserved-sap-user", settings.SapUsername);
        Assert.Equal("preserved-sap-password", settings.SapPassword);
        Assert.Equal("preserved-bi1-user", settings.Bi1Username);
        Assert.Equal("preserved-sage-password", settings.SagePassword);

        Assert.Equal("preserved-sharepoint-secret", sharePoint.ClientSecret);
        Assert.Equal("new-tenant", sharePoint.TenantId);

        Assert.Equal("preserved-server-user", server.Username);
        Assert.Equal("preserved-server-password", server.Password);
        Assert.True(server.UseSsl);

        Assert.Equal("preserved-site-user", site.UsernameOverride);
        Assert.Equal("preserved-site-password", site.PasswordOverride);
        Assert.Equal("D:\\manual\\trch.xlsx", site.ManualImportFilePath);
        Assert.Equal("MANUAL_EXCEL", site.SourceSystem);

        Assert.Equal("Record", rule.RuleScope);
        Assert.Equal("FirstNonEmpty", rule.TransformationType);
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
        db.ExportSettings.Add(new ExportSettings
        {
            SapUsername = "sap-user",
            SapPassword = "sap-password",
            Bi1Username = "bi1-user",
            Bi1Password = "bi1-password",
            SageUsername = "sage-user",
            SagePassword = "sage-password"
        });
        db.HanaServers.Add(new HanaServer
        {
            Id = 1,
            Name = "Server A",
            Host = "hana-a",
            Port = 30015,
            Username = "server-user",
            Password = "server-password",
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
        db.ExportSettings.Add(new ExportSettings
        {
            SapUsername = "preserved-sap-user",
            SapPassword = "preserved-sap-password",
            Bi1Username = "preserved-bi1-user",
            Bi1Password = "preserved-bi1-password",
            SageUsername = "preserved-sage-user",
            SagePassword = "preserved-sage-password"
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
