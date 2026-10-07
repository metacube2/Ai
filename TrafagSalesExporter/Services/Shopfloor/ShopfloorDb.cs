using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>Eine Ergebniszeile als Name/Wert-Paare.</summary>
public sealed class SfRow
{
    private readonly Dictionary<string, object?> _v;
    public SfRow(Dictionary<string, object?> values) => _v = values;

    public object? this[string name] => _v.TryGetValue(name, out var o) ? o : null;
    public string? Str(string name) => this[name] switch { null => null, DBNull => null, string s => s, var o => Convert.ToString(o, CultureInfo.InvariantCulture) };
    public long Long(string name) => this[name] switch { null => 0, DBNull => 0, long l => l, var o => Convert.ToInt64(o, CultureInfo.InvariantCulture) };
    public bool IsNull(string name) => this[name] is null or DBNull;
}

/// <summary>
/// SQLite-Zugriff des Shopfloor. Tabellenschema identisch zu server.py, zd05.py, forecast.py und
/// notify.py, damit die gelieferte Datei data/shopfloor.db unveraendert weiterverwendet wird.
/// </summary>
public sealed class ShopfloorDb
{
    /// <summary>Schreibzugriffe laufen nacheinander (wie WRITE_LOCK in server.py).</summary>
    public object WriteLock { get; } = new();

    public string Path { get; }

    public ShopfloorDb(string path) => Path = path;

