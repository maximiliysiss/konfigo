using System;

namespace Konfigo.Domain.ValueType;

public static class LocalUserRoles
{
    public const string Admin = "admin";
    public const string Developer = "developer";

    public static readonly string[] All = [Admin, Developer];

    public static bool IsValid(string role) => Array.Exists(All, r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
}
