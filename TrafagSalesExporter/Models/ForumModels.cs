namespace TrafagSalesExporter.Models;

// Trafag Reddit (2026-10-09, Wunsch Ingo): internes Forum fuer alles, was in SharePoint keinen Platz findet.
// Das Beste aus Reddit (Communities, Hot/Neu/Top, verschachtelte Kommentare, Karma) und StackOverflow
// (Fragen mit akzeptierter Antwort, Tags, Reputation, aehnliche Fragen). Doku docs/TRAFAG_REDDIT_2026-10-09.md.

public sealed class ForumCommunity
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "Forum";
    public string Color { get; set; } = "#C8501E";
    public string CreatedByLogin { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int SortOrder { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class ForumPost
{
    public int Id { get; set; }
    public int CommunityId { get; set; }
    public string Kind { get; set; } = ForumPostKinds.Discussion;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string AuthorLogin { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EditedAtUtc { get; set; }
    public DateTime LastActivityUtc { get; set; }
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public int CommentCount { get; set; }
    public int ViewCount { get; set; }
    public int? AcceptedCommentId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class ForumComment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int? ParentId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string AuthorLogin { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EditedAtUtc { get; set; }
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class ForumVote
{
    public int Id { get; set; }
    public string TargetKind { get; set; } = ForumTargetKinds.Post;
    public int TargetId { get; set; }
    public string VoterLogin { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ForumBookmark
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserLogin { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public static class ForumPostKinds
{
    public const string Discussion = "Discussion";
    public const string Question = "Question";
    public const string Link = "Link";
    public const string Idea = "Idea";

    public static readonly string[] All = [Discussion, Question, Link, Idea];
}

public static class ForumTargetKinds
{
    public const string Post = "P";
    public const string Comment = "C";
}
