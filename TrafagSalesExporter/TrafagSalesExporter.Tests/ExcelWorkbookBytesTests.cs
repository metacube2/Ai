using ClosedXML.Excel;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ExcelWorkbookBytesTests
{
    [Fact]
    public void CreateWorkbookBytes_WritesOneSheetPerTable_WithHeadersAndTypedValues()
    {
        var service = new ExcelExportService();
        var sheets = new List<ExcelSheetData>
        {
            new("Land", new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { ["CountryKey"] = "CH", ["NetSales"] = 1234.50m },
                new Dictionary<string, object?> { ["CountryKey"] = "DE", ["NetSales"] = 9876m }
            }),
            new("Detail", new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { ["Material"] = "ABC", ["Qty"] = 3 }
            })
        };

        var bytes = service.CreateWorkbookBytes(sheets);

        Assert.NotEmpty(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(2, workbook.Worksheets.Count);

        var land = workbook.Worksheet("Land");
        Assert.Equal("CountryKey", land.Cell(1, 1).GetString());
        Assert.Equal("CH", land.Cell(2, 1).GetString());
        Assert.Equal(1234.50, land.Cell(2, 2).GetDouble(), 2); // numeric, not text
        Assert.Equal("Detail", workbook.Worksheet("Detail").Name);
    }

    [Fact]
    public void CreateWorkbookBytes_DeduplicatesAndTruncatesSheetNames()
    {
        var service = new ExcelExportService();
        var longName = new string('X', 40);
        var sheets = new List<ExcelSheetData>
        {
            new(longName, new List<IReadOnlyDictionary<string, object?>>()),
            new(longName, new List<IReadOnlyDictionary<string, object?>>())
        };

        var bytes = service.CreateWorkbookBytes(sheets);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.All(workbook.Worksheets, ws => Assert.True(ws.Name.Length <= 31));
    }

    [Fact]
    public void CreateWorkbookBytes_WithNoSheets_ProducesValidWorkbook()
    {
        var service = new ExcelExportService();

        var bytes = service.CreateWorkbookBytes(new List<ExcelSheetData>());

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Single(workbook.Worksheets);
    }
}
