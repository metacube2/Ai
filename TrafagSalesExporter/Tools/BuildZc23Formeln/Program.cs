using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using W = DocumentFormat.OpenXml.Wordprocessing;

// Erzeugt die Beschreibung der neuen ZC23-Felder (Materialanteil, Arbeit CZ) mit Formeln, Beispiel und Hinweisen.
// Quelle: saptasks/zc23_materialanteil/NOTES.md und der Quelltext saptasks/zm_offene_kdauf/teile/opt_mata_*.abap, 15.10.2026.
// Aufruf aus der Repository-Wurzel: dotnet run --project .tmp_tools/BuildZc23Formeln -- docs/ZC23_Materialanteil_Formeln_2026-10-15.docx

var outputPath = Path.GetFullPath(args.Length > 0 ? args[0] : "docs/ZC23_Materialanteil_Formeln_2026-10-15.docx");
var temporaryPath = Path.Combine(Path.GetDirectoryName(outputPath)!, Path.GetFileNameWithoutExtension(outputPath) + ".new.docx");
if (File.Exists(temporaryPath))
    File.Delete(temporaryPath);

var pflichtTexte = new List<string>();
Paragraph P(string text, string style = "BodyText") { pflichtTexte.Add(text); return Para(text, style); }

using (var document = WordprocessingDocument.Create(temporaryPath, WordprocessingDocumentType.Document))
{
    var mainPart = document.AddMainDocumentPart();
    mainPart.Document = new Document();
    var body = mainPart.Document.AppendChild(new Body());
    AddStyles(mainPart);
    AddNumbering(mainPart);
    var headerId = AddHeader(mainPart);
    var footerId = AddFooter(mainPart);

    body.Append(P("ZC23: Materialanteil und Arbeit CZ", "Title"));
    body.Append(P("Formeln hinter den neuen Feldern der offenen Kundenaufträge", "Subtitle"));
    body.Append(P("Ingo Kohler · SAP · Stand 15.10.2026 · Transport T76K912736 (Import P76 offen)", "Meta"));

    body.Append(P(
        "ZC23 (Programm ZM_OFFENE_KDAUF) zeigt je Auftragsposition den Standardwert, also die Herstellkosten zum "
        + "Standardpreis. Neu zeigt ZC23 daneben, welcher Teil davon eingekauftes Material ist und welcher Teil "
        + "Arbeit an tschechischen Arbeitsplätzen. Die Rechnung folgt der Preisliste Tschechien (Transaktion ZM03, "
        + "Programm ZLO_CALC_CZ_PREISE) und liest die Standardkalkulation des Materials (wie CK11N/CK13N).", "Intro"));

    body.Append(H1("1. Die Felder auf einen Blick"));
    body.Append(GridTable(
        ["Spalte in ZC23", "Technisch", "Formel je Auftragsposition"],
        [
            ["Standardpreis (bisher)", "STANDARDPREIS", "MBEW-STPRS des Materials im Werk"],
            ["Standardwert (bisher)", "STANDARDWERT", "Standardpreis × bestätigte Menge (BMENG)"],
            ["Materialanteil je Einheit CHF", "MATPREIS", "M(K) = Material je Basiseinheit der Kalkulation K"],
            ["Materialanteil CHF", "MATANTEIL", "M(K) × BMENG"],
            ["Arbeit CZ CHF", "ARBEIT_CZ", "C(K) × BMENG, C(K) = Arbeit an CZ-Arbeitsplätzen je Basiseinheit"],
        ],
        [1500, 1100, 2400]));
    body.Append(P(
        "Die drei neuen Spalten stehen direkt hinter „Standardwert“. Materialanteil und Arbeit CZ haben eine Summenzeile. "
        + "Die Währung ist die Hauswährung des gewählten Werks, bei Werk 1100 CHF."));

    body.Append(H1("2. Welche Kalkulation K verwendet wird"));
    body.Append(P("Standardkalkulation des Materials im Werk: Kalkulationsart 01, Version 01, nicht gelöscht. Im Selektionsbild, Block „Materialanteil“:"));
    body.Append(GridTable(
        ["Auswahl", "Welche Kalkulation"],
        [
            ["Freigegebene Standardkalkulation (Vorgabe)", "jüngste mit Freigabe (KEKO-FREIG = X) und Gültig-ab (KADAT) bis heute. Gehört zum Standardpreis, Standardwert und Materialanteil passen zusammen."],
            ["Jüngste Standardkalkulation (wie ZM03)", "jüngste mit Kalkulationsdatum (KADKY) bis heute und ohne Fehlerstatus KF"],
        ],
        [1800, 3200]));

    body.Append(H1("3. Materialanteil je Einheit M(K)"));
    body.Append(P("Summe der Positionswerte der Kalkulation (CKIS, Wert WRTFW_KPF für die Kalkulationslosgrösse), geteilt durch die Losgrösse:"));
    body.Append(Code(
        "M(K) = ( Σ Material + Σ Fremdleistung* ) / Losgrösse(K)"));
    body.Append(GridTable(
        ["Position in der Kalkulation", "Zählt als"],
        [
            ["Material (Typ M/I), Zukaufteil (Beschaffungsart F)", "Material, voller Positionswert"],
            ["Material ohne eigene Unterkalkulation", "Material, voller Positionswert"],
            ["Material aus einem anderen Werk", "Material, voller Positionswert"],
            ["Material aus Eigenfertigung (Halbfabrikat)", "wird aufgelöst: M(Unterkalkulation) × Positionsmenge, über alle Stufen"],
            ["Fremdleistung (Typ F)", "nur mit Schalter „Fremdleistungen zum Material zählen“"],
            ["Eigenleistung (Typ E), Zuschläge, übrige", "nie Material"],
        ],
        [2500, 2500]));
    body.Append(P(
        "Durch die Auflösung der Halbfabrikate zählt nur eingekauftes Material. Die Arbeit, die in einem selbst "
        + "gefertigten Halbfabrikat steckt, landet nicht im Materialanteil."));

    body.Append(H1("4. Arbeit CZ je Einheit C(K)"));
    body.Append(P("Dieselbe Auflösung über alle Stufen, aber nur Eigenleistung an tschechischen Arbeitsplätzen:"));
    body.Append(Code(
        "C(K) = ( Σ Eigenleistung an CZ-Arbeitsplätzen + Σ C(Unterkalkulation) × Positionsmenge ) / Losgrösse(K)"));
    body.Append(P(
        "CZ-Arbeitsplatz heisst: Arbeitsplatzname beginnt mit „CZ“ (zum Beispiel CZCDA00, CZABM00), wie in ZM03. "
        + "Es gibt in SAP kein eigenes tschechisches Werk; die tschechische Fertigung läuft im Werk 1100 über diese "
        + "Arbeitsplätze und wird deshalb in CHF bewertet. Gemeinkosten-Zuschläge zählen wie in ZM03 nicht dazu."));

    body.Append(H1("5. Beispiel B87813 (Membrane)"));
    body.Append(P("Kalkulationslosgrösse 1'200 Stück, Auftragsposition über 1'200 Stück, Standardpreis 1.25 CHF:"));
    body.Append(GridTable(
        ["Grösse", "Rechnung", "Ergebnis CHF"],
        [
            ["Standardwert", "1.25 × 1'200", "1'500.00"],
            ["Materialanteil je Einheit", "133.78 / 1'200", "0.1115"],
            ["Materialanteil", "0.1115 × 1'200", "133.78"],
            ["Arbeit CZ", "1'355.18 / 1'200 × 1'200", "1'355.18"],
            ["Rest: Arbeit Schweiz und Gemeinkosten", "1'500.00 − 133.78 − 1'355.18", "11.04"],
        ],
        [1800, 2000, 1200]));
    body.Append(P("ZM03 zeigt für B87813 denselben Stand: Arbeit CZ 1.13 und Material 0.11 je Stück."));

    body.Append(H1("6. Bedienung"));
    body.Append(Bullet(Track("Block „Materialanteil“ im Selektionsbild: „Materialanteil rechnen“ (Vorgabe ein), Wahl der Kalkulation, Ankreuzfeld Fremdleistungen.")));
    body.Append(Bullet(Track("Schalter aus: die drei Spalten bleiben leer, ZC23 läuft so schnell wie vorher.")));
    body.Append(Bullet(Track("Gespeicherte Layouts zeigen neue Spalten nicht automatisch. Einmal „Layout ändern“, die Spalten aus dem Spaltenvorrat einblenden und sichern.")));
    body.Append(Bullet(Track("Gespeicherte Varianten (zum Beispiel „BI UPDATE“) rechnen mit der Vorgabe, also mit Materialanteil.")));
    body.Append(Bullet(Track("Für Power BI stehen die Werte in ZPOWERBI_KDAUF und im OData-Dienst ZPOWERBI_EINKAUF_SRV, Entität ZPOWERBI_KDAUFSet: Standardwert, Matanteil, ArbeitCz.")));

    body.Append(H1("7. Grenzen der Rechnung"));
    body.Append(Bullet(Track("Positionsmengen werden nicht in die Mengeneinheit der Unterkalkulation umgerechnet (wie ZM03).")));
    body.Append(Bullet(Track("Der Standardwert nutzt den Standardpreis ohne Preiseinheit (unverändert seit jeher); bei den geprüften Materialien ist die Preiseinheit 1.")));
    body.Append(Bullet(Track("Ein Schwesterwerk am selben Standort zählt als „anderes Werk“ und damit als Material; ZM03 vergleicht Standorte.")));
    body.Append(Bullet(Track("Arbeit in der Schweiz und Gemeinkosten werden nicht einzeln ausgewiesen, sie stecken im Rest.")));

    body.Append(H1("8. Prüfung (T76, 15.10.2026)"));
    body.Append(GridTable(
        ["Prüfung", "Ergebnis"],
        [
            ["Vergleich mit dem bisherigen Programm, Q2 2026, Werk 1100", "4'392 Zeilen, alle bisherigen Felder identisch"],
            ["ZM03 B80004 (Stahlmembrane)", "Material 0.12 je Stück, wie ZC23"],
            ["ZM03 B87813", "Arbeit CZ 1.13 und Material 0.11 je Stück, wie ZC23"],
            ["Unabhängige Nachrechnung aus KEKO/CKIS/MARC", "44640, 42211, E11221, B87813: auf den Rappen gleich"],
            ["Laufzeit Q2 2026 im Dialog", "15.9 s mit Materialanteil und Fortschreibung, 7.5 s ohne"],
        ],
        [2600, 2400]));

    body.Append(new SectionProperties(
        new HeaderReference { Type = HeaderFooterValues.Default, Id = headerId },
        new FooterReference { Type = HeaderFooterValues.Default, Id = footerId },
        new PageSize { Width = 11906, Height = 16838 },
        new PageMargin { Top = 1134, Right = 1134, Bottom = 1134, Left = 1134, Header = 567, Footer = 567, Gutter = 0 }));
    mainPart.Document.Save();
}

