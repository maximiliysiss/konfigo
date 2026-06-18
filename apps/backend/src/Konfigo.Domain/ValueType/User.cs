using System;

namespace Konfigo.Domain.ValueType;

public sealed record User(UserId Id, string Email, string Role)
{
    public bool IsAdmin() => string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);
}
