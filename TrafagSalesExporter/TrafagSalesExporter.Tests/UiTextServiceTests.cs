using TrafagSalesExporter.Services;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Tests;

public class UiTextServiceTests
{
    [Fact]
    public void Text_Returns_Selected_Language_Or_English_Fallback()
    {
        var service = new UiTextService();

        Assert.Equal("Standorte", service.Text("Standorte", "Sites"));

        service.SetLanguage("en");
        Assert.Equal("Sites", service.Text("Standorte", "Sites"));

        service.SetLanguage("es");
        Assert.Equal("Sitios", service.Text("Standorte", "Sites"));

        service.SetLanguage("it");
        Assert.Equal("Sedi", service.Text("Standorte", "Sites"));

        service.SetLanguage("hi");
        Assert.Equal("साइटें", service.Text("Standorte", "Sites"));
        Assert.Equal("Untranslated English", service.Text("Nicht uebersetzt", "Untranslated English"));

        service.SetLanguage("sq");
        Assert.Equal("Lokacionet", service.Text("Standorte", "Sites"));

        service.SetLanguage("tr");
        Assert.Equal("Lokasyonlar", service.Text("Standorte", "Sites"));

        service.SetLanguage("tlh");
        Assert.Equal("Daqmey", service.Text("Standorte", "Sites"));

        service.SetLanguage("klingon");
        Assert.Equal("tlh", service.CurrentLanguage);
    }

    [Fact]
    public void Generated_Translations_Cover_Every_Literal_Ui_Key_And_Preserve_Placeholders()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var componentRoot = Path.Combine(projectRoot, "Components");
        Assert.True(Directory.Exists(componentRoot), $"Component directory not found: {componentRoot}");

        var source = string.Join('\n', Directory.GetFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        var matches = Regex.Matches(source,
            @"(?:\bT|UiText\.Text)\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*""((?:[^""\\]|\\.)*)""",
            RegexOptions.Singleline);
        var expected = matches
            .Select(match => new
            {
                German = Regex.Unescape(match.Groups[1].Value),
                English = Regex.Unescape(match.Groups[2].Value)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.German))
            .GroupBy(x => x.German, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().English, StringComparer.OrdinalIgnoreCase);
        var attributeMatches = Regex.Matches(source,
            @"(?<prefix>[A-Za-z]+)De=""((?:[^""\\]|\\.)*)"".*?\k<prefix>En=""((?:[^""\\]|\\.)*)""",
            RegexOptions.Singleline);
        foreach (Match match in attributeMatches)
        {
            var german = Regex.Unescape(match.Groups[2].Value);
            if (!string.IsNullOrWhiteSpace(german))
                expected.TryAdd(german, Regex.Unescape(match.Groups[3].Value));
        }

        // Dynamic cards and records commonly store their German/English text as
        // adjacent constructor arguments before passing the pair to T(...).
        var adjacentMatches = Regex.Matches(source,
            @"""((?:[^""\r\n\\]|\\.)*)""\s*,\s*""((?:[^""\r\n\\]|\\.)*)""",
            RegexOptions.Singleline);
        foreach (Match match in adjacentMatches)
        {
            var german = Regex.Unescape(match.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(german))
                expected.TryAdd(german, Regex.Unescape(match.Groups[2].Value));
        }

        expected["Projekte"] = "Projects";

        foreach (var language in new[] { "es", "it", "hi", "sq", "tr", "tlh" })
        {
            var translations = UiTextGeneratedTranslations.All[language];
            var purchasingTranslations = PurchasingUiTextGeneratedTranslations.All[language];
            var missing = expected.Keys
                .Where(key => !translations.ContainsKey(key) && !purchasingTranslations.ContainsKey(key))
                .ToArray();
            Assert.True(missing.Length == 0, $"{language} is missing: {string.Join(" | ", missing)}");

            foreach (var pair in expected)
            {
                var translated = translations.TryGetValue(pair.Key, out var generalTranslation)
                    ? generalTranslation
                    : purchasingTranslations[pair.Key];
                Assert.False(string.IsNullOrWhiteSpace(translated), $"{language}/{pair.Key} is empty");
                Assert.Equal(Placeholders(pair.Value), Placeholders(translated));
            }
        }
    }

