using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Modules.Lieferantenbewertung;

public sealed record LbBenutzer(string Login, bool IstAdmin);

/// <summary>Daten der Lieferantenbewertung lesen und pflegen. Lesen darf jeder mit Zugang zum Einkauf, schreiben nur
/// <see cref="LbOptions.EditUsers"/> und entsperrte Admins; die Pruefung steht hier, nicht nur in der Oberflaeche.</summary>
public sealed class LbService(IDbContextFactory<LbDbContext> dbFactory, IOptionsMonitor<LbOptions> options, TimeProvider zeit)
{
    private readonly SemaphoreSlim _schemaSperre = new(1, 1);
    private volatile bool _schemaBereit;

    public bool Aktiv => options.CurrentValue.Enabled;

    public bool DarfBearbeiten(LbBenutzer b)
        => b.IstAdmin || (!string.IsNullOrWhiteSpace(b.Login)
            && options.CurrentValue.EditUsers.Any(u => string.Equals(u.Trim(), b.Login.Trim(), StringComparison.OrdinalIgnoreCase)));

    public DateOnly Heute => DateOnly.FromDateTime(zeit.GetLocalNow().DateTime);

    private async Task<LbDbContext> DbAsync()
    {
        var db = await dbFactory.CreateDbContextAsync();
        if (!_schemaBereit)
        {
            await _schemaSperre.WaitAsync();
            try
            {
                if (!_schemaBereit)
                {
                    var pfad = db.Database.GetDbConnection().DataSource;
                    if (!string.IsNullOrWhiteSpace(pfad) && Path.GetDirectoryName(pfad) is { Length: > 0 } ordner)
                        Directory.CreateDirectory(ordner);
                    await db.Database.EnsureCreatedAsync();
                    _schemaBereit = true;
                }
            }
            finally
            {
                _schemaSperre.Release();
            }
        }
        return db;
    }

    private void Pruefe(LbBenutzer b)
    {
        if (!DarfBearbeiten(b))
            throw new UnauthorizedAccessException("Keine Berechtigung zum Bearbeiten der Lieferantenbewertung.");
    }

    private static string Name(string? name) => (name ?? "").Trim();

    // ---------- Lesen ----------

    public async Task<LbDaten> LadeAsync()
    {
        await using var db = await DbAsync();
        return new LbDaten(
            await db.Bewertungen.AsNoTracking().Include(x => x.Zeilen).ToListAsync(),
            await db.Zertifikate.AsNoTracking().ToListAsync(),
            await db.Massnahmen.AsNoTracking().ToListAsync(),
            await db.Termine.AsNoTracking().ToListAsync(),
            await db.Dokumente.AsNoTracking().ToListAsync());
    }

