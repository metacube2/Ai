namespace TrafagSalesExporter.Services;

public static class MaterialKeyNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = new string(value
            .Trim()
            .ToUpperInvariant()
            .Where(ch => !char.IsWhiteSpace(ch))
            .ToArray());

        var withoutLeadingZeros = normalized.TrimStart('0');
        return string.IsNullOrWhiteSpace(withoutLeadingZeros) ? "0" : withoutLeadingZeros;
    }
}
