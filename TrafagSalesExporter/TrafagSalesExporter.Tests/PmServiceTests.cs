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
        foreach (var table in new[] { "PmProjects", "PmMembers", "PmColumns", "PmSprints", "PmTasks", "PmChecklistItems", "PmComments", "PmActivities" })
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
        Assert.Contains(detail.Activity, a => a.Text.Contains("Priorität High") && a.Text.Contains("Titel"));

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

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
