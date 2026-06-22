using System;
using System.Linq;
using Konfigo.Authorization;
using Konfigo.Domain.ValueType;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Konfigo.Extensions;

internal static class UserExtensions
{
    public static User GetUser(this HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("User is not authenticated");

        var options = context.RequestServices.GetRequiredService<IOptions<KonfigoAuthenticationOptions>>();

        var id = user.FindFirst(options.Value.IdClaimType);
        var email = user.FindFirst(options.Value.EmailClaimType);
        var roles = user.FindAll(options.Value.RoleClaimType).Select(c => c.Value).ToArray();

        if (string.IsNullOrEmpty(id?.Value) || string.IsNullOrEmpty(email?.Value) || roles is [])
            throw new InvalidOperationException("User is missing required claims");

        return new User(Id: new UserId(id.Value), Email: email.Value, Roles: roles);
    }
}
