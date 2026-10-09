namespace TrafagSalesExporter.Services;

internal static class DatabaseSchemaSql
{
    internal static string GetGroupMaterialMastersCreateSql() => @"
CREATE TABLE GroupMaterialMasters (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    MaterialKey TEXT NOT NULL,
    Plant TEXT NOT NULL,
    RefreshedAtUtc TEXT NOT NULL
);";

    internal static string GetCustomerMarketSegmentsCreateSql() => @"
CREATE TABLE CustomerMarketSegments (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Tsc TEXT NOT NULL,
    CustomerNumber TEXT NOT NULL,
    CustomerName TEXT NOT NULL DEFAULT '',
    Segment TEXT NOT NULL,
    IsConfirmed INTEGER NOT NULL DEFAULT 0,
    ProposalNote TEXT NOT NULL DEFAULT '',
    Source TEXT NOT NULL DEFAULT '',
    UpdatedAtUtc TEXT NOT NULL
);";

    internal static string GetSegmentNamePatternsCreateSql() => @"
CREATE TABLE SegmentNamePatterns (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Pattern TEXT NOT NULL,
    Segment TEXT NOT NULL,
    Tsc TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Note TEXT NOT NULL DEFAULT '',
    UpdatedAtUtc TEXT NOT NULL
);";

    internal static string GetMarketSurveyEntriesCreateSql() => @"
CREATE TABLE MarketSurveyEntries (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SurveyName TEXT NOT NULL DEFAULT '',
    Country TEXT NOT NULL DEFAULT '',
    CustomerName TEXT NOT NULL DEFAULT '',
    CustomerShort TEXT NOT NULL DEFAULT '',
    CustomerType TEXT NOT NULL DEFAULT '',
    BusinessType TEXT NOT NULL DEFAULT '',
    Application TEXT NOT NULL DEFAULT '',
    ApplicationDescription TEXT NOT NULL DEFAULT '',
    Status TEXT NOT NULL DEFAULT '',
    TrafagUsp TEXT NOT NULL DEFAULT '',
    Competitor TEXT NOT NULL DEFAULT '',
    Product TEXT NOT NULL DEFAULT '',
    MaterialNumber TEXT NOT NULL DEFAULT '',
    EstimatedQuantity TEXT NOT NULL DEFAULT '',
    EstimatedPrice TEXT NOT NULL DEFAULT '',
    Comments TEXT NOT NULL DEFAULT '',
    LinkedTsc TEXT NOT NULL DEFAULT '',
    LinkedCustomerNumber TEXT NOT NULL DEFAULT '',
    UpdatedAtUtc TEXT NOT NULL
);";

    internal static string GetGroupStandardCostsCreateSql() => @"
CREATE TABLE GroupStandardCosts (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    MaterialKey TEXT NOT NULL,
    ValuationArea TEXT NOT NULL,
    UnitCost REAL NOT NULL DEFAULT 0,
    Currency TEXT NOT NULL DEFAULT '',
    RefreshedAtUtc TEXT NOT NULL
);";

