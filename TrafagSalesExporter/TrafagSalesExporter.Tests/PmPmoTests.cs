using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.Projects;

namespace TrafagSalesExporter.Tests;

// PMO-Funktionen aus der Sitzung vom 2026-10-29: Risiken in Franken mit Burndown und PMO-Vorlage, Vorlagen
// Produktentwicklung und IT-Projekt, Projektstrukturplan fuer SAP PS, Decision Log.
public sealed class PmPmoTests : IDisposable
{
    private static readonly PmUser Lead = new("anna", "Muster, Anna", false);
    private static readonly PmUser Guest = new("gast", "Gast Leser", false);
    private static readonly DateTime Mon = new(2026, 11, 2);

    private readonly SqliteConnection _connection;
    private readonly PmService _service;

    public PmPmoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        foreach (var table in new[] { "PmTasks", "PmRisks", "PmRiskSnapshots", "PmDecisions" })
            db.Database.ExecuteSqlRaw($"DROP TABLE {table}");
        new DatabaseSchemaMaintenanceService().EnsureSchema(db);
        _service = new PmService(new TestDbContextFactory(options), new PmNotifier());
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Product_Development_Template_Builds_Phases_Milestones_And_Chain()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput
        {
            Name = "Drucktransmitter neue Generation", Key = "DTNG", Template = PmTemplates.ProductDevelopment, StartDate = Mon
        });
        var sheet = (await _service.GetSheetAsync(id, Lead))!;

        Assert.Equal("M1 Projektstart", sheet.Rows[0].Title);
        Assert.True(sheet.Rows[0].IsMilestone);
        Assert.Equal(Mon, sheet.Rows[0].Start);
        Assert.Equal(3, sheet.Rows.Count(r => r.Type == PmTaskTypes.Epic));
        var m2 = sheet.Rows.Single(r => r.Title == "M2 Design-Freeze");
        var m3 = sheet.Rows.Single(r => r.Title.StartsWith("M3 Produktiv"));
        var design = sheet.Rows.Single(r => r.Title == "Design");
        Assert.True(design.HasChildren);
        Assert.Equal(PmTaskTypes.Subtask, sheet.Rows.Single(r => r.Title == "Firmware").Type);
        // Spezifikation 10 AT, Firmware 20 AT, Prototypen 15 AT: M2 frueh im Maerz 2027 (Feiertage eingerechnet).
        Assert.True(m2.Start > design.End);
        Assert.True(m3.Start > m2.Start);
        Assert.True(m2.IsCritical);
        Assert.Contains(sheet.Rows, r => r.Title == "Projektabschluss und Entlastung" && r.IsMilestone);

        var risks = await _service.GetRisksAsync(id, Lead);
        Assert.Equal(PmRiskTemplates.StandardRisks(PmTemplates.ProductDevelopment).Count, risks.Count);
        Assert.Equal(1, (await _service.GetRiskBurndownAsync(id, Lead)).Count);
    }

    [Fact]
    public async Task It_Project_Template_Follows_Hermes_And_Kanban_Stays_Empty()
    {
        var it = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Zeiterfassung App", Key = "ZEIT", Template = PmTemplates.ItProject, StartDate = Mon });
        var rows = (await _service.GetSheetAsync(it, Lead))!.Rows;
        Assert.Equal(["Initialisierung", "Konzept", "Realisierung", "Einführung"], rows.Where(r => r.Type == PmTaskTypes.Epic).Select(r => r.Title));
        Assert.Equal(4, rows.Count(r => r.IsMilestone));

        var kanban = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Freies Projekt", Key = "FREI" });
        Assert.Empty((await _service.GetSheetAsync(kanban, Lead))!.Rows);
        Assert.Empty(await _service.GetRisksAsync(kanban, Lead));
    }

    [Fact]
    public async Task Risk_Francs_Give_Expected_Value_Contingency_And_Daily_Burndown()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Linie 4 Umbau", Key = "LIN" });
        var r1 = await _service.SaveRiskAsync(Lead, id, new PmRiskInput { Title = "Lieferverzug", Probability = 4, Impact = 4, CostImpactChf = 100_000, ContingencyChf = 50_000 });
        await _service.SaveRiskAsync(Lead, id, new PmRiskInput { Title = "Zulassung", Probability = 1, Impact = 5, CostImpactChf = 200_000 });

        var risks = await _service.GetRisksAsync(id, Lead);
        var lief = risks.Single(r => r.Id == r1);
        Assert.Equal(70, lief.ProbabilityPercent);
        Assert.Equal(70_000, lief.ExpectedValueChf);
        Assert.Equal(20_000, risks.Single(r => r.Title == "Zulassung").ExpectedValueChf);
        var summary = (await _service.GetSheetAsync(id, Lead))!.Risks;
        Assert.Equal(90_000, summary.ExpectedValueChf);
        Assert.Equal(50_000, summary.ContingencyChf);

        await _service.SaveRiskAsync(Lead, id, new PmRiskInput { Id = r1, Title = "Lieferverzug", Probability = 4, Impact = 4, CostImpactChf = 100_000, Status = PmRiskStatuses.Closed });
        var burndown = await _service.GetRiskBurndownAsync(id, Lead);
        var today = Assert.Single(burndown);
        Assert.Equal(1, today.Active);
        Assert.Equal(20_000, today.ExpectedValueChf);
        Assert.Equal(0, today.ContingencyChf);

        await Assert.ThrowsAsync<PmException>(() => _service.SaveRiskAsync(Lead, id, new PmRiskInput { Title = "Negativ", CostImpactChf = -1 }));
        Assert.Equal(2, await _service.AddStandardRisksAsync(Lead, id, PmTemplates.Kanban));
        Assert.Equal(0, await _service.AddStandardRisksAsync(Lead, id, PmTemplates.Kanban));
        await Assert.ThrowsAsync<PmException>(() => _service.AddStandardRisksAsync(Guest, id, PmTemplates.Kanban));
    }

    [Fact]
    public async Task Wbs_Codes_Rollups_Cost_Types_And_Csv()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Linie 4 Umbau", Key = "LIN" });
        var epic = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Phase Bau", Type = PmTaskTypes.Epic });
        var a = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Fundament", EpicId = epic, StartDate = Mon, DueDate = Mon.AddDays(1) });
        var b = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Maschine; Typ X", EpicId = epic });
        var loose = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Abnahme" });

        await _service.UpdateWbsFieldAsync(Lead, a, PmService.WbsFields.Hours, "40");
        await _service.UpdateWbsFieldAsync(Lead, a, PmService.WbsFields.Cost, "12'500.50");
        await _service.UpdateWbsFieldAsync(Lead, a, PmService.WbsFields.CostType, PmCostTypes.Opex);
        await _service.UpdateWbsFieldAsync(Lead, b, PmService.WbsFields.Cost, "80000");
        await _service.UpdateWbsFieldAsync(Lead, b, PmService.WbsFields.CostType, PmCostTypes.Capex);
        await _service.UpdateWbsFieldAsync(Lead, loose, PmService.WbsFields.Hours, "8");

        var wbs = (await _service.GetWbsAsync(id, Lead))!;
        Assert.Equal(["LIN.01", "LIN.01.01", "LIN.01.02", "LIN.02"], wbs.Rows.Select(r => r.Code));
        var phase = wbs.Rows[0];
        Assert.Equal(40, phase.TotalHours);
        Assert.Equal(92_500.5, phase.TotalCostChf);
        Assert.Equal(48, wbs.TotalHours);
        Assert.Equal(92_500.5, wbs.TotalCostChf);
        Assert.Equal(80_000, wbs.CapexChf);
        Assert.Equal(12_500.5, wbs.OpexChf);

        var csv = await _service.ExportWbsCsvAsync(id, Lead);
        Assert.StartsWith("﻿PSP-Element;Ebene;Bezeichnung", csv);
        Assert.Contains("LIN.01.02;2;\"Maschine; Typ X\";Task;LIN-3;;;0;80000;0;80000;CAPEX;", csv);

        await Assert.ThrowsAsync<PmException>(() => _service.UpdateWbsFieldAsync(Lead, a, PmService.WbsFields.Hours, "viel"));
        await Assert.ThrowsAsync<PmException>(() => _service.UpdateWbsFieldAsync(Lead, a, PmService.WbsFields.CostType, "Sonstiges"));
        await Assert.ThrowsAsync<PmException>(() => _service.UpdateWbsFieldAsync(Guest, a, PmService.WbsFields.Hours, "1"));
    }

    [Fact]
    public async Task Decision_Log_Numbers_Statuses_And_Rights()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Linie 4 Umbau", Key = "LIN" });
        var task = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Antrieb wählen" });
        var d1 = await _service.SaveDecisionAsync(Lead, id, new PmDecisionInput
        {
            Title = "Antrieb Lieferant A", Decision = "Lieferant A", Rationale = "Lieferzeit", Alternatives = "Lieferant B",
            Status = PmDecisionStatuses.Decided, DecidedOn = Mon, DecidedBy = "Lenkungsausschuss", TaskId = task
        });
        await _service.SaveDecisionAsync(Lead, id, new PmDecisionInput { Title = "Zweitquelle prüfen" });

        var log = await _service.GetDecisionsAsync(id, Lead);
        Assert.Equal(2, log.Count);
        var first = log.Single(d => d.Id == d1);
        Assert.Equal("LIN-D1", first.Key);
        Assert.Equal("LIN-1", first.TaskKey);
        Assert.Equal(PmDecisionStatuses.Decided, first.Status);
        Assert.Equal("Muster, Anna", first.AuthorName);

        await Assert.ThrowsAsync<PmException>(() => _service.SaveDecisionAsync(Lead, id, new PmDecisionInput { Title = "Ohne Datum", Status = PmDecisionStatuses.Decided }));
        await Assert.ThrowsAsync<PmException>(() => _service.SaveDecisionAsync(Guest, id, new PmDecisionInput { Title = "Darf nicht" }));
        await _service.SetDecisionArchivedAsync(Lead, d1, true);
        Assert.Single(await _service.GetDecisionsAsync(id, Lead));
        Assert.Equal(2, (await _service.GetDecisionsAsync(id, Lead, includeArchived: true)).Count);
    }

    [Fact]
    public async Task Risk_View_Carries_Archive_Flag_So_The_List_Loads_Once()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Linie 4 Umbau", Key = "LIN" });
        var kept = await _service.SaveRiskAsync(Lead, id, new PmRiskInput { Title = "Lieferverzug" });
        var gone = await _service.SaveRiskAsync(Lead, id, new PmRiskInput { Title = "Alt" });
        await _service.SetRiskArchivedAsync(Lead, gone, true);

        Assert.DoesNotContain(await _service.GetRisksAsync(id, Lead), r => r.IsArchived);
        var all = await _service.GetRisksAsync(id, Lead, includeArchived: true);
        Assert.True(all.Single(r => r.Id == gone).IsArchived);
        Assert.False(all.Single(r => r.Id == kept).IsArchived);
    }

    [Fact]
    public void Pmo_Scale_Texts_Are_Translated_In_Every_Projects_Language()
    {
        // Die Stufenbeschreibungen sind C#-Paare in PmRiskTemplates und werden dynamisch uebersetzt; der Razor-Test
        // findet sie nicht, deshalb hier.
        var texts = PmRiskTemplates.ProbabilityScale.Concat(PmRiskTemplates.ImpactScale).Select(s => s.German).ToList();
        Assert.Equal(10, texts.Count);
        foreach (var language in PmUiTextCatalog.Languages)
            Assert.All(texts, t => Assert.True(PmUiTextCatalog.All[language].ContainsKey(t), $"{language} fehlt: {t}"));
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
