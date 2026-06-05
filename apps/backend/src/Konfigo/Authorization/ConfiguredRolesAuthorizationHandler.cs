using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Konfigo.Authorization;

internal sealed class ConfiguredRolesAuthorizationHandler : AuthorizationHandler<ConfiguredRolesRequirement>
{
    private readonly IOptionsMonitor<KonfigoAuthorizationOptions> _options;

    public ConfiguredRolesAuthorizationHandler(IOptionsMonitor<KonfigoAuthorizationOptions> options) => _options = options;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ConfiguredRolesRequirement requirement)
    {
        var roles = _options.CurrentValue.GetRoles(requirement.PolicyKey);

        if (roles.Length > 0 && roles.Any(context.User.IsInRole))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
