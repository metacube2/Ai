using TrafagSalesExporter.Services.Forum;

namespace TrafagSalesExporter.Tests;

public sealed class ForumMarkdownTests
{
    [Fact]
    public void Html_And_Script_Are_Encoded_Never_Rendered()
    {
        var html = ForumMarkdown.ToHtml("<script>alert(1)</script> <img src=x onerror=alert(1)>");
        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Only_Http_Https_Mailto_Links_Become_Anchors()
    {
        Assert.Contains("<a href=\"https://trafag.com\"", ForumMarkdown.ToHtml("[Trafag](https://trafag.com)"));
        var bad = ForumMarkdown.ToHtml("[klick](javascript:alert(1))");
        Assert.DoesNotContain("<a ", bad);
        Assert.Contains("klick", bad);
        var quote = ForumMarkdown.ToHtml("[x](https://a.ch/\"onmouseover=\"alert(1))");
        Assert.DoesNotContain("\"onmouseover", quote);
    }

    [Fact]
    public void Formatting_Lists_Quotes_Code_And_Mentions()
    {
        var html = ForumMarkdown.ToHtml("### Titel\n**fett** und *kursiv* und `a<b`\n\n- eins\n- zwei\n\n1. erst\n\n> Zitat\n\n```\n<x>**nicht fett**\n```\nHallo @ingo.kohler, siehe https://example.com/a.");
        Assert.Contains("<h4>Titel</h4>", html);
        Assert.Contains("<strong>fett</strong>", html);
        Assert.Contains("<em>kursiv</em>", html);
        Assert.Contains("<code>a&lt;b</code>", html);
        Assert.Contains("<ul><li>eins</li><li>zwei</li></ul>", html);
        Assert.Contains("<ol><li>erst</li></ol>", html);
        Assert.Contains("<blockquote>Zitat</blockquote>", html);
        Assert.Contains("<pre><code>&lt;x&gt;**nicht fett**</code></pre>", html);
        Assert.Contains("<span class=\"frm-mention\">@ingo.kohler</span>", html);
        Assert.Contains("href=\"https://example.com/a\"", html);
    }

    [Fact]
    public void Snippet_Strips_Markup_And_Shortens()
    {
        Assert.Equal("fett und Link", ForumMarkdown.Snippet("**fett** und [Link](https://x.ch)"));
        Assert.EndsWith(" …", ForumMarkdown.Snippet(new string('a', 300) + " b", 50));
    }

    [Fact]
    public void Ranking_Hot_Prefers_Newer_And_Better_Posts()
    {
        var day = new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);
        Assert.True(ForumRanking.Hot(10, 0, day) > ForumRanking.Hot(1, 0, day));
        Assert.True(ForumRanking.Hot(1, 0, day.AddDays(1)) > ForumRanking.Hot(10, 0, day));
        Assert.True(ForumRanking.Confidence(50, 2) > ForumRanking.Confidence(3, 0));
        Assert.Equal(0, ForumRanking.Controversy(5, 0));
        Assert.True(ForumRanking.Controversy(10, 9) > ForumRanking.Controversy(10, 1));
        Assert.Equal(ForumLevel.Member, ForumRanking.Level(50));
        Assert.Equal(200, ForumRanking.NextLevelAt(50));
    }
}
