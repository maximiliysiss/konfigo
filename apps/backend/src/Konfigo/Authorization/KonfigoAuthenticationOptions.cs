using System.Security.Claims;

namespace Konfigo.Authorization;

public sealed class KonfigoAuthenticationOptions
{
    public const string SectionName = "Authentication";
    public AuthenticationProvider Provider { get; init; } = AuthenticationProvider.Saml;
    public string RoleClaimType { get; init; } = ClaimsIdentity.DefaultRoleClaimType;
}

public enum AuthenticationProvider
{
    OpenId,
    Saml,
    Jwt,
}
