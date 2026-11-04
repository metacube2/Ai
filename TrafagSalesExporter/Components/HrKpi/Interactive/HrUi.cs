using System.Globalization;

namespace TrafagSalesExporter.Components.HrKpi.Interactive;

/// <summary>Gemeinsame Darstellungshilfen der HR-Interaktivansichten (2026-11-04).</summary>
public static class HrUi
{
    private static readonly string[] Palette = "#F08A4B #4FC3F7 #81C784 #BA68C8 #FFD54F #E57373 #4DB6AC #9575CD #AED581 #FF8A65 #90A4AE #F06292 #7986CB #A1887F".Split(' ');

    /// <summary>Feste Farbe je Schluessel, unabhaengig von der Reihenfolge (stabil ueber Neuberechnungen).</summary>
    public static string Color(string key)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in key)
                hash = (hash * 31) + c;
            return Palette[(int)((uint)hash % (uint)Palette.Length)];
        }
    }

    public static string CohortColor(int index) => Palette[index % Palette.Length];

    /// <summary>In der Managementsicht nur die Personalnummer, sonst der Name.</summary>
    public static string Person(bool managementView, int personalnummer, string name)
        => managementView || string.IsNullOrWhiteSpace(name) ? "#" + personalnummer.ToString(CultureInfo.InvariantCulture) : name;

    public static string Quarter(DateTime day) => $"Q{((day.Month - 1) / 3) + 1} {day.Year}";

    public static string Month(int month) => CultureInfo.GetCultureInfo("de-CH").DateTimeFormat.GetAbbreviatedMonthName(month);

    public static string Pct(double share) => (share * 100).ToString("0.0", CultureInfo.InvariantCulture) + " %";

    public static string Pct(decimal share) => Pct((double)share);

    /// <summary>Jahre, in denen SAP-Absenzen beginnen oder enden, neueste zuerst.</summary>
    public static IReadOnlyList<int> CaseYears(IEnumerable<TrafagSalesExporter.Services.HrAbsenceSapCase> cases, DateTime today)
        => cases.SelectMany(c => new[] { c.Von.Year, c.Bis.Year >= 9000 ? today.Year : c.Bis.Year })
            .Where(y => y <= today.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();
}