    [Fact]
    public void Purchasing_Translations_Cover_Every_Dynamic_Key_In_Every_Language()
    {
        Assert.Equal(
            PurchasingUiTextCatalog.All.Count,
            PurchasingUiTextCatalog.All.Select(pair => pair.German).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        foreach (var language in new[] { "es", "it", "hi", "sq", "tr", "tlh" })
        {
            var service = new UiTextService();
            service.SetLanguage(language);
            var translations = PurchasingUiTextGeneratedTranslations.All[language];

            foreach (var pair in PurchasingUiTextCatalog.All)
            {
                Assert.True(translations.TryGetValue(pair.German, out var translated),
                    $"{language} is missing dynamic purchasing text: {pair.German}");
                Assert.False(string.IsNullOrWhiteSpace(translated), $"{language}/{pair.German} is empty");
                Assert.Equal(Placeholders(pair.English), Placeholders(translated));

                var resolved = service.Text(pair.German, pair.English);
                Assert.False(string.IsNullOrWhiteSpace(resolved));
                if (language == "tlh")
                    Assert.Equal(PurchasingKlingonOverrides.All[pair.German], resolved);
            }

            Assert.NotEqual("Purchasing analysis", service.Text("Einkauf Analyse", "Purchasing analysis"));
        }

        var albanian = new UiTextService();
        albanian.SetLanguage("sq");
        Assert.NotEqual("purchasing cache full values", albanian.Text("Einkauf Cache Vollwerte", "purchasing cache full values"));
        Assert.NotEqual("Purchasing analysis", albanian.Text("Einkauf Analyse", "Purchasing analysis"));
    }

    [Fact]
    public void Purchasing_Pages_React_To_Language_Changes_And_Language_Is_Scoped()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var program = File.ReadAllText(Path.Combine(projectRoot, "Program.cs"));
        Assert.Contains("AddScoped<IUiTextService, UiTextService>()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<IUiTextService, UiTextService>()", program, StringComparison.Ordinal);

        foreach (var relativePath in new[]
                 {
                     Path.Combine("Components", "Pages", "PurchasingDashboard.razor"),
                     Path.Combine("Components", "Pages", "PurchasingDataSources.razor")
                 })
        {
            var source = File.ReadAllText(Path.Combine(projectRoot, relativePath));
            Assert.Contains("UiText.Changed += HandleLanguageChanged", source, StringComparison.Ordinal);
            Assert.Contains("UiText.Changed -= HandleLanguageChanged", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Klingon_Catalogue_Uses_Latin_Script_Only()
    {
        var invalid = UiTextGeneratedTranslations.All["tlh"]
            .Concat(PurchasingKlingonOverrides.All)
            .Where(pair => pair.Value.Any(character => character > 127 &&
                CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.UppercaseLetter
                    or UnicodeCategory.LowercaseLetter
                    or UnicodeCategory.TitlecaseLetter
                    or UnicodeCategory.OtherLetter))
            .Select(pair => pair.Key)
            .ToArray();

        Assert.True(invalid.Length == 0, $"Non-Latin Klingon entries: {string.Join(" | ", invalid)}");

        var englishBusinessWords = new Regex(
            @"\b(cache|live|position|value|period|sample|schedule|spend|performance|supplier|item|row|simulation|until|matrix|deleted|latest|known|rating|unit|detail|due|short|open|quantities|top|net|fields|headers|productive|actual|quality|issue|procurement|fallback|stock|invoice|material|currency|purchase|order|country|selected)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var klingonFallbacks = PurchasingKlingonOverrides.All
            .Where(pair => englishBusinessWords.IsMatch(pair.Value))
            .Select(pair => pair.Key)
            .ToArray();

        Assert.True(klingonFallbacks.Length == 0,
            $"English business words remain in Klingon purchasing text: {string.Join(" | ", klingonFallbacks)}");
    }

    private static string[] Placeholders(string value) => Regex.Matches(value, "\\{[^{}]+\\}")
        .Select(match => match.Value)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();
}
