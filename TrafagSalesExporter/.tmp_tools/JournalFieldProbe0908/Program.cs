using Microsoft.EntityFrameworkCore;
using Sap.Data.Hana;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services.DataSources;
using System.Globalization;

// One read-only connection per site. No retry, no credentials in output, no app/log writes.
var dbPath = args[0];
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={dbPath};Mode=ReadOnly;Default Timeout=30").Options;
await using var db = new AppDbContext(options);
var definitions = await db.SourceSystemDefinitions.AsNoTracking().ToListAsync();
var sites = await db.Sites.AsNoTracking().Where(s => new[] { "TRFR", "TRIT", "TRUS", "TRIN" }.Contains(s.TSC)).ToListAsync();
foreach (var site in sites)
{
    Console.WriteLine($"SITE {site.TSC}");
    var server = await HanaServerResolver.BuildEffectiveServerAsync(db, site, definitions.Single(d => d.Code == site.SourceSystem));
    try
    {
        // Reachability without authentication, bounded independently of HANA's native timeouts.
        using var tcp = new System.Net.Sockets.TcpClient();
        await tcp.ConnectAsync(server.Host, server.Port).WaitAsync(TimeSpan.FromSeconds(8));
        using var connection = new HanaConnection(server.BuildConnectionString());
        await connection.OpenAsync();
        var schema = site.Schema.Trim().ToUpperInvariant();
        if (schema.Any(c => !(char.IsLetterOrDigit(c) || c == '_'))) throw new InvalidOperationException("Invalid schema");
        var columns = new Dictionary<string, List<string>>();
        using (var command = new HanaCommand("SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE_NAME FROM SYS.TABLE_COLUMNS WHERE SCHEMA_NAME = :schema AND TABLE_NAME IN ('OJDT','JDT1','OITR','ITR1','ORCT','OVPM','ODIM','OACT') ORDER BY TABLE_NAME, POSITION", connection))
        {
            command.Parameters.Add(new HanaParameter("schema", HanaDbType.NVarChar) { Value = schema });
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var table = reader.GetString(0); var col = reader.GetString(1);
                if (!columns.ContainsKey(table)) columns[table] = new();
                columns[table].Add(col);
                if (new[] { "date", "paid", "recon", "profit", "ocr", "baldu", "dim", "cancel", "shortname", "intrn", "extrn", "father", "export" }.Any(x => col.Contains(x, StringComparison.OrdinalIgnoreCase)))
                    Console.WriteLine($"COLUMN {table}.{col} {reader.GetString(2)}");
            }
        }
        foreach (var table in new[] { "JDT1", "OJDT", "OITR", "ORCT", "OVPM", "OACT" })
        {
            if (!columns.TryGetValue(table, out var cols)) continue;
            var selected = cols.Where(c => new[] { "date", "paid", "profit", "ocr", "recondate", "export" }.Any(x => c.Contains(x, StringComparison.OrdinalIgnoreCase))).ToList();
            var select = "COUNT(*) AS TOTAL" + string.Concat(selected.Select(c => $", SUM(CASE WHEN NULLIF(TRIM(TO_NVARCHAR(\"{c}\")), '') IS NOT NULL THEN 1 ELSE 0 END) AS \"{c}\""));
            var range = table == "JDT1" ? $" j INNER JOIN \"{schema}\".\"OJDT\" h ON j.\"TransId\"=h.\"TransId\" WHERE h.\"RefDate\">=DATE'2025-01-01'" : cols.Contains("RefDate") ? " WHERE \"RefDate\">=DATE'2025-01-01'" : "";
            if (table == "JDT1") select = "COUNT(*) AS TOTAL" + string.Concat(selected.Select(c => $", SUM(CASE WHEN NULLIF(TRIM(TO_NVARCHAR(j.\"{c}\")), '') IS NOT NULL THEN 1 ELSE 0 END) AS \"{c}\""));
            await Print(connection, $"SELECT {select} FROM \"{schema}\".\"{table}\"{range}", table);
        }
        if (columns.TryGetValue("ODIM", out var dims))
            await Print(connection, $"SELECT {string.Join(',', dims.Select(c => $"\"{c}\""))} FROM \"{schema}\".\"ODIM\"", "DIMENSIONS");
        if (args.Length > 1)
        {
            var sql = (await File.ReadAllTextAsync(args[1])).Replace("{schema}", schema);
            foreach (var statement in sql.Split("\n;;", StringSplitOptions.RemoveEmptyEntries))
            {
                if (!statement.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("SELECT required");
                await Print(connection, statement, "EXTRA");
            }
        }
    }
    catch (Exception ex) { Console.WriteLine($"UNAVAILABLE {ex.GetType().Name}" + (ex is HanaException h ? $" code={h.ErrorCode}" : "")); }
}

static async Task Print(HanaConnection connection, string sql, string label)
{
    using var cmd = new HanaCommand(sql, connection) { CommandTimeout = 60 };
    using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine(label + " | " + string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(reader.GetName)));
    int n=0;
    while (await reader.ReadAsync() && n++ < 20)
        Console.WriteLine(string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture))));
}
