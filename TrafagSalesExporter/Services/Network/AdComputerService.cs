using System.DirectoryServices;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Services;

public sealed record AdComputer(string Name, string OperatingSystem, DateTime? LastLogonUtc, DateTime? CreatedUtc, bool Enabled);

public sealed class AdComputerResult
{
    public bool Enabled { get; init; }
    public string? Error { get; init; }
    public DateTime? ReadAt { get; init; }
    public IReadOnlyList<AdComputer> Computers { get; init; } = [];
    public IEnumerable<AdComputer> Stale(int days) => Computers.Where(c => c.Enabled && (c.LastLogonUtc is null || c.LastLogonUtc < DateTime.UtcNow.AddDays(-days)));
}

/// <summary>
/// Liest Computerkonten aus dem AD, nur lesend und OHNE Personen (Entscheid Ingo 2026-10-02:
/// „nur AD-Computer ohne Personen“, vorher mit der IT klaeren). Standardmaessig AUS:
/// <c>NetworkProbe:AdEnabled</c> muss auf true stehen. Ergebnis eine Stunde gemerkt.
/// </summary>
public sealed class AdComputerService
{
    private readonly IOptionsMonitor<NetworkProbeOptions> _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AdComputerResult? _cache;

    public AdComputerService(IOptionsMonitor<NetworkProbeOptions> options)
    {
        _options = options;
    }

    public async Task<AdComputerResult> GetAsync()
    {
        if (!_options.CurrentValue.AdEnabled)
            return new AdComputerResult { Enabled = false };
        if (!OperatingSystem.IsWindows())
            return new AdComputerResult { Enabled = true, Error = "Nur auf Windows-Servern moeglich." };

        await _gate.WaitAsync();
        try
        {
            if (_cache is { ReadAt: { } at } && at > DateTime.Now.AddHours(-1))
                return _cache;
            _cache = await Task.Run(Read);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static AdComputerResult Read()
    {
        try
        {
            using var root = new DirectoryEntry();
            using var searcher = new DirectorySearcher(root, "(objectCategory=computer)",
                ["name", "operatingSystem", "lastLogonTimestamp", "whenCreated", "userAccountControl"])
            {
                PageSize = 500,
                SizeLimit = 20000
            };
            var list = new List<AdComputer>();
            using var results = searcher.FindAll();
            foreach (SearchResult r in results)
            {
                string Text(string p) => r.Properties[p].Count > 0 ? Convert.ToString(r.Properties[p][0]) ?? "" : "";
                DateTime? lastLogon = r.Properties["lastLogonTimestamp"].Count > 0 && r.Properties["lastLogonTimestamp"][0] is long ft && ft > 0
                    ? DateTime.FromFileTimeUtc(ft) : null;
                DateTime? created = r.Properties["whenCreated"].Count > 0 && r.Properties["whenCreated"][0] is DateTime c ? c.ToUniversalTime() : null;
                var uac = r.Properties["userAccountControl"].Count > 0 ? Convert.ToInt32(r.Properties["userAccountControl"][0]) : 0;
                list.Add(new AdComputer(Text("name"), Text("operatingSystem"), lastLogon, created, (uac & 0x2) == 0));
            }
            return new AdComputerResult { Enabled = true, ReadAt = DateTime.Now, Computers = list.OrderBy(x => x.Name).ToList() };
        }
        catch (Exception ex)
        {
            return new AdComputerResult { Enabled = true, ReadAt = DateTime.Now, Error = ex.Message };
        }
    }
}
