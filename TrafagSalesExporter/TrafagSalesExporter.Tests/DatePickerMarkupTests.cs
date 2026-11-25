using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Tests;

// 2026-11-25 (Meldung Lieferantenentwicklung, Register Zertifikate): MudBlazor 7 uebernimmt das Style eines MudDatePicker
// auch in das Kalenderfenster. Mit Style="max-width:170px" war der Kalender 170 px breit statt mindestens 310 px und
// nur teilweise sichtbar. Breite deshalb immer ueber einen umgebenden Container setzen, nie ueber Style am Picker.
public class DatePickerMarkupTests
{
    // Bis zum Tag-Ende lesen, nicht bis zum ersten '>': Lambdas wie DateChanged="@(d => ...)" enthalten selbst ein '>'.
    private static readonly Regex PickerTag = new(@"<(MudDatePicker|MudDateRangePicker|MudTimePicker)\b.*?(/>|</\1>)", RegexOptions.Singleline);

    private static string Root() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static IEnumerable<(string Datei, string Tag)> PickerTags()
    {
        var root = Root();
        foreach (var ordner in new[] { "Components", "Modules" })
        {
            foreach (var datei in Directory.EnumerateFiles(Path.Combine(root, ordner), "*.razor", SearchOption.AllDirectories))
            {
                foreach (Match m in PickerTag.Matches(File.ReadAllText(datei)))
                    yield return (Path.GetRelativePath(root, datei), m.Value);
            }
        }
    }

    [Fact]
    public void Kein_Datumsfeld_Setzt_Style_Weil_Es_Das_Kalenderfenster_Begrenzt()
    {
        var tags = PickerTags().ToList();
        Assert.NotEmpty(tags);
        var mitStyle = tags.Where(t => Regex.IsMatch(t.Tag, @"\sStyle\s*=")).Select(t => t.Datei + ": " + t.Tag).ToList();
        Assert.True(mitStyle.Count == 0, "Style am Datumsfeld begrenzt den Kalender:\n" + string.Join("\n", mitStyle));
    }

    [Fact]
    public void Datumsfelder_Der_Lieferantenentwicklung_Sind_Eintippbar_Und_Schweizerdeutsch()
    {
        var tags = PickerTags().Where(t => t.Datei.Contains("Lieferantenbewertung", StringComparison.Ordinal)).ToList();
        Assert.Equal(5, tags.Count);
        Assert.All(tags, t =>
        {
            Assert.Contains("Editable=\"true\"", t.Tag, StringComparison.Ordinal);
            Assert.Contains("DateFormat=\"dd.MM.yyyy\"", t.Tag, StringComparison.Ordinal);
            Assert.Contains("Culture=\"@DeCh\"", t.Tag, StringComparison.Ordinal);
        });
    }
}
