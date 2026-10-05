using System.Text.Json;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class LogisticsPeopleTests
{
    private static LiveTransferItem Ta(string tanum, int minute, string user, bool confirmed = true)
        => new("100", tanum, "0001", "601", "80001", "M", "1100", "001", "A", "916", "X", 1m, "ST",
            new DateTime(2026, 10, 5, 8, 0, 0), confirmed, confirmed ? new DateTime(2026, 10, 5, 8, 0, 0).AddMinutes(minute) : null,
            "ANLEGER", "", user);

    [Fact]
    public void Anonym_Enthaelt_Keine_Benutzer()
    {
        var anonymous = LogisticsPeople.Anonymize([Ta("1", 5, "MUELLER"), Ta("2", 9, "MEIER")]);

        Assert.False(LogisticsPeople.HasNames(anonymous));
        Assert.All(anonymous, t => Assert.Equal("", t.CreatedBy + t.PickedBy + t.ConfirmedBy));
        Assert.Empty(LogisticsPeople.Outputs(anonymous));
    }

    [Fact]
    public void Stunden_Mit_Pausen_Geschaetzt()
    {
        // 8:00-8:30 durchgehend (Abstaende 10 min), dann Pause, 9:30 einzeln (2 min Mindestzeit).
        var times = new[] { 0, 10, 20, 30, 90 }.Select(m => new DateTime(2026, 10, 5, 8, 0, 0).AddMinutes(m)).ToList();

        Assert.Equal(32.0 / 60, LogisticsPeople.EstimateHours(times), 6);
    }

    [Fact]
    public void Leistung_Je_Person()
    {
        var people = LogisticsPeople.Outputs([Ta("1", 0, "MUELLER"), Ta("2", 10, "MUELLER"), Ta("3", 5, "MEIER"), Ta("4", 0, "MEIER", confirmed: false)]);

        var m = people.Single(p => p.User == "MUELLER");
        Assert.Equal(2, m.Confirmed);
        Assert.Equal(10.0 / 60, m.EstimatedHours, 6);
        Assert.Equal(12, m.PerHour!.Value, 6);
        Assert.Equal(1, people.Single(p => p.User == "MEIER").Confirmed);
    }

    [Fact]
    public void Lesen_Ohne_Und_Mit_Neuen_Feldern()
    {
        using var alt = JsonDocument.Parse("""{"Lgnum":"100","Tanum":"1","Tapos":"1","Pquit":"X","Qdatu":"20261005","Qzeit":"081500"}""");
        using var neu = JsonDocument.Parse("""{"Lgnum":"100","Tanum":"2","Tapos":"1","Pquit":"X","Qdatu":"20261005","Qzeit":"081500","Bname":"A1","Ename":"","Qname":"Q1"}""");

        var items = SapGatewayLogisticsLiveReader.ParseTransfers([alt.RootElement, neu.RootElement]);

        Assert.Equal("", items[0].ConfirmedBy);
        Assert.Equal("Q1", items[1].ConfirmedBy);
        Assert.Equal("A1", items[1].CreatedBy);

        using var lf = JsonDocument.Parse("""{"Vbeln":"80001","Wadat":"20261008","WadatIst":"20261008","WaZeit":"143000"}""");
        var d = SapGatewayLogisticsLiveReader.ParseDeliveries([lf.RootElement]).Single();
        Assert.Equal(new DateTime(2026, 10, 8), d.PlannedGoodsIssue);
        Assert.Equal(new DateTime(2026, 10, 8, 14, 30, 0), d.GoodsIssueAt);
    }

    [Fact]
    public void Freischaltung_Nur_Mit_Passwort()
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("geheim123")));
        var monitor = new StaticMonitor(new LogisticsPeopleAccessOptions { Username = "logistik", PasswordHash = hash });
        var access = new LogisticsPeopleAccessService(monitor);

        Assert.False(access.TryUnlock("logistik", "falsch"));
        Assert.True(access.TryUnlock("Logistik", "geheim123"));
        access.Lock();
        Assert.False(access.IsUnlocked);
        Assert.False(new LogisticsPeopleAccessService(new StaticMonitor(new LogisticsPeopleAccessOptions())).TryUnlock("logistik", ""));
    }

    [Fact]
    public void Kapazitaet_Je_Tag_Lesen()
    {
        using var doc = JsonDocument.Parse("""{"Kapid":"10000262","Kapname":"LAG00","Kapar":"002","Arbpl":"MLE01 MLE02","Tag":"20261006","BedarfH":"6.5","AngebotH":"8","Vorgaenge":12,"KeinStandard":""}""");

        var day = SapGatewayLogisticsLiveReader.ParseCapacity([doc.RootElement]).Single();

        Assert.Equal("10000262", day.CapacityId);
        Assert.Equal(new DateOnly(2026, 10, 6), day.Day);
        Assert.Equal(81.25, day.LoadPercent!.Value, 6);
        Assert.False(day.NoStandard);
        Assert.Null((day with { SupplyHours = 0 }).LoadPercent);
    }

    private sealed class StaticMonitor(LogisticsPeopleAccessOptions value) : IOptionsMonitor<LogisticsPeopleAccessOptions>
    {
        public LogisticsPeopleAccessOptions CurrentValue => value;
        public LogisticsPeopleAccessOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<LogisticsPeopleAccessOptions, string?> listener) => null;
    }
}