    internal static string GetSupplierMaterialOverridesCreateSql() => @"
CREATE TABLE SupplierMaterialOverrides (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Tsc TEXT NOT NULL,
    MaterialKey TEXT NOT NULL,
    SupplierNumber TEXT NOT NULL DEFAULT '',
    SupplierName TEXT NOT NULL DEFAULT '',
    SupplierCountry TEXT NOT NULL DEFAULT '',
    Source TEXT NOT NULL DEFAULT '',
    ImportedAtUtc TEXT NOT NULL
);";

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
    AuditCsvEnabled INTEGER NOT NULL DEFAULT 1,
    UseAuditCsvAsCentralSource INTEGER NOT NULL DEFAULT 0,
    LocalAuditCsvFolder TEXT NOT NULL DEFAULT '',
    ExchangeRateDateField TEXT NOT NULL DEFAULT 'PostingDate',
    GroupMarginCostCurrencyMode TEXT NOT NULL DEFAULT 'Convert',
    -- Marker fuer den einmaligen Nachzug des Waehrungsbeschlusses vom 2026-08-27; bei einer
    -- neuen Datenbank ist nichts nachzuziehen, deshalb direkt 1.
    GroupMarginCostCurrencyDecision20260827Applied INTEGER NOT NULL DEFAULT 1,
    SupplierFallbackMode TEXT NOT NULL DEFAULT 'ChPlantMaster',
    InternalSupplierCostSourceMode TEXT NOT NULL DEFAULT 'DeliveringEntityCosts',
    B1GroupStandardCostMode TEXT NOT NULL DEFAULT 'LatestPositive',
    GroupMarginChfRateMode TEXT NOT NULL DEFAULT 'BudgetRate',
    MarcForeignProcurementMode TEXT NOT NULL DEFAULT 'Ignore',
    LastTimerRunUtc TEXT NULL
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
    SalesType TEXT NOT NULL DEFAULT '',
    GroupMaterialNumber TEXT NOT NULL DEFAULT '',
    CustomerNumber TEXT NOT NULL,
    CustomerName TEXT NOT NULL,
    CustomerCountry TEXT NOT NULL,
    CustomerIndustry TEXT NOT NULL,
    StandardCost TEXT NOT NULL,
    StandardCostCurrency TEXT NOT NULL,
    StandardCostVariable TEXT NULL,
    StandardCostFixed TEXT NULL,
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
    LineRegistrationDate TEXT NULL,
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

    // Trafag Projekte (2026-10-09) ersetzt die Poor Man's Project Management Suite (Tabelle ProjectItems, nie genutzt,
    // bleibt in bestehenden Datenbanken liegen). Doku docs/TRAFAG_PROJEKTE_2026-10-09.md.
    internal static IEnumerable<string> GetPmCreateSql()
    {
        yield return @"
CREATE TABLE PmProjects (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Key TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    Color TEXT NOT NULL DEFAULT '#C8501E',
    Icon TEXT NOT NULL DEFAULT 'ViewKanban',
    Template TEXT NOT NULL DEFAULT 'Kanban',
    LeadLogin TEXT NOT NULL DEFAULT '',
    LeadName TEXT NOT NULL DEFAULT '',
    StartDate TEXT NULL,
    DueDate TEXT NULL,
    NextNumber INTEGER NOT NULL DEFAULT 1,
    IsArchived INTEGER NOT NULL DEFAULT 0,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);";
        yield return @"
CREATE TABLE PmMembers (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ProjectId INTEGER NOT NULL,
    Login TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Role TEXT NOT NULL DEFAULT 'Member',
    AddedAtUtc TEXT NOT NULL
);";
        yield return @"
CREATE TABLE PmColumns (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ProjectId INTEGER NOT NULL,
    Name TEXT NOT NULL DEFAULT '',
    Category TEXT NOT NULL DEFAULT 'Todo',
    SortOrder INTEGER NOT NULL DEFAULT 0,
    WipLimit INTEGER NOT NULL DEFAULT 0
);";
        yield return @"
CREATE TABLE PmSprints (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ProjectId INTEGER NOT NULL,
    Name TEXT NOT NULL DEFAULT '',
    Goal TEXT NOT NULL DEFAULT '',
    StartDate TEXT NULL,
    EndDate TEXT NULL,
    State TEXT NOT NULL DEFAULT 'Planned',
    CompletedAtUtc TEXT NULL
);";
        yield return @"
CREATE TABLE PmTasks (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ProjectId INTEGER NOT NULL,
    Number INTEGER NOT NULL DEFAULT 0,
    Type TEXT NOT NULL DEFAULT 'Task',
    Title TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    ColumnId INTEGER NOT NULL DEFAULT 0,
    Priority TEXT NOT NULL DEFAULT 'Medium',
    AssigneeLogin TEXT NOT NULL DEFAULT '',
    AssigneeName TEXT NOT NULL DEFAULT '',
    ReporterLogin TEXT NOT NULL DEFAULT '',
    ReporterName TEXT NOT NULL DEFAULT '',
    Labels TEXT NOT NULL DEFAULT '',
    StartDate TEXT NULL,
    DueDate TEXT NULL,
    StoryPoints INTEGER NOT NULL DEFAULT 0,
    ParentId INTEGER NULL,
    EpicId INTEGER NULL,
    SprintId INTEGER NULL,
    Rank REAL NOT NULL DEFAULT 0,
    Cover TEXT NOT NULL DEFAULT '',
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    CompletedAtUtc TEXT NULL,
    IsArchived INTEGER NOT NULL DEFAULT 0
);";
        yield return @"
CREATE TABLE PmChecklistItems (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    TaskId INTEGER NOT NULL,
    Text TEXT NOT NULL DEFAULT '',
    IsDone INTEGER NOT NULL DEFAULT 0,
    SortOrder INTEGER NOT NULL DEFAULT 0
);";
        yield return @"
CREATE TABLE PmComments (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    TaskId INTEGER NOT NULL,
    Body TEXT NOT NULL DEFAULT '',
    AuthorLogin TEXT NOT NULL DEFAULT '',
    AuthorName TEXT NOT NULL DEFAULT '',
    CreatedAtUtc TEXT NOT NULL
);";
        yield return @"
CREATE TABLE PmActivities (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ProjectId INTEGER NOT NULL,
    TaskId INTEGER NULL,
    Login TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Text TEXT NOT NULL DEFAULT '',
    AtUtc TEXT NOT NULL
);";
    }

