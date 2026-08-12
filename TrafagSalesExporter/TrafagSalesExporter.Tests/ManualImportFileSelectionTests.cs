using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Tests fuer die Dateiauswahl des Manual-Imports
/// (<see cref="SharePointUploadService.SelectManualImportFileNames"/>).
///
/// Diese Auswahl war bis 2026-07-28 nur ueber einen Graph-Aufruf erreichbar und damit
/// ungetestet - beide bisherigen Produktionsfehler sassen genau hier: die
/// UK-Selbstfuetterung (2026-07-13) und der stillschweigend uebergangene Jahres-Backfill
/// (2026-07-28). Dateinamen in den Tests sind echte Namen aus dem Produktivordner.
/// </summary>
public class ManualImportFileSelectionTests
{
    private static SharePointUploadService.ManualImportCandidate File(string name, string? lastModified = null)
        => new(name, lastModified is null ? null : DateTimeOffset.Parse(lastModified));

    private static IReadOnlyList<string> SelectUk(params SharePointUploadService.ManualImportCandidate[] files)
        => SharePointUploadService.SelectManualImportFileNames(files, "TRUK", isSpainImport: false, preferredYear: null);

    [Fact]
    public void Reads_All_Annual_Files_So_History_And_Current_Year_Coexist()
    {
        // Der Fehler vom 2026-07-28: TRUK_2025.xlsx wurde nie gelesen, weil
        // 110326_TRUK_2026YTD.xlsx als Jahr 2026 gewann und nur EINE Jahresdatei genommen
        // wurde. UK konnte dadurch strukturell nie 2025 und 2026 gleichzeitig fuehren.
        var selected = SelectUk(
            File("110326_TRUK_2026YTD.xlsx", "2026-03-11T08:00:00Z"),
            File("TRUK_2025.xlsx", "2026-07-28T14:00:00Z"));

        Assert.Contains("TRUK_2025.xlsx", selected);
        Assert.Contains("110326_TRUK_2026YTD.xlsx", selected);
    }

    [Fact]
    public void Orders_Annual_Files_Ascending_By_Year()
    {
        // Aufsteigend, damit bei gleichem Belegschluessel der neuere Jahrgang gewinnt -
        // die Deduplizierung laesst die spaeter gelesene Zeile gewinnen.
        var selected = SelectUk(
            File("110326_TRUK_2026YTD.xlsx", "2026-03-11T08:00:00Z"),
            File("TRUK_2025.xlsx", "2026-07-28T14:00:00Z"));

        Assert.Equal(
            ["TRUK_2025.xlsx", "110326_TRUK_2026YTD.xlsx"],
            selected.Take(2));
    }

    [Fact]
    public void Takes_Only_The_Newest_File_Per_Year()
    {
        // Mehrere Staende desselben Jahres (Korrekturlauf/Neu-Upload): der aktuelle gilt,
        // nicht beide zusammen.
        var selected = SelectUk(
            File("TRUK_2025.xlsx", "2026-07-28T14:00:00Z"),
            File("TRUK_2025_korrigiert.xlsx", "2026-07-28T16:00:00Z"));

        Assert.Equal(["TRUK_2025_korrigiert.xlsx"], selected);
    }

    [Fact]
    public void Adds_Deltas_Newer_Than_The_Newest_Annual_File()
    {
        var selected = SelectUk(
            File("TRUK_2025.xlsx", "2026-07-28T14:00:00Z"),
            File("110326_TRUK_2026YTD.xlsx", "2026-03-11T08:00:00Z"),
            File("130326_TRUK.xlsx", "2026-03-13T08:00:00Z"),
            File("160326_TRUK.xlsx", "2026-03-16T08:00:00Z"));

        Assert.Contains("130326_TRUK.xlsx", selected);
        Assert.Contains("160326_TRUK.xlsx", selected);
        // Deltas nach den Jahresdateien, aufsteigend nach Datum.
        Assert.True(selected.ToList().IndexOf("130326_TRUK.xlsx") > selected.ToList().IndexOf("110326_TRUK_2026YTD.xlsx"));
        Assert.True(selected.ToList().IndexOf("160326_TRUK.xlsx") > selected.ToList().IndexOf("130326_TRUK.xlsx"));
    }

