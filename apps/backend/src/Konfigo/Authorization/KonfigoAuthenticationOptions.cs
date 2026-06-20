using System.Security.Claims;

namespace Konfigo.Authorization;

public sealed class KonfigoAuthenticationOptions
{
    public const string SectionName = "Authentication";
    public AuthenticationProvider Provider { get; init; } = AuthenticationProvider.Saml;

    public string IdClaimType { get; init; } = ClaimTypes.NameIdentifier;
    public string RoleClaimType { get; init; } = ClaimsIdentity.DefaultRoleClaimType;
    public string EmailClaimType { get; init; } = ClaimTypes.Email;

    public KonfigoJwtOptions Jwt { get; init; } = new();
}

public sealed class KonfigoJwtOptions
{
    public string? ClientId { get; init; }
    public string? AuthorizeUrl { get; init; }
    public string? TokenUrl { get; init; }
    public string Scopes { get; init; } = "openid profile email";
}

public enum AuthenticationProvider
{
    OpenId,
    Saml,
    Jwt,
}
