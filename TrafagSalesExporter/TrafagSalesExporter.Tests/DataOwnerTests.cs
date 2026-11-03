using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

// Datenverantwortung (Dataowner) 2026-11-03: Tabelle aus dem Wartungs-SQL passt zum EF-Modell, Startbelegung nur
// beim ersten Anlegen, Pflichtfelder und Normalisierung.
public class DataOwnerTests
{
    [Fact]
    public void EnsureDataOwnersTable_Creates_Table_Seeds_Once_And_Matches_Ef_Model()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        using (var db = new AppDbContext(options))
        {
            DatabaseSchemaMaintenanceService.EnsureDataOwnersTable(db);
            var rows = db.DataOwners.AsNoTracking().OrderBy(x => x.SortOrder).ToList();
            Assert.Equal(DataOwnerRules.InitialEntries().Count, rows.Count);
            Assert.All(rows, r => Assert.Equal(DataOwnerStatus.Provisional, r.Status));
            Assert.Contains(rows, r => r.DataDomain == "Produktionsdaten" && r.Responsible == "Dragan Radanovic");
            Assert.Contains(rows, r => r.DataDomain == "Lieferanten- und Einkaufsdaten" && r.KeyUser == "Marco Widmer");

            // Ingo loescht einen Eintrag; ein erneuter Start darf ihn nicht zurueckholen.
            db.DataOwners.Remove(db.DataOwners.First());
            db.SaveChanges();
        }

        using (var db = new AppDbContext(options))
        {
            DatabaseSchemaMaintenanceService.EnsureDataOwnersTable(db);
            Assert.Equal(DataOwnerRules.InitialEntries().Count - 1, db.DataOwners.Count());

            db.DataOwners.Add(new DataOwner
            {
                DataDomain = "Qualitaetsdaten", Responsible = "Test", Status = DataOwnerStatus.Confirmed,
                UpdatedAtUtc = DateTime.UtcNow, UpdatedBy = "koi"
            });
            db.SaveChanges();
            Assert.Equal(DataOwnerStatus.Confirmed, db.DataOwners.AsNoTracking().Single(x => x.DataDomain == "Qualitaetsdaten").Status);
        }
    }

    [Fact]
    public void Rules_Require_Domain_And_Responsible_Trim_And_Fix_Status()
    {
        var entry = new DataOwner
        {
            DataDomain = "  Materialstamm  ", Responsible = " ", Status = "irgendwas",
            Note = new string('x', DataOwnerRules.NoteMax + 50)
        };
        DataOwnerRules.Normalize(entry);

        Assert.Equal("Materialstamm", entry.DataDomain);
        Assert.Equal(DataOwnerStatus.Provisional, entry.Status);
        Assert.Equal(DataOwnerRules.NoteMax, entry.Note.Length);
        Assert.False(DataOwnerRules.IsValid(entry));

        entry.Responsible = "Fabio Palma";
        Assert.True(DataOwnerRules.IsValid(entry));
    }
}