using (var verification = WordprocessingDocument.Open(temporaryPath, false))
{
    var text = verification.MainDocumentPart!.Document.Body!.InnerText;
    foreach (var pflicht in pflichtTexte)
        if (!text.Contains(pflicht, StringComparison.Ordinal))
            throw new InvalidOperationException($"Word-Validierung: Text fehlt: {pflicht[..Math.Min(40, pflicht.Length)]}");
    var errors = new OpenXmlValidator(FileFormatVersions.Office2021).Validate(verification).ToList();
    if (errors.Count > 0)
        throw new InvalidOperationException("OpenXML-Validierung: " + string.Join(" | ", errors.Take(5).Select(e => $"{e.Description} @ {e.Path?.XPath}")));
}

File.Move(temporaryPath, outputPath, true);
Console.WriteLine($"Word-Dokument erstellt: {outputPath} ({pflichtTexte.Count} Pflichttexte geprüft)");
return 0;

string Track(string text) { pflichtTexte.Add(text); return text; }

// ------------------------------------------------------------------ Formatierung (wie BuildChefUebersicht0902)

static void AddStyles(MainDocumentPart mainPart)
{
    var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
    var styles = new Styles();
    styles.Append(new DocDefaults(
        new RunPropertiesDefault(new RunPropertiesBaseStyle(
            new RunFonts { Ascii = "Aptos", HighAnsi = "Aptos", EastAsia = "Aptos" },
            new FontSize { Val = "22" },
            new Languages { Val = "de-CH" })),
        new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
            new SpacingBetweenLines { After = "120", Line = "276", LineRule = LineSpacingRuleValues.Auto }))));
    styles.Append(Style("Normal", "Normal", 22, "202020"));
    styles.Append(Style("Title", "Titel", 40, "17365D", bold: true, after: 80));
    styles.Append(Style("Subtitle", "Untertitel", 25, "5B6573", italic: true, after: 60));
    styles.Append(Style("Intro", "Einleitung", 22, "303030", before: 200, after: 220));
    styles.Append(Style("Heading1", "Überschrift 1", 30, "17365D", bold: true, before: 320, after: 140, keepNext: true));
    styles.Append(Style("Meta", "Metadaten", 20, "303030", after: 0));
    styles.Append(Style("BodyText", "Textkörper", 22, "202020", after: 120));
    var code = Style("Code", "Formel", 21, "17365D", before: 80, after: 160);
    code.StyleRunProperties!.RunFonts = new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas", EastAsia = "Consolas" };
    styles.Append(code);
    stylesPart.Styles = styles;
}

