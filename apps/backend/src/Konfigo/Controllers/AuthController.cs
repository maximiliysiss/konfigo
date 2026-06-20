using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Konfigo.Authorization;
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
    [HttpGet("auth/config")]
    public IActionResult GetConfig()
    {
        var opts = _authenticationOptions.CurrentValue;
        var provider = opts.Provider;

        if (provider == AuthenticationProvider.Jwt)
        {
            return Ok(new
            {
                provider = "jwt",
                jwt = new
                {
                    authorizeUrl = opts.Jwt.AuthorizeUrl,
                    tokenUrl = opts.Jwt.TokenUrl,
                    clientId = opts.Jwt.ClientId,
                    scopes = opts.Jwt.Scopes
                }
            });
        }

        return Ok(new { provider = provider.ToString().ToLowerInvariant() });
    }

    [AllowAnonymous]
    [HttpGet("auth/login")]
    public Task Login([FromQuery] string? returnUrl)
    {
        var safeReturn = IsSafeReturnUrl(returnUrl) ? returnUrl! : "/";
        _logger.LogAuthenticationLoginChallengeStarted(returnUrl, safeReturn);

        return HttpContext.ChallengeAsync(
            scheme: GetChallengeScheme(_authenticationOptions.CurrentValue.Provider),
            properties: new AuthenticationProperties { RedirectUri = safeReturn });
    }

    [Authorize]
    [HttpGet("auth/logout")]
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
    [HttpGet("auth/me")]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            _logger.LogCurrentUserUnauthenticated();
            return NoContent();
        }

        var value = new
        {
            id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value,
            name = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("displayName")?.Value,
            roles = User.Identities
                .SelectMany(identity => User.FindAll(identity.RoleClaimType))
                .Select(c => c.Value)
                .Distinct()
                .ToArray(),
            permissions = _authorizationOptions.CurrentValue.GetPermissions(User)
        };

        _logger.LogCurrentUserCompleted(value.id, value.roles.Length);

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
