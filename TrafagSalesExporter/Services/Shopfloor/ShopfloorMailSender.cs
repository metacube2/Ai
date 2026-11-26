using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Mailversand aus dem Shopfloor (Fabio 26.11.2026 Punkt 11, Entscheid Ingo: bauen, Schalter aus). appsettings
/// <c>Shopfloor:Mail</c>. Solange <see cref="Modus"/> nicht „smtp“ ist oder Host, Absender oder <see cref="SendenAb"/> fehlen,
/// wird nichts versendet und der Postausgang bleibt wie bisher ein Protokoll.
/// Passwort nie ins Repository: nur auf dem Server, z. B. Umgebungsvariable <c>Shopfloor__Mail__Passwort</c> im App-Pool.
/// </summary>
public sealed class ShopfloorMailOptions
{
    /// <summary>„aus“ (Standard) oder „smtp“.</summary>
    public string Modus { get; set; } = "aus";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    /// <summary>STARTTLS/SSL (Exchange Online Port 587: true; interner Relay meist false).</summary>
    public bool Ssl { get; set; }
    /// <summary>Leer = ohne Anmeldung (interner Relay).</summary>
    public string Benutzer { get; set; } = string.Empty;
    public string Passwort { get; set; } = string.Empty;
    /// <summary>Absenderadresse, z. B. shopfloor@trafag.com (Vorschlag Fabio, legt ICT an).</summary>
    public string Absender { get; set; } = string.Empty;
    public string AbsenderName { get; set; } = "PPA Shop-Floor";
    /// <summary>
    /// Nur Mails, die ab diesem Zeitpunkt („JJJJ-MM-TT HH:MM“) in den Postausgang kamen, werden versendet. Aeltere „wartend“-Mails
    /// (Protokoll seit Oktober 2026) werden beim ersten Lauf als „nicht gesendet“ markiert und nie nachtraeglich verschickt.
    /// </summary>
    public string SendenAb { get; set; } = string.Empty;
    public int IntervallSekunden { get; set; } = 60;
    public int MaxVersuche { get; set; } = 3;

    public DateTime? SendenAbZeit()
        => DateTime.TryParseExact(SendenAb?.Trim(), ["yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"], CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d) ? d : null;

    public bool Ein => string.Equals(Modus?.Trim(), "smtp", StringComparison.OrdinalIgnoreCase);

    /// <summary>Was fehlt, damit versendet wird (leer = bereit).</summary>
    public List<string> Fehlt()
    {
        var f = new List<string>();
        if (!Ein) f.Add("Modus nicht „smtp“");
        if (string.IsNullOrWhiteSpace(Host)) f.Add("Mailserver");
        if (string.IsNullOrWhiteSpace(Absender)) f.Add("Absender");
        if (SendenAbZeit() is null) f.Add("SendenAb");
        return f;
    }

    public bool Bereit => Fehlt().Count == 0;
}

public static class ShopfloorMailVersand
{
    public const string NichtGesendet = "nicht gesendet";

