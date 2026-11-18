using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ReadOnlySqlGuardTests
{
    [Theory]
    [InlineData("SELECT 1 FROM DUMMY")]
    [InlineData("select * from TRAFAG_LIVE.\"OITM\"")]
    [InlineData("WITH x AS (SELECT 1 AS a FROM DUMMY) SELECT a FROM x")]
    [InlineData("SELECT 1 FROM DUMMY;")]
    [InlineData("   SELECT 1 FROM DUMMY   ")]
    public void Erlaubt_lesende_Statements(string sql)
    {
        Assert.True(ReadOnlySqlGuard.IsAllowed(sql), ReadOnlySqlGuard.Validate(sql));
    }

    [Theory]
    [InlineData("UPDATE \"OITM\" SET \"CardCode\" = 'X'")]
    [InlineData("DELETE FROM \"OITM\"")]
    [InlineData("INSERT INTO \"OITM\" VALUES (1)")]
    [InlineData("DROP TABLE \"OITM\"")]
    [InlineData("TRUNCATE TABLE \"OITM\"")]
    [InlineData("CALL SOME_PROCEDURE")]
    [InlineData("MERGE INTO \"OITM\" USING x ON 1=1")]
    [InlineData("GRANT SELECT ON SCHEMA X TO Y")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Lehnt_alles_ab_was_nicht_zweifelsfrei_lesend_ist(string? sql)
    {
        // Die Standortsysteme sind fremde Produktivsysteme - eine Positivliste ist hier
        // Pflicht, keine Stilfrage.
        Assert.False(ReadOnlySqlGuard.IsAllowed(sql));
        Assert.NotNull(ReadOnlySqlGuard.Validate(sql));
    }

    [Fact]
    public void Lehnt_ein_angehaengtes_zweites_Statement_ab()
    {
        var sql = "SELECT 1 FROM DUMMY; DELETE FROM \"OITM\"";

        Assert.False(ReadOnlySqlGuard.IsAllowed(sql));
        Assert.Contains("Semikolon", ReadOnlySqlGuard.Validate(sql));
    }

    [Fact]
    public void Lehnt_Kommentarzeichen_ab_weil_sie_ein_Statement_verdecken_koennen()
    {
        Assert.False(ReadOnlySqlGuard.IsAllowed("SELECT 1 FROM DUMMY -- Rest"));
        Assert.False(ReadOnlySqlGuard.IsAllowed("SELECT /* versteckt */ 1 FROM DUMMY"));
    }

    [Fact]
    public void Ein_abschliessendes_Semikolon_bleibt_erlaubt()
    {
        Assert.True(ReadOnlySqlGuard.IsAllowed("SELECT 1 FROM DUMMY ;  "));
    }
}
