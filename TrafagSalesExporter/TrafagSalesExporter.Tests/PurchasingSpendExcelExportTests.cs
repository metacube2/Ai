using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>Excel-Download im Spend-Aufriss (Wunsch Armin, Auftrag Ingo 24.11.2026): keine Kombifelder, Betraege als Zahlen.</summary>
public class PurchasingSpendExcelExportTests
{
    [Theory]
    // Warengruppe aus dem Katalog T023T: Gedankenstrich (PurchasingMaterialGroupTextCatalog.Resolve).
    [InlineData("10.08.00 – Leiterplatten bestüc", "10.08.00", "Leiterplatten bestüc")]
    // Material aus MaterialLabelSql: Bindestrich; Text darf selbst einen Bindestrich enthalten.
    [InlineData("H20201 - Thermostat - Kapillar 2 m", "H20201", "Thermostat - Kapillar 2 m")]
    [InlineData("064776 - Stanz- Biegeteil", "064776", "Stanz- Biegeteil")]
    // Geviertstrich als Variante.
    [InlineData("D13006 — Membran", "D13006", "Membran")]
    // Code ohne Text: Warengruppe ohne Katalogtext, Material ohne MAKT.
    [InlineData("99.99.99", "99.99.99", "")]
    [InlineData("H17500", "H17500", "")]
    // Mehrere Woerter ohne Trenner: Platzhalter oder Bestelltext, also Text ohne Code.
    [InlineData("ohne Warengruppe", "", "ohne Warengruppe")]
    [InlineData("Reparatur Pumpe", "", "Reparatur Pumpe")]
    // Bindestrich ohne Leerzeichen gehoert zum Code.
    [InlineData("AB-123", "AB-123", "")]
    [InlineData("", "", "")]
    [InlineData(null, "", "")]
    public void SplitCodeText_SplitsCodeAndText(string? label, string code, string text)
    {
        var result = PurchasingSpendExcelExport.SplitCodeText(label);

        Assert.Equal(code, result.Code);
        Assert.Equal(text, result.Text);
    }

    [Fact]
    public void SplitCodeText_MatchesCatalogFormatOfMaterialGroup()
    {
        var label = PurchasingMaterialGroupTextCatalog.Resolve("10.08.00");

        var (code, text) = PurchasingSpendExcelExport.SplitCodeText(label);

        Assert.Equal("10.08.00", code);
        Assert.False(string.IsNullOrEmpty(text));
        Assert.DoesNotContain("–", text);
    }

    [Fact]
    public void BuildCascadeTable_ExportsAllLevelsWithSeparateCodeAndTextColumns()
    {
        var table = PurchasingSpendExcelExport.BuildCascadeTable(SamplePerspective(), [2025, 2026]);

        Assert.Equal(
            [
                "Ebene", "Lieferant", "Warengruppe", "Warengruppe Text", "Material", "Material Text",
                "Bestellwert CHF 2025", "Bestellwert CHF 2026", "Bestellwert CHF Gesamt"
            ],
            table.Columns.Select(column => column.Header).ToArray());

        // Ganzer Baum, unabhaengig vom Aufklappzustand: 2 Lieferanten, 3 Warengruppen, 4 Materialien.
        Assert.Equal(9, table.Rows.Count);
        Assert.Equal([1, 2, 3, 3, 2, 3, 1, 2, 3], table.Rows.Select(row => (int)row[0]!).ToArray());

        // Zeile Material unter Lieferant A / 10.08.00: Vorfahren wiederholt, Code und Text getrennt.
        var material = table.Rows[2];
        Assert.Equal("Lieferant A AG", material[1]);
        Assert.Equal("10.08.00", material[2]);
        Assert.Equal("Leiterplatten bestüc", material[3]);
        Assert.Equal("H20201", material[4]);
        Assert.Equal("Leiterplatte - bestueckt", material[5]);
        Assert.Equal(100m, material[6]);
        Assert.Equal(0m, material[7]);
        Assert.Equal(100m, material[8]);

        // Material ohne Text: leerer Text, kein Fehler.
        var withoutText = table.Rows[3];
        Assert.Equal("H17500", withoutText[4]);
        Assert.Equal(string.Empty, withoutText[5]);

        // Ebene 1 hat leere tiefere Spalten.
        Assert.Null(table.Rows[0][2]);
        Assert.Null(table.Rows[0][4]);
    }

