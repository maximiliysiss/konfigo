using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Konfigo.Authorization;
using Konfigo.Controllers.Models.Auth;
using Konfigo.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sustainsys.Saml2.AspNetCore2;

namespace Konfigo.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IOptionsMonitor<KonfigoAuthenticationOptions> _authenticationOptions;
    private readonly IOptionsMonitor<KonfigoAuthorizationOptions> _authorizationOptions;

    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IOptionsMonitor<KonfigoAuthenticationOptions> authenticationOptions,
        IOptionsMonitor<KonfigoAuthorizationOptions> authorizationOptions,
        ILogger<AuthController> logger)
    {
        _authenticationOptions = authenticationOptions;
        _authorizationOptions = authorizationOptions;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("config")]
    public ProviderConfiguration GetConfig()
    {
        var authenticationOptions = _authenticationOptions.CurrentValue;
        var provider = authenticationOptions.Provider.ToString().ToLowerInvariant();

        return authenticationOptions.Provider switch
        {
            AuthenticationProvider.Jwt => new ProviderConfiguration
            {
                Provider = provider,
                Jwt = new ProviderConfiguration.JwtOptions
                {
                    AuthorizeUrl = authenticationOptions.Jwt.AuthorizeUrl,
                    TokenUrl = authenticationOptions.Jwt.TokenUrl,
                    ClientId = authenticationOptions.Jwt.ClientId,
                    Scopes = authenticationOptions.Jwt.Scopes
                }
            },
            _ => new ProviderConfiguration { Provider = provider },
        };
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public Task Login([FromQuery] string? returnUrl)
    {
        var safeReturn = IsSafeReturnUrl(returnUrl) ? returnUrl! : "/";
        _logger.LogAuthenticationLoginChallengeStarted(returnUrl, safeReturn);

        return HttpContext.ChallengeAsync(
            scheme: GetChallengeScheme(_authenticationOptions.CurrentValue.Provider),
            properties: new AuthenticationProperties { RedirectUri = safeReturn });
    }

    [Authorize]
    [HttpGet("logout")]
    public async Task Logout([FromQuery] string? returnUrl)
    {
        var safeReturn = IsSafeReturnUrl(returnUrl) ? returnUrl! : "/login";
        _logger.LogAuthenticationLogoutStarted(returnUrl, safeReturn);

        var provider = _authenticationOptions.CurrentValue.Provider;
        if (provider == AuthenticationProvider.Jwt)
        {
            return;
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(
            scheme: GetChallengeScheme(provider),
            properties: new AuthenticationProperties { RedirectUri = safeReturn });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            _logger.LogCurrentUserUnauthenticated();
            return NoContent();
        }

        var value = new
        {
            id = User.FindFirst(_authenticationOptions.CurrentValue.IdClaimType)?.Value,
            email = User.FindFirst(_authenticationOptions.CurrentValue.EmailClaimType)?.Value,
            name = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("displayName")?.Value,
            roles = User.Identities
                .SelectMany(identity => User.FindAll(identity.RoleClaimType))
                .Select(c => c.Value)
                .ToHashSet(),
            permissions = _authorizationOptions.CurrentValue.GetPermissions(User)
        };

        _logger.LogCurrentUserCompleted(value.id, value.roles.Count);

        return Ok(value);
    }

    private static bool IsSafeReturnUrl(string? url) =>
        !string.IsNullOrEmpty(url)
        && Uri.IsWellFormedUriString(url, UriKind.Relative)
        && url.StartsWith('/')
        && !url.StartsWith("//")
        && !url.StartsWith("/\\");

    private static string GetChallengeScheme(AuthenticationProvider provider)
    {
        return provider switch
        {
            AuthenticationProvider.OpenId => OpenIdConnectDefaults.AuthenticationScheme,
            AuthenticationProvider.Jwt => JwtBearerDefaults.AuthenticationScheme,
            _ => Saml2Defaults.Scheme,
        };
    }
}
