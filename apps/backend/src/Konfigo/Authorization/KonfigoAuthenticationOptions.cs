using System.Security.Claims;

namespace Konfigo.Authorization;

public sealed class KonfigoAuthenticationOptions
{
    public const string SectionName = "Authentication";
    public AuthenticationProvider Provider { get; init; } = AuthenticationProvider.Saml;

    public string IdClaimType { get; init; } = ClaimTypes.NameIdentifier;
    public string RoleClaimType { get; init; } = ClaimsIdentity.DefaultRoleClaimType;
    public string EmailClaimType { get; init; } = ClaimTypes.Email;
}

public enum AuthenticationProvider
{
    OpenId,
    Saml,
    Jwt,
}