    [Fact]
    public void BuildCascadeTable_LevelSumsAreEqualAndTotalIsSumOfYears()
    {
        var table = PurchasingSpendExcelExport.BuildCascadeTable(SamplePerspective(), [2025, 2026]);

        decimal SumOfLevel(int level, int column)
            => table.Rows.Where(row => (int)row[0]! == level).Sum(row => (decimal)row[column]!);

        var total = SumOfLevel(1, 8);
        Assert.Equal(1000m, total);
        Assert.Equal(total, SumOfLevel(2, 8));
        Assert.Equal(total, SumOfLevel(3, 8));
        foreach (var row in table.Rows)
            Assert.Equal((decimal)row[8]!, (decimal)row[6]! + (decimal)row[7]!);
    }

    [Fact]
    public void BuildCascadeTable_DoesNotSplitSupplierEvenWithHyphen()
    {
        var perspective = new PurchasingSpendPerspectiveResult(
            "supplier", "Lieferant", "Supplier", ["Lieferant"], ["Supplier"],
            [Node("Müller - Söhne AG", 5m, 2026)], ["supplier"]);

        var table = PurchasingSpendExcelExport.BuildCascadeTable(perspective, [2026]);

        Assert.Equal(["Ebene", "Lieferant", "Bestellwert CHF 2026", "Bestellwert CHF Gesamt"], table.Columns.Select(c => c.Header).ToArray());
        Assert.Equal("Müller - Söhne AG", table.Rows[0][1]);
    }

    [Fact]
    public void CreateWorkbookBytes_WritesNumbersAsNumericCellsWithFormatAndFrozenBoldHeader()
    {
        var table = PurchasingSpendExcelExport.BuildCascadeExport(
            SamplePerspective(), [2025, 2026], onlyPurchasingParts: true, "2025-01 - 2026-11", new DateTime(2026, 11, 24, 10, 0, 0));

        var bytes = PurchasingSpendExcelExport.CreateWorkbookBytes(table);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        Assert.Equal("Produktgruppe nach Verwendung", ws.Name.Substring(0, 29));
        Assert.True(ws.Name.Length <= 31);
        Assert.DoesNotContain(":", ws.Name);
        Assert.Equal("Warengruppe Text", ws.Cell(1, 4).GetString());
        Assert.True(ws.Cell(1, 1).Style.Font.Bold);
        Assert.Equal(1, ws.SheetView.SplitRow);
        Assert.True(ws.AutoFilter.IsEnabled);

        // Zeile 4 = Material H20201.
        Assert.Equal("H20201", ws.Cell(4, 5).GetString());
        Assert.Equal(XLDataType.Text, ws.Cell(4, 5).DataType);
        Assert.Equal(XLDataType.Number, ws.Cell(4, 7).DataType);
        Assert.Equal(100d, ws.Cell(4, 7).GetDouble());
        Assert.Equal("#,##0.00", ws.Cell(4, 7).Style.NumberFormat.Format);
        Assert.Equal(XLDataType.Number, ws.Cell(4, 8).DataType);
        Assert.Equal(XLDataType.Number, ws.Cell(2, 1).DataType);

        var info = workbook.Worksheet("Info");
        Assert.Contains(info.Column(1).CellsUsed(), cell => cell.GetString() == "Umfang");
        Assert.Contains(info.Column(2).CellsUsed(), cell => cell.GetString() == PurchasingSpendExcelExport.ScopePurchasingParts);
        Assert.Contains(info.Column(2).CellsUsed(), cell => cell.GetString() == "2025-01 - 2026-11");
    }

