using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Forum;

public interface IForumService
{
    Task EnsureDefaultsAsync();
    Task<IReadOnlyList<ForumCommunityInfo>> GetCommunitiesAsync();
    Task<ForumCommunityInfo> CreateCommunityAsync(ForumUser user, string name, string description, string icon, string color);
    Task<IReadOnlyList<ForumPostSummary>> GetFeedAsync(ForumFeedQuery query, ForumUser user);
    Task<ForumPostDetail?> GetPostAsync(int id, ForumUser user, bool countView = true);
    Task<ForumPostInput?> GetPostInputAsync(int id);
    Task CountViewAsync(int id);
    Task<int> CreatePostAsync(ForumUser user, ForumPostInput input);
    Task UpdatePostAsync(ForumUser user, int id, ForumPostInput input);
    Task DeletePostAsync(ForumUser user, int id);
    Task TogglePinAsync(ForumUser user, int id);
    Task<int> AddCommentAsync(ForumUser user, int postId, int? parentId, string body);
    Task UpdateCommentAsync(ForumUser user, int commentId, string body);
    Task DeleteCommentAsync(ForumUser user, int commentId);
    Task<(int Score, int MyVote)> VoteAsync(ForumUser user, string targetKind, int targetId, int value);
    Task AcceptAnswerAsync(ForumUser user, int postId, int commentId);
    Task<bool> ToggleBookmarkAsync(ForumUser user, int postId);
    Task<IReadOnlyList<ForumContributor>> GetContributorsAsync(int take = 10);
    Task<ForumContributor?> GetContributorAsync(string login);
    Task<IReadOnlyList<(string Tag, int Count)>> GetPopularTagsAsync(int take = 20);
    Task<IReadOnlyList<ForumPostSummary>> FindSimilarAsync(string title, ForumUser user, int? excludeId = null, int take = 5);
    Task<ForumStats> GetStatsAsync();
}

/// <summary>
/// Trafag Reddit: Beiträge, Kommentare, Stimmen, Lesezeichen und Reputation. Alles in der App-SQLite,
/// Mengen eines Firmenforums (Tausende, nicht Millionen), darum wird für Sortierung und Reputation im
/// Speicher gerechnet. Regeln: nicht für eigene Inhalte stimmen, nur der Fragesteller akzeptiert eine
/// Antwort, aendern und löschen darf der Autor oder ein Admin (Admin-Passwort entsperrt).
/// </summary>
public sealed partial class ForumService : IForumService
{
    public const int TitleMin = 5;
    public const int TitleMax = 200;
    public const int BodyMax = 20000;
    public const int CommentMax = 10000;
    public const int MaxTags = 5;

    // Stimmen und Lesezeichen nacheinander schreiben: zwei schnelle Klicks fanden sonst beide "keine Stimme"
    // und der zweite Insert scheiterte am Unique-Index. SQLite schreibt ohnehin nur einzeln.
    private static readonly SemaphoreSlim ToggleGate = new(1, 1);

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ForumNotifier _notifier;

    public ForumService(IDbContextFactory<AppDbContext> dbFactory, ForumNotifier notifier)
    {
        _dbFactory = dbFactory;
        _notifier = notifier;
    }

    public async Task EnsureDefaultsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.ForumCommunities.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var defaults = new (string Slug, string Name, string Description, string Icon, string Color)[]
        {
            ("allgemein", "Allgemein", "Alles, was sonst nirgends hinpasst.", "Forum", "#C8501E"),
            ("fragen", "Fragen & Antworten", "Wer weiss wie? Frage stellen, beste Antwort akzeptieren.", "HelpOutline", "#1E88E5"),
            ("ideen", "Ideen & Verbesserungen", "Was könnten wir besser machen? Abstimmen entscheidet, was oben steht.", "Lightbulb", "#F9A825"),
            ("sap", "SAP-Tipps", "Transaktionen, Kniffe, Fehlermeldungen und ihre Lösung.", "Storage", "#0A6ED1"),
            ("it-tools", "IT & Tools", "Excel, Teams, Drucker, Laptop: Hilfe von Kollegen.", "Computer", "#546E7A"),
            ("fundstuecke", "Fundstücke & Links", "Lesenswertes, Normen, Messen, Artikel aus der Branche.", "Link", "#8E24AA"),
            ("marktplatz", "Marktplatz", "Biete, suche, verschenke: Velo, Wohnung, Fahrgemeinschaft.", "Storefront", "#2E7D32"),
            ("kantine", "Kantine & Freizeit", "Mittagsmenü, Anlässe, Sport und alles für die Pause.", "EmojiPeople", "#00897B"),
        };
        var order = 10;
        foreach (var d in defaults)
        {
            db.ForumCommunities.Add(new ForumCommunity
            {
                Slug = d.Slug, Name = d.Name, Description = d.Description, Icon = d.Icon, Color = d.Color,
                CreatedByLogin = "system", CreatedByName = "Trafag Reddit", CreatedAtUtc = now, SortOrder = order
            });
            order += 10;
        }
        await db.SaveChangesAsync();