    [Fact]
    public void Ignores_Deltas_Older_Than_The_Newest_Annual_File()
    {
        // Ein Delta vor dem Basisstand ist im Basisstand bereits enthalten.
        var selected = SelectUk(
            File("110326_TRUK_2026YTD.xlsx", "2026-03-11T08:00:00Z"),
            File("050326_TRUK.xlsx", "2026-03-05T08:00:00Z"));

        Assert.DoesNotContain("050326_TRUK.xlsx", selected);
    }

    [Fact]
    public void Excludes_Own_Export_Output_So_The_Import_Cannot_Feed_Itself()
    {
        // Der Bug vom 2026-07-13: Der Standortexport laedt seine Ausgabe in denselben
        // Ordner, aus dem der Import liest. Ohne diesen Ausschluss ersetzte UK seinen
        // Bestand taeglich durch die eigene Audit-CSV vom Vortag.
        var selected = SelectUk(
            File("110326_TRUK_2026YTD.xlsx", "2026-03-11T08:00:00Z"),
            File("Sales_TRUK_2026-05-11.xlsx", "2026-05-11T08:00:00Z"),
            File("Sales_ProcessedMergeInput_TRUK_2026-07-27.csv", "2026-07-27T08:00:00Z"));

        Assert.DoesNotContain("Sales_TRUK_2026-05-11.xlsx", selected);
        Assert.DoesNotContain("Sales_ProcessedMergeInput_TRUK_2026-07-27.csv", selected);
        Assert.Equal(["110326_TRUK_2026YTD.xlsx"], selected);
    }

    [Fact]
    public void Ignores_Files_Of_Other_Sites()
    {
        var selected = SelectUk(
            File("110326_TRUK_2026YTD.xlsx", "2026-03-11T08:00:00Z"),
            File("TRDE_2025.xlsx", "2026-07-28T14:00:00Z"));

        Assert.DoesNotContain("TRDE_2025.xlsx", selected);
    }

    [Fact]
    public void Spain_Reads_Base_File_Before_Range_Files()
    {
        var selected = SharePointUploadService.SelectManualImportFileNames(
            [
                File("Spain_Sales_range_20260528_to_20260603.csv", "2026-06-03T08:00:00Z"),
                File("Spain_Sales_2025.csv", "2026-01-05T08:00:00Z"),
                File("Spain_Sales_range_20260101_to_20260601.csv", "2026-07-28T08:00:00Z")
            ],
            "TRSE",
            isSpainImport: true,
            preferredYear: null);

        Assert.Equal(
            [
                "Spain_Sales_2025.csv",
                "Spain_Sales_range_20260101_to_20260601.csv",
                "Spain_Sales_range_20260528_to_20260603.csv"
            ],
            selected);
    }

    [Fact]
    public void Spain_Ignores_Files_That_Are_Not_Spain_Sales_Csv()
    {
        var selected = SharePointUploadService.SelectManualImportFileNames(
            [
                File("Spain_Sales_2025.csv", "2026-01-05T08:00:00Z"),
                File("Sales_TRSE_2026-05-20.xlsx", "2026-05-20T08:00:00Z")
            ],
            "TRSE",
            isSpainImport: true,
            preferredYear: null);

        Assert.Equal(["Spain_Sales_2025.csv"], selected);
    }

    [Fact]
    public void Explicit_Year_Selects_That_Years_Annual_File()
    {
        var selected = SharePointUploadService.SelectManualImportFileNames(
            [
                File("TRUK_2025.xlsx", "2026-07-28T14:00:00Z"),
                File("110326_TRUK_2026YTD.xlsx", "2026-03-11T08:00:00Z")
            ],
            "TRUK",
            isSpainImport: false,
            preferredYear: 2025);

        Assert.Equal(["TRUK_2025.xlsx"], selected);
    }

    [Fact]
    public void Falls_Back_To_Dated_Files_When_No_Annual_File_Exists()
    {
        var selected = SelectUk(
            File("130326_TRUK.xlsx", "2026-03-13T08:00:00Z"),
            File("160326_TRUK.xlsx", "2026-03-16T08:00:00Z"));

        Assert.Equal(["130326_TRUK.xlsx", "160326_TRUK.xlsx"], selected);
    }

    [Fact]
    public void Throws_When_Folder_Has_Nothing_Usable()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            SelectUk(File("liesmich.txt", "2026-07-28T14:00:00Z")));

        Assert.Contains("TRUK", error.Message);
    }
}