    internal static readonly string[] PmIndexSql =
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_PmProjects_Key ON PmProjects (Key)",
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_PmMembers_Project_Login ON PmMembers (ProjectId, Login)",
        "CREATE INDEX IF NOT EXISTS IX_PmColumns_Project ON PmColumns (ProjectId)",
        "CREATE INDEX IF NOT EXISTS IX_PmSprints_Project ON PmSprints (ProjectId)",
        "CREATE INDEX IF NOT EXISTS IX_PmTasks_Project ON PmTasks (ProjectId, IsArchived)",
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_PmTasks_Project_Number ON PmTasks (ProjectId, Number)",
        "CREATE INDEX IF NOT EXISTS IX_PmTasks_Assignee ON PmTasks (AssigneeLogin)",
        "CREATE INDEX IF NOT EXISTS IX_PmChecklistItems_Task ON PmChecklistItems (TaskId)",
        "CREATE INDEX IF NOT EXISTS IX_PmComments_Task ON PmComments (TaskId)",
        "CREATE INDEX IF NOT EXISTS IX_PmActivities_Project ON PmActivities (ProjectId, AtUtc)"
    ];

    // Trafag Reddit (2026-10-09): Forum, Doku docs/TRAFAG_REDDIT_2026-10-09.md.
    internal static IEnumerable<string> GetForumCreateSql()
    {
        yield return @"
CREATE TABLE ForumCommunities (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Slug TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    Icon TEXT NOT NULL DEFAULT 'Forum',
    Color TEXT NOT NULL DEFAULT '#C8501E',
    CreatedByLogin TEXT NOT NULL DEFAULT '',
    CreatedByName TEXT NOT NULL DEFAULT '',
    CreatedAtUtc TEXT NOT NULL,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    IsArchived INTEGER NOT NULL DEFAULT 0
);";
        yield return @"
CREATE TABLE ForumPosts (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    CommunityId INTEGER NOT NULL,
    Kind TEXT NOT NULL DEFAULT 'Discussion',
    Title TEXT NOT NULL DEFAULT '',
    Body TEXT NOT NULL DEFAULT '',
    Url TEXT NOT NULL DEFAULT '',
    Tags TEXT NOT NULL DEFAULT '',
    AuthorLogin TEXT NOT NULL DEFAULT '',
    AuthorName TEXT NOT NULL DEFAULT '',
    CreatedAtUtc TEXT NOT NULL,
    EditedAtUtc TEXT NULL,
    LastActivityUtc TEXT NOT NULL,
    UpVotes INTEGER NOT NULL DEFAULT 0,
    DownVotes INTEGER NOT NULL DEFAULT 0,
    CommentCount INTEGER NOT NULL DEFAULT 0,
    ViewCount INTEGER NOT NULL DEFAULT 0,
    AcceptedCommentId INTEGER NULL,
    IsPinned INTEGER NOT NULL DEFAULT 0,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);";
        yield return @"
CREATE TABLE ForumComments (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    PostId INTEGER NOT NULL,
    ParentId INTEGER NULL,
    Body TEXT NOT NULL DEFAULT '',
    AuthorLogin TEXT NOT NULL DEFAULT '',
    AuthorName TEXT NOT NULL DEFAULT '',
    CreatedAtUtc TEXT NOT NULL,
    EditedAtUtc TEXT NULL,
    UpVotes INTEGER NOT NULL DEFAULT 0,
    DownVotes INTEGER NOT NULL DEFAULT 0,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);";
        yield return @"
CREATE TABLE ForumVotes (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    TargetKind TEXT NOT NULL DEFAULT 'P',
    TargetId INTEGER NOT NULL,
    VoterLogin TEXT NOT NULL DEFAULT '',
    Value INTEGER NOT NULL DEFAULT 0,
    CreatedAtUtc TEXT NOT NULL
);";
        yield return @"
CREATE TABLE ForumBookmarks (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    PostId INTEGER NOT NULL,
    UserLogin TEXT NOT NULL DEFAULT '',
    CreatedAtUtc TEXT NOT NULL
);";
    }

    internal static readonly string[] ForumIndexSql =
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_ForumCommunities_Slug ON ForumCommunities (Slug)",
        "CREATE INDEX IF NOT EXISTS IX_ForumPosts_Community ON ForumPosts (CommunityId, IsDeleted)",
        "CREATE INDEX IF NOT EXISTS IX_ForumComments_Post ON ForumComments (PostId)",
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_ForumVotes_Target_Voter ON ForumVotes (TargetKind, TargetId, VoterLogin)",
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_ForumBookmarks_Post_User ON ForumBookmarks (PostId, UserLogin)"
    ];

    internal static string GetPurchasingEkkoCacheCreateSql() => @"
