using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class SharePointFolderPagingTests
{
    /// <summary>
    /// ISS-020: Der Spanien-Import las nur die erste Graph-Seite (hoechstens 200 Eintraege) des
    /// Ordners. Ab Mitte Juli 2026 lagen die neuesten Spanien-Exporte auf Folgeseiten und kamen
    /// nie in der Datenbank an. Alle Seiten muessen eingesammelt werden.
    /// </summary>
    [Fact]
    public async Task CollectAllPagesAsync_FollowsNextLinksUntilTheLastPage()
    {
        var pages = new Dictionary<string, (List<string>? Items, string? NextLink)>
        {
            ["seite-2"] = (["c", "d"], "seite-3"),
            ["seite-3"] = (["e"], null),
        };
        var requested = new List<string>();

        var result = await SharePointUploadService.CollectAllPagesAsync(
            ["a", "b"],
            "seite-2",
            link =>
            {
                requested.Add(link);
                return Task.FromResult(pages[link]);
            });

        Assert.Equal(["a", "b", "c", "d", "e"], result);
        Assert.Equal(["seite-2", "seite-3"], requested);
    }

    [Fact]
    public async Task CollectAllPagesAsync_WithoutNextLink_ReturnsFirstPageOnly()
    {
        var result = await SharePointUploadService.CollectAllPagesAsync<string>(
            ["a"],
            null,
            _ => throw new InvalidOperationException("Es darf keine Folgeseite geladen werden."));

        Assert.Equal(["a"], result);
    }

    [Fact]
    public async Task CollectAllPagesAsync_EmptyPagesAndNullFirstPage_AreTolerated()
    {
        var result = await SharePointUploadService.CollectAllPagesAsync<string>(
            null,
            "seite-2",
            _ => Task.FromResult<(List<string>?, string?)>((null, null)));

        Assert.Empty(result);
    }

    [Fact]
    public async Task CollectAllPagesAsync_StopsAfterMaxPages()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SharePointUploadService.CollectAllPagesAsync<string>(
                [],
                "immer",
                _ => Task.FromResult<(List<string>?, string?)>((["x"], "immer")),
                maxPages: 5));
    }
}
