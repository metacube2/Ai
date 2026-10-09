namespace TrafagSalesExporter.Services.Projects;

/// <summary>Kleine Anzeigehilfen fuer Trafag Projekte.</summary>
public static class PmText
{
    /// <summary>AD-Anzeigename "Kohler, Ingo" als "Ingo Kohler"; alles andere unveraendert.</summary>
    public static string PersonName(string? name)
    {
        var value = (name ?? string.Empty).Trim();
        var comma = value.IndexOf(", ", StringComparison.Ordinal);
        if (comma <= 0 || value.IndexOf(',', comma + 1) >= 0)
            return value;
        return $"{value[(comma + 2)..].Trim()} {value[..comma].Trim()}";
    }

    public static string Hex(string? color) => string.IsNullOrWhiteSpace(color) ? "#C8501E" : color;
}
