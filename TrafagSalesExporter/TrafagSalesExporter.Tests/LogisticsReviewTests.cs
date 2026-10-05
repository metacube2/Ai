using System.Text.Json;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>Review 2026-10-05 zu Logistik live: Randfaelle, die vorher still falsch gerechnet wurden.</summary>
public sealed class LogisticsReviewTests
{
    private static readonly DateOnly Day = new(2026, 10, 5);
    private static readonly DateTime D0 = new(2026, 10, 5, 0, 0, 0);

    private static IEnumerable<JsonElement> Rows(string json)
        => JsonDocument.Parse(json).RootElement.GetProperty("d").GetProperty("results").EnumerateArray().Select(x => x.Clone()).ToList();

    private static LiveTransferItem Ta(string tanum, bool confirmed, DateTime created, DateTime? confirmedAt = null, string lgnum = "110",
        string delivery = "80001", string fromBin = "A-01-1", string fromType = "001")
        => new(lgnum, tanum, "0001", "601", delivery, "36385", "1100", fromType, fromBin, "916", "X", 1m, "ST", created, confirmed, confirmedAt);

    private static LiveDelivery Delivery(string no, DateTime? planned, int positions = 2, int picked = 0, string gi = "", DateTime? giAt = null, string lgnum = "110")
        => new(no, "LF", "1100", lgnum, "K1", "Kunde", "A", gi, positions, picked, 0, planned, giAt);

    // 2: Warenausgang ohne Uhrzeit und widerspruechlich werden gezaehlt statt still verworfen.
    [Fact]
    public void BisWarenausgang_Zaehlt_Lieferungen_Ohne_Uhrzeit_Und_Widerspruechliche()
    {
        var runs = LogisticsProductivity.PickingRuns(
        [
            Ta("1", true, D0.AddHours(8), D0.AddHours(9), delivery: "80001"),
            Ta("2", true, D0.AddHours(8), D0.AddHours(9), delivery: "80002"),
            Ta("3", true, D0.AddHours(8), D0.AddHours(9), delivery: "80003"),
            Ta("4", true, D0.AddHours(8), D0.AddHours(9), delivery: "80004")
        ]);
        var stats = LogisticsProductivity.GoodsIssueDurations(runs,
        [
            Delivery("80001", D0, gi: "C", giAt: D0.AddHours(10)),
            Delivery("80002", D0, gi: "C", giAt: D0),                       // nur Datum
            Delivery("80003", D0, gi: "C", giAt: D0.AddHours(7)),           // vor dem ersten TA
            Delivery("80004", D0)                                           // kein Warenausgang
        ]);

        Assert.Equal([120d], stats.Minutes);
        Assert.Equal(1, stats.WithoutTime);
        Assert.Equal(1, stats.Inconsistent);
        Assert.Equal([120d], LogisticsProductivity.UntilGoodsIssue(runs, [Delivery("80001", D0, giAt: D0.AddHours(10))]));
    }

    // 6: Frueher quittierte Positionen zaehlen nicht als Leistung des Tages.
    [Fact]
    public void Leistung_Zaehlt_Nur_Was_Am_Tag_Quittiert_Wurde()
    {
        var yesterday = D0.AddDays(-1);
        var items = new[]
        {
            Ta("1", true, yesterday.AddHours(8), yesterday.AddHours(9)),
            Ta("2", true, D0.AddHours(7), D0.AddHours(8)),
            Ta("3", false, yesterday.AddHours(10))
        };

        Assert.Equal(1, Assert.Single(LogisticsProductivity.HourlyOutput(items, Day)).Confirmed);
        Assert.Equal(2, LogisticsProductivity.HourlyOutput(items).Sum(o => o.Confirmed));
        Assert.Equal((1, 1), LogisticsCapacityStore.PickingFromLive(items, Day));
        var snapshot = new LogisticsLiveSnapshot { Day = Day, Transfers = items };
        Assert.Equal(1, snapshot.TransferItemsConfirmed);
        Assert.Equal(1, snapshot.TransferItemsOpen);
        Assert.Equal(2, snapshot.TransferItemsTotal);
    }

