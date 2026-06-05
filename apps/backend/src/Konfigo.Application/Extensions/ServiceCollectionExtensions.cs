using Konfigo.Application.Infrastructure.DateTime;
using Konfigo.Application.Services.ApplicationServices;
using Konfigo.Application.Services.ApplicationServices.Audit;
using Konfigo.Application.Services.Configurations;
using Konfigo.Application.Services.Configurations.Audit;
using Konfigo.Application.Services.Configurations.Options;
using Konfigo.Application.Services.Configurations.Tracking;
using Konfigo.Application.Services.Notifications;
using Konfigo.Application.Services.Notifications.Outbox.Grcp;
using Konfigo.Application.Services.Updater;
using Microsoft.Extensions.DependencyInjection;
using Publo.Abstraction.Extensions;

namespace Konfigo.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services
            .AddScoped<IApplicationsService, ApplicationsService>()
            .AddScoped<IConfigEntryService, ConfigEntryService>()
            .AddScoped<IConfigVersionService, ConfigVersionService>();

        services
            .Decorate<IConfigEntryService, TrackingConfigEntryService>();

        services
            .Decorate<IApplicationsService, AuditApplicationsService>()
            .Decorate<IConfigEntryService, AuditConfigEntryService>()
            .Decorate<IConfigVersionService, AuditConfigVersionService>();

        services
            .AddScoped<IConfigChangeNotifier, GrpcConfigChangeNotifier>();

        services
            .AddPubloExecutor<GrpcEvent, GrpcEventExecutor>();

        services
            .AddSingleton<IUpdaterService, UpdaterService>();

        services
            .AddOptions<ConfigEntryServiceOptions>()
            .BindConfiguration(nameof(ConfigEntryServiceOptions));

        services
            .AddOptions<ConfigVersionServiceOptions>()
            .BindConfiguration(nameof(ConfigVersionServiceOptions));

        return services;
    }
}
