using Microsoft.AspNetCore.Authorization;

namespace TrafagSalesExporter.Security;

public static class SecurityPolicyFactory
{
    public static AuthorizationPolicy BuildAccessPolicy(SecurityOptions settings, bool useDevelopmentAuthentication)
    {
        var builder = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser();

        if (!useDevelopmentAuthentication && settings.AccessGroups.Count > 0)
        {
            builder.RequireAssertion(context =>
                settings.AccessGroups.Any(group => context.User.IsInRole(group)));
        }

        return builder.Build();
    }

    public static AuthorizationPolicy BuildAdminPolicy(SecurityOptions settings, bool useDevelopmentAuthentication)
    {
        var builder = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser();

        builder.RequireAssertion(context =>
            useDevelopmentAuthentication && context.User.HasClaim(DevelopmentAuthenticationHandler.AdminClaimType, "true") ||
            settings.AdminGroups.Any(group => context.User.IsInRole(group)));

        return builder.Build();
    }
}