    [Fact]
    public void Personenleistung_Mit_Tag_Ignoriert_Vortage()
    {
        var yesterday = D0.AddDays(-1);
        var items = new[]
        {
            Ta("1", true, yesterday.AddHours(8), yesterday.AddHours(9)) with { ConfirmedBy = "MUELLER", CreatedBy = "MUELLER" },
            Ta("2", true, D0.AddHours(7), D0.AddHours(8)) with { ConfirmedBy = "MUELLER", CreatedBy = "MUELLER" }
        };

        var person = Assert.Single(LogisticsPeople.Outputs(items, Day));

        Assert.Equal(1, person.Confirmed);
        Assert.Equal(1, person.Created);
    }

    // 7: Storno ueber Stornozaehler, Original wird ueber den Schluessel entwertet; ohne Feld bleibt alles beim Alten.
    [Fact]
    public void Storno_Mit_Stzhl_Entwertet_Das_Original()
    {
        var rows = Rows("""
            {"d":{"results":[
              {"Rueck":"0000012345","Rmzhl":"00000001","Aufnr":"1","Vornr":"0010","Ersda":"20261005","Erzet":"080000","Lmnga":"5","Stokz":""},
              {"Rueck":"0000012345","Rmzhl":"00000002","Aufnr":"1","Vornr":"0010","Ersda":"20261005","Erzet":"090000","Lmnga":"5","Stokz":"","Stzhl":"00000001"}
            ]}}
            """);
        var parsed = SapGatewayLogisticsLiveReader.ParseConfirmations(rows);
        Assert.False(parsed[0].Cancelled);
        Assert.True(parsed[1].Cancelled);

        var target = new Dictionary<(string, string), LiveConfirmation>();
        LogisticsLiveService.MergeConfirmations(target, [parsed[0]]);
        Assert.False(target[("0000012345", "00000001")].Cancelled);
        LogisticsLiveService.MergeConfirmations(target, [parsed[1]]);
        Assert.True(target[("0000012345", "00000001")].Cancelled);
    }

    [Fact]
    public void Stzhl_Null_Gilt_Nicht_Als_Storno()
    {
        var rows = Rows("""{"d":{"results":[{"Rueck":"1","Rmzhl":"00000001","Ersda":"20261005","Erzet":"080000","Stokz":"","Stzhl":"00000000"}]}}""");

        Assert.False(Assert.Single(SapGatewayLogisticsLiveReader.ParseConfirmations(rows)).Cancelled);
    }

    // 4/8: Filter mit Lagernummer und ueberfaellig nur auf Wunsch.
    [Fact]
    public void Lieferungsfilter_Traegt_Lagernummer_Und_Ueberfaellig_Nur_Wenn_Verlangt()
    {
        Assert.Equal("Datum eq '20261005'", SapGatewayLogisticsLiveReader.BuildDeliveryFilter(Day, null, false));
        Assert.Equal("Datum eq '20261005' and Lgnum eq '110'", SapGatewayLogisticsLiveReader.BuildDeliveryFilter(Day, "110", false));
        Assert.Equal("Datum eq '20261005' and Lgnum eq '110' and Ueberf eq 'X'", SapGatewayLogisticsLiveReader.BuildDeliveryFilter(Day, "110", true));
    }

    [Fact]
    public void Vorschau_Hat_Heute_Ueberfaellig_Und_Zaehlt_Nur_Lieferungen_Mit_Positionen()
    {
        var buckets = LogisticsOutlook.Build(
        [
            Delivery("1", D0, positions: 3, picked: 1),
            Delivery("2", D0, positions: 0),
            Delivery("3", D0.AddDays(-2), positions: 4),
            Delivery("4", D0.AddDays(-2), positions: 4, gi: "C"),
            Delivery("5", D0.AddDays(1), positions: 2)
        ], Day);

        var overdue = Assert.Single(buckets, b => b.Overdue);
        Assert.Equal((1, 4), (overdue.Deliveries, overdue.Positions));
        var today = buckets.First(b => !b.Overdue);
        Assert.Equal(Day, today.Day);
        Assert.Equal((1, 2, 1), (today.Deliveries, today.Positions, today.WithoutPositions));
        Assert.Equal(15, buckets.Count(b => !b.Overdue));
        Assert.DoesNotContain(buckets, b => b.Overdue && b.Day != Day);
    }

    [Fact]
    public void Vorschau_Ohne_Ueberfaellige_Hat_Keinen_Eimer()
        => Assert.DoesNotContain(LogisticsOutlook.Build([Delivery("1", D0)], Day), b => b.Overdue);