CREATE TABLE PurchasingEkkoCache (
    Ebeln TEXT NOT NULL PRIMARY KEY,
    Bedat TEXT NULL,
    Aedat TEXT NULL,
    Lifnr TEXT NOT NULL DEFAULT '',
    SupplierName TEXT NOT NULL DEFAULT '',
    Bukrs TEXT NOT NULL DEFAULT '',
    Bstyp TEXT NOT NULL DEFAULT '',
    Bsart TEXT NOT NULL DEFAULT '',
    Konnr TEXT NOT NULL DEFAULT '',
    Waers TEXT NOT NULL DEFAULT '',
    Wkurs TEXT NOT NULL DEFAULT '0',
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
    MaraMatkl TEXT NOT NULL DEFAULT '',
    Maktx TEXT NOT NULL DEFAULT '',
    Menge TEXT NOT NULL DEFAULT '0',
    Meins TEXT NOT NULL DEFAULT '',
    Netwr TEXT NOT NULL DEFAULT '0',
    Loekz TEXT NOT NULL DEFAULT '',
    Mstae TEXT NOT NULL DEFAULT '',
    Elikz TEXT NOT NULL DEFAULT '',
    Ktmng TEXT NOT NULL DEFAULT '0',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Ebeln, Ebelp)
);";

    /// <summary>
    /// Mengenkontrakt-Positionen (EKKO.BSTYP = K) aus <c>EinkKontraktSet</c>. Eine Zeile je
    /// Ebeln + Ebelp. Alle Zahlen als TEXT wie in den uebrigen Einkaufs-Caches; Datumsfelder
    /// sind ISO (yyyy-MM-dd) oder leer. Wird bei jedem Lauf vollstaendig ersetzt, nicht
    /// fortgeschrieben. Grundlage der Kennzahl "Offener Mengenkontraktwert (wie ME3L)".
    /// </summary>
    internal static string GetPurchasingContractCacheCreateSql() => @"