        var general = await db.ForumCommunities.FirstAsync(c => c.Slug == "allgemein");
        db.ForumPosts.Add(new ForumPost
        {
            CommunityId = general.Id,
            Kind = ForumPostKinds.Discussion,
            Title = "Willkommen bei Trafag Reddit",
            Body = WelcomeText,
            Tags = "anleitung,regeln",
            AuthorLogin = "system",
            AuthorName = "Trafag Reddit",
            CreatedAtUtc = now,
            LastActivityUtc = now,
            IsPinned = true
        });
        await db.SaveChangesAsync();
    }

    private const string WelcomeText = """
        Hier ist Platz für alles, was in SharePoint keinen Ort findet: Fragen, Ideen, Tipps, Fundstücke und den Marktplatz.

        ### So funktioniert es
        - **Abstimmen:** Pfeil hoch, wenn ein Beitrag hilft oder gefällt, Pfeil runter, wenn er nicht weiterbringt. Die besten Beiträge steigen nach oben.
        - **Fragen:** Wähle beim Erstellen „Frage“. Wer fragt, kann die beste Antwort mit dem Haken akzeptieren; sie steht dann zuoberst und grün.
        - **Communities:** Jedes Thema hat seine eigene Ecke. Fehlt eine, kann jeder eine neue anlegen.
        - **Reputation:** Jede Stimme für deinen Beitrag gibt 10 Punkte, eine akzeptierte Antwort 15. Gegenstimmen kosten 2.
        - **Formatierung:** `**fett**`, `*kursiv*`, `- Liste`, `> Zitat`, `[Text](https://...)` und Code in Backticks.

        ### Regeln
        1. Freundlich bleiben, du schreibst mit deinem Namen.
        2. Keine vertraulichen Kunden-, Personal- oder Finanzdaten.
        3. Erst suchen, dann fragen: beim Tippen des Titels erscheinen ähnliche Beiträge.
        """;

    public async Task<IReadOnlyList<ForumCommunityInfo>> GetCommunitiesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var communities = await db.ForumCommunities.AsNoTracking().Where(c => !c.IsArchived).ToListAsync();
        var posts = await db.ForumPosts.AsNoTracking().Where(p => !p.IsDeleted)
            .Select(p => new { p.Id, p.CommunityId, p.AuthorLogin, p.LastActivityUtc }).ToListAsync();
        var postCommunity = posts.ToDictionary(p => p.Id, p => p.CommunityId);
        var commenters = await db.ForumComments.AsNoTracking().Where(c => !c.IsDeleted)
            .Select(c => new { c.PostId, c.AuthorLogin }).ToListAsync();

        var members = posts.Select(p => (p.CommunityId, p.AuthorLogin))
            .Concat(commenters.Where(c => postCommunity.ContainsKey(c.PostId)).Select(c => (postCommunity[c.PostId], c.AuthorLogin)))
            .Where(x => x.AuthorLogin != "system")
            .Distinct()
            .GroupBy(x => x.Item1)
            .ToDictionary(g => g.Key, g => g.Count());

        return communities
            .Select(c =>
            {
                var own = posts.Where(p => p.CommunityId == c.Id).ToList();
                return new ForumCommunityInfo(c.Id, c.Slug, c.Name, c.Description, c.Icon, c.Color, own.Count,
                    members.GetValueOrDefault(c.Id), own.Count == 0 ? null : own.Max(p => p.LastActivityUtc), c.CreatedByName);
            })
            .OrderBy(c => communities.First(x => x.Id == c.Id).SortOrder == 0 ? int.MaxValue : communities.First(x => x.Id == c.Id).SortOrder)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<ForumCommunityInfo> CreateCommunityAsync(ForumUser user, string name, string description, string icon, string color)
    {
        RequireUser(user);
        name = (name ?? string.Empty).Trim();
        description = (description ?? string.Empty).Trim();
        if (name.Length is < 3 or > 40)
            throw new ForumException("Der Name braucht 3 bis 40 Zeichen.", "The name needs 3 to 40 characters.");
        if (description.Length > 300)
            throw new ForumException("Die Beschreibung darf höchstens 300 Zeichen haben.", "The description may have at most 300 characters.");
        var slug = Slugify(name);
        if (slug.Length < 2)
            throw new ForumException("Der Name braucht Buchstaben oder Ziffern.", "The name needs letters or digits.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.ForumCommunities.AnyAsync(c => c.Slug == slug))
            throw new ForumException("Diese Community gibt es schon.", "This community already exists.");

        var community = new ForumCommunity
        {
            Slug = slug,
            Name = name,
            Description = description,
            Icon = ForumIcons.Choices.Contains(icon) ? icon : "Forum",
            Color = ForumIcons.Colors.Contains(color) ? color : ForumIcons.Colors[0],
            CreatedByLogin = user.Login,
            CreatedByName = user.DisplayName,
            CreatedAtUtc = DateTime.UtcNow,
            SortOrder = 0
        };
        db.ForumCommunities.Add(community);
        await db.SaveChangesAsync();
        _notifier.Publish(null);
        return new ForumCommunityInfo(community.Id, community.Slug, community.Name, community.Description, community.Icon,
            community.Color, 0, 0, null, community.CreatedByName);
    }

    public async Task<IReadOnlyList<ForumPostSummary>> GetFeedAsync(ForumFeedQuery query, ForumUser user)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var posts = await db.ForumPosts.AsNoTracking().Where(p => !p.IsDeleted).ToListAsync();
        var communities = await db.ForumCommunities.AsNoTracking().ToDictionaryAsync(c => c.Id);

        IEnumerable<ForumPost> filtered = posts;
        if (!string.IsNullOrWhiteSpace(query.CommunitySlug))
        {
            var community = communities.Values.FirstOrDefault(c => c.Slug == query.CommunitySlug);
            filtered = community is null ? [] : filtered.Where(p => p.CommunityId == community.Id);
        }
        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            var tag = query.Tag.Trim().ToLowerInvariant();
            filtered = filtered.Where(p => SplitTags(p.Tags).Contains(tag));
        }
        if (!string.IsNullOrWhiteSpace(query.AuthorLogin))
            filtered = filtered.Where(p => string.Equals(p.AuthorLogin, query.AuthorLogin, StringComparison.OrdinalIgnoreCase));
        if (query.SavedOnly)
        {
            var saved = await SavedPostIdsAsync(db, user);
            filtered = filtered.Where(p => saved.Contains(p.Id));
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var words = query.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            filtered = filtered.Where(p => words.All(w =>
                p.Title.Contains(w, StringComparison.CurrentCultureIgnoreCase) ||
                p.Body.Contains(w, StringComparison.CurrentCultureIgnoreCase) ||
                p.Tags.Contains(w, StringComparison.CurrentCultureIgnoreCase) ||
                p.AuthorName.Contains(w, StringComparison.CurrentCultureIgnoreCase)));
        }
        if (query.Sort is ForumSorts.Top or ForumSorts.Controversial && ForumPeriods.Since(query.Period, DateTime.UtcNow) is { } since)
            filtered = filtered.Where(p => p.CreatedAtUtc >= since);
        if (query.Sort == ForumSorts.Unanswered)
            filtered = filtered.Where(p => p.Kind == ForumPostKinds.Question && p.AcceptedCommentId is null);

        var ordered = query.Sort switch
        {
            ForumSorts.New => filtered.OrderByDescending(p => p.CreatedAtUtc),
            ForumSorts.Top => filtered.OrderByDescending(p => p.UpVotes - p.DownVotes).ThenByDescending(p => p.CreatedAtUtc),
            ForumSorts.Active => filtered.OrderByDescending(p => p.LastActivityUtc),
            ForumSorts.Controversial => filtered.OrderByDescending(p => ForumRanking.Controversy(p.UpVotes, p.DownVotes)).ThenByDescending(p => p.CreatedAtUtc),
            ForumSorts.Unanswered => filtered.OrderBy(p => p.CommentCount).ThenByDescending(p => p.CreatedAtUtc),
            _ => filtered.OrderByDescending(p => p.IsPinned).ThenByDescending(p => ForumRanking.Hot(p.UpVotes, p.DownVotes, p.CreatedAtUtc))
        };

        var page = ordered.Take(Math.Clamp(query.Take, 1, 500)).ToList();
        return await SummariesAsync(db, page, communities, user);
    }

    public async Task<ForumPostDetail?> GetPostAsync(int id, ForumUser user, bool countView = true)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var post = await db.ForumPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (post is null)
            return null;
        if (countView)
        {
            post.ViewCount++;
            await db.SaveChangesAsync();
        }

        var communities = await db.ForumCommunities.AsNoTracking().ToDictionaryAsync(c => c.Id);
        var summary = (await SummariesAsync(db, [post], communities, user))[0];
        var comments = await db.ForumComments.AsNoTracking().Where(c => c.PostId == id).ToListAsync();
        var reputation = await ReputationAsync(db);
        var myVotes = await MyVotesAsync(db, user, ForumTargetKinds.Comment, comments.Select(c => c.Id).ToList());

        var byParent = comments.ToLookup(c => c.ParentId);
        IReadOnlyList<ForumCommentView> Build(int? parentId, bool topLevel)
        {
            var list = new List<ForumCommentView>();
            foreach (var c in byParent[parentId])
            {
                var replies = Build(c.Id, false);
                if (c.IsDeleted && replies.Count == 0)
                    continue;
                list.Add(new ForumCommentView(
                    c.Id, c.ParentId, c.IsDeleted ? string.Empty : c.Body,
                    c.IsDeleted ? string.Empty : c.AuthorLogin, c.IsDeleted ? string.Empty : c.AuthorName,
                    c.IsDeleted ? 0 : reputation.GetValueOrDefault(c.AuthorLogin),
                    c.CreatedAtUtc, c.EditedAtUtc, c.UpVotes - c.DownVotes, c.UpVotes, c.DownVotes,
                    post.AcceptedCommentId == c.Id, c.IsDeleted,
                    !c.IsDeleted && string.Equals(c.AuthorLogin, post.AuthorLogin, StringComparison.OrdinalIgnoreCase),
                    myVotes.GetValueOrDefault(c.Id), replies));
            }
            return list
                .OrderByDescending(c => topLevel && c.IsAccepted)
                .ThenByDescending(c => ForumRanking.Confidence(c.UpVotes, c.DownVotes))
                .ThenByDescending(c => c.Score)
                .ThenBy(c => c.CreatedAtUtc)
                .ToList();
        }

        return new ForumPostDetail(summary, post.Body, Build(null, true));
    }

    public async Task CountViewAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.ForumPosts.Where(p => p.Id == id && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));
    }

    public async Task<ForumPostInput?> GetPostInputAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var post = await db.ForumPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        return post is null ? null : new ForumPostInput
        {
            CommunityId = post.CommunityId, Kind = post.Kind, Title = post.Title, Body = post.Body, Url = post.Url,
            Tags = string.Join(", ", SplitTags(post.Tags))
        };
    }

    public async Task<int> CreatePostAsync(ForumUser user, ForumPostInput input)
    {
        RequireUser(user);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var (title, body, url, tags) = await ValidateAsync(db, input);
        var now = DateTime.UtcNow;
        var post = new ForumPost
        {
            CommunityId = input.CommunityId,
            Kind = input.Kind,
            Title = title,
            Body = body,
            Url = url,
            Tags = tags,
            AuthorLogin = user.Login,
            AuthorName = user.DisplayName,
            CreatedAtUtc = now,
            LastActivityUtc = now
        };
        db.ForumPosts.Add(post);
        await db.SaveChangesAsync();
        _notifier.Publish(post.Id);
        return post.Id;
    }

    public async Task UpdatePostAsync(ForumUser user, int id, ForumPostInput input)
    {
        RequireUser(user);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var post = await db.ForumPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted) ?? throw NotFound();
        RequireOwnerOrAdmin(user, post.AuthorLogin);
        var (title, body, url, tags) = await ValidateAsync(db, input);
        post.CommunityId = input.CommunityId;
        post.Kind = input.Kind;
        post.Title = title;
        post.Body = body;
        post.Url = url;
        post.Tags = tags;
        post.EditedAtUtc = DateTime.UtcNow;
        if (post.Kind != ForumPostKinds.Question)
            post.AcceptedCommentId = null;
        await db.SaveChangesAsync();
        _notifier.Publish(post.Id);
    }

    public async Task DeletePostAsync(ForumUser user, int id)
    {
        RequireUser(user);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var post = await db.ForumPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted) ?? throw NotFound();
        RequireOwnerOrAdmin(user, post.AuthorLogin);
        post.IsDeleted = true;
        post.IsPinned = false;
        await db.SaveChangesAsync();
        _notifier.Publish(post.Id);
    }

    public async Task TogglePinAsync(ForumUser user, int id)
    {
        if (!user.IsAdmin)
            throw new ForumException("Anheften dürfen nur Admins.", "Only admins may pin posts.");
        await using var db = await _dbFactory.CreateDbContextAsync();
        var post = await db.ForumPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted) ?? throw NotFound();
        post.IsPinned = !post.IsPinned;
        await db.SaveChangesAsync();
        _notifier.Publish(post.Id);
    }

    public async Task<int> AddCommentAsync(ForumUser user, int postId, int? parentId, string body)
    {
        RequireUser(user);
        body = (body ?? string.Empty).Trim();
        if (body.Length == 0)
            throw new ForumException("Der Kommentar ist leer.", "The comment is empty.");
        if (body.Length > CommentMax)
            throw new ForumException($"Ein Kommentar darf höchstens {CommentMax} Zeichen haben.", $"A comment may have at most {CommentMax} characters.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var post = await db.ForumPosts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted) ?? throw NotFound();
        if (parentId is { } pid && !await db.ForumComments.AnyAsync(c => c.Id == pid && c.PostId == postId && !c.IsDeleted))
            throw new ForumException("Der Kommentar, auf den du antwortest, gibt es nicht mehr.", "The comment you reply to no longer exists.");

        var now = DateTime.UtcNow;
        var comment = new ForumComment
        {
            PostId = postId,
            ParentId = parentId,
            Body = body,
            AuthorLogin = user.Login,
            AuthorName = user.DisplayName,
            CreatedAtUtc = now
        };
        db.ForumComments.Add(comment);
        post.CommentCount++;
        post.LastActivityUtc = now;
        await db.SaveChangesAsync();
        _notifier.Publish(postId);
        return comment.Id;
    }

    public async Task UpdateCommentAsync(ForumUser user, int commentId, string body)
    {
        RequireUser(user);
        body = (body ?? string.Empty).Trim();
        if (body.Length == 0 || body.Length > CommentMax)
            throw new ForumException($"Ein Kommentar braucht 1 bis {CommentMax} Zeichen.", $"A comment needs 1 to {CommentMax} characters.");
        await using var db = await _dbFactory.CreateDbContextAsync();
        var comment = await db.ForumComments.FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted) ?? throw NotFound();
        RequireOwnerOrAdmin(user, comment.AuthorLogin);
        comment.Body = body;
        comment.EditedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        _notifier.Publish(comment.PostId);
    }

    public async Task DeleteCommentAsync(ForumUser user, int commentId)
    {
        RequireUser(user);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var comment = await db.ForumComments.FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted) ?? throw NotFound();
        RequireOwnerOrAdmin(user, comment.AuthorLogin);
        comment.IsDeleted = true;
        var post = await db.ForumPosts.FirstOrDefaultAsync(p => p.Id == comment.PostId);
        if (post is not null)
        {
            post.CommentCount = Math.Max(0, post.CommentCount - 1);
            if (post.AcceptedCommentId == commentId)
                post.AcceptedCommentId = null;
        }
        await db.SaveChangesAsync();
        _notifier.Publish(comment.PostId);
    }

    public async Task<(int Score, int MyVote)> VoteAsync(ForumUser user, string targetKind, int targetId, int value)
    {
        RequireUser(user);
        await ToggleGate.WaitAsync();
        try
        {
            return await VoteCoreAsync(user, targetKind, targetId, value);
        }
        finally
        {
            ToggleGate.Release();
        }
    }

    private async Task<(int Score, int MyVote)> VoteCoreAsync(ForumUser user, string targetKind, int targetId, int value)
    {
        value = Math.Sign(value);
        await using var db = await _dbFactory.CreateDbContextAsync();

        ForumPost? post = null;
        ForumComment? comment = null;
        string author;
        int postId;
        if (targetKind == ForumTargetKinds.Post)
        {
            post = await db.ForumPosts.FirstOrDefaultAsync(p => p.Id == targetId && !p.IsDeleted) ?? throw NotFound();
            author = post.AuthorLogin;
            postId = post.Id;
        }
        else if (targetKind == ForumTargetKinds.Comment)
        {
            comment = await db.ForumComments.FirstOrDefaultAsync(c => c.Id == targetId && !c.IsDeleted) ?? throw NotFound();
            author = comment.AuthorLogin;
            postId = comment.PostId;
        }
        else
        {
            throw NotFound();
        }

        if (string.Equals(author, user.Login, StringComparison.OrdinalIgnoreCase))
            throw new ForumException("Für eigene Beiträge kann man nicht abstimmen.", "You cannot vote on your own posts.");

        var vote = await db.ForumVotes.FirstOrDefaultAsync(v => v.TargetKind == targetKind && v.TargetId == targetId && v.VoterLogin == user.Login);
        var myVote = value;
        if (vote is null)
        {
            if (value != 0)
                db.ForumVotes.Add(new ForumVote { TargetKind = targetKind, TargetId = targetId, VoterLogin = user.Login, Value = value, CreatedAtUtc = DateTime.UtcNow });
        }
        else if (value == 0 || vote.Value == value)
        {
            // Zweiter Klick auf denselben Pfeil nimmt die Stimme zurueck, wie bei Reddit.
            db.ForumVotes.Remove(vote);
            myVote = 0;
        }
        else
        {
            vote.Value = value;
            vote.CreatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();

        var values = await db.ForumVotes.Where(v => v.TargetKind == targetKind && v.TargetId == targetId).Select(v => v.Value).ToListAsync();
        var up = values.Count(v => v > 0);
        var down = values.Count(v => v < 0);
        if (post is not null)
        {
            post.UpVotes = up;
            post.DownVotes = down;
        }
        else if (comment is not null)
        {
            comment.UpVotes = up;
            comment.DownVotes = down;
        }
        await db.SaveChangesAsync();
        _notifier.Publish(postId);
        return (up - down, myVote);
    }

    public async Task AcceptAnswerAsync(ForumUser user, int postId, int commentId)
    {
        RequireUser(user);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var post = await db.ForumPosts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted) ?? throw NotFound();
        if (post.Kind != ForumPostKinds.Question)
            throw new ForumException("Antworten akzeptieren geht nur bei Fragen.", "Only questions can have an accepted answer.");
        if (!string.Equals(post.AuthorLogin, user.Login, StringComparison.OrdinalIgnoreCase))
            throw new ForumException("Nur wer gefragt hat, akzeptiert die Antwort.", "Only the person who asked can accept an answer.");
        var comment = await db.ForumComments.FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId && !c.IsDeleted) ?? throw NotFound();
        if (comment.ParentId is not null)
            throw new ForumException("Nur direkte Antworten auf die Frage lassen sich akzeptieren.", "Only direct answers to the question can be accepted.");

        post.AcceptedCommentId = post.AcceptedCommentId == commentId ? null : commentId;
        post.LastActivityUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        _notifier.Publish(postId);
    }

    public async Task<bool> ToggleBookmarkAsync(ForumUser user, int postId)
    {
        RequireUser(user);
        await ToggleGate.WaitAsync();
        try
        {
            return await ToggleBookmarkCoreAsync(user, postId);
        }
        finally
        {
            ToggleGate.Release();
        }
    }

    private async Task<bool> ToggleBookmarkCoreAsync(ForumUser user, int postId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.ForumBookmarks.FirstOrDefaultAsync(b => b.PostId == postId && b.UserLogin == user.Login);
        if (existing is not null)
        {
            db.ForumBookmarks.Remove(existing);
            await db.SaveChangesAsync();
            return false;
        }
        if (!await db.ForumPosts.AnyAsync(p => p.Id == postId && !p.IsDeleted))
            throw NotFound();
        db.ForumBookmarks.Add(new ForumBookmark { PostId = postId, UserLogin = user.Login, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<ForumContributor>> GetContributorsAsync(int take = 10)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var all = await ContributorsAsync(db);
        return all.Take(take).ToList();
    }

    public async Task<ForumContributor?> GetContributorAsync(string login)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var all = await ContributorsAsync(db);
        return all.FirstOrDefault(c => string.Equals(c.Login, login, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<(string Tag, int Count)>> GetPopularTagsAsync(int take = 20)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tags = await db.ForumPosts.AsNoTracking().Where(p => !p.IsDeleted && p.Tags != "").Select(p => p.Tags).ToListAsync();
        return tags.SelectMany(SplitTags)
            .GroupBy(t => t)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2).ThenBy(x => x.Key)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<ForumPostSummary>> FindSimilarAsync(string title, ForumUser user, int? excludeId = null, int take = 5)
    {
        var words = SignificantWords(title);
        if (words.Count == 0)
            return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        var posts = await db.ForumPosts.AsNoTracking().Where(p => !p.IsDeleted && p.Id != (excludeId ?? 0)).ToListAsync();
        var scored = posts
            .Select(p =>
            {
                var titleWords = SignificantWords(p.Title);
                var tagWords = SplitTags(p.Tags).ToHashSet();
                var hits = words.Count(w => titleWords.Contains(w)) * 2 + words.Count(w => tagWords.Contains(w))
                           + words.Count(w => p.Body.Contains(w, StringComparison.CurrentCultureIgnoreCase));
                return (Post: p, Hits: hits);
            })
            .Where(x => x.Hits >= Math.Min(2, words.Count * 2))
            .OrderByDescending(x => x.Hits)
            .ThenByDescending(x => x.Post.UpVotes - x.Post.DownVotes)
            .Take(take)
            .Select(x => x.Post)
            .ToList();
        var communities = await db.ForumCommunities.AsNoTracking().ToDictionaryAsync(c => c.Id);
        return await SummariesAsync(db, scored, communities, user);
    }

    public async Task<ForumStats> GetStatsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var posts = await db.ForumPosts.AsNoTracking().Where(p => !p.IsDeleted)
            .Select(p => new { p.AuthorLogin, p.Kind, p.AcceptedCommentId }).ToListAsync();
        var comments = await db.ForumComments.AsNoTracking().Where(c => !c.IsDeleted).Select(c => c.AuthorLogin).ToListAsync();
        var votes = await db.ForumVotes.CountAsync();
        var people = posts.Select(p => p.AuthorLogin).Concat(comments).Where(l => l != "system").Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var open = posts.Count(p => p.Kind == ForumPostKinds.Question && p.AcceptedCommentId is null);
        return new ForumStats(posts.Count, comments.Count, votes, people, open);
    }

    // ---------- Hilfen ----------

    private async Task<IReadOnlyList<ForumPostSummary>> SummariesAsync(AppDbContext db, IReadOnlyList<ForumPost> posts,
        IReadOnlyDictionary<int, ForumCommunity> communities, ForumUser user)
    {
        if (posts.Count == 0)
            return [];
        var reputation = await ReputationAsync(db);
        var ids = posts.Select(p => p.Id).ToList();
        var myVotes = await MyVotesAsync(db, user, ForumTargetKinds.Post, ids);
        var saved = await SavedPostIdsAsync(db, user);
        return posts.Select(p =>
        {
            communities.TryGetValue(p.CommunityId, out var c);
            return new ForumPostSummary(
                p.Id, c?.Slug ?? string.Empty, c?.Name ?? string.Empty, c?.Color ?? ForumIcons.Colors[0], c?.Icon ?? "Forum",
                p.Kind, p.Title, ForumMarkdown.Snippet(p.Body), p.Url, SplitTags(p.Tags),
                p.AuthorLogin, p.AuthorName, reputation.GetValueOrDefault(p.AuthorLogin),
                p.CreatedAtUtc, p.EditedAtUtc, p.LastActivityUtc,
                p.UpVotes - p.DownVotes, p.UpVotes, p.DownVotes, p.CommentCount, p.ViewCount,
                p.AcceptedCommentId is not null, p.IsPinned, myVotes.GetValueOrDefault(p.Id), saved.Contains(p.Id));
        }).ToList();
    }

    private static async Task<Dictionary<int, int>> MyVotesAsync(AppDbContext db, ForumUser user, string kind, IReadOnlyCollection<int> ids)
    {
        if (!user.IsKnown || ids.Count == 0)
            return [];
        return await db.ForumVotes.AsNoTracking()
            .Where(v => v.TargetKind == kind && v.VoterLogin == user.Login && ids.Contains(v.TargetId))
            .ToDictionaryAsync(v => v.TargetId, v => v.Value);
    }

    private static async Task<HashSet<int>> SavedPostIdsAsync(AppDbContext db, ForumUser user)
    {
        if (!user.IsKnown)
            return [];
        var ids = await db.ForumBookmarks.AsNoTracking().Where(b => b.UserLogin == user.Login).Select(b => b.PostId).ToListAsync();
        return ids.ToHashSet();
    }

    private static async Task<Dictionary<string, int>> ReputationAsync(AppDbContext db) =>
        (await ContributorsAsync(db)).ToDictionary(c => c.Login, c => c.Reputation, StringComparer.OrdinalIgnoreCase);

    private static async Task<List<ForumContributor>> ContributorsAsync(AppDbContext db)
    {
        var posts = await db.ForumPosts.AsNoTracking().Where(p => !p.IsDeleted)
            .Select(p => new { p.AuthorLogin, p.AuthorName, p.UpVotes, p.DownVotes, p.AcceptedCommentId, p.CreatedAtUtc }).ToListAsync();
        var comments = await db.ForumComments.AsNoTracking().Where(c => !c.IsDeleted)
            .Select(c => new { c.Id, c.AuthorLogin, c.AuthorName, c.UpVotes, c.DownVotes, c.CreatedAtUtc }).ToListAsync();
        var commentAuthor = comments.ToDictionary(c => c.Id, c => c.AuthorLogin);

        var rep = new Dictionary<string, Tally>(StringComparer.OrdinalIgnoreCase);
        void Add(string login, string name, DateTime at, int points, int post = 0, int comment = 0, int accepted = 0)
        {
            if (login.Length == 0 || login == "system")
                return;
            if (!rep.TryGetValue(login, out var cur))
                rep[login] = cur = new Tally();
            if (name.Length > 0 && at >= cur.NameAt)
            {
                cur.Name = name;
                cur.NameAt = at;
            }
            cur.Rep += points;
            cur.Posts += post;
            cur.Comments += comment;
            cur.Accepted += accepted;
        }

        foreach (var p in posts)
        {
            Add(p.AuthorLogin, p.AuthorName, p.CreatedAtUtc,
                p.UpVotes * ForumRanking.PostUpvotePoints - p.DownVotes * ForumRanking.DownvotePenalty, post: 1);
            if (p.AcceptedCommentId is { } acc && commentAuthor.TryGetValue(acc, out var answerer)
                && !string.Equals(answerer, p.AuthorLogin, StringComparison.OrdinalIgnoreCase))
            {
                Add(answerer, string.Empty, DateTime.MinValue, ForumRanking.AcceptedAnswerPoints, accepted: 1);
                Add(p.AuthorLogin, p.AuthorName, p.CreatedAtUtc, ForumRanking.AcceptingPoints);
            }
        }
        foreach (var c in comments)
            Add(c.AuthorLogin, c.AuthorName, c.CreatedAtUtc,
                c.UpVotes * ForumRanking.CommentUpvotePoints - c.DownVotes * ForumRanking.DownvotePenalty, comment: 1);

        return rep
            .Select(kv => new ForumContributor(kv.Key, kv.Value.Name.Length > 0 ? kv.Value.Name : kv.Key, Math.Max(0, kv.Value.Rep),
                kv.Value.Posts, kv.Value.Comments, kv.Value.Accepted, ForumRanking.Level(Math.Max(0, kv.Value.Rep))))
            .OrderByDescending(c => c.Reputation)
            .ThenByDescending(c => c.Posts + c.Comments)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private sealed class Tally
    {
        public string Name = string.Empty;
        public DateTime NameAt = DateTime.MinValue;
        public int Rep;
        public int Posts;
        public int Comments;
        public int Accepted;
    }

    private static async Task<(string Title, string Body, string Url, string Tags)> ValidateAsync(AppDbContext db, ForumPostInput input)
    {
        var title = (input.Title ?? string.Empty).Trim();
        var body = (input.Body ?? string.Empty).Trim();
        var url = (input.Url ?? string.Empty).Trim();
        if (title.Length < TitleMin || title.Length > TitleMax)
            throw new ForumException($"Der Titel braucht {TitleMin} bis {TitleMax} Zeichen.", $"The title needs {TitleMin} to {TitleMax} characters.");
        if (body.Length > BodyMax)
            throw new ForumException($"Der Text darf höchstens {BodyMax} Zeichen haben.", $"The text may have at most {BodyMax} characters.");
        if (!ForumPostKinds.All.Contains(input.Kind))
            throw new ForumException("Unbekannte Beitragsart.", "Unknown post type.");
        if (url.Length > 0 && !ForumMarkdown.IsSafeUrl(url))
            throw new ForumException("Der Link muss mit http:// oder https:// beginnen.", "The link must start with http:// or https://.");
        if (input.Kind == ForumPostKinds.Link && url.Length == 0)
            throw new ForumException("Ein Link-Beitrag braucht einen Link.", "A link post needs a link.");
        if (!await db.ForumCommunities.AnyAsync(c => c.Id == input.CommunityId && !c.IsArchived))
            throw new ForumException("Bitte eine Community wählen.", "Please choose a community.");
        return (title, body, url, string.Join(",", NormalizeTags(input.Tags)));
    }

    public static IReadOnlyList<string> NormalizeTags(string? raw) =>
        TagSplitRegex().Split(raw ?? string.Empty)
            .Select(t => TagCleanRegex().Replace(t.Trim().ToLowerInvariant(), string.Empty))
            .Where(t => t.Length is >= 2 and <= 30)
            .Distinct()
            .Take(MaxTags)
            .ToList();

    private static IReadOnlyList<string> SplitTags(string? stored) =>
        string.IsNullOrWhiteSpace(stored) ? [] : stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "der", "die", "das", "und", "oder", "ein", "eine", "einen", "wie", "was", "wer", "wo", "ist", "sind", "mit", "fuer", "für",
        "von", "auf", "bei", "im", "in", "zu", "den", "dem", "des", "kann", "man", "ich", "wir", "gibt", "es", "nicht", "the", "and",
        "how", "what", "for", "with", "can", "does", "is", "are", "to", "of", "a", "an"
    };

    public static HashSet<string> SignificantWords(string? text) =>
        WordRegex().Matches(text ?? string.Empty)
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => w.Length >= 3 && !StopWords.Contains(w))
            .ToHashSet();

    public static string Slugify(string name)
    {
        var s = name.Trim().ToLowerInvariant()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss").Replace("&", " und ");
        s = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.IsAsciiLetterOrDigit(ch) ? ch : '-');
        }
        return DashRegex().Replace(sb.ToString(), "-").Trim('-');
    }

    private static void RequireUser(ForumUser user)
    {
        if (!user.IsKnown)
            throw new ForumException("Ohne Windows-Anmeldung kann man nur lesen.", "Without a Windows sign-in you can only read.");
    }

    private static void RequireOwnerOrAdmin(ForumUser user, string authorLogin)
    {
        if (!user.IsAdmin && !string.Equals(user.Login, authorLogin, StringComparison.OrdinalIgnoreCase))
            throw new ForumException("Ändern und löschen darf nur, wer es geschrieben hat.", "Only the author can edit or delete this.");
    }

    private static ForumException NotFound() =>
        new("Der Beitrag ist nicht mehr vorhanden.", "The post no longer exists.");

    [GeneratedRegex(@"[,;\s#]+")] private static partial Regex TagSplitRegex();
    [GeneratedRegex(@"[^\p{L}\p{N}+.\-]")] private static partial Regex TagCleanRegex();
    [GeneratedRegex(@"[\p{L}\p{N}]+")] private static partial Regex WordRegex();
    [GeneratedRegex("-{2,}")] private static partial Regex DashRegex();
}
