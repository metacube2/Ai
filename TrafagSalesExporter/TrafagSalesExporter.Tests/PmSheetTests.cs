using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.Projects;

namespace TrafagSalesExporter.Tests;

// Smartsheet-Ansicht, Risikoregister und Uebernahme der Vorhaben (2026-10-29).
public sealed class PmSheetTests : IDisposable
{
    private static readonly PmUser Lead = new("anna", "Muster, Anna", false);
    private static readonly PmUser Guest = new("gast", "Gast Leser", false);

    // Montag 2026-11-02 bis Freitag 2026-11-06, keine Feiertage.
    private static readonly DateTime Mon = new(2026, 11, 2);

    private readonly SqliteConnection _connection;
    private readonly PmService _service;
    private readonly DbContextOptions<AppDbContext> _options;

    public PmSheetTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
        // Die neuen Tabellen muessen auch ueber die Schema-SQL einer bestehenden Produktiv-DB entstehen.
        foreach (var table in new[] { "PmProjects", "PmMembers", "PmColumns", "PmSprints", "PmTasks", "PmChecklistItems", "PmComments", "PmActivities", "PmHermes", "PmTaskLinks", "PmRisks" })
            db.Database.ExecuteSqlRaw($"DROP TABLE {table}");
        new DatabaseSchemaMaintenanceService().EnsureSchema(db);
        _service = new PmService(new TestDbContextFactory(_options), new PmNotifier());
    }

    public void Dispose() => _connection.Dispose();

    // ---------- Reine Terminregeln ----------

    [Fact]
    public void Workdays_Skip_Weekend_And_Zurich_Holidays()
    {
        var friday = new DateTime(2026, 11, 6);
        Assert.Equal(new DateTime(2026, 11, 9), PmSchedule.AddWorkdays(friday, 1));
        Assert.Equal(new DateTime(2026, 11, 9), PmSchedule.SnapForward(new DateTime(2026, 11, 7)));
        Assert.Equal(friday, PmSchedule.SnapBackward(new DateTime(2026, 11, 8)));
        // Weihnachten (Fr 25.12.) und Stephanstag (Sa 26.12.) sind frei.
        Assert.Equal(new DateTime(2026, 12, 28), PmSchedule.AddWorkdays(new DateTime(2026, 12, 24), 1));
        Assert.Equal(5, PmSchedule.Duration(Mon, friday));
        Assert.Equal(friday, PmSchedule.EndFor(Mon, 5));
        Assert.Equal(Mon, PmSchedule.EndFor(Mon, 0));
    }

    [Fact]
    public void Successor_Follows_Predecessor_In_Both_Directions_And_Keeps_Duration()
    {
        var a = new PmSchedule.Node { Id = 1, Start = Mon, End = Mon.AddDays(2) };
        var b = new PmSchedule.Node { Id = 2, Start = Mon, End = Mon.AddDays(1) };
        var links = new[] { new PmSchedule.Link(1, 2, 0) };

        PmSchedule.Apply([a, b], links);
        Assert.Equal(Mon.AddDays(3), b.Start); // Donnerstag
        Assert.Equal(new DateTime(2026, 11, 6), b.End); // zwei Tage Dauer bleiben

        a.End = Mon;
        PmSchedule.Apply([a, b], links);
        Assert.Equal(Mon.AddDays(1), b.Start); // zieht zurueck auf Dienstag

        PmSchedule.Apply([a, b], [new PmSchedule.Link(1, 2, 2)]);
        Assert.Equal(Mon.AddDays(3), b.Start); // Versatz zwei Arbeitstage
    }

    [Fact]
    public void Summary_Row_Rolls_Up_Dates_And_Weighted_Progress()
    {
        var epic = new PmSchedule.Node { Id = 10 };
        var a = new PmSchedule.Node { Id = 1, RollupParentId = 10, Start = Mon, End = Mon.AddDays(3), Percent = 100 };
        var b = new PmSchedule.Node { Id = 2, RollupParentId = 10, Start = Mon.AddDays(7), End = Mon.AddDays(7), Percent = 0 };

        PmSchedule.Apply([epic, a, b], []);

        Assert.True(epic.HasChildren);
        Assert.Equal(Mon, epic.Start);
        Assert.Equal(Mon.AddDays(7), epic.End);
        Assert.Equal(80, epic.Percent); // 4 Tage zu 100 %, 1 Tag zu 0 %
    }

    [Fact]
    public void Critical_Path_Marks_Longest_Chain_And_Float_On_Parallel_Task()
    {
        var a = new PmSchedule.Node { Id = 1, Start = Mon, End = Mon.AddDays(2) };
        var c = new PmSchedule.Node { Id = 3, Start = Mon, End = Mon.AddDays(1) };
        var b = new PmSchedule.Node { Id = 2, Start = Mon, End = Mon };

        PmSchedule.Apply([a, b, c], [new PmSchedule.Link(1, 3, 0)]);

        Assert.True(a.Critical);
        Assert.True(c.Critical);
        Assert.False(b.Critical);
        Assert.Equal(4, b.FloatDays); // endet Mo, Projektende Fr
    }

    [Fact]
    public void Cycle_Is_Detected_And_Predecessor_Text_Round_Trips()
    {
        var links = new[] { new PmSchedule.Link(1, 2, 0), new PmSchedule.Link(2, 3, 0) };
        Assert.True(PmSchedule.WouldCreateCycle(links, 3, 1));
        Assert.True(PmSchedule.WouldCreateCycle(links, 2, 2));
        Assert.False(PmSchedule.WouldCreateCycle(links, 1, 3));

        var parsed = PmSchedule.ParsePredecessors("3, 5FS+2; LOG-7-1");
        Assert.Equal([(3, 0), (5, 2), (7, -1)], parsed!);
        Assert.Equal("3, 5+2, 7-1", PmSchedule.FormatPredecessors(parsed!));
        Assert.Null(PmSchedule.ParsePredecessors("drei"));
        Assert.Empty(PmSchedule.ParsePredecessors(" ")!);
    }

    // ---------- Service ----------

    private async Task<(int Project, int A, int B)> TwoTasksAsync()
    {
        var project = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Linie 4 Umbau", Key = "LIN" });
        var a = await _service.CreateTaskAsync(Lead, project, new PmTaskInput { Title = "Planung", StartDate = Mon, DueDate = Mon.AddDays(2) });
        var b = await _service.CreateTaskAsync(Lead, project, new PmTaskInput { Title = "Montage", StartDate = Mon, DueDate = Mon.AddDays(1) });
        return (project, a, b);
    }

    [Fact]
    public async Task Predecessor_Cell_Moves_Successor_And_Rejects_Unknown_Or_Cycle()
    {
        var (project, a, b) = await TwoTasksAsync();

        await _service.UpdateSheetFieldAsync(Lead, b, PmService.SheetFields.Predecessors, "1");
        var sheet = (await _service.GetSheetAsync(project, Lead))!;
        var rowB = sheet.Rows.Single(r => r.Id == b);
        Assert.Equal("1", rowB.Predecessors);
        Assert.Equal(Mon.AddDays(3), rowB.Start);
        Assert.Equal(2, rowB.Duration);
        Assert.Equal(2, sheet.CriticalCount);

        await Assert.ThrowsAsync<PmException>(() => _service.UpdateSheetFieldAsync(Lead, b, PmService.SheetFields.Predecessors, "99"));
        await Assert.ThrowsAsync<PmException>(() => _service.UpdateSheetFieldAsync(Lead, a, PmService.SheetFields.Predecessors, "2"));

        // Vorgaenger in der Zeitachse spaeter: der Nachfolger rueckt mit.
        await _service.RescheduleAsync(Lead, a, Mon.AddDays(7), Mon.AddDays(9));
        rowB = (await _service.GetSheetAsync(project, Lead))!.Rows.Single(r => r.Id == b);
        Assert.Equal(Mon.AddDays(10), rowB.Start);
    }

    [Fact]
    public async Task Dragging_Successor_Snaps_To_Earliest_Or_Adds_Lag()
    {
        var (project, _, b) = await TwoTasksAsync();
        await _service.UpdateSheetFieldAsync(Lead, b, PmService.SheetFields.Predecessors, "1");

        await _service.RescheduleAsync(Lead, b, Mon, Mon.AddDays(1));
        var row = (await _service.GetSheetAsync(project, Lead))!.Rows.Single(r => r.Id == b);
        Assert.Equal(Mon.AddDays(3), row.Start);

        await _service.RescheduleAsync(Lead, b, Mon.AddDays(7), Mon.AddDays(8));
        row = (await _service.GetSheetAsync(project, Lead))!.Rows.Single(r => r.Id == b);
        Assert.Equal(Mon.AddDays(7), row.Start);
        Assert.Equal("1+2", row.Predecessors);

        // Wochenende rueckt auf Montag.
        await _service.UpdateSheetFieldAsync(Lead, b, PmService.SheetFields.Predecessors, "");
        await _service.RescheduleAsync(Lead, b, new DateTime(2026, 11, 7), new DateTime(2026, 11, 8));
        row = (await _service.GetSheetAsync(project, Lead))!.Rows.Single(r => r.Id == b);
        Assert.Equal(new DateTime(2026, 11, 9), row.Start);
    }

    [Fact]
    public async Task Indent_Builds_Epic_And_Subtask_Outline_And_Summary_Rows_Are_Calculated()
    {
        var project = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Linie 4 Umbau", Key = "LIN" });
        var epic = await _service.CreateTaskAsync(Lead, project, new PmTaskInput { Title = "Phase Bau", Type = PmTaskTypes.Epic });
        var a = await _service.CreateTaskAsync(Lead, project, new PmTaskInput { Title = "Fundament", StartDate = Mon, DueDate = Mon.AddDays(1) });
        var b = await _service.CreateTaskAsync(Lead, project, new PmTaskInput { Title = "Anker setzen", StartDate = Mon.AddDays(2), DueDate = Mon.AddDays(4) });

        await _service.IndentAsync(Lead, a);
        await _service.IndentAsync(Lead, b);
        await _service.IndentAsync(Lead, b);
        var rows = (await _service.GetSheetAsync(project, Lead))!.Rows;
        Assert.Equal([epic, a, b], rows.Select(r => r.Id));
        Assert.Equal([0, 1, 2], rows.Select(r => r.Level));
        Assert.Equal(PmTaskTypes.Subtask, rows[2].Type);
        Assert.True(rows[0].HasChildren && rows[1].HasChildren);
        // Wie Smartsheet: eine Zeile mit Unteraufgaben uebernimmt deren Spanne (Mi bis Fr), das Epic ebenso.
        Assert.Equal(Mon.AddDays(2), rows[1].Start);
        Assert.Equal(Mon.AddDays(2), rows[0].Start);
        Assert.Equal(Mon.AddDays(4), rows[0].End);

        await Assert.ThrowsAsync<PmException>(() => _service.RescheduleAsync(Lead, a, Mon.AddDays(7), Mon.AddDays(8)));
        await Assert.ThrowsAsync<PmException>(() => _service.UpdateSheetFieldAsync(Lead, epic, PmService.SheetFields.Duration, "3"));
        await Assert.ThrowsAsync<PmException>(() => _service.IndentAsync(Lead, epic));

        await _service.OutdentAsync(Lead, b);
        await _service.OutdentAsync(Lead, b);
        rows = (await _service.GetSheetAsync(project, Lead))!.Rows;
        Assert.Equal(0, rows.Single(r => r.Id == b).Level);
        Assert.Equal(PmTaskTypes.Task, rows.Single(r => r.Id == b).Type);
    }

    [Fact]
    public async Task Duration_Milestone_And_Done_Status_Update_The_Row()
    {
        var (project, a, _) = await TwoTasksAsync();
        await _service.UpdateSheetFieldAsync(Lead, a, PmService.SheetFields.Duration, "5");
        var row = (await _service.GetSheetAsync(project, Lead))!.Rows.Single(r => r.Id == a);
        Assert.Equal(new DateTime(2026, 11, 6), row.End);

        await _service.UpdateSheetFieldAsync(Lead, a, PmService.SheetFields.Milestone, "true");
        row = (await _service.GetSheetAsync(project, Lead))!.Rows.Single(r => r.Id == a);
        Assert.True(row.IsMilestone);
        Assert.Equal(row.Start, row.End);
        Assert.Equal(0, row.Duration);

        var board = (await _service.GetBoardAsync(project, Lead))!;
        var done = board.Columns.Single(c => c.Category == PmCategories.Done);
        await _service.UpdateSheetFieldAsync(Lead, a, PmService.SheetFields.Status, done.Id.ToString());
        row = (await _service.GetSheetAsync(project, Lead))!.Rows.Single(r => r.Id == a);
        Assert.Equal(100, row.Percent);

        await Assert.ThrowsAsync<PmException>(() => _service.UpdateSheetFieldAsync(Guest, a, PmService.SheetFields.Percent, "50"));
        await Assert.ThrowsAsync<PmException>(() => _service.RescheduleAsync(Guest, a, Mon, Mon));
    }

    [Fact]
    public async Task Panel_Date_Change_Also_Moves_Successor()
    {
        var (project, a, b) = await TwoTasksAsync();
        await _service.UpdateSheetFieldAsync(Lead, b, PmService.SheetFields.Predecessors, "1");
        var card = (await _service.GetTaskAsync(a, Lead))!.Card;
        await _service.UpdateTaskAsync(Lead, a, new PmTaskInput { Title = card.Title, StartDate = Mon, DueDate = Mon.AddDays(4) });

        var row = (await _service.GetSheetAsync(project, Lead))!.Rows.Single(r => r.Id == b);
        Assert.Equal(Mon.AddDays(7), row.Start);
    }

    [Fact]
    public async Task Risks_Score_Level_And_Summary()
    {
        var (project, a, _) = await TwoTasksAsync();
        await _service.SaveRiskAsync(Lead, project, new PmRiskInput { Title = "Lieferverzug Antrieb", Probability = 4, Impact = 5, TaskId = a, OwnerLogin = "anna" });
        var low = await _service.SaveRiskAsync(Lead, project, new PmRiskInput { Title = "Kleiner Mehraufwand", Probability = 1, Impact = 2 });

        var risks = await _service.GetRisksAsync(project, Lead);
        Assert.Equal(["LIN-R1", "LIN-R2"], risks.Select(r => r.Key));
        Assert.Equal(20, risks[0].Score);
        Assert.Equal(PmRiskLevels.Critical, risks[0].Level);
        Assert.Equal("LIN-1", risks[0].TaskKey);
        Assert.Equal(PmRiskLevels.Low, risks[1].Level);

        var sheet = (await _service.GetSheetAsync(project, Lead))!;
        Assert.Equal(PmRiskLevels.Critical, sheet.Rows.Single(r => r.Id == a).RiskLevel);
        Assert.Equal(PmRiskLevels.Critical, sheet.Risks.MaxLevel);
        Assert.Equal(2, sheet.Risks.Active);

        await _service.SaveRiskAsync(Lead, project, new PmRiskInput { Id = risks[0].Id, Title = risks[0].Title, Probability = 4, Impact = 5, Status = PmRiskStatuses.Closed });
        Assert.Equal(PmRiskLevels.Low, (await _service.GetSheetAsync(project, Lead))!.Risks.MaxLevel);

        await Assert.ThrowsAsync<PmException>(() => _service.SaveRiskAsync(Guest, project, new PmRiskInput { Title = "Darf nicht" }));
        await Assert.ThrowsAsync<PmException>(() => _service.SaveRiskAsync(Lead, project, new PmRiskInput { Title = "Fremder Eigner", OwnerLogin = "gast" }));
        await _service.SetRiskArchivedAsync(Lead, low, true);
        Assert.Single(await _service.GetRisksAsync(project, Lead));
        Assert.Equal(20, PmRiskLevels.Score(9, 4)); // gekappt auf 5 x 4
    }

    [Fact]
    public async Task Vorhaben_Import_Creates_Private_Projects_With_Hermes_And_Tasks_Then_Updates()
    {
        const string vorhaben = "﻿ID\tVorhaben\tHERMES-Stufe\tAuftraggeber\tAnwendervertreter\tFreigabe\tAbnahme\tAbschluss\tAmpel\tStand in einem Satz\tNächster Entscheid\tErledigt, wenn\n" +
            "PM-01\tZLO03: Auswertung korrigieren\tVorhaben\tfestlegen\tAnn-Katrin\t14.08.2026\toffen\toffen\tGelb\tTest steht aus\tTesten\tMengen stimmen\n" +
            "PM-02\tZC12: Nullmengen\tKleinauftrag\t-\t-\t-\t-\t-\tRot\tUrsache eingegrenzt\tEinspielen\tNullmengen korrekt\n";
        const string todo = "Nr\tDringlichkeit\tBereich\tID\tThema\tWer\tPrio\tStatus\tFällig\tNächster Schritt\tErledigt am\tVorhaben\tHERMES-Stufe\n" +
            "9\tHeute\tSAP\tPM-01\tZLO03 Mengen\tIngo\tHoch\tWartet auf Import\t\tImport\t\tPM-01\tVorhaben\n" +
            "124\tDiese Woche\tSAP\tPM-01\tZLO03 Disponent\tIngo\tMittel\tErledigt\t30.10.2026\tArmin\t29.10.2026\tPM-01\tVorhaben\n" +
            "50\tSpäter\tHR\t\tFremd\tSonja\tMittel\tOffen\t\t\t\t\t\n";

        var result = await _service.SyncVorhabenAsync(Lead, vorhaben, todo);
        Assert.Equal(2, result.ProjectsCreated);
        Assert.Equal(2, result.TasksCreated);
        Assert.Equal(["PM01", "PM02"], result.Keys);

        var projects = await _service.GetProjectsAsync(Lead);
        var pm01 = projects.Single(p => p.Key == "PM01");
        Assert.Equal(PmVisibility.Private, pm01.Visibility);
        Assert.DoesNotContain(await _service.GetProjectsAsync(Guest), p => p.Key == "PM01");
        var board = (await _service.GetBoardAsync(pm01.Id, Lead))!;
        Assert.Equal(PmHermesStages.Undertaking, board.Project.Hermes!.Stage);
        Assert.Equal(PmTraffic.Yellow, board.Project.Hermes.Traffic);
        Assert.Equal(new DateTime(2026, 8, 14), board.Project.Hermes.ReleasedOn);
        Assert.Equal(string.Empty, board.Project.Hermes.Sponsor);
        Assert.Equal(2, board.Tasks.Count);

        var again = await _service.SyncVorhabenAsync(Lead, vorhaben.Replace("Test steht aus", "Import offen"), todo);
        Assert.Equal(0, again.ProjectsCreated);
        Assert.Equal(2, again.ProjectsUpdated);
        Assert.Equal(0, again.TasksCreated);
        board = (await _service.GetBoardAsync(pm01.Id, Lead))!;
        Assert.Equal("Import offen", board.Project.Hermes!.StatusLine);
        Assert.Equal(2, board.Tasks.Count);
    }

    [Fact]
    public async Task Baseline_Freezes_Current_Dates()
    {
        var (project, a, _) = await TwoTasksAsync();
        await _service.SetBaselineAsync(Lead, project, clear: false);
        await _service.RescheduleAsync(Lead, a, Mon.AddDays(7), Mon.AddDays(9));
        var row = (await _service.GetSheetAsync(project, Lead))!.Rows.Single(r => r.Id == a);
        Assert.Equal(Mon, row.BaselineStart);
        Assert.Equal(Mon.AddDays(7), row.Start);
        await Assert.ThrowsAsync<PmException>(() => _service.SetBaselineAsync(Guest, project, clear: true));
    }

    // ---------- Raster und Gantt: Rechenhilfen der Oberflaeche (PmSheetLayout) ----------

    private static PmSheetRow SheetRow(int id, int level, DateTime? start = null, DateTime? end = null, int duration = 0,
        bool milestone = false, string title = "Zeile", bool hasChildren = false, string predecessors = "") =>
        new(id, $"PM-{id}", id, "Task", title, level, null, hasChildren, start, end, duration, milestone, 0, predecessors,
            false, 0, 1, "Offen", "Todo", "Medium", string.Empty, string.Empty, [], false, null, null, string.Empty, 0);

    [Fact]
    public void Layout_Range_Starts_On_Monday_And_Pads_For_Dragging()
    {
        var today = new DateTime(2026, 10, 29); // Donnerstag
        var (from, to) = PmSheetLayout.Range(Mon, Mon.AddDays(4), today, PmSheetLayout.Zooms.Weeks);
        Assert.Equal(DayOfWeek.Monday, from.DayOfWeek);
        Assert.True(from <= today.AddDays(-14));
        Assert.Equal(DayOfWeek.Sunday, to.DayOfWeek);
        Assert.True(to >= Mon.AddDays(4 + 91));

        var (mFrom, mTo) = PmSheetLayout.Range(null, null, today, PmSheetLayout.Zooms.Months);
        Assert.Equal(1, mFrom.Day);
        Assert.Equal(DateTime.DaysInMonth(mTo.Year, mTo.Month), mTo.Day);
        Assert.True(mTo > today.AddDays(56));
    }

    [Fact]
    public void Layout_Pixels_And_Dates_Round_Trip_With_Inclusive_End()
    {
        var dw = PmSheetLayout.DayWidth(PmSheetLayout.Zooms.Days);
        Assert.Equal(0, PmSheetLayout.X(Mon, Mon, dw));
        Assert.Equal(3 * dw, PmSheetLayout.X(Mon, Mon.AddDays(3), dw));
        Assert.Equal(dw, PmSheetLayout.Width(Mon, Mon, dw));
        Assert.Equal(5 * dw, PmSheetLayout.Width(Mon, Mon.AddDays(4), dw));
        Assert.Equal(Mon.AddDays(3), PmSheetLayout.DayAt(Mon, 3 * dw + dw - 0.1, dw));
        Assert.Equal(Mon.AddDays(-1), PmSheetLayout.DayAt(Mon, -1, dw));
    }

    [Fact]
    public void Layout_Scale_Covers_The_Range_And_Weeks_Start_On_Monday()
    {
        var (from, to) = PmSheetLayout.Range(Mon, Mon.AddDays(30), Mon, PmSheetLayout.Zooms.Weeks);
        var dw = PmSheetLayout.DayWidth(PmSheetLayout.Zooms.Weeks);
        var total = PmSheetLayout.Width(from, to, dw);
        var weeks = PmSheetLayout.BottomScale(from, to, PmSheetLayout.Zooms.Weeks);
        Assert.All(weeks, w => Assert.Equal(DayOfWeek.Monday, w.Day.DayOfWeek));
        Assert.Equal(total, weeks.Sum(w => w.Width), 3);
        var months = PmSheetLayout.TopScale(from, to, PmSheetLayout.Zooms.Weeks);
        Assert.Equal(total, months.Sum(m => m.Width), 3);
        Assert.Equal(0, months[0].X);
        Assert.Equal(45, PmSheetLayout.IsoWeek(Mon));
    }

    [Fact]
    public void Layout_NonWorkdays_Include_Weekend_And_Christmas()
    {
        var from = new DateTime(2026, 12, 21); // Montag
        var runs = PmSheetLayout.NonWorkdayRuns(from, from.AddDays(13));
        // Fr 25.12. bis So 27.12. am Stueck, dann Neujahr (Fr 1.1.) mit dem Wochenende.
        Assert.Contains((4, 3), runs);
        Assert.Contains((11, 3), runs);
        Assert.True(PmSheetLayout.IsHoliday(new DateTime(2026, 12, 25)));
        Assert.False(PmSheetLayout.IsHoliday(new DateTime(2026, 12, 27)));
        Assert.Contains("5", PmSheetLayout.NonWorkdayIndexes(from, from.AddDays(6)).Split(','));
    }

    [Fact]
    public void Layout_Collapse_And_Filter_Keep_The_Outline_Readable()
    {
        var rows = new[] { SheetRow(1, 0, hasChildren: true, title: "Epic"), SheetRow(2, 1, hasChildren: true), SheetRow(3, 2, title: "Treffer"), SheetRow(4, 1), SheetRow(5, 0) };
        Assert.Equal([1, 5], PmSheetLayout.VisibleRows(rows, new HashSet<int> { 1 }, null).Select(r => r.Id));
        Assert.Equal([1, 2, 4, 5], PmSheetLayout.VisibleRows(rows, new HashSet<int> { 2 }, null).Select(r => r.Id));
        // Filter: Treffer plus seine Elternzeilen.
        Assert.Equal([1, 2, 3], PmSheetLayout.VisibleRows(rows, new HashSet<int>(), r => r.Title == "Treffer").Select(r => r.Id));
        var parents = PmSheetLayout.OutlineParents(rows);
        Assert.Equal(2, parents[3]);
        Assert.Equal(1, parents[4]);
        Assert.Null(parents[5]);
    }

    [Fact]
    public void Layout_Move_Target_Stays_Within_The_Same_Group()
    {
        var rows = new[] { SheetRow(1, 0, hasChildren: true), SheetRow(2, 1), SheetRow(3, 1), SheetRow(4, 1), SheetRow(5, 0) };
        Assert.Equal((true, (int?)2), PmSheetLayout.MoveTarget(rows, 3, up: true));
        Assert.Equal((false, (int?)null), PmSheetLayout.MoveTarget(rows, 2, up: true));
        Assert.Equal((true, (int?)4), PmSheetLayout.MoveTarget(rows, 2, up: false));
        Assert.Equal((true, (int?)null), PmSheetLayout.MoveTarget(rows, 3, up: false));
        Assert.Equal((false, (int?)null), PmSheetLayout.MoveTarget(rows, 4, up: false));
        Assert.Equal((true, (int?)1), PmSheetLayout.MoveTarget(rows, 5, up: true));
    }

    [Fact]
    public void Layout_Drop_Keeps_Working_Day_Duration_When_Moving()
    {
        // Mo bis Fr (5 Arbeitstage) um zwei Tage verschoben: Mi bis Di der Folgewoche, nicht Mi bis So.
        var row = SheetRow(1, 0, Mon, Mon.AddDays(4), 5);
        Assert.Equal((Mon.AddDays(2), Mon.AddDays(8)), PmSheetLayout.DropDates(Mon, row, PmSheetLayout.DragModes.Move, 2, 6));
        // Linker Griff hinter das Ende: Start = Ende.
        Assert.Equal((Mon.AddDays(4), Mon.AddDays(4)), PmSheetLayout.DropDates(Mon, row, PmSheetLayout.DragModes.Start, 9, 4));
        Assert.Equal((Mon, Mon.AddDays(9)), PmSheetLayout.DropDates(Mon, row, PmSheetLayout.DragModes.End, 0, 9));
        var milestone = SheetRow(2, 0, Mon, Mon, 0, milestone: true);
        Assert.Equal((Mon.AddDays(7), Mon.AddDays(7)), PmSheetLayout.DropDates(Mon, milestone, PmSheetLayout.DragModes.Move, 5, 5));
        Assert.Null(PmSheetLayout.DropDates(Mon, SheetRow(3, 0), PmSheetLayout.DragModes.Move, 1, 1));
    }

    [Fact]
    public void Layout_Arrow_Runs_Right_Angled_And_Back_Between_Rows()
    {
        var h = PmSheetLayout.RowHeight;
        Assert.Equal($"M100 {h / 2} H108 V{h + h / 2} H200", PmSheetLayout.ArrowPath(0, 100, 1, 200));
        // Nachfolger beginnt links vom Vorgaengerende: Weg zwischen den Zeilen zurueck.
        Assert.Equal($"M100 {h / 2} H108 V{h} H72 V{h + h / 2} H80", PmSheetLayout.ArrowPath(0, 100, 1, 80));
        var edges = PmSheetLayout.BarEdges(Mon, SheetRow(1, 0, Mon.AddDays(1), Mon.AddDays(1), 0, milestone: true), 30);
        Assert.Equal((45 - PmSheetLayout.MilestoneHalf, 45 + PmSheetLayout.MilestoneHalf), edges);
    }

    [Fact]
    public void Layout_Add_Predecessor_Appends_Once()
    {
        Assert.Equal("3", PmSheetLayout.AddPredecessor(string.Empty, 3));
        Assert.Equal("3, 5+2", PmSheetLayout.AddPredecessor("5+2", 3));
        Assert.Null(PmSheetLayout.AddPredecessor("3, 5+2", 5));
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
