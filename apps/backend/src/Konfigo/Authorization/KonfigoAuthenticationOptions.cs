namespace Konfigo.Authorization;

public sealed class KonfigoAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public AuthenticationProvider Provider { get; init; } = AuthenticationProvider.Saml;

    public SamlAuthenticationOptions Saml { get; init; } = new();

    public OpenIdAuthenticationOptions OpenId { get; init; } = new();

    public JwtAuthenticationOptions Jwt { get; init; } = new();
}

public enum AuthenticationProvider
{
    OpenId,
    Saml,
    Jwt,
}

public sealed class SamlAuthenticationOptions
{
    public string ServiceProviderEntityId { get; init; } = "konfigo";

    public string IdentityProviderEntityId { get; init; } = string.Empty;

    public string MetadataLocation { get; init; } = string.Empty;

    public bool LoadMetadata { get; init; } = true;
}

public sealed class OpenIdAuthenticationOptions
{
    public string Authority { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string ResponseType { get; init; } = "code";

    public string[] Scopes { get; init; } = ["openid", "profile", "email"];
}

public sealed class JwtAuthenticationOptions
{
    public string Authority { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public bool RequireHttpsMetadata { get; init; } = true;
}
