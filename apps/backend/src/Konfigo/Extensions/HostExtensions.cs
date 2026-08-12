using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.LocalUsers;
using Konfigo.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Konfigo.Extensions;

public static class HostExtensions
{
    public static async Task<IHost> SeedLocalAdminAsync(this IHost host, CancellationToken cancellationToken)
    {
        using var scope = host.Services.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<IOptions<KonfigoAuthenticationOptions>>().Value;
        if (options.Provider != AuthenticationProvider.Local)
            return host;

        var localUsersService = scope.ServiceProvider.GetRequiredService<ILocalUsersService>();

        await localUsersService.EnsureSeededAsync(options.Local.DefaultAdminUsername, options.Local.DefaultAdminPassword, cancellationToken);

        return host;
    }
}