    [Fact]
    public void BuildRegionTable_SplitsMaterialGroupAndWritesShareAsFraction()
    {
        var groups = new List<PurchasingRegionPieGroup>
        {
            new("10.08.00 – Leiterplatten bestüc", 200m,
                [new PurchasingLiveChartPoint("CH", 150m), new PurchasingLiveChartPoint("DE", 50m)])
        };

        var table = PurchasingSpendExcelExport.BuildRegionTable(groups);

        Assert.Equal(
            ["Warengruppe", "Warengruppe Text", "Lieferantenland", "Bestellwert CHF", "Anteil an Warengruppe", "Bestellwert Warengruppe CHF"],
            table.Columns.Select(c => c.Header).ToArray());
        Assert.Equal("10.08.00", table.Rows[0][0]);
        Assert.Equal("Leiterplatten bestüc", table.Rows[0][1]);
        Assert.Equal(0.75m, table.Rows[0][4]);

        using var workbook = new XLWorkbook(new MemoryStream(PurchasingSpendExcelExport.CreateWorkbookBytes(table)));
        var cell = workbook.Worksheet(1).Cell(2, 5);
        Assert.Equal(XLDataType.Number, cell.DataType);
        Assert.Equal("0.0%", cell.Style.NumberFormat.Format);
    }

    [Fact]
    public void BuildAbcXyzActionTable_HasSeparateClassesAndNumericCounts()
    {
        var rows = new List<PurchasingAbcXyzActionRow>
        {
            new("AX", "A", "X", 1234.5m, 12, 3, "Rahmenvertrag pruefen", "Review framework", "High")
        };

        var table = PurchasingSpendExcelExport.BuildAbcXyzActionTable(rows);

        Assert.Equal(
            ["Klasse", "ABC", "XYZ", "Spend CHF", "Materialien", "Lieferanten", "Empfohlener Pruefauftrag", "Prioritaet"],
            table.Columns.Select(c => c.Header).ToArray());
        Assert.Equal(new object?[] { "AX", "A", "X", 1234.5m, 12, 3, "Rahmenvertrag pruefen", "hoch" }, table.Rows[0]);
    }

    [Fact]
    public void BuildXyzCountTable_WritesCountsAndShares()
    {
        var table = PurchasingSpendExcelExport.BuildXyzCountTable(
            [new PurchasingLiveChartPoint("X", 30m), new PurchasingLiveChartPoint("ohne XYZ", 10m)]);

        Assert.Equal(["XYZ-Klasse", "Anzahl Artikel", "Anteil"], table.Columns.Select(c => c.Header).ToArray());
        Assert.Equal(30m, table.Rows[0][1]);
        Assert.Equal(0.75m, table.Rows[0][2]);
    }

    [Fact]
    public void BuildMaterialShareTable_TakesPartCodeFromKeyAndKeepsAllLevels()
    {
        var part = new PurchasingMaterialShareNode("FP_THERMOST.|10.08.00|H20201", "H20201 Leiterplatte bestückt", 40m, 0m, 8m, "ST", 5m, [], ConfirmedQuantity: 4m);
        var partWithoutText = new PurchasingMaterialShareNode("FP_THERMOST.|10.08.00|H17500", "H17500", 10m, 0m, 2m, "ST", 5m, []);
        var materialGroup = new PurchasingMaterialShareNode("FP_THERMOST.|10.08.00", "10.08.00 – Leiterplatten bestüc", 50m, 0m, 0m, string.Empty, 0m, [part, partWithoutText], ConfirmedQuantity: 4m);
        var group = new PurchasingMaterialShareNode("FP_THERMOST.", "FP_THERMOST.", 50m, 200m, 0m, string.Empty, 0m, [materialGroup], 120m, 1m, 6m);

        var table = PurchasingSpendExcelExport.BuildMaterialShareTable([group]);

        Assert.Equal(
            [
                "Ebene", "Produktgruppe", "Materialgruppe", "Materialgruppe Text", "Material", "Material Text",
                "Bestaetigte Menge", "Umsatz bestaetigt CHF", "Herstellkosten HK CHF", "HK-Abdeckung Umsatz", "Materialanteil CHF", "Anteil",
                "Anteil bezogen auf", "Bedarfsmenge", "Einheit", "Stueckkosten CHF"
            ],
            table.Columns.Select(c => c.Header).ToArray());
        Assert.Equal(4, table.Rows.Count);
        Assert.Equal(0.25m, table.Rows[0][11]);
        Assert.Equal("10.08.00", table.Rows[1][2]);
        Assert.Equal("Leiterplatten bestüc", table.Rows[1][3]);
        Assert.Equal("H20201", table.Rows[2][4]);
        Assert.Equal("Leiterplatte bestückt", table.Rows[2][5]);
        Assert.Equal(0.8m, table.Rows[2][11]);
        Assert.Equal("H17500", table.Rows[3][4]);
        Assert.Equal(6m, table.Rows[0][6]);
        Assert.Equal(4m, table.Rows[1][6]);
        Assert.Equal(4m, table.Rows[2][6]);

        using var workbook = new XLWorkbook(new MemoryStream(PurchasingSpendExcelExport.CreateWorkbookBytes(table)));
        var ws = workbook.Worksheet(1);
        Assert.Equal("Bestaetigte Menge", ws.Cell(1, 7).GetString());
        Assert.Equal(XLDataType.Number, ws.Cell(2, 7).DataType);
        Assert.Equal(6d, ws.Cell(2, 7).GetDouble());
        Assert.Equal("#,##0", ws.Cell(2, 7).Style.NumberFormat.Format);
        Assert.Equal(string.Empty, table.Rows[3][5]);
    }

