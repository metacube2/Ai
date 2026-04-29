using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TrafagSalesExporter.Security;

namespace TrafagSalesExporter.Tests;

public class SecurityPolicyFactoryTests
{
    [Fact]
    public async Task AccessPolicy_Allows_User_In_Configured_Access_Group()
    {
        var policy = SecurityPolicyFactory.BuildAccessPolicy(new SecurityOptions
        {
            AccessGroups = ["TRAFAG\\TrafagSalesExporter-Users"]
        }, useDevelopmentAuthentication: false);

        var result = await AuthorizeAsync(policy, CreateUser(roles: ["TRAFAG\\TrafagSalesExporter-Users"]));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AccessPolicy_Denies_User_Outside_Configured_Access_Group()
    {
        var policy = SecurityPolicyFactory.BuildAccessPolicy(new SecurityOptions
        {
            AccessGroups = ["TRAFAG\\TrafagSalesExporter-Users"]
        }, useDevelopmentAuthentication: false);

        var result = await AuthorizeAsync(policy, CreateUser(roles: ["TRAFAG\\OtherGroup"]));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AccessPolicy_Allows_Authenticated_User_When_Development_Authentication_Is_Active()
    {
        var policy = SecurityPolicyFactory.BuildAccessPolicy(new SecurityOptions
        {
            AccessGroups = ["TRAFAG\\TrafagSalesExporter-Users"]
        }, useDevelopmentAuthentication: true);

        var result = await AuthorizeAsync(policy, CreateUser());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AdminPolicy_Allows_User_In_Admin_Group()
    {
        var policy = SecurityPolicyFactory.BuildAdminPolicy(new SecurityOptions
        {
            AdminGroups = ["TRAFAG\\TrafagSalesExporter-Admins"]
        }, useDevelopmentAuthentication: false);

        var result = await AuthorizeAsync(policy, CreateUser(roles: ["TRAFAG\\TrafagSalesExporter-Admins"]));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AdminPolicy_Denies_Normal_Access_User()
    {
        var policy = SecurityPolicyFactory.BuildAdminPolicy(new SecurityOptions
        {
            AdminGroups = ["TRAFAG\\TrafagSalesExporter-Admins"]
        }, useDevelopmentAuthentication: false);

        var result = await AuthorizeAsync(policy, CreateUser(roles: ["TRAFAG\\TrafagSalesExporter-Users"]));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AdminPolicy_Allows_Development_Admin_Claim()
    {
        var policy = SecurityPolicyFactory.BuildAdminPolicy(new SecurityOptions(), useDevelopmentAuthentication: true);

        var result = await AuthorizeAsync(policy, CreateUser(claims:
        [
            new Claim(DevelopmentAuthenticationHandler.AdminClaimType, "true")
        ]));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AdminPolicy_Denies_Development_User_Without_Admin_Claim()
    {
        var policy = SecurityPolicyFactory.BuildAdminPolicy(new SecurityOptions(), useDevelopmentAuthentication: true);

        var result = await AuthorizeAsync(policy, CreateUser());

        Assert.False(result.Succeeded);
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, ClaimsPrincipal user)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IAuthorizationService>();

        return await service.AuthorizeAsync(user, resource: null, policy);
    }

    private static ClaimsPrincipal CreateUser(IEnumerable<string>? roles = null, IEnumerable<Claim>? claims = null)
    {
        var allClaims = new List<Claim>
        {
            new(ClaimTypes.Name, "TRAFAG\\tester")
        };

        allClaims.AddRange((roles ?? []).Select(role => new Claim(ClaimTypes.Role, role)));
        allClaims.AddRange(claims ?? []);

        return new ClaimsPrincipal(new ClaimsIdentity(allClaims, "Test"));
    }

}
