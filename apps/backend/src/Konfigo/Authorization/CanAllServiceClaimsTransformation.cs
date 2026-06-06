using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Konfigo.Authorization;

public sealed class CanAllServiceClaimsTransformation(IOptions<KonfigoAuthorizationOptions> options) : IClaimsTransformation
{
    private const string AllServicesValue = "all";
    private const string ServiceClaimType = "srv";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(ServiceClaimType, AllServicesValue))
        {
            return Task.FromResult(principal);
        }

        var hasCanAllRole = options.Value
            .GetRoles(AuthorizationPolicyNames.CanAll)
            .Any(role => options.Value.UserHasRole(principal, role));

        if (!hasCanAllRole)
        {
            return Task.FromResult(principal);
        }

        var identity = principal.Identities.FirstOrDefault(x => x.IsAuthenticated);
        identity?.AddClaim(new Claim(ServiceClaimType, AllServicesValue));

        return Task.FromResult(principal);
    }
}
