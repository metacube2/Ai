using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// 2026-11-16 (Meldung Ingo): „Excel oeffnen“ auf der Export-Seite startete die Datei per Process.Start auf dem Webserver und
/// scheiterte im IIS mit „Access is denied“. Seither Download in den Browser.
/// </summary>
public class ExportFileDownloadTests
{
    [Theory]
    [InlineData(@"C:\inetpub\wwwcust\BiDashboard\output\Sales_All_2026-11-13.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("Export.XLSX", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("liste.csv", "text/csv")]
    [InlineData("datei.bin", "application/octet-stream")]
    [InlineData("ohne_endung", "application/octet-stream")]
    public void Inhaltstyp_Nach_Endung(string pfad, string erwartet)
        => Assert.Equal(erwartet, ExportFileDownload.ContentType(pfad));

    [Fact]
    public void Keine_Seite_Startet_Dateien_Auf_Dem_Server()
    {
        // Process.Start in einer Seite laeuft auf dem Webserver, nicht beim Benutzer. Dateien gehen als Download an den Browser.
        var root = Path.Combine(ShopfloorTestEnv.ProjectRoot, "Components");
        var treffer = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => File.ReadAllText(f).Contains("Process.Start(", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(root, f))
            .ToList();
        Assert.Empty(treffer);
    }

    [Fact]
    public void Download_Js_Hat_SaveStream_Und_Wird_Mit_Version_Geladen()
    {
        var js = File.ReadAllText(Path.Combine(ShopfloorTestEnv.ProjectRoot, "wwwroot", "js", "download.js"));
        Assert.Contains("saveStream", js);
        var app = File.ReadAllText(Path.Combine(ShopfloorTestEnv.ProjectRoot, "Components", "App.razor"));
        Assert.Contains("js/download.js?v=@AssetVersion", app);
    }
}
