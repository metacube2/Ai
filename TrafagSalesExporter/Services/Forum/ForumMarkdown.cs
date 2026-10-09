using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace TrafagSalesExporter.Services.Forum;

/// <summary>
/// Kleiner, sicherer Markdown-Teil für Trafag Reddit: erst alles HTML-kodieren, dann nur bekannte Muster
/// in Tags umsetzen. Kein rohes HTML aus Beiträgen, Links nur http/https/mailto, Bilder gibt es nicht.
/// Unterstuetzt: Absaetze, Zeilenumbruch, ### Ueberschrift, - / * / 1. Listen, > Zitat, ```Codeblock```,
/// `Code`, **fett**, *kursiv*, ~~durchgestrichen~~, [Text](https://...), nackte https-Adressen, @Erwaehnung.
/// </summary>
public static partial class ForumMarkdown
{
    public static MarkupString Render(string? markdown) => new(ToHtml(markdown));

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var html = new StringBuilder();
        var paragraph = new List<string>();
        string? listTag = null;
        var quote = new List<string>();

        void FlushParagraph()
        {
            if (paragraph.Count == 0) return;
            html.Append("<p>").Append(string.Join("<br/>", paragraph.Select(Inline))).Append("</p>");
            paragraph.Clear();
        }
        void FlushList()
        {
            if (listTag is null) return;
            html.Append("</").Append(listTag).Append('>');
            listTag = null;
        }
        void FlushQuote()
        {
            if (quote.Count == 0) return;
            html.Append("<blockquote>").Append(string.Join("<br/>", quote.Select(Inline))).Append("</blockquote>");
            quote.Clear();
        }
        void FlushAll() { FlushParagraph(); FlushList(); FlushQuote(); }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                FlushAll();
                var code = new List<string>();
                i++;
                while (i < lines.Length && !lines[i].Trim().StartsWith("```", StringComparison.Ordinal))
                    code.Add(lines[i++]);
                html.Append("<pre><code>").Append(WebUtility.HtmlEncode(string.Join("\n", code))).Append("</code></pre>");
                continue;
            }

            if (trimmed.Length == 0)
            {
                FlushAll();
                continue;
            }

            var heading = HeadingRegex().Match(trimmed);
            if (heading.Success)
            {
                FlushAll();
                html.Append("<h4>").Append(Inline(heading.Groups[1].Value)).Append("</h4>");
                continue;
            }

            if (trimmed.StartsWith('>'))
            {
                FlushParagraph(); FlushList();
                quote.Add(trimmed[1..].TrimStart());
                continue;
            }

            var bullet = BulletRegex().Match(trimmed);
            var numbered = NumberedRegex().Match(trimmed);
            if (bullet.Success || numbered.Success)
            {
                FlushParagraph(); FlushQuote();
                var tag = bullet.Success ? "ul" : "ol";
                if (listTag != tag)
                {
                    FlushList();
                    html.Append('<').Append(tag).Append('>');
                    listTag = tag;
                }
                html.Append("<li>").Append(Inline((bullet.Success ? bullet : numbered).Groups[1].Value)).Append("</li>");
                continue;
            }

            FlushList(); FlushQuote();
            paragraph.Add(trimmed);
        }

        FlushAll();
        return html.ToString();
    }

    /// <summary>Klartext für Vorschau und Suche: Markdown-Zeichen weg, auf Laenge gekuerzt.</summary>
    public static string Snippet(string? markdown, int maxLength = 220)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;
        var text = LinkRegex().Replace(markdown, "$1");
        text = StripRegex().Replace(text, " ");
        text = SpaceRegex().Replace(text, " ").Trim();
        return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + " …";
    }

    private static string Inline(string text)
    {
        // Code-Stellen zuerst herausnehmen, damit darin nichts formatiert wird.
        var codes = new List<string>();
        text = InlineCodeRegex().Replace(text, m =>
        {
            codes.Add("<code>" + WebUtility.HtmlEncode(m.Groups[1].Value) + "</code>");
            return "\u0001" + (codes.Count - 1) + "\u0002";
        });

        var links = new List<string>();
        text = LinkRegex().Replace(text, m =>
        {
            links.Add(Anchor(m.Groups[2].Value, WebUtility.HtmlEncode(m.Groups[1].Value)));
            return "\u0003" + (links.Count - 1) + "\u0004";
        });
        text = BareUrlRegex().Replace(text, m =>
        {
            links.Add(Anchor(m.Value, WebUtility.HtmlEncode(m.Value)));
            return "\u0003" + (links.Count - 1) + "\u0004";
        });

        var encoded = WebUtility.HtmlEncode(text);
        encoded = BoldRegex().Replace(encoded, "<strong>$1</strong>");
        encoded = ItalicRegex().Replace(encoded, "<em>$1</em>");
        encoded = StrikeRegex().Replace(encoded, "<del>$1</del>");
        encoded = MentionRegex().Replace(encoded, "$1<span class=\"frm-mention\">@$2</span>");

        encoded = PlaceholderLinkRegex().Replace(encoded, m => links[int.Parse(m.Groups[1].Value)]);
        encoded = PlaceholderCodeRegex().Replace(encoded, m => codes[int.Parse(m.Groups[1].Value)]);
        return encoded;
    }

    private static string Anchor(string url, string encodedLabel)
    {
        if (!IsSafeUrl(url))
            return encodedLabel;
        return $"<a href=\"{WebUtility.HtmlEncode(url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{encodedLabel}</a>";
    }

    public static bool IsSafeUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto);

    [GeneratedRegex(@"^#{1,6}\s+(.+)$")] private static partial Regex HeadingRegex();
    [GeneratedRegex(@"^[-*]\s+(.+)$")] private static partial Regex BulletRegex();
    [GeneratedRegex(@"^\d+[.)]\s+(.+)$")] private static partial Regex NumberedRegex();
    [GeneratedRegex(@"`([^`]+)`")] private static partial Regex InlineCodeRegex();
    [GeneratedRegex(@"\[([^\]]+)\]\(([^)\s]+)\)")] private static partial Regex LinkRegex();
    [GeneratedRegex(@"(?<![\w/])(https?://[^\s<>()\u0001-\u0004]+[^\s<>().,;:!?\u0001-\u0004])")] private static partial Regex BareUrlRegex();
    [GeneratedRegex(@"\*\*(.+?)\*\*")] private static partial Regex BoldRegex();
    [GeneratedRegex(@"(?<![*\w])\*(?!\s)(.+?)(?<!\s)\*(?![*\w])")] private static partial Regex ItalicRegex();
    [GeneratedRegex(@"~~(.+?)~~")] private static partial Regex StrikeRegex();
    [GeneratedRegex(@"(^|\s)@([\p{L}][\p{L}\p{N}._-]{1,40})")] private static partial Regex MentionRegex();
    [GeneratedRegex("\u0003(\\d+)\u0004")] private static partial Regex PlaceholderLinkRegex();
    [GeneratedRegex("\u0001(\\d+)\u0002")] private static partial Regex PlaceholderCodeRegex();
    [GeneratedRegex(@"[#>*_`~]+|^\s*[-*]\s+|^\s*\d+[.)]\s+", RegexOptions.Multiline)] private static partial Regex StripRegex();
    [GeneratedRegex(@"\s+")] private static partial Regex SpaceRegex();
}
