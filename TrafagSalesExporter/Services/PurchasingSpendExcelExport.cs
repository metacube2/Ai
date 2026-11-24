using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>Zellart einer Exportspalte; bestimmt Datentyp und Zahlenformat in Excel.</summary>
public enum SpendExcelColumnKind
{
    Text,
    Count,
    Amount,
    Percent,
    Quantity,
    UnitCost
}

public sealed record SpendExcelColumn(string Header, SpendExcelColumnKind Kind);

/// <summary>
/// Eine Exporttabelle: Spalten, Zeilen (ein Wert je Spalte; Text als string, Zahlen als decimal/int, leer als null) und
/// Angaben fuer das Blatt „Info“ (Aufriss, Zeitraum, Schalter).
/// </summary>
public sealed record SpendExcelTable(
    string Title,
    IReadOnlyList<SpendExcelColumn> Columns,
    IReadOnlyList<object?[]> Rows,
    IReadOnlyList<KeyValuePair<string, string>> Info);

/// <summary>
/// Excel-Download je Tabelle im Spend-Aufriss (Wunsch Armin, Auftrag Ingo 24.11.2026): „keine Kombi-Felder, so dass die
/// Informationen im Excel ohne Nacharbeit genutzt werden koennen“. Code und Text stehen in zwei Spalten, Betraege,
/// Prozente und Mengen sind Zahlen mit Zahlenformat, die Waehrung steht im Spaltenkopf. Die Kaskade wird ganz exportiert
/// (alle Ebenen, Spalte „Ebene“, Vorfahren in jeder Zeile wiederholt), unabhaengig vom Aufklappzustand.
/// Die Spaltenkoepfe sind bewusst deutsch (Arbeitssprache Einkauf).
/// </summary>
public static class PurchasingSpendExcelExport
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Ebenenschluessel, deren Anzeige „Code – Text“ ist und getrennt wird (siehe <see cref="PurchasingSpendPerspectiveResult.LevelKeys"/>).</summary>
    public const string LevelKeyMaterialGroup = "materialgroup";
    public const string LevelKeyArticle = "article";

    // Trenner zwischen Code und Text in den Anzeigedaten: Warengruppe „10.08.00 – Text“ (Gedankenstrich, Katalog T023T),
    // Material „Matnr - Maktx“ (Bindestrich, MaterialLabelSql). Geviertstrich als Variante.
    private static readonly string[] Separators = [" – ", " — ", " - "];

    private const string AmountFormat = "#,##0.00";
    private const string PercentFormat = "0.0%";
    private const string CountFormat = "#,##0";
    private const string QuantityFormat = "#,##0.###";
    private const string UnitCostFormat = "#,##0.00####";
    private const int AdjustRowLimit = 500;

    /// <summary>
    /// Trennt eine Anzeige „Code – Text“ in Code und Text. Getrennt wird nur am ersten Leerzeichen und nur, wenn dort ein
    /// Trenner steht (Code ohne Leerzeichen, SAP-Nummern haben keine). Ohne Trenner: ein Wort ist ein Code ohne Text
    /// (z.B. Warengruppe ohne Katalogtext), mehrere Woerter sind Text ohne Code (z.B. „ohne Warengruppe“, Bestelltext
    /// einer Textbestellung). Leere Eingabe ergibt zwei leere Felder.
    /// </summary>
    public static (string Code, string Text) SplitCodeText(string? label)
    {
        var value = (label ?? string.Empty).Trim();
        if (value.Length == 0)
            return (string.Empty, string.Empty);

        var firstSpace = value.IndexOfAny([' ', '\t']);
        if (firstSpace < 0)
            return (value, string.Empty);

        foreach (var separator in Separators)
        {
            if (string.CompareOrdinal(value, firstSpace, separator, 0, separator.Length) == 0)
                return (value[..firstSpace], value[(firstSpace + separator.Length)..].Trim());
        }

        return (string.Empty, value);
    }

    /// <summary>Blattname: ohne die in Excel verbotenen Zeichen, hoechstens 31 Zeichen.</summary>
    public static string SheetName(string? name)
    {
        var cleaned = new StringBuilder();
        foreach (var ch in name ?? string.Empty)
            cleaned.Append(ch is ':' or '\\' or '/' or '?' or '*' or '[' or ']' ? ' ' : ch);
        var result = CollapseSpaces(cleaned.ToString()).Trim('\'', ' ');
        if (result.Length == 0)
            result = "Aufriss";
        return result.Length > 31 ? result[..31].TrimEnd() : result;
    }

    /// <summary>Dateiname <c>Spend_Aufriss_&lt;Aufriss&gt;_&lt;JJJJ-MM-TT&gt;.xlsx</c>, nur Buchstaben, Ziffern, Unterstrich und Bindestrich.</summary>
    public static string FileName(string? aufriss, DateTime date)
    {
        var builder = new StringBuilder();
        foreach (var ch in (aufriss ?? string.Empty).Normalize(NormalizationForm.FormC))
        {
            if (char.IsLetterOrDigit(ch) || ch == '-')
                builder.Append(ch);
            else if (builder.Length > 0 && builder[^1] != '_')
                builder.Append('_');
        }

        var part = builder.ToString().Trim('_');
        if (part.Length == 0)
            part = "Aufriss";
        return $"Spend_Aufriss_{part}_{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.xlsx";
    }

    /// <summary>
    /// Kaskade eines Einstiegs, ganzer Baum in Tiefensuche: je Knoten eine Zeile mit Ebene, den Werten aller Ebenen bis
    /// zum Knoten (Vorfahren wiederholt, tiefere Ebenen leer), je Jahr dem Bestellwert und der Gesamtsumme.
    /// Die Summe der Ebene 1 ist der Gesamtwert; ueber alle Ebenen summiert zaehlt jeder Wert mehrfach.
    /// </summary>
    public static SpendExcelTable BuildCascadeTable(
        PurchasingSpendPerspectiveResult perspective,
        IReadOnlyList<int> years,
        IReadOnlyList<KeyValuePair<string, string>>? info = null)
    {
        var levelCount = Math.Max(perspective.LevelLabelsDe.Count, Depth(perspective.Rows));
        var levelLabels = Enumerable.Range(0, levelCount)
            .Select(index => index < perspective.LevelLabelsDe.Count ? perspective.LevelLabelsDe[index] : $"Ebene {index + 1}")
            .ToList();
        var split = Enumerable.Range(0, levelCount).Select(index => IsSplitLevel(perspective, index)).ToList();

        var columns = new List<SpendExcelColumn> { new("Ebene", SpendExcelColumnKind.Count) };
        var levelColumnStart = new int[levelCount];
        for (var level = 0; level < levelCount; level++)
        {
            levelColumnStart[level] = columns.Count;
            columns.Add(new SpendExcelColumn(levelLabels[level], SpendExcelColumnKind.Text));
            if (split[level])
                columns.Add(new SpendExcelColumn(levelLabels[level] + " Text", SpendExcelColumnKind.Text));
        }

        var firstYearColumn = columns.Count;
        foreach (var year in years)
            columns.Add(new SpendExcelColumn($"Bestellwert CHF {year.ToString(CultureInfo.InvariantCulture)}", SpendExcelColumnKind.Amount));
        columns.Add(new SpendExcelColumn("Bestellwert CHF Gesamt", SpendExcelColumnKind.Amount));

        var rows = new List<object?[]>();
        var path = new string[levelCount];
        void Walk(IReadOnlyList<PurchasingSpendCascadeNode> nodes, int depth)
        {
            foreach (var node in nodes)
            {
                path[depth] = node.Label;
                var row = new object?[columns.Count];
                row[0] = depth + 1;
                for (var level = 0; level <= depth; level++)
                {
                    var start = levelColumnStart[level];
                    if (split[level])
                    {
                        var (code, text) = SplitCodeText(path[level]);
                        row[start] = code;
                        row[start + 1] = text;
                    }
                    else
                    {
                        row[start] = path[level];
                    }
                }

                for (var i = 0; i < years.Count; i++)
                    row[firstYearColumn + i] = node.YearValues.TryGetValue(years[i], out var value) ? value : 0m;
                row[firstYearColumn + years.Count] = node.Total;
                rows.Add(row);

                if (node.Children.Count > 0 && depth + 1 < levelCount)
                    Walk(node.Children, depth + 1);
            }
        }

        Walk(perspective.Rows, 0);
        return new SpendExcelTable(perspective.LabelDe, columns, rows, info ?? []);
    }

    /// <summary>Region je Warengruppe (Kuchen): je Warengruppe und Lieferantenland eine Zeile, Anteil an der Warengruppe.</summary>
    public static SpendExcelTable BuildRegionTable(
        IReadOnlyList<PurchasingRegionPieGroup> groups,
        IReadOnlyList<KeyValuePair<string, string>>? info = null)
    {
        var columns = new List<SpendExcelColumn>
        {
            new("Warengruppe", SpendExcelColumnKind.Text),
            new("Warengruppe Text", SpendExcelColumnKind.Text),
            new("Lieferantenland", SpendExcelColumnKind.Text),
            new("Bestellwert CHF", SpendExcelColumnKind.Amount),
            new("Anteil an Warengruppe", SpendExcelColumnKind.Percent),
            new("Bestellwert Warengruppe CHF", SpendExcelColumnKind.Amount)
        };
        var rows = new List<object?[]>();
        foreach (var group in groups)
        {
            var (code, text) = SplitCodeText(group.MaterialGroup);
            foreach (var slice in group.Slices)
                rows.Add([code, text, slice.Label, slice.Value, Fraction(slice.Value, group.Total), group.Total]);
        }

        return new SpendExcelTable("Region je Warengruppe", columns, rows, info ?? []);
    }

    /// <summary>Anzahl Artikel je XYZ-Klasse.</summary>
    public static SpendExcelTable BuildXyzCountTable(
        IReadOnlyList<PurchasingLiveChartPoint> rows,
        IReadOnlyList<KeyValuePair<string, string>>? info = null)
    {
        var total = rows.Sum(row => row.Value);
        var columns = new List<SpendExcelColumn>
        {
            new("XYZ-Klasse", SpendExcelColumnKind.Text),
            new("Anzahl Artikel", SpendExcelColumnKind.Count),
            new("Anteil", SpendExcelColumnKind.Percent)
        };
        return new SpendExcelTable(
            "Anzahl Artikel nach XYZ",
            columns,
            rows.Select(row => new object?[] { row.Label, row.Value, Fraction(row.Value, total) }).ToList(),
            info ?? []);
    }

    /// <summary>ABC/XYZ-Massnahmenmatrix mit getrennten Klassen ABC und XYZ.</summary>
    public static SpendExcelTable BuildAbcXyzActionTable(
        IReadOnlyList<PurchasingAbcXyzActionRow> rows,
        IReadOnlyList<KeyValuePair<string, string>>? info = null)
    {
        var columns = new List<SpendExcelColumn>
        {
            new("Klasse", SpendExcelColumnKind.Text),
            new("ABC", SpendExcelColumnKind.Text),
            new("XYZ", SpendExcelColumnKind.Text),
            new("Spend CHF", SpendExcelColumnKind.Amount),
            new("Materialien", SpendExcelColumnKind.Count),
            new("Lieferanten", SpendExcelColumnKind.Count),
            new("Empfohlener Pruefauftrag", SpendExcelColumnKind.Text),
            new("Prioritaet", SpendExcelColumnKind.Text)
        };
        return new SpendExcelTable(
            "ABC-XYZ-Massnahmenmatrix",
            columns,
            rows.Select(row => new object?[]
            {
                row.Classification, row.Abc, row.Xyz, row.SpendChf, row.MaterialCount, row.SupplierCount, row.ActionDe,
                ActionPriorityDe(row.Severity)
            }).ToList(),
            info ?? []);
    }

    /// <summary>
    /// Materialanteil je Produktgruppe, alle drei Ebenen (Produktgruppe, Materialgruppe, Teil). Der Teile-Code kommt aus dem
    /// Knotenschluessel (Produktgruppe|Materialgruppe|Teil), nicht aus der Anzeige „Teil Text“, die nur ein Leerzeichen trennt.
    /// Die Spalte „Anteil“ bezieht sich wie in der Anzeige auf die Ebene darueber; die Basis steht in „Anteil bezogen auf“.
    /// </summary>
    public static SpendExcelTable BuildMaterialShareTable(
        IReadOnlyList<PurchasingMaterialShareNode> groups,
        IReadOnlyList<KeyValuePair<string, string>>? info = null)
    {
        var columns = new List<SpendExcelColumn>
        {
            new("Ebene", SpendExcelColumnKind.Count),
            new("Produktgruppe", SpendExcelColumnKind.Text),
            new("Materialgruppe", SpendExcelColumnKind.Text),
            new("Materialgruppe Text", SpendExcelColumnKind.Text),
            new("Material", SpendExcelColumnKind.Text),
            new("Material Text", SpendExcelColumnKind.Text),
            new("Umsatz bestaetigt CHF", SpendExcelColumnKind.Amount),
            new("Herstellkosten HK CHF", SpendExcelColumnKind.Amount),
            new("HK-Abdeckung Umsatz", SpendExcelColumnKind.Percent),
            new("Bestaetigte Menge", SpendExcelColumnKind.Count),
            new("Materialanteil CHF", SpendExcelColumnKind.Amount),
            new("Anteil", SpendExcelColumnKind.Percent),
            new("Anteil bezogen auf", SpendExcelColumnKind.Text),
            new("Bedarfsmenge", SpendExcelColumnKind.Quantity),
            new("Einheit", SpendExcelColumnKind.Text),
            new("Stueckkosten CHF", SpendExcelColumnKind.UnitCost)
        };

        var rows = new List<object?[]>();
        foreach (var group in groups)
        {
            rows.Add(
            [
                1, group.Label, null, null, null, null, group.Revenue, group.ManufacturingCost,
                group.Revenue > 0m ? group.ManufacturingCostCoverage : null, group.ConfirmedQuantity,
                group.Value, Fraction(group.Value, group.Revenue), "Umsatz", null, null, null
            ]);
            foreach (var materialGroup in group.Children)
            {
                var (mgCode, mgText) = SplitCodeText(materialGroup.Label);
                rows.Add(
                [
                    2, group.Label, mgCode, mgText, null, null, null, null, null, materialGroup.ConfirmedQuantity,
                    materialGroup.Value, Fraction(materialGroup.Value, group.Value), "Produktgruppe", null, null, null
                ]);
                foreach (var part in materialGroup.Children)
                {
                    var (partCode, partText) = SplitPart(part);
                    rows.Add(
                    [
                        3, group.Label, mgCode, mgText, partCode, partText, null, null, null, part.ConfirmedQuantity,
                        part.Value, Fraction(part.Value, materialGroup.Value), "Materialgruppe", part.Quantity,
                        part.Unit, part.UnitCost
                    ]);
                }
            }
        }

        return new SpendExcelTable("Materialanteil je Produktgruppe", columns, rows, info ?? []);
    }

    /// <summary>Arbeitsmappe: Tabellenblatt (Kopf fett und fixiert, Autofilter, Zahlenformate) und Blatt „Info“.</summary>
    public static byte[] CreateWorkbookBytes(SpendExcelTable table)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(SheetName(table.Title));
        for (var column = 0; column < table.Columns.Count; column++)
        {
            var cell = ws.Cell(1, column + 1);
            cell.Value = table.Columns[column].Header;
            cell.Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in table.Rows)
        {
            for (var column = 0; column < table.Columns.Count && column < row.Length; column++)
                SetValue(ws.Cell(rowNumber, column + 1), row[column], table.Columns[column].Kind);
            rowNumber++;
        }

        var lastRow = Math.Max(1, rowNumber - 1);
        for (var column = 0; column < table.Columns.Count; column++)
        {
            var format = FormatFor(table.Columns[column].Kind);
            if (format is not null && lastRow > 1)
                ws.Range(2, column + 1, lastRow, column + 1).Style.NumberFormat.Format = format;
        }

        ws.Range(1, 1, lastRow, Math.Max(1, table.Columns.Count)).SetAutoFilter();
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, Math.Max(1, table.Columns.Count)).AdjustToContents(1, Math.Min(lastRow, AdjustRowLimit));
        for (var column = 1; column <= table.Columns.Count; column++)
        {
            var width = ws.Column(column).Width;
            ws.Column(column).Width = Math.Clamp(width + 2, 8, 60);
        }

        var info = workbook.Worksheets.Add("Info");
        info.Cell(1, 1).Value = "Spend-Aufriss: " + table.Title;
        info.Cell(1, 1).Style.Font.Bold = true;
        var infoRow = 3;
        foreach (var entry in table.Info)
        {
            info.Cell(infoRow, 1).Value = entry.Key;
            info.Cell(infoRow, 1).Style.Font.Bold = true;
            info.Cell(infoRow, 2).Value = entry.Value;
            infoRow++;
        }

        info.Column(1).Width = 28;
        info.Column(2).Width = 120;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Angaben fuer das Blatt „Info“: was exportiert wurde, mit welchem Umfang und Zeitraum.</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> BuildInfo(
        string aufriss,
        string scope,
        string? period,
        IReadOnlyList<int>? years,
        DateTime createdAt,
        string? levels = null,
        string? note = null)
    {
        var info = new List<KeyValuePair<string, string>> { new("Aufriss", aufriss) };
        if (!string.IsNullOrWhiteSpace(levels))
            info.Add(new("Ebenen", levels));
        info.Add(new("Umfang", scope));
        if (!string.IsNullOrWhiteSpace(period))
            info.Add(new("Zeitraum", period));
        if (years is { Count: > 0 })
            info.Add(new("Jahre", string.Join(", ", years.Select(year => year.ToString(CultureInfo.InvariantCulture)))));
        info.Add(new("Erstellt", createdAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)));
        if (!string.IsNullOrWhiteSpace(note))
            info.Add(new("Hinweis", note));
        return info;
    }

    public const string ScopePurchasingParts = "Nur Einkaufsteile (Disponent 001 bis 006)";
    public const string ScopeAllPurchasing = "Ganzer Einkauf";
    public const string ScopeAllPurchasingFixed = "Ganzer Einkauf (der Schalter Nur Einkaufsteile wirkt auf diese Tabelle nicht)";
    public const string RegionFileLabel = "Region je Warengruppe";
    public const string XyzFileLabel = "Anzahl Artikel nach XYZ";
    public const string AbcXyzFileLabel = "ABC-XYZ-Massnahmenmatrix";
    public const string MaterialShareFileLabel = "Materialanteil je Produktgruppe";
    public const string RegionNote =
        "Wie die Kuchen auf der Seite: die 6 groessten Warengruppen, je Warengruppe die 8 groessten Lieferantenlaender, der Rest als uebrige (n). " +
        "Land = LFA1-LAND1 des Lieferanten, Werte in CHF.";
    public const string MaterialShareNote =
        "Materialanteil = bestaetigte Menge (ZC23) mal Stuecklistenmenge (ZLO03 Top-Down) mal Stueckkosten, nur Einkaufsteile Disponent 001 bis 006. " +
        "Nicht das Einkaufsvolumen der anderen Einstiege. Die Spalte Anteil bezieht sich auf die Ebene darueber (Spalte Anteil bezogen auf).";

    /// <summary>Kaskade des gewaehlten Einstiegs samt Info-Blatt, so wie die Seite ihn gerade zeigt (Schalter, Zeitraum).</summary>
    public static SpendExcelTable BuildCascadeExport(
        PurchasingSpendPerspectiveResult perspective,
        IReadOnlyList<int> years,
        bool onlyPurchasingParts,
        string? period,
        DateTime createdAt)
        => BuildCascadeTable(perspective, years, BuildInfo(
            perspective.LabelDe,
            onlyPurchasingParts ? ScopePurchasingParts : ScopeAllPurchasing,
            period,
            years,
            createdAt,
            string.Join(" > ", perspective.LevelLabelsDe),
            LevelSumNote + " Werte in CHF zum Belegkurs (EKKO.WAERS/WKURS), Bestellwert EKPO.NETWR, Zeitraum nach EKKO.BEDAT."));

    /// <summary>Materialanteil samt Info-Blatt (Monatsbereich der Auswahl und Datenstand des Einkauf-Laufs).</summary>
    public static SpendExcelTable BuildMaterialShareExport(
        IReadOnlyList<PurchasingMaterialShareNode> rows,
        string fromMonth,
        string toMonth,
        string loadedAt,
        DateTime createdAt)
        => BuildMaterialShareTable(rows, BuildInfo(
            MaterialShareFileLabel,
            ScopePurchasingParts,
            $"Termin bestaetigt {fromMonth} bis {toMonth}" + (string.IsNullOrWhiteSpace(loadedAt) ? string.Empty : $", Datenstand {loadedAt}"),
            null,
            createdAt,
            "Produktgruppe > Materialgruppe > Material",
            MaterialShareNote));

    /// <summary>Hinweis fuer Exporte mit Spalte „Ebene“: jede Ebene enthaelt die ganze Summe.</summary>
    public const string LevelSumNote =
        "Jede Zeile ist ein Knoten des Aufrisses; die Spalte Ebene sagt, auf welcher Stufe. Die Summe der Ebene 1 ist der Gesamtwert, " +
        "jede tiefere Ebene ergibt dieselbe Summe noch einmal. Zum Summieren nach Ebene filtern.";

    public static string ActionPriorityDe(string severity)
        => severity switch
        {
            "High" => "hoch",
            "Medium" => "mittel",
            "Warning" => "Datenpflege",
            _ => "optimieren"
        };

    private static void SetValue(IXLCell cell, object? value, SpendExcelColumnKind kind)
    {
        switch (value)
        {
            case null:
                return;
            case string text:
                // Nummern als Text: fuehrende Nullen (Disponent 001, Material 064776) bleiben erhalten.
                if (text.Length > 0)
                    cell.Value = text;
                return;
            case decimal number:
                cell.Value = number;
                return;
            case int number:
                cell.Value = number;
                return;
            case long number:
                cell.Value = number;
                return;
            case double number:
                cell.Value = number;
                return;
            default:
                cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return;
        }
    }

    private static string? FormatFor(SpendExcelColumnKind kind)
        => kind switch
        {
            SpendExcelColumnKind.Amount => AmountFormat,
            SpendExcelColumnKind.Percent => PercentFormat,
            SpendExcelColumnKind.Count => CountFormat,
            SpendExcelColumnKind.Quantity => QuantityFormat,
            SpendExcelColumnKind.UnitCost => UnitCostFormat,
            _ => null
        };

    private static bool IsSplitLevel(PurchasingSpendPerspectiveResult perspective, int index)
    {
        if (perspective.LevelKeys is { } keys)
            return index < keys.Count && keys[index] is LevelKeyMaterialGroup or LevelKeyArticle;

        // Rueckfall fuer Ergebnisse ohne Ebenenschluessel (aeltere Aufrufer).
        return index < perspective.LevelLabelsDe.Count && perspective.LevelLabelsDe[index] is "Warengruppe" or "Material";
    }

    private static (string Code, string Text) SplitPart(PurchasingMaterialShareNode part)
    {
        var separator = part.Key.LastIndexOf('|');
        var code = separator >= 0 ? part.Key[(separator + 1)..] : part.Key;
        if (code.Length == 0)
            return SplitCodeText(part.Label);
        var label = part.Label;
        if (label == code)
            return (code, string.Empty);
        return label.StartsWith(code + " ", StringComparison.Ordinal)
            ? (code, label[(code.Length + 1)..].Trim())
            : (code, label);
    }

    private static int Depth(IReadOnlyList<PurchasingSpendCascadeNode> nodes)
        => nodes.Count == 0 ? 0 : 1 + nodes.Max(node => Depth(node.Children));

    private static decimal? Fraction(decimal value, decimal total)
        => total <= 0m ? null : Math.Round(value / total, 6);

    private static string CollapseSpaces(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch == ' ' && builder.Length > 0 && builder[^1] == ' ')
                continue;
            builder.Append(ch);
        }

        return builder.ToString();
    }

}