static W.Style Style(string id, string name, int size, string color, bool bold = false,
    bool italic = false, int before = 0, int after = 120, bool keepNext = false)
{
    var runProperties = new StyleRunProperties(new RunFonts { Ascii = "Aptos", HighAnsi = "Aptos", EastAsia = "Aptos" });
    if (bold) runProperties.Append(new Bold());
    if (italic) runProperties.Append(new Italic());
    runProperties.Append(new Color { Val = color });
    runProperties.Append(new FontSize { Val = size.ToString() });
    var paragraphProperties = new StyleParagraphProperties();
    if (keepNext) paragraphProperties.Append(new KeepNext());
    paragraphProperties.Append(new SpacingBetweenLines
    { Before = before.ToString(), After = after.ToString(), Line = "276", LineRule = LineSpacingRuleValues.Auto });
    return new W.Style(new StyleName { Val = name }, new BasedOn { Val = "Normal" }, new NextParagraphStyle { Val = "Normal" },
        paragraphProperties, runProperties)
    { Type = StyleValues.Paragraph, StyleId = id, CustomStyle = true };
}

static void AddNumbering(MainDocumentPart mainPart)
{
    var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
    numberingPart.Numbering = new Numbering(
        new AbstractNum(
            new Level(
                new NumberingFormat { Val = NumberFormatValues.Bullet },
                new LevelText { Val = "•" },
                new LevelJustification { Val = LevelJustificationValues.Left },
                new PreviousParagraphProperties(new Indentation { Left = "540", Hanging = "260" }),
                new NumberingSymbolRunProperties(new RunFonts { Ascii = "Aptos", HighAnsi = "Aptos" }))
            { LevelIndex = 0 })
        { AbstractNumberId = 1 },
        new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = 1 });
}

