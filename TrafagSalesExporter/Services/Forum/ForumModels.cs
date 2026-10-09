using MudBlazor;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Forum;

public sealed record ForumUser(string Login, string DisplayName, bool IsAdmin)
{
    public static readonly ForumUser Anonymous = new(string.Empty, string.Empty, false);
    public bool IsKnown => Login.Length > 0;
}

public static class ForumSorts
{
    public const string Hot = "hot";
    public const string New = "new";
    public const string Top = "top";
    public const string Active = "active";
    public const string Unanswered = "unanswered";
    public const string Controversial = "controversial";
}

public static class ForumPeriods
{
    public const string Day = "day";
    public const string Week = "week";
    public const string Month = "month";
    public const string Year = "year";
    public const string All = "all";

    public static DateTime? Since(string? period, DateTime nowUtc) => period switch
    {
        Day => nowUtc.AddDays(-1),
        Week => nowUtc.AddDays(-7),
        Month => nowUtc.AddMonths(-1),
        Year => nowUtc.AddYears(-1),
        _ => null
    };
}

public sealed record ForumFeedQuery
{
    public string? CommunitySlug { get; init; }
    public string Sort { get; init; } = ForumSorts.Hot;
    public string Period { get; init; } = ForumPeriods.All;
    public string? Search { get; init; }
    public string? Tag { get; init; }
    public string? AuthorLogin { get; init; }
    public bool SavedOnly { get; init; }
    public int Take { get; init; } = 50;
}

public sealed record ForumCommunityInfo(
    int Id, string Slug, string Name, string Description, string Icon, string Color,
    int PostCount, int MemberCount, DateTime? LastActivityUtc, string CreatedByName);

public sealed record ForumPostSummary(
    int Id, string CommunitySlug, string CommunityName, string CommunityColor, string CommunityIcon,
    string Kind, string Title, string Snippet, string Url, IReadOnlyList<string> Tags,
    string AuthorLogin, string AuthorName, int AuthorReputation,
    DateTime CreatedAtUtc, DateTime? EditedAtUtc, DateTime LastActivityUtc,
    int Score, int UpVotes, int DownVotes, int CommentCount, int ViewCount,
    bool IsAnswered, bool IsPinned, int MyVote, bool IsSaved);

public sealed record ForumCommentView(
    int Id, int? ParentId, string Body, string AuthorLogin, string AuthorName, int AuthorReputation,
    DateTime CreatedAtUtc, DateTime? EditedAtUtc, int Score, int UpVotes, int DownVotes,
    bool IsAccepted, bool IsDeleted, bool IsByPostAuthor, int MyVote, IReadOnlyList<ForumCommentView> Replies);

public sealed record ForumPostDetail(ForumPostSummary Summary, string Body, IReadOnlyList<ForumCommentView> Comments);

public sealed record ForumContributor(string Login, string Name, int Reputation, int Posts, int Comments, int Accepted, ForumLevel Level);

public sealed record ForumStats(int Posts, int Comments, int Votes, int Contributors, int OpenQuestions);

public sealed record ForumPostInput
{
    public int CommunityId { get; set; }
    public string Kind { get; set; } = ForumPostKinds.Discussion;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
}

public sealed class ForumException(string german, string english) : Exception(english)
{
    public string German { get; } = german;
    public string English { get; } = english;
}

public static class ForumIcons
{
    public static readonly string[] Choices =
        ["Forum", "HelpOutline", "Lightbulb", "Storage", "Computer", "Link", "Storefront", "EmojiPeople",
         "Celebration", "School", "Build", "Science", "LocalShipping", "Factory", "Groups", "Public"];

    public static readonly string[] Colors =
        ["#C8501E", "#1E88E5", "#F9A825", "#0A6ED1", "#546E7A", "#8E24AA", "#2E7D32", "#00897B", "#D81B60", "#6D4C41"];

    public static string Resolve(string? icon) => icon switch
    {
        "HelpOutline" => Icons.Material.Filled.HelpOutline,
        "Lightbulb" => Icons.Material.Filled.Lightbulb,
        "Storage" => Icons.Material.Filled.Storage,
        "Computer" => Icons.Material.Filled.Computer,
        "Link" => Icons.Material.Filled.Link,
        "Storefront" => Icons.Material.Filled.Storefront,
        "EmojiPeople" => Icons.Material.Filled.EmojiPeople,
        "Celebration" => Icons.Material.Filled.Celebration,
        "School" => Icons.Material.Filled.School,
        "Build" => Icons.Material.Filled.Build,
        "Science" => Icons.Material.Filled.Science,
        "LocalShipping" => Icons.Material.Filled.LocalShipping,
        "Factory" => Icons.Material.Filled.Factory,
        "Groups" => Icons.Material.Filled.Groups,
        "Public" => Icons.Material.Filled.Public,
        _ => Icons.Material.Filled.Forum
    };
}

/// <summary>Meldet allen offenen Seiten, dass sich etwas geaendert hat (neuer Beitrag, Stimme, Kommentar).</summary>
public sealed class ForumNotifier
{
    public event Action<int?>? Changed;

    public void Publish(int? postId)
    {
        if (Changed is not { } handlers)
            return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<int?>>())
        {
            try { handler(postId); }
            catch { /* eine abgestuerzte Seite darf die anderen nicht stoeren */ }
        }
    }
}
