// Statuszeilen-Programm fuer Claude Code (settings.json › statusLine).
// Liest das dokumentierte Statuszeilen-JSON von stdin, gibt es an den Agenten-Monitor weiter
// (POST 127.0.0.1:47811/status, kurzer Timeout, Fehler werden ignoriert) und schreibt eine kurze Zeile,
// z. B. „Fable 5.2 · 5h 34 % · Woche 12 % · Kontext 45 %“. Die Zeile darf nie leer sein, sonst blendet
// Claude Code sie aus.
using System.Globalization;
using System.Text;
using AgentMonitor.Core;

Console.OutputEncoding = new UTF8Encoding(false);
string input;
using (var stdin = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false)))
    input = await stdin.ReadToEndAsync();

var port = int.TryParse(Environment.GetEnvironmentVariable("AGENTMONITOR_PORT"), out var p) ? p : 47811;
var send = Task.Run(async () =>
{
    try
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(400) };
        using var content = new StringContent(input, Encoding.UTF8, "application/json");
        using var _ = await http.PostAsync($"http://127.0.0.1:{port}/status", content);
    }
    catch (Exception)
    {
        // Monitor laeuft nicht: Statuszeile trotzdem ausgeben.
    }
});

var info = StatusInfo.Parse(input);
var parts = new List<string>();
if (info is not null)
{
    if (StatusInfo.ShortModel(info.ModelId, info.ModelName) is { } m) parts.Add(m);
    if (info.FiveHour is { } f) parts.Add($"5h {f.UsedPercent.ToString("0", CultureInfo.InvariantCulture)} %");
    if (info.SevenDay is { } w) parts.Add($"Woche {w.UsedPercent.ToString("0", CultureInfo.InvariantCulture)} %");
    if (info.ContextPercent is { } c) parts.Add($"Kontext {c.ToString("0", CultureInfo.InvariantCulture)} %");
}
Console.WriteLine(parts.Count > 0 ? string.Join(" · ", parts) : "Agenten-Monitor");

await send;
