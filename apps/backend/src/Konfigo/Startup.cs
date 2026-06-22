using Konfigo.Application.Extensions;
using Konfigo.Authorization;
using Konfigo.Extensions;
using Konfigo.Grpc;
using Konfigo.Hubs;
using Konfigo.Infrastructure.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Konfigo;

public sealed class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddControllers(opt => opt.Filters.Add<ServiceAccessActionFilter>());

        services
            .AddOpenApi();

        services
            .AddGrpc();

        services
            .AddSignalR();

        services
            .AddApi(configuration)
            .AddApplication()
            .AddInfrastructure();
    }

    public void Configure(IApplicationBuilder app)
    {
        const ForwardedHeaders xForwardedProto =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

        var forwardedHeadersOptions = new ForwardedHeadersOptions { ForwardedHeaders = xForwardedProto };

        app.UseForwardedHeaders(forwardedHeadersOptions);

        app.UseApi(configuration);

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(ConfigureEndpoints);
    }

    private static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenApi();
        endpoints.MapControllers();
        endpoints.MapGrpcService<RealtimeConfigGrpcService>();
        endpoints.MapHub<RealtimeConfigHub>("/hubs/config");
    }
}
