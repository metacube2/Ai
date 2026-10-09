namespace TrafagSalesExporter.Services.Forum;

/// <summary>
/// Sortierung und Reputation für Trafag Reddit. Hot wie Reddit (log10 der Stimmen plus Alter, 12,5 Stunden
/// je Zehnerpotenz), Kommentare nach Wilson-Untergrenze ("Beste" bei Reddit), Reputation wie StackOverflow.
/// </summary>
public static class ForumRanking
{
    private static readonly DateTime Epoch = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public const int PostUpvotePoints = 10;
    public const int CommentUpvotePoints = 10;
    public const int DownvotePenalty = 2;
    public const int AcceptedAnswerPoints = 15;
    public const int AcceptingPoints = 2;

    public static double Hot(int up, int down, DateTime createdUtc)
    {
        var score = up - down;
        var order = Math.Log10(Math.Max(Math.Abs(score), 1));
        var sign = Math.Sign(score);
        var seconds = (createdUtc - Epoch).TotalSeconds;
        return Math.Round(sign * order + seconds / 45000d, 7);
    }

    /// <summary>Untere Grenze des 95-%-Wilson-Intervalls; wenige Stimmen zaehlen weniger als viele.</summary>
    public static double Confidence(int up, int down)
    {
        var n = up + down;
        if (n == 0)
            return 0;
        const double z = 1.959964;
        var phat = (double)up / n;
        return (phat + z * z / (2 * n) - z * Math.Sqrt((phat * (1 - phat) + z * z / (4 * n)) / n)) / (1 + z * z / n);
    }

    /// <summary>Kontroverse wie Reddit: viele Stimmen, beide Seiten aehnlich stark.</summary>
    public static double Controversy(int up, int down)
    {
        if (up <= 0 || down <= 0)
            return 0;
        var magnitude = up + down;
        var balance = up > down ? (double)down / up : (double)up / down;
        return Math.Pow(magnitude, balance);
    }

    public static ForumLevel Level(int reputation) => reputation switch
    {
        >= 5000 => ForumLevel.Legend,
        >= 1000 => ForumLevel.Expert,
        >= 200 => ForumLevel.Connoisseur,
        >= 50 => ForumLevel.Member,
        _ => ForumLevel.Newcomer
    };

    public static int NextLevelAt(int reputation) => reputation switch
    {
        >= 5000 => 0,
        >= 1000 => 5000,
        >= 200 => 1000,
        >= 50 => 200,
        _ => 50
    };
}

public enum ForumLevel
{
    Newcomer,
    Member,
    Connoisseur,
    Expert,
    Legend
}
