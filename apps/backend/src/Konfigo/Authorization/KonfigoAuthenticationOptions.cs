using System.Security.Claims;

namespace Konfigo.Authorization;

public sealed class KonfigoAuthenticationOptions
{
    public const string SectionName = "Authentication";
    public AuthenticationProvider Provider { get; init; } = AuthenticationProvider.OpenId;

    public string IdClaimType { get; init; } = ClaimTypes.NameIdentifier;
    public string RoleClaimType { get; init; } = ClaimsIdentity.DefaultRoleClaimType;
    public string EmailClaimType { get; init; } = ClaimTypes.Email;

    public KonfigoJwtOptions Jwt { get; init; } = new();
    public KonfigoSamlOptions Saml { get; init; } = new();
}

public sealed class KonfigoJwtOptions
{
    public string? ClientId { get; init; }
    public string? AuthorizeUrl { get; init; }
    public string? TokenUrl { get; init; }
    public string Scopes { get; init; } = "openid profile email";
}

public sealed class KonfigoSamlOptions
{
    public string SpOptionsEntityId { get; init; } = string.Empty;
    public string SpOptionsModulePath { get; init; } = string.Empty;
    public string IdentityProviderEntityId { get; init; } = string.Empty;
    public string IdentityProviderMetadataUrl { get; init; } = string.Empty;
}

public enum AuthenticationProvider
{
    OpenId,
    Saml,
    Jwt,
}
