using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Konfigo.Authorization;

public sealed class KonfigoAuthorizationOptions
{
    public const string SectionName = "Authorization";

    public string[] KnownRoleClaimTypes { get; set; } = [ClaimTypes.Role, "role", "roles", "groups", "name"];

    public Dictionary<string, string[]> Policies { get; set; } = new()
    {
        [AuthorizationPolicyNames.CanAll] = ["admin"],
        [AuthorizationPolicyNames.CanChange] = ["developer"],
    };

    public string[] GetRoles(string policyKey)
    {
        var roles = Policies.TryGetValue(policyKey, out var configuredRoles)
            ? configuredRoles
            : [];

        if (policyKey == AuthorizationPolicyNames.CanChange)
            roles = [.. roles, .. GetRoles(AuthorizationPolicyNames.CanAll)];

        return roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct()
            .ToArray();
    }

    public string[] GetPermissions(ClaimsPrincipal user) =>
        new[] { AuthorizationPolicyNames.CanAll, AuthorizationPolicyNames.CanChange }
            .Where(permission => GetRoles(permission).Any(role => UserHasRole(user, role)))
            .ToArray();

    public bool UserHasRole(ClaimsPrincipal user, string role) =>
        user.IsInRole(role)
        || user.Identities.Any(identity =>
            identity.Claims.Any(claim => IsRoleClaim(claim, identity.RoleClaimType) && claim.Value == role));

    private bool IsRoleClaim(Claim claim, string identityRoleClaimType) =>
        claim.Type == identityRoleClaimType
        || KnownRoleClaimTypes.Contains(claim.Type);
}