    /// <summary>
    /// Ein Durchlauf: alte Protokollmails markieren, dann die wartenden Mails ab <c>SendenAb</c> versenden. <paramref name="send"/>
    /// verschickt eine Mail (im Betrieb SMTP, im Test eine Liste). Liefert (gesendet, fehlgeschlagen, alt markiert).
    /// </summary>
    /// <remarks>Der Versand selbst laeuft ausserhalb der Schreibsperre der Datenbank (ein langsamer Mailserver blockiert das Board nicht).</remarks>
    public static (int Gesendet, int Fehler, int Alt) Lauf(ShopfloorDb db, ShopfloorMailOptions o, Action<MailMessage> send)
    {
        if (!o.Bereit) return (0, 0, 0);
        var ab = o.SendenAbZeit()!.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var (alt, offen) = db.Write(con =>
        {
            var n = ShopfloorDb.Exec(con,
                "UPDATE mail_outbox SET status=?, error='vor dem Einschalten des Versands protokolliert' WHERE status='wartend' AND ts<?",
                NichtGesendet, ab);
            var rows = ShopfloorDb.Query(con,
                "SELECT id, rcpt, subject, html, text, tries FROM mail_outbox WHERE status='wartend' AND ts>=? ORDER BY id LIMIT 50", ab);
            return (n, rows);
        });
        int ok = 0, err = 0;
        foreach (var r in offen)
        {
            var id = r.Long("id");
            var to = SfJson.ParseArray(r.Str("rcpt")).OfType<JsonObject>()
                .Select(x => (Name: SfJson.Str(x["name"]), Email: SfJson.Str(x["email"]).Trim()))
                .Where(x => x.Email.Contains('@')).ToList();
            if (to.Count == 0)
            {
                db.Write(con => ShopfloorDb.Exec(con, "UPDATE mail_outbox SET status='keine Adresse' WHERE id=?", id));
                continue;
            }
            try
            {
                using var m = new MailMessage { From = new MailAddress(o.Absender.Trim(), o.AbsenderName), Subject = r.Str("subject") ?? "[Shop-Floor]" };
                foreach (var (name, email) in to) m.To.Add(new MailAddress(email, name));
                m.Body = r.Str("html") ?? r.Str("text") ?? string.Empty;
                m.IsBodyHtml = r.Str("html") is not null;
                if (m.IsBodyHtml && r.Str("text") is { } text)
                    m.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, "text/plain"));
                send(m);
                db.Write(con => ShopfloorDb.Exec(con,
                    "UPDATE mail_outbox SET status='gesendet', sent_at=?, error=NULL, tries=tries+1 WHERE id=?", ShopfloorDb.Now(), id));
                ok++;
            }
            catch (Exception ex)
            {
                var tries = r.Long("tries") + 1;
                var msg = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                db.Write(con => ShopfloorDb.Exec(con, "UPDATE mail_outbox SET status=?, error=?, tries=? WHERE id=?",
                    tries >= o.MaxVersuche ? "Fehler" : "wartend", msg, tries, id));
                err++;
            }
        }
        return (ok, err, alt);
    }

    public static Action<MailMessage> Smtp(ShopfloorMailOptions o) => m =>
    {
        using var c = new SmtpClient(o.Host.Trim(), o.Port) { EnableSsl = o.Ssl, DeliveryMethod = SmtpDeliveryMethod.Network, Timeout = 30000 };
        if (!string.IsNullOrWhiteSpace(o.Benutzer))
            c.Credentials = new NetworkCredential(o.Benutzer.Trim(), o.Passwort);
        else
            c.UseDefaultCredentials = false;
        c.Send(m);
    };

    /// <summary>Versandstatus fuer die Seite „Personen &amp; Mail“ (ohne Passwort).</summary>
    public static JsonObject Status(ShopfloorMailOptions o, string baseUrl, SqliteConnection con)
    {
        var fehlt = new JsonArray();
        foreach (var f in o.Fehlt()) fehlt.Add(f);
        return new JsonObject
        {
            ["modus"] = o.Ein ? "smtp" : "aus",
            ["absender"] = o.Absender ?? string.Empty,
            ["server"] = string.IsNullOrWhiteSpace(o.Host) ? string.Empty : $"{o.Host.Trim()}:{o.Port}",
            ["senden_ab"] = o.SendenAb ?? string.Empty,
            ["base_url"] = baseUrl,
            ["bereit"] = o.Bereit,
            ["fehlt"] = fehlt,
            ["wartend"] = ShopfloorDb.QueryOne(con, "SELECT count(*) n FROM mail_outbox WHERE status='wartend'")?.Long("n") ?? 0
        };
    }
}

/// <summary>Versendet den Postausgang im Hintergrund, sobald <c>Shopfloor:Mail</c> bereit ist (Standard: aus).</summary>
public sealed class ShopfloorMailSender : BackgroundService
{
    private readonly ShopfloorStoreProvider _provider;
    private readonly IOptionsMonitor<ShopfloorOptions> _options;
    private readonly ILogger<ShopfloorMailSender> _logger;

    public ShopfloorMailSender(ShopfloorStoreProvider provider, IOptionsMonitor<ShopfloorOptions> options, ILogger<ShopfloorMailSender> logger)
    {
        _provider = provider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken).ConfigureAwait(false);
        while (!stoppingToken.IsCancellationRequested)
        {
            var o = _options.CurrentValue.Mail;
            try
            {
                if (o.Bereit)
                {
                    var (ok, err, alt) = ShopfloorMailVersand.Lauf(_provider.Store.Db, o, ShopfloorMailVersand.Smtp(o));
                    if (ok + err + alt > 0)
                        _logger.LogInformation("Shopfloor-Mail: {Ok} gesendet, {Err} Fehler, {Alt} alte Protokollmails nicht versendet.", ok, err, alt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Shopfloor-Mailversand fehlgeschlagen.");
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(15, o.IntervallSekunden)), stoppingToken).ConfigureAwait(false);
        }
    }
}
