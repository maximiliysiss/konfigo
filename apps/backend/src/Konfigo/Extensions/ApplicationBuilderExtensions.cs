using Konfigo.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Konfigo.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApi(this IApplicationBuilder app, IConfiguration configuration)
    {
        var options = configuration
            .GetSection(KonfigoAuthenticationOptions.SectionName)
            .Get<KonfigoAuthenticationOptions>() ?? new KonfigoAuthenticationOptions();

        if (options.Provider == AuthenticationProvider.Saml)
        {
            // Sustainsys.Saml2 sets SameSite=None on state cookies for cross-site SAML flows.
            // Browsers reject SameSite=None without Secure over HTTP, so the ACS callback
            // never receives the cookie. Downgrade to Lax when not on HTTPS.
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Unspecified,
                OnAppendCookie = ctx =>
                {
                    if (!ctx.Context.Request.IsHttps && ctx.CookieOptions.SameSite == SameSiteMode.None)
                        ctx.CookieOptions.SameSite = SameSiteMode.Lax;
                },
                OnDeleteCookie = ctx =>
                {
                    if (!ctx.Context.Request.IsHttps && ctx.CookieOptions.SameSite == SameSiteMode.None)
                        ctx.CookieOptions.SameSite = SameSiteMode.Lax;
                }
            });
        }

        return app;
    }
}
