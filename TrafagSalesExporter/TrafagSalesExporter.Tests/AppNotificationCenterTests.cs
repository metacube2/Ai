using MudBlazor;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Tests der Sammelstelle hinter der Statusampel in der Kopfleiste
/// (Auftrag Ingo, 2026-08-21). Geprueft wird die Zustandslogik ohne Oberflaeche:
/// Zaehler, Ampelfarbe, Obergrenze und das Zuruecksetzen.
/// </summary>
public class AppNotificationCenterTests
{
    [Fact]
    public void Record_HaeltMeldungNeuesteZuerst()
    {
        var center = new AppNotificationCenter();

        center.Record("erste", Severity.Info);
        center.Record("zweite", Severity.Info);

        Assert.Equal(2, center.Notifications.Count);
        Assert.Equal("zweite", center.Notifications[0].Message);
        Assert.Equal("erste", center.Notifications[1].Message);
        Assert.Equal(2, center.UnseenCount);
    }

    /// <summary>
    /// Der Kernfall der Ampel: ohne Auffaelligkeit gruen, sonst die schwerste Einstufung.
    /// `Info` und `Normal` heben die Ampel bewusst NICHT von Gruen ab, sonst stuende sie
    /// bei 129 Meldungsstellen dauernd auf Gelb und waere wertlos.
    /// </summary>
    [Theory]
    [InlineData(Severity.Info, Severity.Success)]
    [InlineData(Severity.Normal, Severity.Success)]
    [InlineData(Severity.Success, Severity.Success)]
    [InlineData(Severity.Warning, Severity.Warning)]
    [InlineData(Severity.Error, Severity.Error)]
    public void WorstUnseen_StuftRichtigEin(Severity recorded, Severity expected)
    {
        var center = new AppNotificationCenter();
        center.Record("x", recorded);

        Assert.Equal(expected, center.WorstUnseen);
    }

    [Fact]
    public void WorstUnseen_FehlerSchlaegtWarnungUnabhaengigVonDerReihenfolge()
    {
        var first = new AppNotificationCenter();
        first.Record("Warnung", Severity.Warning);
        first.Record("Fehler", Severity.Error);

        var second = new AppNotificationCenter();
        second.Record("Fehler", Severity.Error);
        second.Record("Warnung", Severity.Warning);

        Assert.Equal(Severity.Error, first.WorstUnseen);
        Assert.Equal(Severity.Error, second.WorstUnseen);
    }

    [Fact]
    public void WorstUnseen_IstGruenOhneMeldungen()
    {
        var center = new AppNotificationCenter();

        Assert.Equal(Severity.Success, center.WorstUnseen);
        Assert.Equal(0, center.UnseenCount);
        Assert.Empty(center.Notifications);
    }

    /// <summary>
    /// Ansehen setzt die Ampel zurueck, loescht die Liste aber NICHT - sonst waere die
    /// Meldung genau nach dem Hinsehen nicht mehr nachlesbar.
    /// </summary>
    [Fact]
    public void MarkAllSeen_SetztAmpelZurueckAberBehaeltDieListe()
    {
        var center = new AppNotificationCenter();
        center.Record("Fehler", Severity.Error);

        center.MarkAllSeen();

        Assert.Equal(0, center.UnseenCount);
        Assert.Equal(Severity.Success, center.WorstUnseen);
        Assert.Single(center.Notifications);
    }

    /// <summary>
    /// Nach dem Ansehen darf eine NEUE Meldung die Ampel wieder ausschlagen lassen; sonst
    /// bliebe sie nach dem ersten Blick fuer immer gruen.
    /// </summary>
    [Fact]
    public void Record_NachAnsehenSchlaegtDieAmpelWiederAus()
    {
        var center = new AppNotificationCenter();
        center.Record("alt", Severity.Error);
        center.MarkAllSeen();

        center.Record("neu", Severity.Warning);

        Assert.Equal(1, center.UnseenCount);
        Assert.Equal(Severity.Warning, center.WorstUnseen);
    }

    /// <summary>
    /// Die schon angesehene Fehlermeldung darf die Ampel nicht weiter auf Rot halten,
    /// obwohl sie in der Liste bleibt.
    /// </summary>
    [Fact]
    public void WorstUnseen_IgnoriertBereitsAngeseheneMeldungen()
    {
        var center = new AppNotificationCenter();
        center.Record("alter Fehler", Severity.Error);
        center.MarkAllSeen();

        center.Record("neuer Hinweis", Severity.Info);

        Assert.Equal(Severity.Success, center.WorstUnseen);
        Assert.Equal(2, center.Notifications.Count);
    }

    [Fact]
    public void Clear_LeertListeUndZaehler()
    {
        var center = new AppNotificationCenter();
        center.Record("a", Severity.Error);
        center.Record("b", Severity.Warning);

        center.Clear();

        Assert.Empty(center.Notifications);
        Assert.Equal(0, center.UnseenCount);
        Assert.Equal(Severity.Success, center.WorstUnseen);
    }

    /// <summary>
    /// Ein Vollimport kann sehr viele Meldungen erzeugen. Ohne Obergrenze waechst die Liste
    /// ueber die Lebensdauer der Sitzung unbegrenzt.
    /// </summary>
    [Fact]
    public void Record_BegrenztDieListeUndHaeltDieNeuesten()
    {
        var center = new AppNotificationCenter();

        for (var i = 0; i < AppNotificationCenter.MaxEntries + 25; i++)
            center.Record($"Meldung {i}", Severity.Info);

        Assert.Equal(AppNotificationCenter.MaxEntries, center.Notifications.Count);
        Assert.Equal($"Meldung {AppNotificationCenter.MaxEntries + 24}", center.Notifications[0].Message);
        // Der Zaehler darf die Liste nie ueberschreiten, sonst zeigte die Ampel mehr
        // ungesehene Meldungen an, als nachlesbar sind.
        Assert.Equal(AppNotificationCenter.MaxEntries, center.UnseenCount);
    }

    [Fact]
    public void Record_UebergehtLeereTexte()
    {
        var center = new AppNotificationCenter();

        center.Record("", Severity.Error);
        center.Record("   ", Severity.Error);

        Assert.Empty(center.Notifications);
        Assert.Equal(Severity.Success, center.WorstUnseen);
    }

    [Fact]
    public void Record_SchneidetRandzeichenAb()
    {
        var center = new AppNotificationCenter();

        center.Record("  Text mit Rand  ", Severity.Info);

        Assert.Equal("Text mit Rand", center.Notifications[0].Message);
    }

    [Fact]
    public void Changed_MeldetJedeAenderung()
    {
        var center = new AppNotificationCenter();
        var count = 0;
        center.Changed += () => count++;

        center.Record("a", Severity.Info);
        center.MarkAllSeen();
        center.Clear();

        Assert.Equal(3, count);
    }

    /// <summary>
    /// Ohne Aenderung darf kein Ereignis kommen, sonst zeichnet die Oberflaeche grundlos neu.
    /// </summary>
    [Fact]
    public void Changed_SchweigtOhneAenderung()
    {
        var center = new AppNotificationCenter();
        var count = 0;
        center.Changed += () => count++;

        center.MarkAllSeen();
        center.Clear();

        Assert.Equal(0, count);
    }
}
