using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Konfigo.Authorization;

public sealed class KonfigoAuthorizationOptions
{
    public const string SectionName = "Authorization";

    public Dictionary<string, string[]> Policies { get; init; } = new()
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
            .Where(permission => GetRoles(permission).Any(user.IsInRole))
            .ToArray();
}
