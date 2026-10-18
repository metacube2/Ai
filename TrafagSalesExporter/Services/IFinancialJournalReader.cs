using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Liest Hauptbuch-Buchungszeilen (Journal) aus einem Quellsystem.
/// Aktuell nur SAP B1 ueber HANA (OJDT/JDT1); weitere ERP-Systeme folgen
/// als eigene Implementierungen.
/// </summary>
public interface IFinancialJournalReader
{
    Task<List<FinancialJournalEntry>> GetJournalEntriesAsync(
        HanaServer server,
        string schema,
        string tsc,
        string land,
        string sourceSystem,
        string dateFilter,
        CancellationToken cancellationToken = default);
}
