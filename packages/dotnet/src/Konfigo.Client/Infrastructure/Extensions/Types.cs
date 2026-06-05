using System;

namespace Konfigo.Client.Infrastructure.Extensions;

internal static class Types
{
    public static bool IsNumber(this Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type == typeof(decimal) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(uint) ||
               type == typeof(ulong) ||
               type == typeof(ushort) ||
               type == typeof(sbyte);
    }
}
