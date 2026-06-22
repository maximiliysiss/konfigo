using System;

namespace Konfigo.Domain.ValueType;

public sealed record User(UserId Id, string Email, string[] Roles)
{
    public bool IsAdmin() => Roles.Contains("admin", StringComparer.OrdinalIgnoreCase);
}
