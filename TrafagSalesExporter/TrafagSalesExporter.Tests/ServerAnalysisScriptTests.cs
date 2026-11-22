using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ServerAnalysisScriptTests
{
    [Theory]
    [InlineData("TRIN__01_salestype_discovery.sql", "TRIN")]
    [InlineData("trin_01.sql", "TRIN")]
    [InlineData("TRIT__udf.sql", "TRIT")]
    public void ResolveTsc_liest_den_Standort_aus_dem_Dateinamen(string fileName, string expected)
    {
        Assert.Equal(expected, ServerAnalysisScript.ResolveTsc(fileName));
    }

    [Theory]
    [InlineData("ohneUnterstrich.sql")]
    [InlineData("_beginntMitUnterstrich.sql")]
    [InlineData("TR-IN_abfrage.sql")]
    [InlineData("")]
    [InlineData(null)]
    public void ResolveTsc_liefert_null_statt_einen_Standort_zu_raten(string? fileName)
    {
        // Wichtig: eine Abfrage darf nie gegen einen geratenen Standort laufen - im
        // Zweifelsfall wird die Datei uebersprungen.
        Assert.Null(ServerAnalysisScript.ResolveTsc(fileName));
    }

    [Fact]
    public void SplitStatements_trennt_an_der_Trennzeile_und_nimmt_die_Kommentarzeile_als_Beschriftung()
    {
        var raw = string.Join("\n",
            "-- Erste Abfrage",
            "SELECT 1 FROM DUMMY",
            ";;",
            "-- Zweite Abfrage",
            "SELECT 2",
            "FROM DUMMY");

        var statements = ServerAnalysisScript.SplitStatements(raw);

        Assert.Equal(2, statements.Count);
        Assert.Equal("Erste Abfrage", statements[0].Label);
        Assert.Equal("SELECT 1 FROM DUMMY", statements[0].Sql);
        Assert.Equal("Zweite Abfrage", statements[1].Label);
        Assert.Equal("SELECT 2 FROM DUMMY", statements[1].Sql);
    }

    [Fact]
    public void SplitStatements_entfernt_Kommentarzeilen_aus_dem_SQL()
    {
        // Der Guardrail lehnt '--' ab; Kommentare muessen deshalb vorher entfernt sein,
        // sonst wuerde jede dokumentierte Abfrage abgelehnt.
        var raw = string.Join("\n",
            "-- Beschriftung",
            "-- weitere Erklaerung",
            "SELECT COUNT(*)",
            "-- Hinweis mitten im Statement",
            "FROM DUMMY");

        var statements = ServerAnalysisScript.SplitStatements(raw);

        Assert.Single(statements);
        Assert.Equal("SELECT COUNT(*) FROM DUMMY", statements[0].Sql);
        Assert.True(ReadOnlySqlGuard.IsAllowed(statements[0].Sql));
    }

    [Fact]
    public void SplitStatements_ignoriert_Bloecke_ohne_SQL()
    {
        var raw = "-- nur ein Kommentar\n;;\nSELECT 1 FROM DUMMY";

        var statements = ServerAnalysisScript.SplitStatements(raw);

        Assert.Single(statements);
        Assert.Equal("SELECT 1 FROM DUMMY", statements[0].Sql);
    }

    [Fact]
    public void SplitStatements_verarbeitet_Windows_Zeilenumbrueche()
    {
        var raw = "-- Eins\r\nSELECT 1 FROM DUMMY\r\n;;\r\n-- Zwei\r\nSELECT 2 FROM DUMMY";

        var statements = ServerAnalysisScript.SplitStatements(raw);

        Assert.Equal(2, statements.Count);
        Assert.Equal("SELECT 2 FROM DUMMY", statements[1].Sql);
    }

    [Fact]
    public void ApplySchema_ersetzt_beide_Platzhalter_in_der_passenden_Schreibweise()
    {
        // {schema} behaelt die konfigurierte Schreibweise (HANA-Identifier sind
        // case-sensitiv), {SCHEMA} wird fuer Systemsichten gross geschrieben.
        var sql = "SELECT * FROM {schema}.\"OITM\" WHERE '{SCHEMA}' = '{SCHEMA}'";

        var result = ServerAnalysisScript.ApplySchema(sql, "it01_p");

        Assert.Equal("SELECT * FROM it01_p.\"OITM\" WHERE 'IT01_P' = 'IT01_P'", result);
    }
}
