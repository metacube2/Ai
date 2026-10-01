using System.Text.Json;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class LogisticsLiveTests
{
    private static IEnumerable<JsonElement> Rows(string json)
        => JsonDocument.Parse(json).RootElement.GetProperty("d").GetProperty("results").EnumerateArray().Select(x => x.Clone()).ToList();

    [Fact]
    public void Filter_Hat_Immer_Das_Datum_Und_Optional_Die_Uhrzeit()
    {
        Assert.Equal("Datum eq '20261001'", SapGatewayLogisticsLiveReader.BuildFilter(new DateOnly(2026, 10, 1), null));
        Assert.Equal("Datum eq '20261001' and Abzeit eq '143015'",
            SapGatewayLogisticsLiveReader.BuildFilter(new DateOnly(2026, 10, 1), new TimeOnly(14, 30, 15)));
        Assert.Contains("LogTaSet?$format=json&$top=2000&$skip=4000&$filter=",
            SapGatewayLogisticsLiveReader.BuildPageUrl("http://sap/srv/", "LogTaSet", "x", 4000));
    }

    [Fact]
    public void ParseTransfers_Liest_Position_Ohne_Benutzer()
    {
        var rows = Rows("""
            {"d":{"results":[{"Lgnum":"110","Tanum":"0000123456","Tapos":"0001","Bwlvs":"601","Vbeln":"0080001234",
              "Matnr":"000000000000036385","Werks":"1100","Vltyp":"001","Vlpla":"A-01","Nltyp":"916","Nlpla":"0080001234",
              "Menge":"2.000","Meins":"ST","Bdatu":"20261001","Bzeit":"141500","Pquit":"X","Qdatu":"20261001","Qzeit":"142010"}]}}
            """);

        var item = Assert.Single(SapGatewayLogisticsLiveReader.ParseTransfers(rows));

        Assert.Equal("36385", item.Material);
        Assert.True(item.Confirmed);
        Assert.Equal(new DateTime(2026, 10, 1, 14, 20, 10), item.ConfirmedAt);
        Assert.Equal(2m, item.Quantity);
    }

    [Fact]
    public void ParseConfirmations_Erkennt_Storno_Und_Endrueckmeldung()
    {
        var rows = Rows("""
            {"d":{"results":[{"Rueck":"0000012345","Rmzhl":"00000001","Aufnr":"000001241824","Vornr":"0010","Arbpl":"MONT1",
              "Werks":"1100","Ersda":"20261001","Erzet":"143000","Lmnga":"5.000","Xmnga":"1.000","Meinh":"ST","Aueru":"X","Stokz":"",
              "Matnr":"000000000000036385","Gamng":"10.000","Igmng":"5.000"}]}}
            """);

        var c = Assert.Single(SapGatewayLogisticsLiveReader.ParseConfirmations(rows));

        Assert.Equal("1241824", c.Order);
        Assert.True(c.Final);
        Assert.False(c.Cancelled);
        Assert.Equal(1m, c.Scrap);
        Assert.Equal(new DateTime(2026, 10, 1, 14, 30, 0), c.At);
    }

    [Fact]
    public void Erster_Abruf_Liest_Den_Ganzen_Tag_Danach_Nur_Die_Letzten_Minuten()
    {
        var now = new DateTime(2026, 10, 1, 14, 30, 0);

        Assert.Equal(TimeOnly.MinValue, LogisticsLiveService.FromTimeFor(null, now));
        Assert.Equal(new TimeOnly(14, 27, 30), LogisticsLiveService.FromTimeFor(new DateTime(2026, 10, 1, 14, 29, 30), now));
        // Kurz nach Mitternacht: Ueberlappung reicht in den Vortag, also ab 00:00.
        Assert.Equal(TimeOnly.MinValue, LogisticsLiveService.FromTimeFor(new DateTime(2026, 10, 1, 0, 1, 0), new DateTime(2026, 10, 1, 0, 1, 30)));
        // Letzter Abruf von gestern: neuer Tag beginnt bei 00:00.
        Assert.Equal(TimeOnly.MinValue, LogisticsLiveService.FromTimeFor(new DateTime(2026, 9, 30, 23, 59, 0), now));
    }

    [Fact]
    public void Merge_Ersetzt_Bekannte_Positionen_Statt_Sie_Zu_Verdoppeln()
    {
        var target = new Dictionary<(string, string, string), LiveTransferItem>();
        var open = Transfer("1", confirmed: false);
        LogisticsLiveService.Merge(target, [open], x => (x.Lgnum, x.Tanum, x.Tapos));
        LogisticsLiveService.Merge(target, [open with { Confirmed = true }], x => (x.Lgnum, x.Tanum, x.Tapos));

        Assert.True(Assert.Single(target.Values).Confirmed);
    }

    [Fact]
    public void Arbeitsplaetze_Und_Auftraege_Ohne_Stornos()
    {
        var now = new DateTime(2026, 10, 1, 14, 30, 0);
        var snapshot = new LogisticsLiveSnapshot
        {
            Day = new DateOnly(2026, 10, 1),
            Confirmations =
            [
                Confirmation("1", "MONT1", now.AddMinutes(-5), 3m),
                Confirmation("2", "MONT1", now.AddMinutes(-40), 2m),
                Confirmation("3", "PRUEF", now.AddMinutes(-90), 1m),
                Confirmation("4", "PRUEF", now.AddMinutes(-1), 9m) with { Cancelled = true }
            ]
        };

        var centers = LogisticsLiveService.WorkCenters(snapshot);
        var mont = Assert.Single(centers, x => x.WorkCenter == "MONT1");
        Assert.Equal(2, mont.ConfirmationsToday);
        Assert.Equal("aktiv", LogisticsLiveService.ActivityClass(mont.LastAt, now));
        Assert.Equal("ruhig", LogisticsLiveService.ActivityClass(Assert.Single(centers, x => x.WorkCenter == "PRUEF").LastAt, now));
        Assert.Equal("eben", LogisticsLiveService.ActivityClass(now.AddMinutes(-40), now));
        Assert.Equal(6m, Assert.Single(LogisticsLiveService.Orders(snapshot)).YieldToday);
    }

    [Fact]
    public void Durchsatz_Zaehlt_Angelegt_Und_Quittiert_Je_Stunde()
    {
        var snapshot = new LogisticsLiveSnapshot
        {
            Day = new DateOnly(2026, 10, 1),
            Transfers =
            [
                Transfer("1", confirmed: true) with { CreatedAt = new DateTime(2026, 10, 1, 7, 10, 0), ConfirmedAt = new DateTime(2026, 10, 1, 8, 5, 0) },
                Transfer("2", confirmed: false) with { CreatedAt = new DateTime(2026, 10, 1, 7, 50, 0) }
            ]
        };

        var hours = LogisticsLiveService.TransferThroughputByHour(snapshot);

        Assert.Equal((7, 2, 0), hours[7]);
        Assert.Equal((8, 0, 1), hours[8]);
        Assert.Equal(1, snapshot.TransferItemsOpen);
    }

    private static LiveTransferItem Transfer(string tanum, bool confirmed)
        => new("110", tanum, "0001", "601", "", "36385", "1100", "001", "A-01", "916", "X", 1m, "ST",
            new DateTime(2026, 10, 1, 7, 0, 0), confirmed, confirmed ? new DateTime(2026, 10, 1, 8, 0, 0) : null);

    private static LiveConfirmation Confirmation(string rmzhl, string workCenter, DateTime at, decimal yield)
        => new("0000012345", rmzhl, "1241824", "0010", workCenter, "1100", at, yield, 0m, "ST", false, false, "36385", 10m, 5m);
}
