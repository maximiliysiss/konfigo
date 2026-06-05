using System;
using System.Reflection;

namespace Konfigo.Client.Extensions;

internal static class TypeExtensions
{
    public static bool HasCustomAttribute<T>(this Type type) where T : Attribute => type.GetCustomAttribute<T>() is not null;
    public static bool HasCustomAttribute<T>(this PropertyInfo prop) where T : Attribute => prop.GetCustomAttribute<T>() is not null;
}
