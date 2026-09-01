using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// ISS-003.4: Materialien mit einem aktiven, nicht geloeschten echten externen Einkaufsbeleg
/// aus dem bereits geladenen Einkauf-Cache (<c>PurchasingEkpoCache</c>/<c>PurchasingEkkoCache</c>).
///
/// Grundlage ist ausschliesslich die vorhandene Einkaufsdaten-Ladung fuer das Einkauf-Dashboard,
/// kein zusaetzlicher SAP-Zugriff. "Aktiv" heisst <c>EKPO.Loekz</c> leer; "echt extern" heisst,
/// der Lieferant (<c>EKKO.Lifnr</c>/<c>SupplierName</c>) traegt keinen der bekannten
/// Trafag/GFS-Marker aus <see cref="GroupMarginSupplierClassifier"/> - dieselbe Regel wie auf
/// der Verkaufsseite, damit ein interner Bestelltransfer nicht faelschlich als Fremdbezug zaehlt.
///
/// Siehe docs/FINANCE_SUPPLIER.md Abschnitt 6: die einzige offene Fachentscheidung ist, ob ein
/// solcher Beleg die CH/AT-Herstellerregel durchbricht - die TSC-Regel, die jede TRCH/TRAT-
/// Verkaufszeile unbedingt als "Intern / TR_AG" klassifiziert, unabhaengig von den
/// Supplier-Feldern (siehe <see cref="GroupMarginSupplierClassifier.Resolve"/>).
/// </summary>
public static class ForeignProcurementEvidenceStore
{
    public static async Task<IReadOnlySet<string>> LoadMaterialKeysAsync(
        AppDbContext db, CancellationToken cancellationToken = default)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var result = new HashSet<string>(StringComparer.Ordinal);

        // Beide Tabellen entstehen ueber das rohe Schema-SQL, nicht ueber EF-Migrationen; ein
        // Testkontext, der nur EnsureCreated() aufruft, kennt sie nicht. Produktiv legt die
        // Schema-Initialisierung sie immer an (leer, bis der erste Einkauf-Lauf sie fuellt) -
        // hier fehlen sie also nur in Tests oder vor dem allerersten Schema-Setup.
        await using (var probe = connection.CreateCommand())
        {
            probe.CommandText =
                "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' " +
                "AND name IN ('PurchasingEkpoCache', 'PurchasingEkkoCache');";
            var tableCount = Convert.ToInt32(await probe.ExecuteScalarAsync(cancellationToken));
            if (tableCount < 2)
                return result;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT DISTINCT p.Matnr, k.Lifnr, k.SupplierName
FROM PurchasingEkpoCache p
JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE p.Loekz = '' AND p.Matnr <> '' AND k.Lifnr <> '';";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var matnr = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var lifnr = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var supplierName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

            // Ein interner Bestelltransfer (Lieferant traegt einen Trafag/GFS-Marker) ist kein
            // Fremdbezugsindiz - dieselbe Regel wie bei der Verkaufs-Klassifikation.
            if (GroupMarginSupplierClassifier.MatchesInternalSupplierMarker(lifnr, supplierName, null))
                continue;

            var key = MaterialKeyNormalizer.Normalize(matnr);
            if (key.Length > 0)
                result.Add(key);
        }

        return result;
    }

    public static IReadOnlySet<string> LoadMaterialKeys(AppDbContext db)
        => LoadMaterialKeysAsync(db).GetAwaiter().GetResult();
}
