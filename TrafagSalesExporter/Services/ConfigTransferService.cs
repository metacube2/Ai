using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ConfigTransferService : IConfigTransferService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ConfigTransferService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string> ExportJsonAsync(bool includeSecrets)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var sharePoint = await db.SharePointConfigs.FirstOrDefaultAsync();
        var exportSettings = await db.ExportSettings.FirstOrDefaultAsync();
        var sourceSystems = await db.SourceSystemDefinitions.OrderBy(x => x.Code).ToListAsync();
        var exchangeRates = await db.CurrencyExchangeRates
            .OrderBy(x => x.FromCurrency)
            .ThenBy(x => x.ToCurrency)
            .ThenByDescending(x => x.ValidFrom)
            .ToListAsync();
        var hanaServers = await db.HanaServers.OrderBy(x => x.Name).ToListAsync();
        var sites = await db.Sites.OrderBy(x => x.Land).ToListAsync();
        var rules = await db.FieldTransformationRules.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        var sapSources = await db.SapSourceDefinitions.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        var sapJoins = await db.SapJoinDefinitions.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        var sapMappings = await db.SapFieldMappings.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();

        var serverKeyMap = hanaServers.ToDictionary(x => x.Id, _ => Guid.NewGuid().ToString("N"));
        var siteKeyMap = sites.ToDictionary(x => x.Id, _ => Guid.NewGuid().ToString("N"));

        var package = new ConfigTransferPackage
        {
            IncludesSecrets = includeSecrets,
            SharePointConfig = sharePoint is null ? null : new ConfigTransferSharePoint
            {
                SiteUrl = sharePoint.SiteUrl,
                ExportFolder = sharePoint.ExportFolder,
                CentralExportFolder = sharePoint.CentralExportFolder,
                TenantId = sharePoint.TenantId,
                ClientId = sharePoint.ClientId,
                ClientSecret = includeSecrets ? sharePoint.ClientSecret : null
            },
            ExportSettings = exportSettings is null ? null : new ConfigTransferExportSettings
            {
                DateFilter = exportSettings.DateFilter,
                TimerHour = exportSettings.TimerHour,
                TimerMinute = exportSettings.TimerMinute,
                TimerEnabled = exportSettings.TimerEnabled,
                DebugLoggingEnabled = exportSettings.DebugLoggingEnabled,
                LocalSiteExportFolder = exportSettings.LocalSiteExportFolder,
                LocalConsolidatedExportFolder = exportSettings.LocalConsolidatedExportFolder
            },
            SourceSystemDefinitions = sourceSystems.Select(system => new ConfigTransferSourceSystemDefinition
            {
                Code = system.Code,
                DisplayName = system.DisplayName,
                ConnectionKind = system.ConnectionKind,
                IsActive = system.IsActive,
                CentralServiceUrl = system.CentralServiceUrl,
                CentralUsername = includeSecrets ? system.CentralUsername : null,
                CentralPassword = includeSecrets ? system.CentralPassword : null
            }).ToList(),
            CurrencyExchangeRates = exchangeRates.Select(rate => new ConfigTransferCurrencyExchangeRate
            {
                FromCurrency = rate.FromCurrency,
                ToCurrency = rate.ToCurrency,
                Rate = rate.Rate,
                ValidFrom = rate.ValidFrom,
                ValidTo = rate.ValidTo,
                Notes = rate.Notes,
                IsActive = rate.IsActive
            }).ToList(),
            HanaServers = hanaServers.Select(server => new ConfigTransferHanaServer
            {
                Key = serverKeyMap[server.Id],
                SourceSystem = server.SourceSystem,
                Name = server.Name,
                Host = server.Host,
                Port = server.Port,
                DatabaseName = server.DatabaseName,
                UseSsl = server.UseSsl,
                ValidateCertificate = server.ValidateCertificate,
                AdditionalParams = server.AdditionalParams
            }).ToList(),
            Sites = sites.Select(site => new ConfigTransferSite
            {
                Key = siteKeyMap[site.Id],
                HanaServerKey = site.HanaServerId.HasValue && serverKeyMap.TryGetValue(site.HanaServerId.Value, out var serverKey) ? serverKey : null,
                Schema = site.Schema,
                TSC = site.TSC,
                Land = site.Land,
                SourceSystem = site.SourceSystem,
                UsernameOverride = includeSecrets ? site.UsernameOverride : null,
                PasswordOverride = includeSecrets ? site.PasswordOverride : null,
                LocalExportFolderOverride = site.LocalExportFolderOverride,
                ManualImportFilePath = site.ManualImportFilePath,
                ManualImportLastUploadedAtUtc = site.ManualImportLastUploadedAtUtc,
                SapServiceUrl = site.SapServiceUrl,
                SapEntitySet = site.SapEntitySet,
                SapEntitySetsCache = site.SapEntitySetsCache,
                SapEntitySetsRefreshedAtUtc = site.SapEntitySetsRefreshedAtUtc,
                IsActive = site.IsActive
            }).ToList(),
            FieldTransformationRules = rules.Select(r => new FieldTransformationRule
            {
                SourceSystem = r.SourceSystem,
                SourceField = r.SourceField,
                TargetField = r.TargetField,
                TransformationType = r.TransformationType,
                RuleScope = r.RuleScope,
                Argument = r.Argument,
                SortOrder = r.SortOrder,
                IsActive = r.IsActive
            }).ToList(),
            SapSourceDefinitions = sapSources.Select(s => new ConfigTransferSapSourceDefinition
            {
                SiteKey = siteKeyMap[s.SiteId],
                Alias = s.Alias,
                EntitySet = s.EntitySet,
                IsPrimary = s.IsPrimary,
                IsActive = s.IsActive,
                SortOrder = s.SortOrder
            }).ToList(),
            SapJoinDefinitions = sapJoins.Select(j => new ConfigTransferSapJoinDefinition
            {
                SiteKey = siteKeyMap[j.SiteId],
                LeftAlias = j.LeftAlias,
                RightAlias = j.RightAlias,
                LeftKeys = j.LeftKeys,
                RightKeys = j.RightKeys,
                JoinType = j.JoinType,
                IsActive = j.IsActive,
                SortOrder = j.SortOrder
            }).ToList(),
            SapFieldMappings = sapMappings.Select(m => new ConfigTransferSapFieldMapping
            {
                SiteKey = siteKeyMap[m.SiteId],
                TargetField = m.TargetField,
                SourceExpression = m.SourceExpression,
                IsRequired = m.IsRequired,
                IsActive = m.IsActive,
                SortOrder = m.SortOrder
            }).ToList()
        };

        return JsonSerializer.Serialize(package, JsonOptions);
    }

    public async Task ImportJsonAsync(string json)
    {
        var package = JsonSerializer.Deserialize<ConfigTransferPackage>(json, JsonOptions)
            ?? throw new InvalidOperationException("Konfigurationsdatei konnte nicht gelesen werden.");

        using var db = await _dbFactory.CreateDbContextAsync();
        var existingSharePoint = await db.SharePointConfigs.FirstOrDefaultAsync();
        var existingSettings = await db.ExportSettings.FirstOrDefaultAsync();
        var existingSourceSystems = await db.SourceSystemDefinitions.ToListAsync();
        var existingServers = await db.HanaServers.ToListAsync();
        var existingExchangeRates = await db.CurrencyExchangeRates.ToListAsync();
        var existingSites = await db.Sites.ToListAsync();
        var existingRules = await db.FieldTransformationRules.ToListAsync();
        var existingSapSources = await db.SapSourceDefinitions.ToListAsync();
        var existingSapJoins = await db.SapJoinDefinitions.ToListAsync();
        var existingSapMappings = await db.SapFieldMappings.ToListAsync();
        var existingCentralRecords = await db.CentralSalesRecords.ToListAsync();

        var preservedSharePointSecret = existingSharePoint?.ClientSecret ?? string.Empty;
        var preservedSourceSystemSecrets = existingSourceSystems.ToDictionary(
            x => x.Code,
            x => (CentralUsername: x.CentralUsername, CentralPassword: x.CentralPassword),
            StringComparer.OrdinalIgnoreCase);
        var preservedSiteSecrets = existingSites.ToDictionary(
            x => BuildSiteSignature(x.Land, x.TSC, x.Schema, x.SourceSystem),
            x => (x.UsernameOverride, x.PasswordOverride));

        if (existingSapMappings.Count > 0) db.SapFieldMappings.RemoveRange(existingSapMappings);
        if (existingSapJoins.Count > 0) db.SapJoinDefinitions.RemoveRange(existingSapJoins);
        if (existingSapSources.Count > 0) db.SapSourceDefinitions.RemoveRange(existingSapSources);
        if (existingRules.Count > 0) db.FieldTransformationRules.RemoveRange(existingRules);
        if (existingExchangeRates.Count > 0) db.CurrencyExchangeRates.RemoveRange(existingExchangeRates);
        if (existingCentralRecords.Count > 0) db.CentralSalesRecords.RemoveRange(existingCentralRecords);
        if (existingSites.Count > 0) db.Sites.RemoveRange(existingSites);
        if (existingServers.Count > 0) db.HanaServers.RemoveRange(existingServers);
        if (existingSourceSystems.Count > 0) db.SourceSystemDefinitions.RemoveRange(existingSourceSystems);
        if (existingSharePoint is not null) db.SharePointConfigs.Remove(existingSharePoint);
        if (existingSettings is not null) db.ExportSettings.Remove(existingSettings);
        await db.SaveChangesAsync();

        var newSharePoint = package.SharePointConfig is null ? new SharePointConfig() : new SharePointConfig
        {
            SiteUrl = package.SharePointConfig.SiteUrl,
            ExportFolder = package.SharePointConfig.ExportFolder,
            CentralExportFolder = package.SharePointConfig.CentralExportFolder,
            TenantId = package.SharePointConfig.TenantId,
            ClientId = package.SharePointConfig.ClientId,
            ClientSecret = package.IncludesSecrets ? package.SharePointConfig.ClientSecret ?? string.Empty : preservedSharePointSecret
        };
        db.SharePointConfigs.Add(newSharePoint);

        var importedSettings = package.ExportSettings ?? new ConfigTransferExportSettings();
        db.ExportSettings.Add(new ExportSettings
        {
            DateFilter = importedSettings.DateFilter,
            TimerHour = importedSettings.TimerHour,
            TimerMinute = importedSettings.TimerMinute,
            TimerEnabled = importedSettings.TimerEnabled,
            DebugLoggingEnabled = importedSettings.DebugLoggingEnabled,
            LocalSiteExportFolder = importedSettings.LocalSiteExportFolder,
            LocalConsolidatedExportFolder = importedSettings.LocalConsolidatedExportFolder
        });

        var importedSourceSystems = package.SourceSystemDefinitions.Count > 0
            ? package.SourceSystemDefinitions
            : BuildDefaultSourceSystems();

        foreach (var sourceSystem in importedSourceSystems)
        {
            preservedSourceSystemSecrets.TryGetValue(sourceSystem.Code, out var preserved);
            db.SourceSystemDefinitions.Add(new SourceSystemDefinition
            {
                Code = sourceSystem.Code,
                DisplayName = sourceSystem.DisplayName,
                ConnectionKind = sourceSystem.ConnectionKind,
                IsActive = sourceSystem.IsActive,
                CentralServiceUrl = sourceSystem.CentralServiceUrl,
                CentralUsername = package.IncludesSecrets ? sourceSystem.CentralUsername ?? string.Empty : preserved.CentralUsername ?? string.Empty,
                CentralPassword = package.IncludesSecrets ? sourceSystem.CentralPassword ?? string.Empty : preserved.CentralPassword ?? string.Empty
            });
        }

        if (package.CurrencyExchangeRates.Count > 0)
        {
            db.CurrencyExchangeRates.AddRange(package.CurrencyExchangeRates.Select(rate => new CurrencyExchangeRate
            {
                FromCurrency = rate.FromCurrency,
                ToCurrency = rate.ToCurrency,
                Rate = rate.Rate,
                ValidFrom = rate.ValidFrom,
                ValidTo = rate.ValidTo,
                Notes = rate.Notes,
                IsActive = rate.IsActive
            }));
        }

        var serverIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var server in package.HanaServers)
        {
            var entity = new HanaServer
            {
                SourceSystem = server.SourceSystem,
                Name = server.Name,
                Host = server.Host,
                Port = server.Port,
                Username = string.Empty,
                Password = string.Empty,
                DatabaseName = server.DatabaseName,
                UseSsl = server.UseSsl,
                ValidateCertificate = server.ValidateCertificate,
                AdditionalParams = server.AdditionalParams
            };
            db.HanaServers.Add(entity);
            await db.SaveChangesAsync();
            serverIdMap[server.Key] = entity.Id;
        }

        var siteIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var site in package.Sites)
        {
            preservedSiteSecrets.TryGetValue(BuildSiteSignature(site.Land, site.TSC, site.Schema, site.SourceSystem), out var preserved);
            var entity = new Site
            {
                HanaServerId = !string.IsNullOrWhiteSpace(site.HanaServerKey) && serverIdMap.TryGetValue(site.HanaServerKey, out var mappedServerId)
                    ? mappedServerId
                    : null,
                Schema = site.Schema,
                TSC = site.TSC,
                Land = site.Land,
                SourceSystem = site.SourceSystem,
                UsernameOverride = package.IncludesSecrets ? site.UsernameOverride ?? string.Empty : preserved.UsernameOverride ?? string.Empty,
                PasswordOverride = package.IncludesSecrets ? site.PasswordOverride ?? string.Empty : preserved.PasswordOverride ?? string.Empty,
                LocalExportFolderOverride = site.LocalExportFolderOverride,
                ManualImportFilePath = site.ManualImportFilePath,
                ManualImportLastUploadedAtUtc = site.ManualImportLastUploadedAtUtc,
                SapServiceUrl = site.SapServiceUrl,
                SapEntitySet = site.SapEntitySet,
                SapEntitySetsCache = site.SapEntitySetsCache,
                SapEntitySetsRefreshedAtUtc = site.SapEntitySetsRefreshedAtUtc,
                IsActive = site.IsActive
            };
            db.Sites.Add(entity);
            await db.SaveChangesAsync();
            siteIdMap[site.Key] = entity.Id;
        }

        if (package.FieldTransformationRules.Count > 0)
        {
            db.FieldTransformationRules.AddRange(package.FieldTransformationRules.Select(r => new FieldTransformationRule
            {
                SourceSystem = r.SourceSystem,
                SourceField = r.SourceField,
                TargetField = r.TargetField,
                TransformationType = r.TransformationType,
                RuleScope = r.RuleScope,
                Argument = r.Argument,
                SortOrder = r.SortOrder,
                IsActive = r.IsActive
            }));
        }

        if (package.SapSourceDefinitions.Count > 0)
        {
            db.SapSourceDefinitions.AddRange(package.SapSourceDefinitions
                .Where(x => siteIdMap.ContainsKey(x.SiteKey))
                .Select(x => new SapSourceDefinition
                {
                    SiteId = siteIdMap[x.SiteKey],
                    Alias = x.Alias,
                    EntitySet = x.EntitySet,
                    IsPrimary = x.IsPrimary,
                    IsActive = x.IsActive,
                    SortOrder = x.SortOrder
                }));
        }

        if (package.SapJoinDefinitions.Count > 0)
        {
            db.SapJoinDefinitions.AddRange(package.SapJoinDefinitions
                .Where(x => siteIdMap.ContainsKey(x.SiteKey))
                .Select(x => new SapJoinDefinition
                {
                    SiteId = siteIdMap[x.SiteKey],
                    LeftAlias = x.LeftAlias,
                    RightAlias = x.RightAlias,
                    LeftKeys = x.LeftKeys,
                    RightKeys = x.RightKeys,
                    JoinType = x.JoinType,
                    IsActive = x.IsActive,
                    SortOrder = x.SortOrder
                }));
        }

        if (package.SapFieldMappings.Count > 0)
        {
            db.SapFieldMappings.AddRange(package.SapFieldMappings
                .Where(x => siteIdMap.ContainsKey(x.SiteKey))
                .Select(x => new SapFieldMapping
                {
                    SiteId = siteIdMap[x.SiteKey],
                    TargetField = x.TargetField,
                    SourceExpression = x.SourceExpression,
                    IsRequired = x.IsRequired,
                    IsActive = x.IsActive,
                    SortOrder = x.SortOrder
                }));
        }

        await db.SaveChangesAsync();
    }
    private static string BuildSiteSignature(string land, string tsc, string schema, string sourceSystem)
        => $"{land}|{tsc}|{schema}|{sourceSystem}".ToUpperInvariant();

    private static List<ConfigTransferSourceSystemDefinition> BuildDefaultSourceSystems()
    {
        return
        [
            new ConfigTransferSourceSystemDefinition
            {
                Code = "SAP",
                DisplayName = "SAP",
                ConnectionKind = SourceSystemConnectionKinds.SapGateway,
                IsActive = true,
                CentralServiceUrl = string.Empty
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
}
