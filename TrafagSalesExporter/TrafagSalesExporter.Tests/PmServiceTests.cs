using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.Projects;

namespace TrafagSalesExporter.Tests;

public sealed class PmServiceTests : IDisposable
{
    private static readonly PmUser Lead = new("anna", "Muster, Anna", false);
    private static readonly PmUser Ben = new("ben", "Ben Beispiel", false);
    private static readonly PmUser Guest = new("gast", "Gast Leser", false);
    private static readonly PmUser Admin = new("chef", "Chef Admin", true);

    private readonly SqliteConnection _connection;
    private readonly PmService _service;
    private readonly DbContextOptions<AppDbContext> _options;

    public PmServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
        // Die Schema-SQL muss die Tabellen auch ohne EF anlegen (bestehende Produktiv-DB).
        foreach (var table in new[] { "PmProjects", "PmMembers", "PmColumns", "PmSprints", "PmTasks", "PmChecklistItems", "PmComments", "PmActivities", "PmHermes" })
            db.Database.ExecuteSqlRaw($"DROP TABLE {table}");
        new DatabaseSchemaMaintenanceService().EnsureSchema(db);
        _service = new PmService(new TestDbContextFactory(_options), new PmNotifier());
    }

    [Fact]
    public async Task Create_Project_From_Template_Makes_Lead_Columns_And_Sprint()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Logistik Umbau Halle 3", Template = PmTemplates.Scrum });
        var board = (await _service.GetBoardAsync(id, Lead))!;

        Assert.Equal("LUH3", board.Project.Key);
        Assert.Equal(["Zu erledigen", "In Arbeit", "Test", "Erledigt"], board.Columns.Select(c => c.Name));
        Assert.Equal(PmCategories.Done, board.Columns[^1].Category);
        Assert.Single(board.Sprints);
        Assert.True(board.CanEdit);
        Assert.True(board.CanManage);
        var lead = Assert.Single(board.Project.Members);
        Assert.Equal("anna", lead.Login);

        await Assert.ThrowsAsync<PmException>(() => _service.CreateProjectAsync(Ben, new PmProjectInput { Name = "Noch eins", Key = "LUH3" }));
        await Assert.ThrowsAsync<PmException>(() => _service.CreateProjectAsync(Ben, new PmProjectInput { Name = "Falscher Key", Key = "1AB" }));
        await Assert.ThrowsAsync<PmException>(() => _service.CreateProjectAsync(PmUser.Anonymous, new PmProjectInput { Name = "Ohne Login" }));
    }

    [Fact]
    public async Task Everyone_Sees_Only_Members_Edit_Only_Lead_Manages()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Einkauf Rahmenvertrag", Key = "EINK" });

        var guestBoard = (await _service.GetBoardAsync(id, Guest))!;
        Assert.False(guestBoard.CanEdit);
        await Assert.ThrowsAsync<PmException>(() => _service.CreateTaskAsync(Guest, id, new PmTaskInput { Title = "Darf nicht" }));
        await Assert.ThrowsAsync<PmException>(() => _service.AddMemberAsync(Ben, id, "ben", "Ben"));

        await _service.AddMemberAsync(Lead, id, "TRAFAG\\Ben", "Ben Beispiel");
        var taskId = await _service.CreateTaskAsync(Ben, id, new PmTaskInput { Title = "Angebote einholen", AssigneeLogin = "ben" });
        Assert.Equal("EINK-1", (await _service.GetTaskAsync(taskId, Ben))!.Card.Key);
        await Assert.ThrowsAsync<PmException>(() => _service.AddColumnAsync(Ben, id, "Freigabe", PmCategories.InProgress));
        await Assert.ThrowsAsync<PmException>(() => _service.CreateTaskAsync(Ben, id, new PmTaskInput { Title = "Fremd zuweisen", AssigneeLogin = "gast" }));

        // Kommentieren darf jeder Angemeldete, Admin darf alles.
        await _service.AddCommentAsync(Guest, taskId, "Gibt es schon einen Lieferanten?");
        await _service.AddColumnAsync(Admin, id, "Freigabe", PmCategories.InProgress);
        await Assert.ThrowsAsync<PmException>(() => _service.RemoveMemberAsync(Lead, id, "anna"));
    }

    [Fact]
    public async Task Numbers_Count_Up_And_Moving_Sets_Rank_And_Completion()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Board Test", Key = "BT" });
        var a = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "A" });
        var b = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "B" });
        var c = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "C" });
        var board = (await _service.GetBoardAsync(id, Lead))!;
        Assert.Equal(["BT-1", "BT-2", "BT-3"], board.Tasks.Select(t => t.Key));

        var todo = board.Columns[0].Id;
        var done = board.Columns[^1].Id;
        // C vor A ziehen
        await _service.MoveTaskAsync(Lead, c, todo, a);
        board = (await _service.GetBoardAsync(id, Lead))!;
        Assert.Equal([c, a, b], board.Tasks.Where(t => t.ColumnId == todo).OrderBy(t => t.Rank).Select(t => t.Id));

        await _service.MoveTaskAsync(Lead, b, done, null);
        var moved = (await _service.GetTaskAsync(b, Lead))!.Card;
        Assert.Equal(PmCategories.Done, moved.Category);
        Assert.NotNull(moved.CompletedAtUtc);
        var summary = (await _service.GetProjectsAsync(Lead)).Single();
        Assert.Equal(33, summary.ProgressPercent);

        await _service.MoveTaskAsync(Lead, b, todo, null);
        Assert.Null((await _service.GetTaskAsync(b, Lead))!.Card.CompletedAtUtc);
    }

    [Fact]
    public async Task Update_Logs_Changes_And_Overdue_Is_Detected()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Historie", Key = "HIS" });
        var task = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Bericht" });
        var input = new PmTaskInput { Title = "Bericht Q4", Priority = PmPriorities.High, DueDate = DateTime.Today.AddDays(-2), Labels = "finance, Finance, quartal", AssigneeLogin = "anna" };
        await _service.UpdateTaskAsync(Lead, task, input);

        var detail = (await _service.GetTaskAsync(task, Lead))!;
        Assert.True(detail.Card.IsOverdue);
        Assert.Equal(["finance", "quartal"], detail.Card.Labels);
        Assert.Equal("Muster, Anna", detail.Card.AssigneeName);
        Assert.Contains(detail.Activity, a => a.Text.Contains("Priorität hoch") && a.Text.Contains("Titel"));

        var mine = Assert.Single(await _service.GetMyTasksAsync(Lead));
        Assert.Equal(task, mine.Card.Id);
    }

    [Fact]
    public async Task Checklist_Subtasks_And_Epics_Roll_Up()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Hierarchie", Key = "HIE" });
        var epic = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Neue Linie", Type = PmTaskTypes.Epic });
        var story = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Layout", Type = PmTaskTypes.Story, EpicId = epic });
        var sub = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Masse aufnehmen", ParentId = story });
        var item = await _service.AddChecklistItemAsync(Lead, story, "Plan drucken");
        await _service.AddChecklistItemAsync(Lead, story, "Plan freigeben");
        await _service.ToggleChecklistItemAsync(Lead, item);

        var board = (await _service.GetBoardAsync(id, Lead))!;
        Assert.Single(board.Epics);
        var card = board.Tasks.Single(t => t.Id == story);
        Assert.Equal("Neue Linie", card.EpicTitle);
        Assert.Equal((1, 2), (card.ChecklistDone, card.ChecklistTotal));
        Assert.Equal((0, 1), (card.SubtasksDone, card.SubtasksTotal));
        Assert.Equal(PmTaskTypes.Subtask, board.Tasks.Single(t => t.Id == sub).Type);
        await Assert.ThrowsAsync<PmException>(() => _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Falsches Epic", EpicId = story }));

        await _service.SetTaskArchivedAsync(Lead, story, true);
        board = (await _service.GetBoardAsync(id, Lead))!;
        Assert.DoesNotContain(board.Tasks, t => t.Id == story || t.Id == sub);
    }

    [Fact]
    public async Task Sprint_Start_Complete_Moves_Open_Work()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Scrum Team", Key = "SCR", Template = PmTemplates.Scrum });
        var board = (await _service.GetBoardAsync(id, Lead))!;
        var sprint1 = board.Sprints[0].Id;
        var a = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "A", StoryPoints = 3 });
        var b = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "B", StoryPoints = 5 });
        await _service.PlanTaskAsync(Lead, a, sprint1, null);
        await _service.PlanTaskAsync(Lead, b, sprint1, a);
        board = (await _service.GetBoardAsync(id, Lead))!;
        Assert.Equal([b, a], board.Tasks.Where(t => t.SprintId == sprint1).OrderBy(t => t.Rank).Select(t => t.Id));

        await _service.StartSprintAsync(Lead, sprint1, DateTime.Today.AddDays(-3), DateTime.Today.AddDays(10), "Pilot läuft");
        var sprint2 = await _service.CreateSprintAsync(Lead, id, "", "", null, null);
        await Assert.ThrowsAsync<PmException>(() => _service.StartSprintAsync(Lead, sprint2, DateTime.Today, DateTime.Today.AddDays(13), ""));

        await _service.MoveTaskAsync(Lead, a, board.Columns[^1].Id, null);
        var moved = await _service.CompleteSprintAsync(Lead, sprint1, sprint2);
        Assert.Equal(1, moved);
        board = (await _service.GetBoardAsync(id, Lead))!;
        Assert.Equal(sprint2, board.Tasks.Single(t => t.Id == b).SprintId);
        Assert.Equal(PmSprintStates.Closed, board.Sprints.Single(s => s.Id == sprint1).State);
        Assert.Equal("SCR Sprint 2", board.Sprints.Single(s => s.Id == sprint2).Name);
    }

    [Fact]
    public async Task Done_Task_In_Closed_Sprint_And_Task_Of_Removed_Member_Stay_Editable()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Nachlauf", Key = "NL", Template = PmTemplates.Scrum });
        await _service.AddMemberAsync(Lead, id, "ben", "Ben Beispiel");
        var board = (await _service.GetBoardAsync(id, Lead))!;
        var sprint = board.Sprints[0].Id;
        var done = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Fertig", SprintId = sprint });
        var bens = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Bens Aufgabe", AssigneeLogin = "ben" });
        await _service.StartSprintAsync(Lead, sprint, DateTime.Today, DateTime.Today.AddDays(13), "");
        await _service.MoveTaskAsync(Lead, done, board.Columns[^1].Id, null);
        await _service.CompleteSprintAsync(Lead, sprint, null);
        await _service.RemoveMemberAsync(Lead, id, "ben");

        var doneInput = new PmTaskInput { Title = "Fertig, nachgetragen", SprintId = sprint, ColumnId = board.Columns[^1].Id };
        await _service.UpdateTaskAsync(Lead, done, doneInput);
        var bensInput = new PmTaskInput { Title = "Bens Aufgabe", AssigneeLogin = "ben", DueDate = DateTime.Today.AddDays(3) };
        await _service.UpdateTaskAsync(Lead, bens, bensInput);

        Assert.Equal("Fertig, nachgetragen", (await _service.GetTaskAsync(done, Lead))!.Card.Title);
        Assert.Equal("ben", (await _service.GetTaskAsync(bens, Lead))!.Card.AssigneeLogin);
        // Neu zuweisen an ein Nicht-Mitglied bleibt verboten.
        await Assert.ThrowsAsync<PmException>(() => _service.UpdateTaskAsync(Lead, done, new PmTaskInput { Title = "x", SprintId = sprint, AssigneeLogin = "ben" }));
    }

    [Fact]
    public void Burndown_Uses_Points_And_Stops_Today()
    {
        var today = new DateTime(2026, 10, 9);
        var sprint = new PmSprintView(1, "S", "", today.AddDays(-2), today.AddDays(2), PmSprintStates.Active, null);
        PmCard Card(int points, DateTime? done) => new(1, 1, "X-1", 1, "Task", "t", 1, "c", done is null ? "Todo" : "Done", "Medium", "", "", [],
            null, null, points, null, "", null, "", 1, 0, "", 0, 0, 0, 0, 0, today, today, done?.ToUniversalTime(), false);
        var points = PmService.Burndown(sprint, [Card(3, today.AddDays(-1).AddHours(10)), Card(5, null)], today);

        Assert.Equal(5, points.Count);
        Assert.Equal(8, points[0].Ideal);
        Assert.Equal(0, points[^1].Ideal);
        Assert.Equal(8, points[0].Remaining);
        Assert.Equal(5, points[1].Remaining);
        Assert.Equal(5, points[2].Remaining);
        Assert.Null(points[3].Remaining);
    }

    [Fact]
    public async Task Columns_Can_Be_Reordered_And_Deleted_Cards_Move()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Spalten", Key = "SP" });
        var board = (await _service.GetBoardAsync(id, Lead))!;
        var review = board.Columns.Single(c => c.Name == "Review").Id;
        var task = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Prüfen", ColumnId = review });

        await _service.MoveColumnAsync(Lead, review, -1);
        board = (await _service.GetBoardAsync(id, Lead))!;
        Assert.Equal(["Zu erledigen", "Review", "In Arbeit", "Erledigt"], board.Columns.Select(c => c.Name));

        await _service.UpdateColumnAsync(Lead, review, "Review", PmCategories.InProgress, 2);
        await _service.DeleteColumnAsync(Lead, review);
        board = (await _service.GetBoardAsync(id, Lead))!;
        Assert.Equal("In Arbeit", board.Tasks.Single(t => t.Id == task).ColumnName);
    }

    [Theory]
    [InlineData("Logistik Umbau Halle 3", "LUH3")]
    [InlineData("Einkauf", "EINK")]
    [InlineData("Öl", "OL")]
    [InlineData("3D Druck", "P3D")]
    public void Key_Suggestion_Is_Valid(string name, string key) => Assert.Equal(key, PmService.SuggestKey(name));

    [Fact]
    public void Rank_Between_Inserts_In_The_Middle()
    {
        var list = new List<(int, double)> { (1, 1000), (2, 2000), (3, 3000) };
        Assert.Equal(1500, PmService.RankBetween(list, 2));
        Assert.Equal(0, PmService.RankBetween(list, 1));
        Assert.Equal(4000, PmService.RankBetween(list, null));
        Assert.Equal(1000, PmService.RankBetween([], null));
    }

    [Fact]
    public void Ad_Name_Is_Shown_First_Name_First() =>
        Assert.Equal("Ingo Kohler", PmText.PersonName("Kohler, Ingo"));

    [Fact]
    public void Old_Project_Menu_Group_Is_Replaced_By_Top_Level_Link()
    {
        using var db = new AppDbContext(_options);
        db.NavigationMenuItems.RemoveRange(db.NavigationMenuItems);
        db.NavigationMenuItems.Add(new NavigationMenuItem { Key = "poor-mans-project-management", TitleDe = "Poor Man's Project Management Suite", TitleEn = "x", ItemType = NavigationMenuItemTypes.Group, Icon = "Assignment", SortOrder = 40 });
        db.NavigationMenuItems.Add(new NavigationMenuItem { Key = "projects", ParentKey = "poor-mans-project-management", TitleDe = "Projekte", TitleEn = "Projects", ItemType = NavigationMenuItemTypes.Link, Href = "projekte", Icon = "ViewKanban", SortOrder = 10 });
        db.SaveChanges();

        new DatabaseSeedService().SeedDefaults(db);

        Assert.False(db.NavigationMenuItems.Any(x => x.Key == "poor-mans-project-management"));
        var projects = db.NavigationMenuItems.Single(x => x.Key == "projects");
        Assert.Null(projects.ParentKey);
        Assert.Equal("Trafag Projekte", projects.TitleDe);
    }

    [Fact]
    public async Task Hermes_Details_Are_Saved_By_Lead_And_Shown_In_Summary()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Auswertung Phase-Out", Key = "PHO" });
        Assert.Null((await _service.GetProjectsAsync(Lead)).Single(p => p.Id == id).Hermes);

        var input = new PmHermesInput
        {
            Stage = PmHermesStages.Undertaking, Traffic = PmTraffic.Yellow, EffortDays = 12,
            Sponsor = "  Leitung Logistik ", UserRepresentative = "Disponent", StatusLine = "Umsetzung läuft",
            NextDecision = "Abnahmetermin", DoneWhen = "Stichprobe stimmt", ReleasedOn = new DateTime(2026, 10, 13, 15, 0, 0)
        };
        await Assert.ThrowsAsync<PmException>(() => _service.SaveHermesAsync(Ben, id, input));
        await _service.SaveHermesAsync(Lead, id, input);

        var hermes = (await _service.GetProjectsAsync(Guest)).Single(p => p.Id == id).Hermes!;
        Assert.Equal(PmHermesStages.Undertaking, hermes.Stage);
        Assert.Equal("Leitung Logistik", hermes.Sponsor);
        Assert.Equal(new DateTime(2026, 10, 13), hermes.ReleasedOn);
        Assert.Equal(PmHermesPhases.Execution, hermes.Phase);

        // Zweites Speichern aktualisiert dieselbe Zeile statt eine neue anzulegen.
        input.AcceptedOn = new DateTime(2026, 11, 2);
        await _service.SaveHermesAsync(Admin, id, input);
        using var db = new AppDbContext(_options);
        Assert.Equal(1, db.PmHermes.Count(h => h.ProjectId == id));
        Assert.Equal(PmHermesPhases.Introduction, (await _service.GetProjectsAsync(Lead)).Single(p => p.Id == id).Hermes!.Phase);
    }

    [Fact]
    public void Hermes_Decision_Points_Must_Follow_Each_Other()
    {
        Assert.Throws<PmException>(() => PmService.ValidateHermes(new PmHermesInput { AcceptedOn = new DateTime(2026, 10, 1) }));
        Assert.Throws<PmException>(() => PmService.ValidateHermes(new PmHermesInput { ReleasedOn = new DateTime(2026, 10, 1), ClosedOn = new DateTime(2026, 10, 2) }));
        Assert.Throws<PmException>(() => PmService.ValidateHermes(new PmHermesInput { ReleasedOn = new DateTime(2026, 10, 5), AcceptedOn = new DateTime(2026, 10, 1) }));
        Assert.Throws<PmException>(() => PmService.ValidateHermes(new PmHermesInput { DoneWhen = new string('x', PmService.HermesTextMax + 1) }));

        var clean = PmService.ValidateHermes(new PmHermesInput { Stage = "Gross", Traffic = "Blau", EffortDays = -3 });
        Assert.Equal(string.Empty, clean.Stage);
        Assert.Equal(string.Empty, clean.Traffic);
        Assert.Equal(0, clean.EffortDays);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(4, PmHermesStages.SmallOrder)]
    [InlineData(5, PmHermesStages.Undertaking)]
    [InlineData(40, PmHermesStages.Undertaking)]
    [InlineData(41, PmHermesStages.Project)]
    public void Hermes_Stage_Is_Suggested_From_Effort(int days, string expected) =>
        Assert.Equal(expected, PmHermesStages.Suggest(days));

    [Fact]
    public async Task Hermes_Demo_Is_Seeded_Once_And_Stays_Away_When_Archived()
    {
        using (var db = new AppDbContext(_options))
        {
            await PmService.SeedHermesDemoAsync(db, "koi", "Kohler, Ingo");
            await PmService.SeedHermesDemoAsync(db, "koi", "Kohler, Ingo");
        }

        var koi = new PmUser("koi", "Kohler, Ingo", false);
        var demo = Assert.Single(await _service.GetProjectsAsync(koi), p => p.Key == PmService.DemoKey);
        Assert.Equal(PmHermesStages.Undertaking, demo.Hermes!.Stage);
        Assert.Equal(PmHermesPhases.Execution, demo.Hermes.Phase);
        Assert.Equal(5, demo.Total);
        Assert.True(demo.IsMember);
        var board = (await _service.GetBoardAsync(demo.Id, koi))!;
        Assert.True(board.CanManage);

        await _service.SetProjectArchivedAsync(koi, demo.Id, true);
        using (var db = new AppDbContext(_options))
            await PmService.SeedHermesDemoAsync(db, "koi", "Kohler, Ingo");
        Assert.Single(await _service.GetProjectsAsync(koi, includeArchived: true), p => p.Key == PmService.DemoKey);
    }

    [Fact]
    public async Task Private_Project_Is_Invisible_To_Others_And_Admins_Readers_See_But_Cannot_Edit()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Meine Wochen-Todo", Key = "WTODO", Visibility = PmVisibility.Private });
        var taskId = await _service.CreateTaskAsync(Lead, id, new PmTaskInput { Title = "Geheim" });

        Assert.Single(await _service.GetProjectsAsync(Lead), p => p.Id == id && p.IsPrivate);
        Assert.DoesNotContain(await _service.GetProjectsAsync(Ben), p => p.Id == id);
        Assert.DoesNotContain(await _service.GetProjectsAsync(Admin), p => p.Id == id);
        Assert.Null(await _service.GetBoardAsync(id, Ben));
        Assert.Null(await _service.GetBoardAsync(id, Admin));
        Assert.Null(await _service.GetTaskAsync(taskId, Admin));
        await Assert.ThrowsAsync<PmException>(() => _service.CreateTaskAsync(Admin, id, new PmTaskInput { Title = "Admin darf nicht" }));
        await Assert.ThrowsAsync<PmException>(() => _service.UpdateProjectAsync(Admin, id, new PmProjectInput { Name = "Umbenannt", Visibility = PmVisibility.Public }));

        await _service.AddMemberAsync(Lead, id, "ben", "Ben Beispiel", PmRoles.Viewer);
        var benBoard = (await _service.GetBoardAsync(id, Ben))!;
        Assert.False(benBoard.CanEdit);
        Assert.False(benBoard.CanManage);
        Assert.Equal(PmRoles.Viewer, benBoard.Project.Members.Single(m => m.Login == "ben").Role);
        Assert.NotNull(await _service.GetTaskAsync(taskId, Ben));
        await Assert.ThrowsAsync<PmException>(() => _service.CreateTaskAsync(Ben, id, new PmTaskInput { Title = "Leser darf nicht" }));

        await _service.SetMemberRoleAsync(Lead, id, "ben", PmRoles.Member);
        Assert.True((await _service.GetBoardAsync(id, Ben))!.CanEdit);
        await _service.CreateTaskAsync(Ben, id, new PmTaskInput { Title = "Jetzt schon" });

        await _service.UpdateProjectAsync(Lead, id, new PmProjectInput { Name = "Meine Wochen-Todo", Visibility = PmVisibility.Public });
        Assert.Contains(await _service.GetProjectsAsync(Guest), p => p.Id == id && !p.IsPrivate);
        Assert.True((await _service.GetBoardAsync(id, Admin))!.CanEdit);
    }

    [Fact]
    public async Task Sync_WochenTodo_Creates_Then_Updates_By_Number()
    {
        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Wochen Todo", Key = "WT", Visibility = PmVisibility.Private });
        const string header = "Nr\tDringlichkeit\tBereich\tID\tThema\tWer\tPrio\tStatus\tFällig\tNächster Schritt\tErledigt am\tVorhaben\tHERMES-Stufe";
        var first = string.Join("\n",
            header,
            "1\tErledigt\tFinance ES\tISS-004.2\tSpanien Buchungsdatum\tIngo\tMittel\tErledigt\t\tEntscheid 02.09.\t02.09.2026\tPM-05\tProjekt",
            "94\tHeute\tSAP ZLO03\tPM-01\tZLO03 Tageszeit\tIngo\tHoch\tOffen\t2026-10-19\tNur lesend prüfen\t\t\t",
            "95\tDiese Woche\tSAP Berechtigung\t\tWolfurt Rahmenvertrag\tIngo\tHoch\tIn P76, Ladelauf offen\t\tSU53 holen\t\t\t",
            "x\tkaputt");

        var result = await _service.SyncWochenTodoAsync(Lead, id, "﻿" + first);
        Assert.Equal(new PmImportResult(3, 0, 0, 0), result);
        var board = (await _service.GetBoardAsync(id, Lead))!;
        var done = board.Tasks.Single(t => t.Title == "Spanien Buchungsdatum");
        Assert.Equal(PmCategories.Done, done.Category);
        var today = board.Tasks.Single(t => t.Title == "ZLO03 Tageszeit");
        Assert.Equal(PmPriorities.Highest, today.Priority);
        Assert.Equal(new DateTime(2026, 10, 19), today.DueDate);
        Assert.Equal("anna", today.AssigneeLogin);
        Assert.Contains("SAP ZLO03", today.Labels);
        Assert.Equal(PmCategories.InProgress, board.Tasks.Single(t => t.Title == "Wolfurt Rahmenvertrag").Category);

        var second = first.Replace("94\tHeute\tSAP ZLO03\tPM-01\tZLO03 Tageszeit\tIngo\tHoch\tOffen", "94\tHeute\tSAP ZLO03\tPM-01\tZLO03 Tageszeit\tIngo\tHoch\tErledigt")
            + "\n96\tNächste Woche\tPlattform\t\tNeue Zeile\tIngo\tNiedrig\tOffen\t\t\t\t\t";
        result = await _service.SyncWochenTodoAsync(Lead, id, second);
        Assert.Equal(new PmImportResult(1, 1, 2, 0), result);
        board = (await _service.GetBoardAsync(id, Lead))!;
        Assert.Equal(4, board.Tasks.Count);
        Assert.Equal(PmCategories.Done, board.Tasks.Single(t => t.Title == "ZLO03 Tageszeit").Category);
        Assert.Equal(PmPriorities.Low, board.Tasks.Single(t => t.Title == "Neue Zeile").Priority);

        await Assert.ThrowsAsync<PmException>(() => _service.SyncWochenTodoAsync(Ben, id, second));
        await Assert.ThrowsAsync<PmException>(() => _service.SyncWochenTodoAsync(Lead, id, "irgendwas\nohne Kopf"));
    }

    [Fact]
    public async Task Schema_Adds_Visibility_And_ExternalRef_To_Existing_Tables()
    {
        using (var db = new AppDbContext(_options))
        {
            // Stand vor 2026-10-13: Tabellen ohne die neuen Spalten.
            db.Database.ExecuteSqlRaw("DROP TABLE PmProjects");
            db.Database.ExecuteSqlRaw("DROP TABLE PmTasks");
            foreach (var sql in DatabaseSchemaSql.GetPmCreateSql().Where(s => s.Contains("CREATE TABLE PmProjects") || s.Contains("CREATE TABLE PmTasks")))
                db.Database.ExecuteSqlRaw(sql.Replace("    Visibility TEXT NOT NULL DEFAULT 'Public',\n", "").Replace("    Visibility TEXT NOT NULL DEFAULT 'Public',\r\n", "")
                    .Replace(",\n    ExternalRef TEXT NOT NULL DEFAULT ''", "").Replace(",\r\n    ExternalRef TEXT NOT NULL DEFAULT ''", ""));
            Assert.Throws<SqliteException>(() => db.Database.ExecuteSqlRaw("SELECT Visibility FROM PmProjects"));
            new DatabaseSchemaMaintenanceService().EnsureSchema(db);
        }

        var id = await _service.CreateProjectAsync(Lead, new PmProjectInput { Name = "Nach dem Update", Key = "NDU", Visibility = PmVisibility.Private });
        Assert.True((await _service.GetProjectsAsync(Lead)).Single(p => p.Id == id).IsPrivate);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
