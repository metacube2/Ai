// Baut das betriebswirtschaftliche Anwenderhandbuch (2026-10-06) aus den Kapitel-Markdowns in docs/handbuch/kapitel.
// Aufruf aus dem Projektordner: dotnet run --project Tools/HandbuchDocx -- <kapitelordner> <ausgabe.docx> <logo.jpg>
// Unterstuetzt nur die Elemente aus docs/handbuch/VORGABE.md: #, ##, ###, Absatz, "- " Aufzaehlung, **fett**, |Tabelle|.
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

var dir = args.Length > 0 ? args[0] : "docs/handbuch/kapitel";
var output = args.Length > 1 ? args[1] : "docs/handbuch/Trafag_Cockpit_Handbuch.docx";
// Trafag CI wie im Cockpit (ARCHITECTURE.md, Skin seit 2026-10-01): Orange #C8501E, dunkel #B5481B, Schrift Open Sans.
const string Red = "C8501E";
const string Dark = "B5481B";
var logoPath = args.Length > 2 ? args[2] : "wwwroot/trafag.jpg";
const string Grey = "595959";

var files = Directory.GetFiles(dir, "*.md").Where(f => Regex.IsMatch(Path.GetFileName(f), @"^\d\d_")).Order().ToList();
using var doc = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document);
var main = doc.AddMainDocumentPart();
main.Document = new Document(new Body());
var body = main.Document.Body!;
AddStyles(main);
AddNumbering(main);
var settings = main.AddNewPart<DocumentSettingsPart>();
settings.Settings = new Settings(new UpdateFieldsOnOpen { Val = true }, new DefaultTabStop { Val = 708 });
var footerId = AddFooter(main);
var logoId = AddLogo(main, logoPath);
var headerId = AddHeader(main, logoId);

// Titelseite
body.Append(Spacer(1600));
body.Append(new Paragraph(new Run(Logo(logoId, 4.6, 1, "Trafag Logo"))));
body.Append(Spacer(600));
body.Append(P("Trafag Cockpit", "Title"));
body.Append(P("Anwenderhandbuch aus betriebswirtschaftlicher Sicht", "Subtitle"));
body.Append(P("Was jede Seite und jede Grafik zeigt, warum es sie gibt, wie die Zahlen entstehen und wo man sie in der täglichen Arbeit als Controller, Einkäufer, Verkäufer, Logistiker, HR oder Geschäftsleitung einsetzt.", null));
body.Append(Spacer(1200));
body.Append(P($"Stand der Software: Commit 3ad0dae vom 5. Oktober 2026 · Ausgabe {DateTime.Today:dd.MM.yyyy}", "Meta"));
body.Append(P("Herausgeber: Ingo Kohler, SAP Specialist, Trafag AG", "Meta"));
body.Append(PageBreak());

// Inhaltsverzeichnis (Word aktualisiert das Feld beim Oeffnen)
body.Append(P("Inhalt", "TocTitle"));
body.Append(new Paragraph(
    new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
    new Run(new FieldCode(" TOC \\o \"1-2\" \\h \\z \\u ") { Space = SpaceProcessingModeValues.Preserve }),
    new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
    new Run(new Text("Das Inhaltsverzeichnis wird beim Öffnen aktualisiert (sonst Rechtsklick → Feld aktualisieren).")),
    new Run(new FieldChar { FieldCharType = FieldCharValues.End })));
body.Append(PageBreak());

// 00_einleitung.md sortiert von selbst zuerst.
var all = files;