static string AddHeader(MainDocumentPart mainPart)
{
    var part = mainPart.AddNewPart<HeaderPart>();
    part.Header = new Header(Para("Trafag · SAP ZC23 · Materialanteil und Arbeit CZ · Stand 15.10.2026", "Meta", "6F7782", 18));
    part.Header.Save();
    return mainPart.GetIdOfPart(part);
}

static string AddFooter(MainDocumentPart mainPart)
{
    var part = mainPart.AddNewPart<FooterPart>();
    var paragraph = new Paragraph(new ParagraphProperties(
        new ParagraphStyleId { Val = "Meta" }, new Justification { Val = JustificationValues.Right }));
    var runProperties = new RunProperties(new Color { Val = "6F7782" }, new FontSize { Val = "18" });
    paragraph.Append(new Run(runProperties, new Text("Seite ")));
    paragraph.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
    paragraph.Append(new Run(new FieldCode(" PAGE ")));
    paragraph.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
    part.Footer = new Footer(paragraph);
    part.Footer.Save();
    return mainPart.GetIdOfPart(part);
}

static Paragraph Para(string text, string styleId, string? color = null, int? size = null)
{
    var runProperties = new RunProperties();
    if (color is not null) runProperties.Append(new Color { Val = color });
    if (size is not null) runProperties.Append(new FontSize { Val = size.Value.ToString() });
    return new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
        new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
}

