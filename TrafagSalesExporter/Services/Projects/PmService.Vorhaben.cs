using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

// Uebernahme der Vorhaben PM-01 ff. (2026-10-29, Entscheid Ingo „je Vorhaben ein Projekt“): Vorhaben_HERMES.tsv
// liefert Name und HERMES-Angaben, Wochen_Todo.tsv die Aufgaben (Zeilen mit ID oder Vorhaben = PM-nn). Wiederholbar:
// das Projekt wird ueber ExternalRef "PM-nn" wiedergefunden, die TSV bleibt fuehrend. Neue Projekte sind privat mit
// dem Hochladenden als Leitung (Wochen_Todo-Wunsch vom 2026-10-13); die Sichtbarkeit laesst sich danach umstellen.
public sealed partial class PmService
{
    internal sealed record VorhabenRow(
        string Id, string Name, string Stage, string Sponsor, string UserRepresentative,
        DateTime? ReleasedOn, DateTime? AcceptedOn, DateTime? ClosedOn, string Traffic,
        string StatusLine, string NextDecision, string DoneWhen);

    public async Task<PmVorhabenResult> SyncVorhabenAsync(PmUser user, string vorhabenTsv, string? wochenTodoTsv)
    {
        RequireUser(user);
        var vorhaben = ParseVorhaben(vorhabenTsv);
        if (vorhaben.Count == 0)
            throw new PmException("Die Datei enthält keine Vorhaben (erwartet: Vorhaben_HERMES.tsv mit Spalten ID, Vorhaben …).",
                "The file holds no undertakings (expected Vorhaben_HERMES.tsv with columns ID, Vorhaben …).");
        var todo = ParseWochenTodo(wochenTodoTsv);

        int projectsCreated = 0, projectsUpdated = 0, tasksCreated = 0, tasksUpdated = 0;
        var keys = new List<string>();
        await WriteGate.WaitAsync();
        try
        {
            foreach (var v in vorhaben)
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var key = VorhabenKey(v.Id);
                var project = await db.PmProjects.FirstOrDefaultAsync(p => p.ExternalRef == v.Id)
                    ?? await db.PmProjects.FirstOrDefaultAsync(p => p.Key == key);
                var now = DateTime.UtcNow;
                var name = v.Name.Length > 80 ? v.Name[..80].Trim() : v.Name;
                if (name.Length < 3)
                    name = $"Vorhaben {v.Id}";
                if (project is null)
                {
                    project = new PmProject
                    {
                        Key = key, Name = name, Description = $"Übernommen aus der Wochen_Todo ({v.Id}).",
                        Color = PmStyle.Colors[projectsCreated % PmStyle.Colors.Length], Icon = PmStyle.Icons[0],
                        Template = PmTemplates.Kanban, LeadLogin = user.Login, LeadName = user.DisplayName,
                        Visibility = PmVisibility.Private, ExternalRef = v.Id, CreatedAtUtc = now, UpdatedAtUtc = now
                    };
                    db.PmProjects.Add(project);
                    await db.SaveChangesAsync();
                    db.PmMembers.Add(new PmMember { ProjectId = project.Id, Login = user.Login, Name = user.DisplayName, Role = PmRoles.Lead, AddedAtUtc = now });
                    var order = 10;
                    foreach (var (columnName, category) in TemplateColumns(PmTemplates.Kanban))
                    {
                        db.PmColumns.Add(new PmColumn { ProjectId = project.Id, Name = columnName, Category = category, SortOrder = order });
                        order += 10;
                    }
                    Log(db, project.Id, null, user, $"hat das Projekt {key} aus dem Vorhaben {v.Id} angelegt");
                    await db.SaveChangesAsync();
                    projectsCreated++;
                }
                else
                {
                    await RequireManageAsync(db, user, project);
                    project.ExternalRef = v.Id;
                    if (project.Name != name)
                        project.Name = name;
                    projectsUpdated++;
                }

                var hermes = await db.PmHermes.FirstOrDefaultAsync(h => h.ProjectId == project.Id);
                if (hermes is null)
                {
                    hermes = new PmHermes { ProjectId = project.Id };
                    db.PmHermes.Add(hermes);
                }
                var clean = ValidateHermesLenient(v, hermes.EffortDays);
                hermes.Stage = clean.Stage;
                hermes.Sponsor = clean.Sponsor;
                hermes.UserRepresentative = clean.UserRepresentative;
                hermes.EffortDays = clean.EffortDays;
                hermes.Traffic = clean.Traffic;
                hermes.StatusLine = clean.StatusLine;
                hermes.NextDecision = clean.NextDecision;
                hermes.DoneWhen = clean.DoneWhen;
                hermes.ReleasedOn = clean.ReleasedOn;
                hermes.AcceptedOn = clean.AcceptedOn;
                hermes.ClosedOn = clean.ClosedOn;
                hermes.UpdatedAtUtc = now;

                var rows = todo.Where(r => Same(r.Id, v.Id) || Same(r.Undertaking, v.Id)).ToList();
                if (rows.Count > 0)
                {
                    var columns = await db.PmColumns.Where(c => c.ProjectId == project.Id).OrderBy(c => c.SortOrder).ToListAsync();
                    var isMember = await db.PmMembers.AnyAsync(m => m.ProjectId == project.Id && m.Login == user.Login);
                    var result = await UpsertWochenTodoRowsAsync(db, project, columns, rows, user, isMember);
                    tasksCreated += result.Created;
                    tasksUpdated += result.Updated;
                }
                project.UpdatedAtUtc = now;
                Log(db, project.Id, null, user, $"hat das Vorhaben {v.Id} abgeglichen ({rows.Count} Zeilen der Wochen_Todo)");
                await db.SaveChangesAsync();
                await ApplyScheduleAsync(db, project.Id);
                await db.SaveChangesAsync();
                keys.Add(project.Key);
                _notifier.Publish(project.Id);
            }
            _notifier.Publish(0);
            return new PmVorhabenResult(projectsCreated, projectsUpdated, tasksCreated, tasksUpdated, keys);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    // "PM-01" -> "PM01"; erfuellt die Schluesselregel (Buchstabe zuerst, 2 bis 6 Zeichen).
    internal static string VorhabenKey(string id)
    {
        var key = new string(id.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (key.Length == 0 || !char.IsLetter(key[0]))
            key = "PM" + key;
        return key.Length > 6 ? key[..6] : key;
    }

    // Wie ValidateHermes, aber ohne Abbruch: Entscheidungspunkte ausser der Reihe werden ab der ersten Luecke geleert,
    // „festlegen“ und „offen“ bleiben leer, damit eine unvollstaendige Zeile trotzdem ankommt.
    private static PmHermesInput ValidateHermesLenient(VorhabenRow v, int effortDays)
    {
        static string Text(string value) => value.Trim() is "-" or "festlegen" or "offen" ? string.Empty : value.Trim();
        static string Cut(string value) => value.Length > HermesTextMax ? value[..HermesTextMax] : value;
        var released = v.ReleasedOn;
        var accepted = released is null || v.AcceptedOn < released ? null : v.AcceptedOn;
        var closed = accepted is null || v.ClosedOn < accepted ? null : v.ClosedOn;
        return new PmHermesInput
        {
            Stage = v.Stage switch
            {
                "Kleinauftrag" => PmHermesStages.SmallOrder,
                "Vorhaben" => PmHermesStages.Undertaking,
                "Projekt" => PmHermesStages.Project,
                _ => string.Empty
            },
            Traffic = v.Traffic switch
            {
                "Grün" or "Gruen" => PmTraffic.Green,
                "Gelb" => PmTraffic.Yellow,
                "Rot" => PmTraffic.Red,
                _ => string.Empty
            },
            Sponsor = Cut(Text(v.Sponsor)),
            UserRepresentative = Cut(Text(v.UserRepresentative)),
            EffortDays = effortDays,
            StatusLine = Cut(Text(v.StatusLine)),
            NextDecision = Cut(Text(v.NextDecision)),
            DoneWhen = Cut(Text(v.DoneWhen)),
            ReleasedOn = released,
            AcceptedOn = accepted,
            ClosedOn = closed
        };
    }

    internal static IReadOnlyList<VorhabenRow> ParseVorhaben(string? tsv)
    {
        var lines = (tsv ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        if (lines.Length < 2)
            return [];
        var header = lines[0].TrimStart('﻿').Split('\t').Select(h => h.Trim()).ToList();
        int Col(params string[] names) => header.FindIndex(h => names.Any(n => string.Equals(h, n, StringComparison.OrdinalIgnoreCase)));
        var id = Col("ID");
        var name = Col("Vorhaben");
        if (id < 0 || name < 0)
            return [];
        var stage = Col("HERMES-Stufe");
        var sponsor = Col("Auftraggeber");
        var rep = Col("Anwendervertreter");
        var released = Col("Freigabe");
        var accepted = Col("Abnahme");
        var closed = Col("Abschluss");
        var traffic = Col("Ampel");
        var status = Col("Stand in einem Satz");
        var next = Col("Nächster Entscheid", "Naechster Entscheid");
        var doneWhen = Col("Erledigt, wenn", "Erledigt wenn");
        var result = new List<VorhabenRow>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var cells = line.Split('\t');
            string Cell(int index) => index >= 0 && index < cells.Length ? cells[index].Trim() : string.Empty;
            var vid = Cell(id);
            if (!vid.StartsWith("PM-", StringComparison.OrdinalIgnoreCase) || Cell(name).Length == 0)
                continue;
            result.Add(new VorhabenRow(vid.ToUpperInvariant(), Cell(name), Cell(stage), Cell(sponsor), Cell(rep),
                ParseDay(Cell(released)), ParseDay(Cell(accepted)), ParseDay(Cell(closed)), Cell(traffic),
                Cell(status), Cell(next), Cell(doneWhen)));
        }
        return result;
    }
}
