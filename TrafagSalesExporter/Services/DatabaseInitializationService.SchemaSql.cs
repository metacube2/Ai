namespace TrafagSalesExporter.Services;

internal static class DatabaseSchemaSql
{
    internal static string GetExportLogsCreateSql() => @"
CREATE TABLE ExportLogs (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Timestamp TEXT NOT NULL,
    SiteId INTEGER NOT NULL,
    Land TEXT NOT NULL,
    TSC TEXT NOT NULL,
    Status TEXT NOT NULL,
    RowCount INTEGER NOT NULL,
    ErrorMessage TEXT NULL,
    FileName TEXT NOT NULL DEFAULT '',
    FilePath TEXT NOT NULL DEFAULT '',
    DurationSeconds REAL NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    internal static string GetExportSettingsCreateSql() => @"
CREATE TABLE ExportSettings (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    DateFilter TEXT NOT NULL,
    TimerHour INTEGER NOT NULL,
    TimerMinute INTEGER NOT NULL,
    TimerEnabled INTEGER NOT NULL,
    DebugLoggingEnabled INTEGER NOT NULL DEFAULT 0,
    LocalSiteExportFolder TEXT NOT NULL DEFAULT '',
    LocalConsolidatedExportFolder TEXT NOT NULL DEFAULT '',
    ExchangeRateDateField TEXT NOT NULL DEFAULT 'PostingDate'
);";

    internal static string GetHanaServersCreateSql() => @"
CREATE TABLE HanaServers (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SourceSystem TEXT NOT NULL,
    Name TEXT NOT NULL,
    Host TEXT NOT NULL,
    Port INTEGER NOT NULL,
    DatabaseName TEXT NOT NULL DEFAULT '',
    UseSsl INTEGER NOT NULL DEFAULT 0,
    ValidateCertificate INTEGER NOT NULL DEFAULT 0,
    AdditionalParams TEXT NOT NULL DEFAULT ''
);";

    internal static string GetSitesCreateSql() => @"
CREATE TABLE Sites (
    Id INTEGER NOT NULL CONSTRAINT PK_Sites PRIMARY KEY AUTOINCREMENT,
    HanaServerId INTEGER NULL,
    Schema TEXT NOT NULL,
    TSC TEXT NOT NULL,
    Land TEXT NOT NULL,
    SourceSystem TEXT NOT NULL DEFAULT 'SAP',
    UsernameOverride TEXT NOT NULL DEFAULT '',
    PasswordOverride TEXT NOT NULL DEFAULT '',
    LocalExportFolderOverride TEXT NOT NULL DEFAULT '',
    ManualImportFilePath TEXT NOT NULL DEFAULT '',
    ManualImportLastUploadedAtUtc TEXT NULL,
    SapServiceUrl TEXT NOT NULL DEFAULT '',
    SapEntitySet TEXT NOT NULL DEFAULT '',
    SapEntitySetsCache TEXT NOT NULL DEFAULT '',
    SapEntitySetsRefreshedAtUtc TEXT NULL,
    IsActive INTEGER NOT NULL,
    CONSTRAINT FK_Sites_HanaServers_HanaServerId FOREIGN KEY (HanaServerId) REFERENCES HanaServers (Id)
);";

    internal static string GetAppEventLogsCreateSql() => @"
CREATE TABLE AppEventLogs (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Timestamp TEXT NOT NULL,
    Level TEXT NOT NULL,
    Category TEXT NOT NULL,
    SiteId INTEGER NULL,
    Land TEXT NOT NULL,
    Message TEXT NOT NULL,
    Details TEXT NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    internal static string GetCentralSalesRecordsCreateSql() => @"
CREATE TABLE CentralSalesRecords (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    StoredAtUtc TEXT NOT NULL,
    SiteId INTEGER NOT NULL,
    SourceSystem TEXT NOT NULL,
    ExtractionDate TEXT NOT NULL,
    Tsc TEXT NOT NULL,
    DocumentEntry INTEGER NOT NULL DEFAULT 0,
    InvoiceNumber TEXT NOT NULL,
    PositionOnInvoice INTEGER NOT NULL,
    Material TEXT NOT NULL,
    Name TEXT NOT NULL,
    ProductGroup TEXT NOT NULL,
    ProductHierarchyCode TEXT NOT NULL DEFAULT '',
    ProductHierarchyText TEXT NOT NULL DEFAULT '',
    ProductFamilyCode TEXT NOT NULL DEFAULT '',
    ProductFamilyText TEXT NOT NULL DEFAULT '',
    ProductDivisionCode TEXT NOT NULL DEFAULT '',
    ProductDivisionText TEXT NOT NULL DEFAULT '',
    ProductMappingAssigned TEXT NOT NULL DEFAULT '',
    Quantity TEXT NOT NULL,
    SupplierNumber TEXT NOT NULL,
    SupplierName TEXT NOT NULL,
    SupplierCountry TEXT NOT NULL,
    CustomerNumber TEXT NOT NULL,
    CustomerName TEXT NOT NULL,
    CustomerCountry TEXT NOT NULL,
    CustomerIndustry TEXT NOT NULL,
    StandardCost TEXT NOT NULL,
    StandardCostCurrency TEXT NOT NULL,
    PurchaseOrderNumber TEXT NOT NULL,
    SalesPriceValue TEXT NOT NULL,
    SalesCurrency TEXT NOT NULL,
    DocumentCurrency TEXT NOT NULL DEFAULT '',
    DocumentTotalForeignCurrency TEXT NOT NULL DEFAULT '0',
    DocumentTotalLocalCurrency TEXT NOT NULL DEFAULT '0',
    VatSumForeignCurrency TEXT NOT NULL DEFAULT '0',
    VatSumLocalCurrency TEXT NOT NULL DEFAULT '0',
    DocumentRate TEXT NOT NULL DEFAULT '0',
    CompanyCurrency TEXT NOT NULL DEFAULT '',
    Incoterms2020 TEXT NOT NULL,
    SalesResponsibleEmployee TEXT NOT NULL,
    PostingDate TEXT NULL,
    InvoiceDate TEXT NULL,
    OrderDate TEXT NULL,
    Land TEXT NOT NULL,
    DocumentType TEXT NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    internal static string GetSapSourceDefinitionsCreateSql() => @"
CREATE TABLE SapSourceDefinitions (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    Alias TEXT NOT NULL,
    EntitySet TEXT NOT NULL,
    IsPrimary INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    internal static string GetSapJoinDefinitionsCreateSql() => @"
CREATE TABLE SapJoinDefinitions (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    LeftAlias TEXT NOT NULL,
    RightAlias TEXT NOT NULL,
    LeftKeys TEXT NOT NULL,
    RightKeys TEXT NOT NULL,
    JoinType TEXT NOT NULL DEFAULT 'Left',
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    internal static string GetSapFieldMappingsCreateSql() => @"
CREATE TABLE SapFieldMappings (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    TargetField TEXT NOT NULL,
    SourceExpression TEXT NOT NULL,
    IsRequired INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    internal static string GetManualExcelColumnMappingsCreateSql() => @"
CREATE TABLE ManualExcelColumnMappings (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    TargetField TEXT NOT NULL,
    SourceHeader TEXT NOT NULL,
    IsRequired INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    internal static string GetFinanceReferencesCreateSql() => @"
CREATE TABLE FinanceReferences (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Key TEXT NOT NULL,
    Label TEXT NOT NULL,
    Year INTEGER NOT NULL DEFAULT 2025,
    LocalCurrencyValue TEXT NULL,
    CheckValue TEXT NULL,
    Notes TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1
);";

    internal static string GetFinanceIntercompanyRulesCreateSql() => @"
CREATE TABLE FinanceIntercompanyRules (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ScopeKey TEXT NOT NULL DEFAULT '',
    CustomerNumber TEXT NOT NULL DEFAULT '',
    CustomerNameContains TEXT NOT NULL DEFAULT '',
    Notes TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1
);";

    internal static string GetFinanceRulesCreateSql() => @"
CREATE TABLE FinanceRules (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ScopeKey TEXT NOT NULL DEFAULT '',
    Year INTEGER NULL,
    RuleType TEXT NOT NULL DEFAULT 'Exclude',
    FieldName TEXT NOT NULL DEFAULT '',
    MatchType TEXT NOT NULL DEFAULT 'Contains',
    MatchValue TEXT NOT NULL DEFAULT '',
    NumericValue TEXT NULL,
    Notes TEXT NOT NULL DEFAULT '',
    SortOrder INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1
);";

    internal static string GetNavigationMenuItemsCreateSql() => @"
CREATE TABLE NavigationMenuItems (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Key TEXT NOT NULL,
    ParentKey TEXT NULL,
    TitleDe TEXT NOT NULL DEFAULT '',
    TitleEn TEXT NOT NULL DEFAULT '',
    Icon TEXT NOT NULL DEFAULT '',
    Href TEXT NOT NULL DEFAULT '',
    ItemType TEXT NOT NULL DEFAULT 'Link',
    Match TEXT NOT NULL DEFAULT 'Prefix',
    RequiredPolicy TEXT NOT NULL DEFAULT '',
    IsVisible INTEGER NOT NULL DEFAULT 1,
    IsExpanded INTEGER NOT NULL DEFAULT 0,
    IsSystem INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0
);";

    internal static string GetPurchasingEkkoCacheCreateSql() => @"
CREATE TABLE PurchasingEkkoCache (
    Ebeln TEXT NOT NULL PRIMARY KEY,
    Bedat TEXT NULL,
    Aedat TEXT NULL,
    Lifnr TEXT NOT NULL DEFAULT '',
    Bukrs TEXT NOT NULL DEFAULT '',
    Bsart TEXT NOT NULL DEFAULT '',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL
);";

    internal static string GetPurchasingEkpoCacheCreateSql() => @"
CREATE TABLE PurchasingEkpoCache (
    Ebeln TEXT NOT NULL,
    Ebelp TEXT NOT NULL,
    Matnr TEXT NOT NULL DEFAULT '',
    Txz01 TEXT NOT NULL DEFAULT '',
    Matkl TEXT NOT NULL DEFAULT '',
    Menge TEXT NOT NULL DEFAULT '0',
    Meins TEXT NOT NULL DEFAULT '',
    Netwr TEXT NOT NULL DEFAULT '0',
    Loekz TEXT NOT NULL DEFAULT '',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Ebeln, Ebelp)
);";

    internal static string GetPurchasingEketCacheCreateSql() => @"
CREATE TABLE PurchasingEketCache (
    Ebeln TEXT NOT NULL,
    Ebelp TEXT NOT NULL,
    Etenr TEXT NOT NULL,
    Eindt TEXT NULL,
    Menge TEXT NOT NULL DEFAULT '0',
    Wemng TEXT NOT NULL DEFAULT '0',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Ebeln, Ebelp, Etenr)
);";

    internal static string GetPurchasingSyncStateCreateSql() => @"
CREATE TABLE PurchasingSyncState (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Mode TEXT NOT NULL DEFAULT '',
    Status TEXT NOT NULL DEFAULT '',
    StartedAtUtc TEXT NULL,
    CompletedAtUtc TEXT NULL,
    FromDate TEXT NULL,
    ToDate TEXT NULL,
    LastSuccessfulDeltaAtUtc TEXT NULL,
    EkkoRows INTEGER NOT NULL DEFAULT 0,
    EkpoRows INTEGER NOT NULL DEFAULT 0,
    EketRows INTEGER NOT NULL DEFAULT 0,
    Message TEXT NOT NULL DEFAULT ''
);";
}