var first = true;
foreach (var file in all)
{
    if (!first) body.Append(PageBreak());
    first = false;
    var lines = File.ReadAllLines(file);
    for (var i = 0; i < lines.Length; i++)
    {
        var line = lines[i].TrimEnd();
        if (line.Length == 0) continue;
        if (line.StartsWith("# ")) { body.Append(P(line[2..], "Heading1")); continue; }
        if (line.StartsWith("## ")) { body.Append(P(line[3..], "Heading2")); continue; }
        if (line.StartsWith("### ")) { body.Append(P(line[4..], "Heading3")); continue; }
        if (line.StartsWith("- ") || line.StartsWith("* "))
        {
            body.Append(Bullet(line[2..]));
            continue;
        }
        if (line.StartsWith("|"))
        {
            var rows = new List<string>();
            while (i < lines.Length && lines[i].TrimStart().StartsWith("|")) rows.Add(lines[i++].Trim());
            i--;
            body.Append(Table(rows));
            body.Append(Spacer(80));
            continue;
        }
        // Absatz: Folgezeilen bis Leerzeile zusammenziehen
        var text = line;
        while (i + 1 < lines.Length && lines[i + 1].Trim().Length > 0 && !Regex.IsMatch(lines[i + 1], @"^(#|- |\* |\|)"))
            text += " " + lines[++i].Trim();
        body.Append(P(text, null));
    }
}

body.Append(new SectionProperties(
    new HeaderReference { Type = HeaderFooterValues.Default, Id = headerId },
    new FooterReference { Type = HeaderFooterValues.Default, Id = footerId },
    new TitlePage(),
    new PageSize { Width = 11906, Height = 16838 },
    new PageMargin { Top = 1300, Bottom = 1300, Left = 1300, Right = 1300, Header = 600, Footer = 600 }));
main.Document.Save();
Console.WriteLine($"{output}: {all.Count} Dateien");

static Paragraph P(string text, string? style)
{
    var p = new Paragraph();
    if (style != null) p.Append(new ParagraphProperties(new ParagraphStyleId { Val = style }));
    foreach (var r in Runs(text)) p.Append(r);
    return p;
}

static IEnumerable<Run> Runs(string text)
{
    var parts = Regex.Split(text, @"(\*\*[^*]+\*\*)");
    foreach (var part in parts)
    {
        if (part.Length == 0) continue;
        var bold = part.StartsWith("**") && part.EndsWith("**") && part.Length > 4;
        var t = bold ? part[2..^2] : part;
        t = t.Replace("`", "");
        var run = new Run(new Text(t) { Space = SpaceProcessingModeValues.Preserve });
        if (bold) run.PrependChild(new RunProperties(new Bold()));
        yield return run;
    }
}

static Paragraph Bullet(string text)
{
    var p = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "ListBullet" },
        new NumberingProperties(new NumberingLevelReference { Val = 0 }, new NumberingId { Val = 1 })));
    foreach (var r in Runs(text)) p.Append(r);
    return p;
}

static Paragraph Spacer(int after) => new(new ParagraphProperties(new SpacingBetweenLines { After = after.ToString(), Before = "0" }));

static Paragraph PageBreak() => new(new Run(new Break { Type = BreakValues.Page }));

static Table Table(List<string> rows)
{
    static string[] Cells(string row) => row.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
    var data = rows.Where(r => !Regex.IsMatch(r, @"^\|[\s:\-|]+\|?$")).Select(Cells).ToList();
    var cols = data.Max(r => r.Length);
    var width = 9306 / cols;
    var table = new Table(new TableProperties(
        new TableStyle { Val = "Gitter" },
        new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
        new TableLayout { Type = TableLayoutValues.Autofit }));
    table.Append(new TableGrid(Enumerable.Range(0, cols).Select(_ => new GridColumn { Width = width.ToString() })));
    for (var r = 0; r < data.Count; r++)
    {
        var tr = new TableRow();
        if (r == 0) tr.Append(new TableRowProperties(new TableHeader()));
        for (var c = 0; c < cols; c++)
        {
            var text = c < data[r].Length ? data[r][c] : "";
            var p = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "TableText" }));
            foreach (var run in Runs(r == 0 ? $"**{text.Trim('*')}**" : text)) p.Append(run);
            var tcp = new TableCellProperties(new TableCellWidth { Width = width.ToString(), Type = TableWidthUnitValues.Dxa });
            if (r == 0) tcp.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = "F9E3D9", Color = "auto" });
            tr.Append(new TableCell(tcp, p));
        }
        table.Append(tr);
    }
    return table;
}

