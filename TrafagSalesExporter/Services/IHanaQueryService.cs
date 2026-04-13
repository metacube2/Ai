using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IHanaQueryService
{
    List<SalesRecord> GetSalesRecords(HanaServer server, string schema, string tsc, string land, string dateFilter);
    ConnectionTestResult TestConnectionDetailed(HanaServer server);
    void TestConnection(HanaServer server);
}
