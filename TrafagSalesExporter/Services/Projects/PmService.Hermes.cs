using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

// HERMES zugeschnitten (2026-10-13, Wunsch Ingo): Einstufung, Rollen, Ampel, Statuszeile und die drei
// Entscheidungspunkte (1) Freigabe, (2) Abnahme, (3) Abschluss je Projekt, wie in
// projektmanagement/Vorhaben_HERMES.tsv. Pflegen darf die Projektleitung oder ein Admin.
public sealed partial class PmService
{
    public const int HermesTextMax = 1000;
    public const string DemoKey = "HDEMO";

    public async Task SaveHermesAsync(PmUser user, int projectId, PmHermesInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw NotFound();
        await RequireManageAsync(db, user, project);
        var clean = ValidateHermes(input);

        var row = await db.PmHermes.FirstOrDefaultAsync(h => h.ProjectId == projectId);
        if (row is null)
        {
            row = new PmHermes { ProjectId = projectId };
            db.PmHermes.Add(row);
        }
        row.Stage = clean.Stage;
        row.Sponsor = clean.Sponsor;
        row.UserRepresentative = clean.UserRepresentative;
        row.EffortDays = clean.EffortDays;
        row.Traffic = clean.Traffic;
        row.StatusLine = clean.StatusLine;
        row.NextDecision = clean.NextDecision;
        row.DoneWhen = clean.DoneWhen;
        row.ReleasedOn = clean.ReleasedOn;
        row.AcceptedOn = clean.AcceptedOn;
        row.ClosedOn = clean.ClosedOn;
        row.UpdatedAtUtc = DateTime.UtcNow;
        project.UpdatedAtUtc = row.UpdatedAtUtc;
        Log(db, projectId, null, user, "hat die HERMES-Angaben geändert");
        await db.SaveChangesAsync();
        _notifier.Publish(0);
        _notifier.Publish(projectId);
    }

    internal static PmHermesInput ValidateHermes(PmHermesInput input)
    {
        static string Text(string? value) => (value ?? string.Empty).Trim();

        var clean = new PmHermesInput
        {
            Stage = PmHermesStages.All.Contains(input.Stage) ? input.Stage : string.Empty,
            Traffic = PmTraffic.All.Contains(input.Traffic) ? input.Traffic : string.Empty,
            Sponsor = Text(input.Sponsor),
            UserRepresentative = Text(input.UserRepresentative),
            EffortDays = Math.Max(0, input.EffortDays),
            StatusLine = Text(input.StatusLine),
            NextDecision = Text(input.NextDecision),
            DoneWhen = Text(input.DoneWhen),
            ReleasedOn = input.ReleasedOn?.Date,
            AcceptedOn = input.AcceptedOn?.Date,
            ClosedOn = input.ClosedOn?.Date
        };
        if (new[] { clean.Sponsor, clean.UserRepresentative, clean.StatusLine, clean.NextDecision, clean.DoneWhen }.Any(t => t.Length > HermesTextMax))
            throw new PmException($"Ein HERMES-Feld ist länger als {HermesTextMax} Zeichen.", $"A HERMES field is longer than {HermesTextMax} characters.");

        // Die Entscheidungspunkte bauen aufeinander auf: keine Abnahme ohne Freigabe, kein Abschluss ohne Abnahme.
        if (clean.AcceptedOn.HasValue && !clean.ReleasedOn.HasValue)
            throw new PmException("Eine Abnahme setzt die Freigabe voraus. Bitte zuerst das Datum der Freigabe eintragen.", "Acceptance requires the release. Please enter the release date first.");
        if (clean.ClosedOn.HasValue && !clean.AcceptedOn.HasValue)
            throw new PmException("Ein Abschluss setzt die Abnahme voraus. Bitte zuerst das Datum der Abnahme eintragen.", "Closure requires acceptance. Please enter the acceptance date first.");
        if (clean.AcceptedOn < clean.ReleasedOn || clean.ClosedOn < clean.AcceptedOn)
            throw new PmException("Die Entscheidungspunkte müssen in der Reihenfolge Freigabe, Abnahme, Abschluss liegen.", "The decision points must follow the order release, acceptance, closure.");
        return clean;
    }

    internal static PmHermesView? ToHermesView(PmHermes? row) => row is null ? null : new PmHermesView(
        row.Stage, row.Sponsor, row.UserRepresentative, row.EffortDays, row.Traffic,
        row.StatusLine, row.NextDecision, row.DoneWhen, row.ReleasedOn, row.AcceptedOn, row.ClosedOn, row.UpdatedAtUtc);

