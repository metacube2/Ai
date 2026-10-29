using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

// Projektvorlagen mit fertigem Plan (Sitzung 2026-10-29). Produktentwicklung nach Philips Standard-Zeitplan aus
// Smartsheet: Projektstart, Phase Spezifikation bis Meilenstein 2 (Design-Freeze), Industrialisierung bis Meilenstein 3
// (produktiv), Optimierung. IT-Projekt nach HERMES (Initialisierung, Konzept, Realisierung, Einfuehrung). Die Dauern sind
// Vorschlaege in Arbeitstagen; die Kette haengt ueber Ende-Anfang-Abhaengigkeiten, verschieben zieht alles mit.
public sealed partial class PmService
{
    internal sealed record TemplateItem(string Key, string Title, string Type, int Days, string? Parent, string[] After,
        bool Milestone = false);

    internal static IReadOnlyList<TemplateItem> TemplatePlan(string template) => template switch
    {
        PmTemplates.ProductDevelopment =>
        [
            new("m1", "M1 Projektstart", PmTaskTypes.Task, 0, null, [], true),
            new("p1", "Phase 1: Spezifikation und Entwicklung", PmTaskTypes.Epic, 0, null, []),
            new("spec", "Spezifikation erstellen und abnehmen", PmTaskTypes.Task, 10, "p1", ["m1"]),
            new("fk", "Fertigungskonzept", PmTaskTypes.Task, 10, "p1", ["spec"]),
            new("sk", "Softwarekonzept Betriebsmittelbau", PmTaskTypes.Task, 10, "p1", ["spec"]),
            new("design", "Design", PmTaskTypes.Task, 0, "p1", []),
            new("dfmea", "Design-FMEA", PmTaskTypes.Subtask, 5, "design", ["spec"]),
            new("mech", "Mechanik", PmTaskTypes.Subtask, 15, "design", ["spec"]),
            new("pcb", "Hardware / PCB", PmTaskTypes.Subtask, 15, "design", ["spec"]),
            new("fw", "Firmware", PmTaskTypes.Subtask, 20, "design", ["spec"]),
            new("pack", "Konzept Produktverpackung", PmTaskTypes.Subtask, 5, "design", ["spec"]),
            new("label", "Typenschild", PmTaskTypes.Subtask, 3, "design", ["spec"]),
            new("proto", "Prototypen bauen und testen", PmTaskTypes.Task, 15, "p1", ["mech", "pcb", "fw"]),
            new("m2", "M2 Design-Freeze", PmTaskTypes.Task, 0, "p1", ["proto", "dfmea", "fk", "sk", "pack", "label"], true),
            new("p2", "Phase 2: Industrialisierung", PmTaskTypes.Epic, 0, null, []),
            new("bm", "Betriebsmittel und Prüfmittel bauen", PmTaskTypes.Task, 20, "p2", ["m2"]),
            new("null", "Nullserie und Auswertung", PmTaskTypes.Task, 10, "p2", ["bm"]),
            new("approval", "Zulassung, Dokumentation und Freigabe Serie", PmTaskTypes.Task, 10, "p2", ["m2"]),
            new("m3", "M3 Produktiv (Verkauf bzw. Produktion)", PmTaskTypes.Task, 0, "p2", ["null", "approval"], true),
            new("p3", "Phase 3: Optimierung", PmTaskTypes.Epic, 0, null, []),
            new("fpy", "Kinderkrankheiten beheben, First Pass Yield auf Ziel", PmTaskTypes.Task, 20, "p3", ["m3"]),
            new("end", "Projektabschluss und Entlastung", PmTaskTypes.Task, 0, "p3", ["fpy"], true)
        ],
        PmTemplates.ItProject =>
        [
            new("i", "Initialisierung", PmTaskTypes.Epic, 0, null, []),
            new("auftrag", "Projektauftrag mit Zielen, Abnahmekriterien und Rollen", PmTaskTypes.Task, 5, "i", []),
            new("f1", "Freigabe Projektauftrag", PmTaskTypes.Task, 0, "i", ["auftrag"], true),
            new("k", "Konzept", PmTaskTypes.Epic, 0, null, []),
            new("anf", "Anforderungen mit dem Anwendervertreter", PmTaskTypes.Task, 10, "k", ["f1"]),
            new("lsg", "Lösungskonzept, Schnittstellen und Datenschutz", PmTaskTypes.Task, 10, "k", ["anf"]),
            new("f2", "Konzept abgenommen", PmTaskTypes.Task, 0, "k", ["lsg"], true),
            new("r", "Realisierung", PmTaskTypes.Epic, 0, null, []),
            new("bau", "Umsetzen", PmTaskTypes.Task, 20, "r", ["f2"]),
            new("test", "Test mit Anwendern", PmTaskTypes.Task, 5, "r", ["bau"]),
            new("f3", "Abnahme", PmTaskTypes.Task, 0, "r", ["test"], true),
            new("e", "Einführung", PmTaskTypes.Epic, 0, null, []),
            new("schul", "Schulung und Anleitung", PmTaskTypes.Task, 3, "e", ["f3"]),
            new("live", "Produktiv setzen", PmTaskTypes.Task, 1, "e", ["schul"]),
            new("f4", "Projektabschluss", PmTaskTypes.Task, 0, "e", ["live"], true)
        ],
        _ => []
    };