static void AddStyles(MainDocumentPart main)
{
    var part = main.AddNewPart<StyleDefinitionsPart>();
    Style Para(string id, string name, int size, bool bold = false, string? color = null, int before = 0, int after = 120, bool keepNext = false, int? outline = null, string? basedOn = "Normal")
    {
        var rp = new StyleRunProperties();
        if (bold) rp.Append(new Bold());
        if (color != null) rp.Append(new Color { Val = color });
        rp.Append(new FontSize { Val = (size * 2).ToString() });
        var pp = new StyleParagraphProperties();
        if (keepNext) pp.Append(new KeepNext());
        pp.Append(new SpacingBetweenLines { Before = before.ToString(), After = after.ToString() });
        if (outline != null) pp.Append(new OutlineLevel { Val = outline.Value });
        var s = new Style(new StyleName { Val = name }, new PrimaryStyle()) { Type = StyleValues.Paragraph, StyleId = id };
        if (basedOn != null) s.Append(new BasedOn { Val = basedOn });
        s.Append(pp, rp);
        return s;
    }
    var normal = new Style(new StyleName { Val = "Normal" }, new PrimaryStyle(),
        new StyleParagraphProperties(new SpacingBetweenLines { After = "120", Line = "276", LineRule = LineSpacingRuleValues.Auto }),
        new StyleRunProperties(new RunFonts { Ascii = "Open Sans", HighAnsi = "Open Sans", ComplexScript = "Arial", EastAsia = "Open Sans" }, new FontSize { Val = "21" }))
    { Type = StyleValues.Paragraph, StyleId = "Normal", Default = true };
    var styles = new Styles(normal,
        Para("Title", "Title", 30, true, Red, 0, 200),
        Para("Subtitle", "Subtitle", 16, false, Grey, 0, 300),
        Para("Meta", "Meta", 10, false, Grey, 0, 60),
        Para("TocTitle", "TOC Heading", 18, true, Red, 0, 240),
        WithBottomRule(Para("Heading1", "heading 1", 20, true, Red, 0, 240, true, 0)),
        Para("Heading2", "heading 2", 14, true, Dark, 360, 120, true, 1),
        Para("Heading3", "heading 3", 11, true, Red, 200, 60, true, 2),
        Para("ListBullet", "List Bullet", 10, after: 60),
        Para("TableText", "Table Text", 9, after: 0),
        Para("TOC1", "toc 1", 11, true, after: 60),
        Para("TOC2", "toc 2", 10, after: 20));
    styles.Append(new Style(new StyleName { Val = "Gitter" },
        new StyleTableProperties(new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
            new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
            new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
            new RightBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" }),
            new TableCellMarginDefault(new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                new TableCellLeftMargin { Width = 80, Type = TableWidthValues.Dxa },
                new TableCellRightMargin { Width = 80, Type = TableWidthValues.Dxa })))
    { Type = StyleValues.Table, StyleId = "Gitter" });
    // TOC2 einruecken
    styles.Elements<Style>().First(s => s.StyleId == "TOC2").StyleParagraphProperties!.Append(new Indentation { Left = "300" });
    part.Styles = styles;
}

static Style WithBottomRule(Style s)
{
    s.StyleParagraphProperties!.Append(new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 8, Color = "C8501E", Space = 6 }));
    return s;
}

