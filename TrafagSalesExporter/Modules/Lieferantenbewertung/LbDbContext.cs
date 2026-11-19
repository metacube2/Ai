using Microsoft.EntityFrameworkCore;

namespace TrafagSalesExporter.Modules.Lieferantenbewertung;

/// <summary>Eigene SQLite-Datenbank der Lieferantenbewertung, getrennt von trafag_exporter.db (wie Handbestellwesen).</summary>
public sealed class LbDbContext(DbContextOptions<LbDbContext> options) : DbContext(options)
{
    public DbSet<LbBewertung> Bewertungen => Set<LbBewertung>();
    public DbSet<LbBewertungZeile> BewertungZeilen => Set<LbBewertungZeile>();
    public DbSet<LbZertifikat> Zertifikate => Set<LbZertifikat>();
    public DbSet<LbMassnahme> Massnahmen => Set<LbMassnahme>();
    public DbSet<LbTermin> Termine => Set<LbTermin>();
    public DbSet<LbDokument> Dokumente => Set<LbDokument>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<LbBewertung>(e =>
        {
            e.ToTable("Bewertung");
            e.HasIndex(x => x.LieferantNr);
            e.HasMany(x => x.Zeilen).WithOne().HasForeignKey(x => x.BewertungId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<LbBewertungZeile>(e => e.ToTable("BewertungZeile"));
        b.Entity<LbZertifikat>(e => { e.ToTable("Zertifikat"); e.HasIndex(x => x.LieferantNr); });
        b.Entity<LbMassnahme>(e => { e.ToTable("Massnahme"); e.HasIndex(x => x.LieferantNr); });
        b.Entity<LbTermin>(e => { e.ToTable("Termin"); e.HasIndex(x => x.LieferantNr); });
        b.Entity<LbDokument>(e => { e.ToTable("Dokument"); e.HasIndex(x => x.LieferantNr); });
    }
}
