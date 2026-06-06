using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Konfigo.Authorization;

internal sealed class ConfiguredRolesAuthorizationHandler : AuthorizationHandler<ConfiguredRolesRequirement>
{
    private readonly IOptionsMonitor<KonfigoAuthorizationOptions> _authorizationOptions;

    public ConfiguredRolesAuthorizationHandler(IOptionsMonitor<KonfigoAuthorizationOptions> authorizationOptions)
        => _authorizationOptions = authorizationOptions;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ConfiguredRolesRequirement requirement)
    {
        var options = _authorizationOptions.CurrentValue;
        var roles = options.GetRoles(requirement.PolicyKey);

        if (roles.Length > 0 && roles.Any(role => options.UserHasRole(context.User, role)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
