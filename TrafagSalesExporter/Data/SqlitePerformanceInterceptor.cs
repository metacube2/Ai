using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TrafagSalesExporter.Data;

/// <summary>
/// Setzt beim Oeffnen jeder SQLite-Verbindung einen groesseren Seitencache und Memory-Mapping.
///
/// ANLASS 2026-09-28: Der Server brauchte fuer die Einkaufsansicht 98,6 bis 104,7 Sekunden, der
/// Entwicklungsrechner fuer dieselben 55 Abfragen gegen eine Kopie derselben Datenbank 12 Sekunden.
/// SQLite haelt je Verbindung standardmaessig nur rund 2 MB Seiten im Speicher, die Datenbank hat
/// 470 MB. Jede Abfrage liest deshalb von neuem aus der Datei; auf dem Entwicklungsrechner liegt die
/// Datei im Windows-Dateicache, auf der Server-VM offenbar nicht. Ob das die ganze Erklaerung ist,
/// zeigt die Vorwaermzeit im Serverprotokoll („Einkauf-Standardansicht vorberechnet in ...").
///
/// Gesetzt wird ueber <see cref="DbConnection.StateChange"/>, nicht ueber ConnectionOpened: viele
/// Dienste oeffnen die Verbindung selbst (<c>Database.GetDbConnection().OpenAsync()</c>), dabei
/// feuern die Opened-Interceptoren von EF nicht. StateChange feuert bei jedem Oeffnen, auch wenn die
/// Verbindung aus dem Pool kommt; die drei PRAGMAs kosten dann Mikrosekunden.
/// </summary>
public sealed class SqlitePerformanceInterceptor : DbConnectionInterceptor
{
    /// <summary>Seitencache je Verbindung in KiB (negativ = KiB statt Seiten): 64 MB.</summary>
    internal const int CacheSizeKiB = 65536;

    /// <summary>Memory-Mapping der Datei: 256 MB, geteilt ueber den Dateicache des Betriebssystems.</summary>
    internal const long MmapSizeBytes = 268_435_456;

    public override DbConnection ConnectionCreated(ConnectionCreatedEventData eventData, DbConnection result)
    {
        result.StateChange += OnStateChange;
        return result;
    }

    private static void OnStateChange(object sender, StateChangeEventArgs e)
    {
        if (e.CurrentState != ConnectionState.Open || sender is not SqliteConnection connection)
            return;

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                $"PRAGMA cache_size = -{CacheSizeKiB}; PRAGMA mmap_size = {MmapSizeBytes}; PRAGMA temp_store = MEMORY;";
            command.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Nur eine Beschleunigung: scheitert sie, arbeitet die Verbindung mit den Standardwerten.
        }
    }
}