CREATE TABLE PurchasingContractCache (
    Ebeln TEXT NOT NULL,
    Ebelp TEXT NOT NULL,
    Bukrs TEXT NOT NULL DEFAULT '',
    Bsart TEXT NOT NULL DEFAULT '',
    Lifnr TEXT NOT NULL DEFAULT '',
    SupplierName TEXT NOT NULL DEFAULT '',
    Matnr TEXT NOT NULL DEFAULT '',
    Txz01 TEXT NOT NULL DEFAULT '',
    Matkl TEXT NOT NULL DEFAULT '',
    Waers TEXT NOT NULL DEFAULT '',
    Wkurs TEXT NOT NULL DEFAULT '0',
    Kdatb TEXT NULL,
    Kdate TEXT NULL,
    Loekz TEXT NOT NULL DEFAULT '',
    Meins TEXT NOT NULL DEFAULT '',
    Ktmng TEXT NOT NULL DEFAULT '0',
    Netpr TEXT NOT NULL DEFAULT '0',
    Peinh TEXT NOT NULL DEFAULT '1',
    Zwert TEXT NOT NULL DEFAULT '0',
    Abmng TEXT NOT NULL DEFAULT '0',
    Abwrt TEXT NOT NULL DEFAULT '0',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Ebeln, Ebelp)
);";

    /// <summary>
    /// Lebenszyklus-Code (MARA-ZZLZCOD) und Sortiments-Code (MARA-ZZLZCODSORT) je Material aus
    /// <c>EinkMatLzSet</c>. Nur Materialien mit mindestens einem gesetzten Code. <c>Matnr</c> ist
    /// NORMALISIERT (Grossbuchstaben, ohne fuehrende Nullen), damit der Join auf EKPO.MATNR
    /// unabhaengig von der Schreibweise greift. Wird bei jedem Lauf vollstaendig ersetzt.
    /// </summary>
    internal static string GetPurchasingMaterialLzCacheCreateSql() => @"
CREATE TABLE PurchasingMaterialLzCache (
    Matnr TEXT NOT NULL PRIMARY KEY,
    Lzcode TEXT NOT NULL DEFAULT '',
    Lzsort TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL
);";

    /// <summary>
    /// Verwendung einer Einkaufskomponente in verkuerzten Nummern (VKNR) mit deren Disponent, aus
    /// <c>ZSTR_LZCODE_USAGESet</c> Bottom-Up (<see cref="PurchasingComponentDispoLoader"/>). Grundlage der
    /// Produktgruppe im Spend-Aufriss. Schluessel normalisiert wie <c>PurchasingMaterialLzCache</c>.
    /// </summary>
    internal static string GetPurchasingComponentDispoCacheCreateSql() => @"
CREATE TABLE PurchasingComponentDispoCache (
    Kompnr TEXT NOT NULL,
    Vknr TEXT NOT NULL,
    VknrDispo TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Kompnr, Vknr)
);";

    /// <summary>Wann eine Komponente zuletzt gefragt wurde und mit wie vielen Verwendungen, auch bei 0 Treffern.</summary>
    internal static string GetPurchasingComponentDispoStateCreateSql() => @"
