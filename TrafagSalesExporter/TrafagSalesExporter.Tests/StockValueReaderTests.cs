using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Tests der Lagerwert-Aggregation (Wunsch Armin, docs/EINKAUF_LAGERWERT_2026-08-18.md).
///
/// Geprueft wird die reine Rechenlogik ohne SAP. Die Faelle sind nicht erfunden, sondern
/// stammen aus der Live-Messung auf T76/100 vom 2026-08-18/19.
/// </summary>
public class StockValueReaderTests
{
    private static Dictionary<string, object?> Row(
        string matnr, string salk3, string lbkum = "0", string bwtar = "")
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Matnr"] = matnr,
            ["Salk3"] = salk3,
            ["Lbkum"] = lbkum,
            ["Bwtar"] = bwtar
        };

    [Fact]
    public void Aggregate_SummiertJeDisponent()
    {
        var rows = new[]
        {
            Row("A1", "100.00", "10"),
            Row("A2", "50.50", "5"),
            Row("B1", "200.00", "20")
        };
        var planners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A1"] = "001",
            ["A2"] = "001",
            ["B1"] = "003"
        };

        var snapshot = SapGatewayStockValueReader.Aggregate("1100", rows, planners, DateTime.UtcNow);

        Assert.Equal(2, snapshot.Rows.Count);
        var first = snapshot.Rows.Single(row => row.Planner == "001");
        Assert.Equal(150.50m, first.Value);
        Assert.Equal(15m, first.Quantity);
        Assert.Equal(2, first.MaterialCount);
        Assert.Equal(350.50m, snapshot.TotalValue);
    }

    /// <summary>
    /// Der Kernfall der Abgrenzung: nur die Einkaufsdisponenten zaehlen. Am 2026-08-19 waren
    /// das CHF 8'993'258.78 von CHF 10'947'696.40 gesamt.
    /// </summary>
    [Fact]
    public void ValueForPlanners_GrenztAufEinkaufsdisponentenAb()
    {
        var rows = new[]
        {
            Row("A1", "1000.00"),   // 001 - Einkauf
            Row("B1", "2000.00"),   // 003 - Einkauf
            Row("C1", "9999.00")    // A99 - Produktion, gehoert NICHT dazu
        };
        var planners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A1"] = "001",
            ["B1"] = "003",
            ["C1"] = "A99"
        };

        var snapshot = SapGatewayStockValueReader.Aggregate("1100", rows, planners, DateTime.UtcNow);

        Assert.Equal(3000.00m, snapshot.ValueForPlanners(PurchasingDashboardService.PurchasingPlanners));
        Assert.Equal(2, snapshot.MaterialCountForPlanners(PurchasingDashboardService.PurchasingPlanners));
        // Die Gesamtsumme enthaelt weiterhin alles - die Abgrenzung ist ein Filter, keine
        // Loeschung. Sonst waere der Anteil am Gesamtlagerwert nicht mehr ausweisbar.
        Assert.Equal(12999.00m, snapshot.TotalValue);
    }

    /// <summary>
    /// Getrennte Bewertung: SAP fuehrt je Material einen Kopfsatz (BWTAR leer) UND Teilsaetze.
    /// Beide zu summieren verdoppelt den Wert. Am 2026-08-18 gab es im Bewertungskreis 1100
    /// zwar 0 Teilsaetze, aber die Absicherung muss halten, falls sich das aendert.
    /// </summary>
    [Fact]
    public void Aggregate_IgnoriertTeilsaetzeMitBewertungsart()
    {
        var rows = new[]
        {
            Row("A1", "100.00", bwtar: ""),        // Kopfsatz, zaehlt
            Row("A1", "60.00", bwtar: "ROH"),      // Teilsatz, darf NICHT dazukommen
            Row("A1", "40.00", bwtar: "FERT")      // Teilsatz, darf NICHT dazukommen
        };
        var planners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["A1"] = "001" };

        var snapshot = SapGatewayStockValueReader.Aggregate("1100", rows, planners, DateTime.UtcNow);

        Assert.Equal(100.00m, snapshot.TotalValue);
        Assert.Equal(1, snapshot.TotalMaterialCount);
    }

    /// <summary>
    /// Material ohne gepflegten Disponenten darf nicht stillschweigend verschwinden - sonst
    /// stimmt die Gesamtsumme nicht mehr mit MB5L ueberein und niemand merkt es.
    /// Am 2026-08-18 hatten 2'172 Materialien im Werk 1100 keinen Disponenten.
    /// </summary>
    [Fact]
    public void Aggregate_HaeltMaterialienOhneDisponentSichtbar()
    {
        var rows = new[] { Row("A1", "100.00"), Row("X1", "25.00") };
        var planners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["A1"] = "001" };

        var snapshot = SapGatewayStockValueReader.Aggregate("1100", rows, planners, DateTime.UtcNow);

        Assert.Equal(125.00m, snapshot.TotalValue);
        var orphan = snapshot.Rows.Single(row => row.Planner == SapGatewayStockValueReader.WithoutPlanner);
        Assert.Equal(25.00m, orphan.Value);
        // Aber sie zaehlen nicht zu den Einkaufsteilen.
        Assert.Equal(100.00m, snapshot.ValueForPlanners(PurchasingDashboardService.PurchasingPlanners));
    }

    [Fact]
    public void Aggregate_SortiertNachWertAbsteigend()
    {
        var rows = new[] { Row("A1", "10.00"), Row("B1", "500.00"), Row("C1", "100.00") };
        var planners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A1"] = "001",
            ["B1"] = "002",
            ["C1"] = "003"
        };

        var snapshot = SapGatewayStockValueReader.Aggregate("1100", rows, planners, DateTime.UtcNow);

        Assert.Equal(["002", "003", "001"], snapshot.Rows.Select(row => row.Planner).ToArray());
    }

    /// <summary>
    /// Negative Bestandswerte sind fachlich moeglich und duerfen die Summe mindern, statt
    /// als Fehler verworfen zu werden.
    /// </summary>
    [Fact]
    public void Aggregate_UebernimmtNegativeWerte()
    {
        var rows = new[] { Row("A1", "100.00"), Row("A2", "-30.00") };
        var planners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A1"] = "001",
            ["A2"] = "001"
        };

        var snapshot = SapGatewayStockValueReader.Aggregate("1100", rows, planners, DateTime.UtcNow);

        Assert.Equal(70.00m, snapshot.TotalValue);
    }

    [Fact]
    public void Aggregate_UebergehtZeilenOhneMaterialnummer()
    {
        var rows = new[] { Row("", "999.00"), Row("A1", "100.00") };
        var planners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["A1"] = "001" };

        var snapshot = SapGatewayStockValueReader.Aggregate("1100", rows, planners, DateTime.UtcNow);

        Assert.Equal(100.00m, snapshot.TotalValue);
    }

    /// <summary>
    /// Der Cache muss nach Ablauf der Lebensdauer nichts mehr liefern, sonst zeigt die Kachel
    /// stumm einen veralteten Stand als waere er aktuell.
    /// </summary>
    [Fact]
    public void GetCached_LiefertNichtsOhneVorherigenRead()
    {
        var reader = new SapGatewayStockValueReader(new NullAppEventLogService());

        Assert.Null(reader.GetCached("1100"));
        Assert.Null(reader.GetCached(""));
    }

    /// <summary>Ein leerer Bewertungskreis wuerde ueber mehrere Hauswaehrungen summieren.</summary>
    [Fact]
    public async Task RefreshAsync_LehntLeerenBewertungskreisAb()
    {
        var reader = new SapGatewayStockValueReader(new NullAppEventLogService());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            reader.RefreshAsync("http://example/", "u", "p", "  "));
    }

    private static Dictionary<string, object?> MarcRow(string matnr, string werks, string dispo)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Matnr"] = matnr,
            ["Werks"] = werks,
            ["Dispo"] = dispo
        };

    /// <summary>
    /// REGRESSION zum Vorfall 2026-08-19 bis 2026-08-21. `MARCSet` ignoriert `$filter`, der
    /// Aufruf bekommt also ALLE Werke geliefert. Ohne clientseitigen Filter erhielt ein
    /// Material, das in mehreren Werken liegt, den Disponenten des zuletzt gelesenen Werks
    /// (Last-Wins) und die Abgrenzung auf die Einkaufsdisponenten war still falsch.
    /// </summary>
    [Fact]
    public void ParsePlannerMap_FiltertFremdeWerkeAus()
    {
        var rows = new[]
        {
            MarcRow("A1", "1100", "001"),
            MarcRow("A1", "1200", "A99"),   // anderes Werk, darf 1100 NICHT ueberschreiben
            MarcRow("B1", "1200", "A99")    // gehoert gar nicht in den Bewertungskreis
        };

        var map = SapGatewayStockValueReader.ParsePlannerMap(rows, "1100", out var sawDispo);

        Assert.True(sawDispo);
        Assert.Equal("001", map["A1"]);
        Assert.False(map.ContainsKey("B1"));
        Assert.Single(map);
    }

    /// <summary>
    /// Liefert das Set kein `Werks`, darf nicht stumm eine leere Zuordnung entstehen — dann
    /// waere der ganze Lagerwert ohne Disponent und die Kachel zeigte nur „(ohne Disponent)".
    /// </summary>
    [Fact]
    public void ParsePlannerMap_NimmtAlleZeilenWennWerksFehlt()
    {
        var rows = new[]
        {
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                { ["Matnr"] = "A1", ["Dispo"] = "001" }
        };

        var map = SapGatewayStockValueReader.ParsePlannerMap(rows, "1100", out var sawDispo);

        Assert.True(sawDispo);
        Assert.Equal("001", map["A1"]);
    }

    /// <summary>
    /// Fehlt `Dispo` ganz, muss das erkennbar sein. Der Aufrufer schreibt daraufhin eine
    /// Warnung, statt die Abgrenzung still wirkungslos zu lassen.
    /// </summary>
    [Fact]
    public void ParsePlannerMap_MeldetFehlendesDispoFeld()
    {
        var rows = new[]
        {
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                { ["Matnr"] = "A1", ["Werks"] = "1100" }
        };

        var map = SapGatewayStockValueReader.ParsePlannerMap(rows, "1100", out var sawDispo);

        Assert.False(sawDispo);
        Assert.Equal(string.Empty, map["A1"]);
    }

    /// <summary>
    /// Ohne Bewertungskreis wird nicht gefiltert. Das ist kein Normalfall, aber es darf keine
    /// leere Zuordnung ergeben.
    /// </summary>
    [Fact]
    public void ParsePlannerMap_OhneBewertungskreisNimmtAlles()
    {
        var rows = new[]
        {
            MarcRow("A1", "1100", "001"),
            MarcRow("B1", "1200", "002")
        };

        var map = SapGatewayStockValueReader.ParsePlannerMap(rows, "", out _);

        Assert.Equal(2, map.Count);
    }

    private sealed class NullAppEventLogService : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info",
            int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message,
            int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }
}
