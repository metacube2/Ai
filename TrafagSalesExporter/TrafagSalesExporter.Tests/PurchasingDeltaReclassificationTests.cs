using Microsoft.Data.Sqlite;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Tests fuer die Stammdaten-Nachklassifizierung im Einkauf-Delta
/// (<see cref="PurchasingDataRefreshService.ApplyMaterialMasterToWholeCacheAsync"/>).
///
/// Hintergrund (Sitzung mit Marco und Armin, 2026-07-30): Auf die Frage, ob im SAP-Materialstamm
/// nachgepflegte Warengruppen im Dashboard ankommen, wurde "ja, dynamisch" geantwortet. Das war nur
/// zur Haelfte richtig - das naechtliche Delta laedt nur geaenderte und noch offene Belege, also
/// blieb ein Material, das ausschliesslich auf alten, abgeschlossenen Bestellungen liegt, dauerhaft
/// bei seiner alten Warengruppe. Genau das ist der Dummy-Fall (produktiv 34.6 % aller Positionen
/// ohne verwertbare Warengruppe). Diese Tests halten fest, dass die Nachklassifizierung den GANZEN
/// Cache erfasst und nicht nur die geholten Belege.
/// </summary>
public class PurchasingDeltaReclassificationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public PurchasingDeltaReclassificationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Execute(@"
CREATE TABLE PurchasingEkpoCache (
    Ebeln TEXT NOT NULL,
    Ebelp TEXT NOT NULL,
    Matnr TEXT NOT NULL DEFAULT '',
    Matkl TEXT NOT NULL DEFAULT '',
    MaraMatkl TEXT NOT NULL DEFAULT '',
    MaraAbc TEXT NOT NULL DEFAULT '',
    MaraXyz TEXT NOT NULL DEFAULT '',
    Mstae TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (Ebeln, Ebelp)
);");
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task ApplyMaterialMaster_Updates_Item_On_Old_Closed_Order()
    {
        // Der Kernfall: Bestellung ist alt und abgeschlossen, taucht im Delta also nie auf. Trotzdem
        // muss die nachgepflegte Warengruppe ankommen - vorher blieb sie auf der Dummy-Gruppe "01".
        //
        // Wichtig am Aufbau: Der EKPO-Cache haelt die Materialnummer SAP-intern zero-padded
        // ("000000000000002217"), die Stammdaten-Map ist dagegen ueber NormalizeMatnr geschluesselt,
        // das fuehrende Nullen entfernt ("2217"). Die Nachklassifizierung muss also dieselben
        // Resolve-Funktionen nutzen wie der Upsert, sonst findet sie nichts und wuerde die
        // Warengruppe sogar leerschreiben.
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Matkl, MaraMatkl) VALUES ('ALT1', '10', '000000000000002217', '01', '01');");

        var updated = await ApplyAsync(
            statusMap: new() { ["2217"] = new PurchasingDataRefreshService.MaterialMasterInfo("", "20.05.00") },
            classificationMap: []);

        Assert.Equal(1, updated);
        Assert.Equal("20.05.00", ReadSingle("MaraMatkl"));
    }

    [Fact]
    public async Task ApplyMaterialMaster_Writes_Abc_Xyz_And_Status()
    {
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr) VALUES ('ALT2', '10', 'C34882');");

        var updated = await ApplyAsync(
            statusMap: new() { ["C34882"] = new PurchasingDataRefreshService.MaterialMasterInfo("98", "30.01.00") },
            classificationMap: new() { ["C34882"] = new PurchasingDataRefreshService.MaterialClassification("A", "Z") });

        Assert.Equal(1, updated);
        Assert.Equal("30.01.00", ReadSingle("MaraMatkl"));
        Assert.Equal("A", ReadSingle("MaraAbc"));
        Assert.Equal("Z", ReadSingle("MaraXyz"));
        Assert.Equal("98", ReadSingle("Mstae"));
    }

    [Fact]
    public async Task ApplyMaterialMaster_Reports_Zero_When_Nothing_Changed()
    {
        // Zweiter Nachtlauf ohne Stammdatenaenderung darf nicht alle Cachezeilen umschreiben,
        // sonst kostet jede Nacht unnoetig Schreiblast auf 237'000 Zeilen.
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl) VALUES ('ALT3', '10', 'C34882', '30.01.00');");

        var updated = await ApplyAsync(
            statusMap: new() { ["C34882"] = new PurchasingDataRefreshService.MaterialMasterInfo("", "30.01.00") },
            classificationMap: []);

        Assert.Equal(0, updated);
    }

    [Fact]
    public async Task ApplyMaterialMaster_Leaves_Cache_Untouched_When_MasterData_Empty()
    {
        // Schutz gegen den teuren Fehlerfall: ein fehlgeschlagener oder leerer Stammdaten-Read darf
        // die vorhandenen Warengruppen NICHT flaechendeckend leerschreiben.
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl) VALUES ('ALT4', '10', 'C34882', '30.01.00');");

        var updated = await ApplyAsync(statusMap: [], classificationMap: []);

        Assert.Equal(0, updated);
        Assert.Equal("30.01.00", ReadSingle("MaraMatkl"));
    }

    [Fact]
    public async Task ApplyMaterialMaster_Clears_Group_When_Material_Vanished_From_Master()
    {
        // Ist die Map befuellt, das Material aber nicht enthalten, gilt der Stamm als fuehrend:
        // die Warengruppe wird geleert, damit im Dashboard der COALESCE-Fallback auf die
        // Beleg-Warengruppe greift und keine veraltete Stammgruppe haengen bleibt.
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Matkl, MaraMatkl) VALUES ('ALT5', '10', 'WEG1', '01', '30.01.00');");

        var updated = await ApplyAsync(
            statusMap: new() { ["ANDERES"] = new PurchasingDataRefreshService.MaterialMasterInfo("", "20.05.00") },
            classificationMap: []);

        Assert.Equal(1, updated);
        Assert.Equal(string.Empty, ReadSingle("MaraMatkl"));
        Assert.Equal("01", ReadSingle("Matkl"));
    }

    [Fact]
    public async Task ApplyMaterialMaster_Skips_Rows_Without_Material()
    {
        // Gekontierte Bestellpositionen haben keine Materialnummer (eine der beiden Erklaerungen
        // fuer die Dummy-Warengruppen aus der Sitzung). Sie duerfen nicht angefasst werden.
        Execute("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Matkl, MaraMatkl) VALUES ('ALT6', '10', '', '01', '');");

        var updated = await ApplyAsync(
            statusMap: new() { ["C34882"] = new PurchasingDataRefreshService.MaterialMasterInfo("", "20.05.00") },
            classificationMap: []);

        Assert.Equal(0, updated);
        Assert.Equal(string.Empty, ReadSingle("MaraMatkl"));
    }

    private async Task<int> ApplyAsync(
        Dictionary<string, PurchasingDataRefreshService.MaterialMasterInfo> statusMap,
        Dictionary<string, PurchasingDataRefreshService.MaterialClassification> classificationMap)
    {
        await using var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync();
        var updated = await PurchasingDataRefreshService.ApplyMaterialMasterToWholeCacheAsync(
            _connection, transaction, statusMap, classificationMap, CancellationToken.None);
        await transaction.CommitAsync();
        return updated;
    }

    private void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private string ReadSingle(string column)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT {column} FROM PurchasingEkpoCache LIMIT 1;";
        return command.ExecuteScalar()?.ToString() ?? string.Empty;
    }
}
