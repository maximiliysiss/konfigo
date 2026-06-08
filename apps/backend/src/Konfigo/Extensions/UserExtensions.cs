using System;
using System.Security.Claims;
using Konfigo.Domain.ValueType;

namespace Konfigo.Extensions;

internal static class UserExtensions
{
    public static UserId GetId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return value is null ? throw new InvalidOperationException("User id not found") : new UserId(value);
    }

    public static UserId? GetMemberId(this ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("User is not authenticated");

        return user.IsInRole("admin")
            ? null
            : user.GetId();
    }
}
