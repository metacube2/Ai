using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Benutzerhandbuch (2026-10-06): liest die Kapitel-Markdowns aus wwwroot/handbuch/kapitel, dieselben,
/// aus denen Tools/HandbuchDocx die Word-Datei baut. Unterstuetzt nur das Subset aus
/// docs/handbuch/VORGABE.md: #, ##, ###, Absatz, "- " Aufzaehlung, **fett**, |Tabelle|.
/// </summary>
public static class UserManualContent
{
    public sealed record Block(char Kind, string Text, List<string> Items, List<string[]> Rows);
    public sealed record Section(string Title, string Anchor, List<Block> Blocks);
    public sealed record Chapter(string Title, List<Section> Sections);

    public const string DocxPath = "handbuch/Trafag_Cockpit_Handbuch.docx";

    public static string AnchorHref(Section s) => "handbuch#" + s.Anchor;

    public static List<Chapter> Load(string webRoot)
    {
        var dir = Path.Combine(webRoot, "handbuch", "kapitel");
        if (!Directory.Exists(dir)) return [];
        return Directory.GetFiles(dir, "*.md").Order().Select(f => Parse(File.ReadAllLines(f))).Where(c => c.Title.Length > 0).ToList();
    }

    public static bool Matches(Section s, string search)
    {
        bool Has(string text) => text.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);
        return Has(s.Title) || s.Blocks.Any(b => Has(b.Text) || b.Items.Any(Has) || b.Rows.Any(r => r.Any(Has)));
    }

    public static Chapter Parse(string[] lines)
    {
        var title = "";
        var sections = new List<Section> { new("", "einleitung", []) };
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            var blocks = sections[^1].Blocks;
            if (line.Length == 0) continue;
            if (line.StartsWith("# ")) { title = line[2..].Trim(); continue; }
            if (line.StartsWith("## "))
            {
                var t = line[3..].Trim();
                sections.Add(new(t, "s" + sections.Count + "-" + Regex.Replace(t.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-'), []));
                continue;
            }
            if (line.StartsWith("### ")) { blocks.Add(new('h', line[4..].Trim(), [], [])); continue; }
            if (IsBullet(line))
            {
                var items = new List<string>();
                while (i < lines.Length && IsBullet(lines[i])) items.Add(lines[i++][2..].Trim());
                i--;
                blocks.Add(new('l', "", items, []));
                continue;
            }
            if (line.TrimStart().StartsWith('|'))
            {
                var rows = new List<string[]>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                {
                    var row = lines[i++].Trim();
                    if (!Regex.IsMatch(row, @"^\|[\s:\-|]+\|?$")) rows.Add(row.Trim('|').Split('|').Select(c => c.Trim()).ToArray());
                }
                i--;
                if (rows.Count > 0) blocks.Add(new('t', "", [], rows));
                continue;
            }
            var text = line.Trim();
            while (i + 1 < lines.Length && lines[i + 1].Trim().Length > 0 && !Regex.IsMatch(lines[i + 1], @"^(#|- |\* |\s*\|)"))
                text += " " + lines[++i].Trim();
            blocks.Add(new('p', text, [], []));
        }
        if (sections[0].Blocks.Count == 0) sections.RemoveAt(0);
        return new Chapter(title, sections);
    }

    private static bool IsBullet(string line) => line.StartsWith("- ") || line.StartsWith("* ");

    /// <summary>HTML-sicher, nur **fett** wird umgesetzt.</summary>
    public static MarkupString Inline(string text)
    {
        var html = WebUtility.HtmlEncode(text.Replace("`", ""));
        return new MarkupString(Regex.Replace(html, @"\*\*(.+?)\*\*", "<strong>$1</strong>"));
    }
}
