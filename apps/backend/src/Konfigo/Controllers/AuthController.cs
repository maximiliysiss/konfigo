using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.LocalUsers;
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
    private readonly ILocalUsersService _localUsersService;

    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IOptionsMonitor<KonfigoAuthenticationOptions> authenticationOptions,
        IOptionsMonitor<KonfigoAuthorizationOptions> authorizationOptions,
        ILocalUsersService localUsersService,
        ILogger<AuthController> logger)
    {
        _authenticationOptions = authenticationOptions;
        _authorizationOptions = authorizationOptions;
        _localUsersService = localUsersService;
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
        var provider = _authenticationOptions.CurrentValue.Provider;
        if (provider is AuthenticationProvider.Jwt or AuthenticationProvider.Local)
        {
            return Task.CompletedTask;
        }

        var safeReturn = IsSafeReturnUrl(returnUrl) ? returnUrl! : "/";
        _logger.LogAuthenticationLoginChallengeStarted(returnUrl, safeReturn);

        return HttpContext.ChallengeAsync(
            scheme: GetChallengeScheme(provider),
            properties: new AuthenticationProperties { RedirectUri = safeReturn });
    }

    [AllowAnonymous]
    [HttpPost("local/login")]
    public async Task<IActionResult> LocalLogin([FromBody] LocalLoginRequest request, CancellationToken cancellationToken)
    {
        if (_authenticationOptions.CurrentValue.Provider != AuthenticationProvider.Local)
        {
            return NotFound();
        }

        var user = await _localUsersService.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var authenticationOptions = _authenticationOptions.CurrentValue;

        var claims = new List<Claim>
        {
            new(authenticationOptions.IdClaimType, user.Username),
            new(authenticationOptions.EmailClaimType, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok();
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

        if (provider == AuthenticationProvider.Local)
        {
            return;
        }

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