    /// <summary>Eine Zeile je Lieferant, der irgendeinen Eintrag hat.</summary>
    public static List<LbUebersichtZeile> Uebersicht(LbDaten d, DateOnly heute)
    {
        var namen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Merke(string nr, string name)
        {
            if (!namen.TryGetValue(nr, out var alt) || string.IsNullOrWhiteSpace(alt))
                namen[nr] = name;
        }
        foreach (var b in d.Bewertungen.OrderByDescending(x => x.ErfasstAm)) Merke(b.LieferantNr, b.LieferantName);
        foreach (var z in d.Zertifikate) Merke(z.LieferantNr, z.LieferantName);
        foreach (var m in d.Massnahmen) Merke(m.LieferantNr, m.LieferantName);
        foreach (var t in d.Termine) Merke(t.LieferantNr, t.LieferantName);
        foreach (var o in d.Dokumente) Merke(o.LieferantNr, o.LieferantName);

        return namen.Select(kv =>
            {
                var bew = d.Bewertungen.Where(x => x.LieferantNr == kv.Key).ToList();
                var naechster = d.Termine
                    .Where(x => x.LieferantNr == kv.Key && x.Status == "Geplant" && x.Datum is not null)
                    .OrderBy(x => x.Datum).FirstOrDefault();
                return new LbUebersichtZeile(
                    kv.Key,
                    kv.Value,
                    LbKatalog.Juengste(bew),
                    bew.Count,
                    LbKatalog.SchlechtesteAmpel(d.Zertifikate.Where(x => x.LieferantNr == kv.Key), heute),
                    d.Massnahmen.Count(x => x.LieferantNr == kv.Key && x.Status != "Erledigt"),
                    naechster);
            })
            .OrderBy(x => x.LieferantName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // ---------- Schreiben ----------

    /// <summary>Neue Bewertung; aeltere bleiben als Verlauf erhalten.</summary>
    public async Task<int> NeueBewertungAsync(LbBenutzer b, string lieferantNr, string lieferantName, DateOnly datum, string verantwortlich,
        IEnumerable<LbBewertungZeile> zeilen)
    {
        Pruefe(b);
        var nr = LbKatalog.NormalisiereNr(lieferantNr);
        if (string.IsNullOrWhiteSpace(lieferantNr))
            throw new ArgumentException("Lieferant fehlt.");
        var liste = zeilen.Select(z => new LbBewertungZeile
        {
            Themenfeld = z.Themenfeld,
            Note = z.Note is 1 or 2 or 4 or 5 ? z.Note : null,
            Feststellung = (z.Feststellung ?? "").Trim(),
        }).ToList();
        if (liste.All(z => z.Note is null && z.Feststellung.Length == 0))
            throw new ArgumentException("Mindestens ein Themenfeld bewerten oder eine Feststellung eintragen.");

        await using var db = await DbAsync();
        var bew = new LbBewertung
        {
            LieferantNr = nr,
            LieferantName = Name(lieferantName),
            Datum = datum,
            Verantwortlich = Name(verantwortlich),
            ErfasstVon = b.Login,
            ErfasstAm = zeit.GetLocalNow().DateTime,
            Zeilen = liste,
        };
        db.Bewertungen.Add(bew);
        await db.SaveChangesAsync();
        return bew.Id;
    }

    /// <summary>Neu anlegen (Id 0) oder aendern. Pflichtfeld je Art: Zertifikat Typ, Massnahme Text, Termin Art, Dokument Link oder Beschreibung.</summary>
    public async Task SpeichereAsync<T>(LbBenutzer b, T eintrag) where T : class, ILbEintrag
    {
        Pruefe(b);
        if (string.IsNullOrWhiteSpace(eintrag.LieferantNr))
            throw new ArgumentException("Lieferant fehlt.");
        var fehlt = eintrag switch
        {
            LbZertifikat z when string.IsNullOrWhiteSpace(z.Typ) => "Typ fehlt.",
            LbMassnahme m when string.IsNullOrWhiteSpace(m.Massnahme) => "Massnahme fehlt.",
            LbTermin t when string.IsNullOrWhiteSpace(t.Art) => "Art fehlt.",
            LbDokument o when string.IsNullOrWhiteSpace(o.Link) && string.IsNullOrWhiteSpace(o.Beschreibung) => "Link oder Beschreibung fehlt.",
            _ => null,
        };
        if (fehlt is not null)
            throw new ArgumentException(fehlt);

        eintrag.LieferantNr = LbKatalog.NormalisiereNr(eintrag.LieferantNr);
        eintrag.LieferantName = Name(eintrag.LieferantName);
        eintrag.GeaendertVon = b.Login;
        eintrag.GeaendertAm = zeit.GetLocalNow().DateTime;
        await using var db = await DbAsync();
        if (eintrag.Id == 0)
            db.Set<T>().Add(eintrag);
        else
            db.Set<T>().Update(eintrag);
        await db.SaveChangesAsync();
    }

    /// <summary>Loescht einen Eintrag; bei einer Bewertung samt Zeilen.</summary>
    public async Task LoescheAsync<T>(LbBenutzer b, int eintragId) where T : class
    {
        Pruefe(b);
        await using var db = await DbAsync();
        object? e = typeof(T) == typeof(LbBewertung)
            ? await db.Bewertungen.Include(x => x.Zeilen).FirstOrDefaultAsync(x => x.Id == eintragId)
            : await db.Set<T>().FindAsync(eintragId);
        if (e is null)
            return;
        db.Remove(e);
        await db.SaveChangesAsync();
    }
}

public sealed record LbDaten(
    List<LbBewertung> Bewertungen,
    List<LbZertifikat> Zertifikate,
    List<LbMassnahme> Massnahmen,
    List<LbTermin> Termine,
    List<LbDokument> Dokumente);
