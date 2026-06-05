using FluentMigrator.Runner.VersionTableInfo;

namespace Konfigo.Infrastructure.Persistence.Migrations.Configuration;

internal sealed class VersionTableMetaData : IVersionTableMetaData
{
    public bool OwnsSchema => false;

    public string SchemaName => "public";

    public string TableName => "version_info";

    public string ColumnName => "version";

    public string UniqueIndexName => "version_info_version_idx";

    public string AppliedOnColumnName => "applied_on";
    public bool CreateWithPrimaryKey => false;

    public string DescriptionColumnName => "description";
}
