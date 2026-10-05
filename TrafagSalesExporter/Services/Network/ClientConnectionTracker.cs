using System.Net;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Zaehlt Verbindungsabbrueche der Browser-Verbindung (Blazor-Circuit) je Netzbereich, damit sich
/// schlechte WLAN-Zonen zeigen (Reiter Netzwerk > Arbeitsplaetze). Als Abbruch ("drop") zaehlt nur ein
/// Verbindungsverlust, auf den innerhalb weniger Minuten die Wiederverbindung desselben Circuits folgt;
/// ein geschlossener Tab trennt ebenfalls und waere sonst jedes Mal ein Abbruch. Gespeichert wird nur der
/// /24-Bereich (IPv4) bzw. "IPv6"/"lokal", nie die volle IP und nie der Benutzer.
///
/// Die Client-IP ist nur beim Oeffnen des Circuits ueber IHttpContextAccessor erreichbar; das ist in
/// .NET 8 nicht garantiert. Fehlt sie, wird "unbekannt" gezaehlt (nach dem Deploy nachmessen).
/// Wirft nie: ein Fehler hier darf keine Seite stoeren.
/// </summary>
public sealed class ClientConnectionTracker : CircuitHandler
{
    private readonly NetworkStore _store;
    private readonly IHttpContextAccessor _http;
    private string _subnet = "unbekannt";
    private bool _firstUp = true;
    private DateTime? _downAt;

    /// <summary>Laengste Pause zwischen Trennung und Wiederverbindung, die noch als Abbruch zaehlt.</summary>
    internal static readonly TimeSpan ReconnectWindow = TimeSpan.FromMinutes(5);

    public ClientConnectionTracker(NetworkStore store, IHttpContextAccessor http)
    {
        _store = store;
        _http = http;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _subnet = SubnetOf(_http.HttpContext?.Connection.RemoteIpAddress);
        return SafeSaveAsync("open");
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Noch nichts speichern: ob es ein Abbruch war, zeigt erst die Wiederverbindung.
        _downAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Das erste "up" ist der normale Aufbau, erst danach ist es eine Wiederverbindung.
        if (_firstUp)
        {
            _firstUp = false;
            return Task.CompletedTask;
        }
        return SaveReconnectAsync();
    }

    private async Task SaveReconnectAsync()
    {
        var now = DateTime.UtcNow;
        var down = _downAt;
        _downAt = null;
        if (down is { } d && now - d <= ReconnectWindow)
            await SafeSaveAsync("drop", d);
        await SafeSaveAsync("up", now);
    }

    internal static string SubnetOf(IPAddress? address)
    {
        if (address is null)
            return "unbekannt";
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address))
            return "lokal";
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return "IPv6";
        var b = address.GetAddressBytes();
        return $"{b[0]}.{b[1]}.{b[2]}.0/24";
    }

    private Task SafeSaveAsync(string eventType) => SafeSaveAsync(eventType, DateTime.UtcNow);

    private async Task SafeSaveAsync(string eventType, DateTime atUtc)
    {
        try
        {
            await _store.SaveClientEventAsync(new NetworkClientEvent(atUtc, _subnet, eventType), CancellationToken.None);
        }
        catch
        {
            // bewusst geschluckt
        }
    }
}
