using System.Text.Json;
using AgentMonitor.Core;

namespace AgentMonitor.Tests;

public class ParsingAndSummaryTests
{
    private static JsonElement J(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void Parse_liest_Pflicht_und_Zusatzfelder()
    {
        var e = HookEvent.Parse("""
            {"hook_event_name":"PostToolUse","session_id":"abc","agent_id":"","agent_type":"Explore",
             "tool_name":"Read","tool_use_id":"tu1","duration_ms":42,"tool_input":{"file_path":"C:\\x\\a.cs"},
             "unbekannt":{"tief":[1,2,3]}}
            """);
        Assert.NotNull(e);
        Assert.Equal("PostToolUse", e!.Event);
        Assert.Null(e.AgentId); // leerer String gilt als Hauptagent
        Assert.Equal("Explore", e.AgentType);
        Assert.Equal(42, e.DurationMs);
        Assert.Equal("liest a.cs", ToolSummary.Describe(e.ToolName, e.ToolInput));
        Assert.Equal("C:/x", ToolSummary.Detail(e.ToolName, e.ToolInput));
    }

    [Theory]
    [InlineData("""{"session_id":"x"}""")]
    [InlineData("""{"hook_event_name":"Stop"}""")]
    [InlineData("""[1,2]""")]
    [InlineData("""kein json""")]
    public void Parse_lehnt_unvollstaendiges_ab(string json) => Assert.Null(HookEvent.Parse(json));

    [Fact]
    public void Bash_zeigt_Beschreibung_vor_Befehl_und_kuerzt()
    {
        Assert.Equal("Tests", ToolSummary.Describe("Bash", J("""{"command":"dotnet test","description":"Tests"}""")));
        var lang = ToolSummary.Describe("Bash", J($$"""{"command":"{{new string('x', 200)}}"}"""));
        Assert.Equal(ToolSummary.MaxLength, lang.Length);
        Assert.EndsWith("…", lang);
    }

    [Fact]
    public void Mehrzeilige_Befehle_werden_einzeilig()
    {
        Assert.Equal("führt aus: a b", ToolSummary.Describe("PowerShell", J("""{"command":"a\r\n\n   b"}""")));
    }

    [Fact]
    public void WebFetch_zeigt_nur_den_Host()
    {
        Assert.Equal("lädt Webseite example.org", ToolSummary.Describe("WebFetch", J("""{"url":"https://example.org/geheim?token=1"}""")));
    }

    [Fact]
    public void Agent_zeigt_Typ_und_Beschreibung()
    {
        Assert.Equal("startet Unteragent Explore – Suche", ToolSummary.Describe("Agent", J("""{"subagent_type":"Explore","description":"Suche","prompt":"langer Text"}""")));
    }

    [Fact]
    public void MCP_Namen_werden_lesbar()
    {
        Assert.Equal("navigate", ToolSummary.DisplayName("mcp__claude-in-chrome__navigate"));
        Assert.Equal("Browser: navigate example.org", ToolSummary.Describe("mcp__claude-in-chrome__navigate", J("""{"url":"https://example.org/x"}""")));
    }

    [Fact]
    public void Ohne_Eingabe_leere_Beschreibung()
    {
        Assert.Equal("liest", ToolSummary.Describe("Read", null));
        Assert.Null(ToolSummary.Detail("Read", null));
    }
}