    [Theory]
    [InlineData("Produktgruppe nach Verwendung : Lieferant", "Spend_Aufriss_Produktgruppe_nach_Verwendung_Lieferant_2026-11-24.xlsx")]
    [InlineData("Lieferant", "Spend_Aufriss_Lieferant_2026-11-24.xlsx")]
    [InlineData("ABC-XYZ-Massnahmenmatrix", "Spend_Aufriss_ABC-XYZ-Massnahmenmatrix_2026-11-24.xlsx")]
    [InlineData("", "Spend_Aufriss_Aufriss_2026-11-24.xlsx")]
    public void FileName_IsSafeAndDated(string aufriss, string expected)
        => Assert.Equal(expected, PurchasingSpendExcelExport.FileName(aufriss, new DateTime(2026, 11, 24)));

    [Theory]
    [InlineData("Produktgruppe nach Verwendung : Warengruppe")]
    [InlineData("Beschaffungsregion : Warengruppe")]
    [InlineData("a/b\\c?d*e[f]g")]
    public void SheetName_IsValidForExcel(string name)
    {
        var sheet = PurchasingSpendExcelExport.SheetName(name);

        Assert.True(sheet.Length is > 0 and <= 31);
        Assert.DoesNotContain(sheet, ch => ch is ':' or '\\' or '/' or '?' or '*' or '[' or ']');
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add(sheet);
    }

    private static PurchasingSpendPerspectiveResult SamplePerspective()
    {
        var supplierA = new PurchasingSpendCascadeNode("Lieferant A AG", Years((2025, 300m), (2026, 300m)), 600m,
        [
            new PurchasingSpendCascadeNode("10.08.00 – Leiterplatten bestüc", Years((2025, 100m), (2026, 50m)), 150m,
            [
                Node("H20201 - Leiterplatte - bestueckt", 100m, 2025),
                Node("H17500", 50m, 2026)
            ]),
            new PurchasingSpendCascadeNode("ohne Warengruppe", Years((2025, 200m), (2026, 250m)), 450m,
            [
                new PurchasingSpendCascadeNode("Reparatur Pumpe", Years((2025, 200m), (2026, 250m)), 450m, [])
            ])
        ]);
        var supplierB = new PurchasingSpendCascadeNode("Lieferant B", Years((2026, 400m)), 400m,
        [
            new PurchasingSpendCascadeNode("20.06.00 – Stanz- Biegeteile", Years((2026, 400m)), 400m,
            [
                Node("D13006 - Membran", 400m, 2026)
            ])
        ]);
        return new PurchasingSpendPerspectiveResult(
            "usage-productgroup-supplier",
            "Produktgruppe nach Verwendung : Lieferant",
            "Product group by usage : supplier",
            ["Lieferant", "Warengruppe", "Material"],
            ["Supplier", "Material group", "Material"],
            [supplierA, supplierB],
            ["supplier", "materialgroup", "article"]);
    }

    private static PurchasingSpendCascadeNode Node(string label, decimal value, int year)
        => new(label, Years((year, value)), value, []);

    private static IReadOnlyDictionary<int, decimal> Years(params (int Year, decimal Value)[] values)
        => values.ToDictionary(item => item.Year, item => item.Value);
}
