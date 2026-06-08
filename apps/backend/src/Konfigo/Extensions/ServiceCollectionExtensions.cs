using System;
using System.Threading.Tasks;
using Konfigo.Application.Services.Notifications;
using Konfigo.Authorization;
using Konfigo.Notifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sustainsys.Saml2.AspNetCore2;

namespace Konfigo.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddScoped<IConfigChangeNotifier, SignalRConfigChangeNotifier>();

        services
            .Configure<KonfigoAuthenticationOptions>(configuration.GetSection(KonfigoAuthenticationOptions.SectionName))
            .Configure<KonfigoAuthorizationOptions>(configuration.GetSection(KonfigoAuthorizationOptions.SectionName));

        services
            .AddScoped<IAuthorizationHandler, ConfiguredRolesAuthorizationHandler>();
        services
            .AddScoped<IClaimsTransformation, CanAllServiceClaimsTransformation>();

        services.AddConfiguredAuthentication(configuration);
        services.AddConfiguredAuthorization();

        return services;
    }

    private static IServiceCollection AddConfiguredAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration
            .GetSection(KonfigoAuthenticationOptions.SectionName)
            .Get<KonfigoAuthenticationOptions>() ?? new KonfigoAuthenticationOptions();

        return options.Provider switch
        {
            AuthenticationProvider.OpenId => AddOpenId(),
            AuthenticationProvider.Saml => AddSaml(),
            AuthenticationProvider.Jwt => AddJwt(),
            _ => throw new InvalidOperationException("No authentication provider configured.")
        };

        IServiceCollection AddOpenId()
        {
            services
                .AddAuthentication(authenticationOptions =>
                {
                    authenticationOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    authenticationOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    authenticationOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddOpenIdConnect(openIdOptions =>
                {
                    configuration
                        .GetSection($"{KonfigoAuthenticationOptions.SectionName}:OpenId")
                        .Bind(openIdOptions);

                    openIdOptions.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProviderForSignOut = ctx =>
                        {
                            if (!string.IsNullOrEmpty(ctx.ProtocolMessage.IssuerAddress))
                                return Task.CompletedTask;

                            ctx.Response.Redirect(ctx.Properties.RedirectUri ?? "/login");
                            ctx.HandleResponse();

                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        IServiceCollection AddJwt()
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwtOptions => configuration.GetSection($"{KonfigoAuthenticationOptions.SectionName}:Jwt").Bind(jwtOptions));

            return services;
        }

        IServiceCollection AddSaml()
        {
            services
                .AddAuthentication(authenticationOptions =>
                {
                    authenticationOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    authenticationOptions.DefaultChallengeScheme = Saml2Defaults.Scheme;
                    authenticationOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddSaml2(samlOptions => configuration.GetSection($"{KonfigoAuthenticationOptions.SectionName}:Saml").Bind(samlOptions));

            return services;
        }
    }

    private static void AddConfiguredAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicyNames.CanAll,
                policy =>
                    policy
                        .RequireAuthenticatedUser()
                        .AddRequirements(new ConfiguredRolesRequirement(AuthorizationPolicyNames.CanAll)));

            options.AddPolicy(
                AuthorizationPolicyNames.CanChange,
                policy =>
                    policy
                        .RequireAuthenticatedUser()
                        .AddRequirements(new ConfiguredRolesRequirement(AuthorizationPolicyNames.CanChange)));
        });
    }
}
