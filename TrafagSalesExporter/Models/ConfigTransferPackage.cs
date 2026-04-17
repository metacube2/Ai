namespace TrafagSalesExporter.Models;

public class ConfigTransferPackage
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IncludesSecrets { get; set; }
    public ConfigTransferSharePoint? SharePointConfig { get; set; }
    public ConfigTransferExportSettings? ExportSettings { get; set; }
    public List<ConfigTransferCurrencyExchangeRate> CurrencyExchangeRates { get; set; } = [];
    public List<ConfigTransferHanaServer> HanaServers { get; set; } = [];
    public List<ConfigTransferSite> Sites { get; set; } = [];
    public List<FieldTransformationRule> FieldTransformationRules { get; set; } = [];
    public List<ConfigTransferSapSourceDefinition> SapSourceDefinitions { get; set; } = [];
    public List<ConfigTransferSapJoinDefinition> SapJoinDefinitions { get; set; } = [];
    public List<ConfigTransferSapFieldMapping> SapFieldMappings { get; set; } = [];
}

public class ConfigTransferSharePoint
{
    public string SiteUrl { get; set; } = string.Empty;
    public string ExportFolder { get; set; } = string.Empty;
    public string CentralExportFolder { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
}

public class ConfigTransferExportSettings
{
    public string DateFilter { get; set; } = "2025-01-01";
    public int TimerHour { get; set; } = 3;
    public int TimerMinute { get; set; }
    public bool TimerEnabled { get; set; } = true;
    public bool DebugLoggingEnabled { get; set; }
    public string LocalSiteExportFolder { get; set; } = string.Empty;
    public string LocalConsolidatedExportFolder { get; set; } = string.Empty;
    public string? SapUsername { get; set; }
    public string? SapPassword { get; set; }
    public string? Bi1Username { get; set; }
    public string? Bi1Password { get; set; }
    public string? SageUsername { get; set; }
    public string? SagePassword { get; set; }
}

public class ConfigTransferCurrencyExchangeRate
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ConfigTransferHanaServer
{
    public string Key { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 30015;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public bool ValidateCertificate { get; set; }
    public string AdditionalParams { get; set; } = string.Empty;
}

public class ConfigTransferSite
{
    public string Key { get; set; } = Guid.NewGuid().ToString("N");
    public string? HanaServerKey { get; set; }
    public string Schema { get; set; } = string.Empty;
    public string TSC { get; set; } = string.Empty;
    public string Land { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = "SAP";
    public string? UsernameOverride { get; set; }
    public string? PasswordOverride { get; set; }
    public string LocalExportFolderOverride { get; set; } = string.Empty;
    public string ManualImportFilePath { get; set; } = string.Empty;
    public DateTime? ManualImportLastUploadedAtUtc { get; set; }
    public string SapServiceUrl { get; set; } = string.Empty;
    public string SapEntitySet { get; set; } = string.Empty;
    public string SapEntitySetsCache { get; set; } = string.Empty;
    public DateTime? SapEntitySetsRefreshedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ConfigTransferSapSourceDefinition
{
    public string SiteKey { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string EntitySet { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class ConfigTransferSapJoinDefinition
{
    public string SiteKey { get; set; } = string.Empty;
    public string LeftAlias { get; set; } = string.Empty;
    public string RightAlias { get; set; } = string.Empty;
    public string LeftKeys { get; set; } = string.Empty;
    public string RightKeys { get; set; } = string.Empty;
    public string JoinType { get; set; } = "Left";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class ConfigTransferSapFieldMapping
{
    public string SiteKey { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string SourceExpression { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
