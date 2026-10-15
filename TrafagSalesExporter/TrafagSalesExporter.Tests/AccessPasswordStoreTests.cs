using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Security;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>Passwoerter aller Bereiche in der DB, gesetzt ueber die Admin-Seite (2026-10-15).</summary>
public sealed class AccessPasswordStoreTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"access-pw-{Guid.NewGuid():N}.db");
    private readonly FakeAdminAccess _adminAccess = new();

    private static string Hash(string password) => AccessPasswordSettingsWriter.HashPassword(password);

    private ServiceProvider Build(bool withDb = true)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<AdminAccessOptions>(o => { o.Enabled = true; o.Username = "admin"; o.PasswordHash = Hash("altadmin1"); });
        services.Configure<FinanceCockpitAccessOptions>(o => { o.Enabled = true; o.Username = "finance"; o.PasswordHash = Hash("altfinance1"); });
        services.Configure<HrKpiAccessOptions>(o =>
        {
            o.Enabled = true; o.Username = "hr"; o.PasswordHash = Hash("althr123");
            o.AdminUsers.Add(new HrKpiAccessUserOptions { Username = "hradmin", PasswordHash = Hash("althradmin1") });
        });
        services.Configure<ShopfloorOptions>(o => { o.LoginUsername = "operations"; o.LoginPasswordHash = Hash("altoperations1"); });
        services.Configure<LogisticsPeopleAccessOptions>(o => { o.Username = "logistik"; o.PasswordHash = Hash("altlogistik1"); });
        if (withDb)
            services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={_dbPath};Pooling=False"));
        AccessPasswordAdministration.Register(services);
        services.AddSingleton<IAdminAccessService>(_adminAccess);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Ohne_Db_Eintrag_Gilt_Appsettings()
    {
        using var sp = Build();
        Assert.Equal(Hash("altadmin1"), sp.GetRequiredService<IOptions<AdminAccessOptions>>().Value.PasswordHash);
        Assert.Equal(Hash("altoperations1"), sp.GetRequiredService<IOptionsMonitor<ShopfloorOptions>>().CurrentValue.LoginPasswordHash);
    }

    [Fact]
    public void Ohne_Cockpit_Db_Gilt_Appsettings_Und_Nichts_Wirft()
    {
        using var sp = Build(withDb: false);
        Assert.Equal(Hash("altfinance1"), sp.GetRequiredService<IOptions<FinanceCockpitAccessOptions>>().Value.PasswordHash);
        Assert.Empty(sp.GetRequiredService<IAccessPasswordStore>().ReadAll());
    }

    [Fact]
    public void Db_Eintrag_Hat_Vorrang_Auch_Nach_Neustart()
    {
        using (var first = Build())
        {
            first.GetRequiredService<IAccessPasswordStore>().Save(AccessPasswordAreas.Admin, Hash("neuadmin1"), "koi");
            first.GetRequiredService<IAccessPasswordStore>().Save(AccessPasswordAreas.HrUser("HRADMIN"), Hash("neuhradmin1"), "koi");
        }

        using var restarted = Build();   // wie nach einem Deploy: appsettings hat wieder den alten Hash
        Assert.Equal(Hash("neuadmin1"), restarted.GetRequiredService<IOptions<AdminAccessOptions>>().Value.PasswordHash);
        var hr = restarted.GetRequiredService<IOptions<HrKpiAccessOptions>>().Value;
        Assert.Equal(Hash("althr123"), hr.PasswordHash);
        Assert.Equal(Hash("neuhradmin1"), hr.AdminUsers.Single().PasswordHash);
    }

    [Fact]
    public void Gesperrter_Admin_Kann_Nichts_Setzen()
    {
        using var sp = Build();
        _adminAccess.Unlocked = false;
        using var scope = sp.CreateScope();
        Assert.False(scope.ServiceProvider.GetRequiredService<IAccessPasswordAdministration>().TrySetPassword(AccessPasswordAreas.Finance, "neufinance1", "koi"));
        Assert.Equal(Hash("altfinance1"), sp.GetRequiredService<IOptions<FinanceCockpitAccessOptions>>().Value.PasswordHash);
    }

    [Fact]
    public void Kurzes_Passwort_Und_Unbekannter_Bereich_Werden_Abgelehnt()
    {
        using var sp = Build();
        using var scope = sp.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAccessPasswordAdministration>();
        Assert.False(admin.TrySetPassword(AccessPasswordAreas.Finance, "kurz", "koi"));
        Assert.False(admin.TrySetPassword("gibtsnicht", "langgenug1", "koi"));
    }

    [Fact]
    public void Alle_Bereiche_Werden_Aufgelistet()
    {
        using var sp = Build();
        using var scope = sp.CreateScope();
        var areas = scope.ServiceProvider.GetRequiredService<IAccessPasswordAdministration>().ListAccounts().Select(a => a.Area).ToList();
        Assert.Equal(["admin", "finance", "hr", "hr:hradmin", "operations", "logistik"], areas);
    }

    [Fact]
    public void Operations_Neues_Passwort_Gilt_Sofort_Und_Alte_Anmeldung_Verfaellt()
    {
        using var sp = Build();
        var monitor = sp.GetRequiredService<IOptionsMonitor<ShopfloorOptions>>();
        var protector = new EphemeralDataProtectionProvider();
        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var oldCookie = ShopfloorLogin.CreateCookieValue(protector, expires, monitor.CurrentValue.LoginPasswordHash);
        Assert.True(ShopfloorLogin.IsValidCookie(protector, oldCookie, DateTimeOffset.UtcNow, monitor.CurrentValue.LoginPasswordHash));

        using var scope = sp.CreateScope();
        Assert.True(scope.ServiceProvider.GetRequiredService<IAccessPasswordAdministration>().TrySetPassword(AccessPasswordAreas.Operations, "neuoperations1", "koi"));

        Assert.True(ShopfloorLogin.Verify(monitor.CurrentValue, "operations", "neuoperations1"));
        Assert.False(ShopfloorLogin.Verify(monitor.CurrentValue, "operations", "altoperations1"));
        Assert.False(ShopfloorLogin.IsValidCookie(protector, oldCookie, DateTimeOffset.UtcNow, monitor.CurrentValue.LoginPasswordHash));
    }

    [Fact]
    public void Finance_Hr_Logistik_Neues_Passwort_Gilt_Sofort()
    {
        using var sp = Build();
        using var scope = sp.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAccessPasswordAdministration>();
        Assert.True(admin.TrySetPassword(AccessPasswordAreas.Finance, "neufinance1", "koi"));
        Assert.True(admin.TrySetPassword(AccessPasswordAreas.HrUser("hradmin"), "neuhradmin1", "koi"));
        Assert.True(admin.TrySetPassword(AccessPasswordAreas.Logistics, "neulogistik1", "koi"));

        Assert.Equal(Hash("neufinance1"), sp.GetRequiredService<IOptions<FinanceCockpitAccessOptions>>().Value.PasswordHash);
        Assert.Equal(Hash("neuhradmin1"), sp.GetRequiredService<IOptions<HrKpiAccessOptions>>().Value.AdminUsers.Single().PasswordHash);
        Assert.Equal(Hash("neulogistik1"), sp.GetRequiredService<IOptionsMonitor<LogisticsPeopleAccessOptions>>().CurrentValue.PasswordHash);
        var finance = admin.ListAccounts().Single(a => a.Area == AccessPasswordAreas.Finance);
        Assert.Equal("koi", finance.ChangedBy);
    }

    [Fact]
    public void Operations_Login_Schalter_Gilt_Sofort_Und_Nach_Neustart()
    {
        using (var sp = Build())
        {
            var monitor = sp.GetRequiredService<IOptionsMonitor<ShopfloorOptions>>();
            Assert.False(ShopfloorAccess.IsAllowed(monitor.CurrentValue, "TRAFAG\\fremder"));

            using var scope = sp.CreateScope();
            var admin = scope.ServiceProvider.GetRequiredService<IAccessPasswordAdministration>();
            Assert.True(admin.OperationsLoginRequired);
            Assert.True(admin.TrySetOperationsLoginRequired(false, "koi"));
            Assert.False(admin.OperationsLoginRequired);
            Assert.True(ShopfloorAccess.IsAllowed(monitor.CurrentValue, "TRAFAG\\fremder"));

            _adminAccess.Unlocked = false;
            Assert.False(admin.TrySetOperationsLoginRequired(true, "koi"));
            _adminAccess.Unlocked = true;
        }

        using (var restarted = Build())   // wie nach einem Deploy: appsettings sagt wieder OpenForAll = false
        {
            Assert.True(restarted.GetRequiredService<IOptionsMonitor<ShopfloorOptions>>().CurrentValue.OpenForAll);
            using var scope = restarted.CreateScope();
            Assert.True(scope.ServiceProvider.GetRequiredService<IAccessPasswordAdministration>().TrySetOperationsLoginRequired(true, "koi"));
            Assert.False(ShopfloorAccess.IsAllowed(restarted.GetRequiredService<IOptionsMonitor<ShopfloorOptions>>().CurrentValue, "TRAFAG\\fremder"));
        }
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch (IOException) { }
    }

    private sealed class FakeAdminAccess : IAdminAccessService
    {
        public bool Unlocked { get; set; } = true;
        public bool IsEnabled => true;
        public bool IsConfigured => true;
        public bool IsUnlocked => Unlocked;
        public bool TryUnlock(string username, string password) => Unlocked;
        public bool TryChangePassword(string username, string currentPassword, string newPassword) => false;
        public void Lock() => Unlocked = false;
    }
}
