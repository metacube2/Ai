using System.Net.Http.Headers;
using System.Text;
using Microsoft.Data.Sqlite;

var conn = new SqliteConnection(@"Data Source=C:\Users\koi\source\repos\Ai\TrafagSalesExporter\trafag_exporter.db");
await conn.OpenAsync();
string sapUsername = "", sapPassword = "";
var cmd = conn.CreateCommand();
cmd.CommandText = "select SapUsername, SapPassword from ExportSettings limit 1";
using (var r = await cmd.ExecuteReaderAsync())
{
    if (await r.ReadAsync())
    {
        sapUsername = r.IsDBNull(0) ? "" : r.GetString(0);
        sapPassword = r.IsDBNull(1) ? "" : r.GetString(1);
    }
}
if (string.IsNullOrWhiteSpace(sapUsername) || string.IsNullOrWhiteSpace(sapPassword)) throw new Exception("Central SAP credentials missing");
var serviceUrl = @"http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/";
using var client = new HttpClient();
client.Timeout = TimeSpan.FromSeconds(20);
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{sapUsername}:{sapPassword}")));
foreach (var url in new[]{ serviceUrl, serviceUrl + "" })
{
    Console.WriteLine($"URL|{url}");
    using var response = await client.GetAsync(url);
    Console.WriteLine($"STATUS|{(int)response.StatusCode}|{response.ReasonPhrase}");
    foreach (var header in response.Headers)
        Console.WriteLine($"HEADER|{header.Key}|{string.Join(",", header.Value)}");
    foreach (var header in response.Content.Headers)
        Console.WriteLine($"HEADER|{header.Key}|{string.Join(",", header.Value)}");
    var body = await response.Content.ReadAsStringAsync();
    Console.WriteLine("BODY_START");
    Console.WriteLine(body.Length > 5000 ? body[..5000] : body);
    Console.WriteLine("BODY_END");
}
