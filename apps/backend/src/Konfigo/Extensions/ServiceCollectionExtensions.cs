using System;
using Konfigo.Application.Services.Notifications;
using Konfigo.Authorization;
using Konfigo.Notifications;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;

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
                    openIdOptions.Authority = options.OpenId.Authority;
                    openIdOptions.ClientId = options.OpenId.ClientId;
                    openIdOptions.ClientSecret = options.OpenId.ClientSecret;
                    openIdOptions.RequireHttpsMetadata = options.OpenId.RequireHttpsMetadata;
                    openIdOptions.ResponseType = options.OpenId.ResponseType;

                    openIdOptions.Scope.Clear();

                    foreach (var scope in options.OpenId.Scopes)
                        openIdOptions.Scope.Add(scope);

                    openIdOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        RoleClaimType = options.RoleClaimType,
                    };
                });

            return services;
        }

        IServiceCollection AddJwt()
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwtOptions =>
                {
                    jwtOptions.Authority = options.Jwt.Authority;
                    jwtOptions.Audience = options.Jwt.Audience;
                    jwtOptions.RequireHttpsMetadata = options.Jwt.RequireHttpsMetadata;
                    jwtOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        RoleClaimType = options.RoleClaimType,
                    };
                });

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
                .AddSaml2(samlOptions =>
                {
                    samlOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    samlOptions.SPOptions.EntityId = new EntityId(options.Saml.ServiceProviderEntityId);

                    if (string.IsNullOrWhiteSpace(options.Saml.IdentityProviderEntityId))
                        return;

                    var identityProvider = new IdentityProvider(
                        new EntityId(options.Saml.IdentityProviderEntityId),
                        samlOptions.SPOptions);

                    if (!string.IsNullOrWhiteSpace(options.Saml.MetadataLocation))
                        identityProvider.MetadataLocation = options.Saml.MetadataLocation;

                    identityProvider.LoadMetadata = options.Saml.LoadMetadata;
                    samlOptions.IdentityProviders.Add(identityProvider);
                });

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