static Paragraph H1(string text) => Para(text, "Heading1");

static Paragraph Code(string text) => new(
    new ParagraphProperties(
        new ParagraphStyleId { Val = "Code" },
        new ParagraphBorders(new LeftBorder { Val = BorderValues.Single, Color = "B4C6E7", Size = 12, Space = 8 }),
        new Shading { Val = ShadingPatternValues.Clear, Fill = "F4F7FB" },
        new Indentation { Left = "220" }),
    new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

static Paragraph Bullet(string text) => new(
    new ParagraphProperties(
        new ParagraphStyleId { Val = "BodyText" },
        new NumberingProperties(new NumberingLevelReference { Val = 0 }, new NumberingId { Val = 1 }),
        new SpacingBetweenLines { After = "60", Line = "276", LineRule = LineSpacingRuleValues.Auto }),
    new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

static Table NewTable(int[] widths)
{
    var table = new Table();
    table.Append(new TableProperties(
        new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
        new TableBorders(
            new TopBorder { Val = BorderValues.Single, Color = "B4C6E7", Size = 4 },
            new LeftBorder { Val = BorderValues.Single, Color = "B4C6E7", Size = 4 },
            new BottomBorder { Val = BorderValues.Single, Color = "B4C6E7", Size = 4 },
            new RightBorder { Val = BorderValues.Single, Color = "B4C6E7", Size = 4 },
            new InsideHorizontalBorder { Val = BorderValues.Single, Color = "D9E2F3", Size = 3 },
            new InsideVerticalBorder { Val = BorderValues.Single, Color = "D9E2F3", Size = 3 })));
    var grid = new TableGrid();
    foreach (var width in widths)
        grid.Append(new GridColumn { Width = width.ToString() });
    table.Append(grid);
    return table;
}

static TableCell Cell(string text, string fill, bool bold, int width, string? color = null)
{
    var runProperties = new RunProperties();
    if (bold) runProperties.Append(new Bold());
    if (color is not null) runProperties.Append(new Color { Val = color });
    return new TableCell(
        new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = width.ToString() },
            new Shading { Val = ShadingPatternValues.Clear, Fill = fill },
            new TableCellMargin(
                new TopMargin { Width = "70", Type = TableWidthUnitValues.Dxa },
                new TableCellLeftMargin { Width = 100, Type = TableWidthValues.Dxa },
                new BottomMargin { Width = "70", Type = TableWidthUnitValues.Dxa },
                new TableCellRightMargin { Width = 100, Type = TableWidthValues.Dxa })),
        new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "Meta" }),
            new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
}

static Table GridTable(string[] headers, IReadOnlyList<string[]> rows, int[] widths)
{
    var table = NewTable(widths);
    var headerRow = new TableRow(new TableRowProperties(new TableHeader()));
    for (var index = 0; index < headers.Length; index++)
        headerRow.Append(Cell(headers[index], "17365D", true, widths[index], "FFFFFF"));
    table.Append(headerRow);
    var alternate = false;
    foreach (var row in rows)
    {
        var fill = alternate ? "F4F7FB" : "FFFFFF";
        var tableRow = new TableRow();
        for (var index = 0; index < row.Length; index++)
            tableRow.Append(Cell(row[index], fill, index == 0, widths[index]));
        table.Append(tableRow);
        alternate = !alternate;
    }
    return table;
}
