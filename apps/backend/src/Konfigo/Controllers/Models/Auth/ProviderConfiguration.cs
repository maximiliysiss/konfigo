namespace Konfigo.Controllers.Models.Auth;

public sealed class ProviderConfiguration
{
    public required string Provider { get; set; }
    public JwtOptions? Jwt { get; set; }

    public sealed class JwtOptions
    {
        public required string? AuthorizeUrl { get; set; }
        public required string? TokenUrl { get; set; }
        public required string? ClientId { get; set; }
        public required string Scopes { get; set; }
    }
}