CREATE TABLE PurchasingComponentDispoState (
    Kompnr TEXT NOT NULL PRIMARY KEY,
    CheckedAtUtc TEXT NOT NULL,
    UsageCount INTEGER NOT NULL DEFAULT 0
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

    /// <summary>
    /// Zuletzt gelesener Lagerwert je Disponent, damit die KPI-Kachel einen Neustart uebersteht.
    ///
    /// VORFALL 2026-08-19 bis 2026-08-24, der zu dieser Tabelle fuehrte: Der Lagerwert lag
    /// ausschliesslich in einem Feld des Singleton-Readers, mit 20 Stunden Haltbarkeit. Jeder
    /// Neustart des IIS-Workers loeschte ihn, und weil der naechtliche Einkauf-Lauf nur im
    /// planmaessigen Slot laeuft (nicht im Nachhol-Lauf), war die Kachel praktisch jeden
    /// Morgen wieder auf "wartet auf Einkauf-Lauf". Ein gelesener Wert muss die Nacht
    /// ueberleben, sonst ist er fuer eine Kachel wertlos.
    ///
    /// BEWUSST OHNE HALTBARKEIT: ein alter Wert wird angezeigt, aber immer mit
    /// <c>ReadAtUtc</c> daneben. Ein sichtbar alter Stand ist fachlich brauchbar, eine leere
    /// Kachel ist es nicht. Die Bewertung des Alters gehoert zum Leser, nicht zum Speicher.
    ///
    /// Betraege liegen als Text vor, wie in allen Einkaufstabellen dieser Datenbank, und
    /// werden invariant formatiert und gelesen.
    /// </summary>
    internal static string GetPurchasingStockValueCacheCreateSql() => @"
CREATE TABLE PurchasingStockValueCache (
    ValuationArea TEXT NOT NULL,
    Planner TEXT NOT NULL,
    Value TEXT NOT NULL DEFAULT '0',
    Quantity TEXT NOT NULL DEFAULT '0',
    MaterialCount INTEGER NOT NULL DEFAULT 0,
    ReadAtUtc TEXT NOT NULL,
    PRIMARY KEY (ValuationArea, Planner)
);";

    /// <summary>
    /// Verlauf des Lagerwerts, ein Stand je Tag und Disponent (Wunsch aus dem Einkauf vom
    /// September 2026: Lagerwert woechentlich festhalten und als Trend zeigen).
    ///
    /// JE TAG, NICHT JE WOCHE GESPEICHERT: Der Einkauf-Lauf liest taeglich. Die Woche bildet
    /// erst die Anzeige (letzter Tag der Kalenderwoche). So geht nichts verloren, falls spaeter
    /// ein anderer Takt gewuenscht wird, und ein zweiter Lauf am selben Tag ersetzt nur diesen
    /// Tag.
    ///
    /// ALLE DISPONENTEN, NICHT NUR 001-005: Die Abgrenzung "Einkaufsteile" wird beim Lesen
    /// angewendet. Faellt Armins Entscheid zu Disponent 004 anders aus, rechnet sich damit der
    /// ganze Verlauf rueckwirkend richtig, statt dass alte Punkte eine andere Summe tragen.
    ///
    /// Wird NIE geloescht, anders als <c>PurchasingStockValueCache</c>.
    /// </summary>
    internal static string GetPurchasingStockValueHistoryCreateSql() => @"
CREATE TABLE PurchasingStockValueHistory (
    ValuationArea TEXT NOT NULL,
    SnapshotDate TEXT NOT NULL,
    Planner TEXT NOT NULL,
    Value TEXT NOT NULL DEFAULT '0',
    Quantity TEXT NOT NULL DEFAULT '0',
    MaterialCount INTEGER NOT NULL DEFAULT 0,
    ReadAtUtc TEXT NOT NULL,
    PRIMARY KEY (ValuationArea, SnapshotDate, Planner)
);";

    // MaterialUsageSet/MaterialParentSet - siehe docs/abap/README_LZCODE_WEBSERVICE.md.
    // SAP-seitiges EntitySet existiert noch nicht (Entwurf fuer Lucas), Tabellen bleiben
    // additiv leer, bis MaterialUsageDataRefreshService erfolgreich laden kann.
    internal static string GetMaterialUsageCacheCreateSql() => @"
CREATE TABLE MaterialUsageCache (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Richtung TEXT NOT NULL DEFAULT '',
    Vknr TEXT NOT NULL DEFAULT '',
    VknrMstae TEXT NOT NULL DEFAULT '',
    VknrDispo TEXT NOT NULL DEFAULT '',
    VknrVerbrauch TEXT NOT NULL DEFAULT '0',
    Kompnr TEXT NOT NULL DEFAULT '',
    KompnrMaktx TEXT NOT NULL DEFAULT '',
    KompnrMeins TEXT NOT NULL DEFAULT '',
    Menge TEXT NOT NULL DEFAULT '0',
    Exklusiv INTEGER NOT NULL DEFAULT 0,
    Verbrauch TEXT NOT NULL DEFAULT '0',
    Labst TEXT NOT NULL DEFAULT '0',
    FesteZugang TEXT NOT NULL DEFAULT '0',
    GeplZugang TEXT NOT NULL DEFAULT '0',
    FesteAbgang TEXT NOT NULL DEFAULT '0',
    GeplAbgang TEXT NOT NULL DEFAULT '0',
    Endbestand TEXT NOT NULL DEFAULT '0',
    Omeng TEXT NOT NULL DEFAULT '0',
    Mkmng TEXT NOT NULL DEFAULT '0',
    Stueckkosten TEXT NOT NULL DEFAULT '0',
    WertFesteZug TEXT NOT NULL DEFAULT '0',
    WertGeplZug TEXT NOT NULL DEFAULT '0',
    WertFesteAbg TEXT NOT NULL DEFAULT '0',
    WertGeplAbg TEXT NOT NULL DEFAULT '0',
    WertEndbestand TEXT NOT NULL DEFAULT '0',
    Owert TEXT NOT NULL DEFAULT '0',
    Omkwr TEXT NOT NULL DEFAULT '0',
    Dismm TEXT NOT NULL DEFAULT '',
    Minbe TEXT NOT NULL DEFAULT '0',
    Disls TEXT NOT NULL DEFAULT '',
    Bstfe TEXT NOT NULL DEFAULT '0',
    Eisbe TEXT NOT NULL DEFAULT '0',
    Mstae TEXT NOT NULL DEFAULT '',
    Mstav TEXT NOT NULL DEFAULT '',
    Beskz TEXT NOT NULL DEFAULT '',
    Zzlzcod TEXT NOT NULL DEFAULT '',
    Zzlzcodsort TEXT NOT NULL DEFAULT '',
    Baugruppe INTEGER NOT NULL DEFAULT 0,
    Waers TEXT NOT NULL DEFAULT '',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL
);";

    // Optionale ZC23-Referenzliste. Solange ein Disponent hier nicht gepflegt ist, zeigt der
    // Produktgruppen-Aufriss ehrlich "Disponent <Code>" statt eine Produktgruppe zu erfinden.
    // Die Tabelle ist bewusst getrennt vom ZLO03-Cache: ZLO03 liefert den Disponenten je
    // Kopfmaterial, ZC23 liefert dessen fachliche Produktgruppen-Bezeichnung.
    internal static string GetPurchasingProductGroupMapCreateSql() => @"
CREATE TABLE PurchasingProductGroupMap (
    Disponent TEXT NOT NULL PRIMARY KEY,
    ProductGroup TEXT NOT NULL DEFAULT '',
    ProductGroupText TEXT NOT NULL DEFAULT '',
    Source TEXT NOT NULL DEFAULT 'ZC23',
    UpdatedAtUtc TEXT NOT NULL DEFAULT ''
);";

    // SAP-ZDISPO-Referenz fuer den Produktgruppen-Aufriss. Die Tabelle erlaubt bewusst
    // Sternmuster und mehrere Gruppen je Muster; jeder Einkauf-Full-Load und jedes Delta
    // ersetzt ihren Inhalt atomar aus SAP OData.
    internal static string GetPurchasingSpendDisponentRuleCreateSql() => @"
CREATE TABLE PurchasingSpendDisponentRule (
    DisponentPattern TEXT NOT NULL,
    ProductGroup TEXT NOT NULL DEFAULT '',
    ProductGroupText TEXT NOT NULL DEFAULT '',
    Source TEXT NOT NULL DEFAULT 'SAP OData',
    UpdatedAtUtc TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (DisponentPattern, ProductGroup)
);";

    internal static string GetMaterialParentCacheCreateSql() => @"
CREATE TABLE MaterialParentCache (
    Kompnr TEXT NOT NULL,
    ElternMatnr TEXT NOT NULL,
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Kompnr, ElternMatnr)
);";

    internal static string GetMaterialUsageSyncStateCreateSql() => @"
