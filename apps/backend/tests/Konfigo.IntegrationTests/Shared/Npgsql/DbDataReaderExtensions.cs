using System.Data.Common;

namespace Konfigo.IntegrationTests.Shared.Npgsql;

internal static class DbDataReaderExtensions
{
    public static T GetFieldValue<T>(this DbDataReader reader, string name)
        => reader.IsDBNull(reader.GetOrdinal(name)) ? default! : reader.GetFieldValue<T>(reader.GetOrdinal(name));

    public static string GetString(this DbDataReader reader, string name)
        => reader.GetString(reader.GetOrdinal(name));

    public static int GetInt32(this DbDataReader reader, string name)
        => reader.GetInt32(reader.GetOrdinal(name));

    public static string? GetNullableString(this DbDataReader reader, string name)
        => reader.IsDBNull(reader.GetOrdinal(name)) ? null : reader.GetString(name);
}