static void AddNumbering(MainDocumentPart main)
{
    var part = main.AddNewPart<NumberingDefinitionsPart>();
    part.Numbering = new Numbering(
        new AbstractNum(new Level(
            new NumberingFormat { Val = NumberFormatValues.Bullet },
            new LevelText { Val = "•" },
            new LevelJustification { Val = LevelJustificationValues.Left },
            new PreviousParagraphProperties(new Indentation { Left = "360", Hanging = "240" }),
            new NumberingSymbolRunProperties(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" }, new Color { Val = Red }))
        { LevelIndex = 0 }) { AbstractNumberId = 1 },
        new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = 1 });
}

static string AddFooter(MainDocumentPart main)
{
    var part = main.AddNewPart<FooterPart>();
    part.Footer = new Footer(new Paragraph(
        new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
        new Run(new RunProperties(new Color { Val = Grey }, new FontSize { Val = "16" }), new Text("Trafag Cockpit · Anwenderhandbuch · Seite ") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new RunProperties(new Color { Val = Grey }, new FontSize { Val = "16" }), new FieldChar { FieldCharType = FieldCharValues.Begin }),
        new Run(new RunProperties(new Color { Val = Grey }, new FontSize { Val = "16" }), new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new RunProperties(new Color { Val = Grey }, new FontSize { Val = "16" }), new FieldChar { FieldCharType = FieldCharValues.Separate }),
        new Run(new RunProperties(new Color { Val = Grey }, new FontSize { Val = "16" }), new Text("1")),
        new Run(new RunProperties(new Color { Val = Grey }, new FontSize { Val = "16" }), new FieldChar { FieldCharType = FieldCharValues.End })));
    return main.GetIdOfPart(part);
}

static string AddLogo(MainDocumentPart main, string path)
{
    var part = main.AddImagePart(ImagePartType.Jpeg);
    using (var s = File.OpenRead(path)) part.FeedData(s);
    return main.GetIdOfPart(part);
}

// Logo inline, Breite in cm; Seitenverhaeltnis des trafag.jpg (200 x 97).
static Drawing Logo(string relId, double widthCm, uint id, string name)
{
    const long EmuPerCm = 360000;
    var cx = (long)(widthCm * EmuPerCm);
    var cy = cx * 97 / 200;
    return new Drawing(new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = cx, Cy = cy },
        new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = id, Name = name },
        new DocumentFormat.OpenXml.Drawing.Graphic(new DocumentFormat.OpenXml.Drawing.GraphicData(
            new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties { Id = id, Name = name },
                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                    new DocumentFormat.OpenXml.Drawing.Blip { Embed = relId },
                    new DocumentFormat.OpenXml.Drawing.Stretch(new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                    new DocumentFormat.OpenXml.Drawing.Transform2D(
                        new DocumentFormat.OpenXml.Drawing.Offset { X = 0, Y = 0 },
                        new DocumentFormat.OpenXml.Drawing.Extents { Cx = cx, Cy = cy }),
                    new DocumentFormat.OpenXml.Drawing.PresetGeometry { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle })))
        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
    { DistanceFromTop = 0, DistanceFromBottom = 0, DistanceFromLeft = 0, DistanceFromRight = 0 });
}

// Kopfzeile: Logo links, Titel rechts, orange Linie darunter (ab Seite 2, Titelseite ohne Kopf).
static string AddHeader(MainDocumentPart main, string logoRelId)
{
    var part = main.AddNewPart<HeaderPart>();
    // Das Bild muss auch im Header-Teil referenziert sein.
    var img = part.AddImagePart(ImagePartType.Jpeg);
    using (var s = ((ImagePart)main.GetPartById(logoRelId)).GetStream()) img.FeedData(s);
    var rid = part.GetIdOfPart(img);
    part.Header = new Header(new Paragraph(
        new ParagraphProperties(
            new Tabs(new TabStop { Val = TabStopValues.Right, Position = 9306 }),
            new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 12, Color = "C8501E", Space = 4 })),
        new Run(Logo(rid, 2.0, 10, "Trafag Logo Kopf")),
        new Run(new TabChar()),
        new Run(new RunProperties(new Color { Val = "C8501E" }, new FontSize { Val = "16" }), new Text("Trafag Cockpit · Anwenderhandbuch"))));
    return main.GetIdOfPart(part);
}
