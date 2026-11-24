using Microsoft.Data.Sqlite;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>Wochensicherung der Datenbank in der App (2026-11-24).</summary>
public sealed class DatabaseBackupTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dbbackup-" + Guid.NewGuid().ToString("N"));
    private readonly string _db;

    public DatabaseBackupTests()
    {
        Directory.CreateDirectory(_root);
        _db = Path.Combine(_root, "trafag_exporter.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
        }
    }

    private void CreateDatabase(int rows)
    {
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _db, Pooling = false }.ToString());
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE t (id INTEGER PRIMARY KEY, v TEXT);";
        cmd.ExecuteNonQuery();
        for (var i = 0; i < rows; i++)
        {
            cmd.CommandText = $"INSERT INTO t (v) VALUES ('Zeile {i}')";
            cmd.ExecuteNonQuery();
        }
    }

    private static long Count(string path)
    {
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ToString());
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM t";
        return (long)cmd.ExecuteScalar()!;
    }

    private static readonly DatabaseBackupOptions Options = new();

    [Theory]
    // Samstag 24.11.2026 ist ein Dienstag; 28.11.2026 ist Samstag.
    [InlineData("2026-11-28 03:00", true)]   // Samstag im Fenster
    [InlineData("2026-11-28 04:59", true)]
    [InlineData("2026-11-28 02:59", false)]  // vor dem Fenster
    [InlineData("2026-11-28 05:00", false)]  // nach dem Fenster
    [InlineData("2026-11-28 10:00", false)]  // tagsueber nie, auch wenn faellig
    [InlineData("2026-11-24 03:30", false)]  // Dienstag, letzte Sicherung jung genug
    public void IsDue_Nur_Am_Sicherungstag_Im_Nachtfenster(string now, bool expected)
    {
        var at = DateTime.Parse(now);
        var newest = new DateTime(2026, 11, 21, 3, 5, 0);
        Assert.Equal(expected, DatabaseBackupFiles.IsDue(at, Options, newest, newest));
    }

    [Fact]
    public void IsDue_Holt_Eine_Verpasste_Woche_In_Der_Naechsten_Nacht_Nach_Aber_Nicht_Tagsueber()
    {
        var old = new DateTime(2026, 11, 16, 17, 0, 0); // letzte Sicherung des Deploy-Werkzeugs
        Assert.True(DatabaseBackupFiles.IsDue(new DateTime(2026, 11, 25, 3, 10, 0), Options, old, null));
        Assert.False(DatabaseBackupFiles.IsDue(new DateTime(2026, 11, 24, 14, 0, 0), Options, old, null));
        Assert.True(DatabaseBackupFiles.IsDue(new DateTime(2026, 11, 25, 3, 10, 0), Options, null, null));
    }

    [Fact]
    public void IsDue_Nicht_Zweimal_In_Derselben_Nacht_Und_Nicht_Wenn_Ausgeschaltet()
    {
        var saturday = new DateTime(2026, 11, 28, 3, 40, 0);
        var ownToday = new DateTime(2026, 11, 28, 3, 1, 0);
        Assert.False(DatabaseBackupFiles.IsDue(saturday, Options, ownToday, ownToday));
        Assert.False(DatabaseBackupFiles.IsDue(saturday, new DatabaseBackupOptions { Enabled = false }, null, null));
    }

    [Fact]
    public void Create_Schreibt_Eine_Lesbare_Kopie_Unter_Dem_Namen_Den_Das_Deploy_Werkzeug_Erkennt()
    {
        CreateDatabase(250);
        var result = DatabaseBackupFiles.Create(_db, new DateTime(2026, 11, 28, 3, 0, 0), keep: 4);

        Assert.True(result.Ok, result.Message);
        Assert.Equal(Path.Combine(_root, "trafag_exporter.db.before-appsicherung-20261128-030000.bak"), result.File);
        Assert.Equal(250, Count(result.File!));
        // Das Deploy-Werkzeug sucht trafag_exporter.db.before-*.bak (DeployHeadless Program.cs, Wochensicherung).
        Assert.Contains(result.File!, Directory.GetFiles(_root, "trafag_exporter.db.before-*.bak"));
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }

    [Fact]
    public void Prune_Behaelt_Die_Neuesten_Eigenen_Und_Laesst_Die_Des_Deploy_Werkzeugs_Stehen()
    {
        CreateDatabase(3);
        var deploy = _db + ".before-wochensicherung-20261116-165059.bak";
        File.WriteAllText(deploy, "x");
        for (var day = 1; day <= 6; day++)
            Assert.True(DatabaseBackupFiles.Create(_db, new DateTime(2026, 11, day, 3, 0, 0), keep: 4).Ok);

        var own = Directory.GetFiles(_root, "*.before-appsicherung-*.bak").Select(Path.GetFileName).Order().ToList();
        Assert.Equal(
            ["trafag_exporter.db.before-appsicherung-20261103-030000.bak", "trafag_exporter.db.before-appsicherung-20261104-030000.bak",
             "trafag_exporter.db.before-appsicherung-20261105-030000.bak", "trafag_exporter.db.before-appsicherung-20261106-030000.bak"],
            own);
        Assert.True(File.Exists(deploy));
    }

    [Fact]
    public void Create_Ohne_Datenbank_Meldet_Fehler_Und_Hinterlaesst_Nichts()
    {
        var result = DatabaseBackupFiles.Create(_db, new DateTime(2026, 11, 28, 3, 0, 0), keep: 4);
        Assert.False(result.Ok);
        Assert.Contains("nicht gefunden", result.Message);
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public void List_Zeigt_Eigene_Und_Fremde_Sicherungen_Neueste_Zuerst()
    {
        CreateDatabase(1);
        var deploy = _db + ".before-wochensicherung-20261116-165059.bak";
        File.WriteAllText(deploy, "x");
        File.SetLastWriteTime(deploy, new DateTime(2026, 11, 16, 17, 4, 0));
        File.WriteAllText(_db + ".before-x.bak.source", "nicht zaehlen");
        var own = DatabaseBackupFiles.Create(_db, DateTime.Now, keep: 4);

        var list = DatabaseBackupFiles.List(_db);
        Assert.Equal(2, list.Count);
        Assert.True(list[0].FromApp);
        Assert.Equal(Path.GetFileName(own.File), list[0].Name);
        Assert.False(list[1].FromApp);
    }

    [Fact]
    public void ResolveDatabasePath_Wie_Sqlite_Relativ_Zum_Arbeitsverzeichnis()
    {
        Assert.Equal(Path.GetFullPath("trafag_exporter.db"),
            DatabaseBackupFiles.ResolveDatabasePath("Data Source=trafag_exporter.db;Default Timeout=60"));
    }
}