CREATE TABLE MaterialUsageSyncState (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Status TEXT NOT NULL DEFAULT '',
    StartedAtUtc TEXT NULL,
    CompletedAtUtc TEXT NULL,
    UsageRows INTEGER NOT NULL DEFAULT 0,
    ParentRows INTEGER NOT NULL DEFAULT 0,
    Message TEXT NOT NULL DEFAULT ''
);";

    internal static string GetFinancialJournalEntriesCreateSql() => @"
CREATE TABLE FinancialJournalEntries (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    StoredAtUtc TEXT NOT NULL,
    ExtractionDate TEXT NOT NULL,
    Tsc TEXT NOT NULL DEFAULT '',
    Land TEXT NOT NULL DEFAULT '',
    CompanySchema TEXT NOT NULL DEFAULT '',
    CompanyCode TEXT NOT NULL DEFAULT '',
    SourceSystem TEXT NOT NULL DEFAULT '',
    JournalEntryId TEXT NOT NULL DEFAULT '',
    JournalEntryLineId INTEGER NOT NULL DEFAULT 0,
    PostingDate TEXT NULL,
    DueDate TEXT NULL,
    ClearingDate TEXT NULL,
    ClearingReference TEXT NOT NULL DEFAULT '',
    ReconciliationDate TEXT NULL,
    ClearingCount INTEGER NOT NULL DEFAULT 0,
    IsClearingCancelled INTEGER NOT NULL DEFAULT 0,
    FiscalYear INTEGER NOT NULL DEFAULT 0,
    FiscalPeriod INTEGER NOT NULL DEFAULT 0,
    AccountCode TEXT NOT NULL DEFAULT '',
    AccountName TEXT NOT NULL DEFAULT '',
    DebitAmount TEXT NOT NULL DEFAULT '0',
    CreditAmount TEXT NOT NULL DEFAULT '0',
    SignedAmountLocal TEXT NOT NULL DEFAULT '0',
    LocalCurrency TEXT NOT NULL DEFAULT '',
    TransactionCurrency TEXT NOT NULL DEFAULT '',
    SignedAmountTransaction TEXT NOT NULL DEFAULT '0',
    CostCenter TEXT NOT NULL DEFAULT '',
    Dimension2 TEXT NOT NULL DEFAULT '',
    LineMemo TEXT NOT NULL DEFAULT '',
    TransactionType TEXT NOT NULL DEFAULT '',
    SourceDocumentNumber TEXT NOT NULL DEFAULT '',
    IsManual INTEGER NOT NULL DEFAULT 0,
    IsReversal INTEGER NOT NULL DEFAULT 0
);";
}
