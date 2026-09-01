using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ForeignProcurementEvidenceStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public ForeignProcurementEvidenceStoreTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private void CreatePurchasingCacheTables()
    {
        Execute(@"
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
);");
        Execute(@"
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
);");
    }

    private void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [Fact]
    public async Task LoadMaterialKeysAsync_ReturnsMaterial_WithActiveExternalPurchaseDocument()
    {
        CreatePurchasingCacheTables();
        Execute("INSERT INTO PurchasingEkkoCache (Ebeln, Lifnr, SupplierName, LastLoadedAtUtc) VALUES ('4500001', 'V-001', 'Aptasic SA', '2026-08-31');");
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Loekz, LastLoadedAtUtc) VALUES ('4500001', '10', '000000000000E11155', '', '2026-08-31');");

        var keys = await ForeignProcurementEvidenceStore.LoadMaterialKeysAsync(_db);

        Assert.Contains("E11155", keys);
    }

    [Fact]
    public async Task LoadMaterialKeysAsync_Excludes_DeletedPurchaseOrderLine()
    {
        CreatePurchasingCacheTables();
        Execute("INSERT INTO PurchasingEkkoCache (Ebeln, Lifnr, SupplierName, LastLoadedAtUtc) VALUES ('4500002', 'V-002', 'Presto Engineering', '2026-08-31');");
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Loekz, LastLoadedAtUtc) VALUES ('4500002', '10', 'E11221', 'X', '2026-08-31');");

        var keys = await ForeignProcurementEvidenceStore.LoadMaterialKeysAsync(_db);

        Assert.DoesNotContain("E11221", keys);
    }

    [Fact]
    public async Task LoadMaterialKeysAsync_Excludes_InternalTrafagVendor()
    {
        // Ein interner Bestelltransfer ist kein Fremdbezugsindiz - dieselbe Regel wie auf der
        // Verkaufsseite (GroupMarginSupplierClassifier.MatchesInternalSupplierMarker).
        CreatePurchasingCacheTables();
        Execute("INSERT INTO PurchasingEkkoCache (Ebeln, Lifnr, SupplierName, LastLoadedAtUtc) VALUES ('4500003', '60000', 'Trafag AG', '2026-08-31');");
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Loekz, LastLoadedAtUtc) VALUES ('4500003', '10', 'F88103', '', '2026-08-31');");

        var keys = await ForeignProcurementEvidenceStore.LoadMaterialKeysAsync(_db);

        Assert.DoesNotContain("F88103", keys);
    }

    [Fact]
    public async Task LoadMaterialKeysAsync_Excludes_EmptyVendor()
    {
        CreatePurchasingCacheTables();
        Execute("INSERT INTO PurchasingEkkoCache (Ebeln, Lifnr, SupplierName, LastLoadedAtUtc) VALUES ('4500004', '', '', '2026-08-31');");
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Loekz, LastLoadedAtUtc) VALUES ('4500004', '10', 'C13614', '', '2026-08-31');");

        var keys = await ForeignProcurementEvidenceStore.LoadMaterialKeysAsync(_db);

        Assert.DoesNotContain("C13614", keys);
    }

    [Fact]
    public async Task LoadMaterialKeysAsync_WithoutPurchasingCacheTables_ReturnsEmpty_InsteadOfThrowing()
    {
        // Ein Testkontext mit nur EnsureCreated() kennt die rohen Einkauf-Cache-Tabellen nicht;
        // produktiv legt die Schema-Initialisierung sie immer an. Muss trotzdem nicht werfen.
        var keys = await ForeignProcurementEvidenceStore.LoadMaterialKeysAsync(_db);

        Assert.Empty(keys);
    }
}
