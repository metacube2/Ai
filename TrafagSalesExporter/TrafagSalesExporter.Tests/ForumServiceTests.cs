using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.Forum;

namespace TrafagSalesExporter.Tests;

public sealed class ForumServiceTests : IDisposable
{
    private static readonly ForumUser Anna = new("anna", "Anna Muster", false);
    private static readonly ForumUser Ben = new("ben", "Ben Beispiel", false);
    private static readonly ForumUser Cleo = new("cleo", "Cleo Test", false);
    private static readonly ForumUser Admin = new("chef", "Chef Admin", true);

    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;
    private readonly ForumService _service;

    public ForumServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        // Die Schema-SQL muss die Tabellen auch ohne EF anlegen (bestehende Produktiv-DB).
        foreach (var table in new[] { "ForumCommunities", "ForumPosts", "ForumComments", "ForumVotes", "ForumBookmarks" })
            db.Database.ExecuteSqlRaw($"DROP TABLE {table}");
        new DatabaseSchemaMaintenanceService().EnsureSchema(db);

        _dbFactory = new TestDbContextFactory(options);
        _service = new ForumService(_dbFactory, new ForumNotifier());
    }

    [Fact]
    public async Task Defaults_Create_Communities_And_Pinned_Welcome_Once()
    {
        await _service.EnsureDefaultsAsync();
        await _service.EnsureDefaultsAsync();

        var communities = await _service.GetCommunitiesAsync();
        Assert.Equal(8, communities.Count);
        Assert.Equal("allgemein", communities[0].Slug);

        var feed = await _service.GetFeedAsync(new ForumFeedQuery(), Anna);
        var welcome = Assert.Single(feed);
        Assert.True(welcome.IsPinned);
        Assert.Equal("system", welcome.AuthorLogin);
    }

    [Fact]
    public async Task Votes_Toggle_Change_And_Block_Own_Posts()
    {
        var id = await CreateAsync(Anna, "Wie drucke ich Etiketten aus SAP?", ForumPostKinds.Question);

        var ex = await Assert.ThrowsAsync<ForumException>(() => _service.VoteAsync(Anna, ForumTargetKinds.Post, id, 1));
        Assert.Contains("eigene", ex.German, StringComparison.OrdinalIgnoreCase);

        Assert.Equal((1, 1), await _service.VoteAsync(Ben, ForumTargetKinds.Post, id, 1));
        Assert.Equal((2, 1), await _service.VoteAsync(Cleo, ForumTargetKinds.Post, id, 1));
        // Zweiter Klick auf denselben Pfeil nimmt die Stimme zurueck.
        Assert.Equal((1, 0), await _service.VoteAsync(Cleo, ForumTargetKinds.Post, id, 1));
        // Wechsel von hoch auf runter.
        Assert.Equal((-1, -1), await _service.VoteAsync(Ben, ForumTargetKinds.Post, id, -1));

        var post = (await _service.GetPostAsync(id, Ben, countView: false))!.Summary;
        Assert.Equal(-1, post.Score);
        Assert.Equal(-1, post.MyVote);
        Assert.Equal(0, post.UpVotes);
        Assert.Equal(1, post.DownVotes);
    }

    [Fact]
    public async Task Only_Asker_Accepts_Top_Level_Answer_And_Reputation_Follows()
    {
        var id = await CreateAsync(Anna, "Welche Transaktion zeigt offene Bestellungen?", ForumPostKinds.Question);
        var answer = await _service.AddCommentAsync(Ben, id, null, "ME2N mit Auswahlparameter WE101.");
        var reply = await _service.AddCommentAsync(Cleo, id, answer, "Oder ME2M je Material.");

        await Assert.ThrowsAsync<ForumException>(() => _service.AcceptAnswerAsync(Ben, id, answer));
        await Assert.ThrowsAsync<ForumException>(() => _service.AcceptAnswerAsync(Anna, id, reply));

        await _service.VoteAsync(Anna, ForumTargetKinds.Comment, answer, 1);
        await _service.AcceptAnswerAsync(Anna, id, answer);

        var detail = (await _service.GetPostAsync(id, Anna))!;
        Assert.True(detail.Summary.IsAnswered);
        Assert.Equal(2, detail.Summary.CommentCount);
        var top = Assert.Single(detail.Comments);
        Assert.True(top.IsAccepted);
        Assert.Single(top.Replies);

        var ben = await _service.GetContributorAsync("ben");
        Assert.NotNull(ben);
        Assert.Equal(ForumRanking.CommentUpvotePoints + ForumRanking.AcceptedAnswerPoints, ben!.Reputation);
        Assert.Equal(1, ben.Accepted);
        var anna = await _service.GetContributorAsync("anna");
        Assert.Equal(ForumRanking.AcceptingPoints, anna!.Reputation);

        var open = await _service.GetFeedAsync(new ForumFeedQuery { Sort = ForumSorts.Unanswered }, Anna);
        Assert.DoesNotContain(open, p => p.Id == id);
    }

    [Fact]
    public async Task Edit_And_Delete_Only_By_Author_Or_Admin()
    {
        var id = await CreateAsync(Anna, "Velo zu verschenken", ForumPostKinds.Discussion);
        var input = (await _service.GetPostInputAsync(id))!;
        input.Title = "Velo zu verschenken, abgeholt";

        await Assert.ThrowsAsync<ForumException>(() => _service.UpdatePostAsync(Ben, id, input));
        await _service.UpdatePostAsync(Anna, id, input);
        Assert.Equal("Velo zu verschenken, abgeholt", (await _service.GetPostAsync(id, Ben))!.Summary.Title);

        await Assert.ThrowsAsync<ForumException>(() => _service.DeletePostAsync(Ben, id));
        await _service.DeletePostAsync(Admin, id);
        Assert.Null(await _service.GetPostAsync(id, Anna));
    }

    [Fact]
    public async Task Deleted_Comment_Keeps_Its_Replies_As_Placeholder()
    {
        var id = await CreateAsync(Anna, "Kantine: neues Menue am Freitag", ForumPostKinds.Discussion);
        var parent = await _service.AddCommentAsync(Ben, id, null, "Endlich wieder Fisch.");
        await _service.AddCommentAsync(Cleo, id, parent, "Und vegetarisch?");
        var lonely = await _service.AddCommentAsync(Cleo, id, null, "Wird ohnehin geloescht.");

        await _service.DeleteCommentAsync(Ben, parent);
        await _service.DeleteCommentAsync(Cleo, lonely);

        var detail = (await _service.GetPostAsync(id, Anna))!;
        var placeholder = Assert.Single(detail.Comments);
        Assert.True(placeholder.IsDeleted);
        Assert.Equal(string.Empty, placeholder.AuthorName);
        Assert.Single(placeholder.Replies);
        Assert.Equal(1, detail.Summary.CommentCount);
    }

    [Fact]
    public async Task Feed_Filters_By_Community_Tag_Search_Saved_And_Sorts_Top()
    {
        var a = await CreateAsync(Anna, "Excel Pivot mit Datenmodell", ForumPostKinds.Question, "it-tools", "excel, pivot");
        var b = await CreateAsync(Ben, "Idee: Fahrgemeinschaften ab Bahnhof", ForumPostKinds.Idea, "ideen", "mobilitaet");
        await _service.VoteAsync(Cleo, ForumTargetKinds.Post, b, 1);
        await _service.VoteAsync(Anna, ForumTargetKinds.Post, b, 1);
        await _service.ToggleBookmarkAsync(Cleo, a);

        Assert.Equal([b, a], (await _service.GetFeedAsync(new ForumFeedQuery { Sort = ForumSorts.Top }, Cleo)).Select(p => p.Id).Where(x => x != 1));
        Assert.Equal([a], (await _service.GetFeedAsync(new ForumFeedQuery { CommunitySlug = "it-tools" }, Cleo)).Select(p => p.Id));
        Assert.Equal([a], (await _service.GetFeedAsync(new ForumFeedQuery { Tag = "Excel" }, Cleo)).Select(p => p.Id));
        Assert.Equal([b], (await _service.GetFeedAsync(new ForumFeedQuery { Search = "bahnhof fahrgemeinschaften" }, Cleo)).Select(p => p.Id));
        var saved = Assert.Single(await _service.GetFeedAsync(new ForumFeedQuery { SavedOnly = true }, Cleo));
        Assert.True(saved.IsSaved);

        var tags = await _service.GetPopularTagsAsync();
        Assert.Contains(tags, t => t.Tag == "excel");
    }

    [Fact]
    public async Task Similar_Posts_Are_Found_By_Title_Words()
    {
        var id = await CreateAsync(Anna, "Drucker im 2. Stock druckt keine Etiketten", ForumPostKinds.Question);
        await CreateAsync(Ben, "Kantine Menueplan", ForumPostKinds.Discussion);

        var similar = await _service.FindSimilarAsync("Etiketten Drucker streikt", Cleo);
        Assert.Equal(id, Assert.Single(similar).Id);
        Assert.Empty(await _service.FindSimilarAsync("Etiketten Drucker", Cleo, excludeId: id));
    }

    [Fact]
    public async Task Validation_Rejects_Bad_Input_And_Anonymous_Users()
    {
        await _service.EnsureDefaultsAsync();
        var community = (await _service.GetCommunitiesAsync())[0].Id;

        await Assert.ThrowsAsync<ForumException>(() => _service.CreatePostAsync(ForumUser.Anonymous,
            new ForumPostInput { CommunityId = community, Title = "Gueltiger Titel" }));
        await Assert.ThrowsAsync<ForumException>(() => _service.CreatePostAsync(Anna,
            new ForumPostInput { CommunityId = community, Title = "kurz" }));
        await Assert.ThrowsAsync<ForumException>(() => _service.CreatePostAsync(Anna,
            new ForumPostInput { CommunityId = community, Title = "Link ohne Adresse", Kind = ForumPostKinds.Link }));
        await Assert.ThrowsAsync<ForumException>(() => _service.CreatePostAsync(Anna,
            new ForumPostInput { CommunityId = community, Title = "Gefaehrlicher Link", Url = "javascript:alert(1)" }));
        await Assert.ThrowsAsync<ForumException>(() => _service.CreatePostAsync(Anna,
            new ForumPostInput { CommunityId = 9999, Title = "Ohne Community" }));
    }

    [Fact]
    public async Task Community_Slug_Is_Unique_And_Readable()
    {
        await _service.EnsureDefaultsAsync();
        var created = await _service.CreateCommunityAsync(Anna, "Lean & 5S Ölwanne", "Ordnung am Arbeitsplatz", "Build", "#2E7D32");
        Assert.Equal("lean-und-5s-oelwanne", created.Slug);
        await Assert.ThrowsAsync<ForumException>(() => _service.CreateCommunityAsync(Ben, "Lean & 5S ölwanne", "", "Forum", "#000000"));
        Assert.Equal(9, (await _service.GetCommunitiesAsync()).Count);
    }

    [Fact]
    public void Tags_Are_Normalized_And_Limited()
    {
        Assert.Equal(["sap", "me21n", "excel", "a1", "zz7"], ForumService.NormalizeTags("SAP, #ME21N; c  excel,excel a1 zz7 x2 y3"));
    }

    private async Task<int> CreateAsync(ForumUser user, string title, string kind, string community = "allgemein", string tags = "")
    {
        await _service.EnsureDefaultsAsync();
        var communityId = (await _service.GetCommunitiesAsync()).Single(c => c.Slug == community).Id;
        return await _service.CreatePostAsync(user, new ForumPostInput
        {
            CommunityId = communityId,
            Kind = kind,
            Title = title,
            Body = "Text zu " + title,
            Tags = tags
        });
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;

        public AppDbContext CreateDbContext() => new(_options);
    }
}