    // 1: Fehler nur eine Minute, Erfolg 15.
    [Fact]
    public void Fehler_Werden_Nur_Eine_Minute_Gemerkt()
    {
        Assert.Equal(TimeSpan.FromMinutes(15), LogisticsLiveService.CacheTtl(null));
        Assert.Equal(TimeSpan.FromMinutes(1), LogisticsLiveService.CacheTtl("Timeout"));
    }

    // 11: Lagernummer im Gang, doppelte Zellen, abgeschnittene Gaenge.
    [Fact]
    public void Gaenge_Verschiedener_Lager_Bleiben_Getrennt()
    {
        var bins = LogisticsBinLayout.Bins([Ta("1", false, D0, lgnum: "110", fromBin: "A-01-1"), Ta("2", false, D0, lgnum: "120", fromBin: "A-01-1")]);

        var aisles = LogisticsBinLayout.Aisles(bins);

        Assert.Equal(2, aisles.Count(a => a.Aisle == "A"));
        Assert.Equal(["110", "120"], aisles.Select(a => a.Lgnum).Order().ToArray());
    }

    [Fact]
    public void Plaetze_Auf_Derselben_Zelle_Werden_Zusammengefasst()
    {
        var bins = LogisticsBinLayout.Bins([Ta("1", false, D0, fromBin: "A-01-1"), Ta("2", false, D0, fromBin: "A-1-1"), Ta("3", false, D0, fromBin: "A-2-1")]);

        var aisle = Assert.Single(LogisticsBinLayout.Aisles(bins));

        Assert.Equal(2, aisle.Bins.Count);
        Assert.Equal(2, aisle.Bins.Select(b => (b.Column, b.Level)).Distinct().Count());
        Assert.Equal(2, aisle.Bins.Max(b => b.Moves.Count));
    }

    [Fact]
    public void Zu_Viele_Gaenge_Werden_Gezaehlt()
    {
        var items = Enumerable.Range(1, 30).Select(i => Ta(i.ToString(), false, D0, fromBin: $"G{i:00}-01-1")).ToList();

        var (shown, omitted) = LogisticsBinLayout.AislesLimited(LogisticsBinLayout.Bins(items), 24);

        Assert.Equal(24, shown.Count);
        Assert.Equal(6, omitted);
    }

    // 12: Gutmenge je Auftrag aus dem letzten Vorgang, Einheiten nicht gemischt.
    [Fact]
    public void Auftragsmenge_Kommt_Aus_Dem_Letzten_Vorgang()
    {
        var now = new DateTime(2026, 10, 5, 14, 0, 0);
        LiveConfirmation C(string rmzhl, string op, DateTime at, decimal yield, string unit = "ST")
            => new("1", rmzhl, "1241824", op, "MONT1", "1100", at, yield, 0m, unit, false, false, "36385", 10m, 5m);
        var snapshot = new LogisticsLiveSnapshot
        {
            Day = Day,
            Confirmations = [C("1", "0010", now.AddHours(-3), 10m), C("2", "0020", now.AddHours(-2), 4m), C("3", "0020", now.AddHours(-1), 3m), C("4", "0030", now.AddMinutes(-90), 2m, "H")]
        };

        Assert.Equal(7m, Assert.Single(LogisticsLiveService.Orders(snapshot)).YieldToday);
        Assert.Equal(17m, Assert.Single(LogisticsLiveService.WorkCenters(snapshot)).YieldToday);
    }

    // 14: Bedarf ohne verfuegbare Stunden.
    [Fact]
    public void Bedarf_Ohne_Verfuegbare_Stunden_Ist_Keine_Kapazitaet_Erfasst()
    {
        var entry = new LogisticsCapacityEntry(Day, "Rüsten Kundenaufträge", 0, 0, 6, 10, 0);
        Assert.Null(entry.LoadPercent);
        Assert.True(entry.NoCapacityRecorded);
        Assert.False(entry with { OpenUnits = 0 } is { NoCapacityRecorded: true });
        Assert.False((entry with { AvailableHours = 8 }).NoCapacityRecorded);
    }

    [Fact]
    public void Lieferung_Kennt_Ob_Die_Warenausgangszeit_Da_Ist()
    {
        Assert.False(Delivery("1", D0, giAt: D0).GoodsIssueTimeKnown);
        Assert.False(Delivery("1", D0).GoodsIssueTimeKnown);
        Assert.True(Delivery("1", D0, giAt: D0.AddHours(9)).GoodsIssueTimeKnown);
    }
}
