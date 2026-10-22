using Microsoft.EntityFrameworkCore;

namespace TrafagSalesExporter.Modules.Handbestellwesen;

/// <summary>Eigene SQLite-Datenbank des Handbestellwesens, getrennt von trafag_exporter.db (wie Shopfloor).</summary>
public sealed class HbDbContext(DbContextOptions<HbDbContext> options) : DbContext(options)
{
    public DbSet<HbBestellung> Bestellungen => Set<HbBestellung>();
    public DbSet<HbZaehler> Zaehler => Set<HbZaehler>();
    public DbSet<HbKreditor> Kreditoren => Set<HbKreditor>();
    public DbSet<HbMitarbeiter> Mitarbeiter => Set<HbMitarbeiter>();
    public DbSet<HbKostenstelle> Kostenstellen => Set<HbKostenstelle>();
    public DbSet<HbSachkonto> Sachkonten => Set<HbSachkonto>();
    public DbSet<HbProtokoll> Protokoll => Set<HbProtokoll>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<HbBestellung>(e =>
        {
            e.ToTable("Bestellung");
            e.HasIndex(x => x.Bestellnummer).IsUnique();
            e.HasIndex(x => x.Status);
            // SQLite kennt kein decimal; als Text gespeichert bleibt der Betrag exakt (EF-Standard).
            e.Property(x => x.Bestellwert).HasConversion<string>();
            e.Property(x => x.Rechnungsbetrag).HasConversion<string>();
        });
        b.Entity<HbZaehler>(e =>
        {
            e.ToTable("Zaehler");
            e.HasKey(x => new { x.Jahr, x.Kostenstelle });
        });
        b.Entity<HbKreditor>(e => { e.ToTable("Kreditor"); e.HasKey(x => x.Nr); });
        b.Entity<HbMitarbeiter>(e =>
        {
            e.ToTable("Mitarbeiter");
            e.HasIndex(x => x.Kurzzeichen).IsUnique();
            e.Property(x => x.Limit).HasConversion<string>();
            e.Ignore(x => x.Name);
        });
        b.Entity<HbKostenstelle>(e => { e.ToTable("Kostenstelle"); e.HasKey(x => x.Nr); });
        b.Entity<HbSachkonto>(e => { e.ToTable("Sachkonto"); e.HasKey(x => x.Nr); });
        b.Entity<HbProtokoll>(e => { e.ToTable("Protokoll"); e.HasIndex(x => x.Bezug); });
    }
}