    /// <summary>
    /// Legt einmalig das Demoprojekt HDEMO an, das zeigt, wo welche HERMES-Angabe hingehoert.
    /// Existiert der Schluessel schon (auch archiviert), passiert nichts; archivieren reicht also zum Ausblenden.
    /// </summary>
    public static async Task SeedHermesDemoAsync(AppDbContext db, string leadLogin, string leadName)
    {
        if (await db.PmProjects.AnyAsync(p => p.Key == DemoKey))
            return;

        var now = DateTime.UtcNow;
        var today = DateTime.Today;
        var project = new PmProject
        {
            Key = DemoKey,
            Name = "HERMES-Demo: Beispielvorhaben",
            Description = "Zeigt, wo die HERMES-Angaben hingehören. Die Werte sind erfunden; das Projekt darf archiviert werden.",
            Color = "#3949AB", Icon = "Rocket", Template = PmTemplates.Kanban,
            LeadLogin = leadLogin, LeadName = leadName,
            StartDate = today, DueDate = today.AddDays(42), CreatedAtUtc = now, UpdatedAtUtc = now
        };
        db.PmProjects.Add(project);
        await db.SaveChangesAsync();

        db.PmMembers.Add(new PmMember { ProjectId = project.Id, Login = leadLogin, Name = leadName, Role = PmRoles.Lead, AddedAtUtc = now });
        var columns = new List<PmColumn>();
        var order = 10;
        foreach (var (name, category) in TemplateColumns(PmTemplates.Kanban))
        {
            var column = new PmColumn { ProjectId = project.Id, Name = name, Category = category, SortOrder = order };
            columns.Add(column);
            db.PmColumns.Add(column);
            order += 10;
        }
        await db.SaveChangesAsync();

        var todo = columns.First(c => c.Category == PmCategories.Todo).Id;
        var doing = columns.First(c => c.Category == PmCategories.InProgress).Id;
        var done = columns.First(c => c.Category == PmCategories.Done).Id;
        (string Title, string Description, int Column, int Days)[] tasks =
        [
            ("Projektauftrag schreiben und vom Auftraggeber unterschreiben lassen",
             "Vorlage im Blatt „Vorlage Projektauftrag“ der Wochen_Todo.xlsx. Mit der Unterschrift ist der Entscheidungspunkt ① Freigabe erreicht; das Datum gehört unter Einstellungen in den HERMES-Block.", done, 0),
            ("Abnahmekriterien mit dem Anwendervertreter festlegen",
             "Die Kriterien stehen im HERMES-Block unter „Erledigt, wenn“. Sie sind später die Prüfliste für das Abnahmeprotokoll.", done, 3),
            ("Umsetzung",
             "Die eigentliche Arbeit, beliebig in Aufgaben aufgeteilt. Die Ampel und der Stand in einem Satz zeigen der Geschäftsleitung im Reiter Portfolio, wie es läuft.", doing, 21),
            ("Abnahmetest mit dem Anwendervertreter",
             "Kriterien einzeln prüfen und im Abnahmeprotokoll festhalten. Mit der Unterschrift des Anwendervertreters ist ② Abnahme erreicht.", todo, 35),
            ("Übergabe in den Betrieb",
             "Betriebsdokumentation ablegen und die Zuständigkeit im Betrieb nennen. Damit ist ③ Abschluss erreicht und das Vorhaben fertig.", todo, 42)
        ];
        var number = 1;
        foreach (var (title, description, column, days) in tasks)
        {
            db.PmTasks.Add(new PmTask
            {
                ProjectId = project.Id, Number = number, Type = PmTaskTypes.Task, Title = title, Description = description,
                ColumnId = column, Priority = PmPriorities.Medium, AssigneeLogin = leadLogin, AssigneeName = leadName,
                ReporterLogin = leadLogin, ReporterName = leadName, DueDate = today.AddDays(days), Rank = number * 1000d,
                CreatedAtUtc = now, UpdatedAtUtc = now, CompletedAtUtc = column == done ? now : null
            });
            number++;
        }
        project.NextNumber = number;

        db.PmHermes.Add(new PmHermes
        {
            ProjectId = project.Id,
            Stage = PmHermesStages.Undertaking,
            Sponsor = "Beispiel: Leiterin Logistik (entscheidet und priorisiert)",
            UserRepresentative = "Beispiel: Disponent aus der Fachabteilung (definiert fachlich und nimmt ab)",
            EffortDays = 15,
            Traffic = PmTraffic.Green,
            StatusLine = "Der Auftrag ist freigegeben, die Umsetzung läuft im Plan.",
            NextDecision = "Abnahmetest mit dem Anwendervertreter terminieren.",
            DoneWhen = "Die Auswertung liefert für eine vereinbarte Stichprobe dieselben Werte wie SAP, und der Anwendervertreter hat das Abnahmeprotokoll unterschrieben.",
            ReleasedOn = today,
            UpdatedAtUtc = now
        });
        db.PmActivities.Add(new PmActivity { ProjectId = project.Id, Login = leadLogin, Name = leadName, Text = "hat das Demoprojekt HDEMO angelegt", AtUtc = now });
        await db.SaveChangesAsync();
    }
}