    public SqliteConnection Open()
    {
        var con = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path, DefaultTimeout = 15 }.ToString());
        con.Open();
        return con;
    }

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS records (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            module TEXT NOT NULL, nr TEXT NOT NULL,
            data TEXT NOT NULL DEFAULT '{}', meta TEXT NOT NULL DEFAULT '{}',
            version INTEGER NOT NULL DEFAULT 1,
            created_at TEXT, created_by TEXT, updated_at TEXT, updated_by TEXT,
            deleted INTEGER NOT NULL DEFAULT 0,
            UNIQUE(module, nr));
        CREATE INDEX IF NOT EXISTS idx_rec_mod ON records(module, deleted);
        CREATE TABLE IF NOT EXISTS daily (
            module TEXT NOT NULL, day TEXT NOT NULL,
            data TEXT NOT NULL DEFAULT '{}', meta TEXT NOT NULL DEFAULT '{}',
            version INTEGER NOT NULL DEFAULT 1,
            updated_at TEXT, updated_by TEXT,
            PRIMARY KEY(module, day));
        CREATE TABLE IF NOT EXISTS history (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            module TEXT, ref TEXT, ts TEXT, user TEXT, source TEXT, changes TEXT);
        CREATE INDEX IF NOT EXISTS idx_hist ON history(module, ref);
        CREATE TABLE IF NOT EXISTS zd05_rows (day TEXT, material TEXT, data TEXT, updated_at TEXT, updated_by TEXT,
            PRIMARY KEY(day, material));
        CREATE TABLE IF NOT EXISTS zd05_days (day TEXT PRIMARY KEY, imported_at TEXT, imported_by TEXT, source TEXT,
            filename TEXT, kpi TEXT, kpi_fixed INTEGER DEFAULT 0);
        CREATE TABLE IF NOT EXISTS abc_xyz (material TEXT PRIMARY KEY, abc TEXT, xyz TEXT, text TEXT, art TEXT);
        CREATE TABLE IF NOT EXISTS zd05_week (kw TEXT PRIMARY KEY, data TEXT, updated_at TEXT, updated_by TEXT);
        CREATE TABLE IF NOT EXISTS fc_orders (id INTEGER PRIMARY KEY AUTOINCREMENT, day TEXT, dept TEXT, data TEXT, imported_at TEXT, imported_by TEXT);
        CREATE INDEX IF NOT EXISTS idx_fc_day ON fc_orders(day);
        CREATE TABLE IF NOT EXISTS fc_plan (dept TEXT, day TEXT, data TEXT, updated_at TEXT, updated_by TEXT, PRIMARY KEY(dept, day));
        CREATE TABLE IF NOT EXISTS fc_settings (dept TEXT PRIMARY KEY, data TEXT, updated_at TEXT, updated_by TEXT);
        CREATE TABLE IF NOT EXISTS fc_imports (id INTEGER PRIMARY KEY AUTOINCREMENT, ts TEXT, user TEXT, source TEXT, filename TEXT, von TEXT, bis TEXT, anzahl INTEGER);
        CREATE TABLE IF NOT EXISTS people (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, kuerzel TEXT, email TEXT,
            typ TEXT, aliases TEXT, aktiv INTEGER DEFAULT 1, digest INTEGER DEFAULT 0);
        CREATE TABLE IF NOT EXISTS mail_outbox (id INTEGER PRIMARY KEY AUTOINCREMENT, ts TEXT, user TEXT, module TEXT, ref TEXT,
            rcpt TEXT, subject TEXT, html TEXT, text TEXT, status TEXT, error TEXT, tries INTEGER DEFAULT 0, sent_at TEXT, reason TEXT);
        CREATE TABLE IF NOT EXISTS mail_state (k TEXT PRIMARY KEY, v TEXT);
        """;

    /// <summary>Legt die Datei aus dem mitgelieferten Startstand an, falls sie fehlt, und das Schema.</summary>
    public void Initialize(string? seedPath)
    {
        var dir = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        if (!File.Exists(Path) && !string.IsNullOrEmpty(seedPath) && File.Exists(seedPath))
            File.Copy(seedPath, Path);

        lock (WriteLock)
        {
            using var con = Open();
            Exec(con, "PRAGMA journal_mode=WAL");
            Exec(con, Schema);
            ShopfloorMail.SeedPeopleIfEmpty(con);
        }
    }

    /// <summary>Schreibzugriff: eine Sperre, eine Transaktion, bei Fehler Rollback (wie WRITE_LOCK in server.py).</summary>
    public T Write<T>(Func<SqliteConnection, T> work)
    {
        lock (WriteLock)
        {
            using var con = Open();
            Exec(con, "BEGIN IMMEDIATE");
            try
            {
                var result = work(con);
                Exec(con, "COMMIT");
                return result;
            }
            catch
            {
                try { Exec(con, "ROLLBACK"); } catch (SqliteException) { }
                throw;
            }
        }
    }

    // ------------------------------------------------------------ SQL-Helfer
    private static string Numbered(string sql)
    {
        var sb = new StringBuilder(sql.Length + 16);
        var n = 0;
        foreach (var c in sql)
        {
            if (c == '?') sb.Append("$p").Append(n++);
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private static SqliteCommand Command(SqliteConnection con, string sql, object?[] args)
    {
        var cmd = con.CreateCommand();
        cmd.CommandText = args.Length == 0 ? sql : Numbered(sql);
        for (var i = 0; i < args.Length; i++)
            cmd.Parameters.AddWithValue("$p" + i, args[i] ?? DBNull.Value);
        return cmd;
    }

    public static int Exec(SqliteConnection con, string sql, params object?[] args)
    {
        using var cmd = Command(con, sql, args);
        return cmd.ExecuteNonQuery();
    }

    public static long Insert(SqliteConnection con, string sql, params object?[] args)
    {
        Exec(con, sql, args);
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT last_insert_rowid()";
        return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public static List<SfRow> Query(SqliteConnection con, string sql, params object?[] args)
    {
        using var cmd = Command(con, sql, args);
        using var r = cmd.ExecuteReader();
        var rows = new List<SfRow>();
        while (r.Read())
        {
            var d = new Dictionary<string, object?>(r.FieldCount, StringComparer.Ordinal);
            for (var i = 0; i < r.FieldCount; i++)
                d[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            rows.Add(new SfRow(d));
        }
        return rows;
    }

    public static SfRow? QueryOne(SqliteConnection con, string sql, params object?[] args)
        => Query(con, sql, args).FirstOrDefault();

    public static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    // ------------------------------------------------------------ Sicherung
    /// <summary>Tageskopie in den Sicherungsordner, aeltere als keepDays werden geloescht.</summary>
    public void Backup(string backupDir, int keepDays, bool force = false)
    {
        if (!File.Exists(Path))
            return;
        Directory.CreateDirectory(backupDir);
        var target = System.IO.Path.Combine(backupDir, $"shopfloor-{DateTime.Today:yyyy-MM-dd}.db");
        if (File.Exists(target) && !force)
            return;
        if (File.Exists(target))
            File.Delete(target);
        using (var src = Open())
        using (var dst = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = target, Pooling = false }.ToString()))
        {
            dst.Open();
            src.BackupDatabase(dst);
        }
        var limit = DateTime.Today.AddDays(-keepDays);
        foreach (var f in Directory.GetFiles(backupDir, "shopfloor-*.db"))
        {
            var m = Regex.Match(System.IO.Path.GetFileName(f), @"^shopfloor-(\d{4}-\d{2}-\d{2})\.db$");
            if (m.Success && DateTime.TryParseExact(m.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && d < limit)
                File.Delete(f);
        }
    }
}