    // Legt den Plan einer Vorlage im frisch angelegten Projekt an (Nummern, Gliederung, Abhaengigkeiten, Termine ab
    // start) und uebernimmt die Standardrisiken der PMO-Vorlage. Laeuft innerhalb der Schreibschleuse von
    // CreateProjectAsync und speichert selbst.
    internal static async Task SeedTemplateAsync(AppDbContext db, PmProject project, PmUser user, DateTime start)
    {
        var plan = TemplatePlan(project.Template);
        if (plan.Count == 0)
            return;
        var todo = await db.PmColumns.Where(c => c.ProjectId == project.Id).OrderBy(c => c.SortOrder).FirstAsync();
        var now = DateTime.UtcNow;
        var first = PmSchedule.SnapForward(start);
        var byKey = new Dictionary<string, PmTask>();
        var order = 1000.0;
        foreach (var item in plan)
        {
            byKey.TryGetValue(item.Parent ?? string.Empty, out var parent);
            var task = new PmTask
            {
                ProjectId = project.Id,
                Number = project.NextNumber++,
                Type = item.Type,
                Title = item.Title,
                ColumnId = todo.Id,
                Priority = item.Milestone ? PmPriorities.High : PmPriorities.Medium,
                ReporterLogin = user.Login,
                ReporterName = user.DisplayName,
                IsMilestone = item.Milestone,
                // Vorlaeufige Termine; die Kette rechnet ApplyScheduleAsync gleich danach.
                StartDate = item.Type == PmTaskTypes.Epic || item.Days == 0 && !item.Milestone ? null : first,
                DueDate = item.Type == PmTaskTypes.Epic || item.Days == 0 && !item.Milestone ? null : PmSchedule.EndFor(first, item.Days),
                EpicId = parent?.Type == PmTaskTypes.Epic ? parent.Id : parent?.EpicId,
                Rank = order,
                SheetOrder = order,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            order += 1000;
            if (parent is not null && parent.Type != PmTaskTypes.Epic)
                task.ParentId = parent.Id;
            db.PmTasks.Add(task);
            await db.SaveChangesAsync();
            byKey[item.Key] = task;
        }
        foreach (var item in plan)
            foreach (var after in item.After)
                db.PmTaskLinks.Add(new PmTaskLink { ProjectId = project.Id, PredecessorId = byKey[after].Id, SuccessorId = byKey[item.Key].Id });
        await db.SaveChangesAsync();
        await ApplyScheduleAsync(db, project.Id);
        await AddStandardRisksCoreAsync(db, project, PmRiskTemplates.StandardRisks(project.Template));
        Log(db, project.Id, null, user, $"hat den Plan der Vorlage angelegt ({plan.Count} Zeilen)");
        await db.SaveChangesAsync();
        await UpsertRiskSnapshotAsync(db, project.Id);
        await db.SaveChangesAsync();
    }
}
