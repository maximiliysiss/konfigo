using System;
using System.Linq;
using System.Security.Claims;
using Konfigo.Domain.ValueType;

namespace Konfigo.Extensions;

internal static class UserExtensions
{
    private const string AllServicesKey = "all";
    private const string ServiceKey = "srv";

    public static UserId GetId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return value is null ? throw new InvalidOperationException("User id not found") : new UserId(value);
    }

    public static bool IsAllowed(this ClaimsPrincipal user, string serviceName)
    {
        var claims = user
            .FindAll(ServiceKey)
            .ToArray();

        return claims.Any(IsValid);

        bool IsValid(Claim c) =>
            c.Value.Equals(serviceName, StringComparison.OrdinalIgnoreCase) ||
            c.Value.Equals(AllServicesKey, StringComparison.OrdinalIgnoreCase);
    }

    public static string[]? GetServices(this ClaimsPrincipal user)
    {
        var claims = user
            .FindAll(ServiceKey)
            .ToArray();

        if (claims.Any(IsAll))
        {
            return null;
        }

        return claims
            .Select(c => c.Value)
            .ToArray();

        static bool IsAll(Claim c) => c.Value.Equals(AllServicesKey, StringComparison.OrdinalIgnoreCase);
    }
}
