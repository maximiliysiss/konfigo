using System;
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
        var role = user.FindFirst(options.Value.RoleClaimType);

        if (string.IsNullOrEmpty(id?.Value) || string.IsNullOrEmpty(email?.Value) || string.IsNullOrEmpty(role?.Value))
            throw new InvalidOperationException("User is missing required claims");

        return new User(Id: new UserId(id.Value), Email: email.Value, Role: role.Value);
    }
}
