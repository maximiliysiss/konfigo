using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.VersionTableInfo;

namespace Konfigo.Infrastructure.Persistence.Migrations.Infrastructure;

internal sealed class VersionTableMetaDataAccessor(IVersionTableMetaData versionTableMetaData) : IVersionTableMetaDataAccessor
{
    public IVersionTableMetaData VersionTableMetaData { get; } = versionTableMetaData;
}
